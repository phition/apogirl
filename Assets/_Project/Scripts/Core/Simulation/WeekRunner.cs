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

        /// <summary>이벤트 대기 중: ResolveEvent(choiceId)를 호출해야 슬롯이 끝난다.</summary>
        public bool AwaitingEvent;

        /// <summary>이 슬롯 뒤에 일어난 이벤트와 선택 (연쇄 이벤트 포함, 순서대로).</summary>
        public readonly List<EventChoiceResult> Events = new List<EventChoiceResult>();

        /// <summary>UI가 멈춰서 연출해야 하는 슬롯 (대성공·실패·탐험·위반·게임 종료). 나머지는 빠른 진행.</summary>
        public bool StopForPresentation;
    }

    /// <summary>주간 정산 결과.</summary>
    public sealed class WeekSummary
    {
        public int Week;
        public readonly List<SlotOutcome> Slots = new List<SlotOutcome>();
        public readonly StateDelta Delta = new StateDelta();
        public string DiaryKey = "";
        public GameStatus StatusAfter;
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
        }

        public event Action<SlotOutcome> SlotResolved;

        public event Action<WeekSummary> WeekEnded;

        public bool IsWeekComplete => _state.Calendar.WeekComplete;

        public bool IsGameOver => _state.Status != GameStatus.Playing;

        /// <summary>true면 탐험 슬롯에서 멈추고 실시간 탐험 결과를 기다린다 (false면 IExpeditionRunner로 즉시 처리).</summary>
        public bool DeferExpeditions { get; set; }

        public bool HasPendingExpedition => _pending != null;

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
                UpkeepRules.ApplyDaily(_state, Balance, delta, (int)TraitRules.Sum(_state, _db, TraitModifierKind.FoodSaving));
            }

            var actionId = string.IsNullOrEmpty(slot.ActionId) ? PlanPreparer.RestIdFor(index, Balance) : slot.ActionId;
            var action = _db.GetAction(actionId);
            outcome.ActionId = actionId;

            var consecutive = action.IsExpedition && player.LastSlotWasExpedition;
            var violation = slot.ForcedRest && !action.IsRest;
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

            if (calendar.Phase == DayPhase.Night && !action.IsExpedition && player.Vitals.Hp > 0)
            {
                outcome.Raid = RaidRules.RollNight(_state, action, Balance, _random, delta);
            }

            if (player.Vitals.Hp <= 0)
            {
                _state.Status = GameStatus.GameOver;
                _state.EndCause = HpZeroCause(outcome);
            }

            outcome.StopForPresentation = outcome.Result != CheckResult.Success || action.IsExpedition || violation || IsGameOver || outcome.Raid != null;

            // 이벤트 판정은 달력을 넘기기 전 (지금 슬롯의 요일·낮밤 기준)
            EventDef ev = null;
            var context = new EventContext { Phase = calendar.Phase, Day = calendar.Day, ActionId = action.Id, ActionCategory = action.Category };
            if (!IsGameOver)
            {
                ev = EventScheduler.Pick(_state, _db, context, _random);
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
            var result = EventResolver.Choose(_state, pending.Event, choiceId, pending.Context, Balance, _random, pending.Outcome.Delta);
            pending.Outcome.Events.Add(result);
            CheckGameOver();
            if (!IsGameOver && !string.IsNullOrEmpty(result.NextEventId) && _db.TryGetEvent(result.NextEventId, out var next))
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
            if (vitals.Hunger >= Balance.StarvationThreshold || vitals.Thirst >= Balance.DehydrationThreshold)
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
            if (calendar.Week >= Balance.RaidStartWeek - 1)
            {
                EffectApplier.ChangeThreat(_state, Balance.ThreatWeekly, Balance, summary.Delta);
            }

            CheckBreakdown();
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
                calendar.Day = 1;
                calendar.Phase = DayPhase.Day;
                calendar.WeekComplete = false;
                _state.Plan = PlanPreparer.CreateWeekPlan(_state, Balance);
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
            }

            ApplyEquipment(outcome.ExpeditionRequest, result, delta);

            EffectApplier.ChangeVital(_state, VitalType.Fatigue, (int)Math.Round(result.FatigueDelta), Balance, delta);
            var stress = (int)Math.Round(result.StressDelta * (1f + TraitRules.Sum(_state, _db, TraitModifierKind.ExpeditionStress)));
            EffectApplier.ChangeVital(_state, VitalType.Stress, StatRules.ScaleStress(stress, player.SleepDebt, Balance), Balance, delta);
            EffectApplier.ChangeVital(_state, VitalType.Hp, result.HpDelta, Balance, delta);
            EffectApplier.ChangeSleepDebt(_state, result.SleepDebtDelta, Balance, delta);
            EffectApplier.ChangeConscience(_state, result.ConscienceDelta, Balance, delta);
            if (result.SleepDebtDelta > 0)
            {
                player.ReducedNextRest = true;
            }

            foreach (var loot in result.ResourceLoot)
            {
                EffectApplier.ChangeResource(_state, loot.Resource, loot.Amount, delta);
            }

            foreach (var item in result.Loot)
            {
                _state.Storage.Add(item);
            }

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
