using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Events;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Planning;
using ApoGirl.Core.Random;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>부상·청결·빗물 정수, 교실동 특수실·거처 선택 해금 (사용자 요청).</summary>
    public sealed class InjuryAndRoomsTests
    {
        [Test]
        public void Injury_FromThirtyPercentLoss_BlocksHealing_UntilTreated()
        {
            var db = M6Act4Tests.ShippedDb();
            var balance = db.Balance;
            var state = NewGameFactory.Create(balance, 1);
            var delta = new StateDelta();

            state.Player.Vitals.Hp = 71;
            Assert.AreEqual(0, InjuryRules.CheckIncident(state, 100, balance, delta), "29 잃은 것은 부상이 아니다");
            Assert.IsFalse(InjuryRules.IsInjured(state, balance));

            state.Player.Vitals.Cleanliness = 0;
            state.Player.Vitals.Infection = 0;
            state.Player.Vitals.Hp = 70;
            Assert.AreEqual(10, InjuryRules.CheckIncident(state, 100, balance, delta), "청결 0이면 상처 감염 +10");
            Assert.IsTrue(InjuryRules.IsInjured(state, balance));
            Assert.AreEqual(10, state.Player.Vitals.Infection);

            // 부상 중에는 휴식으로도 체력이 오르지 않는다 (의약품이 없으면)
            state.Base.SetResource(ResourceType.Medicine, 0);
            ActionResolver.Resolve(state, db.GetAction(balance.NightRestActionId), db, new SeededRandom(SeededRandom.CreateState(1)), delta);
            Assert.AreEqual(70, state.Player.Vitals.Hp);

            // 의약품이 없으면 치료되지 않는다
            var treat = db.GetAction("ACT_TREAT_WOUND");
            Assert.IsTrue(PlanEditing.IsUnlocked(state, db, treat), "부상이면 치료 행동이 열린다");
            state.Base.SetResource(ResourceType.Medicine, 0);
            ActionResolver.Resolve(state, treat, db, new SeededRandom(SeededRandom.CreateState(1)), delta);
            Assert.IsTrue(InjuryRules.IsInjured(state, balance), "약 없이는 낫지 않는다");

            state.Base.SetResource(ResourceType.Medicine, 1);
            ActionResolver.Resolve(state, treat, db, new SeededRandom(SeededRandom.CreateState(1)), delta);
            Assert.IsFalse(InjuryRules.IsInjured(state, balance));
            Assert.AreEqual(85, state.Player.Vitals.Hp, "치료하면서 회복 +15");
            Assert.AreEqual(0, state.Base.GetResource(ResourceType.Medicine));

            // 깨끗하면(청결 50 이상) 다쳐도 상처 감염은 없다 (부상은 그대로)
            state.Player.Vitals.Cleanliness = balance.WoundCleanThreshold;
            var infection = state.Player.Vitals.Infection;
            state.Player.Vitals.Hp = 50;
            Assert.AreEqual(1, InjuryRules.CheckIncident(state, 80, balance, delta));
            Assert.AreEqual(infection, state.Player.Vitals.Infection);
            Assert.IsTrue(InjuryRules.IsInjured(state, balance));
        }

        [Test]
        public void MedicineSkill_RaisesInjuryThreshold_AndHealsWithoutMedicine()
        {
            var db = M6Act4Tests.ShippedDb();
            var balance = db.Balance;
            Assert.AreEqual(30, InjuryRules.Threshold(balance, 0));
            Assert.AreEqual(40, InjuryRules.Threshold(balance, 2), "Lv 2면 40%");
            Assert.AreEqual(55, InjuryRules.Threshold(balance, 5));

            var state = NewGameFactory.Create(balance, 1);
            state.Player.Proficiency[ProficiencyType.Medicine] = new ProficiencyState { Level = 2, Xp = balance.ProficiencyXpThresholds[1] };
            state.Player.Vitals.Hp = 65;
            Assert.AreEqual(0, InjuryRules.CheckIncident(state, 100, balance, new StateDelta()), "35를 잃어도 Lv 2면 부상 아님");
            Assert.IsTrue(InjuryRules.PatchedUp(state, 100, balance), "대신 응급 치료로 마무리");
            state.Player.Vitals.Hp = 60;
            Assert.Greater(InjuryRules.CheckIncident(state, 100, balance, new StateDelta()), 0, "40이면 부상");

            // 응급처치 수업 성공: 의약품을 써서 부상이 풀린다 (의약품이 없으면 그대로)
            var lesson = db.GetAction("ACT_EDU_MEDICINE");
            var healed = false;
            for (var seed = 1; seed <= 20 && !healed; seed++)
            {
                state.Flags.Add(balance.InjuredFlag);
                state.Base.SetResource(ResourceType.Medicine, 0);
                var noMedicine = ActionResolver.Resolve(state, lesson, db, new SeededRandom(SeededRandom.CreateState(seed)), new StateDelta());
                Assert.IsTrue(InjuryRules.IsInjured(state, balance), "의약품이 없으면 수업에 성공해도 그대로 (" + noMedicine + ")");

                state.Base.SetResource(ResourceType.Medicine, 2);
                var result = ActionResolver.Resolve(state, lesson, db, new SeededRandom(SeededRandom.CreateState(seed)), new StateDelta());
                Assert.AreEqual(result == CheckResult.Failure, InjuryRules.IsInjured(state, balance), "성공하면 치유, 실패하면 그대로");
                Assert.AreEqual(result == CheckResult.Failure ? 2 : 1, state.Base.GetResource(ResourceType.Medicine), "치유하면 의약품 1");
                healed = result != CheckResult.Failure;
            }

            Assert.IsTrue(healed);
        }

        [Test]
        public void NightRest_WithMedicine_AlwaysTreatsInjury()
        {
            var db = M6Act4Tests.ShippedDb();
            var balance = db.Balance;
            var state = NewGameFactory.Create(balance, 1);
            state.Flags.Add(balance.InjuredFlag);
            state.Player.Vitals.Hp = 50;
            state.Base.SetResource(ResourceType.Medicine, 3);
            var delta = new StateDelta();
            ActionResolver.Resolve(state, db.GetAction(balance.NightRestActionId), db, new SeededRandom(SeededRandom.CreateState(1)), delta);
            Assert.IsFalse(InjuryRules.IsInjured(state, balance), "의약품이 있으면 휴식 때 반드시 써서 치유");
            Assert.AreEqual(2, state.Base.GetResource(ResourceType.Medicine));
            Assert.Greater(state.Player.Vitals.Hp, 50, "치유한 뒤 휴식 회복은 그대로");
            Assert.Contains(balance.InjuredFlag, delta.FlagsCleared);

            // 다치지 않았으면 의약품을 쓰지 않는다
            ActionResolver.Resolve(state, db.GetAction(balance.NightRestActionId), db, new SeededRandom(SeededRandom.CreateState(1)), new StateDelta());
            Assert.AreEqual(2, state.Base.GetResource(ResourceType.Medicine));

            // 낮잠으로는 치유되지 않는다 (밤 휴식만)
            state.Flags.Add(balance.InjuredFlag);
            ActionResolver.Resolve(state, db.GetAction(balance.DayRestActionId), db, new SeededRandom(SeededRandom.CreateState(1)), new StateDelta());
            Assert.IsTrue(InjuryRules.IsInjured(state, balance), "낮잠은 의약품을 쓰지 않는다");
            Assert.AreEqual(2, state.Base.GetResource(ResourceType.Medicine));
        }

        [Test]
        public void WeekRunner_ExpeditionWithBigLoss_SetsInjury()
        {
            var db = TestContent.CreateDatabase();
            var state = TestContent.NewGame(db);
            state.Plan.Slots[0].ActionId = TestContent.DayExpedition;
            var stub = new StubExpeditionRunner { Handler = _ => new ExpeditionResult { Status = ExtractStatus.Extracted, HpDelta = -35 } };
            var runner = new WeekRunner(state, db, new SeededRandom(state.Rng), stub);
            runner.PrepareWeek();
            var outcome = runner.ExecuteNextSlot();
            Assert.Greater(outcome.WoundInfection, 0);
            Assert.IsTrue(InjuryRules.IsInjured(state, db.Balance));
            Assert.IsTrue(outcome.StopForPresentation, "부상은 결과 카드로 보여 준다");

            // 응급처치 숙련이 있으면 조금 다친 탐험은 '응급 치료로 마무리'
            var state2 = TestContent.NewGame(db);
            state2.Player.Proficiency[ProficiencyType.Medicine] = new ProficiencyState { Level = 1, Xp = 10 };
            state2.Plan.Slots[0].ActionId = TestContent.DayExpedition;
            var stub2 = new StubExpeditionRunner { Handler = _ => new ExpeditionResult { Status = ExtractStatus.Extracted, HpDelta = -20 } };
            var runner2 = new WeekRunner(state2, db, new SeededRandom(state2.Rng), stub2);
            runner2.PrepareWeek();
            var patched = runner2.ExecuteNextSlot();
            Assert.IsTrue(patched.FirstAidPatched);
            Assert.AreEqual(0, patched.WoundInfection);
        }

        [Test]
        public void Purify_NeedsRainwater_AndRainBarrelGivesRawWater()
        {
            var db = M6Act4Tests.ShippedDb();
            var balance = db.Balance;
            var state = NewGameFactory.Create(balance, 1);
            var purify = db.GetAction("ACT_SURV_PURIFY_WATER");
            state.Base.SetResource(ResourceType.Water, 0);
            state.Base.SetResource(ResourceType.RawWater, 0);
            ActionResolver.Resolve(state, purify, db, new SeededRandom(SeededRandom.CreateState(1)), new StateDelta());
            Assert.AreEqual(0, state.Base.GetResource(ResourceType.Water), "빗물이 없으면 정수할 게 없다");

            Assert.IsFalse(ApoGirl.Core.Planning.PlanEditing.IsUnlocked(state, db, purify), "빗물이 없으면 계획에도 없다");

            // 한 번에 최대 12까지: 성공 80% · 대성공 100% · 실패 50%가 식수로
            state.Base.SetResource(ResourceType.RawWater, 15);
            ActionResolver.Resolve(state, purify, db, new SeededRandom(SeededRandom.CreateState(1)), new StateDelta());
            Assert.AreEqual(3, state.Base.GetResource(ResourceType.RawWater));
            Assert.That(state.Base.GetResource(ResourceType.Water), Is.EqualTo(6).Or.EqualTo(10).Or.EqualTo(12));

            // 빗물통은 식수가 아니라 빗물을 모은다, 청결은 주마다 떨어진다
            state.Flags.Add(UpkeepRules.FlagRainBarrels);
            state.Base.Weather = WeatherType.Rain;
            var water = state.Base.GetResource(ResourceType.Water);
            var raw = state.Base.GetResource(ResourceType.RawWater);
            state.Player.Vitals.Cleanliness = 50;
            UpkeepRules.ApplyWeekly(state, balance, new StateDelta());
            Assert.AreEqual(raw + balance.WeeklyRawWater + balance.RainCollectWater + balance.RainBarrelWater, state.Base.GetResource(ResourceType.RawWater));
            Assert.AreEqual(water, state.Base.GetResource(ResourceType.Water));
            Assert.AreEqual(50 - balance.CleanlinessWeeklyDecay, state.Player.Vitals.Cleanliness);
            Assert.AreEqual(balance.StartingCleanliness, NewGameFactory.Create(balance, 2).Player.Vitals.Cleanliness);
        }

        [Test]
        public void RoomSearch_SetsFlag_AndClassroomEventNeedsIt()
        {
            var location = new LocationDef { Id = "LOC_T", Layout = new BuildingLayoutDef() };
            location.Layout.RoomTemplates.Add(new RoomTemplateDef { Id = "ROOM_A", Containers = { new ContainerDef { Id = "X" } } });
            location.Layout.Doors.Add(new DoorDef { RoomId = "R0", TemplateId = "ROOM_A" });
            location.Layout.Doors.Add(new DoorDef { RoomId = "R1", TemplateId = "ROOM_A", SearchFlag = "SEARCHED_T" });
            var state = new GameState();
            ContainerRules.ApplyRoomFlags(location, new[] { HostileRules.RoomKey("LOC_T", 0, 0) }, state, new StateDelta());
            Assert.IsFalse(state.Flags.Contains("SEARCHED_T"));
            ContainerRules.ApplyRoomFlags(location, new[] { HostileRules.CorridorKey("LOC_T", 1), HostileRules.RoomKey("LOC_T", 1, 0) }, state, new StateDelta());
            Assert.IsTrue(state.Flags.Contains("SEARCHED_T"));

            var db = M6Act4Tests.ShippedDb();
            var game = NewGameFactory.Create(db.Balance, 1);
            game.Calendar.Week = 3;
            var ev = db.Events.First(e => e.Id == "EVT_A1_CLASSROOM");
            var context = new EventContext { ActionId = "ACT_EXP_DAY_SCHOOL", ActionCategory = ActionCategory.Expedition, Day = 2, Phase = DayPhase.Day };
            Assert.IsFalse(ConditionEvaluator.IsMet(game, ev.Condition, context, db.Balance), "1층 끝 교실을 뒤지기 전에는 안 뜬다");
            game.Flags.Add("SEARCHED_1F_END");
            Assert.IsTrue(ConditionEvaluator.IsMet(game, ev.Condition, context, db.Balance));
        }

        [Test]
        public void ClassroomChoice_OpensEducationOrDefenseFirst_BothAfterActOne()
        {
            var db = M6Act4Tests.ShippedDb();
            bool Open(GameState s, string id) => PlanEditing.IsUnlocked(s, db, db.GetAction(id));
            var education = new[] { "ACT_EDU_ENGINEERING", "ACT_EDU_SCIENCE", "ACT_EDU_MUSIC" };
            var defense = new[] { "ACT_SURV_TRAP", "ACT_SURV_ALARM", "ACT_SURV_COVERS" };

            GameState Make(params string[] flags)
            {
                var s = NewGameFactory.Create(db.Balance, 1);
                s.Calendar.Week = 6;
                s.Flags.Add("UI_DEFENSE");
                foreach (var f in flags) s.Flags.Add(f);
                return s;
            }

            var none = Make();
            Assert.IsTrue(education.Concat(defense).All(id => !Open(none, id)), "아직 아무것도 안 열림");

            var movedIn = Make("CLASSROOM_MOVED_IN");
            Assert.IsTrue(education.All(id => Open(movedIn, id)), "거처로 삼으면 교육 과목 먼저");
            Assert.IsTrue(defense.All(id => !Open(movedIn, id)));

            var stayed = Make("STAYED_STORAGE");
            Assert.IsTrue(defense.All(id => Open(stayed, id)), "창고에 남으면 방어 카드 먼저");
            Assert.IsTrue(education.All(id => !Open(stayed, id)));

            var later = Make("CLASSROOM_MOVED_IN", "ACT1_DONE");
            Assert.IsTrue(education.Concat(defense).All(id => Open(later, id)), "1막이 끝나면 둘 다");
        }
    }
}
