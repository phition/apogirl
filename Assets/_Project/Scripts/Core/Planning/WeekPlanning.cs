using System.Collections.Generic;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Planning
{
    public enum PlanWarningType
    {
        /// <summary>정의에 없는 행동 ID.</summary>
        UnknownAction,

        /// <summary>낮 전용 행동을 밤에 두는 등 슬롯 태그 불일치.</summary>
        SlotTagMismatch,

        /// <summary>강제 휴식 슬롯에 휴식이 아닌 행동 (경고 후 허용, D7).</summary>
        ForcedRestViolation,

        /// <summary>탐험 직후 탐험 (가장 무거운 위반).</summary>
        ConsecutiveExpedition,

        /// <summary>한 주에 탐험 3회 이상 또는 낮·밤 탐험 모두 배치.</summary>
        HighIntensityWeek,

        /// <summary>수면 부채 2단계 이상에서 밤 탐험 배치.</summary>
        NightExpeditionWithSleepDebt,
    }

    public sealed class PlanWarning
    {
        public PlanWarning(PlanWarningType type, int slotIndex)
        {
            Type = type;
            SlotIndex = slotIndex;
        }

        public PlanWarningType Type { get; }

        /// <summary>해당 슬롯 (주 전체 경고는 -1).</summary>
        public int SlotIndex { get; }
    }

    /// <summary>주간 계획 준비: 새 주 생성, 강제 휴식 표시·자동 배치 (문서 4.2).</summary>
    public static class PlanPreparer
    {
        public static string RestIdFor(int slotIndex, BalanceConfig balance)
        {
            return CalendarRules.PhaseOf(slotIndex) == DayPhase.Day ? balance.DayRestActionId : balance.NightRestActionId;
        }

        /// <summary>새 주 계획: 이월된 잠금 휴식 + 수면 부채 강제 휴식 단계면 낮 슬롯 1칸 휴식 잠금.</summary>
        public static WeekPlan CreateWeekPlan(GameState state, BalanceConfig balance)
        {
            var plan = new WeekPlan();
            var carry = state.CarryOver.LockedRestSlots;
            for (var i = 0; i < carry && i < WeekPlan.SlotCount; i++)
            {
                LockRest(plan, i, balance);
            }

            state.CarryOver.LockedRestSlots = 0;

            if (state.Player.SleepDebt >= balance.SleepDebtForcedRestStage)
            {
                for (var i = 0; i < WeekPlan.SlotCount; i += 2)
                {
                    if (!plan.Slots[i].Locked)
                    {
                        LockRest(plan, i, balance);
                        break;
                    }
                }
            }

            return plan;
        }

        /// <summary>
        /// 탐험 직후 슬롯을 강제 휴식으로 표시하고, 비어 있으면 휴식을 자동 배치한다.
        /// 플레이어가 이미 둔 행동은 덮어쓰지 않는다 (경고 후 허용).
        /// </summary>
        public static void ApplyForcedRest(GameState state, IDefinitionDatabase db)
        {
            var slots = state.Plan.Slots;
            foreach (var slot in slots)
            {
                if (!slot.Locked)
                {
                    slot.ForcedRest = false;
                }
            }

            for (var i = 0; i < slots.Count - 1; i++)
            {
                if (!IsExpedition(db, slots[i].ActionId))
                {
                    continue;
                }

                var next = slots[i + 1];
                next.ForcedRest = true;
                if (string.IsNullOrEmpty(next.ActionId))
                {
                    next.ActionId = RestIdFor(i + 1, db.Balance);
                }
            }
        }

        /// <summary>지정 슬롯을 휴식으로 잠근다.</summary>
        public static void LockRest(WeekPlan plan, int slotIndex, BalanceConfig balance)
        {
            var slot = plan.Slots[slotIndex];
            slot.ActionId = RestIdFor(slotIndex, balance);
            slot.ForcedRest = true;
            slot.Locked = true;
        }

        internal static bool IsExpedition(IDefinitionDatabase db, string actionId)
        {
            return !string.IsNullOrEmpty(actionId) && db.TryGetAction(actionId, out var action) && action.IsExpedition;
        }
    }

    /// <summary>계획 검증. 계획을 막지 않고 경고 목록만 돌려준다 (D7).</summary>
    public static class PlanValidator
    {
        public static List<PlanWarning> Validate(GameState state, IDefinitionDatabase db)
        {
            var warnings = new List<PlanWarning>();
            var slots = state.Plan.Slots;
            var expeditionCount = 0;
            var hasDay = false;
            var hasNight = false;

            for (var i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                if (string.IsNullOrEmpty(slot.ActionId))
                {
                    continue;
                }

                if (!db.TryGetAction(slot.ActionId, out var action))
                {
                    warnings.Add(new PlanWarning(PlanWarningType.UnknownAction, i));
                    continue;
                }

                var phase = CalendarRules.PhaseOf(i);
                if ((action.SlotTag == SlotTag.DayOnly && phase == DayPhase.Night) ||
                    (action.SlotTag == SlotTag.NightOnly && phase == DayPhase.Day))
                {
                    warnings.Add(new PlanWarning(PlanWarningType.SlotTagMismatch, i));
                }

                if (slot.ForcedRest && !action.IsRest)
                {
                    warnings.Add(new PlanWarning(PlanWarningType.ForcedRestViolation, i));
                }

                if (!action.IsExpedition)
                {
                    continue;
                }

                expeditionCount++;
                if (phase == DayPhase.Day) hasDay = true;
                else hasNight = true;

                if (i > 0 && PlanPreparer.IsExpedition(db, slots[i - 1].ActionId))
                {
                    warnings.Add(new PlanWarning(PlanWarningType.ConsecutiveExpedition, i));
                }

                if (phase == DayPhase.Night && state.Player.SleepDebt >= 2)
                {
                    warnings.Add(new PlanWarning(PlanWarningType.NightExpeditionWithSleepDebt, i));
                }
            }

            if (expeditionCount >= 3 || (hasDay && hasNight))
            {
                warnings.Add(new PlanWarning(PlanWarningType.HighIntensityWeek, -1));
            }

            return warnings;
        }
    }
}
