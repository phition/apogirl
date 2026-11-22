using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ApoGirl.Core.Balance;
using ApoGirl.Core.Definitions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ApoGirl.Systems.Content
{
    /// <summary>
    /// 밸런스 덮어쓰기 파일 (Docs/BALANCE_LAB_PROPOSAL.md 7.1, D-1 A안).
    /// BalanceConfig.cs 기본값과 다른 수치만 적는다. Sync Balance Asset이 기본값 위에 얹어 에셋을 만든다.
    /// 지금은 고칠 수 있는 칸(정수·실수·참거짓)과 난이도 배율만 다룬다.
    /// </summary>
    public static class BalanceOverridesJson
    {
        public const string DefaultPath = "Assets/_Project/Balance/balance_overrides.json";

        public sealed class Overrides
        {
            /// <summary>파일 맨 위 안내 (읽을 때는 무시).</summary>
            [JsonProperty("_설명")]
            public string Description = "";

            /// <summary>BalanceConfig 필드 이름 → 값.</summary>
            public Dictionary<string, JToken> Fields = new Dictionary<string, JToken>();

            /// <summary>난이도 ID → (DifficultyDef 필드 → 값).</summary>
            public Dictionary<string, Dictionary<string, JToken>> Difficulties = new Dictionary<string, Dictionary<string, JToken>>();

            [JsonIgnore]
            public int Count => Fields.Count + Difficulties.Sum(d => d.Value.Count);
        }

        /// <summary>지금 값에서 코드 기본값과 다른 것만 모은다.</summary>
        public static Overrides Collect(BalanceConfig live)
        {
            var defaults = new BalanceConfig();
            var result = new Overrides();
            foreach (var field in BalanceFields.All.Where(f => BalanceFields.IsEditable(f.FieldType)))
            {
                var value = field.GetValue(live);
                if (!Equals(value, field.GetValue(defaults)))
                {
                    result.Fields[field.Name] = JToken.FromObject(value);
                }
            }

            foreach (var difficulty in live.Difficulties ?? new DifficultyDef[0])
            {
                var basis = defaults.Difficulties?.FirstOrDefault(d => d.Id == difficulty.Id);
                if (basis == null)
                {
                    continue; // 코드에 없는 난이도는 덮어쓰기로 만들지 않는다
                }

                foreach (var field in typeof(DifficultyDef).GetFields().Where(f => BalanceFields.IsEditable(f.FieldType)))
                {
                    var value = field.GetValue(difficulty);
                    if (Equals(value, field.GetValue(basis)))
                    {
                        continue;
                    }

                    if (!result.Difficulties.TryGetValue(difficulty.Id, out var fields))
                    {
                        result.Difficulties[difficulty.Id] = fields = new Dictionary<string, JToken>();
                    }

                    fields[field.Name] = JToken.FromObject(value);
                }
            }

            return result;
        }

        /// <summary>덮어쓰기를 설정에 얹는다. 없어진 필드·난이도나 맞지 않는 값은 경고로 돌려준다.</summary>
        public static List<string> Apply(BalanceConfig config, Overrides overrides)
        {
            var warnings = new List<string>();
            if (overrides == null)
            {
                return warnings;
            }

            foreach (var pair in overrides.Fields)
            {
                var field = BalanceFields.Find(pair.Key);
                if (field == null || !BalanceFields.IsEditable(field.FieldType))
                {
                    warnings.Add("없는 필드: " + pair.Key);
                    continue;
                }

                if (!TrySet(field, config, pair.Value))
                {
                    warnings.Add("값이 맞지 않음: " + pair.Key + " = " + pair.Value);
                }
            }

            foreach (var difficulty in overrides.Difficulties)
            {
                var def = config.Difficulties?.FirstOrDefault(d => d.Id == difficulty.Key);
                if (def == null)
                {
                    warnings.Add("없는 난이도: " + difficulty.Key);
                    continue;
                }

                foreach (var pair in difficulty.Value)
                {
                    var field = typeof(DifficultyDef).GetField(pair.Key);
                    if (field == null || !BalanceFields.IsEditable(field.FieldType) || !TrySet(field, def, pair.Value))
                    {
                        warnings.Add("난이도 값 오류: " + difficulty.Key + "." + pair.Key);
                    }
                }
            }

            return warnings;
        }

        /// <summary>고칠 수 있는 칸만 코드 기본값으로 되돌린다 (배열·표는 그대로).</summary>
        public static void ResetEditable(BalanceConfig live)
        {
            var defaults = new BalanceConfig();
            foreach (var field in BalanceFields.All.Where(f => BalanceFields.IsEditable(f.FieldType)))
            {
                field.SetValue(live, field.GetValue(defaults));
            }

            foreach (var difficulty in live.Difficulties ?? new DifficultyDef[0])
            {
                var basis = defaults.Difficulties?.FirstOrDefault(d => d.Id == difficulty.Id);
                if (basis == null)
                {
                    continue;
                }

                foreach (var field in typeof(DifficultyDef).GetFields().Where(f => BalanceFields.IsEditable(f.FieldType)))
                {
                    field.SetValue(difficulty, field.GetValue(basis));
                }
            }
        }

        private static bool TrySet(System.Reflection.FieldInfo field, object target, JToken value)
        {
            try
            {
                field.SetValue(target, Convert.ChangeType(value.ToObject<object>(), field.FieldType, CultureInfo.InvariantCulture));
                return true;
            }
            catch (Exception exception) when (exception is FormatException || exception is InvalidCastException || exception is OverflowException)
            {
                return false;
            }
        }

        public static Overrides Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new Overrides();
            }

            return JsonConvert.DeserializeObject<Overrides>(json, ContentJson.Settings) ?? new Overrides();
        }

        /// <summary>필드는 BalanceConfig 선언 순서로 적는다 (git diff가 읽기 쉽게).</summary>
        public static string Write(Overrides overrides)
        {
            var order = BalanceFields.All.Select((f, i) => (f.Name, i)).ToDictionary(p => p.Name, p => p.i);
            var root = new JObject
            {
                ["_설명"] = "밸런스 표가 쓰는 파일: BalanceConfig.cs 기본값과 다른 수치만. Sync Balance Asset이 기본값 위에 얹는다 (Docs/BALANCE_LAB_PROPOSAL.md).",
                ["Fields"] = new JObject(overrides.Fields.OrderBy(p => order.TryGetValue(p.Key, out var i) ? i : int.MaxValue).Select(p => new JProperty(p.Key, p.Value))),
                ["Difficulties"] = new JObject(overrides.Difficulties.OrderBy(p => p.Key).Select(p => new JProperty(p.Key, new JObject(p.Value.Select(f => new JProperty(f.Key, f.Value)))))),
            };
            return root.ToString(Formatting.Indented).Replace("\r\n", "\n") + "\n";
        }

        /// <summary>내보내기: 고친 탐험 모델을 콘텐츠 JSON과 같은 모양({ "ExpeditionModels": [...] })으로.</summary>
        public static string WriteModels(IEnumerable<ExpeditionModelDef> models)
        {
            return JsonConvert.SerializeObject(new { ExpeditionModels = models }, Formatting.Indented, new Newtonsoft.Json.Converters.StringEnumConverter()).Replace("\r\n", "\n") + "\n";
        }

        public static Overrides Load(string path = DefaultPath)
        {
            return File.Exists(path) ? Parse(File.ReadAllText(path)) : new Overrides();
        }
    }
}
