using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>M0 스모크 테스트: 테스트 파이프라인과 GameState 기본값 확인.</summary>
    public sealed class GameStateDefaultsTests
    {
        [Test]
        public void NewGameState_StartsAtWeek1Day1Daytime()
        {
            var state = new GameState();

            Assert.AreEqual(GameState.CurrentSaveVersion, state.SaveVersion);
            Assert.AreEqual(1, state.Calendar.Week);
            Assert.AreEqual(1, state.Calendar.Day);
            Assert.AreEqual(DayPhase.Day, state.Calendar.Phase);
        }

        [Test]
        public void NewWeekPlan_Has14EmptySlots()
        {
            var plan = new WeekPlan();

            Assert.AreEqual(14, plan.Slots.Count);
            Assert.That(plan.Slots, Has.All.Matches<PlannedSlot>(s => s.ActionId == "" && !s.Locked));
        }

        [Test]
        public void NewTeacher_IsAliveAndNotReinfected()
        {
            var teacher = new GameState().Teacher;

            Assert.IsTrue(teacher.Alive);
            Assert.IsFalse(teacher.Reinfected);
        }
    }
}
