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

        /// <summary>기록 엔딩: 1년 동안 남은 플레이 기록으로 고른다 (Docs/PROPOSAL_ENDINGS_EXPANSION 4절). 값은 저장된 정의의 숫자라 끝에 더한다.</summary>
        Record,
    }

    /// <summary>
    /// 엔딩이 놓이는 막 (Docs/PROPOSAL_ENDINGS_EXPANSION 7절). 구조 엔딩은 세 막을 하나씩 골라 잇는다.
    /// Whole = 한 편으로 끝나는 엔딩(배드). 값은 저장된 정의의 숫자라 차례를 바꾸지 않는다.
    /// </summary>
    public enum EndingAct
    {
        Whole,

        /// <summary>Ⅰ 구조의 날: 누가 타고 누가 남았나.</summary>
        Rescue,

        /// <summary>Ⅱ 돌아보는 밤: 그 1년이 내 안에 남긴 것.</summary>
        Reflection,

        /// <summary>Ⅲ 그 뒤의 나: 어떤 사람이 되었나.</summary>
        Future,
    }

    /// <summary>
    /// 점수 항목: 값 × 가중치. 같은 우선순위 후보끼리는 먼저 '문턱을 넘은 정도'(EndingResolver.Margin)로 겨루고,
    /// 그것마저 같을 때만 이 점수가 가른다 (기획서 9.2절 E3).
    /// </summary>
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

        /// <summary>비어 있지 않으면 엔딩의 CodaKeys 대신 쓴다 (막 Ⅲ 꼬리로 넘기는 줄).</summary>
        public List<string> CodaKeys = new List<string>();

        /// <summary>Resources/Art/Story/Endings 아래 그림 이름. 비거나 그림이 없으면 엔딩 ID 그림.</summary>
        public string Image = "";
    }

    /// <summary>엔딩 하나. 제목은 UI 키 "ending.{Id}.title", 후일담은 CutKeys 순서대로.</summary>
    [Serializable]
    public sealed class EndingDef
    {
        public string Id = "";
        public EndingCategory Category;

        /// <summary>이 엔딩이 놓이는 막. 같은 막의 조각끼리만 겨룬다 (배드는 Whole).</summary>
        public EndingAct Act;

        /// <summary>높을수록 먼저 (문서 8.1: 히든 > 특화 > 균형 > 일반). 막 안에서만 비교한다.</summary>
        public int Priority;

        /// <summary>배드 엔딩 전용: GameState.EndCause가 이 중 하나여야 한다 (비면 모든 원인의 대체 엔딩).</summary>
        public List<string> EndCauses = new List<string>();

        public ConditionDef Condition = new ConditionDef();
        public List<EndingScoreTerm> Score = new List<EndingScoreTerm>();

        /// <summary>이 막에서 보여 주는 컷.</summary>
        public List<string> CutKeys = new List<string>();

        /// <summary>막 Ⅲ의 꼬리로 넘기는 줄: 회고 축의 "그 뒤" 한 줄, 구조 장면의 세상 이야기(완치 논문 등).</summary>
        public List<string> CodaKeys = new List<string>();

        /// <summary>구조 장면 전용: 비어 있지 않으면 미래(막 Ⅲ)를 판정하지 않고 이 컷을 본편으로 쓴다 (남기로 한 판).</summary>
        public List<string> FutureKeys = new List<string>();

        /// <summary>미래 전용: 본편으로 뽑히지 않았어도 조건이 맞으면 다른 미래 뒤에 붙는 한 줄 (함께한 사람들·혼자 걸은 길).</summary>
        public List<string> TailKeys = new List<string>();

        /// <summary>후일담 배경색 (#RRGGBB). 일러스트가 들어오기 전까지의 분위기 색.</summary>
        public string Color = "#1E2230";

        /// <summary>조건별 컷·그림 변형 (처음 맞는 하나).</summary>
        public List<EndingVariantDef> Variants = new List<EndingVariantDef>();

        public string TitleKey => "ending." + Id + ".title";
    }
}
