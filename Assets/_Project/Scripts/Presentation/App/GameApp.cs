using System;
using System.Collections;
using System.IO;
using ApoGirl.Core.Expedition;
using ApoGirl.Data;
using ApoGirl.Systems.Save;
using ApoGirl.Systems.Session;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ApoGirl.Presentation.App
{
    /// <summary>앱 전역 서비스 보관소 (가벼운 서비스 로케이터). 게임 세션 하나만 둔다.</summary>
    public static class GameApp
    {
        public const string TitleScene = "Title";
        public const string HubScene = "Hub";
        public const string ExpeditionScene = "Expedition";

        public static GameSession Session { get; private set; }

        /// <summary>진행 중인 실시간 탐험 요청 (탐험 씬이 읽는다).</summary>
        public static ExpeditionRequest ActiveExpedition { get; private set; }

        /// <summary>탐험 씬이 보고한 결과. 허브가 받아서 세션에 넘긴다.</summary>
        public static ExpeditionResult ExpeditionResult { get; private set; }

        /// <summary>탐험 씬을 추가 로드하고, 결과가 보고되면 씬을 내린 뒤 콜백한다 (문서 11.2.1).</summary>
        public static IEnumerator RunExpedition(ExpeditionRequest request, Action<ExpeditionResult> onFinished)
        {
            ActiveExpedition = request;
            ExpeditionResult = null;
            yield return SceneManager.LoadSceneAsync(ExpeditionScene, LoadSceneMode.Additive);
            var scene = SceneManager.GetSceneByName(ExpeditionScene);
            SceneManager.SetActiveScene(scene);
            while (ExpeditionResult == null)
            {
                yield return null;
            }

            var result = ExpeditionResult;
            ActiveExpedition = null;
            ExpeditionResult = null;
            yield return SceneManager.UnloadSceneAsync(scene);
            onFinished(result);
        }

        /// <summary>탐험 씬에서 호출: 결과 보고.</summary>
        public static void ReportExpedition(ExpeditionResult result)
        {
            ExpeditionResult = result ?? throw new ArgumentNullException(nameof(result));
        }

        public static void Initialize(DefinitionDatabaseAsset database)
        {
            if (Session != null)
            {
                return;
            }

            var saveDirectory = Path.Combine(Application.persistentDataPath, "Saves");
            var gallery = new EndingGallery(Application.persistentDataPath);
            Session = new GameSession(database.Build(), new SaveService(saveDirectory, new GameStateSerializer()), null, gallery)
            {
                UseRealtimeExpeditions = true,
                InteractiveEvents = true,
            };
        }

        /// <summary>씬을 단독 실행했을 때(Boot를 거치지 않음)도 동작하게 한다.</summary>
        public static GameSession EnsureSession(DefinitionDatabaseAsset fallback)
        {
            if (Session == null && fallback != null)
            {
                Initialize(fallback);
            }

            return Session;
        }

        public static void GoTo(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }

        // 도메인 리로드를 끈 상태에서도 플레이마다 초기화
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Session = null;
            ActiveExpedition = null;
            ExpeditionResult = null;
        }
    }
}
