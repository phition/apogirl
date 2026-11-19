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
        public static readonly Color VisitedDoorColor = new Color(0.16f, 0.17f, 0.2f);

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
            Rect(root, "Interior", new Vector2(0f, 0.15f), new Vector2(room.Width, h - 0.3f), RoomColors[colorIndex % RoomColors.Length], -5);
            Rect(root, "Floor", new Vector2(-0.3f, -0.15f), new Vector2(room.Width + 0.6f, 0.3f), new Color(0.09f, 0.09f, 0.1f), -3);
            Rect(root, "Ceiling", new Vector2(-0.3f, h - 0.15f), new Vector2(room.Width + 0.6f, 0.3f), new Color(0.09f, 0.09f, 0.1f), -3);
            Rect(root, "WallL", new Vector2(-0.3f, 0f), new Vector2(0.3f, h), new Color(0.1f, 0.1f, 0.11f), -3);
            Rect(root, "WallR", new Vector2(room.Width, 0f), new Vector2(0.3f, h), new Color(0.1f, 0.1f, 0.11f), -3);

            // 뒷벽 창문 (분위기용)
            for (var x = 3f; x < room.Width - 2f; x += 4f)
            {
                Rect(root, "Window", new Vector2(x, 2.1f), new Vector2(2.2f, 1.2f), new Color(0.42f, 0.47f, 0.5f), -4);
            }
        }

        private void BuildCorridors()
        {
            var h = _layout.FloorHeight;
            var length = _layout.CorridorLength;
            Rect(_root, "Ground", new Vector2(-30f, -3f), new Vector2(length + 60f, 3f), new Color(0.12f, 0.13f, 0.12f), -10);
            Rect(_root, "Shell", new Vector2(-0.6f, 0f), new Vector2(length + 1.2f, _layout.Floors * h + 0.4f), new Color(0.16f, 0.17f, 0.18f), -9);

            for (var floor = 0; floor < _layout.Floors; floor++)
            {
                var y = FloorY(floor);
                Rect(_root, "Corridor" + floor, new Vector2(0f, y + 0.15f), new Vector2(length, h - 0.3f), CorridorColor, -5);
                // 복도 창문 띠
                Rect(_root, "CorridorWindows" + floor, new Vector2(0f, y + 2.9f), new Vector2(length, 0.5f), new Color(0.34f, 0.37f, 0.4f), -4);
            }

            for (var floor = 0; floor <= _layout.Floors; floor++)
            {
                Rect(_root, "Slab", new Vector2(0f, FloorY(floor) - 0.15f), new Vector2(length, 0.3f), new Color(0.09f, 0.09f, 0.1f), -3);
            }
        }
    }
}
