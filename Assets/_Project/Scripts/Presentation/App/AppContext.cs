using System.IO;
using ApoGirl.Data;
using ApoGirl.Systems.Save;
using ApoGirl.Systems.Session;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ApoGirl.Presentation.App
{
    /// <summary>앱 전역 서비스 보관소 (가벼운 서비스 로케이터). 게임 세션 하나만 둔다.</summary>
    public static class AppContext
    {
        public const string TitleScene = "Title";
        public const string HubScene = "Hub";

        public static GameSession Session { get; private set; }

        public static void Initialize(DefinitionDatabaseAsset database)
        {
            if (Session != null)
            {
                return;
            }

            var saveDirectory = Path.Combine(Application.persistentDataPath, "Saves");
            Session = new GameSession(database.Build(), new SaveService(saveDirectory, new GameStateSerializer()));
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
        }
    }
}
