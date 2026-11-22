using System;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Random;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Expedition
{
    /// <summary>
    /// 확률 기반 탐험 모델 (문서 12장 M7: 실시간 탐험을 기대 수익/손실/쓰러짐 확률로 추상화).
    /// 요청의 Seed만으로 결과가 결정되므로 같은 요청은 항상 같은 결과를 낸다.
    /// </summary>
    public sealed class HeadlessExpeditionRunner : IExpeditionRunner
    {
        private readonly IDefinitionDatabase _db;
        private readonly IEncounterChooser _encounterChooser;

        public HeadlessExpeditionRunner(IDefinitionDatabase db, IEncounterChooser encounterChooser = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _encounterChooser = encounterChooser ?? new RandomEncounterChooser();
        }

        public ExpeditionResult Run(ExpeditionRequest request)
        {
            var model = _db.GetExpeditionModel(request.LocationId);
            var timeYield = request.TimeOfDay == ExpeditionTime.Day ? model.Day : model.Night;
            var random = new SeededRandom(SeededRandom.CreateState(request.Seed));

            request.Stats.TryGetValue(StatType.Agility, out var agility);
            var downChance = timeYield.BaseDownChance
                             - agility * model.AgilityDownReduction
                             + request.SleepDebt * model.SleepDebtDownIncrease
                             + (request.ConsecutiveRun ? model.ConsecutiveDownIncrease : 0f);
            downChance = Math.Min(Math.Max(downChance * request.HarmMultiplier, 0f), 0.95f);

            var result = new ExpeditionResult
            {
                FatigueDelta = timeYield.Fatigue,
                StressDelta = random.Range(timeYield.StressMin, Math.Max(timeYield.StressMin, timeYield.StressMax)),
            };

            if (random.NextDouble() < downChance)
            {
                // 쓰러짐: 배낭 전부 분실, 중상, 스트레스 대폭 상승 (문서 6.3.6)
                result.Status = ExtractStatus.Down;
                result.HpDelta = -timeYield.HpLossOnDown;
                result.StressDelta += timeYield.StressOnDown;
                result.Injuries = InjuryFlags.Fracture;
                return result;
            }

            result.Status = ExtractStatus.Extracted;
            foreach (var range in timeYield.Resources)
            {
                var amount = random.Range(range.Min, Math.Max(range.Min, range.Max));
                if (amount > 0)
                {
                    result.ResourceLoot.Add(new ResourceAmount(range.Resource, amount));
                }
            }

            if (request.TimeOfDay == ExpeditionTime.Day && timeYield.ConscienceMax > 0)
            {
                result.ConscienceDelta = random.Range(0, timeYield.ConscienceMax);
            }

            if (request.TimeOfDay == ExpeditionTime.Night && random.NextDouble() < timeYield.DawnReturnChance)
            {
                result.SleepDebtDelta = 1;
            }

            // M7: 조우도 확률로 (봇 판에서도 간호사·주동자·연구 자료 갈래가 열리게). 기존 굴림 뒤에 굴려 자원 결과는 그대로다.
            RollEncounters(request, timeYield, random, result);

            // 교실동 상자 고갈·적대적 생존자: 따로 뽑은 난수로 (기존 굴림 결과를 흔들지 않는다)
            ApplyDepletion(request, model, result);
            ApplyHostile(request, result);
            ApplyLikedItem(request, result);
            ApplyRooftop(request, result);
            var deep = ApplyDeep(request, result);
            ApplyCollectibles(request, result, deep);
            return result;
        }

        /// <summary>기호품(기획 D.4): 그 지역 전리품 표에 있는 기호품 하나를 확률로 줍는다. 따로 뽑은 난수로 (자원 결과를 흔들지 않는다).</summary>
        private void ApplyLikedItem(ExpeditionRequest request, ExpeditionResult result)
        {
            var balance = _db.Balance;
            if (result.Status != ExtractStatus.Extracted || balance.LikedItems == null || !_db.TryGetLocation(request.LocationId, out var location))
            {
                return;
            }

            var chance = request.TimeOfDay == ExpeditionTime.Day ? balance.HeadlessLikedItemDayChance : balance.HeadlessLikedItemNightChance;
            var random = new SeededRandom(SeededRandom.CreateState(request.Seed ^ 0x61F7ED));
            if (chance <= 0f || random.NextDouble() >= chance)
            {
                return;
            }

            var liked = new HashSet<string>(balance.LikedItems.SelectMany(l => l.Items ?? Array.Empty<string>()));
            var candidates = LootTableIds(location)
                .SelectMany(id => _db.TryGetLootTable(id, out var table) ? table.Entries.Select(e => e.ItemId) : Enumerable.Empty<string>())
                .Where(liked.Contains)
                .Distinct()
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToList();
            if (candidates.Count == 0)
            {
                return;
            }

            result.Loot.Add(new ItemStack { ItemId = candidates[random.Range(0, candidates.Count - 1)], Count = 1 });
        }

        /// <summary>옥상(4층)까지 올라가 본 판: 확률로 옥상 플래그를 남긴다. 따로 뽑은 난수로 (다른 결과를 흔들지 않는다).</summary>
        /// <summary>
        /// 깊은 곳 (기획 B): 깊은 곳이 있는 지역에서 무사히 돌아온 판은 확률로 깊이 들어가 봤다고 본다.
        /// 들어갔으면 깊은 곳 상자의 전리품 표에서 물건 하나(확률)와 스트레스. 따로 뽑은 난수로 (다른 결과를 흔들지 않는다).
        /// </summary>
        private bool ApplyDeep(ExpeditionRequest request, ExpeditionResult result)
        {
            if (result.Status != ExtractStatus.Extracted || !_db.TryGetLocation(request.LocationId, out var location)
                || location.Layout == null || location.Layout.DeepFloors <= 0)
            {
                return false;
            }

            var balance = _db.Balance;
            var random = new SeededRandom(SeededRandom.CreateState(request.Seed ^ 0xDEE9));
            var chance = balance.HeadlessDeepChance + (request.KnowsDeepExits ? balance.HeadlessDeepSpecialBonus : 0f);
            if (random.NextDouble() >= chance)
            {
                return false;
            }

            result.StressDelta += balance.HeadlessDeepStress;
            var layout = location.Layout;
            var tables = layout.Containers.Where(c => layout.IsDeep(c.Floor) && !string.IsNullOrEmpty(c.LootTableId))
                .Select(c => c.LootTableId).Distinct().OrderBy(id => id, StringComparer.Ordinal).ToList();
            if (tables.Count > 0 && random.NextDouble() < balance.HeadlessDeepItemChance
                && _db.TryGetLootTable(tables[random.Range(0, tables.Count - 1)], out var table) && table.Entries.Count > 0)
            {
                var total = table.Entries.Sum(e => Math.Max(0, e.Weight));
                var roll = random.Range(0, Math.Max(0, total - 1));
                foreach (var entry in table.Entries)
                {
                    roll -= Math.Max(0, entry.Weight);
                    if (roll < 0)
                    {
                        result.Loot.Add(new ItemStack { ItemId = entry.ItemId, Count = Math.Max(1, entry.MinCount) });
                        break;
                    }
                }
            }

            return true;
        }

        /// <summary>줍는 조우(CD, 기획 A.3): 무사히 돌아온 탐험에서 확률로 첫 선택지(챙긴다). 깊은 곳의 CD는 깊이 들어간 판만. 따로 뽑은 난수로.</summary>
        private void ApplyCollectibles(ExpeditionRequest request, ExpeditionResult result, bool deep)
        {
            if (result.Status != ExtractStatus.Extracted || !_db.TryGetLocation(request.LocationId, out var location))
            {
                return;
            }

            var random = new SeededRandom(SeededRandom.CreateState(request.Seed ^ 0x0C0D15));
            foreach (var encounter in EncounterRules.Active(location, request.TimeOfDay, request.Flags).Where(e => e.Collectible && e.Choices.Count > 0).ToList())
            {
                if (location.Layout != null && location.Layout.IsDeep(encounter.Floor) && !deep)
                {
                    continue;
                }

                if (random.NextDouble() < _db.Balance.HeadlessCdChance)
                {
                    EncounterRules.Apply(result, encounter, encounter.Choices[0]);
                }
            }
        }

        private void ApplyRooftop(ExpeditionRequest request, ExpeditionResult result)
        {
            if (result.Status != ExtractStatus.Extracted || !_db.TryGetLocation(request.LocationId, out var location)
                || location.Layout == null || string.IsNullOrEmpty(location.Layout.RooftopFlag) || request.Flags.Contains(location.Layout.RooftopFlag))
            {
                return;
            }

            var random = new SeededRandom(SeededRandom.CreateState(request.Seed ^ 0x2F00F7));
            if (random.NextDouble() < _db.Balance.HeadlessRooftopChance)
            {
                result.Flags.Add(location.Layout.RooftopFlag);
            }
        }

        private static IEnumerable<string> LootTableIds(LocationDef location)
        {
            var layout = location.Layout;
            var ids = new List<string> { location.DefaultLootTableId };
            if (layout != null)
            {
                ids.AddRange(layout.Containers.Select(c => c.LootTableId));
                ids.AddRange(layout.Doors.Select(d => d.LootTableId));
                foreach (var template in layout.RoomTemplates)
                {
                    ids.Add(template.DefaultLootTableId);
                    ids.AddRange(template.Containers.Select(c => c.LootTableId));
                }
            }

            return ids.Where(id => !string.IsNullOrEmpty(id)).Distinct();
        }

        /// <summary>상자가 고갈되는 지역: 이번에 뒤질 상자를 남은 것 중에서 고르고, 남은 상자가 모자라면 수확을 그만큼 줄인다.</summary>
        private void ApplyDepletion(ExpeditionRequest request, ExpeditionModelDef model, ExpeditionResult result)
        {
            if (model.ContainersPerRun <= 0 || !_db.TryGetLocation(request.LocationId, out var location) || !location.DepletingContainers)
            {
                return;
            }

            var available = HostileRules.AllContainerKeys(location).Where(k => !request.LootedContainers.Contains(k)).ToList();
            var random = new SeededRandom(SeededRandom.CreateState(request.Seed ^ 0xDE91E7));
            var picked = Math.Min(model.ContainersPerRun, available.Count);
            var rate = location.Tutorial ? 0f : _db.Balance.BaseEmptyContainerRate;
            var filled = 0;
            for (var i = 0; i < picked; i++)
            {
                var index = random.Range(i, available.Count - 1);
                (available[i], available[index]) = (available[index], available[i]);
                if (ContainerRules.BaselineEmpty(request.WorldSeed, available[i], rate))
                {
                    continue; // 처음부터 빈 상자: 기록하지 않는다 (복구 대상 아님)
                }

                filled++;
                result.SearchedContainers.Add(available[i]);
            }

            // 모델 수확은 '모든 상자가 차 있을 때' 기준: 실제로 찬 상자 비율만큼만 얻는다
            var fraction = (float)filled / model.ContainersPerRun;
            if (fraction >= 1f)
            {
                return;
            }

            foreach (var loot in result.ResourceLoot)
            {
                loot.Amount = (int)Math.Round(loot.Amount * fraction, MidpointRounding.AwayFromZero);
            }

            result.ResourceLoot.RemoveAll(r => r.Amount <= 0);
        }

        /// <summary>적대적 생존자가 있으면 확률로 붙잡힌다 (실시간 탐험의 도망·숨기를 확률로 대신).</summary>
        private void ApplyHostile(ExpeditionRequest request, ExpeditionResult result)
        {
            if (request.HostileTemperament <= 0)
            {
                return;
            }

            var balance = _db.Balance;
            var random = new SeededRandom(SeededRandom.CreateState(request.Seed ^ 0x40571F));
            var caught = Math.Min(balance.HeadlessHostileCaughtMax,
                balance.HeadlessHostileCaughtChance + Math.Max(0, request.HostileCount - 1) * balance.HeadlessHostileCaughtPerExtra);
            if (random.NextDouble() < caught)
            {
                HostileRules.Apply(result, HostileRules.OutcomeFor(request.HostileTemperament), request.Hp, balance);
            }
        }

        private void RollEncounters(ExpeditionRequest request, ExpeditionYield timeYield, IRandomService random, ExpeditionResult result)
        {
            if (timeYield.EncounterChance <= 0f || !_db.TryGetLocation(request.LocationId, out var location))
            {
                return;
            }

            foreach (var encounter in EncounterRules.Active(location, request.TimeOfDay, request.Flags).ToList())
            {
                if (encounter.Collectible || encounter.Choices.Count == 0 || random.NextDouble() >= timeYield.EncounterChance)
                {
                    continue;
                }

                var index = _encounterChooser.Choose(encounter, request, random);
                index = Math.Min(Math.Max(index, 0), encounter.Choices.Count - 1);
                EncounterRules.Apply(result, encounter, encounter.Choices[index]);
            }
        }
    }
}
