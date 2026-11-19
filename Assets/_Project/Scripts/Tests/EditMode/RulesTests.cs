using System.Collections.Generic;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Random;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    public sealed class SeededRandomTests
    {
        [Test]
        public void SameSeed_ProducesSameSequence()
        {
            var a = new SeededRandom(SeededRandom.CreateState(7));
            var b = new SeededRandom(SeededRandom.CreateState(7));
            for (var i = 0; i < 100; i++)
            {
                Assert.AreEqual(a.NextDouble(), b.NextDouble());
            }
        }

        [Test]
        public void RestoredState_ContinuesSameSequence()
        {
            var state = SeededRandom.CreateState(99);
            var random = new SeededRandom(state);
            random.NextDouble();
            var snapshot = new RandomState { Seed = state.Seed, State = state.State };

            var expected = new List<double> { random.NextDouble(), random.NextDouble(), random.NextDouble() };
            var resumed = new SeededRandom(snapshot);
            CollectionAssert.AreEqual(expected, new List<double> { resumed.NextDouble(), resumed.NextDouble(), resumed.NextDouble() });
        }

        [Test]
        public void Range_StaysWithinInclusiveBounds()
        {
            var random = new SeededRandom(SeededRandom.CreateState(3));
            for (var i = 0; i < 1000; i++)
            {
                var value = random.Range(-2, 5);
                Assert.That(value, Is.InRange(-2, 5));
                Assert.That(random.NextDouble(), Is.GreaterThanOrEqualTo(0.0).And.LessThan(1.0));
            }
        }
    }

    public sealed class StatRulesTests
    {
        private readonly BalanceConfig _balance = new BalanceConfig();

        [TestCase(0, 1f)]
        [TestCase(99, 1f)]
        [TestCase(100, 0.5f)]
        [TestCase(199, 0.5f)]
        [TestCase(200, 0.25f)]
        public void GrowthMultiplier_FollowsBands(int value, float expected)
        {
            Assert.AreEqual(expected, StatRules.GrowthMultiplierAt(value, _balance));
        }

        [Test]
        public void ScaleGain_AppliesMultiplierAndDiminishing()
        {
            Assert.AreEqual(5, StatRules.ScaleGain(10, 3, 1.5f, _balance)); // 4.5 → 5
            Assert.AreEqual(2, StatRules.ScaleGain(150, 3, 1f, _balance)); // 1.5 → 2
            Assert.AreEqual(0, StatRules.ScaleGain(250, 1, 1f, _balance)); // 0.25 → 0
        }

        [TestCase(0, 0)]
        [TestCase(9, 0)]
        [TestCase(10, 1)]
        [TestCase(44, 2)]
        [TestCase(45, 3)]
        [TestCase(100, 5)]
        [TestCase(500, 5)]
        public void LevelForXp_UsesThresholds(int xp, int level)
        {
            Assert.AreEqual(level, StatRules.LevelForXp(xp, _balance));
        }

        [Test]
        public void StageValue_ClampsToLastEntry()
        {
            Assert.AreEqual(0.15f, StatRules.StageValue(_balance.SleepDebtCheckPenalty, 9));
            Assert.AreEqual(0f, StatRules.StageValue(_balance.SleepDebtCheckPenalty, -1));
        }
    }

    public sealed class DerivedStatsTests
    {
        [Test]
        public void CombatPower_UsesFormula()
        {
            var balance = new BalanceConfig();
            var state = new GameState();
            state.Player.SetStat(StatType.Strength, 50);
            state.Player.SetStat(StatType.Agility, 40);
            state.Player.SetStat(StatType.Stamina, 30);
            state.Player.GetProficiency(ProficiencyType.SelfDefense).Level = 2;

            // 50×0.4 + 40×0.3 + 30×0.1 + 2×8 = 20 + 12 + 3 + 16
            Assert.AreEqual(51, DerivedStats.CombatPower(state, balance));
        }

        [Test]
        public void StressThreshold_IsCapped()
        {
            var balance = new BalanceConfig();
            var state = new GameState();
            state.Player.SetStat(StatType.Willpower, 20);
            Assert.AreEqual(56, DerivedStats.StressThreshold(state, balance)); // 50 + 6

            state.Player.SetStat(StatType.Willpower, 500);
            Assert.AreEqual(90, DerivedStats.StressThreshold(state, balance));
        }
    }

    public sealed class CheckResolverTests
    {
        private readonly BalanceConfig _balance = new BalanceConfig();

        [Test]
        public void Chance_IsClampedToMinAndMax()
        {
            var state = new GameState();
            state.Player.SetStat(StatType.Intellect, 999);
            var easy = new CheckDef { BaseRate = 0.5f, StatWeights = { new StatWeight(StatType.Intellect, 0.01f) } };
            Assert.AreEqual(0.95f, CheckResolver.ComputeChance(state, easy, _balance));

            state.Player.Vitals.Stress = 100;
            var hard = new CheckDef { BaseRate = 0f };
            Assert.AreEqual(0.05f, CheckResolver.ComputeChance(state, hard, _balance));
        }

        [Test]
        public void Chance_IncludesPenalties()
        {
            var state = new GameState();
            state.Player.Vitals.Stress = 50;
            state.Player.Vitals.Fatigue = 50;
            state.Player.SleepDebt = 3;
            var check = new CheckDef { BaseRate = 0.8f };

            // 0.8 − 0.15 − 0.1 − 0.1
            Assert.AreEqual(0.45f, CheckResolver.ComputeChance(state, check, _balance), 1e-5f);
        }

        [TestCase(0.05, CheckResult.GreatSuccess)]
        [TestCase(0.074, CheckResult.GreatSuccess)]
        [TestCase(0.076, CheckResult.Success)]
        [TestCase(0.49, CheckResult.Success)]
        [TestCase(0.5, CheckResult.Failure)]
        public void Roll_UsesGreatSuccessRatio(double roll, CheckResult expected)
        {
            Assert.AreEqual(expected, CheckResolver.Roll(0.5f, new FixedRandom(roll), _balance));
        }
    }
}
