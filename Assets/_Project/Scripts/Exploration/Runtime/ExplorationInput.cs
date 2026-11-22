using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ApoGirl.Exploration.Runtime
{
    /// <summary>
    /// 탐험 입력 한 프레임: 키보드·마우스를 합친다. 탐험 화면은 키 대신 이것만 읽는다.
    /// 키보드: A/D 이동 · Shift 달리기 · Ctrl 웅크리기 · Q/좌클릭 공격 · E/우클릭 상호작용 · Space 숨기 · W 살피기 · S 듣기 · F 손전등 · Tab 배낭 · 1~4 퀵슬롯.
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

        public void Read(Keyboard keyboard, Mouse mouse, Func<Mouse, bool> mouseFree)
        {
            var k = keyboard;
            var m = mouse;
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
        }
    }
}
