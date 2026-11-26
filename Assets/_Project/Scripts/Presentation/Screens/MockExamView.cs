using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApoGirl.Presentation.Art;
using ApoGirl.Presentation.Dialogue;
using ApoGirl.Presentation.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace ApoGirl.Presentation.Screens
{
    /// <summary>모의고사 문항 데이터 (Resources/Exam/mock_exam.json). 과목마다 문항을 넉넉히 두고 시험 때 PerSubject개씩 무작위로 낸다.</summary>
    [Serializable]
    public sealed class MockExamConfig
    {
        [Serializable]
        public sealed class Question
        {
            /// <summary>지문 (없으면 빈칸).</summary>
            public string Passage = "";
            public string Stem = "";
            public string[] Choices = Array.Empty<string>();

            /// <summary>정답 번호 (1~5).</summary>
            public int Answer;
        }

        [Serializable]
        public sealed class Subject
        {
            /// <summary>과목 ID. 이름은 문구 exam.subject.{Id}.</summary>
            public string Id = "";
            public Question[] Questions = Array.Empty<Question>();
        }

        public int PerSubject = 5;
        public int PointsPerQuestion = 4;
        public Subject[] Subjects = Array.Empty<Subject>();

        public static MockExamConfig Load()
        {
            var asset = Resources.Load<TextAsset>("Exam/mock_exam");
            return asset != null ? JsonUtility.FromJson<MockExamConfig>(asset.text) ?? new MockExamConfig() : new MockExamConfig();
        }

        /// <summary>과목마다 PerSubject개씩 무작위로 뽑은 시험지 (과목 순서는 그대로 = 교시 순서).</summary>
        public List<(Subject Subject, List<Question> Questions)> Draw(System.Random random)
        {
            var paper = new List<(Subject, List<Question>)>();
            foreach (var subject in Subjects)
            {
                var picked = subject.Questions.OrderBy(_ => random.Next()).Take(Math.Max(1, PerSubject)).ToList();
                paper.Add((subject, picked));
            }

            return paper;
        }
    }

    /// <summary>
    /// 수능날 모의고사 (사용자 요청 2026-11-26): 책상 위 시험지 한 장. 교시마다 과목 하나, 객관식 답을 눌러 표시하고
    /// 마지막 교시 뒤 채점한다. 점수는 게임 수치에 쓰이지 않는다 — 대본이 exam_score()로 선생님의 말만 가른다.
    /// 대본 명령 &lt;&lt;mock_exam&gt;&gt;이 Run을 부르고 끝날 때까지 기다린다 (HubScreen의 대화 명령 처리).
    /// </summary>
    public sealed class MockExamView
    {
        private static readonly string[] Circled = { "①", "②", "③", "④", "⑤" };

        private readonly VisualElement _host;
        private MockExamConfig _config;
        private List<(MockExamConfig.Subject Subject, List<MockExamConfig.Question> Questions)> _paper;
        private int[][] _answers;
        private int _period;
        private VisualElement _overlay;
        private VisualElement _sheet;
        private TaskCompletionSource<int> _done;

        public MockExamView(VisualElement host)
        {
            _host = host;
        }

        /// <summary>마지막으로 본 시험 점수 (0~100). 안 봤으면 -1. 대본 함수 exam_score()가 읽는다.</summary>
        public static int LastScore { get; set; } = -1;

        /// <summary>시험을 치르고 점수(0~100)를 돌려준다. 문항이 없으면 바로 -1.</summary>
        public Task<int> Run()
        {
            _config = MockExamConfig.Load();
            _paper = _config.Draw(new System.Random());
            if (_paper.Count == 0)
            {
                return Task.FromResult(-1);
            }

            _answers = _paper.Select(p => new int[p.Questions.Count]).ToArray();
            _period = 0;
            _done = new TaskCompletionSource<int>();

            _overlay = new VisualElement { name = "mock-exam" };
            _overlay.AddToClassList("exam-overlay");
            StoryArt.SetCover(_overlay, StoryArt.Ui("Paper/ui_desk_wood"));
            var shade = new VisualElement { pickingMode = PickingMode.Ignore };
            shade.AddToClassList("exam-shade");
            _overlay.Add(shade);
            _sheet = new VisualElement();
            _sheet.AddToClassList("exam-paper");
            _overlay.Add(_sheet);
            _host.Add(_overlay);
            ShowPeriod();
            return _done.Task;
        }

        // ── 문제지 한 교시 ──

        private void ShowPeriod()
        {
            _sheet.Clear();
            var (subject, questions) = _paper[_period];
            var subjectName = Loc.T("exam.subject." + subject.Id);

            AddClip();
            var head = Row("exam-head");
            var titles = new VisualElement();
            titles.AddToClassList("exam-titles");
            titles.Add(Text(Loc.T("exam.kicker"), "exam-kicker"));
            var line = Row("exam-title-row");
            line.Add(Text(Loc.F("exam.period", _period + 1), "exam-period"));
            line.Add(Text(Loc.F("exam.area", subjectName), "exam-subject"));
            titles.Add(line);
            head.Add(titles);
            var name = new VisualElement();
            name.AddToClassList("exam-namebox");
            name.Add(Text(Loc.T("exam.name"), "exam-name-label"));
            name.Add(Text(YarnFunctions.Name(), "exam-name-value"));
            head.Add(name);
            _sheet.Add(head);
            _sheet.Add(Rule());
            _sheet.Add(Text(Loc.F("exam.notice", questions.Count, _config.PointsPerQuestion), "exam-notice"));

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList("exam-body");
            for (var i = 0; i < questions.Count; i++)
            {
                scroll.Add(BuildQuestion(i, questions[i]));
            }

            _sheet.Add(scroll);

            var foot = Row("exam-foot");
            var progress = Text("", "exam-progress");
            foot.Add(progress);
            var last = _period == _paper.Count - 1;
            var next = new Button(() =>
            {
                if (last)
                {
                    ShowResult();
                }
                else
                {
                    _period++;
                    ShowPeriod();
                }
            }) { text = Loc.T(last ? "exam.submit" : "exam.next") };
            next.AddToClassList("exam-next");
            foot.Add(next);
            _sheet.Add(foot);
            _progress = progress;
            UpdateProgress();
        }

        private Label _progress;

        private void UpdateProgress()
        {
            var answered = _answers[_period].Count(a => a > 0);
            _progress.text = Loc.F("exam.progress", answered, _answers[_period].Length);
        }

        private VisualElement BuildQuestion(int index, MockExamConfig.Question question)
        {
            var box = new VisualElement();
            box.AddToClassList("exam-q");
            if (!string.IsNullOrEmpty(question.Passage))
            {
                box.Add(Text(question.Passage, "exam-passage"));
            }

            var stem = Row("exam-stem-row");
            stem.Add(Text((index + 1) + ".", "exam-number"));
            stem.Add(Text(question.Stem, "exam-stem"));
            box.Add(stem);

            var choices = new VisualElement();
            choices.AddToClassList("exam-choices");
            var buttons = new List<Button>();
            for (var c = 0; c < question.Choices.Length && c < Circled.Length; c++)
            {
                var pick = c + 1;
                var button = new Button();
                button.AddToClassList("exam-choice");
                var mark = new VisualElement { pickingMode = PickingMode.Ignore };
                mark.AddToClassList("exam-mark");
                var number = Text(Circled[c], "exam-choice-number");
                number.pickingMode = PickingMode.Ignore;
                var text = Text(question.Choices[c], "exam-choice-text");
                text.pickingMode = PickingMode.Ignore;
                button.Add(mark);
                button.Add(number);
                button.Add(text);
                button.clicked += () =>
                {
                    // 같은 답을 다시 누르면 지운다
                    _answers[_period][index] = _answers[_period][index] == pick ? 0 : pick;
                    for (var b = 0; b < buttons.Count; b++)
                    {
                        buttons[b].EnableInClassList("selected", _answers[_period][index] == b + 1);
                    }

                    UpdateProgress();
                };
                button.EnableInClassList("selected", _answers[_period][index] == pick);
                buttons.Add(button);
                choices.Add(button);
            }

            box.Add(choices);
            return box;
        }

        // ── 채점 ──

        private void ShowResult()
        {
            _sheet.Clear();
            AddClip();
            _sheet.Add(Text(Loc.T("exam.result_title"), "exam-result-title"));
            _sheet.Add(Rule());

            var total = 0;
            var count = 0;
            var table = new VisualElement();
            table.AddToClassList("exam-result-table");
            for (var p = 0; p < _paper.Count; p++)
            {
                var (subject, questions) = _paper[p];
                var row = Row("exam-result-row");
                row.Add(Text(Loc.F("exam.period", p + 1), "exam-result-period"));
                row.Add(Text(Loc.T("exam.subject." + subject.Id), "exam-result-subject"));
                var marks = Row("exam-result-marks");
                var correct = 0;
                for (var i = 0; i < questions.Count; i++)
                {
                    var right = _answers[p][i] == questions[i].Answer;
                    correct += right ? 1 : 0;
                    var mark = new VisualElement();
                    mark.AddToClassList("exam-result-mark");
                    mark.AddToClassList(right ? "right" : "wrong");
                    mark.tooltip = Loc.F("exam.answer_tip", i + 1, questions[i].Answer);
                    marks.Add(mark);
                }

                row.Add(marks);
                row.Add(Text(Loc.F("exam.result_score", correct, questions.Count, correct * _config.PointsPerQuestion), "exam-result-score"));
                table.Add(row);
                total += correct;
                count += questions.Count;
            }

            _sheet.Add(table);
            _sheet.Add(Rule());
            var score = total * _config.PointsPerQuestion;
            var full = count * _config.PointsPerQuestion;
            var sum = Row("exam-total-row");
            sum.Add(Text(Loc.T("exam.total"), "exam-total-label"));
            sum.Add(Text(Loc.F("exam.total_score", score, full), "exam-total"));
            _sheet.Add(sum);
            _sheet.Add(Text(Loc.T("exam.result_note"), "exam-result-note"));

            // 잘 본 시험지에만 도장을 찍는다 (못 봐도 나무라는 도장은 없다)
            var stamp = score * 100 >= full * 80 ? "Paper/ui_stamp_great" : score * 100 >= full * 50 ? "Paper/ui_stamp_good" : null;
            if (stamp != null)
            {
                var image = new VisualElement { pickingMode = PickingMode.Ignore };
                image.AddToClassList("exam-stamp");
                StoryArt.SetContain(image, StoryArt.Ui(stamp));
                _sheet.Add(image);
            }

            var foot = Row("exam-foot");
            foot.Add(Text("", "exam-progress"));
            var close = new Button(() => Finish(score)) { text = Loc.T("exam.close") };
            close.AddToClassList("exam-next");
            foot.Add(close);
            _sheet.Add(foot);
        }

        private void Finish(int score)
        {
            LastScore = score;
            _overlay.RemoveFromHierarchy();
            _overlay = null;
            _done.TrySetResult(score);
        }

        // ── 작은 도구 ──

        private void AddClip()
        {
            var clip = new VisualElement { pickingMode = PickingMode.Ignore };
            clip.AddToClassList("exam-clip");
            StoryArt.SetContain(clip, StoryArt.Ui("Paper/ui_paper_clip"));
            _sheet.Add(clip);
        }

        private static VisualElement Row(string cssClass)
        {
            var row = new VisualElement();
            row.AddToClassList(cssClass);
            return row;
        }

        private static VisualElement Rule()
        {
            var rule = new VisualElement();
            rule.AddToClassList("exam-rule");
            return rule;
        }

        private static Label Text(string text, string cssClass)
        {
            var label = new Label(text);
            label.AddToClassList(cssClass);
            return label;
        }
    }
}
