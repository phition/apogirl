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
            downChance = ApplyLoadoutDown(request, downChance);
            var decoy = request.TimeOfDay == ExpeditionTime.Night ? CarriedDecoy(request) : null;
            if (decoy != null)
            {
                // 제작 P6: 밤에 허수아비를 하나 세워 무리를 돌린다
                downChance -= _db.Balance.HeadlessDecoyNightDown;
            }

            downChance = Math.Min(Math.Max(downChance * request.HarmMultiplier, 0f), 0.95f);
            var bagYield = BagYieldMultiplier(request);
            var bagRandom = bagYield != 1f ? new SeededRandom(SeededRandom.CreateState(request.Seed ^ 0x3BA6C5)) : null;

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
            if (decoy != null)
            {
                result.UsedItems.Add(decoy);
            }

            foreach (var range in timeYield.Resources)
            {
                var amount = random.Range(range.Min, Math.Max(range.Min, range.Max));
                if (bagRandom != null && amount > 0)
                {
                    amount = ScaleAmount(amount, bagYield, bagRandom);
                }

                if (amount > 0)
                {
                    result.ResourceLoot.Add(new ResourceAmount(range.Resource, amount));
                }
            }

            RollConscience(request, timeYield, result);
            RollQuiet(request, result);

            if (request.TimeOfDay == ExpeditionTime.Night && random.NextDouble() < timeYield.DawnReturnChance)
            {
                result.SleepDebtDelta = 1;
                result.FatigueDelta += _db.Balance.DawnFatigue;
                result.StressDelta += _db.Balance.DawnStress;
            }

            // M7: 조우도 확률로 (봇 판에서도 간호사·주동자·연구 자료 갈래가 열리게). 기존 굴림 뒤에 굴려 자원 결과는 그대로다.
            RollEncounters(request, timeYield, random, result);

            // 교실동 상자 고갈·적대적 생존자: 따로 뽑은 난수로 (기존 굴림 결과를 흔들지 않는다)
            ApplyDepletion(request, model, result);
            ApplyHostile(request, result);
            ApplyLikedItem(request, result);
            ApplyPaper(request, result);
            ApplyLockedRooms(request, timeYield, result);
            ApplyRooftop(request, result);
            var deep = ApplyDeep(request, result, out var deepRoom);
            result.EnteredDeep = deep;
            ApplyCollectibles(request, result, deep, deepRoom);
            ApplyDeepRoomEncounters(request, result, deepRoom);
            ApplyEquipmentWear(request, result);
            return result;
        }

        /// <summary>
        /// 찬 방어구·도구의 내구·전력 (따로 뽑은 난수): 방어구는 한 번에 조금, 쇠지렛대는 확률로 닳고, 밤에는 빛 도구 하나가 전력을 쓴다.
        /// 닳는 장비가 0이 되면 부서져 잃는다.
        /// </summary>
        private void ApplyEquipmentWear(ExpeditionRequest request, ExpeditionResult result)
        {
            var loadout = request.Loadout;
            if (loadout == null)
            {
                return;
            }

            var balance = _db.Balance;
            var random = new SeededRandom(SeededRandom.CreateState(request.Seed ^ 0x7E4A11));
            var lightUsed = false;
            foreach (var id in new[] { loadout.Armor, loadout.Tool, loadout.Tool2 })
            {
                if (string.IsNullOrEmpty(id) || result.OwnedDurability.ContainsKey(id) || !_db.TryGetItem(id, out var item) || item.MaxDurability <= 0)
                {
                    continue;
                }

                var left = request.StartDurability(item);
                if (item.Slot == EquipSlot.Armor)
                {
                    left -= balance.HeadlessArmorWear;
                }
                else if (item.UsesPower)
                {
                    if (request.TimeOfDay == ExpeditionTime.Night && !lightUsed && left > 0)
                    {
                        left -= balance.HeadlessFlashlightNightDrain;
                        lightUsed = true;
                    }
                }
                else if (item.Does(ToolKind.Crowbar) && random.NextDouble() < balance.HeadlessCrowbarWearChance)
                {
                    left -= balance.CrowbarWearPerUse;
                }

                if (item.Wears && left <= 0)
                {
                    result.LostOwned.Add(id);
                    continue;
                }

                result.OwnedDurability[id] = Math.Max(0, left);
            }
        }

        /// <summary>
        /// 양심의 무게에 쓰는 기록 (사용자 결정 2026-11-27): 쓰러뜨린 좀비 수(무기가 없으면 적게)와 사람이 살던 방을 뒤졌는지.
        /// 판 시드에서 따로 뽑은 난수를 써서 기존 굴림 순서(수확·조우·새벽 귀환)는 흔들지 않는다.
        /// </summary>
        private void RollConscience(ExpeditionRequest request, ExpeditionYield timeYield, ExpeditionResult result)
        {
            var balance = _db.Balance;
            var random = new SeededRandom(SeededRandom.CreateState(request.Seed ^ 0x51A7E9));
            var max = request.TimeOfDay == ExpeditionTime.Night ? balance.HeadlessFelledNightMax : balance.HeadlessFelledDayMax;
            var felled = random.Range(0, Math.Max(0, max));
            if (!HasWeapon(request))
            {
                felled = (int)Math.Floor(felled * balance.HeadlessFelledUnarmedRatio);
            }

            result.ZombiesFelled = felled;
            result.SearchedOwnedRoom = timeYield.OwnedRoomChance > 0f && random.NextDouble() < timeYield.OwnedRoomChance;
        }

        /// <summary>
        /// 1년의 기록: 좀비에게 한 번도 쫓기지 않고 돌아온 탐험인가 (실시간에서는 추격 상태를 직접 본다).
        /// 무사히 돌아온 탐험만, 낮·밤 기본 확률에 민첩을 더해 굴린다. 따로 뽑은 난수라 기존 굴림은 흔들리지 않는다.
        /// </summary>
        private void RollQuiet(ExpeditionRequest request, ExpeditionResult result)
        {
            var balance = _db.Balance;
            if (result.Status != ExtractStatus.Extracted
                || (balance.ConscienceFelledThreshold > 0 && result.ZombiesFelled >= balance.ConscienceFelledThreshold))
            {
                result.Undetected = false;
                return;
            }

            request.Stats.TryGetValue(StatType.Agility, out var agility);
            var chance = (request.TimeOfDay == ExpeditionTime.Night ? balance.HeadlessQuietNightChance : balance.HeadlessQuietDayChance)
                         + agility * balance.HeadlessQuietPerAgility;
            var random = new SeededRandom(SeededRandom.CreateState(request.Seed ^ 0x9A1E7));
            result.Undetected = random.NextDouble() < Math.Min(balance.HeadlessQuietMax, chance);
        }

        private bool HasWeapon(ExpeditionRequest request)
        {
            var weaponId = request.Loadout?.MainWeapon;
            return !string.IsNullOrEmpty(weaponId) && _db.TryGetItem(weaponId, out var weapon) && weapon.WeaponPower > 0;
        }

        /// <summary>
        /// 찬 장비로 쓰러짐 확률을 고친다 (제작 C5): 무기 위력·밤 손전등만큼 빼고 방어구 감소율만큼 줄인다.
        /// 장비가 없으면 그대로다. 확률 문턱만 바꾸므로 기존 굴림 순서는 흔들지 않는다.
        /// </summary>
        private float ApplyLoadoutDown(ExpeditionRequest request, float downChance)
        {
            var loadout = request.Loadout;
            if (loadout == null)
            {
                return downChance;
            }

            var balance = _db.Balance;
            if (!string.IsNullOrEmpty(loadout.MainWeapon) && _db.TryGetItem(loadout.MainWeapon, out var weapon) && weapon.WeaponPower > 0)
            {
                downChance -= weapon.WeaponPower * balance.HeadlessWeaponDownPerPower;
            }

            if (request.TimeOfDay == ExpeditionTime.Night && (IsLit(request, loadout.Tool) || IsLit(request, loadout.Tool2)))
            {
                downChance -= balance.HeadlessFlashlightNightDown;
            }

            if (!string.IsNullOrEmpty(loadout.Armor) && _db.TryGetItem(loadout.Armor, out var armor) && armor.DamageReduction > 0f)
            {
                downChance *= 1f - Math.Min(1f, armor.DamageReduction) * balance.HeadlessArmorDownFactor;
            }

            return downChance;
        }

        /// <summary>챙겨 간 허수아비 하나 (울리는 것을 먼저, 없으면 null).</summary>
        private string CarriedDecoy(ExpeditionRequest request)
        {
            var carry = request.Loadout?.Carry;
            if (carry == null)
            {
                return null;
            }

            string best = null;
            var bestSeconds = 0f;
            foreach (var id in carry)
            {
                if (_db.TryGetItem(id, out var item) && item.IsDecoy && item.Decoy.Seconds > bestSeconds)
                {
                    best = id;
                    bestSeconds = item.Decoy.Seconds;
                }
            }

            return best;
        }

        /// <summary>가방 칸 수로 자원 수확 배율 (제작 C5). 가방이 없거나 기준 칸 수면 1.</summary>
        private float BagYieldMultiplier(ExpeditionRequest request)
        {
            var balance = _db.Balance;
            var bag = request.Loadout?.Backpack;
            if (string.IsNullOrEmpty(bag) || !_db.TryGetItem(bag, out var def) || def.GridWidth <= 0 || def.GridHeight <= 0)
            {
                return 1f;
            }

            var cells = def.GridWidth * def.GridHeight;
            return Math.Max(0f, 1f + (cells - balance.HeadlessBagBaseCells) * balance.HeadlessBagYieldPerCell);
        }

        /// <summary>배율을 곱한 양: 정수 부분 + 소수 부분은 따로 뽑은 난수로 올린다.</summary>
        private static int ScaleAmount(int amount, float multiplier, IRandomService random)
        {
            var scaled = amount * multiplier;
            var whole = (int)Math.Floor(scaled);
            return whole + (random.NextDouble() < scaled - whole ? 1 : 0);
        }

        /// <summary>기호품(기획 D.4):그 지역 전리품 표에 있는 기호품 하나를 확률로 줍는다. 따로 뽑은 난수로 (자원 결과를 흔들지 않는다).</summary>
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
        /// 들어간 판은 다시 확률로 특별한 방(큰 문·빗장) 하나를 열어 금고 표에서 물건 몇 개와 스트레스 (deepRoom).
        /// </summary>
        private bool ApplyDeep(ExpeditionRequest request, ExpeditionResult result, out bool deepRoom)
        {
            deepRoom = false;
            if (result.Status != ExtractStatus.Extracted || !_db.TryGetLocation(request.LocationId, out var location)
                || location.Layout == null || location.Layout.DeepFloors <= 0)
            {
                return false;
            }

            var balance = _db.Balance;
            var random = new SeededRandom(SeededRandom.CreateState(request.Seed ^ 0xDEE9));
            var chance = balance.HeadlessDeepChance + (request.KnowsDeepExits ? balance.HeadlessDeepSpecialBonus : 0f);
            var disaster = Rules.DisasterRules.Active(request.Week, request.WeatherId, balance);
            if (disaster != null && disaster.Id == Rules.DisasterRules.Monsoon)
            {
                chance *= balance.MonsoonHeadlessDeepMultiplier;   // 장마: 아래층이 물에 잠겨 덜 내려간다
            }
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
                AddWeightedItem(table, random, result);
            }

            // 특별한 방 (큰 문·빗장): 그 방들의 기본 전리품 표(금고 표)에서 몇 개
            var roomTables = DeepRoomTemplates(layout).Select(t => t.DefaultLootTableId).Where(id => !string.IsNullOrEmpty(id))
                .Distinct().OrderBy(id => id, StringComparer.Ordinal).ToList();
            if (roomTables.Count > 0 && random.NextDouble() < balance.HeadlessDeepRoomChance)
            {
                deepRoom = true;
                result.StressDelta += balance.HeadlessDeepRoomStress;
                if (_db.TryGetLootTable(roomTables[random.Range(0, roomTables.Count - 1)], out var roomTable) && roomTable.Entries.Count > 0)
                {
                    for (var i = 0; i < balance.HeadlessDeepRoomItems; i++)
                    {
                        AddWeightedItem(roomTable, random, result);
                    }
                }
            }

            return true;
        }

        /// <summary>전력이 남은 빛 도구 (전력이 없는 옛 정의면 그냥 빛 도구).</summary>
        private bool IsLit(ExpeditionRequest request, string itemId) =>
            !string.IsNullOrEmpty(itemId) && _db.TryGetItem(itemId, out var tool) && tool.Does(ToolKind.Flashlight)
            && (!tool.UsesPower || request.StartDurability(tool) > 0);

        /// <summary>깊은 곳 층에 있는 방의 템플릿 (특별한 방).</summary>
        private static IEnumerable<RoomTemplateDef> DeepRoomTemplates(BuildingLayoutDef layout) =>
            layout.Doors.Where(d => layout.IsDeep(d.Floor)).Select(d => layout.FindTemplate(d.TemplateId)).Where(t => t != null);

        /// <summary>깊은 곳 특별한 방 안의 조우인가 (헤드리스는 특별한 방을 연 판만).</summary>
        private static bool InDeepRoom(LocationDef location, EncounterDef encounter) =>
            location.Layout != null && location.Layout.IsDeep(encounter.Floor) && !string.IsNullOrEmpty(encounter.RoomId);

        /// <summary>전리품 표에서 가중치로 물건 하나 (최소 묶음 수).</summary>
        private static void AddWeightedItem(LootTableDef table, IRandomService random, ExpeditionResult result)
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

        /// <summary>
        /// 깊은 곳 특별한 방 안의 조우 (진실 단서 등, 줍는 물건 제외): 특별한 방을 연 판만 HeadlessDeepRoomEncounterChance로.
        /// 따로 뽑은 난수로.
        /// </summary>
        private void ApplyDeepRoomEncounters(ExpeditionRequest request, ExpeditionResult result, bool deepRoom)
        {
            if (!deepRoom || !_db.TryGetLocation(request.LocationId, out var location))
            {
                return;
            }

            var random = new SeededRandom(SeededRandom.CreateState(request.Seed ^ 0xDEE90E));
            foreach (var encounter in EncounterRules.Active(location, request.TimeOfDay, request.Flags)
                         .Where(e => !e.Collectible && e.Choices.Count > 0 && InDeepRoom(location, e)).ToList())
            {
                if (random.NextDouble() >= _db.Balance.HeadlessDeepRoomEncounterChance)
                {
                    continue;
                }

                var index = _encounterChooser.Choose(encounter, request, random);
                index = Math.Min(Math.Max(index, 0), encounter.Choices.Count - 1);
                EncounterRules.Apply(result, encounter, encounter.Choices[index]);
            }
        }

        /// <summary>
        /// 줍는 조우(CD, 기획 A.3): 무사히 돌아온 탐험에서 확률로 첫 선택지(챙긴다). 깊은 곳의 CD는 깊이 들어간 판만,
        /// 깊은 곳 특별한 방 안의 CD는 그 방을 연 판만. 따로 뽑은 난수로.
        /// </summary>
        private void ApplyCollectibles(ExpeditionRequest request, ExpeditionResult result, bool deep, bool deepRoom)
        {
            if (result.Status != ExtractStatus.Extracted || !_db.TryGetLocation(request.LocationId, out var location))
            {
                return;
            }

            var random = new SeededRandom(SeededRandom.CreateState(request.Seed ^ 0x0C0D15));
            foreach (var encounter in EncounterRules.Active(location, request.TimeOfDay, request.Flags).Where(e => e.Collectible && e.Choices.Count > 0).ToList())
            {
                if (location.Layout != null && location.Layout.IsDeep(encounter.Floor) && (!deep || (InDeepRoom(location, encounter) && !deepRoom)))
                {
                    continue;
                }

                if (random.NextDouble() < _db.Balance.HeadlessCdChance)
                {
                    EncounterRules.Apply(result, encounter, encounter.Choices[0]);
                }
            }
        }

        /// <summary>이 시간대 변형에서 잠긴 방 (그 안의 조우는 보통 굴림에서 뺀다).</summary>
        private static HashSet<string> LockedThisRun(LocationDef location, ExpeditionTime time)
        {
            var variant = time == ExpeditionTime.Day ? location.DayVariant : location.NightVariant;
            return new HashSet<string>(variant?.LockedRooms ?? new List<string>());
        }

        /// <summary>
        /// 낮에 잠겼다 밤에 열리는 방 (PaperRules.DuskRooms, 주택가 102호 등)과 한 번뿐인 상자 (ContainerDef.Once).
        /// 낮 탐험은 HeadlessDuskRoomChance로 시계를 넘겨 황혼까지 버틴 판만 그 방에 닿고 (시간 초과 피로·스트레스),
        /// 닿으면 방 안 조우를 치르고 한 번뿐인 상자를 뒤진다. 밤 탐험은 그 방이 열려 있어 조우는 보통 굴림으로,
        /// 한 번뿐인 상자는 조우 확률로 뒤진다. 따로 뽑은 난수로 (다른 결과를 흔들지 않는다).
        /// </summary>
        private void ApplyLockedRooms(ExpeditionRequest request, ExpeditionYield timeYield, ExpeditionResult result)
        {
            if (result.Status != ExtractStatus.Extracted || !_db.TryGetLocation(request.LocationId, out var location) || location.Layout == null)
            {
                return;
            }

            var balance = _db.Balance;
            var random = new SeededRandom(SeededRandom.CreateState(request.Seed ^ 0x0D05C7));
            var duskRooms = new HashSet<string>(Rules.PaperRules.DuskRooms(location));
            var day = request.TimeOfDay == ExpeditionTime.Day;
            var dusk = day && duskRooms.Count > 0 && random.NextDouble() < balance.HeadlessDuskRoomChance;
            if (dusk)
            {
                result.FatigueDelta += balance.StrandedFatigue;
                result.StressDelta += balance.StrandedStress;
                foreach (var encounter in EncounterRules.Active(location, request.TimeOfDay, request.Flags)
                             .Where(e => !e.Collectible && e.Choices.Count > 0 && duskRooms.Contains(e.RoomId)).ToList())
                {
                    var index = _encounterChooser.Choose(encounter, request, random);
                    index = Math.Min(Math.Max(index, 0), encounter.Choices.Count - 1);
                    EncounterRules.Apply(result, encounter, encounter.Choices[index]);
                }
            }

            var locked = LockedThisRun(location, request.TimeOfDay);
            var layout = location.Layout;
            for (var d = 0; d < layout.Doors.Count; d++)
            {
                var door = layout.Doors[d];
                var template = layout.FindTemplate(door.TemplateId);
                if (template == null || layout.IsDeep(door.Floor))
                {
                    continue;
                }

                // 잠긴 방이면 황혼에 열린 방만, 열린 방이면 조우 확률로
                var lockedRoom = locked.Contains(door.RoomId);
                for (var i = 0; i < template.Containers.Count; i++)
                {
                    var container = template.Containers[i];
                    var key = HostileRules.RoomKey(location.Id, d, i);
                    if (!container.Once || request.LootedContainers.Contains(key) || result.SearchedContainers.Contains(key))
                    {
                        continue;
                    }

                    var reached = lockedRoom ? dusk && duskRooms.Contains(door.RoomId) : random.NextDouble() < timeYield.EncounterChance;
                    if (!reached)
                    {
                        continue;
                    }

                    var tableId = string.IsNullOrEmpty(container.LootTableId) ? template.DefaultLootTableId : container.LootTableId;
                    if (_db.TryGetLootTable(tableId, out var table) && table.Entries.Count > 0)
                    {
                        AddWeightedItem(table, random, result);
                    }

                    result.SearchedContainers.Add(key);
                }
            }
        }

        /// <summary>
        /// 논문 (PaperRules): 그 지역 전리품 표에 있는 논문 하나를 HeadlessPaperChance로 줍는다.
        /// 한 번뿐인 상자만 쓰는 표는 빼고 (ApplyLockedRooms가 따로). 따로 뽑은 난수로.
        /// </summary>
        private void ApplyPaper(ExpeditionRequest request, ExpeditionResult result)
        {
            var balance = _db.Balance;
            if (result.Status != ExtractStatus.Extracted || balance.HeadlessPaperChance <= 0f || !_db.TryGetLocation(request.LocationId, out var location))
            {
                return;
            }

            var random = new SeededRandom(SeededRandom.CreateState(request.Seed ^ 0x9A9E25));
            if (random.NextDouble() >= balance.HeadlessPaperChance)
            {
                return;
            }

            var onceTables = OnceTableIds(location);
            var candidates = LootTableIds(location).Where(id => !onceTables.Contains(id))
                .SelectMany(id => _db.TryGetLootTable(id, out var table) ? table.Entries.Select(e => e.ItemId) : Enumerable.Empty<string>())
                .Where(id => _db.TryGetItem(id, out var item) && item.IsPaper && Rules.PaperRules.CanLoot(_db, id, request.Flags))
                .Distinct()
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToList();
            if (candidates.Count > 0)
            {
                result.Loot.Add(new ItemStack { ItemId = candidates[random.Range(0, candidates.Count - 1)], Count = 1 });
            }
        }

        /// <summary>한 번뿐인 상자만 쓰는 전리품 표 (다른 상자·방 기본 표로도 쓰이면 빼지 않는다).</summary>
        private static HashSet<string> OnceTableIds(LocationDef location)
        {
            var once = new HashSet<string>();
            var layout = location.Layout;
            if (layout == null)
            {
                return once;
            }

            var shared = new HashSet<string> { location.DefaultLootTableId ?? "" };
            foreach (var container in layout.Containers)
            {
                shared.Add(container.LootTableId ?? "");
            }

            foreach (var template in layout.RoomTemplates)
            {
                shared.Add(template.DefaultLootTableId ?? "");
                foreach (var container in template.Containers)
                {
                    (container.Once ? once : shared).Add(container.LootTableId ?? "");
                }
            }

            once.ExceptWith(shared);
            once.Remove("");
            return once;
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

            // 한 번뿐인 상자(102호 논문 등)는 ApplyLockedRooms에서 따로 (아무 상자 고르기에 섞이면 물건 없이 비어 버린다)
            var available = HostileRules.AllContainerKeys(location, includeDeepRooms: false)
                .Where(k => !request.LootedContainers.Contains(k) && !ContainerRules.IsOnce(location, k)).ToList();
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

            var locked = LockedThisRun(location, request.TimeOfDay);
            foreach (var encounter in EncounterRules.Active(location, request.TimeOfDay, request.Flags).ToList())
            {
                // 깊은 곳 특별한 방 안의 조우는 ApplyDeepRoomEncounters에서 (방을 연 판만),
                // 이 시간대에 잠긴 방 안의 조우는 ApplyLockedRooms에서 (낮이면 황혼까지 버틴 판만)
                if (encounter.Collectible || encounter.Choices.Count == 0 || InDeepRoom(location, encounter)
                    || (!string.IsNullOrEmpty(encounter.RoomId) && locked.Contains(encounter.RoomId))
                    || random.NextDouble() >= timeYield.EncounterChance)
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
