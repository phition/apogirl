using System;

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
