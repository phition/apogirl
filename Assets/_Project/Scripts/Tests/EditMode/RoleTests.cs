using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using ApoGirl.Systems.Save;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>
    /// 동료 역할: 정수 보정의 소수점 누적(살림·간호), 습격 방어 반올림,
    /// 역할 재기획 P1(Docs/ROLE_ROOMS_PROPOSAL.md) — 방 묶음·열린 방 배율·기본 역할·정원·적성·서는 방·세이브 v8 변환.
    /// </summary>
    public sealed class RoleTests
    {
        // 적성이 없는 동료 (밸런스 기본 적성 표에 없는 ID)
        private const string Plain = "NPC_TEST_A";
        private const string Plain2 = "NPC_TEST_B";
        private const string Plain3 = "NPC_TEST_C";

        private static RoleRoomDef Room(string id, string facility) => new RoleRoomDef { Id = id, Facility = facility };

        private static InMemoryDefinitionDatabase Db()
        {
            var extras = new DefinitionExtras
            {
                Roles =
                {
                    new RoleDef
                    {
                        Id = "GUARD", Basic = true, Rooms = { Room("ENTRANCE", "TEST_GATE") },
                        Modifiers = { new TraitModifier { Kind = TraitModifierKind.RaidGuard, Value = 6 } },
                    },
                    new RoleDef { Id = "HOUSE", Modifiers = { new TraitModifier { Kind = TraitModifierKind.FoodSaving, Value = 1 } } },
                    new RoleDef
                    {
                        Id = "MEDIC",
                        Modifiers =
                        {
                            new TraitModifier { Kind = TraitModifierKind.InfectionResist, Value = 1 },
                            new TraitModifier { Kind = TraitModifierKind.TeacherCare, Value = 1 },
                        },
                    },
                    new RoleDef
                    {
                        Id = "STUDY", Rooms = { Room("CLASSROOM", "CLASSROOM"), Room("LIBRARY", "LIBRARY"), Room("SCIENCE", "SCIENCE") },
                        Modifiers = { new TraitModifier { Kind = TraitModifierKind.EducationGain, Value = 0.08f } },
                    },
                    new RoleDef
                    {
                        Id = "SCOUT", Basic = true,
                        Modifiers =
                        {
                            new TraitModifier { Kind = TraitModifierKind.ExpeditionStress, Value = -0.1f },
                            new TraitModifier { Kind = TraitModifierKind.SearchSpeed, Value = 0.1f },
                        },
                    },
                },
            };
            return new InMemoryDefinitionDatabase(new BalanceConfig(), TestContent.CreateActions(), new[] { TestContent.CreateModel() },
                TestContent.CreateItems(), new[] { TestContent.CreateLootTable() }, new[] { TestContent.CreateLocation() }, extras: extras);
        }

        private static GameState NewState(InMemoryDefinitionDatabase db)
        {
            var state = TestContent.NewGame(db);
            state.Difficulty = ""; // 난이도 배율 없이 (감염 증가량을 그대로 비교)
            return state;
        }

        private static NpcState Join(GameState state, string id, string role, int affinity = 10, int since = 0)
        {
            var npc = new NpcState { Status = NpcStatus.Joined, Role = role, Affinity = affinity, RoleSinceWeek = since };
            state.Npcs[id] = npc;
            return npc;
        }

        private static GameState WithCompanion(InMemoryDefinitionDatabase db, string role, int affinity, string id = Plain)
        {
            var state = NewState(db);
            Join(state, id, role, affinity);
            return state;
        }

        [Test]
        public void Spread_KeepsFractionsOverTime()
        {
            int Total(float value, int steps)
            {
                var sum = 0;
                for (var i = 1; i <= steps; i++)
                {
                    sum += TraitRules.Spread(value, i);
                }

                return sum;
            }

            Assert.AreEqual(2, Total(0.5f, 4), "×0.5: 하루건너 1");
            Assert.AreEqual(5, Total(1.25f, 4));
            Assert.AreEqual(6, Total(1.5f, 4));
            Assert.AreEqual(4, Total(1f, 4));
            Assert.AreEqual(0, Total(0f, 4));
            Assert.AreEqual(0, Total(-1f, 4));
        }

        [Test]
        public void RoundInt_RoundsHalfUp()
        {
            Assert.AreEqual(8, TraitRules.RoundInt(7.5f));
            Assert.AreEqual(7, TraitRules.RoundInt(6.5f), "은행가 반올림이면 6");
            Assert.AreEqual(3, TraitRules.RoundInt(3f));
        }

        [Test]
        public void House_WithNegativeBond_StillSavesHalf()
        {
            var db = Db();
            var state = WithCompanion(db, "HOUSE", -5);
            var saving = TraitRules.Sum(state, db, TraitModifierKind.FoodSaving);
            Assert.AreEqual(0.5f, saving, 1e-5, "호감 음수 ×0.5");
            var total = 0;
            for (var step = 1; step <= 6; step++)
            {
                total += TraitRules.Spread(saving, step);
            }

            Assert.AreEqual(3, total, "예전에는 (int)0.5 = 0이라 하나도 아끼지 못했다");
        }

        [Test]
        public void Medic_HighBond_AveragesOneAndHalfPerWeek()
        {
            var db = Db();
            int Growth(string role, int week)
            {
                var state = WithCompanion(db, role, 90);
                state.Calendar.Week = week;
                state.Player.Vitals.Infection = 10;
                InfectionRules.WeeklyPlayer(state, db, new StateDelta());
                return state.Player.Vitals.Infection;
            }

            var reduced = 0;
            for (var week = 1; week <= 4; week++)
            {
                reduced += Growth("", week) - Growth("MEDIC", week);
            }

            Assert.AreEqual(6, reduced, "×1.5 → 4주에 6 (1,2,1,2)");
        }

        [Test]
        public void RoomBundle_NeedsOpenRoom_AndGrowsWithRooms()
        {
            var db = Db();
            var state = WithCompanion(db, "STUDY", 10);
            var study = RoleRules.Find(db, "STUDY");
            Assert.IsFalse(RoleRules.IsAvailable(state, study), "교실이 닫혀 있으면 맡길 수 없다");
            Assert.AreEqual(0f, TraitRules.Sum(state, db, TraitModifierKind.EducationGain), 1e-5, "방이 없으면 효과 없음");
            Assert.IsNull(RoleRules.RoomFor(state, db, Plain));

            state.Base.Facilities["CLASSROOM"] = 1;
            Assert.IsTrue(RoleRules.IsAvailable(state, study));
            Assert.AreEqual(0.08f, TraitRules.Sum(state, db, TraitModifierKind.EducationGain), 1e-5);
            Assert.AreEqual("CLASSROOM", RoleRules.RoomFor(state, db, Plain));

            state.Base.Facilities["LIBRARY"] = 1;
            Assert.AreEqual(0.10f, TraitRules.Sum(state, db, TraitModifierKind.EducationGain), 1e-5, "방 2개 ×1.25");
            state.Base.Facilities["SCIENCE"] = 1;
            Assert.AreEqual(0.12f, TraitRules.Sum(state, db, TraitModifierKind.EducationGain), 1e-5, "방 3개 ×1.5");
        }

        [Test]
        public void BasicRole_AvailableBeforeItsRoomOpens()
        {
            var db = Db();
            var state = WithCompanion(db, "GUARD", 10);
            Assert.IsTrue(RoleRules.IsAvailable(state, RoleRules.Find(db, "GUARD")));
            Assert.AreEqual(6f, TraitRules.Sum(state, db, TraitModifierKind.RaidGuard), 1e-5, "기본 역할은 방이 닫혀도 ×1");
            Assert.IsNull(RoleRules.RoomFor(state, db, Plain), "방이 열리기 전에는 지내는 곳에 선다");
            state.Base.Facilities["TEST_GATE"] = 1;
            Assert.AreEqual("ENTRANCE", RoleRules.RoomFor(state, db, Plain));
        }

        [Test]
        public void Capacity_LateHolderHasNoEffect_AndAssignSkipsFullRole()
        {
            var db = Db();
            var state = NewState(db);
            Join(state, Plain, "GUARD", since: 3);
            Join(state, Plain2, "GUARD", since: 5);
            Join(state, Plain3, "GUARD", since: 7); // 이벤트로 붙은 셋째 (정원 초과)
            Assert.IsTrue(RoleRules.IsActive(state, Plain, db.Balance));
            Assert.IsTrue(RoleRules.IsActive(state, Plain2, db.Balance));
            Assert.IsFalse(RoleRules.IsActive(state, Plain3, db.Balance), "늦게 붙은 사람은 효과 없음");
            Assert.AreEqual(12f, TraitRules.Sum(state, db, TraitModifierKind.RaidGuard), 1e-5);

            state.Npcs[Plain3].Role = "";
            Assert.IsFalse(RoleRules.Choices(state, db, Plain3).Exists(r => r.Id == "GUARD"), "정원이 찬 역할은 단추에서 빠진다");
            Assert.IsFalse(RoleRules.Assign(state, db, Plain3, "GUARD"));
            Assert.IsTrue(RoleRules.Choices(state, db, Plain).Exists(r => r.Id == "GUARD"), "이미 맡은 사람에게는 남는다");

            state.Npcs[Plain].Role = "";
            Assert.IsTrue(RoleRules.Assign(state, db, Plain3, "GUARD"), "자리가 나면 맡길 수 있다");
        }

        [Test]
        public void SecondHolder_StandsInNextOpenRoom()
        {
            var db = Db();
            var state = NewState(db);
            state.Base.Facilities["CLASSROOM"] = 1;
            state.Base.Facilities["SCIENCE"] = 1;
            Join(state, Plain, "STUDY", since: 1);
            Join(state, Plain2, "STUDY", since: 2);
            Assert.AreEqual("CLASSROOM", RoleRules.RoomFor(state, db, Plain));
            Assert.AreEqual("SCIENCE", RoleRules.RoomFor(state, db, Plain2), "닫힌 도서실은 건너뛴다");
        }

        [Test]
        public void Aptitude_Multiplies()
        {
            var db = Db();
            var nurse = WithCompanion(db, "MEDIC", 10, "NPC_NURSE");
            Assert.AreEqual(1.25f, TraitRules.Sum(nurse, db, TraitModifierKind.InfectionResist), 1e-5, "박 선생님 간호 ×1.25");
            var plain = WithCompanion(db, "MEDIC", 10);
            Assert.AreEqual(1f, TraitRules.Sum(plain, db, TraitModifierKind.InfectionResist), 1e-5);

            var scout = WithCompanion(db, "SCOUT", 10, "NPC_DOHYEON");
            Assert.AreEqual(-0.125f, TraitRules.Sum(scout, db, TraitModifierKind.ExpeditionStress), 1e-5, "도현 정찰 ×1.25");
            Assert.AreEqual(0.125f, TraitRules.ExplorationModifiers(scout, db)["SearchSpeed"], 1e-5);

            scout.Npcs["NPC_DOHYEON"].InjuredWeeks = 1;
            Assert.AreEqual(0f, TraitRules.Sum(scout, db, TraitModifierKind.ExpeditionStress), 1e-5, "다치면 역할 효과 없음");
        }

        [Test]
        public void SaveV7_MigratesOldRoleIds()
        {
            var db = Db();
            var state = NewState(db);
            Join(state, "NPC_DOHYEON", "GUARD");
            Join(state, "NPC_NURSE", "MEDIC");
            Join(state, "NPC_BYSTANDER", "MOOD");
            Join(state, "NPC_RINGLEADER", "COOK");
            Join(state, "NPC_SURVIVOR_A", "lookout");
            Join(state, Plain, "TUTOR");
            Join(state, Plain2, "MECHANIC");
            Join(state, Plain3, "student");
            var serializer = new GameStateSerializer();
            var json = Newtonsoft.Json.Linq.JObject.Parse(serializer.Serialize(state));
            json["SaveVersion"] = 7;
            var old = serializer.Deserialize(json.ToString());
            Assert.AreEqual(GameState.CurrentSaveVersion, old.SaveVersion);
            Assert.AreEqual("GUARD", old.Npcs["NPC_DOHYEON"].Role);
            Assert.AreEqual("MEDIC", old.Npcs["NPC_NURSE"].Role);
            Assert.AreEqual("COMFORT", old.Npcs["NPC_BYSTANDER"].Role);
            Assert.AreEqual("HOUSE", old.Npcs["NPC_RINGLEADER"].Role);
            Assert.AreEqual("GUARD", old.Npcs["NPC_SURVIVOR_A"].Role);
            Assert.AreEqual("STUDY", old.Npcs[Plain].Role);
            Assert.AreEqual("HOUSE", old.Npcs[Plain2].Role);
            Assert.AreEqual("", old.Npcs[Plain3].Role, "모르는 예전 이름은 역할 없음");
        }

        [Test]
        public void ContentRoles_MatchSixRoles()
        {
            var content = ApoGirl.Systems.Content.ContentJson.LoadFolder(ContentPipelineTests.ProjectPath(ApoGirl.Systems.Content.ContentJson.ContentFolder));
            CollectionAssert.AreEqual(new[] { "GUARD", "SCOUT", "MEDIC", "HOUSE", "STUDY", "COMFORT" }, content.Roles.ConvertAll(r => r.Id));
            Assert.IsTrue(content.Roles.Find(r => r.Id == "GUARD").Basic);
            Assert.IsTrue(content.Roles.Find(r => r.Id == "SCOUT").Basic);
            foreach (var role in content.Roles)
            {
                Assert.IsNotEmpty(role.Rooms, role.Id + " 방 묶음");
            }
        }
    }
}
