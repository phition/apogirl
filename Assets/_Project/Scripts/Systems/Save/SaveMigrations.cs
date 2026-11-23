using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ApoGirl.Systems.Save
{
    /// <summary>기본 제공 세이브 마이그레이션 체인.</summary>
    public static class SaveMigrations
    {
        public static IEnumerable<ISaveMigration> BuiltIn
        {
            get
            {
                yield return new V1ToV2();
                yield return new V2ToV3();
                yield return new V3ToV4();
                yield return new V4ToV5();
                yield return new V5ToV6();
                yield return new V6ToV7();
                yield return new V7ToV8();
                yield return new V8ToV9();
                yield return new V9ToV10();
                yield return new V10ToV11();
                yield return new V11ToV12();
            }
        }

        /// <summary>v2: 지난주 계획(LastWeekPlan), 계획 프리셋(PlanPresets) 추가.</summary>
        private sealed class V1ToV2 : ISaveMigration
        {
            public int FromVersion => 1;

            public void Migrate(JObject root)
            {
                if (root["PlanPresets"] == null)
                {
                    root["PlanPresets"] = new JArray();
                }
            }
        }

        /// <summary>v4: 주간 기록(History) 추가 (M5). 정신 붕괴 연속 주 수는 기본값 0.</summary>
        private sealed class V3ToV4 : ISaveMigration
        {
            public int FromVersion => 3;

            public void Migrate(JObject root)
            {
                if (root["History"] == null)
                {
                    root["History"] = new JArray();
                }
            }
        }

        /// <summary>v5: 허기·갈증(0~100, 높을수록 굶음) → 포만·수분(−100~+100, 0 = 보통). 부호를 뒤집어 옮긴다.</summary>
        private sealed class V4ToV5 : ISaveMigration
        {
            public int FromVersion => 4;

            public void Migrate(JObject root)
            {
                if (!(root["Player"]?["Vitals"] is JObject vitals))
                {
                    return;
                }

                if (vitals["Hunger"] != null)
                {
                    vitals["Satiety"] = -(vitals.Value<int?>("Hunger") ?? 0);
                    vitals.Remove("Hunger");
                }

                if (vitals["Thirst"] != null)
                {
                    vitals["Hydration"] = -(vitals.Value<int?>("Thirst") ?? 0);
                    vitals.Remove("Thirst");
                }
            }
        }

        /// <summary>v6: 세력(Factions) 추가 (2~4막 보강). 처음 평판은 게임이 불러올 때 채운다.</summary>
        private sealed class V5ToV6 : ISaveMigration
        {
            public int FromVersion => 5;

            public void Migrate(JObject root)
            {
                if (root["Factions"] == null)
                {
                    root["Factions"] = new JObject();
                }
            }
        }

        /// <summary>v3: NPC 관계(Npcs), 이벤트 기록(Events) 추가 (M3).</summary>
        private sealed class V2ToV3 : ISaveMigration
        {
            public int FromVersion => 2;

            public void Migrate(JObject root)
            {
                if (root["Npcs"] == null)
                {
                    root["Npcs"] = new JObject();
                }

                if (root["Events"] == null)
                {
                    root["Events"] = new JObject();
                }
            }
        }

        /// <summary>v7: 동료·선생님 몸 상태(Body)와 배분 기록(GiftLog) 추가 (기획 D절). 새 필드는 기본값으로 채워진다.</summary>
        private sealed class V6ToV7 : ISaveMigration
        {
            public int FromVersion => 6;

            public void Migrate(JObject root)
            {
            }
        }

        /// <summary>
        /// v8: 역할 재기획 (Docs/ROLE_ROOMS_PROPOSAL.md). 예전 역할 ID를 새 역할로 옮긴다:
        /// 요리·정비 → 살림, 공부 친구 → 공부, 분위기 메이커 → 위로, 이벤트의 예전 이름(lookout → 경비, scout → 정찰, 그 밖의 모르는 이름 → 없음).
        /// 역할을 받은 주(RoleSinceWeek)는 0으로 둔다 (정원이 넘치면 ID 순).
        /// </summary>
        private sealed class V7ToV8 : ISaveMigration
        {
            public int FromVersion => 7;

            private static readonly Dictionary<string, string> Map = new Dictionary<string, string>
            {
                ["GUARD"] = "GUARD",
                ["SCOUT"] = "SCOUT",
                ["MEDIC"] = "MEDIC",
                ["COOK"] = "HOUSE",
                ["MECHANIC"] = "HOUSE",
                ["TUTOR"] = "STUDY",
                ["MOOD"] = "COMFORT",
                ["lookout"] = "GUARD",
                ["scout"] = "SCOUT",
            };

            public void Migrate(JObject root)
            {
                if (!(root["Npcs"] is JObject npcs))
                {
                    return;
                }

                foreach (var property in npcs.Properties())
                {
                    if (!(property.Value is JObject npc))
                    {
                        continue;
                    }

                    var role = npc.Value<string>("Role") ?? "";
                    npc["Role"] = role.Length == 0 ? "" : Map.TryGetValue(role, out var mapped) ? mapped : "";
                }
            }
        }

        /// <summary>v9: CD 수집 (기획 A). 주운 주차(CdWeeks)·고른 곡(CdTrack) 추가. 새 필드는 기본값으로 채워진다.</summary>
        private sealed class V8ToV9 : ISaveMigration
        {
            public int FromVersion => 8;

            public void Migrate(JObject root)
            {
            }
        }

        /// <summary>v10: 방과후 동아리 (확장 7). 같이 할 사람(ClubPartner)·동료별 활동 횟수(ClubCounts). 새 필드는 기본값으로 채워진다.</summary>
        private sealed class V9ToV10 : ISaveMigration
        {
            public int FromVersion => 9;

            public void Migrate(JObject root)
            {
            }
        }

        /// <summary>v11: 전력 = 유지력. 쌓여 있던 전력은 발전 용량으로 바뀐다 (복구했으면 3, 아니면 0).</summary>
        private sealed class V10ToV11 : ISaveMigration
        {
            public int FromVersion => 10;

            public void Migrate(JObject root)
            {
                if (!(root["Base"] is JObject baseState))
                {
                    return;
                }

                if (!(baseState["Resources"] is JObject resources))
                {
                    resources = new JObject();
                    baseState["Resources"] = resources;
                }

                var restored = baseState["PowerRestored"]?.Value<bool>() ?? false;
                resources["Power"] = restored ? 3 : 0;
            }
        }

        /// <summary>v12: 주간 식단 (FoodDiet·WaterDiet). 새 필드는 기본값(챙겨 먹는다)으로 채워진다.</summary>
        private sealed class V11ToV12 : ISaveMigration
        {
            public int FromVersion => 11;

            public void Migrate(JObject root)
            {
            }
        }
    }
}
