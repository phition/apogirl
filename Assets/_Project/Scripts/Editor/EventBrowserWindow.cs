using System.Collections.Generic;
using System.Linq;
using System.Text;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Events;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using ApoGirl.Data;
using ApoGirl.Presentation.App;
using UnityEditor;
using UnityEngine;

namespace ApoGirl.EditorTools
{
    /// <summary>
    /// 이벤트 브라우저 (메뉴 ApoGirl/Content/Event Browser): 이벤트 목록·필터, 조건 요약,
    /// 지금 상태에서 발생 가능 여부 미리보기 (플레이 중이면 실제 세이브, 아니면 지정 주차의 새 게임).
    /// </summary>
    public sealed class EventBrowserWindow : EditorWindow
    {
        private IDefinitionDatabase _db;
        private string _search = "";
        private int _kindFilter = -1;
        private int _previewWeek = 1;
        private int _previewDay = 1;
        private DayPhase _previewPhase = DayPhase.Day;
        private ActionCategory _previewCategory = ActionCategory.Rest;
        private Vector2 _listScroll;
        private Vector2 _detailScroll;
        private string _selected;

        [MenuItem("ApoGirl/Content/Event Browser")]
        public static void Open()
        {
            GetWindow<EventBrowserWindow>("Event Browser").Reload();
        }

        private void Reload()
        {
            var asset = AssetDatabase.LoadAssetAtPath<DefinitionDatabaseAsset>(ContentPipeline.DatabasePath);
            _db = asset != null ? asset.Build() : null;
        }

        private void OnGUI()
        {
            if (_db == null)
            {
                Reload();
                if (_db == null)
                {
                    EditorGUILayout.HelpBox("정의 DB를 찾을 수 없음", MessageType.Error);
                    return;
                }
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Import", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    ContentPipeline.Import();
                    Reload();
                }

                if (GUILayout.Button("Validate", EditorStyles.toolbarButton, GUILayout.Width(70)))
                {
                    ContentPipeline.Validate();
                }

                if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    Reload();
                }

                _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
                var kinds = new[] { "All" }.Concat(System.Enum.GetNames(typeof(EventKind))).ToArray();
                _kindFilter = EditorGUILayout.Popup(_kindFilter + 1, kinds, EditorStyles.toolbarPopup, GUILayout.Width(90)) - 1;
            }

            var state = PreviewState(out var live);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(live ? "미리보기: 플레이 중인 세이브" : "미리보기: 새 게임", GUILayout.Width(180));
                using (new EditorGUI.DisabledScope(live))
                {
                    _previewWeek = EditorGUILayout.IntSlider("주차", _previewWeek, 1, CalendarState.TotalWeeks);
                }

                _previewDay = EditorGUILayout.IntSlider("요일", _previewDay, 1, 7);
                _previewPhase = (DayPhase)EditorGUILayout.EnumPopup(_previewPhase, GUILayout.Width(60));
                _previewCategory = (ActionCategory)EditorGUILayout.EnumPopup(_previewCategory, GUILayout.Width(90));
            }

            var context = new EventContext { Day = _previewDay, Phase = _previewPhase, ActionCategory = _previewCategory, ActionId = "PREVIEW" };
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawList(state, context);
                DrawDetail(state, context, live);
            }
        }

        private GameState PreviewState(out bool live)
        {
            var session = Application.isPlaying ? GameApp.Session : null;
            live = session?.State != null;
            if (live)
            {
                return session.State;
            }

            var state = NewGameFactory.Create(_db.Balance, 1);
            state.Calendar.Week = _previewWeek;
            return state;
        }

        private void DrawList(GameState state, EventContext context)
        {
            using (var scroll = new EditorGUILayout.ScrollViewScope(_listScroll, GUILayout.Width(330)))
            {
                _listScroll = scroll.scrollPosition;
                foreach (var ev in _db.Events)
                {
                    if (_kindFilter >= 0 && (int)ev.Kind != _kindFilter) continue;
                    if (!string.IsNullOrEmpty(_search) && ev.Id.IndexOf(_search, System.StringComparison.OrdinalIgnoreCase) < 0) continue;

                    var available = EventScheduler.IsAvailable(state, ev);
                    var met = available && Met(state, ev.Condition, context);
                    var mark = met ? "●" : available ? "○" : "×";
                    var style = _selected == ev.Id ? EditorStyles.boldLabel : EditorStyles.label;
                    if (GUILayout.Button($"{mark} {ev.Id}  [{ev.Kind}]", style))
                    {
                        _selected = ev.Id;
                    }
                }
            }
        }

        private void DrawDetail(GameState state, EventContext context, bool live)
        {
            using (var scroll = new EditorGUILayout.ScrollViewScope(_detailScroll))
            {
                _detailScroll = scroll.scrollPosition;
                if (_selected == null || !_db.TryGetEvent(_selected, out var ev))
                {
                    EditorGUILayout.HelpBox("● 지금 조건 충족 / ○ 조건 불충족 / × 이미 발생(1회성·쿨다운)", MessageType.Info);
                    return;
                }

                EditorGUILayout.LabelField(ev.Id, EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"{ev.Kind} · 우선순위 {ev.Priority} · 가중치 {ev.Weight} · {(ev.Once ? "1회" : "반복, 쿨다운 " + ev.CooldownWeeks + "주")} · 노드 {ev.Node}");
                EditorGUILayout.LabelField("조건 충족", Met(state, ev.Condition, context) ? "예" : "아니오");
                EditorGUILayout.TextArea(Describe(ev.Condition), EditorStyles.wordWrappedLabel);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("선택지", EditorStyles.boldLabel);
                var views = EventResolver.Choices(state, ev, context, _db.Balance);
                foreach (var choice in ev.Choices)
                {
                    var view = views.FirstOrDefault(v => v.Id == choice.Id);
                    var status = view == null ? "숨김" : view.Enabled ? "가능" : "비용 부족";
                    var chance = choice.HasCheck && view != null ? $" · 성공 {view.Chance:P0}" : "";
                    var next = string.IsNullOrEmpty(choice.NextEventId) ? "" : " → " + choice.NextEventId;
                    EditorGUILayout.LabelField($"#{choice.Id}  ({status}{chance}){next}");
                }

                if (live && GameApp.Session.IsPlanning && GUILayout.Button("지금 강제 발생 (플레이 중 · 계획 단계)"))
                {
                    GameApp.Session.DebugForceEvent(ev.Id);
                }
            }
        }

        private bool Met(GameState state, ConditionDef condition, EventContext context)
        {
            try
            {
                return ConditionEvaluator.IsMet(state, condition, context, _db.Balance);
            }
            catch (System.ArgumentException)
            {
                return false;
            }
        }

        private static string Describe(ConditionDef c)
        {
            var parts = new List<string>();
            if (c.WeekMin > 0 || c.WeekMax > 0) parts.Add($"주차 {c.WeekMin}~{(c.WeekMax > 0 ? c.WeekMax.ToString() : "")}");
            if (c.Days.Count > 0) parts.Add("요일 " + string.Join(",", c.Days));
            if (c.Phase != PhaseFilter.Any) parts.Add(c.Phase.ToString());
            if (c.Seasons.Count > 0) parts.Add("계절 " + string.Join(",", c.Seasons));
            if (c.Month > 0) parts.Add($"날짜 {c.Month}/{(c.DayOfMonth > 0 ? c.DayOfMonth.ToString() : "*")}");
            if (c.ActionCategories.Count > 0) parts.Add("행동 " + string.Join(",", c.ActionCategories));
            if (c.ActionIds.Count > 0) parts.Add("행동ID " + string.Join(",", c.ActionIds));
            if (c.FlagsAll.Count > 0) parts.Add("플래그 모두 " + string.Join(",", c.FlagsAll));
            if (c.FlagsAny.Count > 0) parts.Add("플래그 하나 " + string.Join(",", c.FlagsAny));
            if (c.FlagsNone.Count > 0) parts.Add("플래그 없음 " + string.Join(",", c.FlagsNone));
            if (c.CluesAll.Count > 0) parts.Add("단서 " + string.Join(",", c.CluesAll));
            foreach (var check in c.Checks) parts.Add($"{check.Kind}:{check.Key} {check.Op} {check.Value}");
            if (c.AnyChecks.Count > 0) parts.Add("하나 이상: " + string.Join(" | ", c.AnyChecks.Select(k => $"{k.Kind}:{k.Key} {k.Op} {k.Value}")));
            foreach (var npc in c.NpcStatuses) parts.Add($"{npc.NpcId}={npc.Status}");
            var builder = new StringBuilder();
            builder.Append(parts.Count == 0 ? "조건 없음" : string.Join(" · ", parts));
            return builder.ToString();
        }
    }
}
