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
    /// 오른쪽 뒤에는 준비하는 자리 그림(거처별, art_map.json PrepScenes)을 깔고 어둠 막으로 덮는다 — 그림이 없으면 예전 단색 판.
    /// 그 아래 "챙길 것"(제작 P6): 창고 소모품(붕대·허수아비 등)을 배낭에 미리 넣는다 (누르면 하나 더, 우클릭이면 하나 빼기).
    /// 장비를 바꾸면 입상도 바로 바뀐다.
    /// </summary>
    internal sealed class ExpeditionPrepView
    {
        private readonly GameSession _session;
        private readonly VisualElement _sheet;
        private readonly VisualElement _art;
        private readonly VisualElement _side;
        private readonly VisualElement _sideArt;
        private readonly VisualElement _info;
        private readonly VisualElement _loadout;
        private readonly VisualElement _buttons;
        private readonly StandingView _standing;

        public ExpeditionPrepView(VisualElement root, GameSession session)
        {
            _session = session;
            _sheet = root.Q("expedition-sheet");
            _art = root.Q("exp-art");
            _side = root.Q("exp-side");
            _sideArt = root.Q("exp-side-art");
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

            // 준비하는 자리 그림 위의 어둠 막: 입상 쪽(왼쪽)은 옅게, 장비 칸 쪽(오른쪽)은 짙게
            var veilElement = root.Q("exp-side-veil");
            if (veilElement != null)
            {
                var veil = new UnityEngine.Texture2D(64, 1, UnityEngine.TextureFormat.RGBA32, false) { wrapMode = UnityEngine.TextureWrapMode.Clamp };
                for (var x = 0; x < 64; x++)
                {
                    veil.SetPixel(x, 0, new UnityEngine.Color(0.05f, 0.075f, 0.065f, UnityEngine.Mathf.SmoothStep(VeilLeft, VeilRight, x / 63f)));
                }

                veil.Apply();
                veilElement.style.backgroundImage = new StyleBackground(veil);
                veilElement.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(Length.Percent(100), Length.Percent(100)));
            }
        }

        /// <summary>
        /// 어둠 막의 짙기 (왼쪽 끝 → 오른쪽 끝). 선형 색공간에서 섞여 눈에 보이는 밝기는 (1 − 짙기)^0.45쯤이다
        /// — 0.5면 그림이 70%, 0.87이면 40% 밝기로 보인다.
        /// </summary>
        private const float VeilLeft = 0.5f;
        private const float VeilRight = 0.87f;

        public bool IsOpen => !_sheet.ClassListContains("hidden");

        public void Show(ExpeditionRequest request, Action onDepart, Action onDispatch = null)
        {
            var db = _session.Definitions;
            var state = _session.State;
            var day = request.TimeOfDay == ExpeditionTime.Day;
            StoryArt.SetCover(_art, StoryArt.Location(request.LocationId));
            _sheet.EnableInClassList("night", !day);

            // 오른쪽 뒤: 준비하는 자리 (밤 그림이 없으면 낮 그림을 어둡게 물들인다)
            var scene = StoryArt.PrepScene(state, !day, out var nightArt, out var focusX);
            StoryArt.SetCover(_sideArt, scene, focusX);
            _side?.EnableInClassList("has-art", scene != null && _sideArt != null);
            _sideArt?.EnableInClassList("tint-night", !day && !nightArt);

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
            BuildEntry(request, disaster);
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

        /// <summary>후문 진입을 고른 지역 (이번 실행 동안 기억, 저장하지 않음).</summary>
        private static readonly HashSet<string> RearChoice = new HashSet<string>();

        /// <summary>
        /// 정문·후문 진입: 쇠지렛대로 열어 둔 탈출로가 있는 학교 밖 지역만 (ExitRules). 후문이면 탈출로 끝에서 시작한다.
        /// 재해가 다시 막은 후문(폭설)은 고를 수 없다.
        /// </summary>
        private void BuildEntry(ExpeditionRequest request, DisasterDef disaster)
        {
            var state = _session.State;
            request.RearEntry = false;
            if (!ExitRules.IsOpened(state.Flags, request.LocationId) || !_session.Definitions.TryGetLocation(request.LocationId, out var plain)
                || !ExitRules.CanEnterRear(ExitRules.RunMap(plain, state.Flags, disaster)))
            {
                return;
            }

            request.RearEntry = RearChoice.Contains(request.LocationId);
            Add(_buttons, Loc.T("prep.entry"), "exp-quick-label");
            var row = new VisualElement();
            row.AddToClassList("exp-entry-row");
            Button front = null;
            Button rear = null;
            void Pick(bool back)
            {
                request.RearEntry = back;
                if (back) RearChoice.Add(request.LocationId);
                else RearChoice.Remove(request.LocationId);
                front.EnableInClassList("exp-quick-on", !back);
                rear.EnableInClassList("exp-quick-on", back);
            }

            front = new Button(() => Pick(false)) { text = Loc.T("prep.entry_front"), tooltip = Loc.T("prep.entry_front_hint") };
            rear = new Button(() => Pick(true)) { text = Loc.T("prep.entry_rear"), tooltip = Loc.T("prep.entry_rear_hint") };
            front.AddToClassList("exp-entry");
            rear.AddToClassList("exp-entry");
            row.Add(front);
            row.Add(rear);
            _buttons.Add(row);
            Pick(request.RearEntry);
        }

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

        /// <summary>
        /// 장비 한 칸 (◀ ▶로 고른다). other: 같은 종류 다른 칸에 찬 것 (도구 두 칸은 서로 다른 도구만).
        /// 같은 장비가 내구도별로 여럿이면 내구도마다 한 칸씩 고른다 (고르면 그 묶음을 창고 맨 앞으로, LoadoutRules.BringToFront).
        /// </summary>
        private void AddSlot(string labelKey, EquipSlot slot, string current, Action<string> set, string other = "")
        {
            var options = new List<(string Id, int Left)> { ("", 0) };
            options.AddRange(Choices(slot).Where(o => string.IsNullOrEmpty(other) || o.Id != other));
            var currentLeft = CurrentLeft(current);
            var index = Math.Max(0, options.FindIndex(o => o.Id == (current ?? "") && (o.Id.Length == 0 || o.Left == currentLeft)));

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

                var pick = options[(index + direction + options.Count) % options.Count];
                if (pick.Id.Length > 0 && _session.Definitions.TryGetItem(pick.Id, out var picked))
                {
                    LoadoutRules.BringToFront(_session.State, picked, pick.Left);
                }

                set(pick.Id);
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

        /// <summary>그 칸에 찰 수 있는 것: 장비 ID마다, 내구가 있으면 남은 내구도마다 하나 (많이 남은 것부터).</summary>
        private List<(string Id, int Left)> Choices(EquipSlot slot)
        {
            var db = _session.Definitions;
            var result = new List<(string Id, int Left)>();
            foreach (var group in _session.State.Storage
                         .Where(s => s.Count > 0 && db.TryGetItem(s.ItemId, out var def) && def.Slot == slot)
                         .GroupBy(s => s.ItemId))
            {
                db.TryGetItem(group.Key, out var item);
                result.AddRange(group.Select(s => item.LeftOf(s.Durability)).Distinct().OrderByDescending(l => l).Select(l => (group.Key, l)));
            }

            return result;
        }

        /// <summary>찬 장비의 남은 내구 (그 ID의 맨 앞 묶음).</summary>
        private int CurrentLeft(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || !_session.Definitions.TryGetItem(itemId, out var item))
            {
                return 0;
            }

            var stack = _session.State.Storage.Find(s => s.ItemId == itemId && s.Count > 0);
            return item.LeftOf(stack?.Durability ?? 0);
        }


        /// <summary>장비 이름 ×개수. 내구도가 다른 같은 장비가 있으면 지금 고른 내구도의 개수와 "다른 내구 n개".</summary>
        private string Label(string itemId)
        {
            var count = LoadoutRules.CountInStorage(_session.State, itemId);
            if (count > 1 && _session.Definitions.TryGetItem(itemId, out var item) && item.MaxDurability > 0)
            {
                var left = CurrentLeft(itemId);
                var same = _session.State.Storage.Where(s => s.ItemId == itemId && item.LeftOf(s.Durability) == left).Sum(s => s.Count);
                if (same < count)
                {
                    return Loc.Item(itemId) + (same > 1 ? " ×" + same : "") + " " + Loc.F("prep.other_durability", count - same);
                }
            }

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
