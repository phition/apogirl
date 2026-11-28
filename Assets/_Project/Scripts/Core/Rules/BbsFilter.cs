using System;
using System.Collections.Generic;
using System.Text;

namespace ApoGirl.Core.Rules
{
    /// <summary>
    /// PC통신 열린 게시판의 금칙어 필터 (사용자 요청 2026-11-28). 올릴 때는 걸리면 거절하고(<see cref="Hits"/>),
    /// 받은 글을 보여 줄 때는 그 낱말만 가린다(<see cref="Mask"/>) — 고친 게임으로 올린 글도 화면에서는 가려진다.
    /// 비교하는 법 (<see cref="Normalize"/>):
    /// - 글자만 본다. 기호·숫자·보이지 않는 글자는 뺀다 ("시@발", "시1발" → "시발"). 영문은 소문자로, 전각 영문은 반각으로.
    /// - 띄어쓰기는 지킨다 — "다시 발견"이 걸리지 않게. 다만 한 글자씩 띄어 쓴 것은 붙여서 본다 ("시 발" → "시발").
    /// - 예외 낱말(<see cref="Allow"/>) 안에 든 것은 넘어간다 ("시발점").
    /// 낱말만으로는 뜻(위협·괴롭힘)을 가리지 못한다 — 그쪽은 신고가 맡는다.
    /// </summary>
    public sealed class BbsFilter
    {
        public const char DefaultMask = '○';

        public static readonly BbsFilter Empty = new BbsFilter(null);

        private readonly List<string> _words = new List<string>();
        private readonly List<string> _allow = new List<string>();
        private readonly List<string> _invalid = new List<string>();
        private readonly char _mask;

        public BbsFilter(IEnumerable<string> words, IEnumerable<string> allow = null, char mask = DefaultMask)
        {
            _mask = mask;
            Collect(words, _words, _invalid);
            Collect(allow, _allow, _invalid);
        }

        /// <summary>막는 낱말·예외 낱말 (비교하는 꼴로 바꾼 것).</summary>
        public IReadOnlyList<string> Words => _words;

        public IReadOnlyList<string> Allow => _allow;

        /// <summary>쓸 수 없어 버린 항목 (비었거나 숫자·기호가 든 낱말 — 비교할 때 숫자·기호를 빼므로 맞출 수 없다).</summary>
        public IReadOnlyList<string> Invalid => _invalid;

        private static void Collect(IEnumerable<string> source, List<string> into, List<string> invalid)
        {
            if (source == null)
            {
                return;
            }

            foreach (var raw in source)
            {
                if (string.IsNullOrWhiteSpace(raw))
                {
                    continue;
                }

                var word = Normalize(raw);
                if (word.Length == 0 || !OnlyLetters(raw))
                {
                    invalid.Add(raw);
                }
                else if (!into.Contains(word))
                {
                    into.Add(word);
                }
            }
        }

        private static bool OnlyLetters(string raw)
        {
            foreach (var c in raw)
            {
                if (!char.IsWhiteSpace(c) && !char.IsLetter(c))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 목록을 고친 새 필터: 낱말을 더하고(add), 빼고(remove), 예외를 더한다(allow). 서버의 목록을 기본 목록에 얹을 때 쓴다.
        /// </summary>
        public BbsFilter With(IEnumerable<string> add, IEnumerable<string> allow, IEnumerable<string> remove)
        {
            var removed = new BbsFilter(remove);
            var words = new List<string>();
            foreach (var word in _words)
            {
                if (!removed._words.Contains(word))
                {
                    words.Add(word);
                }
            }

            if (add != null)
            {
                words.AddRange(add);
            }

            var allowed = new List<string>(_allow);
            if (allow != null)
            {
                allowed.AddRange(allow);
            }

            return new BbsFilter(words, allowed, _mask);
        }

        /// <summary>쉼표·줄바꿈으로 나눈 목록 한 줄을 낱말들로 (콘솔에서 글 한 줄로 고칠 수 있게).</summary>
        public static List<string> SplitList(string text)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(text))
            {
                return result;
            }

            foreach (var part in text.Split(',', '\n', '\r', ';', '，'))
            {
                var word = part.Trim();
                if (word.Length > 0)
                {
                    result.Add(word);
                }
            }

            return result;
        }

        /// <summary>
        /// 비교하는 꼴로 바꾼다. map을 주면 바꾼 글의 글자마다 원래 글의 자리를 적는다 (가릴 때 쓴다).
        /// 낱말 사이는 빈칸 하나, 한 글자짜리 낱말이 이어지면 붙인다.
        /// </summary>
        public static string Normalize(string text, List<int> map = null)
        {
            map?.Clear();
            if (string.IsNullOrEmpty(text))
            {
                return "";
            }

            var builder = new StringBuilder(text.Length);
            var token = new List<(char Letter, int At)>();
            var previousSingle = false;
            var index = 0;
            while (index <= text.Length)
            {
                var end = index == text.Length;
                if (!end && !char.IsWhiteSpace(text[index]))
                {
                    var letter = Letter(text[index]);
                    if (letter != '\0')
                    {
                        token.Add((letter, index));
                    }

                    index++;
                    continue;
                }

                // 낱말 하나가 끝났다 (기호뿐이던 낱말은 없던 것으로 친다)
                if (token.Count > 0)
                {
                    var single = token.Count == 1;
                    if (builder.Length > 0 && !(single && previousSingle))
                    {
                        builder.Append(' ');
                        map?.Add(token[0].At);
                    }

                    foreach (var (letter, at) in token)
                    {
                        builder.Append(letter);
                        map?.Add(at);
                    }

                    previousSingle = single;
                    token.Clear();
                }

                index++;
            }

            return builder.ToString();
        }

        /// <summary>비교에 쓰는 글자 (글자가 아니면 '\0'): 소문자, 전각 영문 → 반각.</summary>
        private static char Letter(char c)
        {
            if (c >= 'Ａ' && c <= 'Ｚ')
            {
                return (char)(c - 'Ａ' + 'a');
            }

            if (c >= 'ａ' && c <= 'ｚ')
            {
                return (char)(c - 'ａ' + 'a');
            }

            return char.IsLetter(c) ? char.ToLowerInvariant(c) : '\0';
        }

        /// <summary>막는 낱말이 들어 있는가.</summary>
        public bool Hits(string text)
        {
            return _words.Count > 0 && Spans(Normalize(text)).Count > 0;
        }

        /// <summary>막는 낱말을 가린 글. 길이와 띄어쓰기는 그대로다 (낱말 사이에 끼운 기호도 함께 가린다).</summary>
        public string Mask(string text)
        {
            if (string.IsNullOrEmpty(text) || _words.Count == 0)
            {
                return text ?? "";
            }

            var map = new List<int>();
            var spans = Spans(Normalize(text, map));
            if (spans.Count == 0)
            {
                return text;
            }

            var chars = text.ToCharArray();
            foreach (var (start, end) in spans)
            {
                for (var i = map[start]; i <= map[end - 1]; i++)
                {
                    if (!char.IsWhiteSpace(chars[i]))
                    {
                        chars[i] = _mask;
                    }
                }
            }

            return new string(chars);
        }

        /// <summary>비교하는 꼴의 글에서 막는 낱말이 놓인 자리들 [시작, 끝). 예외 낱말 안에 든 것은 뺀다.</summary>
        private List<(int Start, int End)> Spans(string normal)
        {
            var result = new List<(int, int)>();
            if (normal.Length == 0)
            {
                return result;
            }

            var allowed = new List<(int Start, int End)>();
            foreach (var word in _allow)
            {
                for (var at = normal.IndexOf(word, StringComparison.Ordinal); at >= 0; at = normal.IndexOf(word, at + 1, StringComparison.Ordinal))
                {
                    allowed.Add((at, at + word.Length));
                }
            }

            foreach (var word in _words)
            {
                for (var at = normal.IndexOf(word, StringComparison.Ordinal); at >= 0; at = normal.IndexOf(word, at + 1, StringComparison.Ordinal))
                {
                    var end = at + word.Length;
                    var excused = false;
                    foreach (var (start, stop) in allowed)
                    {
                        if (start <= at && end <= stop)
                        {
                            excused = true;
                            break;
                        }
                    }

                    if (!excused)
                    {
                        result.Add((at, end));
                    }
                }
            }

            return result;
        }
    }
}
