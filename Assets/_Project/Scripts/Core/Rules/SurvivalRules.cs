using System;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
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
        /// 매일 밤 처리: 포만·수분이 빠지고, 0 아래면 그날 몫을 먹고 마셔 0까지 채운다 (0 이상이면 먹지 않아 아낀다).
        /// 그다음 포만·수분 단계에 따라 체력·스트레스가 오르내린다 (배부르면 버프, 허기지면 디버프).
        /// </summary>
        public static void ApplyDaily(GameState state, BalanceConfig balance, StateDelta delta, int foodSaving = 0)
        {
            var difficulty = DifficultyRules.Get(state, balance);
            var baseFood = DailyFoodNeed(state.Calendar.Week, state.Calendar.Day, balance, difficulty.Consumption);
            var food = state.Calendar.Day % 2 == 0 ? Math.Max(0, baseFood - foodSaving) : baseFood;
            var foodShare = Share(state, ResourceType.Food, food);
            Nourish(state, ResourceType.Food, food, VitalType.Satiety, balance.SatietyDailyDrain, balance.MealSatiety, balance, delta);
            var water = DifficultyRules.DailyShare(balance.DailyWater, difficulty.Consumption, state.Calendar.Day)
                        + (state.Base.Weather == WeatherType.Heat ? balance.HeatExtraWater : 0);
            var waterShare = Share(state, ResourceType.Water, water);
            Nourish(state, ResourceType.Water, water, VitalType.Hydration, balance.HydrationDailyDrain, balance.DrinkHydration, balance, delta);
            if (state.Base.Weather == WeatherType.Cold && balance.ColdDailyStress > 0)
            {
                // M6: 한파 (문서 6.6)
                EffectApplier.ChangeVital(state, VitalType.Stress, balance.ColdDailyStress, balance, delta);
            }

            ApplyBand(state, Band(balance.SatietyBands, state.Player.Vitals.Satiety), balance, delta);
            ApplyBand(state, Band(balance.HydrationBands, state.Player.Vitals.Hydration), balance, delta);

            // 기획 D-1: 동료·선생님도 같은 공용 식사로 (그날 식량이 넉넉했던 비율만큼) 채운다. 식량을 더 쓰지 않는다.
            GiveRules.ApplyDaily(state, balance, foodShare, waterShare);
        }

        /// <summary>그날 공용 식사가 넉넉한 비율 (0~1, 필요량이 없으면 1).</summary>
        private static double Share(GameState state, ResourceType resource, int need)
        {
            return need <= 0 ? 1.0 : Math.Min(1.0, state.Base.GetResource(resource) / (double)need);
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

        /// <summary>하루치가 빠지고, 0 아래면 가진 만큼 먹어(마셔) 0까지 채운다 (모자라면 비율만큼).</summary>
        private static void Nourish(
            GameState state, ResourceType resource, int need, VitalType vital, int drain, int restore, BalanceConfig balance, StateDelta delta)
        {
            EffectApplier.ChangeVital(state, vital, -drain, balance, delta);
            var current = state.Player.Vitals.GetVital(vital);
            if (current >= 0 || need <= 0)
            {
                return;
            }

            var have = state.Base.GetResource(resource);
            var used = Math.Min(have, need);
            if (used <= 0)
            {
                return;
            }

            EffectApplier.ChangeResource(state, resource, -used, delta);
            var gain = (int)Math.Round(restore * used / (double)need, MidpointRounding.AwayFromZero);
            EffectApplier.ChangeVital(state, vital, Math.Min(gain, -current), balance, delta);
        }

        /// <summary>그날의 기본 식량 소모. 겨울에는 배율을 하루 단위로 나눠 쓴다 (소수점 누적: 2 × 1.3 → 2,3,2,3,3,2,3).</summary>
        public static int DailyFoodNeed(int week, int day, BalanceConfig balance, float difficultyMultiplier = 1f)
        {
            var multiplier = (double)difficultyMultiplier;
            if (CalendarRules.GetSeason(week) == Season.Winter && balance.WinterFoodMultiplier > 1f)
            {
                multiplier *= balance.WinterFoodMultiplier;
            }

            return DifficultyRules.DailyShare(balance.DailyFood, multiplier, day);
        }

        /// <summary>주간 정산: 청결 저하, 전력(복구 이후만), 겨울 난방 연료, 식량 부패.</summary>
        public static void ApplyWeekly(GameState state, BalanceConfig balance, StateDelta delta)
        {
            EffectApplier.ChangeVital(state, VitalType.Cleanliness, -balance.CleanlinessWeeklyDecay, balance, delta);
            GiveRules.ApplyWeekly(state, balance);

            if (state.Base.PowerRestored)
            {
                EffectApplier.ChangeResource(state, ResourceType.Power, -balance.WeeklyPower, delta);
            }

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
            var spoiled = SpoiledFood(state.Base.GetResource(ResourceType.Food), balance);
            if (spoiled > 0)
            {
                EffectApplier.ChangeResource(state, ResourceType.Food, -spoiled, delta);
            }
        }

        public static int SpoiledFood(int food, BalanceConfig balance)
        {
            var excess = food - balance.FoodSpoilageThreshold;
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
}
