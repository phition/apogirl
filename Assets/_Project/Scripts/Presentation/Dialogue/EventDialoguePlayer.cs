using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApoGirl.Core.Events;
using ApoGirl.Presentation.Text;
using UnityEngine;
using Yarn;
using Yarn.Markup;
using Yarn.Unity;

namespace ApoGirl.Presentation.Dialogue
{
    /// <summary>화면에 보일 선택지 하나.</summary>
    public sealed class DialogueChoice
    {
        public int OptionId;
        public string Text = "";
        public bool Enabled = true;

        /// <summary>이벤트 데이터의 선택지 ID (Yarn 태그 #choice:ID). 없으면 대사 분기용 선택지.</summary>
        public string ChoiceId = "";

        /// <summary>판정이 있는 선택지의 성공 확률 (없으면 null).</summary>
        public float? Chance;
    }

    /// <summary>대화 화면. 줄 표시 후 플레이어가 넘길 때까지, 선택지는 고를 때까지 기다린다.</summary>
    public interface IDialogueView
    {
        void Open();

        void Close();

        Task ShowLine(string speaker, string text, CancellationToken token);

        Task<int> ShowOptions(IReadOnlyList<DialogueChoice> choices, CancellationToken token);
    }

    /// <summary>
    /// Yarn 노드 하나를 실행하는 진행기 (Yarn 코어 VM을 직접 구동, DialogueRunner는 쓰지 않는다).
    /// 대사 원문은 Unity Localization "Dialogue" 테이블 → 없으면 Yarn 프로젝트 기본 언어(ko) 순으로 찾는다.
    /// 선택지의 #choice:ID 태그는 이벤트 데이터의 선택지와 연결되고, 고르는 즉시 onChoice로 알린다.
    /// </summary>
    public sealed class EventDialoguePlayer : IDialogueResponder
    {
        public const string DialogueTable = "Dialogue";
        public const string ChoiceTagPrefix = "choice:";
        private const string LocaleCode = "ko";

        private readonly YarnProject _project;
        private readonly IDialogueView _view;
        private readonly Yarn.Dialogue _dialogue;
        private readonly LineParser _parser = new LineParser();
        private IReadOnlyList<EventChoiceView> _eventChoices = Array.Empty<EventChoiceView>();

        /// <summary>Yarn 명령 처리 (예: 프롤로그의 &lt;&lt;input name&gt;&gt;). null이면 경고만 남긴다.</summary>
        public Func<Command, CancellationToken, Task> CommandHandler { get; set; }
        private Action<string> _onChoice;

        public EventDialoguePlayer(YarnProject project, IDialogueView view)
        {
            _project = project ? project : throw new ArgumentNullException(nameof(project));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            var storage = new MemoryVariableStore { Program = project.Program };
            _dialogue = new Yarn.Dialogue(storage)
            {
                Responder = this,
                Program = project.Program,
                LogErrorMessage = message => Debug.LogError("[Yarn] " + message),
                LogDebugMessage = _ => { },
            };

            var replacer = new BuiltInMarkupReplacer();
            _parser.RegisterMarkerProcessor("select", replacer);
            _parser.RegisterMarkerProcessor("plural", replacer);
            _parser.RegisterMarkerProcessor("ordinal", replacer);
        }

        /// <summary>마지막으로 고른 이벤트 선택지의 결과 (Yarn 함수 succeeded() 등에서 읽는다).</summary>
        public static EventChoiceResult LastChoiceResult { get; set; }

        public bool HasNode(string node) => _project.Program != null && _project.Program.Nodes.ContainsKey(node ?? "");

        /// <summary>노드를 끝까지 실행한다. eventChoices: 지금 이벤트의 선택지 (숨김 제외).</summary>
        public async Task Run(string node, IReadOnlyList<EventChoiceView> eventChoices, Action<string> onChoice, CancellationToken token)
        {
            _eventChoices = eventChoices; // null = 데이터 선택지와 대조하지 않음 (프롤로그)
            _onChoice = onChoice;
            _view.Open();
            try
            {
                await _dialogue.SetNode(node, token);
                await _dialogue.Start(token);
            }
            finally
            {
                _view.Close();
            }
        }

        // ── IDialogueResponder ──

        public async ValueTask HandleLine(Line line, CancellationToken token)
        {
            var (speaker, text) = Localize(line);
            await _view.ShowLine(speaker, text, token);
        }

        public async ValueTask<int> HandleOptions(OptionSet options, CancellationToken token)
        {
            var choices = new List<DialogueChoice>();
            foreach (var option in options.Options)
            {
                var choiceId = ChoiceTag(option.Line.ID);
                var choice = new DialogueChoice { OptionId = option.ID, Text = Localize(option.Line).Text, Enabled = option.IsAvailable, ChoiceId = choiceId ?? "" };
                if (choiceId != null && _eventChoices != null)
                {
                    var data = _eventChoices.FirstOrDefault(c => c.Id == choiceId);
                    if (data == null)
                    {
                        continue; // 조건이 안 맞아 숨긴 선택지
                    }

                    choice.Enabled &= data.Enabled;
                    choice.Chance = data.Def.HasCheck ? data.Chance : (float?)null;
                }
                else if (!option.IsAvailable)
                {
                    continue;
                }

                choices.Add(choice);
            }

            if (choices.Count == 0)
            {
                return Yarn.Dialogue.NoOptionSelected;
            }

            var selected = await _view.ShowOptions(choices, token);
            var picked = choices.First(c => c.OptionId == selected);
            if (!string.IsNullOrEmpty(picked.ChoiceId))
            {
                _onChoice?.Invoke(picked.ChoiceId);
            }

            return selected;
        }

        public async ValueTask HandleCommand(Command command, CancellationToken token)
        {
            if (CommandHandler != null)
            {
                await CommandHandler(command, token);
                return;
            }

            Debug.LogWarning("[Yarn] 처리하지 않는 명령: " + command.Text);
        }

        public ValueTask HandleNodeStart(string node, CancellationToken token) => default;

        public ValueTask HandleNodeComplete(string node, CancellationToken token) => default;

        public ValueTask HandleDialogueComplete() => default;

        public ValueTask PrepareForLines(List<string> lineIDs, CancellationToken token) => default;

        public bool CanCallFunction(string functionName)
        {
            return functionName == StandardLibrary.EngineProvidedFunctionNames.Visited
                   || functionName == StandardLibrary.EngineProvidedFunctionNames.VisitedCount
                   || YarnFunctions.Has(functionName);
        }

        public ValueTask<IConvertible> InvokeFunction(string functionName, IConvertible[] parameters, CancellationToken token)
        {
            if (functionName == StandardLibrary.EngineProvidedFunctionNames.Visited)
            {
                return new ValueTask<IConvertible>(_dialogue.GetNodeVisitCount(parameters[0].ToString(System.Globalization.CultureInfo.InvariantCulture)) > 0);
            }

            if (functionName == StandardLibrary.EngineProvidedFunctionNames.VisitedCount)
            {
                return new ValueTask<IConvertible>((int)_dialogue.GetNodeVisitCount(parameters[0].ToString(System.Globalization.CultureInfo.InvariantCulture)));
            }

            return new ValueTask<IConvertible>(YarnFunctions.Invoke(functionName, parameters));
        }

        // ── 문자열 ──

        private (string Speaker, string Text) Localize(Line line)
        {
            var source = ResolveShadow(line.ID);
            var raw = Loc.Lookup(DialogueTable, source) ?? _project.baseLocalization?.GetLocalizedString(source);
            if (raw == null)
            {
                Debug.LogWarning("[Yarn] 대사 없음: " + line.ID);
                return ("", line.ID);
            }

            var parsed = _parser.ParseString(LineParser.ExpandSubstitutions(raw, line.Substitutions), LocaleCode);
            var speaker = "";
            if (parsed.TryGetAttributeWithName("character", out var character))
            {
                if (character.Properties.TryGetValue("name", out var name))
                {
                    speaker = name.StringValue;
                }

                parsed = parsed.DeleteRange(character);
            }

            return (speaker, parsed.Text.Trim());
        }

        private string ResolveShadow(string lineId)
        {
            return _project.lineMetadata?.GetShadowLineSource(lineId) ?? lineId;
        }

        private string ChoiceTag(string lineId)
        {
            var metadata = _project.lineMetadata?.GetMetadata(lineId);
            if (metadata == null)
            {
                return null;
            }

            foreach (var tag in metadata)
            {
                if (tag.StartsWith(ChoiceTagPrefix, StringComparison.Ordinal))
                {
                    return tag.Substring(ChoiceTagPrefix.Length);
                }
            }

            return null;
        }
    }
}
