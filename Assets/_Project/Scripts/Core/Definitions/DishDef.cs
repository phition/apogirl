using System;

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

        /// <summary>먹고 나서 줄어드는 허기·갈증·스트레스 (양수 = 그만큼 낮춘다).</summary>
        public int Hunger;
        public int Thirst;
        public int Stress;
    }
}
