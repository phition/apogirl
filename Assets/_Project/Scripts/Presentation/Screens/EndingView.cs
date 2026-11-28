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

            // 엔딩 원화 (M6-5, 영상 대신): 그림 위에 글자가 읽히도록 컷 무대에 어두운 판을 깐다
            // 엔딩 변형 그림이 아직 없으면 엔딩 기본 그림
            var art = Art.StoryArt.Ending(result.ArtKey) ?? Art.StoryArt.Ending(result.Ending?.Id);
            Art.StoryArt.SetCover(_root, art);
            _root.EnableInClassList("with-art", art != null);

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
            // 세 막 엔딩: 막 제목 카드 → 컷 → 명단 … 차례대로 (EndingResult.Pages). 배드 엔딩은 컷만
            if (_step < _result.Pages.Count)
            {
                var page = _result.Pages[_step];
                switch (page.Kind)
                {
                    case EndingPageKind.ActTitle:
                        ShowActTitle(page);
                        break;
                    case EndingPageKind.Roster:
                        ShowEpilogue(page.Lines);
                        break;
                    default:
                        ShowCut(page.Key);
                        break;
                }
            }
            else
            {
                ShowRecord();
            }
        }

        /// <summary>막 제목 카드: "구조의 날 / 돌아보는 밤 / 그 뒤의 나"와 그 막에서 고른 조각의 제목.</summary>
        private void ShowActTitle(EndingPage page)
        {
            _stage.Clear();
            var act = new Label(Loc.T(page.Key));
            act.AddToClassList("ending-category");
            _stage.Add(act);
            var piece = _result.Of(page.Act)?.Piece;
            if (piece != null)
            {
                var title = new Label(StoryText.T(piece.TitleKey, _state));
                title.AddToClassList("ending-act-title");
                _stage.Add(title);
            }

            KoreanFont.ApplyDeep(_stage);
            FadeIn(_stage);
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
            var sub = SubTitle();
            if (sub.Length > 0)
            {
                // 세 막 엔딩: 큰 제목(그 뒤의 나) 아래에 작게 회고 축 · 구조 장면
                var small = new Label(sub);
                small.AddToClassList("ending-subtitle");
                _stage.Add(small);
            }
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
            var counter = new Label($"{_step + 1} / {TotalPages}");
            counter.AddToClassList("ending-counter");
            _stage.Add(counter);
            KoreanFont.ApplyDeep(_stage);
            FadeIn(_stage);
        }

        /// <summary>넘기는 장 수 (막 제목·컷·명단).</summary>
        private int TotalPages => _result.Pages.Count;

        /// <summary>큰 제목 아래의 작은 줄: 회고 축 · 구조 장면 (대표 엔딩과 같은 조각은 뺀다). 배드 엔딩은 없다.</summary>
        private string SubTitle()
        {
            var parts = new List<string>();
            foreach (var act in new[] { EndingAct.Reflection, EndingAct.Rescue })
            {
                var piece = _result.Of(act)?.Piece;
                if (piece != null && piece != _result.Ending)
                {
                    parts.Add(StoryText.T(piece.TitleKey, _state));
                }
            }

            return string.Join(" · ", parts);
        }

        /// <summary>명단: 함께한 사람들과 세력의 '그 뒤'를 한 장에 모아 보여 준다.</summary>
        private void ShowEpilogue(List<string> keys)
        {
            _stage.Clear();
            var header = new Label(Loc.T("ending.roster"));
            header.AddToClassList("ending-category");
            header.style.marginBottom = 18;
            _stage.Add(header);
            foreach (var key in keys)
            {
                var line = new Label(StoryText.T(key, _state));
                line.AddToClassList("ending-epilogue-line");
                line.style.whiteSpace = WhiteSpace.Normal;
                line.style.marginTop = 8;
                _stage.Add(line);
            }

            var counter = new Label($"{_step + 1} / {TotalPages}");
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

            // 세 막 한 줄씩: 구조의 날 · 돌아보는 밤 · 그 뒤의 나
            foreach (var act in _result.Acts)
            {
                if (act.Piece != null)
                {
                    AddLine(scroll, Loc.T("ending.act." + act.Act) + " — " + StoryText.T(act.Piece.TitleKey, _state), "ending-record-line");
                }
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

            if (_result.EpilogueKeys.Count + _result.MoreEpilogueKeys.Count > 0)
            {
                // 명단에 실은 줄과, 다 못 실은 나머지 소식
                AddSection(scroll, Loc.T("ending.epilogue"));
                foreach (var key in _result.EpilogueKeys)
                {
                    AddLine(scroll, "· " + StoryText.T(key, _state), "ending-record-line");
                }

                foreach (var key in _result.MoreEpilogueKeys)
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
    }
}
