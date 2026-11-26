using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ApoGirl.Core.Rules;
using ApoGirl.Presentation.Dialogue;
using ApoGirl.Presentation.Text;
using ApoGirl.Systems.Session;
using UnityEngine;
using UnityEngine.UIElements;

namespace ApoGirl.Presentation.Screens
{
    /// <summary>PC통신 '이제누리' 화면 데이터 (Resources/Bbs/bbs.json): 접속 시간, 전화 거는 글줄, 메뉴, 공지, 게시판, 대화방.</summary>
    [Serializable]
    public sealed class BbsConfig
    {
        [Serializable]
        public sealed class MenuItem
        {
            public string Key = "";
            public string Title = "";

            /// <summary>notice · wiki · board · chat · help</summary>
            public string Kind = "";

            /// <summary>GO 뒤에 치는 이름.</summary>
            public string Go = "";
        }

        [Serializable]
        public sealed class Post
        {
            public string Title = "";
            public string Date = "";
            public string Author = "";
            public string[] Body = Array.Empty<string>();
        }

        public string Service = "이제누리";
        public string Tagline = "";
        public string Number = "01420";

        /// <summary>한 주에 컴퓨터가 버티는 시간(초). 다 쓰면 멎는다. 화면에는 보여 주지 않는다.</summary>
        public float SessionSeconds = 180f;
        public int PageRows = 17;
        public int Columns = 76;
        public string[] Dial = Array.Empty<string>();
        public string[] Banner = Array.Empty<string>();
        public MenuItem[] Menu = Array.Empty<MenuItem>();
        public string[] Help = Array.Empty<string>();
        public Post[] Notices = Array.Empty<Post>();
        public Post[] Posts = Array.Empty<Post>();
        public string[] ChatIntro = Array.Empty<string>();
        public string[] ChatSilence = Array.Empty<string>();

        public static BbsConfig Load()
        {
            var asset = Resources.Load<TextAsset>("Bbs/bbs");
            return asset != null ? JsonUtility.FromJson<BbsConfig>(asset.text) ?? new BbsConfig() : new BbsConfig();
        }
    }

    /// <summary>생존 백과 = 게임 위키 (Docs/Wiki → Tools/sync_wiki.py → Resources/Bbs/wiki.json).</summary>
    [Serializable]
    public sealed class BbsWiki
    {
        [Serializable]
        public sealed class Page
        {
            public string Id = "";
            public string Title = "";
            public string Category = "";

            /// <summary>none · light · heavy (heavy는 목록에 [!]).</summary>
            public string Spoiler = "none";
            public string Body = "";
        }

        public Page[] Pages = Array.Empty<Page>();

        public static BbsWiki Load()
        {
            var asset = Resources.Load<TextAsset>("Bbs/wiki");
            return asset != null ? JsonUtility.FromJson<BbsWiki>(asset.text) ?? new BbsWiki() : new BbsWiki();
        }
    }

    /// <summary>
    /// PC통신 '이제누리' (사용자 요청 2026-11-26): 파란 글자 화면(둥근모꼴), 명령을 타이핑해 쓴다.
    /// 번호 = 메뉴·글, P = 앞 화면, T = 처음, N/B = 쪽 넘김, GO 이름, S 낱말(백과 찾기), W 글쓰기, H 도움말, X 끊기.
    /// 컴퓨터 전력이 불안정해 한 주에 SessionSeconds만 버틴다 — 남은 시간은 보여 주지 않고, 다 쓰면 WentDown을 부른다.
    /// 내가 쓴 게시판 글은 persistentDataPath/Bbs.json에 남는다 (모든 판 공통).
    /// </summary>
    public sealed class BbsView
    {
        // 이번 주에 남은 시간 (접속을 끊었다가 다시 들어와도 이어진다)
        private static int _budgetWeek = -1;
        private static float _budget;

        private readonly VisualElement _host;
        private readonly GameSession _session;
        private BbsConfig _config;
        private BbsWiki _wiki;
        private VisualElement _overlay;
        private Label _screen;
        private Label _prompt;
        private TextField _field;
        private IVisualElementScheduledItem _clock;
        private float _lastTick;

        // 지금 화면
        private string _title = "";
        private List<string> _rows = new List<string>();
        private int _page;
        private string _hint = "";
        private string _notice = "";
        private Func<string, bool> _handler;
        private Func<string, bool> _raw;   // 대화방·글쓰기: 명령 대신 글을 그대로 받는다
        private readonly Stack<Action> _back = new Stack<Action>();
        private Action _current;

        public BbsView(VisualElement host, GameSession session)
        {
            _host = host;
            _session = session;
        }

        public bool IsOpen => _overlay != null;

        /// <summary>컴퓨터가 멎었다 (시간을 다 썼다).</summary>
        public Action WentDown { get; set; }

        public void Open()
        {
            if (IsOpen || _session.State == null)
            {
                return;
            }

            _config = BbsConfig.Load();
            _wiki = BbsWiki.Load();
            var week = _session.State.Calendar.Week;
            if (week != _budgetWeek)
            {
                _budgetWeek = week;
                _budget = _config.SessionSeconds;
            }

            _overlay = new VisualElement { name = "bbs" };
            _overlay.AddToClassList("bbs-overlay");
            // 글꼴은 Theme.uss(.bbs-overlay)가 둥근모꼴(UI/Fonts/DungGeunMo.ttf, 고정폭)로 정한다
            var frame = new VisualElement();
            frame.AddToClassList("bbs-frame");
            _screen = new Label { enableRichText = false };
            _screen.AddToClassList("bbs-screen");
            frame.Add(_screen);
            var line = new VisualElement();
            line.AddToClassList("bbs-input-row");
            _prompt = new Label(Loc.T("bbs.prompt")) { enableRichText = false };
            _prompt.AddToClassList("bbs-prompt");
            _field = new TextField { maxLength = 60 };
            _field.AddToClassList("bbs-field");
            _field.RegisterCallback<KeyDownEvent>(OnKey, TrickleDown.TrickleDown);
            line.Add(_prompt);
            line.Add(_field);
            frame.Add(line);
            _overlay.Add(frame);
            _host.Add(_overlay);
            _back.Clear();
            _raw = null;
            _lastTick = Time.unscaledTime;
            _clock = _overlay.schedule.Execute(Tick).Every(250);
            Dial();
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            _clock?.Pause();
            _overlay.RemoveFromHierarchy();
            _overlay = null;
        }

        // ── 시간: 컴퓨터는 오래 버티지 못한다 ──

        private void Tick()
        {
            var now = Time.unscaledTime;
            _budget -= now - _lastTick;
            _lastTick = now;
            if (_budget <= 0f)
            {
                Close();
                _session.BbsWentDown();
                WentDown?.Invoke();
            }
        }

        // ── 입력 ──

        private void OnKey(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                evt.StopPropagation();
                var text = _field.value ?? "";
                _field.value = "";
                Submit(text);
                Focus();
            }
            else if (evt.keyCode == KeyCode.Escape)
            {
                evt.StopPropagation();
                Close();
            }
        }

        /// <summary>명령 한 줄을 처리한다 (시험·자동화에서도 부른다).</summary>
        public void Submit(string text)
        {
            if (!IsOpen || !_field.enabledSelf)
            {
                return;
            }

            _notice = "";
            if (_raw != null)
            {
                if (!_raw(text))
                {
                    Render();
                }

                return;
            }

            var command = (text ?? "").Trim();
            var upper = command.ToUpperInvariant();
            if (upper.Length == 0)
            {
                Render();
                return;
            }

            if (upper == "X" || upper == "BYE" || upper == "Q")
            {
                Close();
                return;
            }

            if (upper == "T")
            {
                _back.Clear();
                ShowMain();
                return;
            }

            if (upper == "P")
            {
                if (_back.Count > 0)
                {
                    _back.Pop()();
                }
                else
                {
                    ShowMain();
                }

                return;
            }

            if (upper == "N" || upper == "B")
            {
                var pages = PageCount();
                _page = Mathf.Clamp(_page + (upper == "N" ? 1 : -1), 0, pages - 1);
                Render();
                return;
            }

            if (upper == "H" || upper == "?")
            {
                Go(ShowHelp);
                return;
            }

            if (upper.StartsWith("GO "))
            {
                var name = upper.Substring(3).Trim();
                var item = _config.Menu.FirstOrDefault(m => string.Equals(m.Go, name, StringComparison.OrdinalIgnoreCase));
                if (item != null)
                {
                    _back.Clear();
                    _current = ShowMain;
                    Go(() => Enter(item));
                    return;
                }
            }

            if (upper.StartsWith("S ") && command.Length > 2)
            {
                var word = command.Substring(2).Trim();
                Go(() => ShowSearch(word));
                return;
            }

            if (_handler != null && _handler(upper))
            {
                return;
            }

            _notice = Loc.T("bbs.unknown");
            Render();
        }

        private void Focus()
        {
            if (IsOpen)
            {
                _field.schedule.Execute(() => _field.Focus()).StartingIn(30);
            }
        }

        // ── 화면 옮기기 ──

        /// <summary>새 화면으로 들어간다 (P로 지금 화면에 돌아올 수 있게 쌓는다).</summary>
        private void Go(Action next)
        {
            if (_current != null)
            {
                _back.Push(_current);
            }

            next();
        }

        private void Set(Action self, string title, IEnumerable<string> lines, string hint, Func<string, bool> handler)
        {
            _current = self;
            _title = title;
            _rows = Wrap(lines);
            _page = 0;
            _hint = hint;
            _handler = handler;
            Render();
        }

        private int PageCount() => Mathf.Max(1, Mathf.CeilToInt(_rows.Count / (float)Mathf.Max(1, _config.PageRows)));

        private void Render()
        {
            if (!IsOpen)
            {
                return;
            }

            var text = new StringBuilder();
            text.Append(" 〈").Append(_config.Service).Append("〉  ").Append(_title).Append('\n');
            text.Append(' ').Append(new string('─', _config.Columns / 2 * 2 - 2)).Append('\n');
            var rows = Mathf.Max(1, _config.PageRows);
            var pages = PageCount();
            _page = Mathf.Clamp(_page, 0, pages - 1);
            for (var i = 0; i < rows; i++)
            {
                var index = _page * rows + i;
                text.Append(' ').Append(index < _rows.Count ? _rows[index] : "").Append('\n');
            }

            text.Append(' ').Append(new string('─', _config.Columns / 2 * 2 - 2)).Append('\n');
            text.Append(' ').Append(pages > 1 ? Loc.F("bbs.page", _page + 1, pages) + "   " : "").Append(_hint).Append('\n');
            text.Append(' ').Append(_notice);
            _screen.text = text.ToString();
        }

        // ── 접속 ──

        private void Dial()
        {
            _field.SetEnabled(false);
            _title = Loc.T("bbs.dialing");
            _rows = new List<string>();
            _hint = "";
            _handler = null;
            Render();
            var lines = _config.Dial;
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                _overlay.schedule.Execute(() =>
                {
                    _rows.Add(line);
                    Render();
                }).StartingIn(350 * (i + 1));
            }

            _overlay.schedule.Execute(() =>
            {
                if (!IsOpen)
                {
                    return;
                }

                _field.SetEnabled(true);
                ShowMain();
                Focus();
            }).StartingIn(350 * (lines.Length + 2));
        }

        private void ShowMain()
        {
            var lines = new List<string>(_config.Banner) { "" };
            foreach (var item in _config.Menu)
            {
                lines.Add("      " + item.Key + ".  " + item.Title + (string.IsNullOrEmpty(item.Go) ? "" : "   (GO " + item.Go.ToUpperInvariant() + ")"));
            }

            lines.Add("      X.  " + Loc.T("bbs.menu_quit"));
            lines.Add("");
            lines.Add("   " + Loc.F("bbs.welcome", YarnFunctions.Name()));
            _back.Clear();
            Set(ShowMain, Loc.T("bbs.main"), lines, Loc.T("bbs.hint.main"), command =>
            {
                var item = _config.Menu.FirstOrDefault(m => m.Key == command);
                if (item == null)
                {
                    return false;
                }

                Go(() => Enter(item));
                return true;
            });
        }

        private void Enter(BbsConfig.MenuItem item)
        {
            switch (item.Kind)
            {
                case "notice": ShowPosts(Loc.T("bbs.notice"), _config.Notices, false); break;
                case "wiki": ShowWikiCategories(); break;
                case "board": ShowBoard(); break;
                case "chat": ShowChat(); break;
                default: ShowHelp(); break;
            }
        }

        private void ShowHelp()
        {
            Set(ShowHelp, Loc.T("bbs.help"), _config.Help, Loc.T("bbs.hint.read"), null);
        }

        // ── 공지·게시판 ──

        private void ShowPosts(string title, IReadOnlyList<BbsConfig.Post> posts, bool board)
        {
            var lines = new List<string>();
            for (var i = 0; i < posts.Count; i++)
            {
                lines.Add(Pad((i + 1) + ".", 5) + Pad(posts[i].Date, 8) + Pad(posts[i].Author, 16) + posts[i].Title);
            }

            if (posts.Count == 0)
            {
                lines.Add(Loc.T("bbs.empty"));
            }

            Action self = () => ShowPosts(title, posts, board);
            Set(self, title, lines, Loc.T(board ? "bbs.hint.board" : "bbs.hint.list"), command =>
            {
                if (board && command == "W")
                {
                    Go(StartWrite);
                    return true;
                }

                if (!int.TryParse(command, out var number) || number < 1 || number > posts.Count)
                {
                    return false;
                }

                var post = posts[number - 1];
                Go(() => ShowPost(title, post));
                return true;
            });
        }

        private void ShowPost(string board, BbsConfig.Post post)
        {
            var lines = new List<string>
            {
                Loc.F("bbs.post_head", post.Title),
                Loc.F("bbs.post_by", post.Author, post.Date),
                "",
            };
            lines.AddRange(post.Body);
            Set(() => ShowPost(board, post), board, lines, Loc.T("bbs.hint.read"), null);
        }

        private void ShowBoard()
        {
            var posts = new List<BbsConfig.Post>(_config.Posts);
            posts.AddRange(BbsStore.Load().Posts);
            ShowPosts(Loc.T("bbs.board"), posts, true);
        }

        private void StartWrite()
        {
            string title = null;
            _current = ShowBoard;
            _title = Loc.T("bbs.write");
            _rows = Wrap(new[] { Loc.T("bbs.write_guide") });
            _page = 0;
            _hint = Loc.T("bbs.hint.write");
            _handler = null;
            _prompt.text = Loc.T("bbs.write_title");
            _raw = text =>
            {
                text = (text ?? "").Trim();
                if (text.Length == 0)
                {
                    // 빈 줄 = 그만둔다
                    EndRaw();
                    ShowBoard();
                    return true;
                }

                if (title == null)
                {
                    title = text;
                    _rows.Add(Loc.F("bbs.post_head", title));
                    _prompt.text = Loc.T("bbs.write_body");
                    return false;
                }

                var state = _session.State;
                BbsStore.Add(new BbsConfig.Post
                {
                    Title = title,
                    Author = YarnFunctions.Name(),
                    Date = Loc.F("bbs.week_date", state != null ? state.Calendar.Week : 0),
                    Body = new[] { text },
                });
                EndRaw();
                ShowBoard();
                _notice = Loc.T("bbs.write_done");
                Render();
                return true;
            };
            Render();
        }

        private void EndRaw()
        {
            _raw = null;
            _prompt.text = Loc.T("bbs.prompt");
        }

        // ── 대화방: 아무도 없다 ──

        private void ShowChat()
        {
            _current = ShowChat;
            _title = Loc.T("bbs.chat");
            _rows = Wrap(_config.ChatIntro);
            _rows.Add("");
            _page = 0;
            _hint = Loc.T("bbs.hint.chat");
            _handler = null;
            _prompt.text = Loc.T("bbs.chat_prompt");
            var said = 0;
            _raw = text =>
            {
                text = (text ?? "").Trim();
                if (text.Length == 0 || text.ToUpperInvariant() == "/Q")
                {
                    EndRaw();
                    if (_back.Count > 0)
                    {
                        _back.Pop()();
                    }
                    else
                    {
                        ShowMain();
                    }

                    return true;
                }

                _rows.AddRange(Wrap(new[] { Loc.F("bbs.chat_line", YarnFunctions.Given(), text) }));
                if (_config.ChatSilence.Length > 0)
                {
                    _rows.Add("  " + _config.ChatSilence[said % _config.ChatSilence.Length]);
                }

                said++;
                _page = PageCount() - 1;   // 늘 맨 아래 쪽
                return false;
            };
            Render();
        }

        // ── 생존 백과 (위키) ──

        private void ShowWikiCategories()
        {
            var categories = _wiki.Pages.Select(p => p.Category).Distinct().ToList();
            var lines = new List<string> { Loc.T("bbs.wiki_intro"), "" };
            for (var i = 0; i < categories.Count; i++)
            {
                var count = _wiki.Pages.Count(p => p.Category == categories[i]);
                lines.Add(Pad((i + 1) + ".", 5) + Pad(categories[i], 22) + Loc.F("bbs.wiki_count", count));
            }

            if (categories.Count == 0)
            {
                lines.Add(Loc.T("bbs.empty"));
            }

            Set(ShowWikiCategories, Loc.T("bbs.wiki"), lines, Loc.T("bbs.hint.wiki"), command =>
            {
                if (!int.TryParse(command, out var number) || number < 1 || number > categories.Count)
                {
                    return false;
                }

                var category = categories[number - 1];
                Go(() => ShowWikiList(Loc.T("bbs.wiki") + " ― " + category, _wiki.Pages.Where(p => p.Category == category).ToList()));
                return true;
            });
        }

        private void ShowWikiList(string title, IReadOnlyList<BbsWiki.Page> pages)
        {
            var lines = new List<string>();
            for (var i = 0; i < pages.Count; i++)
            {
                lines.Add(Pad((i + 1) + ".", 5) + (pages[i].Spoiler == "heavy" ? "[!] " : "    ") + pages[i].Title);
            }

            if (pages.Count == 0)
            {
                lines.Add(Loc.T("bbs.search_none"));
            }
            else
            {
                lines.Add("");
                lines.Add(Loc.T("bbs.wiki_spoiler"));
            }

            Set(() => ShowWikiList(title, pages), title, lines, Loc.T("bbs.hint.list"), command =>
            {
                if (!int.TryParse(command, out var number) || number < 1 || number > pages.Count)
                {
                    return false;
                }

                var page = pages[number - 1];
                Go(() => ShowWikiPage(page));
                return true;
            });
        }

        private void ShowWikiPage(BbsWiki.Page page)
        {
            Set(() => ShowWikiPage(page), Loc.T("bbs.wiki") + " ― " + page.Title, page.Body.Split('\n'), Loc.T("bbs.hint.read"), null);
        }

        private void ShowSearch(string word)
        {
            var found = _wiki.Pages
                .Where(p => p.Title.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0 || p.Body.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderByDescending(p => p.Title.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
            ShowWikiList(Loc.F("bbs.search_title", word, found.Count), found);
        }

        // ── 글자 폭 (한글 2칸, 영문 1칸) ──

        private static int Width(char c)
        {
            if (c < 0x1100)
            {
                return 1;                                  // 라틴 글자·숫자·기본 기호
            }

            if (c >= 0x2000 && c <= 0x206F)
            {
                return 1;                                  // 줄표·말줄임표 같은 문장 부호
            }

            return 2;                                      // 한글·한자·선 그리기·도형·전각 기호
        }

        private static int Width(string text)
        {
            var width = 0;
            foreach (var c in text)
            {
                width += Width(c);
            }

            return width;
        }

        private static string Pad(string text, int columns)
        {
            text ??= "";
            var width = Width(text);
            return width >= columns ? text + " " : text + new string(' ', columns - width);
        }

        /// <summary>화면 폭에 맞춰 줄을 접는다 (쪽 수를 정확히 세려고 미리 접어 둔다).</summary>
        private List<string> Wrap(IEnumerable<string> lines)
        {
            var result = new List<string>();
            var max = Mathf.Max(20, _config.Columns);
            foreach (var raw in lines ?? Enumerable.Empty<string>())
            {
                var line = (raw ?? "").Replace("\r", "");
                if (Width(line) <= max)
                {
                    result.Add(line);
                    continue;
                }

                var indent = new string(' ', Mathf.Min(6, line.Length - line.TrimStart().Length + (line.TrimStart().StartsWith("·") ? 2 : 0)));
                var current = new StringBuilder();
                var width = 0;
                foreach (var c in line)
                {
                    var w = Width(c);
                    if (width + w > max)
                    {
                        result.Add(current.ToString());
                        current.Clear();
                        current.Append(indent);
                        width = indent.Length;
                    }

                    current.Append(c);
                    width += w;
                }

                if (current.Length > 0)
                {
                    result.Add(current.ToString());
                }
            }

            return result;
        }
    }

    /// <summary>내가 열린 게시판에 쓴 글 (persistentDataPath/Bbs.json, 모든 판 공통 — 음악실 기록과 같은 방식).</summary>
    [Serializable]
    public sealed class BbsStore
    {
        private const int MaxPosts = 30;

        public List<BbsConfig.Post> Posts = new List<BbsConfig.Post>();

        private static string FilePath => Path.Combine(Application.persistentDataPath, "Bbs.json");

        public static BbsStore Load()
        {
            try
            {
                return File.Exists(FilePath) ? JsonUtility.FromJson<BbsStore>(File.ReadAllText(FilePath)) ?? new BbsStore() : new BbsStore();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("PC통신 글 불러오기 실패: " + exception.Message);
                return new BbsStore();
            }
        }

        public static void Add(BbsConfig.Post post)
        {
            var store = Load();
            store.Posts.Add(post);
            if (store.Posts.Count > MaxPosts)
            {
                store.Posts.RemoveRange(0, store.Posts.Count - MaxPosts);
            }

            try
            {
                File.WriteAllText(FilePath, JsonUtility.ToJson(store));
            }
            catch (Exception exception)
            {
                Debug.LogWarning("PC통신 글 저장 실패: " + exception.Message);
            }
        }
    }
}
