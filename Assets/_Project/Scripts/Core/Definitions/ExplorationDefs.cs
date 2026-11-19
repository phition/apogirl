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

    /// <summary>계단·사다리: 같은 X 위치에서 TargetFloor로 이동.</summary>
    [Serializable]
    public sealed class ConnectorDef
    {
        public ConnectorKind Kind;
        public int Floor;
        public float X;
        public int TargetFloor;
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
    }

    [Serializable]
    public sealed class ExitDef
    {
        public ExitKind Kind;
        public int Floor;
        public float X;
        public bool StartsLocked;
    }

    /// <summary>
    /// 방 템플릿: 같은 종류의 방(일반 교실 등)은 내부 배치를 돌려 쓰고 전리품만 방마다 다르게 굴린다.
    /// 컨테이너 X는 방 왼쪽 끝 기준. 방 출입문은 ExitX 위치.
    /// </summary>
    [Serializable]
    public sealed class RoomTemplateDef
    {
        public string Id = "";
        public float Width = 14f;
        public float ExitX = 1.2f;
        public List<ContainerDef> Containers = new List<ContainerDef>();
        public string DefaultLootTableId = "";
    }

    /// <summary>복도의 문: 위치 → 방 템플릿. Number는 방 이름 표기용 (예: 2-3 교실의 3).</summary>
    [Serializable]
    public sealed class DoorDef
    {
        public string RoomId = "";
        public string TemplateId = "";
        public int Floor;
        public float X;
        public int Number;

        /// <summary>비어 있지 않으면 이 방의 기본 전리품 테이블 (템플릿 기본값보다 우선).</summary>
        public string LootTableId = "";
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
        public List<ConnectorDef> Connectors = new List<ConnectorDef>();
        public List<DoorDef> Doors = new List<DoorDef>();
        public List<RoomTemplateDef> RoomTemplates = new List<RoomTemplateDef>();
        public List<ContainerDef> Containers = new List<ContainerDef>();
        public List<ExitDef> Exits = new List<ExitDef>();

        public RoomTemplateDef FindTemplate(string id) => RoomTemplates.Find(t => t.Id == id);
    }

    /// <summary>좀비 유형 (문서 6.3.5-(4)). 속도는 유닛/초, 시야는 유닛.</summary>
    [Serializable]
    public sealed class EnemyDef
    {
        public string Id = "";
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
    }
}
