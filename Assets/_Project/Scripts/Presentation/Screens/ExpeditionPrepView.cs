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
    /// 오른쪽은 지금 차림의 주인공 입상과 장비 칸(배낭·주무기·도구·방어구, ◀ ▶로 바꾸기).
    /// 그 아래 "챙길 것"(제작 P6): 창고 소모품(붕대·허수아비 등)을 배낭에 미리 넣는다 (누르면 하나 더, 우클릭이면 하나 빼기).
    /// 장비를 바꾸면 입상도 바로 바뀐다.
    /// </summary>
    internal sealed class ExpeditionPrepView
    {
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

            // 계절 재해 (확장 6): 지도가 바뀌는 지역이면 안내
            var disaster = DisasterRules.Active(state, db.Balance);
            if (disaster != null && db.TryGetLocation(request.LocationId, out var hit) && !ReferenceEquals(DisasterRules.Apply(hit, disaster), hit))
            {
                Add(_info, Loc.T("prep.disaster." + disaster.Id), "exp-line");
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
            AddSlot("prep.tool", EquipSlot.Tool, loadout.Tool, id => loadout.Tool = id, loadout.Tool2);
            AddSlot("prep.tool2", EquipSlot.Tool, loadout.Tool2, id => loadout.Tool2 = id, loadout.Tool);
            AddSlot("prep.armor", EquipSlot.Armor, loadout.Armor, id => loadout.Armor = id);
            BuildCarry();

            KoreanFont.ApplyDeep(_loadout);

            // 차림이 바뀌면 입상도 (각오한 얼굴로)
            _standing.React("determined", 600f);
        }

        /// <summary>장비 한 칸 (◀ ▶로 고른다). other: 같은 종류 다른 칸에 찬 것 (도구 두 칸은 서로 다른 도구만).</summary>
        private void AddSlot(string labelKey, EquipSlot slot, string current, Action<string> set, string other = "")
        {
            var options = new List<string> { "" };
            options.AddRange(Owned(slot).Select(o => o.ItemId).Where(id => string.IsNullOrEmpty(other) || id != other));
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

        /// <summary>챙길 것: 창고 소모품 종류마다 한 칸 (아이콘·이름·챙김/창고 수). 배낭 칸을 넘지 않게 담는다.</summary>
        private void BuildCarry()
        {
            var db = _session.Definitions;
            var loadout = _session.State.Loadout;
            var kinds = _session.State.Storage
                .Where(s => s.Count > 0 && db.TryGetItem(s.ItemId, out var def) && LoadoutRules.IsCarryable(def))
                .Select(s => s.ItemId)
                .Distinct()
                .ToList();
            if (kinds.Count == 0)
            {
                return;
            }

            var cells = LoadoutRules.BagCells(loadout, db);
            var used = LoadoutRules.CarryCells(loadout, db);
            var title = Add(_loadout, Loc.F("prep.carry", used, cells), "exp-quick-label");
            title.tooltip = Loc.T("prep.carry_hint");
            var row = new VisualElement();
            row.AddToClassList("exp-quick-row");
            foreach (var id in kinds)
            {
                db.TryGetItem(id, out var item);
                var taken = loadout.Carry.Count(c => c == id);
                var stock = LoadoutRules.CountInStorage(_session.State, id);
                var size = Math.Max(1, item.Width) * Math.Max(1, item.Height);
                var tile = new Button(() =>
                {
                    if (taken < stock && used + size <= cells)
                    {
                        loadout.Carry.Add(id);
                        BuildLoadout();
                    }
                }) { tooltip = Loc.Item(id) + " — " + Loc.T("prep.carry_hint") };
                tile.AddToClassList("exp-quick");
                tile.EnableInClassList("exp-quick-on", taken > 0);
                tile.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button == 1 && loadout.Carry.Remove(id))
                    {
                        evt.StopPropagation();
                        BuildLoadout();
                    }
                }, TrickleDown.TrickleDown);

                var icon = new VisualElement();
                icon.AddToClassList("exp-icon");
                StoryArt.SetContain(icon, StoryArt.ItemIcon(id));
                tile.Add(icon);
                Add(tile, Loc.Item(id), "exp-quick-name");
                Add(tile, taken + " / " + stock, "exp-quick-name");
                row.Add(tile);
            }

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
                    return Join(Loc.F("prep.info_armor", (int)Math.Round(item.DamageReduction * 100)), Left(item));
                case EquipSlot.Tool:
                    var tool = Join(UiText.ToolKinds(item), Left(item));
                    if (item.UsesPower && LeftValue(item) <= 0)
                    {
                        // 전력이 바닥난 빛 도구: 출정 때 거점 배터리로 채운다
                        tool = Join(tool, Loc.T(_session.State.Base.GetResource(ResourceType.Battery) > 0 ? "prep.power_charge" : "prep.power_dead"));
                    }

                    return tool;
                default:
                    return "";
            }
        }

        /// <summary>창고의 그 장비(맨 앞 묶음)의 남은 내구·전력.</summary>
        private int LeftValue(ItemDef item)
        {
            var stack = _session.State.Storage.Find(s => s.ItemId == item.Id);
            return item.LeftOf(stack?.Durability ?? 0);
        }

        private string Left(ItemDef item) => item.MaxDurability > 0 ? UiText.Durability(item, LeftValue(item)) : "";

        private static string Join(string a, string b) => string.IsNullOrEmpty(b) ? a : string.IsNullOrEmpty(a) ? b : a + " · " + b;

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
