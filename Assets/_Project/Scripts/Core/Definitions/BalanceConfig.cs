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

    /// <summary>
    /// 계절 재해 (확장 6): 정해진 주 사이에 그 날씨가 되면 재해다. 그 주에는 날씨가 ForceChance 확률로 이 날씨로 바뀐다.
    /// 재해는 깊은 곳이 있는 지역의 지도를 바꾼다 (DisasterRules.Apply).
    /// </summary>
    [Serializable]
    public sealed class DisasterDef
    {
        public string Id = "";
        public int FromWeek;
        public int ToWeek;
        public WeatherType Weather;
        public float ForceChance;
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
        public int StartingCleanliness = 30;
        public int CleanlinessWeeklyDecay = 15;

        public List<ResourceAmount> InitialResources = new List<ResourceAmount>
        {
            new ResourceAmount(ResourceType.Food, 14),
            new ResourceAmount(ResourceType.Water, 20),
            new ResourceAmount(ResourceType.Medicine, 2),
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

        /// <summary>1일의 요일 (0 = 월 … 6 = 일). 3월 16일 월요일, 프롤로그 "월요일 아침"과 맞춘다 (계획 보드 요일 표시).</summary>
        public int StartWeekday = 0;

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

        /// <summary>배낭 없이 나갈 때의 주머니 격자. 세로 3칸이라 1×3 야구방망이가 들어간다(튜토리얼 창고 탈출, 2026-11-23).</summary>
        public int PocketWidth = 2;
        public int PocketHeight = 3;

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

        // ── 식사 (사용자 결정 2026-11-23: 주간 식단, 한 사람당 소비) ──

        /// <summary>식단별 한 사람 하루 소비 (식량·물 각각): 아껴 먹는다 / 챙겨 먹는다 / 잘 먹는다. 한 주 실행 직전 배분 화면에서 고른다.</summary>
        public int DietSaveUnits = 1;
        public int DietCareUnits = 2;
        public int DietWellUnits = 3;

        /// <summary>식량 1·물 1이 채우는 포만·수분 (한 사람 몫). 아침마다 먹는다.</summary>
        public int FoodSatietyPerUnit = 14;
        public int WaterHydrationPerUnit = 15;

        /// <summary>매일 밤이 시작되기 전에 빠지는 포만·수분 (주인공·동료·선생님 모두).</summary>
        public int SatietyDailyDrain = 8;
        public int HydrationDailyDrain = 8;

        /// <summary>식량이나 물이 모자라 식단을 못 지킨 날, 주인공·동료마다 오르는 스트레스 (이 범위에서 무작위).</summary>
        public int DietShortStressMin = 2;
        public int DietShortStressMax = 5;

        /// <summary>주인공 탐험 한 번의 포만·수분 소모 (챙겨 먹으면 탐험을 다녀도 유지되게, 2026-11-23).</summary>
        public int ExpeditionSatietyDrain = 5;
        public int ExpeditionHydrationDrain = 6;

        /// <summary>허기(포만 0 아래) 1당 최대 체력, 갈증(수분 0 아래) 1당 탐험 최대 스태미너가 줄어드는 비율. 최대치의 하한.</summary>
        public float HungerMaxHpPenalty = 0.01f;
        public float ThirstStaminaPenalty = 0.01f;
        public float NourishMaxFloor = 0.5f;

        /// <summary>이 값 이하면 굶주림·탈수로 쓰러진 것으로 친다 (사망 원인 판정).</summary>
        public int StarvationThreshold = -60;

        /// <summary>처음 요리를 완성하면 켜지는 플래그 (첫 요리 장면 EVT_SYS_FIRST_DISH).</summary>
        public string FirstDishFlag = "COOKED_DISH";

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

        // ── 관계 단계 (기획 C절) ──

        /// <summary>관계 단계 경계: 이 값 이상이면 다음 단계 (0 미만 서먹 · 0~ 데면데면 · 20~ 편함 · 50~ 가까움 · 80~ 믿음).</summary>
        public int[] BondTierMins = { 0, 20, 50, 80 };

        /// <summary>C.2 ① 역할 효율: 관계 단계별로 역할 효과에 곱한다 (서먹 ×0.5, 가까움 ×1.25, 믿음 ×1.5).</summary>
        public float[] BondRoleMultipliers = { 0.5f, 1f, 1f, 1.25f, 1.5f };

        /// <summary>C.1 윤 선생님 호감 구간: 선생님이 가르치는 교육 행동에만 (높은 구간 하나만 적용, 병세 단계만큼 약해짐).</summary>
        public TeacherBondBand[] TeacherBondBands =
        {
            new TeacherBondBand { MinAffinity = 30, CheckBonus = 0.05f, XpMultiplier = 1.1f },
            new TeacherBondBand { MinAffinity = 60, CheckBonus = 0.10f, XpMultiplier = 1.2f },
            new TeacherBondBand { MinAffinity = 80, CheckBonus = 0.15f, XpMultiplier = 1.3f, GreatSuccessBonus = 0.05f },
        };

        /// <summary>생활기록부 선생님의 한마디: 이 호감 미만이면 딱딱한 말투, TeacherCloseNoteAffinity 이상이면 다정한 맺음말.</summary>
        public int TeacherFormalNoteBelow = 30;
        public int TeacherCloseNoteAffinity = 80;

        // C.5 호감이 떨어지는 경우 (BondDecayRules). 떨어진 뒤 처음 배분하면 호감이 배로 오른다.
        public int NeglectWeeks = 6;
        public int NeglectAffinity = 2;
        public int NeglectFloor = 50;
        public string GuardRoleId = "GUARD";
        public string ScoutRoleId = "SCOUT";
        public int InjuredGuardAffinity = 3;
        public int NurseUntreatedAffinity = 6;
        public int SeniorEmptyRuns = 2;
        public int SeniorEmptyAffinity = 3;
        public int NoLessonWeeks = 3;
        public int NoLessonAffinity = 3;

        /// <summary>C.4 ①: 이벤트 효과로 오르는 동료 호감 배율 (선생님·떨어질 때·배분은 제외).</summary>
        public float CompanionAffinityGainScale = 2f;

        // ── 동료 몸 상태·이번 주 배분 (기획 D절) ──

        /// <summary>다친 동료의 하루 스트레스, 주마다 저절로 풀리는 스트레스.</summary>
        public int CompanionInjuredDailyStress = 1;
        public int CompanionWeeklyStressRecovery = 6;

        /// <summary>스트레스가 이 이상인 동료는 역할 효과가 배율만큼 준다.</summary>
        public int CompanionStressRoleThreshold = 70;
        public float CompanionStressedRoleMultiplier = 0.5f;

        /// <summary>역할 재기획 (Docs/ROLE_ROOMS_PROPOSAL.md): 한 역할에 효과를 받는 사람 수, 넘치면 늦게 붙은 사람은 효과 없음.</summary>
        public int RoleCapacity = 2;

        /// <summary>묶음 안에서 열린 방이 하나 늘 때마다 역할 효과 가산 (1개 ×1, 2개 ×1.25, 3개 ×1.5).</summary>
        public float RoleExtraRoomBonus = 0.25f;

        /// <summary>적성 역할을 맡기면 효과 배율.</summary>
        public float RoleAptitudeMultiplier = 1.25f;

        /// <summary>적성 (3절). 동료 → 잘 맞는 역할.</summary>
        public RoleAptitudeDef[] RoleAptitudes =
        {
            new RoleAptitudeDef { NpcId = "NPC_NURSE", Roles = new[] { "MEDIC" } },
            new RoleAptitudeDef { NpcId = "NPC_DOHYEON", Roles = new[] { "SCOUT" } },
            new RoleAptitudeDef { NpcId = "NPC_BYSTANDER", Roles = new[] { "COMFORT", "STUDY" } },
            new RoleAptitudeDef { NpcId = "NPC_RINGLEADER", Roles = new[] { "HOUSE", "SCOUT" } },
            new RoleAptitudeDef { NpcId = "NPC_SURVIVOR_A", Roles = new[] { "GUARD", "COMFORT" } },
        };

        /// <summary>
        /// 동료 특기 (기획 C.2 ②): 마지막 개인 장면(호감 80)을 보면 깃발 SpecialFlagPrefix+NPC가 켜진다.
        /// 합류해 있고 호감이 SpecialMinAffinity 이상일 때만 효과 (떨어지면 잠시 꺼짐).
        /// </summary>
        public string SpecialFlagPrefix = "SPECIAL_";
        public int SpecialMinAffinity = 80;
        public CompanionSpecialDef[] CompanionSpecials =
        {
            new CompanionSpecialDef { NpcId = "NPC_NURSE", Kind = SpecialKind.MedicineCostDown, Value = 1 },
            new CompanionSpecialDef { NpcId = "NPC_DOHYEON", Kind = SpecialKind.RaidDamageCut, Value = 0.2f },
            new CompanionSpecialDef { NpcId = "NPC_BYSTANDER", Kind = SpecialKind.WeeklyStressRelief, Value = 3 },
            new CompanionSpecialDef { NpcId = "NPC_RINGLEADER", Kind = SpecialKind.TributeCut, Value = 2, Target = "FAC_SAGEORI" },
            new CompanionSpecialDef { NpcId = "NPC_SURVIVOR_A", Kind = SpecialKind.DefenseBonus, Value = 10 },
            new CompanionSpecialDef { NpcId = "NPC_WANDERER", Kind = SpecialKind.DeepExitKnown, Value = 1, Visitor = true },
        };

        // ── 위기 때 남느냐 떠나느냐 (기획 C.2 ③) ──

        /// <summary>위기 주(결산 때 식량·식수가 바닥, 또는 그 주 습격에 뚫림)에 호감이 이 값 미만인 동료가 떠날 후보 (C.5: 0 미만).</summary>
        public int CrisisLeaveBelowAffinity = 0;

        /// <summary>한 주에 예고하는 동료 수 (호감이 낮은 사람부터).</summary>
        public int CrisisLeavePerWeek = 1;

        /// <summary>예고 깃발 접두어 (+NPC ID). 다음 주 이벤트 EVT_CRISIS_LEAVE_{키}가 이 깃발로 열린다.</summary>
        public string CrisisLeaveFlagPrefix = "LEAVE_WARN_";

        /// <summary>습격에 뚫린 밤, 호감이 이 값 이상인 동료가 주인공 앞을 막아설 수 있다.</summary>
        public int RaidCoverMinAffinity = 60;
        public float RaidCoverChance = 0.5f;

        /// <summary>막아섰을 때 주인공이 받는 체력 피해 배율 (0 = 전부 대신 맞음).</summary>
        public float RaidCoverHpScale = 0f;
        public int RaidCoverInjuryWeeks = 2;

        // ── 프롤로그의 메아리 (기획 PROPOSAL_PROLOGUE_ECHOES, 2026-11-23) ──

        /// <summary>성격 F: 이 특성이 있으면 동료 스트레스가 오를 때 그 동료 호감(0~100)에 비례해 덜 오른다. 호감 100이면 −Max.</summary>
        public string CompanionStressReliefTraitId = "PT_F";
        public float CompanionStressReliefMax = 0.4f;

        /// <summary>좋아하는 과목: 프롤로그 과목 → 교육 숙련. 그 숙련 교육을 하면 스트레스 −, 가장 큰 능력치 상승 +.</summary>
        public List<FavoriteSubjectDef> FavoriteSubjects = new List<FavoriteSubjectDef>
        {
            new FavoriteSubjectDef { SubjectId = "KOREAN", Proficiency = ProficiencyType.Humanities },
            new FavoriteSubjectDef { SubjectId = "MATH_SCI", Proficiency = ProficiencyType.Engineering },
            new FavoriteSubjectDef { SubjectId = "PE", Proficiency = ProficiencyType.SelfDefense },
            new FavoriteSubjectDef { SubjectId = "TECH_HOME", Proficiency = ProficiencyType.Cooking },
            new FavoriteSubjectDef { SubjectId = "BIO", Proficiency = ProficiencyType.Medicine },
            new FavoriteSubjectDef { SubjectId = "MIND", Proficiency = ProficiencyType.Mind },
        };

        public int FavoriteSubjectStressRelief = 1;
        public int FavoriteSubjectGainBonus = 1;

        /// <summary>"딱히 없어"(NONE): 이 숙련 단계에 처음 닿은 과목이 좋아하는 과목이 된다 (깃발 FAVORITE_FOUND).</summary>
        public int FavoriteFoundLevel = 3;
        public string FavoriteNoneId = "NONE";
        public string FavoriteFoundFlag = "FAVORITE_FOUND";

        /// <summary>희망 → 의지: 희망이 이 값 이상인 주는 결산 때 의지 +.</summary>
        public int HopeWillpowerThreshold = 70;
        public int HopeWillpowerWeekly = 1;

        /// <summary>의지 → 희망: 희망이 깎일 때 의지 1당 이만큼 덜 깎인다 (상한 Max).</summary>
        public float WillpowerHopeGuardPerPoint = 0.004f;
        public float WillpowerHopeGuardMax = 0.4f;

        /// <summary>희망이 이 값 이상이면: 위기 이탈 예고가 확률로 비켜 가고, 설득 판정(UsesHope)에 보너스.</summary>
        public int CrisisHopeThreshold = 60;
        public float CrisisHopeSkipChance = 0.35f;
        public float CrisisHopePersuadeBonus = 0.2f;

        /// <summary>희망 덕에 설득이 통했을 때 켜는 깃발 접두어 (+NPC ID). 허브 말풍선 "희망이 느껴져".</summary>
        public string HopeHeldFlagPrefix = "HOPE_HELD_";

        /// <summary>함께한 시간 (역할 재기획 4절): 주인공이 방 스케줄을 하면 그 방 동료 호감 +1 (적성 역할이면 + 보너스), 한 사람당 주 상한.</summary>
        public int TogetherAffinity = 1;
        public int TogetherAptitudeBonus = 1;
        public int TogetherWeeklyCap = 2;
        public int TogetherAptitudeWeeklyCap = 3;

        /// <summary>윤 선생님: 선생님이 가르치는 수업에서만 (아프면 TeacherCareActionId에서만), 주 상한.</summary>
        public int TeacherTogetherAffinity = 1;
        public int TeacherTogetherWeeklyCap = 1;
        public string TeacherCareActionId = "ACT_CARE_TEACHER";
        public string[] TeacherIllFlags = { "TEACHER_REINFECTED", "TEACHER_BEDRIDDEN", "TEACHER_ISOLATED" };
        public string[] TeacherAbsentFlags = { "TEACHER_LEFT_BEHIND" };

        /// <summary>동료 탐험 (역할 재기획 5절): 주 1번(확률로 2번), 정찰 역할은 늘 2번. 같은 날 낮에 많아야 OutingDailyCap명.</summary>
        public float OutingExtraChance = 0.7f;
        public int OutingScoutCount = 2;
        public int OutingDailyCap = 2;

        /// <summary>외출하지 않는 인물 (고양이 등).</summary>
        public string[] OutingExcludedNpcs = { "NPC_CAT" };

        /// <summary>물자: 외출 한 번에 한 종류, 양은 그 지역 주인공 낮 산출 × 비율 (정찰 적성이면 + 보너스). 갈 곳은 가장 모자란 자원을 주는 곳에 가중치.</summary>
        public float OutingYieldShare = 0.7f;   // 2026-11-23 밸런스 표: 주인공의 70% (예전 30%)
        public float OutingScoutAptitudeBonus = 0.3f;

        /// <summary>수확 종류 비중 (2026-11-22): 식량·물 각 30%, 자재 20%, 의약·연료·전력 각 5%, 나머지 5%는 빈손.</summary>
        public OutingHaulDef[] OutingHaul =
        {
            new OutingHaulDef { Resource = ResourceType.Food, Weight = 0.30f, FallbackMin = 6, FallbackMax = 12 },
            new OutingHaulDef { Resource = ResourceType.Water, Weight = 0.30f, FallbackMin = 4, FallbackMax = 10 },
            new OutingHaulDef { Resource = ResourceType.Materials, Weight = 0.20f, FallbackMin = 1, FallbackMax = 3 },
            new OutingHaulDef { Resource = ResourceType.Medicine, Weight = 0.05f, FallbackMin = 1, FallbackMax = 3 },
            new OutingHaulDef { Resource = ResourceType.Fuel, Weight = 0.05f, FallbackMin = 1, FallbackMax = 3 },
            new OutingHaulDef { Resource = ResourceType.Battery, Weight = 0.05f, FallbackMin = 1, FallbackMax = 2 },
        };
        public float OutingLackWeight = 3f;
        public float OutingLikedChance = 0.1f;

        /// <summary>나갈 때 스트레스·포만·수분.</summary>
        public int OutingStress = 8;
        public int OutingSatietyDrain = 20;
        public int OutingHydrationDrain = 25;

        /// <summary>다침: 확률(정찰 역할은 낮게), 그 가운데 "부상" 비율. 가벼운 상처는 1주(다음 주 결산에 낫는다).</summary>
        public float OutingInjuryChance = 0.25f;
        public float OutingScoutInjuryChance = 0.15f;
        public float OutingSevereShare = 0.2f;
        public int OutingSevereWeeks = 2;
        public int OutingMinorStress = 5;
        public int OutingSevereStress = 15;

        /// <summary>"이번 주는 남아 줘"를 2주 연속 켜면 답답해 스트레스.</summary>
        public int OutingHeldBackStress = 3;

        /// <summary>배분: 음식(식량 1)·마실 것(식수 1)·의약품(의약품 1)·기호품(창고 물건 1). 한 사람에게 종류별 주 1회.</summary>
        public int GiveFoodSatiety = 45;
        public int GiveFoodStress = -3;
        public int GiveWaterHydration = 55;
        public int GiveWaterStress = -2;
        public int GiveMedicineInjuryWeeks = 1;
        public int GiveMedicineStress = -5;
        public int GiveAffinityBasic = 2;
        public int GiveAffinityMedicineInjured = 4;
        public int GiveAffinityLiked = 6;
        public int GiveLikedStress = -10;
        public int GiveOtherGiftStress = -3;

        /// <summary>같은 사람에게 같은 물건을 이 주 수 안에 다시 주면 호감·스트레스 효과가 반으로 준다.</summary>
        public int GiftRepeatWeeks = 2;

        /// <summary>허브 한 줄 멘트: 호감이 이 이상인 동료가 n번에 1번꼴로 대신 말한다 (경고·실행 중에는 선생님).</summary>
        public int HubCompanionLineAffinity = 50;
        public int HubCompanionLineEvery = 4;

        /// <summary>헤드리스(자동) 탐험에서 그 지역 전리품 표에 있는 기호품 하나를 줍는 확률 (낮·밤).</summary>
        public float HeadlessLikedItemDayChance = 0.3f;
        public float HeadlessLikedItemNightChance = 0.15f;

        /// <summary>헤드리스(자동) 탐험에서 옥상이 있는 지역(학교)의 옥상까지 올라가 보는 확률 (옥상 플래그 → 거점 옥상 해금).</summary>
        public float HeadlessRooftopChance = 0.5f;

        /// <summary>헤드리스(자동) 탐험에서 그 지역의 CD(줍는 조우)를 찾아 챙기는 확률 (기획 A.3, 휴대폰에서도 모을 수 있게).</summary>
        public float HeadlessCdChance = 0.3f;

        // ── 헤드리스 장비 (제작 C5) ──

        /// <summary>헤드리스 쓰러짐 확률 × (1 − 방어구 피해 감소율 × 이 값).</summary>
        public float HeadlessArmorDownFactor = 0.15f;

        /// <summary>헤드리스 쓰러짐 확률에서 찬 무기 위력 1마다 빼는 값.</summary>
        public float HeadlessWeaponDownPerPower = 0.01f;

        /// <summary>헤드리스 밤 탐험에 손전등을 들면 쓰러짐 확률에서 빼는 값.</summary>
        public float HeadlessFlashlightNightDown = 0.02f;

        /// <summary>헤드리스 자원 수확의 기준 가방 칸 수 (학교 가방 5×4). 가방이 없어도 이 칸 수로 본다.</summary>
        public int HeadlessBagBaseCells = 20;

        /// <summary>가방 칸이 기준보다 많은(적은) 칸마다 헤드리스 자원 수확에 더하는(빼는) 비율.</summary>
        public float HeadlessBagYieldPerCell = 0.012f;

        // ── 제작 (Docs/CRAFTING_PROPOSAL.md) ──

        /// <summary>여러 칸 제작의 뒤 칸을 잠그는 공용 행동 ("제작 중").</summary>
        public string CraftContinueActionId = "ACT_CRAFT_CONTINUE";

        /// <summary>"제작 중" 칸 하나의 피로·스트레스.</summary>
        public int CraftContinueFatigue = 7;
        public int CraftContinueStress = 1;

        /// <summary>완성 판정에 실패하면 돌려받는 자원 재료 비율 (내림). 앞 단계 물건은 모두 돌려받는다.</summary>
        public float CraftFailRefund = 0.5f;

        /// <summary>완성 판정이 대성공이면: 무기 내구 배율, 무기가 아닌 물건은 돌려받는 자재.</summary>
        public float CraftFineDurability = 1.25f;
        public int CraftFineRefund = 1;

        /// <summary>정전(발전기 고장) 중에 밤 칸에서 일한(착수했거나 이어 한) 제작의 완성 판정 −.</summary>
        public float BlackoutCraftPenalty = 0.10f;

        /// <summary>작업대 Ⅱ(바이스·공구판)를 달면 켜지는 깃발: 3단계 물건 해금.</summary>
        public string WorkshopFlag = "WORKBENCH_2";

        /// <summary>작업대 Ⅱ: 모든 제작 판정 +.</summary>
        public float WorkshopCheckBonus = 0.05f;

        /// <summary>무기 손질: 최대 내구의 이 비율만큼 채운다 (상한 = 최대치).</summary>
        public float WeaponMaintainRestore = 0.5f;

        /// <summary>정수 줄이 강화하는 정수 행동.</summary>
        public string PurifyActionId = "ACT_SURV_PURIFY_WATER";

        /// <summary>모래 필터: 주간 정산 때 저절로 거르는 빗물, 그 비율. 정수 행동이 실패해도 남는 비율.</summary>
        public int SandFilterWeeklyRaw = 4;
        public float SandFilterRate = 0.75f;
        public float SandFilterFailRate = 0.7f;

        /// <summary>끓임 정수대: 정수 한 번에 거르는 빗물, 드는 연료, 판정 + (연료가 없으면 예전대로).</summary>
        public int BoilStationPurify = 18;
        public int BoilStationFuel = 1;
        public float BoilStationCheckBonus = 0.1f;

        /// <summary>자동 정수기 (전력 1): 매일 아침 식사 전에 거르는 빗물, 그 비율.</summary>
        public int AutoPurifyDaily = 3;
        public float AutoPurifyRate = 0.9f;

        // ── 허수아비 (제작 P6) ──

        /// <summary>교문 허수아비가 서 있다 / 울리는 허수아비로 바꿔 세웠다 (레시피가 켠다, 쓰러지면 둘 다 꺼진다).</summary>
        public string ScarecrowFlag = "SCARECROW_UP";
        public string ScarecrowBellFlag = "SCARECROW_BELL";

        /// <summary>교문 허수아비: 내구(막은 습격마다 −1, 뚫리면 0), 밤 습격 세기 −.</summary>
        public int ScarecrowDurability = 3;
        public int ScarecrowRaidRelief = 12;

        /// <summary>울리는 교문 허수아비: 내구, 습격 세기 −, 뚫렸을 때 식량 피해 배율 (깡통 경보와 함께 울린다).</summary>
        public int ScarecrowBellDurability = 4;
        public int ScarecrowBellRaidRelief = 18;
        public float ScarecrowBellBreachFoodScale = 0.8f;

        /// <summary>세운 허수아비에 붙은 좀비 하나가 초당 깎는 체력.</summary>
        public float DecoyBiteDps = 1f;

        /// <summary>허수아비에 붙은 좀비가 주인공을 보는 거리 배율.</summary>
        public float DecoyCaughtSightMultiplier = 0.5f;

        /// <summary>소리를 따라온 좀비가 이 거리 안에 허수아비가 있으면 붙는다.</summary>
        public float DecoyLatchDistance = 1.5f;

        /// <summary>헤드리스 밤 탐험에 허수아비를 챙기면 쓰러짐 확률에서 빼는 값 (하나를 쓴다).</summary>
        public float HeadlessDecoyNightDown = 0.04f;

        // ── 방과후 동아리 (확장 7) ──

        /// <summary>동아리를 같이 한 사람의 호감.</summary>
        public int ClubAffinity = 4;

        /// <summary>그 사람이 좋아하는 활동이었을 때의 호감.</summary>
        public int ClubLikedAffinity = 7;

        /// <summary>좋아하는 활동을 이만큼 같이 하면 그 사람의 동아리 개인 장면이 열린다.</summary>
        public int ClubSceneCount = 3;

        /// <summary>동료마다 좋아하는 활동.</summary>
        public ClubLikeDef[] ClubLikes =
        {
            new ClubLikeDef { NpcId = "NPC_NURSE", Activity = "STUDY" },
            new ClubLikeDef { NpcId = "NPC_DOHYEON", Activity = "SPORTS" },
            new ClubLikeDef { NpcId = "NPC_RINGLEADER", Activity = "SPORTS" },
            new ClubLikeDef { NpcId = "NPC_BYSTANDER", Activity = "SINGING" },
            new ClubLikeDef { NpcId = "NPC_SURVIVOR_A", Activity = "SINGING" },
            new ClubLikeDef { NpcId = "NPC_WANDERER", Activity = "BOARDGAME" },
        };

        // ── 전력 = 유지력 (2026-11-22) ──

        /// <summary>용량 +1에 드는 배터리 (공학 숙련 0).</summary>
        public int PowerUpgradeBatteries = 5;

        /// <summary>공학 숙련 한 단계마다 줄어드는 배터리.</summary>
        public int PowerUpgradeBatteriesPerLevel = 1;

        /// <summary>아무리 숙련이 높아도 이만큼은 든다.</summary>
        public int PowerUpgradeMinBatteries = 2;

        /// <summary>한 번에 오르는 용량.</summary>
        public int PowerUpgradeAmount = 1;

        /// <summary>발전 용량 상한.</summary>
        public int PowerMaxCapacity = 6;

        /// <summary>전기 기기. 용량이 모자라면 이 순서대로 켜진다. 전자 편의기기는 여기에 더한다.</summary>
        public ApplianceDef[] Appliances =
        {
            new ApplianceDef { Id = "FRIDGE", Power = 1, Flag = "SEARCHED_HOMEEC", FoodSpoilageBonus = 30 },
            new ApplianceDef { Id = "PURIFIER", Power = 1, Flag = "AUTO_PURIFIER" },   // 자동 정수기 (제작 P5): 냉장고 다음 차례
        };

        // ── 방송반 송출 (확장 5) ──

        /// <summary>방송 한 번에 누군가 응답할 확률 (응답 장면 EVT_BROADCAST_REPLY_*).</summary>
        public float BroadcastReplyChance = 0.3f;

        /// <summary>방송한 주의 밤 습격 확률 배율 (위치가 알려진다).</summary>
        public float BroadcastRaidMultiplier = 1.2f;

        /// <summary>방송을 듣고 찾아올 수 있는 세력. 이 세력과 휴전(중립 이상)이거나 사라졌으면 위험이 없다.</summary>
        public string BroadcastRiskFaction = "FAC_SAGEORI";

        // ── 고양이 '급식' (확장 2) ──

        /// <summary>눌러앉은 고양이가 주마다 먹는 식량 (모자라면 먹지 않는다: 알아서 쥐를 잡는다).</summary>
        public int CatFoodPerWeek = 1;

        /// <summary>주인공 스트레스가 이만큼 이상이면 주 결산 때 고양이가 무릎에 올라온다.</summary>
        public int CatComfortStressMin = 60;

        /// <summary>무릎에 올라왔을 때 스트레스 감소.</summary>
        public int CatComfortStress = 4;

        /// <summary>습격이 뚫렸을 때 피해 배율 (고양이가 먼저 털을 세워 경고한다).</summary>
        public float CatRaidDamageScale = 0.85f;

        // ── 탐험의 깊이 (기획 B) ──

        /// <summary>헤드리스(자동) 탐험에서 깊은 곳까지 들어가 보는 확률 (깊은 곳이 있는 지역, 무사히 돌아온 판).</summary>
        public float HeadlessDeepChance = 0.65f;

        /// <summary>유랑자 특기(깊은 곳 출구를 안다)가 있으면 더하는 확률.</summary>
        public float HeadlessDeepSpecialBonus = 0.35f;

        /// <summary>헤드리스에서 깊은 곳에 들어갔을 때 깊은 곳 전리품 표에서 물건 하나를 줍는 확률.</summary>
        public float HeadlessDeepItemChance = 0.6f;

        /// <summary>헤드리스에서 깊은 곳에 들어갔을 때의 스트레스 (어둡고 길을 잃는다).</summary>
        public int HeadlessDeepStress = 2;

        /// <summary>깊은 곳에서는 소리가 울린다: 소음 반경 배율.</summary>
        public float DeepNoiseMultiplier = 1.2f;

        /// <summary>깊은 곳 밝기 (늘 어둡다, 1 = 복도 기본).</summary>
        public float DeepAmbient = 0.22f;

        /// <summary>일방 출구: 한 번 누르면 "정말 나갈까?", 이 시간 안에 다시 누르면 나간다 (초).</summary>
        public float OneWayConfirmSeconds = 3f;

        /// <summary>깊은 곳 큰 문(금고문·방화 셔터)을 밀어 여는 시간 (초, 길게 누른다).</summary>
        public float HeavyDoorSeconds = 4f;

        /// <summary>쇠지렛대가 있으면 큰 문 여는 시간 배율.</summary>
        public float HeavyDoorCrowbarMultiplier = 0.5f;

        /// <summary>큰 문을 미는 동안 0.5초마다 나는 소음 반경.</summary>
        public float HeavyDoorNoise = 5f;

        /// <summary>큰 문이 열리는 순간 쿵 하는 소음 반경.</summary>
        public float HeavyDoorSlamNoise = 9f;

        /// <summary>깊은 곳 빗장을 푸는 시간 (초, 길게 누른다).</summary>
        public float BarredDoorSeconds = 2f;

        /// <summary>빗장을 푸는 동안 0.5초마다 나는 소음 반경 (조용하다).</summary>
        public float BarredDoorNoise = 1.5f;

        /// <summary>헤드리스에서 깊은 곳에 들어간 판이 특별한 방(큰 문·빗장) 하나를 여는 확률.</summary>
        public float HeadlessDeepRoomChance = 0.5f;

        /// <summary>헤드리스에서 특별한 방을 열었을 때 그 방 전리품 표에서 줍는 물건 수.</summary>
        public int HeadlessDeepRoomItems = 2;

        /// <summary>헤드리스에서 특별한 방을 열었을 때 더하는 스트레스.</summary>
        public int HeadlessDeepRoomStress = 1;

        /// <summary>헤드리스에서 특별한 방 안의 조우(CD·단서)를 찾는 확률 (특별한 방을 연 판만, 조우마다).</summary>
        public float HeadlessDeepRoomEncounterChance = 0.6f;

        /// <summary>좋아하는 물건 (기획 D.4). 기호품 칸에는 이 표에 있는 물건만 고를 수 있다.</summary>
        public LikedItemsDef[] LikedItems =
        {
            new LikedItemsDef { NpcId = "NPC_TEACHER", Items = new[] { "ITEM_PAPERBACK", "ITEM_PEN" } },
            new LikedItemsDef { NpcId = "NPC_NURSE", Items = new[] { "ITEM_COFFEE_MIX", "ITEM_HAND_CREAM" } },
            new LikedItemsDef { NpcId = "NPC_DOHYEON", Items = new[] { "ITEM_COMIC", "ITEM_SPORTS_DRINK" } },
            new LikedItemsDef { NpcId = "NPC_BYSTANDER", Items = new[] { "ITEM_KEYRING", "ITEM_STICKERS" } },
            new LikedItemsDef { NpcId = "NPC_RINGLEADER", Items = new[] { "ITEM_HAIR_TIE", "ITEM_LIP_BALM" } },
            new LikedItemsDef { NpcId = "NPC_SURVIVOR_A", Items = new[] { "ITEM_GUITAR_SCORE", "ITEM_GUITAR_STRINGS" } },
            new LikedItemsDef { NpcId = "NPC_WANDERER", Items = new[] { "ITEM_OLD_MAP", "ITEM_CANDY" } },
            new LikedItemsDef { NpcId = "NPC_CAT", Items = new[] { "ITEM_TUNA_CAN", "ITEM_CAT_TREAT" } },
        };

        // ── 난이도 (M7-3: M7-d 배율, M7-e 이야기 중심 편의) ──

        public string DefaultDifficulty = "STANDARD";

        public DifficultyDef[] Difficulties =
        {
            new DifficultyDef
            {
                Id = "STORY", Harm = 0.6f, HumanThreat = 0.7f, Infection = 0.6f, Yield = 1.5f,
                // 식량·물 변환 ×1.5 (통조림·생수 5 → 7.5), 수확·의약품도 넉넉하게 (2026-11-23 밸런스 표)
                FoodYield = 1.5f, WaterYield = 1.5f, MedicineYield = 1.5f,
                WeeklyUndo = true, Dispatch = true, DispatchYield = 0.7f,
            },
            // 표준: 식량·물·의약품 ×1 (2026-11-23 밸런스 표)
            new DifficultyDef { Id = "STANDARD", FoodYield = 1f, WaterYield = 1f, MedicineYield = 1f },
            // 하드: 가혹해진 세계에 맞춰 소모는 표준과 같게 (2026-11-20 결정, M7-d의 ×1.25에서 수정)
            new DifficultyDef
            {
                Id = "HARD", Consumption = 1f, Harm = 1.4f, HumanThreat = 1.3f, Infection = 1.3f, Yield = 0.9f,
                // 식량·물·의약품 ×0.8 (통조림·생수 5 → 4, 2026-11-23 밸런스 표)
                FoodYield = 0.8f, WaterYield = 0.8f, MedicineYield = 0.8f,
            },
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
        public int RainCollectWater = 10;

        /// <summary>매주 학교 물탱크·화장실에서 받아 두는 생활용수 (RawWater, 정수해야 식수).</summary>
        public int WeeklyRawWater = 5;

        /// <summary>옥상 텃밭 (플래그 ROOF_GARDEN): 겨울을 뺀 매주 식량.</summary>
        public int RoofGardenFood = 10;

        // ── 거점 방어 작업 ──

        /// <summary>함정: 습격·침입 1회에 하나를 써서 방어력 +TrapPower (최대 MaxTraps개).</summary>
        public int TrapPower = 25;

        /// <summary>거점을 비운 밤(수학여행)의 습격: 방어도 반영 비율 (주인공 전투력은 빠진다).</summary>
        public float AwayDefenseFactor = 0.5f;
        public int MaxTraps = 3;

        /// <summary>깡통 경보장치 (플래그 ALARM_INSTALLED): 방어력 +AlarmPower.</summary>
        public int AlarmPower = 10;

        /// <summary>출입구 보강 (플래그 ENTRANCE_REINFORCED): 방어 상한.</summary>
        public int ReinforcedDefenseMax = 80;

        /// <summary>창문 가림막 (플래그 WINDOW_COVERS): 위협 증가 배율.</summary>
        public float CoveredThreatMultiplier = 0.7f;

        // ── M7-2 후반 압박 (M7-f, 문서 9.0 "자원 압박이 서서히 상승") ──

        /// <summary>겨울(40~52주) 식량 소모 배율. 무리 하루 소모를 소수점까지 누적해 나눠 쓴다 (2 × 1.3 → 주 18).</summary>
        public float WinterFoodMultiplier = 1.2f;

        /// <summary>겨울 주간 난방 연료. 모자라면 NoHeatStress.</summary>
        public int WinterWeeklyFuel = 2;
        public int NoHeatStress = 4;

        /// <summary>주간 정산: 식량이 이 값을 넘으면 넘는 양의 FoodSpoilageRate만큼 상한다 (올림).</summary>
        public int FoodSpoilageThreshold = 60;
        public float FoodSpoilageRate = 0.18f;

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

        // ── 구조 헬기 (4막 끝, BoardingRules) ──
        /// <summary>헬기 1회 탑승 정원. 넘으면 구조 전날 밤에 한 명을 남기는 선택.</summary>
        public int RescueSeats = 8;

        // ── M6 날씨 (계절별 가중치: 맑음·비·폭염·한파) ──
        public WeatherOdds[] WeatherBySeason =
        {
            new WeatherOdds(Season.Spring, 6, 3, 0, 1),
            new WeatherOdds(Season.Summer, 5, 4, 2, 0),
            new WeatherOdds(Season.Autumn, 6, 3, 0, 1),
            new WeatherOdds(Season.Winter, 4, 1, 0, 5),
        };

        // ── 계절 재해 (확장 6, DisasterRules): 장마(침수)·폭설(진입로 막힘) ──
        public DisasterDef[] Disasters =
        {
            new DisasterDef { Id = "MONSOON", FromWeek = 15, ToWeek = 19, Weather = WeatherType.Rain, ForceChance = 0.75f },
            new DisasterDef { Id = "SNOWSTORM", FromWeek = 42, ToWeek = 46, Weather = WeatherType.Cold, ForceChance = 0.8f },
        };

        /// <summary>장마: 깊은 곳 맨 아래층이 물에 잠긴다. 걸음 배율·소음 배율 (물을 헤치고 걷는다).</summary>
        public float FloodMoveMultiplier = 0.7f;
        public float FloodNoiseMultiplier = 1.5f;

        /// <summary>장마 때 헤드리스에서 깊은 곳에 들어갈 확률 배율 (물이 차서 덜 내려간다).</summary>
        public float MonsoonHeadlessDeepMultiplier = 0.6f;

        public float RainNoiseBonus = -0.25f;
        public float RainRaidMultiplier = 0.6f;
        /// <summary>폭염 날 한 사람당 더 드는 물.</summary>
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

        /// <summary>실시간 탐험 수색 시간 배율 (2026-11-21: 30% 줄임).</summary>
        public float SearchTimeMultiplier = 0.7f;

        /// <summary>스트레스가 이보다 높으면 수색이 느려진다. 100에서 StressSearchMaxExtra만큼 더 걸린다 (짜증 섞인 한숨).</summary>
        public float StressSearchThreshold = 60f;
        public float StressSearchMaxExtra = 0.5f;

        // ── 탐험 스태미너 (달리기·싸움) ──
        /// <summary>최대치 = StaminaBase + 체력 능력치 × StaminaPerStat.</summary>
        public float StaminaBase = 100f;
        public float StaminaPerStat = 0.5f;

        /// <summary>달리는 동안 초당 소모, 쉴 때 초당 회복 (마지막으로 쓴 뒤 StaminaRegenDelay초부터).</summary>
        public float StaminaRunDrain = 16f;
        public float StaminaRegen = 14f;
        public float StaminaRegenDelay = 0.8f;

        /// <summary>바닥나면 숨이 차 이만큼 다시 찰 때까지 달릴 수 없다.</summary>
        public float StaminaRecoverToRun = 25f;
        public float StaminaAttackCost = 15f;
        public float StaminaShoveCost = 12f;

        /// <summary>이번 탐험 동안 깎이는 최대치 (게이지에 붉게): 피로 1당 비율, 부상(INJURED) 비율, 그래도 남는 최소 비율.</summary>
        public float StaminaFatiguePenalty = 0.004f;
        public float StaminaInjuryPenalty = 0.25f;
        public float StaminaMinCapRatio = 0.3f;

        // ── 숨기 (Space 누르고 있기) ──
        /// <summary>숨는 데 걸리는 시간 = HideSeconds / (1 + 민첩 × HideAgilityFactor), 최소 HideMinSeconds.</summary>
        public float HideSeconds = 1.5f;
        public float HideAgilityFactor = 0.01f;
        public float HideMinSeconds = 0.4f;

        /// <summary>수면 부채가 이 이상이면 숨어 있다가 잠들 수 있다: 1초마다 (부채 − 기준 + 1) × HideSleepChancePerDebt.</summary>
        public int HideSleepDebtThreshold = 2;
        public float HideSleepChancePerDebt = 0.015f;

        /// <summary>잠든 동안 탐험 시계가 흐르는 배율.</summary>
        public float HideSleepClockMultiplier = 2f;

        /// <summary>깨어나기: Space 한 번에 오르는 양(1이면 깸), 손을 놓으면 초당 줄어드는 양.</summary>
        public float WakePerPress = 0.12f;
        public float WakeDecay = 0.3f;
        public int BackpackWidth = 6;
        public int BackpackHeight = 4;

        /// <summary>시계가 끝날 때까지 나오지 못했을 때 추가 피로·스트레스 (M2b-1 임시 규칙, M2b-2에서 황혼 각성으로 대체).</summary>
        public int StrandedFatigue = 10;
        public int StrandedStress = 10;

        /// <summary>밤 탐험에서 새벽까지 버텼을 때 추가 피로·스트레스 (수면 부채 +1과 함께, 쓰러짐 제외).</summary>
        public int DawnFatigue = 10;
        public int DawnStress = 10;

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

        /// <summary>
        /// 추격하다 놓친 좀비: 놓친 곳 근처를 서성이는 시간(초), 그 뒤 물러나는 거리(유닛).
        /// 낮(휴면·선잠)에는 조금 물러나 다시 잠들고, 밤에는 멀리 물러나 깨어 있는 채로 배회한다.
        /// </summary>
        public float LostLingerSeconds = 5f;
        public float LostRetreatDay = 6f;
        public float LostRetreatNight = 18f;
        public float PatrolRange = 4f;
        public float AwarenessDecayPerSecond = 0.5f;
        public float CrouchSightMultiplier = 0.6f;

        /// <summary>이 거리 안에서 쫓아오는 좀비가 보고 있으면 숨을 수 없다.</summary>
        public float HideBlockDistance = 4f;

        public float ShoveRange = 1.3f;
        public float ShoveStunSeconds = 2.5f;
        public float ShovePush = 1.5f;
        public float ShoveCooldown = 1.5f;

        /// <summary>
        /// 좀비 손이 닿아 물린 순간: 다침 동작 시간(초) — 그동안 문 좀비는 그 자리에 멈췄다가 다시 다가온다.
        /// 그 뒤 무적 시간(초) 동안 주인공이 깜빡이며 더 물리지 않고 다침 동작도 없다. 깜빡임은 초당 횟수.
        /// </summary>
        public float HurtReactSeconds = 0.4f;
        public float HurtInvulnSeconds = 2f;
        public float HurtBlinkPerSecond = 8f;

        /// <summary>
        /// 적대적 생존자가 좀비에게 물리면 그 자리에 멈춰(기절 별) 깜빡이는 시간(초). 그동안 좀비는 그 사람을 물지 않고 다른 사람(주인공·다른 생존자)을 노린다.
        /// 생존자는 주인공과 사이에 좀비가 있으면 걸어서(좀비를 깨우지 않는다) 다가오고, 없으면 달려온다 (ThreatSimulation).
        /// </summary>
        public float SurvivorBittenSeconds = 3f;

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

        /// <summary>
        /// 낮 공간별 밝기 배율 (M6-5, 그림만 — 시야·수색 규칙은 그대로): 복도는 햇빛 줄기가 살도록 조금 낮추고,
        /// 전기가 끊긴 방 안은 어둡게.
        /// </summary>
        public float DayCorridorAmbient = 0.72f;
        public float DayRoomAmbient = 0.22f;

        /// <summary>복도 햇빛 줄기 밝기·간격(유닛), 방의 복도 쪽 창으로 새어 드는 빛 밝기 (조명 밝기는 선형 값이라 0.15도 꽤 밝다).</summary>
        public float SunbeamIntensity = 2.3f;
        public float SunbeamSpacing = 6f;

        /// <summary>복도 한 층에 드는 빛줄기(햇빛·달빛) 수: 층마다 이 범위에서 정해진다 (자리는 층마다 다르고 늘 같다).</summary>
        public int SunbeamsPerFloorMin = 1;
        public int SunbeamsPerFloorMax = 2;

        /// <summary>
        /// 학교 밖 지역의 바깥 길 (유닛): 진입로(정문 앞, 여기서 시작)와 탈출로(다른 출구 뒤). 어느 쪽이든 길 끝에서 학교로 돌아간다.
        /// </summary>
        public float ApproachRoadWidth = 18f;
        public float EscapeRouteWidth = 14f;
        public float RoomWindowLightIntensity = 0.15f;

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

        /// <summary>낮에 어두운 방 안을 문틈으로 살필 때 보이는 거리 (문 근처만). 밤에는 문틈으로 아무것도 보이지 않는다.</summary>
        public float RoomPeekRange = 2.5f;

        /// <summary>어둠 속 기물이 시야에 들어오고 나갈 때 나타나고 사라지는 빠르기 (초당 불투명도).</summary>
        public float FogFadeSpeed = 4f;

        /// <summary>문틈으로 살필 때 안쪽 좀비에게 들킬 정도 (시야 배율).</summary>
        public float PeekVisibility = 0.35f;

        /// <summary>문에 귀 기울이는 데 걸리는 시간, 잠든 숨소리가 들리는 거리.</summary>
        public float ListenSeconds = 1.2f;
        public float ListenBreathRange = 8f;

        /// <summary>손전등이 비추는 방향으로 보이는 거리.</summary>
        public float FlashlightViewDistance = 9f;
    }
}
