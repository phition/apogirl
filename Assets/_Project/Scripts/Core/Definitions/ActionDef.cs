using System;
using System.Collections.Generic;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Definitions
{
    /// <summary>행동 카테고리 (문서 6).</summary>
    public enum ActionCategory
    {
        Education,
        Survival,
        Rest,
        Daily,
        Expedition,
        /// <summary>물건·설비 만들기 (Docs/CRAFTING_PROPOSAL.md). 에셋에 저장된 정수가 밀리지 않게 맨 끝에 둔다.</summary>
        Craft,
    }

    /// <summary>배치 가능한 슬롯 (문서 6.1 슬롯 구분).</summary>
    public enum SlotTag
    {
        Both,
        DayOnly,
        NightOnly,
    }

    /// <summary>선생님 지도 방식 (문서 3.2.1).</summary>
    public enum TeachingMode
    {
        None,

        /// <summary>교재를 찾아오고 독학을 지도 (독학 효율 보정).</summary>
        SelfStudyGuided,

        /// <summary>국어·인문·기록 직접 지도 (효율 대폭 보정).</summary>
        TeacherDirect,
    }

    [Serializable]
    public sealed class StatWeight
    {
        public StatType Stat;
        public float Weight;

        public StatWeight()
        {
        }

        public StatWeight(StatType stat, float weight)
        {
            Stat = stat;
            Weight = weight;
        }
    }

    /// <summary>성공 판정 정의. 확률 = 기본 + Σ(스탯×가중치) + 숙련도×가중치 − 페널티.</summary>
    [Serializable]
    public sealed class CheckDef
    {
        public float BaseRate = 0.5f;
        public List<StatWeight> StatWeights = new List<StatWeight>();
        public bool UsesProficiency;
        public ProficiencyType Proficiency;
        public float ProficiencyWeight = 0.05f;

        /// <summary>희망이 BalanceConfig.CrisisHopeThreshold 이상이면 CrisisHopePersuadeBonus를 더한다 (위기 이탈 설득).</summary>
        public bool UsesHope;
    }

    /// <summary>행동 정의 (문서 6: 카테고리 → 세부 행동 → 결과 데이터).</summary>
    [Serializable]
    public sealed class ActionDef
    {
        public string Id = "";
        public ActionCategory Category;
        public SlotTag SlotTag = SlotTag.Both;

        public List<ResourceAmount> ResourceCosts = new List<ResourceAmount>();
        public int FatigueCost;

        /// <summary>스트레스 경중 단계 1~5 (0 = 쉬는 일정). 실제 값은 밸런스 ActionStressMin~Max (StressRules.ActionBase, 기본 5~10).</summary>
        public int StressCost;

        /// <summary>false면 판정 없이 항상 성공.</summary>
        public bool HasCheck;

        public CheckDef Check = new CheckDef();

        /// <summary>성공 기준 성장량. 대성공/실패 배율과 각종 보정이 곱해진다.</summary>
        public List<StatAmount> StatGains = new List<StatAmount>();

        public bool HasProficiency;
        public ProficiencyType Proficiency;
        public int ProficiencyXp;
        public TeachingMode Teaching;

        public GameEffect OnSuccess = new GameEffect();
        public GameEffect OnGreatSuccess = new GameEffect();
        public GameEffect OnFailure = new GameEffect();

        /// <summary>탐험 행동 전용: 시간대와 지역 ID.</summary>
        public ExpeditionTime ExpeditionTime;

        public string LocationId = "";

        // ── M4 ──

        /// <summary>해금 조건 (맞지 않으면 팔레트에 보이지 않고 배치할 수 없다).</summary>
        public ConditionDef Unlock = new ConditionDef();

        /// <summary>밤 습격 방어 보정 (야간 경계 등).</summary>
        public int RaidGuard;

        /// <summary>시설 보정: 시설 레벨당 성장 배율 가산.</summary>
        public string BonusFacilityId = "";
        public float BonusPerLevel;

        /// <summary>위협도 증가 (소음·냄새가 큰 행동, 탐험).</summary>
        public int ThreatGain;

        /// <summary>
        /// 갇혀 있는 동안의 행동 (1주차 창고): 밤 습격이 없고, 이 행동 ID를 조건(ActionIds)으로 건 이벤트만 뜬다.
        /// </summary>
        public bool Confined;

        /// <summary>
        /// 이 행동 뒤 칸 몇 개를 LockActionId로 잠근다 (수학여행: 낮을 고르면 그날 밤도 '여행 중').
        /// </summary>
        public int LockNextSlots;
        public string LockActionId = "";

        /// <summary>거점을 비운 칸 (여행 중인 밤): 밤 습격 때 주인공 전투력 없이 방어도 일부로만 막고, 주인공은 다치지 않는다.</summary>
        public bool AwayFromBase;

        public bool IsRest => Category == ActionCategory.Rest;
        public bool IsExpedition => Category == ActionCategory.Expedition;

        /// <summary>방과후 동아리 활동 (확장 7): BOARDGAME·SINGING·SPORTS·STUDY. 비어 있으면 동아리가 아니다. 한 주에 하나만.</summary>
        public string ClubActivity = "";

        public bool IsClub => !string.IsNullOrEmpty(ClubActivity);

        /// <summary>방송반 송출 (확장 5): SONG·INFO·LETTER. 비어 있으면 방송이 아니다. 한 주에 하나만.</summary>
        public string BroadcastProgram = "";

        public bool IsBroadcast => !string.IsNullOrEmpty(BroadcastProgram);

        /// <summary>쓰려면 여유 전력(용량 − 켜진 기기)이 이만큼 있어야 한다 (PowerRules.Free). 전력을 쓰지는 않는다.</summary>
        public int MinFreePower;

        /// <summary>배터리를 써서 발전 용량을 올리는 행동 (PowerRules.Upgrade, 비용은 공학 숙련에 따라).</summary>
        public bool UpgradesPower;

        /// <summary>한 주 계획에 하나만 놓이는 묶음 (동아리·방송). 비어 있으면 제한 없음.</summary>
        public string WeeklyGroup => IsClub ? "CLUB" : IsBroadcast ? "BROADCAST" : "";

        /// <summary>제작 레시피 (Docs/CRAFTING_PROPOSAL.md 4.2절). Work가 0이면 제작이 아니다 (Unity 직렬화는 null을 두지 않는다).</summary>
        public CraftDef Craft = new CraftDef();

        public bool IsCraft => Craft != null && Craft.Work > 0;
    }

    /// <summary>제작 레시피의 앞 단계 물건 (착수 때 창고에서 뺀다).</summary>
    [Serializable]
    public sealed class CraftItemCost
    {
        public string ItemId = "";
        public int Count = 1;
    }

    /// <summary>
    /// 제작 레시피 (CraftRules). 낮 칸에서 착수하면 뒤 Work − 1칸이 "제작 중"으로 잠기고, 마지막 칸에서 한 번 판정한다.
    /// 재료(ResourceCosts·ItemCosts)는 착수 칸에서 낸다.
    /// </summary>
    [Serializable]
    public sealed class CraftDef
    {
        /// <summary>설계도의 줄 (WORKBENCH·BAG·WEAPON·ARMOR·LIGHT·WATER·DECOY)과 단계 1~3.</summary>
        public string Line = "";
        public int Tier;

        /// <summary>완성까지 칸 수 (1~3). 0이면 제작이 아니다.</summary>
        public int Work;

        /// <summary>만드는 아이템과 개수 (설비·손질이면 비움).</summary>
        public string Product = "";
        public int Count = 1;

        public List<CraftItemCost> ItemCosts = new List<CraftItemCost>();

        /// <summary>완성 때 켜는 설비 깃발 (WORKBENCH 등).</summary>
        public List<string> SetFlags = new List<string>();

        /// <summary>창고 보관 상한 (만드는 아이템 개수, 0 = 없음).</summary>
        public int MaxOwned;

        /// <summary>무기 손질: 물건 대신 찬 무기의 내구를 채운다 (BalanceConfig.WeaponMaintainRestore).</summary>
        public bool MaintainWeapon;

        /// <summary>카드 아래 흐리게 보일 다음 단계 레시피.</summary>
        public string Next = "";
    }
}
