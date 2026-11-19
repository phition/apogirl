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
        public float WalkNoise = 2.5f;
        public float RunNoise = 7f;
        public float DoorNoise = 4f;
        public float StairsNoise = 3f;
        public float SearchNoise = 1.5f;
        public float DropNoise = 3f;
        public float ShoveNoise = 7f;

        /// <summary>과적 시 이동 소음 배율 (금속 덜컹거림).</summary>
        public float OverweightNoiseMultiplier = 1.5f;

        /// <summary>군집 각성 게이지: 임계값, 초당 감소, 소음 반경 1유닛당 증가, 근처 체류 초당 증가와 거리, 연쇄 각성 반경.</summary>
        public float StirThreshold = 100f;
        public float StirDecayPerSecond = 3f;
        public float StirPerNoiseUnit = 9f;
        public float PresenceStirPerSecond = 5f;
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

        /// <summary>손전등이 비추는 방향으로 보이는 거리.</summary>
        public float FlashlightViewDistance = 9f;
    }
}
