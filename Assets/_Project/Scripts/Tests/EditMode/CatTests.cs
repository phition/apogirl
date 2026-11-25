using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Events;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>고양이 '급식' (확장 2): 사람 수에 들지 않고, 다치거나 떠나지 않으며, 주마다 먹고 지친 날 무릎에 올라온다.</summary>
    public sealed class CatTests
    {
        private static GameState WithCat(int catAffinity = 0)
        {
            var state = new GameState();
            state.Npcs[CatRules.CatId] = new NpcState { Status = NpcStatus.Joined, Affinity = catAffinity };
            return state;
        }

        [Test]
        public void Cat_IsNotCountedAsCompanion()
        {
            var state = WithCat(90);
            state.Npcs["NPC_DOHYEON"] = new NpcState { Status = NpcStatus.Joined, Affinity = 70 };
            Assert.AreEqual(1, ConditionEvaluator.ValueOf(state, ValueKind.Companions, ""), "고양이와 둘이면 동료 없이 끝난 판이다");
            Assert.AreEqual(1, ConditionEvaluator.ValueOf(state, ValueKind.CloseCompanions, "60"));
        }

        [Test]
        public void ApplyWeekly_EatsAndComfortsWhenStressed()
        {
            var balance = new BalanceConfig();
            var state = WithCat();
            state.Base.SetResource(ResourceType.Food, 5);
            state.Player.Vitals.Stress = balance.CatComfortStressMin;
            Assert.IsTrue(CatRules.ApplyWeekly(state, balance, new StateDelta()));
            Assert.AreEqual(5 - balance.CatFoodPerWeek, state.Base.GetResource(ResourceType.Food));
            Assert.AreEqual(balance.CatComfortStressMin - balance.CatComfortStress, state.Player.Vitals.Stress);

            // 식량이 없으면 먹지 않고(알아서 쥐를 잡는다), 덜 지쳤으면 무릎에 오지 않는다
            state.Base.SetResource(ResourceType.Food, 0);
            state.Player.Vitals.Stress = 10;
            Assert.IsFalse(CatRules.ApplyWeekly(state, balance, new StateDelta()));
            Assert.AreEqual(0, state.Base.GetResource(ResourceType.Food));

            // 아직 눌러앉지 않았으면 아무것도 하지 않는다
            var stray = new GameState();
            stray.Base.SetResource(ResourceType.Food, 5);
            stray.Player.Vitals.Stress = 90;
            Assert.IsFalse(CatRules.ApplyWeekly(stray, balance, new StateDelta()));
            Assert.AreEqual(5, stray.Base.GetResource(ResourceType.Food));
        }

        [Test]
        public void RaidDamage_IsReducedOnlyAtHome()
        {
            var balance = new BalanceConfig();
            Assert.AreEqual(balance.CatRaidDamageScale, CatRules.RaidDamageScale(WithCat(), balance, false));
            Assert.AreEqual(1f, CatRules.RaidDamageScale(WithCat(), balance, true), "거점을 비운 밤에는 경고해 줄 수 없다");
            Assert.AreEqual(1f, CatRules.RaidDamageScale(new GameState(), balance, false));
        }

        [Test]
        public void Cat_NeverCoversLeavesOrTakesRole()
        {
            var balance = new BalanceConfig { RaidCoverChance = 1f };
            var state = WithCat(100);
            Assert.AreEqual("", CrisisRules.RollCover(state, balance, new FixedRandom(0.0)), "13장: 고양이는 다치지 않는다");

            state.Npcs[CatRules.CatId].Affinity = -50;
            state.Base.SetResource(ResourceType.Food, 0);
            CollectionAssert.IsEmpty(CrisisRules.WarnWeekly(state, balance), "고양이는 위기 이탈 후보가 아니다");

            var db = TestContent.CreateDatabase();
            CollectionAssert.IsEmpty(RoleRules.Choices(state, db, CatRules.CatId));
            Assert.IsFalse(RoleRules.Assign(state, db, CatRules.CatId, "GUARD"));
        }

        [Test]
        public void Recipients_PutCatLast()
        {
            var state = WithCat();
            state.Npcs["NPC_SURVIVOR_A"] = new NpcState { Status = NpcStatus.Joined };
            state.Npcs["NPC_BYSTANDER"] = new NpcState { Status = NpcStatus.Joined };
            var list = GiveRules.Recipients(state, new BalanceConfig());
            Assert.AreEqual(CatRules.CatId, list[list.Count - 1]);
        }
    }
}
