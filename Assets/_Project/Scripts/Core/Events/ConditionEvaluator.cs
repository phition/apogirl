using System;
using System.Collections.Generic;
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
        /// <summary>
        /// 엔딩 판정 중에 앞 막에서 뽑힌 조각 ID (EndingResolver가 판정하는 동안만 채운다, 스레드마다 따로 — 봇은 뒤 스레드에서 돈다).
        /// ValueKind.EndingAct가 읽는다. 판정 밖에서는 null.
        /// </summary>
        [ThreadStatic] public static HashSet<string> EndingActs;

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
                    foreach (var pair in state.Npcs)
                    {
                        if (pair.Value.Status == NpcStatus.Joined && !CatRules.IsCat(pair.Key)) joined++;   // 고양이는 사람 수에 들지 않는다
                    }

                    return joined;
                case ValueKind.CloseCompanions:
                    var min = int.TryParse(key, out var parsed) ? parsed : 0;
                    var close = 0;
                    foreach (var pair in state.Npcs)
                    {
                        if (pair.Value.Status == NpcStatus.Joined && pair.Value.Affinity >= min && !CatRules.IsCat(pair.Key)) close++;
                    }

                    return close;
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
                case ValueKind.FactionStanding: return state.Factions.TryGetValue(key ?? "", out var faction) ? faction.Standing : 0;
                case ValueKind.FactionKnown: return state.Factions.TryGetValue(key ?? "", out var known) && known.Known ? 1 : 0;
                case ValueKind.FactionTier:
                    return (int)Rules.FactionRules.TierOf(state.Factions.TryGetValue(key ?? "", out var tiered) ? tiered.Standing : 0);
                case ValueKind.BoardingCount: return Rules.BoardingRules.CandidateCount(state);
                case ValueKind.Boarded: return Rules.BoardingRules.Boarding(state).Contains(key ?? "") ? 1 : 0;
                case ValueKind.LeftBehind: return Rules.BoardingRules.Stayers(state).Count;
                case ValueKind.EndingAct: return EndingActs != null && EndingActs.Contains(key ?? "") ? 1 : 0;
                case ValueKind.ActionCountPrefix:
                    var prefixed = 0;
                    if (!string.IsNullOrEmpty(key))
                    {
                        var prefixes = key.Split('|');
                        foreach (var pair in state.Exploration.ActionCounts)
                        {
                            // 행동 하나는 한 번만 센다 (머리글 여러 개에 걸려도)
                            foreach (var prefix in prefixes)
                            {
                                if (prefix.Length > 0 && pair.Key.StartsWith(prefix, StringComparison.Ordinal))
                                {
                                    prefixed += pair.Value;
                                    break;
                                }
                            }
                        }
                    }

                    return prefixed;
                case ValueKind.Record: return RecordOf(state, key);
                case ValueKind.FlagCount:
                    var lit = 0;
                    foreach (var flag in (key ?? "").Split('|'))
                    {
                        if (flag.Length > 0 && state.Flags.Contains(flag)) lit++;
                    }

                    return lit;
                case ValueKind.FacilityCount:
                    var opened = 0;
                    foreach (var facility in state.Base.Facilities.Values)
                    {
                        if (facility >= 1) opened++;
                    }

                    return opened;
                case ValueKind.VisitedLocations: return VisitedLocations(state);
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
                case ValueKind.BoardingCount:
                case ValueKind.LeftBehind:
                case ValueKind.FacilityCount:
                case ValueKind.VisitedLocations:
                    return true;
                case ValueKind.Record:
                    return Array.IndexOf(RecordKeys, key) >= 0;
                case ValueKind.CloseCompanions:
                    return int.TryParse(key, out _);
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

        /// <summary>ValueKind.Record의 Key로 쓸 수 있는 기록 이름.</summary>
        public static readonly string[] RecordKeys =
        {
            "Downs", "Raids", "RaidsRepelled", "Intrusions", "IntrusionsLost", "HostileEvictions", "HostileRobberies", "HostileBeatings",
            "ZombiesFelled", "HeavyFelledRuns", "OwnedRoomRuns", "CdCount",
        };

        private static int RecordOf(GameState state, string key)
        {
            var stats = state.Exploration;
            switch (key)
            {
                case "Downs": return stats.Downs;
                case "Raids": return stats.Raids;
                case "RaidsRepelled": return stats.RaidsRepelled;
                case "Intrusions": return stats.Intrusions;
                case "IntrusionsLost": return stats.IntrusionsLost;
                case "HostileEvictions": return stats.HostileEvictions;
                case "HostileRobberies": return stats.HostileRobberies;
                case "HostileBeatings": return stats.HostileBeatings;
                case "ZombiesFelled": return stats.ZombiesFelled;
                case "HeavyFelledRuns": return stats.HeavyFelledRuns;
                case "OwnedRoomRuns": return stats.OwnedRoomRuns;
                case "CdCount": return state.CdWeeks.Count;
                default: throw new ArgumentException($"없는 기록: '{key}'");
            }
        }

        private const string DayRunPrefix = "ACT_EXP_DAY_";
        private const string NightRunPrefix = "ACT_EXP_NIGHT_";

        /// <summary>낮·밤 탐험 행동(ACT_EXP_DAY_x · ACT_EXP_NIGHT_x)을 한 번이라도 한 지역 수. 튜토리얼(ACT_EXP_TUTORIAL)은 머리글이 달라 빠진다.</summary>
        private static int VisitedLocations(GameState state)
        {
            var seen = new HashSet<string>();
            foreach (var pair in state.Exploration.ActionCounts)
            {
                if (pair.Value <= 0)
                {
                    continue;
                }

                if (pair.Key.StartsWith(DayRunPrefix, StringComparison.Ordinal)) seen.Add(pair.Key.Substring(DayRunPrefix.Length));
                else if (pair.Key.StartsWith(NightRunPrefix, StringComparison.Ordinal)) seen.Add(pair.Key.Substring(NightRunPrefix.Length));
            }

            return seen.Count;
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
