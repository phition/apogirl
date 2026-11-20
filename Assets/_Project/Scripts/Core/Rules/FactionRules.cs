using System;
using System.Collections.Generic;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>
    /// 외부 세력 (2~4막 보강, Docs/ACTS_EXPANSION_PLAN.md): 평판 −100~+100, 단계(적대·경계·중립·호의·동맹),
    /// 단계별 영향(습격·침입·적대적 생존자 배율, 매주 자원·스트레스), 거래, 접촉이 끊기면 멀어짐.
    /// 영향은 알게 된 세력만 적용한다.
    /// </summary>
    public static class FactionRules
    {
        public const int MinStanding = -100;
        public const int MaxStanding = 100;

        /// <summary>단계 경계: 적대 ≤ −60 &lt; 경계 ≤ −20 &lt; 중립 &lt; 30 ≤ 호의 &lt; 60 ≤ 동맹.</summary>
        public const int HostileMax = -60;
        public const int WaryMax = -20;
        public const int FriendlyMin = 30;
        public const int AlliedMin = 60;

        public const int LogSize = 3;

        public static FactionTier TierOf(int standing)
        {
            if (standing <= HostileMax) return FactionTier.Hostile;
            if (standing <= WaryMax) return FactionTier.Wary;
            if (standing >= AlliedMin) return FactionTier.Allied;
            if (standing >= FriendlyMin) return FactionTier.Friendly;
            return FactionTier.Neutral;
        }

        public static bool TryGetDef(IDefinitionDatabase db, string id, out FactionDef def)
        {
            def = null;
            var list = db?.Extras?.Factions;
            if (list == null || string.IsNullOrEmpty(id))
            {
                return false;
            }

            foreach (var candidate in list)
            {
                if (candidate.Id == id)
                {
                    def = candidate;
                    return true;
                }
            }

            return false;
        }

        /// <summary>정의된 세력 중 상태가 없는 것을 처음 평판으로 채운다 (새 게임·불러오기·v6 마이그레이션 뒤).</summary>
        public static void EnsureInitialized(GameState state, IDefinitionDatabase db)
        {
            var list = db?.Extras?.Factions;
            if (state == null || list == null)
            {
                return;
            }

            foreach (var def in list)
            {
                if (!string.IsNullOrEmpty(def.Id) && !state.Factions.ContainsKey(def.Id))
                {
                    state.Factions[def.Id] = new FactionState { Standing = Clamp(def.StartStanding) };
                }
            }
        }

        /// <summary>떠난 세력 (GoneFlag가 켜짐).</summary>
        public static bool IsGone(GameState state, FactionDef def) =>
            def != null && !string.IsNullOrEmpty(def.GoneFlag) && state.Flags.Contains(def.GoneFlag);

        public static FactionState Get(GameState state, string id)
        {
            if (!state.Factions.TryGetValue(id, out var faction))
            {
                faction = new FactionState();
                state.Factions[id] = faction;
            }

            return faction;
        }

        public static void Change(GameState state, FactionChange change, StateDelta delta)
        {
            if (change == null || string.IsNullOrEmpty(change.FactionId))
            {
                return;
            }

            Change(state, change.FactionId, change.Standing, change.Reveal, change.LogKey, delta);
        }

        public static void Change(GameState state, string factionId, int standing, bool reveal, string logKey, StateDelta delta)
        {
            var faction = Get(state, factionId);
            if (reveal && !faction.Known)
            {
                faction.Known = true;
                delta?.FactionsRevealed.Add(factionId);
            }

            var before = faction.Standing;
            faction.Standing = Clamp(before + standing);
            var applied = faction.Standing - before;
            if (applied != 0)
            {
                delta?.AddFactionStanding(factionId, applied);
                if (TierOf(before) != TierOf(faction.Standing))
                {
                    delta?.FactionTierChanges.Add(factionId + ":" + TierOf(faction.Standing));
                }
            }

            if (applied != 0 || reveal || !string.IsNullOrEmpty(logKey))
            {
                faction.LastContactWeek = state.Calendar.Week;
            }

            if (!string.IsNullOrEmpty(logKey) || applied != 0)
            {
                faction.Log.Insert(0, new FactionLogEntry { Week = state.Calendar.Week, TextKey = logKey ?? "", Standing = applied });
                if (faction.Log.Count > LogSize)
                {
                    faction.Log.RemoveRange(LogSize, faction.Log.Count - LogSize);
                }
            }
        }

        /// <summary>지금 적용되는 영향 (알게 된 세력, 지금 단계에 맞는 것).</summary>
        public static IEnumerable<(FactionDef Faction, FactionEffectDef Effect)> ActiveEffects(GameState state, IDefinitionDatabase db)
        {
            var list = db?.Extras?.Factions;
            if (state == null || list == null)
            {
                yield break;
            }

            foreach (var def in list)
            {
                if (!state.Factions.TryGetValue(def.Id, out var faction) || !faction.Known || IsGone(state, def))
                {
                    continue;
                }

                var tier = TierOf(faction.Standing);
                foreach (var effect in def.Effects)
                {
                    if (tier >= effect.MinTier && tier <= effect.MaxTier)
                    {
                        yield return (def, effect);
                    }
                }
            }
        }

        public static float RaidChanceMultiplier(GameState state, IDefinitionDatabase db) => Product(state, db, e => e.RaidChanceMultiplier);

        public static float IntrusionMultiplier(GameState state, IDefinitionDatabase db) => Product(state, db, e => e.IntrusionMultiplier);

        public static float HostileMultiplier(GameState state, IDefinitionDatabase db) => Product(state, db, e => e.HostileMultiplier);

        public static int RaidStrengthBonus(GameState state, IDefinitionDatabase db)
        {
            var sum = 0;
            foreach (var (_, effect) in ActiveEffects(state, db))
            {
                sum += effect.RaidStrengthBonus;
            }

            return sum;
        }

        /// <summary>
        /// 주간 정산: 무전 거래 도착, 단계별 매주 자원·스트레스, 접촉이 끊긴 세력은 멀어진다.
        /// </summary>
        public static void ApplyWeekly(GameState state, IDefinitionDatabase db, BalanceConfig balance, StateDelta delta)
        {
            var list = db?.Extras?.Factions;
            if (state == null || list == null)
            {
                return;
            }

            foreach (var def in list)
            {
                if (!state.Factions.TryGetValue(def.Id, out var faction))
                {
                    continue;
                }

                foreach (var pending in faction.Pending)
                {
                    if (Enum.TryParse<ResourceType>(pending.Key, out var type) && pending.Value != 0)
                    {
                        EffectApplier.ChangeResource(state, type, pending.Value, delta);
                    }
                }

                faction.Pending.Clear();
                if (!faction.Known || IsGone(state, def))
                {
                    continue;
                }

                var tier = TierOf(faction.Standing);
                foreach (var effect in def.Effects)
                {
                    if (tier < effect.MinTier || tier > effect.MaxTier)
                    {
                        continue;
                    }

                    foreach (var resource in effect.WeeklyResources)
                    {
                        var amount = resource.Amount;
                        if (amount < 0)
                        {
                            amount = -Math.Min(-amount, state.Base.GetResource(resource.Resource));
                        }

                        if (amount != 0)
                        {
                            EffectApplier.ChangeResource(state, resource.Resource, amount, delta);
                        }
                    }

                    if (effect.WeeklyStress != 0)
                    {
                        EffectApplier.ChangeVital(state, VitalType.Stress, effect.WeeklyStress, balance, delta);
                    }
                }

                if (def.DriftAfterWeeks > 0 && def.WeeklyDrift > 0 && faction.Standing != def.DriftTarget
                    && state.Calendar.Week - faction.LastContactWeek >= def.DriftAfterWeeks)
                {
                    var step = Math.Min(def.WeeklyDrift, Math.Abs(def.DriftTarget - faction.Standing)) * Math.Sign(def.DriftTarget - faction.Standing);
                    var lastContact = faction.LastContactWeek;
                    Change(state, def.Id, step, false, "", delta);
                    faction.LastContactWeek = lastContact; // 멀어짐은 접촉이 아니다
                }
            }
        }

        /// <summary>새로 켜진 플래그에 반응한다 (탐험 조우 선택 등).</summary>
        public static void OnFlagsSet(GameState state, IDefinitionDatabase db, IEnumerable<string> flags, StateDelta delta)
        {
            var list = db?.Extras?.Factions;
            if (state == null || list == null || flags == null)
            {
                return;
            }

            foreach (var flag in flags)
            {
                foreach (var def in list)
                {
                    foreach (var reaction in def.FlagReactions)
                    {
                        if (reaction.Flag == flag)
                        {
                            Change(state, def.Id, reaction.Standing, reaction.Reveal, reaction.LogKey, delta);
                        }
                    }
                }
            }
        }

        /// <summary>거래할 수 없는 이유 (UI 키), 할 수 있으면 null.</summary>
        public static string TradeBlocker(GameState state, IDefinitionDatabase db, string factionId, string offerId)
        {
            if (!TryGetDef(db, factionId, out var def) || !state.Factions.TryGetValue(factionId, out var faction) || !faction.Known)
            {
                return "faction.trade.unknown";
            }

            if (IsGone(state, def))
            {
                return "faction.trade.gone";
            }

            var offer = def.Trades.Find(t => t.Id == offerId);
            if (offer == null)
            {
                return "faction.trade.unknown";
            }

            if (TierOf(faction.Standing) < offer.MinTier)
            {
                return "faction.trade.tier";
            }

            if (faction.LastTradeWeek > 0 && state.Calendar.Week - faction.LastTradeWeek < Math.Max(1, def.TradeIntervalWeeks))
            {
                return "faction.trade.weekly";
            }

            foreach (var give in offer.Give)
            {
                if (state.Base.GetResource(give.Resource) < give.Amount)
                {
                    return "faction.trade.short";
                }
            }

            return null;
        }

        public static bool Trade(GameState state, IDefinitionDatabase db, string factionId, string offerId, StateDelta delta)
        {
            if (TradeBlocker(state, db, factionId, offerId) != null)
            {
                return false;
            }

            TryGetDef(db, factionId, out var def);
            var offer = def.Trades.Find(t => t.Id == offerId);
            var faction = state.Factions[factionId];
            foreach (var give in offer.Give)
            {
                EffectApplier.ChangeResource(state, give.Resource, -give.Amount, delta);
            }

            foreach (var get in offer.Get)
            {
                if (offer.Delayed)
                {
                    var key = get.Resource.ToString();
                    faction.Pending.TryGetValue(key, out var current);
                    faction.Pending[key] = current + get.Amount;
                }
                else
                {
                    EffectApplier.ChangeResource(state, get.Resource, get.Amount, delta);
                }
            }

            faction.LastTradeWeek = state.Calendar.Week;
            Change(state, factionId, offer.StandingChange, false, "trade." + factionId + "." + offer.Id + ".log", delta);
            return true;
        }

        private static float Product(GameState state, IDefinitionDatabase db, Func<FactionEffectDef, float> pick)
        {
            var product = 1f;
            foreach (var (_, effect) in ActiveEffects(state, db))
            {
                product *= Math.Max(0f, pick(effect));
            }

            return product;
        }

        private static int Clamp(int value) => Math.Min(Math.Max(value, MinStanding), MaxStanding);
    }
}
