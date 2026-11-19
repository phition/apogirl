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
    }

    /// <summary>
    /// 상호작용 대상 하나 (계단·컨테이너·탈출구). 문서 11.2.1의 IInteractable에 해당하며,
    /// M2b-1은 판정을 디렉터가 직접 하므로 데이터+시각 표현만 갖는다.
    /// </summary>
    public sealed class ExplorationTarget
    {
        public TargetKind Kind;
        public int Floor;
        public float X;
        public GameObject View;

        public ConnectorDef Connector;

        public ContainerDef Container;
        public int ContainerIndex;
        public float SearchSeconds;
        public float SearchProgress;
        public bool Searched;
        public readonly List<ItemStack> Contents = new List<ItemStack>();

        public ExitDef Exit;
        public bool Locked;

        /// <summary>상호작용 가능 거리 (월드 유닛).</summary>
        public const float Reach = 0.8f;

        public void MarkSearched()
        {
            Searched = true;
            if (View != null)
            {
                View.GetComponent<SpriteRenderer>().color = new Color(0.35f, 0.33f, 0.3f);
            }
        }
    }
}
