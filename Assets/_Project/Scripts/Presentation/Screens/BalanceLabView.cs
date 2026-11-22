using System;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Balance;
using ApoGirl.Core.Rules;
using ApoGirl.Presentation.Text;
using ApoGirl.Systems.Content;
using ApoGirl.Systems.Session;
using UnityEngine;
using UnityEngine.UIElements;

namespace ApoGirl.Presentation.Screens
{
    /// <summary>
    /// 밸런스 표 (Docs/BALANCE_LAB_PROPOSAL.md L1·L2): 중요 수치를 탭별 표로 보고 그 자리에서 고친다.
    /// 고친 값은 지금 판의 정의 데이터에 바로 들어간다 (저장·봇 실행은 다음 단계).
    /// </summary>
    internal sealed class BalanceLabView
    {
        private readonly GameSession _session;
        private readonly BalanceLabModel _model;
        private readonly BalanceLabLayout _layout;
        private readonly BalanceSections _sections;
        private readonly VisualElement _overlay;
        private readonly VisualElement _tabBar;
        private readonly Label _hint;
        private readonly TextField _search;
        private readonly ScrollView _body;
        private readonly Label _changed;
        private readonly List<Action> _refreshers = new List<Action>();
        private readonly Action _onClosed;
        private BalanceLabTab _tab;

        public BalanceLabView(VisualElement root, GameSession session, Action onClosed)
        {
            _session = session;
            _onClosed = onClosed;
            _model = BalanceLabModel.For(session.Definitions);
            _layout = BalanceLabJson.ParseLayout(Resources.Load<TextAsset>(BalanceLabJson.LayoutResource)?.text);
            _sections = BalanceLabJson.ParseSections(Resources.Load<TextAsset>(BalanceLabJson.SectionsResource)?.text);

            _overlay = new VisualElement { name = "balance-lab" };
            _overlay.AddToClassList("lab-overlay");
            _overlay.AddToClassList("hidden");
            var window = new VisualElement();
            window.AddToClassList("lab-window");
            _overlay.Add(window);

            var header = new VisualElement();
            header.AddToClassList("lab-header");
            var title = new Label(Loc.T("lab.title"));
            title.AddToClassList("lab-title");
            header.Add(title);
            _search = new TextField { label = Loc.T("lab.search") };
            _search.AddToClassList("lab-search");
            _search.RegisterValueChangedCallback(_ => Build());
            header.Add(_search);
            header.Add(SmallButton(Loc.T("lab.revert_all"), () =>
            {
                _model.RevertAll();
                Changed();
            }));
            header.Add(SmallButton(Loc.T("lab.close"), Hide));
            window.Add(header);

            _tabBar = new VisualElement();
            _tabBar.AddToClassList("lab-tabs");
            window.Add(_tabBar);
            foreach (var tab in _layout.Tabs)
            {
                var button = SmallButton(tab.Title, () => Select(tab));
                button.userData = tab;
                button.AddToClassList("lab-tab");
                _tabBar.Add(button);
            }

            _hint = new Label();
            _hint.AddToClassList("lab-hint");
            window.Add(_hint);

            _body = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            _body.AddToClassList("lab-body");
            window.Add(_body);

            _changed = new Label();
            _changed.AddToClassList("lab-changed");
            window.Add(_changed);
            var help = new Label(Loc.T("lab.help"));
            help.AddToClassList("lab-help");
            window.Add(help);

            root.Add(_overlay);
            if (_layout.Tabs.Count > 0)
            {
                _tab = _layout.Tabs[0];
            }
        }

        public bool IsOpen => !_overlay.ClassListContains("hidden");

        public void Toggle()
        {
            if (IsOpen) Hide();
            else Show();
        }

        public void Show()
        {
            _overlay.RemoveFromClassList("hidden");
            _overlay.BringToFront();
            Build();
        }

        public void Hide()
        {
            _overlay.AddToClassList("hidden");
            _onClosed?.Invoke(); // 고친 값으로 허브 표시를 다시 그린다
        }

        private void Select(BalanceLabTab tab)
        {
            _tab = tab;
            _search.SetValueWithoutNotify("");
            Build();
        }

        private void Build()
        {
            _body.Clear();
            _refreshers.Clear();
            foreach (var button in _tabBar.Children())
            {
                button.EnableInClassList("selected", button.userData == _tab);
            }

            if (_tab == null)
            {
                return;
            }

            _hint.text = _tab.Hint;
            switch (_tab.Kind)
            {
                case "Expedition":
                    BuildExpedition();
                    break;
                case "Difficulty":
                    BuildDifficulty();
                    break;
                case "All":
                    BuildAll();
                    break;
                default:
                    BuildFields(_tab.Rows);
                    break;
            }

            Changed();
        }

        // ── Fields: 줄마다 이름·원래·지금·단위·언제부터·설명 ──

        private void BuildFields(IEnumerable<BalanceLabRow> rows)
        {
            _body.Add(HeaderRow(Loc.T("lab.col.name"), Loc.T("lab.col.baseline"), Loc.T("lab.col.value"), Loc.T("lab.col.unit"), Loc.T("lab.col.when"), Loc.T("lab.col.note")));
            var group = (string)null;
            foreach (var row in rows.Where(Matches))
            {
                if (row.Group != group && !string.IsNullOrEmpty(row.Group))
                {
                    group = row.Group;
                    var heading = new Label(group);
                    heading.AddToClassList("lab-group");
                    _body.Add(heading);
                }

                _body.Add(FieldRow(row));
            }
        }

        private bool Matches(BalanceLabRow row)
        {
            var query = _search.value?.Trim();
            return string.IsNullOrEmpty(query)
                   || row.Field.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                   || (row.Name ?? "").Contains(query)
                   || (row.Group ?? "").Contains(query);
        }

        private VisualElement FieldRow(BalanceLabRow row)
        {
            var line = new VisualElement();
            line.AddToClassList("lab-row");
            line.Add(Cell(string.IsNullOrEmpty(row.Name) ? row.Field : row.Name, "lab-c-name"));
            var cell = _model.Field(row.Field);
            if (cell == null)
            {
                var field = BalanceFields.Find(row.Field);
                line.Add(Cell("", "lab-c-base"));
                line.Add(Cell(field == null ? "(없는 필드)" : BalanceFields.Summary(field.GetValue(_model.Balance)), "lab-c-summary"));
            }
            else
            {
                Apply(cell, row);
                var baseline = Cell(cell.BaselineText, "lab-c-base");
                line.Add(baseline);
                line.Add(Editor(cell, line));
            }

            line.Add(Cell(row.Unit, "lab-c-unit"));
            line.Add(Cell(WhenText(cell?.When ?? BalanceWhen.Unknown), "lab-c-when"));
            line.Add(Cell(string.IsNullOrEmpty(row.Note) ? row.Field : row.Note + " · " + row.Field, "lab-c-note"));
            return line;
        }

        private static void Apply(BalanceCell cell, BalanceLabRow row)
        {
            if (row.Step > 0) cell.Step = row.Step;
            if (row.Min.HasValue) cell.Min = row.Min.Value;
            if (row.Max.HasValue) cell.Max = row.Max.Value;
            if (Enum.TryParse<BalanceWhen>(row.When, out var when)) cell.When = when;
        }

        // ── Expedition: 지역 × 낮·밤 ──

        private void BuildExpedition()
        {
            var header = new List<string> { Loc.T("lab.col.region"), Loc.T("lab.col.time") };
            header.AddRange(BalanceLabModel.ModelResources.Select(r => Loc.T("lab.res." + r)));
            header.AddRange(BalanceLabModel.ModelStats.Select(s => Loc.T("lab.stat." + s)));
            var head = new VisualElement();
            head.AddToClassList("lab-row");
            head.AddToClassList("lab-head");
            head.Add(Cell(header[0], "lab-c-region"));
            head.Add(Cell(header[1], "lab-c-time"));
            for (var i = 2; i < header.Count; i++)
            {
                head.Add(Cell(header[i], i < 2 + BalanceLabModel.ModelResources.Length ? "lab-c-range" : "lab-c-num"));
            }

            _body.Add(head);
            foreach (var id in _model.ModelIds)
            {
                var nameKey = "palette.exp." + id;
                var name = Loc.T(nameKey);
                name = name == nameKey ? id : name.Replace(" 탐험", "");
                if (!string.IsNullOrEmpty(_search.value) && !name.Contains(_search.value.Trim()) && id.IndexOf(_search.value.Trim(), StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                foreach (var night in new[] { false, true })
                {
                    var line = new VisualElement();
                    line.AddToClassList("lab-row");
                    line.AddToClassList(night ? "lab-night" : "lab-day");
                    line.Add(Cell(night ? "" : name, "lab-c-region"));
                    line.Add(Cell(Loc.T(night ? "lab.night" : "lab.day"), "lab-c-time"));
                    foreach (var resource in BalanceLabModel.ModelResources)
                    {
                        var range = new VisualElement();
                        range.AddToClassList("lab-c-range");
                        range.Add(Editor(_model.ModelCell(id, night, resource + ".Min"), line, "lab-mini"));
                        range.Add(Cell("~", "lab-tilde"));
                        range.Add(Editor(_model.ModelCell(id, night, resource + ".Max"), line, "lab-mini"));
                        line.Add(range);
                    }

                    foreach (var stat in BalanceLabModel.ModelStats)
                    {
                        line.Add(Editor(_model.ModelCell(id, night, stat), line, "lab-c-num"));
                    }

                    _body.Add(line);
                }
            }
        }

        // ── Difficulty: 난이도 × 배율 ──

        private void BuildDifficulty()
        {
            var current = DifficultyRules.Get(_session.State, _model.Balance);
            _hint.text = _tab.Hint + "  " + Loc.F("lab.difficulty_current", Loc.T("difficulty." + current.Id + ".name"));
            var head = new VisualElement();
            head.AddToClassList("lab-row");
            head.AddToClassList("lab-head");
            head.Add(Cell(Loc.T("lab.col.difficulty"), "lab-c-region"));
            foreach (var field in BalanceLabModel.DifficultyFields)
            {
                head.Add(Cell(Loc.T("lab.diff." + field), "lab-c-num"));
            }

            _body.Add(head);
            foreach (var difficulty in _model.Difficulties)
            {
                var line = new VisualElement();
                line.AddToClassList("lab-row");
                line.EnableInClassList("lab-current", difficulty.Id == current.Id);
                line.Add(Cell(Loc.T("difficulty." + difficulty.Id + ".name"), "lab-c-region"));
                foreach (var field in BalanceLabModel.DifficultyFields)
                {
                    var cell = _model.DifficultyCell(difficulty.Id, field);
                    line.Add(cell == null ? Cell("-", "lab-c-num") : Editor(cell, line, "lab-c-num"));
                }

                _body.Add(line);
            }
        }

        // ── All: BalanceConfig 전체, 절 순서 ──

        private void BuildAll()
        {
            var named = _layout.Tabs.SelectMany(t => t.Rows).GroupBy(r => r.Field).ToDictionary(g => g.Key, g => g.First());
            var listed = new HashSet<string>();
            var groups = new List<(string Title, List<string> Fields)>();
            foreach (var section in _sections.Sections)
            {
                groups.Add((section.Title, section.Fields.Where(f => BalanceFields.Find(f) != null).ToList()));
                listed.UnionWith(section.Fields);
            }

            var rest = BalanceFields.All.Select(f => f.Name).Where(n => !listed.Contains(n)).ToList();
            if (rest.Count > 0)
            {
                groups.Add((Loc.T("lab.other_section"), rest));
            }

            var rows = new List<BalanceLabRow>();
            foreach (var (title, fields) in groups)
            {
                foreach (var field in fields)
                {
                    var row = named.TryGetValue(field, out var known)
                        ? new BalanceLabRow { Field = field, Group = title, Name = known.Name, Unit = known.Unit, When = known.When, Step = known.Step, Min = known.Min, Max = known.Max }
                        : new BalanceLabRow { Field = field, Group = title, Name = field };
                    rows.Add(row);
                }
            }

            BuildFields(rows);
        }

        // ── 칸 ──

        /// <summary>고칠 수 있는 칸: 숫자는 글자 칸(Enter로 적용, 휠로 단계), 참거짓은 토글.</summary>
        private VisualElement Editor(BalanceCell cell, VisualElement line, string extraClass = "lab-c-value")
        {
            if (cell == null)
            {
                return Cell("-", extraClass);
            }

            VisualElement editor;
            Action refresh;
            if (cell.IsBool)
            {
                var toggle = new Toggle();
                toggle.RegisterValueChangedCallback(e =>
                {
                    cell.TrySet(e.newValue ? "true" : "false", out _);
                    Changed();
                });
                editor = toggle;
                refresh = () => toggle.SetValueWithoutNotify((bool)cell.Value);
            }
            else
            {
                var text = new TextField { isDelayed = true };
                text.RegisterValueChangedCallback(e =>
                {
                    if (!cell.TrySet(e.newValue, out _))
                    {
                        text.SetValueWithoutNotify(cell.Text);
                    }

                    Changed();
                });
                text.RegisterCallback<WheelEvent>(e =>
                {
                    var steps = (e.delta.y < 0 ? 1 : -1) * (e.shiftKey ? 10 : 1);
                    cell.Nudge(steps);
                    Changed();
                    e.StopPropagation();
                }, TrickleDown.TrickleDown);
                editor = text;
                refresh = () => text.SetValueWithoutNotify(cell.Text);
            }

            editor.AddToClassList("lab-edit");
            editor.AddToClassList(extraClass);
            editor.tooltip = cell.Key + " · " + Loc.T("lab.col.baseline") + " " + cell.BaselineText;
            void Paint()
            {
                refresh();
                editor.EnableInClassList("lab-dirty", cell.Changed);
                line.EnableInClassList("lab-row-dirty", line.Query(className: "lab-dirty").ToList().Count > 0);
            }

            Paint();
            _refreshers.Add(Paint);
            return editor;
        }

        private void Changed()
        {
            foreach (var refresh in _refreshers)
            {
                refresh();
            }

            var changed = _model.Changed.ToList();
            _changed.text = changed.Count == 0
                ? Loc.T("lab.changed_none")
                : Loc.F("lab.changed", changed.Count) + " · " + string.Join(" · ", changed.Take(12).Select(c => c.Key + " " + c.BaselineText + "→" + c.Text)) + (changed.Count > 12 ? " …" : "");
            _changed.EnableInClassList("lab-changed-on", changed.Count > 0);
        }

        private static string WhenText(BalanceWhen when) => Loc.T("lab.when." + when);

        private static VisualElement HeaderRow(params string[] columns)
        {
            var classes = new[] { "lab-c-name", "lab-c-base", "lab-c-value", "lab-c-unit", "lab-c-when", "lab-c-note" };
            var row = new VisualElement();
            row.AddToClassList("lab-row");
            row.AddToClassList("lab-head");
            for (var i = 0; i < columns.Length; i++)
            {
                row.Add(Cell(columns[i], classes[i]));
            }

            return row;
        }

        private static Label Cell(string text, string cssClass)
        {
            var label = new Label(text);
            label.AddToClassList("lab-cell");
            label.AddToClassList(cssClass);
            return label;
        }

        private static Button SmallButton(string text, Action onClick)
        {
            var button = new Button(onClick) { text = text };
            button.AddToClassList("small-button");
            return button;
        }
    }
}
