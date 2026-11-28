using System;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>막 평가·1년의 기록 막대 그래프의 값 계산: 계열 뽑기, 변화량, 눈금, 막대 높이 (화면과 떨어져 있다).</summary>
    public sealed class StatChartTests
    {
        private static readonly int[] ActEnds = { 13, 26, 39, 52 };

        /// <summary>1주차부터 lastRecorded주차까지 주마다 모든 능력치가 +1씩 자란 기록 (n주차 끝 = base + n), 지금 값은 따로.</summary>
        private static GameState StateWith(int lastRecorded, int week, int now, int startValue = 10)
        {
            var state = NewGameFactory.Create(new BalanceConfig(), 7);
            state.History.Clear();
            for (var w = 1; w <= lastRecorded; w++)
            {
                var record = new WeekRecord { Week = w };
                foreach (StatType stat in Enum.GetValues(typeof(StatType)))
                {
                    record.Stats[stat] = startValue + w;
                }

                state.History.Add(record);
            }

            state.Calendar.Week = week;
            foreach (StatType stat in Enum.GetValues(typeof(StatType)))
            {
                state.Player.SetStat(stat, now);
            }

            return state;
        }

        [Test]
        public void NiceAxis_UsesOneTwoFiveSteps_AndCoversTheMax()
        {
            Assert.AreEqual((5, 20), StatChart.NiceAxis(0, 5, 20), "값이 없어도 최소 꼭대기");
            Assert.AreEqual((5, 20), StatChart.NiceAxis(20, 5, 20));
            Assert.AreEqual((5, 25), StatChart.NiceAxis(21, 5, 20));
            Assert.AreEqual((20, 100), StatChart.NiceAxis(82, 5, 20));
            Assert.AreEqual((20, 100), StatChart.NiceAxis(100, 5, 20));
            Assert.AreEqual((50, 150), StatChart.NiceAxis(101, 5, 20));
            Assert.AreEqual((50, 250), StatChart.NiceAxis(240, 5, 20));
            Assert.AreEqual((100, 300), StatChart.NiceAxis(251, 5, 20));
        }

        [Test]
        public void NiceAxis_NeverExceedsTheTickCount_AndTopIsAMultipleOfTheStep()
        {
            for (var ticks = 1; ticks <= 8; ticks++)
            {
                for (var max = 0; max <= 1200; max += 7)
                {
                    var (step, top) = StatChart.NiceAxis(max, ticks, 10);
                    Assert.GreaterOrEqual(top, Math.Max(max, 10), $"max {max} ticks {ticks}");
                    Assert.AreEqual(0, top % step, $"max {max} ticks {ticks}");
                    Assert.LessOrEqual(top / step, ticks, $"max {max} ticks {ticks}");
                    var lead = step;
                    while (lead >= 10) lead /= 10;
                    Assert.Contains(lead, new[] { 1, 2, 5 }, $"step {step}");
                }
            }
        }

        [Test]
        public void ForAct_StartsFromThePreviousWeeksRecord()
        {
            // 2막 평가 (26주 7일 밤): 25주차까지 기록, 14주차부터의 구간 → 시작은 13주차 끝
            var state = StateWith(25, 26, 60);
            var data = StatChart.ForAct(state, 14, 2);

            Assert.IsFalse(data.Year);
            Assert.AreEqual(2, data.Series.Count);
            Assert.AreEqual(StatChartPoint.Start, data.Series[0].Point);
            Assert.AreEqual(13, data.Series[0].Week);
            Assert.AreEqual(StatChartPoint.Now, data.Series[1].Point);
            Assert.AreEqual(2, data.Series[1].Act);
            Assert.AreEqual(26, data.Series[1].Week);

            Assert.AreEqual(Enum.GetValues(typeof(StatType)).Length, data.Groups.Count);
            foreach (var group in data.Groups)
            {
                Assert.AreEqual(new[] { 23, 60 }, group.Values);
                Assert.AreEqual(37, group.Delta);
            }
        }

        [Test]
        public void ForAct_FirstAct_StartsFromTheEarliestRecord()
        {
            var state = StateWith(12, 13, 40);
            var data = StatChart.ForAct(state, 1, 1);

            Assert.AreEqual(1, data.Series[0].Week, "0주차 기록은 없다 → 1주차 끝에서 시작");
            Assert.AreEqual(new[] { 11, 40 }, data.Groups[0].Values);
        }

        [Test]
        public void ForAct_WithoutHistory_ShowsOnlyNow()
        {
            var state = StateWith(0, 13, 40);
            var data = StatChart.ForAct(state, 1, 1);

            Assert.AreEqual(1, data.Series.Count);
            Assert.AreEqual(StatChartPoint.Now, data.Series[0].Point);
            Assert.AreEqual(0, data.Groups[0].Delta, "견줄 시작이 없으면 변화도 없다");
            Assert.AreEqual(0, data.TopGain().Gain);
        }

        [Test]
        public void ForYear_FullRun_HasStartAndEveryActEnd()
        {
            // 52주차에 구조: 51주차까지 기록, 마지막 막대는 지금 값
            var state = StateWith(51, 52, 90);
            var data = StatChart.ForYear(state, ActEnds);

            Assert.IsTrue(data.Year);
            Assert.AreEqual(new[] { StatChartPoint.Start, StatChartPoint.ActEnd, StatChartPoint.ActEnd, StatChartPoint.ActEnd, StatChartPoint.ActEnd },
                data.Series.Select(s => s.Point).ToArray());
            Assert.AreEqual(new[] { 0, 1, 2, 3, 4 }, data.Series.Select(s => s.Act).ToArray());
            Assert.AreEqual(new[] { 1, 13, 26, 39, 52 }, data.Series.Select(s => s.Week).ToArray());
            Assert.AreEqual(new[] { 11, 23, 36, 49, 90 }, data.Groups[0].Values);
            Assert.AreEqual(79, data.Groups[0].Delta);
        }

        [Test]
        public void ForYear_EndedMidAct_LastBarIsNow()
        {
            // 30주차에 끝난 판 (3막 도중)
            var state = StateWith(29, 30, 50);
            var data = StatChart.ForYear(state, ActEnds);

            Assert.AreEqual(new[] { StatChartPoint.Start, StatChartPoint.ActEnd, StatChartPoint.ActEnd, StatChartPoint.Now },
                data.Series.Select(s => s.Point).ToArray());
            Assert.AreEqual(new[] { 0, 1, 2, 3 }, data.Series.Select(s => s.Act).ToArray());
            Assert.AreEqual(new[] { 11, 23, 36, 50 }, data.Groups[0].Values);
        }

        [Test]
        public void ForYear_SkipsActsWithoutARecord()
        {
            // 디버그로 52주차에 건너뛴 판: 기록이 3주차까지뿐
            var state = StateWith(3, 52, 70);
            var data = StatChart.ForYear(state, ActEnds);

            Assert.AreEqual(2, data.Series.Count);
            Assert.AreEqual(StatChartPoint.Start, data.Series[0].Point);
            Assert.AreEqual(4, data.Series[1].Act);
            Assert.AreEqual(new[] { 11, 70 }, data.Groups[0].Values);

            // 기록이 하나도 없으면 지금 값만
            var empty = StatChart.ForYear(StateWith(0, 52, 70), ActEnds);
            Assert.AreEqual(1, empty.Series.Count);
            Assert.AreEqual(new[] { 70 }, empty.Groups[0].Values);
        }

        [Test]
        public void Axis_CoversTheLargestValue_AndRatiosMatch()
        {
            var state = StateWith(25, 26, 60);
            state.Player.SetStat(StatType.Intellect, 130);
            var data = StatChart.ForAct(state, 14, 2);

            Assert.AreEqual(50, data.AxisStep);
            Assert.AreEqual(150, data.AxisMax);
            Assert.AreEqual(new[] { 0, 50, 100, 150 }, data.Ticks().ToArray());
            Assert.AreEqual(130f / 150f, data.Ratio(130), 0.0001f);
            Assert.AreEqual(0f, data.Ratio(0));
            Assert.AreEqual(0f, data.Ratio(-5), "0 아래는 막대가 없다");
            Assert.AreEqual(1f, data.Ratio(999), "꼭대기를 넘지 않는다");
        }

        [Test]
        public void TopGain_PicksTheLargestDelta_AndDeltaCanBeNegative()
        {
            var state = StateWith(25, 26, 30);
            state.Player.SetStat(StatType.Craft, 55);
            state.Player.SetStat(StatType.Stamina, 15);
            var data = StatChart.ForAct(state, 14, 2);

            var top = data.TopGain();
            Assert.AreEqual(StatType.Craft, top.Stat);
            Assert.AreEqual(32, top.Gain);
            Assert.AreEqual(-8, data.Groups.First(g => g.Stat == StatType.Stamina).Delta, "줄어든 능력치는 음수");
        }
    }
}
