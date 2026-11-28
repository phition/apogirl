using System.Collections.Generic;
using ApoGirl.Core.Rules;
using ApoGirl.Presentation.Screens;
using NUnit.Framework;

namespace ApoGirl.Tests.PlayMode
{
    /// <summary>PC통신 온라인 게시판의 글 다루기 (통신 없이 확인할 수 있는 부분): 받은 글 읽기·올릴 글 만들기·낱말 거르기.</summary>
    public sealed class BbsOnlineTests
    {
        private static BbsOnlineConfig Config() => new BbsOnlineConfig { MaxTitle = 10, MaxBody = 20, MaxAuthor = 6 };

        private static BbsFilter Filter() => new BbsFilter(new[] { "나쁜말" });

        [Test]
        public void ParsePosts_ReadsInPushOrder_SkipsBrokenAndCleans()
        {
            var json = "{"
                       + "\"pA\":{\"title\":\"둘째 글\",\"body\":\"잘 지내?\",\"author\":\"도현\",\"date\":\"03.17\",\"uid\":\"u2\",\"ts\":2},"
                       + "\"pZ\":{\"title\":\"첫 글입니다 아주 긴 제목\",\"body\":\"여기 <b>나쁜말</b> 있음\\n다음 줄\",\"author\":\"서진\",\"date\":\"03.16\",\"uid\":\"u1\",\"ts\":1},"
                       + "\"-Nc\":{\"title\":\"\",\"body\":\"제목 없음\"},"
                       + "\"-Nd\":\"글이 아님\""
                       + "}";
            var posts = BbsOnline.ParsePosts(json, Config(), Filter());
            Assert.AreEqual(2, posts.Count, "제목·내용이 빈 글과 글이 아닌 값은 건너뛴다");
            Assert.AreEqual("pZ", posts[0].Id, "글 ID와 올린 계정을 기억한다 (신고·가리기에 쓴다)");
            Assert.AreEqual("u1", posts[0].Uid);
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

        // ── 금칙어·신고 (2026-11-28): 필터의 비교 규칙은 EditMode BbsFilterTests, 여기서는 게임에 든 목록과 서버 자료 읽기 ──

        [Test]
        public void BuiltInFilter_BlocksSwearing_ButNotOrdinaryGameTalk()
        {
            var filter = BbsFilterData.Load();
            Assert.Greater(filter.Words.Count, 30, "게임에 든 금칙어 목록(Resources/Bbs/bbs_filter.json)을 읽는다");
            Assert.AreEqual(0, filter.Invalid.Count, "쓸 수 없는 항목(숫자·기호가 든 낱말): " + string.Join(", ", filter.Invalid));

            foreach (var bad in new[] { "야 이 병신아", "시@발 진짜", "ㅅ ㅂ 뭐야", "FUCK you", "카톡 아이디 알려 줘", "그냥 나가 죽어" })
            {
                Assert.IsTrue(filter.Hits(bad), "걸려야 한다: " + bad);
            }

            // 이 게임에서 평범하게 오가는 말은 걸리지 않아야 한다
            foreach (var fine in new[]
                     {
                         "다시 발견했어요", "김 씨 발자국이 있었다", "가방을 뒤져라", "통조림을 씹어 먹었다", "새끼 고양이를 봤어요", "문을 열어 보지 못했다",
                         "감염된 사람이 죽었다", "시체가 복도에 있다", "여기가 시발점이다", "잠을 자지 못했다", "이 병 신기하네", "3시 발표를 들었다",
                         "정수장 물은 끓여 드세요", "혹시 바쁘세요", "급식이는 잘 지내요",
                     })
            {
                Assert.IsFalse(filter.Hits(fine), "걸리면 안 된다: " + fine + " → " + filter.Mask(fine));
            }
        }

        [Test]
        public void MergeFilter_AddsRemovesAndAllowsFromTheServerList()
        {
            var merged = BbsOnline.MergeFilter(new BbsFilter(new[] { "나쁜말", "애매한말" }), "{\"words\":\"새말, 또 새말\",\"remove\":\"애매한말\",\"allow\":\"나쁜말투\"}");
            Assert.IsTrue(merged.Hits("이건 새말이야"));
            Assert.IsTrue(merged.Hits("그건 또 새말"), "두 낱말짜리 말도 쉼표로 나눠 넣는다");
            Assert.IsFalse(merged.Hits("애매한말"), "서버에서 뺀 낱말");
            Assert.IsTrue(merged.Hits("나쁜말 하지 마"));
            Assert.IsFalse(merged.Hits("나쁜말투"), "서버에서 더한 예외");

            var same = new BbsFilter(new[] { "나쁜말" });
            Assert.AreSame(same, BbsOnline.MergeFilter(same, "null"), "서버에 목록이 없으면 그대로");
            Assert.AreSame(same, BbsOnline.MergeFilter(same, "깨진 응답"));
            Assert.AreSame(same, BbsOnline.MergeFilter(same, "[1,2]"));
        }

        [Test]
        public void Visible_HidesBannedAuthors_ReportedPosts_AndWhatIReported()
        {
            var posts = new List<BbsConfig.Post>
            {
                new BbsConfig.Post { Title = "평범", Id = "p1", Uid = "u1" },
                new BbsConfig.Post { Title = "차단된 사람", Id = "p2", Uid = "bad" },
                new BbsConfig.Post { Title = "신고 셋", Id = "p3", Uid = "u3" },
                new BbsConfig.Post { Title = "신고 둘", Id = "p4", Uid = "u4" },
                new BbsConfig.Post { Title = "내가 신고", Id = "p5", Uid = "u5" },
                new BbsConfig.Post { Title = "방금 신고", Id = "p6", Uid = "u6" },
            };
            var reports = BbsOnline.ParseReports("{\"p3\":{\"a\":1,\"b\":2,\"c\":3},\"p4\":{\"a\":1,\"b\":2},\"p5\":{\"me\":9},\"p9\":\"깨짐\"}");
            Assert.AreEqual(3, reports["p3"].Count);
            Assert.IsFalse(reports.ContainsKey("p9"));
            var banned = BbsOnline.ParseKeys("{\"bad\":true,\"stillBad\":false}");
            Assert.IsTrue(banned.Contains("bad"));
            Assert.IsTrue(banned.Contains("stillBad"), "값은 보지 않는다 — 규칙이 항목이 있는지만 보므로 false로 두어도 차단이다");

            var visible = BbsOnline.Visible(posts, reports, banned, "me", 3, new HashSet<string> { "p6" });
            CollectionAssert.AreEqual(new[] { "p1", "p4" }, visible.ConvertAll(p => p.Id));

            // 신고 기록·차단 목록을 못 받았으면(규칙 게시 전·회선 문제) 그만큼 덜 가려질 뿐이다
            Assert.AreEqual(6, BbsOnline.Visible(posts, BbsOnline.ParseReports(null), BbsOnline.ParseKeys(null), "me", 3, null).Count);
            Assert.AreEqual(6, BbsOnline.Visible(posts, reports, null, "", 0, null).Count, "문턱이 0이면 신고가 쌓여도 가리지 않는다");
        }

        [Test]
        public void BuildReport_WritesTheReportAndItsTimeTogether()
        {
            var json = BbsOnline.BuildReport("pABC", "uid-1");
            StringAssert.Contains("\"reports/pABC/uid-1\":{\".sv\":\"timestamp\"}", json);
            StringAssert.Contains("\"users/uid-1/lastReport\":{\".sv\":\"timestamp\"}", json);
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

        private static string IdentityFile(long firstSeenMs, long lastPostMs, long lastReportMs = 0)
        {
            // 테스트는 사용자의 실제 계정 파일(persistentDataPath/BbsOnline.json)을 건드리지 않는다
            var path = System.IO.Path.Combine(UnityEngine.Application.temporaryCachePath, "BbsOnlineTest_" + System.Guid.NewGuid().ToString("N") + ".json");
            System.IO.File.WriteAllText(path, "{\"Uid\":\"uid-test\",\"RefreshToken\":\"r\",\"FirstSeenMs\":" + firstSeenMs + ",\"LastPostMs\":" + lastPostMs + ",\"LastReportMs\":" + lastReportMs + "}");
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

        [Test]
        public void Report_RightAfterAReport_AnswersTooSoonWithoutCallingTheServer()
        {
            var now = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var path = IdentityFile(now - 3_600_000, 0, now - 5_000);
            var fresh = IdentityFile(now, 0);
            try
            {
                var online = new BbsOnline(Online(), path, false, BbsFilter.Empty);
                Assert.AreEqual(BbsSendResult.Ok, online.PostWait().Result, "신고 간격은 글 올리기와 따로 센다");
                BbsSendResult? result = null;
                var seconds = 0;
                online.Report(new BbsConfig.Post { Title = "제목", Id = "p1", Uid = "u1" }, "서진", (r, s) =>
                {
                    result = r;
                    seconds = s;
                });
                Assert.AreEqual(BbsSendResult.TooSoon, result, "바로 답한다 (서버에 보내지 않는다)");
                Assert.That(seconds, Is.InRange(20, 30));

                BbsSendResult? local = null;
                online.Report(new BbsConfig.Post { Title = "내 컴퓨터의 글" }, "서진", (r, _) => local = r);
                Assert.AreEqual(BbsSendResult.Offline, local, "글 ID가 없는 글(내 컴퓨터의 글·처음부터 있던 글)은 신고할 수 없다");

                Assert.AreEqual(BbsSendResult.NewAccount, new BbsOnline(Online(), fresh, false, BbsFilter.Empty).ReportWait().Result, "새 계정은 신고도 기다린다");
                Assert.AreEqual(BbsSendResult.Ok, new BbsOnline(Online(), fresh, true, BbsFilter.Empty).ReportWait().Result, "시험 경로에는 새 계정 대기가 없다");
            }
            finally
            {
                System.IO.File.Delete(path);
                System.IO.File.Delete(fresh);
            }
        }
    }
}
