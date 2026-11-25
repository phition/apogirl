using System;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>
    /// 행동 한 번에 받는 스트레스 (사용자 결정 2026-11-25). 표준 난이도 기준으로
    /// 쉬는 일정이 아닌 행동은 기본값(행동은 경중 단계 → <see cref="ActionBase"/> 5~10, 탐험은 탐험 모델 10~15)에
    /// 피로·부상·양심의 무게·수면 부채 보정을 더해 곱한다. 한 번에 받는 상한은 없다.
    /// 난이도 배율(이야기 0.8·하드 1.2)을 곱한다. 기본값이 0 이하(쉬는 일정·회복)면 그대로 돌려준다.
    /// 휴식이 내리는 스트레스는 <see cref="RestRelief"/> (밤 휴식 값, 낮잠은 그 비율).
    /// </summary>
    public static class StressRules
    {
        /// <summary>행동 경중 단계(행동 데이터 StressCost, 1~ActionStressLevels) → 기본 스트레스. 0 이하는 그대로.</summary>
        public static int ActionBase(BalanceConfig balance, int level)
        {
            if (level <= 0 || balance == null)
            {
                return level;
            }

            var levels = Math.Max(2, balance.ActionStressLevels);
            var t = (Math.Min(level, levels) - 1) / (float)(levels - 1);
            var value = balance.ActionStressMin + (balance.ActionStressMax - balance.ActionStressMin) * t;
            return Math.Max(1, (int)Math.Round(value, MidpointRounding.AwayFromZero));
        }

        /// <summary>행동(경중 단계)의 스트레스: <see cref="ActionBase"/>에 상태 보정·난이도.</summary>
        public static int HitAction(GameState state, BalanceConfig balance, int level, int relief = 0)
        {
            return Hit(state, balance, ActionBase(balance, level), relief);
        }

        /// <summary>행동(경중 단계)의 스트레스를 앞으로의 피로·수면 부채로 (계획 흐름 예측).</summary>
        public static int HitActionAt(GameState state, BalanceConfig balance, int level, int relief, int fatigue, int sleepDebt)
        {
            return HitAt(state, balance, ActionBase(balance, level), relief, fatigue, sleepDebt);
        }

        /// <summary>휴식이 내리는 스트레스 (양수): 밤 휴식 NightRestStress, 낮잠은 그 DayRestStressRatio배, 그 밖의 행동은 0.</summary>
        public static int RestRelief(BalanceConfig balance, ActionDef action)
        {
            if (balance == null || action == null)
            {
                return 0;
            }

            if (action.Id == balance.NightRestActionId)
            {
                return Math.Max(0, balance.NightRestStress);
            }

            if (action.Id == balance.DayRestActionId)
            {
                return Math.Max(0, (int)Math.Round(balance.NightRestStress * balance.DayRestStressRatio, MidpointRounding.AwayFromZero));
            }

            return 0;
        }

        /// <summary>
        /// 기본 스트레스에 상태 보정·난이도를 적용한 값. relief는 먼저 빼는 감소(좋아하는 과목 등)로,
        /// 기본값이 양수였다면 결과는 적어도 1이다.
        /// </summary>
        public static int Hit(GameState state, BalanceConfig balance, int baseStress, int relief = 0)
        {
            if (state?.Player == null)
            {
                return HitWith(state, balance, baseStress, relief, 0f);
            }

            return HitWith(state, balance, baseStress, relief, Bonus(state, balance));
        }

        /// <summary>
        /// 앞으로의 피로·수면 부채로 계산한 값 (계획 흐름 예측 PlanForecast.ProjectWeek).
        /// 부상·양심은 지금 상태를 쓴다.
        /// </summary>
        public static int HitAt(GameState state, BalanceConfig balance, int baseStress, int relief, int fatigue, int sleepDebt)
        {
            return HitWith(state, balance, baseStress, relief, Bonus(state, balance, fatigue, sleepDebt));
        }

        private static int HitWith(GameState state, BalanceConfig balance, int baseStress, int relief, float bonus)
        {
            if (baseStress <= 0 || balance == null)
            {
                return baseStress;
            }

            var amount = Math.Max(1, baseStress - Math.Max(0, relief));
            var difficulty = DifficultyRules.Get(state, balance).Stress;
            var value = amount * (1f + bonus) * difficulty;
            var rounded = (int)Math.Round(value, MidpointRounding.AwayFromZero);
            return Math.Max(1, rounded);
        }

        /// <summary>상태 보정의 합 (0 = 보정 없음, 0.5 = ×1.5).</summary>
        public static float Bonus(GameState state, BalanceConfig balance)
        {
            if (state?.Player == null)
            {
                return 0f;
            }

            return Bonus(state, balance, state.Player.Vitals.Fatigue, state.Player.SleepDebt);
        }

        /// <summary>피로·수면 부채를 따로 준 보정 합 (부상·양심은 지금 상태).</summary>
        public static float Bonus(GameState state, BalanceConfig balance, int fatigue, int sleepDebt)
        {
            if (state?.Player == null)
            {
                return 0f;
            }

            var player = state.Player;
            var bonus = 0f;

            // 피로: 문턱마다 단계 (가장 높은 단계 하나)
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
            bonus += StatRules.StageValue(balance.SleepDebtStressMultiplier, sleepDebt) - 1f;
            return Math.Max(0f, bonus);
        }
    }
}
