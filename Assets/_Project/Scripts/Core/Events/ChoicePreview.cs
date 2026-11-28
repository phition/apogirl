using System;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Events
{
    /// <summary>미리 보기 한 줄이 가리키는 것.</summary>
    public enum PreviewKind
    {
        Stat,
        Vital,
        Resource,
        Hidden,
        SleepDebt,
        Conscience,
        Defense,
        Threat,
        Traps,
        NpcAffinity,
        NpcTrust,
        NpcStatus,
        NpcRole,
        FactionStanding,
        FactionKnown,
        FactionTier,
        ItemGained,
        ItemLost,
        FlagSet,
        FlagCleared,
        Clue,
        Facility,
        Trait,
        Proficiency,
        EndingTag,
        TeacherMobility,
        TeacherInfection,
        TraumaCare,
        PowerRestored,
        Dish,

        /// <summary>이벤트 습격 (세기 = Amount). 결과는 난수라 미리 보기에 넣지 않는다.</summary>
        Raid,

        /// <summary>낮 침입 대응 (Value = 대응 방식). 결과는 난수라 미리 보기에 넣지 않는다.</summary>
        Intrusion,
    }

    /// <summary>미리 보기 한 줄: 무엇이(Kind·Key) 얼마나(Amount) 바뀌는가.</summary>
    public sealed class PreviewEntry
    {
        public PreviewKind Kind;

        /// <summary>능력치·자원 등은 열거형 이름, 인물·세력·아이템·깃발 등은 ID.</summary>
        public string Key = "";

        /// <summary>수치인가 (false면 이름만 보여 준다).</summary>
        public bool Numeric;

        /// <summary>실제로 적용될 값 (범위 제한·배율을 거친 값). 아이템은 개수, 숙련은 오른 뒤 단계.</summary>
        public int Amount;

        /// <summary>데이터에 정의된 값. 실제 값과 다르면 화면이 같이 보여 준다.</summary>
        public int Defined;

        /// <summary>바뀐 뒤의 상태·단계 이름 (인물 상태, 역할, 세력 단계, 침입 대응).</summary>
        public string Value = "";

        /// <summary>이미 그 상태라 바뀌지 않는다 (이미 켜진 깃발·이미 아는 단서).</summary>
        public bool Already;

        /// <summary>정의값과 실제 값이 다르다.</summary>
        public bool Adjusted => Numeric && Amount != Defined;
    }

    /// <summary>선택지의 한 갈래 (판정이 없으면 하나, 있으면 성공·실패).</summary>
    public sealed class PreviewBranch
    {
        public List<PreviewEntry> Entries = new List<PreviewEntry>();

        /// <summary>이 선택으로 체력이 0이 된다 (게임 오버).</summary>
        public bool GameOver;

        /// <summary>이어서 바로 시작할 이벤트 (없으면 "").</summary>
        public string NextEventId = "";

        /// <summary>이어지는 이벤트가 시작될 때 바로 적용되는 효과.</summary>
        public List<PreviewEntry> NextEntries = new List<PreviewEntry>();

        public PreviewEntry Find(PreviewKind kind, string key) => Entries.Find(e => e.Kind == kind && e.Key == key);
    }

    /// <summary>선택지 하나를 골랐을 때의 결과.</summary>
    public sealed class ChoicePreviewResult
    {
        public string ChoiceId = "";

        /// <summary>비용을 낼 수 있다 (false면 화면에서 고를 수 없는 선택지).</summary>
        public bool Enabled = true;

        public bool HasCheck;

        /// <summary>지금 상태에서의 성공 확률 (판정이 없으면 1).</summary>
        public float Chance = 1f;

        /// <summary>판정이 없거나 성공했을 때.</summary>
        public PreviewBranch Success = new PreviewBranch();

        /// <summary>판정에 실패했을 때 (판정이 없으면 null).</summary>
        public PreviewBranch Failure;
    }

    /// <summary>
    /// 디버그: 이벤트 선택지를 골랐을 때 바뀔 값을 미리 본다. 상태 사본에 실제 적용 경로(비용 → 효과 → 부상 판정 →
    /// 이어지는 이벤트의 시작 효과)를 그대로 돌려 '실제로 적용될 값'을 얻는다 — 원본 상태와 난수는 건드리지 않는다.
    /// 난수가 드는 결과(이벤트 습격·낮 침입 대응·희망 덕에 통한 설득)는 돌리지 않고 이름만 남긴다.
    /// </summary>
    public static class ChoicePreview
    {
        /// <param name="clone">지금 상태의 깊은 사본을 만든다 (부를 때마다 새 사본). 사본만 고친다.</param>
        public static ChoicePreviewResult Build(Func<GameState> clone, IDefinitionDatabase db, EventDef ev, EventChoiceDef choice, BalanceConfig balance)
        {
            if (clone == null || ev == null || choice == null || balance == null)
            {
                return null;
            }

            var now = clone();
            var result = new ChoicePreviewResult
            {
                ChoiceId = choice.Id,
                Enabled = EventResolver.CanAfford(now, choice),
                HasCheck = choice.HasCheck,
                Chance = choice.HasCheck ? CheckResolver.ComputeChance(now, choice.Check, balance) : 1f,
            };
            result.Success = Branch(now, db, ev, choice, choice.Effect, balance);
            if (choice.HasCheck)
            {
                result.Failure = Branch(clone(), db, ev, choice, choice.OnFailure, balance);
            }

            return result;
        }

        /// <summary>효과 하나만 (비용·이어지는 이벤트 없이). 이벤트의 시작 효과(OnShow) 같은 것을 볼 때.</summary>
        public static List<PreviewEntry> Effect(GameState copy, GameEffect effect, BalanceConfig balance)
        {
            return Apply(copy, null, effect, balance);
        }

        private static PreviewBranch Branch(GameState copy, IDefinitionDatabase db, EventDef ev, EventChoiceDef choice, GameEffect effect, BalanceConfig balance)
        {
            var branch = new PreviewBranch { Entries = Apply(copy, choice.Costs, effect, balance) };
            branch.GameOver = copy.Player.Vitals.Hp <= 0;
            var nextId = string.IsNullOrEmpty(choice.NextEventId) ? ev.ThenEventId : choice.NextEventId;
            if (!branch.GameOver && !string.IsNullOrEmpty(nextId) && db != null && db.TryGetEvent(nextId, out var next))
            {
                branch.NextEventId = next.Id;
                branch.NextEntries = Apply(copy, null, next.OnShow, balance);
            }

            return branch;
        }

        /// <summary>사본에 비용과 효과를 적용하고 달라진 것을 줄로 만든다 (EventResolver.Choose·WeekRunner.ApplyChoice와 같은 차례).</summary>
        private static List<PreviewEntry> Apply(GameState copy, List<ResourceAmount> costs, GameEffect effect, BalanceConfig balance)
        {
            effect = effect ?? new GameEffect();
            var before = Probe.Of(copy);
            var delta = new StateDelta();
            var hpBefore = copy.Player.Vitals.Hp;
            foreach (var cost in costs ?? Enumerable.Empty<ResourceAmount>())
            {
                EffectApplier.ChangeResource(copy, cost.Resource, -cost.Amount, delta);
            }

            EffectApplier.Apply(copy, effect, balance, delta);
            InjuryRules.CheckIncident(copy, hpBefore, balance, delta); // 체력을 크게 잃으면 부상 (난수 없음)
            return Collect(before, Probe.Of(copy), delta, costs, effect);
        }

        private static List<PreviewEntry> Collect(Probe before, Probe after, StateDelta delta, List<ResourceAmount> costs, GameEffect effect)
        {
            var entries = new List<PreviewEntry>();

            // ── 수치: 정의값과 실제 값을 같이 ──
            Numbers(entries, PreviewKind.Stat, delta.Stats, Sum(effect.Stats, s => s.Stat, s => s.Amount));
            Numbers(entries, PreviewKind.Vital, delta.Vitals, Sum(effect.Vitals, v => v.Vital, v => v.Amount));
            var resources = Sum(effect.Resources, r => r.Resource, r => r.Amount);
            foreach (var cost in costs ?? Enumerable.Empty<ResourceAmount>())
            {
                resources.TryGetValue(cost.Resource, out var current);
                resources[cost.Resource] = current - cost.Amount;
            }

            Numbers(entries, PreviewKind.Resource, delta.Resources, resources);
            Numbers(entries, PreviewKind.Hidden, delta.Hidden, Sum(effect.Hidden, h => h.Hidden, h => h.Amount));
            Number(entries, PreviewKind.SleepDebt, "", delta.SleepDebt, effect.SleepDebt);
            Number(entries, PreviewKind.Conscience, "", delta.Conscience, effect.Conscience);
            Number(entries, PreviewKind.Defense, "", delta.Defense, effect.Defense);
            Number(entries, PreviewKind.Threat, "", delta.Threat, effect.Threat);
            Number(entries, PreviewKind.Traps, "", after.Traps - before.Traps, effect.Traps);

            // ── 인물 (선생님은 신뢰도 호감으로 합친다, EffectApplier.ChangeNpc) ──
            var affinity = new Dictionary<string, int>();
            var trust = new Dictionary<string, int>();
            foreach (var change in effect.Npcs.Where(c => c != null && !string.IsNullOrEmpty(c.NpcId)))
            {
                var teacher = change.NpcId == EffectApplier.TeacherNpcId;
                Add(affinity, change.NpcId, change.Affinity + (teacher ? change.Trust : 0));
                if (!teacher)
                {
                    Add(trust, change.NpcId, change.Trust);
                }
            }

            Numbers(entries, PreviewKind.NpcAffinity, delta.NpcAffinity, affinity);
            Numbers(entries, PreviewKind.NpcTrust, Diff(before.Trust, after.Trust), trust);
            foreach (var change in delta.NpcStatusChanges)
            {
                var parts = change.Split(':');
                if (parts.Length == 2)
                {
                    entries.Add(new PreviewEntry { Kind = PreviewKind.NpcStatus, Key = parts[0], Value = parts[1] });
                }
            }

            foreach (var pair in after.Roles)
            {
                before.Roles.TryGetValue(pair.Key, out var role);
                if ((role ?? "") != pair.Value)
                {
                    entries.Add(new PreviewEntry { Kind = PreviewKind.NpcRole, Key = pair.Key, Value = pair.Value });
                }
            }

            // ── 세력 ──
            foreach (var id in delta.FactionsRevealed)
            {
                entries.Add(new PreviewEntry { Kind = PreviewKind.FactionKnown, Key = id });
            }

            Numbers(entries, PreviewKind.FactionStanding, delta.FactionStanding,
                Sum(effect.Factions.Where(f => f != null && !string.IsNullOrEmpty(f.FactionId)), f => f.FactionId, f => f.Standing));
            foreach (var change in delta.FactionTierChanges)
            {
                var parts = change.Split(':');
                if (parts.Length == 2)
                {
                    entries.Add(new PreviewEntry { Kind = PreviewKind.FactionTier, Key = parts[0], Value = parts[1] });
                }
            }

            // ── 이름으로 보여 줄 것 ──
            foreach (var item in delta.ItemsGained)
            {
                entries.Add(new PreviewEntry { Kind = PreviewKind.ItemGained, Key = item.ItemId, Amount = item.Count, Defined = item.Count });
            }

            foreach (var item in delta.ItemsLost)
            {
                entries.Add(new PreviewEntry { Kind = PreviewKind.ItemLost, Key = item.ItemId, Amount = item.Count, Defined = item.Count });
            }

            Names(entries, PreviewKind.FlagSet, delta.FlagsSet, effect.SetFlags);
            Names(entries, PreviewKind.FlagCleared, delta.FlagsCleared, effect.ClearFlags);
            Names(entries, PreviewKind.Clue, delta.CluesAdded, effect.AddClues);
            Names(entries, PreviewKind.Facility, delta.FacilitiesOpened, effect.Facilities.Select(f => f.FacilityId));
            Names(entries, PreviewKind.Trait, after.Traits.Where(t => !before.Traits.Contains(t)), effect.AddTraits);
            foreach (var pair in after.Proficiencies)
            {
                before.Proficiencies.TryGetValue(pair.Key, out var level);
                if (pair.Value != level)
                {
                    entries.Add(new PreviewEntry { Kind = PreviewKind.Proficiency, Key = pair.Key.ToString(), Amount = pair.Value, Defined = pair.Value });
                }
            }

            Numbers(entries, PreviewKind.EndingTag, Diff(before.EndingTags, after.EndingTags),
                Sum(effect.EndingTags.Where(t => t != null && !string.IsNullOrEmpty(t.Tag)), t => t.Tag, t => t.Amount));

            // ── 선생님·그 밖 ──
            if (after.TeacherMobility != before.TeacherMobility)
            {
                entries.Add(new PreviewEntry { Kind = PreviewKind.TeacherMobility, Amount = after.TeacherMobility, Defined = effect.TeacherMobility });
            }

            Number(entries, PreviewKind.TeacherInfection, "", after.TeacherInfection - before.TeacherInfection, effect.TeacherInfection);
            if (effect.TraumaCare > 0)
            {
                entries.Add(new PreviewEntry { Kind = PreviewKind.TraumaCare, Amount = effect.TraumaCare, Defined = effect.TraumaCare });
            }

            if (delta.PowerRestored)
            {
                entries.Add(new PreviewEntry { Kind = PreviewKind.PowerRestored });
            }

            foreach (var dish in delta.DishesCooked)
            {
                entries.Add(new PreviewEntry { Kind = PreviewKind.Dish, Key = dish });
            }

            // ── 난수가 드는 결과: 돌리지 않고 이름만 ──
            if (effect.RaidStrength > 0)
            {
                entries.Add(new PreviewEntry { Kind = PreviewKind.Raid, Amount = effect.RaidStrength, Defined = effect.RaidStrength });
            }

            if (!string.IsNullOrEmpty(effect.IntrusionResponse))
            {
                entries.Add(new PreviewEntry { Kind = PreviewKind.Intrusion, Value = effect.IntrusionResponse });
            }

            return entries;
        }

        // ── 줄 만들기 ──

        private static void Number(List<PreviewEntry> entries, PreviewKind kind, string key, int actual, int defined)
        {
            if (actual != 0 || defined != 0)
            {
                entries.Add(new PreviewEntry { Kind = kind, Key = key, Numeric = true, Amount = actual, Defined = defined });
            }
        }

        /// <summary>정의된 것을 먼저(정의 차례대로), 그 뒤 규칙이 덧붙인 것(예: 부상 감염, 허기로 깎인 체력).</summary>
        private static void Numbers<TKey>(List<PreviewEntry> entries, PreviewKind kind, Dictionary<TKey, int> actual, Dictionary<TKey, int> defined)
        {
            foreach (var pair in defined)
            {
                actual.TryGetValue(pair.Key, out var applied);
                Number(entries, kind, pair.Key.ToString(), applied, pair.Value);
            }

            foreach (var pair in actual)
            {
                if (!defined.ContainsKey(pair.Key))
                {
                    Number(entries, kind, pair.Key.ToString(), pair.Value, 0);
                }
            }
        }

        /// <summary>실제로 바뀐 것, 그리고 정의에는 있지만 이미 그 상태라 바뀌지 않는 것.</summary>
        private static void Names(List<PreviewEntry> entries, PreviewKind kind, IEnumerable<string> changed, IEnumerable<string> defined)
        {
            var seen = new HashSet<string>();
            foreach (var id in changed)
            {
                if (!string.IsNullOrEmpty(id) && seen.Add(id))
                {
                    entries.Add(new PreviewEntry { Kind = kind, Key = id });
                }
            }

            foreach (var id in defined)
            {
                if (!string.IsNullOrEmpty(id) && seen.Add(id))
                {
                    entries.Add(new PreviewEntry { Kind = kind, Key = id, Already = true });
                }
            }
        }

        private static Dictionary<TKey, int> Sum<T, TKey>(IEnumerable<T> items, Func<T, TKey> key, Func<T, int> amount)
        {
            var sums = new Dictionary<TKey, int>();
            foreach (var item in items)
            {
                Add(sums, key(item), amount(item));
            }

            return sums;
        }

        private static void Add<TKey>(Dictionary<TKey, int> map, TKey key, int amount)
        {
            map.TryGetValue(key, out var current);
            map[key] = current + amount;
        }

        private static Dictionary<string, int> Diff(Dictionary<string, int> before, Dictionary<string, int> after)
        {
            var diff = new Dictionary<string, int>();
            foreach (var pair in after)
            {
                before.TryGetValue(pair.Key, out var old);
                if (pair.Value != old)
                {
                    diff[pair.Key] = pair.Value - old;
                }
            }

            return diff;
        }

        /// <summary>변화 기록(StateDelta)에 남지 않는 값들의 스냅샷 (앞뒤를 비교한다).</summary>
        private sealed class Probe
        {
            public readonly Dictionary<string, int> Trust = new Dictionary<string, int>();
            public readonly Dictionary<string, string> Roles = new Dictionary<string, string>();
            public readonly Dictionary<string, int> EndingTags = new Dictionary<string, int>();
            public readonly Dictionary<ProficiencyType, int> Proficiencies = new Dictionary<ProficiencyType, int>();
            public readonly HashSet<string> Traits = new HashSet<string>();
            public int Traps;
            public int TeacherMobility;
            public int TeacherInfection;

            public static Probe Of(GameState state)
            {
                var probe = new Probe
                {
                    Traps = state.Base.Traps,
                    TeacherMobility = state.Teacher.MobilityStage,
                    TeacherInfection = state.Teacher.InfectionProgress,
                };
                foreach (var pair in state.Npcs)
                {
                    probe.Trust[pair.Key] = pair.Value.Trust;
                    probe.Roles[pair.Key] = pair.Value.Role ?? "";
                }

                foreach (var pair in state.EndingTags)
                {
                    probe.EndingTags[pair.Key] = pair.Value;
                }

                foreach (ProficiencyType type in Enum.GetValues(typeof(ProficiencyType)))
                {
                    probe.Proficiencies[type] = state.Player.ProficiencyLevel(type);
                }

                foreach (var trait in state.Player.Traits)
                {
                    probe.Traits.Add(trait);
                }

                return probe;
            }
        }
    }
}
