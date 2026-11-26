using System;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>
    /// 식량 줄 (제작: 옥상 텃밭 → 비닐하우스 → 스프링클러, 사용자 결정 2026-11-26).
    /// 밭은 하루치(한 주 생산 ÷ 7)씩 자란다. 아침(식사 전)에 순서대로:
    /// ① 살림 동료가 빗물을 조금 거른다 (역할 보정 RainPurify) → ② 스프링클러가 켜져 있고 빗물이 있으면 물을 주어 그날 ×1.5
    /// → ③ 살림 동료가 하루치를 거둔다 (역할 보정 GardenHarvest, 1 = 전부, 상한 GardenHarvestCap). 거두지 못한 몫은 쌓아 두었다가
    /// 주간 정산 때 주인공이 GardenUnpickedRate만큼 거둔다 (나머지는 시든다).
    /// 계산(Morning)은 상태를 바꾸지 않아 거점 예상(BaseForecast)도 같은 식을 쓴다.
    /// </summary>
    public static class GardenRules
    {
        public const string FlagGreenhouse = "GREENHOUSE";
        public const string FlagSprinkler = "SPRINKLER";
        public const string SprinklerApplianceId = "SPRINKLER";

        /// <summary>밭이 있는가 (텃밭 또는 비닐하우스).</summary>
        public static bool HasField(GameState state) =>
            state.Flags.Contains(UpkeepRules.FlagRoofGarden) || state.Flags.Contains(FlagGreenhouse);

        /// <summary>이번 주 한 주 생산: 비닐하우스면 GreenhouseFood(겨울엔 비율), 텃밭이면 RoofGardenFood(겨울엔 0).</summary>
        public static float WeeklyYield(GameState state, BalanceConfig balance)
        {
            var winter = CalendarRules.GetSeason(state.Calendar.Week) == Season.Winter;
            if (state.Flags.Contains(FlagGreenhouse))
            {
                return balance.GreenhouseFood * (winter ? balance.GreenhouseWinterRatio : 1f);
            }

            return state.Flags.Contains(UpkeepRules.FlagRoofGarden) && !winter ? balance.RoofGardenFood : 0f;
        }

        /// <summary>스프링클러가 있는데 전기가 없어 멈췄다.</summary>
        public static bool SprinklerStopped(GameState state, BalanceConfig balance) =>
            state.Flags.Contains(FlagSprinkler) && !PowerRules.IsPowered(state, balance, SprinklerApplianceId);

        /// <summary>
        /// 아침 한 번의 계산 (상태는 바꾸지 않는다). raw = 지금 빗물(자동 정수기가 거른 뒤), carry·unpicked = 지금까지 쌓인 몫,
        /// step = 날 번호(주 × 7 + 날, 정수 보정의 소수점 나누기). 돌려준 값으로 Apply하거나 예상에 더한다.
        /// </summary>
        public static GardenMorning Morning(GameState state, IDefinitionDatabase db, int raw, float carry, float unpicked, int step)
        {
            var balance = db.Balance;
            var result = new GardenMorning { Carry = carry, Unpicked = unpicked };

            // ① 살림 동료가 빗물을 거른다
            var purify = TraitRules.Sum(state, db, TraitModifierKind.RainPurify);
            var take = Math.Min(raw, TraitRules.Spread(purify * Math.Max(0, balance.HouseRainPurifyDaily), step));
            if (take > 0)
            {
                result.RawUsed += take;
                result.Water = (int)Math.Round(take * balance.HouseRainPurifyRate, MidpointRounding.AwayFromZero);
                raw -= take;
            }

            // ② 스프링클러: 밭이 자라는 날, 켜져 있고 빗물이 있으면
            var daily = WeeklyYield(state, balance) / CalendarState.DaysPerWeek;
            if (daily <= 0f)
            {
                return result;
            }

            if (state.Flags.Contains(FlagSprinkler) && PowerRules.IsPowered(state, balance, SprinklerApplianceId)
                && raw >= balance.SprinklerRawWaterDaily)
            {
                result.Sprinkled = true;
                result.RawUsed += balance.SprinklerRawWaterDaily;
                daily *= balance.SprinklerYieldMultiplier;
            }

            result.Grown = daily;

            // ③ 살림 동료가 거둔다 (나머지는 정산 때 주인공이)
            var harvest = Math.Max(0f, TraitRules.Sum(state, db, TraitModifierKind.GardenHarvest));
            var picked = daily * Math.Min(harvest, balance.GardenHarvestCap);
            result.Unpicked += daily * Math.Max(0f, 1f - harvest);
            result.Carry += picked;
            result.Food = (int)Math.Floor(result.Carry + 1e-4f);
            result.Carry -= result.Food;
            return result;
        }

        /// <summary>아침(식사 전, 자동 정수기 다음): 거르고 물 주고 거둔다. 실제 상태에 적용한 결과.</summary>
        public static GardenMorning ApplyMorning(GameState state, IDefinitionDatabase db, StateDelta delta)
        {
            var calendar = state.Calendar;
            var result = Morning(state, db, state.Base.GetResource(ResourceType.RawWater), state.Base.GardenCarry, state.Base.GardenUnpicked,
                Step(calendar.Week, calendar.Day));
            if (result.RawUsed > 0)
            {
                EffectApplier.ChangeResource(state, ResourceType.RawWater, -result.RawUsed, delta);
            }

            if (result.Water > 0)
            {
                EffectApplier.ChangeResource(state, ResourceType.Water, result.Water, delta);
            }

            if (result.Food > 0)
            {
                EffectApplier.ChangeResource(state, ResourceType.Food, result.Food, delta);
            }

            state.Base.GardenCarry = result.Carry;
            state.Base.GardenUnpicked = result.Unpicked;
            return result;
        }

        /// <summary>주간 정산: 거두지 못한 밭 식량을 주인공이 GardenUnpickedRate만큼 거둔다 (반올림). 거둔 양.</summary>
        public static int WeeklyPick(float unpicked, BalanceConfig balance) =>
            unpicked <= 0f ? 0 : (int)Math.Round(unpicked * balance.GardenUnpickedRate, MidpointRounding.AwayFromZero);

        public static int ApplyWeekly(GameState state, BalanceConfig balance, StateDelta delta)
        {
            var food = WeeklyPick(state.Base.GardenUnpicked, balance);
            state.Base.GardenUnpicked = 0f;
            if (food > 0)
            {
                EffectApplier.ChangeResource(state, ResourceType.Food, food, delta);
            }

            return food;
        }

        /// <summary>날 번호 (정수 보정의 소수점을 나누는 차례).</summary>
        public static int Step(int week, int day) => (week - 1) * CalendarState.DaysPerWeek + day;
    }

    /// <summary>아침 한 번의 밭·빗물 계산 결과.</summary>
    public sealed class GardenMorning
    {
        /// <summary>쓴 빗물 (살림 동료 정수 + 스프링클러).</summary>
        public int RawUsed;

        /// <summary>살림 동료가 거른 식수.</summary>
        public int Water;

        /// <summary>스프링클러로 물을 주었다.</summary>
        public bool Sprinkled;

        /// <summary>오늘 자란 양 (스프링클러 배율 포함).</summary>
        public float Grown;

        /// <summary>살림 동료가 오늘 창고에 넣은 식량.</summary>
        public int Food;

        public float Carry;
        public float Unpicked;
    }
}
