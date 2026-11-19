using ApoGirl.Core.Definitions;
using ApoGirl.Core.Random;
using ApoGirl.Exploration.Model;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    public sealed class DoorPlayTests
    {
        private BalanceConfig _balance;
        private ThreatSimulation _sim;
        private readonly EnemyDef _walker = new EnemyDef { SightRange = 7f, AwarenessRate = 2f };

        [SetUp]
        public void SetUp()
        {
            _balance = new BalanceConfig();
            _sim = new ThreatSimulation(_balance, new SeededRandom(SeededRandom.CreateState(5)));
            _sim.SetAreaWidth(0, 14f);
        }

        [Test]
        public void Peek_FacingZombieNotices_AndComesToDoor()
        {
            var zombie = _sim.AddZombie(_walker, 0, 2.5f);
            zombie.Facing = -1;
            zombie.State = ZombieState.Idle;
            var noticed = false;
            for (var i = 0; i < 20 && !noticed; i++)
            {
                noticed = _sim.ExposeAt(0, 1.2f, 0.1f, ActivityMods.Neutral);
            }

            Assert.IsTrue(noticed);
            Assert.AreEqual(ZombieState.Investigate, zombie.State);
            Assert.AreEqual(1.2f, zombie.TargetX);
        }

        [Test]
        public void Peek_FarOrBackTurnedZombie_DoesNotNotice()
        {
            var far = _sim.AddZombie(_walker, 0, 9f);
            far.Facing = -1;
            var back = _sim.AddZombie(_walker, 0, 2.5f);
            back.Facing = 1;
            for (var i = 0; i < 30; i++)
            {
                Assert.IsFalse(_sim.ExposeAt(0, 1.2f, 0.1f, ActivityMods.Neutral));
            }

            Assert.AreEqual(0f, far.Awareness, "7 × 0.35 = 2.45 밖");
            Assert.AreEqual(0f, back.Awareness, "등 돌림");
        }

        [Test]
        public void Listen_ReportsAwakeAndSleeping()
        {
            var crowd = _sim.AddCrowd(0, 5f);
            _sim.AddZombie(_walker, 0, 5f, crowd);
            _sim.AddZombie(_walker, 0, 12f, crowd);
            _sim.AddZombie(_walker, 0, 3f);
            _sim.AddZombie(_walker, 1, 1f);

            var heard = _sim.Listen(0, 1.2f);
            Assert.AreEqual(1, heard.AwakeCount, "다른 공간 제외");
            Assert.AreEqual(1.8f, heard.NearestAwake, 1e-4f);
            Assert.AreEqual(1, heard.SleepingNearby, "8유닛 안의 숨소리만");

            var quiet = _sim.Listen(1, 13f);
            Assert.AreEqual(1, quiet.AwakeCount);
        }

        [Test]
        public void AwakeZombie_HearsWithBonus()
        {
            _sim.SetAreaWidth(0, 80f);
            var zombie = _sim.AddZombie(_walker, 0, 20f);
            // 걷기 소음 3.2 × 보너스 1.25 = 4 → 3.9유닛 떨어진 소리도 들린다
            _sim.Emit(new NoiseEvent(0, 23.9f, _balance.WalkNoise));
            _sim.Step(0.1f, new PlayerSnapshot { AreaId = 5 }, ActivityMods.Neutral);
            Assert.AreEqual(ZombieState.Investigate, zombie.State);
        }
    }
}
