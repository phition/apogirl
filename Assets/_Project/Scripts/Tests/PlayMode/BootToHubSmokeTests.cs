using System.Collections;
using ApoGirl.Core.State;
using ApoGirl.Presentation.App;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace ApoGirl.Tests.PlayMode
{
    /// <summary>Boot → Title → Hub 로드 후 한 주를 실행해도 에러 로그가 없는지 (에러 로그가 나오면 테스트 실패).</summary>
    public sealed class BootToHubSmokeTests
    {
        [UnityTest]
        public IEnumerator BootTitleHub_RunOneWeek_NoErrors()
        {
            SceneManager.LoadScene("Boot");
            yield return WaitForScene(GameApp.TitleScene);
            Assert.IsNotNull(GameApp.Session, "Boot가 세션을 만들어야 함");

            var title = Object.FindAnyObjectByType<UIDocument>();
            Assert.IsNotNull(title.rootVisualElement.Q<Button>(className: "title-button"));

            GameApp.Session.NewGame(12345);
            GameApp.GoTo(GameApp.HubScene);
            yield return WaitForScene(GameApp.HubScene);
            yield return null;

            var session = GameApp.Session;
            session.SetSlot(0, "ACT_EDU_HUMANITIES");
            session.SetSlot(2, "ACT_EXP_DAY_SCHOOL");
            session.BeginWeek();
            while (session.IsExecuting)
            {
                session.Step();
                yield return null;
            }

            Assert.AreEqual(2, session.State.Calendar.Week);
            Assert.AreEqual(GameStatus.Playing, session.State.Status);
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
