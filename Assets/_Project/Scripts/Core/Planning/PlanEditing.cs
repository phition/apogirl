using System;
using System.Collections.Generic;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Planning
{
    public enum PlanEditResult
    {
        Ok,

        /// <summary>계획 단계가 아님 (주 실행 중이거나 게임 종료).</summary>
        NotPlanning,

        /// <summary>잠긴 슬롯 (이월 휴식, 쓰러짐 요양, 수면 부채 4단계).</summary>
        Locked,

        UnknownAction,

        /// <summary>아직 해금되지 않은 행동 (M4).</summary>
        NotUnlocked,

        /// <summary>낮 전용 행동을 밤에 두는 등 슬롯 태그 불일치.</summary>
        WrongSlot,

        /// <summary>한 주에 한 번만 둘 수 있는 행동 (동아리, 확장 7).</summary>
        WeeklyLimit,
    }

    /// <summary>주간 계획 편집 (문서 4.3 플래너 UX). 편집 후 강제 휴식 표시를 다시 계산한다.</summary>
    public static class PlanEditing
    {
        public const int MaxPresets = 5;

        /// <summary>한 주의 첫 슬롯을 실행하기 전에만 계획을 바꿀 수 있다.</summary>
        public static bool CanEdit(GameState state)
        {
            var calendar = state.Calendar;
            return state.Status == GameStatus.Playing && !calendar.WeekComplete
                                                      && calendar.Day == 1 && calendar.Phase == DayPhase.Day;
        }

        /// <summary>해금 조건 (ActionDef.Unlock). 행동 조건(ActionCategories)은 계획 시점에는 검사하지 않는다.</summary>
        public static bool IsUnlocked(GameState state, IDefinitionDatabase db, ActionDef action)
        {
            if (action.Unlock == null)
            {
                return true;
            }

            try
            {
                return Events.ConditionEvaluator.IsMet(state, action.Unlock, Events.EventContext.FromCalendar(state), db.Balance);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        public static bool IsAllowedIn(ActionDef action, int slotIndex)
        {
            var phase = CalendarRules.PhaseOf(slotIndex);
            return action.SlotTag == SlotTag.Both
                   || (action.SlotTag == SlotTag.DayOnly && phase == DayPhase.Day)
                   || (action.SlotTag == SlotTag.NightOnly && phase == DayPhase.Night);
        }

        public static PlanEditResult SetSlot(GameState state, IDefinitionDatabase db, int slotIndex, string actionId)
        {
            var check = CheckSlot(state, slotIndex);
            if (check != PlanEditResult.Ok)
            {
                return check;
            }

            if (!db.TryGetAction(actionId, out var action))
            {
                return PlanEditResult.UnknownAction;
            }

            if (!IsAllowedIn(action, slotIndex))
            {
                return PlanEditResult.WrongSlot;
            }

            if (!IsUnlocked(state, db, action))
            {
                return PlanEditResult.NotUnlocked;
            }

            if (action.IsClub && ClubRules.InPlan(state, db, slotIndex) > 0)
            {
                return PlanEditResult.WeeklyLimit;
            }

            state.Plan.Slots[slotIndex].ActionId = actionId;
            PlanPreparer.ApplyForcedRest(state, db);
            return PlanEditResult.Ok;
        }

        public static PlanEditResult ClearSlot(GameState state, IDefinitionDatabase db, int slotIndex)
        {
            var check = CheckSlot(state, slotIndex);
            if (check != PlanEditResult.Ok)
            {
                return check;
            }

            state.Plan.Slots[slotIndex].ActionId = "";
            PlanPreparer.ApplyForcedRest(state, db);
            return PlanEditResult.Ok;
        }

        /// <summary>지난주 계획을 잠기지 않은 슬롯에 복사한다. 지금 둘 수 없는 행동은 비워 둔다.</summary>
        public static bool CopyLastWeek(GameState state, IDefinitionDatabase db)
        {
            if (!CanEdit(state) || state.LastWeekPlan == null)
            {
                return false;
            }

            ApplyActionIds(state, db, ExtractIds(state.LastWeekPlan));
            return true;
        }

        /// <summary>현재 계획을 이름으로 저장 (같은 이름은 덮어씀, 최대 개수 초과 시 실패).</summary>
        public static bool SavePreset(GameState state, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            var existing = state.PlanPresets.Find(p => p.Name == name);
            if (existing == null)
            {
                if (state.PlanPresets.Count >= MaxPresets)
                {
                    return false;
                }

                existing = new PlanPreset { Name = name };
                state.PlanPresets.Add(existing);
            }

            existing.ActionIds = ExtractIds(state.Plan);
            return true;
        }

        public static bool ApplyPreset(GameState state, IDefinitionDatabase db, string name)
        {
            var preset = state.PlanPresets.Find(p => p.Name == name);
            if (preset == null || !CanEdit(state))
            {
                return false;
            }

            ApplyActionIds(state, db, preset.ActionIds);
            return true;
        }

        public static bool DeletePreset(GameState state, string name)
        {
            return state.PlanPresets.RemoveAll(p => p.Name == name) > 0;
        }

        /// <summary>행동 ID만 복사한 계획 (잠금·강제 휴식 표시는 버림).</summary>
        public static WeekPlan CloneActions(WeekPlan plan)
        {
            var clone = new WeekPlan();
            for (var i = 0; i < WeekPlan.SlotCount && i < plan.Slots.Count; i++)
            {
                clone.Slots[i].ActionId = plan.Slots[i].ActionId;
            }

            return clone;
        }

        private static List<string> ExtractIds(WeekPlan plan)
        {
            var ids = new List<string>(WeekPlan.SlotCount);
            foreach (var slot in plan.Slots)
            {
                ids.Add(slot.ActionId);
            }

            return ids;
        }

        private static void ApplyActionIds(GameState state, IDefinitionDatabase db, IList<string> ids)
        {
            var slots = state.Plan.Slots;
            for (var i = 0; i < slots.Count; i++)
            {
                if (slots[i].Locked)
                {
                    continue;
                }

                var id = i < ids.Count ? ids[i] : "";
                var valid = !string.IsNullOrEmpty(id) && db.TryGetAction(id, out var action) && IsAllowedIn(action, i) && IsUnlocked(state, db, action);
                slots[i].ActionId = valid ? id : "";
            }

            PlanPreparer.ApplyForcedRest(state, db);
        }

        private static PlanEditResult CheckSlot(GameState state, int slotIndex)
        {
            if (!CanEdit(state))
            {
                return PlanEditResult.NotPlanning;
            }

            if (slotIndex < 0 || slotIndex >= state.Plan.Slots.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex));
            }

            return state.Plan.Slots[slotIndex].Locked ? PlanEditResult.Locked : PlanEditResult.Ok;
        }
    }
}
