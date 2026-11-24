using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Planning;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>제작 P3 (C1): 재료·앞 단계 물건, 계획 잠금, 주 끝 막기, 마지막 칸 판정, 실패 환불, 대성공, 보관 상한, 손질.</summary>
    public sealed class CraftRulesTests
    {
        private const string One = "ACT_CRAFT_T_ONE";       // 1칸, 자재 2 → 몽둥이
        private const string Two = "ACT_CRAFT_T_TWO";       // 2칸, 자재 4 + 몽둥이 → 감은 몽둥이 (판정)
        private const string Three = "ACT_CRAFT_T_THREE";   // 3칸, 자재 1 → 가방 (보관 1)
        private const string Maintain = "ACT_CRAFT_T_MAINTAIN";
        private const string Club = "ITEM_T_CLUB";
        private const string Taped = "ITEM_T_TAPED";
        private const string Bag = "ITEM_T_BAG";

        private static IEnumerable<ActionDef> CraftActions(BalanceConfig balance, float chance)
        {
            yield return new ActionDef { Id = balance.CraftContinueActionId, Category = ActionCategory.Craft, SlotTag = SlotTag.Both };
            yield return Recipe(One, 1, 2, new CraftDef { Work = 1, Product = Club, Next = Two });
            var two = Recipe(Two, 2, 4, new CraftDef { Work = 2, Product = Taped, ItemCosts = { new CraftItemCost { ItemId = Club, Count = 1 } } });
            two.HasCheck = true;
            two.Check = new CheckDef { BaseRate = chance };
            yield return two;
            yield return Recipe(Three, 3, 1, new CraftDef { Work = 3, Product = Bag, MaxOwned = 1 });
            yield return Recipe(Maintain, 1, 1, new CraftDef { Work = 1, MaintainWeapon = true });
        }

        private static ActionDef Recipe(string id, int work, int materials, CraftDef craft)
        {
            craft.Work = work;
            return new ActionDef
            {
                Id = id,
                Category = ActionCategory.Craft,
                SlotTag = SlotTag.DayOnly,
                FatigueCost = 5,
                ResourceCosts = { new ResourceAmount(ResourceType.Materials, materials) },
                Craft = craft,
            };
        }

        private static IEnumerable<ItemDef> CraftItems()
        {
            yield return new ItemDef { Id = Club, Slot = EquipSlot.Weapon, WeaponPower = 1, MaxDurability = 8 };
            yield return new ItemDef { Id = Taped, Slot = EquipSlot.Weapon, WeaponPower = 2, MaxDurability = 12 };
            yield return new ItemDef { Id = Bag, Slot = EquipSlot.Backpack, GridWidth = 3, GridHeight = 4 };
        }

        private static InMemoryDefinitionDatabase Db(float chance = 0.95f)
        {
            var balance = new BalanceConfig();
            return new InMemoryDefinitionDatabase(balance, TestContent.CreateActions().Concat(CraftActions(balance, chance)), new[] { TestContent.CreateModel() },
                TestContent.CreateItems().Concat(CraftItems()), new[] { TestContent.CreateLootTable() }, new[] { TestContent.CreateLocation() });
        }

        private static GameState NewState(InMemoryDefinitionDatabase db, int materials, params ItemStack[] items)
        {
            var state = TestContent.NewGame(db, 7);
            state.Base.Resources[ResourceType.Materials] = materials;
            state.Storage.Clear();
            state.Storage.AddRange(items);
            return state;
        }

        private static ItemStack Stack(string id, int durability = 0) => new ItemStack { ItemId = id, Count = 1, Durability = durability };

        private static int Materials(GameState state) => state.Base.GetResource(ResourceType.Materials);

        private static CraftOutcome Run(GameState state, IDefinitionDatabase db, string actionId, int slotIndex, params double[] rolls) =>
            CraftRules.Execute(state, db, db.GetAction(actionId), slotIndex, new FixedRandom(rolls), new StateDelta());

        // ── 계획 ──

        [Test]
        public void Plan_LocksFollowingSlots_AndClearingStartReleasesThem()
        {
            var db = Db();
            var state = NewState(db, 10);
            var continueId = db.Balance.CraftContinueActionId;

            Assert.AreEqual(PlanEditResult.Ok, PlanEditing.SetSlot(state, db, 2, Three));
            for (var i = 3; i <= 4; i++)
            {
                var slot = state.Plan.Slots[i];
                Assert.AreEqual(continueId, slot.ActionId, "뒤 두 칸은 \"제작 중\"");
                Assert.IsTrue(slot.Locked && slot.CraftLocked);
            }

            Assert.AreEqual(PlanEditResult.Locked, PlanEditing.SetSlot(state, db, 3, TestContent.RestNight), "잠긴 칸은 못 바꾼다");
            Assert.AreEqual("", state.Plan.Slots[5].ActionId, "그 뒤 칸은 그대로");

            Assert.AreEqual(PlanEditResult.Ok, PlanEditing.ClearSlot(state, db, 2));
            for (var i = 3; i <= 4; i++)
            {
                Assert.AreEqual("", state.Plan.Slots[i].ActionId, "착수 카드를 빼면 풀린다");
                Assert.IsFalse(state.Plan.Slots[i].Locked || state.Plan.Slots[i].CraftLocked);
            }
        }

        [Test]
        public void Plan_NoRoomAtWeekEnd_OrBeforeOtherLock()
        {
            var db = Db();
            var state = NewState(db, 10);

            Assert.AreEqual(PlanEditResult.CraftNoRoom, PlanEditing.SetSlot(state, db, 12, Three), "7일 낮에서 3칸은 주를 넘는다");
            Assert.AreEqual(PlanEditResult.Ok, PlanEditing.SetSlot(state, db, 12, Two), "2칸은 7일 밤까지 들어간다");
            Assert.IsTrue(state.Plan.Slots[13].CraftLocked);

            PlanPreparer.LockRest(state.Plan, 5, db.Balance);
            Assert.AreEqual(PlanEditResult.CraftNoRoom, PlanEditing.SetSlot(state, db, 4, Two), "다른 까닭으로 잠긴 칸 앞에는 못 둔다");
        }

        [Test]
        public void Plan_CopyLastWeek_RelocksAndDropsStrayContinueSlots()
        {
            var db = Db();
            var state = NewState(db, 10);
            var continueId = db.Balance.CraftContinueActionId;
            state.LastWeekPlan = new WeekPlan();
            state.LastWeekPlan.Slots[0].ActionId = Two;
            state.LastWeekPlan.Slots[1].ActionId = continueId;
            state.LastWeekPlan.Slots[5].ActionId = continueId;   // 착수 없이 떠도는 칸

            Assert.IsTrue(PlanEditing.CopyLastWeek(state, db));

            Assert.AreEqual(Two, state.Plan.Slots[0].ActionId);
            Assert.IsTrue(state.Plan.Slots[1].CraftLocked);
            Assert.AreEqual("", state.Plan.Slots[5].ActionId);
        }

        // ── 실행 ──

        [Test]
        public void Start_PaysMaterialsAndItems_LastSlotCompletes()
        {
            var db = Db();
            var state = NewState(db, 10, Stack(Club, 3));
            state.Loadout.MainWeapon = Club;

            var start = Run(state, db, Two, 0);
            Assert.AreEqual(CraftStep.Started, start.Step);
            Assert.AreEqual(1, start.Remaining);
            Assert.AreEqual(6, Materials(state), "자원은 착수 칸에서 낸다");
            Assert.AreEqual(0, LoadoutRules.CountInStorage(state, Club), "앞 단계 물건도 착수 칸에서");
            Assert.AreEqual("", state.Loadout.MainWeapon, "다 쓴 물건은 장착에서도 빠진다");
            Assert.IsNotNull(state.Crafting.Active);

            var done = Run(state, db, db.Balance.CraftContinueActionId, 1, 0.5);
            Assert.AreEqual(CraftStep.Done, done.Step);
            Assert.AreEqual(Taped, done.ProductId);
            Assert.IsNull(state.Crafting.Active);
            Assert.AreEqual(12, state.Storage.Single(s => s.ItemId == Taped).Durability, "새 무기는 최대 내구");
            Assert.IsTrue(state.Flags.Contains(CraftRules.CraftedFlagPrefix + Two));
        }

        [Test]
        public void Failure_RefundsHalfResources_AndItemsWithTheirDurability()
        {
            var db = Db(0.5f);
            var state = NewState(db, 10, Stack(Club, 3), Stack(Club, 7));

            Run(state, db, Two, 0);
            Assert.AreEqual(7, state.Storage.Single(s => s.ItemId == Club).Durability, "가장 닳은 것부터 쓴다");

            var failed = Run(state, db, db.Balance.CraftContinueActionId, 1, 0.99);
            Assert.AreEqual(CraftStep.Failed, failed.Step);
            Assert.AreEqual(6 + 2, Materials(state), "자원은 반(내림)");
            CollectionAssert.AreEquivalent(new[] { 3, 7 }, state.Storage.Where(s => s.ItemId == Club).Select(s => s.Durability), "물건은 그 내구 그대로");
            Assert.IsFalse(state.Storage.Any(s => s.ItemId == Taped));
        }

        [Test]
        public void GreatSuccess_MakesFineWeapon()
        {
            var db = Db(0.5f);
            var state = NewState(db, 10, Stack(Club, 3));

            Run(state, db, Two, 0);
            var fine = Run(state, db, db.Balance.CraftContinueActionId, 1, 0.0);

            Assert.AreEqual(CraftStep.Fine, fine.Step);
            Assert.AreEqual(15, state.Storage.Single(s => s.ItemId == Taped).Durability, "잘 만든 무기는 내구 ×1.25");
        }

        [Test]
        public void MaxOwned_And_MissingItems_BlockStart()
        {
            var db = Db();
            Assert.AreEqual(CraftBlock.MaxOwned, CraftRules.CheckStart(NewState(db, 10, Stack(Bag)), db, db.GetAction(Three)));
            Assert.AreEqual(CraftBlock.Items, CraftRules.CheckStart(NewState(db, 10), db, db.GetAction(Two)));
            Assert.AreEqual(CraftBlock.Resources, CraftRules.CheckStart(NewState(db, 1, Stack(Club, 3)), db, db.GetAction(Two)));
            Assert.AreEqual(CraftBlock.NothingToMaintain, CraftRules.CheckStart(NewState(db, 10, Stack(Club, 8)), db, db.GetAction(Maintain)), "닳지 않은 무기는 손질할 게 없다");
            Assert.AreEqual(CraftBlock.None, CraftRules.CheckStart(NewState(db, 10, Stack(Club, 3)), db, db.GetAction(Two)));

            var preview = ActionPreviewer.Preview(NewState(db, 10), db, Two);
            Assert.AreEqual(2, preview.CraftWork);
            Assert.AreEqual(CraftBlock.Items, preview.CraftBlock);
            Assert.IsFalse(preview.Affordable, "앞 단계 물건이 없으면 카드는 흐리다");
        }

        [Test]
        public void NoMaterials_PaysNothing_AndFollowingSlotsRest()
        {
            var db = Db();
            var state = NewState(db, 10);
            PlanEditing.SetSlot(state, db, 0, Three);
            state.Base.Resources[ResourceType.Materials] = 0;

            var outcome = Run(state, db, Three, 0);

            Assert.AreEqual(CraftStep.NoMaterials, outcome.Step);
            Assert.IsNull(state.Crafting.Active);
            Assert.AreEqual(db.Balance.NightRestActionId, state.Plan.Slots[1].ActionId, "뒤 칸은 쉬는 칸으로");
            Assert.AreEqual(db.Balance.DayRestActionId, state.Plan.Slots[2].ActionId);
        }

        [Test]
        public void Abort_RefundsLikeFailure()
        {
            var db = Db();
            var state = NewState(db, 10, Stack(Club, 3));
            Run(state, db, Two, 0);

            Assert.IsTrue(CraftRules.Abort(state, db, new StateDelta()));
            Assert.IsNull(state.Crafting.Active);
            Assert.AreEqual(8, Materials(state));
            Assert.AreEqual(1, LoadoutRules.CountInStorage(state, Club));
            Assert.IsFalse(CraftRules.Abort(state, db, new StateDelta()), "정리할 것이 없으면 false");
        }

        [Test]
        public void Maintain_RestoresHalf_EquippedFirst_CappedAtMax()
        {
            var db = Db();
            var state = NewState(db, 10, Stack(Club, 2), Stack(Taped, 10));
            state.Loadout.MainWeapon = Taped;

            var first = Run(state, db, Maintain, 0);
            Assert.AreEqual(CraftStep.Maintained, first.Step);
            Assert.AreEqual(Taped, first.WeaponId, "찬 무기가 닳았으면 그것부터");
            Assert.AreEqual(12, state.Storage.Single(s => s.ItemId == Taped).Durability, "최대를 넘지 않는다");

            var second = Run(state, db, Maintain, 2);
            Assert.AreEqual(Club, second.WeaponId, "그다음은 창고에서 가장 닳은 무기");
            Assert.AreEqual(2 + 4, state.Storage.Single(s => s.ItemId == Club).Durability, "최대 내구의 반만큼");
        }

        [Test]
        public void WeekRunner_RunsCraftAcrossSlots()
        {
            var db = Db();
            var state = NewState(db, 10, Stack(Club, 3));
            Assert.AreEqual(PlanEditResult.Ok, PlanEditing.SetSlot(state, db, 0, Two));
            var runner = new WeekRunner(state, db, new FixedRandom(), new StubExpeditionRunner());
            runner.PrepareWeek();

            var start = runner.ExecuteNextSlot();
            Assert.AreEqual(CraftStep.Started, start.Craft.Step);
            Assert.IsTrue(state.Plan.Slots[1].Locked);

            var finish = runner.ExecuteNextSlot();
            Assert.AreEqual(CraftStep.Done, finish.Craft.Step);
            Assert.AreEqual(1, LoadoutRules.CountInStorage(state, Taped));
            Assert.IsNull(state.Crafting.Active);
        }
    }
}
