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

        // ── M4 ──
        public int Defense;
        public int Threat;
        public List<FacilityChange> Facilities = new List<FacilityChange>();
        public List<string> AddTraits = new List<string>();
        public List<ProficiencySet> Proficiencies = new List<ProficiencySet>();

        /// <summary>선생님 이동 단계를 최소 이 값으로 (0 = 변화 없음, 1 = 목발 …).</summary>
        public int TeacherMobility;

        public bool RestorePower;

        /// <summary>정수: 모아 둔 빗물(RawWater)을 최대 이만큼 식수로 바꾼다 (비율 PurifyRate, 나머지는 버려진다).</summary>
        public int PurifyWater;
        public float PurifyRate = 1f;

        /// <summary>0보다 크면 이 세기의 밤 습격을 즉시 판정한다 (이벤트 효과 전용).</summary>
        public int RaidStrength;

        // ── M6 ──

        /// <summary>선생님 재감염 발병 (문서 7.2). 한 번 켜지면 되돌릴 수 없다.</summary>
        public bool TeacherReinfect;

        /// <summary>선생님 감염 진행도 변화 (돌봄 −, 악화 +). 재감염 전에는 무시.</summary>
        public int TeacherInfection;

        /// <summary>폭행 트라우마 회복을 앞당기는 주 수 (선생님과의 대화·마음 돌보기).</summary>
        public int TraumaCare;

        /// <summary>함정 설치 개수 (상한 MaxTraps).</summary>
        public int Traps;

        /// <summary>낮 침입 대응 (Fight / Hide / Yield). 거점이 IntrusionRules로 판정한다.</summary>
        public string IntrusionResponse = "";

        /// <summary>다른 효과를 이 효과에 더한다 (목록은 이어 붙이고, 수치는 더하고, 단계·세기는 큰 쪽).</summary>
        public GameEffect MergeFrom(GameEffect other)
        {
            if (other == null)
            {
                return this;
            }

            Stats.AddRange(other.Stats);
            Vitals.AddRange(other.Vitals);
            Resources.AddRange(other.Resources);
            Hidden.AddRange(other.Hidden);
            SleepDebt += other.SleepDebt;
            Conscience += other.Conscience;
            SetFlags.AddRange(other.SetFlags);
            ClearFlags.AddRange(other.ClearFlags);
            AddClues.AddRange(other.AddClues);
            EndingTags.AddRange(other.EndingTags);
            Npcs.AddRange(other.Npcs);
            Items.AddRange(other.Items);
            Defense += other.Defense;
            Threat += other.Threat;
            Facilities.AddRange(other.Facilities);
            AddTraits.AddRange(other.AddTraits);
            Proficiencies.AddRange(other.Proficiencies);
            TeacherMobility = Math.Max(TeacherMobility, other.TeacherMobility);
            RestorePower |= other.RestorePower;
            RaidStrength = Math.Max(RaidStrength, other.RaidStrength);
            TeacherReinfect |= other.TeacherReinfect;
            TeacherInfection += other.TeacherInfection;
            TraumaCare += other.TraumaCare;
            Traps += other.Traps;
            if (!string.IsNullOrEmpty(other.IntrusionResponse)) IntrusionResponse = other.IntrusionResponse;
            return this;
        }
    }
}
