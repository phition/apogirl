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
        public void Diet_NotDiscounted_FoodWaterConvertByDifficulty()
        {
            var balance = new BalanceConfig();
            var state = NewGameFactory.Create(balance, 1);
            state.Difficulty = DifficultyRules.Story;
            state.Base.SetResource(ResourceType.Water, 100);
            for (var d = 1; d <= 7; d++)
            {
                state.Calendar.Day = d;
                UpkeepRules.ApplyMorning(state, balance, new StateDelta(), new FixedRandom());
            }

            Assert.AreEqual(100 - 2 * 7, state.Base.GetResource(ResourceType.Water), "이야기 중심도 식단 그대로 먹는다 (혼자, 챙겨 먹는다)");
            Assert.AreEqual(14, UpkeepRules.WeeklyNeed(state, balance, ResourceType.Food, DietLevel.Care));

            // 통조림·생수 5 → 이야기 중심 7.5 · 표준 5 · 하드 4
            float Convert(string id) => 5 * balance.Difficulties.First(x => x.Id == id).FoodYield;
            Assert.AreEqual(7.5f, Convert("STORY"), 1e-4f);
            Assert.AreEqual(5f, Convert("STANDARD"), 1e-4f);
            Assert.AreEqual(4f, Convert("HARD"), 1e-4f);
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
            Assert.That(state.Base.GetResource(ResourceType.Food), Is.EqualTo(16), "식량은 전체 배율 없이 변환만: 20 × 0.8 = 16 (통조림 5 → 4)");
        }

        private static GameSession Session(string directory, string difficulty, out InMemoryDefinitionDatabase db, EndingGallery gallery = null)
        {
            db = TestContent.CreateDatabase();
            var session = new GameSession(db, new SaveService(directory, new GameStateSerializer()), gallery: gallery);
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
                // 52주차까지 가서 엔딩을 본 적이 없으면 이야기 중심이라도 되돌릴 수 없다
                var fresh = Session(directory, DifficultyRules.Story, out _, new EndingGallery(directory));
                RunWeek(fresh);
                Assert.IsFalse(fresh.UndoUnlocked);
                Assert.IsFalse(fresh.CanUndoWeek, "52주차 엔딩을 보기 전에는 되돌리기가 없다");

                var gallery = new EndingGallery(directory);
                gallery.Record("ENDING_SURVIVOR", 52);
                var session = Session(directory, DifficultyRules.Story, out _, gallery);
                Assert.IsTrue(session.UndoUnlocked);
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

                var standard = Session(directory, DifficultyRules.Standard, out _, gallery);
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
