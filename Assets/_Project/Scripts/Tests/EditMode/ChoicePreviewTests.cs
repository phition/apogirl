using System;
using System.IO;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Events;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;
using ApoGirl.Systems.Save;
using ApoGirl.Systems.Session;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>디버그 "선택지 결과 미리 보기": 효과 → 미리 보기 줄 (화면 없이), 읽기만 하는지.</summary>
    public sealed class ChoicePreviewTests
    {
        private string _directory;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "ApoGirlPreviewTest_" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        private static InMemoryDefinitionDatabase Db(params EventDef[] events)
        {
            return new InMemoryDefinitionDatabase(
                new BalanceConfig(), TestContent.CreateActions(), new[] { TestContent.CreateModel() }, TestContent.CreateItems(),
                new[] { TestContent.CreateLootTable() }, new[] { TestContent.CreateLocation() }, null, events);
        }

        /// <summary>세이브 직렬화로 깊은 사본을 만든다 (GameSession.PreviewChoice와 같은 방법).</summary>
        private static Func<GameState> Cloner(GameState state)
        {
            var serializer = new GameStateSerializer();
            var json = serializer.Serialize(state);
            return () => serializer.Deserialize(json);
        }

        private static EventDef Event(string id, params EventChoiceDef[] choices)
        {
            var ev = new EventDef { Id = id, Kind = EventKind.Story, Condition = new ConditionDef { Days = { 1 }, Phase = PhaseFilter.Day } };
            ev.Choices.AddRange(choices);
            return ev;
        }

        [Test]
        public void Preview_ShowsActualValues_AndDefinedWhenClamped()
        {
            var choice = new EventChoiceDef
            {
                Id = "A",
                Costs = { new ResourceAmount(ResourceType.Materials, 2) },
                Effect =
                {
                    Stats = { new StatAmount(StatType.Intellect, 3) },
                    Vitals = { new VitalAmount(VitalType.Stress, 10) },
                    Resources = { new ResourceAmount(ResourceType.Food, 5) },
                    Hidden = { new HiddenAmount(HiddenType.Humanity, 2) },
                    Conscience = 1,
                    SetFlags = { "NEW_FLAG", "OLD_FLAG" },
                    AddClues = { "CLUE_X" },
                    Items = { new ItemGrant { ItemId = "ITEM_TEST", Count = 2 } },
                },
            };
            var ev = Event("EVT_PREVIEW", choice);
            var db = Db(ev);
            var state = TestContent.NewGame(db);
            state.Player.Vitals.Stress = db.Balance.VitalMax - 4;   // 스트레스는 4만 더 오를 수 있다
            state.Base.SetResource(ResourceType.Materials, 5);
            state.Flags.Add("OLD_FLAG");

            var preview = ChoicePreview.Build(Cloner(state), db, ev, choice, db.Balance);

            Assert.IsTrue(preview.Enabled);
            Assert.IsFalse(preview.HasCheck);
            Assert.IsNull(preview.Failure, "판정이 없으면 갈래는 하나");
            var branch = preview.Success;

            var intellect = branch.Find(PreviewKind.Stat, "Intellect");
            Assert.AreEqual(3, intellect.Amount);
            Assert.IsFalse(intellect.Adjusted);

            var stress = branch.Find(PreviewKind.Vital, "Stress");
            Assert.AreEqual(4, stress.Amount, "범위 제한을 거친 실제 값");
            Assert.AreEqual(10, stress.Defined);
            Assert.IsTrue(stress.Adjusted, "정의값과 다르면 표시한다");

            Assert.AreEqual(5, branch.Find(PreviewKind.Resource, "Food").Amount);
            Assert.AreEqual(-2, branch.Find(PreviewKind.Resource, "Materials").Amount, "비용도 자원 변화로");
            Assert.AreEqual(2, branch.Find(PreviewKind.Hidden, "Humanity").Amount);
            Assert.AreEqual(1, branch.Find(PreviewKind.Conscience, "").Amount);

            Assert.IsFalse(branch.Find(PreviewKind.FlagSet, "NEW_FLAG").Already);
            Assert.IsTrue(branch.Find(PreviewKind.FlagSet, "OLD_FLAG").Already, "이미 켜진 깃발은 바뀌지 않는다고 표시");
            Assert.IsNotNull(branch.Find(PreviewKind.Clue, "CLUE_X"));
            Assert.AreEqual(2, branch.Find(PreviewKind.ItemGained, "ITEM_TEST").Amount);
            Assert.IsFalse(branch.GameOver);
            Assert.AreEqual("", branch.NextEventId);
        }

        [Test]
        public void Preview_Check_HasBothBranches_AndCurrentChance()
        {
            var choice = new EventChoiceDef
            {
                Id = "TRY",
                HasCheck = true,
                Check = { BaseRate = 0.4f, StatWeights = { new StatWeight(StatType.Intellect, 0.01f) } },
                Effect = { Resources = { new ResourceAmount(ResourceType.Food, 4) } },
                OnFailure = { Vitals = { new VitalAmount(VitalType.Hp, -5) } },
            };
            var ev = Event("EVT_CHECK", choice);
            var db = Db(ev);
            var state = TestContent.NewGame(db);

            var preview = ChoicePreview.Build(Cloner(state), db, ev, choice, db.Balance);

            Assert.IsTrue(preview.HasCheck);
            Assert.AreEqual(CheckResolver.ComputeChance(state, choice.Check, db.Balance), preview.Chance, 1e-5f, "지금 상태의 성공 확률");
            Assert.AreEqual(4, preview.Success.Find(PreviewKind.Resource, "Food").Amount);
            Assert.IsNull(preview.Success.Find(PreviewKind.Vital, "Hp"));
            Assert.IsNotNull(preview.Failure);
            Assert.AreEqual(-5, preview.Failure.Find(PreviewKind.Vital, "Hp").Amount);
            Assert.IsNull(preview.Failure.Find(PreviewKind.Resource, "Food"));
        }

        [Test]
        public void Preview_NpcAffinity_UsesRulesNotRawDefinition()
        {
            var choice = new EventChoiceDef
            {
                Id = "JOIN",
                Effect = { Npcs = { new NpcChange { NpcId = "NPC_X", Affinity = 12, SetStatus = NpcStatus.Joined, SetRole = "guard" } } },
            };
            var ev = Event("EVT_JOIN", choice);
            var db = Db(ev);
            var state = TestContent.NewGame(db);

            var branch = ChoicePreview.Build(Cloner(state), db, ev, choice, db.Balance).Success;

            // 합류하면 호감 바닥 JoinAffinity 위에, 이벤트 호감 배율을 곱한 값이 붙는다 (EffectApplier)
            var expected = db.Balance.JoinAffinity + (int)Math.Round(12 * db.Balance.CompanionAffinityGainScale);
            var affinity = branch.Find(PreviewKind.NpcAffinity, "NPC_X");
            Assert.AreEqual(expected, affinity.Amount);
            Assert.AreEqual(12, affinity.Defined);
            Assert.AreEqual("Joined", branch.Find(PreviewKind.NpcStatus, "NPC_X").Value);
            Assert.AreEqual("guard", branch.Find(PreviewKind.NpcRole, "NPC_X").Value);
        }

        [Test]
        public void Preview_InjuredBlocksHeal_AndBigLossMeansInjury()
        {
            var heal = new EventChoiceDef { Id = "HEAL", Effect = { Vitals = { new VitalAmount(VitalType.Hp, 10) } } };
            var hurt = new EventChoiceDef { Id = "HURT", Effect = { Vitals = { new VitalAmount(VitalType.Hp, -60) } } };
            var ev = Event("EVT_HP", heal, hurt);
            var db = Db(ev);
            var state = TestContent.NewGame(db);
            state.Player.Vitals.Hp = 70;

            // 크게 다치면 부상 깃발이 켜진다 (선택 적용 뒤의 부상 판정까지 따라간다)
            var hurtBranch = ChoicePreview.Build(Cloner(state), db, ev, hurt, db.Balance).Success;
            Assert.AreEqual(-60, hurtBranch.Find(PreviewKind.Vital, "Hp").Amount);
            Assert.IsNotNull(hurtBranch.Find(PreviewKind.FlagSet, db.Balance.InjuredFlag), "정의에 없어도 규칙이 켜는 깃발이 보인다");

            // 부상 중에는 체력이 회복되지 않는다 → 실제 0, 정의 +10
            state.Flags.Add(db.Balance.InjuredFlag);
            var healEntry = ChoicePreview.Build(Cloner(state), db, ev, heal, db.Balance).Success.Find(PreviewKind.Vital, "Hp");
            Assert.AreEqual(0, healEntry.Amount);
            Assert.AreEqual(10, healEntry.Defined);
            Assert.IsTrue(healEntry.Adjusted);
        }

        [Test]
        public void Preview_NextEvent_ShowsItsOnShowEffect_AndRandomResultsByNameOnly()
        {
            var next = new EventDef { Id = "EVT_AFTER", Kind = EventKind.Story, Condition = new ConditionDef { FlagsAll = { "NEVER" } } };
            next.OnShow.Vitals.Add(new VitalAmount(VitalType.Stress, 7));
            var go = new EventChoiceDef { Id = "GO", NextEventId = "EVT_AFTER", Effect = { RaidStrength = 5, IntrusionResponse = "Fight" } };
            var dead = new EventChoiceDef { Id = "DEAD", NextEventId = "EVT_AFTER", Effect = { Vitals = { new VitalAmount(VitalType.Hp, -999) } } };
            var ev = Event("EVT_FIRST", go, dead);
            var db = Db(ev, next);
            var state = TestContent.NewGame(db);
            state.Player.Vitals.Stress = 10;

            var branch = ChoicePreview.Build(Cloner(state), db, ev, go, db.Balance).Success;
            Assert.AreEqual("EVT_AFTER", branch.NextEventId);
            Assert.AreEqual(7, branch.NextEntries.Single(e => e.Kind == PreviewKind.Vital && e.Key == "Stress").Amount);
            Assert.AreEqual(5, branch.Find(PreviewKind.Raid, "").Amount, "습격은 세기만 (결과는 난수)");
            Assert.AreEqual("Fight", branch.Find(PreviewKind.Intrusion, "").Value);
            Assert.IsNull(branch.Find(PreviewKind.Vital, "Hp"), "난수가 드는 결과는 돌리지 않는다");

            var over = ChoicePreview.Build(Cloner(state), db, ev, dead, db.Balance).Success;
            Assert.IsTrue(over.GameOver);
            Assert.AreEqual("", over.NextEventId, "게임 오버면 이어지는 이벤트가 없다");
        }

        [Test]
        public void SessionPreview_ReadsOnly_AndMatchesWhatChoosingApplies()
        {
            var choice = new EventChoiceDef
            {
                Id = "A",
                Effect =
                {
                    Stats = { new StatAmount(StatType.Intellect, 2) },
                    Vitals = { new VitalAmount(VitalType.Stress, 6) },
                    Resources = { new ResourceAmount(ResourceType.Food, 3) },
                    SetFlags = { "PICKED_A" },
                    RaidStrength = 0,
                },
            };
            var risky = new EventChoiceDef
            {
                Id = "B", HasCheck = true, Check = { BaseRate = 0.5f },
                Effect = { SetFlags = { "PICKED_B" } }, OnFailure = { Vitals = { new VitalAmount(VitalType.Stress, 3) } },
            };
            var ev = Event("EVT_DAY1", choice, risky);
            var db = Db(ev);
            var session = new GameSession(db, new SaveService(_directory, new GameStateSerializer())) { InteractiveEvents = true };
            session.NewGame(7);
            session.BeginWeek();
            Assert.IsTrue(session.Step().AwaitingEvent);

            var serializer = new GameStateSerializer();
            var before = serializer.Serialize(session.State);
            var preview = session.PreviewChoice(session.PendingEvent, "A");
            var previewB = session.PreviewChoice(session.PendingEvent, "B");
            Assert.AreEqual(before, serializer.Serialize(session.State), "미리 보기는 상태도 난수도 바꾸지 않는다");
            Assert.IsNull(session.PreviewChoice(session.PendingEvent, "NOPE"), "없는 선택지");
            Assert.IsTrue(previewB.HasCheck);
            Assert.IsNotNull(previewB.Failure);

            // 실제로 고르면 미리 본 만큼 바뀐다
            var player = session.State.Player;
            var intellect = player.GetStat(StatType.Intellect);
            var stress = player.Vitals.Stress;
            var food = session.State.Base.GetResource(ResourceType.Food);
            var outcome = session.ResolveEvent("A");
            Assert.AreEqual("A", outcome.Events.Last().ChoiceId);
            Assert.IsTrue(session.State.Flags.Contains("PICKED_A"));
            Assert.AreEqual(player.GetStat(StatType.Intellect) - intellect, preview.Success.Find(PreviewKind.Stat, "Intellect").Amount);
            Assert.AreEqual(player.Vitals.Stress - stress, preview.Success.Find(PreviewKind.Vital, "Stress").Amount);
            Assert.AreEqual(session.State.Base.GetResource(ResourceType.Food) - food, preview.Success.Find(PreviewKind.Resource, "Food").Amount);
            Assert.AreEqual(2, preview.Success.Find(PreviewKind.Stat, "Intellect").Amount);
            Assert.AreEqual(6, preview.Success.Find(PreviewKind.Vital, "Stress").Amount);
            Assert.AreEqual(3, preview.Success.Find(PreviewKind.Resource, "Food").Amount);
        }
    }
}
