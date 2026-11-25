using System;
using System.Collections.Generic;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Planning
{
    /// <summary>칸 하나를 실행한 직후의 예상 (계획 보드 흐름 띠·칸 꼬리표).</summary>
    public sealed class SlotProjection
    {
        public int Fatigue;
        public int Stress;
        public int SleepDebt;

        /// <summary>이 칸에서 바뀐 양 (회복이면 음수).</summary>
        public int FatigueDelta;
        public int StressDelta;

        /// <summary>탐험처럼 무작위가 커서 "대략"인 칸.</summary>
        public bool Uncertain;

        /// <summary>이 칸에서 오를 능력치 기대값 (StatType 순서).</summary>
        public readonly float[] Gains = new float[WeekForecast.StatCount];
    }

    /// <summary>한 주 계획의 흐름 예측: 칸마다 피로·스트레스·수면 부채와 능력치 기대 증감.</summary>
    public sealed class WeekProjection
    {
        public int StartFatigue;
        public int StartStress;
        public int StartSleepDebt;
        public SlotProjection[] Slots = new SlotProjection[WeekPlan.SlotCount];

        /// <summary>한 주 능력치 기대 증감 합 (StatType 순서).</summary>
        public readonly float[] StatTotals = new float[WeekForecast.StatCount];

        public SlotProjection Last => Slots[Slots.Length - 1];
    }

    /// <summary>
    /// 계획 흐름 예측 (Docs/TEACHER_PLAN_PROPOSAL.md 6절). WeekRunner와 같은 규칙을 칸마다 따르되,
    /// 판정·탐험의 무작위는 기대값(평균)으로 둔다. 아침·저녁 식사, 동료 외출, 이벤트는 넣지 않는다.
    /// 상태를 바꾸지 않는다.
    /// </summary>
    public static class WeekForecast
    {
        public static readonly int StatCount = Enum.GetValues(typeof(StatType)).Length;

        /// <param name="actionIds">미리 볼 계획 (null이면 지금 계획). 빈 칸은 휴식으로 본다.</param>
        public static WeekProjection Project(GameState state, IDefinitionDatabase db, IList<string> actionIds = null)
        {
            var balance = db.Balance;
            var player = state.Player;
            var plan = state.Plan.Slots;
            var count = WeekPlan.SlotCount;
            var result = new WeekProjection
            {
                StartFatigue = player.Vitals.Fatigue,
                StartStress = player.Vitals.Stress,
                StartSleepDebt = player.SleepDebt,
            };

            var fatigue = player.Vitals.Fatigue;
            var stress = player.Vitals.Stress;
            var debt = player.SleepDebt;
            var streak = player.NightRestStreak;
            var previousWasExpedition = player.LastSlotWasExpedition;
            var routineId = player.RoutineActionId ?? "";
            var routineStreak = player.RoutineStreak;
            var reducedRest = player.ReducedNextRest;
            var stats = new int[StatCount];
            for (var s = 0; s < StatCount; s++)
            {
                stats[s] = player.GetStat((StatType)s);
            }

            for (var i = 0; i < count; i++)
            {
                var slot = new SlotProjection();
                result.Slots[i] = slot;
                var planned = actionIds != null ? (i < actionIds.Count ? actionIds[i] : "") : (i < plan.Count ? plan[i].ActionId : "");
                var id = string.IsNullOrEmpty(planned) ? PlanPreparer.RestIdFor(i, balance) : planned;
                var fatigueBefore = fatigue;
                var stressBefore = stress;

                if (!db.TryGetAction(id, out var action))
                {
                    previousWasExpedition = false;
                    Finish(slot, fatigue, stress, debt, fatigueBefore, stressBefore);
                    continue;
                }

                // 강제 휴식: 앞 칸이 탐험이었거나 미리 잠긴 휴식 칸 (PlanPreparer.ApplyForcedRest와 같은 기준)
                var previousId = i == 0 ? null : Id(actionIds, plan, i - 1);
                var forced = (i < plan.Count && plan[i].Locked && plan[i].ForcedRest) || (i > 0 && PlanPreparer.IsExpedition(db, previousId));
                var consecutive = action.IsExpedition && previousWasExpedition;
                if (forced && !action.IsRest)
                {
                    fatigue = Clamp(fatigue + balance.ViolationFatigue, balance);
                    stress = Clamp(stress + balance.ViolationStress, balance);
                    if (!consecutive)
                    {
                        debt = AddDebt(debt, balance.ForcedRestViolationSleepDebt, balance);
                    }
                }

                if (consecutive)
                {
                    debt = AddDebt(debt, balance.ConsecutiveExpeditionSleepDebt, balance);
                }

                if (action.IsExpedition)
                {
                    slot.Uncertain = true;
                    if (db.TryGetExpeditionModel(action.LocationId, out var model))
                    {
                        var yield = action.ExpeditionTime == ExpeditionTime.Night ? model.Night : model.Day;
                        var baseStress = (yield.StressMin + Math.Max(yield.StressMin, yield.StressMax)) / 2f;
                        var scaled = (int)Math.Round(baseStress * (1f + TraitRules.Sum(state, db, TraitModifierKind.ExpeditionStress)));
                        fatigue = Clamp(fatigue + yield.Fatigue, balance);
                        stress = Clamp(stress + StressRules.HitAt(state, balance, scaled, 0, fatigue, debt), balance);
                    }

                    if (action.ExpeditionTime == ExpeditionTime.Night)
                    {
                        debt = AddDebt(debt, balance.NightExpeditionSleepDebt, balance);
                    }
                }
                else
                {
                    if (CalendarRules.PhaseOf(i) == DayPhase.Day && action.Category != ActionCategory.Education)
                    {
                        routineId = "";
                        routineStreak = 0;
                    }

                    ApplyAction(state, db, action, i, slot, stats, ref fatigue, ref stress, debt, ref routineId, ref routineStreak, ref reducedRest);
                }

                if (CalendarRules.PhaseOf(i) == DayPhase.Night)
                {
                    if (action.IsRest)
                    {
                        streak++;
                        if (streak >= balance.NightRestStreakToRepay)
                        {
                            debt = AddDebt(debt, -balance.NightRestRepayAmount, balance);
                            streak = 0;
                        }
                    }
                    else
                    {
                        streak = 0;
                    }
                }

                previousWasExpedition = action.IsExpedition;
                Finish(slot, fatigue, stress, debt, fatigueBefore, stressBefore);
                for (var s = 0; s < StatCount; s++)
                {
                    result.StatTotals[s] += slot.Gains[s];
                }
            }

            return result;
        }

        private static void ApplyAction(GameState state, IDefinitionDatabase db, ActionDef action, int index, SlotProjection slot, int[] stats,
            ref int fatigue, ref int stress, int debt, ref string routineId, ref int routineStreak, ref bool reducedRest)
        {
            var balance = db.Balance;
            var affordable = true;
            foreach (var cost in action.ResourceCosts)
            {
                if (state.Base.GetResource(cost.Resource) < SpecialRules.Cost(state, balance, cost))
                {
                    affordable = false;
                }
            }

            affordable &= PowerRules.Allows(state, balance, action);

            // 제작: 착수 칸은 레시피 비용, 뒤 칸("제작 중")은 이어 하기 비용 (CraftRules.ChargeVitals)
            if (CraftRules.IsContinue(action, balance))
            {
                fatigue = Clamp(fatigue + balance.CraftContinueFatigue, balance);
                stress = Clamp(stress + StressRules.HitActionAt(state, balance, balance.CraftContinueStress, 0, fatigue, debt), balance);
                return;
            }

            fatigue = Clamp(fatigue + action.FatigueCost, balance);
            var favorite = EchoRules.IsFavorite(state, balance, action);
            stress = Clamp(stress + StressRules.HitActionAt(state, balance, action.StressCost, favorite ? balance.FavoriteSubjectStressRelief : 0, fatigue, debt), balance);

            var chance = 1f;
            var great = 0f;
            if (action.HasCheck && !action.IsCraft)
            {
                chance = affordable ? Chance(state, db, action, fatigue, stress) : 0f;
                great = balance.GreatSuccessRatio + TraitRules.Sum(state, db, TraitModifierKind.GreatSuccessBonus)
                                                  + MentorBondRules.GreatSuccessBonus(state, action, balance);
            }

            if (action.IsCraft && CraftRules.CheckStart(state, db, action) != CraftBlock.None)
            {
                affordable = false;
            }

            // 연속 수업 보너스 (ActionResolver.UpdateRoutine과 같은 규칙)
            var routineBonus = 0f;
            if (action.Category == ActionCategory.Education)
            {
                routineStreak = routineId == action.Id ? routineStreak + 1 : 0;
                routineId = action.Id;
                routineBonus = ActionResolver.RoutineBonus(state.Player, routineStreak, balance);
            }

            if (affordable)
            {
                // 판정 결과의 기대 배율: 대성공·성공·실패를 확률로 섞는다
                var success = ActionResolver.GainMultiplierAt(state, action, CheckResult.Success, routineBonus, balance, debt);
                var factor = action.HasCheck && !action.IsCraft
                    ? chance * (1f - great) + chance * great * balance.GreatSuccessGainMultiplier + (1f - chance) * balance.FailureGainMultiplier
                    : 1f;
                var multiplier = success * factor * ActionResolver.ExtraGainMultiplier(state, action, db);
                foreach (var gain in action.StatGains)
                {
                    var stat = (int)gain.Stat;
                    var amount = StatRules.ScaleGain(stats[stat], gain.Amount, multiplier, balance);
                    slot.Gains[stat] += amount;
                    stats[stat] += amount;
                }

                // 결과 효과의 피로·스트레스 (휴식 회복 등): 판정이 있으면 성공·실패를 확률로 섞는다
                var recovery = 1f;
                if (action.IsRest)
                {
                    recovery = (reducedRest ? balance.ReducedRestMultiplier : 1f) * (1f + TraitRules.Sum(state, db, TraitModifierKind.RestRecovery));
                    reducedRest = false;
                }

                var hit = action.HasCheck && !action.IsCraft ? chance : 1f;
                fatigue = Clamp(fatigue + (int)Math.Round(VitalOf(action.OnSuccess, VitalType.Fatigue, recovery) * hit
                                                          + VitalOf(action.OnFailure, VitalType.Fatigue, recovery) * (1f - hit)), balance);
                stress = Clamp(stress + (int)Math.Round(VitalOf(action.OnSuccess, VitalType.Stress, recovery) * hit
                                                        + VitalOf(action.OnFailure, VitalType.Stress, recovery) * (1f - hit)), balance);
                stress = Clamp(stress - (int)Math.Round(StressRules.RestRelief(balance, action) * recovery, MidpointRounding.AwayFromZero), balance);
            }
        }

        /// <summary>판정 확률 (ActionResolver.CheckChance와 같은 보정, 피로·스트레스는 예측값).</summary>
        private static float Chance(GameState state, IDefinitionDatabase db, ActionDef action, int fatigue, int stress)
        {
            var balance = db.Balance;
            var key = action.Check.UsesProficiency ? action.Check.Proficiency.ToString() : "";
            var chance = CheckResolver.ComputeChance(state, action.Check, balance, fatigue, stress)
                         + TraitRules.Sum(state, db, TraitModifierKind.CheckBonus, key)
                         + MentorBondRules.CheckBonus(state, action, balance)
                         + PurifyRules.CheckBonus(state, balance, action);
            return Math.Min(Math.Max(chance, balance.CheckMinChance), balance.CheckMaxChance);
        }

        private static float VitalOf(GameEffect effect, VitalType vital, float recovery)
        {
            if (effect == null)
            {
                return 0f;
            }

            var total = 0f;
            foreach (var amount in effect.Vitals)
            {
                if (amount.Vital != vital)
                {
                    continue;
                }

                total += amount.Amount < 0 ? (float)Math.Round(amount.Amount * recovery, MidpointRounding.AwayFromZero) : amount.Amount;
            }

            return total;
        }

        private static string Id(IList<string> actionIds, IList<PlannedSlot> plan, int index)
        {
            if (actionIds != null)
            {
                return index < actionIds.Count ? actionIds[index] : "";
            }

            return index < plan.Count ? plan[index].ActionId : "";
        }

        private static void Finish(SlotProjection slot, int fatigue, int stress, int debt, int fatigueBefore, int stressBefore)
        {
            slot.Fatigue = fatigue;
            slot.Stress = stress;
            slot.SleepDebt = debt;
            slot.FatigueDelta = fatigue - fatigueBefore;
            slot.StressDelta = stress - stressBefore;
        }

        private static int Clamp(int value, BalanceConfig balance) => Math.Min(Math.Max(value, 0), balance.VitalMax);

        private static int AddDebt(int debt, int amount, BalanceConfig balance) => Math.Min(Math.Max(debt + amount, 0), balance.SleepDebtMax);
    }
}
