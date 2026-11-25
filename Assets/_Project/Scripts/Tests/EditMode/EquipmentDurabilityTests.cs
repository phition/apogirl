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
    /// <summary>방어구·쇠지렛대 내구도(부서지면 사라지고 못 고침)와 빛 도구 전력(바닥나도 남고 건전지로 충전).</summary>
    public sealed class EquipmentDurabilityTests
    {
        private const string Light = "ITEM_T_LIGHT";
        private const string Bar = "ITEM_T_BAR";
        private const string Vest = "ITEM_T_VEST";
        private const string Club = "ITEM_T_CLUB";

        private static InMemoryDefinitionDatabase Db()
        {
            var items = TestContent.CreateItems().Concat(new[]
            {
                new ItemDef { Id = Light, Slot = EquipSlot.Tool, Tool = ToolKind.Flashlight, MaxDurability = 100 },
                new ItemDef { Id = Bar, Height = 2, Slot = EquipSlot.Tool, Tool = ToolKind.Crowbar, MaxDurability = 4 },
                new ItemDef { Id = Vest, Width = 2, Height = 2, Slot = EquipSlot.Armor, DamageReduction = 0.3f, MaxDurability = 10 },
                new ItemDef { Id = Club, Height = 2, Slot = EquipSlot.Weapon, WeaponPower = 1, MaxDurability = 8 },
            });
            return new InMemoryDefinitionDatabase(new BalanceConfig(), TestContent.CreateActions(), new[] { TestContent.CreateModel() }, items,
                new[] { TestContent.CreateLootTable() }, new[] { TestContent.CreateLocation() });
        }

        private static GameState NewState(InMemoryDefinitionDatabase db, params ItemStack[] stacks)
        {
            var state = TestContent.NewGame(db, 7);
            state.Storage.Clear();
            state.Storage.AddRange(stacks);
            return state;
        }

        private static ExpeditionRequest RunWeek(GameState state, IDefinitionDatabase db, ExpeditionResult result)
        {
            var stub = new StubExpeditionRunner { Handler = r => result };
            state.Plan.Slots[2].ActionId = TestContent.DayExpedition;
            var runner = new WeekRunner(state, db, new SeededRandom(state.Rng), stub);
            runner.PrepareWeek();
            while (!runner.IsWeekComplete && !runner.IsGameOver) runner.ExecuteNextSlot();
            return stub.Requests.FirstOrDefault();
        }

        [Test]
        public void Crowbar_WearsDown_ThenBreaksAndLeavesToolSlot()
        {
            var db = Db();
            var state = NewState(db, new ItemStack { ItemId = Bar, Count = 1 });
            state.Loadout.Tool = Bar;

            LoadoutRules.WearItem(state, db, Bar, 1, new StateDelta());
            Assert.AreEqual(3, state.Storage[0].Durability, "새것(0 = 4)에서 1 닳아 3");
            LoadoutRules.WearItem(state, db, Bar, 3, new StateDelta());
            Assert.AreEqual(0, LoadoutRules.CountInStorage(state, Bar), "0이 되면 부서져 사라진다");
            Assert.AreEqual("", state.Loadout.Tool, "찬 칸도 비운다");
        }

        [Test]
        public void Light_EmptyPowerStays_AndNegativeWearCharges()
        {
            var db = Db();
            var state = NewState(db, new ItemStack { ItemId = Light, Count = 1 });
            state.Loadout.Tool = Light;

            LoadoutRules.WearItem(state, db, Light, 150, new StateDelta());
            Assert.AreEqual(1, LoadoutRules.CountInStorage(state, Light), "전력이 바닥나도 사라지지 않는다");
            Assert.AreEqual(-1, state.Storage[0].Durability, "바닥 = −1");
            Assert.AreEqual(Light, state.Loadout.Tool);

            LoadoutRules.WearItem(state, db, Light, -40, new StateDelta());
            Assert.AreEqual(40, state.Storage[0].Durability, "음수 소모 = 충전");
        }

        [Test]
        public void TwoInOneStack_OnlyOneGetsWorn()
        {
            var db = Db();
            var state = NewState(db, new ItemStack { ItemId = Vest, Count = 2 });
            LoadoutRules.SetDurability(state, db, Vest, 6, new StateDelta());
            Assert.AreEqual(2, LoadoutRules.CountInStorage(state, Vest));
            Assert.AreEqual(6, state.Storage[0].Durability, "맨 앞 하나만 닳는다");
            Assert.AreEqual(0, state.Storage[1].Durability, "다른 하나는 새것");
        }

        [Test]
        public void Departure_ChargesEmptyLightFromBaseBattery_DeductedOnReturn()
        {
            var db = Db();
            var state = NewState(db, new ItemStack { ItemId = Light, Count = 1, Durability = -1 }, new ItemStack { ItemId = Vest, Count = 1, Durability = 7 });
            state.Loadout.Tool = Light;
            state.Loadout.Armor = Vest;
            state.Base.SetResource(ResourceType.Battery, 2);

            var result = new ExpeditionResult { Status = ExtractStatus.Extracted };
            result.OwnedDurability[Light] = 25;
            result.OwnedDurability[Vest] = 5;
            var request = RunWeek(state, db, result);

            Assert.AreEqual(40, request.EquipDurability[Light], "바닥난 빛 도구는 배터리 하나로 40%");
            Assert.AreEqual(7, request.EquipDurability[Vest]);
            Assert.AreEqual(1, request.BaseBatteriesUsed);
            Assert.AreEqual(1, state.Base.GetResource(ResourceType.Battery), "쓴 배터리는 귀환 때 뺀다");
            Assert.AreEqual(25, state.Storage.First(s => s.ItemId == Light).Durability);
            Assert.AreEqual(5, state.Storage.First(s => s.ItemId == Vest).Durability);
        }

        [Test]
        public void Departure_NoBattery_LightStaysEmpty()
        {
            var db = Db();
            var state = NewState(db, new ItemStack { ItemId = Light, Count = 1, Durability = -1 });
            state.Loadout.Tool = Light;
            state.Base.SetResource(ResourceType.Battery, 0);

            var request = ExpeditionRequestFactory.Create(state, db.GetAction(TestContent.NightExpedition), 1, false, db);
            Assert.AreEqual(0, request.EquipDurability[Light]);
            Assert.AreEqual(0, request.BaseBatteriesUsed);
        }

        [Test]
        public void ArmorBrokenOnTheRun_LeavesStorageAndSlot()
        {
            var db = Db();
            var state = NewState(db, new ItemStack { ItemId = Vest, Count = 1, Durability = 2 });
            state.Loadout.Armor = Vest;
            var result = new ExpeditionResult { Status = ExtractStatus.Extracted };
            result.OwnedDurability[Vest] = 0;
            RunWeek(state, db, result);

            Assert.AreEqual(0, LoadoutRules.CountInStorage(state, Vest));
            Assert.AreEqual("", state.Loadout.Armor);
        }

        [Test]
        public void Headless_ArmorWears_NightLightDrains_DayLightKeeps()
        {
            var db = Db();
            var balance = db.Balance;
            foreach (var action in new[] { TestContent.DayExpedition, TestContent.NightExpedition })
            {
                for (var seed = 1; seed <= 40; seed++)
                {
                    var state = NewState(db, new ItemStack { ItemId = Light, Count = 1 }, new ItemStack { ItemId = Vest, Count = 1 });
                    state.Loadout.Tool = Light;
                    state.Loadout.Armor = Vest;
                    var result = new HeadlessExpeditionRunner(db).Run(ExpeditionRequestFactory.Create(state, db.GetAction(action), seed, false, db));
                    if (result.Status != ExtractStatus.Extracted)
                    {
                        continue;
                    }

                    Assert.AreEqual(10 - balance.HeadlessArmorWear, result.OwnedDurability[Vest]);
                    var night = action == TestContent.NightExpedition;
                    Assert.AreEqual(night ? 100 - balance.HeadlessFlashlightNightDrain : 100, result.OwnedDurability[Light]);
                }
            }
        }

        [Test]
        public void Maintain_OnlyWeapons()
        {
            var db = Db();
            var state = NewState(db, new ItemStack { ItemId = Bar, Count = 1, Durability = 1 }, new ItemStack { ItemId = Vest, Count = 1, Durability = 3 });
            Assert.IsNull(CraftRules.WeaponToMaintain(state, db), "방어구·쇠지렛대는 손질(수리)할 수 없다");
            state.Storage.Add(new ItemStack { ItemId = Club, Count = 1, Durability = 2 });
            Assert.AreEqual(Club, CraftRules.WeaponToMaintain(state, db)?.ItemId);
        }

        [Test]
        public void LootedGearInBackpack_KeepsPowerAndDurability()
        {
            var db = Db();
            var placed = new[]
            {
                new PlacedItem { ItemId = Light, Wear = 100 },
                new PlacedItem { ItemId = Light, Wear = 30 },
                new PlacedItem { ItemId = Vest, Wear = 4 },
            };
            var result = RunResultBuilder.Build(ExtractStatus.Extracted, placed, db, 15, 6, 0, 10f);
            var lights = result.Loot.Where(s => s.ItemId == Light).Select(s => s.Durability).OrderBy(d => d).ToList();
            CollectionAssert.AreEqual(new[] { -1, 70 }, lights, "바닥난 손전등도 그대로 들어온다");
            Assert.AreEqual(6, result.Loot.First(s => s.ItemId == Vest).Durability);
        }
    }
}
