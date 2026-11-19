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

    /// <summary>방: 층과 가로 범위 [X, X + Width].</summary>
    [Serializable]
    public sealed class RoomDef
    {
        public string Id = "";
        public int Floor;
        public float X;
        public float Width = 6f;
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

    /// <summary>건물 단면 레이아웃 (수작업 템플릿, 문서 6.3.8). 0층이 1층(지상).</summary>
    [Serializable]
    public sealed class BuildingLayoutDef
    {
        public int Floors = 3;
        public float FloorHeight = 4f;
        public float Width = 36f;
        public List<RoomDef> Rooms = new List<RoomDef>();
        public List<ConnectorDef> Connectors = new List<ConnectorDef>();
        public List<ContainerDef> Containers = new List<ContainerDef>();
        public List<ExitDef> Exits = new List<ExitDef>();
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
    }
}
