using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ApoGirl.Presentation.Art;
using ApoGirl.Presentation.Audio;
using ApoGirl.Presentation.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace ApoGirl.Presentation.Dialogue
{
    /// <summary>
    /// 화면 아래 대화창 (문서 10장 비주얼 노벨 표준): 타이핑 → 클릭/Space로 완성·넘김, 스킵(선택지까지), 오토, 로그.
    /// Hub.uxml의 "dialogue" 요소를 쓴다.
    /// </summary>
    public sealed class DialogueView : IDialogueView
    {
        private const int TypingIntervalMs = 22;
        private const int AutoDelayMs = 1400;
        private const int SkipDelayMs = 60;

        private readonly VisualElement _layer;
        private readonly Label _speaker;
        private readonly Label _text;
        private readonly Label _next;
        private readonly VisualElement _options;
        private readonly ScrollView _log;
        private readonly Button _autoButton;
        private readonly Button _skipButton;
        private readonly List<string> _history = new List<string>();
        private readonly VisualElement _portrait;
        private readonly VisualElement _portraitFrame;
        private readonly VisualElement _box;
        private readonly Label _preview;

        /// <summary>말하는 사람 → 초상화 (M6-5). 비거나 null을 돌려주면 초상화를 숨긴다.</summary>
        public Func<string, Texture2D> PortraitFor { get; set; }

        private TaskCompletionSource<bool> _advance;
        private IVisualElementScheduledItem _typing;
        private string _full = "";
        private int _shown;
        private bool _auto;
        private bool _skip;

        public DialogueView(VisualElement root)
        {
            _layer = root.Q("dialogue");
            _speaker = root.Q<Label>("dialogue-speaker");
            _text = root.Q<Label>("dialogue-text");
            _next = root.Q<Label>("dialogue-next");
            _options = root.Q("dialogue-options");
            _log = root.Q<ScrollView>("dialogue-log");
            root.Q("dialogue-box").RegisterCallback<PointerDownEvent>(_ => Advance());

            // 초상화: 대화창 안 왼쪽, 글 앞에 선 그림 (그림이 없으면 숨김).
            // 가는 둥근 액자만 두르고 배경은 비운다 — 다시 그린 초상은 배경이 투명하다.
            _box = root.Q("dialogue-box");
            var body = new VisualElement { name = "dialogue-body", pickingMode = PickingMode.Ignore };
            body.AddToClassList("dialogue-body");
            while (_box.childCount > 0)
            {
                body.Add(_box[0]);
            }

            _portraitFrame = new VisualElement { name = "dialogue-portrait", pickingMode = PickingMode.Ignore };
            _portraitFrame.AddToClassList("dialogue-portrait");
            _portraitFrame.AddToClassList("hidden");
            _portrait = new VisualElement { name = "dialogue-portrait-image", pickingMode = PickingMode.Ignore };
            _portrait.AddToClassList("dialogue-portrait-image");
            _portraitFrame.Add(_portrait);
            _box.Add(_portraitFrame);
            _box.Add(body);

            // 디버그 "선택지 결과 미리 보기": 선택지 위에 마우스가 오면 대화창 위에 뜨는 판.
            // 칸마다가 아니라 선택지 묶음에서 받는다 — 고를 수 없는(비활성) 선택지 위에서도 뜨게.
            _preview = new Label { name = "dialogue-preview", pickingMode = PickingMode.Ignore };
            _preview.AddToClassList("dialogue-preview");
            _preview.AddToClassList("hidden");
            _layer.Add(_preview);
            _options.RegisterCallback<PointerMoveEvent>(evt => HoverOptions(evt.position), TrickleDown.TrickleDown);
            _options.RegisterCallback<PointerLeaveEvent>(_ => HidePreview());

            _autoButton = Tool(root, "dialogue-auto", Loc.T("dialogue.auto"), () =>
            {
                _auto = !_auto;
                _autoButton.EnableInClassList("selected", _auto);
                if (_auto && _shown >= _full.Length) ScheduleAuto();
            });
            _skipButton = Tool(root, "dialogue-skip", Loc.T("dialogue.skip"), () =>
            {
                if (_options.childCount > 0)
                {
                    return; // 선택지에서는 멈춘다
                }

                _skip = true;
                CompleteTyping();
                _advance?.TrySetResult(true);
            });
            Tool(root, "dialogue-log-button", Loc.T("dialogue.log"), ToggleLog);
        }

        public bool IsOpen => !_layer.ClassListContains("hidden");

        /// <summary>지금 떠 있는 미리 보기 글 (없으면 "").</summary>
        public string PreviewText => _preview.ClassListContains("hidden") ? "" : _preview.text;

        /// <summary>마우스 자리(패널 좌표)에 있는 선택지의 미리 보기를 띄운다. 선택지 밖이거나 미리 보기가 없으면 숨긴다.</summary>
        public void HoverOptions(Vector2 position)
        {
            foreach (var child in _options.Children())
            {
                if (child.userData is OptionPreview preview && child.worldBound.Contains(position))
                {
                    ShowPreview(preview);
                    return;
                }
            }

            HidePreview();
        }

        private void ShowPreview(OptionPreview preview)
        {
            if (preview.Text == null)
            {
                try
                {
                    preview.Text = preview.Make() ?? "";
                }
                catch (Exception exception)
                {
                    // 디버그 기능이 대화를 멈추게 하지 않는다
                    Debug.LogWarning("[선택지 미리 보기] " + exception.Message);
                    preview.Text = "";
                }
            }

            if (preview.Text.Length == 0)
            {
                HidePreview();
                return;
            }

            // 대화창 바로 위, 왼쪽 끝에 맞춘다
            var box = _layer.WorldToLocal(_box.worldBound);
            _preview.text = preview.Text;
            _preview.style.left = box.xMin;
            _preview.style.bottom = _layer.layout.height - box.yMin + 6f;
            _preview.RemoveFromClassList("hidden");
        }

        private void HidePreview() => _preview.AddToClassList("hidden");

        /// <summary>선택지 단추에 붙여 두는 미리 보기 (처음 마우스가 올 때 한 번 계산).</summary>
        private sealed class OptionPreview
        {
            public Func<string> Make;
            public string Text;
        }

        public void Open()
        {
            _skip = false;
            HidePreview();
            _options.Clear();
            _text.text = "";
            _speaker.text = "";
            _speaker.AddToClassList("hidden"); // 빈 종이 이름표가 남지 않게
            _log.AddToClassList("hidden");
            _layer.RemoveFromClassList("hidden");
            Sound.Play("Sfx/ui_page_turn", SoundCategory.Ui);
        }

        public void Close()
        {
            _typing?.Pause();
            HidePreview();
            _layer.AddToClassList("hidden");
            ShowPortrait(null);
            SetScene(null);
            Sound.StopVoice();
        }

        /// <summary>말하는 사람 초상 (null이면 액자째 숨기고 글이 왼쪽 끝부터).</summary>
        private void ShowPortrait(Texture2D portrait)
        {
            StoryArt.SetContain(_portrait, portrait);
            if (portrait != null)
            {
                // 발치(아래)에 붙여 세운다
                _portrait.style.backgroundPositionY = new StyleBackgroundPosition(new BackgroundPosition(BackgroundPositionKeyword.Bottom));
            }

            _portraitFrame.EnableInClassList("hidden", portrait == null);
            _box.EnableInClassList("with-portrait", portrait != null);
        }

        /// <summary>대화 뒤에 깔 사건 그림 (null이면 지운다).</summary>
        public void SetScene(Texture2D scene)
        {
            StoryArt.SetCover(_layer, scene);
            _layer.EnableInClassList("with-scene", scene != null);
        }

        /// <summary>클릭·Space·Enter: 타이핑 중이면 완성, 아니면 다음 줄.</summary>
        public void Advance()
        {
            if (!IsOpen || _options.childCount > 0)
            {
                return;
            }

            if (_shown < _full.Length)
            {
                CompleteTyping();
                return;
            }

            _advance?.TrySetResult(true);
        }

        public async Task ShowLine(string speaker, string text, CancellationToken token)
        {
            HidePreview();
            _options.Clear();
            _speaker.text = speaker ?? "";
            _speaker.EnableInClassList("hidden", string.IsNullOrEmpty(speaker));
            var portrait = string.IsNullOrEmpty(speaker) ? null : PortraitFor?.Invoke(speaker);
            ShowPortrait(portrait);
            _history.Add(string.IsNullOrEmpty(speaker) ? text : speaker + ": " + text);
            Sound.OnLine(speaker, text);
            _full = text ?? "";
            _advance = new TaskCompletionSource<bool>();
            _next.AddToClassList("hidden");
            _typing?.Pause();
            if (_skip)
            {
                CompleteTyping();
                _text.schedule.Execute(() => _advance?.TrySetResult(true)).ExecuteLater(SkipDelayMs);
            }
            else
            {
                _shown = 0;
                _text.text = "";
                _typing = _text.schedule.Execute(TypeNext).Every(TypingIntervalMs);
            }

            using (token.Register(() => _advance?.TrySetCanceled()))
            {
                await _advance.Task;
            }
        }

        public async Task<int> ShowOptions(IReadOnlyList<DialogueChoice> choices, CancellationToken token)
        {
            _skip = false;
            _next.AddToClassList("hidden");
            var picked = new TaskCompletionSource<int>();
            _options.Clear();
            foreach (var choice in choices)
            {
                var label = choice.Text;
                if (choice.Chance.HasValue)
                {
                    label += "  " + Loc.F("dialogue.chance", (int)Math.Round(choice.Chance.Value * 100f));
                }

                var id = choice.OptionId;
                var text = choice.Text;
                var button = new Button(() =>
                {
                    _history.Add("▶ " + text);
                    HidePreview();
                    _options.Clear();
                    picked.TrySetResult(id);
                }) { text = label };
                button.AddToClassList("dialogue-option");
                button.SetEnabled(choice.Enabled);
                if (choice.Preview != null)
                {
                    button.userData = new OptionPreview { Make = choice.Preview };
                }

                _options.Add(button);
            }

            using (token.Register(() => picked.TrySetCanceled()))
            {
                return await picked.Task;
            }
        }

        private void TypeNext()
        {
            _shown = Math.Min(_full.Length, _shown + 1);
            _text.text = _full.Substring(0, _shown);
            if (_shown >= _full.Length)
            {
                CompleteTyping();
            }
        }

        private void CompleteTyping()
        {
            _typing?.Pause();
            _shown = _full.Length;
            _text.text = _full;
            _next.RemoveFromClassList("hidden");
            if (_auto && !_skip)
            {
                ScheduleAuto();
            }
        }

        private void ScheduleAuto()
        {
            var target = _advance;
            _text.schedule.Execute(() =>
            {
                if (_auto && target == _advance)
                {
                    target?.TrySetResult(true);
                }
            }).ExecuteLater(AutoDelayMs);
        }

        private void ToggleLog()
        {
            var show = _log.ClassListContains("hidden");
            _log.Clear();
            if (show)
            {
                foreach (var entry in _history)
                {
                    var label = new Label(entry);
                    label.AddToClassList("dialogue-log-line");
                    _log.Add(label);
                }

                _log.schedule.Execute(() => _log.scrollOffset = new UnityEngine.Vector2(0f, float.MaxValue));
            }

            _log.EnableInClassList("hidden", !show);
        }

        private static Button Tool(VisualElement root, string name, string text, Action onClick)
        {
            var button = root.Q<Button>(name);
            button.text = text;
            button.clicked += onClick;
            return button;
        }
    }
}
