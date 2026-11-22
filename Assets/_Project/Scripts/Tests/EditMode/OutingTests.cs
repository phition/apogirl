using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>동료 탐험 (역할 재기획 P3, Docs/ROLE_ROOMS_PROPOSAL.md 5절): 일정·나가지 않는 때·하루 상한, 물자, 다침 두 종류, 자리 비움, 붙잡기.</summary>
    public sealed class OutingTests
    {
        private const string A = "NPC_TEST_A";
        private const string B = "NPC_TEST_B";
        private const string C = "NPC_TEST_C";

        private static readonly ActionDef Lesson = new ActionDef { Id = "ACT_T_LESSON", Category = ActionCategory.Education };

        private static InMemoryDefinitionDatabase Db(System.Action<BalanceConfig> tweak = null)
        {
            var balance = new BalanceConfig();
            tweak?.Invoke(balance);
            var extras = new DefinitionExtras
            {
                Roles =
                {
                    new RoleDef
                    {
                        Id = "STUDY",
                        Rooms = { new RoleRoomDef { Id = "CLASSROOM", Always = true, Actions = { Lesson.Id } } },
                        Modifiers = { new TraitModifier { Kind = TraitModifierKind.EducationGain, Value = 0.1f } },
                    },
                    new RoleDef
                    {
                        Id = "HOUSE", Rooms = { new RoleRoomDef { Id = "HOMEEC", Always = true } },
                        Modifiers = { new TraitModifier { Kind = TraitModifierKind.FoodSaving, Value = 1f } },
                    },
                    new RoleDef
                    {
                        Id = "MEDIC", Rooms = { new RoleRoomDef { Id = "INFIRMARY", Always = true } },
                        Modifiers = { new TraitModifier { Kind = TraitModifierKind.InfectionResist, Value = 7f } },
                    },
                    new RoleDef { Id = "SCOUT", Basic = true },
                },
            };
            return new InMemoryDefinitionDatabase(balance, TestContent.CreateActions(), new[] { TestContent.CreateModel() },
                TestContent.CreateItems(), new[] { TestContent.CreateLootTable() }, new[] { TestContent.CreateLocation() }, extras: extras);
        }

        private static GameState State(InMemoryDefinitionDatabase db, int seed = 1234, bool visited = true)
        {
            var state = TestContent.NewGame(db, seed);
            state.Difficulty = "";
            state.Calendar.Week = 6;
            if (visited)
            {
                state.Exploration.ActionCounts[TestContent.DayExpedition] = 1;
            }

            return state;
        }

        private static NpcState Join(GameState state, string id, string role = "STUDY")
        {
            var npc = new NpcState { Status = NpcStatus.Joined, Role = role, RoleSinceWeek = 1, Affinity = 10 };
            state.Npcs[id] = npc;
            return npc;
        }

        [Test]
        public void Plan_OneOrTwoDays_ScoutAlwaysTwo_NoneWithoutVisitedLocation()
        {
            var db = Db();
            for (var seed = 1; seed <= 20; seed++)
            {
                var state = State(db, seed);
                Join(state, A);
                Join(state, B, "SCOUT");
                var plan = OutingRules.Plan(state, db);
                Assert.That(plan[A].Count, Is.InRange(1, 2));
                Assert.AreEqual(2, plan[B].Count, "정찰은 늘 2번");
                Assert.IsTrue(plan[A].All(p => p.LocationId == TestContent.Location));
                CollectionAssert.AreEqual(plan[A].Select(p => p.Day), OutingRules.Plan(state, db)[A].Select(p => p.Day), "같은 판·같은 주면 같은 일정 (예고 = 실제)");
            }

            var fresh = State(db, visited: false);
            Join(fresh, A);
            Assert.IsEmpty(OutingRules.Plan(fresh, db), "가 본 곳이 없으면 나가지 않는다");
        }

        [Test]
        public void Plan_ExcludesHurtStressedDistrustfulHeldAndCat_DailyCapTwo()
        {
            var db = Db();
            var state = State(db);
            Join(state, "NPC_X_INJURED").InjuredWeeks = 1;
            Join(state, "NPC_X_MINOR").MinorWoundWeeks = 1;
            Join(state, "NPC_X_STRESS").Body.Stress = db.Balance.CompanionStressRoleThreshold;
            Join(state, "NPC_X_DISTRUST").Affinity = -1;
            Join(state, "NPC_X_HELD").StayHome = true;
            Join(state, "NPC_CAT");
            Assert.IsEmpty(OutingRules.Plan(state, db));

            var crowd = State(db);
            for (var i = 0; i < 6; i++)
            {
                Join(crowd, "NPC_S" + i, "SCOUT");
            }

            var perDay = OutingRules.Plan(crowd, db).SelectMany(p => p.Value).GroupBy(p => p.Day).Select(g => g.Count());
            Assert.IsTrue(perDay.All(n => n <= db.Balance.OutingDailyCap), "같은 날 낮에 많아야 2명");
        }

        [Test]
        public void DepartAndReturn_AwayDuringDay_BringsSupplies()
        {
            var db = Db(b => b.OutingInjuryChance = 0f);
            var state = State(db);
            var npc = Join(state, A);
            OutingRules.Schedule(state, db);
            var day = npc.OutingDays[0];
            state.Calendar.Day = day;
            state.Calendar.Phase = DayPhase.Day;
            var food = state.Base.GetResource(ResourceType.Food);

            CollectionAssert.AreEqual(new[] { A }, OutingRules.Depart(state, db));
            Assert.IsTrue(npc.Away);
            Assert.AreEqual(db.Balance.OutingStress, npc.Body.Stress);

            state.Calendar.Phase = DayPhase.Night;
            var back = OutingRules.Return(state, db, new StateDelta()).Single();
            Assert.IsFalse(npc.Away);
            Assert.AreEqual(OutingWound.None, back.Wound);
            var gained = back.Resources.Sum(r => r.Amount);
            Assert.Greater(gained, 0, "낮 산출 6~10·4~8의 OutingYieldShare만큼");
            Assert.LessOrEqual(gained, (int)System.Math.Ceiling((10 + 8) * db.Balance.OutingYieldShare) + 1);
            Assert.AreEqual(food + back.Resources.Where(r => r.Resource == ResourceType.Food).Sum(r => r.Amount), state.Base.GetResource(ResourceType.Food));
        }

        [Test]
        public void Wounds_MinorHealsNextWeekOrWithMedicine_SevereIsInjury()
        {
            var db = Db(b => { b.OutingInjuryChance = 1f; b.OutingSevereShare = 0f; });
            var state = State(db);
            var npc = Join(state, A);
            OutingRules.Schedule(state, db);
            state.Calendar.Day = npc.OutingDays[0];
            state.Calendar.Phase = DayPhase.Night;
            Assert.AreEqual(OutingWound.Minor, OutingRules.Return(state, db, new StateDelta()).Single().Wound);
            Assert.AreEqual(1, npc.MinorWoundWeeks);
            Assert.AreEqual(0, npc.InjuredWeeks, "가벼운 상처는 역할을 쉬지 않는다");
            Assert.IsFalse(OutingRules.CanGoOut(npc, A, db.Balance), "탐험만 쉰다");

            OutingRules.FinishWeek(state);
            Assert.AreEqual(1, npc.MinorWoundWeeks, "입은 주 결산에는 그대로");
            state.Calendar.Week++;
            OutingRules.FinishWeek(state);
            Assert.AreEqual(0, npc.MinorWoundWeeks, "다음 주 결산에 낫는다");

            npc.MinorWoundWeeks = 1;
            npc.MinorWoundWeek = state.Calendar.Week;
            state.Base.SetResource(ResourceType.Medicine, 1);
            GiveRules.Give(state, db.Balance, A, GiveKind.Medicine, null, new StateDelta(), out _);
            Assert.AreEqual(0, npc.MinorWoundWeeks, "의약품이면 바로 낫는다");

            var severeDb = Db(b => { b.OutingInjuryChance = 1f; b.OutingSevereShare = 1f; });
            var other = State(severeDb);
            var hurt = Join(other, B);
            OutingRules.Schedule(other, severeDb);
            other.Calendar.Day = hurt.OutingDays[0];
            other.Calendar.Phase = DayPhase.Night;
            Assert.AreEqual(OutingWound.Severe, OutingRules.Return(other, severeDb, new StateDelta()).Single().Wound);
            Assert.AreEqual(severeDb.Balance.OutingSevereWeeks, hurt.InjuredWeeks);
        }

        [Test]
        public void Away_RoleEffects_SlotZero_FoodDayZero_WeeklyScaled_TogetherAbsent()
        {
            var db = Db();
            var state = State(db);
            var student = Join(state, A);
            var cook = Join(state, B, "HOUSE");
            var medic = Join(state, C, "MEDIC");
            foreach (var npc in new[] { student, cook, medic })
            {
                npc.OutingDays.Add(3);
                npc.OutingLocations.Add(TestContent.Location);
            }

            state.Calendar.Day = 3;
            state.Calendar.Phase = DayPhase.Day;
            Assert.Greater(TraitRules.Sum(state, db, TraitModifierKind.EducationGain), 0f, "나가기 전 (아직 자리에 있다)");
            OutingRules.Depart(state, db);
            Assert.AreEqual(0f, TraitRules.Sum(state, db, TraitModifierKind.EducationGain), "나간 낮 칸: 슬롯 효과 없음");
            var together = TogetherRules.Apply(state, db, Lesson, new StateDelta()).Single();
            Assert.IsTrue(together.Absent);
            Assert.AreEqual(10, student.Affinity, "자리에 없으면 함께한 시간 없음");

            state.Calendar.Phase = DayPhase.Night;
            Assert.AreEqual(0f, TraitRules.Sum(state, db, TraitModifierKind.FoodSaving), "그날 저녁은 준비하지 못했다");
            OutingRules.Return(state, db, new StateDelta());
            Assert.Greater(TraitRules.Sum(state, db, TraitModifierKind.EducationGain), 0f, "밤에는 돌아와 있다");
            Assert.AreEqual(6f, TraitRules.Sum(state, db, TraitModifierKind.InfectionResist), 0.001f, "주 단위 효과는 나간 날만큼 (6/7)");
            state.Calendar.Day = 4;
            Assert.Greater(TraitRules.Sum(state, db, TraitModifierKind.FoodSaving), 0f);
        }

        [Test]
        public void HeldBackTwoWeeksInARow_Stressed()
        {
            var db = Db();
            var state = State(db);
            var npc = Join(state, A);
            npc.StayHome = true;
            Assert.IsTrue(OutingRules.WantsOut(state, db, A), "붙잡아도 나가고 싶어 하는 마음은 그대로 (서랍 토글)");
            Assert.IsFalse(OutingRules.Plan(state, db).ContainsKey(A));
            OutingRules.Schedule(state, db);
            Assert.IsEmpty(npc.OutingDays, "붙잡으면 나가지 않는다");
            Assert.AreEqual(0, npc.Body.Stress, "첫 주는 괜찮다");
            OutingRules.FinishWeek(state);
            Assert.IsFalse(npc.StayHome, "붙잡기는 한 주만");

            npc.StayHome = true;
            OutingRules.Schedule(state, db);
            Assert.AreEqual(db.Balance.OutingHeldBackStress, npc.Body.Stress, "2주 연속이면 답답하다");
        }
    }
}
