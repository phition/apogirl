using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Endings;
using ApoGirl.Core.Events;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Random;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using ApoGirl.Systems.Content;
using ApoGirl.Systems.Save;
using ApoGirl.Systems.Session;
using NUnit.Framework;
using UnityEngine;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>M6-4: 4막 완치 판정·병세 정지, 52주 전체 진행과 엔딩 도달, 회차 요소.</summary>
    public sealed class M6Act4Tests
    {
        internal static InMemoryDefinitionDatabase ShippedDb()
        {
            var content = ContentJson.LoadFolder(ContentPipelineTests.ProjectPath(ContentJson.ContentFolder));
            return new InMemoryDefinitionDatabase(
                new BalanceConfig(), content.Actions, content.ExpeditionModels, content.Items, content.LootTables,
                null, content.Enemies, content.Events, content.Npcs,
                new DefinitionExtras
                {
                    Traits = content.Traits, Facilities = content.Facilities, Lines = content.Lines, Prologue = content.Prologue ?? new PrologueDef(),
                    Endings = content.Endings, Roles = content.Roles, StartBonuses = content.StartBonuses, Factions = content.Factions,
                });
        }

        /// <summary>완치 시도 조건을 모두 갖춘 상태 (50주 밤, 치료 단서 6, 의학 Lv4, 연구 자료, 치료의 길, 선생님 혈액형 확인 + O형 주인공).</summary>
        internal static GameState CureReady(IDefinitionDatabase db, int seed)
        {
            var state = NewGameFactory.Create(db.Balance, seed);
            state.Calendar.Week = 48;
            EffectApplier.Apply(state, new GameEffect { TeacherReinfect = true, TeacherInfection = 0 }, db.Balance, new StateDelta());
            InfectionRules.ChangeTeacherInfection(state, 50, db.Balance);
            state.Flags.Add("RESEARCH_DATA");
            state.Flags.Add("CURE_ROUTE");
            state.Flags.Add("TEACHER_BLOOD_KNOWN"); // M7: 수혈 (D16). O형은 B형 선생님께 직접 수혈 가능
            state.Player.Profile.BloodType = BloodType.O;
            state.Player.Proficiency[ProficiencyType.Medicine] = new ProficiencyState { Level = 4 };
            for (var i = 0; i < 6; i++) state.Clues.Add("CURE_TEST_" + i);
            return state;
        }

        [Test]
        public void CureAttempt_ConditionAndRoughlyThirtyPercent()
        {
            var db = ShippedDb();
            Assert.IsTrue(db.TryGetEvent("EVT_A4_CURE_ATTEMPT", out var ev));
            var night = new EventContext { Phase = DayPhase.Night, Day = 3, ActionId = "ACT_REST_NIGHT", ActionCategory = ActionCategory.Rest };

            var notReady = CureReady(db, 1);
            notReady.Player.Proficiency[ProficiencyType.Medicine] = new ProficiencyState { Level = 3 };
            Assert.IsFalse(ConditionEvaluator.IsMet(notReady, ev.Condition, night, db.Balance), "의학 Lv3이면 시도 불가");

            var cured = 0;
            const int trials = 300;
            for (var seed = 1; seed <= trials; seed++)
            {
                var state = CureReady(db, seed);
                Assert.IsTrue(ConditionEvaluator.IsMet(state, ev.Condition, night, db.Balance), "조건을 갖추면 시도 가능");
                EventResolver.Choose(state, ev, "TREAT_SELF", night, db.Balance, new SeededRandom(SeededRandom.CreateState(seed * 7919)), new StateDelta());
                Assert.IsTrue(state.Flags.Contains("CURE_ATTEMPTED"), "성공·실패 모두 시도 기록");
                if (state.Flags.Contains(InfectionRules.FlagTeacherCured)) cured++;
            }

            var rate = (float)cured / trials;
            Assert.That(rate, Is.InRange(0.15f, 0.45f), $"완치 성공률 약 30% (실제 {rate:P0})");
        }

        [Test]
        public void Cured_StopsProgressAndStage()
        {
            var db = ShippedDb();
            var state = CureReady(db, 3);
            state.Flags.Add(InfectionRules.FlagTeacherCured);
            InfectionRules.ChangeTeacherInfection(state, -100, db.Balance);
            InfectionRules.WeeklyTeacher(state, db);
            InfectionRules.WeeklyTeacher(state, db);
            Assert.AreEqual(0, state.Teacher.InfectionProgress, "완치 후 진행 없음");
            Assert.AreEqual(0, InfectionRules.TeacherStage(state, db.Balance));
        }

        [Test]
        public void DebugUnlockAll_OpensEveryExpeditionAndFacility()
        {
            var directory = Path.Combine(Path.GetTempPath(), "ApoGirlUnlock_" + Guid.NewGuid().ToString("N"));
            try
            {
                var db = ShippedDb();
                var session = new GameSession(db, new SaveService(directory, new GameStateSerializer()));
                session.NewGame(3);
                session.DebugUnlockAll();

                var expeditions = db.Actions.Where(a => !string.IsNullOrEmpty(a.LocationId) && a.Id != "ACT_EXP_TUTORIAL").ToList();
                Assert.IsTrue(expeditions.Any(a => a.LocationId.Contains("WATERWORKS")), "4막 지역도 검사 대상");
                foreach (var action in expeditions)
                {
                    Assert.IsTrue(session.IsUnlocked(action), action.Id + " 해금");
                }

                foreach (var facility in db.Extras.Facilities)
                {
                    Assert.GreaterOrEqual(session.State.Base.Facilities[facility.Id], 1, facility.Id + " 열림");
                }

                // 거처 선택별 거점 행동(교실 수업·창고 방어)과 특수실도 함께 열린다
                foreach (var id in new[] { "ACT_EDU_SCIENCE", "ACT_EDU_MUSIC", "ACT_EDU_ENGINEERING", "ACT_NIGHT_WORK", "ACT_SURV_COVERS" })
                {
                    Assert.IsTrue(session.IsUnlocked(db.GetAction(id)), id + " 해금");
                }

                // 디버그 장비 주기: 빈 칸이면 바로 장착
                session.State.Loadout.Tool = "";
                session.DebugGiveItem("ITEM_TOOL_FLASHLIGHT");
                Assert.AreEqual("ITEM_TOOL_FLASHLIGHT", session.State.Loadout.Tool);
                Assert.IsTrue(session.State.Storage.Any(i => i.ItemId == "ITEM_TOOL_FLASHLIGHT"));
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        [Test]
        public void NewGamePlus_AppliesBonus_AndPlaysStorageFirst()
        {
            var directory = Path.Combine(Path.GetTempPath(), "ApoGirlNg_" + Guid.NewGuid().ToString("N"));
            try
            {
                var db = ShippedDb();
                var gallery = new EndingGallery(directory);
                var session = new GameSession(db, new SaveService(directory, new GameStateSerializer()), null, gallery);
                session.NewGame(5);
                Assert.IsFalse(session.State.Flags.Contains(GameSession.NgPlusFlag), "엔딩을 보기 전에는 1회차");

                gallery.Record("ENDING_SURVIVOR", 52);
                session.PendingStartBonus = db.Extras.StartBonuses.First(b => b.Id == "SUPPLIES");
                var foodBefore = NewGameFactory.Create(db.Balance, 5).Base.GetResource(ResourceType.Food);
                session.NewGame(5);
                Assert.IsTrue(session.State.Flags.Contains(GameSession.NgPlusFlag));
                Assert.Greater(session.State.Base.GetResource(ResourceType.Food), foodBefore, "시작 보너스 적용");
                Assert.IsNull(session.PendingStartBonus, "보너스는 한 번만");

                // "창고 안의 7일" 회상은 1주차 6일 밤 끝, 문이 열리기 직전 (TutorialWeekTests)
                Assert.IsTrue(session.HasPendingOpening);
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        /// <summary>헤드리스 52주 전체: 혼합 전략이 구조까지 버티고, 마지막 선택과 엔딩이 정해진다 (Temp/ApoGirlYearBalance.txt).</summary>
        [Test]
        public void FullYear_MixedStrategy_ReachesRescueEnding()
        {
            var db = ShippedDb();
            var report = new StringBuilder();
            var failures = new StringBuilder();
            var rescued = 0;
            foreach (var seed in Act1BalanceTests.Seeds(24))
            {
                var state = NewGameFactory.Create(db.Balance, seed);
                EffectApplier.Apply(state, db.Extras.Prologue.Common, db.Balance, new StateDelta());
                TutorialWeekRules.Apply(state, db);
                var runner = new WeekRunner(state, db, new SeededRandom(state.Rng), new HeadlessExpeditionRunner(db));
                while (state.Status == GameStatus.Playing)
                {
                    if (state.Calendar.Week > 1) YearPlan(state, db);
                    runner.PrepareWeek();
                    while (!runner.IsWeekComplete && !runner.IsGameOver) runner.ExecuteNextSlot();
                    if (runner.IsGameOver) break;
                    runner.FinishWeek();
                }

                var ending = EndingResolver.Resolve(state, db);
                report.AppendLine($"[seed {seed}] {state.Status} week {state.Calendar.Week} cause {state.EndCause} ending {ending.Ending?.Id} cuts {ending.CutKeys.Count} " +
                                  $"teacher {state.Teacher.InfectionProgress} final {string.Join(",", new[] { "TEACHER_LAST_LESSON", "TEACHER_ISOLATED", "TEACHER_EXPOSED", "CHOSE_TO_STAY", "TEACHER_LEFT_BEHIND" }.Where(state.Flags.Contains))} " +
                                  $"food {state.Base.GetResource(ResourceType.Food)} water {state.Base.GetResource(ResourceType.Water)} hp {state.Player.Vitals.Hp}");
                // 배드 목표 50%인 세계: 단순 계획기의 52주 생존은 시드에 크게 흔들린다. 시드 24개 중 1개 이상이 구조 엔딩까지 가면 통과 (밸런스 판단은 봇 시뮬레이터)
                var ok = state.Status == GameStatus.Rescued && state.Flags.Contains("FINAL_CHOICE") && ending.Ending != null && !ending.IsBad;
                if (ok)
                {
                    rescued++;
                }
                else
                {
                    failures.AppendLine($"seed {seed}: 구조 엔딩까지 못 감 ({state.Status}, {state.EndCause}, {state.Calendar.Week}주, 엔딩 {ending.Ending?.Id})");
                }
            }

            Directory.CreateDirectory("Temp");
            File.WriteAllText("Temp/ApoGirlYearBalance.txt", report.ToString());
            Debug.Log(report.ToString());
            Assert.GreaterOrEqual(rescued, 1, failures.ToString());
        }

        /// <summary>1~4막 혼합 전략 (주 2회 탐험, 해금된 가장 좋은 곳).</summary>
        private static void YearPlan(GameState state, IDefinitionDatabase db)
        {
            string Pick(params string[] ids) => ids.FirstOrDefault(id => db.TryGetAction(id, out var a) && ApoGirl.Core.Planning.PlanEditing.IsUnlocked(state, db, a)) ?? "";
            var first = Pick("ACT_EXP_DAY_MART", "ACT_EXP_DAY_STORE", "ACT_EXP_DAY_SCHOOL");
            var second = Pick("ACT_EXP_DAY_RESIDENTIAL", "ACT_EXP_DAY_STORE", "ACT_EXP_DAY_SCHOOL");
            var slots = state.Plan.Slots;
            for (var i = 0; i < slots.Count; i++)
            {
                if (slots[i].Locked) continue;
                var day = i % 2 == 0;
                var dayIndex = i / 2;
                var id = day
                    ? dayIndex == 1 ? first
                    : dayIndex == 4 ? second
                    : dayIndex == 3 ? Pick("ACT_SURV_RESTORE_POWER", "ACT_SURV_MAKE_CRUTCH", "ACT_SURV_BARRICADE")
                    : dayIndex == 6 ? "ACT_SURV_PURIFY_WATER"
                    : state.Player.Vitals.Infection >= 25 && dayIndex == 2 ? Pick("ACT_TREAT_INFECTION", "ACT_EDU_HUMANITIES")
                    : "ACT_EDU_HUMANITIES"
                    : dayIndex == 5 ? Pick("ACT_CARE_TEACHER", "ACT_NIGHT_TALK") : "ACT_REST_NIGHT";
                if (string.IsNullOrEmpty(id) || !db.TryGetAction(id, out var action) || !ApoGirl.Core.Planning.PlanEditing.IsUnlocked(state, db, action)
                    || !ApoGirl.Core.Planning.PlanEditing.IsAllowedIn(action, i))
                {
                    id = day ? "ACT_REST_DAY" : "ACT_REST_NIGHT";
                }

                slots[i].ActionId = id;
            }

            EffectHelpers.ApplyCareRules(state, db);
        }
    }
}
