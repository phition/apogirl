using System;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>
    /// 정수 줄 (Docs/CRAFTING_PROPOSAL.md 3.6절, 제작 P5): 모래 필터 → 끓임 정수대 → 자동 정수기.
    /// 빗물(RawWater)이 적어서 이 줄은 물을 더 만드는 것보다 정수 칸을 아껴 주는 쪽이다.
    /// </summary>
    public static class PurifyRules
    {
        /// <summary>페트병 모래 필터: 정수 실패도 더 건지고, 주간 정산 때 빗물 조금을 저절로 거른다.</summary>
        public const string FlagSandFilter = "SAND_FILTER";

        /// <summary>끓임 정수대: 연료를 쓰면 정수 한 번에 더 많이, 더 잘.</summary>
        public const string FlagBoilStation = "BOIL_STATION";

        /// <summary>자동 정수기 (전기 기기 PURIFIER의 깃발).</summary>
        public const string FlagAutoPurifier = "AUTO_PURIFIER";

        public const string PurifierApplianceId = "PURIFIER";

        public static bool IsPurify(ActionDef action, BalanceConfig balance) =>
            action != null && action.Id == balance.PurifyActionId;

        /// <summary>끓임 정수대를 이번 정수에 쓸 수 있다 (연료가 있다).</summary>
        public static bool CanBoil(GameState state, BalanceConfig balance) =>
            state.Flags.Contains(FlagBoilStation) && state.Base.GetResource(ResourceType.Fuel) >= balance.BoilStationFuel;

        /// <summary>정수 판정 보정: 끓임 정수대 (연료가 있을 때).</summary>
        public static float CheckBonus(GameState state, BalanceConfig balance, ActionDef action) =>
            IsPurify(action, balance) && CanBoil(state, balance) ? balance.BoilStationCheckBonus : 0f;

        /// <summary>
        /// 정수 행동의 효과를 설비에 맞게 고친 사본 (정수가 아니거나 설비가 없으면 그대로).
        /// 끓임 정수대를 쓰면 여기서 연료를 낸다.
        /// </summary>
        public static GameEffect Adjust(GameState state, BalanceConfig balance, ActionDef action, CheckResult result, GameEffect effect, StateDelta delta)
        {
            if (effect == null || effect.PurifyWater <= 0 || !IsPurify(action, balance))
            {
                return effect;
            }

            var filter = state.Flags.Contains(FlagSandFilter);
            var boil = CanBoil(state, balance);
            if (!filter && !boil)
            {
                return effect;
            }

            var copy = effect.Clone();
            if (filter && result == CheckResult.Failure)
            {
                copy.PurifyRate = Math.Max(copy.PurifyRate, balance.SandFilterFailRate);
            }

            if (boil)
            {
                copy.PurifyWater = Math.Max(copy.PurifyWater, balance.BoilStationPurify);
                EffectApplier.ChangeResource(state, ResourceType.Fuel, -balance.BoilStationFuel, delta);
            }

            return copy;
        }

        /// <summary>주간 정산: 모래 필터가 빗물 몇을 저절로 거른다. 거른 식수 양.</summary>
        public static int ApplyWeeklyFilter(GameState state, BalanceConfig balance, StateDelta delta)
        {
            return state.Flags.Contains(FlagSandFilter)
                ? Convert(state, balance.SandFilterWeeklyRaw, balance.SandFilterRate, delta)
                : 0;
        }

        /// <summary>자동 정수기가 거점에 있는데 전기가 없어 멈췄다.</summary>
        public static bool PurifierStopped(GameState state, BalanceConfig balance) =>
            state.Flags.Contains(FlagAutoPurifier) && !PowerRules.IsPowered(state, balance, PurifierApplianceId);

        /// <summary>아침(식사 전): 켜진 자동 정수기가 빗물을 거른다. 거른 식수 양.</summary>
        public static int ApplyMorning(GameState state, BalanceConfig balance, StateDelta delta)
        {
            return state.Flags.Contains(FlagAutoPurifier) && PowerRules.IsPowered(state, balance, PurifierApplianceId)
                ? Convert(state, balance.AutoPurifyDaily, balance.AutoPurifyRate, delta)
                : 0;
        }

        /// <summary>빗물을 최대 limit만큼 식수로 (비율만큼 남고, 반올림).</summary>
        private static int Convert(GameState state, int limit, float rate, StateDelta delta)
        {
            var raw = Math.Min(limit, state.Base.GetResource(ResourceType.RawWater));
            if (raw <= 0)
            {
                return 0;
            }

            var water = (int)Math.Round(raw * rate, MidpointRounding.AwayFromZero);
            EffectApplier.ChangeResource(state, ResourceType.RawWater, -raw, delta);
            EffectApplier.ChangeResource(state, ResourceType.Water, water, delta);
            return water;
        }
    }
}
