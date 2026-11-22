using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>
    /// 호감이 떨어지는 경우 (기획 C.5) 중 지금 데이터로 판정할 수 있는 것.
    /// 사람마다 기록(GiveRules.Log)에 키를 둔다: care(마지막으로 챙긴 주), cooled(떨어진 주, 화해 보너스용),
    /// no_lesson(선생님 수업이 없던 연속 주), empty_runs(빈손 탐험 연속, 선배).
    /// </summary>
    public static class BondDecayRules
    {
        public const string CareKey = "care";
        public const string CooledKey = "cooled";
        public const string NoLessonKey = "no_lesson";
        public const string EmptyRunsKey = "empty_runs";
        public const string NurseId = "NPC_NURSE";
        public const string SeniorId = "NPC_SURVIVOR_A";

        /// <summary>주간 결산 (WeekRunner.FinishWeek): 이번 주 계획과 상태를 보고 호감을 깎는다.</summary>
        public static void ApplyWeekly(GameState state, IDefinitionDatabase db, StateDelta delta)
        {
            var balance = db.Balance;
            var week = state.Calendar.Week;
            foreach (var pair in state.Npcs.Where(n => n.Value.Status == NpcStatus.Joined).ToList())
            {
                var id = pair.Key;
                var npc = pair.Value;
                var log = GiveRules.Log(state, id);

                // 오래 돌보지 않음: 처음 보는 동료는 이번 주부터 센다. 바닥 아래로는 내리지 않는다
                if (!log.ContainsKey(CareKey))
                {
                    log[CareKey] = week;
                }
                else if (week - log[CareKey] >= balance.NeglectWeeks && npc.Affinity > balance.NeglectFloor)
                {
                    Drop(state, id, -System.Math.Min(balance.NeglectAffinity, npc.Affinity - balance.NeglectFloor), balance, delta);
                }

                // 다친 동료에게 경계를 계속 맡김
                if (npc.InjuredWeeks > 0 && npc.Role == balance.GuardRoleId)
                {
                    Drop(state, id, -balance.InjuredGuardAffinity, balance, delta);
                }
            }

            // 박 선생님: 의약품이 있는데 다친 사람을 그 주에 치료하지 않고 넘김
            if (IsJoined(state, NurseId) && state.Base.GetResource(ResourceType.Medicine) > 0)
            {
                var untreated = state.Npcs.Any(n => n.Value.Status == NpcStatus.Joined && n.Value.InjuredWeeks > 0
                                                     && !(GiveRules.Log(state, n.Key).TryGetValue("kind:" + GiveKind.Medicine, out var w) && w == week));
                if (untreated)
                {
                    Drop(state, NurseId, -balance.NurseUntreatedAffinity, balance, delta);
                }
            }

            // 윤 선생님: 선생님이 가르치는 수업이 연속으로 없던 주
            if (GiveRules.Recipients(state, balance).Contains(GiveRules.TeacherId))
            {
                var taught = state.Plan.Slots.Any(s => !string.IsNullOrEmpty(s.ActionId) && db.TryGetAction(s.ActionId, out var a) && a.Teaching != TeachingMode.None);
                var log = GiveRules.Log(state, GiveRules.TeacherId);
                log.TryGetValue(NoLessonKey, out var streak);
                streak = taught ? 0 : streak + 1;
                log[NoLessonKey] = streak;
                if (streak >= balance.NoLessonWeeks)
                {
                    Drop(state, GiveRules.TeacherId, -balance.NoLessonAffinity, balance, delta);
                }
            }
        }

        /// <summary>탐험에서 돌아왔을 때 (선배: 빈손 귀환이 연속이면 실망).</summary>
        public static void OnExpedition(GameState state, ExpeditionResult result, BalanceConfig balance, StateDelta delta)
        {
            if (!IsJoined(state, SeniorId) || result == null)
            {
                return;
            }

            var log = GiveRules.Log(state, SeniorId);
            var empty = result.Status != ExtractStatus.Extracted || (result.Loot.Count == 0 && result.ResourceLoot.All(r => r.Amount <= 0));
            log.TryGetValue(EmptyRunsKey, out var runs);
            runs = empty ? runs + 1 : 0;
            if (runs >= balance.SeniorEmptyRuns)
            {
                Drop(state, SeniorId, -balance.SeniorEmptyAffinity, balance, delta);
                runs = 0;
            }

            log[EmptyRunsKey] = runs;
        }

        /// <summary>떨어진 뒤 처음 챙기면 호감을 배로 (화해 보너스). 쓰면 지운다.</summary>
        public static int ReconcileBonus(GameState state, string id, int affinity)
        {
            var log = GiveRules.Log(state, id);
            if (affinity <= 0 || !log.Remove(CooledKey))
            {
                return affinity;
            }

            return affinity * 2;
        }

        private static void Drop(GameState state, string id, int amount, BalanceConfig balance, StateDelta delta)
        {
            if (amount >= 0)
            {
                return;
            }

            EffectApplier.ChangeNpc(state, new NpcChange { NpcId = id, Affinity = amount }, balance, delta);
            GiveRules.Log(state, id)[CooledKey] = state.Calendar.Week;
        }

        private static bool IsJoined(GameState state, string id) => state.Npcs.TryGetValue(id, out var npc) && npc.Status == NpcStatus.Joined;
    }
}
