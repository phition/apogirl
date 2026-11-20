using System;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Random;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>고정 계획으로 1년 전체를 돌려 예외·불변식 위반이 없는지 확인한다.</summary>
    public sealed class FullYearSmokeTests
    {
        [TestCase(1)]
        [TestCase(2026)]
        [TestCase(31337)]
        public void FullYear_RunsWithoutViolatingInvariants(int seed)
        {
            var db = TestContent.CreateDatabase();
            var state = NewGameFactory.Create(db.Balance, seed);
            var runner = new WeekRunner(state, db, new SeededRandom(state.Rng), new HeadlessExpeditionRunner(db));
            var weeks = 0;

            while (state.Status == GameStatus.Playing)
            {
                FillPlan(state);
                runner.PrepareWeek();
                while (!runner.IsWeekComplete && !runner.IsGameOver)
                {
                    runner.ExecuteNextSlot();
                    AssertInvariants(state, db.Balance);
                }

                if (runner.IsGameOver)
                {
                    break;
                }

                runner.FinishWeek();
                weeks++;
                Assert.Less(weeks, 60, "무한 루프 방지");
            }

            Assert.AreNotEqual(GameStatus.Playing, state.Status);
            if (state.Status == GameStatus.Rescued)
            {
                Assert.AreEqual(52, weeks);
                Assert.AreEqual(52, state.Diary.Count);
            }
        }

        /// <summary>낮: 교육·물 정화·낮 탐험 2회 / 밤: 휴식 (탐험 직후 강제 휴식은 PrepareWeek가 채움).</summary>
        private static void FillPlan(GameState state)
        {
            string[] day = { TestContent.Humanities, TestContent.PurifyWater, TestContent.DayExpedition, TestContent.MedicineStudy, TestContent.DayExpedition, TestContent.PurifyWater, TestContent.Humanities };
            for (var d = 0; d < 7; d++)
            {
                var daySlot = state.Plan.Slots[d * 2];
                if (!daySlot.Locked)
                {
                    daySlot.ActionId = day[d];
                }
            }
        }

        private static void AssertInvariants(GameState state, BalanceConfig balance)
        {
            foreach (StatType stat in Enum.GetValues(typeof(StatType)))
            {
                Assert.That(state.Player.GetStat(stat), Is.InRange(0, balance.StatMax));
            }

            foreach (VitalType vital in Enum.GetValues(typeof(VitalType)))
            {
                // 포만·수분은 0이 가운데 (−최대 ~ +최대)
                var min = vital == VitalType.Satiety || vital == VitalType.Hydration ? -balance.VitalMax : 0;
                Assert.That(state.Player.Vitals.GetVital(vital), Is.InRange(min, balance.VitalMax));
            }

            foreach (ResourceType resource in Enum.GetValues(typeof(ResourceType)))
            {
                Assert.That(state.Base.GetResource(resource), Is.GreaterThanOrEqualTo(0));
            }

            Assert.That(state.Player.SleepDebt, Is.InRange(0, balance.SleepDebtMax));
            Assert.That(state.Player.Conscience, Is.InRange(0, balance.ConscienceMax));
            Assert.That(state.Calendar.Week, Is.InRange(1, 52));
        }
    }
}
