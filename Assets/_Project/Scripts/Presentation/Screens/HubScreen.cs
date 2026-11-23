using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ApoGirl.Core.Expedition;
using ApoGirl.Core.Rules;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using ApoGirl.Data;
using ApoGirl.Presentation.App;
using ApoGirl.Presentation.Art;
using ApoGirl.Presentation.Audio;
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
        private BalanceLabView _lab;
        private EndingView _ending;
        private Label _teacherLine;
        private Button _runButton;
        private Label _toast;
        private ExecutionSpeed _speed = ExecutionSpeed.Fast;
        private WeekSummary _pendingSummary;
        private int _fontIndex = 1;
        private DialogueView _dialogueView;
        private ExpeditionPrepView _prep;
        private SettlementView _settlement;
        private FactionView _faction;
        private RecordDiaryView _diary;
        private StorageView _storage;
        private EventDialoguePlayer _dialoguePlayer;
        private VisualElement _teacherAvatar;
        private HubLayers _layers;
        private DockView _dock;
        private MiniVitalsView _miniVitals;
        private StandingView _standing;
        private VisualElement _ticker;
        private VisualElement _standingArea;
        private HubSectionView _section;
        private VisualElement _sectionFrame;
        private VisualElement _teacherBox;
        private int _lastWarningCount = -1;
        private PeopleDrawerView _people;
        private GiveSheetView _give;

        /// <summary>허브 한 줄 멘트를 누를 때마다 다음 사람·다음 말로 (기획 D.5).</summary>
        private int _lineOffset;

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
            _status = new StatusPanelView(_root, _session) { FactionClicked = ShowFactionCard };
            _diary = new RecordDiaryView(_root, _session);
            _storage = new StorageView(_root, _session);
            _palette = new PaletteView(_root, _session);
            _planner = new PlannerView(_root, _session, _palette.ActionFor, ShowToast);
            _planSide = new PlanSideView(_root, _session);
            _layers = new HubLayers(_root) { CanPlan = () => _session.IsPlanning, Opened = () => _storage.Refresh() };
            _dock = new DockView(_root, _session, () => _layers.ToggleBoard());
            _miniVitals = new MiniVitalsView(_root, _session, () => _layers.ToggleRecord());
            _standing = new StandingView(_root.Q("standing"), () => _session.State, () => _session.Definitions.Balance);
            _ticker = _root.Q("ticker");
            CdPlayer.Resume(_session.State); // 고른 CD가 있으면 허브에 들어올 때 이어서 (기획 A)
            _standingArea = _root.Q("standing-area");
            _sectionFrame = _root.Q("section-frame");
            _section = new HubSectionView(_root.Q("section"), _session)
            {
                ActionPicked = id =>
                {
                    _palette.Select(id);
                    _layers.SetBoard(true);
                    ShowToast(Loc.T("plan.pick_slot"));
                },
            };
            _teacherBox = _root.Q("teacher-box");
            _card = new CardOverlay(_root);
            _lab = new BalanceLabView(_root, _session, Refresh);
            _debug = new DebugPanelView(_root.Q("debug-panel"), _session, ForceEvent, ShowEnd, () => _lab.Toggle());
            _dialogueView = new DialogueView(_root)
            {
                PortraitFor = speaker => StoryArt.Portrait(speaker, YarnFunctions.Name(), _session.State),
            };
            Sound.AttachUiClicks(_root);
            _teacherAvatar = new VisualElement { name = "teacher-avatar", pickingMode = PickingMode.Ignore };
            _teacherAvatar.AddToClassList("teacher-avatar");
            _root.Q("teacher-box").Add(_teacherAvatar);
            _prep = new ExpeditionPrepView(_root, _session);
            _settlement = new SettlementView(_root, _session);
            _faction = new FactionView(_root, _session)
            {
                Traded = message =>
                {
                    ShowToast(message);
                    Refresh();
                },
            };
            _people = new PeopleDrawerView(_root, _session) { Changed = Refresh };
            _give = new GiveSheetView(_root, _session);
            _teacherBox.RegisterCallback<ClickEvent>(_ =>
            {
                _lineOffset++;
                Refresh();
            });
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
            CdPlayer.Pause(); // 허브를 떠나면 (타이틀·엔딩) CD도 멈춘다
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.f1Key.wasPressedThisFrame && DebugAccess.Enabled)
            {
                _debug.Toggle();
            }

            // 밸런스 표가 열려 있으면 허브 단축키는 쉬고, Esc는 표를 닫는다
            if (_lab.IsOpen)
            {
                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    _lab.Hide();
                }

                return;
            }

            var busy = _card.IsOpen || _dialogueView.IsOpen || (_ending != null && _ending.IsOpen) || _settlement.IsOpen || _prep.IsOpen || _give.IsOpen;

            // Esc: 배분 화면이 열려 있으면 닫는다 (실행하지 않고 계획으로)
            if (_give.IsOpen && keyboard.escapeKey.wasPressedThisFrame)
            {
                _give.Hide();
                return;
            }

            // Esc: 세력 화면이 열려 있으면 먼저 닫는다
            if (_faction.IsOpen && keyboard.escapeKey.wasPressedThisFrame)
            {
                _faction.Hide();
                return;
            }

            // 대화 중에는 말하는 사람 초상이 왼쪽에 나오므로 입상·요약·선생님 한마디를 잠시 숨긴다
            _standingArea.EnableInClassList("hidden", _dialogueView.IsOpen);
            _teacherBox.EnableInClassList("hidden", _dialogueView.IsOpen);
            if (!busy && keyboard.tabKey.wasPressedThisFrame)
            {
                _layers.ToggleBoard();
            }

            if (!busy && keyboard.escapeKey.wasPressedThisFrame)
            {
                _section.HideRoomPanel();
                _layers.CloseTop();
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
            StoryArt.SetIcon(_root.Q<Button>("menu-button"), StoryArt.Icon("menu.settings"), 22f);
            _root.Q<Button>("menu-button").clicked += ShowMenuCard;
            _root.Q<Button>("plan-open-button").clicked += () => _layers.ToggleBoard();

            _runButton = _root.Q<Button>("run-button");
            _runButton.clicked += () =>
            {
                if (!_session.IsPlanning || _card.IsOpen || _give.IsOpen)
                {
                    return;
                }

                // 한 주 실행: ① 이번 주 식단·배분 → ② 스케줄 실행 (1주차 창고에 갇힌 주는 바로 실행)
                _layers.CloseAll();
                if (!_give.Show(received => StartCoroutine(RunWeek(received))))
                {
                    StartCoroutine(RunWeek(null));
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
                StoryArt.SetIcon(button, StoryArt.Icon(speed == ExecutionSpeed.Normal ? "ctl.speed_1" : speed == ExecutionSpeed.Fast ? "ctl.speed_2" : "ctl.skip"), 18f);
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
            _diary.Refresh();
            _storage.Refresh();
            _palette.Refresh();
            var warned = _planSide.Refresh();
            _planner.Refresh(warned);
            _dock.Refresh(warned);
            _miniVitals.Refresh();
            _people.Refresh();
            _debug.Refresh();

            // 입상: 계획을 고치다 경고가 늘면 잠깐 놀란 얼굴
            var warningCount = _session.IsPlanning ? _session.Warnings().Count : 0;
            if (_session.IsPlanning && _lastWarningCount >= 0 && warningCount > _lastWarningCount)
            {
                _standing.React("surprised", 1.4f);
            }
            else
            {
                _standing.Refresh(warningCount > 0);
            }

            _lastWarningCount = _session.IsPlanning ? warningCount : -1;
            var planButton = _root.Q<Button>("plan-open-button");
            planButton.text = _session.IsPlanning ? Loc.T("hud.plan") : Loc.T("hud.plan_running");
            planButton.SetEnabled(_session.IsPlanning);

            _runButton.text = _session.IsExecuting ? Loc.T("plan.running") : Loc.T("plan.run");
            _runButton.SetEnabled(_session.IsPlanning);
            foreach (var speed in new[] { ExecutionSpeed.Normal, ExecutionSpeed.Fast, ExecutionSpeed.EventsOnly })
            {
                _root.Q("speed-" + speed).EnableInClassList("selected", speed == _speed);
            }

            // 0겹: 선생님을 만난 뒤에는 학교 단면도, 그 전(창고에 갇힌 1주차)에는 거처 그림
            var withSection = _session.HasMetTeacher;
            _sectionFrame.EnableInClassList("hidden", !withSection);
            _root.Q("screen").EnableInClassList("with-section", withSection);
            if (withSection)
            {
                if (_session.IsPlanning)
                {
                    _section.SetTime(Core.State.DayPhase.Day, false);
                }

                _section.Refresh();
            }

            // 그림 (M6-5): 거처 배경, 선생님 얼굴 (그림이 없으면 지금처럼 글자)
            var backdrop = StoryArt.Backdrop(_session.State);
            StoryArt.SetCover(_root.Q("backdrop"), backdrop);
            _root.Q("backdrop-label").EnableInClassList("hidden", backdrop != null);
            // 한 줄 멘트: 보통은 윤 선생님, 가까워진 동료가 가끔 대신 말한다 (기획 D.5)
            var speaker = _session.HasMetTeacher ? CompanionSpeaker(warned.Count > 0) : null;
            var avatar = !_session.HasMetTeacher ? null : speaker != null ? StoryArt.Icon("npc." + speaker) : StoryArt.Ui("avatar_teacher");
            StoryArt.SetContain(_teacherAvatar, avatar);
            _teacherAvatar.EnableInClassList("hidden", avatar == null);

            // 선생님을 만나기 전 (1주차 창고): 혼잣말
            _root.Q<Label>("teacher-name").text = !_session.HasMetTeacher ? Loc.T("hub.alone_name") : speaker != null ? Loc.T("npc." + speaker) : Loc.T("hub.teacher_name");
            _teacherLine.text = !_session.HasMetTeacher ? Loc.T(_session.IsExecuting ? "hub.alone_running" : "hub.alone_line")
                : speaker != null ? CompanionLine(speaker) : TeacherLine(warned.Count > 0);
        }

        /// <summary>
        /// 이번에 한 줄 멘트를 할 동료 (없으면 null = 선생님). 실행 중·경고·수면 부채는 늘 선생님이 말한다.
        /// 호감이 일정 이상인 동료만, 주마다(또는 멘트를 누를 때마다) 몇 번에 한 번 차례로 나온다.
        /// </summary>
        private string CompanionSpeaker(bool hasWarnings)
        {
            var state = _session.State;
            var balance = _session.Definitions.Balance;
            if (_session.IsExecuting || hasWarnings || state.Player.SleepDebt >= 3 || balance.HubCompanionLineEvery <= 0)
            {
                return null;
            }

            var close = state.Npcs.Where(n => n.Value.Status == NpcStatus.Joined && n.Value.Affinity >= balance.HubCompanionLineAffinity)
                .Select(n => n.Key).OrderBy(k => k, System.StringComparer.Ordinal).ToList();
            var turn = state.Calendar.Week + _lineOffset;
            if (close.Count == 0 || turn % balance.HubCompanionLineEvery != 0)
            {
                return null;
            }

            return close[turn / balance.HubCompanionLineEvery % close.Count];
        }

        private string CompanionLine(string npcId)
        {
            var index = (_session.State.Calendar.Week + _lineOffset) % 3;

            // 적성 방이 아직 없으면 가끔 바람을 말한다 (방 이름은 말하지 않는다)
            if (index == 0 && ApoGirl.Core.Rules.TogetherRules.WishesForAptitudeRoom(_session.State, _session.Definitions, npcId))
            {
                var wish = Loc.Lookup(Loc.UiTable, "hubline.wish." + npcId);
                if (!string.IsNullOrEmpty(wish))
                {
                    return wish;
                }
            }

            var text = Loc.Lookup(Loc.UiTable, "hubline." + npcId + "." + index);
            return string.IsNullOrEmpty(text) ? Loc.T("hubline.generic." + index) : text;
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
            if (line != null)
            {
                return Loc.T(line.TextKey);
            }

            // 가까워진 선생님은 말투가 조금 풀린다 (기획 C.1)
            var close = state.Teacher.Affinity >= _session.Definitions.Balance.HubCompanionLineAffinity;
            return Loc.T((close ? "teacher.idle_close." : "teacher.idle.") + (state.Calendar.Week + _lineOffset) % 4);
        }

        // ── 실행 ──

        /// <summary>배분을 받은 사람들이 차례로 고마워한다: 단면도 머리 위 말풍선, 안 보이면 알림 한 줄.</summary>
        private void ShowThanks(List<(string Id, bool Liked)> received)
        {
            if (received == null || received.Count == 0)
            {
                return;
            }

            var hidden = new List<string>();
            for (var i = 0; i < received.Count; i++)
            {
                var (id, liked) = received[i];
                var text = PeopleUi.Thanks(id, liked, _session.State.Calendar.Week * 7 + i);
                if (!_session.HasMetTeacher || !_section.ShowBubble(id, text, i * 0.7f))
                {
                    hidden.Add(Loc.F("give.thanks_toast", Loc.T("npc." + id), text));
                }
            }

            if (hidden.Count > 0)
            {
                ShowToast(string.Join("\n", hidden));
            }
        }

        /// <summary>희망 덕에 남기로 한 동료: "{이름}, 너랑 같이 있으면 희망이 느껴져." (동료별 문구 hope.bubble.{NPC}, 없으면 hope.bubble)</summary>
        private void ShowHopeHeld(string npcId)
        {
            if (string.IsNullOrEmpty(npcId))
            {
                return;
            }

            var text = HopeBubble(npcId);
            if (!_session.HasMetTeacher || !_section.ShowBubble(npcId, text, 0.4f, 3.6f))
            {
                ShowToast(Loc.F("give.thanks_toast", Loc.T("npc." + npcId), text));
            }
        }

        /// <summary>말풍선 문구: {0} 부르는 이름, {1} 부르는 말(이름+아/야), {2} 성까지 붙인 이름.</summary>
        public static string HopeBubble(string npcId)
        {
            var given = YarnFunctions.Given();
            var key = "hope.bubble." + npcId;
            var format = Loc.T(key);
            if (format == key)
            {
                format = Loc.T("hope.bubble");
            }

            return string.Format(format, given, ApoGirl.Core.Text.KoreanJosa.Attach(given, "아/야"), YarnFunctions.Name());
        }

        /// <summary>함께한 시간: 곁에 있던 사람 머리 위 말풍선 (단면도에 안 보이면 생략, 결과 한 줄에 이름이 남는다).</summary>
        private void ShowTogether(SlotOutcome outcome)
        {
            if (outcome.Together == null || !_session.HasMetTeacher)
            {
                return;
            }

            var seed = outcome.Week * 31 + outcome.Day * 3 + (int)outcome.Phase;
            var shown = 0;
            for (var i = 0; i < outcome.Together.Count; i++)
            {
                var entry = outcome.Together[i];
                if (!entry.Absent)
                {
                    _section.ShowBubble(entry.Id, PeopleUi.Together(entry.Id, entry.Aptitude, seed + i * 7), 0.2f + shown++ * 0.6f);
                }
            }

            // 동아리 (확장 7): 같이 한 사람 말풍선
            if (outcome.Club != null)
            {
                _section.ShowBubble(outcome.Club.NpcId, Loc.T("club.bubble." + outcome.Club.Activity), 0.2f + shown++ * 0.6f);
            }

            // 동료 탐험에서 돌아온 사람: 구해 온 것 말풍선
            foreach (var back in outcome.OutingReturns ?? new List<OutingReturn>())
            {
                _section.ShowBubble(back.NpcId, PeopleUi.ReturnBubble(back), 0.2f + shown++ * 0.6f, 3f);
            }
        }

        private IEnumerator RunWeek(List<(string Id, bool Liked)> received)
        {
            _pendingSummary = null;
            _layers.CloseAll();
            _session.BeginWeek();
            ShowThanks(received);
            while (_session.IsExecuting)
            {
                // 단면도: 이번 칸의 방으로 걸어간다 (보통 속도면 걷는 모습을 조금 보여 준다)
                if (_session.HasMetTeacher)
                {
                    var calendar = _session.State.Calendar;
                    var slot = _session.State.Plan.Slots[CalendarRules.SlotIndex(calendar.Day, calendar.Phase)];
                    _section.SetTime(calendar.Phase, true);
                    var travel = _section.Act(slot.ActionId);
                    var wait = _speed == ExecutionSpeed.Normal ? Mathf.Min(travel, 3f) : _speed == ExecutionSpeed.Fast ? Mathf.Min(travel, 1.8f) : 0f;
                    if (wait > 0.05f)
                    {
                        yield return new WaitForSeconds(wait);
                    }
                }

                var outcome = _session.Step();
                ShowTogether(outcome);
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
                        CdPlayer.Pause(); // 탐험을 떠나면 멈추고, 돌아오면 같은 곡이 이어진다 (기획 A)
                        yield return GameApp.RunExpedition(outcome.ExpeditionRequest, result => expeditionResult = result);
                        _root.style.display = DisplayStyle.Flex;
                        SetPanelRendererEnabled(true);
                        CdPlayer.Resume(_session.State);
                        outcome = _session.CompleteExpedition(expeditionResult);
                    }
                }

                // 탐험에서 돌아오면 탐험 정산(결과 카드)부터 보여 주고, 그 뒤에 이어지는 이벤트를 진행한다
                var expeditionSlot = outcome.Expedition != null;
                if (expeditionSlot && outcome.StopForPresentation)
                {
                    var closed = false;
                    ShowResult(outcome, () => closed = true);
                    while (!closed)
                    {
                        yield return null;
                    }
                }

                while (_session.HasPendingEvent)
                {
                    yield return PlayPendingEvent();
                }

                // 창고에 갇혀 있는 날: 그날의 대사가 곧 연출이라 결과 카드는 생략한다 (끝난 판은 그대로 보여 줌)
                var confined = !_session.IsEnded && _session.Definitions.TryGetAction(outcome.ActionId ?? "", out var slotAction) && slotAction.Confined;
                var resultShown = expeditionSlot && outcome.StopForPresentation;
                if (resultShown)
                {
                    // 결과 카드는 이벤트 앞에서 이미 보여 줬다
                }
                else if (outcome.StopForPresentation && !confined)
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
                    if (!confined)
                    {
                        ShowTicker(outcome);
                    }

                    var delay = ExecutionPacer.DelayAfter(outcome, _speed);
                    if (delay > 0f)
                    {
                        yield return new WaitForSeconds(delay);
                    }
                }
            }

            _section.SetTime(Core.State.DayPhase.Day, false);
            Refresh();

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
                _dialogueView.SetScene(StoryArt.EventImage(ev.Id));
                Sound.OnEvent(ev.Id);
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

            // 프롤로그의 메아리 G: 희망 덕에 설득이 통했으면 그 동료 머리 위 말풍선 (안 보이면 알림 한 줄)
            ShowHopeHeld(EventDialoguePlayer.LastChoiceResult?.HopeHeldNpcId);

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

            // 소리 (M6-5): 습격이면 경보, 아니면 성공·실패
            if (outcome.Raid != null)
            {
                Sound.Play("Sfx/ui_raid_alarm", SoundCategory.Sfx);
            }
            else
            {
                Sound.Play(outcome.Result == Core.Rules.CheckResult.Failure ? "Sfx/ui_failure" : "Sfx/ui_success", SoundCategory.Ui);
            }

            if (outcome.Raid != null)
            {
                lines.Add(Loc.F(outcome.Raid.Repelled ? "result.raid_repelled" : "result.raid_breached", outcome.Raid.Strength, outcome.Raid.Power));
                if (!string.IsNullOrEmpty(outcome.Raid.CoveredNpcId))
                {
                    lines.Add(Loc.F("result.raid_covered", Loc.T("npc." + outcome.Raid.CoveredNpcId)));
                }

                if (outcome.Raid.CatWarned)
                {
                    lines.Add(Loc.T("result.raid_cat"));   // 확장 2: 고양이가 먼저 털을 세웠다
                }
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

            if (outcome.FirstAidPatched)
            {
                var skilled = _session.State.Player.ProficiencyLevel(ProficiencyType.Medicine) >= _session.Definitions.Balance.FirstAidSkilledLevel;
                lines.Add(Loc.T(skilled ? "result.first_aid_skilled" : "result.first_aid"));
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

        /// <summary>주간 결산: 펼친 일기장 (일기·이번 주 일 / 자란 것·살림·몸과 마음·선생님 한마디).</summary>
        private void ShowSummary(WeekSummary summary, System.Action onClosed)
        {
            System.Action undo = null;
            if (_session.CanUndoWeek)
            {
                // 이야기 중심: 이번 주를 시작 직전으로 되돌린다 (같은 주는 한 번)
                undo = () =>
                {
                    _session.UndoWeek();
                    ShowToast(Loc.T("summary.undone"));
                    onClosed();
                };
            }

            _settlement.Show(summary, onClosed, undo);
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
                (Loc.T(_session.UseRealtimeExpeditions ? "menu.explore_manual" : "menu.explore_auto"), ToggleAutoExplore),
                (Loc.T("menu.to_title"), () => GameApp.GoTo(GameApp.TitleScene)),
                (Loc.T("common.close"), _card.Hide));
        }

        /// <summary>탐험 직접 조작 ↔ 자동(헤드리스). 다음 탐험부터.</summary>
        private void ToggleAutoExplore()
        {
            ControlPrefs.AutoExplore = _session.UseRealtimeExpeditions;
            _session.UseRealtimeExpeditions = !ControlPrefs.AutoExplore;
            ShowMenuCard();
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

        /// <summary>세력 화면 (3겹 전용): 얼굴·소개·평판 눈금·영향, 거래 카드, 최근 일.</summary>
        private void ShowFactionCard(string factionId)
        {
            if (_card.IsOpen || _dialogueView.IsOpen || !_session.IsPlanning)
            {
                return;
            }

            _layers.CloseAll();
            _faction.Show(factionId);
        }

        /// <summary>
        /// 실행 중 평범한 칸: 카드로 멈추지 않고 장면 위에 결과 한 줄을 흘려보낸다 (최근 4줄, 몇 초 뒤 사라짐).
        /// 대성공이면 입상이 밝게 웃는다.
        /// </summary>
        private void ShowTicker(SlotOutcome outcome)
        {
            if (string.IsNullOrEmpty(outcome.ActionId))
            {
                return;
            }

            var deltas = UiText.DeltaLines(outcome.Delta);
            var detail = UiText.Result(outcome.Result) + (deltas.Count > 0 ? "  " + string.Join("  ", deltas.Take(3)) : "");
            var together = PeopleUi.TogetherLine(outcome.Together);
            if (together.Length > 0)
            {
                detail += "  · " + together;
            }

            foreach (var club in PeopleUi.ClubLines(outcome.Club))
            {
                detail += "  · " + club;
            }

            if (outcome.Broadcast != null)
            {
                if (outcome.Broadcast.Replied)
                {
                    detail += "  · " + Loc.T("result.broadcast_reply");
                }

                if (outcome.Broadcast.Risky)
                {
                    detail += "  · " + Loc.T("result.broadcast_risk");
                }
            }

            var outings = PeopleUi.OutingLines(outcome.OutingDepartures, outcome.OutingReturns);
            if (outings.Count > 0)
            {
                detail += "  · " + string.Join(" · ", outings);
            }
            var line = new Label(Loc.F("hud.ticker", outcome.Day, UiText.Phase(outcome.Phase), Loc.Action(outcome.ActionId, "short"), detail));
            line.AddToClassList("ticker-line");
            line.EnableInClassList("great", outcome.Result == Core.Rules.CheckResult.GreatSuccess);
            line.EnableInClassList("fail", outcome.Result == Core.Rules.CheckResult.Failure);
            _ticker.Add(line);
            while (_ticker.childCount > 4)
            {
                _ticker.RemoveAt(0);
            }

            line.schedule.Execute(() => line.AddToClassList("fading")).ExecuteLater(2600);
            line.schedule.Execute(() => line.RemoveFromHierarchy()).ExecuteLater(3000);
            if (outcome.Result == Core.Rules.CheckResult.GreatSuccess)
            {
                _standing.React("bright", 1.6f);
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
