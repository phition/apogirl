using System;
using System.Linq;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using ApoGirl.Presentation.Text;
using ApoGirl.Systems.Session;
using UnityEngine.UIElements;

namespace ApoGirl.Presentation.Screens
{
    /// <summary>디버그 패널 (F1, 문서 11.4 치트 콘솔의 초기판).</summary>
    internal sealed class DebugPanelView
    {
        private readonly VisualElement _panel;
        private readonly GameSession _session;
        private readonly Label _status;

        public DebugPanelView(VisualElement panel, GameSession session, Action<string> forceEvent = null, Action showEnd = null, Action openBalanceLab = null)
        {
            _panel = panel;
            _session = session;

            var title = new Label(Loc.T("debug.title"));
            title.AddToClassList("section-header");
            _panel.Add(title);

            // 밸런스 표 (Docs/BALANCE_LAB_PROPOSAL.md)
            if (openBalanceLab != null)
            {
                Add("debug.balance_lab", openBalanceLab);
            }

            Add("debug.resources", () =>
            {
                foreach (var type in new[] { ResourceType.Food, ResourceType.Water, ResourceType.Medicine, ResourceType.Materials, ResourceType.Battery })
                {
                    _session.DebugChangeResource(type, 10);
                }
            });
            Add("debug.stats", () => _session.DebugChangeAllStats(10));
            Add("debug.restore_vitals", () => _session.DebugRestoreVitals());
            Add("debug.debt_up", () => _session.DebugChangeSleepDebt(1));
            Add("debug.debt_down", () => _session.DebugChangeSleepDebt(-1));
            Add("debug.hp_down", () => _session.DebugChangeVital(VitalType.Hp, -20));
            Add("debug.stress_up", () => _session.DebugChangeVital(VitalType.Stress, 20));
            Add("debug.power", () => _session.DebugTogglePowerRestored());
            Add("debug.week_next", () => _session.DebugJumpToWeek(_session.State.Calendar.Week + 1));
            Add("debug.week_last", () => _session.DebugJumpToWeek(51));
            Add("debug.week_52", () => _session.DebugJumpToWeek(52));

            // M6: 감염·병세·날씨·구조 공지
            Add("debug.infect", () => _session.DebugInfect(25));
            Add("debug.teacher_reinfect", () => _session.DebugTeacherInfection(25));
            Add("debug.weather", () => _session.DebugCycleWeather());
            Add("debug.rescue_flag", () => _session.DebugToggleFlag("RESCUE_ANNOUNCED"));

            // M5: 엔딩 확인 (계획 단계에서 지금 상태로 판을 끝낸다)
            Add("debug.end_rescue", () =>
            {
                _session.DebugEndGame(GameStatus.Rescued);
                if (_session.IsEnded) showEnd?.Invoke();
            });
            var causes = new System.Collections.Generic.List<string>
            {
                WeekRunner.EndCauseExpedition, WeekRunner.EndCauseRaid, WeekRunner.EndCauseStarvation, WeekRunner.EndCauseCollapse, WeekRunner.EndCauseInfection, WeekRunner.EndCauseHpZero,
            };
            var causePicker = new DropdownField(Loc.T("debug.end_bad"), causes, 0);
            causePicker.AddToClassList("debug-dropdown");
            _panel.Add(causePicker);
            Add("debug.end_bad", () =>
            {
                _session.DebugEndGame(GameStatus.GameOver, causePicker.value);
                if (_session.IsEnded) showEnd?.Invoke();
            });
            Add("debug.force_down", () =>
            {
                _session.DebugExpedition.ForceNext = ExtractStatus.Down;
                Refresh();
            });
            Add("debug.force_ok", () =>
            {
                _session.DebugExpedition.ForceNext = ExtractStatus.Extracted;
                Refresh();
            });

            Add("debug.force_hostile", () =>
            {
                var expedition = _session.DebugExpedition;
                expedition.ForceHostile = (expedition.ForceHostile + 1) % 4;
                Refresh();
            });

            Add("debug.expedition_mode", () =>
            {
                _session.UseRealtimeExpeditions = !_session.UseRealtimeExpeditions;
                Refresh();
            });

            Add("debug.ui_all", () =>
            {
                var flags = _session.State.Flags;
                if (!flags.Remove("UI_ALL")) flags.Add("UI_ALL");
                Refresh();
            });
            Add("debug.unlock_all", () => _session.DebugUnlockAll());

            // 장비 주기: 장비 칸이 있는 아이템 (가방·무기·도구·방어구·소모품)
            var gear = _session.Definitions.Items.Where(i => i.Slot != ApoGirl.Core.Definitions.EquipSlot.None)
                .OrderBy(i => i.Slot).ThenBy(i => i.Id).ToList();
            if (gear.Count > 0)
            {
                var labels = gear.Select(i => Loc.Item(i.Id) + " (" + i.Id + ")").ToList();
                var gearPicker = new DropdownField(Loc.T("debug.give_item"), labels, 0);
                gearPicker.AddToClassList("debug-dropdown");
                _panel.Add(gearPicker);
                Add("debug.give_item", () => _session.DebugGiveItem(gear[Math.Max(0, gearPicker.index)].Id));
                Add("debug.give_basic_gear", () =>
                {
                    foreach (var id in new[] { "ITEM_BAG_HIKING", "ITEM_WEAPON_PIPE", "ITEM_TOOL_FLASHLIGHT", "ITEM_ARMOR_PADDING", "ITEM_BANDAGE", "ITEM_SNACK" })
                    {
                        _session.DebugGiveItem(id);
                    }
                });
            }

            Add("debug.events_interactive", () =>
            {
                _session.InteractiveEvents = !_session.InteractiveEvents;
                Refresh();
            });

            // 이벤트 강제 발생 (계획 단계에서만)
            var eventIds = _session.Definitions.Events.Select(e => e.Id).ToList();
            if (eventIds.Count > 0 && forceEvent != null)
            {
                var picker = new DropdownField(Loc.T("debug.event"), eventIds, 0);
                picker.AddToClassList("debug-dropdown");
                _panel.Add(picker);
                Add("debug.force_event", () => forceEvent(picker.value));
            }

            _status = new Label();
            _status.AddToClassList("debug-status");
            _panel.Add(_status);
        }

        public void Toggle()
        {
            _panel.ToggleInClassList("hidden");
            Refresh();
        }

        public void Refresh()
        {
            var forced = _session.DebugExpedition.ForceNext;
            var hostile = _session.DebugExpedition.ForceHostile;
            _status.text = "seed " + _session.State.Rng.Seed + " · next expedition: " + (forced?.ToString() ?? "-")
                           + " · survivor " + (hostile > 0 ? "T" + hostile : "-")
                           + " · trauma " + _session.State.Player.AssaultTrauma
                           + " · looted " + _session.State.Exploration.LootedContainers.Count
                           + " · planning " + _session.IsPlanning
                           + " · expedition " + (_session.UseRealtimeExpeditions ? "realtime" : "headless")
                           + " · events " + (_session.InteractiveEvents ? "interactive" : "auto")
                           + " · seen " + _session.State.Events.Records.Count
                           + " · ending " + Preview()
                           + " · infection " + _session.State.Player.Vitals.Infection
                           + " · teacher " + (_session.State.Teacher.Reinfected ? _session.State.Teacher.InfectionProgress.ToString() : "-");
        }

        /// <summary>지금 상태로 구조된다면 받을 엔딩 (판정 미리보기).</summary>
        private string Preview()
        {
            var state = _session.State;
            var status = state.Status;
            if (status == GameStatus.Playing)
            {
                state.Status = GameStatus.Rescued;
            }

            try
            {
                var result = ApoGirl.Core.Endings.EndingResolver.Resolve(state, _session.Definitions);
                return result.Ending == null ? "-" : result.Ending.Id + " (" + result.Score.ToString("0") + ")";
            }
            finally
            {
                state.Status = status;
            }
        }

        private void Add(string key, Action onClick)
        {
            var button = new Button(onClick) { text = Loc.T(key) };
            button.AddToClassList("small-button");
            _panel.Add(button);
        }
    }
}
