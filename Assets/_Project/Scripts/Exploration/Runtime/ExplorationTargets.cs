using System.Collections.Generic;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.State;
using UnityEngine;

namespace ApoGirl.Exploration.Runtime
{
    public enum TargetKind
    {
        Connector,
        Container,
        Exit,

        /// <summary>복도의 방 문 (들어가기).</summary>
        Door,

        /// <summary>방 안의 출입문 (복도로 나가기).</summary>
        RoomExit,

        /// <summary>바깥 길에서 건물로 들어가는 문 (LinkArea·LinkX로 이동).</summary>
        Passage,

        /// <summary>사람·스토리 오브젝트 조우 (M4).</summary>
        Encounter,
    }

    /// <summary>
    /// 이동 가능한 공간 하나: 층별 복도 또는 방 내부. 방 내부는 복도와 떨어진 별도 위치에 처음 들어갈 때 만든다.
    /// 좌표 X는 공간 왼쪽 끝 기준 (로컬).
    /// </summary>
    public sealed class ExplorationArea
    {
        public bool IsRoom;

        /// <summary>건물 밖 길 (진입로·탈출로): 복도도 방도 아니다. 조명은 바깥 밝기, 좀비는 문으로 따라 나올 수 있다.</summary>
        public bool IsOutdoor;

        /// <summary>옥상 (건물 맨 위층 복도 자리): 방·좀비가 없고, 벽 대신 철조망 너머로 마을이 보인다. 조명은 바깥 밝기.</summary>
        public bool IsRoof;

        /// <summary>깊은 곳 복도 (기획 B): 늘 어둡고 소리가 울린다. 깊은 문·일방 출구로만 오간다.</summary>
        public bool IsDeep;

        /// <summary>바깥 길 중 진입로 (왼쪽 끝이 학교 쪽). 탈출로는 오른쪽 끝이 학교 쪽.</summary>
        public bool IsEntryRoad;
        public int Floor;
        public Vector2 Origin;
        public float Width;
        public string Label = "";

        public int DoorIndex = -1;
        public DoorDef Door;

        /// <summary>방이 차지하는 복도 구간의 왼쪽 끝 (방 X + SpanX = 복도 X).</summary>
        public float SpanX;

        /// <summary>복도 쪽 문 대상들 (앞문·뒷문).</summary>
        public readonly List<ExplorationTarget> CorridorDoors = new List<ExplorationTarget>();
        public RoomTemplateDef Template;
        public bool Built;
        public bool Visited;
        public GameObject Root;
        public readonly List<ExplorationTarget> Targets = new List<ExplorationTarget>();
    }

    /// <summary>상호작용 대상 하나 (계단·컨테이너·탈출구·문). 문서 11.2.1의 IInteractable에 해당.</summary>
    public sealed class ExplorationTarget
    {
        /// <summary>상호작용 가능 거리 (월드 유닛).</summary>
        public const float Reach = 0.8f;

        public TargetKind Kind;
        public ExplorationArea Area;
        public float X;
        public GameObject View;

        public ConnectorDef Connector;

        public ContainerDef Container;
        public float SearchSeconds;

        /// <summary>스트레스가 없었다면 걸렸을 수색 시간. 이걸 넘기면 짜증 섞인 한숨 (한 번).</summary>
        public float CalmSeconds;
        public bool Sighed;
        public float SearchProgress;
        public bool Searched;
        public readonly List<ItemStack> Contents = new List<ItemStack>();

        /// <summary>상자 고갈 지역의 상자 키 (뒤지면 거점이 기록, 이미 뒤진 상자면 null).</summary>
        public string LootKey;

        public ExitDef Exit;
        public bool Locked;

        /// <summary>지나가면 서게 될 다른 공간 (건물 출구 → 바깥 길, 바깥 길의 문 → 복도). 비면 Exit는 곧바로 귀환.</summary>
        public ExplorationArea LinkArea;

        /// <summary>Door: 들어갈 방 / RoomExit: 이 문이 속한 방.</summary>
        public ExplorationArea Room;

        /// <summary>문을 지나면 서게 될 반대편 공간의 X (Door → 방 안 X, RoomExit → 복도 X).</summary>
        public float LinkX;

        public bool IsDoorway => Kind == TargetKind.Door || Kind == TargetKind.RoomExit;

        // ── M4 ──
        public EncounterDef Encounter;

        /// <summary>이 시간대에 열리지 않는 방 문 (셔터 등).</summary>
        public bool RoomLocked;

        /// <summary>쇠지렛대로 열 수 있는 잠긴 문 (Forced가 되면 열림).</summary>
        public bool Forceable;
        public bool Forced;

        /// <summary>깊은 곳 특별한 방 문 (큰 문·빗장): 길게 눌러 SealProgress를 채우면 Forced.</summary>
        public DoorSeal Seal;
        public float SealProgress;

        /// <summary>봉인 표시 코드 그림 (열리면 지운다).</summary>
        public readonly List<GameObject> SealViews = new List<GameObject>();

        public bool Sealed => Seal != DoorSeal.None && !Forced;

        /// <summary>문 위에 겹친 조우(잠긴 집 문 등)가 가린 방 문 그림. 조우가 사라지면 다시 보인다.</summary>
        public GameObject CoveredDoorView;

        public int Floor => Area.Floor;

        public void MarkSearched()
        {
            Searched = true;
            Tint(new Color(0.62f, 0.6f, 0.56f));
        }

        public void Tint(Color color)
        {
            if (View != null)
            {
                View.GetComponent<SpriteRenderer>().color = color;
            }
        }
    }
}
