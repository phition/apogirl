using System;
using System.Collections.Generic;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Endings;
using ApoGirl.Core.State;
using ApoGirl.Presentation.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace ApoGirl.Presentation.Screens
{
    /// <summary>
    /// 엔딩 연출 (문서 8.1-3, 8.3): 제목 → 후일담 컷 (클릭·Space로 넘김) → 1년의 기록 → 타이틀로.
    /// 일러스트가 들어오기 전까지는 엔딩별 배경색과 글만 쓴다 (M5-d).
    /// </summary>
    public sealed class EndingView
    {
        private const float FadeSeconds = 0.6f;

        private readonly VisualElement _root;
        private readonly GameState _state;
        private readonly EndingResult _result;
        private readonly bool _isNew;
        private readonly Action _onFinished;
        private readonly VisualElement _stage;
        private readonly Label _hint;
        private int _step = -1;
        private bool _recordShown;

        public EndingView(VisualElement parent, GameState state, EndingResult result, bool isNew, Action onFinished)
        {
            _state = state;
            _result = result;
            _isNew = isNew;
            _onFinished = onFinished;

            _root = new VisualElement { name = "ending-view" };
            _root.AddToClassList("ending-view");
            _root.style.backgroundColor = BackgroundColor();
            parent.Add(_root);

            _stage = new VisualElement();
            _stage.AddToClassList("ending-stage");
            _root.Add(_stage);

            _hint = new Label(Loc.T("ending.click"));
            _hint.AddToClassList("ending-hint");
            _root.Add(_hint);

            _root.RegisterCallback<PointerDownEvent>(_ => Advance());
            KoreanFont.ApplyDeep(_root);
            ShowTitle();
        }

        public bool IsOpen => _root.parent != null;

        /// <summary>다음 컷으로 (기록 화면에서는 무시: 버튼으로 나간다).</summary>
        public void Advance()
        {
            if (_recordShown)
            {
                return;
            }

            _step++;
            if (_step < _result.CutKeys.Count)
            {
                ShowCut(_result.CutKeys[_step]);
            }
            else
            {
                ShowRecord();
            }
        }

        private void ShowTitle()
        {
            _stage.Clear();
            var ending = _result.Ending;
            var category = new Label(ending == null ? "" : Loc.T("ending.cat." + ending.Category));
            category.AddToClassList("ending-category");
            var title = new Label(ending == null ? Loc.T("end." + _state.Status) : StoryText.T(ending.TitleKey, _state));
            title.AddToClassList("ending-title");
            _stage.Add(category);
            _stage.Add(title);
            if (_isNew)
            {
                var badge = new Label(Loc.T("ending.new"));
                badge.AddToClassList("ending-new");
                _stage.Add(badge);
            }

            FadeIn(_stage);
        }

        private void ShowCut(string key)
        {
            _stage.Clear();
            var text = new Label(StoryText.T(key, _state));
            text.AddToClassList("ending-cut");
            _stage.Add(text);
            var counter = new Label($"{_step + 1} / {_result.CutKeys.Count}");
            counter.AddToClassList("ending-counter");
            _stage.Add(counter);
            KoreanFont.ApplyDeep(_stage);
            FadeIn(_stage);
        }

        private void ShowRecord()
        {
            _recordShown = true;
            _hint.AddToClassList("hidden");
            _stage.Clear();
            _stage.AddToClassList("ending-record");

            var header = new Label(Loc.T("ending.record_title"));
            header.AddToClassList("ending-record-title");
            _stage.Add(header);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("ending-record-scroll");
            _stage.Add(scroll);

            var ending = _result.Ending;
            if (ending != null)
            {
                AddLine(scroll, Loc.T("ending.cat." + ending.Category) + " · " + StoryText.T(ending.TitleKey, _state), "ending-record-ending");
            }

            AddLine(scroll, Loc.F("ending.weeks", _state.Calendar.Week), "ending-record-line");

            // 능력치 그래프 (주간 기록)
            AddSection(scroll, Loc.T("ending.stats"));
            scroll.Add(new StatGraph(_state));

            var exploration = _state.Exploration;
            AddLine(scroll, Loc.F("ending.tendency", exploration.DayExplorations, exploration.NightExplorations, exploration.Downs), "ending-record-line");
            AddLine(scroll, Companions(), "ending-record-line");

            if (_result.MomentKeys.Count > 0)
            {
                AddSection(scroll, Loc.T("ending.moments"));
                foreach (var key in _result.MomentKeys)
                {
                    AddLine(scroll, "· " + StoryText.T(key, _state), "ending-record-line");
                }
            }

            if (_state.Diary.Count > 0)
            {
                AddSection(scroll, Loc.T("ending.diary"));
                foreach (var entry in _state.Diary)
                {
                    var text = Loc.Lookup(Loc.UiTable, entry.TextKey);
                    if (!string.IsNullOrEmpty(text))
                    {
                        AddLine(scroll, Loc.F("ending.diary_line", entry.Week, StoryText.Format(text, _state)), "ending-record-diary");
                    }
                }
            }

            var button = new Button(Finish) { text = Loc.T("ending.to_title") };
            button.AddToClassList("ending-button");
            _stage.Add(button);
            KoreanFont.ApplyDeep(_stage);
            FadeIn(_stage);
        }

        private string Companions()
        {
            var names = new List<string>();
            foreach (var pair in _state.Npcs)
            {
                if (pair.Value.Status == NpcStatus.Joined)
                {
                    names.Add(Loc.T("npc." + pair.Key));
                }
            }

            return names.Count == 0 ? Loc.T("ending.companions_none") : Loc.F("ending.companions", string.Join(", ", names));
        }

        private void Finish()
        {
            _root.RemoveFromHierarchy();
            _onFinished?.Invoke();
        }

        private static void AddSection(VisualElement parent, string text)
        {
            var label = new Label(text);
            label.AddToClassList("ending-record-section");
            parent.Add(label);
        }

        private static void AddLine(VisualElement parent, string text, string cls)
        {
            var label = new Label(text);
            label.AddToClassList(cls);
            parent.Add(label);
        }

        private StyleColor BackgroundColor()
        {
            var hex = _result.Ending?.Color;
            return !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out var color) ? color : new Color(0.12f, 0.13f, 0.18f);
        }

        /// <summary>짧은 페이드 인 (스타일 전환 대신 스케줄러로 투명도를 올린다).</summary>
        private static void FadeIn(VisualElement element)
        {
            element.style.opacity = 0f;
            var start = Time.realtimeSinceStartup;
            IVisualElementScheduledItem item = null;
            item = element.schedule.Execute(() =>
            {
                var t = Mathf.Clamp01((Time.realtimeSinceStartup - start) / FadeSeconds);
                element.style.opacity = t;
                if (t >= 1f)
                {
                    item.Pause();
                }
            }).Every(16);
        }

        /// <summary>능력치 8종의 주간 변화 꺾은선 + 범례 (주간 기록 History).</summary>
        private sealed class StatGraph : VisualElement
        {
            private static readonly Color[] Palette =
            {
                new Color(0.85f, 0.45f, 0.40f), new Color(0.90f, 0.62f, 0.35f), new Color(0.88f, 0.82f, 0.45f), new Color(0.55f, 0.78f, 0.48f),
                new Color(0.45f, 0.75f, 0.78f), new Color(0.50f, 0.58f, 0.88f), new Color(0.72f, 0.55f, 0.85f), new Color(0.85f, 0.55f, 0.70f),
            };

            private readonly List<WeekRecord> _history;

            public StatGraph(GameState state)
            {
                _history = state.History;
                AddToClassList("ending-graph-box");

                var canvas = new VisualElement();
                canvas.AddToClassList("ending-graph");
                canvas.generateVisualContent += Draw;
                Add(canvas);

                var legend = new VisualElement();
                legend.AddToClassList("ending-legend");
                var stats = (StatType[])Enum.GetValues(typeof(StatType));
                for (var i = 0; i < stats.Length; i++)
                {
                    var item = new Label(Loc.T("stat." + stats[i]) + " " + state.Player.GetStat(stats[i]));
                    item.AddToClassList("ending-legend-item");
                    item.style.color = Palette[i % Palette.Length];
                    legend.Add(item);
                }

                Add(legend);
            }

            private void Draw(MeshGenerationContext context)
            {
                var rect = context.visualElement.contentRect;
                if (_history.Count == 0 || rect.width <= 1f || rect.height <= 1f)
                {
                    return;
                }

                var max = 10;
                foreach (var record in _history)
                {
                    foreach (var value in record.Stats.Values)
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
                var weeks = Math.Max(1, _history.Count - 1);
                for (var i = 0; i < stats.Length; i++)
                {
                    painter.strokeColor = Palette[i % Palette.Length];
                    painter.BeginPath();
                    for (var w = 0; w < _history.Count; w++)
                    {
                        _history[w].Stats.TryGetValue(stats[i], out var value);
                        var point = new Vector2(rect.width * w / weeks, rect.height * (1f - (float)value / max));
                        if (w == 0) painter.MoveTo(point);
                        else painter.LineTo(point);
                    }

                    painter.Stroke();
                }
            }
        }
    }
}
