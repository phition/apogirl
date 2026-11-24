using System;
using System.IO;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Random;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using ApoGirl.Systems.Save;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    public sealed class SaveTests
    {
        private InMemoryDefinitionDatabase _db;
        private GameStateSerializer _serializer;

        [SetUp]
        public void SetUp()
        {
            _db = TestContent.CreateDatabase();
            _serializer = new GameStateSerializer();
        }

        private static void PlayWeek(GameState state, IDefinitionDatabase db)
        {
            var random = new SeededRandom(state.Rng);
            var runner = new WeekRunner(state, db, random, new HeadlessExpeditionRunner(db));
            state.Plan.Slots[0].ActionId = TestContent.Humanities;
            state.Plan.Slots[2].ActionId = TestContent.DayExpedition;
            state.Plan.Slots[4].ActionId = TestContent.MedicineStudy;
            state.Plan.Slots[7].ActionId = TestContent.NightExpedition;
            runner.PrepareWeek();
            while (!runner.IsWeekComplete && !runner.IsGameOver)
            {
                runner.ExecuteNextSlot();
            }

            if (!runner.IsGameOver)
            {
                runner.FinishWeek();
            }
        }

        [Test]
        public void RoundTrip_PreservesState()
        {
            var state = TestContent.NewGame(_db);
            PlayWeek(state, _db);
            state.Flags.Add("FLAG_TEST");
            state.EndingTags["END_TAG"] = 3;
            state.Friend.ContactName = "친구 🙂";

            var json = _serializer.Serialize(state);
            var loaded = _serializer.Deserialize(json);

            Assert.AreEqual(json, _serializer.Serialize(loaded));
            Assert.AreEqual(14, loaded.Plan.Slots.Count, "컬렉션이 중복 생성되지 않아야 함");
            Assert.AreEqual("친구 🙂", loaded.Friend.ContactName);
            StringAssert.Contains("\"Intellect\"", json, "enum은 문자열로 저장");
        }

        [Test]
        public void FutureVersion_IsRejected()
        {
            var root = JObject.Parse(_serializer.Serialize(TestContent.NewGame(_db)));
            root["SaveVersion"] = GameState.CurrentSaveVersion + 1;
            Assert.Throws<SaveVersionException>(() => _serializer.Deserialize(root.ToString()));
        }

        [Test]
        public void OldVersion_RunsMigrationChain()
        {
            var root = JObject.Parse(_serializer.Serialize(TestContent.NewGame(_db)));
            root["SaveVersion"] = 0;
            root.Remove("EndCause");

            Assert.Throws<SaveVersionException>(() => _serializer.Deserialize(root.ToString()), "경로가 없으면 거부");

            var migration = new TestMigration();
            var loaded = new GameStateSerializer(new[] { migration }).Deserialize(root.ToString());
            Assert.IsTrue(migration.Called);
            Assert.AreEqual(GameState.CurrentSaveVersion, loaded.SaveVersion);
            Assert.AreEqual("MIGRATED", loaded.EndCause);
        }

        [Test]
        public void V12Save_NightWorkBecomesNightRest()
        {
            var state = TestContent.NewGame(_db);
            state.Plan.Slots[1].ActionId = "ACT_NIGHT_WORK";
            state.LastWeekPlan = new WeekPlan();
            state.LastWeekPlan.Slots[3].ActionId = "ACT_NIGHT_WORK";
            state.PlanPresets.Add(new PlanPreset { Name = "옛 계획", ActionIds = { "ACT_REST_DAY", "ACT_NIGHT_WORK" } });
            var root = JObject.Parse(_serializer.Serialize(state));
            root["SaveVersion"] = 12;

            var loaded = _serializer.Deserialize(root.ToString());

            Assert.AreEqual(GameState.CurrentSaveVersion, loaded.SaveVersion);
            Assert.AreEqual("ACT_REST_NIGHT", loaded.Plan.Slots[1].ActionId);
            Assert.AreEqual("ACT_REST_NIGHT", loaded.LastWeekPlan.Slots[3].ActionId);
            CollectionAssert.AreEqual(new[] { "ACT_REST_DAY", "ACT_REST_NIGHT" }, loaded.PlanPresets[0].ActionIds);
        }

        [Test]
        public void V13Save_GetsEmptyCrafting()
        {
            var state = TestContent.NewGame(_db);
            var root = JObject.Parse(_serializer.Serialize(state));
            root["SaveVersion"] = 13;
            root.Remove("Crafting");
            ((JObject)root["Base"]).Remove("Scarecrow");

            var loaded = _serializer.Deserialize(root.ToString());

            Assert.AreEqual(GameState.CurrentSaveVersion, loaded.SaveVersion);
            Assert.IsNotNull(loaded.Crafting);
            Assert.IsNull(loaded.Crafting.Active);
            Assert.AreEqual(0, loaded.Base.Scarecrow);
        }

        [Test]
        public void CraftJob_RoundTrips()
        {
            var state = TestContent.NewGame(_db);
            state.Crafting.Active = new CraftJob { RecipeId = "ACT_CRAFT_X", Remaining = 2, NightWorked = true };
            state.Crafting.Active.PaidResources[ResourceType.Materials] = 3;
            state.Crafting.Active.PaidItems.Add(new ItemStack { ItemId = "ITEM_X", Count = 1, Durability = 4 });
            state.Plan.Slots[1].CraftLocked = true;

            var loaded = _serializer.Deserialize(_serializer.Serialize(state));

            Assert.AreEqual(2, loaded.Crafting.Active.Remaining);
            Assert.IsTrue(loaded.Crafting.Active.NightWorked);
            Assert.AreEqual(3, loaded.Crafting.Active.PaidResources[ResourceType.Materials]);
            Assert.AreEqual(4, loaded.Crafting.Active.PaidItems[0].Durability);
            Assert.IsTrue(loaded.Plan.Slots[1].CraftLocked);
        }

        [Test]
        public void LoadedGame_ContinuesIdentically()
        {
            var uninterrupted = TestContent.NewGame(_db, 777);
            PlayWeek(uninterrupted, _db);
            PlayWeek(uninterrupted, _db);

            var interrupted = TestContent.NewGame(_db, 777);
            PlayWeek(interrupted, _db);
            var resumed = _serializer.Deserialize(_serializer.Serialize(interrupted));
            PlayWeek(resumed, _db);

            Assert.AreEqual(_serializer.Serialize(uninterrupted), _serializer.Serialize(resumed));
        }

        [Test]
        public void SaveService_WritesAndReadsSlots()
        {
            var directory = Path.Combine(Path.GetTempPath(), "ApoGirlSaveTest_" + Guid.NewGuid().ToString("N"));
            try
            {
                var service = new SaveService(directory, _serializer);
                var state = TestContent.NewGame(_db);
                state.Calendar.Week = 7;

                service.Save(SaveService.AutoSlot, state);
                service.Save(SaveService.ManualSlot(2), state);
                service.Save(SaveService.ManualSlot(2), state);

                Assert.IsTrue(service.Exists(SaveService.ManualSlot(2)));
                Assert.IsFalse(service.Exists(SaveService.ManualSlot(1)));
                Assert.AreEqual(7, service.Load(SaveService.AutoSlot).Calendar.Week);
                Assert.Throws<ArgumentException>(() => service.Save("../evil", state));
                Assert.Throws<ArgumentOutOfRangeException>(() => SaveService.ManualSlot(4));

                service.Delete(SaveService.AutoSlot);
                Assert.IsFalse(service.Exists(SaveService.AutoSlot));
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        private sealed class TestMigration : ISaveMigration
        {
            public bool Called { get; private set; }

            public int FromVersion => 0;

            public void Migrate(JObject root)
            {
                Called = true;
                root["EndCause"] = "MIGRATED";
            }
        }
    }
}
