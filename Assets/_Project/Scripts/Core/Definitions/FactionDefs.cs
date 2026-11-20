using System;
using System.Collections.Generic;

namespace ApoGirl.Core.Definitions
{
    /// <summary>세력 평판 단계 (BalanceConfig.Faction* 경계). 순서가 곧 우호도 순서다.</summary>
    public enum FactionTier
    {
        Hostile,
        Wary,
        Neutral,
        Friendly,
        Allied,
    }

    /// <summary>
    /// 외부 세력 (2~4막 보강: 사거리 파·더불어 마트·UBC). 이름·소개는 UI 키 "faction.{Id}.name/desc",
    /// 단계별 근황은 "faction.{Id}.status.{Tier}".
    /// </summary>
    [Serializable]
    public sealed class FactionDef
    {
        public string Id = "";

        /// <summary>처음 평판 (−100~+100).</summary>
        public int StartStanding;

        /// <summary>마지막 접촉 뒤 이만큼 주가 지나면 매주 DriftTarget 쪽으로 WeeklyDrift씩 움직인다 (0 = 안 움직임).</summary>
        public int DriftAfterWeeks;
        public int WeeklyDrift;
        public int DriftTarget;

        /// <summary>단계별 영향 (알게 된 세력만 적용).</summary>
        public List<FactionEffectDef> Effects = new List<FactionEffectDef>();

        public List<TradeOfferDef> Trades = new List<TradeOfferDef>();

        /// <summary>거래 주기 (주). 1 = 매주, 2 = 격주 (운반조가 2주에 한 번 돈다).</summary>
        public int TradeIntervalWeeks = 1;

        /// <summary>이 플래그가 처음 켜지면 평판이 바뀐다 (탐험 조우 선택 "ENC_{조우}_{선택}" 등).</summary>
        public List<FactionFlagReaction> FlagReactions = new List<FactionFlagReaction>();
    }

    /// <summary>평판 단계 범위 [MinTier, MaxTier]에서 적용되는 영향.</summary>
    [Serializable]
    public sealed class FactionEffectDef
    {
        public FactionTier MinTier = FactionTier.Hostile;
        public FactionTier MaxTier = FactionTier.Allied;

        /// <summary>밤 습격 확률·낮 침입 확률·낮 탐험 적대적 생존자 확률 배율 (세력끼리 곱한다).</summary>
        public float RaidChanceMultiplier = 1f;
        public float IntrusionMultiplier = 1f;
        public float HostileMultiplier = 1f;

        /// <summary>밤 습격 세기 가산.</summary>
        public int RaidStrengthBonus;

        /// <summary>매주 정산 때 자원 변화 (통행세 −, 선물 +). 모자라면 있는 만큼만.</summary>
        public List<ResourceAmount> WeeklyResources = new List<ResourceAmount>();

        /// <summary>매주 정산 때 스트레스 변화 (라디오 위안 − 등).</summary>
        public int WeeklyStress;

        /// <summary>세력 카드에 보이는 영향 문장 (UI 키).</summary>
        public string TextKey = "";
    }

    /// <summary>거래 하나: Give를 내고 Get을 받는다. 이름은 UI 키 "trade.{세력}.{Id}".</summary>
    [Serializable]
    public sealed class TradeOfferDef
    {
        public string Id = "";
        public FactionTier MinTier = FactionTier.Friendly;
        public List<ResourceAmount> Give = new List<ResourceAmount>();
        public List<ResourceAmount> Get = new List<ResourceAmount>();

        /// <summary>거래하면 바뀌는 평판.</summary>
        public int StandingChange;

        /// <summary>무전 거래: 받는 물건이 다음 주 시작에 도착한다.</summary>
        public bool Delayed;
    }

    [Serializable]
    public sealed class FactionFlagReaction
    {
        public string Flag = "";
        public int Standing;
        public bool Reveal;

        /// <summary>세력 카드 '최근 일'에 남길 문장 (UI 키, 비면 평판 변화만).</summary>
        public string LogKey = "";
    }

    /// <summary>이벤트 효과의 세력 변화.</summary>
    [Serializable]
    public sealed class FactionChange
    {
        public string FactionId = "";
        public int Standing;

        /// <summary>처음 알게 된다 (허브 세력 칸에 나타남).</summary>
        public bool Reveal;

        /// <summary>세력 카드 '최근 일'에 남길 문장 (UI 키).</summary>
        public string LogKey = "";
    }
}
