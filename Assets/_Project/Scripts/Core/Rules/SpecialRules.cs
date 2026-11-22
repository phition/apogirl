using System;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>
    /// 동료 특기 (기획 C.2 ②): 그 동료의 마지막 개인 장면(호감 80)을 보면 깃발 SPECIAL_{NPC}가 켜지고, 역할과 무관하게 늘 켜진다.
    /// 합류해 있고 호감이 SpecialMinAffinity 이상일 때만 효과가 있다 (떨어지면 잠시 꺼진다, C.5). 표는 BalanceConfig.CompanionSpecials.
    /// </summary>
    public static class SpecialRules
    {
        public static string Flag(BalanceConfig balance, string npcId) => (balance.SpecialFlagPrefix ?? "SPECIAL_") + npcId;

        public static CompanionSpecialDef Find(BalanceConfig balance, string npcId) =>
            balance.CompanionSpecials?.FirstOrDefault(s => s.NpcId == npcId);

        /// <summary>마지막 개인 장면을 봐서 특기를 얻었다 (꺼져 있어도 true).</summary>
        public static bool IsUnlocked(GameState state, BalanceConfig balance, string npcId) =>
            Find(balance, npcId) != null && state.Flags.Contains(Flag(balance, npcId));

        /// <summary>지금 효과가 있다: 얻었고, 합류해 있고, 호감이 기준 이상.</summary>
        public static bool IsActive(GameState state, BalanceConfig balance, string npcId)
        {
            return IsUnlocked(state, balance, npcId) && state.Npcs.TryGetValue(npcId ?? "", out var npc)
                   && npc.Status == NpcStatus.Joined && npc.Affinity >= balance.SpecialMinAffinity;
        }

        /// <summary>켜져 있는 특기의 값 합 (대상이 있으면 그 대상만).</summary>
        public static float Value(GameState state, BalanceConfig balance, SpecialKind kind, string target = null)
        {
            if (balance.CompanionSpecials == null || state == null)
            {
                return 0f;
            }

            var sum = 0f;
            foreach (var special in balance.CompanionSpecials)
            {
                if (special.Kind == kind && (target == null || special.Target == target) && IsActive(state, balance, special.NpcId))
                {
                    sum += special.Value;
                }
            }

            return sum;
        }

        /// <summary>행동의 자원 비용 (박 선생님: 의약품 2개 이상 드는 행동은 하나 덜, 최소 1).</summary>
        public static int Cost(GameState state, BalanceConfig balance, ResourceAmount cost)
        {
            if (cost.Resource != ResourceType.Medicine || cost.Amount < 2)
            {
                return cost.Amount;
            }

            var discount = (int)Math.Round(Value(state, balance, SpecialKind.MedicineCostDown));
            return Math.Max(1, cost.Amount - discount);
        }

        /// <summary>습격·침입 계산에 더하는 방어도 (선배).</summary>
        public static int DefenseBonus(GameState state, BalanceConfig balance) =>
            (int)Math.Round(Value(state, balance, SpecialKind.DefenseBonus));

        /// <summary>습격에 뚫렸을 때 피해 배율 (도현: 0.8).</summary>
        public static float RaidDamageScale(GameState state, BalanceConfig balance) =>
            Math.Max(0f, 1f - Value(state, balance, SpecialKind.RaidDamageCut));

        /// <summary>습격 피해 한 가지에 배율을 곱한다 (반올림, 피해가 있으면 최소 1).</summary>
        public static int ScaleDamage(int amount, float scale) =>
            amount <= 0 ? amount : Math.Max(1, (int)Math.Round(amount * scale));
    }
}
