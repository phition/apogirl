using System;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.State;

namespace ApoGirl.Exploration.Model
{
    /// <summary>
    /// 탐험 한 판의 플레이 기록 (M2b-3). 실제 플레이 수치로 헤드리스 확률 모델을 보정하는 데 쓴다 (문서 12장 M7).
    /// JsonUtility로 한 줄씩 저장하므로 [Serializable] + public 필드.
    /// </summary>
    [Serializable]
    public sealed class PlayLogEntry
    {
        public string Timestamp = "";
        public string LocationId = "";
        public string TimeOfDay = "";
        public int Seed;
        public string Status = "";
        public float TimeSpent;
        public int HpDelta;
        public bool DuskAwakened;
        public bool DawnReturn;
        public int ContainersSearched;
        public int RoomsEntered;
        public int TimesSpotted;
        public int CrowdsWoken;
        public int DamageTaken;
        public bool DebugUsed;
        public List<ResourceAmount> Resources = new List<ResourceAmount>();
        public List<string> Items = new List<string>();
    }

    /// <summary>지역·시간대별 요약과 헤드리스 모델 제안값.</summary>
    public sealed class PlayLogSummary
    {
        public string LocationId = "";
        public string TimeOfDay = "";
        public int Runs;
        public float DownRate;
        public float LateRate;
        public float AverageTimeSpent;
        public float AverageHpDelta;

        /// <summary>자원별 한 판 획득량 [하위 25%, 상위 25%] → ExpeditionYield.Resources 제안값.</summary>
        public readonly List<ResourceRange> SuggestedResources = new List<ResourceRange>();
    }

    public static class PlayLogAnalyzer
    {
        /// <summary>디버그 도구를 쓴 판은 제외하고 지역·시간대별로 묶는다.</summary>
        public static List<PlayLogSummary> Summarize(IEnumerable<PlayLogEntry> entries)
        {
            var result = new List<PlayLogSummary>();
            var valid = entries.Where(e => e != null && !e.DebugUsed).ToList();
            foreach (var group in valid.GroupBy(e => (e.LocationId, e.TimeOfDay)))
            {
                var runs = group.ToList();
                var summary = new PlayLogSummary
                {
                    LocationId = group.Key.LocationId,
                    TimeOfDay = group.Key.TimeOfDay,
                    Runs = runs.Count,
                    DownRate = runs.Count(r => r.Status == "Down") / (float)runs.Count,
                    LateRate = runs.Count(r => r.Status == "Stranded" || r.DawnReturn) / (float)runs.Count,
                    AverageTimeSpent = runs.Average(r => r.TimeSpent),
                    AverageHpDelta = (float)runs.Average(r => r.HpDelta),
                };

                foreach (ResourceType resource in Enum.GetValues(typeof(ResourceType)))
                {
                    // 쓰러진 판은 0으로 포함 (기대 수익에 손실이 반영되도록)
                    var amounts = runs
                        .Select(r => r.Resources.Where(a => a.Resource == resource).Sum(a => a.Amount))
                        .OrderBy(a => a)
                        .ToList();
                    if (amounts.All(a => a == 0))
                    {
                        continue;
                    }

                    summary.SuggestedResources.Add(new ResourceRange(resource, Percentile(amounts, 0.25f), Percentile(amounts, 0.75f)));
                }

                result.Add(summary);
            }

            return result;
        }

        private static int Percentile(List<int> sorted, float p)
        {
            var index = (int)Math.Round((sorted.Count - 1) * p, MidpointRounding.AwayFromZero);
            return sorted[Math.Min(Math.Max(index, 0), sorted.Count - 1)];
        }
    }
}
