using System;
using System.Collections.Generic;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Planning;
using ApoGirl.Core.Random;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Simulation
{
    /// <summary>새 게임 상태 생성. 0막 프롤로그(M3/M4)는 profile·분배값을 만들어 여기에 넘긴다.</summary>
    public static class NewGameFactory
    {
        /// <param name="statAllocation">기본값에 더할 분배 (문서 7.1: 약 +45, 음수는 교환형 선택).</param>
        public static GameState Create(
            BalanceConfig balance, int seed, ProfileState profile = null, IEnumerable<StatAmount> statAllocation = null)
        {
            var state = new GameState { Rng = SeededRandom.CreateState(seed) };
            if (profile != null)
            {
                state.Player.Profile = profile;
            }

            foreach (StatType stat in Enum.GetValues(typeof(StatType)))
            {
                state.Player.SetStat(stat, balance.BaseStatValue);
            }

            if (statAllocation != null)
            {
                foreach (var allocation in statAllocation)
                {
                    var value = state.Player.GetStat(allocation.Stat) + allocation.Amount;
                    state.Player.SetStat(allocation.Stat, Math.Min(Math.Max(value, 0), balance.StatMax));
                }
            }

            foreach (ProficiencyType proficiency in Enum.GetValues(typeof(ProficiencyType)))
            {
                state.Player.Proficiency[proficiency] = new ProficiencyState();
            }

            state.Player.Vitals.Hp = balance.StartingHp;
            state.Player.Vitals.Cleanliness = balance.StartingCleanliness;

            foreach (ResourceType resource in Enum.GetValues(typeof(ResourceType)))
            {
                state.Base.SetResource(resource, 0);
            }

            foreach (var resource in balance.InitialResources)
            {
                state.Base.SetResource(resource.Resource, resource.Amount);
            }

            state.Plan = PlanPreparer.CreateWeekPlan(state, balance);
            return state;
        }
    }
}
