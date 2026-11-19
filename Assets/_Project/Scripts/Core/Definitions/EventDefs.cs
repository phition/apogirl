using System;
using System.Collections.Generic;

namespace ApoGirl.Core.Definitions
{
    /// <summary>이벤트 종류 (문서 6.6, 4.4). Random·Crisis는 가중치 추첨과 빈도 상한을 따른다.</summary>
    public enum EventKind
    {
        /// <summary>메인 스토리·인물 서사. 조건이 맞으면 우선순위대로.</summary>
        Story,

        /// <summary>학사력 ("오늘은 원래 ○○ 날이었다"). 날짜 조건과 함께 쓴다.</summary>
        Calendar,

        /// <summary>밤 이벤트 (선생님 대화·회상). 우선순위대로.</summary>
        Night,

        /// <summary>조건 기반 랜덤 이벤트.</summary>
        Random,

        /// <summary>위기 이벤트 (습격·부패·질병). 랜덤과 같은 추첨 규칙.</summary>
        Crisis,
    }

    /// <summary>
    /// 이벤트 정의 (문서 부록 A). 대사는 Yarn 노드(<see cref="DialogueNode"/>), 선택지의 결과는 여기 데이터가 가진다.
    /// Yarn 선택지는 <c>#choice:ID</c> 태그로 <see cref="EventChoiceDef.Id"/>와 연결한다.
    /// </summary>
    [Serializable]
    public sealed class EventDef
    {
        public string Id = "";
        public EventKind Kind;
        public ConditionDef Condition = new ConditionDef();

        /// <summary>높을수록 먼저 (Story/Calendar/Night).</summary>
        public int Priority;

        /// <summary>추첨 가중치 (Random/Crisis).</summary>
        public float Weight = 1f;

        /// <summary>한 번만 발생.</summary>
        public bool Once = true;

        /// <summary>반복 이벤트의 재발생 대기 주수.</summary>
        public int CooldownWeeks;

        /// <summary>Yarn 노드 이름. 비면 Id를 노드 이름으로 쓴다.</summary>
        public string DialogueNode = "";

        /// <summary>이벤트가 시작될 때 바로 적용되는 효과.</summary>
        public GameEffect OnShow = new GameEffect();

        /// <summary>비어 있으면 선택 없이 "계속"으로 끝나는 서술 이벤트.</summary>
        public List<EventChoiceDef> Choices = new List<EventChoiceDef>();

        /// <summary>M6: 막 평가 이벤트면 막 번호 (끝나면 평가 카드), 아니면 0.</summary>
        public int ReviewAct;

        /// <summary>막 평가 카드의 그래프가 시작하는 주.</summary>
        public int ReviewFromWeek = 1;

        public bool IsRandom => Kind == EventKind.Random || Kind == EventKind.Crisis;

        public string Node => string.IsNullOrEmpty(DialogueNode) ? Id : DialogueNode;
    }

    /// <summary>선택지 하나. 조건이 안 맞으면 숨기고, 비용을 못 내면 비활성으로 보인다.</summary>
    [Serializable]
    public sealed class EventChoiceDef
    {
        public string Id = "";

        /// <summary>Yarn이 없을 때 쓰는 선택지 문구 키 (UI 테이블).</summary>
        public string TextKey = "";

        public ConditionDef Condition = new ConditionDef();
        public List<ResourceAmount> Costs = new List<ResourceAmount>();

        /// <summary>판정이 있는 선택지: 성공 시 Effect, 실패 시 OnFailure (문서 9-2 실패도 이야기).</summary>
        public bool HasCheck;
        public CheckDef Check = new CheckDef();

        public GameEffect Effect = new GameEffect();
        public GameEffect OnFailure = new GameEffect();

        /// <summary>선택 후 이어서 바로 시작할 이벤트 (조건 무시).</summary>
        public string NextEventId = "";
    }

    /// <summary>NPC 정의 (문서 6.4). 이름은 UI 테이블 "npc.{Id}" 키.</summary>
    [Serializable]
    public sealed class NpcDef
    {
        public string Id = "";

        /// <summary>합류 시 기본 역할 (경비·요리·교사 …).</summary>
        public string DefaultRole = "";

        public int StartAffinity;
        public int StartTrust;
    }
}
