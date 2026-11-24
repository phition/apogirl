using System;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Simulation.Bots
{
    /// <summary>
    /// 봇의 장비 고르기 (제작 C5, Docs/CRAFTING_PROPOSAL.md 4.8): 칸을 실행하기 직전마다 창고에서 고른다.
    /// "Best"(기본): 가방 = 칸 수, 무기 = 위력 × 남은 내구 비율, 방어구 = 피해 감소율, 도구 = 밤이면 손전등 > 지렛대, 낮이면 지렛대 > 손전등.
    /// "None": 손대지 않는다 (사람이 고르지 않은 것처럼 빈 칸 그대로).
    /// </summary>
    public static class BotLoadout
    {
        public const string Best = "Best";

        public static void Choose(GameState state, IDefinitionDatabase db, BotStrategyDef strategy, bool night)
        {
            if (strategy == null || !string.Equals(strategy.Loadout ?? Best, Best, StringComparison.Ordinal))
            {
                return;
            }

            var loadout = state.Loadout;
            loadout.Backpack = Pick(state, db, EquipSlot.Backpack, loadout.Backpack, def => def.GridWidth * def.GridHeight);
            loadout.MainWeapon = Pick(state, db, EquipSlot.Weapon, loadout.MainWeapon, def => WeaponScore(state, def));
            loadout.Armor = Pick(state, db, EquipSlot.Armor, loadout.Armor, def => def.DamageReduction);
            loadout.Tool = Pick(state, db, EquipSlot.Tool, loadout.Tool, def => ToolScore(def, night, null));
            // 둘째 도구 칸: 첫 칸이 못 하는 일 중 가장 나은 것 (멀티툴이면 빛·지렛대를 이미 다 한다)
            var first = !string.IsNullOrEmpty(loadout.Tool) && db.TryGetItem(loadout.Tool, out var firstDef) ? firstDef : null;
            loadout.Tool2 = Pick(state, db, EquipSlot.Tool, loadout.Tool2, def => ToolScore(def, night, first));
            if (loadout.Tool2 == loadout.Tool)
            {
                loadout.Tool2 = "";
            }
        }

        /// <summary>그 칸에 맞는 창고 물건 중 점수가 가장 높은 것 (같으면 지금 찬 것, 그다음 창고 앞쪽). 점수 0 이하뿐이면 지금 것.</summary>
        private static string Pick(GameState state, IDefinitionDatabase db, EquipSlot slot, string current, Func<ItemDef, float> score)
        {
            var best = current ?? "";
            var bestScore = !string.IsNullOrEmpty(best) && LoadoutRules.CountInStorage(state, best) > 0 && db.TryGetItem(best, out var currentDef)
                ? score(currentDef)
                : 0f;
            foreach (var stack in state.Storage)
            {
                if (stack.Count <= 0 || !db.TryGetItem(stack.ItemId, out var def) || def.Slot != slot)
                {
                    continue;
                }

                var value = score(def);
                if (value > bestScore)
                {
                    best = def.Id;
                    bestScore = value;
                }
            }

            return best;
        }

        /// <summary>위력 × 남은 내구 비율 (찬 무기 내구는 그 ID의 맨 앞 묶음, 0이면 새것).</summary>
        internal static float WeaponScore(GameState state, ItemDef def)
        {
            if (def.WeaponPower <= 0)
            {
                return 0f;
            }

            if (def.MaxDurability <= 0)
            {
                return def.WeaponPower;
            }

            var stack = state.Storage.Find(s => s.ItemId == def.Id);
            var left = stack == null || stack.Durability <= 0 ? def.MaxDurability : Math.Min(stack.Durability, def.MaxDurability);
            return def.WeaponPower * (float)left / def.MaxDurability;
        }

        /// <summary>도구 점수: 하는 일마다 (밤엔 빛 2·지렛대 1, 낮엔 반대) + 빛이 더 멀리 닿는 만큼. covered가 이미 하는 일은 뺀다.</summary>
        private static float ToolScore(ItemDef def, bool night, ItemDef covered)
        {
            var score = 0f;
            if (def.Does(ToolKind.Flashlight) && (covered == null || !covered.Does(ToolKind.Flashlight)))
            {
                score += (night ? 2f : 1f) + Math.Max(0f, def.LightRange - 1f);
            }

            if (def.Does(ToolKind.Crowbar) && (covered == null || !covered.Does(ToolKind.Crowbar)))
            {
                score += night ? 1f : 2f;
            }

            return score;
        }
    }
}
