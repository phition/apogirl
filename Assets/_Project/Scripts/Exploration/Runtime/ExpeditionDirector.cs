using System;
using System.Collections.Generic;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Random;
using ApoGirl.Data;
using ApoGirl.Exploration.Model;
using ApoGirl.Presentation.App;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace ApoGirl.Exploration.Runtime
{
    /// <summary>
    /// 탐험 씬 진입점 (M2b-1): 요청을 받아 건물을 만들고, 이동·계단·수색·배낭·시계·탈출을 돌린 뒤 결과를 허브에 보고한다.
    /// 허브를 거치지 않고 씬을 단독 실행하면 디버그 요청으로 시작한다.
    /// </summary>
    public sealed class ExpeditionDirector : MonoBehaviour
    {
        private const float PlayerWidth = 0.6f;
        private const float PlayerHeight = 1.7f;
        private const float MinOrthographicSize = 3.4f;
        private const float MinVisibleWidth = 16f;

        [Tooltip("허브 없이 단독 실행할 때 쓸 정의 DB")]
        [SerializeField] private DefinitionDatabaseAsset _fallbackDatabase;

        [SerializeField] private PanelSettings _panelSettings;

        [Tooltip("탐험 HUD 레이아웃 (Assets/_Project/UI/Expedition.uxml)")]
        [SerializeField] private VisualTreeAsset _hudLayout;

        [Tooltip("단독 실행 시 지역")]
        [SerializeField] private string _debugLocationId = "LOC_SCHOOL_INTERIOR";

        [Tooltip("단독 실행 시 시간대")]
        [SerializeField] private ExpeditionTime _debugTimeOfDay = ExpeditionTime.Day;

        private readonly List<ExplorationTarget> _targets = new List<ExplorationTarget>();
        private IDefinitionDatabase _db;
        private BalanceConfig _balance;
        private BuildingView _building;
        private Transform _player;
        private Camera _camera;
        private ExpeditionHud _hud;
        private bool _standalone;
        private int _baseFatigue;
        private int _runStress;
        private bool _facingRight = true;

        public ExpeditionRequest Request { get; private set; }

        public LocationDef Location { get; private set; }

        public RunClock Clock { get; private set; }

        public BackpackGrid Backpack { get; private set; }

        public float CarryCapacity { get; private set; }

        public int Floor { get; private set; }

        public float X { get; private set; }

        public ExplorationTarget Nearest { get; private set; }

        public ExplorationTarget OpenContainer { get; private set; }

        /// <summary>디버그: 시간 배속 (F2).</summary>
        public float TimeScale { get; private set; } = 1f;

        public bool IsFinished { get; private set; }

        public ExpeditionResult Result { get; private set; }

        public IDefinitionDatabase Definitions => _db;

        public IReadOnlyList<ExplorationTarget> Targets => _targets;

        public BuildingView Building => _building;

        private void Start()
        {
            var session = GameApp.EnsureSession(_fallbackDatabase);
            _db = session.Definitions;
            _balance = _db.Balance;
            Request = GameApp.ActiveExpedition;
            if (Request == null)
            {
                _standalone = true;
                if (!session.HasGame)
                {
                    session.NewGame(Environment.TickCount);
                }

                var player = session.State.Player;
                Request = new ExpeditionRequest
                {
                    LocationId = _debugLocationId,
                    Seed = Environment.TickCount,
                    TimeOfDay = _debugTimeOfDay,
                    Stats = new Dictionary<Core.State.StatType, int>(player.Stats),
                    Hp = player.Vitals.Hp,
                    Week = session.State.Calendar.Week,
                };
            }

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
            SetupCamera();
            SetupLight(day);
            EnsureEventSystem();

            var document = gameObject.AddComponent<UIDocument>();
            document.panelSettings = _panelSettings;
            document.sortingOrder = 10;
            document.visualTreeAsset = _hudLayout;
            _hud = new ExpeditionHud(document.rootVisualElement, this);
        }

        private void Update()
        {
            if (IsFinished || _hud == null)
            {
                return;
            }

            var dt = Time.deltaTime;
            Clock.Tick(dt * TimeScale);
            if (Clock.IsOver)
            {
                Finish(ExtractStatus.Stranded);
                return;
            }

            HandleInput(dt);
            UpdateViews(dt);
            _hud.Refresh();
        }

        // ── 공개 조작 (HUD·테스트에서 호출) ──

        public bool TakeItem(int contentIndex)
        {
            var container = OpenContainer;
            if (container == null || contentIndex < 0 || contentIndex >= container.Contents.Count)
            {
                return false;
            }

            var stack = container.Contents[contentIndex];
            var item = _db.GetItem(stack.ItemId);
            if (Backpack.TryAutoPlace(item.Id, item.Width, item.Height, item.Weight) == null)
            {
                _hud.ShowMessage("exp.backpack_full");
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
            if (!Backpack.Remove(placed))
            {
                return;
            }

            OpenContainer?.Contents.Add(new Core.State.ItemStack { ItemId = placed.ItemId, Count = 1 });
        }

        public void CloseContainer()
        {
            OpenContainer = null;
        }

        /// <summary>이미 수색한 컨테이너를 연다.</summary>
        public void Open(ExplorationTarget container)
        {
            if (container != null && container.Kind == TargetKind.Container && container.Searched)
            {
                OpenContainer = container;
            }
        }

        /// <summary>탈출 지점에서 호출 (테스트에서도 사용).</summary>
        public void ExtractNow()
        {
            Finish(ExtractStatus.Extracted);
        }

        public bool IsOverweight => Backpack.TotalWeight > CarryCapacity;

        // ── 내부 ──

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

            var move = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
                       - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
            if (move != 0f)
            {
                _facingRight = move > 0f;
                if (OpenContainer != null)
                {
                    CloseContainer();
                }

                var speed = ExplorationRules.MoveSpeed(keyboard.leftShiftKey.isPressed, keyboard.leftCtrlKey.isPressed, IsOverweight, _balance);
                var (min, max) = _building.FloorBounds(Floor);
                X = Mathf.Clamp(X + move * speed * dt, min, max);
            }

            Nearest = FindNearest();
            if (Nearest == null)
            {
                return;
            }

            switch (Nearest.Kind)
            {
                case TargetKind.Container:
                    if (!Nearest.Searched && keyboard.eKey.isPressed)
                    {
                        Nearest.SearchProgress += dt;
                        if (Nearest.SearchProgress >= Nearest.SearchSeconds)
                        {
                            Nearest.MarkSearched();
                            OpenContainer = Nearest;
                        }
                    }
                    else if (Nearest.Searched && keyboard.eKey.wasPressedThisFrame)
                    {
                        OpenContainer = OpenContainer == Nearest ? null : Nearest;
                    }

                    break;
                case TargetKind.Connector:
                    if (keyboard.eKey.wasPressedThisFrame)
                    {
                        Floor = Nearest.Connector.TargetFloor;
                        CloseContainer();
                    }

                    break;
                case TargetKind.Exit:
                    if (keyboard.eKey.wasPressedThisFrame)
                    {
                        if (Nearest.Locked)
                        {
                            _hud.ShowMessage("exp.exit_locked");
                        }
                        else
                        {
                            ExtractNow();
                        }
                    }

                    break;
            }
        }

        private ExplorationTarget FindNearest()
        {
            ExplorationTarget best = null;
            var bestDistance = ExplorationTarget.Reach;
            foreach (var target in _targets)
            {
                if (target.Floor != Floor)
                {
                    continue;
                }

                var distance = Mathf.Abs(target.X - X);
                if (distance <= bestDistance)
                {
                    best = target;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private void Finish(ExtractStatus status)
        {
            if (IsFinished)
            {
                return;
            }

            IsFinished = true;
            Result = RunResultBuilder.Build(status, Backpack.Items, _db, _baseFatigue, _runStress, 0, Clock.Elapsed);
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

        private void BuildWorld()
        {
            var world = new GameObject("World").transform;
            world.SetParent(transform, false);
            var layout = Location.Layout;
            _building = new BuildingView(world, layout);

            foreach (var connector in layout.Connectors)
            {
                var view = BuildingView.Rect(world, "Stairs", new Vector2(connector.X - 0.45f, _building.FloorY(connector.Floor) + 0.15f),
                    new Vector2(0.9f, 2.7f), new Color(0.32f, 0.38f, 0.5f), 0);
                _targets.Add(new ExplorationTarget { Kind = TargetKind.Connector, Floor = connector.Floor, X = connector.X, Connector = connector, View = view });
            }

            for (var i = 0; i < layout.Containers.Count; i++)
            {
                var container = layout.Containers[i];
                var size = ContainerSize(container.Kind);
                var view = BuildingView.Rect(world, "Container_" + container.Id, new Vector2(container.X - size.x / 2f, _building.FloorY(container.Floor) + 0.15f),
                    size, new Color(0.55f, 0.45f, 0.3f), 1);
                var target = new ExplorationTarget
                {
                    Kind = TargetKind.Container,
                    Floor = container.Floor,
                    X = container.X,
                    Container = container,
                    ContainerIndex = i,
                    SearchSeconds = ExplorationRules.SearchSeconds(container.SearchSeconds, Request.Stats, _balance),
                    View = view,
                };

                var tableId = string.IsNullOrEmpty(container.LootTableId) ? Location.DefaultLootTableId : container.LootTableId;
                if (_db.TryGetLootTable(tableId, out var table))
                {
                    var random = new SeededRandom(SeededRandom.CreateState(ExplorationRules.ContainerSeed(Request.Seed, i)));
                    target.Contents.AddRange(LootRoller.Roll(table, random));
                }

                _targets.Add(target);
            }

            ExitDef start = null;
            foreach (var exit in layout.Exits)
            {
                var color = exit.StartsLocked ? new Color(0.3f, 0.3f, 0.3f) : new Color(0.35f, 0.6f, 0.38f);
                var view = BuildingView.Rect(world, "Exit", new Vector2(exit.X - 0.6f, _building.FloorY(exit.Floor) + 0.15f), new Vector2(1.2f, 2.8f), color, 0);
                _targets.Add(new ExplorationTarget { Kind = TargetKind.Exit, Floor = exit.Floor, X = exit.X, Exit = exit, Locked = exit.StartsLocked, View = view });
                if (exit.Kind == ExitKind.Main && start == null)
                {
                    start = exit;
                }
            }

            Floor = start?.Floor ?? 0;
            X = start?.X ?? 1f;

            _player = BuildingView.Rect(world, "Player", Vector2.zero, new Vector2(PlayerWidth, PlayerHeight), new Color(0.88f, 0.84f, 0.76f), 10).transform;
            // 바라보는 방향 표시 (부모 스케일 기준 0~1 좌표)
            BuildingView.Rect(_player, "Facing", new Vector2(0.65f, 0.78f), new Vector2(0.35f, 0.05f), new Color(0.2f, 0.2f, 0.2f), 11);
        }

        private void SetupCamera()
        {
            var go = new GameObject("ExpeditionCamera");
            go.transform.SetParent(transform, false);
            _camera = go.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = 3.4f;
            _camera.depth = 10;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.07f, 0.08f, 0.09f);
            if (FindAnyObjectByType<AudioListener>() == null)
            {
                go.AddComponent<AudioListener>();
            }

            UpdateViews(10f);
        }

        private void SetupLight(bool day)
        {
            var go = new GameObject("GlobalLight2D");
            go.transform.SetParent(transform, false);
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = day ? 1f : 0.25f;
            light.color = day ? new Color(1f, 0.97f, 0.9f) : new Color(0.6f, 0.7f, 1f);
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

        private void UpdateViews(float dt)
        {
            if (_player == null)
            {
                return;
            }

            var floorY = _building.FloorY(Floor);
            var crouching = Keyboard.current != null && Keyboard.current.leftCtrlKey.isPressed;
            var height = crouching ? PlayerHeight * 0.65f : PlayerHeight;
            _player.localPosition = new Vector3(X - PlayerWidth / 2f, floorY + 0.15f, 0f);
            _player.localScale = new Vector3(_facingRight ? PlayerWidth : -PlayerWidth, height, 1f);
            if (!_facingRight)
            {
                _player.localPosition += new Vector3(PlayerWidth, 0f, 0f);
            }

            // 화면 비율과 관계없이 가로 최소 MinVisibleWidth 유닛, 세로 한 층 이상이 보이게
            _camera.orthographicSize = Mathf.Max(MinOrthographicSize, MinVisibleWidth / (2f * Mathf.Max(0.1f, _camera.aspect)));
            var target = new Vector3(X, floorY + _building.FloorHeight * 0.55f, -10f);
            var cameraTransform = _camera.transform;
            cameraTransform.position = Vector3.Lerp(cameraTransform.position, target, Mathf.Clamp01(dt * 6f));
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
