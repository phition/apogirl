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

        /// <summary>과학 (정수·약품 효율, 거처를 교실로 삼으면 열린다).</summary>
        Science,

        /// <summary>음악 (스트레스·트라우마 회복).</summary>
        Music,
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

        /// <summary>빗물·받아 둔 물. 그대로는 마실 수 없고 정수해야 식수(Water)가 된다.</summary>
        RawWater,

        /// <summary>배터리 (전력 = 유지력): 모아서 발전 용량을 올린다 (PowerRules.Upgrade).</summary>
        Battery,
    }

    /// <summary>하루 안의 시간대. 1일 = 낮 슬롯 1 + 밤 슬롯 1 (문서 4.1).</summary>
    public enum DayPhase
    {
        Day,
        Night,
    }

    /// <summary>생존 상태 수치 (문서 5.2). 모두 0~100.</summary>
    public enum VitalType
    {
        Hp,
        Fatigue,
        Stress,
        Infection,
        Cleanliness,
        /// <summary>포만 (−100~+100, 0 = 보통). +면 배부름, −면 허기짐.</summary>
        Satiety,

        /// <summary>수분 (−100~+100, 0 = 보통). −면 목마름.</summary>
        Hydration,
    }

    /// <summary>엔딩 판정용 숨김 수치 (문서 5.3).</summary>
    public enum HiddenType
    {
        TraumaRecovery,
        SelfAwareness,
        Humanity,
        Hope,
        Isolation,
    }

    /// <summary>계절 (문서 4.4). 봄 1~13 / 여름 14~26 / 가을 27~39 / 겨울 40~52주.</summary>
    public enum Season
    {
        Spring,
        Summer,
        Autumn,
        Winter,
    }

    /// <summary>주간 날씨 (M6, 문서 6.6). 계절표로 굴린다.</summary>
    public enum WeatherType
    {
        Clear,
        Rain,
        Heat,
        Cold,
    }

    /// <summary>게임 진행 상태.</summary>
    public enum GameStatus
    {
        Playing,
        Rescued,
        GameOver,
    }

    /// <summary>NPC 관계 단계 (문서 6.4). Unknown = 아직 만나지 않음.</summary>
    public enum NpcStatus
    {
        Unknown,
        Met,
        Joined,
        Left,
        Lost,
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
