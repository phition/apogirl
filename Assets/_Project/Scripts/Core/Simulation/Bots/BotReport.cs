using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Expedition;

namespace ApoGirl.Core.Simulation.Bots
{
    /// <summary>봇 판 결과를 CSV와 요약 문서로 묶는다 (파일 쓰기는 호출하는 쪽).</summary>
    public static class BotReport
    {
        public const string RunsFile = "runs.csv";
        public const string WeeksFile = "weeks.csv";
        public const string ExpeditionsFile = "expeditions.csv";
        public const string SummaryFile = "summary.md";

        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        /// <summary>M7-c 엔딩 분포 목표 (봇 기준, 표준 난이도). 묶음 이름 → (최소, 최대, 해당 구분).</summary>
        public static readonly (string Name, float Min, float Max, EndingCategory[] Categories)[] EndingTargets =
        {
            ("기본 (일반·균형)", 0.30f, 0.45f, new[] { EndingCategory.Normal, EndingCategory.Balanced }),
            ("특화", 0.30f, 0.40f, new[] { EndingCategory.Specialized }),
            ("관계·서사", 0.10f, 0.20f, new[] { EndingCategory.Relationship, EndingCategory.Narrative }),
            ("히든", 0.01f, 0.05f, new[] { EndingCategory.Hidden }),
            ("배드", 0.10f, 0.20f, new[] { EndingCategory.Bad }),
        };

        private static readonly (string Name, Func<BotWeekSample, int> Get)[] WeekMetrics =
        {
            ("food", w => w.Food),
            ("water", w => w.Water),
            ("medicine", w => w.Medicine),
            ("fuel", w => w.Fuel),
            ("materials", w => w.Materials),
            ("hp", w => w.Hp),
            ("stress", w => w.Stress),
            ("fatigue", w => w.Fatigue),
            ("infection", w => w.Infection),
            ("sleep_debt", w => w.SleepDebt),
            ("teacher_infection", w => w.TeacherInfection),
            ("threat", w => w.Threat),
            ("defense", w => w.Defense),
        };

        private static readonly string[] ValueNames = BotSimulator.TrackedValues().Select(v => v.Name).ToArray();

        public static Dictionary<string, string> Build(IReadOnlyList<BotRunResult> runs, string header = "")
        {
            return new Dictionary<string, string>
            {
                [RunsFile] = RunsCsv(runs),
                [WeeksFile] = WeeksCsv(runs),
                [ExpeditionsFile] = ExpeditionsCsv(runs),
                [SummaryFile] = Summary(runs, header),
            };
        }

        // ── CSV ──

        public static string RunsCsv(IEnumerable<BotRunResult> runs)
        {
            var sb = new StringBuilder();
            sb.AppendLine("strategy,seed,blood,personality,status,end_week,end_cause,ending,category,day_exp,night_exp,downs,raids,raids_repelled,companions,events," +
                          "teacher_reinfected,teacher_alive,blood_known,need_pack,got_pack,cure_attempted,cured,final_choice,avg_sleep_debt,max_sleep_debt,final_food,final_water," +
                          "eligible_endings,flags," + string.Join(",", ValueNames));
            foreach (var r in runs)
            {
                var last = r.Weeks.LastOrDefault();
                sb.AppendLine(string.Join(",",
                    r.StrategyId, r.Seed, r.BloodType, r.Personality, r.Status, r.EndWeek, r.EndCause, r.EndingId, r.EndingCategory?.ToString() ?? "",
                    r.Expeditions.Count(e => !e.Night), r.Expeditions.Count(e => e.Night), r.Expeditions.Count(e => e.Status == ExtractStatus.Down),
                    r.Raids, r.RaidsRepelled, r.Companions, r.Events,
                    B(r.TeacherReinfected), B(r.TeacherAlive), B(r.TeacherBloodKnown), B(r.NeededBloodPack), B(r.GotBloodPack), B(r.CureAttempted), B(r.TeacherCured),
                    r.FinalChoice, F(r.Weeks.Count == 0 ? 0 : r.Weeks.Average(w => w.SleepDebt)), r.Weeks.Count == 0 ? 0 : r.Weeks.Max(w => w.SleepDebt),
                    last?.Food ?? 0, last?.Water ?? 0,
                    string.Join(";", r.EligibleEndings), string.Join(";", r.TrackedFlags.OrderBy(f => f)),
                    string.Join(",", ValueNames.Select(n => r.FinalValues.TryGetValue(n, out var v) ? v.ToString(Inv) : ""))));
            }

            return sb.ToString();
        }

        /// <summary>전략·주·지표별 평균과 하위·상위 10% (그 주까지 살아 있는 판만).</summary>
        public static string WeeksCsv(IEnumerable<BotRunResult> runs)
        {
            var sb = new StringBuilder();
            sb.AppendLine("strategy,week,metric,runs,mean,p10,p90");
            foreach (var group in runs.GroupBy(r => r.StrategyId))
            {
                var maxWeek = group.SelectMany(r => r.Weeks).Select(w => w.Week).DefaultIfEmpty(0).Max();
                for (var week = 1; week <= maxWeek; week++)
                {
                    var samples = group.Select(r => r.Weeks.FirstOrDefault(w => w.Week == week)).Where(w => w != null).ToList();
                    if (samples.Count == 0)
                    {
                        continue;
                    }

                    foreach (var (name, get) in WeekMetrics)
                    {
                        var values = samples.Select(get).OrderBy(v => v).ToList();
                        sb.AppendLine(string.Join(",", group.Key, week, name, values.Count, F(values.Average()), Percentile(values, 0.1f), Percentile(values, 0.9f)));
                    }
                }
            }

            return sb.ToString();
        }

        /// <summary>전략·지역·낮밤별 탐험 횟수, 쓰러짐률, 평균 수확.</summary>
        public static string ExpeditionsCsv(IEnumerable<BotRunResult> runs)
        {
            var sb = new StringBuilder();
            sb.AppendLine("strategy,location,time,count,down_rate,bite_rate,avg_food,avg_water,avg_medicine,avg_materials,avg_items");
            var rows = runs.SelectMany(r => r.Expeditions.Select(e => (r.StrategyId, e)))
                .GroupBy(x => (x.StrategyId, x.e.LocationId, x.e.Night))
                .OrderBy(g => g.Key.StrategyId).ThenBy(g => g.Key.LocationId).ThenBy(g => g.Key.Night);
            foreach (var g in rows)
            {
                var list = g.Select(x => x.e).ToList();
                sb.AppendLine(string.Join(",", g.Key.StrategyId, g.Key.LocationId, g.Key.Night ? "Night" : "Day", list.Count,
                    F(list.Count(e => e.Status == ExtractStatus.Down) / (float)list.Count), F(list.Count(e => e.Bitten) / (float)list.Count),
                    F(list.Average(e => e.Food)), F(list.Average(e => e.Water)), F(list.Average(e => e.Medicine)), F(list.Average(e => e.Materials)), F(list.Average(e => e.Items))));
            }

            return sb.ToString();
        }

        // ── 요약 ──

        public static string Summary(IReadOnlyList<BotRunResult> runs, string header = "")
        {
            var sb = new StringBuilder();
            sb.AppendLine("# 봇 시뮬레이션 요약 (M7-1)");
            sb.AppendLine();
            if (!string.IsNullOrEmpty(header))
            {
                sb.AppendLine(header);
                sb.AppendLine();
            }

            sb.AppendLine($"판 수 {runs.Count}. 엔딩 구분 비율은 판 전체 기준, 생존 = 52주 구조.");
            sb.AppendLine();

            // 전략별 개요
            sb.AppendLine("## 전략별 개요");
            sb.AppendLine();
            sb.AppendLine("| 전략 | 판 | 생존 | 평균 사망 주 | 탐험/주 (낮·밤) | 쓰러짐률 낮·밤 | 수면 부채 평균·최대 | 습격 (막음) | 동료 | 완치 시도·성공 | 생존자 쫓김·강탈·구타 (낮 탐험당) | 낮 침입 (크게 당함) |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (var g in runs.GroupBy(r => r.StrategyId))
            {
                var list = g.ToList();
                var dead = list.Where(r => !r.Survived).ToList();
                var weeks = Math.Max(1, list.Sum(r => r.Weeks.Count));
                var day = list.SelectMany(r => r.Expeditions).Where(e => !e.Night).ToList();
                var night = list.SelectMany(r => r.Expeditions).Where(e => e.Night).ToList();
                var debts = list.SelectMany(r => r.Weeks).Select(w => w.SleepDebt).DefaultIfEmpty(0).ToList();
                sb.AppendLine($"| {g.Key} | {list.Count} | {P(list.Count(r => r.Survived), list.Count)} | {(dead.Count == 0 ? "-" : F(dead.Average(r => r.EndWeek)))} " +
                              $"| {F(day.Count / (float)weeks)} · {F(night.Count / (float)weeks)} | {Rate(day)} · {Rate(night)} " +
                              $"| {F(debts.Average())} · {debts.Max()} | {F(list.Average(r => r.Raids))} ({F(list.Average(r => r.RaidsRepelled))}) " +
                              $"| {F(list.Average(r => r.Companions))} | {P(list.Count(r => r.CureAttempted), list.Count)} · {P(list.Count(r => r.TeacherCured), list.Count)} " +
                              $"| {P(day.Count(e => e.Hostile == HostileOutcome.Evicted), day.Count)} · {P(day.Count(e => e.Hostile == HostileOutcome.Robbed), day.Count)} · {P(day.Count(e => e.Hostile == HostileOutcome.Beaten), day.Count)} " +
                              $"| {F(list.Average(r => r.Intrusions))} ({F(list.Average(r => r.IntrusionsLost))}) |");
            }

            sb.AppendLine();

            // 엔딩 분포 vs 목표
            sb.AppendLine("## 엔딩 분포 (M7-c 목표 대비)");
            sb.AppendLine();
            var strategies = runs.Select(r => r.StrategyId).Distinct().ToList();
            sb.AppendLine("| 묶음 | 목표 | 전체 | " + string.Join(" | ", strategies) + " |");
            sb.AppendLine("|---|---|---|" + string.Concat(strategies.Select(_ => "---|")));
            foreach (var (name, min, max, categories) in EndingTargets)
            {
                bool In(BotRunResult r) => r.EndingCategory.HasValue && categories.Contains(r.EndingCategory.Value);
                var total = runs.Count == 0 ? 0f : runs.Count(In) / (float)runs.Count;
                var mark = total < min ? " ▼" : total > max ? " ▲" : " ✓";
                sb.AppendLine($"| {name} | {min:P0}~{max:P0} | {total:P0}{mark} | " +
                              string.Join(" | ", strategies.Select(s => P(runs.Count(r => r.StrategyId == s && In(r)), runs.Count(r => r.StrategyId == s)))) + " |");
            }

            var none = runs.Count(r => !r.EndingCategory.HasValue);
            if (none > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"엔딩이 정해지지 않은 판: {none}");
            }

            sb.AppendLine();

            // 엔딩별
            sb.AppendLine("## 엔딩별 횟수");
            sb.AppendLine();
            sb.AppendLine("| 엔딩 | 구분 | 횟수 | 비율 | " + string.Join(" | ", strategies) + " |");
            sb.AppendLine("|---|---|---|---|" + string.Concat(strategies.Select(_ => "---|")));
            foreach (var g in runs.GroupBy(r => (r.EndingId, r.EndingCategory)).OrderByDescending(g => g.Count()))
            {
                sb.AppendLine($"| {(g.Key.EndingId == "" ? "(없음)" : g.Key.EndingId)} | {g.Key.EndingCategory} | {g.Count()} | {P(g.Count(), runs.Count)} | " +
                              string.Join(" | ", strategies.Select(s => g.Count(r => r.StrategyId == s).ToString(Inv))) + " |");
            }

            sb.AppendLine();

            // 사망 원인
            var deaths = runs.Where(r => !r.Survived).ToList();
            sb.AppendLine("## 사망 원인과 주차");
            sb.AppendLine();
            if (deaths.Count == 0)
            {
                sb.AppendLine("사망 없음.");
            }
            else
            {
                sb.AppendLine("| 원인 | 횟수 | 주차 (최소·중앙·최대) | 전략별 |");
                sb.AppendLine("|---|---|---|---|");
                foreach (var g in deaths.GroupBy(r => r.EndCause == "" ? "(없음)" : r.EndCause).OrderByDescending(g => g.Count()))
                {
                    var weeks = g.Select(r => r.EndWeek).OrderBy(w => w).ToList();
                    sb.AppendLine($"| {g.Key} | {g.Count()} | {weeks.First()} · {Percentile(weeks, 0.5f)} · {weeks.Last()} | " +
                                  string.Join(", ", g.GroupBy(r => r.StrategyId).Select(s => s.Key + " " + s.Count())) + " |");
                }
            }

            sb.AppendLine();

            // 선생님·수혈 갈래
            sb.AppendLine("## 선생님·수혈 갈래");
            sb.AppendLine();
            sb.AppendLine("| 전략 | 재감염 | 선생님 생존 | 혈액형 확인 | 혈액 팩 필요 → 확보 | 완치 시도 | 완치 | 마지막 선택 |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|");
            foreach (var g in runs.GroupBy(r => r.StrategyId))
            {
                var list = g.ToList();
                var needed = list.Where(r => r.NeededBloodPack).ToList();
                var finals = string.Join(", ", list.Where(r => r.FinalChoice != "").GroupBy(r => r.FinalChoice).OrderByDescending(f => f.Count()).Select(f => f.Key + " " + f.Count()));
                sb.AppendLine($"| {g.Key} | {P(list.Count(r => r.TeacherReinfected), list.Count)} | {P(list.Count(r => r.TeacherAlive), list.Count)} " +
                              $"| {P(list.Count(r => r.TeacherBloodKnown), list.Count)} | {needed.Count} → {needed.Count(r => r.GotBloodPack)} " +
                              $"| {P(list.Count(r => r.CureAttempted), list.Count)} | {P(list.Count(r => r.TeacherCured), list.Count)} | {(finals == "" ? "-" : finals)} |");
            }

            sb.AppendLine();

            // 엔딩 조건 충족률 (구조된 판)
            var rescued = runs.Where(r => r.Survived).ToList();
            sb.AppendLine("## 엔딩 조건 충족률 (구조된 판, 판정 전)");
            sb.AppendLine();
            sb.AppendLine("조건만 본 비율. 실제 엔딩은 우선순위 → 점수로 하나가 고른다.");
            sb.AppendLine();
            sb.AppendLine("| 엔딩 | 조건 충족 | 실제 엔딩 | " + string.Join(" | ", strategies) + " |");
            sb.AppendLine("|---|---|---|" + string.Concat(strategies.Select(_ => "---|")));
            foreach (var id in rescued.SelectMany(r => r.EligibleEndings).Distinct().OrderByDescending(id => rescued.Count(r => r.EligibleEndings.Contains(id))))
            {
                sb.AppendLine($"| {id} | {P(rescued.Count(r => r.EligibleEndings.Contains(id)), rescued.Count)} | {P(rescued.Count(r => r.EndingId == id), rescued.Count)} | " +
                              string.Join(" | ", strategies.Select(s => P(rescued.Count(r => r.StrategyId == s && r.EligibleEndings.Contains(id)), rescued.Count(r => r.StrategyId == s)))) + " |");
            }

            sb.AppendLine();

            // 끝났을 때 수치
            sb.AppendLine("## 끝났을 때 수치 평균 (구조된 판)");
            sb.AppendLine();
            sb.AppendLine("| 수치 | 전체 (최소·중앙·최대) | " + string.Join(" | ", strategies) + " |");
            sb.AppendLine("|---|---|" + string.Concat(strategies.Select(_ => "---|")));
            foreach (var name in ValueNames)
            {
                var values = rescued.Where(r => r.FinalValues.ContainsKey(name)).Select(r => r.FinalValues[name]).OrderBy(v => v).ToList();
                if (values.Count == 0)
                {
                    continue;
                }

                sb.AppendLine($"| {name} | {F(values.Average())} ({values.First()}·{Percentile(values, 0.5f)}·{values.Last()}) | " +
                              string.Join(" | ", strategies.Select(s =>
                              {
                                  var list = rescued.Where(r => r.StrategyId == s && r.FinalValues.ContainsKey(name)).Select(r => r.FinalValues[name]).ToList();
                                  return list.Count == 0 ? "-" : F(list.Average());
                              })) + " |");
            }

            sb.AppendLine();

            // 갈래 플래그
            sb.AppendLine("## 갈래 플래그 도달률 (전체 판)");
            sb.AppendLine();
            sb.AppendLine("| 플래그 | 전체 | " + string.Join(" | ", strategies) + " |");
            sb.AppendLine("|---|---|" + string.Concat(strategies.Select(_ => "---|")));
            foreach (var flag in BotSimulator.TrackedFlagIds)
            {
                sb.AppendLine($"| {flag} | {P(runs.Count(r => r.TrackedFlags.Contains(flag)), runs.Count)} | " +
                              string.Join(" | ", strategies.Select(s => P(runs.Count(r => r.StrategyId == s && r.TrackedFlags.Contains(flag)), runs.Count(r => r.StrategyId == s)))) + " |");
            }

            sb.AppendLine();

            // 지역별 탐험
            sb.AppendLine("## 지역별 탐험 (전 전략 합계)");
            sb.AppendLine();
            sb.AppendLine("| 지역 | 시간 | 횟수 | 쓰러짐률 | 물림률 | 평균 식량 | 평균 물 | 평균 약 | 평균 자재 |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|---|");
            foreach (var g in runs.SelectMany(r => r.Expeditions).GroupBy(e => (e.LocationId, e.Night)).OrderBy(g => g.Key.LocationId).ThenBy(g => g.Key.Night))
            {
                var list = g.ToList();
                sb.AppendLine($"| {g.Key.LocationId} | {(g.Key.Night ? "밤" : "낮")} | {list.Count} | {Rate(list)} | {P(list.Count(e => e.Bitten), list.Count)} " +
                              $"| {F(list.Average(e => e.Food))} | {F(list.Average(e => e.Water))} | {F(list.Average(e => e.Medicine))} | {F(list.Average(e => e.Materials))} |");
            }

            sb.AppendLine();

            // 막 끝 자원 (13·26·39·52주)
            sb.AppendLine("## 막 끝 자원 평균 (식량 / 물 / 약, 살아 있는 판)");
            sb.AppendLine();
            sb.AppendLine("| 전략 | 13주 | 26주 | 39주 | 52주 |");
            sb.AppendLine("|---|---|---|---|---|");
            foreach (var g in runs.GroupBy(r => r.StrategyId))
            {
                var cells = new[] { 13, 26, 39, 52 }.Select(week =>
                {
                    var samples = g.Select(r => r.Weeks.FirstOrDefault(w => w.Week == week)).Where(w => w != null).ToList();
                    return samples.Count == 0 ? "-" : $"{F(samples.Average(w => w.Food))} / {F(samples.Average(w => w.Water))} / {F(samples.Average(w => w.Medicine))}";
                });
                sb.AppendLine($"| {g.Key} | " + string.Join(" | ", cells) + " |");
            }

            return sb.ToString();
        }

        // ── 도우미 ──

        private static string Rate(IReadOnlyCollection<BotExpeditionSample> list)
        {
            return list.Count == 0 ? "-" : P(list.Count(e => e.Status == ExtractStatus.Down), list.Count);
        }

        private static string P(int count, int total)
        {
            return total == 0 ? "-" : (count / (float)total).ToString("P0", Inv);
        }

        private static string F(double value)
        {
            return value.ToString("0.##", Inv);
        }

        private static string B(bool value)
        {
            return value ? "1" : "0";
        }

        /// <summary>정렬된 목록의 백분위 (가까운 순위).</summary>
        public static int Percentile(IReadOnlyList<int> sorted, float p)
        {
            if (sorted.Count == 0)
            {
                return 0;
            }

            var index = (int)Math.Round(p * (sorted.Count - 1));
            return sorted[Math.Max(0, Math.Min(sorted.Count - 1, index))];
        }
    }
}
