using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Planning;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>열어 둔 탈출로(다음 탐험에도 열림·후문 진입), 특별 휴식 주 1회, 같은 장비를 내구도별로 고르기.</summary>
    public sealed class ExitAndOutingTests
    {
        private static readonly BalanceConfig Balance = new BalanceConfig();

        private static LocationDef Town()
        {
            var location = TestContent.CreateLocation();
            location.Layout.DeepFloors = 1;
            location.Layout.DeepCorridorLength = 20f;
            location.Layout.Exits.Add(new ExitDef { Kind = ExitKind.Alternate, Floor = 0, X = 30f, StartsLocked = true });
            return location;
        }

        [Test]
        public void OpenedExit_StaysOpen_AndAllowsRearEntry()
        {
            var plain = Town();
            var none = new HashSet<string>();
            Assert.AreSame(plain, ExitRules.Apply(plain, none), "열지 않았으면 원본 그대로");
            Assert.IsFalse(ExitRules.CanEnterRear(ExitRules.RunMap(plain, none, null)));

            var flags = new HashSet<string> { ExitRules.OpenFlag(plain.Id) };
            var opened = ExitRules.Apply(plain, flags);
            Assert.IsFalse(opened.Layout.Exits.First(e => e.Kind == ExitKind.Alternate).StartsLocked);
            Assert.IsTrue(plain.Layout.Exits.First(e => e.Kind == ExitKind.Alternate).StartsLocked, "원본 지도는 건드리지 않는다");
            Assert.IsTrue(ExitRules.CanEnterRear(ExitRules.RunMap(plain, flags, null)));
        }

        [Test]
        public void Snowstorm_BuriesOpenedExit_NoRearEntry()
        {
            var plain = Town();
            var flags = new HashSet<string> { ExitRules.OpenFlag(plain.Id) };
            var snow = DisasterRules.Active(44, WeatherType.Cold, Balance);
            Assert.IsNotNull(snow);
            var map = ExitRules.RunMap(plain, flags, snow);
            Assert.IsTrue(map.Layout.Exits.First(e => e.Kind == ExitKind.Alternate).StartsLocked, "눈에 파묻힌 1층 탈출구는 열어 뒀어도 막힌다");
            Assert.IsFalse(ExitRules.CanEnterRear(map));
        }

        [Test]
        public void School_HasNoRearEntry()
        {
            var school = Town();
            school.Id = "LOC_SCHOOL_INTERIOR";
            var flags = new HashSet<string> { ExitRules.OpenFlag(school.Id) };
            Assert.IsFalse(ExitRules.CanEnterRear(ExitRules.RunMap(school, flags, null)), "바깥 길이 없는 교실동은 후문 진입이 없다");
        }

        [Test]
        public void Outings_OnePerWeek()
        {
            ActionDef Outing(string id) => new ActionDef { Id = id, Category = ActionCategory.Daily, SlotTag = SlotTag.DayOnly, WeeklyLimitGroup = "OUTING" };
            var db = new InMemoryDefinitionDatabase(new BalanceConfig(), TestContent.CreateActions().Concat(new[] { Outing("ACT_OUTING_A"), Outing("ACT_TRIP_B") }),
                new[] { TestContent.CreateModel() }, TestContent.CreateItems(), new[] { TestContent.CreateLootTable() }, new[] { TestContent.CreateLocation() });
            var state = TestContent.NewGame(db);
            Assert.AreEqual(PlanEditResult.Ok, PlanEditing.SetSlot(state, db, 2, "ACT_OUTING_A"));
            Assert.AreEqual(PlanEditResult.WeeklyLimit, PlanEditing.SetSlot(state, db, 4, "ACT_OUTING_A"), "같은 소풍도 한 주에 한 번");
            Assert.AreEqual(PlanEditResult.WeeklyLimit, PlanEditing.SetSlot(state, db, 4, "ACT_TRIP_B"), "다른 특별 휴식도 한 주에 하나");
            Assert.AreEqual(PlanEditResult.Ok, PlanEditing.SetSlot(state, db, 2, "ACT_TRIP_B"), "같은 칸을 바꾸는 건 된다");
        }

        [Test]
        public void BringToFront_PicksStackByDurability()
        {
            var bat = new ItemDef { Id = "ITEM_T_BAT", Slot = EquipSlot.Weapon, MaxDurability = 12 };
            var state = new GameState();
            state.Storage.Add(new ItemStack { ItemId = bat.Id, Count = 1, Durability = 3 });
            state.Storage.Add(new ItemStack { ItemId = "ITEM_OTHER", Count = 1 });
            state.Storage.Add(new ItemStack { ItemId = bat.Id, Count = 2 });

            Assert.IsTrue(LoadoutRules.BringToFront(state, bat, 12));
            Assert.AreEqual(0, state.Storage.First(s => s.ItemId == bat.Id).Durability, "새것(0 = 12) 묶음이 맨 앞");
            Assert.AreEqual(2, state.Storage.First(s => s.ItemId == bat.Id).Count);
            Assert.IsTrue(LoadoutRules.BringToFront(state, bat, 3));
            Assert.AreEqual(3, state.Storage.First(s => s.ItemId == bat.Id).Durability);
            Assert.IsFalse(LoadoutRules.BringToFront(state, bat, 7), "그 내구의 묶음이 없으면 그대로");
            Assert.AreEqual(3, state.Storage.Count);
        }
    }
}
