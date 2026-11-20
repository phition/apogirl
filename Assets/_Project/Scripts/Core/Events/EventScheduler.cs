using System;
using System.Collections.Generic;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Random;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Events
{
    /// <summary>
    /// 슬롯이 끝날 때 일어날 이벤트를 고른다.
    /// 1) 스토리·학사력·밤 이벤트: 조건이 맞는 것 중 우선순위가 가장 높은 하나.
    /// 2) 없으면 랜덤·위기 이벤트: 빈도 상한(하루·주) 이내에서 확률로 굴리고 가중치 추첨.
    /// 랜덤 후보가 없으면 난수를 쓰지 않는다 (기존 난수열 보존).
    /// </summary>
    public static class EventScheduler
    {
        public static EventDef Pick(GameState state, IDefinitionDatabase db, EventContext context, IRandomService random)
        {
            var balance = db.Balance;
            EventDef best = null;
            List<EventDef> randoms = null;

            // 갇혀 있는 동안: 그 행동을 조건으로 건 이벤트만 (무작위 이벤트 없음)
            var confined = !string.IsNullOrEmpty(context?.ActionId) && db.TryGetAction(context.ActionId, out var action) && action.Confined;
            foreach (var ev in db.Events)
            {
                if (confined && (ev.IsRandom || !ev.Condition.ActionIds.Contains(context.ActionId)))
                {
                    continue;
                }

                if (!IsAvailable(state, ev) || !ConditionEvaluator.IsMet(state, ev.Condition, context, balance))
                {
                    continue;
                }

                if (ev.IsRandom)
                {
                    (randoms ??= new List<EventDef>()).Add(ev);
                }
                else if (best == null || ev.Priority > best.Priority)
                {
                    best = ev;
                }
            }

            if (best != null)
            {
                return best;
            }

            if (randoms == null || !RandomQuotaLeft(state, context, balance))
            {
                return null;
            }

            if (random.NextDouble() >= balance.RandomEventChance)
            {
                return null;
            }

            return WeightedPick(randoms, random);
        }

        /// <summary>1회성·쿨다운 검사.</summary>
        public static bool IsAvailable(GameState state, EventDef ev)
        {
            if (!state.Events.Records.TryGetValue(ev.Id, out var record) || record.Count == 0)
            {
                return true;
            }

            if (ev.Once)
            {
                return false;
            }

            return state.Calendar.Week - record.LastWeek >= Math.Max(0, ev.CooldownWeeks);
        }

        public static bool RandomQuotaLeft(GameState state, EventContext context, BalanceConfig balance)
        {
            var log = state.Events;
            var week = state.Calendar.Week;
            var weekCount = log.RandomWeek == week ? log.RandomCountThisWeek : 0;
            if (weekCount >= balance.RandomEventsPerWeek)
            {
                return false;
            }

            return balance.RandomEventsPerDay > 0 && log.LastRandomDayKey != DayKey(week, context.Day);
        }

        internal static int DayKey(int week, int day) => week * 10 + day;

        private static EventDef WeightedPick(List<EventDef> events, IRandomService random)
        {
            var total = 0f;
            foreach (var ev in events)
            {
                total += Math.Max(0f, ev.Weight);
            }

            if (total <= 0f)
            {
                return null;
            }

            var roll = (float)random.NextDouble() * total;
            foreach (var ev in events)
            {
                roll -= Math.Max(0f, ev.Weight);
                if (roll < 0f)
                {
                    return ev;
                }
            }

            return events[events.Count - 1];
        }
    }

    /// <summary>선택지 하나의 표시 상태.</summary>
    public sealed class EventChoiceView
    {
        public EventChoiceDef Def;

        /// <summary>비용을 낼 수 있음 (false면 비활성으로 표시).</summary>
        public bool Enabled;

        /// <summary>판정이 있으면 성공 확률, 없으면 1.</summary>
        public float Chance = 1f;

        public string Id => Def?.Id ?? "";
    }

    public sealed class EventChoiceResult
    {
        public string EventId = "";
        public string ChoiceId = "";
        public CheckResult Result = CheckResult.Success;
        public string NextEventId = "";
    }

    /// <summary>이벤트 시작·선택 처리.</summary>
    public static class EventResolver
    {
        /// <summary>선택지가 없는 서술 이벤트의 "계속".</summary>
        public const string ContinueChoiceId = "";

        /// <summary>이벤트 시작: 기록 갱신 + OnShow 효과.</summary>
        public static void Begin(GameState state, EventDef ev, EventContext context, BalanceConfig balance, StateDelta delta)
        {
            var log = state.Events;
            if (!log.Records.TryGetValue(ev.Id, out var record))
            {
                record = new EventRecord();
                log.Records[ev.Id] = record;
            }

            record.Count++;
            record.LastWeek = state.Calendar.Week;
            if (ev.IsRandom)
            {
                var week = state.Calendar.Week;
                if (log.RandomWeek != week)
                {
                    log.RandomWeek = week;
                    log.RandomCountThisWeek = 0;
                }

                log.RandomCountThisWeek++;
                log.LastRandomDayKey = EventScheduler.DayKey(week, context.Day);
            }

            EffectApplier.Apply(state, ev.OnShow, balance, delta);
            if (ev.OnShow.RaidStrength > 0)
            {
                LastRaid = RaidRules.Resolve(state, ev.OnShow.RaidStrength, 0, true, balance, RaidRandom ?? new SeededRandom(state.Rng), delta);
            }
        }

        /// <summary>이벤트로 강제된 습격의 마지막 결과 (화면 표시용).</summary>
        public static RaidResult LastRaid { get; set; }

        /// <summary>테스트용 난수 주입 (null이면 게임 상태의 시드 난수).</summary>
        public static IRandomService RaidRandom { get; set; }

        /// <summary>보여줄 선택지 (조건이 안 맞는 선택지는 숨김).</summary>
        public static List<EventChoiceView> Choices(GameState state, EventDef ev, EventContext context, BalanceConfig balance)
        {
            var views = new List<EventChoiceView>();
            foreach (var choice in ev.Choices)
            {
                if (!ConditionEvaluator.IsMet(state, choice.Condition, context, balance))
                {
                    continue;
                }

                views.Add(new EventChoiceView
                {
                    Def = choice,
                    Enabled = CanAfford(state, choice),
                    Chance = choice.HasCheck ? CheckResolver.ComputeChance(state, choice.Check, balance) : 1f,
                });
            }

            return views;
        }

        public static bool CanAfford(GameState state, EventChoiceDef choice)
        {
            foreach (var cost in choice.Costs)
            {
                if (state.Base.GetResource(cost.Resource) < cost.Amount)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>선택 적용. 고를 수 없는 선택지면 예외.</summary>
        public static EventChoiceResult Choose(
            GameState state, EventDef ev, string choiceId, EventContext context, BalanceConfig balance, IRandomService random, StateDelta delta)
        {
            var result = new EventChoiceResult { EventId = ev.Id, ChoiceId = choiceId ?? "" };
            var views = Choices(state, ev, context, balance);
            if (views.Count == 0 || (string.IsNullOrEmpty(choiceId) && !views.Exists(v => v.Enabled)))
            {
                if (!string.IsNullOrEmpty(choiceId))
                {
                    throw new ArgumentException($"선택지가 없는 이벤트: '{ev.Id}'");
                }

                Record(state, ev, result);
                return result;
            }

            var view = views.Find(v => v.Id == choiceId);
            if (view == null || !view.Enabled)
            {
                throw new ArgumentException($"고를 수 없는 선택지: '{ev.Id}:{choiceId}'");
            }

            var choice = view.Def;
            foreach (var cost in choice.Costs)
            {
                EffectApplier.ChangeResource(state, cost.Resource, -cost.Amount, delta);
            }

            if (choice.HasCheck)
            {
                result.Result = CheckResolver.Roll(view.Chance, random, balance);
            }

            var effect = result.Result == CheckResult.Failure ? choice.OnFailure : choice.Effect;
            EffectApplier.Apply(state, effect, balance, delta);
            if (effect.RaidStrength > 0)
            {
                LastRaid = RaidRules.Resolve(state, effect.RaidStrength, 0, true, balance, random, delta);
            }
            result.NextEventId = choice.NextEventId ?? "";
            Record(state, ev, result);
            return result;
        }

        private static void Record(GameState state, EventDef ev, EventChoiceResult result)
        {
            if (state.Events.Records.TryGetValue(ev.Id, out var record))
            {
                record.LastChoice = result.ChoiceId;
            }

            if (!string.IsNullOrEmpty(result.ChoiceId))
            {
                state.Events.ChoiceLog.Add(ev.Id + ":" + result.ChoiceId);
            }
        }
    }

    /// <summary>화면 없이 이벤트를 진행할 때(테스트·봇·헤드리스) 선택지를 고른다.</summary>
    public interface IEventChooser
    {
        string Choose(GameState state, EventDef ev, IReadOnlyList<EventChoiceView> choices);
    }

    /// <summary>기본: 고를 수 있는 첫 선택지.</summary>
    public sealed class FirstChoiceChooser : IEventChooser
    {
        public string Choose(GameState state, EventDef ev, IReadOnlyList<EventChoiceView> choices)
        {
            foreach (var choice in choices)
            {
                if (choice.Enabled)
                {
                    return choice.Id;
                }
            }

            return EventResolver.ContinueChoiceId;
        }
    }
}
