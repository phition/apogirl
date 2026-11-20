using System;
using System.Collections.Generic;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Definitions
{
    /// <summary>비교 대상 종류. Key는 종류별 이름(예: Stat → "Intellect", NpcAffinity → "NPC_NURSE").</summary>
    public enum ValueKind
    {
        Stat,
        Proficiency,
        Vital,
        Resource,
        Hidden,
        SleepDebt,
        Conscience,
        TeacherAffinity,
        NpcAffinity,
        NpcTrust,
        EndingTag,
        ItemCount,

        // ── M4 ──
        Facility,
        Defense,
        Threat,
        TeacherMobility,

        /// <summary>특성 보유 (1/0).</summary>
        Trait,

        /// <summary>탐험 횟수 (Key: Day / Night / 비면 전체).</summary>
        Expeditions,

        /// <summary>이 행동을 실행한 횟수 (Key = 행동 ID, 성공·실패 무관).</summary>
        ActionCount,

        // ── M5 파생 수치 (엔딩 판정) ──

        /// <summary>밤 탐험 비율 0~100 (탐험이 없으면 0).</summary>
        NightRatio,

        /// <summary>일기 수 (주간 정산마다 1줄).</summary>
        DiaryCount,

        /// <summary>거점에 합류한 NPC 수 (선생님 제외).</summary>
        Companions,

        /// <summary>능력치 최대 − 최소 (작을수록 고르게 키움).</summary>
        StatSpread,

        /// <summary>가장 낮은 능력치.</summary>
        StatMin,

        /// <summary>스트레스 한계로 버틴 연속 주 수 (정신 붕괴 경고).</summary>
        BreakdownWeeks,

        /// <summary>단서 수 (Key가 있으면 그 접두어로 시작하는 단서만).</summary>
        ClueCount,

        // ── M6 ──

        /// <summary>선생님 감염 진행도 0~100 (재감염 전에는 0).</summary>
        TeacherInfection,

        /// <summary>선생님 병세 단계 (진행도 / TeacherStageSize).</summary>
        TeacherStage,

        /// <summary>구조까지 남은 주 (52 − 현재 주).</summary>
        WeeksToRescue,

        /// <summary>이번 주 날씨가 Key(WeatherType 이름)이면 1.</summary>
        Weather,

        /// <summary>이 역할을 맡은 동료 수 (Key = 역할 ID).</summary>
        RoleCount,

        // ── M7 ──

        /// <summary>플래그 Key가 켜져 있으면 1 (AnyChecks로 "플래그 또는 수치" 조건을 만들 때).</summary>
        Flag,

        /// <summary>주인공이 혈액형 Key(받는 사람)에게 수혈할 수 있으면 1. 주인공 혈액형을 모르면 0 (D16).</summary>
        BloodCompatible,

        /// <summary>설치한 함정 수.</summary>
        Traps,
    }

    public enum CompareOp
    {
        GreaterOrEqual,
        LessOrEqual,
        Equal,
        NotEqual,
        Greater,
        Less,
    }

    public enum PhaseFilter
    {
        Any,
        Day,
        Night,
    }

    /// <summary>수치 비교 하나: (Kind, Key) Op Value.</summary>
    [Serializable]
    public sealed class ValueCheck
    {
        public ValueKind Kind;
        public string Key = "";
        public CompareOp Op;
        public int Value;

        public ValueCheck()
        {
        }

        public ValueCheck(ValueKind kind, string key, CompareOp op, int value)
        {
            Kind = kind;
            Key = key ?? "";
            Op = op;
            Value = value;
        }
    }

    [Serializable]
    public sealed class NpcStatusCheck
    {
        public string NpcId = "";
        public NpcStatus Status;
    }

    /// <summary>
    /// 이벤트·선택지 발생 조건 (문서 부록 A의 trigger). 비어 있는 항목은 검사하지 않는다.
    /// 재귀 구조를 피하려고(Unity 직렬화 깊이 제한) "하나 이상" 조건은 AnyChecks·FlagsAny로만 표현한다.
    /// </summary>
    [Serializable]
    public sealed class ConditionDef
    {
        /// <summary>주차 범위 (0 = 제한 없음).</summary>
        public int WeekMin;
        public int WeekMax;

        /// <summary>요일(1~7) 목록. 비면 아무 날.</summary>
        public List<int> Days = new List<int>();

        public PhaseFilter Phase;
        public List<Season> Seasons = new List<Season>();

        /// <summary>실제 날짜(학사력). 0이면 검사 안 함. 그 날짜가 포함된 날에만 참.</summary>
        public int Month;
        public int DayOfMonth;

        /// <summary>방금 실행한 슬롯의 행동 분류 / ID 제한 (예: 밤 휴식 중 대화).</summary>
        public List<ActionCategory> ActionCategories = new List<ActionCategory>();
        public List<string> ActionIds = new List<string>();

        public List<string> FlagsAll = new List<string>();
        public List<string> FlagsAny = new List<string>();
        public List<string> FlagsNone = new List<string>();
        public List<string> CluesAll = new List<string>();

        /// <summary>모두 만족해야 함.</summary>
        public List<ValueCheck> Checks = new List<ValueCheck>();

        /// <summary>하나 이상 만족해야 함 (비면 검사 안 함).</summary>
        public List<ValueCheck> AnyChecks = new List<ValueCheck>();

        public List<NpcStatusCheck> NpcStatuses = new List<NpcStatusCheck>();

        /// <summary>주인공 생일이 이 날에 포함될 때만 (학사력 생일 이벤트).</summary>
        public bool IsBirthday;
    }
}
