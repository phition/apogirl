using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace ApoGirl.EditorTools
{
    /// <summary>
    /// 문자열 원본(JSON) → Unity Localization 문자열 테이블 동기화.
    /// 원본: Assets/_Project/Localization/Source/ko.json  { "테이블": { "키": "문장" } }
    /// 원본이 정본이며, 테이블은 생성물이다 (테이블을 직접 고치지 말 것).
    /// </summary>
    public static class LocalizationSync
    {
        private const string Root = "Assets/_Project/Localization";
        private const string SourcePath = Root + "/Source/ko.json";
        private const string TablesFolder = Root + "/Tables";
        private const string LocaleCode = "ko";

        [MenuItem("ApoGirl/Localization/Sync Tables")]
        public static void Sync()
        {
            var source = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(SourcePath));
            var settings = EnsureSettings();
            var locale = EnsureLocale(settings);
            EnsureFolder(TablesFolder);

            var total = 0;
            foreach (var table in source)
            {
                var collection = LocalizationEditorSettings.GetStringTableCollection(table.Key)
                                 ?? LocalizationEditorSettings.CreateStringTableCollection(table.Key, TablesFolder);
                var stringTable = collection.GetTable(locale.Identifier) as StringTable
                                  ?? collection.AddNewTable(locale.Identifier) as StringTable;

                foreach (var entry in table.Value)
                {
                    stringTable.AddEntry(entry.Key, entry.Value);
                    total++;
                }

                EditorUtility.SetDirty(stringTable);
                EditorUtility.SetDirty(collection.SharedData);
                EditorUtility.SetDirty(collection);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[LocalizationSync] {source.Count}개 테이블, {total}개 항목 동기화");
        }

        private static LocalizationSettings EnsureSettings()
        {
            var settings = LocalizationEditorSettings.ActiveLocalizationSettings;
            if (settings != null)
            {
                return settings;
            }

            EnsureFolder(Root);
            settings = ScriptableObject.CreateInstance<LocalizationSettings>();
            AssetDatabase.CreateAsset(settings, Root + "/LocalizationSettings.asset");
            LocalizationEditorSettings.ActiveLocalizationSettings = settings;
            return settings;
        }

        private static Locale EnsureLocale(LocalizationSettings settings)
        {
            var identifier = new LocaleIdentifier(LocaleCode);
            var locale = LocalizationEditorSettings.GetLocale(identifier);
            if (locale == null)
            {
                EnsureFolder(Root + "/Locales");
                locale = Locale.CreateLocale(identifier);
                locale.name = $"Korean ({LocaleCode})";
                AssetDatabase.CreateAsset(locale, $"{Root}/Locales/{locale.name}.asset");
                LocalizationEditorSettings.AddLocale(locale);
            }

            var selectors = settings.GetStartupLocaleSelectors();
            if (!selectors.Exists(s => s is SpecificLocaleSelector))
            {
                selectors.Insert(0, new SpecificLocaleSelector { LocaleId = identifier });
                EditorUtility.SetDirty(settings);
            }

            return locale;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
