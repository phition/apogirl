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
    }
}
