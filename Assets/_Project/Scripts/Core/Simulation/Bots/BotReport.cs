using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Rules;

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

        /// <summary>M7-c 엔딩 분포 목표 (봇 기준, 표준 난이도. 2026-11-20 배드 50% 중심으로 상향 (부상 도입 뒤)). 묶음 이름 → (최소, 최대, 해당 구분).</summary>
        public static readonly (string Name, float Min, float Max, EndingCategory[] Categories)[] EndingTargets =
        {
            // 배드 50% 뒤 나머지 절반을 예전 비율대로 (2026-11-20)
            ("기본 (일반·균형)", 0.15f, 0.25f, new[] { EndingCategory.Normal, EndingCategory.Balanced }),
            ("특화·기록", 0.15f, 0.25f, new[] { EndingCategory.Specialized, EndingCategory.Record }),
            ("관계·서사", 0.06f, 0.12f, new[] { EndingCategory.Relationship, EndingCategory.Narrative }),
            ("히든", 0.01f, 0.03f, new[] { EndingCategory.Hidden }),
            ("배드", 0.45f, 0.55f, new[] { EndingCategory.Bad }),
        };

        private static readonly (string Name, Func<BotWeekSample, int> Get)[] WeekMetrics =
        {
            ("food", w => w.Food),
            ("water", w => w.Water),
            ("raw_water", w => w.RawWater),
            ("cleanliness", w => w.Cleanliness),
            ("injured", w => w.Injured),
            ("medicine", w => w.Medicine),
            ("fuel", w => w.Fuel),
            ("materials", w => w.Materials),
            ("hp", w => w.Hp),
            ("stress", w => w.Stress),
            ("stress_peak", w => w.StressPeak),
            ("stress_mean", w => w.StressMean),
            ("stress_up", w => w.StressUp),
            ("stress_down", w => w.StressDown),
            ("fatigue", w => w.Fatigue),
            ("infection", w => w.Infection),
            ("sleep_debt", w => w.SleepDebt),
            ("teacher_infection", w => w.TeacherInfection),
            ("threat", w => w.Threat),
            ("defense", w => w.Defense),
            ("companions", w => w.Companions),
            ("companions_injured", w => w.CompanionsInjured),
            ("companions_minor", w => w.CompanionsMinor),
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

            // 세 막 엔딩: 위 표의 엔딩은 대표(막 Ⅲ 그 뒤의 나, 배드는 그 엔딩)다. 구조된 판의 막 Ⅰ·Ⅱ 분포는 따로 센다
            var rescuedRuns = runs.Where(r => r.RescueId != "").ToList();
            if (rescuedRuns.Count > 0)
            {
                foreach (var (title, pick) in new (string, Func<BotRunResult, string>)[] { ("구조 장면 (막 Ⅰ)", r => r.RescueId), ("회고 축 (막 Ⅱ)", r => r.ReflectionId) })
                {
                    sb.AppendLine("## " + title + " — 구조된 판 " + rescuedRuns.Count.ToString(Inv));
                    sb.AppendLine();
                    sb.AppendLine("| 조각 | 횟수 | 비율 |");
                    sb.AppendLine("|---|---|---|");
                    foreach (var g in rescuedRuns.GroupBy(pick).OrderByDescending(g => g.Count()))
                    {
                        sb.AppendLine($"| {(g.Key == "" ? "(없음)" : g.Key)} | {g.Count()} | {P(g.Count(), rescuedRuns.Count)} |");
                    }

                    sb.AppendLine();
                }
            }

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

            AppendEndingReach(sb, runs, rescued);
            AppendRecords(sb, runs, rescued, strategies);

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

            // 동료 탐험 (역할 재기획 8절): 외출 횟수·물자·다침, 동시에 부상인 동료 (목표 평균 1명 미만)
            sb.AppendLine("## 동료 탐험 (역할 재기획 P5)");
            sb.AppendLine();
            sb.AppendLine("| 전략 | 외출 / 동료·주 | 물자 / 외출 | 물자 / 주 | 기호품 | 가벼운 상처 · 부상 (외출당) | 동시 부상 평균 · 최대 | 적성 역할 비율 |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|");
            foreach (var g in runs.GroupBy(r => r.StrategyId))
            {
                var list = g.ToList();
                var outings = list.SelectMany(r => r.Outings).ToList();
                var weeks = list.SelectMany(r => r.Weeks).ToList();
                var companionWeeks = Math.Max(1, weeks.Sum(w => w.Companions));
                var withCompanions = weeks.Where(w => w.Companions > 0).ToList();
                sb.AppendLine($"| {g.Key} | {F(outings.Count / (float)companionWeeks)} | {(outings.Count == 0 ? "-" : F(outings.Average(o => o.Resources)))} " +
                              $"| {F(outings.Sum(o => o.Resources) / (float)Math.Max(1, weeks.Count))} | {P(outings.Count(o => o.Item), outings.Count)} " +
                              $"| {P(outings.Count(o => o.Wound == OutingWound.Minor), outings.Count)} · {P(outings.Count(o => o.Wound == OutingWound.Severe), outings.Count)} " +
                              $"| {(withCompanions.Count == 0 ? "-" : F(withCompanions.Average(w => w.CompanionsInjured)))} · {weeks.Select(w => w.CompanionsInjured).DefaultIfEmpty(0).Max()} " +
                              $"| {P(weeks.Sum(w => w.CompanionsOnAptitude), companionWeeks)} |");
            }

            sb.AppendLine();

            // CD 수집 (기획 A): 판 끝에 가진 CD 수, 8장을 다 모은 판
            sb.AppendLine("## CD 수집 (기획 A)");
            sb.AppendLine();
            sb.AppendLine("| 전략 | 평균 CD | 1장 이상 | 8장 모두 |");
            sb.AppendLine("|---|---|---|---|");
            foreach (var g in runs.GroupBy(r => r.StrategyId))
            {
                var list = g.ToList();
                sb.AppendLine($"| {g.Key} | {F((float)list.Average(r => r.Cds))} | {P(list.Count(r => r.Cds > 0), list.Count)} | {P(list.Count(r => r.Cds >= 8), list.Count)} |");
            }

            sb.AppendLine();

            // 동아리 (확장 7): 판마다 동아리 횟수, 동아리 개인 장면
            sb.AppendLine("## 동아리 (확장 7)");
            sb.AppendLine();
            sb.AppendLine("| 전략 | 평균 동아리 횟수 | 개인 장면 평균 | 개인 장면 1편 이상 |");
            sb.AppendLine("|---|---|---|---|");
            foreach (var g in runs.GroupBy(r => r.StrategyId))
            {
                var list = g.ToList();
                sb.AppendLine($"| {g.Key} | {F((float)list.Average(r => r.ClubSessions))} | {F((float)list.Average(r => r.ClubScenes))} | {P(list.Count(r => r.ClubScenes > 0), list.Count)} |");
            }

            sb.AppendLine();

            // 방송반 송출 (확장 5): 판마다 방송 횟수, 응답 장면 수
            sb.AppendLine("## 방송 (확장 5)");
            sb.AppendLine();
            sb.AppendLine("| 전략 | 평균 방송 횟수 | 응답 장면 평균 | 방송한 판 |");
            sb.AppendLine("|---|---|---|---|");
            foreach (var g in runs.GroupBy(r => r.StrategyId))
            {
                var list = g.ToList();
                sb.AppendLine($"| {g.Key} | {F((float)list.Average(r => r.Broadcasts))} | {F((float)list.Average(r => r.BroadcastReplies))} | {P(list.Count(r => r.Broadcasts > 0), list.Count)} |");
            }

            sb.AppendLine();

            // 고양이 (확장 2): 끝났을 때 눌러앉아 있던 판, 동료 없이 끝난 판 가운데 고양이가 있던 판
            sb.AppendLine("## 고양이 (확장 2)");
            sb.AppendLine();
            sb.AppendLine("| 전략 | 고양이와 끝남 | 동료 없이 끝난 판 중 고양이 |");
            sb.AppendLine("|---|---|---|");
            foreach (var g in runs.GroupBy(r => r.StrategyId))
            {
                var list = g.ToList();
                var alone = list.Where(r => r.Companions == 0).ToList();
                sb.AppendLine($"| {g.Key} | {P(list.Count(r => r.Cat), list.Count)} | {(alone.Count == 0 ? "-" : P(alone.Count(r => r.Cat), alone.Count))} |");
            }

            sb.AppendLine();

            // 막 끝 자원 (13·26·39·52주)
            AppendCraft(sb, runs);
            AppendStress(sb, runs);

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

        /// <summary>
        /// 스트레스 (사용자 요청 2026-11-25): 막(13주)마다 주 끝·주 중 최고·칸 평균·한 주에 오른 합·내린 합 (살아 있는 판의 주 평균),
        /// 칸 종류별 한 칸 평균 변화 (그 칸 시작의 식사·저녁 허기 몫도 들어간다).
        /// </summary>
        /// <summary>
        /// 엔딩 P6 (Docs/PROPOSAL_ENDINGS_EXPANSION 11절): 막마다 조각이 몇 판에서 조건을 채웠고 몇 판에서 뽑혔나,
        /// 그리고 봇 판에서 한 번도 조건을 채우지 못한 조각. 구조된 판만 본다 (엔딩 조건은 구조된 뒤에 판정한다).
        /// </summary>
        private static void AppendEndingReach(StringBuilder sb, IReadOnlyList<BotRunResult> runs, List<BotRunResult> rescued)
        {
            var catalog = runs.Select(r => r.EndingCatalog).FirstOrDefault(c => c.Count > 0);
            if (catalog == null)
            {
                return;
            }

            sb.AppendLine("## 엔딩 조각 도달 (엔딩 P6) — 구조된 판 " + rescued.Count.ToString(Inv));
            sb.AppendLine();
            sb.AppendLine("막마다 조건을 채운 판과 실제로 뽑힌 판. 막을 적지 않은 조각은 그 뒤의 나로 센다. 뒤 막의 '앞 막 조건'(EndingAct)은 판정 밖이라 0으로 본다.");
            sb.AppendLine();
            foreach (var (act, title) in new[] { (EndingAct.Rescue, "Ⅰ 구조의 날"), (EndingAct.Reflection, "Ⅱ 돌아보는 밤"), (EndingAct.Future, "Ⅲ 그 뒤의 나") })
            {
                var pieces = catalog.Where(c => (c.Act == EndingAct.Whole ? EndingAct.Future : c.Act) == act).Select(c => c.Id).ToList();
                int Eligible(string id) => rescued.Count(x => x.EligibleEndings.Contains(id));
                int Picked(string id) => rescued.Count(x => act == EndingAct.Rescue ? x.RescueId == id : act == EndingAct.Reflection ? x.ReflectionId == id : x.EndingId == id);
                var reached = pieces.Where(id => Eligible(id) > 0 || Picked(id) > 0).OrderByDescending(Eligible).ToList();
                sb.AppendLine($"### {title} — {pieces.Count}조각 가운데 {reached.Count}조각이 조건을 채움");
                sb.AppendLine();
                if (reached.Count > 0)
                {
                    sb.AppendLine("| 조각 | 조건 충족 | 뽑힘 |");
                    sb.AppendLine("|---|---|---|");
                    foreach (var id in reached)
                    {
                        sb.AppendLine($"| {id} | {Eligible(id)} ({P(Eligible(id), rescued.Count)}) | {Picked(id)} ({P(Picked(id), rescued.Count)}) |");
                    }

                    sb.AppendLine();
                }

                var never = pieces.Where(id => !reached.Contains(id)).ToList();
                sb.AppendLine("한 번도 조건을 채우지 못함 (" + never.Count.ToString(Inv) + "): " + (never.Count == 0 ? "없음" : string.Join(", ", never)));
                sb.AppendLine();
            }
        }

        /// <summary>엔딩 P6: 1년의 기록 평균 (엔딩 문턱을 잡을 때 본다). 전체 판은 일찍 끝난 판이 섞여 낮게 나온다.</summary>
        private static void AppendRecords(StringBuilder sb, IReadOnlyList<BotRunResult> runs, List<BotRunResult> rescued, List<string> strategies)
        {
            var keys = runs.SelectMany(r => r.Records.Keys).Distinct().ToList();
            if (keys.Count == 0)
            {
                return;
            }

            sb.AppendLine("## 1년의 기록 (엔딩 P3·P6)");
            sb.AppendLine();
            sb.AppendLine("끝났을 때의 값 평균. 전체 판에는 일찍 끝난 판이 섞여 있다. 구조된 판은 52주를 채운 값(최소·중앙·최대)이다.");
            sb.AppendLine();
            sb.AppendLine("| 기록 | 구조된 판 평균 (최소·중앙·최대) | 전체 판 평균 | " + string.Join(" | ", strategies) + " |");
            sb.AppendLine("|---|---|---|" + string.Concat(strategies.Select(_ => "---|")));
            foreach (var key in keys)
            {
                var all = runs.Where(x => x.Records.ContainsKey(key)).Select(x => x.Records[key]).ToList();
                var done = rescued.Where(x => x.Records.ContainsKey(key)).Select(x => x.Records[key]).OrderBy(v => v).ToList();
                var doneText = done.Count == 0 ? "-" : $"{F(done.Average())} ({done.First()}·{Percentile(done, 0.5f)}·{done.Last()})";
                sb.AppendLine($"| {key} | {doneText} | {(all.Count == 0 ? "-" : F(all.Average()))} | " +
                              string.Join(" | ", strategies.Select(st =>
                              {
                                  var list = runs.Where(x => x.StrategyId == st && x.Records.ContainsKey(key)).Select(x => x.Records[key]).ToList();
                                  return list.Count == 0 ? "-" : F(list.Average());
                              })) + " |");
            }

            sb.AppendLine();
        }

        private static void AppendStress(StringBuilder sb, IReadOnlyList<BotRunResult> runs)
        {
            sb.AppendLine("## 스트레스 (주 중)");
            sb.AppendLine();
            sb.AppendLine("주 평균 — 주 끝 / 주 중 최고 / 칸 평균 / 오른 합 / 내린 합 (살아 있는 판).");
            sb.AppendLine();
            sb.AppendLine("| 전략 | 1~13주 | 14~26주 | 27~39주 | 40~52주 | 100 닿은 주 |");
            sb.AppendLine("|---|---|---|---|---|---|");
            foreach (var g in runs.GroupBy(r => r.StrategyId))
            {
                var cells = new[] { (1, 13), (14, 26), (27, 39), (40, 52) }.Select(range =>
                {
                    var samples = g.SelectMany(r => r.Weeks).Where(w => w.Week >= range.Item1 && w.Week <= range.Item2).ToList();
                    return samples.Count == 0 ? "-"
                        : $"{F(samples.Average(w => w.Stress))} / {F(samples.Average(w => w.StressPeak))} / {F(samples.Average(w => w.StressMean))} / {F(samples.Average(w => w.StressUp))} / {F(samples.Average(w => w.StressDown))}";
                });
                var all = g.SelectMany(r => r.Weeks).ToList();
                sb.AppendLine($"| {g.Key} | " + string.Join(" | ", cells) + $" | {P(all.Count(w => w.StressPeak >= 100), all.Count)} |");
            }

            sb.AppendLine();
            var kinds = runs.SelectMany(r => r.StressBySlot.Keys).Distinct().OrderBy(k => k, StringComparer.Ordinal).ToList();
            sb.AppendLine("칸 종류별 한 칸 평균 변화 (칸 수):");
            sb.AppendLine();
            sb.AppendLine("| 전략 | " + string.Join(" | ", kinds) + " |");
            sb.AppendLine("|---|" + string.Concat(kinds.Select(_ => "---|")));
            foreach (var g in runs.GroupBy(r => r.StrategyId))
            {
                var cells = kinds.Select(k =>
                {
                    var sum = 0;
                    var count = 0;
                    foreach (var r in g)
                    {
                        if (r.StressBySlot.TryGetValue(k, out var v))
                        {
                            sum += v.Sum;
                            count += v.Count;
                        }
                    }

                    return count == 0 ? "-" : $"{(sum >= 0 ? "+" : "")}{F(sum / (double)count)} ({count})";
                });
                sb.AppendLine($"| {g.Key} | " + string.Join(" | ", cells) + " |");
            }

            sb.AppendLine();
        }

        /// <summary>제작 (P7, Docs/CRAFTING_PROPOSAL.md 4.8): 판당 완성 수, 정수 설비·교문 허수아비가 있었던 주 비율, 레시피별 완성 판 비율·첫 완성 주(중앙).</summary>
        private static void AppendCraft(StringBuilder sb, IReadOnlyList<BotRunResult> runs)
        {
            var strategies = runs.Select(r => r.StrategyId).Distinct().ToList();
            sb.AppendLine("## 제작 (제작 P7)");
            sb.AppendLine();
            sb.AppendLine("| 전략 | 완성 / 판 | 정수 설비 주 | 끓임 정수대↑ 주 | 자동 정수기 주 | 교문 허수아비 주 |");
            sb.AppendLine("|---|---|---|---|---|---|");
            foreach (var g in runs.GroupBy(r => r.StrategyId))
            {
                var list = g.ToList();
                var weeks = list.SelectMany(r => r.Weeks).ToList();
                sb.AppendLine($"| {g.Key} | {F(list.Average(r => r.Crafts))} | {P(weeks.Count(w => w.PurifierTier >= 1), weeks.Count)} | {P(weeks.Count(w => w.PurifierTier >= 2), weeks.Count)} " +
                              $"| {P(weeks.Count(w => w.PurifierTier >= 3), weeks.Count)} | {P(weeks.Count(w => w.Scarecrow), weeks.Count)} |");
            }

            sb.AppendLine();
            sb.AppendLine("| 레시피 | " + string.Join(" | ", strategies) + " |");
            sb.AppendLine("|---|" + string.Concat(strategies.Select(_ => "---|")));
            foreach (var recipe in runs.SelectMany(r => r.CraftFirstWeek.Keys).Distinct().OrderBy(k => k, StringComparer.Ordinal))
            {
                var cells = strategies.Select(s =>
                {
                    var list = runs.Where(r => r.StrategyId == s).ToList();
                    var firsts = list.Where(r => r.CraftFirstWeek.ContainsKey(recipe)).Select(r => r.CraftFirstWeek[recipe]).OrderBy(w => w).ToList();
                    var perRun = list.Average(r => r.CraftCounts.TryGetValue(recipe, out var n) ? n : 0);
                    return firsts.Count == 0 ? "-" : $"{P(firsts.Count, list.Count)} ({Percentile(firsts, 0.5f)}주) · {F(perRun)}회";
                });
                sb.AppendLine($"| {recipe} | " + string.Join(" | ", cells) + " |");
            }

            sb.AppendLine();
            sb.AppendLine("칸: 완성한 판 비율 (처음 완성한 주의 중앙값) · 판당 완성 횟수.");
            sb.AppendLine();
        }

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
