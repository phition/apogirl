using System.Text.RegularExpressions;
using ApoGirl.Core.State;
using ApoGirl.Core.Text;

namespace ApoGirl.Presentation.Text
{
    /// <summary>
    /// 대화 밖 문장(후일담·기록)의 이름 치환. {name} 전체 이름, {given} 부르는 이름, {friend} 친구 호칭.
    /// 조사는 콜론 뒤에 쌍으로 쓴다: {given:은/는}, {friend:이/가}, {given:아/야}.
    /// </summary>
    public static class StoryText
    {
        private static readonly Regex Token = new Regex(@"\{(name|given|friend)(?::([^}]+))?\}", RegexOptions.Compiled);

        public static string Format(string text, GameState state)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text ?? "";
            }

            return Token.Replace(text, match =>
            {
                var word = Word(match.Groups[1].Value, state);
                return match.Groups[2].Success ? KoreanJosa.Attach(word, match.Groups[2].Value) : word;
            });
        }

        /// <summary>UI 문자열 키를 찾아 치환까지.</summary>
        public static string T(string key, GameState state) => Format(Loc.T(key), state);

        public static string Name(GameState state)
        {
            var name = state?.Player.Profile.Name;
            return string.IsNullOrEmpty(name) ? "한서진" : name;
        }

        /// <summary>별명 → 없으면 성을 뺀 이름 (세 글자 이름일 때).</summary>
        public static string Given(GameState state)
        {
            var nickname = state?.Player.Profile.Nickname;
            if (!string.IsNullOrEmpty(nickname)) return nickname;
            var name = Name(state);
            return name.Length == 3 ? name.Substring(1) : name;
        }

        public static string Friend(GameState state)
        {
            var friend = state?.Friend;
            if (friend != null && !string.IsNullOrEmpty(friend.CallName)) return friend.CallName;
            if (friend != null && !string.IsNullOrEmpty(friend.ContactName)) return friend.ContactName;
            return Loc.T("npc.friend_default");
        }

        private static string Word(string token, GameState state)
        {
            switch (token)
            {
                case "name": return Name(state);
                case "given": return Given(state);
                default: return Friend(state);
            }
        }
    }
}
