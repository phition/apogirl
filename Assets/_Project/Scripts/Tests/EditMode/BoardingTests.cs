using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Endings;
using ApoGirl.Core.Events;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using ApoGirl.Systems.Content;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>구조 헬기 좌석: 탑승 후보 수, 9명일 때 한 명 남기기(짝), 빈자리, 엔딩 변형.</summary>
    public sealed class BoardingTests
    {
        private static readonly string[] Companions =
        {
            BoardingRules.Nurse, BoardingRules.Dohyeon, BoardingRules.Bystander, BoardingRules.Ringleader, BoardingRules.Senior,
        };

        private static InMemoryDefinitionDatabase ShippedDb()
        {
            var content = ContentJson.LoadFolder(ContentPipelineTests.ProjectPath(ContentJson.ContentFolder));
            return new InMemoryDefinitionDatabase(
                new BalanceConfig(), content.Actions, content.ExpeditionModels, content.Items, content.LootTables,
                null, content.Enemies, content.Events, content.Npcs,
                new DefinitionExtras { Traits = content.Traits, Facilities = content.Facilities, Lines = content.Lines, Prologue = content.Prologue ?? new PrologueDef(), Endings = content.Endings, Roles = content.Roles, StartBonuses = content.StartBonuses, Factions = content.Factions });
        }

        /// <summary>선생님 완치 + 동료 전원 + 준 = 9명.</summary>
        private static GameState FullHouse(BalanceConfig balance, bool cured = true, bool jun = true)
        {
            var state = NewGameFactory.Create(balance, 3);
            if (cured) state.Flags.Add(M6Rules.FlagTeacherCured);
            if (jun) state.Flags.Add(BoardingRules.FlagJunJoins);
            foreach (var id in Companions) state.Npcs[id] = new NpcState { Status = NpcStatus.Joined };
            return state;
        }

        [Test]
        public void Count_IsNineOnlyWhenCuredAndEveryoneJoined()
        {
            var balance = new BalanceConfig();
            Assert.AreEqual(8, balance.RescueSeats);
            Assert.AreEqual(9, BoardingRules.CandidateCount(FullHouse(balance)));
            Assert.IsTrue(BoardingRules.NeedsSeatChoice(FullHouse(balance), balance));

            Assert.AreEqual(8, BoardingRules.CandidateCount(FullHouse(balance, cured: false)), "완치가 아니면 선생님은 셈에 없다");
            Assert.IsFalse(BoardingRules.NeedsSeatChoice(FullHouse(balance, cured: false), balance));
            Assert.IsFalse(BoardingRules.NeedsSeatChoice(FullHouse(balance, jun: false), balance));

            var noSenior = FullHouse(balance);
            noSenior.Npcs[BoardingRules.Senior].Status = NpcStatus.Met;
            Assert.IsFalse(BoardingRules.NeedsSeatChoice(noSenior, balance), "선배가 없으면 8명");
        }

        [TestCase(BoardingRules.FlagStayTeachers, new[] { BoardingRules.Teacher, BoardingRules.Nurse }, 1)]
        [TestCase(BoardingRules.FlagStayFriend, new[] { BoardingRules.Friend, BoardingRules.Player }, 1)]
        [TestCase(BoardingRules.FlagStayRingleader, new[] { BoardingRules.Ringleader, BoardingRules.Senior }, 1)]
        [TestCase(BoardingRules.FlagStayBystander, new[] { BoardingRules.Bystander, BoardingRules.Senior }, 1)]
        [TestCase(BoardingRules.FlagStaySenior, new[] { BoardingRules.Senior }, 0)]
        [TestCase(BoardingRules.FlagStaySelf, new[] { BoardingRules.Player, BoardingRules.Ringleader, BoardingRules.Teacher }, 2)]
        public void Choice_PairsStayAndChildrenAlwaysBoard(string flag, string[] stayers, int freeSeats)
        {
            var balance = new BalanceConfig();
            var state = FullHouse(balance);
            state.Flags.Add(flag);

            CollectionAssert.AreEquivalent(stayers, BoardingRules.Stayers(state));
            var boarding = BoardingRules.Boarding(state);
            CollectionAssert.Contains(boarding, BoardingRules.Dohyeon);
            CollectionAssert.Contains(boarding, BoardingRules.Jun);
            Assert.LessOrEqual(boarding.Count, balance.RescueSeats);
            Assert.AreEqual(freeSeats, BoardingRules.FreeSeats(state, balance));
        }

        [Test]
        public void EmptySeat_GoesToSageoriChildOnlyOnGoodTerms()
        {
            var balance = new BalanceConfig();
            var state = FullHouse(balance);
            state.Flags.Add(BoardingRules.FlagStayTeachers);
            Assert.IsFalse(BoardingRules.SageoriChildBoards(state, balance), "사거리와 경계 상태면 빈자리로 떠난다");

            state.Flags.Add(BoardingRules.FlagJunMediated);
            Assert.IsTrue(BoardingRules.SageoriChildBoards(state, balance));

            var senior = FullHouse(balance);
            senior.Flags.Add(BoardingRules.FlagStaySenior);
            senior.Flags.Add(BoardingRules.FlagJunMediated);
            Assert.IsFalse(BoardingRules.SageoriChildBoards(senior, balance), "선배 혼자 남으면 빈자리가 없다");
        }

        [Test]
        public void Shipped_SeatChoiceEvent_OnlyWithNine()
        {
            var db = ShippedDb();
            Assert.IsTrue(db.TryGetEvent("EVT_A4_SEAT_CHOICE", out var ev));
            CollectionAssert.AreEquivalent(new[] { "TEACHERS", "FRIEND", "RINGLEADER", "BYSTANDER", "SENIOR", "SELF" }, ev.Choices.Select(c => c.Id));

            var context = new EventContext { Phase = DayPhase.Night, Day = 5 };
            var full = FullHouse(db.Balance);
            full.Calendar.Week = CalendarState.TotalWeeks;
            Assert.IsTrue(ConditionEvaluator.IsMet(full, ev.Condition, context, db.Balance));

            var eight = FullHouse(db.Balance, jun: false);
            eight.Calendar.Week = CalendarState.TotalWeeks;
            Assert.IsFalse(ConditionEvaluator.IsMet(eight, ev.Condition, context, db.Balance));
        }

        [TestCase(BoardingRules.FlagStaySenior, "SEAT_SENIOR", "ending_seat_senior")]
        [TestCase(BoardingRules.FlagStaySelf, "SEAT_SELF", "ending_seat_self")]
        [TestCase(BoardingRules.FlagStayFriend, "SEAT_FRIEND", "ending_seat_friend")]
        public void Shipped_CureEnding_UsesSeatVariant(string flag, string variantId, string image)
        {
            var db = ShippedDb();
            var state = FullHouse(db.Balance);
            state.Calendar.Week = CalendarState.TotalWeeks;
            state.Status = GameStatus.Rescued;
            state.Flags.Add(flag);

            var result = EndingResolver.Resolve(state, db);
            Assert.AreEqual("ENDING_HIDDEN_CURE", result.Ending.Id);
            Assert.AreEqual(variantId, result.Variant?.Id);
            Assert.AreEqual(image, result.ArtKey);
            Assert.IsTrue(result.CutKeys[0].Contains(".seat_"), "변형 컷이 기본 컷 대신 나온다");

            var plain = FullHouse(db.Balance);
            plain.Calendar.Week = CalendarState.TotalWeeks;
            plain.Status = GameStatus.Rescued;
            var basic = EndingResolver.Resolve(plain, db);
            Assert.IsNull(basic.Variant);
            Assert.AreEqual("ENDING_HIDDEN_CURE", basic.ArtKey);
        }
    }
}
