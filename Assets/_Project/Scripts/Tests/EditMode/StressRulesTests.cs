using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>행동 스트레스 (사용자 결정 2026-11-25): 행동 단계 → 5~10, 탐험 10~15, 피로·부상·양심·수면 부채 보정, 상한 없음, 난이도 ±20%, 휴식 회복 밤 10·낮잠 절반.</summary>
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
        public void ActionLevels_MapToFiveToTen()
        {
            var balance = new BalanceConfig();
            var state = Fresh(balance);
            CollectionAssert.AreEqual(new[] { 5, 6, 8, 9, 10 }, new[] { 1, 2, 3, 4, 5 }.Select(l => StressRules.ActionBase(balance, l)).ToArray());
            Assert.AreEqual(0, StressRules.ActionBase(balance, 0), "쉬는 일정은 0");
            Assert.AreEqual(10, StressRules.ActionBase(balance, 9), "마지막 단계 위는 마지막 단계");
            Assert.AreEqual(5, StressRules.HitAction(state, balance, 1));
            Assert.AreEqual(4, StressRules.HitAction(state, balance, 1, relief: 1), "좋아하는 과목 −1");

            balance.ActionStressMin = 2;
            balance.ActionStressMax = 6;
            CollectionAssert.AreEqual(new[] { 2, 3, 4, 5, 6 }, new[] { 1, 2, 3, 4, 5 }.Select(l => StressRules.ActionBase(balance, l)).ToArray(), "밸런스 표 값을 따른다");
        }

        [Test]
        public void Modifiers_AddUp_NoCap()
        {
            var balance = new BalanceConfig();
            var state = Fresh(balance);

            state.Player.Vitals.Fatigue = 70;          // +25%
            Assert.AreEqual(5, StressRules.Hit(state, balance, 4));
            state.Flags.Add(balance.InjuredFlag);      // +25%
            state.Player.Conscience = 4;               // +20%
            state.Player.SleepDebt = 3;                // +25%
            Assert.AreEqual(1.95f, 1f + StressRules.Bonus(state, balance), 1e-4f);
            Assert.AreEqual(29, StressRules.Hit(state, balance, 15), "15 × 1.95 = 29.25 → 29");

            state.Player.Vitals.Fatigue = 95;          // +40%
            state.Player.Conscience = 10;              // +50%
            state.Player.SleepDebt = 5;                // +50%
            Assert.AreEqual(40, StressRules.Hit(state, balance, 15), "15 × 2.65 = 39.75 → 40, 상한 없음");
        }

        [Test]
        public void Difficulty_StoryAndHard_PlusMinusTwentyPercent()
        {
            var balance = new BalanceConfig();
            Assert.AreEqual(8, StressRules.Hit(Fresh(balance, DifficultyRules.Story), balance, 10));
            Assert.AreEqual(12, StressRules.Hit(Fresh(balance, DifficultyRules.Hard), balance, 10));
            Assert.AreEqual(1, StressRules.Hit(Fresh(balance, DifficultyRules.Story), balance, 1), "이야기 중심도 최소 1");
        }

        [Test]
        public void RestRelief_NightTen_DayHalf()
        {
            var balance = new BalanceConfig();
            var night = new ActionDef { Id = balance.NightRestActionId, Category = ActionCategory.Rest };
            var day = new ActionDef { Id = balance.DayRestActionId, Category = ActionCategory.Rest };
            Assert.AreEqual(10, StressRules.RestRelief(balance, night));
            Assert.AreEqual(5, StressRules.RestRelief(balance, day));
            Assert.AreEqual(0, StressRules.RestRelief(balance, new ActionDef { Id = "ACT_TRIP_NIGHT", Category = ActionCategory.Rest }), "소풍 등은 데이터 값 그대로");

            balance.NightRestStress = 16;
            balance.DayRestStressRatio = 0.25f;
            Assert.AreEqual(16, StressRules.RestRelief(balance, night));
            Assert.AreEqual(4, StressRules.RestRelief(balance, day));
        }
    }
}
