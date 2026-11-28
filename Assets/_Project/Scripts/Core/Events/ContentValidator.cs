using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;

namespace ApoGirl.Core.Events
{
    public enum IssueSeverity
    {
        Warning,
        Error,
    }

    public sealed class ContentIssue
    {
        public IssueSeverity Severity;
        public string Id = "";
        public string Message = "";

        public override string ToString() => $"[{Severity}] {Id}: {Message}";
    }

    /// <summary>검증에 필요한 바깥 정보 (Yarn 대본, UI 문자열 키). 없으면 해당 검사를 건너뛴다.</summary>
    public sealed class ContentValidationInput
    {
        /// <summary>Yarn 노드 이름 → 그 노드의 #choice: 태그들.</summary>
        public Dictionary<string, HashSet<string>> YarnNodes;

        /// <summary>UI 테이블 키.</summary>
        public HashSet<string> UiKeys;
    }

    /// <summary>
    /// 이벤트·NPC 정의 검증 (메뉴 ApoGirl/Content/Validate, 테스트에서도 사용).
    /// 오류: 실행 중 예외나 진행 불가로 이어지는 것. 경고: 동작은 하지만 의도와 다를 수 있는 것.
    /// </summary>
    public static class ContentValidator
    {
        public static List<ContentIssue> Validate(IDefinitionDatabase db, ContentValidationInput input = null)
        {
            var issues = new List<ContentIssue>();
            var npcIds = new HashSet<string> { EffectApplier.TeacherNpcId };
            foreach (var npc in db.Npcs)
            {
                npcIds.Add(npc.Id);
            }

            foreach (var ev in db.Events)
            {
                CheckCondition(db, ev.Id, ev.Condition, npcIds, issues);
                CheckEffect(db, ev.Id + " OnShow", ev.OnShow, npcIds, issues);
                if (!string.IsNullOrEmpty(ev.ThenEventId) && !db.TryGetEvent(ev.ThenEventId, out _))
                {
                    Add(issues, IssueSeverity.Error, ev.Id, "이어지는 이벤트 없음: " + ev.ThenEventId);
                }

                if (ev.IsRandom && ev.Weight <= 0f)
                {
                    Add(issues, IssueSeverity.Warning, ev.Id, "랜덤 이벤트 가중치가 0 이하");
                }

                if (!ev.Once && ev.CooldownWeeks <= 0 && !ev.IsRandom)
                {
                    Add(issues, IssueSeverity.Warning, ev.Id, "반복 스토리 이벤트에 쿨다운이 없음 (조건이 맞는 동안 매 슬롯 발생)");
                }

                var choiceIds = new HashSet<string>();
                var hasFreeChoice = ev.Choices.Count == 0;
                foreach (var choice in ev.Choices)
                {
                    var where = ev.Id + ":" + choice.Id;
                    if (string.IsNullOrEmpty(choice.Id))
                    {
                        Add(issues, IssueSeverity.Error, ev.Id, "선택지 ID가 비어 있음");
                    }
                    else if (!choiceIds.Add(choice.Id))
                    {
                        Add(issues, IssueSeverity.Error, where, "선택지 ID 중복");
                    }

                    CheckCondition(db, where, choice.Condition, npcIds, issues);
                    CheckEffect(db, where, choice.Effect, npcIds, issues);
                    CheckEffect(db, where + " OnFailure", choice.OnFailure, npcIds, issues);
                    if (!string.IsNullOrEmpty(choice.NextEventId) && !db.TryGetEvent(choice.NextEventId, out _))
                    {
                        Add(issues, IssueSeverity.Error, where, "연쇄 이벤트 없음: " + choice.NextEventId);
                    }

                    if (choice.Costs.Count == 0 && IsEmpty(choice.Condition))
                    {
                        hasFreeChoice = true;
                    }
                }

                if (!hasFreeChoice)
                {
                    Add(issues, IssueSeverity.Warning, ev.Id, "조건·비용 없는 선택지가 없음 (모두 막히면 첫 선택지 없이 넘어감)");
                }

                CheckDialogue(ev, choiceIds, input, issues);
            }

            CheckEndings(db, input, npcIds, issues);
            return issues;
        }

        /// <summary>M5: 엔딩 ID·조건·점수 키·문자열, 엔딩 레이어 줄.</summary>
        private static void CheckEndings(IDefinitionDatabase db, ContentValidationInput input, HashSet<string> npcIds, List<ContentIssue> issues)
        {
            var ids = new HashSet<string>();
            var hasBadFallback = false;
            var hasRescueFallback = false;
            foreach (var ending in db.Extras.Endings)
            {
                if (string.IsNullOrEmpty(ending.Id) || !ids.Add(ending.Id))
                {
                    Add(issues, IssueSeverity.Error, ending.Id, "엔딩 ID가 비었거나 중복");
                    continue;
                }

                CheckCondition(db, ending.Id, ending.Condition, npcIds, issues);
                foreach (var alt in ending.AltConditions)
                {
                    CheckCondition(db, ending.Id, alt, npcIds, issues);
                }
                foreach (var term in ending.Score)
                {
                    if (!ConditionEvaluator.IsValidKey(term.Kind, term.Key))
                    {
                        Add(issues, IssueSeverity.Error, ending.Id, $"점수 항목 키 오류: {term.Kind} '{term.Key}'");
                    }
                }

                if (ending.CutKeys.Count == 0)
                {
                    Add(issues, IssueSeverity.Warning, ending.Id, "후일담 컷이 없음");
                }

                if (ending.Category != EndingCategory.Bad && ending.EndCauses.Count > 0)
                {
                    Add(issues, IssueSeverity.Warning, ending.Id, "EndCauses는 배드 엔딩에서만 쓰인다");
                }

                var unconditional = IsEmpty(ending.Condition);
                if (ending.Category == EndingCategory.Bad && ending.EndCauses.Count == 0 && unconditional) hasBadFallback = true;
                if (ending.Category != EndingCategory.Bad && unconditional) hasRescueFallback = true;

                if (input?.UiKeys != null)
                {
                    if (!input.UiKeys.Contains(ending.TitleKey))
                    {
                        Add(issues, IssueSeverity.Error, ending.Id, "제목 문자열 없음: " + ending.TitleKey);
                    }

                    foreach (var key in ending.CutKeys)
                    {
                        if (!input.UiKeys.Contains(key))
                        {
                            Add(issues, IssueSeverity.Error, ending.Id, "후일담 문자열 없음: " + key);
                        }
                    }
                }

                foreach (var variant in ending.Variants)
                {
                    var where = ending.Id + "/" + variant.Id;
                    if (string.IsNullOrEmpty(variant.Id)) Add(issues, IssueSeverity.Error, ending.Id, "엔딩 변형 ID 없음");
                    if (IsEmpty(variant.Condition)) Add(issues, IssueSeverity.Warning, where, "조건 없는 엔딩 변형 (항상 기본 컷을 가린다)");
                    CheckCondition(db, where, variant.Condition, npcIds, issues);
                    if (input?.UiKeys == null) continue;
                    foreach (var key in variant.CutKeys)
                    {
                        if (!input.UiKeys.Contains(key)) Add(issues, IssueSeverity.Error, where, "변형 컷 문자열 없음: " + key);
                    }
                }
            }

            if (db.Extras.Endings.Count > 0)
            {
                if (!hasBadFallback) Add(issues, IssueSeverity.Error, "ENDINGS", "원인·조건 없는 배드 엔딩(대체 엔딩)이 없음");
                if (!hasRescueFallback) Add(issues, IssueSeverity.Error, "ENDINGS", "조건 없는 구조 엔딩(기본 엔딩)이 없음");
            }

            foreach (var line in db.Extras.Lines)
            {
                CheckCondition(db, line.Id, line.Condition, npcIds, issues);
                if (input?.UiKeys != null && !string.IsNullOrEmpty(line.TextKey) && !input.UiKeys.Contains(line.TextKey))
                {
                    Add(issues, IssueSeverity.Error, line.Id, "대사 줄 문자열 없음: " + line.TextKey);
                }
            }
        }

        private static void CheckDialogue(EventDef ev, HashSet<string> choiceIds, ContentValidationInput input, List<ContentIssue> issues)
        {
            if (input?.YarnNodes == null)
            {
                return;
            }

            if (!input.YarnNodes.TryGetValue(ev.Node, out var tags))
            {
                Add(issues, IssueSeverity.Warning, ev.Id, $"Yarn 노드 '{ev.Node}' 없음 → 카드로 표시");
                if (input.UiKeys != null)
                {
                    if (!input.UiKeys.Contains("event." + ev.Id + ".title"))
                    {
                        Add(issues, IssueSeverity.Error, ev.Id, "Yarn 노드도, UI 키 event." + ev.Id + ".title도 없음");
                    }

                    foreach (var choice in ev.Choices)
                    {
                        if (string.IsNullOrEmpty(choice.TextKey) || !input.UiKeys.Contains(choice.TextKey))
                        {
                            Add(issues, IssueSeverity.Error, ev.Id + ":" + choice.Id, "카드 표시용 TextKey 없음");
                        }
                    }
                }

                return;
            }

            foreach (var id in choiceIds)
            {
                if (!tags.Contains(id))
                {
                    Add(issues, IssueSeverity.Error, ev.Id + ":" + id, $"Yarn 노드 '{ev.Node}'에 #choice:{id} 선택지가 없음");
                }
            }

            foreach (var tag in tags)
            {
                if (!choiceIds.Contains(tag))
                {
                    Add(issues, IssueSeverity.Error, ev.Id, $"Yarn의 #choice:{tag}에 해당하는 데이터 선택지가 없음");
                }
            }
        }

        private static void CheckCondition(IDefinitionDatabase db, string where, ConditionDef condition, HashSet<string> npcIds, List<ContentIssue> issues)
        {
            if (condition == null)
            {
                return;
            }

            if (condition.WeekMin > 0 && condition.WeekMax > 0 && condition.WeekMin > condition.WeekMax)
            {
                Add(issues, IssueSeverity.Error, where, "WeekMin > WeekMax");
            }

            foreach (var day in condition.Days)
            {
                if (day < 1 || day > 7) Add(issues, IssueSeverity.Error, where, "요일은 1~7: " + day);
            }

            if (condition.Month < 0 || condition.Month > 12) Add(issues, IssueSeverity.Error, where, "월은 1~12");

            foreach (var id in condition.ActionIds)
            {
                if (!db.TryGetAction(id, out _)) Add(issues, IssueSeverity.Error, where, "행동 없음: " + id);
            }

            foreach (var check in condition.Checks) CheckValue(db, where, check, npcIds, issues);
            foreach (var check in condition.AnyChecks) CheckValue(db, where, check, npcIds, issues);
            foreach (var npc in condition.NpcStatuses)
            {
                if (!npcIds.Contains(npc.NpcId)) Add(issues, IssueSeverity.Error, where, "NPC 없음: " + npc.NpcId);
            }
        }

        private static void CheckValue(IDefinitionDatabase db, string where, ValueCheck check, HashSet<string> npcIds, List<ContentIssue> issues)
        {
            if (!ConditionEvaluator.IsValidKey(check.Kind, check.Key))
            {
                Add(issues, IssueSeverity.Error, where, $"{check.Kind}에 맞지 않는 키: '{check.Key}'");
                return;
            }

            if ((check.Kind == ValueKind.NpcAffinity || check.Kind == ValueKind.NpcTrust) && !npcIds.Contains(check.Key))
            {
                Add(issues, IssueSeverity.Error, where, "NPC 없음: " + check.Key);
            }

            if (check.Kind == ValueKind.ItemCount && !db.TryGetItem(check.Key, out _))
            {
                Add(issues, IssueSeverity.Error, where, "아이템 없음: " + check.Key);
            }
        }

        private static void CheckEffect(IDefinitionDatabase db, string where, GameEffect effect, HashSet<string> npcIds, List<ContentIssue> issues)
        {
            if (effect == null)
            {
                return;
            }

            foreach (var npc in effect.Npcs)
            {
                if (!npcIds.Contains(npc.NpcId)) Add(issues, IssueSeverity.Error, where, "NPC 없음: " + npc.NpcId);
            }

            foreach (var item in effect.Items)
            {
                if (!db.TryGetItem(item.ItemId, out _)) Add(issues, IssueSeverity.Error, where, "아이템 없음: " + item.ItemId);
            }
        }

        private static bool IsEmpty(ConditionDef c)
        {
            return c == null || (c.WeekMin == 0 && c.WeekMax == 0 && c.Days.Count == 0 && c.Phase == PhaseFilter.Any && c.Seasons.Count == 0
                                 && c.Month == 0 && c.ActionCategories.Count == 0 && c.ActionIds.Count == 0 && c.FlagsAll.Count == 0
                                 && c.FlagsAny.Count == 0 && c.FlagsNone.Count == 0 && c.CluesAll.Count == 0 && c.Checks.Count == 0
                                 && c.AnyChecks.Count == 0 && c.NpcStatuses.Count == 0 && !c.IsBirthday);
        }

        private static void Add(List<ContentIssue> issues, IssueSeverity severity, string id, string message)
        {
            issues.Add(new ContentIssue { Severity = severity, Id = id, Message = message });
        }
    }

    /// <summary>Yarn 대본 텍스트에서 노드 이름과 #choice: 태그를 뽑는다 (컴파일 없이, 검증용).</summary>
    public static class YarnScriptScanner
    {
        private static readonly Regex ChoiceTag = new Regex(@"#choice:([A-Za-z0-9_]+)", RegexOptions.Compiled);

        public static Dictionary<string, HashSet<string>> Scan(string source, Dictionary<string, HashSet<string>> into = null)
        {
            var nodes = into ?? new Dictionary<string, HashSet<string>>();
            string title = null;
            var inBody = false;
            foreach (var rawLine in (source ?? "").Split('\n'))
            {
                var line = rawLine.Trim();
                if (!inBody)
                {
                    if (line.StartsWith("title:", StringComparison.Ordinal))
                    {
                        title = line.Substring("title:".Length).Trim();
                    }
                    else if (line == "---" && title != null)
                    {
                        inBody = true;
                        if (!nodes.ContainsKey(title))
                        {
                            nodes[title] = new HashSet<string>();
                        }
                    }

                    continue;
                }

                if (line == "===")
                {
                    inBody = false;
                    title = null;
                    continue;
                }

                if (line.StartsWith("->", StringComparison.Ordinal))
                {
                    foreach (Match match in ChoiceTag.Matches(line))
                    {
                        nodes[title].Add(match.Groups[1].Value);
                    }
                }
            }

            return nodes;
        }
    }
}
