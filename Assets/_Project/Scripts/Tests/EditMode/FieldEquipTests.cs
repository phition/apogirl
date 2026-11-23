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
