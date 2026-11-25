using System;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Planning;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;
using ApoGirl.Presentation.Text;
using ApoGirl.Systems.Session;
using UnityEngine;
using UnityEngine.UIElements;

namespace ApoGirl.Presentation.Screens
{
    /// <summary>선생님 메모 한 줄을 화면 문구로 (토큰 "loc:"·"npc:"·"act:"·"day:"·"res:"·"stat:"을 이름으로 바꾼다).</summary>
    internal static class TeacherText
    {
        public static string Note(TeacherNote note, BalanceConfig balance)
        {
            if (note == null || string.IsNullOrEmpty(note.Key))
            {
                return "";
            }

            var args = (note.Args ?? Array.Empty<string>()).Select(a => (object)Token(a, balance)).ToArray();
            return args.Length == 0 ? Loc.T(note.Key) : Loc.F(note.Key, args);
        }

        public static string Token(string token, BalanceConfig balance)
        {
            if (string.IsNullOrEmpty(token))
            {
                return "";
            }

            var colon = token.IndexOf(':');
            if (colon < 0)
            {
                return token;
            }

            var kind = token.Substring(0, colon);
            var value = token.Substring(colon + 1);
            switch (kind)
            {
                case "loc": return Loc.T("location." + value);
                case "npc": return Loc.T("npc." + value);
                case "act": return Loc.Action(value, "short");
                case "day": return int.TryParse(value, out var day) ? Loc.T("weekday." + CalendarRules.Weekday(day, balance)) : value;
                case "res": return Enum.TryParse<ResourceType>(value, out var res) ? UiText.Resource(res) : value;
                case "stat": return Enum.TryParse<StatType>(value, out var stat) ? UiText.Stat(stat) : value;
                default: return value;
            }
        }
    }

    /// <summary>
    /// 계획 보드 아래 흐름 띠 (Docs/TEACHER_PLAN_PROPOSAL.md 6절): 칸마다 예상 피로·스트레스 선과 수면 부채 막대,
    /// 위험선(선생님 다듬기 문턱), 그 아래 한 주 능력치 기대 증감 칩. 끄는 동안에는 지금 계획을 흐리게, 바뀔 계획을 진하게 겹친다.
    /// </summary>
    internal sealed class PlanFlowView
    {
        private static readonly Color FatigueColor = new Color(0.91f, 0.69f, 0.29f);
        private static readonly Color StressColor = new Color(0.80f, 0.45f, 0.85f);
        private static readonly Color DebtColor = new Color(0.42f, 0.61f, 0.88f);
        private static readonly Color DangerColor = new Color(0.86f, 0.33f, 0.30f, 0.55f);
        private static readonly Color GridColor = new Color(1f, 1f, 1f, 0.07f);
        private static readonly Color AxisColor = new Color(1f, 1f, 1f, 0.16f);
        private static readonly Color OldColor = new Color(0.7f, 0.7f, 0.7f, 0.45f);

        private readonly GameSession _session;
        private readonly VisualElement _root;
        private readonly VisualElement _graph;
        private readonly VisualElement _axis;
        private readonly Label[] _ticks = new Label[AxisTicks];
        private readonly VisualElement _stats;
        private readonly Label _summary;
        private WeekProjection _current;
        private WeekProjection _ghost;
        private int _focus = -1;

        // 세로축 위 끝 (값에 맞춰 20·40·…·100, 눈금은 그 1/4씩)
        private const int AxisTicks = 4;
        private const int AxisStep = 20;
        private int _max = AxisStep;

        public PlanFlowView(VisualElement root, GameSession session)
        {
            _session = session;
            _root = root.Q("plan-flow");

            // 그래프는 계획표의 요일 칸과 같은 자리에 (왼쪽은 낮·밤 머리 칸 너비만큼 비운다)
            var strip = new VisualElement();
            strip.AddToClassList("plan-flow-strip");
            // 낮·밤 머리 칸 자리에는 세로축 눈금 숫자
            _axis = new VisualElement();
            _axis.AddToClassList("planner-phase");
            _axis.AddToClassList("plan-flow-axis");
            for (var k = 0; k < AxisTicks; k++)
            {
                _ticks[k] = new Label { pickingMode = PickingMode.Ignore };
                _ticks[k].AddToClassList("plan-flow-tick");
                _axis.Add(_ticks[k]);
            }

            strip.Add(_axis);
            _graph = new VisualElement();
            _graph.AddToClassList("plan-flow-graph");
            _graph.generateVisualContent += Draw;
            _graph.RegisterCallback<GeometryChangedEvent>(_ => LayoutAxis());
            strip.Add(_graph);
            _root.Add(strip);

            // 아래 줄도 그래프 왼쪽 끝에서 시작한다
            var bottom = new VisualElement();
            bottom.AddToClassList("plan-flow-bottom");
            var gutter = new VisualElement();
            gutter.AddToClassList("plan-flow-gutter");
            bottom.Add(gutter);
            bottom.Add(Legend("flow.fatigue", "fatigue"));
            bottom.Add(Legend("flow.stress", "stress"));
            bottom.Add(Legend("flow.sleep_debt", "debt"));
            _stats = new VisualElement();
            _stats.AddToClassList("plan-flow-stats");
            bottom.Add(_stats);
            _summary = new Label();
            _summary.AddToClassList("plan-flow-summary");
            bottom.Add(_summary);
            _root.Add(bottom);
        }

        public WeekProjection Current => _current;

        public void Refresh()
        {
            var planning = _session.HasGame && _session.IsPlanning;
            _root.EnableInClassList("hidden", !planning);
            if (!planning)
            {
                _current = null;
                _ghost = null;
                return;
            }

            _current = _session.Projection();
            _ghost = null;
            Redraw();
        }

        /// <summary>바뀔 계획 미리 보기 (null이면 끈다).</summary>
        public void SetGhost(IList<string> ids)
        {
            _ghost = ids == null || !_session.IsPlanning ? null : _session.Projection(ids);
            Redraw();
        }

        /// <summary>마우스가 올라간 칸 (세로 줄로 밝힌다, -1이면 없음).</summary>
        public void SetFocus(int slot)
        {
            if (_focus == slot)
            {
                return;
            }

            _focus = slot;
            _graph.MarkDirtyRepaint();
        }

        private void Redraw()
        {
            _max = AxisMax();
            LayoutAxis();
            _graph.MarkDirtyRepaint();
            _stats.Clear();
            if (_current == null)
            {
                _summary.text = "";
                return;
            }

            for (var s = 0; s < WeekForecast.StatCount; s++)
            {
                var now = _current.StatTotals[s];
                var then = _ghost?.StatTotals[s] ?? now;
                if (Math.Abs(now) < 0.05f && Math.Abs(then) < 0.05f)
                {
                    continue;
                }

                var chip = new Label();
                chip.AddToClassList("flow-chip");
                var name = UiText.Stat((StatType)s);
                if (_ghost != null && Math.Abs(then - now) >= 0.05f)
                {
                    chip.text = name + " " + Signed(now) + " → " + Signed(then);
                    chip.AddToClassList(then > now ? "up" : "down");
                }
                else
                {
                    chip.text = name + " " + Signed(now);
                }

                _stats.Add(chip);
            }

            if (_stats.childCount == 0)
            {
                var none = new Label(Loc.T("flow.no_gain"));
                none.AddToClassList("flow-chip");
                none.AddToClassList("none");
                _stats.Add(none);
            }

            var last = (_ghost ?? _current).Last;
            _summary.text = Loc.F("flow.summary", last.Fatigue, last.Stress, last.SleepDebt);
            var balance = _session.Definitions.Balance;
            _summary.EnableInClassList("danger", last.Fatigue >= balance.TeacherFatigueLimit || last.Stress >= balance.TeacherStressLimit
                                                 || last.SleepDebt >= balance.TeacherSleepDebtLimit);
        }

        /// <summary>세로축 위 끝: 지금·바뀔 계획의 피로·스트레스 가운데 가장 큰 값을 20 단위로 올림 (20~100).</summary>
        private int AxisMax()
        {
            var peak = 0;
            foreach (var projection in new[] { _current, _ghost })
            {
                if (projection == null)
                {
                    continue;
                }

                peak = Math.Max(peak, Math.Max(projection.StartFatigue, projection.StartStress));
                foreach (var slot in projection.Slots)
                {
                    peak = Math.Max(peak, Math.Max(slot.Fatigue, slot.Stress));
                }
            }

            var max = (peak + AxisStep - 1) / AxisStep * AxisStep;
            return Mathf.Clamp(max, AxisStep, 100);
        }

        // 그래프 세로 자리 (Draw와 눈금 숫자가 같이 쓴다)
        private const float PlotTop = 6f;
        private const float DebtBand = 0.22f;

        private float PlotBottom(float height) => height - height * DebtBand - 2f;

        private float ValueY(float height, int value) => PlotBottom(height) - (PlotBottom(height) - PlotTop) * Mathf.Clamp(value, 0, _max) / _max;

        /// <summary>눈금 숫자를 그래프의 같은 높이에 놓는다.</summary>
        private void LayoutAxis()
        {
            var height = _graph.contentRect.height;
            var step = _max / AxisTicks;
            for (var k = 0; k < AxisTicks; k++)
            {
                var value = step * (k + 1);
                _ticks[k].text = value.ToString();
                _ticks[k].style.top = ValueY(height, value) - 8f;
                _ticks[k].style.display = height < 10f ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        private static string Signed(float value)
        {
            var text = value.ToString("0.#");
            return value > 0 ? "+" + text : text;
        }

        private static VisualElement Legend(string key, string kind)
        {
            var label = new Label(Loc.T(key));
            label.AddToClassList("flow-legend");
            label.AddToClassList(kind);
            return label;
        }

        // ── 그리기 ──

        private void Draw(MeshGenerationContext context)
        {
            if (_current == null)
            {
                return;
            }

            var rect = _graph.contentRect;
            if (rect.width < 10f || rect.height < 10f)
            {
                return;
            }

            var paint = context.painter2D;
            var balance = _session.Definitions.Balance;
            var debtBand = rect.height * DebtBand;

            float X(int slot) => (slot + 0.5f) * rect.width / WeekPlan.SlotCount;
            float Y(int value) => ValueY(rect.height, value);

            // 세로축 눈금선 (0 = 바닥선은 조금 진하게)
            paint.lineWidth = 1f;
            Line(paint, AxisColor, new Vector2(0, Y(0)), new Vector2(rect.width, Y(0)));
            for (var k = 1; k <= AxisTicks; k++)
            {
                var y = Y(_max / AxisTicks * k);
                Line(paint, GridColor, new Vector2(0, y), new Vector2(rect.width, y));
            }

            // 요일 칸 구분선, 마우스가 올라간 칸
            paint.lineWidth = 1f;
            for (var day = 1; day < CalendarState.DaysPerWeek; day++)
            {
                var x = day * 2 * rect.width / WeekPlan.SlotCount;
                Line(paint, GridColor, new Vector2(x, 0), new Vector2(x, rect.height));
            }

            if (_focus >= 0)
            {
                var w = rect.width / WeekPlan.SlotCount;
                paint.fillColor = new Color(1f, 1f, 1f, 0.06f);
                paint.BeginPath();
                paint.MoveTo(new Vector2(_focus * w, 0));
                paint.LineTo(new Vector2((_focus + 1) * w, 0));
                paint.LineTo(new Vector2((_focus + 1) * w, rect.height));
                paint.LineTo(new Vector2(_focus * w, rect.height));
                paint.ClosePath();
                paint.Fill();
            }

            // 위험선: 선생님이 다듬는 문턱 (지금 눈금 안에 들어올 때만)
            if (balance.TeacherFatigueLimit <= _max) Dashed(paint, DangerColor, Y(balance.TeacherFatigueLimit), rect.width);
            if (balance.TeacherStressLimit <= _max) Dashed(paint, DangerColor, Y(balance.TeacherStressLimit), rect.width);

            // 수면 부채 막대 (아래 띠)
            var source = _ghost ?? _current;
            var w2 = rect.width / WeekPlan.SlotCount;
            for (var i = 0; i < WeekPlan.SlotCount; i++)
            {
                var debt = source.Slots[i].SleepDebt;
                if (debt <= 0)
                {
                    continue;
                }

                var h = Mathf.Min(debtBand - 2f, debtBand * debt / Mathf.Max(1f, balance.TeacherSleepDebtLimit + 1f));
                paint.fillColor = debt >= balance.TeacherSleepDebtLimit ? DangerColor : DebtColor;
                paint.BeginPath();
                paint.MoveTo(new Vector2(i * w2 + 3f, rect.height - h));
                paint.LineTo(new Vector2((i + 1) * w2 - 3f, rect.height - h));
                paint.LineTo(new Vector2((i + 1) * w2 - 3f, rect.height));
                paint.LineTo(new Vector2(i * w2 + 3f, rect.height));
                paint.ClosePath();
                paint.Fill();
            }

            // 선: 끄는 중이면 지금 계획은 흐리게, 바뀔 계획을 색으로
            if (_ghost != null)
            {
                Series(paint, _current, OldColor, OldColor, X, Y, 2f);
                Series(paint, _ghost, FatigueColor, StressColor, X, Y, 3f);
            }
            else
            {
                Series(paint, _current, FatigueColor, StressColor, X, Y, 3f);
            }
        }

        private static void Series(Painter2D paint, WeekProjection projection, Color fatigue, Color stress, Func<int, float> x, Func<int, float> y, float width)
        {
            Polyline(paint, fatigue, width, projection.StartFatigue, projection.Slots.Select(s => s.Fatigue).ToArray(), x, y);
            Polyline(paint, stress, width, projection.StartStress, projection.Slots.Select(s => s.Stress).ToArray(), x, y);

            // 탐험처럼 무작위가 큰 칸은 속이 빈 점
            for (var i = 0; i < projection.Slots.Length; i++)
            {
                var slot = projection.Slots[i];
                Dot(paint, fatigue, new Vector2(x(i), y(slot.Fatigue)), slot.Uncertain);
                Dot(paint, stress, new Vector2(x(i), y(slot.Stress)), slot.Uncertain);
            }
        }

        private static void Polyline(Painter2D paint, Color color, float width, int start, int[] values, Func<int, float> x, Func<int, float> y)
        {
            paint.strokeColor = color;
            paint.lineWidth = width;
            paint.lineJoin = LineJoin.Round;
            paint.lineCap = LineCap.Round;
            paint.BeginPath();
            paint.MoveTo(new Vector2(0f, y(start)));
            for (var i = 0; i < values.Length; i++)
            {
                paint.LineTo(new Vector2(x(i), y(values[i])));
            }

            paint.Stroke();
        }

        private static void Dot(Painter2D paint, Color color, Vector2 at, bool hollow)
        {
            paint.BeginPath();
            paint.Arc(at, 3.5f, Angle.Degrees(0f), Angle.Degrees(360f));
            if (hollow)
            {
                paint.strokeColor = color;
                paint.lineWidth = 1.5f;
                paint.Stroke();
            }
            else
            {
                paint.fillColor = color;
                paint.Fill();
            }
        }

        private static void Line(Painter2D paint, Color color, Vector2 a, Vector2 b)
        {
            paint.strokeColor = color;
            paint.BeginPath();
            paint.MoveTo(a);
            paint.LineTo(b);
            paint.Stroke();
        }

        private static void Dashed(Painter2D paint, Color color, float y, float width)
        {
            paint.strokeColor = color;
            paint.lineWidth = 1f;
            for (var x = 0f; x < width; x += 10f)
            {
                paint.BeginPath();
                paint.MoveTo(new Vector2(x, y));
                paint.LineTo(new Vector2(Mathf.Min(width, x + 5f), y));
                paint.Stroke();
            }
        }
    }
}
