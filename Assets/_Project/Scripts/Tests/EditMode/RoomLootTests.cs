using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Exploration.Model;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    public sealed class RoomLootTests
    {
        [Test]
        public void SameRoomSameSeed_SameContents()
        {
            var db = TestContent.CreateDatabase();
            var location = TestContent.CreateLocation();
            for (var seed = 0; seed < 20; seed++)
            {
                var a = RoomLoot.RollRoom(db, location, 0, seed);
                var b = RoomLoot.RollRoom(db, location, 0, seed);
                Assert.AreEqual(3, a.Count, "템플릿 컨테이너 수만큼");
                for (var i = 0; i < a.Count; i++)
                {
                    CollectionAssert.AreEqual(a[i].Select(x => x.ItemId), b[i].Select(x => x.ItemId));
                }
            }
        }

        [Test]
        public void SameTemplateDifferentRooms_DifferentContents()
        {
            var db = TestContent.CreateDatabase();
            var location = TestContent.CreateLocation();
            var differs = 0;
            for (var seed = 0; seed < 20; seed++)
            {
                var first = Flatten(RoomLoot.RollRoom(db, location, 0, seed));
                var second = Flatten(RoomLoot.RollRoom(db, location, 1, seed));
                if (first != second)
                {
                    differs++;
                }
            }

            Assert.Greater(differs, 15, "같은 템플릿이라도 방마다 결과가 달라야 함");
            Assert.AreNotEqual(RoomLoot.RoomSeed(5, 0), RoomLoot.RoomSeed(5, 1));
        }

        [Test]
        public void TablePriority_ContainerThenDoorThenTemplateThenLocation()
        {
            var location = new LocationDef { DefaultLootTableId = "LOC" };
            var template = new RoomTemplateDef { DefaultLootTableId = "TPL" };
            var door = new DoorDef { LootTableId = "DOOR" };

            Assert.AreEqual("BOX", RoomLoot.ResolveTable(new ContainerDef { LootTableId = "BOX" }, door, template, location));
            Assert.AreEqual("DOOR", RoomLoot.ResolveTable(new ContainerDef(), door, template, location));
            Assert.AreEqual("TPL", RoomLoot.ResolveTable(new ContainerDef(), new DoorDef(), template, location));
            Assert.AreEqual("LOC", RoomLoot.ResolveTable(new ContainerDef(), null, null, location));
        }

        [Test]
        public void MissingTable_YieldsEmptyContainers()
        {
            var db = TestContent.CreateDatabase();
            var location = TestContent.CreateLocation();
            var rolled = RoomLoot.RollRoom(db, location, 2, 1); // R3: 없는 테이블 지정
            Assert.IsTrue(rolled.All(c => c.Count == 0));
        }

        private static string Flatten(System.Collections.Generic.List<System.Collections.Generic.List<Core.State.ItemStack>> room)
        {
            return string.Join("|", room.Select(c => string.Join(",", c.Select(i => i.ItemId))));
        }
    }
}
