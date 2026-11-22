using System;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Events;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Planning;
using ApoGirl.Core.Random;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Simulation
{
    /// <summary>슬롯 하나의 실행 결과.</summary>
    public sealed class SlotOutcome
    {
        public int Week;
        public int Day;
        public DayPhase Phase;
        public string ActionId = "";

        /// <summary>이 칸에 곁에 있던 사람 (함께한 시간). 비어 있으면 혼자.</summary>
        public List<TogetherEntry> Together = new List<TogetherEntry>();
        public CheckResult Result;
        public bool ForcedRestViolated;
        public bool ConsecutiveExpedition;

        /// <summary>탐험 슬롯이었다면 결과, 아니면 null.</summary>
        public ExpeditionResult Expedition;

        /// <summary>탐험 슬롯의 요청 (실시간 탐험에 넘길 데이터).</summary>
        public ExpeditionRequest ExpeditionRequest;

        /// <summary>실시간 탐험 대기 중: CompleteExpedition(result)를 호출해야 슬롯이 끝난다.</summary>
        public bool AwaitingExpedition;

        public readonly StateDelta Delta = new StateDelta();

        /// <summary>이 밤에 일어난 습격 (없으면 null).</summary>
        public RaidResult Raid;

        /// <summary>M6: 탐험에서 물렸다면 결과 (없으면 null).</summary>
        public BiteResult Bite;

        /// <summary>낮 침입으로 이 슬롯의 행동이 취소되었다 (침입 이벤트로 대응).</summary>
        public bool IntrusionTriggered;

        /// <summary>낮 침입 대응 결과 (없으면 null).</summary>
        public IntrusionResult Intrusion;

        /// <summary>이벤트 대기 중: ResolveEvent(choiceId)를 호출해야 슬롯이 끝난다.</summary>
        public bool AwaitingEvent;

        /// <summary>이 슬롯 뒤에 일어난 이벤트와 선택 (연쇄 이벤트 포함, 순서대로).</summary>
        public readonly List<EventChoiceResult> Events = new List<EventChoiceResult>();

        /// <summary>UI가 멈춰서 연출해야 하는 슬롯 (대성공·실패·탐험·위반·게임 종료). 나머지는 빠른 진행.</summary>
        public bool StopForPresentation;

        /// <summary>이번 슬롯(또는 이벤트)에서 새로 부상을 입었으면 상처 감염량 (아니면 0).</summary>
        public int WoundInfection;

        /// <summary>탐험에서 다쳤지만 응급 치료로 마무리했다 (응급처치 숙련, 부상까지는 아님).</summary>
        public bool FirstAidPatched;

        /// <summary>행동 직전 체력 (하루 소모 뒤). 부상 판정 기준.</summary>
        internal int HpBefore;
    }

    /// <summary>주간 정산 결과.</summary>
    public sealed class WeekSummary
    {
        public int Week;
        public readonly List<SlotOutcome> Slots = new List<SlotOutcome>();
        public readonly StateDelta Delta = new StateDelta();
        public string DiaryKey = "";
        public GameStatus StatusAfter;

        /// <summary>이번 정산에서 물자가 다시 채워진 지역 (매달 첫 주).</summary>
        public readonly List<string> Restocked = new List<string>();
    }

    /// <summary>
    /// 한 주(7일 × 낮/밤)를 한 슬롯씩 실행한다. 순서: 1일차 낮 → 1일차 밤(일일 소모) → … → 7일차 밤 → 주간 정산.
    /// </summary>
    public sealed class WeekRunner
    {
        public const string EndCauseHpZero = "END_CAUSE_HP_ZERO";

        // M5 배드 엔딩 원인 (EndingDef.EndCauses와 맞춘다)
        public const string EndCauseExpedition = "END_CAUSE_EXPEDITION";
        public const string EndCauseRaid = "END_CAUSE_RAID";
        public const string EndCauseStarvation = "END_CAUSE_STARVATION";
        public const string EndCauseCollapse = "END_CAUSE_COLLAPSE";
        public const string EndCauseInfection = "END_CAUSE_INFECTION";

        private readonly GameState _state;
        private readonly IDefinitionDatabase _db;
        private readonly IRandomService _random;
        private readonly IExpeditionRunner _expeditionRunner;
        private readonly List<SlotOutcome> _outcomes = new List<SlotOutcome>();
        private PendingExpedition _pending;
        private readonly IEventChooser _eventChooser;
        private PendingEventState _pendingEvent;

        public WeekRunner(
            GameState state, IDefinitionDatabase db, IRandomService random, IExpeditionRunner expeditionRunner, IEventChooser eventChooser = null)
        {
            _eventChooser = eventChooser ?? new FirstChoiceChooser();
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _expeditionRunner = expeditionRunner ?? throw new ArgumentNullException(nameof(expeditionRunner));
            FactionRules.EnsureInitialized(_state, _db);
        }

        public event Action<SlotOutcome> SlotResolved;

        public event Action<WeekSummary> WeekEnded;

        public bool IsWeekComplete => _state.Calendar.WeekComplete;

        public bool IsGameOver => _state.Status != GameStatus.Playing;

        /// <summary>true면 탐험 슬롯에서 멈추고 실시간 탐험 결과를 기다린다 (false면 IExpeditionRunner로 즉시 처리).</summary>
        public bool DeferExpeditions { get; set; }

        public bool HasPendingExpedition => _pending != null;

        /// <summary>대기 중인 탐험 슬롯의 행동 (탐험 중 이벤트 조건 판정용).</summary>
        public ActionDef PendingExpeditionAction => _pending?.Action;

        /// <summary>탐험 중 치른 이벤트가 이어 부른 다음 이벤트 (슬롯을 끝낼 때 바로 시작).</summary>
        private string _chainedEventId;

        public ExpeditionRequest PendingRequest => _pending?.Outcome.ExpeditionRequest;

        /// <summary>true면 이벤트에서 멈추고 ResolveEvent를 기다린다 (false면 IEventChooser가 즉시 고른다).</summary>
        public bool DeferEvents { get; set; }

        public bool HasPendingEvent => _pendingEvent != null;

        /// <summary>진행 중인 이벤트 (없으면 null).</summary>
        public EventDef PendingEvent => _pendingEvent?.Event;

        /// <summary>진행 중인 이벤트의 선택지 (숨김 제외).</summary>
        public IReadOnlyList<EventChoiceView> PendingChoices => _pendingEvent?.Choices;

        private BalanceConfig Balance => _db.Balance;

        /// <summary>계획 확정 직후 호출: 강제 휴식 표시·자동 배치.</summary>
        public void PrepareWeek()
        {
            PlanPreparer.ApplyForcedRest(_state, _db);
        }

        public SlotOutcome ExecuteNextSlot()
        {
            if (IsGameOver)
            {
                throw new InvalidOperationException("게임이 이미 종료됨");
            }

            if (IsWeekComplete)
            {
                throw new InvalidOperationException("이번 주 슬롯을 모두 실행함. FinishWeek()를 먼저 호출할 것");
            }

            if (HasPendingExpedition)
            {
                throw new InvalidOperationException("탐험 결과를 기다리는 중. CompleteExpedition()을 먼저 호출할 것");
            }

            ThrowIfEventPending();

            var calendar = _state.Calendar;
            var player = _state.Player;
            var index = CalendarRules.SlotIndex(calendar.Day, calendar.Phase);
            var slot = _state.Plan.Slots[index];
            var outcome = new SlotOutcome { Week = calendar.Week, Day = calendar.Day, Phase = calendar.Phase };
            var delta = outcome.Delta;

            if (calendar.Phase == DayPhase.Night)
            {
                // 식량 절약은 짝수 날(주 3번)에만: 그 차례 번호로 소수점을 나눠 쌓는다 (요리 ×0.5 → 하루건너 1)
                var savingStep = (calendar.Week - 1) * (CalendarState.DaysPerWeek / 2) + calendar.Day / 2;
                UpkeepRules.ApplyDaily(_state, Balance, delta, TraitRules.Spread(TraitRules.Sum(_state, _db, TraitModifierKind.FoodSaving), savingStep));
            }

            outcome.HpBefore = player.Vitals.Hp;
            var actionId = string.IsNullOrEmpty(slot.ActionId) ? PlanPreparer.RestIdFor(index, Balance) : slot.ActionId;
            var action = _db.GetAction(actionId);

            // 낮 침입 (3막부터): 그날 낮 행동을 취소하고 침입 이벤트로 대응한다
            if (calendar.Phase == DayPhase.Day && _db.TryGetAction(Balance.IntrusionActionId, out var intrusionAction)
                && _db.TryGetEvent(Balance.IntrusionEventId, out _) && IntrusionRules.Roll(_state, Balance, _random, _db))
            {
                outcome.IntrusionTriggered = true;
                action = intrusionAction;
                actionId = action.Id;
            }

            outcome.ActionId = actionId;

            var consecutive = action.IsExpedition && player.LastSlotWasExpedition;
            var violation = slot.ForcedRest && !action.IsRest && !outcome.IntrusionTriggered;
            if (violation)
            {
                outcome.ForcedRestViolated = true;
                EffectApplier.ChangeVital(_state, VitalType.Fatigue, Balance.ViolationFatigue, Balance, delta);
                EffectApplier.ChangeVital(_state, VitalType.Stress, Balance.ViolationStress, Balance, delta);
                if (!consecutive)
                {
                    EffectApplier.ChangeSleepDebt(_state, Balance.ForcedRestViolationSleepDebt, Balance, delta);
                }
            }

            if (consecutive)
            {
                outcome.ConsecutiveExpedition = true;
                EffectApplier.ChangeSleepDebt(_state, Balance.ConsecutiveExpeditionSleepDebt, Balance, delta);
            }

            if (action.IsExpedition)
            {
                outcome.ExpeditionRequest = ExpeditionRequestFactory.Create(_state, action, _random.NextSeed(), consecutive, _db);
                _pending = new PendingExpedition { Action = action, Index = index, Violation = violation, Outcome = outcome };
                if (DeferExpeditions)
                {
                    outcome.AwaitingExpedition = true;
                    return outcome;
                }

                return CompleteExpedition(_expeditionRunner.Run(outcome.ExpeditionRequest));
            }

            if (calendar.Phase == DayPhase.Day && action.Category != ActionCategory.Education)
            {
                ActionResolver.ResetRoutine(player);
            }

            outcome.Result = ActionResolver.Resolve(_state, action, _db, _random, delta);

            // 함께한 시간: 이 방에 서 있던 동료·가르친 선생님의 호감 (역할 재기획 4절)
            outcome.Together = TogetherRules.Apply(_state, _db, action, delta);
            return FinishSlot(action, violation, outcome);
        }

        /// <summary>대기 중인 탐험 슬롯에 결과를 반영하고 슬롯을 끝낸다.</summary>
        public SlotOutcome CompleteExpedition(ExpeditionResult result)
        {
            if (_pending == null)
            {
                throw new InvalidOperationException("대기 중인 탐험이 없음");
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var pending = _pending;
            _pending = null;
            pending.Outcome.AwaitingExpedition = false;
            pending.Outcome.Result = ApplyExpedition(pending.Action, pending.Index, result, pending.Outcome);
            return FinishSlot(pending.Action, pending.Violation, pending.Outcome);
        }

        private SlotOutcome FinishSlot(ActionDef action, bool violation, SlotOutcome outcome)
        {
            var calendar = _state.Calendar;
            var player = _state.Player;
            var delta = outcome.Delta;
            if (calendar.Phase == DayPhase.Night)
            {
                SleepDebtRules.OnNightSlotResolved(_state, action.IsRest, Balance, delta);
            }

            player.LastSlotWasExpedition = action.IsExpedition;

            // M4: 실행 횟수, 위협도, 밤 습격
            _state.Exploration.ActionCounts.TryGetValue(action.Id, out var count);
            _state.Exploration.ActionCounts[action.Id] = count + 1;
            if (action.ThreatGain != 0)
            {
                EffectApplier.ChangeThreat(_state, action.ThreatGain, Balance, delta);
            }

            if (calendar.Phase == DayPhase.Night && !action.IsExpedition && !action.Confined && player.Vitals.Hp > 0)
            {
                var companionGuard = TraitRules.RoundInt(TraitRules.Sum(_state, _db, TraitModifierKind.RaidGuard));
                outcome.Raid = RaidRules.RollNight(_state, action, Balance, _random, delta, companionGuard, _db);
            }

            // 수학여행처럼 뒤 칸까지 쓰는 행동: 뒤 칸을 정해진 행동으로 잠근다
            if (action.LockNextSlots > 0 && !string.IsNullOrEmpty(action.LockActionId) && _db.TryGetAction(action.LockActionId, out _))
            {
                var index = CalendarRules.SlotIndex(calendar.Day, calendar.Phase);
                for (var k = 1; k <= action.LockNextSlots && index + k < WeekPlan.SlotCount; k++)
                {
                    PlanPreparer.LockAction(_state.Plan, index + k, action.LockActionId);
                }
            }

            if (player.Vitals.Hp <= 0)
            {
                _state.Status = GameStatus.GameOver;
                _state.EndCause = HpZeroCause(outcome);
            }

            // 부상: 이 슬롯(탐험·습격 포함)에서 체력을 크게 잃었으면
            outcome.WoundInfection = InjuryRules.CheckIncident(_state, outcome.HpBefore, Balance, delta);
            outcome.FirstAidPatched = action.IsExpedition && outcome.WoundInfection == 0 && InjuryRules.PatchedUp(_state, outcome.HpBefore, Balance);

            outcome.StopForPresentation = outcome.Result != CheckResult.Success || action.IsExpedition || violation || IsGameOver || outcome.Raid != null
                                          || outcome.WoundInfection > 0;

            // 이벤트 판정은 달력을 넘기기 전 (지금 슬롯의 요일·낮밤 기준)
            EventDef ev = null;
            var context = new EventContext { Phase = calendar.Phase, Day = calendar.Day, ActionId = action.Id, ActionCategory = action.Category };
            if (!IsGameOver)
            {
                var chained = _chainedEventId;
                _chainedEventId = null;
                ev = outcome.IntrusionTriggered && _db.TryGetEvent(Balance.IntrusionEventId, out var intrusion)
                    ? intrusion
                    : !string.IsNullOrEmpty(chained) && _db.TryGetEvent(chained, out var next) ? next
                    : EventScheduler.Pick(_state, _db, context, _random);
            }

            Advance();
            _outcomes.Add(outcome);
            if (ev != null)
            {
                StartEvent(ev, context, outcome);
                if (HasPendingEvent)
                {
                    return outcome;
                }
            }

            SlotResolved?.Invoke(outcome);
            return outcome;
        }

        /// <summary>대기 중인 이벤트에 선택을 반영한다. 연쇄 이벤트가 있으면 다시 대기 상태가 된다.</summary>
        public SlotOutcome ResolveEvent(string choiceId)
        {
            if (_pendingEvent == null)
            {
                throw new InvalidOperationException("대기 중인 이벤트가 없음");
            }

            var pending = _pendingEvent;
            _pendingEvent = null;
            ApplyChoice(pending, choiceId);
            AutoResolveEvents();
            if (!HasPendingEvent)
            {
                pending.Outcome.AwaitingEvent = false;
                SlotResolved?.Invoke(pending.Outcome);
            }

            return pending.Outcome;
        }

        /// <summary>디버그: 조건을 무시하고 이벤트를 지금 시작한다.</summary>
        public SlotOutcome ForceEvent(string eventId)
        {
            ThrowIfEventPending();
            var ev = _db.Events.FirstOrDefault(e => e.Id == eventId) ?? throw new ArgumentException($"이벤트 없음: '{eventId}'");
            var outcome = new SlotOutcome { Week = _state.Calendar.Week, Day = _state.Calendar.Day, Phase = _state.Calendar.Phase, ActionId = "" };
            StartEvent(ev, EventContext.FromCalendar(_state), outcome);
            return outcome;
        }

        private void StartEvent(EventDef ev, EventContext context, SlotOutcome outcome)
        {
            EventResolver.Begin(_state, ev, context, Balance, outcome.Delta);
            CheckGameOver();
            _pendingEvent = new PendingEventState
            {
                Event = ev,
                Context = context,
                Outcome = outcome,
                Choices = EventResolver.Choices(_state, ev, context, Balance),
            };
            outcome.AwaitingEvent = true;
            outcome.StopForPresentation = true;
            AutoResolveEvents();
            if (!HasPendingEvent)
            {
                outcome.AwaitingEvent = false;
            }
        }

        private void ApplyChoice(PendingEventState pending, string choiceId)
        {
            var hpBefore = _state.Player.Vitals.Hp;
            var result = EventResolver.Choose(_state, pending.Event, choiceId, pending.Context, Balance, _random, pending.Outcome.Delta);
            pending.Outcome.Events.Add(result);

            // 낮 침입 대응: 고른 선택지의 대응 방식으로 판정
            var chosen = pending.Event.Choices.Find(c => c.Id == result.ChoiceId);
            var effect = chosen == null ? null : result.Result == CheckResult.Failure ? chosen.OnFailure : chosen.Effect;
            if (effect != null && !string.IsNullOrEmpty(effect.IntrusionResponse))
            {
                pending.Outcome.Intrusion = IntrusionRules.Resolve(_state, _db, effect.IntrusionResponse, Balance, _random, pending.Outcome.Delta);
            }

            // 부상: 이벤트 선택(침입 대응 등)으로 체력을 크게 잃었으면
            var wound = InjuryRules.CheckIncident(_state, hpBefore, Balance, pending.Outcome.Delta);
            if (wound > 0)
            {
                pending.Outcome.WoundInfection = wound;
            }

            CheckGameOver();
            var nextId = string.IsNullOrEmpty(result.NextEventId) ? pending.Event.ThenEventId : result.NextEventId;
            if (!IsGameOver && !string.IsNullOrEmpty(nextId) && _db.TryGetEvent(nextId, out var next))
            {
                EventResolver.Begin(_state, next, pending.Context, Balance, pending.Outcome.Delta);
                _pendingEvent = new PendingEventState
                {
                    Event = next,
                    Context = pending.Context,
                    Outcome = pending.Outcome,
                    Choices = EventResolver.Choices(_state, next, pending.Context, Balance),
                };
            }
        }

        /// <summary>
        /// 탐험 중 치른 이벤트를 적용한다. 이미 본 1회성 이벤트·없는 이벤트는 건너뛰고,
        /// 고른 선택지를 지금 고를 수 없으면(조건·비용이 바뀜) 고를 수 있는 첫 선택지로 마무리한다.
        /// </summary>
        private void ApplyFieldEvents(ActionDef action, ExpeditionResult result, SlotOutcome outcome)
        {
            var calendar = _state.Calendar;
            var context = new EventContext { Phase = calendar.Phase, Day = calendar.Day, ActionId = action.Id, ActionCategory = action.Category };
            foreach (var field in result.FieldEvents)
            {
                if (field == null || !_db.TryGetEvent(field.EventId, out var ev) || !EventScheduler.IsAvailable(_state, ev))
                {
                    continue;
                }

                EventResolver.Begin(_state, ev, context, Balance, outcome.Delta);
                var choices = EventResolver.Choices(_state, ev, context, Balance);
                var choiceId = choices.Exists(c => c.Id == field.ChoiceId && c.Enabled)
                    ? field.ChoiceId
                    : choices.Find(c => c.Enabled)?.Id ?? EventResolver.ContinueChoiceId;
                var chosen = EventResolver.Choose(_state, ev, choiceId, context, Balance, _random, outcome.Delta);
                outcome.Events.Add(chosen);
                var nextId = string.IsNullOrEmpty(chosen.NextEventId) ? ev.ThenEventId : chosen.NextEventId;
                if (!string.IsNullOrEmpty(nextId))
                {
                    _chainedEventId = nextId;
                }
            }

            CheckGameOver();
        }

        /// <summary>DeferEvents가 꺼져 있으면 선택자로 바로 진행.</summary>
        private void AutoResolveEvents()
        {
            while (!DeferEvents && _pendingEvent != null)
            {
                var pending = _pendingEvent;
                _pendingEvent = null;
                ApplyChoice(pending, _eventChooser.Choose(_state, pending.Event, pending.Choices));
            }
        }

        private void CheckGameOver()
        {
            if (_state.Status == GameStatus.Playing && _state.Player.Vitals.Hp <= 0)
            {
                _state.Status = GameStatus.GameOver;
                _state.EndCause = EndCauseHpZero;
            }
        }

        /// <summary>체력이 0이 된 슬롯의 상황으로 종료 원인을 정한다.</summary>
        private string HpZeroCause(SlotOutcome outcome)
        {
            if (outcome.Expedition != null)
            {
                return EndCauseExpedition;
            }

            if (outcome.Raid != null && !outcome.Raid.Repelled)
            {
                return EndCauseRaid;
            }

            var vitals = _state.Player.Vitals;
            if (vitals.Satiety <= Balance.StarvationThreshold || vitals.Hydration <= Balance.DehydrationThreshold)
            {
                return EndCauseStarvation;
            }

            return EndCauseHpZero;
        }

        /// <summary>정신 붕괴: 정산 때 스트레스가 한계인 주가 이어지면 종료 (경고는 일기·코멘트 조건 BreakdownWeeks).</summary>
        private void CheckBreakdown()
        {
            var player = _state.Player;
            if (player.Vitals.Stress >= Balance.CollapseStress)
            {
                player.BreakdownWeeks++;
                if (_state.Status == GameStatus.Playing && Balance.CollapseWeeks > 0 && player.BreakdownWeeks >= Balance.CollapseWeeks)
                {
                    _state.Status = GameStatus.GameOver;
                    _state.EndCause = EndCauseCollapse;
                }
            }
            else
            {
                player.BreakdownWeeks = 0;
            }
        }

        private void RecordWeek(int week)
        {
            var player = _state.Player;
            var record = new WeekRecord { Week = week, Hp = player.Vitals.Hp, Stress = player.Vitals.Stress, SleepDebt = player.SleepDebt };
            foreach (StatType stat in Enum.GetValues(typeof(StatType)))
            {
                record.Stats[stat] = player.GetStat(stat);
            }

            _state.History.RemoveAll(r => r.Week == week);
            _state.History.Add(record);
        }

        private void ThrowIfEventPending()
        {
            if (HasPendingEvent)
            {
                throw new InvalidOperationException("이벤트 선택을 기다리는 중. ResolveEvent()를 먼저 호출할 것");
            }
        }

        /// <summary>주간 정산: 주간 소모, 일기, 주 넘김(또는 52주 종료 → 구조).</summary>
        public WeekSummary FinishWeek()
        {
            if (!IsWeekComplete)
            {
                throw new InvalidOperationException("아직 이번 주 슬롯이 남아 있음");
            }

            ThrowIfEventPending();

            var calendar = _state.Calendar;
            var summary = new WeekSummary { Week = calendar.Week, DiaryKey = "" };
            UpkeepRules.ApplyWeekly(_state, Balance, summary.Delta);
            BondDecayRules.ApplyWeekly(_state, _db, summary.Delta); // 기획 C.5
            FactionRules.ApplyWeekly(_state, _db, Balance, summary.Delta);
            if (calendar.Week >= Balance.RaidStartWeek - 1)
            {
                EffectApplier.ChangeThreat(_state, Balance.ThreatWeekly, Balance, summary.Delta);
            }

            CheckBreakdown();

            // M6: 감염 진행 (주인공 → 최대치면 감염 배드 엔딩, 선생님 → 병세 단계)
            if (InfectionRules.WeeklyPlayer(_state, _db, summary.Delta) && _state.Status == GameStatus.Playing)
            {
                _state.Status = GameStatus.GameOver;
                _state.EndCause = EndCauseInfection;
            }

            InfectionRules.WeeklyTeacher(_state, _db);
            AssaultTraumaRules.Care(_state, 1, Balance);
            IntrusionRules.HealWeekly(_state);
            _state.Exploration.DayExposure = Math.Max(0, _state.Exploration.DayExposure - Balance.ExposureDecayPerWeek);
            RecordWeek(calendar.Week);
            var diary = LineRules.Pick(_state, _db, LineKind.Diary, new EventContext { Phase = DayPhase.Night, Day = CalendarState.DaysPerWeek });
            foreach (var outcome in _outcomes)
            {
                summary.Slots.Add(outcome);
                summary.Delta.Merge(outcome.Delta);
            }

            _outcomes.Clear();
            summary.DiaryKey = diary?.TextKey ?? $"DIARY_W{calendar.Week:00}";
            if (!string.IsNullOrEmpty(summary.DiaryKey))
            {
                _state.Diary.Add(new DiaryEntry { Week = calendar.Week, TextKey = summary.DiaryKey });
            }

            if (_state.Status != GameStatus.Playing)
            {
                // 정산 중 종료 (정신 붕괴): 주를 넘기지 않는다
            }
            else if (calendar.Week >= CalendarState.TotalWeeks)
            {
                if (_state.Status == GameStatus.Playing)
                {
                    _state.Status = GameStatus.Rescued;
                }
            }
            else
            {
                _state.LastWeekPlan = PlanEditing.CloneActions(_state.Plan);
                calendar.Week++;

                // 매달 첫 주: 교실동을 뺀 지역의 물자가 일부 다시 채워진다
                if (ContainerRules.MonthStarted(calendar.Week - 1, calendar.Week, Balance))
                {
                    summary.Restocked.AddRange(ContainerRules.Restock(_state, _db, _random, Balance));
                }

                calendar.Day = 1;
                calendar.Phase = DayPhase.Day;
                calendar.WeekComplete = false;
                _state.Plan = PlanPreparer.CreateWeekPlan(_state, Balance);
                _state.Base.Weather = WeatherRules.Roll(calendar.Week, Balance, _random);
            }

            summary.StatusAfter = _state.Status;
            WeekEnded?.Invoke(summary);
            return summary;
        }

        private CheckResult ApplyExpedition(ActionDef action, int index, ExpeditionResult result, SlotOutcome outcome)
        {
            var delta = outcome.Delta;
            var player = _state.Player;
            outcome.Expedition = result;

            if (action.ExpeditionTime == ExpeditionTime.Night)
            {
                _state.Exploration.NightExplorations++;
                EffectApplier.ChangeSleepDebt(_state, Balance.NightExpeditionSleepDebt, Balance, delta);
                EffectApplier.ChangeThreat(_state, Balance.ThreatPerNightExpedition, Balance, delta);
            }
            else
            {
                _state.Exploration.DayExplorations++;
                EffectApplier.ChangeThreat(_state, Balance.ThreatPerDayExpedition, Balance, delta);

                // 생존자가 다니는 곳에 낮에 나가면 눈에 띈다 (교실동·튜토리얼 제외)
                if (!_db.TryGetLocation(action.LocationId, out var visited) || visited.DayVariant.HostileChanceMultiplier > 0f)
                {
                    _state.Exploration.DayExposure = Math.Min(Balance.MaxExposure, _state.Exploration.DayExposure + 1);
                }
            }

            ApplyEquipment(outcome.ExpeditionRequest, result, delta);
            ApplyHostile(outcome.ExpeditionRequest, result, delta);

            // 교실동 등: 뒤진 상자는 다음 출정에도 비어 있다
            foreach (var key in result.SearchedContainers)
            {
                _state.Exploration.LootedContainers.Add(key);
            }

            // 특정 방을 뒤졌다는 표시 (예: 1층 끝 교실 → 거처 이벤트)
            if (_db.TryGetLocation(action.LocationId, out var searchedLocation))
            {
                ContainerRules.ApplyRoomFlags(searchedLocation, result.SearchedContainers, _state, delta);
            }

            EffectApplier.ChangeVital(_state, VitalType.Fatigue, (int)Math.Round(result.FatigueDelta), Balance, delta);
            // 폭행 트라우마: 낮 탐험마다 스트레스가 더 오른다
            var traumaStress = action.ExpeditionTime == ExpeditionTime.Day ? AssaultTraumaRules.DayExpeditionStress(_state, Balance) : 0;
            var stress = (int)Math.Round((result.StressDelta + traumaStress) * (1f + TraitRules.Sum(_state, _db, TraitModifierKind.ExpeditionStress)));
            EffectApplier.ChangeVital(_state, VitalType.Stress, StatRules.ScaleStress(stress, player.SleepDebt, Balance), Balance, delta);
            EffectApplier.ChangeVital(_state, VitalType.Hp, result.HpDelta, Balance, delta);
            EffectApplier.ChangeSleepDebt(_state, result.SleepDebtDelta, Balance, delta);
            EffectApplier.ChangeConscience(_state, result.ConscienceDelta, Balance, delta);
            if (result.SleepDebtDelta > 0)
            {
                player.ReducedNextRest = true;
            }

            var difficulty = DifficultyRules.Get(_state, Balance);
            foreach (var loot in result.ResourceLoot)
            {
                var amount = DifficultyRules.Scale(loot.Amount, difficulty.Yield);
                var perResource = loot.Resource == ResourceType.Food ? difficulty.FoodYield
                    : loot.Resource == ResourceType.Water ? difficulty.WaterYield
                    : loot.Resource == ResourceType.Medicine ? difficulty.MedicineYield
                    : 1f;
                if (Math.Abs(perResource - 1f) > 0.0001f)
                {
                    // 난이도별 식량·물·의약품 배율: 적게 나오는 자원은 반올림으로는 안 바뀌므로 소수 부분은 확률로 (1.4 → 1, 40%로 2)
                    var scaled = amount * Math.Max(0f, perResource);
                    amount = (int)Math.Floor(scaled) + (_random.NextDouble() < scaled - Math.Floor(scaled) ? 1 : 0);
                }

                EffectApplier.ChangeResource(_state, loot.Resource, amount, delta);
            }

            foreach (var item in result.Loot)
            {
                _state.Storage.Add(item);
            }

            BondDecayRules.OnExpedition(_state, result, Balance, delta); // 기획 C.5: 선배는 빈손 귀환이 이어지면 실망

            foreach (var flag in result.Flags)
            {
                if (_state.Flags.Add(flag))
                {
                    delta.FlagsSet.Add(flag);
                }
            }

            foreach (var clue in result.Clues)
            {
                _state.Clues.Add(clue);
            }

            // 조우 선택 플래그에 세력이 반응한다 (예: 더불어 마트와 거래 → 평판 +)
            FactionRules.OnFlagsSet(_state, _db, result.Flags, delta);

            // 탐험 중 그 자리에서 치른 이벤트: 시작·선택 효과를 지금 적용하고 본 것으로 기록 (허브에서 다시 뜨지 않는다)
            ApplyFieldEvents(action, result, outcome);

            // M6: 정찰 정보는 한 번 쓰면 사라진다
            _state.Flags.Remove(ScoutRules.FlagFor(action.LocationId));

            // 복구 뒤 첫 방문: '흔적'은 한 번만 보여 준다
            _state.Flags.Remove(ContainerRules.RestockFlag(action.LocationId));

            // M6: 물림 (쓰러짐 또는 물림 부상) → 응급 처치 판정 (문서 3.1.1)
            outcome.Bite = InfectionRules.RollBite(
                _state, _db, _random, action.ExpeditionTime == ExpeditionTime.Night, result.Status == ExtractStatus.Down,
                (result.Injuries & InjuryFlags.Bite) != 0, delta);

            if (result.Status == ExtractStatus.Down)
            {
                _state.Exploration.Downs++;
                LockFollowingSlots(index, Balance.DownLockedRestSlots);
                return CheckResult.Failure;
            }

            // 7일차 밤 탐험의 강제 휴식은 다음 주 1일차 낮으로 넘어간다 (문서 4.2 주 경계)
            if (index + 1 >= WeekPlan.SlotCount)
            {
                _state.CarryOver.LockedRestSlots = Math.Max(_state.CarryOver.LockedRestSlots, 1);
            }

            return CheckResult.Success;
        }

        /// <summary>장비: 쓰러지면 착용 장비·퀵슬롯 전부 분실 (M4 결정), 아니면 쓴 소모품만 빠지고 무기가 닳는다.</summary>
        private void ApplyEquipment(ExpeditionRequest request, ExpeditionResult result, StateDelta delta)
        {
            var loadout = request?.Loadout;
            if (loadout == null)
            {
                return;
            }

            if (result.Status == ExtractStatus.Down && Balance.DownLosesEquipment)
            {
                foreach (var id in LoadoutRules.EquippedIds(loadout))
                {
                    LoadoutRules.RemoveFromStorage(_state, id, 1, delta);
                }

                foreach (var id in loadout.QuickSlots)
                {
                    LoadoutRules.RemoveFromStorage(_state, id, 1, delta);
                }

                LoadoutRules.Sanitize(_state);
                return;
            }

            foreach (var id in result.UsedItems)
            {
                LoadoutRules.RemoveFromStorage(_state, id, 1, delta);
            }

            LoadoutRules.WearItem(_state, _db, loadout.MainWeapon, result.WeaponWear, delta);
            LoadoutRules.Sanitize(_state);
        }

        /// <summary>적대적 생존자에게 붙잡힘: 강탈·구타는 들고 간 소모품도 잃고, 구타는 무기까지 잃고 트라우마가 생긴다.</summary>
        private void ApplyHostile(ExpeditionRequest request, ExpeditionResult result, StateDelta delta)
        {
            var stats = _state.Exploration;
            switch (result.Hostile)
            {
                case HostileOutcome.Evicted:
                    stats.HostileEvictions++;
                    return;
                case HostileOutcome.Robbed:
                    stats.HostileRobberies++;
                    break;
                case HostileOutcome.Beaten:
                    stats.HostileBeatings++;
                    break;
                default:
                    return;
            }

            var loadout = request?.Loadout;
            if (loadout != null)
            {
                var used = new List<string>(result.UsedItems);
                foreach (var id in loadout.QuickSlots)
                {
                    if (string.IsNullOrEmpty(id) || used.Remove(id))
                    {
                        continue;
                    }

                    LoadoutRules.RemoveFromStorage(_state, id, 1, delta);
                }

                if (result.Hostile == HostileOutcome.Beaten && !string.IsNullOrEmpty(loadout.MainWeapon))
                {
                    LoadoutRules.RemoveFromStorage(_state, loadout.MainWeapon, 1, delta);
                }

                LoadoutRules.Sanitize(_state);
            }

            if (result.Hostile == HostileOutcome.Beaten)
            {
                AssaultTraumaRules.Add(_state, Balance);
                EffectApplier.ChangeHidden(_state, HiddenType.TraumaRecovery, -Balance.BeatenTraumaRecoveryLoss, Balance, delta);
            }
        }

        private void LockFollowingSlots(int index, int count)
        {
            var overflow = 0;
            for (var k = 1; k <= count; k++)
            {
                var target = index + k;
                if (target < WeekPlan.SlotCount)
                {
                    PlanPreparer.LockRest(_state.Plan, target, Balance);
                }
                else
                {
                    overflow++;
                }
            }

            _state.CarryOver.LockedRestSlots = Math.Max(_state.CarryOver.LockedRestSlots, overflow);
        }

        private void Advance()
        {
            var calendar = _state.Calendar;
            if (calendar.Phase == DayPhase.Day)
            {
                calendar.Phase = DayPhase.Night;
            }
            else if (calendar.Day < CalendarState.DaysPerWeek)
            {
                calendar.Day++;
                calendar.Phase = DayPhase.Day;
            }
            else
            {
                calendar.WeekComplete = true;
            }
        }

        private sealed class PendingEventState
        {
            public EventDef Event;
            public EventContext Context;
            public SlotOutcome Outcome;
            public List<EventChoiceView> Choices;
        }

        private sealed class PendingExpedition
        {
            public ActionDef Action;
            public int Index;
            public bool Violation;
            public SlotOutcome Outcome;
        }
    }
}
