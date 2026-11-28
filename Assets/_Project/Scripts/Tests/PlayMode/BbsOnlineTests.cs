using ApoGirl.Presentation.Screens;
using NUnit.Framework;

namespace ApoGirl.Tests.PlayMode
{
    /// <summary>PC통신 온라인 게시판의 글 다루기 (통신 없이 확인할 수 있는 부분): 받은 글 읽기·올릴 글 만들기·낱말 거르기.</summary>
    public sealed class BbsOnlineTests
    {
        private static BbsOnlineConfig Config() => new BbsOnlineConfig { MaxTitle = 10, MaxBody = 20, MaxAuthor = 6, BlockedWords = new[] { "나쁜말" } };

        [Test]
        public void ParsePosts_ReadsInPushOrder_SkipsBrokenAndCleans()
        {
            var json = "{"
                       + "\"pA\":{\"title\":\"둘째 글\",\"body\":\"잘 지내?\",\"author\":\"도현\",\"date\":\"03.17\",\"uid\":\"u2\",\"ts\":2},"
                       + "\"pZ\":{\"title\":\"첫 글입니다 아주 긴 제목\",\"body\":\"여기 <b>나쁜말</b> 있음\\n다음 줄\",\"author\":\"서진\",\"date\":\"03.16\",\"uid\":\"u1\",\"ts\":1},"
                       + "\"-Nc\":{\"title\":\"\",\"body\":\"제목 없음\"},"
                       + "\"-Nd\":\"글이 아님\""
                       + "}";
            var posts = BbsOnline.ParsePosts(json, Config());
            Assert.AreEqual(2, posts.Count, "제목·내용이 빈 글과 글이 아닌 값은 건너뛴다");
            Assert.AreEqual("첫 글입니다 아주", posts[0].Title, "서버 시각 차례대로(글 ID 차례가 아니다), 제목은 한도에서 자른다");
            Assert.AreEqual("서진(" + BbsOnline.Tag("u1") + ")", posts[0].Author, "올린이에는 계정 꼬리표가 붙는다");
            Assert.AreEqual("03.16", posts[0].Date);
            StringAssert.DoesNotContain("나쁜말", posts[0].Body[0]);
            StringAssert.DoesNotContain("<", posts[0].Body[0], "글꼴 태그를 넣지 못한다");
            StringAssert.DoesNotContain("\n", posts[0].Body[0]);
            Assert.AreEqual("둘째 글", posts[1].Title);

            Assert.AreEqual(0, BbsOnline.ParsePosts("null", Config()).Count, "글이 하나도 없으면 서버는 null을 준다");
            Assert.AreEqual(0, BbsOnline.ParsePosts("깨진 응답", Config()).Count);
        }

        [Test]
        public void BuildPost_TrimsAndStampsServerTime()
        {
            var post = new BbsConfig.Post { Title = "  제목이 한도보다 훨씬 길다  ", Body = new[] { "내용" }, Author = "서진", Date = "03.16" };
            var json = BbsOnline.BuildPost(post, "uid-1", Config());
            StringAssert.Contains("\"title\":\"제목이 한도보다 훨\"", json);
            StringAssert.Contains("\"uid\":\"uid-1\"", json);
            StringAssert.Contains("\"ts\":{\".sv\":\"timestamp\"}", json, "시각은 서버가 채운다");

            // 올리기 요청: 글과 마지막 글 시각을 한 번에
            var send = BbsOnline.BuildSend(post, "uid-1", "pABC", Config());
            StringAssert.Contains("\"board/pABC\":{", send);
            StringAssert.Contains("\"users/uid-1/lastPost\":{\".sv\":\"timestamp\"}", send);
            Assert.AreNotEqual(BbsOnline.NewPostId(), BbsOnline.NewPostId());
        }

        [Test]
        public void Tag_IsStablePerAccount_FourReadableLetters()
        {
            var a = BbsOnline.Tag("abcDEF123uidOne");
            Assert.AreEqual(4, a.Length);
            Assert.AreEqual(a, BbsOnline.Tag("abcDEF123uidOne"), "같은 계정은 늘 같은 꼬리표");
            Assert.AreNotEqual(a, BbsOnline.Tag("abcDEF123uidTwo"));
            StringAssert.IsMatch("^[2-9A-HJ-NP-Z]{4}$", a, "헷갈리는 글자(0·O·1·I)는 쓰지 않는다");
            Assert.AreEqual("", BbsOnline.Tag(""));
            Assert.AreEqual("서진", BbsOnline.AuthorWithTag("서진", null));
        }

        [Test]
        public void IsBlocked_FindsWordsIgnoringCase()
        {
            var config = new BbsOnlineConfig { BlockedWords = new[] { "Bad", "나쁜말" } };
            Assert.IsTrue(BbsOnline.IsBlocked("this is BAD", config));
            Assert.IsTrue(BbsOnline.IsBlocked("그건 나쁜말이야", config));
            Assert.IsFalse(BbsOnline.IsBlocked("잘 지내?", config));
            Assert.IsFalse(BbsOnline.IsBlocked("", config));
        }

        // ── 글 사이 간격·새 계정 대기 (2026-11-28 다시 맞춤): 보내기 전에 "n초 뒤에 다시"를 알려 준다. 통신은 하지 않는다 ──

        [Test]
        public void WaitSeconds_CountsDownWithMargin_AndForgivesAClockThatWentBack()
        {
            const long now = 1_000_000_000L;
            Assert.AreEqual(0, BbsOnline.WaitSeconds(now, 0, 60), "기록이 없으면 기다리지 않는다");
            Assert.AreEqual(60 + BbsOnline.ClockMarginSeconds, BbsOnline.WaitSeconds(now, now, 60), "방금이면 간격 + 여유");
            Assert.AreEqual(BbsOnline.ClockMarginSeconds + 1, BbsOnline.WaitSeconds(now, now - 59_000, 60));
            Assert.AreEqual(0, BbsOnline.WaitSeconds(now, now - (60 + BbsOnline.ClockMarginSeconds) * 1000L, 60), "다 지났다");
            Assert.AreEqual(60 + BbsOnline.ClockMarginSeconds, BbsOnline.WaitSeconds(now, now + 3_600_000, 60), "시계가 거꾸로 갔어도 가장 길어야 간격 + 여유");
            Assert.AreEqual(0, BbsOnline.WaitSeconds(now, now, 0), "간격이 0이면 기다리지 않는다");
        }

        private static string IdentityFile(long firstSeenMs, long lastPostMs)
        {
            // 테스트는 사용자의 실제 계정 파일(persistentDataPath/BbsOnline.json)을 건드리지 않는다
            var path = System.IO.Path.Combine(UnityEngine.Application.temporaryCachePath, "BbsOnlineTest_" + System.Guid.NewGuid().ToString("N") + ".json");
            System.IO.File.WriteAllText(path, "{\"Uid\":\"uid-test\",\"RefreshToken\":\"r\",\"FirstSeenMs\":" + firstSeenMs + ",\"LastPostMs\":" + lastPostMs + "}");
            return path;
        }

        private static BbsOnlineConfig Online() =>
            new BbsOnlineConfig { Enabled = true, DatabaseUrl = "https://example.invalid", ApiKey = "k", PostIntervalSeconds = 60, NewAccountWaitSeconds = 120 };

        [Test]
        public void PostWait_NewAccountFirst_ThenTheGapBetweenPosts_TestPathSkipsTheNewAccountWait()
        {
            var now = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var fresh = IdentityFile(now, now);
            try
            {
                var live = new BbsOnline(Online(), fresh, false);
                Assert.AreEqual("uid-test", live.Uid);
                Assert.AreEqual(BbsSendResult.NewAccount, live.PostWait().Result, "정식 경로: 새 계정 대기가 먼저");
                Assert.Greater(live.PostWait().Seconds, 100);

                var test = new BbsOnline(Online(), fresh, true);
                Assert.IsTrue(test.UsesTestPath);
                Assert.AreEqual(BbsSendResult.TooSoon, test.PostWait().Result, "시험 경로에는 새 계정 대기가 없다 — 글 사이 간격만");
            }
            finally
            {
                System.IO.File.Delete(fresh);
            }

            var old = IdentityFile(now - 3_600_000, now - 3_600_000);
            try
            {
                Assert.AreEqual(BbsSendResult.Ok, new BbsOnline(Online(), old, false).PostWait().Result, "오래된 계정은 바로 올릴 수 있다");
            }
            finally
            {
                System.IO.File.Delete(old);
            }

            var legacy = IdentityFile(0, 0);
            try
            {
                Assert.AreEqual(BbsSendResult.Ok, new BbsOnline(Online(), legacy, false).PostWait().Result, "시각 기록이 없는 옛 계정 파일도 올릴 수 있다");
            }
            finally
            {
                System.IO.File.Delete(legacy);
            }
        }

        [Test]
        public void Send_RightAfterAPost_AnswersTooSoonWithoutCallingTheServer()
        {
            var now = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var path = IdentityFile(now - 3_600_000, now - 5_000);
            try
            {
                var online = new BbsOnline(Online(), path, false);
                BbsSendResult? result = null;
                var seconds = 0;
                online.Send(new BbsConfig.Post { Title = "제목", Body = new[] { "내용" }, Author = "서진", Date = "03.16" }, (r, s) =>
                {
                    result = r;
                    seconds = s;
                });
                Assert.AreEqual(BbsSendResult.TooSoon, result, "바로 답한다 (서버에 보내지 않는다)");
                Assert.That(seconds, Is.InRange(50, 60));

                var off = new BbsOnline(new BbsOnlineConfig { Enabled = false }, path, false);
                BbsSendResult? offline = null;
                off.Send(new BbsConfig.Post { Title = "제목", Body = new[] { "내용" } }, (r, _) => offline = r);
                Assert.AreEqual(BbsSendResult.Offline, offline, "온라인을 꺼 두었으면 보내지 않는다");
            }
            finally
            {
                System.IO.File.Delete(path);
            }
        }
    }
}
