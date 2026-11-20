using System;
using System.Collections.Generic;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Events;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Endings
{
    /// <summary>엔딩 판정 결과: 고른 엔딩, 보여 줄 후일담 컷(공통 레이어 포함), 주요 장면.</summary>
    public sealed class EndingResult
    {
        public EndingDef Ending;

        /// <summary>엔딩 고유 컷 + 공통 레이어 컷 (UI 문자열 키, 순서대로).</summary>
        public readonly List<string> CutKeys = new List<string>();

        /// <summary>"1년의 기록"의 주요 장면 (UI 문자열 키).</summary>
        public readonly List<string> MomentKeys = new List<string>();

        /// <summary>'그 뒤의 이야기': 세력·동료 후일담 (묶음마다 한 줄, 정의 순서). 구조 엔딩만.</summary>
        public readonly List<string> EpilogueKeys = new List<string>();

        /// <summary>판정에 쓴 점수 (디버그 표시용).</summary>
        public float Score;

        public bool IsBad => Ending != null && Ending.Category == EndingCategory.Bad;
    }

    /// <summary>
    /// 엔딩 판정 (문서 8.1, 부록 B). 순수 함수: 상태를 바꾸지 않는다.
    /// 게임오버 → 종료 원인이 맞는 배드 엔딩. 구조 → 조건을 만족한 엔딩 중 우선순위, 그다음 점수.
    /// </summary>
    public static class EndingResolver
    {
        /// <summary>구조 레이어 순서 (후일담 뒤에 이 순서로 붙는다).</summary>
        private static readonly LineKind[] Layers = { LineKind.EndingTeacher, LineKind.EndingFriend, LineKind.EndingCure, LineKind.EndingTendency };

        public static EndingResult Resolve(GameState state, IDefinitionDatabase db)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (db == null) throw new ArgumentNullException(nameof(db));

            var result = new EndingResult();
            var bad = state.Status == GameStatus.GameOver;
            var context = EventContext.FromCalendar(state);

            EndingDef best = null;
            var bestScore = float.MinValue;
            foreach (var ending in db.Extras.Endings)
            {
                if ((ending.Category == EndingCategory.Bad) != bad)
                {
                    continue;
                }

                if (bad && ending.EndCauses.Count > 0 && !ending.EndCauses.Contains(state.EndCause))
                {
                    continue;
                }

                if (!IsMet(state, ending.Condition, context, db.Balance))
                {
                    continue;
                }

                var score = Score(state, ending);

                // 원인이 정확히 맞는 배드 엔딩이 대체 엔딩(원인 없음)보다 먼저
                var priority = ending.Priority + (bad && ending.EndCauses.Count > 0 ? 10000 : 0);
                var bestPriority = best == null ? int.MinValue : best.Priority + (bad && best.EndCauses.Count > 0 ? 10000 : 0);
                if (best == null || priority > bestPriority || (priority == bestPriority && score > bestScore))
                {
                    best = ending;
                    bestScore = score;
                }
            }

            result.Ending = best;
            result.Score = best == null ? 0f : bestScore;
            if (best != null)
            {
                result.CutKeys.AddRange(best.CutKeys);
            }

            if (!bad)
            {
                foreach (var kind in Layers)
                {
                    var line = LineRules.Pick(state, db, kind, context);
                    if (line != null && !string.IsNullOrEmpty(line.TextKey))
                    {
                        result.CutKeys.Add(line.TextKey);
                    }
                }

                // 그 뒤의 이야기: 묶음마다 조건에 맞는 가장 높은 우선순위 한 줄 (묶음은 처음 나온 순서)
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

                foreach (var group in groups)
                {
                    result.EpilogueKeys.Add(bestByGroup[group].TextKey);
                }
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
