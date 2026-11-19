using System;
using System.Collections.Generic;
using ApoGirl.Core.Definitions;

namespace ApoGirl.Core.Simulation.Bots
{
    // M7-1: 봇 시뮬레이터 설정. 원본은 Assets/_Project/Balance/bot_strategies.json (콘텐츠가 아닌 개발 도구 데이터).

    /// <summary>봇 시뮬레이터 설정 전체.</summary>
    [Serializable]
    public sealed class BotConfigDef
    {
        /// <summary>전략마다 돌릴 판 수 (에디터 메뉴 기본값).</summary>
        public int RunsPerStrategy = 40;

        /// <summary>판 시드 = BaseSeed + 판 번호. 전략끼리 같은 시드를 써서 비교한다.</summary>
        public int BaseSeed = 1000;

        /// <summary>안전장치: 이 주를 넘기면 판을 끊는다.</summary>
        public int MaxWeeks = 60;

        /// <summary>행동 묶음 (이름 → 후보). 칸·규칙의 후보에 행동 ID 대신 쓸 수 있다.</summary>
        public Dictionary<string, BotGroupDef> Groups = new Dictionary<string, BotGroupDef>();

        /// <summary>모든 전략에 전략 규칙 다음으로 적용하는 규칙 (치료·체력 보호 등).</summary>
        public List<BotRuleDef> CommonRules = new List<BotRuleDef>();

        /// <summary>모든 전략에 더하는 이벤트 선택 가중치.</summary>
        public List<BotChoiceBiasDef> CommonChoiceBias = new List<BotChoiceBiasDef>();

        /// <summary>한 문항에서 여러 번 고르는 프롤로그 (예: 성격 유형 네 글자). 나머지 문항은 선택지 하나를 무작위로.</summary>
        public List<BotPrologueGroupDef> PrologueGroups = new List<BotPrologueGroupDef>();

        public List<BotStrategyDef> Strategies = new List<BotStrategyDef>();
    }

    public enum BotPickMode
    {
        /// <summary>해금된 첫 후보.</summary>
        First,

        /// <summary>해금된 후보 중 가중 무작위.</summary>
        Random,
    }

    /// <summary>행동 묶음.</summary>
    [Serializable]
    public sealed class BotGroupDef
    {
        public BotPickMode Pick = BotPickMode.First;
        public List<string> Actions = new List<string>();

        /// <summary>Random일 때 Actions와 같은 순서의 가중치 (비면 모두 1).</summary>
        public List<float> Weights = new List<float>();
    }

    /// <summary>전략 하나: 주간 틀 14칸 + 조건부 규칙 + 선택 가중치.</summary>
    [Serializable]
    public sealed class BotStrategyDef
    {
        public string Id = "";
        public string Description = "";

        /// <summary>1~7일 낮 칸. 칸마다 "후보|후보|…" (행동 ID 또는 묶음 이름). 아무것도 안 되면 휴식.</summary>
        public List<string> Day = new List<string>();

        /// <summary>1~7일 밤 칸.</summary>
        public List<string> Night = new List<string>();

        public List<BotRuleDef> Rules = new List<BotRuleDef>();
        public List<BotChoiceBiasDef> ChoiceBias = new List<BotChoiceBiasDef>();

        /// <summary>조우에서 첫 선택지(대개 다가가기·챙기기)의 가중치. 나머지는 1.</summary>
        public float EncounterFirstChoiceWeight = 1f;
    }

    /// <summary>조건부 규칙: 조건이 맞으면 지정 칸을 후보로 바꾼다 (후보가 하나도 안 되면 그대로 둔다).</summary>
    [Serializable]
    public sealed class BotRuleDef
    {
        public string Note = "";

        /// <summary>주 계획 시점(그 주 1일 낮)에 판정한다. 비면 항상.</summary>
        public ConditionDef When = new ConditionDef();

        /// <summary>"D1"~"D7"(낮), "N1"~"N7"(밤).</summary>
        public List<string> Slots = new List<string>();

        /// <summary>"후보|후보|…"</summary>
        public string Use = "";
    }

    /// <summary>이벤트 선택지 가중치: Ids 중 하나면 Weight (여러 개 맞으면 곱한다). 기본 1.</summary>
    [Serializable]
    public sealed class BotChoiceBiasDef
    {
        public List<string> Ids = new List<string>();
        public float Weight = 1f;
    }

    /// <summary>한 문항에서 묶음마다 하나씩 고른다 (예: [E,I], [S,N], [T,F], [J,P]).</summary>
    [Serializable]
    public sealed class BotPrologueGroupDef
    {
        public string QuestionId = "";
        public List<List<string>> Sets = new List<List<string>>();
    }
}
