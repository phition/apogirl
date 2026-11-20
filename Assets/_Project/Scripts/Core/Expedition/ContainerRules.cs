using System;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Random;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Expedition
{
    /// <summary>
    /// 탐험 상자: 처음부터 빈 상자(판 시드), 뒤진 상자 기록, 매달 첫 주 복구와 '누군가 머물다 간 흔적' 표시.
    /// 실시간 탐험과 헤드리스가 같이 쓴다.
    /// </summary>
    public static class ContainerRules
    {
        /// <summary>뒤진 상자 키가 속한 방들의 SearchFlag를 세운다 (예: 1층 끝 교실 → 거처 이벤트).</summary>
        public static void ApplyRoomFlags(LocationDef location, IEnumerable<string> searchedKeys, GameState state, StateDelta delta)
        {
            var layout = location?.Layout;
            if (layout == null || searchedKeys == null)
            {
                return;
            }

            var prefix = location.Id + "/R";
            foreach (var key in searchedKeys)
            {
                if (key == null || !key.StartsWith(prefix, StringComparison.Ordinal))
                {
                    continue;
                }

                var dot = key.IndexOf('.', prefix.Length);
                if (dot < 0 || !int.TryParse(key.Substring(prefix.Length, dot - prefix.Length), out var doorIndex) || doorIndex < 0 || doorIndex >= layout.Doors.Count)
                {
                    continue;
                }

                var flag = layout.Doors[doorIndex].SearchFlag;
                if (!string.IsNullOrEmpty(flag) && state.Flags.Add(flag))
                {
                    delta.FlagsSet.Add(flag);
                }
            }
        }

        public const string RestockFlagPrefix = "RESTOCKED_";

        /// <summary>이 지역의 물자가 다시 채워졌고 아직 들어가 보지 않았다.</summary>
        public static string RestockFlag(string locationId) => RestockFlagPrefix + locationId;

        /// <summary>판마다 정해지는, 처음부터 빈 상자인가 (복구되지 않는다).</summary>
        public static bool BaselineEmpty(int worldSeed, string key, float rate)
        {
            if (rate <= 0f || string.IsNullOrEmpty(key))
            {
                return false;
            }

            unchecked
            {
                var hash = (ulong)(uint)worldSeed * 0x9E3779B97F4A7C15UL;
                foreach (var c in key)
                {
                    hash = (hash ^ c) * 0x100000001B3UL;
                }

                // SplitMix64 마무리 (고른 분포)
                hash += 0x9E3779B97F4A7C15UL;
                hash = (hash ^ (hash >> 30)) * 0xBF58476D1CE4E5B9UL;
                hash = (hash ^ (hash >> 27)) * 0x94D049BB133111EBUL;
                hash ^= hash >> 31;
                return (hash >> 11) * (1.0 / (1UL << 53)) < rate;
            }
        }

        /// <summary>weekBefore 주의 1일과 weekAfter 주의 1일 사이에 달이 바뀌었는가.</summary>
        public static bool MonthStarted(int weekBefore, int weekAfter, BalanceConfig balance)
        {
            return CalendarRules.DateOf(weekBefore, 1, balance).Month != CalendarRules.DateOf(weekAfter, 1, balance).Month;
        }

        /// <summary>
        /// 매달 첫 주: 복구되는 지역마다 뒤진 상자의 RestockFraction을 다시 채우고 '흔적' 플래그를 켠다. 반환: 복구된 지역 ID.
        /// </summary>
        public static List<string> Restock(GameState state, IDefinitionDatabase db, IRandomService random, BalanceConfig balance)
        {
            var restocked = new List<string>();
            var looted = state.Exploration.LootedContainers;
            var byLocation = looted.GroupBy(k => k.Substring(0, Math.Max(0, k.IndexOf('/'))))
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .ToList();
            foreach (var group in byLocation)
            {
                if (!db.TryGetLocation(group.Key, out var location) || !location.Restocks)
                {
                    continue;
                }

                var keys = group.OrderBy(k => k, StringComparer.Ordinal).ToList();
                var count = (int)Math.Round(keys.Count * balance.RestockFraction, MidpointRounding.AwayFromZero);
                if (count <= 0)
                {
                    continue;
                }

                for (var i = 0; i < count; i++)
                {
                    var index = random.Range(i, keys.Count - 1);
                    (keys[i], keys[index]) = (keys[index], keys[i]);
                    looted.Remove(keys[i]);
                }

                state.Flags.Add(RestockFlag(group.Key));
                restocked.Add(group.Key);
            }

            return restocked;
        }
    }
}
