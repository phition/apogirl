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

        /// <summary>하루 식량·물 소모 배율.</summary>
        public float Consumption = 1f;

        /// <summary>탐험 피해·쓰러짐 배율 (헤드리스 쓰러짐 확률, 실시간 좀비 피해).</summary>
        public float Harm = 1f;

        /// <summary>사람 위협 배율 (밤 습격·낮 침입·탐험 중 생존자 등장 확률).</summary>
        public float HumanThreat = 1f;

        /// <summary>감염 진행 배율 (주인공·선생님의 주간 진행).</summary>
        public float Infection = 1f;

        /// <summary>탐험 자원 수확 배율.</summary>
        public float Yield = 1f;

        /// <summary>주간 되돌리기 (매주 1회, 같은 주는 한 번).</summary>
        public bool WeeklyUndo;

        /// <summary>자동 수색: 직접 들어가지 않고 헤드리스로 처리.</summary>
        public bool Dispatch;

        /// <summary>자동 수색 보상 배율.</summary>
        public float DispatchYield = 0.7f;
    }
}
