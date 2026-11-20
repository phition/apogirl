using System;
using System.IO;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Random;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using ApoGirl.Systems.Save;
using ApoGirl.Systems.Session;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>M7-3: 난이도 배율, 이야기 중심의 주간 되돌리기·자동 수색.</summary>
    public sealed class M7DifficultyTests
    {
        [Test]
        public void Consumption_ScalesDailyFoodAndWater()
        {
            var balance = new BalanceConfig();
            int Week(float m) => Enumerable.Range(1, 7).Sum(d => UpkeepRules.DailyFoodNeed(10, d, balance, m));
            Assert.AreEqual(14, Week(1f));
            Assert.AreEqual(10, Week(0.75f), "이야기 중심 ×0.75 → 주 10");
            Assert.AreEqual(17, Week(1.25f), "하드 ×1.25 → 주 17");
            Assert.AreEqual(22, Enumerable.Range(1, 7).Sum(d => UpkeepRules.DailyFoodNeed(45, d, balance, 1.25f)), "겨울과 겹치면 2 × 1.3 × 1.25");

            var state = NewGameFactory.Create(balance, 1);
            state.Difficulty = DifficultyRules.Story;
            state.Base.SetResource(ResourceType.Water, 100);
            for (var d = 1; d <= 7; d++)
            {
                state.Calendar.Day = d;
                UpkeepRules.ApplyDaily(state, balance, new StateDelta());
            }

            Assert.AreEqual(90, state.Base.GetResource(ResourceType.Water), "물도 ×0.75");
        }

        [Test]
        public void HumanThreat_AndHarm_FollowDifficulty()
        {
            var db = TestContent.CreateDatabase();
            var state = TestContent.NewGame(db);
            state.Calendar.Week = 20;
            state.Base.Threat = 60;
            var standard = RaidRules.Chance(state, db.Balance);
            state.Difficulty = DifficultyRules.Story;
            Assert.AreEqual(standard * 0.7f, RaidRules.Chance(state, db.Balance), 1e-5);
            state.Difficulty = DifficultyRules.Hard;
            Assert.AreEqual(standard * 1.3f, RaidRules.Chance(state, db.Balance), 1e-5);

            var request = ExpeditionRequestFactory.Create(state, db.GetAction(TestContent.DayExpedition), 3, false, db);
            Assert.AreEqual(1.4f, request.HarmMultiplier, 1e-5);
            Assert.AreEqual(1.3f, request.HumanThreatMultiplier, 1e-5);

            // 헤드리스 쓰러짐: 배율 0이면 쓰러지지 않는다
            db.GetExpeditionModel(TestContent.Location).Day.BaseDownChance = 0.9f;
            request.HarmMultiplier = 0f;
            Assert.IsTrue(Enumerable.Range(1, 30).All(s =>
            {
                request.Seed = s;
                return new HeadlessExpeditionRunner(db).Run(request).Status != ExtractStatus.Down;
            }));
        }

        [Test]
        public void Hard_ReducesExpeditionYield()
        {
            var db = TestContent.CreateDatabase();
            var state = TestContent.NewGame(db);
            state.Difficulty = DifficultyRules.Hard;
            state.Base.SetResource(ResourceType.Food, 0);
            state.Plan.Slots[2].ActionId = TestContent.DayExpedition;
            var stub = new StubExpeditionRunner
            {
                Handler = _ => new ExpeditionResult { Status = ExtractStatus.Extracted, ResourceLoot = { new ResourceAmount(ResourceType.Food, 20) } },
            };
            var runner = new WeekRunner(state, db, new SeededRandom(state.Rng), stub);
            runner.PrepareWeek();
            runner.ExecuteNextSlot();
            runner.ExecuteNextSlot();
            runner.ExecuteNextSlot();
            Assert.AreEqual(17, state.Base.GetResource(ResourceType.Food), "20 × 0.85");
        }

        private static GameSession Session(string directory, string difficulty, out InMemoryDefinitionDatabase db)
        {
            db = TestContent.CreateDatabase();
            var session = new GameSession(db, new SaveService(directory, new GameStateSerializer()));
            session.InteractiveEvents = false;
            session.PendingDifficulty = difficulty;
            session.NewGame(5);
            return session;
        }

        private static void RunWeek(GameSession session)
        {
            session.BeginWeek();
            while (session.IsExecuting)
            {
                var outcome = session.Step();
                if (outcome.AwaitingExpedition)
                {
                    session.DispatchExpedition();
                }
            }
        }

        [Test]
        public void Story_UndoesEachWeekOnce_StandardCannot()
        {
            var directory = Path.Combine(Path.GetTempPath(), "ApoGirlUndo_" + Guid.NewGuid().ToString("N"));
            try
            {
                var session = Session(directory, DifficultyRules.Story, out _);
                Assert.AreEqual(DifficultyRules.Story, session.State.Difficulty);
                Assert.IsFalse(session.CanUndoWeek, "아직 실행한 주가 없다");

                RunWeek(session);
                Assert.AreEqual(2, session.State.Calendar.Week);
                Assert.IsTrue(session.CanUndoWeek);
                Assert.IsTrue(session.UndoWeek());
                Assert.AreEqual(1, session.State.Calendar.Week, "1주차 시작으로");
                Assert.IsTrue(session.IsPlanning, "계획을 다시 짤 수 있다");

                RunWeek(session);
                Assert.IsFalse(session.CanUndoWeek, "같은 주는 한 번만");
                RunWeek(session);
                Assert.IsTrue(session.CanUndoWeek, "다음 주는 다시 되돌릴 수 있다");

                var standard = Session(directory, DifficultyRules.Standard, out _);
                RunWeek(standard);
                Assert.IsFalse(standard.CanUndoWeek, "표준에는 되돌리기가 없다");
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        [Test]
        public void Story_DispatchesExpedition_WithReducedYield()
        {
            var directory = Path.Combine(Path.GetTempPath(), "ApoGirlDispatch_" + Guid.NewGuid().ToString("N"));
            try
            {
                var session = Session(directory, DifficultyRules.Story, out var db);
                db.GetExpeditionModel(TestContent.Location).Day.BaseDownChance = 0f;
                session.UseRealtimeExpeditions = true;
                session.State.Plan.Slots[2].ActionId = TestContent.DayExpedition;
                session.BeginWeek();
                SlotOutcome outcome = null;
                while (session.IsExecuting && (outcome == null || !outcome.AwaitingExpedition))
                {
                    outcome = session.Step();
                }

                Assert.IsTrue(outcome.AwaitingExpedition);
                Assert.IsTrue(session.CanDispatch);
                var expected = new HeadlessExpeditionRunner(db).Run(session.PendingExpedition).ResourceLoot.Sum(r => r.Amount);
                var dispatched = session.DispatchExpedition();
                Assert.IsFalse(dispatched.AwaitingExpedition);
                Assert.Less(dispatched.Expedition.ResourceLoot.Sum(r => r.Amount), expected, "보상 ×0.7");

                var standard = Session(directory, DifficultyRules.Standard, out _);
                Assert.IsFalse(standard.Difficulty.Dispatch, "표준에는 자동 수색이 없다");
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }
    }
}
