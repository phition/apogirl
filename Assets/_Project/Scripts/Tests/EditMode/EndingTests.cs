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
            CollectionAssert.AreEqual(new[] { "teacher.lesson", "friend.default", "cut.RESCUE" }, result.CutKeys, "구조의 날(선생님 → 친구, 각 하나) → 그 뒤의 나(엔딩 컷)");
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
            state.Player.Vitals.Satiety = -90;
            state.Player.Vitals.Hydration = -90;
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
                new DefinitionExtras { Traits = content.Traits, Facilities = content.Facilities, Lines = content.Lines, Prologue = content.Prologue ?? new PrologueDef(), Endings = content.Endings, Roles = content.Roles, StartBonuses = content.StartBonuses, Factions = content.Factions });
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
            foreach (var cause in new[] { WeekRunner.EndCauseExpedition, WeekRunner.EndCauseRaid, WeekRunner.EndCauseStarvation, WeekRunner.EndCauseCollapse, WeekRunner.EndCauseInfection, WeekRunner.EndCauseHpZero })
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
        [TestCase("ENDING_PARAMEDIC")]
        [TestCase("ENDING_SCHOOL_NURSE")]
        [TestCase("ENDING_CHEF")]
        [TestCase("ENDING_FARMER")]
        [TestCase("ENDING_WATER_SCIENTIST")]
        [TestCase("ENDING_MUSICIAN")]
        [TestCase("ENDING_COUNSELOR")]
        [TestCase("ENDING_NEGOTIATOR")]
        [TestCase("ENDING_ATHLETE")]
        [TestCase("ENDING_JOURNALIST")]
        [TestCase("ENDING_REHAB")]
        [TestCase("ENDING_RECORD_SHOP")]
        [TestCase("ENDING_VET")]
        [TestCase("ENDING_RADIO_HOST")]
        [TestCase("ENDING_ARCHITECT")]
        [TestCase("ENDING_SCHOLAR")]
        [TestCase("ENDING_SOLDIER")]
        [TestCase("ENDING_NIGHT_SCOUT")]
        [TestCase("ENDING_KEEPER")]
        [TestCase("ENDING_SEEKER")]
        public void Shipped_EndingIsReachable(string endingId)
        {
            var db = ShippedDb();
            var state = ShippedRescued(db, s =>
            {
                var p = s.Player;
                switch (endingId)
                {
                    case "ENDING_DOCTOR": // M7-2 기준: 의학 Lv4, 지력 175, 인간성 55
                        p.Proficiency[ProficiencyType.Medicine] = new ProficiencyState { Level = 5 };
                        p.Stats[StatType.Intellect] = 200;
                        p.Hidden.Humanity = 60;
                        break;
                    case "ENDING_ENGINEER":
                        p.Proficiency[ProficiencyType.Engineering] = new ProficiencyState { Level = 4 };
                        p.Stats[StatType.Craft] = 190; // M7-2 기준: 공학 Lv3, 손재주 175
                        break;
                    case "ENDING_GUARD":
                        p.Proficiency[ProficiencyType.SelfDefense] = new ProficiencyState { Level = 3 };
                        p.Stats[StatType.Strength] = 90; // M7-2 기준: 근력 80, 민첩 45, 호신술 Lv1
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

                    // ── P1 새 엔딩 13종 (기획서 3·4절): 문턱을 조금 넘긴 상태 ──
                    case "ENDING_PARAMEDIC":
                        p.Proficiency[ProficiencyType.Medicine] = new ProficiencyState { Level = 3 };
                        p.Stats[StatType.Stamina] = 55;
                        p.Stats[StatType.Agility] = 44;
                        s.Exploration.ActionCounts["ACT_TREAT_WOUND"] = 2;
                        s.Exploration.ActionCounts["ACT_TREAT_INFECTION"] = 1;
                        break;
                    case "ENDING_SCHOOL_NURSE":
                        p.Proficiency[ProficiencyType.Medicine] = new ProficiencyState { Level = 2 };
                        p.Stats[StatType.Empathy] = 105;
                        s.Npcs["NPC_NURSE"] = new NpcState { Status = NpcStatus.Joined, Affinity = 70 };
                        break;
                    case "ENDING_CHEF":
                        p.Proficiency[ProficiencyType.Cooking] = new ProficiencyState { Level = 4 };
                        p.Stats[StatType.Craft] = 150;
                        p.Stats[StatType.Empathy] = 95;
                        s.Exploration.ActionCounts["ACT_EDU_COOKING"] = 12;
                        break;
                    case "ENDING_FARMER":
                        p.Proficiency[ProficiencyType.Survival] = new ProficiencyState { Level = 3 };
                        p.Stats[StatType.Stamina] = 50;
                        s.Flags.Add("GREENHOUSE");
                        break;
                    case "ENDING_WATER_SCIENTIST":
                        p.Proficiency[ProficiencyType.Science] = new ProficiencyState { Level = 3 };
                        p.Stats[StatType.Intellect] = 165;
                        s.Flags.Add("AUTO_PURIFIER");
                        break;
                    case "ENDING_MUSICIAN":
                        p.Proficiency[ProficiencyType.Music] = new ProficiencyState { Level = 3 };
                        p.Stats[StatType.Empathy] = 115;
                        s.Exploration.ActionCounts["ACT_CLUB_SINGING"] = 4;
                        break;
                    case "ENDING_COUNSELOR":
                        p.Proficiency[ProficiencyType.Mind] = new ProficiencyState { Level = 3 };
                        p.Stats[StatType.Empathy] = 115;
                        p.Hidden.TraumaRecovery = 22;
                        s.Exploration.ActionCounts["ACT_NIGHT_TALK"] = 10;
                        break;
                    case "ENDING_NEGOTIATOR":
                        p.Stats[StatType.Leadership] = 85;
                        p.Stats[StatType.Intellect] = 135;
                        foreach (var id in new[] { "FAC_SAGEORI", "FAC_MART", "FAC_UBC" }) s.Factions[id] = new FactionState { Known = true, Standing = 5 };
                        s.Flags.Add("MART_MEDIATED");
                        break;
                    case "ENDING_ATHLETE":
                        p.Stats[StatType.Stamina] = 65;
                        p.Stats[StatType.Agility] = 65;
                        p.Traits.Add("TRAIT_SPRINTER");
                        break;
                    case "ENDING_JOURNALIST":
                        p.Proficiency[ProficiencyType.Humanities] = new ProficiencyState { Level = 2 };
                        p.Stats[StatType.Intellect] = 145;
                        foreach (var clue in new[] { "TRUTH_A", "TRUTH_B", "TRUTH_C", "TRUTH_D" }) s.Clues.Add(clue);
                        break;
                    case "ENDING_REHAB":
                        s.Exploration.ActionCounts["ACT_CARE_TEACHER"] = 25;
                        s.Teacher.Affinity = 85;
                        s.Flags.Add("CRUTCH_DONE");
                        s.Flags.Add("PROSTHETIC_DONE");
                        break;
                    case "ENDING_RECORD_SHOP":
                        s.Flags.Add("CD_SHELF_FULL");
                        s.Exploration.ActionCounts["ACT_BROADCAST_SONG"] = 4;
                        break;
                    case "ENDING_VET":
                        s.Flags.Add("CAT_HOME");
                        s.Flags.Add("CAT_FED_2");
                        p.Stats[StatType.Empathy] = 95;
                        p.Proficiency[ProficiencyType.Medicine] = new ProficiencyState { Level = 1 };
                        break;

                    // ── P2 새 엔딩 7종 (새 판정 종류) ──
                    case "ENDING_RADIO_HOST":
                        p.Stats[StatType.Leadership] = 75;
                        p.Stats[StatType.Empathy] = 105;
                        s.Exploration.ActionCounts["ACT_BROADCAST_INFO"] = 5;
                        s.Exploration.ActionCounts["ACT_BROADCAST_LETTER"] = 3;
                        s.Factions["FAC_UBC"] = new FactionState { Known = true, Standing = 35 };
                        break;
                    case "ENDING_ARCHITECT":
                        p.Proficiency[ProficiencyType.Engineering] = new ProficiencyState { Level = 3 };
                        p.Stats[StatType.Intellect] = 155;
                        OpenFacilities(s, 7);
                        s.Exploration.ActionCounts["ACT_SURV_BARRICADE"] = 2;
                        s.Exploration.ActionCounts["ACT_SURV_REINFORCE"] = 2;
                        break;
                    case "ENDING_SCHOLAR":
                        p.Stats[StatType.Intellect] = 205;
                        ApoGirl.Core.Rules.ExamRules.Record(s, 84, db.Balance);
                        s.Exploration.ActionCounts["ACT_NIGHT_STUDY"] = 10;
                        break;
                    case "ENDING_SOLDIER":
                        s.Exploration.ZombiesFelled = 160;
                        s.Exploration.RaidsRepelled = 3;
                        p.Stats[StatType.Strength] = 50;
                        p.Proficiency[ProficiencyType.SelfDefense] = new ProficiencyState { Level = 2 };
                        p.Conscience = 3;
                        s.Flags.Add("CONSCIENCE_CONFESSED");
                        break;
                    case "ENDING_NIGHT_SCOUT":
                        s.Exploration.NightExplorations = 26;
                        s.Exploration.DayExplorations = 4;
                        s.Exploration.Downs = 1;
                        break;
                    case "ENDING_KEEPER":
                        s.Exploration.DayExplorations = 3;
                        OpenFacilities(s, 7);
                        s.Exploration.ActionCounts["ACT_SURV_CLEAN"] = 10;
                        break;
                    case "ENDING_SEEKER":
                        foreach (var flag in new[] { "DH_RESCUED", "BULLY_RESCUED", "JUN_SAVED", "WANDERER_SHELTERED" }) s.Flags.Add(flag);
                        break;
                }
            });

            // 세 막 엔딩: 그 엔딩이 제 막의 조각으로 뽑히면 된다 (구조 장면·회고 축·미래)
            var result = EndingResolver.Resolve(state, db);
            Assert.IsTrue(result.Acts.Exists(act => act.Piece.Id == endingId), endingId + " ← " + string.Join(", ", result.Acts.ConvertAll(act => act.Piece.Id)));
        }

        // ── 세 막 엔딩 (Docs/PROPOSAL_ENDINGS_EXPANSION 7절): 구조의 날 → 돌아보는 밤 → 그 뒤의 나 ──

        [Test]
        public void Acts_PlainRescue_PicksOnePiecePerAct_AndBridges()
        {
            var db = ShippedDb();
            // 동료가 한 명 있는 평범한 판 (동료가 없으면 구조 장면이 "혼자 선 옥상"이 된다)
            var result = EndingResolver.Resolve(ShippedRescued(db, s => s.Npcs["NPC_DOHYEON"] = new NpcState { Status = NpcStatus.Joined }), db);
            Assert.AreEqual("ENDING_RESCUE_MORNING", result.PieceId(EndingAct.Rescue));
            Assert.AreEqual("ENDING_RESCUE_ALONE", EndingResolver.Resolve(ShippedRescued(db, null), db).PieceId(EndingAct.Rescue), "동료가 없으면 혼자 선 옥상");
            Assert.AreEqual("ENDING_REFLECT_DEFAULT", result.PieceId(EndingAct.Reflection));
            Assert.AreEqual("ENDING_SURVIVOR", result.PieceId(EndingAct.Future));
            Assert.AreEqual("ENDING_SURVIVOR", result.Ending.Id, "대표 엔딩은 그 뒤의 나");

            // 화면 차례: 막 제목 셋, 그 사이의 컷. 구조 장면 컷이 맨 앞, 미래 본편 컷이 맨 뒤
            Assert.AreEqual(3, result.Pages.FindAll(page => page.Kind == EndingPageKind.ActTitle).Count);
            Assert.AreEqual(EndingPageKind.ActTitle, result.Pages[0].Kind);
            Assert.AreEqual("ending.ENDING_SURVIVOR.cut1", result.CutKeys[0]);
            Assert.AreEqual("ending.ENDING_SURVIVOR.cut3", result.CutKeys[result.CutKeys.Count - 1]);
            CollectionAssert.Contains(result.CutKeys, "ending.ENDING_REFLECT_DEFAULT.cut1");
            // 다리 문장: 선생님이 남았으니 "손을 흔드는 사람이 점이 될 때까지", 그리고 시간 건너뛰기 없는 기본 다리
            CollectionAssert.Contains(result.CutKeys, "line.bridge_left");
            CollectionAssert.Contains(result.CutKeys, "line.bridge_future");
            Assert.Less(result.CutKeys.IndexOf("line.bridge_left"), result.CutKeys.IndexOf("ending.ENDING_REFLECT_DEFAULT.cut1"));
            Assert.Less(result.CutKeys.IndexOf("ending.ENDING_REFLECT_DEFAULT.cut1"), result.CutKeys.IndexOf("line.bridge_future"));
        }

        [Test]
        public void Acts_ChoseToStay_SkipsFuture_AndKeepsTheLastLesson()
        {
            var db = ShippedDb();
            var result = EndingResolver.Resolve(ShippedRescued(db, s =>
            {
                s.Flags.Add("CHOSE_TO_STAY");
                // 직업 조건을 채워도 남기로 한 판은 미래를 판정하지 않는다
                s.Player.Proficiency[ProficiencyType.SelfDefense] = new ProficiencyState { Level = 3 };
                s.Player.Stats[StatType.Strength] = 90;
                s.Player.Stats[StatType.Agility] = 60;
            }), db);
            Assert.AreEqual("ENDING_HIDDEN_STAY", result.PieceId(EndingAct.Rescue));
            Assert.AreEqual("", result.PieceId(EndingAct.Future));
            Assert.AreEqual("ENDING_HIDDEN_STAY", result.Ending.Id);
            Assert.AreEqual("ending.ENDING_HIDDEN_STAY.cut3", result.CutKeys[result.CutKeys.Count - 1], "두 사람의 수업이 계속되었다");
            CollectionAssert.Contains(result.CutKeys, "line.bridge_stay");
            CollectionAssert.Contains(result.CutKeys, "line.bridge_future_stay");
        }

        [Test]
        public void Acts_ReflectionCodaClosesTheFuture_AndLaterActsReadEarlierOnes()
        {
            var db = ShippedDb();
            // 아직 닫힌 문 + 직업 없음: 회고 꼬리(10년 후 상담실 문)가 맨 끝, 다리 문장도 그 축의 것
            var result = EndingResolver.Resolve(ShippedRescued(db, s =>
            {
                s.Player.Hidden.TraumaRecovery = 0;
                s.Player.Hidden.Isolation = 40;
            }), db);
            Assert.AreEqual("ENDING_NARR_UNHEALED", result.PieceId(EndingAct.Reflection));
            Assert.AreEqual("ending.ENDING_NARR_UNHEALED.cut3", result.CutKeys[result.CutKeys.Count - 1]);
            CollectionAssert.Contains(result.CutKeys, "line.bridge_future_unhealed", "뒤 막의 조건이 앞 막의 결과를 읽는다 (ValueKind.EndingAct)");
            CollectionAssert.DoesNotContain(result.CutKeys, "line.bridge_future");

            // 판정 밖에서는 EndingAct가 늘 0이다 (이벤트 조건에 잘못 써도 켜지지 않는다)
            Assert.AreEqual(0, ConditionEvaluator.ValueOf(ShippedRescued(db, null), ValueKind.EndingAct, "ENDING_NARR_UNHEALED"));
        }

        [Test]
        public void Acts_RosterShowsTopLines_RestGoToTheRecord()
        {
            var lines = new List<LineDef>();
            for (var i = 0; i < 9; i++)
            {
                lines.Add(new LineDef { Id = "E" + i, Kind = LineKind.EndingEpilogue, Group = "G" + i, Priority = i, TextKey = "ep." + i });
            }

            lines.Add(new LineDef { Id = "MIND", Kind = LineKind.EndingEpilogue, Group = "MIND", TextKey = "ep.mind" });
            var db = Db(new[] { Ending("RESCUE", EndingCategory.Normal, 100) }, lines.ToArray());
            var result = EndingResolver.Resolve(Ended(db, GameStatus.Rescued), db);

            Assert.AreEqual(db.Balance.EndingRosterMax, result.EpilogueKeys.Count);
            CollectionAssert.AreEqual(new[] { "ep.3", "ep.4", "ep.5", "ep.6", "ep.7", "ep.8" }, result.EpilogueKeys, "우선순위가 높은 줄부터, 차례는 정의 순서");
            CollectionAssert.AreEqual(new[] { "ep.0", "ep.1", "ep.2" }, result.MoreEpilogueKeys, "나머지는 1년의 기록에");
            var roster = result.Pages.Find(page => page.Kind == EndingPageKind.Roster);
            Assert.AreEqual(6, roster.Lines.Count);
            CollectionAssert.Contains(result.CutKeys, "ep.mind", "마음 상태 줄은 명단이 아니라 돌아보는 밤에 놓인다");
            CollectionAssert.DoesNotContain(result.EpilogueKeys, "ep.mind");
        }

        [Test]
        public void Acts_BoardedAndLeftBehind_ReadTheSeatChoice()
        {
            var db = ShippedDb();
            var state = ShippedRescued(db, s =>
            {
                s.Flags.Add("TEACHER_CURED");
                s.Npcs["NPC_NURSE"] = new NpcState { Status = NpcStatus.Joined };
            });
            Assert.AreEqual(1, ConditionEvaluator.ValueOf(state, ValueKind.Boarded, "NPC_NURSE"));
            Assert.AreEqual(1, ConditionEvaluator.ValueOf(state, ValueKind.Boarded, "NPC_TEACHER"));
            Assert.AreEqual(0, ConditionEvaluator.ValueOf(state, ValueKind.LeftBehind, ""));

            state.Flags.Add("SEAT_STAY_TEACHERS");
            Assert.AreEqual(0, ConditionEvaluator.ValueOf(state, ValueKind.Boarded, "NPC_NURSE"), "두 선생님이 남았다");
            Assert.AreEqual(2, ConditionEvaluator.ValueOf(state, ValueKind.LeftBehind, ""));
        }

        // ── P0.5 점수: 같은 우선순위끼리는 '문턱을 넘은 정도'로 겨룬다 (기획서 9.2절 E3) ──

        [Test]
        public void Margin_ComparesByHowFarPastTheBar_NotByRawNumbers()
        {
            // 지력은 190대, 근력은 35대로 자란다 — 점수 합으로는 지력을 쓰는 엔딩이 늘 이긴다
            var scholar = Ending("SCHOLAR", EndingCategory.Specialized, 300, StatAtLeast(StatType.Intellect, 190));
            scholar.Score.Add(new EndingScoreTerm { Kind = ValueKind.Stat, Key = "Intellect" });
            var guard = Ending("GUARD", EndingCategory.Specialized, 300, StatAtLeast(StatType.Strength, 35));
            guard.Score.Add(new EndingScoreTerm { Kind = ValueKind.Stat, Key = "Strength" });
            var db = Db(new[] { Ending("NORMAL", EndingCategory.Normal, 100), scholar, guard });

            var state = Ended(db, GameStatus.Rescued);
            state.Player.Stats[StatType.Intellect] = 200;   // 문턱의 1.05배
            state.Player.Stats[StatType.Strength] = 60;     // 문턱의 1.71배 (점수 합은 200 > 60)
            var result = EndingResolver.Resolve(state, db);
            Assert.AreEqual("GUARD", result.Ending.Id, "문턱을 더 많이 넘은 쪽");
            Assert.AreEqual(60f / 35f, result.Of(EndingAct.Future).Margin, 0.001f);

            state.Player.Stats[StatType.Strength] = 500;
            Assert.AreEqual(2f, EndingResolver.Margin(state, guard, db.Balance.EndingMarginCap), 0.001f, "한 조건이 넘은 정도는 상한 2");

            // 넘은 정도가 같으면 점수 항목이 가른다
            state.Player.Stats[StatType.Intellect] = 380;
            state.Player.Stats[StatType.Strength] = 70;
            Assert.AreEqual("SCHOLAR", EndingResolver.Resolve(state, db).Ending.Id, "둘 다 2배 → 점수 합 (380 > 70)");
        }

        [Test]
        public void Margin_SkipsYesNoAndAtMostChecks_AndCountsAnyGroupOnce()
        {
            var ending = Ending("X", EndingCategory.Specialized, 300, new ConditionDef
            {
                Checks =
                {
                    new ValueCheck(ValueKind.Stat, "Stamina", CompareOp.GreaterOrEqual, 50),
                    new ValueCheck(ValueKind.Flag, "DONE", CompareOp.GreaterOrEqual, 1),          // 예/아니오 → 빼고
                    new ValueCheck(ValueKind.Companions, "", CompareOp.LessOrEqual, 3),            // '이하' → 빼고
                },
                AnyChecks =
                {
                    new ValueCheck(ValueKind.Trait, "TRAIT_SPRINTER", CompareOp.GreaterOrEqual, 1),
                    new ValueCheck(ValueKind.ActionCount, "ACT_CLUB_SPORTS", CompareOp.GreaterOrEqual, 6),
                },
            });
            var db = Db(new[] { ending });
            var state = Ended(db, GameStatus.Rescued);
            state.Player.Stats[StatType.Stamina] = 75;                                  // 1.5
            state.Player.Traits.Add("TRAIT_SPRINTER");                                  // 묶음은 특성으로 채움 → 1
            Assert.AreEqual((1.5f + 1f) / 2f, EndingResolver.Margin(state, ending, 2f), 0.001f);

            state.Exploration.ActionCounts["ACT_CLUB_SPORTS"] = 9;                      // 묶음에서 가장 많이 넘은 것 1.5
            Assert.AreEqual(1.5f, EndingResolver.Margin(state, ending, 2f), 0.001f);
            Assert.AreEqual(1f, EndingResolver.Margin(state, Ending("PLAIN", EndingCategory.Normal, 100), 2f), "셀 조건이 없으면 1");
        }

        [Test]
        public void ActionCountPrefix_SumsActionsStartingWithTheKey()
        {
            var db = ShippedDb();
            var state = ShippedRescued(db, s =>
            {
                s.Exploration.ActionCounts["ACT_TREAT_WOUND"] = 2;
                s.Exploration.ActionCounts["ACT_TREAT_INFECTION"] = 3;
                s.Exploration.ActionCounts["ACT_EDU_COOKING"] = 7;
            });
            Assert.AreEqual(5, ConditionEvaluator.ValueOf(state, ValueKind.ActionCountPrefix, "ACT_TREAT_"));
            Assert.AreEqual(0, ConditionEvaluator.ValueOf(state, ValueKind.ActionCountPrefix, ""));
        }

        private static readonly string[] FacilityIds = { "STORAGE", "CLASSROOM", "INFIRMARY", "ROOFTOP", "BROADCAST", "CAFETERIA", "LIBRARY", "SCIENCE", "GYM", "STAFFROOM" };

        /// <summary>시설을 앞에서부터 count곳만 연 상태로 만든다.</summary>
        private static void OpenFacilities(GameState state, int count)
        {
            for (var i = 0; i < FacilityIds.Length; i++)
            {
                state.Base.Facilities[FacilityIds[i]] = i < count ? 1 : 0;
            }
        }

        // ── P2 판정 종류 (기획서 8.1절) ──

        [Test]
        public void Record_ReadsTheYearsNumbers()
        {
            var db = ShippedDb();
            var state = ShippedRescued(db, s =>
            {
                s.Exploration.Downs = 2;
                s.Exploration.RaidsRepelled = 4;
                s.Exploration.ZombiesFelled = 37;
                s.CdWeeks["CD_01"] = 5;
                s.CdWeeks["CD_03"] = 9;
            });
            Assert.AreEqual(2, ConditionEvaluator.ValueOf(state, ValueKind.Record, "Downs"));
            Assert.AreEqual(4, ConditionEvaluator.ValueOf(state, ValueKind.Record, "RaidsRepelled"));
            Assert.AreEqual(37, ConditionEvaluator.ValueOf(state, ValueKind.Record, "ZombiesFelled"));
            Assert.AreEqual(2, ConditionEvaluator.ValueOf(state, ValueKind.Record, "CdCount"));
            foreach (var key in ConditionEvaluator.RecordKeys)
            {
                Assert.IsTrue(ConditionEvaluator.IsValidKey(ValueKind.Record, key), key);
                Assert.DoesNotThrow(() => ConditionEvaluator.ValueOf(state, ValueKind.Record, key), key);
            }

            Assert.IsFalse(ConditionEvaluator.IsValidKey(ValueKind.Record, "Nope"));
            Assert.Throws<ArgumentException>(() => ConditionEvaluator.ValueOf(state, ValueKind.Record, "Nope"));
        }

        [Test]
        public void FlagCount_FacilityCount_VisitedLocations_AndGroupedActionCounts()
        {
            var db = ShippedDb();
            var state = ShippedRescued(db, s =>
            {
                s.Flags.Add("DH_RESCUED");
                s.Flags.Add("JUN_SAVED");
                OpenFacilities(s, 4);
                s.Exploration.ActionCounts["ACT_EXP_TUTORIAL"] = 1;
                s.Exploration.ActionCounts["ACT_EXP_DAY_STORE"] = 3;
                s.Exploration.ActionCounts["ACT_EXP_NIGHT_STORE"] = 2;
                s.Exploration.ActionCounts["ACT_EXP_NIGHT_SUBWAY"] = 1;
                s.Exploration.ActionCounts["ACT_EXP_DAY_MART"] = 0;
                s.Exploration.ActionCounts["ACT_SURV_BARRICADE"] = 2;
                s.Exploration.ActionCounts["ACT_SURV_REINFORCE"] = 3;
                s.Exploration.ActionCounts["ACT_SURV_CLEAN"] = 9;
            });
            Assert.AreEqual(2, ConditionEvaluator.ValueOf(state, ValueKind.FlagCount, "DH_RESCUED|BULLY_RESCUED|JUN_SAVED"));
            Assert.AreEqual(0, ConditionEvaluator.ValueOf(state, ValueKind.FlagCount, ""));
            Assert.AreEqual(4, ConditionEvaluator.ValueOf(state, ValueKind.FacilityCount, ""));
            Assert.AreEqual(2, ConditionEvaluator.ValueOf(state, ValueKind.VisitedLocations, ""), "편의점(낮·밤은 한 곳)과 지하철 — 튜토리얼과 0번 간 곳은 빼고");
            Assert.AreEqual(5, ConditionEvaluator.ValueOf(state, ValueKind.ActionCountPrefix, "ACT_SURV_BARRICADE|ACT_SURV_REINFORCE"));
            Assert.AreEqual(14, ConditionEvaluator.ValueOf(state, ValueKind.ActionCountPrefix, "ACT_SURV_|ACT_SURV_CLEAN"), "행동 하나는 한 번만 센다");
        }

        [Test]
        public void Exam_HighScoresLeaveFlags()
        {
            var db = ShippedDb();
            var state = ShippedRescued(db, null);
            ApoGirl.Core.Rules.ExamRules.Record(state, -1, db.Balance);
            Assert.IsFalse(state.Flags.Contains("EXAM_TAKEN"), "안 봤으면 아무것도 남지 않는다");

            ApoGirl.Core.Rules.ExamRules.Record(state, 60, db.Balance);
            Assert.IsTrue(state.Flags.Contains("EXAM_TAKEN"));
            Assert.IsFalse(state.Flags.Contains("EXAM_SCORE_80"));

            ApoGirl.Core.Rules.ExamRules.Record(state, 80, db.Balance);
            Assert.IsTrue(state.Flags.Contains("EXAM_SCORE_80"));
            Assert.IsFalse(state.Flags.Contains("EXAM_SCORE_95"));

            ApoGirl.Core.Rules.ExamRules.Record(state, 96, db.Balance);
            Assert.IsTrue(state.Flags.Contains("EXAM_SCORE_95"));
        }

        // ── P2 새 엔딩: 조건과 변형 ──

        [Test]
        public void Soldier_NeedsTheWeightSpokenAndCarried_NotJustTheCount()
        {
            var db = ShippedDb();
            var state = ShippedRescued(db, s =>
            {
                s.Exploration.ZombiesFelled = 300;
                s.Exploration.RaidsRepelled = 5;
                s.Player.Stats[StatType.Strength] = 60;
                s.Player.Proficiency[ProficiencyType.SelfDefense] = new ProficiencyState { Level = 3 };
                s.Player.Conscience = 3;
            });
            Assert.AreNotEqual("ENDING_SOLDIER", EndingResolver.Resolve(state, db).Ending.Id, "쓰러뜨린 수만으로는 열리지 않는다 (고해 없음)");

            state.Flags.Add("CONSCIENCE_CONFESSED");
            Assert.AreEqual("ENDING_SOLDIER", EndingResolver.Resolve(state, db).Ending.Id);

            state.Player.Conscience = 7;
            Assert.AreNotEqual("ENDING_SOLDIER", EndingResolver.Resolve(state, db).Ending.Id, "지금 무게가 6을 넘으면 아니다");
        }

        [Test]
        public void RadioHostAndSeeker_VariantsFollowWhatWasFound()
        {
            var db = ShippedDb();
            var host = ShippedRescued(db, s =>
            {
                s.Player.Stats[StatType.Leadership] = 75;
                s.Player.Stats[StatType.Empathy] = 105;
                s.Exploration.ActionCounts["ACT_BROADCAST_INFO"] = 8;
                s.Factions["FAC_UBC"] = new FactionState { Known = true, Standing = 35 };
            });
            CollectionAssert.Contains(EndingResolver.Resolve(host, db).CutKeys, "ending.ENDING_RADIO_HOST.cut3", "CD 없이 방송한 판");
            host.CdWeeks["CD_02"] = 20;
            CollectionAssert.Contains(EndingResolver.Resolve(host, db).CutKeys, "ending.ENDING_RADIO_HOST.cd.cut3");

            var seeker = ShippedRescued(db, s =>
            {
                foreach (var flag in new[] { "DH_RESCUED", "BULLY_RESCUED", "JUN_SAVED", "WANDERER_SHELTERED" }) s.Flags.Add(flag);
            });
            CollectionAssert.Contains(EndingResolver.Resolve(seeker, db).CutKeys, "ending.ENDING_SEEKER.cut3");
            seeker.Flags.Add("DH_MOTHER_FOUND");
            CollectionAssert.Contains(EndingResolver.Resolve(seeker, db).CutKeys, "ending.ENDING_SEEKER.mother.cut3");
        }

        [Test]
        public void KeeperAndNightScout_AreAboutHowTheYearWasPlayed()
        {
            var db = ShippedDb();
            var keeper = ShippedRescued(db, s =>
            {
                s.Exploration.DayExplorations = 6;
                OpenFacilities(s, 8);
                s.Exploration.ActionCounts["ACT_SURV_CLEAN"] = 12;
            });
            Assert.AreEqual("ENDING_KEEPER", EndingResolver.Resolve(keeper, db).Ending.Id);
            keeper.Exploration.DayExplorations = 7;
            Assert.AreNotEqual("ENDING_KEEPER", EndingResolver.Resolve(keeper, db).Ending.Id, "탐험 7번이면 학교에 머문 판이 아니다");

            var scout = ShippedRescued(db, s =>
            {
                s.Exploration.NightExplorations = 30;
                s.Exploration.DayExplorations = 5;
            });
            Assert.AreEqual("ENDING_NIGHT_SCOUT", EndingResolver.Resolve(scout, db).Ending.Id);
            scout.Exploration.Downs = 2;
            Assert.AreNotEqual("ENDING_NIGHT_SCOUT", EndingResolver.Resolve(scout, db).Ending.Id, "두 번 쓰러졌으면 아니다");
        }

        // ── P1 새 엔딩: 막을 넘는 조건과 변형 ──

        [Test]
        public void SchoolNurse_NeedsTheNurseOnTheHelicopter()
        {
            var db = ShippedDb();
            var state = ShippedRescued(db, s =>
            {
                s.Player.Proficiency[ProficiencyType.Medicine] = new ProficiencyState { Level = 2 };
                s.Player.Stats[StatType.Empathy] = 105;
                s.Npcs["NPC_NURSE"] = new NpcState { Status = NpcStatus.Joined, Affinity = 70 };
                s.Flags.Add("TEACHER_CURED");
            });
            Assert.AreEqual("ENDING_SCHOOL_NURSE", EndingResolver.Resolve(state, db).PieceId(EndingAct.Future));

            state.Flags.Add("SEAT_STAY_TEACHERS");
            Assert.AreNotEqual("ENDING_SCHOOL_NURSE", EndingResolver.Resolve(state, db).PieceId(EndingAct.Future), "박 선생님이 남았으면 이 길은 없다");
        }

        [Test]
        public void Rehab_DoesNotNeedTheCure_AndTheLastCutFollowsTheTeacher()
        {
            var db = ShippedDb();
            var state = ShippedRescued(db, s =>
            {
                s.Exploration.ActionCounts["ACT_CARE_TEACHER"] = 30;
                s.Teacher.Affinity = 90;
                s.Flags.Add("CRUTCH_DONE");
                s.Flags.Add("PROSTHETIC_DONE");
            });
            var stayed = EndingResolver.Resolve(state, db);
            Assert.AreEqual("ENDING_REHAB", stayed.Ending.Id, "완치하지 못한 판에서도 나온다");
            Assert.AreEqual(EndingCategory.Record, stayed.Ending.Category);
            CollectionAssert.Contains(stayed.CutKeys, "ending.ENDING_REHAB.cut3", "선생님이 남은 판의 마지막 컷");

            state.Flags.Add("TEACHER_CURED");
            var cured = EndingResolver.Resolve(state, db);
            Assert.AreEqual("ENDING_REHAB", cured.Ending.Id);
            Assert.AreEqual("ENDING_HIDDEN_CURE", cured.PieceId(EndingAct.Rescue), "완치는 구조 장면으로 함께 나온다");
            CollectionAssert.Contains(cured.CutKeys, "ending.ENDING_REHAB.cured.cut3");
            CollectionAssert.DoesNotContain(cured.CutKeys, "ending.ENDING_REHAB.cut3");

            state.Flags.Remove("TEACHER_CURED");
            state.Flags.Add("TEACHER_LEFT_BEHIND");
            Assert.AreNotEqual("ENDING_REHAB", EndingResolver.Resolve(state, db).Ending.Id, "선생님을 두고 돌아선 판은 아니다");
        }

        [Test]
        public void Counselor_ConfessionChangesACut_ButIsNotRequired()
        {
            var db = ShippedDb();
            var state = ShippedRescued(db, s =>
            {
                s.Player.Proficiency[ProficiencyType.Mind] = new ProficiencyState { Level = 3 };
                s.Player.Stats[StatType.Empathy] = 115;
                s.Player.Hidden.TraumaRecovery = 22;
                s.Exploration.ActionCounts["ACT_NIGHT_TALK"] = 10;
            });
            var plain = EndingResolver.Resolve(state, db);
            Assert.AreEqual("ENDING_COUNSELOR", plain.Ending.Id, "고해 장면이 없었던 판도 상담사가 될 수 있다");
            CollectionAssert.Contains(plain.CutKeys, "ending.ENDING_COUNSELOR.cut2");

            state.Flags.Add("CONFESS_AFRAID");
            var afraid = EndingResolver.Resolve(state, db);
            CollectionAssert.Contains(afraid.CutKeys, "ending.ENDING_COUNSELOR.afraid.cut2");
            CollectionAssert.DoesNotContain(afraid.CutKeys, "ending.ENDING_COUNSELOR.cut2");
        }

        [Test]
        public void Shipped_NewEndingTexts_AllExist()
        {
            var db = ShippedDb();
            var ui = (JObject)JObject.Parse(File.ReadAllText("Assets/_Project/Localization/Source/ko.json"))["UI"];
            foreach (var ending in db.Extras.Endings)
            {
                Assert.IsNotNull(ui[ending.TitleKey], ending.TitleKey);
                foreach (var key in ending.CutKeys.Concat(ending.CodaKeys).Concat(ending.FutureKeys).Concat(ending.TailKeys)
                             .Concat(ending.Variants.SelectMany(v => v.CutKeys.Concat(v.CodaKeys))))
                {
                    Assert.IsNotNull(ui[key], ending.Id + ": " + key);
                }

                Assert.IsNotNull(ui["ending.cat." + ending.Category], "구분 문구: " + ending.Category);
            }
        }
    }
}
