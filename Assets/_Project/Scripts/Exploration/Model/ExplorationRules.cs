using System;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Random;
using ApoGirl.Core.State;

namespace ApoGirl.Exploration.Model
{
    /// <summary>스탯과 탐험의 연결 (문서 6.3.7). 수치는 BalanceConfig.</summary>
    public static class ExplorationRules
    {
        public static float CarryCapacity(IReadOnlyDictionary<StatType, int> stats, BalanceConfig balance)
        {
            stats.TryGetValue(StatType.Strength, out var strength);
            return balance.BaseCarryWeight + strength * balance.CarryWeightPerStrength;
        }

        /// <summary>손재주가 높을수록 수색이 빠르다 (상한 있음).</summary>
        public static float SearchSeconds(float baseSeconds, IReadOnlyDictionary<StatType, int> stats, BalanceConfig balance)
        {
            stats.TryGetValue(StatType.Craft, out var craft);
            var bonus = Math.Min(balance.MaxSearchSpeedBonus, craft * balance.SearchSpeedPerCraft);
            return baseSeconds * (1f - bonus) * balance.SearchTimeMultiplier;
        }

        /// <summary>스트레스가 높으면 수색이 더 걸린다: 1(기준 이하) ~ 1 + StressSearchMaxExtra(스트레스 100).</summary>
        public static float StressSearchFactor(int stress, BalanceConfig balance)
        {
            var over = (stress - balance.StressSearchThreshold) / Math.Max(1f, 100f - balance.StressSearchThreshold);
            return 1f + Math.Max(0f, Math.Min(1f, over)) * balance.StressSearchMaxExtra;
        }

        /// <summary>숨는 데 걸리는 시간: 민첩이 오를수록 계속 짧아진다 (최소값 있음).</summary>
        public static float HideSeconds(IReadOnlyDictionary<StatType, int> stats, BalanceConfig balance)
        {
            stats.TryGetValue(StatType.Agility, out var agility);
            return Math.Max(balance.HideMinSeconds, balance.HideSeconds / (1f + Math.Max(0, agility) * balance.HideAgilityFactor));
        }

        /// <summary>스태미너 최대치: 체력 능력치가 높을수록 크다.</summary>
        public static float StaminaMax(IReadOnlyDictionary<StatType, int> stats, BalanceConfig balance)
        {
            stats.TryGetValue(StatType.Stamina, out var stamina);
            return balance.StaminaBase + Math.Max(0, stamina) * balance.StaminaPerStat;
        }

        /// <summary>이번 탐험에서 쓸 수 있는 최대치: 피로·부상만큼 일시적으로 깎인다.</summary>
        public static float StaminaCap(float max, int fatigue, bool injured, BalanceConfig balance)
        {
            var ratio = 1f - Math.Max(0, fatigue) * balance.StaminaFatiguePenalty - (injured ? balance.StaminaInjuryPenalty : 0f);
            return max * Math.Max(balance.StaminaMinCapRatio, Math.Min(1f, ratio));
        }

        /// <summary>숨어 있는 1초 동안 잠들 확률 (수면 부채가 기준 미만이면 0).</summary>
        public static float HideSleepChancePerSecond(int sleepDebt, BalanceConfig balance)
        {
            return sleepDebt < balance.HideSleepDebtThreshold ? 0f : (sleepDebt - balance.HideSleepDebtThreshold + 1) * balance.HideSleepChancePerDebt;
        }

        public static float MoveSpeed(bool running, bool crouching, bool overweight, BalanceConfig balance)
        {
            var speed = crouching ? balance.CrouchSpeed : running ? balance.RunSpeed : balance.WalkSpeed;
            return overweight ? speed * balance.OverweightSpeedMultiplier : speed;
        }

        /// <summary>컨테이너별 결정적 시드 (같은 출정 시드면 같은 내용물).</summary>
        public static int ContainerSeed(int runSeed, int containerIndex)
        {
            unchecked
            {
                return (runSeed * 397) ^ ((containerIndex + 1) * 7919);
            }
        }
    }

    /// <summary>전리품 굴림 (시드 기반, 문서 11.2 난수 원칙).</summary>
    public static class LootRoller
    {
        public static List<ItemStack> Roll(LootTableDef table, IRandomService random)
        {
            var result = new List<ItemStack>();
            var totalWeight = table.Entries.Sum(e => Math.Max(0, e.Weight));
            if (totalWeight <= 0 || random.NextDouble() < table.EmptyChance)
            {
                return result;
            }

            var rolls = random.Range(table.MinRolls, Math.Max(table.MinRolls, table.MaxRolls));
            for (var i = 0; i < rolls; i++)
            {
                var pick = random.Range(1, totalWeight);
                foreach (var entry in table.Entries)
                {
                    pick -= Math.Max(0, entry.Weight);
                    if (pick > 0)
                    {
                        continue;
                    }

                    var count = random.Range(entry.MinCount, Math.Max(entry.MinCount, entry.MaxCount));
                    for (var c = 0; c < count; c++)
                    {
                        result.Add(new ItemStack { ItemId = entry.ItemId, Count = 1 });
                    }

                    break;
                }
            }

            return result;
        }
    }

    /// <summary>탐험 종료 시 허브로 돌려줄 결과를 만든다 (문서 6.3.4-6 정산, 6.3.6 쓰러짐).</summary>
    public static class RunResultBuilder
    {
        /// <param name="baseFatigue">시간대 기본 피로 (헤드리스 모델과 같은 값).</param>
        /// <param name="runStress">런 중 누적된 스트레스 (기본 + 사건).</param>
        public static ExpeditionResult Build(
            ExtractStatus status,
            IEnumerable<PlacedItem> backpack,
            IDefinitionDatabase db,
            int baseFatigue,
            int runStress,
            int hpDelta,
            float timeSpent)
        {
            var balance = db.Balance;
            var result = new ExpeditionResult
            {
                Status = status,
                FatigueDelta = baseFatigue,
                StressDelta = runStress,
                HpDelta = hpDelta,
                TimeSpent = timeSpent,
            };

            if (status == ExtractStatus.Stranded)
            {
                result.FatigueDelta += balance.StrandedFatigue;
                result.StressDelta += balance.StrandedStress;
            }

            if (status == ExtractStatus.Down)
            {
                // 쓰러짐: 배낭 전부 분실
                result.Injuries = InjuryFlags.Fracture;
                return result;
            }

            var resources = new Dictionary<ResourceType, int>();
            var items = new Dictionary<string, int>();
            foreach (var placed in backpack)
            {
                // 가지고 나간 장비를 벗어 배낭에 넣은 것은 이미 창고에 있다
                if (placed.Owned)
                {
                    continue;
                }

                if (db.TryGetItem(placed.ItemId, out var item) && item.ResourceYield.Count > 0)
                {
                    foreach (var amount in item.ResourceYield)
                    {
                        resources.TryGetValue(amount.Resource, out var current);
                        resources[amount.Resource] = current + amount.Amount;
                    }
                }
                else if (item != null && item.MaxDurability > 0 && placed.Wear > 0)
                {
                    // 주워 쓰다 배낭에 넣은 무기: 남은 내구도를 그대로 창고에 (한 자루씩 따로)
                    result.Loot.Add(new ItemStack { ItemId = placed.ItemId, Count = 1, Durability = Math.Max(1, item.MaxDurability - placed.Wear) });
                }
                else
                {
                    items.TryGetValue(placed.ItemId, out var count);
                    items[placed.ItemId] = count + 1;
                }
            }

            foreach (var pair in resources)
            {
                result.ResourceLoot.Add(new ResourceAmount(pair.Key, pair.Value));
            }

            foreach (var pair in items)
            {
                result.Loot.Add(new ItemStack { ItemId = pair.Key, Count = pair.Value });
            }

            return result;
        }
    }
}
