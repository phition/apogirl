using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Random;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>
    /// 논문 (사용자 요청 2026-11-26): 탐험에서 주워 귀환하면 창고에 넣지 않고 읽는다.
    /// 그 과목 수업(ItemDef.StudyActionId)을 BalanceConfig.PaperClassCount번 성공한 만큼 능력치(성장 체감 그대로)와
    /// 숙련 경험치가 오른다. 선생님 지도·연속 행동·시설 보정·피로·스트레스는 없다 (읽기만 한다).
    /// </summary>
    public static class PaperRules
    {
        /// <summary>논문이면 읽어 반영하고 true. 논문이 아니거나 수업 정의가 없으면 false (그대로 창고로).</summary>
        public static bool Read(GameState state, IDefinitionDatabase db, string itemId, StateDelta delta)
        {
            if (!db.TryGetItem(itemId, out var item) || !item.IsPaper || !db.TryGetAction(item.StudyActionId, out var action))
            {
                return false;
            }

            var balance = db.Balance;
            var player = state.Player;
            for (var n = 0; n < balance.PaperClassCount; n++)
            {
                foreach (var gain in action.StatGains)
                {
                    var amount = StatRules.ScaleGain(player.GetStat(gain.Stat), gain.Amount, 1f, balance);
                    EffectApplier.ChangeStat(state, gain.Stat, amount, balance, delta);
                }

                if (action.HasProficiency)
                {
                    ActionResolver.AddProficiencyXp(state, action.Proficiency, action.ProficiencyXp, balance, delta);
                }
            }

            if (!string.IsNullOrEmpty(item.ReadFlag) && state.Flags.Add(item.ReadFlag))
            {
                delta.FlagsSet.Add(item.ReadFlag);
            }

            return true;
        }

        /// <summary>그 물건이 지금 상자에서 나올 수 있는가 (ItemDef.LootFlag가 비었거나 켜져 있다). 모르는 물건은 그대로 둔다.</summary>
        public static bool CanLoot(IDefinitionDatabase db, string itemId, ICollection<string> flags)
        {
            return !db.TryGetItem(itemId, out var item) || string.IsNullOrEmpty(item.LootFlag) || (flags != null && flags.Contains(item.LootFlag));
        }

        /// <summary>
        /// 상자에서 굴려진 물건을 실제로 남기는가: LootFlag가 꺼졌으면 빼고, 지역 논문(LootFlag 있는 논문)은 PaperLootKeepChance로 거른다.
        /// seed는 판·상자마다 정해진 값 (같은 판 같은 상자면 같은 결과).
        /// </summary>
        public static bool KeepRolled(IDefinitionDatabase db, string itemId, ICollection<string> flags, int seed)
        {
            if (!CanLoot(db, itemId, flags))
            {
                return false;
            }

            if (!db.TryGetItem(itemId, out var item) || !item.IsPaper || string.IsNullOrEmpty(item.LootFlag))
            {
                return true;
            }

            return new SeededRandom(SeededRandom.CreateState(seed)).NextDouble() < db.Balance.PaperLootKeepChance;
        }

        /// <summary>상자 키의 고정 해시 (판 시드와 섞어 KeepRolled의 seed로).</summary>
        public static int KeyHash(string key)
        {
            unchecked
            {
                var hash = 17;
                foreach (var c in key ?? "")
                {
                    hash = hash * 31 + c;
                }

                return hash;
            }
        }

        /// <summary>그 논문이 가르치는 수업 (없으면 null).</summary>
        public static ActionDef StudyAction(IDefinitionDatabase db, string itemId) =>
            db.TryGetItem(itemId, out var item) && item.IsPaper && db.TryGetAction(item.StudyActionId, out var action) ? action : null;

        /// <summary>
        /// 낮에는 잠겼다가 밤(낮 탐험의 황혼 포함)에는 열리는 방: 낮 변형 LockedRooms 가운데 밤 변형에서 잠기지 않는 것.
        /// 밤에 쇠지렛대로만 열리는 방(ForceableRooms)도 든다.
        /// </summary>
        public static List<string> DuskRooms(LocationDef location)
        {
            if (location?.DayVariant == null)
            {
                return new List<string>();
            }

            var night = location.NightVariant ?? new LocationVariantDef();
            return location.DayVariant.LockedRooms.Where(r => !night.LockedRooms.Contains(r)).Distinct().ToList();
        }
    }
}
