using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Events;
using ApoGirl.Data;
using ApoGirl.Systems.Content;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using Yarn.Unity;

namespace ApoGirl.EditorTools
{
    /// <summary>
    /// M3 콘텐츠 파이프라인.
    /// 원본: Content/**/*.json (이벤트·NPC·행동·아이템·전리품·특성·시설·대사 줄·프롤로그) + Resources/Dialogue/**/*.yarn (대사).
    /// Import: JSON → Data/Events·Data/NPCs SO 에셋 + 정의 DB 등록 → Yarn 대사를 "Dialogue" 문자열 테이블로 동기화 → 검증.
    /// SO 에셋은 생성물이다 (직접 고치지 말고 JSON을 고칠 것).
    /// </summary>
    public static class ContentPipeline
    {
        public const string EventAssetFolder = "Assets/_Project/Data/Events";
        public const string NpcAssetFolder = "Assets/_Project/Data/NPCs";
        public const string DatabasePath = "Assets/_Project/Data/DefinitionDatabase.asset";
        public const string DialogueFolder = "Assets/_Project/Resources/Dialogue";
        public const string YarnProjectPath = DialogueFolder + "/ApoGirl.yarnproject";
        public const string LocalizationSource = "Assets/_Project/Localization/Source/ko.json";
        public const string ReportPath = "Temp/ApoGirlContentReport.txt";
        private const string DialogueTable = "Dialogue";
        private const string LocaleCode = "ko";

        [MenuItem("ApoGirl/Content/Import Events")]
        public static void ImportMenu()
        {
            var errors = Import();
            EditorUtility.DisplayDialog("콘텐츠 가져오기", errors == 0 ? "완료 (오류 없음)" : $"완료 — 오류 {errors}개. 콘솔을 확인하세요.", "확인");
        }

        public const string BalancePath = "Assets/_Project/Data/Balance/BalanceConfig.asset";

        /// <summary>
        /// M7-2: 밸런스 에셋을 BalanceConfig의 현재 값으로 다시 쓴다 (테스트와 같은 값).
        /// 에셋 파일에 없는 필드는 처음 불러온 시점의 기본값이 메모리에 남아 코드 변경이 반영되지 않던 문제를 막는다.
        /// </summary>
        [MenuItem("ApoGirl/Balance/Sync Balance Asset")]
        public static void SyncBalance()
        {
            var balance = AssetDatabase.LoadAssetAtPath<BalanceConfigAsset>(BalancePath);
            if (balance == null)
            {
                Debug.LogError($"[Content] 밸런스 에셋 없음: {BalancePath}");
                return;
            }

            balance.EditorSetConfig(new BalanceConfig());
            EditorUtility.SetDirty(balance);
            AssetDatabase.SaveAssets();
            WriteBalanceSections();
        }

        /// <summary>밸런스 표 "전체" 탭의 절 이름: BalanceConfig.cs의 "// ── 절 ──" 줄을 뽑아 Resources에 둔다 (빌드에서도 쓰게).</summary>
        public static void WriteBalanceSections()
        {
            if (!File.Exists(BalanceLabJson.BalanceSourcePath))
            {
                return;
            }

            var json = BalanceLabJson.WriteSections(BalanceLabJson.ExtractSections(File.ReadAllText(BalanceLabJson.BalanceSourcePath)));
            if (File.Exists(BalanceLabJson.SectionsPath) && File.ReadAllText(BalanceLabJson.SectionsPath) == json)
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(BalanceLabJson.SectionsPath));
            File.WriteAllText(BalanceLabJson.SectionsPath, json);
            AssetDatabase.ImportAsset(BalanceLabJson.SectionsPath);
        }

        /// <summary>가져오기 전체 과정. 검증 오류 수를 돌려준다.</summary>
        public static int Import()
        {
            EnsureYarnProject();
            var content = ContentJson.LoadFolder(ContentJson.ContentFolder);
            var events = Upsert<EventDefinitionAsset, EventDef>(EventAssetFolder, content.Events, e => e.Id, (a, d) => a.EditorSetDefinition(d));
            var npcs = Upsert<NpcDefinitionAsset, NpcDef>(NpcAssetFolder, content.Npcs, n => n.Id, (a, d) => a.EditorSetDefinition(d));

            var database = AssetDatabase.LoadAssetAtPath<DefinitionDatabaseAsset>(DatabasePath);
            database.EditorSetStoryContents(events, npcs);

            // M4: 행동·아이템·전리품·탐험 모델·적 (원본에 있는 종류만 갱신), 특성·시설·대사 줄·프롤로그 묶음
            if (content.Actions.Count > 0 || content.Items.Count > 0)
            {
                var actions = Upsert<ActionDefinitionAsset, ActionDef>("Assets/_Project/Data/Actions", content.Actions, d => d.Id, (a, d) => a.EditorSetDefinition(d));
                var items = Upsert<ItemDefinitionAsset, ItemDef>("Assets/_Project/Data/Items", content.Items, d => d.Id, (a, d) => a.EditorSetDefinition(d));
                var loot = Upsert<LootTableAsset, LootTableDef>("Assets/_Project/Data/Loot", content.LootTables, d => d.Id, (a, d) => a.EditorSetDefinition(d));
                var models = Upsert<ExpeditionModelAsset, ExpeditionModelDef>("Assets/_Project/Data/Expeditions", content.ExpeditionModels, d => d.Id, (a, d) => a.EditorSetDefinition(d));
                var enemies = Upsert<EnemyAsset, EnemyDef>("Assets/_Project/Data/Enemies", content.Enemies, d => d.Id, (a, d) => a.EditorSetDefinition(d));
                var extras = AssetDatabase.LoadAssetAtPath<StoryExtrasAsset>(ExtrasPath);
                if (extras == null)
                {
                    extras = ScriptableObject.CreateInstance<StoryExtrasAsset>();
                    AssetDatabase.CreateAsset(extras, ExtrasPath);
                }

                extras.EditorSet(content.Traits, content.Facilities, content.Lines, content.Prologue ?? new PrologueDef(), content.Endings, content.Roles, content.StartBonuses, content.Factions);
                EditorUtility.SetDirty(extras);
                database.EditorSetContent(actions, items, loot, models, enemies, extras);
                Debug.Log($"[Content] 행동 {actions.Count}, 아이템 {items.Count}, 전리품 {loot.Count}, 모델 {models.Count}, 적 {enemies.Count}, 특성 {content.Traits.Count}, 시설 {content.Facilities.Count}, 대사 줄 {content.Lines.Count}, 엔딩 {content.Endings.Count}, 역할 {content.Roles.Count}, 세력 {content.Factions.Count}");
            }

            SyncBalance();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Content] 이벤트 {events.Count}개, NPC {npcs.Count}개 가져옴");

            SyncDialogueTable();
            return Validate();
        }

        public const string ExtrasPath = "Assets/_Project/Data/StoryExtras.asset";

        /// <summary>현재 정의 DB의 행동·아이템·전리품·탐험 모델·적을 JSON으로 내보낸다 (원본 이전용).</summary>
        [MenuItem("ApoGirl/Content/Export Definitions To JSON")]
        public static void ExportDefinitions()
        {
            var db = AssetDatabase.LoadAssetAtPath<DefinitionDatabaseAsset>(DatabasePath).Build();
            var content = new ContentFile();
            content.Actions.AddRange(db.Actions);
            foreach (var guid in AssetDatabase.FindAssets("t:ItemDefinitionAsset")) content.Items.Add(AssetDatabase.LoadAssetAtPath<ItemDefinitionAsset>(AssetDatabase.GUIDToAssetPath(guid)).ToDefinition());
            foreach (var guid in AssetDatabase.FindAssets("t:LootTableAsset")) content.LootTables.Add(AssetDatabase.LoadAssetAtPath<LootTableAsset>(AssetDatabase.GUIDToAssetPath(guid)).ToDefinition());
            foreach (var guid in AssetDatabase.FindAssets("t:ExpeditionModelAsset")) content.ExpeditionModels.Add(AssetDatabase.LoadAssetAtPath<ExpeditionModelAsset>(AssetDatabase.GUIDToAssetPath(guid)).ToDefinition());
            foreach (var guid in AssetDatabase.FindAssets("t:EnemyAsset")) content.Enemies.Add(AssetDatabase.LoadAssetAtPath<EnemyAsset>(AssetDatabase.GUIDToAssetPath(guid)).ToDefinition());
            Directory.CreateDirectory("Temp/ContentExport");
            File.WriteAllText("Temp/ContentExport/exported.json", ContentJson.Serialize(content));
            Debug.Log("[Content] 내보냄: Temp/ContentExport/exported.json");
        }

        [MenuItem("ApoGirl/Content/Validate")]
        public static void ValidateMenu()
        {
            var errors = Validate();
            EditorUtility.DisplayDialog("콘텐츠 검증", errors == 0 ? "오류 없음" : $"오류 {errors}개. 콘솔·{ReportPath}를 확인하세요.", "확인");
        }

        /// <summary>정의 DB·Yarn 대본·UI 문자열을 대조한다. 결과는 콘솔과 Temp/ApoGirlContentReport.txt.</summary>
        public static int Validate()
        {
            var db = AssetDatabase.LoadAssetAtPath<DefinitionDatabaseAsset>(DatabasePath).Build();
            var input = new ContentValidationInput { YarnNodes = ScanYarn(), UiKeys = LoadUiKeys() };
            var issues = ContentValidator.Validate(db, input);

            var report = new StringBuilder();
            report.AppendLine($"events={db.Events.Count} npcs={db.Npcs.Count} yarnNodes={input.YarnNodes.Count} issues={issues.Count}");
            foreach (var issue in issues)
            {
                report.AppendLine(issue.ToString());
                if (issue.Severity == IssueSeverity.Error)
                {
                    Debug.LogError("[Content] " + issue);
                }
                else
                {
                    Debug.LogWarning("[Content] " + issue);
                }
            }

            File.WriteAllText(ReportPath, report.ToString());
            var errors = issues.Count(i => i.Severity == IssueSeverity.Error);
            Debug.Log($"[Content] 검증: 오류 {errors}, 경고 {issues.Count - errors}");
            return errors;
        }

        /// <summary>Yarn 프로젝트의 기본 언어 대사를 Unity Localization "Dialogue" 테이블로 옮긴다 (D21: Unity Localization 연동).</summary>
        [MenuItem("ApoGirl/Content/Sync Dialogue Table")]
        public static void SyncDialogueTable()
        {
            AssetDatabase.ImportAsset(YarnProjectPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive);
            var project = AssetDatabase.LoadAssetAtPath<YarnProject>(YarnProjectPath);
            if (project == null || project.baseLocalization == null)
            {
                Debug.LogError("[Content] Yarn 프로젝트를 불러오지 못함 (대본 컴파일 오류?): " + YarnProjectPath);
                return;
            }

            var collection = LocalizationEditorSettings.GetStringTableCollection(DialogueTable)
                             ?? LocalizationEditorSettings.CreateStringTableCollection(DialogueTable, "Assets/_Project/Localization/Tables");
            var locale = LocalizationEditorSettings.GetLocale(new LocaleIdentifier(LocaleCode));
            var table = collection.GetTable(locale.Identifier) as StringTable ?? collection.AddNewTable(locale.Identifier) as StringTable;

            var ids = project.baseLocalization.GetLineIDs().ToList();
            var keep = new HashSet<string>(ids);
            foreach (var stale in collection.SharedData.Entries.Select(e => e.Key).Where(k => !keep.Contains(k)).ToList())
            {
                collection.RemoveEntry(stale);
            }

            foreach (var id in ids)
            {
                table.AddEntry(id, project.baseLocalization.GetLocalizedString(id));
            }

            EditorUtility.SetDirty(table);
            EditorUtility.SetDirty(collection.SharedData);
            EditorUtility.SetDirty(collection);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Content] 대사 {ids.Count}줄 → '{DialogueTable}' 테이블");
        }

        /// <summary>Resources/Dialogue 아래 *.yarn에서 노드와 #choice 태그를 모은다.</summary>
        public static Dictionary<string, HashSet<string>> ScanYarn()
        {
            var nodes = new Dictionary<string, HashSet<string>>();
            if (!Directory.Exists(DialogueFolder))
            {
                return nodes;
            }

            foreach (var file in Directory.GetFiles(DialogueFolder, "*.yarn", SearchOption.AllDirectories))
            {
                YarnScriptScanner.Scan(File.ReadAllText(file), nodes);
            }

            return nodes;
        }

        private static HashSet<string> LoadUiKeys()
        {
            var source = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(LocalizationSource));
            return source.TryGetValue("UI", out var ui) ? new HashSet<string>(ui.Keys) : new HashSet<string>();
        }

        /// <summary>
        /// [YarnFunction] 선언은 Yarn 소스 생성기가 ProjectSettings/Packages/dev.yarnspinner/*.ysls.json으로 만든다.
        /// 프로젝트가 그 파일을 읽어야 name()·josa() 같은 함수를 컴파일할 수 있다.
        /// </summary>
        private const string GeneratedDefinitions = "${workspaceRoot}/ProjectSettings/Packages/dev.yarnspinner/**/*.ysls.json";

        private static void EnsureYarnProject()
        {
            Yarn.Compiler.Project project;
            if (File.Exists(YarnProjectPath))
            {
                project = Yarn.Compiler.Project.LoadFromFile(YarnProjectPath);
                if (project.Definitions != null && project.Definitions.Contains(GeneratedDefinitions))
                {
                    return;
                }
            }
            else
            {
                Directory.CreateDirectory(DialogueFolder);
                project = new Yarn.Compiler.Project
                {
                    BaseLanguage = LocaleCode,
                    SourceFilePatterns = new[] { "**/*.yarn" },
                    ExcludeFilePatterns = new[] { "**/*~/*" },
                };
            }

            project.Definitions ??= new List<string>();
            project.Definitions.Add(GeneratedDefinitions);
            project.SaveToFile(YarnProjectPath);
            AssetDatabase.ImportAsset(YarnProjectPath, ImportAssetOptions.ForceUpdate);
            Debug.Log("[Content] Yarn 프로젝트 설정 갱신: " + YarnProjectPath);
        }

        /// <summary>정의마다 에셋 하나 (파일 이름 = ID). 원본에 없는 에셋은 지운다.</summary>
        private static List<TAsset> Upsert<TAsset, TDef>(string folder, List<TDef> defs, System.Func<TDef, string> idOf, System.Action<TAsset, TDef> set)
            where TAsset : DefinitionAsset
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                var parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
                AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
            }

            var result = new List<TAsset>();
            var ids = new HashSet<string>();
            foreach (var def in defs)
            {
                var id = idOf(def);
                if (string.IsNullOrEmpty(id) || !ids.Add(id))
                {
                    Debug.LogError($"[Content] 빈 ID 또는 중복 ID: '{id}' ({typeof(TDef).Name})");
                    continue;
                }

                var path = $"{folder}/{id}.asset";
                var asset = AssetDatabase.LoadAssetAtPath<TAsset>(path);
                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<TAsset>();
                    AssetDatabase.CreateAsset(asset, path);
                }

                asset.EditorSetId(id);
                set(asset, def);
                EditorUtility.SetDirty(asset);
                result.Add(asset);
            }

            foreach (var guid in AssetDatabase.FindAssets("t:" + typeof(TAsset).Name, new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<TAsset>(path);
                if (asset != null && !ids.Contains(asset.Id))
                {
                    AssetDatabase.DeleteAsset(path);
                    Debug.Log("[Content] 원본에 없는 에셋 삭제: " + path);
                }
            }

            return result;
        }
    }
}
