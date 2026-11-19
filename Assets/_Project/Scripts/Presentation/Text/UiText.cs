using System.Collections.Generic;
using System.Text;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Planning;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;

namespace ApoGirl.Presentation.Text
{
    /// <summary>화면 표시용 문자열 조합.</summary>
    public static class UiText
    {
        public static string Stat(StatType type) => Loc.T("stat." + type);

        public static string Resource(ResourceType type) => Loc.T("res." + type);

        public static string Vital(VitalType type) => Loc.T("vital." + type);

        public static string Proficiency(ProficiencyType type) => Loc.T("prof." + type);

        public static string Season(Season season) => Loc.T("season." + season);

        public static string Phase(DayPhase phase) => Loc.T("phase." + phase);

        public static string Result(CheckResult result) => Loc.T("result." + result);

        public static string Signed(int value) => value > 0 ? "+" + value : value.ToString();

        public static string SlotLabel(int slotIndex)
        {
            return Loc.F("warn.slot", CalendarRules.DayOf(slotIndex), Phase(CalendarRules.PhaseOf(slotIndex)));
        }

        public static string Warning(PlanWarning warning)
        {
            var key = "warn." + warning.Type;
            return warning.SlotIndex < 0 ? Loc.T(key) : Loc.F(key, SlotLabel(warning.SlotIndex));
        }

        /// <summary>변화량을 줄 단위 문자열로. 변화가 없으면 빈 목록.</summary>
        public static List<string> DeltaLines(StateDelta delta)
        {
            var lines = new List<string>();
            AppendGroup(lines, delta.Stats, Stat);
            AppendGroup(lines, delta.Resources, Resource);
            AppendGroup(lines, delta.Vitals, Vital);
            if (delta.SleepDebt != 0)
            {
                lines.Add(Loc.F("result.sleep_debt", Signed(delta.SleepDebt)));
            }

            if (delta.Conscience != 0)
            {
                lines.Add(Loc.F("result.conscience", Signed(delta.Conscience)));
            }

            foreach (var level in delta.ProficiencyLevelUps)
            {
                lines.Add(Loc.F("result.levelup", Proficiency(level)));
            }

            return lines;
        }

        private static void AppendGroup<TKey>(List<string> lines, Dictionary<TKey, int> values, System.Func<TKey, string> name)
        {
            var builder = new StringBuilder();
            foreach (var pair in values)
            {
                if (pair.Value == 0)
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append("  ");
                }

                builder.Append(name(pair.Key)).Append(' ').Append(Signed(pair.Value));
            }

            if (builder.Length > 0)
            {
                lines.Add(builder.ToString());
            }
        }
    }
}
