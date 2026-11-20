using System;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>
    /// 요리: 요리 숙련에 맞는 가장 좋은 요리(대성공이면 한 단계 위)를 만들어 먹는다. 포만·수분이 0 위로도 올라 배부름이 된다.
    /// 재료(식량·물)가 모자라면 만들 수 있는 아래 단계로, 하나도 못 만들면 아무 일도 없다.
    /// </summary>
    public static class CookingRules
    {
        /// <summary>요리를 만들었으면 그 정의, 아니면 null. bonusTiers: 대성공이면 1.</summary>
        public static DishDef Cook(GameState state, BalanceConfig balance, int bonusTiers, StateDelta delta)
        {
            var dishes = balance.CookingDishes;
            if (dishes == null || dishes.Length == 0)
            {
                return null;
            }

            // 숙련으로 만들 수 있는 가장 높은 단계 (+대성공 보너스)
            var level = state.Player.ProficiencyLevel(ProficiencyType.Cooking);
            var tier = -1;
            for (var i = 0; i < dishes.Length; i++)
            {
                if (dishes[i].MinCookingLevel <= level)
                {
                    tier = i;
                }
            }

            tier = Math.Min(dishes.Length - 1, tier + Math.Max(0, bonusTiers));
            for (var i = tier; i >= 0; i--)
            {
                var dish = dishes[i];
                if (state.Base.GetResource(ResourceType.Food) < dish.FoodCost || state.Base.GetResource(ResourceType.Water) < dish.WaterCost)
                {
                    continue;
                }

                EffectApplier.ChangeResource(state, ResourceType.Food, -dish.FoodCost, delta);
                EffectApplier.ChangeResource(state, ResourceType.Water, -dish.WaterCost, delta);
                EffectApplier.ChangeVital(state, VitalType.Satiety, dish.Satiety, balance, delta);
                EffectApplier.ChangeVital(state, VitalType.Hydration, dish.Hydration, balance, delta);
                if (dish.Stress != 0)
                {
                    EffectApplier.ChangeVital(state, VitalType.Stress, -dish.Stress, balance, delta);
                }

                // 좋은 요리는 능력치도 조금
                foreach (var stat in dish.Stats)
                {
                    EffectApplier.ChangeStat(state, stat.Stat, stat.Amount, balance, delta);
                }

                delta.DishesCooked.Add(dish.Id);
                if (!string.IsNullOrEmpty(balance.FirstDishFlag) && state.Flags.Add(balance.FirstDishFlag))
                {
                    delta.FlagsSet.Add(balance.FirstDishFlag);
                }

                return dish;
            }

            return null;
        }
    }
}
