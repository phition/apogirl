using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>식량 줄 (옥상 텃밭 → 비닐하우스 → 스프링클러): 하루치 생산, 살림 동료의 아침 수확·빗물 거르기, 주말 70%, 겨울, 스프링클러 전력·빗물.</summary>
    public sealed class GardenRulesTests
    {
        private const string Cook = "NPC_TEST_A";

        private static InMemoryDefinitionDatabase Db()
        {
            var extras = new DefinitionExtras
            {
                Roles =
                {
                    new RoleDef
                    {
                        Id = "HOUSE",
                        Modifiers =
                        {
                            new TraitModifier { Kind = TraitModifierKind.GardenHarvest, Value = 1 },
                            new TraitModifier { Kind = TraitModifierKind.RainPurify, Value = 1 },
                        },
                    },
                },
            };
            return new InMemoryDefinitionDatabase(new BalanceConfig(), TestContent.CreateActions(), new[] { TestContent.CreateModel() },
                TestContent.CreateItems(), new[] { TestContent.CreateLootTable() }, new[] { TestContent.CreateLocation() }, extras: extras);
        }

        private static GameState NewState(InMemoryDefinitionDatabase db, int week, int raw = 0, bool cook = false)
        {
            var state = TestContent.NewGame(db, 5);
            state.Calendar.Week = week;
            state.Base.Resources[ResourceType.Food] = 0;
            state.Base.Resources[ResourceType.Water] = 0;
            state.Base.Resources[ResourceType.RawWater] = raw;
            if (cook)
            {
                state.Npcs[Cook] = new NpcState { Status = NpcStatus.Joined, Role = "HOUSE", Affinity = 60 };
                Assert.GreaterOrEqual(TraitRules.Sum(state, db, TraitModifierKind.GardenHarvest), 1f, "준비: 살림 동료가 하루치를 다 거둔다");
            }

            return state;
        }

        private static int Res(GameState state, ResourceType type) => state.Base.GetResource(type);

        /// <summary>한 주 일곱 아침을 돌린다.</summary>
        private static void Mornings(GameState state, IDefinitionDatabase db)
        {
            for (var day = 1; day <= CalendarState.DaysPerWeek; day++)
            {
                state.Calendar.Day = day;
                GardenRules.ApplyMorning(state, db, new StateDelta());
            }
        }

        [Test]
        public void Garden_WithoutHouse_PlayerPicksSeventyPercentAtWeekend()
        {
            var db = Db();
            var state = NewState(db, 10);
            state.Flags.Add(UpkeepRules.FlagRoofGarden);
            Mornings(state, db);
            Assert.AreEqual(0, Res(state, ResourceType.Food), "아무도 아침에 거두지 않는다");
            Assert.AreEqual(db.Balance.RoofGardenFood, state.Base.GardenUnpicked, 1e-3, "한 주 내내 자라 쌓였다");

            Assert.AreEqual(7, GardenRules.ApplyWeekly(state, db.Balance, new StateDelta()), "10 × 0.7");
            Assert.AreEqual(7, Res(state, ResourceType.Food));
            Assert.AreEqual(0f, state.Base.GardenUnpicked, "정산 뒤 비운다 (나머지는 시든다)");
        }

        [Test]
        public void Garden_HouseCompanionHarvestsEveryMorning()
        {
            var db = Db();
            var state = NewState(db, 10, cook: true);
            state.Flags.Add(UpkeepRules.FlagRoofGarden);
            // 거두는 몫 = 역할 세기 (호감·방 배율이 붙어 1을 넘으면 더 알뜰히 거둔다, 상한 GardenHarvestCap)
            var share = System.Math.Min(TraitRules.Sum(state, db, TraitModifierKind.GardenHarvest), db.Balance.GardenHarvestCap);
            state.Calendar.Day = 1;
            var first = GardenRules.ApplyMorning(state, db, new StateDelta());
            Assert.AreEqual((int)System.Math.Floor(db.Balance.RoofGardenFood / 7f * share + 1e-4), first.Food, "하루치 10/7 × 몫, 소수점은 이어 쌓는다");

            for (var day = 2; day <= CalendarState.DaysPerWeek; day++)
            {
                state.Calendar.Day = day;
                GardenRules.ApplyMorning(state, db, new StateDelta());
            }

            Assert.AreEqual((int)System.Math.Floor(db.Balance.RoofGardenFood * share + 1e-3), Res(state, ResourceType.Food), "한 주에 텃밭 생산 × 몫을 거뒀다");
            Assert.GreaterOrEqual(Res(state, ResourceType.Food), db.Balance.RoofGardenFood);
            Assert.AreEqual(0, GardenRules.ApplyWeekly(state, db.Balance, new StateDelta()), "남은 몫이 없다");
        }

        [Test]
        public void HouseCompanion_PurifiesALittleRainEachMorning()
        {
            var db = Db();
            var state = NewState(db, 10, raw: 5, cook: true);
            var take = TraitRules.Spread(TraitRules.Sum(state, db, TraitModifierKind.RainPurify) * db.Balance.HouseRainPurifyDaily, GardenRules.Step(10, 1));
            Assert.GreaterOrEqual(take, 1, "역할 몫 1 이상이면 하루 빗물 1 이상");
            state.Calendar.Day = 1;
            var morning = GardenRules.ApplyMorning(state, db, new StateDelta());
            Assert.AreEqual(take, morning.RawUsed);
            Assert.AreEqual((int)System.Math.Round(take * db.Balance.HouseRainPurifyRate, System.MidpointRounding.AwayFromZero), morning.Water, "× 0.75 반올림");
            Assert.AreEqual(5 - take, Res(state, ResourceType.RawWater));
            Assert.AreEqual(morning.Water, Res(state, ResourceType.Water));
            Assert.AreEqual(0, morning.Food, "밭이 없으면 거둘 것도 없다");

            var alone = NewState(db, 10, raw: 5);
            alone.Calendar.Day = 1;
            Assert.AreEqual(0, GardenRules.ApplyMorning(alone, db, new StateDelta()).RawUsed, "살림 동료가 없으면 거르지 않는다");
        }

        [Test]
        public void Greenhouse_GrowsMore_AndHalfInWinter()
        {
            var db = Db();
            var balance = db.Balance;
            var garden = NewState(db, 45);
            garden.Flags.Add(UpkeepRules.FlagRoofGarden);
            Assert.AreEqual(0f, GardenRules.WeeklyYield(garden, balance), "텃밭은 겨울에 자라지 않는다");

            var house = NewState(db, 45);
            house.Flags.Add(UpkeepRules.FlagRoofGarden);
            house.Flags.Add(GardenRules.FlagGreenhouse);
            Assert.AreEqual(balance.GreenhouseFood * balance.GreenhouseWinterRatio, GardenRules.WeeklyYield(house, balance), 1e-4, "비닐하우스는 겨울에 반");
            house.Calendar.Week = 20;
            Assert.AreEqual(balance.GreenhouseFood, GardenRules.WeeklyYield(house, balance), 1e-4);
            Assert.Greater(balance.GreenhouseFood, balance.RoofGardenFood);
        }

        [Test]
        public void Sprinkler_UsesPowerTwoAndRain_ForOneAndAHalf()
        {
            var db = Db();
            var balance = db.Balance;
            var state = NewState(db, 20, raw: 3);
            state.Flags.Add(UpkeepRules.FlagRoofGarden);
            state.Flags.Add(GardenRules.FlagGreenhouse);
            state.Flags.Add(GardenRules.FlagSprinkler);
            state.Calendar.Day = 1;

            // 발전기 복구 전: 멈춰 있다
            Assert.IsTrue(GardenRules.SprinklerStopped(state, balance));
            var dark = GardenRules.Morning(state, db, 3, 0f, 0f, 1);
            Assert.IsFalse(dark.Sprinkled);
            Assert.AreEqual(balance.GreenhouseFood / 7f, dark.Grown, 1e-4);

            state.Base.PowerRestored = true;
            state.Base.Resources[ResourceType.Power] = 3;
            Assert.IsFalse(GardenRules.SprinklerStopped(state, balance), "용량 3이면 스프링클러 2가 돈다");
            Assert.AreEqual(1, PowerRules.Free(state, balance));

            var wet = GardenRules.ApplyMorning(state, db, new StateDelta());
            Assert.IsTrue(wet.Sprinkled);
            Assert.AreEqual(balance.SprinklerRawWaterDaily, wet.RawUsed, "빗물 1을 쓴다");
            Assert.AreEqual(balance.GreenhouseFood / 7f * balance.SprinklerYieldMultiplier, wet.Grown, 1e-4, "그날 ×1.5");
            Assert.AreEqual(2, Res(state, ResourceType.RawWater));

            // 빗물이 없으면 물을 주지 못한다
            var dry = GardenRules.Morning(state, db, 0, 0f, 0f, 2);
            Assert.IsFalse(dry.Sprinkled);
            Assert.AreEqual(0, dry.RawUsed);

            // 냉장고·자동 정수기가 먼저 전력을 쓰면 용량 3으로는 모자란다
            state.Flags.Add("SEARCHED_HOMEEC");
            state.Flags.Add(PurifyRules.FlagAutoPurifier);
            Assert.IsTrue(GardenRules.SprinklerStopped(state, balance), "1 + 1 + 2 > 3");
            Assert.Contains("sprinkler", BaseForecast.Alerts(state, db, null, null, null));
        }
    }
}
