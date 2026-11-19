using System;
using System.Collections.Generic;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Random;
using ApoGirl.Core.State;
using ApoGirl.Data;
using ApoGirl.Exploration.Model;
using ApoGirl.Presentation.App;
using ApoGirl.Presentation.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace ApoGirl.Exploration.Runtime
{
    /// <summary>
    /// 탐험 씬 진입점: 요청을 받아 건물(층별 복도 + 문 + 방 템플릿)을 만들고,
    /// 이동·계단·방 출입·수색·배낭·시계·탈출을 돌린 뒤 결과를 허브에 보고한다.
    /// 허브를 거치지 않고 씬을 단독 실행하면 디버그 요청으로 시작한다.
    /// </summary>
    public sealed class ExpeditionDirector : MonoBehaviour
    {
        private const float PlayerWidth = 0.6f;
        private const float PlayerHeight = 1.7f;
        private const float MinOrthographicSize = 5f;
        private const float MaxOrthographicSize = 8f;
        private const float MinVisibleWidth = 26f;
        private const float RoomSpacing = 30f;
        private const float EdgeMargin = 0.3f;
        private const float MoveNoiseInterval = 0.35f;
        private const float SearchNoiseInterval = 0.5f;

        [Tooltip("허브 없이 단독 실행할 때 쓸 정의 DB")]
        [SerializeField] private DefinitionDatabaseAsset _fallbackDatabase;

        [SerializeField] private PanelSettings _panelSettings;

        [Tooltip("탐험 HUD 레이아웃 (Assets/_Project/UI/Expedition.uxml)")]
        [SerializeField] private VisualTreeAsset _hudLayout;

        [Tooltip("단독 실행 시 지역")]
        [SerializeField] private string _debugLocationId = "LOC_SCHOOL_INTERIOR";

        [Tooltip("단독 실행 시 시간대")]
        [SerializeField] private ExpeditionTime _debugTimeOfDay = ExpeditionTime.Day;

        private readonly List<ExplorationArea> _corridors = new List<ExplorationArea>();
        private readonly List<ExplorationArea> _rooms = new List<ExplorationArea>();
        private readonly List<ExplorationTarget> _targets = new List<ExplorationTarget>();
        private IDefinitionDatabase _db;
        private BalanceConfig _balance;
        private BuildingView _building;
        private Transform _world;
        private Transform _player;
        private SpriteRenderer _playerRenderer;
        private Camera _camera;
        private ExpeditionHud _hud;
        private bool _standalone;
        private int _baseFatigue;
        private int _runStress;
        private bool _facingRight = true;
        private readonly List<ExplorationArea> _areasById = new List<ExplorationArea>();
        private readonly List<ZombieView> _zombieViews = new List<ZombieView>();
        private ThreatSimulation _threat;
        private Light2D _light;
        private int _startHp;
        private float _moveNoiseTimer;
        private float _searchNoiseTimer;
        private float _shoveTimer;
        private bool _crouching;

        public ExpeditionRequest Request { get; private set; }

        public LocationDef Location { get; private set; }

        public RunClock Clock { get; private set; }

        public BackpackGrid Backpack { get; private set; }

        public float CarryCapacity { get; private set; }

        /// <summary>현재 서 있는 공간 (복도 또는 방).</summary>
        public ExplorationArea CurrentArea { get; private set; }

        /// <summary>현재 공간 기준 X.</summary>
        public float X { get; private set; }

        public int Floor => CurrentArea.Floor;

        public bool InRoom => CurrentArea.IsRoom;

        public ExplorationTarget Nearest { get; private set; }

        public ExplorationTarget OpenContainer { get; private set; }

        /// <summary>디버그: 시간 배속 (F2).</summary>
        public float TimeScale { get; private set; } = 1f;

        public bool IsFinished { get; private set; }

        public ExpeditionResult Result { get; private set; }

        public IDefinitionDatabase Definitions => _db;

        /// <summary>지금까지 만들어진 모든 상호작용 대상 (방 내부는 처음 들어갈 때 추가).</summary>
        public IReadOnlyList<ExplorationTarget> Targets => _targets;

        public IReadOnlyList<ExplorationArea> Rooms => _rooms;

        public bool IsOverweight => Backpack.TotalWeight > CarryCapacity;

        /// <summary>탐험 중 현재 HP (0이 되면 쓰러짐).</summary>
        public int Hp { get; private set; }

        /// <summary>사물함·책상 밑에 숨어 있음.</summary>
        public bool Hidden { get; private set; }

        /// <summary>낮 시계가 끝나 황혼 각성이 일어남 (이후 탈출은 시간 초과 귀환).</summary>
        public bool DuskAwakened { get; private set; }

        public ActivityMods Activity { get; private set; } = ActivityMods.Neutral;

        /// <summary>HUD 소음 표시: 최근 낸 소음 반경 (서서히 줄어듦).</summary>
        public float NoiseLevel { get; private set; }

        public ThreatSimulation Threat => _threat;

        public PlayerSnapshot Snapshot => new PlayerSnapshot { AreaId = AreaId(CurrentArea), X = X, Crouching = _crouching, Hidden = Hidden };

        public int AreaId(ExplorationArea area) => _areasById.IndexOf(area);

        /// <summary>탐험 카메라 (HUD가 월드 위치를 화면 좌표로 바꿀 때 사용).</summary>
        public Camera ViewCamera => _camera;

        /// <summary>좀비 머리 위 월드 좌표 (Zzz·표시용).</summary>
        public Vector3 HeadPosition(ZombieUnit unit)
        {
            var area = _areasById[unit.AreaId];
            var origin = _world.TransformPoint(area.Origin);
            return new Vector3(origin.x + unit.X, origin.y + 2.1f, 0f);
        }

        /// <summary>HUD 위치 표시: "2층 복도 · 2-3 교실 앞" / "2-3 교실".</summary>
        public string LocationLabel
        {
            get
            {
                if (InRoom)
                {
                    return CurrentArea.Label;
                }

                var label = Loc.F("exp.corridor", Floor + 1);
                if (Nearest != null && Nearest.Kind == TargetKind.Door)
                {
                    label += " · " + Loc.F("exp.near_door", Nearest.Room.Label);
                }

                return label;
            }
        }

        private void Start()
        {
            var session = GameApp.EnsureSession(_fallbackDatabase);
            _db = session.Definitions;
            _balance = _db.Balance;
            Request = GameApp.ActiveExpedition ?? CreateDebugRequest(session);
            _standalone = GameApp.ActiveExpedition == null;

            Location = _db.GetLocation(Request.LocationId);
            var day = Request.TimeOfDay == ExpeditionTime.Day;
            if (_db.TryGetExpeditionModel(Request.LocationId, out var model))
            {
                var timeYield = day ? model.Day : model.Night;
                _baseFatigue = timeYield.Fatigue;
                _runStress = timeYield.StressMin;
            }
            else
            {
                _baseFatigue = day ? 15 : 20;
                _runStress = day ? 6 : 15;
            }

            Clock = new RunClock(day ? Location.DayRunSeconds : Location.NightRunSeconds, Request.TimeOfDay);
            Backpack = new BackpackGrid(_balance.BackpackWidth, _balance.BackpackHeight);
            CarryCapacity = ExplorationRules.CarryCapacity(Request.Stats, _balance);

            BuildWorld();
            Hp = _startHp = Math.Max(1, Request.Hp);
            BuildThreats(day ? Location.DayVariant : Location.NightVariant);
            SetupCamera();
            SetupLight(day);
            EnsureEventSystem();

            var document = gameObject.AddComponent<UIDocument>();
            document.panelSettings = _panelSettings;
            document.sortingOrder = 10;
            document.visualTreeAsset = _hudLayout;
            _hud = new ExpeditionHud(document.rootVisualElement, this);
            UpdateZombieViews();
        }

        private void Update()
        {
            if (IsFinished || _hud == null)
            {
                return;
            }

            var dt = Time.deltaTime;
            Clock.Tick(dt * TimeScale);
            if (Clock.IsOver && !DuskAwakened)
            {
                if (Request.TimeOfDay == ExpeditionTime.Day)
                {
                    TriggerDuskAwakening();
                }
                else
                {
                    // 밤 시계 종료(새벽) 처리는 M2b-3
                    Finish(ExtractStatus.Stranded);
                    return;
                }
            }

            HandleInput(dt);
            StepThreats(dt);
            if (IsFinished)
            {
                return;
            }

            UpdateViews(dt);
            UpdateZombieViews();
            _hud.Refresh();
        }

        // ── 공개 조작 (입력·HUD·테스트에서 호출) ──

        /// <summary>계단·문·방 출입문·탈출구를 사용한다 (거리 검사는 호출 측 책임). 컨테이너는 수색이 끝난 경우 연다.</summary>
        public void UseTarget(ExplorationTarget target)
        {
            if (target == null || IsFinished)
            {
                return;
            }

            switch (target.Kind)
            {
                case TargetKind.Connector:
                    EmitNoise(CurrentArea, X, _balance.StairsNoise);
                    MoveTo(_corridors[target.Connector.TargetFloor], target.X);
                    EmitNoise(CurrentArea, X, _balance.StairsNoise);
                    break;
                case TargetKind.Door:
                    EnsureRoomBuilt(target.Room);
                    EmitNoise(CurrentArea, X, _balance.DoorNoise);
                    MoveTo(target.Room, target.Room.Template.ExitX);
                    EmitNoise(CurrentArea, X, _balance.DoorNoise);
                    if (!target.Room.Visited)
                    {
                        target.Room.Visited = true;
                        target.Tint(BuildingView.VisitedDoorColor);
                    }

                    break;
                case TargetKind.RoomExit:
                    EmitNoise(CurrentArea, X, _balance.DoorNoise);
                    MoveTo(_corridors[target.Room.Floor], target.Room.Door.X);
                    EmitNoise(CurrentArea, X, _balance.DoorNoise);
                    break;
                case TargetKind.Exit:
                    if (target.Locked)
                    {
                        _hud?.ShowMessage("exp.exit_locked");
                    }
                    else
                    {
                        ExtractNow();
                    }

                    break;
                case TargetKind.Container:
                    Open(target);
                    break;
            }
        }

        /// <summary>이미 수색한 컨테이너를 연다.</summary>
        public void Open(ExplorationTarget container)
        {
            if (container != null && container.Kind == TargetKind.Container && container.Searched)
            {
                OpenContainer = container;
            }
        }

        public void CloseContainer()
        {
            OpenContainer = null;
        }

        public bool TakeItem(int contentIndex)
        {
            var container = OpenContainer;
            if (container == null || contentIndex < 0 || contentIndex >= container.Contents.Count)
            {
                return false;
            }

            var item = _db.GetItem(container.Contents[contentIndex].ItemId);
            if (Backpack.TryAutoPlace(item.Id, item.Width, item.Height, item.Weight) == null)
            {
                _hud?.ShowMessage("exp.backpack_full");
                return false;
            }

            container.Contents.RemoveAt(contentIndex);
            return true;
        }

        public void TakeAll()
        {
            while (OpenContainer != null && OpenContainer.Contents.Count > 0)
            {
                if (!TakeItem(0))
                {
                    return;
                }
            }
        }

        /// <summary>배낭에서 버린다. 열려 있는 컨테이너가 있으면 그 안에 돌려놓는다.</summary>
        public void DropItem(PlacedItem placed)
        {
            if (Backpack.Remove(placed))
            {
                OpenContainer?.Contents.Add(new ItemStack { ItemId = placed.ItemId, Count = 1 });
                EmitNoise(CurrentArea, X, _balance.DropNoise);
            }
        }

        /// <summary>탈출 지점에서 호출 (테스트에서도 사용).</summary>
        public void ExtractNow()
        {
            Finish(DuskAwakened ? ExtractStatus.Stranded : ExtractStatus.Extracted);
        }

        /// <summary>숨기/나오기 (사물함·책상 근처). 추격당하며 가까이서 보이고 있으면 실패.</summary>
        public bool ToggleHide()
        {
            if (Hidden)
            {
                Hidden = false;
                return true;
            }

            var spot = Nearest;
            if (spot == null || spot.Kind != TargetKind.Container
                || (spot.Container.Kind != ContainerKind.Locker && spot.Container.Kind != ContainerKind.Desk))
            {
                return false;
            }

            if (!_threat.CanHide(Snapshot, Activity))
            {
                _hud?.ShowMessage("exp.hide_blocked");
                return false;
            }

            X = spot.X;
            Hidden = true;
            CloseContainer();
            return true;
        }

        /// <summary>바라보는 방향의 좀비를 밀친다 (큰 소음).</summary>
        public bool Shove()
        {
            if (_shoveTimer > 0f || Hidden)
            {
                return false;
            }

            _shoveTimer = _balance.ShoveCooldown;
            var shoved = _threat.Shove(Snapshot, _facingRight ? 1 : -1);
            EmitNoise(CurrentArea, X, _balance.ShoveNoise);
            if (shoved != null)
            {
                _hud?.ShowMessage("exp.shoved");
            }

            return shoved != null;
        }

        /// <summary>디버그·테스트: 소음 발생.</summary>
        public void DebugEmitNoise(int areaId, float x, float radius)
        {
            _threat.Emit(new NoiseEvent(areaId, x, radius));
        }

        // ── 입력 ──

        private void HandleInput(float dt)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.f2Key.wasPressedThisFrame)
            {
                TimeScale = TimeScale > 1f ? 1f : 10f;
            }

            if (keyboard.tabKey.wasPressedThisFrame)
            {
                _hud.ToggleBackpack();
            }

            if (_shoveTimer > 0f)
            {
                _shoveTimer -= dt;
            }

            Nearest = FindNearest();
            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                ToggleHide();
            }

            if (Hidden)
            {
                return;
            }

            if (keyboard.qKey.wasPressedThisFrame)
            {
                Shove();
            }

            _crouching = keyboard.leftCtrlKey.isPressed;
            var running = keyboard.leftShiftKey.isPressed && !_crouching;
            var move = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
                       - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
            if (move != 0f)
            {
                _facingRight = move > 0f;
                CloseContainer();
                var speed = ExplorationRules.MoveSpeed(running, _crouching, IsOverweight, _balance);
                X = Mathf.Clamp(X + move * speed * dt, EdgeMargin, CurrentArea.Width - EdgeMargin);

                _moveNoiseTimer -= dt;
                if (_moveNoiseTimer <= 0f)
                {
                    _moveNoiseTimer = MoveNoiseInterval;
                    var radius = _crouching ? _balance.CrouchNoise : running ? _balance.RunNoise : _balance.WalkNoise;
                    EmitNoise(CurrentArea, X, IsOverweight ? radius * _balance.OverweightNoiseMultiplier : radius);
                }

                Nearest = FindNearest();
            }
            if (Nearest == null)
            {
                return;
            }

            if (Nearest.Kind == TargetKind.Container && !Nearest.Searched)
            {
                if (keyboard.eKey.isPressed)
                {
                    _searchNoiseTimer -= dt;
                    if (_searchNoiseTimer <= 0f)
                    {
                        _searchNoiseTimer = SearchNoiseInterval;
                        EmitNoise(CurrentArea, X, _balance.SearchNoise);
                    }

                    Nearest.SearchProgress += dt;
                    if (Nearest.SearchProgress >= Nearest.SearchSeconds)
                    {
                        Nearest.MarkSearched();
                        OpenContainer = Nearest;
                    }
                }

                return;
            }

            if (keyboard.eKey.wasPressedThisFrame)
            {
                if (Nearest.Kind == TargetKind.Container && OpenContainer == Nearest)
                {
                    CloseContainer();
                }
                else
                {
                    UseTarget(Nearest);
                }
            }
        }

        private ExplorationTarget FindNearest()
        {
            ExplorationTarget best = null;
            var bestDistance = ExplorationTarget.Reach;
            foreach (var target in CurrentArea.Targets)
            {
                var distance = Mathf.Abs(target.X - X);
                if (distance <= bestDistance)
                {
                    best = target;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private void MoveTo(ExplorationArea area, float x)
        {
            CurrentArea = area;
            X = Mathf.Clamp(x, EdgeMargin, area.Width - EdgeMargin);
            CloseContainer();
            Hidden = false;
            Nearest = null;
            SnapCamera();
        }

        private void Finish(ExtractStatus status)
        {
            if (IsFinished)
            {
                return;
            }

            IsFinished = true;
            var hpDelta = status == ExtractStatus.Down
                ? Math.Min(0, _balance.DownRemainingHp - _startHp)
                : Hp - _startHp;
            Result = RunResultBuilder.Build(status, Backpack.Items, _db, _baseFatigue, _runStress, hpDelta, Clock.Elapsed);
            if (_standalone)
            {
                _hud?.ShowEnd(Result);
                Debug.Log($"[Expedition] 단독 실행 종료: {status}, 자원 {Result.ResourceLoot.Count}종, 아이템 {Result.Loot.Count}종");
            }
            else
            {
                GameApp.ReportExpedition(Result);
            }
        }

        // ── 월드 구성 ──

        private ExpeditionRequest CreateDebugRequest(Systems.Session.GameSession session)
        {
            if (!session.HasGame)
            {
                session.NewGame(Environment.TickCount);
            }

            var player = session.State.Player;
            return new ExpeditionRequest
            {
                LocationId = _debugLocationId,
                Seed = Environment.TickCount,
                TimeOfDay = _debugTimeOfDay,
                Stats = new Dictionary<StatType, int>(player.Stats),
                Hp = player.Vitals.Hp,
                Week = session.State.Calendar.Week,
            };
        }

        private void BuildWorld()
        {
            _world = new GameObject("World").transform;
            _world.SetParent(transform, false);
            var layout = Location.Layout;
            _building = new BuildingView(_world, layout);

            for (var floor = 0; floor < layout.Floors; floor++)
            {
                _corridors.Add(new ExplorationArea
                {
                    Floor = floor,
                    Origin = new Vector2(0f, _building.FloorY(floor)),
                    Width = layout.CorridorLength,
                    Built = true,
                });
            }

            _areasById.InsertRange(0, _corridors);

            foreach (var connector in layout.Connectors)
            {
                var view = Rect(_corridors[connector.Floor], "Stairs", connector.X - 0.5f, new Vector2(1f, 2.7f), new Color(0.32f, 0.38f, 0.5f), 0);
                AddTarget(new ExplorationTarget { Kind = TargetKind.Connector, Area = _corridors[connector.Floor], X = connector.X, Connector = connector, View = view });
            }

            for (var i = 0; i < layout.Doors.Count; i++)
            {
                var door = layout.Doors[i];
                var template = layout.FindTemplate(door.TemplateId);
                if (template == null)
                {
                    Debug.LogWarning($"방 템플릿 없음: {door.TemplateId}");
                    continue;
                }

                var room = new ExplorationArea
                {
                    IsRoom = true,
                    Floor = door.Floor,
                    Origin = new Vector2(layout.CorridorLength + RoomSpacing * (i + 1), 0f),
                    Width = template.Width,
                    DoorIndex = i,
                    Door = door,
                    Template = template,
                    Label = Loc.F("roomname." + template.Id, door.Floor + 1, door.Number),
                };
                _rooms.Add(room);
                _areasById.Add(room);

                var corridor = _corridors[door.Floor];
                var view = Rect(corridor, "Door_" + door.RoomId, door.X - 0.7f, new Vector2(1.4f, 2.8f), BuildingView.DoorColor, 0);
                AddTarget(new ExplorationTarget { Kind = TargetKind.Door, Area = corridor, X = door.X, Room = room, View = view });
            }

            for (var i = 0; i < layout.Containers.Count; i++)
            {
                var container = layout.Containers[i];
                var target = CreateContainer(_corridors[container.Floor], container);
                target.Contents.AddRange(RoomLoot.RollCorridorContainer(_db, Location, i, Request.Seed));
            }

            ExitDef start = null;
            foreach (var exit in layout.Exits)
            {
                var color = exit.StartsLocked ? new Color(0.3f, 0.3f, 0.3f) : new Color(0.35f, 0.6f, 0.38f);
                var view = Rect(_corridors[exit.Floor], "Exit", exit.X - 0.8f, new Vector2(1.6f, 3f), color, 0);
                AddTarget(new ExplorationTarget { Kind = TargetKind.Exit, Area = _corridors[exit.Floor], X = exit.X, Exit = exit, Locked = exit.StartsLocked, View = view });
                if (exit.Kind == ExitKind.Main && start == null)
                {
                    start = exit;
                }
            }

            CurrentArea = _corridors[start?.Floor ?? 0];
            X = start?.X ?? 1f;

            _player = BuildingView.Rect(_world, "Player", Vector2.zero, new Vector2(PlayerWidth, PlayerHeight), new Color(0.88f, 0.84f, 0.76f), 10).transform;
            _playerRenderer = _player.GetComponent<SpriteRenderer>();
            // 바라보는 방향 표시 (부모 스케일 기준 0~1 좌표)
            BuildingView.Rect(_player, "Facing", new Vector2(0.65f, 0.78f), new Vector2(0.35f, 0.05f), new Color(0.2f, 0.2f, 0.2f), 11);
        }

        /// <summary>방 내부는 처음 들어갈 때 만든다. 전리품은 방 시드로 굴려 같은 템플릿이라도 방마다 다르다.</summary>
        private void EnsureRoomBuilt(ExplorationArea room)
        {
            if (room.Built)
            {
                return;
            }

            room.Built = true;
            _building.BuildRoom(room, room.DoorIndex);
            var exitView = Rect(room, "RoomExit", room.Template.ExitX - 0.7f, new Vector2(1.4f, 2.8f), BuildingView.DoorColor, 0);
            AddTarget(new ExplorationTarget { Kind = TargetKind.RoomExit, Area = room, X = room.Template.ExitX, Room = room, View = exitView });

            var contents = RoomLoot.RollRoom(_db, Location, room.DoorIndex, Request.Seed);
            for (var i = 0; i < room.Template.Containers.Count; i++)
            {
                var target = CreateContainer(room, room.Template.Containers[i]);
                if (i < contents.Count)
                {
                    target.Contents.AddRange(contents[i]);
                }
            }
        }

        private ExplorationTarget CreateContainer(ExplorationArea area, ContainerDef container)
        {
            var size = ContainerSize(container.Kind);
            var view = Rect(area, "Container_" + container.Id, container.X - size.x / 2f, size, new Color(0.55f, 0.45f, 0.3f), 1);
            var target = new ExplorationTarget
            {
                Kind = TargetKind.Container,
                Area = area,
                X = container.X,
                Container = container,
                SearchSeconds = ExplorationRules.SearchSeconds(container.SearchSeconds, Request.Stats, _balance),
                View = view,
            };
            AddTarget(target);
            return target;
        }

        private void AddTarget(ExplorationTarget target)
        {
            target.Area.Targets.Add(target);
            _targets.Add(target);
        }

        private GameObject Rect(ExplorationArea area, string name, float localX, Vector2 size, Color color, int order)
        {
            var parent = area.Root != null ? area.Root.transform : _world;
            var origin = area.Root != null ? Vector2.zero : area.Origin;
            return BuildingView.Rect(parent, name, origin + new Vector2(localX, 0.15f), size, color, order);
        }

        private void SetupCamera()
        {
            var go = new GameObject("ExpeditionCamera");
            go.transform.SetParent(transform, false);
            _camera = go.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.depth = 10;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.07f, 0.08f, 0.09f);
            if (FindAnyObjectByType<AudioListener>() == null)
            {
                go.AddComponent<AudioListener>();
            }

            SnapCamera();
        }

        private void SetupLight(bool day)
        {
            var go = new GameObject("GlobalLight2D");
            go.transform.SetParent(transform, false);
            _light = go.AddComponent<Light2D>();
            _light.lightType = Light2D.LightType.Global;
            _light.intensity = day ? 1f : 0.25f;
            _light.color = day ? new Color(1f, 0.97f, 0.9f) : new Color(0.6f, 0.7f, 1f);
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null || FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        private void SnapCamera()
        {
            UpdateViews(1000f);
        }

        private void UpdateViews(float dt)
        {
            if (_player == null || _camera == null)
            {
                return;
            }

            var origin = CurrentArea.Origin;
            var height = _crouching || Hidden ? PlayerHeight * 0.65f : PlayerHeight;
            var worldX = origin.x + X;
            _player.localPosition = new Vector3(worldX + (_facingRight ? -PlayerWidth / 2f : PlayerWidth / 2f), origin.y + 0.15f, 0f);
            _player.localScale = new Vector3(_facingRight ? PlayerWidth : -PlayerWidth, height, 1f);
            _playerRenderer.color = Hidden ? new Color(0.88f, 0.84f, 0.76f, 0.3f) : new Color(0.88f, 0.84f, 0.76f);

            // 화면 비율과 관계없이 가로 최소 MinVisibleWidth 유닛이 보이게 (멀리서 건물과 캐릭터를 함께 잡는 앵글)
            _camera.orthographicSize = Mathf.Clamp(MinVisibleWidth / (2f * Mathf.Max(0.1f, _camera.aspect)), MinOrthographicSize, MaxOrthographicSize);
            // 방이 화면에 다 들어오면 방 전체를 가운데에, 아니면 플레이어를 따라가되 공간 끝 너머 빈 곳은 덜 보이게
            var halfWidth = _camera.orthographicSize * _camera.aspect;
            var width = CurrentArea.Width;
            var cameraX = width <= halfWidth * 2f
                ? origin.x + width / 2f
                : Mathf.Clamp(worldX, origin.x + halfWidth - 2f, origin.x + width - halfWidth + 2f);
            var target = new Vector3(cameraX, origin.y + _building.FloorHeight * 0.5f, -10f);
            var cameraTransform = _camera.transform;
            cameraTransform.position = Vector3.Lerp(cameraTransform.position, target, Mathf.Clamp01(dt * 6f));
        }

        // ── 위협 ──

        private void BuildThreats(LocationVariantDef variant)
        {
            _threat = new ThreatSimulation(_balance, new SeededRandom(SeededRandom.CreateState(Request.Seed ^ 0x5EED)));
            for (var id = 0; id < _areasById.Count; id++)
            {
                _threat.SetAreaWidth(id, _areasById[id].Width);
            }

            foreach (var room in _rooms)
            {
                var corridorId = AreaId(_corridors[room.Floor]);
                var roomId = AreaId(room);
                _threat.AddLink(corridorId, room.Door.X, roomId, room.Template.ExitX);
                _threat.AddLink(roomId, room.Template.ExitX, corridorId, room.Door.X);
            }

            var spread = new SeededRandom(SeededRandom.CreateState(Request.Seed ^ 0x2B));
            foreach (var spawn in variant.Spawns)
            {
                var area = ResolveSpawnArea(spawn);
                if (area == null)
                {
                    Debug.LogWarning($"좀비 배치 공간을 찾을 수 없음: {spawn.Floor}층 {spawn.RoomId}");
                    continue;
                }

                if (!_db.TryGetEnemy(spawn.EnemyId, out var enemy))
                {
                    enemy = new EnemyDef { Id = spawn.EnemyId };
                }

                var areaId = AreaId(area);
                var crowd = spawn.Dormant ? _threat.AddCrowd(areaId, spawn.X) : null;
                for (var i = 0; i < spawn.Count; i++)
                {
                    // 무리 안에서 고르게 벌려 세우고 약간만 흔든다 (한 덩어리로 겹쳐 보이지 않게)
                    var slot = spawn.Count > 1 ? 2f * spawn.Spread / spawn.Count : 0f;
                    var x = spawn.X - spawn.Spread + slot * (i + 0.5f) + (float)(spread.NextDouble() * 2 - 1) * slot * 0.2f;
                    if (spawn.Count <= 1)
                    {
                        x = spawn.X;
                    }
                    var zombie = _threat.AddZombie(enemy, areaId, x, crowd);
                    var body = BuildingView.Rect(_world, "Zombie_" + zombie.Id, Vector2.zero, Vector2.one, new Color(0.42f, 0.5f, 0.4f), 6);
                    var marker = BuildingView.Rect(_world, "Awareness_" + zombie.Id, Vector2.zero, Vector2.one, Color.yellow, 12);
                    _zombieViews.Add(new ZombieView { Unit = zombie, Body = body.transform, BodyRenderer = body.GetComponent<SpriteRenderer>(), Marker = marker.transform, MarkerRenderer = marker.GetComponent<SpriteRenderer>() });
                }
            }
        }

        private ExplorationArea ResolveSpawnArea(SpawnDef spawn)
        {
            if (string.IsNullOrEmpty(spawn.RoomId))
            {
                return spawn.Floor >= 0 && spawn.Floor < _corridors.Count ? _corridors[spawn.Floor] : null;
            }

            return _rooms.Find(r => r.Door.RoomId == spawn.RoomId);
        }

        private void StepThreats(float dt)
        {
            Activity = ActivityModel.Evaluate(Clock.Phase, DuskAwakened, _balance);
            NoiseLevel = Mathf.Max(0f, NoiseLevel - dt * 6f);
            var events = _threat.Step(dt, Snapshot, Activity);
            if (events.Damage > 0)
            {
                Hp = Math.Max(0, Hp - events.Damage);
                _hud?.ShowMessage("exp.hurt");
                if (Hp <= 0)
                {
                    Finish(ExtractStatus.Down);
                    return;
                }
            }

            if (events.Spotted)
            {
                _hud?.ShowMessage("exp.spotted");
            }
            else if (events.WokenCrowds.Count > 0)
            {
                _hud?.ShowMessage("exp.crowd_woke");
            }
        }

        private void TriggerDuskAwakening()
        {
            DuskAwakened = true;
            _threat.AwakenAll();
            if (_light != null)
            {
                _light.intensity = 0.35f;
                _light.color = new Color(0.75f, 0.6f, 0.65f);
            }

            _hud?.ShowMessage("exp.dusk");
        }

        private void EmitNoise(ExplorationArea area, float x, float radius)
        {
            if (_threat == null || radius <= 0f)
            {
                return;
            }

            _threat.Emit(new NoiseEvent(AreaId(area), x, radius));
            if (area == CurrentArea)
            {
                NoiseLevel = Mathf.Max(NoiseLevel, radius);
            }
        }

        private void UpdateZombieViews()
        {
            foreach (var view in _zombieViews)
            {
                var unit = view.Unit;
                var area = _areasById[unit.AreaId];
                var origin = area.Root != null ? (Vector2)area.Root.transform.localPosition : area.Origin;
                var dormant = unit.State == ZombieState.Dormant;

                // 휴면도 선 채로 (고개 숙인 듯 살짝 낮게), 머리 위 Zzz는 HUD가 그린다
                var size = dormant ? new Vector2(0.55f, 1.6f) : new Vector2(0.6f, 1.75f);
                view.Body.localPosition = new Vector3(origin.x + unit.X - size.x / 2f, origin.y + 0.15f, 0f);
                view.Body.localScale = new Vector3(size.x, size.y, 1f);
                var shade = 1f + (unit.Id % 3 - 1) * 0.08f;
                var color = dormant ? new Color(0.34f, 0.4f, 0.34f)
                    : unit.State == ZombieState.Stunned ? new Color(0.35f, 0.4f, 0.55f)
                    : unit.State == ZombieState.Chase ? new Color(0.6f, 0.38f, 0.33f)
                    : new Color(0.42f, 0.5f, 0.4f);
                view.BodyRenderer.color = new Color(color.r * shade, color.g * shade, color.b * shade);

                var showMarker = !dormant && (unit.Awareness > 0.02f || unit.State == ZombieState.Chase);
                view.Marker.gameObject.SetActive(showMarker);
                if (showMarker)
                {
                    var width = unit.State == ZombieState.Chase ? 0.7f : 0.7f * unit.Awareness;
                    view.Marker.localPosition = new Vector3(origin.x + unit.X - 0.35f, origin.y + 2.15f, 0f);
                    view.Marker.localScale = new Vector3(Mathf.Max(0.05f, width), 0.12f, 1f);
                    view.MarkerRenderer.color = unit.State == ZombieState.Chase ? new Color(0.9f, 0.25f, 0.2f) : new Color(0.95f, 0.8f, 0.2f);
                }
            }
        }

        private sealed class ZombieView
        {
            public ZombieUnit Unit;
            public Transform Body;
            public SpriteRenderer BodyRenderer;
            public Transform Marker;
            public SpriteRenderer MarkerRenderer;
        }

        private static Vector2 ContainerSize(ContainerKind kind)
        {
            switch (kind)
            {
                case ContainerKind.Locker: return new Vector2(0.8f, 1.9f);
                case ContainerKind.Desk: return new Vector2(1.4f, 0.8f);
                case ContainerKind.Cabinet: return new Vector2(1.0f, 1.4f);
                case ContainerKind.Fridge: return new Vector2(0.9f, 1.8f);
                default: return new Vector2(0.9f, 0.7f);
            }
        }
    }
}
