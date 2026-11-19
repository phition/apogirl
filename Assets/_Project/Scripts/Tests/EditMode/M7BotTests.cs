using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Random;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.Simulation.Bots;
using ApoGirl.Core.State;
using ApoGirl.Systems.Content;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>M7-1: 봇 시뮬레이터 (설정 점검, 계획 규칙, 한 판 진행, 보고서).</summary>
    public sealed class M7BotTests
    {
        private static BotConfigDef Config()
        {
            return BotConfigJson.Load(ContentPipelineTests.ProjectPath(BotConfigJson.DefaultPath));
        }

        [Test]
        public void ShippedConfig_IsValid()
        {
            var config = Config();
            Assert.GreaterOrEqual(config.Strategies.Count, 5, "전략 5종 (낮만·밤만·혼합·교육·돌봄)");
            var errors = BotPlanner.Validate(config, M6Act4Tests.ShippedDb());
            Assert.IsEmpty(errors, string.Join("\n", errors));
        }

        [Test]
        public void SlotIndex_DayAndNight()
        {
            Assert.AreEqual(0, BotPlanner.SlotIndex("D1"));
            Assert.AreEqual(13, BotPlanner.SlotIndex("N7"));
            Assert.AreEqual(7, BotPlanner.SlotIndex("n4"));
            Assert.AreEqual(-1, BotPlanner.SlotIndex("D8"));
            Assert.AreEqual(-1, BotPlanner.SlotIndex("X1"));
        }

        [Test]
        public void NightOnly_UsesDayExpeditionsUntilNightRunsUnlock()
        {
            var db = M6Act4Tests.ShippedDb();
            var config = Config();
            var strategy = config.Strategies.First(s => s.Id == "NIGHT_ONLY");
            var state = NewGameFactory.Create(db.Balance, 3);
            state.Flags.Add("LOC_SCHOOL_DAY");
            var random = new SeededRandom(SeededRandom.CreateState(1));

            BotPlanner.Plan(state, db, config, strategy, random);
            Assert.AreEqual("ACT_EXP_DAY_SCHOOL", state.Plan.Slots[BotPlanner.SlotIndex("D2")].ActionId, "해금 전: 낮 탐험");
            Assert.AreEqual(BotPlanner.RestNight, state.Plan.Slots[BotPlanner.SlotIndex("N1")].ActionId, "밤 탐험이 잠겨 있으면 휴식");

            state.Flags.Add("NIGHT_RUNS");
            BotPlanner.Plan(state, db, config, strategy, random);
            Assert.AreEqual("ACT_EXP_NIGHT_SCHOOL", state.Plan.Slots[BotPlanner.SlotIndex("N1")].ActionId);
            Assert.AreEqual(BotPlanner.RestDay, state.Plan.Slots[BotPlanner.SlotIndex("D2")].ActionId, "해금 뒤: 밤 탐험 다음 낮은 휴식");
        }

        [Test]
        public void CommonRules_TreatInfectionAndRestWhenHurt()
        {
            var db = M6Act4Tests.ShippedDb();
            var config = Config();
            var strategy = config.Strategies.First(s => s.Id == "DAY_ONLY");
            var state = NewGameFactory.Create(db.Balance, 4);
            state.Flags.Add("LOC_SCHOOL_DAY");
            state.Player.Vitals.Infection = 30;
            state.Player.Vitals.Hp = 20;
            BotPlanner.Plan(state, db, config, strategy, new SeededRandom(SeededRandom.CreateState(2)));

            Assert.AreEqual("ACT_TREAT_INFECTION", state.Plan.Slots[BotPlanner.SlotIndex("D3")].ActionId);
            Assert.AreEqual(BotPlanner.RestDay, state.Plan.Slots[BotPlanner.SlotIndex("D2")].ActionId, "체력이 낮으면 탐험 대신 휴식");
        }

        [Test]
        public void EventChooser_FollowsBias()
        {
            var chooser = new BotEventChooser(new[] { new BotChoiceBiasDef { Ids = { "CARE" }, Weight = 0f } }, new SeededRandom(SeededRandom.CreateState(9)));
            var choices = new[]
            {
                new ApoGirl.Core.Events.EventChoiceView { Def = new EventChoiceDef { Id = "CARE" }, Enabled = true },
                new ApoGirl.Core.Events.EventChoiceView { Def = new EventChoiceDef { Id = "LEAVE" }, Enabled = true },
                new ApoGirl.Core.Events.EventChoiceView { Def = new EventChoiceDef { Id = "LOCKED" }, Enabled = false },
            };
            for (var i = 0; i < 20; i++)
            {
                Assert.AreEqual("LEAVE", chooser.Choose(null, null, choices), "가중치 0·잠긴 선택지는 고르지 않는다");
            }
        }

        [Test]
        public void ProloguePick_FillsPersonalityAndBloodType()
        {
            var db = M6Act4Tests.ShippedDb();
            var config = Config();
            var bloods = new System.Collections.Generic.HashSet<BloodType>();
            for (var seed = 1; seed <= 40; seed++)
            {
                var picks = BotSimulator.PickPrologue(db.Extras.Prologue, config, new SeededRandom(SeededRandom.CreateState(seed)));
                var profile = new ProfileState();
                ApoGirl.Core.Rules.PrologueRules.BuildEffect(db.Extras.Prologue, picks, profile);
                Assert.AreEqual(4, profile.PersonalityType.Length, "성격 유형 네 글자");
                bloods.Add(profile.BloodType);
            }

            Assert.GreaterOrEqual(bloods.Count, 4, "혈액형이 고르게 나온다 (모름 포함)");
        }

        /// <summary>전략마다 한 판씩 끝까지 (콘텐츠 JSON DB라 조우는 없음). 보고서 파일이 모두 만들어진다.</summary>
        [Test]
        public void EveryStrategy_PlaysToTheEnd_AndReports()
        {
            var db = M6Act4Tests.ShippedDb();
            var config = Config();
            var results = config.Strategies.Select(s => BotSimulator.Run(db, config, s, 11)).ToList();
            foreach (var r in results)
            {
                Assert.AreNotEqual(GameStatus.Playing, r.Status, $"{r.StrategyId}: 판이 끝나야 한다");
                Assert.IsNotEmpty(r.EndingId, $"{r.StrategyId}: 엔딩이 정해져야 한다");
                Assert.IsNotEmpty(r.Expeditions, $"{r.StrategyId}: 탐험 기록");
                Assert.IsNotEmpty(r.Weeks, $"{r.StrategyId}: 주간 기록");
            }

            var again = BotSimulator.Run(db, config, config.Strategies[0], 11);
            Assert.AreEqual(results[0].EndingId, again.EndingId, "같은 시드면 같은 결과");
            Assert.AreEqual(results[0].EndWeek, again.EndWeek);

            var files = BotReport.Build(results);
            Assert.AreEqual(4, files.Count);
            Assert.IsTrue(files.Values.All(f => f.Split('\n').Length > 2), "모든 보고서에 내용");
            StringAssert.Contains("엔딩 분포", files[BotReport.SummaryFile]);
        }
    }
}
