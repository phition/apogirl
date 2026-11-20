using System;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Planning;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;
using ApoGirl.Presentation.Art;
using ApoGirl.Presentation.Text;
using ApoGirl.Systems.Session;
using UnityEngine.UIElements;

namespace ApoGirl.Presentation.Screens
{
    // 허브 화면 영역별 뷰. 규칙은 모르고, GameSession의 상태를 그리고 명령만 호출한다.

    /// <summary>점진적 노출 (문서 5.3): 이벤트가 세우는 UI_* 플래그, 4주차 이후 또는 디버그 UI_ALL이면 전부 보인다.</summary>
    internal static class Disclosure
    {
        public static bool Has(GameState state, string flag)
        {
            return state.Flags.Contains(flag) || state.Flags.Contains("UI_ALL")
                   || (flag != "UI_DEFENSE" && flag != "UI_SLEEPDEBT" && state.Calendar.Week >= 4);
        }
    }

    /// <summary>상단 바: 달력, 자원, 정전 표시.</summary>
    internal sealed class TopBarView
    {
        private static readonly ResourceType[] Shown =
        {
            ResourceType.Food, ResourceType.Water, ResourceType.RawWater, ResourceType.Medicine, ResourceType.Materials, ResourceType.Power, ResourceType.Fuel,
        };

        /// <summary>1~3주차엔 식량·물만 (문서 5.3 점진적 노출). UI_RESOURCES 이후 전부.</summary>
        private static bool Visible(GameState state, ResourceType type)
        {
            // 빗물(정수 필요)은 받아 둔 게 있을 때만 식수 옆에 따로 보인다
            if (type == ResourceType.RawWater)
            {
                return state.Base.GetResource(ResourceType.RawWater) > 0;
            }

            return type == ResourceType.Food || type == ResourceType.Water || Disclosure.Has(state, "UI_RESOURCES");
        }

        private Label _defense;

        private readonly GameSession _session;
        private readonly Label _calendar;
        private readonly Dictionary<ResourceType, Label> _resources = new Dictionary<ResourceType, Label>();
        private readonly Label _blackout;

        public TopBarView(VisualElement root, GameSession session)
        {
            _session = session;
            _calendar = root.Q<Label>("calendar-label");
            var container = root.Q("resources");
            foreach (var type in Shown)
            {
                var label = new Label();
                label.AddToClassList("resource");
                StoryArt.SetIcon(label, StoryArt.Icon("res." + type));
                container.Add(label);
                _resources[type] = label;
            }

            _defense = new Label();
            _defense.AddToClassList("resource");
            StoryArt.SetIcon(_defense, StoryArt.Icon("hub.defense"));
            container.Add(_defense);

            _blackout = new Label(Loc.T("hub.blackout"));
            _blackout.AddToClassList("blackout");
            container.Add(_blackout);

            _weather = new Label();
            _weather.AddToClassList("resource");
            container.Add(_weather);

            _rescue = new Label();
            _rescue.AddToClassList("resource");
            _rescue.AddToClassList("rescue-countdown");
            StoryArt.SetIcon(_rescue, StoryArt.Icon("hub.rescue"));
            container.Add(_rescue);
        }

        private readonly Label _weather;
        private readonly Label _rescue;

        public void Refresh()
        {
            var state = _session.State;
            var calendar = state.Calendar;
            var season = UiText.Season(CalendarRules.GetSeason(calendar.Week));
            StoryArt.SetIcon(_calendar, StoryArt.Icon("season." + CalendarRules.GetSeason(calendar.Week)), 22f);
            _calendar.text = _session.IsPlanning
                ? Loc.F("hub.calendar_planning", calendar.Week, season)
                : Loc.F("hub.calendar", calendar.Week, season, calendar.Day, UiText.Phase(calendar.Phase));

            foreach (var pair in _resources)
            {
                pair.Value.text = UiText.Resource(pair.Key) + " " + state.Base.GetResource(pair.Key);
                pair.Value.EnableInClassList("hidden", !Visible(state, pair.Key));
            }

            var showDefense = Disclosure.Has(state, "UI_DEFENSE");
            _defense.EnableInClassList("hidden", !showDefense);
            _defense.text = Loc.F("hub.defense", state.Base.Defense, state.Base.Threat);
            _defense.EnableInClassList("danger", state.Base.Threat > state.Base.Defense + 15);

            _blackout.EnableInClassList("hidden", !state.Base.IsBlackout());

            // M6: 날씨, 구조 카운트다운 (라디오 공지 이후)
            var weather = state.Base.Weather;
            _weather.text = Loc.F("hub.weather", Loc.T("weather." + weather));
            StoryArt.SetIcon(_weather, StoryArt.Icon("weather." + weather));
            _weather.tooltip = weather == WeatherType.Clear ? "" : Loc.T("weather." + weather + ".desc");
            var announced = state.Flags.Contains("RESCUE_ANNOUNCED");
            _rescue.EnableInClassList("hidden", !announced);
            _rescue.text = Loc.F("hub.rescue", Math.Max(0, CalendarState.TotalWeeks - calendar.Week));
        }
    }

    /// <summary>좌측 스탯 패널.</summary>
    internal sealed class StatusPanelView
    {
        private static readonly VitalType[] ShownVitals =
        {
            VitalType.Hp, VitalType.Fatigue, VitalType.Stress, VitalType.Cleanliness, VitalType.Satiety, VitalType.Hydration,
        };

        private readonly GameSession _session;
        private readonly Dictionary<StatType, Label> _stats = new Dictionary<StatType, Label>();
        private readonly Dictionary<ProficiencyType, Label> _proficiencies = new Dictionary<ProficiencyType, Label>();
        private readonly Dictionary<VitalType, ProgressBar> _vitals = new Dictionary<VitalType, ProgressBar>();
        private readonly Label _sleepDebt;
        private readonly Label _conscience;
        private readonly Label _derived;
        private readonly VisualElement _statsSection = new VisualElement();
        private readonly VisualElement _profSection = new VisualElement();
        private readonly VisualElement _debtSection = new VisualElement();
        private readonly VisualElement _baseSection = new VisualElement();
        private readonly Label _baseInfo;

        public StatusPanelView(VisualElement root, GameSession session)
        {
            _session = session;
            var panel = root.Q("status-panel");

            panel.Add(Header("status.vitals"));
            foreach (var vital in ShownVitals)
            {
                // 포만·수분은 −100 ~ +100 (가운데 0)
                var centered = vital == VitalType.Satiety || vital == VitalType.Hydration;
                var bar = new ProgressBar { lowValue = centered ? -100 : 0, highValue = 100 };
                bar.AddToClassList("vital-bar");
                bar.AddToClassList("vital-" + vital.ToString().ToLowerInvariant());
                StoryArt.SetIcon(bar, StoryArt.Icon("vital." + vital), 16f);
                panel.Add(bar);
                _vitals[vital] = bar;
            }

            _injury = Row(panel);
            _injury.AddToClassList("danger");
            _injury.style.whiteSpace = WhiteSpace.Normal;

            _symptom = Row(panel);
            _symptom.AddToClassList("danger");
            StoryArt.SetIcon(_symptom, StoryArt.Icon("status.symptom"), 16f);

            panel.Add(_debtSection);
            _sleepDebt = Row(_debtSection);
            _conscience = Row(_debtSection);
            StoryArt.SetIcon(_sleepDebt, StoryArt.Icon("status.sleep_debt"), 16f);
            StoryArt.SetIcon(_conscience, StoryArt.Icon("status.conscience"), 16f);

            panel.Add(_statsSection);
            _statsSection.Add(Header("status.stats"));
            foreach (StatType stat in Enum.GetValues(typeof(StatType)))
            {
                _stats[stat] = Row(_statsSection);
                StoryArt.SetIcon(_stats[stat], StoryArt.Icon("stat." + stat), 16f);
            }

            _derived = Row(_statsSection);
            _derived.AddToClassList("derived");

            panel.Add(_profSection);
            _profSection.Add(Header("status.proficiency"));
            foreach (ProficiencyType proficiency in Enum.GetValues(typeof(ProficiencyType)))
            {
                _proficiencies[proficiency] = Row(_profSection);
                StoryArt.SetIcon(_proficiencies[proficiency], StoryArt.Icon("prof." + proficiency), 16f);
            }

            panel.Add(_baseSection);
            _baseSection.Add(Header("status.base"));
            _baseInfo = Row(_baseSection);
            _baseInfo.style.whiteSpace = WhiteSpace.Normal;
            _baseSection.Add(_companions);

            panel.Add(_factionSection);
            _factionSection.Add(Header("status.factions"));
            _factionSection.Add(_factions);
        }

        /// <summary>세력 이름을 누르면 (허브가 세력 카드를 연다).</summary>
        public Action<string> FactionClicked { get; set; }

        private readonly VisualElement _factionSection = new VisualElement();
        private readonly VisualElement _factions = new VisualElement();

        /// <summary>알게 된 세력: 이름 · 단계 평판, 아래 한 줄 근황.</summary>
        private void RefreshFactions(GameState state)
        {
            _factions.Clear();
            var any = false;
            foreach (var def in _session.Definitions.Extras.Factions)
            {
                if (!state.Factions.TryGetValue(def.Id, out var faction) || !faction.Known)
                {
                    continue;
                }

                any = true;
                var tier = FactionRules.TierOf(faction.Standing);
                var gone = FactionRules.IsGone(state, def);
                var factionId = def.Id;
                var statusKey = gone ? "faction." + def.Id + ".status.gone" : "faction." + def.Id + ".status." + tier;
                var button = new Button(() => FactionClicked?.Invoke(factionId))
                {
                    text = Loc.F("status.faction", Loc.T("faction." + def.Id + ".name"), gone ? Loc.T("faction.tier.gone") : Loc.T("faction.tier." + tier),
                        (faction.Standing > 0 ? "+" : "") + faction.Standing),
                    tooltip = Loc.T(statusKey),
                };
                button.AddToClassList("small-button");
                button.AddToClassList("companion-button");
                button.AddToClassList("faction-" + tier.ToString().ToLowerInvariant());
                _factions.Add(button);

                // ProgressBar는 음수 최솟값을 그리지 못해서 0~200으로 옮겨 그린다 (가운데 = 평판 0)
                var bar = new ProgressBar { lowValue = 0, highValue = FactionRules.MaxStanding - FactionRules.MinStanding, value = faction.Standing - FactionRules.MinStanding };
                bar.AddToClassList("faction-bar");
                _factions.Add(bar);

                var status = Row(_factions);
                status.text = Loc.T(statusKey);
                status.AddToClassList("dim");
                status.style.whiteSpace = WhiteSpace.Normal;
            }

            _factionSection.EnableInClassList("hidden", !any);
        }

        private readonly Label _symptom;
        private readonly Label _injury;
        private readonly VisualElement _companions = new VisualElement();

        /// <summary>합류한 동료: 누르면 역할이 차례로 바뀐다 (계획 단계에서만).</summary>
        private void RefreshCompanions(GameState state)
        {
            _companions.Clear();
            var roles = _session.Definitions.Extras.Roles;
            foreach (var pair in state.Npcs.Where(n => n.Value.Status == NpcStatus.Joined))
            {
                var npcId = pair.Key;
                var npc = pair.Value;
                var roleName = string.IsNullOrEmpty(npc.Role) ? Loc.T("role.none") : Loc.T("role." + npc.Role);
                var button = new Button(() =>
                {
                    if (roles.Count == 0) return;
                    var index = roles.FindIndex(r => r.Id == npc.Role);
                    var next = index + 1 >= roles.Count ? "" : roles[index + 1].Id;
                    _session.SetNpcRole(npcId, next);
                })
                {
                    text = Loc.F("status.npc", Loc.T("npc." + npcId), npc.Affinity) + " · " + roleName,
                    tooltip = string.IsNullOrEmpty(npc.Role) ? Loc.T("status.role_hint") : Loc.T("role." + npc.Role + ".desc"),
                };
                button.AddToClassList("small-button");
                button.AddToClassList("companion-button");
                StoryArt.SetIcon(button, StoryArt.Icon("npc." + npcId), 24f);
                button.SetEnabled(_session.IsPlanning && roles.Count > 0);
                _companions.Add(button);
            }
        }

        public void Refresh()
        {
            var state = _session.State;
            var player = state.Player;
            var balance = _session.Definitions.Balance;

            foreach (var pair in _vitals)
            {
                var value = player.Vitals.GetVital(pair.Key);
                pair.Value.value = value;
                pair.Value.title = UiText.Vital(pair.Key) + " " + value;
                if (pair.Key == VitalType.Satiety || pair.Key == VitalType.Hydration)
                {
                    var band = UpkeepRules.Band(pair.Key == VitalType.Satiety ? balance.SatietyBands : balance.HydrationBands, value);
                    pair.Value.title = UiText.Vital(pair.Key) + " " + (value > 0 ? "+" : "") + value + (band != null ? " · " + Loc.T("band." + band.Key) : "");
                }

                var basic = pair.Key == VitalType.Hp || pair.Key == VitalType.Stress || pair.Key == VitalType.Fatigue || pair.Key == VitalType.Cleanliness
                            || pair.Key == VitalType.Satiety || pair.Key == VitalType.Hydration;
                pair.Value.EnableInClassList("hidden", !basic && !Disclosure.Has(state, "UI_RESOURCES"));
            }

            _statsSection.EnableInClassList("hidden", !Disclosure.Has(state, "UI_STATS"));
            _profSection.EnableInClassList("hidden", !Disclosure.Has(state, "UI_PROFICIENCY"));
            _debtSection.EnableInClassList("hidden", !Disclosure.Has(state, "UI_SLEEPDEBT"));

            var facilities = state.Base.Facilities.Where(f => f.Value > 0).Select(f => Loc.T("facility." + f.Key)).ToList();
            var people = new List<string>();
            if (_session.HasMetTeacher)
            {
                people.Add(Loc.F("status.teacher", state.Teacher.Affinity, Loc.T("mobility." + Math.Min(state.Teacher.MobilityStage, 2))));
            }

            var stage = InfectionRules.TeacherStage(state, balance);
            if (stage > 0)
            {
                people.Add(Loc.T("teacher.stage." + Math.Min(stage, 4)));
            }

            _baseInfo.text = Loc.F("status.facilities", facilities.Count > 0 ? string.Join(", ", facilities) : "-") + "\n" + string.Join("\n", people);

            // 부상: 의약품으로 치료하기 전까지 체력이 회복되지 않는다
            var injured = InjuryRules.IsInjured(state, balance);
            _injury.text = Loc.T("status.injured");
            _injury.EnableInClassList("hidden", !injured);

            // M6: 증상 (감염도 숫자는 숨긴다, 문서 5.3)
            var symptom = InfectionRules.SymptomLevel(state);
            _symptom.text = Loc.T("symptom." + symptom);
            _symptom.EnableInClassList("hidden", symptom == 0);

            RefreshCompanions(state);
            RefreshFactions(state);

            _sleepDebt.text = Loc.T("status.sleep_debt") + "  " + Pips(player.SleepDebt, balance.SleepDebtMax);
            _sleepDebt.EnableInClassList("danger", player.SleepDebt >= 3);
            _conscience.text = Loc.T("status.conscience") + "  " + player.Conscience + " / " + balance.ConscienceMax;

            foreach (var pair in _stats)
            {
                pair.Value.text = UiText.Stat(pair.Key) + "  " + player.GetStat(pair.Key);
            }

            _derived.text = Loc.T("status.combat") + " " + DerivedStats.CombatPower(state, balance)
                            + "   " + Loc.T("status.stress_threshold") + " " + DerivedStats.StressThreshold(state, balance);

            foreach (var pair in _proficiencies)
            {
                var level = player.ProficiencyLevel(pair.Key);
                pair.Value.text = UiText.Proficiency(pair.Key) + "  " + Loc.F("status.level", level);
                pair.Value.EnableInClassList("dim", level == 0);
            }
        }

        private static Label Header(string key)
        {
            var label = new Label(Loc.T(key));
            label.AddToClassList("section-header");
            return label;
        }

        private static Label Row(VisualElement parent)
        {
            var label = new Label();
            label.AddToClassList("status-row");
            parent.Add(label);
            return label;
        }

        private static string Pips(int value, int max)
        {
            return new string('■', Math.Max(0, value)) + new string('□', Math.Max(0, max - value));
        }
    }

    /// <summary>우측 행동 팔레트: 카테고리 탭 + 행동 카드(미리보기).</summary>
    internal sealed class PaletteView
    {
        private static readonly ActionCategory[] Tabs =
        {
            ActionCategory.Education, ActionCategory.Survival, ActionCategory.Rest, ActionCategory.Daily, ActionCategory.Expedition,
        };

        private readonly GameSession _session;
        private readonly VisualElement _tabs;
        private readonly ScrollView _list;
        private ActionCategory _category = ActionCategory.Education;

        public PaletteView(VisualElement root, GameSession session)
        {
            _session = session;
            _tabs = root.Q("palette-tabs");
            _list = root.Q<ScrollView>("palette-list");
            foreach (var category in Tabs)
            {
                var tab = new Button(() =>
                {
                    _category = category;
                    Refresh();
                }) { text = Loc.T("cat." + category), name = "tab-" + category };
                tab.AddToClassList("tab");
                StoryArt.SetIcon(tab, StoryArt.Icon("cat." + category), 16f);
                _tabs.Add(tab);
            }

            root.Q<Label>("palette-hint").text = Loc.T("palette.hint");
        }

        public string SelectedActionId { get; private set; } = "";

        public void Refresh()
        {
            foreach (var category in Tabs)
            {
                _tabs.Q("tab-" + category).EnableInClassList("selected", category == _category);
            }

            _list.Clear();
            foreach (var action in _session.ActionsIn(_category))
            {
                if (_session.IsUnlocked(action))
                {
                    _list.Add(BuildCard(action));
                }
            }
        }

        private VisualElement BuildCard(ActionDef action)
        {
            var preview = _session.Preview(action.Id);
            var card = new Button(() =>
            {
                SelectedActionId = SelectedActionId == action.Id ? "" : action.Id;
                Refresh();
            });
            card.AddToClassList("action-card");
            card.EnableInClassList("selected", SelectedActionId == action.Id);
            card.EnableInClassList("unaffordable", !preview.Affordable);

            card.Add(Line(Loc.Action(action.Id), "action-name"));
            if (preview.IsExpedition)
            {
                var key = preview.ExpeditionTime == ExpeditionTime.Day ? "palette.expedition_day" : "palette.expedition_night";
                card.Add(Line(Loc.T(key), "action-info"));
                return card;
            }

            var chance = preview.HasCheck
                ? Loc.F("palette.chance", Math.Round(preview.SuccessChance * 100))
                : Loc.T("palette.no_check");
            if (!preview.Affordable)
            {
                chance = Loc.T("palette.unaffordable");
            }

            card.Add(Line(chance + "  ·  " + Loc.F("palette.cost", preview.FatigueCost, preview.StressCost), "action-info"));

            var gains = preview.ExpectedGains.Where(g => g.Amount != 0)
                .Select(g => UiText.Stat(g.Stat) + " " + UiText.Signed(g.Amount)).ToList();
            if (gains.Count > 0)
            {
                card.Add(Line(Loc.F("palette.gains", string.Join("  ", gains)), "action-gains"));
            }

            return card;
        }

        private static Label Line(string text, string cssClass)
        {
            var label = new Label(text);
            label.AddToClassList(cssClass);
            return label;
        }
    }

    /// <summary>하단 주간 플래너: 7일 × 낮/밤.</summary>
    internal sealed class PlannerView
    {
        private readonly GameSession _session;
        private readonly Func<string> _selectedAction;
        private readonly Action<string> _notify;
        private readonly Button[] _cells = new Button[WeekPlan.SlotCount];

        public PlannerView(VisualElement root, GameSession session, Func<string> selectedAction, Action<string> notify)
        {
            _session = session;
            _selectedAction = selectedAction;
            _notify = notify;
            var planner = root.Q("planner");

            var header = new VisualElement();
            header.AddToClassList("planner-row");
            header.Add(Corner());
            for (var day = 1; day <= CalendarState.DaysPerWeek; day++)
            {
                var label = new Label(Loc.F("planner.day_header", day));
                label.AddToClassList("planner-day");
                header.Add(label);
            }

            planner.Add(header);
            foreach (var phase in new[] { DayPhase.Day, DayPhase.Night })
            {
                var row = new VisualElement();
                row.AddToClassList("planner-row");
                var rowLabel = new Label(UiText.Phase(phase));
                rowLabel.AddToClassList("planner-phase");
                row.Add(rowLabel);
                for (var day = 1; day <= CalendarState.DaysPerWeek; day++)
                {
                    var index = CalendarRules.SlotIndex(day, phase);
                    var cell = new Button(() => OnClick(index));
                    cell.AddToClassList("slot");
                    cell.RegisterCallback<PointerDownEvent>(evt =>
                    {
                        if (evt.button == 1)
                        {
                            OnClear(index);
                        }
                    });
                    _cells[index] = cell;
                    row.Add(cell);
                }

                planner.Add(row);
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
                var text = empty ? Loc.T("planner.empty") : Loc.Action(slot.ActionId, "short");
                if (slot.Locked)
                {
                    text += "\n" + Loc.T("planner.locked");
                }
                else if (slot.ForcedRest)
                {
                    text += "\n" + Loc.T("planner.forced");
                }

                cell.text = text;
                cell.EnableInClassList("empty", empty);
                cell.EnableInClassList("locked", slot.Locked);
                cell.EnableInClassList("forced", slot.ForcedRest && !slot.Locked);
                cell.EnableInClassList("warned", warnedSlots.Contains(i));
                cell.EnableInClassList("done", executing && i < current);
                cell.EnableInClassList("current", executing && i == current);
                cell.SetEnabled(_session.IsPlanning);
            }
        }

        private void OnClick(int index)
        {
            var actionId = _selectedAction();
            if (string.IsNullOrEmpty(actionId))
            {
                _notify(Loc.T("plan.select_first"));
                return;
            }

            Report(_session.SetSlot(index, actionId));
        }

        private void OnClear(int index)
        {
            if (_session.IsPlanning)
            {
                Report(_session.ClearSlot(index));
            }
        }

        private void Report(PlanEditResult result)
        {
            switch (result)
            {
                case PlanEditResult.Locked:
                    _notify(Loc.T("plan.edit_locked"));
                    break;
                case PlanEditResult.WrongSlot:
                    _notify(Loc.T("plan.edit_wrong_slot"));
                    break;
            }
        }

        private static VisualElement Corner()
        {
            var corner = new VisualElement();
            corner.AddToClassList("planner-phase");
            return corner;
        }
    }

    /// <summary>플래너 옆: 경고, 예상 수면 부채.</summary>
    internal sealed class PlanSideView
    {
        private readonly GameSession _session;
        private readonly ScrollView _warnings;
        private readonly Label _forecast;

        public PlanSideView(VisualElement root, GameSession session)
        {
            _session = session;
            _warnings = root.Q<ScrollView>("warnings");
            _forecast = root.Q<Label>("forecast");
        }

        /// <returns>경고가 걸린 슬롯 번호.</returns>
        public HashSet<int> Refresh()
        {
            var warned = new HashSet<int>();
            _warnings.Clear();
            if (!_session.IsPlanning)
            {
                _forecast.text = "";
                return warned;
            }

            var warnings = _session.Warnings();
            foreach (var warning in warnings)
            {
                var label = new Label("※ " + UiText.Warning(warning));
                label.AddToClassList("warning");
                _warnings.Add(label);
                if (warning.SlotIndex >= 0)
                {
                    warned.Add(warning.SlotIndex);
                }
            }

            if (warnings.Count == 0)
            {
                var none = new Label(Loc.T("warn.none"));
                none.AddToClassList("warning-none");
                _warnings.Add(none);
            }

            var projection = _session.ProjectedSleepDebt();
            var end = projection.Length > 0 ? projection[projection.Length - 1] : _session.State.Player.SleepDebt;
            _forecast.text = Loc.F("plan.forecast", _session.State.Player.SleepDebt, end);
            _forecast.EnableInClassList("danger", end >= 3);
            return warned;
        }
    }
}
