using System.Collections.Generic;
using UnityEngine;

namespace ApoGirl.Exploration.Runtime
{
    /// <summary>
    /// 탐험 화면 스프라이트 저장소. 찾는 순서: 픽셀 아트(ExplorationPixel, PixelLab·M6-5) → 받은 그림(ExplorationHD) → 코드 그림
    /// (Resources/Art/Exploration/{키}, 생성기: 메뉴 ApoGirl/Art/Build Exploration Sprites).
    /// 스프라이트가 없으면 그레이박스 사각형으로 대체해 아트 없이도 동작한다.
    /// </summary>
    public static partial class ExplorationArt
    {
        private const string Folder = "Art/Exploration/";

        /// <summary>받은 그림 (M6-5, 같은 세계 크기의 4배 픽셀). 없으면 코드 그림.</summary>
        private const string HdFolder = "Art/ExplorationHD/";
        /// <summary>픽셀 아트 (M6-5, PPU 36 = 캐릭터 64px). 동작마다 프레임 수가 달라 FrameCount로 센다.</summary>
        private const string PixelFolder = "Art/ExplorationPixel/";

        /// <summary>픽셀 아트의 유닛당 픽셀 (가져오기 설정 ArtImportSettings.PixelPixelsPerUnit과 같다). 카메라 정수 배율 계산에 쓴다.</summary>
        public const float PixelsPerUnit = 36f;

        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, int> Frames = new Dictionary<string, int>();

        /// <summary>
        /// 플레이를 시작할 때마다 캐시를 비운다. 이 프로젝트는 플레이 모드 진입 때 도메인을 다시 읽지 않아(Enter Play Mode Options)
        /// 비우지 않으면 고친 그림·exploration_art.json·가방 자리표가 에디터를 다시 켤 때까지 옛 값으로 남는다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCaches()
        {
            Cache.Clear();
            Frames.Clear();
            PixelKeys.Clear();
            _artMap = null;
            _anchors = null;
        }

        /// <summary>
        /// 픽셀 아트에 있는 프레임 수 (prefix + 0, 1, 2 …가 이어지는 만큼). 픽셀 아트가 없으면 fallback (코드 그림 프레임 수).
        /// </summary>
        /// <summary>픽셀 아트(ExplorationPixel)에 그 키가 있는지. 픽셀 아트는 정수배가 아닌 크기로 늘리지 않는다.</summary>
        public static bool HasPixel(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            if (!PixelKeys.TryGetValue(key, out var has))
            {
                has = Resources.Load<Sprite>(PixelFolder + key) != null;
                PixelKeys[key] = has;
            }

            return has;
        }

        private static readonly Dictionary<string, bool> PixelKeys = new Dictionary<string, bool>();

        public static int FrameCount(string prefix, int fallback)
        {
            if (!Frames.TryGetValue(prefix, out var count))
            {
                count = 0;
                while (count < 32 && Resources.Load<Sprite>(PixelFolder + prefix + count) != null)
                {
                    count++;
                }

                Frames[prefix] = count;
            }

            return count > 0 ? count : Mathf.Max(1, fallback);
        }

        /// <summary>
        /// 조명을 받는 기본 스프라이트 재질. URP는 2D 기본 재질(Sprite-Lit-Default)을 에디터에서만 돌려주고
        /// 빌드에서는 null이라, 코드로 만든 스프라이트가 조명을 안 받는 Sprites/Default가 된다
        /// (빌드에서 밤·어두운 방이 환하게 보이던 문제, 2026-11-23). 그래서 직접 넣는다.
        /// </summary>
        public static Material LitMaterial => _litMaterial != null ? _litMaterial : _litMaterial = Resources.Load<Material>("Art/exploration_sprite_lit");

        private static Material _litMaterial;

        /// <summary>재질이 없거나 조명을 안 받는 기본 재질이면 조명 받는 재질로 (에디터와 같게).</summary>
        public static SpriteRenderer Lit(SpriteRenderer renderer)
        {
            if (renderer != null && LitMaterial != null && IsUnlitDefault(renderer.sharedMaterial))
            {
                renderer.sharedMaterial = LitMaterial;
            }

            return renderer;
        }

        /// <summary>엔진 내장 Sprites/Default (빌드에서 SpriteRenderer가 처음 받는 재질).</summary>
        public static bool IsUnlitDefault(Material material) => material == null || material.shader == null || material.shader.name == "Sprites/Default";

        public static Sprite Get(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            if (!Cache.TryGetValue(key, out var sprite))
            {
                var family = HdFamily(key);
                sprite = Resources.Load<Sprite>(PixelFolder + key);
                if (sprite == null && family != null)
                {
                    sprite = Resources.Load<Sprite>(PixelFolder + family);
                }

                if (sprite == null)
                {
                    sprite = Resources.Load<Sprite>(HdFolder + key);
                }

                if (sprite == null && family != null)
                {
                    sprite = Resources.Load<Sprite>(HdFolder + family);
                }

                if (sprite == null)
                {
                    sprite = Resources.Load<Sprite>(Folder + key);
                }

                Cache[key] = sprite;
            }

            return sprite;
        }

        /// <summary>
        /// 받은 그림에 동작별 프레임이 없을 때 같이 쓸 그림: 주인공은 대기 그림, 좀비는 대기(휴면은 누운 그림).
        /// </summary>
        private static string HdFamily(string key)
        {
            if (key.StartsWith("player_")) return "player_idle";
            if (key.StartsWith("survivor_")) return "survivor_idle";
            if (key.StartsWith("zombie_"))
            {
                var kind = key.Substring("zombie_".Length);
                var cut = kind.IndexOf('_');
                if (cut > 0)
                {
                    var name = kind.Substring(0, cut);
                    return kind.Substring(cut + 1).StartsWith("dormant") ? "zombie_" + name + "_dormant" : "zombie_" + name + "_idle";
                }
            }

            return null;
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
            var renderer = Lit(go.AddComponent<SpriteRenderer>());
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
            var renderer = Lit(go.AddComponent<SpriteRenderer>());
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

            // 테두리(9-슬라이스)가 있는 그림은 늘리지 않고 가운데 조각을 반복한다 (도트가 찌그러지지 않게)
            if (sprite.border != Vector4.zero)
            {
                var sliced = new GameObject(name);
                sliced.transform.SetParent(parent, false);
                sliced.transform.localPosition = position;
                var slicedRenderer = Lit(sliced.AddComponent<SpriteRenderer>());
                slicedRenderer.sprite = sprite;
                slicedRenderer.drawMode = SpriteDrawMode.Tiled;
                slicedRenderer.tileMode = SpriteTileMode.Continuous;
                slicedRenderer.size = new Vector2(Mathf.Round(size.x * PixelsPerUnit) / PixelsPerUnit, sprite.bounds.size.y);
                slicedRenderer.sortingOrder = order;
                return sliced;
            }

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var bounds = sprite.bounds.size;
            go.transform.localScale = new Vector3(size.x / Mathf.Max(0.01f, bounds.x), size.y / Mathf.Max(0.01f, bounds.y), 1f);
            var renderer = Lit(go.AddComponent<SpriteRenderer>());
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            return go;
        }

        /// <summary>방 템플릿별 안쪽 벽 타일.</summary>
        public static string InteriorWall(string templateId)
        {
            if (templateId.Contains("RESTROOM") || templateId.Contains("SUBWAY")) return "wall_restroom";
            if (templateId.Contains("HOUSE") || templateId.Contains("POLICE")) return "wall_office";
            if (templateId.Contains("MART")) return "wall_store";
            if (templateId.Contains("WATER")) return "wall_pharmacy";
            if (templateId.Contains("OFFICE") && !templateId.Contains("STORE")) return "wall_office";
            if (templateId.Contains("CLASSROOM")) return "wall_classroom";
            if (templateId.Contains("STORE")) return "wall_store";
            if (templateId.Contains("PH_")) return "wall_pharmacy";
            return "wall_corridor";
        }

        /// <summary>지역별 복도(매장 바닥층) 벽 타일.</summary>
        public static string CorridorWall(string locationId)
        {
            locationId ??= string.Empty;
            var mapped = MappedWall(Map.Corridor, locationId, null);
            if (mapped != null) return mapped;
            if (locationId.Contains("STORE")) return "wall_store";
            if (locationId.Contains("PHARMACY")) return "wall_pharmacy";
            if (locationId.Contains("RESIDENTIAL")) return "wall_room_outside";
            if (locationId.Contains("SUBWAY")) return "wall_restroom";
            if (locationId.Contains("MART")) return "wall_store";
            if (locationId.Contains("POLICE")) return "wall_corridor";
            if (locationId.Contains("WATERWORKS")) return "wall_restroom";
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
