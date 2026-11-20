using System;
using System.Collections.Generic;
using ApoGirl.Core.Definitions;
using UnityEngine;

namespace ApoGirl.Exploration.Runtime
{
    /// <summary>
    /// 탐험 그림 표 (Resources/Art/exploration_art.json): 지역·방 성격에 따라 어떤 그림을 쓸지 데이터로 정한다.
    /// - 벽(방 안·복도·복도에서 보이는 방 바깥벽), 문·출구, 컨테이너(상자·책상 등), 조우 물건, 장식(바닥·벽에 흩어 놓는 소품).
    /// - 표에 없거나 그 그림이 아직 없으면 예전 규칙(ContainerSprite 등)과 코드 그림으로 돌아간다.
    /// 주인공 장비 차림(옷·무기별 그림 접두어)과 가방을 붙일 자리(프레임마다 등 위치)도 여기서 찾는다.
    /// </summary>
    public static partial class ExplorationArt
    {
        [Serializable]
        private sealed class WallRule
        {
            public string Location = "";
            public string Template = "";
            public string Key = "";
        }

        [Serializable]
        private sealed class DoorRule
        {
            public string Location = "";
            public string Template = "";
            public string Closed = "";
            public string Open = "";
        }

        [Serializable]
        private sealed class ExitRule
        {
            public string Location = "";
            public string Open = "";
            public string Locked = "";
        }

        [Serializable]
        private sealed class ContainerRule
        {
            public string Location = "";
            public string Template = "";
            public string Id = "";
            public string Kind = "";
            public string Key = "";
        }

        [Serializable]
        private sealed class EncounterRule
        {
            public string Id = "";
            public string Key = "";
        }

        [Serializable]
        public sealed class DecorRule
        {
            public string Location = "";

            /// <summary>"" = 복도, "*" = 모든 방, 그 밖에는 방 템플릿 ID에 들어 있는 글자.</summary>
            public string Template = "";
            public string[] Floor = Array.Empty<string>();
            public string[] Wall = Array.Empty<string>();

            /// <summary>유닛당 놓을 확률에 가까운 값 (0.1 = 10유닛에 하나쯤).</summary>
            public float FloorDensity = 0.1f;
            public float WallDensity = 0.08f;
        }

        [Serializable]
        private sealed class ArtMap
        {
            public WallRule[] Interior = Array.Empty<WallRule>();
            public WallRule[] Corridor = Array.Empty<WallRule>();
            public WallRule[] RoomOutside = Array.Empty<WallRule>();
            public DoorRule[] Doors = Array.Empty<DoorRule>();
            public ExitRule[] Exits = Array.Empty<ExitRule>();
            public ContainerRule[] Containers = Array.Empty<ContainerRule>();
            public EncounterRule[] Encounters = Array.Empty<EncounterRule>();
            public DecorRule[] Decor = Array.Empty<DecorRule>();
        }

        [Serializable]
        private sealed class AnchorEntry
        {
            public string Key = "";
            public float X;
            public float Y;
            public float Angle;
        }

        [Serializable]
        private sealed class AnchorFile
        {
            public AnchorEntry[] Frames = Array.Empty<AnchorEntry>();
        }

        private static ArtMap _artMap;
        private static Dictionary<string, AnchorEntry> _anchors;

        private static ArtMap Map
        {
            get
            {
                if (_artMap == null)
                {
                    var asset = Resources.Load<TextAsset>("Art/exploration_art");
                    _artMap = asset != null ? JsonUtility.FromJson<ArtMap>(asset.text) ?? new ArtMap() : new ArtMap();
                }

                return _artMap;
            }
        }

        private static bool Has(string text, string part) => string.IsNullOrEmpty(part) || (!string.IsNullOrEmpty(text) && text.Contains(part));

        private static string MappedWall(WallRule[] rules, string locationId, string templateId)
        {
            foreach (var rule in rules)
            {
                if (Has(locationId, rule.Location) && Has(templateId, rule.Template) && Get(rule.Key) != null)
                {
                    return rule.Key;
                }
            }

            return null;
        }

        /// <summary>방 안쪽 벽: 표(방 템플릿·지역) → 예전 규칙.</summary>
        public static string InteriorWall(string templateId, string locationId)
        {
            return MappedWall(Map.Interior, locationId, templateId) ?? InteriorWall(templateId);
        }

        /// <summary>복도에서 보이는 방 바깥벽.</summary>
        public static string RoomOutsideWall(string locationId, string templateId)
        {
            return MappedWall(Map.RoomOutside, locationId, templateId) ?? "wall_room_outside";
        }

        /// <summary>문 그림 (지역·방 성격별). 없으면 door_closed / door_open.</summary>
        public static string Door(string locationId, string templateId, bool open)
        {
            foreach (var rule in Map.Doors)
            {
                if (Has(locationId, rule.Location) && Has(templateId, rule.Template))
                {
                    var key = open ? rule.Open : rule.Closed;
                    if (Get(key) != null)
                    {
                        return key;
                    }
                }
            }

            return open ? "door_open" : "door_closed";
        }

        /// <summary>출구 그림 (지역별). 없으면 prop_exit / prop_exit_locked.</summary>
        public static string Exit(string locationId, bool locked)
        {
            foreach (var rule in Map.Exits)
            {
                if (Has(locationId, rule.Location))
                {
                    var key = locked ? rule.Locked : rule.Open;
                    if (Get(key) != null)
                    {
                        return key;
                    }
                }
            }

            return locked ? "prop_exit_locked" : "prop_exit";
        }

        /// <summary>컨테이너 그림: 표(지역·방·ID 앞글자·종류)에 맞는 첫 줄. 없으면 null (예전 규칙을 쓴다).</summary>
        public static string Container(string locationId, string templateId, ContainerDef container)
        {
            var kind = container.Kind.ToString();
            foreach (var rule in Map.Containers)
            {
                if (Has(locationId, rule.Location) && Has(templateId, rule.Template)
                    && (string.IsNullOrEmpty(rule.Id) || container.Id.StartsWith(rule.Id, StringComparison.Ordinal))
                    && (string.IsNullOrEmpty(rule.Kind) || rule.Kind == kind)
                    && (!string.IsNullOrEmpty(rule.Template) || !string.IsNullOrEmpty(rule.Id) || string.IsNullOrEmpty(container.SpriteKey))
                    && Get(rule.Key) != null)
                {
                    return rule.Key;
                }
            }

            return null;
        }

        /// <summary>조우 물건 그림 (조우 ID). 없으면 null.</summary>
        public static string Encounter(string encounterId)
        {
            foreach (var rule in Map.Encounters)
            {
                if (rule.Id == encounterId && Get(rule.Key) != null)
                {
                    return rule.Key;
                }
            }

            return null;
        }

        /// <summary>장식 규칙: templateId가 null이면 복도. 맞는 첫 줄.</summary>
        public static DecorRule Decor(string locationId, string templateId)
        {
            foreach (var rule in Map.Decor)
            {
                if (!Has(locationId, rule.Location))
                {
                    continue;
                }

                if (templateId == null ? string.IsNullOrEmpty(rule.Template) : rule.Template == "*" || (!string.IsNullOrEmpty(rule.Template) && templateId.Contains(rule.Template)))
                {
                    return rule;
                }
            }

            return null;
        }

        // ── 주인공 장비 차림 ──

        /// <summary>아이템 ID에서 그림 이름 조각을 뽑는다 (ITEM_WEAPON_BAT → bat). 비면 fallback.</summary>
        public static string ItemToken(string itemId, string itemPrefix, string fallback)
        {
            return !string.IsNullOrEmpty(itemId) && itemId.StartsWith(itemPrefix, StringComparison.Ordinal)
                ? itemId.Substring(itemPrefix.Length).ToLowerInvariant()
                : fallback;
        }

        /// <summary>
        /// 주인공 그림 접두어: player_{옷}_{무기}_ (예: player_padding_bat_). 그 차림 그림이 없으면
        /// 교복+그 무기 → 그 옷+맨손 → 교복+맨손 → 예전 그림(player_) 순서로 물러난다.
        /// </summary>
        public static string PlayerPrefix(string armor, string weapon)
        {
            var candidates = new[]
            {
                $"player_{armor}_{weapon}_", $"player_uniform_{weapon}_", $"player_{armor}_none_", "player_uniform_none_",
            };
            foreach (var prefix in candidates)
            {
                if (HasPixel(prefix + "idle_0") || HasPixel(prefix + "idle"))
                {
                    return prefix;
                }
            }

            return "player_";
        }

        /// <summary>가방 색 (그림 bag_{종류}_{색}).</summary>
        public static readonly string[] BagColors = { "brown", "blue", "pink", "red", "yellow", "green" };

        /// <summary>가방 그림 키: 판의 시드로 색을 하나 고른다 (그 색 그림이 없으면 기본 그림 bag_{종류}).</summary>
        public static string BagKey(string kind, int seed)
        {
            var colored = $"bag_{kind}_{BagColors[(seed & 0x7fffffff) % BagColors.Length]}";
            return Get(colored) != null ? colored : "bag_" + kind;
        }

        /// <summary>
        /// 가방을 붙일 자리: 그 프레임에서 등 위치(발 기준, 오른쪽을 볼 때의 유닛)와 몸통 기울기(도).
        /// 표(Resources/Art/ExplorationPixel/player_anchors.json)에 없으면 false.
        /// </summary>
        public static bool TryGetBackAnchor(string spriteKey, out Vector2 position, out float angle)
        {
            if (_anchors == null)
            {
                _anchors = new Dictionary<string, AnchorEntry>();
                var asset = Resources.Load<TextAsset>("Art/ExplorationPixel/player_anchors");
                var file = asset != null ? JsonUtility.FromJson<AnchorFile>(asset.text) : null;
                if (file != null)
                {
                    foreach (var entry in file.Frames)
                    {
                        _anchors[entry.Key] = entry;
                    }
                }
            }

            if (spriteKey != null && _anchors.TryGetValue(spriteKey, out var found))
            {
                position = new Vector2(found.X, found.Y);
                angle = found.Angle;
                return true;
            }

            position = default;
            angle = 0f;
            return false;
        }

        /// <summary>프레임이 여러 장 있으면(key_0, key_1 …) 그 그림을 돌려 가며 보여 주는 부품을 붙인다 (숨 쉬는 조우 인물 등).</summary>
        public static void Animate(GameObject view, string key, float framesPerSecond)
        {
            if (view == null)
            {
                return;
            }

            var count = FrameCount(key + "_", 0);
            var renderer = view.GetComponent<SpriteRenderer>();
            if (renderer == null || !HasPixel(key + "_0") || count < 2)
            {
                return;
            }

            var frames = new Sprite[count];
            for (var i = 0; i < count; i++)
            {
                frames[i] = Get(key + "_" + i);
            }

            var flipbook = view.AddComponent<SpriteFlipbook>();
            flipbook.Frames = frames;
            flipbook.FramesPerSecond = framesPerSecond;
            flipbook.Offset = (view.name.GetHashCode() & 0xff) / 255f * count;
        }
    }

    /// <summary>스프라이트 프레임을 일정한 빠르기로 돌려 보여 준다.</summary>
    public sealed class SpriteFlipbook : MonoBehaviour
    {
        public Sprite[] Frames = Array.Empty<Sprite>();
        public float FramesPerSecond = 6f;
        public float Offset;

        private SpriteRenderer _renderer;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (_renderer == null || Frames.Length == 0)
            {
                return;
            }

            _renderer.sprite = Frames[(int)(Time.time * FramesPerSecond + Offset) % Frames.Length];
        }
    }
}
