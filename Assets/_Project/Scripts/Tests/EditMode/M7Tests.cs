using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Events;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Random;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>M7-0: 혈액형 수혈 (D16), 헤드리스 탐험의 조우.</summary>
    public sealed class M7Tests
    {
        private static readonly EventContext Night = new EventContext
        {
            Phase = DayPhase.Night, Day = 3, ActionId = "ACT_REST_NIGHT", ActionCategory = ActionCategory.Rest,
        };

        // ── 혈액형 ──

        [Test]
        public void BloodRules_AboOnly()
        {
            Assert.IsTrue(BloodRules.CanDonate(BloodType.O, BloodType.B), "O형은 누구에게나");
            Assert.IsTrue(BloodRules.CanDonate(BloodType.B, BloodType.B));
            Assert.IsFalse(BloodRules.CanDonate(BloodType.A, BloodType.B));
            Assert.IsFalse(BloodRules.CanDonate(BloodType.AB, BloodType.B));
            Assert.IsTrue(BloodRules.CanDonate(BloodType.A, BloodType.AB), "AB형은 누구에게서나");
            Assert.IsFalse(BloodRules.CanDonate(BloodType.Unknown, BloodType.B), "모르면 줄 수 없다");
        }

        [TestCase(BloodType.O, "EVT_A4_BLOOD_MATCH")]
        [TestCase(BloodType.B, "EVT_A4_BLOOD_MATCH")]
        [TestCase(BloodType.A, "EVT_A4_BLOOD_MISMATCH")]
        [TestCase(BloodType.AB, "EVT_A4_BLOOD_MISMATCH")]
        [TestCase(BloodType.Unknown, "EVT_A4_BLOOD_UNKNOWN")]
        public void BloodRecord_BranchesByPlayerBloodType(BloodType blood, string expected)
        {
            var db = M6Act4Tests.ShippedDb();
            var state = M6Act4Tests.CureReady(db, 1);
            state.Flags.Remove("TEACHER_BLOOD_KNOWN");
            state.Player.Profile.BloodType = blood;
            if (blood == BloodType.Unknown) state.Flags.Add("BLOOD_UNKNOWN");

            var met = new[] { "EVT_A4_BLOOD_MATCH", "EVT_A4_BLOOD_MISMATCH", "EVT_A4_BLOOD_UNKNOWN" }
                .Where(id => db.TryGetEvent(id, out var ev) && ConditionEvaluator.IsMet(state, ev.Condition, Night, db.Balance))
                .ToList();
            CollectionAssert.AreEqual(new[] { expected }, met, "혈액형마다 기록 이벤트는 하나만");

            Assert.IsTrue(db.TryGetEvent("EVT_A4_CURE_ATTEMPT", out var attempt));
            Assert.IsFalse(ConditionEvaluator.IsMet(state, attempt.Condition, Night, db.Balance), "선생님 혈액형을 알기 전에는 처치 불가");
        }

        [Test]
        public void CureAttempt_MismatchNeedsBloodPack()
        {
            var db = M6Act4Tests.ShippedDb();
            Assert.IsTrue(db.TryGetEvent("EVT_A4_CURE_ATTEMPT", out var ev));
            var state = M6Act4Tests.CureReady(db, 2);
            state.Player.Profile.BloodType = BloodType.A;
            Assert.IsFalse(ConditionEvaluator.IsMet(state, ev.Condition, Night, db.Balance), "부적합 + 혈액 팩 없음 → 시도 불가");

            state.Flags.Add("BLOOD_PACK");
            Assert.IsTrue(ConditionEvaluator.IsMet(state, ev.Condition, Night, db.Balance), "혈액 팩이 있으면 시도 가능 (루트는 잠기지 않는다)");
            var choices = EventResolver.Choices(state, ev, Night, db.Balance).Select(v => v.Id).ToList();
            CollectionAssert.Contains(choices, "TREAT_PACK");
            CollectionAssert.DoesNotContain(choices, "TREAT_SELF");

            EventResolver.Choose(state, ev, "TREAT_PACK", Night, db.Balance, new SeededRandom(SeededRandom.CreateState(11)), new StateDelta());
            Assert.IsTrue(state.Flags.Contains("CURE_ATTEMPTED"));
            Assert.IsFalse(state.Flags.Contains("BLOOD_PACK"), "혈액 팩은 성공·실패 모두 소모");
        }

        [Test]
        public void CureAttempt_DirectTransfusionCostsPlayer()
        {
            var db = M6Act4Tests.ShippedDb();
            Assert.IsTrue(db.TryGetEvent("EVT_A4_CURE_ATTEMPT", out var ev));
            var state = M6Act4Tests.CureReady(db, 3);
            state.Player.Vitals.Hp = 80;
            state.Player.Vitals.Fatigue = 0;
            var choices = EventResolver.Choices(state, ev, Night, db.Balance).Select(v => v.Id).ToList();
            CollectionAssert.Contains(choices, "TREAT_SELF");
            CollectionAssert.DoesNotContain(choices, "TREAT_PACK", "혈액 팩이 없으면 팩 선택지 없음");

            EventResolver.Choose(state, ev, "TREAT_SELF", Night, db.Balance, new SeededRandom(SeededRandom.CreateState(5)), new StateDelta());
            Assert.Less(state.Player.Vitals.Hp, 80, "직접 수혈은 체력 부담");
            Assert.Greater(state.Player.Vitals.Fatigue, 0);
        }

        // ── 헤드리스 조우 ──

        private static (InMemoryDefinitionDatabase Db, ExpeditionModelDef Model, LocationDef Location) EncounterDb()
        {
            var db = TestContent.CreateDatabase();
            var model = db.GetExpeditionModel(TestContent.Location);
            model.Day.BaseDownChance = 0f;
            model.Day.EncounterChance = 1f;
            Assert.IsTrue(db.TryGetLocation(TestContent.Location, out var location));
            location.DayVariant.Encounters.Add(new EncounterDef
            {
                Id = "KID", FlagsNone = { "KID_MET" },
                Choices =
                {
                    new EncounterChoiceDef { Id = "TAKE", SetFlags = { "KID_MET" }, AddClues = { "CLUE_KID" }, StressDelta = 3 },
                },
            });
            return (db, model, location);
        }

        private static ExpeditionRequest DayRequest(GameState state, IDefinitionDatabase db, int seed)
        {
            return ExpeditionRequestFactory.Create(state, db.GetAction(TestContent.DayExpedition), seed, false, db);
        }

        [Test]
        public void Headless_MeetsActiveEncounter()
        {
            var (db, _, _) = EncounterDb();
            var state = TestContent.NewGame(db);
            var result = new HeadlessExpeditionRunner(db).Run(DayRequest(state, db, 7));

            Assert.AreEqual(ExtractStatus.Extracted, result.Status);
            CollectionAssert.Contains(result.Flags, "KID_MET");
            CollectionAssert.Contains(result.Flags, "ENC_KID_TAKE", "선택 기록 플래그도 실시간 탐험과 같다");
            CollectionAssert.Contains(result.Clues, "CLUE_KID");
        }

        [Test]
        public void Headless_SkipsInactiveOrDownOrZeroChance()
        {
            var (db, model, _) = EncounterDb();
            var state = TestContent.NewGame(db);
            state.Flags.Add("KID_MET");
            Assert.IsEmpty(new HeadlessExpeditionRunner(db).Run(DayRequest(state, db, 7)).Flags, "조건이 안 맞는 조우는 없다");

            state.Flags.Remove("KID_MET");
            model.Day.EncounterChance = 0f;
            Assert.IsEmpty(new HeadlessExpeditionRunner(db).Run(DayRequest(state, db, 7)).Flags, "확률 0");

            model.Day.EncounterChance = 1f;
            model.Day.BaseDownChance = 1f;
            var down = new HeadlessExpeditionRunner(db).Run(DayRequest(state, db, 7));
            Assert.AreEqual(ExtractStatus.Down, down.Status);
            Assert.IsEmpty(down.Flags, "쓰러지면 조우 결과도 없다");
        }

        [Test]
        public void Headless_EncounterRollsDoNotChangeLoot()
        {
            var (db, model, _) = EncounterDb();
            var state = TestContent.NewGame(db);
            var with = new HeadlessExpeditionRunner(db).Run(DayRequest(state, db, 21));
            model.Day.EncounterChance = 0f;
            var without = new HeadlessExpeditionRunner(db).Run(DayRequest(state, db, 21));

            CollectionAssert.AreEqual(
                without.ResourceLoot.Select(r => r.Resource + ":" + r.Amount).ToList(),
                with.ResourceLoot.Select(r => r.Resource + ":" + r.Amount).ToList(),
                "조우 굴림은 기존 굴림 뒤라 자원 결과가 같다");
        }
    }
}
