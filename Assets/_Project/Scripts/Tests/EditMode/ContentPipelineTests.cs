using System;
using System.IO;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Events;
using ApoGirl.Systems.Content;
using NUnit.Framework;
using UnityEngine;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>M3-3: 콘텐츠 JSON 읽기, Yarn 태그 스캔, 검증기, 실제 샘플 콘텐츠의 무결성.</summary>
    public sealed class ContentPipelineTests
    {
        private static string ProjectPath(string assetPath) => Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? "", assetPath);

        private static InMemoryDefinitionDatabase Db(ContentFile content)
        {
            return new InMemoryDefinitionDatabase(
                new BalanceConfig(), TestContent.CreateActions(), new[] { TestContent.CreateModel() }, TestContent.CreateItems(),
                new[] { TestContent.CreateLootTable() }, new[] { TestContent.CreateLocation() }, null, content.Events, content.Npcs);
        }

        [Test]
        public void Json_ParsesEnumsByName_AndRejectsUnknownFields()
        {
            var content = ContentJson.Parse(@"{ ""Events"": [ { ""Id"": ""EVT_A"", ""Kind"": ""Crisis"",
                ""Condition"": { ""Phase"": ""Night"", ""Checks"": [ { ""Kind"": ""Stat"", ""Key"": ""Intellect"", ""Op"": ""Less"", ""Value"": 5 } ] } } ] }");
            var ev = content.Events.Single();
            Assert.AreEqual(EventKind.Crisis, ev.Kind);
            Assert.AreEqual(PhaseFilter.Night, ev.Condition.Phase);
            Assert.AreEqual(CompareOp.Less, ev.Condition.Checks[0].Op);
            Assert.IsTrue(ev.Once, "기본값 유지");

            Assert.Throws<FormatException>(() => ContentJson.Parse(@"{ ""Events"": [ { ""Id"": ""EVT_B"", ""Prioirty"": 3 } ] }"), "오타 필드");
        }

        [Test]
        public void YarnScanner_CollectsNodesAndChoiceTags()
        {
            var nodes = YarnScriptScanner.Scan("title: N1\ntags: x\n---\n줄\n-> 가 #choice:A\n    -> 하위 #choice:B\n===\ntitle: N2\n---\n-> 그냥\n===\n");
            CollectionAssert.AreEquivalent(new[] { "A", "B" }, nodes["N1"]);
            Assert.AreEqual(0, nodes["N2"].Count);
        }

        [Test]
        public void Validator_FindsBrokenReferences()
        {
            var ev = new EventDef { Id = "EVT_BAD" };
            ev.Condition.Checks.Add(new ValueCheck(ValueKind.Stat, "Inteligence", CompareOp.Greater, 1));
            ev.Choices.Add(new EventChoiceDef { Id = "A", NextEventId = "EVT_NOPE", Effect = { Npcs = { new NpcChange { NpcId = "NPC_GHOST" } } } });
            ev.Choices.Add(new EventChoiceDef { Id = "A" });
            var db = Db(new ContentFile { Events = { ev } });
            var input = new ContentValidationInput { YarnNodes = YarnScriptScanner.Scan("title: EVT_BAD\n---\n-> x #choice:Z\n===\n") };

            var errors = ContentValidator.Validate(db, input).Where(i => i.Severity == IssueSeverity.Error).Select(i => i.Message).ToList();
            Assert.IsTrue(errors.Any(m => m.Contains("Inteligence")), "잘못된 스탯 키");
            Assert.IsTrue(errors.Any(m => m.Contains("EVT_NOPE")), "없는 연쇄 이벤트");
            Assert.IsTrue(errors.Any(m => m.Contains("NPC_GHOST")), "없는 NPC");
            Assert.IsTrue(errors.Any(m => m.Contains("중복")), "선택지 ID 중복");
            Assert.IsTrue(errors.Any(m => m.Contains("#choice:A")), "Yarn에 없는 선택지");
            Assert.IsTrue(errors.Any(m => m.Contains("#choice:Z")), "데이터에 없는 Yarn 태그");
        }

        [Test]
        public void ShippedContent_HasNoErrors()
        {
            var content = ContentJson.LoadFolder(ProjectPath(ContentJson.ContentFolder));
            Assert.Greater(content.Events.Count, 0, "샘플 이벤트가 있어야 함");

            var nodes = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.HashSet<string>>();
            foreach (var file in Directory.GetFiles(ProjectPath("Assets/_Project/Resources/Dialogue"), "*.yarn", SearchOption.AllDirectories))
            {
                YarnScriptScanner.Scan(File.ReadAllText(file), nodes);
            }

            var issues = ContentValidator.Validate(Db(content), new ContentValidationInput { YarnNodes = nodes });
            var errors = issues.Where(i => i.Severity == IssueSeverity.Error).ToList();
            Assert.IsEmpty(errors, string.Join("\n", errors));
        }

        [Test]
        public void ShippedContent_SurvivesHeadlessYear()
        {
            var content = ContentJson.LoadFolder(ProjectPath(ContentJson.ContentFolder));
            var db = Db(content);
            var state = TestContent.NewGame(db, 99);
            var runner = new ApoGirl.Core.Simulation.WeekRunner(state, db, new ApoGirl.Core.Random.SeededRandom(state.Rng), new StubExpeditionRunner());
            for (var week = 0; week < 10 && state.Status == ApoGirl.Core.State.GameStatus.Playing; week++)
            {
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

            Assert.IsTrue(state.Events.Records.ContainsKey("EVT_CAL_MOCK_EXAM"), "2주차 3/26 학사력 이벤트");
            Assert.IsTrue(state.Events.Records.ContainsKey("EVT_NIGHT_TEACHER_TALK_01"), "밤 휴식 대화");
        }
    }
}
