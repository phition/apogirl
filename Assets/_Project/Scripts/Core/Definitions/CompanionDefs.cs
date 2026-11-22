using System;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Definitions
{
    /// <summary>이번 주 배분(기획 D.3)에서 주는 것의 종류.</summary>
    public enum GiveKind
    {
        Food,
        Water,
        Medicine,
        Gift,
    }

    /// <summary>한 사람이 좋아하는 물건 (기획 D.4). NpcId는 동료 ID, 윤 선생님은 NPC_TEACHER, 고양이는 NPC_CAT.</summary>
    [Serializable]
    public sealed class LikedItemsDef
    {
        public string NpcId = "";
        public string[] Items = Array.Empty<string>();
    }

    /// <summary>적성 (역할 재기획 3절): 이 동료와 잘 맞는 역할. 맡기면 효과가 커진다.</summary>
    [Serializable]
    public sealed class RoleAptitudeDef
    {
        public string NpcId = "";
        public string[] Roles = Array.Empty<string>();
    }

    /// <summary>
    /// 동료 탐험 수확 한 종류 (역할 재기획 P5): 외출 한 번에 이 표에서 한 종류를 Weight 확률로 뽑는다 (합이 1보다 작으면 나머지는 빈손).
    /// 양은 그 지역 주인공 낮 산출 × OutingYieldShare. 지역에 없는 자원이면 가 본 지역 평균, 그것도 없으면 Fallback 범위(주인공 기준) × 비율.
    /// </summary>
    [Serializable]
    public sealed class OutingHaulDef
    {
        public ResourceType Resource;
        public float Weight;
        public int FallbackMin;
        public int FallbackMax;
    }

    /// <summary>동료 특기의 종류 (기획 C.2 ②).</summary>
    public enum SpecialKind
    {
        /// <summary>의약품 2개 이상 드는 행동은 Value만큼 덜 (최소 1). 박 선생님.</summary>
        MedicineCostDown,

        /// <summary>밤 습격에 뚫렸을 때 피해 비율 감소 (0.2 = −20%). 도현.</summary>
        RaidDamageCut,

        /// <summary>주간 결산 때 주인공 스트레스 −Value. 송예지.</summary>
        WeeklyStressRelief,

        /// <summary>Target 세력에 주마다 바치는 자원 −Value (휴전 중 공물). 최하린.</summary>
        TributeCut,

        /// <summary>습격·침입 계산의 방어도 +Value. 선배.</summary>
        DefenseBonus,

        /// <summary>깊은 곳(탐험 깊이 B)의 일방 출구 위치를 안다. 유랑자. B가 생기면 효과가 붙는다.</summary>
        DeepExitKnown,
    }

    /// <summary>동료 특기 한 줄 (기획 C.2 ②). 이름·설명 문구는 special.{NpcId}.name / .desc.</summary>
    [Serializable]
    public sealed class CompanionSpecialDef
    {
        public string NpcId = "";
        public SpecialKind Kind;
        public float Value;

        /// <summary>대상 (TributeCut이면 세력 ID).</summary>
        public string Target = "";

        /// <summary>합류하지 않고 들르는 인물 (유랑자): 방문 사다리 마지막 장면의 깃발만 있으면 켜진다 (합류·호감 검사 없음).</summary>
        public bool Visitor;
    }

    /// <summary>윤 선생님 호감 구간 (기획 C.1): 선생님 수업의 성공률·숙련 경험치·대성공 보정.</summary>
    [Serializable]
    public sealed class TeacherBondBand
    {
        public int MinAffinity;
        public float CheckBonus;
        public float XpMultiplier = 1f;
        public float GreatSuccessBonus;
    }
}
