using System.Collections;
using System.Linq;
using System.Threading;
using ApoGirl.Core.Events;
using ApoGirl.Exploration.Runtime;
using ApoGirl.Presentation.App;
using ApoGirl.Presentation.Dialogue;
using ApoGirl.Presentation.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace ApoGirl.Tests.PlayMode
{
    /// <summary>
    /// 디버그 "선택지 결과 미리 보기"의 화면 쪽: 선택지에 마우스를 올리면 판이 뜨는지, 미리 보기 글이 어떻게 적히는지.
    /// 켜고 끄는 설정(PlayerPrefs)은 건드리지 않는다 — 미리 보기 글을 대화 화면에 직접 넣어 본다.
    /// </summary>
    public sealed class ChoicePreviewViewTests
    {
        [UnityTest]
        public IEnumerator HoverOption_ShowsPreview_EvenOnDisabledChoice()
        {
            yield return BootToHub(31337);

            var hub = Resources.FindObjectsOfTypeAll<UIDocument>()
                .First(d => d.isActiveAndEnabled && d.rootVisualElement != null && d.rootVisualElement.Q("dialogue") != null);
            var host = DialogueTree();
            hub.rootVisualElement.Add(host);
            try
            {
                var view = new DialogueView(host);
                view.Open();
                var made = 0;
                var task = view.ShowOptions(new[]
                {
                    new DialogueChoice { OptionId = 0, Text = "첫째", Preview = () => { made++; return "미리 A"; } },
                    new DialogueChoice { OptionId = 1, Text = "둘째", Enabled = false, Preview = () => "미리 B" },
                    new DialogueChoice { OptionId = 2, Text = "셋째" },
                }, CancellationToken.None);
                yield return null;
                yield return null;

                var buttons = host.Q("dialogue-options").Children().ToList();
                Assert.AreEqual(3, buttons.Count);
                Assert.Greater(buttons[0].worldBound.height, 1f, "선택지가 배치되어야 함");
                Assert.AreEqual("", view.PreviewText, "마우스를 올리기 전에는 없다");

                // 자리로 직접
                view.HoverOptions(buttons[0].worldBound.center);
                Assert.AreEqual("미리 A", view.PreviewText);
                view.HoverOptions(buttons[0].worldBound.center);
                Assert.AreEqual(1, made, "글은 처음 한 번만 만든다");
                view.HoverOptions(buttons[1].worldBound.center);
                Assert.AreEqual("미리 B", view.PreviewText, "고를 수 없는 선택지도");
                view.HoverOptions(buttons[2].worldBound.center);
                Assert.AreEqual("", view.PreviewText, "미리 보기가 없는 선택지(대사 분기)는 숨긴다");

                // 진짜 마우스 이동 이벤트로 (비활성 단추 위에서도 선택지 묶음이 받는지)
                MovePointer(host, buttons[0].worldBound.center);
                yield return null;
                Assert.AreEqual("미리 A", view.PreviewText);
                MovePointer(host, buttons[1].worldBound.center);
                yield return null;
                Assert.AreEqual("미리 B", view.PreviewText, "비활성 선택지 위의 마우스 이동");

                view.Close();
                Assert.AreEqual("", view.PreviewText, "대화가 닫히면 판도 닫힌다");
                Assert.IsFalse(task.IsCompleted, "미리 보기는 선택을 대신하지 않는다");
            }
            finally
            {
                host.RemoveFromHierarchy();
            }
        }

        [UnityTest]
        public IEnumerator PreviewText_WritesSignedAmounts_DefinedValues_AndBranches()
        {
            yield return BootToHub(31338);
            Assert.AreNotEqual("preview.title", Loc.T("preview.title"), "문구 표에 미리 보기 문구가 있어야 함 (Sync Tables)");

            var preview = new ChoicePreviewResult { ChoiceId = "A", HasCheck = true, Chance = 0.62f, Failure = new PreviewBranch() };
            preview.Success.Entries.Add(new PreviewEntry { Kind = PreviewKind.Stat, Key = "Intellect", Numeric = true, Amount = 3, Defined = 3 });
            preview.Success.Entries.Add(new PreviewEntry { Kind = PreviewKind.Vital, Key = "Stress", Numeric = true, Amount = 4, Defined = 10 });
            preview.Success.Entries.Add(new PreviewEntry { Kind = PreviewKind.Resource, Key = "Food", Numeric = true, Amount = -2, Defined = -2 });
            preview.Success.Entries.Add(new PreviewEntry { Kind = PreviewKind.NpcAffinity, Key = "NPC_TEACHER", Numeric = true, Amount = 5, Defined = 5 });
            preview.Success.Entries.Add(new PreviewEntry { Kind = PreviewKind.FlagSet, Key = "FLAG_A" });
            preview.Success.Entries.Add(new PreviewEntry { Kind = PreviewKind.FlagSet, Key = "FLAG_OLD", Already = true });
            preview.Success.NextEventId = "EVT_NEXT_TEST";
            preview.Success.NextEntries.Add(new PreviewEntry { Kind = PreviewKind.Vital, Key = "Hp", Numeric = true, Amount = -5, Defined = -5 });

            var lines = ChoicePreviewText.Lines(preview);
            Assert.AreEqual(Loc.T("preview.title"), lines[0]);
            CollectionAssert.Contains(lines, Loc.F("preview.success", 62));
            CollectionAssert.Contains(lines, Loc.F("preview.failure", 38));
            CollectionAssert.Contains(lines, Loc.T("stat.Intellect") + " +3");
            CollectionAssert.Contains(lines, Loc.T("vital.Stress") + " +4 " + Loc.F("preview.defined", "+10"), "실제 값과 정의값이 다르면 같이");
            CollectionAssert.Contains(lines, Loc.T("res.Food") + " -2");
            CollectionAssert.Contains(lines, Loc.F("preview.affinity", Loc.T("npc.NPC_TEACHER") + " +5"), "호감은 수치로");
            CollectionAssert.Contains(lines, Loc.F("preview.flags", "+FLAG_A  +FLAG_OLD" + Loc.T("preview.already")));
            CollectionAssert.Contains(lines, Loc.F("preview.next", "EVT_NEXT_TEST"));
            CollectionAssert.Contains(lines, Loc.F("preview.next_show", Loc.T("vital.Hp") + " -5"));
            Assert.AreEqual(Loc.T("preview.none"), lines[lines.Count - 1], "빈 갈래(실패)는 '바뀌는 값 없음'");

            Assert.AreEqual("", ChoicePreviewText.Text(null), "미리 볼 것이 없으면 빈 글 (판을 띄우지 않는다)");
        }

        /// <summary>허브·탐험 화면과 같은 이름의 대화 요소 묶음 (DialogueView가 찾는 것).</summary>
        private static VisualElement DialogueTree()
        {
            var host = new VisualElement { name = "preview-test-host" };
            host.style.position = Position.Absolute;
            host.style.left = 0;
            host.style.top = 0;
            host.style.right = 0;
            host.style.bottom = 0;

            var layer = new VisualElement { name = "dialogue" };
            layer.AddToClassList("dialogue");
            layer.AddToClassList("hidden");
            var log = new ScrollView { name = "dialogue-log" };
            log.AddToClassList("dialogue-log");
            log.AddToClassList("hidden");
            layer.Add(log);
            layer.Add(new Button { name = "dialogue-log-button" });
            layer.Add(new Button { name = "dialogue-auto" });
            layer.Add(new Button { name = "dialogue-skip" });

            var box = new VisualElement { name = "dialogue-box" };
            box.AddToClassList("dialogue-box");
            box.Add(new Label { name = "dialogue-speaker" });
            box.Add(new Label { name = "dialogue-text" });
            var options = new VisualElement { name = "dialogue-options" };
            options.AddToClassList("dialogue-options");
            box.Add(options);
            box.Add(new Label { name = "dialogue-next" });
            layer.Add(box);
            host.Add(layer);
            return host;
        }

        private static void MovePointer(VisualElement element, Vector2 position)
        {
            var source = new Event { type = EventType.MouseMove, mousePosition = position };
            using (var moved = PointerMoveEvent.GetPooled(source))
            {
                element.panel.visualTree.SendEvent(moved);
            }
        }

        private static IEnumerator BootToHub(int seed)
        {
            SceneManager.LoadScene("Boot");
            yield return null;
            ExpeditionDirector.PlayLogEnabled = false;
            yield return WaitForScene(GameApp.TitleScene);
            Assert.IsNotNull(GameApp.Session, "Boot가 세션을 만들어야 함");

            ApoGirl.Presentation.Screens.HubScreen.AutoPlayOpening = false;
            GameApp.Session.NewGame(seed);
            GameApp.GoTo(GameApp.HubScene);
            yield return WaitForScene(GameApp.HubScene);
            yield return null;
        }

        private static IEnumerator WaitForScene(string name)
        {
            for (var i = 0; i < 300 && SceneManager.GetActiveScene().name != name; i++)
            {
                yield return null;
            }

            Assert.AreEqual(name, SceneManager.GetActiveScene().name);
        }
    }
}
