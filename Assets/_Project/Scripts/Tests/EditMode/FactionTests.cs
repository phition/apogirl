using System.Collections.Generic;
using System.IO;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using ApoGirl.Systems.Content;
using ApoGirl.Systems.Save;
using NUnit.Framework;
using UnityEngine;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>세력 (2~4막 보강): 평판 단계·영향·거래·멀어짐·플래그 반응·세이브, 수학여행 밤 잠금·거점 비운 밤 습격.</summary>
    public sealed class FactionTests
    {
        private static InMemoryDefinitionDatabase ContentDb()
        {
            var root = Path.GetDirectoryName(Application.dataPath) ?? "";
            var content = ContentJson.LoadFolder(Path.Combine(root, ContentJson.ContentFolder));
            return new InMemoryDefinitionDatabase(
                new BalanceConfig(), content.Actions, content.ExpeditionModels, content.Items, content.LootTables,
                null, content.Enemies, content.Events, content.Npcs,
                new DefinitionExtras
                {
                    Traits = content.Traits, Facilities = content.Facilities, Lines = content.Lines, Prologue = content.Prologue ?? new PrologueDef(),
                    Endings = content.Endings, Roles = content.Roles, StartBonuses = content.StartBonuses, Factions = content.Factions,
                });
        }

        private static GameState NewState(IDefinitionDatabase db, int week = 20)
        {
            var state = NewGameFactory.Create(db.Balance, 1);
            state.Calendar.Week = week;
            FactionRules.EnsureInitialized(state, db);
            return state;
        }

        [Test]
        public void Tiers_FollowStandingThresholds()
        {
            Assert.AreEqual(FactionTier.Hostile, FactionRules.TierOf(-60));
            Assert.AreEqual(FactionTier.Wary, FactionRules.TierOf(-59));
            Assert.AreEqual(FactionTier.Wary, FactionRules.TierOf(-20));
            Assert.AreEqual(FactionTier.Neutral, FactionRules.TierOf(-19));
            Assert.AreEqual(FactionTier.Neutral, FactionRules.TierOf(29));
            Assert.AreEqual(FactionTier.Friendly, FactionRules.TierOf(30));
            Assert.AreEqual(FactionTier.Allied, FactionRules.TierOf(60));
        }

        [Test]
        public void ShippedFactions_StartAtDesignedStandings()
        {
            var db = ContentDb();
            var state = NewState(db);
            Assert.AreEqual(-40, state.Factions["FAC_SAGEORI"].Standing, "사거리 파: 경계");
            Assert.AreEqual(0, state.Factions["FAC_MART"].Standing, "더불어 마트: 중립");
            Assert.AreEqual(20, state.Factions["FAC_UBC"].Standing, "UBC: 중립 (교신하면 호의)");
            Assert.IsFalse(state.Factions.Values.Any(f => f.Known), "처음에는 모르는 세력");
        }

        [Test]
        public void Change_RevealsClampsLogsAndReportsTier()
        {
            var db = ContentDb();
            var state = NewState(db);
            var delta = new StateDelta();
            FactionRules.Change(state, new FactionChange { FactionId = "FAC_SAGEORI", Standing = -30, Reveal = true, LogKey = "x" }, delta);
            var faction = state.Factions["FAC_SAGEORI"];
            Assert.IsTrue(faction.Known);
            Assert.AreEqual(-70, faction.Standing);
            CollectionAssert.Contains(delta.FactionsRevealed, "FAC_SAGEORI");
            CollectionAssert.Contains(delta.FactionTierChanges, "FAC_SAGEORI:Hostile");
            Assert.AreEqual(-30, delta.FactionStanding["FAC_SAGEORI"]);

            FactionRules.Change(state, "FAC_SAGEORI", -100, false, "a", delta);
            Assert.AreEqual(FactionRules.MinStanding, faction.Standing, "−100에서 멈춘다");
            FactionRules.Change(state, "FAC_SAGEORI", 1, false, "b", delta);
            FactionRules.Change(state, "FAC_SAGEORI", 1, false, "c", delta);
            Assert.AreEqual(FactionRules.LogSize, faction.Log.Count, "최근 일은 3줄까지");
            Assert.AreEqual("c", faction.Log[0].TextKey, "새것이 앞");
        }

        [Test]
        public void Effects_ApplyOnlyWhenKnown()
        {
            var db = ContentDb();
            var state = NewState(db);
            state.Factions["FAC_SAGEORI"].Standing = -80;
            Assert.AreEqual(1f, FactionRules.RaidChanceMultiplier(state, db), 1e-5, "모르는 세력은 영향 없음");

            state.Factions["FAC_SAGEORI"].Known = true;
            Assert.AreEqual(1.25f, FactionRules.RaidChanceMultiplier(state, db), 1e-5);
            Assert.AreEqual(1.3f, FactionRules.IntrusionMultiplier(state, db), 1e-5);
            Assert.AreEqual(5, FactionRules.RaidStrengthBonus(state, db));

            // 휴전(중립 이상): 침입이 줄고 통행세
            state.Factions["FAC_SAGEORI"].Standing = 0;
            Assert.AreEqual(0.75f, FactionRules.IntrusionMultiplier(state, db), 1e-5);
            state.Base.SetResource(ResourceType.Food, 1);
            FactionRules.ApplyWeekly(state, db, db.Balance, new StateDelta());
            Assert.AreEqual(0, state.Base.GetResource(ResourceType.Food), "통행세는 있는 만큼만");

            var chanceBase = RaidRules.Chance(state, db.Balance);
            Assert.AreEqual(chanceBase * 0.9f, RaidRules.Chance(state, db.Balance, db), 1e-5, "습격 확률에 세력 배율");
        }

        [Test]
        public void Trade_ChecksTierWeekAndStock_DelayedArrivesNextWeek()
        {
            var db = ContentDb();
            var state = NewState(db);
            var mart = state.Factions["FAC_MART"];
            mart.Known = true;
            state.Base.SetResource(ResourceType.Medicine, 2);
            state.Base.SetResource(ResourceType.Food, 0);
            Assert.AreEqual("faction.trade.tier", FactionRules.TradeBlocker(state, db, "FAC_MART", "MEDICINE_FOOD"), "중립은 약품 거래 불가");

            mart.Standing = 30;
            Assert.IsTrue(FactionRules.Trade(state, db, "FAC_MART", "MEDICINE_FOOD", new StateDelta()));
            Assert.AreEqual(1, state.Base.GetResource(ResourceType.Medicine));
            Assert.AreEqual(5, state.Base.GetResource(ResourceType.Food));
            Assert.AreEqual(33, mart.Standing);
            Assert.AreEqual("faction.trade.weekly", FactionRules.TradeBlocker(state, db, "FAC_MART", "MEDICINE_FOOD"), "세력마다 주 1회");

            // UBC 무전 거래: 다음 주 정산 때 도착
            var ubc = state.Factions["FAC_UBC"];
            ubc.Known = true;
            ubc.Standing = 30;
            state.Base.SetResource(ResourceType.Fuel, 0);
            Assert.AreEqual("faction.trade.short", FactionRules.TradeBlocker(state, db, "FAC_UBC", "FUEL_MEDICINE"));
            state.Base.SetResource(ResourceType.Fuel, 1);
            Assert.IsTrue(FactionRules.Trade(state, db, "FAC_UBC", "FUEL_MEDICINE", new StateDelta()));
            Assert.AreEqual(1, state.Base.GetResource(ResourceType.Medicine), "아직 안 왔다");
            FactionRules.ApplyWeekly(state, db, db.Balance, new StateDelta());
            Assert.AreEqual(2, state.Base.GetResource(ResourceType.Medicine), "다음 주 도착");
        }

        [Test]
        public void Drift_PullsTowardTargetAfterSilence()
        {
            var db = ContentDb();
            var state = NewState(db, 30);
            var ubc = state.Factions["FAC_UBC"];
            ubc.Known = true;
            ubc.Standing = 30;
            ubc.LastContactWeek = 28;
            FactionRules.ApplyWeekly(state, db, db.Balance, new StateDelta());
            Assert.AreEqual(30, ubc.Standing, "4주가 안 지나면 그대로");

            ubc.LastContactWeek = 26;
            FactionRules.ApplyWeekly(state, db, db.Balance, new StateDelta());
            Assert.AreEqual(27, ubc.Standing, "연락이 끊기면 매주 3씩 식는다");
            Assert.AreEqual(26, ubc.LastContactWeek, "멀어짐은 접촉이 아니다");
        }

        [Test]
        public void EncounterFlags_ChangeStanding()
        {
            var db = ContentDb();
            var state = NewState(db);
            FactionRules.OnFlagsSet(state, db, new[] { "ENC_MART_GROUP_TALK" }, new StateDelta());
            Assert.IsTrue(state.Factions["FAC_MART"].Known);
            Assert.AreEqual(5, state.Factions["FAC_MART"].Standing);
        }

        [Test]
        public void SaveV5_MigratesAndKeepsFactions()
        {
            var db = ContentDb();
            var serializer = new GameStateSerializer();
            var state = NewState(db);
            state.Factions["FAC_MART"].Known = true;
            state.Factions["FAC_MART"].Standing = 42;
            var restored = serializer.Deserialize(serializer.Serialize(state));
            Assert.AreEqual(42, restored.Factions["FAC_MART"].Standing);
            Assert.IsTrue(restored.Factions["FAC_MART"].Known);

            // v5 세이브 (Factions 없음) → 빈 목록 → 게임이 처음 평판으로 채운다
            var jobject = Newtonsoft.Json.Linq.JObject.Parse(serializer.Serialize(state));
            jobject["SaveVersion"] = 5;
            jobject.Remove("Factions");
            var old = serializer.Deserialize(jobject.ToString());
            Assert.AreEqual(0, old.Factions.Count);
            FactionRules.EnsureInitialized(old, db);
            Assert.AreEqual(-40, old.Factions["FAC_SAGEORI"].Standing);
        }

        [Test]
        public void SchoolTrip_LocksNightAndAwayRaidSparesPlayer()
        {
            var db = ContentDb();
            var state = NewState(db, 31);
            state.Plan.Slots[0].ActionId = "ACT_SCHOOL_TRIP";
            var runner = new WeekRunner(state, db, new FixedRandom { Default = 0.99 }, new StubExpeditionRunner());
            runner.PrepareWeek();
            runner.ExecuteNextSlot();
            Assert.IsTrue(state.Flags.Contains("TRIP_DONE"));
            Assert.AreEqual("ACT_TRIP_NIGHT", state.Plan.Slots[1].ActionId, "그날 밤은 여행 중");
            Assert.IsTrue(state.Plan.Slots[1].Locked);

            // 거점을 비운 밤의 습격: 주인공은 다치지 않고, 방어는 약하다
            var hp = state.Player.Vitals.Hp;
            var result = RaidRules.Resolve(state, 999, 0, false, db.Balance, new FixedRandom(), new StateDelta(), true);
            Assert.IsFalse(result.Repelled);
            Assert.IsTrue(result.Away);
            Assert.AreEqual(hp, state.Player.Vitals.Hp);
            Assert.IsTrue(state.Flags.Contains(RaidRules.FlagWhileAway));
            var home = DefenseRules.Power(state, db.Balance, 0, new FixedRandom(), false);
            var away = DefenseRules.Power(state, db.Balance, 0, new FixedRandom(), false, true);
            Assert.Less(away, home);
        }
    }
}
