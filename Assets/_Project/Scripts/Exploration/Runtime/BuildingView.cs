using System.Collections.Generic;
using ApoGirl.Core.Definitions;
using UnityEngine;

namespace ApoGirl.Exploration.Runtime
{
    /// <summary>
    /// 건물 레이아웃 데이터를 그레이박스 도형으로 그린다 (색 사각형). 아트 단계에서 Tilemap/스프라이트로 교체한다.
    /// 좌표: 층 f의 바닥 높이 = f × FloorHeight, X는 레이아웃 X 그대로.
    /// </summary>
    public sealed class BuildingView
    {
        private static readonly Color[] RoomColors =
        {
            new Color(0.27f, 0.29f, 0.31f),
            new Color(0.31f, 0.30f, 0.27f),
            new Color(0.25f, 0.28f, 0.27f),
        };

        private static Sprite _square;
        private readonly Transform _root;
        private readonly BuildingLayoutDef _layout;

        public BuildingView(Transform root, BuildingLayoutDef layout)
        {
            _root = root;
            _layout = layout;
            Build();
        }

        public float FloorHeight => _layout.FloorHeight;

        public float Width => _layout.Width;

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

        /// <summary>층 안에서 이동 가능한 X 범위 (방들의 합집합 경계).</summary>
        public (float min, float max) FloorBounds(int floor)
        {
            var min = float.MaxValue;
            var max = float.MinValue;
            foreach (var room in _layout.Rooms)
            {
                if (room.Floor != floor)
                {
                    continue;
                }

                min = Mathf.Min(min, room.X);
                max = Mathf.Max(max, room.X + room.Width);
            }

            return min > max ? (0f, _layout.Width) : (min + 0.3f, max - 0.3f);
        }

        public RoomDef RoomAt(int floor, float x)
        {
            foreach (var room in _layout.Rooms)
            {
                if (room.Floor == floor && x >= room.X && x <= room.X + room.Width)
                {
                    return room;
                }
            }

            return null;
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

        private void Build()
        {
            var h = _layout.FloorHeight;
            Rect(_root, "Ground", new Vector2(-20f, -2f), new Vector2(_layout.Width + 40f, 2f), new Color(0.12f, 0.13f, 0.12f), -10);
            Rect(_root, "Shell", new Vector2(-0.4f, 0f), new Vector2(_layout.Width + 0.8f, _layout.Floors * h + 0.4f), new Color(0.16f, 0.17f, 0.18f), -9);

            var index = 0;
            var drawnWalls = new HashSet<string>();
            foreach (var room in _layout.Rooms)
            {
                var y = FloorY(room.Floor);
                Rect(_root, "Room_" + room.Id, new Vector2(room.X, y + 0.15f), new Vector2(room.Width, h - 0.3f), RoomColors[index++ % RoomColors.Length], -5);
                foreach (var wallX in new[] { room.X, room.X + room.Width })
                {
                    var key = room.Floor + ":" + wallX.ToString("F2");
                    if (drawnWalls.Add(key))
                    {
                        // 문틀 형태: 위쪽만 막힌 벽 (방 사이는 자유롭게 지나간다)
                        Rect(_root, "Wall", new Vector2(wallX - 0.08f, y + 2.4f), new Vector2(0.16f, h - 2.4f), new Color(0.1f, 0.1f, 0.11f), -4);
                    }
                }
            }

            for (var floor = 0; floor <= _layout.Floors; floor++)
            {
                Rect(_root, "Slab", new Vector2(0f, FloorY(floor) - 0.15f), new Vector2(_layout.Width, 0.3f), new Color(0.09f, 0.09f, 0.1f), -3);
            }
        }
    }
}
