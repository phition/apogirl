using System;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;
using ApoGirl.Presentation.Screens;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace ApoGirl.Tests.PlayMode
{
    /// <summary>
    /// 능력치 막대 그래프 화면 (막 평가·1년의 기록): 값(StatChart)대로 막대·눈금·범례·변화량이 서는지.
    /// 세이브·갤러리를 건드리지 않게 상태를 손으로 만든다.
    /// </summary>
    public sealed class StatGraphTests
    {
        private static readonly StatType[] Stats = (StatType[])Enum.GetValues(typeof(StatType));

        /// <summary>계열과 능력치별 값을 직접 준 그래프 값 (눈금 20 간격, 꼭대기 100).</summary>
        private static StatChartData Data(bool year, StatChartSeries[] series, Func<StatType, int[]> values)
        {
            var data = new StatChartData { Year = year, AxisStep = 20, AxisMax = 100 };
            data.Series.AddRange(series);
            foreach (var stat in Stats)
            {
                data.Groups.Add(new StatChartGroup { Stat = stat, Values = values(stat) });
            }

            return data;
        }

        private static StatChartSeries Series(StatChartPoint point, int act = 0) => new StatChartSeries { Point = point, Act = act };

        private static List<VisualElement> All(VisualElement root, string cls) => root.Query(className: cls).ToList();

        [Test]
        public void ActChart_HasTwoBarsPerStat_WithHeightsFromTheValues()
        {
            var data = Data(false, new[] { Series(StatChartPoint.Start), Series(StatChartPoint.Now, 2) },
                stat => stat == StatType.Stamina ? new[] { 40, 25 } : stat == StatType.Craft ? new[] { 0, 50 } : new[] { 30, 30 });
            var graph = new StatGraph(data);

            Assert.IsTrue(graph.ClassListContains("act"));
            var bars = All(graph, "stat-chart-bar");
            Assert.AreEqual(Stats.Length * 2, bars.Count);

            // 체력 묶음: 40 → 25 (눈금 꼭대기 100)
            Assert.AreEqual(40f, bars[0].style.height.value.value, 0.01f);
            Assert.AreEqual(25f, bars[1].style.height.value.value, 0.01f);
            Assert.AreEqual(LengthUnit.Percent, bars[0].style.height.value.unit);
            Assert.IsTrue(bars[0].ClassListContains("s-start"));
            Assert.IsTrue(bars[1].ClassListContains("s-now"));
            Assert.IsTrue(bars[1].ClassListContains("last"));

            // 값 글자는 마지막 막대 위에만
            var values = All(graph, "stat-chart-value");
            Assert.AreEqual(Stats.Length, values.Count);
            Assert.AreEqual("25", ((Label)values[0]).text);
            Assert.AreSame(bars[1], values[0].parent);

            // 0인 막대는 얇은 최소 높이도 없다
            var craft = Array.IndexOf(Stats, StatType.Craft);
            Assert.IsTrue(bars[craft * 2].ClassListContains("empty"));
            Assert.IsFalse(bars[craft * 2 + 1].ClassListContains("empty"));
        }

        [Test]
        public void Deltas_AreMarkedUpDownOrSame()
        {
            var data = Data(false, new[] { Series(StatChartPoint.Start), Series(StatChartPoint.Now, 2) },
                stat => stat == StatType.Stamina ? new[] { 40, 25 } : stat == StatType.Craft ? new[] { 0, 50 } : new[] { 30, 30 });
            var graph = new StatGraph(data);

            var deltas = All(graph, "stat-chart-delta");
            Assert.AreEqual(Stats.Length, deltas.Count);
            Assert.IsTrue(deltas[Array.IndexOf(Stats, StatType.Stamina)].ClassListContains("down"));
            Assert.IsTrue(deltas[Array.IndexOf(Stats, StatType.Craft)].ClassListContains("up"));
            Assert.IsTrue(deltas[Array.IndexOf(Stats, StatType.Empathy)].ClassListContains("same"));
        }

        [Test]
        public void YearChart_ColorsFollowTheAct_NotTheOrder()
        {
            // 3막 도중에 끝난 판: 첫 주 · 1막 · 2막 · 마지막 날(3막)
            var data = Data(true,
                new[] { Series(StatChartPoint.Start), Series(StatChartPoint.ActEnd, 1), Series(StatChartPoint.ActEnd, 2), Series(StatChartPoint.Now, 3) },
                _ => new[] { 10, 20, 30, 40 });
            var graph = new StatGraph(data);

            Assert.IsTrue(graph.ClassListContains("year"));
            var bars = All(graph, "stat-chart-bar");
            Assert.AreEqual(Stats.Length * 4, bars.Count);
            Assert.AreEqual(new[] { "s-start", "s-act1", "s-act2", "s-act3" },
                bars.Take(4).Select(b => b.GetClasses().First(c => c.StartsWith("s-"))).ToArray());

            // 범례는 계열마다 하나, 같은 색 클래스
            var swatches = All(graph, "stat-chart-swatch");
            Assert.AreEqual(4, swatches.Count);
            Assert.IsTrue(swatches[3].ClassListContains("s-act3"));
            Assert.AreEqual(4, All(graph, "stat-chart-legend-label").Count);
        }

        [Test]
        public void Axis_HasATickAndAGridLinePerStep()
        {
            var data = Data(true, new[] { Series(StatChartPoint.Start), Series(StatChartPoint.ActEnd, 4) }, _ => new[] { 10, 90 });
            var graph = new StatGraph(data);

            var ticks = All(graph, "stat-chart-tick");
            Assert.AreEqual(new[] { "0", "20", "40", "60", "80", "100" }, ticks.Select(t => ((Label)t).text).ToArray());
            Assert.AreEqual(60f, ticks[3].style.bottom.value.value, 0.01f);

            var grid = All(graph, "stat-chart-grid");
            Assert.AreEqual(6, grid.Count);
            Assert.IsTrue(grid[0].ClassListContains("base"), "0 눈금은 바닥선");
            Assert.AreEqual(60f, grid[3].style.bottom.value.value, 0.01f);
        }

        [Test]
        public void Highlight_WritesTheValuesOfThatStat_AndClears()
        {
            var data = Data(false, new[] { Series(StatChartPoint.Start), Series(StatChartPoint.Now, 2) },
                stat => stat == StatType.Intellect ? new[] { 31, 77 } : new[] { 5, 5 });
            var graph = new StatGraph(data);
            var hint = graph.DetailText;
            var index = Array.IndexOf(Stats, StatType.Intellect);

            graph.Highlight(index);
            Assert.AreNotEqual(hint, graph.DetailText, "짚으면 아래 줄이 그 능력치의 값으로 바뀐다");
            Assert.IsTrue(graph.ClassListContains("has-focus"));
            Assert.IsTrue(All(graph, "stat-chart-column")[index].ClassListContains("focused"));
            Assert.AreEqual(1, All(graph, "focused").Count(e => e.ClassListContains("stat-chart-column")));

            graph.Highlight(-1);
            Assert.AreEqual(hint, graph.DetailText);
            Assert.IsFalse(graph.ClassListContains("has-focus"));
        }

        [Test]
        public void SingleSeries_HasNoDeltaRow()
        {
            // 기록이 없는 판 (디버그로 건너뜀): 지금 값 막대만
            var data = Data(true, new[] { Series(StatChartPoint.ActEnd, 4) }, _ => new[] { 60 });
            var graph = new StatGraph(data);

            Assert.AreEqual(Stats.Length, All(graph, "stat-chart-bar").Count);
            Assert.AreEqual(0, All(graph, "stat-chart-delta").Count);
        }

        [Test]
        public void Config_ActEndWeeksAreAscending()
        {
            var config = ReportChartConfig.Load();
            Assert.Greater(config.ActEndWeeks.Length, 0);
            for (var i = 1; i < config.ActEndWeeks.Length; i++)
            {
                Assert.Greater(config.ActEndWeeks[i], config.ActEndWeeks[i - 1]);
            }

            Assert.AreEqual(CalendarState.TotalWeeks, config.ActEndWeeks[config.ActEndWeeks.Length - 1], "마지막 막은 52주차에 끝난다");
            Assert.Greater(config.Ticks, 0);
        }
    }
}
