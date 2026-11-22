using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>함께한 시간 (역할 재기획 P2, Docs/ROLE_ROOMS_PROPOSAL.md 4절·3.1절): 방 스케줄 → 호감, 주 상한, 적성, 부상·정원 초과, 윤 선생님, 적성 방 바람.</summary>
    public sealed class TogetherTests
    {
        private const string Plain = "NPC_TEST_A";
        private const string Plain2 = "NPC_TEST_B";
        private const string Plain3 = "NPC_TEST_C";
        private const string Nurse = "NPC_NURSE";

        private static readonly ActionDef Lesson = new ActionDef { Id = "ACT_T_LESSON", Category = ActionCategory.Education, Teaching = TeachingMode.TeacherDirect };
        private static readonly ActionDef Medicine = new ActionDef { Id = "ACT_T_MEDICINE", Category = ActionCategory.Education };
        private static readonly ActionDef Care = new ActionDef { Id = "ACT_CARE_TEACHER", Category = ActionCategory.Survival };
        private static readonly ActionDef Elsewhere = new ActionDef { Id = "ACT_T_OTHER", Category = ActionCategory.Survival };

        private static InMemoryDefinitionDatabase Db()
        {
            var extras = new DefinitionExtras
            {
                Roles =
                {
                    new RoleDef
                    {
                        Id = "STUDY",
                        Rooms = { new RoleRoomDef { Id = "CLASSROOM", Facility = "CLASSROOM", Actions = { Lesson.Id } } },
                    },
                    new RoleDef
                    {
                        Id = "MEDIC",
                        Rooms = { new RoleRoomDef { Id = "INFIRMARY", Facility = "INFIRMARY", Actions = { Medicine.Id, Care.Id } } },
                    },
                },
            };
            return new InMemoryDefinitionDatabase(new BalanceConfig(), TestContent.CreateActions(), new[] { TestContent.CreateModel() },
                TestContent.CreateItems(), new[] { TestContent.CreateLootTable() }, new[] { TestContent.CreateLocation() }, extras: extras);
        }

        private static GameState State(InMemoryDefinitionDatabase db, bool teacher = false)
        {
            var state = TestContent.NewGame(db);
            state.Calendar.Week = 5;
            state.Base.Facilities["CLASSROOM"] = 1;
            if (teacher)
            {
                state.Events.Records[db.Balance.OpeningEventId] = new EventRecord { Count = 1, LastWeek = 1 };
            }

            return state;
        }

        private static NpcState Join(GameState state, string id, string role, int since = 1)
        {
            var npc = new NpcState { Status = NpcStatus.Joined, Role = role, RoleSinceWeek = since, Affinity = 10 };
            state.Npcs[id] = npc;
            return npc;
        }

        [Test]
        public void CompanionInRoom_GainsOne_UpToWeeklyCap_ThenNextWeekAgain()
        {
            var db = Db();
            var state = State(db);
            var npc = Join(state, Plain, "STUDY");
            for (var i = 0; i < 4; i++)
            {
                TogetherRules.Apply(state, db, Lesson, new StateDelta());
            }

            Assert.AreEqual(10 + db.Balance.TogetherWeeklyCap, npc.Affinity, "주 2까지");
            Assert.AreEqual(5, GiveRules.Log(state, Plain)[BondDecayRules.CareKey], "돌봄 기록이 이번 주로");

            state.Calendar.Week = 6;
            var present = TogetherRules.Apply(state, db, Lesson, new StateDelta());
            Assert.AreEqual(13, npc.Affinity, "다음 주에는 다시 오른다");
            Assert.AreEqual(1, present.Single().Gained);
        }

        [Test]
        public void OtherRoomOrClosedRoom_NoOneThere()
        {
            var db = Db();
            var state = State(db);
            Join(state, Plain, "STUDY");
            Assert.IsEmpty(TogetherRules.Apply(state, db, Elsewhere, new StateDelta()), "방 스케줄이 아니다");
            Assert.IsEmpty(TogetherRules.Apply(state, db, Medicine, new StateDelta()), "그 방에 아무도 없다");

            state.Base.Facilities["CLASSROOM"] = 0;
            Assert.IsEmpty(TogetherRules.Apply(state, db, Lesson, new StateDelta()), "방이 닫혀 거처에 있다");
        }

        [Test]
        public void Aptitude_GainsTwo_CapThree()
        {
            var db = Db();
            var state = State(db);
            state.Base.Facilities["INFIRMARY"] = 1;
            var nurse = Join(state, Nurse, "MEDIC");
            var first = TogetherRules.Apply(state, db, Medicine, new StateDelta()).Single();
            Assert.IsTrue(first.Aptitude);
            Assert.AreEqual(12, nurse.Affinity);
            TogetherRules.Apply(state, db, Medicine, new StateDelta());
            TogetherRules.Apply(state, db, Medicine, new StateDelta());
            Assert.AreEqual(10 + db.Balance.TogetherAptitudeWeeklyCap, nurse.Affinity, "적성은 주 3까지");
        }

        [Test]
        public void Injured_NoGain_OverCapacity_StillGains()
        {
            var db = Db();
            var state = State(db);
            var a = Join(state, Plain, "STUDY", 1);
            var b = Join(state, Plain2, "STUDY", 2);
            var c = Join(state, Plain3, "STUDY", 3);   // 정원 2를 넘은 셋째
            a.InjuredWeeks = 1;
            var present = TogetherRules.Apply(state, db, Lesson, new StateDelta()).Select(t => t.Id).ToList();
            CollectionAssert.AreEquivalent(new[] { Plain2, Plain3 }, present);
            Assert.IsFalse(RoleRules.IsActive(state, Plain3, db.Balance), "셋째는 역할 효과가 없지만");
            Assert.AreEqual(11, c.Affinity, "함께한 시간은 유효하다");
            Assert.AreEqual(10, a.Affinity, "부상이면 오르지 않는다");
            Assert.AreEqual(11, b.Affinity);
        }

        [Test]
        public void Teacher_OnlyTeachingLessons_WeeklyOne_IllOnlyCare()
        {
            var db = Db();
            var state = State(db, teacher: true);
            var before = state.Teacher.Affinity;
            TogetherRules.Apply(state, db, Elsewhere, new StateDelta());
            Assert.AreEqual(before, state.Teacher.Affinity, "가르치는 수업이 아니면 없다");

            var present = TogetherRules.Apply(state, db, Lesson, new StateDelta());
            Assert.AreEqual(GiveRules.TeacherId, present.Single().Id);
            TogetherRules.Apply(state, db, Lesson, new StateDelta());
            Assert.AreEqual(before + 1, state.Teacher.Affinity, "주 1까지");

            state.Calendar.Week = 6;
            state.Flags.Add("TEACHER_ISOLATED");
            TogetherRules.Apply(state, db, Lesson, new StateDelta());
            Assert.AreEqual(before + 1, state.Teacher.Affinity, "아프면 수업에는 없다");
            TogetherRules.Apply(state, db, Care, new StateDelta());
            Assert.AreEqual(before + 2, state.Teacher.Affinity, "선생님 돌보기에서만");

            state.Calendar.Week = 7;
            state.Flags.Add("TEACHER_LEFT_BEHIND");
            Assert.IsFalse(TogetherRules.Apply(state, db, Care, new StateDelta()).Any(t => t.Id == GiveRules.TeacherId), "남겨졌으면 없다");
        }

        [Test]
        public void Aptitude_WishWhenNoRoom_HintWhenOpen()
        {
            var db = Db();
            var state = State(db);
            Join(state, Nurse, "STUDY");
            Assert.IsTrue(TogetherRules.WishesForAptitudeRoom(state, db, Nurse), "보건실이 없으면 바란다");
            Assert.IsNull(TogetherRules.OpenAptitudeRole(state, db, Nurse));
            Assert.IsFalse(TogetherRules.WishesForAptitudeRoom(state, db, Plain), "적성이 없는 사람은 바라지 않는다");

            state.Base.Facilities["INFIRMARY"] = 1;
            Assert.IsFalse(TogetherRules.WishesForAptitudeRoom(state, db, Nurse));
            Assert.AreEqual("MEDIC", TogetherRules.OpenAptitudeRole(state, db, Nurse), "맡길 수 있으면 힌트");
        }
    }
}
