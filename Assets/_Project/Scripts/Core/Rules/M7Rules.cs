using System;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>
    /// 수혈 적합성 (문서 7.1 ④, D16). ABO만 다루고 Rh는 다루지 않는다.
    /// 혈액형을 모르면(Unknown) 적합 여부를 확인할 수 없으므로 부적합으로 본다 → 혈액 팩 경로.
    /// </summary>
    public static class BloodRules
    {
        public static bool CanDonate(BloodType donor, BloodType recipient)
        {
            if (donor == BloodType.Unknown || recipient == BloodType.Unknown)
            {
                return false;
            }

            if (donor == BloodType.O || recipient == BloodType.AB)
            {
                return true;
            }

            return donor == recipient;
        }
    }

    /// <summary>적대적 생존자에게 구타당한 트라우마 (단계당 낮 탐험 스트레스 증가, 시간·돌봄으로 회복).</summary>
    public static class AssaultTraumaRules
    {
        public const string FlagAssaulted = "ASSAULTED_ON_RUN";

        /// <summary>구타당함: 단계 +1 (상한), 회복 진행 초기화.</summary>
        public static void Add(GameState state, BalanceConfig balance)
        {
            var player = state.Player;
            player.AssaultTrauma = Math.Min(balance.MaxAssaultTrauma, player.AssaultTrauma + 1);
            player.TraumaCareProgress = 0;
            state.Flags.Add(FlagAssaulted);
        }

        /// <summary>낮 탐험 추가 스트레스.</summary>
        public static int DayExpeditionStress(GameState state, BalanceConfig balance)
        {
            return state.Player.AssaultTrauma * balance.DayTraumaStressPerLevel;
        }

        /// <summary>회복 진행 (주 단위). 주간 정산은 1, 돌봄 행동은 GameEffect.TraumaCare. 다 차면 1단계 회복.</summary>
        public static void Care(GameState state, int weeks, BalanceConfig balance)
        {
            var player = state.Player;
            if (player.AssaultTrauma <= 0 || weeks <= 0)
            {
                return;
            }

            player.TraumaCareProgress += weeks;
            var need = Math.Max(1, balance.TraumaRecoveryWeeks);
            while (player.AssaultTrauma > 0 && player.TraumaCareProgress >= need)
            {
                player.AssaultTrauma--;
                player.TraumaCareProgress -= need;
            }

            if (player.AssaultTrauma == 0)
            {
                player.TraumaCareProgress = 0;
            }
        }
    }
}
