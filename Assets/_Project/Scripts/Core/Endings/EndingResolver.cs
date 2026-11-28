using System;
using System.Collections.Generic;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Events;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Endings
{
    /// <summary>엔딩 화면 한 장의 종류.</summary>
    public enum EndingPageKind
    {
        /// <summary>막 제목 카드 ("구조의 날" 등). Key = 제목 문구 키.</summary>
        ActTitle,

        /// <summary>글 한 컷. Key = 문구 키.</summary>
        Cut,

        /// <summary>명단: 그날 함께한 사람들의 "그 뒤" 줄을 한 장에. Lines = 문구 키들.</summary>
        Roster,
    }

    /// <summary>엔딩 화면 한 장 (세 막 엔딩의 넘기는 단위).</summary>
    public sealed class EndingPage
    {
        public EndingPageKind Kind;
        public EndingAct Act;
        public string Key = "";
        public readonly List<string> Lines = new List<string>();
    }

    /// <summary>한 막의 판정 결과: 고른 조각과 변형.</summary>
    public sealed class EndingActResult
    {
        public EndingAct Act;
        public EndingDef Piece;
        public EndingVariantDef Variant;
        public float Score;
    }

    /// <summary>엔딩 판정 결과: 고른 엔딩, 보여 줄 후일담 컷(공통 레이어 포함), 주요 장면.</summary>
    public sealed class EndingResult
    {
        /// <summary>
        /// 대표 엔딩: 배드면 그 엔딩, 구조면 막 Ⅲ(그 뒤의 나)의 엔딩 — 남기로 한 판(미래를 판정하지 않음)은 구조 장면.
        /// 제목·그림·봇 통계·갤러리의 기본 칸이 이것을 쓴다.
        /// </summary>
        public EndingDef Ending;

        /// <summary>구조 엔딩의 세 막: 구조의 날 → 돌아보는 밤 → 그 뒤의 나 (배드 엔딩은 비어 있다).</summary>
        public readonly List<EndingActResult> Acts = new List<EndingActResult>();

        /// <summary>화면에 넘겨 보여 줄 차례 (막 제목·컷·명단·다리 문장). 배드 엔딩은 컷만.</summary>
        public readonly List<EndingPage> Pages = new List<EndingPage>();

        /// <summary>보여 주는 모든 글 컷 (문구 키, 순서대로: 다리 문장·공통 줄 포함, 막 제목·명단 제외).</summary>
        public readonly List<string> CutKeys = new List<string>();

        /// <summary>"1년의 기록"의 주요 장면 (UI 문자열 키).</summary>
        public readonly List<string> MomentKeys = new List<string>();

        /// <summary>명단의 '그 뒤' 줄: 세력·동료 후일담 (묶음마다 한 줄). 화면에는 앞의 몇 줄만, 나머지는 MoreEpilogueKeys.</summary>
        public readonly List<string> EpilogueKeys = new List<string>();

        /// <summary>명단에 다 못 실은 '그 뒤' 줄 ("1년의 기록" 화면에 둔다).</summary>
        public readonly List<string> MoreEpilogueKeys = new List<string>();

        /// <summary>판정에 쓴 점수 (대표 엔딩, 디버그 표시용).</summary>
        public float Score;

        /// <summary>대표 엔딩의 변형 (없으면 null).</summary>
        public EndingVariantDef Variant;

        /// <summary>엔딩 그림 이름: 변형 그림이 있으면 그것, 아니면 엔딩 ID. 구조 장면의 변형 그림(좌석 선택)이 있으면 그것이 먼저.</summary>
        public string ArtKey
        {
            get
            {
                var rescue = Of(EndingAct.Rescue);
                if (rescue?.Variant != null && !string.IsNullOrEmpty(rescue.Variant.Image))
                {
                    return rescue.Variant.Image;
                }

                return Variant != null && !string.IsNullOrEmpty(Variant.Image) ? Variant.Image : Ending?.Id;
            }
        }

        public bool IsBad => Ending != null && Ending.Category == EndingCategory.Bad;

        /// <summary>그 막의 결과 (없으면 null).</summary>
        public EndingActResult Of(EndingAct act) => Acts.Find(a => a.Act == act);

        /// <summary>그 막에서 고른 조각의 ID (없으면 빈 글).</summary>
        public string PieceId(EndingAct act) => Of(act)?.Piece?.Id ?? "";
    }

    /// <summary>
    /// 엔딩 판정 (문서 8.1, 부록 B, Docs/PROPOSAL_ENDINGS_EXPANSION 7절). 순수 함수: 상태를 바꾸지 않는다.
    /// 게임오버 → 종료 원인이 맞는 배드 엔딩 한 편.
    /// 구조 → 세 막을 따로 고른다: Ⅰ 구조의 날(누가 타고 누가 남았나) → Ⅱ 돌아보는 밤(그 1년이 남긴 것) → Ⅲ 그 뒤의 나(어떤 사람이 되었나).
    /// 막마다 조건을 만족한 조각 중 우선순위, 그다음 점수. 뒤 막의 조건은 앞 막의 결과를 읽을 수 있다 (ValueKind.EndingAct).
    /// </summary>
    public static class EndingResolver
    {
        /// <summary>다리 문장 묶음 (LineKind.EndingBridge의 Group).</summary>
        public const string BridgeToReflection = "TO_REFLECTION";
        public const string BridgeToFuture = "TO_FUTURE";

        public static EndingResult Resolve(GameState state, IDefinitionDatabase db)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (db == null) throw new ArgumentNullException(nameof(db));

            var result = new EndingResult();
            var context = EventContext.FromCalendar(state);
            var previous = ConditionEvaluator.EndingActs;
            ConditionEvaluator.EndingActs = new HashSet<string>();
            try
            {
                if (state.Status == GameStatus.GameOver)
                {
                    ResolveBad(state, db, context, result);
                }
                else
                {
                    ResolveRescue(state, db, context, result);
                }
            }
            finally
            {
                ConditionEvaluator.EndingActs = previous;
            }

            foreach (var line in db.Extras.Lines)
            {
                if (line.Kind == LineKind.EndingMoment && !string.IsNullOrEmpty(line.TextKey) && IsMet(state, line.Condition, context, db.Balance))
                {
                    result.MomentKeys.Add(line.TextKey);
                }
            }

            return result;
        }

        // ── 배드 엔딩: 한 편 그대로 ──

        private static void ResolveBad(GameState state, IDefinitionDatabase db, EventContext context, EndingResult result)
        {
            EndingDef best = null;
            var bestScore = float.MinValue;
            foreach (var ending in db.Extras.Endings)
            {
                if (ending.Category != EndingCategory.Bad)
                {
                    continue;
                }

                if (ending.EndCauses.Count > 0 && !ending.EndCauses.Contains(state.EndCause))
                {
                    continue;
                }

                if (!IsMet(state, ending.Condition, context, db.Balance))
                {
                    continue;
                }

                // 원인이 정확히 맞는 배드 엔딩이 대체 엔딩(원인 없음)보다 먼저
                var score = Score(state, ending);
                var priority = ending.Priority + (ending.EndCauses.Count > 0 ? 10000 : 0);
                var bestPriority = best == null ? int.MinValue : best.Priority + (best.EndCauses.Count > 0 ? 10000 : 0);
                if (best == null || priority > bestPriority || (priority == bestPriority && score > bestScore))
                {
                    best = ending;
                    bestScore = score;
                }
            }

            result.Ending = best;
            result.Score = best == null ? 0f : bestScore;
            if (best == null)
            {
                return;
            }

            result.Variant = VariantOf(state, best, context, db.Balance);
            foreach (var key in result.Variant != null && result.Variant.CutKeys.Count > 0 ? result.Variant.CutKeys : best.CutKeys)
            {
                AddCut(result, EndingAct.Whole, key);
            }
        }

        // ── 구조 엔딩: 세 막 ──

        private static void ResolveRescue(GameState state, IDefinitionDatabase db, EventContext context, EndingResult result)
        {
            var balance = db.Balance;

            // 막마다 고른다. 고른 조각은 뒤 막의 조건이 읽을 수 있게 알려 둔다
            var rescue = Pick(state, db, context, EndingAct.Rescue);
            Remember(result, rescue);
            var reflection = Pick(state, db, context, EndingAct.Reflection);
            Remember(result, reflection);
            // 구조 장면이 미래까지 정해 두었으면(남기로 한 판) 미래는 판정하지 않는다
            var fixedFuture = rescue?.Piece != null && rescue.Piece.FutureKeys.Count > 0;
            var future = fixedFuture ? null : Pick(state, db, context, EndingAct.Future);
            Remember(result, future);

            var main = future ?? rescue ?? reflection;
            result.Ending = main?.Piece;
            result.Variant = main?.Variant;
            result.Score = main?.Score ?? 0f;

            // ── Ⅰ 구조의 날: 장면 → 선생님·친구의 그날 → 명단(그 뒤) ──
            AddTitle(result, EndingAct.Rescue);
            AddCuts(result, EndingAct.Rescue, CutsOf(rescue));
            AddLine(result, EndingAct.Rescue, LineRules.Pick(state, db, LineKind.EndingTeacher, context));
            AddLine(result, EndingAct.Rescue, LineRules.Pick(state, db, LineKind.EndingFriend, context));

            var reflectionGroups = new HashSet<string>(balance.EndingReflectionGroups ?? Array.Empty<string>());
            var roster = new List<LineDef>();
            var inner = new List<LineDef>();
            foreach (var line in EpilogueLines(state, db, context))
            {
                (reflectionGroups.Contains(line.Group ?? "") ? inner : roster).Add(line);
            }

            // 명단은 우선순위가 높은 줄부터 몇 줄만 (정의 순서를 지킨다). 나머지는 1년의 기록에
            var shown = new HashSet<LineDef>(TopByPriority(roster, Math.Max(0, balance.EndingRosterMax)));
            var page = new EndingPage { Kind = EndingPageKind.Roster, Act = EndingAct.Rescue };
            foreach (var line in roster)
            {
                if (shown.Contains(line))
                {
                    result.EpilogueKeys.Add(line.TextKey);
                    page.Lines.Add(line.TextKey);
                }
                else
                {
                    result.MoreEpilogueKeys.Add(line.TextKey);
                }
            }

            if (page.Lines.Count > 0)
            {
                result.Pages.Add(page);
            }

            // ── Ⅱ 돌아보는 밤: 회고 축 → 마음 상태·희망 → 탐험 성향 ──
            AddLine(result, EndingAct.Rescue, Bridge(state, db, context, BridgeToReflection));
            AddTitle(result, EndingAct.Reflection);
            AddCuts(result, EndingAct.Reflection, CutsOf(reflection));
            foreach (var line in inner)
            {
                AddCut(result, EndingAct.Reflection, line.TextKey);
            }

            AddLine(result, EndingAct.Reflection, LineRules.Pick(state, db, LineKind.EndingTendency, context));

            // ── Ⅲ 그 뒤의 나: 미래 본편 → 꼬리(관계 · 세상 · 회고) ──
            AddLine(result, EndingAct.Reflection, Bridge(state, db, context, BridgeToFuture));
            AddTitle(result, EndingAct.Future);
            var body = fixedFuture ? new List<string>(rescue.Piece.FutureKeys) : CutsOf(future);
            var tails = new List<string>();
            if (!fixedFuture)
            {
                // 관계 꼬리: 본편으로 뽑히지 않았지만 조건이 맞는 미래의 한 줄 (함께한 사람들·혼자 걸은 길)
                foreach (var ending in db.Extras.Endings)
                {
                    if (ending.Category != EndingCategory.Bad && ending.TailKeys.Count > 0 && ending != future?.Piece && IsMet(state, ending.Condition, context, balance))
                    {
                        tails.AddRange(ending.TailKeys);
                    }
                }
            }

            // 세상 꼬리: 구조 장면이 넘긴 줄(완치 논문 등), 치료 단서 줄
            tails.AddRange(CodaOf(rescue));
            var cure = LineRules.Pick(state, db, LineKind.EndingCure, context);
            if (cure != null && !string.IsNullOrEmpty(cure.TextKey))
            {
                tails.Add(cure.TextKey);
            }

            // 맨 끝은 회고 꼬리, 없으면 미래 본편의 마지막 컷이 맺는다
            var coda = CodaOf(reflection);
            if (coda.Count == 0 && body.Count > 1 && tails.Count > 0)
            {
                var last = body[body.Count - 1];
                body.RemoveAt(body.Count - 1);
                AddCuts(result, EndingAct.Future, body);
                AddCuts(result, EndingAct.Future, tails);
                AddCut(result, EndingAct.Future, last);
            }
            else
            {
                AddCuts(result, EndingAct.Future, body);
                AddCuts(result, EndingAct.Future, tails);
                AddCuts(result, EndingAct.Future, coda);
            }
        }

        /// <summary>그 막의 조각 중 조건을 만족한 것에서 우선순위, 그다음 점수.</summary>
        private static EndingActResult Pick(GameState state, IDefinitionDatabase db, EventContext context, EndingAct act)
        {
            EndingDef best = null;
            var bestScore = float.MinValue;
            foreach (var ending in db.Extras.Endings)
            {
                // 막을 적지 않은(Whole) 구조 엔딩은 미래(막 Ⅲ)로 본다 — 세 막으로 나누기 전의 정의와 테스트용 정의
                var endingAct = ending.Act == EndingAct.Whole ? EndingAct.Future : ending.Act;
                if (endingAct != act || ending.Category == EndingCategory.Bad || !IsMet(state, ending.Condition, context, db.Balance))
                {
                    continue;
                }

                var score = Score(state, ending);
                if (best == null || ending.Priority > best.Priority || (ending.Priority == best.Priority && score > bestScore))
                {
                    best = ending;
                    bestScore = score;
                }
            }

            return best == null
                ? null
                : new EndingActResult { Act = act, Piece = best, Score = bestScore, Variant = VariantOf(state, best, context, db.Balance) };
        }

        private static void Remember(EndingResult result, EndingActResult act)
        {
            if (act?.Piece == null)
            {
                return;
            }

            result.Acts.Add(act);
            ConditionEvaluator.EndingActs?.Add(act.Piece.Id);
        }

        private static EndingVariantDef VariantOf(GameState state, EndingDef ending, EventContext context, BalanceConfig balance)
        {
            foreach (var variant in ending.Variants)
            {
                if (IsMet(state, variant.Condition, context, balance))
                {
                    return variant;
                }
            }

            return null;
        }

        private static List<string> CutsOf(EndingActResult act)
        {
            if (act?.Piece == null)
            {
                return new List<string>();
            }

            return new List<string>(act.Variant != null && act.Variant.CutKeys.Count > 0 ? act.Variant.CutKeys : act.Piece.CutKeys);
        }

        private static List<string> CodaOf(EndingActResult act)
        {
            if (act?.Piece == null)
            {
                return new List<string>();
            }

            return new List<string>(act.Variant != null && act.Variant.CodaKeys.Count > 0 ? act.Variant.CodaKeys : act.Piece.CodaKeys);
        }

        /// <summary>그 뒤의 이야기: 묶음마다 조건에 맞는 가장 높은 우선순위 한 줄 (묶음은 처음 나온 순서).</summary>
        private static List<LineDef> EpilogueLines(GameState state, IDefinitionDatabase db, EventContext context)
        {
            var groups = new List<string>();
            var bestByGroup = new Dictionary<string, LineDef>();
            foreach (var line in db.Extras.Lines)
            {
                if (line.Kind != LineKind.EndingEpilogue || string.IsNullOrEmpty(line.TextKey) || !IsMet(state, line.Condition, context, db.Balance))
                {
                    continue;
                }

                var group = line.Group ?? "";
                if (!bestByGroup.TryGetValue(group, out var current))
                {
                    groups.Add(group);
                    bestByGroup[group] = line;
                }
                else if (line.Priority > current.Priority)
                {
                    bestByGroup[group] = line;
                }
            }

            var result = new List<LineDef>();
            foreach (var group in groups)
            {
                result.Add(bestByGroup[group]);
            }

            return result;
        }

        /// <summary>우선순위가 높은 것부터 count개 (같으면 앞에 있는 것).</summary>
        private static List<LineDef> TopByPriority(List<LineDef> lines, int count)
        {
            var sorted = new List<LineDef>(lines);
            // 안정 정렬: 삽입 정렬 (줄 수가 적다)
            for (var i = 1; i < sorted.Count; i++)
            {
                var item = sorted[i];
                var j = i - 1;
                while (j >= 0 && sorted[j].Priority < item.Priority)
                {
                    sorted[j + 1] = sorted[j];
                    j--;
                }

                sorted[j + 1] = item;
            }

            return sorted.Count > count ? sorted.GetRange(0, count) : sorted;
        }

        /// <summary>막 사이의 다리 문장: 그 묶음에서 조건에 맞는 가장 높은 우선순위 하나.</summary>
        private static LineDef Bridge(GameState state, IDefinitionDatabase db, EventContext context, string group)
        {
            LineDef best = null;
            foreach (var line in db.Extras.Lines)
            {
                if (line.Kind == LineKind.EndingBridge && line.Group == group && !string.IsNullOrEmpty(line.TextKey)
                    && IsMet(state, line.Condition, context, db.Balance) && (best == null || line.Priority > best.Priority))
                {
                    best = line;
                }
            }

            return best;
        }

        private static void AddTitle(EndingResult result, EndingAct act)
        {
            result.Pages.Add(new EndingPage { Kind = EndingPageKind.ActTitle, Act = act, Key = "ending.act." + act });
        }

        private static void AddLine(EndingResult result, EndingAct act, LineDef line)
        {
            if (line != null && !string.IsNullOrEmpty(line.TextKey))
            {
                AddCut(result, act, line.TextKey);
            }
        }

        private static void AddCuts(EndingResult result, EndingAct act, List<string> keys)
        {
            foreach (var key in keys)
            {
                AddCut(result, act, key);
            }
        }

        private static void AddCut(EndingResult result, EndingAct act, string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            result.CutKeys.Add(key);
            result.Pages.Add(new EndingPage { Kind = EndingPageKind.Cut, Act = act, Key = key });
        }

        /// <summary>점수 = Σ 값 × 가중치.</summary>
        public static float Score(GameState state, EndingDef ending)
        {
            var score = 0f;
            foreach (var term in ending.Score)
            {
                try
                {
                    score += ConditionEvaluator.ValueOf(state, term.Kind, term.Key) * term.Weight;
                }
                catch (ArgumentException)
                {
                    // 잘못된 키는 검증 도구가 잡는다
                }
            }

            return score;
        }

        private static bool IsMet(GameState state, ConditionDef condition, EventContext context, BalanceConfig balance)
        {
            try
            {
                return ConditionEvaluator.IsMet(state, condition, context, balance);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
    }
}
