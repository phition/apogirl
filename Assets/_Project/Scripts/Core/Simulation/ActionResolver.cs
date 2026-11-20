using System;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Random;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Simulation
{
    /// <summary>탐험이 아닌 행동 하나를 해결한다: 비용 → 판정 → 성장·숙련도 → 결과 효과.</summary>
    public static class ActionResolver
    {
        public static CheckResult Resolve(GameState state, ActionDef action, IDefinitionDatabase db, IRandomService random, StateDelta delta)
        {
            var balance = db.Balance;
            var player = state.Player;

            var affordable = true;
            foreach (var cost in action.ResourceCosts)
            {
                if (state.Base.GetResource(cost.Resource) < cost.Amount)
                {
                    affordable = false;
                    break;
                }
            }

            if (affordable)
            {
                foreach (var cost in action.ResourceCosts)
                {
                    EffectApplier.ChangeResource(state, cost.Resource, -cost.Amount, delta);
                }
            }

            EffectApplier.ChangeVital(state, VitalType.Fatigue, action.FatigueCost, balance, delta);
            EffectApplier.ChangeVital(state, VitalType.Stress, StatRules.ScaleStress(action.StressCost, player.SleepDebt, balance), balance, delta);

            CheckResult result;
            if (!affordable)
            {
                result = CheckResult.Failure;
            }
            else if (action.HasCheck)
            {
                result = CheckResolver.Roll(CheckChance(state, action, db), random, balance, TraitRules.Sum(state, db, TraitModifierKind.GreatSuccessBonus));
            }
            else
            {
                result = CheckResult.Success;
            }

            var routineBonus = UpdateRoutine(player, action, balance);
            if (affordable)
            {
                var multiplier = GainMultiplier(state, action, result, routineBonus, balance) * ExtraGainMultiplier(state, action, db);
                foreach (var gain in action.StatGains)
                {
                    var current = player.GetStat(gain.Stat);
                    EffectApplier.ChangeStat(state, gain.Stat, StatRules.ScaleGain(current, gain.Amount, multiplier, balance), balance, delta);
                }

                if (action.HasProficiency && result != CheckResult.Failure)
                {
                    var xp = action.ProficiencyXp + (result == CheckResult.GreatSuccess ? balance.GreatSuccessBonusXp : 0);
                    AddProficiencyXp(state, action.Proficiency, xp, balance, delta);
                }
            }

            var recovery = 1f;
            if (action.IsRest && player.ReducedNextRest)
            {
                recovery = balance.ReducedRestMultiplier;
                player.ReducedNextRest = false;
            }

            if (action.IsRest)
            {
                recovery *= 1f + TraitRules.Sum(state, db, TraitModifierKind.RestRecovery);

                // 부상 중 밤 휴식: 의약품이 있으면 반드시 써서 치유한다 (그 뒤 휴식 회복은 그대로)
                if (action.Id == balance.NightRestActionId)
                {
                    InjuryRules.TreatWithMedicine(state, balance, delta);
                }
            }

            // 비용을 못 냈으면 아무 효과도 없다 (빗물 없이 정수해도 실패 보상을 받지 않게)
            EffectApplier.Apply(state, affordable ? EffectFor(action, result) : null, balance, delta, recovery);
            return result;
        }

        /// <summary>판정 확률: 기본 공식 + 특성 보정 (M4).</summary>
        public static float CheckChance(GameState state, ActionDef action, IDefinitionDatabase db)
        {
            var balance = db.Balance;
            var chance = CheckResolver.ComputeChance(state, action.Check, balance);
            var key = action.Check.UsesProficiency ? action.Check.Proficiency.ToString() : "";
            chance += TraitRules.Sum(state, db, TraitModifierKind.CheckBonus, key);
            return Math.Min(Math.Max(chance, balance.CheckMinChance), balance.CheckMaxChance);
        }

        /// <summary>특성(교육 성장)·시설 보정 배율 (M4).</summary>
        public static float ExtraGainMultiplier(GameState state, ActionDef action, IDefinitionDatabase db)
        {
            var multiplier = 1f;
            if (action.Category == ActionCategory.Education)
            {
                multiplier += TraitRules.Sum(state, db, TraitModifierKind.EducationGain);
            }

            if (!string.IsNullOrEmpty(action.BonusFacilityId) && state.Base.Facilities.TryGetValue(action.BonusFacilityId, out var level))
            {
                multiplier += level * action.BonusPerLevel;
            }

            return multiplier;
        }

        public static void AddProficiencyXp(GameState state, ProficiencyType type, int xp, BalanceConfig balance, StateDelta delta)
        {
            if (xp <= 0)
            {
                return;
            }

            var proficiency = state.Player.GetProficiency(type);
            var before = proficiency.Level;
            proficiency.Xp += xp;
            proficiency.Level = StatRules.LevelForXp(proficiency.Xp, balance);
            if (proficiency.Level > before)
            {
                delta.ProficiencyLevelUps.Add(type);
            }
        }

        /// <summary>성장량 배율 = 결과 × 선생님 지도 × (1 + 연속 행동) × (1 − 수면 부채 효율 페널티).</summary>
        public static float GainMultiplier(GameState state, ActionDef action, CheckResult result, float routineBonus, BalanceConfig balance)
        {
            return ResultMultiplier(result, balance)
                   * TeachingMultiplier(state, action, balance)
                   * (1f + routineBonus)
                   * (1f - StatRules.StageValue(balance.SleepDebtEfficiencyPenalty, state.Player.SleepDebt));
        }

        /// <summary>지금 이 행동을 하면 받을 연속 행동 보너스 (상태를 바꾸지 않음).</summary>
        public static float PeekRoutineBonus(PlayerState player, ActionDef action, BalanceConfig balance)
        {
            if (action.Category != ActionCategory.Education)
            {
                return 0f;
            }

            var streak = player.RoutineActionId == action.Id ? player.RoutineStreak + 1 : 0;
            return RoutineBonus(player, streak, balance);
        }

        /// <summary>교육이 아닌 낮 행동을 하면 연속 행동 기록이 끊긴다.</summary>
        public static void ResetRoutine(PlayerState player)
        {
            player.RoutineActionId = "";
            player.RoutineStreak = 0;
        }

        /// <summary>같은 교육을 연속하면 회당 보너스 (문서 4.3, 성격 유형 J는 상한 증가).</summary>
        private static float UpdateRoutine(PlayerState player, ActionDef action, BalanceConfig balance)
        {
            if (action.Category != ActionCategory.Education)
            {
                return 0f;
            }

            if (player.RoutineActionId == action.Id)
            {
                player.RoutineStreak++;
            }
            else
            {
                player.RoutineActionId = action.Id;
                player.RoutineStreak = 0;
            }

            return RoutineBonus(player, player.RoutineStreak, balance);
        }

        private static float RoutineBonus(PlayerState player, int streak, BalanceConfig balance)
        {
            var isJ = (player.Profile.PersonalityType ?? "").IndexOf('J') >= 0;
            var max = isJ ? balance.RoutineBonusMaxForJ : balance.RoutineBonusMax;
            return Math.Min(max, streak * balance.RoutineBonusPerStep);
        }

        private static float ResultMultiplier(CheckResult result, BalanceConfig balance)
        {
            switch (result)
            {
                case CheckResult.GreatSuccess: return balance.GreatSuccessGainMultiplier;
                case CheckResult.Failure: return balance.FailureGainMultiplier;
                default: return 1f;
            }
        }

        private static float TeachingMultiplier(GameState state, ActionDef action, BalanceConfig balance)
        {
            if (!state.Teacher.Alive)
            {
                return 1f;
            }

            float multiplier;
            switch (action.Teaching)
            {
                case TeachingMode.TeacherDirect: multiplier = balance.TeacherDirectMultiplier; break;
                case TeachingMode.SelfStudyGuided: multiplier = balance.TeacherGuidanceMultiplier; break;
                default: return 1f;
            }

            // M6: 재감염 병세 단계마다 멘토 기능이 약해진다 (문서 5.3.1)
            var decay = Math.Min(1f, InfectionRules.TeacherStage(state, balance) * balance.MentorDecayPerStage);
            return 1f + (multiplier - 1f) * (1f - decay);
        }

        private static GameEffect EffectFor(ActionDef action, CheckResult result)
        {
            switch (result)
            {
                case CheckResult.GreatSuccess: return action.OnGreatSuccess;
                case CheckResult.Failure: return action.OnFailure;
                default: return action.OnSuccess;
            }
        }
    }
}
