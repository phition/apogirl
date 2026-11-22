using System;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>
    /// 이번 주 배분 (기획 D.3·D.4): 한 주 실행 직전에 동료·선생님에게 음식·마실 것·의약품·기호품을 준다.
    /// 기본 식사와 별도인 추가 관리다. 한 사람에게 종류별 주 1회.
    /// </summary>
    public static class GiveRules
    {
        public const string TeacherId = EffectApplier.TeacherNpcId;

        /// <summary>배분 받을 수 있는 사람: 선생님(창고 문을 연 뒤, 살아 있을 때) + 합류한 동료.</summary>
        public static List<string> Recipients(GameState state, BalanceConfig balance)
        {
            var result = new List<string>();
            if (state.Teacher.Alive && !string.IsNullOrEmpty(balance.OpeningEventId) && state.Events.Records.ContainsKey(balance.OpeningEventId))
            {
                result.Add(TeacherId);
            }

            // 고양이는 맨 아래
            result.AddRange(state.Npcs.Where(n => n.Value.Status == NpcStatus.Joined).Select(n => n.Key)
                .OrderBy(k => CatRules.IsCat(k)).ThenBy(k => k, StringComparer.Ordinal));
            return result;
        }

        public static BodyState Body(GameState state, string id)
        {
            if (id == TeacherId)
            {
                return state.Teacher.Body ?? (state.Teacher.Body = new BodyState());
            }

            if (!state.Npcs.TryGetValue(id ?? "", out var npc))
            {
                return null;
            }

            return npc.Body ?? (npc.Body = new BodyState());
        }

        /// <summary>그 사람의 기록 (배분 kind:/item:, 관계 C.5 키). 세이브에 남는다.</summary>
        public static Dictionary<string, int> Log(GameState state, string id)
        {
            if (id == TeacherId)
            {
                return state.Teacher.GiftLog ?? (state.Teacher.GiftLog = new Dictionary<string, int>());
            }

            var npc = state.Npcs[id];
            return npc.GiftLog ?? (npc.GiftLog = new Dictionary<string, int>());
        }

        /// <summary>그 사람이 좋아하는 물건인가.</summary>
        public static bool Likes(BalanceConfig balance, string id, string itemId)
        {
            return balance.LikedItems != null && balance.LikedItems.Any(l => l.NpcId == id && l.Items != null && l.Items.Contains(itemId));
        }

        /// <summary>기호품 칸에 고를 수 있는 물건 (좋아하는 물건 표에 있는 것), 창고에 있는 것만.</summary>
        public static List<string> GiftCandidates(GameState state, BalanceConfig balance)
        {
            var all = new HashSet<string>(balance.LikedItems?.SelectMany(l => l.Items ?? Array.Empty<string>()) ?? Enumerable.Empty<string>());
            return state.Storage.Where(s => s.Count > 0 && all.Contains(s.ItemId)).Select(s => s.ItemId).Distinct().ToList();
        }

        /// <summary>줄 수 없는 이유 (UI 키), 줄 수 있으면 null.</summary>
        public static string Blocker(GameState state, BalanceConfig balance, string id, GiveKind kind, string itemId = null)
        {
            if (!Recipients(state, balance).Contains(id))
            {
                return "give.blocked.absent";
            }

            if (Log(state, id).TryGetValue("kind:" + kind, out var week) && week == state.Calendar.Week)
            {
                return "give.blocked.used";
            }

            switch (kind)
            {
                case GiveKind.Food:
                    return state.Base.GetResource(ResourceType.Food) > 0 ? null : "give.blocked.no_food";
                case GiveKind.Water:
                    return state.Base.GetResource(ResourceType.Water) > 0 ? null : "give.blocked.no_water";
                case GiveKind.Medicine:
                    return state.Base.GetResource(ResourceType.Medicine) > 0 ? null : "give.blocked.no_medicine";
                default:
                    return !string.IsNullOrEmpty(itemId) && GiftCandidates(state, balance).Contains(itemId) ? null : "give.blocked.no_item";
            }
        }

        /// <summary>준다. 줄 수 없으면 false. liked = 좋아하는 물건이었는가.</summary>
        public static bool Give(GameState state, BalanceConfig balance, string id, GiveKind kind, string itemId, StateDelta delta, out bool liked)
        {
            liked = false;
            if (Blocker(state, balance, id, kind, itemId) != null)
            {
                return false;
            }

            var body = Body(state, id);
            var log = Log(state, id);
            var affinity = balance.GiveAffinityBasic;
            switch (kind)
            {
                case GiveKind.Food:
                    EffectApplier.ChangeResource(state, ResourceType.Food, -1, delta);
                    body.Satiety = ClampSigned(body.Satiety + balance.GiveFoodSatiety, balance);
                    body.Stress = ClampStress(body.Stress + balance.GiveFoodStress, balance);
                    break;
                case GiveKind.Water:
                    EffectApplier.ChangeResource(state, ResourceType.Water, -1, delta);
                    body.Hydration = ClampSigned(body.Hydration + balance.GiveWaterHydration, balance);
                    body.Stress = ClampStress(body.Stress + balance.GiveWaterStress, balance);
                    break;
                case GiveKind.Medicine:
                    EffectApplier.ChangeResource(state, ResourceType.Medicine, -1, delta);
                    body.Stress = ClampStress(body.Stress + balance.GiveMedicineStress, balance);
                    if (id != TeacherId && state.Npcs[id].MinorWoundWeeks > 0)
                    {
                        state.Npcs[id].MinorWoundWeeks = 0;   // 동료 탐험의 가벼운 상처는 의약품이면 바로 낫는다
                    }

                    if (id != TeacherId && state.Npcs[id].InjuredWeeks > 0)
                    {
                        var npc = state.Npcs[id];
                        npc.InjuredWeeks = Math.Max(0, npc.InjuredWeeks - balance.GiveMedicineInjuryWeeks);
                        affinity = balance.GiveAffinityMedicineInjured;
                    }

                    break;
                default:
                    LoadoutRules.RemoveFromStorage(state, itemId, 1, delta);
                    liked = Likes(balance, id, itemId);
                    var repeat = log.TryGetValue("item:" + itemId, out var last) && state.Calendar.Week - last < balance.GiftRepeatWeeks;
                    affinity = liked ? balance.GiveAffinityLiked : balance.GiveAffinityBasic;
                    var stress = liked ? balance.GiveLikedStress : balance.GiveOtherGiftStress;
                    if (repeat)
                    {
                        affinity /= 2;
                        stress /= 2;
                    }

                    body.Stress = ClampStress(body.Stress + stress, balance);
                    log["item:" + itemId] = state.Calendar.Week;
                    break;
            }

            log["kind:" + kind] = state.Calendar.Week;
            log[BondDecayRules.CareKey] = state.Calendar.Week;
            affinity = BondDecayRules.ReconcileBonus(state, id, affinity); // C.5: 서먹해진 뒤 처음 챙기면 배로
            if (affinity != 0)
            {
                EffectApplier.ChangeNpc(state, new NpcChange { NpcId = id, Affinity = affinity }, balance, delta);
            }

            return true;
        }

        /// <summary>하루 정산 (UpkeepRules.ApplyDaily): 포만·수분이 줄고, 공용 식사를 주인공이 먹은 비율만큼 채운다. 단계별 스트레스.</summary>
        public static void ApplyDaily(GameState state, BalanceConfig balance, double foodShare, double waterShare)
        {
            foreach (var id in Recipients(state, balance))
            {
                var body = Body(state, id);
                body.Satiety = Nourish(body.Satiety, balance.CompanionSatietyDailyDrain, balance.CompanionMealSatiety, foodShare, balance);
                body.Hydration = Nourish(body.Hydration, balance.CompanionHydrationDailyDrain, balance.CompanionDrinkHydration, waterShare, balance);
                var stress = (UpkeepRules.Band(balance.SatietyBands, body.Satiety)?.Stress ?? 0)
                             + (UpkeepRules.Band(balance.HydrationBands, body.Hydration)?.Stress ?? 0);
                if (id != TeacherId && state.Npcs[id].InjuredWeeks > 0)
                {
                    stress += balance.CompanionInjuredDailyStress;
                }

                if (state.Base.Weather == WeatherType.Cold)
                {
                    stress += balance.ColdDailyStress;
                }

                body.Stress = ClampStress(body.Stress + stress, balance);
            }
        }

        /// <summary>주간 정산: 스트레스가 조금 풀린다.</summary>
        public static void ApplyWeekly(GameState state, BalanceConfig balance)
        {
            foreach (var id in Recipients(state, balance))
            {
                var body = Body(state, id);
                body.Stress = ClampStress(body.Stress - balance.CompanionWeeklyStressRecovery, balance);
            }
        }

        /// <summary>스트레스가 높아 역할 효과가 줄어드는가.</summary>
        public static bool IsStressed(NpcState npc, BalanceConfig balance) =>
            npc?.Body != null && npc.Body.Stress >= balance.CompanionStressRoleThreshold;

        private static int Nourish(int value, int drain, int restore, double share, BalanceConfig balance)
        {
            value = ClampSigned(value - drain, balance);
            if (value < 0 && share > 0)
            {
                var gain = (int)Math.Round(restore * Math.Min(1.0, share), MidpointRounding.AwayFromZero);
                value = Math.Min(0, value + gain);
            }

            return value;
        }

        private static int ClampSigned(int value, BalanceConfig balance) => Math.Max(-balance.VitalMax, Math.Min(balance.VitalMax, value));

        private static int ClampStress(int value, BalanceConfig balance) => Math.Max(0, Math.Min(balance.VitalMax, value));
    }
}
