using System;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Events;
using ApoGirl.Core.State;

namespace ApoGirl.Presentation.Text
{
    /// <summary>
    /// 디버그 "선택지 결과 미리 보기"의 글: 코어가 계산한 결과(<see cref="ChoicePreviewResult"/>)를 줄로 만든다.
    /// 수치는 "이름 +3", 실제 값이 정의값과 다르면 "이름 +2 (정의 +3)". 깃발·단서·이벤트는 디버그용이라 ID 그대로.
    /// </summary>
    public static class ChoicePreviewText
    {
        /// <summary>미리 보기 판에 넣을 글 (없으면 "").</summary>
        public static string Text(ChoicePreviewResult preview, string note = null)
        {
            if (preview == null)
            {
                return "";
            }

            var lines = Lines(preview);
            if (!string.IsNullOrEmpty(note))
            {
                lines.Add(note);
            }

            return string.Join("\n", lines);
        }

        public static List<string> Lines(ChoicePreviewResult preview)
        {
            var lines = new List<string> { Loc.T("preview.title") };
            if (preview == null)
            {
                return lines;
            }

            if (!preview.Enabled)
            {
                lines.Add(Loc.T("preview.disabled"));
            }

            if (preview.HasCheck && preview.Failure != null)
            {
                var percent = (int)Math.Round(preview.Chance * 100f);
                lines.Add(Loc.F("preview.success", percent));
                lines.AddRange(BranchLines(preview.Success));
                lines.Add(Loc.F("preview.failure", 100 - percent));
                lines.AddRange(BranchLines(preview.Failure));
            }
            else
            {
                lines.AddRange(BranchLines(preview.Success));
            }

            return lines;
        }

        /// <summary>한 갈래의 줄. 바뀌는 것이 없으면 "바뀌는 값 없음" 한 줄.</summary>
        public static List<string> BranchLines(PreviewBranch branch)
        {
            var lines = EntryLines(branch.Entries);
            if (branch.GameOver)
            {
                lines.Add(Loc.T("preview.game_over"));
            }

            if (!string.IsNullOrEmpty(branch.NextEventId))
            {
                lines.Add(Loc.F("preview.next", EventName(branch.NextEventId)));
                lines.AddRange(EntryLines(branch.NextEntries).Select(line => Loc.F("preview.next_show", line)));
            }

            if (lines.Count == 0)
            {
                lines.Add(Loc.T("preview.none"));
            }

            return lines;
        }

        /// <summary>탐험 조우 선택지 (깃발·단서·스트레스 — 정의값).</summary>
        public static string Encounter(EncounterChoiceDef choice)
        {
            if (choice == null)
            {
                return "";
            }

            var lines = new List<string> { Loc.T("preview.title") };
            if (Math.Abs(choice.StressDelta) > 0.001f)
            {
                var stress = (int)Math.Round(choice.StressDelta);
                lines.Add(Loc.F("preview.encounter_stress", UiText.Signed(stress)));
            }

            if (choice.SetFlags.Count > 0)
            {
                lines.Add(Loc.F("preview.flags", string.Join("  ", choice.SetFlags.Select(f => "+" + f))));
            }

            if (choice.AddClues.Count > 0)
            {
                lines.Add(Loc.F("preview.clues", string.Join("  ", choice.AddClues)));
            }

            if (lines.Count == 1)
            {
                lines.Add(Loc.T("preview.none"));
            }

            return string.Join("\n", lines);
        }

        /// <summary>줄 묶음: 같은 종류는 한 줄에 모은다 (차례는 코어가 준 그대로).</summary>
        public static List<string> EntryLines(IReadOnlyList<PreviewEntry> entries)
        {
            var lines = new List<string>();
            if (entries == null || entries.Count == 0)
            {
                return lines;
            }

            Group(lines, entries, null, PreviewKind.Stat);
            Group(lines, entries, null, PreviewKind.Vital);
            Group(lines, entries, null, PreviewKind.Resource);
            Group(lines, entries, "preview.hidden", PreviewKind.Hidden);
            Group(lines, entries, null, PreviewKind.SleepDebt, PreviewKind.Conscience, PreviewKind.Defense, PreviewKind.Threat, PreviewKind.Traps);
            Group(lines, entries, "preview.affinity", PreviewKind.NpcAffinity);
            Group(lines, entries, "preview.trust", PreviewKind.NpcTrust);
            Group(lines, entries, null, PreviewKind.NpcStatus, PreviewKind.NpcRole);
            Group(lines, entries, "preview.faction_known", PreviewKind.FactionKnown);
            Group(lines, entries, "preview.faction", PreviewKind.FactionStanding);
            Group(lines, entries, null, PreviewKind.FactionTier);
            Group(lines, entries, "preview.items", PreviewKind.ItemGained);
            Group(lines, entries, "preview.items_lost", PreviewKind.ItemLost);
            Group(lines, entries, "preview.flags", PreviewKind.FlagSet, PreviewKind.FlagCleared);
            Group(lines, entries, "preview.clues", PreviewKind.Clue);
            Group(lines, entries, "preview.facility", PreviewKind.Facility);
            Group(lines, entries, "preview.traits", PreviewKind.Trait);
            Group(lines, entries, "preview.proficiency", PreviewKind.Proficiency);
            Group(lines, entries, "preview.ending_tags", PreviewKind.EndingTag);
            Group(lines, entries, null, PreviewKind.TeacherMobility, PreviewKind.TeacherInfection, PreviewKind.TraumaCare, PreviewKind.PowerRestored);
            Group(lines, entries, "preview.dish", PreviewKind.Dish);
            foreach (var entry in entries.Where(e => e.Kind == PreviewKind.Raid || e.Kind == PreviewKind.Intrusion))
            {
                lines.Add(One(entry));
            }

            return lines;
        }

        private static void Group(List<string> lines, IReadOnlyList<PreviewEntry> entries, string formatKey, params PreviewKind[] kinds)
        {
            var parts = entries.Where(e => Array.IndexOf(kinds, e.Kind) >= 0).Select(One).Where(text => !string.IsNullOrEmpty(text)).ToList();
            if (parts.Count == 0)
            {
                return;
            }

            var joined = string.Join("  ", parts);
            lines.Add(formatKey == null ? joined : Loc.F(formatKey, joined));
        }

        /// <summary>항목 하나의 글.</summary>
        public static string One(PreviewEntry entry)
        {
            switch (entry.Kind)
            {
                case PreviewKind.Stat: return Number(entry, Name("stat." + entry.Key, entry.Key));
                case PreviewKind.Vital: return Number(entry, Name("vital." + entry.Key, entry.Key));
                case PreviewKind.Resource: return Number(entry, Name("res." + entry.Key, entry.Key));
                case PreviewKind.Hidden: return Number(entry, Name("preview.hidden." + entry.Key, entry.Key));
                case PreviewKind.SleepDebt: return Loc.F("result.sleep_debt", Amount(entry));
                case PreviewKind.Conscience: return Loc.F("result.conscience", Amount(entry));
                case PreviewKind.Defense: return Loc.F("result.defense", Amount(entry));
                case PreviewKind.Threat: return Loc.F("result.threat", Amount(entry));
                case PreviewKind.Traps: return Loc.F("preview.traps", Amount(entry));
                case PreviewKind.NpcAffinity:
                case PreviewKind.NpcTrust: return Number(entry, Npc(entry.Key));
                case PreviewKind.NpcStatus: return Loc.F("preview.npc_status", Npc(entry.Key), Name("npc.status." + entry.Value, entry.Value));
                case PreviewKind.NpcRole: return Loc.F("preview.npc_role", Npc(entry.Key), Name("role." + entry.Value, entry.Value));
                case PreviewKind.FactionStanding: return Number(entry, Faction(entry.Key));
                case PreviewKind.FactionKnown: return Faction(entry.Key);
                case PreviewKind.FactionTier: return Loc.F("preview.faction_tier", Faction(entry.Key), Name("faction.tier." + entry.Value, entry.Value));
                case PreviewKind.ItemGained:
                case PreviewKind.ItemLost: return Loc.Item(entry.Key) + " ×" + entry.Amount;
                case PreviewKind.FlagSet: return "+" + entry.Key + Already(entry);
                case PreviewKind.FlagCleared: return "−" + entry.Key + Already(entry);
                case PreviewKind.Clue: return entry.Key + Already(entry);
                case PreviewKind.Facility: return Name("facility." + entry.Key, entry.Key) + Already(entry);
                case PreviewKind.Trait: return Name("trait." + entry.Key, entry.Key) + Already(entry);
                case PreviewKind.Proficiency:
                    return Enum.TryParse<ProficiencyType>(entry.Key, out var proficiency)
                        ? UiText.Proficiency(proficiency) + " Lv " + entry.Amount
                        : entry.Key + " Lv " + entry.Amount;
                case PreviewKind.EndingTag: return Number(entry, entry.Key);
                case PreviewKind.TeacherMobility: return Loc.F("preview.teacher_mobility", entry.Amount);
                case PreviewKind.TeacherInfection: return Loc.F("preview.teacher_infection", Amount(entry));
                case PreviewKind.TraumaCare: return Loc.F("preview.trauma_care", entry.Amount);
                case PreviewKind.PowerRestored: return Loc.T("result.power_restored");
                case PreviewKind.Dish: return Name("dish." + entry.Key, entry.Key);
                case PreviewKind.Raid: return Loc.F("preview.raid", entry.Amount);
                case PreviewKind.Intrusion: return Loc.F("preview.intrusion", entry.Value);
                default: return entry.Key;
            }
        }

        private static string Number(PreviewEntry entry, string name) => name + " " + Amount(entry);

        /// <summary>"+3", 정의값과 다르면 "+2 (정의 +3)".</summary>
        private static string Amount(PreviewEntry entry)
        {
            var text = entry.Amount == 0 ? "0" : UiText.Signed(entry.Amount);
            return entry.Adjusted ? text + " " + Loc.F("preview.defined", entry.Defined == 0 ? "0" : UiText.Signed(entry.Defined)) : text;
        }

        private static string Already(PreviewEntry entry) => entry.Already ? Loc.T("preview.already") : "";

        private static string Npc(string id) => Name("npc." + id, id);

        private static string Faction(string id) => Name("faction." + id + ".name", id);

        /// <summary>이어지는 이벤트: ID (제목이 있으면 같이).</summary>
        private static string EventName(string id)
        {
            var title = Loc.Lookup(Loc.UiTable, "event." + id + ".title");
            return string.IsNullOrEmpty(title) ? id : id + " (" + title + ")";
        }

        private static string Name(string key, string fallback)
        {
            var text = Loc.Lookup(Loc.UiTable, key);
            return string.IsNullOrEmpty(text) ? fallback : text;
        }
    }
}
