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
            // 빗물은 받아 둔 게 있을 때만 식수 옆에 따로 보인다
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
            StoryArt.SetIcon(_calendar, StoryArt.Icon("season." + CalendarRules.GetSeason(calendar.Week)), 28f);
            var date = CalendarRules.DateOf(calendar.Week, Math.Max(1, calendar.Day), _session.Definitions.Balance);
            _calendar.text = _session.IsPlanning
                ? Loc.F("hub.calendar_planning", calendar.Week, date.Month, date.Day, season)
                : Loc.F("hub.calendar", calendar.Week, date.Month, date.Day, season, UiText.Phase(calendar.Phase));

            foreach (var pair in _resources)
            {
                pair.Value.text = pair.Key == ResourceType.Power
                    ? Loc.F("hub.power", PowerRules.Free(state, _session.Definitions.Balance), PowerRules.Capacity(state))   // 여유/용량 (전력 = 유지력)
                    : (pair.Key == ResourceType.RawWater ? Loc.T("hud.raw_water") : UiText.Resource(pair.Key)) + " " + state.Base.GetResource(pair.Key);   // 상단 줄은 짧게 "빗물" (다른 화면은 "빗물(정수 필요)")
                pair.Value.EnableInClassList("hidden", !Visible(state, pair.Key));
            }

            var showDefense = Disclosure.Has(state, "UI_DEFENSE");
            _defense.EnableInClassList("hidden", !showDefense);
            _defense.text = Loc.F("hub.defense", state.Base.Defense, state.Base.Threat);
            _defense.EnableInClassList("danger", state.Base.Threat > state.Base.Defense + 15);

            _blackout.EnableInClassList("hidden", !ApoGirl.Core.Rules.PowerRules.IsBlackout(state));

            // M6: 날씨, 구조 카운트다운 (라디오 공지 이후)
            var weather = state.Base.Weather;
            var disaster = DisasterRules.Active(state, _session.Definitions.Balance);
            if (disaster != null)
            {
                // 계절 재해 (확장 6): 장마·폭설
                _weather.text = Loc.F("hub.weather", Loc.T("disaster." + disaster.Id));
                StoryArt.SetIcon(_weather, StoryArt.Icon("disaster." + disaster.Id));
                _weather.tooltip = Loc.T("weather." + weather + ".desc") + "\n" + Loc.T("disaster." + disaster.Id + ".desc");
            }
            else
            {
                _weather.text = Loc.F("hub.weather", Loc.T("weather." + weather));
                StoryArt.SetIcon(_weather, StoryArt.Icon("weather." + weather));
                _weather.tooltip = weather == WeatherType.Clear ? "" : Loc.T("weather." + weather + ".desc");
            }
            var announced = state.Flags.Contains("RESCUE_ANNOUNCED");
            _rescue.EnableInClassList("hidden", !announced);
            _rescue.text = Loc.F("hub.rescue", Math.Max(0, CalendarState.TotalWeeks - calendar.Week));
        }
    }

    /// <summary>
    /// 세력 덧판: 인물 서랍 왼쪽 가장자리에 붙은 화살표 탭 — 누르면 왼쪽으로 펼쳐 알게 된 세력 카드(얼굴·단계·평판 띠·근황·영향·거래 상태)를 보인다.
    /// 예전에는 거점 서랍 아래에 있었다 (2026-11-25 사용자 요청으로 옮김). 아는 세력이 없으면 탭도 숨긴다.
    /// </summary>
    internal sealed class FactionAddonView
    {
        private readonly GameSession _session;
        private readonly VisualElement _root;
        private readonly VisualElement _sheet;
        private readonly Button _tab;
        private readonly VisualElement _factions;
        private bool _expanded;

        public FactionAddonView(VisualElement root, GameSession session)
        {
            _session = session;
            _root = root.Q("people-factions");
            _sheet = root.Q("people-factions-sheet");
            _tab = root.Q<Button>("people-factions-tab");
            _factions = root.Q("people-factions-list");
            var title = root.Q<Label>("people-factions-title");
            if (title != null)
            {
                title.text = Loc.T("status.factions");
            }

            if (_tab != null)
            {
                _tab.clicked += () =>
                {
                    _expanded = !_expanded;
                    Sync();
                };

            }

            // 지금 거래할 수 있는 세력이 있으면 접힌 탭 모서리에 초록 점 (.trade-ready).
            // 탭(Button) 안에 넣으면 글자 크기로 재지 못해 탭이 줄어드니 덧판에 직접 띄운다.
            if (_root != null)
            {
                var dot = new VisualElement { name = "people-factions-dot", pickingMode = PickingMode.Ignore };
                dot.AddToClassList("people-factions-dot");
                _root.Add(dot);
            }

            Sync();
        }

        /// <summary>세력 이름을 누르면 (허브가 세력 카드를 연다).</summary>
        public Action<string> FactionClicked { get; set; }

        private void Sync()
        {
            _sheet?.EnableInClassList("hidden", !_expanded);
            _root?.EnableInClassList("expanded", _expanded);
            if (_tab != null)
            {
                _tab.text = Loc.T(_expanded ? "people.factions.close" : "people.factions.open");
                _tab.tooltip = Loc.T("people.factions.hint");
            }
        }

        /// <summary>최근 평판 변화를 머리 줄에 보이는 기간 (주).</summary>
        private const int RecentWeeks = 3;

        /// <summary>
        /// 알게 된 세력마다 카드 하나: 얼굴·이름·단계 칩·최근 변화 / 다섯 단계 띠와 다음 단계까지 / 근황 / 지금 받는 영향 /
        /// 거래 상태(지금 가능 n건·다음 거래까지·필요한 단계·물자 부족)와 무전 도착 예정. 카드를 누르면 거래 화면.
        /// </summary>
        public void Refresh()
        {
            if (_root == null)
            {
                return;
            }

            var state = _session.State;
            _factions.Clear();
            var any = false;
            var anyReady = false;
            foreach (var def in _session.Definitions.Extras.Factions)
            {
                if (!state.Factions.TryGetValue(def.Id, out var faction) || !faction.Known)
                {
                    continue;
                }

                any = true;
                _factions.Add(Card(def, faction, out var ready));
                anyReady |= ready > 0;
            }

            _root.EnableInClassList("hidden", !any);
            _root.EnableInClassList("trade-ready", anyReady);
            Sync();
        }

        private VisualElement Card(FactionDef def, FactionState faction, out int ready)
        {
            var state = _session.State;
            var tier = FactionRules.TierOf(faction.Standing);
            var gone = FactionRules.IsGone(state, def);
            var tierClass = "tier-" + (gone ? "gone" : tier.ToString().ToLowerInvariant());
            var factionId = def.Id;

            var card = new VisualElement();
            card.AddToClassList("fac-mini");
            card.EnableInClassList("gone", gone);
            card.AddManipulator(new Clickable(() => FactionClicked?.Invoke(factionId)));
            card.tooltip = Loc.T("people.factions.card_hint");

            // ── 머리: 얼굴 | 이름·단계·최근 변화 / 단계 띠 / 다음 단계까지 ──
            var head = new VisualElement();
            head.AddToClassList("fac-mini-head");
            var face = new VisualElement();
            face.AddToClassList("fac-mini-face");
            face.AddToClassList(tierClass);
            var portrait = StoryArt.Faction(def.Id);
            if (portrait != null)
            {
                StoryArt.SetCover(face, portrait);
            }
            else
            {
                var name0 = Loc.T("faction." + def.Id + ".name");
                Add(face, name0.Length > 0 ? name0.Substring(0, 1) : "?", "fac-mini-initial");
            }

            head.Add(face);

            var side = new VisualElement();
            side.AddToClassList("fac-mini-side");
            var titleRow = new VisualElement();
            titleRow.AddToClassList("fac-mini-title");
            Add(titleRow, Loc.T("faction." + def.Id + ".name"), "fac-mini-name");
            var chip = Add(titleRow, gone ? Loc.T("faction.tier.gone") : Loc.T("faction.tier." + tier), "fac-mini-tier");
            chip.AddToClassList(tierClass);
            if (!gone && faction.Log.Count > 0 && faction.Log[0].Standing != 0 && state.Calendar.Week - faction.Log[0].Week < RecentWeeks)
            {
                var last = faction.Log[0];
                var trend = Add(titleRow, (last.Standing > 0 ? "▲" : "▼") + UiText.Signed(last.Standing), "fac-mini-trend");
                trend.AddToClassList(last.Standing > 0 ? "up" : "down");
                trend.tooltip = Loc.F("faction.log.week", last.Week) + "  "
                    + (string.IsNullOrEmpty(last.TextKey) ? Loc.T("faction.log.drift") : Loc.T(last.TextKey));
            }

            side.Add(titleRow);
            if (!gone)
            {
                side.Add(MiniGauge(faction.Standing));
                Add(side, NextTierText(faction.Standing, tier), "fac-mini-next");
            }

            head.Add(side);
            card.Add(head);

            // ── 근황 · 지금 받는 영향 ──
            Add(card, Loc.T(gone ? "faction." + def.Id + ".status.gone" : "faction." + def.Id + ".status." + tier), "fac-mini-status");
            if (!gone)
            {
                foreach (var effect in def.Effects)
                {
                    if (tier >= effect.MinTier && tier <= effect.MaxTier && !string.IsNullOrEmpty(effect.TextKey))
                    {
                        Add(card, "· " + Loc.T(effect.TextKey), "fac-mini-effect");
                    }
                }
            }

            // ── 거래 줄: 상태 | 거래 화면 ▸ ──
            var foot = new VisualElement();
            foot.AddToClassList("fac-mini-foot");
            var summary = Add(foot, TradeSummary(def, faction, gone, out ready, out var summaryClass), "fac-mini-trade");
            summary.AddToClassList(summaryClass);
            Add(foot, Loc.T(def.Trades.Count > 0 && !gone ? "people.factions.open_trade" : "people.factions.open_card"), "fac-mini-link");
            card.Add(foot);

            var pending = new List<string>();
            foreach (var item in faction.Pending)
            {
                if (item.Value != 0 && Enum.TryParse<ResourceType>(item.Key, out var type))
                {
                    pending.Add(UiText.Resource(type) + " " + item.Value);
                }
            }

            if (pending.Count > 0)
            {
                Add(card, Loc.F("people.factions.pending", string.Join(", ", pending)), "fac-mini-pending");
            }

            return card;
        }

        /// <summary>다섯 단계 색 띠 위에 지금 자리 (거래 화면 눈금의 작은 판).</summary>
        private static VisualElement MiniGauge(int standing)
        {
            var gauge = new VisualElement();
            gauge.AddToClassList("fac-mini-gauge");
            var span = (float)(FactionRules.MaxStanding - FactionRules.MinStanding);
            foreach (var band in FactionView.Bands)
            {
                var part = new VisualElement();
                part.AddToClassList("fac-mini-band");
                part.AddToClassList("tier-" + band.Tier.ToString().ToLowerInvariant());
                part.style.width = Length.Percent(100f * (band.To - band.From) / span);
                part.tooltip = Loc.T("faction.tier." + band.Tier);
                gauge.Add(part);
            }

            var marker = new VisualElement();
            marker.AddToClassList("fac-mini-marker");
            marker.pickingMode = PickingMode.Ignore;
            marker.style.left = Length.Percent(100f * (standing - FactionRules.MinStanding) / span);
            gauge.Add(marker);
            return gauge;
        }

        /// <summary>"평판 −20 · 중립까지 +1" — 한 단계 위까지 남은 양 (동맹이면 끝).</summary>
        private static string NextTierText(int standing, FactionTier tier)
        {
            var value = Loc.F("people.factions.standing", UiText.Signed(standing));
            int next;
            switch (tier)
            {
                case FactionTier.Hostile: next = FactionRules.HostileMax + 1; break;
                case FactionTier.Wary: next = FactionRules.WaryMax + 1; break;
                case FactionTier.Neutral: next = FactionRules.FriendlyMin; break;
                case FactionTier.Friendly: next = FactionRules.AlliedMin; break;
                default: return value + "  ·  " + Loc.T("people.factions.top");
            }

            return value + "  ·  " + Loc.F("people.factions.next", Loc.T("faction.tier." + (tier + 1)), next - standing);
        }

        /// <summary>거래 상태 한 줄: 지금 가능 n건 / 다음 거래까지 n주 / 물자 부족 / 필요한 단계 / 계획 중에만 / 거래 없음.</summary>
        private string TradeSummary(FactionDef def, FactionState faction, bool gone, out int ready, out string cssClass)
        {
            ready = 0;
            cssClass = "dim";
            if (gone)
            {
                return Loc.T("people.factions.trade_gone");
            }

            if (def.Trades.Count == 0)
            {
                return Loc.T("people.factions.trade_none");
            }

            var weekly = false;
            var shortOf = false;
            var planning = false;
            var minTier = FactionTier.Allied;
            foreach (var offer in def.Trades)
            {
                var blocker = _session.TradeBlocker(def.Id, offer.Id);
                if (blocker == null)
                {
                    ready++;
                }

                weekly |= blocker == "faction.trade.weekly" || blocker == "faction.trade.same";
                shortOf |= blocker == "faction.trade.short";
                planning |= blocker == "faction.trade.planning";
                if (offer.MinTier < minTier)
                {
                    minTier = offer.MinTier;
                }
            }

            if (ready > 0)
            {
                cssClass = "ready";
                return Loc.F("people.factions.trade_ready", ready);
            }

            if (planning)
            {
                return Loc.T("people.factions.trade_planning");
            }

            if (weekly)
            {
                var wait = faction.LastTradeWeek + Math.Max(1, def.TradeIntervalWeeks) - _session.State.Calendar.Week;
                return Loc.F("people.factions.trade_wait", Math.Max(1, wait));
            }

            if (shortOf)
            {
                cssClass = "warn";
                return Loc.T("people.factions.trade_short");
            }

            return Loc.F("people.factions.trade_tier", Loc.T("faction.tier." + minTier));
        }

        private static Label Add(VisualElement parent, string text, string cssClass)
        {
            var label = new Label(text);
            label.AddToClassList(cssClass);
            parent.Add(label);
            return label;
        }
    }

    /// <summary>우측 행동 팔레트: 카테고리 탭 + 행동 카드(미리보기).</summary>
    internal sealed class PaletteView
    {
        private static readonly ActionCategory[] Tabs =
        {
            ActionCategory.Education, ActionCategory.Survival, ActionCategory.Rest, ActionCategory.Daily, ActionCategory.Expedition,
            ActionCategory.Craft,
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
                StoryArt.SetIcon(tab, StoryArt.Icon("cat." + category), 18f);
                _tabs.Add(tab);
            }

            root.Q<Label>("palette-hint").text = Loc.T("palette.hint");
        }

        public string SelectedActionId { get; private set; } = "";

        /// <summary>제작 탭 머리의 "설계도" 단추 (제작 P7, CraftTreeView).</summary>
        public Action OpenTree { get; set; }

        // 카드 끌어다 놓기: 카드를 눌러 끌면 계획 칸에 바로 놓는다 (PlannerView.BeginCardDrag·MoveCardDrag·EndCardDrag에 잇는다).
        // 시간대별로 놓일 행동(지역 카드는 낮·밤이 다르다), 끄는 그림, 이름.
        public Action<Func<DayPhase, string>, UnityEngine.Texture2D, string> DragBegan { get; set; }
        public Action<UnityEngine.Vector2> DragMoved { get; set; }

        /// <summary>놓은 자리 (패널 좌표). 칸에 놓였으면 true.</summary>
        public Func<UnityEngine.Vector2, bool> DragEnded { get; set; }

        private const float CardDragThreshold = 8f;
        private int _dragPointer = -1;
        private UnityEngine.Vector2 _dragFrom;
        private bool _cardDragging;
        private bool _suppressClick;

        /// <summary>행동을 고른 상태로 (단면도에서 방의 행동을 눌렀을 때). 그 행동의 분류 탭으로 옮긴다.</summary>
        public void Select(string actionId)
        {
            if (_session.Definitions.TryGetAction(actionId, out var action) && Array.IndexOf(Tabs, action.Category) >= 0)
            {
                _category = action.Category;
            }

            SelectedActionId = actionId;
            Refresh();
        }

        public void Refresh()
        {
            // 제작 탭은 열린 제작 카드가 하나라도 있을 때만 보인다 (Docs/CRAFTING_PROPOSAL.md 1.1절)
            var craftOpen = _session.ActionsIn(ActionCategory.Craft).Any(_session.IsUnlocked);
            if (!craftOpen && _category == ActionCategory.Craft)
            {
                _category = ActionCategory.Education;
            }

            foreach (var category in Tabs)
            {
                var tab = _tabs.Q("tab-" + category);
                tab.EnableInClassList("selected", category == _category);
                tab.style.display = category != ActionCategory.Craft || craftOpen ? DisplayStyle.Flex : DisplayStyle.None;
            }

            _list.Clear();
            if (_category == ActionCategory.Craft && OpenTree != null)
            {
                var tree = new Button(() => OpenTree()) { text = Loc.T("craft.tree.open_button") };
                tree.AddToClassList("ct-open");
                _list.Add(tree);
            }

            foreach (var action in _session.ActionsIn(_category))
            {
                if (!_session.IsUnlocked(action))
                {
                    continue;
                }

                // 지역 탐험은 지역마다 카드 한 장: 낮 탐험이 열려 있으면 밤 탐험 카드는 따로 두지 않는다
                if (IsLocationRun(action) && action.ExpeditionTime == ExpeditionTime.Night && Sibling(action, DayPhase.Day) != null)
                {
                    continue;
                }

                _list.Add(BuildCard(action));
            }
        }

        /// <summary>
        /// 그 시간대 칸에 놓을 행동 ID. 지역 탐험 카드는 낮 칸이면 낮 탐험, 밤 칸이면 밤 탐험이 된다.
        /// 고른 것이 없으면 "", 그 지역의 그 시간대 탐험이 아직 열리지 않았으면 null.
        /// </summary>
        public string ActionFor(DayPhase phase)
        {
            if (string.IsNullOrEmpty(SelectedActionId) || !_session.Definitions.TryGetAction(SelectedActionId, out var action) || !IsLocationRun(action))
            {
                return SelectedActionId;
            }

            return Sibling(action, phase)?.Id;
        }

        private static bool IsLocationRun(ActionDef action) =>
            action.Category == ActionCategory.Expedition && !string.IsNullOrEmpty(action.LocationId);

        private static ExpeditionTime TimeOf(DayPhase phase) => phase == DayPhase.Day ? ExpeditionTime.Day : ExpeditionTime.Night;

        /// <summary>같은 지역의 그 시간대 탐험 (열린 것만). 자기 시간대면 자기 자신.</summary>
        private ActionDef Sibling(ActionDef action, DayPhase phase)
        {
            if (action.ExpeditionTime == TimeOf(phase))
            {
                return action;
            }

            return _session.ActionsIn(ActionCategory.Expedition)
                .FirstOrDefault(a => a.LocationId == action.LocationId && a.ExpeditionTime == TimeOf(phase) && _session.IsUnlocked(a));
        }

        private bool IsSelected(ActionDef action)
        {
            if (SelectedActionId == action.Id)
            {
                return true;
            }

            // 지역 카드: 같은 지역의 낮·밤 어느 쪽을 골라도 그 카드가 골린 것으로 보인다 (단면도에서 고른 경우)
            return IsLocationRun(action) && _session.Definitions.TryGetAction(SelectedActionId ?? "", out var selected)
                   && IsLocationRun(selected) && selected.LocationId == action.LocationId
                   && Sibling(selected, DayPhase.Day)?.Id == Sibling(action, DayPhase.Day)?.Id;
        }

        private VisualElement BuildCard(ActionDef action)
        {
            var preview = _session.Preview(action.Id);
            var card = new Button(() =>
            {
                if (_suppressClick)
                {
                    // 끌다가 놓은 것은 누르기로 치지 않는다
                    _suppressClick = false;
                    return;
                }

                SelectedActionId = IsSelected(action) ? "" : action.Id;
                Refresh();
            });
            card.AddToClassList("action-card");
            var locationRun = IsLocationRun(action);
            BindDrag(card, action, locationRun);
            var night = locationRun ? Sibling(action, DayPhase.Night) : null;
            var day = locationRun ? Sibling(action, DayPhase.Day) : null;
            var tag = locationRun ? (day != null && night != null ? SlotTag.Both : day != null ? SlotTag.DayOnly : SlotTag.NightOnly) : action.SlotTag;
            PlanLook.Phase(card, tag);

            // 거점 행동 아이콘 (P4 제작품 그림)
            PlanLook.Icon(card, StoryArt.Icon("act." + action.Id), 40f);
            card.EnableInClassList("selected", IsSelected(action));
            card.EnableInClassList("unaffordable", !preview.Affordable);

            if (locationRun)
            {
                // 지역 카드: 이름은 지역만 (낮·밤은 카드 색으로), 그 아래 그 지역에서 얻을 수 있는 물자
                var nameKey = "palette.exp." + action.LocationId;
                var name = Loc.T(nameKey);
                card.Add(Line(name == nameKey ? Loc.Action(action.Id) : name, "action-name"));
                var loot = LootRow(action.LocationId, day != null, night != null);
                if (loot != null)
                {
                    card.Add(loot);
                }

                return card;
            }

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

            card.Add(Line(chance + "  ·  " + Loc.F("palette.cost", UiText.Signed(preview.FatigueNet), UiText.Signed(preview.StressNet)), "action-info"));

            if (action.IsCraft)
            {
                // 제작: 재료·칸 수·못 하는 까닭, 그리고 아직 열리지 않은 다음 단계
                var craftLine = CraftUi.CardLine(action, preview, _session);
                if (craftLine.Length > 0)
                {
                    card.Add(Line(craftLine, "action-info"));
                }

                var next = CraftUi.NextLine(action, _session);
                if (next != null)
                {
                    card.Add(Line(next, "action-gains"));
                }
            }

            var gains = preview.ExpectedGains.Where(g => g.Amount != 0)
                .Select(g => UiText.Stat(g.Stat) + " " + UiText.Signed(g.Amount)).ToList();
            if (gains.Count > 0)
            {
                card.Add(Line(Loc.F("palette.gains", string.Join("  ", gains)), "action-gains"));
            }

            return card;
        }

        /// <summary>
        /// 지역에서 얻을 수 있는 물자 칩 (탐험 모델의 열린 시간대 자원, 많은 순).
        /// 나오지 않을 때도 있는 것(최소 0)은 흐리게. 모델이 없으면 null.
        /// </summary>
        private VisualElement LootRow(string locationId, bool dayOpen, bool nightOpen)
        {
            if (!_session.Definitions.TryGetExpeditionModel(locationId, out var model))
            {
                return null;
            }

            var ranges = new List<ResourceRange>();
            if (dayOpen) ranges.AddRange(model.Day.Resources);
            if (nightOpen) ranges.AddRange(model.Night.Resources);
            var resources = ranges.Where(r => r.Max > 0)
                .GroupBy(r => r.Resource)
                .Select(g => (Resource: g.Key, Max: g.Max(r => r.Max), Sure: g.Any(r => r.Min > 0)))
                .OrderByDescending(r => r.Max)
                .ToList();
            if (resources.Count == 0)
            {
                return null;
            }

            var row = new VisualElement();
            row.AddToClassList("action-loot");
            row.Add(Line(Loc.T("palette.loot"), "action-loot-head"));
            foreach (var (resource, _, sure) in resources)
            {
                // 괄호 설명은 칩에서 뺀다 (빗물(정수 필요) → 빗물)
                var resourceName = UiText.Resource(resource);
                var paren = resourceName.IndexOf('(');
                var chip = Line(paren > 0 ? resourceName.Substring(0, paren) : resourceName, "loot-chip");
                StoryArt.SetIcon(chip, StoryArt.Icon("res." + resource), 18f);
                chip.EnableInClassList("rare", !sure);
                chip.tooltip = sure ? "" : Loc.T("palette.loot_rare");
                row.Add(chip);
            }

            return row;
        }

        /// <summary>
        /// 카드 끌기: 누른 채 조금 움직이면 끌기가 시작되고, 계획 칸 위에서 놓으면 그 칸에 들어간다.
        /// 단추의 누르기 처리보다 먼저 받도록 내려가는 단계(TrickleDown)에 건다 — 단추가 포인터를 잡고 있어 움직임·떼기는 카드로 온다.
        /// </summary>
        private void BindDrag(Button card, ActionDef action, bool locationRun)
        {
            card.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == 0 && _session.IsPlanning)
                {
                    _dragPointer = evt.pointerId;
                    _dragFrom = evt.position;
                    _cardDragging = false;
                    _suppressClick = false;
                }
            }, TrickleDown.TrickleDown);

            card.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (_dragPointer != evt.pointerId)
                {
                    return;
                }

                if (!_cardDragging)
                {
                    if (((UnityEngine.Vector2)evt.position - _dragFrom).magnitude < CardDragThreshold || DragBegan == null)
                    {
                        return;
                    }

                    _cardDragging = true;
                    Func<DayPhase, string> actionFor;
                    if (locationRun)
                    {
                        actionFor = phase => Sibling(action, phase)?.Id;
                    }
                    else
                    {
                        actionFor = phase => action.Id;
                    }

                    var icon = StoryArt.Icon("act." + action.Id) ?? StoryArt.Icon("cat." + action.Category);
                    DragBegan(actionFor, icon, Loc.Action(action.Id, "short"));
                }

                DragMoved?.Invoke(evt.position);
            }, TrickleDown.TrickleDown);

            card.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (_dragPointer != evt.pointerId)
                {
                    return;
                }

                _dragPointer = -1;
                if (_cardDragging)
                {
                    _cardDragging = false;
                    // 카드 위에서 놓으면 단추가 누르기로 받아들이니 한 번 걸러 낸다
                    _suppressClick = card.worldBound.Contains(evt.position);
                    DragEnded?.Invoke(evt.position);
                }
            }, TrickleDown.TrickleDown);

            card.RegisterCallback<PointerCaptureOutEvent>(_ =>
            {
                if (_cardDragging)
                {
                    _cardDragging = false;
                    _dragPointer = -1;
                    DragEnded?.Invoke(new UnityEngine.Vector2(-1f, -1f));
                }
            });
        }

        private static Label Line(string text, string cssClass)
        {
            var label = new Label(text);
            label.AddToClassList(cssClass);
            return label;
        }
    }

    /// <summary>
    /// 하단 주간 플래너: 7일 × 낮/밤. 손질 (Docs/TEACHER_PLAN_PROPOSAL.md 5절):
    /// 누르기 = 고른 행동 넣기(빈손이면 그 칸 행동 집기), 끌기 = 옮기기·바꾸기, 오른쪽 = 비우기,
    /// 칸 오른쪽 위 "고정" 꼬리표 = 고정 시간표 켜고 끄기. 선생님이 넣은 칸은 분필 밑줄.
    /// </summary>
    internal sealed class PlannerView
    {
        private const float DragThreshold = 8f;
        private const float GhostHalf = 30f; // 끄는 아이콘 판 반지름 (Theme.uss .slot-drag-ghost 60px)

        private readonly GameSession _session;
        private readonly Func<DayPhase, string> _selectedAction;
        private readonly Action<string> _notify;
        private readonly VisualElement _planner;
        private readonly VisualElement[] _cells = new VisualElement[WeekPlan.SlotCount];
        private readonly VisualElement[] _icons = new VisualElement[WeekPlan.SlotCount];
        private readonly Label[] _labels = new Label[WeekPlan.SlotCount];
        private readonly Label[] _pins = new Label[WeekPlan.SlotCount];

        private int _press = -1;
        private UnityEngine.Vector2 _pressAt;
        private bool _dragging;
        private int _dropTarget = -1;
        private string[][] _dropIds;
        private VisualElement _dragGhost;

        /// <summary>빈손으로 찬 칸을 누르면 그 행동을 팔레트에서 고른다 (스포이트).</summary>
        public Action<string> Picked;

        /// <summary>마우스가 올라간 칸 (-1 = 나감).</summary>
        public Action<int> Hovered;

        /// <summary>놓으면 바뀔 계획 (흐름 띠 유령 비교, null = 끔).</summary>
        public Action<IList<string>> GhostChanged;

        /// <summary>고정 시간표 (칸 → 값). 꼬리표를 누르면 TogglePin.</summary>
        public Func<IDictionary<int, string>> Pins;
        public Action<int> TogglePin;

        /// <param name="selectedAction">그 시간대 칸에 놓을 행동 (없으면 "", 그 시간대에 열리지 않은 지역 탐험이면 null).</param>
        public PlannerView(VisualElement root, GameSession session, Func<DayPhase, string> selectedAction, Action<string> notify)
        {
            _session = session;
            _selectedAction = selectedAction;
            _notify = notify;
            _planner = root.Q("planner");

            var header = new VisualElement();
            header.AddToClassList("planner-row");
            header.AddToClassList("planner-header");
            header.Add(Corner());
            for (var day = 1; day <= CalendarState.DaysPerWeek; day++)
            {
                var label = new Label();
                label.AddToClassList("planner-day");
                PlanLook.Weekday(label, day, session.Definitions.Balance);
                header.Add(label);
            }

            _planner.Add(header);
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
                    var cell = new VisualElement();
                    cell.AddToClassList("slot");
                    cell.AddToClassList(phase == DayPhase.Day ? "slot-day" : "slot-night");
                    cell.RegisterCallback<PointerDownEvent>(evt => OnDown(index, evt));
                    cell.RegisterCallback<PointerMoveEvent>(evt => OnMove(index, evt));
                    cell.RegisterCallback<PointerUpEvent>(evt => OnUp(index, evt));
                    cell.RegisterCallback<PointerEnterEvent>(_ => OnEnter(index));
                    cell.RegisterCallback<PointerLeaveEvent>(_ => OnLeave(index));

                    // 칸 안: 위에 행동 아이콘, 아래에 이름, 바닥에 분필 밑줄, 오른쪽 위 고정 꼬리표
                    var icon = new VisualElement { pickingMode = PickingMode.Ignore };
                    icon.AddToClassList("slot-icon");
                    var label = new Label { pickingMode = PickingMode.Ignore };
                    label.AddToClassList("slot-label");
                    var chalk = new VisualElement { pickingMode = PickingMode.Ignore };
                    chalk.AddToClassList("slot-chalk");
                    var pin = new Label(Loc.T("teacher.pin_tag"));
                    pin.AddToClassList("slot-pin");
                    pin.RegisterCallback<PointerDownEvent>(evt =>
                    {
                        evt.StopPropagation();
                        if (evt.button == 0 && _session.IsPlanning)
                        {
                            TogglePin?.Invoke(index);
                        }
                    });
                    cell.Add(icon);
                    cell.Add(label);
                    cell.Add(chalk);
                    cell.Add(pin);
                    cell.RegisterCallback<GeometryChangedEvent>(_ => FitLabel(label, cell));   // 칸 크기가 정해지면 글꼴을 다시 맞춘다
                    _cells[index] = cell;
                    _icons[index] = icon;
                    _labels[index] = label;
                    _pins[index] = pin;
                    row.Add(cell);
                }

                _planner.Add(row);
            }
        }

        public void Refresh(ICollection<int> warnedSlots)
        {
            var state = _session.State;
            var calendar = state.Calendar;
            var current = calendar.WeekComplete ? WeekPlan.SlotCount : CalendarRules.SlotIndex(calendar.Day, calendar.Phase);
            var executing = _session.IsExecuting;
            var planning = _session.IsPlanning;
            var pins = Pins?.Invoke();

            for (var i = 0; i < _cells.Length; i++)
            {
                var slot = state.Plan.Slots[i];
                var cell = _cells[i];
                var empty = string.IsNullOrEmpty(slot.ActionId);
                var pinned = pins != null && pins.ContainsKey(i);
                var text = empty ? Loc.T("planner.empty") : Loc.Action(slot.ActionId, "short");
                if (empty && pinned && planning)
                {
                    // 비어 있는 고정 칸: 선생님 추천을 누르면 들어갈 것
                    text = Loc.F("teacher.pin_empty", PinName(pins[i]));
                }

                // 칸에는 아이콘과 이름만 (잠김·강제 휴식·설명은 오른쪽 칸 설명에 — 칸 크기가 흔들리지 않게)
                if (empty && slot.Locked)
                {
                    text = Loc.T("planner.locked");
                }
                else if (empty && slot.ForcedRest)
                {
                    text = Loc.T("planner.forced");
                }

                _labels[i].userData = text;
                cell.EnableInClassList("empty", empty);
                FitLabel(_labels[i], cell);
                var iconTexture = empty ? null : SlotIcon(slot);
                StoryArt.SetContain(_icons[i], iconTexture);
                _icons[i].style.display = iconTexture == null ? DisplayStyle.None : DisplayStyle.Flex;
                cell.EnableInClassList("empty", empty);
                cell.EnableInClassList("locked", slot.Locked);
                cell.EnableInClassList("forced", slot.ForcedRest && !slot.Locked);
                cell.EnableInClassList("warned", warnedSlots.Contains(i));
                cell.EnableInClassList("done", executing && i < current);
                cell.EnableInClassList("current", executing && i == current);
                cell.EnableInClassList("teacher", planning && _session.IsTeacherSlot(i) && !slot.Locked);
                cell.EnableInClassList("pinned", planning && pinned);
                _pins[i].EnableInClassList("on", pinned);
                _pins[i].style.display = planning && !slot.Locked ? DisplayStyle.Flex : DisplayStyle.None;
                cell.SetEnabled(planning);
            }
        }

        /// <summary>칸 이름 글꼴 크기 후보 (큰 것부터). 이름이 길면 칸 크기는 그대로 두고 글꼴을 줄인다.</summary>
        private static readonly int[] LabelSizes = { 17, 15, 13, 12, 11 };

        /// <summary>글자 폭 어림: 한글 한 자 = 글꼴 크기 1, 그 밖의 글자·빈칸 = 0.55.</summary>
        private static float TextUnits(string text)
        {
            var units = 0f;
            foreach (var c in text)
            {
                units += c >= 0x1100 ? 1f : 0.55f;
            }

            return units;
        }

        /// <summary>
        /// 이름(label.userData)이 칸 안에 들어가게 글꼴을 고른다. 칸 크기는 바꾸지 않는다.
        /// ① 한 줄에 들어가면 그대로(15px까지), ② 낱말이 둘 이상이면 가운데 빈칸에서 두 줄로 나누고 들어가는 가장 큰 글꼴,
        /// ③ 그래도 안 되면 한 줄에 들어가는 가장 큰 글꼴. 칸 크기를 아직 모르면(첫 배치 전) 기본 크기로 두고 배치 뒤 다시 맞춘다.
        /// </summary>
        private static void FitLabel(Label label, VisualElement cell)
        {
            var text = label.userData as string ?? label.text ?? "";
            var width = cell.resolvedStyle.width;
            if (float.IsNaN(width) || width <= 20f)
            {
                label.text = text;
                label.style.fontSize = LabelSizes[0];
                return;
            }

            var room = Math.Max(20f, width - 12f);
            var units = TextUnits(text);
            foreach (var size in LabelSizes)
            {
                if (size >= 15 && units * size <= room)
                {
                    label.text = text;
                    label.style.fontSize = size;
                    return;
                }
            }

            // 두 줄: 가운데에 가장 가까운 빈칸에서 나눈다 (한글은 아무 데서나 꺾여 낱말이 쪼개지므로 직접 줄을 바꾼다)
            var cut = -1;
            for (var i = 1; i < text.Length - 1; i++)
            {
                if (text[i] == ' ' && (cut < 0 || Math.Abs(i - text.Length / 2f) < Math.Abs(cut - text.Length / 2f)))
                {
                    cut = i;
                }
            }

            if (cut > 0)
            {
                var first = text.Substring(0, cut);
                var second = text.Substring(cut + 1);
                var longest = Math.Max(TextUnits(first), TextUnits(second));
                foreach (var size in LabelSizes)
                {
                    if (longest * size <= room)
                    {
                        label.text = first + (char)10 + second;
                        label.style.fontSize = size;
                        return;
                    }
                }
            }

            foreach (var size in LabelSizes)
            {
                if (units * size <= room)
                {
                    label.text = text;
                    label.style.fontSize = size;
                    return;
                }
            }

            // 가장 작은 글꼴로도 한 줄에 안 들어가면 두 줄로 흘린다 (칸은 넘치는 글을 잘라 낸다)
            label.text = text;
            label.style.fontSize = LabelSizes[LabelSizes.Length - 1];
        }

        /// <summary>선생님 추천 직후: 선생님 칸을 앞에서부터 한 칸씩 분필로 채운다 (칸마다 40ms, 전부 0.6초 안).</summary>
        public void PlayChalk(IReadOnlyList<bool> placed)
        {
            var order = Enumerable.Range(0, _cells.Length).Where(i => placed[i]).ToList();
            if (order.Count == 0)
            {
                return;
            }

            var step = Math.Min(40, 600 / order.Count);
            foreach (var i in order)
            {
                _cells[i].AddToClassList("chalk-pre");
            }

            for (var k = 0; k < order.Count; k++)
            {
                var cell = _cells[order[k]];
                cell.schedule.Execute(() => cell.RemoveFromClassList("chalk-pre")).ExecuteLater(30 + k * step);
            }
        }

        private string PinName(string token)
        {
            return token == TeacherPins.ClubToken ? Loc.T("teacher.pin_club") : Loc.Action(token, "short");
        }

        /// <summary>칸 아이콘: 강제 휴식 → 행동 아이콘 → 행동 분류 아이콘 (도크 칸과 같은 순서).</summary>
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

        // ── 누르기·끌기 ──

        private void OnDown(int index, PointerDownEvent evt)
        {
            if (!_session.IsPlanning)
            {
                return;
            }

            if (evt.button == 1)
            {
                OnClear(index);
                return;
            }

            if (evt.button != 0)
            {
                return;
            }

            _press = index;
            _pressAt = evt.position;
            _dragging = false;
            _cells[index].CapturePointer(evt.pointerId);
        }

        private void OnMove(int index, PointerMoveEvent evt)
        {
            if (_press < 0 || !_cells[_press].HasPointerCapture(evt.pointerId))
            {
                return;
            }

            if (!_dragging)
            {
                var slot = _session.State.Plan.Slots[_press];
                if (((UnityEngine.Vector2)evt.position - _pressAt).magnitude < DragThreshold || string.IsNullOrEmpty(slot.ActionId) || slot.Locked)
                {
                    return;
                }

                BeginDrag();
            }

            // 끄는 그림은 마우스를 따라가고, 아래 칸을 놓을 곳으로 본다
            var local = _planner.WorldToLocal(evt.position);
            _dragGhost.style.left = local.x - GhostHalf;
            _dragGhost.style.top = local.y - GhostHalf;
            var target = Array.FindIndex(_cells, c => c.worldBound.Contains(evt.position));
            if (target != _dropTarget)
            {
                if (_dropTarget >= 0) _cells[_dropTarget].RemoveFromClassList("drop-target");
                _dropTarget = target;
                if (target >= 0) _cells[target].AddToClassList("drop-target");
                GhostChanged?.Invoke(target >= 0 && target != _press ? _dropIds[target] : null);
            }
        }

        private void OnUp(int index, PointerUpEvent evt)
        {
            if (_press < 0)
            {
                return;
            }

            var from = _press;
            if (_cells[from].HasPointerCapture(evt.pointerId))
            {
                _cells[from].ReleasePointer(evt.pointerId);
            }

            _press = -1;
            if (_dragging)
            {
                var to = _dropTarget;
                EndDrag();
                if (to >= 0 && to != from)
                {
                    var moved = _session.MoveSlot(from, to);
                    if (moved == PlanEditResult.Ok)
                    {
                        Audio.Sound.Play("Sfx/ui_slot_place", Audio.SoundCategory.Ui);
                    }

                    Report(moved, to);
                }

                return;
            }

            if (_cells[from].worldBound.Contains(evt.position))
            {
                OnClick(from);
            }
        }

        private void BeginDrag()
        {
            _dragging = true;
            var actionId = _session.State.Plan.Slots[_press].ActionId;
            _dropIds = new string[_cells.Length][];
            for (var i = 0; i < _cells.Length; i++)
            {
                _dropIds[i] = i == _press ? null : _session.PreviewIds(_press, i, null);
                _cells[i].EnableInClassList("drop-ok", i != _press && _dropIds[i] != null);
                _cells[i].EnableInClassList("drop-bad", i != _press && _dropIds[i] == null && !_session.State.Plan.Slots[i].Locked);
            }

            _cells[_press].AddToClassList("drag-source");

            // 끄는 그림: 행동 아이콘 (아이콘이 없는 행동만 짧은 이름)
            _dragGhost = new VisualElement { pickingMode = PickingMode.Ignore };
            _dragGhost.AddToClassList("slot-drag-ghost");
            var icon = SlotIcon(_session.State.Plan.Slots[_press]);
            if (icon != null)
            {
                var image = new VisualElement { pickingMode = PickingMode.Ignore };
                image.AddToClassList("slot-drag-icon");
                StoryArt.SetContain(image, icon);
                _dragGhost.Add(image);
            }
            else
            {
                _dragGhost.AddToClassList("text");
                _dragGhost.Add(new Label(Loc.Action(actionId, "short")) { pickingMode = PickingMode.Ignore });
            }

            _planner.Add(_dragGhost);
        }

        // ── 팔레트 카드를 끌어다 놓기 (PaletteView.DragBegan·DragMoved·DragEnded) ──

        private Func<DayPhase, string> _cardAction;

        /// <summary>카드 끌기 시작: 놓을 수 있는 칸을 초록으로, 못 놓는 칸을 흐리게, 끄는 그림은 화면 맨 위 겹에.</summary>
        public void BeginCardDrag(Func<DayPhase, string> actionFor, UnityEngine.Texture2D icon, string label)
        {
            if (!_session.IsPlanning || _dragging)
            {
                return;
            }

            _dragging = true;
            _press = -1;
            _cardAction = actionFor;
            _dropIds = new string[_cells.Length][];
            for (var i = 0; i < _cells.Length; i++)
            {
                var id = actionFor(CalendarRules.PhaseOf(i));
                _dropIds[i] = string.IsNullOrEmpty(id) ? null : _session.PreviewIds(-1, i, id);
                _cells[i].EnableInClassList("drop-ok", _dropIds[i] != null);
                _cells[i].EnableInClassList("drop-bad", _dropIds[i] == null && !_session.State.Plan.Slots[i].Locked);
            }

            _dragGhost = new VisualElement { pickingMode = PickingMode.Ignore };
            _dragGhost.AddToClassList("slot-drag-ghost");
            if (icon != null)
            {
                var image = new VisualElement { pickingMode = PickingMode.Ignore };
                image.AddToClassList("slot-drag-icon");
                StoryArt.SetContain(image, icon);
                _dragGhost.Add(image);
            }
            else
            {
                _dragGhost.AddToClassList("text");
                _dragGhost.Add(new Label(label) { pickingMode = PickingMode.Ignore });
            }

            // 카드 쪽(플래너 밖)에서도 보이게 화면 전체 겹에 올린다
            GhostHost().Add(_dragGhost);
        }

        public void MoveCardDrag(UnityEngine.Vector2 position)
        {
            if (!_dragging || _cardAction == null || _dragGhost == null)
            {
                return;
            }

            var local = _dragGhost.parent.WorldToLocal(position);
            _dragGhost.style.left = local.x - GhostHalf;
            _dragGhost.style.top = local.y - GhostHalf;
            var target = Array.FindIndex(_cells, c => c.worldBound.Contains(position));
            if (target != _dropTarget)
            {
                if (_dropTarget >= 0) _cells[_dropTarget].RemoveFromClassList("drop-target");
                _dropTarget = target;
                if (target >= 0) _cells[target].AddToClassList("drop-target");
                GhostChanged?.Invoke(target >= 0 ? _dropIds[target] : null);
            }
        }

        /// <summary>카드를 놓았다. 칸 위였으면 그 칸에 넣고 true (못 넣으면 까닭을 알린다).</summary>
        public bool EndCardDrag(UnityEngine.Vector2 position)
        {
            if (!_dragging || _cardAction == null)
            {
                return false;
            }

            var actionFor = _cardAction;
            var to = Array.FindIndex(_cells, c => c.worldBound.Contains(position));
            _cardAction = null;
            EndDrag();
            if (to < 0 || !_session.IsPlanning)
            {
                return false;
            }

            var phase = CalendarRules.PhaseOf(to);
            var actionId = actionFor(phase);
            if (string.IsNullOrEmpty(actionId))
            {
                // 지역 카드인데 그 시간대 탐험이 아직 열리지 않았다
                _notify(Loc.T(phase == DayPhase.Night ? "plan.expedition_night_locked" : "plan.expedition_day_locked"));
                return true;
            }

            var placed = _session.SetSlot(to, actionId);
            if (placed == PlanEditResult.Ok)
            {
                Audio.Sound.Play("Sfx/ui_slot_place", Audio.SoundCategory.Ui);
            }

            Report(placed, to);
            return true;
        }

        private VisualElement GhostHost()
        {
            var host = _planner;
            while (host.parent != null && host.name != "screen")
            {
                host = host.parent;
            }

            return host;
        }

        private void EndDrag()
        {
            _dragging = false;
            _dropTarget = -1;
            _dropIds = null;
            _dragGhost?.RemoveFromHierarchy();
            _dragGhost = null;
            foreach (var cell in _cells)
            {
                cell.RemoveFromClassList("drop-ok");
                cell.RemoveFromClassList("drop-bad");
                cell.RemoveFromClassList("drop-target");
                cell.RemoveFromClassList("drag-source");
            }

            GhostChanged?.Invoke(null);
        }

        private void OnEnter(int index)
        {
            if (_dragging)
            {
                return;
            }

            Hovered?.Invoke(index);

            // 고른 행동이 있으면 놓았을 때의 흐름을 미리 보여 준다
            var actionId = _session.IsPlanning ? _selectedAction(CalendarRules.PhaseOf(index)) : null;
            if (!string.IsNullOrEmpty(actionId) && _session.State.Plan.Slots[index].ActionId != actionId)
            {
                GhostChanged?.Invoke(_session.PreviewIds(-1, index, actionId));
            }
        }

        private void OnLeave(int index)
        {
            if (_dragging)
            {
                return;
            }

            Hovered?.Invoke(-1);
            GhostChanged?.Invoke(null);
        }

        private void OnClick(int index)
        {
            var phase = CalendarRules.PhaseOf(index);
            var actionId = _selectedAction(phase);
            if (actionId == null)
            {
                // 지역 카드인데 그 시간대 탐험이 아직 열리지 않았다
                _notify(Loc.T(phase == DayPhase.Night ? "plan.expedition_night_locked" : "plan.expedition_day_locked"));
                return;
            }

            if (actionId.Length == 0)
            {
                // 빈손: 찬 칸이면 그 행동을 집는다 (스포이트)
                var slot = _session.State.Plan.Slots[index];
                if (!string.IsNullOrEmpty(slot.ActionId) && !slot.Locked && slot.ActionId != _session.Definitions.Balance.CraftContinueActionId)
                {
                    Picked?.Invoke(slot.ActionId);
                    return;
                }

                _notify(Loc.T("plan.select_first"));
                return;
            }

            var placed = _session.SetSlot(index, actionId);
            if (placed == PlanEditResult.Ok)
            {
                Audio.Sound.Play("Sfx/ui_slot_place", Audio.SoundCategory.Ui);
            }

            Report(placed, index);
        }

        private void OnClear(int index)
        {
            if (_session.IsPlanning)
            {
                Report(_session.ClearSlot(index), index);
            }
        }

        private void Report(PlanEditResult result, int index)
        {
            switch (result)
            {
                case PlanEditResult.Locked:
                    // 제작이 미리 잠근 칸이면 푸는 법을 알려 준다
                    var craftLocked = _session.HasGame && index < _session.State.Plan.Slots.Count && _session.State.Plan.Slots[index].CraftLocked;
                    _notify(Loc.T(craftLocked ? "plan.edit_craft_locked" : "plan.edit_locked"));
                    break;
                case PlanEditResult.CraftNoRoom:
                    _notify(Loc.T("plan.edit_craft_no_room"));
                    break;
                case PlanEditResult.WrongSlot:
                    _notify(Loc.T("plan.edit_wrong_slot"));
                    break;
                case PlanEditResult.WeeklyLimit:
                    _notify(Loc.T("plan.edit_weekly_limit"));
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

    /// <summary>플래너 옆: 선생님 메모, 마우스가 올라간 칸 설명, 경고, 예상 수면 부채.</summary>
    internal sealed class PlanSideView
    {
        private readonly GameSession _session;
        private readonly ScrollView _warnings;
        private readonly Label _forecast;
        private readonly VisualElement _memo;
        private readonly Label _detail;

        public PlanSideView(VisualElement root, GameSession session)
        {
            _session = session;
            _warnings = root.Q<ScrollView>("warnings");
            _forecast = root.Q<Label>("forecast");
            _memo = root.Q("teacher-memo");
            _detail = root.Q<Label>("slot-detail");
        }

        /// <summary>칸 설명 (Docs/TEACHER_PLAN_PROPOSAL.md 6.3 칸 꼬리표): 무엇·왜(선생님)·예상 피로·스트레스·능력치. -1이면 안내 한 줄.</summary>
        public void ShowSlot(int slot, WeekProjection projection)
        {
            if (!_session.IsPlanning || slot < 0 || projection == null)
            {
                _detail.text = _session.IsPlanning ? Loc.T("plan.slot_hint") : "";
                _detail.RemoveFromClassList("on");
                return;
            }

            var balance = _session.Definitions.Balance;
            var planned = _session.State.Plan.Slots[slot];
            var p = projection.Slots[slot];
            var beforeFatigue = slot == 0 ? projection.StartFatigue : projection.Slots[slot - 1].Fatigue;
            var beforeStress = slot == 0 ? projection.StartStress : projection.Slots[slot - 1].Stress;
            var lines = new List<string>
            {
                Loc.F("plan.slot_title", Loc.T("weekday." + CalendarRules.Weekday(CalendarRules.DayOf(slot), balance)), UiText.Phase(CalendarRules.PhaseOf(slot)),
                    string.IsNullOrEmpty(planned.ActionId) ? Loc.T("plan.slot_empty_rest") : Loc.Action(planned.ActionId)),
            };
            if (planned.Locked)
            {
                lines.Add(Loc.T("plan.slot_locked"));
            }
            else if (planned.ForcedRest)
            {
                lines.Add(Loc.T("plan.slot_forced"));
            }

            // 행동 설명 (있을 때만): 칸에는 이름만 쓰고 설명은 여기에
            var about = string.IsNullOrEmpty(planned.ActionId) ? null : Loc.Lookup(Loc.ActionTable, planned.ActionId + ".desc");
            if (!string.IsNullOrEmpty(about))
            {
                lines.Add(about);
            }

            var reason = _session.IsTeacherSlot(slot) ? _session.TeacherReason(slot) : null;
            if (reason != null)
            {
                lines.Add(Loc.F("plan.slot_teacher", TeacherText.Note(reason, balance)));
            }

            lines.Add(Loc.F(p.Uncertain ? "plan.slot_flow_rough" : "plan.slot_flow", beforeFatigue, p.Fatigue, beforeStress, p.Stress, p.SleepDebt));
            var gains = Enumerable.Range(0, WeekForecast.StatCount).Where(s => p.Gains[s] >= 0.05f)
                .Select(s => UiText.Stat((StatType)s) + " +" + p.Gains[s].ToString("0.#")).ToList();
            if (gains.Count > 0)
            {
                lines.Add(string.Join(" · ", gains));
            }

            _detail.text = string.Join("\n", lines);
            _detail.AddToClassList("on");
        }

        private void RefreshMemo()
        {
            _memo.Clear();
            var notes = _session.IsPlanning ? _session.TeacherNotes : null;
            _memo.EnableInClassList("hidden", notes == null || notes.Count == 0);
            if (notes == null || notes.Count == 0)
            {
                return;
            }

            // 머리: 선생님 뱃지 + "선생님 메모"
            var head = new VisualElement();
            head.AddToClassList("teacher-memo-head");
            head.Add(PeopleUi.Badge(ApoGirl.Core.Rules.GiveRules.TeacherId, 30f));
            var title = new Label(Loc.T("teacher.memo_title"));
            title.AddToClassList("teacher-memo-title");
            head.Add(title);
            _memo.Add(head);
            var balance = _session.Definitions.Balance;
            foreach (var note in notes)
            {
                var where = note.Slot >= 0
                    ? Loc.T("weekday." + CalendarRules.Weekday(CalendarRules.DayOf(note.Slot), balance)) + UiText.Phase(CalendarRules.PhaseOf(note.Slot)) + " · "
                    : "";
                var line = new Label(where + TeacherText.Note(note, balance));
                line.AddToClassList("teacher-memo-line");
                _memo.Add(line);
            }
        }

        /// <returns>경고가 걸린 슬롯 번호.</returns>
        public HashSet<int> Refresh()
        {
            var warned = new HashSet<int>();
            _warnings.Clear();
            RefreshMemo();
            if (!_session.IsPlanning)
            {
                _forecast.text = "";
                _detail.text = "";
                return warned;
            }

            if (!_detail.ClassListContains("on"))
            {
                _detail.text = Loc.T("plan.slot_hint");
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
            if (OutingRules.Plan(_session.State, _session.Definitions).Count > 0)
            {
                // 동료 탐험: 미리보기는 지금 상태 기준이라 외출하는 동료가 있으면 실제와 달라질 수 있다
                _forecast.text += "\n" + Loc.T("plan.outing_note");
            }
            _forecast.EnableInClassList("danger", end >= 3);
            return warned;
        }
    }
}
