using System;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>
    /// 부상: 한 사건에서 최대 체력의 InjuryHpFraction 이상을 잃으면 부상 상태(플래그)가 된다.
    /// 부상 중에는 체력이 회복되지 않고(EffectApplier), 의약품 치료 행동으로만 풀린다.
    /// 다칠 때 상처 감염이 오르고, 청결이 낮을수록 더 오른다.
    /// </summary>
    public static class InjuryRules
    {
        public static bool IsInjured(GameState state, BalanceConfig balance) =>
            balance != null && !string.IsNullOrEmpty(balance.InjuredFlag) && state.Flags.Contains(balance.InjuredFlag);

        /// <summary>더러움 0~1 (청결 최대면 0).</summary>
        public static float Dirt(GameState state, BalanceConfig balance)
        {
            var max = Math.Max(1, balance.VitalMax);
            return 1f - Math.Min(Math.Max(state.Player.Vitals.Cleanliness, 0), max) / (float)max;
        }

        /// <summary>부상 때 오르는 상처 감염 (난이도 감염 배율 반영 전).</summary>
        public static int WoundInfection(GameState state, BalanceConfig balance) =>
            balance.WoundInfectionBase + (int)Math.Round(balance.WoundInfectionDirty * Dirt(state, balance), MidpointRounding.AwayFromZero);

        /// <summary>사건 전 체력과 지금 체력을 비교해 부상을 판정한다. 새로 다쳤으면 상처 감염량(1 이상), 아니면 0.</summary>
        public static int CheckIncident(GameState state, int hpBefore, BalanceConfig balance, StateDelta delta)
        {
            var hp = state.Player.Vitals.Hp;
            var lost = hpBefore - hp;
            if (lost <= 0 || hp <= 0 || lost < balance.InjuryHpFraction * balance.VitalMax)
            {
                return 0;
            }

            var gain = DifficultyRules.Scale(WoundInfection(state, balance), DifficultyRules.Get(state, balance).Infection);
            EffectApplier.ChangeVital(state, VitalType.Infection, gain, balance, delta);
            if (!string.IsNullOrEmpty(balance.InjuredFlag) && state.Flags.Add(balance.InjuredFlag))
            {
                delta.FlagsSet.Add(balance.InjuredFlag);
            }

            return Math.Max(1, gain);
        }
    }
}
