using System;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Endings;
using ApoGirl.Core.Events;
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
        private bool _useRealtimeExpeditions;
        private bool _interactiveEvents;
        private bool _forcedEvent;

        public GameSession(IDefinitionDatabase db, SaveService saves, IExpeditionRunner expeditionRunner = null, EndingGallery gallery = null)
        {
            Gallery = gallery;
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

        /// <summary>실시간 탐험 슬롯 도달: UI가 탐험 씬을 열고 CompleteExpedition(result)로 돌려줘야 한다.</summary>
        public event Action<ExpeditionRequest> ExpeditionStarted;

        /// <summary>이벤트 시작 (InteractiveEvents일 때): UI가 대화를 보여주고 ResolveEvent(choiceId)로 돌려줘야 한다.</summary>
        public event Action<EventDef> EventStarted;

        public IDefinitionDatabase Definitions => _db;

        public GameState State { get; private set; }

        public DebugExpeditionRunner DebugExpedition => _expedition;

        /// <summary>엔딩 갤러리 (없으면 기록하지 않음).</summary>
        public EndingGallery Gallery { get; }

        /// <summary>이번 판의 엔딩 (게임이 끝난 뒤에만, 그 전에는 null).</summary>
        public EndingResult Ending { get; private set; }

        /// <summary>이번 엔딩을 갤러리에서 처음 봤는가.</summary>
        public bool EndingIsNew { get; private set; }

        public bool HasGame => State != null;

        public bool IsPlanning => HasGame && !_executing && !HasPendingEvent && PlanEditing.CanEdit(State);

        public bool IsExecuting => _executing;

        public bool IsEnded => HasGame && State.Status != GameStatus.Playing;

        /// <summary>true: 탐험 슬롯에서 실시간 탐험을 기다린다. false: 헤드리스 확률 모델로 즉시 처리 (테스트·봇·디버그).</summary>
        public bool UseRealtimeExpeditions
        {
            get => _useRealtimeExpeditions;
            set
            {
                _useRealtimeExpeditions = value;
                if (_runner != null && !_runner.HasPendingExpedition)
                {
                    _runner.DeferExpeditions = value;
                }
            }
        }

        public ExpeditionRequest PendingExpedition => _runner?.PendingRequest;

        /// <summary>true: 이벤트에서 멈추고 선택을 기다린다. false: 첫 선택지로 즉시 진행 (테스트·봇).</summary>
        public bool InteractiveEvents
        {
            get => _interactiveEvents;
            set
            {
                _interactiveEvents = value;
                if (_runner != null && !_runner.HasPendingEvent)
                {
                    _runner.DeferEvents = value;
                }
            }
        }

        public bool HasPendingEvent => _runner != null && _runner.HasPendingEvent;

        public EventDef PendingEvent => _runner?.PendingEvent;

        public IReadOnlyList<EventChoiceView> PendingChoices => _runner?.PendingChoices;

        // ── 게임 시작·저장 ──

        /// <summary>다음 NewGame에 적용할 회차 시작 보너스 (타이틀에서 고른다). 적용 후 비운다.</summary>
        public StartBonusDef PendingStartBonus { get; set; }

        /// <summary>엔딩을 하나라도 본 적 있는가 (회차 요소 해금).</summary>
        public bool IsNewGamePlus => Gallery != null && Gallery.Entries.Count > 0;

        public void NewGame(int seed, ProfileState profile = null, GameEffect prologue = null)
        {
            var state = NewGameFactory.Create(_db.Balance, seed, profile);
            if (prologue != null)
            {
                EffectApplier.Apply(state, prologue, _db.Balance, new StateDelta());
            }

            // M6 회차 요소: 시작 보너스 + 2회차 표시 ("창고 안의 7일")
            if (IsNewGamePlus)
            {
                state.Flags.Add(NgPlusFlag);
            }

            if (PendingStartBonus != null)
            {
                EffectApplier.Apply(state, PendingStartBonus.Effect, _db.Balance, new StateDelta());
                state.Flags.Add("BONUS_" + PendingStartBonus.Id);
                PendingStartBonus = null;
            }

            // 1주차 1일 낮: 튜토리얼 런 고정 (문서 6.3.3)
            var tutorial = _db.Balance.TutorialActionId;
            if (!string.IsNullOrEmpty(tutorial) && _db.TryGetAction(tutorial, out _))
            {
                state.Plan.Slots[0].ActionId = tutorial;
                state.Plan.Slots[0].Locked = true;
                PlanPreparer.ApplyForcedRest(state, _db);
            }

            Attach(state);
        }

        /// <summary>새 게임 직후 아직 보지 않은 시작 이벤트 (BalanceConfig.OpeningEventId).</summary>
        public bool HasPendingOpening => HasGame && NextOpeningEventId() != null;

        public const string NgPlusFlag = "NG_PLUS";

        /// <summary>시작 이벤트를 띄운다 (계획 단계에서, 슬롯 밖). 2회차면 "창고 안의 7일"이 먼저.</summary>
        public SlotOutcome StartOpening()
        {
            return DebugForceEvent(NextOpeningEventId());
        }

        private string NextOpeningEventId()
        {
            var balance = _db.Balance;
            if (State.Flags.Contains(NgPlusFlag) && IsUnseen(balance.NgPlusOpeningEventId) && !IsSeen(balance.OpeningEventId))
            {
                return balance.NgPlusOpeningEventId;
            }

            return IsUnseen(balance.OpeningEventId) ? balance.OpeningEventId : null;
        }

        private bool IsUnseen(string eventId) =>
            !string.IsNullOrEmpty(eventId) && _db.TryGetEvent(eventId, out _) && !State.Events.Records.ContainsKey(eventId);

        private bool IsSeen(string eventId) => !string.IsNullOrEmpty(eventId) && State.Events.Records.ContainsKey(eventId);

        /// <summary>출정 준비 화면에서 로드아웃을 바꾼 뒤 대기 중인 탐험 요청에 반영한다.</summary>
        public void RefreshPendingLoadout()
        {
            var request = _runner?.PendingRequest;
            if (request != null)
            {
                ExpeditionRequestFactory.RefreshLoadout(request, State);
                StateChanged?.Invoke();
            }
        }

        /// <summary>계획 화면 팔레트: 지금 해금된 행동인가.</summary>
        public bool IsUnlocked(ActionDef action) => PlanEditing.IsUnlocked(State, _db, action);

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

        /// <summary>M6: 합류한 동료의 역할을 바꾼다 (계획 단계에서만, 빈 문자열 = 역할 없음).</summary>
        public bool SetNpcRole(string npcId, string roleId) => EditBool(() => Core.Rules.RoleRules.Assign(State, _db, npcId, roleId));

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
            if (outcome.AwaitingExpedition)
            {
                _expedition.ApplyForcedHostile(outcome.ExpeditionRequest);
                StateChanged?.Invoke();
                ExpeditionStarted?.Invoke(outcome.ExpeditionRequest);
                return outcome;
            }

            return AfterSlot(outcome);
        }

        /// <summary>진행 중인 이벤트에 선택을 반영한다. 연쇄 이벤트가 이어지면 다시 EventStarted.</summary>
        public SlotOutcome ResolveEvent(string choiceId)
        {
            if (!HasPendingEvent)
            {
                throw new InvalidOperationException("대기 중인 이벤트가 없음");
            }

            return AfterSlot(_runner.ResolveEvent(choiceId));
        }

        /// <summary>디버그: 계획 단계에서 이벤트를 강제로 시작한다.</summary>
        public SlotOutcome DebugForceEvent(string eventId)
        {
            if (!IsPlanning)
            {
                throw new InvalidOperationException("계획 단계에서만 강제 발생할 수 있음");
            }

            _forcedEvent = true;
            var outcome = _runner.ForceEvent(eventId);
            return AfterSlot(outcome);
        }

        /// <summary>실시간 탐험 결과를 반영하고 슬롯을 끝낸다 (주 마지막 슬롯이면 정산까지).</summary>
        public SlotOutcome CompleteExpedition(ExpeditionResult result)
        {
            if (_runner == null || !_runner.HasPendingExpedition)
            {
                throw new InvalidOperationException("대기 중인 탐험이 없음");
            }

            return AfterSlot(_runner.CompleteExpedition(result));
        }

        private SlotOutcome AfterSlot(SlotOutcome outcome)
        {
            if (outcome.AwaitingEvent)
            {
                StateChanged?.Invoke();
                EventStarted?.Invoke(_runner.PendingEvent);
                return outcome;
            }

            if (_forcedEvent)
            {
                // 강제 이벤트는 슬롯이 아니므로 정산·진행 없이 끝
                _forcedEvent = false;
                if (State.Status != GameStatus.Playing)
                {
                    ResolveEnding();
                }

                CheckEnded();
                StateChanged?.Invoke();
                return outcome;
            }

            SlotResolved?.Invoke(outcome);

            if (_runner.IsGameOver)
            {
                _executing = false;
                ResolveEnding();
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

                if (State.Status != GameStatus.Playing)
                {
                    ResolveEnding();
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

        /// <summary>디버그: 생존 상태 회복 (체력 최대, 배고픔·갈증·피로·스트레스 0). 감염·수면 부채는 그대로.</summary>
        public void DebugRestoreVitals()
        {
            var vitals = State.Player.Vitals;
            vitals.Hp = _db.Balance.VitalMax;
            vitals.Hunger = 0;
            vitals.Thirst = 0;
            vitals.Fatigue = 0;
            vitals.Stress = 0;
            State.Player.BreakdownWeeks = 0;
            StateChanged?.Invoke();
        }

        /// <summary>디버그 "지역·시설 전부 해금"에서 지역 플래그 외에 함께 켜는 플래그 (UI·1막 계획).</summary>
        private static readonly string[] DebugUnlockExtraFlags = { "NIGHT_RUNS", "UI_ALL", "UI_DEFENSE", "CRUTCH_PLAN", "POWER_PLAN" };

        /// <summary>
        /// 디버그: 모든 탐험 지역과 시설을 연다.
        /// 지역은 데이터에서 읽는다: 탐험 행동(LocationId 있음)의 해금 조건 FlagsAll을 전부 켠다. 막이 늘어나도 따로 고칠 필요가 없다.
        /// 시설은 정의된 것 전부를 최소 1레벨로 만든다(이미 높으면 그대로).
        /// </summary>
        public void DebugUnlockAll()
        {
            foreach (var flag in DebugUnlockExtraFlags)
            {
                State.Flags.Add(flag);
            }

            foreach (var action in _db.Actions)
            {
                if (string.IsNullOrEmpty(action.LocationId) || action.Unlock == null)
                {
                    continue;
                }

                foreach (var flag in action.Unlock.FlagsAll)
                {
                    State.Flags.Add(flag);
                }
            }

            foreach (var facility in _db.Extras.Facilities)
            {
                State.Base.Facilities.TryGetValue(facility.Id, out var level);
                State.Base.Facilities[facility.Id] = Math.Max(level, 1);
            }

            StateChanged?.Invoke();
        }

        /// <summary>M6 디버그: 주인공 감염도 변화.</summary>
        public void DebugInfect(int amount)
        {
            EffectApplier.ChangeVital(State, VitalType.Infection, amount, _db.Balance, new StateDelta());
            StateChanged?.Invoke();
        }

        /// <summary>M6 디버그: 선생님 재감염 (이미 재감염이면 진행도 +amount).</summary>
        public void DebugTeacherInfection(int amount)
        {
            var effect = new GameEffect { TeacherReinfect = true, TeacherInfection = State.Teacher.Reinfected ? amount : 0 };
            EffectApplier.Apply(State, effect, _db.Balance, new StateDelta());
            StateChanged?.Invoke();
        }

        /// <summary>M6 디버그: 날씨 순환.</summary>
        public void DebugCycleWeather()
        {
            var values = (WeatherType[])Enum.GetValues(typeof(WeatherType));
            State.Base.Weather = values[((int)State.Base.Weather + 1) % values.Length];
            StateChanged?.Invoke();
        }

        /// <summary>M6 디버그: 구조 공지 플래그 토글.</summary>
        public void DebugToggleFlag(string flag)
        {
            if (!State.Flags.Remove(flag))
            {
                State.Flags.Add(flag);
            }

            StateChanged?.Invoke();
        }

        public void DebugTogglePowerRestored()
        {
            State.Base.PowerRestored = !State.Base.PowerRestored;
            StateChanged?.Invoke();
        }

        /// <summary>디버그: 지금 상태로 판을 끝낸다 (구조 또는 원인을 지정한 게임오버). 계획 단계에서만.</summary>
        public void DebugEndGame(GameStatus status, string endCause = "")
        {
            if (!IsPlanning || status == GameStatus.Playing)
            {
                return;
            }

            State.Status = status;
            State.EndCause = status == GameStatus.GameOver ? (string.IsNullOrEmpty(endCause) ? WeekRunner.EndCauseHpZero : endCause) : "";
            ResolveEnding();
            StateChanged?.Invoke();
            GameEnded?.Invoke(status);
        }

        /// <summary>엔딩 판정과 갤러리 기록 (판마다 한 번).</summary>
        private void ResolveEnding()
        {
            if (Ending != null)
            {
                return;
            }

            Ending = EndingResolver.Resolve(State, _db);
            EndingIsNew = Ending.Ending != null && Gallery != null && Gallery.Record(Ending.Ending.Id, State.Calendar.Week);
        }

        private void Attach(GameState state)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            Ending = null;
            EndingIsNew = false;
            _executing = false;
            _runner = new WeekRunner(state, _db, new SeededRandom(state.Rng), _expedition)
            {
                DeferExpeditions = _useRealtimeExpeditions,
                DeferEvents = _interactiveEvents,
            };
            StateChanged?.Invoke();
        }

        private void CheckEnded()
        {
            if (State.Status != GameStatus.Playing)
            {
                GameEnded?.Invoke(State.Status);
            }
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

        /// <summary>디버그: 다음 낮 탐험에 적대적 생존자를 이 성향으로 (0 = 정상 판정). 1회만.</summary>
        public int ForceHostile { get; set; }

        /// <summary>강제 성향이 있으면 요청에 싣고 지운다 (낮 탐험만). 실시간·헤드리스 모두 출발 전에 부른다.</summary>
        public void ApplyForcedHostile(ExpeditionRequest request)
        {
            if (ForceHostile > 0 && request != null && request.TimeOfDay == ExpeditionTime.Day)
            {
                request.HostileTemperament = ForceHostile;
                ForceHostile = 0;
            }
        }

        public ExpeditionResult Run(ExpeditionRequest request)
        {
            ApplyForcedHostile(request);
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
