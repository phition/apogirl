using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Expedition;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>탐험마다 다른 적 배치 (SpawnRules): 수·종류·층은 그대로, 자리만 시드로 바뀐다. 생존자는 한 층에 한 명.</summary>
    public sealed class SpawnRulesTests
    {
        private static LocationDef Location()
        {
            var layout = new BuildingLayoutDef { Floors = 2, FloorHeight = 4f, CorridorLength = 80f };
            layout.RoomTemplates.Add(new RoomTemplateDef { Id = "ROOM_A", Width = 14f });
            layout.Doors.Add(new DoorDef { RoomId = "R1", TemplateId = "ROOM_A", Floor = 0, X = 10f });
            layout.Doors.Add(new DoorDef { RoomId = "R2", TemplateId = "ROOM_A", Floor = 0, X = 30f });
            layout.Doors.Add(new DoorDef { RoomId = "R3", TemplateId = "ROOM_A", Floor = 0, X = 50f });
            layout.Doors.Add(new DoorDef { RoomId = "R_LOCKED", TemplateId = "ROOM_A", Floor = 0, X = 64f });
            layout.Doors.Add(new DoorDef { RoomId = "R_UP", TemplateId = "ROOM_A", Floor = 1, X = 10f });
            layout.Exits.Add(new ExitDef { Kind = ExitKind.Main, Floor = 0, X = 78f });
            layout.Connectors.Add(new ConnectorDef { Kind = ConnectorKind.Stairs, Floor = 0, X = 4f, TargetFloor = 1 });
            layout.Connectors.Add(new ConnectorDef { Kind = ConnectorKind.Stairs, Floor = 1, X = 2.6f, TargetFloor = 0 });
            var variant = new LocationVariantDef
            {
                Spawns =
                {
                    new SpawnDef { EnemyId = "E_A", Floor = 0, X = 30f, Count = 3, Spread = 2f, Dormant = true },
                    new SpawnDef { EnemyId = "E_B", Floor = 1, X = 40f, Count = 2, Spread = 1.5f },
                    new SpawnDef { EnemyId = "E_C", Floor = 0, RoomId = "R1", X = 8f, Count = 2, Spread = 1f, Dormant = true },
                    new SpawnDef { EnemyId = "E_D", Floor = 0, X = 50f, Count = 1, Spread = 0f, Fixed = true },
                },
                LockedRooms = { "R_LOCKED" },
            };
            return new LocationDef { Id = "LOC_T", Layout = layout, DayVariant = variant, NightVariant = new LocationVariantDef() };
        }

        [Test]
        public void Arrange_KeepsKindCountFloor_ChangesPlacesBySeed()
        {
            var location = Location();
            var balance = new BalanceConfig();
            var seen = new HashSet<string>();
            var rooms = new HashSet<string>();
            for (var n = 1; n <= 200; n++)
            {
                // 출정 시드는 서로 멀리 떨어진 값이다 (이어진 정수는 난수열이 한 칸씩 밀린 것이라 겹친다)
                var seed = n * 7919 + 13;
                var spawns = SpawnRules.Arrange(location, location.DayVariant, seed, balance);
                Assert.AreEqual(4, spawns.Count);
                for (var i = 0; i < spawns.Count; i++)
                {
                    var source = location.DayVariant.Spawns[i];
                    Assert.AreEqual(source.EnemyId, spawns[i].EnemyId);
                    Assert.AreEqual(source.Count, spawns[i].Count);
                    Assert.AreEqual(source.Floor, spawns[i].Floor);
                    Assert.AreEqual(source.Dormant, spawns[i].Dormant);
                }

                // 복도 무리: 출구·계단에서 떨어지고 복도 안
                Assert.That(spawns[0].X, Is.InRange(2.5f, 77.5f));
                Assert.That(System.Math.Abs(spawns[0].X - 78f), Is.GreaterThanOrEqualTo(balance.SpawnSafeDistance), "출구 앞은 비운다");
                Assert.That(System.Math.Abs(spawns[0].X - 4f), Is.GreaterThanOrEqualTo(balance.SpawnStairsDistance), "계단 앞은 비운다");
                // 방 무리: 같은 층의 열 수 있는 방, 방 안 자리
                Assert.That(new[] { "R1", "R2", "R3" }, Does.Contain(spawns[2].RoomId));
                Assert.That(spawns[2].X, Is.InRange(2f, 12f));
                // 고정 배치는 그대로
                Assert.AreEqual(50f, spawns[3].X);
                seen.Add(spawns[0].X + "/" + spawns[1].X + "/" + spawns[2].RoomId + "/" + spawns[2].X);
                rooms.Add(spawns[2].RoomId);
            }

            Assert.That(seen.Count, Is.GreaterThan(150), "탐험마다 다른 배치");
            Assert.AreEqual(3, rooms.Count, "방 무리는 같은 층의 다른 방으로도 간다");
            Assert.AreEqual(30f, location.DayVariant.Spawns[0].X, "정의는 건드리지 않는다");
        }

        [Test]
        public void Arrange_SameSeedSamePlaces_AndOffKeepsDefinition()
        {
            var location = Location();
            var balance = new BalanceConfig();
            var a = SpawnRules.Arrange(location, location.DayVariant, 77, balance);
            var b = SpawnRules.Arrange(location, location.DayVariant, 77, balance);
            CollectionAssert.AreEqual(a.Select(s => s.RoomId + s.X), b.Select(s => s.RoomId + s.X));

            balance.SpawnShuffle = false;
            var off = SpawnRules.Arrange(location, location.DayVariant, 77, balance);
            CollectionAssert.AreEqual(location.DayVariant.Spawns.Select(s => s.RoomId + s.X), off.Select(s => s.RoomId + s.X));
        }

        [Test]
        public void Hostile_OnePerFloor_AndCountCappedByFloors()
        {
            var location = Location();
            for (var seed = 1; seed <= 100; seed++)
            {
                var places = SpawnRules.HostilePlaces(location, location.DayVariant, 3, seed);
                Assert.AreEqual(2, places.Count, "2층 건물이면 둘까지");
                Assert.AreEqual(2, places.Select(p => p.Floor).Distinct().Count(), "한 층에 한 명");
            }

            Assert.AreEqual(1, SpawnRules.HostilePlaces(location, location.DayVariant, 1, 5).Count);
            Assert.AreEqual(2, SpawnRules.HostileFloors(location));

            var balance = new BalanceConfig();
            ExpeditionRequest request = null;
            for (var s = 1; request == null || request.HostileTemperament == 0; s++)
            {
                request = new ExpeditionRequest { TimeOfDay = ExpeditionTime.Day, Week = 45, Seed = s, HumanThreatMultiplier = 1f };
                HostileRules.Assign(request, location, balance);
            }

            Assert.AreEqual(2, request.HostileCount, "4막 3명이라도 2층 건물이면 2명");
        }
    }
}
