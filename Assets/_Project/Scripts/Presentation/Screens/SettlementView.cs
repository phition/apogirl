using System;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using ApoGirl.Presentation.Art;
using ApoGirl.Presentation.Text;
using ApoGirl.Systems.Session;
using UnityEngine.UIElements;

namespace ApoGirl.Presentation.Screens
{
    /// <summary>
    /// 3겹 주간 결산 (Docs/UI_REDESIGN_PROPOSAL.md): 펼친 일기장. 왼쪽 쪽은 일기와 이번 주에 있었던 일,
    /// 오른쪽 쪽은 자란 것·살림·몸과 마음, 아래에 선생님의 빨간 펜 한마디. 수치와 이야기를 나란히 둔다.
    /// </summary>
    internal sealed class SettlementView
    {
        private const int MaxEvents = 6;

        private readonly VisualElement _sheet;
        private readonly VisualElement _left;
        private readonly VisualElement _right;
        private readonly VisualElement _buttons;
        private readonly GameSession _session;

        public SettlementView(VisualElement root, GameSession session)
        {
            _session = session;
            _sheet = root.Q("settlement");
            _left = root.Q("settle-left");
            _right = root.Q("settle-right");
            _buttons = root.Q("settle-buttons");
            StoryArt.SetContain(root.Q("settle-book"), StoryArt.Ui("ui_paper_diary_spread"));
        }

        public bool IsOpen => !_sheet.ClassListContains("hidden");

        public void Show(WeekSummary summary, Action onNext, Action onUndo)
        {
            var state = _session.State;
            _left.Clear();
            _right.Clear();
            _buttons.Clear();

            // 뒤: 거처의 밤 (어둡게), 왼쪽 쪽 위: 한 주 끝 도장
            var night = StoryArt.Backdrop(state, true);
            _sheet.style.backgroundImage = night != null ? new StyleBackground(night) : new StyleBackground(StyleKeyword.Null);
            _sheet.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Cover));
            _sheet.style.unityBackgroundImageTintColor = new UnityEngine.Color(0.42f, 0.42f, 0.48f, 1f);
            var stamp = StoryArt.Ui("Paper/ui_stamp_week_done");
            if (stamp != null)
            {
                var mark = new Label(Loc.F("settle.stamp", summary.Week));
                mark.AddToClassList("settle-stamp");
                mark.style.backgroundImage = new StyleBackground(stamp);
                mark.pickingMode = PickingMode.Ignore;
                _left.Add(mark);
            }

            // ── 왼쪽 쪽: 일기 ──
            var season = UiText.Season(CalendarRules.GetSeason(summary.Week));
            Add(_left, Loc.F("settle.title", summary.Week, season), "settle-title");
            var diary = Loc.Lookup(Loc.UiTable, summary.DiaryKey);
            Add(_left, string.IsNullOrEmpty(diary) ? Loc.T("settle.no_diary") : StoryText.Format(diary, state), "settle-diary");

            var events = NotableLines(summary);
            if (events.Count > 0)
            {
                Add(_left, Loc.T("settle.week_events"), "settle-head");
                foreach (var line in events.Take(MaxEvents))
                {
                    Add(_left, "· " + line, "settle-line");
                }
            }

            // ── 오른쪽 쪽: 수치 ──
            var delta = summary.Delta;
            var grown = delta.Stats.Where(p => p.Value != 0).OrderByDescending(p => p.Value).ToList();
            if (grown.Count > 0 || delta.ProficiencyLevelUps.Count > 0)
            {
                Add(_right, Loc.T("settle.grown"), "settle-head");
                var max = Math.Max(1, grown.Count > 0 ? grown.Max(p => Math.Abs(p.Value)) : 1);
                foreach (var pair in grown)
                {
                    var row = new VisualElement();
                    row.AddToClassList("settle-stat");
                    var name = new Label(UiText.Stat(pair.Key) + " " + UiText.Signed(pair.Value));
                    name.AddToClassList("settle-stat-name");
                    row.Add(name);
                    var bar = new VisualElement();
                    bar.AddToClassList("settle-stat-bar");
                    bar.EnableInClassList("down", pair.Value < 0);
                    bar.style.width = Length.Percent(Math.Min(100f, 100f * Math.Abs(pair.Value) / max) * 0.6f);
                    row.Add(bar);
                    _right.Add(row);
                }

                foreach (var level in delta.ProficiencyLevelUps)
                {
                    Add(_right, Loc.F("result.levelup", UiText.Proficiency(level)), "settle-line strong");
                }
            }

            var resources = delta.Resources.Where(p => p.Value != 0).ToList();
            if (resources.Count > 0)
            {
                Add(_right, Loc.T("settle.supplies"), "settle-head");
                Add(_right, string.Join("   ", resources.Select(p => UiText.Resource(p.Key) + " " + UiText.Signed(p.Value)
                                                                    + " (" + state.Base.GetResource(p.Key) + ")")), "settle-line");
            }

            Add(_right, Loc.T("settle.body"), "settle-head");
            var player = state.Player;
            Add(_right, Loc.F("settle.vitals", player.Vitals.GetVital(VitalType.Hp), player.Vitals.GetVital(VitalType.Fatigue),
                player.Vitals.GetVital(VitalType.Stress), player.SleepDebt), "settle-line");

            // 선생님의 빨간 펜 (만난 뒤부터)
            if (_session.HasMetTeacher)
            {
                var comment = LineRules.Pick(state, _session.Definitions, LineKind.TeacherComment, Core.Events.EventContext.FromCalendar(state));
                if (comment != null)
                {
                    Add(_right, StoryText.Format(Loc.T(comment.TextKey), state), "settle-teacher");
                }
            }

            var foot = new List<string>();
            if (summary.StatusAfter == GameStatus.Playing)
            {
                foot.Add(Loc.T("summary.autosaved"));
            }

            if (state.Flags.Contains("RESCUE_ANNOUNCED"))
            {
                foot.Add(Loc.F("hub.rescue", Math.Max(0, CalendarState.TotalWeeks - state.Calendar.Week)));
            }

            if (foot.Count > 0)
            {
                Add(_right, string.Join("  ·  ", foot), "settle-foot");
            }

            // ── 단추 ──
            var next = new Button(() =>
            {
                Hide();
                onNext();
            }) { text = Loc.T("summary.next") };
            next.AddToClassList("settle-next");
            _buttons.Add(next);
            if (onUndo != null)
            {
                var undo = new Button(() =>
                {
                    Hide();
                    onUndo();
                }) { text = Loc.T("summary.undo") };
                undo.AddToClassList("settle-undo");
                _buttons.Add(undo);
            }

            KoreanFont.ApplyDeep(_sheet);
            _sheet.RemoveFromClassList("hidden");
            next.Focus();
        }

        public void Hide() => _sheet.AddToClassList("hidden");

        /// <summary>이번 주에 눈에 띈 일: 대성공·실패, 탐험, 습격·침입.</summary>
        private List<string> NotableLines(WeekSummary summary)
        {
            var lines = new List<string>();
            foreach (var slot in summary.Slots)
            {
                var when = Loc.F("settle.when", slot.Day, UiText.Phase(slot.Phase));
                if (slot.Raid != null)
                {
                    lines.Add(when + " " + Loc.T(slot.Raid.Repelled ? "settle.raid_held" : "settle.raid_lost"));
                    if (!string.IsNullOrEmpty(slot.Raid.CoveredNpcId))
                    {
                        lines.Add(Loc.F("settle.raid_covered", Loc.T("npc." + slot.Raid.CoveredNpcId)));
                    }
                }
                else if (slot.Expedition != null && slot.ExpeditionRequest != null)
                {
                    lines.Add(when + " " + Loc.T("location." + slot.ExpeditionRequest.LocationId) + " — " + Loc.T("settle.exp_" + slot.Expedition.Status));
                }
                else if (!string.IsNullOrEmpty(slot.ActionId) && (slot.Result == CheckResult.GreatSuccess || slot.Result == CheckResult.Failure))
                {
                    lines.Add(when + " " + Loc.Action(slot.ActionId, "short") + " " + UiText.Result(slot.Result));
                }
            }

            // 동료 탐험: 이번 주 동료들이 구해 온 것을 한 줄로 (역할 재기획 5.1절)
            var returns = summary.Slots.Where(s => s.OutingReturns != null).SelectMany(s => s.OutingReturns).ToList();
            if (returns.Count > 0)
            {
                var total = returns.SelectMany(r => r.Resources).GroupBy(r => r.Resource)
                    .Select(g => new ResourceAmount(g.Key, g.Sum(r => r.Amount))).OrderBy(r => (int)r.Resource);
                var items = returns.Where(r => !string.IsNullOrEmpty(r.ItemId)).Select(r => Loc.Item(r.ItemId));
                var haul = string.Join(", ", new[] { PeopleUi.Haul(total) }.Concat(items).Where(t => t.Length > 0));
                lines.Add(Loc.F("settle.outing_haul", returns.Select(r => r.NpcId).Distinct().Count(), haul.Length > 0 ? haul : Loc.T("settle.outing_nothing")));
            }

            // 위기 이탈 예고 (기획 C.2 ③): 이유가 보이게
            foreach (var id in summary.LeaveWarned)
            {
                lines.Add(Loc.F("settle.leave_warn", Loc.T("npc." + id)));
            }

            return lines;
        }

        private static Label Add(VisualElement parent, string text, string classes)
        {
            var label = new Label(text);
            foreach (var cssClass in classes.Split(' '))
            {
                label.AddToClassList(cssClass);
            }

            parent.Add(label);
            return label;
        }
    }
}
