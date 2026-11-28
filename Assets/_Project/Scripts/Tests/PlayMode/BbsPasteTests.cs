using System.Collections;
using ApoGirl.Presentation.Screens;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace ApoGirl.Tests.PlayMode
{
    /// <summary>PC통신 입력 칸은 붙여넣기를 받지 않는다 (사용자 요청 2026-11-28): 밖에서 써 둔 글을 통째로 넣지 못하게.</summary>
    public sealed class BbsPasteTests
    {
        [Test]
        public void IsPasteKey_CtrlV_CmdV_ShiftInsert()
        {
            Assert.IsTrue(BbsView.IsPasteKey(KeyCode.V, EventModifiers.Control));
            Assert.IsTrue(BbsView.IsPasteKey(KeyCode.V, EventModifiers.Command));
            Assert.IsTrue(BbsView.IsPasteKey(KeyCode.V, EventModifiers.Control | EventModifiers.Shift));
            Assert.IsTrue(BbsView.IsPasteKey(KeyCode.Insert, EventModifiers.Shift));
            Assert.IsFalse(BbsView.IsPasteKey(KeyCode.V, EventModifiers.None), "그냥 V는 글자다");
            Assert.IsFalse(BbsView.IsPasteKey(KeyCode.V, EventModifiers.Shift));
            Assert.IsFalse(BbsView.IsPasteKey(KeyCode.Insert, EventModifiers.None));
            Assert.IsFalse(BbsView.IsPasteKey(KeyCode.C, EventModifiers.Control), "복사는 막지 않는다");
        }

        /// <summary>
        /// 실제 입력 칸에서: 막지 않은 칸에는 붙고(이 확인이 있어야 아래가 뜻이 있다), 막은 칸에는 붙지 않는다.
        /// 클립보드는 끝나면 원래대로 돌려놓는다.
        /// </summary>
        [UnityTest]
        public IEnumerator GuardedField_IgnoresPaste_WhileAPlainFieldAcceptsIt()
        {
            var clipboard = GUIUtility.systemCopyBuffer;
            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            var host = new GameObject("BbsPasteTest");
            try
            {
                var document = host.AddComponent<UIDocument>();
                document.panelSettings = settings;
                yield return null;
                var root = document.rootVisualElement;
                Assert.IsNotNull(root, "화면 뿌리");

                var plain = new TextField { maxLength = 60 };
                var guarded = new TextField { maxLength = 60 };
                var refused = 0;
                BbsView.GuardPaste(guarded, () => refused++);
                root.Add(plain);
                root.Add(guarded);
                yield return null;

                GUIUtility.systemCopyBuffer = "밖에서 써 온 글";

                // 붙여넣기 글쇠 Ctrl+V
                yield return Press(plain, KeyCode.V, EventModifiers.Control);
                Assert.AreEqual("밖에서 써 온 글", plain.value, "막지 않은 칸에는 붙는다");

                guarded.value = "직접 친 글";
                yield return Press(guarded, KeyCode.V, EventModifiers.Control);
                Assert.AreEqual("직접 친 글", guarded.value, "막은 칸에는 붙지 않는다");
                Assert.AreEqual(1, refused, "막았다고 알린다");

                // Shift+Insert는 이 Unity 버전(6000.6)의 실행 중 입력 칸에서는 붙여넣기가 아니다 — 그래도 막아 둔다 (버전이 바뀌어도 안전하게)
                yield return Press(guarded, KeyCode.Insert, EventModifiers.Shift);
                Assert.AreEqual("직접 친 글", guarded.value);
                Assert.AreEqual(2, refused);

                // 붙여넣기 명령 (글쇠가 아닌 길)
                guarded.value = "직접 친 글";
                var count = refused;
                yield return Command(guarded, "Paste");
                Assert.AreEqual("직접 친 글", guarded.value, "붙여넣기 명령도 받지 않는다");
                Assert.AreEqual(count + 1, refused);

                // 직접 치는 글은 그대로 들어간다
                guarded.value = "";
                yield return Press(guarded, KeyCode.None, EventModifiers.None, 'a');
                Assert.AreEqual("a", guarded.value, "붙여넣기가 아닌 글쇠는 막지 않는다");
                Assert.AreEqual(count + 1, refused);
            }
            finally
            {
                GUIUtility.systemCopyBuffer = clipboard;
                Object.Destroy(host);
                Object.Destroy(settings);
            }
        }

        /// <summary>글쇠를 받는 곳은 입력 칸 안쪽의 글 편집 요소다 (칸 겉에 보내면 아무 일도 없다).</summary>
        private static VisualElement Editor(TextField field)
        {
            var editor = field.Q<TextElement>();
            Assert.IsNotNull(editor, "입력 칸 안쪽의 글 편집 요소");
            return editor;
        }

        private static IEnumerator Press(TextField field, KeyCode key, EventModifiers modifiers, char character = '\0')
        {
            field.Focus();
            yield return null;
            var target = Editor(field);
            using (var down = KeyDownEvent.GetPooled(character, key, modifiers))
            {
                down.target = target;
                target.SendEvent(down);
            }

            yield return null;
        }

        private static IEnumerator Command(TextField field, string name)
        {
            field.Focus();
            yield return null;
            var target = Editor(field);
            using (var validate = ValidateCommandEvent.GetPooled(name))
            {
                validate.target = target;
                target.SendEvent(validate);
            }

            using (var execute = ExecuteCommandEvent.GetPooled(name))
            {
                execute.target = target;
                target.SendEvent(execute);
            }

            yield return null;
        }
    }
}
