using System.Collections.Generic;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>
    /// 구조 헬기 탑승 (4막 끝). 정원은 BalanceConfig.RescueSeats(기본 8).
    /// 선생님이 완치되고 동료가 모두 모이면 9명이 되어, 구조 전날 밤에 한 명을 남기는 선택(EVT_A4_SEAT_CHOICE)을 한다.
    /// 고른 사람의 짝이 함께 남고, 둘 이상 내려 남는 자리는 사거리와 사이가 좋을 때만 사거리 아이가 탄다.
    /// </summary>
    public static class BoardingRules
    {
        public const string Player = "PLAYER";
        public const string Friend = "FRIEND";
        public const string Teacher = "NPC_TEACHER";
        public const string Nurse = "NPC_NURSE";
        public const string Dohyeon = "NPC_DOHYEON";
        public const string Bystander = "NPC_BYSTANDER";
        public const string Ringleader = "NPC_RINGLEADER";
        public const string Senior = "NPC_SURVIVOR_A";
        public const string Jun = "JUN";

        public const string FlagJunJoins = "JUN_JOINS";
        public const string FlagJunMediated = "JUN_MEDIATED";
        public const string FlagChoseToStay = "CHOSE_TO_STAY";
        public const string FactionSageori = "FAC_SAGEORI";

        /// <summary>좌석 선택 결과 깃발 (선택지 Effect가 켠다).</summary>
        public const string FlagStayTeachers = "SEAT_STAY_TEACHERS";
        public const string FlagStayFriend = "SEAT_STAY_FRIEND";
        public const string FlagStayRingleader = "SEAT_STAY_RINGLEADER";
        public const string FlagStayBystander = "SEAT_STAY_BYSTANDER";
        public const string FlagStaySenior = "SEAT_STAY_SENIOR";
        public const string FlagStaySelf = "SEAT_STAY_SELF";

        public static readonly string[] StayFlags =
        {
            FlagStayTeachers, FlagStayFriend, FlagStayRingleader, FlagStayBystander, FlagStaySenior, FlagStaySelf,
        };

        private static readonly string[] CompanionIds = { Nurse, Dohyeon, Bystander, Ringleader, Senior };

        /// <summary>좌석 선택이 없을 때 헬기에 오를 사람 (주인공·친구·완치된 선생님·합류한 동료·준).</summary>
        public static List<string> Candidates(GameState state)
        {
            var list = new List<string>();
            if (!state.Flags.Contains(FlagChoseToStay)) list.Add(Player);
            list.Add(Friend);
            if (state.Flags.Contains(InfectionRules.FlagTeacherCured)) list.Add(Teacher);
            foreach (var id in CompanionIds)
            {
                if (state.Npcs.TryGetValue(id, out var npc) && npc.Status == NpcStatus.Joined) list.Add(id);
            }

            if (state.Flags.Contains(FlagJunJoins)) list.Add(Jun);
            return list;
        }

        public static int CandidateCount(GameState state) => Candidates(state).Count;

        /// <summary>정원을 넘어 한 명을 남기는 선택이 필요한가.</summary>
        public static bool NeedsSeatChoice(GameState state, BalanceConfig balance) => CandidateCount(state) > balance.RescueSeats;

        /// <summary>고른 결과에 따라 남는 사람 (짝 포함). 고르지 않았으면 빈 목록.</summary>
        public static List<string> Stayers(GameState state)
        {
            var f = state.Flags;
            if (f.Contains(FlagStayTeachers)) return new List<string> { Teacher, Nurse };
            if (f.Contains(FlagStayFriend)) return new List<string> { Friend, Player };
            if (f.Contains(FlagStayRingleader)) return new List<string> { Ringleader, Senior };
            if (f.Contains(FlagStayBystander)) return new List<string> { Bystander, Senior };
            if (f.Contains(FlagStaySenior)) return new List<string> { Senior };
            if (f.Contains(FlagStaySelf)) return new List<string> { Player, Ringleader, Teacher };
            return new List<string>();
        }

        /// <summary>남는 사람을 뺀 탑승자.</summary>
        public static List<string> Boarding(GameState state)
        {
            var list = Candidates(state);
            foreach (var id in Stayers(state)) list.Remove(id);
            return list;
        }

        /// <summary>탑승자를 태우고 남는 빈자리 수.</summary>
        public static int FreeSeats(GameState state, BalanceConfig balance) => System.Math.Max(0, balance.RescueSeats - Boarding(state).Count);

        /// <summary>사거리와 사이가 좋은가 (준이 강철 앞을 막아섰거나 동맹).</summary>
        public static bool SageoriOnGoodTerms(GameState state)
        {
            if (state.Flags.Contains(FlagJunMediated)) return true;
            return state.Factions.TryGetValue(FactionSageori, out var faction) && FactionRules.TierOf(faction.Standing) >= FactionTier.Allied;
        }

        /// <summary>좌석 선택으로 생긴 빈자리에 사거리 아이가 타는가.</summary>
        public static bool SageoriChildBoards(GameState state, BalanceConfig balance) =>
            Stayers(state).Count > 0 && FreeSeats(state, balance) > 0 && SageoriOnGoodTerms(state);
    }
}
