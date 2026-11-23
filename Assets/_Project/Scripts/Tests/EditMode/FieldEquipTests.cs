using System.Linq;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Random;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using ApoGirl.Exploration.Model;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>탐험 중 장비 바꾸기의 귀환 반영 (ExpeditionResult.FinalLoadout·EquippedLoot·LostOwned)과 배낭 격자 보조 함수.</summary>
    public sealed class FieldEquipTests
    {
        private static void RunWeek(GameState state, ApoGirl.Core.Definitions.IDefinitionDatabase db, ExpeditionResult result)
        {
            var stub = new StubExpeditionRunner { Handler = r => result };
            state.Plan.Slots[2].ActionId = TestContent.DayExpedition;
            var runner = new WeekRunner(state, db, new SeededRandom(state.Rng), stub);
            runner.PrepareWeek();
            while (!runner.IsWeekComplete && !runner.IsGameOver) runner.ExecuteNextSlot();
        }

        [Test]
        public void SwappedWeapon_LootedOneJoinsStorageAndLoadout_DroppedOwnedOneLeaves()
        {
            var db = TestContent.CreateDatabase();
            var state = TestContent.NewGame(db);
            state.Storage.Add(new ItemStack { ItemId = "ITEM_SCRAP", Count = 1 });
            state.Loadout.MainWeapon = "ITEM_SCRAP";

            var result = new ExpeditionResult { Status = ExtractStatus.Extracted };
            result.FinalLoadout = new LoadoutState { MainWeapon = "ITEM_FOUND_WEAPON" };
            result.EquippedLoot.Add("ITEM_FOUND_WEAPON");
            result.LostOwned.Add("ITEM_SCRAP");
            RunWeek(state, db, result);

            Assert.AreEqual(0, LoadoutRules.CountInStorage(state, "ITEM_SCRAP"), "버리고 온 내 무기는 창고에서 빠진다");
            Assert.AreEqual(1, LoadoutRules.CountInStorage(state, "ITEM_FOUND_WEAPON"), "주워 찬 무기는 창고에 들어온다");
            Assert.AreEqual("ITEM_FOUND_WEAPON", state.Loadout.MainWeapon, "다음 출정에도 그 무기를 찬다");
        }

        [Test]
        public void NoSwap_LeavesLoadoutAlone()
        {
            var db = TestContent.CreateDatabase();
            var state = TestContent.NewGame(db);
            state.Storage.Add(new ItemStack { ItemId = "ITEM_SCRAP", Count = 1 });
            state.Loadout.MainWeapon = "ITEM_SCRAP";
            RunWeek(state, db, new ExpeditionResult { Status = ExtractStatus.Extracted });

            Assert.AreEqual("ITEM_SCRAP", state.Loadout.MainWeapon);
            Assert.AreEqual(1, LoadoutRules.CountInStorage(state, "ITEM_SCRAP"));
        }

        [Test]
        public void OwnedItemsInBackpack_AreNotCountedAsLoot()
        {
            var db = TestContent.CreateDatabase();
            var placed = new[]
            {
                new PlacedItem { ItemId = "ITEM_TEST_GEAR", Owned = true },
                new PlacedItem { ItemId = "ITEM_TEST_GEAR" },
            };
            var result = RunResultBuilder.Build(ExtractStatus.Extracted, placed, db, 15, 6, 0, 10f);
            var total = 0;
            foreach (var stack in result.Loot) total += stack.ItemId == "ITEM_TEST_GEAR" ? stack.Count : 0;
            Assert.AreEqual(1, total, "벗어 넣은 내 장비는 이미 창고에 있으니 전리품이 아니다");
        }

        private const string Pipe = "ITEM_T_PIPE";
        private const string Vest = "ITEM_T_VEST";
        private const string Coat = "ITEM_T_COAT";

        /// <summary>내구도 있는 무기와 방어구 둘을 더한 정의 DB.</summary>
        private static ApoGirl.Core.Definitions.IDefinitionDatabase GearDatabase()
        {
            var items = TestContent.CreateItems().Concat(new[]
            {
                new ApoGirl.Core.Definitions.ItemDef { Id = Pipe, Width = 1, Height = 2, Slot = ApoGirl.Core.Definitions.EquipSlot.Weapon, WeaponPower = 2, MaxDurability = 10 },
                new ApoGirl.Core.Definitions.ItemDef { Id = Vest, Width = 2, Height = 2, Slot = ApoGirl.Core.Definitions.EquipSlot.Armor, DamageReduction = 0.3f },
                new ApoGirl.Core.Definitions.ItemDef { Id = Coat, Width = 2, Height = 2, Slot = ApoGirl.Core.Definitions.EquipSlot.Armor, DamageReduction = 0.15f },
            });
            return new ApoGirl.Core.Definitions.InMemoryDefinitionDatabase(
                new ApoGirl.Core.Definitions.BalanceConfig(), TestContent.CreateActions(), new[] { TestContent.CreateModel() }, items,
                new[] { TestContent.CreateLootTable() }, new[] { TestContent.CreateLocation() });
        }

        [Test]
        public void SwappedArmor_LootedOneIsWornNextTime_DroppedOwnedOneLeaves()
        {
            var db = GearDatabase();
            var state = TestContent.NewGame(db);
            state.Storage.Add(new ItemStack { ItemId = Coat, Count = 1 });
            state.Loadout.Armor = Coat;

            var result = new ExpeditionResult { Status = ExtractStatus.Extracted };
            result.FinalLoadout = new LoadoutState { Armor = Vest };
            result.EquippedLoot.Add(Vest);
            result.LostOwned.Add(Coat);
            RunWeek(state, db, result);

            Assert.AreEqual(0, LoadoutRules.CountInStorage(state, Coat));
            Assert.AreEqual(1, LoadoutRules.CountInStorage(state, Vest));
            Assert.AreEqual(Vest, state.Loadout.Armor, "주워 입은 방어구를 다음 출정에도 입는다");
        }

        [Test]
        public void LootedWeaponInBackpack_KeepsItsWear()
        {
            var db = GearDatabase();
            var placed = new[]
            {
                new PlacedItem { ItemId = Pipe, Wear = 3 },
                new PlacedItem { ItemId = Pipe },
            };
            var result = RunResultBuilder.Build(ExtractStatus.Extracted, placed, db, 15, 6, 0, 10f);
            var pipes = result.Loot.Where(s => s.ItemId == Pipe).ToList();
            Assert.AreEqual(2, pipes.Sum(s => s.Count));
            Assert.IsTrue(pipes.Any(s => s.Count == 1 && s.Durability == 7), "쓰다 넣은 파이프는 남은 내구도 7로 따로 들어온다");
            Assert.IsTrue(pipes.Any(s => s.Durability == 0), "새 파이프는 새것 그대로");
        }

        [Test]
        public void EquippedLootWeapon_BecomesTheWornOneWithItsDurability()
        {
            var db = GearDatabase();
            var state = TestContent.NewGame(db);
            state.Storage.Add(new ItemStack { ItemId = Pipe, Count = 1, Durability = 9 });
            state.Loadout.MainWeapon = Pipe;

            // 내 파이프는 1 닳은 채 벗어 버리고, 주운 파이프를 4 닳게 쓰며 차고 돌아왔다
            var result = new ExpeditionResult { Status = ExtractStatus.Extracted, WeaponWear = 1 };
            result.FinalLoadout = new LoadoutState { MainWeapon = Pipe };
            result.EquippedLoot.Add(Pipe);
            result.EquippedLootWear = 4;
            result.LostOwned.Add(Pipe);
            RunWeek(state, db, result);

            var pipes = state.Storage.Where(s => s.ItemId == Pipe).ToList();
            Assert.AreEqual(1, pipes.Count, "버린 내 파이프는 빠지고 주운 파이프만 남는다");
            Assert.AreEqual(6, pipes[0].Durability, "주운 파이프의 남은 내구도가 저장된다");
            Assert.AreEqual(Pipe, state.Loadout.MainWeapon);
        }

        [Test]
        public void TwoSameWeapons_WornOneKeepsWear_FreshOneStaysFresh()
        {
            var db = GearDatabase();
            var state = TestContent.NewGame(db);
            state.Storage.Add(new ItemStack { ItemId = Pipe, Count = 1, Durability = 5 });
            state.Storage.Add(new ItemStack { ItemId = Pipe, Count = 1 });
            state.Loadout.MainWeapon = Pipe;

            RunWeek(state, db, new ExpeditionResult { Status = ExtractStatus.Extracted, WeaponWear = 2 });

            var pipes = state.Storage.Where(s => s.ItemId == Pipe).ToList();
            Assert.AreEqual(2, pipes.Count);
            Assert.AreEqual(3, pipes[0].Durability, "찬 파이프(맨 앞)만 닳는다");
            Assert.AreEqual(0, pipes[1].Durability, "다른 파이프는 새것 그대로");
        }

        [Test]
        public void Grid_AutoPlaceExisting_AndInsertBack()
        {
            var grid = new BackpackGrid(2, 2);
            var big = new PlacedItem { ItemId = "A", Width = 2, Height = 1, Owned = true, Wear = 3 };
            Assert.IsTrue(grid.TryAutoPlace(big));
            Assert.AreEqual(0, big.Y);
            Assert.IsTrue(grid.Remove(big));
            Assert.IsTrue(grid.Insert(big), "꺼낸 자리에 다시 넣는다");
            Assert.IsFalse(grid.Insert(new PlacedItem { ItemId = "B", X = 0, Y = 0 }), "막힌 자리에는 못 넣는다");
            Assert.IsTrue(grid.TryAutoPlace(new PlacedItem { ItemId = "C", Width = 2, Height = 1 }));
            Assert.IsFalse(grid.TryAutoPlace(new PlacedItem { ItemId = "D" }), "가득 차면 false");
            Assert.AreEqual(3, big.Wear);
        }
    }
}
