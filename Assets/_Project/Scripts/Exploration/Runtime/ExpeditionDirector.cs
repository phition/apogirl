using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        /// <summary>보이는 공간이 바뀔 때(문 통과·문틈 살피기) 짧게 어두워졌다 밝아진다.</summary>
        private const float FadeOutSeconds = 0.1f;
        private const float FadeInSeconds = 0.15f;

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
        private SpriteRenderer _fadeOverlay;

        /// <summary>카메라가 지금 비추고 있는 공간 (페이드 중에는 목표 공간과 다를 수 있다).</summary>
        private ExplorationArea _shownArea;
        private ExpeditionHud _hud;
        private bool _standalone;
        private int _baseFatigue;
        private int _runStress;
        private bool _facingRight = true;
        private readonly List<ExplorationArea> _areasById = new List<ExplorationArea>();
        private readonly List<ZombieView> _zombieViews = new List<ZombieView>();
        private readonly List<GameObject> _silhouettes = new List<GameObject>();
        private ThreatSimulation _threat;
        private Light2D _light;
        private int _startHp;
        private float _moveNoiseTimer;

        // ── M4: 장비·조우·튜토리얼 ──
        private ItemDef _weapon;
        private ItemDef _armor;
        private ItemDef _backpackItem;
        private ToolKind _toolKind;
        private int _weaponDurability;
        private int _weaponWear;
        private float _attackTimer;
        private readonly List<string> _quickSlots = new List<string>();
        private readonly List<string> _usedItems = new List<string>();
        private readonly List<string> _runFlags = new List<string>();
        private readonly List<string> _runClues = new List<string>();
        private float _moveMultiplier = 1f;
        private bool _playerSprites;
        private bool _animMoving;
        private bool _animRunning;
        private float _animTime;
        private float _attackAnim;
        private float _noiseMultiplier = 1f;
        private float _tutorialMoved;
        private float _tutorialCrouched;
        private bool _tutorialSearched;
        private bool _tutorialHidden;
        private bool _tutorialOpenedBag;
        private float _searchNoiseTimer;
        private float _shoveTimer;
        private bool _crouching;
        private Light2D _flashlight;
        private Light2D _selfLight;
        private float _ambientIntensity;
        private readonly PlayLogEntry _log = new PlayLogEntry();

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

        public PlayerSnapshot Snapshot => new PlayerSnapshot { AreaId = AreaId(CurrentArea), X = X, Crouching = _crouching, Hidden = Hidden, Visibility = Visibility };

        public int AreaId(ExplorationArea area) => _areasById.IndexOf(area);

        /// <summary>어둠 (밤 탐험, 또는 낮 탐험의 황혼 각성 이후).</summary>
        public bool IsDark => Request.TimeOfDay == ExpeditionTime.Night || DuskAwakened;

        public bool FlashlightOn { get; private set; }

        /// <summary>장비 (HUD 표시용).</summary>
        public ItemDef Weapon => _weapon;
        public ItemDef Armor => _armor;
        public ToolKind ToolKind => _toolKind;
        public int WeaponDurabilityLeft => _weapon == null ? 0 : Math.Max(0, _weaponDurability - _weaponWear);
        public IReadOnlyList<string> QuickSlots => _quickSlots;

        /// <summary>조우 선택 중 (시간 정지).</summary>
        public ExplorationTarget OpenEncounter { get; private set; }

        /// <summary>튜토리얼 안내 키 (튜토리얼 런이 아니면 null).</summary>
        public string TutorialHintKey
        {
            get
            {
                if (Location == null || !Location.Tutorial) return null;
                if (_tutorialMoved < 3f) return "tut.move";
                if (_tutorialCrouched < 1f) return "tut.crouch";
                if (!_tutorialSearched) return "tut.search";
                if (!_tutorialOpenedBag) return "tut.backpack";
                if (!_tutorialHidden) return "tut.hide";
                return "tut.extract";
            }
        }

        /// <summary>밤 시계가 끝나 새벽이 됨: 활동성이 가라앉고, 이후 탈출은 새벽 귀환 (수면 부채 +1, 문서 6.3.2-(4)).</summary>
        public bool DawnReturn { get; private set; }

        /// <summary>디버그 (F3): AI 상태·소음 반경 표시.</summary>
        public bool DebugOverlay { get; private set; }

        /// <summary>디버그 (F4): 무적.</summary>
        public bool Invincible { get; private set; }

        /// <summary>디버그 (F5): 전체 시야 (조명 최대).</summary>
        public bool FullVision { get; private set; }

        /// <summary>
        /// 플레이어 화면에 이 좀비를 그리는가: 같은 공간에 있고, 어둠 속이면 가까이 있거나 손전등에 비춰질 때만.
        /// 디버그 전체 시야(F5)·AI 표시(F3)에서는 모두 보인다.
        /// </summary>
        public bool IsVisibleToPlayer(ZombieUnit unit)
        {
            if (unit.IsDown)
            {
                return false; // 쓰러진 좀비는 치운다 (고어 지양, 문서 13-4)
            }

            if (FullVision || DebugOverlay)
            {
                return true;
            }

            if (PeekTarget != null)
            {
                return unit.AreaId == AreaId(PeekArea) && Mathf.Abs(unit.X - PeekTarget.LinkX) <= PeekViewRange;
            }

            return PlayerViewRules.CanPlayerSee(
                AreaId(CurrentArea), X, _facingRight ? 1 : -1, IsDark, FlashlightOn && !Hidden,
                unit.AreaId, unit.X, _balance);
        }

        /// <summary>문틈으로 살피는 중인 문 (W를 누르고 있는 동안).</summary>
        public ExplorationTarget PeekTarget { get; private set; }

        /// <summary>귀 기울이는 진행도 0~1 (S).</summary>
        public float ListenProgress { get; private set; }

        /// <summary>문틈 너머 공간.</summary>
        public ExplorationArea PeekArea => PeekTarget == null ? null
            : PeekTarget.Kind == TargetKind.Door ? PeekTarget.Room : _corridors[PeekTarget.Room.Floor];

        /// <summary>문틈으로 살필 때 보이는 거리.</summary>
        public float PeekViewRange => IsDark ? _balance.NightViewDistance : _balance.PeekRange;

        /// <summary>좀비 눈에 띄는 정도: 낮 1, 어둠 0.6, 손전등 1.5.</summary>
        public float Visibility => (!IsDark ? 1f : FlashlightOn ? _balance.FlashlightExposure : _balance.DarknessVisibility)
                                   * Mathf.Max(0.3f, 1f - Request.Modifier("StealthBonus"));

        /// <summary>탐험 카메라 (HUD가 월드 위치를 화면 좌표로 바꿀 때 사용).</summary>
        public Camera ViewCamera => _camera;

        /// <summary>화면 전환 페이드 정도 (0 = 밝음, 1 = 완전히 어두움).</summary>
        public float ScreenFade { get; private set; }

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
                if (PeekTarget != null)
                {
                    var other = PeekArea;
                    return Loc.F("exp.peeking_into", other.IsRoom ? other.Label : Loc.F("exp.corridor", other.Floor + 1));
                }

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
            SetupEquipment();
            Backpack = _backpackItem != null && _backpackItem.GridWidth > 0
                ? new BackpackGrid(_backpackItem.GridWidth, _backpackItem.GridHeight)
                : new BackpackGrid(_balance.PocketWidth, _balance.PocketHeight);
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

            if (OpenEncounter != null)
            {
                _hud.Refresh();
                return;
            }

            var dt = Time.deltaTime;
            Clock.Tick(dt * TimeScale);
            if (Clock.IsOver)
            {
                if (Request.TimeOfDay == ExpeditionTime.Day && !DuskAwakened)
                {
                    TriggerDuskAwakening();
                }
                else if (Request.TimeOfDay == ExpeditionTime.Night && !DawnReturn)
                {
                    TriggerDawn();
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
            UpdateWindowSilhouettes();
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
                case TargetKind.Door when target.RoomLocked:
                    _hud?.ShowMessage("exp.room_shuttered");
                    break;
                case TargetKind.Door when target.Forceable && !target.Forced:
                    if (_toolKind != ToolKind.Crowbar)
                    {
                        _hud?.ShowMessage("exp.room_locked_need_crowbar");
                        break;
                    }

                    target.Forced = true;
                    foreach (var other in target.Room.CorridorDoors)
                    {
                        other.Forced = true;
                        var doorRenderer = other.View != null ? other.View.GetComponent<SpriteRenderer>() : null;
                        if (doorRenderer != null && ExplorationArt.Get("door_open") != null)
                        {
                            doorRenderer.sprite = ExplorationArt.Get("door_open");
                            doorRenderer.color = Color.white;
                        }
                    }

                    EmitNoise(CurrentArea, X, _balance.ShoveNoise);
                    _hud?.ShowMessage("exp.forced_open");
                    break;
                case TargetKind.Encounter:
                    OpenEncounter = target;
                    break;
                case TargetKind.Door:
                    EnsureRoomBuilt(target.Room);
                    EmitNoise(CurrentArea, X, _balance.DoorNoise);
                    MoveTo(target.Room, target.LinkX);
                    EmitNoise(CurrentArea, X, _balance.DoorNoise);
                    if (!target.Room.Visited)
                    {
                        _log.RoomsEntered++;
                        target.Room.Visited = true;
                        foreach (var door in target.Room.CorridorDoors)
                        {
                            door.Tint(BuildingView.VisitedDoorColor);
                        }
                    }

                    break;
                case TargetKind.RoomExit:
                    EmitNoise(CurrentArea, X, _balance.DoorNoise);
                    MoveTo(_corridors[target.Room.Floor], target.LinkX);
                    EmitNoise(CurrentArea, X, _balance.DoorNoise);
                    break;
                case TargetKind.Exit:
                    if (target.Locked && _toolKind == ToolKind.Crowbar)
                    {
                        target.Locked = false;
                        var exitRenderer = target.View != null ? target.View.GetComponent<SpriteRenderer>() : null;
                        var openSprite = ExplorationArt.Get("prop_exit");
                        if (exitRenderer != null && openSprite != null)
                        {
                            exitRenderer.sprite = openSprite;
                            exitRenderer.color = Color.white;
                        }
                        else
                        {
                            target.Tint(new Color(0.35f, 0.6f, 0.38f));
                        }
                        EmitNoise(CurrentArea, X, _balance.ShoveNoise);
                        _hud?.ShowMessage("exp.forced_open");
                    }
                    else if (target.Locked)
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

        /// <summary>손전등 켜기/끄기 (F). 어둠 속 수색이 빨라지는 대신 멀리서도 눈에 띈다.</summary>
        public void ToggleFlashlight()
        {
            if (_toolKind != ToolKind.Flashlight && !FlashlightOn)
            {
                _hud?.ShowMessage("exp.no_flashlight");
                return;
            }

            FlashlightOn = !FlashlightOn;
        }

        /// <summary>
        /// Q: 주무기가 있으면 공격 (타격 누적·쓰러뜨림·내구도 소모·무기 소음), 없으면 밀치기.
        /// 정면전은 손해라는 감각을 위해 소음이 크고 내구도가 닳는다 (문서 6.3.5-(4)).
        /// </summary>
        public bool Attack()
        {
            if (_weapon == null)
            {
                return Shove();
            }

            if (_attackTimer > 0f || Hidden)
            {
                return false;
            }

            _attackTimer = Mathf.Max(0.3f, _weapon.AttackCooldown);
            _attackAnim = 0.25f;
            EmitNoise(CurrentArea, X, _weapon.AttackNoise);
            var hit = _threat.Strike(Snapshot, _facingRight ? 1 : -1, _weapon.WeaponPower, _weapon.StunSeconds, _balance.ShoveRange + 0.3f);
            if (hit == null)
            {
                return false;
            }

            _weaponWear++;
            _hud?.ShowMessage(hit.IsDown ? "exp.zombie_down" : "exp.hit");
            if (_weaponDurability > 0 && _weaponWear >= _weaponDurability)
            {
                _hud?.ShowMessage("exp.weapon_broke");
                _weapon = null;
            }

            return true;
        }

        /// <summary>퀵슬롯 소모품 사용 (1~4). 탐험 중 HP 회복·스트레스 완화.</summary>
        public bool UseQuickSlot(int index)
        {
            if (index < 0 || index >= _quickSlots.Count || IsFinished)
            {
                return false;
            }

            var item = _db.GetItemOrNull(_quickSlots[index]);
            if (item == null)
            {
                return false;
            }

            _usedItems.Add(item.Id);
            _quickSlots.RemoveAt(index);
            if (item.UseHp > 0)
            {
                Hp = Math.Min(Math.Max(_startHp, Hp), Hp + item.UseHp);
            }

            if (item.UseStress > 0)
            {
                _runStress = Math.Max(0, _runStress - item.UseStress);
            }

            _hud?.ShowText(Loc.F("exp.used_item", Loc.Item(item.Id)));
            return true;
        }

        /// <summary>조우 선택지를 고른다.</summary>
        public void ChooseEncounter(int choiceIndex)
        {
            var target = OpenEncounter;
            if (target == null || choiceIndex < 0 || choiceIndex >= target.Encounter.Choices.Count)
            {
                return;
            }

            var choice = target.Encounter.Choices[choiceIndex];
            _runFlags.AddRange(choice.SetFlags);
            _runClues.AddRange(choice.AddClues);
            _runStress = Math.Max(0, _runStress + (int)choice.StressDelta);
            _runFlags.Add("ENC_" + target.Encounter.Id + "_" + choice.Id);
            OpenEncounter = null;
            if (choice.Removes)
            {
                target.Area.Targets.Remove(target);
                _targets.Remove(target);
                if (target.View != null)
                {
                    target.View.SetActive(false);
                }
            }
            else
            {
                target.Encounter = null;
                target.Area.Targets.Remove(target);
                _targets.Remove(target);
            }
        }

        public void CancelEncounter()
        {
            OpenEncounter = null;
        }

        private void SetupEquipment()
        {
            var loadout = Request.Loadout ?? new LoadoutState();
            _backpackItem = _db.GetItemOrNull(loadout.Backpack);
            _weapon = _db.GetItemOrNull(loadout.MainWeapon);
            _armor = _db.GetItemOrNull(loadout.Armor);
            var tool = _db.GetItemOrNull(loadout.Tool);
            _toolKind = tool?.Tool ?? ToolKind.None;
            _weaponDurability = _weapon == null ? 0 : Request.WeaponDurability > 0 ? Request.WeaponDurability : _weapon.MaxDurability;
            _quickSlots.Clear();
            _quickSlots.AddRange(loadout.QuickSlots.Where(id => !string.IsNullOrEmpty(id)));

            _moveMultiplier = (_backpackItem?.MoveSpeedMultiplier ?? 1f) * (_armor?.MoveSpeedMultiplier ?? 1f);
            _noiseMultiplier = (_backpackItem?.NoiseMultiplier ?? 1f) * (_armor?.NoiseMultiplier ?? 1f)
                               * Mathf.Max(0.2f, 1f + Request.Modifier("NoiseMultiplier"));
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
                _log.DebugUsed = true;
            }

            HandleDebugKeys(keyboard);
            if (keyboard.fKey.wasPressedThisFrame)
            {
                ToggleFlashlight();
            }

            if (keyboard.tabKey.wasPressedThisFrame)
            {
                _hud.ToggleBackpack();
                _tutorialOpenedBag = true;
            }

            if (_shoveTimer > 0f)
            {
                _shoveTimer -= dt;
            }

            if (_attackTimer > 0f)
            {
                _attackTimer -= dt;
            }

            if (keyboard.digit1Key.wasPressedThisFrame) UseQuickSlot(0);
            if (keyboard.digit2Key.wasPressedThisFrame) UseQuickSlot(1);
            if (keyboard.digit3Key.wasPressedThisFrame) UseQuickSlot(2);
            if (keyboard.digit4Key.wasPressedThisFrame) UseQuickSlot(3);

            Nearest = FindNearest();
            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                ToggleHide();
            }

            if (Hidden)
            {
                return;
            }

            if (HandleDoorSenses(keyboard, dt))
            {
                return;
            }

            if (keyboard.qKey.wasPressedThisFrame)
            {
                Attack();
            }

            if (Hidden)
            {
                _tutorialHidden = true;
            }

            _crouching = keyboard.leftCtrlKey.isPressed;
            var running = keyboard.leftShiftKey.isPressed && !_crouching;
            _animRunning = running;
            var move = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
                       - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
            _animMoving = move != 0f;
            if (move != 0f)
            {
                _facingRight = move > 0f;
                CloseContainer();
                var speed = ExplorationRules.MoveSpeed(running, _crouching, IsOverweight, _balance)
                            * _moveMultiplier * (running ? 1f + Request.Modifier("RunSpeed") : 1f);
                _tutorialMoved += Mathf.Abs(move) * speed * dt;
                if (_crouching)
                {
                    _tutorialCrouched += dt;
                }
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

                    var searchRate = 1f + Request.Modifier("SearchSpeed");
                    Nearest.SearchProgress += (IsDark && !FlashlightOn ? dt / _balance.DarkSearchMultiplier : dt) * searchRate;
                    if (Nearest.SearchProgress >= Nearest.SearchSeconds)
                    {
                        _log.ContainersSearched++;
                        Nearest.MarkSearched();
                        _tutorialSearched = true;
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
            Result.UsedItems.AddRange(_usedItems);
            Result.WeaponWear = _weaponWear;
            foreach (var flag in _runFlags)
            {
                if (!Result.Flags.Contains(flag)) Result.Flags.Add(flag);
            }

            foreach (var clue in _runClues)
            {
                if (!Result.Clues.Contains(clue)) Result.Clues.Add(clue);
            }

            if (DawnReturn && status != ExtractStatus.Down)
            {
                // 새벽까지 버틴 대가: 수면 부채 +1, 다음 강제 휴식 회복 감소 (허브가 처리)
                Result.SleepDebtDelta = 1;
            }

            WritePlayLog(status);
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
                var view = Prop(_corridors[connector.Floor], "Stairs", connector.TargetFloor > connector.Floor ? "prop_stairs_up" : "prop_stairs_down",
                    connector.X, new Vector2(1f, 2.7f), new Color(0.32f, 0.38f, 0.5f), 0);
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
                    SpanX = door.X,
                    Template = template,
                    Label = Loc.F("roomname." + template.Id, door.Floor + 1, door.Number),
                };
                _rooms.Add(room);
                _areasById.Add(room);

                var corridor = _corridors[door.Floor];
                foreach (var doorX in template.DoorXs)
                {
                    var corridorX = room.SpanX + doorX;
                    var view = Prop(corridor, "Door_" + door.RoomId, "door_closed", corridorX, new Vector2(1.4f, 2.8f), BuildingView.DoorColor, 0);
                    var target = new ExplorationTarget { Kind = TargetKind.Door, Area = corridor, X = corridorX, Room = room, LinkX = doorX, View = view };
                    var variant = Request.TimeOfDay == ExpeditionTime.Day ? Location.DayVariant : Location.NightVariant;
                    target.RoomLocked = variant.LockedRooms.Contains(door.RoomId);
                    target.Forceable = variant.ForceableRooms.Contains(door.RoomId);
                    if (target.RoomLocked || target.Forceable)
                    {
                        target.Tint(target.RoomLocked ? new Color(0.42f, 0.42f, 0.48f) : new Color(0.88f, 0.62f, 0.56f));
                    }
                    AddTarget(target);
                    room.CorridorDoors.Add(target);
                }
            }

            for (var i = 0; i < layout.Containers.Count; i++)
            {
                var container = layout.Containers[i];
                var target = CreateContainer(_corridors[container.Floor], container);
                target.Contents.AddRange(RoomLoot.RollCorridorContainer(_db, Location, i, Request.Seed));
            }

            foreach (var encounter in ActiveEncounters())
            {
                if (string.IsNullOrEmpty(encounter.RoomId) && encounter.Floor >= 0 && encounter.Floor < _corridors.Count)
                {
                    CreateEncounter(_corridors[encounter.Floor], encounter);
                }
            }

            ExitDef start = null;
            foreach (var exit in layout.Exits)
            {
                var color = exit.StartsLocked ? new Color(0.3f, 0.3f, 0.3f) : new Color(0.35f, 0.6f, 0.38f);
                var view = Prop(_corridors[exit.Floor], "Exit", exit.StartsLocked ? "prop_exit_locked" : "prop_exit", exit.X, new Vector2(1.6f, 3f), color, 0);
                AddTarget(new ExplorationTarget { Kind = TargetKind.Exit, Area = _corridors[exit.Floor], X = exit.X, Exit = exit, Locked = exit.StartsLocked, View = view });
                if (exit.Kind == ExitKind.Main && start == null)
                {
                    start = exit;
                }
            }

            CurrentArea = _corridors[start?.Floor ?? 0];
            X = start?.X ?? 1f;

            _playerSprites = ExplorationArt.Get("player_idle") != null;
            _player = _playerSprites
                ? ExplorationArt.Place(_world, "Player", "player_idle", Vector2.zero, 10, Vector2.zero, Color.white).transform
                : BuildingView.Rect(_world, "Player", Vector2.zero, new Vector2(PlayerWidth, PlayerHeight), new Color(0.88f, 0.84f, 0.76f), 10).transform;
            _playerRenderer = _player.GetComponent<SpriteRenderer>();
            // 바라보는 방향 표시 (부모 스케일 기준 0~1 좌표)
            if (!_playerSprites)
            {
                BuildingView.Rect(_player, "Facing", new Vector2(0.65f, 0.78f), new Vector2(0.35f, 0.05f), new Color(0.2f, 0.2f, 0.2f), 11);
            }
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
            foreach (var doorX in room.Template.DoorXs)
            {
                var exitView = Prop(room, "RoomExit", "door_closed", doorX, new Vector2(1.4f, 2.8f), BuildingView.DoorColor, 0);
                AddTarget(new ExplorationTarget { Kind = TargetKind.RoomExit, Area = room, X = doorX, Room = room, LinkX = room.SpanX + doorX, View = exitView });
            }

            foreach (var encounter in ActiveEncounters())
            {
                if (encounter.RoomId == room.Door.RoomId)
                {
                    CreateEncounter(room, encounter);
                }
            }

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

        /// <summary>플레이어 애니메이션 프레임 키 (걷기 4프레임, 달리기, 웅크리기, 공격).</summary>
        private string PlayerFrame(float dt)
        {
            if (dt < 100f)
            {
                _animTime += dt;
                _attackAnim = Mathf.Max(0f, _attackAnim - dt);
            }

            if (_attackAnim > 0f) return "player_attack";
            if (_crouching || Hidden) return "player_crouch";
            if (!_animMoving) return "player_idle";
            var frame = (int)(_animTime * (_animRunning ? 12f : 7f)) % 4;
            return (_animRunning ? "player_run_" : "player_walk_") + frame;
        }

        /// <summary>스프라이트 소품 (피벗 = 바닥 가운데). 없으면 기존 사각형.</summary>
        private GameObject Prop(ExplorationArea area, string name, string key, float centerX, Vector2 fallbackSize, Color fallbackColor, int order)
        {
            var parent = area.Root != null ? area.Root.transform : _world;
            var origin = area.Root != null ? Vector2.zero : area.Origin;
            return ExplorationArt.Place(parent, name, key, origin + new Vector2(centerX, 0.15f), order, fallbackSize, fallbackColor);
        }

        private string ContainerSprite(ContainerKind kind)
        {
            var store = Location.Id.Contains("STORE");
            var pharmacy = Location.Id.Contains("PHARMACY");
            switch (kind)
            {
                case ContainerKind.Locker: return "prop_locker";
                case ContainerKind.Desk: return store || pharmacy ? "office_desk" : "prop_desk";
                case ContainerKind.Cabinet: return pharmacy ? "prop_medcabinet" : "prop_cabinet";
                case ContainerKind.Fridge: return "prop_fridge";
                default: return store ? "prop_shelf" : "prop_box";
            }
        }

        /// <summary>이번 시간대 변형에서 출정 플래그 조건이 맞는 조우.</summary>
        private IEnumerable<EncounterDef> ActiveEncounters()
        {
            var variant = Request.TimeOfDay == ExpeditionTime.Day ? Location.DayVariant : Location.NightVariant;
            var flags = Request.Flags ?? new HashSet<string>();
            foreach (var encounter in variant.Encounters)
            {
                if (encounter.FlagsAll.All(flags.Contains) && !encounter.FlagsNone.Any(flags.Contains))
                {
                    yield return encounter;
                }
            }
        }

        private void CreateEncounter(ExplorationArea area, EncounterDef encounter)
        {
            var key = encounter.Id.Contains("NOTE") || encounter.Id.Contains("NOTICE") ? "prop_paper" : "person_student_crouch";
            var view = Prop(area, "Encounter_" + encounter.Id, key, encounter.X, new Vector2(0.6f, 1.5f), new Color(0.55f, 0.62f, 0.75f), 9);
            AddTarget(new ExplorationTarget { Kind = TargetKind.Encounter, Area = area, X = encounter.X, Encounter = encounter, View = view });
        }

        private ExplorationTarget CreateContainer(ExplorationArea area, ContainerDef container)
        {
            var size = ContainerSize(container.Kind);
            var view = Prop(area, "Container_" + container.Id, ContainerSprite(container.Kind), container.X, size, new Color(0.55f, 0.45f, 0.3f), 1);
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

            var fade = BuildingView.Rect(go.transform, "ScreenFade", new Vector2(-100f, -100f), new Vector2(200f, 200f), new Color(0f, 0f, 0f, 0f), 1000);
            fade.transform.localPosition = new Vector3(-100f, -100f, 1f);
            _fadeOverlay = fade.GetComponent<SpriteRenderer>();
            _shownArea = CurrentArea;
            SnapCamera();
        }

        private void SetupLight(bool day)
        {
            var go = new GameObject("GlobalLight2D");
            go.transform.SetParent(transform, false);
            _light = go.AddComponent<Light2D>();
            _light.lightType = Light2D.LightType.Global;
            SetAmbient(day ? 1f : _balance.NightAmbientIntensity, day ? new Color(1f, 0.97f, 0.9f) : new Color(0.6f, 0.7f, 1f));

            // 손전등: 바라보는 방향의 원뿔 빛 (밤에만 의미)
            var flash = new GameObject("Flashlight");
            flash.transform.SetParent(_world, false);
            _flashlight = flash.AddComponent<Light2D>();
            _flashlight.lightType = Light2D.LightType.Point;
            _flashlight.pointLightInnerRadius = 1f;
            _flashlight.pointLightOuterRadius = 10f;
            _flashlight.pointLightInnerAngle = 40f;
            _flashlight.pointLightOuterAngle = 70f;
            _flashlight.intensity = 1.2f;
            _flashlight.color = new Color(1f, 0.95f, 0.8f);

            // 어둠 속에서도 자기 주변은 희미하게 보이게
            var glow = new GameObject("SelfGlow");
            glow.transform.SetParent(_world, false);
            _selfLight = glow.AddComponent<Light2D>();
            _selfLight.lightType = Light2D.LightType.Point;
            _selfLight.pointLightInnerRadius = 0.3f;
            _selfLight.pointLightOuterRadius = _balance.NightViewDistance;
            _selfLight.intensity = 0.55f;
            _selfLight.color = new Color(0.8f, 0.85f, 1f);
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
            _shownArea = PeekTarget != null ? PeekArea : CurrentArea;
            ScreenFade = 0f;
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
            if (_playerSprites)
            {
                _player.localPosition = new Vector3(worldX, origin.y + 0.15f, 0f);
                _playerRenderer.flipX = !_facingRight;
                _playerRenderer.sprite = ExplorationArt.Get(PlayerFrame(dt));
                _playerRenderer.color = Hidden ? new Color(1f, 1f, 1f, 0.3f) : Color.white;
            }
            else
            {
                _player.localPosition = new Vector3(worldX + (_facingRight ? -PlayerWidth / 2f : PlayerWidth / 2f), origin.y + 0.15f, 0f);
                _player.localScale = new Vector3(_facingRight ? PlayerWidth : -PlayerWidth, height, 1f);
                _playerRenderer.color = Hidden ? new Color(0.88f, 0.84f, 0.76f, 0.3f) : new Color(0.88f, 0.84f, 0.76f);
            }

            var center = new Vector3(worldX, origin.y + 1.4f, 0f);
            if (_flashlight != null)
            {
                _flashlight.enabled = IsDark && FlashlightOn && !Hidden;
                _flashlight.transform.localPosition = center;
                _flashlight.transform.localRotation = Quaternion.Euler(0f, 0f, _facingRight ? -90f : 90f);
            }

            if (_selfLight != null)
            {
                _selfLight.enabled = IsDark;
                _selfLight.transform.localPosition = center;
            }

            // 화면 비율과 관계없이 가로 최소 MinVisibleWidth 유닛이 보이게 (멀리서 건물과 캐릭터를 함께 잡는 앵글)
            _camera.orthographicSize = Mathf.Clamp(MinVisibleWidth / (2f * Mathf.Max(0.1f, _camera.aspect)), MinOrthographicSize, MaxOrthographicSize);
            // 방이 화면에 다 들어오면 방 전체를 가운데에, 아니면 플레이어를 따라가되 공간 끝 너머 빈 곳은 덜 보이게.
            // 문틈으로 살피는 동안에는 문 너머 공간을 비춘다.
            var wantedArea = PeekTarget != null ? PeekArea : CurrentArea;
            var cut = false;
            if (_shownArea != wantedArea)
            {
                // 다른 공간으로 넘어갈 때는 카메라를 끌고 가지 않고 어둡게 → 잘라 넘기기 → 밝게
                ScreenFade = Mathf.Min(1f, ScreenFade + dt / FadeOutSeconds);
                if (ScreenFade >= 1f)
                {
                    _shownArea = wantedArea;
                    cut = true;
                }
            }
            else if (ScreenFade > 0f)
            {
                ScreenFade = Mathf.Max(0f, ScreenFade - dt / FadeInSeconds);
            }

            if (_fadeOverlay != null)
            {
                _fadeOverlay.color = new Color(0f, 0f, 0f, ScreenFade);
            }

            var viewArea = _shownArea ?? wantedArea;
            var viewOrigin = viewArea.Origin;
            var focusLocal = viewArea == PeekArea && PeekTarget != null ? PeekTarget.LinkX
                : viewArea == CurrentArea ? X
                : (_camera.transform.position.x - viewOrigin.x);
            var focusX = viewOrigin.x + focusLocal;
            var halfWidth = _camera.orthographicSize * _camera.aspect;
            var width = viewArea.Width;
            var cameraX = width <= halfWidth * 2f
                ? viewOrigin.x + width / 2f
                : Mathf.Clamp(focusX, viewOrigin.x + halfWidth - 2f, viewOrigin.x + width - halfWidth + 2f);
            var target = new Vector3(cameraX, viewOrigin.y + _building.FloorHeight * 0.5f, -10f);
            var cameraTransform = _camera.transform;
            cameraTransform.position = cut || dt >= 1000f ? target : Vector3.Lerp(cameraTransform.position, target, Mathf.Clamp01(dt * 6f));
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
                foreach (var doorX in room.Template.DoorXs)
                {
                    _threat.AddLink(corridorId, room.SpanX + doorX, roomId, doorX);
                    _threat.AddLink(roomId, doorX, corridorId, room.SpanX + doorX);
                }
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
                    var kind = ExplorationArt.ZombieKind(enemy.Id);
                    var body = ExplorationArt.Get($"zombie_{kind}_walk_0") != null
                        ? ExplorationArt.Place(_world, "Zombie_" + zombie.Id, $"zombie_{kind}_walk_0", Vector2.zero, 6, Vector2.zero, Color.white)
                        : BuildingView.Rect(_world, "Zombie_" + zombie.Id, Vector2.zero, Vector2.one, new Color(0.42f, 0.5f, 0.4f), 6);
                    var marker = BuildingView.Rect(_world, "Awareness_" + zombie.Id, Vector2.zero, Vector2.one, Color.yellow, 12);
                    _zombieViews.Add(new ZombieView
                    {
                        Unit = zombie, Body = body.transform, BodyRenderer = body.GetComponent<SpriteRenderer>(), Marker = marker.transform,
                        MarkerRenderer = marker.GetComponent<SpriteRenderer>(), Kind = kind, Sprites = ExplorationArt.Get($"zombie_{kind}_walk_0") != null,
                        LastX = zombie.X,
                    });
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
            if (events.Damage > 0 && !Invincible)
            {
                var damage = Mathf.Max(1, Mathf.RoundToInt(events.Damage * (1f - Mathf.Clamp01(_armor?.DamageReduction ?? 0f))));
                _log.DamageTaken += damage;
                Hp = Math.Max(0, Hp - damage);
                _hud?.ShowMessage("exp.hurt");
                if (Hp <= 0)
                {
                    Finish(ExtractStatus.Down);
                    return;
                }
            }

            _log.CrowdsWoken += events.WokenCrowds.Count;
            if (events.Spotted)
            {
                _log.TimesSpotted++;
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
            SetAmbient(0.2f, new Color(0.75f, 0.6f, 0.65f));
            _hud?.ShowMessage("exp.dusk");
        }

        /// <summary>
        /// 문 앞 감각: W(누르고 있기) 문틈으로 살피기, S(누르고 있기) 귀 기울이기. 처리 중이면 true (이동하지 않음).
        /// </summary>
        private bool HandleDoorSenses(Keyboard keyboard, float dt)
        {
            var door = Nearest != null && Nearest.IsDoorway ? Nearest : null;
            if (door != null && keyboard.wKey.isPressed)
            {
                if (door.Kind == TargetKind.Door)
                {
                    EnsureRoomBuilt(door.Room);
                }

                PeekTarget = door;
                ListenProgress = 0f;
                if (_threat.ExposeAt(AreaId(PeekArea), door.LinkX, dt, Activity))
                {
                    _hud?.ShowMessage("exp.peek_noticed");
                }

                return true;
            }

            PeekTarget = null;
            if (door != null && keyboard.sKey.isPressed)
            {
                var before = ListenProgress;
                ListenProgress = Mathf.Min(1f, ListenProgress + dt / Mathf.Max(0.1f, _balance.ListenSeconds));
                if (before < 1f && ListenProgress >= 1f)
                {
                    ReportListen(door);
                }

                return true;
            }

            ListenProgress = 0f;
            return false;
        }

        private void ReportListen(ExplorationTarget door)
        {
            var other = door.Kind == TargetKind.Door ? door.Room : _corridors[door.Room.Floor];
            var heard = _threat.Listen(AreaId(other), door.LinkX);
            string key;
            if (heard.AwakeCount > 0)
            {
                key = heard.NearestAwake <= 4f ? "exp.listen_steps_near" : "exp.listen_steps";
            }
            else
            {
                key = heard.SleepingNearby > 0 ? "exp.listen_breath" : "exp.listen_quiet";
            }

            var count = Loc.T(heard.AwakeCount > 1 ? "exp.listen_many" : "exp.listen_one");
            _hud?.ShowText(Loc.F(key, count));
        }

        /// <summary>벽 너머로 전해지는 소음: 방 ↔ 복도 (반경 감쇠).</summary>
        private void EmitThroughWalls(ExplorationArea area, float x, float radius)
        {
            var muffled = radius * _balance.WallNoiseFactor;
            if (muffled < 0.5f)
            {
                return;
            }

            if (area.IsRoom)
            {
                _threat.Emit(new NoiseEvent(AreaId(_corridors[area.Floor]), area.SpanX + x, muffled));
                return;
            }

            foreach (var room in _rooms)
            {
                if (room.Floor != area.Floor || x < room.SpanX - muffled || x > room.SpanX + room.Width + muffled)
                {
                    continue;
                }

                var local = Mathf.Clamp(x - room.SpanX, 0f, room.Width);
                _threat.Emit(new NoiseEvent(AreaId(room), local, muffled));
            }
        }

        /// <summary>
        /// 창문 너머 실루엣: 방 안에서는 창밖 복도의 좀비, 복도에서는 교실 창 안쪽의 좀비를 어두운 형체로 그린다.
        /// 밤에는 플레이어 시야 규칙(거리·손전등)을 창 너머 위치에 그대로 적용한다.
        /// </summary>
        private void UpdateWindowSilhouettes()
        {
            var used = 0;
            if (PeekTarget == null)
            {
                if (CurrentArea.IsRoom)
                {
                    var corridorId = AreaId(_corridors[CurrentArea.Floor]);
                    var playerCorridorX = CurrentArea.SpanX + X;
                    foreach (var window in CurrentArea.Template.Windows)
                    {
                        var from = CurrentArea.SpanX + window.X;
                        var to = from + window.Width;
                        foreach (var unit in _threat.Zombies)
                        {
                            if (unit.AreaId == corridorId && unit.X >= from && unit.X <= to && SeesThroughWindow(corridorId, playerCorridorX, unit))
                            {
                                PlaceSilhouette(used++, CurrentArea, unit.X - CurrentArea.SpanX, unit.State == ZombieState.Dormant);
                            }
                        }
                    }
                }
                else
                {
                    foreach (var room in _rooms)
                    {
                        if (room.Floor != CurrentArea.Floor)
                        {
                            continue;
                        }

                        var roomId = AreaId(room);
                        var playerLocalX = X - room.SpanX;
                        foreach (var window in room.Template.Windows)
                        {
                            foreach (var unit in _threat.Zombies)
                            {
                                if (unit.AreaId == roomId && unit.X >= window.X && unit.X <= window.X + window.Width
                                    && SeesThroughWindow(roomId, playerLocalX, unit))
                                {
                                    PlaceSilhouette(used++, CurrentArea, room.SpanX + unit.X, unit.State == ZombieState.Dormant);
                                }
                            }
                        }
                    }
                }
            }

            for (var i = 0; i < _silhouettes.Count; i++)
            {
                _silhouettes[i].SetActive(i < used);
            }
        }

        private bool SeesThroughWindow(int areaId, float mappedPlayerX, ZombieUnit unit)
        {
            return FullVision || PlayerViewRules.CanPlayerSee(areaId, mappedPlayerX, _facingRight ? 1 : -1, IsDark, FlashlightOn && !Hidden, areaId, unit.X, _balance);
        }

        private void PlaceSilhouette(int index, ExplorationArea area, float x, bool dormant)
        {
            while (_silhouettes.Count <= index)
            {
                var sil = ExplorationArt.Get("silhouette") != null
                    ? ExplorationArt.Place(_world, "Silhouette", "silhouette", Vector2.zero, -3, Vector2.zero, Color.clear)
                    : BuildingView.Rect(_world, "Silhouette", Vector2.zero, Vector2.one, BuildingView.SilhouetteColor, -3);
                sil.GetComponent<SpriteRenderer>().color = BuildingView.SilhouetteColor;
                _silhouettes.Add(sil);
            }

            // 창틀 안에 머리·어깨만 보이게 (잠든 좀비는 조금 낮게)
            var origin = area.Origin;
            var height = dormant ? 0.75f : 1.05f;
            var shape = _silhouettes[index].transform;
            if (ExplorationArt.Get("silhouette") != null)
            {
                shape.localPosition = new Vector3(origin.x + x, origin.y + BuildingView.WindowBottom + (dormant ? -0.25f : 0f), 0f);
                return;
            }

            shape.localPosition = new Vector3(origin.x + x - 0.27f, origin.y + BuildingView.WindowBottom, 0f);
            shape.localScale = new Vector3(0.55f, height, 1f);
        }

        private void TriggerDawn()
        {
            DawnReturn = true;
            SetAmbient(_balance.DawnAmbientIntensity, new Color(0.85f, 0.8f, 0.9f));
            _hud?.ShowMessage("exp.dawn");
        }

        private void SetAmbient(float intensity, Color color)
        {
            _ambientIntensity = intensity;
            if (_light != null)
            {
                _light.intensity = FullVision ? 1f : intensity;
                _light.color = color;
            }
        }

        private void HandleDebugKeys(Keyboard keyboard)
        {
            if (keyboard.f3Key.wasPressedThisFrame)
            {
                DebugOverlay = !DebugOverlay;
            }

            if (keyboard.f4Key.wasPressedThisFrame)
            {
                Invincible = !Invincible;
                _log.DebugUsed = true;
            }

            if (keyboard.f5Key.wasPressedThisFrame)
            {
                FullVision = !FullVision;
                _light.intensity = FullVision ? 1f : _ambientIntensity;
                _log.DebugUsed = true;
            }

            if (keyboard.f6Key.wasPressedThisFrame)
            {
                DebugGiveItem();
            }

            if (keyboard.f7Key.wasPressedThisFrame)
            {
                Clock.Tick(60f);
                _log.DebugUsed = true;
            }

            if (keyboard.f8Key.wasPressedThisFrame)
            {
                Clock.Tick(Clock.Length);
                _log.DebugUsed = true;
            }
        }

        /// <summary>디버그 (F6): 무작위 아이템 하나를 배낭에.</summary>
        public void DebugGiveItem()
        {
            _log.DebugUsed = true;
            if (!_db.TryGetLootTable(Location.DefaultLootTableId, out var table) || table.Entries.Count == 0)
            {
                return;
            }

            var entry = table.Entries[UnityEngine.Random.Range(0, table.Entries.Count)];
            if (_db.TryGetItem(entry.ItemId, out var item))
            {
                Backpack.TryAutoPlace(item.Id, item.Width, item.Height, item.Weight);
            }
        }

        /// <summary>false면 플레이 기록을 남기지 않는다 (자동 테스트가 보정 데이터를 오염시키지 않게).</summary>
        public static bool PlayLogEnabled { get; set; } = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            PlayLogEnabled = true;
        }

        private void WritePlayLog(ExtractStatus status)
        {
            if (!PlayLogEnabled)
            {
                return;
            }

            try
            {
                _log.Timestamp = DateTime.Now.ToString("o");
                _log.LocationId = Request.LocationId;
                _log.TimeOfDay = Request.TimeOfDay.ToString();
                _log.Seed = Request.Seed;
                _log.Status = status.ToString();
                _log.TimeSpent = Clock.Elapsed;
                _log.HpDelta = Result.HpDelta;
                _log.DuskAwakened = DuskAwakened;
                _log.DawnReturn = DawnReturn;
                _log.Resources = Result.ResourceLoot;
                _log.Items.Clear();
                foreach (var item in Result.Loot)
                {
                    _log.Items.Add(item.ItemId + "x" + item.Count);
                }

                var directory = Path.Combine(Application.persistentDataPath, "PlayLogs");
                Directory.CreateDirectory(directory);
                File.AppendAllText(Path.Combine(directory, "expeditions.jsonl"), JsonUtility.ToJson(_log) + "\n");
            }
            catch (Exception exception)
            {
                Debug.LogWarning("플레이 기록 저장 실패: " + exception.Message);
            }
        }

        private void EmitNoise(ExplorationArea area, float x, float radius)
        {
            if (_threat == null || radius <= 0f)
            {
                return;
            }

            radius *= _noiseMultiplier;
            _threat.Emit(new NoiseEvent(AreaId(area), x, radius));
            EmitThroughWalls(area, x, radius);
            if (DebugOverlay)
            {
                var origin = area.Origin;
                var ring = BuildingView.Rect(_world, "NoiseDebug", new Vector2(origin.x + x - radius, origin.y + 0.2f),
                    new Vector2(radius * 2f, 0.12f), new Color(1f, 0.95f, 0.4f, 0.6f), 20);
                Destroy(ring, 0.6f);
            }

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
                var visible = IsVisibleToPlayer(unit);
                view.Body.gameObject.SetActive(visible);
                if (!visible)
                {
                    view.Marker.gameObject.SetActive(false);
                    continue;
                }

                var area = _areasById[unit.AreaId];
                var origin = area.Root != null ? (Vector2)area.Root.transform.localPosition : area.Origin;
                var dormant = unit.State == ZombieState.Dormant;

                // 휴면도 선 채로 (고개 숙인 듯 살짝 낮게), 머리 위 Zzz는 HUD가 그린다
                var shade = 1f + (unit.Id % 3 - 1) * 0.08f;
                if (view.Sprites)
                {
                    var moved = Mathf.Abs(unit.X - view.LastX) > 0.0005f;
                    if (moved)
                    {
                        view.MoveTime += Time.deltaTime;
                        view.BodyRenderer.flipX = unit.X < view.LastX;
                    }

                    view.LastX = unit.X;
                    var phase = (int)((dormant ? Time.time * 0.8f : view.MoveTime * (unit.State == ZombieState.Chase ? 7f : 4f)) + unit.Id) % 2;
                    var pose = dormant ? "dormant_" + phase
                        : unit.State == ZombieState.Stunned ? "stun"
                        : unit.State == ZombieState.Chase ? "chase_" + phase
                        : "walk_" + (moved ? phase : 0);
                    view.BodyRenderer.sprite = ExplorationArt.Get($"zombie_{view.Kind}_{pose}");
                    view.Body.localPosition = new Vector3(origin.x + unit.X, origin.y + 0.15f, 0f);
                    view.BodyRenderer.color = unit.State == ZombieState.Stunned ? new Color(0.75f, 0.8f, 1f) : new Color(shade, shade, shade);
                    view.Body.localScale = Vector3.one;
                    goto Marker;
                }

                var size = dormant ? new Vector2(0.55f, 1.6f) : new Vector2(0.6f, 1.75f);
                view.Body.localPosition = new Vector3(origin.x + unit.X - size.x / 2f, origin.y + 0.15f, 0f);
                view.Body.localScale = new Vector3(size.x, size.y, 1f);
                var color = dormant ? new Color(0.34f, 0.4f, 0.34f)
                    : unit.State == ZombieState.Stunned ? new Color(0.35f, 0.4f, 0.55f)
                    : unit.State == ZombieState.Chase ? new Color(0.6f, 0.38f, 0.33f)
                    : new Color(0.42f, 0.5f, 0.4f);
                view.BodyRenderer.color = new Color(color.r * shade, color.g * shade, color.b * shade);

                Marker:
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
            public string Kind = "wanderer";
            public bool Sprites;
            public float LastX;
            public float MoveTime;
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
