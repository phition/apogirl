using System;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Random;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>거점 방어 작업의 효과: 함정(소모), 깡통 경보, 출입구 보강(방어 상한), 창문 가림막(위협 증가 감소).</summary>
    public static class DefenseRules
    {
        public const string FlagAlarm = "ALARM_INSTALLED";
        public const string FlagReinforced = "ENTRANCE_REINFORCED";
        public const string FlagWindowCovers = "WINDOW_COVERS";

        /// <summary>방어 상한 (출입구 보강 뒤 상향).</summary>
        public static int Max(GameState state, BalanceConfig balance)
        {
            return state.Flags.Contains(FlagReinforced) ? Math.Max(balance.DefenseMax, balance.ReinforcedDefenseMax) : balance.DefenseMax;
        }

        /// <summary>습격·침입 때 더하는 방어력: 경보 + 함정 하나 (쓰면 사라진다).</summary>
        public static int ExtraPower(GameState state, BalanceConfig balance, bool consumeTrap)
        {
            var power = state.Flags.Contains(FlagAlarm) ? balance.AlarmPower : 0;
            if (consumeTrap && state.Base.Traps > 0)
            {
                state.Base.Traps--;
                power += balance.TrapPower;
            }

            return power;
        }

        /// <summary>거점 방어력 (습격과 같은 계산 + 경보·함정).</summary>
        public static int Power(GameState state, BalanceConfig balance, int guard, IRandomService random, bool consumeTrap)
        {
            return balance.RaidBasePower + DerivedStats.CombatPower(state, balance)
                   + (int)Math.Round(state.Base.Defense * balance.RaidDefenseWeight)
                   + guard
                   + ExtraPower(state, balance, consumeTrap)
                   + random.Range(0, Math.Max(0, balance.RaidPowerVariance));
        }
    }

    /// <summary>낮 침입 한 번의 결과.</summary>
    public sealed class IntrusionResult
    {
        /// <summary>Fight / Hide / Yield.</summary>
        public string Response = "";

        /// <summary>맞서서 막았거나 숨어서 넘겼다.</summary>
        public bool Held;

        public int Strength;
        public int Power;
        public readonly Dictionary<ResourceType, int> Lost = new Dictionary<ResourceType, int>();

        /// <summary>다친 동료 (없으면 빈 문자열).</summary>
        public string InjuredNpcId = "";
    }

    /// <summary>
    /// 낮 침입 (깡패, 3막부터): 낮 슬롯마다 확률로 일어나 그날 낮 행동이 취소되고 침입 이벤트로 대응한다.
    /// 대비가 부족하면 식량·물·약을 빼앗기고 학교에 있던 동료가 다친다.
    /// </summary>
    public static class IntrusionRules
    {
        public const string Fight = "Fight";
        public const string Hide = "Hide";
        public const string Yield = "Yield";

        public static float WeeklyChance(int week, BalanceConfig balance)
        {
            if (week < balance.IntrusionStartWeek)
            {
                return 0f;
            }

            return week >= 40 ? balance.IntrusionWeeklyChanceAct4 : balance.IntrusionWeeklyChanceAct3;
        }

        /// <summary>낮 슬롯 하나의 확률 (주 확률을 7일로 나눈다). 확률 0이면 난수를 쓰지 않는다.</summary>
        public static bool Roll(GameState state, BalanceConfig balance, IRandomService random)
        {
            var weekly = WeeklyChance(state.Calendar.Week, balance);
            if (weekly <= 0f)
            {
                return false;
            }

            if (weekly >= 1f)
            {
                return true; // 시험용: 매일
            }

            var daily = 1.0 - Math.Pow(1.0 - weekly, 1.0 / CalendarState.DaysPerWeek);
            return random.NextDouble() < daily;
        }

        public static IntrusionResult Resolve(GameState state, IDefinitionDatabase db, string response, BalanceConfig balance, IRandomService random, StateDelta delta)
        {
            var result = new IntrusionResult
            {
                Response = response ?? "",
                Strength = balance.IntrusionStrength + (state.Calendar.Week >= 40 ? balance.IntrusionStrengthAct4Bonus : 0)
                           + random.Range(0, Math.Max(0, balance.IntrusionStrengthVariance)),
            };

            var guard = (int)Math.Round(TraitRules.Sum(state, db, TraitModifierKind.RaidGuard));
            state.Exploration.Intrusions++;
            switch (result.Response)
            {
                case Fight:
                    result.Power = DefenseRules.Power(state, balance, guard, random, true);
                    result.Held = result.Power >= result.Strength;
                    if (result.Held)
                    {
                        EffectApplier.ChangeVital(state, VitalType.Stress, balance.IntrusionWinStress, balance, delta);
                        EffectApplier.ChangeDefense(state, -balance.RaidRepelDefenseLoss, balance, delta);
                    }
                    else
                    {
                        Overrun(state, result, balance, random, delta);
                    }

                    break;

                case Hide:
                    var chance = balance.IntrusionHideBaseChance + state.Base.Defense * balance.IntrusionHidePerDefense
                                 + (state.Flags.Contains(DefenseRules.FlagAlarm) ? balance.IntrusionHideAlarmBonus : 0f);
                    result.Held = random.NextDouble() < chance;
                    if (result.Held)
                    {
                        // 숨어서 넘겼지만 그들이 뒤지고 갔다
                        Take(state, result, balance.IntrusionLossHidden, delta);
                        EffectApplier.ChangeVital(state, VitalType.Stress, balance.IntrusionHideStress, balance, delta);
                    }
                    else
                    {
                        Overrun(state, result, balance, random, delta);
                    }

                    break;

                default:
                    result.Response = Yield;
                    Take(state, result, balance.IntrusionLossYield, delta);
                    EffectApplier.ChangeVital(state, VitalType.Stress, balance.IntrusionYieldStress, balance, delta);
                    break;
            }

            if (!result.Held && result.Response != Yield)
            {
                state.Exploration.IntrusionsLost++;
            }

            return result;
        }

        /// <summary>대비 부족: 크게 빼앗기고, 주인공이 다치고, 동료 한 명이 다친다.</summary>
        private static void Overrun(GameState state, IntrusionResult result, BalanceConfig balance, IRandomService random, StateDelta delta)
        {
            Take(state, result, balance.IntrusionLossLost, delta);
            EffectApplier.ChangeVital(state, VitalType.Hp, -balance.IntrusionLoseHp, balance, delta);
            EffectApplier.ChangeVital(state, VitalType.Stress, balance.IntrusionLoseStress, balance, delta);
            EffectApplier.ChangeDefense(state, -balance.RaidBreachDefenseLoss, balance, delta);

            var companions = state.Npcs
                .Where(n => n.Key != EffectApplier.TeacherNpcId && n.Value.Status == NpcStatus.Joined && n.Value.InjuredWeeks <= 0)
                .Select(n => n.Key)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToList();
            if (companions.Count > 0)
            {
                var id = companions[random.Range(0, companions.Count - 1)];
                state.Npcs[id].InjuredWeeks = balance.IntrusionInjuryWeeks;
                result.InjuredNpcId = id;
            }
        }

        private static void Take(GameState state, IntrusionResult result, float fraction, StateDelta delta)
        {
            foreach (var type in new[] { ResourceType.Food, ResourceType.Water, ResourceType.Medicine })
            {
                var have = state.Base.GetResource(type);
                var amount = (int)Math.Ceiling(have * fraction - 1e-6);
                if (amount > 0)
                {
                    EffectApplier.ChangeResource(state, type, -amount, delta);
                    result.Lost[type] = amount;
                }
            }
        }

        /// <summary>주간 정산: 다친 동료의 남은 주 수를 줄인다.</summary>
        public static void HealWeekly(GameState state)
        {
            foreach (var npc in state.Npcs.Values)
            {
                if (npc.InjuredWeeks > 0)
                {
                    npc.InjuredWeeks--;
                }
            }
        }
    }
}
