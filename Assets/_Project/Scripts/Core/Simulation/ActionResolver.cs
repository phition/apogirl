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
                result = CheckResolver.Roll(CheckResolver.ComputeChance(state, action.Check, balance), random, balance);
            }
            else
            {
                result = CheckResult.Success;
            }

            var routineBonus = UpdateRoutine(player, action, balance);
            if (affordable)
            {
                var multiplier = ResultMultiplier(result, balance)
                                 * TeachingMultiplier(state, action, balance)
                                 * (1f + routineBonus)
                                 * (1f - StatRules.StageValue(balance.SleepDebtEfficiencyPenalty, player.SleepDebt));
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

            EffectApplier.Apply(state, EffectFor(action, result), balance, delta, recovery);
            return result;
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

            var isJ = (player.Profile.PersonalityType ?? "").IndexOf('J') >= 0;
            var max = isJ ? balance.RoutineBonusMaxForJ : balance.RoutineBonusMax;
            return Math.Min(max, player.RoutineStreak * balance.RoutineBonusPerStep);
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

            switch (action.Teaching)
            {
                case TeachingMode.TeacherDirect: return balance.TeacherDirectMultiplier;
                case TeachingMode.SelfStudyGuided: return balance.TeacherGuidanceMultiplier;
                default: return 1f;
            }
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
