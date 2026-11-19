using System.Collections;
using System.Linq;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.State;
using ApoGirl.Exploration.Runtime;
using ApoGirl.Presentation.App;
using ApoGirl.Presentation.Dialogue;
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

                ResolveAll(session);

                yield return null;
            }

            Assert.IsTrue(session.State.Events.Records.ContainsKey("EVT_NIGHT_TEACHER_TALK_01"), "1주차 밤 휴식 대화");

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
                if (target.Kind == TargetKind.RoomExit && Mathf.Abs(target.X - director.X) < 0.01f)
                {
                    roomExit = target; // 들어온 그 문
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

        [UnityTest]
        public IEnumerator NightExpedition_IsDark_HasAwakeZombies_DawnAddsSleepDebt()
        {
            yield return BootToHub(4242);

            var session = GameApp.Session;
            session.SetSlot(1, "ACT_EXP_NIGHT_SCHOOL");
            session.BeginWeek();
            session.Step(); // 1일차 낮
            ResolveAll(session);
            var outcome = session.Step();
            Assert.IsTrue(outcome.AwaitingExpedition);
            Assert.AreEqual(ExpeditionTime.Night, outcome.ExpeditionRequest.TimeOfDay);

            ExpeditionResult result = null;
            var host = new GameObject("CoroutineHost").AddComponent<CoroutineHost>();
            host.StartCoroutine(GameApp.RunExpedition(outcome.ExpeditionRequest, r => result = r));

            ExpeditionDirector director = null;
            for (var i = 0; i < 300 && (director == null || director.Clock == null); i++)
            {
                director = Object.FindAnyObjectByType<ExpeditionDirector>();
                yield return null;
            }

            Assert.IsNotNull(director);
            Assert.IsTrue(director.IsDark);
            Assert.Less(director.Visibility, 1f, "손전등 없이 어둠 속에서는 덜 보인다");
            var awake = 0;
            foreach (var zombie in director.Threat.Zombies)
            {
                if (zombie.IsAwake)
                {
                    awake++;
                }
            }

            Assert.Greater(awake, 5, "밤에는 깨어 있는 좀비가 많다");

            // 새벽까지 버틴 뒤 귀환
            director.Clock.Tick(director.Clock.Length);
            yield return null;
            yield return null;
            Assert.IsTrue(director.DawnReturn);
            director.ExtractNow();
            for (var i = 0; i < 300 && result == null; i++)
            {
                yield return null;
            }

            Assert.AreEqual(1, result.SleepDebtDelta, "새벽 귀환: 수면 부채 +1");
            var debtBefore = session.State.Player.SleepDebt;
            session.CompleteExpedition(result);
            Assert.AreEqual(debtBefore + 2, session.State.Player.SleepDebt, "밤 탐험 +1, 새벽 귀환 +1");
            Object.Destroy(host.gameObject);
        }

        /// <summary>M3: 허브에서 Yarn 대사가 문자열 테이블로 나오고, #choice 선택이 이벤트 데이터에 반영된다.</summary>
        [UnityTest]
        public IEnumerator HubDialogue_RunsYarnNode_AndAppliesChoice()
        {
            yield return BootToHub(31);

            var session = GameApp.Session;
            var root = Object.FindAnyObjectByType<ApoGirl.Presentation.Screens.HubScreen>().GetComponent<UIDocument>().rootVisualElement;
            var project = Resources.Load<Yarn.Unity.YarnProject>(ApoGirl.Presentation.Screens.HubScreen.YarnProjectResource);
            Assert.IsNotNull(project, "Resources의 Yarn 프로젝트");
            Assert.IsNotNull(project.Program, "Yarn 대본 컴파일");

            var view = new DialogueView(root);
            var player = new EventDialoguePlayer(project, view);
            session.DebugForceEvent("EVT_NIGHT_TEACHER_TALK_01");
            Assert.IsTrue(session.HasPendingEvent);

            string picked = null;
            var task = player.Run(session.PendingEvent.Node, session.PendingChoices, id =>
            {
                picked = id;
                session.ResolveEvent(id);
            }, System.Threading.CancellationToken.None);

            var options = root.Q("dialogue-options");
            var text = root.Q<Label>("dialogue-text");
            var sawJosa = false;
            var seen = new System.Collections.Generic.List<string>();
            for (var frame = 0; frame < 2000 && !task.IsCompleted; frame++)
            {
                sawJosa |= text.text.Contains("한서진이 깬 걸");
                if (seen.Count == 0 || seen[seen.Count - 1] != text.text) seen.Add(text.text);
                if (options.childCount > 0)
                {
                    var buttons = options.Query<Button>().ToList();
                    Assert.AreEqual(2, buttons.Count, "선택지 2개");
                    using (var submit = NavigationSubmitEvent.GetPooled())
                    {
                        submit.target = buttons[0];
                        buttons[0].SendEvent(submit);
                    }
                }
                else
                {
                    view.Advance();
                }

                yield return null;
            }

            Assert.IsTrue(task.IsCompleted, "대화가 끝나야 함");
            Assert.IsFalse(task.IsFaulted, task.Exception?.ToString());
            Assert.IsTrue(sawJosa, "조사 자동 처리 (한서진 + 이/가): " + string.Join(" / ", seen));
            Assert.AreEqual("ASK", picked);
            Assert.IsFalse(session.HasPendingEvent);
            Assert.IsTrue(session.State.Flags.Contains("ASKED_TEACHER_LEG"));
            Assert.AreEqual(3, session.State.Teacher.Affinity);
        }

        private static IEnumerator BootToHub(int seed)
        {
            SceneManager.LoadScene("Boot");
            yield return null;
            ExpeditionDirector.PlayLogEnabled = false;
            yield return WaitForScene(GameApp.TitleScene);
            Assert.IsNotNull(GameApp.Session, "Boot가 세션을 만들어야 함");

            var title = Object.FindAnyObjectByType<UIDocument>();
            Assert.IsNotNull(title.rootVisualElement.Q<Button>(className: "title-button"));

            ApoGirl.Presentation.Screens.HubScreen.AutoPlayOpening = false;
            GameApp.Session.NewGame(seed);
            GameApp.GoTo(GameApp.HubScene);
            yield return WaitForScene(GameApp.HubScene);
            yield return null;

            // 시작 이벤트는 바로 넘기고, 1주차 튜토리얼 고정 슬롯은 풀어 둔다 (각 테스트가 직접 계획을 짠다)
            var session = GameApp.Session;
            if (session.HasPendingOpening)
            {
                session.StartOpening();
                ResolveAll(session);
            }

            session.State.Plan.Slots[0].Locked = false;
            session.State.Plan.Slots[0].ActionId = "";
            session.State.Flags.Add("LOC_SCHOOL_DAY");
            session.State.Flags.Add("NIGHT_RUNS");
        }

        private static void ResolveAll(ApoGirl.Systems.Session.GameSession session)
        {
            for (var guard = 0; guard < 20 && session.HasPendingEvent; guard++)
            {
                var choice = session.PendingChoices.FirstOrDefault(c => c.Enabled);
                session.ResolveEvent(choice?.Id ?? "");
            }
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
