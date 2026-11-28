using ApoGirl.Core.Definitions;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>
    /// 수능날 모의고사 (EVT_CAL_SUNEUNG). 점수는 수치에 쓰이지 않고, 높은 점수만 깃발로 남겨 엔딩(연구자)이 읽는다
    /// (Docs/PROPOSAL_ENDINGS_EXPANSION 8.3절). 봇은 시험을 보지 않는다.
    /// </summary>
    public static class ExamRules
    {
        public const string FlagTaken = "EXAM_TAKEN";
        public const string FlagHigh = "EXAM_SCORE_80";
        public const string FlagTop = "EXAM_SCORE_95";

        /// <summary>시험을 마친 점수(0~100)를 남긴다. 안 봤으면(음수) 아무것도 하지 않는다.</summary>
        public static void Record(GameState state, int score, BalanceConfig balance)
        {
            if (state == null || balance == null || score < 0)
            {
                return;
            }

            state.Flags.Add(FlagTaken);
            if (score >= balance.ExamHighScore) state.Flags.Add(FlagHigh);
            if (score >= balance.ExamTopScore) state.Flags.Add(FlagTop);
        }
    }
}
