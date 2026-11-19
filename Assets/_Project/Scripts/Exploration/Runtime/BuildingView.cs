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

        public BuildingView(Transform root, BuildingLayoutDef layout)
        {
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
            Rect(root, "Void", new Vector2(-30f, -20f), new Vector2(room.Width + 60f, 40f), new Color(0.05f, 0.05f, 0.06f), -12);
            ExplorationArt.Tiled(root, "Interior", ExplorationArt.InteriorWall(room.Template.Id), new Vector2(0f, 0.15f), new Vector2(room.Width, h - 0.3f), -5,
                RoomColors[colorIndex % RoomColors.Length]);
            if (room.Template.Id.Contains("CLASSROOM"))
            {
                // 칠판: 교실 앞쪽 (창문과 겹치지 않게 뒤쪽 벽면 위쪽)
                ExplorationArt.Place(root, "Chalkboard", "prop_chalkboard", new Vector2(room.Width * 0.5f - 3f, 1.5f), -4, Vector2.zero, Color.clear);
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
                ExplorationArt.Stretched(root, "Window", "prop_window", new Vector2(window.X, WindowBottom), new Vector2(window.Width, WindowHeight), -4, WindowColor);
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
                ExplorationArt.Tiled(_root, "Corridor" + floor, "wall_corridor", new Vector2(0f, y + 0.15f), new Vector2(length, h - 0.3f), -6, CorridorColor);
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
                ExplorationArt.Tiled(_root, "RoomWall_" + door.RoomId, "wall_room_outside", new Vector2(door.X, y + 0.15f), new Vector2(template.Width, h - 0.3f), -5, RoomWallColor);
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
        }
    }
}
