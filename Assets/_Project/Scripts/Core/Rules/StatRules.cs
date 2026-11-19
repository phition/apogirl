using System;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>스탯 성장·숙련도 레벨 규칙.</summary>
    public static class StatRules
    {
        /// <summary>현재 값에 해당하는 성장 체감 배율 (GrowthBands는 FromValue 오름차순).</summary>
        public static float GrowthMultiplierAt(int currentValue, BalanceConfig balance)
        {
            var multiplier = 1f;
            foreach (var band in balance.GrowthBands)
            {
                if (currentValue >= band.FromValue)
                {
                    multiplier = band.Multiplier;
                }
            }

            return multiplier;
        }

        /// <summary>기본 성장량 × 보정 × 체감 배율. 반올림 결과가 0이면 성장 없음.</summary>
        public static int ScaleGain(int currentValue, int baseAmount, float multiplier, BalanceConfig balance)
        {
            if (baseAmount <= 0)
            {
                return baseAmount;
            }

            var raw = baseAmount * multiplier * GrowthMultiplierAt(currentValue, balance);
            return (int)Math.Round(raw, MidpointRounding.AwayFromZero);
        }

        public static int LevelForXp(int xp, BalanceConfig balance)
        {
            var level = 0;
            var thresholds = balance.ProficiencyXpThresholds;
            for (var i = 0; i < thresholds.Length; i++)
            {
                if (xp >= thresholds[i])
                {
                    level = i + 1;
                }
            }

            return Math.Min(level, balance.ProficiencyMaxLevel);
        }

        /// <summary>단계별 표에서 값을 읽는다. 단계가 표보다 크면 마지막 값.</summary>
        public static float StageValue(float[] table, int stage)
        {
            if (table == null || table.Length == 0)
            {
                return 0f;
            }

            var index = Math.Min(Math.Max(stage, 0), table.Length - 1);
            return table[index];
        }

        /// <summary>수면 부채 단계에 따른 스트레스 증가 배율을 적용한 값.</summary>
        public static int ScaleStress(int amount, int sleepDebt, BalanceConfig balance)
        {
            if (amount <= 0)
            {
                return amount;
            }

            var multiplier = StageValue(balance.SleepDebtStressMultiplier, sleepDebt);
            return (int)Math.Round(amount * multiplier, MidpointRounding.AwayFromZero);
        }
    }

    /// <summary>파생 수치 (문서 5.1.2).</summary>
    public static class DerivedStats
    {
        public static int CombatPower(GameState state, BalanceConfig balance)
        {
            var player = state.Player;
            var value = player.GetStat(StatType.Strength) * balance.CombatStrength
                        + player.GetStat(StatType.Agility) * balance.CombatAgility
                        + player.GetStat(StatType.Stamina) * balance.CombatStamina
                        + player.ProficiencyLevel(ProficiencyType.SelfDefense) * balance.CombatPerSelfDefenseLevel;
            return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }

        public static int StressThreshold(GameState state, BalanceConfig balance)
        {
            var player = state.Player;
            var value = balance.StressThresholdBase
                        + player.GetStat(StatType.Willpower) * balance.StressThresholdPerWillpower
                        + player.ProficiencyLevel(ProficiencyType.Mind) * balance.StressThresholdPerMindLevel;
            return (int)Math.Round(Math.Min(value, balance.StressThresholdCap), MidpointRounding.AwayFromZero);
        }

        /// <summary>스트레스가 임계값을 넘었는가 (상태이상 이벤트 판정 대상, 이벤트 자체는 M3).</summary>
        public static bool IsOverStressed(GameState state, BalanceConfig balance)
        {
            return state.Player.Vitals.Stress > StressThreshold(state, balance);
        }
    }
}
