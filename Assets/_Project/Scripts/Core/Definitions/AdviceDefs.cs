using System;
using System.Collections.Generic;

namespace ApoGirl.Core.Definitions
{
    /// <summary>
    /// 거점 상황판 "다음 할 일" 한 줄 (BASE_TAB_PROPOSAL 6절·P4). 원본은 Content/Base/base_advice.json.
    /// 문구는 UI 키 "advice.{Id}" (선생님 말투). 조건(이벤트와 같은 ConditionDef)과 상황판 예상값 비교(Facts)를 모두 만족해야 나온다.
    /// </summary>
    [Serializable]
    public sealed class AdviceDef
    {
        public string Id = "";

        /// <summary>클수록 먼저 (같으면 파일 순서).</summary>
        public int Priority;

        /// <summary>누르면 계획 보드에서 고를 행동. 잠겼거나 이번 주 계획에 이미 있으면 이 줄은 나오지 않는다.</summary>
        public string ActionId = "";

        /// <summary>행동이 없을 때 누르면 갈 곳: "roles"(역할 판), "board"(계획 보드), 비면 누를 수 없는 줄.</summary>
        public string Target = "";

        /// <summary>깃발·주차·자원 같은 이벤트 조건 (비면 검사 안 함).</summary>
        public ConditionDef Condition;

        /// <summary>상황판 예상값 비교 (모두 만족해야 함).</summary>
        public List<AdviceFactCheck> Facts = new List<AdviceFactCheck>();
    }

    /// <summary>상황판 예상값 하나: Fact Op Value.</summary>
    [Serializable]
    public sealed class AdviceFactCheck
    {
        public BaseFact Fact;
        public CompareOp Op;
        public int Value;
    }

    /// <summary>"다음 할 일"이 읽는 상황판 예상값 (BaseAdvice.Facts가 BaseForecast에서 센다).</summary>
    public enum BaseFact
    {
        /// <summary>위협 − 방어(특기 보탬 포함). 습격이 잦은 까닭 줄과 같은 값.</summary>
        ThreatGap,

        /// <summary>이번 주 밤 습격 가능성 단계: 0 없음·1 낮음·2 보통·3 높음 (안전 칸이 안 보이면 0).</summary>
        RaidLevel,

        /// <summary>습격이 오면 뚫릴 위험: 0 모름·1 든든·2 반반·3 뚫리기 쉬움.</summary>
        RepelRisk,

        /// <summary>깡통 경보가 있으면 1.</summary>
        HasAlarm,

        /// <summary>식량이 처음 모자라는 요일 (0 = 이번 주는 버틴다).</summary>
        FoodShortDay,

        /// <summary>식수가 처음 모자라는 요일 (0 = 이번 주는 버틴다).</summary>
        WaterShortDay,

        /// <summary>주말에 상할 식량.</summary>
        FoodSpoil,

        /// <summary>주말 식량이 다음 주 같은 식단에 모자라면 1.</summary>
        FoodTight,

        /// <summary>주말 식수가 다음 주 같은 식단에 모자라면 1.</summary>
        WaterTight,

        /// <summary>지금 빗물 (정수해야 마신다).</summary>
        RawWater,

        /// <summary>배터리로 발전 용량을 올릴 수 있으면 1.</summary>
        CanUpgradePower,

        /// <summary>허수아비가 쓰러져 있으면 1.</summary>
        ScarecrowDown,

        /// <summary>역할 없이 쉬는 동료 수 (고양이 빼고).</summary>
        IdleCompanions,

        /// <summary>맡길 수 있는 역할의 빈 자리 수.</summary>
        FreeRoleSlots,
    }
}
