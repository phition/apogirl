using System;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>
    /// 관계 단계 (기획 C절): 호감 하나를 단계로 나눠 효과에 쓴다.
    /// 0 서먹(0 미만) · 1 데면데면 · 2 편함 · 3 가까움 · 4 믿음. 경계는 BalanceConfig.BondTierMins.
    /// </summary>
    public static class BondRules
    {
        public static int Tier(int affinity, BalanceConfig balance)
        {
            var mins = balance.BondTierMins;
            var tier = 0;
            for (var i = 0; i < mins.Length; i++)
            {
                if (affinity >= mins[i])
                {
                    tier = i + 1;
                }
            }

            return tier;
        }

        /// <summary>C.2 ① 역할 효율: 맡긴 역할 효과에 곱하는 배율 (관계 단계별).</summary>
        public static float RoleMultiplier(NpcState npc, BalanceConfig balance)
        {
            var table = balance.BondRoleMultipliers;
            if (npc == null || table == null || table.Length == 0)
            {
                return 1f;
            }

            return table[Math.Min(Tier(npc.Affinity, balance), table.Length - 1)];
        }

        /// <summary>C.4 ① 이벤트 효과로 오르는 동료 호감에 배율 (떨어질 때·선생님은 그대로).</summary>
        public static int ScaleContentGain(string npcId, int affinity, BalanceConfig balance)
        {
            if (affinity <= 0 || npcId == Effects.EffectApplier.TeacherNpcId)
            {
                return affinity;
            }

            return (int)Math.Round(affinity * balance.CompanionAffinityGainScale, MidpointRounding.AwayFromZero);
        }
    }
}
