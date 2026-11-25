using System;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>
    /// 행동 한 번에 받는 스트레스 (사용자 결정 2026-11-25). 표준 난이도 기준으로
    /// 쉬는 일정이 아닌 행동은 기본값 1~10(경중), 피로·부상·양심의 무게·수면 부채 보정을 더해 곱하고
    /// 한 번에 최대 <see cref="BalanceConfig.StressHitCap"/>(20). 난이도 배율(이야기 0.8·하드 1.2)은 기본값과 상한에 함께 곱한다.
    /// 기본값이 0 이하(쉬는 일정·회복)면 그대로 돌려준다.
    /// </summary>
    public static class StressRules
    {
        /// <summary>
        /// 기본 스트레스에 상태 보정·난이도를 적용한 값. relief는 먼저 빼는 감소(좋아하는 과목 등)로,
        /// 기본값이 양수였다면 결과는 적어도 1이다.
        /// </summary>
        public static int Hit(GameState state, BalanceConfig balance, int baseStress, int relief = 0)
        {
            if (baseStress <= 0 || balance == null)
            {
                return baseStress;
            }

            var amount = Math.Max(1, baseStress - Math.Max(0, relief));
            var difficulty = DifficultyRules.Get(state, balance).Stress;
            var value = amount * (1f + Bonus(state, balance)) * difficulty;
            var cap = balance.StressHitCap * difficulty;
            var rounded = (int)Math.Round(Math.Min(value, cap), MidpointRounding.AwayFromZero);
            return Math.Max(1, rounded);
        }

        /// <summary>상태 보정의 합 (0 = 보정 없음, 0.5 = ×1.5).</summary>
        public static float Bonus(GameState state, BalanceConfig balance)
        {
            if (state?.Player == null)
            {
                return 0f;
            }

            var player = state.Player;
            var bonus = 0f;

            // 피로: 문턱마다 단계 (가장 높은 단계 하나)
            var fatigue = player.Vitals.Fatigue;
            for (var i = 0; i < balance.StressFatigueThresholds.Length && i < balance.StressFatigueBonus.Length; i++)
            {
                if (fatigue >= balance.StressFatigueThresholds[i])
                {
                    bonus = Math.Max(bonus, balance.StressFatigueBonus[i]);
                }
            }

            if (InjuryRules.IsInjured(state, balance))
            {
                bonus += balance.StressInjuredBonus;
            }

            bonus += Math.Max(0, player.Conscience) * balance.StressPerConscience;
            bonus += StatRules.StageValue(balance.SleepDebtStressMultiplier, player.SleepDebt) - 1f;
            return Math.Max(0f, bonus);
        }
    }
}
