using System;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.State;
using ApoGirl.Core.Rules;
using ApoGirl.Presentation.Art;
using ApoGirl.Presentation.Audio;
using ApoGirl.Presentation.Text;
using ApoGirl.Systems.Session;
using UnityEngine;
using UnityEngine.UIElements;

namespace ApoGirl.Presentation.Screens
{
    /// <summary>허브 단면도 배치 표 (Resources/Art/hub_section.json). 길이 단위는 그림 픽셀(Scale배로 그린다).</summary>
    [Serializable]
    public sealed class HubSectionConfig
    {
        [Serializable]
        public sealed class PropDef
        {
            public string Key = "";
            public float X;

            /// <summary>바닥에서 띄울 높이 (벽에 거는 것).</summary>
            public float Y;
        }

        [Serializable]
        public sealed class RoomDef
        {
            public string Id = "";
            public string NameKey = "";

            /// <summary>방을 눌렀을 때 패널에 보이는 일러스트 (Resources/Art 아래, 없으면 생략).</summary>
            public string Art = "";
            public int Floor;
            public float X;
            public float Width = 150;
            public string Wall = "";
            public string FloorTex = "";

            /// <summary>열리는 조건: 시설 단계가 1 이상 / 상태 플래그 / 늘 열림.</summary>
            public string Facility = "";
            public string Flag = "";
            public bool Always;

            /// <summary>옥상 (벽 없이 하늘 아래).</summary>
            public bool Roof;
            public PropDef[] Props = Array.Empty<PropDef>();
            public string[] Actions = Array.Empty<string>();

            /// <summary>이 방에 지어 놓은 설비 그림 (조건이 맞으면 방 패널의 방 그림 위에 겹친다).</summary>
            public BuiltDef[] Built = Array.Empty<BuiltDef>();

            /// <summary>지어 놓으면 단면도에 실제로 놓이는 구조물 (옥상 빗물받이·텃밭·안테나 등, 조건은 Flag/MinDefense).</summary>
            public BuiltPropDef[] BuiltProps = Array.Empty<BuiltPropDef>();

            /// <summary>CD 진열장 (기획 A): 벽에 칸이 보이고, 방을 누르면 CD 플레이어가 뜬다. X·Y는 방 안 그림 픽셀 (Y는 바닥에서 위로).</summary>
            public bool CdShelf;
            public float CdShelfX = 90;
            public float CdShelfY = 30;
        }

        [Serializable]
        public sealed class BuiltPropDef
        {
            public string Flag = "";
            public int MinDefense;
            public string Key = "";
            public float X;
            public float Y;
        }

        /// <summary>방 사이 벽과 문, 건물 바깥 벽.</summary>
        [Serializable]
        public sealed class WallDef
        {
            public float Width = 8;
            public float OuterWidth = 10;
            public string OuterTex = "wall_brick";
            /// <summary>
            /// 고전 단면도 방식: 닫힌 문은 벽 속의 가는 선, 열리면 문 높이만큼 벽이 비고 문짝이 한쪽으로 젖혀져 보인다.
            /// 높이는 인물 키에 맞춘다 (학생 64, 선생님 75 그림 픽셀).
            /// </summary>
            public float DoorHeight = 70;
            public float DoorLine = 2;
            public float DoorPanel = 14;

            /// <summary>걷는 사람이 이만큼 가까우면 열린다 (그림 픽셀), 지나간 뒤 이만큼 있다 닫힌다 (초).</summary>
            public float OpenDistance = 26;
            public float CloseDelay = 0.35f;
        }

        [Serializable]
        public sealed class BuiltDef
        {
            /// <summary>보이는 조건: 플래그 / 방어도 이상 (둘 다 비면 늘).</summary>
            public string Flag = "";
            public int MinDefense;
            public string Image = "";
        }

        [Serializable]
        public sealed class KeyValue
        {
            public string Category = "";
            public string Role = "";
            public string Npc = "";
            public string Room = "";
            public string Key = "";
        }

        [Serializable]
        public sealed class TeacherDef
        {
            public string Seated = "";
            public string Crutch = "";
            public string Cane = "";
            public string Ill = "";
            public string[] IllFlags = Array.Empty<string>();
            public string[] GoneFlags = Array.Empty<string>();
            public string IsolatedFlag = "";
            public string IsolatedRoom = "";

            /// <summary>역할 재기획 3.1: 선생님은 역할 없이 이 방(교실)에 있다. 닫혀 있으면 지내는 곳.</summary>
            public string Room = "";
        }

        public int Scale = 2;
        public float Width = 780;
        public float FloorHeight = 120;
        public float RoofHeight = 96;
        public float Ground = 10;
        public float StairsX = 732;
        public float StairsWidth = 48;
        public float WalkSpeed = 110;
        public string ShelterFlag = "";
        public string ShelterRoom = "";

        /// <summary>동료 탐험: 나가고 들어오는 현관 방.</summary>
        public string Entrance = "ENTRANCE";
        public string DefaultShelter = "";
        public PropDef[] ShelterProps = Array.Empty<PropDef>();
        public RoomDef[] Rooms = Array.Empty<RoomDef>();
        public string[] ShelterActions = Array.Empty<string>();
        public KeyValue[] CategoryRooms = Array.Empty<KeyValue>();
        public KeyValue[] NpcSprites = Array.Empty<KeyValue>();
        public TeacherDef Teacher = new TeacherDef();

        public WallDef Walls = new WallDef();

        /// <summary>계단실 그림: 아래층들(오르는 계단), 맨 위층(옥상 문).</summary>
        public string StairsUp = "prop_stairs_up";
        public string StairsTop = "prop_stairs_down";

        /// <summary>옥상 난간 위 철조망 (가로로 이어 붙인다).</summary>
        public string RoofFence = "";

        /// <summary>단면도 뒤 바깥 풍경 (마을·하늘·날씨).</summary>
        public HubSkyConfig Sky = new HubSkyConfig();

        private static HubSectionConfig _loaded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLoaded() => _loaded = null;

        public static HubSectionConfig Load()
        {
            if (_loaded == null)
            {
                var asset = Resources.Load<TextAsset>("Art/hub_section");
                _loaded = asset != null ? JsonUtility.FromJson<HubSectionConfig>(asset.text) ?? new HubSectionConfig() : new HubSectionConfig();
            }

            return _loaded;
        }
    }

    /// <summary>
    /// 허브 0겹: 학교 단면도 (Docs/UI_REDESIGN_PROPOSAL.md 2단계). 방은 시설이 열리면 밝아지고, 사람은 행동에 맞는 방으로 걸어간다.
    /// 그림은 탐험 픽셀 아트(Resources/Art/ExplorationPixel)를 Scale배로 그린다. 규칙은 모르고 상태를 읽어 그리기만 한다.
    /// </summary>
    internal sealed class HubSectionView
    {
        private const string Pixel = "ExplorationPixel/";
        private const float FrameSeconds = 1f / 7f;

        private readonly VisualElement _host;
        private readonly GameSession _session;
        private readonly HubSectionConfig _config = HubSectionConfig.Load();
        private readonly Dictionary<string, RoomView> _rooms = new Dictionary<string, RoomView>();
        private readonly List<Actor> _actors = new List<Actor>();
        private readonly Dictionary<string, Texture2D[]> _frames = new Dictionary<string, Texture2D[]>();
        private readonly VisualElement _sky;
        private readonly VisualElement _building;
        private readonly VisualElement _actorLayer;
        private readonly VisualElement _roomPanel;
        private readonly HubSkyView _skyView;
        private readonly List<DoorView> _doors = new List<DoorView>();
        private Actor _player;
        private Actor _teacher;
        private string _builtSignature;
        private string _cdRoomId = "";
        private DayPhase _phase = DayPhase.Day;
        private bool _executing;

        /// <summary>이번 낮 칸에 밖에 나간 동료 (밤 칸이 시작되면 현관으로 돌아온다).</summary>
        private readonly HashSet<string> _outToday = new HashSet<string>();

        public HubSectionView(VisualElement host, GameSession session)
        {
            _host = host;
            _session = session;
            _sky = Add(_host, "hub-sky");
            _sky.pickingMode = PickingMode.Ignore;
            var skyOrigin = Add(_sky, "hub-sky-origin");
            _skyView = new HubSkyView(skyOrigin, _host.parent ?? _host, _config.Sky, _config.Scale);
            _building = Add(_host, "hub-building");
            _actorLayer = Add(_host, "hub-actors");
            _actorLayer.pickingMode = PickingMode.Ignore;
            _roomPanel = Add(_host, "hub-room-panel");
            _roomPanel.AddToClassList("hidden");
            _host.schedule.Execute(Tick).Every(33);
            _host.RegisterCallback<ClickEvent>(_ => HideRoomPanel());
            _roomPanel.RegisterCallback<ClickEvent>(evt => evt.StopPropagation()); // 패널 안(CD 플레이어 등)을 눌러도 닫히지 않게
        }

        /// <summary>방에서 행동을 골랐을 때 (허브가 계획 보드를 그 행동으로 연다).</summary>
        public Action<string> ActionPicked { get; set; }

        private float Px(float art) => art * _config.Scale;

        private float ArtHeight => _config.FloorHeight * 3 + _config.RoofHeight + _config.Ground;

        /// <summary>그 층 바닥의 높이 (단면도 위에서부터, 그림 픽셀).</summary>
        private float FloorBottom(int floor) => ArtHeight - _config.Ground - floor * _config.FloorHeight;

        // ── 상태 반영 ──

        public void Refresh()
        {
            var state = _session.State;
            if (state == null)
            {
                return;
            }

            var signature = Signature(state);
            if (signature != _builtSignature)
            {
                _builtSignature = signature;
                Build(state);
            }

            RefreshActors(state, !_executing);
            ApplyLight();
        }

        /// <summary>낮·밤과 실행 중인지 (밤에는 방이 어두워지고 사람 있는 방만 불빛이 남는다).</summary>
        public void SetTime(DayPhase phase, bool executing)
        {
            var changed = executing && (phase != _phase || !_executing);
            _phase = phase;
            _executing = executing;
            if (!executing)
            {
                // 실행이 끝나면 모두 제자리 (외출 표시도 지운다)
                _outToday.Clear();
                foreach (var actor in _actors)
                {
                    actor.SetAway(false);
                }

                UpdateAwayMarks();
            }

            if (changed)
            {
                // 동료 탐험 (역할 재기획 5.4절): 낮 칸 시작에 그날 나가는 사람은 현관으로 걸어가 사라지고, 밤 칸 시작에 현관으로 돌아온다
                var returning = new HashSet<string>(_outToday);
                _outToday.Clear();
                var state = _session.State;
                if (phase == DayPhase.Day && state != null)
                {
                    foreach (var pair in state.Npcs.Where(n => n.Value.Status == NpcStatus.Joined && n.Value.OutingDays.Contains(state.Calendar.Day)))
                    {
                        _outToday.Add(pair.Key);
                    }
                }

                var door = EntranceSpot();
                // 밤에는 경비 말고는 지내는 곳으로 돌아가 쉬고, 낮에는 맡은 방으로 간다
                foreach (var actor in _actors.Where(a => a.Kind == "npc"))
                {
                    if (_outToday.Contains(actor.Id) && door.HasValue)
                    {
                        actor.WalkTo(door.Value.Floor, door.Value.X, this);
                        actor.HideOnArrive = true;
                        continue;
                    }

                    if (returning.Contains(actor.Id) && door.HasValue)
                    {
                        actor.SetAway(false);
                        actor.Place(door.Value.Floor, door.Value.X);
                    }

                    var night = phase == DayPhase.Night && !actor.Guard;
                    actor.WalkTo(night ? actor.HomeFloor : actor.PostFloor, night ? actor.HomeX : actor.PostX, this);
                }

                UpdateAwayMarks();
            }

            ApplyLight();
        }

        /// <summary>
        /// 실행 중 한 칸: 주인공이 그 행동의 방으로 걸어간다 (가르치는 수업이면 선생님도). 걸리는 시간(초)을 돌려준다.
        /// </summary>
        public float Act(string actionId)
        {
            HideRoomPanel();
            if (_player == null || string.IsNullOrEmpty(actionId))
            {
                return 0f;
            }

            var room = RoomFor(actionId);
            if (room == null)
            {
                return 0f;
            }

            var seconds = _player.WalkTo(room.Def.Floor, room.Def.X + room.Def.Width * 0.42f, this);
            if (_teacher != null && _teacher.CanWalk)
            {
                // 가르치는 수업이면 그 방으로, 아니면 지내는 곳으로
                var teaching = _session.Definitions.TryGetAction(actionId, out var action) && action.Category == ActionCategory.Education
                               && room.Def.Id != Shelter(_session.State);
                if (teaching)
                {
                    _teacher.WalkTo(room.Def.Floor, room.Def.X + room.Def.Width * 0.72f, this);
                }
                else
                {
                    _teacher.WalkTo(_teacher.HomeFloor, _teacher.HomeX, this);
                }
            }

            return seconds;
        }

        /// <summary>현관 안쪽, 바깥문 가까운 자리 (현관 방이 없으면 null).</summary>
        private (int Floor, float X)? EntranceSpot()
        {
            if (string.IsNullOrEmpty(_config.Entrance) || !_rooms.TryGetValue(_config.Entrance, out var room))
            {
                return null;
            }

            return (room.Def.Floor, room.Def.X + Mathf.Min(14f, room.Def.Width * 0.2f));
        }

        /// <summary>밖에 나간 동료가 맡은 방은 이름 옆에 "외출" (방이 비어 보이는 까닭).</summary>
        private void UpdateAwayMarks()
        {
            var awayRooms = new HashSet<string>(_actors.Where(a => a.Kind == "npc" && _outToday.Contains(a.Id) && !string.IsNullOrEmpty(a.PostRoom))
                .Select(a => a.PostRoom));
            foreach (var room in _rooms.Values)
            {
                if (room.Name == null || !room.Open)
                {
                    continue;
                }

                var away = awayRooms.Contains(room.Def.Id);
                room.Name.text = away ? RoomName(room.Def) + "  " + Loc.T("hub.room_away") : RoomName(room.Def);
                room.Element.EnableInClassList("away", away);
            }
        }

        private string Signature(GameState state)
        {
            var parts = new List<string> { Shelter(state) };
            foreach (var room in _config.Rooms)
            {
                parts.Add(IsOpen(state, room) ? "1" : "0");
                foreach (var built in room.BuiltProps)
                {
                    parts.Add(BuiltShown(state, built) ? "b" : "-");
                }
            }

            parts.Add("cd" + CdRules.Count(state));

            return string.Join(",", parts);
        }

        private bool IsOpen(GameState state, HubSectionConfig.RoomDef room)
        {
            if (room.Always)
            {
                return true;
            }

            if (!string.IsNullOrEmpty(room.Flag) && state.Flags.Contains(room.Flag))
            {
                return true;
            }

            return !string.IsNullOrEmpty(room.Facility) && state.Base.Facilities.TryGetValue(room.Facility, out var level) && level > 0;
        }

        private string Shelter(GameState state) =>
            !string.IsNullOrEmpty(_config.ShelterFlag) && state.Flags.Contains(_config.ShelterFlag) ? _config.ShelterRoom : _config.DefaultShelter;

        private RoomView RoomFor(string actionId)
        {
            var state = _session.State;
            string target = null;
            if (_config.ShelterActions.Contains(actionId))
            {
                target = Shelter(state);
            }

            target ??= _config.Rooms.FirstOrDefault(r => r.Actions.Contains(actionId))?.Id;
            if (target == null && _session.Definitions.TryGetAction(actionId, out var action))
            {
                var mapped = _config.CategoryRooms.FirstOrDefault(c => c.Category == action.Category.ToString())?.Room;
                target = mapped == "@shelter" ? Shelter(state) : mapped;
            }

            // 아직 열지 않은 방의 일은 지내는 곳에서 한다
            if (target == null || !_rooms.TryGetValue(target, out var room) || !room.Open)
            {
                _rooms.TryGetValue(Shelter(state), out room);
            }

            return room;
        }

        // ── 그리기 ──

        private void Build(GameState state)
        {
            _building.Clear();
            _rooms.Clear();
            HideRoomPanel();
            _host.style.width = Px(_config.Width);
            _host.style.height = Px(ArtHeight);

            // 건물 외벽 (벽돌) 과 땅
            var shellTop = FloorBottom(2) - _config.FloorHeight;
            var shell = Place(_building, "hub-shell", -8, shellTop - 6, _config.Width + 16, _config.FloorHeight * 3 + 6);
            Tile(shell, "wall_brick");
            Place(_building, "hub-ground", -400, ArtHeight - _config.Ground, _config.Width + 800, _config.Ground + 40);
            _skyView.Build(_config.Width, shellTop - 6, ArtHeight - _config.Ground);

            // 계단실 (오른쪽 끝, 층마다): 아래층은 오르는 계단, 맨 위층은 옥상 문. 그림이 층 높이면 칸을 채운다
            for (var floor = 0; floor < 3; floor++)
            {
                var stairs = Place(_building, "hub-stairs", _config.StairsX, FloorBottom(floor) - _config.FloorHeight, _config.StairsWidth, _config.FloorHeight);
                var texture = Tex(floor < 2 ? _config.StairsUp : _config.StairsTop) ?? Tex(floor < 2 ? "prop_stairs_up" : "prop_stairs_down");
                if (texture != null)
                {
                    var full = texture.height >= _config.FloorHeight - 10;
                    var art = full
                        ? Place(stairs, "hub-prop", 0, 0, _config.StairsWidth, _config.FloorHeight)
                        : Place(stairs, "hub-prop", (_config.StairsWidth - texture.width) * 0.5f, _config.FloorHeight - texture.height - 6, texture.width, texture.height);
                    if (full)
                    {
                        art.style.backgroundImage = new StyleBackground(texture);
                        art.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(Length.Percent(100), Length.Percent(100)));
                    }
                    else
                    {
                        Image(art, texture);
                    }
                }
            }

            var shelter = Shelter(state);
            foreach (var def in _config.Rooms)
            {
                var open = IsOpen(state, def);
                var top = def.Roof ? FloorBottom(3) - _config.RoofHeight : FloorBottom(def.Floor) - _config.FloorHeight;
                var height = def.Roof ? _config.RoofHeight : _config.FloorHeight;
                var element = Place(_building, "hub-room", def.X, top, def.Width, height);
                element.EnableInClassList("roof", def.Roof);
                var view = new RoomView { Def = def, Element = element, Open = open };
                _rooms[def.Id] = view;

                if (!def.Roof)
                {
                    Tile(element, def.Wall, bottomAligned: true);
                    var floorStrip = Place(element, "hub-floor", 0, height - 10, def.Width, 10);
                    Tile(floorStrip, def.FloorTex, bottomAligned: true);
                    Place(element, "hub-slab", 0, 0, def.Width, 5);
                }
                else
                {
                    // 옥상: 철조망(난간 위, 소품 뒤) + 난간
                    var fence = Tex(_config.RoofFence);
                    if (fence != null)
                    {
                        var strip = Place(element, "hub-fence", 0, height - 16 - fence.height, def.Width, fence.height);
                        strip.pickingMode = PickingMode.Ignore;
                        Tile(strip, _config.RoofFence, bottomAligned: true);
                    }

                    // 옥상 바닥(슬래브): 벽돌 윗면(height - 6)까지 채워 하늘이 비치는 틈이 없게
                    Place(element, "hub-parapet", 0, height - 18, def.Width, 13);
                }

                // 소품 바닥선: 방은 바닥 띠 위, 옥상은 슬래브 윗면에서 조금 들어간 곳 (닿아 보이게)
                var propFloor = def.Roof ? height - 15 : height - 8;
                foreach (var prop in def.Props)
                {
                    AddProp(element, prop, propFloor);
                }

                // 지어 놓은 구조물 (빗물받이·텃밭·안테나 등): 조건이 맞으면 그 자리에 실제로 놓인다
                foreach (var built in def.BuiltProps)
                {
                    if (BuiltShown(state, built))
                    {
                        AddProp(element, new HubSectionConfig.PropDef { Key = built.Key, X = built.X, Y = built.Y }, propFloor);
                    }
                }

                if (def.CdShelf && open)
                {
                    AddCdShelf(state, element, def, propFloor);
                }

                if (def.Id == shelter)
                {
                    foreach (var prop in _config.ShelterProps)
                    {
                        AddProp(element, new HubSectionConfig.PropDef { Key = prop.Key, X = def.Width + prop.X, Y = prop.Y }, height - 8);
                    }
                }

                // 옥상에는 어둡게 덮는 막을 두지 않는다 (하늘·구름·비가 가리지 않게, 밤에는 소품 색만 낮춘다)
                if (!def.Roof)
                {
                    view.Dark = Place(element, "hub-dark", 0, 0, def.Width, height);
                    view.Dark.pickingMode = PickingMode.Ignore;
                    view.Glow = Place(element, "hub-glow", 0, 0, def.Width, height);
                    view.Glow.pickingMode = PickingMode.Ignore;
                }

                var name = new Label(RoomName(def));
                name.AddToClassList("hub-room-name");
                name.pickingMode = PickingMode.Ignore;
                element.Add(name);
                view.Name = name;
                element.EnableInClassList("locked", !open);
                element.EnableInClassList("shelter", def.Id == shelter);
                if (!open)
                {
                    name.text = "?";
                }

                var roomId = def.Id;
                element.RegisterCallback<ClickEvent>(evt =>
                {
                    ShowRoomPanel(roomId);
                    evt.StopPropagation();
                });
            }

            BuildWalls();

        }

        /// <summary>
        /// 층마다 방 사이 벽과 문, 건물 양쪽 바깥 벽. 문은 방과 방, 방과 계단실 사이에 있고 걷는 사람이 지나갈 때 열린다.
        /// </summary>
        private void BuildWalls()
        {
            _doors.Clear();
            var walls = _config.Walls;
            for (var floor = 0; floor < 3; floor++)
            {
                var top = FloorBottom(floor) - _config.FloorHeight;
                var edges = new SortedSet<float> { _config.StairsX };
                foreach (var room in _config.Rooms.Where(r => !r.Roof && r.Floor == floor))
                {
                    if (room.X > 0.5f) edges.Add(room.X);
                    if (room.X + room.Width < _config.StairsX - 0.5f) edges.Add(room.X + room.Width);
                }

                // 바깥 벽 (벽돌)
                foreach (var x in new[] { -walls.OuterWidth + 2, _config.Width - 2 })
                {
                    var outer = Place(_building, "hub-outer-wall", x, top - 6, walls.OuterWidth, _config.FloorHeight + 6);
                    outer.pickingMode = PickingMode.Ignore;
                    Tile(outer, walls.OuterTex);
                }

                // 안쪽 벽과 문: 벽은 문 위(늘)·문 높이(닫혔을 때만) 두 토막, 문은 선(닫힘)과 문짝(열림)
                var floorY = FloorBottom(floor) - 6;
                var doorTop = floorY - walls.DoorHeight;
                foreach (var x in edges)
                {
                    var left = x - walls.Width * 0.5f;
                    var upper = Place(_building, "hub-wall", left, top + 5, walls.Width, doorTop - (top + 5));
                    upper.pickingMode = PickingMode.Ignore;
                    var lower = Place(_building, "hub-wall", left, doorTop, walls.Width, floorY - doorTop + 4);
                    lower.pickingMode = PickingMode.Ignore;
                    var lintel = Place(_building, "hub-door-frame", left - 1, doorTop - 2, walls.Width + 2, 2);
                    lintel.pickingMode = PickingMode.Ignore;
                    var line = Place(_building, "hub-door-line", x - walls.DoorLine * 0.5f, doorTop, walls.DoorLine, floorY - doorTop);
                    line.pickingMode = PickingMode.Ignore;
                    var panel = Place(_building, "hub-door-panel", x + walls.Width * 0.5f, doorTop, walls.DoorPanel, floorY - doorTop);
                    panel.pickingMode = PickingMode.Ignore;
                    panel.AddToClassList("hidden");
                    _doors.Add(new DoorView { Floor = floor, X = x, Lower = lower, Line = line, Panel = panel });
                }
            }
        }

        /// <summary>걷는 사람이 가까우면 문을 열고, 지나간 뒤 잠깐 있다 닫는다.</summary>
        private void TickDoors(float dt)
        {
            if (_doors.Count == 0)
            {
                return;
            }

            var walls = _config.Walls;
            foreach (var door in _doors)
            {
                Actor nearest = null;
                foreach (var actor in _actors)
                {
                    if (actor.Walking && actor.Floor == door.Floor && Mathf.Abs(actor.X - door.X) < walls.OpenDistance
                        && (nearest == null || Mathf.Abs(actor.X - door.X) < Mathf.Abs(nearest.X - door.X)))
                    {
                        nearest = actor;
                    }
                }

                door.Hold = nearest != null ? walls.CloseDelay : Mathf.Max(0f, door.Hold - dt);
                var open = door.Hold > 0f;
                if (nearest != null)
                {
                    // 문짝은 사람이 있는 반대쪽으로 젖혀져 길을 막지 않는다
                    var side = nearest.X < door.X ? 1f : -1f;
                    var left = side > 0 ? door.X + walls.Width * 0.5f : door.X - walls.Width * 0.5f - walls.DoorPanel;
                    door.Panel.style.left = Px(left);
                }

                if (open != door.Open)
                {
                    door.Open = open;
                    door.Lower.EnableInClassList("hidden", open);
                    door.Line.EnableInClassList("hidden", open);
                    door.Panel.EnableInClassList("hidden", !open);
                }
            }
        }

        private static bool BuiltShown(GameState state, HubSectionConfig.BuiltPropDef built) =>
            (string.IsNullOrEmpty(built.Flag) || state.Flags.Contains(built.Flag)) && state.Base.Defense >= built.MinDefense;

        /// <summary>CD 진열장 칸 (기획 A): 처음부터 빈 칸이 다 보이고, 주운 CD는 그 색으로 꽂힌다.</summary>
        private void AddCdShelf(GameState state, VisualElement room, HubSectionConfig.RoomDef def, float floorY)
        {
            const float slotW = 5f, slotH = 8f;
            var slots = CdPlayer.Slots;
            var shelf = Place(room, "hub-cd-shelf", def.CdShelfX, floorY - def.CdShelfY - slotH - 2f, slots * slotW + 2f, slotH + 2f);
            shelf.pickingMode = PickingMode.Ignore;
            for (var i = 0; i < slots; i++)
            {
                var entry = i < CdPlayer.All.Count ? CdPlayer.All[i] : null;
                var slot = Place(shelf, "hub-cd-slot", 1f + i * slotW, 1f, slotW - 1f, slotH);
                slot.pickingMode = PickingMode.Ignore;
                var owned = entry != null && CdRules.Has(state, entry.Id);
                slot.EnableInClassList("owned", owned);
                if (owned && ColorUtility.TryParseHtmlString(entry.Color, out var color))
                {
                    slot.style.backgroundColor = color;
                }
            }
        }

        /// <summary>방송반 패널의 CD 플레이어: 가진 CD를 누르면 틀고, 다시 누르거나 끄기로 멈춘다.</summary>
        private void AddCdPlayer(GameState state)
        {
            var count = CdRules.Count(state);
            var head = new Label(Loc.T("hub.cd.list_title") + "  ·  " + Loc.F("hub.cd.shelf", count, CdPlayer.Slots));
            head.AddToClassList("hub-room-panel-note");
            head.AddToClassList("hub-cd-head");
            _roomPanel.Add(head);
            if (count == 0)
            {
                var empty = new Label(Loc.T("hub.cd.empty"));
                empty.AddToClassList("hub-room-panel-note");
                _roomPanel.Add(empty);
                return;
            }

            foreach (var entry in CdPlayer.All)
            {
                if (!CdRules.Has(state, entry.Id))
                {
                    continue;
                }

                var id = entry.Id;
                var playing = CdPlayer.IsPlaying(state, id);
                var label = Loc.T("cd." + id + ".title") + (playing ? "  ♪ " + Loc.T("hub.cd.playing") : "");
                if (!CdPlayer.HasClip(id))
                {
                    label += "  (" + Loc.T("music.no_clip") + ")";
                }

                var button = new Button(() =>
                {
                    if (CdPlayer.IsPlaying(_session.State, id)) CdPlayer.Stop(_session.State);
                    else CdPlayer.Play(_session.State, id);
                    ShowRoomPanel(_cdRoomId);
                }) { text = label };
                button.AddToClassList("hub-room-action");
                button.AddToClassList("hub-cd-track");
                button.EnableInClassList("playing", playing);
                var weekText = state.CdWeeks.TryGetValue(id, out var week) ? "  ·  " + Loc.F("hub.cd.found_week", week) : "";
                button.tooltip = Loc.T("cd." + id + ".owner") + weekText;
                if (ColorUtility.TryParseHtmlString(entry.Color, out var color))
                {
                    button.style.borderLeftColor = color;
                }

                _roomPanel.Add(button);
            }

            var hint = new Label(Loc.T("hub.cd.hint"));
            hint.AddToClassList("hub-room-panel-note");
            hint.AddToClassList("hub-cd-hint");
            _roomPanel.Add(hint);
        }

        private void AddProp(VisualElement room, HubSectionConfig.PropDef prop, float floorY)
        {
            var texture = Tex(prop.Key);
            if (texture == null)
            {
                return;
            }

            var element = Place(room, "hub-prop", prop.X - texture.width * 0.5f, floorY - prop.Y - texture.height, texture.width, texture.height);
            element.pickingMode = PickingMode.Ignore;
            Image(element, texture);
        }

        private string RoomName(HubSectionConfig.RoomDef def)
        {
            if (!string.IsNullOrEmpty(def.NameKey))
            {
                return Loc.T(def.NameKey);
            }

            return !string.IsNullOrEmpty(def.Facility) ? Loc.T("facility." + def.Facility) : def.Id;
        }

        // ── 방 누르기 ──

        private void ShowRoomPanel(string roomId)
        {
            if (_executing || !_rooms.TryGetValue(roomId, out var room))
            {
                return;
            }

            _roomPanel.Clear();
            var title = new Label(room.Open ? RoomName(room.Def) : Loc.T("hub.room_locked"));
            title.AddToClassList("hub-room-panel-title");
            _roomPanel.Add(title);

            var art = room.Open && !string.IsNullOrEmpty(room.Def.Art) ? StoryArt.Load(room.Def.Art) : null;
            VisualElement picture = null;
            if (art != null)
            {
                picture = new VisualElement { pickingMode = PickingMode.Ignore };
                picture.AddToClassList("hub-room-art");
                StoryArt.SetCover(picture, art);
                _roomPanel.Add(picture);
            }

            // 지어 놓은 설비 (P4 허브 그림): 방 그림이 있으면 그 아래쪽에, 없으면 따로 한 줄로
            var shelf = new VisualElement { pickingMode = PickingMode.Ignore };
            shelf.AddToClassList("hub-room-built");
            shelf.EnableInClassList("standalone", picture == null);
            if (room.Open)
            {
                var state = _session.State;
                foreach (var built in room.Def.Built)
                {
                    var shown = (string.IsNullOrEmpty(built.Flag) || state.Flags.Contains(built.Flag)) && state.Base.Defense >= built.MinDefense;
                    var texture = shown ? StoryArt.Load(built.Image) : null;
                    if (texture == null)
                    {
                        continue;
                    }

                    var item = new VisualElement { pickingMode = PickingMode.Ignore };
                    item.AddToClassList("hub-room-built-item");
                    StoryArt.SetContain(item, texture);
                    shelf.Add(item);
                }
            }

            if (shelf.childCount > 0)
            {
                (picture ?? _roomPanel).Add(shelf);
            }

            if (room.Open)
            {
                var state = _session.State;
                var actions = new List<string>(room.Def.Actions);
                if (room.Def.Id == Shelter(state))
                {
                    var shelterNote = new Label(Loc.T("hub.room_shelter"));
                    shelterNote.AddToClassList("hub-room-panel-note");
                    _roomPanel.Add(shelterNote);
                    actions.InsertRange(0, _config.ShelterActions);
                }

                var any = false;
                foreach (var id in actions.Distinct())
                {
                    if (!_session.Definitions.TryGetAction(id, out var action) || !_session.IsUnlocked(action))
                    {
                        continue;
                    }

                    any = true;
                    var actionId = id;
                    var button = new Button(() =>
                    {
                        HideRoomPanel();
                        ActionPicked?.Invoke(actionId);
                    }) { text = Loc.Action(id) };
                    button.AddToClassList("hub-room-action");
                    var actionIcon = StoryArt.Icon("act." + id);
                    if (actionIcon != null)
                    {
                        StoryArt.SetIcon(button, actionIcon, 26f);
                    }
                    _roomPanel.Add(button);
                }

                if (!any && !room.Def.CdShelf)
                {
                    var none = new Label(Loc.T("hub.room_none"));
                    none.AddToClassList("hub-room-panel-note");
                    _roomPanel.Add(none);
                }

                if (room.Def.CdShelf)
                {
                    _cdRoomId = roomId;
                    AddCdPlayer(state);
                }
            }

            // 방 위쪽 가운데에 띄운다 (화면 밖으로 나가지 않게)
            var left = Mathf.Clamp(Px(room.Def.X + room.Def.Width * 0.5f) - 130f, 0f, Px(_config.Width) - 260f);
            var top = room.Def.Roof ? Px(FloorBottom(3)) - 10f : Px(FloorBottom(room.Def.Floor) - _config.FloorHeight) + 12f;
            _roomPanel.style.left = left;
            _roomPanel.style.top = top;
            _roomPanel.RemoveFromClassList("hidden");
            KoreanFont.ApplyDeep(_roomPanel);
        }

        public void HideRoomPanel() => _roomPanel.AddToClassList("hidden");

        // ── 사람 ──

        private void RefreshActors(GameState state, bool resetPositions)
        {
            var shelter = Shelter(state);
            _rooms.TryGetValue(shelter, out var home);
            if (home == null)
            {
                return;
            }

            // 주인공
            var prefix = PlayerPrefix(state.Loadout.Armor, state.Loadout.MainWeapon, state.Calendar.Week);
            _player ??= NewActor("player");
            _player.SetLook(Frames(prefix + "idle_"), Frames(prefix + "walk_"));
            if (resetPositions)
            {
                _player.Place(home.Def.Floor, home.Def.X + home.Def.Width * 0.38f);
            }

            // 선생님
            var teacherKey = TeacherKey(state, out var canWalk, out var teacherRoom);
            if (teacherKey == null)
            {
                RemoveActor(ref _teacher);
            }
            else
            {
                _teacher ??= NewActor("teacher");
                _teacher.Id = ApoGirl.Core.Rules.GiveRules.TeacherId;
                _teacher.SetLook(Frames(teacherKey + "_idle_", teacherKey), canWalk ? Frames(teacherKey + "_idle_", teacherKey) : null);
                _teacher.CanWalk = canWalk;
                var teacherHome = _rooms.TryGetValue(teacherRoom ?? shelter, out var r) && r.Open ? r : home;
                _teacher.HomeFloor = teacherHome.Def.Floor;
                _teacher.HomeX = teacherHome.Def.X + teacherHome.Def.Width * 0.7f;
                if (resetPositions)
                {
                    _teacher.Place(_teacher.HomeFloor, _teacher.HomeX);
                }
            }

            // 동료 (역할에 맞는 방, 없으면 지내는 곳)
            foreach (var actor in _actors.Where(a => a.Kind == "npc").ToList())
            {
                actor.Element.RemoveFromHierarchy();
                _actors.Remove(actor);
            }

            var index = 0;
            foreach (var pair in state.Npcs.Where(n => n.Value.Status == NpcStatus.Joined))
            {
                var sprite = _config.NpcSprites.FirstOrDefault(s => s.Npc == pair.Key)?.Key;
                if (sprite == null || Tex(sprite) == null)
                {
                    continue;
                }

                // 역할 재기획: 역할의 방 묶음에서 열린 방 (코어 규칙, 같은 역할 둘째 사람은 다음 방)
                var roleRoom = ApoGirl.Core.Rules.RoleRules.RoomFor(state, _session.Definitions, pair.Key);
                var room = roleRoom != null && _rooms.TryGetValue(roleRoom, out var rr) && rr.Open ? rr : home;
                var actor = NewActor("npc");
                actor.Id = pair.Key;
                actor.SetLook(Frames(sprite + "_idle_", sprite), null);
                actor.Guard = pair.Value.Role == "GUARD";
                actor.HomeFloor = home.Def.Floor;
                actor.HomeX = home.Def.X + home.Def.Width * (0.12f + 0.1f * (index % 4));
                actor.PostFloor = room.Def.Floor;
                actor.PostX = room.Def.X + room.Def.Width * (0.2f + 0.18f * (index % 3));
                actor.PostRoom = room.Def.Id;
                var atNight = _executing && _phase == DayPhase.Night && !actor.Guard;
                actor.Place(atNight ? actor.HomeFloor : actor.PostFloor, atNight ? actor.HomeX : actor.PostX);

                // 밖에 나가 있는 낮: 현관에 숨겨 둔다 (밤 칸이 시작되면 거기서 걸어 들어온다)
                var door = EntranceSpot();
                if (_executing && _phase == DayPhase.Day && _outToday.Contains(pair.Key) && door.HasValue)
                {
                    actor.Place(door.Value.Floor, door.Value.X);
                    actor.SetAway(true);
                }

                actor.Phase = index * 0.37f;
                index++;
            }

            UpdateAwayMarks();
        }

        private string TeacherKey(GameState state, out bool canWalk, out string room)
        {
            canWalk = false;
            room = null;
            var teacher = _config.Teacher;
            if (!_session.HasMetTeacher || teacher.GoneFlags.Any(state.Flags.Contains))
            {
                return null;
            }

            if (!string.IsNullOrEmpty(teacher.IsolatedFlag) && state.Flags.Contains(teacher.IsolatedFlag))
            {
                room = teacher.IsolatedRoom;
                return teacher.Ill;
            }

            if (teacher.IllFlags.Any(state.Flags.Contains))
            {
                return teacher.Ill;
            }

            room = string.IsNullOrEmpty(teacher.Room) ? null : teacher.Room;
            switch (state.Teacher.MobilityStage)
            {
                case 0: return teacher.Seated;
                case 1:
                    canWalk = true;
                    return teacher.Crutch;
                default:
                    canWalk = true;
                    return teacher.Cane;
            }
        }

        private Actor NewActor(string kind)
        {
            var element = new VisualElement { pickingMode = PickingMode.Ignore };
            element.AddToClassList("hub-actor");
            _actorLayer.Add(element);
            var actor = new Actor { Kind = kind, Element = element, Scale = _config.Scale };
            _actors.Add(actor);
            return actor;
        }

        private void RemoveActor(ref Actor actor)
        {
            if (actor == null)
            {
                return;
            }

            actor.Element.RemoveFromHierarchy();
            _actors.Remove(actor);
            actor = null;
        }

        /// <summary>주인공 그림 접두어: player_{옷}_{무기}_ (옷은 방어구, 없으면 계절 교복. 그림이 없으면 교복·맨손 쪽으로 물러난다).</summary>
        private string PlayerPrefix(string armorId, string weaponId, int week)
        {
            var armor = StandingConfig.PlayerOutfitToken(armorId, week);
            var weapon = Token(weaponId, "ITEM_WEAPON_", "none");
            foreach (var prefix in new[] { $"player_{armor}_{weapon}_", $"player_uniform_{weapon}_", $"player_{armor}_none_", "player_uniform_none_" })
            {
                if (Tex(prefix + "idle_0") != null)
                {
                    return prefix;
                }
            }

            return "player_";
        }

        private static string Token(string itemId, string prefix, string fallback) =>
            !string.IsNullOrEmpty(itemId) && itemId.StartsWith(prefix, StringComparison.Ordinal) ? itemId.Substring(prefix.Length).ToLowerInvariant() : fallback;

        /// <summary>key0, key1 … 프레임 (없으면 정지 그림 하나, 그것도 없으면 빈 목록).</summary>
        private Texture2D[] Frames(string prefix, string still = null)
        {
            var cacheKey = prefix + "|" + still;
            if (_frames.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            var list = new List<Texture2D>();
            for (var i = 0; i < 64; i++)
            {
                var texture = Tex(prefix + i);
                if (texture == null)
                {
                    break;
                }

                list.Add(texture);
            }

            if (list.Count == 0 && still != null && Tex(still) != null)
            {
                list.Add(Tex(still));
            }

            var frames = list.ToArray();
            _frames[cacheKey] = frames;
            return frames;
        }

        // ── 매 틱: 걷기·동작·불빛 ──

        private float _lightTimer;

        private void Tick(TimerState timer)
        {
            var dt = Mathf.Min(0.1f, timer.deltaTime / 1000f);
            foreach (var actor in _actors)
            {
                actor.Tick(dt, this);
            }

            _skyView.Tick(dt);
            TickDoors(dt);
            TickBubbles(dt);

            _lightTimer += dt;
            if (_lightTimer > 0.3f)
            {
                _lightTimer = 0f;
                ApplyLight();
            }
        }

        private void ApplyLight()
        {
            var night = _executing && _phase == DayPhase.Night;
            _sky.EnableInClassList("night", night);
            _host.parent?.EnableInClassList("night", night);
            var blackout = _session.State != null && _session.State.Base.IsBlackout();
            if (_session.State != null)
            {
                _skyView.Apply(night, _session.State.Base.Weather, Core.Rules.CalendarRules.GetSeason(_session.State.Calendar.Week));
            }
            foreach (var room in _rooms.Values)
            {
                if (room.Dark == null)
                {
                    // 옥상: 막 없이 소품만 밤빛으로
                    room.Element.EnableInClassList("night", night);
                    continue;
                }

                var occupied = _actors.Any(a => !a.Away && a.Floor == room.Def.Floor && a.X >= room.Def.X && a.X < room.Def.X + room.Def.Width && !room.Def.Roof);
                room.Dark.EnableInClassList("night", night && !occupied);
                room.Dark.EnableInClassList("dim", night && occupied);
                room.Dark.EnableInClassList("blackout", night && blackout && !occupied);
                room.Glow.EnableInClassList("on", night && occupied);
            }
        }

        internal float StairsCenter => _config.StairsX + _config.StairsWidth * 0.5f;
        internal float WalkSpeed => _config.WalkSpeed;

        // ── 말풍선 (이번 주 배분을 받은 사람이 고마워한다) ──

        private sealed class Bubble
        {
            public string Id;
            public Label Label;
            public float Delay;
            public float Left;
        }

        private readonly List<Bubble> _bubbles = new List<Bubble>();

        /// <summary>그 사람이 단면도에 보이면 머리 위에 말풍선을 띄우고 true. 안 보이면 false (허브가 다른 곳에 띄운다).</summary>
        public bool ShowBubble(string id, string text, float delay, float seconds = 2.4f)
        {
            if (string.IsNullOrEmpty(id) || !_actors.Any(a => a.Id == id && !a.Away && !a.Element.ClassListContains("hidden")))
            {
                return false;
            }

            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("hub-bubble");
            label.AddToClassList("hidden");
            _actorLayer.Add(label);
            _bubbles.Add(new Bubble { Id = id, Label = label, Delay = delay, Left = seconds });
            return true;
        }

        private void TickBubbles(float dt)
        {
            for (var i = _bubbles.Count - 1; i >= 0; i--)
            {
                var bubble = _bubbles[i];
                if (bubble.Delay > 0f)
                {
                    bubble.Delay -= dt;
                    continue;
                }

                bubble.Left -= dt;
                var actor = _actors.FirstOrDefault(a => a.Id == bubble.Id);
                if (bubble.Left <= 0f || actor == null)
                {
                    bubble.Label.RemoveFromHierarchy();
                    _bubbles.RemoveAt(i);
                    continue;
                }

                var height = actor.Current != null ? actor.Current.height : 80;
                bubble.Label.RemoveFromClassList("hidden");
                bubble.Label.style.left = Px(actor.X) - 150f; // 말풍선 폭 300px (USS), 화면 픽셀
                bubble.Label.style.top = Px(FloorBottom(actor.Floor) - 6 - height) - 44f;
                bubble.Label.style.opacity = Mathf.Clamp01(bubble.Left / 0.4f);
            }
        }

        internal void PlaceActor(Actor actor)
        {
            var frame = actor.Current;
            var width = frame != null ? frame.width : 96;
            var height = frame != null ? frame.height : 80;
            actor.Element.style.width = Px(width);
            actor.Element.style.height = Px(height);
            actor.Element.style.left = Px(actor.X - width * 0.5f);
            actor.Element.style.top = Px(FloorBottom(actor.Floor) - 6 - height);
        }

        // ── 작은 도구 ──

        private static VisualElement Add(VisualElement parent, string cssClass)
        {
            var element = new VisualElement();
            element.AddToClassList(cssClass);
            parent.Add(element);
            return element;
        }

        private VisualElement Place(VisualElement parent, string cssClass, float x, float y, float width, float height)
        {
            var element = Add(parent, cssClass);
            element.style.position = Position.Absolute;
            element.style.left = Px(x);
            element.style.top = Px(y);
            element.style.width = Px(width);
            element.style.height = Px(height);
            return element;
        }

        private void Tile(VisualElement element, string key, bool bottomAligned = false)
        {
            var texture = Tex(key);
            if (texture == null)
            {
                return;
            }

            element.style.backgroundImage = new StyleBackground(texture);
            element.style.backgroundRepeat = new StyleBackgroundRepeat(new BackgroundRepeat(Repeat.Repeat, Repeat.NoRepeat));
            element.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(new Length(Px(texture.width)), new Length(Px(texture.height))));
            element.style.backgroundPositionX = new StyleBackgroundPosition(new BackgroundPosition(BackgroundPositionKeyword.Left));
            element.style.backgroundPositionY = new StyleBackgroundPosition(new BackgroundPosition(bottomAligned ? BackgroundPositionKeyword.Bottom : BackgroundPositionKeyword.Top));
        }

        private static void Image(VisualElement element, Texture2D texture)
        {
            element.style.backgroundImage = new StyleBackground(texture);
            element.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Contain));
        }

        private static Texture2D Tex(string key) => StoryArt.Load(Pixel + key);

        private sealed class DoorView
        {
            public int Floor;
            public float X;
            public VisualElement Lower;
            public VisualElement Line;
            public VisualElement Panel;
            public bool Open;
            public float Hold;
        }

        private sealed class RoomView
        {
            public HubSectionConfig.RoomDef Def;
            public VisualElement Element;
            public VisualElement Dark;
            public VisualElement Glow;
            public Label Name;
            public bool Open;
        }

        /// <summary>단면도 위의 사람 하나: 층·위치, 걷는 길(계단을 거쳐), 대기·걷기 프레임.</summary>
        internal sealed class Actor
        {
            private readonly Queue<(int Floor, float X, bool Jump)> _path = new Queue<(int, float, bool)>();
            private Texture2D[] _idle = Array.Empty<Texture2D>();
            private Texture2D[] _walk = Array.Empty<Texture2D>();
            private float _time;
            private bool _facingLeft;

            public string Kind;

            /// <summary>사람 ID (선생님은 NPC_TEACHER, 동료는 NPC_…). 주인공은 비어 있다.</summary>
            public string Id;
            public VisualElement Element;
            public int Scale;
            public int Floor;
            public float X;
            public float Phase;
            public bool CanWalk = true;
            public bool Guard;
            public int HomeFloor;
            public float HomeX;
            public int PostFloor;
            public float PostX;

            /// <summary>맡은 방 ID (외출 표시).</summary>
            public string PostRoom;

            /// <summary>걸어가 닿으면 사라진다 (현관에서 밖으로 나가는 동료).</summary>
            public bool HideOnArrive;

            /// <summary>밖에 나가 단면도에 없다.</summary>
            public bool Away { get; private set; }

            public void SetAway(bool away)
            {
                Away = away;
                HideOnArrive = false;
                Element.EnableInClassList("away", away);
            }

            public Texture2D Current { get; private set; }

            public bool Walking => _path.Count > 0;

            public void SetLook(Texture2D[] idle, Texture2D[] walk)
            {
                _idle = idle ?? Array.Empty<Texture2D>();
                _walk = walk != null && walk.Length > 0 ? walk : _idle;
                Element.EnableInClassList("hidden", _idle.Length == 0);
            }

            public void Place(int floor, float x)
            {
                _path.Clear();
                Floor = floor;
                X = x;
            }

            /// <summary>그곳까지 걸어간다 (층이 다르면 계단으로). 걸리는 시간(초).</summary>
            public float WalkTo(int floor, float x, HubSectionView view)
            {
                _path.Clear();
                var distance = 0f;
                var from = X;
                if (floor != Floor)
                {
                    _path.Enqueue((Floor, view.StairsCenter, false));
                    _path.Enqueue((floor, view.StairsCenter, true));
                    distance += Mathf.Abs(view.StairsCenter - from) + Mathf.Abs(view.StairsCenter - x) + 30f * Mathf.Abs(floor - Floor);
                }
                else
                {
                    distance += Mathf.Abs(x - from);
                }

                _path.Enqueue((floor, x, false));
                return distance / Mathf.Max(1f, view.WalkSpeed);
            }

            public void Tick(float dt, HubSectionView view)
            {
                _time += dt;
                if (Walking)
                {
                    var (floor, x, jump) = _path.Peek();
                    if (jump)
                    {
                        Floor = floor;
                        X = x;
                        _path.Dequeue();
                    }
                    else
                    {
                        var step = view.WalkSpeed * dt;
                        var dx = x - X;
                        if (Mathf.Abs(dx) <= step)
                        {
                            X = x;
                            _path.Dequeue();
                        }
                        else
                        {
                            X += Mathf.Sign(dx) * step;
                            _facingLeft = dx < 0;
                        }
                    }
                }

                if (HideOnArrive && !Walking)
                {
                    SetAway(true);
                }

                var frames = Walking ? _walk : _idle;
                if (frames.Length > 0)
                {
                    Current = frames[(int)((_time + Phase) / FrameSeconds) % frames.Length];
                    Element.style.backgroundImage = new StyleBackground(Current);
                }

                Element.style.scale = new StyleScale(new Scale(new Vector2(_facingLeft ? -1f : 1f, 1f)));
                view.PlaceActor(this);
            }
        }
    }
}
