using System;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>
    /// 교문 허수아비 (제작 P6, Docs/CRAFTING_PROPOSAL.md 3.7절): 마대·빗자루·양동이 머리로 세운 거점 설비.
    /// 밤 습격의 세기만 줄인다 (오는 횟수는 그대로). 막은 습격마다 내구 −1, 뚫리면 쓰러진다(0).
    /// 사람(낮 침입)에게는 효과가 없다 — 낮 침입 규칙은 이것을 읽지 않는다.
    /// </summary>
    public static class ScarecrowRules
    {
        public static bool IsStanding(GameState state) => state.Base.Scarecrow > 0;

        public static bool IsBell(GameState state, BalanceConfig balance) =>
            IsStanding(state) && !string.IsNullOrEmpty(balance.ScarecrowBellFlag) && state.Flags.Contains(balance.ScarecrowBellFlag);

        /// <summary>지금 밤 습격 세기에서 빼는 값 (쓰러져 있으면 0).</summary>
        public static int Relief(GameState state, BalanceConfig balance)
        {
            if (!IsStanding(state))
            {
                return 0;
            }

            return IsBell(state, balance) ? balance.ScarecrowBellRaidRelief : balance.ScarecrowRaidRelief;
        }

        /// <summary>뚫렸을 때 식량 피해 배율 (울리는 허수아비가 서 있으면 깡통 경보와 함께 울려 덜 잃는다).</summary>
        public static float BreachFoodScale(GameState state, BalanceConfig balance) =>
            IsBell(state, balance) ? balance.ScarecrowBellBreachFoodScale : 1f;

        /// <summary>제작이 끝나 깃발을 켠 뒤: 허수아비 레시피면 내구를 채운다 (울리는 것은 바꿔 세운다).</summary>
        public static void OnCrafted(GameState state, CraftDef craft, BalanceConfig balance)
        {
            if (craft?.SetFlags == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(balance.ScarecrowBellFlag) && craft.SetFlags.Contains(balance.ScarecrowBellFlag))
            {
                state.Base.Scarecrow = balance.ScarecrowBellDurability;
            }
            else if (!string.IsNullOrEmpty(balance.ScarecrowFlag) && craft.SetFlags.Contains(balance.ScarecrowFlag))
            {
                state.Base.Scarecrow = balance.ScarecrowDurability;
            }
        }

        /// <summary>습격이 끝난 뒤 내구를 줄인다. 0이 되면 두 깃발을 끄고 쓰러졌다고 표시한다 (다시 만들 수 있다).</summary>
        public static void AfterRaid(GameState state, RaidResult result, BalanceConfig balance, StateDelta delta)
        {
            if (result.ScarecrowRelief <= 0 || !IsStanding(state))
            {
                return;
            }

            state.Base.Scarecrow = result.Repelled ? Math.Max(0, state.Base.Scarecrow - 1) : 0;
            if (state.Base.Scarecrow > 0)
            {
                return;
            }

            result.ScarecrowFell = true;
            foreach (var flag in new[] { balance.ScarecrowFlag, balance.ScarecrowBellFlag })
            {
                if (!string.IsNullOrEmpty(flag) && state.Flags.Remove(flag))
                {
                    delta.FlagsCleared.Add(flag);
                }
            }
        }
    }
}
