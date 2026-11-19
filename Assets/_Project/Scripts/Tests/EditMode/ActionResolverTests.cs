using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    public sealed class ActionResolverTests
    {
        private InMemoryDefinitionDatabase _db;
        private GameState _state;

        [SetUp]
        public void SetUp()
        {
            _db = TestContent.CreateDatabase();
            _state = TestContent.NewGame(_db);
        }

        private CheckResult Resolve(string actionId, params double[] rolls)
        {
            return ActionResolver.Resolve(_state, _db.GetAction(actionId), _db, new FixedRandom(rolls), new StateDelta());
        }

        [Test]
        public void Success_WithTeacherDirect_AppliesMultiplier()
        {
            // 확률 ≈ 0.6 + 10×0.004 − 페널티 ≈ 0.62, 굴림 0.5 → 성공
            Assert.AreEqual(CheckResult.Success, Resolve(TestContent.Humanities, 0.5));

            // 지력 3 × 1.5(직접 지도) = 4.5 → 5, 감수성 1 × 1.5 = 1.5 → 2
            Assert.AreEqual(15, _state.Player.GetStat(StatType.Intellect));
            Assert.AreEqual(12, _state.Player.GetStat(StatType.Empathy));
            Assert.AreEqual(5, _state.Player.Vitals.Fatigue);
            Assert.AreEqual(2, _state.Player.Vitals.Stress);
        }

        [Test]
        public void GreatSuccess_AndFailure_ScaleGains()
        {
            Assert.AreEqual(CheckResult.GreatSuccess, Resolve(TestContent.Humanities, 0.01));
            Assert.AreEqual(17, _state.Player.GetStat(StatType.Intellect)); // 3 × 1.5 × 1.5 = 6.75 → 7

            SetUp();
            Assert.AreEqual(CheckResult.Failure, Resolve(TestContent.Humanities, 0.99));
            Assert.AreEqual(12, _state.Player.GetStat(StatType.Intellect)); // 3 × 0.5 × 1.5 = 2.25 → 2
        }

        [Test]
        public void NoTeacher_RemovesTeachingBonus()
        {
            _state.Teacher.Alive = false;
            Resolve(TestContent.Humanities, 0.5);
            Assert.AreEqual(13, _state.Player.GetStat(StatType.Intellect));
        }

        [Test]
        public void Routine_SameEducationInARow_GetsBonus()
        {
            _state.Teacher.Alive = false;
            Resolve(TestContent.Humanities, 0.5); // 3 → 13
            Resolve(TestContent.Humanities, 0.5); // 3 × 1.1 = 3.3 → 3 → 16
            Resolve(TestContent.Humanities, 0.5); // 3 × 1.2 = 3.6 → 4 → 20
            Assert.AreEqual(20, _state.Player.GetStat(StatType.Intellect));
            Assert.AreEqual(2, _state.Player.RoutineStreak);

            Resolve(TestContent.MedicineStudy, 0.4);
            Assert.AreEqual(0, _state.Player.RoutineStreak);
        }

        [Test]
        public void Routine_BonusIsCapped_AndHigherForJ()
        {
            _state.Player.RoutineActionId = TestContent.Humanities;
            _state.Player.RoutineStreak = 10;
            _state.Teacher.Alive = false;
            Resolve(TestContent.Humanities, 0.5); // 3 × 1.3 = 3.9 → 4
            Assert.AreEqual(14, _state.Player.GetStat(StatType.Intellect));

            SetUp();
            _state.Player.Profile.PersonalityType = "INTJ";
            _state.Player.RoutineActionId = TestContent.Humanities;
            _state.Player.RoutineStreak = 10;
            _state.Teacher.Alive = false;
            _state.Player.SetStat(StatType.Intellect, 10);
            Resolve(TestContent.Humanities, 0.5); // 3 × 1.4 = 4.2 → 4
            Assert.AreEqual(14, _state.Player.GetStat(StatType.Intellect));
        }

        [Test]
        public void Proficiency_LevelsUpAtThreshold()
        {
            var delta = new StateDelta();
            ActionResolver.AddProficiencyXp(_state, ProficiencyType.Humanities, 9, _db.Balance, delta);
            Assert.AreEqual(0, _state.Player.ProficiencyLevel(ProficiencyType.Humanities));

            Resolve(TestContent.Humanities, 0.5);
            Assert.AreEqual(1, _state.Player.ProficiencyLevel(ProficiencyType.Humanities));
        }

        [Test]
        public void Failure_GivesNoProficiencyXp()
        {
            Resolve(TestContent.Humanities, 0.99);
            Assert.AreEqual(0, _state.Player.GetProficiency(ProficiencyType.Humanities).Xp);
        }

        [Test]
        public void InsufficientResources_FailsWithoutGains()
        {
            Assert.AreEqual(CheckResult.Failure, Resolve(TestContent.Craft));
            Assert.AreEqual(10, _state.Player.GetStat(StatType.Craft));

            _state.Base.SetResource(ResourceType.Materials, 5);
            Assert.AreEqual(CheckResult.Success, Resolve(TestContent.Craft));
            Assert.AreEqual(12, _state.Player.GetStat(StatType.Craft));
            Assert.AreEqual(3, _state.Base.GetResource(ResourceType.Materials));
        }

        [Test]
        public void SleepDebt_ReducesEfficiency()
        {
            _state.Teacher.Alive = false;
            _state.Player.SleepDebt = 1;
            Resolve(TestContent.Humanities, 0.3); // 3 × 0.9 = 2.7 → 3
            _state.Player.SetStat(StatType.Empathy, 10);
            Assert.AreEqual(13, _state.Player.GetStat(StatType.Intellect));
        }

        [Test]
        public void ReducedNextRest_HalvesRecoveryOnce()
        {
            _state.Player.Vitals.Fatigue = 50;
            _state.Player.ReducedNextRest = true;
            Resolve(TestContent.RestNight);
            Assert.AreEqual(42, _state.Player.Vitals.Fatigue); // −15 × 0.5 = −7.5 → −8
            Assert.IsFalse(_state.Player.ReducedNextRest);

            Resolve(TestContent.RestNight);
            Assert.AreEqual(27, _state.Player.Vitals.Fatigue);
        }

        [Test]
        public void Vitals_AreClamped()
        {
            Resolve(TestContent.RestNight);
            Assert.AreEqual(0, _state.Player.Vitals.Fatigue);
            Assert.AreEqual(0, _state.Player.Vitals.Stress);
        }
    }
}
