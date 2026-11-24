using System;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Events;
using ApoGirl.Core.Rules;
using ApoGirl.Core.State;
using ApoGirl.Presentation.Art;
using ApoGirl.Presentation.Text;
using ApoGirl.Systems.Session;
using UnityEngine.UIElements;

namespace ApoGirl.Presentation.Screens
{
    /// <summary>
    /// 3겹 설계도 (제작 P7, Docs/CRAFTING_PROPOSAL.md 2.1절): 제작 탭 머리의 "설계도" 단추로 연다.
    /// 줄(작업대·정수·가방·무기·보호구·빛·허수아비)마다 단계 순으로 칸을 화살표로 잇고, 칸마다 아이콘·이름·단계·상태를 쓴다.
    /// 지음 / 열림(재료·칸 수·못 하는 까닭) / 잠김(앞 단계·방·숙련·시설·주차). 읽기 전용이고, 열린 칸을 누르면 팔레트가 그 카드를 고른 채 닫힌다.
    /// 마지막 줄 "그 밖의 설비"는 생존에서 옮겨 온 설비 카드 중 열렸거나 지은 것만 (이야기로 열리는 것을 미리 보이지 않게).
    /// </summary>
    internal sealed class CraftTreeView
    {
        private static readonly string[] Lines = { "WORKBENCH", "WATER", "BAG", "WEAPON", "ARMOR", "LIGHT", "SCARECROW" };
        private static readonly string[] TierMarks = { "", "Ⅰ", "Ⅱ", "Ⅲ" };

        private readonly GameSession _session;
        private readonly VisualElement _sheet;
        private readonly ScrollView _body;

        public CraftTreeView(VisualElement root, GameSession session)
        {
            _session = session;
            _sheet = new VisualElement { name = "craft-tree-sheet" };
            _sheet.AddToClassList("sheet");
            _sheet.AddToClassList("hidden");
            var panel = new VisualElement();
            panel.AddToClassList("ct-panel");

            var head = new VisualElement();
            head.AddToClassList("ct-head");
            Add(head, Loc.T("craft.tree.title"), "ct-title");
            Add(head, Loc.T("craft.tree.hint"), "ct-hint");
            var close = new Button(Hide) { text = Loc.T("hud.close") };
            close.AddToClassList("ct-close");
            head.Add(close);
            panel.Add(head);

            _body = new ScrollView();
            _body.AddToClassList("ct-body");
            panel.Add(_body);
            _sheet.Add(panel);
            root.Add(_sheet);
            KoreanFont.ApplyDeep(_sheet);
        }

        /// <summary>열린 칸을 눌렀을 때 (팔레트에서 그 카드를 고른다).</summary>
        public Action<string> Picked { get; set; }

        public bool IsOpen => !_sheet.ClassListContains("hidden");

        public void Show()
        {
            Build();
            _sheet.RemoveFromClassList("hidden");
        }

        public void Hide() => _sheet.AddToClassList("hidden");

        private void Build()
        {
            _body.Clear();
            var recipes = _session.ActionsIn(ActionCategory.Craft).Where(a => a.IsCraft).ToList();
            foreach (var line in Lines)
            {
                var steps = recipes.Where(a => a.Craft.Line == line).OrderBy(a => a.Craft.Tier).ThenBy(a => a.Craft.Work).ToList();
                if (steps.Count > 0)
                {
                    _body.Add(Row(Loc.T("craft.line." + line), steps));
                }
            }

            // 그 밖의 설비: 열렸거나 지은 것만
            var others = _session.ActionsIn(ActionCategory.Craft)
                .Where(a => !a.IsCraft && !a.Unlock.FlagsAll.Contains("SYSTEM_ONLY") && (Built(a) || _session.IsUnlocked(a)))
                .ToList();
            if (others.Count > 0)
            {
                _body.Add(Row(Loc.T("craft.line.OTHER"), others, arrows: false));
            }

            KoreanFont.ApplyDeep(_body);
        }

        private VisualElement Row(string title, IReadOnlyList<ActionDef> steps, bool arrows = true)
        {
            var row = new VisualElement();
            row.AddToClassList("ct-row");
            Add(row, title, "ct-line");
            var cells = new VisualElement();
            cells.AddToClassList("ct-cells");
            for (var i = 0; i < steps.Count; i++)
            {
                if (i > 0 && arrows)
                {
                    // 같은 단계끼리는 점, 다음 단계로는 화살표
                    Add(cells, steps[i].Craft.Tier == steps[i - 1].Craft.Tier ? "·" : "→", "ct-arrow");
                }

                cells.Add(Cell(steps[i]));
            }

            row.Add(cells);
            return row;
        }

        private VisualElement Cell(ActionDef action)
        {
            var unlocked = _session.IsUnlocked(action);
            var built = Built(action);
            var cell = new Button(() =>
            {
                if (_session.IsUnlocked(action))
                {
                    Hide();
                    Picked?.Invoke(action.Id);
                }
            });
            cell.AddToClassList("ct-cell");
            cell.EnableInClassList("built", built);
            cell.EnableInClassList("open", unlocked && !built);
            cell.EnableInClassList("locked", !unlocked && !built);

            var top = new VisualElement();
            top.AddToClassList("ct-cell-top");
            var icon = new VisualElement();
            icon.AddToClassList("ct-icon");
            StoryArt.SetContain(icon, StoryArt.Icon("act." + action.Id));
            top.Add(icon);
            var names = new VisualElement();
            names.AddToClassList("ct-names");
            var tier = action.IsCraft && action.Craft.Tier > 0 && action.Craft.Tier < TierMarks.Length ? TierMarks[action.Craft.Tier] + " " : "";
            Add(names, tier + ShortName(action.Id), "ct-name");
            Add(names, Status(action, unlocked, built), "ct-status");
            top.Add(names);
            cell.Add(top);

            var detail = Detail(action, unlocked, built);
            if (detail.Length > 0)
            {
                Add(cell, detail, "ct-detail");
            }

            return cell;
        }

        /// <summary>지어 둔 설비인가: 해금 조건의 FlagsNone(지으면 켜지는 깃발)이 켜져 있다. 물건 레시피는 늘 거짓.</summary>
        private bool Built(ActionDef action)
        {
            var flags = _session.State.Flags;
            if (action.IsCraft && action.Craft.SetFlags.Count > 0)
            {
                return action.Craft.SetFlags.All(flags.Contains);
            }

            return !action.IsCraft && action.Unlock.FlagsNone.Count > 0 && action.Unlock.FlagsNone.All(flags.Contains);
        }

        private string Status(ActionDef action, bool unlocked, bool built)
        {
            if (built)
            {
                return Loc.T("craft.tree.built");
            }

            if (!unlocked)
            {
                return Loc.T("craft.tree.locked");
            }

            return _session.State.Flags.Contains(CraftRules.CraftedFlagPrefix + action.Id) ? Loc.T("craft.tree.made") : Loc.T("craft.tree.open");
        }

        /// <summary>열린 칸: 재료·칸 수·못 하는 까닭 (카드 둘째 줄과 같은 말). 잠긴 칸: 남은 조건.</summary>
        private string Detail(ActionDef action, bool unlocked, bool built)
        {
            if (built)
            {
                return "";
            }

            if (unlocked)
            {
                return action.IsCraft ? CraftUi.CardLine(action, _session.Preview(action.Id), _session) : "";
            }

            return string.Join(" · ", Reasons(action.Unlock));
        }

        /// <summary>잠긴 까닭: 주차, 빠진 깃발(앞 단계·지은 설비·뒤진 방·그 밖), 모자란 숙련·시설. 모르는 조건은 한 번만 "조건이 남았어요".</summary>
        private List<string> Reasons(ConditionDef condition)
        {
            var state = _session.State;
            var reasons = new List<string>();
            var unknown = false;
            if (condition.WeekMin > 0 && state.Calendar.Week < condition.WeekMin)
            {
                reasons.Add(Loc.F("craft.req.week", condition.WeekMin));
            }

            foreach (var flag in condition.FlagsAll.Where(f => !state.Flags.Contains(f)))
            {
                var text = FlagReason(flag);
                if (text == null)
                {
                    unknown = true;
                }
                else
                {
                    reasons.Add(text);
                }
            }

            var checks = condition.Checks.Where(c => !ConditionEvaluator.Check(state, c)).ToList();
            if (condition.AnyChecks.Count > 0 && !condition.AnyChecks.Any(c => ConditionEvaluator.Check(state, c)))
            {
                checks.Add(condition.AnyChecks[0]);
            }

            foreach (var check in checks)
            {
                var text = CheckReason(check);
                if (text == null)
                {
                    unknown = true;
                }
                else
                {
                    reasons.Add(text);
                }
            }

            if (unknown || reasons.Count == 0)
            {
                reasons.Add(Loc.T("craft.req.other"));
            }

            return reasons;
        }

        private string FlagReason(string flag)
        {
            const string crafted = CraftRules.CraftedFlagPrefix;
            const string searched = "SEARCHED_";
            if (flag.StartsWith(crafted, StringComparison.Ordinal))
            {
                return Loc.F("craft.req.crafted", Loc.Josa(ShortName(flag.Substring(crafted.Length)), "을/를"));
            }

            if (flag.StartsWith(searched, StringComparison.Ordinal))
            {
                var key = "hubroom." + flag.Substring(searched.Length);
                var room = Loc.T(key);
                return room == key ? null : Loc.F("craft.req.room", Loc.Josa(room, "을/를"));
            }

            // 다른 레시피가 지어서 켜는 깃발 (작업대·모래 필터·교문 허수아비…)
            var maker = _session.ActionsIn(ActionCategory.Craft).FirstOrDefault(a => a.IsCraft && a.Craft.SetFlags.Contains(flag));
            if (maker != null)
            {
                return Loc.F("craft.req.built", Loc.Josa(ShortName(maker.Id), "이/가"));
            }

            var flagKey = "craft.req.flag." + flag;
            var text = Loc.T(flagKey);
            return text == flagKey ? null : text;
        }

        private static string CheckReason(ValueCheck check)
        {
            switch (check.Kind)
            {
                case ValueKind.Proficiency when Enum.TryParse<ProficiencyType>(check.Key, out var type):
                    return Loc.F("craft.req.prof", UiText.Proficiency(type), check.Value);
                case ValueKind.Facility:
                    var key = "facility." + check.Key;
                    var name = Loc.T(key);
                    return name == key ? null : Loc.F("craft.req.facility", Loc.Josa(name, "이/가"));
                default:
                    return null;
            }
        }

        private static string ShortName(string actionId)
        {
            var name = Loc.Action(actionId, "short");
            return name == actionId ? Loc.Action(actionId) : name;
        }

        private static Label Add(VisualElement parent, string text, string cssClass)
        {
            var label = new Label(text);
            label.AddToClassList(cssClass);
            parent.Add(label);
            return label;
        }
    }
}
