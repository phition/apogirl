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

            _roleCard = null;
            if (db.Extras.Roles.Count > 0 && Companions(state).Count > 0)
            {
                RolesCard(state);
            }

            KoreanFont.ApplyDeep(_host);
        }

        // ── 역할 판 (P3) ──

        /// <summary>역할 칸에서 [ + ]를 눌러 사람을 고르는 중인 역할 (null이면 아님).</summary>
        private string _picking;

        private VisualElement _roleCard;

        /// <summary>서랍 안 역할 칸으로 내린다 (인물 서랍·방 패널의 역할 줄을 눌렀을 때). 접혀 있으면 편다.</summary>
        public void ScrollToRoles()
        {
            if (PlayerPrefs.GetInt(FoldPrefix + "roles", 0) != 0)
            {
                PlayerPrefs.SetInt(FoldPrefix + "roles", 0);
                PlayerPrefs.Save();
                Refresh();
            }

            var scroll = _host.GetFirstAncestorOfType<ScrollView>();
            _host.schedule.Execute(() =>
            {
                if (_roleCard != null && scroll != null)
                {
                    scroll.ScrollTo(_roleCard);
                }
            }).StartingIn(60);
        }

        /// <summary>합류한 동료 (고양이 빼고, 역할을 맡을 수 있는 사람).</summary>
        private static List<string> Companions(GameState state) =>
            state.Npcs.Where(n => n.Value.Status == NpcStatus.Joined && !CatRules.IsCat(n.Key))
                .Select(n => n.Key).OrderBy(id => id, StringComparer.Ordinal).ToList();

        /// <summary>
        /// 역할 6줄 × 자리(정원): 방 묶음과 배율, 맡은 사람 뱃지(적성 ★·스트레스·다침·외출·정원 넘침), 빈 자리 [ + ].
        /// 맨 아래 쉬는 사람. 바꾸기는 계획 중에만 (GameSession.SetNpcRole, 규칙은 RoleRules.Assign 그대로).
        /// </summary>
        private void RolesCard(GameState state)
        {
            var db = _session.Definitions;
            var balance = db.Balance;
            var companions = Companions(state);
            var idle = companions.Where(id => string.IsNullOrEmpty(state.Npcs[id].Role)).ToList();
            var assigned = companions.Count - idle.Count;
            var openSlots = db.Extras.Roles.Where(r => RoleRules.IsAvailable(state, r))
                .Sum(r => Math.Max(0, balance.RoleCapacity - RoleRules.Holders(state, r.Id).Count));
            var tone = idle.Count > 0 && openSlots > 0 ? "warn" : "ok";
            var verdict = idle.Count > 0 && openSlots > 0 ? Loc.F("base.roles.idle_count", idle.Count) : Loc.F("base.roles.filled", assigned);
            var body = Card("roles", "base.roles", "menu.people", verdict, tone);
            _roleCard = _host.Children().LastOrDefault();
            if (body == null)
            {
                return;
            }

            var planning = _session.IsPlanning;
            var outings = planning ? OutingRules.Plan(state, db) : new Dictionary<string, List<OutingPlan>>();
            _section ??= HubSectionConfig.Load();
            foreach (var role in db.Extras.Roles)
            {
                var available = RoleRules.IsAvailable(state, role);
                var row = new VisualElement();
                row.AddToClassList("base-role");
                row.EnableInClassList("locked", !available);

                // 머리: 역할 이름 + 방 묶음·배율
                var head = new VisualElement();
                head.AddToClassList("base-role-head");
                var name = new Label(Loc.T("role." + role.Id)) { tooltip = Loc.T("role." + role.Id + ".desc") };
                name.AddToClassList("base-role-name");
                StoryArt.SetIcon(name, StoryArt.Icon("role." + role.Id), 18f);
                head.Add(name);
                var rooms = new Label(RoleRooms(state, role, available));
                rooms.AddToClassList("base-role-rooms");
                head.Add(rooms);
                row.Add(head);

                if (available)
                {
                    var slots = new VisualElement();
                    slots.AddToClassList("base-role-slots");
                    var holders = RoleRules.Holders(state, role.Id);
                    foreach (var id in holders)
                    {
                        slots.Add(HolderBadge(state, role, id, outings, planning));
                    }

                    var capacity = balance.RoleCapacity <= 0 ? holders.Count + 1 : balance.RoleCapacity;
                    for (var i = holders.Count; i < capacity; i++)
                    {
                        var roleId = role.Id;
                        var add = new Button(() =>
                        {
                            _picking = _picking == roleId ? null : roleId;
                            Refresh();
                        }) { text = "+", tooltip = Loc.T(planning ? "base.roles.add_hint" : "base.roles.planning_only") };
                        add.AddToClassList("base-role-add");
                        add.EnableInClassList("on", _picking == role.Id);
                        add.SetEnabled(planning && companions.Any(id => state.Npcs[id].Role != roleId));
                        slots.Add(add);
                    }

                    row.Add(slots);
                    if (planning && _picking == role.Id)
                    {
                        row.Add(Candidates(state, role, companions));
                    }
                }

                body.Add(row);
            }

            // 쉬는 사람
            var rest = new VisualElement();
            rest.AddToClassList("base-role-rest");
            var restTitle = new Label(Loc.T(idle.Count > 0 ? "base.roles.idle" : "base.roles.idle_none"));
            restTitle.AddToClassList("base-role-rest-title");
            rest.Add(restTitle);
            foreach (var id in idle)
            {
                var badge = Badge(id, "");
                var wish = Loc.T("people.wish." + id);
                badge.tooltip = wish != "people.wish." + id ? wish : Loc.T("base.roles.idle_hint");
                rest.Add(badge);
            }

            body.Add(rest);
        }

        /// <summary>역할의 방 묶음: 열린 방 이름 · 배율, 열린 방이 없으면 잠김과 여는 방법.</summary>
        private string RoleRooms(GameState state, RoleDef role, bool available)
        {
            if (role.Rooms.Count == 0)
            {
                return "";
            }

            var open = role.Rooms.Where(r => RoleRules.IsRoomOpen(state, r)).ToList();
            if (!available || open.Count == 0)
            {
                var first = _section.Rooms.FirstOrDefault(r => r.Id == role.Rooms[0].Id);
                var hint = first != null ? RoomText.Hint(first) : "";
                return role.Basic ? Loc.T("base.roles.no_room") : Loc.F("base.roles.locked", hint);
            }

            var names = string.Join("·", open.Select(r => RoomNameOf(r.Id)));
            var scale = RoleRules.RoomScale(state, role, _session.Definitions.Balance);
            var closed = role.Rooms.Count - open.Count;
            var text = scale > 1.001f ? names + "  " + Loc.F("base.roles.scale", scale.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)) : names;
            return closed > 0 ? text + "  " + Loc.F("base.roles.more_rooms", closed) : text;
        }

        private string RoomNameOf(string roomId)
        {
            var room = _section.Rooms.FirstOrDefault(r => r.Id == roomId);
            return room != null ? HubSectionConfig.RoomName(room) : roomId;
        }

        /// <summary>맡은 사람 뱃지. 우클릭 = 역할에서 뺀다 (계획 중).</summary>
        private VisualElement HolderBadge(GameState state, RoleDef role, string id, Dictionary<string, List<OutingPlan>> outings, bool planning)
        {
            var balance = _session.Definitions.Balance;
            var npc = state.Npcs[id];
            var tags = new List<string>();
            var notes = new List<string>();
            var badge = Badge(id, RoleRules.HasAptitude(balance, id, role.Id) ? Loc.T("hub.room_aptitude") : "");
            if (!RoleRules.IsActive(state, id, balance))
            {
                badge.AddToClassList("overflow");
                notes.Add(Loc.T("people.role_overflow"));
            }

            if (GiveRules.IsStressed(npc, balance))
            {
                badge.AddToClassList("weak");
                notes.Add(Loc.T("base.roles.stressed"));
            }

            if (npc.InjuredWeeks > 0)
            {
                badge.AddToClassList("weak");
                tags.Add(Loc.T("base.roles.injured_tag"));
                notes.Add(Loc.T("base.roles.injured"));
            }

            if (outings.TryGetValue(id, out var plans) && plans.Count > 0)
            {
                tags.Add(Loc.F("base.roles.out_tag", plans.Count));
                notes.Add(Loc.F("base.roles.out", plans.Count));
            }

            if (tags.Count > 0)
            {
                var tag = new Label(string.Join(" ", tags));
                tag.AddToClassList("base-role-tag");
                badge.Add(tag);
            }

            if (planning)
            {
                notes.Add(Loc.T("base.roles.remove_hint"));
                var npcId = id;
                badge.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button == 1)
                    {
                        _session.SetNpcRole(npcId, "");
                        evt.StopPropagation();
                    }
                });
            }

            badge.tooltip = string.Join("\n", notes);
            return badge;
        }

        /// <summary>[ + ] 아래: 이 역할을 맡길 수 있는 사람 (쉬는 사람 먼저, 지금 맡은 역할을 작게).</summary>
        private VisualElement Candidates(GameState state, RoleDef role, List<string> companions)
        {
            var balance = _session.Definitions.Balance;
            var strip = new VisualElement();
            strip.AddToClassList("base-role-pick");
            var title = new Label(Loc.T("base.roles.pick"));
            title.AddToClassList("base-role-pick-title");
            strip.Add(title);
            foreach (var id in companions.Where(c => state.Npcs[c].Role != role.Id).OrderBy(c => string.IsNullOrEmpty(state.Npcs[c].Role) ? 0 : 1))
            {
                var current = state.Npcs[id].Role;
                var badge = Badge(id, RoleRules.HasAptitude(balance, id, role.Id) ? Loc.T("hub.room_aptitude") : "");
                if (!string.IsNullOrEmpty(current))
                {
                    var tag = new Label(Loc.T("role." + current));
                    tag.AddToClassList("base-role-tag");
                    badge.Add(tag);
                }

                var npcId = id;
                var roleId = role.Id;
                badge.AddToClassList("pick");
                badge.tooltip = string.IsNullOrEmpty(current) ? Loc.T("base.roles.pick_hint") : Loc.F("base.roles.move_hint", Loc.T("role." + current));
                badge.RegisterCallback<ClickEvent>(_ =>
                {
                    _picking = null;
                    if (!_session.SetNpcRole(npcId, roleId))
                    {
                        Refresh();
                    }
                });
                strip.Add(badge);
            }

            return strip;
        }

        /// <summary>얼굴 + 이름 (+ 적성 ★) 뱃지.</summary>
        private static VisualElement Badge(string id, string mark)
        {
            var badge = new VisualElement();
            badge.AddToClassList("base-person");
            badge.Add(PeopleUi.Badge(id, 26f));
            var name = new Label(PeopleUi.Name(id) + (string.IsNullOrEmpty(mark) ? "" : " " + mark));
            name.AddToClassList("base-person-name");
            badge.Add(name);
            return badge;
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
                var effects = RoomText.Effects(db, room.Facility);
                var text = new Label(!isOpen ? RoomText.Hint(room) : effects.Count > 0 ? string.Join(" · ", effects) : RoomText.Use(room));
                text.AddToClassList("base-room-text");
                text.EnableInClassList("use", isOpen && effects.Count == 0);   // 효과(초록)가 아니라 쓰임(보통 글자)
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
                // 습격이 얼마나 자주 오는가 (머리의 "밤 습격 ○○"의 까닭)
                foreach (var reason in raid.Reasons)
                {
                    var line = Line(body, "· " + Loc.F("base.why." + reason.Key, reason.Value));
                    line.AddToClassList(reason.Good ? "good" : "bad");
                }

                // 오면 막는가: 막는 힘 대 습격 세기 (위 까닭과 따로 — 방어보다 위협이 높아도 전투력으로 막을 수 있다)
                var repel = Line(body, Loc.F("base.repel", Loc.T("base.repel." + raid.Repel)));
                repel.AddToClassList(Tone(raid.Repel));
                var p = raid.RepelParts;
                if (p != null)
                {
                    var why = Line(body, "· " + Loc.F("base.repel.why", Range(p.Power, p.Power + p.PowerSpread), Range(p.StrengthMin, p.StrengthMax)));
                    why.AddToClassList("dim");
                    var pieces = new List<string> { Loc.F("base.repel.part.base", p.Base), Loc.F("base.repel.part.combat", p.Combat), Loc.F("base.repel.part.defense", p.Defense) };
                    if (p.Gear > 0) pieces.Add(Loc.F("base.repel.part.gear", p.Gear));
                    if (p.Relief > 0) pieces.Add(Loc.F("base.repel.part.relief", p.Relief));
                    var detail = Line(body, "  " + string.Join(" · ", pieces));
                    detail.AddToClassList("dim");
                    detail.AddToClassList("base-repel-parts");
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
            var foodParts = new List<string> { Loc.F(food.Mornings > 0 ? "base.use" : "base.use_none", food.Use, Loc.T("diet." + state.FoodDiet), food.Mornings) };
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
            var waterParts = new List<string> { Loc.F(water.Mornings > 0 ? "base.use" : "base.use_none", water.Use, Loc.T("diet." + state.WaterDiet), water.Mornings) };
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

        /// <summary>"12" 또는 "12~15".</summary>
        private static string Range(int min, int max) => max > min ? min + "~" + max : min.ToString();

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
