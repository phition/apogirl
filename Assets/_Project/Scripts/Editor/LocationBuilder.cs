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
    /// 지역 목록: 튜토리얼(체육관 창고 → 본관 창고), 학교 교실동, 편의점, 약국·의원.
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
                Save("LOC_SCHOOL_INTERIOR", School()),
                Save("LOC_STORE", Store()),
                Save("LOC_PHARMACY", Pharmacy()),
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

        private static ContainerDef C(string id, ContainerKind kind, int floor, float x, string loot = "")
        {
            var seconds = kind == ContainerKind.Locker ? 3f : kind == ContainerKind.Desk ? 2f : kind == ContainerKind.Cabinet ? 3.5f : kind == ContainerKind.Fridge ? 4f : 2.5f;
            return new ContainerDef { Id = id, Kind = kind, Floor = floor, X = x, SearchSeconds = seconds, LootTableId = loot };
        }

        private static SpawnDef Crowd(string enemy, int floor, string room, float x, int count, float spread)
        {
            return new SpawnDef { EnemyId = enemy, Floor = floor, RoomId = room, X = x, Count = count, Spread = spread, Dormant = true };
        }

        private static SpawnDef Walk(string enemy, int floor, string room, float x, int count, float spread)
        {
            return new SpawnDef { EnemyId = enemy, Floor = floor, RoomId = room, X = x, Count = count, Spread = spread, Dormant = false };
        }

        private static void Stairs(BuildingLayoutDef layout, float x)
        {
            for (var f = 0; f < layout.Floors - 1; f++)
            {
                layout.Connectors.Add(new ConnectorDef { Kind = ConnectorKind.Stairs, Floor = f, X = x, TargetFloor = f + 1 });
                layout.Connectors.Add(new ConnectorDef { Kind = ConnectorKind.Stairs, Floor = f + 1, X = x - 1.4f, TargetFloor = f });
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
                    C("BALL_CART", ContainerKind.Box, 0, 7f),
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
                DayVariant = new LocationVariantDef { Spawns = { Crowd("ENEMY_WANDERER", 0, "", 30f, 2, 1.2f) } },
                NightVariant = new LocationVariantDef { Spawns = { Crowd("ENEMY_WANDERER", 0, "", 30f, 2, 1.2f) } },
            };
        }

        // ── 학교 교실동 (M2b 배치 + M4 도현 조우) ──

        private static LocationDef School()
        {
            var layout = new BuildingLayoutDef { Floors = 4, FloorHeight = 4f, CorridorLength = 124f };
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
                Containers = { C("SINK", ContainerKind.Cabinet, 0, 4.2f), C("SUPPLY", ContainerKind.Box, 0, 6.6f) },
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

            Stairs(layout, 3f);
            Stairs(layout, 121f);

            void R(string template, int floor, float spanX, int number) =>
                Room(layout, $"ROOM_{floor + 1}F_{template}_{number}", template, floor, spanX, number);

            R("ROOM_OFFICE", 0, 6f, 0);
            R("ROOM_CLASSROOM", 0, 24f, 1);
            R("ROOM_CLASSROOM", 0, 38f, 2);
            R("ROOM_CLASSROOM", 0, 60f, 3);
            R("ROOM_CLASSROOM", 0, 74f, 4);
            R("ROOM_CLASSROOM", 0, 88f, 5);
            R("ROOM_RESTROOM", 0, 102f, 0);
            for (var f = 1; f < layout.Floors; f++)
            {
                for (var i = 0; i < 7; i++) R("ROOM_CLASSROOM", f, 6f + 14f * i, i + 1);
                R("ROOM_RESTROOM", f, 104f, 0);
            }

            for (var f = 0; f < layout.Floors; f++)
            {
                layout.Containers.Add(C($"C_{f + 1}F_HALL_LOCKER_A", ContainerKind.Locker, f, 114.5f, "LOOT_SCHOOL_LOCKER"));
                layout.Containers.Add(C($"C_{f + 1}F_HALL_LOCKER_B", ContainerKind.Locker, f, f == 0 ? 53.5f : 117.5f, "LOOT_SCHOOL_LOCKER"));
            }

            layout.Exits.Add(new ExitDef { Kind = ExitKind.Main, Floor = 0, X = 56f });
            layout.Exits.Add(new ExitDef { Kind = ExitKind.Alternate, Floor = 3, X = 123f, StartsLocked = true });

            // 도현 (첫 NPC, M4 결정): 밤의 노크 소리를 들은 뒤(DH_HEARD) 3층 3-5 교실에 있다
            EncounterDef Dohyeon() => new EncounterDef
            {
                Id = "DOHYEON", Floor = 2, RoomId = "ROOM_3F_ROOM_CLASSROOM_5", X = 9.5f,
                FlagsAll = { "DH_HEARD" }, FlagsNone = { "DH_RESCUED", "DH_LEFT", "DH_HELPED" },
                PromptKey = "enc.DOHYEON.prompt",
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
                    Crowd("ENEMY_WANDERER", 3, "", 30f, 5, 2.5f),
                    Crowd("ENEMY_WANDERER", 1, "ROOM_2F_ROOM_CLASSROOM_4", 8f, 3, 2f),
                    Crowd("ENEMY_WANDERER", 2, "ROOM_3F_ROOM_CLASSROOM_2", 7f, 2, 1.5f),
                    Crowd("ENEMY_WANDERER", 3, "ROOM_4F_ROOM_CLASSROOM_6", 8f, 3, 2f),
                    Crowd("ENEMY_LISTENER", 3, "ROOM_4F_ROOM_RESTROOM_0", 5f, 2, 1f),
                    Walk("ENEMY_WANDERER", 0, "", 95f, 1, 0f),
                    Walk("ENEMY_LISTENER", 2, "", 30f, 1, 0f),
                    Walk("ENEMY_WANDERER", 3, "", 78f, 1, 0f),
                },
                Encounters = { Dohyeon(), Notice() },
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
                    Walk("ENEMY_WANDERER", 3, "", 36f, 3, 8f),
                    Walk("ENEMY_WANDERER", 3, "", 87f, 2, 7f),
                    Walk("ENEMY_WANDERER", 0, "ROOM_1F_ROOM_OFFICE_0", 9f, 2, 4f),
                    Walk("ENEMY_WANDERER", 1, "ROOM_2F_ROOM_CLASSROOM_2", 7f, 1, 0f),
                    Walk("ENEMY_LISTENER", 2, "ROOM_3F_ROOM_CLASSROOM_5", 3f, 1, 0f),
                    Walk("ENEMY_WANDERER", 3, "ROOM_4F_ROOM_CLASSROOM_3", 7f, 2, 3f),
                },
                Encounters = { Dohyeon(), Notice() },
            };

            return new LocationDef
            {
                Layout = layout, DayRunSeconds = 360f, NightRunSeconds = 360f, DefaultLootTableId = "LOOT_SCHOOL_GENERAL",
                DayVariant = day, NightVariant = night,
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
                },
                NightVariant = new LocationVariantDef
                {
                    Spawns = { Walk("ENEMY_WANDERER", 0, "", 12f, 2, 5f), Walk("ENEMY_WANDERER", 0, "", 28f, 2, 5f), Walk("ENEMY_LISTENER", 0, "", 36f, 1, 0f) },
                    ForceableRooms = { "ROOM_STORE_OFFICE_1" },
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
                    Encounters = { doctorNote },
                },
            };
        }
    }
}
