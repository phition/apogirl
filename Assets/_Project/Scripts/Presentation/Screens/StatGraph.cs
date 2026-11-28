using System;
using System.Collections.Generic;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;
using ApoGirl.Presentation.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace ApoGirl.Presentation.Screens
{
    /// <summary>막 평가·1년의 기록 막대 그래프 설정 (Resources/Art/report_chart.json). 색·크기는 Theme.uss의 .stat-chart 절.</summary>
    [Serializable]
    public sealed class ReportChartConfig
    {
        /// <summary>막마다 마지막 주차 (1년의 기록이 이 주의 기록으로 막대를 세운다).</summary>
        public int[] ActEndWeeks = { 13, 26, 39, 52 };

        /// <summary>세로축 눈금 수 (0을 빼고 이 수를 넘지 않게).</summary>
        public int Ticks = StatChart.DefaultTicks;

        /// <summary>세로축 꼭대기의 최솟값.</summary>
        public int MinAxisMax = StatChart.DefaultMinAxisMax;

        private static ReportChartConfig _loaded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLoaded() => _loaded = null;

        public static ReportChartConfig Load()
        {
            if (_loaded == null)
            {
                var asset = Resources.Load<TextAsset>("Art/report_chart");
                _loaded = asset != null ? JsonUtility.FromJson<ReportChartConfig>(asset.text) ?? new ReportChartConfig() : new ReportChartConfig();
            }

            return _loaded;
        }
    }

    /// <summary>
    /// 능력치 8종의 묶음 세로 막대 그래프 (값 계산은 Core StatChart). 엔딩의 "1년의 기록"과 막 평가 카드가 함께 쓴다 —
    /// 수치 요약은 그래프, 서사는 문장으로.
    /// 막 평가: 능력치마다 막 시작·지금 두 막대. 1년의 기록: 첫 주 + 막이 끝날 때마다 한 막대.
    /// 마지막 막대 위에 값, 이름 아래에 변화량(+n·−n), 능력치에 마우스를 올리면 아래 한 줄에 계열별 값.
    /// </summary>
    public sealed class StatGraph : VisualElement
    {
        private readonly StatChartData _data;
        private readonly Label _detail;
        private readonly List<VisualElement> _columns = new List<VisualElement>();
        private readonly List<VisualElement> _nameCells = new List<VisualElement>();

        /// <summary>1년의 기록: 처음부터 막 단위로.</summary>
        public StatGraph(GameState state)
            : this(Year(state))
        {
        }

        /// <summary>막 평가: fromWeek주차부터 지금까지.</summary>
        public StatGraph(GameState state, int fromWeek, int act)
            : this(Act(state, fromWeek, act))
        {
        }

        public StatGraph(StatChartData data)
        {
            _data = data;
            AddToClassList("stat-chart");
            AddToClassList(data.Year ? "year" : "act");

            Add(Legend());

            var body = new VisualElement();
            body.AddToClassList("stat-chart-body");
            body.Add(Axis());
            body.Add(Plot());
            Add(body);

            Add(Names());

            _detail = new Label();
            _detail.AddToClassList("stat-chart-detail");
            Add(_detail);
            Highlight(-1);
        }

        public StatChartData Data => _data;

        /// <summary>아래 한 줄에 지금 보이는 글 (테스트용).</summary>
        public string DetailText => _detail.text;

        /// <summary>1년의 기록용 값.</summary>
        public static StatChartData Year(GameState state)
        {
            var config = ReportChartConfig.Load();
            return StatChart.ForYear(state, config.ActEndWeeks, config.Ticks, config.MinAxisMax);
        }

        /// <summary>막 평가용 값.</summary>
        public static StatChartData Act(GameState state, int fromWeek, int act)
        {
            var config = ReportChartConfig.Load();
            return StatChart.ForAct(state, fromWeek, act, config.Ticks, config.MinAxisMax);
        }

        /// <summary>계열 이름: 막 시작·지금 / 첫 주·n막·마지막 날.</summary>
        public static string SeriesName(StatChartData data, StatChartSeries series)
        {
            switch (series.Point)
            {
                case StatChartPoint.Start:
                    return Loc.T(data.Year ? "chart.series.first_week" : "chart.series.act_start");
                case StatChartPoint.ActEnd:
                    return Loc.F("chart.series.act", series.Act);
                default:
                    return Loc.T(data.Year ? "chart.series.last_day" : "chart.series.now");
            }
        }

        /// <summary>계열의 색 클래스: 시작은 빈 막대, 막은 막 번호를 따른다 (계열 수가 달라져도 같은 막은 같은 색).</summary>
        public static string SeriesClass(StatChartData data, StatChartSeries series)
        {
            if (series.Point == StatChartPoint.Start)
            {
                return "s-start";
            }

            // 막 평가의 '지금'은 늘 가장 짙은 색
            return !data.Year ? "s-now" : "s-act" + Mathf.Clamp(series.Act, 1, 4);
        }

        /// <summary>변화량 글: +n · −n · 변화 없음.</summary>
        public static string DeltaText(int delta)
        {
            return delta > 0 ? Loc.F("chart.delta.up", delta) : delta < 0 ? Loc.F("chart.delta.down", -delta) : Loc.T("chart.delta.same");
        }

        /// <summary>능력치 하나를 짚는다 (−1 = 짚은 것 없음): 그 묶음을 밝히고 아래 한 줄에 계열별 값을 적는다.</summary>
        public void Highlight(int index)
        {
            for (var i = 0; i < _columns.Count; i++)
            {
                _columns[i].EnableInClassList("focused", i == index);
                _nameCells[i].EnableInClassList("focused", i == index);
            }

            EnableInClassList("has-focus", index >= 0 && index < _data.Groups.Count);
            if (index < 0 || index >= _data.Groups.Count)
            {
                _detail.text = Loc.T(_data.Series.Count > 1 ? "chart.hint" : "chart.hint_single");
                return;
            }

            var group = _data.Groups[index];
            var parts = new List<string>();
            for (var s = 0; s < _data.Series.Count; s++)
            {
                parts.Add(Loc.F("chart.detail.item", SeriesName(_data, _data.Series[s]), group.Values[s]));
            }

            var name = Loc.T("stat." + group.Stat);
            var values = string.Join(" · ", parts);
            _detail.text = _data.Series.Count > 1
                ? Loc.F("chart.detail_delta", name, values, DeltaText(group.Delta))
                : Loc.F("chart.detail", name, values);
        }

        private VisualElement Legend()
        {
            var legend = new VisualElement();
            legend.AddToClassList("stat-chart-legend");
            foreach (var series in _data.Series)
            {
                var item = new VisualElement();
                item.AddToClassList("stat-chart-legend-item");
                var swatch = new VisualElement();
                swatch.AddToClassList("stat-chart-swatch");
                swatch.AddToClassList(SeriesClass(_data, series));
                item.Add(swatch);
                var label = new Label(SeriesName(_data, series));
                label.AddToClassList("stat-chart-legend-label");
                item.Add(label);
                legend.Add(item);
            }

            return legend;
        }

        /// <summary>세로축: 눈금 숫자 (눈금선과 같은 높이에).</summary>
        private VisualElement Axis()
        {
            var axis = new VisualElement();
            axis.AddToClassList("stat-chart-axis");
            foreach (var tick in _data.Ticks())
            {
                var label = new Label(tick.ToString());
                label.AddToClassList("stat-chart-tick");
                label.style.bottom = Length.Percent(_data.Ratio(tick) * 100f);
                axis.Add(label);
            }

            return axis;
        }

        /// <summary>그림 칸: 눈금선 위에 능력치 묶음마다 막대.</summary>
        private VisualElement Plot()
        {
            var plot = new VisualElement();
            plot.AddToClassList("stat-chart-plot");
            foreach (var tick in _data.Ticks())
            {
                var line = new VisualElement();
                line.AddToClassList("stat-chart-grid");
                line.EnableInClassList("base", tick == 0);
                line.style.bottom = Length.Percent(_data.Ratio(tick) * 100f);
                line.pickingMode = PickingMode.Ignore;
                plot.Add(line);
            }

            var columns = new VisualElement();
            columns.AddToClassList("stat-chart-columns");
            for (var g = 0; g < _data.Groups.Count; g++)
            {
                var index = g;
                var group = _data.Groups[g];
                var column = new VisualElement();
                column.AddToClassList("stat-chart-column");
                for (var s = 0; s < _data.Series.Count; s++)
                {
                    var last = s == _data.Series.Count - 1;
                    var bar = new VisualElement();
                    bar.AddToClassList("stat-chart-bar");
                    bar.AddToClassList(SeriesClass(_data, _data.Series[s]));
                    bar.EnableInClassList("last", last);
                    bar.EnableInClassList("empty", group.Values[s] <= 0);
                    bar.style.height = Length.Percent(_data.Ratio(group.Values[s]) * 100f);
                    bar.pickingMode = PickingMode.Ignore;
                    if (last)
                    {
                        // 값은 마지막 막대 위에만 (모든 막대에 숫자를 달면 읽히지 않는다 — 나머지는 짚으면 아래 줄에)
                        var value = new Label(group.Values[s].ToString());
                        value.AddToClassList("stat-chart-value");
                        value.pickingMode = PickingMode.Ignore;
                        bar.Add(value);
                    }

                    column.Add(bar);
                }

                column.RegisterCallback<PointerEnterEvent>(_ => Highlight(index));
                column.RegisterCallback<PointerLeaveEvent>(_ => Highlight(-1));
                _columns.Add(column);
                columns.Add(column);
            }

            plot.Add(columns);
            return plot;
        }

        /// <summary>가로축: 능력치 이름과 그 아래 변화량.</summary>
        private VisualElement Names()
        {
            var names = new VisualElement();
            names.AddToClassList("stat-chart-names");
            for (var g = 0; g < _data.Groups.Count; g++)
            {
                var index = g;
                var group = _data.Groups[g];
                var cell = new VisualElement();
                cell.AddToClassList("stat-chart-name-cell");
                var name = new Label(Loc.T("stat." + group.Stat));
                name.AddToClassList("stat-chart-name");
                cell.Add(name);
                if (_data.Series.Count > 1)
                {
                    var delta = new Label(DeltaText(group.Delta));
                    delta.AddToClassList("stat-chart-delta");
                    delta.AddToClassList(group.Delta > 0 ? "up" : group.Delta < 0 ? "down" : "same");
                    cell.Add(delta);
                }

                cell.RegisterCallback<PointerEnterEvent>(_ => Highlight(index));
                cell.RegisterCallback<PointerLeaveEvent>(_ => Highlight(-1));
                _nameCells.Add(cell);
                names.Add(cell);
            }

            return names;
        }
    }
}
