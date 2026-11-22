using System;
using System.Collections.Generic;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Events;
using ApoGirl.Core.Random;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>
    /// 보정 합산: 특성 (문서 7.1 ③⑥) + 시설 레벨 (M6) + 합류한 동료의 역할 (M6). 정의는 모두 데이터.
    /// </summary>
    public static class TraitRules
    {
        /// <summary>
        /// 정수로 쓰는 보정(식량 절약·감염 감소)의 소수점을 시간에 나눠 쌓는다: step번째 몫 = ⌊v·step⌋ − ⌊v·(step−1)⌋.
        /// 0.5면 0,1,0,1…, 1.25면 1,1,1,2…, 1.5면 1,2,1,2… (평균이 정확히 v). 음수는 0.
        /// </summary>
        public static int Spread(float value, int step)
        {
            if (value <= 0f)
            {
                return 0;
            }

            step = Math.Max(1, step);
            return (int)Math.Floor(value * (double)step + 1e-6) - (int)Math.Floor(value * (double)(step - 1) + 1e-6);
        }

        /// <summary>한 번에 정하는 정수 보정 (습격 방어): .5는 올린다 (은행가 반올림으로 7.5가 8, 6.5가 6이 되지 않게).</summary>
        public static int RoundInt(float value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);

        public static float Sum(GameState state, IDefinitionDatabase db, TraitModifierKind kind, string key = "")
        {
            var total = 0f;
            foreach (var traitId in state.Player.Traits)
            {
                var trait = db.Extras.Traits.Find(t => t.Id == traitId);
                if (trait != null)
                {
                    total += Match(trait.Modifiers, kind, key);
                }
            }

            foreach (var facility in db.Extras.Facilities)
            {
                if (facility.Modifiers.Count > 0 && state.Base.Facilities.TryGetValue(facility.Id, out var level) && level > 0)
                {
                    total += Match(facility.Modifiers, kind, key) * level;
                }
            }

            if (db.Extras.Roles.Count > 0)
            {
                foreach (var pair in state.Npcs)
                {
                    var npc = pair.Value;
                    if (npc.Status != NpcStatus.Joined || string.IsNullOrEmpty(npc.Role) || npc.InjuredWeeks > 0)
                    {
                        continue;
                    }

                    // 동료 탐험으로 자리를 비웠다 (역할 재기획 5.4절)
                    var away = OutingRules.AwayFactor(state, npc, kind);
                    if (away <= 0f)
                    {
                        continue;
                    }

                    var role = db.Extras.Roles.Find(r => r.Id == npc.Role);
                    var match = role == null ? 0f : Match(role.Modifiers, kind, key);
                    if (match != 0f)
                    {
                        // 기획 D-1: 스트레스가 높은 동료는 역할 효과가 준다. C.2 ①: 관계 단계에 따라 역할 효율이 달라진다.
                        // 역할 재기획: 정원·열린 방 수·적성 배율 (RoleRules.EffectScale)
                        var scale = (GiveRules.IsStressed(npc, db.Balance) ? db.Balance.CompanionStressedRoleMultiplier : 1f)
                                    * BondRules.RoleMultiplier(npc, db.Balance)
                                    * RoleRules.EffectScale(state, db, pair.Key, role)
                                    * away;
                        total += match * scale;
                    }
                }
            }

            return total;
        }

        private static float Match(List<TraitModifier> modifiers, TraitModifierKind kind, string key)
        {
            var total = 0f;
            foreach (var modifier in modifiers)
            {
                if (modifier.Kind == kind && (string.IsNullOrEmpty(modifier.Key) || modifier.Key == key))
                {
                    total += modifier.Value;
                }
            }

            return total;
        }

        /// <summary>탐험 모드로 넘길 배율 묶음 (키 = TraitModifierKind 이름).</summary>
        public static Dictionary<string, float> ExplorationModifiers(GameState state, IDefinitionDatabase db)
        {
            var result = new Dictionary<string, float>();
            foreach (var kind in new[] { TraitModifierKind.SearchSpeed, TraitModifierKind.NoiseMultiplier, TraitModifierKind.RunSpeed, TraitModifierKind.StealthBonus })
            {
                var value = Sum(state, db, kind);
                if (kind == TraitModifierKind.NoiseMultiplier && state.Base.Weather == WeatherType.Rain)
                {
                    // M6: 빗소리가 발소리를 덮는다 (문서 6.3.8)
                    value += db.Balance.RainNoiseBonus;
                }

                if (Math.Abs(value) > 0.0001f)
                {
                    result[kind.ToString()] = value;
                }
            }

            return result;
        }
    }

    public sealed class RaidResult
    {
        public int Strength;
        public int Power;
        public bool Repelled;

        /// <summary>이벤트로 강제된 습격 (막 마무리 등).</summary>
        public bool Scripted;

        /// <summary>거점을 비운 밤의 습격 (수학여행 중).</summary>
        public bool Away;
    }

    /// <summary>밤 습격 (문서 5.5): 방어도 vs 위협도 vs 전투력.</summary>
    public static class RaidRules
    {
        public const string FlagRepelled = "LAST_RAID_REPELLED";
        public const string FlagBreached = "LAST_RAID_BREACHED";

        /// <summary>거점을 비운 밤(수학여행)에 습격이 왔다 (돌아오는 장면이 갈린다).</summary>
        public const string FlagWhileAway = "RAID_WHILE_AWAY";

        public static float Chance(GameState state, BalanceConfig balance, IDefinitionDatabase db = null)
        {
            if (state.Calendar.Week < balance.RaidStartWeek)
            {
                return 0f;
            }

            var chance = balance.RaidBaseChance + (state.Base.Threat - state.Base.Defense) * balance.RaidChancePerPoint;
            chance = Math.Min(Math.Max(chance, 0f), balance.RaidMaxChance) * DifficultyRules.Get(state, balance).HumanThreat
                     * FactionRules.RaidChanceMultiplier(state, db);
            return state.Base.Weather == WeatherType.Rain ? chance * balance.RainRaidMultiplier : chance;
        }

        /// <summary>밤 슬롯마다: 확률로 습격이 오면 판정. 위협 후보가 0이면 난수를 쓰지 않는다.</summary>
        public static RaidResult RollNight(GameState state, ActionDef action, BalanceConfig balance, IRandomService random, StateDelta delta, int companionGuard = 0, IDefinitionDatabase db = null)
        {
            var chance = Chance(state, balance, db);
            if (chance <= 0f || random.NextDouble() >= chance)
            {
                return null;
            }

            // 세력 영향 (사거리 파가 적대면 더 세게 온다)
            var threat = state.Base.Threat + FactionRules.RaidStrengthBonus(state, db);
            return Resolve(state, threat, (action?.RaidGuard ?? 0) + companionGuard, false, balance, random, delta, action?.AwayFromBase ?? false);
        }

        public static RaidResult Resolve(GameState state, int threat, int guard, bool scripted, BalanceConfig balance, IRandomService random, StateDelta delta, bool away = false)
        {
            var strength = threat + random.Range(0, Math.Max(0, balance.RaidStrengthVariance));
            var result = new RaidResult
            {
                Scripted = scripted,
                Strength = strength,
                Power = DefenseRules.Power(state, balance, guard, random, true, away),
                Away = away,
            };
            result.Repelled = result.Power >= result.Strength;

            state.Exploration.Raids++;
            if (away)
            {
                state.Flags.Add(FlagWhileAway);
            }

            state.Flags.Remove(FlagRepelled);
            state.Flags.Remove(FlagBreached);
            if (result.Repelled)
            {
                state.Exploration.RaidsRepelled++;
                state.Flags.Add(FlagRepelled);
                EffectApplier.ChangeVital(state, VitalType.Stress, balance.RaidRepelStress, balance, delta);
                EffectApplier.ChangeDefense(state, -balance.RaidRepelDefenseLoss, balance, delta);
            }
            else
            {
                state.Flags.Add(FlagBreached);
                EffectApplier.ChangeVital(state, VitalType.Stress, balance.RaidBreachStress, balance, delta);
                if (!away)
                {
                    EffectApplier.ChangeVital(state, VitalType.Hp, -balance.RaidBreachHp, balance, delta);
                }

                EffectApplier.ChangeResource(state, ResourceType.Food, -balance.RaidBreachFood, delta);
                EffectApplier.ChangeDefense(state, -balance.RaidBreachDefenseLoss, balance, delta);
            }

            EffectApplier.ChangeThreat(state, -balance.RaidThreatRelief, balance, delta);
            return result;
        }
    }

    /// <summary>장비 (문서 5.6, M4 결정: 쓰러지면 배낭과 착용 장비 전부 분실).</summary>
    public static class LoadoutRules
    {
        public static IEnumerable<string> EquippedIds(LoadoutState loadout)
        {
            if (!string.IsNullOrEmpty(loadout.Backpack)) yield return loadout.Backpack;
            if (!string.IsNullOrEmpty(loadout.MainWeapon)) yield return loadout.MainWeapon;
            if (!string.IsNullOrEmpty(loadout.Tool)) yield return loadout.Tool;
            if (!string.IsNullOrEmpty(loadout.Armor)) yield return loadout.Armor;
        }

        public static int CountInStorage(GameState state, string itemId)
        {
            var count = 0;
            foreach (var stack in state.Storage)
            {
                if (stack.ItemId == itemId) count += stack.Count;
            }

            return count;
        }

        /// <summary>창고에 없는 장비·소모품은 로드아웃에서 뺀다 (출정 직전 정리).</summary>
        public static void Sanitize(GameState state)
        {
            var loadout = state.Loadout;
            if (CountInStorage(state, loadout.Backpack) <= 0) loadout.Backpack = "";
            if (CountInStorage(state, loadout.MainWeapon) <= 0) loadout.MainWeapon = "";
            if (CountInStorage(state, loadout.Tool) <= 0) loadout.Tool = "";
            if (CountInStorage(state, loadout.Armor) <= 0) loadout.Armor = "";

            var used = new Dictionary<string, int>();
            for (var i = loadout.QuickSlots.Count - 1; i >= 0; i--)
            {
                var id = loadout.QuickSlots[i];
                used.TryGetValue(id, out var n);
                if (string.IsNullOrEmpty(id) || CountInStorage(state, id) <= n)
                {
                    loadout.QuickSlots.RemoveAt(i);
                    continue;
                }

                used[id] = n + 1;
            }
        }

        /// <summary>창고에서 아이템 n개를 뺀다. 실제로 뺀 수를 돌려준다.</summary>
        public static int RemoveFromStorage(GameState state, string itemId, int count, StateDelta delta)
        {
            var removed = 0;
            for (var i = state.Storage.Count - 1; i >= 0 && removed < count; i--)
            {
                var stack = state.Storage[i];
                if (stack.ItemId != itemId)
                {
                    continue;
                }

                var take = Math.Min(stack.Count, count - removed);
                stack.Count -= take;
                removed += take;
                if (stack.Count <= 0)
                {
                    state.Storage.RemoveAt(i);
                }
            }

            if (removed > 0)
            {
                delta.ItemsLost.Add(new ItemStack { ItemId = itemId, Count = removed });
            }

            return removed;
        }

        /// <summary>무기 내구도 소모. 0이 되면 부서져 사라진다.</summary>
        public static void WearItem(GameState state, IDefinitionDatabase db, string itemId, int wear, StateDelta delta)
        {
            if (string.IsNullOrEmpty(itemId) || wear <= 0 || !db.TryGetItem(itemId, out var def) || def.MaxDurability <= 0)
            {
                return;
            }

            var stack = state.Storage.Find(s => s.ItemId == itemId);
            if (stack == null)
            {
                return;
            }

            if (stack.Durability <= 0)
            {
                stack.Durability = def.MaxDurability;
            }

            stack.Durability -= wear;
            if (stack.Durability <= 0)
            {
                RemoveFromStorage(state, itemId, 1, delta);
                if (state.Loadout.MainWeapon == itemId && CountInStorage(state, itemId) <= 0)
                {
                    state.Loadout.MainWeapon = "";
                }
            }
        }
    }

    /// <summary>조건으로 고르는 한 줄 (일기·선생님 코멘트).</summary>
    public static class LineRules
    {
        public static LineDef Pick(GameState state, IDefinitionDatabase db, LineKind kind, EventContext context)
        {
            LineDef best = null;
            foreach (var line in db.Extras.Lines)
            {
                if (line.Kind != kind || (best != null && line.Priority <= best.Priority))
                {
                    continue;
                }

                try
                {
                    if (ConditionEvaluator.IsMet(state, line.Condition, context, db.Balance))
                    {
                        best = line;
                    }
                }
                catch (ArgumentException)
                {
                    // 잘못된 키는 검증 도구가 잡는다
                }
            }

            return best;
        }
    }
}
