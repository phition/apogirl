using System;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;
using ApoGirl.Presentation.Art;
using ApoGirl.Presentation.Text;
using ApoGirl.Systems.Session;
using UnityEngine;
using UnityEngine.UIElements;

namespace ApoGirl.Presentation.Screens
{
    /// <summary>
    /// 거점 상황판 (Docs/BASE_TAB_PROPOSAL.md P1): 오른쪽 거점 서랍 맨 위에 알림·안전·식량과 물·전력 칸.
    /// 값은 BaseForecast가 읽기만 해서 낸다. 칸 머리를 누르면 접고 펴고(PlayerPrefs), 행동 단추는 계획 보드에서 그 행동을 고른다.
    /// </summary>
    internal sealed class BaseBoardView
    {
        private const string FoldPrefix = "ApoGirl.BaseBoard.Fold.";

        // 안전 칸에서 권할 수 있는 거점 방어 행동 (차례대로, 할 수 있는 것만 최대 MaxActions개)
        private const string Barricade = "ACT_SURV_BARRICADE";
        private const string Reinforce = "ACT_SURV_REINFORCE";
        private const string Trap = "ACT_SURV_TRAP";
        private const string Alarm = "ACT_SURV_ALARM";
        private const string Covers = "ACT_SURV_COVERS";
        private const string NightWatch = "ACT_NIGHT_WATCH";
        private const string Scarecrow = "ACT_CRAFT_SCARECROW";
        private const string Purify = "ACT_SURV_PURIFY_WATER";
        private const string Repair = "ACT_SURV_REPAIR_GENERATOR";
        private const string Upgrade = "ACT_SURV_UPGRADE_POWER";
        private const int MaxActions = 3;

        private readonly GameSession _session;
        private readonly VisualElement _host = new VisualElement();
        private string _seen = "";

        public BaseBoardView(VisualElement root, GameSession session)
        {
            _session = session;
            _host.AddToClassList("base-board");
            var panel = root.Q("base-panel");
            panel?.Insert(0, _host);
        }

        /// <summary>행동 단추를 눌렀을 때 (허브가 계획 보드를 그 행동으로 연다).</summary>
        public Action<string> ActionPicked { get; set; }

        /// <summary>지금 알림 (경고 점용). Refresh 때 새로 센다.</summary>
        public IReadOnlyList<string> Alerts { get; private set; } = Array.Empty<string>();

        /// <summary>서랍을 열어 보지 않은 새 알림이 있다 (거점 단추의 빨간 점).</summary>
        public bool HasUnseenAlert => Alerts.Count > 0 && Signature() != _seen;

        /// <summary>서랍을 열었다: 지금 알림을 본 것으로. 다음 주가 되거나 새 알림이 생기면 다시 점이 뜬다.</summary>
        public void MarkSeen() => _seen = Signature();

        private string Signature() => _session.State.Calendar.Week + ":" + string.Join(",", Alerts);

        public void Refresh()
        {
            var state = _session.State;
            if (state == null)
            {
                return;
            }

            var db = _session.Definitions;
            var raid = BaseForecast.Raid(state, db);
            var food = BaseForecast.Food(state, db);
            var water = BaseForecast.Water(state, db);
            var power = BaseForecast.Power(state, db.Balance);
            var showSafety = Disclosure.Has(state, "UI_DEFENSE");
            var showPower = power.Restored || power.Capacity > 0;
            var alerts = BaseForecast.Alerts(state, db, showSafety ? raid : null, food, water);
            if (!showPower)
            {
                alerts.Remove("generator");
            }

            Alerts = alerts;

            _host.Clear();
            if (alerts.Count > 0)
            {
                var band = new VisualElement();
                band.AddToClassList("base-alerts");
                foreach (var key in alerts)
                {
                    var line = new Label(Loc.T("base.alert." + key));
                    line.AddToClassList("base-alert");
                    band.Add(line);
                }

                _host.Add(band);
            }

            if (showSafety)
            {
                Safety(state, raid);
            }

            Supply(state, food, water);
            if (showPower)
            {
                PowerCard(power);
            }

            if (state.Base.Facilities.Any(f => f.Value > 0))
            {
                RoomsCard(state);
            }

            KoreanFont.ApplyDeep(_host);
        }

        // ── 시설 (P2) ──

        /// <summary>시설 줄에 마우스를 올렸다·뗐다 (null) — 허브가 단면도에서 그 방을 빛낸다.</summary>
        public Action<string> RoomHovered { get; set; }

        /// <summary>시설 줄을 눌렀다 — 허브가 서랍을 닫고 단면도에서 그 방을 보여 준다.</summary>
        public Action<string> RoomPicked { get; set; }

        private HubSectionConfig _section;

        /// <summary>단면도의 방마다 한 줄: 연 방은 효과(없으면 쓰임), 잠긴 방은 "?"와 여는 방법. 연 방이 먼저.</summary>
        private void RoomsCard(GameState state)
        {
            _section ??= HubSectionConfig.Load();
            var rooms = _section.Rooms;
            var open = rooms.Where(r => HubSectionConfig.IsRoomOpen(state, r)).ToList();
            var body = Card("rooms", "base.rooms", "menu.school", Loc.F("base.rooms.count", open.Count, rooms.Length), "ok");
            if (body == null)
            {
                return;
            }

            var db = _session.Definitions;
            foreach (var room in open.Concat(rooms.Where(r => !open.Contains(r))))
            {
                var isOpen = open.Contains(room);
                var roomId = room.Id;
                var row = new Button(() => RoomPicked?.Invoke(roomId)) { tooltip = Loc.T("base.rooms.hint") };
                row.AddToClassList("base-room");
                row.EnableInClassList("locked", !isOpen);
                var name = new Label(isOpen ? HubSectionConfig.RoomName(room) : Loc.F("base.rooms.locked", HubSectionConfig.RoomName(room)));
                name.AddToClassList("base-room-name");
                row.Add(name);
                var text = new Label(isOpen ? RoomText.Summary(db, room) : RoomText.Hint(room));
                text.AddToClassList("base-room-text");
                row.Add(text);
                row.RegisterCallback<PointerEnterEvent>(_ => RoomHovered?.Invoke(roomId));
                row.RegisterCallback<PointerLeaveEvent>(_ => RoomHovered?.Invoke(null));
                body.Add(row);
            }
        }

        // ── 안전 ──

        private void Safety(GameState state, RaidForecast raid)
        {
            var balance = _session.Definitions.Balance;
            string verdict;
            string tone;
            if (raid.NotYet)
            {
                verdict = Loc.T("base.raid.not_yet");
                tone = "ok";
            }
            else if (raid.Level == RiskLevel.None)
            {
                verdict = Loc.T("base.raid.none");
                tone = "ok";
            }
            else
            {
                verdict = Loc.F("base.raid.level", Loc.T("base.risk." + raid.Level));
                tone = Tone(raid.Level);
            }

            var body = Card("safety", "base.safety", "hub.defense", verdict, tone);
            if (body == null)
            {
                return;
            }

            Bar(body, Loc.F("base.defense", state.Base.Defense, raid.DefenseMax), state.Base.Defense, raid.DefenseMax, "defense");
            Bar(body, Loc.F("base.threat", state.Base.Threat), state.Base.Threat, balance.ThreatMax, "threat");
            if (!raid.NotYet)
            {
                var repel = Line(body, Loc.F("base.repel", Loc.T("base.repel." + raid.Repel)));
                repel.AddToClassList(Tone(raid.Repel));
                foreach (var reason in raid.Reasons)
                {
                    var line = Line(body, "· " + Loc.F("base.why." + reason.Key, reason.Value));
                    line.AddToClassList(reason.Good ? "good" : "bad");
                }
            }

            // 함정 ●●○ · 경보 · 허수아비
            var parts = new List<string>
            {
                Loc.F("base.traps", Dots(raid.Traps, raid.MaxTraps, "●", "○")),
                Loc.T(raid.Alarm ? "base.alarm.on" : "base.alarm.off"),
            };
            if (raid.Scarecrow > 0)
            {
                parts.Add(Loc.F("base.scarecrow", Dots(raid.Scarecrow, Math.Max(raid.Scarecrow, raid.ScarecrowMax), "▮", "▯")));
            }
            else if (raid.ScarecrowDown)
            {
                parts.Add(Loc.T("base.scarecrow.down"));
            }

            Line(body, string.Join(" · ", parts)).AddToClassList("dim");

            if (raid.IntrusionChance > 0f)
            {
                var level = raid.IntrusionChance >= balance.BaseBoardRaidHigh ? RiskLevel.High
                    : raid.IntrusionChance >= balance.BaseBoardRaidMedium ? RiskLevel.Medium
                    : RiskLevel.Low;
                Line(body, Loc.F("base.intrusion", Loc.T("base.risk." + level))).AddToClassList(Tone(level));
            }

            var actions = new List<string>();
            if (raid.ScarecrowDown) actions.Add(Scarecrow);
            if (state.Base.Defense < raid.DefenseMax) actions.Add(Barricade);
            if (raid.Traps < raid.MaxTraps) actions.Add(Trap);
            if (!raid.Alarm) actions.Add(Alarm);
            if (!state.Flags.Contains(DefenseRules.FlagReinforced)) actions.Add(Reinforce);
            if (!state.Flags.Contains(DefenseRules.FlagWindowCovers)) actions.Add(Covers);
            actions.Add(NightWatch);
            Actions(body, actions);
        }

        // ── 식량과 물 ──

        private void Supply(GameState state, SupplyForecast food, SupplyForecast water)
        {
            var shortNow = food.ShortDay > 0 || water.ShortDay > 0;
            var tight = food.End < food.NextWeekNeed || water.End < water.NextWeekNeed;
            var verdict = Loc.T(shortNow ? "base.supply.short" : tight ? "base.supply.tight" : "base.supply.ok");
            var body = Card("supply", "base.supply", "res.Food", verdict, shortNow ? "bad" : tight ? "warn" : "ok");
            if (body == null)
            {
                return;
            }

            var balance = _session.Definitions.Balance;

            // 식량
            var foodHead = Line(body, Loc.F("base.food", food.Now, food.End));
            foodHead.AddToClassList("base-supply-head");
            StoryArt.SetIcon(foodHead, StoryArt.Icon("res.Food"), 18f);
            var foodParts = new List<string> { Loc.F("base.use", food.Use, Loc.T("diet." + state.FoodDiet)) };
            if (food.Garden > 0) foodParts.Add(Loc.F("base.garden", food.Garden));
            if (food.Spoil > 0) foodParts.Add(Loc.F("base.spoil", food.Spoil));
            if (food.Cat > 0) foodParts.Add(Loc.F("base.cat", food.Cat));
            Line(body, string.Join(" · ", foodParts)).AddToClassList("dim");
            if (food.ShortDay > 0)
            {
                Line(body, Loc.F("base.short", Day(food.ShortDay, balance))).AddToClassList("bad");
            }
            else if (food.End < food.NextWeekNeed)
            {
                Line(body, Loc.F("base.next_week", food.NextWeekNeed)).AddToClassList("warn");
            }

            Line(body, food.FridgeBonus > 0
                ? Loc.F("base.spoil_line_fridge", food.SpoilThreshold, food.FridgeBonus)
                : Loc.F("base.spoil_line", food.SpoilThreshold)).AddToClassList("dim");

            // 물
            var waterHead = Line(body, Loc.F("base.water", water.Now, water.End));
            waterHead.AddToClassList("base-supply-head");
            StoryArt.SetIcon(waterHead, StoryArt.Icon("res.Water"), 18f);
            var waterParts = new List<string> { Loc.F("base.use", water.Use, Loc.T("diet." + state.WaterDiet)) };
            if (water.Purified > 0) waterParts.Add(Loc.F("base.purified", water.Purified));
            Line(body, string.Join(" · ", waterParts)).AddToClassList("dim");
            if (water.ShortDay > 0)
            {
                Line(body, Loc.F("base.short", Day(water.ShortDay, balance))).AddToClassList("bad");
            }
            else if (water.End < water.NextWeekNeed)
            {
                Line(body, Loc.F("base.next_week", water.NextWeekNeed)).AddToClassList("warn");
            }

            if (water.Raw > 0 || water.RawEnd > 0)
            {
                Line(body, Loc.F("base.raw", water.Raw, water.RawEnd)).AddToClassList("dim");
            }

            Actions(body, water.Raw > 0 ? new List<string> { Purify } : new List<string>());
        }

        // ── 전력 ──

        private void PowerCard(PowerForecast power)
        {
            var verdict = power.Broken ? Loc.T("base.power.broken")
                : power.Free <= 0 ? Loc.T("base.power.full")
                : Loc.F("base.power.free", power.Free);
            var body = Card("power", "base.power", "res.Power", verdict, power.Broken ? "bad" : power.Free <= 0 ? "warn" : "ok");
            if (body == null)
            {
                return;
            }

            // 용량 칸: 켜진 기기 몫 · 여유 · 아직 없는 용량
            var used = power.Appliances.Where(a => a.On).Sum(a => a.Power);
            var cells = new VisualElement();
            cells.AddToClassList("base-power-cells");
            for (var i = 0; i < Math.Max(power.MaxCapacity, power.Capacity); i++)
            {
                var cell = new VisualElement();
                cell.AddToClassList("base-power-cell");
                cell.AddToClassList(power.Broken && i < power.Capacity ? "broken" : i < used ? "used" : i < power.Capacity ? "free" : "none");
                cells.Add(cell);
            }

            var row = new VisualElement();
            row.AddToClassList("base-power-row");
            row.Add(cells);
            row.Add(new Label(Loc.F("base.power.capacity", power.Free, power.Capacity)));
            body.Add(row);

            foreach (var appliance in power.Appliances)
            {
                var name = Loc.T("appliance." + appliance.Id);
                Line(body, Loc.F(appliance.On ? "base.appliance.on" : "base.appliance.off", name, appliance.Power))
                    .AddToClassList(appliance.On ? "dim" : "warn");
            }

            if (power.Capacity >= power.MaxCapacity)
            {
                Line(body, Loc.T("base.power.max")).AddToClassList("dim");
            }
            else if (power.Restored)
            {
                Line(body, Loc.F("base.power.battery", power.Batteries, power.UpgradeCost)).AddToClassList(power.CanUpgrade ? "good" : "dim");
            }

            var actions = new List<string>();
            if (power.Broken) actions.Add(Repair);
            if (power.CanUpgrade) actions.Add(Upgrade);
            Actions(body, actions);
        }

        // ── 칸 만들기 ──

        /// <summary>칸 머리(아이콘·제목·판정)와 몸통. 접혀 있으면 머리만 그리고 null.</summary>
        private VisualElement Card(string id, string titleKey, string iconKey, string verdict, string tone)
        {
            var card = new VisualElement();
            card.AddToClassList("base-card");
            var folded = PlayerPrefs.GetInt(FoldPrefix + id, 0) != 0;
            card.EnableInClassList("folded", folded);

            var head = new Button(() =>
            {
                PlayerPrefs.SetInt(FoldPrefix + id, folded ? 0 : 1);
                PlayerPrefs.Save();
                Refresh();
            });
            head.AddToClassList("base-card-head");
            var title = new Label(Loc.T(titleKey));
            title.AddToClassList("base-card-title");
            StoryArt.SetIcon(title, StoryArt.Icon(iconKey), 22f);
            head.Add(title);
            var chip = new Label(verdict);
            chip.AddToClassList("base-verdict");
            chip.AddToClassList(tone);
            head.Add(chip);
            var fold = new Label(folded ? "▸" : "▾");
            fold.AddToClassList("base-fold");
            head.Add(fold);
            card.Add(head);
            _host.Add(card);
            if (folded)
            {
                return null;
            }

            var body = new VisualElement();
            body.AddToClassList("base-card-body");
            card.Add(body);
            return body;
        }

        private static Label Line(VisualElement parent, string text)
        {
            var label = new Label(text);
            label.AddToClassList("base-line");
            parent.Add(label);
            return label;
        }

        private static void Bar(VisualElement parent, string title, int value, int max, string css)
        {
            var bar = new ProgressBar { lowValue = 0, highValue = Math.Max(1, max), value = Math.Max(0, Math.Min(value, max)), title = title };
            bar.AddToClassList("base-bar");
            bar.AddToClassList("base-bar-" + css);
            parent.Add(bar);
        }

        /// <summary>계획 단계에서, 열려 있는 행동만 단추로 (최대 MaxActions개). 누르면 계획 보드에서 그 행동을 고른다.</summary>
        private void Actions(VisualElement parent, List<string> ids)
        {
            if (!_session.IsPlanning)
            {
                return;
            }

            var row = new VisualElement();
            row.AddToClassList("base-actions");
            foreach (var id in ids.Distinct())
            {
                if (row.childCount >= MaxActions)
                {
                    break;
                }

                if (!_session.Definitions.TryGetAction(id, out var action) || !_session.IsUnlocked(action))
                {
                    continue;
                }

                var actionId = id;
                var button = new Button(() => ActionPicked?.Invoke(actionId)) { text = Loc.Action(id), tooltip = Loc.T("base.pick_hint") };
                button.AddToClassList("small-button");
                button.AddToClassList("base-action");
                var icon = StoryArt.Icon("act." + id);
                if (icon != null)
                {
                    StoryArt.SetIcon(button, icon, 20f);
                }

                row.Add(button);
            }

            if (row.childCount > 0)
            {
                parent.Add(row);
            }
        }

        private static string Tone(RiskLevel level) =>
            level == RiskLevel.High ? "bad" : level == RiskLevel.Medium ? "warn" : "ok";

        private static string Dots(int filled, int total, string on, string off)
        {
            var text = "";
            for (var i = 0; i < total; i++)
            {
                text += i < filled ? on : off;
            }

            return text;
        }

        private static string Day(int day, BalanceConfig balance) => Loc.T("weekday." + CalendarRules.Weekday(day, balance));
    }
}
