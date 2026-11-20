using System;
using System.Collections.Generic;
using System.IO;
using ApoGirl.Core.Definitions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ApoGirl.Systems.Content
{
    /// <summary>콘텐츠 원본 파일 하나: NPC와 이벤트 목록.</summary>
    public sealed class ContentFile
    {
        public List<NpcDef> Npcs = new List<NpcDef>();
        public List<EventDef> Events = new List<EventDef>();

        // ── M4 ──
        public List<ActionDef> Actions = new List<ActionDef>();
        public List<ItemDef> Items = new List<ItemDef>();
        public List<LootTableDef> LootTables = new List<LootTableDef>();
        public List<ExpeditionModelDef> ExpeditionModels = new List<ExpeditionModelDef>();
        public List<EnemyDef> Enemies = new List<EnemyDef>();
        public List<TraitDef> Traits = new List<TraitDef>();
        public List<FacilityDef> Facilities = new List<FacilityDef>();
        public List<LineDef> Lines = new List<LineDef>();

        // ── M5 ──
        public List<EndingDef> Endings = new List<EndingDef>();

        // ── M6 ──
        public List<RoleDef> Roles = new List<RoleDef>();
        public List<StartBonusDef> StartBonuses = new List<StartBonusDef>();

        // ── 2~4막 보강 ──
        public List<FactionDef> Factions = new List<FactionDef>();

        /// <summary>여러 파일 중 하나에만 둔다 (마지막 것이 이긴다).</summary>
        public PrologueDef Prologue;
    }

    /// <summary>
    /// 이벤트·NPC 원본 JSON (Assets/_Project/Content/Events/*.json) 읽기.
    /// 필드 이름은 C# 정의와 같고, enum은 이름 문자열로 쓴다. 모르는 필드는 오타로 보고 오류를 낸다.
    /// </summary>
    public static class ContentJson
    {
        public const string ContentFolder = "Assets/_Project/Content";

        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Error,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            Converters = { new StringEnumConverter() },
        };

        public static ContentFile Parse(string json, string sourceName = "")
        {
            try
            {
                return JsonConvert.DeserializeObject<ContentFile>(json, Settings) ?? new ContentFile();
            }
            catch (JsonException exception)
            {
                throw new FormatException($"콘텐츠 JSON 오류 ({sourceName}): {exception.Message}", exception);
            }
        }

        /// <summary>폴더의 모든 *.json을 파일 이름 순으로 합친다.</summary>
        public static ContentFile LoadFolder(string folder)
        {
            var merged = new ContentFile();
            if (!Directory.Exists(folder))
            {
                return merged;
            }

            var files = Directory.GetFiles(folder, "*.json", SearchOption.AllDirectories);
            Array.Sort(files, StringComparer.Ordinal);
            foreach (var file in files)
            {
                var content = Parse(File.ReadAllText(file), Path.GetFileName(file));
                merged.Npcs.AddRange(content.Npcs);
                merged.Events.AddRange(content.Events);
                merged.Actions.AddRange(content.Actions);
                merged.Items.AddRange(content.Items);
                merged.LootTables.AddRange(content.LootTables);
                merged.ExpeditionModels.AddRange(content.ExpeditionModels);
                merged.Enemies.AddRange(content.Enemies);
                merged.Traits.AddRange(content.Traits);
                merged.Facilities.AddRange(content.Facilities);
                merged.Lines.AddRange(content.Lines);
                merged.Endings.AddRange(content.Endings);
                merged.Roles.AddRange(content.Roles);
                merged.StartBonuses.AddRange(content.StartBonuses);
                merged.Factions.AddRange(content.Factions);
                if (content.Prologue != null)
                {
                    merged.Prologue = content.Prologue;
                }
            }

            return merged;
        }

        public static string Serialize(ContentFile content)
        {
            return JsonConvert.SerializeObject(content, Formatting.Indented, Settings);
        }
    }
}
