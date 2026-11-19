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
            Add(Loc.T("title.new_game"), () =>
            {
                _session.NewGame(Environment.TickCount);
                GameApp.GoTo(GameApp.HubScene);
            });

            var canContinue = _session.HasSave(SaveService.AutoSlot);
            Add(Loc.T("title.continue"), () => LoadAndEnter(SaveService.AutoSlot)).SetEnabled(canContinue);
            Add(Loc.T("title.load"), ShowLoad);
            Add(Loc.T("title.quit"), Quit);
        }

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
