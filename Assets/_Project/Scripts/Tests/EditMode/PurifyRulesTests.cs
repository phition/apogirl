using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>정수 줄 (제작 P5, Docs/CRAFTING_PROPOSAL.md 3.6절): 모래 필터·끓임 정수대·자동 정수기.</summary>
    public sealed class PurifyRulesTests
    {
        private static ActionDef Purify(BalanceConfig balance) => new ActionDef
        {
            Id = balance.PurifyActionId,
            Category = ActionCategory.Survival,
            SlotTag = SlotTag.DayOnly,
            HasCheck = true,
            Check = new CheckDef { BaseRate = 0.75f },
            OnSuccess = new GameEffect { PurifyWater = 12, PurifyRate = 0.8f },
            OnGreatSuccess = new GameEffect { PurifyWater = 12, PurifyRate = 1f },
            OnFailure = new GameEffect { PurifyWater = 12, PurifyRate = 0.5f },
        };

        /// <summary>테스트 콘텐츠의 간단한 정수 행동을 실제와 같은 모양(빗물 12, 성공 0.8·대성공 1·실패 0.5)으로 바꾼 DB.</summary>
        private static InMemoryDefinitionDatabase Db(out ActionDef purify)
        {
            var balance = new BalanceConfig();
            purify = Purify(balance);
            return new InMemoryDefinitionDatabase(balance, TestContent.CreateActions().Where(a => a.Id != balance.PurifyActionId).Concat(new[] { purify }), new[] { TestContent.CreateModel() },
                TestContent.CreateItems(), new[] { TestContent.CreateLootTable() }, new[] { TestContent.CreateLocation() });
        }

        private static GameState NewState(InMemoryDefinitionDatabase db, int raw, int water = 0, int fuel = 0)
        {
            var state = TestContent.NewGame(db, 7);
            state.Base.Resources[ResourceType.RawWater] = raw;
            state.Base.Resources[ResourceType.Water] = water;
            state.Base.Resources[ResourceType.Fuel] = fuel;
            return state;
        }

        private static int Res(GameState state, ResourceType type) => state.Base.GetResource(type);

        [Test]
        public void SandFilter_FailureKeepsMore()
        {
            var db = Db(out var purify);
            var plain = NewState(db, 12);
            Assert.AreEqual(CheckResult.Failure, ActionResolver.Resolve(plain, purify, db, new FixedRandom(0.999), new StateDelta()));
            Assert.AreEqual(6, Res(plain, ResourceType.Water), "필터 없이 실패: 12 × 0.5");

            var filtered = NewState(db, 12);
            filtered.Flags.Add(PurifyRules.FlagSandFilter);
            Assert.AreEqual(CheckResult.Failure, ActionResolver.Resolve(filtered, purify, db, new FixedRandom(0.999), new StateDelta()));
            Assert.AreEqual(8, Res(filtered, ResourceType.Water), "필터가 있으면 실패해도 12 × 0.7");
        }

        [Test]
        public void SandFilter_WeeklyConvertsSomeRawWater()
        {
            var db = Db(out _);
            var state = NewState(db, 10);
            Assert.AreEqual(0, PurifyRules.ApplyWeeklyFilter(state, db.Balance, new StateDelta()), "필터가 없으면 그대로");

            state.Flags.Add(PurifyRules.FlagSandFilter);
            Assert.AreEqual(3, PurifyRules.ApplyWeeklyFilter(state, db.Balance, new StateDelta()), "빗물 4 × 0.75");
            Assert.AreEqual(6, Res(state, ResourceType.RawWater));
            Assert.AreEqual(3, Res(state, ResourceType.Water));
        }

        [Test]
        public void BoilStation_UsesFuel_ForBiggerBatch_AndFallsBackWithoutFuel()
        {
            var db = Db(out var purify);
            var state = NewState(db, 30, fuel: 1);
            state.Flags.Add(PurifyRules.FlagBoilStation);
            var before = ActionResolver.CheckChance(state, purify, db);
            Assert.Greater(before, ActionResolver.CheckChance(NewState(db, 30), purify, db), "연료가 있으면 판정 +");

            Assert.AreEqual(CheckResult.Success, ActionResolver.Resolve(state, purify, db, new FixedRandom(0.5), new StateDelta()));
            Assert.AreEqual(12, Res(state, ResourceType.RawWater), "빗물 18을 거른다");
            Assert.AreEqual(14, Res(state, ResourceType.Water), "18 × 0.8 반올림");
            Assert.AreEqual(0, Res(state, ResourceType.Fuel), "연료 1");

            Assert.AreEqual(CheckResult.Success, ActionResolver.Resolve(state, purify, db, new FixedRandom(0.5), new StateDelta()));
            Assert.AreEqual(0, Res(state, ResourceType.RawWater), "연료가 없으면 예전대로 12");
        }

        [Test]
        public void AutoPurifier_RunsEachMorning_StopsInBlackout_AndUsesFreePower()
        {
            var db = Db(out _);
            var balance = db.Balance;
            var state = NewState(db, 10);
            state.Flags.Add(PurifyRules.FlagAutoPurifier);
            Assert.AreEqual(0, PurifyRules.ApplyMorning(state, balance, new StateDelta()), "발전기 복구 전에는 멈춰 있다");
            Assert.IsTrue(PurifyRules.PurifierStopped(state, balance));

            state.Base.PowerRestored = true;
            state.Base.Resources[ResourceType.Power] = 3;
            state.Flags.Add("SEARCHED_HOMEEC");   // 냉장고
            Assert.AreEqual(1, PowerRules.Free(state, balance), "냉장고 1 + 정수기 1 → 여유 1");
            Assert.AreEqual(3, PurifyRules.ApplyMorning(state, balance, new StateDelta()), "빗물 3 × 0.9 반올림");
            Assert.AreEqual(7, Res(state, ResourceType.RawWater));
            Assert.IsFalse(PurifyRules.PurifierStopped(state, balance));

            state.Flags.Add(PowerRules.FlagBroken);
            Assert.AreEqual(0, PurifyRules.ApplyMorning(state, balance, new StateDelta()), "정전이면 멈춘다");
            Assert.IsTrue(PurifyRules.PurifierStopped(state, balance));
        }

        [Test]
        public void AutoPurifier_WaitsBehindFridge_WhenCapacityIsShort()
        {
            var db = Db(out _);
            var balance = db.Balance;
            var state = NewState(db, 10);
            state.Base.PowerRestored = true;
            state.Base.Resources[ResourceType.Power] = 1;
            state.Flags.Add("SEARCHED_HOMEEC");
            state.Flags.Add(PurifyRules.FlagAutoPurifier);
            Assert.AreEqual(0, PurifyRules.ApplyMorning(state, balance, new StateDelta()), "용량 1은 냉장고가 먼저 쓴다");
        }
    }
}
