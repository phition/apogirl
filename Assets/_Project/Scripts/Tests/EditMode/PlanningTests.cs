using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Planning;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    public sealed class PlanningTests
    {
        private InMemoryDefinitionDatabase _db;
        private GameState _state;

        [SetUp]
        public void SetUp()
        {
            _db = TestContent.CreateDatabase();
            _state = TestContent.NewGame(_db);
        }

        private PlannedSlot Slot(int index) => _state.Plan.Slots[index];

        [Test]
        public void DayExpedition_MarksSameNightAsForcedRest_AndFillsRest()
        {
            Slot(4).ActionId = TestContent.DayExpedition; // 3일차 낮
            PlanPreparer.ApplyForcedRest(_state, _db);

            Assert.IsTrue(Slot(5).ForcedRest);
            Assert.AreEqual(TestContent.RestNight, Slot(5).ActionId);
            Assert.IsFalse(Slot(5).Locked);
        }

        [Test]
        public void NightExpedition_MarksNextDayAsForcedRest()
        {
            Slot(5).ActionId = TestContent.NightExpedition; // 3일차 밤
            PlanPreparer.ApplyForcedRest(_state, _db);

            Assert.IsTrue(Slot(6).ForcedRest);
            Assert.AreEqual(TestContent.RestDay, Slot(6).ActionId);
        }

        [Test]
        public void PlayerActionInForcedRestSlot_IsKept_AndWarned()
        {
            Slot(4).ActionId = TestContent.DayExpedition;
            Slot(5).ActionId = TestContent.Humanities;
            PlanPreparer.ApplyForcedRest(_state, _db);

            Assert.AreEqual(TestContent.Humanities, Slot(5).ActionId);
            var warnings = PlanValidator.Validate(_state, _db);
            Assert.IsTrue(warnings.Any(w => w.Type == PlanWarningType.ForcedRestViolation && w.SlotIndex == 5));
        }

        [Test]
        public void ConsecutiveExpedition_IsWarned_AsHighIntensityToo()
        {
            Slot(4).ActionId = TestContent.DayExpedition;
            Slot(5).ActionId = TestContent.NightExpedition;
            PlanPreparer.ApplyForcedRest(_state, _db);

            var warnings = PlanValidator.Validate(_state, _db);
            Assert.IsTrue(warnings.Any(w => w.Type == PlanWarningType.ConsecutiveExpedition && w.SlotIndex == 5));
            Assert.IsTrue(warnings.Any(w => w.Type == PlanWarningType.ForcedRestViolation && w.SlotIndex == 5));
            Assert.IsTrue(warnings.Any(w => w.Type == PlanWarningType.HighIntensityWeek));
        }

        [Test]
        public void SlotTagMismatch_AndUnknownAction_AreWarned()
        {
            Slot(1).ActionId = TestContent.PurifyWater; // 낮 전용을 밤에
            Slot(2).ActionId = "ACT_DOES_NOT_EXIST";

            var warnings = PlanValidator.Validate(_state, _db);
            Assert.IsTrue(warnings.Any(w => w.Type == PlanWarningType.SlotTagMismatch && w.SlotIndex == 1));
            Assert.IsTrue(warnings.Any(w => w.Type == PlanWarningType.UnknownAction && w.SlotIndex == 2));
        }

        [Test]
        public void NightExpedition_WithSleepDebt2_IsWarned()
        {
            _state.Player.SleepDebt = 2;
            Slot(3).ActionId = TestContent.NightExpedition;
            var warnings = PlanValidator.Validate(_state, _db);
            Assert.IsTrue(warnings.Any(w => w.Type == PlanWarningType.NightExpeditionWithSleepDebt));
        }

        [Test]
        public void ClearingExpedition_RemovesForcedRestFlag()
        {
            Slot(4).ActionId = TestContent.DayExpedition;
            PlanPreparer.ApplyForcedRest(_state, _db);
            Slot(4).ActionId = TestContent.Humanities;
            PlanPreparer.ApplyForcedRest(_state, _db);
            Assert.IsFalse(Slot(5).ForcedRest);
        }

        [Test]
        public void NewWeekPlan_AppliesCarryOverLocks()
        {
            _state.CarryOver.LockedRestSlots = 3;
            var plan = PlanPreparer.CreateWeekPlan(_state, _db.Balance);

            Assert.IsTrue(plan.Slots.Take(3).All(s => s.Locked && s.ForcedRest));
            Assert.AreEqual(TestContent.RestDay, plan.Slots[0].ActionId);
            Assert.AreEqual(TestContent.RestNight, plan.Slots[1].ActionId);
            Assert.IsFalse(plan.Slots[3].Locked);
            Assert.AreEqual(0, _state.CarryOver.LockedRestSlots);
        }

        [Test]
        public void NewWeekPlan_SleepDebtStage4_LocksOneDaySlot()
        {
            _state.Player.SleepDebt = 4;
            var plan = PlanPreparer.CreateWeekPlan(_state, _db.Balance);
            Assert.AreEqual(1, plan.Slots.Count(s => s.Locked));
            Assert.IsTrue(plan.Slots[0].Locked);
        }
    }
}
