using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using ApoGirl.Data;
using ApoGirl.Presentation.App;
using ApoGirl.Presentation.Dialogue;
using ApoGirl.Presentation.Text;
using ApoGirl.Systems.Save;
using ApoGirl.Systems.Session;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace ApoGirl.Presentation.Screens
{
    /// <summary>
    /// 거점 허브 화면 (문서 10장): 상단 바, 스탯, 행동 팔레트, 주간 플래너, 실행·결과 카드·주간 정산, 디버그(F1).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class HubScreen : MonoBehaviour
    {
        private static readonly string[] FontClasses = { "font-small", "font-normal", "font-large" };

        [Tooltip("Boot 씬을 거치지 않고 Hub를 바로 실행할 때 쓸 정의 DB")]
        [SerializeField] private DefinitionDatabaseAsset _fallbackDatabase;

        private GameSession _session;
        private VisualElement _root;
        private TopBarView _topBar;
        private StatusPanelView _status;
        private PaletteView _palette;
        private PlannerView _planner;
        private PlanSideView _planSide;
        private CardOverlay _card;
        private DebugPanelView _debug;
        private EndingView _ending;
        private Label _teacherLine;
        private Button _runButton;
        private Label _toast;
        private ExecutionSpeed _speed = ExecutionSpeed.Fast;
        private WeekSummary _pendingSummary;
        private int _fontIndex = 1;
        private DialogueView _dialogueView;
        private ExpeditionPrepView _prep;
        private EventDialoguePlayer _dialoguePlayer;

        /// <summary>테스트용: 허브 진입 시 시작 이벤트를 자동으로 띄우지 않는다.</summary>
        public static bool AutoPlayOpening = true;

        /// <summary>Yarn 프로젝트 (Resources/Dialogue/ApoGirl.yarnproject).</summary>
        public const string YarnProjectResource = "Dialogue/ApoGirl";

        private void OnEnable()
        {
            _session = GameApp.EnsureSession(_fallbackDatabase);
            if (!_session.HasGame)
            {
                _session.NewGame(System.Environment.TickCount);
            }

            _root = GetComponent<UIDocument>().rootVisualElement;
            KoreanFont.Apply(_root);
            _root.Q("screen").AddToClassList(FontClasses[_fontIndex]);

            _topBar = new TopBarView(_root, _session);
            _status = new StatusPanelView(_root, _session);
            _palette = new PaletteView(_root, _session);
            _planner = new PlannerView(_root, _session, () => _palette.SelectedActionId, ShowToast);
            _planSide = new PlanSideView(_root, _session);
            _card = new CardOverlay(_root);
            _debug = new DebugPanelView(_root.Q("debug-panel"), _session, ForceEvent, ShowEnd);
            _dialogueView = new DialogueView(_root);
            _prep = new ExpeditionPrepView(_session, _card);
            var yarnProject = Resources.Load<Yarn.Unity.YarnProject>(YarnProjectResource);
            if (yarnProject != null && yarnProject.Program != null)
            {
                _dialoguePlayer = new EventDialoguePlayer(yarnProject, _dialogueView);
            }
            else
            {
                Debug.LogWarning("Yarn 프로젝트를 불러오지 못함: Resources/" + YarnProjectResource);
            }
            _teacherLine = _root.Q<Label>("teacher-line");
            _toast = _root.Q<Label>("toast");
            _root.Q<Label>("teacher-name").text = Loc.T("hub.teacher_name");
            _root.Q<Label>("backdrop-label").text = Loc.T("hub.backdrop");

            BindButtons();

            _session.StateChanged += Refresh;
            _session.WeekEnded += OnWeekEnded;
            Refresh();

            if (_session.IsEnded)
            {
                ShowEnd();
            }
            else if (_session.HasPendingOpening && AutoPlayOpening)
            {
                StartCoroutine(OpeningRoutine());
            }
        }

        /// <summary>
        /// Unity 6.6은 UIDocument 옆에 PanelRenderer를 붙여 같은 UXML 사본을 따로 그린다.
        /// 탐험 중 허브를 숨길 때 그 사본도 함께 꺼야 탐험 화면이 가려지지 않는다.
        /// </summary>
        private void SetPanelRendererEnabled(bool enabled)
        {
            var renderer = GetComponent<PanelRenderer>();
            if (renderer != null)
            {
                renderer.enabled = enabled;
            }
        }

        /// <summary>새 게임 직후: 1막 시작 이벤트 (선생님과의 만남).</summary>
        private IEnumerator OpeningRoutine()
        {
            yield return null;
            while (_session.HasPendingOpening && _session.IsPlanning)
            {
                _session.StartOpening();
                while (_session.HasPendingEvent)
                {
                    yield return PlayPendingEvent();
                }
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (_session == null)
            {
                return;
            }

            _session.StateChanged -= Refresh;
            _session.WeekEnded -= OnWeekEnded;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.f1Key.wasPressedThisFrame)
            {
                _debug.Toggle();
            }

            if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)
            {
                if (_ending != null && _ending.IsOpen)
                {
                    _ending.Advance();
                }
                else
                {
                    _dialogueView.Advance();
                }
            }
        }

        private void BindButtons()
        {
            _root.Q<Button>("save-button").text = Loc.T("hub.save");
            _root.Q<Button>("save-button").clicked += ShowSaveCard;
            _root.Q<Button>("menu-button").text = Loc.T("hub.menu");
            _root.Q<Button>("menu-button").clicked += ShowMenuCard;

            _runButton = _root.Q<Button>("run-button");
            _runButton.clicked += () =>
            {
                if (_session.IsPlanning && !_card.IsOpen)
                {
                    StartCoroutine(RunWeek());
                }
            };

            var planButtons = _root.Q("plan-buttons");
            planButtons.Add(SmallButton(Loc.T("plan.copy_last"), () => _session.CopyLastWeek()));
            planButtons.Add(SmallButton(Loc.T("plan.save_preset"), ShowSavePresetCard));
            planButtons.Add(SmallButton(Loc.T("plan.load_preset"), ShowPresetListCard));

            var speedButtons = _root.Q("speed-buttons");
            foreach (var speed in new[] { ExecutionSpeed.Normal, ExecutionSpeed.Fast, ExecutionSpeed.EventsOnly })
            {
                var button = SmallButton(Loc.T("speed." + speed), () =>
                {
                    _speed = speed;
                    Refresh();
                });
                button.name = "speed-" + speed;
                speedButtons.Add(button);
            }
        }

        private void Refresh()
        {
            if (_session?.State == null)
            {
                return;
            }

            _topBar.Refresh();
            _status.Refresh();
            _palette.Refresh();
            var warned = _planSide.Refresh();
            _planner.Refresh(warned);
            _debug.Refresh();

            _runButton.text = _session.IsExecuting ? Loc.T("plan.running") : Loc.T("plan.run");
            _runButton.SetEnabled(_session.IsPlanning);
            foreach (var speed in new[] { ExecutionSpeed.Normal, ExecutionSpeed.Fast, ExecutionSpeed.EventsOnly })
            {
                _root.Q("speed-" + speed).EnableInClassList("selected", speed == _speed);
            }

            // 선생님을 만나기 전 (1주차 창고): 혼잣말
            _root.Q<Label>("teacher-name").text = _session.HasMetTeacher ? Loc.T("hub.teacher_name") : Loc.T("hub.alone_name");
            _teacherLine.text = _session.HasMetTeacher ? TeacherLine(warned.Count > 0) : Loc.T(_session.IsExecuting ? "hub.alone_running" : "hub.alone_line");
        }

        private string TeacherLine(bool hasWarnings)
        {
            var state = _session.State;
            if (_session.IsExecuting)
            {
                return Loc.T("teacher.running");
            }

            if (state.Player.SleepDebt >= 3)
            {
                return Loc.T("teacher.sleep_debt");
            }

            if (hasWarnings)
            {
                return Loc.T("teacher.warn");
            }

            var line = ApoGirl.Core.Rules.LineRules.Pick(state, _session.Definitions, ApoGirl.Core.Definitions.LineKind.TeacherComment,
                ApoGirl.Core.Events.EventContext.FromCalendar(state));
            return line != null ? Loc.T(line.TextKey) : Loc.T("teacher.idle." + state.Calendar.Week % 4);
        }

        // ── 실행 ──

        private IEnumerator RunWeek()
        {
            _pendingSummary = null;
            _session.BeginWeek();
            while (_session.IsExecuting)
            {
                var outcome = _session.Step();
                if (outcome.AwaitingExpedition)
                {
                    // 출정 준비 (목적지·예보·로드아웃) → 실시간 탐험: 허브 UI를 숨기고 탐험 씬을 추가 로드 → 결과를 세션에 반영
                    var departed = false;
                    var dispatched = false;
                    _prep.Show(outcome.ExpeditionRequest, () => departed = true, () => dispatched = true);
                    while (!departed && !dispatched)
                    {
                        yield return null;
                    }

                    if (dispatched)
                    {
                        // 이야기 중심: 자동 수색 (헤드리스, 보상 감소)
                        outcome = _session.DispatchExpedition();
                    }
                    else
                    {
                        ExpeditionResult expeditionResult = null;
                        _root.style.display = DisplayStyle.None;
                        SetPanelRendererEnabled(false);
                        yield return GameApp.RunExpedition(outcome.ExpeditionRequest, result => expeditionResult = result);
                        _root.style.display = DisplayStyle.Flex;
                        SetPanelRendererEnabled(true);
                        outcome = _session.CompleteExpedition(expeditionResult);
                    }
                }

                while (_session.HasPendingEvent)
                {
                    yield return PlayPendingEvent();
                }

                // 창고에 갇혀 있는 날: 그날의 대사가 곧 연출이라 결과 카드는 생략한다 (끝난 판은 그대로 보여 줌)
                var confined = !_session.IsEnded && _session.Definitions.TryGetAction(outcome.ActionId ?? "", out var slotAction) && slotAction.Confined;
                if (outcome.StopForPresentation && !confined)
                {
                    var closed = false;
                    ShowResult(outcome, () => closed = true);
                    while (!closed)
                    {
                        yield return null;
                    }
                }
                else
                {
                    var delay = ExecutionPacer.DelayAfter(outcome, _speed);
                    if (delay > 0f)
                    {
                        yield return new WaitForSeconds(delay);
                    }
                }
            }

            if (_pendingSummary != null)
            {
                var closed = false;
                ShowSummary(_pendingSummary, () => closed = true);
                _pendingSummary = null;
                while (!closed)
                {
                    yield return null;
                }
            }

            if (_session.IsEnded)
            {
                ShowEnd();
            }
        }

        /// <summary>
        /// 대기 중인 이벤트 하나를 보여준다. Yarn 노드가 있으면 대화로, 없으면 카드로.
        /// 선택지를 고르는 즉시 세션에 반영하고(판정 결과를 대화가 읽을 수 있게), 대화가 선택 없이 끝나면 첫 선택지로 마무리한다.
        /// </summary>
        private IEnumerator PlayPendingEvent()
        {
            var ev = _session.PendingEvent;
            var choices = _session.PendingChoices;
            var resolved = false;
            EventDialoguePlayer.LastChoiceResult = null;

            void Choose(string choiceId)
            {
                if (resolved || _session.PendingEvent != ev)
                {
                    return;
                }

                resolved = true;
                var outcome = _session.ResolveEvent(choiceId);
                EventDialoguePlayer.LastChoiceResult = outcome.Events.Count > 0 ? outcome.Events[outcome.Events.Count - 1] : null;
            }

            if (_dialoguePlayer != null && _dialoguePlayer.HasNode(ev.Node))
            {
                var task = _dialoguePlayer.Run(ev.Node, choices, Choose, destroyCancellationToken);
                while (!task.IsCompleted)
                {
                    yield return null;
                }

                if (task.IsFaulted)
                {
                    Debug.LogException(task.Exception);
                }
            }
            else
            {
                var closed = false;
                ShowEventCard(ev, choices, id =>
                {
                    Choose(id);
                    closed = true;
                });
                while (!closed)
                {
                    yield return null;
                }
            }

            if (!resolved && _session.PendingEvent == ev)
            {
                var first = choices.FirstOrDefault(c => c.Enabled);
                Choose(first?.Id ?? "");
            }

            // M6: 막 평가 카드 (그 막의 능력치 그래프 + 선생님 한마디)
            if (ev.ReviewAct > 0 && !_session.IsEnded)
            {
                var closed = false;
                ShowReviewCard(ev, () => closed = true);
                while (!closed)
                {
                    yield return null;
                }
            }
        }

        private void ShowReviewCard(ApoGirl.Core.Definitions.EventDef ev, System.Action onClosed)
        {
            var state = _session.State;
            var top = StatGraph.TopGain(state, ev.ReviewFromWeek);
            var lines = new List<string> { Loc.F("review.weeks", ev.ReviewFromWeek, state.Calendar.Week) };
            if (top.Gain > 0)
            {
                lines.Add(Loc.F("review.growth", UiText.Stat(top.Stat), top.Gain));
            }

            var line = ApoGirl.Core.Rules.LineRules.Pick(state, _session.Definitions, ApoGirl.Core.Definitions.LineKind.ActReview,
                ApoGirl.Core.Events.EventContext.FromCalendar(state));
            if (line != null)
            {
                lines.Add("");
                lines.Add(StoryText.Format(Loc.T(line.TextKey), state));
            }

            _card.Show(Loc.F("review.title", ev.ReviewAct), string.Join("\n", lines), true, (Loc.T("common.ok"), () =>
            {
                _card.Hide();
                onClosed();
            }));
            _card.Extra.Add(new StatGraph(state, ev.ReviewFromWeek, 160f));
            KoreanFont.ApplyDeep(_card.Extra);
        }

        /// <summary>Yarn 노드가 없는 이벤트: 문자열 테이블(event.{ID}.title/body, 선택지 TextKey)로 카드 표시.</summary>
        private void ShowEventCard(ApoGirl.Core.Definitions.EventDef ev, IReadOnlyList<ApoGirl.Core.Events.EventChoiceView> choices, System.Action<string> onPick)
        {
            var buttons = new List<(string, System.Action)>();
            foreach (var choice in choices)
            {
                var id = choice.Id;
                var label = string.IsNullOrEmpty(choice.Def.TextKey) ? id : Loc.T(choice.Def.TextKey);
                if (choice.Enabled)
                {
                    buttons.Add((label, () =>
                    {
                        _card.Hide();
                        onPick(id);
                    }));
                }
            }

            if (buttons.Count == 0)
            {
                buttons.Add((Loc.T("common.ok"), () =>
                {
                    _card.Hide();
                    onPick("");
                }));
            }

            var title = Loc.Lookup(Loc.UiTable, "event." + ev.Id + ".title") ?? ev.Id;
            var body = Loc.Lookup(Loc.UiTable, "event." + ev.Id + ".body") ?? "";
            _card.Show(title, body, true, buttons.ToArray());
        }

        /// <summary>디버그 패널: 계획 단계에서 이벤트를 강제로 발생시킨다.</summary>
        private void ForceEvent(string eventId)
        {
            if (!_session.IsPlanning || _card.IsOpen || _dialogueView.IsOpen)
            {
                ShowToast(Loc.T("debug.force_event_planning"));
                return;
            }

            StartCoroutine(ForceEventRoutine(eventId));
        }

        private IEnumerator ForceEventRoutine(string eventId)
        {
            var outcome = _session.DebugForceEvent(eventId);
            while (_session.HasPendingEvent)
            {
                yield return PlayPendingEvent();
            }

            var closed = false;
            var lines = UiText.DeltaLines(outcome.Delta);
            _card.Show(Loc.F("debug.event_result", eventId), string.Join("\n", lines.Count > 0 ? lines : new List<string> { Loc.T("result.no_change") }), false,
                (Loc.T("common.ok"), () =>
                {
                    _card.Hide();
                    closed = true;
                }));
            while (!closed)
            {
                yield return null;
            }

            Refresh();
        }

        private void OnWeekEnded(WeekSummary summary)
        {
            _pendingSummary = summary;
        }

        private void ShowResult(SlotOutcome outcome, System.Action onClosed)
        {
            var title = Loc.F("result.title", outcome.Day, UiText.Phase(outcome.Phase), Loc.Action(outcome.ActionId))
                        + "  —  " + UiText.Result(outcome.Result);
            var lines = new List<string>();
            if (outcome.ForcedRestViolated)
            {
                lines.Add(Loc.T("result.violation"));
            }

            if (outcome.ConsecutiveExpedition)
            {
                lines.Add(Loc.T("result.consecutive"));
            }

            if (outcome.Raid != null)
            {
                lines.Add(Loc.F(outcome.Raid.Repelled ? "result.raid_repelled" : "result.raid_breached", outcome.Raid.Strength, outcome.Raid.Power));
            }

            if (outcome.Intrusion != null)
            {
                var intrusion = outcome.Intrusion;
                lines.Add(Loc.F("result.intrusion_" + intrusion.Response + (intrusion.Held ? "_held" : "_lost"), intrusion.Strength, intrusion.Power));
                if (!string.IsNullOrEmpty(intrusion.InjuredNpcId))
                {
                    lines.Add(Loc.F("result.intrusion_injured", Loc.T("npc." + intrusion.InjuredNpcId)));
                }
            }

            if (outcome.WoundInfection > 0)
            {
                lines.Add(Loc.T(_session.State.Player.Vitals.Cleanliness < _session.Definitions.Balance.WoundCleanThreshold ? "result.injured_dirty" : "result.injured"));
            }

            if (outcome.Expedition != null && outcome.ExpeditionRequest != null && outcome.ExpeditionRequest.Restocked)
            {
                lines.Add(Loc.T("result.restocked"));
            }

            if (outcome.Expedition != null)
            {
                lines.Add(outcome.Expedition.Hostile != HostileOutcome.None
                    ? Loc.T("result.hostile_" + outcome.Expedition.Hostile)
                    : Loc.T("result.expedition_" + outcome.Expedition.Status));
                if (outcome.Expedition.Hostile == HostileOutcome.Beaten)
                {
                    lines.Add(Loc.T("result.trauma"));
                }
                if (outcome.Bite != null)
                {
                    lines.Add(Loc.T(outcome.Bite.Treated ? "result.bite_treated" : "result.bite"));
                }

                if (outcome.Expedition.Loot.Count > 0)
                {
                    lines.Add(Loc.F("result.items", string.Join("  ", outcome.Expedition.Loot.Select(i => Loc.Item(i.ItemId) + " ×" + i.Count))));
                }
            }

            var deltas = UiText.DeltaLines(outcome.Delta);
            lines.AddRange(deltas.Count > 0 ? deltas : new List<string> { Loc.T("result.no_change") });

            _card.Show(title, string.Join("\n", lines), true, (Loc.T("common.ok"), () =>
            {
                _card.Hide();
                onClosed();
            }));
        }

        private void ShowSummary(WeekSummary summary, System.Action onClosed)
        {
            var lines = new List<string>();
            var diary = Loc.Lookup(Loc.UiTable, summary.DiaryKey);
            if (!string.IsNullOrEmpty(diary))
            {
                lines.Add(Loc.F("summary.diary", diary));
                lines.Add("");
            }

            lines.AddRange(UiText.DeltaLines(summary.Delta));
            if (summary.StatusAfter == GameStatus.Playing)
            {
                lines.Add("");
                lines.Add(Loc.T("summary.autosaved"));
            }

            if (_session.CanUndoWeek)
            {
                // 이야기 중심: 이번 주를 시작 직전으로 되돌린다 (같은 주는 한 번)
                _card.Show(Loc.F("summary.title", summary.Week), string.Join("\n", lines), true,
                    (Loc.T("summary.next"), () =>
                    {
                        _card.Hide();
                        onClosed();
                    }),
                    (Loc.T("summary.undo"), () =>
                    {
                        _card.Hide();
                        _session.UndoWeek();
                        ShowToast(Loc.T("summary.undone"));
                        onClosed();
                    }));
                return;
            }

            _card.Show(Loc.F("summary.title", summary.Week), string.Join("\n", lines), true, (Loc.T("summary.next"), () =>
            {
                _card.Hide();
                onClosed();
            }));
        }

        /// <summary>엔딩 연출 (M5). 판정은 세션이 게임 종료 시점에 한 번 해 둔다.</summary>
        private void ShowEnd()
        {
            if (_ending != null && _ending.IsOpen)
            {
                return;
            }

            _card.Hide();
            var result = _session.Ending ?? Core.Endings.EndingResolver.Resolve(_session.State, _session.Definitions);
            _ending = new EndingView(_root, _session.State, result, _session.EndingIsNew, () => GameApp.GoTo(GameApp.TitleScene));
        }

        // ── 저장·메뉴·프리셋 ──

        private void ShowSaveCard()
        {
            if (!_session.IsPlanning)
            {
                ShowToast(Loc.T("save.only_planning"));
                return;
            }

            var buttons = new List<(string, System.Action)>();
            for (var i = 1; i <= SaveService.ManualSlotCount; i++)
            {
                var slot = i;
                var slotName = SaveService.ManualSlot(slot);
                var label = Loc.F("save.slot", slot) + (_session.HasSave(slotName) ? "" : " (" + Loc.T("save.empty") + ")");
                buttons.Add((label, () =>
                {
                    _session.Save(slotName);
                    _card.ShowMessage(Loc.T("save.title"), Loc.F("save.done", slot));
                }));
            }

            buttons.Add((Loc.T("common.close"), _card.Hide));
            _card.Show(Loc.T("save.title"), "", false, buttons.ToArray());
        }

        private void ShowMenuCard()
        {
            _card.Show(
                Loc.T("menu.title"),
                "",
                false,
                (Loc.F("menu.font_size", Loc.T("font." + FontClasses[_fontIndex])), CycleFontSize),
                (Loc.T("menu.to_title"), () => GameApp.GoTo(GameApp.TitleScene)),
                (Loc.T("common.close"), _card.Hide));
        }

        private void CycleFontSize()
        {
            var screen = _root.Q("screen");
            screen.RemoveFromClassList(FontClasses[_fontIndex]);
            _fontIndex = (_fontIndex + 1) % FontClasses.Length;
            screen.AddToClassList(FontClasses[_fontIndex]);
            ShowMenuCard();
        }

        private void ShowSavePresetCard()
        {
            if (!_session.IsPlanning)
            {
                return;
            }

            _card.Show(Loc.T("plan.save_preset"), "", false,
                (Loc.T("common.ok"), () =>
                {
                    var name = _card.Extra.Q<TextField>()?.value?.Trim();
                    if (_session.SavePreset(name))
                    {
                        _card.ShowMessage(Loc.T("plan.save_preset"), Loc.T("plan.preset_saved"));
                    }
                    else
                    {
                        ShowToast(Loc.F("plan.preset_full", Core.Planning.PlanEditing.MaxPresets));
                    }
                }),
                (Loc.T("common.cancel"), _card.Hide));
            var field = new TextField(Loc.T("plan.preset_name")) { value = Loc.F("plan.preset_default", _session.State.Calendar.Week) };
            field.AddToClassList("preset-field");
            _card.Extra.Add(field);
        }

        private void ShowPresetListCard()
        {
            if (!_session.IsPlanning)
            {
                return;
            }

            var presets = _session.State.PlanPresets.Select(p => p.Name).ToList();
            _card.Show(Loc.T("plan.load_preset"), presets.Count == 0 ? Loc.T("plan.preset_empty") : "", false,
                (Loc.T("common.close"), _card.Hide));
            foreach (var name in presets)
            {
                var row = new VisualElement();
                row.AddToClassList("preset-row");
                var label = new Label(name);
                label.AddToClassList("preset-name");
                row.Add(label);
                row.Add(SmallButton(Loc.T("plan.preset_apply"), () =>
                {
                    _session.ApplyPreset(name);
                    _card.Hide();
                }));
                row.Add(SmallButton(Loc.T("plan.preset_delete"), () =>
                {
                    _session.DeletePreset(name);
                    ShowPresetListCard();
                }));
                _card.Extra.Add(row);
            }
        }

        private void ShowToast(string message)
        {
            _toast.text = message;
            _toast.RemoveFromClassList("hidden");
            _toast.schedule.Execute(() => _toast.AddToClassList("hidden")).ExecuteLater(1800);
        }

        private static Button SmallButton(string text, System.Action onClick)
        {
            var button = new Button(onClick) { text = text };
            button.AddToClassList("small-button");
            return button;
        }
    }
}
