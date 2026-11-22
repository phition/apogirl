using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>
    /// 고양이 '급식' (확장 2): 사람 동료가 아니다. 동료 수·엔딩 조건·위기 이탈·대신 다침·역할·외출·좌석에 들지 않는다.
    /// 눌러앉으면 주마다 식량을 조금 먹고, 주인공이 지치면 무릎에 올라오고, 습격이 뚫리면 먼저 털을 세워 피해가 준다.
    /// 13장: 고양이는 다치거나 죽지 않는다.
    /// </summary>
    public static class CatRules
    {
        public const string CatId = "NPC_CAT";

        /// <summary>거처에 눌러앉았다 (합류 이벤트에서 켠다).</summary>
        public const string FlagHome = "CAT_HOME";

        public static bool IsCat(string id) => id == CatId;

        public static bool IsHome(GameState state) => state.Npcs.TryGetValue(CatId, out var cat) && cat.Status == NpcStatus.Joined;

        /// <summary>습격이 뚫렸을 때 피해 배율 (고양이가 있으면 먼저 털을 세워 경고한다, 거점을 비운 밤은 제외).</summary>
        public static float RaidDamageScale(GameState state, BalanceConfig balance, bool away) =>
            !away && IsHome(state) ? balance.CatRaidDamageScale : 1f;

        /// <summary>주 결산: 식량을 조금 먹고(없으면 알아서 쥐를 잡는다), 스트레스가 높으면 무릎에 올라온다. 무릎에 올라왔으면 true.</summary>
        public static bool ApplyWeekly(GameState state, BalanceConfig balance, StateDelta delta)
        {
            if (!IsHome(state))
            {
                return false;
            }

            var food = state.Base.GetResource(ResourceType.Food);
            if (balance.CatFoodPerWeek > 0 && food >= balance.CatFoodPerWeek)
            {
                EffectApplier.ChangeResource(state, ResourceType.Food, -balance.CatFoodPerWeek, delta);
            }

            if (balance.CatComfortStress > 0 && state.Player.Vitals.Stress >= balance.CatComfortStressMin)
            {
                EffectApplier.ChangeVital(state, VitalType.Stress, -balance.CatComfortStress, balance, delta);
                return true;
            }

            return false;
        }
    }
}
