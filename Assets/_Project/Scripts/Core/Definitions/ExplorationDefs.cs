using System;
using System.Collections.Generic;

namespace ApoGirl.Core.Definitions
{
    // 탐험 모드 정의 데이터 (문서 5.6, 6.3.6, 6.3.8, 11.2.1). 거리 단위는 월드 유닛(약 1m).

    public enum ItemGrade
    {
        Common,
        Rare,
        Quest,
        Story,
    }

    /// <summary>아이템: 크기(w×h) / 무게 / 소음 / 가치 / 등급 / 내구도 / 거점 자원 환산.</summary>
    [Serializable]
    public sealed class ItemDef
    {
        public string Id = "";
        public int Width = 1;
        public int Height = 1;
        public float Weight = 0.5f;

        /// <summary>달릴 때 덜컹거리는 소음 기여도 (M2b-2 소음 시스템).</summary>
        public float Noise = 0.1f;

        public int Value = 1;
        public ItemGrade Grade;
        public int MaxDurability;

        /// <summary>추출 시 거점 자원으로 바뀌는 양. 비어 있으면 아이템 그대로 보관.</summary>
        public List<ResourceAmount> ResourceYield = new List<ResourceAmount>();

        // ── 장비 (M4, 문서 5.6) ──
        public EquipSlot Slot;

        /// <summary>배낭: 격자 크기.</summary>
        public int GridWidth;
        public int GridHeight;

        /// <summary>배낭·방어구: 이동 속도 배율, 이동 소음 배율 (1 = 변화 없음).</summary>
        public float MoveSpeedMultiplier = 1f;
        public float NoiseMultiplier = 1f;

        /// <summary>무기: 한 번에 주는 타격(좀비 Toughness를 깎음), 공격 소음, 재사용 대기, 경직 시간.</summary>
        public int WeaponPower;
        public float AttackNoise = 6f;
        public float AttackCooldown = 1f;
        public float StunSeconds = 1.5f;

        /// <summary>방어구: 받는 피해 감소율 (0~1).</summary>
        public float DamageReduction;

        public ToolKind Tool;

        /// <summary>소모품: 탐험 중 사용 시 HP 회복, 스트레스 감소.</summary>
        public int UseHp;
        public int UseStress;
    }

    [Serializable]
    public sealed class LootEntry
    {
        public string ItemId = "";
        public int Weight = 1;
        public int MinCount = 1;
        public int MaxCount = 1;

        public LootEntry()
        {
        }

        public LootEntry(string itemId, int weight, int minCount = 1, int maxCount = 1)
        {
            ItemId = itemId;
            Weight = weight;
            MinCount = minCount;
            MaxCount = maxCount;
        }
    }

    /// <summary>전리품 테이블: 컨테이너 하나를 열 때 굴린다.</summary>
    [Serializable]
    public sealed class LootTableDef
    {
        public string Id = "";
        public int MinRolls = 1;
        public int MaxRolls = 2;

        /// <summary>컨테이너가 비어 있을 확률.</summary>
        public float EmptyChance = 0.15f;

        public List<LootEntry> Entries = new List<LootEntry>();
    }

    public enum ConnectorKind
    {
        Stairs,
        Ladder,

        /// <summary>깊은 문 (기획 B): 겉 건물 ↔ 깊은 곳, 양쪽에 하나씩 둔다. 낡은 철제 방화문.</summary>
        DeepDoor,

        /// <summary>일방 출구 (기획 B): 깊은 곳 → 겉 건물, 나가기만 한다. 안쪽에 푸시 바가 달린 비상구.</summary>
        OneWayExit,

        /// <summary>일방 출구의 바깥쪽: 손잡이 없는 민짜 철문. 지나갈 수 없다 ("안에서만 열린다").</summary>
        OneWayBack,
    }

    public enum ContainerKind
    {
        Locker,
        Desk,
        Cabinet,
        Fridge,
        Box,
    }

    public enum ExitKind
    {
        Main,
        Alternate,
    }

    /// <summary>계단·사다리: 같은 X 위치에서 TargetFloor로 이동. 깊은 문·일방 출구는 TargetX로 (음수면 같은 X).</summary>
    [Serializable]
    public sealed class ConnectorDef
    {
        public ConnectorKind Kind;
        public int Floor;
        public float X;
        public int TargetFloor;
        public float TargetX = -1f;

        public bool IsStairs => Kind == ConnectorKind.Stairs || Kind == ConnectorKind.Ladder;

        public float ArrivalX => TargetX >= 0f ? TargetX : X;

        /// <summary>무너져서 쓸 수 없는 계단 (보이기만 하고 지나갈 수 없다. 다른 쪽 계단으로 돌아가야 한다).</summary>
        public bool Collapsed;

        /// <summary>계절 재해로 막힌 연결 (실행 중에만, DisasterRules.Apply가 Collapsed와 함께 채운다): 재해 ID.</summary>
        [NonSerialized] public string BlockedBy = "";

        public ConnectorDef Copy() => (ConnectorDef)MemberwiseClone();
    }

    [Serializable]
    public sealed class ContainerDef
    {
        public string Id = "";
        public ContainerKind Kind;
        public int Floor;
        public float X;
        public float SearchSeconds = 2.5f;

        /// <summary>비어 있으면 지역 기본 테이블.</summary>
        public string LootTableId = "";

        /// <summary>비어 있지 않으면 종류 기본 그림 대신 쓸 스프라이트 키 (예: prop_bed).</summary>
        public string SpriteKey = "";

        /// <summary>반드시 들어 있는 상자: 판마다 정해지는 '처음부터 빈 상자' 판정에서 빠진다 (예: 교실동 분실물 상자의 책가방).</summary>
        public bool Guaranteed;
    }

    [Serializable]
    public sealed class ExitDef
    {
        public ExitKind Kind;
        public int Floor;
        public float X;
        public bool StartsLocked;

        /// <summary>계절 재해로 막힌 출구 (눈에 파묻힘: 쇠지렛대로도 못 연다). 재해 ID, 실행 중에만.</summary>
        [NonSerialized] public string BlockedBy = "";

        public ExitDef Copy() => (ExitDef)MemberwiseClone();
    }

    /// <summary>복도 쪽 벽의 창문 (방 왼쪽 끝 기준 X, 폭). 안팎에서 서로의 실루엣이 보인다.</summary>
    [Serializable]
    public sealed class WindowDef
    {
        public float X;
        public float Width = 3f;

        public WindowDef()
        {
        }

        public WindowDef(float x, float width)
        {
            X = x;
            Width = width;
        }
    }

    /// <summary>
    /// 방 템플릿: 같은 종류의 방(일반 교실 등)은 내부 배치를 돌려 쓰고 전리품만 방마다 다르게 굴린다.
    /// 방은 복도를 따라 [SpanX, SpanX + Width] 구간을 차지하고, 방 안 X와 복도 X는 SpanX만큼 차이 난다.
    /// 문(DoorXs: 앞문·뒷문)과 창문은 복도 쪽 벽에 있다.
    /// </summary>
    [Serializable]
    public sealed class RoomTemplateDef
    {
        public string Id = "";
        public float Width = 14f;
        public List<float> DoorXs = new List<float> { 1.2f };
        public List<WindowDef> Windows = new List<WindowDef>();
        public List<ContainerDef> Containers = new List<ContainerDef>();
        public string DefaultLootTableId = "";

        /// <summary>뒷벽 장식 스프라이트 (비면 교실은 칠판, 나머지는 없음).</summary>
        public string WallDecor = "";
    }

    /// <summary>복도에 붙은 방 하나: X = 방 왼쪽 끝의 복도 위치(SpanX). Number는 방 이름 표기용 (예: 2-3 교실의 3).</summary>
    [Serializable]
    public sealed class DoorDef
    {
        public string RoomId = "";
        public string TemplateId = "";
        public int Floor;

        /// <summary>방 왼쪽 끝이 놓이는 복도 X. 문 위치 = X + 템플릿 DoorXs.</summary>
        public float X;
        public int Number;

        /// <summary>비어 있지 않으면 이 방의 기본 전리품 테이블 (템플릿 기본값보다 우선).</summary>
        public string LootTableId = "";

        /// <summary>이 방의 상자를 하나라도 뒤지면 세우는 플래그 (예: 1층 끝 교실 → 거처 이벤트).</summary>
        public string SearchFlag = "";
    }

    /// <summary>
    /// 건물 레이아웃 (문서 6.3.3 단면 구조): 층마다 긴 복도, 복도를 따라 문 → 방(템플릿 내부).
    /// 0층이 1층(지상). 복도의 Containers는 복도에 놓인 사물함 등.
    /// </summary>
    [Serializable]
    public sealed class BuildingLayoutDef
    {
        public int Floors = 4;
        public float FloorHeight = 4f;
        public float CorridorLength = 80f;

        /// <summary>옥상인 층 (-1 = 없음). 옥상은 방·좀비가 없고 벽 대신 철조망 너머로 마을이 보인다.</summary>
        public int RooftopFloor = -1;

        /// <summary>옥상에 처음 올라서면 남기는 플래그 (귀환 뒤 거점 옥상 해금 이벤트의 조건).</summary>
        public string RooftopFlag = "";

        /// <summary>옥상 길이 (0 = 복도와 같다). 옥상은 X = 0 ~ 이 길이만 걸을 수 있다 — 넘는 계단·출구는 두지 않는다.</summary>
        public float RooftopLength;

        public bool IsRooftop(int floor) => RooftopFloor >= 0 && floor == RooftopFloor;

        /// <summary>
        /// 깊은 곳 층 수 (기획 B, 0 = 없음). 깊은 곳 층 번호는 겉 층 뒤에 붙는다: Floors, Floors + 1, … (= 지하 1, 지하 2, …).
        /// 그래서 층 번호로 찾는 배치(좀비·상자·조우·계단)는 깊은 곳에도 그대로 쓴다.
        /// </summary>
        public int DeepFloors;

        /// <summary>깊은 곳 복도 길이.</summary>
        public float DeepCorridorLength = 40f;

        /// <summary>깊은 곳 이름 문구 키 (예: deep.LOC_SUBWAY, 화면 위 층 표시 "지하 1 · 역무 구역").</summary>
        public string DeepNameKey = "";

        public int TotalFloors => Floors + Math.Max(0, DeepFloors);

        public bool IsDeep(int floor) => DeepFloors > 0 && floor >= Floors && floor < TotalFloors;

        /// <summary>깊은 곳 층의 몇 번째 지하인가 (지하 1 = 1).</summary>
        public int DeepLevel(int floor) => floor - Floors + 1;

        public float CorridorLengthOf(int floor) => IsDeep(floor) ? DeepCorridorLength
            : IsRooftop(floor) && RooftopLength > 0f ? Math.Min(RooftopLength, CorridorLength) : CorridorLength;

        public List<ConnectorDef> Connectors = new List<ConnectorDef>();
        public List<DoorDef> Doors = new List<DoorDef>();
        public List<RoomTemplateDef> RoomTemplates = new List<RoomTemplateDef>();
        public List<ContainerDef> Containers = new List<ContainerDef>();
        public List<ExitDef> Exits = new List<ExitDef>();

        public RoomTemplateDef FindTemplate(string id) => RoomTemplates.Find(t => t.Id == id);

        /// <summary>연결·출구 목록만 새로 만든 사본 (계절 재해가 바꾼다). 나머지는 원본과 함께 쓴다.</summary>
        public BuildingLayoutDef CopyLinks()
        {
            var copy = (BuildingLayoutDef)MemberwiseClone();
            copy.Connectors = Connectors.ConvertAll(c => c.Copy());
            copy.Exits = Exits.ConvertAll(e => e.Copy());
            return copy;
        }
    }

    /// <summary>적 종류. Survivor = 자원을 찾아 나온 적대적 생존자 (낮 탐험, 붙잡히면 성향에 따라 쫓겨남·강탈·구타).</summary>
    public enum EnemyKind
    {
        Zombie,
        Survivor,
    }

    /// <summary>좀비·적대적 생존자 유형 (문서 6.3.5-(4)). 속도는 유닛/초, 시야는 유닛.</summary>
    [Serializable]
    public sealed class EnemyDef
    {
        public string Id = "";
        public EnemyKind Kind = EnemyKind.Zombie;
        public float WalkSpeed = 1f;
        public float RunSpeed = 3.2f;
        public float SightRange = 7f;

        /// <summary>소음을 듣는 반경 배율 (청각 민감형은 2).</summary>
        public float HearingMultiplier = 1f;

        /// <summary>보고 있을 때 인지 게이지가 차는 속도 (초당, 1이면 가득).</summary>
        public float AwarenessRate = 1.2f;

        public int Damage = 12;
        public float HitCooldown = 1.2f;
        public float AttackRange = 0.7f;

        /// <summary>무기 타격을 몇 번 버티는가 (WeaponPower 합계가 넘으면 쓰러짐).</summary>
        public int Toughness = 3;

        // ── 적대적 생존자 ──

        /// <summary>경계(조사·추격·수색) 중 이 거리 안에 숨은 사람을 찾아낸다 (0이면 숨으면 못 찾음).</summary>
        public float SeesHiddenWithin;

        /// <summary>배회 반경 (0이면 밸런스 기본값).</summary>
        public float PatrolRange;

        /// <summary>false면 무기에 기절만 하고 쓰러지지 않는다 (사람).</summary>
        public bool CanBeDowned = true;
    }

    /// <summary>
    /// 좀비 배치. RoomId가 비어 있으면 해당 층 복도, 아니면 그 방 안 (방 기준 X).
    /// Dormant 배치는 하나의 휴면 군집이 되어 각성 게이지를 공유한다 (문서 6.3.2-(3)).
    /// </summary>
    [Serializable]
    public sealed class SpawnDef
    {
        public string EnemyId = "";
        public int Floor;
        public string RoomId = "";
        public float X;
        public int Count = 1;
        public float Spread = 1.5f;
        public bool Dormant;
    }

    /// <summary>시간대별 변형 (문서 6.3.8 DayVariant / NightVariant). M2b-2는 좀비 배치만.</summary>
    [Serializable]
    public sealed class LocationVariantDef
    {
        public List<SpawnDef> Spawns = new List<SpawnDef>();

        /// <summary>이 시간대에 열 수 없는 방 (예: 약국 약품고는 낮에 셔터).</summary>
        public List<string> LockedRooms = new List<string>();

        /// <summary>쇠지렛대로만 열 수 있는 방 (큰 소음).</summary>
        public List<string> ForceableRooms = new List<string>();

        public List<EncounterDef> Encounters = new List<EncounterDef>();

        /// <summary>적대적 생존자 등장 확률 배율 (막별 기본 확률 × 이 값, 0이면 나오지 않음).</summary>
        public float HostileChanceMultiplier = 1f;

        /// <summary>적대적 생존자 시작 위치 후보 (EnemyId는 무시). 비면 출구에서 먼 복도 아무 곳.</summary>
        public List<SpawnDef> HostileSpots = new List<SpawnDef>();
    }

    /// <summary>탐험 지역 (실시간 모드). ID는 같은 지역의 헤드리스 ExpeditionModelDef와 같다.</summary>
    [Serializable]
    public sealed class LocationDef
    {
        public string Id = "";
        public BuildingLayoutDef Layout = new BuildingLayoutDef();
        public float DayRunSeconds = 360f;
        public float NightRunSeconds = 360f;
        public string DefaultLootTableId = "";
        public LocationVariantDef DayVariant = new LocationVariantDef();
        public LocationVariantDef NightVariant = new LocationVariantDef();

        /// <summary>튜토리얼 런 (화면 안내 표시).</summary>
        public bool Tutorial;

        /// <summary>한 번 뒤진 상자는 다음 출정에도 비어 있다 (모든 지역 기본).</summary>
        public bool DepletingContainers = true;

        /// <summary>매달 첫 주에 뒤진 상자의 일부가 다시 채워진다 (교실동은 false: 무한 파밍 방지).</summary>
        public bool Restocks = true;
        public LocationDef Copy() => (LocationDef)MemberwiseClone();
    }
}
