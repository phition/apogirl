using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>주인공이 거점에 없던 밤의 습격 (수학여행·밤 탐험 중). 주인공은 다치지 않는다.</summary>
        public bool Away;

        /// <summary>주인공이 밤 탐험에 나간 사이의 습격 (돌아와서 안다, 2026-11-26).</summary>
        public bool WhileExpedition;

        /// <summary>뚫렸을 때 주인공 앞을 막아서다 다친 동료 (기획 C.2 ③, 없으면 빈 문자열).</summary>
        public string CoveredNpcId = "";

        /// <summary>뚫린 밤에 고양이가 먼저 털을 세워 경고했다 (확장 2, 피해가 조금 줄었다).</summary>
        public bool CatWarned;

        /// <summary>교문 허수아비가 줄인 습격 세기 (제작 P6, 0이면 없었다).</summary>
        public int ScarecrowRelief;

        /// <summary>이번 습격으로 교문 허수아비가 쓰러졌다 (다시 만들 수 있다).</summary>
        public bool ScarecrowFell;
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

            var chance = balance.RaidBaseChance + (state.Base.Threat - state.Base.Defense - SpecialRules.DefenseBonus(state, balance)) * balance.RaidChancePerPoint;
            chance = Math.Min(Math.Max(chance, 0f), balance.RaidMaxChance) * DifficultyRules.Get(state, balance).HumanThreat
                     * FactionRules.RaidChanceMultiplier(state, db) * BroadcastRules.RaidMultiplier(state, db, balance);
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
            var absence = action == null ? RaidAbsence.None
                : action.AwayFromBase ? RaidAbsence.Trip
                : action.IsExpedition ? RaidAbsence.Expedition
                : RaidAbsence.None;
            return Resolve(state, threat, (action?.RaidGuard ?? 0) + companionGuard, false, balance, random, delta, absence);
        }

        public static RaidResult Resolve(GameState state, int threat, int guard, bool scripted, BalanceConfig balance, IRandomService random, StateDelta delta, bool away = false) =>
            Resolve(state, threat, guard, scripted, balance, random, delta, away ? RaidAbsence.Trip : RaidAbsence.None);

        public static RaidResult Resolve(GameState state, int threat, int guard, bool scripted, BalanceConfig balance, IRandomService random, StateDelta delta, RaidAbsence absence)
        {
            var away = absence != RaidAbsence.None;   // 주인공이 없다: 전투력이 빠지고, 주인공은 다치지 않는다
            var trip = absence == RaidAbsence.Trip;   // 다 같이 비웠다: 고양이도 없고, 돌아오는 장면이 갈린다
            var strength = threat + random.Range(0, Math.Max(0, balance.RaidStrengthVariance));
            // 제작 P6: 몰려든 무리 일부가 교문 앞 허수아비에 붙는다 (세기만 줄인다)
            var relief = Math.Min(ScarecrowRules.Relief(state, balance), Math.Max(0, strength));
            strength -= relief;
            var result = new RaidResult
            {
                Scripted = scripted,
                Strength = strength,
                ScarecrowRelief = relief,
                Power = DefenseRules.Power(state, balance, guard, random, true, absence),
                Away = away,
                WhileExpedition = absence == RaidAbsence.Expedition,
            };
            result.Repelled = result.Power >= result.Strength;

            state.Exploration.Raids++;
            if (trip)
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
                var scale = SpecialRules.RaidDamageScale(state, balance); // 도현 특기: 피해 −20%
                scale *= CatRules.RaidDamageScale(state, balance, trip);   // 확장 2: 고양이가 먼저 털을 세운다 (밤 탐험 중이면 동료들에게)
                result.CatWarned = !trip && CatRules.IsHome(state);
                if (!away)
                {
                    // 관계가 높은 동료가 대신 다치기도 한다 (기획 C.2 ③)
                    result.CoveredNpcId = CrisisRules.RollCover(state, balance, random);
                    var hp = SpecialRules.ScaleDamage(balance.RaidBreachHp, scale);
                    if (!string.IsNullOrEmpty(result.CoveredNpcId))
                    {
                        hp = (int)System.Math.Round(hp * balance.RaidCoverHpScale);
                    }

                    if (hp > 0)
                    {
                        EffectApplier.ChangeVital(state, VitalType.Hp, -hp, balance, delta);
                    }
                }

                EffectApplier.ChangeResource(state, ResourceType.Food, -SpecialRules.ScaleDamage(balance.RaidBreachFood, scale * ScarecrowRules.BreachFoodScale(state, balance)), delta);
                EffectApplier.ChangeDefense(state, -SpecialRules.ScaleDamage(balance.RaidBreachDefenseLoss, scale), balance, delta);
            }

            ScarecrowRules.AfterRaid(state, result, balance, delta);
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
            if (!string.IsNullOrEmpty(loadout.Tool2)) yield return loadout.Tool2;
            if (!string.IsNullOrEmpty(loadout.Armor)) yield return loadout.Armor;
        }

        /// <summary>
        /// 같은 장비가 내구도별로 여럿일 때 찰 것을 고른다: 창고에서 그 내구(남은 값 left)의 묶음을 그 ID의 맨 앞으로 옮긴다
        /// (찬 장비 = 그 ID의 맨 앞 묶음). 그런 묶음이 없으면 false.
        /// </summary>
        public static bool BringToFront(GameState state, ItemDef item, int left)
        {
            var first = state.Storage.FindIndex(s => s.ItemId == item.Id && s.Count > 0);
            var chosen = state.Storage.FindIndex(s => s.ItemId == item.Id && s.Count > 0 && item.LeftOf(s.Durability) == left);
            if (chosen < 0)
            {
                return false;
            }

            if (chosen > first)
            {
                var stack = state.Storage[chosen];
                state.Storage.RemoveAt(chosen);
                state.Storage.Insert(first, stack);
            }

            return true;
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

        /// <summary>창고에 없는 장비는 로드아웃에서 뺀다 (출정 직전 정리).</summary>
        public static void Sanitize(GameState state)
        {
            var loadout = state.Loadout;
            if (CountInStorage(state, loadout.Backpack) <= 0) loadout.Backpack = "";
            if (CountInStorage(state, loadout.MainWeapon) <= 0) loadout.MainWeapon = "";
            if (CountInStorage(state, loadout.Tool) <= 0) loadout.Tool = "";
            if (CountInStorage(state, loadout.Tool2) <= 0 || loadout.Tool2 == loadout.Tool) loadout.Tool2 = "";   // 같은 도구 두 칸은 뜻이 없다
            if (CountInStorage(state, loadout.Armor) <= 0) loadout.Armor = "";

            // 퀵슬롯은 없앴다 (소모품은 탐험 중 배낭에서 쓴다). 옛 세이브에 남은 값은 비운다.
            loadout.QuickSlots.Clear();

            // 챙길 것: 창고에 있는 만큼만
            loadout.Carry ??= new List<string>();
            var counted = new Dictionary<string, int>();
            loadout.Carry.RemoveAll(id =>
            {
                if (string.IsNullOrEmpty(id))
                {
                    return true;
                }

                counted.TryGetValue(id, out var taken);
                if (taken >= CountInStorage(state, id))
                {
                    return true;
                }

                counted[id] = taken + 1;
                return false;
            });
        }

        /// <summary>"챙길 것"에 담을 수 있는 창고 물건: 장비가 아닌 소모품 (회복·스트레스 완화·허수아비).</summary>
        public static bool IsCarryable(ItemDef item) =>
            item != null && item.Slot == EquipSlot.None && (item.UseHp > 0 || item.UseStress > 0 || item.IsDecoy);

        /// <summary>챙겨 갈 배낭의 칸 수 (가방이 없으면 주머니).</summary>
        public static int BagCells(LoadoutState loadout, IDefinitionDatabase db)
        {
            ItemDef bag = null;
            var hasBag = !string.IsNullOrEmpty(loadout.Backpack) && db.TryGetItem(loadout.Backpack, out bag);
            return hasBag && bag.GridWidth > 0 ? bag.GridWidth * bag.GridHeight : db.Balance.PocketWidth * db.Balance.PocketHeight;
        }

        /// <summary>챙긴 물건이 차지하는 칸 수.</summary>
        public static int CarryCells(LoadoutState loadout, IDefinitionDatabase db)
        {
            var cells = 0;
            foreach (var id in loadout.Carry)
            {
                cells += !db.TryGetItem(id, out var item) ? 0 : Math.Max(1, item.Width) * Math.Max(1, item.Height);
            }

            return cells;
        }

        /// <summary>창고에서 아이템 n개를 뺀다. 실제로 뺀 수를 돌려준다.
        /// fromFront면 앞 묶음부터 (찬 무기는 그 ID의 맨 앞 묶음이다 — 내구도가 그 묶음에 있다).</summary>
        public static int RemoveFromStorage(GameState state, string itemId, int count, StateDelta delta, bool fromFront = false)
        {
            var removed = 0;
            var order = new List<int>();
            for (var i = 0; i < state.Storage.Count; i++)
            {
                if (state.Storage[i].ItemId == itemId)
                {
                    order.Add(i);
                }
            }

            if (!fromFront)
            {
                order.Reverse();
            }

            var emptied = new List<int>();
            foreach (var i in order)
            {
                if (removed >= count)
                {
                    break;
                }

                var stack = state.Storage[i];
                var take = Math.Min(stack.Count, count - removed);
                stack.Count -= take;
                removed += take;
                if (stack.Count <= 0)
                {
                    emptied.Add(i);
                }
            }

            foreach (var i in emptied.OrderByDescending(i => i))
            {
                state.Storage.RemoveAt(i);
            }

            if (removed > 0)
            {
                delta.ItemsLost.Add(new ItemStack { ItemId = itemId, Count = removed });
            }

            return removed;
        }

        /// <summary>장비 내구도 소모 (그 ID의 맨 앞 묶음). 닳는 장비는 0이 되면 부서져 사라지고, 빛 도구는 전력만 준다 (음수면 충전).</summary>
        public static void WearItem(GameState state, IDefinitionDatabase db, string itemId, int wear, StateDelta delta)
        {
            if (string.IsNullOrEmpty(itemId) || wear == 0 || !db.TryGetItem(itemId, out var def) || def.MaxDurability <= 0)
            {
                return;
            }

            var stack = state.Storage.Find(s => s.ItemId == itemId);
            if (stack == null || (wear < 0 && !def.UsesPower))
            {
                return;
            }

            SetDurability(state, db, itemId, def.LeftOf(stack.Durability) - wear, delta);
        }

        /// <summary>
        /// 장비(그 ID의 맨 앞 묶음)의 남은 내구·전력을 적는다. 닳는 장비(무기·방어구·쇠지렛대)가 0이 되면 부서져 사라지고,
        /// 빛 도구는 0~최대치로 남는다 (바닥 = −1). 여러 개 묶음이면 하나를 떼어 앞에 둔다.
        /// </summary>
        public static void SetDurability(GameState state, IDefinitionDatabase db, string itemId, int left, StateDelta delta)
        {
            if (string.IsNullOrEmpty(itemId) || !db.TryGetItem(itemId, out var def) || def.MaxDurability <= 0)
            {
                return;
            }

            var stack = state.Storage.Find(s => s.ItemId == itemId);
            if (stack == null)
            {
                return;
            }

            if (def.UsesPower)
            {
                Single(state, stack).Durability = def.StoredOf(Math.Min(Math.Max(left, 0), def.MaxDurability));
                return;
            }

            if (left > 0)
            {
                Single(state, stack).Durability = def.StoredOf(left);
                return;
            }

            // 부서짐: 맨 앞 하나를 빼고, 남은 것이 없으면 찬 칸도 비운다
            RemoveFromStorage(state, itemId, 1, delta, fromFront: true);
            if (CountInStorage(state, itemId) > 0)
            {
                return;
            }

            var loadout = state.Loadout;
            if (loadout.MainWeapon == itemId) loadout.MainWeapon = "";
            if (loadout.Armor == itemId) loadout.Armor = "";
            if (loadout.Tool == itemId) loadout.Tool = "";
            if (loadout.Tool2 == itemId) loadout.Tool2 = "";
        }

        /// <summary>건전지 하나로 채우는 전력 (BalanceConfig.FlashlightBatteryRestore × 최대치).</summary>
        public static int BatteryCharge(ItemDef item, BalanceConfig balance) =>
            Math.Max(1, (int)Math.Round(item.MaxDurability * balance.FlashlightBatteryRestore));

        /// <summary>여러 개가 한 묶음이면 하나를 떼어 그 앞에 두고 돌려준다 (내구는 한 개에만 적는다).</summary>
        private static ItemStack Single(GameState state, ItemStack stack)
        {
            if (stack.Count <= 1)
            {
                return stack;
            }

            stack.Count--;
            var one = new ItemStack { ItemId = stack.ItemId, Count = 1, Durability = stack.Durability };
            state.Storage.Insert(state.Storage.IndexOf(stack), one);
            return one;
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
