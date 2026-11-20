using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>포만·수분: 0이 가운데. 배부르면 먹지 않아 아끼고 버프, 허기지면 디버프 (사용자 요청).</summary>
    public sealed class NourishmentTests
    {
        private static GameState Make(BalanceConfig balance, int satiety, int food, int water = 50)
        {
            var state = NewGameFactory.Create(balance, 1);
            state.Calendar.Day = 1;
            state.Player.Vitals.Satiety = satiety;
            state.Player.Vitals.Hp = 50;
            state.Player.Vitals.Stress = 50;
            state.Base.SetResource(ResourceType.Food, food);
            state.Base.SetResource(ResourceType.Water, water);
            return state;
        }

        [Test]
        public void Full_SkipsMeal_SavesFood_AndBuffs()
        {
            var balance = new BalanceConfig();
            var state = Make(balance, 60, 10);
            UpkeepRules.ApplyDaily(state, balance, new StateDelta());
            Assert.AreEqual(35, state.Player.Vitals.Satiety, "하루치만 빠진다");
            Assert.AreEqual(10, state.Base.GetResource(ResourceType.Food), "배부르면 먹지 않아 식량을 아낀다");
            Assert.AreEqual("FULL", UpkeepRules.Band(balance.SatietyBands, 35).Key);
            Assert.Greater(state.Player.Vitals.Hp, 50, "배부르면 체력이 조금 오른다");
            Assert.Less(state.Player.Vitals.Stress, 50);
        }

        [Test]
        public void Normal_EatsToZero_AndHungry_RecoversWithFood()
        {
            var balance = new BalanceConfig();
            var state = Make(balance, 0, 10);
            UpkeepRules.ApplyDaily(state, balance, new StateDelta());
            Assert.AreEqual(0, state.Player.Vitals.Satiety, "보통이면 하루치를 먹어 0으로");
            Assert.AreEqual(8, state.Base.GetResource(ResourceType.Food));

            state = Make(balance, -60, 10);
            UpkeepRules.ApplyDaily(state, balance, new StateDelta());
            Assert.AreEqual(-60 - balance.SatietyDailyDrain + balance.MealSatiety, state.Player.Vitals.Satiety, "굶었어도 먹으면 매일 회복된다");
        }

        [Test]
        public void Hungry_Debuffs_StarvingHurts()
        {
            var balance = new BalanceConfig();
            var hungry = Make(balance, -10, 0);
            UpkeepRules.ApplyDaily(hungry, balance, new StateDelta());
            Assert.AreEqual(-35, hungry.Player.Vitals.Satiety);
            Assert.AreEqual(50, hungry.Player.Vitals.Hp, "허기짐은 체력 피해 없음");
            Assert.Greater(hungry.Player.Vitals.Stress, 50, "허기지면 스트레스");

            var starving = Make(balance, -50, 0);
            UpkeepRules.ApplyDaily(starving, balance, new StateDelta());
            Assert.AreEqual(50 - balance.StarvationHpLoss, starving.Player.Vitals.Hp, "굶주림은 체력 피해");
        }

        [Test]
        public void GoodMealBuffer_OutlastsShortage()
        {
            // 정식으로 +100 가까이 채워 두면 식량 없이도 며칠은 굶주림까지 가지 않는다
            var balance = new BalanceConfig();
            var fed = Make(balance, 90, 0);
            var plain = Make(balance, 0, 0);
            for (var day = 1; day <= 5; day++)
            {
                fed.Calendar.Day = plain.Calendar.Day = day;
                UpkeepRules.ApplyDaily(fed, balance, new StateDelta());
                UpkeepRules.ApplyDaily(plain, balance, new StateDelta());
            }

            Assert.Greater(fed.Player.Vitals.Satiety, balance.StarvationThreshold);
            Assert.LessOrEqual(plain.Player.Vitals.Satiety, balance.StarvationThreshold);
            Assert.Greater(fed.Player.Vitals.Hp, plain.Player.Vitals.Hp);
        }

        [Test]
        public void Gauge_IsCentered_AndClamped()
        {
            var balance = new BalanceConfig();
            var state = Make(balance, 0, 0);
            EffectApplier.ChangeVital(state, VitalType.Satiety, -500, balance, new StateDelta());
            Assert.AreEqual(-balance.VitalMax, state.Player.Vitals.Satiety);
            EffectApplier.ChangeVital(state, VitalType.Satiety, 500, balance, new StateDelta());
            Assert.AreEqual(balance.VitalMax, state.Player.Vitals.Satiety);
        }
    }
}
