using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.State;
using ApoGirl.Presentation.Art;
using ApoGirl.Presentation.Text;
using ApoGirl.Systems.Session;
using UnityEngine.UIElements;

namespace ApoGirl.Presentation.Screens
{
    /// <summary>
    /// 보관함 덧판: 거점 서랍 왼쪽 가장자리의 화살표 탭을 누르면 왼쪽으로 펼쳐진다 (세력 덧판과 같은 꼴, 2026-11-25 사용자 요청 —
    /// 예전에는 생활기록부 서랍과 함께 열렸다). 거점 자원(배터리 포함)과 창고 아이템 적재 현황.
    /// 아이템은 장비·소모품·기호품·기타로 묶고, 찬 장비는 "착용 중", 무기는 남은 내구도를 붙인다.
    /// 글꼴은 거점 서랍(#drawer-right)의 USS를 따르니 KoreanFont.ApplyDeep을 쓰지 않는다.
    /// </summary>
    internal sealed class StorageView
    {
        // 상단 자원줄과 같은 순서 + 상단에서 뺀 배터리. 전력은 쓰면 줄지 않는 용량이라 여기 두지 않는다
        private static readonly ResourceType[] Resources =
        {
            ResourceType.Food, ResourceType.Water, ResourceType.RawWater, ResourceType.Medicine, ResourceType.Materials, ResourceType.Fuel, ResourceType.Battery,
            ResourceType.Ammo, ResourceType.Intel,
        };

        private static readonly string[] Groups = { "equipment", "consumable", "liked", "other" };

        private readonly GameSession _session;
        private readonly VisualElement _drawer;
        private readonly VisualElement _addon;
        private readonly VisualElement _sheet;
        private readonly Button _tab;
        private readonly ScrollView _list;
        private string _shownKey;
        private bool _expanded;

        public StorageView(VisualElement root, GameSession session)
        {
            _session = session;
            _drawer = root.Q("drawer-right");
            _addon = root.Q("base-storage");
            _sheet = root.Q("base-storage-sheet");
            _tab = root.Q<Button>("base-storage-tab");
            _list = root.Q<ScrollView>("storage-list");
            root.Q<Label>("storage-title").text = Loc.T("storage.title");
            if (_tab != null)
            {
                _tab.clicked += () =>
                {
                    _expanded = !_expanded;
                    Sync();
                    Refresh();
                };
            }

            Sync();
        }

        private void Sync()
        {
            _sheet?.EnableInClassList("hidden", !_expanded);
            _addon?.EnableInClassList("expanded", _expanded);
            if (_tab != null)
            {
                _tab.text = Loc.T(_expanded ? "storage.close" : "storage.open");
                _tab.tooltip = Loc.T("storage.hint");
            }
        }

        public void Refresh()
        {
            var state = _session.State;
            // 거점 서랍이 닫혔거나 덧판을 접어 두었으면 그리지 않는다 (펼칠 때 다시 부른다)
            if (_list == null || state == null || !_expanded || (_drawer != null && _drawer.ClassListContains("closed")))
            {
                return;
            }

            // 바뀐 게 없으면 다시 그리지 않는다
            var key = string.Join(",", Resources.Select(r => state.Base.GetResource(r)))
                      + "|" + string.Join(",", state.Storage.Select(s => s.ItemId + "x" + s.Count + "d" + s.Durability))
                      + "|" + state.Loadout.Backpack + state.Loadout.MainWeapon + state.Loadout.Tool + state.Loadout.Tool2 + state.Loadout.Armor;
            if (key == _shownKey)
            {
                return;
            }

            _shownKey = key;
            _list.Clear();

            // ── 자원 ──
            AddHead(Loc.T("storage.resources"));
            var resources = new VisualElement();
            resources.AddToClassList("storage-chips");
            foreach (var type in Resources)
            {
                var amount = state.Base.GetResource(type);
                if (amount <= 0 && type != ResourceType.Food && type != ResourceType.Water && type != ResourceType.Battery)
                {
                    continue;
                }

                var chip = new Label(UiText.Resource(type) + " " + amount);
                chip.AddToClassList("storage-chip");
                chip.EnableInClassList("zero", amount <= 0);
                StoryArt.SetIcon(chip, StoryArt.Icon("res." + type), 20f);
                resources.Add(chip);
            }

            _list.Add(resources);

            // ── 아이템: 같은 것끼리 합쳐 종류별로 ──
            var db = _session.Definitions;
            var liked = new HashSet<string>(db.Balance.LikedItems.SelectMany(l => l.Items));
            var equipped = new HashSet<string>(new[] { state.Loadout.Backpack, state.Loadout.MainWeapon, state.Loadout.Tool, state.Loadout.Tool2, state.Loadout.Armor }.Where(id => !string.IsNullOrEmpty(id)));
            var items = state.Storage.Where(s => s.Count > 0)
                .GroupBy(s => s.ItemId)
                .Select(g => (Id: g.Key, Count: g.Sum(s => s.Count), Durability: g.First().Durability)) // 찬 장비 = 그 ID의 맨 앞 묶음
                .ToList();

            foreach (var group in Groups)
            {
                var rows = items.Where(i => GroupOf(i.Id, db, liked) == group).OrderBy(i => Loc.Item(i.Id)).ToList();
                if (rows.Count == 0)
                {
                    continue;
                }

                AddHead(Loc.T("storage." + group));
                foreach (var (id, count, durability) in rows)
                {
                    var row = new VisualElement();
                    row.AddToClassList("storage-row");
                    var icon = new VisualElement();
                    icon.AddToClassList("storage-icon");
                    StoryArt.SetContain(icon, StoryArt.ItemIcon(id));
                    row.Add(icon);
                    var name = new Label(Loc.Item(id) + (count > 1 ? " ×" + count : ""));
                    name.AddToClassList("storage-name");
                    row.Add(name);
                    var note = Note(id, durability, equipped.Contains(id), db);
                    if (note.Length > 0)
                    {
                        var label = new Label(note);
                        label.AddToClassList("storage-note");
                        label.EnableInClassList("equipped", equipped.Contains(id));
                        row.Add(label);
                    }

                    _list.Add(row);
                }
            }

            if (items.Count == 0)
            {
                AddHead(Loc.T("storage.items"));
                var empty = new Label(Loc.T("storage.empty"));
                empty.AddToClassList("storage-note");
                _list.Add(empty);
            }

        }

        private static string GroupOf(string itemId, IDefinitionDatabase db, HashSet<string> liked)
        {
            if (liked.Contains(itemId))
            {
                return "liked";
            }

            if (!db.TryGetItem(itemId, out var item))
            {
                return "other";
            }

            switch (item.Slot)
            {
                case EquipSlot.Weapon:
                case EquipSlot.Backpack:
                case EquipSlot.Tool:
                case EquipSlot.Armor:
                    return "equipment";
                case EquipSlot.Consumable:
                    return "consumable";
                default:
                    return item.UseHp > 0 || item.UseStress > 0 ? "consumable" : "other";
            }
        }

        /// <summary>오른쪽 작은 글: 착용 중, 무기 내구도.</summary>
        private static string Note(string itemId, int durability, bool equipped, IDefinitionDatabase db)
        {
            var parts = new List<string>();
            if (equipped)
            {
                parts.Add(Loc.T("storage.equipped"));
            }

            if (db.TryGetItem(itemId, out var item) && item.MaxDurability > 0)
            {
                parts.Add(UiText.Durability(item, item.LeftOf(durability)));
            }

            return string.Join(" · ", parts);
        }

        private void AddHead(string text)
        {
            var head = new Label(text);
            head.AddToClassList("storage-head");
            _list.Add(head);
        }
    }
}
