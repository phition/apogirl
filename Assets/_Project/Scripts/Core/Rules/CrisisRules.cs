using System;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Random;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>
    /// 위기 때 남느냐 떠나느냐 (기획 C.2 ③).
    /// - 주 결산 때 호감이 CrisisLeaveBelowAffinity 미만인 동료가 떠날 기색을 보인다 (위기 주가 아니어도, 사용자 결정 2026-11-25 —
    ///   예전엔 식량·식수 바닥이나 습격 돌파 주에만): 깃발 LEAVE_WARN_{NPC} → 다음 주 첫 칸에 EVT_CRISIS_LEAVE_{키}로 붙잡을 기회.
    /// - 습격에 뚫린 밤, 호감이 RaidCoverMinAffinity 이상인 동료가 주인공 앞을 막아서다 대신 다치기도 한다.
    /// </summary>
    public static class CrisisRules
    {
        public static string WarnFlag(BalanceConfig balance, string npcId) => (balance.CrisisLeaveFlagPrefix ?? "LEAVE_WARN_") + npcId;

        /// <summary>떠날 후보(호감이 기준 미만, 낮은 사람부터 한 주에 CrisisLeavePerWeek명)에 예고 깃발을 켜고 그 ID들을 돌려준다.</summary>
        public static List<string> WarnWeekly(GameState state, BalanceConfig balance)
        {
            var warned = new List<string>();
            if (balance.CrisisLeavePerWeek <= 0)
            {
                return warned;
            }

            var candidates = state.Npcs
                .Where(n => n.Key != EffectApplier.TeacherNpcId && !CatRules.IsCat(n.Key) && n.Value.Status == NpcStatus.Joined
                            && n.Value.Affinity < balance.CrisisLeaveBelowAffinity && !state.Flags.Contains(WarnFlag(balance, n.Key)))
                .OrderBy(n => n.Value.Affinity)
                .ThenBy(n => n.Key, StringComparer.Ordinal)
                .Where(n => !EchoRules.HopeSparesLeave(state, balance, n.Key)) // 프롤로그의 메아리 G: 희망이 높으면 확률로 비켜 간다
                .Take(balance.CrisisLeavePerWeek)
                .Select(n => n.Key);
            foreach (var id in candidates)
            {
                state.Flags.Add(WarnFlag(balance, id));
                warned.Add(id);
            }

            return warned;
        }

        /// <summary>
        /// 습격에 뚫린 밤: 집에 있고 다치지 않은, 호감이 기준 이상인 동료 가운데 가장 가까운 사람이 확률로 막아선다.
        /// 막아서면 그 동료가 다치고 ID를 돌려준다 (후보가 없으면 난수를 쓰지 않는다).
        /// </summary>
        public static string RollCover(GameState state, BalanceConfig balance, IRandomService random)
        {
            var best = state.Npcs
                .Where(n => n.Key != EffectApplier.TeacherNpcId && !CatRules.IsCat(n.Key) && n.Value.Status == NpcStatus.Joined && n.Value.InjuredWeeks <= 0
                            && !OutingRules.IsAwayNow(state, n.Value) && n.Value.Affinity >= balance.RaidCoverMinAffinity)
                .OrderByDescending(n => n.Value.Affinity)
                .ThenBy(n => n.Key, StringComparer.Ordinal)
                .Select(n => n.Key)
                .FirstOrDefault();
            if (best == null || random == null || random.NextDouble() >= balance.RaidCoverChance)
            {
                return "";
            }

            state.Npcs[best].InjuredWeeks = Math.Max(state.Npcs[best].InjuredWeeks, balance.RaidCoverInjuryWeeks);
            return best;
        }
    }
}
