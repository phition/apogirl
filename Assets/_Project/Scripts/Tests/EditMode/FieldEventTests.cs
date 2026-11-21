using System;
using System.IO;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using ApoGirl.Systems.Save;
using ApoGirl.Systems.Session;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>탐험 중 그 자리에서 치른 이벤트: 미리 보기(조우 플래그 반영), 귀환 때 적용, 허브에서 다시 뜨지 않음.</summary>
    public sealed class FieldEventTests
    {
        private const string FieldEvent = "EVT_FIELD";
        private string _directory;

        [SetUp]
        public void SetUp() => _directory = Path.Combine(Path.GetTempPath(), "ApoGirlFieldEvent_" + Guid.NewGuid().ToString("N"));

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }

        private GameSession Session()
        {
            // 조우 선택 플래그 MET가 켜진 채 탐험 슬롯이 끝나면 뜨는 이벤트 (선택지 KEEP은 MET이 있어야 보인다)
            var ev = new EventDef
            {
                Id = FieldEvent, Kind = EventKind.Story, Priority = 90,
                Condition = new ConditionDef { FlagsAll = { "MET" }, ActionCategories = { ActionCategory.Expedition } },
            };
            ev.Choices.Add(new EventChoiceDef { Id = "KEEP", Condition = new ConditionDef { FlagsAll = { "MET" } }, Effect = { SetFlags = { "KEPT" } } });
            ev.Choices.Add(new EventChoiceDef { Id = "PART", Effect = { SetFlags = { "PARTED" } } });

            var db = new InMemoryDefinitionDatabase(
                new BalanceConfig(), TestContent.CreateActions(), new[] { TestContent.CreateModel() }, TestContent.CreateItems(),
                new[] { TestContent.CreateLootTable() }, new[] { TestContent.CreateLocation() }, null, new[] { ev });
            var session = new GameSession(db, new SaveService(_directory, new GameStateSerializer())) { InteractiveEvents = true };
            session.NewGame(11);
            session.UseRealtimeExpeditions = true;
            session.State.Plan.Slots[2].ActionId = TestContent.DayExpedition;
            session.BeginWeek();
            SlotOutcome outcome = null;
            while (session.IsExecuting && (outcome == null || !outcome.AwaitingExpedition))
            {
                outcome = session.Step();
            }

            Assert.IsTrue(outcome.AwaitingExpedition, "탐험 칸에서 멈춤");
            return session;
        }

        [Test]
        public void Preview_UsesRunFlags_WithoutChangingState()
        {
            var session = Session();
            Assert.IsNull(session.PreviewFieldEvent(FieldEvent, Array.Empty<string>(), out var none), "조우 플래그가 없으면 치를 수 없다");
            Assert.IsNull(none);

            var choices = session.PreviewFieldEvent(FieldEvent, new[] { "MET" }, out var ev);
            Assert.AreEqual(FieldEvent, ev.Id);
            CollectionAssert.AreEqual(new[] { "KEEP", "PART" }, choices.Select(c => c.Id).ToArray(), "MET이 있어야 보이는 선택지도 보인다");
            Assert.IsFalse(session.State.Flags.Contains("MET"), "미리 보기는 상태를 바꾸지 않는다");
        }

        [Test]
        public void FieldEvent_AppliesOnReturn_AndDoesNotFireAgainInHub()
        {
            var session = Session();
            var result = new ExpeditionResult
            {
                Status = ExtractStatus.Extracted,
                Flags = { "MET" },
                FieldEvents = { new FieldEventChoice { EventId = FieldEvent, ChoiceId = "PART" } },
            };

            var outcome = session.CompleteExpedition(result);
            Assert.IsTrue(session.State.Flags.Contains("PARTED"), "탐험 중 고른 선택지가 적용된다");
            Assert.IsFalse(session.State.Flags.Contains("KEPT"));
            Assert.IsFalse(session.HasPendingEvent, "허브에서 같은 이벤트가 다시 뜨지 않는다");
            Assert.IsTrue(outcome.Events.Any(e => e.EventId == FieldEvent && e.ChoiceId == "PART"), "결과 카드용 기록");
            Assert.AreEqual(1, session.State.Events.Records[FieldEvent].Count);
        }

        [Test]
        public void WithoutFieldEvent_HubEventStillFires()
        {
            // 자동 탐험·작은 선택지 창처럼 그 자리에서 치르지 않았으면 예전처럼 귀환 뒤 허브에서 뜬다
            var session = Session();
            session.CompleteExpedition(new ExpeditionResult { Status = ExtractStatus.Extracted, Flags = { "MET" } });
            Assert.IsTrue(session.HasPendingEvent);
            Assert.AreEqual(FieldEvent, session.PendingEvent.Id);
        }
    }
}
