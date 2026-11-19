using System.Collections.Generic;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Random;
using ApoGirl.Core.State;

namespace ApoGirl.Exploration.Model
{
    /// <summary>
    /// 방·복도 컨테이너의 전리품 결정. 같은 출정 시드 + 같은 방 → 같은 결과,
    /// 같은 템플릿이라도 방이 다르면 다른 결과.
    /// </summary>
    public static class RoomLoot
    {
        public static int RoomSeed(int runSeed, int doorIndex)
        {
            unchecked
            {
                return runSeed * 486187739 + (doorIndex + 1) * 104729;
            }
        }

        /// <summary>테이블 우선순위: 컨테이너 지정 > 문(방) 지정 > 템플릿 기본 > 지역 기본.</summary>
        public static string ResolveTable(ContainerDef container, DoorDef door, RoomTemplateDef template, LocationDef location)
        {
            if (!string.IsNullOrEmpty(container.LootTableId)) return container.LootTableId;
            if (door != null && !string.IsNullOrEmpty(door.LootTableId)) return door.LootTableId;
            if (template != null && !string.IsNullOrEmpty(template.DefaultLootTableId)) return template.DefaultLootTableId;
            return location.DefaultLootTableId;
        }

        /// <summary>방 하나의 컨테이너별 내용물 (템플릿 컨테이너 순서).</summary>
        public static List<List<ItemStack>> RollRoom(IDefinitionDatabase db, LocationDef location, int doorIndex, int runSeed)
        {
            var door = location.Layout.Doors[doorIndex];
            var template = location.Layout.FindTemplate(door.TemplateId);
            var result = new List<List<ItemStack>>();
            if (template == null)
            {
                return result;
            }

            var roomSeed = RoomSeed(runSeed, doorIndex);
            for (var i = 0; i < template.Containers.Count; i++)
            {
                var table = ResolveTable(template.Containers[i], door, template, location);
                result.Add(Roll(db, table, ExplorationRules.ContainerSeed(roomSeed, i)));
            }

            return result;
        }

        /// <summary>복도 컨테이너 내용물.</summary>
        public static List<ItemStack> RollCorridorContainer(IDefinitionDatabase db, LocationDef location, int containerIndex, int runSeed)
        {
            var container = location.Layout.Containers[containerIndex];
            return Roll(db, ResolveTable(container, null, null, location), ExplorationRules.ContainerSeed(runSeed, containerIndex));
        }

        private static List<ItemStack> Roll(IDefinitionDatabase db, string tableId, int seed)
        {
            return db.TryGetLootTable(tableId, out var table)
                ? LootRoller.Roll(table, new SeededRandom(SeededRandom.CreateState(seed)))
                : new List<ItemStack>();
        }
    }
}
