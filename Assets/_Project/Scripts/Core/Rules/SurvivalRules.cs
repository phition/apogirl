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
        /// 매일 밤 처리: 식량·물 소모, 부족 시 허기·갈증 상승과 HP 감소.
        /// HP 감소는 그날 실제로 먹지(마시지) 못했을 때만. 먹었다면 허기가 높아도 회복만 한다.
        /// </summary>
        public static void ApplyDaily(GameState state, BalanceConfig balance, StateDelta delta, int foodSaving = 0)
        {
            var baseFood = DailyFoodNeed(state.Calendar.Week, state.Calendar.Day, balance);
            var food = state.Calendar.Day % 2 == 0 ? Math.Max(0, baseFood - foodSaving) : baseFood;
            var ate = Consume(state, ResourceType.Food, food, VitalType.Hunger, balance.HungerRecovery, balance.HungerIncrease, balance, delta);
            var water = balance.DailyWater + (state.Base.Weather == WeatherType.Heat ? balance.HeatExtraWater : 0);
            var drank = Consume(state, ResourceType.Water, water, VitalType.Thirst, balance.ThirstRecovery, balance.ThirstIncrease, balance, delta);
            if (state.Base.Weather == WeatherType.Cold && balance.ColdDailyStress > 0)
            {
                // M6: 한파 (문서 6.6)
                EffectApplier.ChangeVital(state, VitalType.Stress, balance.ColdDailyStress, balance, delta);
            }

            var vitals = state.Player.Vitals;
            if (!ate && vitals.Hunger >= balance.StarvationThreshold)
            {
                EffectApplier.ChangeVital(state, VitalType.Hp, -balance.StarvationHpLoss, balance, delta);
            }

            if (!drank && vitals.Thirst >= balance.DehydrationThreshold)
            {
                EffectApplier.ChangeVital(state, VitalType.Hp, -balance.DehydrationHpLoss, balance, delta);
            }
        }

        /// <summary>그날의 기본 식량 소모. 겨울에는 배율을 하루 단위로 나눠 쓴다 (소수점 누적: 2 × 1.3 → 2,3,2,3,3,2,3).</summary>
        public static int DailyFoodNeed(int week, int day, BalanceConfig balance)
        {
            if (CalendarRules.GetSeason(week) != Season.Winter || balance.WinterFoodMultiplier <= 1f)
            {
                return balance.DailyFood;
            }

            var perDay = balance.DailyFood * (double)balance.WinterFoodMultiplier;
            return (int)Math.Floor(perDay * day + 1e-6) - (int)Math.Floor(perDay * (day - 1) + 1e-6);
        }

        /// <summary>주간 정산: 전력(복구 이후만), 겨울 난방 연료, 식량 부패.</summary>
        public static void ApplyWeekly(GameState state, BalanceConfig balance, StateDelta delta)
        {
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
            if (state.Flags.Contains(FlagRainBarrels) && state.Base.Weather == WeatherType.Rain)
            {
                EffectApplier.ChangeResource(state, ResourceType.Water, balance.RainBarrelWater, delta);
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
        private static bool Consume(
            GameState state, ResourceType resource, int need, VitalType vital, int recovery, int increase,
            BalanceConfig balance, StateDelta delta)
        {
            var have = state.Base.GetResource(resource);
            if (have >= need)
            {
                EffectApplier.ChangeResource(state, resource, -need, delta);
                EffectApplier.ChangeVital(state, vital, -recovery, balance, delta);
                return true;
            }

            EffectApplier.ChangeResource(state, resource, -have, delta);
            EffectApplier.ChangeVital(state, vital, increase, balance, delta);
            return false;
        }
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
