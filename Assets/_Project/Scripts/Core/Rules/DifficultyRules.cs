using System;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>현재 판의 난이도 (정의가 없으면 표준 = 배율 1).</summary>
    public static class DifficultyRules
    {
        public const string Story = "STORY";
        public const string Standard = "STANDARD";
        public const string Hard = "HARD";

        private static readonly DifficultyDef Neutral = new DifficultyDef();

        public static DifficultyDef Get(GameState state, BalanceConfig balance)
        {
            return Find(state?.Difficulty, balance) ?? Neutral;
        }

        public static DifficultyDef Find(string id, BalanceConfig balance)
        {
            if (balance?.Difficulties == null || string.IsNullOrEmpty(id))
            {
                return null;
            }

            return Array.Find(balance.Difficulties, d => d != null && d.Id == id);
        }

        /// <summary>정수 수치에 배율을 곱한다 (반올림, 원래 값이 양수면 최소 1).</summary>
        public static int Scale(int value, float multiplier)
        {
            if (value <= 0 || Math.Abs(multiplier - 1f) < 0.0001f)
            {
                return value;
            }

            return Math.Max(1, (int)Math.Round(value * multiplier, MidpointRounding.AwayFromZero));
        }

        /// <summary>하루 단위로 나눠 쓰는 소모량 (소수점 누적: 2 × 0.75 → 1,2,1,2,1,2,1).</summary>
        public static int DailyShare(int perDay, double multiplier, int day)
        {
            if (Math.Abs(multiplier - 1.0) < 0.0001)
            {
                return perDay;
            }

            var amount = perDay * multiplier;
            return (int)Math.Floor(amount * day + 1e-6) - (int)Math.Floor(amount * (day - 1) + 1e-6);
        }
    }
}
