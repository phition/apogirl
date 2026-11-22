using System;
using System.Collections.Generic;
using ApoGirl.Presentation.Art;
using ApoGirl.Presentation.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace ApoGirl.Exploration.Runtime
{
    /// <summary>
    /// 탐험 가상 패드 (휴대폰): 왼쪽 화면 어디든 눌러 끄는 떠다니는 조이스틱 + 오른쪽 단추.
    /// 조이스틱 좌우 = 걷기(끝까지 밀면 달리기), 위 = 문틈 살피기(W), 아래 = 귀 기울이기(S).
    /// 오른쪽 빈 곳을 위아래로 끌면 손전등을 겨눈다 (손을 떼도 그 자리를 기억).
    /// 자리·크기는 Resources/Art/virtual_pad.json. HUD 창(배낭·전리품·조우)보다 아래 겹이라 창 위 터치는 창이 받는다.
    /// </summary>
    public sealed class VirtualPad
    {
        [Serializable]
        public sealed class ButtonDef
        {
            public string Id = "";
            public string Label = "";   // 현지화 키
            public string Icon = "";    // art_map 아이콘 키 (없으면 글자만)
            public float Right;
            public float Bottom;
            public float Size = 100f;
            public bool Toggle;
        }

        [Serializable]
        public sealed class PadConfig
        {
            public float Opacity = 0.6f;
            public float StickRadius = 90f;
            public float Deadzone = 0.2f;
            public float RunThreshold = 0.85f;
            public float VerticalThreshold = 0.6f;
            public float StickZoneWidth = 0.45f;   // 화면 너비 비율 (왼쪽부터)
            public float AimZoneStart = 0.5f;      // 이 비율부터 오른쪽 끝까지 = 손전등 겨누기 자리
            public float LabelSize = 22f;
            public List<ButtonDef> Buttons = new List<ButtonDef>();
        }

        private sealed class PadButton
        {
            public ButtonDef Def;
            public VisualElement Element;
            public int PointerId = -1;
            public bool Held;
            public bool Down;   // 지난 읽기 뒤로 눌렸는가
            public bool On;     // 토글 상태
        }

        public const string ConfigPath = "Art/virtual_pad";

        private readonly PadConfig _config;
        private readonly VisualElement _root;
        private readonly VisualElement _stickBase;
        private readonly VisualElement _stickKnob;
        private readonly Dictionary<string, PadButton> _buttons = new Dictionary<string, PadButton>();
        private int _stickPointer = -1;
        private Vector2 _stickOrigin;
        private int _aimPointer = -1;

        /// <summary>조이스틱 (−1~1, 위가 +).</summary>
        public Vector2 Stick { get; private set; }

        /// <summary>손전등 겨누기 자리 (화면 좌표, 왼쪽 아래 원점). 한 번도 끌지 않았으면 null.</summary>
        public Vector2? AimScreen { get; private set; }

        public PadConfig Config => _config;

        public static PadConfig LoadConfig()
        {
            var asset = Resources.Load<TextAsset>(ConfigPath);
            var config = asset != null ? JsonUtility.FromJson<PadConfig>(asset.text) : null;
            return config ?? new PadConfig();
        }

        public VirtualPad(VisualElement parent, PadConfig config)
        {
            _config = config ?? new PadConfig();
            _root = new VisualElement { name = "virtual-pad", pickingMode = PickingMode.Ignore };
            Stretch(_root);
            _root.style.opacity = _config.Opacity;
            parent.Insert(0, _root);   // HUD 창보다 아래 겹

            var stickZone = new VisualElement { name = "pad-stick-zone" };
            stickZone.style.position = Position.Absolute;
            stickZone.style.left = 0;
            stickZone.style.top = Length.Percent(18);   // 위쪽 정보 줄은 비워 둔다
            stickZone.style.bottom = 0;
            stickZone.style.width = Length.Percent(_config.StickZoneWidth * 100f);
            _root.Add(stickZone);
            stickZone.RegisterCallback<PointerDownEvent>(OnStickDown);
            stickZone.RegisterCallback<PointerMoveEvent>(OnStickMove);
            stickZone.RegisterCallback<PointerUpEvent>(e => EndStick(stickZone, e.pointerId));
            stickZone.RegisterCallback<PointerCancelEvent>(e => EndStick(stickZone, e.pointerId));

            var aimZone = new VisualElement { name = "pad-aim-zone" };
            aimZone.style.position = Position.Absolute;
            aimZone.style.right = 0;
            aimZone.style.top = Length.Percent(18);
            aimZone.style.bottom = 0;
            aimZone.style.width = Length.Percent((1f - _config.AimZoneStart) * 100f);
            _root.Add(aimZone);
            aimZone.RegisterCallback<PointerDownEvent>(e =>
            {
                if (_aimPointer >= 0)
                {
                    return;
                }

                _aimPointer = e.pointerId;
                aimZone.CapturePointer(e.pointerId);
                SetAim(e.position);
            });
            aimZone.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (e.pointerId == _aimPointer)
                {
                    SetAim(e.position);
                }
            });
            aimZone.RegisterCallback<PointerUpEvent>(e => EndAim(aimZone, e.pointerId));
            aimZone.RegisterCallback<PointerCancelEvent>(e => EndAim(aimZone, e.pointerId));

            _stickBase = Circle(_config.StickRadius * 2f, new Color(1f, 1f, 1f, 0.12f));
            _stickBase.style.display = DisplayStyle.None;
            _root.Add(_stickBase);
            _stickKnob = Circle(_config.StickRadius * 0.9f, new Color(1f, 1f, 1f, 0.45f));
            _stickKnob.style.position = Position.Absolute;
            _stickBase.Add(_stickKnob);
            CenterKnob(Vector2.zero);

            foreach (var def in _config.Buttons)
            {
                AddButton(def);
            }

            KoreanFont.ApplyDeep(_root);
        }

        public bool Visible
        {
            get => _root.style.display != DisplayStyle.None;
            set => _root.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>단추를 누르고 있는가.</summary>
        public bool Held(string id) => _buttons.TryGetValue(id, out var button) && button.Held;

        /// <summary>지난 읽기 뒤로 눌렸는가 (읽으면 지워진다).</summary>
        public bool TakeDown(string id)
        {
            if (!_buttons.TryGetValue(id, out var button) || !button.Down)
            {
                return false;
            }

            button.Down = false;
            return true;
        }

        /// <summary>토글 단추 상태.</summary>
        public bool IsOn(string id) => _buttons.TryGetValue(id, out var button) && button.On;

        public void Remove() => _root.RemoveFromHierarchy();

        // ── 조이스틱 ──

        private void OnStickDown(PointerDownEvent e)
        {
            if (_stickPointer >= 0)
            {
                return;
            }

            _stickPointer = e.pointerId;
            ((VisualElement)e.currentTarget).CapturePointer(e.pointerId);
            _stickOrigin = (Vector2)e.position;
            _stickBase.style.display = DisplayStyle.Flex;
            _stickBase.style.left = _stickOrigin.x - _config.StickRadius;
            _stickBase.style.top = _stickOrigin.y - _config.StickRadius;
            UpdateStick(_stickOrigin);
        }

        private void OnStickMove(PointerMoveEvent e)
        {
            if (e.pointerId == _stickPointer)
            {
                UpdateStick(e.position);
            }
        }

        private void UpdateStick(Vector2 position)
        {
            var offset = Vector2.ClampMagnitude(position - _stickOrigin, _config.StickRadius);
            CenterKnob(offset);
            var value = offset / Mathf.Max(1f, _config.StickRadius);
            value.y = -value.y;   // 화면 아래 → 위가 +
            Stick = value.magnitude < _config.Deadzone ? Vector2.zero : value;
        }

        private void EndStick(VisualElement zone, int pointerId)
        {
            if (pointerId != _stickPointer)
            {
                return;
            }

            zone.ReleasePointer(pointerId);
            _stickPointer = -1;
            Stick = Vector2.zero;
            _stickBase.style.display = DisplayStyle.None;
            CenterKnob(Vector2.zero);
        }

        private void CenterKnob(Vector2 offset)
        {
            var knob = _config.StickRadius * 0.45f;
            _stickKnob.style.left = _config.StickRadius - knob + offset.x;
            _stickKnob.style.top = _config.StickRadius - knob + offset.y;
        }

        // ── 손전등 겨누기 ──

        private void SetAim(Vector2 panelPosition)
        {
            var size = _root.panel?.visualTree.layout.size ?? Vector2.zero;
            if (size.x <= 0f || size.y <= 0f)
            {
                return;
            }

            AimScreen = new Vector2(panelPosition.x / size.x * Screen.width, (1f - panelPosition.y / size.y) * Screen.height);
        }

        private void EndAim(VisualElement zone, int pointerId)
        {
            if (pointerId == _aimPointer)
            {
                zone.ReleasePointer(pointerId);
                _aimPointer = -1;
            }
        }

        // ── 단추 ──

        private void AddButton(ButtonDef def)
        {
            var element = Circle(def.Size, new Color(0.08f, 0.08f, 0.1f, 0.55f));
            element.name = "pad-" + def.Id;
            element.style.right = def.Right;
            element.style.bottom = def.Bottom;
            element.style.alignItems = Align.Center;
            element.style.justifyContent = Justify.Center;
            SetBorder(element, 3f, new Color(1f, 1f, 1f, 0.6f));

            var icon = string.IsNullOrEmpty(def.Icon) ? null : StoryArt.Icon(def.Icon);
            if (icon != null)
            {
                var image = new VisualElement { pickingMode = PickingMode.Ignore };
                image.style.width = image.style.height = def.Size * 0.42f;
                image.style.backgroundImage = new StyleBackground(icon);
                element.Add(image);
            }

            var label = new Label(Loc.T(def.Label)) { pickingMode = PickingMode.Ignore };
            var text = Loc.T(def.Label);
            var fit = def.Size * 0.78f / Mathf.Max(1, text.Length);   // 한 줄에 들어가게
            label.style.fontSize = Mathf.Min(_config.LabelSize * Mathf.Clamp(def.Size / 100f, 0.7f, 1.2f), fit);
            label.style.whiteSpace = WhiteSpace.NoWrap;
            label.style.color = Color.white;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.marginTop = label.style.marginBottom = 0;
            label.style.paddingTop = label.style.paddingBottom = 0;
            element.Add(label);
            _root.Add(element);

            var button = new PadButton { Def = def, Element = element };
            _buttons[def.Id] = button;
            element.RegisterCallback<PointerDownEvent>(e =>
            {
                if (button.PointerId >= 0)
                {
                    return;
                }

                button.PointerId = e.pointerId;
                element.CapturePointer(e.pointerId);
                button.Held = true;
                button.Down = true;
                if (def.Toggle)
                {
                    button.On = !button.On;
                }

                Paint(button);
                e.StopPropagation();
            });
            element.RegisterCallback<PointerUpEvent>(e => Release(button, e.pointerId));
            element.RegisterCallback<PointerCancelEvent>(e => Release(button, e.pointerId));
            element.RegisterCallback<PointerCaptureOutEvent>(e => Release(button, button.PointerId));
            Paint(button);
        }

        private static void Release(PadButton button, int pointerId)
        {
            if (pointerId != button.PointerId || pointerId < 0)
            {
                return;
            }

            if (button.Element.HasPointerCapture(pointerId))
            {
                button.Element.ReleasePointer(pointerId);
            }

            button.PointerId = -1;
            button.Held = false;
            Paint(button);
        }

        private static void Paint(PadButton button)
        {
            var lit = button.Held || button.On;
            button.Element.style.backgroundColor = lit ? new Color(0.95f, 0.85f, 0.55f, 0.7f) : new Color(0.08f, 0.08f, 0.1f, 0.55f);
        }

        private static VisualElement Circle(float size, Color color)
        {
            var element = new VisualElement();
            element.style.position = Position.Absolute;
            element.style.width = size;
            element.style.height = size;
            element.style.backgroundColor = color;
            var radius = size * 0.5f;
            element.style.borderTopLeftRadius = element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = element.style.borderBottomRightRadius = radius;
            return element;
        }

        private static void SetBorder(VisualElement element, float width, Color color)
        {
            element.style.borderTopWidth = element.style.borderBottomWidth = width;
            element.style.borderLeftWidth = element.style.borderRightWidth = width;
            element.style.borderTopColor = element.style.borderBottomColor = color;
            element.style.borderLeftColor = element.style.borderRightColor = color;
        }

        private static void Stretch(VisualElement element)
        {
            element.style.position = Position.Absolute;
            element.style.left = element.style.right = element.style.top = element.style.bottom = 0;
        }
    }
}
