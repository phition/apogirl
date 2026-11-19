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
            return baseSeconds * (1f - bonus);
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
                if (db.TryGetItem(placed.ItemId, out var item) && item.ResourceYield.Count > 0)
                {
                    foreach (var amount in item.ResourceYield)
                    {
                        resources.TryGetValue(amount.Resource, out var current);
                        resources[amount.Resource] = current + amount.Amount;
                    }
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
