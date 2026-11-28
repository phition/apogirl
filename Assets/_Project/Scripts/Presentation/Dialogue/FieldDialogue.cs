using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ApoGirl.Core.Events;
using ApoGirl.Presentation.Art;
using ApoGirl.Presentation.Audio;
using ApoGirl.Presentation.Screens;
using UnityEngine;
using UnityEngine.UIElements;

namespace ApoGirl.Presentation.Dialogue
{
    /// <summary>
    /// 탐험 화면 위 대화 (동료가 될 인물과 그 자리에서): 탐험 HUD 문서의 대화 요소(dialogue-*)에 허브와 같은 대화 창을 띄우고
    /// 이벤트 컷을 깐다. 선택은 세션에 바로 반영하지 않고 고른 ID만 돌려준다 (탐험 결과에 담겨 귀환 때 적용).
    /// </summary>
    public sealed class FieldDialogue
    {
        private readonly DialogueView _view;
        private readonly EventDialoguePlayer _player;

        public FieldDialogue(VisualElement root)
        {
            if (root.Q("dialogue") == null)
            {
                return;
            }

            _view = new DialogueView(root);
            var project = Resources.Load<Yarn.Unity.YarnProject>(HubScreen.YarnProjectResource);
            if (project != null && project.Program != null)
            {
                _player = new EventDialoguePlayer(project, _view);
            }
        }

        public bool IsOpen => _view != null && _view.IsOpen;

        public bool HasNode(string node) => _player != null && !string.IsNullOrEmpty(node) && _player.HasNode(node);

        /// <summary>Space·Enter·클릭: 타이핑 중이면 완성, 아니면 다음 줄.</summary>
        public void Advance() => _view?.Advance();

        /// <summary>
        /// 노드를 끝까지 보여 준다. choices가 있으면 그 이벤트의 선택지만(조건에 안 맞는 것은 숨김), null이면 노드의 선택지 그대로.
        /// 고른 선택지 ID는 onChoice로 (한 번만). preview: 디버그 "선택지 결과 미리 보기" (선택지 ID → 글, 끄면 null).
        /// </summary>
        public Task Run(string node, string sceneEventId, IReadOnlyList<EventChoiceView> choices, Action<string> onChoice, CancellationToken token,
            Func<string, string> preview = null)
        {
            if (!HasNode(node))
            {
                return Task.CompletedTask;
            }

            EventDialoguePlayer.LastChoiceResult = null;
            _view.SetScene(StoryArt.EventImage(sceneEventId));
            if (!string.IsNullOrEmpty(sceneEventId))
            {
                Sound.OnEvent(sceneEventId);
            }

            var picked = false;
            _player.ChoicePreview = preview;
            return _player.Run(node, choices, id =>
            {
                if (!picked)
                {
                    picked = true;
                    onChoice?.Invoke(id);
                }
            }, token);
        }
    }
}
