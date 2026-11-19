using System;
using System.Collections.Generic;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Random;
using ApoGirl.Core.Rules;

namespace ApoGirl.Exploration.Model
{
    /// <summary>좀비 활동성 단계 (문서 6.3.2-(2)): 휴면 → 선잠 → 경계 → 사냥.</summary>
    public enum ActivityLevel
    {
        Dormant,
        Stirring,
        Alert,
        Hunting,
    }

    /// <summary>현재 시간대의 감지·이동 배율.</summary>
    public readonly struct ActivityMods
    {
        public ActivityMods(ActivityLevel level, float sight, float hearing, float speed, float stir)
        {
            Level = level;
            Sight = sight;
            Hearing = hearing;
            Speed = speed;
            Stir = stir;
        }

        public ActivityLevel Level { get; }
        public float Sight { get; }
        public float Hearing { get; }
        public float Speed { get; }

        /// <summary>휴면 군집 각성 게이지 증가 배율.</summary>
        public float Stir { get; }

        public static ActivityMods Neutral => new ActivityMods(ActivityLevel.Alert, 1f, 1f, 1f, 1f);
    }

    public static class ActivityModel
    {
        /// <param name="awakened">낮 시계가 끝난 뒤 남아 있음 (황혼 각성, 문서 6.3.2-(3)).</param>
        public static ActivityMods Evaluate(ClockPhase phase, bool awakened, BalanceConfig balance)
        {
            var i = (int)phase;
            var stir = StatRules.StageValue(balance.ActivityStir, i);
            if (awakened)
            {
                return new ActivityMods(ActivityLevel.Hunting, balance.AwakenedSight, balance.AwakenedHearing, balance.AwakenedSpeed, stir);
            }

            ActivityLevel level;
            switch (phase)
            {
                case ClockPhase.Morning: level = ActivityLevel.Dormant; break;
                case ClockPhase.Afternoon: level = ActivityLevel.Stirring; break;
                case ClockPhase.Dusk: level = ActivityLevel.Alert; break;
                case ClockPhase.Evening: level = ActivityLevel.Alert; break;
                case ClockPhase.Midnight: level = ActivityLevel.Hunting; break;
                default: level = ActivityLevel.Stirring; break;
            }

            return new ActivityMods(
                level,
                StatRules.StageValue(balance.ActivitySight, i),
                StatRules.StageValue(balance.ActivityHearing, i),
                StatRules.StageValue(balance.ActivitySpeed, i),
                stir);
        }
    }

    /// <summary>소음 하나 (문서 11.2.1 NoiseEvent): 공간·위치·반경.</summary>
    public sealed class NoiseEvent
    {
        public NoiseEvent(int areaId, float x, float radius)
        {
            AreaId = areaId;
            X = x;
            Radius = radius;
        }

        public int AreaId { get; }
        public float X { get; }
        public float Radius { get; }
    }

    /// <summary>공간 연결 (문 ↔ 방 출입문). 좀비가 추격할 때 따라 들어온다. 계단은 연결하지 않는다 (계단 도주 가능).</summary>
    public sealed class AreaLink
    {
        public int FromArea;
        public float FromX;
        public int ToArea;
        public float ToX;
    }

    /// <summary>플레이어 상태 스냅샷 (매 틱 입력).</summary>
    public sealed class PlayerSnapshot
    {
        public int AreaId;
        public float X;
        public bool Crouching;
        public bool Hidden;

        /// <summary>좀비 눈에 띄는 정도 (어둠 0.6, 손전등 1.5 등). 시야 거리에 곱한다.</summary>
        public float Visibility = 1f;
    }

    public enum ZombieState
    {
        Dormant,
        Idle,
        Investigate,
        Chase,
        Search,
        Return,
        Stunned,
    }

    public sealed class ZombieUnit
    {
        public int Id;
        public EnemyDef Def;
        public int AreaId;
        public float X;
        public ZombieState State;

        /// <summary>0~1. 1이 되면 발각 (의심 → 경계 → 발각).</summary>
        public float Awareness;

        public int TargetArea;
        public float TargetX;
        public float Timer;
        public float StunTimer;
        public float HitTimer;
        public int Facing = 1;
        public int CrowdId = -1;
        public int HomeArea;
        public float HomeX;

        public bool IsAwake => State != ZombieState.Dormant;
    }

    /// <summary>휴면 군집 (문서 6.3.2-(3) Crowd Site): 각성 게이지를 공유하고 임계에서 함께 깨어난다.</summary>
    public sealed class CrowdState
    {
        public int Id;
        public int AreaId;
        public float X;
        public float Stirring;
        public bool Awake;
        public readonly List<ZombieUnit> Members = new List<ZombieUnit>();
    }

    /// <summary>귀 기울이기 결과.</summary>
    public sealed class ListenResult
    {
        public int AwakeCount;

        /// <summary>깨어 있는 좀비까지 가장 가까운 거리 (없으면 float.MaxValue).</summary>
        public float NearestAwake;

        public int SleepingNearby;
    }

    public sealed class ThreatEvents
    {
        /// <summary>이번 틱에 플레이어가 받은 피해.</summary>
        public int Damage;

        /// <summary>새로 발각됨 (어느 좀비든 추격 시작).</summary>
        public bool Spotted;

        public readonly List<CrowdState> WokenCrowds = new List<CrowdState>();
    }

    /// <summary>
    /// 탐험 위협 시뮬레이션 (UnityEngine 비참조, 시드 난수). 공간은 정수 ID, 위치는 공간 내 X.
    /// 처리 순서: 소음 → 군집 게이지 → 좀비 상태 기계 (문서 11.2.1 AI: 배회 → 조사 → 추격 → 수색 → 복귀).
    /// </summary>
    public sealed class ThreatSimulation
    {
        private const float ArriveDistance = 0.1f;
        private const float EdgeMargin = 0.3f;
        private const float TouchDistance = 1f;

        private readonly BalanceConfig _balance;
        private readonly IRandomService _random;
        private readonly List<ZombieUnit> _zombies = new List<ZombieUnit>();
        private readonly List<CrowdState> _crowds = new List<CrowdState>();
        private readonly List<AreaLink> _links = new List<AreaLink>();
        private readonly Dictionary<int, float> _areaWidths = new Dictionary<int, float>();
        private List<NoiseEvent> _pendingNoise = new List<NoiseEvent>();

        public ThreatSimulation(BalanceConfig balance, IRandomService random)
        {
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public IReadOnlyList<ZombieUnit> Zombies => _zombies;

        public IReadOnlyList<CrowdState> Crowds => _crowds;

        /// <summary>황혼 각성이 일어났다.</summary>
        public bool Awakened { get; private set; }

        // ── 구성 ──

        public void SetAreaWidth(int areaId, float width) => _areaWidths[areaId] = width;

        public void AddLink(int fromArea, float fromX, int toArea, float toX)
        {
            _links.Add(new AreaLink { FromArea = fromArea, FromX = fromX, ToArea = toArea, ToX = toX });
        }

        public CrowdState AddCrowd(int areaId, float x)
        {
            var crowd = new CrowdState { Id = _crowds.Count, AreaId = areaId, X = x };
            _crowds.Add(crowd);
            return crowd;
        }

        /// <summary>군집을 주면 휴면 상태로, 아니면 깨어 있는 배회자로 추가.</summary>
        public ZombieUnit AddZombie(EnemyDef def, int areaId, float x, CrowdState crowd = null)
        {
            var zombie = new ZombieUnit
            {
                Id = _zombies.Count,
                Def = def,
                AreaId = areaId,
                X = Clamp(areaId, x),
                State = crowd != null ? ZombieState.Dormant : ZombieState.Idle,
                HomeArea = areaId,
                HomeX = x,
                TargetArea = areaId,
                TargetX = x,
                Facing = _random.NextDouble() < 0.5 ? -1 : 1,
                CrowdId = crowd?.Id ?? -1,
            };
            _zombies.Add(zombie);
            crowd?.Members.Add(zombie);
            return zombie;
        }

        // ── 입력 ──

        public void Emit(NoiseEvent noise) => _pendingNoise.Add(noise);

        /// <summary>낮 시계가 끝난 뒤 체류: 모든 군집이 깨어나 배회한다 (문서 6.3.2-(3) 황혼 각성).</summary>
        public void AwakenAll()
        {
            Awakened = true;
            foreach (var crowd in _crowds)
            {
                crowd.Awake = true;
                crowd.Stirring = _balance.StirThreshold;
            }

            foreach (var zombie in _zombies)
            {
                if (zombie.State == ZombieState.Dormant)
                {
                    zombie.State = ZombieState.Idle;
                    zombie.Timer = 0f;
                }
            }
        }

        /// <summary>추격 중인 좀비가 가까이서 보고 있으면 숨을 수 없다.</summary>
        public bool CanHide(PlayerSnapshot player, ActivityMods mods)
        {
            foreach (var zombie in _zombies)
            {
                if (zombie.State == ZombieState.Chase && zombie.AreaId == player.AreaId
                    && Math.Abs(zombie.X - player.X) <= _balance.HideBlockDistance && CanSee(zombie, player, mods))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>바라보는 방향의 가장 가까운 깨어 있는 좀비를 밀친다 (기절 + 밀려남). 소음은 호출 측이 낸다.</summary>
        public ZombieUnit Shove(PlayerSnapshot player, int facing)
        {
            ZombieUnit best = null;
            var bestDistance = _balance.ShoveRange;
            foreach (var zombie in _zombies)
            {
                if (!zombie.IsAwake || zombie.AreaId != player.AreaId)
                {
                    continue;
                }

                var dx = zombie.X - player.X;
                if (Math.Sign(dx) != facing && Math.Abs(dx) > 0.2f)
                {
                    continue;
                }

                if (Math.Abs(dx) <= bestDistance)
                {
                    best = zombie;
                    bestDistance = Math.Abs(dx);
                }
            }

            if (best != null)
            {
                best.State = ZombieState.Stunned;
                best.StunTimer = _balance.ShoveStunSeconds;
                best.X = Clamp(best.AreaId, best.X + facing * _balance.ShovePush);
                best.Facing = -facing;
            }

            return best;
        }

        /// <summary>
        /// 문틈으로 살피는 중: 그 공간의 문 위치에 희미하게 노출된 것으로 보고, 보고 있는 좀비의 인지 게이지가 오른다.
        /// 다 차면 문 쪽으로 조사하러 온다. 반환: 이번 틱에 눈치챈 좀비가 있는가.
        /// </summary>
        public bool ExposeAt(int areaId, float x, float dt, ActivityMods mods)
        {
            var peeker = new PlayerSnapshot { AreaId = areaId, X = x, Crouching = true, Visibility = _balance.PeekVisibility };
            var noticed = false;
            foreach (var zombie in _zombies)
            {
                if (!zombie.IsAwake || zombie.AreaId != areaId || zombie.State == ZombieState.Chase || !CanSee(zombie, peeker, mods))
                {
                    continue;
                }

                zombie.Awareness = Math.Min(1f, zombie.Awareness + zombie.Def.AwarenessRate * dt);
                if (zombie.Awareness >= 1f && zombie.State != ZombieState.Investigate)
                {
                    zombie.State = ZombieState.Investigate;
                    zombie.TargetArea = areaId;
                    zombie.TargetX = x;
                    noticed = true;
                }
            }

            return noticed;
        }

        /// <summary>문에 귀 기울이기: 문 너머 공간의 소리 정보.</summary>
        public ListenResult Listen(int areaId, float x)
        {
            var result = new ListenResult { NearestAwake = float.MaxValue };
            foreach (var zombie in _zombies)
            {
                if (zombie.AreaId != areaId)
                {
                    continue;
                }

                var d = Math.Abs(zombie.X - x);
                if (zombie.IsAwake)
                {
                    result.AwakeCount++;
                    result.NearestAwake = Math.Min(result.NearestAwake, d);
                }
                else if (d <= _balance.ListenBreathRange)
                {
                    result.SleepingNearby++;
                }
            }

            return result;
        }

        /// <summary>HUD용: 같은 공간에서 가장 가까운 잠든 군집.</summary>
        public CrowdState NearestDormantCrowd(PlayerSnapshot player, float maxDistance)
        {
            CrowdState best = null;
            var bestDistance = maxDistance;
            foreach (var crowd in _crowds)
            {
                if (crowd.Awake || crowd.AreaId != player.AreaId)
                {
                    continue;
                }

                var d = Math.Abs(crowd.X - player.X);
                if (d <= bestDistance)
                {
                    best = crowd;
                    bestDistance = d;
                }
            }

            return best;
        }

        // ── 틱 ──

        public ThreatEvents Step(float dt, PlayerSnapshot player, ActivityMods mods)
        {
            var events = new ThreatEvents();
            var noises = _pendingNoise;
            _pendingNoise = new List<NoiseEvent>();
            foreach (var noise in noises)
            {
                Hear(noise, mods);
            }

            foreach (var crowd in _crowds)
            {
                if (crowd.Awake)
                {
                    continue;
                }

                if (!player.Hidden && player.AreaId == crowd.AreaId && Math.Abs(player.X - crowd.X) <= _balance.PresenceRange)
                {
                    crowd.Stirring += _balance.PresenceStirPerSecond * mods.Stir * dt * (player.Crouching ? 0.5f : 1f);
                }

                // 임계 도달을 먼저 판정하고, 아니면 서서히 가라앉는다
                if (crowd.Stirring >= _balance.StirThreshold)
                {
                    var toward = player.AreaId == crowd.AreaId ? player.X : crowd.X;
                    WakeCrowd(crowd, toward, events);
                    continue;
                }

                crowd.Stirring = Math.Max(0f, crowd.Stirring - _balance.StirDecayPerSecond * dt);
            }

            foreach (var zombie in _zombies)
            {
                if (zombie.IsAwake)
                {
                    TickZombie(zombie, dt, player, mods, events);
                }
            }

            return events;
        }

        public bool CanSee(ZombieUnit zombie, PlayerSnapshot player, ActivityMods mods)
        {
            if (player.Hidden || player.AreaId != zombie.AreaId || zombie.State == ZombieState.Stunned)
            {
                return false;
            }

            var dx = player.X - zombie.X;
            var distance = Math.Abs(dx);
            var range = zombie.Def.SightRange * mods.Sight * player.Visibility
                        * (player.Crouching ? _balance.CrouchSightMultiplier : 1f);
            if (distance > range)
            {
                return false;
            }

            return distance <= TouchDistance || Math.Sign(dx) == zombie.Facing;
        }

        private void Hear(NoiseEvent noise, ActivityMods mods)
        {
            foreach (var crowd in _crowds)
            {
                if (crowd.Awake || crowd.AreaId != noise.AreaId)
                {
                    continue;
                }

                var reach = noise.Radius * mods.Hearing;
                var d = Math.Abs(crowd.X - noise.X);
                if (d <= reach)
                {
                    var falloff = 1f - 0.5f * d / Math.Max(0.01f, reach);
                    crowd.Stirring += noise.Radius * _balance.StirPerNoiseUnit * mods.Stir * falloff;
                }
            }

            foreach (var zombie in _zombies)
            {
                if (!zombie.IsAwake || zombie.AreaId != noise.AreaId
                    || zombie.State == ZombieState.Chase || zombie.State == ZombieState.Stunned)
                {
                    continue;
                }

                var reach = noise.Radius * mods.Hearing * zombie.Def.HearingMultiplier * _balance.ZombieHearingBonus;
                if (Math.Abs(zombie.X - noise.X) <= reach)
                {
                    zombie.State = ZombieState.Investigate;
                    zombie.TargetArea = noise.AreaId;
                    zombie.TargetX = noise.X;
                }
            }
        }

        private void WakeCrowd(CrowdState crowd, float towardX, ThreatEvents events)
        {
            crowd.Awake = true;
            crowd.Stirring = _balance.StirThreshold;
            foreach (var member in crowd.Members)
            {
                if (member.State == ZombieState.Dormant)
                {
                    member.State = ZombieState.Investigate;
                    member.TargetArea = crowd.AreaId;
                    member.TargetX = towardX;
                }
            }

            events.WokenCrowds.Add(crowd);

            // 연쇄 각성: 깨어나는 소리가 주변 군집을 흔든다 (다음 틱에 처리)
            _pendingNoise.Add(new NoiseEvent(crowd.AreaId, crowd.X, _balance.ChainWakeRadius));
        }

        private void TickZombie(ZombieUnit zombie, float dt, PlayerSnapshot player, ActivityMods mods, ThreatEvents events)
        {
            if (zombie.HitTimer > 0f)
            {
                zombie.HitTimer -= dt;
            }

            if (zombie.State == ZombieState.Stunned)
            {
                zombie.StunTimer -= dt;
                if (zombie.StunTimer <= 0f)
                {
                    zombie.State = ZombieState.Investigate;
                    zombie.TargetArea = zombie.AreaId;
                    zombie.TargetX = player.AreaId == zombie.AreaId ? player.X : zombie.X;
                }

                return;
            }

            var sees = CanSee(zombie, player, mods);
            if (sees)
            {
                var distance = Math.Abs(player.X - zombie.X);
                var range = Math.Max(0.1f, zombie.Def.SightRange * mods.Sight);
                var closeness = 1f + Math.Max(0f, 1f - distance / range);
                zombie.Awareness = Math.Min(1f, zombie.Awareness + zombie.Def.AwarenessRate * closeness * dt);
            }
            else if (zombie.State != ZombieState.Chase)
            {
                zombie.Awareness = Math.Max(0f, zombie.Awareness - _balance.AwarenessDecayPerSecond * dt);
            }

            if (sees && zombie.Awareness >= 1f && zombie.State != ZombieState.Chase)
            {
                zombie.State = ZombieState.Chase;
                events.Spotted = true;
            }

            switch (zombie.State)
            {
                case ZombieState.Idle:
                    zombie.Timer -= dt;
                    if (zombie.Timer <= 0f || Math.Abs(zombie.X - zombie.TargetX) <= ArriveDistance)
                    {
                        zombie.TargetX = Clamp(zombie.AreaId, zombie.HomeX + (float)(_random.NextDouble() * 2 - 1) * _balance.PatrolRange);
                        zombie.Timer = 3f + (float)_random.NextDouble() * 4f;
                    }

                    MoveToward(zombie, zombie.TargetX, zombie.Def.WalkSpeed * 0.5f * mods.Speed, dt);
                    break;

                case ZombieState.Investigate:
                    if (MoveToward(zombie, zombie.TargetX, zombie.Def.WalkSpeed * mods.Speed, dt))
                    {
                        zombie.State = ZombieState.Search;
                        zombie.Timer = _balance.SearchDuration;
                    }

                    break;

                case ZombieState.Chase:
                    TickChase(zombie, dt, player, mods, events, sees);
                    break;

                case ZombieState.Search:
                    zombie.Timer -= dt;
                    if (Math.Abs(zombie.X - zombie.TargetX) <= ArriveDistance)
                    {
                        zombie.TargetX = Clamp(zombie.AreaId, zombie.X + (float)(_random.NextDouble() * 2 - 1) * 2f);
                    }

                    MoveToward(zombie, zombie.TargetX, zombie.Def.WalkSpeed * 0.6f * mods.Speed, dt);
                    if (zombie.Timer <= 0f)
                    {
                        zombie.State = ZombieState.Return;
                        if (zombie.HomeArea != zombie.AreaId)
                        {
                            // 다른 공간까지 따라왔으면 그곳을 새 배회 위치로 삼는다
                            zombie.HomeArea = zombie.AreaId;
                            zombie.HomeX = zombie.X;
                        }
                    }

                    break;

                case ZombieState.Return:
                    if (MoveToward(zombie, zombie.HomeX, zombie.Def.WalkSpeed * 0.5f * mods.Speed, dt))
                    {
                        zombie.State = ZombieState.Idle;
                        zombie.Timer = 0f;
                    }

                    break;
            }
        }

        private void TickChase(ZombieUnit zombie, float dt, PlayerSnapshot player, ActivityMods mods, ThreatEvents events, bool sees)
        {
            if (sees)
            {
                zombie.TargetArea = player.AreaId;
                zombie.TargetX = player.X;
            }

            var arrived = MoveToward(zombie, zombie.TargetX, zombie.Def.RunSpeed * mods.Speed, dt);

            if (sees && Math.Abs(player.X - zombie.X) <= zombie.Def.AttackRange && zombie.HitTimer <= 0f)
            {
                events.Damage += zombie.Def.Damage;
                zombie.HitTimer = zombie.Def.HitCooldown;
                return;
            }

            if (sees || !arrived)
            {
                return;
            }

            // 마지막으로 본 곳에 도착했는데 안 보인다: 플레이어가 그 문으로 들어갔으면 따라간다
            foreach (var link in _links)
            {
                if (link.FromArea == zombie.AreaId && link.ToArea == player.AreaId && Math.Abs(link.FromX - zombie.X) <= 1f && !player.Hidden)
                {
                    zombie.AreaId = link.ToArea;
                    zombie.X = Clamp(link.ToArea, link.ToX);
                    zombie.TargetArea = player.AreaId;
                    zombie.TargetX = player.X;
                    return;
                }
            }

            zombie.State = ZombieState.Search;
            zombie.Timer = _balance.SearchDuration;
            zombie.Awareness = 0.5f;
        }

        /// <returns>목표에 도착했는가.</returns>
        private bool MoveToward(ZombieUnit zombie, float targetX, float speed, float dt)
        {
            var dx = targetX - zombie.X;
            if (Math.Abs(dx) <= ArriveDistance)
            {
                return true;
            }

            var step = Math.Min(Math.Abs(dx), speed * dt);
            zombie.Facing = Math.Sign(dx);
            zombie.X = Clamp(zombie.AreaId, zombie.X + zombie.Facing * step);
            return Math.Abs(targetX - zombie.X) <= ArriveDistance;
        }

        private float Clamp(int areaId, float x)
        {
            if (!_areaWidths.TryGetValue(areaId, out var width))
            {
                return x;
            }

            return Math.Min(Math.Max(x, EdgeMargin), width - EdgeMargin);
        }
    }
}
