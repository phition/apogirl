using System;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Random;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>동료 탐험에서 다친 정도 (역할 재기획 5.3절). Minor = 가벼운 상처(1주, 탐험만 쉼), Severe = 부상(역할도 쉼).</summary>
    public enum OutingWound
    {
        None,
        Minor,
        Severe,
    }

    /// <summary>이번 주 한 번의 외출 (낮 슬롯 하루, 갈 곳).</summary>
    public sealed class OutingPlan
    {
        public int Day;
        public string LocationId = "";
    }

    /// <summary>외출에서 돌아온 결과 (그날 밤 슬롯 시작).</summary>
    public sealed class OutingReturn
    {
        public string NpcId = "";
        public string LocationId = "";
        public List<ResourceAmount> Resources = new List<ResourceAmount>();

        /// <summary>주워 온 기호품 (없으면 ""), 그 물건을 좋아하는 사람 (자기 자신일 수도).</summary>
        public string ItemId = "";
        public string ItemFor = "";
        public OutingWound Wound;
    }

    /// <summary>
    /// 동료 탐험 (역할 재기획 5절). 동료가 주에 1~2번 낮에 혼자 나가 물자를 구해 오고, 그동안 자리를 비운다.
    /// 일정은 (판 시드, 주차, 동료 ID)로만 정해져 계획 중 예고(Plan)와 실제(PrepareWeek → Schedule)가 같다.
    /// 난수는 따로 뽑아 기존 판의 난수 순서를 흔들지 않는다.
    /// </summary>
    public static class OutingRules
    {
        private const int SeedSalt = 0x0C7A11;

        /// <summary>이번 주 외출 일정 (붙잡은 동료·나갈 수 없는 동료는 빠진다). 상태를 바꾸지 않는다.</summary>
        public static Dictionary<string, List<OutingPlan>> Plan(GameState state, IDefinitionDatabase db)
        {
            var result = new Dictionary<string, List<OutingPlan>>();
            var balance = db.Balance;
            var locations = VisitedLocations(state, db);
            if (locations.Count == 0 || IsConfinedWeek(state, db))
            {
                return result;
            }

            var perDay = new int[CalendarState.DaysPerWeek + 1];
            foreach (var pair in state.Npcs.OrderBy(n => n.Key, StringComparer.Ordinal))
            {
                var id = pair.Key;
                var npc = pair.Value;
                if (!CanGoOut(npc, id, balance) || npc.StayHome)
                {
                    continue;
                }

                var random = Random(state, id, 1);
                var count = npc.Role == balance.ScoutRoleId ? balance.OutingScoutCount
                    : 1 + (random.NextDouble() < balance.OutingExtraChance ? 1 : 0);
                var days = Enumerable.Range(1, CalendarState.DaysPerWeek).OrderBy(_ => random.NextDouble()).ToList();
                var plans = new List<OutingPlan>();
                foreach (var day in days)
                {
                    if (plans.Count >= count)
                    {
                        break;
                    }

                    if (balance.OutingDailyCap > 0 && perDay[day] >= balance.OutingDailyCap)
                    {
                        continue;
                    }

                    perDay[day]++;
                    plans.Add(new OutingPlan { Day = day, LocationId = PickLocation(state, db, locations, random) });
                }

                if (plans.Count > 0)
                {
                    result[id] = plans.OrderBy(p => p.Day).ToList();
                }
            }

            return result;
        }

        /// <summary>
        /// 주 시작 (WeekRunner.PrepareWeek): 일정을 동료 상태에 적고, 붙잡은 주를 센다 (연속이면 답답해 스트레스).
        /// </summary>
        public static void Schedule(GameState state, IDefinitionDatabase db)
        {
            var balance = db.Balance;
            var plan = Plan(state, db);
            foreach (var pair in state.Npcs)
            {
                var npc = pair.Value;
                npc.OutingDays.Clear();
                npc.OutingLocations.Clear();
                if (plan.TryGetValue(pair.Key, out var plans))
                {
                    npc.OutingDays.AddRange(plans.Select(p => p.Day));
                    npc.OutingLocations.AddRange(plans.Select(p => p.LocationId));
                }

                if (npc.Status != NpcStatus.Joined)
                {
                    continue;
                }

                if (npc.StayHome)
                {
                    npc.StayStreak++;
                    if (npc.StayStreak >= 2)
                    {
                        AddStress(npc, balance.OutingHeldBackStress, balance);
                    }
                }
                else
                {
                    npc.StayStreak = 0;
                }
            }
        }

        /// <summary>나갈 수 있는 동료인가 (부상·가벼운 상처·스트레스·호감 음수·외출하지 않는 인물 제외).</summary>
        public static bool CanGoOut(NpcState npc, string id, BalanceConfig balance)
        {
            return npc.Status == NpcStatus.Joined && npc.InjuredWeeks <= 0 && npc.MinorWoundWeeks <= 0 && npc.Affinity >= 0
                   && (npc.Body?.Stress ?? 0) < balance.CompanionStressRoleThreshold
                   && (balance.OutingExcludedNpcs == null || Array.IndexOf(balance.OutingExcludedNpcs, id) < 0);
        }

        /// <summary>지금 외출해 자리에 없는가 (그날 낮 칸 시작부터 밤 칸 시작까지, 낮 칸 뒤 이벤트 포함).</summary>
        public static bool IsAwayNow(GameState state, NpcState npc) => npc.Away;

        /// <summary>
        /// 역할 효과에 곱할 자리 배율 (5.4절): 식량 절약은 나간 날 0, 감염·병세 감소(주 단위)는 나간 날만큼 줄고(6/7·5/7),
        /// 나머지(슬롯 단위·낮 침입 방어)는 나간 낮 슬롯에 0. 밤 습격 방어는 영향 없음.
        /// </summary>
        public static float AwayFactor(GameState state, NpcState npc, TraitModifierKind kind)
        {
            if (npc.OutingDays.Count == 0 && !npc.Away)
            {
                return 1f;
            }

            switch (kind)
            {
                case TraitModifierKind.FoodSaving:
                    return npc.OutingDays.Contains(state.Calendar.Day) ? 0f : 1f;
                case TraitModifierKind.InfectionResist:
                case TraitModifierKind.TeacherCare:
                    return Math.Max(0f, CalendarState.DaysPerWeek - npc.OutingDays.Distinct().Count()) / CalendarState.DaysPerWeek;
                default:
                    return IsAwayNow(state, npc) ? 0f : 1f;
            }
        }

        /// <summary>낮 슬롯 시작: 오늘 나가는 동료 (나가며 스트레스·배고픔·목마름).</summary>
        public static List<string> Depart(GameState state, IDefinitionDatabase db)
        {
            var balance = db.Balance;
            var result = new List<string>();
            if (state.Calendar.Phase != DayPhase.Day)
            {
                return result;
            }

            foreach (var pair in state.Npcs.Where(n => n.Value.Status == NpcStatus.Joined && n.Value.OutingDays.Contains(state.Calendar.Day))
                         .OrderBy(n => n.Key, StringComparer.Ordinal))
            {
                pair.Value.Away = true;
                var body = pair.Value.Body ?? (pair.Value.Body = new BodyState());
                body.Stress = Clamp(body.Stress + balance.OutingStress, 0, balance.VitalMax);
                body.Satiety = Clamp(body.Satiety - balance.OutingSatietyDrain, -balance.VitalMax, balance.VitalMax);
                body.Hydration = Clamp(body.Hydration - balance.OutingHydrationDrain, -balance.VitalMax, balance.VitalMax);
                result.Add(pair.Key);
            }

            return result;
        }

        /// <summary>밤 슬롯 시작: 오늘 나갔던 동료가 돌아온다 (물자·기호품·다침).</summary>
        public static List<OutingReturn> Return(GameState state, IDefinitionDatabase db, StateDelta delta)
        {
            var balance = db.Balance;
            var result = new List<OutingReturn>();
            if (state.Calendar.Phase != DayPhase.Night)
            {
                return result;
            }

            var difficulty = DifficultyRules.Get(state, balance);
            foreach (var pair in state.Npcs.Where(n => n.Value.Status == NpcStatus.Joined && n.Value.OutingDays.Contains(state.Calendar.Day))
                         .OrderBy(n => n.Key, StringComparer.Ordinal).ToList())
            {
                var id = pair.Key;
                var npc = pair.Value;
                var index = npc.OutingDays.IndexOf(state.Calendar.Day);
                var locationId = index < npc.OutingLocations.Count ? npc.OutingLocations[index] : "";
                var random = Random(state, id, 100 + state.Calendar.Day);
                var back = new OutingReturn { NpcId = id, LocationId = locationId };
                npc.Away = false;

                // 물자: 그 지역 헤드리스 산출의 일부 (정찰 적성이면 더), 난이도 배율, 소수 부분은 확률로
                if (db.TryGetExpeditionModel(locationId, out var model))
                {
                    var share = balance.OutingYieldShare * (RoleRules.HasAptitude(balance, id, balance.ScoutRoleId) ? 1f + balance.OutingScoutAptitudeBonus : 1f)
                                * difficulty.Yield;
                    foreach (var range in model.Day.Resources)
                    {
                        var perResource = range.Resource == ResourceType.Food ? difficulty.FoodYield
                            : range.Resource == ResourceType.Water ? difficulty.WaterYield
                            : range.Resource == ResourceType.Medicine ? difficulty.MedicineYield
                            : 1f;
                        var scaled = random.Range(range.Min, Math.Max(range.Min, range.Max)) * share * Math.Max(0f, perResource);
                        var amount = (int)Math.Floor(scaled) + (random.NextDouble() < scaled - Math.Floor(scaled) ? 1 : 0);
                        if (amount > 0)
                        {
                            EffectApplier.ChangeResource(state, range.Resource, amount, delta);
                            back.Resources.Add(new ResourceAmount(range.Resource, amount));
                        }
                    }
                }

                // 기호품: 자기 것이나 다른 사람 것을 하나
                if (random.NextDouble() < balance.OutingLikedChance)
                {
                    var people = GiveRules.Recipients(state, balance)
                        .Where(p => balance.LikedItems?.Any(l => l.NpcId == p && l.Items != null && l.Items.Length > 0) == true).ToList();
                    if (people.Count > 0)
                    {
                        var who = people[random.Range(0, people.Count - 1)];
                        var items = balance.LikedItems.First(l => l.NpcId == who).Items;
                        back.ItemId = items[random.Range(0, items.Length - 1)];
                        back.ItemFor = who;
                        EffectApplier.AddItem(state, back.ItemId, 1, delta);
                    }
                }

                // 다침: 정찰 역할은 덜 다친다. 그 가운데 일부만 "부상"
                var injuryChance = npc.Role == balance.ScoutRoleId ? balance.OutingScoutInjuryChance : balance.OutingInjuryChance;
                if (random.NextDouble() < injuryChance)
                {
                    if (random.NextDouble() < balance.OutingSevereShare)
                    {
                        back.Wound = OutingWound.Severe;
                        npc.InjuredWeeks = Math.Max(npc.InjuredWeeks, balance.OutingSevereWeeks);
                        AddStress(npc, balance.OutingSevereStress, balance);
                    }
                    else
                    {
                        back.Wound = OutingWound.Minor;
                        npc.MinorWoundWeeks = 1;
                        npc.MinorWoundWeek = state.Calendar.Week;
                        AddStress(npc, balance.OutingMinorStress, balance);
                    }
                }

                result.Add(back);
            }

            return result;
        }

        /// <summary>주간 결산 끝: 일정과 붙잡기를 지우고, 지난주 이전에 입은 가벼운 상처는 낫는다.</summary>
        public static void FinishWeek(GameState state)
        {
            foreach (var npc in state.Npcs.Values)
            {
                npc.OutingDays.Clear();
                npc.OutingLocations.Clear();
                npc.StayHome = false;
                npc.Away = false;
                if (npc.MinorWoundWeeks > 0 && state.Calendar.Week > npc.MinorWoundWeek)
                {
                    npc.MinorWoundWeeks = 0;
                }
            }
        }

        /// <summary>주인공이 한 번이라도 가 본 지역 (탐험 행동을 한 번 이상 했다), 동료가 갈 수 있는 곳 (튜토리얼 제외).</summary>
        public static List<string> VisitedLocations(GameState state, IDefinitionDatabase db)
        {
            var result = new List<string>();
            foreach (var pair in state.Exploration.ActionCounts.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                if (pair.Value <= 0 || !db.TryGetAction(pair.Key, out var action) || !action.IsExpedition || string.IsNullOrEmpty(action.LocationId))
                {
                    continue;
                }

                if (db.TryGetLocation(action.LocationId, out var location) && location.Tutorial)
                {
                    continue;
                }

                if (!result.Contains(action.LocationId) && db.TryGetExpeditionModel(action.LocationId, out _))
                {
                    result.Add(action.LocationId);
                }
            }

            return result;
        }

        /// <summary>갈 곳: 거점에 가장 모자란 자원(식량·식수·자재·의약품)을 많이 주는 곳일수록 잘 고른다.</summary>
        private static string PickLocation(GameState state, IDefinitionDatabase db, List<string> locations, SeededRandom random)
        {
            var needs = new[] { ResourceType.Food, ResourceType.Water, ResourceType.Materials, ResourceType.Medicine };
            var lacking = needs.OrderBy(r => state.Base.GetResource(r)).ThenBy(r => (int)r).First();
            var yields = locations.Select(id =>
            {
                db.TryGetExpeditionModel(id, out var model);
                var range = model?.Day.Resources.FirstOrDefault(r => r.Resource == lacking);
                return range == null ? 0f : (range.Min + range.Max) * 0.5f;
            }).ToList();
            var best = Math.Max(0.0001f, yields.Max());
            var weights = yields.Select(y => 1f + db.Balance.OutingLackWeight * y / best).ToList();
            var roll = random.NextDouble() * weights.Sum();
            for (var i = 0; i < locations.Count; i++)
            {
                roll -= weights[i];
                if (roll <= 0)
                {
                    return locations[i];
                }
            }

            return locations[locations.Count - 1];
        }

        private static bool IsConfinedWeek(GameState state, IDefinitionDatabase db) =>
            state.Plan.Slots.Any(s => !string.IsNullOrEmpty(s.ActionId) && db.TryGetAction(s.ActionId, out var a) && a.Confined);

        /// <summary>(판 시드, 주차, 동료, 쓰임)마다 따로 뽑는 난수.</summary>
        private static SeededRandom Random(GameState state, string npcId, int purpose)
        {
            unchecked
            {
                var hash = (int)2166136261;
                foreach (var c in npcId ?? "")
                {
                    hash = (hash ^ c) * 16777619;
                }

                var seed = (state.Rng?.Seed ?? 0) ^ SeedSalt ^ (state.Calendar.Week * 7919) ^ (purpose * 104729) ^ hash;
                return new SeededRandom(SeededRandom.CreateState(seed));
            }
        }

        private static void AddStress(NpcState npc, int amount, BalanceConfig balance)
        {
            var body = npc.Body ?? (npc.Body = new BodyState());
            body.Stress = Clamp(body.Stress + amount, 0, balance.VitalMax);
        }

        private static int Clamp(int value, int min, int max) => Math.Max(min, Math.Min(max, value));
    }
}
