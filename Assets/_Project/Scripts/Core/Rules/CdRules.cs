using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>
    /// CD 수집 (기획 A). 탐험에서 주운 CD는 깃발 CD_01~08로 남고, 처음 주운 주차를 CdWeeks에 적는다.
    /// 진열장 칸 수·곡 목록은 표현 데이터(Resources/Art/cd_map.json)에 있다. 여기서는 깃발 이름 규칙만 안다.
    /// </summary>
    public static class CdRules
    {
        public const string Prefix = "CD_";

        /// <summary>CD 깃발인가 (CD_ 뒤가 숫자). 조우 선택 기록 ENC_CD_… 같은 것은 아니다.</summary>
        public static bool IsCd(string flag)
        {
            if (string.IsNullOrEmpty(flag) || !flag.StartsWith(Prefix) || flag.Length == Prefix.Length)
            {
                return false;
            }

            for (var i = Prefix.Length; i < flag.Length; i++)
            {
                if (!char.IsDigit(flag[i])) return false;
            }

            return true;
        }

        /// <summary>새로 켜진 깃발 가운데 CD면 처음 주운 주차를 적는다. 새로 적은 CD ID를 돌려준다.</summary>
        public static List<string> Record(GameState state, IEnumerable<string> newFlags)
        {
            var found = new List<string>();
            foreach (var flag in newFlags.Where(IsCd))
            {
                if (!state.CdWeeks.ContainsKey(flag))
                {
                    state.CdWeeks[flag] = state.Calendar.Week;
                    found.Add(flag);
                }
            }

            return found;
        }

        public static bool Has(GameState state, string cdId) => state.Flags.Contains(cdId);

        /// <summary>가진 CD 수.</summary>
        public static int Count(GameState state) => state.Flags.Count(IsCd);

        /// <summary>곡을 고른다. 가진 CD만, 빈 문자열이면 끈다.</summary>
        public static bool Select(GameState state, string cdId)
        {
            if (string.IsNullOrEmpty(cdId))
            {
                state.CdTrack = "";
                return true;
            }

            if (!Has(state, cdId))
            {
                return false;
            }

            state.CdTrack = cdId;
            return true;
        }
    }
}
