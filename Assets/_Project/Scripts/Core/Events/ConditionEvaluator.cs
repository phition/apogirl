using System;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Events
{
    /// <summary>이벤트 판정 시점의 맥락: 방금 끝난 슬롯 (주차·요일·낮밤은 GameState.Calendar를 쓴다).</summary>
    public sealed class EventContext
    {
        public DayPhase Phase;
        public int Day;
        public string ActionId = "";
        public ActionCategory ActionCategory;

        /// <summary>계획 화면 등 슬롯과 무관한 판정 (행동 조건은 통과).</summary>
        public static EventContext FromCalendar(GameState state)
        {
            return new EventContext { Phase = state.Calendar.Phase, Day = state.Calendar.Day, ActionId = "" };
        }
    }

    /// <summary><see cref="ConditionDef"/> 판정. 순수 함수 (상태를 바꾸지 않는다).</summary>
    public static class ConditionEvaluator
    {
        public static bool IsMet(GameState state, ConditionDef condition, EventContext context, BalanceConfig balance)
        {
            if (condition == null)
            {
                return true;
            }

            var week = state.Calendar.Week;
            if (condition.WeekMin > 0 && week < condition.WeekMin) return false;
            if (condition.WeekMax > 0 && week > condition.WeekMax) return false;
            if (condition.Days.Count > 0 && !condition.Days.Contains(context.Day)) return false;
            if (condition.Phase == PhaseFilter.Day && context.Phase != DayPhase.Day) return false;
            if (condition.Phase == PhaseFilter.Night && context.Phase != DayPhase.Night) return false;
            if (condition.Seasons.Count > 0 && !condition.Seasons.Contains(CalendarRules.GetSeason(week))) return false;

            if (condition.Month > 0)
            {
                var date = CalendarRules.DateOf(week, context.Day, balance);
                if (date.Month != condition.Month) return false;
                if (condition.DayOfMonth > 0 && date.Day != condition.DayOfMonth) return false;
            }

            if (!string.IsNullOrEmpty(context.ActionId))
            {
                if (condition.ActionCategories.Count > 0 && !condition.ActionCategories.Contains(context.ActionCategory)) return false;
                if (condition.ActionIds.Count > 0 && !condition.ActionIds.Contains(context.ActionId)) return false;
            }
            else if (condition.ActionCategories.Count > 0 || condition.ActionIds.Count > 0)
            {
                return false;
            }

            foreach (var flag in condition.FlagsAll)
            {
                if (!state.Flags.Contains(flag)) return false;
            }

            foreach (var flag in condition.FlagsNone)
            {
                if (state.Flags.Contains(flag)) return false;
            }

            if (condition.FlagsAny.Count > 0)
            {
                var any = false;
                foreach (var flag in condition.FlagsAny)
                {
                    any |= state.Flags.Contains(flag);
                }

                if (!any) return false;
            }

            foreach (var clue in condition.CluesAll)
            {
                if (!state.Clues.Contains(clue)) return false;
            }

            foreach (var check in condition.Checks)
            {
                if (!Check(state, check)) return false;
            }

            if (condition.AnyChecks.Count > 0)
            {
                var any = false;
                foreach (var check in condition.AnyChecks)
                {
                    any |= Check(state, check);
                }

                if (!any) return false;
            }

            foreach (var npc in condition.NpcStatuses)
            {
                if (NpcStatusOf(state, npc.NpcId) != npc.Status) return false;
            }

            if (condition.IsBirthday)
            {
                var profile = state.Player.Profile;
                var date = CalendarRules.DateOf(week, context.Day, balance);
                if (profile.BirthMonth <= 0 || date.Month != profile.BirthMonth || date.Day != profile.BirthDay) return false;
            }

            return true;
        }

        public static bool Check(GameState state, ValueCheck check)
        {
            return Compare(ValueOf(state, check.Kind, check.Key), check.Op, check.Value);
        }

        public static NpcStatus NpcStatusOf(GameState state, string npcId)
        {
            if (npcId == EffectApplier.TeacherNpcId)
            {
                return state.Teacher.Alive ? NpcStatus.Joined : NpcStatus.Lost;
            }

            return state.Npcs.TryGetValue(npcId ?? "", out var npc) ? npc.Status : NpcStatus.Unknown;
        }

        /// <summary>Key가 enum 이름일 때 해석 실패하면 예외 (콘텐츠 검증 도구가 미리 잡는다).</summary>
        public static int ValueOf(GameState state, ValueKind kind, string key)
        {
            switch (kind)
            {
                case ValueKind.Stat: return state.Player.GetStat(Parse<StatType>(key));
                case ValueKind.Proficiency: return state.Player.ProficiencyLevel(Parse<ProficiencyType>(key));
                case ValueKind.Vital: return state.Player.Vitals.GetVital(Parse<VitalType>(key));
                case ValueKind.Resource: return state.Base.GetResource(Parse<ResourceType>(key));
                case ValueKind.Hidden: return state.Player.Hidden.GetHidden(Parse<HiddenType>(key));
                case ValueKind.SleepDebt: return state.Player.SleepDebt;
                case ValueKind.Conscience: return state.Player.Conscience;
                case ValueKind.TeacherAffinity: return state.Teacher.Affinity;
                case ValueKind.NpcAffinity:
                    if (key == EffectApplier.TeacherNpcId) return state.Teacher.Affinity;
                    return state.Npcs.TryGetValue(key ?? "", out var a) ? a.Affinity : 0;
                case ValueKind.NpcTrust: return state.Npcs.TryGetValue(key ?? "", out var t) ? t.Trust : 0;
                case ValueKind.EndingTag: return state.EndingTags.TryGetValue(key ?? "", out var tag) ? tag : 0;
                case ValueKind.ItemCount:
                    var count = 0;
                    foreach (var stack in state.Storage)
                    {
                        if (stack.ItemId == key) count += stack.Count;
                    }

                    return count;
                case ValueKind.Facility: return state.Base.Facilities.TryGetValue(key ?? "", out var level) ? level : 0;
                case ValueKind.Defense: return state.Base.Defense;
                case ValueKind.Threat: return state.Base.Threat;
                case ValueKind.TeacherMobility: return state.Teacher.MobilityStage;
                case ValueKind.Trait: return state.Player.Traits.Contains(key ?? "") ? 1 : 0;
                case ValueKind.Expeditions:
                    var exploration = state.Exploration;
                    if (key == "Day") return exploration.DayExplorations;
                    if (key == "Night") return exploration.NightExplorations;
                    return exploration.DayExplorations + exploration.NightExplorations;
                case ValueKind.ActionCount: return ActionCountOf(state, key);
                case ValueKind.NightRatio:
                    var total = state.Exploration.DayExplorations + state.Exploration.NightExplorations;
                    return total <= 0 ? 0 : state.Exploration.NightExplorations * 100 / total;
                case ValueKind.DiaryCount: return state.Diary.Count;
                case ValueKind.Companions:
                    var joined = 0;
                    foreach (var npc in state.Npcs.Values)
                    {
                        if (npc.Status == NpcStatus.Joined) joined++;
                    }

                    return joined;
                case ValueKind.StatSpread: return StatExtreme(state, true) - StatExtreme(state, false);
                case ValueKind.StatMin: return StatExtreme(state, false);
                case ValueKind.BreakdownWeeks: return state.Player.BreakdownWeeks;
                case ValueKind.ClueCount:
                    if (string.IsNullOrEmpty(key)) return state.Clues.Count;
                    var clues = 0;
                    foreach (var clue in state.Clues)
                    {
                        if (clue.StartsWith(key, StringComparison.Ordinal)) clues++;
                    }

                    return clues;
                case ValueKind.TeacherInfection: return state.Teacher.Reinfected ? state.Teacher.InfectionProgress : 0;
                case ValueKind.TeacherStage: return InfectionRules.TeacherStage(state, null);
                case ValueKind.WeeksToRescue: return Math.Max(0, CalendarState.TotalWeeks - state.Calendar.Week);
                case ValueKind.Weather: return state.Base.Weather.ToString() == key ? 1 : 0;
                case ValueKind.RoleCount:
                    var roles = 0;
                    foreach (var npc in state.Npcs.Values)
                    {
                        if (npc.Status == NpcStatus.Joined && npc.Role == key) roles++;
                    }

                    return roles;
                case ValueKind.Flag: return state.Flags.Contains(key) ? 1 : 0;
                case ValueKind.Traps: return state.Base.Traps;
                case ValueKind.BloodCompatible:
                    return Enum.TryParse<BloodType>(key, out var recipient) && BloodRules.CanDonate(state.Player.Profile.BloodType, recipient) ? 1 : 0;
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }

        /// <summary>Key가 해당 종류에서 올바른지 (검증 도구용).</summary>
        public static bool IsValidKey(ValueKind kind, string key)
        {
            switch (kind)
            {
                case ValueKind.Stat: return Enum.TryParse<StatType>(key, out _);
                case ValueKind.Proficiency: return Enum.TryParse<ProficiencyType>(key, out _);
                case ValueKind.Vital: return Enum.TryParse<VitalType>(key, out _);
                case ValueKind.Resource: return Enum.TryParse<ResourceType>(key, out _);
                case ValueKind.Hidden: return Enum.TryParse<HiddenType>(key, out _);
                case ValueKind.SleepDebt:
                case ValueKind.Conscience:
                case ValueKind.TeacherAffinity:
                case ValueKind.Defense:
                case ValueKind.Threat:
                case ValueKind.TeacherMobility:
                case ValueKind.NightRatio:
                case ValueKind.DiaryCount:
                case ValueKind.Companions:
                case ValueKind.StatSpread:
                case ValueKind.StatMin:
                case ValueKind.BreakdownWeeks:
                case ValueKind.ClueCount:
                case ValueKind.TeacherInfection:
                case ValueKind.TeacherStage:
                case ValueKind.WeeksToRescue:
                case ValueKind.Traps:
                    return true;
                case ValueKind.Weather:
                    return Enum.TryParse<WeatherType>(key, out _);
                case ValueKind.Expeditions:
                    return string.IsNullOrEmpty(key) || key == "Day" || key == "Night";
                case ValueKind.BloodCompatible:
                    return Enum.TryParse<BloodType>(key, out var blood) && blood != BloodType.Unknown;
                default: return !string.IsNullOrEmpty(key);
            }
        }

        public static bool Compare(int value, CompareOp op, int target)
        {
            switch (op)
            {
                case CompareOp.GreaterOrEqual: return value >= target;
                case CompareOp.LessOrEqual: return value <= target;
                case CompareOp.Equal: return value == target;
                case CompareOp.NotEqual: return value != target;
                case CompareOp.Greater: return value > target;
                case CompareOp.Less: return value < target;
                default: throw new ArgumentOutOfRangeException(nameof(op), op, null);
            }
        }

        private static int StatExtreme(GameState state, bool max)
        {
            var result = max ? int.MinValue : int.MaxValue;
            foreach (StatType stat in Enum.GetValues(typeof(StatType)))
            {
                var value = state.Player.GetStat(stat);
                result = max ? Math.Max(result, value) : Math.Min(result, value);
            }

            return result;
        }

        private static int ActionCountOf(GameState state, string actionId)
        {
            return state.Exploration.ActionCounts.TryGetValue(actionId ?? "", out var n) ? n : 0;
        }

        private static T Parse<T>(string key) where T : struct
        {
            if (!Enum.TryParse<T>(key, out var value))
            {
                throw new ArgumentException($"{typeof(T).Name}에 없는 키: '{key}'");
            }

            return value;
        }
    }
}
