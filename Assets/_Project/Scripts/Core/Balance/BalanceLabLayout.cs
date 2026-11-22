using System.Collections.Generic;

namespace ApoGirl.Core.Balance
{
    /// <summary>밸런스 표의 탭 구성 (Resources/Balance/balance_lab_layout.json).</summary>
    public sealed class BalanceLabLayout
    {
        public List<BalanceLabTab> Tabs = new List<BalanceLabTab>();
    }

    public sealed class BalanceLabTab
    {
        public string Id = "";
        public string Title = "";

        /// <summary>Fields = Rows 목록, Expedition = 지역 × 낮·밤 탐험 모델, Difficulty = 난이도 배율, All = BalanceConfig 전체.</summary>
        public string Kind = "Fields";

        /// <summary>탭 맨 위 한 줄 설명.</summary>
        public string Hint = "";

        public List<BalanceLabRow> Rows = new List<BalanceLabRow>();
    }

    public sealed class BalanceLabRow
    {
        /// <summary>BalanceConfig 필드 이름.</summary>
        public string Field = "";

        /// <summary>탭 안 소제목 (바뀌는 줄에서 소제목을 띄운다).</summary>
        public string Group = "";

        public string Name = "";
        public string Unit = "";
        public string Note = "";

        /// <summary>휠 한 칸 (0 = 자동).</summary>
        public double Step;

        public double? Min;
        public double? Max;

        /// <summary>Now · NextWeek · NextExpedition · NewGame (비우면 모름).</summary>
        public string When = "";
    }

    /// <summary>BalanceConfig.cs의 절 이름 (편집기 Sync Balance Asset이 소스에서 뽑아 Resources/Balance/balance_sections.json에 쓴다).</summary>
    public sealed class BalanceSections
    {
        public List<BalanceSection> Sections = new List<BalanceSection>();
    }

    public sealed class BalanceSection
    {
        public string Title = "";
        public List<string> Fields = new List<string>();
    }
}
