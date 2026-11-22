using System;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>동아리 한 번의 결과 (확장 7). 같이 한 사람이 없으면 null.</summary>
    public sealed class ClubOutcome
    {
        public string NpcId = "";
        public string Activity = "";

        /// <summary>그 사람이 좋아하는 활동이었다.</summary>
        public bool Liked;

        public int Gained;

        /// <summary>이번에 동아리 개인 장면이 열렸다.</summary>
        public bool SceneUnlocked;
    }

    /// <summary>
    /// 방과후 동아리 (확장 7): 주 1회 밤, 동료 한 명과 활동 하나. 같이 할 사람은 인물 서랍에서 고르고(GameState.ClubPartner),
    /// 없으면 집에 있는 사람 가운데 호감이 가장 높은 사람. 고양이는 같이 하지 않는다.
    /// 좋아하는 활동을 여러 번 하면 그 사람의 동아리 개인 장면이 열린다 (깃발 CLUB_SCENE_{NPC}).
    /// </summary>
    public static class ClubRules
    {
        /// <summary>이번 동아리를 같이 한 사람 (대본 분기용, 다음 동아리 때 지운다).</summary>
        public const string WithPrefix = "CLUB_WITH_";

        public static string WithFlag(string npcId) => WithPrefix + npcId;

        public static string SceneFlag(string npcId) => "CLUB_SCENE_" + npcId;

        public static string LikedActivity(BalanceConfig balance, string npcId) =>
            balance.ClubLikes?.FirstOrDefault(l => l.NpcId == npcId)?.Activity ?? "";

        public static bool Likes(BalanceConfig balance, string npcId, string activity) =>
            !string.IsNullOrEmpty(activity) && LikedActivity(balance, npcId) == activity;

        /// <summary>같이 할 수 있는 사람: 합류했고, 사람이고(고양이 제외), 밖에 나가 있지 않다.</summary>
        public static bool CanJoin(GameState state, string npcId) =>
            !string.IsNullOrEmpty(npcId) && !CatRules.IsCat(npcId) && state.Npcs.TryGetValue(npcId, out var npc)
            && npc.Status == NpcStatus.Joined && !OutingRules.IsAwayNow(state, npc);

        /// <summary>같이 할 수 있는 사람들 (호감 높은 순, 같으면 ID 순).</summary>
        public static List<string> Candidates(GameState state) =>
            state.Npcs.Where(n => CanJoin(state, n.Key))
                .OrderByDescending(n => n.Value.Affinity)
                .ThenBy(n => n.Key, StringComparer.Ordinal)
                .Select(n => n.Key)
                .ToList();

        /// <summary>이번 동아리를 같이 할 사람: 고른 사람이 있으면 그 사람, 아니면 호감이 가장 높은 사람. 없으면 null.</summary>
        public static string Partner(GameState state) =>
            CanJoin(state, state.ClubPartner) ? state.ClubPartner : Candidates(state).FirstOrDefault();

        public static int Count(GameState state, string npcId, string activity) =>
            state.Npcs.TryGetValue(npcId ?? "", out var npc) && npc.ClubCounts.TryGetValue(activity ?? "", out var count) ? count : 0;

        /// <summary>계획에 이미 놓인 동아리 수 (그 칸은 빼고 센다).</summary>
        public static int InPlan(GameState state, IDefinitionDatabase db, int exceptSlot)
        {
            var count = 0;
            for (var i = 0; i < state.Plan.Slots.Count; i++)
            {
                if (i != exceptSlot && db.TryGetAction(state.Plan.Slots[i].ActionId ?? "", out var other) && other.IsClub)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>동아리 행동을 실행한 칸: 같이 한 사람의 호감, 활동 횟수, 개인 장면 깃발.</summary>
        public static ClubOutcome Apply(GameState state, IDefinitionDatabase db, ActionDef action, StateDelta delta)
        {
            if (action == null || !action.IsClub)
            {
                return null;
            }

            state.Flags.RemoveWhere(f => f.StartsWith(WithPrefix, StringComparison.Ordinal));
            var id = Partner(state);
            if (id == null)
            {
                return null;
            }

            var balance = db.Balance;
            var npc = state.Npcs[id];
            var liked = Likes(balance, id, action.ClubActivity);
            var amount = liked ? balance.ClubLikedAffinity : balance.ClubAffinity;
            var before = npc.Affinity;
            EffectApplier.ChangeNpc(state, new NpcChange { NpcId = id, Affinity = amount }, balance, delta);
            GiveRules.Log(state, id)[BondDecayRules.CareKey] = state.Calendar.Week;   // 돌봄으로 센다 (오래 돌보지 않음 방지)
            state.Flags.Add(WithFlag(id));

            npc.ClubCounts.TryGetValue(action.ClubActivity, out var count);
            npc.ClubCounts[action.ClubActivity] = ++count;
            var outcome = new ClubOutcome { NpcId = id, Activity = action.ClubActivity, Liked = liked, Gained = npc.Affinity - before };
            if (liked && count >= balance.ClubSceneCount && !state.Flags.Contains(SceneFlag(id)))
            {
                state.Flags.Add(SceneFlag(id));
                outcome.SceneUnlocked = true;
            }

            return outcome;
        }
    }
}
