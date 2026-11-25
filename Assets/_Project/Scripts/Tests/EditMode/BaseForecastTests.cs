using System;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Random;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>거점 상황판 예상 (BASE_TAB_PROPOSAL P1): 실제 한 주 실행과 같은 값, 난수를 쓰지 않음, 습격 가능성 계산.</summary>
    public sealed class BaseForecastTests
    {
        /// <summary>습격이 오지 않게 (방어가 위협을 크게 앞섬) 맞춘 10주차 거점.</summary>
        private static GameState Quiet(InMemoryDefinitionDatabase db)
        {
            var state = TestContent.NewGame(db, 77);
            state.Calendar.Week = 10;
            state.Base.Defense = 60;
            state.Base.Threat = 0;
            state.Base.Weather = WeatherType.Clear;
            return state;
        }

        private static void RunWeek(GameState state, IDefinitionDatabase db, Action<WeekRunner> afterPrepare = null)
        {
            var runner = new WeekRunner(state, db, new SeededRandom(state.Rng), new StubExpeditionRunner());
            runner.PrepareWeek();
            afterPrepare?.Invoke(runner);
            while (state.Status == GameStatus.Playing && !runner.IsWeekComplete)
            {
                runner.ExecuteNextSlot();
            }

            runner.FinishWeek();
        }

        [Test]
        public void FoodAndWater_MatchOneWeekOfExecution()
        {
            var db = TestContent.CreateDatabase();
            var state = Quiet(db);
            state.Base.Resources[ResourceType.Food] = 75;
            state.Base.Resources[ResourceType.Water] = 30;
            state.Base.Resources[ResourceType.RawWater] = 4;
            state.Base.Weather = WeatherType.Rain;
            state.Flags.Add(UpkeepRules.FlagRoofGarden);
            state.Flags.Add(UpkeepRules.FlagRainBarrels);
            state.Flags.Add(PurifyRules.FlagSandFilter);

            SupplyForecast food = null;
            SupplyForecast water = null;
            RunWeek(state, db, _ =>
            {
                food = BaseForecast.Food(state, db);
                water = BaseForecast.Water(state, db);
            });

            Assert.Greater(food.Garden, 0, "텃밭이 셈에 들어간다");
            Assert.Greater(food.Spoil, 0, "문턱을 넘는 식량은 상한다");
            Assert.Greater(water.RawGain, db.Balance.WeeklyRawWater, "비 오는 주의 빗물");
            Assert.AreEqual(food.End, state.Base.GetResource(ResourceType.Food), "식량 예상 = 실제");
            Assert.AreEqual(water.End, state.Base.GetResource(ResourceType.Water), "식수 예상 = 실제");
            Assert.AreEqual(water.RawEnd, state.Base.GetResource(ResourceType.RawWater), "빗물 예상 = 실제");
        }

        [Test]
        public void Shortage_FoundOnTheRightDay()
        {
            var db = TestContent.CreateDatabase();
            var state = Quiet(db);
            var perDay = UpkeepRules.DietUnits(state.FoodDiet, db.Balance) * UpkeepRules.Headcount(state, db.Balance);
            state.Base.Resources[ResourceType.Food] = perDay * 3;   // 사흘치
            var runner = new WeekRunner(state, db, new SeededRandom(state.Rng), new StubExpeditionRunner());
            runner.PrepareWeek();
            var food = BaseForecast.Food(state, db);
            Assert.AreEqual(4, food.ShortDay, "나흘째 아침부터 모자란다");
            Assert.Contains("food_short", BaseForecast.Alerts(state, db, null, food, null));
        }

        [Test]
        public void Forecast_DoesNotTouchRandomOrState()
        {
            var db = TestContent.CreateDatabase();
            var state = Quiet(db);
            state.Base.Threat = 70;
            state.Base.Defense = 10;
            state.Base.Traps = 2;
            var rng = state.Rng.State;
            var traps = state.Base.Traps;
            var food = state.Base.GetResource(ResourceType.Food);

            BaseForecast.Raid(state, db);
            BaseForecast.Food(state, db);
            BaseForecast.Water(state, db);
            BaseForecast.Power(state, db.Balance);

            Assert.AreEqual(rng, state.Rng.State, "난수를 쓰지 않는다");
            Assert.AreEqual(traps, state.Base.Traps, "함정을 쓰지 않는다");
            Assert.AreEqual(food, state.Base.GetResource(ResourceType.Food));
        }

        [Test]
        public void Raid_CountsHomeNightsOnly_AndUsesRaidChance()
        {
            var db = TestContent.CreateDatabase();
            var state = Quiet(db);
            state.Base.Threat = 80;
            state.Base.Defense = 10;
            var runner = new WeekRunner(state, db, new SeededRandom(state.Rng), new StubExpeditionRunner());
            runner.PrepareWeek();
            state.Plan.Slots[CalendarRules.SlotIndex(2, DayPhase.Night)].ActionId = TestContent.NightExpedition;

            var raid = BaseForecast.Raid(state, db);
            var p = RaidRules.Chance(state, db.Balance, db);
            Assert.AreEqual(6, raid.NightsLeft);
            Assert.AreEqual(1, raid.NightsAway, "밤 탐험 칸은 거점에 없다");
            Assert.AreEqual(1.0 - Math.Pow(1.0 - p, 6), raid.WeekChance, 1e-5);
            Assert.AreEqual(RiskLevel.High, raid.Level, "위협이 방어보다 70 높으면 높음");
            Assert.IsTrue(raid.Reasons.Exists(r => r.Key == "gap_over" && r.Value == 70));

            state.Calendar.Week = db.Balance.RaidStartWeek - 1;
            Assert.IsTrue(BaseForecast.Raid(state, db).NotYet, "습격이 시작되기 전");
        }

        [Test]
        public void RepelChance_EdgesAndTrap()
        {
            var db = TestContent.CreateDatabase();
            var state = Quiet(db);
            Assert.AreEqual(1f, BaseForecast.RepelChance(state, db), 1e-6, "위협 0, 방어 60이면 늘 막는다");

            state.Base.Threat = 100;
            state.Base.Defense = 0;
            var weak = BaseForecast.RepelChance(state, db);
            state.Base.Traps = 1;
            var withTrap = BaseForecast.RepelChance(state, db);
            Assert.Less(weak, 0.5f);
            Assert.Greater(withTrap, weak, "함정이 있으면 더 잘 막는다");
        }
    }
}
