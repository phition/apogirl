using System.Collections.Generic;
using UnityEngine;

namespace ApoGirl.Exploration.Runtime
{
    /// <summary>
    /// 탐험 화면 스프라이트 저장소 (Resources/Art/Exploration/{키}). 생성기: 메뉴 ApoGirl/Art/Build Exploration Sprites.
    /// 스프라이트가 없으면 그레이박스 사각형으로 대체해 아트 없이도 동작한다.
    /// </summary>
    public static class ExplorationArt
    {
        private const string Folder = "Art/Exploration/";
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            if (!Cache.TryGetValue(key, out var sprite))
            {
                sprite = Resources.Load<Sprite>(Folder + key);
                Cache[key] = sprite;
            }

            return sprite;
        }

        /// <summary>바닥 기준점(스프라이트 피벗)에 그대로 놓는다. 없으면 fallbackSize 사각형 (아래 가운데 기준).</summary>
        public static GameObject Place(Transform parent, string name, string key, Vector2 position, int order, Vector2 fallbackSize, Color fallbackColor)
        {
            var sprite = Get(key);
            if (sprite == null)
            {
                return BuildingView.Rect(parent, name, position - new Vector2(fallbackSize.x / 2f, 0f), fallbackSize, fallbackColor, order);
            }

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return go;
        }

        /// <summary>사각형 영역을 타일로 채운다 (왼쪽 아래 기준). 없으면 단색 사각형.</summary>
        public static GameObject Tiled(Transform parent, string name, string key, Vector2 position, Vector2 size, int order, Color fallbackColor)
        {
            var sprite = Get(key);
            if (sprite == null)
            {
                return BuildingView.Rect(parent, name, position, size, fallbackColor, order);
            }

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = size;
            renderer.sortingOrder = order;
            return go;
        }

        /// <summary>영역에 맞게 늘려 놓는다 (창문처럼 크기가 데이터로 정해진 것, 왼쪽 아래 기준).</summary>
        public static GameObject Stretched(Transform parent, string name, string key, Vector2 position, Vector2 size, int order, Color fallbackColor)
        {
            var sprite = Get(key);
            if (sprite == null)
            {
                return BuildingView.Rect(parent, name, position, size, fallbackColor, order);
            }

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var bounds = sprite.bounds.size;
            go.transform.localScale = new Vector3(size.x / Mathf.Max(0.01f, bounds.x), size.y / Mathf.Max(0.01f, bounds.y), 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return go;
        }

        /// <summary>방 템플릿별 안쪽 벽 타일.</summary>
        public static string InteriorWall(string templateId)
        {
            if (templateId.Contains("RESTROOM")) return "wall_restroom";
            if (templateId.Contains("OFFICE") && !templateId.Contains("STORE")) return "wall_office";
            if (templateId.Contains("CLASSROOM")) return "wall_classroom";
            if (templateId.Contains("STORE")) return "wall_store";
            if (templateId.Contains("PH_")) return "wall_pharmacy";
            return "wall_corridor";
        }

        /// <summary>적 유형별 스프라이트 접두어.</summary>
        public static string ZombieKind(string enemyId)
        {
            if (enemyId.Contains("LISTENER")) return "listener";
            if (enemyId.Contains("WATCHER")) return "watcher";
            return "wanderer";
        }
    }
}
