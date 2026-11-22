using System.Collections.Generic;
using ApoGirl.Core.Definitions;
using UnityEngine;

namespace ApoGirl.Exploration.Runtime
{
    /// <summary>
    /// 그레이박스 도형 그리기 (색 사각형). 아트 단계에서 Tilemap/스프라이트로 교체한다.
    /// 복도: 층 f의 바닥 높이 = f × FloorHeight, X = 0 ~ CorridorLength.
    /// 방 내부: ExplorationArea.Origin에 템플릿 폭만큼 따로 그린다.
    /// </summary>
    public sealed class BuildingView
    {
        public static readonly Color CorridorColor = new Color(0.28f, 0.29f, 0.3f);
        public static readonly Color DoorColor = new Color(0.2f, 0.22f, 0.28f);
        public static readonly Color VisitedDoorColor = new Color(0.72f, 0.72f, 0.76f);
        public static readonly Color RoomWallColor = new Color(0.24f, 0.25f, 0.27f);
        public static readonly Color WindowColor = new Color(0.4f, 0.45f, 0.48f);
        public static readonly Color SilhouetteColor = new Color(0.1f, 0.11f, 0.12f, 0.92f);

        /// <summary>창문 높이 범위 (층 바닥 기준).</summary>
        public const float WindowBottom = 1.7f;
        public const float WindowHeight = 1.5f;

        private static readonly Color[] RoomColors =
        {
            new Color(0.31f, 0.3f, 0.27f),
            new Color(0.27f, 0.3f, 0.29f),
            new Color(0.3f, 0.28f, 0.29f),
        };

        private static Sprite _square;
        private readonly Transform _root;
        private readonly BuildingLayoutDef _layout;

        private readonly string _corridorWall;
        private readonly string _locationId;

        public BuildingView(Transform root, BuildingLayoutDef layout, string locationId = "")
        {
            _locationId = locationId ?? "";
            _corridorWall = ExplorationArt.CorridorWall(locationId);
            _root = root;
            _layout = layout;
            BuildCorridors();
            BuildDeep();
        }

        public float FloorHeight => _layout.FloorHeight;

        public float CorridorLength => _layout.CorridorLength;

        /// <summary>1×1 유닛, 왼쪽 아래 기준 흰 사각형.</summary>
        public static Sprite Square
        {
            get
            {
                if (_square == null)
                {
                    _square = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero, 4f);
                    _square.name = "GrayboxSquare";
                }

                return _square;
            }
        }

        /// <summary>
        /// 층 바닥의 높이. 깊은 곳 층(기획 B)은 건물 아래 멀리 따로 그린다 (지하 1이 위, 지하 2가 그 아래).
        /// 땅속 단면처럼 붙여 그리면 겉 건물에서 보이므로, 화면에는 지금 층만 보인다는 점을 이용해 떼어 둔다.
        /// </summary>
        public float FloorY(int floor) => _layout.IsDeep(floor)
            ? DeepTopY - _layout.DeepLevel(floor) * _layout.FloorHeight
            : floor * _layout.FloorHeight;

        /// <summary>깊은 곳 맨 위 천장 높이 (바깥 길 -70보다 아래).</summary>
        private const float DeepTopY = -120f;

        private static readonly Color DeepWallColor = new Color(0.13f, 0.14f, 0.15f);

        /// <summary>창틀 두께 (유닛): 창 그림 가장자리 2픽셀.</summary>
        private const float WindowFrame = 2f / 36f;

        /// <summary>
        /// 창 유리 자리의 마스크: 창 너머 좀비 실루엣(VisibleInsideMask)이 이 사각형 안에서만 보인다.
        /// 다른 그림은 마스크를 따르지 않으니 영향이 없다.
        /// </summary>
        private static void AddWindowMask(Transform parent, float x, float width, float y = 0f)
        {
            var go = new GameObject("WindowMask");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(x + WindowFrame, y + WindowBottom + WindowFrame, 0f);
            go.transform.localScale = new Vector3(Mathf.Max(0.05f, width - WindowFrame * 2f), WindowHeight - WindowFrame * 2f, 1f);
            go.AddComponent<SpriteMask>().sprite = Square;
        }

        public static GameObject Rect(Transform parent, string name, Vector2 position, Vector2 size, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = Square;
            renderer.color = color;
            renderer.sortingOrder = order;
            return go;
        }

        /// <summary>1점 투시 방: 옆벽 그림 폭, 뒤쪽 바닥이 올라오는 높이, 천장이 내려오는 높이 (유닛, 그림 픽셀에 맞춘 값).</summary>
        private const float SideWallWidth = 72f / 36f;
        private const float BackFloorRise = 16f / 36f;
        private const float BackCeilingDrop = 12f / 36f;

        /// <summary>
        /// 그 방을 1점 투시로 그릴 때 쓰는 그림 이름 조각: 방 안쪽 벽 그림(wall_X)에 맞는 옆벽(wall_side_X_l/r)과 바닥(wall_floor_X)이
        /// 모두 있을 때 X, 없으면 null (예전 평면 그림).
        /// </summary>
        private static string PerspectiveKind(RoomTemplateDef template, string locationId)
        {
            if (template == null)
            {
                return null;
            }

            var wall = ExplorationArt.InteriorWall(template.Id, locationId);
            var kind = wall != null && wall.StartsWith("wall_") ? wall.Substring(5) : null;
            return kind != null && ExplorationArt.Get($"wall_side_{kind}_l") != null && ExplorationArt.Get($"wall_side_{kind}_r") != null
                && ExplorationArt.Get($"wall_floor_{kind}") != null ? kind : null;
        }

        /// <summary>뒷벽에 붙은 것(문 등)을 올려 그릴 높이: 1점 투시 방이면 뒤쪽 바닥 높이, 아니면 0.</summary>
        public static float BackWallOffset(RoomTemplateDef template, string locationId) => PerspectiveKind(template, locationId) != null ? BackFloorRise : 0f;

        /// <summary>방 내부를 그린다 (같은 템플릿은 같은 모양).</summary>
        public void BuildRoom(ExplorationArea room, int colorIndex)
        {
            var root = new GameObject("Room_" + room.Door.RoomId).transform;
            root.SetParent(_root, false);
            root.localPosition = room.Origin;
            room.Root = root.gameObject;

            var h = _layout.FloorHeight;
            Rect(root, "Void", new Vector2(-30f, -20f), new Vector2(room.Width + 60f, 40f), new Color(0.05f, 0.05f, 0.06f), -7);
            ExplorationArt.Tiled(root, "Interior", ExplorationArt.InteriorWall(room.Template.Id, _locationId), new Vector2(0f, 0.15f), new Vector2(room.Width, h - 0.3f), -5,
                RoomColors[colorIndex % RoomColors.Length]);
            // 뒷벽 장식: 템플릿 WallDecor (양호실 적십자, 과학실 주기율표 등), 없으면 교실은 칠판
            // 1점 투시 방: 칠판·게시판은 옆벽 그림에 들어 있다 (뒷벽에는 문과 복도 쪽 창만)
            var perspective = PerspectiveKind(room.Template, _locationId);
            var decorKey = !string.IsNullOrEmpty(room.Template.WallDecor) ? room.Template.WallDecor
                : room.Template.Id.Contains("CLASSROOM") && perspective == null ? "prop_chalkboard" : "";
            var decor = string.IsNullOrEmpty(decorKey) ? null : ExplorationArt.Get(decorKey);
            (float, float) decorSpan = (0f, 0f);
            if (decor != null)
            {
                // 창문과 겹치지 않는 가장 넓은 벽면 가운데. 들어갈 자리가 없으면 걸지 않는다
                var decorWidth = decor.bounds.size.x;
                // (칠판처럼 넓은 것은 빈 벽이 없으면 방 가운데 뒷벽에 건다. 창은 앞벽이라 유리 너머로 비쳐 보인다)
                if (!TryFindWallGap(room, decorWidth + 0.6f, out var gapCenter))
                {
                    gapCenter = room.Width * 0.5f;
                }

                ExplorationArt.Place(root, "WallDecor", decorKey, new Vector2(gapCenter - decorWidth * 0.5f, 1.5f), -4, Vector2.zero, Color.clear);
                decorSpan = (gapCenter - decorWidth * 0.5f, gapCenter + decorWidth * 0.5f);
            }

            // 흩어 놓는 장식 (그림 표의 Decor): 컨테이너·문·창·뒷벽 장식을 피해 바닥과 벽에
            var roomRule = ExplorationArt.Decor(_locationId, room.Template.Id);
            if (roomRule != null)
            {
                var blockedFloor = new List<(float, float)>();
                var blockedWall = new List<(float, float)>();
                foreach (var doorX in room.Template.DoorXs)
                {
                    blockedFloor.Add((doorX - 1.1f, doorX + 1.1f));
                    blockedWall.Add((doorX - 1.1f, doorX + 1.1f));
                }

                foreach (var container in room.Template.Containers)
                {
                    blockedFloor.Add((container.X - 1.1f, container.X + 1.1f));
                    blockedWall.Add((container.X - 0.9f, container.X + 0.9f));
                }

                foreach (var window in room.Template.Windows)
                {
                    blockedWall.Add((window.X - 0.3f, window.X + window.Width + 0.3f));
                }

                if (decorSpan.Item2 > decorSpan.Item1)
                {
                    blockedWall.Add((decorSpan.Item1 - 0.3f, decorSpan.Item2 + 0.3f));
                }

                Scatter(root, roomRule, room.Width, 0f, blockedFloor, blockedWall, StableHash(room.Door.RoomId));
            }

            for (var x = 2f; x < room.Width - 1f; x += 5f)
            {
                ExplorationArt.Place(root, "Lamp", "prop_ceiling_lamp", new Vector2(x, h - 0.15f), -3, Vector2.zero, Color.clear);
            }
            var sideSpan = perspective != null ? SideWallWidth + 0.3f : 0.3f;
            Rect(root, "Floor", new Vector2(-sideSpan, -0.15f), new Vector2(room.Width + sideSpan * 2f, 0.3f), new Color(0.09f, 0.09f, 0.1f), -3);
            Rect(root, "Ceiling", new Vector2(-sideSpan, h - 0.15f), new Vector2(room.Width + sideSpan * 2f, 0.3f), new Color(0.09f, 0.09f, 0.1f), -3);
            if (perspective != null)
            {
                // 얕은 1점 투시: 뒷벽 아래에 바닥 띠, 위에 천장 띠, 양옆에 원근으로 줄어드는 옆벽 (입구 쪽 칠판, 반대쪽 게시판)
                var color = RoomColors[colorIndex % RoomColors.Length];
                ExplorationArt.Tiled(root, "PerspFloor", $"wall_floor_{perspective}", new Vector2(-SideWallWidth, 0.15f), new Vector2(room.Width + SideWallWidth * 2f, BackFloorRise), -4, color);
                Rect(root, "PerspCeiling", new Vector2(-SideWallWidth, h - 0.15f - BackCeilingDrop), new Vector2(room.Width + SideWallWidth * 2f, BackCeilingDrop), new Color(0.2f, 0.19f, 0.18f), -4);
                ExplorationArt.Place(root, "SideWallL", $"wall_side_{perspective}_l", new Vector2(-SideWallWidth, 0.15f), -3, Vector2.zero, Color.clear);
                ExplorationArt.Place(root, "SideWallR", $"wall_side_{perspective}_r", new Vector2(room.Width, 0.15f), -3, Vector2.zero, Color.clear);
                Rect(root, "WallL", new Vector2(-SideWallWidth - 0.3f, 0f), new Vector2(0.3f, h), new Color(0.1f, 0.1f, 0.11f), -3);
                Rect(root, "WallR", new Vector2(room.Width + SideWallWidth, 0f), new Vector2(0.3f, h), new Color(0.1f, 0.1f, 0.11f), -3);
            }
            else
            {
                Rect(root, "WallL", new Vector2(-0.3f, 0f), new Vector2(0.3f, h), new Color(0.1f, 0.1f, 0.11f), -3);
                Rect(root, "WallR", new Vector2(room.Width, 0f), new Vector2(0.3f, h), new Color(0.1f, 0.1f, 0.11f), -3);
            }

            // 복도 쪽 창문: 너머로 복도의 실루엣이 보인다
            foreach (var window in room.Template.Windows)
            {
                // 복도 쪽 벽(앞면)의 창: 반투명 유리라 뒤 벽의 칠판 앞에 겹쳐 그린다
                var glass = ExplorationArt.Stretched(root, "Window", "prop_window", new Vector2(window.X, WindowBottom), new Vector2(window.Width, WindowHeight), -2, WindowColor);
                AddWindowMask(root, window.X, window.Width);

                // 방 안에서는 창이 화면 앞쪽 벽에 있다: 반투명하게 그려 뒤 벽(칠판·장식)이 비쳐 보이게
                var glassRenderer = glass.GetComponent<SpriteRenderer>();
                if (glassRenderer != null && ExplorationArt.Get("prop_window") != null && perspective == null)
                {
                    glassRenderer.color = new Color(1f, 1f, 1f, 0.55f);
                }
            }
        }

        /// <summary>
        /// 건물 밖 길 (진입로·탈출로): 뒤 벽(거리·골목) + 그 위 먼 건물 윤곽 + 하늘, 바닥. 장식은 그림 표의 OUT_ENTRY / OUT_ESCAPE 규칙.
        /// </summary>
        public void BuildOutdoor(ExplorationArea area, bool entry, List<(float, float)> blocked)
        {
            var root = new GameObject(entry ? "Outdoor_Entry" : "Outdoor_Escape").transform;
            root.SetParent(_root, false);
            root.localPosition = area.Origin;
            area.Root = root.gameObject;

            var h = _layout.FloorHeight;
            var w = area.Width;
            Rect(root, "Sky", new Vector2(-40f, -20f), new Vector2(w + 80f, 60f), new Color(0.56f, 0.63f, 0.7f), -9);
            // 먼 건물 윤곽: 너비·높이가 제각각인 건물을 두 겹으로 (지역·길마다 같은 모양)
            var skyline = new System.Random(StableHash(_locationId + (entry ? "sky_entry" : "sky_escape")));
            for (var layer = 0; layer < 2; layer++)
            {
                var shade = layer == 0 ? new Color(0.44f, 0.5f, 0.57f) : new Color(0.29f, 0.33f, 0.38f);
                for (var x = -40f; x < w + 40f;)
                {
                    var width = 2.2f + (float)skyline.NextDouble() * 3.2f;
                    var height = (layer == 0 ? 3.6f : 1.4f) + (float)skyline.NextDouble() * (layer == 0 ? 3.4f : 2.8f);
                    Rect(root, "Far", new Vector2(x, h - 0.3f), new Vector2(width, height), shade, -8 + layer);
                    if (layer == 1)
                    {
                        // 불 꺼진 창 몇 줄
                        for (var wy = 0.5f; wy < height - 0.4f; wy += 0.7f)
                        {
                            Rect(root, "FarWin", new Vector2(x + 0.3f, h - 0.3f + wy), new Vector2(width - 0.6f, 0.22f), new Color(0.2f, 0.23f, 0.27f), -6);
                        }
                    }

                    x += width + (float)skyline.NextDouble() * 0.5f;
                }
            }

            ExplorationArt.Tiled(root, "Backdrop", entry ? "wall_out_street" : "wall_out_alley", new Vector2(-12f, 0.15f), new Vector2(w + 24f, h - 0.3f), -6,
                new Color(0.33f, 0.34f, 0.36f));
            Rect(root, "Ground", new Vector2(-40f, -20f), new Vector2(w + 80f, 20f), new Color(0.03f, 0.03f, 0.04f), -5);
            Rect(root, "Road", new Vector2(-40f, -0.15f), new Vector2(w + 80f, 0.3f), new Color(0.2f, 0.2f, 0.21f), -3);
            Rect(root, "EdgeL", new Vector2(-40f, -20f), new Vector2(40f - 0.2f, 60f), new Color(0.02f, 0.02f, 0.03f, 0.55f), 40);
            Rect(root, "EdgeR", new Vector2(w + 0.2f, -20f), new Vector2(40f, 60f), new Color(0.02f, 0.02f, 0.03f, 0.55f), 40);

            var rule = ExplorationArt.Decor(_locationId, entry ? "OUT_ENTRY" : "OUT_ESCAPE");
            if (rule != null)
            {
                Scatter(root, rule, w, 0f, new List<(float, float)>(blocked), new List<(float, float)>(blocked), StableHash(_locationId + (entry ? "entry" : "escape")));
            }
        }

        /// <summary>창문 사이 빈 벽 중 가장 넓은 구간의 가운데 (needed 이상일 때만).</summary>
        private static bool TryFindWallGap(ExplorationArea room, float needed, out float center)
        {
            var edges = new List<(float Start, float End)>();
            foreach (var window in room.Template.Windows)
            {
                edges.Add((window.X, window.X + window.Width));
            }
            edges.Sort((l, r) => l.Start.CompareTo(r.Start));

            var bestWidth = 0f;
            var bestCenter = 0f;
            var cursor = 1.5f; // 문 자리 여유
            var limit = room.Width - 1.5f;
            foreach (var (start, end) in edges)
            {
                Consider(cursor, Mathf.Min(start, limit));
                cursor = Mathf.Max(cursor, end);
            }
            Consider(cursor, limit);
            center = bestCenter;
            return bestWidth >= needed;

            void Consider(float from, float to)
            {
                if (to - from > bestWidth)
                {
                    bestWidth = to - from;
                    bestCenter = (from + to) * 0.5f;
                }
            }
        }

        /// <summary>
        /// 깊은 곳 복도 (기획 B): 콘크리트 벽, 천장의 배관·케이블 트레이, 꺼진 듯 희미한 비상등. 늘 어둡다(조명은 ExplorationLighting).
        /// </summary>
        private void BuildDeep()
        {
            var h = _layout.FloorHeight;
            var length = _layout.DeepCorridorLength;
            for (var floor = _layout.Floors; floor < _layout.TotalFloors; floor++)
            {
                var y = FloorY(floor);
                Rect(_root, "DeepRock", new Vector2(-20f, y - 2f), new Vector2(length + 40f, h + 4f), new Color(0.025f, 0.025f, 0.03f), -10);
                ExplorationArt.Tiled(_root, "DeepWall" + floor, DeepWallKey(), new Vector2(0f, y + 0.15f), new Vector2(length, h - 0.3f), -6, DeepWallColor);
                var floorArt = ExplorationArt.Get("wall_floor_deep") != null;
                if (floorArt)
                {
                    ExplorationArt.Tiled(_root, "DeepFloor", "wall_floor_deep", new Vector2(0f, y - 0.3f), new Vector2(length, 0.45f), -3, Color.clear);
                }
                else
                {
                    Rect(_root, "DeepSlab", new Vector2(0f, y - 0.15f), new Vector2(length, 0.3f), new Color(0.07f, 0.07f, 0.075f), -3);
                }

                Rect(_root, "DeepCeiling", new Vector2(0f, y + h - 0.15f), new Vector2(length, 0.3f), new Color(0.07f, 0.07f, 0.075f), -3);

                // 천장 배관 두 줄과 케이블 트레이: 그림 띠(deep_ceiling)가 있으면 이어 붙이고, 없으면 사각형
                var ceiling = ExplorationArt.Get("deep_ceiling");
                var bandBottom = y + h - 1.05f;
                if (ceiling != null)
                {
                    var band = ceiling.bounds.size.y;
                    bandBottom = y + h - 0.15f - band;
                    ExplorationArt.Tiled(_root, "DeepCeilingBand", "deep_ceiling", new Vector2(0f, bandBottom), new Vector2(length, band), -4, Color.clear);
                }
                else
                {
                    Rect(_root, "DeepPipe", new Vector2(0f, y + h - 0.55f), new Vector2(length, 0.16f), new Color(0.28f, 0.24f, 0.2f), -4);
                    Rect(_root, "DeepPipe", new Vector2(0f, y + h - 0.8f), new Vector2(length, 0.1f), new Color(0.22f, 0.24f, 0.26f), -4);
                    Rect(_root, "DeepTray", new Vector2(0f, y + h - 1.05f), new Vector2(length, 0.06f), new Color(0.18f, 0.18f, 0.19f), -4);
                }

                var lamp = ExplorationArt.Get("deep_lamp");
                for (var x = 5f; x < length - 2f; x += 9f)
                {
                    // 배관 받침과 희미한 비상등 (등 그림은 띠 바로 아래에 매단다)
                    if (ceiling == null)
                    {
                        Rect(_root, "DeepBracket", new Vector2(x, y + h - 1.05f), new Vector2(0.08f, 0.9f), new Color(0.2f, 0.2f, 0.21f), -4);
                    }

                    if (lamp != null)
                    {
                        ExplorationArt.Place(_root, "DeepLamp", "deep_lamp", new Vector2(x + 1.35f, bandBottom - lamp.bounds.size.y), -3, Vector2.zero, Color.clear);
                    }
                    else
                    {
                        Rect(_root, "DeepLamp", new Vector2(x + 1.2f, y + h - 1.4f), new Vector2(0.3f, 0.12f), new Color(0.45f, 0.62f, 0.5f, 0.8f), -3);
                    }
                }

                // 벽 아래 물자국 (벽 그림이 없을 때만 — 그림에는 이미 그려져 있다)
                if (ExplorationArt.Get(DeepWallKey()) == null)
                {
                    Rect(_root, "DeepStain", new Vector2(0f, y + 0.15f), new Vector2(length, 0.45f), new Color(0.09f, 0.1f, 0.1f, 0.85f), -5);
                }

                // 지역 장식 (그림 표 Decor의 Template "DEEP"): 계단·문·출구·상자 자리를 피해서
                var deepRule = ExplorationArt.Decor(_locationId, DeepDecorTemplate);
                if (deepRule != null && deepRule.Template == DeepDecorTemplate)
                {
                    var blocked = BlockedOnFloor(floor);
                    Scatter(_root, deepRule, length, y, blocked, blocked, StableHash(_locationId) + floor * 7919);
                }
            }
        }

        /// <summary>깊은 곳 장식 규칙의 Template 이름 (방 템플릿 ID와 겹치지 않는다).</summary>
        private const string DeepDecorTemplate = "DEEP";

        /// <summary>그 층에서 장식을 놓지 않을 자리: 계단·깊은 문·출구·상자 앞.</summary>
        private List<(float, float)> BlockedOnFloor(int floor)
        {
            var blocked = new List<(float, float)>();
            foreach (var connector in _layout.Connectors)
            {
                if (connector.Floor == floor)
                {
                    blocked.Add((connector.X - 2.6f, connector.X + 2.6f));   // 계단실·문 폭
                }
            }

            foreach (var exit in _layout.Exits)
            {
                if (exit.Floor == floor)
                {
                    blocked.Add((exit.X - 1.5f, exit.X + 1.5f));
                }
            }

            foreach (var container in _layout.Containers)
            {
                if (container.Floor == floor)
                {
                    blocked.Add((container.X - 1.1f, container.X + 1.1f));
                }
            }

            return blocked;
        }

        /// <summary>깊은 곳 벽 그림: 지역 것(wall_deep_residential 등)이 있으면 그것, 없으면 공통 wall_deep.</summary>
        private string DeepWallKey()
        {
            var id = _locationId.StartsWith("LOC_", System.StringComparison.Ordinal) ? _locationId.Substring(4) : _locationId;
            var regional = "wall_deep_" + id.ToLowerInvariant();
            return ExplorationArt.Get(regional) != null ? regional : "wall_deep";
        }

        private void BuildCorridors()
        {
            var h = _layout.FloorHeight;
            var length = _layout.CorridorLength;
            // 건물 아래는 그리지 않는다 (땅속 단면처럼 보이면 아랫길이 있는 것으로 읽힌다): 어두운 바닥 한 장
            Rect(_root, "Ground", new Vector2(-30f, -20f), new Vector2(length + 60f, 20f), new Color(0.03f, 0.03f, 0.04f), -10);
            var shellFloors = _layout.RooftopFloor >= 0 ? _layout.RooftopFloor : _layout.Floors;
            ExplorationArt.Tiled(_root, "Shell", "wall_brick", new Vector2(-0.6f, 0f), new Vector2(length + 1.2f, shellFloors * h + 0.4f), -9, new Color(0.16f, 0.17f, 0.18f));

            for (var floor = 0; floor < _layout.Floors; floor++)
            {
                var y = FloorY(floor);
                if (_layout.IsRooftop(floor))
                {
                    BuildRooftop(y);
                    continue;
                }

                ExplorationArt.Tiled(_root, "Corridor" + floor, _corridorWall, new Vector2(0f, y + 0.15f), new Vector2(length, h - 0.3f), -6, CorridorColor);
                for (var x = 3f; x < length - 1f; x += 7f)
                {
                    ExplorationArt.Place(_root, "Lamp", "prop_ceiling_lamp", new Vector2(x, y + h - 0.15f), -3, Vector2.zero, Color.clear);
                }
            }

            // 복도에서 보이는 교실 벽과 창문 (방이 차지하는 구간)
            foreach (var door in _layout.Doors)
            {
                var template = _layout.FindTemplate(door.TemplateId);
                if (template == null)
                {
                    continue;
                }

                var y = FloorY(door.Floor);
                ExplorationArt.Tiled(_root, "RoomWall_" + door.RoomId, ExplorationArt.RoomOutsideWall(_locationId, template.Id), new Vector2(door.X, y + 0.15f), new Vector2(template.Width, h - 0.3f), -5, RoomWallColor);
                foreach (var window in template.Windows)
                {
                    ExplorationArt.Stretched(_root, "RoomWindow", "prop_window", new Vector2(door.X + window.X, y + WindowBottom), new Vector2(window.Width, WindowHeight), -4, WindowColor);
                    AddWindowMask(_root, door.X + window.X, window.Width, y);
                }

                // 교실 명패 (첫 문 위)
                if (template.DoorXs.Count > 0)
                {
                    ExplorationArt.Place(_root, "Plate", "prop_plate", new Vector2(door.X + template.DoorXs[0] + 1.1f, y + 2.75f), -3, Vector2.zero, Color.clear);
                }
            }

            for (var floor = 0; floor <= _layout.Floors; floor++)
            {
                if (floor > 0 && _layout.IsRooftop(floor - 1))
                {
                    continue;   // 옥상 위는 하늘
                }

                Rect(_root, "Slab", new Vector2(0f, floor * h - 0.15f), new Vector2(length, 0.3f), new Color(0.09f, 0.09f, 0.1f), -3);
            }

            // 복도에 흩어 놓는 장식 (그림 표의 Decor): 문·창·계단·출구·컨테이너를 피해서
            var corridorRule = ExplorationArt.Decor(_locationId, null);
            if (corridorRule != null)
            {
                for (var floor = 0; floor < _layout.Floors; floor++)
                {
                    if (_layout.IsRooftop(floor))
                    {
                        continue;   // 옥상 소품은 BuildRooftop이 자리를 정해 둔다
                    }

                    var blockedFloor = new List<(float, float)>();
                    var blockedWall = new List<(float, float)>();
                    foreach (var door in _layout.Doors)
                    {
                        var template = door.Floor == floor ? _layout.FindTemplate(door.TemplateId) : null;
                        if (template == null)
                        {
                            continue;
                        }

                        foreach (var doorX in template.DoorXs)
                        {
                            blockedFloor.Add((door.X + doorX - 1.1f, door.X + doorX + 1.1f));
                            blockedWall.Add((door.X + doorX - 1.1f, door.X + doorX + 1.6f));
                        }

                        foreach (var window in template.Windows)
                        {
                            blockedWall.Add((door.X + window.X - 0.3f, door.X + window.X + window.Width + 0.3f));
                        }
                    }

                    foreach (var connector in _layout.Connectors)
                    {
                        if (connector.Floor == floor)
                        {
                            blockedFloor.Add((connector.X - 1.3f, connector.X + 1.3f));
                            blockedWall.Add((connector.X - 1.3f, connector.X + 1.3f));
                        }
                    }

                    foreach (var exit in _layout.Exits)
                    {
                        if (exit.Floor == floor)
                        {
                            blockedFloor.Add((exit.X - 1.5f, exit.X + 1.5f));
                            blockedWall.Add((exit.X - 1.5f, exit.X + 1.5f));
                        }
                    }

                    foreach (var container in _layout.Containers)
                    {
                        if (container.Floor == floor)
                        {
                            blockedFloor.Add((container.X - 1.1f, container.X + 1.1f));
                            blockedWall.Add((container.X - 0.9f, container.X + 0.9f));
                        }
                    }

                    Scatter(_root, corridorRule, length, FloorY(floor), blockedFloor, blockedWall, StableHash(_locationId) + floor * 7919);
                }
            }
        }

        /// <summary>옥상 소품 자리 (복도 X, 그림 키): 계단·출구·상자 자리를 피했다.</summary>
        private static readonly (float X, string Key)[] RooftopProps =
        {
            (12f, "decor_acunits"), (22f, "decor_vent"), (31f, "decor_tank"), (52f, "hub_roof_antenna"),
            (63f, "decor_pipes"), (74f, "decor_acunits"), (85f, "decor_tank"), (105f, "decor_vent"),
        };

        /// <summary>
        /// 옥상 한 층: 하늘 → 철조망 너머 마을(roof_town) → 철조망(roof_fence) → 바닥 콘크리트. 천장 등·흩어 놓는 장식은 없다.
        /// 그림이 없으면 색 사각형으로 대신한다.
        /// </summary>
        private void BuildRooftop(float y)
        {
            var h = _layout.FloorHeight;
            var length = _layout.CorridorLengthOf(_layout.RooftopFloor);
            Rect(_root, "RoofSky", new Vector2(-30f, y), new Vector2(length + 60f, h + 30f), RoofSkyColor, -10);
            ExplorationArt.Tiled(_root, "RoofTown", "roof_town", new Vector2(-8f, y), new Vector2(length + 16f, h), -8, new Color(0.42f, 0.46f, 0.5f));
            ExplorationArt.Tiled(_root, "RoofFence", "roof_fence", new Vector2(-0.6f, y), new Vector2(length + 1.2f, h), -6, new Color(0.5f, 0.52f, 0.54f, 0.5f));
            Rect(_root, "RoofFloor", new Vector2(-0.6f, y - 0.15f), new Vector2(length + 1.2f, 0.3f), new Color(0.36f, 0.36f, 0.35f), -3);
            foreach (var (x, key) in RooftopProps)
            {
                if (x < length - 1f)
                {
                    ExplorationArt.Place(_root, "RoofProp_" + key, key, new Vector2(x, y + 0.15f), -4, Vector2.zero, Color.clear);
                }
            }
        }

        /// <summary>옥상 하늘 색 (조명이 낮·밤 밝기를 입힌다).</summary>
        public static readonly Color RoofSkyColor = new Color(0.55f, 0.68f, 0.8f);

        /// <summary>
        /// 장식을 흩어 놓는다 (같은 지역·방이면 늘 같은 자리). 바닥 장식은 벽에 붙여 소품 뒤(-2)에, 벽 장식은 눈높이쯤(-4)에.
        /// </summary>
        private static void Scatter(Transform parent, ExplorationArt.DecorRule rule, float width, float baseY, List<(float, float)> blockedFloor, List<(float, float)> blockedWall, int seed)
        {
            var random = new System.Random(seed);
            ScatterRow(parent, rule.Floor, rule.FloorDensity, width, baseY + 0.15f, 0f, -2, blockedFloor, random, blockedWall);
            ScatterRow(parent, rule.Wall, rule.WallDensity, width, baseY + 1.75f, 0.5f, -4, blockedWall, random);
        }

        private static void ScatterRow(Transform parent, string[] keys, float density, float width, float y, float yJitter, int order, List<(float, float)> blocked, System.Random random, List<(float, float)> tallBlocked = null)
        {
            if (keys == null || keys.Length == 0 || density <= 0f)
            {
                return;
            }

            var x = 1.2f + (float)random.NextDouble() * 2f;
            while (x < width - 1.2f)
            {
                var key = keys[random.Next(keys.Length)];
                var sprite = ExplorationArt.Get(key);
                var jitter = (float)random.NextDouble();
                var gap = 1f / density * (0.6f + (float)random.NextDouble() * 0.8f);
                if (sprite != null)
                {
                    var half = sprite.bounds.size.x * 0.5f;
                    var free = true;
                    foreach (var (from, to) in blocked)
                    {
                        if (x + half > from && x - half < to)
                        {
                            free = false;
                            break;
                        }
                    }

                    // 키 큰 바닥 장식(자판기 등)은 창문·벽 장식 자리도 피한다
                    if (free && tallBlocked != null && sprite.bounds.size.y > 1.5f)
                    {
                        foreach (var (from, to) in tallBlocked)
                        {
                            if (x + half > from && x - half < to)
                            {
                                free = false;
                                break;
                            }
                        }
                    }

                    if (free)
                    {
                        // 도트 격자에 맞춘 자리 (피벗 = 아래 가운데)
                        var px = Mathf.Round(x * ExplorationArt.PixelsPerUnit) / ExplorationArt.PixelsPerUnit;
                        var py = Mathf.Round((y + jitter * yJitter) * ExplorationArt.PixelsPerUnit) / ExplorationArt.PixelsPerUnit;
                        ExplorationArt.Place(parent, "Decor_" + key, key, new Vector2(px, py), order, Vector2.zero, Color.clear);
                        blocked.Add((x - half - 0.2f, x + half + 0.2f));
                    }
                }

                x += Mathf.Max(0.8f, gap);
            }
        }

        /// <summary>실행할 때마다 같은 값이 나오는 문자열 해시 (string.GetHashCode는 실행마다 달라질 수 있다).</summary>
        private static int StableHash(string text)
        {
            unchecked
            {
                var hash = (int)2166136261;
                foreach (var c in text ?? "")
                {
                    hash = (hash ^ c) * 16777619;
                }

                return hash & 0x7fffffff;
            }
        }
    }
}
