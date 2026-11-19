using System;
using ApoGirl.Data;
using ApoGirl.Presentation.App;
using ApoGirl.Presentation.Text;
using ApoGirl.Systems.Save;
using ApoGirl.Systems.Session;
using UnityEngine;
using UnityEngine.UIElements;

namespace ApoGirl.Presentation.Screens
{
    /// <summary>타이틀: 새 게임 / 이어하기(자동 저장) / 불러오기(수동 3칸) / 종료.</summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class TitleScreen : MonoBehaviour
    {
        [Tooltip("Boot 씬을 거치지 않고 Title을 바로 실행할 때 쓸 정의 DB")]
        [SerializeField] private DefinitionDatabaseAsset _fallbackDatabase;

        private GameSession _session;
        private VisualElement _buttons;
        private Label _message;

        private void OnEnable()
        {
            _session = GameApp.EnsureSession(_fallbackDatabase);
            var root = GetComponent<UIDocument>().rootVisualElement;
            KoreanFont.Apply(root);
            root.Q<Label>("title-label").text = Loc.T("title.game");
            root.Q<Label>("subtitle-label").text = Loc.T("title.subtitle");
            _buttons = root.Q("title-buttons");
            _message = root.Q<Label>("title-message");
            ShowMain();
        }

        private void ShowMain()
        {
            _buttons.Clear();
            _message.text = "";
            Add(Loc.T("title.new_game"), () => ChooseBonus(() => StartCoroutine(RunPrologue())));
            Add(Loc.T("title.quick_start"), () => ChooseBonus(() =>
            {
                _session.NewGame(Environment.TickCount, null, _session.Definitions.Extras.Prologue.Common);
                GameApp.GoTo(GameApp.HubScene);
            }));

            var canContinue = _session.HasSave(SaveService.AutoSlot);
            Add(Loc.T("title.continue"), () => LoadAndEnter(SaveService.AutoSlot)).SetEnabled(canContinue);
            Add(Loc.T("title.load"), ShowLoad);
            Add(Loc.T("title.gallery"), ShowGallery);
            Add(Loc.T("title.quit"), Quit);
        }

        /// <summary>0막 프롤로그 → 새 게임 → 허브 (Yarn 프로젝트가 없으면 빠른 시작).</summary>
        private System.Collections.IEnumerator RunPrologue()
        {
            var project = Resources.Load<Yarn.Unity.YarnProject>(HubScreen.YarnProjectResource);
            if (project == null || project.Program == null || !project.Program.Nodes.ContainsKey(PrologueFlow.StartNode))
            {
                Debug.LogWarning("프롤로그 대본이 없어 빠른 시작으로 진행");
                _session.NewGame(Environment.TickCount);
                GameApp.GoTo(GameApp.HubScene);
                yield break;
            }

            _buttons.AddToClassList("hidden");
            var root = GetComponent<UIDocument>().rootVisualElement;
            var flow = new PrologueFlow(root, _session, project);
            _advance = flow.Advance;
            yield return flow.Run(destroyCancellationToken);
            _advance = null;
            GameApp.GoTo(GameApp.HubScene);
        }

        private void Update()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
            {
                var root = GetComponent<UIDocument>().rootVisualElement;
                if (root.Q("prologue-input") is VisualElement input && input.ClassListContains("hidden"))
                {
                    _advance?.Invoke();
                }
            }
        }

        private System.Action _advance;

        private void ShowLoad()
        {
            _buttons.Clear();
            var any = false;
            for (var i = 1; i <= SaveService.ManualSlotCount; i++)
            {
                var slot = SaveService.ManualSlot(i);
                var exists = _session.HasSave(slot);
                any |= exists;
                Add(Loc.F("title.load_slot", i), () => LoadAndEnter(slot)).SetEnabled(exists);
            }

            _message.text = any ? "" : Loc.T("title.no_saves");
            Add(Loc.T("common.close"), ShowMain);
        }

        /// <summary>회차 요소: 엔딩을 하나라도 봤으면 시작 보너스를 하나 고른 뒤 시작한다.</summary>
        private void ChooseBonus(Action start)
        {
            var bonuses = _session.Definitions.Extras.StartBonuses;
            _session.PendingStartBonus = null;
            if (!_session.IsNewGamePlus || bonuses.Count == 0)
            {
                start();
                return;
            }

            _buttons.Clear();
            _message.text = Loc.T("title.bonus_pick");
            foreach (var bonus in bonuses)
            {
                var picked = bonus;
                var button = Add(Loc.T("bonus." + bonus.Id + ".name"), () =>
                {
                    _session.PendingStartBonus = picked;
                    start();
                });
                button.tooltip = Loc.T("bonus." + bonus.Id + ".desc");
            }

            Add(Loc.T("title.bonus_none"), start);
            Add(Loc.T("common.close"), ShowMain);
        }

        private void ShowGallery()
        {
            _buttons.AddToClassList("hidden");
            var root = GetComponent<UIDocument>().rootVisualElement;
            new GalleryView(root, _session.Definitions, _session.Gallery, () => _buttons.RemoveFromClassList("hidden"));
        }

        private void LoadAndEnter(string slot)
        {
            try
            {
                _session.Load(slot);
                GameApp.GoTo(GameApp.HubScene);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("세이브 불러오기 실패: " + exception.Message);
                _message.text = exception.Message;
            }
        }

        private Button Add(string text, Action onClick)
        {
            var button = new Button(onClick) { text = text };
            button.AddToClassList("title-button");
            _buttons.Add(button);
            return button;
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
