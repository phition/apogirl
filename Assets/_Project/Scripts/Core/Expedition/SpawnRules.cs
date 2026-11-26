using System;
using System.Collections.Generic;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Random;

namespace ApoGirl.Core.Expedition
{
    /// <summary>
    /// 탐험마다 다른 적 배치 (사용자 요청 2026-11-26). 지역 정의의 배치(SpawnDef)는 '몇 층에 어떤 무리가 몇 마리'까지만 정하고,
    /// 자리는 출정 시드로 다시 고른다: 복도 무리는 그 층 복도 어딘가(출구·계단·만남 자리는 피해서),
    /// 방 안 무리는 같은 층의 다른 방으로 옮겨 가기도 한다. 수와 종류는 그대로라 난이도는 같다.
    /// 적대적 생존자는 한 층에 한 명만 선다.
    /// </summary>
    public static class SpawnRules
    {
        private const int ArrangeSalt = 0x51A7E;
        private const int HostileSalt = 0x5A1;

        /// <summary>이번 출정의 좀비 배치 (정의의 사본, 같은 시드면 같은 배치).</summary>
        public static List<SpawnDef> Arrange(LocationDef location, LocationVariantDef variant, int seed, BalanceConfig balance)
        {
            var result = new List<SpawnDef>();
            if (variant == null)
            {
                return result;
            }

            var layout = location?.Layout;
            var shuffle = layout != null && balance != null && balance.SpawnShuffle;
            var random = new SeededRandom(SeededRandom.CreateState(seed ^ ArrangeSalt));
            var placed = new List<SpawnDef>();
            foreach (var source in variant.Spawns)
            {
                var spawn = Copy(source);
                if (shuffle && !source.Fixed)
                {
                    if (string.IsNullOrEmpty(spawn.RoomId))
                    {
                        PlaceInCorridor(spawn, layout, variant, balance, random, placed);
                    }
                    else
                    {
                        PlaceInRoom(spawn, layout, variant, balance, random);
                    }
                }

                placed.Add(spawn);
                result.Add(spawn);
            }

            return result;
        }

        private static void PlaceInCorridor(SpawnDef spawn, BuildingLayoutDef layout, LocationVariantDef variant, BalanceConfig balance, SeededRandom random, List<SpawnDef> placed)
        {
            var length = layout.CorridorLengthOf(spawn.Floor);
            var margin = 2.5f + spawn.Spread;
            if (length <= margin * 2f + 1f)
            {
                return;
            }

            // 피할 자리: 출구(들어서는 곳), 계단·깊은 문(올라서는 곳), 복도의 만남(대화 중에 덮치지 않게)
            var avoid = new List<(float X, float Radius)>();
            foreach (var exit in layout.Exits)
            {
                if (exit.Floor == spawn.Floor)
                {
                    avoid.Add((exit.X, balance.SpawnSafeDistance + spawn.Spread));
                }
            }

            foreach (var connector in layout.Connectors)
            {
                if (connector.Floor == spawn.Floor)
                {
                    avoid.Add((connector.X, balance.SpawnStairsDistance + spawn.Spread));
                }

                if (connector.TargetFloor == spawn.Floor)
                {
                    avoid.Add((connector.ArrivalX, balance.SpawnStairsDistance + spawn.Spread));
                }
            }

            foreach (var encounter in variant.Encounters)
            {
                if (encounter.Floor == spawn.Floor && string.IsNullOrEmpty(encounter.RoomId))
                {
                    avoid.Add((encounter.X, balance.SpawnStairsDistance + spawn.Spread));
                }
            }

            foreach (var other in placed)
            {
                if (other.Floor == spawn.Floor && string.IsNullOrEmpty(other.RoomId))
                {
                    avoid.Add((other.X, other.Spread + spawn.Spread + 2f));
                }
            }

            for (var attempt = 0; attempt < 16; attempt++)
            {
                var x = margin + (float)random.NextDouble() * (length - margin * 2f);
                var free = true;
                foreach (var (ax, radius) in avoid)
                {
                    if (Math.Abs(x - ax) < radius)
                    {
                        free = false;
                        break;
                    }
                }

                if (free)
                {
                    spawn.X = (float)Math.Round(x, 1);
                    return;
                }
            }

            // 빈 자리를 못 찾으면 정의의 자리 그대로
        }

        private static void PlaceInRoom(SpawnDef spawn, BuildingLayoutDef layout, LocationVariantDef variant, BalanceConfig balance, SeededRandom random)
        {
            var home = layout.Doors.Find(d => d.RoomId == spawn.RoomId);
            if (home == null || home.Seal != DoorSeal.None)
            {
                return;   // 깊은 곳 특별한 방(빗장 안의 감염자 등)은 그 방 그대로
            }

            var door = home;
            if (random.NextDouble() < balance.SpawnRoomMoveChance)
            {
                var candidates = new List<DoorDef>();
                foreach (var other in layout.Doors)
                {
                    if (other.Floor == home.Floor && other.Seal == DoorSeal.None && layout.FindTemplate(other.TemplateId) != null
                        && !variant.LockedRooms.Contains(other.RoomId) && !variant.Encounters.Exists(e => e.RoomId == other.RoomId))
                    {
                        candidates.Add(other);
                    }
                }

                if (candidates.Count > 0)
                {
                    door = candidates[random.Range(0, candidates.Count - 1)];
                }
            }

            var template = layout.FindTemplate(door.TemplateId);
            if (template == null)
            {
                return;
            }

            spawn.RoomId = door.RoomId;
            var from = Math.Min(3f + spawn.Spread, template.Width * 0.5f);
            var to = Math.Max(from, template.Width - 2f - spawn.Spread);
            spawn.X = (float)Math.Round(from + random.NextDouble() * (to - from), 1);
        }

        /// <summary>생존자가 설 수 있는 층 수 (겉 층, 옥상 제외). 한 층에 한 명이라 이 수가 곧 최대 인원.</summary>
        public static int HostileFloors(LocationDef location)
        {
            var layout = location?.Layout;
            if (layout == null)
            {
                return int.MaxValue;
            }

            var floors = 0;
            for (var f = 0; f < layout.Floors; f++)
            {
                if (!layout.IsRooftop(f))
                {
                    floors++;
                }
            }

            return Math.Max(1, floors);
        }

        /// <summary>
        /// 적대적 생존자가 설 자리: 한 층에 한 명. 지정 후보(HostileSpots)가 그 층에 있으면 그중 하나, 없으면 출구에서 먼 복도.
        /// </summary>
        public static List<SpawnDef> HostilePlaces(LocationDef location, LocationVariantDef variant, int count, int seed)
        {
            var result = new List<SpawnDef>();
            var layout = location?.Layout;
            if (layout == null || count <= 0)
            {
                return result;
            }

            var random = new SeededRandom(SeededRandom.CreateState(seed ^ HostileSalt));
            var floors = new List<int>();
            for (var f = 0; f < layout.Floors; f++)   // 깊은 곳·옥상에는 두지 않는다
            {
                if (!layout.IsRooftop(f))
                {
                    floors.Add(f);
                }
            }

            for (var i = floors.Count - 1; i > 0; i--)
            {
                var j = random.Range(0, i);
                (floors[i], floors[j]) = (floors[j], floors[i]);
            }

            var start = layout.Exits.Find(e => e.Kind == ExitKind.Main);
            for (var i = 0; i < count && i < floors.Count; i++)
            {
                var floor = floors[i];
                var spots = variant?.HostileSpots.FindAll(s => s.Floor == floor);
                if (spots != null && spots.Count > 0)
                {
                    var spot = spots[random.Range(0, spots.Count - 1)];
                    result.Add(new SpawnDef { Floor = floor, RoomId = spot.RoomId, X = spot.X, Count = 1, Spread = 0f });
                    continue;
                }

                var length = layout.CorridorLengthOf(floor);
                var startNearLeft = start == null || start.Floor != floor || start.X < length / 2f;
                var x = startNearLeft
                    ? length * (0.6f + 0.3f * (float)random.NextDouble())
                    : length * (0.1f + 0.3f * (float)random.NextDouble());
                result.Add(new SpawnDef { Floor = floor, X = x, Count = 1, Spread = 0f });
            }

            return result;
        }

        private static SpawnDef Copy(SpawnDef source)
        {
            return new SpawnDef
            {
                EnemyId = source.EnemyId, Floor = source.Floor, RoomId = source.RoomId, X = source.X,
                Count = source.Count, Spread = source.Spread, Dormant = source.Dormant, Fixed = source.Fixed,
            };
        }
    }
}
