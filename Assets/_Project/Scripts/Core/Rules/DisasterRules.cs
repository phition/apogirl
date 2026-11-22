using System;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Random;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Rules
{
    /// <summary>
    /// 계절 재해 (확장 6): 같은 지역이 계절마다 다른 미로가 된다.
    /// 재해 기간(BalanceConfig.Disasters)에 그 날씨이면 재해다. 지도는 깊은 곳이 있는 지역만 바뀐다.
    /// - 장마(MONSOON, 비): 깊은 곳 맨 아래층이 물에 잠긴다(걸음 느림·소리 큼). 그 층 일방 출구는 물에 막히고, 대신 평소 잠긴 탈출구가 물에 밀려 열린다.
    /// - 폭설(SNOWSTORM, 한파): 1층 깊은 문(양쪽)이 눈에 막혀 두 번째 깊은 문(2층)으로 들어가야 하고, 1층 보조 탈출구는 눈에 파묻힌다.
    /// </summary>
    public static class DisasterRules
    {
        public const string Monsoon = "MONSOON";
        public const string Snowstorm = "SNOWSTORM";

        /// <summary>그 주에 재해 기간인 재해 (날씨는 보지 않는다).</summary>
        public static DisasterDef PeriodOf(int week, BalanceConfig balance) =>
            (balance.Disasters ?? Array.Empty<DisasterDef>()).FirstOrDefault(d => week >= d.FromWeek && week <= d.ToWeek);

        /// <summary>지금 일어난 재해 (기간 + 날씨). 없으면 null.</summary>
        public static DisasterDef Active(int week, WeatherType weather, BalanceConfig balance)
        {
            var period = PeriodOf(week, balance);
            return period != null && period.Weather == weather ? period : null;
        }

        public static DisasterDef Active(GameState state, BalanceConfig balance) => Active(state.Calendar.Week, state.Base.Weather, balance);

        /// <summary>탐험 요청의 날씨 문자열로 (요청에는 날씨 이름이 담긴다).</summary>
        public static DisasterDef Active(int week, string weatherId, BalanceConfig balance) =>
            Enum.TryParse<WeatherType>(weatherId ?? "", out var weather) ? Active(week, weather, balance) : null;

        /// <summary>주 날씨: 계절 굴림 뒤, 재해 기간이면 ForceChance로 재해 날씨가 된다 (기간 밖에서는 난수를 더 쓰지 않는다).</summary>
        public static WeatherType RollWeather(int week, BalanceConfig balance, IRandomService random)
        {
            var weather = WeatherRules.Roll(week, balance, random);
            var period = PeriodOf(week, balance);
            if (period != null && weather != period.Weather && random.NextDouble() < period.ForceChance)
            {
                weather = period.Weather;
            }

            return weather;
        }

        /// <summary>물에 잠긴 층 (장마 때 깊은 곳 맨 아래층). 없으면 −1.</summary>
        public static int FloodedFloor(BuildingLayoutDef layout, DisasterDef disaster) =>
            disaster != null && disaster.Id == Monsoon && layout != null && layout.DeepFloors > 0 ? layout.TotalFloors - 1 : -1;

        /// <summary>재해가 바꾼 지역 (사본). 바뀔 것이 없으면 원본을 그대로 돌려준다.</summary>
        public static LocationDef Apply(LocationDef location, DisasterDef disaster)
        {
            var layout = location?.Layout;
            if (disaster == null || layout == null || layout.DeepFloors <= 0)
            {
                return location;
            }

            var copy = location.Copy();
            copy.Layout = layout.CopyLinks();
            var changed = disaster.Id switch
            {
                Monsoon => Flood(copy.Layout, disaster),
                Snowstorm => Snow(copy.Layout, disaster),
                _ => false,
            };
            return changed ? copy : location;
        }

        private static bool Flood(BuildingLayoutDef layout, DisasterDef disaster)
        {
            var flooded = FloodedFloor(layout, disaster);
            var changed = false;
            foreach (var c in layout.Connectors.Where(c => c.Floor == flooded && c.Kind == ConnectorKind.OneWayExit))
            {
                Block(c, disaster);
                changed = true;
            }

            foreach (var exit in layout.Exits.Where(e => e.Kind == ExitKind.Alternate && e.StartsLocked))
            {
                exit.StartsLocked = false;   // 물에 밀려 열렸다
                changed = true;
            }

            return changed;
        }

        private static bool Snow(BuildingLayoutDef layout, DisasterDef disaster)
        {
            var changed = false;
            var entries = layout.Connectors.Where(c => c.Kind == ConnectorKind.DeepDoor && !layout.IsDeep(c.Floor)).ToList();
            if (entries.Count >= 2 && entries.Any(c => c.Floor == 0) && entries.Any(c => c.Floor != 0))
            {
                foreach (var door in entries.Where(c => c.Floor == 0))
                {
                    Block(door, disaster);
                    // 깊은 곳 쪽 짝 문도 막힌다
                    foreach (var pair in layout.Connectors.Where(c => c.Kind == ConnectorKind.DeepDoor && c.Floor == door.TargetFloor && c.TargetFloor == door.Floor))
                    {
                        Block(pair, disaster);
                    }
                }

                changed = true;
            }

            foreach (var exit in layout.Exits.Where(e => e.Kind == ExitKind.Alternate && e.Floor == 0))
            {
                exit.StartsLocked = true;
                exit.BlockedBy = disaster.Id;   // 눈에 파묻혔다
                changed = true;
            }

            return changed;
        }

        private static void Block(ConnectorDef connector, DisasterDef disaster)
        {
            connector.Collapsed = true;
            connector.BlockedBy = disaster.Id;
        }
    }
}
