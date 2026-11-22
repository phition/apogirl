using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ApoGirl.Core.Balance;
using Newtonsoft.Json;

namespace ApoGirl.Systems.Content
{
    /// <summary>밸런스 표 데이터 읽기·쓰기 (Docs/BALANCE_LAB_PROPOSAL.md).</summary>
    public static class BalanceLabJson
    {
        public const string LayoutResource = "Balance/balance_lab_layout";
        public const string SectionsResource = "Balance/balance_sections";
        public const string SectionsPath = "Assets/_Project/Resources/Balance/balance_sections.json";
        public const string BalanceSourcePath = "Assets/_Project/Scripts/Core/Definitions/BalanceConfig.cs";

        public static BalanceLabLayout ParseLayout(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<BalanceLabLayout>(json ?? "", ContentJson.Settings) ?? new BalanceLabLayout();
            }
            catch (JsonException exception)
            {
                throw new FormatException("밸런스 표 레이아웃 JSON 오류: " + exception.Message, exception);
            }
        }

        public static BalanceSections ParseSections(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new BalanceSections();
            }

            return JsonConvert.DeserializeObject<BalanceSections>(json, ContentJson.Settings) ?? new BalanceSections();
        }

        public static string WriteSections(BalanceSections sections)
        {
            return JsonConvert.SerializeObject(sections, Formatting.Indented) + "\n";
        }

        private static readonly Regex SectionLine = new Regex(@"^\s*//\s*──\s*(.+?)\s*──");
        private static readonly Regex FieldLine = new Regex(@"^        public\s+[\w\[\]<>,\s]+?\s+(\w+)\s*(=|;)");

        /// <summary>BalanceConfig.cs 소스에서 "// ── 절 ──" 줄과 그 아래 필드 이름을 뽑는다 (클래스 BalanceConfig 안만).</summary>
        public static BalanceSections ExtractSections(string source)
        {
            var result = new BalanceSections();
            var inside = false;
            BalanceSection current = null;
            foreach (var line in (source ?? "").Split('\n'))
            {
                if (!inside)
                {
                    inside = line.Contains("class BalanceConfig");
                    continue;
                }

                if (line.StartsWith("    }"))
                {
                    break; // 클래스 끝
                }

                var section = SectionLine.Match(line);
                if (section.Success)
                {
                    current = new BalanceSection { Title = section.Groups[1].Value };
                    result.Sections.Add(current);
                    continue;
                }

                var field = FieldLine.Match(line);
                if (field.Success && !line.Contains("const ") && !line.Contains("static "))
                {
                    if (current == null)
                    {
                        current = new BalanceSection { Title = "-" };
                        result.Sections.Add(current);
                    }

                    current.Fields.Add(field.Groups[1].Value);
                }
            }

            result.Sections.RemoveAll(s => s.Fields.Count == 0);
            return result;
        }

        /// <summary>봇 실행 한 번의 엔딩 표 (지난번 대비용, Temp/BalanceLab/last_run.json).</summary>
        public sealed class BotRunRecord
        {
            public string Stamp = "";
            public string Difficulty = "";
            public int Runs;
            public List<string> Strategies = new List<string>();
            public List<ApoGirl.Core.Simulation.Bots.BotEndingRow> Rows = new List<ApoGirl.Core.Simulation.Bots.BotEndingRow>();
        }

        public static string WriteBotRun(BotRunRecord record)
        {
            return JsonConvert.SerializeObject(record, Formatting.Indented).Replace("\r\n", "\n") + "\n";
        }

        public static BotRunRecord ParseBotRun(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonConvert.DeserializeObject<BotRunRecord>(json, ContentJson.Settings);
            }
            catch (JsonException)
            {
                return null; // 지난번 기록이 망가졌으면 비교 없이 간다
            }
        }
    }
}
