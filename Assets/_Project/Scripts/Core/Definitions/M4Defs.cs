using System;
using System.Collections.Generic;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Definitions
{
    // M4: 특성·시설·대사 줄(일기/코멘트)·0막 프롤로그·장비·탐험 조우 정의.

    /// <summary>특성 효과 종류. 값은 배율 가산(0.15 = +15%) 또는 정수 가산.</summary>
    public enum TraitModifierKind
    {
        /// <summary>교육 성장량 배율 가산.</summary>
        EducationGain,

        /// <summary>판정 성공률 가산 (Key = 숙련도 이름, 비면 모든 판정).</summary>
        CheckBonus,

        /// <summary>대성공 비율 가산.</summary>
        GreatSuccessBonus,

        /// <summary>이틀에 한 번 식량 소모 감소량.</summary>
        FoodSaving,

        /// <summary>휴식 회복량 배율 가산.</summary>
        RestRecovery,

        /// <summary>탐험 스트레스 배율 가산 (음수 = 감소).</summary>
        ExpeditionStress,

        // ── 탐험 모드 (ExpeditionRequest.Modifiers로 전달) ──
        SearchSpeed,
        NoiseMultiplier,
        RunSpeed,
        StealthBonus,

        // ── M6 (시설·동료 역할에서 주로 쓴다) ──

        /// <summary>밤 습격 방어력 가산 (정수).</summary>
        RaidGuard,

        /// <summary>주간 감염 증가 감소량 (정수, 주인공).</summary>
        InfectionResist,

        /// <summary>주간 선생님 감염 진행 감소량 (정수).</summary>
        TeacherCare,
    }

    [Serializable]
    public sealed class TraitModifier
    {
        public TraitModifierKind Kind;
        public string Key = "";
        public float Value;
    }

    /// <summary>특성 (동아리 특성, 성격 유형 축). 이름은 UI 키 "trait.{Id}".</summary>
    [Serializable]
    public sealed class TraitDef
    {
        public string Id = "";
        public List<TraitModifier> Modifiers = new List<TraitModifier>();
    }

    /// <summary>거점 시설 (문서 3.3). 레벨은 BaseState.Facilities, 0 = 잠김. 이름은 UI 키 "facility.{Id}".</summary>
    [Serializable]
    public sealed class FacilityDef
    {
        public string Id = "";
        public int MaxLevel = 1;

        /// <summary>M6: 레벨마다 더해지는 보정 (특성과 같은 방식으로 합산).</summary>
        public List<TraitModifier> Modifiers = new List<TraitModifier>();
    }

    /// <summary>M6: 동료 역할 (경비·요리·간호·교사 …). 이름은 UI 키 "role.{Id}". 합류한 동료마다 보정이 더해진다.</summary>
    [Serializable]
    public sealed class RoleDef
    {
        public string Id = "";
        public List<TraitModifier> Modifiers = new List<TraitModifier>();
    }

    [Serializable]
    public sealed class FacilityChange
    {
        public string FacilityId = "";
        public int Level = 1;
    }

    [Serializable]
    public sealed class ProficiencySet
    {
        public ProficiencyType Proficiency;

        /// <summary>최소 이 레벨이 되게 한다 (이미 높으면 그대로).</summary>
        public int Level = 1;
    }

    public enum LineKind
    {
        /// <summary>주간 정산의 일기 한 줄.</summary>
        Diary,

        /// <summary>허브의 선생님 한마디 (계획 단계).</summary>
        TeacherComment,

        // ── M5 엔딩 공통 레이어 (문서 8.3): 구조 엔딩의 후일담 뒤에 하나씩 덧붙는다 ──
        EndingTeacher,
        EndingFriend,
        EndingCure,
        EndingTendency,

        /// <summary>"1년의 기록"의 주요 장면: 조건을 만족하는 줄 전부.</summary>
        EndingMoment,

        /// <summary>M6: 막 평가 카드의 선생님 한마디 (가장 높은 우선순위 하나).</summary>
        ActReview,
    }

    /// <summary>조건으로 고르는 한 줄 (가장 높은 Priority 중 첫 번째). 문장은 UI 키 TextKey.</summary>
    [Serializable]
    public sealed class LineDef
    {
        public string Id = "";
        public LineKind Kind;
        public ConditionDef Condition = new ConditionDef();
        public int Priority;
        public string TextKey = "";
    }

    /// <summary>0막 프롤로그 문항 하나 (Yarn 노드 PROLOGUE_{Id}에서 #choice:선택지ID로 고른다).</summary>
    [Serializable]
    public sealed class PrologueQuestion
    {
        public string Id = "";
        public List<PrologueOption> Options = new List<PrologueOption>();
    }

    [Serializable]
    public sealed class PrologueOption
    {
        public string Id = "";

        /// <summary>새 게임 상태에 적용 (스탯 분배·숨김 수치·특성·숙련도·플래그).</summary>
        public GameEffect Effect = new GameEffect();

        /// <summary>프로필 문자열 칸 (BloodType, GradeBandId, FavoriteSubjectId, ClubId 중 하나에 기록). 비면 기록 안 함.</summary>
        public string ProfileField = "";
        public string ProfileValue = "";
    }

    [Serializable]
    public sealed class PrologueDef
    {
        public List<PrologueQuestion> Questions = new List<PrologueQuestion>();

        /// <summary>모든 문항 공통으로 먼저 적용 (예: 시작 아이템).</summary>
        public GameEffect Common = new GameEffect();
    }

    // ── 장비 (문서 5.6) ──

    public enum EquipSlot
    {
        None,
        Backpack,
        Weapon,
        Tool,
        Armor,
        Consumable,
    }

    public enum ToolKind
    {
        None,
        Flashlight,
        Crowbar,
    }

    /// <summary>탐험 중 사람·스토리 오브젝트와의 조우 (문서 6.3.5-(5)). 문구는 UI 키.</summary>
    [Serializable]
    public sealed class EncounterDef
    {
        public string Id = "";
        public int Floor;

        /// <summary>방 안이면 방 ID (X는 방 로컬), 비면 복도.</summary>
        public string RoomId = "";
        public float X;

        /// <summary>요청 시점의 플래그 조건 (ExpeditionRequest.Flags).</summary>
        public List<string> FlagsAll = new List<string>();
        public List<string> FlagsNone = new List<string>();

        public string PromptKey = "";
        public List<EncounterChoiceDef> Choices = new List<EncounterChoiceDef>();
    }

    [Serializable]
    public sealed class EncounterChoiceDef
    {
        public string Id = "";
        public string TextKey = "";
        public List<string> SetFlags = new List<string>();
        public List<string> AddClues = new List<string>();
        public float StressDelta;

        /// <summary>선택 후 조우 대상이 사라지는가 (데려감·떠남).</summary>
        public bool Removes = true;
    }

    /// <summary>정의 DB의 M4 묶음 (특성·시설·한 줄 대사·프롤로그).</summary>
    public sealed class DefinitionExtras
    {
        public List<TraitDef> Traits = new List<TraitDef>();
        public List<FacilityDef> Facilities = new List<FacilityDef>();
        public List<LineDef> Lines = new List<LineDef>();
        public PrologueDef Prologue = new PrologueDef();

        /// <summary>M5: 엔딩 목록 (등록 순서 = 같은 우선순위·점수일 때의 순서).</summary>
        public List<EndingDef> Endings = new List<EndingDef>();

        /// <summary>M6: 동료 역할.</summary>
        public List<RoleDef> Roles = new List<RoleDef>();
    }
}
