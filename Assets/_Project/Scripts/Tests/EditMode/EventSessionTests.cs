using System;
using System.IO;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.State;
using ApoGirl.Core.Text;
using ApoGirl.Systems.Save;
using ApoGirl.Systems.Session;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>M3-2: 세션의 이벤트 대기·선택·강제 발생, 한국어 조사.</summary>
    public sealed class EventSessionTests
    {
        private string _directory;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "ApoGirlEventTest_" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        private GameSession Session(params EventDef[] events)
        {
            var db = new InMemoryDefinitionDatabase(
                new BalanceConfig(), TestContent.CreateActions(), new[] { TestContent.CreateModel() }, TestContent.CreateItems(),
                new[] { TestContent.CreateLootTable() }, new[] { TestContent.CreateLocation() }, null, events);
            var session = new GameSession(db, new SaveService(_directory, new GameStateSerializer())) { InteractiveEvents = true };
            session.NewGame(7);
            return session;
        }

        private static EventDef FirstDayEvent()
        {
            var ev = new EventDef { Id = "EVT_DAY1", Kind = EventKind.Story, Condition = new ConditionDef { Days = { 1 }, Phase = PhaseFilter.Day } };
            ev.Choices.Add(new EventChoiceDef { Id = "A", Effect = { SetFlags = { "PICKED_A" } } });
            ev.Choices.Add(new EventChoiceDef { Id = "B", Effect = { SetFlags = { "PICKED_B" } } });
            return ev;
        }

        [Test]
        public void InteractiveEvent_FiresEventStarted_AndWaitsForChoice()
        {
            var session = Session(FirstDayEvent());
            string started = null;
            session.EventStarted += ev => started = ev.Id;
            session.BeginWeek();

            var outcome = session.Step();
            Assert.IsTrue(outcome.AwaitingEvent);
            Assert.AreEqual("EVT_DAY1", started);
            Assert.IsTrue(session.HasPendingEvent);
            Assert.IsFalse(session.IsPlanning);
            Assert.Throws<InvalidOperationException>(() => session.Step());

            CollectionAssert.AreEqual(new[] { "A", "B" }, session.PendingChoices.Select(c => c.Id).ToArray());
            session.ResolveEvent("B");
            Assert.IsFalse(session.HasPendingEvent);
            Assert.IsTrue(session.State.Flags.Contains("PICKED_B"));

            for (var i = 1; i < WeekPlan.SlotCount; i++)
            {
                session.Step();
            }

            Assert.IsTrue(session.IsPlanning, "주가 정상적으로 끝남");
            Assert.AreEqual(2, session.State.Calendar.Week);
        }

        [Test]
        public void EventOnLastSlot_FinishesWeekAfterChoice()
        {
            var ev = FirstDayEvent();
            ev.Condition = new ConditionDef { Days = { 7 }, Phase = PhaseFilter.Night };
            var session = Session(ev);
            var ended = 0;
            session.WeekEnded += _ => ended++;
            session.BeginWeek();
            for (var i = 0; i < WeekPlan.SlotCount - 1; i++)
            {
                session.Step();
            }

            var last = session.Step();
            Assert.IsTrue(last.AwaitingEvent);
            Assert.AreEqual(0, ended, "선택 전에는 정산하지 않음");
            session.ResolveEvent("A");
            Assert.AreEqual(1, ended);
            Assert.IsTrue(session.IsPlanning);
        }

        [Test]
        public void DebugForceEvent_RunsOutsideSlots()
        {
            var ev = FirstDayEvent();
            ev.Condition = new ConditionDef { WeekMin = 40 };
            var session = Session(ev);
            var outcome = session.DebugForceEvent("EVT_DAY1");
            Assert.IsTrue(outcome.AwaitingEvent);
            session.ResolveEvent("A");
            Assert.IsTrue(session.State.Flags.Contains("PICKED_A"));
            Assert.IsTrue(session.IsPlanning);
            Assert.AreEqual(1, session.State.Calendar.Week, "달력은 그대로");
        }

        [Test]
        public void NonInteractive_AutoResolves()
        {
            var session = Session(FirstDayEvent());
            session.InteractiveEvents = false;
            session.BeginWeek();
            var outcome = session.Step();
            Assert.IsFalse(outcome.AwaitingEvent);
            Assert.AreEqual("A", outcome.Events.Single().ChoiceId);
        }

        [TestCase("서진", "이/가", "서진이")]
        [TestCase("서아", "이/가", "서아가")]
        [TestCase("서진", "아/야", "서진아")]
        [TestCase("서아", "아/야", "서아야")]
        [TestCase("선생님", "은/는", "선생님은")]
        [TestCase("친구", "을/를", "친구를")]
        [TestCase("교실", "으로/로", "교실로")]
        [TestCase("옥상", "으로", "옥상으로")]
        [TestCase("학교", "으로/로", "학교로")]
        [TestCase("지우", "과와", "지우와")]
        [TestCase("Sam", "이/가", "Sam이")]
        [TestCase("3", "을/를", "3을")]
        public void KoreanJosa_PicksParticle(string word, string pair, string expected)
        {
            Assert.AreEqual(expected, KoreanJosa.Attach(word, pair));
        }
    }
}
