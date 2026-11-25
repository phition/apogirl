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
            public CombatRule Combat = new CombatRule();
        }

        [Serializable]
        public sealed class ImpactEntry
        {
            /// <summary>무기 그림 토큰 (bat·pipe·baton·mop …).</summary>
            public string Weapon = "";

            /// <summary>공격 동작 진행도(0~1) 중 맞는 순간. 16프레임 휘두르기면 8번째 프레임 = 0.5.</summary>
            public float At = 0.5f;

            /// <summary>이 무기의 타격 이펙트 그림 접두어 (fx_hit_heavy_ …). 비면 DefaultFx.</summary>
            public string Fx = "";

            /// <summary>
            /// 공격 프레임을 돌리는 차례 (비면 0..n 고르게). 들어 올리기를 줄이고 머리 위에서 잠깐 멈췄다가
            /// 내려치기를 길게 보여 주려고 쓴다. At은 이 차례 안의 진행도다.
            /// </summary>
            public int[] Frames = Array.Empty<int>();
        }

        /// <summary>
        /// 타격 연출: 공격은 그림에서 무기가 닿는 순간에 판정하고, 그때 맞은 자리에 타격 이펙트를 띄운다.
        /// 이펙트 그림 fx_hit_0..n(파일 수로 셈)이 있으면 그것을 한 번 돌리고, 없으면 코드 그림(흰 십자 번쩍임).
        /// </summary>
        [Serializable]
        public sealed class CombatRule
        {
            public ImpactEntry[] ImpactAt = Array.Empty<ImpactEntry>();
            public float DefaultImpactAt = 0.5f;
            public string DefaultFx = "fx_hit_";
            public float FxFramesPerSecond = 24f;
            public float FxHeight = 1.1f;
            public float FxScale = 1f;

            /// <summary>기절한 좀비·생존자 머리 위에서 도는 별 그림 접두어 (fx_dizzy_0..n, 파일 수로 셈). 그림이 없으면 예전 노란 게이지.</summary>
            public string DizzyFx = "fx_dizzy_";
            public float DizzyFramesPerSecond = 12.5f;

            /// <summary>
            /// 별 그림 밑변의 높이 (발 기준, 유닛)와 머리 쪽으로 옮길 가로 거리 (오른쪽을 볼 때, 유닛).
            /// 기절 동작은 고개를 앞으로 숙여 머리가 몸 가운데에서 비켜나 있고 종류마다 머리 높이도 달라, DizzyOffsets에 그 종류가 있으면 그것.
            /// </summary>
            public float DizzyHeight = 1.3f;
            public float DizzyX;
            public DizzyOffsetEntry[] DizzyOffsets = Array.Empty<DizzyOffsetEntry>();
        }

        /// <summary>종류(zombie_{Kind}_ · survivor)별 기절 별 자리.</summary>
        [Serializable]
        public sealed class DizzyOffsetEntry
        {
            public string Kind = "";
            public float Height;
            public float X;
        }

        /// <summary>그 종류의 기절 별 자리: 밑변 높이(발 기준)와 머리 쪽 가로 거리 (오른쪽을 볼 때, 유닛).</summary>
        public static Vector2 DizzyOffset(string kind)
        {
            var combat = Combat;
            foreach (var entry in combat.DizzyOffsets)
            {
                if (entry != null && entry.Kind == kind)
                {
                    return new Vector2(entry.X, entry.Height);
                }
            }

            return new Vector2(combat.DizzyX, combat.DizzyHeight);
        }

        /// <summary>타격 연출 설정 (exploration_art.json의 Combat).</summary>
        public static CombatRule Combat => Map.Combat ?? new CombatRule();

        /// <summary>그 무기의 맞는 순간 (공격 동작 진행도 0~1).</summary>
        public static float ImpactAt(string weaponToken)
        {
            var combat = Combat;
            foreach (var entry in combat.ImpactAt)
            {
                if (entry.Weapon == weaponToken)
                {
                    return Mathf.Clamp01(entry.At);
                }
            }

            return Mathf.Clamp01(combat.DefaultImpactAt);
        }

        /// <summary>그 무기의 공격 프레임 차례 (없으면 빈 배열 = 고르게).</summary>
        public static int[] AttackFrames(string weaponToken)
        {
            foreach (var entry in Combat.ImpactAt)
            {
                if (entry.Weapon == weaponToken && entry.Frames != null)
                {
                    return entry.Frames;
                }
            }

            return Array.Empty<int>();
        }

        /// <summary>그 무기의 타격 이펙트 그림 접두어 (그림이 없으면 기본 fx_hit_).</summary>
        public static string HitFxPrefix(string weaponToken)
        {
            var combat = Combat;
            var fallback = string.IsNullOrEmpty(combat.DefaultFx) ? "fx_hit_" : combat.DefaultFx;
            foreach (var entry in combat.ImpactAt)
            {
                if (entry.Weapon == weaponToken && !string.IsNullOrEmpty(entry.Fx) && HasPixel(entry.Fx + "0"))
                {
                    return entry.Fx;
                }
            }

            return fallback;
        }

        [Serializable]
        private sealed class AnchorEntry
        {
            public string Key = "";
            public float X;
            public float Y;
            public float Angle;

            /// <summary>그 프레임에서 발이 그림 바닥보다 떠 있는 높이 (유닛). 이만큼 내려 그려 발을 바닥에 붙인다.</summary>
            public float Lift;
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
            // 그림을 빌려 쓰는 아이템(제작 C10)은 standing_map.json ItemLooks 표가 먼저다
            return ApoGirl.Presentation.Screens.StandingConfig.LookToken(itemId, itemPrefix, fallback);
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
            LoadAnchors();
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

        /// <summary>주인공 프레임의 발 높이 (유닛, 표에 없으면 0): 공격·대걸레 그림처럼 발이 그림 바닥보다 뜬 프레임을 바닥에 붙일 때 쓴다.</summary>
        public static float FootLift(string spriteKey)
        {
            LoadAnchors();
            return spriteKey != null && _anchors.TryGetValue(spriteKey, out var found) ? found.Lift : 0f;
        }

        private static void LoadAnchors()
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
