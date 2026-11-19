using System;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Planning;
using ApoGirl.Core.Random;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using ApoGirl.Systems.Save;

namespace ApoGirl.Systems.Session
{
    /// <summary>
    /// 게임 1판의 진입점 (MonoBehaviour 없음). UI는 이 클래스의 이벤트를 구독하고 명령만 호출한다.
    /// 흐름: NewGame/Load → 계획 편집 → BeginWeek → Step × 14 (마지막 Step이 주간 정산 + 자동 저장) → 계획 …
    /// </summary>
    public sealed class GameSession
    {
        private readonly IDefinitionDatabase _db;
        private readonly SaveService _saves;
        private readonly DebugExpeditionRunner _expedition;
        private WeekRunner _runner;
        private bool _executing;

        public GameSession(IDefinitionDatabase db, SaveService saves, IExpeditionRunner expeditionRunner = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _saves = saves ?? throw new ArgumentNullException(nameof(saves));
            _expedition = new DebugExpeditionRunner(expeditionRunner ?? new HeadlessExpeditionRunner(db));
        }

        /// <summary>상태가 바뀜 (화면 전체 갱신 신호).</summary>
        public event Action StateChanged;

        public event Action<SlotOutcome> SlotResolved;

        /// <summary>주간 정산 완료 (자동 저장 이후).</summary>
        public event Action<WeekSummary> WeekEnded;

        public event Action<GameStatus> GameEnded;

        public IDefinitionDatabase Definitions => _db;

        public GameState State { get; private set; }

        public DebugExpeditionRunner DebugExpedition => _expedition;

        public bool HasGame => State != null;

        public bool IsPlanning => HasGame && !_executing && PlanEditing.CanEdit(State);

        public bool IsExecuting => _executing;

        public bool IsEnded => HasGame && State.Status != GameStatus.Playing;

        // ── 게임 시작·저장 ──

        public void NewGame(int seed, ProfileState profile = null)
        {
            Attach(NewGameFactory.Create(_db.Balance, seed, profile));
        }

        public bool HasSave(string slot) => _saves.Exists(slot);

        public void Load(string slot)
        {
            Attach(_saves.Load(slot));
        }

        /// <summary>계획 단계에서만 저장할 수 있다 (주 진행 중 상태는 저장하지 않음).</summary>
        public void Save(string slot)
        {
            if (!IsPlanning)
            {
                throw new InvalidOperationException("계획 단계에서만 저장할 수 있음");
            }

            _saves.Save(slot, State);
        }

        // ── 계획 ──

        public PlanEditResult SetSlot(int slotIndex, string actionId)
        {
            return Edit(() => PlanEditing.SetSlot(State, _db, slotIndex, actionId));
        }

        public PlanEditResult ClearSlot(int slotIndex)
        {
            return Edit(() => PlanEditing.ClearSlot(State, _db, slotIndex));
        }

        public bool CopyLastWeek() => EditBool(() => PlanEditing.CopyLastWeek(State, _db));

        public bool SavePreset(string name) => EditBool(() => PlanEditing.SavePreset(State, name));

        public bool ApplyPreset(string name) => EditBool(() => PlanEditing.ApplyPreset(State, _db, name));

        public bool DeletePreset(string name) => EditBool(() => PlanEditing.DeletePreset(State, name));

        public List<PlanWarning> Warnings() => PlanValidator.Validate(State, _db);

        public int[] ProjectedSleepDebt() => PlanForecast.ProjectSleepDebt(State, _db);

        public ActionPreview Preview(string actionId) => ActionPreviewer.Preview(State, _db, actionId);

        public IEnumerable<ActionDef> ActionsIn(ActionCategory category)
        {
            return _db.Actions.Where(a => a.Category == category);
        }

        // ── 실행 ──

        public void BeginWeek()
        {
            if (!IsPlanning)
            {
                throw new InvalidOperationException("계획 단계가 아님");
            }

            _runner.PrepareWeek();
            _executing = true;
            StateChanged?.Invoke();
        }

        /// <summary>슬롯 하나 실행. 마지막 슬롯이면 주간 정산과 자동 저장까지 처리한다.</summary>
        public SlotOutcome Step()
        {
            if (!_executing)
            {
                throw new InvalidOperationException("주가 시작되지 않음");
            }

            var outcome = _runner.ExecuteNextSlot();
            SlotResolved?.Invoke(outcome);

            if (_runner.IsGameOver)
            {
                _executing = false;
                StateChanged?.Invoke();
                GameEnded?.Invoke(State.Status);
                return outcome;
            }

            if (_runner.IsWeekComplete)
            {
                var summary = _runner.FinishWeek();
                _executing = false;
                if (State.Status == GameStatus.Playing)
                {
                    _saves.Save(SaveService.AutoSlot, State);
                }

                StateChanged?.Invoke();
                WeekEnded?.Invoke(summary);
                if (State.Status != GameStatus.Playing)
                {
                    GameEnded?.Invoke(State.Status);
                }

                return outcome;
            }

            StateChanged?.Invoke();
            return outcome;
        }

        // ── 디버그 (문서 11.4 치트 콘솔) ──

        public void DebugChangeResource(ResourceType type, int amount)
        {
            EffectApplier.ChangeResource(State, type, amount, new StateDelta());
            StateChanged?.Invoke();
        }

        public void DebugChangeAllStats(int amount)
        {
            foreach (StatType stat in Enum.GetValues(typeof(StatType)))
            {
                EffectApplier.ChangeStat(State, stat, amount, _db.Balance, new StateDelta());
            }

            StateChanged?.Invoke();
        }

        public void DebugChangeSleepDebt(int amount)
        {
            EffectApplier.ChangeSleepDebt(State, amount, _db.Balance, new StateDelta());
            StateChanged?.Invoke();
        }

        public void DebugChangeVital(VitalType type, int amount)
        {
            EffectApplier.ChangeVital(State, type, amount, _db.Balance, new StateDelta());
            StateChanged?.Invoke();
        }

        /// <summary>계획 단계에서 지정 주차로 이동 (새 계획).</summary>
        public void DebugJumpToWeek(int week)
        {
            if (!IsPlanning)
            {
                return;
            }

            State.Calendar.Week = Math.Min(Math.Max(week, 1), CalendarState.TotalWeeks);
            State.Plan = PlanPreparer.CreateWeekPlan(State, _db.Balance);
            StateChanged?.Invoke();
        }

        public void DebugTogglePowerRestored()
        {
            State.Base.PowerRestored = !State.Base.PowerRestored;
            StateChanged?.Invoke();
        }

        private void Attach(GameState state)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            _executing = false;
            _runner = new WeekRunner(state, _db, new SeededRandom(state.Rng), _expedition);
            StateChanged?.Invoke();
        }

        private PlanEditResult Edit(Func<PlanEditResult> edit)
        {
            if (!IsPlanning)
            {
                return PlanEditResult.NotPlanning;
            }

            var result = edit();
            if (result == PlanEditResult.Ok)
            {
                StateChanged?.Invoke();
            }

            return result;
        }

        private bool EditBool(Func<bool> edit)
        {
            if (!IsPlanning)
            {
                return false;
            }

            var ok = edit();
            if (ok)
            {
                StateChanged?.Invoke();
            }

            return ok;
        }
    }

    /// <summary>디버그용: 다음 탐험 결과를 강제로 지정할 수 있는 실행기 래퍼.</summary>
    public sealed class DebugExpeditionRunner : IExpeditionRunner
    {
        private readonly IExpeditionRunner _inner;

        public DebugExpeditionRunner(IExpeditionRunner inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        /// <summary>null이면 정상 실행. 지정하면 다음 탐험 1회에만 적용.</summary>
        public ExtractStatus? ForceNext { get; set; }

        public ExpeditionResult Run(ExpeditionRequest request)
        {
            var forced = ForceNext;
            ForceNext = null;
            var result = _inner.Run(request);
            if (forced == ExtractStatus.Down && result.Status != ExtractStatus.Down)
            {
                return new ExpeditionResult
                {
                    Status = ExtractStatus.Down,
                    HpDelta = -30,
                    StressDelta = result.StressDelta + 30,
                    FatigueDelta = result.FatigueDelta,
                    Injuries = InjuryFlags.Fracture,
                };
            }

            if (forced == ExtractStatus.Extracted && result.Status == ExtractStatus.Down)
            {
                result.Status = ExtractStatus.Extracted;
                result.HpDelta = 0;
                result.Injuries = InjuryFlags.None;
            }

            return result;
        }
    }

    public enum ExecutionSpeed
    {
        Normal,
        Fast,

        /// <summary>멈출 슬롯만 보여주고 나머지는 즉시 진행.</summary>
        EventsOnly,
    }

    /// <summary>실행 연출 속도 (문서 4.3). 멈춤 판단은 SlotOutcome.StopForPresentation을 따른다.</summary>
    public static class ExecutionPacer
    {
        public static float DelayAfter(SlotOutcome outcome, ExecutionSpeed speed)
        {
            if (outcome.StopForPresentation)
            {
                return 0f;
            }

            switch (speed)
            {
                case ExecutionSpeed.Normal: return 0.5f;
                case ExecutionSpeed.Fast: return 0.15f;
                default: return 0f;
            }
        }
    }
}
