using System;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Simulation.Bots
{
    /// <summary>
    /// 봇의 동료 역할 배정 (역할 재기획 8절, P5): 매주 계획 전에.
    /// "Aptitude": 맡을 수 있는 적성 역할이 있으면 그것(적성 순서대로), 없으면 지금 역할이 효과 없을 때만(빈 역할·방 없음·정원 초과)
    /// RolePriority에서 맡을 수 있는 첫 역할. "Keep"(또는 빈 값): 합류 때 역할 그대로.
    /// </summary>
    public static class BotRoles
    {
        public const string Aptitude = "Aptitude";

        public static void Assign(GameState state, IDefinitionDatabase db, BotConfigDef config)
        {
            if (config == null || config.RoleAssignment != Aptitude)
            {
                return;
            }

            var balance = db.Balance;
            foreach (var id in state.Npcs.Where(n => n.Value.Status == NpcStatus.Joined).Select(n => n.Key).OrderBy(k => k, StringComparer.Ordinal).ToList())
            {
                if (config.RoleKeepNpcs.Contains(id))
                {
                    continue;
                }

                var npc = state.Npcs[id];
                var choices = RoleRules.Choices(state, db, id).Select(r => r.Id).ToList();
                var aptitude = balance.RoleAptitudes?.FirstOrDefault(a => a.NpcId == id)?.Roles ?? Array.Empty<string>();

                // 이미 적성 역할을 맡아 효과가 있으면 그대로
                if (aptitude.Contains(npc.Role) && RoleRules.IsActive(state, id, balance) && RoleScale(state, db, npc.Role) > 0f)
                {
                    continue;
                }

                var pick = aptitude.FirstOrDefault(r => choices.Contains(r) && RoleScale(state, db, r) > 0f);
                if (pick == null)
                {
                    var working = !string.IsNullOrEmpty(npc.Role) && RoleRules.IsActive(state, id, balance) && RoleScale(state, db, npc.Role) > 0f;
                    if (working)
                    {
                        continue;
                    }

                    pick = config.RolePriority.FirstOrDefault(r => choices.Contains(r) && RoleScale(state, db, r) > 0f);
                }

                if (pick != null && pick != npc.Role)
                {
                    RoleRules.Assign(state, db, id, pick);
                }
            }
        }

        private static float RoleScale(GameState state, IDefinitionDatabase db, string roleId)
        {
            var role = RoleRules.Find(db, roleId);
            return role == null ? 0f : RoleRules.RoomScale(state, role, db.Balance);
        }
    }
}
