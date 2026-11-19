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

        /// <summary>새 게임 1주차 1일 낮에 고정되는 튜토리얼 런 행동 (없으면 생략).</summary>
        public string TutorialActionId = "ACT_EXP_TUTORIAL";

        /// <summary>새 게임을 시작하면 허브에서 바로 띄우는 이벤트.</summary>
        public string OpeningEventId = "EVT_A1_OPENING";

        /// <summary>밤 습격 (문서 5.5): 이 주차부터, 확률 = 기본 + (위협 − 방어) × 계수, 상한.</summary>
        public int RaidStartWeek = 4;
        public float RaidBaseChance = 0.02f;
        public float RaidChancePerPoint = 0.004f;
        public float RaidMaxChance = 0.25f;

        /// <summary>습격 세기 = 위협 + 0~분산, 방어력 = 전투력 + 방어도 × 배율 + 경계 보정 + 0~분산.</summary>
        public int RaidStrengthVariance = 20;
        public int RaidPowerVariance = 20;
        public float RaidDefenseWeight = 2f;

        /// <summary>거점 기본 방어력 (선생님·지형). 전투력·방어도에 더해진다.</summary>
        public int RaidBasePower = 20;

        /// <summary>습격 결과.</summary>
        public int RaidRepelStress = 6;
        public int RaidRepelDefenseLoss = 1;
        public int RaidBreachStress = 18;
        public int RaidBreachHp = 10;
        public int RaidBreachFood = 3;
        public int RaidBreachDefenseLoss = 3;
        public int RaidThreatRelief = 8;

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
        public int HungerRecovery = 20;
        public int HungerIncrease = 20;
        public int ThirstRecovery = 25;
        public int ThirstIncrease = 40;
        public int StarvationThreshold = 60;
        public int StarvationHpLoss = 5;
        public int DehydrationThreshold = 60;
        public int DehydrationHpLoss = 10;

        // ── M6 주인공 감염 (문서 3.1.1): 쓰러지면 물릴 수 있다. 응급 처치 성공 시 감염이 휴면 ──
        public float BiteChanceOnDownDay = 0.25f;
        public float BiteChanceOnDownNight = 0.5f;
        public int BiteInfection = 30;
        public int BiteInfectionTreated = 8;
        public float FirstAidBaseChance = 0.15f;
        public float FirstAidPerMedicineLevel = 0.1f;
        public int InfectionWeeklyGrowth = 6;
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
            new WeatherOdds(Season.Summer, 4, 4, 3, 0),
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
