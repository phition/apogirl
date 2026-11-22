using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>호감이 떨어지는 경우 (기획 C.5): 오래 돌보지 않음, 다친 경계, 박 선생님·선배·윤 선생님이 싫어하는 것, 화해 보너스.</summary>
    public sealed class BondDecayTests
    {
        private static (GameState State, InMemoryDefinitionDatabase Db) Make()
        {
            var db = TestContent.CreateDatabase();
            var state = TestContent.NewGame(db);
            state.Calendar.Week = 10;
            return (state, db);
        }

        private static NpcState Join(GameState state, string id, int affinity)
        {
            var npc = new NpcState { Status = NpcStatus.Joined, Affinity = affinity };
            state.Npcs[id] = npc;
            return npc;
        }

        [Test]
        public void Neglect_After6Weeks_StopsAtFloor()
        {
            var (state, db) = Make();
            var npc = Join(state, "NPC_DOHYEON", 51);
            BondDecayRules.ApplyWeekly(state, db, new StateDelta());
            Assert.AreEqual(51, npc.Affinity, "처음 보는 주는 세기 시작만");
            state.Calendar.Week += db.Balance.NeglectWeeks;
            BondDecayRules.ApplyWeekly(state, db, new StateDelta());
            Assert.AreEqual(db.Balance.NeglectFloor, npc.Affinity, "−2지만 50 아래로는 내리지 않는다");
            BondDecayRules.ApplyWeekly(state, db, new StateDelta());
            Assert.AreEqual(db.Balance.NeglectFloor, npc.Affinity);
        }

        [Test]
        public void InjuredGuard_Drops_AndReconcileDoubles()
        {
            var (state, db) = Make();
            var npc = Join(state, "NPC_DOHYEON", 20);
            npc.Role = "GUARD";
            npc.InjuredWeeks = 2;
            BondDecayRules.ApplyWeekly(state, db, new StateDelta());
            Assert.AreEqual(20 - db.Balance.InjuredGuardAffinity, npc.Affinity);

            npc.Role = "";
            state.Base.SetResource(ResourceType.Food, 5);
            var before = npc.Affinity;
            Assert.IsTrue(GiveRules.Give(state, db.Balance, "NPC_DOHYEON", GiveKind.Food, null, new StateDelta(), out _));
            Assert.AreEqual(before + db.Balance.GiveAffinityBasic * 2, npc.Affinity, "서먹해진 뒤 처음 챙기면 배로");
        }

        [Test]
        public void Nurse_DislikesUntreatedInjury()
        {
            var (state, db) = Make();
            var nurse = Join(state, BondDecayRules.NurseId, 30);
            Join(state, "NPC_DOHYEON", 30).InjuredWeeks = 1;
            state.Base.SetResource(ResourceType.Medicine, 2);
            BondDecayRules.ApplyWeekly(state, db, new StateDelta());
            Assert.AreEqual(30 - db.Balance.NurseUntreatedAffinity, nurse.Affinity);

            state.Calendar.Week++;
            var treated = nurse.Affinity;
            Assert.IsTrue(GiveRules.Give(state, db.Balance, "NPC_DOHYEON", GiveKind.Medicine, null, new StateDelta(), out _));
            BondDecayRules.ApplyWeekly(state, db, new StateDelta());
            Assert.AreEqual(treated, nurse.Affinity, "약을 주면 괜찮다");
        }

        [Test]
        public void Senior_TwoEmptyRunsInARow()
        {
            var (state, db) = Make();
            var senior = Join(state, BondDecayRules.SeniorId, 30);
            var empty = new ExpeditionResult { Status = ExtractStatus.Extracted };
            var full = new ExpeditionResult { Status = ExtractStatus.Extracted, ResourceLoot = { new ResourceAmount(ResourceType.Food, 3) } };
            BondDecayRules.OnExpedition(state, empty, db.Balance, new StateDelta());
            BondDecayRules.OnExpedition(state, full, db.Balance, new StateDelta());
            BondDecayRules.OnExpedition(state, empty, db.Balance, new StateDelta());
            Assert.AreEqual(30, senior.Affinity, "연속이 아니면 괜찮다");
            BondDecayRules.OnExpedition(state, empty, db.Balance, new StateDelta());
            Assert.AreEqual(30 - db.Balance.SeniorEmptyAffinity, senior.Affinity);
        }

        [Test]
        public void Teacher_NoLessonsThreeWeeks()
        {
            var (state, db) = Make();
            state.Events.Records[db.Balance.OpeningEventId] = new EventRecord { Count = 1, LastWeek = 1 };
            foreach (var slot in state.Plan.Slots)
            {
                slot.ActionId = TestContent.RestDay;
            }

            state.Teacher.Affinity = 40;
            for (var i = 0; i < db.Balance.NoLessonWeeks - 1; i++)
            {
                BondDecayRules.ApplyWeekly(state, db, new StateDelta());
            }

            Assert.AreEqual(40, state.Teacher.Affinity);
            BondDecayRules.ApplyWeekly(state, db, new StateDelta());
            Assert.AreEqual(40 - db.Balance.NoLessonAffinity, state.Teacher.Affinity);
        }
    }
}
