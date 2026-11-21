using System;
using System.Collections.Generic;

namespace ApoGirl.Core.Definitions
{
    // M5: 엔딩 정의 (문서 8). 원본은 Content/Story/endings.json, 문장은 UI 문자열 키.

    /// <summary>엔딩 구분. 판정 우선순위는 EndingDef.Priority로 정하고, 구분은 갤러리 분류와 배드 판정에 쓴다.</summary>
    public enum EndingCategory
    {
        Bad,
        Normal,
        Balanced,
        Specialized,
        Relationship,
        Narrative,
        Hidden,
    }

    /// <summary>점수 항목: 값 × 가중치. 같은 우선순위 후보끼리 점수가 높은 쪽이 이긴다.</summary>
    [Serializable]
    public sealed class EndingScoreTerm
    {
        public ValueKind Kind;
        public string Key = "";
        public float Weight = 1f;
    }

    /// <summary>
    /// 엔딩 변형: 같은 엔딩 안에서 조건(예: 좌석 선택으로 남은 사람)에 따라 컷 문구와 그림을 바꾼다.
    /// 정의 순서대로 처음 맞는 하나만 쓴다. 갤러리 칸은 엔딩 하나로 둔다.
    /// </summary>
    [Serializable]
    public sealed class EndingVariantDef
    {
        public string Id = "";
        public ConditionDef Condition = new ConditionDef();

        /// <summary>비어 있지 않으면 엔딩의 CutKeys 대신 쓴다.</summary>
        public List<string> CutKeys = new List<string>();

        /// <summary>Resources/Art/Story/Endings 아래 그림 이름. 비거나 그림이 없으면 엔딩 ID 그림.</summary>
        public string Image = "";
    }

    /// <summary>엔딩 하나. 제목은 UI 키 "ending.{Id}.title", 후일담은 CutKeys 순서대로.</summary>
    [Serializable]
    public sealed class EndingDef
    {
        public string Id = "";
        public EndingCategory Category;

        /// <summary>높을수록 먼저 (문서 8.1: 히든 > 특화 > 균형 > 일반).</summary>
        public int Priority;

        /// <summary>배드 엔딩 전용: GameState.EndCause가 이 중 하나여야 한다 (비면 모든 원인의 대체 엔딩).</summary>
        public List<string> EndCauses = new List<string>();

        public ConditionDef Condition = new ConditionDef();
        public List<EndingScoreTerm> Score = new List<EndingScoreTerm>();

        /// <summary>후일담 컷 (3~6개 권장).</summary>
        public List<string> CutKeys = new List<string>();

        /// <summary>후일담 배경색 (#RRGGBB). 일러스트가 들어오기 전까지의 분위기 색.</summary>
        public string Color = "#1E2230";

        /// <summary>조건별 컷·그림 변형 (처음 맞는 하나).</summary>
        public List<EndingVariantDef> Variants = new List<EndingVariantDef>();

        public string TitleKey => "ending." + Id + ".title";
    }
}
