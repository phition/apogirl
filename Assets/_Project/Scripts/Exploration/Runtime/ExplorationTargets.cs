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
    }

    /// <summary>
    /// 이동 가능한 공간 하나: 층별 복도 또는 방 내부. 방 내부는 복도와 떨어진 별도 위치에 처음 들어갈 때 만든다.
    /// 좌표 X는 공간 왼쪽 끝 기준 (로컬).
    /// </summary>
    public sealed class ExplorationArea
    {
        public bool IsRoom;
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
        public float SearchProgress;
        public bool Searched;
        public readonly List<ItemStack> Contents = new List<ItemStack>();

        public ExitDef Exit;
        public bool Locked;

        /// <summary>Door: 들어갈 방 / RoomExit: 이 문이 속한 방.</summary>
        public ExplorationArea Room;

        /// <summary>문을 지나면 서게 될 반대편 공간의 X (Door → 방 안 X, RoomExit → 복도 X).</summary>
        public float LinkX;

        public bool IsDoorway => Kind == TargetKind.Door || Kind == TargetKind.RoomExit;

        public int Floor => Area.Floor;

        public void MarkSearched()
        {
            Searched = true;
            Tint(new Color(0.35f, 0.33f, 0.3f));
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
