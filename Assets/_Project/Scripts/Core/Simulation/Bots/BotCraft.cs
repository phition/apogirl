using System;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Simulation.Bots
{
    /// <summary>
    /// 봇의 제작 판단 (제작 P7, Docs/CRAFTING_PROPOSAL.md 4.8): 묶음 CRAFT의 후보 중 지금 만들 만한 것인가.
    /// 착수할 수 있어야 하고(CraftRules.CheckStart), 물건이면 같은 칸에 그만한 것을 이미 가졌으면 만들지 않는다.
    /// 설비(작업대·정수·허수아비)는 해금 조건의 FlagsNone이, 보관 상한·손질할 무기는 CheckStart가 막는다.
    /// </summary>
    public static class BotCraft
    {
        public static bool Wanted(GameState state, IDefinitionDatabase db, ActionDef action)
        {
            if (action == null || !action.IsCraft)
            {
                return true;
            }

            if (CraftRules.CheckStart(state, db, action) != CraftBlock.None)
            {
                return false;
            }

            var craft = action.Craft;
            if (string.IsNullOrEmpty(craft.Product) || !db.TryGetItem(craft.Product, out var product))
            {
                return true;
            }

            // 헤드리스 기준 칸보다 작은 가방은 차도 수확이 줄 뿐이라 만들지 않는다
            if (product.Slot == EquipSlot.Backpack && product.GridWidth * product.GridHeight <= db.Balance.HeadlessBagBaseCells)
            {
                return false;
            }

            var score = Score(state, product);
            if (score <= 0f)
            {
                // 칸이 없는 물건(휴대 허수아비 등): 보관 상한이 없으면 하나만
                return craft.MaxOwned > 0 || LoadoutRules.CountInStorage(state, product.Id) == 0;
            }

            // 같은 칸에 그만한(점수가 같거나 높은) 물건이 창고에 있으면 만들지 않는다
            return !state.Storage.Any(stack => stack.Count > 0 && db.TryGetItem(stack.ItemId, out var owned)
                                               && owned.Slot == product.Slot && Score(state, owned) >= score);
        }

        /// <summary>칸별 점수 (BotLoadout과 같은 잣대): 가방 = 칸 수, 무기 = 위력 × 남은 내구, 방어구 = 감소율, 도구 = 하는 일 + 빛 거리. 칸 없는 물건은 0.</summary>
        public static float Score(GameState state, ItemDef def)
        {
            switch (def.Slot)
            {
                case EquipSlot.Backpack:
                    return def.GridWidth * def.GridHeight;
                case EquipSlot.Weapon:
                    return BotLoadout.WeaponScore(state, def);
                case EquipSlot.Armor:
                    return def.DamageReduction;
                case EquipSlot.Tool:
                    return (def.Does(ToolKind.Flashlight) ? 1f + Math.Max(0f, def.LightRange) : 0f) + (def.Does(ToolKind.Crowbar) ? 1f : 0f);
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// 밤 칸에는 창고의 휴대 허수아비 하나를 "챙길 것"에 넣는다 (오래 버티는 것 먼저, 배낭 칸 안에서만). 낮 칸에는 비운다
        /// (헤드리스는 밤 탐험에서만 쓰고, 들고 나가 쓰러지면 잃기 때문).
        /// </summary>
        public static void ChooseCarry(GameState state, IDefinitionDatabase db, bool night)
        {
            var carry = state.Loadout.Carry;
            carry.RemoveAll(id => db.TryGetItem(id, out var def) && def.IsDecoy);
            if (!night)
            {
                return;
            }

            ItemDef best = null;
            foreach (var stack in state.Storage)
            {
                if (stack.Count > 0 && db.TryGetItem(stack.ItemId, out var def) && def.IsDecoy && (best == null || def.Decoy.Seconds > best.Decoy.Seconds))
                {
                    best = def;
                }
            }

            if (best == null)
            {
                return;
            }

            carry.Add(best.Id);
            if (LoadoutRules.CarryCells(state.Loadout, db) > LoadoutRules.BagCells(state.Loadout, db))
            {
                carry.Remove(best.Id);
            }
        }
    }
}
