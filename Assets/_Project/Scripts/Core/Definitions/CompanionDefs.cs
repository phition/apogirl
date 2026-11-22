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
