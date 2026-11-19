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
