using System;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Effects
{
    /// <summary>
    /// 모든 상태 변경의 단일 진입점. 범위 제한(Clamp)을 보장하고 실제 변화량을 <see cref="StateDelta"/>에 기록한다.
    /// </summary>
    public static class EffectApplier
    {
        /// <param name="recoveryMultiplier">피로·스트레스 감소(회복)량에 곱하는 배율. 새벽 귀환 후 휴식 등.</param>
        public static void Apply(GameState state, GameEffect effect, BalanceConfig balance, StateDelta delta, float recoveryMultiplier = 1f)
        {
            if (effect == null)
            {
                return;
            }

            foreach (var stat in effect.Stats)
            {
                ChangeStat(state, stat.Stat, stat.Amount, balance, delta);
            }

            // 의약품으로 부상 치유 (그 뒤의 체력 회복이 막히지 않게 먼저)
            if (effect.TreatInjury)
            {
                Rules.InjuryRules.TreatWithMedicine(state, balance, delta);
            }

            // 플래그 해제를 먼저: 부상 치료(INJURED 해제)와 같은 효과의 체력 회복이 막히지 않게
            foreach (var flag in effect.ClearFlags)
            {
                if (!string.IsNullOrEmpty(flag) && state.Flags.Remove(flag))
                {
                    delta.FlagsCleared.Add(flag);
                }
            }

            foreach (var vital in effect.Vitals)
            {
                var amount = vital.Amount;
                if (amount < 0 && (vital.Vital == VitalType.Fatigue || vital.Vital == VitalType.Stress))
                {
                    amount = (int)Math.Round(amount * recoveryMultiplier, MidpointRounding.AwayFromZero);
                }

                ChangeVital(state, vital.Vital, amount, balance, delta);
            }

            foreach (var resource in effect.Resources)
            {
                ChangeResource(state, resource.Resource, resource.Amount, delta);
            }

            foreach (var hidden in effect.Hidden)
            {
                ChangeHidden(state, hidden.Hidden, hidden.Amount, balance, delta);
            }

            ChangeSleepDebt(state, effect.SleepDebt, balance, delta);
            ChangeConscience(state, effect.Conscience, balance, delta);

            foreach (var flag in effect.SetFlags)
            {
                if (!string.IsNullOrEmpty(flag) && state.Flags.Add(flag))
                {
                    delta.FlagsSet.Add(flag);
                }
            }

            foreach (var clue in effect.AddClues)
            {
                if (!string.IsNullOrEmpty(clue) && state.Clues.Add(clue))
                {
                    delta.CluesAdded.Add(clue);
                }
            }

            foreach (var tag in effect.EndingTags)
            {
                if (string.IsNullOrEmpty(tag.Tag))
                {
                    continue;
                }

                state.EndingTags.TryGetValue(tag.Tag, out var current);
                state.EndingTags[tag.Tag] = current + tag.Amount;
            }

            foreach (var change in effect.Npcs)
            {
                ChangeNpc(state, change, balance, delta);
            }

            foreach (var item in effect.Items)
            {
                AddItem(state, item.ItemId, item.Count, delta);
            }

            // ── M4 ──
            ChangeDefense(state, effect.Defense, balance, delta);
            ChangeThreat(state, effect.Threat, balance, delta);
            foreach (var facility in effect.Facilities)
            {
                if (string.IsNullOrEmpty(facility.FacilityId))
                {
                    continue;
                }

                state.Base.Facilities.TryGetValue(facility.FacilityId, out var level);
                if (facility.Level > level)
                {
                    state.Base.Facilities[facility.FacilityId] = facility.Level;
                    delta.FacilitiesOpened.Add(facility.FacilityId);
                }
            }

            foreach (var trait in effect.AddTraits)
            {
                if (!string.IsNullOrEmpty(trait) && !state.Player.Traits.Contains(trait))
                {
                    state.Player.Traits.Add(trait);
                }
            }

            foreach (var set in effect.Proficiencies)
            {
                var proficiency = state.Player.GetProficiency(set.Proficiency);
                if (proficiency.Level < set.Level)
                {
                    proficiency.Level = Math.Min(set.Level, balance.ProficiencyMaxLevel);
                    var thresholds = balance.ProficiencyXpThresholds;
                    var need = proficiency.Level - 1 < thresholds.Length ? thresholds[proficiency.Level - 1] : proficiency.Xp;
                    proficiency.Xp = Math.Max(proficiency.Xp, need);
                }
            }

            if (effect.TeacherMobility > state.Teacher.MobilityStage)
            {
                state.Teacher.MobilityStage = effect.TeacherMobility;
            }

            // 요리: 숙련에 맞는 요리를 만들어 먹는다 (재료 소모)
            if (effect.CookDish > 0)
            {
                Rules.CookingRules.Cook(state, balance, effect.CookDish - 1, delta);
            }

            // 정수: 빗물을 식수로 (비율만큼만 남는다)
            if (effect.PurifyWater > 0)
            {
                var raw = Math.Min(effect.PurifyWater, state.Base.GetResource(ResourceType.RawWater));
                if (raw > 0)
                {
                    ChangeResource(state, ResourceType.RawWater, -raw, delta);
                    ChangeResource(state, ResourceType.Water, (int)Math.Round(raw * effect.PurifyRate, MidpointRounding.AwayFromZero), delta);
                }
            }

            if (effect.RestorePower && !state.Base.PowerRestored)
            {
                state.Base.PowerRestored = true;
                delta.PowerRestored = true;
            }

            // M6: 선생님 재감염 (7.2)
            if (effect.TeacherReinfect && !state.Teacher.Reinfected)
            {
                state.Teacher.Reinfected = true;
                state.Flags.Add(ApoGirl.Core.Rules.InfectionRules.FlagTeacherReinfected);
                delta.FlagsSet.Add(ApoGirl.Core.Rules.InfectionRules.FlagTeacherReinfected);
            }

            if (effect.TeacherInfection != 0)
            {
                ApoGirl.Core.Rules.InfectionRules.ChangeTeacherInfection(state, effect.TeacherInfection, balance);
            }

            if (effect.TraumaCare > 0)
            {
                ApoGirl.Core.Rules.AssaultTraumaRules.Care(state, effect.TraumaCare, balance);
            }

            if (effect.Traps != 0)
            {
                state.Base.Traps = Clamp(state.Base.Traps + effect.Traps, 0, balance.MaxTraps);
            }
        }

        public static int ChangeDefense(GameState state, int amount, BalanceConfig balance, StateDelta delta)
        {
            var before = state.Base.Defense;
            state.Base.Defense = Clamp(before + amount, 0, ApoGirl.Core.Rules.DefenseRules.Max(state, balance));
            delta.Defense += state.Base.Defense - before;
            return state.Base.Defense - before;
        }

        public static int ChangeThreat(GameState state, int amount, BalanceConfig balance, StateDelta delta)
        {
            var before = state.Base.Threat;
            if (amount > 0 && state.Flags.Contains(ApoGirl.Core.Rules.DefenseRules.FlagWindowCovers))
            {
                amount = Math.Max(1, (int)Math.Round(amount * balance.CoveredThreatMultiplier));
            }

            state.Base.Threat = Clamp(before + amount, 0, balance.ThreatMax);
            delta.Threat += state.Base.Threat - before;
            return state.Base.Threat - before;
        }

        /// <summary>윤 선생님 ID. 호감 변화는 Teacher.Affinity에 반영한다.</summary>
        public const string TeacherNpcId = "NPC_TEACHER";

        /// <summary>NPC 상태를 가져온다. 없으면 만든다 (Unknown).</summary>
        public static NpcState GetNpc(GameState state, string npcId)
        {
            if (!state.Npcs.TryGetValue(npcId, out var npc))
            {
                npc = new NpcState();
                state.Npcs[npcId] = npc;
            }

            return npc;
        }

        public static void ChangeNpc(GameState state, NpcChange change, BalanceConfig balance, StateDelta delta)
        {
            if (change == null || string.IsNullOrEmpty(change.NpcId))
            {
                return;
            }

            if (change.NpcId == TeacherNpcId)
            {
                var before = state.Teacher.Affinity;
                state.Teacher.Affinity = Clamp(before + change.Affinity, balance.NpcAffinityMin, balance.NpcAffinityMax);
                delta.AddNpcAffinity(change.NpcId, state.Teacher.Affinity - before);
                return;
            }

            var npc = GetNpc(state, change.NpcId);
            var affinityBefore = npc.Affinity;
            npc.Affinity = Clamp(npc.Affinity + change.Affinity, balance.NpcAffinityMin, balance.NpcAffinityMax);
            npc.Trust = Clamp(npc.Trust + change.Trust, balance.NpcAffinityMin, balance.NpcAffinityMax);
            delta.AddNpcAffinity(change.NpcId, npc.Affinity - affinityBefore);
            if (change.SetStatus != NpcStatus.Unknown && npc.Status != change.SetStatus)
            {
                npc.Status = change.SetStatus;
                delta.NpcStatusChanges.Add(change.NpcId + ":" + change.SetStatus);
            }

            if (!string.IsNullOrEmpty(change.SetRole))
            {
                npc.Role = change.SetRole;
            }
        }

        public static void AddItem(GameState state, string itemId, int count, StateDelta delta)
        {
            if (string.IsNullOrEmpty(itemId) || count <= 0)
            {
                return;
            }

            state.Storage.Add(new ItemStack { ItemId = itemId, Count = count });
            delta.ItemsGained.Add(new ItemStack { ItemId = itemId, Count = count });
        }

        public static int ChangeStat(GameState state, StatType type, int amount, BalanceConfig balance, StateDelta delta)
        {
            var before = state.Player.GetStat(type);
            var after = Clamp(before + amount, 0, balance.StatMax);
            state.Player.SetStat(type, after);
            delta.AddStat(type, after - before);
            return after - before;
        }

        public static int ChangeVital(GameState state, VitalType type, int amount, BalanceConfig balance, StateDelta delta)
        {
            // 부상 중에는 체력이 회복되지 않는다 (휴식·음식·이벤트 모두)
            if (type == VitalType.Hp && amount > 0 && Rules.InjuryRules.IsInjured(state, balance))
            {
                return 0;
            }

            var vitals = state.Player.Vitals;
            var before = vitals.GetVital(type);
            // 포만·수분은 0이 가운데 (−최대 ~ +최대)
            var min = type == VitalType.Satiety || type == VitalType.Hydration ? -balance.VitalMax : 0;
            var after = Clamp(before + amount, min, balance.VitalMax);
            vitals.SetVital(type, after);
            delta.AddVital(type, after - before);
            return after - before;
        }

        /// <summary>자원은 0 미만으로 내려가지 않는다. 보관량 상한은 창고 레벨 도입 시 추가.</summary>
        public static int ChangeResource(GameState state, ResourceType type, int amount, StateDelta delta)
        {
            var before = state.Base.GetResource(type);
            var after = Math.Max(0, before + amount);
            state.Base.SetResource(type, after);
            delta.AddResource(type, after - before);
            return after - before;
        }

        public static int ChangeHidden(GameState state, HiddenType type, int amount, BalanceConfig balance, StateDelta delta)
        {
            var hidden = state.Player.Hidden;
            var before = hidden.GetHidden(type);
            var after = Clamp(before + amount, 0, balance.HiddenMax);
            hidden.SetHidden(type, after);
            delta.AddHidden(type, after - before);
            return after - before;
        }

        public static int ChangeSleepDebt(GameState state, int amount, BalanceConfig balance, StateDelta delta)
        {
            var before = state.Player.SleepDebt;
            var after = Clamp(before + amount, 0, balance.SleepDebtMax);
            state.Player.SleepDebt = after;
            delta.SleepDebt += after - before;
            return after - before;
        }

        public static int ChangeConscience(GameState state, int amount, BalanceConfig balance, StateDelta delta)
        {
            var before = state.Player.Conscience;
            var after = Clamp(before + amount, 0, balance.ConscienceMax);
            state.Player.Conscience = after;
            delta.Conscience += after - before;
            return after - before;
        }

        private static int Clamp(int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }
}
