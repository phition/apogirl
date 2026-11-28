using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Simulation.Bots;
using ApoGirl.Data;
using ApoGirl.Systems.Content;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace ApoGirl.EditorTools
{
    /// <summary>
    /// M7-1: 실제 정의 DB(지역·조우 포함)로 전략마다 여러 판을 52주 돌려 Temp/BotSim/에 보고서를 쓴다.
    /// 전략·판 수는 Assets/_Project/Balance/bot_strategies.json.
    /// </summary>
    public static class BotSimulationMenu
    {
        public const string OutputFolder = "Temp/BotSim";

        [MenuItem("ApoGirl/Balance/Run Bot Simulation")]
        public static void RunDefault()
        {
            Run(-1);
        }

        [MenuItem("ApoGirl/Balance/Run Bot Simulation (Quick, 5 per strategy)")]
        public static void RunQuick()
        {
            Run(5);
        }

        /// <summary>M7-3: 난이도마다 따로 돌려 Temp/BotSim_{난이도}/에 쓴다.</summary>
        [MenuItem("ApoGirl/Balance/Run Bot Simulation (All Difficulties)")]
        public static void RunAllDifficulties()
        {
            var balance = AssetDatabase.LoadAssetAtPath<DefinitionDatabaseAsset>(ContentPipeline.DatabasePath)?.Build().Balance;
            foreach (var difficulty in balance?.Difficulties ?? new DifficultyDef[0])
            {
                if (Run(-1, difficulty.Id, OutputFolder + "_" + difficulty.Id) == null)
                {
                    return;
                }
            }
        }

        /// <summary>
        /// 배치 모드용 (-executeMethod): 난이도마다 돌려 환경 변수 BOTSIM_OUT 폴더 아래 {난이도}/에 쓴다.
        /// 배치 Unity는 끝날 때 프로젝트의 Temp 폴더를 지우므로, Temp/BotSim에 쓰면 보고서가 남지 않는다.
        /// </summary>
        public static void RunBatchAllDifficulties()
        {
            var root = Environment.GetEnvironmentVariable("BOTSIM_OUT");
            if (string.IsNullOrEmpty(root))
            {
                Debug.LogError("[BotSim] 환경 변수 BOTSIM_OUT(보고서를 쓸 폴더)이 없다");
                return;
            }

            var balance = AssetDatabase.LoadAssetAtPath<DefinitionDatabaseAsset>(ContentPipeline.DatabasePath)?.Build().Balance;
            foreach (var difficulty in balance?.Difficulties ?? new DifficultyDef[0])
            {
                if (Run(-1, difficulty.Id, Path.Combine(root, difficulty.Id)) == null)
                {
                    return;
                }
            }
        }

        /// <summary>밸런스 변형 하나: 덮어쓰기(필드·난이도 배율)와 탐험 모델 스트레스 더하기.</summary>
        public sealed class BalanceVariant
        {
            public string Name = "";
            public string Note = "";
            public Dictionary<string, Newtonsoft.Json.Linq.JToken> Fields = new Dictionary<string, Newtonsoft.Json.Linq.JToken>();
            public Dictionary<string, Dictionary<string, Newtonsoft.Json.Linq.JToken>> Difficulties = new Dictionary<string, Dictionary<string, Newtonsoft.Json.Linq.JToken>>();

            /// <summary>모든 탐험 모델(낮·밤)의 StressMin·StressMax에 더하는 값 (음수면 줄인다, 0 미만은 0).</summary>
            public int ExpeditionStress;
        }

        /// <summary>
        /// 배치 모드용 (-executeMethod): 밸런스 변형 여러 개를 같은 시드로 돌려 비교한다 (마지막 목표 맞추기용 실험, 게임 값은 바꾸지 않는다).
        /// 환경 변수 BOTSIM_VARIANTS = 변형 목록 JSON 파일([{Name, Note, Fields, Difficulties, ExpeditionStress}]),
        /// BOTSIM_OUT = 결과 폴더 ({Name}/summary.md 와 compare.csv), BOTSIM_DIFFICULTY = 난이도 (없으면 설정값).
        /// 변형의 값은 지금 밸런스(코드 기본값 + balance_overrides.json) 위에 얹는다.
        /// </summary>
        public static void RunBatchVariants()
        {
            var root = Environment.GetEnvironmentVariable("BOTSIM_OUT");
            var file = Environment.GetEnvironmentVariable("BOTSIM_VARIANTS");
            if (string.IsNullOrEmpty(root) || string.IsNullOrEmpty(file) || !File.Exists(file))
            {
                Debug.LogError("[BotSim] 환경 변수 BOTSIM_OUT(결과 폴더)·BOTSIM_VARIANTS(변형 목록 JSON)가 필요하다");
                return;
            }

            var difficulty = Environment.GetEnvironmentVariable("BOTSIM_DIFFICULTY");
            var variants = Newtonsoft.Json.JsonConvert.DeserializeObject<List<BalanceVariant>>(File.ReadAllText(file)) ?? new List<BalanceVariant>();
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("variant,strategy,runs,rescued_pct,collapse_pct,starvation_pct,infection_pct,expedition_pct,avg_end_week,stress_q1,stress_q2,stress_q3,stress_q4,high_stress_weeks_per_10,high_stress_weeks_rescued_median");
            foreach (var variant in variants)
            {
                var results = new List<BotRunResult>();
                var summary = Run(-1, string.IsNullOrEmpty(difficulty) ? null : difficulty, Path.Combine(root, variant.Name), db =>
                {
                    var warnings = BalanceOverridesJson.Apply(db.Balance, new BalanceOverridesJson.Overrides { Fields = variant.Fields, Difficulties = variant.Difficulties });
                    foreach (var warning in warnings)
                    {
                        Debug.LogWarning($"[BotSim] 변형 {variant.Name}: {warning}");
                    }

                    if (variant.ExpeditionStress != 0)
                    {
                        // 탐험 모델은 지역 ID로 찾는다 (탐험 행동의 LocationId)
                        var models = new HashSet<ExpeditionModelDef>();
                        foreach (var action in db.Actions.Where(a => a.IsExpedition))
                        {
                            if (db.TryGetExpeditionModel(action.LocationId, out var model))
                            {
                                models.Add(model);
                            }
                        }

                        foreach (var model in models)
                        {
                            foreach (var yield in new[] { model.Day, model.Night })
                            {
                                yield.StressMin = Math.Max(0, yield.StressMin + variant.ExpeditionStress);
                                yield.StressMax = Math.Max(0, yield.StressMax + variant.ExpeditionStress);
                            }
                        }
                    }
                }, results);
                if (summary == null)
                {
                    return;
                }

                foreach (var group in results.GroupBy(x => x.StrategyId).Select(g => (Id: g.Key, Runs: g.ToList())).Append((Id: "ALL", Runs: results)))
                {
                    csv.AppendLine(CompareRow(variant.Name, group.Id, group.Runs));
                }
            }

            File.WriteAllText(Path.Combine(root, "compare.csv"), csv.ToString());
            Debug.Log($"[BotSim] 변형 {variants.Count}개 비교 → {Path.Combine(root, "compare.csv")}");
        }

        private static string CompareRow(string variant, string strategy, List<BotRunResult> runs)
        {
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            string Pct(Func<BotRunResult, bool> pick) => (runs.Count == 0 ? 0f : runs.Count(pick) * 100f / runs.Count).ToString("0.#", inv);
            string Cause(string cause) => Pct(x => !x.Survived && x.EndCause == cause);
            string Quarter(int from, int to)
            {
                // 주 끝 스트레스 평균 (살아 있는 주)
                var values = runs.SelectMany(x => x.Weeks).Where(w => w.Week >= from && w.Week <= to).Select(w => (double)w.Stress).ToList();
                return values.Count == 0 ? "" : values.Average().ToString("0.#", inv);
            }

            var weeks = runs.Sum(x => Math.Max(1, x.EndWeek));
            var high = runs.Sum(x => x.Records.TryGetValue("HighStressWeeks", out var n) ? n : 0);
            var rescuedHigh = runs.Where(x => x.Survived && x.Records.ContainsKey("HighStressWeeks")).Select(x => x.Records["HighStressWeeks"]).OrderBy(v => v).ToList();
            return string.Join(",", variant, strategy, runs.Count.ToString(inv), Pct(x => x.Survived),
                Cause(WeekRunnerCauses.Collapse), Cause(WeekRunnerCauses.Starvation), Cause(WeekRunnerCauses.Infection), Cause(WeekRunnerCauses.Expedition),
                (runs.Count == 0 ? 0 : runs.Average(x => x.EndWeek)).ToString("0.#", inv),
                Quarter(1, 13), Quarter(14, 26), Quarter(27, 39), Quarter(40, 52),
                (weeks == 0 ? 0f : high * 10f / weeks).ToString("0.##", inv),
                rescuedHigh.Count == 0 ? "" : BotReport.Percentile(rescuedHigh, 0.5f).ToString(inv));
        }

        private static class WeekRunnerCauses
        {
            public const string Collapse = ApoGirl.Core.Simulation.WeekRunner.EndCauseCollapse;
            public const string Starvation = ApoGirl.Core.Simulation.WeekRunner.EndCauseStarvation;
            public const string Infection = ApoGirl.Core.Simulation.WeekRunner.EndCauseInfection;
            public const string Expedition = ApoGirl.Core.Simulation.WeekRunner.EndCauseExpedition;
        }

        /// <summary>runsPerStrategy &lt; 0이면 설정값. 요약 문서 경로를 돌려준다 (취소·오류면 null).</summary>
        public static string Run(int runsPerStrategy, string difficulty = null, string outputFolder = null) =>
            Run(runsPerStrategy, difficulty, outputFolder, null, null);

        /// <summary>
        /// tweak: 정의 DB 사본(밸런스·탐험 모델)을 돌리기 전에 손본다 (밸런스 변형 비교). collect: 판별 결과를 받는다.
        /// </summary>
        public static string Run(int runsPerStrategy, string difficulty, string outputFolder, Action<IDefinitionDatabase> tweak, List<BotRunResult> collect)
        {
            var config = BotConfigJson.Load(BotConfigJson.DefaultPath);
            if (!string.IsNullOrEmpty(difficulty))
            {
                config.Difficulty = difficulty;
            }

            outputFolder ??= OutputFolder;
            var asset = AssetDatabase.LoadAssetAtPath<DefinitionDatabaseAsset>(ContentPipeline.DatabasePath);
            if (asset == null)
            {
                Debug.LogError($"[BotSim] 정의 DB 없음: {ContentPipeline.DatabasePath}");
                return null;
            }

            var db = asset.Build();
            tweak?.Invoke(db);
            var errors = BotPlanner.Validate(config, db);
            if (errors.Count > 0)
            {
                Debug.LogError("[BotSim] 설정 오류:\n" + string.Join("\n", errors));
                return null;
            }

            var runs = runsPerStrategy < 0 ? config.RunsPerStrategy : runsPerStrategy;
            var total = runs * config.Strategies.Count;
            var results = new List<BotRunResult>(total);
            var watch = Stopwatch.StartNew();
            try
            {
                foreach (var strategy in config.Strategies)
                {
                    for (var i = 0; i < runs; i++)
                    {
                        var done = results.Count;
                        if (EditorUtility.DisplayCancelableProgressBar("봇 시뮬레이션", $"{strategy.Id} {i + 1}/{runs} (전체 {done}/{total})", done / (float)total))
                        {
                            Debug.LogWarning($"[BotSim] 취소됨 ({done}/{total})");
                            return null;
                        }

                        results.Add(BotSimulator.Run(db, config, strategy, config.BaseSeed + i));
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            watch.Stop();
            var header = $"{DateTime.Now:yyyy-MM-dd HH:mm} · 난이도 {config.Difficulty} · 전략 {config.Strategies.Count}종 × {runs}판 · 시드 {config.BaseSeed}~{config.BaseSeed + runs - 1} · {watch.Elapsed.TotalSeconds:0.0}초\n\n" +
                         string.Join("\n", config.Strategies.Select(s => $"- **{s.Id}**: {s.Description}"));
            Directory.CreateDirectory(outputFolder);
            foreach (var file in BotReport.Build(results, header))
            {
                File.WriteAllText(Path.Combine(outputFolder, file.Key), file.Value);
            }

            collect?.AddRange(results);
            var summary = Path.Combine(outputFolder, BotReport.SummaryFile);
            Debug.Log($"[BotSim] {results.Count}판 완료 ({watch.Elapsed.TotalSeconds:0.0}초) → {Path.GetFullPath(summary)}");
            return summary;
        }
    }
}
