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
    /// 원본: Content/Events/*.json (이벤트·NPC) + Resources/Dialogue/**/*.yarn (대사).
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

        /// <summary>가져오기 전체 과정. 검증 오류 수를 돌려준다.</summary>
        public static int Import()
        {
            EnsureYarnProject();
            var content = ContentJson.LoadFolder(ContentJson.ContentFolder);
            var events = Upsert<EventDefinitionAsset, EventDef>(EventAssetFolder, content.Events, e => e.Id, (a, d) => a.EditorSetDefinition(d));
            var npcs = Upsert<NpcDefinitionAsset, NpcDef>(NpcAssetFolder, content.Npcs, n => n.Id, (a, d) => a.EditorSetDefinition(d));

            var database = AssetDatabase.LoadAssetAtPath<DefinitionDatabaseAsset>(DatabasePath);
            database.EditorSetStoryContents(events, npcs);
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Content] 이벤트 {events.Count}개, NPC {npcs.Count}개 가져옴");

            SyncDialogueTable();
            return Validate();
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
