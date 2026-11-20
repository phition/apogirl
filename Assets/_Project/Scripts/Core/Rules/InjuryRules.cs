using System;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>
    /// 부상: 한 사건에서 최대 체력의 InjuryHpFraction(+응급처치·의학 Lv × 5%) 이상을 잃으면 부상 상태(플래그)가 된다.
    /// 부상 중에는 체력이 회복되지 않는다(EffectApplier). 풀리는 길: 부상 치료(의약품), 응급처치 수업 성공(약 없이),
    /// 휴식(의약품이 있으면 반드시 써서).
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

        /// <summary>부상 때 오르는 상처 감염 (난이도 감염 배율 반영 전). 청결이 기준 이상이면 기본값만.</summary>
        public static int WoundInfection(GameState state, BalanceConfig balance)
        {
            var threshold = Math.Max(1, balance.WoundCleanThreshold);
            var lack = Math.Max(0, threshold - state.Player.Vitals.Cleanliness) / (float)threshold;
            return balance.WoundInfectionBase + (int)Math.Round(balance.WoundInfectionDirty * lack, MidpointRounding.AwayFromZero);
        }

        /// <summary>부상이 되는 체력 손실 (숙련 없을 때: 최대 체력 × 비율, 올림. 0.3 × 100 = 30).</summary>
        public static int Threshold(BalanceConfig balance) => Threshold(balance, 0);

        /// <summary>응급처치·의학 숙련이 높을수록 더 크게 다쳐야 부상 (Lv마다 +5%).</summary>
        public static int Threshold(BalanceConfig balance, int medicineLevel)
        {
            var fraction = balance.InjuryHpFraction + Math.Max(0, medicineLevel) * balance.InjuryHpFractionPerMedicineLevel;
            return Math.Max(1, (int)Math.Ceiling(fraction * balance.VitalMax - 1e-3));
        }

        public static int Threshold(GameState state, BalanceConfig balance) =>
            Threshold(balance, state.Player.ProficiencyLevel(ProficiencyType.Medicine));

        /// <summary>부상이 아닌데 응급 치료로 마무리한 것으로 서술할 만큼 다쳤는가 (응급처치 숙련 1 이상).</summary>
        public static bool PatchedUp(GameState state, int hpBefore, BalanceConfig balance)
        {
            var lost = hpBefore - state.Player.Vitals.Hp;
            return state.Player.Vitals.Hp > 0 && lost >= balance.FirstAidNarrativeMinLoss && lost < Threshold(state, balance)
                   && state.Player.ProficiencyLevel(ProficiencyType.Medicine) >= 1;
        }

        /// <summary>부상 중 휴식: 의약품이 있으면 반드시 써서 치유한다. 치유했으면 true.</summary>
        public static bool TreatOnRest(GameState state, BalanceConfig balance, StateDelta delta)
        {
            if (!IsInjured(state, balance) || balance.RestInjuryMedicine <= 0 || state.Base.GetResource(ResourceType.Medicine) < balance.RestInjuryMedicine)
            {
                return false;
            }

            EffectApplier.ChangeResource(state, ResourceType.Medicine, -balance.RestInjuryMedicine, delta);
            if (state.Flags.Remove(balance.InjuredFlag))
            {
                delta.FlagsCleared.Add(balance.InjuredFlag);
            }

            return true;
        }

        /// <summary>사건 전 체력과 지금 체력을 비교해 부상을 판정한다. 새로 다쳤으면 상처 감염량(1 이상), 아니면 0.</summary>
        public static int CheckIncident(GameState state, int hpBefore, BalanceConfig balance, StateDelta delta)
        {
            var hp = state.Player.Vitals.Hp;
            var lost = hpBefore - hp;
            if (lost <= 0 || hp <= 0 || lost < Threshold(state, balance))
            {
                return 0;
            }

            var gain = DifficultyRules.Scale(WoundInfection(state, balance), DifficultyRules.Get(state, balance).Infection);
            if (gain > 0)
            {
                EffectApplier.ChangeVital(state, VitalType.Infection, gain, balance, delta);
            }

            if (!string.IsNullOrEmpty(balance.InjuredFlag) && state.Flags.Add(balance.InjuredFlag))
            {
                delta.FlagsSet.Add(balance.InjuredFlag);
            }

            // 반환값은 '새로 다쳤다'는 표시로도 쓰므로 최소 1
            return Math.Max(1, gain);
        }
    }
}
