using System;

namespace ApoGirl.Core.Definitions
{
    /// <summary>
    /// 전기 기기 (전력 = 유지력): 놓여 있으면 발전 용량에서 Power만큼 늘 쓴다. 용량이 모자라면 표 순서대로 켜진다.
    /// 앞으로 전자 편의기기(전기 포트·선풍기·전기장판·충전기 등)는 BalanceConfig.Appliances에 한 줄씩 더한다.
    /// </summary>
    [Serializable]
    public sealed class ApplianceDef
    {
        public string Id = "";

        /// <summary>켜 두는 데 드는 전력.</summary>
        public int Power = 1;

        /// <summary>이 깃발이 있어야 거점에 있다 (비면 늘 있다).</summary>
        public string Flag = "";

        /// <summary>켜져 있으면 식량이 상하기 시작하는 문턱이 이만큼 오른다 (냉장고).</summary>
        public int FoodSpoilageBonus;
    }
}
