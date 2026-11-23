using System.Collections.Generic;

namespace ApoGirl.Core.State
{
    // M0 초안: 세이브 1개 = GameState 1개. 고정 집합은 enum, 콘텐츠(행동·아이템·시설·NPC)는 문자열 ID.
    // 세부 필드와 규칙은 M1 클래스 다이어그램에서 확정한다.

    /// <summary>게임 전체 상태. JSON 직렬화 대상이며 MonoBehaviour에 의존하지 않는다.</summary>
    public sealed class GameState
    {
        /// <summary>현재 세이브 포맷 버전. 마이그레이션 시 증가시킨다.</summary>
        public const int CurrentSaveVersion = 12;

        public int SaveVersion { get; set; } = CurrentSaveVersion;
        public GameStatus Status { get; set; } = GameStatus.Playing;

        /// <summary>게임 종료 사유 ID (예: END_CAUSE_STARVATION). 진행 중이면 빈 문자열.</summary>
        public string EndCause { get; set; } = "";

        public RandomState Rng { get; set; } = new RandomState();

        /// <summary>난이도 ID (BalanceConfig.Difficulties). 새 게임에서 정하고 바꾸지 않는다.</summary>
        public string Difficulty { get; set; } = "STANDARD";

        /// <summary>마지막으로 되돌린 주 (같은 주는 한 번만 되돌린다, 이야기 중심).</summary>
        public int LastUndoWeek { get; set; }
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

        /// <summary>NPC ID → 관계 상태 (윤 선생님은 Teacher에 따로 둔다). 없으면 Unknown.</summary>
        public Dictionary<string, NpcState> Npcs { get; set; } = new Dictionary<string, NpcState>();

        /// <summary>이벤트 발생·선택 기록 (1회성, 쿨다운, 랜덤 빈도 상한, 엔딩 회상).</summary>
        public EventLogState Events { get; set; } = new EventLogState();
        public List<DiaryEntry> Diary { get; set; } = new List<DiaryEntry>();
        public CarryOverState CarryOver { get; set; } = new CarryOverState();

        /// <summary>지난주 계획 (행동 ID만, "지난주 복사"용). 첫 주에는 null.</summary>
        public WeekPlan LastWeekPlan { get; set; }

        /// <summary>플레이어가 저장한 루틴 프리셋 (문서 4.3). v2에서 추가.</summary>
        public List<PlanPreset> PlanPresets { get; set; } = new List<PlanPreset>();

        /// <summary>주간 정산마다 남기는 기록 (엔딩 화면의 성장 그래프). v4에서 추가.</summary>
        public List<WeekRecord> History { get; set; } = new List<WeekRecord>();

        /// <summary>세력 ID → 평판·접촉 기록. v6에서 추가.</summary>
        public Dictionary<string, FactionState> Factions { get; set; } = new Dictionary<string, FactionState>();

        /// <summary>CD ID(CD_01~08) → 처음 주운 주차 (기획 A.3). v9에서 추가.</summary>
        public Dictionary<string, int> CdWeeks { get; set; } = new Dictionary<string, int>();

        /// <summary>방송반 콘솔에서 고른 CD ID (허브 배경음). 비면 기본 허브 소리. v9에서 추가.</summary>
        public string CdTrack { get; set; } = "";

        /// <summary>동아리를 같이 할 사람 (확장 7, 인물 서랍에서 고른다). 비면 호감이 가장 높은 사람. v10에서 추가.</summary>
        public string ClubPartner { get; set; } = "";

        /// <summary>이번 주 식단 (식량·물 따로, 배분 화면에서 고르고 다음 주에도 남는다). v12에서 추가.</summary>
        public DietLevel FoodDiet { get; set; } = DietLevel.Care;
        public DietLevel WaterDiet { get; set; } = DietLevel.Care;
    }

    /// <summary>외부 세력과의 관계.</summary>
    public sealed class FactionState
    {
        public bool Known { get; set; }
        public int Standing { get; set; }

        /// <summary>마지막으로 평판이 움직인 주 (멀어짐 계산).</summary>
        public int LastContactWeek { get; set; }

        /// <summary>마지막 거래 주 (세력마다 주 1회).</summary>
        public int LastTradeWeek { get; set; }

        /// <summary>최근 일 (새것이 앞, 최대 FactionLogSize).</summary>
        public List<FactionLogEntry> Log { get; set; } = new List<FactionLogEntry>();

        /// <summary>무전 거래로 다음 주에 도착할 자원 (ResourceType 이름 → 양).</summary>
        public Dictionary<string, int> Pending { get; set; } = new Dictionary<string, int>();
    }

    public sealed class FactionLogEntry
    {
        public int Week { get; set; }
        public string TextKey { get; set; } = "";
        public int Standing { get; set; }
    }

    /// <summary>한 주가 끝났을 때의 모습.</summary>
    public sealed class WeekRecord
    {
        public int Week { get; set; }
        public Dictionary<StatType, int> Stats { get; set; } = new Dictionary<StatType, int>();
        public int Hp { get; set; }
        public int Stress { get; set; }
        public int SleepDebt { get; set; }
    }

    /// <summary>주간 계획 프리셋: 14슬롯 행동 ID.</summary>
    public sealed class PlanPreset
    {
        public string Name { get; set; } = "";
        public List<string> ActionIds { get; set; } = new List<string>();
    }

    /// <summary>다음 주로 넘어가는 규칙 효과.</summary>
    public sealed class CarryOverState
    {
        /// <summary>다음 주 1일차 낮부터 강제 휴식으로 잠글 슬롯 수 (7일차 밤 탐험, 쓰러짐 등).</summary>
        public int LockedRestSlots { get; set; }
    }

    /// <summary>시드 기반 난수 상태(재현 가능한 시뮬레이션용).</summary>
    public sealed class RandomState
    {
        public int Seed { get; set; }

        /// <summary>난수 생성기 내부 상태. 세이브 후 이어서 같은 난수열을 재현한다.</summary>
        public long State { get; set; }
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

        /// <summary>이번 주 14슬롯을 모두 실행했고 주간 정산을 기다리는 중.</summary>
        public bool WeekComplete { get; set; }
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

        /// <summary>연속 밤 휴식 횟수. 규정 횟수에 도달하면 수면 부채를 갚는다.</summary>
        public int NightRestStreak { get; set; }

        /// <summary>연속 행동 보너스 추적용: 마지막 교육 행동 ID와 연속 횟수.</summary>
        public string RoutineActionId { get; set; } = "";

        public int RoutineStreak { get; set; }

        /// <summary>직전 슬롯이 탐험이었는지 (연속 출정 판정).</summary>
        public bool LastSlotWasExpedition { get; set; }

        /// <summary>새벽 귀환 후 다음 휴식의 회복량 감소 (문서 6.3.2-(4)).</summary>
        public bool ReducedNextRest { get; set; }

        /// <summary>주간 정산 때 스트레스가 한계였던 연속 주 수. 한계 주 수에 이르면 정신 붕괴 (M5).</summary>
        public int BreakdownWeeks { get; set; }

        /// <summary>적대적 생존자에게 구타당한 트라우마 단계 (0~MaxAssaultTrauma). 단계마다 낮 탐험 스트레스 증가.</summary>
        public int AssaultTrauma { get; set; }

        /// <summary>트라우마 회복 진행 (주). TraumaRecoveryWeeks에 닿으면 1단계 회복. 다시 맞으면 0.</summary>
        public int TraumaCareProgress { get; set; }
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
        /// <summary>포만 (−100~+100, 0 = 보통). +면 배부름, −면 허기짐.</summary>
        public int Satiety { get; set; }

        /// <summary>수분 (−100~+100, 0 = 보통). −면 목마름.</summary>
        public int Hydration { get; set; }
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

        /// <summary>선생님의 몸 상태와 배분 기록. v7.</summary>
        public BodyState Body { get; set; } = new BodyState();
        public Dictionary<string, int> GiftLog { get; set; } = new Dictionary<string, int>();
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

        /// <summary>설치한 함정 수 (습격·침입 1회에 하나씩 쓴다).</summary>
        public int Traps { get; set; }

        /// <summary>1막 전력 복구 이벤트 전에는 정전이 기본 상태 (문서 5.4).</summary>
        public bool PowerRestored { get; set; }

        /// <summary>이번 주 날씨 (M6). 주간 정산 때 다음 주 날씨를 굴린다.</summary>
        public WeatherType Weather { get; set; }
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

        /// <summary>규칙상 휴식이어야 하는 슬롯 (탐험 직후, 문서 4.2). 다른 행동을 두면 경고 후 허용.</summary>
        public bool ForcedRest { get; set; }

        /// <summary>플레이어가 바꿀 수 없는 슬롯 (주 경계 이월, 쓰러짐, 수면 부채 4단계).</summary>
        public bool Locked { get; set; }
    }

    /// <summary>탐험 성향 태그 (문서 8.1).</summary>
    public sealed class ExplorationStats
    {
        public int DayExplorations { get; set; }
        public int NightExplorations { get; set; }
        public int Downs { get; set; }

        /// <summary>행동 ID → 실행 횟수 (M4: 해금·이벤트 조건용).</summary>
        public Dictionary<string, int> ActionCounts { get; set; } = new Dictionary<string, int>();

        public int Raids { get; set; }
        public int RaidsRepelled { get; set; }

        /// <summary>적대적 생존자에게 붙잡힌 횟수 (쫓겨남 / 강탈 / 구타).</summary>
        public int HostileEvictions { get; set; }
        public int HostileRobberies { get; set; }
        public int HostileBeatings { get; set; }

        /// <summary>다른 생존자 눈에 띈 정도 (생존자가 나오는 지역의 낮 탐험마다 +1, 매주 절반).</summary>
        public int DayExposure { get; set; }

        /// <summary>낮 침입 횟수와 그중 대비가 부족해 크게 당한 횟수.</summary>
        public int Intrusions { get; set; }
        public int IntrusionsLost { get; set; }

        /// <summary>뒤진 상자 키 "지역ID/키" (DepletingContainers 지역만, 교실동 무한 파밍 방지).</summary>
        public HashSet<string> LootedContainers { get; set; } = new HashSet<string>();
    }

    public sealed class NpcState
    {
        public NpcStatus Status { get; set; }
        public int Affinity { get; set; }
        public int Trust { get; set; }
        public string Role { get; set; } = "";

        /// <summary>역할을 받은 주 (정원을 넘으면 먼저 받은 사람이 효과를 받는다). v8.</summary>
        public int RoleSinceWeek { get; set; }

        /// <summary>낮 침입으로 다쳐 역할을 못 하는 남은 주 수.</summary>
        public int InjuredWeeks { get; set; }

        /// <summary>동료 탐험에서 입은 가벼운 상처 (역할·함께한 시간은 그대로, 탐험만 쉰다). 입은 주 다음 주 결산에 낫는다.</summary>
        public int MinorWoundWeeks { get; set; }
        public int MinorWoundWeek { get; set; }

        /// <summary>이번 주 외출하는 날 (낮 슬롯, 1~7)과 갈 곳. 주 시작에 정하고 결산에 지운다.</summary>
        public List<int> OutingDays { get; set; } = new List<int>();
        public List<string> OutingLocations { get; set; } = new List<string>();

        /// <summary>"이번 주는 남아 줘" (주 시작 전 계획 단계에서 켠다), 연속으로 붙잡은 주.</summary>
        public bool StayHome { get; set; }

        /// <summary>지금 외출 중 (그날 낮 칸 시작 ~ 그날 밤 칸 시작). 낮 칸 뒤 이벤트(침입 등)에서도 자리에 없다.</summary>
        public bool Away { get; set; }
        public int StayStreak { get; set; }

        /// <summary>동료의 몸 상태 (스트레스·포만·수분). v7에서 추가 (기획 D-1).</summary>
        public BodyState Body { get; set; } = new BodyState();

        /// <summary>배분 기록: "kind:Food" / "item:ITEM_ID" → 마지막으로 받은 주. v7.</summary>
        public Dictionary<string, int> GiftLog { get; set; } = new Dictionary<string, int>();

        /// <summary>동아리 활동(BOARDGAME 등) → 같이 한 횟수 (확장 7). v10에서 추가.</summary>
        public Dictionary<string, int> ClubCounts { get; set; } = new Dictionary<string, int>();
    }

    /// <summary>동료·선생님의 몸 상태. 주인공 Vitals와 같은 척도 (스트레스 0~100, 포만·수분 −100~+100, 0 = 보통).</summary>
    public sealed class BodyState
    {
        public int Stress { get; set; }
        public int Satiety { get; set; }
        public int Hydration { get; set; }
    }

    public sealed class EventLogState
    {
        /// <summary>이벤트 ID → 기록.</summary>
        public Dictionary<string, EventRecord> Records { get; set; } = new Dictionary<string, EventRecord>();

        /// <summary>선택 순서대로 "이벤트ID:선택지ID" (엔딩 후일담의 KeyChoices).</summary>
        public List<string> ChoiceLog { get; set; } = new List<string>();

        /// <summary>랜덤 빈도 상한용: 마지막으로 센 주차와 그 주의 랜덤 이벤트 수.</summary>
        public int RandomWeek { get; set; }
        public int RandomCountThisWeek { get; set; }

        /// <summary>마지막 랜덤 이벤트가 난 날 (주차 × 10 + 요일).</summary>
        public int LastRandomDayKey { get; set; }
    }

    public sealed class EventRecord
    {
        public int Count { get; set; }
        public int LastWeek { get; set; }
        public string LastChoice { get; set; } = "";
    }

    public sealed class DiaryEntry
    {
        public int Week { get; set; }
        public string TextKey { get; set; } = "";
    }
}
