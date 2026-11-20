using ApoGirl.Core.Definitions;
using ApoGirl.Core.Random;
using ApoGirl.Exploration.Model;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    public sealed class ThreatSimulationTests
    {
        private const int Corridor = 0;
        private const int Room = 1;

        private BalanceConfig _balance;
        private ThreatSimulation _sim;
        private EnemyDef _walker;
        private readonly ActivityMods _neutral = ActivityMods.Neutral;

        [SetUp]
        public void SetUp()
        {
            _balance = new BalanceConfig();
            _sim = new ThreatSimulation(_balance, new SeededRandom(SeededRandom.CreateState(3)));
            _sim.SetAreaWidth(Corridor, 80f);
            _sim.SetAreaWidth(Room, 14f);
            _walker = new EnemyDef { Id = "W", WalkSpeed = 1f, RunSpeed = 3f, SightRange = 7f, AwarenessRate = 1.5f, Damage = 10, HitCooldown = 1f, AttackRange = 0.7f };
        }

        private static PlayerSnapshot Player(int area, float x, bool crouch = false, bool hidden = false)
        {
            return new PlayerSnapshot { AreaId = area, X = x, Crouching = crouch, Hidden = hidden };
        }

        private ThreatEvents Run(float seconds, PlayerSnapshot player, float dt = 0.1f)
        {
            var total = new ThreatEvents();
            for (var t = 0f; t < seconds; t += dt)
            {
                var events = _sim.Step(dt, player, _neutral);
                total.Damage += events.Damage;
                total.Spotted |= events.Spotted;
                total.WokenCrowds.AddRange(events.WokenCrowds);
            }

            return total;
        }

        [Test]
        public void Activity_FollowsClockPhases()
        {
            Assert.AreEqual(ActivityLevel.Dormant, ActivityModel.Evaluate(ClockPhase.Morning, false, _balance).Level);
            Assert.AreEqual(ActivityLevel.Alert, ActivityModel.Evaluate(ClockPhase.Dusk, false, _balance).Level);
            Assert.AreEqual(ActivityLevel.Hunting, ActivityModel.Evaluate(ClockPhase.Midnight, false, _balance).Level);
            var awakened = ActivityModel.Evaluate(ClockPhase.Dusk, true, _balance);
            Assert.AreEqual(ActivityLevel.Hunting, awakened.Level);
            Assert.Greater(awakened.Sight, ActivityModel.Evaluate(ClockPhase.Morning, false, _balance).Sight);
        }

        [Test]
        public void Noise_MakesIdleZombieInvestigate_OnlyWithinHearing()
        {
            var zombie = _sim.AddZombie(_walker, Corridor, 20f);
            _sim.Emit(new NoiseEvent(Corridor, 40f, 7f));
            _sim.Step(0.1f, Player(Room, 1f), _neutral);
            Assert.AreNotEqual(ZombieState.Investigate, zombie.State, "20유닛 밖 소음은 못 듣는다");

            _sim.Emit(new NoiseEvent(Corridor, 25f, 7f));
            _sim.Step(0.1f, Player(Room, 1f), _neutral);
            Assert.AreEqual(ZombieState.Investigate, zombie.State);
            Assert.AreEqual(25f, zombie.TargetX);

            Run(10f, Player(Room, 1f));
            Assert.AreEqual(ZombieState.Search, zombie.State, "소음 위치에 도착하면 수색");
        }

        [Test]
        public void ListenerHearsFarther()
        {
            var listener = _sim.AddZombie(new EnemyDef { HearingMultiplier = 2f, SightRange = 4f }, Corridor, 20f);
            _sim.Emit(new NoiseEvent(Corridor, 32f, 7f));
            _sim.Step(0.1f, Player(Room, 1f), _neutral);
            Assert.AreEqual(ZombieState.Investigate, listener.State);
        }

        [Test]
        public void DormantCrowd_IgnoresSoftNoise_WakesOnRepeatedRunning()
        {
            var crowd = _sim.AddCrowd(Corridor, 40f);
            var a = _sim.AddZombie(_walker, Corridor, 39f, crowd);
            _sim.AddZombie(_walker, Corridor, 41f, crowd);

            _sim.Emit(new NoiseEvent(Corridor, 40f, _balance.CrouchNoise));
            Run(1f, Player(Corridor, 60f));
            Assert.IsFalse(crowd.Awake);
            Assert.AreEqual(ZombieState.Dormant, a.State);

            ThreatEvents events = null;
            for (var i = 0; i < 6 && !crowd.Awake; i++)
            {
                _sim.Emit(new NoiseEvent(Corridor, 42f, _balance.RunNoise));
                events = _sim.Step(0.4f, Player(Corridor, 42f), _neutral);
            }

            Assert.IsTrue(crowd.Awake);
            Assert.Contains(crowd, events.WokenCrowds);
            Assert.AreEqual(ZombieState.Investigate, a.State);
        }

        [Test]
        public void Crowd_StirringDecays_AndLingeringNearbyWakesIt()
        {
            var crowd = _sim.AddCrowd(Corridor, 40f);
            _sim.AddZombie(_walker, Corridor, 40f, crowd);
            crowd.Stirring = 50f;
            Run(5f, Player(Corridor, 70f));
            Assert.AreEqual(50f - 5f * _balance.StirDecayPerSecond, crowd.Stirring, 0.5f);

            Run(60f, Player(Corridor, 42f));
            Assert.IsTrue(crowd.Awake, "근처에 오래 머물면 깨어난다");
        }

        [Test]
        public void WakingCrowd_ShakesNeighbourCrowd()
        {
            var first = _sim.AddCrowd(Corridor, 30f);
            _sim.AddZombie(_walker, Corridor, 30f, first);
            var second = _sim.AddCrowd(Corridor, 36f);
            _sim.AddZombie(_walker, Corridor, 36f, second);
            first.Stirring = _balance.StirThreshold;

            _sim.Step(0.1f, Player(Room, 1f), _neutral);
            Assert.IsTrue(first.Awake);
            Assert.IsFalse(second.Awake);
            _sim.Step(0.1f, Player(Room, 1f), _neutral);
            Assert.Greater(second.Stirring, 30f, "연쇄 각성 소음");
        }

        [Test]
        public void SeeingPlayer_FillsAwareness_ThenChasesAndHits()
        {
            var zombie = _sim.AddZombie(_walker, Corridor, 10f);
            zombie.Facing = 1;
            zombie.State = ZombieState.Investigate;
            zombie.TargetX = 15f;
            var player = Player(Corridor, 15f);
            var events = Run(3f, player);

            Assert.IsTrue(events.Spotted);
            Assert.AreEqual(ZombieState.Chase, zombie.State);
            Assert.Greater(events.Damage, 0, "따라잡아 공격");

            var before = events.Damage;
            events = Run(0.5f, player);
            Assert.LessOrEqual(events.Damage, _walker.Damage, "쿨타임 안에 연타하지 않음");
            Assert.Greater(before, 0);
        }

        [Test]
        public void BehindOrCrouchedOutOfRange_NotSeen()
        {
            var zombie = _sim.AddZombie(_walker, Corridor, 10f);
            zombie.Facing = -1;
            Assert.IsFalse(_sim.CanSee(zombie, Player(Corridor, 14f), _neutral), "등 뒤");
            zombie.Facing = 1;
            Assert.IsTrue(_sim.CanSee(zombie, Player(Corridor, 14f), _neutral));
            Assert.IsFalse(_sim.CanSee(zombie, Player(Corridor, 15f, crouch: true), _neutral), "웅크리면 시야 0.6배");
            Assert.IsFalse(_sim.CanSee(zombie, Player(Corridor, 12f, hidden: true), _neutral));
        }

        [Test]
        public void Hiding_BlockedWhileChasedUpClose()
        {
            var zombie = _sim.AddZombie(_walker, Corridor, 10f);
            zombie.Facing = 1;
            zombie.State = ZombieState.Chase;
            zombie.Awareness = 1f;
            Assert.IsFalse(_sim.CanHide(Player(Corridor, 12f), _neutral));
            Assert.IsTrue(_sim.CanHide(Player(Corridor, 16f), _neutral));
        }

        [Test]
        public void Chase_FollowsThroughDoor_ButNotStairs()
        {
            _sim.AddLink(Corridor, 20f, Room, 1.2f);
            _sim.AddLink(Room, 1.2f, Corridor, 20f);
            var zombie = _sim.AddZombie(_walker, Corridor, 14f);
            zombie.State = ZombieState.Chase;
            zombie.Awareness = 1f;
            zombie.Facing = 1;
            Run(0.5f, Player(Corridor, 20f));

            // 플레이어가 문으로 들어감
            Run(4f, Player(Room, 6f));
            Assert.AreEqual(Room, zombie.AreaId, "문으로 따라 들어온다");

            // 계단으로 올라간 경우 (연결 없음)
            var other = _sim.AddZombie(_walker, Corridor, 50f);
            other.State = ZombieState.Chase;
            other.Awareness = 1f;
            other.TargetArea = Corridor;
            other.TargetX = 55f;
            Run(4f, Player(5, 55f));
            Assert.AreEqual(Corridor, other.AreaId);
            Assert.AreEqual(ZombieState.Search, other.State);
        }

        [Test]
        public void Search_ThenReturn_ThenIdle()
        {
            var zombie = _sim.AddZombie(_walker, Corridor, 10f);
            zombie.State = ZombieState.Search;
            zombie.Timer = 1f;
            Run(1.2f, Player(Room, 1f));
            Assert.AreEqual(ZombieState.Return, zombie.State);
            Run(30f, Player(Room, 1f));
            Assert.AreEqual(ZombieState.Idle, zombie.State);
        }

        /// <summary>좀비가 주인공을 쫓다가(40 → 50 쪽) 놓치게 만든다: 발각될 때까지 보여 주고, 다른 공간으로 사라진다.</summary>
        private ZombieUnit ChaseThenLose(ActivityMods mods)
        {
            var zombie = _sim.AddZombie(_walker, Corridor, 40f);
            zombie.Facing = 1;
            for (var t = 0f; t < 3f && zombie.State != ZombieState.Chase; t += 0.1f)
            {
                _sim.Step(0.1f, Player(Corridor, 44f), mods);
            }

            Assert.AreEqual(ZombieState.Chase, zombie.State);
            for (var t = 0f; t < 10f && zombie.State == ZombieState.Chase; t += 0.1f)
            {
                _sim.Step(0.1f, Player(Room, 1f, hidden: true), mods);
            }

            Assert.AreEqual(ZombieState.Search, zombie.State, "놓치면 그 근처를 서성인다");
            Assert.IsTrue(zombie.LostChase);
            return zombie;
        }

        [Test]
        public void LostChase_Day_LingersThenBacksOffALittleAndSleepsAgain()
        {
            var day = ActivityModel.Evaluate(ClockPhase.Morning, false, _balance);
            var zombie = ChaseThenLose(day);
            var lostX = zombie.X;
            for (var t = 0f; t < 60f && zombie.State != ZombieState.Dormant; t += 0.1f)
            {
                Assert.LessOrEqual(System.Math.Abs(zombie.X - lostX), _balance.LostRetreatDay + 2.5f, "낮에는 조금만 물러난다");
                _sim.Step(0.1f, Player(Room, 1f, hidden: true), day);
            }

            Assert.AreEqual(ZombieState.Dormant, zombie.State, "낮에는 물러난 자리에서 다시 잠든다");
            Assert.GreaterOrEqual(System.Math.Abs(zombie.X - lostX), _balance.LostRetreatDay - 2.5f);
            Assert.IsFalse(_sim.Crowds[zombie.CrowdId].Awake, "다시 잠든 좀비는 잠든 군집에 속한다 (소음으로 다시 깰 수 있다)");
        }

        [Test]
        public void LostChase_Night_BacksOffFarAndStaysAwake()
        {
            var night = ActivityModel.Evaluate(ClockPhase.Midnight, false, _balance);
            var zombie = ChaseThenLose(night);
            var lostX = zombie.X;
            for (var t = 0f; t < 120f && zombie.State != ZombieState.Idle; t += 0.1f)
            {
                _sim.Step(0.1f, Player(Room, 1f, hidden: true), night);
            }

            Assert.AreEqual(ZombieState.Idle, zombie.State, "밤에는 깨어 있는 채로 배회한다");
            Assert.GreaterOrEqual(System.Math.Abs(zombie.X - lostX), _balance.LostRetreatNight - 2.5f, "밤에는 충분히 멀어진다");
        }

        [Test]
        public void Shove_StunsAndPushes_IgnoresDormant()
        {
            var crowd = _sim.AddCrowd(Corridor, 11f);
            var dormant = _sim.AddZombie(_walker, Corridor, 11f, crowd);
            Assert.IsNull(_sim.Shove(Player(Corridor, 10f), 1), "잠든 좀비는 밀치지 않는다");

            var zombie = _sim.AddZombie(_walker, Corridor, 11f);
            var shoved = _sim.Shove(Player(Corridor, 10f), 1);
            Assert.AreSame(zombie, shoved);
            Assert.AreEqual(ZombieState.Stunned, zombie.State);
            Assert.AreEqual(11f + _balance.ShovePush, zombie.X, 1e-4f);
            Assert.IsNull(_sim.Shove(Player(Corridor, 10f), -1), "반대 방향");

            Run(_balance.ShoveStunSeconds + 0.2f, Player(Room, 1f));
            Assert.AreNotEqual(ZombieState.Stunned, zombie.State);
            Assert.AreEqual(ZombieState.Dormant, dormant.State);
        }

        [Test]
        public void AwakenAll_WakesEveryCrowd()
        {
            var crowd = _sim.AddCrowd(Corridor, 40f);
            var member = _sim.AddZombie(_walker, Corridor, 40f, crowd);
            _sim.AwakenAll();
            Assert.IsTrue(_sim.Awakened);
            Assert.IsTrue(crowd.Awake);
            Assert.AreEqual(ZombieState.Idle, member.State);
        }
    }
}
