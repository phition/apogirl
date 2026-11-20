using System;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;
using ApoGirl.Presentation.Art;
using ApoGirl.Presentation.Text;
using ApoGirl.Systems.Session;
using UnityEngine.UIElements;

namespace ApoGirl.Presentation.Screens
{
    /// <summary>
    /// 3겹 출정 준비 (문서 6.3.4-1, Docs/UI_REDESIGN_PROPOSAL.md): 왼쪽은 지역 그림 위에 목적지·시간대·예보·손실 경고,
    /// 오른쪽은 지금 차림의 주인공 입상과 장비 칸(배낭·주무기·도구·방어구, ◀ ▶로 바꾸기)과 퀵슬롯 4칸(누르면 다음 것).
    /// 장비를 바꾸면 입상도 바로 바뀐다.
    /// </summary>
    internal sealed class ExpeditionPrepView
    {
        private const int QuickSlotCount = 4;

        private readonly GameSession _session;
        private readonly VisualElement _sheet;
        private readonly VisualElement _art;
        private readonly VisualElement _info;
        private readonly VisualElement _loadout;
        private readonly VisualElement _buttons;
        private readonly StandingView _standing;

        public ExpeditionPrepView(VisualElement root, GameSession session)
        {
            _session = session;
            _sheet = root.Q("expedition-sheet");
            _art = root.Q("exp-art");
            _info = root.Q("exp-info");
            _loadout = root.Q("exp-loadout");
            _buttons = root.Q("exp-buttons");
            _standing = new StandingView(root.Q("exp-standing"), () => _session.State, () => _session.Definitions.Balance);

            // 지역 그림 아래쪽을 부드럽게 어둡게 (위는 투명 → 아래는 거의 검정)
            var shade = new UnityEngine.Texture2D(1, 64, UnityEngine.TextureFormat.RGBA32, false) { wrapMode = UnityEngine.TextureWrapMode.Clamp };
            for (var y = 0; y < 64; y++)
            {
                var t = 1f - y / 63f;
                shade.SetPixel(0, y, new UnityEngine.Color(0.03f, 0.035f, 0.05f, UnityEngine.Mathf.SmoothStep(0f, 0.9f, t)));
            }

            shade.Apply();
            var shadeElement = root.Q(className: "exp-art-shade");
            shadeElement.style.backgroundImage = new StyleBackground(shade);
            // 1x64 그림을 요소 크기 그대로 늘린다 (Cover면 가운데만 확대돼 경계가 생긴다)
            shadeElement.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(Length.Percent(100), Length.Percent(100)));
        }

        public bool IsOpen => !_sheet.ClassListContains("hidden");

        public void Show(ExpeditionRequest request, Action onDepart, Action onDispatch = null)
        {
            var db = _session.Definitions;
            var state = _session.State;
            var day = request.TimeOfDay == ExpeditionTime.Day;
            StoryArt.SetCover(_art, StoryArt.Location(request.LocationId));
            _sheet.EnableInClassList("night", !day);

            // ── 왼쪽: 목적지·예보 ──
            _info.Clear();
            Add(_info, Loc.T("prep.title"), "exp-kicker");
            Add(_info, Loc.T("location." + request.LocationId), "exp-location");
            Add(_info, Loc.T(day ? "exp.time_day" : "exp.time_night"), day ? "exp-time" : "exp-time night");
            Add(_info, Loc.T(day ? "prep.forecast_day" : "prep.forecast_night"), "exp-line");
            if (db.TryGetExpeditionModel(request.LocationId, out var model))
            {
                var y = day ? model.Day : model.Night;
                Add(_info, Loc.F("prep.cost", y.Fatigue, y.StressMin, Math.Max(y.StressMin, y.StressMax)), "exp-line");
            }

            if (!day)
            {
                Add(_info, Loc.T("prep.night_debt"), "exp-line");
            }

            var weather = state.Base.Weather;
            if (weather != WeatherType.Clear)
            {
                Add(_info, Loc.F("prep.weather", Loc.T("weather." + weather), Loc.T("weather." + weather + ".desc")), "exp-line");
            }

            if (ScoutRules.IsScouted(state, request.LocationId) && db.TryGetLocation(request.LocationId, out var location))
            {
                var variant = day ? location.DayVariant : location.NightVariant;
                Add(_info, Loc.F("prep.scouted", variant.LockedRooms.Count, variant.ForceableRooms.Count, variant.Encounters.Count), "exp-line");
            }

            Add(_info, Loc.T("prep.loss_warning"), "exp-warning");

            // ── 오른쪽: 장비 ──
            BuildLoadout();

            // ── 단추 ──
            _buttons.Clear();
            var depart = new Button(() =>
            {
                _session.RefreshPendingLoadout();
                Hide();
                onDepart();
            }) { text = Loc.T("prep.depart") };
            depart.AddToClassList("exp-depart");
            _buttons.Add(depart);
            if (onDispatch != null && _session.CanDispatch)
            {
                // 이야기 중심: 직접 들어가지 않고 자동 수색
                var dispatch = new Button(() =>
                {
                    _session.RefreshPendingLoadout();
                    Hide();
                    onDispatch();
                }) { text = Loc.T("prep.dispatch"), tooltip = Loc.F("prep.dispatch_hint", Math.Round(_session.Difficulty.DispatchYield * 100)) };
                dispatch.AddToClassList("exp-dispatch");
                _buttons.Add(dispatch);
            }

            KoreanFont.ApplyDeep(_sheet);
            _sheet.RemoveFromClassList("hidden");
            depart.Focus();
        }

        public void Hide() => _sheet.AddToClassList("hidden");

        private void BuildLoadout()
        {
            _loadout.Clear();
            var state = _session.State;
            var loadout = state.Loadout;
            LoadoutRules.Sanitize(state);

            AddSlot("prep.backpack", EquipSlot.Backpack, loadout.Backpack, id => loadout.Backpack = id);
            AddSlot("prep.weapon", EquipSlot.Weapon, loadout.MainWeapon, id => loadout.MainWeapon = id);
            AddSlot("prep.tool", EquipSlot.Tool, loadout.Tool, id => loadout.Tool = id);
            AddSlot("prep.armor", EquipSlot.Armor, loadout.Armor, id => loadout.Armor = id);

            Add(_loadout, Loc.T("prep.quick"), "exp-quick-label");
            var row = new VisualElement();
            row.AddToClassList("exp-quick-row");
            var options = new List<string> { "" };
            options.AddRange(Owned(EquipSlot.Consumable).Select(c => c.ItemId));
            for (var i = 0; i < QuickSlotCount; i++)
            {
                var index = i;
                var current = index < loadout.QuickSlots.Count ? loadout.QuickSlots[index] : "";
                var tile = new Button(() =>
                {
                    // 누르면 다음 소모품으로 (끝이면 비움)
                    var at = Math.Max(0, options.IndexOf(current));
                    var chosen = options[(at + 1) % options.Count];
                    while (loadout.QuickSlots.Count <= index)
                    {
                        loadout.QuickSlots.Add("");
                    }

                    loadout.QuickSlots[index] = chosen;
                    loadout.QuickSlots.RemoveAll(string.IsNullOrEmpty);
                    LoadoutRules.Sanitize(state);
                    BuildLoadout();
                }) { tooltip = NameOf(current) };
                tile.AddToClassList("exp-quick");
                var icon = new VisualElement { pickingMode = PickingMode.Ignore };
                icon.AddToClassList("exp-icon");
                StoryArt.SetContain(icon, StoryArt.ItemIcon(current));
                tile.Add(icon);
                var label = new Label(string.IsNullOrEmpty(current) ? Loc.T("prep.none") : NameOf(current)) { pickingMode = PickingMode.Ignore };
                label.AddToClassList("exp-quick-name");
                tile.Add(label);
                row.Add(tile);
            }

            _loadout.Add(row);
            KoreanFont.ApplyDeep(_loadout);

            // 차림이 바뀌면 입상도 (각오한 얼굴로)
            _standing.React("determined", 600f);
        }

        private void AddSlot(string labelKey, EquipSlot slot, string current, Action<string> set)
        {
            var options = new List<string> { "" };
            options.AddRange(Owned(slot).Select(o => o.ItemId));
            var index = Math.Max(0, options.IndexOf(current));

            var row = new VisualElement();
            row.AddToClassList("exp-slot");
            var icon = new VisualElement();
            icon.AddToClassList("exp-icon");
            StoryArt.SetContain(icon, StoryArt.ItemIcon(current));
            row.Add(icon);

            var text = new VisualElement();
            text.AddToClassList("exp-slot-text");
            Add(text, Loc.T(labelKey), "exp-slot-kind");
            Add(text, string.IsNullOrEmpty(current) ? Loc.T("prep.none") : Label(current), "exp-slot-name");
            var info = SlotInfo(current);
            if (!string.IsNullOrEmpty(info))
            {
                Add(text, info, "exp-slot-info");
            }

            row.Add(text);

            void Step(int direction)
            {
                if (options.Count <= 1)
                {
                    return;
                }

                set(options[(index + direction + options.Count) % options.Count]);
                LoadoutRules.Sanitize(_session.State);
                BuildLoadout();
            }

            var prev = new Button(() => Step(-1)) { text = "◀" };
            prev.AddToClassList("exp-arrow");
            prev.SetEnabled(options.Count > 1);
            var next = new Button(() => Step(1)) { text = "▶" };
            next.AddToClassList("exp-arrow");
            next.SetEnabled(options.Count > 1);
            row.Add(prev);
            row.Add(next);
            _loadout.Add(row);
        }

        private List<ItemStack> Owned(EquipSlot slot)
        {
            var db = _session.Definitions;
            return _session.State.Storage
                .Where(s => s.Count > 0 && db.TryGetItem(s.ItemId, out var def) && def.Slot == slot)
                .GroupBy(s => s.ItemId)
                .Select(g => new ItemStack { ItemId = g.Key, Count = g.Sum(s => s.Count), Durability = g.First().Durability })
                .ToList();
        }

        private string NameOf(string itemId) => string.IsNullOrEmpty(itemId) ? "" : Loc.Item(itemId);

        private string Label(string itemId)
        {
            var count = LoadoutRules.CountInStorage(_session.State, itemId);
            return Loc.Item(itemId) + (count > 1 ? " ×" + count : "");
        }

        private string SlotInfo(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || !_session.Definitions.TryGetItem(itemId, out var item))
            {
                return "";
            }

            switch (item.Slot)
            {
                case EquipSlot.Backpack:
                    return Loc.F("prep.info_backpack", item.GridWidth, item.GridHeight);
                case EquipSlot.Weapon:
                    var stack = _session.State.Storage.Find(s => s.ItemId == itemId);
                    var durability = stack != null && stack.Durability > 0 ? stack.Durability : item.MaxDurability;
                    return Loc.F("prep.info_weapon", item.WeaponPower, durability, item.MaxDurability);
                case EquipSlot.Armor:
                    return Loc.F("prep.info_armor", (int)Math.Round(item.DamageReduction * 100));
                case EquipSlot.Tool:
                    return Loc.T("tool." + item.Tool);
                default:
                    return "";
            }
        }

        private static Label Add(VisualElement parent, string text, string classes)
        {
            var label = new Label(text);
            foreach (var cssClass in classes.Split(' '))
            {
                label.AddToClassList(cssClass);
            }

            parent.Add(label);
            return label;
        }
    }
}
