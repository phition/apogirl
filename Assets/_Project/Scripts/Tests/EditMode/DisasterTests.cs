using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>계절 재해 (확장 6): 기간·날씨, 날씨 굴림, 장마 침수·폭설 막힘(사본만 바뀐다), 헤드리스 깊은 곳 확률.</summary>
    public sealed class DisasterTests
    {
        private static readonly BalanceConfig Balance = new BalanceConfig();

        /// <summary>겉 2층 + 깊은 곳 2층: 깊은 문 1층·2층 두 쌍, 지하 2 비상구 → 2층, 보조 탈출구 1층(잠김)·2층.</summary>
        private static LocationDef Location(bool secondDoor = true)
        {
            var location = TestContent.CreateLocation();
            var layout = location.Layout;
            layout.Floors = 2;
            layout.DeepFloors = 2;
            layout.DeepCorridorLength = 20f;
            layout.Connectors.Add(new ConnectorDef { Kind = ConnectorKind.Stairs, Floor = 0, X = 4f, TargetFloor = 1 });
            layout.Connectors.Add(new ConnectorDef { Kind = ConnectorKind.Stairs, Floor = 1, X = 2.6f, TargetFloor = 0 });
            layout.Connectors.Add(new ConnectorDef { Kind = ConnectorKind.DeepDoor, Floor = 0, X = 15f, TargetFloor = 2, TargetX = 3f });
            layout.Connectors.Add(new ConnectorDef { Kind = ConnectorKind.DeepDoor, Floor = 2, X = 3f, TargetFloor = 0, TargetX = 15f });
            if (secondDoor)
            {
                layout.Connectors.Add(new ConnectorDef { Kind = ConnectorKind.DeepDoor, Floor = 1, X = 10f, TargetFloor = 2, TargetX = 12f });
                layout.Connectors.Add(new ConnectorDef { Kind = ConnectorKind.DeepDoor, Floor = 2, X = 12f, TargetFloor = 1, TargetX = 10f });
            }

            layout.Connectors.Add(new ConnectorDef { Kind = ConnectorKind.Stairs, Floor = 2, X = 16f, TargetFloor = 3 });
            layout.Connectors.Add(new ConnectorDef { Kind = ConnectorKind.Stairs, Floor = 3, X = 14.6f, TargetFloor = 2 });
            layout.Connectors.Add(new ConnectorDef { Kind = ConnectorKind.OneWayExit, Floor = 3, X = 2f, TargetFloor = 1, TargetX = 20f });
            layout.Connectors.Add(new ConnectorDef { Kind = ConnectorKind.OneWayBack, Floor = 1, X = 20f, TargetFloor = 3, TargetX = 2f });
            layout.Exits.Add(new ExitDef { Kind = ExitKind.Alternate, Floor = 0, X = 25f, StartsLocked = true });
            layout.Exits.Add(new ExitDef { Kind = ExitKind.Alternate, Floor = 1, X = 25f, StartsLocked = true });
            return location;
        }

        [Test]
        public void Active_NeedsPeriodAndWeather()
        {
            Assert.AreEqual(DisasterRules.Monsoon, DisasterRules.Active(16, WeatherType.Rain, Balance)?.Id);
            Assert.IsNull(DisasterRules.Active(16, WeatherType.Clear, Balance), "장마철이어도 맑은 주");
            Assert.IsNull(DisasterRules.Active(30, WeatherType.Rain, Balance), "장마철이 아닌 비");
            Assert.AreEqual(DisasterRules.Snowstorm, DisasterRules.Active(44, "Cold", Balance)?.Id);
            Assert.IsNull(DisasterRules.Active(44, "", Balance));
        }

        [Test]
        public void RollWeather_ForcesDisasterWeatherInPeriodOnly()
        {
            // FixedRandom.Range는 최솟값 → 계절 굴림은 맑음
            Assert.AreEqual(WeatherType.Rain, DisasterRules.RollWeather(16, Balance, new FixedRandom(0.5)), "장마철: 75%로 비");
            Assert.AreEqual(WeatherType.Clear, DisasterRules.RollWeather(16, Balance, new FixedRandom(0.9)));
            Assert.AreEqual(WeatherType.Cold, DisasterRules.RollWeather(44, Balance, new FixedRandom(0.5)), "폭설철: 한파");
            Assert.AreEqual(WeatherType.Clear, DisasterRules.RollWeather(30, Balance, new FixedRandom(0.0)), "기간 밖은 계절 굴림 그대로");
        }

        [Test]
        public void Monsoon_FloodsDeepestFloor_BlocksItsExit_OpensLockedExits_OnCopyOnly()
        {
            var original = Location();
            var monsoon = DisasterRules.Active(16, WeatherType.Rain, Balance);
            var flooded = DisasterRules.Apply(original, monsoon);
            Assert.AreNotSame(original, flooded);
            Assert.AreEqual(3, DisasterRules.FloodedFloor(flooded.Layout, monsoon), "지하 2 = 층 번호 3");

            var exit = flooded.Layout.Connectors.Single(c => c.Kind == ConnectorKind.OneWayExit);
            Assert.IsTrue(exit.Collapsed);
            Assert.AreEqual(DisasterRules.Monsoon, exit.BlockedBy);
            Assert.IsTrue(flooded.Layout.Exits.Where(e => e.Kind == ExitKind.Alternate).All(e => !e.StartsLocked), "물에 밀려 열렸다");

            Assert.IsFalse(original.Layout.Connectors.Any(c => c.Collapsed), "원본 지도는 그대로");
            Assert.IsTrue(original.Layout.Exits.Where(e => e.Kind == ExitKind.Alternate).All(e => e.StartsLocked));
            Assert.IsFalse(DeepRules.Issues(flooded.Layout).Any(i => i.Contains("닿을 수 없음") || i.Contains("나갈 수 없음")), "깊은 문으로 되돌아 나올 수 있다");
        }

        [Test]
        public void Snowstorm_BlocksFirstFloorDeepDoorPair_WhenSecondDoorExists()
        {
            var snow = DisasterRules.Active(44, WeatherType.Cold, Balance);
            var layout = DisasterRules.Apply(Location(), snow).Layout;
            var doors = layout.Connectors.Where(c => c.Kind == ConnectorKind.DeepDoor).ToList();
            Assert.IsTrue(doors.Where(c => c.Floor == 0 || (c.Floor == 2 && c.TargetFloor == 0)).All(c => c.Collapsed && c.BlockedBy == DisasterRules.Snowstorm));
            Assert.IsTrue(doors.Where(c => c.Floor == 1 || (c.Floor == 2 && c.TargetFloor == 1)).All(c => !c.Collapsed), "2층 깊은 문으로 들어간다");
            Assert.IsFalse(DeepRules.Issues(layout).Any(i => i.Contains("닿을 수 없음") || i.Contains("나갈 수 없음")));

            var first = layout.Exits.Single(e => e.Kind == ExitKind.Alternate && e.Floor == 0);
            Assert.IsTrue(first.StartsLocked);
            Assert.AreEqual(DisasterRules.Snowstorm, first.BlockedBy, "눈에 파묻힌 쪽문");
            Assert.IsTrue(string.IsNullOrEmpty(layout.Exits.Single(e => e.Kind == ExitKind.Alternate && e.Floor == 1).BlockedBy));

            var single = DisasterRules.Apply(Location(secondDoor: false), snow).Layout;
            Assert.IsFalse(single.Connectors.Any(c => c.Kind == ConnectorKind.DeepDoor && c.Collapsed), "깊은 문이 하나뿐이면 막지 않는다");

            var flat = TestContent.CreateLocation();
            Assert.AreSame(flat, DisasterRules.Apply(flat, snow), "깊은 곳이 없는 지역은 그대로");
        }

        [Test]
        public void Headless_MonsoonLowersDeepChance()
        {
            var deepDb = Db(0.5f);
            var baseDb = Db(0f);   // 깊이 안 들어간 판의 스트레스 (같은 시드)
            int Entered(string weather) => Enumerable.Range(1, 300).Count(seed =>
                Run(deepDb, seed, weather).StressDelta - Run(baseDb, seed, weather).StressDelta == deepDb.Balance.HeadlessDeepStress);

            var clear = Entered("Clear");
            var rain = Entered("Rain");
            Assert.Greater(clear, 0);
            Assert.Less(rain, clear, "장마 날에는 덜 내려간다");
        }

        private static InMemoryDefinitionDatabase Db(float deepChance) => new InMemoryDefinitionDatabase(
            new BalanceConfig { HeadlessDeepChance = deepChance, HeadlessDeepItemChance = 0f },
            TestContent.CreateActions(), new[] { TestContent.CreateModel() },
            TestContent.CreateItems(), new[] { TestContent.CreateLootTable() }, new[] { Location() });

        private static ExpeditionResult Run(InMemoryDefinitionDatabase db, int seed, string weather) =>
            new HeadlessExpeditionRunner(db).Run(new ExpeditionRequest
            {
                LocationId = TestContent.Location, Week = 16, Seed = seed, TimeOfDay = ExpeditionTime.Day, Hp = 100, WeatherId = weather,
            });
    }
}
