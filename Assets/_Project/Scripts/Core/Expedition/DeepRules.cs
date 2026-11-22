using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;

namespace ApoGirl.Core.Expedition
{
    /// <summary>
    /// 탐험의 깊이 (기획 B) 배치 점검: 깊은 곳 층은 겉 층 뒤에 붙고, 깊은 문(양방향)·일방 출구(깊은 곳 → 겉)·계단으로 이어진다.
    /// 지역 빌드 때와 테스트에서 같은 검사를 쓴다.
    /// </summary>
    public static class DeepRules
    {
        /// <summary>배치 문제 목록 (비면 문제없음).</summary>
        public static List<string> Issues(BuildingLayoutDef layout)
        {
            var issues = new List<string>();
            if (layout == null)
            {
                return issues;
            }

            var total = layout.TotalFloors;
            foreach (var c in layout.Connectors)
            {
                if (c.Floor < 0 || c.Floor >= total || c.TargetFloor < 0 || c.TargetFloor >= total)
                {
                    issues.Add($"{c.Kind} 층 범위 밖: {c.Floor} → {c.TargetFloor}");
                    continue;
                }

                if (c.X < 0f || c.X > layout.CorridorLengthOf(c.Floor) || c.ArrivalX > layout.CorridorLengthOf(c.TargetFloor))
                {
                    issues.Add($"{c.Kind} 자리가 복도 밖: {c.Floor}층 {c.X} → {c.TargetFloor}층 {c.ArrivalX}");
                }

                switch (c.Kind)
                {
                    case ConnectorKind.DeepDoor:
                        if (layout.IsDeep(c.Floor) == layout.IsDeep(c.TargetFloor))
                        {
                            issues.Add($"깊은 문은 겉과 깊은 곳을 잇는다: {c.Floor} → {c.TargetFloor}");
                        }

                        if (!layout.Connectors.Any(o => o.Kind == ConnectorKind.DeepDoor && o.Floor == c.TargetFloor && o.TargetFloor == c.Floor
                                                        && Near(o.X, c.ArrivalX) && Near(o.ArrivalX, c.X)))
                        {
                            issues.Add($"깊은 문의 반대쪽이 없음: {c.Floor}층 {c.X}");
                        }

                        break;
                    case ConnectorKind.OneWayExit:
                        if (!layout.IsDeep(c.Floor) || layout.IsDeep(c.TargetFloor))
                        {
                            issues.Add($"일방 출구는 깊은 곳 → 겉: {c.Floor} → {c.TargetFloor}");
                        }

                        if (!layout.Connectors.Any(o => o.Kind == ConnectorKind.OneWayBack && o.Floor == c.TargetFloor && Near(o.X, c.ArrivalX)))
                        {
                            issues.Add($"일방 출구의 바깥쪽 철문이 없음: {c.TargetFloor}층 {c.ArrivalX}");
                        }

                        break;
                }
            }

            if (layout.DeepFloors <= 0)
            {
                if (layout.Connectors.Any(c => !c.IsStairs))
                {
                    issues.Add("깊은 곳이 없는데 깊은 문·일방 출구가 있음");
                }

                return issues;
            }

            // 겉에서 모든 깊은 곳 층에 닿고, 모든 깊은 곳 층에서 겉으로 나갈 수 있어야 한다
            var fromOuter = Reach(layout, Enumerable.Range(0, layout.Floors));
            for (var floor = layout.Floors; floor < total; floor++)
            {
                if (!fromOuter.Contains(floor))
                {
                    issues.Add($"겉에서 지하 {layout.DeepLevel(floor)}에 닿을 수 없음");
                }

                var back = Reach(layout, new[] { floor });
                if (!back.Any(f => !layout.IsDeep(f)))
                {
                    issues.Add($"지하 {layout.DeepLevel(floor)}에서 겉으로 나갈 수 없음");
                }
            }

            if (!layout.Connectors.Any(c => c.Kind == ConnectorKind.OneWayExit))
            {
                issues.Add("깊은 곳에 일방 출구가 없음");
            }

            return issues;
        }

        /// <summary>깊은 문 수 (겉 쪽에서 센다).</summary>
        public static int EntryCount(BuildingLayoutDef layout) =>
            layout?.Connectors.Count(c => c.Kind == ConnectorKind.DeepDoor && !layout.IsDeep(c.Floor)) ?? 0;

        private static HashSet<int> Reach(BuildingLayoutDef layout, IEnumerable<int> start)
        {
            var seen = new HashSet<int>(start);
            var queue = new Queue<int>(seen);
            while (queue.Count > 0)
            {
                var floor = queue.Dequeue();
                foreach (var c in layout.Connectors)
                {
                    var passable = !c.Collapsed && (c.Kind == ConnectorKind.DeepDoor || c.Kind == ConnectorKind.OneWayExit || c.IsStairs);   // 막힌 깊은 문(계절 재해)도 못 지난다
                    if (c.Floor == floor && passable && seen.Add(c.TargetFloor))
                    {
                        queue.Enqueue(c.TargetFloor);
                    }
                }
            }

            return seen;
        }

        private static bool Near(float a, float b) => System.Math.Abs(a - b) < 0.01f;
    }
}
