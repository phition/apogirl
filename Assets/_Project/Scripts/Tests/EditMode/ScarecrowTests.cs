using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Random;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;
using ApoGirl.Exploration.Model;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>허수아비 줄 (제작 P6, Docs/CRAFTING_PROPOSAL.md 3.7절): 교문 허수아비·휴대 허수아비·챙길 것.</summary>
    public sealed class ScarecrowTests
    {
        private const string Dummy = "ITEM_DECOY_DUMMY";
        private const string Bell = "ITEM_DECOY_BELL";
        private const string Bandage = "ITEM_T_BANDAGE";

        private static IEnumerable<ItemDef> Items()
        {
            yield return new ItemDef { Id = Dummy, Height = 2, Decoy = new DecoyDef { Interval = 4f, Radius = 6f, Seconds = 40f, Hp = 10f } };
            yield return new ItemDef { Id = Bell, Height = 2, Decoy = new DecoyDef { Delay = 6f, Interval = 4f, Radius = 9f, Seconds = 60f, Hp = 16f } };
            yield return new ItemDef { Id = Bandage, UseHp = 10 };
        }

        private static InMemoryDefinitionDatabase Db() =>
            new InMemoryDefinitionDatabase(new BalanceConfig(), TestContent.CreateActions(), new[] { TestContent.CreateModel() },
                TestContent.CreateItems().Concat(Items()), new[] { TestContent.CreateLootTable() }, new[] { TestContent.CreateLocation() });

        private static void Build(GameState state, BalanceConfig balance, params string[] flags)
        {
            foreach (var flag in flags)
            {
                state.Flags.Add(flag);
            }

            ScarecrowRules.OnCrafted(state, new CraftDef { SetFlags = flags.ToList() }, balance);
        }

        // ── 교문 허수아비 ──

        [Test]
        public void Gate_LowersRaidStrength_AndWearsOut()
        {
            var db = Db();
            var balance = db.Balance;
            var state = TestContent.NewGame(db, 3);
            Build(state, balance, balance.ScarecrowFlag);
            Assert.AreEqual(balance.ScarecrowDurability, state.Base.Scarecrow);

            var held = RaidRules.Resolve(state, 30, 999, false, balance, new FixedRandom(), new StateDelta());
            Assert.IsTrue(held.Repelled);
            Assert.AreEqual(balance.ScarecrowRaidRelief, held.ScarecrowRelief);
            Assert.AreEqual(30 - balance.ScarecrowRaidRelief, held.Strength, "세기만 준다");
            Assert.AreEqual(balance.ScarecrowDurability - 1, state.Base.Scarecrow, "막은 습격마다 내구 −1");

            var lost = RaidRules.Resolve(state, 999, 0, false, balance, new FixedRandom(), new StateDelta());
            Assert.IsFalse(lost.Repelled);
            Assert.IsTrue(lost.ScarecrowFell);
            Assert.AreEqual(0, state.Base.Scarecrow, "뚫리면 쓰러진다");
            Assert.IsFalse(state.Flags.Contains(balance.ScarecrowFlag), "다시 만들 수 있게 깃발을 끈다");

            var none = RaidRules.Resolve(state, 30, 999, false, balance, new FixedRandom(), new StateDelta());
            Assert.AreEqual(0, none.ScarecrowRelief);
            Assert.AreEqual(30, none.Strength);
        }

        [Test]
        public void BellGate_RelievesMore_AndSavesFoodWhenBreached()
        {
            var db = Db();
            var balance = db.Balance;
            int FoodLost(bool bell)
            {
                var state = TestContent.NewGame(db, 3);
                state.Base.Resources[ResourceType.Food] = 100;
                Build(state, balance, balance.ScarecrowFlag);
                if (bell)
                {
                    Build(state, balance, balance.ScarecrowBellFlag);
                    Assert.AreEqual(balance.ScarecrowBellDurability, state.Base.Scarecrow);
                    Assert.AreEqual(balance.ScarecrowBellRaidRelief, ScarecrowRules.Relief(state, balance));
                }

                RaidRules.Resolve(state, 999, 0, false, balance, new FixedRandom(), new StateDelta());
                Assert.IsFalse(state.Flags.Contains(balance.ScarecrowBellFlag));
                return 100 - state.Base.GetResource(ResourceType.Food);
            }

            Assert.Less(FoodLost(true), FoodLost(false), "울리는 허수아비가 서 있으면 식량 피해 ×0.8");
        }

        // ── 휴대 허수아비 (탐험) ──

        private const int Corridor = 0;
        private static readonly EnemyDef Walker = new EnemyDef { Id = "W", WalkSpeed = 2f, RunSpeed = 3f, SightRange = 7f, AwarenessRate = 1.5f, Damage = 10, HitCooldown = 1f, AttackRange = 0.7f };

        private static ThreatSimulation Sim(BalanceConfig balance)
        {
            var sim = new ThreatSimulation(balance, new SeededRandom(SeededRandom.CreateState(3)));
            sim.SetAreaWidth(Corridor, 80f);
            return sim;
        }

        private static void Run(ThreatSimulation sim, float seconds, PlayerSnapshot player)
        {
            for (var t = 0f; t < seconds; t += 0.1f)
            {
                sim.Step(0.1f, player, ActivityMods.Neutral);
            }
        }

        [Test]
        public void Decoy_DrawsZombies_WhoLatchAndWearItDown()
        {
            var balance = new BalanceConfig();
            var sim = Sim(balance);
            var zombie = sim.AddZombie(Walker, Corridor, 14f);
            var far = new PlayerSnapshot { AreaId = Corridor, X = 70f };
            var decoy = sim.AddDecoy(Corridor, 10f, new DecoyDef { Interval = 4f, Radius = 6f, Seconds = 40f, Hp = 10f });

            Run(sim, 4f, far);
            Assert.IsTrue(zombie.OnDecoy, "소리를 따라와 허수아비에 붙는다");
            Assert.Less(decoy.Hp, 10f, "붙은 좀비가 깎는다");

            Run(sim, 12f, far);
            Assert.IsTrue(decoy.Down, "체력이 다하면 넘어진다");
            Assert.IsFalse(zombie.OnDecoy, "넘어지면 놓아 준다");
        }

        [Test]
        public void Decoy_SurvivorsIgnoreIt()
        {
            var balance = new BalanceConfig();
            var sim = Sim(balance);
            var survivor = sim.AddZombie(new EnemyDef { Id = "S", Kind = EnemyKind.Survivor, WalkSpeed = 2f, RunSpeed = 3f, SightRange = 7f, AwarenessRate = 1.5f }, Corridor, 14f);
            sim.AddDecoy(Corridor, 10f, new DecoyDef { Interval = 4f, Radius = 6f, Seconds = 40f, Hp = 10f });

            Run(sim, 6f, new PlayerSnapshot { AreaId = Corridor, X = 70f });
            Assert.IsFalse(survivor.OnDecoy);
            Assert.AreNotEqual(ZombieState.Investigate, survivor.State, "사람은 허수아비 소리를 따라가지 않는다");
        }

        [Test]
        public void Decoy_BellWaits_AndLatchedZombieSeesLess()
        {
            var balance = new BalanceConfig();
            var sim = Sim(balance);
            var zombie = sim.AddZombie(Walker, Corridor, 14f);
            var bell = sim.AddDecoy(Corridor, 10f, new DecoyDef { Delay = 6f, Interval = 4f, Radius = 9f, Seconds = 60f, Hp = 16f });
            var far = new PlayerSnapshot { AreaId = Corridor, X = 70f };

            Run(sim, 5f, far);
            Assert.IsFalse(zombie.OnDecoy, "울리기 전에는 조용하다");
            Run(sim, 7f, far);
            Assert.IsTrue(zombie.OnDecoy, "울린 뒤 붙는다");

            // 시야 7의 0.5 = 3.5: 5만큼 떨어진 주인공은 보이지 않는다
            zombie.Facing = 1;
            var near = new PlayerSnapshot { AreaId = Corridor, X = zombie.X + 5f };
            Assert.IsFalse(sim.CanSee(zombie, near, ActivityMods.Neutral));
            zombie.DecoyId = -1;
            Assert.IsTrue(sim.CanSee(zombie, near, ActivityMods.Neutral));
            Assert.IsFalse(bell.Down);
        }

        // ── 챙길 것 ──

        [Test]
        public void Carry_IsTrimmedToStorage_AndCopiedIntoRequest()
        {
            var db = Db();
            var state = TestContent.NewGame(db, 3);
            state.Storage.Add(new ItemStack { ItemId = Dummy, Count = 2 });
            state.Loadout.Carry = new List<string> { Dummy, Dummy, Dummy, Bell, "" };
            LoadoutRules.Sanitize(state);
            CollectionAssert.AreEqual(new[] { Dummy, Dummy }, state.Loadout.Carry, "창고에 있는 만큼만");

            Assert.IsTrue(LoadoutRules.IsCarryable(db.GetItem(Dummy)));
            Assert.IsTrue(LoadoutRules.IsCarryable(db.GetItem(Bandage)));
            Assert.AreEqual(4, LoadoutRules.CarryCells(state.Loadout, db));

            var request = ExpeditionRequestFactory.Create(state, db.GetAction(TestContent.NightExpedition), 5, false, db);
            CollectionAssert.AreEqual(state.Loadout.Carry, request.Loadout.Carry);
            Assert.AreNotSame(state.Loadout.Carry, request.Loadout.Carry);
        }

        [Test]
        public void Headless_NightDecoy_LowersDowns_AndIsUsed()
        {
            var db = Db();
            var bare = TestContent.NewGame(db, 7);
            var carrying = TestContent.NewGame(db, 7);
            carrying.Storage.Add(new ItemStack { ItemId = Dummy, Count = 1 });
            carrying.Loadout.Carry = new List<string> { Dummy };

            int bareDowns = 0, decoyDowns = 0;
            for (var seed = 1; seed <= 600; seed++)
            {
                var without = new HeadlessExpeditionRunner(db).Run(ExpeditionRequestFactory.Create(bare, db.GetAction(TestContent.NightExpedition), seed, false, db));
                var with = new HeadlessExpeditionRunner(db).Run(ExpeditionRequestFactory.Create(carrying, db.GetAction(TestContent.NightExpedition), seed, false, db));
                Assert.IsFalse(with.Status == ExtractStatus.Down && without.Status != ExtractStatus.Down, "허수아비로 새로 쓰러지는 판은 없다");
                if (with.Status != ExtractStatus.Down)
                {
                    CollectionAssert.Contains(with.UsedItems, Dummy, "밤 탐험에서 하나를 쓴다");
                }

                bareDowns += without.Status == ExtractStatus.Down ? 1 : 0;
                decoyDowns += with.Status == ExtractStatus.Down ? 1 : 0;
            }

            Assert.Less(decoyDowns, bareDowns);

            var day = new HeadlessExpeditionRunner(db).Run(ExpeditionRequestFactory.Create(carrying, db.GetAction(TestContent.DayExpedition), 1, false, db));
            CollectionAssert.DoesNotContain(day.UsedItems, Dummy, "낮에는 쓰지 않는다");
        }
    }
}
