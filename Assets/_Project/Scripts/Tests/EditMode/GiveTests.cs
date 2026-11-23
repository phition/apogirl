using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using ApoGirl.Systems.Save;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>이번 주 배분(기획 D.3·D.4): 동료·선생님 몸 상태, 나눠 주기 한도·효과, 기호품, 헤드리스 기호품 줍기, 세이브 v7.</summary>
    public sealed class GiveTests
    {
        private const string Npc = "NPC_NURSE";

        private static GameState Make(BalanceConfig balance, bool teacher = true)
        {
            var state = NewGameFactory.Create(balance, 1);
            state.Calendar.Week = 5;
            state.Base.SetResource(ResourceType.Food, 10);
            state.Base.SetResource(ResourceType.Water, 10);
            state.Base.SetResource(ResourceType.Medicine, 3);
            state.Npcs[Npc] = new NpcState { Status = NpcStatus.Joined };
            if (teacher)
            {
                state.Events.Records[balance.OpeningEventId] = new EventRecord { Count = 1, LastWeek = 1 };
            }

            return state;
        }

        [Test]
        public void Recipients_TeacherAfterOpening_PlusJoined()
        {
            var balance = new BalanceConfig();
            CollectionAssert.AreEqual(new[] { Npc }, GiveRules.Recipients(Make(balance, false), balance), "창고 문을 열기 전에는 선생님이 없다");
            CollectionAssert.AreEqual(new[] { GiveRules.TeacherId, Npc }, GiveRules.Recipients(Make(balance), balance));
        }

        [Test]
        public void Food_ConsumesOne_RaisesSatiety_OncePerWeek()
        {
            var balance = new BalanceConfig();
            var state = Make(balance);
            Assert.IsTrue(GiveRules.Give(state, balance, Npc, GiveKind.Food, null, new StateDelta(), out _));
            Assert.AreEqual(9, state.Base.GetResource(ResourceType.Food));
            Assert.AreEqual(balance.GiveFoodSatiety, state.Npcs[Npc].Body.Satiety);
            Assert.AreEqual(balance.GiveAffinityBasic, state.Npcs[Npc].Affinity);
            Assert.AreEqual("give.blocked.used", GiveRules.Blocker(state, balance, Npc, GiveKind.Food));
            Assert.IsNull(GiveRules.Blocker(state, balance, Npc, GiveKind.Water), "종류가 다르면 또 줄 수 있다");

            state.Calendar.Week++;
            Assert.IsNull(GiveRules.Blocker(state, balance, Npc, GiveKind.Food), "다음 주에는 다시");
            state.Base.SetResource(ResourceType.Food, 0);
            Assert.AreEqual("give.blocked.no_food", GiveRules.Blocker(state, balance, Npc, GiveKind.Food));
            Assert.AreEqual("give.blocked.absent", GiveRules.Blocker(state, balance, "NPC_DOHYEON", GiveKind.Water));
        }

        [Test]
        public void Medicine_ShortensInjury_AndThanksMore()
        {
            var balance = new BalanceConfig();
            var state = Make(balance);
            state.Npcs[Npc].InjuredWeeks = 2;
            Assert.IsTrue(GiveRules.Give(state, balance, Npc, GiveKind.Medicine, null, new StateDelta(), out _));
            Assert.AreEqual(2, state.Base.GetResource(ResourceType.Medicine));
            Assert.AreEqual(2 - balance.GiveMedicineInjuryWeeks, state.Npcs[Npc].InjuredWeeks);
            Assert.AreEqual(balance.GiveAffinityMedicineInjured, state.Npcs[Npc].Affinity);
        }

        [Test]
        public void LikedGift_BigBonus_RepeatWithinTwoWeeksHalved()
        {
            var balance = new BalanceConfig();
            var state = Make(balance);
            state.Storage.Add(new ItemStack { ItemId = "ITEM_COFFEE_MIX", Count = 2 });
            state.Storage.Add(new ItemStack { ItemId = "ITEM_COMIC", Count = 1 });
            state.Storage.Add(new ItemStack { ItemId = "ITEM_SCRAP", Count = 1 });
            CollectionAssert.AreEquivalent(new[] { "ITEM_COFFEE_MIX", "ITEM_COMIC" }, GiveRules.GiftCandidates(state, balance), "기호품 표에 있는 것만");
            Assert.AreEqual("give.blocked.no_item", GiveRules.Blocker(state, balance, Npc, GiveKind.Gift, "ITEM_SCRAP"));

            state.Npcs[Npc].Body.Stress = 50;
            Assert.IsTrue(GiveRules.Give(state, balance, Npc, GiveKind.Gift, "ITEM_COFFEE_MIX", new StateDelta(), out var liked));
            Assert.IsTrue(liked, "박 선생님은 커피를 좋아한다");
            Assert.AreEqual(balance.GiveAffinityLiked, state.Npcs[Npc].Affinity);
            Assert.AreEqual(50 + balance.GiveLikedStress, state.Npcs[Npc].Body.Stress);
            Assert.AreEqual(1, LoadoutRules.CountInStorage(state, "ITEM_COFFEE_MIX"));

            state.Calendar.Week++;
            Assert.IsTrue(GiveRules.Give(state, balance, Npc, GiveKind.Gift, "ITEM_COFFEE_MIX", new StateDelta(), out _));
            Assert.AreEqual(balance.GiveAffinityLiked + balance.GiveAffinityLiked / 2, state.Npcs[Npc].Affinity, "2주 안에 같은 물건은 반감");

            state.Calendar.Week++;
            Assert.IsTrue(GiveRules.Give(state, balance, GiveRules.TeacherId, GiveKind.Gift, "ITEM_COMIC", new StateDelta(), out liked));
            Assert.IsFalse(liked, "선생님은 만화책을 특별히 좋아하지 않는다");
        }

        [Test]
        public void DailyUpkeep_Drains_RefillsWhenFed_StarvingStresses()
        {
            var balance = new BalanceConfig();
            var state = Make(balance);
            GiveRules.ApplyMeal(state, balance, GiveRules.Recipients(state, balance), 16, 24, null);
            GiveRules.ApplyEvening(state, balance);
            Assert.AreEqual(16 - balance.SatietyDailyDrain, state.Npcs[Npc].Body.Satiety, "아침 식사 − 밤 전 감소");
            Assert.AreEqual(24 - balance.HydrationDailyDrain, state.Npcs[Npc].Body.Hydration);

            for (var i = 0; i < 5; i++)
            {
                GiveRules.ApplyEvening(state, balance);
            }

            Assert.Less(state.Npcs[Npc].Body.Satiety, 0);
            Assert.Less(state.Npcs[Npc].Body.Hydration, 0);
            Assert.Greater(state.Npcs[Npc].Body.Stress, 0, "굶으면 동료도 스트레스");

            state.Npcs[Npc].Body.Stress = balance.CompanionStressRoleThreshold;
            Assert.IsTrue(GiveRules.IsStressed(state.Npcs[Npc], balance), "지치면 역할 효과가 준다");
            GiveRules.ApplyWeekly(state, balance);
            Assert.AreEqual(balance.CompanionStressRoleThreshold - balance.CompanionWeeklyStressRecovery, state.Npcs[Npc].Body.Stress);
        }

        [Test]
        public void Headless_PicksLikedItemFromLocationTables_Sometimes()
        {
            var table = TestContent.CreateLootTable();
            table.Entries.Add(new LootEntry("ITEM_CANDY", 1));
            var db = new InMemoryDefinitionDatabase(
                new BalanceConfig { HeadlessLikedItemDayChance = 1f }, TestContent.CreateActions(), new[] { TestContent.CreateModel() },
                TestContent.CreateItems().Append(new ItemDef { Id = "ITEM_CANDY", Weight = 0.05f }), new[] { table }, new[] { TestContent.CreateLocation() });
            var runner = new HeadlessExpeditionRunner(db);
            var found = Enumerable.Range(1, 40)
                .Select(seed => runner.Run(new ExpeditionRequest { LocationId = TestContent.Location, Week = 3, Seed = seed, TimeOfDay = ExpeditionTime.Day, Hp = 100 }))
                .Where(r => r.Status == ExtractStatus.Extracted)
                .ToList();
            Assert.IsNotEmpty(found);
            Assert.IsTrue(found.All(r => r.Loot.Any(l => l.ItemId == "ITEM_CANDY")), "확률 1이면 무사히 돌아온 판마다 기호품 하나");
        }

        [Test]
        public void SaveV7_RoundTripsBody_AndV6Migrates()
        {
            var balance = new BalanceConfig();
            var state = Make(balance);
            state.Npcs[Npc].Body.Stress = 33;
            state.Npcs[Npc].GiftLog = new System.Collections.Generic.Dictionary<string, int> { ["kind:Food"] = 5 };
            state.Teacher.Body = new BodyState { Satiety = -20 };
            var serializer = new GameStateSerializer();
            var restored = serializer.Deserialize(serializer.Serialize(state));
            Assert.AreEqual(33, restored.Npcs[Npc].Body.Stress);
            Assert.AreEqual(5, restored.Npcs[Npc].GiftLog["kind:Food"]);
            Assert.AreEqual(-20, restored.Teacher.Body.Satiety);

            var json = Newtonsoft.Json.Linq.JObject.Parse(serializer.Serialize(state));
            json["SaveVersion"] = 6;
            ((Newtonsoft.Json.Linq.JObject)json["Npcs"][Npc]).Remove("Body");
            ((Newtonsoft.Json.Linq.JObject)json["Teacher"]).Remove("Body");
            var old = serializer.Deserialize(json.ToString());
            Assert.AreEqual(GameState.CurrentSaveVersion, old.SaveVersion);
            Assert.IsNotNull(GiveRules.Body(old, Npc), "몸 상태가 없던 세이브도 보통 상태로 시작");
            Assert.AreEqual(0, GiveRules.Body(old, GiveRules.TeacherId).Stress);
        }
    }
}
