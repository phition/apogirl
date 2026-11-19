using System;

namespace ApoGirl.Core.Text
{
    /// <summary>
    /// 한국어 조사 자동 선택 (D21·D23: 이름 변경 시 조사 자동 처리).
    /// 쌍은 "받침 있을 때/없을 때" 순서: "이/가", "은/는", "을/를", "과/와", "아/야", "이에요/예요", "으로/로".
    /// 슬래시 없이 "이가"처럼 붙여 써도 된다 (두 글자일 때). "으로/로"는 ㄹ 받침도 "로".
    /// </summary>
    public static class KoreanJosa
    {
        private const int HangulFirst = 0xAC00;
        private const int HangulLast = 0xD7A3;
        private const int RieulJong = 8;

        /// <summary>단어 + 알맞은 조사.</summary>
        public static string Attach(string word, string pair)
        {
            return (word ?? "") + Particle(word, pair);
        }

        /// <summary>알맞은 조사만.</summary>
        public static string Particle(string word, string pair)
        {
            Split(pair, out var withBatchim, out var withoutBatchim);
            var jong = FinalConsonant(word);
            var isRo = withoutBatchim == "로";
            var useBatchimForm = isRo ? jong > 0 && jong != RieulJong : jong > 0;
            return useBatchimForm ? withBatchim : withoutBatchim;
        }

        /// <summary>마지막 글자의 종성 인덱스 (0 = 받침 없음). 한글이 아니면 숫자·영문을 읽는 소리로 추정.</summary>
        public static int FinalConsonant(string word)
        {
            if (string.IsNullOrEmpty(word))
            {
                return 0;
            }

            var i = word.Length - 1;
            while (i > 0 && (char.IsWhiteSpace(word[i]) || char.IsPunctuation(word[i]) || word[i] == ')'))
            {
                i--;
            }

            var c = word[i];
            if (c >= HangulFirst && c <= HangulLast)
            {
                return (c - HangulFirst) % 28;
            }

            if (c >= '0' && c <= '9')
            {
                // 영 일 이 삼 사 오 육 칠 팔 구
                switch (c)
                {
                    case '0': return 21; // ㅇ
                    case '1': return RieulJong;
                    case '3': return 16; // ㅁ
                    case '6': return 1; // ㄱ
                    case '7': return RieulJong;
                    case '8': return RieulJong;
                    default: return 0;
                }
            }

            switch (char.ToLowerInvariant(c))
            {
                case 'l': return RieulJong;
                case 'm': return 16;
                case 'n': return 4;
                default: return 0;
            }
        }

        private static void Split(string pair, out string withBatchim, out string withoutBatchim)
        {
            pair ??= "";
            var slash = pair.IndexOf('/');
            if (slash >= 0)
            {
                withBatchim = pair.Substring(0, slash);
                withoutBatchim = pair.Substring(slash + 1);
                return;
            }

            if (pair == "으로")
            {
                withBatchim = "으로";
                withoutBatchim = "로";
                return;
            }

            if (pair.Length == 2)
            {
                withBatchim = pair.Substring(0, 1);
                withoutBatchim = pair.Substring(1, 1);
                return;
            }

            throw new ArgumentException($"조사 쌍 형식 오류: '{pair}' (예: \"이/가\")");
        }
    }
}
