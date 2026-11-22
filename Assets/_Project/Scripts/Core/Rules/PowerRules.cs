using System;
using System.Collections.Generic;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>
    /// 전력 = 유지력 (사용자 결정 2026-11-22): 전력(ResourceType.Power)은 쓰면 없어지는 자원이 아니라 발전 용량이다.
    /// 방송실 자전거 발전기를 복구하면 용량이 생기고, 전기 기기(냉장고 등)가 늘 일부를 쓰며, 남는 여유 전력이 있어야 쓰는 행동(방송·무전)이 있다.
    /// 배터리를 모아 용량을 올리고(공학 숙련이 높을수록 덜 든다), 발전기가 고장 나면 수리 행동으로 고쳐야 다시 돈다.
    /// </summary>
    public static class PowerRules
    {
        /// <summary>발전기 고장 (수리 행동이 지운다). 고장 중에는 정전.</summary>
        public const string FlagBroken = "GENERATOR_BROKEN";

        /// <summary>발전 용량: 복구 전이면 0.</summary>
        public static int Capacity(GameState state) =>
            state.Base.PowerRestored ? Math.Max(0, state.Base.GetResource(ResourceType.Power)) : 0;

        /// <summary>발전기가 돌고 있다 (복구됐고, 용량이 있고, 고장 나지 않았다).</summary>
        public static bool Running(GameState state) => Capacity(state) > 0 && !state.Flags.Contains(FlagBroken);

        /// <summary>정전: 발전기가 돌지 않는다.</summary>
        public static bool IsBlackout(this GameState state) => !Running(state);

        /// <summary>거점에 놓인 기기 (켜졌는지와 상관없이).</summary>
        public static IEnumerable<ApplianceDef> Installed(GameState state, BalanceConfig balance)
        {
            if (balance?.Appliances == null)
            {
                yield break;
            }

            foreach (var appliance in balance.Appliances)
            {
                if (string.IsNullOrEmpty(appliance.Flag) || state.Flags.Contains(appliance.Flag))
                {
                    yield return appliance;
                }
            }
        }

        /// <summary>지금 켜진 기기: 표 순서대로, 용량 안에서.</summary>
        public static List<ApplianceDef> Powered(GameState state, BalanceConfig balance)
        {
            var list = new List<ApplianceDef>();
            if (!Running(state))
            {
                return list;
            }

            var left = Capacity(state);
            foreach (var appliance in Installed(state, balance))
            {
                if (appliance.Power <= left)
                {
                    left -= appliance.Power;
                    list.Add(appliance);
                }
            }

            return list;
        }

        public static int Load(GameState state, BalanceConfig balance)
        {
            var sum = 0;
            foreach (var appliance in Powered(state, balance))
            {
                sum += appliance.Power;
            }

            return sum;
        }

        /// <summary>여유 전력 = 용량 − 켜진 기기. 정전이면 0.</summary>
        public static int Free(GameState state, BalanceConfig balance) => Running(state) ? Capacity(state) - Load(state, balance) : 0;

        public static bool IsPowered(GameState state, BalanceConfig balance, string applianceId) =>
            Powered(state, balance).Exists(a => a.Id == applianceId);

        /// <summary>켜진 기기 덕에 식량 부패 문턱이 오르는 양.</summary>
        public static int FoodSpoilageBonus(GameState state, BalanceConfig balance)
        {
            var sum = 0;
            foreach (var appliance in Powered(state, balance))
            {
                sum += appliance.FoodSpoilageBonus;
            }

            return sum;
        }

        /// <summary>용량을 1 올리는 데 드는 배터리: 기본에서 공학 숙련 단계마다 줄고, 최소치 아래로는 내려가지 않는다.</summary>
        public static int UpgradeCost(GameState state, BalanceConfig balance)
        {
            var level = state.Player.ProficiencyLevel(ProficiencyType.Engineering);
            return Math.Max(balance.PowerUpgradeMinBatteries, balance.PowerUpgradeBatteries - level * balance.PowerUpgradeBatteriesPerLevel);
        }

        /// <summary>용량을 올릴 수 있다: 발전기가 복구돼 고장 나지 않았고, 상한 아래이고, 배터리가 충분하다.</summary>
        public static bool CanUpgrade(GameState state, BalanceConfig balance) =>
            state.Base.PowerRestored && !state.Flags.Contains(FlagBroken) && Capacity(state) < balance.PowerMaxCapacity
            && state.Base.GetResource(ResourceType.Battery) >= UpgradeCost(state, balance);

        /// <summary>행동에 필요한 전력 조건 (계획·실행 모두): 여유 전력, 용량 올리기라면 배터리.</summary>
        public static bool Allows(GameState state, BalanceConfig balance, ActionDef action)
        {
            if (action == null)
            {
                return true;
            }

            if (action.MinFreePower > 0 && Free(state, balance) < action.MinFreePower)
            {
                return false;
            }

            return !action.UpgradesPower || CanUpgrade(state, balance);
        }

        /// <summary>용량 올리기: 배터리를 쓰고 용량 +PowerUpgradeAmount (상한까지).</summary>
        public static void Upgrade(GameState state, BalanceConfig balance, StateDelta delta)
        {
            if (!CanUpgrade(state, balance))
            {
                return;
            }

            EffectApplier.ChangeResource(state, ResourceType.Battery, -UpgradeCost(state, balance), delta);
            var gain = Math.Min(balance.PowerUpgradeAmount, balance.PowerMaxCapacity - Capacity(state));
            EffectApplier.ChangeResource(state, ResourceType.Power, gain, delta);
        }
    }
}
