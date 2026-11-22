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
    /// <summary>인물 화면에서 함께 쓰는 것: 둥근 뱃지, 호감 느낌 문구, 몸 상태 막대.</summary>
    internal static class PeopleUi
    {
        /// <summary>호감 단계 (수치는 보이지 않는다, 기획 D.1): 0 서먹 · 1 데면데면 · 2 편함 · 3 가까움 · 4 믿음.</summary>
        public static int BondTier(int affinity, BalanceConfig balance) => BondRules.Tier(affinity, balance);

        public static int Affinity(GameState state, string id)
        {
            if (id == GiveRules.TeacherId)
            {
                return state.Teacher.Affinity;
            }

            return state.Npcs.TryGetValue(id, out var npc) ? npc.Affinity : 0;
        }

        /// <summary>그 사람과의 거리를 말로: bond.{ID}.{단계}, 없으면 bond.generic.{단계}.</summary>
        public static string BondPhrase(GameState state, BalanceConfig balance, string id)
        {
            var tier = BondTier(Affinity(state, id), balance);
            var text = Loc.Lookup(Loc.UiTable, "bond." + id + "." + tier);
            return string.IsNullOrEmpty(text) ? Loc.T("bond.generic." + tier) : text;
        }

        public static string Name(string id) => Loc.T("npc." + id);

        /// <summary>둥근 뱃지 (그림이 없으면 이름 첫 글자).</summary>
        public static VisualElement Badge(string id, float size)
        {
            var badge = new VisualElement();
            badge.AddToClassList("person-badge");
            badge.style.width = size;
            badge.style.height = size;
            var texture = StoryArt.Icon("npc." + id);
            if (texture != null)
            {
                StoryArt.SetContain(badge, texture);
            }
            else
            {
                badge.AddToClassList("empty");
                var name = Name(id);
                var initial = new Label(string.IsNullOrEmpty(name) ? "?" : name.Substring(0, 1));
                initial.AddToClassList("person-initial");
                badge.Add(initial);
            }

            return badge;
        }

        public static ProgressBar Bar(string iconKey, string css, bool centered, int value, string title)
        {
            var bar = new ProgressBar { lowValue = centered ? -100 : 0, highValue = 100, value = value, title = title };
            bar.AddToClassList("vital-bar");
            bar.AddToClassList("person-bar");
            bar.AddToClassList(css);
            StoryArt.SetIcon(bar, StoryArt.Icon(iconKey), 18f);
            return bar;
        }

        /// <summary>함께한 시간 말풍선: 적성 역할이면 가끔 전용 문구(together.{ID}.apt.n), 아니면 그 사람 문구 셋 중 하나, 없으면 공용.</summary>
        public static string Together(string id, bool aptitude, int seed)
        {
            seed = Math.Abs(seed);
            if (aptitude && seed % 2 == 0)
            {
                var special = Loc.Lookup(Loc.UiTable, "together." + id + ".apt." + seed / 2 % 2);
                if (!string.IsNullOrEmpty(special))
                {
                    return special;
                }
            }

            var own = Loc.Lookup(Loc.UiTable, "together." + id + "." + seed % 3);
            return string.IsNullOrEmpty(own) ? Loc.T("together.generic." + seed % 3) : own;
        }

        /// <summary>결과 한 줄 꼬리: "윤 선생님, 도현과 함께 했다" / "하린은 탐험을 나가 자리에 없었다" (조사는 마지막 이름에 맞춘다).</summary>
        public static string TogetherLine(IReadOnlyList<TogetherEntry> together)
        {
            if (together == null || together.Count == 0)
            {
                return "";
            }

            var parts = new List<string>();
            var present = together.Where(t => !t.Absent).Select(t => Name(t.Id)).ToList();
            if (present.Count > 0)
            {
                present[present.Count - 1] = ApoGirl.Core.Text.KoreanJosa.Attach(present[present.Count - 1], "과/와");
                parts.Add(Loc.F("together.with", string.Join(", ", present)));
            }

            var absent = together.Where(t => t.Absent).Select(t => Name(t.Id)).ToList();
            if (absent.Count > 0)
            {
                absent[absent.Count - 1] = ApoGirl.Core.Text.KoreanJosa.Attach(absent[absent.Count - 1], "은/는");
                parts.Add(Loc.F("together.absent", string.Join(", ", absent)));
            }

            return string.Join(" · ", parts);
        }

        /// <summary>동료 탐험 한 줄: 나감 / 돌아옴(구해 온 것·다침).</summary>
        public static List<string> OutingLines(IReadOnlyList<string> departures, IReadOnlyList<OutingReturn> returns)
        {
            var lines = new List<string>();
            foreach (var id in departures ?? new List<string>())
            {
                lines.Add(Loc.F("outing.left", ApoGirl.Core.Text.KoreanJosa.Attach(Name(id), "이/가")));
            }

            foreach (var back in returns ?? new List<OutingReturn>())
            {
                var who = ApoGirl.Core.Text.KoreanJosa.Attach(Name(back.NpcId), "이/가");
                var haul = Haul(back.Resources, back.ItemId);
                var line = haul.Length > 0 ? Loc.F("outing.back", who, haul) : Loc.F("outing.back_empty", who);
                if (back.Wound != OutingWound.None)
                {
                    line += " · " + Loc.T(back.Wound == OutingWound.Severe ? "outing.wound_severe" : "outing.wound_minor");
                }

                lines.Add(line);
            }

            return lines;
        }

        /// <summary>"식량 3, 식수 2, 사탕" (자원 + 기호품).</summary>
        public static string Haul(IEnumerable<ResourceAmount> resources, string itemId = "")
        {
            var parts = resources.Where(r => r.Amount > 0).Select(r => UiText.Resource(r.Resource) + " " + r.Amount).ToList();
            if (!string.IsNullOrEmpty(itemId))
            {
                parts.Add(Loc.Item(itemId));
            }

            return string.Join(", ", parts);
        }

        /// <summary>돌아온 동료 머리 위 말풍선: 기호품을 주웠으면 그 이야기, 아니면 구해 온 것.</summary>
        public static string ReturnBubble(OutingReturn back)
        {
            if (!string.IsNullOrEmpty(back.ItemFor))
            {
                return back.ItemFor == back.NpcId ? Loc.F("outing.bubble_item_self", Loc.Item(back.ItemId))
                    : Loc.F("outing.bubble_item_for", Name(back.ItemFor), Loc.Item(back.ItemId));
            }

            var haul = Haul(back.Resources);
            return haul.Length > 0 ? Loc.F("outing.bubble_haul", haul) : Loc.T("outing.bubble_empty");
        }

        /// <summary>고마워하는 말 한 줄: 좋아하는 물건이면 전용 문구, 아니면 그 사람 문구 셋 중 하나.</summary>
        public static string Thanks(string id, bool liked, int seed)
        {
            if (liked)
            {
                var special = Loc.Lookup(Loc.UiTable, "give.thanks." + id + ".liked");
                if (!string.IsNullOrEmpty(special))
                {
                    return special;
                }
            }

            var own = Loc.Lookup(Loc.UiTable, "give.thanks." + id + "." + Math.Abs(seed) % 3);
            return string.IsNullOrEmpty(own) ? Loc.T("give.thanks.generic." + Math.Abs(seed) % 3) : own;
        }
    }

    /// <summary>
    /// 2겹 인물 서랍 (기획 D.1): 사람마다 카드 한 장 — 뱃지, 이름·역할, 호감은 느낌 문구로만,
    /// 스트레스·포만·수분 막대, 부상. 맨 위는 윤 선생님(이동·감염 단계). 보기 전용이고 배분은 한 주 실행 직전에 한다.
    /// </summary>
    internal sealed class PeopleDrawerView
    {
        private readonly GameSession _session;
        private readonly ScrollView _panel;
        private Dictionary<string, List<OutingPlan>> _outings = new Dictionary<string, List<OutingPlan>>();

        public PeopleDrawerView(VisualElement root, GameSession session)
        {
            _session = session;
            _panel = root.Q<ScrollView>("people-panel");
        }

        /// <summary>"이번 주는 남아 줘"를 바꿨을 때 (허브가 계획 보드·서랍을 새로 그린다).</summary>
        public Action Changed { get; set; }

        public void Refresh()
        {
            if (_panel == null)
            {
                return;
            }

            _panel.Clear();
            var state = _session.State;
            var balance = _session.Definitions.Balance;
            _outings = _session.IsPlanning ? OutingRules.Plan(state, _session.Definitions) : new Dictionary<string, List<OutingPlan>>();
            var recipients = GiveRules.Recipients(state, balance);
            if (recipients.Count == 0)
            {
                var none = new Label(Loc.T("people.none"));
                none.AddToClassList("people-dim");
                _panel.Add(none);
                return;
            }

            foreach (var id in recipients)
            {
                _panel.Add(Card(state, balance, id));
            }

            var hint = new Label(Loc.T("people.hint"));
            hint.AddToClassList("people-dim");
            _panel.Add(hint);
        }

        private VisualElement Card(GameState state, BalanceConfig balance, string id)
        {
            var card = new VisualElement();
            card.AddToClassList("person-card");
            var top = new VisualElement();
            top.AddToClassList("person-top");
            top.Add(PeopleUi.Badge(id, 64f));
            var info = new VisualElement();
            info.AddToClassList("person-info");
            var nameRow = new VisualElement();
            nameRow.AddToClassList("person-name-row");
            var name = new Label(PeopleUi.Name(id));
            name.AddToClassList("person-name");
            nameRow.Add(name);
            var roleText = RoleText(state, id);
            if (!string.IsNullOrEmpty(roleText))
            {
                var role = new Label(roleText);
                role.AddToClassList("person-role");
                nameRow.Add(role);
            }

            info.Add(nameRow);
            var bond = new Label(PeopleUi.BondPhrase(state, balance, id));
            bond.AddToClassList("person-bond");
            info.Add(bond);
            top.Add(info);
            card.Add(top);

            var body = GiveRules.Body(state, id) ?? new BodyState();
            var cat = CatRules.IsCat(id);   // 고양이는 관계·허기만 (확장 2)
            if (!cat)
            {
                card.Add(PeopleUi.Bar("vital.Stress", "vital-stress", false, body.Stress, UiText.Vital(VitalType.Stress) + " " + body.Stress));
            }

            card.Add(PeopleUi.Bar("vital.Satiety", "vital-satiety", true, body.Satiety, UiText.Vital(VitalType.Satiety) + " " + UiText.Signed(body.Satiety)));
            if (!cat)
            {
                card.Add(PeopleUi.Bar("vital.Hydration", "vital-hydration", true, body.Hydration, UiText.Vital(VitalType.Hydration) + " " + UiText.Signed(body.Hydration)));
            }

            foreach (var flag in Flags(state, balance, id))
            {
                var label = new Label(flag);
                label.AddToClassList("person-flag");
                card.Add(label);
            }

            foreach (var line in OutingFlags(state, id))
            {
                var label = new Label(line);
                label.AddToClassList("person-outing");
                card.Add(label);
            }

            // 동료 탐험: 계획 중에만 붙잡을 수 있다 (한 주만, 2주 연속이면 답답해한다)
            if (id != GiveRules.TeacherId && _session.IsPlanning && OutingRules.WantsOut(state, _session.Definitions, id))
            {
                var npc = state.Npcs[id];
                var toggle = new Button(() =>
                {
                    npc.StayHome = !npc.StayHome;
                    Changed?.Invoke();
                    if (Changed == null)
                    {
                        Refresh();
                    }
                }) { text = Loc.T(npc.StayHome ? "people.outing_let_go" : "people.outing_stay") };
                toggle.AddToClassList("person-stay");
                toggle.EnableInClassList("on", npc.StayHome);
                card.Add(toggle);
            }

            return card;
        }

        /// <summary>동료 탐험 줄: 계획 중 예고(날짜 없이)·붙잡음, 실행 중 밖에 나가 있음.</summary>
        private IEnumerable<string> OutingFlags(GameState state, string id)
        {
            if (id == GiveRules.TeacherId || !state.Npcs.TryGetValue(id, out var npc))
            {
                yield break;
            }

            if (_session.IsPlanning)
            {
                var who = ApoGirl.Core.Text.KoreanJosa.Attach(PeopleUi.Name(id), "이/가");
                if (npc.StayHome && OutingRules.WantsOut(state, _session.Definitions, id))
                {
                    yield return Loc.F("people.outing_held", who);
                    if (npc.StayStreak >= 1)
                    {
                        yield return Loc.T("people.outing_held_again");
                    }
                }
                else if (_outings.ContainsKey(id))
                {
                    yield return Loc.F("people.outing_forecast", who);
                }
            }
            else if (npc.Away)
            {
                yield return Loc.T("people.outing_away");
            }
        }

        private static string RoleText(GameState state, string id)
        {
            if (id == GiveRules.TeacherId)
            {
                return Loc.T("people.teacher_role");
            }

            if (CatRules.IsCat(id))
            {
                return "";
            }

            var npc = state.Npcs[id];
            return string.IsNullOrEmpty(npc.Role) ? Loc.T("role.none") : Loc.T("role." + npc.Role);
        }

        private IEnumerable<string> Flags(GameState state, BalanceConfig balance, string id)
        {
            if (id == GiveRules.TeacherId)
            {
                yield return Loc.T("mobility." + Math.Min(state.Teacher.MobilityStage, 2));
                var stage = InfectionRules.TeacherStage(state, balance);
                if (stage > 0)
                {
                    yield return Loc.T("teacher.stage." + Math.Min(stage, 4));
                }

                yield break;
            }

            if (CatRules.IsCat(id))
            {
                yield return Loc.T("people.cat_note");   // 확장 2: 무엇을 해 주는지 한 줄
                yield break;
            }

            var npc = state.Npcs[id];
            if (npc.InjuredWeeks > 0)
            {
                yield return Loc.F("people.injured", npc.InjuredWeeks);
            }

            if (npc.MinorWoundWeeks > 0)
            {
                yield return Loc.T("people.minor_wound");
            }

            // 특기 (기획 C.2 ②): 켜져 있으면 무엇을 해 주는지, 마음이 멀어져 꺼졌으면 그 사실만
            if (SpecialRules.IsUnlocked(state, balance, id))
            {
                yield return SpecialRules.IsActive(state, balance, id)
                    ? Loc.F("people.special_on", Loc.T("special." + id + ".name"), Loc.T("special." + id + ".desc"))
                    : Loc.F("people.special_off", Loc.T("special." + id + ".name"));
            }

            // 역할 재기획: 적성은 느낌 문구로만, 정원을 넘었거나 방이 아직 없으면 그 사실만
            var db = _session.Definitions;
            var role = RoleRules.Find(db, npc.Role);
            if (role != null)
            {
                if (!RoleRules.IsActive(state, id, balance))
                {
                    yield return Loc.T("people.role_overflow");
                }
                else if (RoleRules.RoomScale(state, role, balance) <= 0f)
                {
                    yield return Loc.T("people.role_no_room");
                }
                else if (RoleRules.HasAptitude(balance, id, role.Id))
                {
                    yield return Loc.T("people.aptitude." + id + "." + role.Id);
                }
            }

            // 적성: 맡길 수 있는 적성 역할이 있으면 그 느낌 문구(힌트), 적성 방이 하나도 없으면 방 이름 없이 바람만
            var openAptitude = TogetherRules.OpenAptitudeRole(state, db, id);
            if (openAptitude != null && (role == null || !RoleRules.HasAptitude(balance, id, role.Id)))
            {
                yield return Loc.T("people.aptitude." + id + "." + openAptitude);
            }
            else if (TogetherRules.WishesForAptitudeRoom(state, db, id))
            {
                var wish = Loc.Lookup(Loc.UiTable, "people.wish." + id);
                if (!string.IsNullOrEmpty(wish))
                {
                    yield return wish;
                }
            }

            if (GiveRules.IsStressed(npc, balance))
            {
                yield return Loc.T("people.stressed");
            }
        }
    }

    /// <summary>
    /// 3겹 이번 주 배분하기 (기획 D.3): "한 주 실행"을 누르면 스케줄이 흐르기 전에 이 화면이 먼저 뜬다.
    /// 사람마다 음식·마실 것·의약품·기호품을 고르고(종류별 주 1회), 확인하면 실제로 주고 실행으로 넘어간다.
    /// 고른 것은 확인 전까지 아무것도 쓰지 않는다 (취소하면 그대로).
    /// </summary>
    internal sealed class GiveSheetView
    {
        private static readonly GiveKind[] BasicKinds = { GiveKind.Food, GiveKind.Water, GiveKind.Medicine };

        private readonly GameSession _session;
        private readonly VisualElement _sheet;
        private readonly Label _stock;
        private readonly ScrollView _list;
        private readonly VisualElement _buttons;

        // 확인 전까지 고른 것: (사람, 종류) → 기호품 ID (기본 셋은 빈 문자열)
        private readonly Dictionary<(string Id, GiveKind Kind), string> _picked = new Dictionary<(string, GiveKind), string>();
        private Action<List<(string Id, bool Liked)>> _onRun;
        private Action _onCancel;

        public GiveSheetView(VisualElement root, GameSession session)
        {
            _session = session;
            _sheet = root.Q("give-sheet");
            _stock = root.Q<Label>("give-stock");
            _list = root.Q<ScrollView>("give-list");
            _buttons = root.Q("give-buttons");
            root.Q<Label>("give-title").text = Loc.T("give.title");
            root.Q<Label>("give-hint").text = Loc.T("give.hint");
        }

        public bool IsOpen => _sheet != null && !_sheet.ClassListContains("hidden");

        /// <summary>배분할 사람이 있으면 화면을 열고 true. onRun(받은 사람들)은 확인(또는 배분 없이 실행) 뒤에 부른다.</summary>
        public bool Show(Action<List<(string Id, bool Liked)>> onRun, Action onCancel = null)
        {
            if (_sheet == null || GiveRules.Recipients(_session.State, _session.Definitions.Balance).Count == 0)
            {
                return false;
            }

            _onRun = onRun;
            _onCancel = onCancel;
            _picked.Clear();
            Build();
            _sheet.RemoveFromClassList("hidden");
            return true;
        }

        public void Hide()
        {
            _sheet?.AddToClassList("hidden");
            _onCancel?.Invoke();
            _onCancel = null;
        }

        private void Build()
        {
            var state = _session.State;
            var balance = _session.Definitions.Balance;
            _stock.text = Loc.F("give.stock",
                Remaining(ResourceType.Food, GiveKind.Food), Remaining(ResourceType.Water, GiveKind.Water), Remaining(ResourceType.Medicine, GiveKind.Medicine));

            _list.Clear();
            var candidates = GiveRules.GiftCandidates(state, balance);
            foreach (var id in GiveRules.Recipients(state, balance))
            {
                _list.Add(Row(state, balance, id, candidates));
            }

            _buttons.Clear();
            AddButton(Loc.T("give.cancel"), Hide, "give-cancel");
            AddButton(Loc.T("give.skip"), () => Confirm(false), "give-skip");
            var confirm = AddButton(Loc.F("give.confirm", _picked.Count), () => Confirm(true), "give-confirm");
            confirm.SetEnabled(_picked.Count > 0);
        }

        private VisualElement Row(GameState state, BalanceConfig balance, string id, List<string> candidates)
        {
            var row = new VisualElement();
            row.AddToClassList("give-row");
            row.Add(PeopleUi.Badge(id, 56f));

            var info = new VisualElement();
            info.AddToClassList("give-info");
            var name = new Label(PeopleUi.Name(id));
            name.AddToClassList("person-name");
            info.Add(name);
            var bond = new Label(PeopleUi.BondPhrase(state, balance, id));
            bond.AddToClassList("person-bond");
            info.Add(bond);
            var body = GiveRules.Body(state, id) ?? new BodyState();
            var condition = new Label(Loc.F("give.condition", body.Stress, UiText.Signed(body.Satiety), UiText.Signed(body.Hydration))
                                      + (id != GiveRules.TeacherId && state.Npcs[id].InjuredWeeks > 0 ? "  " + Loc.F("people.injured", state.Npcs[id].InjuredWeeks) : ""));
            condition.AddToClassList("give-condition");
            info.Add(condition);
            row.Add(info);

            var choices = new VisualElement();
            choices.AddToClassList("give-choices");
            var basics = new VisualElement();
            basics.AddToClassList("give-basics");
            foreach (var kind in BasicKinds)
            {
                basics.Add(Choice(id, kind, ""));
            }

            choices.Add(basics);

            // 기호품: 창고에 있는 것 중 고른다. 그 사람이 좋아하는 것을 앞에 (♥)
            var gifts = new VisualElement();
            gifts.AddToClassList("give-gifts");
            var ordered = candidates.OrderByDescending(item => GiveRules.Likes(balance, id, item)).ThenBy(item => item, StringComparer.Ordinal).ToList();
            if (ordered.Count == 0)
            {
                var none = new Label(Loc.T("give.no_gifts"));
                none.AddToClassList("people-dim");
                gifts.Add(none);
            }

            foreach (var item in ordered)
            {
                gifts.Add(Choice(id, GiveKind.Gift, item));
            }

            choices.Add(gifts);
            row.Add(choices);
            return row;
        }

        private Button Choice(string id, GiveKind kind, string itemId)
        {
            var key = (id, kind);
            var selected = _picked.TryGetValue(key, out var pickedItem) && (kind != GiveKind.Gift || pickedItem == itemId);
            var blocker = selected ? null : Blocker(id, kind, itemId);
            var liked = kind == GiveKind.Gift && GiveRules.Likes(_session.Definitions.Balance, id, itemId);
            var text = kind == GiveKind.Gift
                ? (liked ? "♥ " : "") + Loc.Item(itemId) + " " + Loc.F("give.have", ItemRemaining(itemId))
                : Loc.T("give.kind." + kind);
            var button = new Button(() =>
            {
                if (selected)
                {
                    _picked.Remove(key);
                }
                else
                {
                    _picked[key] = itemId; // 기호품은 한 사람에 하나: 다른 것을 고르면 바뀐다
                }

                Build();
            })
            {
                text = text,
                tooltip = blocker != null ? Loc.T(blocker) : Loc.T("give.kind." + kind + ".desc"),
            };
            button.AddToClassList("give-choice");
            button.EnableInClassList("selected", selected);
            button.EnableInClassList("liked", liked);
            StoryArt.SetIcon(button, kind == GiveKind.Gift ? StoryArt.ItemIcon(itemId) : StoryArt.Icon(KindIcon(kind)), 22f);
            button.SetEnabled(selected || blocker == null);
            return button;
        }

        private static string KindIcon(GiveKind kind) => kind == GiveKind.Food ? "res.Food" : kind == GiveKind.Water ? "res.Water" : "res.Medicine";

        /// <summary>세션의 막힘 이유 + 이미 고른 몫을 뺀 재고로 다시 본다.</summary>
        private string Blocker(string id, GiveKind kind, string itemId)
        {
            var blocker = _session.GiveBlocker(id, kind, kind == GiveKind.Gift ? itemId : null);
            if (blocker != null)
            {
                return blocker;
            }

            switch (kind)
            {
                case GiveKind.Food: return Remaining(ResourceType.Food, kind) > 0 ? null : "give.blocked.no_food";
                case GiveKind.Water: return Remaining(ResourceType.Water, kind) > 0 ? null : "give.blocked.no_water";
                case GiveKind.Medicine: return Remaining(ResourceType.Medicine, kind) > 0 ? null : "give.blocked.no_medicine";
                default: return ItemRemaining(itemId) > 0 ? null : "give.blocked.no_item";
            }
        }

        private int Remaining(ResourceType resource, GiveKind kind) =>
            _session.State.Base.GetResource(resource) - _picked.Keys.Count(k => k.Kind == kind);

        private int ItemRemaining(string itemId) =>
            LoadoutRules.CountInStorage(_session.State, itemId) - _picked.Count(p => p.Key.Kind == GiveKind.Gift && p.Value == itemId);

        private void Confirm(bool give)
        {
            var received = new List<(string Id, bool Liked)>();
            if (give)
            {
                foreach (var pair in _picked.OrderBy(p => p.Key.Id, StringComparer.Ordinal).ThenBy(p => p.Key.Kind))
                {
                    var itemId = pair.Key.Kind == GiveKind.Gift ? pair.Value : null;
                    if (!_session.Give(pair.Key.Id, pair.Key.Kind, itemId, out var liked))
                    {
                        continue;
                    }

                    var index = received.FindIndex(r => r.Id == pair.Key.Id);
                    if (index < 0)
                    {
                        received.Add((pair.Key.Id, liked));
                    }
                    else if (liked)
                    {
                        received[index] = (pair.Key.Id, true);
                    }
                }
            }

            var onRun = _onRun;
            _onCancel = null;
            _onRun = null;
            _sheet.AddToClassList("hidden");
            onRun?.Invoke(received);
        }

        private Button AddButton(string text, Action onClick, string css)
        {
            var button = new Button(onClick) { text = text };
            button.AddToClassList("card-button");
            button.AddToClassList(css);
            _buttons.Add(button);
            return button;
        }
    }
}
