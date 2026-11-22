using ApoGirl.Core.Definitions;
using ApoGirl.Core.Random;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>방송 한 번의 결과 (확장 5).</summary>
    public sealed class BroadcastOutcome
    {
        public string Program = "";

        /// <summary>누군가 응답했다 (이어서 응답 장면).</summary>
        public bool Replied;

        /// <summary>이번 주 밤 습격 확률이 오른다 (위치가 알려졌다).</summary>
        public bool Risky;
    }

    /// <summary>
    /// 방송반 송출 (확장 5): 3막에 DJ와 이어진 뒤, 주 1회 밤 방송실에서 짧은 방송을 내보낸다.
    /// 프로그램별 효과(능력치·스트레스·UBC 평판)는 행동 데이터에 있고, 여기서는 응답과 위험만 다룬다.
    /// </summary>
    public static class BroadcastRules
    {
        /// <summary>이번 주에 방송을 내보냈다 (다음 주 시작에 지운다).</summary>
        public const string FlagOnAir = "BROADCAST_ON_AIR";

        /// <summary>이번 방송에 누군가 응답했다 (응답 장면이 지운다).</summary>
        public const string FlagReply = "BROADCAST_REPLY";

        public const string ReplyEventPrefix = "EVT_BROADCAST_REPLY_";

        /// <summary>방송을 듣고 찾아올 세력이 아직 휴전 전이다. 세력 정의가 없거나 사라졌으면 위험이 없다.</summary>
        public static bool AtRisk(GameState state, IDefinitionDatabase db, BalanceConfig balance)
        {
            if (db == null || !FactionRules.TryGetDef(db, balance.BroadcastRiskFaction, out var def) || FactionRules.IsGone(state, def))
            {
                return false;
            }

            var standing = state.Factions.TryGetValue(def.Id, out var faction) ? faction.Standing : def.StartStanding;
            return FactionRules.TierOf(standing) < FactionTier.Neutral;
        }

        /// <summary>밤 습격 확률 배율: 이번 주에 방송했고 위험한 세력이 있으면 오른다.</summary>
        public static float RaidMultiplier(GameState state, IDefinitionDatabase db, BalanceConfig balance) =>
            state.Flags.Contains(FlagOnAir) && AtRisk(state, db, balance) ? balance.BroadcastRaidMultiplier : 1f;

        public static void StartWeek(GameState state) => state.Flags.Remove(FlagOnAir);

        /// <summary>방송 행동을 실행한 칸: 방송 깃발, 응답 굴림. 방송이 아니거나 실패(전력 부족)면 null.</summary>
        public static BroadcastOutcome Apply(GameState state, IDefinitionDatabase db, ActionDef action, CheckResult result, IRandomService random)
        {
            if (action == null || !action.IsBroadcast || result == CheckResult.Failure)
            {
                return null;
            }

            var balance = db.Balance;
            state.Flags.Add(FlagOnAir);
            state.Flags.Remove(FlagReply);
            var outcome = new BroadcastOutcome { Program = action.BroadcastProgram, Risky = AtRisk(state, db, balance) };
            if (random.NextDouble() < balance.BroadcastReplyChance)
            {
                state.Flags.Add(FlagReply);
                outcome.Replied = true;
            }

            return outcome;
        }
    }
}
