using ApoGirl.Core.Definitions;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>행동 스트레스 (사용자 결정 2026-11-25): 기본 1~10, 피로·부상·양심·수면 부채 보정, 한 번 최대 20, 난이도 ±20%.</summary>
    public sealed class StressRulesTests
    {
        private static GameState Fresh(BalanceConfig balance, string difficulty = DifficultyRules.Standard)
        {
            var state = NewGameFactory.Create(balance, 1);
            state.Difficulty = difficulty;
            state.Player.Vitals.Fatigue = 0;
            state.Player.Conscience = 0;
            state.Player.SleepDebt = 0;
            state.Flags.Remove(balance.InjuredFlag);
            return state;
        }

        [Test]
        public void RestStaysAsIs_WorkCostsAtLeastOne()
        {
            var balance = new BalanceConfig();
            var state = Fresh(balance);
            Assert.AreEqual(0, StressRules.Hit(state, balance, 0), "쉬는 일정은 0");
            Assert.AreEqual(-5, StressRules.Hit(state, balance, -5), "회복은 손대지 않는다");
            Assert.AreEqual(1, StressRules.Hit(state, balance, 1));
            Assert.AreEqual(1, StressRules.Hit(state, balance, 1, relief: 1), "좋아하는 과목으로 빼도 최소 1");
            Assert.AreEqual(10, StressRules.Hit(state, balance, 10));
        }

        [Test]
        public void Modifiers_AddUp_CappedAtTwenty()
        {
            var balance = new BalanceConfig();
            var state = Fresh(balance);

            state.Player.Vitals.Fatigue = 70;          // +25%
            Assert.AreEqual(5, StressRules.Hit(state, balance, 4));
            state.Flags.Add(balance.InjuredFlag);      // +25%
            state.Player.Conscience = 4;               // +20%
            state.Player.SleepDebt = 3;                // +25%
            Assert.AreEqual(1.95f, 1f + StressRules.Bonus(state, balance), 1e-4f);
            Assert.AreEqual(20, StressRules.Hit(state, balance, 10), "10 × 1.95 = 19.5 → 20");

            state.Player.Vitals.Fatigue = 95;          // +40%
            state.Player.Conscience = 10;              // +50%
            state.Player.SleepDebt = 5;                // +50%
            Assert.AreEqual(20, StressRules.Hit(state, balance, 10), "한 번에 최대 20");
        }

        [Test]
        public void Difficulty_StoryAndHard_PlusMinusTwentyPercent()
        {
            var balance = new BalanceConfig();
            Assert.AreEqual(8, StressRules.Hit(Fresh(balance, DifficultyRules.Story), balance, 10));
            Assert.AreEqual(12, StressRules.Hit(Fresh(balance, DifficultyRules.Hard), balance, 10));
            Assert.AreEqual(1, StressRules.Hit(Fresh(balance, DifficultyRules.Story), balance, 1), "이야기 중심도 최소 1");

            var story = Fresh(balance, DifficultyRules.Story);
            var hard = Fresh(balance, DifficultyRules.Hard);
            foreach (var state in new[] { story, hard })
            {
                state.Player.Vitals.Fatigue = 95;
                state.Player.Conscience = 10;
                state.Player.SleepDebt = 5;
                state.Flags.Add(balance.InjuredFlag);
            }

            Assert.AreEqual(16, StressRules.Hit(story, balance, 10), "이야기 중심 상한 16");
            Assert.AreEqual(24, StressRules.Hit(hard, balance, 10), "하드 상한 24");
        }
    }
}
