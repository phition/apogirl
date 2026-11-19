using System;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Events;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Planning;
using ApoGirl.Core.Random;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Simulation.Bots
{
    /// <summary>전략의 주간 틀과 규칙으로 한 주 계획을 채운다.</summary>
    public static class BotPlanner
    {
        public const string RestDay = "ACT_REST_DAY";
        public const string RestNight = "ACT_REST_NIGHT";

        /// <summary>잠기지 않은 칸을 모두 다시 채운다: 틀 → 전략 규칙 → 공통 규칙 순.</summary>
        public static void Plan(GameState state, IDefinitionDatabase db, BotConfigDef config, BotStrategyDef strategy, IRandomService random)
        {
            var slots = state.Plan.Slots;
            for (var i = 0; i < slots.Count; i++)
            {
                if (slots[i].Locked)
                {
                    continue;
                }

                var day = i % 2 == 0;
                var template = day ? strategy.Day : strategy.Night;
                var dayIndex = i / 2;
                var candidates = dayIndex < template.Count ? template[dayIndex] : "";
                slots[i].ActionId = Resolve(state, db, config, candidates, i, random) ?? (day ? RestDay : RestNight);
            }

            var context = EventContext.FromCalendar(state);
            foreach (var rule in strategy.Rules.Concat(config.CommonRules))
            {
                if (rule.When != null && !ConditionEvaluator.IsMet(state, rule.When, context, db.Balance))
                {
                    continue;
                }

                foreach (var slotKey in rule.Slots)
                {
                    var index = SlotIndex(slotKey);
                    if (index < 0 || index >= slots.Count || slots[index].Locked)
                    {
                        continue;
                    }

                    var id = Resolve(state, db, config, rule.Use, index, random);
                    if (id != null)
                    {
                        slots[index].ActionId = id;
                    }
                }
            }
        }

        /// <summary>"D1"~"D7" → 0,2,…,12 / "N1"~"N7" → 1,3,…,13. 잘못되면 -1.</summary>
        public static int SlotIndex(string key)
        {
            if (string.IsNullOrEmpty(key) || key.Length < 2 || !int.TryParse(key.Substring(1), out var dayNumber)
                || dayNumber < 1 || dayNumber > CalendarState.DaysPerWeek)
            {
                return -1;
            }

            switch (char.ToUpperInvariant(key[0]))
            {
                case 'D':
                    return (dayNumber - 1) * 2;
                case 'N':
                    return (dayNumber - 1) * 2 + 1;
                default:
                    return -1;
            }
        }

        /// <summary>"후보|후보|…"에서 이 칸에 쓸 수 있는 첫 행동 ID (없으면 null).</summary>
        public static string Resolve(GameState state, IDefinitionDatabase db, BotConfigDef config, string candidates, int slotIndex, IRandomService random)
        {
            foreach (var token in Split(candidates))
            {
                if (config.Groups.TryGetValue(token, out var group))
                {
                    var usable = new List<(string Id, float Weight)>();
                    for (var i = 0; i < group.Actions.Count; i++)
                    {
                        if (Usable(state, db, group.Actions[i], slotIndex))
                        {
                            usable.Add((group.Actions[i], i < group.Weights.Count ? group.Weights[i] : 1f));
                        }
                    }

                    if (usable.Count == 0)
                    {
                        continue;
                    }

                    return group.Pick == BotPickMode.First ? usable[0].Id : usable[WeightedIndex(usable.Select(u => u.Weight).ToList(), random)].Id;
                }

                if (Usable(state, db, token, slotIndex))
                {
                    return token;
                }
            }

            return null;
        }

        public static IEnumerable<string> Split(string candidates)
        {
            return (candidates ?? "").Split('|').Select(t => t.Trim()).Where(t => t.Length > 0);
        }

        private static bool Usable(GameState state, IDefinitionDatabase db, string actionId, int slotIndex)
        {
            return db.TryGetAction(actionId, out var action) && PlanEditing.IsUnlocked(state, db, action) && PlanEditing.IsAllowedIn(action, slotIndex);
        }

        /// <summary>가중치 목록에서 하나 (모두 0 이하면 0번).</summary>
        public static int WeightedIndex(IReadOnlyList<float> weights, IRandomService random)
        {
            var total = weights.Sum(w => Math.Max(0f, w));
            if (total <= 0f)
            {
                return 0;
            }

            var roll = random.NextDouble() * total;
            for (var i = 0; i < weights.Count; i++)
            {
                roll -= Math.Max(0f, weights[i]);
                if (roll < 0)
                {
                    return i;
                }
            }

            return weights.Count - 1;
        }

        /// <summary>설정 점검: 모르는 행동·묶음, 잘못된 칸 이름, 칸 수. 문제 문장 목록을 돌려준다.</summary>
        public static List<string> Validate(BotConfigDef config, IDefinitionDatabase db)
        {
            var errors = new List<string>();

            void CheckCandidates(string where, string candidates)
            {
                foreach (var token in Split(candidates))
                {
                    if (!config.Groups.ContainsKey(token) && !db.TryGetAction(token, out _))
                    {
                        errors.Add($"{where}: 모르는 행동·묶음 '{token}'");
                    }
                }
            }

            void CheckRules(string where, IEnumerable<BotRuleDef> rules)
            {
                foreach (var rule in rules)
                {
                    foreach (var slot in rule.Slots.Where(s => SlotIndex(s) < 0))
                    {
                        errors.Add($"{where}: 잘못된 칸 '{slot}' ({rule.Note})");
                    }

                    CheckCandidates($"{where} ({rule.Note})", rule.Use);
                }
            }

            foreach (var pair in config.Groups)
            {
                foreach (var id in pair.Value.Actions.Where(id => !db.TryGetAction(id, out _)))
                {
                    errors.Add($"묶음 {pair.Key}: 모르는 행동 '{id}'");
                }
            }

            CheckRules("공통 규칙", config.CommonRules);
            if (config.Strategies.Select(s => s.Id).Distinct().Count() != config.Strategies.Count)
            {
                errors.Add("전략 ID 중복");
            }

            foreach (var strategy in config.Strategies)
            {
                if (strategy.Day.Count != CalendarState.DaysPerWeek || strategy.Night.Count != CalendarState.DaysPerWeek)
                {
                    errors.Add($"{strategy.Id}: 낮·밤 칸은 각각 {CalendarState.DaysPerWeek}개여야 함");
                }

                for (var i = 0; i < strategy.Day.Count; i++) CheckCandidates($"{strategy.Id} D{i + 1}", strategy.Day[i]);
                for (var i = 0; i < strategy.Night.Count; i++) CheckCandidates($"{strategy.Id} N{i + 1}", strategy.Night[i]);
                CheckRules(strategy.Id, strategy.Rules);
            }

            return errors;
        }
    }

    /// <summary>이벤트 선택: 고를 수 있는 선택지 중 전략 가중치로 무작위.</summary>
    public sealed class BotEventChooser : IEventChooser
    {
        private readonly IReadOnlyList<BotChoiceBiasDef> _bias;
        private readonly IRandomService _random;

        public BotEventChooser(IEnumerable<BotChoiceBiasDef> bias, IRandomService random)
        {
            _bias = bias.ToList();
            _random = random;
        }

        public string Choose(GameState state, EventDef ev, IReadOnlyList<EventChoiceView> choices)
        {
            var enabled = choices.Where(c => c.Enabled).ToList();
            if (enabled.Count == 0)
            {
                return EventResolver.ContinueChoiceId;
            }

            var weights = enabled.Select(c => Weight(c.Id)).ToList();
            return enabled[BotPlanner.WeightedIndex(weights, _random)].Id;
        }

        public float Weight(string choiceId)
        {
            var weight = 1f;
            foreach (var bias in _bias)
            {
                if (bias.Ids.Contains(choiceId))
                {
                    weight *= bias.Weight;
                }
            }

            return weight;
        }
    }

    /// <summary>조우 선택: 첫 선택지에 가중치를 준 무작위.</summary>
    public sealed class BotEncounterChooser : IEncounterChooser
    {
        private readonly float _firstWeight;

        public BotEncounterChooser(float firstWeight)
        {
            _firstWeight = firstWeight;
        }

        public int Choose(EncounterDef encounter, ExpeditionRequest request, IRandomService random)
        {
            var weights = encounter.Choices.Select((c, i) => i == 0 ? _firstWeight : 1f).ToList();
            return BotPlanner.WeightedIndex(weights, random);
        }
    }
}
