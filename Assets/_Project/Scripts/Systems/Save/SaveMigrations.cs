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
    }
}
