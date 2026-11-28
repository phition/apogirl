using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Endings;
using ApoGirl.Core.Events;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using ApoGirl.Systems.Content;
using ApoGirl.Systems.Save;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>
    /// 엔딩 P3 (Docs/PROPOSAL_ENDINGS_EXPANSION 8.2절·12.5절): 1년의 기록(GameState.Records, 세이브 v17)을 세는 규칙과
    /// 그 기록을 읽는 엔딩 7종, 기존 엔딩 수정 3종.
    /// </summary>
    public sealed class YearRecordTests
    {
        private static InMemoryDefinitionDatabase ShippedDb()
        {
            var content = ContentJson.LoadFolder(ContentPipelineTests.ProjectPath(ContentJson.ContentFolder));
            return new InMemoryDefinitionDatabase(
                new BalanceConfig(), content.Actions, content.ExpeditionModels, content.Items, content.LootTables,
                null, content.Enemies, content.Events, content.Npcs,
                new DefinitionExtras { Traits = content.Traits, Facilities = content.Facilities, Lines = content.Lines, Prologue = content.Prologue ?? new PrologueDef(), Endings = content.Endings, Roles = content.Roles, StartBonuses = content.StartBonuses, Factions = content.Factions });
        }

        private static GameState Rescued(InMemoryDefinitionDatabase db, System.Action<GameState> setup = null)
        {
            var state = NewGameFactory.Create(db.Balance, 7);
            state.Calendar.Week = CalendarState.TotalWeeks;
            state.Status = GameStatus.Rescued;
            setup?.Invoke(state);
            return state;
        }

        // ── 세는 규칙 ──

        [Test]
        public void ConscienceRaised_CountsOnlyWhatRose()
        {
            var db = TestContent.CreateDatabase();
            var state = TestContent.NewGame(db);
            var delta = new StateDelta();
            EffectApplier.ChangeConscience(state, 2, db.Balance, delta);
            EffectApplier.ChangeConscience(state, -1, db.Balance, delta);
            EffectApplier.ChangeConscience(state, 3, db.Balance, delta);
            Assert.AreEqual(4, state.Player.Conscience);
            Assert.AreEqual(5, state.Records.ConscienceRaised, "덜어진 것은 빼지 않는다");

            // 상한에 막혀 실제로 오르지 않은 만큼은 세지 않는다
            EffectApplier.ChangeConscience(state, 100, db.Balance, delta);
            Assert.AreEqual(5 + (db.Balance.ConscienceMax - 4), state.Records.ConscienceRaised);
        }

        [Test]
        public void WeekEnd_CountsHighStressAndEmptyPantryWeeks()
        {
            var db = TestContent.CreateDatabase();
            var state = TestContent.NewGame(db);
            state.Player.Vitals.Stress = db.Balance.RecordHighStress - 1;
            state.Base.SetResource(ResourceType.Food, 3);
            RecordRules.WeekEnd(state, db.Balance);
            Assert.AreEqual(0, state.Records.HighStressWeeks);
            Assert.AreEqual(0, state.Records.HungryWeeks);

            state.Player.Vitals.Stress = db.Balance.RecordHighStress;
            state.Base.SetResource(ResourceType.Food, 0);
            RecordRules.WeekEnd(state, db.Balance);
            RecordRules.WeekEnd(state, db.Balance);
            Assert.AreEqual(2, state.Records.HighStressWeeks);
            Assert.AreEqual(2, state.Records.HungryWeeks);
        }

        [Test]
        public void Expedition_QuietRunsNeedASafeReturn_AndDeepVisitsAreCountedOncePerPlace()
        {
            var db = TestContent.CreateDatabase();
            var state = TestContent.NewGame(db);
            RecordRules.ApplyExpedition(state, "LOC_A", new ExpeditionResult { Status = ExtractStatus.Extracted, Undetected = true, EnteredDeep = true });
            RecordRules.ApplyExpedition(state, "LOC_A", new ExpeditionResult { Status = ExtractStatus.Extracted, Undetected = false, EnteredDeep = true });
            RecordRules.ApplyExpedition(state, "LOC_B", new ExpeditionResult { Status = ExtractStatus.Down, Undetected = true, EnteredDeep = true });
            RecordRules.ApplyExpedition(state, "LOC_C", new ExpeditionResult { Status = ExtractStatus.Extracted, Undetected = true });
            Assert.AreEqual(2, state.Records.QuietRuns, "쓰러진 탐험은 들키지 않았어도 세지 않는다");
            CollectionAssert.AreEquivalent(new[] { "LOC_A", "LOC_B" }, state.Records.DeepVisited);
            Assert.AreEqual(2, ConditionEvaluator.ValueOf(state, ValueKind.Record, "DeepVisited"));
        }

        [Test]
        public void Bbs_CountsOneSessionPerWeek()
        {
            var db = TestContent.CreateDatabase();
            var state = TestContent.NewGame(db);
            state.Calendar.Week = 20;
            Assert.IsTrue(RecordRules.BbsConnected(state));
            Assert.IsFalse(RecordRules.BbsConnected(state), "같은 주에 다시 켜도 한 번");
            state.Calendar.Week = 21;
            Assert.IsTrue(RecordRules.BbsConnected(state));
            Assert.AreEqual(2, state.Records.BbsSessions);
        }

        [Test]
        public void Injury_And_Trade_AreCounted()
        {
            var db = TestContent.CreateDatabase();
            var state = TestContent.NewGame(db);
            RecordRules.Injured(state);
            RecordRules.Traded(state);
            RecordRules.Traded(state);
            Assert.AreEqual(1, ConditionEvaluator.ValueOf(state, ValueKind.Record, "InjuryCount"));
            Assert.AreEqual(2, ConditionEvaluator.ValueOf(state, ValueKind.Record, "TradeCount"));
            foreach (var key in ConditionEvaluator.RecordKeys)
            {
                Assert.DoesNotThrow(() => ConditionEvaluator.ValueOf(state, ValueKind.Record, key), key);
            }
        }

        [Test]
        public void Headless_QuietIsDeterministic_NeverAfterAHeavyFight_AndOnlyOnASafeReturn()
        {
            var db = TestContent.CreateDatabase();
            ExpeditionResult Run(int seed, ExpeditionTime time) => new HeadlessExpeditionRunner(db).Run(new ExpeditionRequest
            {
                LocationId = TestContent.Location, Week = 3, Seed = seed, TimeOfDay = time, Hp = 100,
                Stats = { [StatType.Agility] = 60 },
            });

            var quiet = 0;
            for (var seed = 1; seed <= 60; seed++)
            {
                foreach (var time in new[] { ExpeditionTime.Day, ExpeditionTime.Night })
                {
                    var a = Run(seed, time);
                    Assert.AreEqual(a.Undetected, Run(seed, time).Undetected, "같은 시드면 같은 기록");
                    if (a.Undetected)
                    {
                        quiet++;
                        Assert.AreEqual(ExtractStatus.Extracted, a.Status);
                        Assert.Less(a.ZombiesFelled, db.Balance.ConscienceFelledThreshold);
                    }
                }
            }

            Assert.Greater(quiet, 0, "민첩 60이면 들키지 않은 탐험이 나온다");
            Assert.Less(quiet, 120, "모든 탐험이 조용하지는 않다");
        }

        [Test]
        public void SaveV16_MigratesToV17_WithEmptyRecords()
        {
            var serializer = new GameStateSerializer();
            var db = TestContent.CreateDatabase();
            var root = JObject.Parse(serializer.Serialize(TestContent.NewGame(db)));
            root.Remove("Records");
            root["SaveVersion"] = 16;
            var loaded = serializer.Deserialize(root.ToString());
            Assert.AreEqual(GameState.CurrentSaveVersion, loaded.SaveVersion);
            Assert.IsNotNull(loaded.Records);
            Assert.AreEqual(0, loaded.Records.QuietRuns);
            Assert.IsNotNull(loaded.Records.DeepVisited);

            // 기록은 저장을 거쳐도 남는다
            loaded.Records.QuietRuns = 7;
            loaded.Records.DeepVisited.Add("LOC_SUBWAY");
            var again = serializer.Deserialize(serializer.Serialize(loaded));
            Assert.AreEqual(7, again.Records.QuietRuns);
            CollectionAssert.Contains(again.Records.DeepVisited, "LOC_SUBWAY");
        }

        // ── 기록을 읽는 엔딩 ──

        [TestCase("ENDING_NETWORK")]
        [TestCase("ENDING_SHADOW")]
        [TestCase("ENDING_UNSCATHED")]
        [TestCase("ENDING_CARTOGRAPHER")]
        [TestCase("ENDING_MERCHANT")]
        [TestCase("ENDING_RATIONER")]
        [TestCase("ENDING_NARR_BURDEN")]
        public void Shipped_RecordEndingIsReachable(string endingId)
        {
            var db = ShippedDb();
            var state = Rescued(db, s =>
            {
                var p = s.Player;
                switch (endingId)
                {
                    case "ENDING_NETWORK":
                        p.Proficiency[ProficiencyType.Engineering] = new ProficiencyState { Level = 2 };
                        p.Stats[StatType.Intellect] = 155;
                        s.Flags.Add("BBS_KNOWN");
                        s.Flags.Add("POWER_RESTORED");
                        s.Records.BbsSessions = 6;
                        break;
                    case "ENDING_SHADOW":
                        s.Records.QuietRuns = 20;
                        s.Exploration.ZombiesFelled = 12;
                        p.Stats[StatType.Agility] = 55;
                        break;
                    case "ENDING_UNSCATHED":
                        s.Exploration.DayExplorations = 24;
                        s.Exploration.NightExplorations = 8;
                        s.Records.InjuryCount = 1;
                        break;
                    case "ENDING_CARTOGRAPHER":
                        foreach (var id in new[] { "SCHOOL", "STORE", "PHARMACY", "RESIDENTIAL", "POLICE", "SUBWAY", "MART", "WATERWORKS" })
                        {
                            s.Exploration.ActionCounts["ACT_EXP_DAY_" + id] = 1;
                        }

                        foreach (var id in new[] { "LOC_SUBWAY", "LOC_MART", "LOC_POLICE", "LOC_WATERWORKS" }) s.Records.DeepVisited.Add(id);
                        break;
                    case "ENDING_MERCHANT":
                        s.Records.TradeCount = 12;
                        s.Factions["FAC_MART"] = new FactionState { Known = true, Standing = 35 };
                        break;
                    case "ENDING_RATIONER":
                        s.Records.HungryWeeks = 4;
                        s.Flags.Add("RATION_EQUAL");
                        p.Hidden.Humanity = 65;
                        break;
                    case "ENDING_NARR_BURDEN":
                        s.Flags.Add("CONSCIENCE_CONFESSED");
                        s.Records.ConscienceRaised = 7;
                        p.Conscience = 3;
                        p.Hidden.SelfAwareness = 46;
                        break;
                }
            });

            var result = EndingResolver.Resolve(state, db);
            Assert.IsTrue(result.Acts.Exists(act => act.Piece.Id == endingId), endingId + " ← " + string.Join(", ", result.Acts.ConvertAll(act => act.Piece.Id)));
        }

        [Test]
        public void Burden_IsTheReflection_BridgesToTheFuture_AndTheConfessionChangesACut()
        {
            var db = ShippedDb();
            var state = Rescued(db, s =>
            {
                s.Flags.Add("CONSCIENCE_CONFESSED");
                s.Flags.Add("CONFESS_AFRAID");
                s.Records.ConscienceRaised = 8;
                s.Player.Conscience = 2;
                s.Player.Hidden.SelfAwareness = 50;
            });
            var result = EndingResolver.Resolve(state, db);
            Assert.AreEqual("ENDING_NARR_BURDEN", result.PieceId(EndingAct.Reflection));
            var keys = result.CutKeys;
            CollectionAssert.Contains(keys, "ending.ENDING_NARR_BURDEN.afraid.cut2");
            CollectionAssert.DoesNotContain(keys, "ending.ENDING_NARR_BURDEN.cut2");
            CollectionAssert.Contains(keys, "line.bridge_future_burden");
            Assert.AreEqual("ending.ENDING_NARR_BURDEN.cut3", keys[keys.Count - 1], "회고의 꼬리가 맨 끝을 맺는다");

            // 무게를 덜지 못했으면(지금 5 이상) 이 축이 아니다
            state.Player.Conscience = 5;
            Assert.AreNotEqual("ENDING_NARR_BURDEN", EndingResolver.Resolve(state, db).PieceId(EndingAct.Reflection));
        }

        [Test]
        public void Unhealed_AlsoOpensWhenTheWeightWasNeverSpoken()
        {
            var db = ShippedDb();
            var state = Rescued(db, s =>
            {
                s.Player.Hidden.TraumaRecovery = 15;   // 원래 조건(트라우마 회복 6 이하)은 안 맞는다
                s.Player.Conscience = 9;
            });
            var result = EndingResolver.Resolve(state, db);
            Assert.AreEqual("ENDING_NARR_UNHEALED", result.PieceId(EndingAct.Reflection), "무게 8 이상 · 고해 안 함");
            CollectionAssert.Contains(result.CutKeys, "ending.ENDING_NARR_UNHEALED.weight.cut1");
            Assert.AreEqual("ending.ENDING_NARR_UNHEALED.weight.cut3", result.CutKeys[result.CutKeys.Count - 1]);

            state.Flags.Add("CONSCIENCE_CONFESSED");
            Assert.AreNotEqual("ENDING_NARR_UNHEALED", EndingResolver.Resolve(state, db).PieceId(EndingAct.Reflection), "고해했으면 이 갈래가 아니다");

            // 원래 조건으로 열린 판은 원래 컷
            var old = Rescued(db, s =>
            {
                s.Player.Hidden.TraumaRecovery = 0;
                s.Player.Hidden.Isolation = 40;
            });
            var plain = EndingResolver.Resolve(old, db);
            Assert.AreEqual("ENDING_NARR_UNHEALED", plain.PieceId(EndingAct.Reflection));
            CollectionAssert.Contains(plain.CutKeys, "ending.ENDING_NARR_UNHEALED.cut1");
        }

        [Test]
        public void Overcome_And_Collapse_ReadTheWeight()
        {
            var db = ShippedDb();
            var overcome = Rescued(db, s =>
            {
                s.Player.Hidden.TraumaRecovery = 70;
                s.Player.Hidden.SelfAwareness = 40 + 10;
                s.Player.Conscience = 5;   // 「무게를 든 채로」(4 이하)는 아니다
                s.Flags.Add("CONSCIENCE_CONFESSED");
                s.Records.ConscienceRaised = 5;
            });
            var result = EndingResolver.Resolve(overcome, db);
            Assert.AreEqual("ENDING_NARR_OVERCOME", result.PieceId(EndingAct.Reflection));
            Assert.AreEqual("ending.ENDING_NARR_OVERCOME.burden.cut3", result.CutKeys[result.CutKeys.Count - 1], "편지가 무게까지 받는다");

            var collapsed = NewGameFactory.Create(db.Balance, 3);
            collapsed.Status = GameStatus.GameOver;
            collapsed.EndCause = WeekRunner.EndCauseCollapse;
            CollectionAssert.Contains(EndingResolver.Resolve(collapsed, db).CutKeys, "ending.ENDING_BAD_COLLAPSE.cut2");
            collapsed.Player.Conscience = 6;
            var heavy = EndingResolver.Resolve(collapsed, db).CutKeys;
            CollectionAssert.Contains(heavy, "ending.ENDING_BAD_COLLAPSE.weight.cut2");
            Assert.AreEqual(3, heavy.Count);
        }

        [Test]
        public void ShadowAndSoldier_NeverShareAYear()
        {
            var db = ShippedDb();
            var state = Rescued(db, s =>
            {
                s.Records.QuietRuns = 30;
                s.Player.Stats[StatType.Agility] = 60;
                s.Exploration.ZombiesFelled = 16;
            });
            Assert.AreNotEqual("ENDING_SHADOW", EndingResolver.Resolve(state, db).Ending.Id, "열여섯 마리를 쓰러뜨렸으면 '한 번도 맞서지 않은 사람'이 아니다");
            state.Exploration.ZombiesFelled = 15;
            Assert.AreEqual("ENDING_SHADOW", EndingResolver.Resolve(state, db).Ending.Id);
        }
    }
}
