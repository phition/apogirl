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
        /// <summary>매일 밤 처리: 식량·물 소모, 부족 시 허기·갈증 상승과 HP 감소.</summary>
        public static void ApplyDaily(GameState state, BalanceConfig balance, StateDelta delta)
        {
            Consume(state, ResourceType.Food, balance.DailyFood, VitalType.Hunger, balance.HungerRecovery, balance.HungerIncrease, balance, delta);
            Consume(state, ResourceType.Water, balance.DailyWater, VitalType.Thirst, balance.ThirstRecovery, balance.ThirstIncrease, balance, delta);

            var vitals = state.Player.Vitals;
            if (vitals.Hunger >= balance.StarvationThreshold)
            {
                EffectApplier.ChangeVital(state, VitalType.Hp, -balance.StarvationHpLoss, balance, delta);
            }

            if (vitals.Thirst >= balance.DehydrationThreshold)
            {
                EffectApplier.ChangeVital(state, VitalType.Hp, -balance.DehydrationHpLoss, balance, delta);
            }
        }

        /// <summary>주간 정산: 전력은 복구 이후에만 소모 (복구 전 정전이 기본 상태).</summary>
        public static void ApplyWeekly(GameState state, BalanceConfig balance, StateDelta delta)
        {
            if (!state.Base.PowerRestored)
            {
                return;
            }

            EffectApplier.ChangeResource(state, ResourceType.Power, -balance.WeeklyPower, delta);
        }

        private static void Consume(
            GameState state, ResourceType resource, int need, VitalType vital, int recovery, int increase,
            BalanceConfig balance, StateDelta delta)
        {
            var have = state.Base.GetResource(resource);
            if (have >= need)
            {
                EffectApplier.ChangeResource(state, resource, -need, delta);
                EffectApplier.ChangeVital(state, vital, -recovery, balance, delta);
            }
            else
            {
                EffectApplier.ChangeResource(state, resource, -have, delta);
                EffectApplier.ChangeVital(state, vital, increase, balance, delta);
            }
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
