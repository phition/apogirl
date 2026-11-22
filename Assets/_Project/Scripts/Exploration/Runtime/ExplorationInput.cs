using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ApoGirl.Exploration.Runtime
{
    /// <summary>
    /// 탐험 입력 한 프레임: 키보드·마우스·가상 패드를 합친다. 탐험 화면은 키 대신 이것만 읽는다.
    /// 키보드: A/D 이동 · Shift 달리기 · Ctrl 웅크리기 · Q/좌클릭 공격 · E/우클릭 상호작용 · Space 숨기 · W 살피기 · S 듣기 · F 손전등 · Tab 배낭 · 1~4 퀵슬롯.
    /// 패드: 조이스틱 좌우·끝까지(달리기)·위(살피기)·아래(듣기), 단추 attack·interact·hide·crouch(토글)·flashlight·backpack·quick1~4.
    /// 패드를 쓰는 동안 마우스는 읽지 않는다 (터치가 마우스로 흉내 내져 두 번 눌리지 않게).
    /// </summary>
    public sealed class ExplorationInput
    {
        public float Move;
        public bool Run;
        public bool Crouch;
        public bool Attack;
        public bool InteractDown;
        public bool InteractHeld;
        public bool HideDown;
        public bool HideHeld;
        public bool Peek;
        public bool Listen;
        public bool Flashlight;
        public bool Backpack;
        public bool Advance;
        public int QuickSlot = -1;

        /// <summary>손전등 겨누기 기준 자리 (화면 좌표). 없으면 정면.</summary>
        public Vector2? AimScreen;

        public void Read(Keyboard keyboard, Mouse mouse, Func<Mouse, bool> mouseFree, VirtualPad pad)
        {
            var k = keyboard;
            var m = pad == null ? mouse : null;
            var free = m != null && mouseFree(m);

            var right = k != null && (k.dKey.isPressed || k.rightArrowKey.isPressed);
            var left = k != null && (k.aKey.isPressed || k.leftArrowKey.isPressed);
            Move = (right ? 1f : 0f) - (left ? 1f : 0f);
            Run = k != null && k.leftShiftKey.isPressed;
            Crouch = k != null && k.leftCtrlKey.isPressed;
            Attack = (k != null && k.qKey.wasPressedThisFrame) || (free && m.leftButton.wasPressedThisFrame);
            InteractDown = (k != null && k.eKey.wasPressedThisFrame) || (free && m.rightButton.wasPressedThisFrame);
            InteractHeld = (k != null && k.eKey.isPressed) || (free && m.rightButton.isPressed);
            HideDown = k != null && k.spaceKey.wasPressedThisFrame;
            HideHeld = k != null && k.spaceKey.isPressed;
            Peek = k != null && k.wKey.isPressed;
            Listen = k != null && k.sKey.isPressed;
            Flashlight = k != null && k.fKey.wasPressedThisFrame;
            Backpack = k != null && k.tabKey.wasPressedThisFrame;
            Advance = k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame);
            QuickSlot = k == null ? -1
                : k.digit1Key.wasPressedThisFrame ? 0
                : k.digit2Key.wasPressedThisFrame ? 1
                : k.digit3Key.wasPressedThisFrame ? 2
                : k.digit4Key.wasPressedThisFrame ? 3 : -1;
            AimScreen = m != null ? m.position.ReadValue() : (Vector2?)null;

            if (pad == null)
            {
                return;
            }

            // 가상 패드: 조이스틱은 가로가 세면 걷기, 세로가 세면 문 앞 감각
            var config = pad.Config;
            var stick = pad.Stick;
            var vertical = Mathf.Abs(stick.y) > Mathf.Abs(stick.x) && Mathf.Abs(stick.y) >= config.VerticalThreshold;
            if (vertical)
            {
                Peek |= stick.y > 0f;
                Listen |= stick.y < 0f;
            }
            else if (Mathf.Abs(stick.x) > 0f)
            {
                Move = Mathf.Sign(stick.x);
                Run |= Mathf.Abs(stick.x) >= config.RunThreshold;
            }

            Crouch |= pad.IsOn("crouch");
            Attack |= pad.TakeDown("attack");
            InteractDown |= pad.TakeDown("interact");
            InteractHeld |= pad.Held("interact");
            var hideDown = pad.TakeDown("hide");
            HideDown |= hideDown;
            HideHeld |= pad.Held("hide");
            Advance |= hideDown;
            Flashlight |= pad.TakeDown("flashlight");
            Backpack |= pad.TakeDown("backpack");
            for (var i = 0; i < 4; i++)
            {
                if (pad.TakeDown("quick" + (i + 1)) && QuickSlot < 0)
                {
                    QuickSlot = i;
                }
            }

            AimScreen = pad.AimScreen;
        }
    }
}
