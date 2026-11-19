using System;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Random;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    public enum CheckResult
    {
        Failure,
        Success,
        GreatSuccess,
    }

    /// <summary>성공 판정.</summary>
    public static class CheckResolver
    {
        /// <summary>
        /// 확률 = 기본 + Σ(스탯×가중치) + 숙련도×가중치 − 스트레스·피로·수면 부채 페널티, [최소, 최대]로 제한.
        /// </summary>
        public static float ComputeChance(GameState state, CheckDef check, BalanceConfig balance)
        {
            return ComputeChance(state, check, balance, state.Player.Vitals.Fatigue, state.Player.Vitals.Stress);
        }

        /// <summary>피로·스트레스 값을 지정해 계산 (행동 비용을 먼저 반영한 미리보기용).</summary>
        public static float ComputeChance(GameState state, CheckDef check, BalanceConfig balance, int fatigue, int stress)
        {
            var player = state.Player;
            double chance = check.BaseRate;
            foreach (var weight in check.StatWeights)
            {
                chance += player.GetStat(weight.Stat) * weight.Weight;
            }

            if (check.UsesProficiency)
            {
                chance += player.ProficiencyLevel(check.Proficiency) * check.ProficiencyWeight;
            }

            chance -= stress * balance.StressCheckPenaltyPerPoint;
            chance -= fatigue * balance.FatigueCheckPenaltyPerPoint;
            chance -= StatRules.StageValue(balance.SleepDebtCheckPenalty, player.SleepDebt);

            return (float)Math.Min(Math.Max(chance, balance.CheckMinChance), balance.CheckMaxChance);
        }

        /// <summary>굴림값이 확률×대성공비율 미만이면 대성공, 확률 미만이면 성공.</summary>
        public static CheckResult Roll(float chance, IRandomService random, BalanceConfig balance, float greatBonus = 0f)
        {
            var roll = random.NextDouble();
            if (roll < chance * (balance.GreatSuccessRatio + greatBonus))
            {
                return CheckResult.GreatSuccess;
            }

            return roll < chance ? CheckResult.Success : CheckResult.Failure;
        }
    }
}
