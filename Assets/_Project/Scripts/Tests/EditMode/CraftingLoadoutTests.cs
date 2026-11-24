using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Simulation.Bots;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>제작 P2 (C5): 헤드리스 탐험이 찬 장비를 반영하고, 봇이 창고에서 장비를 고른다.</summary>
    public sealed class CraftingLoadoutTests
    {
        private const string Mop = "ITEM_WEAPON_MOP";
        private const string Bat = "ITEM_WEAPON_BAT";
        private const string Pipe = "ITEM_WEAPON_PIPE";
        private const string School = "ITEM_BAG_SCHOOL";
        private const string Hiking = "ITEM_BAG_HIKING";
        private const string Tote = "ITEM_BAG_TOTE";
        private const string Flashlight = "ITEM_TOOL_FLASHLIGHT";
        private const string Crowbar = "ITEM_TOOL_CROWBAR";
        private const string Padding = "ITEM_ARMOR_PADDING";
        private const string Guard = "ITEM_ARMOR_GUARD";

        private static IEnumerable<ItemDef> Equipment()
        {
            yield return new ItemDef { Id = Mop, Slot = EquipSlot.Weapon, WeaponPower = 1, MaxDurability = 6 };
            yield return new ItemDef { Id = Bat, Slot = EquipSlot.Weapon, WeaponPower = 2, MaxDurability = 10 };
            yield return new ItemDef { Id = Pipe, Slot = EquipSlot.Weapon, WeaponPower = 2, MaxDurability = 14 };
            yield return new ItemDef { Id = School, Slot = EquipSlot.Backpack, GridWidth = 5, GridHeight = 4 };
            yield return new ItemDef { Id = Hiking, Slot = EquipSlot.Backpack, GridWidth = 6, GridHeight = 5 };
            yield return new ItemDef { Id = Tote, Slot = EquipSlot.Backpack, GridWidth = 4, GridHeight = 3 };
            yield return new ItemDef { Id = Flashlight, Slot = EquipSlot.Tool, Tool = ToolKind.Flashlight };
            yield return new ItemDef { Id = Crowbar, Slot = EquipSlot.Tool, Tool = ToolKind.Crowbar };
            yield return new ItemDef { Id = Padding, Slot = EquipSlot.Armor, DamageReduction = 0.3f };
            yield return new ItemDef { Id = Guard, Slot = EquipSlot.Armor, DamageReduction = 0.15f };
        }

        private static InMemoryDefinitionDatabase Db(System.Action<BalanceConfig> tweak = null)
        {
            var balance = new BalanceConfig();
            tweak?.Invoke(balance);
            return new InMemoryDefinitionDatabase(balance, TestContent.CreateActions(), new[] { TestContent.CreateModel() },
                TestContent.CreateItems().Concat(Equipment()), new[] { TestContent.CreateLootTable() }, new[] { TestContent.CreateLocation() });
        }

        private static GameState StateWith(InMemoryDefinitionDatabase db, params string[] items)
        {
            var state = TestContent.NewGame(db, 7);
            state.Storage.Clear();
            foreach (var id in items)
            {
                state.Storage.Add(new ItemStack { ItemId = id, Count = 1 });
            }

            return state;
        }

        private static ExpeditionResult Run(IDefinitionDatabase db, GameState state, string actionId, int seed) =>
            new HeadlessExpeditionRunner(db).Run(ExpeditionRequestFactory.Create(state, db.GetAction(actionId), seed, false, db));

        private static string Describe(ExpeditionResult r) =>
            r.Status + "|" + string.Join(",", r.ResourceLoot.Select(l => l.Resource + ":" + l.Amount))
            + "|" + string.Join(",", r.Loot.Select(l => l.ItemId)) + "|" + r.StressDelta + "|" + r.HpDelta;

        [Test]
        public void NoEquipment_OrSchoolBag_GivesSameResult()
        {
            var db = Db();
            var bare = StateWith(db);
            var school = StateWith(db, School);
            school.Loadout.Backpack = School;
            var zeroDb = Db(b => { b.HeadlessArmorDownFactor = 0f; b.HeadlessWeaponDownPerPower = 0f; b.HeadlessFlashlightNightDown = 0f; b.HeadlessBagYieldPerCell = 0f; });
            var geared = StateWith(zeroDb, Pipe, Hiking, Flashlight, Padding);
            geared.Loadout = new LoadoutState { MainWeapon = Pipe, Backpack = Hiking, Tool = Flashlight, Armor = Padding };

            for (var seed = 1; seed <= 200; seed++)
            {
                foreach (var action in new[] { TestContent.DayExpedition, TestContent.NightExpedition })
                {
                    var expected = Describe(Run(db, bare, action, seed));
                    Assert.AreEqual(expected, Describe(Run(db, school, action, seed)), "학교 가방(기준 20칸)은 장비 없음과 같다");
                    Assert.AreEqual(expected, Describe(Run(zeroDb, geared, action, seed)), "효과 값이 0이면 장비를 차도 같다 (굴림 순서가 그대로)");
                }
            }
        }

        [Test]
        public void WeaponArmorFlashlight_LowerNightDowns_WithoutNewDowns()
        {
            var db = Db();
            var bare = StateWith(db);
            var geared = StateWith(db, Pipe, Flashlight, Padding);
            geared.Loadout = new LoadoutState { MainWeapon = Pipe, Tool = Flashlight, Armor = Padding };

            int bareDowns = 0, gearedDowns = 0;
            for (var seed = 1; seed <= 600; seed++)
            {
                var without = Run(db, bare, TestContent.NightExpedition, seed).Status == ExtractStatus.Down;
                var with = Run(db, geared, TestContent.NightExpedition, seed).Status == ExtractStatus.Down;
                Assert.IsFalse(with && !without, "장비를 차서 새로 쓰러지는 판은 없다 (seed " + seed + ")");
                bareDowns += without ? 1 : 0;
                gearedDowns += with ? 1 : 0;
            }

            Assert.Less(gearedDowns, bareDowns, "무기·손전등·방어구가 밤 쓰러짐을 줄인다");
        }

        [Test]
        public void BagCells_ScaleResourceYield()
        {
            var db = Db();
            var school = StateWith(db, School);
            school.Loadout.Backpack = School;
            var hiking = StateWith(db, Hiking);
            hiking.Loadout.Backpack = Hiking;
            var tote = StateWith(db, Tote);
            tote.Loadout.Backpack = Tote;

            int Total(GameState state, int seed) => Run(db, state, TestContent.DayExpedition, seed).ResourceLoot.Sum(r => r.Amount);

            int baseSum = 0, bigSum = 0, smallSum = 0;
            for (var seed = 1; seed <= 300; seed++)
            {
                var b = Total(school, seed);
                var big = Total(hiking, seed);
                var small = Total(tote, seed);
                Assert.GreaterOrEqual(big, b, "큰 가방은 같은 판에서 덜 가져오지 않는다");
                Assert.LessOrEqual(small, b, "작은 가방은 같은 판에서 더 가져오지 않는다");
                baseSum += b;
                bigSum += big;
                smallSum += small;
            }

            Assert.That(bigSum, Is.EqualTo(baseSum * 1.12).Within(baseSum * 0.03), "30칸 = +12%");
            Assert.That(smallSum, Is.EqualTo(baseSum * 0.904).Within(baseSum * 0.03), "12칸 = −9.6%");
        }

        [Test]
        public void Bot_PicksBestFromStorage()
        {
            var db = Db();
            var state = StateWith(db, Mop, Bat, Pipe, Tote, School, Hiking, Guard, Padding, Flashlight, Crowbar);
            state.Storage.Find(s => s.ItemId == Pipe).Durability = 3; // 남은 내구 3/14 → 0.43
            var strategy = new BotStrategyDef();

            BotLoadout.Choose(state, db, strategy, night: true);
            Assert.AreEqual(Bat, state.Loadout.MainWeapon, "위력 × 남은 내구: 새 방망이 2 > 닳은 파이프 0.43 > 대걸레 1");
            Assert.AreEqual(Hiking, state.Loadout.Backpack, "칸이 가장 많은 가방");
            Assert.AreEqual(Padding, state.Loadout.Armor, "감소율이 가장 높은 방어구");
            Assert.AreEqual(Flashlight, state.Loadout.Tool, "밤에는 손전등");

            BotLoadout.Choose(state, db, strategy, night: false);
            Assert.AreEqual(Crowbar, state.Loadout.Tool, "낮에는 지렛대");

            var none = StateWith(db, Bat, Hiking);
            BotLoadout.Choose(none, db, new BotStrategyDef { Loadout = "None" }, night: true);
            Assert.AreEqual("", none.Loadout.MainWeapon, "None이면 고르지 않는다");
            Assert.AreEqual("", none.Loadout.Backpack);
        }
    }
}
