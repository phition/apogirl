using System.Collections.Generic;
using ApoGirl.Core.Definitions;
using ApoGirl.Data;
using UnityEditor;
using UnityEngine;

namespace ApoGirl.EditorTools
{
    /// <summary>
    /// 탐험 지역 레이아웃 생성기 (메뉴 ApoGirl/Content/Build Locations). 건물 단면은 코드로 짓고 결과를 LocationAsset으로 저장한다.
    /// 수치·배치를 바꾸려면 이 파일을 고친 뒤 메뉴를 다시 실행한다 (에셋은 생성물).
    /// 지역 목록: 튜토리얼(체육관 창고 → 본관 창고), 학교 교실동, 편의점, 약국·의원, 주택가(M6), 지하철역(M6), 마트·물류창고(M6), 파출소(M6), 정수장 연구동(M6).
    /// </summary>
    public static class LocationBuilder
    {
        private const string Folder = "Assets/_Project/Data/Locations";

        [MenuItem("ApoGirl/Content/Build Locations")]
        public static void BuildAll()
        {
            var assets = new List<LocationAsset>
            {
                Save("LOC_TUTORIAL", Tutorial()),
                Save("LOC_SCHOOL_INTERIOR", WithCd(School(), "CD_01", 2, "ROOM_3F_ROOM_MUSIC_7")),
                Save("LOC_STORE", WithCd(Store(), "CD_02", 0, "ROOM_STORE_OFFICE_1")),
                Save("LOC_PHARMACY", WithCd(Pharmacy(), "CD_03", 1, "ROOM_PH_CLINIC_1")),
                // 깊은 곳이 있는 지역의 CD는 깊은 곳 특별한 방 안에 (층 = 깊은 곳 층 번호)
                Save("LOC_RESIDENTIAL", WithCd(ResidentialDeep(Residential()), "CD_04", 3, "ROOM_DEEP_RES_2_V", DeepRoomItemX)),
                Save("LOC_SUBWAY", WithCd(SubwayDeep(Subway()), "CD_05", 2, "ROOM_DEEP_SUB_1_B", DeepRoomItemX)),
                Save("LOC_MART", WithCd(MartDeep(Mart()), "CD_06", 3, "ROOM_DEEP_MART_2_V", DeepRoomItemX)),
                Save("LOC_POLICE", WithCd(PoliceDeep(Police()), "CD_07", 3, "ROOM_DEEP_POLICE_2_B", DeepRoomItemX)),
                Save("LOC_WATERWORKS", WithCd(WaterworksDeep(Waterworks()), "CD_08", 5, "ROOM_DEEP_WATER_3_V", DeepRoomItemX)),
            };

            var database = AssetDatabase.LoadAssetAtPath<DefinitionDatabaseAsset>(ContentPipeline.DatabasePath);
            database.EditorSetLocations(assets);
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            Debug.Log("[Locations] 지역 " + assets.Count + "곳 생성");
        }

        private static LocationAsset Save(string id, LocationDef definition)
        {
            definition.Id = id;
            Diversify(definition);
            foreach (var issue in ApoGirl.Core.Expedition.DeepRules.Issues(definition.Layout))
            {
                Debug.LogError($"[Locations] {id} 깊은 곳 배치: {issue}");
            }

            var path = $"{Folder}/{id}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<LocationAsset>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<LocationAsset>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.EditorSetId(id);
            asset.EditorSetDefinition(definition);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        // ── 공통 도우미 ──

        /// <summary>한 템플릿에서 만드는 배치 수 (원래 배치 포함).</summary>
        private const int LayoutVariants = 4;

        /// <summary>
        /// 방 소품 배치 다양화 (사용자 요청 2026-11-26): 같은 템플릿을 쓰는 방이 둘 이상이면 소품 차례와 간격을 바꾼 배치를
        /// 더 만들어(템플릿 ID 뒤에 _L1~) 방마다 돌려 쓴다. 상자 ID·종류·전리품은 그대로라 수확과 고갈 기록은 같다.
        /// 사람·CD 등 만남이 놓인 방은 자리가 어긋나지 않게 원래 배치를 쓴다. 깊은 곳 특별한 방은 손대지 않는다.
        /// </summary>
        private static void Diversify(LocationDef location)
        {
            var layout = location.Layout;
            var hosts = new HashSet<string>();
            foreach (var variant in new[] { location.DayVariant, location.NightVariant })
            {
                foreach (var encounter in variant.Encounters)
                {
                    if (!string.IsNullOrEmpty(encounter.RoomId))
                    {
                        hosts.Add(encounter.RoomId);
                    }
                }
            }

            foreach (var template in layout.RoomTemplates.ToArray())
            {
                if (template.Id.Contains("DEEP") || template.Containers.Count < 2)
                {
                    continue;
                }

                var doors = layout.Doors.FindAll(d => d.TemplateId == template.Id && d.Seal == DoorSeal.None && !hosts.Contains(d.RoomId));
                if (layout.Doors.FindAll(d => d.TemplateId == template.Id).Count < 2 || doors.Count == 0)
                {
                    continue;
                }

                var count = Mathf.Min(LayoutVariants, doors.Count + (doors.Count < layout.Doors.FindAll(d => d.TemplateId == template.Id).Count ? 1 : 0));
                var made = new List<RoomTemplateDef> { template };
                for (var k = 1; k < count; k++)
                {
                    var alternate = Rearranged(template, k);
                    layout.RoomTemplates.Add(alternate);
                    made.Add(alternate);
                }

                // 원래 배치는 만남이 있는 방이 이미 쓰고 있으면 나머지 방은 새 배치부터 돌려 쓴다
                var first = doors.Count < layout.Doors.FindAll(d => d.TemplateId == template.Id).Count ? 1 : 0;
                for (var i = 0; i < doors.Count; i++)
                {
                    doors[i].TemplateId = made[(first + i) % made.Count].Id;
                }
            }
        }

        /// <summary>소품 차례와 간격을 바꾼 템플릿 사본. k = 1 뒤집기 · 2 번갈아 · 3 반 돌리기, 남는 자리도 k마다 다르게 나눈다.</summary>
        private static RoomTemplateDef Rearranged(RoomTemplateDef source, int k)
        {
            var sorted = new List<ContainerDef>(source.Containers);
            sorted.Sort((a, b) => a.X.CompareTo(b.X));
            var n = sorted.Count;

            // 소품 반폭: 원래 배치에서 이웃과의 간격 절반 (그림 폭을 몰라도 겹치지 않게)
            var half = new float[n];
            for (var i = 0; i < n; i++)
            {
                var left = i > 0 ? sorted[i].X - sorted[i - 1].X : float.MaxValue;
                var right = i < n - 1 ? sorted[i + 1].X - sorted[i].X : float.MaxValue;
                half[i] = Mathf.Clamp(Mathf.Min(left, right) * 0.5f, 0.6f, 1.6f);
            }

            var order = new List<int>();
            switch (k % 3)
            {
                case 1:
                    for (var i = n - 1; i >= 0; i--) order.Add(i);
                    break;
                case 2:
                    for (var i = 0; i < n; i += 2) order.Add(i);
                    for (var i = n - 1 - (n % 2 == 0 ? 0 : 1); i >= 1; i -= 2) order.Add(i);
                    break;
                default:
                    for (var i = 0; i < n; i++) order.Add((i + (n + 1) / 2) % n);
                    break;
            }

            var twoDoors = source.DoorXs.Count > 1;
            var start = sorted[0].X - half[0];
            var end = twoDoors ? sorted[n - 1].X + half[n - 1] : Mathf.Max(sorted[n - 1].X + half[n - 1], source.Width - 0.9f);
            var used = 0f;
            foreach (var h in half) used += h * 2f;
            var free = Mathf.Max(0f, end - start - used);

            // 남는 자리 나누기: 틈 n+1개 (맨 앞, 사이, 맨 뒤)
            var weights = new float[n + 1];
            for (var i = 0; i <= n; i++)
            {
                switch (k % 3)
                {
                    case 1: weights[i] = i == 0 ? 3f : i == n ? 0f : 1f; break;                  // 뒤쪽으로 몰기
                    case 2: weights[i] = i == n / 2 ? 4f : i == 0 || i == n ? 0.3f : 0.6f; break;   // 두 무리로 가르기
                    default: weights[i] = i == n ? 3f : i == 0 ? 0.2f : 1f; break;               // 앞쪽으로 몰기
                }
            }

            var total = 0f;
            foreach (var w in weights) total += w;
            var copy = new RoomTemplateDef
            {
                Id = source.Id + "_L" + k, Width = source.Width, DefaultLootTableId = source.DefaultLootTableId, WallDecor = source.WallDecor,
                DoorXs = new List<float>(source.DoorXs), Windows = new List<WindowDef>(source.Windows),
            };
            var x = start;
            for (var slot = 0; slot < n; slot++)
            {
                var index = order[slot];
                var original = sorted[index];
                x += free * weights[slot] / total + half[index];
                copy.Containers.Add(new ContainerDef
                {
                    Id = original.Id, Kind = original.Kind, Floor = original.Floor, X = Mathf.Round(x * 10f) / 10f, SearchSeconds = original.SearchSeconds,
                    LootTableId = original.LootTableId, SpriteKey = original.SpriteKey, Guaranteed = original.Guaranteed, Once = original.Once,
                });
                x += half[index];
            }

            return copy;
        }

        // ── 탐험의 깊이 (기획 B): 깊은 문·깊은 복도·일방 출구 ──
        // 깊은 곳 층 번호 = 겉 층 수 + (지하 단계 - 1). 문 자리는 모든 시드에서 같다.
        // 초반(주택가·지하철): 깊은 문 1, 깊은 곳 2층. 중반(마트·파출소): 깊은 문 2 (지하 1 서쪽, 지하 2 동쪽). 후반(정수장): 2, 3층.

        private static void Deep(BuildingLayoutDef layout, int floors, float length, string locationId, float stairsX)
        {
            layout.DeepFloors = floors;
            layout.DeepCorridorLength = length;
            layout.DeepNameKey = "deep." + locationId;
            for (var level = 1; level < floors; level++)
            {
                // 깊은 곳은 번호가 클수록 아래: 내려가는 계단(왼쪽 아래로 꺾임)이 왼쪽, 올라가는 계단이 오른쪽 —
                // 겉 층(Stairs)과 같은 모양이 되도록 자리를 바꿔 둔다. 가운데 층(정수장 지하 2층)에서 두 그림이 stairsX − 0.7에서 맞붙는다
                var upper = DeepFloor(layout, level);
                layout.Connectors.Add(new ConnectorDef { Kind = ConnectorKind.Stairs, Floor = upper, X = stairsX - 1.4f, TargetFloor = upper + 1 });
                layout.Connectors.Add(new ConnectorDef { Kind = ConnectorKind.Stairs, Floor = upper + 1, X = stairsX, TargetFloor = upper });
            }
        }

        private static int DeepFloor(BuildingLayoutDef layout, int level) => layout.Floors + level - 1;

        /// <summary>깊은 문 하나: 겉 층의 outerX ↔ 지하 level의 deepX (양쪽에 문).</summary>
        private static void DeepDoor(BuildingLayoutDef layout, int outerFloor, float outerX, int level, float deepX)
        {
            var deep = DeepFloor(layout, level);
            layout.Connectors.Add(new ConnectorDef { Kind = ConnectorKind.DeepDoor, Floor = outerFloor, X = outerX, TargetFloor = deep, TargetX = deepX });
            layout.Connectors.Add(new ConnectorDef { Kind = ConnectorKind.DeepDoor, Floor = deep, X = deepX, TargetFloor = outerFloor, TargetX = outerX });
        }

        /// <summary>일방 출구: 지하 level의 deepX → 겉 층의 outerX (겉에는 손잡이 없는 철문만 보인다).</summary>
        private static void OneWay(BuildingLayoutDef layout, int level, float deepX, int outerFloor, float outerX)
        {
            var deep = DeepFloor(layout, level);
            layout.Connectors.Add(new ConnectorDef { Kind = ConnectorKind.OneWayExit, Floor = deep, X = deepX, TargetFloor = outerFloor, TargetX = outerX });
            layout.Connectors.Add(new ConnectorDef { Kind = ConnectorKind.OneWayBack, Floor = outerFloor, X = outerX, TargetFloor = deep, TargetX = deepX });
        }

        private static void DeepBox(BuildingLayoutDef layout, string id, int level, float x, ContainerKind kind, string loot)
        {
            layout.Containers.Add(C(id, kind, DeepFloor(layout, level), x, loot));
        }

        // ── 깊은 곳 특별한 방: 층마다 두 개 (큰 문 하나·빗장 하나). 상자는 늘 차 있고(Guaranteed) 물자가 많다. CD·진실 단서가 여기 있다 ──
        // 방 안 상자 자리 4.5·8·11.5, 문 1.2. 조우(CD·단서)는 상자 사이 DeepRoomItemX.

        private const float DeepRoomWidth = 13f;
        private const float DeepRoomItemX = 6.25f;

        /// <summary>특별한 방 템플릿: 기본 전리품 표는 그 지역 금고 표(LOOT_*_VAULT), 상자는 모두 Guaranteed.</summary>
        private static RoomTemplateDef DeepRoomTemplate(string id, string vaultLoot, params ContainerDef[] containers)
        {
            var template = new RoomTemplateDef { Id = id, Width = DeepRoomWidth, DefaultLootTableId = vaultLoot, DoorXs = new List<float> { 1.2f } };
            foreach (var container in containers)
            {
                container.Guaranteed = true;
                template.Containers.Add(container);
            }

            return template;
        }

        /// <summary>특별한 방 안 상자 (loot 비우면 금고 표, sprite 비우면 지역·종류별 그림).</summary>
        private static ContainerDef DC(string id, ContainerKind kind, float x, string loot = "", string sprite = "")
        {
            var container = C(id, kind, 0, x, loot);
            container.SpriteKey = sprite;
            return container;
        }

        /// <summary>특별한 방 하나: 지하 level 복도의 spanX에 문 (문 자리 = spanX + 1.2). 빗장 방에는 갇힌 감염자.</summary>
        private static void DeepRoom(LocationDef location, string roomId, string template, int level, float spanX, DoorSeal seal)
        {
            var layout = location.Layout;
            var floor = DeepFloor(layout, level);
            layout.Doors.Add(new DoorDef { RoomId = roomId, TemplateId = template, Floor = floor, X = spanX, Number = level, Seal = seal });
            if (seal == DoorSeal.Barred)
            {
                // 누군가 밖에서 빗장을 채워 가둬 둔 방: 낮엔 둘이 잠들어 있고, 밤엔 하나가 서성인다
                location.DayVariant.Spawns.Add(Crowd("ENEMY_WANDERER", floor, roomId, 9.5f, 2, 1f));
                location.NightVariant.Spawns.Add(Walk("ENEMY_WANDERER", floor, roomId, 9.5f, 1, 0f));
            }
        }

        /// <summary>이미 만든 조우(진실 단서 등)를 깊은 곳 특별한 방 안으로 옮긴다 (낮·밤 모두).</summary>
        private static void MoveEncounter(LocationDef location, string id, int level, string roomId, float x)
        {
            var floor = DeepFloor(location.Layout, level);
            foreach (var variant in new[] { location.DayVariant, location.NightVariant })
            {
                if (variant == null)
                {
                    continue;
                }

                foreach (var encounter in variant.Encounters)
                {
                    if (encounter.Id == id)
                    {
                        encounter.Floor = floor;
                        encounter.RoomId = roomId;
                        encounter.X = x;
                    }
                }
            }
        }

        /// <summary>주택가: 빌라 세 동을 잇는 지하 주차장. 1층 가운데 방화문 → 지하 1 서쪽, 지하 2 서쪽 끝 비상구 → 2층.</summary>
        private static LocationDef ResidentialDeep(LocationDef location)
        {
            var layout = location.Layout;
            Deep(layout, 2, 44f, "LOC_RESIDENTIAL", 40f);
            DeepDoor(layout, 0, 36f, 1, 3f);
            OneWay(layout, 2, 4f, 1, 48f);
            DeepBox(layout, "C_RES_DEEP_TRUNK", 1, 14f, ContainerKind.Box, "LOOT_RESIDENTIAL_DEEP");
            DeepBox(layout, "C_RES_DEEP_LOCKER", 1, 26f, ContainerKind.Locker, "LOOT_RESIDENTIAL_DEEP");
            DeepBox(layout, "C_RES_DEEP_STORAGE", 2, 22f, ContainerKind.Cabinet, "LOOT_RESIDENTIAL_DEEP");
            DeepBox(layout, "C_RES_DEEP_BOX", 2, 32f, ContainerKind.Box, "LOOT_HOUSE_KITCHEN");
            location.DayVariant.Spawns.Add(Crowd("ENEMY_WANDERER", DeepFloor(layout, 1), "", 20f, 2, 1.5f));
            location.DayVariant.Spawns.Add(Walk("ENEMY_LISTENER", DeepFloor(layout, 2), "", 28f, 1, 0f));
            location.NightVariant.Spawns.Add(Walk("ENEMY_WANDERER", DeepFloor(layout, 1), "", 20f, 2, 6f));
            location.NightVariant.Spawns.Add(Walk("ENEMY_WANDERER", DeepFloor(layout, 2), "", 26f, 2, 6f));

            // 특별한 방: 관리실 금고방(큰 문)·지하 창고(빗장). CD_04는 지하 2 금고방
            layout.RoomTemplates.Add(DeepRoomTemplate("ROOM_DEEP_RES_VAULT", "LOOT_RESIDENTIAL_VAULT",
                DC("SAFE", ContainerKind.Box, 4.5f, "", "prop_mart_safe"), DC("LOCKER", ContainerKind.Locker, 8f), DC("TRUNK", ContainerKind.Box, 11.5f, "LOOT_RESIDENTIAL_DEEP")));
            layout.RoomTemplates.Add(DeepRoomTemplate("ROOM_DEEP_RES_STORE", "LOOT_RESIDENTIAL_VAULT",
                DC("SHELF", ContainerKind.Cabinet, 4.5f, "LOOT_RESIDENTIAL_DEEP"), DC("BOX", ContainerKind.Box, 8f, "LOOT_HOUSE_KITCHEN"), DC("CRATE", ContainerKind.Box, 11.5f)));
            DeepRoom(location, "ROOM_DEEP_RES_1_B", "ROOM_DEEP_RES_STORE", 1, 8f, DoorSeal.Barred);
            DeepRoom(location, "ROOM_DEEP_RES_1_V", "ROOM_DEEP_RES_VAULT", 1, 30f, DoorSeal.Heavy);
            DeepRoom(location, "ROOM_DEEP_RES_2_V", "ROOM_DEEP_RES_VAULT", 2, 9f, DoorSeal.Heavy);
            DeepRoom(location, "ROOM_DEEP_RES_2_B", "ROOM_DEEP_RES_STORE", 2, 26f, DoorSeal.Barred);
            return location;
        }

        /// <summary>지하철역: 역무 구역·환기 통로. 승강장 층 방화문 → 지하 1, 지하 2 끝 비상구 → 대합실.</summary>
        private static LocationDef SubwayDeep(LocationDef location)
        {
            var layout = location.Layout;
            Deep(layout, 2, 48f, "LOC_SUBWAY", 44f);
            DeepDoor(layout, 0, 40f, 1, 3f);
            OneWay(layout, 2, 5f, 1, 56f);
            DeepBox(layout, "C_SUB_DEEP_CABINET", 1, 16f, ContainerKind.Cabinet, "LOOT_SUBWAY_DEEP");
            DeepBox(layout, "C_SUB_DEEP_LOCKER", 1, 30f, ContainerKind.Locker, "LOOT_SUBWAY_DEEP");
            DeepBox(layout, "C_SUB_DEEP_DESK", 2, 20f, ContainerKind.Desk, "LOOT_SUBWAY_DEEP");
            DeepBox(layout, "C_SUB_DEEP_BOX", 2, 34f, ContainerKind.Box, "LOOT_SUBWAY_OFFICE");
            location.DayVariant.Spawns.Add(Crowd("ENEMY_WANDERER", DeepFloor(layout, 1), "", 24f, 3, 2f));
            location.DayVariant.Spawns.Add(Walk("ENEMY_LISTENER", DeepFloor(layout, 2), "", 30f, 1, 0f));
            location.NightVariant.Spawns.Add(Walk("ENEMY_WANDERER", DeepFloor(layout, 1), "", 22f, 2, 8f));
            location.NightVariant.Spawns.Add(Walk("ENEMY_LISTENER", DeepFloor(layout, 2), "", 28f, 1, 0f));

            // 특별한 방: 역무 설비실(큰 문, 지하 2에 마지막 방송이 남은 녹음기)·비상 대피 창고(빗장, CD_05는 지하 1)
            layout.RoomTemplates.Add(DeepRoomTemplate("ROOM_DEEP_SUB_VAULT", "LOOT_SUBWAY_VAULT",
                DC("SAFE", ContainerKind.Box, 4.5f, "", "prop_mart_safe"), DC("LOCKER", ContainerKind.Locker, 8f, "", "prop_sub_coinlocker"), DC("CABINET", ContainerKind.Cabinet, 11.5f, "LOOT_SUBWAY_DEEP")));
            layout.RoomTemplates.Add(DeepRoomTemplate("ROOM_DEEP_SUB_SHELTER", "LOOT_SUBWAY_VAULT",
                DC("BOX_A", ContainerKind.Box, 4.5f, "LOOT_SUBWAY_DEEP"), DC("BOX_B", ContainerKind.Box, 8f), DC("CABINET", ContainerKind.Cabinet, 11.5f, "LOOT_SUBWAY_DEEP")));
            DeepRoom(location, "ROOM_DEEP_SUB_1_B", "ROOM_DEEP_SUB_SHELTER", 1, 8f, DoorSeal.Barred);
            DeepRoom(location, "ROOM_DEEP_SUB_1_V", "ROOM_DEEP_SUB_VAULT", 1, 22f, DoorSeal.Heavy);
            DeepRoom(location, "ROOM_DEEP_SUB_2_B", "ROOM_DEEP_SUB_SHELTER", 2, 11f, DoorSeal.Barred);
            DeepRoom(location, "ROOM_DEEP_SUB_2_V", "ROOM_DEEP_SUB_VAULT", 2, 26f, DoorSeal.Heavy);
            MoveEncounter(location, "LAST_BROADCAST", 2, "ROOM_DEEP_SUB_2_V", DeepRoomItemX);
            return location;
        }

        /// <summary>마트·물류창고: 하역장·냉동 창고. 깊은 문 둘 (1층 계산대 뒤 → 지하 1 서쪽, 2층 창고 사이 → 지하 2 동쪽), 비상구 → 2층.</summary>
        private static LocationDef MartDeep(LocationDef location)
        {
            var layout = location.Layout;
            Deep(layout, 2, 50f, "LOC_MART", 26f);
            DeepDoor(layout, 0, 82f, 1, 4f);
            DeepDoor(layout, 1, 45f, 2, 46f);
            OneWay(layout, 2, 4f, 1, 75f);
            DeepBox(layout, "C_MART_DEEP_FREEZER_A", 1, 14f, ContainerKind.Fridge, "LOOT_MART_DEEP");
            DeepBox(layout, "C_MART_DEEP_PALLET_A", 1, 36f, ContainerKind.Box, "LOOT_MART_DEEP");
            DeepBox(layout, "C_MART_DEEP_PALLET_B", 2, 16f, ContainerKind.Box, "LOOT_MART_DEEP");
            DeepBox(layout, "C_MART_DEEP_FREEZER_B", 2, 32f, ContainerKind.Fridge, "LOOT_MART_WAREHOUSE");
            location.DayVariant.Spawns.Add(Crowd("ENEMY_WANDERER", DeepFloor(layout, 1), "", 20f, 3, 2f));
            location.DayVariant.Spawns.Add(Crowd("ENEMY_WANDERER", DeepFloor(layout, 2), "", 28f, 2, 1.5f));
            location.NightVariant.Spawns.Add(Walk("ENEMY_WANDERER", DeepFloor(layout, 1), "", 22f, 2, 8f));
            location.NightVariant.Spawns.Add(Walk("ENEMY_LISTENER", DeepFloor(layout, 2), "", 26f, 1, 0f));

            // 특별한 방: 대형 냉동 창고(큰 문, CD_06은 지하 2)·반품 창고(빗장)
            layout.RoomTemplates.Add(DeepRoomTemplate("ROOM_DEEP_MART_COLD", "LOOT_MART_VAULT",
                DC("FREEZER_A", ContainerKind.Fridge, 4.5f), DC("FREEZER_B", ContainerKind.Fridge, 8f), DC("PALLET", ContainerKind.Box, 11.5f, "LOOT_MART_DEEP")));
            layout.RoomTemplates.Add(DeepRoomTemplate("ROOM_DEEP_MART_STOCK", "LOOT_MART_VAULT",
                DC("PALLET_A", ContainerKind.Box, 4.5f, "LOOT_MART_DEEP"), DC("PALLET_B", ContainerKind.Box, 8f), DC("CAGE", ContainerKind.Cabinet, 11.5f, "LOOT_MART_DEEP")));
            DeepRoom(location, "ROOM_DEEP_MART_1_V", "ROOM_DEEP_MART_COLD", 1, 7f, DoorSeal.Heavy);
            DeepRoom(location, "ROOM_DEEP_MART_1_B", "ROOM_DEEP_MART_STOCK", 1, 39f, DoorSeal.Barred);
            DeepRoom(location, "ROOM_DEEP_MART_2_B", "ROOM_DEEP_MART_STOCK", 2, 8f, DoorSeal.Barred);
            DeepRoom(location, "ROOM_DEEP_MART_2_V", "ROOM_DEEP_MART_COLD", 2, 38f, DoorSeal.Heavy);
            return location;
        }

        /// <summary>파출소: 지하 보관실·옆 건물 기계실. 깊은 문 둘 (1층 → 지하 1 서쪽, 2층 → 지하 2 동쪽), 비상구 → 2층 서쪽.</summary>
        private static LocationDef PoliceDeep(LocationDef location)
        {
            var layout = location.Layout;
            Deep(layout, 2, 40f, "LOC_POLICE", 20f);
            DeepDoor(layout, 0, 34f, 1, 4f);
            DeepDoor(layout, 1, 27f, 2, 36f);
            OneWay(layout, 2, 3f, 1, 8f);
            DeepBox(layout, "C_POLICE_DEEP_EVIDENCE", 1, 12f, ContainerKind.Locker, "LOOT_POLICE_DEEP");
            DeepBox(layout, "C_POLICE_DEEP_CABINET", 1, 30f, ContainerKind.Cabinet, "LOOT_POLICE_OFFICE");
            DeepBox(layout, "C_POLICE_DEEP_LOCKER", 2, 14f, ContainerKind.Locker, "LOOT_POLICE_DEEP");
            DeepBox(layout, "C_POLICE_DEEP_TOOLBOX", 2, 27f, ContainerKind.Box, "LOOT_SUBWAY_OFFICE");
            location.DayVariant.Spawns.Add(Crowd("ENEMY_WANDERER", DeepFloor(layout, 1), "", 24f, 2, 1.5f));
            location.NightVariant.Spawns.Add(Walk("ENEMY_WANDERER", DeepFloor(layout, 1), "", 22f, 2, 6f));
            location.NightVariant.Spawns.Add(Walk("ENEMY_LISTENER", DeepFloor(layout, 2), "", 18f, 1, 0f));

            // 특별한 방: 증거물 보관실(큰 문, 지하 1에 사건 보고서)·임시 보호실(빗장, CD_07은 지하 2)
            layout.RoomTemplates.Add(DeepRoomTemplate("ROOM_DEEP_POLICE_EVIDENCE", "LOOT_POLICE_VAULT",
                DC("SAFE", ContainerKind.Box, 4.5f, "", "prop_mart_safe"), DC("GEAR", ContainerKind.Locker, 8f, "", "prop_pol_gearlocker"), DC("SHELF", ContainerKind.Cabinet, 11.5f, "LOOT_POLICE_DEEP")));
            layout.RoomTemplates.Add(DeepRoomTemplate("ROOM_DEEP_POLICE_HOLD", "LOOT_POLICE_VAULT",
                DC("LOCKER", ContainerKind.Locker, 4.5f, "LOOT_POLICE_DEEP"), DC("BOX", ContainerKind.Box, 8f), DC("DESK", ContainerKind.Desk, 11.5f, "LOOT_POLICE_OFFICE")));
            DeepRoom(location, "ROOM_DEEP_POLICE_1_V", "ROOM_DEEP_POLICE_EVIDENCE", 1, 7f, DoorSeal.Heavy);
            DeepRoom(location, "ROOM_DEEP_POLICE_1_B", "ROOM_DEEP_POLICE_HOLD", 1, 34f, DoorSeal.Barred);
            DeepRoom(location, "ROOM_DEEP_POLICE_2_V", "ROOM_DEEP_POLICE_EVIDENCE", 2, 7f, DoorSeal.Heavy);
            DeepRoom(location, "ROOM_DEEP_POLICE_2_B", "ROOM_DEEP_POLICE_HOLD", 2, 30.5f, DoorSeal.Barred);
            MoveEncounter(location, "POLICE_REPORT", 1, "ROOM_DEEP_POLICE_1_V", DeepRoomItemX);
            return location;
        }

        /// <summary>정수장 연구동: 배관 갤러리·약품 저장고 3층. 깊은 문 둘 (1층 → 지하 1 서쪽, 2층 → 지하 2 동쪽), 지하 3 비상구 → 3층.</summary>
        private static LocationDef WaterworksDeep(LocationDef location)
        {
            var layout = location.Layout;
            Deep(layout, 3, 50f, "LOC_WATERWORKS", 24f);
            DeepDoor(layout, 0, 70f, 1, 4f);
            DeepDoor(layout, 1, 38f, 2, 46f);
            OneWay(layout, 3, 6f, 2, 46f);
            DeepBox(layout, "C_WATER_DEEP_VALVE", 1, 14f, ContainerKind.Box, "LOOT_SUBWAY_OFFICE");
            DeepBox(layout, "C_WATER_DEEP_CHEM_A", 2, 32f, ContainerKind.Cabinet, "LOOT_WATER_DEEP");
            DeepBox(layout, "C_WATER_DEEP_CHEM_B", 3, 16f, ContainerKind.Cabinet, "LOOT_WATER_DEEP");
            DeepBox(layout, "C_WATER_DEEP_CRATE", 3, 38f, ContainerKind.Box, "LOOT_WATER_DEEP");
            location.DayVariant.Spawns.Add(Crowd("ENEMY_WANDERER", DeepFloor(layout, 1), "", 30f, 2, 1.5f));
            location.DayVariant.Spawns.Add(Crowd("ENEMY_WANDERER", DeepFloor(layout, 3), "", 42f, 3, 2f));
            location.NightVariant.Spawns.Add(Walk("ENEMY_WANDERER", DeepFloor(layout, 1), "", 30f, 2, 8f));
            location.NightVariant.Spawns.Add(Walk("ENEMY_LISTENER", DeepFloor(layout, 2), "", 20f, 1, 0f));
            location.NightVariant.Spawns.Add(Walk("ENEMY_WANDERER", DeepFloor(layout, 3), "", 38f, 2, 6f));

            // 특별한 방: 약품 저장고(큰 문, CD_08은 지하 3)·펌프실(빗장)
            layout.RoomTemplates.Add(DeepRoomTemplate("ROOM_DEEP_WATER_CHEM", "LOOT_WATER_VAULT",
                DC("CHEM_A", ContainerKind.Cabinet, 4.5f), DC("CHEM_B", ContainerKind.Cabinet, 8f), DC("CRATE", ContainerKind.Box, 11.5f, "LOOT_WATER_DEEP")));
            layout.RoomTemplates.Add(DeepRoomTemplate("ROOM_DEEP_WATER_PUMP", "LOOT_WATER_VAULT",
                DC("VALVE", ContainerKind.Box, 4.5f, "LOOT_SUBWAY_OFFICE"), DC("CRATE", ContainerKind.Box, 8f), DC("CABINET", ContainerKind.Cabinet, 11.5f, "LOOT_WATER_DEEP")));
            DeepRoom(location, "ROOM_DEEP_WATER_1_B", "ROOM_DEEP_WATER_PUMP", 1, 8f, DoorSeal.Barred);
            DeepRoom(location, "ROOM_DEEP_WATER_1_V", "ROOM_DEEP_WATER_CHEM", 1, 33f, DoorSeal.Heavy);
            DeepRoom(location, "ROOM_DEEP_WATER_2_V", "ROOM_DEEP_WATER_CHEM", 2, 9f, DoorSeal.Heavy);
            DeepRoom(location, "ROOM_DEEP_WATER_2_B", "ROOM_DEEP_WATER_PUMP", 2, 38f, DoorSeal.Barred);
            DeepRoom(location, "ROOM_DEEP_WATER_3_B", "ROOM_DEEP_WATER_PUMP", 3, 10f, DoorSeal.Barred);
            DeepRoom(location, "ROOM_DEEP_WATER_3_V", "ROOM_DEEP_WATER_CHEM", 3, 29f, DoorSeal.Heavy);
            return location;
        }

        /// <summary>
        /// CD 한 장 (기획 A.2): 지역마다 늘 같은 자리, 낮·밤 모두. 챙기면 깃발 {id}.
        /// 깊은 곳이 있는 지역(주택가·지하철·마트·파출소·정수장)의 5장은 깊은 곳 특별한 방 안에 둔다 (층 = 깊은 곳 층).
        /// </summary>
        private static LocationDef WithCd(LocationDef location, string id, int floor, string roomId, float x = 4f)
        {
            EncounterDef Cd() => new EncounterDef
            {
                Id = id, Floor = floor, RoomId = roomId, X = x, SpriteKey = "prop_cd", Collectible = true,
                FlagsNone = { id },
                PromptKey = "enc." + id + ".prompt",
                Choices =
                {
                    new EncounterChoiceDef { Id = "TAKE", TextKey = "enc.cd.take", SetFlags = { id }, StressDelta = -1 },
                    new EncounterChoiceDef { Id = "LEAVE", TextKey = "enc.cd.leave", Removes = false },
                },
            };

            location.DayVariant?.Encounters.Add(Cd());
            location.NightVariant?.Encounters.Add(Cd());
            return location;
        }

        /// <summary>방마다 다른 그림의 상자 (SpriteKey).</summary>
        private static ContainerDef CS(string id, ContainerKind kind, float x, string sprite, string loot = "")
        {
            var container = C(id, kind, 0, x, loot);
            container.SpriteKey = sprite;
            return container;
        }

        private static ContainerDef C(string id, ContainerKind kind, int floor, float x, string loot = "")
        {
            var seconds = kind == ContainerKind.Locker ? 3f : kind == ContainerKind.Desk ? 2f : kind == ContainerKind.Cabinet ? 3.5f : kind == ContainerKind.Fridge ? 4f : 2.5f;
            return new ContainerDef { Id = id, Kind = kind, Floor = floor, X = x, SearchSeconds = seconds, LootTableId = loot };
        }

        /// <summary>반드시 차 있고 한 번 뒤지면 다시 채워지지 않는 상자 (이야기 물건).</summary>
        private static ContainerDef Once(ContainerDef container)
        {
            container.Guaranteed = true;
            container.Once = true;
            return container;
        }

        private static SpawnDef Crowd(string enemy, int floor, string room, float x, int count, float spread)
        {
            return new SpawnDef { EnemyId = enemy, Floor = floor, RoomId = room, X = x, Count = count, Spread = spread, Dormant = true };
        }

        /// <summary>자리를 바꾸지 않는 배치 (연출용: 튜토리얼). 기본은 탐험마다 자리를 다시 고른다.</summary>
        private static SpawnDef Fix(SpawnDef spawn)
        {
            spawn.Fixed = true;
            return spawn;
        }

        private static SpawnDef Walk(string enemy, int floor, string room, float x, int count, float spread)
        {
            return new SpawnDef { EnemyId = enemy, Floor = floor, RoomId = room, X = x, Count = count, Spread = spread, Dormant = false };
        }

        /// <summary>계단 한 줄. collapsedFrom ≥ 0이면 그 층과 위층 사이가 무너져 못 쓴다 (반대쪽 계단으로 돌아가야 한다).</summary>
        private static void Stairs(BuildingLayoutDef layout, float x, int collapsedFrom = -1)
        {
            for (var f = 0; f < layout.Floors - 1; f++)
            {
                layout.Connectors.Add(new ConnectorDef { Kind = ConnectorKind.Stairs, Floor = f, X = x, TargetFloor = f + 1, Collapsed = f == collapsedFrom });
                layout.Connectors.Add(new ConnectorDef { Kind = ConnectorKind.Stairs, Floor = f + 1, X = x - 1.4f, TargetFloor = f, Collapsed = f == collapsedFrom });
            }
        }

        private static void Room(BuildingLayoutDef layout, string roomId, string template, int floor, float spanX, int number, string loot = "")
        {
            layout.Doors.Add(new DoorDef { RoomId = roomId, TemplateId = template, Floor = floor, X = spanX, Number = number, LootTableId = loot });
        }

        // ── 튜토리얼: 체육관 창고 → 본관 창고 (1주차, 문서 6.3.3) ──

        private static LocationDef Tutorial()
        {
            var layout = new BuildingLayoutDef { Floors = 1, FloorHeight = 4f, CorridorLength = 46f };
            layout.RoomTemplates.Add(new RoomTemplateDef
            {
                Id = "ROOM_PE_STORAGE", Width = 12f, DefaultLootTableId = "LOOT_TUTORIAL",
                DoorXs = new List<float> { 1.2f },
                Containers =
                {
                    C("MAT_PILE", ContainerKind.Box, 0, 4f, "LOOT_TUTORIAL_GEAR"),
                    C("BALL_CART", ContainerKind.Box, 0, 7f, "LOOT_TUTORIAL_BAT"),   // 공 수레에 야구 방망이가 꽂혀 있다
                    C("SHELF", ContainerKind.Cabinet, 0, 10f),
                },
            });
            Room(layout, "ROOM_PE_STORAGE_1", "ROOM_PE_STORAGE", 0, 12f, 1);
            layout.Containers.Add(C("C_TUT_LOCKER_A", ContainerKind.Locker, 0, 26f, "LOOT_TUTORIAL"));
            layout.Containers.Add(C("C_TUT_LOCKER_B", ContainerKind.Locker, 0, 34f, "LOOT_TUTORIAL"));
            layout.Containers.Add(C("C_TUT_DESK", ContainerKind.Desk, 0, 40f, "LOOT_TUTORIAL"));
            // 출발점(체육관 창고 쪽 문)은 잠겨 있고, 복도 끝 본관 창고 문으로만 끝낼 수 있다
            layout.Exits.Add(new ExitDef { Kind = ExitKind.Main, Floor = 0, X = 2f, StartsLocked = true });
            layout.Exits.Add(new ExitDef { Kind = ExitKind.Alternate, Floor = 0, X = 44f });

            return new LocationDef
            {
                Layout = layout, DayRunSeconds = 420f, NightRunSeconds = 420f, DefaultLootTableId = "LOOT_TUTORIAL", Tutorial = true,
                DayVariant = new LocationVariantDef { Spawns = { Fix(Crowd("ENEMY_WANDERER", 0, "", 30f, 2, 1.2f)) }, HostileChanceMultiplier = 0f },
                NightVariant = new LocationVariantDef { Spawns = { Fix(Crowd("ENEMY_WANDERER", 0, "", 30f, 2, 1.2f)) } },
            };
        }

        // ── 학교 교실동 (M2b 배치 + M4 도현 조우) ──

        private static LocationDef School()
        {
            var layout = new BuildingLayoutDef { Floors = 4, FloorHeight = 4f, CorridorLength = 124f, RooftopFloor = 3, RooftopFlag = "REACHED_ROOFTOP", RooftopLength = 100f };
            layout.RoomTemplates.Add(new RoomTemplateDef
            {
                Id = "ROOM_CLASSROOM", Width = 14f, DefaultLootTableId = "LOOT_SCHOOL_GENERAL",
                DoorXs = new List<float> { 1.2f, 12.8f },
                Windows = new List<WindowDef> { new WindowDef(2.8f, 3.2f), new WindowDef(7.4f, 3.2f) },
                Containers =
                {
                    C("TEACHER_DESK", ContainerKind.Desk, 0, 3.4f, "LOOT_SCHOOL_TEACHER"),
                    C("DESKS_FRONT", ContainerKind.Desk, 0, 5.6f),
                    C("DESKS_BACK", ContainerKind.Desk, 0, 7.8f),
                    C("CLEANING", ContainerKind.Cabinet, 0, 9.9f, "LOOT_SCHOOL_CLEANING"),
                    C("LOCKERS", ContainerKind.Locker, 0, 11.3f, "LOOT_SCHOOL_LOCKER"),
                },
            });
            layout.RoomTemplates.Add(new RoomTemplateDef
            {
                Id = "ROOM_RESTROOM", Width = 8f, DefaultLootTableId = "LOOT_SCHOOL_GENERAL",
                DoorXs = new List<float> { 1.2f },
                Windows = new List<WindowDef> { new WindowDef(3.2f, 1.6f) },
                Containers = { C("SINK", ContainerKind.Cabinet, 0, 4.2f, "LOOT_SCHOOL_SINK"), C("SUPPLY", ContainerKind.Box, 0, 6.6f) },
            });
            layout.RoomTemplates.Add(new RoomTemplateDef
            {
                Id = "ROOM_OFFICE", Width = 18f, DefaultLootTableId = "LOOT_SCHOOL_TEACHER",
                DoorXs = new List<float> { 1.2f, 16.8f },
                Windows = new List<WindowDef> { new WindowDef(3f, 4f), new WindowDef(9.5f, 4f) },
                Containers =
                {
                    C("DESK_1", ContainerKind.Desk, 0, 3.8f), C("DESK_2", ContainerKind.Desk, 0, 6.4f),
                    C("DESK_3", ContainerKind.Desk, 0, 9.0f), C("DESK_4", ContainerKind.Desk, 0, 11.6f),
                    C("FILES", ContainerKind.Cabinet, 0, 14.0f), C("FRIDGE", ContainerKind.Fridge, 0, 15.4f, "LOOT_SCHOOL_LOCKER"),
                },
            });

            // 특수실 (사용자 요청): 방마다 소품·뒷벽 장식·전리품이 다르다. 문 하나, 창 하나(뒷벽 장식 자리)
            RoomTemplateDef Special(string id, string decor, string loot, params ContainerDef[] containers)
            {
                var template = new RoomTemplateDef
                {
                    Id = id, Width = 14f, DefaultLootTableId = loot, WallDecor = decor,
                    DoorXs = new List<float> { 1.2f, 12.8f },
                    Windows = new List<WindowDef> { new WindowDef(2.6f, 2.6f) },
                };
                template.Containers.AddRange(containers);
                return template;
            }

            layout.RoomTemplates.Add(Special("ROOM_NURSE", "wall_redcross", "LOOT_SCHOOL_NURSE",
                CS("BED_1", ContainerKind.Desk, 3.8f, "prop_bed"), CS("BED_2", ContainerKind.Desk, 6.6f, "prop_bed"),
                CS("MEDCAB", ContainerKind.Cabinet, 9.4f, "prop_medcabinet"), CS("FRIDGE", ContainerKind.Fridge, 11.6f, "prop_fridge")));
            layout.RoomTemplates.Add(Special("ROOM_SCIENCE", "wall_periodic", "LOOT_SCHOOL_SCIENCE",
                CS("BENCH_1", ContainerKind.Desk, 3.8f, "prop_labbench"), CS("BENCH_2", ContainerKind.Desk, 6.8f, "prop_labbench"),
                CS("CHEM", ContainerKind.Cabinet, 9.6f, "prop_chemshelf"), CS("PREP", ContainerKind.Cabinet, 11.6f, "prop_cabinet")));
            layout.RoomTemplates.Add(Special("ROOM_MUSIC", "wall_music", "LOOT_SCHOOL_MUSIC",
                CS("PIANO", ContainerKind.Desk, 4.0f, "prop_piano"), CS("INSTRUMENTS", ContainerKind.Cabinet, 7.6f, "prop_instrument"),
                CS("LOCKERS", ContainerKind.Locker, 10.0f, "prop_locker", "LOOT_SCHOOL_LOCKER"), CS("STANDS", ContainerKind.Box, 11.2f, "prop_box")));
            layout.RoomTemplates.Add(Special("ROOM_HOMEEC", "wall_kitchen", "LOOT_SCHOOL_HOMEEC",
                CS("COUNTER", ContainerKind.Desk, 3.8f, "prop_kitchen"), CS("STOVE", ContainerKind.Desk, 6.6f, "prop_stove"),
                CS("FRIDGE", ContainerKind.Fridge, 9.2f, "prop_fridge"), CS("PANTRY", ContainerKind.Cabinet, 11.4f, "prop_cabinet")));
            layout.RoomTemplates.Add(Special("ROOM_COMPUTER", "wall_whiteboard", "LOOT_SCHOOL_COMPUTER",
                CS("PC_1", ContainerKind.Desk, 3.6f, "prop_computer"), CS("PC_2", ContainerKind.Desk, 6.0f, "prop_computer"),
                CS("PC_3", ContainerKind.Desk, 8.4f, "prop_computer"), CS("RACK", ContainerKind.Cabinet, 11.2f, "prop_rack")));
            layout.RoomTemplates.Add(Special("ROOM_BROADCAST", "wall_onair", "LOOT_SCHOOL_BROADCAST",
                CS("CONSOLE", ContainerKind.Desk, 4.2f, "prop_console"), CS("RACK", ContainerKind.Cabinet, 7.6f, "prop_rack"),
                CS("FUEL", ContainerKind.Box, 9.6f, "prop_box"), CS("LOCKER", ContainerKind.Locker, 11.1f, "prop_locker", "LOOT_SCHOOL_LOCKER")));

            Stairs(layout, 3f);
            Stairs(layout, 121f, collapsedFrom: 1);   // 오른쪽 계단은 2~3층 사이가 무너졌다

            void R(string template, int floor, float spanX, int number) =>
                Room(layout, $"ROOM_{floor + 1}F_{template}_{number}", template, floor, spanX, number);

            // 1층: 교무실·양호실·가사 실습실, 끝 교실(1-5)은 거처 이벤트의 계기
            R("ROOM_OFFICE", 0, 6f, 0);
            R("ROOM_NURSE", 0, 24f, 1);
            R("ROOM_CLASSROOM", 0, 38f, 2);
            R("ROOM_HOMEEC", 0, 60f, 3);
            R("ROOM_CLASSROOM", 0, 74f, 4);
            R("ROOM_CLASSROOM", 0, 88f, 5);
            layout.Doors[layout.Doors.Count - 1].SearchFlag = "SEARCHED_1F_END";
            R("ROOM_RESTROOM", 0, 102f, 0);

            // 2층 끝: 컴퓨터실·과학실, 3층 끝: 방송실·음악실. 4층은 옥상이라 방이 없다
            var specials = new Dictionary<(int Floor, int Number), string>
            {
                { (1, 6), "ROOM_COMPUTER" }, { (1, 7), "ROOM_SCIENCE" }, { (2, 6), "ROOM_BROADCAST" }, { (2, 7), "ROOM_MUSIC" },
            };
            for (var f = 1; f < layout.RooftopFloor; f++)
            {
                for (var i = 0; i < 7; i++) R(specials.TryGetValue((f, i + 1), out var special) ? special : "ROOM_CLASSROOM", f, 6f + 14f * i, i + 1);
                R("ROOM_RESTROOM", f, 104f, 0);
            }

            // 특수실을 처음 뒤지면 발견 장면 (systems_events.json의 EVT_SYS_ROOM_*)
            var roomFlags = new Dictionary<string, string>
            {
                { "ROOM_NURSE", "SEARCHED_NURSE" }, { "ROOM_HOMEEC", "SEARCHED_HOMEEC" }, { "ROOM_COMPUTER", "SEARCHED_COMPUTER" },
                { "ROOM_SCIENCE", "SEARCHED_SCIENCE" }, { "ROOM_MUSIC", "SEARCHED_MUSIC" }, { "ROOM_BROADCAST", "SEARCHED_BROADCAST" },
            };
            foreach (var door in layout.Doors)
            {
                if (string.IsNullOrEmpty(door.SearchFlag) && roomFlags.TryGetValue(door.TemplateId, out var roomFlag)) door.SearchFlag = roomFlag;
            }

            for (var f = 0; f < layout.Floors; f++)
            {
                var lockerA = C($"C_{f + 1}F_HALL_LOCKER_A", ContainerKind.Locker, f, 114.5f, "LOOT_SCHOOL_LOCKER");
                var lockerB = C($"C_{f + 1}F_HALL_LOCKER_B", ContainerKind.Locker, f, f == 0 ? 53.5f : 117.5f, "LOOT_SCHOOL_LOCKER");
                if (f == layout.RooftopFloor)
                {
                    // 옥상: 복도 사물함 대신 비바람에 녹슨 창고함 (상자 번호가 밀리지 않게 자리만 그대로 둔다)
                    // 옥상은 복도보다 짧다(RooftopLength 100): 두 창고함 모두 그 안에
                    lockerA.X = 96f;
                    lockerB.X = 79f;
                    lockerA.SpriteKey = "prop_roof_locker";
                    lockerB.SpriteKey = "prop_roof_locker";
                }
                layout.Containers.Add(lockerA);
                layout.Containers.Add(lockerB);
            }

            // 층마다 분실물 상자 하나: 책가방이 반드시 하나 들어 있다 (교실동에서 기본 가방 4개, 달마다 복구).
            // 상자 번호가 밀리지 않게(뒤진 기록·전리품 시드) 목록 맨 끝에 붙인다
            for (var f = 0; f < layout.Floors; f++)
            {
                var lostAndFound = C($"C_{f + 1}F_LOST_FOUND", ContainerKind.Box, f, 112.7f, "LOOT_SCHOOL_BAG");
                lostAndFound.Guaranteed = true;
                if (f == layout.RooftopFloor)
                {
                    lostAndFound.X = 40f;   // 옥상: 누가 놓고 간 가방 상자
                }
                layout.Containers.Add(lostAndFound);
            }

            layout.Exits.Add(new ExitDef { Kind = ExitKind.Main, Floor = 0, X = 56f });
            layout.Exits.Add(new ExitDef { Kind = ExitKind.Alternate, Floor = 3, X = 99f, StartsLocked = true });

            // 옥상이 짧아 오른쪽 계단은 3층까지만 (옥상 끝 밖으로 나가는 계단은 뺀다)
            layout.Connectors.RemoveAll(c => (c.Floor == layout.RooftopFloor && c.X > layout.RooftopLength)
                || (c.TargetFloor == layout.RooftopFloor && c.Floor != layout.RooftopFloor && c.X > layout.RooftopLength));

            // 도현 (첫 NPC, M4 결정): 밤의 노크 소리를 들은 뒤(DH_HEARD) 3층 3-5 교실에 있다
            EncounterDef Dohyeon() => new EncounterDef
            {
                Id = "DOHYEON", Floor = 2, RoomId = "ROOM_3F_ROOM_CLASSROOM_5", X = 8.85f, SpriteKey = "npc_dohyeon_hiding",
                FlagsAll = { "DH_HEARD" }, FlagsNone = { "DH_RESCUED", "DH_LEFT", "DH_HELPED" },
                PromptKey = "enc.DOHYEON.prompt",
                DialogueNode = "ENC_DOHYEON", SceneEventId = "EVT_DH_02_ARRIVAL",
                Choices =
                {
                    new EncounterChoiceDef { Id = "TAKE", TextKey = "enc.DOHYEON.take", SetFlags = { "DH_RESCUED" }, StressDelta = -3 },
                    new EncounterChoiceDef { Id = "WATER", TextKey = "enc.DOHYEON.water", SetFlags = { "DH_HELPED" } },
                    new EncounterChoiceDef { Id = "LEAVE", TextKey = "enc.DOHYEON.leave", SetFlags = { "DH_LEFT" }, StressDelta = 4 },
                },
            };

            // 교무실 서류 (학교의 비밀 1, 1막 7주 이후)
            EncounterDef Notice() => new EncounterDef
            {
                Id = "SCHOOL_NOTICE", Floor = 0, RoomId = "ROOM_1F_ROOM_OFFICE_0", X = 13f,
                FlagsAll = { "NOTICE_HINT" }, FlagsNone = { "NOTICE_TAKEN", "NOTICE_LEFT" },
                PromptKey = "enc.SCHOOL_NOTICE.prompt",
                Choices =
                {
                    new EncounterChoiceDef { Id = "TAKE", TextKey = "enc.SCHOOL_NOTICE.take", SetFlags = { "NOTICE_TAKEN" }, AddClues = { "CLUE_SCHOOL_NOTICE" } },
                    new EncounterChoiceDef { Id = "LEAVE", TextKey = "enc.SCHOOL_NOTICE.leave", SetFlags = { "NOTICE_LEFT" } },
                },
            };

            var day = new LocationVariantDef
            {
                Spawns =
                {
                    Crowd("ENEMY_WANDERER", 1, "", 55f, 6, 3f),
                    Crowd("ENEMY_WANDERER", 0, "ROOM_1F_ROOM_OFFICE_0", 10f, 3, 3f),
                    Crowd("ENEMY_WANDERER", 2, "", 82f, 4, 2f),
                    Crowd("ENEMY_WANDERER", 1, "ROOM_2F_ROOM_CLASSROOM_4", 8f, 3, 2f),
                    Crowd("ENEMY_WANDERER", 2, "ROOM_3F_ROOM_CLASSROOM_2", 7f, 2, 1.5f),
                    Walk("ENEMY_WANDERER", 0, "", 95f, 1, 0f),
                    Walk("ENEMY_LISTENER", 2, "", 30f, 1, 0f),
                },
                // 4층(옥상)에는 좀비가 없다
                Encounters = { Dohyeon(), Notice() },

                // 교실동에는 적대적 생존자가 나오지 않는다
                HostileChanceMultiplier = 0f,
            };
            var night = new LocationVariantDef
            {
                Spawns =
                {
                    Walk("ENEMY_WANDERER", 0, "", 30f, 2, 8f),
                    Walk("ENEMY_WANDERER", 0, "", 90f, 2, 8f),
                    Walk("ENEMY_WANDERER", 1, "", 27f, 2, 7f),
                    Walk("ENEMY_WANDERER", 1, "", 63f, 3, 8f),
                    Walk("ENEMY_LISTENER", 1, "", 96f, 1, 0f),
                    Walk("ENEMY_WANDERER", 2, "", 45f, 2, 8f),
                    Walk("ENEMY_LISTENER", 2, "", 84f, 2, 7f),
                    Walk("ENEMY_WANDERER", 0, "ROOM_1F_ROOM_OFFICE_0", 9f, 2, 4f),
                    Walk("ENEMY_WANDERER", 1, "ROOM_2F_ROOM_CLASSROOM_2", 7f, 1, 0f),
                    Walk("ENEMY_LISTENER", 2, "ROOM_3F_ROOM_CLASSROOM_5", 3f, 1, 0f),
                },
                Encounters = { Dohyeon(), Notice() },
            };

            return new LocationDef
            {
                Layout = layout, DayRunSeconds = 360f, NightRunSeconds = 360f, DefaultLootTableId = "LOOT_SCHOOL_GENERAL",
                DayVariant = day, NightVariant = night,

                // 교실동은 무한 파밍 불가: 한 번 뒤진 상자는 다음 출정에도 비어 있고, 매달 복구도 없다
                DepletingContainers = true,
                Restocks = false,
            };
        }

        // ── 편의점 (소형, 4~5분, 문서 6.3.8) ──

        private static LocationDef Store()
        {
            var layout = new BuildingLayoutDef { Floors = 1, FloorHeight = 4f, CorridorLength = 40f };
            layout.RoomTemplates.Add(new RoomTemplateDef
            {
                Id = "ROOM_STORE_STOCK", Width = 12f, DefaultLootTableId = "LOOT_STORE_STOCK",
                DoorXs = new List<float> { 1.2f },
                Containers =
                {
                    C("STOCK_1", ContainerKind.Box, 0, 3.6f), C("STOCK_2", ContainerKind.Box, 0, 6f),
                    C("STOCK_3", ContainerKind.Box, 0, 8.4f), C("WALKIN", ContainerKind.Fridge, 0, 10.6f, "LOOT_STORE_FRIDGE"),
                },
            });
            layout.RoomTemplates.Add(new RoomTemplateDef
            {
                Id = "ROOM_STORE_OFFICE", Width = 8f, DefaultLootTableId = "LOOT_STORE_OFFICE",
                DoorXs = new List<float> { 1.2f },
                Containers = { C("OFFICE_DESK", ContainerKind.Desk, 0, 3.8f), C("OFFICE_LOCKER", ContainerKind.Locker, 0, 6.4f) },
            });
            Room(layout, "ROOM_STORE_STOCK_1", "ROOM_STORE_STOCK", 0, 24f, 1);
            Room(layout, "ROOM_STORE_OFFICE_1", "ROOM_STORE_OFFICE", 0, 14f, 1);

            for (var i = 0; i < 6; i++)
            {
                layout.Containers.Add(C($"C_SHELF_{i + 1}", ContainerKind.Box, 0, 5f + i * 1.6f, "LOOT_STORE_SHELF"));
            }

            layout.Containers.Add(C("C_COUNTER", ContainerKind.Desk, 0, 37f, "LOOT_STORE_COUNTER"));
            layout.Containers.Add(C("C_DRINK_FRIDGE", ContainerKind.Fridge, 0, 39f, "LOOT_STORE_FRIDGE"));
            layout.Exits.Add(new ExitDef { Kind = ExitKind.Main, Floor = 0, X = 2f });
            layout.Exits.Add(new ExitDef { Kind = ExitKind.Alternate, Floor = 0, X = 34f, StartsLocked = true });

            return new LocationDef
            {
                Layout = layout, DayRunSeconds = 270f, NightRunSeconds = 240f, DefaultLootTableId = "LOOT_STORE_SHELF",
                DayVariant = new LocationVariantDef
                {
                    Spawns = { Crowd("ENEMY_WANDERER", 0, "", 18f, 3, 1.5f), Walk("ENEMY_WANDERER", 0, "", 30f, 1, 0f), Crowd("ENEMY_WANDERER", 0, "ROOM_STORE_STOCK_1", 6f, 2, 1f) },
                    ForceableRooms = { "ROOM_STORE_OFFICE_1" },
                    Encounters = { FriendTrace() },
                },
                NightVariant = new LocationVariantDef
                {
                    Spawns = { Walk("ENEMY_WANDERER", 0, "", 12f, 2, 5f), Walk("ENEMY_WANDERER", 0, "", 28f, 2, 5f), Walk("ENEMY_LISTENER", 0, "", 36f, 1, 0f) },
                    ForceableRooms = { "ROOM_STORE_OFFICE_1" },
                },
            };
        }

        /// <summary>2막: 편의점 앞 버스 정류장에 친구의 흔적 (문서 7.3, 물건으로만).</summary>
        private static EncounterDef FriendTrace() => new EncounterDef
        {
            Id = "FRIEND_TRACE", Floor = 0, X = 8f,
            FlagsAll = { "A2_STARTED" }, FlagsNone = { "FRIEND_TRACE_FOUND", "FRIEND_TRACE_LEFT" },
            PromptKey = "enc.FRIEND_TRACE.prompt",
            Choices =
            {
                new EncounterChoiceDef { Id = "TAKE", TextKey = "enc.FRIEND_TRACE.take", SetFlags = { "FRIEND_TRACE_FOUND" }, StressDelta = 8 },
                new EncounterChoiceDef { Id = "LEAVE", TextKey = "enc.FRIEND_TRACE.leave", SetFlags = { "FRIEND_TRACE_LEFT" }, StressDelta = 5 },
            },
        };

        // ── 주택가 (낮 전용 성격, 문서 6.3.8): 빌라 골목 2층. 잠긴 102호(낮 셔터, 황혼에 열림)에 박 간호사 ──

        private static LocationDef Residential()
        {
            var layout = new BuildingLayoutDef { Floors = 2, FloorHeight = 4f, CorridorLength = 72f };
            layout.RoomTemplates.Add(new RoomTemplateDef
            {
                Id = "ROOM_HOUSE", Width = 14f, DefaultLootTableId = "LOOT_HOUSE_ROOM",
                DoorXs = new List<float> { 1.2f },
                Windows = new List<WindowDef> { new WindowDef(3f, 3f) },
                Containers =
                {
                    C("KITCHEN", ContainerKind.Cabinet, 0, 4f, "LOOT_HOUSE_KITCHEN"),
                    C("FRIDGE", ContainerKind.Fridge, 0, 6.4f, "LOOT_HOUSE_KITCHEN"),
                    C("DRAWER", ContainerKind.Desk, 0, 9.2f),
                    C("CLOSET", ContainerKind.Locker, 0, 12f),
                },
            });

            // 102호: 박 선생님이 숨어 있는 집. 서랍 자리에 한 번뿐인 상자(의학 논문, 매달 복구 없음)
            layout.RoomTemplates.Add(new RoomTemplateDef
            {
                Id = "ROOM_HOUSE_NURSE", Width = 14f, DefaultLootTableId = "LOOT_HOUSE_ROOM",
                DoorXs = new List<float> { 1.2f },
                Windows = new List<WindowDef> { new WindowDef(3f, 3f) },
                Containers =
                {
                    C("KITCHEN", ContainerKind.Cabinet, 0, 4f, "LOOT_HOUSE_KITCHEN"),
                    C("FRIDGE", ContainerKind.Fridge, 0, 6.4f, "LOOT_HOUSE_KITCHEN"),
                    Once(C("PAPERS", ContainerKind.Desk, 0, 8.4f, "LOOT_HOUSE_NURSE")),
                },
            });

            Stairs(layout, 4f);
            Room(layout, "ROOM_HOUSE_101", "ROOM_HOUSE", 0, 12f, 101);
            Room(layout, "ROOM_HOUSE_102", "ROOM_HOUSE_NURSE", 0, 46f, 102);
            Room(layout, "ROOM_HOUSE_201", "ROOM_HOUSE", 1, 12f, 201);
            Room(layout, "ROOM_HOUSE_202", "ROOM_HOUSE", 1, 30f, 202);
            Room(layout, "ROOM_HOUSE_203", "ROOM_HOUSE", 1, 52f, 203);
            layout.Containers.Add(C("C_RES_MAILBOX", ContainerKind.Box, 0, 28f, "LOOT_HOUSE_ROOM"));
            layout.Containers.Add(C("C_RES_RECYCLE", ContainerKind.Box, 0, 64f, "LOOT_HOUSE_KITCHEN"));
            layout.Exits.Add(new ExitDef { Kind = ExitKind.Main, Floor = 0, X = 69f });
            layout.Exits.Add(new ExitDef { Kind = ExitKind.Alternate, Floor = 1, X = 70f, StartsLocked = true });

            // 잠긴 집: 안에서 인기척. 낮에는 들어갈 수 없고, 문 앞에 물자를 두고 갈 수 있다 (7.3 복선). 박 선생님을 찾은 뒤에는 없다
            var lockedHouse = new EncounterDef
            {
                Id = "LOCKED_HOUSE", Floor = 0, X = 47f,
                FlagsNone = { "LEFT_SUPPLIES_LOCKED_HOUSE", "NURSE_FOUND" },
                PromptKey = "enc.LOCKED_HOUSE.prompt",
                Choices =
                {
                    new EncounterChoiceDef { Id = "SUPPLIES", TextKey = "enc.LOCKED_HOUSE.supplies", SetFlags = { "LEFT_SUPPLIES_LOCKED_HOUSE" }, StressDelta = -3 },
                    new EncounterChoiceDef { Id = "KNOCK", TextKey = "enc.LOCKED_HOUSE.knock", SetFlags = { "KNOCKED_LOCKED_HOUSE" }, Removes = false },
                    new EncounterChoiceDef { Id = "PASS", TextKey = "enc.LOCKED_HOUSE.pass", Removes = false },
                },
            };

            // 박 간호사 (보건 교사): 1층 102호 옷장에 숨어 있다 (낮에는 잠김 — 낮 탐험 시계를 넘겨 황혼이 되면 열린다)
            var nurse = new EncounterDef
            {
                Id = "NURSE", Floor = 0, RoomId = "ROOM_HOUSE_102", X = 11.6f, SpriteKey = "npc_nurse_hiding",
                FlagsAll = { "A2_STARTED" }, FlagsNone = { "NURSE_FOUND", "NURSE_DECLINED" },
                PromptKey = "enc.NURSE.prompt",
                DialogueNode = "ENC_NURSE", SceneEventId = "EVT_A2_NURSE_JOIN",
                Choices =
                {
                    new EncounterChoiceDef { Id = "INVITE", TextKey = "enc.NURSE.invite", SetFlags = { "NURSE_FOUND" }, StressDelta = -4 },
                    new EncounterChoiceDef { Id = "LATER", TextKey = "enc.NURSE.later", SetFlags = { "NURSE_DECLINED" } },
                },
            };

            return new LocationDef
            {
                Layout = layout, DayRunSeconds = 360f, NightRunSeconds = 300f, DefaultLootTableId = "LOOT_HOUSE_ROOM",
                DayVariant = new LocationVariantDef
                {
                    Spawns =
                    {
                        Crowd("ENEMY_WANDERER", 0, "", 22f, 3, 2f),
                        Crowd("ENEMY_WANDERER", 1, "", 40f, 2, 1.5f),
                        Crowd("ENEMY_WANDERER", 0, "ROOM_HOUSE_101", 8f, 2, 1f),
                        Crowd("ENEMY_LISTENER", 1, "ROOM_HOUSE_202", 7f, 1, 0f),
                        Walk("ENEMY_WANDERER", 0, "", 58f, 1, 0f),
                    },
                    LockedRooms = { "ROOM_HOUSE_102" },
                    ForceableRooms = { "ROOM_HOUSE_201" },
                    Encounters = { lockedHouse, nurse },
                },
                NightVariant = new LocationVariantDef
                {
                    Spawns = { Walk("ENEMY_WANDERER", 0, "", 20f, 3, 8f), Walk("ENEMY_WANDERER", 1, "", 40f, 3, 8f), Walk("ENEMY_WATCHER", 0, "", 55f, 1, 0f) },
                    ForceableRooms = { "ROOM_HOUSE_201" },
                    Encounters = { nurse },
                },
            };
        }

        // ── 지하철역 (문서 6.3.8): 위층 대합실, 아래층 승강장 대규모 휴면 군집. 밤에는 터널 대체 추출 ──

        private static LocationDef Subway()
        {
            var layout = new BuildingLayoutDef { Floors = 2, FloorHeight = 4f, CorridorLength = 96f };
            layout.RoomTemplates.Add(new RoomTemplateDef
            {
                Id = "ROOM_SUBWAY_OFFICE", Width = 14f, DefaultLootTableId = "LOOT_SUBWAY_OFFICE",
                DoorXs = new List<float> { 1.2f },
                Windows = new List<WindowDef> { new WindowDef(3f, 3.4f) },
                Containers = { C("OFFICE_DESK", ContainerKind.Desk, 0, 4f), C("OFFICE_CABINET", ContainerKind.Cabinet, 0, 7.4f), C("OFFICE_LOCKER", ContainerKind.Locker, 0, 11f) },
            });
            layout.RoomTemplates.Add(new RoomTemplateDef
            {
                Id = "ROOM_SUBWAY_KIOSK", Width = 10f, DefaultLootTableId = "LOOT_SUBWAY_KIOSK",
                DoorXs = new List<float> { 1.2f },
                Containers = { C("KIOSK_SHELF", ContainerKind.Box, 0, 3.8f), C("KIOSK_FRIDGE", ContainerKind.Fridge, 0, 6.6f, "LOOT_STORE_FRIDGE"), C("KIOSK_BOX", ContainerKind.Box, 0, 8.6f) },
            });
            layout.RoomTemplates.Add(new RoomTemplateDef
            {
                Id = "ROOM_SUBWAY_RESTROOM", Width = 8f, DefaultLootTableId = "LOOT_SUBWAY_KIOSK",
                DoorXs = new List<float> { 1.2f },
                Containers = { C("SINK", ContainerKind.Cabinet, 0, 4.2f), C("SUPPLY", ContainerKind.Box, 0, 6.6f) },
            });

            Stairs(layout, 48f);
            Room(layout, "ROOM_SUBWAY_OFFICE_1", "ROOM_SUBWAY_OFFICE", 1, 14f, 1);
            Room(layout, "ROOM_SUBWAY_KIOSK_1", "ROOM_SUBWAY_KIOSK", 1, 66f, 1);
            Room(layout, "ROOM_SUBWAY_RESTROOM_1", "ROOM_SUBWAY_RESTROOM", 1, 82f, 1);
            Room(layout, "ROOM_SUBWAY_OFFICE_2", "ROOM_SUBWAY_OFFICE", 0, 70f, 2);
            for (var i = 0; i < 4; i++)
            {
                layout.Containers.Add(C($"C_SUB_LOCKER_{i + 1}", ContainerKind.Locker, 1, 34f + i * 2f, "LOOT_SUBWAY_KIOSK"));
            }

            layout.Containers.Add(C("C_SUB_BENCH_A", ContainerKind.Box, 0, 20f, "LOOT_SUBWAY_KIOSK"));
            layout.Containers.Add(C("C_SUB_BENCH_B", ContainerKind.Box, 0, 88f, "LOOT_SUBWAY_OFFICE"));
            layout.Exits.Add(new ExitDef { Kind = ExitKind.Main, Floor = 1, X = 3f });
            layout.Exits.Add(new ExitDef { Kind = ExitKind.Alternate, Floor = 0, X = 94f, StartsLocked = true });

            // 마지막 방송 (진실 단서), 방관했던 동창 (2막)
            var broadcast = new EncounterDef
            {
                Id = "LAST_BROADCAST", Floor = 1, RoomId = "ROOM_SUBWAY_OFFICE_1", X = 8f,
                FlagsNone = { "LAST_BROADCAST_HEARD" },
                PromptKey = "enc.LAST_BROADCAST.prompt",
                Choices =
                {
                    new EncounterChoiceDef { Id = "LISTEN", TextKey = "enc.LAST_BROADCAST.listen", SetFlags = { "LAST_BROADCAST_HEARD" }, AddClues = { "TRUTH_SUBWAY_BROADCAST" }, StressDelta = 4 },
                    new EncounterChoiceDef { Id = "LEAVE", TextKey = "enc.LAST_BROADCAST.leave", Removes = false },
                },
            };
            var bystander = new EncounterDef
            {
                Id = "BYSTANDER", Floor = 0, RoomId = "ROOM_SUBWAY_OFFICE_2", X = 9.2f, SpriteKey = "npc_bystander_sitting",
                FlagsAll = { "A2_STARTED" }, FlagsNone = { "BYSTANDER_MET" },
                PromptKey = "enc.BYSTANDER.prompt",
                DialogueNode = "ENC_BYSTANDER", SceneEventId = "EVT_A2_BYSTANDER",
                Choices =
                {
                    new EncounterChoiceDef { Id = "TAKE", TextKey = "enc.BYSTANDER.take", SetFlags = { "BYSTANDER_MET", "BYSTANDER_TAKEN" }, StressDelta = 10 },
                    new EncounterChoiceDef { Id = "SUPPLY", TextKey = "enc.BYSTANDER.supply", SetFlags = { "BYSTANDER_MET", "BYSTANDER_HELPED" }, StressDelta = 6 },
                    new EncounterChoiceDef { Id = "WALK_AWAY", TextKey = "enc.BYSTANDER.walk_away", SetFlags = { "BYSTANDER_MET", "BYSTANDER_LEFT" }, StressDelta = 6 },
                },
            };

            return new LocationDef
            {
                Layout = layout, DayRunSeconds = 420f, NightRunSeconds = 390f, DefaultLootTableId = "LOOT_SUBWAY_KIOSK",
                DayVariant = new LocationVariantDef
                {
                    Spawns =
                    {
                        Crowd("ENEMY_WANDERER", 0, "", 34f, 10, 6f),
                        Crowd("ENEMY_LISTENER", 0, "", 56f, 2, 1.5f),
                        Crowd("ENEMY_WANDERER", 1, "", 70f, 3, 2f),
                        Crowd("ENEMY_WANDERER", 1, "ROOM_SUBWAY_RESTROOM_1", 4f, 2, 1f),
                        Walk("ENEMY_WANDERER", 1, "", 26f, 1, 0f),
                    },
                    ForceableRooms = { "ROOM_SUBWAY_OFFICE_1" },
                    Encounters = { broadcast, bystander },
                },
                NightVariant = new LocationVariantDef
                {
                    Spawns =
                    {
                        Walk("ENEMY_WANDERER", 0, "", 30f, 4, 10f),
                        Walk("ENEMY_WATCHER", 0, "", 60f, 1, 0f),
                        Walk("ENEMY_WATCHER", 1, "", 40f, 1, 0f),
                        Walk("ENEMY_WANDERER", 1, "", 76f, 3, 8f),
                    },
                    ForceableRooms = { "ROOM_SUBWAY_OFFICE_1" },
                    Encounters = { broadcast },
                },
            };
        }

        // ── 마트·물류창고 (문서 6.3.8): 1층 매장 푸드코트 휴면 군집 사이 대량 물자, 2층 창고·사무실. 어른 무리·주동자 ──

        private static LocationDef Mart()
        {
            var layout = new BuildingLayoutDef { Floors = 2, FloorHeight = 4f, CorridorLength = 110f };
            layout.RoomTemplates.Add(new RoomTemplateDef
            {
                Id = "ROOM_MART_WAREHOUSE", Width = 20f, DefaultLootTableId = "LOOT_MART_WAREHOUSE",
                DoorXs = new List<float> { 1.2f, 18.8f },
                Containers =
                {
                    C("PALLET_1", ContainerKind.Box, 0, 3.8f), C("PALLET_2", ContainerKind.Box, 0, 6.6f), C("PALLET_3", ContainerKind.Box, 0, 9.4f),
                    C("PALLET_4", ContainerKind.Box, 0, 12.2f), C("COLD_STORE", ContainerKind.Fridge, 0, 15f, "LOOT_STORE_FRIDGE"), C("TOOL_RACK", ContainerKind.Cabinet, 0, 17.4f, "LOOT_SUBWAY_OFFICE"),
                },
            });
            layout.RoomTemplates.Add(new RoomTemplateDef
            {
                Id = "ROOM_MART_OFFICE", Width = 12f, DefaultLootTableId = "LOOT_STORE_OFFICE",
                DoorXs = new List<float> { 1.2f },
                Windows = new List<WindowDef> { new WindowDef(3f, 3f) },
                Containers = { C("OFFICE_DESK", ContainerKind.Desk, 0, 4f), C("OFFICE_SAFE", ContainerKind.Cabinet, 0, 7f), C("OFFICE_LOCKER", ContainerKind.Locker, 0, 10f) },
            });

            Stairs(layout, 104f);
            Room(layout, "ROOM_MART_OFFICE_1", "ROOM_MART_OFFICE", 0, 90f, 1);
            Room(layout, "ROOM_MART_WAREHOUSE_1", "ROOM_MART_WAREHOUSE", 1, 20f, 1);
            Room(layout, "ROOM_MART_WAREHOUSE_2", "ROOM_MART_WAREHOUSE", 1, 50f, 2);
            Room(layout, "ROOM_MART_OFFICE_2", "ROOM_MART_OFFICE", 1, 80f, 2);
            for (var i = 0; i < 10; i++)
            {
                layout.Containers.Add(C($"C_MART_SHELF_{i + 1}", ContainerKind.Box, 0, 8f + i * 3.2f, i % 3 == 0 ? "LOOT_MART_SHELF" : "LOOT_STORE_SHELF"));
            }

            layout.Containers.Add(C("C_MART_FOODCOURT_A", ContainerKind.Fridge, 0, 52f, "LOOT_STORE_FRIDGE"));
            layout.Containers.Add(C("C_MART_FOODCOURT_B", ContainerKind.Box, 0, 60f, "LOOT_MART_SHELF"));
            layout.Containers.Add(C("C_MART_CHECKOUT", ContainerKind.Desk, 0, 74f, "LOOT_STORE_COUNTER"));
            layout.Exits.Add(new ExitDef { Kind = ExitKind.Main, Floor = 0, X = 3f });
            layout.Exits.Add(new ExitDef { Kind = ExitKind.Alternate, Floor = 1, X = 108f, StartsLocked = true });

            // 어른 무리의 대표 (협상), 주동자 (직면)
            var group = new EncounterDef
            {
                Id = "MART_GROUP", Floor = 0, RoomId = "ROOM_MART_OFFICE_1", X = 6f, SpriteKey = "npc_mart_adults_idle",
                FlagsAll = { "A3_STARTED" }, FlagsNone = { "MART_GROUP_MET" },
                PromptKey = "enc.MART_GROUP.prompt",
                Choices =
                {
                    new EncounterChoiceDef { Id = "TALK", TextKey = "enc.MART_GROUP.talk", SetFlags = { "MART_GROUP_MET" } },
                    new EncounterChoiceDef { Id = "BACK_OFF", TextKey = "enc.MART_GROUP.back_off", Removes = false },
                },
            };
            var ringleader = new EncounterDef
            {
                Id = "RINGLEADER", Floor = 1, RoomId = "ROOM_MART_OFFICE_2", X = 8.5f, SpriteKey = "npc_ringleader_sitting",
                FlagsAll = { "MART_GROUP_MET" }, FlagsNone = { "RINGLEADER_MET" },
                PromptKey = "enc.RINGLEADER.prompt",
                DialogueNode = "ENC_RINGLEADER", SceneEventId = "EVT_A3_RINGLEADER", PlayEventInField = true,
                Choices =
                {
                    new EncounterChoiceDef { Id = "FACE", TextKey = "enc.RINGLEADER.face", SetFlags = { "RINGLEADER_MET" }, StressDelta = 12 },
                    new EncounterChoiceDef { Id = "LEAVE", TextKey = "enc.RINGLEADER.leave", Removes = false, StressDelta = 6 },
                },
            };

            return new LocationDef
            {
                Layout = layout, DayRunSeconds = 480f, NightRunSeconds = 420f, DefaultLootTableId = "LOOT_MART_SHELF",
                DayVariant = new LocationVariantDef
                {
                    Spawns =
                    {
                        Crowd("ENEMY_WANDERER", 0, "", 56f, 9, 5f),
                        Crowd("ENEMY_LISTENER", 0, "", 64f, 2, 1.5f),
                        Crowd("ENEMY_WANDERER", 1, "ROOM_MART_WAREHOUSE_2", 9f, 3, 2f),
                        Walk("ENEMY_WANDERER", 0, "", 24f, 1, 0f),
                        Walk("ENEMY_WANDERER", 1, "", 66f, 1, 0f),
                    },
                    LockedRooms = { "ROOM_MART_WAREHOUSE_1" },
                    Encounters = { group, ringleader },
                },
                NightVariant = new LocationVariantDef
                {
                    Spawns =
                    {
                        Walk("ENEMY_WANDERER", 0, "", 40f, 4, 12f),
                        Walk("ENEMY_WATCHER", 0, "", 70f, 1, 0f),
                        Walk("ENEMY_WANDERER", 1, "", 40f, 3, 10f),
                        Walk("ENEMY_LISTENER", 1, "", 76f, 2, 4f),
                    },
                    ForceableRooms = { "ROOM_MART_WAREHOUSE_1" },
                },
            };
        }

        // ── 파출소 (문서 6.3.8, 밤 선호): 무기고·사건 기록. 총기는 두지 않는다 ──

        private static LocationDef Police()
        {
            var layout = new BuildingLayoutDef { Floors = 2, FloorHeight = 4f, CorridorLength = 52f };
            layout.RoomTemplates.Add(new RoomTemplateDef
            {
                Id = "ROOM_POLICE_OFFICE", Width = 16f, DefaultLootTableId = "LOOT_POLICE_OFFICE",
                DoorXs = new List<float> { 1.2f },
                Windows = new List<WindowDef> { new WindowDef(3f, 3.4f), new WindowDef(9f, 3.4f) },
                Containers = { C("DESK_A", ContainerKind.Desk, 0, 4f), C("DESK_B", ContainerKind.Desk, 0, 7f), C("FILES", ContainerKind.Cabinet, 0, 11f), C("LOCKER", ContainerKind.Locker, 0, 14f) },
            });
            layout.RoomTemplates.Add(new RoomTemplateDef
            {
                Id = "ROOM_POLICE_ARMORY", Width = 10f, DefaultLootTableId = "LOOT_POLICE_ARMORY",
                DoorXs = new List<float> { 1.2f },
                Containers = { C("RACK_A", ContainerKind.Locker, 0, 3.6f), C("RACK_B", ContainerKind.Locker, 0, 6f), C("CRATE", ContainerKind.Box, 0, 8.4f) },
            });
            layout.RoomTemplates.Add(new RoomTemplateDef
            {
                Id = "ROOM_POLICE_CELL", Width = 10f, DefaultLootTableId = "LOOT_HOUSE_ROOM",
                DoorXs = new List<float> { 1.2f },
                Containers = { C("BUNK", ContainerKind.Box, 0, 5f) },
            });

            Stairs(layout, 4f);
            Room(layout, "ROOM_POLICE_OFFICE_1", "ROOM_POLICE_OFFICE", 0, 16f, 1);
            Room(layout, "ROOM_POLICE_CELL_1", "ROOM_POLICE_CELL", 0, 36f, 1);
            Room(layout, "ROOM_POLICE_ARMORY_1", "ROOM_POLICE_ARMORY", 1, 14f, 1);
            Room(layout, "ROOM_POLICE_OFFICE_2", "ROOM_POLICE_OFFICE", 1, 30f, 2);
            layout.Containers.Add(C("C_POLICE_FRONT_DESK", ContainerKind.Desk, 0, 10f, "LOOT_POLICE_OFFICE"));
            layout.Exits.Add(new ExitDef { Kind = ExitKind.Main, Floor = 0, X = 49f });
            layout.Exits.Add(new ExitDef { Kind = ExitKind.Alternate, Floor = 1, X = 50f, StartsLocked = true });

            var report = new EncounterDef
            {
                Id = "POLICE_REPORT", Floor = 1, RoomId = "ROOM_POLICE_OFFICE_2", X = 8f,
                FlagsNone = { "POLICE_REPORT_TAKEN" },
                PromptKey = "enc.POLICE_REPORT.prompt",
                Choices =
                {
                    new EncounterChoiceDef { Id = "TAKE", TextKey = "enc.POLICE_REPORT.take", SetFlags = { "POLICE_REPORT_TAKEN" }, AddClues = { "TRUTH_POLICE_REPORT" } },
                    new EncounterChoiceDef { Id = "LEAVE", TextKey = "enc.POLICE_REPORT.leave", Removes = false },
                },
            };

            return new LocationDef
            {
                Layout = layout, DayRunSeconds = 330f, NightRunSeconds = 330f, DefaultLootTableId = "LOOT_POLICE_OFFICE",
                DayVariant = new LocationVariantDef
                {
                    Spawns =
                    {
                        Crowd("ENEMY_WANDERER", 0, "", 26f, 4, 2.5f),
                        Crowd("ENEMY_WANDERER", 1, "ROOM_POLICE_OFFICE_2", 8f, 3, 2f),
                        Crowd("ENEMY_LISTENER", 0, "ROOM_POLICE_CELL_1", 5f, 1, 0f),
                        Walk("ENEMY_WANDERER", 1, "", 40f, 1, 0f),
                    },
                    LockedRooms = { "ROOM_POLICE_ARMORY_1" },
                    Encounters = { report },
                },
                NightVariant = new LocationVariantDef
                {
                    Spawns = { Walk("ENEMY_WANDERER", 0, "", 28f, 3, 8f), Walk("ENEMY_WATCHER", 1, "", 36f, 1, 0f), Walk("ENEMY_WANDERER", 1, "", 20f, 2, 5f) },
                    ForceableRooms = { "ROOM_POLICE_ARMORY_1" },
                    Encounters = { report },
                },
            };
        }

        // ── 강변 정수장 연구동 (문서 7.2): 낮엔 거대한 휴면 군집, 밤에만 지하 기록실까지 ──

        private static LocationDef Waterworks()
        {
            var layout = new BuildingLayoutDef { Floors = 3, FloorHeight = 4f, CorridorLength = 100f };
            layout.RoomTemplates.Add(new RoomTemplateDef
            {
                Id = "ROOM_WATER_ARCHIVE", Width = 16f, DefaultLootTableId = "LOOT_WATER_LAB",
                DoorXs = new List<float> { 1.2f },
                Containers = { C("ARCHIVE_A", ContainerKind.Cabinet, 0, 4f), C("ARCHIVE_B", ContainerKind.Cabinet, 0, 7f), C("ARCHIVE_DESK", ContainerKind.Desk, 0, 10.5f), C("SAMPLE_FRIDGE", ContainerKind.Fridge, 0, 13.6f, "LOOT_PHARMACY_STOCK") },
            });
            layout.RoomTemplates.Add(new RoomTemplateDef
            {
                Id = "ROOM_WATER_LAB", Width = 16f, DefaultLootTableId = "LOOT_WATER_LAB",
                DoorXs = new List<float> { 1.2f, 14.8f },
                Windows = new List<WindowDef> { new WindowDef(3f, 3.4f), new WindowDef(9f, 3.4f) },
                Containers = { C("BENCH_A", ContainerKind.Desk, 0, 4f), C("BENCH_B", ContainerKind.Desk, 0, 7f), C("REAGENT", ContainerKind.Cabinet, 0, 10.4f), C("LOCKER", ContainerKind.Locker, 0, 13.2f) },
            });
            layout.RoomTemplates.Add(new RoomTemplateDef
            {
                Id = "ROOM_WATER_CONTROL", Width = 14f, DefaultLootTableId = "LOOT_SUBWAY_OFFICE",
                DoorXs = new List<float> { 1.2f },
                Windows = new List<WindowDef> { new WindowDef(3f, 3.4f) },
                Containers = { C("CONSOLE", ContainerKind.Desk, 0, 5f), C("PARTS", ContainerKind.Box, 0, 8.4f), C("SUPPLY", ContainerKind.Cabinet, 0, 11.4f) },
            });

            Stairs(layout, 6f, collapsedFrom: 1);     // 왼쪽 계단은 2~3층 사이가 무너졌다
            Stairs(layout, 96f);
            Room(layout, "ROOM_WATER_ARCHIVE_1", "ROOM_WATER_ARCHIVE", 0, 40f, 1);
            Room(layout, "ROOM_WATER_CONTROL_1", "ROOM_WATER_CONTROL", 1, 20f, 1);
            Room(layout, "ROOM_WATER_LAB_1", "ROOM_WATER_LAB", 2, 20f, 1);
            Room(layout, "ROOM_WATER_LAB_2", "ROOM_WATER_LAB", 2, 56f, 2);
            for (var i = 0; i < 4; i++)
            {
                layout.Containers.Add(C($"C_WATER_TANK_{i + 1}", ContainerKind.Box, 1, 46f + i * 9f, "LOOT_SUBWAY_OFFICE"));
            }

            layout.Exits.Add(new ExitDef { Kind = ExitKind.Main, Floor = 1, X = 3f });
            layout.Exits.Add(new ExitDef { Kind = ExitKind.Alternate, Floor = 2, X = 98f, StartsLocked = true });

            var data = new EncounterDef
            {
                Id = "RESEARCH_DATA", Floor = 0, RoomId = "ROOM_WATER_ARCHIVE_1", X = 12f,
                FlagsNone = { "RESEARCH_DATA" },
                PromptKey = "enc.RESEARCH_DATA.prompt",
                Choices =
                {
                    new EncounterChoiceDef { Id = "TAKE", TextKey = "enc.RESEARCH_DATA.take", SetFlags = { "RESEARCH_DATA" }, AddClues = { "CURE_RESEARCH_DATA", "TRUTH_RESEARCH_ORIGIN" }, StressDelta = 6 },
                    new EncounterChoiceDef { Id = "LEAVE", TextKey = "enc.RESEARCH_DATA.leave", Removes = false },
                },
            };
            var memo = new EncounterDef
            {
                Id = "RESEARCH_MEMO", Floor = 2, RoomId = "ROOM_WATER_LAB_2", X = 8f,
                FlagsNone = { "RESEARCH_MEMO" },
                PromptKey = "enc.RESEARCH_MEMO.prompt",
                Choices =
                {
                    new EncounterChoiceDef { Id = "TAKE", TextKey = "enc.RESEARCH_MEMO.take", SetFlags = { "RESEARCH_MEMO" }, AddClues = { "CURE_RESEARCH_LOG", "TRUTH_RESEARCH_MEMO" } },
                    new EncounterChoiceDef { Id = "LEAVE", TextKey = "enc.RESEARCH_MEMO.leave", Removes = false },
                },
            };

            return new LocationDef
            {
                Layout = layout, DayRunSeconds = 480f, NightRunSeconds = 450f, DefaultLootTableId = "LOOT_WATER_LAB",
                DayVariant = new LocationVariantDef
                {
                    Spawns =
                    {
                        Crowd("ENEMY_WANDERER", 1, "", 50f, 12, 7f),
                        Crowd("ENEMY_WANDERER", 0, "", 30f, 8, 5f),
                        Crowd("ENEMY_LISTENER", 0, "", 50f, 3, 2f),
                        Crowd("ENEMY_WANDERER", 2, "", 40f, 5, 4f),
                        Walk("ENEMY_WATCHER", 2, "", 78f, 1, 0f),
                    },
                    LockedRooms = { "ROOM_WATER_ARCHIVE_1" },
                    Encounters = { memo },
                },
                NightVariant = new LocationVariantDef
                {
                    Spawns =
                    {
                        Walk("ENEMY_WANDERER", 1, "", 50f, 4, 14f),
                        Walk("ENEMY_WATCHER", 1, "", 80f, 1, 0f),
                        Walk("ENEMY_WANDERER", 0, "", 24f, 2, 6f),
                        Walk("ENEMY_LISTENER", 0, "", 62f, 2, 6f),
                        Walk("ENEMY_WANDERER", 2, "", 40f, 3, 10f),
                    },
                    ForceableRooms = { "ROOM_WATER_ARCHIVE_1" },
                    Encounters = { data, memo },
                },
            };
        }

        // ── 약국·의원 (2층): 낮은 대기실 휴면 군집, 밤에만 약품고 (문서 6.3.8) ──

        private static LocationDef Pharmacy()
        {
            var layout = new BuildingLayoutDef { Floors = 2, FloorHeight = 4f, CorridorLength = 60f };
            layout.RoomTemplates.Add(new RoomTemplateDef
            {
                Id = "ROOM_PH_DISPENSARY", Width = 12f, DefaultLootTableId = "LOOT_PHARMACY_SHELF",
                DoorXs = new List<float> { 1.2f },
                Containers = { C("DISP_SHELF", ContainerKind.Cabinet, 0, 4f), C("DISP_DESK", ContainerKind.Desk, 0, 7f), C("DISP_FRIDGE", ContainerKind.Fridge, 0, 10f, "LOOT_PHARMACY_STOCK") },
            });
            layout.RoomTemplates.Add(new RoomTemplateDef
            {
                Id = "ROOM_PH_CLINIC", Width = 12f, DefaultLootTableId = "LOOT_PHARMACY_CLINIC",
                DoorXs = new List<float> { 1.2f },
                Windows = new List<WindowDef> { new WindowDef(4f, 3f) },
                Containers = { C("CLINIC_DESK", ContainerKind.Desk, 0, 4f), C("CLINIC_CABINET", ContainerKind.Cabinet, 0, 7.5f), C("CLINIC_LOCKER", ContainerKind.Locker, 0, 10.4f) },
            });
            layout.RoomTemplates.Add(new RoomTemplateDef
            {
                Id = "ROOM_PH_STOCKROOM", Width = 10f, DefaultLootTableId = "LOOT_PHARMACY_STOCK",
                DoorXs = new List<float> { 1.2f },
                Containers = { C("STOCK_A", ContainerKind.Cabinet, 0, 3.6f), C("STOCK_B", ContainerKind.Cabinet, 0, 6f), C("STOCK_C", ContainerKind.Box, 0, 8.4f) },
            });

            Stairs(layout, 5f);
            Room(layout, "ROOM_PH_DISPENSARY_1", "ROOM_PH_DISPENSARY", 0, 40f, 1);
            Room(layout, "ROOM_PH_CLINIC_1", "ROOM_PH_CLINIC", 1, 30f, 1);
            Room(layout, "ROOM_PH_CLINIC_2", "ROOM_PH_CLINIC", 1, 44f, 2);
            Room(layout, "ROOM_PH_STOCKROOM_1", "ROOM_PH_STOCKROOM", 1, 10f, 1);

            for (var i = 0; i < 5; i++)
            {
                layout.Containers.Add(C($"C_PH_SHELF_{i + 1}", ContainerKind.Cabinet, 0, 14f + i * 3f, "LOOT_PHARMACY_SHELF"));
            }

            layout.Containers.Add(C("C_PH_COUNTER", ContainerKind.Desk, 0, 34f, "LOOT_PHARMACY_SHELF"));
            layout.Containers.Add(C("C_PH_WAITING_DESK", ContainerKind.Desk, 1, 25f, "LOOT_PHARMACY_CLINIC"));
            layout.Exits.Add(new ExitDef { Kind = ExitKind.Main, Floor = 0, X = 57f });
            layout.Exits.Add(new ExitDef { Kind = ExitKind.Alternate, Floor = 1, X = 58f, StartsLocked = true });

            var doctorNote = new EncounterDef
            {
                Id = "DOCTOR_NOTE", Floor = 1, RoomId = "ROOM_PH_CLINIC_2", X = 6f,
                FlagsNone = { "DOCTOR_NOTE_TAKEN" },
                PromptKey = "enc.DOCTOR_NOTE.prompt",
                Choices =
                {
                    new EncounterChoiceDef { Id = "TAKE", TextKey = "enc.DOCTOR_NOTE.take", SetFlags = { "DOCTOR_NOTE_TAKEN" }, AddClues = { "CLUE_DOCTOR_NOTE_01" } },
                    new EncounterChoiceDef { Id = "LEAVE", TextKey = "enc.DOCTOR_NOTE.leave", Removes = false },
                },
            };

            // M7: 수혈 (D16). 선생님과 혈액형이 맞지 않으면 밤에만 나타난다.
            var bloodFridge = new EncounterDef
            {
                Id = "BLOOD_FRIDGE", Floor = 1, RoomId = "ROOM_PH_CLINIC_1", X = 2.6f, SpriteKey = "prop_fridge",
                FlagsAll = { "NEED_BLOOD_PACK" },
                FlagsNone = { "BLOOD_PACK", "TEACHER_CURED" },
                PromptKey = "enc.BLOOD_FRIDGE.prompt",
                Choices =
                {
                    new EncounterChoiceDef { Id = "TAKE", TextKey = "enc.BLOOD_FRIDGE.take", SetFlags = { "BLOOD_PACK" } },
                    new EncounterChoiceDef { Id = "LEAVE", TextKey = "enc.BLOOD_FRIDGE.leave", Removes = false },
                },
            };

            return new LocationDef
            {
                Layout = layout, DayRunSeconds = 420f, NightRunSeconds = 390f, DefaultLootTableId = "LOOT_PHARMACY_SHELF",
                DayVariant = new LocationVariantDef
                {
                    Spawns =
                    {
                        Crowd("ENEMY_WANDERER", 1, "", 20f, 7, 4f),
                        Crowd("ENEMY_LISTENER", 1, "", 38f, 2, 1f),
                        Crowd("ENEMY_WANDERER", 0, "", 24f, 2, 1.5f),
                        Walk("ENEMY_WANDERER", 0, "", 48f, 1, 0f),
                    },
                    LockedRooms = { "ROOM_PH_STOCKROOM_1" },
                    ForceableRooms = { "ROOM_PH_DISPENSARY_1" },
                    Encounters = { doctorNote },
                },
                NightVariant = new LocationVariantDef
                {
                    Spawns =
                    {
                        Walk("ENEMY_WANDERER", 1, "", 22f, 4, 8f),
                        Walk("ENEMY_WATCHER", 1, "", 40f, 1, 0f),
                        Walk("ENEMY_WANDERER", 0, "", 20f, 2, 6f),
                        Walk("ENEMY_WATCHER", 0, "", 44f, 1, 0f),
                    },
                    ForceableRooms = { "ROOM_PH_DISPENSARY_1" },
                    Encounters = { doctorNote, bloodFridge },
                },
            };
        }
    }
}
