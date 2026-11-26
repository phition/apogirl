using System;
using ApoGirl.Presentation.Text;
using UnityEngine.UIElements;

namespace ApoGirl.Presentation.Screens
{
    /// <summary>
    /// 화면 중앙 카드 하나를 돌려 쓰는 오버레이 (결과 카드, 주간 정산, 저장, 메뉴, 종료 화면).
    /// 본문은 타이핑 연출 가능: 클릭하면 즉시 완성.
    /// </summary>
    public sealed class CardOverlay
    {
        private const int TypingIntervalMs = 18;

        private readonly VisualElement _overlay;
        private readonly VisualElement _cardBox;
        private readonly Label _title;
        private readonly Label _body;
        private readonly VisualElement _extra;
        private readonly VisualElement _buttons;
        private IVisualElementScheduledItem _typing;
        private string _fullBody = "";
        private int _shown;
        private string _variant;

        public CardOverlay(VisualElement root)
        {
            _overlay = root.Q("overlay");
            _cardBox = root.Q("card");
            _title = root.Q<Label>("card-title");
            _body = root.Q<Label>("card-body");
            _extra = root.Q("card-extra");
            _buttons = root.Q("card-buttons");
            _body.RegisterCallback<PointerDownEvent>(_ => CompleteTyping());
        }

        public bool IsOpen => !_overlay.ClassListContains("hidden");

        public VisualElement Extra => _extra;

        public void Show(string title, string body, bool typing, params (string label, Action onClick)[] buttons)
        {
            _title.text = title;
            SetVariant(null);
            _extra.Clear();
            _buttons.Clear();
            foreach (var (label, onClick) in buttons)
            {
                var button = new Button(() =>
                {
                    if (_typing != null && _shown < _fullBody.Length)
                    {
                        CompleteTyping();
                        return;
                    }

                    onClick?.Invoke();
                }) { text = label };
                button.AddToClassList("card-button");
                _buttons.Add(button);
            }

            _fullBody = body ?? "";
            _typing?.Pause();
            if (typing && _fullBody.Length > 0)
            {
                _shown = 0;
                _body.text = "";
                _typing = _body.schedule.Execute(TypeNext).Every(TypingIntervalMs);
            }
            else
            {
                _shown = _fullBody.Length;
                _body.text = _fullBody;
            }

            _overlay.RemoveFromClassList("hidden");
        }

        public void Hide()
        {
            _typing?.Pause();
            _overlay.AddToClassList("hidden");
        }

        /// <summary>카드 모양 갈래 (예: "day-report" = 본문 칸 없이 Extra만). Show가 부를 때마다 지워지니 Show 뒤에 부른다.</summary>
        public void SetVariant(string cssClass)
        {
            if (_cardBox == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(_variant))
            {
                _cardBox.RemoveFromClassList(_variant);
            }

            _variant = cssClass;
            if (!string.IsNullOrEmpty(_variant))
            {
                _cardBox.AddToClassList(_variant);
            }
        }

        /// <summary>닫기 버튼 하나짜리 카드.</summary>
        public void ShowMessage(string title, string body)
        {
            Show(title, body, false, (Loc.T("common.close"), Hide));
        }

        private void TypeNext()
        {
            _shown = Math.Min(_fullBody.Length, _shown + 2);
            _body.text = _fullBody.Substring(0, _shown);
            if (_shown >= _fullBody.Length)
            {
                _typing.Pause();
            }
        }

        private void CompleteTyping()
        {
            _typing?.Pause();
            _shown = _fullBody.Length;
            _body.text = _fullBody;
        }
    }
}
