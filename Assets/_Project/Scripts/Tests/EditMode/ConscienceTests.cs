using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>
    /// 양심의 무게 (사용자 결정 2026-11-27): 한 탐험에 좀비 5마리 이상 쓰러뜨리면 +1, 사람이 살던 방을 뒤지면 +1,
    /// 처음 오르면 고해 장면 깃발과 원인 깃발, 덜어 내기는 깃발을 켜지 않음, 헤드리스 기록은 같은 시드면 같다.
    /// </summary>
    public sealed class ConscienceTests
    {
        private static GameState Fresh(BalanceConfig balance)
        {
            var state = NewGameFactory.Create(balance, 1);
            state.Player.Conscience = 0;
            return state;
        }

        [Test]
        public void FelledBelowThreshold_NoWeight_ButCounted()
        {
            var balance = new BalanceConfig();
            var state = Fresh(balance);
            ConscienceRules.ApplyExpedition(state, new ExpeditionResult { ZombiesFelled = balance.ConscienceFelledThreshold - 1 }, balance, new StateDelta());
            Assert.AreEqual(0, state.Player.Conscience);
            Assert.AreEqual(balance.ConscienceFelledThreshold - 1, state.Exploration.ZombiesFelled);
            Assert.AreEqual(0, state.Exploration.HeavyFelledRuns);
            Assert.IsFalse(state.Flags.Contains(ConscienceRules.FirstFlag));
        }

        [Test]
        public void FelledAtThreshold_RaisesOnce_AndOpensConfession()
        {
            var balance = new BalanceConfig();
            var state = Fresh(balance);
            var delta = new StateDelta();
            ConscienceRules.ApplyExpedition(state, new ExpeditionResult { ZombiesFelled = balance.ConscienceFelledThreshold }, balance, delta);
            Assert.AreEqual(balance.ConscienceFelledAmount, state.Player.Conscience);
            Assert.AreEqual(1, state.Exploration.HeavyFelledRuns);
            Assert.IsTrue(state.Flags.Contains(ConscienceRules.FirstFlag));
            Assert.IsTrue(state.Flags.Contains(ConscienceRules.CauseFelled));
            Assert.Contains(ConscienceRules.FirstFlag, delta.FlagsSet);
        }

        [Test]
        public void OnlyFirstCause_IsRemembered()
        {
            var balance = new BalanceConfig();
            var state = Fresh(balance);
            ConscienceRules.ApplyExpedition(state, new ExpeditionResult { SearchedOwnedRoom = true }, balance, new StateDelta());
            ConscienceRules.ApplyExpedition(state, new ExpeditionResult { ZombiesFelled = 9 }, balance, new StateDelta());
            Assert.AreEqual(2, state.Player.Conscience);
            Assert.AreEqual(1, state.Exploration.OwnedRoomRuns);
            Assert.IsTrue(state.Flags.Contains(ConscienceRules.CauseHome));
            Assert.IsFalse(state.Flags.Contains(ConscienceRules.CauseFelled), "원인 깃발은 처음 한 번만");
        }

        [Test]
        public void Relief_DoesNotOpenConfession()
        {
            var balance = new BalanceConfig();
            var state = Fresh(balance);
            EffectApplier.ChangeConscience(state, -1, balance, new StateDelta());
            Assert.AreEqual(0, state.Player.Conscience);
            Assert.IsFalse(state.Flags.Contains(ConscienceRules.FirstFlag));
        }

        [Test]
        public void ChoiceEffect_MarksChoiceCause_AndConfessionRelievesOne()
        {
            var balance = new BalanceConfig();
            var state = Fresh(balance);
            EffectApplier.Apply(state, new GameEffect { Conscience = 1 }, balance, new StateDelta());
            Assert.AreEqual(1, state.Player.Conscience);
            Assert.IsTrue(state.Flags.Contains(ConscienceRules.CauseChoice));

            // 고해 장면의 선택지 효과 (conscience_events.json): 무게 −1, 마친 깃발
            EffectApplier.Apply(state, new GameEffect { Conscience = -1, SetFlags = { ConscienceRules.ConfessedFlag } }, balance, new StateDelta());
            Assert.AreEqual(0, state.Player.Conscience);
            Assert.IsTrue(state.Flags.Contains(ConscienceRules.ConfessedFlag));
        }

        [Test]
        public void Weight_IsCapped()
        {
            var balance = new BalanceConfig();
            var state = Fresh(balance);
            for (var i = 0; i < balance.ConscienceMax + 5; i++)
            {
                ConscienceRules.ApplyExpedition(state, new ExpeditionResult { ZombiesFelled = 20, SearchedOwnedRoom = true }, balance, new StateDelta());
            }

            Assert.AreEqual(balance.ConscienceMax, state.Player.Conscience);
        }

        [Test]
        public void OwnedRooms_HomesOnly_NurseRoomExcluded()
        {
            var balance = new BalanceConfig();
            Assert.IsTrue(ConscienceRules.IsOwnedRoom(balance, "ROOM_HOUSE"));
            Assert.IsTrue(ConscienceRules.IsOwnedRoom(balance, "ROOM_HOUSE_201"));
            Assert.IsFalse(ConscienceRules.IsOwnedRoom(balance, "ROOM_HOUSE_NURSE"), "박 선생님이 숨은 102호는 뺀다");
            Assert.IsFalse(ConscienceRules.IsOwnedRoom(balance, "ROOM_CLASSROOM"));
            Assert.IsFalse(ConscienceRules.IsOwnedRoom(balance, null));
        }

        [Test]
        public void Headless_SameSeedSameRecord_UnarmedFellsLittle()
        {
            var db = TestContent.CreateDatabase();
            ExpeditionResult Run(int seed) => new HeadlessExpeditionRunner(db).Run(new ExpeditionRequest
            {
                LocationId = TestContent.Location, Week = 3, Seed = seed, TimeOfDay = ExpeditionTime.Night, Hp = 100,
            });

            var maxUnarmed = (int)System.Math.Floor(db.Balance.HeadlessFelledNightMax * db.Balance.HeadlessFelledUnarmedRatio);
            for (var seed = 1; seed <= 40; seed++)
            {
                var a = Run(seed);
                var b = Run(seed);
                Assert.AreEqual(a.ZombiesFelled, b.ZombiesFelled);
                Assert.AreEqual(a.SearchedOwnedRoom, b.SearchedOwnedRoom);
                Assert.LessOrEqual(a.ZombiesFelled, maxUnarmed, "맨손(밀치기)으로는 거의 쓰러뜨리지 못한다");
            }
        }
    }
}
