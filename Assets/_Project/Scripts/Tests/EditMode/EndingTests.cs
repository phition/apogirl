using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Endings;
using ApoGirl.Core.Events;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Random;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using ApoGirl.Systems.Content;
using ApoGirl.Systems.Save;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>M5: 엔딩 판정·공통 레이어·종료 원인·정신 붕괴·주간 기록·갤러리·출시 엔딩 도달.</summary>
    public sealed class EndingTests
    {
        private static InMemoryDefinitionDatabase Db(IEnumerable<EndingDef> endings, IEnumerable<LineDef> lines = null, Action<BalanceConfig> tweak = null)
        {
            var balance = new BalanceConfig();
            tweak?.Invoke(balance);
            return new InMemoryDefinitionDatabase(
                balance, TestContent.CreateActions(), new[] { TestContent.CreateModel() }, TestContent.CreateItems(),
                new[] { TestContent.CreateLootTable() }, new[] { TestContent.CreateLocation() },
                extras: new DefinitionExtras { Endings = endings.ToList(), Lines = (lines ?? Array.Empty<LineDef>()).ToList() });
        }

        private static EndingDef Ending(string id, EndingCategory category, int priority, ConditionDef condition = null, params string[] causes)
        {
            return new EndingDef { Id = id, Category = category, Priority = priority, Condition = condition ?? new ConditionDef(), EndCauses = causes.ToList(), CutKeys = { "cut." + id } };
        }

        private static ConditionDef StatAtLeast(StatType stat, int value) => new ConditionDef { Checks = { new ValueCheck(ValueKind.Stat, stat.ToString(), CompareOp.GreaterOrEqual, value) } };

        private static GameState Ended(InMemoryDefinitionDatabase db, GameStatus status, string cause = "")
        {
            var state = TestContent.NewGame(db);
            state.Status = status;
            state.EndCause = cause;
            return state;
        }

        // ── 판정 ──

        [Test]
        public void Rescue_PicksHighestPriorityMet_ThenScore()
        {
            var normal = Ending("NORMAL", EndingCategory.Normal, 100);
            var doctor = Ending("DOCTOR", EndingCategory.Specialized, 300, StatAtLeast(StatType.Intellect, 50));
            doctor.Score.Add(new EndingScoreTerm { Kind = ValueKind.Stat, Key = "Intellect" });
            var guard = Ending("GUARD", EndingCategory.Specialized, 300, StatAtLeast(StatType.Strength, 50));
            guard.Score.Add(new EndingScoreTerm { Kind = ValueKind.Stat, Key = "Strength" });
            var bad = Ending("BAD", EndingCategory.Bad, 0);
            var db = Db(new[] { bad, normal, doctor, guard });

            var state = Ended(db, GameStatus.Rescued);
            Assert.AreEqual("NORMAL", EndingResolver.Resolve(state, db).Ending.Id, "조건 맞는 특화가 없으면 기본 엔딩");

            state.Player.Stats[StatType.Intellect] = 60;
            Assert.AreEqual("DOCTOR", EndingResolver.Resolve(state, db).Ending.Id, "우선순위가 높은 특화");

            state.Player.Stats[StatType.Strength] = 70;
            Assert.AreEqual("GUARD", EndingResolver.Resolve(state, db).Ending.Id, "같은 우선순위면 점수 (근력 70 > 지력 60)");
        }

        [Test]
        public void GameOver_PicksBadEndingByCause_WithFallback()
        {
            var db = Db(new[]
            {
                Ending("RESCUE", EndingCategory.Normal, 100),
                Ending("BAD_ANY", EndingCategory.Bad, 0),
                Ending("BAD_RAID", EndingCategory.Bad, 0, null, WeekRunner.EndCauseRaid),
            });

            Assert.AreEqual("BAD_RAID", EndingResolver.Resolve(Ended(db, GameStatus.GameOver, WeekRunner.EndCauseRaid), db).Ending.Id);
            Assert.AreEqual("BAD_ANY", EndingResolver.Resolve(Ended(db, GameStatus.GameOver, WeekRunner.EndCauseStarvation), db).Ending.Id, "원인이 맞는 엔딩이 없으면 대체 엔딩");
            Assert.IsTrue(EndingResolver.Resolve(Ended(db, GameStatus.GameOver, WeekRunner.EndCauseRaid), db).IsBad);
        }

        [Test]
        public void Layers_AppendOnlyOnRescue_AndMomentsCollectAll()
        {
            var lines = new[]
            {
                new LineDef { Id = "T0", Kind = LineKind.EndingTeacher, Priority = 0, TextKey = "teacher.default" },
                new LineDef { Id = "T1", Kind = LineKind.EndingTeacher, Priority = 50, TextKey = "teacher.lesson", Condition = new ConditionDef { FlagsAll = { "LESSON" } } },
                new LineDef { Id = "F0", Kind = LineKind.EndingFriend, Priority = 0, TextKey = "friend.default" },
                new LineDef { Id = "M1", Kind = LineKind.EndingMoment, TextKey = "moment.door" },
                new LineDef { Id = "M2", Kind = LineKind.EndingMoment, TextKey = "moment.crutch", Condition = new ConditionDef { FlagsAll = { "CRUTCH_DONE" } } },
            };
            var db = Db(new[] { Ending("RESCUE", EndingCategory.Normal, 100), Ending("BAD", EndingCategory.Bad, 0) }, lines);

            var rescued = Ended(db, GameStatus.Rescued);
            rescued.Flags.Add("LESSON");
            var result = EndingResolver.Resolve(rescued, db);
            CollectionAssert.AreEqual(new[] { "cut.RESCUE", "teacher.lesson", "friend.default" }, result.CutKeys, "엔딩 컷 → 선생님 → 친구 레이어 (각 하나)");
            CollectionAssert.AreEqual(new[] { "moment.door" }, result.MomentKeys);

            rescued.Flags.Add("CRUTCH_DONE");
            Assert.AreEqual(2, EndingResolver.Resolve(rescued, db).MomentKeys.Count, "주요 장면은 조건 맞는 줄 전부");

            var bad = EndingResolver.Resolve(Ended(db, GameStatus.GameOver, WeekRunner.EndCauseHpZero), db);
            CollectionAssert.AreEqual(new[] { "cut.BAD" }, bad.CutKeys, "배드 엔딩에는 구조 레이어가 붙지 않음");
        }

        [Test]
        public void DerivedValues_ForEndingConditions()
        {
            var db = TestContent.CreateDatabase();
            var state = TestContent.NewGame(db);
            state.Exploration.DayExplorations = 3;
            state.Exploration.NightExplorations = 1;
            Assert.AreEqual(25, ConditionEvaluator.ValueOf(state, ValueKind.NightRatio, ""));

            foreach (StatType stat in Enum.GetValues(typeof(StatType))) state.Player.Stats[stat] = 40;
            state.Player.Stats[StatType.Craft] = 70;
            Assert.AreEqual(30, ConditionEvaluator.ValueOf(state, ValueKind.StatSpread, ""));
            Assert.AreEqual(40, ConditionEvaluator.ValueOf(state, ValueKind.StatMin, ""));

            state.Npcs["NPC_A"] = new NpcState { Status = NpcStatus.Joined };
            state.Npcs["NPC_B"] = new NpcState { Status = NpcStatus.Met };
            Assert.AreEqual(1, ConditionEvaluator.ValueOf(state, ValueKind.Companions, ""));

            state.Clues.Add("CURE_A");
            state.Clues.Add("CURE_B");
            state.Clues.Add("TRUTH_A");
            Assert.AreEqual(2, ConditionEvaluator.ValueOf(state, ValueKind.ClueCount, "CURE_"));
            Assert.AreEqual(3, ConditionEvaluator.ValueOf(state, ValueKind.ClueCount, ""));
        }

        // ── 종료 원인 ──

        private static WeekRunner Runner(GameState state, IDefinitionDatabase db, StubExpeditionRunner expedition = null)
        {
            return new WeekRunner(state, db, new SeededRandom(state.Rng), expedition ?? new StubExpeditionRunner());
        }

        [Test]
        public void HpZero_OnExpedition_RecordsExpeditionCause()
        {
            var db = TestContent.CreateDatabase();
            var state = TestContent.NewGame(db);
            state.Player.Vitals.Hp = 10;
            state.Plan.Slots[0].ActionId = TestContent.DayExpedition;
            var expedition = new StubExpeditionRunner { Handler = _ => new ExpeditionResult { Status = ExtractStatus.Down, HpDelta = -50 } };
            var runner = Runner(state, db, expedition);
            runner.PrepareWeek();
            runner.ExecuteNextSlot();
            Assert.AreEqual(GameStatus.GameOver, state.Status);
            Assert.AreEqual(WeekRunner.EndCauseExpedition, state.EndCause);
        }

        [Test]
        public void HpZero_FromHunger_RecordsStarvationCause()
        {
            var db = TestContent.CreateDatabase();
            var state = TestContent.NewGame(db);
            state.Base.Resources[ResourceType.Food] = 0;
            state.Base.Resources[ResourceType.Water] = 0;
            state.Player.Vitals.Hunger = 90;
            state.Player.Vitals.Thirst = 90;
            state.Player.Vitals.Hp = 3;
            var runner = Runner(state, db);
            runner.PrepareWeek();
            while (state.Status == GameStatus.Playing && !runner.IsWeekComplete)
            {
                runner.ExecuteNextSlot();
            }

            Assert.AreEqual(GameStatus.GameOver, state.Status);
            Assert.AreEqual(WeekRunner.EndCauseStarvation, state.EndCause);
        }

        [Test]
        public void Breakdown_EndsAfterConsecutiveMaxStressWeeks_AndResetsOnRelief()
        {
            var db = TestContent.CreateDatabase(b => { b.CollapseWeeks = 2; b.CollapseStress = 90; });
            var state = TestContent.NewGame(db);
            state.Base.Resources[ResourceType.Food] = 200;
            state.Base.Resources[ResourceType.Water] = 200;
            var runner = Runner(state, db);

            void PlayWeekAtStress(int stress)
            {
                runner.PrepareWeek();
                while (!runner.IsWeekComplete && state.Status == GameStatus.Playing)
                {
                    runner.ExecuteNextSlot();
                }

                state.Player.Vitals.Stress = stress;
                if (state.Status == GameStatus.Playing) runner.FinishWeek();
            }

            PlayWeekAtStress(95);
            Assert.AreEqual(1, state.Player.BreakdownWeeks);
            Assert.AreEqual(GameStatus.Playing, state.Status, "1주째는 경고만");
            PlayWeekAtStress(10);
            Assert.AreEqual(0, state.Player.BreakdownWeeks, "스트레스가 내려가면 초기화");
            PlayWeekAtStress(95);
            PlayWeekAtStress(95);
            Assert.AreEqual(GameStatus.GameOver, state.Status);
            Assert.AreEqual(WeekRunner.EndCauseCollapse, state.EndCause);
        }

        [Test]
        public void FinishWeek_RecordsHistory()
        {
            var db = TestContent.CreateDatabase();
            var state = TestContent.NewGame(db);
            var runner = Runner(state, db);
            runner.PrepareWeek();
            while (!runner.IsWeekComplete)
            {
                runner.ExecuteNextSlot();
            }

            runner.FinishWeek();
            Assert.AreEqual(1, state.History.Count);
            Assert.AreEqual(1, state.History[0].Week);
            Assert.AreEqual(Enum.GetValues(typeof(StatType)).Length, state.History[0].Stats.Count);
        }

        // ── 세이브·갤러리 ──

        [Test]
        public void SaveV3_MigratesToV4_WithEmptyHistory()
        {
            var serializer = new GameStateSerializer();
            var db = TestContent.CreateDatabase();
            var root = JObject.Parse(serializer.Serialize(TestContent.NewGame(db)));
            root.Remove("History");
            root["SaveVersion"] = 3;
            var loaded = serializer.Deserialize(root.ToString());
            Assert.AreEqual(GameState.CurrentSaveVersion, loaded.SaveVersion);
            Assert.IsNotNull(loaded.History);
            Assert.AreEqual(0, loaded.History.Count);
        }

        [Test]
        public void Gallery_RecordsAndPersists()
        {
            var directory = Path.Combine(Path.GetTempPath(), "ApoGirlGallery_" + Guid.NewGuid().ToString("N"));
            try
            {
                var gallery = new EndingGallery(directory);
                Assert.IsTrue(gallery.Record("ENDING_A", 52), "처음 본 엔딩");
                Assert.IsFalse(gallery.Record("ENDING_A", 52), "두 번째는 새 엔딩 아님");

                var reloaded = new EndingGallery(directory);
                Assert.IsTrue(reloaded.IsUnlocked("ENDING_A"));
                Assert.AreEqual(2, reloaded.Entries["ENDING_A"].Count);
                Assert.AreEqual(52, reloaded.Entries["ENDING_A"].FirstWeek);
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        // ── 출시 엔딩 (Content/Story/endings.json) ──

        private static InMemoryDefinitionDatabase ShippedDb()
        {
            var content = ContentJson.LoadFolder(ContentPipelineTests.ProjectPath(ContentJson.ContentFolder));
            return new InMemoryDefinitionDatabase(
                new BalanceConfig(), content.Actions, content.ExpeditionModels, content.Items, content.LootTables,
                null, content.Enemies, content.Events, content.Npcs,
                new DefinitionExtras { Traits = content.Traits, Facilities = content.Facilities, Lines = content.Lines, Prologue = content.Prologue ?? new PrologueDef(), Endings = content.Endings, Roles = content.Roles });
        }

        private static GameState ShippedRescued(InMemoryDefinitionDatabase db, Action<GameState> setup)
        {
            var state = NewGameFactory.Create(db.Balance, 7);
            state.Calendar.Week = CalendarState.TotalWeeks;
            state.Status = GameStatus.Rescued;
            setup?.Invoke(state);
            return state;
        }

        [Test]
        public void Shipped_EveryCauseHasBadEnding_AndDefaultRescue()
        {
            var db = ShippedDb();
            Assert.GreaterOrEqual(db.Extras.Endings.Count, 15, "문서 8.2 엔딩 전부");
            foreach (var cause in new[] { WeekRunner.EndCauseExpedition, WeekRunner.EndCauseRaid, WeekRunner.EndCauseStarvation, WeekRunner.EndCauseCollapse, WeekRunner.EndCauseHpZero })
            {
                var state = NewGameFactory.Create(db.Balance, 1);
                state.Status = GameStatus.GameOver;
                state.EndCause = cause;
                var result = EndingResolver.Resolve(state, db);
                Assert.IsTrue(result.IsBad, cause);
                Assert.GreaterOrEqual(result.CutKeys.Count, 3, cause);
            }

            var plain = EndingResolver.Resolve(ShippedRescued(db, null), db);
            Assert.AreEqual("ENDING_SURVIVOR", plain.Ending.Id);
            Assert.GreaterOrEqual(plain.CutKeys.Count, 5, "엔딩 3컷 + 선생님·친구 레이어");
        }

        [TestCase("ENDING_DOCTOR")]
        [TestCase("ENDING_ENGINEER")]
        [TestCase("ENDING_GUARD")]
        [TestCase("ENDING_WRITER")]
        [TestCase("ENDING_BALANCED")]
        [TestCase("ENDING_LONE")]
        [TestCase("ENDING_NARR_OVERCOME")]
        [TestCase("ENDING_HIDDEN_CURE")]
        [TestCase("ENDING_HIDDEN_STAY")]
        public void Shipped_EndingIsReachable(string endingId)
        {
            var db = ShippedDb();
            var state = ShippedRescued(db, s =>
            {
                var p = s.Player;
                switch (endingId)
                {
                    case "ENDING_DOCTOR":
                        p.Proficiency[ProficiencyType.Medicine] = new ProficiencyState { Level = 5 };
                        p.Stats[StatType.Intellect] = 80;
                        p.Hidden.Humanity = 50;
                        break;
                    case "ENDING_ENGINEER":
                        p.Proficiency[ProficiencyType.Engineering] = new ProficiencyState { Level = 4 };
                        p.Stats[StatType.Craft] = 70;
                        break;
                    case "ENDING_GUARD":
                        p.Proficiency[ProficiencyType.SelfDefense] = new ProficiencyState { Level = 3 };
                        p.Stats[StatType.Strength] = 70;
                        p.Stats[StatType.Agility] = 60;
                        break;
                    case "ENDING_WRITER":
                        p.Proficiency[ProficiencyType.Humanities] = new ProficiencyState { Level = 4 };
                        for (var w = 1; w <= 45; w++) s.Diary.Add(new DiaryEntry { Week = w, TextKey = "diary.default" });
                        break;
                    case "ENDING_BALANCED":
                        foreach (StatType stat in Enum.GetValues(typeof(StatType))) p.Stats[stat] = 50;
                        break;
                    case "ENDING_LONE":
                        p.Hidden.Isolation = 80;
                        break;
                    case "ENDING_NARR_OVERCOME":
                        p.Hidden.TraumaRecovery = 70;
                        p.Hidden.SelfAwareness = 50;
                        break;
                    case "ENDING_HIDDEN_CURE":
                        s.Flags.Add("TEACHER_CURED");
                        p.Proficiency[ProficiencyType.Medicine] = new ProficiencyState { Level = 5 };
                        p.Stats[StatType.Intellect] = 80;
                        p.Hidden.Humanity = 50;
                        break;
                    case "ENDING_HIDDEN_STAY":
                        s.Flags.Add("CHOSE_TO_STAY");
                        break;
                }
            });

            Assert.AreEqual(endingId, EndingResolver.Resolve(state, db).Ending.Id);
        }
    }
}
