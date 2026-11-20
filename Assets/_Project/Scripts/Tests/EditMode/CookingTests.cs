using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Random;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>요리 수업: 성공하면 숙련에 맞는 요리를 만들어 먹고 허기·갈증이 줄며 식량·물을 쓴다.</summary>
    public sealed class CookingTests
    {
        private static GameState Hungry(BalanceConfig balance, int food, int water)
        {
            var state = NewGameFactory.Create(balance, 1);
            state.Player.Vitals.Satiety = -60;
            state.Player.Vitals.Hydration = -60;
            state.Base.SetResource(ResourceType.Food, food);
            state.Base.SetResource(ResourceType.Water, water);
            return state;
        }

        [Test]
        public void HigherCookingLevel_MakesBetterDish()
        {
            var balance = new BalanceConfig();
            var state = Hungry(balance, 10, 10);
            var dish = CookingRules.Cook(state, balance, 0, new StateDelta());
            Assert.AreEqual("DISH_PORRIDGE", dish.Id, "숙련 0이면 주먹밥과 죽");
            Assert.AreEqual(-40, state.Player.Vitals.Satiety, "죽은 포만 +20");
            Assert.AreEqual(-45, state.Player.Vitals.Hydration);
            Assert.AreEqual(9, state.Base.GetResource(ResourceType.Food));
            Assert.AreEqual(9, state.Base.GetResource(ResourceType.Water));

            state = Hungry(balance, 10, 10);
            state.Player.Proficiency[ProficiencyType.Cooking] = new ProficiencyState { Level = 4, Xp = balance.ProficiencyXpThresholds[3] };
            Assert.AreEqual(4, state.Player.ProficiencyLevel(ProficiencyType.Cooking));
            dish = CookingRules.Cook(state, balance, 0, new StateDelta());
            Assert.AreEqual("DISH_SET_MEAL", dish.Id);
            Assert.AreEqual(-10, state.Player.Vitals.Satiety, "정식은 포만 +50");
            var stamina = NewGameFactory.Create(balance, 1).Player.GetStat(StatType.Stamina);
            Assert.AreEqual(stamina + 1, state.Player.GetStat(StatType.Stamina), "좋은 요리는 능력치도 조금");

            // 대성공은 한 단계 위
            state = Hungry(balance, 10, 10);
            Assert.AreEqual("DISH_SOUP", CookingRules.Cook(state, balance, 1, new StateDelta()).Id);
        }

        [Test]
        public void NotEnoughIngredients_FallsBackOrSkips()
        {
            var balance = new BalanceConfig();
            var state = Hungry(balance, 1, 1);
            state.Player.Proficiency[ProficiencyType.Cooking] = new ProficiencyState { Level = 4, Xp = balance.ProficiencyXpThresholds[3] };
            Assert.AreEqual("DISH_PORRIDGE", CookingRules.Cook(state, balance, 0, new StateDelta()).Id, "재료가 모자라면 아래 단계");

            state = Hungry(balance, 0, 5);
            var delta = new StateDelta();
            Assert.IsNull(CookingRules.Cook(state, balance, 0, delta), "식량이 없으면 요리 없음");
            Assert.AreEqual(-60, state.Player.Vitals.Satiety);
            Assert.IsEmpty(delta.DishesCooked);
        }

        [Test]
        public void CookingClass_Success_CooksAndRecords()
        {
            var db = M6Act4Tests.ShippedDb();
            var cooking = db.GetAction("ACT_EDU_COOKING");
            Assert.IsEmpty(cooking.ResourceCosts, "재료는 요리가 완성될 때만 쓴다");
            var success = 0;
            for (var seed = 1; seed <= 20; seed++)
            {
                var state = Hungry(db.Balance, 10, 10);
                var delta = new StateDelta();
                var result = ActionResolver.Resolve(state, cooking, db, new SeededRandom(SeededRandom.CreateState(seed)), delta);
                if (result == CheckResult.Failure)
                {
                    Assert.IsEmpty(delta.DishesCooked);
                    Assert.AreEqual(9, state.Base.GetResource(ResourceType.Food), "실패하면 태워서 식량 1");
                    continue;
                }

                success++;
                Assert.AreEqual(1, delta.DishesCooked.Count);
                Assert.Greater(state.Player.Vitals.Satiety, -60);
                Assert.Less(state.Base.GetResource(ResourceType.Water), 10);
            }

            Assert.Greater(success, 0);
        }
    }
}
