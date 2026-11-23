using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>
    /// 포만·수분 (사용자 결정 2026-11-23): 주간 식단(한 사람당 1·2·3개), 아침 식사, 밤이 되기 전 감소,
    /// 못 지킨 날 스트레스, 허기·갈증만큼 최대 체력·스태미너 감소. 0이 가운데, 버프는 단계표 그대로.
    /// </summary>
    public sealed class NourishmentTests
    {
        private const string Npc = "NPC_DOHYEON";

        private static GameState Make(BalanceConfig balance, int satiety, int food, int water = 50, bool companion = false)
        {
            var state = NewGameFactory.Create(balance, 1);
            state.Calendar.Day = 1;
            state.Player.Vitals.Satiety = satiety;
            state.Player.Vitals.Hp = 50;
            state.Player.Vitals.Stress = 50;
            state.Base.SetResource(ResourceType.Food, food);
            state.Base.SetResource(ResourceType.Water, water);
            if (companion)
            {
                state.Npcs[Npc] = new NpcState { Status = NpcStatus.Joined };
            }

            return state;
        }

        [Test]
        public void Morning_EatsDietPerPerson_AndFills()
        {
            var balance = new BalanceConfig();
            var state = Make(balance, 0, 20, 20, companion: true);
            Assert.AreEqual(2, UpkeepRules.Headcount(state, balance), "주인공 + 동료");

            Assert.IsFalse(UpkeepRules.ApplyMorning(state, balance, new StateDelta(), new FixedRandom()));
            Assert.AreEqual(20 - 2 * 2, state.Base.GetResource(ResourceType.Food), "챙겨 먹는다 = 한 사람 2개");
            Assert.AreEqual(20 - 2 * 2, state.Base.GetResource(ResourceType.Water));
            Assert.AreEqual(2 * balance.FoodSatietyPerUnit, state.Player.Vitals.Satiety);
            Assert.AreEqual(2 * balance.WaterHydrationPerUnit, state.Player.Vitals.Hydration);
            Assert.AreEqual(2 * balance.FoodSatietyPerUnit, state.Npcs[Npc].Body.Satiety, "동료도 같은 식사");

            state.FoodDiet = DietLevel.Well;
            state.WaterDiet = DietLevel.Save;
            UpkeepRules.ApplyMorning(state, balance, new StateDelta(), new FixedRandom());
            Assert.AreEqual(16 - 3 * 2, state.Base.GetResource(ResourceType.Food), "잘 먹는다 = 3개");
            Assert.AreEqual(16 - 1 * 2, state.Base.GetResource(ResourceType.Water), "아껴 먹는다 = 1개");
        }

        [Test]
        public void Evening_Drains_AndBandsApply()
        {
            var balance = new BalanceConfig();
            var full = Make(balance, 60, 0);
            full.Player.Vitals.Hydration = 60;
            UpkeepRules.ApplyEvening(full, balance, new StateDelta());
            Assert.AreEqual(60 - balance.SatietyDailyDrain, full.Player.Vitals.Satiety);
            Assert.AreEqual(60 - balance.HydrationDailyDrain, full.Player.Vitals.Hydration);
            Assert.Greater(full.Player.Vitals.Hp, 50, "배부르면 체력이 조금 오른다 (버프는 그대로)");
            Assert.Less(full.Player.Vitals.Stress, 50);

            var hungry = Make(balance, -29, 0); // 보통의 맨 아래: 하루 빠지면 허기
            UpkeepRules.ApplyEvening(hungry, balance, new StateDelta());
            Assert.AreEqual("HUNGRY", UpkeepRules.Band(balance.SatietyBands, hungry.Player.Vitals.Satiety).Key);
            Assert.Greater(hungry.Player.Vitals.Stress, 50, "허기지면 스트레스");
        }

        [Test]
        public void Shortage_EatsWhatIsLeft_AndStressesEveryone()
        {
            var balance = new BalanceConfig();
            var state = Make(balance, 0, 2, 50, companion: true);
            var npcStress = state.Npcs[Npc].Body?.Stress ?? 0;
            Assert.IsTrue(UpkeepRules.ApplyMorning(state, balance, new StateDelta(), new FixedRandom()), "4개가 필요한데 2개");
            Assert.AreEqual(0, state.Base.GetResource(ResourceType.Food));
            Assert.AreEqual(balance.FoodSatietyPerUnit, state.Player.Vitals.Satiety, "있는 만큼 나눠 먹는다 (반)");
            Assert.AreEqual(50 + balance.DietShortStressMin, state.Player.Vitals.Stress, "못 지킨 날 스트레스 (FixedRandom = 최소)");
            Assert.AreEqual(npcStress + balance.DietShortStressMin, state.Npcs[Npc].Body.Stress, "동료도");
        }

        [Test]
        public void Hunger_CutsMaxHp_ThirstCutsStamina_WithFloor()
        {
            var balance = new BalanceConfig();
            var state = Make(balance, 0, 0);
            state.Player.Vitals.Hp = 90;
            EffectApplier.ChangeVital(state, VitalType.Satiety, -30, balance, new StateDelta());
            Assert.AreEqual(70, NourishRules.MaxHp(state, balance), "허기 30 → 최대 체력 −30%");
            Assert.AreEqual(70, state.Player.Vitals.Hp, "지금 체력도 최대치까지 내려간다");
            EffectApplier.ChangeVital(state, VitalType.Hp, 20, balance, new StateDelta());
            Assert.AreEqual(70, state.Player.Vitals.Hp, "회복은 줄어든 최대치까지만");

            EffectApplier.ChangeVital(state, VitalType.Satiety, -70, balance, new StateDelta());
            Assert.AreEqual(50, NourishRules.MaxHp(state, balance), "하한 50%");

            Assert.AreEqual(1f, NourishRules.StaminaFactor(10, balance), 1e-5f);
            Assert.AreEqual(0.8f, NourishRules.StaminaFactor(-20, balance), 1e-5f, "갈증 20 → 스태미너 −20%");
            Assert.AreEqual(0.5f, NourishRules.StaminaFactor(-90, balance), 1e-5f);
        }

        [Test]
        public void StarvingStillHurts()
        {
            var balance = new BalanceConfig();
            var state = Make(balance, -90, 0);
            UpkeepRules.ApplyEvening(state, balance, new StateDelta());
            Assert.AreEqual(50 - balance.StarvationHpLoss, state.Player.Vitals.Hp, "굶주림(−60 이하) 단계의 체력 피해는 남긴다 (굶어 죽을 수 있게)");
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
