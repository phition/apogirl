using System;
using System.Collections.Generic;

namespace ApoGirl.Core.Definitions
{
    /// <summary>
    /// 상태 변화 묶음. 행동 결과, 이벤트 선택지(M3), 탐험 결과 반영에 공통으로 쓴다.
    /// </summary>
    [Serializable]
    public sealed class GameEffect
    {
        public List<StatAmount> Stats = new List<StatAmount>();
        public List<VitalAmount> Vitals = new List<VitalAmount>();
        public List<ResourceAmount> Resources = new List<ResourceAmount>();
        public List<HiddenAmount> Hidden = new List<HiddenAmount>();

        /// <summary>수면 부채 변화량 (+ 증가 / − 상환).</summary>
        public int SleepDebt;

        /// <summary>양심의 무게 변화량.</summary>
        public int Conscience;

        public List<string> SetFlags = new List<string>();
        public List<string> ClearFlags = new List<string>();
        public List<string> AddClues = new List<string>();
        public List<TagAmount> EndingTags = new List<TagAmount>();

        /// <summary>NPC 관계 변화 (NPC_TEACHER는 윤 선생님 호감).</summary>
        public List<NpcChange> Npcs = new List<NpcChange>();

        /// <summary>창고에 넣을 아이템.</summary>
        public List<ItemGrant> Items = new List<ItemGrant>();
    }
}
