using System;
using System.Collections.Generic;
using ApoGirl.Core.State;
using ApoGirl.Presentation.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace ApoGirl.Presentation.Screens
{
    /// <summary>
    /// 능력치 8종의 주간 변화 꺾은선 + 범례 (주간 기록 History + 지금 값).
    /// 엔딩의 "1년의 기록"과 막 평가 카드가 함께 쓴다 (수치 요약은 그래프, 서사는 문장으로).
    /// </summary>
    public sealed class StatGraph : VisualElement
    {
        private static readonly Color[] Palette =
        {
            new Color(0.85f, 0.45f, 0.40f), new Color(0.90f, 0.62f, 0.35f), new Color(0.88f, 0.82f, 0.45f), new Color(0.55f, 0.78f, 0.48f),
            new Color(0.45f, 0.75f, 0.78f), new Color(0.50f, 0.58f, 0.88f), new Color(0.72f, 0.55f, 0.85f), new Color(0.85f, 0.55f, 0.70f),
        };

        private readonly List<Dictionary<StatType, int>> _points = new List<Dictionary<StatType, int>>();

        /// <param name="fromWeek">이 주부터 (0 = 처음부터).</param>
        /// <param name="height">그래프 높이 (px).</param>
        public StatGraph(GameState state, int fromWeek = 0, float height = 220f)
        {
            foreach (var record in state.History)
            {
                if (record.Week >= fromWeek)
                {
                    _points.Add(record.Stats);
                }
            }

            // 마지막 점은 지금 값 (아직 정산 전인 주 포함)
            var now = new Dictionary<StatType, int>();
            foreach (StatType stat in Enum.GetValues(typeof(StatType)))
            {
                now[stat] = state.Player.GetStat(stat);
            }

            _points.Add(now);
            AddToClassList("ending-graph-box");

            var canvas = new VisualElement();
            canvas.AddToClassList("ending-graph");
            canvas.style.height = height;
            canvas.generateVisualContent += Draw;
            Add(canvas);

            var legend = new VisualElement();
            legend.AddToClassList("ending-legend");
            var stats = (StatType[])Enum.GetValues(typeof(StatType));
            var first = _points[0];
            for (var i = 0; i < stats.Length; i++)
            {
                first.TryGetValue(stats[i], out var start);
                var gain = now[stats[i]] - start;
                var item = new Label(Loc.T("stat." + stats[i]) + " " + now[stats[i]] + (gain > 0 ? "  (+" + gain + ")" : ""));
                item.AddToClassList("ending-legend-item");
                item.style.color = Palette[i % Palette.Length];
                legend.Add(item);
            }

            Add(legend);
        }

        /// <summary>그래프 구간에서 가장 많이 자란 능력치와 증가량.</summary>
        public static (StatType Stat, int Gain) TopGain(GameState state, int fromWeek)
        {
            Dictionary<StatType, int> start = null;
            foreach (var record in state.History)
            {
                if (record.Week >= fromWeek)
                {
                    start = record.Stats;
                    break;
                }
            }

            var best = (StatType.Stamina, int.MinValue);
            foreach (StatType stat in Enum.GetValues(typeof(StatType)))
            {
                var before = 0;
                start?.TryGetValue(stat, out before);
                var gain = state.Player.GetStat(stat) - (start == null ? state.Player.GetStat(stat) : before);
                if (gain > best.Item2) best = (stat, gain);
            }

            return best;
        }

        private void Draw(MeshGenerationContext context)
        {
            var rect = context.visualElement.contentRect;
            if (_points.Count == 0 || rect.width <= 1f || rect.height <= 1f)
            {
                return;
            }

            var max = 10;
            foreach (var point in _points)
            {
                foreach (var value in point.Values)
                {
                    max = Math.Max(max, value);
                }
            }

            var painter = context.painter2D;
            painter.lineWidth = 1f;
            painter.strokeColor = new Color(1f, 1f, 1f, 0.12f);
            for (var g = 0; g <= 4; g++)
            {
                var y = rect.height * g / 4f;
                painter.BeginPath();
                painter.MoveTo(new Vector2(0f, y));
                painter.LineTo(new Vector2(rect.width, y));
                painter.Stroke();
            }

            var stats = (StatType[])Enum.GetValues(typeof(StatType));
            painter.lineWidth = 2f;
            var steps = Math.Max(1, _points.Count - 1);
            for (var i = 0; i < stats.Length; i++)
            {
                painter.strokeColor = Palette[i % Palette.Length];
                painter.BeginPath();
                for (var w = 0; w < _points.Count; w++)
                {
                    _points[w].TryGetValue(stats[i], out var value);
                    var point = new Vector2(rect.width * w / steps, rect.height * (1f - (float)value / max));
                    if (w == 0) painter.MoveTo(point);
                    else painter.LineTo(point);
                }

                painter.Stroke();
            }
        }
    }
}
