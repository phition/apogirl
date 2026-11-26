using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>
    /// PC통신 '이제누리' (사용자 요청 2026-11-26). 컴퓨터실이 열린 뒤 선생님의 모뎀 이야기(EVT_BBS_MODEM)가 KnownFlag를 켜면
    /// 허브에서 컴퓨터실 컴퓨터로 접속할 수 있다. 컴퓨터 전력이 불안정해 접속 중에 멎으면 그 주에는 다시 켜지지 않는다.
    /// 접속 시간·화면 내용은 표현 데이터(Resources/Bbs/bbs.json)에 있다. 여기서는 깃발 이름 규칙만 안다.
    /// </summary>
    public static class BbsRules
    {
        public const string KnownFlag = "BBS_KNOWN";
        private const string DownPrefix = "BBS_DOWN_W";

        /// <summary>그 주에 컴퓨터가 멎었다는 깃발.</summary>
        public static string DownFlag(int week) => DownPrefix + week;

        public static bool IsKnown(GameState state) => state != null && state.Flags.Contains(KnownFlag);

        /// <summary>이번 주에는 이미 멎어서 다시 켜지지 않는다.</summary>
        public static bool IsDown(GameState state) => state != null && state.Flags.Contains(DownFlag(state.Calendar.Week));

        public static bool CanConnect(GameState state) => IsKnown(state) && !IsDown(state);

        /// <summary>컴퓨터가 멎었다: 다음 주까지 접속할 수 없다. 지난 주들의 깃발은 지운다 (깃발이 쌓이지 않게).</summary>
        public static void MarkDown(GameState state)
        {
            if (state == null)
            {
                return;
            }

            state.Flags.RemoveWhere(flag => flag.StartsWith(DownPrefix));
            state.Flags.Add(DownFlag(state.Calendar.Week));
        }
    }
}
