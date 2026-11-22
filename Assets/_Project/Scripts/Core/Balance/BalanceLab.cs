using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.State;

namespace ApoGirl.Core.Balance
{
    // 밸런스 표 (Docs/BALANCE_LAB_PROPOSAL.md): 게임이 쓰는 정의 데이터 위에 "칸"을 만들어 보고 고친다.
    // 칸은 지금 값 읽기·쓰기와 처음 열었을 때의 원래 값을 가진다. 화면은 Presentation(BalanceLabView)에 있다.

    /// <summary>바꾼 값이 게임에 먹는 때 (기획서 5절).</summary>
    public enum BalanceWhen
    {
        Unknown,
        Now,
        NextWeek,
        NextExpedition,
        NewGame,
    }

    /// <summary>표의 한 칸 (정수·실수·참거짓 하나).</summary>
    public sealed class BalanceCell
    {
        private readonly Func<object> _get;
        private readonly Action<object> _set;

        public BalanceCell(string key, Type valueType, Func<object> get, Action<object> set)
        {
            Key = key;
            ValueType = valueType;
            _get = get;
            _set = set;
            Baseline = get();
        }

        /// <summary>고유 키: 밸런스 필드는 필드 이름, 탐험 모델은 "Model/{지역}/{Day|Night}/{칸}", 난이도는 "Difficulty/{ID}/{필드}".</summary>
        public string Key { get; }

        public Type ValueType { get; }

        /// <summary>표를 처음 연 때의 값.</summary>
        public object Baseline { get; private set; }

        public object Value => _get();

        public bool Changed => !Equals(_get(), Baseline);

        /// <summary>휠 한 칸에 바뀌는 양 (0이면 종류에 맞게 정한다).</summary>
        public double Step { get; set; }

        public double Min { get; set; } = double.NaN;

        public double Max { get; set; } = double.NaN;

        public BalanceWhen When { get; set; }

        public bool IsBool => ValueType == typeof(bool);

        public bool IsInteger => ValueType == typeof(int);

        public string Text => Format(_get());

        public string BaselineText => Format(Baseline);

        /// <summary>글자로 값을 넣는다. 범위 밖이면 범위 끝으로 맞춘다.</summary>
        public bool TrySet(string text, out string error)
        {
            error = null;
            text = (text ?? "").Trim();
            if (IsBool)
            {
                if (bool.TryParse(text, out var flag) || TryBoolWord(text, out flag))
                {
                    _set(flag);
                    return true;
                }

                error = "true / false";
                return false;
            }

            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            {
                error = "숫자가 아니다";
                return false;
            }

            SetNumber(number);
            return true;
        }

        /// <summary>단계만큼 올리거나 내린다 (참거짓은 뒤집는다).</summary>
        public void Nudge(int steps)
        {
            if (IsBool)
            {
                _set(!(bool)_get());
                return;
            }

            SetNumber(Convert.ToDouble(_get(), CultureInfo.InvariantCulture) + steps * EffectiveStep);
        }

        /// <summary>지금 값에 배율을 곱한다 (정수는 반올림, 최소 1칸은 움직인다).</summary>
        public void Scale(double factor)
        {
            if (IsBool)
            {
                return;
            }

            var current = Convert.ToDouble(_get(), CultureInfo.InvariantCulture);
            var next = current * factor;
            if (IsInteger && current != 0 && Math.Round(next) == current)
            {
                next = current + Math.Sign(factor - 1) * Math.Sign(current);
            }

            SetNumber(next);
        }

        public void Revert() => _set(Baseline);

        /// <summary>지금 값을 새 원래 값으로 (저장한 뒤).</summary>
        public void Rebase() => Baseline = _get();

        public double EffectiveStep
        {
            get
            {
                if (Step > 0) return Step;
                if (IsInteger) return 1;
                var current = Math.Abs(Convert.ToDouble(_get(), CultureInfo.InvariantCulture));
                return current >= 10 ? 1 : current >= 1 ? 0.1 : 0.01;
            }
        }

        private void SetNumber(double number)
        {
            if (!double.IsNaN(Min)) number = Math.Max(Min, number);
            if (!double.IsNaN(Max)) number = Math.Min(Max, number);
            if (IsInteger)
            {
                _set((int)Math.Round(number, MidpointRounding.AwayFromZero));
            }
            else if (ValueType == typeof(float))
            {
                _set((float)Math.Round(number, 4));
            }
            else
            {
                _set(Math.Round(number, 4));
            }
        }

        private static bool TryBoolWord(string text, out bool value)
        {
            value = text == "1" || text == "예" || text == "켬" || text == "on";
            return value || text == "0" || text == "아니오" || text == "끔" || text == "off";
        }

        public static string Format(object value)
        {
            switch (value)
            {
                case null: return "-";
                case float f: return f.ToString("0.####", CultureInfo.InvariantCulture);
                case double d: return d.ToString("0.####", CultureInfo.InvariantCulture);
                case bool b: return b ? "true" : "false";
                default: return Convert.ToString(value, CultureInfo.InvariantCulture);
            }
        }
    }

    /// <summary>BalanceConfig의 필드 목록 (선언 순서).</summary>
    public static class BalanceFields
    {
        private static FieldInfo[] _all;

        public static IReadOnlyList<FieldInfo> All => _all ??= typeof(BalanceConfig)
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .OrderBy(f => f.MetadataToken)
            .ToArray();

        public static FieldInfo Find(string name) => All.FirstOrDefault(f => f.Name == name);

        public static bool IsEditable(Type type) => type == typeof(int) || type == typeof(float) || type == typeof(double) || type == typeof(bool);

        /// <summary>표 안에서 고칠 수 없는 필드(배열·표·글자)의 한 줄 요약.</summary>
        public static string Summary(object value)
        {
            switch (value)
            {
                case null: return "-";
                case string s: return "\"" + s + "\"";
                case System.Collections.ICollection collection: return "[" + collection.Count + "개] " + Preview(collection);
                default: return value.GetType().Name;
            }
        }

        private static string Preview(System.Collections.IEnumerable items)
        {
            var parts = new List<string>();
            foreach (var item in items)
            {
                if (parts.Count == 6)
                {
                    parts.Add("…");
                    break;
                }

                parts.Add(item == null ? "-" : item is float || item is double || item is int || item is string ? BalanceCell.Format(item) : ItemName(item));
            }

            return string.Join(", ", parts);
        }

        // 표 원소는 Id·Resource 같은 이름 필드가 있으면 그것을 보인다
        private static string ItemName(object item)
        {
            var type = item.GetType();
            foreach (var name in new[] { "Id", "Resource", "Season", "Key", "ItemId", "Name" })
            {
                var field = type.GetField(name);
                if (field != null)
                {
                    return Convert.ToString(field.GetValue(item), CultureInfo.InvariantCulture);
                }
            }

            return type.Name;
        }
    }

    /// <summary>
    /// 한 판의 정의 데이터 위에 만든 칸 모음. 같은 DB면 같은 모음을 돌려주어, 창을 닫았다 열어도 원래 값이 남는다.
    /// </summary>
    public sealed class BalanceLabModel
    {
        /// <summary>탐험 모델 행렬의 칸 (자원은 최소·최대 두 칸).</summary>
        public static readonly string[] ModelResources = { "Food", "Water", "Medicine", "Materials", "Fuel", "Battery" };

        public static readonly string[] ModelStats = { "BaseDownChance", "Fatigue", "StressMin", "StressMax", "EncounterChance" };

        public static readonly string[] DifficultyFields = { "Consumption", "Harm", "HumanThreat", "Infection", "Yield", "FoodYield", "WaterYield", "MedicineYield" };

        private static readonly ConditionalWeakTable<IDefinitionDatabase, BalanceLabModel> Models = new ConditionalWeakTable<IDefinitionDatabase, BalanceLabModel>();

        private readonly IDefinitionDatabase _db;
        private readonly Dictionary<string, BalanceCell> _cells = new Dictionary<string, BalanceCell>();
        private List<string> _modelIds;

        private BalanceLabModel(IDefinitionDatabase db)
        {
            _db = db;
        }

        public static BalanceLabModel For(IDefinitionDatabase db) => Models.GetValue(db, d => new BalanceLabModel(d));

        public BalanceConfig Balance => _db.Balance;

        /// <summary>바뀐 칸 (처음 만든 순서).</summary>
        public IEnumerable<BalanceCell> Changed => _cells.Values.Where(c => c.Changed);

        public void RevertAll()
        {
            foreach (var cell in _cells.Values)
            {
                cell.Revert();
            }
        }

        /// <summary>저장한 뒤: 지금 값이 새 원래 값이 된다.</summary>
        public void RebaseAll()
        {
            foreach (var cell in _cells.Values)
            {
                cell.Rebase();
            }
        }

        /// <summary>바뀐 칸이 있는 탐험 모델 지역.</summary>
        public IEnumerable<string> ChangedModelIds => Changed
            .Where(c => c.Key.StartsWith("Model/", StringComparison.Ordinal))
            .Select(c => c.Key.Split('/')[1])
            .Distinct();

        public bool TryGetModel(string id, out ExpeditionModelDef model) => _db.TryGetExpeditionModel(id, out model);

        /// <summary>BalanceConfig 필드 하나의 칸 (고칠 수 없는 종류면 null).</summary>
        public BalanceCell Field(string name)
        {
            if (_cells.TryGetValue(name, out var cell))
            {
                return cell;
            }

            var field = BalanceFields.Find(name);
            if (field == null || !BalanceFields.IsEditable(field.FieldType))
            {
                return null;
            }

            return Remember(new BalanceCell(name, field.FieldType, () => field.GetValue(_db.Balance), v => field.SetValue(_db.Balance, v)));
        }

        /// <summary>탐험 모델이 있는 지역 (행동 목록에 나오는 순서).</summary>
        public IReadOnlyList<string> ModelIds => _modelIds ??= _db.Actions
            .Where(a => a.IsExpedition && !string.IsNullOrEmpty(a.LocationId))
            .Select(a => a.LocationId)
            .Distinct()
            .Where(id => _db.TryGetExpeditionModel(id, out _))
            .ToList();

        /// <summary>
        /// 탐험 모델 칸. column은 자원("Food.Min"·"Food.Max") 또는 ModelStats 이름.
        /// 그 지역·시간대에 없는 자원의 칸을 고치면 범위를 새로 붙인다 (0~0이면 빈 범위와 같다).
        /// </summary>
        public BalanceCell ModelCell(string modelId, bool night, string column)
        {
            var key = "Model/" + modelId + "/" + (night ? "Night" : "Day") + "/" + column;
            if (_cells.TryGetValue(key, out var cell))
            {
                return cell;
            }

            if (!_db.TryGetExpeditionModel(modelId, out var model))
            {
                return null;
            }

            ExpeditionYield Yield() => night ? model.Night : model.Day;
            var dot = column.IndexOf('.');
            if (dot > 0)
            {
                if (!Enum.TryParse<ResourceType>(column.Substring(0, dot), out var resource))
                {
                    return null;
                }

                var max = column.Substring(dot + 1) == "Max";
                ResourceRange Range(bool create)
                {
                    var range = Yield().Resources.FirstOrDefault(r => r.Resource == resource);
                    if (range == null && create)
                    {
                        range = new ResourceRange(resource, 0, 0);
                        Yield().Resources.Add(range);
                    }

                    return range;
                }

                // 처음에 없던 자원은 0~0으로 돌아오면 범위를 다시 뺀다 (빈 범위와 "없음"이 다르게 읽히는 규칙이 있다: OutingHaul)
                var existed = Range(false) != null;
                cell = new BalanceCell(key, typeof(int),
                    () => Range(false) is { } r ? (max ? r.Max : r.Min) : 0,
                    v =>
                    {
                        var r = Range(true);
                        if (max) r.Max = (int)v;
                        else r.Min = (int)v;
                        if (!existed && r.Min == 0 && r.Max == 0)
                        {
                            Yield().Resources.Remove(r);
                        }
                    }) { Min = 0, When = BalanceWhen.Now };
                return Remember(cell);
            }

            var field = typeof(ExpeditionYield).GetField(column);
            if (field == null)
            {
                return null;
            }

            cell = new BalanceCell(key, field.FieldType, () => field.GetValue(Yield()), v => field.SetValue(Yield(), v))
            {
                Min = 0,
                Max = field.FieldType == typeof(float) ? 1 : double.NaN,
                When = BalanceWhen.Now,
            };
            return Remember(cell);
        }

        public IEnumerable<DifficultyDef> Difficulties => _db.Balance.Difficulties ?? new DifficultyDef[0];

        public BalanceCell DifficultyCell(string difficultyId, string fieldName)
        {
            var key = "Difficulty/" + difficultyId + "/" + fieldName;
            if (_cells.TryGetValue(key, out var cell))
            {
                return cell;
            }

            var field = typeof(DifficultyDef).GetField(fieldName);
            if (field == null || !BalanceFields.IsEditable(field.FieldType))
            {
                return null;
            }

            DifficultyDef Def() => Difficulties.FirstOrDefault(d => d.Id == difficultyId);
            if (Def() == null)
            {
                return null;
            }

            return Remember(new BalanceCell(key, field.FieldType, () => field.GetValue(Def()), v => field.SetValue(Def(), v))
            {
                Min = 0,
                Step = 0.05,
                When = BalanceWhen.Now,
            });
        }

        private BalanceCell Remember(BalanceCell cell)
        {
            _cells[cell.Key] = cell;
            return cell;
        }
    }
}
