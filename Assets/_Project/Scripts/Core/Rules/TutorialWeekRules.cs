using System;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Planning;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>
    /// 새 게임 1주차 계획: 탈출 날 전까지는 창고에 갇혀 버티고, 탈출 날 낮에 창고 탈출(튜토리얼), 그날 밤은 휴식.
    /// 모두 잠근다 (탈출 전에 공부·준비하거나 탈출을 여러 번 하지 않도록).
    /// </summary>
    public static class TutorialWeekRules
    {
        /// <summary>튜토리얼 행동이 없으면 아무것도 하지 않고 false.</summary>
        public static bool Apply(GameState state, IDefinitionDatabase db)
        {
            var balance = db.Balance;
            if (string.IsNullOrEmpty(balance.TutorialActionId) || !db.TryGetAction(balance.TutorialActionId, out _))
            {
                return false;
            }

            var plan = state.Plan;
            var escape = CalendarRules.SlotIndex(Math.Min(Math.Max(balance.TutorialDay, 1), 7), DayPhase.Day);
            if (!string.IsNullOrEmpty(balance.StorageWaitActionId) && db.TryGetAction(balance.StorageWaitActionId, out _))
            {
                for (var i = 0; i < escape; i++)
                {
                    Lock(plan.Slots[i], balance.StorageWaitActionId);
                }
            }

            Lock(plan.Slots[escape], balance.TutorialActionId);
            if (escape + 1 < plan.Slots.Count)
            {
                PlanPreparer.LockRest(plan, escape + 1, balance);
            }

            PlanPreparer.ApplyForcedRest(state, db);
            return true;
        }

        private static void Lock(PlannedSlot slot, string actionId)
        {
            slot.ActionId = actionId;
            slot.ForcedRest = false;
            slot.Locked = true;
        }
    }
}
