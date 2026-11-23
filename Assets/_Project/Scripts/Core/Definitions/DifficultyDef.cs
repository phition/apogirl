using System;

namespace ApoGirl.Core.Definitions
{
    /// <summary>
    /// 난이도 (M7-3, M7-d·M7-e). 배율은 1이 표준. 새 게임에서 고르고 바꿀 수 없다.
    /// </summary>
    [Serializable]
    public sealed class DifficultyDef
    {
        public string Id = "STANDARD";

        /// <summary>쓰지 않는다 (2026-11-23부터 식단 소비는 난이도로 할인하지 않는다). 옛 덮어쓰기 파일 호환으로만 남긴다.</summary>
        public float Consumption = 1f;

        /// <summary>탐험 피해·쓰러짐 배율 (헤드리스 쓰러짐 확률, 실시간 좀비 피해).</summary>
        public float Harm = 1f;

        /// <summary>사람 위협 배율 (밤 습격·낮 침입·탐험 중 생존자 등장 확률).</summary>
        public float HumanThreat = 1f;

        /// <summary>감염 진행 배율 (주인공·선생님의 주간 진행).</summary>
        public float Infection = 1f;

        /// <summary>탐험 자원 수확 배율 (모든 자원).</summary>
        public float Yield = 1f;

        /// <summary>
        /// 자원별 수확 배율 (소수는 확률 반올림). 의약품은 Yield에 곱하고, 식량·물은 Yield 없이 이 값만 쓴다:
        /// 구한 식량·물의 변환 (통조림·생수 5 기준 이야기 중심 1 = 5, 표준 0.8 = 4, 하드 0.6 = 3, 사용자 결정 2026-11-23).
        /// </summary>
        public float FoodYield = 1f;
        public float WaterYield = 1f;
        public float MedicineYield = 1f;

        /// <summary>주간 되돌리기 (매주 1회, 같은 주는 한 번).</summary>
        public bool WeeklyUndo;

        /// <summary>자동 수색: 직접 들어가지 않고 헤드리스로 처리.</summary>
        public bool Dispatch;

        /// <summary>자동 수색 보상 배율.</summary>
        public float DispatchYield = 0.7f;
    }
}
