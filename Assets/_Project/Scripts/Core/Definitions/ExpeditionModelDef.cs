using System;
using System.Collections.Generic;

namespace ApoGirl.Core.Definitions
{
    /// <summary>헤드리스 탐험 모델의 시간대별 수치 (문서 6.3.2-(5), 9.0).</summary>
    [Serializable]
    public sealed class ExpeditionYield
    {
        public List<ResourceRange> Resources = new List<ResourceRange>();
        public float BaseDownChance = 0.05f;
        public int Fatigue = 15;
        public int StressMin = 10;
        public int StressMax = 10;

        /// <summary>낮 탐험 도덕 선택으로 오르는 양심의 무게 최댓값 (0이면 변화 없음).</summary>
        public int ConscienceMax;

        /// <summary>밤 탐험에서 새벽까지 버티게 될 확률 (수면 부채 +1, 다음 휴식 회복 감소).</summary>
        public float DawnReturnChance;

        public int HpLossOnDown = 30;
        public int StressOnDown = 30;

        /// <summary>M7: 무사히 돌아온 헤드리스 탐험에서 조건이 맞는 조우 하나를 만날 확률 (조우마다 따로 굴린다).</summary>
        public float EncounterChance = 0.5f;
    }

    /// <summary>
    /// 지역별 탐험 확률 모델. 실시간 탐험(M2b) 전까지 M1 시뮬레이션과 M7 봇 테스트에서 쓴다.
    /// </summary>
    [Serializable]
    public sealed class ExpeditionModelDef
    {
        public string Id = "";
        public ExpeditionYield Day = new ExpeditionYield();
        public ExpeditionYield Night = new ExpeditionYield();
        public float AgilityDownReduction = 0.001f;
        public float SleepDebtDownIncrease = 0.03f;
        public float ConsecutiveDownIncrease = 0.1f;

        /// <summary>상자가 고갈되는 지역(LocationDef.DepletingContainers)에서 헤드리스 1회가 뒤지는 상자 수. 남은 상자가 적으면 수확이 그 비율로 준다.</summary>
        public int ContainersPerRun;
    }
}
