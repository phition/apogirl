using System;
using System.IO;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Random;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using ApoGirl.Exploration.Model;
using ApoGirl.Systems.Save;
using ApoGirl.Systems.Session;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    public sealed class RunClockTests
    {
        [Test]
        public void DayClock_PhasesAndEnd()
        {
            var clock = new RunClock(100f, ExpeditionTime.Day);
            Assert.AreEqual(ClockPhase.Morning, clock.Phase);
            clock.Tick(40f);
            Assert.AreEqual(ClockPhase.Afternoon, clock.Phase);
            clock.Tick(40f);
            Assert.AreEqual(ClockPhase.Dusk, clock.Phase);
            Assert.IsFalse(clock.IsOver);
            clock.Tick(500f);
            Assert.IsTrue(clock.IsOver);
            Assert.AreEqual(0f, clock.Remaining);
            Assert.AreEqual(1f, clock.Normalized);
        }

        [Test]
        public void NightClock_Phases()
        {
            var clock = new RunClock(100f, ExpeditionTime.Night);
            Assert.AreEqual(ClockPhase.Evening, clock.Phase);
            clock.Tick(30f);
            Assert.AreEqual(ClockPhase.Midnight, clock.Phase);
            clock.Tick(45f);
            Assert.AreEqual(ClockPhase.Dawn, clock.Phase);
        }
    }

    public sealed class BackpackGridTests
    {
        [Test]
        public void AutoPlace_FillsFirstFit_AndRejectsWhenFull()
        {
            var grid = new BackpackGrid(3, 2);
            var big = grid.TryAutoPlace("A", 2, 2, 1f);
            Assert.AreEqual((0, 0), (big.X, big.Y));
            var tall = grid.TryAutoPlace("B", 1, 2, 0.5f);
            Assert.AreEqual((2, 0), (tall.X, tall.Y));
            Assert.IsNull(grid.TryAutoPlace("C", 1, 1, 0.1f));
            Assert.AreEqual(1.5f, grid.TotalWeight, 1e-5f);
        }

        [Test]
        public void Pocket_FitsTutorialBat()
        {
            var balance = new ApoGirl.Core.Definitions.BalanceConfig();
            var pocket = new BackpackGrid(balance.PocketWidth, balance.PocketHeight);
            Assert.IsNotNull(pocket.TryAutoPlace("ITEM_WEAPON_BAT", 1, 3, 1f), "배낭 없이도 튜토리얼 창고의 야구방망이(1×3)를 주울 수 있다");
        }

        [Test]
        public void Move_RespectsBoundsAndOverlap()
        {
            var grid = new BackpackGrid(4, 2);
            var a = grid.TryAutoPlace("A", 2, 1, 0f);
            var b = grid.TryAutoPlace("B", 1, 1, 0f);
            Assert.IsFalse(grid.TryMove(b, 1, 0), "겹침");
            Assert.IsFalse(grid.TryMove(a, 3, 0), "범위 밖");
            Assert.IsTrue(grid.TryMove(a, 2, 1));
            Assert.AreSame(a, grid.ItemAt(3, 1));
            Assert.IsTrue(grid.Remove(b));
            Assert.IsNull(grid.ItemAt(2, 0));
        }
    }

    public sealed class LootAndResultTests
    {
        [Test]
        public void LootRoll_IsDeterministicPerSeed()
        {
            var table = TestContent.CreateLootTable();
            for (var seed = 0; seed < 50; seed++)
            {
                var a = LootRoller.Roll(table, new SeededRandom(SeededRandom.CreateState(seed)));
                var b = LootRoller.Roll(table, new SeededRandom(SeededRandom.CreateState(seed)));
                CollectionAssert.AreEqual(a.Select(i => i.ItemId), b.Select(i => i.ItemId));
                Assert.That(a.Count, Is.LessThanOrEqualTo(table.MaxRolls));
            }

            Assert.AreNotEqual(ExplorationRules.ContainerSeed(7, 0), ExplorationRules.ContainerSeed(7, 1));
        }

        [Test]
        public void LootRoll_EmptyChanceAndNoEntries()
        {
            var always = new LootTableDef { EmptyChance = 1f, Entries = { new LootEntry("X", 1) } };
            Assert.IsEmpty(LootRoller.Roll(always, new FixedRandom(0.5)));
            Assert.IsEmpty(LootRoller.Roll(new LootTableDef(), new FixedRandom(0.5)));
        }

        [Test]
        public void Result_ConvertsResources_AndKeepsOtherItems()
        {
            var db = TestContent.CreateDatabase();
            var grid = new BackpackGrid(6, 4);
            grid.TryAutoPlace("ITEM_CANNED_FOOD", 1, 1, 0.4f);
            grid.TryAutoPlace("ITEM_CANNED_FOOD", 1, 1, 0.4f);
            grid.TryAutoPlace("ITEM_WATER_BOTTLE", 1, 2, 0.6f);
            grid.TryAutoPlace("ITEM_TEXTBOOK", 2, 1, 0.8f);

            var result = RunResultBuilder.Build(ExtractStatus.Extracted, grid.Items, db, 15, 6, 0, 120f);
            Assert.AreEqual(4, result.ResourceLoot.First(r => r.Resource == ResourceType.Food).Amount);
            Assert.AreEqual(2, result.ResourceLoot.First(r => r.Resource == ResourceType.Water).Amount);
            Assert.AreEqual("ITEM_TEXTBOOK", result.Loot.Single().ItemId);
            Assert.AreEqual(15f, result.FatigueDelta);
            Assert.AreEqual(6f, result.StressDelta);
        }

        [Test]
        public void Result_DownLosesBackpack_StrandedAddsPenalty()
        {
            var db = TestContent.CreateDatabase();
            var grid = new BackpackGrid(6, 4);
            grid.TryAutoPlace("ITEM_CANNED_FOOD", 1, 1, 0.4f);

            var down = RunResultBuilder.Build(ExtractStatus.Down, grid.Items, db, 15, 6, -30, 10f);
            Assert.IsEmpty(down.ResourceLoot);
            Assert.IsEmpty(down.Loot);
            Assert.AreEqual(-30, down.HpDelta);

            var stranded = RunResultBuilder.Build(ExtractStatus.Stranded, grid.Items, db, 15, 6, 0, 10f);
            Assert.AreEqual(15f + db.Balance.StrandedFatigue, stranded.FatigueDelta);
            Assert.AreEqual(6f + db.Balance.StrandedStress, stranded.StressDelta);
            Assert.AreEqual(2, stranded.ResourceLoot.Single().Amount, "시간 초과여도 가져온 물건은 남는다 (M2b-1 임시 규칙)");
        }

        [Test]
        public void Rules_CarryCapacityAndSearchSpeed()
        {
            var balance = new BalanceConfig();
            var stats = new System.Collections.Generic.Dictionary<StatType, int> { { StatType.Strength, 20 }, { StatType.Craft, 50 } };
            Assert.AreEqual(10f, ExplorationRules.CarryCapacity(stats, balance), 1e-5f);
            Assert.AreEqual(2.55f * 0.7f, ExplorationRules.SearchSeconds(3f, stats, balance), 1e-5f);
            stats[StatType.Craft] = 999;
            Assert.AreEqual(2.1f * 0.7f, ExplorationRules.SearchSeconds(3f, stats, balance), 1e-5f);
            Assert.Less(ExplorationRules.MoveSpeed(true, false, true, balance), ExplorationRules.MoveSpeed(true, false, false, balance));
        }
    }

    public sealed class StaminaHideRulesTests
    {
        [Test]
        public void StressSlowsSearch_OnlyAboveThreshold()
        {
            var balance = new BalanceConfig();
            Assert.AreEqual(1f, ExplorationRules.StressSearchFactor(40, balance), 1e-5f);
            Assert.AreEqual(1f, ExplorationRules.StressSearchFactor(60, balance), 1e-5f);
            Assert.AreEqual(1.25f, ExplorationRules.StressSearchFactor(80, balance), 1e-5f);
            Assert.AreEqual(1.5f, ExplorationRules.StressSearchFactor(100, balance), 1e-5f);
            Assert.AreEqual(1.5f, ExplorationRules.StressSearchFactor(150, balance), 1e-5f);
        }

        [Test]
        public void HideSeconds_ShrinksSteadilyWithAgility()
        {
            var balance = new BalanceConfig();
            var stats = new System.Collections.Generic.Dictionary<StatType, int> { { StatType.Agility, 0 } };
            Assert.AreEqual(1.5f, ExplorationRules.HideSeconds(stats, balance), 1e-5f);
            var last = 1.5f;
            foreach (var agility in new[] { 10, 30, 60, 100 })
            {
                stats[StatType.Agility] = agility;
                var seconds = ExplorationRules.HideSeconds(stats, balance);
                Assert.Less(seconds, last, "민첩이 오를수록 짧아진다");
                last = seconds;
            }

            stats[StatType.Agility] = 100000;
            Assert.AreEqual(balance.HideMinSeconds, ExplorationRules.HideSeconds(stats, balance), 1e-5f);
        }

        [Test]
        public void StaminaCap_FatigueAndInjuryLowerIt()
        {
            var balance = new BalanceConfig();
            var stats = new System.Collections.Generic.Dictionary<StatType, int> { { StatType.Stamina, 40 } };
            var max = ExplorationRules.StaminaMax(stats, balance);
            Assert.AreEqual(120f, max, 1e-5f);
            Assert.AreEqual(max, ExplorationRules.StaminaCap(max, 0, false, balance), 1e-5f);
            Assert.AreEqual(max * 0.8f, ExplorationRules.StaminaCap(max, 50, false, balance), 1e-4f);
            Assert.AreEqual(max * 0.55f, ExplorationRules.StaminaCap(max, 50, true, balance), 1e-4f);
            Assert.AreEqual(max * 0.35f, ExplorationRules.StaminaCap(max, 100, true, balance), 1e-4f);
            balance.StaminaInjuryPenalty = 0.5f;
            Assert.AreEqual(max * balance.StaminaMinCapRatio, ExplorationRules.StaminaCap(max, 100, true, balance), 1e-4f, "아무리 지쳐도 최소 비율은 남는다");
        }

        [Test]
        public void HideSleep_NeedsSleepDebt()
        {
            var balance = new BalanceConfig();
            Assert.AreEqual(0f, ExplorationRules.HideSleepChancePerSecond(1, balance));
            Assert.Greater(ExplorationRules.HideSleepChancePerSecond(2, balance), 0f);
            Assert.Greater(ExplorationRules.HideSleepChancePerSecond(4, balance), ExplorationRules.HideSleepChancePerSecond(2, balance));
        }
    }

    public sealed class DeferredExpeditionTests
    {
        [Test]
        public void DeferredSlot_MatchesImmediateResolution()
        {
            var db = TestContent.CreateDatabase();
            var result = new ExpeditionResult
            {
                Status = ExtractStatus.Extracted,
                FatigueDelta = 15,
                StressDelta = 4,
                ResourceLoot = { new ResourceAmount(ResourceType.Food, 3) },
            };

            var immediate = TestContent.NewGame(db, 11);
            var stub = new StubExpeditionRunner { Handler = _ => result };
            var runner = new WeekRunner(immediate, db, new SeededRandom(immediate.Rng), stub);
            immediate.Plan.Slots[0].ActionId = TestContent.DayExpedition;
            runner.PrepareWeek();
            runner.ExecuteNextSlot();

            var deferred = TestContent.NewGame(db, 11);
            var deferredRunner = new WeekRunner(deferred, db, new SeededRandom(deferred.Rng), new StubExpeditionRunner()) { DeferExpeditions = true };
            deferred.Plan.Slots[0].ActionId = TestContent.DayExpedition;
            deferredRunner.PrepareWeek();
            var pending = deferredRunner.ExecuteNextSlot();

            Assert.IsTrue(pending.AwaitingExpedition);
            Assert.IsTrue(deferredRunner.HasPendingExpedition);
            Assert.AreEqual(1, deferred.Calendar.Day);
            Assert.AreEqual(DayPhase.Day, deferred.Calendar.Phase, "결과 전에는 시간이 흐르지 않음");
            Assert.Throws<InvalidOperationException>(() => deferredRunner.ExecuteNextSlot());
            Assert.AreEqual(stub.Requests[0].Seed, pending.ExpeditionRequest.Seed, "같은 시드로 요청");

            var completed = deferredRunner.CompleteExpedition(result);
            Assert.IsFalse(completed.AwaitingExpedition);
            Assert.AreEqual(DayPhase.Night, deferred.Calendar.Phase);

            var serializer = new GameStateSerializer();
            Assert.AreEqual(serializer.Serialize(immediate), serializer.Serialize(deferred));
        }

        [Test]
        public void Session_RealtimeFlow_PendsThenCompletes()
        {
            var directory = Path.Combine(Path.GetTempPath(), "ApoGirlRealtimeTest_" + Guid.NewGuid().ToString("N"));
            try
            {
                var session = new GameSession(TestContent.CreateDatabase(), new SaveService(directory, new GameStateSerializer()))
                {
                    UseRealtimeExpeditions = true,
                };
                ExpeditionRequest started = null;
                session.ExpeditionStarted += r => started = r;
                session.NewGame(4);
                session.SetSlot(0, TestContent.DayExpedition);
                session.BeginWeek();

                var outcome = session.Step();
                Assert.IsTrue(outcome.AwaitingExpedition);
                Assert.AreSame(outcome.ExpeditionRequest, started);
                Assert.AreSame(started, session.PendingExpedition);
                Assert.AreEqual(TestContent.Location, started.LocationId);
                Assert.Throws<InvalidOperationException>(() => session.Step());

                var done = session.CompleteExpedition(new ExpeditionResult
                {
                    Status = ExtractStatus.Extracted,
                    Loot = { new ItemStack { ItemId = "ITEM_TEXTBOOK", Count = 1 } },
                });
                Assert.IsTrue(done.StopForPresentation);
                Assert.IsNull(session.PendingExpedition);
                Assert.AreEqual("ITEM_TEXTBOOK", session.State.Storage.Single().ItemId);

                while (session.IsExecuting)
                {
                    session.Step();
                }

                Assert.AreEqual(2, session.State.Calendar.Week);
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }
    }
}
