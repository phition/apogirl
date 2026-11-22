using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Random;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;
using ApoGirl.Data;
using ApoGirl.Exploration.Model;
using ApoGirl.Presentation.App;
using ApoGirl.Presentation.Dialogue;
using ApoGirl.Presentation.Screens;
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

        /// <summary>탐험 씬을 바로 실행할 때 손에 들 도구 (예: ITEM_TOOL_FLASHLIGHT, 비우면 맨손).</summary>
        [SerializeField] private string _debugTool = "";

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

        /// <summary>그 자리 대화 (동료가 될 인물): 탐험 HUD 위 대화 창과, 치른 이벤트.</summary>
        private FieldDialogue _fieldDialogue;
        private readonly List<FieldEventChoice> _fieldEvents = new List<FieldEventChoice>();

        /// <summary>조우 대화가 진행 중 (세계는 멈춰 있고 작은 선택지 창은 숨긴다).</summary>
        public bool InFieldDialogue { get; private set; }

        // 적대적 생존자에게 붙잡힘 → 암전 후 귀환 (문서 13장 4항: 암전·짧은 문장 연출)
        private const float ConfrontSeconds = 2.2f;
        private HostileOutcome _hostileOutcome;
        private float _confrontTimer = -1f;

        // 상자 고갈 지역(교실동)에서 이번에 뒤진 상자
        private readonly List<string> _searchedKeys = new List<string>();
        private readonly List<string> _runClues = new List<string>();
        private float _moveMultiplier = 1f;
        private bool _playerSprites;
        private bool _animMoving;
        private bool _animRunning;
        private float _animTime;
        private float _attackAnim;
        private float _shoveAnim;

        /// <summary>무기 휘두르기·맨손 밀치기 동작 길이 (픽셀 아트 프레임을 이 시간에 나눠 보여 준다).</summary>
        private const float AttackAnimSeconds = 0.5f;
        private const float ShoveAnimSeconds = 0.5f;

        /// <summary>맞거나 밀린 좀비의 반응 동작 길이 (초).</summary>
        private const float ReactAnimSeconds = 0.6f;

        /// <summary>픽셀 아트 동작 빠르기 (초당 프레임). 코드 그림은 예전 빠르기를 쓴다.</summary>
        private const float PixelIdleFps = 7f;
        private const float PixelWalkFps = 18f;
        private const float PixelRunFps = 26f;
        private const float PixelSneakFps = 12f;
        private const float PixelZombieWalkFps = 11f;
        private const float PixelZombieChaseFps = 20f;
        private const float PixelZombieIdleFps = 6f;

        /// <summary>주인공 그림 접두어 (옷·무기에 따라, 예: player_padding_bat_)와 지금 프레임 키, 등에 멘 가방 그림.</summary>
        private string _lookPrefix = "player_";
        private string _playerFrameKey = "";
        private SpriteRenderer _bagRenderer;
        private float _noiseMultiplier = 1f;

        /// <summary>계절 재해 (확장 6): 지금 일어난 재해와 물에 잠긴 층 (없으면 null·−1).</summary>
        private DisasterDef _disaster;
        private int _floodedFloor = -1;
        private bool _floodNoticed;
        private bool _disasterChanged;

        /// <summary>후처리 색 보정·블룸·빛 기둥 (Resources/Art/exploration_look.json, F9 비교).</summary>
        private readonly ExplorationLook _look = new ExplorationLook();
        private float _tutorialMoved;
        private float _tutorialCrouched;
        private bool _tutorialSearched;
        private bool _tutorialHidden;
        private bool _tutorialOpenedBag;
        private float _searchNoiseTimer;

        /// <summary>이번 프레임에 수색 중인가 (수색 소리는 이 동안만 반복해서 울린다).</summary>
        private bool _searching;
        private float _shoveTimer;
        private bool _crouching;
        private Light2D _flashlight;
        private Light2D _selfLight;
        private float _ambientIntensity;

        /// <summary>공간별 조명 (낮 복도 햇빛·어두운 방, M6-5)과 보이는 공간에 따른 밝기 배율 (부드럽게 바뀐다).</summary>
        private ExplorationLighting _lighting;
        private float _areaFactor = 1f;
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

        // ── 스태미너 (달리기·싸움) ──
        /// <summary>체력 능력치로 정해지는 최대치.</summary>
        public float StaminaMax { get; private set; } = 100f;

        /// <summary>이번 탐험에서 쓸 수 있는 최대치 (피로·부상으로 깎인 만큼은 게이지에 붉게).</summary>
        public float StaminaCap { get; private set; } = 100f;
        public float Stamina { get; private set; } = 100f;

        /// <summary>바닥나서 숨이 찬 상태: StaminaRecoverToRun까지 달릴 수 없다.</summary>
        public bool Exhausted { get; private set; }

        private float _staminaRestTimer;
        private bool _ranThisFrame;

        // ── 숨기 (누르고 있기)·숨다가 잠들기 ──
        /// <summary>숨는 중 진행도 0~1 (Space를 누르고 있는 동안).</summary>
        public float HideProgress { get; private set; }

        /// <summary>숨어 있다가 잠들었다: 시계가 HideSleepClockMultiplier배로 흐르고, Space 연타로 깬다.</summary>
        public bool Asleep { get; private set; }

        /// <summary>깨어나기 진행도 0~1.</summary>
        public float WakeProgress { get; private set; }

        private float _hideSeconds = 1.5f;
        private bool _hideArmed;
        private float _stressSearchFactor = 1f;
        private System.Random _sleepRandom = new System.Random(0);

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

        /// <summary>그 공간에서 눈이 어둠 규칙을 따르는가: 밤·황혼이면 어디든, 낮에도 방 안은 전기가 끊겨 기본이 어둡다.</summary>
        public bool IsDarkIn(ExplorationArea area) => IsDark || (area != null && (area.IsRoom || area.IsDeep));

        /// <summary>지금 있는 곳이 어두운가 (손전등이 켜지고 HUD에 손전등 상태가 보인다).</summary>
        public bool DarkHere => IsDarkIn(CurrentArea);

        /// <summary>문틈 너머가 너무 어두워 아무것도 보이지 않는다 (밤): 귀 기울이기(S)로 알아낸다.</summary>
        public bool PeekTooDark => PeekTarget != null && PeekViewRange <= 0f;

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
                return unit.AreaId == AreaId(PeekArea) && PeekViewRange > 0f && Mathf.Abs(unit.X - PeekTarget.LinkX) <= PeekViewRange;
            }

            return PlayerViewRules.CanPlayerSee(
                AreaId(CurrentArea), X, _facingRight ? 1 : -1, DarkHere, FlashlightOn && !Hidden,
                unit.AreaId, unit.X, _balance);
        }

        /// <summary>문틈으로 살피는 중인 문 (W를 누르고 있는 동안).</summary>
        public ExplorationTarget PeekTarget { get; private set; }

        /// <summary>귀 기울이는 진행도 0~1 (S).</summary>
        public float ListenProgress { get; private set; }

        /// <summary>문틈 너머 공간.</summary>
        public ExplorationArea PeekArea => PeekTarget == null ? null
            : PeekTarget.Kind == TargetKind.Door ? PeekTarget.Room : _corridors[PeekTarget.Room.Floor];

        /// <summary>
        /// 문틈으로 살필 때 보이는 거리 (문에서부터): 밤에는 0 (아무것도 안 보인다 — 소리로), 낮 방 안은 어두워 문 근처만, 낮 복도는 PeekRange.
        /// </summary>
        public float PeekViewRange => PeekTarget == null || IsDark ? 0f : PeekArea.IsRoom ? _balance.RoomPeekRange : _balance.PeekRange;

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
                    return Loc.F("exp.peeking_into", other.IsRoom || other.IsOutdoor || other.IsRoof || other.IsDeep ? other.Label : Loc.F("exp.corridor", other.Floor + 1));
                }

                if (InRoom || CurrentArea.IsOutdoor || CurrentArea.IsRoof || CurrentArea.IsDeep)
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
            ExplorationLook.Reload();
            Request = GameApp.ActiveExpedition ?? CreateDebugRequest(session);
            _standalone = GameApp.ActiveExpedition == null;

            _disaster = DisasterRules.Active(Request.Week, Request.WeatherId, _balance);
            var plain = _db.GetLocation(Request.LocationId);
            Location = DisasterRules.Apply(plain, _disaster);   // 재해가 바꾼 지도 (사본)
            _disasterChanged = !ReferenceEquals(Location, plain);
            _floodedFloor = DisasterRules.FloodedFloor(Location.Layout, _disaster);
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
            SetupBody();
            Backpack = _backpackItem != null && _backpackItem.GridWidth > 0
                ? new BackpackGrid(_backpackItem.GridWidth, _backpackItem.GridHeight)
                : new BackpackGrid(_balance.PocketWidth, _balance.PocketHeight);
            CarryCapacity = ExplorationRules.CarryCapacity(Request.Stats, _balance);

            BuildWorld();
            BuildFlood();
            Hp = _startHp = Math.Max(1, Request.Hp);
            BuildThreats(day ? Location.DayVariant : Location.NightVariant);
            SetupCamera();
            SetupLight(day);
            EnsureEventSystem();

            var document = gameObject.AddComponent<UIDocument>();
            document.panelSettings = _panelSettings;
            document.sortingOrder = 10;
            document.visualTreeAsset = _hudLayout;
            if (ControlPrefs.PadVisible)
            {
                // 휴대폰: 가상 패드 + 패드용 안내 문구 ("키.pad")
                Loc.Variant = "pad";
                _pad = new VirtualPad(document.rootVisualElement, VirtualPad.LoadConfig());
            }

            _hud = new ExpeditionHud(document.rootVisualElement, this);
            _fieldDialogue = new FieldDialogue(document.rootVisualElement);
            UpdateZombieViews();

            // 물자가 다시 채워진 뒤 첫 방문: 누군가 머물다 간 흔적
            if (Request.Restocked)
            {
                _hud.ShowText(Loc.T("exp.restocked." + Location.Id), 4500);
            }
            else if (_disasterChanged)
            {
                _hud.ShowText(Loc.T("exp.disaster." + _disaster.Id), 4500);   // 장마·폭설로 지도가 바뀌었다
            }
        }

        /// <summary>장마: 물에 잠긴 층의 복도·방 바닥에 물을 깐다.</summary>
        private void BuildFlood()
        {
            if (_floodedFloor < 0)
            {
                return;
            }

            foreach (var area in _areasById.Where(a => a.Floor == _floodedFloor))
            {
                var parent = area.Root != null ? area.Root.transform : _world;
                var origin = area.Root != null ? Vector2.zero : area.Origin;
                BuildingView.Rect(parent, "FloodWater", origin, new Vector2(area.Width, 0.75f), new Color(0.2f, 0.32f, 0.42f, 0.55f), 6);
            }
        }

        private bool InFlood => _floodedFloor >= 0 && CurrentArea != null && CurrentArea.Floor == _floodedFloor;

        private void Update()
        {
            _searching = false;
            _ranThisFrame = false;
            if (IsFinished || _hud == null)
            {
                return;
            }

            // 키보드·마우스·가상 패드를 한 번에 읽는다 (패드의 눌림은 읽을 때 지워져, 대화 중 누른 것이 뒤에 새지 않는다)
            _input.Read(Keyboard.current, Mouse.current, MouseFree, _pad);

            if (OpenEncounter != null)
            {
                // 조우 대화: Space·Enter로 다음 줄 (세계는 멈춘 채)
                if (InFieldDialogue && _input.Advance)
                {
                    _fieldDialogue.Advance();
                }

                _hud.Refresh();
                return;
            }

            if (_confrontTimer >= 0f)
            {
                TickConfrontation(Time.deltaTime);
                return;
            }

            var dt = Time.deltaTime;
            // 숨다가 잠들면 시간이 더 빨리 흐른다
            Clock.Tick(dt * TimeScale * (Asleep ? _balance.HideSleepClockMultiplier : 1f));
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

            if (CurrentArea != null && CurrentArea.IsDeep)
            {
                _deepSeconds += dt;
            }

            HandleInput(dt);
            UpdateStamina(dt);
            StepThreats(dt);
            if (IsFinished)
            {
                return;
            }

            UpdateViews(dt);
            UpdateZombieViews();
            UpdateFog(dt);
            UpdateSounds();
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
                case TargetKind.Connector when target.Connector.Collapsed:
                    Sfx("sfx_door_locked");
                    _hud?.ShowMessage(string.IsNullOrEmpty(target.Connector.BlockedBy) ? "exp.stairs_collapsed" : "exp.blocked." + target.Connector.BlockedBy);
                    break;
                case TargetKind.Connector when target.Connector.Kind == ConnectorKind.OneWayBack:
                    Sfx("sfx_door_locked");
                    _hud?.ShowMessage("exp.oneway_back");
                    break;
                case TargetKind.Connector when target.Connector.Kind == ConnectorKind.OneWayExit:
                    if (_oneWayArmed != target || Time.time > _oneWayArmedUntil)
                    {
                        // 정말 나갈까? (나가면 다시 못 들어온다)
                        _oneWayArmed = target;
                        _oneWayArmedUntil = Time.time + _balance.OneWayConfirmSeconds;
                        _hud?.ShowMessage("exp.oneway_confirm");
                        break;
                    }

                    _oneWayArmed = null;
                    Sfx("sfx_door_open", 0.9f);
                    EmitNoise(CurrentArea, X, _balance.DoorNoise);
                    MoveTo(_corridors[target.Connector.TargetFloor], target.Connector.ArrivalX);
                    Sfx("sfx_oneway_lock", 0.9f);   // 철컥: 등 뒤에서 잠긴다
                    _hud?.ShowMessage("exp.oneway_shut");
                    break;
                case TargetKind.Connector when target.Connector.Kind == ConnectorKind.DeepDoor:
                    Sfx("sfx_deep_door_open", 0.8f);   // 무거운 쇳소리와 지하 바람
                    EmitNoise(CurrentArea, X, _balance.DoorNoise);
                    MoveTo(_corridors[target.Connector.TargetFloor], target.Connector.ArrivalX);
                    EmitNoise(CurrentArea, X, _balance.DoorNoise);
                    if (CurrentArea.IsDeep)
                    {
                        OnEnteredDeep();
                    }

                    break;
                case TargetKind.Passage:
                    Sfx("sfx_door_open", 0.8f);
                    EmitNoise(CurrentArea, X, _balance.DoorNoise);
                    MoveTo(target.LinkArea, target.LinkX);
                    EmitNoise(CurrentArea, X, _balance.DoorNoise);
                    break;
                case TargetKind.Connector:
                    Sfx("sfx_step_run");
                    EmitNoise(CurrentArea, X, _balance.StairsNoise);
                    MoveTo(_corridors[target.Connector.TargetFloor], target.X);
                    EmitNoise(CurrentArea, X, _balance.StairsNoise);
                    break;
                case TargetKind.Door when target.RoomLocked:
                    Sfx("sfx_door_locked");
                    _hud?.ShowMessage("exp.room_shuttered");
                    break;
                case TargetKind.Door when target.Forceable && !target.Forced:
                    if (_toolKind != ToolKind.Crowbar)
                    {
                        Sfx("sfx_door_locked");
                        _hud?.ShowMessage("exp.room_locked_need_crowbar");
                        break;
                    }

                    target.Forced = true;
                    foreach (var other in target.Room.CorridorDoors)
                    {
                        other.Forced = true;
                        var doorRenderer = other.View != null ? other.View.GetComponent<SpriteRenderer>() : null;
                        var openDoor = ExplorationArt.Get(ExplorationArt.Door(Location.Id, target.Room.Template?.Id, true));
                        if (doorRenderer != null && openDoor != null)
                        {
                            doorRenderer.sprite = openDoor;
                            doorRenderer.color = Color.white;
                        }
                    }

                    EmitNoise(CurrentArea, X, _balance.ShoveNoise);
                    Sfx("sfx_door_force");
                    _hud?.ShowMessage("exp.forced_open");
                    break;
                case TargetKind.Encounter:
                    OpenEncounter = target;
                    if (_fieldDialogue != null && _fieldDialogue.HasNode(target.Encounter.DialogueNode))
                    {
                        InFieldDialogue = true;
                        StartCoroutine(FieldDialogueRoutine(target));
                    }

                    break;
                case TargetKind.Door:
                    Sfx("sfx_door_open", 0.8f);
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
                    Sfx("sfx_door_open", 0.8f);
                    EmitNoise(CurrentArea, X, _balance.DoorNoise);
                    MoveTo(_corridors[target.Room.Floor], target.LinkX);
                    EmitNoise(CurrentArea, X, _balance.DoorNoise);
                    break;
                case TargetKind.Exit:
                    if (target.Locked && !string.IsNullOrEmpty(target.Exit?.BlockedBy))
                    {
                        Sfx("sfx_door_locked");
                        _hud?.ShowMessage("exp.exit_blocked." + target.Exit.BlockedBy);   // 눈에 파묻혀 쇠지렛대로도 안 된다
                    }
                    else if (target.Locked && _toolKind == ToolKind.Crowbar)
                    {
                        target.Locked = false;
                        var exitRenderer = target.View != null ? target.View.GetComponent<SpriteRenderer>() : null;
                        var openSprite = ExplorationArt.Get(ExplorationArt.Exit(Location.Id, false));
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
                        Sfx("sfx_door_force");
                        _hud?.ShowMessage("exp.forced_open");
                    }
                    else if (target.Locked)
                    {
                        Sfx("sfx_door_locked");
                        _hud?.ShowMessage("exp.exit_locked");
                    }
                    else if (target.LinkArea != null)
                    {
                        // 학교 밖 지역: 출구는 바깥 길(진입로·탈출로)로 이어지고, 길 끝에서 학교로 돌아간다
                        Sfx("sfx_door_open", 0.8f);
                        EmitNoise(CurrentArea, X, _balance.DoorNoise);
                        MoveTo(target.LinkArea, target.LinkX);
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
            Sfx("sfx_pickup", 0.8f);
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
                if (Asleep)
                {
                    return false; // 잠든 채로는 못 나온다 (Space 연타로 먼저 깨야)
                }

                Hidden = false;
                _hideArmed = false;
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
            _hideArmed = false;
            CloseContainer();
            return true;
        }

        /// <summary>짜증 섞인 한숨 (스트레스로 수색이 늘어질 때). 파일이 없으면 조용히 넘어간다.</summary>
        private const string SighSound = "sfx_sigh_irritated";

        /// <summary>체력 능력치·피로·부상으로 스태미너를, 민첩으로 숨는 시간을, 스트레스로 수색 배율을 정한다.</summary>
        private void SetupBody()
        {
            StaminaMax = ExplorationRules.StaminaMax(Request.Stats, _balance);
            var injured = !string.IsNullOrEmpty(_balance.InjuredFlag) && Request.Flags.Contains(_balance.InjuredFlag);
            StaminaCap = ExplorationRules.StaminaCap(StaminaMax, Request.Fatigue, injured, _balance);
            Stamina = StaminaCap;
            _hideSeconds = ExplorationRules.HideSeconds(Request.Stats, _balance);
            _stressSearchFactor = ExplorationRules.StressSearchFactor(Request.Stress, _balance);
            _sleepRandom = new System.Random(Request.Seed ^ 0x51EE9);
        }

        /// <summary>싸움 한 번의 스태미너. 모자라면 숨이 차 못 한다.</summary>
        private bool SpendStamina(float cost)
        {
            if (Invincible)
            {
                return true;
            }

            if (Stamina < cost)
            {
                _hud?.ShowMessage("exp.out_of_breath");
                return false;
            }

            Stamina -= cost;
            _staminaRestTimer = _balance.StaminaRegenDelay;
            return true;
        }

        /// <summary>매 프레임: 달리면 줄고, 쉬면(잠시 뒤부터) 이번 탐험 최대치까지 찬다.</summary>
        private void UpdateStamina(float dt)
        {
            if (_ranThisFrame && !Invincible)
            {
                Stamina = Mathf.Max(0f, Stamina - _balance.StaminaRunDrain * dt);
                _staminaRestTimer = _balance.StaminaRegenDelay;
                if (Stamina <= 0f && !Exhausted)
                {
                    Exhausted = true;
                    _hud?.ShowMessage("exp.out_of_breath");
                }

                return;
            }

            if (_staminaRestTimer > 0f)
            {
                _staminaRestTimer -= dt;
                return;
            }

            Stamina = Mathf.Min(StaminaCap, Stamina + _balance.StaminaRegen * dt);
            if (Exhausted && Stamina >= Mathf.Min(StaminaCap, _balance.StaminaRecoverToRun))
            {
                Exhausted = false;
            }
        }

        /// <summary>
        /// 숨을 곳(사물함·책상) 앞에서 Space를 누르고 있으면 HideSeconds에 걸쳐 숨는다. 새로 누른 뒤부터만 (나온 직후 계속 누르고 있어도 다시 숨지 않게).
        /// 처리 중이면 true (이동하지 않음).
        /// </summary>
        private bool HandleHideHold(float dt)
        {
            if (_input.HideDown)
            {
                _hideArmed = true;
            }

            var spot = Nearest;
            var canHide = spot != null && spot.Kind == TargetKind.Container
                          && (spot.Container.Kind == ContainerKind.Locker || spot.Container.Kind == ContainerKind.Desk);
            if (!canHide || !_input.HideHeld || !_hideArmed)
            {
                HideProgress = 0f;
                if (!_input.HideHeld)
                {
                    _hideArmed = false;
                }

                return false;
            }

            CloseContainer();
            HideProgress += dt / Mathf.Max(0.05f, _hideSeconds);
            if (HideProgress >= 1f)
            {
                HideProgress = 0f;
                ToggleHide();
                _hideArmed = false;
            }

            return true;
        }

        /// <summary>숨어 있는 동안: Space로 나오기. 수면 부채가 쌓였으면 잠들 수 있고, 잠들면 Space 연타로 깬다.</summary>
        private void HandleHidden(float dt)
        {
            HideProgress = 0f;
            if (Asleep)
            {
                WakeProgress = Mathf.Max(0f, WakeProgress - _balance.WakeDecay * dt);
                if (_input.HideDown)
                {
                    WakeProgress += _balance.WakePerPress;
                    if (WakeProgress >= 1f)
                    {
                        WakeUp();
                    }
                }

                return;
            }

            if (_input.HideDown)
            {
                ToggleHide();
                return;
            }

            var chance = ExplorationRules.HideSleepChancePerSecond(Request.SleepDebt, _balance) * dt;
            if (chance > 0f && _sleepRandom.NextDouble() < chance)
            {
                FallAsleep();
            }
        }

        private void FallAsleep()
        {
            Asleep = true;
            WakeProgress = 0f;
            _hud?.ShowMessage("exp.fell_asleep");
        }

        /// <summary>잠에서 깬다. 연타로 깨면 기본 문구, 물려서 깨면 그 문구.</summary>
        private void WakeUp(string messageKey = "exp.woke_up")
        {
            Asleep = false;
            WakeProgress = 0f;
            _hud?.ShowMessage(messageKey);
        }

        /// <summary>디버그·화면 확인: 가까운 숨을 곳에 바로 숨어 잠든다.</summary>
        public bool DebugHideAndSleep()
        {
            if (!Hidden && !ToggleHide())
            {
                return false;
            }

            FallAsleep();
            return true;
        }

        /// <summary>바라보는 방향의 좀비를 밀친다 (큰 소음).</summary>
        public bool Shove()
        {
            if (_shoveTimer > 0f || Hidden)
            {
                return false;
            }

            if (!SpendStamina(_balance.StaminaShoveCost))
            {
                return false;
            }

            _shoveTimer = _balance.ShoveCooldown;
            _shoveAnim = ShoveAnimSeconds;
            var shoved = _threat.Shove(Snapshot, _facingRight ? 1 : -1);
            Sfx("sfx_push");
            EmitNoise(CurrentArea, X, _balance.ShoveNoise);
            if (shoved != null)
            {
                React(shoved, "pushed_");
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
            Sfx("sfx_flashlight");
        }

        /// <summary>
        /// 좌클릭(Q): 주무기가 있으면 공격 (타격 누적·쓰러뜨림·내구도 소모·무기 소음), 없으면 밀치기.
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

            if (!SpendStamina(_balance.StaminaAttackCost))
            {
                return false;
            }

            _attackTimer = Mathf.Max(0.3f, _weapon.AttackCooldown);
            _attackAnim = AttackAnimSeconds;
            EmitNoise(CurrentArea, X, _weapon.AttackNoise);
            Sfx("sfx_swing");
            var hit = _threat.Strike(Snapshot, _facingRight ? 1 : -1, _weapon.WeaponPower, _weapon.StunSeconds, _balance.ShoveRange + 0.3f);
            if (hit == null)
            {
                return false;
            }

            _weaponWear++;
            React(hit, "hit_");
            Sfx("sfx_hit");
            _hud?.ShowMessage(hit.IsDown ? "exp.zombie_down" : "exp.hit");
            if (_weaponDurability > 0 && _weaponWear >= _weaponDurability)
            {
                _hud?.ShowMessage("exp.weapon_broke");
                _weapon = null;
                RefreshLook();
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
            if (target == null || InFieldDialogue || choiceIndex < 0 || choiceIndex >= target.Encounter.Choices.Count)
            {
                return;
            }

            ApplyEncounterChoice(target, choiceIndex);
            OpenEncounter = null;
        }

        /// <summary>
        /// 그 자리 대화: 이벤트 컷 위에서 조우 대사·선택지 → (현장 장면이면) 같은 컷에서 후속 이벤트 대사·선택지.
        /// 후속 이벤트에서 고른 것은 탐험 결과(FieldEvents)에 담아 귀환 때 적용한다.
        /// </summary>
        private System.Collections.IEnumerator FieldDialogueRoutine(ExplorationTarget target)
        {
            var encounter = target.Encounter;
            var token = destroyCancellationToken;
            string picked = null;
            var task = _fieldDialogue.Run(encounter.DialogueNode, encounter.SceneEventId, null, id => picked = id, token);
            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.IsFaulted)
            {
                Debug.LogException(task.Exception);
            }

            // 대사가 선택 없이 끝났으면(노드 오류 등) 마지막 선택지(보통 물러서기)로
            var index = encounter.Choices.FindIndex(c => c.Id == picked);
            if (index < 0)
            {
                index = encounter.Choices.Count - 1;
            }

            if (index >= 0)
            {
                ApplyEncounterChoice(target, index);
            }

            if (encounter.PlayEventInField && GameApp.Session != null)
            {
                var choices = GameApp.Session.PreviewFieldEvent(encounter.SceneEventId, _runFlags, out var ev);
                if (ev != null && _fieldDialogue.HasNode(ev.Node))
                {
                    string chosen = null;
                    task = _fieldDialogue.Run(ev.Node, ev.Id, choices, id => chosen = id, token);
                    while (!task.IsCompleted)
                    {
                        yield return null;
                    }

                    if (task.IsFaulted)
                    {
                        Debug.LogException(task.Exception);
                    }

                    _fieldEvents.Add(new FieldEventChoice { EventId = ev.Id, ChoiceId = chosen ?? "" });
                }
            }

            InFieldDialogue = false;
            OpenEncounter = null;
        }

        /// <summary>조우 선택 반영: 플래그·단서·스트레스, 대상이 사라지거나 더는 말을 걸 수 없게.</summary>
        private void ApplyEncounterChoice(ExplorationTarget target, int choiceIndex)
        {
            var choice = target.Encounter.Choices[choiceIndex];
            if (target.Encounter.Collectible && choice.Removes)
            {
                Sfx("sfx_cd_pickup", 0.9f);   // 에셋 8차: CD 케이스 딸깍
            }

            _runFlags.AddRange(choice.SetFlags);
            _runClues.AddRange(choice.AddClues);
            _runStress = Math.Max(0, _runStress + (int)choice.StressDelta);
            _runFlags.Add(EncounterRules.ChoiceFlag(target.Encounter, choice));
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
            RefreshLook();
        }

        /// <summary>장비에 맞는 주인공 그림을 고른다 (옷 = 방어구, 없으면 계절 교복, 손 = 주무기). 무기가 부서지면 다시 부른다.</summary>
        private void RefreshLook()
        {
            var armor = StandingConfig.PlayerOutfitToken(_armor?.Id, Request.Week);
            var weapon = ExplorationArt.ItemToken(_weapon?.Id, "ITEM_WEAPON_", "none");
            _lookPrefix = ExplorationArt.PlayerPrefix(armor, weapon);
        }

        /// <summary>등에 멘 가방 (배낭 아이템이 있고 그 그림이 있을 때): 주인공 몸 뒤에 한 겹 더 그린다.</summary>
        private void SetupBag()
        {
            // 가방 색은 판(새 게임)마다 하나로 정해진다: 가죽 갈색·파랑·분홍·빨강·노랑·초록
            var key = ExplorationArt.BagKey(ExplorationArt.ItemToken(_backpackItem?.Id, "ITEM_BAG_", ""), GameApp.Session != null && GameApp.Session.HasGame ? GameApp.Session.State.Rng.Seed : 0);
            var sprite = _backpackItem != null && _playerSprites ? ExplorationArt.Get(key) : null;
            if (sprite == null)
            {
                return;
            }

            var go = new GameObject("PlayerBag");
            go.transform.SetParent(_player, false);
            _bagRenderer = go.AddComponent<SpriteRenderer>();
            _bagRenderer.sprite = sprite;
            _bagRenderer.sortingOrder = 9;
        }

        /// <summary>디버그·테스트: 소음 발생.</summary>
        public void DebugEmitNoise(int areaId, float x, float radius)
        {
            _threat.Emit(new NoiseEvent(areaId, x, radius));
        }

        // ── 입력 ──
        // 마우스 왼쪽 = 공격·밀치기 (바라보는 방향), 오른쪽 = 수색(길게)·들어가기·쓰기. Q·E도 그대로 된다.
        // 열린 창(배낭·전리품·조우) 위에서의 클릭은 창이 받는다.

        private bool MouseFree(Mouse mouse) => mouse != null && (_hud == null || !_hud.PointerOverPanel(mouse.position.ReadValue()));

        private readonly ExplorationInput _input = new ExplorationInput();
        private VirtualPad _pad;

        /// <summary>가상 패드 (휴대폰이거나 메뉴에서 켰을 때만).</summary>
        public VirtualPad Pad => _pad;

        /// <summary>
        /// 손전등 겨누기 (도): 바라보는 쪽에서 마우스 높이를 따라 위(+)·아래(−), 최대 AimMaxAngle. 마우스가 등 뒤에 있으면 높이만 본다.
        /// 보이는 빛만 바뀌고 규칙(시야·발견)은 그대로.
        /// </summary>
        private float _flashlightAim;

        private void UpdateFlashlightAim(Vector3 center, float dt)
        {
            var aim = ExplorationLook.Settings.Flashlight ?? new ExplorationLook.FlashlightConfig();
            var target = 0f;
            if (_input.AimScreen.HasValue && _camera != null && aim.AimMaxAngle > 0f)
            {
                var from = _camera.WorldToScreenPoint(_world.TransformPoint(center));
                var to = _input.AimScreen.Value;
                var dx = Mathf.Max(Mathf.Abs(to.x - from.x), Screen.height * 0.08f);   // 몸 가까이·등 뒤에서는 덜 꺾이게
                target = Mathf.Clamp(Mathf.Atan2(to.y - from.y, dx) * Mathf.Rad2Deg, -aim.AimMaxAngle, aim.AimMaxAngle);
            }

            _flashlightAim = Mathf.Lerp(_flashlightAim, target, 1f - Mathf.Exp(-Mathf.Max(0.1f, aim.AimSmoothing) * dt));
        }

        private void HandleInput(float dt)
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.f2Key.wasPressedThisFrame)
                {
                    TimeScale = TimeScale > 1f ? 1f : 10f;
                    _log.DebugUsed = true;
                }

                HandleDebugKeys(keyboard);
            }

            if (_input.Flashlight)
            {
                ToggleFlashlight();
            }

            if (_input.Backpack)
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

            if (_input.QuickSlot >= 0)
            {
                UseQuickSlot(_input.QuickSlot);
            }

            Nearest = FindNearest();
            if (Hidden)
            {
                HandleHidden(dt);
                return;
            }

            if (HandleHideHold(dt))
            {
                return;
            }

            if (HandleDoorSenses(dt))
            {
                return;
            }

            if (_input.Attack)
            {
                Attack();
            }

            if (Hidden)
            {
                _tutorialHidden = true;
            }

            _crouching = _input.Crouch;
            // 숨이 차면(스태미너 바닥) 다시 찰 때까지 걷기만
            var running = _input.Run && !_crouching && !Exhausted && Stamina > 0f;
            _animRunning = running;
            var move = _input.Move;
            _animMoving = move != 0f;
            if (move != 0f)
            {
                _facingRight = move > 0f;
                _ranThisFrame = running;
                CloseContainer();
                var speed = ExplorationRules.MoveSpeed(running, _crouching, IsOverweight, _balance)
                            * _moveMultiplier * (running ? 1f + Request.Modifier("RunSpeed") : 1f)
                            * (InFlood ? _balance.FloodMoveMultiplier : 1f);   // 장마: 물을 헤치고 걷는다
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
                    Sfx(InFlood ? "sfx_step_water" : running && !_crouching ? "sfx_step_run" : "sfx_step_tile", _crouching ? 0.25f : running ? 0.7f : 0.45f);
                }

                Nearest = FindNearest();
            }
            if (Nearest == null)
            {
                return;
            }

            if (Nearest.Kind == TargetKind.Container && !Nearest.Searched)
            {
                if (_input.InteractHeld)
                {
                    _searchNoiseTimer -= dt;
                    if (_searchNoiseTimer <= 0f)
                    {
                        _searchNoiseTimer = SearchNoiseInterval;
                        EmitNoise(CurrentArea, X, _balance.SearchNoise);
                    }

                    _searching = true;

                    var searchRate = 1f + Request.Modifier("SearchSpeed");
                    Nearest.SearchProgress += (IsDark && !FlashlightOn ? dt / _balance.DarkSearchMultiplier : dt) * searchRate;
                    if (!Nearest.Sighed && Nearest.SearchSeconds > Nearest.CalmSeconds + 0.01f && Nearest.SearchProgress >= Nearest.CalmSeconds)
                    {
                        // 스트레스가 없었다면 끝났을 때: 짜증 섞인 한숨 (그만큼 더 걸린다)
                        Nearest.Sighed = true;
                        Sfx(SighSound, 0.8f);
                    }

                    if (Nearest.SearchProgress >= Nearest.SearchSeconds)
                    {
                        _searching = false;
                        _log.ContainersSearched++;
                        Nearest.MarkSearched();
                        if (!string.IsNullOrEmpty(Nearest.LootKey))
                        {
                            _searchedKeys.Add(Nearest.LootKey);
                        }
                        _tutorialSearched = true;
                        OpenContainer = Nearest;
                    }
                }

                return;
            }

            if (_input.InteractDown)
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

        /// <summary>디버그·화면 확인: 방 ID로 바로 들어간다 (없으면 false).</summary>
        public bool DebugEnterRoom(string roomId, float x = 4f)
        {
            var room = _rooms.Find(r => r.Door.RoomId == roomId);
            if (room == null)
            {
                return false;
            }

            EnsureRoomBuilt(room);
            MoveTo(room, x);
            return true;
        }

        private ExplorationTarget _debugPeek;

        /// <summary>디버그·화면 확인: 그 방 문 앞으로 가서 W를 누르고 있는 것처럼 문틈을 살핀다 (null이면 그만).</summary>
        public bool DebugPeek(string roomId)
        {
            _debugPeek = roomId == null ? null : _targets.Find(t => t.Kind == TargetKind.Door && t.Room.Door.RoomId == roomId);
            if (_debugPeek == null)
            {
                return roomId == null;
            }

            MoveTo(_debugPeek.Area, _debugPeek.X);
            return true;
        }

        /// <summary>디버그·화면 확인: 복도(층) 또는 바깥 길(floor = -1 진입로, -2 탈출로)의 그 자리로 간다.</summary>
        public bool DebugGoTo(int floor, float x)
        {
            var area = floor >= 0 ? (floor < _corridors.Count ? _corridors[floor] : null) : (-floor - 1 < _outdoors.Count ? _outdoors[-floor - 1] : null);
            if (area == null)
            {
                return false;
            }

            MoveTo(area, x);
            return true;
        }

        private void MoveTo(ExplorationArea area, float x)
        {
            CurrentArea = area;
            X = Mathf.Clamp(x, EdgeMargin, area.Width - EdgeMargin);
            if (InFlood && !_floodNoticed)
            {
                _floodNoticed = true;
                _hud?.ShowMessage("exp.flooded");   // 물이 무릎까지 차 있다
            }

            // 옥상에 처음 올라섰다: 귀환하면 거점 옥상이 열린다 (EVT_A1_ROOFTOP_EARLY)
            var roofFlag = Location?.Layout?.RooftopFlag;
            if (area.IsRoof && !string.IsNullOrEmpty(roofFlag) && !_runFlags.Contains(roofFlag))
            {
                _runFlags.Add(roofFlag);
            }
            CloseContainer();
            Hidden = false;
            Nearest = null;
            _look?.ApplyPixelLight(_world);   // 그 사이 새로 생긴 그림도 도트 빛으로
        }

        private void Finish(ExtractStatus status)
        {
            if (IsFinished)
            {
                return;
            }

            IsFinished = true;
            Presentation.Audio.Sound.StopAllLoops();
            if (status != ExtractStatus.Down)
            {
                Sfx("sfx_extract");
            }

            var hpDelta = status == ExtractStatus.Down
                ? Math.Min(0, _balance.DownRemainingHp - _startHp)
                : Hp - _startHp;
            Result = RunResultBuilder.Build(status, Backpack.Items, _db, _baseFatigue, _runStress, hpDelta, Clock.Elapsed);
            Result.UsedItems.AddRange(_usedItems);
            Result.WeaponWear = _weaponWear;
            Result.SearchedContainers.AddRange(_searchedKeys);
            HostileRules.Apply(Result, _hostileOutcome, _startHp, _balance);
            foreach (var flag in _runFlags)
            {
                if (!Result.Flags.Contains(flag)) Result.Flags.Add(flag);
            }

            foreach (var clue in _runClues)
            {
                if (!Result.Clues.Contains(clue)) Result.Clues.Add(clue);
            }

            Result.FieldEvents.AddRange(_fieldEvents);

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
            var request = new ExpeditionRequest
            {
                LocationId = _debugLocationId,
                Seed = Environment.TickCount,
                TimeOfDay = _debugTimeOfDay,
                Stats = new Dictionary<StatType, int>(player.Stats),
                Hp = player.Vitals.Hp,
                Week = session.State.Calendar.Week,
            };
            request.Loadout.Tool = _debugTool ?? "";
            request.LootedContainers.UnionWith(session.State.Exploration.LootedContainers);
            session.DebugExpedition.ApplyForcedHostile(request);
            return request;
        }

        private void BuildWorld()
        {
            _world = new GameObject("World").transform;
            _world.SetParent(transform, false);
            var layout = Location.Layout;
            _building = new BuildingView(_world, layout, Location.Id);

            for (var floor = 0; floor < layout.TotalFloors; floor++)
            {
                var deep = layout.IsDeep(floor);
                _corridors.Add(new ExplorationArea
                {
                    Floor = floor,
                    Origin = new Vector2(0f, _building.FloorY(floor)),
                    Width = layout.CorridorLengthOf(floor),
                    Built = true,
                    IsRoof = layout.IsRooftop(floor),
                    IsDeep = deep,
                    Label = layout.IsRooftop(floor) ? Loc.T("exp.rooftop")
                        : deep ? Loc.F("exp.deep_floor", layout.DeepLevel(floor), Loc.T(layout.DeepNameKey)) : "",
                });
            }

            _areasById.InsertRange(0, _corridors);

            // 계단실: 가까이 붙은 계단(올라가는·내려가는)을 한 칸으로 묶어, 복도 벽이 뚫린 어두운 공간 안에 계단을 둔다
            var wells = new List<(int Floor, float Min, float Max)>();
            foreach (var connector in layout.Connectors.Where(c => c.IsStairs))
            {
                var index = wells.FindIndex(w => w.Floor == connector.Floor && connector.X > w.Min - 4f && connector.X < w.Max + 4f);
                if (index < 0)
                {
                    wells.Add((connector.Floor, connector.X, connector.X));
                }
                else
                {
                    wells[index] = (connector.Floor, Mathf.Min(wells[index].Min, connector.X), Mathf.Max(wells[index].Max, connector.X));
                }
            }

            foreach (var well in wells)
            {
                var origin = _corridors[well.Floor].Origin;
                var left = Mathf.Max(0f, well.Min - 1.1f);
                var right = Mathf.Min(layout.CorridorLengthOf(well.Floor), well.Max + 1.1f);
                var top = layout.FloorHeight - 0.75f;
                BuildingView.Rect(_world, "Stairwell", origin + new Vector2(left, 0.15f), new Vector2(right - left, top - 0.15f), new Color(0.05f, 0.055f, 0.07f), -5);
                BuildingView.Rect(_world, "StairwellBack", origin + new Vector2(left + 0.25f, 0.15f), new Vector2(right - left - 0.5f, top - 0.45f), new Color(0.1f, 0.11f, 0.12f), -5);
                BuildingView.Rect(_world, "StairwellLanding", origin + new Vector2(left, 0.15f), new Vector2(right - left, 0.12f), new Color(0.17f, 0.17f, 0.17f), -4);
                BuildingView.Rect(_world, "StairwellFrameL", origin + new Vector2(left - 0.14f, 0.15f), new Vector2(0.14f, top - 0.15f), new Color(0.3f, 0.31f, 0.29f), -4);
                BuildingView.Rect(_world, "StairwellFrameR", origin + new Vector2(right, 0.15f), new Vector2(0.14f, top - 0.15f), new Color(0.3f, 0.31f, 0.29f), -4);
                BuildingView.Rect(_world, "StairwellLintel", origin + new Vector2(left - 0.14f, top), new Vector2(right - left + 0.28f, 0.16f), new Color(0.3f, 0.31f, 0.29f), -4);
            }

            foreach (var connector in layout.Connectors.Where(c => !c.IsStairs))
            {
                CreateDeepConnector(connector);
            }

            foreach (var connector in layout.Connectors.Where(c => c.IsStairs))
            {
                var stairsKey = connector.Collapsed && ExplorationArt.Get("prop_stairs_collapsed") != null ? "prop_stairs_collapsed"
                    : (connector.TargetFloor > connector.Floor) != layout.IsDeep(connector.Floor) ? "prop_stairs_up" : "prop_stairs_down";   // 깊은 곳은 번호가 클수록 아래
                var view = Prop(_corridors[connector.Floor], "Stairs", stairsKey,
                    connector.X, new Vector2(1f, 2.7f), connector.Collapsed ? new Color(0.25f, 0.24f, 0.23f) : new Color(0.32f, 0.38f, 0.5f), -3);
                var stairsRenderer = view.GetComponent<SpriteRenderer>();
                if (stairsRenderer != null && ExplorationArt.Get(stairsKey) != null)
                {
                    stairsRenderer.color = new Color(0.78f, 0.78f, 0.8f);   // 계단실 안쪽이라 복도보다 조금 어둡게
                }
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
                    var view = Prop(corridor, "Door_" + door.RoomId, ExplorationArt.Door(Location.Id, template.Id, false), corridorX, new Vector2(1.4f, 2.8f), BuildingView.DoorColor, 0);
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
                if (!Deplete(target, HostileRules.CorridorKey(Location.Id, i)))
                {
                    target.Contents.AddRange(RoomLoot.RollCorridorContainer(_db, Location, i, Request.Seed));
                }
            }

            foreach (var encounter in ActiveEncounters())
            {
                if (string.IsNullOrEmpty(encounter.RoomId) && encounter.Floor >= 0 && encounter.Floor < _corridors.Count)
                {
                    CreateEncounter(_corridors[encounter.Floor], encounter);
                }
            }

            ExitDef start = null;
            ExplorationTarget startTarget = null;
            foreach (var exit in layout.Exits)
            {
                var color = exit.StartsLocked ? new Color(0.3f, 0.3f, 0.3f) : new Color(0.35f, 0.6f, 0.38f);
                var view = Prop(_corridors[exit.Floor], "Exit", ExplorationArt.Exit(Location.Id, exit.StartsLocked), exit.X, new Vector2(1.6f, 3f), color, 0);
                var exitTarget = new ExplorationTarget { Kind = TargetKind.Exit, Area = _corridors[exit.Floor], X = exit.X, Exit = exit, Locked = exit.StartsLocked, View = view };
                AddTarget(exitTarget);
                if (HasApproach)
                {
                    BuildOutdoor(exitTarget, exit.Kind == ExitKind.Main);
                }

                if (exit.Kind == ExitKind.Main && start == null)
                {
                    start = exit;
                    startTarget = exitTarget;
                }
            }

            CurrentArea = _corridors[start?.Floor ?? 0];
            X = start?.X ?? 1f;
            if (startTarget != null && startTarget.LinkArea != null)
            {
                // 학교 밖 지역은 진입로 끝(학교에서 오는 길)에서 시작해 걸어 들어간다
                CurrentArea = startTarget.LinkArea;
                X = 2.2f;
            }

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

            SetupBag();
        }

        // ── 탐험의 깊이 (기획 B) ──

        private ExplorationTarget _oneWayArmed;
        private float _oneWayArmedUntil;
        private bool _deepEntered;
        private float _deepSeconds;

        /// <summary>깊은 문(낡은 철제 방화문)·일방 출구(안: 푸시 바 비상구, 밖: 손잡이 없는 민짜 철문).</summary>
        private void CreateDeepConnector(ConnectorDef connector)
        {
            if (connector.Floor >= _corridors.Count || connector.TargetFloor >= _corridors.Count)
            {
                Debug.LogWarning($"깊은 곳 연결의 층을 찾을 수 없음: {connector.Floor} → {connector.TargetFloor}");
                return;
            }

            string key;
            Color color;
            switch (connector.Kind)
            {
                case ConnectorKind.DeepDoor:
                    // 장마·폭설로 막힌 깊은 문은 물에 잠기거나 눈에 묻힌 그림 (없으면 보통 문)
                    key = connector.Collapsed && connector.BlockedBy == DisasterRules.Monsoon && ExplorationArt.Get("prop_deep_door_flooded") != null ? "prop_deep_door_flooded"
                        : connector.Collapsed && connector.BlockedBy == DisasterRules.Snowstorm && ExplorationArt.Get("prop_deep_door_snow") != null ? "prop_deep_door_snow"
                        : "prop_deep_door";
                    color = new Color(0.45f, 0.28f, 0.2f);
                    break;
                case ConnectorKind.OneWayExit:
                    key = "prop_exit_pushbar";
                    color = new Color(0.3f, 0.5f, 0.36f);
                    break;
                default:
                    key = "prop_exit_blank";
                    color = new Color(0.32f, 0.33f, 0.35f);
                    break;
            }

            var area = _corridors[connector.Floor];
            var view = Prop(area, "DeepLink_" + connector.Kind, key, connector.X, new Vector2(1.6f, 3f), color, 0);
            AddTarget(new ExplorationTarget { Kind = TargetKind.Connector, Area = area, X = connector.X, Connector = connector, View = view });
        }

        /// <summary>깊은 곳에 처음 들어섰을 때: 한 줄. 유랑자에게 들은 게 있으면 일방 출구 자리를 알려 준다.</summary>
        private void OnEnteredDeep()
        {
            if (_deepEntered)
            {
                return;
            }

            _deepEntered = true;
            var layout = Location.Layout;
            var exit = layout.Connectors.Find(c => c.Kind == ConnectorKind.OneWayExit);
            if (Request.KnowsDeepExits && exit != null)
            {
                var side = exit.X < layout.CorridorLengthOf(exit.Floor) * 0.5f ? Loc.T("exp.side_west") : Loc.T("exp.side_east");
                _hud?.ShowText(Loc.F("exp.deep_exit_hint", layout.DeepLevel(exit.Floor), side), 3200);
            }
            else
            {
                _hud?.ShowMessage("exp.deep_enter");
            }
        }

        /// <summary>학교 밖 지역(튜토리얼 제외)에는 바깥 길이 있다: 진입로와 탈출로.</summary>
        private bool HasApproach => !Location.Id.Contains("SCHOOL") && !Location.Id.Contains("TUTORIAL");

        private readonly List<ExplorationArea> _outdoors = new List<ExplorationArea>();
        private readonly List<ExplorationTarget> _outdoorLinks = new List<ExplorationTarget>();

        /// <summary>
        /// 건물 출구 하나에 바깥 길을 붙인다. 진입로(정문): 왼쪽 끝 = 학교로 돌아가는 길, 오른쪽 끝 = 건물 문.
        /// 탈출로(다른 출구): 왼쪽 끝 = 건물 문, 오른쪽 끝 = 학교로 돌아가는 길. 어느 길 끝에서든 귀환한다.
        /// </summary>
        private void BuildOutdoor(ExplorationTarget exitTarget, bool entry)
        {
            var width = entry ? _balance.ApproachRoadWidth : _balance.EscapeRouteWidth;
            var area = new ExplorationArea
            {
                IsOutdoor = true,
                Floor = exitTarget.Area.Floor,
                Origin = new Vector2(_outdoors.Count * 90f, -70f),
                Width = width,
                Label = Loc.T(entry ? "exp.area_entry" : "exp.area_escape"),
            };
            _outdoors.Add(area);
            _areasById.Add(area);

            var doorX = entry ? width - 2.2f : 2.2f;
            var returnX = entry ? 1.6f : width - 1.6f;
            _building.BuildOutdoor(area, entry, new List<(float, float)> { (doorX - 1.6f, doorX + 1.6f), (returnX - 1.6f, returnX + 1.6f) });
            CollectFog(area.Root.transform, area);

            var doorView = Prop(area, "BuildingDoor", ExplorationArt.Exit(Location.Id, false), doorX, new Vector2(1.6f, 3f), new Color(0.35f, 0.6f, 0.38f), 0);
            var passage = new ExplorationTarget { Kind = TargetKind.Passage, Area = area, X = doorX, LinkArea = exitTarget.Area, LinkX = exitTarget.X, View = doorView };
            AddTarget(passage);
            var returnView = Prop(area, "ReturnPoint", "prop_out_return", returnX, new Vector2(0.8f, 2.2f), new Color(0.35f, 0.5f, 0.7f), 0);
            AddTarget(new ExplorationTarget { Kind = TargetKind.Exit, Area = area, X = returnX, Exit = exitTarget.Exit, View = returnView });

            exitTarget.LinkArea = area;
            exitTarget.LinkX = doorX;
            _outdoorLinks.Add(exitTarget);
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
            CollectFog(room.Root.transform, room);
            _lighting?.AddRoom(room);
            foreach (var doorX in room.Template.DoorXs)
            {
                var exitView = Prop(room, "RoomExit", ExplorationArt.Door(Location.Id, room.Template?.Id, false), doorX, new Vector2(1.4f, 2.8f), BuildingView.DoorColor, 0);
                // 1점 투시 방에서는 문이 뒷벽에 있다: 뒤쪽 바닥 높이만큼 올려 그린다
                exitView.transform.localPosition += new Vector3(0f, BuildingView.BackWallOffset(room.Template, Location.Id), 0f);
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
                if (!Deplete(target, HostileRules.RoomKey(Location.Id, room.DoorIndex, i)) && i < contents.Count)
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
                _shoveAnim = Mathf.Max(0f, _shoveAnim - dt);
            }

            // 장비 차림별 그림 (_lookPrefix). 픽셀 아트는 동작마다 프레임 수가 달라 세어서 돌린다 (없으면 코드 그림 4프레임).
            var look = _lookPrefix;
            if (_attackAnim > 0f)
            {
                return ProgressFrame(look + "attack_", 1f - _attackAnim / AttackAnimSeconds) ?? look + "attack";
            }

            if (_shoveAnim > 0f)
            {
                var shove = ProgressFrame(look + "shove_", 1f - _shoveAnim / ShoveAnimSeconds);
                if (shove != null)
                {
                    return shove;
                }
            }

            if (Hidden) return look + "crouch";
            if (_crouching)
            {
                return _animMoving ? LoopFrame(look + "crouch_", _animTime, PixelSneakFps) ?? look + "crouch" : look + "crouch";
            }

            if (!_animMoving) return LoopFrame(look + "idle_", Time.time, PixelIdleFps) ?? look + "idle";
            var prefix = look + (_animRunning ? "run_" : "walk_");
            var pixel = LoopFrame(prefix, _animTime, _animRunning ? PixelRunFps : PixelWalkFps);
            if (pixel != null)
            {
                return pixel;
            }

            var count = ExplorationArt.FrameCount(prefix, 4);
            var frame = (int)(_animTime * (_animRunning ? 12f : 7f)) % count;
            return prefix + frame;
        }

        /// <summary>픽셀 아트 프레임이 있으면 진행도(0~1)에 맞는 프레임 키, 없으면 null.</summary>
        private static string ProgressFrame(string prefix, float progress)
        {
            if (!ExplorationArt.HasPixel(prefix + "0"))
            {
                return null;
            }

            var count = ExplorationArt.FrameCount(prefix, 1);
            return prefix + Mathf.Clamp((int)(progress * count), 0, count - 1);
        }

        /// <summary>픽셀 아트 프레임이 있으면 시간에 맞춰 도는 프레임 키, 없으면 null.</summary>
        private static string LoopFrame(string prefix, float time, float framesPerSecond, int offset = 0)
        {
            if (!ExplorationArt.HasPixel(prefix + "0"))
            {
                return null;
            }

            var count = ExplorationArt.FrameCount(prefix, 1);
            return prefix + ((int)(time * framesPerSecond) + offset) % count;
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
            return EncounterRules.Active(Location, Request.TimeOfDay, Request.Flags);
        }

        private void CreateEncounter(ExplorationArea area, EncounterDef encounter)
        {
            var key = ExplorationArt.Encounter(encounter.Id)
                ?? (encounter.Collectible && ExplorationArt.Get(encounter.SpriteKey) == null ? "prop_paper"   // CD 그림이 아직 없으면 종이 자리
                : !string.IsNullOrEmpty(encounter.SpriteKey) ? encounter.SpriteKey
                : encounter.Id.Contains("NOTE") || encounter.Id.Contains("NOTICE") ? "prop_paper" : "person_student_crouch");
            var view = Prop(area, "Encounter_" + encounter.Id, key, encounter.X, new Vector2(0.6f, 1.5f), new Color(0.55f, 0.62f, 0.75f), 9);
            ExplorationArt.Animate(view, key, 6f);   // 숨 쉬는 사람 등 (프레임이 있을 때만)
            AddFog(view, area);
            ExplorationLook.AddCaster(view, ExplorationLook.Settings.Shadows.People);
            AddTarget(new ExplorationTarget { Kind = TargetKind.Encounter, Area = area, X = encounter.X, Encounter = encounter, View = view });
        }

        private ExplorationTarget CreateContainer(ExplorationArea area, ContainerDef container)
        {
            var size = ContainerSize(container.Kind);
            // 방마다 다른 소품 (양호실 침대, 과학실 실험대 등): SpriteKey가 있으면 그 그림
            var sprite = ExplorationArt.Container(Location.Id, area.Template?.Id, container)
                ?? (!string.IsNullOrEmpty(container.SpriteKey) && ExplorationArt.Get(container.SpriteKey) != null
                ? container.SpriteKey
                : ContainerSprite(container.Kind));
            var view = Prop(area, "Container_" + container.Id, sprite, container.X, size, new Color(0.55f, 0.45f, 0.3f), 1);
            AddFog(view, area);
            ExplorationLook.AddCaster(view, ExplorationLook.Settings.Shadows.Containers);
            var target = new ExplorationTarget
            {
                Kind = TargetKind.Container,
                Area = area,
                X = container.X,
                Container = container,
                CalmSeconds = ExplorationRules.SearchSeconds(container.SearchSeconds, Request.Stats, _balance),
                SearchSeconds = ExplorationRules.SearchSeconds(container.SearchSeconds, Request.Stats, _balance) * _stressSearchFactor,
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
            _camera.backgroundColor = SkyColor;
            if (FindAnyObjectByType<AudioListener>() == null)
            {
                go.AddComponent<AudioListener>();
            }

            var fade = BuildingView.Rect(go.transform, "ScreenFade", new Vector2(-100f, -100f), new Vector2(200f, 200f), new Color(0f, 0f, 0f, 0f), 1000);
            fade.transform.localPosition = new Vector3(-100f, -100f, 1f);
            _fadeOverlay = fade.GetComponent<SpriteRenderer>();
            // 다른 층 가리개: 복도에서는 지금 있는 층만 보인다 (위·아래층은 검게)
            _maskAbove = BuildingView.Rect(_world, "FloorMaskAbove", Vector2.zero, new Vector2(600f, 60f), Color.black, 45);
            _maskBelow = BuildingView.Rect(_world, "FloorMaskBelow", Vector2.zero, new Vector2(600f, 60f), Color.black, 45);
            _shownArea = CurrentArea;
            _look.Setup(_camera, transform, Request.TimeOfDay == ExpeditionTime.Day);
            SnapCamera();
        }

        private GameObject _maskAbove;
        private GameObject _maskBelow;

        /// <summary>후처리·빛 기둥이 켜져 있는가 (디버그 F9로 끈다).</summary>
        public bool LookOn => _look.On;

        /// <summary>디버그: 지금 공간 안 x 자리로 옮긴다 (캡처용).</summary>
        public void DebugMoveTo(float x)
        {
            _log.DebugUsed = true;
            MoveTo(CurrentArea, x);
        }

        /// <summary>디버그: 다른 층 복도의 x 자리로 옮긴다 (캡처용).</summary>
        public void DebugMoveToFloor(int floor, float x)
        {
            if (floor < 0 || floor >= _corridors.Count)
            {
                return;
            }

            _log.DebugUsed = true;
            MoveTo(_corridors[floor], x);
        }

        /// <summary>디버그: 후처리·빛 기둥 켜고 끄기 (전후 비교·캡처용).</summary>
        public void ToggleLook()
        {
            _look.Toggle();
            ApplyVolumes();
        }

        /// <summary>빛 기둥(볼류메트릭): 손전등 원뿔과 낮 햇빛 줄기가 공기 중에 보인다.</summary>
        private void ApplyVolumes()
        {
            if (_flashlight != null)
            {
                _flashlight.volumetricEnabled = _look.FlashlightVolume > 0f;
                _flashlight.volumeIntensity = _look.FlashlightVolume;
            }

            if (_flashlightHot != null)
            {
                _flashlightHot.volumetricEnabled = _look.FlashlightVolume > 0f;
                _flashlightHot.volumeIntensity = _look.FlashlightVolume * 0.6f;
            }

            if (_lighting != null)
            {
                _lighting.BeamVolume = _look.SunbeamVolume;
            }

            // 2단계 그림자: 손전등 빛만 그림자를 만든다
            _look.ApplyShadows(_flashlight);
            _look.ApplyShadows(_flashlightHot);
            _look.ApplyShadows(_flashlightSpill);
            _look.ApplyNormals(_flashlight);
            _look.ApplyNormals(_flashlightHot);
            _look.ApplyNormals(_flashlightSpill);
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
            // 손전등은 세 겹으로: 본 빛(부드러운 원뿔) + 뜨거운 가운데(좁고 멀리) + 넓게 번진 빛(가깝고 옅게).
            // 겹쳐서 가운데가 가장 밝고 가장자리·먼 곳으로 자연스럽게 어두워진다.
            _flashlight.pointLightInnerRadius = 0.4f;
            _flashlight.pointLightOuterRadius = 11f;
            _flashlight.pointLightInnerAngle = 14f;
            _flashlight.pointLightOuterAngle = 58f;
            _flashlight.falloffIntensity = 0.75f;
            _flashlight.intensity = 0.85f;
            _flashlight.color = new Color(1f, 0.94f, 0.8f);
            _flashlightHot = ChildCone(flash.transform, "FlashlightHot", 0.3f, 13f, 4f, 20f, 0.6f, 0.55f, new Color(1f, 0.97f, 0.88f));
            _flashlightSpill = ChildCone(flash.transform, "FlashlightSpill", 0.2f, 5.5f, 70f, 150f, 0.9f, 0.22f, new Color(1f, 0.9f, 0.75f));

            // 어둠 속에서도 자기 주변은 희미하게 보이게
            var glow = new GameObject("SelfGlow");
            glow.transform.SetParent(_world, false);
            _selfLight = glow.AddComponent<Light2D>();
            _selfLight.lightType = Light2D.LightType.Point;
            _selfLight.pointLightInnerRadius = 0.3f;
            var glowConfig = ExplorationLook.Settings.SelfGlow ?? new ExplorationLook.SelfGlowConfig();
            _selfLight.pointLightOuterRadius = Mathf.Max(0.5f, _balance.NightViewDistance * glowConfig.RadiusScale);
            _selfLight.intensity = glowConfig.DarkIntensity;
            _selfLight.color = new Color(0.8f, 0.85f, 1f);
            _selfLight.shadowsEnabled = false;   // 그림자는 손전등만

            CollectFog(_world, null);
            _look.ApplyPixelLight(_world);
            _lighting = new ExplorationLighting(_world, _balance, _building.FloorHeight, day ? ExplorationLighting.Phase.Day : ExplorationLighting.Phase.Night);
            foreach (var corridor in _corridors)
            {
                _lighting.AddCorridor(corridor);
            }

            foreach (var room in _rooms)
            {
                if (room.Built)
                {
                    _lighting.AddRoom(room);
                }
            }

            _areaFactor = _lighting.AmbientFactor(CurrentArea);
            ApplyVolumes();
            SetAmbient(_ambientIntensity, _light.color);
        }

        private Light2D _flashlightHot;
        private Light2D _flashlightSpill;

        private static Light2D ChildCone(Transform parent, string name, float innerRadius, float outerRadius, float innerAngle, float outerAngle, float falloff, float intensity, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.pointLightInnerRadius = innerRadius;
            light.pointLightOuterRadius = outerRadius;
            light.pointLightInnerAngle = innerAngle;
            light.pointLightOuterAngle = outerAngle;
            light.falloffIntensity = falloff;
            light.intensity = intensity;
            light.color = color;
            return light;
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
                _playerFrameKey = PlayerFrame(dt);
                _playerRenderer.sprite = ExplorationArt.Get(_playerFrameKey);
                _playerRenderer.color = Hidden ? new Color(1f, 1f, 1f, 0.3f) : Color.white;
                if (_bagRenderer != null)
                {
                    // 가방: 그 프레임의 등 위치에 붙인다 (자리 표에 없는 프레임에서는 숨긴다)
                    var shown = ExplorationArt.TryGetBackAnchor(_playerFrameKey, out var back, out var lean);
                    _bagRenderer.enabled = shown;
                    if (shown)
                    {
                        _bagRenderer.flipX = !_facingRight;
                        _bagRenderer.transform.localPosition = new Vector3(_facingRight ? back.x : -back.x, back.y, 0f);
                        _bagRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, _facingRight ? lean : -lean);
                        _bagRenderer.color = _playerRenderer.color;
                    }
                }
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
                var flashOn = DarkHere && FlashlightOn && !Hidden;
                _flashlight.enabled = flashOn;
                if (_flashlightHot != null) _flashlightHot.enabled = flashOn;
                if (_flashlightSpill != null) _flashlightSpill.enabled = flashOn;

                // 손에 든 빛: 걸을 때 살짝 흔들리고(뛰면 더), 아주 조금 떨린다
                var swayAmount = _animMoving ? (_animRunning ? 3.2f : 1.6f) : 0.35f;
                var sway = Mathf.Sin(Time.time * (_animMoving ? (_animRunning ? 13f : 8f) : 1.7f)) * swayAmount;
                var flicker = 1f + (Mathf.PerlinNoise(Time.time * 9f, 0.37f) - 0.5f) * 0.05f;
                _flashlight.intensity = 0.85f * flicker;
                _flashlight.transform.localPosition = center + new Vector3(_facingRight ? 0.25f : -0.25f, -0.25f, 0f);
                UpdateFlashlightAim(center, Time.unscaledDeltaTime);
                var tilt = _flashlightAim - (_crouching ? 0f : 3f);   // 위가 + (바라보는 쪽 기준)
                _flashlight.transform.localRotation = Quaternion.Euler(0f, 0f, (_facingRight ? -90f + tilt : 90f - tilt) + sway);
            }

            if (_selfLight != null)
            {
                // 낮에도 어두운 방 안에서는 주인공 주변만 희미하게 보이게
                var dimRoom = !IsDark && _lighting != null && _lighting.IsDimRoom(CurrentArea);
                _selfLight.enabled = DarkHere || dimRoom;
                var glow = ExplorationLook.Settings.SelfGlow ?? new ExplorationLook.SelfGlowConfig();
                _selfLight.intensity = IsDark ? glow.DarkIntensity : glow.DimIntensity;
                _selfLight.transform.localPosition = center;
            }

            // 화면 비율과 관계없이 가로 최소 MinVisibleWidth 유닛이 보이게 (멀리서 건물과 캐릭터를 함께 잡는 앵글)
            var wantedSize = Mathf.Clamp(MinVisibleWidth / (2f * Mathf.Max(0.1f, _camera.aspect)), MinOrthographicSize, MaxOrthographicSize);

            // 도트가 고르게 보이도록 그림 1픽셀 = 화면 정수 픽셀이 되게 맞춘다 (1080p·4K에서 2배·4배, 7.5유닛)
            var pixelScale = Mathf.Max(1, Mathf.RoundToInt(_camera.pixelHeight / (2f * wantedSize * ExplorationArt.PixelsPerUnit)));
            _camera.orthographicSize = _camera.pixelHeight / (2f * ExplorationArt.PixelsPerUnit * pixelScale);
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
            var cameraPosition = cut || dt >= 1000f ? target : Vector3.Lerp(cameraTransform.position, target, Mathf.Clamp01(dt * 6f));

            // 화면 픽셀 격자에 맞춰 놓는다 (움직일 때 도트가 떨리지 않게)
            var grid = ExplorationArt.PixelsPerUnit * pixelScale;
            cameraPosition.x = Mathf.Round(cameraPosition.x * grid) / grid;
            cameraPosition.y = Mathf.Round(cameraPosition.y * grid) / grid;
            cameraTransform.position = cameraPosition;

            // 복도에서는 지금 층만: 위층은 천장 슬래브 위부터, 아래층은 바닥 슬래브 아래부터 가린다 (1층 아래 땅은 그대로)
            if (_maskAbove != null)
            {
                var shown = _shownArea ?? CurrentArea;
                var corridorView = !shown.IsRoom && !shown.IsOutdoor;
                _maskAbove.SetActive(corridorView && !shown.IsRoof);   // 옥상 위는 하늘이라 가리지 않는다
                _maskBelow.SetActive(corridorView && shown.Floor > 0);
                if (corridorView)
                {
                    _maskAbove.transform.localPosition = new Vector3(-200f, shown.Origin.y + _building.FloorHeight + 0.15f, 0f);
                    _maskBelow.transform.localPosition = new Vector3(-200f, shown.Origin.y - 0.15f - 60f, 0f);
                }
            }

            // 보이는 공간의 밝기 (낮: 복도는 햇빛, 문 안 방은 어둡게). 문을 지나면 잠깐 동안 눈이 적응하듯 바뀐다.
            if (_lighting != null)
            {
                var wanted = _lighting.AmbientFactor(_shownArea ?? CurrentArea);
                _areaFactor = cut || dt >= 1000f ? wanted : Mathf.MoveTowards(_areaFactor, wanted, dt * 2.5f);
                _lighting.Tick(dt, Clock != null && Request.TimeOfDay == ExpeditionTime.Day ? Clock.Normalized : 0f);
                if (_light != null && !FullVision)
                {
                    _light.intensity = AmbientLightIntensity;
                }
            }
        }

        // ── 위협 ──

        private void BuildThreats(LocationVariantDef variant)
        {
            _threat = new ThreatSimulation(_balance, new SeededRandom(SeededRandom.CreateState(Request.Seed ^ 0x5EED)));
            for (var id = 0; id < _areasById.Count; id++)
            {
                _threat.SetAreaWidth(id, _areasById[id].Width);
            }

            foreach (var link in _outdoorLinks)
            {
                // 건물 출구 ↔ 바깥 길: 쫓아오는 좀비가 문으로 따라 나올 수 있다
                _threat.AddLink(AreaId(link.Area), link.X, AreaId(link.LinkArea), link.LinkX);
                _threat.AddLink(AreaId(link.LinkArea), link.LinkX, AreaId(link.Area), link.X);
            }

            // 깊은 문: 쫓아오는 좀비가 따라 들어올 수 있다 (일방 출구는 등 뒤에서 닫혀 따라오지 못한다)
            foreach (var connector in Location.Layout.Connectors.Where(c => c.Kind == ConnectorKind.DeepDoor))
            {
                if (connector.Floor < _corridors.Count && connector.TargetFloor < _corridors.Count)
                {
                    _threat.AddLink(AreaId(_corridors[connector.Floor]), connector.X, AreaId(_corridors[connector.TargetFloor]), connector.ArrivalX);
                }
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
                    ExplorationLook.AddCaster(body, ExplorationLook.Settings.Shadows.Zombies);
                    var marker = BuildingView.Rect(_world, "Awareness_" + zombie.Id, Vector2.zero, Vector2.one, Color.yellow, 12);
                    _zombieViews.Add(new ZombieView
                    {
                        Unit = zombie, Body = body.transform, BodyRenderer = body.GetComponent<SpriteRenderer>(), Marker = marker.transform,
                        MarkerRenderer = marker.GetComponent<SpriteRenderer>(), Kind = kind, Sprites = ExplorationArt.Get($"zombie_{kind}_walk_0") != null,
                        LastX = zombie.X,
                    });
                }
            }

            SpawnSurvivor(variant);
        }

        /// <summary>적대적 생존자 1명 (요청의 성향이 있으면). 지정 위치 후보 중 하나, 없으면 출구에서 먼 복도.</summary>
        private void SpawnSurvivor(LocationVariantDef variant)
        {
            if (Request.HostileTemperament <= 0)
            {
                return;
            }

            if (!_db.TryGetEnemy(_balance.HostileEnemyId, out var enemy))
            {
                enemy = new EnemyDef { Id = _balance.HostileEnemyId, Kind = EnemyKind.Survivor, CanBeDowned = false };
            }

            var random = new SeededRandom(SeededRandom.CreateState(Request.Seed ^ 0x5A1));
            ExplorationArea area;
            float x;
            var count = Math.Max(1, Request.HostileCount);
            if (variant.HostileSpots.Count > 0)
            {
                var spot = variant.HostileSpots[random.Range(0, variant.HostileSpots.Count - 1)];
                area = ResolveSpawnArea(spot) ?? _corridors[0];
                x = spot.X;
            }
            else
            {
                var start = Location.Layout.Exits.Find(e => e.Kind == ExitKind.Main);
                var outerFloors = Location.Layout.Floors;   // 깊은 곳에는 생존자를 두지 않는다
                var floor = outerFloors > 1 ? random.Range(0, outerFloors - 1) : 0;
                area = _corridors[floor];
                var startNearLeft = start == null || start.Floor != floor || start.X < area.Width / 2f;
                x = startNearLeft
                    ? area.Width * (0.6f + 0.3f * (float)random.NextDouble())
                    : area.Width * (0.1f + 0.3f * (float)random.NextDouble());
            }

            // 한 무리: 첫 사람 근처에 나머지가 선다
            for (var i = 0; i < count; i++)
            {
                var offset = i == 0 ? 0f : (i % 2 == 1 ? 1f : -1f) * (1.5f + 1.2f * (i / 2));
                var unit = _threat.AddZombie(enemy, AreaId(area), x + offset);
                var sprites = ExplorationArt.Get("survivor_idle") != null;
                var body = sprites
                    ? ExplorationArt.Place(_world, "Survivor_" + unit.Id, "survivor_idle", Vector2.zero, 6, Vector2.zero, Color.white)
                    : BuildingView.Rect(_world, "Survivor_" + unit.Id, Vector2.zero, Vector2.one, SurvivorTint, 6);
                ExplorationLook.AddCaster(body, ExplorationLook.Settings.Shadows.People);
                var marker = BuildingView.Rect(_world, "Awareness_" + unit.Id, Vector2.zero, Vector2.one, Color.yellow, 12);
                _zombieViews.Add(new ZombieView
                {
                    Unit = unit, Body = body.transform, BodyRenderer = body.GetComponent<SpriteRenderer>(), Marker = marker.transform,
                    MarkerRenderer = marker.GetComponent<SpriteRenderer>(), Kind = "survivor", Sprites = sprites, Survivor = true, LastX = unit.X,
                });
            }
        }

        /// <summary>생존자 그림이 없을 때의 사각형 색.</summary>
        private static readonly Color SurvivorTint = new Color(0.5f, 0.38f, 0.34f);

        /// <summary>어른이라 주인공보다 조금 크게 그린다.</summary>
        private const float SurvivorScale = 1.12f;

        /// <summary>
        /// 상자 비우기. 처음부터 빈 상자(판 시드)는 겉보기는 같고 뒤져 봐야 비어 있다는 걸 안다.
        /// 고갈 지역에서 이미 뒤진 상자는 수색 완료로 둔다. 반환: 내용물을 넣지 않는가.
        /// </summary>
        private bool Deplete(ExplorationTarget target, string key)
        {
            if (!Location.Tutorial && !(target.Container != null && target.Container.Guaranteed) && ContainerRules.BaselineEmpty(Request.WorldSeed, key, _balance.BaseEmptyContainerRate))
            {
                return true;
            }

            if (!Location.DepletingContainers)
            {
                return false;
            }

            target.LootKey = key;
            if (!Request.LootedContainers.Contains(key))
            {
                return false;
            }

            target.SearchProgress = target.SearchSeconds;
            target.MarkSearched();
            target.LootKey = null;
            return true;
        }

        /// <summary>생존자에게 붙잡힘: 조작을 멈추고 성향에 맞는 문장을 띄운 뒤 암전 → 귀환.</summary>
        private void BeginConfrontation()
        {
            _hostileOutcome = HostileRules.OutcomeFor(Request.HostileTemperament);
            _confrontTimer = 0f;
            CloseContainer();
            Nearest = null;
            _log.HostileOutcome = _hostileOutcome.ToString();
            _hud?.ShowHold(Loc.T("exp.hostile." + _hostileOutcome));
        }

        private void TickConfrontation(float dt)
        {
            _confrontTimer += dt;
            ScreenFade = Mathf.Clamp01(_confrontTimer / ConfrontSeconds);
            if (_fadeOverlay != null)
            {
                _fadeOverlay.color = new Color(0f, 0f, 0f, ScreenFade);
            }

            _hud?.Refresh();
            if (_confrontTimer >= ConfrontSeconds + 0.6f)
            {
                Finish(ExtractStatus.Extracted);
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
            if (events.CaughtBy != null && !Invincible)
            {
                BeginConfrontation();
                return;
            }

            if (events.Damage > 0 && !Invincible)
            {
                var damage = Mathf.Max(1, Mathf.RoundToInt(events.Damage * Request.HarmMultiplier * (1f - Mathf.Clamp01(_armor?.DamageReduction ?? 0f))));
                _log.DamageTaken += damage;
                Hp = Math.Max(0, Hp - damage);
                if (Asleep)
                {
                    // 잠든 사이 물리면 아픔에 바로 깬다 (연타하지 않아도 된다)
                    WakeUp("exp.woke_bitten");
                }
                else
                {
                    _hud?.ShowMessage("exp.hurt");
                }

                Sfx("sfx_z_grab", 0.7f);
                Sfx("sfx_hurt");
                if (Hp <= 0)
                {
                    Sfx("sfx_down");
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
                Sfx("sfx_noise_sting", 0.8f);
                _hud?.ShowMessage("exp.crowd_woke");
            }
        }

        private void TriggerDuskAwakening()
        {
            DuskAwakened = true;
            _threat.AwakenAll();
            SetAmbient(0.2f, new Color(0.75f, 0.6f, 0.65f));
            _lighting?.SetPhase(ExplorationLighting.Phase.Dusk);
            Sfx("sfx_timer_warning");
            Presentation.Audio.Sound.Play("Sfx/sfx_z_horde_far", Presentation.Audio.SoundCategory.Ambience);
            _hud?.ShowMessage("exp.dusk");
        }

        /// <summary>
        /// 문 앞 감각: W(누르고 있기) 문틈으로 살피기, S(누르고 있기) 귀 기울이기. 처리 중이면 true (이동하지 않음).
        /// </summary>
        private bool HandleDoorSenses(float dt)
        {
            var door = Nearest != null && Nearest.IsDoorway ? Nearest : null;
            if (door != null && (_input.Peek || door == _debugPeek))
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
            if (door != null && _input.Listen)
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

            if (area.IsOutdoor)
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
            if (PeekTarget == null && !CurrentArea.IsOutdoor)
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
            return FullVision || PlayerViewRules.CanPlayerSee(areaId, mappedPlayerX, _facingRight ? 1 : -1, IsDarkIn(_areasById[areaId]), FlashlightOn && !Hidden, areaId, unit.X, _balance);
        }

        private void PlaceSilhouette(int index, ExplorationArea area, float x, bool dormant)
        {
            while (_silhouettes.Count <= index)
            {
                var sil = ExplorationArt.Get("silhouette") != null
                    ? ExplorationArt.Place(_world, "Silhouette", "silhouette", Vector2.zero, -3, Vector2.zero, Color.clear)
                    : BuildingView.Rect(_world, "Silhouette", Vector2.zero, Vector2.one, BuildingView.SilhouetteColor, -3);
                var silRenderer = sil.GetComponent<SpriteRenderer>();
                silRenderer.color = BuildingView.SilhouetteColor;
                // 창틀 밖으로 삐져나오지 않게 창 유리 모양(WindowMask) 안에서만 보인다
                silRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                _silhouettes.Add(sil);
            }

            // 유리 바로 위 (복도에서 본 방 창 -4, 방 안 창 -2): 유리 뒤에 그리면 유리에 가려 창 밖 벽에만 비친다
            _silhouettes[index].GetComponent<SpriteRenderer>().sortingOrder = area.IsRoom ? -1 : -3;

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
            _lighting?.SetPhase(ExplorationLighting.Phase.Dawn);
            Sfx("sfx_timer_warning");
            _hud?.ShowMessage("exp.dawn");
        }

        /// <summary>밸런스 값은 체감 밝기(화면 기준). 선형 색 공간에서는 감마를 풀어야 0.12가 실제로 어둡게 보인다.</summary>
        private float AmbientLightIntensity => QualitySettings.activeColorSpace == ColorSpace.Linear
            ? Mathf.Pow(_ambientIntensity * _areaFactor, 2.2f)
            : _ambientIntensity * _areaFactor;

        /// <summary>건물 바깥 배경(조명을 받지 않으므로 밝기에 맞춰 직접 어둡게 한다).</summary>
        private static readonly Color SkyColor = new Color(0.07f, 0.08f, 0.09f);

        private void SetAmbient(float intensity, Color color)
        {
            _ambientIntensity = intensity;
            if (_camera != null)
            {
                _camera.backgroundColor = FullVision ? SkyColor : Color.Lerp(Color.black, SkyColor, intensity);
            }
            if (_light != null)
            {
                _light.intensity = FullVision ? 1f : AmbientLightIntensity;
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
                SetAmbient(_ambientIntensity, _light.color);
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

            // 보이는 맛 비교 (기록에 영향 없음): 후처리·빛 기둥 켜고 끄기
            if (keyboard.f9Key.wasPressedThisFrame)
            {
                ToggleLook();
                _hud?.ShowMessage(LookOn ? "exp.look_on" : "exp.look_off");
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
                _log.DeepSeconds = _deepSeconds;
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
            if (_floodedFloor >= 0 && area.Floor == _floodedFloor)
            {
                radius *= _balance.FloodNoiseMultiplier;   // 물 첨벙이는 소리
            }

            if (area.IsDeep)
            {
                radius *= _balance.DeepNoiseMultiplier;   // 깊은 곳은 소리가 울린다
            }

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

        // ── 소리 (M6-5) ──

        private readonly Dictionary<int, ZombieState> _heardStates = new Dictionary<int, ZombieState>();

        /// <summary>주인공 자리에서 나는 소리.</summary>
        private static void Sfx(string clip, float volume = 1f)
        {
            Presentation.Audio.Sound.Play("Sfx/" + clip, Presentation.Audio.SoundCategory.Sfx, volume);
        }

        /// <summary>
        /// 매 프레임: 같은 구역 좀비가 쫓기 시작하면 유형별 경계 소리, 기절하면 비틀 소리.
        /// 반복 소리 — 숨으면 숨소리, 쫓기면 심장 소리, 같은 구역에 잠든 무리가 있으면 숨소리 무리.
        /// </summary>
        private void UpdateSounds()
        {
            var chased = false;
            var dormantNear = false;
            foreach (var view in _zombieViews)
            {
                var unit = view.Unit;
                var here = unit.AreaId >= 0 && unit.AreaId < _areasById.Count && _areasById[unit.AreaId] == CurrentArea;
                _heardStates.TryGetValue(unit.Id, out var before);
                if (here && !view.Survivor && unit.State != before)
                {
                    if (unit.State == ZombieState.Chase && before != ZombieState.Chase)
                    {
                        Presentation.Audio.Sound.PlayNear($"Sfx/sfx_z_{view.Kind}_alert", unit.X - X);
                    }
                    else if (unit.State == ZombieState.Stunned)
                    {
                        Presentation.Audio.Sound.PlayNear("Sfx/sfx_z_stun", unit.X - X, 12f, Presentation.Audio.SoundCategory.Sfx, 0.7f);
                    }
                }

                _heardStates[unit.Id] = unit.State;
                chased |= here && unit.State == ZombieState.Chase;
                dormantNear |= here && unit.State == ZombieState.Dormant && Mathf.Abs(unit.X - X) < 14f;
            }

            if (Hidden) Presentation.Audio.Sound.Loop("hide", "Sfx/sfx_hide_breath", Presentation.Audio.SoundCategory.Sfx, 0.5f);
            else Presentation.Audio.Sound.StopLoop("hide");
            if (chased) Presentation.Audio.Sound.Loop("tension", "Sfx/sfx_heartbeat", Presentation.Audio.SoundCategory.Sfx, 0.7f);
            else Presentation.Audio.Sound.StopLoop("tension");
            if (dormantNear) Presentation.Audio.Sound.Loop("crowd", "Sfx/sfx_z_dormant_crowd", Presentation.Audio.SoundCategory.Ambience);
            else Presentation.Audio.Sound.StopLoop("crowd");

            // 깊은 곳 환경음 (에셋 8차): 침수된 층은 물소리, 그 밖의 깊은 곳은 지하 통로 울림
            var deepAmb = CurrentArea == null || !CurrentArea.IsDeep ? null : InFlood ? "Sfx/amb_flooded" : "Sfx/amb_deep";
            if (deepAmb != _deepAmbience)
            {
                Presentation.Audio.Sound.StopLoop("deep");
                if (deepAmb != null) Presentation.Audio.Sound.Loop("deep", deepAmb, Presentation.Audio.SoundCategory.Ambience);
                _deepAmbience = deepAmb;
            }

            // 깊은 문 앞에 서면 문틈 바람 한 번
            var deepDoor = Nearest != null && Nearest.Kind == TargetKind.Connector && Nearest.Connector.Kind == ConnectorKind.DeepDoor ? Nearest : null;
            if (deepDoor != null && deepDoor != _draftDoor)
            {
                Sfx("sfx_deep_draft", 0.6f);
            }

            _draftDoor = deepDoor;
        }

        private string _deepAmbience;
        private ExplorationTarget _draftDoor;

        /// <summary>
        /// 수색 소리: E를 누르고 수색하는 동안만 반복해서 울리고, 다 뒤지거나 손을 떼거나 화면이 멈춘 프레임에 바로 끊는다.
        /// (예전에는 0.5초마다 한 번씩 울려서 수색이 끝난 뒤에도 마지막 소리가 끝까지 남았다)
        /// </summary>
        private void LateUpdate()
        {
            if (_searching && !IsFinished)
            {
                Presentation.Audio.Sound.Loop("search", "Sfx/sfx_search", Presentation.Audio.SoundCategory.Sfx, 0.6f);
            }
            else
            {
                Presentation.Audio.Sound.StopLoop("search");
            }
        }

        private void OnDestroy()
        {
            if (_pad != null)
            {
                Loc.Variant = null;
            }

            _look.Dispose();
            Presentation.Audio.Sound.StopAllLoops();
        }

        // ── 기물 안개: 어둠 속 먼 기물(장식·수색할 곳·인물)은 좀비처럼 보이지 않는다 ──

        private sealed class FogProp
        {
            public SpriteRenderer Renderer;
            public ExplorationArea Area;
            public float Min;
            public float Max;
            public float Alpha = 1f;
        }

        private readonly List<FogProp> _fogProps = new List<FogProp>();
        private ExplorationArea _fogShown;

        /// <summary>안개를 따를 기물 하나 (자리는 그림 테두리의 가로 범위, 공간 기준 좌표).</summary>
        private void AddFog(GameObject view, ExplorationArea area)
        {
            var renderer = view != null ? view.GetComponent<SpriteRenderer>() : null;
            if (renderer == null || area == null)
            {
                return;
            }

            var bounds = renderer.bounds;
            var min = _world.InverseTransformPoint(bounds.min).x - area.Origin.x;
            var max = _world.InverseTransformPoint(bounds.max).x - area.Origin.x;
            _fogProps.Add(new FogProp { Renderer = renderer, Area = area, Min = Mathf.Min(min, max), Max = Mathf.Max(min, max) });
        }

        /// <summary>흩어 놓은 장식(Decor_*)·뒷벽 장식(WallDecor). area가 없으면 복도: 높이로 층을 찾는다.</summary>
        private void CollectFog(Transform parent, ExplorationArea area)
        {
            foreach (Transform child in parent)
            {
                if (!child.name.StartsWith("Decor_", StringComparison.Ordinal) && child.name != "WallDecor")
                {
                    continue;
                }

                var target = area;
                if (target == null)
                {
                    var floor = Mathf.Clamp(Mathf.FloorToInt(child.localPosition.y / _building.FloorHeight), 0, _corridors.Count - 1);
                    target = _corridors[floor];
                }

                AddFog(child.gameObject, target);
                if (child.name != "WallDecor")
                {
                    // 바닥에 선 큰 장식만 (벽에 걸린 게시판 등은 뺀다)
                    ExplorationLook.AddCaster(child.gameObject, ExplorationLook.Settings.Shadows.Decor, _world.TransformPoint(target.Origin).y);
                }
            }
        }

        private bool FogVisible(FogProp prop)
        {
            if (FullVision || DebugOverlay)
            {
                return true;
            }

            if (PeekTarget != null)
            {
                // 문틈 너머: 밝은 낮 복도는 다 보이고, 어두운 곳은 문 가까이만 (밤에는 아무것도)
                if (prop.Area != PeekArea || !IsDarkIn(PeekArea))
                {
                    return true;
                }

                var range = PeekViewRange;
                var door = PeekTarget.LinkX;
                return range > 0f && Mathf.Abs(Mathf.Clamp(door, prop.Min, prop.Max) - door) <= range;
            }

            if (prop.Area != CurrentArea)
            {
                return true;
            }

            // 좀비와 같은 시야 규칙: 어둠 속에서는 맨눈 거리 안이거나 손전등이 비추는 쪽만 (넓은 기물은 가장 가까운 끝으로 잰다)
            var nearest = Mathf.Clamp(X, prop.Min, prop.Max);
            return PlayerViewRules.CanPlayerSee(0, X, _facingRight ? 1 : -1, DarkHere, FlashlightOn && !Hidden, 0, nearest, _balance);
        }

        private void UpdateFog(float dt)
        {
            // 보이는 공간이 바뀌면(문을 지나거나 문틈을 볼 때) 바로 맞추고, 그 안에서는 천천히 나타나고 사라진다
            var shown = PeekTarget != null ? PeekArea : CurrentArea;
            var snap = shown != _fogShown;
            _fogShown = shown;
            var step = dt * _balance.FogFadeSpeed;
            foreach (var prop in _fogProps)
            {
                if (prop.Renderer == null)
                {
                    continue;
                }

                var wanted = FogVisible(prop) ? 1f : 0f;
                prop.Alpha = snap ? wanted : Mathf.MoveTowards(prop.Alpha, wanted, step);
                var color = prop.Renderer.color;
                if (!Mathf.Approximately(color.a, prop.Alpha))
                {
                    prop.Renderer.color = new Color(color.r, color.g, color.b, prop.Alpha);
                }

                prop.Renderer.enabled = prop.Alpha > 0.01f;
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
                if (view.Survivor)
                {
                    var walking = Mathf.Abs(unit.X - view.LastX) > 0.0005f;
                    if (walking)
                    {
                        view.MoveTime += Time.deltaTime;
                    }

                    if (view.Sprites)
                    {
                        if (walking) view.BodyRenderer.flipX = unit.X < view.LastX;
                        var running = unit.State == ZombieState.Chase;
                        var survivorPrefix = running ? "survivor_run_" : "survivor_walk_";
                        var survivorCount = ExplorationArt.FrameCount(survivorPrefix, 4);
                        var frame = (int)(view.MoveTime * (running ? 12f : 7f)) % survivorCount;
                        var survivorKey = ReactFrame(view, "survivor_") is string survivorReact ? survivorReact : unit.State == ZombieState.Stunned ? LoopFrame("survivor_stun_", Time.time, PixelZombieIdleFps, unit.Id * 3) ?? "survivor_stun"
                            : !walking ? LoopFrame("survivor_idle_", Time.time, PixelIdleFps, unit.Id * 5) ?? "survivor_idle"
                            : LoopFrame(survivorPrefix, view.MoveTime, running ? PixelRunFps : PixelWalkFps) ?? survivorPrefix + frame;
                        view.BodyRenderer.sprite = ExplorationArt.Get(survivorKey);
                        view.Body.localPosition = new Vector3(origin.x + unit.X, origin.y + 0.15f, 0f);
                        view.Body.localScale = Vector3.one * (ExplorationArt.HasPixel("survivor_idle") ? 1f : SurvivorScale);
                    }
                    else
                    {
                        view.Body.localPosition = new Vector3(origin.x + unit.X - 0.3f, origin.y + 0.15f, 0f);
                        view.Body.localScale = new Vector3(0.6f, 1.8f, 1f);
                    }

                    view.LastX = unit.X;
                    view.BodyRenderer.color = view.Sprites ? Color.white : unit.State == ZombieState.Stunned ? new Color(0.45f, 0.45f, 0.6f) : SurvivorTint;
                    goto Marker;
                }

                if (view.Sprites)
                {
                    var moved = Mathf.Abs(unit.X - view.LastX) > 0.0005f;
                    if (moved)
                    {
                        view.MoveTime += Time.deltaTime;
                        view.BodyRenderer.flipX = unit.X < view.LastX;
                    }

                    view.LastX = unit.X;
                    // 픽셀 아트는 동작마다 프레임 수가 다르다 (없으면 코드 그림 2프레임). 한 바퀴 빠르기는 그대로 둔다.
                    var chasing = unit.State == ZombieState.Chase;
                    var kindPrefix = $"zombie_{view.Kind}_";
                    var poseName = dormant ? "dormant_" : chasing ? "chase_" : "walk_";
                    var poseCount = ExplorationArt.FrameCount(kindPrefix + poseName, 2);
                    var phase = (int)((dormant ? Time.time * 0.8f : view.MoveTime * (chasing ? 7f : 4f)) + unit.Id) % poseCount;

                    // 픽셀 아트: 상태마다 16프레임 안팎 (잠든 숨·서성임·기절 비틀거림까지). 없으면 코드 그림 2프레임
                    var zombieKey = ReactFrame(view, kindPrefix) is string zombieReact ? zombieReact : unit.State == ZombieState.Stunned ? LoopFrame(kindPrefix + "stun_", Time.time, PixelZombieIdleFps * 1.6f, unit.Id * 3) ?? kindPrefix + "stun"
                        : dormant ? LoopFrame(kindPrefix + "sleep_", Time.time, PixelZombieIdleFps * 0.6f, unit.Id * 7)
                            ?? LoopFrame(kindPrefix + "dormant_", Time.time, PixelZombieIdleFps * 0.6f, unit.Id * 7) ?? kindPrefix + "dormant_" + phase
                        : chasing ? LoopFrame(kindPrefix + "chase_", view.MoveTime, PixelZombieChaseFps, unit.Id) ?? kindPrefix + "chase_" + phase
                        : moved ? LoopFrame(kindPrefix + "walk_", view.MoveTime, PixelZombieWalkFps, unit.Id) ?? kindPrefix + "walk_" + phase
                        : LoopFrame(kindPrefix + "idle_", Time.time, PixelZombieIdleFps, unit.Id * 5) ?? kindPrefix + "walk_0";
                    view.BodyRenderer.sprite = ExplorationArt.Get(zombieKey);
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
            public bool Survivor;
            public float LastX;
            public float MoveTime;

            /// <summary>맞거나 밀린 반응 동작 ("hit_" / "pushed_")과 시작한 때. 비어 있으면 없음.</summary>
            public string ReactPose = "";
            public float ReactStart;
        }

        /// <summary>맞거나 밀린 좀비·생존자에게 반응 동작을 건다 (그림이 없으면 기절 동작만 보인다).</summary>
        private void React(ZombieUnit unit, string pose)
        {
            var view = _zombieViews.Find(v => v.Unit == unit);
            if (view != null)
            {
                view.ReactPose = pose;
                view.ReactStart = Time.time;
            }
        }

        /// <summary>반응 동작 중이면 그 프레임 키 (끝났거나 그림이 없으면 null). 반응 중에는 주인공 쪽을 본다.</summary>
        private string ReactFrame(ZombieView view, string prefix)
        {
            if (string.IsNullOrEmpty(view.ReactPose))
            {
                return null;
            }

            var progress = (Time.time - view.ReactStart) / ReactAnimSeconds;
            var key = progress < 1f ? ProgressFrame(prefix + view.ReactPose, progress) : null;
            if (key == null)
            {
                view.ReactPose = "";
                return null;
            }

            view.BodyRenderer.flipX = X < view.Unit.X;
            return key;
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
