using System;
using System.Collections.Generic;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Definitions
{
    /// <summary>스탯 성장 체감 구간: 현재 값이 FromValue 이상이면 Multiplier 적용.</summary>
    [Serializable]
    public sealed class GrowthBand
    {
        public int FromValue;
        public float Multiplier = 1f;

        public GrowthBand()
        {
        }

        public GrowthBand(int fromValue, float multiplier)
        {
            FromValue = fromValue;
            Multiplier = multiplier;
        }
    }

    /// <summary>
    /// 밸런스 수치 모음 (문서 9.1 초기안 + M1 설계 추천안). 모든 규칙 공식은 이 값만 읽는다.
    /// 배열 인덱스가 "단계"인 항목은 0단계부터 시작한다.
    /// </summary>
    /// <summary>계절 하나의 날씨 가중치.</summary>
    [Serializable]
    public sealed class WeatherOdds
    {
        public Season Season;
        public int Clear;
        public int Rain;
        public int Heat;
        public int Cold;

        public WeatherOdds()
        {
        }

        public WeatherOdds(Season season, int clear, int rain, int heat, int cold)
        {
            Season = season;
            Clear = clear;
            Rain = rain;
            Heat = heat;
            Cold = cold;
        }
    }

    [Serializable]
    public sealed class BalanceConfig
    {
        // ── 시작 ──
        public int BaseStatValue = 10;
        public int StartingHp = 100;

        // ── 부상·청결 (사용자 요청) ──

        /// <summary>한 사건(슬롯 하나·이벤트 선택 하나)에서 최대 체력의 이 비율 이상을 잃으면 '부상'.</summary>
        public float InjuryHpFraction = 0.3f;

        /// <summary>응급처치·의학 숙련 1레벨마다 부상 기준이 이만큼 높아진다 (30% → Lv 2면 40%).</summary>
        public float InjuryHpFractionPerMedicineLevel = 0.05f;

        /// <summary>부상을 의약품으로 치유할 때 쓰는 양: 밤 휴식(있으면 반드시)·응급처치 수업 성공 때.</summary>
        public int InjuryMedicineCost = 1;

        /// <summary>응급처치 숙련이 있고 탐험에서 이만큼 이상 잃었지만 부상까지 가지 않았으면 '응급 치료로 마무리' 서술.</summary>
        public int FirstAidNarrativeMinLoss = 15;

        /// <summary>이 숙련 이상이면 응급 치료 서술이 능숙한 쪽으로 바뀐다.</summary>
        public int FirstAidSkilledLevel = 3;

        /// <summary>부상 상태 플래그. 이 동안 체력이 회복되지 않고, 의약품 치료로만 풀린다.</summary>
        public string InjuredFlag = "INJURED";

        /// <summary>
        /// 부상 때 상처 감염: 청결이 WoundCleanThreshold 이상이면 기본값만(0), 그보다 지저분할수록 최대 WoundInfectionDirty까지.
        /// (감염도가 0보다 크면 매주 진행되므로, 깨끗하게 지내면 다쳐도 감염이 시작되지 않게 한다)
        /// </summary>
        public int WoundInfectionBase = 0;
        public int WoundInfectionDirty = 10;
        public int WoundCleanThreshold = 50;

        /// <summary>물림 감염도 더러울수록 늘어난다: × (1 + 더러움 × 이 값).</summary>
        public float BiteDirtyBonus = 0.5f;

        /// <summary>청결: 시작값, 주마다 떨어지는 양. 청소·정수 같은 위생 활동으로 오른다.</summary>
        public int StartingCleanliness = 70;
        public int CleanlinessWeeklyDecay = 15;

        public List<ResourceAmount> InitialResources = new List<ResourceAmount>
        {
            new ResourceAmount(ResourceType.Food, 14),
            new ResourceAmount(ResourceType.Water, 20),
            new ResourceAmount(ResourceType.Medicine, 2),
            new ResourceAmount(ResourceType.Power, 5),
        };

        // ── 범위 ──
        public int StatMax = 999;
        public int VitalMax = 100;
        public int HiddenMax = 100;
        public int ConscienceMax = 10;
        public int SleepDebtMax = 5;
        public int ProficiencyMaxLevel = 5;
        public int NpcAffinityMax = 100;
        public int NpcAffinityMin = -100;

        // ── 달력·이벤트 (M3) ──

        /// <summary>1주차 1일의 실제 날짜 (문서 4.1: 3월 셋째 주 무렵). 윤년은 쓰지 않는다.</summary>
        public int StartMonth = 3;
        public int StartDayOfMonth = 16;

        /// <summary>랜덤·위기 이벤트 빈도 상한 (반복 피로 방지).</summary>
        public int RandomEventsPerDay = 1;
        public int RandomEventsPerWeek = 3;

        /// <summary>슬롯이 끝날 때마다 랜덤 이벤트를 굴릴 확률 (상한 이내일 때).</summary>
        public float RandomEventChance = 0.3f;

        // ── M4: 시작·습격·장비 ──

        /// <summary>
        /// 1주차: TutorialDay 전날까지는 창고에 갇혀 버티고(StorageWaitActionId, 잠금),
        /// TutorialDay 낮에 탈출(TutorialActionId, 잠금), 그날 밤은 휴식(잠금). 행동이 없으면 생략.
        /// </summary>
        public string TutorialActionId = "ACT_EXP_TUTORIAL";

        public string StorageWaitActionId = "ACT_STORAGE_WAIT";

        /// <summary>창고 탈출 날 (1~7). 7이면 1주차 마지막 날.</summary>
        public int TutorialDay = 7;

        /// <summary>새 게임을 시작하면 허브에서 바로 띄우는 도입 이벤트 (창고에 갇힘).</summary>
        public string IntroEventId = "EVT_A1_TRAPPED";

        /// <summary>선생님이 창고 문을 여는 이벤트. 탈출 전날 밤 끝, 그날의 갇힘 이벤트(EVT_A1_STORAGE_D6)에서 ThenEventId로 이어진다.</summary>
        public string OpeningEventId = "EVT_A1_OPENING";

        /// <summary>2회차: 여섯째 날 갇힘 이벤트 대신 뜨는 회상 ("창고 안의 7일", 단서). 데이터 조건(NG_PLUS)으로 뜨고 시작 이벤트로 이어진다.</summary>
        public string NgPlusOpeningEventId = "EVT_NG_STORAGE_7DAYS";

        /// <summary>밤 습격 (문서 5.5): 이 주차부터, 확률 = 기본 + (위협 − 방어) × 계수, 상한.</summary>
        public int RaidStartWeek = 4;
        public float RaidBaseChance = 0.01f; // M7-2: 0.02 → 0.01
        public float RaidChancePerPoint = 0.0015f; // M7-2: 0.004 → 0.0015 (연 30회 → 목표 10~15회)
        public float RaidMaxChance = 0.15f; // M7-2: 0.25 → 0.15

        /// <summary>습격 세기 = 위협 + 0~분산, 방어력 = 전투력 + 방어도 × 배율 + 경계 보정 + 0~분산.</summary>
        public int RaidStrengthVariance = 20;
        public int RaidPowerVariance = 20;
        public float RaidDefenseWeight = 1.2f; // M7-2: 2 → 1.2 (방어 60이면 늘 막던 것을 후반에 가끔 뚫리게)

        /// <summary>거점 기본 방어력 (선생님·지형). 전투력·방어도에 더해진다.</summary>
        public int RaidBasePower = 20;

        /// <summary>습격 결과.</summary>
        public int RaidRepelStress = 6;
        public int RaidRepelDefenseLoss = 1;
        public int RaidBreachStress = 18;
        public int RaidBreachHp = 10;
        public int RaidBreachFood = 3;
        public int RaidBreachDefenseLoss = 3;
        public int RaidThreatRelief = 20; // M7-2: 8 → 20 (습격 빈도는 위협 증가량 ÷ 이 값으로 정해진다: 연 30회 → 약 12회)

        /// <summary>위협도: 탐험마다(낮/밤), 매주 자연 증가, 상한.</summary>
        public int ThreatPerDayExpedition = 2;
        public int ThreatPerNightExpedition = 2;
        public int ThreatWeekly = 1;
        public int ThreatMax = 100;
        public int DefenseMax = 60;

        /// <summary>배낭 없이 나갈 때의 주머니 격자.</summary>
        public int PocketWidth = 3;
        public int PocketHeight = 2;

        /// <summary>쓰러졌을 때 잃는 것: 배낭 내용과 착용 장비 (M4 결정).</summary>
        public bool DownLosesEquipment = true;

        /// <summary>손전등 없이 휴대폰 불빛만 있을 때 밝기·거리 배율.</summary>
        public float PhoneLightMultiplier = 0.55f;

        // ── 성장 ──
        public List<GrowthBand> GrowthBands = new List<GrowthBand>
        {
            new GrowthBand(0, 1f),
            new GrowthBand(100, 0.5f),
            new GrowthBand(200, 0.25f),
        };

        public float GreatSuccessGainMultiplier = 1.5f;
        public float FailureGainMultiplier = 0.5f;
        public float TeacherDirectMultiplier = 1.5f;
        public float TeacherGuidanceMultiplier = 1.15f;
        public float RoutineBonusPerStep = 0.1f;
        public float RoutineBonusMax = 0.3f;
        public float RoutineBonusMaxForJ = 0.4f;

        /// <summary>Lv1~5 도달에 필요한 누적 XP.</summary>
        public int[] ProficiencyXpThresholds = { 10, 25, 45, 70, 100 };

        public int GreatSuccessBonusXp = 1;

        // ── 판정 ──
        public float CheckMinChance = 0.05f;
        public float CheckMaxChance = 0.95f;

        /// <summary>성공 확률 중 대성공이 차지하는 비율.</summary>
        public float GreatSuccessRatio = 0.15f;

        public float StressCheckPenaltyPerPoint = 0.003f;
        public float FatigueCheckPenaltyPerPoint = 0.002f;

        // ── 수면 부채 (문서 6.3.2-(5), 인덱스 = 단계 0~5) ──
        public float[] SleepDebtEfficiencyPenalty = { 0f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f };
        public float[] SleepDebtCheckPenalty = { 0f, 0f, 0f, 0.1f, 0.1f, 0.15f };
        public float[] SleepDebtStressMultiplier = { 1f, 1f, 1f, 1.25f, 1.25f, 1.5f };
        public int SleepDebtForcedRestStage = 4;
        public int NightExpeditionSleepDebt = 1;
        public int ForcedRestViolationSleepDebt = 1;
        public int ConsecutiveExpeditionSleepDebt = 2;
        public int NightRestStreakToRepay = 2;
        public int NightRestRepayAmount = 1;

        // ── 강제 휴식 위반 (문서 4.2) ──
        public int ViolationFatigue = 10;
        public int ViolationStress = 8;

        // ── 소모 (2인 기준, 문서 9.1 / 5.4) ──
        public int DailyFood = 2;
        public int DailyWater = 2;
        public int WeeklyPower = 3;
        // ── 포만·수분 (사용자 요청: 0이 가운데, +면 배부름, −면 허기짐) ──

        /// <summary>매일 밤 빠지는 포만·수분.</summary>
        public int SatietyDailyDrain = 25;
        public int HydrationDailyDrain = 30;

        /// <summary>0 아래면 그날 몫을 먹고 마신다: 포만·수분이 이만큼 오르되 0까지만 (0 이상이면 먹지 않아 아낀다).</summary>
        public int MealSatiety = 45;
        public int DrinkHydration = 55;

        /// <summary>이 값 이하면 굶주림·탈수로 쓰러진 것으로 친다 (사망 원인 판정).</summary>
        public int StarvationThreshold = -60;

        /// <summary>
        /// 요리 (요리 수업 성공 시, 사용자 요청): 숙련이 오를수록 좋은 요리. 같은 재료로 하루 식사보다 허기·갈증을 더 낮춘다.
        /// </summary>
        public DishDef[] CookingDishes =
        {
            new DishDef { Id = "DISH_PORRIDGE", MinCookingLevel = 0, FoodCost = 1, WaterCost = 1, Satiety = 20, Hydration = 15 },
            new DishDef
            {
                Id = "DISH_SOUP", MinCookingLevel = 2, FoodCost = 2, WaterCost = 2, Satiety = 35, Hydration = 40, Stress = 2,
                Stats = { new StatAmount(StatType.Stamina, 1) },
            },
            new DishDef
            {
                Id = "DISH_SET_MEAL", MinCookingLevel = 4, FoodCost = 2, WaterCost = 1, Satiety = 50, Hydration = 30, Stress = 5,
                Stats = { new StatAmount(StatType.Stamina, 1), new StatAmount(StatType.Willpower, 1) },
            },
        };

        public int StarvationHpLoss = 5;
        public int DehydrationThreshold = -60;
        public int DehydrationHpLoss = 10;

        /// <summary>포만 단계 (높은 Min부터): 든든함·배부름·보통·허기짐·굶주림.</summary>
        public NourishmentBand[] SatietyBands =
        {
            new NourishmentBand { Min = 50, Key = "SATED", Hp = 2, Stress = -2 },
            new NourishmentBand { Min = 1, Key = "FULL", Hp = 1, Stress = -1 },
            new NourishmentBand { Min = -29, Key = "NORMAL" },
            new NourishmentBand { Min = -59, Key = "HUNGRY", Stress = 3 },
            new NourishmentBand { Min = -100, Key = "STARVING", Hp = -5, Stress = 5 },
        };

        /// <summary>수분 단계: 넉넉함·촉촉함·보통·목마름·탈수.</summary>
        public NourishmentBand[] HydrationBands =
        {
            new NourishmentBand { Min = 50, Key = "PLENTY", Hp = 1, Stress = -1 },
            new NourishmentBand { Min = 1, Key = "GOOD", Hp = 1 },
            new NourishmentBand { Min = -29, Key = "NORMAL" },
            new NourishmentBand { Min = -59, Key = "THIRSTY", Stress = 3 },
            new NourishmentBand { Min = -100, Key = "DEHYDRATED", Hp = -10, Stress = 5 },
        };

        // ── 난이도 (M7-3: M7-d 배율, M7-e 이야기 중심 편의) ──

        public string DefaultDifficulty = "STANDARD";

        public DifficultyDef[] Difficulties =
        {
            new DifficultyDef
            {
                Id = "STORY", Consumption = 0.75f, Harm = 0.6f, HumanThreat = 0.7f, Infection = 0.6f, Yield = 1f,
                WeeklyUndo = true, Dispatch = true, DispatchYield = 0.7f,
            },
            new DifficultyDef { Id = "STANDARD" },
            // 하드: 가혹해진 세계에 맞춰 소모는 표준과 같게 (2026-11-20 결정, M7-d의 ×1.25에서 수정)
            new DifficultyDef { Id = "HARD", Consumption = 1f, Harm = 1.4f, HumanThreat = 1.3f, Infection = 1.3f, Yield = 0.9f },
        };

        // ── 적대적 생존자 (낮 탐험, M7 추가 요청) ──

        public string HostileEnemyId = "ENEMY_SURVIVOR";

        /// <summary>이 주차부터 나온다 (튜토리얼 기간은 안전).</summary>
        public int HostileStartWeek = 5;

        /// <summary>낮 탐험 1회 등장 확률: 1막(1~13주) / 2막 / 3막 / 4막. 지역 변형의 HostileChanceMultiplier를 곱한다.</summary>
        public float[] HostileChanceByAct = { 0.10f, 0.15f, 0.20f, 0.25f };

        /// <summary>노출도 1당 생존자 등장 확률 추가 (낮에 자주 나가면 다른 생존자 눈에 띈다). 노출도는 낮 탐험마다 +1, 매주 −ExposureDecayPerWeek, 최대 MaxExposure.</summary>
        public float HostileChancePerExposure = 0.06f;
        public int ExposureDecayPerWeek = 1;
        public int MaxExposure = 6;

        /// <summary>노출도 1당 낮 침입 주 확률 추가 ("뒤를 밟혔다", 낮 침입이 시작된 뒤에만).</summary>
        public float IntrusionChancePerExposure = 0.02f;

        /// <summary>막별 성향 가중치 (위협, 강탈, 강탈+구타) × 4막. 막이 갈수록 거칠어진다.</summary>
        public int[] HostileWeightsByAct = { 55, 35, 10, 45, 40, 15, 32, 45, 23, 20, 50, 30 };

        /// <summary>막별 생존자 수 (한 무리는 같은 성향).</summary>
        public int[] HostileCountMinByAct = { 1, 1, 2, 3 };
        public int[] HostileCountMaxByAct = { 1, 2, 3, 3 };

        /// <summary>구타: 체력 손실 (돌아올 때 최소 1은 남긴다), 추가 스트레스, 트라우마 회복 숨김 수치 감소.</summary>
        public int BeatenHpLoss = 40;
        public int BeatenStress = 15;
        public int BeatenTraumaRecoveryLoss = 3;
        public int RobbedStress = 8;
        public int EvictedStress = 4;

        /// <summary>폭행 트라우마: 단계당 낮 탐험 스트레스 +DayTraumaStressPerLevel (최대 MaxAssaultTrauma 단계).</summary>
        public int MaxAssaultTrauma = 3;
        public int DayTraumaStressPerLevel = 4;

        /// <summary>다시 맞지 않고 이만큼 주가 지나면 1단계 회복 (돌봄 행동이 주 수를 앞당긴다: GameEffect.TraumaCare).</summary>
        public int TraumaRecoveryWeeks = 6;

        /// <summary>헤드리스 탐험: 생존자가 있을 때 붙잡힐 확률 (한 명 더 있을 때마다 +PerExtra, 최대 Max).</summary>
        public float HeadlessHostileCaughtChance = 0.5f;
        public float HeadlessHostileCaughtPerExtra = 0.1f;
        public float HeadlessHostileCaughtMax = 0.8f;

        // ── 탐험 상자: 기본 빈 상자, 고갈, 매달 복구 ──

        /// <summary>모든 지역에서 처음부터 비어 있는 상자 비율 (판 시드로 정해지고 복구되지 않는다).</summary>
        public float BaseEmptyContainerRate = 0.3f;

        /// <summary>매달 첫 주: 복구되는 지역(교실동 제외)에서 뒤진 상자 중 다시 채워지는 비율.</summary>
        public float RestockFraction = 0.5f;

        // ── 낮 침입 (깡패, 3막부터) ──

        public int IntrusionStartWeek = 27;
        public float IntrusionWeeklyChanceAct3 = 0.12f;
        public float IntrusionWeeklyChanceAct4 = 0.18f;
        public string IntrusionEventId = "EVT_DAY_INTRUSION";
        public string IntrusionActionId = "ACT_INTRUSION";

        /// <summary>깡패 세기 = 기본 (+4막 보너스) + 0~편차.</summary>
        public int IntrusionStrength = 55;
        public int IntrusionStrengthAct4Bonus = 15;
        public int IntrusionStrengthVariance = 20;

        /// <summary>빼앗기는 식량·물·약 비율: 맞서다 짐 / 숨었다 들킴 / 숨어서 넘김(뒤지고 감) / 내줌.</summary>
        public float IntrusionLossLost = 0.25f;
        public float IntrusionLossHidden = 0.1f;
        public float IntrusionLossYield = 0.15f;

        public float IntrusionHideBaseChance = 0.35f;
        public float IntrusionHideAlarmBonus = 0.3f;
        public float IntrusionHidePerDefense = 0.004f;

        public int IntrusionWinStress = 8;
        public int IntrusionLoseHp = 15;
        public int IntrusionLoseStress = 15;
        public int IntrusionYieldStress = 10;
        public int IntrusionHideStress = 6;

        /// <summary>대비가 부족하면 학교에 있던 동료 한 명이 다쳐 이만큼 주 동안 역할을 못 한다.</summary>
        public int IntrusionInjuryWeeks = 2;

        // ── 거점 생산 ──

        /// <summary>빗물통 (플래그 RAIN_BARRELS): 비 오는 주마다 주간 정산 때 빗물(RawWater, 정수해야 식수).</summary>
        public int RainBarrelWater = 10;

        /// <summary>비 오는 주에는 빗물통 없이도 양동이로 빗물(RawWater)을 조금 받는다.</summary>
        public int RainCollectWater = 5;

        /// <summary>매주 학교 물탱크·화장실에서 받아 두는 생활용수 (RawWater, 정수해야 식수).</summary>
        public int WeeklyRawWater = 5;

        /// <summary>옥상 텃밭 (플래그 ROOF_GARDEN): 겨울을 뺀 매주 식량.</summary>
        public int RoofGardenFood = 7;

        // ── 거점 방어 작업 ──

        /// <summary>함정: 습격·침입 1회에 하나를 써서 방어력 +TrapPower (최대 MaxTraps개).</summary>
        public int TrapPower = 25;
        public int MaxTraps = 3;

        /// <summary>깡통 경보장치 (플래그 ALARM_INSTALLED): 방어력 +AlarmPower.</summary>
        public int AlarmPower = 10;

        /// <summary>출입구 보강 (플래그 ENTRANCE_REINFORCED): 방어 상한.</summary>
        public int ReinforcedDefenseMax = 80;

        /// <summary>창문 가림막 (플래그 WINDOW_COVERS): 위협 증가 배율.</summary>
        public float CoveredThreatMultiplier = 0.7f;

        // ── M7-2 후반 압박 (M7-f, 문서 9.0 "자원 압박이 서서히 상승") ──

        /// <summary>겨울(40~52주) 식량 소모 배율. 하루 소모를 소수점까지 누적해 나눠 쓴다 (2 × 1.3 → 주 18).</summary>
        public float WinterFoodMultiplier = 1.3f;

        /// <summary>겨울 주간 난방 연료. 모자라면 NoHeatStress.</summary>
        public int WinterWeeklyFuel = 2;
        public int NoHeatStress = 6;

        /// <summary>주간 정산: 식량이 이 값을 넘으면 넘는 양의 FoodSpoilageRate만큼 상한다 (올림).</summary>
        public int FoodSpoilageThreshold = 60;
        public float FoodSpoilageRate = 0.2f;

        // ── M6 주인공 감염 (문서 3.1.1): 쓰러지면 물릴 수 있다. 응급 처치 성공 시 감염이 휴면 ──
        public float BiteChanceOnDownDay = 0.25f;
        public float BiteChanceOnDownNight = 0.3f; // M7-2: 0.5 → 0.3 (밤만 전략 감염 사망 과다)
        public int BiteInfection = 30;
        public int BiteInfectionTreated = 8;
        public float FirstAidBaseChance = 0.15f;
        public float FirstAidPerMedicineLevel = 0.1f;
        public int InfectionWeeklyGrowth = 5; // M7-2: 6 → 5
        public int InfectionGrowthReducePerMedicineLevel = 1;
        public int InfectionGrowthMin = 2;

        // ── M6 선생님 재감염 (문서 5.3.1, 7.2) ──
        public int TeacherInfectionWeekly = 6;
        public int TeacherInfectionReducePerMedicineLevel = 1;
        public int TeacherInfectionMinWeekly = 2;
        public int TeacherInfectionColdBonus = 2;
        public int TeacherStageSize = 25;

        /// <summary>병세 단계마다 선생님 지도 보정(1 초과분)이 줄어드는 비율.</summary>
        public float MentorDecayPerStage = 0.2f;

        // ── M6 날씨 (계절별 가중치: 맑음·비·폭염·한파) ──
        public WeatherOdds[] WeatherBySeason =
        {
            new WeatherOdds(Season.Spring, 6, 3, 0, 1),
            new WeatherOdds(Season.Summer, 5, 4, 2, 0),
            new WeatherOdds(Season.Autumn, 6, 3, 0, 1),
            new WeatherOdds(Season.Winter, 4, 1, 0, 5),
        };

        public float RainNoiseBonus = -0.25f;
        public float RainRaidMultiplier = 0.6f;
        public int HeatExtraWater = 1;
        public int ColdDailyStress = 2;

        // ── 정신 붕괴 (M5 배드 엔딩): 주간 정산 때 스트레스가 이 값 이상인 주가 연속 CollapseWeeks면 종료 ──
        public int CollapseStress = 100;
        public int CollapseWeeks = 3;

        // ── 휴식 ──
        public string DayRestActionId = "ACT_REST_DAY";
        public string NightRestActionId = "ACT_REST_NIGHT";

        /// <summary>새벽 귀환 후 다음 휴식의 회복 배율.</summary>
        public float ReducedRestMultiplier = 0.5f;

        /// <summary>쓰러짐 후 강제 요양 슬롯 수 (문서 6.3.6: 2일 = 4슬롯).</summary>
        public int DownLockedRestSlots = 4;

        // ── 파생 수치 (문서 5.1.2) ──
        public float CombatStrength = 0.4f;
        public float CombatAgility = 0.3f;
        public float CombatStamina = 0.1f;
        public float CombatPerSelfDefenseLevel = 8f;
        public float StressThresholdBase = 50f;
        public float StressThresholdPerWillpower = 0.3f;
        public float StressThresholdPerMindLevel = 5f;
        public float StressThresholdCap = 90f;

        // ── 실시간 탐험 (M2b, 문서 6.3.7) ──
        public float WalkSpeed = 2.2f;
        public float RunSpeed = 4f;
        public float CrouchSpeed = 1.2f;
        public float BaseCarryWeight = 8f;
        public float CarryWeightPerStrength = 0.1f;

        /// <summary>적재 한도를 넘었을 때 이동 속도 배율.</summary>
        public float OverweightSpeedMultiplier = 0.6f;

        public float SearchSpeedPerCraft = 0.003f;
        public float MaxSearchSpeedBonus = 0.3f;
        public int BackpackWidth = 6;
        public int BackpackHeight = 4;

        /// <summary>시계가 끝날 때까지 나오지 못했을 때 추가 피로·스트레스 (M2b-1 임시 규칙, M2b-2에서 황혼 각성으로 대체).</summary>
        public int StrandedFatigue = 10;
        public int StrandedStress = 10;

        // ── 탐험 위협 (M2b-2, 문서 6.3.2·6.3.5) ──
        /// <summary>소음 반경 (유닛): 웅크려 걷기 / 걷기 / 달리기 / 문 / 계단 / 수색(0.5초마다) / 버리기 / 밀치기.</summary>
        public float CrouchNoise = 0.8f;
        public float WalkNoise = 4f;
        public float RunNoise = 11f;
        public float DoorNoise = 5.5f;
        public float StairsNoise = 4.5f;
        public float SearchNoise = 3.5f;
        public float DropNoise = 3f;
        public float ShoveNoise = 7f;

        /// <summary>과적 시 이동 소음 배율 (금속 덜컹거림).</summary>
        public float OverweightNoiseMultiplier = 1.5f;

        /// <summary>군집 각성 게이지: 임계값, 초당 감소, 소음 반경 1유닛당 증가, 근처 체류 초당 증가와 거리, 연쇄 각성 반경.</summary>
        public float StirThreshold = 100f;
        public float StirDecayPerSecond = 3f;
        public float StirPerNoiseUnit = 14f;
        public float PresenceStirPerSecond = 7f;
        public float PresenceRange = 4f;
        public float ChainWakeRadius = 12f;

        public float SearchDuration = 6f;
        public float PatrolRange = 4f;
        public float AwarenessDecayPerSecond = 0.5f;
        public float CrouchSightMultiplier = 0.6f;

        /// <summary>이 거리 안에서 쫓아오는 좀비가 보고 있으면 숨을 수 없다.</summary>
        public float HideBlockDistance = 4f;

        public float ShoveRange = 1.3f;
        public float ShoveStunSeconds = 2.5f;
        public float ShovePush = 1.5f;
        public float ShoveCooldown = 1.5f;

        /// <summary>쓰러졌을 때 허브로 돌아가는 HP (중상, 사망 아님).</summary>
        public int DownRemainingHp = 15;

        /// <summary>활동성 배율 (문서 6.3.2-(2) 표). 인덱스 = ClockPhase (오전, 오후, 황혼, 초저녁, 한밤, 새벽).</summary>
        public float[] ActivitySight = { 0.7f, 0.85f, 1f, 1.1f, 1.3f, 1f };
        public float[] ActivityHearing = { 0.7f, 0.9f, 1.1f, 1.2f, 1.4f, 1f };
        public float[] ActivitySpeed = { 0.8f, 0.9f, 1f, 1.1f, 1.25f, 1f };
        public float[] ActivityStir = { 0.8f, 1.2f, 2f, 2f, 2f, 1f };

        /// <summary>황혼 각성 이후 (낮 시계 종료 후 체류) 배율.</summary>
        public float AwakenedSight = 1.3f;
        public float AwakenedHearing = 1.4f;
        public float AwakenedSpeed = 1.25f;

        // ── 밤·조명 (M2b-3, 문서 6.3.5-(2)) ──
        /// <summary>밤 전체 조명 밝기 (0~1), 새벽 이후 밝기.</summary>
        public float NightAmbientIntensity = 0.12f;
        public float DawnAmbientIntensity = 0.45f;

        /// <summary>어둠 속에서 손전등을 끈 플레이어가 좀비 눈에 띄는 정도 (시야 배율).</summary>
        public float DarknessVisibility = 0.6f;

        /// <summary>손전등을 켠 플레이어의 노출 (시야 배율). 시야 확대 vs 노출 증가의 트레이드오프.</summary>
        public float FlashlightExposure = 1.5f;

        /// <summary>어둠 속 손전등 없이 수색하면 걸리는 시간 배율.</summary>
        public float DarkSearchMultiplier = 1.6f;

        /// <summary>어둠 속에서 플레이어가 맨눈으로 알아볼 수 있는 거리 (이 밖의 좀비·NPC는 화면에 그리지 않는다).</summary>
        public float NightViewDistance = 3.5f;

        /// <summary>좀비·잠든 군집이 소음을 듣는 반경 배율 (파밍·이동 소음에 더 민감하게).</summary>
        public float ZombieHearingBonus = 1.4f;

        /// <summary>벽 너머(방 ↔ 복도)로 전해지는 소음 반경 배율.</summary>
        public float WallNoiseFactor = 0.6f;

        /// <summary>문틈으로 살필 때 보이는 거리 (어둠 속에서는 NightViewDistance).</summary>
        public float PeekRange = 7f;

        /// <summary>문틈으로 살필 때 안쪽 좀비에게 들킬 정도 (시야 배율).</summary>
        public float PeekVisibility = 0.35f;

        /// <summary>문에 귀 기울이는 데 걸리는 시간, 잠든 숨소리가 들리는 거리.</summary>
        public float ListenSeconds = 1.2f;
        public float ListenBreathRange = 8f;

        /// <summary>손전등이 비추는 방향으로 보이는 거리.</summary>
        public float FlashlightViewDistance = 9f;
    }
}
