using System;
using ApoGirl.Core.Expedition;
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

        public DebugPanelView(VisualElement panel, GameSession session)
        {
            _panel = panel;
            _session = session;

            var title = new Label(Loc.T("debug.title"));
            title.AddToClassList("section-header");
            _panel.Add(title);

            Add("debug.resources", () =>
            {
                foreach (var type in new[] { ResourceType.Food, ResourceType.Water, ResourceType.Medicine, ResourceType.Materials, ResourceType.Power })
                {
                    _session.DebugChangeResource(type, 10);
                }
            });
            Add("debug.stats", () => _session.DebugChangeAllStats(10));
            Add("debug.debt_up", () => _session.DebugChangeSleepDebt(1));
            Add("debug.debt_down", () => _session.DebugChangeSleepDebt(-1));
            Add("debug.hp_down", () => _session.DebugChangeVital(VitalType.Hp, -20));
            Add("debug.stress_up", () => _session.DebugChangeVital(VitalType.Stress, 20));
            Add("debug.power", () => _session.DebugTogglePowerRestored());
            Add("debug.week_next", () => _session.DebugJumpToWeek(_session.State.Calendar.Week + 1));
            Add("debug.week_last", () => _session.DebugJumpToWeek(51));
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
            _status.text = "seed " + _session.State.Rng.Seed + " · next expedition: " + (forced?.ToString() ?? "-")
                           + " · planning " + _session.IsPlanning;
        }

        private void Add(string key, Action onClick)
        {
            var button = new Button(onClick) { text = Loc.T(key) };
            button.AddToClassList("small-button");
            _panel.Add(button);
        }
    }
}
