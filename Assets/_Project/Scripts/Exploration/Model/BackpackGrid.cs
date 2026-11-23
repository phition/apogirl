using System;
using System.Collections.Generic;
using System.Linq;

namespace ApoGirl.Exploration.Model
{
    /// <summary>배낭에 놓인 아이템 한 칸 묶음.</summary>
    public sealed class PlacedItem
    {
        public string ItemId = "";
        public int X;
        public int Y;
        public int Width = 1;
        public int Height = 1;
        public float Weight;

        /// <summary>가지고 나간 장비를 벗어 넣은 것 (이미 창고에 있으니 전리품이 아니다).</summary>
        public bool Owned;

        /// <summary>무기를 벗어 넣었을 때 그때까지 닳은 양 (다시 차면 이어서 닳는다).</summary>
        public int Wear;

        public bool Covers(int x, int y) => x >= X && x < X + Width && y >= Y && y < Y + Height;
    }

    /// <summary>
    /// 격자형 배낭 (문서 6.3.6). 아이템은 크기(w×h)만큼 칸을 차지한다. 회전은 아직 없음.
    /// </summary>
    public sealed class BackpackGrid
    {
        private readonly List<PlacedItem> _items = new List<PlacedItem>();

        public BackpackGrid(int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            Width = width;
            Height = height;
        }

        public int Width { get; }

        public int Height { get; }

        public IReadOnlyList<PlacedItem> Items => _items;

        public float TotalWeight => _items.Sum(i => i.Weight);

        public PlacedItem ItemAt(int x, int y) => _items.FirstOrDefault(i => i.Covers(x, y));

        public bool CanPlace(int x, int y, int width, int height, PlacedItem ignore = null)
        {
            if (x < 0 || y < 0 || x + width > Width || y + height > Height)
            {
                return false;
            }

            foreach (var item in _items)
            {
                if (item == ignore)
                {
                    continue;
                }

                var overlaps = x < item.X + item.Width && item.X < x + width && y < item.Y + item.Height && item.Y < y + height;
                if (overlaps)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>왼쪽 위부터 첫 빈 자리에 넣는다. 자리가 없으면 null.</summary>
        public PlacedItem TryAutoPlace(string itemId, int width, int height, float weight)
        {
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    if (CanPlace(x, y, width, height))
                    {
                        var placed = new PlacedItem { ItemId = itemId, X = x, Y = y, Width = width, Height = height, Weight = weight };
                        _items.Add(placed);
                        return placed;
                    }
                }
            }

            return null;
        }

        /// <summary>이미 만든 아이템을 왼쪽 위부터 첫 빈 자리에 넣는다 (표시·닳은 양을 그대로 옮길 때). 자리가 없으면 false.</summary>
        public bool TryAutoPlace(PlacedItem item)
        {
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    if (CanPlace(x, y, item.Width, item.Height))
                    {
                        item.X = x;
                        item.Y = y;
                        _items.Add(item);
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>아이템을 원래 자리에 다시 넣는다 (꺼냈다가 되돌릴 때). 자리가 막혔으면 false.</summary>
        public bool Insert(PlacedItem item)
        {
            if (_items.Contains(item) || !CanPlace(item.X, item.Y, item.Width, item.Height))
            {
                return false;
            }

            _items.Add(item);
            return true;
        }

        public bool TryMove(PlacedItem item, int x, int y)
        {
            if (!_items.Contains(item) || !CanPlace(x, y, item.Width, item.Height, item))
            {
                return false;
            }

            item.X = x;
            item.Y = y;
            return true;
        }

        public bool Remove(PlacedItem item) => _items.Remove(item);

        public void Clear() => _items.Clear();
    }
}
