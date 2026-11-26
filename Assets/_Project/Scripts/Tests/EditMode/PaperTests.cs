using System;
using System.IO;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Random;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using ApoGirl.Systems.Content;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>논문(PaperRules)·주택가 102호(황혼에 열리는 방·한 번뿐인 상자) (사용자 요청 2026-11-26).</summary>
    public sealed class PaperTests
    {
        private const string StoryPaper = "ITEM_PAPER_STORY";
        private const string RegionPaper = "ITEM_PAPER_REGION";
        private const string ReadFlag = "PAPER_STORY_READ";
        private const string KnownFlag = "PAPERS_KNOWN";
        private const string Location = "LOC_PAPER";

        private static InMemoryDefinitionDatabase Db(Action<BalanceConfig> tweak = null)
        {
            var balance = new BalanceConfig();
            tweak?.Invoke(balance);
            var items = TestContent.CreateItems().Concat(new[]
            {
                new ItemDef { Id = StoryPaper, Width = 2, StudyActionId = TestContent.MedicineStudy, ReadFlag = ReadFlag },
                new ItemDef { Id = RegionPaper, Width = 2, StudyActionId = TestContent.Humanities, LootFlag = KnownFlag },
            });
            var tables = new[]
            {
                TestContent.CreateLootTable(),
                new LootTableDef { Id = "LOOT_STORY", MinRolls = 1, MaxRolls = 1, EmptyChance = 0f, Entries = { new LootEntry(StoryPaper, 1) } },
                new LootTableDef { Id = "LOOT_ROOM", MinRolls = 1, MaxRolls = 1, EmptyChance = 0f, Entries = { new LootEntry("ITEM_CANNED_FOOD", 5), new LootEntry(RegionPaper, 1) } },
            };
            var model = TestContent.CreateModel();
            model.Id = Location;
            model.Day.BaseDownChance = 0f;
            model.Day.EncounterChance = 1f;
            return new InMemoryDefinitionDatabase(balance, TestContent.CreateActions(), new[] { TestContent.CreateModel(), model }, items, tables,
                new[] { TestContent.CreateLocation(), PaperLocation() });
        }

        /// <summary>낮에는 잠기고 밤에는 열리는 방 R1(숨은 사람 + 한 번뿐인 상자), 늘 열린 방 R2.</summary>
        private static LocationDef PaperLocation()
        {
            var hideout = new RoomTemplateDef
            {
                Id = "ROOM_HIDEOUT", Width = 12, DefaultLootTableId = "LOOT_ROOM",
                Containers =
                {
                    new ContainerDef { Id = "SHELF", Kind = ContainerKind.Box, X = 3 },
                    new ContainerDef { Id = "PAPERS", Kind = ContainerKind.Desk, X = 6, LootTableId = "LOOT_STORY", Guaranteed = true, Once = true },
                },
            };
            var room = new RoomTemplateDef { Id = "ROOM_PLAIN", Width = 10, Containers = { new ContainerDef { Id = "BOX", Kind = ContainerKind.Box, X = 4 } } };
            var hidden = new EncounterDef
            {
                Id = "HIDDEN", Floor = 0, RoomId = "R1", X = 9,
                Choices = { new EncounterChoiceDef { Id = "INVITE", SetFlags = { "HIDDEN_FOUND" } } },
            };
            return new LocationDef
            {
                Id = Location,
                DefaultLootTableId = "LOOT_ROOM",
                Layout = new BuildingLayoutDef
                {
                    Floors = 1, CorridorLength = 30, RoomTemplates = { hideout, room },
                    Doors =
                    {
                        new DoorDef { RoomId = "R1", TemplateId = "ROOM_HIDEOUT", Floor = 0, X = 8, Number = 1 },
                        new DoorDef { RoomId = "R2", TemplateId = "ROOM_PLAIN", Floor = 0, X = 20, Number = 2 },
                    },
                    Exits = { new ExitDef { Kind = ExitKind.Main, Floor = 0, X = 1 } },
                },
                DayVariant = new LocationVariantDef { LockedRooms = { "R1" }, Encounters = { hidden } },
                NightVariant = new LocationVariantDef { Encounters = { hidden } },
            };
        }

        private static ExpeditionRequest Request(ExpeditionTime time, int seed = 5) => new ExpeditionRequest
        {
            LocationId = Location, TimeOfDay = time, Seed = seed, Hp = 100, Week = 20,
        };

        private sealed class FirstChoice : IEncounterChooser
        {
            public int Choose(EncounterDef encounter, ExpeditionRequest request, IRandomService random) => 0;
        }

        [Test]
        public void Read_GivesTwoClassesOfGains_AndSetsReadFlag()
        {
            var db = Db();
            var state = TestContent.NewGame(db);
            var intellect = state.Player.GetStat(StatType.Intellect);
            var craft = state.Player.GetStat(StatType.Craft);
            var delta = new StateDelta();

            Assert.IsTrue(PaperRules.Read(state, db, StoryPaper, delta));
            Assert.AreEqual(2, db.Balance.PaperClassCount);
            Assert.AreEqual(intellect + 2 * 2, state.Player.GetStat(StatType.Intellect), "의학 수업 지능 +2 × 2회");
            Assert.AreEqual(craft + 1 * 2, state.Player.GetStat(StatType.Craft), "손재주 +1 × 2회");
            Assert.AreEqual(2, state.Player.GetProficiency(ProficiencyType.Medicine).Xp, "숙련 경험치 1 × 2회");
            Assert.IsTrue(state.Flags.Contains(ReadFlag));
            CollectionAssert.Contains(delta.FlagsSet, ReadFlag);

            Assert.IsFalse(PaperRules.Read(state, db, "ITEM_TEXTBOOK", new StateDelta()), "논문이 아닌 물건");
        }

        [Test]
        public void Read_FollowsPaperClassCount()
        {
            var db = Db(b => b.PaperClassCount = 3);
            var state = TestContent.NewGame(db);
            PaperRules.Read(state, db, RegionPaper, new StateDelta());
            Assert.AreEqual(3, state.Player.GetProficiency(ProficiencyType.Humanities).Xp);
        }

        [Test]
        public void Expedition_PaperIsReadNotStored()
        {
            var db = Db();
            var state = TestContent.NewGame(db, 11);
            var stub = new StubExpeditionRunner
            {
                Handler = _ => new ExpeditionResult
                {
                    Status = ExtractStatus.Extracted,
                    Loot = { new ItemStack { ItemId = StoryPaper, Count = 1 }, new ItemStack { ItemId = "ITEM_TEXTBOOK", Count = 1 } },
                },
            };
            var runner = new WeekRunner(state, db, new SeededRandom(state.Rng), stub);
            state.Plan.Slots[0].ActionId = TestContent.DayExpedition;
            runner.PrepareWeek();
            var outcome = runner.ExecuteNextSlot();

            CollectionAssert.AreEqual(new[] { StoryPaper }, outcome.PapersRead);
            Assert.IsFalse(state.Storage.Any(s => s.ItemId == StoryPaper), "논문은 창고에 넣지 않는다");
            Assert.IsTrue(state.Storage.Any(s => s.ItemId == "ITEM_TEXTBOOK"));
            Assert.AreEqual(2, state.Player.GetProficiency(ProficiencyType.Medicine).Xp);
            Assert.IsTrue(state.Flags.Contains(ReadFlag));
        }

        [Test]
        public void LootFlag_HidesRegionPaperUntilKnown()
        {
            var db = Db(b => b.PaperLootKeepChance = 1f);
            var none = new System.Collections.Generic.HashSet<string>();
            var known = new System.Collections.Generic.HashSet<string> { KnownFlag };
            Assert.IsFalse(PaperRules.KeepRolled(db, RegionPaper, none, 1), "102호 장면 전에는 나오지 않는다");
            Assert.IsTrue(PaperRules.KeepRolled(db, RegionPaper, known, 1));
            Assert.IsTrue(PaperRules.KeepRolled(db, StoryPaper, none, 1), "102호 논문은 깃발 없이");
            Assert.IsTrue(PaperRules.KeepRolled(db, "ITEM_CANNED_FOOD", none, 1));

            var rare = Db(b => b.PaperLootKeepChance = 0f);
            Assert.IsFalse(PaperRules.KeepRolled(rare, RegionPaper, known, 1), "남기는 확률 0이면 걸러진다");
        }

        [Test]
        public void DuskRooms_AreDayLockedButOpenAtNight()
        {
            CollectionAssert.AreEqual(new[] { "R1" }, PaperRules.DuskRooms(PaperLocation()));
            var both = PaperLocation();
            both.NightVariant.LockedRooms.Add("R1");
            CollectionAssert.IsEmpty(PaperRules.DuskRooms(both), "밤에도 잠기면 황혼에 열리지 않는다");
        }

        [Test]
        public void Headless_DayLockedRoom_OnlyWhenStayingPastDusk()
        {
            var shut = new HeadlessExpeditionRunner(Db(b => { b.HeadlessDuskRoomChance = 0f; b.HeadlessPaperChance = 0f; }), new FirstChoice())
                .Run(Request(ExpeditionTime.Day));
            Assert.AreEqual(ExtractStatus.Extracted, shut.Status);
            CollectionAssert.DoesNotContain(shut.Flags, "HIDDEN_FOUND", "낮에 잠긴 방 안의 사람은 보통 굴림으로 만나지 않는다");
            Assert.IsFalse(shut.Loot.Any(s => s.ItemId == StoryPaper));

            var db = Db(b => { b.HeadlessDuskRoomChance = 1f; b.HeadlessPaperChance = 0f; });
            var baseline = new HeadlessExpeditionRunner(Db(b => { b.HeadlessDuskRoomChance = 0f; b.HeadlessPaperChance = 0f; }), new FirstChoice()).Run(Request(ExpeditionTime.Day));
            var dusk = new HeadlessExpeditionRunner(db, new FirstChoice()).Run(Request(ExpeditionTime.Day));
            CollectionAssert.Contains(dusk.Flags, "HIDDEN_FOUND", "황혼까지 버티면 그 방의 사람을 만난다");
            Assert.IsTrue(dusk.Loot.Any(s => s.ItemId == StoryPaper), "한 번뿐인 상자의 논문");
            CollectionAssert.Contains(dusk.SearchedContainers, HostileRules.RoomKey(Location, 0, 1));
            Assert.AreEqual(baseline.FatigueDelta + db.Balance.StrandedFatigue, dusk.FatigueDelta, 1e-4f, "시간 초과 귀환 피로");

            var request = Request(ExpeditionTime.Day);
            request.LootedContainers.Add(HostileRules.RoomKey(Location, 0, 1));
            var again = new HeadlessExpeditionRunner(db, new FirstChoice()).Run(request);
            Assert.IsFalse(again.Loot.Any(s => s.ItemId == StoryPaper), "이미 뒤진 한 번뿐인 상자는 비어 있다");
        }

        [Test]
        public void Headless_NightRoomIsOpen_EncounterRollsNormally()
        {
            var db = Db(b => { b.HeadlessDuskRoomChance = 0f; b.HeadlessPaperChance = 0f; });
            var model = db.GetExpeditionModel(Location);
            model.Night.EncounterChance = 1f;
            model.Night.BaseDownChance = 0f;
            var night = new HeadlessExpeditionRunner(db, new FirstChoice()).Run(Request(ExpeditionTime.Night));
            CollectionAssert.Contains(night.Flags, "HIDDEN_FOUND");
        }

        [Test]
        public void Headless_RegionPaper_NeedsKnownFlag()
        {
            var db = Db(b => { b.HeadlessPaperChance = 1f; b.HeadlessDuskRoomChance = 0f; });
            var before = new HeadlessExpeditionRunner(db, new FirstChoice()).Run(Request(ExpeditionTime.Day));
            Assert.IsFalse(before.Loot.Any(s => s.ItemId == RegionPaper), "102호 장면 전에는 없다");

            var request = Request(ExpeditionTime.Day);
            request.Flags.Add(KnownFlag);
            var after = new HeadlessExpeditionRunner(db, new FirstChoice()).Run(request);
            Assert.IsTrue(after.Loot.Any(s => s.ItemId == RegionPaper));
            Assert.IsFalse(after.Loot.Any(s => s.ItemId == StoryPaper), "한 번뿐인 상자의 표는 아무 논문 줍기에서 빠진다");
        }

        [Test]
        public void Restock_SkipsOnceContainers()
        {
            var location = PaperLocation();
            var onceKey = HostileRules.RoomKey(Location, 0, 1);
            Assert.IsTrue(ContainerRules.IsOnce(location, onceKey));
            Assert.IsFalse(ContainerRules.IsOnce(location, HostileRules.RoomKey(Location, 0, 0)));
            Assert.IsNull(ContainerRules.Find(location, Location + "/R9.0"));
            Assert.IsNull(ContainerRules.Find(location, "nonsense"));

            var db = Db(b => b.RestockFraction = 1f);
            var state = TestContent.NewGame(db);
            state.Exploration.LootedContainers.Add(onceKey);
            state.Exploration.LootedContainers.Add(HostileRules.RoomKey(Location, 1, 0));
            ContainerRules.Restock(state, db, new SeededRandom(SeededRandom.CreateState(3)), db.Balance);
            CollectionAssert.AreEqual(new[] { onceKey }, state.Exploration.LootedContainers.ToList(), "한 번뿐인 상자만 비어 남는다");
        }

        [Test]
        public void LootAdds_AppendToTablesFromOtherFiles()
        {
            var content = ContentJson.Parse(@"{
  ""LootTables"": [ { ""Id"": ""LOOT_A"", ""Entries"": [ { ""ItemId"": ""ITEM_X"", ""Weight"": 3 } ] } ],
  ""LootAdds"": [ { ""Table"": ""LOOT_A"", ""ItemId"": ""ITEM_PAPER"", ""Weight"": 1 } ]
}");
            ContentJson.ApplyLootAdds(content);
            var table = content.LootTables.Single();
            Assert.AreEqual(2, table.Entries.Count);
            Assert.AreEqual("ITEM_PAPER", table.Entries[1].ItemId);
            Assert.AreEqual(1, table.Entries[1].Weight);

            var broken = ContentJson.Parse(@"{ ""LootAdds"": [ { ""Table"": ""LOOT_NONE"", ""ItemId"": ""ITEM_PAPER"" } ] }");
            Assert.Throws<FormatException>(() => ContentJson.ApplyLootAdds(broken));
        }

        [Test]
        public void ShippedPapers_AreWired()
        {
            var content = ContentJson.LoadFolder(ContentPipelineTests.ProjectPath(ContentJson.ContentFolder));
            var papers = content.Items.Where(i => i.IsPaper).ToList();
            Assert.GreaterOrEqual(papers.Count, 10, "102호 논문 + 과목 9종");
            var actions = content.Actions.ToDictionary(a => a.Id);
            var loc = File.ReadAllText(ContentPipelineTests.ProjectPath("Assets/_Project/Localization/Source/ko.json"));
            foreach (var paper in papers)
            {
                Assert.IsTrue(actions.TryGetValue(paper.StudyActionId, out var action), paper.Id + " → " + paper.StudyActionId);
                Assert.AreEqual(ActionCategory.Education, action.Category, paper.Id);
                StringAssert.Contains("\"" + paper.Id + ".name\"", loc, paper.Id + " 이름");
                StringAssert.Contains("\"" + paper.Id + ".desc\"", loc, paper.Id + " 설명");
            }

            var nurse = papers.Single(p => p.Id == "ITEM_PAPER_NURSE");
            Assert.AreEqual("PAPER_NURSE_READ", nurse.ReadFlag);
            Assert.IsEmpty(nurse.LootFlag, "102호 논문은 처음부터 나온다");
            Assert.IsTrue(papers.Where(p => p != nurse).All(p => p.LootFlag == "PAPERS_KNOWN"), "지역 논문은 102호 장면 뒤에만");

            var regional = papers.Where(p => p != nurse).Select(p => p.Id).ToList();
            foreach (var id in regional)
            {
                Assert.IsTrue(content.LootTables.Any(t => t.Entries.Any(e => e.ItemId == id)), id + "가 어느 전리품 표에 있다");
            }

            Assert.IsTrue(content.LootTables.Single(t => t.Id == "LOOT_HOUSE_NURSE").Entries.All(e => e.ItemId == "ITEM_PAPER_NURSE"));
            var scene = content.Events.Single(e => e.Id == "EVT_A2_NURSE_PAPER");
            CollectionAssert.Contains(scene.Condition.FlagsAll, "PAPER_NURSE_READ");
            CollectionAssert.Contains(scene.OnShow.SetFlags, "PAPERS_KNOWN");
        }
    }
}
