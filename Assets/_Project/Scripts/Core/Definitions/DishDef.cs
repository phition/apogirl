using System;
using System.Collections.Generic;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Definitions
{
    /// <summary>요리 수업으로 만드는 요리 (사용자 요청). 요리 숙련이 MinCookingLevel 이상이면 만들 수 있다.</summary>
    [Serializable]
    public sealed class DishDef
    {
        public string Id = "";
        public int MinCookingLevel;

        /// <summary>요리가 완성될 때 쓰는 재료.</summary>
        public int FoodCost;
        public int WaterCost;

        /// <summary>먹고 나서 오르는 포만·수분 (0 위로도 올라 배부름이 된다), 줄어드는 스트레스.</summary>
        public int Satiety;
        public int Hydration;
        public int Stress;

        /// <summary>좋은 요리는 능력치도 조금 오른다.</summary>
        public List<StatAmount> Stats = new List<StatAmount>();
    }

    /// <summary>포만·수분 단계: 값이 Min 이상인 가장 높은 단계가 그날의 효과 (매일 밤).</summary>
    [Serializable]
    public sealed class NourishmentBand
    {
        public int Min;
        public string Key = "";
        public int Hp;
        public int Stress;
    }
}
