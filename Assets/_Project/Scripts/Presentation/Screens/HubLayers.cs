using System;
using System.Collections.Generic;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;
using ApoGirl.Presentation.Art;
using ApoGirl.Presentation.Text;
using ApoGirl.Systems.Session;
using UnityEngine.UIElements;

namespace ApoGirl.Presentation.Screens
{
    // 허브의 겹 구조 (Docs/UI_REDESIGN_PROPOSAL.md): 2겹(서랍·계획 보드)을 여닫고, 1겹(도크·생명 수치 요약)을 그린다.

    /// <summary>2겹 여닫기: 왼쪽 서랍(생활기록부), 오른쪽 서랍(거점·사람), 계획 보드. 단추의 선택 표시도 맞춘다.</summary>
    internal sealed class HubLayers
    {
        private readonly VisualElement _left;
        private readonly VisualElement _right;
        private readonly VisualElement _people;
        private readonly VisualElement _board;
        private readonly Button _recordButton;
        private readonly Button _baseButton;
        private readonly Button _peopleButton;
        private readonly Button _planButton;
        private readonly VisualElement _screen;

        public HubLayers(VisualElement root)
        {
            _screen = root.Q("screen");
            _left = root.Q("drawer-left");
            _right = root.Q("drawer-right");
            _people = root.Q("drawer-people");
            _board = root.Q("board");
            _recordButton = root.Q<Button>("record-button");
            _baseButton = root.Q<Button>("base-button");
            _peopleButton = root.Q<Button>("people-button");
            _planButton = root.Q<Button>("plan-open-button");
            StoryArt.SetIcon(_recordButton, StoryArt.Icon("menu.record"), 22f);
            StoryArt.SetIcon(_baseButton, StoryArt.Icon("menu.school"), 22f);
            StoryArt.SetIcon(_peopleButton, StoryArt.Icon("menu.people"), 22f);
            StoryArt.SetIcon(_planButton, StoryArt.Icon("menu.plan"), 26f);

            root.Q<Label>("drawer-left-title").text = Loc.T("hud.record_title");
            root.Q<Label>("drawer-right-title").text = Loc.T("hud.base_title");
            root.Q<Label>("drawer-people-title").text = Loc.T("hud.people_title");
            root.Q<Label>("board-title").text = Loc.T("hud.board_title");
            root.Q<Label>("board-hint").text = Loc.T("hud.board_hint");
            foreach (var name in new[] { "drawer-left-close", "drawer-right-close", "drawer-people-close", "board-close" })
            {
                root.Q<Button>(name).text = Loc.T("hud.close");
            }

            root.Q<Button>("drawer-left-close").clicked += () => Set(_left, false);
            root.Q<Button>("drawer-right-close").clicked += () => Set(_right, false);
            root.Q<Button>("drawer-people-close").clicked += () => Set(_people, false);
            root.Q<Button>("board-close").clicked += () => Set(_board, false);
            _recordButton.text = Loc.T("hud.record");
            _baseButton.text = Loc.T("hud.base");
            _peopleButton.text = Loc.T("hud.people");
            _recordButton.clicked += ToggleRecord;
            _baseButton.clicked += ToggleBase;
            _peopleButton.clicked += TogglePeople;
            Sync();
        }

        public bool BoardOpen => IsOpen(_board);

        /// <summary>계획 보드를 열 수 있는가 (계획 단계에서만). 허브가 정한다.</summary>
        public Func<bool> CanPlan { get; set; } = () => true;

        public void ToggleBoard() => SetBoard(!BoardOpen);

        public void SetBoard(bool open)
        {
            Set(_board, open && CanPlan());
        }

        public void ToggleRecord() => Set(_left, !IsOpen(_left));

        // 오른쪽 서랍 둘(거점·인물)은 같은 자리라 하나만 연다
        public void ToggleBase()
        {
            var open = !IsOpen(_right);
            if (open) _people?.EnableInClassList("closed", true);
            Set(_right, open);
            if (open) BaseOpened?.Invoke();
        }

        /// <summary>거점 서랍을 열었을 때 (상황판이 알림을 본 것으로 친다).</summary>
        public Action BaseOpened { get; set; }

        public void CloseBase() => Set(_right, false);

        /// <summary>거점 서랍을 연다 (이미 열려 있으면 그대로).</summary>
        public void OpenBase()
        {
            if (!BaseOpen) ToggleBase();
        }

        public bool BaseOpen => IsOpen(_right);

        /// <summary>거점 단추 오른쪽 위의 빨간 점 (거점 상황판에 새 알림이 있을 때).</summary>
        public void SetBaseAlert(bool on)
        {
            if (_baseDot == null && _baseButton != null)
            {
                _baseDot = new VisualElement { pickingMode = PickingMode.Ignore };
                _baseDot.AddToClassList("hud-alert-dot");
                _baseButton.Add(_baseDot);
            }

            _baseDot?.EnableInClassList("hidden", !on);
        }

        private VisualElement _baseDot;

        public void TogglePeople()
        {
            var open = !IsOpen(_people);
            if (open) _right?.EnableInClassList("closed", true);
            Set(_people, open);
        }

        public bool PeopleOpen => IsOpen(_people);

        public void CloseAll()
        {
            Set(_board, false);
            Set(_left, false);
            Set(_right, false);
            Set(_people, false);
        }

        /// <summary>Esc: 맨 위에 열린 것 하나를 닫는다. 닫은 것이 있으면 true.</summary>
        public bool CloseTop()
        {
            foreach (var element in new[] { _board, _people, _right, _left })
            {
                if (IsOpen(element))
                {
                    Set(element, false);
                    return true;
                }
            }

            return false;
        }

        private static bool IsOpen(VisualElement element) => element != null && !element.ClassListContains("closed");

        private void Set(VisualElement element, bool open)
        {
            element?.EnableInClassList("closed", !open);
            if (element == _right)
            {
                // 보관함 덧판은 거점 서랍에 붙어 있다 (펼쳐 둔 채 서랍을 다시 열면 그 자리에서 다시 그린다)
                Opened?.Invoke();
            }

            Sync();
        }

        /// <summary>거점 서랍을 열고 닫았을 때 (보관함 덧판을 그 자리에서 다시 그린다).</summary>
        public Action Opened { get; set; }

        private void Sync()
        {
            _recordButton?.EnableInClassList("selected", IsOpen(_left));
            _baseButton?.EnableInClassList("selected", IsOpen(_right));
            _peopleButton?.EnableInClassList("selected", IsOpen(_people));
            _planButton?.EnableInClassList("selected", IsOpen(_board));
            _screen?.EnableInClassList("board-open", IsOpen(_board));
            // 오른쪽 서랍(거점·인물)이 열리면 선생님 한 줄을 서랍 왼쪽으로 비켜 둔다
            _screen?.EnableInClassList("right-drawer-open", IsOpen(_right) || IsOpen(_people));
            _screen?.EnableInClassList("base-drawer-open", IsOpen(_right));   // 거점 서랍은 더 넓어 더 비킨다
        }
    }

    /// <summary>1겹 맨 아래: 이번 주 14칸을 한 줄로. 누르면 계획 보드가 열린다. 실행 중에는 지금 칸을 표시한다.</summary>
    internal sealed class DockView
    {
        private readonly GameSession _session;
        private readonly Button[] _cells = new Button[WeekPlan.SlotCount];

        public DockView(VisualElement root, GameSession session, Action onOpen)
        {
            _session = session;
            var slots = root.Q("dock-slots");
            for (var day = 1; day <= CalendarState.DaysPerWeek; day++)
            {
                var column = new VisualElement();
                column.AddToClassList("dock-day");
                var label = new Label();
                label.AddToClassList("dock-day-label");
                PlanLook.Weekday(label, day, session.Definitions.Balance);
                column.Add(label);
                foreach (var phase in new[] { DayPhase.Day, DayPhase.Night })
                {
                    var index = CalendarRules.SlotIndex(day, phase);
                    var cell = new Button(onOpen);
                    cell.AddToClassList("dock-cell");
                    cell.EnableInClassList("day", phase == DayPhase.Day);
                    cell.EnableInClassList("night", phase == DayPhase.Night);
                    _cells[index] = cell;
                    column.Add(cell);
                }

                slots.Add(column);
            }
        }

        public void Refresh(ICollection<int> warnedSlots)
        {
            var state = _session.State;
            var calendar = state.Calendar;
            var current = calendar.WeekComplete ? WeekPlan.SlotCount : CalendarRules.SlotIndex(calendar.Day, calendar.Phase);
            var executing = _session.IsExecuting;
            for (var i = 0; i < _cells.Length; i++)
            {
                var slot = state.Plan.Slots[i];
                var cell = _cells[i];
                var empty = string.IsNullOrEmpty(slot.ActionId);
                cell.text = empty ? "·" : Loc.Action(slot.ActionId, "short");
                StoryArt.SetIcon(cell, empty ? null : SlotIcon(slot), 28f);
                cell.tooltip = empty ? Loc.T("planner.empty") : Loc.Action(slot.ActionId);
                cell.EnableInClassList("empty", empty);
                cell.EnableInClassList("locked", slot.Locked);
                cell.EnableInClassList("forced", slot.ForcedRest && !slot.Locked);
                cell.EnableInClassList("warned", warnedSlots.Contains(i));
                cell.EnableInClassList("done", executing && i < current);
                cell.EnableInClassList("current", executing && i == current);
            }
        }

        /// <summary>칸 아이콘: 강제 휴식 → 행동 아이콘 → 행동 분류 아이콘.</summary>
        private UnityEngine.Texture2D SlotIcon(PlannedSlot slot)
        {
            if (slot.ForcedRest && !slot.Locked)
            {
                var forced = StoryArt.Icon("act.forced_rest");
                if (forced != null) return forced;
            }

            var icon = StoryArt.Icon("act." + slot.ActionId);
            if (icon == null && _session.Definitions.TryGetAction(slot.ActionId, out var action))
            {
                icon = StoryArt.Icon("cat." + action.Category);
            }

            return icon;
        }
    }

    /// <summary>1겹 입상 옆: 생명 수치 요약 (자세한 것은 생활기록부 서랍). 누르면 서랍이 열린다.</summary>
    internal sealed class MiniVitalsView
    {
        private static readonly VitalType[] Shown = { VitalType.Hp, VitalType.Fatigue, VitalType.Stress, VitalType.Satiety, VitalType.Hydration };

        private readonly GameSession _session;
        private readonly Dictionary<VitalType, ProgressBar> _bars = new Dictionary<VitalType, ProgressBar>();
        private readonly Label _injury;
        private readonly Label _symptom;
        private readonly Label _sleep;

        public MiniVitalsView(VisualElement root, GameSession session, Action onOpen)
        {
            _session = session;
            var panel = root.Q("vitals-mini");
            panel.RegisterCallback<ClickEvent>(_ => onOpen());
            foreach (var vital in Shown)
            {
                var centered = vital == VitalType.Satiety || vital == VitalType.Hydration;
                var bar = new ProgressBar { lowValue = centered ? -100 : 0, highValue = 100 };
                bar.AddToClassList("vital-bar");
                bar.AddToClassList("vital-" + vital.ToString().ToLowerInvariant());
                StoryArt.SetIcon(bar, StoryArt.Icon("vital." + vital), 20f);
                panel.Add(bar);
                _bars[vital] = bar;
            }

            _injury = Flag(panel, "");
            _symptom = Flag(panel, "");
            _sleep = Flag(panel, "warn");
        }

        public void Refresh()
        {
            var state = _session.State;
            var player = state.Player;
            var balance = _session.Definitions.Balance;
            foreach (var pair in _bars)
            {
                var value = player.Vitals.GetVital(pair.Key);
                pair.Value.value = value;
                var text = pair.Key == VitalType.Satiety || pair.Key == VitalType.Hydration ? UiText.Signed(value) : value.ToString();
                if (pair.Key == VitalType.Hp)
                {
                    // 허기만큼 줄어든 최대 체력
                    var max = NourishRules.MaxHp(state, balance);
                    if (max < balance.VitalMax)
                    {
                        text += "/" + max;
                    }
                }

                pair.Value.title = UiText.Vital(pair.Key) + " " + text;
            }

            _injury.text = Loc.T("status.injured");
            _injury.EnableInClassList("hidden", !InjuryRules.IsInjured(state, balance));
            var symptom = InfectionRules.SymptomLevel(state);
            _symptom.text = Loc.T("symptom." + symptom);
            _symptom.EnableInClassList("hidden", symptom == 0);
            _sleep.text = Loc.T("status.sleep_debt") + " " + player.SleepDebt;
            _sleep.EnableInClassList("hidden", player.SleepDebt < 3);
        }

        private static Label Flag(VisualElement parent, string extraClass)
        {
            var label = new Label();
            label.AddToClassList("mini-flag");
            if (!string.IsNullOrEmpty(extraClass))
            {
                label.AddToClassList(extraClass);
            }

            label.AddToClassList("hidden");
            parent.Add(label);
            return label;
        }
    }
}
