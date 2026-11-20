using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Random;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using ApoGirl.Systems.Save;
using ApoGirl.Systems.Session;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>1주차: 1~6일은 창고에 갇혀 버티고, 7일 낮에 한 번만 탈출한다.</summary>
    public sealed class TutorialWeekTests
    {
        private static GameSession NewSession(string directory, out InMemoryDefinitionDatabase db)
        {
            db = M6Act4Tests.ShippedDb();
            var session = new GameSession(db, new SaveService(directory, new GameStateSerializer()));
            session.InteractiveEvents = false;
            return session;
        }

        [Test]
        public void NewGame_LocksStorageDays_ThenEscapeOnLastDay()
        {
            var directory = Path.Combine(Path.GetTempPath(), "ApoGirlTutWeek_" + Guid.NewGuid().ToString("N"));
            try
            {
                var session = NewSession(directory, out var db);
                session.NewGame(7);
                var slots = session.State.Plan.Slots;
                var balance = db.Balance;
                Assert.AreEqual(7, balance.TutorialDay);
                for (var i = 0; i < 12; i++)
                {
                    Assert.AreEqual(balance.StorageWaitActionId, slots[i].ActionId, $"슬롯 {i}");
                    Assert.IsTrue(slots[i].Locked);
                }

                Assert.AreEqual(balance.TutorialActionId, slots[12].ActionId, "7일 낮 탈출");
                Assert.IsTrue(slots[12].Locked);
                Assert.AreEqual(balance.NightRestActionId, slots[13].ActionId, "7일 밤 휴식");
                Assert.IsTrue(slots[13].Locked);

                Assert.IsFalse(session.IsUnlocked(db.GetAction(balance.TutorialActionId)), "탈출은 계획 목록에 없다 (여러 번 넣을 수 없음)");
                Assert.IsFalse(session.IsUnlocked(db.GetAction(balance.StorageWaitActionId)));

                Assert.IsTrue(session.HasPendingOpening, "허브 도입: 창고에 갇힘");
                session.StartOpening();
                Assert.IsTrue(session.State.Events.Records.ContainsKey(balance.IntroEventId));
                Assert.IsFalse(session.State.Events.Records.ContainsKey(balance.OpeningEventId), "선생님은 아직 오지 않았다");
                Assert.IsFalse(session.HasPendingOpening);
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        [Test]
        public void Week1_OnlyOpeningBeforeEscape_NoRaidsWhileConfined()
        {
            var db = M6Act4Tests.ShippedDb();
            var state = NewGameFactory.Create(db.Balance, 3);
            EffectHelpers.ApplyPrologueCommon(state, db);
            Assert.IsTrue(TutorialWeekRules.Apply(state, db));
            state.Base.Threat = 100; // 습격이 날 법한 위협도여도 갇혀 있는 동안은 없다

            var runner = new WeekRunner(state, db, new SeededRandom(state.Rng), new HeadlessExpeditionRunner(db));
            var log = new List<(int Slot, string EventId)>();
            var raids = new List<int>();
            runner.PrepareWeek();
            for (var slot = 0; !runner.IsWeekComplete && !runner.IsGameOver; slot++)
            {
                var outcome = runner.ExecuteNextSlot();
                while (runner.HasPendingEvent)
                {
                    outcome = runner.ResolveEvent(runner.PendingChoices.FirstOrDefault(c => c.Enabled)?.Id ?? "");
                }

                log.AddRange(outcome.Events.Select(e => (slot, e.EventId)));
                if (outcome.Raid != null && slot < 12) raids.Add(slot);
            }

            Assert.IsEmpty(raids, "창고에 갇힌 밤에는 습격이 없다");
            var beforeEscape = log.Where(e => e.Slot < 12).ToList();
            Assert.AreEqual(1, beforeEscape.Count, "탈출 전에는 선생님이 문을 여는 장면만: " + string.Join(", ", beforeEscape));
            Assert.AreEqual((11, db.Balance.OpeningEventId), beforeEscape[0], "6일 밤이 끝날 때");
            Assert.IsTrue(log.Any(e => e.Slot == 12 && e.EventId == "EVT_A1_TUTORIAL_DONE"), "7일 낮 탈출 뒤 본관 창고 도착");
            Assert.AreEqual(1, state.Exploration.ActionCounts[db.Balance.TutorialActionId], "탈출은 한 번");
            Assert.IsTrue(state.Flags.Contains("LOC_SCHOOL_DAY"));
        }

        [Test]
        public void NewGamePlus_RecallsSevenDays_RightBeforeDoorOpens()
        {
            var directory = Path.Combine(Path.GetTempPath(), "ApoGirlTutNg_" + Guid.NewGuid().ToString("N"));
            try
            {
                var db = M6Act4Tests.ShippedDb();
                var gallery = new EndingGallery(directory);
                gallery.Record("ENDING_SURVIVOR", 52);
                var session = new GameSession(db, new SaveService(directory, new GameStateSerializer()), null, gallery);
                session.InteractiveEvents = false;
                session.NewGame(5);
                Assert.IsTrue(session.State.Flags.Contains(GameSession.NgPlusFlag));
                session.StartOpening();
                Assert.IsFalse(session.State.Events.Records.ContainsKey(db.Balance.NgPlusOpeningEventId), "회상은 허브 도입이 아니라 6일 밤 끝에");

                var order = new List<string>();
                session.BeginWeek();
                while (session.IsExecuting)
                {
                    order.AddRange(session.Step().Events.Select(e => e.EventId));
                }

                var ng = order.IndexOf(db.Balance.NgPlusOpeningEventId);
                Assert.GreaterOrEqual(ng, 0, string.Join(", ", order));
                Assert.AreEqual(db.Balance.OpeningEventId, order[ng + 1], "회상 다음 바로 문이 열린다");
                Assert.IsTrue(session.State.Clues.Contains("TRUTH_STORAGE_SOUNDS"));
                Assert.AreEqual(2, session.State.Calendar.Week);
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }
    }
}
