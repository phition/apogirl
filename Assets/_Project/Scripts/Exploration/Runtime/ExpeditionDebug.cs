using System.Linq;
using ApoGirl.Core.State;
using ApoGirl.Presentation.App;
using ApoGirl.Presentation.Screens;
using UnityEngine;
using UnityEngine.UIElements;

namespace ApoGirl.Exploration.Runtime
{
    /// <summary>
    /// 디버그·화면 확인용: 허브 씬에서 원하는 지역·장비로 탐험을 바로 연다 (출정 준비 화면을 거치지 않는다).
    /// 에디터의 Unity MCP(RunCommand)나 테스트에서 부른다. 게임 진행에는 쓰지 않는다.
    /// </summary>
    public static class ExpeditionDebug
    {
        /// <summary>
        /// 새 판을 만들어 그 행동(예: ACT_EXP_DAY_MART)의 탐험을 바로 시작한다. 이미 탐험 중이면 먼저 끝내고 false (한 번 더 부른다).
        /// slot: 0 = 1일 낮, 1 = 1일 밤.
        /// </summary>
        public static bool Start(string actionId, int slot = 0, string weapon = "", string armor = "", string bag = "", string tool = "", string tool2 = "")
        {
            var running = Object.FindAnyObjectByType<ExpeditionDirector>();
            if (running != null)
            {
                running.ExtractNow();
                return false;
            }

            var hub = Object.FindAnyObjectByType<HubScreen>();
            if (hub == null)
            {
                Debug.LogWarning("ExpeditionDebug: 허브 씬에서 불러야 한다");
                return false;
            }

            HubScreen.AutoPlayOpening = false;
            ExpeditionDirector.PlayLogEnabled = false;
            var session = GameApp.Session;
            session.NewGame(12345);
            if (session.HasPendingOpening)
            {
                session.StartOpening();
                ResolveAll();
            }

            session.DebugForceEvent(session.Definitions.Balance.OpeningEventId);
            ResolveAll();
            foreach (var planSlot in session.State.Plan.Slots)
            {
                planSlot.Locked = false;
                planSlot.ForcedRest = false;
                planSlot.ActionId = "";
            }

            foreach (var flag in new[] { "LOC_SCHOOL_DAY", "NIGHT_RUNS", "LOC_STORE", "LOC_PHARMACY", "LOC_RESIDENTIAL", "LOC_SUBWAY", "LOC_MART", "LOC_POLICE", "LOC_WATERWORKS", "UI_ALL" })
            {
                session.State.Flags.Add(flag);
            }

            foreach (var id in new[] { weapon, armor, bag, tool, tool2 })
            {
                if (!string.IsNullOrEmpty(id))
                {
                    session.State.Storage.Add(new ItemStack { ItemId = id, Count = 1 });
                }
            }

            session.State.Loadout.MainWeapon = weapon ?? "";
            session.State.Loadout.Armor = armor ?? "";
            session.State.Loadout.Backpack = bag ?? "";
            session.State.Loadout.Tool = tool ?? "";
            session.State.Loadout.Tool2 = tool2 ?? "";
            session.SetSlot(slot, actionId);
            session.BeginWeek();
            var outcome = session.Step();
            for (var guard = 0; guard < 6 && !outcome.AwaitingExpedition && session.IsExecuting; guard++)
            {
                ResolveAll();
                outcome = session.Step();
            }

            if (!outcome.AwaitingExpedition)
            {
                Debug.LogWarning("ExpeditionDebug: 탐험이 시작되지 않았다 (" + actionId + ")");
                return false;
            }

            hub.GetComponent<UIDocument>().rootVisualElement.style.display = DisplayStyle.None;
            hub.SendMessage("SetPanelRendererEnabled", false);
            hub.StartCoroutine(GameApp.RunExpedition(outcome.ExpeditionRequest, _ => { }));
            return true;
        }

        /// <summary>방 템플릿 ID에 그 글자가 들어 있는 첫 방으로 들어간다 (예: "NURSE"). 시계도 맞출 수 있다 (0~1, 음수면 그대로).</summary>
        public static string Room(string templateContains, float x = 5f, float clock = -1f)
        {
            var director = Object.FindAnyObjectByType<ExpeditionDirector>();
            if (director == null)
            {
                return "no expedition";
            }

            SetClock(clock);
            var door = director.Targets.FirstOrDefault(t => t.Kind == TargetKind.Door && t.Room != null && t.Room.Template != null && t.Room.Template.Id.Contains(templateContains));
            return door != null && director.DebugEnterRoom(door.Room.Door.RoomId, x) ? door.Room.Door.RoomId : "no room";
        }

        /// <summary>탐험 시계를 0~1 위치로 (낮: 해의 위치가 따라 바뀐다).</summary>
        public static void SetClock(float normalized)
        {
            var director = Object.FindAnyObjectByType<ExpeditionDirector>();
            if (director != null && normalized >= 0f && director.Clock.Normalized < normalized)
            {
                director.Clock.Tick(director.Clock.Length * normalized - director.Clock.Elapsed);
            }
        }

        private static void ResolveAll()
        {
            var session = GameApp.Session;
            for (var guard = 0; guard < 20 && session.HasPendingEvent; guard++)
            {
                var choice = session.PendingChoices.FirstOrDefault(c => c.Enabled);
                session.ResolveEvent(choice != null ? choice.Id : null);
            }
        }
    }
}
