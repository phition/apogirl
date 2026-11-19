using System.Collections.Generic;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Effects
{
    /// <summary>실제로 적용된 변화량 기록 (범위 제한 후 값). 결과 연출·주간 요약 카드용.</summary>
    public sealed class StateDelta
    {
        public readonly Dictionary<StatType, int> Stats = new Dictionary<StatType, int>();
        public readonly Dictionary<VitalType, int> Vitals = new Dictionary<VitalType, int>();
        public readonly Dictionary<ResourceType, int> Resources = new Dictionary<ResourceType, int>();
        public readonly Dictionary<HiddenType, int> Hidden = new Dictionary<HiddenType, int>();
        public readonly List<ProficiencyType> ProficiencyLevelUps = new List<ProficiencyType>();
        public readonly List<string> FlagsSet = new List<string>();
        public readonly List<string> FlagsCleared = new List<string>();
        public readonly List<string> CluesAdded = new List<string>();
        public readonly Dictionary<string, int> NpcAffinity = new Dictionary<string, int>();

        /// <summary>"NPC_ID:Status" 형식.</summary>
        public readonly List<string> NpcStatusChanges = new List<string>();
        public readonly List<ItemStack> ItemsGained = new List<ItemStack>();
        public int SleepDebt;
        public int Conscience;

        public void AddStat(StatType type, int amount) => Add(Stats, type, amount);

        public void AddVital(VitalType type, int amount) => Add(Vitals, type, amount);

        public void AddResource(ResourceType type, int amount) => Add(Resources, type, amount);

        public void AddHidden(HiddenType type, int amount) => Add(Hidden, type, amount);

        public void AddNpcAffinity(string npcId, int amount) => Add(NpcAffinity, npcId, amount);

        public int StatChange(StatType type) => Stats.TryGetValue(type, out var v) ? v : 0;

        public int VitalChange(VitalType type) => Vitals.TryGetValue(type, out var v) ? v : 0;

        public int ResourceChange(ResourceType type) => Resources.TryGetValue(type, out var v) ? v : 0;

        /// <summary>다른 기록을 이 기록에 더한다 (주간 합계용).</summary>
        public void Merge(StateDelta other)
        {
            foreach (var pair in other.Stats) AddStat(pair.Key, pair.Value);
            foreach (var pair in other.Vitals) AddVital(pair.Key, pair.Value);
            foreach (var pair in other.Resources) AddResource(pair.Key, pair.Value);
            foreach (var pair in other.Hidden) AddHidden(pair.Key, pair.Value);
            ProficiencyLevelUps.AddRange(other.ProficiencyLevelUps);
            FlagsSet.AddRange(other.FlagsSet);
            FlagsCleared.AddRange(other.FlagsCleared);
            CluesAdded.AddRange(other.CluesAdded);
            foreach (var pair in other.NpcAffinity) AddNpcAffinity(pair.Key, pair.Value);
            NpcStatusChanges.AddRange(other.NpcStatusChanges);
            ItemsGained.AddRange(other.ItemsGained);
            SleepDebt += other.SleepDebt;
            Conscience += other.Conscience;
        }

        private static void Add<TKey>(Dictionary<TKey, int> map, TKey key, int amount)
        {
            if (amount == 0)
            {
                return;
            }

            map.TryGetValue(key, out var current);
            map[key] = current + amount;
        }
    }
}
