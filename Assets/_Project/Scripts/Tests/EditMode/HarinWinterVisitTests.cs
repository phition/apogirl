using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Events;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>하린의 겨울 방문: 마트에서 마주하고 데려오지 않은 판에, 첫눈 뒤 하린이 교문으로 찾아온다 (들이기·먹을 것만·돌려보내기).</summary>
    public sealed class HarinWinterVisitTests
    {
        private const string EventId = "EVT_HR_WINTER_VISIT";
        private const string Harin = "NPC_RINGLEADER";

        private static GameState Make(BalanceConfig balance, int week = 43)
        {
            var state = NewGameFactory.Create(balance, 11);
            state.Calendar.Week = week;
            state.Flags.Add("RINGLEADER_MET");
            state.Flags.Add("BULLY_SEPARATED");
            state.Flags.Add("FIRST_SNOW");
            state.Base.Resources[ResourceType.Food] = 20;
            state.Base.Resources[ResourceType.Water] = 20;
            return state;
        }

        private static readonly EventContext Day = new EventContext { Phase = DayPhase.Day, Day = 2 };

        [Test]
        public void Shipped_Visit_OnlyWhenMetNotRescued_AfterFirstSnow()
        {
            var db = M6Act4Tests.ShippedDb();
            Assert.IsTrue(db.TryGetEvent(EventId, out var ev));
            CollectionAssert.AreEquivalent(new[] { "LET_IN", "FEED", "SHUT" }, ev.Choices.Select(c => c.Id));

            Assert.IsTrue(ConditionEvaluator.IsMet(Make(db.Balance), ev.Condition, Day, db.Balance));
            Assert.IsFalse(ConditionEvaluator.IsMet(Make(db.Balance), ev.Condition, new EventContext { Phase = DayPhase.Night, Day = 2 }, db.Balance), "낮에만");
            Assert.IsFalse(ConditionEvaluator.IsMet(Make(db.Balance, 41), ev.Condition, Day, db.Balance), "42주부터");
            Assert.IsFalse(ConditionEvaluator.IsMet(Make(db.Balance, 48), ev.Condition, Day, db.Balance), "47주까지");

            var rescued = Make(db.Balance);
            rescued.Flags.Add("BULLY_RESCUED");
            Assert.IsFalse(ConditionEvaluator.IsMet(rescued, ev.Condition, Day, db.Balance), "이미 데려온 판은 아니다");

            var noSnow = Make(db.Balance);
            noSnow.Flags.Remove("FIRST_SNOW");
            Assert.IsFalse(ConditionEvaluator.IsMet(noSnow, ev.Condition, Day, db.Balance), "첫눈 뒤");

            var neverMet = Make(db.Balance);
            neverMet.Flags.Remove("RINGLEADER_MET");
            Assert.IsFalse(ConditionEvaluator.IsMet(neverMet, ev.Condition, Day, db.Balance), "마트에서 마주한 판만");
        }

        [Test]
        public void LetIn_JoinsHarin_AndOpensHerStoryLine()
        {
            var db = M6Act4Tests.ShippedDb();
            db.TryGetEvent(EventId, out var ev);
            var state = Make(db.Balance);
            EventResolver.Choose(state, ev, "LET_IN", Day, db.Balance, new FixedRandom(0.5), new StateDelta());

            Assert.AreEqual(NpcStatus.Joined, ConditionEvaluator.NpcStatusOf(state, Harin));
            Assert.Less(state.Npcs[Harin].Affinity, db.Balance.JoinAffinity, "스스로 찾아왔지만 아직 서먹하다");
            Assert.IsTrue(state.Flags.Contains("RINGLEADER_NIGHT"), "하린 이야기 줄(시키지 않은 일)이 이어진다");
            Assert.IsFalse(state.Flags.Contains("BULLY_RESCUED"), "마트에서 데려온 것과는 따로");

            Assert.IsTrue(db.TryGetEvent("EVT_HR_02_CHORES", out var chores));
            Assert.IsTrue(ConditionEvaluator.IsMet(state, chores.Condition, Day, db.Balance));
            // 한 번만: 띄울 때(OnShow) 켜는 깃발이 다시 막는다
            CollectionAssert.Contains(ev.OnShow.SetFlags, "HR_WINTER_VISIT");
            state.Flags.Add("HR_WINTER_VISIT");
            Assert.IsFalse(ConditionEvaluator.IsMet(state, ev.Condition, Day, db.Balance), "한 번만");
        }

        [Test]
        public void Refusals_DoNotJoin_AndPickTheirEpilogue()
        {
            var db = M6Act4Tests.ShippedDb();
            db.TryGetEvent(EventId, out var ev);
            foreach (var (choice, epilogue) in new[] { ("FEED", "line.ep_ringleader_winter_fed"), ("SHUT", "line.ep_ringleader_winter_shut") })
            {
                var state = Make(db.Balance);
                EventResolver.Choose(state, ev, choice, Day, db.Balance, new FixedRandom(0.5), new StateDelta());
                Assert.AreNotEqual(NpcStatus.Joined, ConditionEvaluator.NpcStatusOf(state, Harin), choice);

                var lines = db.Extras.Lines.Where(l => l.Kind == LineKind.EndingEpilogue && l.Group == Harin
                                                       && ConditionEvaluator.IsMet(state, l.Condition, new EventContext(), db.Balance))
                    .OrderByDescending(l => l.Priority).ToList();
                Assert.AreEqual(epilogue, lines.First().TextKey, choice);
            }
        }
    }
}
