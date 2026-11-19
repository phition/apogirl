using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Events;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Random;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>M6-1: 주인공 감염, 선생님 재감염, 날씨, 시설·동료 보정, 정찰.</summary>
    public sealed class M6SystemTests
    {
        private static WeekRunner Runner(GameState state, IDefinitionDatabase db, StubExpeditionRunner expedition = null)
        {
            return new WeekRunner(state, db, new SeededRandom(state.Rng), expedition ?? new StubExpeditionRunner());
        }

        private static void PlayWeek(WeekRunner runner, GameState state)
        {
            runner.PrepareWeek();
            while (!runner.IsWeekComplete && state.Status == GameStatus.Playing)
            {
                runner.ExecuteNextSlot();
            }
        }

        // ── 주인공 감염 ──

        [Test]
        public void Down_CanBite_FirstAidDecidesInfection()
        {
            var db = TestContent.CreateDatabase(b => { b.BiteChanceOnDownDay = 1f; b.FirstAidBaseChance = 0f; b.FirstAidPerMedicineLevel = 0f; });
            var state = TestContent.NewGame(db);
            state.Plan.Slots[0].ActionId = TestContent.DayExpedition;
            var expedition = new StubExpeditionRunner { Handler = _ => new ExpeditionResult { Status = ExtractStatus.Down, HpDelta = -10 } };
            var runner = Runner(state, db, expedition);
            runner.PrepareWeek();
            var outcome = runner.ExecuteNextSlot();

            Assert.IsNotNull(outcome.Bite, "쓰러짐 + 확률 1 → 물림");
            Assert.IsFalse(outcome.Bite.Treated);
            Assert.AreEqual(db.Balance.BiteInfection, state.Player.Vitals.Infection);
            Assert.IsTrue(state.Flags.Contains(InfectionRules.FlagBitten));
        }

        [Test]
        public void Extracted_WithoutBiteInjury_NeverBites()
        {
            var db = TestContent.CreateDatabase(b => b.BiteChanceOnDownDay = 1f);
            var state = TestContent.NewGame(db);
            state.Plan.Slots[0].ActionId = TestContent.DayExpedition;
            var runner = Runner(state, db);
            runner.PrepareWeek();
            Assert.IsNull(runner.ExecuteNextSlot().Bite);
            Assert.AreEqual(0, state.Player.Vitals.Infection);
        }

        [Test]
        public void Infection_GrowsWeekly_AndEndsAtMax()
        {
            var db = TestContent.CreateDatabase(b => { b.InfectionWeeklyGrowth = 30; b.InfectionGrowthReducePerMedicineLevel = 5; });
            var state = TestContent.NewGame(db);
            state.Base.Resources[ResourceType.Food] = 200;
            state.Base.Resources[ResourceType.Water] = 200;
            state.Player.Vitals.Infection = 40;
            var runner = Runner(state, db);

            PlayWeek(runner, state);
            runner.FinishWeek();
            Assert.AreEqual(70, state.Player.Vitals.Infection, "의학 Lv0: 주 +30");

            state.Player.Proficiency[ProficiencyType.Medicine] = new ProficiencyState { Level = 2 };
            PlayWeek(runner, state);
            runner.FinishWeek();
            Assert.AreEqual(90, state.Player.Vitals.Infection, "의학 Lv2: 주 +20");
            Assert.AreEqual(3, InfectionRules.SymptomLevel(state), "75 이상 = 손떨림");

            PlayWeek(runner, state);
            runner.FinishWeek();
            Assert.AreEqual(GameStatus.GameOver, state.Status);
            Assert.AreEqual(WeekRunner.EndCauseInfection, state.EndCause);
        }

        // ── 선생님 ──

        [Test]
        public void TeacherReinfect_Progresses_AndCareReduces()
        {
            var db = TestContent.CreateDatabase(b => { b.TeacherInfectionWeekly = 10; b.TeacherInfectionMinWeekly = 1; });
            var state = TestContent.NewGame(db);
            Assert.AreEqual(0, InfectionRules.TeacherStage(state, db.Balance));

            EffectApplier.Apply(state, new GameEffect { TeacherReinfect = true }, db.Balance, new StateDelta());
            Assert.IsTrue(state.Teacher.Reinfected);
            Assert.IsTrue(state.Flags.Contains(InfectionRules.FlagTeacherReinfected));

            for (var i = 0; i < 3; i++) InfectionRules.WeeklyTeacher(state, db);
            Assert.AreEqual(30, state.Teacher.InfectionProgress);
            Assert.AreEqual(1, InfectionRules.TeacherStage(state, db.Balance));

            EffectApplier.Apply(state, new GameEffect { TeacherInfection = -8 }, db.Balance, new StateDelta());
            Assert.AreEqual(22, state.Teacher.InfectionProgress, "돌봄으로 진행도 감소");
            Assert.AreEqual(22, ConditionEvaluator.ValueOf(state, ValueKind.TeacherInfection, ""));

            state.Base.Weather = WeatherType.Cold;
            InfectionRules.WeeklyTeacher(state, db);
            Assert.AreEqual(22 + 10 + db.Balance.TeacherInfectionColdBonus, state.Teacher.InfectionProgress, "한파 가산");
        }

        [Test]
        public void TeacherInfection_IgnoredBeforeReinfect()
        {
            var db = TestContent.CreateDatabase();
            var state = TestContent.NewGame(db);
            EffectApplier.Apply(state, new GameEffect { TeacherInfection = 20 }, db.Balance, new StateDelta());
            InfectionRules.WeeklyTeacher(state, db);
            Assert.AreEqual(0, state.Teacher.InfectionProgress);
        }

        // ── 날씨 ──

        [Test]
        public void Weather_FollowsSeasonTable()
        {
            var balance = new BalanceConfig();
            var random = new SeededRandom(SeededRandom.CreateState(5));
            var winter = new HashSet<WeatherType>();
            var summer = new HashSet<WeatherType>();
            for (var i = 0; i < 300; i++)
            {
                winter.Add(WeatherRules.Roll(45, balance, random));
                summer.Add(WeatherRules.Roll(20, balance, random));
            }

            Assert.IsFalse(winter.Contains(WeatherType.Heat), "겨울에 폭염 없음");
            Assert.IsTrue(winter.Contains(WeatherType.Cold));
            Assert.IsFalse(summer.Contains(WeatherType.Cold), "여름에 한파 없음");
            Assert.IsTrue(summer.Contains(WeatherType.Heat));
        }

        [Test]
        public void Rain_LowersRaidChance_AndQuietsExpedition()
        {
            var db = TestContent.CreateDatabase();
            var state = TestContent.NewGame(db);
            state.Calendar.Week = 10;
            state.Base.Threat = 60;
            var clear = RaidRules.Chance(state, db.Balance);
            state.Base.Weather = WeatherType.Rain;
            Assert.AreEqual(clear * db.Balance.RainRaidMultiplier, RaidRules.Chance(state, db.Balance), 1e-5);

            var modifiers = TraitRules.ExplorationModifiers(state, db);
            Assert.AreEqual(db.Balance.RainNoiseBonus, modifiers[nameof(TraitModifierKind.NoiseMultiplier)], 1e-5);
        }

        [Test]
        public void Heat_CostsExtraWater_ColdAddsStress()
        {
            var db = TestContent.CreateDatabase();
            var state = TestContent.NewGame(db);
            state.Base.Resources[ResourceType.Water] = 50;
            state.Base.Weather = WeatherType.Heat;
            UpkeepRules.ApplyDaily(state, db.Balance, new StateDelta());
            Assert.AreEqual(50 - db.Balance.DailyWater - db.Balance.HeatExtraWater, state.Base.GetResource(ResourceType.Water));

            state.Base.Weather = WeatherType.Cold;
            var stress = state.Player.Vitals.Stress;
            UpkeepRules.ApplyDaily(state, db.Balance, new StateDelta());
            Assert.AreEqual(stress + db.Balance.ColdDailyStress, state.Player.Vitals.Stress);
        }

        // ── 시설·동료 ──

        private static InMemoryDefinitionDatabase DbWithExtras()
        {
            var extras = new DefinitionExtras
            {
                Facilities =
                {
                    new FacilityDef { Id = "LIBRARY", Modifiers = { new TraitModifier { Kind = TraitModifierKind.EducationGain, Value = 0.15f } } },
                },
                Roles =
                {
                    new RoleDef { Id = "GUARD", Modifiers = { new TraitModifier { Kind = TraitModifierKind.RaidGuard, Value = 6 } } },
                },
            };
            return new InMemoryDefinitionDatabase(new BalanceConfig(), TestContent.CreateActions(), new[] { TestContent.CreateModel() },
                TestContent.CreateItems(), new[] { TestContent.CreateLootTable() }, new[] { TestContent.CreateLocation() }, extras: extras);
        }

        [Test]
        public void FacilityAndRoleModifiers_AreSummed()
        {
            var db = DbWithExtras();
            var state = TestContent.NewGame(db);
            Assert.AreEqual(0f, TraitRules.Sum(state, db, TraitModifierKind.EducationGain), 1e-5, "닫힌 시설은 효과 없음");

            state.Base.Facilities["LIBRARY"] = 1;
            Assert.AreEqual(0.15f, TraitRules.Sum(state, db, TraitModifierKind.EducationGain), 1e-5);

            state.Npcs["NPC_A"] = new NpcState { Status = NpcStatus.Met };
            Assert.IsFalse(RoleRules.Assign(state, db, "NPC_A", "GUARD"), "합류 전에는 역할 불가");
            state.Npcs["NPC_A"].Status = NpcStatus.Joined;
            Assert.IsFalse(RoleRules.Assign(state, db, "NPC_A", "NOPE"), "없는 역할");
            Assert.IsTrue(RoleRules.Assign(state, db, "NPC_A", "GUARD"));
            Assert.AreEqual(6f, TraitRules.Sum(state, db, TraitModifierKind.RaidGuard), 1e-5);
            Assert.AreEqual(1, ConditionEvaluator.ValueOf(state, ValueKind.RoleCount, "GUARD"));
        }

        // ── 정찰 ──

        [Test]
        public void ScoutFlag_IsConsumedByExpedition()
        {
            var db = TestContent.CreateDatabase();
            var state = TestContent.NewGame(db);
            var location = db.GetAction(TestContent.DayExpedition).LocationId;
            state.Flags.Add(ScoutRules.FlagFor(location));
            Assert.IsTrue(ScoutRules.IsScouted(state, location));

            state.Plan.Slots[0].ActionId = TestContent.DayExpedition;
            var runner = Runner(state, db);
            runner.PrepareWeek();
            runner.ExecuteNextSlot();
            Assert.IsFalse(ScoutRules.IsScouted(state, location), "출정하면 정찰 정보 소모");
        }
    }
}
