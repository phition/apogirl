using System;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    public sealed class WeekRunnerTests
    {
        private InMemoryDefinitionDatabase _db;
        private GameState _state;
        private StubExpeditionRunner _expedition;
        private WeekRunner _runner;

        [SetUp]
        public void SetUp()
        {
            _db = TestContent.CreateDatabase();
            _state = TestContent.NewGame(_db);
            _expedition = new StubExpeditionRunner();
            _runner = new WeekRunner(_state, _db, new FixedRandom { Default = 0.5 }, _expedition);
        }

        private void RunSlots(int count)
        {
            for (var i = 0; i < count; i++)
            {
                _runner.ExecuteNextSlot();
            }
        }

        [Test]
        public void ExecutesFourteenSlotsInOrder_ThenWeekComplete()
        {
            var first = _runner.ExecuteNextSlot();
            Assert.AreEqual(1, first.Day);
            Assert.AreEqual(DayPhase.Day, first.Phase);
            Assert.AreEqual(TestContent.RestDay, first.ActionId, "빈 슬롯은 기본 휴식");

            var second = _runner.ExecuteNextSlot();
            Assert.AreEqual(DayPhase.Night, second.Phase);

            RunSlots(12);
            Assert.IsTrue(_runner.IsWeekComplete);
            Assert.Throws<InvalidOperationException>(() => _runner.ExecuteNextSlot());
        }

        [Test]
        public void DailyUpkeep_ConsumesFoodAndWaterEachNight()
        {
            RunSlots(14);
            Assert.AreEqual(0, _state.Base.GetResource(ResourceType.Food)); // 14 − 2×7
            Assert.AreEqual(6, _state.Base.GetResource(ResourceType.Water)); // 20 − 2×7
            Assert.AreEqual(5, _state.Base.GetResource(ResourceType.Power), "전력 복구 전에는 소모 없음");
        }

        [Test]
        public void Shortage_RaisesHunger_AndStarvationCostsHp()
        {
            _state.Base.SetResource(ResourceType.Food, 0);
            RunSlots(2);
            Assert.AreEqual(-_db.Balance.SatietyDailyDrain, _state.Player.Vitals.Satiety);
            RunSlots(4); // 3일째 밤: 포만 −75 (굶주림) → HP 감소
            Assert.AreEqual(-3 * _db.Balance.SatietyDailyDrain, _state.Player.Vitals.Satiety);
            Assert.AreEqual(100 - _db.Balance.StarvationHpLoss, _state.Player.Vitals.Hp);
        }

        [Test]
        public void WeeklyPower_ConsumedOnlyAfterRestore()
        {
            _state.Base.PowerRestored = true;
            RunSlots(14);
            _runner.FinishWeek();
            Assert.AreEqual(2, _state.Base.GetResource(ResourceType.Power));
        }

        [Test]
        public void FinishWeek_AdvancesWeek_AndWritesDiary()
        {
            RunSlots(14);
            var summary = _runner.FinishWeek();

            Assert.AreEqual(1, summary.Week);
            Assert.AreEqual(14, summary.Slots.Count);
            Assert.AreEqual(2, _state.Calendar.Week);
            Assert.AreEqual(1, _state.Calendar.Day);
            Assert.IsFalse(_state.Calendar.WeekComplete);
            Assert.AreEqual("DIARY_W01", _state.Diary[0].TextKey);
            Assert.AreEqual(-14, summary.Delta.ResourceChange(ResourceType.Food));
        }

        [Test]
        public void Week52_Finish_SetsRescued()
        {
            _state.Calendar.Week = 52;
            RunSlots(14);
            var summary = _runner.FinishWeek();
            Assert.AreEqual(GameStatus.Rescued, summary.StatusAfter);
            Assert.AreEqual(52, _state.Calendar.Week);
        }

        [TestCase(13, Season.Spring)]
        [TestCase(14, Season.Summer)]
        [TestCase(39, Season.Autumn)]
        [TestCase(40, Season.Winter)]
        [TestCase(52, Season.Winter)]
        public void Season_Boundaries(int week, Season season)
        {
            Assert.AreEqual(season, CalendarRules.GetSeason(week));
        }

        [Test]
        public void NightExpedition_AddsSleepDebt_AndCountsTag()
        {
            _state.Plan.Slots[1].ActionId = TestContent.NightExpedition;
            RunSlots(2);
            Assert.AreEqual(1, _state.Player.SleepDebt);
            Assert.AreEqual(1, _state.Exploration.NightExplorations);
            Assert.AreEqual(TestContent.Location, _expedition.Requests[0].LocationId);
            Assert.AreEqual(ExpeditionTime.Night, _expedition.Requests[0].TimeOfDay);
        }

        [Test]
        public void SleepDebt_DoesNotDecay_WithDayRest_AndRepaysAfterTwoNightRests()
        {
            _state.Player.SleepDebt = 3;
            RunSlots(1); // 낮 휴식
            Assert.AreEqual(3, _state.Player.SleepDebt);
            RunSlots(2); // 밤 휴식 1회
            Assert.AreEqual(3, _state.Player.SleepDebt);
            RunSlots(2); // 밤 휴식 2회 연속 → −1
            Assert.AreEqual(2, _state.Player.SleepDebt);
        }

        [Test]
        public void ForcedRestViolation_AppliesPenalty_AndSleepDebt()
        {
            _state.Plan.Slots[0].ActionId = TestContent.DayExpedition;
            _state.Plan.Slots[1].ActionId = TestContent.Humanities;
            _runner.PrepareWeek();
            RunSlots(2);

            Assert.AreEqual(1, _state.Player.SleepDebt);
        }

        [Test]
        public void ConsecutiveExpedition_AddsTwoDebt_PlusNightDebt()
        {
            _state.Plan.Slots[0].ActionId = TestContent.DayExpedition;
            _state.Plan.Slots[1].ActionId = TestContent.NightExpedition;
            _runner.PrepareWeek();
            RunSlots(1);
            var outcome = _runner.ExecuteNextSlot();

            Assert.IsTrue(outcome.ConsecutiveExpedition);
            Assert.IsTrue(_expedition.Requests[1].ConsecutiveRun);
            Assert.AreEqual(3, _state.Player.SleepDebt); // 연속 +2, 밤 탐험 +1
        }

        [Test]
        public void SleepDebt_IsCappedAtFive()
        {
            _state.Player.SleepDebt = 5;
            _state.Plan.Slots[1].ActionId = TestContent.NightExpedition;
            RunSlots(2);
            Assert.AreEqual(5, _state.Player.SleepDebt);
        }

        [Test]
        public void Day7NightExpedition_LocksNextWeekFirstSlot()
        {
            _state.Plan.Slots[13].ActionId = TestContent.NightExpedition;
            RunSlots(14);
            _runner.FinishWeek();

            Assert.IsTrue(_state.Plan.Slots[0].Locked);
            Assert.AreEqual(TestContent.RestDay, _state.Plan.Slots[0].ActionId);
            Assert.IsFalse(_state.Plan.Slots[1].Locked);
        }

        [Test]
        public void Down_LocksFourFollowingSlots_AcrossWeekBoundary()
        {
            _expedition.Handler = _ => new ExpeditionResult { Status = ExtractStatus.Down, HpDelta = -30 };
            _state.Plan.Slots[12].ActionId = TestContent.DayExpedition; // 7일차 낮
            RunSlots(13);

            Assert.IsTrue(_state.Plan.Slots[13].Locked);
            Assert.AreEqual(1, _state.Exploration.Downs);
            RunSlots(1);
            _runner.FinishWeek();

            for (var i = 0; i < 3; i++)
            {
                Assert.IsTrue(_state.Plan.Slots[i].Locked, $"다음 주 슬롯 {i}");
            }

            Assert.IsFalse(_state.Plan.Slots[3].Locked);
        }

        [Test]
        public void DawnReturn_ReducesNextRestRecovery()
        {
            _expedition.Handler = _ => new ExpeditionResult { Status = ExtractStatus.Extracted, SleepDebtDelta = 1, FatigueDelta = 20 };
            _state.Plan.Slots[1].ActionId = TestContent.NightExpedition;
            RunSlots(2);
            Assert.AreEqual(2, _state.Player.SleepDebt);
            Assert.IsTrue(_state.Player.ReducedNextRest);

            RunSlots(1); // 다음 날 낮 휴식: −10 × 0.5
            Assert.AreEqual(15, _state.Player.Vitals.Fatigue);
        }

        [Test]
        public void ExpeditionLoot_IsAddedToBase()
        {
            _expedition.Handler = _ => new ExpeditionResult
            {
                Status = ExtractStatus.Extracted,
                ResourceLoot = { new ResourceAmount(ResourceType.Medicine, 3) },
                Clues = { "CLUE_TEST" },
            };
            _state.Plan.Slots[0].ActionId = TestContent.DayExpedition;
            RunSlots(1);
            Assert.AreEqual(5, _state.Base.GetResource(ResourceType.Medicine));
            Assert.IsTrue(_state.Clues.Contains("CLUE_TEST"));
            Assert.AreEqual(1, _state.Exploration.DayExplorations);
        }

        [Test]
        public void HpZero_EndsGame()
        {
            _expedition.Handler = _ => new ExpeditionResult { Status = ExtractStatus.Down, HpDelta = -200 };
            _state.Plan.Slots[0].ActionId = TestContent.DayExpedition;
            var outcome = _runner.ExecuteNextSlot();

            Assert.AreEqual(GameStatus.GameOver, _state.Status);
            Assert.IsTrue(outcome.StopForPresentation);
            Assert.Throws<InvalidOperationException>(() => _runner.ExecuteNextSlot());
        }
    }
}
