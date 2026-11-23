using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Random;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using ApoGirl.Exploration.Model;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>적대적 생존자 (낮 탐험: 등장·성향·대치 결과·트라우마)와 교실동 상자 고갈.</summary>
    public sealed class M7HostileTests
    {
        private static ExpeditionRequest DayRequest(int week, int seed, ExpeditionTime time = ExpeditionTime.Day)
        {
            return new ExpeditionRequest { LocationId = TestContent.Location, Week = week, Seed = seed, TimeOfDay = time, Hp = 100 };
        }

        // ── 등장·성향 ──

        [Test]
        public void Roll_DayOnly_AfterStartWeek_RisesByAct_AndFollowsWeights()
        {
            var balance = new BalanceConfig();
            Assert.AreEqual(0, HostileRules.Roll(DayRequest(30, 1, ExpeditionTime.Night), null, balance), "밤에는 나오지 않는다");
            Assert.IsTrue(Enumerable.Range(1, 500).All(s => HostileRules.Roll(DayRequest(4, s), null, balance) == 0), "시작 주 이전에는 없다");
            var none = new LocationDef { DayVariant = new LocationVariantDef { HostileChanceMultiplier = 0f } };
            Assert.IsTrue(Enumerable.Range(1, 500).All(s => HostileRules.Roll(DayRequest(30, s), none, balance) == 0), "배율 0 지역(튜토리얼)은 없다");

            float Rate(int week) => Enumerable.Range(1, 4000).Count(s => HostileRules.Roll(DayRequest(week, s), null, balance) > 0) / 4000f;
            Assert.That(Rate(8), Is.InRange(0.07f, 0.13f), "1막 10%");
            Assert.That(Rate(45), Is.InRange(0.21f, 0.29f), "4막 25%");

            var picks = Enumerable.Range(1, 20000).Select(s => HostileRules.Roll(DayRequest(45, s), null, balance)).Where(t => t > 0).ToList();
            Assert.That(picks.Count(t => t == 1) / (float)picks.Count, Is.InRange(0.15f, 0.25f), "4막 1단계 20%");
            Assert.That(picks.Count(t => t == 3) / (float)picks.Count, Is.InRange(0.25f, 0.35f), "4막 3단계 30%");
            var early = Enumerable.Range(1, 20000).Select(s => HostileRules.Roll(DayRequest(8, s), null, balance)).Where(t => t > 0).ToList();
            Assert.That(early.Count(t => t == 1) / (float)early.Count, Is.InRange(0.49f, 0.61f), "1막 1단계 55%");

            // 수: 1막 1명, 4막 3명
            var request = DayRequest(45, 1);
            for (var s = 1; request.HostileTemperament == 0; s++)
            {
                request = DayRequest(45, s);
                HostileRules.Assign(request, null, balance);
            }

            Assert.AreEqual(3, request.HostileCount);
        }

        [Test]
        public void Apply_RobbedLosesLoot_BeatenAlsoLosesHpButSurvives()
        {
            var balance = new BalanceConfig();
            ExpeditionResult Loaded() => new ExpeditionResult
            {
                Status = ExtractStatus.Extracted, HpDelta = -10,
                Loot = { new ItemStack { ItemId = "ITEM_SCRAP", Count = 1 } },
                ResourceLoot = { new ResourceAmount(ResourceType.Food, 5) },
            };

            var evicted = Loaded();
            HostileRules.Apply(evicted, HostileOutcome.Evicted, 100, balance);
            Assert.AreEqual(1, evicted.Loot.Count, "쫓겨남은 가진 것을 지킨다");

            var robbed = Loaded();
            HostileRules.Apply(robbed, HostileOutcome.Robbed, 100, balance);
            Assert.IsEmpty(robbed.Loot);
            Assert.IsEmpty(robbed.ResourceLoot);
            Assert.AreEqual(-10, robbed.HpDelta);

            var beaten = Loaded();
            HostileRules.Apply(beaten, HostileOutcome.Beaten, 100, balance);
            Assert.IsEmpty(beaten.Loot);
            Assert.AreEqual(-10 - balance.BeatenHpLoss, beaten.HpDelta);

            var fragile = Loaded();
            HostileRules.Apply(fragile, HostileOutcome.Beaten, 25, balance);
            Assert.AreEqual(1, 25 + fragile.HpDelta, "구타로 죽지는 않는다 (체력 1은 남는다)");
        }

        // ── 거점 반영·트라우마 ──

        [Test]
        public void Beaten_LosesWeapon_GainsTrauma_ThatRaisesDayStressAndHeals()
        {
            var db = TestContent.CreateDatabase();
            var state = TestContent.NewGame(db);
            state.Storage.Add(new ItemStack { ItemId = "ITEM_CANNED_FOOD", Count = 2 });
            state.Storage.Add(new ItemStack { ItemId = "ITEM_SCRAP", Count = 1 });
            state.Loadout.MainWeapon = "ITEM_SCRAP";
            var stub = new StubExpeditionRunner
            {
                Handler = r => new ExpeditionResult { Status = ExtractStatus.Extracted, Hostile = HostileOutcome.Beaten, HpDelta = -40 },
            };

            state.Plan.Slots[2].ActionId = TestContent.DayExpedition;
            var runner = new WeekRunner(state, db, new SeededRandom(state.Rng), stub);
            runner.PrepareWeek();
            while (!runner.IsWeekComplete && !runner.IsGameOver) runner.ExecuteNextSlot();

            Assert.AreEqual(1, state.Player.AssaultTrauma);
            Assert.AreEqual(1, state.Exploration.HostileBeatings);
            Assert.AreEqual(2, LoadoutRules.CountInStorage(state, "ITEM_CANNED_FOOD"), "퀵슬롯이 없어 창고 소모품은 들고 가지 않는다");
            Assert.AreEqual(0, LoadoutRules.CountInStorage(state, "ITEM_SCRAP"), "구타는 무기까지");

            // 트라우마: 낮 탐험 스트레스 추가
            Assert.AreEqual(db.Balance.DayTraumaStressPerLevel, AssaultTraumaRules.DayExpeditionStress(state, db.Balance));

            // 회복: 6주 (이번 주 정산 포함) 또는 돌봄으로 앞당김
            runner.FinishWeek();
            Assert.AreEqual(1, state.Player.TraumaCareProgress);
            EffectApplier.Apply(state, new GameEffect { TraumaCare = 5 }, db.Balance, new StateDelta());
            Assert.AreEqual(0, state.Player.AssaultTrauma, "6주치가 차면 1단계 회복");
        }

        // ── 위협 시뮬레이션 ──

        private static (ThreatSimulation Sim, BalanceConfig Balance) Sim()
        {
            var balance = new BalanceConfig();
            var sim = new ThreatSimulation(balance, new SeededRandom(SeededRandom.CreateState(7)));
            sim.SetAreaWidth(0, 60f);
            return (sim, balance);
        }

        private static EnemyDef Survivor() => new EnemyDef
        {
            Id = "S", Kind = EnemyKind.Survivor, WalkSpeed = 1.4f, RunSpeed = 3.7f, SightRange = 12f, AwarenessRate = 2.2f,
            AttackRange = 0.8f, Toughness = 99, SeesHiddenWithin = 2.5f, CanBeDowned = false, Damage = 0,
        };

        [Test]
        public void Survivor_CatchesInsteadOfHurting_AndFindsHiddenNearby()
        {
            var (sim, _) = Sim();
            var unit = sim.AddZombie(Survivor(), 0, 10f);
            unit.Facing = 1;
            var player = new PlayerSnapshot { AreaId = 0, X = 14f };
            ZombieUnit caught = null;
            var damage = 0;
            for (var i = 0; i < 200 && caught == null; i++)
            {
                var events = sim.Step(0.05f, player, ActivityMods.Neutral);
                caught = events.CaughtBy;
                damage += events.Damage;
            }

            Assert.AreSame(unit, caught, "붙잡는다");
            Assert.AreEqual(0, damage, "피해 대신 대치");

            // 숨어도 경계 중인 생존자가 가까우면 찾아낸다 (좀비는 못 찾음)
            unit.State = ZombieState.Search;
            var hidden = new PlayerSnapshot { AreaId = 0, X = unit.X + 2f, Hidden = true };
            Assert.IsTrue(sim.CanSee(unit, hidden, ActivityMods.Neutral));
            var zombie = sim.AddZombie(new EnemyDef { Id = "Z", SightRange = 7f }, 0, unit.X);
            zombie.State = ZombieState.Search;
            Assert.IsFalse(sim.CanSee(zombie, hidden, ActivityMods.Neutral));
            hidden.X = unit.X + 4f;
            Assert.IsFalse(sim.CanSee(unit, hidden, ActivityMods.Neutral), "멀리 숨으면 안 보인다");
        }

        [Test]
        public void Survivor_IsNotDownedByWeapons_AndIgnoresDaytimeSluggishness()
        {
            var (sim, _) = Sim();
            var unit = sim.AddZombie(Survivor(), 0, 11f);
            var player = new PlayerSnapshot { AreaId = 0, X = 10f };
            for (var i = 0; i < 10; i++)
            {
                sim.Strike(player, 1, 50, 2f, 2f);
            }

            Assert.AreNotEqual(ZombieState.Down, unit.State, "사람은 기절만 한다");
            Assert.AreEqual(ZombieState.Stunned, unit.State);

            var sleepy = new ActivityMods(ActivityLevel.Alert, 0.3f, 0.3f, 0.5f, 1f);
            unit.State = ZombieState.Idle;
            unit.Facing = 1;
            var far = new PlayerSnapshot { AreaId = 0, X = 21f };
            Assert.IsTrue(sim.CanSee(unit, far, sleepy), "낮 활동 보정(시야 ×0.3)을 받지 않는다");
        }

        // ── 교실동 상자 고갈 ──

        [Test]
        public void Depleting_HeadlessYieldShrinksAsContainersRunOut_AndWeekRunnerRemembers()
        {
            var db = TestContent.CreateDatabase(b => b.BaseEmptyContainerRate = 0f);
            var model = db.GetExpeditionModel(TestContent.Location);
            model.Day.BaseDownChance = 0f;
            model.ContainersPerRun = 4;
            Assert.IsTrue(db.TryGetLocation(TestContent.Location, out var location));
            location.DepletingContainers = true;
            var all = HostileRules.AllContainerKeys(location);
            Assert.AreEqual(10, all.Count, "복도 1 + 방 3 × 3");

            var request = DayRequest(1, 3);
            var fresh = new HeadlessExpeditionRunner(db).Run(request);
            Assert.AreEqual(4, fresh.SearchedContainers.Count);

            request.LootedContainers.UnionWith(all.Take(8));
            var half = new HeadlessExpeditionRunner(db).Run(request);
            Assert.AreEqual(2, half.SearchedContainers.Count, "남은 상자 2개");
            Assert.IsTrue(half.SearchedContainers.All(k => !request.LootedContainers.Contains(k)));
            Assert.Less(half.ResourceLoot.Sum(r => r.Amount), fresh.ResourceLoot.Sum(r => r.Amount), "남은 상자 비율만큼 수확이 준다");

            request.LootedContainers.UnionWith(all);
            var empty = new HeadlessExpeditionRunner(db).Run(request);
            Assert.IsEmpty(empty.ResourceLoot, "다 뒤진 곳에서는 나오는 게 없다");

            // 거점: 뒤진 상자를 기록하고, 다음 요청에 실어 보낸다
            var state = TestContent.NewGame(db);
            state.Plan.Slots[2].ActionId = TestContent.DayExpedition;
            var runner = new WeekRunner(state, db, new SeededRandom(state.Rng), new HeadlessExpeditionRunner(db));
            runner.PrepareWeek();
            while (!runner.IsWeekComplete && !runner.IsGameOver) runner.ExecuteNextSlot();
            Assert.AreEqual(4, state.Exploration.LootedContainers.Count);
            var next = ExpeditionRequestFactory.Create(state, db.GetAction(TestContent.DayExpedition), 9, false, db);
            CollectionAssert.AreEquivalent(state.Exploration.LootedContainers, next.LootedContainers);
        }
    }
}
