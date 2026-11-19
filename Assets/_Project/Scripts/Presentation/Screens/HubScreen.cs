using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using ApoGirl.Data;
using ApoGirl.Presentation.App;
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
        private Label _teacherLine;
        private Button _runButton;
        private Label _toast;
        private ExecutionSpeed _speed = ExecutionSpeed.Fast;
        private WeekSummary _pendingSummary;
        private int _fontIndex = 1;

        private void OnEnable()
        {
            _session = AppContext.EnsureSession(_fallbackDatabase);
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
            _debug = new DebugPanelView(_root.Q("debug-panel"), _session);
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

            _teacherLine.text = TeacherLine(warned.Count > 0);
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

            return hasWarnings ? Loc.T("teacher.warn") : Loc.T("teacher.idle." + state.Calendar.Week % 4);
        }

        // ── 실행 ──

        private IEnumerator RunWeek()
        {
            _pendingSummary = null;
            _session.BeginWeek();
            while (_session.IsExecuting)
            {
                var outcome = _session.Step();
                if (outcome.StopForPresentation)
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

            if (outcome.Expedition != null)
            {
                lines.Add(Loc.T(outcome.Expedition.Status == ExtractStatus.Down ? "result.expedition_down" : "result.expedition_ok"));
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
            var lines = UiText.DeltaLines(summary.Delta);
            if (summary.StatusAfter == GameStatus.Playing)
            {
                lines.Add("");
                lines.Add(Loc.T("summary.autosaved"));
            }

            _card.Show(Loc.F("summary.title", summary.Week), string.Join("\n", lines), true, (Loc.T("summary.next"), () =>
            {
                _card.Hide();
                onClosed();
            }));
        }

        private void ShowEnd()
        {
            var state = _session.State;
            _card.Show(
                Loc.T("end." + state.Status),
                Loc.F("end.body", state.Calendar.Week),
                false,
                (Loc.T("end.to_title"), () => AppContext.GoTo(AppContext.TitleScene)));
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
                (Loc.T("menu.to_title"), () => AppContext.GoTo(AppContext.TitleScene)),
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
