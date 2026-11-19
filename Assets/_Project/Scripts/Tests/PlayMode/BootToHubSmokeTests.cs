using System.Collections;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.State;
using ApoGirl.Exploration.Runtime;
using ApoGirl.Presentation.App;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace ApoGirl.Tests.PlayMode
{
    /// <summary>씬 흐름 스모크 테스트. 에러 로그가 나오면 실패한다.</summary>
    public sealed class BootToHubSmokeTests
    {
        [UnityTest]
        public IEnumerator BootTitleHub_RunOneWeek_NoErrors()
        {
            yield return BootToHub(12345);

            var session = GameApp.Session;
            session.SetSlot(0, "ACT_EDU_HUMANITIES");
            session.SetSlot(2, "ACT_EXP_DAY_SCHOOL");
            session.BeginWeek();
            while (session.IsExecuting)
            {
                session.Step();
                if (session.PendingExpedition != null)
                {
                    session.CompleteExpedition(new ExpeditionResult { Status = ExtractStatus.Extracted });
                }

                yield return null;
            }

            Assert.AreEqual(2, session.State.Calendar.Week);
            Assert.AreEqual(GameStatus.Playing, session.State.Status);
        }

        [UnityTest]
        public IEnumerator HubExpedition_LoadsSceneSearchesAndReturns()
        {
            yield return BootToHub(777);

            var session = GameApp.Session;
            Assert.IsTrue(session.UseRealtimeExpeditions);
            session.SetSlot(0, "ACT_EXP_DAY_SCHOOL");
            session.BeginWeek();
            var outcome = session.Step();
            Assert.IsTrue(outcome.AwaitingExpedition);

            ExpeditionResult result = null;
            var host = new GameObject("CoroutineHost").AddComponent<CoroutineHost>();
            host.StartCoroutine(GameApp.RunExpedition(outcome.ExpeditionRequest, r => result = r));

            ExpeditionDirector director = null;
            for (var i = 0; i < 300 && (director == null || director.Clock == null); i++)
            {
                director = Object.FindAnyObjectByType<ExpeditionDirector>();
                yield return null;
            }

            Assert.IsNotNull(director, "탐험 씬이 로드되어야 함");
            Assert.Greater(director.Targets.Count, 5, "건물에 계단·컨테이너·출구가 있어야 함");

            // 복도 → 교실 → 복도: 방에 들어가면 템플릿 컨테이너가 생기고, 나오면 같은 문 앞으로 돌아온다
            ExplorationTarget door = null;
            foreach (var target in director.Targets)
            {
                if (target.Kind == TargetKind.Door)
                {
                    door = target;
                    break;
                }
            }

            Assert.IsNotNull(door);
            var before = director.Targets.Count;
            director.UseTarget(door);
            Assert.IsTrue(director.InRoom);
            Assert.Greater(director.Targets.Count, before, "방 내부 컨테이너 생성");
            ExplorationTarget roomExit = null;
            foreach (var target in director.CurrentArea.Targets)
            {
                if (target.Kind == TargetKind.RoomExit)
                {
                    roomExit = target;
                }
            }

            director.UseTarget(roomExit);
            Assert.IsFalse(director.InRoom);
            Assert.AreEqual(door.X, director.X, 0.01f);
            Assert.AreEqual(door.Floor, director.Floor);

            // 휴면 군집 근처에 큰 소음을 내면 깨어난다
            Assert.Greater(director.Threat.Crowds.Count, 0, "낮 배치에 휴면 군집이 있어야 함");
            var crowd = director.Threat.Crowds[0];
            for (var i = 0; i < 20 && !crowd.Awake; i++)
            {
                director.DebugEmitNoise(crowd.AreaId, crowd.X, 7f);
                yield return null;
            }

            Assert.IsTrue(crowd.Awake, "소음으로 군집 각성");

            // 첫 컨테이너를 강제로 열어 모두 가져간 뒤 탈출
            foreach (var target in director.Targets)
            {
                if (target.Kind == TargetKind.Container && target.Contents.Count > 0)
                {
                    target.MarkSearched();
                    director.Open(target);
                    director.TakeAll();
                    break;
                }
            }

            director.ExtractNow();
            for (var i = 0; i < 300 && result == null; i++)
            {
                yield return null;
            }

            Assert.IsNotNull(result, "결과가 허브로 돌아와야 함");
            Assert.AreEqual(ExtractStatus.Extracted, result.Status);
            session.CompleteExpedition(result);
            Assert.AreEqual(1, session.State.Exploration.DayExplorations);
            Assert.IsFalse(SceneManager.GetSceneByName(GameApp.ExpeditionScene).isLoaded, "탐험 씬은 내려가야 함");
            Object.Destroy(host.gameObject);
        }

        private static IEnumerator BootToHub(int seed)
        {
            SceneManager.LoadScene("Boot");
            yield return WaitForScene(GameApp.TitleScene);
            Assert.IsNotNull(GameApp.Session, "Boot가 세션을 만들어야 함");

            var title = Object.FindAnyObjectByType<UIDocument>();
            Assert.IsNotNull(title.rootVisualElement.Q<Button>(className: "title-button"));

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

        private sealed class CoroutineHost : MonoBehaviour
        {
        }
    }
}
