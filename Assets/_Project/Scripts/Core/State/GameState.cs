using System.Collections.Generic;

namespace ApoGirl.Core.State
{
    // M0 초안: 세이브 1개 = GameState 1개. 고정 집합은 enum, 콘텐츠(행동·아이템·시설·NPC)는 문자열 ID.
    // 세부 필드와 규칙은 M1 클래스 다이어그램에서 확정한다.

    /// <summary>게임 전체 상태. JSON 직렬화 대상이며 MonoBehaviour에 의존하지 않는다.</summary>
    public sealed class GameState
    {
        /// <summary>현재 세이브 포맷 버전. 마이그레이션 시 증가시킨다.</summary>
        public const int CurrentSaveVersion = 1;

        public int SaveVersion { get; set; } = CurrentSaveVersion;
        public RandomState Rng { get; set; } = new RandomState();
        public CalendarState Calendar { get; set; } = new CalendarState();
        public PlayerState Player { get; set; } = new PlayerState();
        public MentorState Teacher { get; set; } = new MentorState();
        public FriendState Friend { get; set; } = new FriendState();
        public BaseState Base { get; set; } = new BaseState();
        public List<ItemStack> Storage { get; set; } = new List<ItemStack>();
        public LoadoutState Loadout { get; set; } = new LoadoutState();
        public WeekPlan Plan { get; set; } = new WeekPlan();
        public HashSet<string> Flags { get; set; } = new HashSet<string>();
        public HashSet<string> Clues { get; set; } = new HashSet<string>();
        public Dictionary<string, int> EndingTags { get; set; } = new Dictionary<string, int>();
        public ExplorationStats Exploration { get; set; } = new ExplorationStats();
        public List<DiaryEntry> Diary { get; set; } = new List<DiaryEntry>();
    }

    /// <summary>시드 기반 난수 상태(재현 가능한 시뮬레이션용).</summary>
    public sealed class RandomState
    {
        public int Seed { get; set; }
        public long Step { get; set; }
    }

    /// <summary>달력: 52주 × 7일 × 낮/밤 (문서 4.1, D5).</summary>
    public sealed class CalendarState
    {
        public const int TotalWeeks = 52;
        public const int DaysPerWeek = 7;

        /// <summary>1~52</summary>
        public int Week { get; set; } = 1;

        /// <summary>1~7</summary>
        public int Day { get; set; } = 1;

        public DayPhase Phase { get; set; } = DayPhase.Day;
    }

    public sealed class PlayerState
    {
        public ProfileState Profile { get; set; } = new ProfileState();
        public Dictionary<StatType, int> Stats { get; set; } = new Dictionary<StatType, int>();
        public Dictionary<ProficiencyType, ProficiencyState> Proficiency { get; set; } = new Dictionary<ProficiencyType, ProficiencyState>();
        public VitalState Vitals { get; set; } = new VitalState();

        /// <summary>수면 부채 0~5. 자연 감소 없음 (D8).</summary>
        public int SleepDebt { get; set; }

        /// <summary>양심의 무게 0~10.</summary>
        public int Conscience { get; set; }

        public HiddenState Hidden { get; set; } = new HiddenState();
        public List<string> Traits { get; set; } = new List<string>();
    }

    /// <summary>0막 프롤로그에서 정해지는 인적 사항 (문서 7.1).</summary>
    public sealed class ProfileState
    {
        /// <summary>기본 이름 (D23). 표시 문자열은 추후 로컬라이제이션 키로 대체.</summary>
        public string Name { get; set; } = "한서진";

        public string Nickname { get; set; } = "";
        public int BirthMonth { get; set; }
        public int BirthDay { get; set; }

        /// <summary>성격 유형 4축, 예: "ISTJ". 게임 내 표기는 "성격 유형 검사".</summary>
        public string PersonalityType { get; set; } = "";

        public BloodType BloodType { get; set; } = BloodType.Unknown;
        public string GradeBandId { get; set; } = "";
        public string FavoriteSubjectId { get; set; } = "";
        public string ClubId { get; set; } = "";
    }

    public sealed class ProficiencyState
    {
        /// <summary>0~5</summary>
        public int Level { get; set; }

        public int Xp { get; set; }
    }

    public sealed class VitalState
    {
        public int Hp { get; set; }
        public int Fatigue { get; set; }
        public int Stress { get; set; }
        public int Infection { get; set; }
        public int Cleanliness { get; set; }
        public int Hunger { get; set; }
        public int Thirst { get; set; }
    }

    /// <summary>엔딩 판정용 숨김 수치 (문서 5.3).</summary>
    public sealed class HiddenState
    {
        public int TraumaRecovery { get; set; }
        public int SelfAwareness { get; set; }
        public int Humanity { get; set; }
        public int Hope { get; set; }
        public int Isolation { get; set; }
    }

    /// <summary>윤 선생님 상태 (문서 3.2.1, 5.3.1, 7.2).</summary>
    public sealed class MentorState
    {
        public int Affinity { get; set; }

        /// <summary>0 = 기어서 이동, 1 = 목발, 2 = 간이 의족 …</summary>
        public int MobilityStage { get; set; }

        public bool Reinfected { get; set; }
        public int InfectionProgress { get; set; }
        public bool Alive { get; set; } = true;
    }

    /// <summary>친구 (문서 7.3). 재회 변주용 플래그는 GameState.Flags에 둔다.</summary>
    public sealed class FriendState
    {
        /// <summary>휴대폰 연락처 이름(로컬 세이브에만 저장).</summary>
        public string ContactName { get; set; } = "";

        /// <summary>대사용 호칭.</summary>
        public string CallName { get; set; } = "";
    }

    public sealed class BaseState
    {
        public Dictionary<ResourceType, int> Resources { get; set; } = new Dictionary<ResourceType, int>();

        /// <summary>시설 ID → 레벨(0 = 잠김/미개방).</summary>
        public Dictionary<string, int> Facilities { get; set; } = new Dictionary<string, int>();

        public int Defense { get; set; }
        public int Threat { get; set; }

        /// <summary>1막 전력 복구 이벤트 전에는 정전이 기본 상태 (문서 5.4).</summary>
        public bool PowerRestored { get; set; }
    }

    public sealed class ItemStack
    {
        public string ItemId { get; set; } = "";
        public int Count { get; set; }
        public int Durability { get; set; }
    }

    /// <summary>장비 슬롯 (문서 5.6). 값은 아이템 ID, 비어 있으면 빈 문자열.</summary>
    public sealed class LoadoutState
    {
        public string Backpack { get; set; } = "";
        public string MainWeapon { get; set; } = "";
        public string Tool { get; set; } = "";
        public string Armor { get; set; } = "";
        public List<string> QuickSlots { get; set; } = new List<string>();
    }

    /// <summary>한 주의 계획: 7일 × 낮/밤 = 14슬롯 (문서 4.1).</summary>
    public sealed class WeekPlan
    {
        public const int SlotCount = CalendarState.DaysPerWeek * 2;

        public List<PlannedSlot> Slots { get; set; } = CreateEmptySlots();

        private static List<PlannedSlot> CreateEmptySlots()
        {
            var slots = new List<PlannedSlot>(SlotCount);
            for (var i = 0; i < SlotCount; i++)
            {
                slots.Add(new PlannedSlot());
            }

            return slots;
        }
    }

    public sealed class PlannedSlot
    {
        public string ActionId { get; set; } = "";

        /// <summary>탐험 후 강제 휴식처럼 시스템이 잠근 슬롯 (문서 4.2).</summary>
        public bool Locked { get; set; }
    }

    /// <summary>탐험 성향 태그 (문서 8.1).</summary>
    public sealed class ExplorationStats
    {
        public int DayExplorations { get; set; }
        public int NightExplorations { get; set; }
        public int Downs { get; set; }
    }

    public sealed class DiaryEntry
    {
        public int Week { get; set; }
        public string TextKey { get; set; } = "";
    }
}
