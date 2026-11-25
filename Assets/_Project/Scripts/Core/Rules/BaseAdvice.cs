using System;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Events;
using ApoGirl.Core.Planning;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>
    /// 거점 상황판 "다음 할 일" (BASE_TAB_PROPOSAL 6절·P4): base_advice.json 줄 가운데 지금 맞는 것을 우선순위대로 고른다.
    /// 읽기만 한다 (상태·난수를 바꾸지 않는다).
    /// </summary>
    public static class BaseAdvice
    {
        /// <summary>상황판 예상에서 센 값. raid가 null이면(안전 칸이 아직 안 보임) 습격 값은 0.</summary>
        public static Dictionary<BaseFact, int> Facts(GameState state, IDefinitionDatabase db, RaidForecast raid, SupplyForecast food, SupplyForecast water)
        {
            var balance = db.Balance;
            var facts = new Dictionary<BaseFact, int>();
            var showRaid = raid != null && !raid.NotYet;
            facts[BaseFact.ThreatGap] = state.Base.Threat - state.Base.Defense - SpecialRules.DefenseBonus(state, balance);
            facts[BaseFact.RaidLevel] = showRaid ? (int)raid.Level : 0;
            facts[BaseFact.RepelRisk] = showRaid ? (int)raid.Repel : 0;
            facts[BaseFact.HasAlarm] = state.Flags.Contains(DefenseRules.FlagAlarm) ? 1 : 0;
            facts[BaseFact.FoodShortDay] = food?.ShortDay ?? 0;
            facts[BaseFact.WaterShortDay] = water?.ShortDay ?? 0;
            facts[BaseFact.FoodSpoil] = food?.Spoil ?? 0;
            facts[BaseFact.FoodTight] = food != null && food.End < food.NextWeekNeed ? 1 : 0;
            facts[BaseFact.WaterTight] = water != null && water.End < water.NextWeekNeed ? 1 : 0;
            facts[BaseFact.RawWater] = state.Base.GetResource(ResourceType.RawWater);
            facts[BaseFact.CanUpgradePower] = PowerRules.CanUpgrade(state, balance) ? 1 : 0;
            facts[BaseFact.ScarecrowDown] = BaseForecast.ScarecrowDown(state, balance) ? 1 : 0;

            var companions = state.Npcs.Where(n => n.Value.Status == NpcStatus.Joined && !CatRules.IsCat(n.Key)).ToList();
            facts[BaseFact.IdleCompanions] = companions.Count(n => string.IsNullOrEmpty(n.Value.Role));
            facts[BaseFact.FreeRoleSlots] = balance.RoleCapacity <= 0
                ? 0
                : db.Extras.Roles.Where(r => RoleRules.IsAvailable(state, r))
                    .Sum(r => Math.Max(0, balance.RoleCapacity - RoleRules.Holders(state, r.Id).Count));
            return facts;
        }

        /// <summary>지금 맞는 줄을 우선순위가 높은 것부터 max개.</summary>
        public static List<AdviceDef> Pick(GameState state, IDefinitionDatabase db, RaidForecast raid, SupplyForecast food, SupplyForecast water, int max)
        {
            var list = db.Extras.Advice;
            if (list == null || list.Count == 0 || max <= 0)
            {
                return new List<AdviceDef>();
            }

            var facts = Facts(state, db, raid, food, water);
            var context = EventContext.FromCalendar(state);
            return list
                .Select((advice, index) => (advice, index))
                .Where(x => Fits(state, db, x.advice, facts, context))
                .OrderByDescending(x => x.advice.Priority)
                .ThenBy(x => x.index)
                .Take(max)
                .Select(x => x.advice)
                .ToList();
        }

        private static bool Fits(GameState state, IDefinitionDatabase db, AdviceDef advice, Dictionary<BaseFact, int> facts, EventContext context)
        {
            foreach (var check in advice.Facts)
            {
                if (!facts.TryGetValue(check.Fact, out var value) || !ConditionEvaluator.Compare(value, check.Op, check.Value))
                {
                    return false;
                }
            }

            if (advice.Condition != null)
            {
                try
                {
                    if (!ConditionEvaluator.IsMet(state, advice.Condition, context, db.Balance))
                    {
                        return false;
                    }
                }
                catch (ArgumentException)
                {
                    return false;
                }
            }

            if (string.IsNullOrEmpty(advice.ActionId))
            {
                return true;
            }

            // 고를 수 없는 행동은 권하지 않고, 이미 계획에 넣었으면 다시 말하지 않는다
            return db.TryGetAction(advice.ActionId, out var action)
                   && PlanEditing.IsUnlocked(state, db, action)
                   && state.Plan.Slots.All(s => s.ActionId != advice.ActionId);
        }
    }
}
