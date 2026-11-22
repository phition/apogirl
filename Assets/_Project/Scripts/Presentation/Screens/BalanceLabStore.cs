using System;
using System.IO;
using System.Linq;
using ApoGirl.Core.Balance;
using ApoGirl.Core.Definitions;
using ApoGirl.Systems.Content;
using UnityEngine;

namespace ApoGirl.Presentation.Screens
{
    /// <summary>
    /// 밸런스 표 저장 창구 (Docs/BALANCE_LAB_PROPOSAL.md 7.3).
    /// 저장·기본값으로는 저장소 파일을 쓰므로 에디터가 채운다(Editor/BalanceLabEditorStore). 빌드에서는 비어 있어 단추가 꺼진다.
    /// 내보내기는 어디서나 persistentDataPath에 쓴다.
    /// </summary>
    public static class BalanceLabStore
    {
        public struct Result
        {
            /// <summary>덮어쓰기 파일에 적힌 수치 수.</summary>
            public int Fields;

            /// <summary>고쳐 쓴 탐험 모델 지역 수.</summary>
            public int Models;

            /// <summary>실패하면 이유 (성공이면 null).</summary>
            public string Error;

            /// <summary>내보낸 파일 경로.</summary>
            public string Path;
        }

        /// <summary>에디터: 덮어쓰기 파일 + 콘텐츠 JSON에 쓰고 에셋을 다시 만든다.</summary>
        public static Func<IDefinitionDatabase, Result> Save;

        /// <summary>에디터: 덮어쓰기 파일을 비우고 고칠 수 있는 칸을 코드 기본값으로.</summary>
        public static Func<IDefinitionDatabase, Result> ResetToDefaults;

        public static bool CanSave => Save != null;

        /// <summary>지금 바뀐 값을 persistentDataPath/BalanceLab/에 쓴다 (빌드에서 고친 값을 에디터로 옮길 때).</summary>
        public static Result Export(IDefinitionDatabase db)
        {
            try
            {
                var folder = System.IO.Path.Combine(Application.persistentDataPath, "BalanceLab");
                Directory.CreateDirectory(folder);
                var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var overrides = BalanceOverridesJson.Collect(db.Balance);
                var path = System.IO.Path.Combine(folder, "overrides_" + stamp + ".json");
                File.WriteAllText(path, BalanceOverridesJson.Write(overrides));

                var model = BalanceLabModel.For(db);
                var models = model.ChangedModelIds
                    .Select(id => model.TryGetModel(id, out var def) ? def : null)
                    .Where(def => def != null)
                    .ToList();
                if (models.Count > 0)
                {
                    File.WriteAllText(System.IO.Path.Combine(folder, "models_" + stamp + ".json"), BalanceOverridesJson.WriteModels(models));
                }

                return new Result { Fields = overrides.Count, Models = models.Count, Path = path };
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                return new Result { Error = exception.Message };
            }
        }
    }
}
