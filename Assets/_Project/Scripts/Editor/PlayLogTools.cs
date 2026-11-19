using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ApoGirl.Core.Definitions;
using ApoGirl.Data;
using ApoGirl.Exploration.Model;
using UnityEditor;
using UnityEngine;

namespace ApoGirl.EditorTools
{
    /// <summary>
    /// 실시간 탐험 플레이 기록 → 헤드리스 확률 모델 보정 (문서 12장 M7: 실제 플레이 로그로 보정).
    /// 기록 위치: Application.persistentDataPath/PlayLogs/expeditions.jsonl (디버그 도구를 쓴 판은 제외).
    /// </summary>
    public static class PlayLogTools
    {
        private const string DatabasePath = "Assets/_Project/Data/DefinitionDatabase.asset";

        private static string LogPath => Path.Combine(Application.persistentDataPath, "PlayLogs", "expeditions.jsonl");

        [MenuItem("ApoGirl/Exploration/Summarize Play Logs")]
        public static void Summarize()
        {
            var summaries = Load();
            if (summaries == null)
            {
                return;
            }

            var builder = new StringBuilder($"[PlayLog] {LogPath}\n");
            foreach (var s in summaries)
            {
                builder.AppendLine($"- {s.LocationId} / {s.TimeOfDay}: {s.Runs}판, 쓰러짐 {s.DownRate:P0}, 늦은 귀환 {s.LateRate:P0}, " +
                                   $"평균 {s.AverageTimeSpent:0}초, HP {s.AverageHpDelta:+0;-0}");
                foreach (var r in s.SuggestedResources)
                {
                    builder.AppendLine($"    {r.Resource}: {r.Min}~{r.Max}");
                }
            }

            Debug.Log(builder.ToString());
        }

        [MenuItem("ApoGirl/Exploration/Apply Play Logs To Headless Models")]
        public static void Apply()
        {
            var summaries = Load();
            if (summaries == null)
            {
                return;
            }

            var database = AssetDatabase.LoadAssetAtPath<DefinitionDatabaseAsset>(DatabasePath);
            var models = AssetDatabase.FindAssets("t:ExpeditionModelAsset")
                .Select(guid => AssetDatabase.LoadAssetAtPath<ExpeditionModelAsset>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(m => m != null)
                .ToList();

            var applied = 0;
            foreach (var summary in summaries.Where(s => s.Runs >= 3))
            {
                var model = models.FirstOrDefault(m => m.Id == summary.LocationId);
                if (model == null)
                {
                    continue;
                }

                var definition = model.ToDefinition();
                var timeYield = summary.TimeOfDay == "Night" ? definition.Night : definition.Day;
                timeYield.Resources = summary.SuggestedResources.Select(r => new ResourceRange(r.Resource, r.Min, r.Max)).ToList();
                timeYield.BaseDownChance = summary.DownRate;
                model.EditorSetDefinition(definition);
                EditorUtility.SetDirty(model);
                applied++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[PlayLog] 헤드리스 모델 {applied}건 보정 (3판 이상 기록된 지역·시간대만). DB: {(database != null ? DatabasePath : "없음")}");
        }

        private static List<PlayLogSummary> Load()
        {
            if (!File.Exists(LogPath))
            {
                Debug.LogWarning($"[PlayLog] 기록 없음: {LogPath}");
                return null;
            }

            var entries = File.ReadAllLines(LogPath)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => JsonUtility.FromJson<PlayLogEntry>(line))
                .ToList();
            return PlayLogAnalyzer.Summarize(entries);
        }
    }
}
