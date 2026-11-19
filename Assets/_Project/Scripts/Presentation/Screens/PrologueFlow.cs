using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApoGirl.Core.Definitions;
using ApoGirl.Core.Effects;
using ApoGirl.Core.Simulation;
using ApoGirl.Core.State;
using ApoGirl.Presentation.Dialogue;
using ApoGirl.Presentation.Text;
using ApoGirl.Systems.Session;
using UnityEngine;
using UnityEngine.UIElements;
using Yarn.Unity;

namespace ApoGirl.Presentation.Screens
{
    /// <summary>
    /// 0막 프롤로그 (문서 7.1): 등굣길 친구와의 대화로 이름·친구 연락처·생일·성격 유형·혈액형·성적·과목·동아리·마음 상태·약속을 정한다.
    /// Yarn 노드 PROLOGUE_START → (생활기록부 카드) → PROLOGUE_AFTER. 선택지 태그는 #choice:문항.선택지,
    /// 입력은 &lt;&lt;input name|contact|birthday|personality&gt;&gt; 명령. 결과는 PrologueDef 데이터로 새 게임에 반영한다.
    /// </summary>
    internal sealed class PrologueFlow
    {
        public const string StartNode = "PROLOGUE_START";
        public const string AfterNode = "PROLOGUE_AFTER";

        private readonly VisualElement _root;
        private readonly GameSession _session;
        private readonly YarnProject _project;
        private readonly VisualElement _input;
        private readonly CardOverlay _card;
        private readonly ProfileState _profile = new ProfileState();
        private readonly FriendState _friend = new FriendState();
        private readonly List<(string Question, string Option)> _picks = new List<(string, string)>();
        private DialogueView _view;

        /// <summary>Space·Enter: 대사 넘기기.</summary>
        public void Advance() => _view?.Advance();

        public PrologueFlow(VisualElement root, GameSession session, YarnProject project)
        {
            _root = root;
            _session = session;
            _project = project;
            _input = root.Q("prologue-input");
            _card = new CardOverlay(root);
        }

        public IEnumerator Run(CancellationToken token)
        {
            _profile.Name = Loc.T("prologue.default_name");
            _friend.ContactName = Loc.T("npc.friend_default");
            _friend.CallName = _friend.ContactName;
            YarnFunctions.PreviewProfile = _profile;
            YarnFunctions.PreviewFriend = _friend;

            var view = new DialogueView(_root);
            _view = view;
            var player = new EventDialoguePlayer(_project, view) { CommandHandler = HandleCommand };
            yield return Wait(player.Run(StartNode, null, Record, token));

            var effect = BuildEffect();
            var closed = false;
            ShowRecordCard(effect, () => closed = true);
            while (!closed)
            {
                yield return null;
            }

            if (player.HasNode(AfterNode))
            {
                yield return Wait(player.Run(AfterNode, null, _ => { }, token));
            }

            YarnFunctions.PreviewProfile = null;
            YarnFunctions.PreviewFriend = null;
            _session.NewGame(Environment.TickCount, _profile, effect);
            _session.State.Friend.ContactName = _friend.ContactName;
            _session.State.Friend.CallName = _friend.CallName;
        }

        private static IEnumerator Wait(Task task)
        {
            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.IsFaulted)
            {
                Debug.LogException(task.Exception);
            }
        }

        private void Record(string tag)
        {
            var dot = tag.IndexOf('.');
            if (dot <= 0)
            {
                return;
            }

            _picks.Add((tag.Substring(0, dot), tag.Substring(dot + 1)));
        }

        /// <summary>공통 효과 + 고른 선택지 효과를 합치고, 프로필 칸을 채운다.</summary>
        private GameEffect BuildEffect()
        {
            var prologue = _session.Definitions.Extras.Prologue;
            var effect = new GameEffect().MergeFrom(prologue.Common);
            var personality = "";
            foreach (var (questionId, optionId) in _picks)
            {
                var option = prologue.Questions.Find(q => q.Id == questionId)?.Options.Find(o => o.Id == optionId);
                if (option == null)
                {
                    continue;
                }

                effect.MergeFrom(option.Effect);
                if (questionId == "PERSONALITY")
                {
                    personality += optionId;
                }

                switch (option.ProfileField)
                {
                    case "BloodType":
                        _profile.BloodType = Enum.TryParse<BloodType>(option.ProfileValue, out var blood) ? blood : BloodType.Unknown;
                        break;
                    case "GradeBandId":
                        _profile.GradeBandId = option.ProfileValue;
                        break;
                    case "FavoriteSubjectId":
                        _profile.FavoriteSubjectId = option.ProfileValue;
                        break;
                    case "ClubId":
                        _profile.ClubId = option.ProfileValue;
                        break;
                }
            }

            _profile.PersonalityType = personality;
            return effect;
        }

        /// <summary>생활기록부 카드: 인적사항·성격·특기·성적 = 시작 스탯 시트 (문서 7.1).</summary>
        private void ShowRecordCard(GameEffect effect, Action onClosed)
        {
            var preview = NewGameFactory.Create(_session.Definitions.Balance, 1, _profile);
            EffectApplier.Apply(preview, effect, _session.Definitions.Balance, new StateDelta());
            var player = preview.Player;
            var lines = new List<string>
            {
                Loc.F("record.name", _profile.Name, _profile.BirthMonth, _profile.BirthDay),
                Loc.F("record.personality", string.IsNullOrEmpty(_profile.PersonalityType) ? "-" : _profile.PersonalityType, Loc.T("blood." + _profile.BloodType)),
                Loc.F("record.school", Loc.T("grade." + _profile.GradeBandId), Loc.T("subject." + _profile.FavoriteSubjectId), Loc.T("club." + _profile.ClubId)),
                Loc.F("record.traits", string.Join(", ", player.Traits.Where(t => t.StartsWith("TRAIT_", StringComparison.Ordinal)).Select(t => Loc.T("trait." + t)))),
                "",
                string.Join("   ", Enum.GetValues(typeof(StatType)).Cast<StatType>().Select(s => UiText.Stat(s) + " " + player.GetStat(s))),
                Loc.F("record.total", Enum.GetValues(typeof(StatType)).Cast<StatType>().Sum(s => player.GetStat(s))),
                "",
                Loc.F("record.friend", _friend.ContactName),
            };
            _card.Show(Loc.T("record.title"), string.Join("\n", lines), false, (Loc.T("record.ok"), () =>
            {
                _card.Hide();
                onClosed();
            }));
        }

        // ── 입력 명령 ──

        private async Task HandleCommand(Yarn.Command command, CancellationToken token)
        {
            var parts = command.Text.Split(' ');
            if (parts.Length < 2 || parts[0] != "input")
            {
                Debug.LogWarning("[Prologue] 알 수 없는 명령: " + command.Text);
                return;
            }

            var done = new TaskCompletionSource<bool>();
            _input.Clear();
            _input.RemoveFromClassList("hidden");
            switch (parts[1])
            {
                case "name":
                    TextInput("prologue.input_name", _profile.Name, 8, value => _profile.Name = value, done);
                    break;
                case "contact":
                    TextInput("prologue.input_contact", _friend.ContactName, 10, value =>
                    {
                        _friend.ContactName = value;
                        _friend.CallName = value;
                    }, done);
                    break;
                case "birthday":
                    BirthdayInput(done);
                    break;
                case "personality":
                    PersonalityInput(done);
                    break;
                default:
                    done.TrySetResult(true);
                    break;
            }

            using (token.Register(() => done.TrySetCanceled()))
            {
                await done.Task;
            }

            _input.Clear();
            _input.AddToClassList("hidden");
        }

        private void TextInput(string titleKey, string initial, int maxLength, Action<string> apply, TaskCompletionSource<bool> done)
        {
            _input.Add(Title(titleKey));
            var row = Row();
            var field = new TextField { value = initial, maxLength = maxLength };
            field.AddToClassList("prologue-field");
            row.Add(field);
            row.Add(Confirm(() =>
            {
                var value = (field.value ?? "").Trim();
                if (value.Length == 0)
                {
                    return;
                }

                apply(value);
                done.TrySetResult(true);
            }));
            _input.Add(row);
            field.schedule.Execute(() => field.Focus());
        }

        private void BirthdayInput(TaskCompletionSource<bool> done)
        {
            _input.Add(Title("prologue.input_birthday"));
            var row = Row();
            var months = Enumerable.Range(1, 12).Select(m => Loc.F("prologue.month", m)).ToList();
            var days = Enumerable.Range(1, 31).Select(d => Loc.F("prologue.day", d)).ToList();
            var month = new DropdownField(months, 4);
            var day = new DropdownField(days, 14);
            month.AddToClassList("prologue-field");
            day.AddToClassList("prologue-field");
            row.Add(month);
            row.Add(day);
            row.Add(Confirm(() =>
            {
                _profile.BirthMonth = month.index + 1;
                _profile.BirthDay = Math.Min(day.index + 1, DaysIn(_profile.BirthMonth));
                done.TrySetResult(true);
            }));
            _input.Add(row);
        }

        private void PersonalityInput(TaskCompletionSource<bool> done)
        {
            _input.Add(Title("prologue.input_personality"));
            var pairs = new[] { ("E", "I"), ("S", "N"), ("T", "F"), ("J", "P") };
            var chosen = new[] { "E", "S", "T", "J" };
            var row = Row();
            for (var i = 0; i < pairs.Length; i++)
            {
                var index = i;
                var (left, right) = pairs[i];
                var a = new Button { text = left };
                var b = new Button { text = right };
                a.AddToClassList("prologue-axis");
                b.AddToClassList("prologue-axis");
                void Paint()
                {
                    a.EnableInClassList("selected", chosen[index] == left);
                    b.EnableInClassList("selected", chosen[index] == right);
                }

                a.clicked += () =>
                {
                    chosen[index] = left;
                    Paint();
                };
                b.clicked += () =>
                {
                    chosen[index] = right;
                    Paint();
                };
                Paint();
                row.Add(a);
                row.Add(b);
            }

            row.Add(Confirm(() =>
            {
                foreach (var letter in chosen)
                {
                    _picks.Add(("PERSONALITY", letter));
                }

                done.TrySetResult(true);
            }));
            _input.Add(row);
        }

        private static int DaysIn(int month) => new[] { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 }[month - 1];

        private static Label Title(string key)
        {
            var label = new Label(Loc.T(key));
            label.AddToClassList("prologue-input-title");
            return label;
        }

        private static VisualElement Row()
        {
            var row = new VisualElement();
            row.AddToClassList("prologue-row");
            return row;
        }

        private static Button Confirm(Action onClick)
        {
            var button = new Button(onClick) { text = Loc.T("common.ok") };
            button.AddToClassList("small-button");
            return button;
        }
    }
}
