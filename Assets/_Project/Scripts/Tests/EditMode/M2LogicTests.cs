using System;
using System.IO;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Planning;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using ApoGirl.Systems.Save;
using ApoGirl.Systems.Session;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    public sealed class PlanEditingTests
    {
        private InMemoryDefinitionDatabase _db;
        private GameState _state;

        [SetUp]
        public void SetUp()
        {
            _db = TestContent.CreateDatabase();
            _state = TestContent.NewGame(_db);
        }

        [Test]
        public void SetSlot_ChecksLockAndSlotTag_AndAppliesForcedRest()
        {
            Assert.AreEqual(PlanEditResult.Ok, PlanEditing.SetSlot(_state, _db, 0, TestContent.DayExpedition));
            Assert.IsTrue(_state.Plan.Slots[1].ForcedRest, "탐험 직후 강제 휴식");

            Assert.AreEqual(PlanEditResult.WrongSlot, PlanEditing.SetSlot(_state, _db, 3, TestContent.PurifyWater));
            Assert.AreEqual(PlanEditResult.UnknownAction, PlanEditing.SetSlot(_state, _db, 2, "ACT_NOPE"));

            _state.Plan.Slots[4].Locked = true;
            Assert.AreEqual(PlanEditResult.Locked, PlanEditing.SetSlot(_state, _db, 4, TestContent.Humanities));
            Assert.AreEqual(PlanEditResult.Locked, PlanEditing.ClearSlot(_state, _db, 4));
        }

        [Test]
        public void Editing_IsRejected_OnceWeekStarted()
        {
            _state.Calendar.Phase = DayPhase.Night;
            Assert.AreEqual(PlanEditResult.NotPlanning, PlanEditing.SetSlot(_state, _db, 2, TestContent.Humanities));
        }

        [Test]
        public void CopyLastWeek_SkipsLockedSlots()
        {
            Assert.IsFalse(PlanEditing.CopyLastWeek(_state, _db), "첫 주에는 지난주 없음");

            _state.LastWeekPlan = new WeekPlan();
            _state.LastWeekPlan.Slots[0].ActionId = TestContent.Humanities;
            _state.LastWeekPlan.Slots[2].ActionId = TestContent.DayExpedition;
            PlanPreparer.LockRest(_state.Plan, 0, _db.Balance);

            Assert.IsTrue(PlanEditing.CopyLastWeek(_state, _db));
            Assert.AreEqual(TestContent.RestDay, _state.Plan.Slots[0].ActionId);
            Assert.AreEqual(TestContent.DayExpedition, _state.Plan.Slots[2].ActionId);
            Assert.IsTrue(_state.Plan.Slots[3].ForcedRest);
        }

        [Test]
        public void Presets_SaveApplyOverwriteAndLimit()
        {
            PlanEditing.SetSlot(_state, _db, 0, TestContent.Humanities);
            Assert.IsTrue(PlanEditing.SavePreset(_state, "공부 주간"));
            PlanEditing.ClearSlot(_state, _db, 0);
            Assert.IsTrue(PlanEditing.ApplyPreset(_state, _db, "공부 주간"));
            Assert.AreEqual(TestContent.Humanities, _state.Plan.Slots[0].ActionId);

            Assert.IsTrue(PlanEditing.SavePreset(_state, "공부 주간"), "같은 이름은 덮어쓰기");
            for (var i = 2; i <= PlanEditing.MaxPresets; i++)
            {
                Assert.IsTrue(PlanEditing.SavePreset(_state, "p" + i));
            }

            Assert.IsFalse(PlanEditing.SavePreset(_state, "초과"));
            Assert.IsFalse(PlanEditing.SavePreset(_state, " "));
            Assert.IsTrue(PlanEditing.DeletePreset(_state, "p2"));
            Assert.AreEqual(PlanEditing.MaxPresets - 1, _state.PlanPresets.Count);
        }

        [Test]
        public void FinishWeek_StoresLastWeekPlan()
        {
            var runner = new WeekRunner(_state, _db, new FixedRandom(), new StubExpeditionRunner());
            _state.Plan.Slots[0].ActionId = TestContent.Humanities;
            while (!runner.IsWeekComplete)
            {
                runner.ExecuteNextSlot();
            }

            runner.FinishWeek();
            Assert.AreEqual(TestContent.Humanities, _state.LastWeekPlan.Slots[0].ActionId);
        }
    }

    public sealed class PlanInsightsTests
    {
        private InMemoryDefinitionDatabase _db;
        private GameState _state;

        [SetUp]
        public void SetUp()
        {
            _db = TestContent.CreateDatabase();
            _state = TestContent.NewGame(_db);
        }

        [Test]
        public void Preview_ChanceMatchesResolverAfterCosts()
        {
            _state.Player.Vitals.Stress = 30;
            var preview = ActionPreviewer.Preview(_state, _db, TestContent.Humanities);

            var action = _db.GetAction(TestContent.Humanities);
            var expected = CheckResolver.ComputeChance(_state, action.Check, _db.Balance, action.FatigueCost, 30 + action.StressCost);
            Assert.AreEqual(expected, preview.SuccessChance, 1e-6f);
            Assert.AreEqual(expected * _db.Balance.GreatSuccessRatio, preview.GreatSuccessChance, 1e-6f);
        }

        [Test]
        public void Preview_ExpectedGainsMatchActualSuccess()
        {
            var preview = ActionPreviewer.Preview(_state, _db, TestContent.Humanities);
            var before = _state.Player.GetStat(StatType.Intellect);
            ActionResolver.Resolve(_state, _db.GetAction(TestContent.Humanities), _db, new FixedRandom(0.5), new StateDelta());

            var expected = preview.ExpectedGains.First(g => g.Stat == StatType.Intellect).Amount;
            Assert.AreEqual(before + expected, _state.Player.GetStat(StatType.Intellect));
        }

        [Test]
        public void Preview_UnaffordableHasZeroChance()
        {
            var preview = ActionPreviewer.Preview(_state, _db, TestContent.Craft);
            Assert.IsFalse(preview.Affordable);
            Assert.AreEqual(0f, preview.SuccessChance);
        }

        [Test]
        public void Forecast_MatchesActualSleepDebt()
        {
            _state.Player.SleepDebt = 1;
            PlanEditing.SetSlot(_state, _db, 1, TestContent.NightExpedition);
            PlanEditing.SetSlot(_state, _db, 4, TestContent.DayExpedition);
            PlanEditing.SetSlot(_state, _db, 5, TestContent.NightExpedition); // 연속 출정 + 위반
            PlanEditing.SetSlot(_state, _db, 9, TestContent.Humanities);
            var forecast = PlanForecast.ProjectSleepDebt(_state, _db);

            var runner = new WeekRunner(_state, _db, new FixedRandom(), new StubExpeditionRunner());
            runner.PrepareWeek();
            for (var i = 0; i < 14; i++)
            {
                runner.ExecuteNextSlot();
                Assert.AreEqual(forecast[i], _state.Player.SleepDebt, $"슬롯 {i}");
            }
        }
    }

    public sealed class GameSessionTests
    {
        private string _directory;
        private GameSession _session;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "ApoGirlSessionTest_" + Guid.NewGuid().ToString("N"));
            _session = new GameSession(TestContent.CreateDatabase(), new SaveService(_directory, new GameStateSerializer()));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        [Test]
        public void PlanRunWeek_AutosavesAndContinues()
        {
            var weekEnded = 0;
            var slots = 0;
            _session.WeekEnded += _ => weekEnded++;
            _session.SlotResolved += _ => slots++;

            _session.NewGame(5);
            Assert.IsTrue(_session.IsPlanning);
            Assert.AreEqual(PlanEditResult.Ok, _session.SetSlot(0, TestContent.Humanities));

            _session.BeginWeek();
            Assert.IsFalse(_session.IsPlanning);
            Assert.AreEqual(PlanEditResult.NotPlanning, _session.SetSlot(2, TestContent.Humanities));
            Assert.Throws<InvalidOperationException>(() => _session.Save(SaveService.ManualSlot(1)));

            while (_session.IsExecuting)
            {
                _session.Step();
            }

            Assert.AreEqual(14, slots);
            Assert.AreEqual(1, weekEnded);
            Assert.AreEqual(2, _session.State.Calendar.Week);
            Assert.IsTrue(_session.HasSave(SaveService.AutoSlot));

            var serializer = new GameStateSerializer();
            var expected = serializer.Serialize(_session.State);
            _session.Load(SaveService.AutoSlot);
            Assert.AreEqual(expected, serializer.Serialize(_session.State));
            Assert.IsTrue(_session.IsPlanning);
        }

        [Test]
        public void DebugForceDown_AppliesOnce()
        {
            _session.NewGame(9);
            _session.SetSlot(0, TestContent.DayExpedition);
            _session.DebugExpedition.ForceNext = ExtractStatus.Down;
            _session.BeginWeek();
            var outcome = _session.Step();

            Assert.AreEqual(ExtractStatus.Down, outcome.Expedition.Status);
            Assert.IsNull(_session.DebugExpedition.ForceNext);
            Assert.AreEqual(1, _session.State.Exploration.Downs);
        }

        [Test]
        public void GameOver_RaisesEvent()
        {
            GameStatus? ended = null;
            _session.GameEnded += s => ended = s;
            _session.NewGame(3);
            _session.DebugChangeVital(VitalType.Hp, -99);
            _session.State.Base.SetResource(ResourceType.Water, 0);
            _session.BeginWeek();
            while (_session.IsExecuting)
            {
                _session.Step();
            }

            Assert.AreEqual(GameStatus.GameOver, ended);
            Assert.IsTrue(_session.IsEnded);
        }

        [Test]
        public void ExecutionPacer_NoDelayOnStops()
        {
            var stop = new SlotOutcome { StopForPresentation = true };
            var pass = new SlotOutcome();
            Assert.AreEqual(0f, ExecutionPacer.DelayAfter(stop, ExecutionSpeed.Normal));
            Assert.Greater(ExecutionPacer.DelayAfter(pass, ExecutionSpeed.Normal), ExecutionPacer.DelayAfter(pass, ExecutionSpeed.Fast));
            Assert.AreEqual(0f, ExecutionPacer.DelayAfter(pass, ExecutionSpeed.EventsOnly));
        }
    }

    public sealed class SaveMigrationTests
    {
        [Test]
        public void V1Save_LoadsWithPresetsList()
        {
            var serializer = new GameStateSerializer();
            var root = JObject.Parse(serializer.Serialize(TestContent.NewGame(TestContent.CreateDatabase())));
            root["SaveVersion"] = 1;
            root.Remove("PlanPresets");
            root.Remove("LastWeekPlan");
            root.Remove("Events");
            root.Remove("Npcs");

            var loaded = serializer.Deserialize(root.ToString());
            Assert.AreEqual(GameState.CurrentSaveVersion, loaded.SaveVersion);
            Assert.IsNotNull(loaded.Events, "v3: 이벤트 기록");
            Assert.IsNotNull(loaded.Npcs, "v3: NPC");
            Assert.IsNotNull(loaded.PlanPresets);
            Assert.IsNull(loaded.LastWeekPlan);
        }
    }
}
