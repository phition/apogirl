using System.Collections.Generic;
using ApoGirl.Core.Rules;
using NUnit.Framework;

namespace ApoGirl.Tests.EditMode
{
    /// <summary>PC통신 열린 게시판의 금칙어 필터: 비교 규칙(기호·띄어쓰기·예외)과 가리기.</summary>
    public sealed class BbsFilterTests
    {
        private static BbsFilter Filter() => new BbsFilter(new[] { "나쁜말", "Bad", "ㄴㅃ", "나가 버려" }, new[] { "나쁜말투" });

        [Test]
        public void Normalize_KeepsLettersOnly_JoinsSpelledOutLetters_KeepsWordGaps()
        {
            Assert.AreEqual("나쁜말", BbsFilter.Normalize("나@쁜1말!!"), "기호·숫자는 뺀다");
            Assert.AreEqual("bad word", BbsFilter.Normalize("  BAD   Word "), "소문자로, 낱말 사이는 빈칸 하나");
            Assert.AreEqual("bad", BbsFilter.Normalize("ＢＡｄ"), "전각 영문은 반각으로");
            Assert.AreEqual("나쁜말", BbsFilter.Normalize("나 쁜 말"), "한 글자씩 띄어 쓴 것은 붙인다");
            Assert.AreEqual("나쁜말", BbsFilter.Normalize("나 . 쁜 ~ 말"), "사이에 기호만 있는 낱말은 없던 것으로 친다");
            Assert.AreEqual("다시 발견", BbsFilter.Normalize("다시 발견"), "두 글자 이상인 낱말은 붙이지 않는다");
            Assert.AreEqual("김씨 발자국", BbsFilter.Normalize("김 씨 발자국"), "한 글자 낱말은 한 글자 낱말끼리만 붙는다");
            Assert.AreEqual("나쁜말", BbsFilter.Normalize("나​쁜​말"), "보이지 않는 글자를 끼워도 같다");
            Assert.AreEqual("", BbsFilter.Normalize("123 !!! "));
            Assert.AreEqual("", BbsFilter.Normalize(null));

            var map = new List<int>();
            Assert.AreEqual("가나 다", BbsFilter.Normalize("가-나  다", map));
            CollectionAssert.AreEqual(new[] { 0, 2, 5, 5 }, map, "바꾼 글의 글자마다 원래 자리 (빈칸은 다음 낱말의 첫 자리)");
        }

        [Test]
        public void Hits_FindsWordsThroughSymbolsAndSpacing_ButNotAcrossWords()
        {
            var filter = Filter();
            Assert.IsTrue(filter.Hits("그건 나쁜말이야"));
            Assert.IsTrue(filter.Hits("this is BAD"));
            Assert.IsTrue(filter.Hits("나.쁜.말"));
            Assert.IsTrue(filter.Hits("나 쁜 말 하지 마"));
            Assert.IsTrue(filter.Hits("ㄴ ㅃ"));
            Assert.IsTrue(filter.Hits("그냥 나가 버려"), "두 낱말짜리 말은 띄어 쓴 그대로 맞춘다");
            Assert.IsFalse(filter.Hits("나가버려"), "붙여 쓴 꼴은 따로 넣어야 한다");

            Assert.IsFalse(filter.Hits("잘 지내?"));
            Assert.IsFalse(filter.Hits("하나 쁜말"), "낱말 사이를 넘어서는 맞추지 않는다 (나 + 쁜말)");
            Assert.IsFalse(filter.Hits("그 나쁜말투 좀"), "예외 낱말 안에 든 것은 넘어간다");
            Assert.IsTrue(filter.Hits("나쁜말투 말고 나쁜말"), "예외 밖에 또 있으면 걸린다");
            Assert.IsFalse(filter.Hits(""));
            Assert.IsFalse(filter.Hits(null));
            Assert.IsFalse(BbsFilter.Empty.Hits("나쁜말"));
        }

        [Test]
        public void Mask_CoversOnlyTheWord_KeepingLengthAndSpaces()
        {
            var filter = Filter();
            Assert.AreEqual("그건 ○○○이야", filter.Mask("그건 나쁜말이야"));
            Assert.AreEqual("○○○○○ 하지 마", filter.Mask("나.쁜.말 하지 마"), "끼운 기호도 함께 가린다");
            Assert.AreEqual("○ ○ ○ 하지 마", filter.Mask("나 쁜 말 하지 마"), "띄어쓰기는 그대로");
            Assert.AreEqual("this is ○○○!", filter.Mask("this is BAD!"));
            Assert.AreEqual("그 나쁜말투 좀", filter.Mask("그 나쁜말투 좀"));
            Assert.AreEqual("○○○ 그리고 ○○○", filter.Mask("나쁜말 그리고 나쁜말"), "여러 번 나와도 모두");
            Assert.AreEqual("잘 지내?", filter.Mask("잘 지내?"));
            Assert.AreEqual("", filter.Mask(null));
            Assert.AreEqual("그건 ※※※", new BbsFilter(new[] { "나쁜말" }, null, '※').Mask("그건 나쁜말"), "가리는 글자는 바꿀 수 있다");
        }

        [Test]
        public void Entries_WithDigitsOrSymbolsAreRejected_DuplicatesCollapse()
        {
            var filter = new BbsFilter(new[] { "나쁜말", "나쁜말", " 나쁜말 ", "18놈", "a.b", "", null, "!!!" });
            CollectionAssert.AreEqual(new[] { "나쁜말" }, filter.Words);
            CollectionAssert.AreEquivalent(new[] { "18놈", "a.b", "!!!" }, filter.Invalid, "숫자·기호가 든 낱말은 비교할 때 숫자·기호를 빼므로 맞출 수 없다");
            Assert.IsFalse(filter.Hits("이 놈"), "버린 항목이 엉뚱한 글을 막지 않는다");
        }

        [Test]
        public void With_AddsRemovesAndAllows()
        {
            var filter = Filter().With(new[] { "새말" }, new[] { "새말씀" }, new[] { "BAD" });
            Assert.IsTrue(filter.Hits("새말"));
            Assert.IsFalse(filter.Hits("새말씀"));
            Assert.IsFalse(filter.Hits("bad"), "뺀 낱말 (대소문자는 가리지 않는다)");
            Assert.IsTrue(filter.Hits("나쁜말"));
            Assert.IsFalse(filter.Hits("나쁜말투"), "원래 예외는 남는다");
            Assert.IsTrue(Filter().With(null, null, null).Hits("bad"));
        }

        [Test]
        public void SplitList_ReadsCommaOrLineSeparatedWords()
        {
            CollectionAssert.AreEqual(new[] { "하나", "둘 셋", "넷" }, BbsFilter.SplitList(" 하나, 둘 셋 ,\n넷,, "));
            Assert.AreEqual(0, BbsFilter.SplitList(null).Count);
            Assert.AreEqual(0, BbsFilter.SplitList("  ").Count);
        }
    }
}
