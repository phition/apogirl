using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>함께한 사람 한 명 (그 칸에 그 방에 있던 사람). Gained = 이번에 오른 호감 (주 상한에 걸리면 0).</summary>
    public sealed class TogetherEntry
    {
        public string Id = "";
        public bool Aptitude;
        public int Gained;
    }

    /// <summary>
    /// 함께한 시간 (역할 재기획 4절): 주인공이 방 R의 스케줄을 하면 그 칸에 방 R에 서 있는 동료의 호감이 오른다.
    /// 적성 역할이면 더 많이·주 상한도 높게. 윤 선생님은 선생님이 가르치는 수업에서만(아프면 "선생님 돌보기"에서만).
    /// 부상이면 오르지 않고, 정원 초과는 유효하다. 기록은 사람마다 GiveRules.Log에 (together = 이번 주 오른 양, together_week = 그 주).
    /// </summary>
    public static class TogetherRules
    {
        public const string CountKey = "together";
        public const string WeekKey = "together_week";

        /// <summary>그 행동이 스케줄인 방의 ID (역할 방 묶음 기준). 없으면 null.</summary>
        public static string RoomOf(IDefinitionDatabase db, string actionId)
        {
            if (string.IsNullOrEmpty(actionId))
            {
                return null;
            }

            foreach (var role in db.Extras.Roles)
            {
                foreach (var room in role.Rooms)
                {
                    if (room.Actions != null && room.Actions.Contains(actionId))
                    {
                        return room.Id;
                    }
                }
            }

            return null;
        }

        /// <summary>그 행동을 하는 칸에 곁에 있는 사람 (선생님 먼저, 동료는 ID 순). 호감은 바꾸지 않는다.</summary>
        public static List<TogetherEntry> Present(GameState state, IDefinitionDatabase db, ActionDef action)
        {
            var result = new List<TogetherEntry>();
            if (action == null || action.IsExpedition || action.IsRest)
            {
                return result;
            }

            var balance = db.Balance;
            if (TeacherPresent(state, balance, action))
            {
                result.Add(new TogetherEntry { Id = GiveRules.TeacherId });
            }

            var roomId = RoomOf(db, action.Id);
            if (roomId == null)
            {
                return result;
            }

            foreach (var pair in state.Npcs.Where(n => n.Value.Status == NpcStatus.Joined).OrderBy(n => n.Key, System.StringComparer.Ordinal))
            {
                var npc = pair.Value;
                if (npc.InjuredWeeks > 0 || RoleRules.RoomFor(state, db, pair.Key) != roomId)
                {
                    continue;
                }

                result.Add(new TogetherEntry { Id = pair.Key, Aptitude = RoleRules.HasAptitude(balance, pair.Key, npc.Role) });
            }

            return result;
        }

        /// <summary>칸을 실행한 뒤: 곁에 있던 사람의 호감을 올리고(주 상한까지) 돌봄 기록을 이번 주로. 곁에 있던 사람 목록을 돌려준다.</summary>
        public static List<TogetherEntry> Apply(GameState state, IDefinitionDatabase db, ActionDef action, StateDelta delta)
        {
            var balance = db.Balance;
            var week = state.Calendar.Week;
            var present = Present(state, db, action);
            foreach (var entry in present)
            {
                var teacher = entry.Id == GiveRules.TeacherId;
                var amount = teacher ? balance.TeacherTogetherAffinity
                    : balance.TogetherAffinity + (entry.Aptitude ? balance.TogetherAptitudeBonus : 0);
                var cap = teacher ? balance.TeacherTogetherWeeklyCap
                    : entry.Aptitude ? balance.TogetherAptitudeWeeklyCap : balance.TogetherWeeklyCap;

                var log = GiveRules.Log(state, entry.Id);
                var used = log.TryGetValue(WeekKey, out var logged) && logged == week && log.TryGetValue(CountKey, out var count) ? count : 0;
                var gain = System.Math.Max(0, System.Math.Min(amount, cap - used));
                if (gain > 0)
                {
                    EffectApplier.ChangeNpc(state, new NpcChange { NpcId = entry.Id, Affinity = gain }, balance, delta);
                    log[WeekKey] = week;
                    log[CountKey] = used + gain;
                }

                entry.Gained = gain;
                log[BondDecayRules.CareKey] = week;
            }

            return present;
        }

        /// <summary>
        /// 적성 역할의 방이 하나도 열리지 않아 "그런 곳이 있으면" 하고 바라는 중인가 (방 이름은 말하지 않는다).
        /// 적성이 없거나 적성 역할 가운데 하나라도 맡을 수 있으면 false.
        /// </summary>
        public static bool WishesForAptitudeRoom(GameState state, IDefinitionDatabase db, string npcId)
        {
            var aptitude = db.Balance.RoleAptitudes?.FirstOrDefault(a => a.NpcId == npcId);
            if (aptitude?.Roles == null || aptitude.Roles.Length == 0)
            {
                return false;
            }

            return !aptitude.Roles.Any(roleId => RoleRules.IsAvailable(state, RoleRules.Find(db, roleId)));
        }

        /// <summary>맡길 수 있는데 아직 맡기지 않은 적성 역할 (인물 서랍 힌트). 없으면 null.</summary>
        public static string OpenAptitudeRole(GameState state, IDefinitionDatabase db, string npcId)
        {
            var aptitude = db.Balance.RoleAptitudes?.FirstOrDefault(a => a.NpcId == npcId);
            if (aptitude?.Roles == null || !state.Npcs.TryGetValue(npcId ?? "", out var npc))
            {
                return null;
            }

            return aptitude.Roles.FirstOrDefault(roleId => roleId != npc.Role && RoleRules.IsAvailable(state, RoleRules.Find(db, roleId)));
        }

        private static bool TeacherPresent(GameState state, BalanceConfig balance, ActionDef action)
        {
            if (!GiveRules.Recipients(state, balance).Contains(GiveRules.TeacherId)
                || (balance.TeacherAbsentFlags != null && balance.TeacherAbsentFlags.Any(state.Flags.Contains)))
            {
                return false;
            }

            var ill = balance.TeacherIllFlags != null && balance.TeacherIllFlags.Any(state.Flags.Contains);
            return ill ? action.Id == balance.TeacherCareActionId : action.Teaching != TeachingMode.None;
        }
    }
}
