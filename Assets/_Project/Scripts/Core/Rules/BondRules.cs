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

    /// <summary>윤 선생님 호감 구간 효과 (기획 C.1): 선생님이 가르치는 교육 행동의 성공률·숙련 경험치·대성공.</summary>
    public static class MentorBondRules
    {
        /// <summary>지금 적용되는 구간 (선생님이 가르치는 행동이 아니거나, 선생님이 없거나, 구간 미만이면 null).</summary>
        public static TeacherBondBand Band(GameState state, ActionDef action, BalanceConfig balance)
        {
            if (action == null || action.Teaching == TeachingMode.None || !state.Teacher.Alive || balance.TeacherBondBands == null)
            {
                return null;
            }

            TeacherBondBand best = null;
            foreach (var band in balance.TeacherBondBands)
            {
                if (state.Teacher.Affinity >= band.MinAffinity && (best == null || band.MinAffinity > best.MinAffinity))
                {
                    best = band;
                }
            }

            return best;
        }

        /// <summary>병세 단계만큼 멘토 기능이 약해진다 (TeachingMultiplier와 같은 비율).</summary>
        private static float Strength(GameState state, BalanceConfig balance) =>
            1f - Math.Min(1f, InfectionRules.TeacherStage(state, balance) * balance.MentorDecayPerStage);

        public static float CheckBonus(GameState state, ActionDef action, BalanceConfig balance) =>
            (Band(state, action, balance)?.CheckBonus ?? 0f) * Strength(state, balance);

        public static float GreatSuccessBonus(GameState state, ActionDef action, BalanceConfig balance) =>
            (Band(state, action, balance)?.GreatSuccessBonus ?? 0f) * Strength(state, balance);

        public static int ScaleXp(GameState state, ActionDef action, int xp, BalanceConfig balance)
        {
            var band = Band(state, action, balance);
            if (band == null)
            {
                return xp;
            }

            var multiplier = 1f + (band.XpMultiplier - 1f) * Strength(state, balance);
            return (int)Math.Round(xp * multiplier, MidpointRounding.AwayFromZero);
        }
    }
}
