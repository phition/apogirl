using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using ApoGirl.Core.Balance;
using ApoGirl.Core.Definitions;

namespace ApoGirl.Systems.Content
{
    /// <summary>
    /// 밸런스 표 저장 (Docs/BALANCE_LAB_PROPOSAL.md 7.2): 콘텐츠 JSON의 탐험 모델 숫자만 글자 단위로 바꾼다.
    /// 손으로 맞춘 줄 모양(한 줄짜리 Day·Night)을 지키려고 파일을 다시 직렬화하지 않는다.
    /// 바꾸는 것: Resources 배열, 수치 칸(BalanceLabModel.ModelStats).
    /// </summary>
    public static class ExpeditionModelPatcher
    {
        /// <summary>text 안에서 model.Id 모델을 찾아 고친 글을 돌려준다. 못 찾으면 그대로 (found = false).</summary>
        public static string Patch(string text, ExpeditionModelDef model, out bool found)
        {
            found = false;
            var section = text.IndexOf("\"ExpeditionModels\"", StringComparison.Ordinal);
            if (section < 0)
            {
                return text;
            }

            var arrayStart = text.IndexOf('[', section);
            var arrayEnd = Match(text, arrayStart, '[', ']');
            var id = new Regex("\"Id\"\\s*:\\s*\"" + Regex.Escape(model.Id) + "\"").Match(text, arrayStart, arrayEnd - arrayStart);
            if (!id.Success)
            {
                return text;
            }

            var modelStart = text.LastIndexOf('{', id.Index);
            var modelEnd = Match(text, modelStart, '{', '}');
            var body = text.Substring(modelStart, modelEnd - modelStart + 1);
            body = PatchYield(body, "Day", model.Day);
            body = PatchYield(body, "Night", model.Night);
            found = true;
            return text.Substring(0, modelStart) + body + text.Substring(modelEnd + 1);
        }

        private static string PatchYield(string model, string key, ExpeditionYield yield)
        {
            var match = new Regex("\"" + key + "\"\\s*:\\s*\\{").Match(model);
            if (!match.Success)
            {
                return model;
            }

            var start = match.Index + match.Length - 1;
            var end = Match(model, start, '{', '}');
            var obj = model.Substring(start, end - start + 1);
            obj = PatchResources(obj, yield);
            var defaults = new ExpeditionYield();
            foreach (var name in BalanceLabModel.ModelStats)
            {
                var field = typeof(ExpeditionYield).GetField(name);
                obj = PatchNumber(obj, name, field.GetValue(yield), field.GetValue(defaults));
            }

            return model.Substring(0, start) + obj + model.Substring(end + 1);
        }

        private static string PatchResources(string obj, ExpeditionYield yield)
        {
            var list = yield.Resources.Count == 0
                ? "[]"
                : "[ " + string.Join(", ", yield.Resources.Select(r => "{ \"Resource\": \"" + r.Resource + "\", \"Min\": " + r.Min + ", \"Max\": " + r.Max + " }")) + " ]";
            var match = new Regex("\"Resources\"\\s*:\\s*\\[").Match(obj);
            if (!match.Success)
            {
                return yield.Resources.Count == 0 ? obj : obj.Substring(0, 1) + " \"Resources\": " + list + "," + obj.Substring(1);
            }

            var start = match.Index + match.Length - 1;
            var end = Match(obj, start, '[', ']');
            var old = obj.Substring(start, end - start + 1);
            return Same(old, yield) ? obj : obj.Substring(0, start) + list + obj.Substring(end + 1);
        }

        // 원래 배열과 내용이 같으면 손대지 않는다 (줄 모양 그대로)
        private static bool Same(string oldArray, ExpeditionYield yield)
        {
            var entries = Regex.Matches(oldArray, "\"Resource\"\\s*:\\s*\"(\\w+)\"\\s*,\\s*\"Min\"\\s*:\\s*(-?\\d+)\\s*,\\s*\"Max\"\\s*:\\s*(-?\\d+)");
            if (entries.Count != yield.Resources.Count)
            {
                return false;
            }

            for (var i = 0; i < entries.Count; i++)
            {
                var r = yield.Resources[i];
                var m = entries[i];
                if (m.Groups[1].Value != r.Resource.ToString() || int.Parse(m.Groups[2].Value) != r.Min || int.Parse(m.Groups[3].Value) != r.Max)
                {
                    return false;
                }
            }

            return true;
        }

        private static string PatchNumber(string obj, string name, object value, object defaultValue)
        {
            var match = new Regex("(\"" + name + "\"\\s*:\\s*)(-?[0-9][0-9.eE+-]*)").Match(obj);
            if (match.Success)
            {
                var old = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                if (Math.Abs(old - Convert.ToDouble(value, CultureInfo.InvariantCulture)) < 1e-6)
                {
                    return obj;
                }

                var number = match.Groups[2];
                return obj.Substring(0, number.Index) + Format(value) + obj.Substring(number.Index + number.Length);
            }

            if (Equals(value, defaultValue))
            {
                return obj;
            }

            // 없던 칸: 닫는 괄호 앞에 붙인다
            var close = obj.LastIndexOf('}');
            var head = obj.Substring(0, close).TrimEnd();
            return head + ", \"" + name + "\": " + Format(value) + " " + obj.Substring(close);
        }

        private static string Format(object value)
        {
            return value is float f ? f.ToString("0.0###", CultureInfo.InvariantCulture) : Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        /// <summary>여는 괄호 위치에서 짝이 되는 닫는 괄호 위치 (문자열 안은 건너뛴다).</summary>
        private static int Match(string text, int open, char openChar, char closeChar)
        {
            var depth = 0;
            var inString = false;
            for (var i = open; i < text.Length; i++)
            {
                var c = text[i];
                if (inString)
                {
                    if (c == '\\') i++;
                    else if (c == '"') inString = false;
                    continue;
                }

                if (c == '"') inString = true;
                else if (c == openChar) depth++;
                else if (c == closeChar && --depth == 0) return i;
            }

            throw new FormatException("괄호 짝이 맞지 않는다");
        }
    }
}
