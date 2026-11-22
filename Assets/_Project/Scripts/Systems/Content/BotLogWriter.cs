using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ApoGirl.Core.Simulation.Bots;

namespace ApoGirl.Systems.Content
{
    /// <summary>
    /// 밸런스 표 L5 (Docs/BALANCE_LAB_PROPOSAL.md 6.3): Docs/BOT_ENDING_LOG.md 새 절 초안을 만들고 맨 아래에 붙인다.
    /// 형식은 문서의 기존 절과 같다 — "## #N · 날짜 · 제목", 바뀐 것, 봇 설정, 묶음 표, 참고 지표, 방향성. 절 사이는 빈 줄 하나.
    /// </summary>
    public static class BotLogWriter
    {
        public const string DefaultPath = "Docs/BOT_ENDING_LOG.md";
        public const string TitlePlaceholder = "(제목 — 직접 입력)";

        private static readonly Regex Heading = new Regex(@"^## #(\d+)\b", RegexOptions.Multiline);

        /// <summary>봇 기록(last_run.json) 하나로 초안을 만든다: 바뀐 값·설정·참고 지표·방향성은 기록에 담긴 것.</summary>
        public static string Draft(int number, string date, BalanceLabJson.BotRunRecord record)
        {
            return Draft(new DraftInput
            {
                Number = number, Date = date, Record = record,
                Changes = record?.Changes ?? new List<string>(), Settings = record?.Settings ?? "", Reference = record?.Reference ?? "",
            });
        }

        public sealed class DraftInput
        {
            public int Number;
            public string Date = "";

            /// <summary>"DailyFood 2 → 3 (하루 식량)" 같은 줄. 비어 있으면 "밸런스 표 수정 없음".</summary>
            public List<string> Changes = new List<string>();

            /// <summary>"5전략 × 40판, 표준 난이도, 시드 1000~1039, 코드 `abc1234` + 밸런스 표 수정 2개."</summary>
            public string Settings = "";

            public BalanceLabJson.BotRunRecord Record;
            public BalanceLabJson.BotRunRecord Previous;

            /// <summary>참고 지표 한 줄 (사망 원인 등).</summary>
            public string Reference = "";
        }

        /// <summary>문서의 마지막 번호 다음 수 (기록이 없으면 1).</summary>
        public static int NextNumber(string doc)
        {
            var numbers = Heading.Matches(doc ?? "").Cast<Match>().Select(m => int.Parse(m.Groups[1].Value)).ToList();
            return numbers.Count == 0 ? 1 : numbers.Max() + 1;
        }

        public static string Draft(DraftInput input)
        {
            var sb = new StringBuilder();
            sb.Append("## #").Append(input.Number).Append(" · ").Append(input.Date).Append(" · ").AppendLine(TitlePlaceholder);
            sb.AppendLine();
            sb.AppendLine("**바뀐 것** (밸런스 표, 코드 기본값 대비)");
            if (input.Changes.Count == 0)
            {
                sb.AppendLine("- 밸런스 표 수정 없음 (코드 그대로).");
            }

            foreach (var change in input.Changes)
            {
                sb.Append("- ").AppendLine(change);
            }

            sb.AppendLine("- (메모 — 직접 입력)");
            sb.AppendLine();
            sb.Append("**봇 설정:** ").AppendLine(input.Settings);
            sb.AppendLine();

            var record = input.Record;
            if (record != null && record.Rows.Count > 0)
            {
                sb.AppendLine("| 묶음 | 전체 | " + string.Join(" | ", record.Strategies) + " |");
                sb.AppendLine("|---|---|" + string.Concat(record.Strategies.Select(_ => "---|")));
                foreach (var row in record.Rows)
                {
                    sb.AppendLine($"| {row.Name} | {Pct(row.Total)} | " +
                                  string.Join(" | ", record.Strategies.Select(s => Pct(row.ByStrategy.TryGetValue(s, out var v) ? v : 0f))) + " |");
                }

                sb.AppendLine();
            }

            if (!string.IsNullOrEmpty(input.Reference))
            {
                sb.Append("**참고 지표:** ").AppendLine(input.Reference);
                sb.AppendLine();
            }

            sb.AppendLine("**방향성**");
            sb.Append("- (자동 초안) ").AppendLine(!string.IsNullOrEmpty(record?.Direction) ? record.Direction : Direction(record, input.Previous));
            sb.AppendLine("- **마지막 조정 때 볼 것**");
            sb.AppendLine("  - (직접 입력)");
            return sb.ToString().Replace("\r\n", "\n");
        }

        /// <summary>배드 비율의 움직임과 가장 크게 움직인 전략 둘. 지난번이 없으면 지금 배드만.</summary>
        public static string Direction(BalanceLabJson.BotRunRecord now, BalanceLabJson.BotRunRecord before)
        {
            var bad = now?.Rows.LastOrDefault(r => r.Name.StartsWith("배드", StringComparison.Ordinal));
            if (bad == null)
            {
                return "(봇 결과 없음)";
            }

            var target = $"목표 {bad.Min * 100:0}~{bad.Max * 100:0}%";
            var old = before?.Rows.FirstOrDefault(r => r.Name == bad.Name);
            if (old == null)
            {
                return $"배드 {Pct(bad.Total)} ({target}). 비교할 지난 실행이 없다.";
            }

            var moved = now.Strategies
                .Where(s => old.ByStrategy.ContainsKey(s))
                .Select(s => (Id: s, Diff: bad.ByStrategy.TryGetValue(s, out var v) ? v - old.ByStrategy[s] : 0f))
                .Where(m => Math.Abs(m.Diff) >= 0.005f)
                .OrderByDescending(m => Math.Abs(m.Diff))
                .Take(2)
                .ToList();
            var text = $"배드 {Pct(old.Total)} → {Pct(bad.Total)} ({target}, {before.Stamp} 실행 대비).";
            return moved.Count == 0
                ? text + " 전략별로도 움직임이 없다."
                : text + " " + string.Join("·", moved.Select(m => m.Id)) + "에서 가장 크게 움직였다 (" +
                  string.Join(", ", moved.Select(m => $"{m.Id} {(m.Diff > 0 ? "+" : "−")}{Math.Abs(m.Diff) * 100:0}%p")) + ").";
        }

        /// <summary>"사망 원인은 END_CAUSE_INFECTION 57 · END_CAUSE_STARVATION 22" — summary.md "사망 원인" 표와 같은 묶음.</summary>
        public static string DeathCauses(IEnumerable<BotRunResult> runs)
        {
            var deaths = runs.Where(r => !r.Survived).ToList();
            if (deaths.Count == 0)
            {
                return "사망 없음.";
            }

            return "사망 원인은 " + string.Join(" · ", deaths.GroupBy(r => r.EndCause == "" ? "(없음)" : r.EndCause)
                .OrderByDescending(g => g.Count()).Select(g => g.Key + " " + g.Count())) + ".";
        }

        /// <summary>문서 끝에 빈 줄 하나를 두고 붙인다. 예전 기록은 그대로 둔다.</summary>
        public static string Append(string doc, string section)
        {
            return (doc ?? "").Replace("\r\n", "\n").TrimEnd() + "\n\n" + section.Replace("\r\n", "\n").Trim() + "\n";
        }

        /// <summary>붙이기 전 점검: 제목을 안 고쳤거나 번호가 문서와 안 맞으면 이유를 돌려준다.</summary>
        public static string Check(string doc, string section)
        {
            var match = Heading.Match(section ?? "");
            if (!match.Success)
            {
                return "첫 줄이 \"## #번호 · 날짜 · 제목\" 꼴이어야 합니다.";
            }

            if (int.Parse(match.Groups[1].Value) != NextNumber(doc))
            {
                return $"번호가 #{NextNumber(doc)}이어야 합니다 (문서가 그 사이 바뀌었을 수 있습니다).";
            }

            return section.Contains(TitlePlaceholder) ? "제목을 적어 주세요." : null;
        }

        private static string Pct(float value) => (value * 100).ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%";
    }
}
