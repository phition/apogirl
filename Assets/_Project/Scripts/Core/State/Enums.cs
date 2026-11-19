namespace ApoGirl.Core.State
{
    /// <summary>기본 능력치 8종 (문서 5.1). 설계상 고정된 집합이라 enum으로 둔다.</summary>
    public enum StatType
    {
        Stamina,
        Strength,
        Agility,
        Intellect,
        Craft,
        Willpower,
        Empathy,
        Leadership,
    }

    /// <summary>교육 숙련도 7종 (문서 5.1.1). Lv 0~5.</summary>
    public enum ProficiencyType
    {
        Medicine,
        Survival,
        Engineering,
        SelfDefense,
        Cooking,
        Humanities,
        Mind,
    }

    /// <summary>거점 자원 (문서 5.4).</summary>
    public enum ResourceType
    {
        Food,
        Water,
        Medicine,
        Fuel,
        Power,
        Materials,
        Ammo,
        Intel,
    }

    /// <summary>하루 안의 시간대. 1일 = 낮 슬롯 1 + 밤 슬롯 1 (문서 4.1).</summary>
    public enum DayPhase
    {
        Day,
        Night,
    }

    /// <summary>혈액형 (문서 7.1 ④). Rh는 다루지 않는다.</summary>
    public enum BloodType
    {
        Unknown,
        A,
        B,
        O,
        AB,
    }
}
