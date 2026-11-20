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

        /// <summary>runsPerStrategy &lt; 0이면 설정값. 요약 문서 경로를 돌려준다 (취소·오류면 null).</summary>
        public static string Run(int runsPerStrategy, string difficulty = null, string outputFolder = null)
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

            var summary = Path.Combine(outputFolder, BotReport.SummaryFile);
            Debug.Log($"[BotSim] {results.Count}판 완료 ({watch.Elapsed.TotalSeconds:0.0}초) → {Path.GetFullPath(summary)}");
            return summary;
        }
    }
}
