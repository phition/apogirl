using System;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;
using ApoGirl.Presentation.Text;
using ApoGirl.Systems.Session;
using UnityEngine.UIElements;

namespace ApoGirl.Presentation.Screens
{
    /// <summary>
    /// 출정 준비 (문서 6.3.4-1, M4 전체 로드아웃): 목적지·시간대 정보와 예보, 배낭·주무기·도구·방어구·퀵슬롯 선택.
    /// 쓰러지면 가져간 장비를 모두 잃는다는 경고를 함께 보여준다. 카드 오버레이의 추가 영역에 그린다.
    /// </summary>
    internal sealed class ExpeditionPrepView
    {
        private const int QuickSlotCount = 4;

        private readonly GameSession _session;
        private readonly CardOverlay _card;

        public ExpeditionPrepView(GameSession session, CardOverlay card)
        {
            _session = session;
            _card = card;
        }

        public void Show(ExpeditionRequest request, Action onDepart, Action onDispatch = null)
        {
            var db = _session.Definitions;
            var day = request.TimeOfDay == ExpeditionTime.Day;
            var lines = new List<string>
            {
                Loc.T("location." + request.LocationId) + " · " + Loc.T(day ? "exp.time_day" : "exp.time_night"),
                Loc.T(day ? "prep.forecast_day" : "prep.forecast_night"),
            };

            if (db.TryGetExpeditionModel(request.LocationId, out var model))
            {
                var y = day ? model.Day : model.Night;
                lines.Add(Loc.F("prep.cost", y.Fatigue, y.StressMin, Math.Max(y.StressMin, y.StressMax)));
            }

            if (!day)
            {
                lines.Add(Loc.T("prep.night_debt"));
            }

            // M6: 날씨, 정찰 정보 (출정 한 번에 소모)
            var weather = _session.State.Base.Weather;
            if (weather != WeatherType.Clear)
            {
                lines.Add(Loc.F("prep.weather", Loc.T("weather." + weather), Loc.T("weather." + weather + ".desc")));
            }

            if (ScoutRules.IsScouted(_session.State, request.LocationId) && db.TryGetLocation(request.LocationId, out var location))
            {
                var variant = day ? location.DayVariant : location.NightVariant;
                lines.Add(Loc.F("prep.scouted", variant.LockedRooms.Count, variant.ForceableRooms.Count, variant.Encounters.Count));
            }

            lines.Add(Loc.T("prep.loss_warning"));
            (string, Action) depart = (Loc.T("prep.depart"), () =>
            {
                _session.RefreshPendingLoadout();
                _card.Hide();
                onDepart();
            });
            if (onDispatch != null && _session.CanDispatch)
            {
                // 이야기 중심: 직접 들어가지 않고 자동 수색
                lines.Add(Loc.F("prep.dispatch_hint", Math.Round(_session.Difficulty.DispatchYield * 100)));
                _card.Show(Loc.T("prep.title"), string.Join("\n", lines), false, depart, (Loc.T("prep.dispatch"), () =>
                {
                    _session.RefreshPendingLoadout();
                    _card.Hide();
                    onDispatch();
                }));
            }
            else
            {
                _card.Show(Loc.T("prep.title"), string.Join("\n", lines), false, depart);
            }

            BuildLoadout(_card.Extra);
        }

        private void BuildLoadout(VisualElement extra)
        {
            extra.Clear();
            var state = _session.State;
            var loadout = state.Loadout;
            LoadoutRules.Sanitize(state);

            AddSlot(extra, "prep.backpack", EquipSlot.Backpack, loadout.Backpack, id => loadout.Backpack = id);
            AddSlot(extra, "prep.weapon", EquipSlot.Weapon, loadout.MainWeapon, id => loadout.MainWeapon = id);
            AddSlot(extra, "prep.tool", EquipSlot.Tool, loadout.Tool, id => loadout.Tool = id);
            AddSlot(extra, "prep.armor", EquipSlot.Armor, loadout.Armor, id => loadout.Armor = id);

            var consumables = Owned(EquipSlot.Consumable);
            var row = new VisualElement();
            row.AddToClassList("prep-row");
            row.Add(new Label(Loc.T("prep.quick")) { name = "prep-quick-label" });
            for (var i = 0; i < QuickSlotCount; i++)
            {
                var index = i;
                var current = index < loadout.QuickSlots.Count ? loadout.QuickSlots[index] : "";
                var options = new List<string> { "" };
                options.AddRange(consumables.Select(c => c.ItemId));
                var field = new DropdownField(options.Select(Label).ToList(), Math.Max(0, options.IndexOf(current)));
                field.AddToClassList("prep-quick");
                FieldStyle.Dark(field);
                field.RegisterValueChangedCallback(_ =>
                {
                    var chosen = options[Math.Max(0, field.index)];
                    while (loadout.QuickSlots.Count <= index)
                    {
                        loadout.QuickSlots.Add("");
                    }

                    loadout.QuickSlots[index] = chosen;
                    loadout.QuickSlots.RemoveAll(string.IsNullOrEmpty);
                    LoadoutRules.Sanitize(state);
                    BuildLoadout(extra);
                });
                row.Add(field);
            }

            extra.Add(row);
        }

        private void AddSlot(VisualElement parent, string labelKey, EquipSlot slot, string current, Action<string> set)
        {
            var owned = Owned(slot);
            var options = new List<string> { "" };
            options.AddRange(owned.Select(o => o.ItemId));
            var row = new VisualElement();
            row.AddToClassList("prep-row");
            row.Add(new Label(Loc.T(labelKey)));
            var field = new DropdownField(options.Select(Label).ToList(), Math.Max(0, options.IndexOf(current)));
            field.AddToClassList("prep-field");
            FieldStyle.Dark(field);
            field.RegisterValueChangedCallback(_ =>
            {
                set(options[Math.Max(0, field.index)]);
                LoadoutRules.Sanitize(_session.State);
            });
            row.Add(field);
            var info = new Label(SlotInfo(current));
            info.AddToClassList("prep-info");
            field.RegisterValueChangedCallback(_ => info.text = SlotInfo(options[Math.Max(0, field.index)]));
            row.Add(info);
            parent.Add(row);
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
            if (string.IsNullOrEmpty(itemId))
            {
                return Loc.T("prep.none");
            }

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
    }
}
