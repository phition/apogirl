using System;
using System.Collections.Generic;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>막대 한 계열이 가리키는 때.</summary>
    public enum StatChartPoint
    {
        /// <summary>구간의 시작 (막 평가 = 앞 막이 끝났을 때, 1년 = 첫 주가 끝났을 때).</summary>
        Start,

        /// <summary>그 막이 끝났을 때.</summary>
        ActEnd,

        /// <summary>지금 (막 평가의 '지금', 막 도중에 끝난 판의 '마지막 날').</summary>
        Now,
    }

    /// <summary>막대 한 계열 (능력치 묶음마다 하나씩 서는 막대).</summary>
    public sealed class StatChartSeries
    {
        public StatChartPoint Point;

        /// <summary>몇 막의 값인가 (1~, 시작 계열은 0).</summary>
        public int Act;

        /// <summary>몇 주차 기록인가 (지금 값이면 지금 주차).</summary>
        public int Week;
    }

    /// <summary>능력치 하나의 묶음: 계열 순서대로의 값.</summary>
    public sealed class StatChartGroup
    {
        public StatType Stat;
        public int[] Values = Array.Empty<int>();

        public int First => Values.Length > 0 ? Values[0] : 0;
        public int Last => Values.Length > 0 ? Values[Values.Length - 1] : 0;

        /// <summary>구간 동안의 변화 (마지막 − 처음).</summary>
        public int Delta => Last - First;
    }

    /// <summary>막 평가·1년의 기록에 그리는 능력치 막대 그래프의 값 (화면과 떨어져 있다).</summary>
    public sealed class StatChartData
    {
        public List<StatChartSeries> Series = new List<StatChartSeries>();
        public List<StatChartGroup> Groups = new List<StatChartGroup>();

        /// <summary>1년 전체를 막 단위로 본 그래프인가 (false = 한 막의 시작과 지금).</summary>
        public bool Year;

        /// <summary>세로축 꼭대기 값 (눈금 간격의 배수).</summary>
        public int AxisMax = 10;

        /// <summary>눈금 간격.</summary>
        public int AxisStep = 5;

        /// <summary>눈금 값들 (0부터 꼭대기까지).</summary>
        public List<int> Ticks()
        {
            var ticks = new List<int>();
            for (var value = 0; value <= AxisMax; value += Math.Max(1, AxisStep))
            {
                ticks.Add(value);
            }

            return ticks;
        }

        /// <summary>막대 높이 비율 0~1 (0 이하는 0, 꼭대기를 넘으면 1).</summary>
        public float Ratio(int value)
        {
            if (AxisMax <= 0 || value <= 0)
            {
                return 0f;
            }

            return Math.Min(1f, (float)value / AxisMax);
        }

        /// <summary>가장 많이 자란 능력치와 그 증가량 (묶음이 없으면 증가량 0).</summary>
        public (StatType Stat, int Gain) TopGain()
        {
            var best = (StatType.Stamina, 0);
            var found = false;
            foreach (var group in Groups)
            {
                if (!found || group.Delta > best.Item2)
                {
                    best = (group.Stat, group.Delta);
                    found = true;
                }
            }

            return best;
        }
    }

    /// <summary>
    /// 능력치 막대 그래프의 값 계산: 주간 기록(GameState.History)과 지금 값에서 계열을 뽑고, 변화량·눈금·막대 높이를 낸다.
    /// 주간 기록은 그 주가 '끝났을 때'의 모습이다 — n주차부터의 구간은 n−1주차 기록에서 시작한다.
    /// </summary>
    public static class StatChart
    {
        /// <summary>눈금 수 기본값 (0을 빼고 이쯤 되게).</summary>
        public const int DefaultTicks = 5;

        /// <summary>세로축 꼭대기의 최솟값 기본값 (값이 아주 작을 때 막대가 꽉 차 보이지 않게).</summary>
        public const int DefaultMinAxisMax = 20;

        /// <summary>한 막의 변화: 막 시작(fromWeek 앞 주의 기록)과 지금.</summary>
        public static StatChartData ForAct(GameState state, int fromWeek, int act = 0, int ticks = DefaultTicks, int minAxisMax = DefaultMinAxisMax)
        {
            var data = new StatChartData { Year = false };
            var columns = new List<Dictionary<StatType, int>>();
            var start = StartRecord(state, fromWeek);
            if (start != null)
            {
                data.Series.Add(new StatChartSeries { Point = StatChartPoint.Start, Week = start.Week });
                columns.Add(start.Stats);
            }

            data.Series.Add(new StatChartSeries { Point = StatChartPoint.Now, Act = act, Week = state.Calendar.Week });
            columns.Add(NowStats(state));
            Fill(data, columns, ticks, minAxisMax);
            return data;
        }

        /// <summary>
        /// 1년을 막 단위로: 첫 주 + 끝난 막마다 그 막 마지막 주의 기록 + 지금 있는 막의 지금 값.
        /// 지금 주차가 그 막의 마지막 주면 '막 끝', 아니면 '마지막 날'(Now)로 표시한다. 기록이 없는 막은 건너뛴다.
        /// </summary>
        public static StatChartData ForYear(GameState state, IReadOnlyList<int> actEndWeeks, int ticks = DefaultTicks, int minAxisMax = DefaultMinAxisMax)
        {
            var data = new StatChartData { Year = true };
            var columns = new List<Dictionary<StatType, int>>();
            var week = state.Calendar.Week;
            var start = StartRecord(state, 0);
            if (start != null && start.Week < week)
            {
                data.Series.Add(new StatChartSeries { Point = StatChartPoint.Start, Week = start.Week });
                columns.Add(start.Stats);
            }

            var count = actEndWeeks?.Count ?? 0;
            var nowAct = count == 0 ? 1 : count;
            var nowAtEnd = false;
            for (var i = 0; i < count; i++)
            {
                var end = actEndWeeks[i];
                if (week <= end)
                {
                    nowAct = i + 1;
                    nowAtEnd = week == end;
                    break;
                }

                var record = RecordAt(state, end);
                if (record != null && (start == null || record.Week > start.Week))
                {
                    data.Series.Add(new StatChartSeries { Point = StatChartPoint.ActEnd, Act = i + 1, Week = end });
                    columns.Add(record.Stats);
                }
            }

            data.Series.Add(new StatChartSeries { Point = nowAtEnd ? StatChartPoint.ActEnd : StatChartPoint.Now, Act = nowAct, Week = week });
            columns.Add(NowStats(state));
            Fill(data, columns, ticks, minAxisMax);
            return data;
        }

        /// <summary>
        /// 보기 좋은 세로축: 간격은 1·2·5 × 10ⁿ 가운데 눈금 수가 ticks를 넘지 않는 가장 작은 것, 꼭대기는 간격의 배수.
        /// </summary>
        public static (int Step, int Max) NiceAxis(int maxValue, int ticks = DefaultTicks, int minAxisMax = DefaultMinAxisMax)
        {
            var top = Math.Max(1, Math.Max(maxValue, minAxisMax));
            var wanted = Math.Max(1, ticks);
            var magnitude = 1;
            while (true)
            {
                foreach (var unit in new[] { 1, 2, 5 })
                {
                    var step = unit * magnitude;
                    var lines = (top + step - 1) / step;
                    if (lines <= wanted)
                    {
                        return (step, lines * step);
                    }
                }

                magnitude *= 10;
            }
        }

        /// <summary>fromWeek주차부터의 구간이 시작하는 기록: 앞 주(fromWeek−1)의 기록, 없으면 그 뒤 가장 이른 기록.</summary>
        private static WeekRecord StartRecord(GameState state, int fromWeek)
        {
            var before = RecordAt(state, fromWeek - 1);
            if (before != null)
            {
                return before;
            }

            WeekRecord first = null;
            foreach (var record in state.History)
            {
                if (record.Week >= fromWeek && (first == null || record.Week < first.Week))
                {
                    first = record;
                }
            }

            return first;
        }

        private static WeekRecord RecordAt(GameState state, int week)
        {
            foreach (var record in state.History)
            {
                if (record.Week == week)
                {
                    return record;
                }
            }

            return null;
        }

        private static Dictionary<StatType, int> NowStats(GameState state)
        {
            var now = new Dictionary<StatType, int>();
            foreach (StatType stat in Enum.GetValues(typeof(StatType)))
            {
                now[stat] = state.Player.GetStat(stat);
            }

            return now;
        }

        private static void Fill(StatChartData data, List<Dictionary<StatType, int>> columns, int ticks, int minAxisMax)
        {
            var max = 0;
            foreach (StatType stat in Enum.GetValues(typeof(StatType)))
            {
                var group = new StatChartGroup { Stat = stat, Values = new int[columns.Count] };
                for (var i = 0; i < columns.Count; i++)
                {
                    columns[i].TryGetValue(stat, out var value);
                    group.Values[i] = value;
                    max = Math.Max(max, value);
                }

                data.Groups.Add(group);
            }

            var (step, top) = NiceAxis(max, ticks, minAxisMax);
            data.AxisStep = step;
            data.AxisMax = top;
        }
    }
}
