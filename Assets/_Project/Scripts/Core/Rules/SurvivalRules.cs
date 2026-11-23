using System;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Random;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>수면 부채 규칙 (D8: 자연 감소 없음, 의도적 회복으로만 상환).</summary>
    public static class SleepDebtRules
    {
        /// <summary>밤 슬롯 하나가 끝났을 때 호출. 밤 휴식이 규정 횟수 연속되면 부채를 갚는다.</summary>
        public static void OnNightSlotResolved(GameState state, bool wasNightRest, BalanceConfig balance, StateDelta delta)
        {
            var player = state.Player;
            if (!wasNightRest)
            {
                player.NightRestStreak = 0;
                return;
            }

            player.NightRestStreak++;
            if (player.NightRestStreak >= balance.NightRestStreakToRepay)
            {
                EffectApplier.ChangeSleepDebt(state, -balance.NightRestRepayAmount, balance, delta);
                player.NightRestStreak = 0;
            }
        }
    }

    /// <summary>자원 소모 규칙 (문서 5.4, 9.1).</summary>
    public static class UpkeepRules
    {
        public const string FlagRainBarrels = "RAIN_BARRELS";
        public const string FlagRoofGarden = "ROOF_GARDEN";

        /// <summary>
        /// 아침 식사 (낮 칸 시작, 사용자 결정 2026-11-23): 이번 주 식단대로 한 사람당 식량·물을 먹고 마신다.
        /// 식량 1 = 포만 FoodSatietyPerUnit, 물 1 = 수분 WaterHydrationPerUnit (한 사람 몫). 모자라면 있는 만큼 나눠 먹고,
        /// 식단을 못 지킨 날은 주인공·동료마다 스트레스가 DietShortStressMin~Max 오른다. 못 지켰으면 true.
        /// </summary>
        public static bool ApplyMorning(GameState state, BalanceConfig balance, StateDelta delta, IRandomService random, int foodSaving = 0)
        {
            var companions = GiveRules.Recipients(state, balance);
            var foodUnits = DietUnits(state.FoodDiet, balance);
            var waterUnits = DietUnits(state.WaterDiet, balance);
            var food = DailyFoodNeed(state, balance, foodSaving);
            var water = DailyWaterNeed(state, balance);
            var foodShare = Eat(state, ResourceType.Food, food, delta);
            var waterShare = Eat(state, ResourceType.Water, water, delta);
            var satiety = (int)Math.Round(foodUnits * balance.FoodSatietyPerUnit * foodShare, MidpointRounding.AwayFromZero);
            var hydration = (int)Math.Round(waterUnits * balance.WaterHydrationPerUnit * waterShare, MidpointRounding.AwayFromZero);
            EffectApplier.ChangeVital(state, VitalType.Satiety, satiety, balance, delta);
            EffectApplier.ChangeVital(state, VitalType.Hydration, hydration, balance, delta);
            var shortage = foodShare < 1.0 || waterShare < 1.0;
            if (shortage && random != null)
            {
                EffectApplier.ChangeVital(state, VitalType.Stress, ShortStress(balance, random), balance, delta);
            }

            GiveRules.ApplyMeal(state, balance, companions, satiety, hydration, shortage ? random : null);
            return shortage;
        }

        /// <summary>
        /// 밤이 시작되기 전 (밤 칸 시작): 포만·수분이 하루치 빠지고, 단계에 따라 체력·스트레스가 오르내린다 (배부르면 버프, 굶주리면 디버프).
        /// 허기·갈증은 최대 체력·탐험 최대 스태미너도 깎는다 (NourishRules).
        /// </summary>
        public static void ApplyEvening(GameState state, BalanceConfig balance, StateDelta delta)
        {
            EffectApplier.ChangeVital(state, VitalType.Satiety, -balance.SatietyDailyDrain, balance, delta);
            EffectApplier.ChangeVital(state, VitalType.Hydration, -balance.HydrationDailyDrain, balance, delta);
            if (state.Base.Weather == WeatherType.Cold && balance.ColdDailyStress > 0)
            {
                // M6: 한파 (문서 6.6)
                EffectApplier.ChangeVital(state, VitalType.Stress, balance.ColdDailyStress, balance, delta);
            }

            ApplyBand(state, Band(balance.SatietyBands, state.Player.Vitals.Satiety), balance, delta);
            ApplyBand(state, Band(balance.HydrationBands, state.Player.Vitals.Hydration), balance, delta);
            GiveRules.ApplyEvening(state, balance);
        }

        /// <summary>식단 한 단계의 한 사람 하루 소비.</summary>
        public static int DietUnits(DietLevel level, BalanceConfig balance)
        {
            switch (level)
            {
                case DietLevel.Save: return balance.DietSaveUnits;
                case DietLevel.Well: return balance.DietWellUnits;
                default: return balance.DietCareUnits;
            }
        }

        /// <summary>식사하는 사람 수: 주인공 + 선생님·합류한 동료 (고양이는 세지 않는다).</summary>
        public static int Headcount(GameState state, BalanceConfig balance) => 1 + GiveRules.Recipients(state, balance).Count(id => !CatRules.IsCat(id));

        /// <summary>그날 식량 소비: 식단 × 사람 수 × 겨울 배율 (소수점은 날마다 나눠 쌓는다), 짝수 날 식량 절약. 난이도로 할인하지 않는다 (사용자 결정 2026-11-23).</summary>
        public static int DailyFoodNeed(GameState state, BalanceConfig balance, int foodSaving = 0)
        {
            var perDay = DietUnits(state.FoodDiet, balance) * Headcount(state, balance);
            var need = DailyFoodNeed(state.Calendar.Week, state.Calendar.Day, perDay, balance);
            return state.Calendar.Day % 2 == 0 ? Math.Max(0, need - foodSaving) : need;
        }

        /// <summary>그날 물 소비: 식단 × 사람 수, 폭염이면 한 사람당 더.</summary>
        public static int DailyWaterNeed(GameState state, BalanceConfig balance)
        {
            var headcount = Headcount(state, balance);
            return DietUnits(state.WaterDiet, balance) * headcount + (state.Base.Weather == WeatherType.Heat ? balance.HeatExtraWater * headcount : 0);
        }

        /// <summary>그 식단을 한 주 지키는 데 드는 양 (배분 화면 안내용: 겨울은 넣고 날씨·절약은 뺀다).</summary>
        public static int WeeklyNeed(GameState state, BalanceConfig balance, ResourceType resource, DietLevel level)
        {
            var perDay = DietUnits(level, balance) * Headcount(state, balance);
            var total = 0;
            for (var day = 1; day <= CalendarState.DaysPerWeek; day++)
            {
                total += resource == ResourceType.Food ? DailyFoodNeed(state.Calendar.Week, day, perDay, balance) : perDay;
            }

            return total;
        }

        /// <summary>재고에 맞춘 식단 (봇·헤드리스 점검용): 3주를 버틸 만큼 넉넉하면 잘, 한 주를 버티면 챙겨, 아니면 아껴 먹는다.</summary>
        public static DietLevel SuggestDiet(GameState state, BalanceConfig balance, ResourceType resource)
        {
            var have = state.Base.GetResource(resource);
            if (WeeklyNeed(state, balance, resource, DietLevel.Well) * 3 <= have)
            {
                return DietLevel.Well;
            }

            return WeeklyNeed(state, balance, resource, DietLevel.Care) <= have ? DietLevel.Care : DietLevel.Save;
        }

        private static int ShortStress(BalanceConfig balance, IRandomService random) =>
            random.Range(balance.DietShortStressMin, Math.Max(balance.DietShortStressMin, balance.DietShortStressMax));

        /// <summary>있는 만큼 먹는다. 먹은 비율 (0~1, 필요량이 없으면 1).</summary>
        private static double Eat(GameState state, ResourceType resource, int need, StateDelta delta)
        {
            if (need <= 0)
            {
                return 1.0;
            }

            var used = Math.Min(state.Base.GetResource(resource), need);
            if (used > 0)
            {
                EffectApplier.ChangeResource(state, resource, -used, delta);
            }

            return used / (double)need;
        }

        /// <summary>값이 Min 이상인 가장 높은 단계 (없으면 null).</summary>
        public static NourishmentBand Band(NourishmentBand[] bands, int value)
        {
            if (bands == null)
            {
                return null;
            }

            NourishmentBand best = null;
            foreach (var band in bands)
            {
                if (value >= band.Min && (best == null || band.Min > best.Min))
                {
                    best = band;
                }
            }

            return best;
        }

        private static void ApplyBand(GameState state, NourishmentBand band, BalanceConfig balance, StateDelta delta)
        {
            if (band == null)
            {
                return;
            }

            if (band.Hp != 0) EffectApplier.ChangeVital(state, VitalType.Hp, band.Hp, balance, delta);
            if (band.Stress != 0) EffectApplier.ChangeVital(state, VitalType.Stress, band.Stress, balance, delta);
        }

        /// <summary>하루 식량 perDay에 겨울·난이도 배율을 하루 단위로 나눠 쓴다 (소수점 누적: 2 × 1.3 → 2,3,2,3,3,2,3).</summary>
        public static int DailyFoodNeed(int week, int day, int perDay, BalanceConfig balance, float difficultyMultiplier = 1f)
        {
            var multiplier = (double)difficultyMultiplier;
            if (CalendarRules.GetSeason(week) == Season.Winter && balance.WinterFoodMultiplier > 1f)
            {
                multiplier *= balance.WinterFoodMultiplier;
            }

            return DifficultyRules.DailyShare(perDay, multiplier, day);
        }

        /// <summary>주간 정산: 청결 저하, 겨울 난방 연료, 식량 부패 (전력은 유지력이라 줄지 않는다).</summary>
        public static void ApplyWeekly(GameState state, BalanceConfig balance, StateDelta delta)
        {
            EffectApplier.ChangeVital(state, VitalType.Cleanliness, -balance.CleanlinessWeeklyDecay, balance, delta);
            GiveRules.ApplyWeekly(state, balance);

            // M7-2: 겨울 난방
            if (CalendarRules.GetSeason(state.Calendar.Week) == Season.Winter && balance.WinterWeeklyFuel > 0)
            {
                var fuel = state.Base.GetResource(ResourceType.Fuel);
                EffectApplier.ChangeResource(state, ResourceType.Fuel, -Math.Min(fuel, balance.WinterWeeklyFuel), delta);
                if (fuel < balance.WinterWeeklyFuel)
                {
                    EffectApplier.ChangeVital(state, VitalType.Stress, balance.NoHeatStress, balance, delta);
                }
            }

            // 거점 생산: 빗물통(비 오는 주), 옥상 텃밭(겨울 제외)
            // 생활용수: 물탱크·화장실에 남은 물 (그대로는 못 마신다)
            EffectApplier.ChangeResource(state, ResourceType.RawWater, balance.WeeklyRawWater, delta);

            // 비 오는 주: 양동이로 받는 빗물 + 빗물통이 있으면 더
            if (state.Base.Weather == WeatherType.Rain)
            {
                var rain = balance.RainCollectWater + (state.Flags.Contains(FlagRainBarrels) ? balance.RainBarrelWater : 0);
                EffectApplier.ChangeResource(state, ResourceType.RawWater, rain, delta);
            }

            if (state.Flags.Contains(FlagRoofGarden) && CalendarRules.GetSeason(state.Calendar.Week) != Season.Winter)
            {
                EffectApplier.ChangeResource(state, ResourceType.Food, balance.RoofGardenFood, delta);
            }

            // M7-2: 보관량을 넘는 식량은 상한다
            var spoiled = SpoiledFood(state.Base.GetResource(ResourceType.Food), balance, PowerRules.FoodSpoilageBonus(state, balance));   // 냉장고가 켜져 있으면 덜 상한다
            if (spoiled > 0)
            {
                EffectApplier.ChangeResource(state, ResourceType.Food, -spoiled, delta);
            }
        }

        public static int SpoiledFood(int food, BalanceConfig balance, int thresholdBonus = 0)
        {
            var excess = food - balance.FoodSpoilageThreshold - thresholdBonus;
            return excess <= 0 || balance.FoodSpoilageRate <= 0f ? 0 : (int)Math.Ceiling(excess * balance.FoodSpoilageRate - 1e-6);
        }

        /// <summary>필요량을 다 채웠으면 true.</summary>
    }

    /// <summary>달력 규칙.</summary>
    public static class CalendarRules
    {
        public static Season GetSeason(int week)
        {
            if (week <= 13) return Season.Spring;
            if (week <= 26) return Season.Summer;
            if (week <= 39) return Season.Autumn;
            return Season.Winter;
        }

        /// <summary>(1일차 낮 = 0) … (7일차 밤 = 13)</summary>
        public static int SlotIndex(int day, DayPhase phase)
        {
            return (day - 1) * 2 + (phase == DayPhase.Night ? 1 : 0);
        }

        public static DayPhase PhaseOf(int slotIndex)
        {
            return slotIndex % 2 == 0 ? DayPhase.Day : DayPhase.Night;
        }

        /// <summary>그 주 day(1~7)의 요일: 0 = 월 … 5 = 토, 6 = 일.</summary>
        public static int Weekday(int day, BalanceConfig balance)
        {
            var start = balance != null ? balance.StartWeekday : 0;
            return (((start + day - 1) % 7) + 7) % 7;
        }

        public static int DayOf(int slotIndex)
        {
            return slotIndex / 2 + 1;
        }

        private static readonly int[] DaysInMonth = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        /// <summary>주차·요일의 실제 날짜 (월, 일). 학사력 이벤트 조건용 (문서 4.4, D20).</summary>
        public static (int Month, int Day) DateOf(int week, int day, BalanceConfig balance)
        {
            var month = Math.Min(Math.Max(balance.StartMonth, 1), 12);
            var date = Math.Min(Math.Max(balance.StartDayOfMonth, 1), DaysInMonth[month - 1]);
            var offset = Math.Max(0, (week - 1) * CalendarState.DaysPerWeek + (day - 1));
            date += offset;
            while (date > DaysInMonth[month - 1])
            {
                date -= DaysInMonth[month - 1];
                month = month % 12 + 1;
            }

            return (month, date);
        }
    }

    /// <summary>허기·갈증의 대가 (사용자 결정 2026-11-23): 포만 0 아래 1당 최대 체력, 수분 0 아래 1당 탐험 최대 스태미너가 줄어든다 (하한 있음).</summary>
    public static class NourishRules
    {
        /// <summary>value(포만·수분)가 0 아래인 만큼 줄어든 최대치 비율 (1 = 그대로).</summary>
        public static float Factor(int value, float penaltyPerPoint, BalanceConfig balance)
        {
            return value >= 0 ? 1f : Math.Max(balance.NourishMaxFloor, 1f - (-value) * penaltyPerPoint);
        }

        /// <summary>지금 최대 체력 (허기만큼 깎인 값).</summary>
        public static int MaxHp(GameState state, BalanceConfig balance)
        {
            return (int)Math.Round(balance.VitalMax * Factor(state.Player.Vitals.Satiety, balance.HungerMaxHpPenalty, balance), MidpointRounding.AwayFromZero);
        }

        /// <summary>탐험 최대 스태미너 배율 (갈증만큼).</summary>
        public static float StaminaFactor(int hydration, BalanceConfig balance) => Factor(hydration, balance.ThirstStaminaPenalty, balance);
    }
}
