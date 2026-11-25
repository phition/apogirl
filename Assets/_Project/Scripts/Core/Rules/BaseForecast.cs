using System;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>
    /// 거점 상황판 (Docs/BASE_TAB_PROPOSAL.md P1): 지금 상태로 이번 주 남은 칸을 내다본다.
    /// 화면에 보여 주기만 하므로 상태도 난수도 건드리지 않는다. 계산은 실제 규칙(RaidRules·UpkeepRules·PurifyRules·PowerRules)을 그대로 따른다.
    /// </summary>
    public static class BaseForecast
    {
        public static RaidForecast Raid(GameState state, IDefinitionDatabase db)
        {
            var balance = db.Balance;
            var result = new RaidForecast();
            if (state.Calendar.Week < balance.RaidStartWeek)
            {
                result.NotYet = true;
                return result;
            }

            // 밤 습격은 거점에 있는 밤 칸에서만 굴린다 (밤 탐험·창고에 갇힌 칸은 빠진다, WeekRunner)
            for (var day = state.Calendar.WeekComplete ? CalendarState.DaysPerWeek + 1 : state.Calendar.Day; day <= CalendarState.DaysPerWeek; day++)
            {
                var index = CalendarRules.SlotIndex(day, DayPhase.Night);
                var slot = state.Plan.Slots[index];
                var actionId = string.IsNullOrEmpty(slot.ActionId) ? balance.NightRestActionId : slot.ActionId;
                if (db.TryGetAction(actionId, out var action) && (action.IsExpedition || action.Confined))
                {
                    result.NightsAway++;
                    if (action.IsExpedition)
                    {
                        result.NightsOut++;
                    }

                    continue;
                }

                result.NightsLeft++;
            }

            result.NightChance = RaidRules.Chance(state, balance, db);
            result.WeekChance = result.NightsLeft <= 0 ? 0f : (float)(1.0 - Math.Pow(1.0 - result.NightChance, result.NightsLeft));
            result.Level = result.NightsLeft <= 0 || result.WeekChance <= 0f ? RiskLevel.None
                : result.WeekChance >= balance.BaseBoardRaidHigh ? RiskLevel.High
                : result.WeekChance >= balance.BaseBoardRaidMedium ? RiskLevel.Medium
                : RiskLevel.Low;

            result.RepelParts = Repel(state, db);
            result.RepelChance = result.RepelParts.Chance;
            result.Repel = result.RepelChance >= balance.BaseBoardRepelSafe ? RiskLevel.Low
                : result.RepelChance >= balance.BaseBoardRepelRisky ? RiskLevel.Medium
                : RiskLevel.High;

            // 왜 그런가: 확률을 올리고 내리는 것 (RaidRules.Chance와 같은 항목)
            var gap = state.Base.Threat - state.Base.Defense - SpecialRules.DefenseBonus(state, balance);
            result.Reasons.Add(gap > 0 ? new BaseReason("gap_over", gap, false) : new BaseReason("gap_under", -gap, true));
            if (state.Base.Weather == WeatherType.Rain && balance.RainRaidMultiplier < 1f)
            {
                result.Reasons.Add(new BaseReason("rain", 0, true));
            }

            if (BroadcastRules.RaidMultiplier(state, db, balance) > 1f)
            {
                result.Reasons.Add(new BaseReason("broadcast", 0, false));
            }

            var faction = FactionRules.RaidChanceMultiplier(state, db);
            if (Math.Abs(faction - 1f) > 0.001f || FactionRules.RaidStrengthBonus(state, db) > 0)
            {
                result.Reasons.Add(new BaseReason(faction < 1f ? "faction_down" : "faction_up", 0, faction < 1f));
            }

            if (result.NightsOut > 0)
            {
                result.Reasons.Add(new BaseReason("away_nights", result.NightsOut, true));
            }

            // 막는 힘에 보태는 것
            result.Traps = state.Base.Traps;
            result.MaxTraps = balance.MaxTraps;
            result.Alarm = state.Flags.Contains(DefenseRules.FlagAlarm);
            result.Scarecrow = state.Base.Scarecrow;
            result.ScarecrowMax = ScarecrowRules.IsBell(state, balance) ? balance.ScarecrowBellDurability : balance.ScarecrowDurability;
            result.ScarecrowDown = ScarecrowDown(state, balance);
            result.DefenseMax = DefenseRules.Max(state, balance);

            // 낮 침입 (3막부터): 남은 낮 칸마다 주 확률을 7로 나눈 몫 (IntrusionRules.Roll과 같은 계산)
            var weekly = IntrusionRules.WeeklyChance(state.Calendar.Week, balance);
            if (weekly > 0f)
            {
                weekly += state.Exploration.DayExposure * balance.IntrusionChancePerExposure;
                weekly *= DifficultyRules.Get(state, balance).HumanThreat * FactionRules.IntrusionMultiplier(state, db);
                var days = DaysLeft(state, DayPhase.Day);
                result.IntrusionChance = weekly >= 1f ? (days > 0 ? 1f : 0f) : (float)(1.0 - Math.Pow(1.0 - weekly, days / (double)CalendarState.DaysPerWeek));
            }

            return result;
        }

        /// <summary>습격이 한 번 오면 막을 확률: 세기와 방어력의 흔들림(둘 다 고른 정수)을 모두 세어 본다 (RaidRules.Resolve와 같은 식).</summary>
        public static float RepelChance(GameState state, IDefinitionDatabase db) => Repel(state, db).Chance;

        /// <summary>막는 힘과 습격 세기를 나눠 본다 (상황판의 "왜 막는가" 줄). 식은 RaidRules.Resolve와 같다.</summary>
        public static RepelBreakdown Repel(GameState state, IDefinitionDatabase db)
        {
            var balance = db.Balance;
            var r = new RepelBreakdown
            {
                Base = balance.RaidBasePower,
                Combat = DerivedStats.CombatPower(state, balance),
                Defense = (int)Math.Round((state.Base.Defense + SpecialRules.DefenseBonus(state, balance)) * balance.RaidDefenseWeight),
                Gear = TraitRules.RoundInt(TraitRules.Sum(state, db, TraitModifierKind.RaidGuard))
                       + (state.Flags.Contains(DefenseRules.FlagAlarm) ? balance.AlarmPower : 0)
                       + (state.Base.Traps > 0 ? balance.TrapPower : 0),
                Threat = state.Base.Threat + FactionRules.RaidStrengthBonus(state, db),
                Relief = ScarecrowRules.Relief(state, balance),
                PowerSpread = Math.Max(0, balance.RaidPowerVariance),
                StrengthSpread = Math.Max(0, balance.RaidStrengthVariance),
            };
            r.Chance = Count(r.Power, r.Threat, r.Relief, r.StrengthSpread, r.PowerSpread);
            return r;
        }

        private static float Count(int power, int threat, int relief, int strengthSpread, int powerSpread)
        {
            long wins = 0;
            for (var s = 0; s <= strengthSpread; s++)
            {
                var strength = threat + s;
                strength -= Math.Min(relief, Math.Max(0, strength));
                // power + p >= strength 인 p의 수
                var need = strength - power;
                var count = need <= 0 ? powerSpread + 1 : Math.Max(0, powerSpread - need + 1);
                wins += count;
            }

            return wins / (float)((strengthSpread + 1L) * (powerSpread + 1L));
        }

        public static SupplyForecast Food(GameState state, IDefinitionDatabase db)
        {
            var balance = db.Balance;
            var result = new SupplyForecast { Now = state.Base.GetResource(ResourceType.Food) };
            var stock = result.Now;
            var perDay = UpkeepRules.DietUnits(state.FoodDiet, balance) * UpkeepRules.Headcount(state, balance);
            var saving = TraitRules.Sum(state, db, TraitModifierKind.FoodSaving);
            foreach (var day in Mornings(state))
            {
                var need = UpkeepRules.DailyFoodNeed(state.Calendar.Week, day, perDay, balance);
                if (day % 2 == 0)
                {
                    var step = (state.Calendar.Week - 1) * (CalendarState.DaysPerWeek / 2) + day / 2;
                    need = Math.Max(0, need - TraitRules.Spread(saving, step));
                }

                result.Use += need;
                result.Mornings++;
                if (stock < need && result.ShortDay == 0)
                {
                    result.ShortDay = day;
                }

                stock -= Math.Min(stock, need);
            }

            // 주간 정산: 텃밭(겨울 제외) → 부패 → 고양이 (WeekRunner.FinishWeek 순서)
            if (state.Flags.Contains(UpkeepRules.FlagRoofGarden) && CalendarRules.GetSeason(state.Calendar.Week) != Season.Winter)
            {
                result.Garden = balance.RoofGardenFood;
                stock += result.Garden;
            }

            result.SpoilThreshold = balance.FoodSpoilageThreshold + PowerRules.FoodSpoilageBonus(state, balance);
            result.FridgeBonus = PowerRules.FoodSpoilageBonus(state, balance);
            result.Spoil = UpkeepRules.SpoiledFood(stock, balance, result.FridgeBonus);
            stock -= result.Spoil;
            if (CatRules.IsHome(state) && balance.CatFoodPerWeek > 0 && stock >= balance.CatFoodPerWeek)
            {
                result.Cat = balance.CatFoodPerWeek;
                stock -= result.Cat;
            }

            result.End = stock;
            result.NextWeekNeed = UpkeepRules.WeeklyNeed(state, balance, ResourceType.Food, state.FoodDiet);
            return result;
        }

        public static SupplyForecast Water(GameState state, IDefinitionDatabase db)
        {
            var balance = db.Balance;
            var result = new SupplyForecast { Now = state.Base.GetResource(ResourceType.Water), Raw = state.Base.GetResource(ResourceType.RawWater) };
            var stock = result.Now;
            var raw = result.Raw;
            var purifier = state.Flags.Contains(PurifyRules.FlagAutoPurifier) && PowerRules.IsPowered(state, balance, PurifyRules.PurifierApplianceId);
            foreach (var day in Mornings(state))
            {
                // 자동 정수기는 식사 전에 거른다 (PurifyRules.ApplyMorning)
                if (purifier && raw > 0)
                {
                    var take = Math.Min(balance.AutoPurifyDaily, raw);
                    var made = (int)Math.Round(take * balance.AutoPurifyRate, MidpointRounding.AwayFromZero);
                    raw -= take;
                    stock += made;
                    result.Purified += made;
                }

                var need = UpkeepRules.DailyWaterNeed(state, balance);
                result.Use += need;
                result.Mornings++;
                if (stock < need && result.ShortDay == 0)
                {
                    result.ShortDay = day;
                }

                stock -= Math.Min(stock, need);
            }

            // 주간 정산: 생활용수 + 비 오는 주의 빗물(빗물통이면 더) → 모래 필터 (UpkeepRules.ApplyWeekly)
            var rawGain = balance.WeeklyRawWater;
            if (state.Base.Weather == WeatherType.Rain)
            {
                rawGain += balance.RainCollectWater + (state.Flags.Contains(UpkeepRules.FlagRainBarrels) ? balance.RainBarrelWater : 0);
            }

            result.RawGain = rawGain;
            raw += rawGain;
            if (state.Flags.Contains(PurifyRules.FlagSandFilter) && raw > 0)
            {
                var take = Math.Min(balance.SandFilterWeeklyRaw, raw);
                var made = (int)Math.Round(take * balance.SandFilterRate, MidpointRounding.AwayFromZero);
                raw -= take;
                stock += made;
                result.Purified += made;
            }

            result.End = stock;
            result.RawEnd = raw;
            result.NextWeekNeed = UpkeepRules.WeeklyNeed(state, balance, ResourceType.Water, state.WaterDiet);
            return result;
        }

        public static PowerForecast Power(GameState state, BalanceConfig balance)
        {
            var result = new PowerForecast
            {
                Restored = state.Base.PowerRestored,
                Broken = state.Flags.Contains(PowerRules.FlagBroken),
                Capacity = PowerRules.Capacity(state),
                MaxCapacity = balance.PowerMaxCapacity,
                Free = PowerRules.Free(state, balance),
                Batteries = state.Base.GetResource(ResourceType.Battery),
                UpgradeCost = PowerRules.UpgradeCost(state, balance),
                CanUpgrade = PowerRules.CanUpgrade(state, balance),
            };
            var powered = PowerRules.Powered(state, balance);
            foreach (var appliance in PowerRules.Installed(state, balance))
            {
                result.Appliances.Add(new ApplianceStatus(appliance.Id, appliance.Power, powered.Contains(appliance)));
            }

            return result;
        }

        /// <summary>서랍을 열어 봐야 할 일 (거점 단추의 경고 점).</summary>
        public static List<string> Alerts(GameState state, IDefinitionDatabase db, RaidForecast raid, SupplyForecast food, SupplyForecast water)
        {
            var balance = db.Balance;
            var alerts = new List<string>();
            if (state.Base.PowerRestored && state.Flags.Contains(PowerRules.FlagBroken))
            {
                alerts.Add("generator");
            }

            if (PurifyRules.PurifierStopped(state, balance))
            {
                alerts.Add("purifier");
            }

            if (ScarecrowDown(state, balance))
            {
                alerts.Add("scarecrow");
            }

            if (state.Flags.Contains(RaidRules.FlagBreached))
            {
                alerts.Add("breached");
            }

            if (state.Npcs.Any(n => n.Value.Status == NpcStatus.Joined && n.Value.InjuredWeeks > 0 && !CatRules.IsCat(n.Key)))
            {
                alerts.Add("injured");
            }

            if (food != null && food.ShortDay > 0)
            {
                alerts.Add("food_short");
            }

            if (water != null && water.ShortDay > 0)
            {
                alerts.Add("water_short");
            }

            if (raid != null && raid.Level == RiskLevel.High)
            {
                alerts.Add("raid_high");
            }

            return alerts;
        }

        /// <summary>교문 허수아비를 세운 적이 있는데 지금 쓰러져 있다.</summary>
        public static bool ScarecrowDown(GameState state, BalanceConfig balance) =>
            state.Flags.Contains("CRAFTED_ACT_CRAFT_SCARECROW") && !ScarecrowRules.IsStanding(state);

        /// <summary>아직 먹지 않은 아침이 있는 날들 (낮 칸 시작에 먹는다).</summary>
        private static IEnumerable<int> Mornings(GameState state)
        {
            var calendar = state.Calendar;
            if (calendar.WeekComplete)
            {
                yield break;
            }

            var first = calendar.Phase == DayPhase.Day ? calendar.Day : calendar.Day + 1;
            for (var day = first; day <= CalendarState.DaysPerWeek; day++)
            {
                yield return day;
            }
        }

        private static int DaysLeft(GameState state, DayPhase phase)
        {
            var calendar = state.Calendar;
            if (calendar.WeekComplete)
            {
                return 0;
            }

            var first = phase == DayPhase.Day && calendar.Phase == DayPhase.Night ? calendar.Day + 1 : calendar.Day;
            return Math.Max(0, CalendarState.DaysPerWeek - first + 1);
        }
    }

    public enum RiskLevel
    {
        None,
        Low,
        Medium,
        High,
    }

    /// <summary>상황판 이유 한 줄: 문구 키 base.why.{Key}, 값 {0}, 좋은 쪽이면 Good.</summary>
    public readonly struct BaseReason
    {
        public BaseReason(string key, int value, bool good)
        {
            Key = key;
            Value = value;
            Good = good;
        }

        public string Key { get; }
        public int Value { get; }
        public bool Good { get; }
    }

    /// <summary>습격 한 번을 막는 힘(기본·전투력·방어·함정과 경보) 대 습격 세기(위협·허수아비 덜어 냄), 둘 다 흔들림이 있다.</summary>
    public sealed class RepelBreakdown
    {
        public int Base;
        public int Combat;
        public int Defense;

        /// <summary>함정·경보·경비 특성 몫.</summary>
        public int Gear;
        public int Threat;
        public int Relief;
        public int PowerSpread;
        public int StrengthSpread;
        public float Chance;

        public int Power => Base + Combat + Defense + Gear;
        public int StrengthMin => Math.Max(0, Threat - Relief);
        public int StrengthMax => Math.Max(0, Threat + StrengthSpread - Relief);
    }

    public sealed class RaidForecast
    {
        /// <summary>막는 힘과 습격 세기 (이유 줄).</summary>
        public RepelBreakdown RepelParts;

        /// <summary>아직 습격이 오지 않는 주 (RaidStartWeek 전).</summary>
        public bool NotYet;

        public int NightsLeft;

        /// <summary>습격을 굴리지 않는 밤 (밤 탐험·창고에 갇힌 칸).</summary>
        public int NightsAway;

        /// <summary>그 가운데 밤 탐험 (이유 줄).</summary>
        public int NightsOut;
        public float NightChance;
        public float WeekChance;
        public RiskLevel Level;

        /// <summary>습격이 오면 막을 확률과 그 단계 (Low = 든든, High = 위험).</summary>
        public float RepelChance;

        public RiskLevel Repel;
        public float IntrusionChance;
        public readonly List<BaseReason> Reasons = new List<BaseReason>();
        public int Traps;
        public int MaxTraps;
        public bool Alarm;
        public int Scarecrow;
        public int ScarecrowMax;
        public bool ScarecrowDown;
        public int DefenseMax;
    }

    public sealed class SupplyForecast
    {
        public int Now;
        public int Use;

        /// <summary>이번 주 남은 아침 수 (Use는 그 아침들에 드는 양을 모두 더한 것).</summary>
        public int Mornings;
        public int End;

        /// <summary>처음으로 모자라는 날 (0 = 이번 주는 버틴다).</summary>
        public int ShortDay;

        /// <summary>다음 주 같은 식단에 드는 양.</summary>
        public int NextWeekNeed;

        // 식량
        public int Garden;
        public int Spoil;
        public int Cat;
        public int SpoilThreshold;
        public int FridgeBonus;

        // 물
        public int Raw;
        public int RawGain;
        public int RawEnd;
        public int Purified;
    }

    public sealed class PowerForecast
    {
        public bool Restored;
        public bool Broken;
        public int Capacity;
        public int MaxCapacity;
        public int Free;
        public int Batteries;
        public int UpgradeCost;
        public bool CanUpgrade;
        public readonly List<ApplianceStatus> Appliances = new List<ApplianceStatus>();
    }

    public readonly struct ApplianceStatus
    {
        public ApplianceStatus(string id, int power, bool on)
        {
            Id = id;
            Power = power;
            On = on;
        }

        public string Id { get; }
        public int Power { get; }
        public bool On { get; }
    }
}
