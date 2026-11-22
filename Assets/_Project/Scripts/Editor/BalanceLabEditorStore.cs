using System;
using System.IO;
using System.Linq;
using System.Text;
using ApoGirl.Core.Balance;
using ApoGirl.Core.Definitions;
using ApoGirl.Presentation.Screens;
using ApoGirl.Systems.Content;
using UnityEditor;
using UnityEngine;

namespace ApoGirl.EditorTools
{
    /// <summary>
    /// 밸런스 표 저장 (Docs/BALANCE_LAB_PROPOSAL.md 7.1~7.3, L3).
    /// 수치 → Assets/_Project/Balance/balance_overrides.json (코드 기본값과 다른 것만) → Sync Balance Asset.
    /// 탐험 모델 → Content/**/*.json의 그 지역 줄을 글자 단위로 고침 → Import Events.
    /// </summary>
    [InitializeOnLoad]
    public static class BalanceLabEditorStore
    {
        static BalanceLabEditorStore()
        {
            BalanceLabStore.Save = Save;
            BalanceLabStore.ResetToDefaults = ResetToDefaults;
        }

        public static BalanceLabStore.Result Save(IDefinitionDatabase db)
        {
            try
            {
                var model = BalanceLabModel.For(db);
                var overrides = BalanceOverridesJson.Collect(db.Balance);
                WriteOverrides(overrides);

                var written = 0;
                var missing = new StringBuilder();
                foreach (var id in model.ChangedModelIds.ToList())
                {
                    if (!model.TryGetModel(id, out var def))
                    {
                        continue;
                    }

                    var result = PatchModel(def);
                    if (result == null) missing.Append(id).Append(' ');
                    else if (result.Value) written++;
                }

                ContentPipeline.SyncBalance();
                if (written > 0)
                {
                    ContentPipeline.Import();
                }

                model.RebaseAll();
                Debug.Log($"[BalanceLab] 저장: 덮어쓰기 {overrides.Count}개, 탐험 모델 {written}곳");
                return new BalanceLabStore.Result
                {
                    Fields = overrides.Count,
                    Models = written,
                    Error = missing.Length > 0 ? "콘텐츠 JSON에서 못 찾은 모델: " + missing : null,
                };
            }
            catch (Exception exception) when (exception is IOException || exception is FormatException || exception is UnauthorizedAccessException)
            {
                Debug.LogException(exception);
                return new BalanceLabStore.Result { Error = exception.Message };
            }
        }

        public static BalanceLabStore.Result ResetToDefaults(IDefinitionDatabase db)
        {
            try
            {
                BalanceOverridesJson.ResetEditable(db.Balance);
                WriteOverrides(new BalanceOverridesJson.Overrides());
                ContentPipeline.SyncBalance();
                BalanceLabModel.For(db).RebaseAll();
                return new BalanceLabStore.Result();
            }
            catch (IOException exception)
            {
                return new BalanceLabStore.Result { Error = exception.Message };
            }
        }

        private static void WriteOverrides(BalanceOverridesJson.Overrides overrides)
        {
            var path = BalanceOverridesJson.DefaultPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var json = BalanceOverridesJson.Write(overrides);
            if (!File.Exists(path) || File.ReadAllText(path).Replace("\r\n", "\n") != json)
            {
                File.WriteAllText(path, json);
                AssetDatabase.ImportAsset(path);
            }
        }

        /// <summary>그 모델이 든 콘텐츠 JSON을 찾아 고친다. 못 찾으면 null, 바뀐 게 없으면 false.</summary>
        private static bool? PatchModel(ExpeditionModelDef def)
        {
            foreach (var file in Directory.GetFiles(ContentJson.ContentFolder, "*.json", SearchOption.AllDirectories))
            {
                var bytes = File.ReadAllBytes(file);
                var bom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
                var text = new UTF8Encoding(false).GetString(bytes, bom ? 3 : 0, bytes.Length - (bom ? 3 : 0));
                var patched = ExpeditionModelPatcher.Patch(text, def, out var found);
                if (!found)
                {
                    continue;
                }

                if (patched == text)
                {
                    return false;
                }

                File.WriteAllText(file, patched, new UTF8Encoding(bom));
                Debug.Log($"[BalanceLab] {def.Id} → {file}");
                return true;
            }

            return null;
        }
    }
}
