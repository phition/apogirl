using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>전력 = 유지력: 발전 용량·기기·여유 전력, 고장, 배터리로 용량 올리기, 냉장고.</summary>
    public sealed class PowerTests
    {
        private static GameState Powered(int capacity)
        {
            var state = new GameState();
            state.Base.PowerRestored = true;
            state.Base.Resources[ResourceType.Power] = capacity;
            return state;
        }

        [Test]
        public void Free_IsCapacityMinusPoweredAppliances_BrokenMeansBlackout()
        {
            var balance = new BalanceConfig();
            var state = Powered(3);
            Assert.AreEqual(3, PowerRules.Free(state, balance), "기기가 없으면 용량 그대로");

            state.Flags.Add("SEARCHED_HOMEEC");   // 가사실 냉장고
            Assert.IsTrue(PowerRules.IsPowered(state, balance, "FRIDGE"));
            Assert.AreEqual(2, PowerRules.Free(state, balance), "발전기 3 − 냉장고 1 = 방송할 수 있는 2");

            state.Flags.Add(PowerRules.FlagBroken);
            Assert.IsTrue(state.IsBlackout());
            Assert.AreEqual(0, PowerRules.Free(state, balance));
            Assert.IsFalse(PowerRules.IsPowered(state, balance, "FRIDGE"), "고장이면 냉장고도 꺼진다");

            var dark = new GameState();
            dark.Base.Resources[ResourceType.Power] = 5;
            Assert.AreEqual(0, PowerRules.Capacity(dark), "복구 전에는 용량 0");
        }

        [Test]
        public void MinFreePower_GatesActionsWithoutSpendingPower()
        {
            var balance = new BalanceConfig();
            var state = Powered(2);
            state.Flags.Add("SEARCHED_HOMEEC");
            var broadcast = new ActionDef { Id = "ACT_X", MinFreePower = 2 };
            Assert.IsFalse(PowerRules.Allows(state, balance, broadcast), "여유 1");

            state.Base.Resources[ResourceType.Power] = 3;
            Assert.IsTrue(PowerRules.Allows(state, balance, broadcast));
            Assert.AreEqual(3, PowerRules.Capacity(state), "쓰는 게 아니라 유지력");
        }

        [Test]
        public void Upgrade_CostDropsWithEngineering_SpendsBatteries_UpToMax()
        {
            var balance = new BalanceConfig();
            var state = Powered(3);
            Assert.AreEqual(balance.PowerUpgradeBatteries, PowerRules.UpgradeCost(state, balance), "숙련 0이면 기본 5개");

            state.Player.Proficiency[ProficiencyType.Engineering] = new ProficiencyState { Level = 2 };
            Assert.AreEqual(balance.PowerUpgradeBatteries - 2 * balance.PowerUpgradeBatteriesPerLevel, PowerRules.UpgradeCost(state, balance));
            state.Player.Proficiency[ProficiencyType.Engineering].Level = 5;
            Assert.AreEqual(balance.PowerUpgradeMinBatteries, PowerRules.UpgradeCost(state, balance), "최소치 아래로는 안 내려간다");

            state.Player.Proficiency[ProficiencyType.Engineering].Level = 2;
            state.Base.Resources[ResourceType.Battery] = 2;
            Assert.IsFalse(PowerRules.CanUpgrade(state, balance), "배터리 부족");

            state.Base.Resources[ResourceType.Battery] = 4;
            PowerRules.Upgrade(state, balance, new StateDelta());
            Assert.AreEqual(4, PowerRules.Capacity(state));
            Assert.AreEqual(1, state.Base.GetResource(ResourceType.Battery));

            state.Base.Resources[ResourceType.Power] = balance.PowerMaxCapacity;
            state.Base.Resources[ResourceType.Battery] = 99;
            Assert.IsFalse(PowerRules.CanUpgrade(state, balance), "상한");

            state.Base.Resources[ResourceType.Power] = 3;
            state.Flags.Add(PowerRules.FlagBroken);
            Assert.IsFalse(PowerRules.CanUpgrade(state, balance), "고장 중에는 먼저 고쳐야 한다");
        }

        [Test]
        public void Fridge_RaisesSpoilageThreshold()
        {
            var balance = new BalanceConfig();
            var state = Powered(3);
            state.Flags.Add("SEARCHED_HOMEEC");
            var bonus = PowerRules.FoodSpoilageBonus(state, balance);
            Assert.AreEqual(30, bonus);
            Assert.Greater(UpkeepRules.SpoiledFood(80, balance), 0);
            Assert.AreEqual(0, UpkeepRules.SpoiledFood(80, balance, bonus), "냉장고가 있으면 90까지 안 상한다");
        }
    }
}
