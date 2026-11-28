using System;
using System.Collections;
using System.Reflection;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.State;
using ApoGirl.Exploration.Runtime;
using ApoGirl.Presentation.App;
using ApoGirl.Presentation.Art;
using ApoGirl.Presentation.Screens;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace ApoGirl.Tests.PlayMode
{
    /// <summary>
    /// 출정 준비 화면의 그림: 왼쪽 가는 곳 그림(art_map.json Locations — 튜토리얼은 새 그림이 올 때까지 교실동 그림을 빌린다)과
    /// 오른쪽 준비하는 자리 그림(PrepScenes — 거처 깃발·낮밤). 그림이 없는 지역은 조용히 빈 판으로 돌아간다.
    /// 세이브를 건드리지 않는다 (새 게임만 만들고 저장하지 않는다).
    /// </summary>
    public sealed class ExpeditionPrepArtTests
    {
        [Test]
        public void Locations_TutorialAndSchool_HaveArt_UnknownHasNone()
        {
            Assert.IsNotNull(StoryArt.Location("LOC_TUTORIAL"), "튜토리얼(창고 → 교실동): 새 그림이 없으면 Fallback 그림");
            Assert.IsNotNull(StoryArt.Location("LOC_SCHOOL_INTERIOR"), "교실동");
            Assert.IsNull(StoryArt.Location("LOC_NO_SUCH_PLACE"), "표에 없는 지역은 그림 없음");
        }

        [Test]
        public void PrepScene_FollowsTheShelter_AndTheTimeOfDay()
        {
            var storage = new GameState();
            var classroom = new GameState();
            classroom.Flags.Add("CLASSROOM_MOVED_IN");

            var storageDay = StoryArt.PrepScene(storage, false, out var dayIsNightArt, out var focus);
            Assert.IsNotNull(storageDay, "창고 거처 (기본 줄)");
            Assert.IsFalse(dayIsNightArt, "낮에는 밤 그림이 아니다");
            Assert.That(focus, Is.InRange(0f, 1f));

            var classroomDay = StoryArt.PrepScene(classroom, false, out _, out var classroomFocus);
            Assert.IsNotNull(classroomDay, "교실 거처");
            Assert.AreNotSame(storageDay, classroomDay, "거처가 다르면 그림도 다르다");
            Assert.That(classroomFocus, Is.InRange(0f, 1f));

            foreach (var state in new[] { storage, classroom })
            {
                var day = StoryArt.PrepScene(state, false, out _, out _);
                var night = StoryArt.PrepScene(state, true, out var nightArt, out _);
                Assert.IsNotNull(night, "밤 출정에도 그림이 있다");
                if (nightArt)
                {
                    Assert.AreNotSame(day, night, "밤 그림이 따로 있으면 그것");
                }
                else
                {
                    Assert.AreSame(day, night, "밤 그림이 없으면 낮 그림을 돌려주고 화면이 물들인다");
                }
            }

            Assert.IsNotNull(StoryArt.PrepScene(null, false, out _, out _), "상태가 없어도 기본 줄");
        }

        [UnityTest]
        public IEnumerator PrepView_ShowsArtOnBothSides_BehindTheControls()
        {
            SceneManager.LoadScene("Boot");
            yield return null;
            ExpeditionDirector.PlayLogEnabled = false;
            yield return WaitForScene(GameApp.TitleScene);
            HubScreen.AutoPlayOpening = false;
            GameApp.Session.NewGame(4711);
            GameApp.GoTo(GameApp.HubScene);
            yield return WaitForScene(GameApp.HubScene);
            yield return null;

            var hub = Object.FindAnyObjectByType<HubScreen>();
            var root = hub.GetComponent<UIDocument>().rootVisualElement;
            var prep = typeof(HubScreen).GetField("_prep", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(hub);
            Assert.IsNotNull(prep, "허브가 출정 준비 화면을 만들어야 함");
            var show = prep.GetType().GetMethod("Show");
            var hide = prep.GetType().GetMethod("Hide");

            var art = root.Q("exp-art");
            var side = root.Q("exp-side");
            var sideArt = root.Q("exp-side-art");
            var veil = root.Q("exp-side-veil");
            Assert.IsNotNull(sideArt, "Hub.uxml에 준비하는 자리 그림 요소");
            Assert.IsNotNull(veil, "Hub.uxml에 어둠 막 요소");

            // 1주차 튜토리얼: 창고 → 교실동
            show.Invoke(prep, new object[] { new ExpeditionRequest { LocationId = "LOC_TUTORIAL", TimeOfDay = ExpeditionTime.Day }, (Action)(() => { }), null });
            yield return null;
            yield return null;
            Assert.AreSame(StoryArt.Location("LOC_TUTORIAL"), art.style.backgroundImage.value.texture, "왼쪽: 학교 배경 그림");
            Assert.IsTrue(side.ClassListContains("has-art"), "오른쪽: 그림이 있으면 has-art");
            Assert.AreSame(StoryArt.PrepScene(GameApp.Session.State, false, out _, out _), sideArt.style.backgroundImage.value.texture, "오른쪽: 준비하는 자리 그림");
            Assert.AreEqual(DisplayStyle.Flex, sideArt.resolvedStyle.display, "그림이 보인다");
            Assert.IsNotNull(veil.style.backgroundImage.value.texture, "그림 위 어둠 막");
            Assert.IsFalse(sideArt.ClassListContains("tint-night"), "낮에는 물들이지 않는다");

            // 그림과 막은 입상·장비 칸 뒤에 깔리고 마우스를 받지 않는다
            Assert.Less(side.IndexOf(sideArt), side.IndexOf(root.Q("exp-standing")));
            Assert.Less(side.IndexOf(veil), side.IndexOf(root.Q("exp-panel")));
            Assert.AreEqual(PickingMode.Ignore, sideArt.pickingMode);
            Assert.AreEqual(PickingMode.Ignore, veil.pickingMode);
            Assert.Greater(root.Q("exp-loadout").childCount, 0, "장비 칸은 그대로");

            // 교실동 밤: 밤 그림이 따로 있으면 물들이지 않고, 없으면 낮 그림을 물들인다
            show.Invoke(prep, new object[] { new ExpeditionRequest { LocationId = "LOC_SCHOOL_INTERIOR", TimeOfDay = ExpeditionTime.Night }, (Action)(() => { }), null });
            yield return null;
            Assert.AreSame(StoryArt.Location("LOC_SCHOOL_INTERIOR"), art.style.backgroundImage.value.texture);
            StoryArt.PrepScene(GameApp.Session.State, true, out var nightArt, out _);
            Assert.AreEqual(!nightArt, sideArt.ClassListContains("tint-night"));

            // 그림 표에 없는 지역: 왼쪽은 예전처럼 빈 판 (오른쪽은 거처 그림이라 그대로)
            show.Invoke(prep, new object[] { new ExpeditionRequest { LocationId = "LOC_NO_SUCH_PLACE", TimeOfDay = ExpeditionTime.Day }, (Action)(() => { }), null });
            yield return null;
            Assert.AreEqual(StyleKeyword.Null, art.style.backgroundImage.keyword, "그림이 없으면 배경 그림을 지운다");

            hide.Invoke(prep, null);
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
