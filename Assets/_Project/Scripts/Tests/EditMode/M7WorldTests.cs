using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Random;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>탐험 상자(기본 빈 상자·매달 복구·흔적), 낮 침입(깡패), 거점 방어 작업.</summary>
    public sealed class M7WorldTests
    {
        // ── 상자 ──

        [Test]
        public void BaselineEmpty_IsAboutThirtyPercent_AndFixedPerWorld()
        {
            var keys = Enumerable.Range(0, 3000).Select(i => "LOC_X/R" + i + ".0").ToList();
            var empty = keys.Count(k => ContainerRules.BaselineEmpty(77, k, 0.3f));
            Assert.That(empty / 3000f, Is.InRange(0.27f, 0.33f));
            Assert.IsTrue(keys.All(k => ContainerRules.BaselineEmpty(77, k, 0.3f) == ContainerRules.BaselineEmpty(77, k, 0.3f)), "같은 판이면 같은 상자");
            Assert.AreNotEqual(empty, keys.Count(k => ContainerRules.BaselineEmpty(78, k, 0.3f)), "판이 다르면 다른 상자");
            Assert.IsFalse(keys.Any(k => ContainerRules.BaselineEmpty(77, k, 0f)));
        }

        [Test]
        public void Restock_RefillsHalfOfLootedOnce_AMonth_ExceptNonRestockingAreas_AndLeavesTrace()
        {
            var db = TestContent.CreateDatabase();
            Assert.IsTrue(db.TryGetLocation(TestContent.Location, out var location));
            var state = TestContent.NewGame(db);
            var looted = HostileRules.AllContainerKeys(location);
            state.Exploration.LootedContainers.UnionWith(looted);

            var restocked = ContainerRules.Restock(state, db, new SeededRandom(SeededRandom.CreateState(3)), db.Balance);
            CollectionAssert.AreEqual(new[] { TestContent.Location }, restocked);
            Assert.AreEqual(5, state.Exploration.LootedContainers.Count, "뒤진 10개 중 절반 복구");
            Assert.IsTrue(state.Flags.Contains(ContainerRules.RestockFlag(TestContent.Location)));

            location.Restocks = false;
            ContainerRules.Restock(state, db, new SeededRandom(SeededRandom.CreateState(4)), db.Balance);
            Assert.AreEqual(5, state.Exploration.LootedContainers.Count, "교실동처럼 복구 안 하는 지역");

            var months = Enumerable.Range(1, 51).Count(w => ContainerRules.MonthStarted(w, w + 1, db.Balance));
            Assert.That(months, Is.InRange(11, 12), "1년에 매달 한 번");

            // 요청에 '흔적'이 실리고, 다녀오면 지워진다
            var request = ExpeditionRequestFactory.Create(state, db.GetAction(TestContent.DayExpedition), 5, false, db);
            Assert.IsTrue(request.Restocked);
            var stub = new StubExpeditionRunner();
            state.Plan.Slots[2].ActionId = TestContent.DayExpedition;
            var runner = new WeekRunner(state, db, new SeededRandom(state.Rng), stub);
            runner.PrepareWeek();
            while (!runner.IsWeekComplete && !runner.IsGameOver) runner.ExecuteNextSlot();
            Assert.IsTrue(stub.Requests.Single().Restocked);
            Assert.IsFalse(state.Flags.Contains(ContainerRules.RestockFlag(TestContent.Location)), "흔적은 한 번만");
        }

        // ── 낮 침입 ──

        private static GameState IntrusionState(InMemoryDefinitionDatabase db)
        {
            var state = TestContent.NewGame(db);
            state.Calendar.Week = 30;
            state.Base.SetResource(ResourceType.Food, 100);
            state.Base.SetResource(ResourceType.Water, 100);
            state.Base.SetResource(ResourceType.Medicine, 20);
            state.Npcs["NPC_HELPER"] = new NpcState { Status = NpcStatus.Joined, Role = "GUARD" };
            return state;
        }

        [Test]
        public void Intrusion_FightLostWhenUnprepared_TakesSuppliesAndInjuresCompanion()
        {
            var db = TestContent.CreateDatabase(b => { b.IntrusionStrength = 500; b.IntrusionStrengthVariance = 0; });
            var state = IntrusionState(db);
            var result = IntrusionRules.Resolve(state, db, IntrusionRules.Fight, db.Balance, new SeededRandom(SeededRandom.CreateState(1)), new StateDelta());

            Assert.IsFalse(result.Held);
            Assert.AreEqual(75, state.Base.GetResource(ResourceType.Food), "25% 빼앗김");
            Assert.AreEqual("NPC_HELPER", result.InjuredNpcId);
            Assert.AreEqual(db.Balance.IntrusionInjuryWeeks, state.Npcs["NPC_HELPER"].InjuredWeeks);
            Assert.AreEqual(1, state.Exploration.IntrusionsLost);

            IntrusionRules.HealWeekly(state);
            IntrusionRules.HealWeekly(state);
            Assert.AreEqual(0, state.Npcs["NPC_HELPER"].InjuredWeeks, "두 주 뒤 회복");
        }

        [Test]
        public void Intrusion_PreparedFightHolds_TrapIsUsed_YieldCostsLessWithoutInjury()
        {
            var db = TestContent.CreateDatabase(b => { b.IntrusionStrength = 0; b.IntrusionStrengthVariance = 0; });
            var state = IntrusionState(db);
            state.Base.Traps = 2;
            var held = IntrusionRules.Resolve(state, db, IntrusionRules.Fight, db.Balance, new SeededRandom(SeededRandom.CreateState(2)), new StateDelta());
            Assert.IsTrue(held.Held);
            Assert.AreEqual(100, state.Base.GetResource(ResourceType.Food));
            Assert.AreEqual(1, state.Base.Traps, "함정 하나를 썼다");

            var yielded = IntrusionRules.Resolve(state, db, IntrusionRules.Yield, db.Balance, new SeededRandom(SeededRandom.CreateState(3)), new StateDelta());
            Assert.AreEqual(85, state.Base.GetResource(ResourceType.Food), "15% 내줌");
            Assert.IsEmpty(yielded.InjuredNpcId);
        }

        [Test]
        public void Intrusion_CancelsDaySlot_AndForcesTheEvent()
        {
            var db = M6Act4Tests.ShippedDb();
            db.Balance.IntrusionWeeklyChanceAct3 = 0.99f;
            var state = NewGameFactory.Create(db.Balance, 9);
            state.Calendar.Week = 30;
            var outcomes = new List<SlotOutcome>();
            var runner = new WeekRunner(state, db, new SeededRandom(state.Rng), new StubExpeditionRunner());
            runner.SlotResolved += outcomes.Add;
            runner.PrepareWeek();
            while (!runner.IsWeekComplete && !runner.IsGameOver) runner.ExecuteNextSlot();

            var intrusion = outcomes.FirstOrDefault(o => o.IntrusionTriggered);
            Assert.IsNotNull(intrusion, "주 99%면 이번 주에 침입이 있다");
            Assert.AreEqual(DayPhase.Day, intrusion.Phase);
            Assert.AreEqual(db.Balance.IntrusionActionId, intrusion.ActionId, "그날 낮 행동은 취소");
            Assert.IsNotNull(intrusion.Intrusion, "침입 이벤트에서 대응을 골랐다");
            Assert.IsTrue(state.Events.Records.ContainsKey(db.Balance.IntrusionEventId));
        }

        // ── 거점 방어 작업 ──

        [Test]
        public void DefenseWorks_RaiseCap_AndCoversSlowThreat_AndAlarmAddsPower()
        {
            var balance = new BalanceConfig();
            var state = NewGameFactory.Create(balance, 1);
            EffectApplier.ChangeDefense(state, 100, balance, new StateDelta());
            Assert.AreEqual(balance.DefenseMax, state.Base.Defense);
            state.Flags.Add(DefenseRules.FlagReinforced);
            EffectApplier.ChangeDefense(state, 100, balance, new StateDelta());
            Assert.AreEqual(balance.ReinforcedDefenseMax, state.Base.Defense, "출입구 보강: 상한 80");

            state.Base.Threat = 0;
            EffectApplier.ChangeThreat(state, 10, balance, new StateDelta());
            Assert.AreEqual(10, state.Base.Threat);
            state.Flags.Add(DefenseRules.FlagWindowCovers);
            EffectApplier.ChangeThreat(state, 10, balance, new StateDelta());
            Assert.AreEqual(17, state.Base.Threat, "가림막: 위협 증가 ×0.7");

            Assert.AreEqual(0, DefenseRules.ExtraPower(state, balance, false));
            state.Flags.Add(DefenseRules.FlagAlarm);
            Assert.AreEqual(balance.AlarmPower, DefenseRules.ExtraPower(state, balance, false));

            EffectApplier.Apply(state, new GameEffect { Traps = 5 }, balance, new StateDelta());
            Assert.AreEqual(balance.MaxTraps, state.Base.Traps, "함정은 상한까지");
        }
    }
}
