using System;
using System.Collections.Generic;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
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

        public readonly StateDelta Delta = new StateDelta();

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

        private readonly GameState _state;
        private readonly IDefinitionDatabase _db;
        private readonly IRandomService _random;
        private readonly IExpeditionRunner _expeditionRunner;
        private readonly List<SlotOutcome> _outcomes = new List<SlotOutcome>();

        public WeekRunner(GameState state, IDefinitionDatabase db, IRandomService random, IExpeditionRunner expeditionRunner)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _expeditionRunner = expeditionRunner ?? throw new ArgumentNullException(nameof(expeditionRunner));
        }

        public event Action<SlotOutcome> SlotResolved;

        public event Action<WeekSummary> WeekEnded;

        public bool IsWeekComplete => _state.Calendar.WeekComplete;

        public bool IsGameOver => _state.Status != GameStatus.Playing;

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

            var calendar = _state.Calendar;
            var player = _state.Player;
            var index = CalendarRules.SlotIndex(calendar.Day, calendar.Phase);
            var slot = _state.Plan.Slots[index];
            var outcome = new SlotOutcome { Week = calendar.Week, Day = calendar.Day, Phase = calendar.Phase };
            var delta = outcome.Delta;

            if (calendar.Phase == DayPhase.Night)
            {
                UpkeepRules.ApplyDaily(_state, Balance, delta);
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
                outcome.Result = RunExpedition(action, index, consecutive, outcome);
            }
            else
            {
                if (calendar.Phase == DayPhase.Day && action.Category != ActionCategory.Education)
                {
                    ActionResolver.ResetRoutine(player);
                }

                outcome.Result = ActionResolver.Resolve(_state, action, _db, _random, delta);
            }

            if (calendar.Phase == DayPhase.Night)
            {
                SleepDebtRules.OnNightSlotResolved(_state, action.IsRest, Balance, delta);
            }

            player.LastSlotWasExpedition = action.IsExpedition;

            if (player.Vitals.Hp <= 0)
            {
                _state.Status = GameStatus.GameOver;
                _state.EndCause = EndCauseHpZero;
            }

            outcome.StopForPresentation = outcome.Result != CheckResult.Success || action.IsExpedition || violation || IsGameOver;

            Advance();
            _outcomes.Add(outcome);
            SlotResolved?.Invoke(outcome);
            return outcome;
        }

        /// <summary>주간 정산: 주간 소모, 일기, 주 넘김(또는 52주 종료 → 구조).</summary>
        public WeekSummary FinishWeek()
        {
            if (!IsWeekComplete)
            {
                throw new InvalidOperationException("아직 이번 주 슬롯이 남아 있음");
            }

            var calendar = _state.Calendar;
            var summary = new WeekSummary { Week = calendar.Week, DiaryKey = $"DIARY_W{calendar.Week:00}" };
            UpkeepRules.ApplyWeekly(_state, Balance, summary.Delta);
            foreach (var outcome in _outcomes)
            {
                summary.Slots.Add(outcome);
                summary.Delta.Merge(outcome.Delta);
            }

            _outcomes.Clear();
            _state.Diary.Add(new DiaryEntry { Week = calendar.Week, TextKey = summary.DiaryKey });

            if (calendar.Week >= CalendarState.TotalWeeks)
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

        private CheckResult RunExpedition(ActionDef action, int index, bool consecutive, SlotOutcome outcome)
        {
            var delta = outcome.Delta;
            var player = _state.Player;
            var request = ExpeditionRequestFactory.Create(_state, action, _random.NextSeed(), consecutive);
            var result = _expeditionRunner.Run(request);
            outcome.Expedition = result;

            if (action.ExpeditionTime == ExpeditionTime.Night)
            {
                _state.Exploration.NightExplorations++;
                EffectApplier.ChangeSleepDebt(_state, Balance.NightExpeditionSleepDebt, Balance, delta);
            }
            else
            {
                _state.Exploration.DayExplorations++;
            }

            EffectApplier.ChangeVital(_state, VitalType.Fatigue, (int)Math.Round(result.FatigueDelta), Balance, delta);
            var stress = (int)Math.Round(result.StressDelta);
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
    }
}
