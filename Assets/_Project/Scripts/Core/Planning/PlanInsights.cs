using System;
using System.Collections.Generic;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Planning
{
    /// <summary>행동 카드 툴팁용 미리보기 (현재 상태 기준).</summary>
    public sealed class ActionPreview
    {
        public string ActionId = "";
        public ActionCategory Category;
        public bool HasCheck;

        /// <summary>판정이 없으면 1.</summary>
        public float SuccessChance = 1f;

        public float GreatSuccessChance;
        public bool Affordable = true;
        public readonly List<ResourceAmount> Costs = new List<ResourceAmount>();
        public int FatigueCost;
        public int StressCost;

        /// <summary>성공 시 예상 성장량 (보정·체감 반영, 반올림 후).</summary>
        public readonly List<StatAmount> ExpectedGains = new List<StatAmount>();

        public bool IsExpedition;
        public ExpeditionTime ExpeditionTime;
        public string LocationId = "";
    }

    public static class ActionPreviewer
    {
        public static ActionPreview Preview(GameState state, IDefinitionDatabase db, string actionId)
        {
            var action = db.GetAction(actionId);
            var balance = db.Balance;
            var player = state.Player;
            var preview = new ActionPreview
            {
                ActionId = action.Id,
                Category = action.Category,
                HasCheck = action.HasCheck,
                FatigueCost = action.FatigueCost,
                StressCost = StatRules.ScaleStress(action.StressCost, player.SleepDebt, balance),
                IsExpedition = action.IsExpedition,
                ExpeditionTime = action.ExpeditionTime,
                LocationId = action.LocationId,
            };

            foreach (var cost in action.ResourceCosts)
            {
                preview.Costs.Add(new ResourceAmount(cost.Resource, cost.Amount));
                if (state.Base.GetResource(cost.Resource) < cost.Amount)
                {
                    preview.Affordable = false;
                }
            }

            if (action.HasCheck)
            {
                // 실제 해결 순서와 같게: 피로·스트레스 비용을 먼저 반영한 뒤 확률 계산
                var fatigue = Math.Min(balance.VitalMax, player.Vitals.Fatigue + preview.FatigueCost);
                var stress = Math.Min(balance.VitalMax, player.Vitals.Stress + preview.StressCost);
                var key = action.Check.UsesProficiency ? action.Check.Proficiency.ToString() : "";
                var chance = CheckResolver.ComputeChance(state, action.Check, balance, fatigue, stress) + TraitRules.Sum(state, db, TraitModifierKind.CheckBonus, key);
                preview.SuccessChance = Math.Min(Math.Max(chance, balance.CheckMinChance), balance.CheckMaxChance);
                preview.GreatSuccessChance = preview.SuccessChance * (balance.GreatSuccessRatio + TraitRules.Sum(state, db, TraitModifierKind.GreatSuccessBonus));
            }

            if (!preview.Affordable)
            {
                preview.SuccessChance = 0f;
                preview.GreatSuccessChance = 0f;
            }

            var multiplier = ActionResolver.GainMultiplier(
                state, action, CheckResult.Success, ActionResolver.PeekRoutineBonus(player, action, balance), balance)
                * ActionResolver.ExtraGainMultiplier(state, action, db);
            foreach (var gain in action.StatGains)
            {
                var amount = StatRules.ScaleGain(player.GetStat(gain.Stat), gain.Amount, multiplier, balance);
                preview.ExpectedGains.Add(new StatAmount(gain.Stat, amount));
            }

            return preview;
        }
    }

    /// <summary>
    /// 주간 계획의 예상 수면 부채 (플래너 하단 표시). WeekRunner와 같은 규칙을 따르되
    /// 탐험 중 무작위 결과(새벽 귀환, 쓰러짐)는 반영하지 않는다.
    /// </summary>
    public static class PlanForecast
    {
        /// <returns>각 슬롯 실행 직후의 예상 수면 부채 (길이 14).</returns>
        public static int[] ProjectSleepDebt(GameState state, IDefinitionDatabase db)
        {
            var balance = db.Balance;
            var slots = state.Plan.Slots;
            var result = new int[slots.Count];
            var debt = state.Player.SleepDebt;
            var streak = state.Player.NightRestStreak;
            var previousWasExpedition = state.Player.LastSlotWasExpedition;

            for (var i = 0; i < slots.Count; i++)
            {
                var id = string.IsNullOrEmpty(slots[i].ActionId) ? PlanPreparer.RestIdFor(i, balance) : slots[i].ActionId;
                if (!db.TryGetAction(id, out var action))
                {
                    result[i] = debt;
                    previousWasExpedition = false;
                    continue;
                }

                var consecutive = action.IsExpedition && previousWasExpedition;
                if (slots[i].ForcedRest && !action.IsRest && !consecutive)
                {
                    debt = Add(debt, balance.ForcedRestViolationSleepDebt, balance);
                }

                if (consecutive)
                {
                    debt = Add(debt, balance.ConsecutiveExpeditionSleepDebt, balance);
                }

                if (action.IsExpedition && action.ExpeditionTime == ExpeditionTime.Night)
                {
                    debt = Add(debt, balance.NightExpeditionSleepDebt, balance);
                }

                if (CalendarRules.PhaseOf(i) == DayPhase.Night)
                {
                    if (action.IsRest)
                    {
                        streak++;
                        if (streak >= balance.NightRestStreakToRepay)
                        {
                            debt = Add(debt, -balance.NightRestRepayAmount, balance);
                            streak = 0;
                        }
                    }
                    else
                    {
                        streak = 0;
                    }
                }

                previousWasExpedition = action.IsExpedition;
                result[i] = debt;
            }

            return result;
        }

        private static int Add(int debt, int amount, BalanceConfig balance)
        {
            return Math.Min(Math.Max(debt + amount, 0), balance.SleepDebtMax);
        }
    }
}
