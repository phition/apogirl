using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Planning;
using ApoGirl.Core.Random;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.Simulation.Bots;
using ApoGirl.Core.State;
using ApoGirl.Systems.Content;
using ApoGirl.Systems.Save;
using ApoGirl.Systems.Session;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>선생님 추천 편성 (Docs/TEACHER_PLAN_PROPOSAL.md): 추천 규칙, 고정 시간표, 칸 옮기기, 흐름 예측, 편집 기록.</summary>
    public sealed class TeacherPlannerTests
    {
        private static InMemoryDefinitionDatabase ContentDb()
        {
            var content = ContentJson.LoadFolder(ContentPipelineTests.ProjectPath(ContentJson.ContentFolder));
            return new InMemoryDefinitionDatabase(
                new BalanceConfig(), content.Actions, content.ExpeditionModels, content.Items, content.LootTables,
                null, content.Enemies, content.Events, content.Npcs,
                new DefinitionExtras
                {
                    Traits = content.Traits, Facilities = content.Facilities, Lines = content.Lines, Prologue = content.Prologue ?? new PrologueDef(),
                    Endings = content.Endings, Roles = content.Roles, StartBonuses = content.StartBonuses, Factions = content.Factions,
                    Advice = content.Advice,
                });
        }

        private static BotConfigDef Config() => BotConfigJson.Load(ContentPipelineTests.ProjectPath(BotConfigJson.DefaultPath));

        /// <summary>
        /// TEACHER 봇처럼 한 주씩 돌린다 (선생님이 있으면 추천, 없으면 봇 칸). onPlanned는 주마다 계획한 직후 불린다.
        /// stopWeek 주의 계획 단계에서 멈춘다 (그 주는 계획하지 않은 채).
        /// </summary>
        private static GameState Run(IDefinitionDatabase db, int seed, int stopWeek, Action<GameState, TeacherResult, List<StoryHint>> onPlanned = null)
        {
            var config = Config();
            var strategy = config.Strategies.First(s => s.Id == "TEACHER");
            var state = NewGameFactory.Create(db.Balance, seed);
            TutorialWeekRules.Apply(state, db);
            var botRandom = new SeededRandom(SeededRandom.CreateState(seed));
            var runner = new WeekRunner(state, db, new SeededRandom(state.Rng), new HeadlessExpeditionRunner(db, new BotEncounterChooser(1f)),
                new BotEventChooser(config.CommonChoiceBias, botRandom));
            runner.ForceEvent(db.Balance.IntroEventId);
            while (state.Status == GameStatus.Playing && state.Calendar.Week < stopWeek)
            {
                if (TeacherPlanner.Available(state, db.Balance))
                {
                    var hints = StoryHints.PendingLocations(state, db);
                    var result = TeacherPlanner.Recommend(state, db);
                    onPlanned?.Invoke(state, result, hints);
                }
                else
                {
                    BotPlanner.Plan(state, db, config, strategy, botRandom);
                }

                runner.PrepareWeek();
                while (!runner.IsWeekComplete && !runner.IsGameOver)
                {
                    runner.ExecuteNextSlot();
                }

                if (runner.IsGameOver)
                {
                    break;
                }

                runner.FinishWeek();
            }

            return state;
        }

        /// <summary>week 주 계획 단계 (한 번 진행해 둔 판을 복사해 쓴다). 식량·물은 넉넉히, 감염·부상은 없앤다.</summary>
        private static GameState Week(IDefinitionDatabase db, int week, int seed = 3)
        {
            var key = (week, seed);
            if (!Cache.TryGetValue(key, out var saved))
            {
                var state = Run(db, seed, week);
                Assert.AreEqual(GameStatus.Playing, state.Status, $"{seed}판이 {week}주 전에 끝났다");
                Assert.AreEqual(week, state.Calendar.Week);
                saved = Serializer.Serialize(state);
                Cache[key] = saved;
            }

            var copy = Serializer.Deserialize(saved);
            copy.Base.SetResource(ResourceType.Food, 80);
            copy.Base.SetResource(ResourceType.Water, 80);
            copy.Player.Vitals.Infection = 0;
            copy.Flags.Remove("INJURED");
            return copy;
        }

        private static readonly GameStateSerializer Serializer = new GameStateSerializer();
        private static readonly Dictionary<(int, int), string> Cache = new Dictionary<(int, int), string>();

        private static string UnlockedClass(GameState state, IDefinitionDatabase db, int slot) =>
            db.Actions.Where(a => a.Category == ActionCategory.Education && PlanEditing.IsUnlocked(state, db, a) && PlanEditing.IsAllowedIn(a, slot)
                                  && a.ResourceCosts.Count == 0)
                .OrderBy(a => a.Id, StringComparer.Ordinal).Select(a => a.Id).First();

        private static bool IsExpedition(IDefinitionDatabase db, string id) => db.TryGetAction(id ?? "", out var a) && a.IsExpedition;

        private static bool IsRest(IDefinitionDatabase db, string id) => db.TryGetAction(id ?? "", out var a) && a.IsRest;

        /// <summary>추천이 지켜야 할 것: 빈 칸 없음, 칸마다 둘 수 있는 행동, 휴식 3+, 탐험 1~3, 일상 1+, 고정 칸.</summary>
        private static void AssertRules(GameState state, IDefinitionDatabase db, TeacherResult result, string where)
        {
            var slots = state.Plan.Slots;
            var balance = db.Balance;
            for (var i = 0; i < slots.Count; i++)
            {
                Assert.IsNotEmpty(slots[i].ActionId, $"{where}: {TeacherPins.Key(i)} 빈 칸");
                if (slots[i].Locked)
                {
                    continue;
                }

                Assert.IsTrue(db.TryGetAction(slots[i].ActionId, out var action), $"{where}: {slots[i].ActionId}");
                Assert.IsTrue(PlanEditing.IsAllowedIn(action, i), $"{where}: {action.Id}은 {TeacherPins.Key(i)}에 못 둔다");
                Assert.IsTrue(PlanEditing.IsUnlocked(state, db, action), $"{where}: {action.Id} 잠김");
            }

            if (slots.Any(s => s.Locked))
            {
                return;   // 창고 주·잠긴 주는 개수 규칙을 보지 않는다
            }

            var expeditions = slots.Count(s => IsExpedition(db, s.ActionId));
            Assert.GreaterOrEqual(slots.Count(s => IsRest(db, s.ActionId)), balance.TeacherMinRests, $"{where}: 휴식");
            Assert.LessOrEqual(expeditions, balance.TeacherMaxExpeditions, $"{where}: 탐험 상한");
            var anyRun = db.Actions.Any(a => a.IsExpedition && PlanEditing.IsUnlocked(state, db, a) && !BotPlanner.Exhausted(state, db, a));
            if (anyRun)
            {
                Assert.GreaterOrEqual(expeditions, balance.TeacherMinExpeditions, $"{where}: 탐험 보장");
            }

            var daily = Enumerable.Range(0, slots.Count).Count(i => !result.Pinned[i] && db.TryGetAction(slots[i].ActionId, out var a) && a.Category == ActionCategory.Daily);
            Assert.GreaterOrEqual(daily, balance.TeacherMinDaily, $"{where}: 일상");
            var dayRuns = Enumerable.Range(0, slots.Count).Any(i => IsExpedition(db, slots[i].ActionId) && CalendarRules.PhaseOf(i) == DayPhase.Day);
            var nightRuns = Enumerable.Range(0, slots.Count).Any(i => IsExpedition(db, slots[i].ActionId) && CalendarRules.PhaseOf(i) == DayPhase.Night);
            var shortage = result.Reasons.Any(r => r != null && (r.Key == "teacher.why.short" || r.Key == "teacher.why.tight"));
            Assert.IsFalse(dayRuns && nightRuns && !shortage, $"{where}: 낮·밤 탐험은 식량·물이 모자란 주에만 섞는다");
            Assert.LessOrEqual(result.Notes.Count, balance.TeacherNoteLines);
        }

        [Test]
        public void Recommend_KeepsRulesThroughAYear()
        {
            // TEACHER 봇처럼 한 해를 돌리며 주마다 추천을 점검한다.
            // 판이 일찍 끝나면 (밸런스는 마지막에 맞춘다, D26) 다음 시드로 이어서 추천한 주를 40주 넘게 모은다.
            var db = ContentDb();
            Assert.IsTrue(Config().Strategies.First(s => s.Id == "TEACHER").TeacherPlan);
            var recommended = 0;
            var ends = new List<string>();
            foreach (var seed in new[] { 11, 12, 13, 14, 15 })
            {
                var state = Run(db, seed, 53, (st, result, hints) =>
                {
                    recommended++;
                    AssertRules(st, db, result, st.Calendar.Week + "주");

                    // 이야기가 기다리는 탐험(낮·밤까지 정해진)이 둘 수 있는 것이면 들어간다
                    var placeable = hints.FirstOrDefault(h => !h.IsEncounter && db.Actions.Any(a => a.Id == h.ActionId && !BotPlanner.Exhausted(st, db, a)));
                    if (placeable != null)
                    {
                        Assert.IsTrue(hints.Any(h => st.Plan.Slots.Any(s => s.ActionId == h.ActionId)),
                            $"{st.Calendar.Week}주: 이야기 탐험 {placeable.ActionId}");
                    }
                });
                ends.Add($"{seed}판 {state.Status} {state.Calendar.Week}주");
                if (recommended >= 40)
                {
                    break;
                }
            }

            Assert.GreaterOrEqual(recommended, 40, "선생님이 짠 주 (끝: " + string.Join(", ", ends) + ")");
        }

        [Test]
        public void Recommend_IsDeterministic_AndKeepsPlayerSlots()
        {
            var db = ContentDb();
            var state = Week(db, 20);
            var first = TeacherPlanner.Recommend(state, db);
            var ids = PlanEditing.Snapshot(state);
            TeacherPlanner.Recommend(state, db);
            CollectionAssert.AreEqual(ids, PlanEditing.Snapshot(state), "같은 상태면 같은 추천");
            Assert.IsTrue(first.Placed.Any(p => p));

            // 플레이어가 넣은 칸은 둔다
            PlanEditing.ClearAll(state, db);
            var mine = UnlockedClass(state, db, 4);
            Assert.AreEqual(PlanEditResult.Ok, PlanEditing.SetSlot(state, db, 4, mine));
            var result = TeacherPlanner.Recommend(state, db, null, new HashSet<int> { 4 });
            Assert.AreEqual(mine, state.Plan.Slots[4].ActionId);
            Assert.IsFalse(result.Placed[4]);
        }

        [Test]
        public void Pins_DiaryOnSaturdayNight_NoClubBeforeWeek14_AndLockedPinIsSkipped()
        {
            var db = ContentDb();
            var state = Week(db, 5);
            var result = TeacherPlanner.Recommend(state, db);
            var n3 = BotPlanner.SlotIndex("N3");
            var n6 = BotPlanner.SlotIndex("N6");
            Assert.AreEqual("ACT_NIGHT_DIARY", state.Plan.Slots[n6].ActionId);
            Assert.IsTrue(result.Pinned[n6]);
            Assert.IsFalse(result.Pinned[n3], "14주 전에는 동아리 고정 칸이 없다 (D7)");
            Assert.IsFalse(db.TryGetAction(state.Plan.Slots[n3].ActionId, out var a3) && a3.IsClub);

            // 열리지 않은 행동의 고정 칸은 그 주만 건너뛴다 (고정은 남는다, D2)
            var pins = new[] { "D2=ACT_NO_SUCH_ACTION", "N6=ACT_NIGHT_DIARY" };
            result = TeacherPlanner.Recommend(state, db, pins);
            Assert.IsFalse(result.Pinned[BotPlanner.SlotIndex("D2")]);
            Assert.AreEqual(2, TeacherPins.Parse(pins).Count);
            CollectionAssert.AreEqual(pins, TeacherPins.Format(TeacherPins.Parse(pins)));
            Assert.AreEqual(TeacherPins.ClubToken, TeacherPins.TokenFor(db, db.Actions.First(a => a.IsClub).Id));
        }

        [Test]
        public void FoodRunningOut_AddsFoodExpeditions()
        {
            var db = ContentDb();
            var plenty = Week(db, 20);
            plenty.Base.SetResource(ResourceType.Food, 200);
            plenty.Base.SetResource(ResourceType.Water, 200);
            var calm = TeacherPlanner.Recommend(plenty, db);
            var calmRuns = plenty.Plan.Slots.Count(s => IsExpedition(db, s.ActionId));

            var hungry = Week(db, 20);
            hungry.Base.SetResource(ResourceType.Food, 0);
            Assert.That(BaseForecast.Food(hungry, db).ShortDay, Is.InRange(1, db.Balance.TeacherCriticalShortDay));
            var result = TeacherPlanner.Recommend(hungry, db);
            var runs = hungry.Plan.Slots.Count(s => IsExpedition(db, s.ActionId));
            Assert.Greater(runs, calmRuns, "식량이 바닥나면 탐험을 더 넣는다");
            Assert.IsTrue(result.Reasons.Any(r => r != null && r.Key == "teacher.why.short"));
            Assert.IsFalse(calm.Reasons.Any(r => r != null && r.Key == "teacher.why.short"));
        }

        [Test]
        public void Infected_WithoutMedicine_GoesForMedicine_WithMedicine_Treats()
        {
            var db = ContentDb();
            var state = Week(db, 20);
            state.Player.Vitals.Infection = 40;
            state.Base.SetResource(ResourceType.Medicine, 6);
            TeacherPlanner.Recommend(state, db);
            Assert.IsTrue(state.Plan.Slots.Any(s => s.ActionId == "ACT_TREAT_INFECTION"));

            state.Base.SetResource(ResourceType.Medicine, 0);
            var medicinePlaces = db.Actions.Where(a => a.IsExpedition && PlanEditing.IsUnlocked(state, db, a) && db.TryGetExpeditionModel(a.LocationId, out var m)
                                                       && m.Day.Resources.Concat(m.Night.Resources).Any(r => r.Resource == ResourceType.Medicine)).ToList();
            Assert.IsNotEmpty(medicinePlaces, "20주면 의약품이 나오는 곳이 열려 있다");
            var result = TeacherPlanner.Recommend(state, db);
            Assert.IsFalse(state.Plan.Slots.Any(s => s.ActionId == "ACT_TREAT_INFECTION"));
            Assert.IsTrue(result.Reasons.Any(r => r != null && r.Key == "teacher.why.medicine"));
        }

        [Test]
        public void TeacherAbsent_CannotRecommend()
        {
            var db = ContentDb();
            var state = Week(db, 20);
            Assert.IsTrue(TeacherPlanner.Available(state, db.Balance));
            state.Flags.Add(db.Balance.TeacherAbsentFlags.First());
            Assert.IsFalse(TeacherPlanner.Available(state, db.Balance));
            state.Flags.Clear();
            state.Events.Records.Remove(db.Balance.OpeningEventId);
            Assert.IsFalse(TeacherPlanner.Available(state, db.Balance), "창고 문을 열기 전에는 없다");
        }

        [Test]
        public void Move_SwapsAndChecksSlotTime()
        {
            var db = ContentDb();
            var state = Week(db, 20);
            var d1 = BotPlanner.SlotIndex("D1");
            var d3 = BotPlanner.SlotIndex("D3");
            var n2 = BotPlanner.SlotIndex("N2");
            PlanEditing.ClearAll(state, db);
            var study = UnlockedClass(state, db, d1);
            Assert.AreEqual(PlanEditResult.Ok, PlanEditing.SetSlot(state, db, d1, study));
            Assert.AreEqual(PlanEditResult.Ok, PlanEditing.SetSlot(state, db, d3, "ACT_REST_DAY"));
            Assert.AreEqual(PlanEditResult.Ok, PlanEditing.Move(state, db, d1, d3));
            Assert.AreEqual("ACT_REST_DAY", state.Plan.Slots[d1].ActionId);
            Assert.AreEqual(study, state.Plan.Slots[d3].ActionId);

            // 낮 휴식은 밤 칸으로 못 옮긴다
            Assert.AreEqual(PlanEditResult.WrongSlot, PlanEditing.Move(state, db, d1, n2));
            Assert.AreEqual("ACT_REST_DAY", state.Plan.Slots[d1].ActionId);
            Assert.AreEqual(PlanEditResult.UnknownAction, PlanEditing.Move(state, db, BotPlanner.SlotIndex("D5"), d1));
        }

        [Test]
        public void Forecast_MatchesRestfulWeek()
        {
            var db = ContentDb();
            // 이벤트·습격 없이 세 칸 넘게 흘러가는 주를 고른다 (판 진행이 바뀌어도 테스트가 흔들리지 않게)
            var checkedSlots = 0;
            foreach (var week in new[] { 20, 18, 22, 24, 26, 16, 28, 30, 32, 34, 36, 38, 15, 17, 19, 21, 23 })
            {
                checkedSlots = Math.Max(checkedSlots, RestfulWeekMatches(db, week));
                if (checkedSlots > 2)
                {
                    break;
                }
            }

            Assert.Greater(checkedSlots, 2);
        }

        /// <summary>그 주를 모두 휴식으로 채워 예측과 실제를 칸마다 비교한다. 놀랄 일 없이 지나간 칸 수를 돌려준다.</summary>
        private static int RestfulWeekMatches(IDefinitionDatabase db, int week)
        {
            var state = Week(db, week);
            state.Player.Vitals.Fatigue = 70;
            state.Player.Vitals.Stress = 40;
            for (var i = 0; i < state.Plan.Slots.Count; i++)
            {
                Assert.AreEqual(PlanEditResult.Ok, PlanEditing.SetSlot(state, db, i, PlanPreparer.RestIdFor(i, db.Balance)));
            }

            var forecast = WeekForecast.Project(state, db);
            Assert.AreEqual(70, forecast.StartFatigue);
            // 예측은 휴식의 기본 회복만 센다 (동료 위로·고양이·특기·이벤트는 세지 않음): 실제는 예측과 같거나 낫다
            var runner = new WeekRunner(state, db, new SeededRandom(state.Rng), new HeadlessExpeditionRunner(db));
            var surprise = false;
            runner.SlotResolved += outcome => surprise |= outcome.Events.Count > 0 || outcome.Raid != null || outcome.IntrusionTriggered;
            runner.PrepareWeek();
            var slot = 0;
            while (!runner.IsWeekComplete && !runner.IsGameOver && !surprise)
            {
                runner.ExecuteNextSlot();
                if (surprise)
                {
                    break; // 습격·이벤트가 난 칸은 예측 밖이라 비교하지 않는다
                }

                Assert.LessOrEqual(Math.Abs(forecast.Slots[slot].Fatigue - state.Player.Vitals.Fatigue), 5, $"{TeacherPins.Key(slot)} 피로");
                Assert.LessOrEqual(state.Player.Vitals.Stress, forecast.Slots[slot].Stress, $"{TeacherPins.Key(slot)} 스트레스 (보수적)");
                Assert.AreEqual(forecast.Slots[slot].SleepDebt, state.Player.SleepDebt, $"{TeacherPins.Key(slot)} 수면 부채");
                slot++;
            }

            Assert.AreEqual(0, forecast.Last.Fatigue);
            return slot;
        }

        [Test]
        public void Session_RecommendUndoRedo_AndEditErasesChalk()
        {
            var directory = Path.Combine(Path.GetTempPath(), "ApoGirlTeacherTest_" + Guid.NewGuid().ToString("N"));
            try
            {
                var db = ContentDb();
                var saves = new SaveService(directory, new GameStateSerializer());
                var session = new GameSession(db, saves);
                session.NewGame(4);
                Assert.IsFalse(session.TeacherAvailable, "1주차 창고: 선생님을 아직 못 만났다");

                var week = Week(db, 20);
                PlanEditing.ClearAll(week, db);
                saves.Save(SaveService.AutoSlot, week);
                session.Load(SaveService.AutoSlot);
                Assert.IsTrue(session.TeacherAvailable);
                Assert.IsFalse(session.CanUndoPlan);

                var d4 = BotPlanner.SlotIndex("D4");
                var mine = UnlockedClass(session.State, db, d4);
                Assert.AreEqual(PlanEditResult.Ok, session.SetSlot(d4, mine));
                var result = session.Recommend();
                Assert.IsNotNull(result);
                Assert.AreEqual(mine, session.State.Plan.Slots[d4].ActionId, "플레이어 칸은 둔다");
                Assert.IsFalse(session.IsTeacherSlot(d4));
                var teacherSlot = Enumerable.Range(0, 14).First(i => session.IsTeacherSlot(i) && !session.State.Plan.Slots[i].Locked && !IsRest(db, session.State.Plan.Slots[i].ActionId));
                Assert.IsNotNull(session.TeacherReason(teacherSlot));
                var recommended = session.PlanIds();

                // 선생님 칸을 고치면 분필 자국이 지워진다
                var other = CalendarRules.PhaseOf(teacherSlot) == DayPhase.Day ? "ACT_REST_DAY" : "ACT_REST_NIGHT";
                Assert.AreEqual(PlanEditResult.Ok, session.SetSlot(teacherSlot, other));
                Assert.IsFalse(session.IsTeacherSlot(teacherSlot));

                // 되돌리기: 편집 → 추천 → 플레이어 칸 순서로
                Assert.IsTrue(session.UndoPlan());
                CollectionAssert.AreEqual(recommended, session.PlanIds());
                Assert.IsTrue(session.IsTeacherSlot(teacherSlot), "되돌리면 분필 자국도 돌아온다");
                Assert.IsTrue(session.UndoPlan());
                Assert.IsFalse(session.IsTeacherSlot(teacherSlot));
                Assert.AreEqual(mine, session.State.Plan.Slots[d4].ActionId);
                Assert.IsTrue(session.RedoPlan());
                CollectionAssert.AreEqual(recommended, session.PlanIds());

                // 다시 추천(전부)은 플레이어 칸도 고친다
                session.Recommend(all: true);
                Assert.IsTrue(session.IsTeacherSlot(d4), "전부 다시 짜면 선생님 칸이 된다");

                // 놓아 보기는 계획을 바꾸지 않는다
                var before = session.PlanIds();
                var ghost = session.PreviewIds(-1, d4, "ACT_REST_DAY");
                Assert.IsNotNull(ghost);
                Assert.AreEqual("ACT_REST_DAY", ghost[d4]);
                CollectionAssert.AreEqual(before, session.PlanIds());
                Assert.IsNull(session.PreviewIds(-1, BotPlanner.SlotIndex("N4"), "ACT_REST_DAY"), "낮 휴식은 밤 칸에 못 놓는다");

                // 전부 비우기도 한 번에 되돌린다
                Assert.IsTrue(session.ClearPlan());
                Assert.IsTrue(session.UndoPlan());
                CollectionAssert.AreEqual(before, session.PlanIds());
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }
    }
}
