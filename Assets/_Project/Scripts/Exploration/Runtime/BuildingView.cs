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

        public float FloorY(int floor) => floor * _layout.FloorHeight;

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
            var decorKey = !string.IsNullOrEmpty(room.Template.WallDecor) ? room.Template.WallDecor
                : room.Template.Id.Contains("CLASSROOM") ? "prop_chalkboard" : "";
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
            Rect(root, "Floor", new Vector2(-0.3f, -0.15f), new Vector2(room.Width + 0.6f, 0.3f), new Color(0.09f, 0.09f, 0.1f), -3);
            Rect(root, "Ceiling", new Vector2(-0.3f, h - 0.15f), new Vector2(room.Width + 0.6f, 0.3f), new Color(0.09f, 0.09f, 0.1f), -3);
            Rect(root, "WallL", new Vector2(-0.3f, 0f), new Vector2(0.3f, h), new Color(0.1f, 0.1f, 0.11f), -3);
            Rect(root, "WallR", new Vector2(room.Width, 0f), new Vector2(0.3f, h), new Color(0.1f, 0.1f, 0.11f), -3);

            // 복도 쪽 창문: 너머로 복도의 실루엣이 보인다
            foreach (var window in room.Template.Windows)
            {
                // 복도 쪽 벽(앞면)의 창: 반투명 유리라 뒤 벽의 칠판 앞에 겹쳐 그린다
                ExplorationArt.Stretched(root, "Window", "prop_window", new Vector2(window.X, WindowBottom), new Vector2(window.Width, WindowHeight), -2, WindowColor);
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

        private void BuildCorridors()
        {
            var h = _layout.FloorHeight;
            var length = _layout.CorridorLength;
            ExplorationArt.Tiled(_root, "Ground", "ground", new Vector2(-30f, -3f), new Vector2(length + 60f, 3f), -10, new Color(0.12f, 0.13f, 0.12f));
            ExplorationArt.Tiled(_root, "Shell", "wall_brick", new Vector2(-0.6f, 0f), new Vector2(length + 1.2f, _layout.Floors * h + 0.4f), -9, new Color(0.16f, 0.17f, 0.18f));

            for (var floor = 0; floor < _layout.Floors; floor++)
            {
                var y = FloorY(floor);
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
                }

                // 교실 명패 (첫 문 위)
                if (template.DoorXs.Count > 0)
                {
                    ExplorationArt.Place(_root, "Plate", "prop_plate", new Vector2(door.X + template.DoorXs[0] + 1.1f, y + 2.75f), -3, Vector2.zero, Color.clear);
                }
            }

            for (var floor = 0; floor <= _layout.Floors; floor++)
            {
                Rect(_root, "Slab", new Vector2(0f, FloorY(floor) - 0.15f), new Vector2(length, 0.3f), new Color(0.09f, 0.09f, 0.1f), -3);
            }

            // 복도에 흩어 놓는 장식 (그림 표의 Decor): 문·창·계단·출구·컨테이너를 피해서
            var corridorRule = ExplorationArt.Decor(_locationId, null);
            if (corridorRule != null)
            {
                for (var floor = 0; floor < _layout.Floors; floor++)
                {
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
