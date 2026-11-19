using System;
using System.Collections.Generic;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Events;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;
using ApoGirl.Core.Text;
using ApoGirl.Presentation.App;
using ApoGirl.Presentation.Text;
using Yarn.Unity;

namespace ApoGirl.Presentation.Dialogue
{
    /// <summary>
    /// Yarn 스크립트에서 쓰는 함수. [YarnFunction]은 Yarn 프로젝트가 컴파일할 때 함수 선언(타입)으로 쓰이고,
    /// 실행은 <see cref="EventDialoguePlayer"/>가 <see cref="Invoke"/>로 직접 호출한다.
    /// 예: <c>{josa(name(), "이/가")} 고개를 끄덕였다.</c>
    /// </summary>
    public static class YarnFunctions
    {
        private static readonly Dictionary<string, Func<IConvertible[], IConvertible>> Table =
            new Dictionary<string, Func<IConvertible[], IConvertible>>
            {
                ["name"] = _ => Name(),
                ["given"] = _ => Given(),
                ["friend"] = _ => Friend(),
                ["josa"] = p => Josa(Str(p, 0), Str(p, 1)),
                ["flag"] = p => Flag(Str(p, 0)),
                ["stat"] = p => (float)Stat(Str(p, 0)),
                ["resource"] = p => (float)Resource(Str(p, 0)),
                ["affinity"] = p => (float)Affinity(Str(p, 0)),
                ["week"] = _ => (float)Week(),
                ["succeeded"] = _ => Succeeded(),
                ["great_success"] = _ => GreatSuccess(),
            };

        private static GameState State => GameApp.Session?.State;

        public static bool Has(string name) => Table.ContainsKey(name ?? "");

        public static IConvertible Invoke(string name, IConvertible[] parameters)
        {
            if (!Table.TryGetValue(name ?? "", out var function))
            {
                throw new InvalidOperationException("알 수 없는 Yarn 함수: " + name);
            }

            return function(parameters ?? Array.Empty<IConvertible>());
        }

        /// <summary>주인공 이름 (기본 한서진, D23).</summary>
        [YarnFunction("name")]
        public static string Name() => State?.Player.Profile.Name ?? "한서진";

        /// <summary>부르는 이름: 별명 → 없으면 성을 뺀 이름 (세 글자 이름일 때). 예: 한서진 → 서진.</summary>
        [YarnFunction("given")]
        public static string Given()
        {
            var profile = State?.Player.Profile;
            if (profile != null && !string.IsNullOrEmpty(profile.Nickname)) return profile.Nickname;
            var name = Name();
            return name.Length == 3 ? name.Substring(1) : name;
        }

        /// <summary>친구 호칭 (D19: 연락처 이름 → 없으면 기본 이름).</summary>
        [YarnFunction("friend")]
        public static string Friend()
        {
            var friend = State?.Friend;
            if (friend != null && !string.IsNullOrEmpty(friend.CallName)) return friend.CallName;
            if (friend != null && !string.IsNullOrEmpty(friend.ContactName)) return friend.ContactName;
            return Loc.T("npc.friend_default");
        }

        /// <summary>단어 + 조사. pair 예: "이/가", "은/는", "을/를", "과/와", "아/야", "으로/로".</summary>
        [YarnFunction("josa")]
        public static string Josa(string word, string pair) => KoreanJosa.Attach(word, pair);

        [YarnFunction("flag")]
        public static bool Flag(string id) => State != null && State.Flags.Contains(id);

        /// <summary>능력치 값 (StatType 이름).</summary>
        [YarnFunction("stat")]
        public static int Stat(string key) => State == null ? 0 : ConditionEvaluator.ValueOf(State, ValueKind.Stat, key);

        [YarnFunction("resource")]
        public static int Resource(string key) => State == null ? 0 : ConditionEvaluator.ValueOf(State, ValueKind.Resource, key);

        /// <summary>NPC 호감 (NPC_TEACHER = 윤 선생님).</summary>
        [YarnFunction("affinity")]
        public static int Affinity(string npcId) => State == null ? 0 : ConditionEvaluator.ValueOf(State, ValueKind.NpcAffinity, npcId);

        [YarnFunction("week")]
        public static int Week() => State?.Calendar.Week ?? 1;

        /// <summary>방금 고른 선택지의 판정이 실패가 아니었는지.</summary>
        [YarnFunction("succeeded")]
        public static bool Succeeded() => EventDialoguePlayer.LastChoiceResult == null || EventDialoguePlayer.LastChoiceResult.Result != CheckResult.Failure;

        [YarnFunction("great_success")]
        public static bool GreatSuccess() => EventDialoguePlayer.LastChoiceResult?.Result == CheckResult.GreatSuccess;

        private static string Str(IConvertible[] parameters, int index)
        {
            // Yarn은 인자를 Yarn.Value(IConvertible)로 넘긴다: ToString()은 "[Value: …]"라서 IFormatProvider 버전을 쓴다
            return parameters.Length > index && parameters[index] != null
                ? parameters[index].ToString(System.Globalization.CultureInfo.InvariantCulture)
                : "";
        }
    }
}
