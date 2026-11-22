using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApoGirl.Core.Definitions;

namespace ApoGirl.Core.Simulation.Bots
{
    /// <summary>봇 묶음 실행의 진행·취소 (뒤 스레드와 화면이 함께 본다).</summary>
    public sealed class BotBatchProgress
    {
        private int _done;
        private volatile bool _cancel;

        public int Total { get; internal set; }

        public int Done => Volatile.Read(ref _done);

        public bool Cancelled => _cancel;

        public void Cancel() => _cancel = true;

        internal void Step() => Interlocked.Increment(ref _done);
    }

    /// <summary>엔딩 묶음 한 줄: 목표 범위와 전체·전략별 비율.</summary>
    public sealed class BotEndingRow
    {
        public string Name = "";
        public float Min;
        public float Max;
        public float Total;
        public Dictionary<string, float> ByStrategy = new Dictionary<string, float>();

        public bool InTarget => Total >= Min && Total <= Max;
    }

    /// <summary>
    /// 밸런스 표 L4 (Docs/BALANCE_LAB_PROPOSAL.md 6절): 같은 DB로 전략 × 판을 돌린다.
    /// 메뉴 Run Bot Simulation과 같은 BotSimulator.Run·같은 시드(BaseSeed + i)·같은 순서라 같은 결과가 나온다.
    /// DB는 실행 중 바뀌지 않는 사본이어야 한다 (SnapshotDatabase).
    /// </summary>
    public static class BotBatch
    {
        /// <summary>취소되면 null. 결과는 전략 순서 → 판 순서.</summary>
        public static List<BotRunResult> Run(IDefinitionDatabase db, BotConfigDef config, IReadOnlyList<BotStrategyDef> strategies, int runs, BotBatchProgress progress = null, int parallelism = 1)
        {
            progress ??= new BotBatchProgress();
            var jobs = strategies.SelectMany(s => Enumerable.Range(0, runs).Select(i => (Strategy: s, Seed: config.BaseSeed + i))).ToList();
            progress.Total = jobs.Count;
            var results = new BotRunResult[jobs.Count];
            var options = new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, parallelism) };
            Parallel.For(0, jobs.Count, options, (index, loop) =>
            {
                if (progress.Cancelled)
                {
                    loop.Stop();
                    return;
                }

                results[index] = BotSimulator.Run(db, config, jobs[index].Strategy, jobs[index].Seed);
                progress.Step();
            });

            return progress.Cancelled ? null : results.ToList();
        }

        /// <summary>BotReport.EndingTargets 묶음마다 전체·전략별 비율.</summary>
        public static List<BotEndingRow> EndingTable(IReadOnlyList<BotRunResult> runs)
        {
            var strategies = runs.Select(r => r.StrategyId).Distinct().ToList();
            var rows = new List<BotEndingRow>();
            foreach (var (name, min, max, categories) in BotReport.EndingTargets)
            {
                bool In(BotRunResult r) => r.EndingCategory.HasValue && categories.Contains(r.EndingCategory.Value);
                var row = new BotEndingRow { Name = name, Min = min, Max = max, Total = Ratio(runs.Count(In), runs.Count) };
                foreach (var strategy in strategies)
                {
                    var mine = runs.Where(r => r.StrategyId == strategy).ToList();
                    row.ByStrategy[strategy] = Ratio(mine.Count(In), mine.Count);
                }

                rows.Add(row);
            }

            return rows;
        }

        private static float Ratio(int count, int total) => total == 0 ? 0f : count / (float)total;
    }
}
