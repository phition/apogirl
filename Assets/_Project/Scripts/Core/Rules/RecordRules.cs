using ApoGirl.Core.Definitions;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>
    /// 1년의 기록 (GameState.Records, Docs/PROPOSAL_ENDINGS_EXPANSION 8.2절). 게임 수치는 바꾸지 않고 숫자만 센다 —
    /// 엔딩 조건(ValueKind.Record)이 읽는다. 무게·부상·거래는 그 규칙이 직접 부르고, 주 끝과 탐험 결과는 WeekRunner가 부른다.
    /// </summary>
    public static class RecordRules
    {
        /// <summary>양심의 무게가 실제로 오른 만큼 (덜어진 것은 세지 않는다).</summary>
        public static void ConscienceRose(GameState state, int amount)
        {
            if (state != null && amount > 0)
            {
                state.Records.ConscienceRaised += amount;
            }
        }

        /// <summary>새로 다쳤다 (INJURED가 붙었다).</summary>
        public static void Injured(GameState state)
        {
            if (state != null)
            {
                state.Records.InjuryCount++;
            }
        }

        /// <summary>세력과 거래가 성사됐다.</summary>
        public static void Traded(GameState state)
        {
            if (state != null)
            {
                state.Records.TradeCount++;
            }
        }

        /// <summary>PC통신에 접속했다. 한 주에 한 번만 센다 (계획 중에 몇 번을 켜도). 셌으면 true.</summary>
        public static bool BbsConnected(GameState state)
        {
            if (state == null || state.Records.BbsLastWeek == state.Calendar.Week)
            {
                return false;
            }

            state.Records.BbsLastWeek = state.Calendar.Week;
            state.Records.BbsSessions++;
            return true;
        }

        /// <summary>주간 정산: 스트레스 한계로 마친 주, 식량이 바닥난 주.</summary>
        public static void WeekEnd(GameState state, BalanceConfig balance)
        {
            if (state == null || balance == null)
            {
                return;
            }

            if (balance.RecordHighStress > 0 && state.Player.Vitals.Stress >= balance.RecordHighStress)
            {
                state.Records.HighStressWeeks++;
            }

            if (state.Base.GetResource(ResourceType.Food) <= 0)
            {
                state.Records.HungryWeeks++;
            }
        }

        /// <summary>탐험 결과: 좀비에게 쫓기지 않고 돌아온 탐험, 깊은 층에 들어가 본 지역.</summary>
        public static void ApplyExpedition(GameState state, string locationId, ExpeditionResult result)
        {
            if (state == null || result == null)
            {
                return;
            }

            if (result.Undetected && result.Status == ExtractStatus.Extracted)
            {
                state.Records.QuietRuns++;
            }

            if (result.EnteredDeep && !string.IsNullOrEmpty(locationId))
            {
                state.Records.DeepVisited.Add(locationId);
            }
        }
    }
}
