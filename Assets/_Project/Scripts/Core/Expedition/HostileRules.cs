using System;
using System.Collections.Generic;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Random;

namespace ApoGirl.Core.Expedition
{
    /// <summary>
    /// 적대적 생존자 (낮 탐험): 등장·성향 판정과 붙잡혔을 때의 결과. 실시간 탐험과 헤드리스가 같이 쓴다.
    /// 상자 고갈(교실동) 키도 여기서 정한다.
    /// </summary>
    public static class HostileRules
    {
        /// <summary>등장 판정 난수 (출정 시드에서 따로 뽑아 다른 굴림을 흔들지 않는다).</summary>
        private const int RollSalt = 0x40571E;

        /// <summary>요청에 생존자 성향·수를 정해 싣는다 (낮 탐험만).</summary>
        public static void Assign(ExpeditionRequest request, LocationDef location, BalanceConfig balance)
        {
            request.HostileTemperament = Roll(request, location, balance, out var count);
            request.HostileCount = count;
        }

        /// <summary>이번 출정의 생존자 성향 (0 = 없음, 1~3). 밤 탐험·시작 주 이전·배율 0이면 0.</summary>
        public static int Roll(ExpeditionRequest request, LocationDef location, BalanceConfig balance) => Roll(request, location, balance, out _);

        public static int Roll(ExpeditionRequest request, LocationDef location, BalanceConfig balance, out int count)
        {
            count = 0;
            if (balance == null || request == null || request.TimeOfDay != ExpeditionTime.Day || request.Week < balance.HostileStartWeek)
            {
                return 0;
            }

            var act = ActIndex(request.Week);
            var chance = Chance(request.Week, balance) * (location?.DayVariant?.HostileChanceMultiplier ?? 1f);
            if (chance <= 0f)
            {
                return 0;
            }

            var random = new SeededRandom(SeededRandom.CreateState(request.Seed ^ RollSalt));
            if (random.NextDouble() >= chance)
            {
                return 0;
            }

            var weights = new int[3];
            for (var i = 0; i < 3; i++)
            {
                var index = act * 3 + i;
                weights[i] = balance.HostileWeightsByAct != null && index < balance.HostileWeightsByAct.Length ? Math.Max(0, balance.HostileWeightsByAct[index]) : 1;
            }

            var min = At(balance.HostileCountMinByAct, act, 1);
            var max = Math.Max(min, At(balance.HostileCountMaxByAct, act, min));
            count = Math.Max(1, random.Range(min, max));

            var total = weights[0] + weights[1] + weights[2];
            if (total <= 0)
            {
                return 1;
            }

            var pick = random.Range(0, total - 1);
            for (var i = 0; i < weights.Length; i++)
            {
                pick -= weights[i];
                if (pick < 0)
                {
                    return i + 1;
                }
            }

            return weights.Length;
        }

        /// <summary>막 번호 0~3 (1~13 / 14~26 / 27~39 / 40~52주).</summary>
        public static int ActIndex(int week) => week <= 13 ? 0 : week <= 26 ? 1 : week <= 39 ? 2 : 3;

        /// <summary>막별 기본 확률.</summary>
        public static float Chance(int week, BalanceConfig balance)
        {
            if (week < balance.HostileStartWeek || balance.HostileChanceByAct == null || balance.HostileChanceByAct.Length == 0)
            {
                return 0f;
            }

            var act = Math.Min(ActIndex(week), balance.HostileChanceByAct.Length - 1);
            return balance.HostileChanceByAct[act];
        }

        private static int At(int[] values, int index, int fallback)
        {
            return values != null && values.Length > 0 ? values[Math.Min(index, values.Length - 1)] : fallback;
        }

        public static HostileOutcome OutcomeFor(int temperament)
        {
            switch (temperament)
            {
                case 1: return HostileOutcome.Evicted;
                case 2: return HostileOutcome.Robbed;
                case 3: return HostileOutcome.Beaten;
                default: return HostileOutcome.None;
            }
        }

        /// <summary>
        /// 붙잡힌 결과를 탐험 결과에 반영한다. 쫓겨남은 가진 것을 지키고, 강탈·구타는 이번에 얻은 것을 모두 잃는다.
        /// 구타는 체력을 크게 잃지만 돌아올 때 최소 1은 남긴다. (들고 간 소모품·무기 분실, 트라우마는 거점이 처리)
        /// </summary>
        public static void Apply(ExpeditionResult result, HostileOutcome outcome, int startHp, BalanceConfig balance)
        {
            if (result == null || outcome == HostileOutcome.None)
            {
                return;
            }

            result.Hostile = outcome;
            switch (outcome)
            {
                case HostileOutcome.Evicted:
                    result.StressDelta += balance.EvictedStress;
                    break;
                case HostileOutcome.Robbed:
                    result.Loot.Clear();
                    result.ResourceLoot.Clear();
                    result.StressDelta += balance.RobbedStress;
                    break;
                case HostileOutcome.Beaten:
                    result.Loot.Clear();
                    result.ResourceLoot.Clear();
                    result.StressDelta += balance.BeatenStress;
                    var hpAfter = startHp + result.HpDelta;
                    result.HpDelta -= Math.Max(0, Math.Min(balance.BeatenHpLoss, hpAfter - 1));
                    break;
            }
        }

        // ── 상자 고갈 (DepletingContainers) ──

        public static string CorridorKey(string locationId, int containerIndex) => locationId + "/C" + containerIndex;

        public static string RoomKey(string locationId, int doorIndex, int containerIndex) => locationId + "/R" + doorIndex + "." + containerIndex;

        /// <summary>지역의 모든 상자 키 (복도 + 방 템플릿 상자).</summary>
        public static List<string> AllContainerKeys(LocationDef location)
        {
            var keys = new List<string>();
            var layout = location?.Layout;
            if (layout == null)
            {
                return keys;
            }

            for (var i = 0; i < layout.Containers.Count; i++)
            {
                keys.Add(CorridorKey(location.Id, i));
            }

            for (var d = 0; d < layout.Doors.Count; d++)
            {
                var template = layout.FindTemplate(layout.Doors[d].TemplateId);
                if (template == null)
                {
                    continue;
                }

                for (var i = 0; i < template.Containers.Count; i++)
                {
                    keys.Add(RoomKey(location.Id, d, i));
                }
            }

            return keys;
        }
    }
}
