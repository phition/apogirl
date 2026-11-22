using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ApoGirl.Core.Balance;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Simulation.Bots;
using ApoGirl.Presentation.Text;
using ApoGirl.Systems.Content;
using UnityEngine;
using UnityEngine.UIElements;

namespace ApoGirl.Presentation.Screens
{
    /// <summary>
    /// 밸런스 표 봇 띠 (Docs/BALANCE_LAB_PROPOSAL.md 6절, L4).
    /// 누른 순간의 표 값으로 사본 DB를 만들어 뒤 스레드에서 전략 × 판을 돌리고, 엔딩 묶음 표·목표 띠·지난번 대비를 보인다.
    /// 보고서는 메뉴 Run Bot Simulation과 같은 폴더·같은 파일로 쓴다.
    /// </summary>
    internal sealed class BalanceLabBotPanel
    {
        private const string LastRunFile = "last_run.json";

        private readonly IDefinitionDatabase _db;
        private readonly VisualElement _root;
        private readonly VisualElement _strategies;
        private readonly IntegerField _runs;
        private readonly IntegerField _seed;
        private readonly DropdownField _difficulty;
        private readonly List<string> _difficultyIds = new List<string>();
        private readonly Button _run;
        private readonly Button _open;
        private readonly ProgressBar _progress;
        private readonly VisualElement _table;
        private readonly Label _status;

        private BotBatchProgress _running;
        private Task<Outcome> _task;
        private IVisualElementScheduledItem _poll;
        private string _summaryPath;

        private sealed class Outcome
        {
            public List<BotRunResult> Results;
            public string Error;
            public string SummaryPath;
            public double Seconds;
            public BalanceLabJson.BotRunRecord Record;
            public BalanceLabJson.BotRunRecord Previous;
        }

        public BalanceLabBotPanel(VisualElement parent, IDefinitionDatabase db)
        {
            _db = db;
            _root = new VisualElement();
            _root.AddToClassList("lab-bot");
            parent.Add(_root);

            var bar = new VisualElement();
            bar.AddToClassList("lab-bot-bar");
            _root.Add(bar);
            var title = new Label(Loc.T("lab.bot.title"));
            title.AddToClassList("lab-bot-title");
            bar.Add(title);

            _strategies = new VisualElement();
            _strategies.AddToClassList("lab-bot-strategies");
            bar.Add(_strategies);

            _runs = new IntegerField(Loc.T("lab.bot.runs")) { value = 5 };
            _runs.AddToClassList("lab-bot-int");
            bar.Add(_runs);
            bar.Add(Small("5", () => _runs.value = 5));
            bar.Add(Small("40", () => _runs.value = 40));

            _difficulty = new DropdownField(Loc.T("lab.bot.difficulty"));
            _difficulty.AddToClassList("lab-bot-difficulty");
            bar.Add(_difficulty);

            _seed = new IntegerField(Loc.T("lab.bot.seed"));
            _seed.AddToClassList("lab-bot-int");
            bar.Add(_seed);

            _run = Small(Loc.T("lab.bot.run"), RunOrCancel);
            _run.AddToClassList("lab-save");
            bar.Add(_run);
            _open = Small(Loc.T("lab.bot.open"), () =>
            {
                if (!string.IsNullOrEmpty(_summaryPath)) Application.OpenURL("file://" + Path.GetFullPath(_summaryPath));
            });
            _open.SetEnabled(false);
            bar.Add(_open);

            _progress = new ProgressBar();
            _progress.AddToClassList("lab-bot-progress");
            _progress.AddToClassList("hidden");
            _root.Add(_progress);

            _table = new VisualElement();
            _table.AddToClassList("lab-bot-table");
            _root.Add(_table);

            _status = new Label();
            _status.AddToClassList("lab-status");
            _root.Add(_status);

            Setup();
            ShowRecord(LoadLast(), null);
        }

        private void Setup()
        {
            BotConfigDef config;
            try
            {
                config = BalanceLabStore.BotConfig();
            }
            catch (Exception exception) when (exception is IOException || exception is FormatException)
            {
                config = null;
                _status.text = Loc.F("lab.bot.error", exception.Message);
            }

            _strategies.Clear();
            if (config == null)
            {
                _run.SetEnabled(false);
                if (string.IsNullOrEmpty(_status.text)) _status.text = Loc.T("lab.bot.no_config");
                return;
            }

            foreach (var strategy in config.Strategies)
            {
                var toggle = new Toggle(strategy.Id) { value = true, tooltip = strategy.Description, userData = strategy.Id };
                toggle.AddToClassList("lab-bot-toggle");
                _strategies.Add(toggle);
            }

            _seed.value = config.BaseSeed;
            _difficultyIds.Clear();
            var names = new List<string>();
            foreach (var difficulty in _db.Balance.Difficulties ?? new DifficultyDef[0])
            {
                _difficultyIds.Add(difficulty.Id);
                names.Add(Loc.T("difficulty." + difficulty.Id + ".name"));
            }

            _difficulty.choices = names;
            var index = Math.Max(0, _difficultyIds.IndexOf(config.Difficulty));
            if (names.Count > 0) _difficulty.index = index;
        }

        private void RunOrCancel()
        {
            if (_running != null)
            {
                _running.Cancel();
                _status.text = Loc.T("lab.bot.cancelling");
                return;
            }

            BotConfigDef config;
            try
            {
                config = BalanceLabStore.BotConfig(); // 누를 때마다 다시 읽어 json 수정이 바로 먹는다
            }
            catch (Exception exception) when (exception is IOException || exception is FormatException)
            {
                _status.text = Loc.F("lab.bot.error", exception.Message);
                return;
            }

            if (config == null)
            {
                _status.text = Loc.T("lab.bot.no_config");
                return;
            }

            var chosen = _strategies.Children().OfType<Toggle>().Where(t => t.value).Select(t => (string)t.userData).ToHashSet();
            var strategies = config.Strategies.Where(s => chosen.Contains(s.Id)).ToList();
            var runs = Math.Max(1, _runs.value);
            if (strategies.Count == 0)
            {
                _status.text = Loc.T("lab.bot.no_strategy");
                return;
            }

            config.BaseSeed = _seed.value;
            if (_difficulty.index >= 0 && _difficulty.index < _difficultyIds.Count)
            {
                config.Difficulty = _difficultyIds[_difficulty.index];
            }

            // 지금 표의 값으로 사본을 만든다 (메인 스레드). 실행 중 표를 더 고쳐도 이 실행은 흔들리지 않는다.
            var snapshot = Snapshot(_db);
            var errors = BotPlanner.Validate(config, snapshot);
            if (errors.Count > 0)
            {
                _status.text = Loc.F("lab.bot.error", string.Join(" / ", errors.Take(3)));
                return;
            }

            var changed = BalanceOverridesJson.Collect(snapshot.Balance).Count + BalanceLabModel.For(_db).ChangedModelIds.Count();
            var header = $"{DateTime.Now:yyyy-MM-dd HH:mm} · 난이도 {config.Difficulty} · 전략 {strategies.Count}종 × {runs}판 · 시드 {config.BaseSeed}~{config.BaseSeed + runs - 1} · 밸런스 표 (기본값과 다른 값 {changed}개)";
            var folder = BalanceLabStore.BotFolder;
            var work = BalanceLabStore.Work;
            var parallelism = Math.Max(1, Environment.ProcessorCount - 2);

            _running = new BotBatchProgress();
            var progress = _running;
            _task = Task.Run(() => Execute(snapshot, config, strategies, runs, progress, parallelism, header, folder, work));
            _run.text = Loc.T("lab.bot.cancel");
            _progress.RemoveFromClassList("hidden");
            _progress.value = 0;
            _status.text = Loc.T("lab.bot.running");
            _poll = _root.schedule.Execute(Poll).Every(200);
        }

        // 뒤 스레드: 유니티 API를 쓰지 않는다
        private static Outcome Execute(IDefinitionDatabase db, BotConfigDef config, List<BotStrategyDef> strategies, int runs, BotBatchProgress progress,
            int parallelism, string header, string folder, string work)
        {
            var outcome = new Outcome();
            try
            {
                var watch = Stopwatch.StartNew();
                outcome.Results = BotBatch.Run(db, config, strategies, runs, progress, parallelism);
                watch.Stop();
                outcome.Seconds = watch.Elapsed.TotalSeconds;
                if (outcome.Results == null)
                {
                    return outcome;
                }

                var fullHeader = header + $" · {outcome.Seconds:0.0}초\n\n" + string.Join("\n", strategies.Select(s => $"- **{s.Id}**: {s.Description}"));
                Directory.CreateDirectory(folder);
                foreach (var file in BotReport.Build(outcome.Results, fullHeader))
                {
                    File.WriteAllText(Path.Combine(folder, file.Key), file.Value);
                }

                outcome.SummaryPath = Path.Combine(folder, BotReport.SummaryFile);
                var lastPath = Path.Combine(work, LastRunFile);
                outcome.Previous = File.Exists(lastPath) ? BalanceLabJson.ParseBotRun(File.ReadAllText(lastPath)) : null;
                outcome.Record = new BalanceLabJson.BotRunRecord
                {
                    Stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                    Difficulty = config.Difficulty,
                    Runs = runs,
                    Strategies = strategies.Select(s => s.Id).ToList(),
                    Rows = BotBatch.EndingTable(outcome.Results),
                };
                Directory.CreateDirectory(work);
                File.WriteAllText(lastPath, BalanceLabJson.WriteBotRun(outcome.Record));
            }
            catch (Exception exception)
            {
                outcome.Error = exception.GetType().Name + ": " + exception.Message;
            }

            return outcome;
        }

        private void Poll()
        {
            if (_running == null || _task == null)
            {
                return;
            }

            _progress.highValue = Math.Max(1, _running.Total);
            _progress.value = _running.Done;
            _progress.title = $"{_running.Done}/{_running.Total}";
            if (!_task.IsCompleted)
            {
                return;
            }

            _poll?.Pause();
            var outcome = _task.IsFaulted ? new Outcome { Error = _task.Exception?.GetBaseException().Message } : _task.Result;
            _running = null;
            _task = null;
            _run.text = Loc.T("lab.bot.run");
            _progress.AddToClassList("hidden");

            if (!string.IsNullOrEmpty(outcome.Error))
            {
                _status.text = Loc.F("lab.bot.error", outcome.Error);
                UnityEngine.Debug.LogError("[BalanceLab] 봇 실행 오류: " + outcome.Error);
                return;
            }

            if (outcome.Results == null)
            {
                _status.text = Loc.T("lab.bot.cancelled");
                return;
            }

            _summaryPath = outcome.SummaryPath;
            _open.SetEnabled(true);
            ShowRecord(outcome.Record, outcome.Previous);
            _status.text = Loc.F("lab.bot.done", outcome.Results.Count, outcome.Seconds.ToString("0.0"), outcome.SummaryPath);
            UnityEngine.Debug.Log($"[BalanceLab] 봇 {outcome.Results.Count}판 완료 ({outcome.Seconds:0.0}초) → {Path.GetFullPath(outcome.SummaryPath)}");
        }

        private static BalanceLabJson.BotRunRecord LoadLast()
        {
            try
            {
                var path = Path.Combine(BalanceLabStore.Work, LastRunFile);
                return File.Exists(path) ? BalanceLabJson.ParseBotRun(File.ReadAllText(path)) : null;
            }
            catch (IOException)
            {
                return null;
            }
        }

        // ── 결과 표: 묶음 × (목표·전체·전략들), 칸마다 지난번 대비 ▲▼ ──

        private void ShowRecord(BalanceLabJson.BotRunRecord record, BalanceLabJson.BotRunRecord previous)
        {
            _table.Clear();
            if (record == null || record.Rows.Count == 0)
            {
                return;
            }

            var caption = new Label(Loc.F("lab.bot.caption", record.Stamp, record.Difficulty, record.Strategies.Count, record.Runs) +
                                    (previous != null ? Loc.F("lab.bot.versus", previous.Stamp) : ""));
            caption.AddToClassList("lab-hint");
            _table.Add(caption);

            var head = Row();
            head.AddToClassList("lab-head");
            head.Add(Cell(Loc.T("lab.bot.group"), "lab-bot-name"));
            head.Add(Cell(Loc.T("lab.bot.target"), "lab-bot-num"));
            head.Add(Cell(Loc.T("lab.bot.total"), "lab-bot-num"));
            foreach (var strategy in record.Strategies)
            {
                head.Add(Cell(strategy, "lab-bot-num"));
            }

            _table.Add(head);
            foreach (var row in record.Rows)
            {
                var before = previous?.Rows.FirstOrDefault(r => r.Name == row.Name);
                var line = Row();
                line.Add(Cell(row.Name, "lab-bot-name"));
                line.Add(Cell($"{row.Min * 100:0}~{row.Max * 100:0}%", "lab-bot-num"));
                var total = Cell(Percent(row.Total, before?.Total), "lab-bot-num");
                total.AddToClassList(row.InTarget ? "lab-bot-in" : "lab-bot-out");
                line.Add(total);
                foreach (var strategy in record.Strategies)
                {
                    row.ByStrategy.TryGetValue(strategy, out var value);
                    float? old = before != null && before.ByStrategy.TryGetValue(strategy, out var o) ? o : (float?)null;
                    line.Add(Cell(Percent(value, old), "lab-bot-num"));
                }

                _table.Add(line);
            }
        }

        /// <summary>"48% ▲6": 지난번과 0.5%p 이상 다르면 화살표와 차이.</summary>
        internal static string Percent(float value, float? previous)
        {
            var text = $"{value * 100:0}%";
            if (!previous.HasValue)
            {
                return text;
            }

            var diff = (value - previous.Value) * 100;
            if (Math.Abs(diff) < 0.5f)
            {
                return text;
            }

            return text + (diff > 0 ? " ▲" : " ▼") + Math.Abs(diff).ToString("0");
        }

        /// <summary>지금 값(밸런스·탐험 모델)의 깊은 사본 + 나머지는 원래 DB. DefinitionDatabaseAsset.Build와 같은 직렬화 규칙.</summary>
        internal static SnapshotDatabase Snapshot(IDefinitionDatabase live)
        {
            var models = new Dictionary<string, ExpeditionModelDef>();
            foreach (var id in BalanceLabModel.For(live).ModelIds)
            {
                if (live.TryGetExpeditionModel(id, out var model))
                {
                    models[id] = Copy(model);
                }
            }

            return new SnapshotDatabase(live, Copy(live.Balance), models);
        }

        private static T Copy<T>(T source) where T : new()
        {
            var copy = new T();
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(source), copy);
            return copy;
        }

        private static VisualElement Row()
        {
            var row = new VisualElement();
            row.AddToClassList("lab-row");
            return row;
        }

        private static Label Cell(string text, string cssClass)
        {
            var label = new Label(text);
            label.AddToClassList("lab-cell");
            label.AddToClassList(cssClass);
            return label;
        }

        private static Button Small(string text, Action onClick)
        {
            var button = new Button(onClick) { text = text };
            button.AddToClassList("small-button");
            return button;
        }
    }
}
