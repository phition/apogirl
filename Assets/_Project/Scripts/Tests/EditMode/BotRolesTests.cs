using ApoGirl.Core.Definitions;
using ApoGirl.Core.Simulation.Bots;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>봇 역할 배정 (역할 재기획 P5): 적성 우선, 없으면 효과 없는 역할만 우선순위로 바꾼다.</summary>
    public sealed class BotRolesTests
    {
        private static InMemoryDefinitionDatabase Db()
        {
            var extras = new DefinitionExtras
            {
                Roles =
                {
                    new RoleDef { Id = "MEDIC", Rooms = { new RoleRoomDef { Id = "INFIRMARY", Always = true } } },
                    new RoleDef { Id = "STUDY", Rooms = { new RoleRoomDef { Id = "LIBRARY", Facility = "LIBRARY" } } },
                    new RoleDef { Id = "GUARD", Basic = true },
                },
            };
            return new InMemoryDefinitionDatabase(new BalanceConfig(), TestContent.CreateActions(), new[] { TestContent.CreateModel() },
                TestContent.CreateItems(), new[] { TestContent.CreateLootTable() }, new[] { TestContent.CreateLocation() }, extras: extras);
        }

        [Test]
        public void Aptitude_First_ThenPriorityOnlyWhenRoleDoesNothing()
        {
            var db = Db();
            var state = TestContent.NewGame(db, 7);
            state.Npcs["NPC_NURSE"] = new NpcState { Status = NpcStatus.Joined, Role = "GUARD" };
            state.Npcs["NPC_X_IDLE"] = new NpcState { Status = NpcStatus.Joined, Role = "STUDY" };   // 도서관이 닫혀 효과 없음
            state.Npcs["NPC_X_GUARD"] = new NpcState { Status = NpcStatus.Joined, Role = "GUARD" };
            state.Npcs["NPC_CAT"] = new NpcState { Status = NpcStatus.Joined, Role = "" };
            var config = new BotConfigDef { RoleAssignment = BotRoles.Aptitude, RolePriority = { "STUDY", "GUARD" }, RoleKeepNpcs = { "NPC_CAT" } };

            BotRoles.Assign(state, db, config);
            Assert.AreEqual("MEDIC", state.Npcs["NPC_NURSE"].Role, "간호 적성 → 보건실");
            Assert.AreEqual("GUARD", state.Npcs["NPC_X_IDLE"].Role, "닫힌 방 역할은 맡을 수 있는 다음 역할로");
            Assert.AreEqual("GUARD", state.Npcs["NPC_X_GUARD"].Role, "효과 있는 역할은 그대로");
            Assert.AreEqual("", state.Npcs["NPC_CAT"].Role, "고양이는 건드리지 않는다");

            var keep = TestContent.NewGame(db, 7);
            keep.Npcs["NPC_NURSE"] = new NpcState { Status = NpcStatus.Joined, Role = "GUARD" };
            BotRoles.Assign(keep, db, new BotConfigDef());
            Assert.AreEqual("GUARD", keep.Npcs["NPC_NURSE"].Role, "Keep이면 그대로");
        }
    }
}
