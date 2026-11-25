using System;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Events;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>M3-1: 조건 엔진, 이벤트 스케줄러·선택, 주간 진행 연동, NPC.</summary>
    public sealed class EventSystemTests
    {
        private static InMemoryDefinitionDatabase Db(IEnumerable<EventDef> events, Action<BalanceConfig> tweak = null, IEnumerable<NpcDef> npcs = null)
        {
            var balance = new BalanceConfig();
            tweak?.Invoke(balance);
            return new InMemoryDefinitionDatabase(
                balance, TestContent.CreateActions(), new[] { TestContent.CreateModel() }, TestContent.CreateItems(),
                new[] { TestContent.CreateLootTable() }, new[] { TestContent.CreateLocation() }, null, events, npcs);
        }

        private static EventContext DayRest(int day = 1) => new EventContext
        {
            Phase = DayPhase.Day, Day = day, ActionId = TestContent.RestDay, ActionCategory = ActionCategory.Rest,
        };

        private static EventDef Story(string id, int priority = 0, ConditionDef condition = null) => new EventDef
        {
            Id = id, Kind = EventKind.Story, Priority = priority, Condition = condition ?? new ConditionDef(),
        };

        // ── 달력 ──

        [Test]
        public void DateOf_StartsMid_March_AndWrapsYear()
        {
            var balance = new BalanceConfig();
            Assert.AreEqual((3, 16), CalendarRules.DateOf(1, 1, balance));
            Assert.AreEqual((4, 1), CalendarRules.DateOf(3, 3, balance));
            Assert.AreEqual((3, 14), CalendarRules.DateOf(52, 7, balance), "52주 끝 = 다음 해 3월 중순");
            var graduation = CalendarRules.DateOf(48, 1, balance);
            Assert.AreEqual(2, graduation.Month, "47~48주차 = 2월 (졸업식)");
        }

        // ── 조건 ──

        [Test]
        public void Condition_WeekPhaseFlagsAndChecks()
        {
            var db = TestContent.CreateDatabase();
            var state = TestContent.NewGame(db);
            state.Calendar.Week = 5;
            var condition = new ConditionDef
            {
                WeekMin = 3, WeekMax = 6, Phase = PhaseFilter.Day,
                FlagsAll = { "A" }, FlagsNone = { "B" },
                Checks = { new ValueCheck(ValueKind.Stat, "Intellect", CompareOp.GreaterOrEqual, 10) },
            };

            Assert.IsFalse(ConditionEvaluator.IsMet(state, condition, DayRest(), db.Balance), "플래그 A 없음");
            state.Flags.Add("A");
            Assert.IsTrue(ConditionEvaluator.IsMet(state, condition, DayRest(), db.Balance));

            var night = DayRest();
            night.Phase = DayPhase.Night;
            Assert.IsFalse(ConditionEvaluator.IsMet(state, condition, night, db.Balance), "낮 전용");

            state.Flags.Add("B");
            Assert.IsFalse(ConditionEvaluator.IsMet(state, condition, DayRest(), db.Balance), "금지 플래그");
            state.Flags.Remove("B");

            state.Calendar.Week = 7;
            Assert.IsFalse(ConditionEvaluator.IsMet(state, condition, DayRest(), db.Balance), "주차 범위 밖");
        }

        [Test]
        public void Condition_AnyChecks_DateAndActionCategory()
        {
            var db = TestContent.CreateDatabase();
            var state = TestContent.NewGame(db);
            var any = new ConditionDef
            {
                AnyChecks =
                {
                    new ValueCheck(ValueKind.Resource, "Food", CompareOp.Less, 0),
                    new ValueCheck(ValueKind.SleepDebt, "", CompareOp.Equal, 0),
                },
            };
            Assert.IsTrue(ConditionEvaluator.IsMet(state, any, DayRest(), db.Balance), "둘 중 하나 참");

            var date = new ConditionDef { Month = 3, DayOfMonth = 18 };
            Assert.IsFalse(ConditionEvaluator.IsMet(state, date, DayRest(1), db.Balance));
            Assert.IsTrue(ConditionEvaluator.IsMet(state, date, DayRest(3), db.Balance), "1주차 3일 = 3월 18일");

            var nightRest = new ConditionDef { ActionCategories = { ActionCategory.Education } };
            Assert.IsFalse(ConditionEvaluator.IsMet(state, nightRest, DayRest(), db.Balance), "휴식 슬롯이라 불일치");
        }

        [Test]
        public void Condition_NpcStatusAndAffinity()
        {
            var db = TestContent.CreateDatabase();
            var state = TestContent.NewGame(db);
            var condition = new ConditionDef
            {
                NpcStatuses = { new NpcStatusCheck { NpcId = "NPC_X", Status = NpcStatus.Joined } },
                Checks = { new ValueCheck(ValueKind.NpcAffinity, "NPC_X", CompareOp.GreaterOrEqual, 10) },
            };
            Assert.IsFalse(ConditionEvaluator.IsMet(state, condition, DayRest(), db.Balance));

            var effect = new GameEffect { Npcs = { new NpcChange { NpcId = "NPC_X", Affinity = 12, SetStatus = NpcStatus.Joined, SetRole = "guard" } } };
            var delta = new StateDelta();
            EffectApplier.Apply(state, effect, db.Balance, delta);
            Assert.IsTrue(ConditionEvaluator.IsMet(state, condition, DayRest(), db.Balance));
            Assert.AreEqual("guard", state.Npcs["NPC_X"].Role);
            var gain = (int)System.Math.Round(12 * db.Balance.CompanionAffinityGainScale);   // 이벤트로 오르는 동료 호감은 배율 (기획 C.4)
            Assert.AreEqual(db.Balance.JoinAffinity + gain, state.Npcs["NPC_X"].Affinity, "합류하면 호감 바닥 JoinAffinity 위에 장면의 호감을 더한다");
            Assert.AreEqual(db.Balance.JoinAffinity + gain, delta.NpcAffinity["NPC_X"]);

            var again = new StateDelta();
            EffectApplier.Apply(state, new GameEffect { Npcs = { new NpcChange { NpcId = "NPC_X", Affinity = 4, SetStatus = NpcStatus.Joined } } }, db.Balance, again);
            Assert.AreEqual((int)System.Math.Round(4 * db.Balance.CompanionAffinityGainScale), again.NpcAffinity["NPC_X"], "이미 합류한 동료는 바닥을 다시 적용하지 않는다");

            EffectApplier.Apply(state, new GameEffect { Npcs = { new NpcChange { NpcId = EffectApplier.TeacherNpcId, Affinity = 5 } } }, db.Balance, delta);
            Assert.AreEqual(5, state.Teacher.Affinity, "NPC_TEACHER = 윤 선생님 호감");
        }

        [Test]
        public void Effect_ClearsFlags_AddsCluesAndItems()
        {
            var db = TestContent.CreateDatabase();
            var state = TestContent.NewGame(db);
            state.Flags.Add("OLD");
            var delta = new StateDelta();
            EffectApplier.Apply(state, new GameEffect
            {
                ClearFlags = { "OLD" }, AddClues = { "CLUE_RADIO" }, Items = { new ItemGrant("ITEM_TEST", 2) },
            }, db.Balance, delta);

            Assert.IsFalse(state.Flags.Contains("OLD"));
            Assert.IsTrue(state.Clues.Contains("CLUE_RADIO"));
            Assert.AreEqual(2, state.Storage.Where(s => s.ItemId == "ITEM_TEST").Sum(s => s.Count));
            CollectionAssert.AreEqual(new[] { "OLD" }, delta.FlagsCleared);
        }

        // ── 스케줄러 ──

        [Test]
        public void Scheduler_PicksHighestPriority_AndOnceEventsDoNotRepeat()
        {
            var db = Db(new[] { Story("EVT_LOW", 1), Story("EVT_HIGH", 5) });
            var state = TestContent.NewGame(db);
            var random = new FixedRandom();

            var first = EventScheduler.Pick(state, db, DayRest(), random);
            Assert.AreEqual("EVT_HIGH", first.Id);
            EventResolver.Begin(state, first, DayRest(), db.Balance, new StateDelta());
            Assert.AreEqual("EVT_LOW", EventScheduler.Pick(state, db, DayRest(), random).Id, "1회성은 다시 안 나옴");
        }

        [Test]
        public void Scheduler_RepeatableEvent_WaitsCooldown()
        {
            var ev = Story("EVT_REPEAT");
            ev.Once = false;
            ev.CooldownWeeks = 2;
            var db = Db(new[] { ev });
            var state = TestContent.NewGame(db);
            EventResolver.Begin(state, ev, DayRest(), db.Balance, new StateDelta());

            Assert.IsNull(EventScheduler.Pick(state, db, DayRest(), new FixedRandom()));
            state.Calendar.Week += 2;
            Assert.IsNotNull(EventScheduler.Pick(state, db, DayRest(), new FixedRandom()));
        }

        [Test]
        public void Scheduler_RandomEvents_RespectChanceAndQuotas()
        {
            var ev = new EventDef { Id = "EVT_RANDOM", Kind = EventKind.Random, Once = false };
            var db = Db(new[] { ev }, b =>
            {
                b.RandomEventChance = 0.5f;
                b.RandomEventsPerWeek = 2;
            });
            var state = TestContent.NewGame(db);

            Assert.IsNull(EventScheduler.Pick(state, db, DayRest(1), new FixedRandom(0.9)), "확률 실패");
            var picked = EventScheduler.Pick(state, db, DayRest(1), new FixedRandom(0.1, 0.0));
            Assert.AreEqual("EVT_RANDOM", picked.Id);

            EventResolver.Begin(state, picked, DayRest(1), db.Balance, new StateDelta());
            Assert.IsNull(EventScheduler.Pick(state, db, DayRest(1), new FixedRandom(0.0, 0.0)), "하루 1개");

            EventResolver.Begin(state, picked, DayRest(2), db.Balance, new StateDelta());
            Assert.IsNull(EventScheduler.Pick(state, db, DayRest(3), new FixedRandom(0.0, 0.0)), "주 2개");

            state.Calendar.Week++;
            Assert.IsNotNull(EventScheduler.Pick(state, db, DayRest(1), new FixedRandom(0.0, 0.0)), "다음 주엔 초기화");
        }

        [Test]
        public void Scheduler_WithoutRandomCandidates_DoesNotConsumeRandom()
        {
            var db = Db(new[] { Story("EVT_LATER", 0, new ConditionDef { WeekMin = 10 }) });
            var state = TestContent.NewGame(db);
            var random = new FixedRandom(0.123);
            Assert.IsNull(EventScheduler.Pick(state, db, DayRest(), random));
            Assert.AreEqual(0.123, random.NextDouble(), "난수열 보존");
        }

        // ── 선택 ──

        [Test]
        public void Choices_HideByCondition_DisableByCost_PayCostOnChoose()
        {
            var ev = Story("EVT_CHOICE");
            ev.Choices.Add(new EventChoiceDef { Id = "PAY", Costs = { new ResourceAmount(ResourceType.Food, 3) }, Effect = { Hidden = { new HiddenAmount(HiddenType.Hope, 5) } } });
            ev.Choices.Add(new EventChoiceDef { Id = "SECRET", Condition = new ConditionDef { FlagsAll = { "KNOWS" } } });
            ev.Choices.Add(new EventChoiceDef { Id = "PASS" });
            var db = Db(new[] { ev });
            var state = TestContent.NewGame(db);

            var views = EventResolver.Choices(state, ev, DayRest(), db.Balance);
            CollectionAssert.AreEqual(new[] { "PAY", "PASS" }, views.Select(v => v.Id).ToArray());
            Assert.IsTrue(views[0].Enabled);

            var food = state.Base.GetResource(ResourceType.Food);
            var delta = new StateDelta();
            var result = EventResolver.Choose(state, ev, "PAY", DayRest(), db.Balance, new FixedRandom(), delta);
            Assert.AreEqual("PAY", result.ChoiceId);
            Assert.AreEqual(food - 3, state.Base.GetResource(ResourceType.Food));
            Assert.AreEqual(5, state.Player.Hidden.Hope);
            CollectionAssert.Contains(state.Events.ChoiceLog, "EVT_CHOICE:PAY");

            state.Base.SetResource(ResourceType.Food, 1);
            Assert.IsFalse(EventResolver.Choices(state, ev, DayRest(), db.Balance)[0].Enabled, "식량 부족 → 비활성");
            Assert.Throws<ArgumentException>(() => EventResolver.Choose(state, ev, "PAY", DayRest(), db.Balance, new FixedRandom(), new StateDelta()));
            Assert.Throws<ArgumentException>(() => EventResolver.Choose(state, ev, "SECRET", DayRest(), db.Balance, new FixedRandom(), new StateDelta()));
        }

        [Test]
        public void CheckedChoice_FailureAppliesOnFailure()
        {
            var ev = Story("EVT_CHECK");
            ev.Choices.Add(new EventChoiceDef
            {
                Id = "TRY", HasCheck = true, Check = new CheckDef { BaseRate = 0f },
                Effect = { SetFlags = { "WON" } }, OnFailure = { SetFlags = { "LOST" } },
            });
            var db = Db(new[] { ev });
            var state = TestContent.NewGame(db);

            var result = EventResolver.Choose(state, ev, "TRY", DayRest(), db.Balance, new FixedRandom(0.99, 0.99), new StateDelta());
            Assert.AreEqual(CheckResult.Failure, result.Result);
            Assert.IsTrue(state.Flags.Contains("LOST"));
            Assert.IsFalse(state.Flags.Contains("WON"));
        }

        // ── 주간 진행 연동 ──

        [Test]
        public void Runner_DeferredEvent_BlocksUntilResolved_AndChains()
        {
            var first = Story("EVT_FIRST", 0, new ConditionDef { Days = { 1 }, Phase = PhaseFilter.Day });
            first.OnShow.SetFlags.Add("SEEN_FIRST");
            first.Choices.Add(new EventChoiceDef { Id = "GO", NextEventId = "EVT_SECOND", Effect = { Hidden = { new HiddenAmount(HiddenType.Hope, 3) } } });
            first.Choices.Add(new EventChoiceDef { Id = "STAY" });
            var second = Story("EVT_SECOND", 0, new ConditionDef { WeekMin = 99 });
            var db = Db(new[] { first, second });
            var state = TestContent.NewGame(db);
            var runner = new WeekRunner(state, db, new FixedRandom(), new StubExpeditionRunner()) { DeferEvents = true };

            var outcome = runner.ExecuteNextSlot();
            Assert.IsTrue(outcome.AwaitingEvent);
            Assert.AreEqual("EVT_FIRST", runner.PendingEvent.Id);
            Assert.IsTrue(state.Flags.Contains("SEEN_FIRST"), "OnShow 즉시 적용");
            Assert.Throws<InvalidOperationException>(() => runner.ExecuteNextSlot());

            runner.ResolveEvent("GO");
            Assert.IsTrue(outcome.AwaitingEvent, "연쇄 이벤트 대기");
            Assert.AreEqual("EVT_SECOND", runner.PendingEvent.Id, "연쇄는 조건 무시");

            runner.ResolveEvent(EventResolver.ContinueChoiceId);
            Assert.IsFalse(outcome.AwaitingEvent);
            Assert.AreEqual(2, outcome.Events.Count);
            Assert.AreEqual(3, outcome.Delta.Hidden[HiddenType.Hope], "이벤트 효과도 슬롯 변화량에 기록");
            Assert.AreEqual(DayPhase.Night, state.Calendar.Phase, "슬롯은 이미 넘어감");
            Assert.DoesNotThrow(() => runner.ExecuteNextSlot());
        }

        [Test]
        public void Runner_NotDeferred_AutoChoosesFirstEnabled()
        {
            var ev = Story("EVT_AUTO", 0, new ConditionDef { Phase = PhaseFilter.Night, ActionCategories = { ActionCategory.Rest } });
            ev.Choices.Add(new EventChoiceDef { Id = "A", Effect = { SetFlags = { "AUTO_A" } } });
            ev.Choices.Add(new EventChoiceDef { Id = "B" });
            var db = Db(new[] { ev });
            var state = TestContent.NewGame(db);
            var runner = new WeekRunner(state, db, new FixedRandom(), new StubExpeditionRunner());

            var day = runner.ExecuteNextSlot();
            Assert.AreEqual(0, day.Events.Count, "낮에는 조건 불일치");
            var night = runner.ExecuteNextSlot();
            Assert.IsFalse(night.AwaitingEvent);
            Assert.AreEqual("A", night.Events.Single().ChoiceId);
            Assert.IsTrue(state.Flags.Contains("AUTO_A"));
        }

        [Test]
        public void FullWeek_WithEvents_FinishesNormally()
        {
            var random = new EventDef { Id = "EVT_R", Kind = EventKind.Crisis, Once = false, OnShow = { Resources = { new ResourceAmount(ResourceType.Food, -1) } } };
            var db = Db(new[] { random }, b => b.RandomEventChance = 1f);
            var state = TestContent.NewGame(db);
            var runner = new WeekRunner(state, db, new FixedRandom(), new StubExpeditionRunner());
            for (var i = 0; i < WeekPlan.SlotCount; i++)
            {
                runner.ExecuteNextSlot();
            }

            Assert.AreEqual(3, state.Events.Records["EVT_R"].Count, "주 상한 3");
            var summary = runner.FinishWeek();
            Assert.AreEqual(3, summary.Slots.Sum(s => s.Events.Count));
        }
    }
}
