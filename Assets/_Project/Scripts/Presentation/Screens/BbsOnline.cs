using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ApoGirl.Presentation.App;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace ApoGirl.Presentation.Screens
{
    /// <summary>
    /// PC통신 '열린 게시판'의 온라인 저장소 (사용자 요청 2026-11-26): Firebase 실시간 데이터베이스를 REST로 읽고 쓴다 (SDK 없음, PC 전용).
    /// 로그인은 익명 로그인 한 번 — 받은 갱신 토큰을 persistentDataPath/BbsOnline.json에 두고 다음부터 그것으로 새 토큰을 받는다.
    /// 설정은 Resources/Bbs/bbs_online.json. 연결이 안 되면 조용히 실패를 알리고, 화면은 내 컴퓨터의 글만 보여 준다.
    /// 서버 쪽 보호는 보안 규칙이 맡는다 (Docs/BBS_ONLINE.md): 로그인한 사람만, 새 글만, 형식이 맞는 글만.
    /// </summary>
    public sealed class BbsOnlineConfig
    {
        public bool Enabled;
        public string DatabaseUrl = "";
        public string ApiKey = "";

        /// <summary>
        /// 자료가 놓이는 뿌리 경로: 정식 빌드 / 에디터·개발 빌드 (시험 글이 정식 게시판에 섞이지 않게).
        /// 그 아래 board(글)·users(이용자 기록: 이름·첫 접속·마지막 글 시각)·banned(차단, 콘솔에서만)가 놓인다.
        /// </summary>
        public string RootPath = "bbs/v1";
        public string TestRootPath = "bbs/test";

        /// <summary>한 번에 받아 오는 최근 글 수, 요청 제한 시간(초).</summary>
        public int ReadLimit = 40;
        public int TimeoutSeconds = 8;

        /// <summary>글 길이 한도 (보안 규칙의 한도와 같아야 한다).</summary>
        public int MaxTitle = 30;
        public int MaxBody = 120;
        public int MaxAuthor = 12;

        /// <summary>
        /// 글 사이 간격과 새 계정이 글을 쓰기까지 기다리는 시간(초). **보안 규칙의 60000·120000과 같아야 한다** —
        /// 막는 것은 규칙이고, 이 값은 "n초 뒤에 다시"를 미리 알려 주는 데만 쓴다. 시험 경로는 새 계정 대기가 없다.
        /// </summary>
        public int PostIntervalSeconds = 60;
        public int NewAccountWaitSeconds = 120;

        /// <summary>이 낱말이 들어간 글은 올리지 않고, 받은 글에서는 ○로 가린다 (소문자로 비교).</summary>
        public string[] BlockedWords = Array.Empty<string>();

        public static BbsOnlineConfig Load()
        {
            var asset = Resources.Load<TextAsset>("Bbs/bbs_online");
            if (asset == null)
            {
                return new BbsOnlineConfig();
            }

            try
            {
                return JsonConvert.DeserializeObject<BbsOnlineConfig>(asset.text) ?? new BbsOnlineConfig();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("PC통신 온라인 설정 읽기 실패: " + exception.Message);
                return new BbsOnlineConfig();
            }
        }
    }

    /// <summary>글 올리기 결과.</summary>
    public enum BbsSendResult
    {
        Ok,

        /// <summary>방금 글을 올렸다 (글 사이 간격). 보내지 않았다.</summary>
        TooSoon,

        /// <summary>새로 만든 계정이라 아직 글을 쓸 수 없다. 보내지 않았다.</summary>
        NewAccount,

        /// <summary>서버가 받지 않았다 (규칙: 간격·차단·형식 등).</summary>
        Refused,

        /// <summary>연결이 안 됐다.</summary>
        Offline,
    }

    public sealed class BbsOnline
    {
        private readonly BbsOnlineConfig _config;
        private readonly string _identityPath;
        private readonly bool _testPath;
        private string _idToken;
        private string _refreshToken;
        private string _uid;
        private float _tokenUntil;

        /// <summary>이 계정을 처음 등록한 때·마지막으로 글을 올린 때 (내 컴퓨터 시계, 유닉스 ms). "n초 뒤에 다시"를 알려 주는 데만 쓴다.</summary>
        private long _firstSeenMs;
        private long _lastPostMs;

        /// <summary>
        /// identityPath: 계정 파일 자리 (비면 persistentDataPath/BbsOnline.json — 테스트는 다른 자리를 준다).
        /// testPath: 시험 경로를 쓸지 (비면 에디터·개발 빌드에서만).
        /// </summary>
        public BbsOnline(BbsOnlineConfig config = null, string identityPath = null, bool? testPath = null)
        {
            _config = config ?? BbsOnlineConfig.Load();
            _identityPath = string.IsNullOrEmpty(identityPath) ? System.IO.Path.Combine(Application.persistentDataPath, "BbsOnline.json") : identityPath;
            _testPath = testPath ?? DebugAccess.Enabled;
            LoadIdentity();
        }

        /// <summary>시험 경로(bbs/test)를 쓰는가. 시험 경로에는 새 계정 대기가 없다.</summary>
        public bool UsesTestPath => _testPath;

        /// <summary>지금 계정 ID (로그인 전이면 빈 글).</summary>
        public string Uid => _uid ?? "";

        public BbsOnlineConfig Config => _config;

        /// <summary>온라인 게시판을 쓸 수 있는 설정인가 (켜져 있고 주소·키가 있다).</summary>
        public bool Available => _config.Enabled && !string.IsNullOrEmpty(_config.DatabaseUrl) && !string.IsNullOrEmpty(_config.ApiKey);

        private string RootUrl => _config.DatabaseUrl.TrimEnd('/') + "/" + (_testPath ? _config.TestRootPath : _config.RootPath).Trim('/');

        private string BoardUrl => RootUrl + "/board.json";

        private string UserUrl(string child) => RootUrl + "/users/" + _uid + "/" + child + ".json";

        // ── 순수 함수 (테스트 대상) ──

        /// <summary>서버가 준 글 묶음(JSON 객체: 푸시 ID → 글)을 올린 차례대로 읽는다. 형식이 어긋난 글은 건너뛴다.</summary>
        public static List<BbsConfig.Post> ParsePosts(string json, BbsOnlineConfig config)
        {
            var result = new List<BbsConfig.Post>();
            if (string.IsNullOrWhiteSpace(json) || json.Trim() == "null")
            {
                return result;
            }

            JObject root;
            try
            {
                root = JObject.Parse(json);
            }
            catch (JsonException)
            {
                return result;
            }

            var entries = new List<(long Time, string Key, JObject Value)>();
            foreach (var property in root.Properties())
            {
                if (property.Value is JObject value)
                {
                    var time = value["ts"] != null && value["ts"].Type == JTokenType.Integer ? value.Value<long>("ts") : 0L;
                    entries.Add((time, property.Name, value));
                }
            }

            // 올린 차례 = 서버 시각 (같으면 글 ID)
            entries.Sort((a, b) => a.Time != b.Time ? a.Time.CompareTo(b.Time) : string.CompareOrdinal(a.Key, b.Key));
            foreach (var (_, _, value) in entries)
            {
                var title = Clean(value.Value<string>("title"), config.MaxTitle, config);
                var body = Clean(value.Value<string>("body"), config.MaxBody, config);
                if (title.Length == 0 || body.Length == 0)
                {
                    continue;
                }

                result.Add(new BbsConfig.Post
                {
                    Title = title,
                    Body = new[] { body },
                    Author = AuthorWithTag(Clean(value.Value<string>("author"), config.MaxAuthor, config), value.Value<string>("uid")),
                    Date = Clean(value.Value<string>("date"), 8, config),
                });
            }

            return result;
        }

        /// <summary>
        /// 계정 꼬리표: UID에서 뽑은 4글자 (헷갈리는 글자 0·O·1·I는 뺀 32자). 같은 이름을 쓰는 사람이 많아도 올린이를 구분한다.
        /// UID만으로 정해지니 이름을 바꿔도 같은 사람이다.
        /// </summary>
        public static string Tag(string uid)
        {
            if (string.IsNullOrEmpty(uid))
            {
                return "";
            }

            const string alphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";
            unchecked
            {
                var hash = 2166136261u;
                foreach (var c in uid)
                {
                    hash = (hash ^ c) * 16777619u;
                }

                var tag = new char[4];
                for (var i = 0; i < tag.Length; i++)
                {
                    tag[i] = alphabet[(int)(hash & 31u)];
                    hash >>= 5;
                }

                return new string(tag);
            }
        }

        /// <summary>화면에 보일 올린이: 이름(꼬리표).</summary>
        public static string AuthorWithTag(string author, string uid)
        {
            var tag = Tag(uid);
            return tag.Length == 0 ? author : author + "(" + tag + ")";
        }

        /// <summary>
        /// 글 올리기 요청의 본문: 글과 '마지막 글 시각'을 한 번에 쓴다 (뿌리 경로에 여러 자리 갱신).
        /// 보안 규칙이 둘이 함께 올 때만 받아 주므로, 글 사이 간격 제한을 건너뛸 수 없다.
        /// </summary>
        public static string BuildSend(BbsConfig.Post post, string uid, string postId, BbsOnlineConfig config)
        {
            var update = new JObject
            {
                ["board/" + postId] = JObject.Parse(BuildPost(post, uid, config)),
                ["users/" + uid + "/lastPost"] = new JObject { [".sv"] = "timestamp" },
            };
            return update.ToString(Formatting.None);
        }

        /// <summary>새 글 ID: 겹치지 않을 만큼 긴 무작위 글자 (차례는 서버 시각 ts로 정한다).</summary>
        public static string NewPostId()
        {
            return "p" + Guid.NewGuid().ToString("N");
        }

        /// <summary>올릴 글의 본문(JSON). ts는 서버가 채우는 시각이다.</summary>
        public static string BuildPost(BbsConfig.Post post, string uid, BbsOnlineConfig config)
        {
            var body = new JObject
            {
                ["title"] = Trim(post.Title, config.MaxTitle),
                ["body"] = Trim(post.Body != null && post.Body.Length > 0 ? post.Body[0] : "", config.MaxBody),
                ["author"] = Trim(post.Author, config.MaxAuthor),
                ["date"] = Trim(post.Date, 8),
                ["uid"] = uid ?? "",
                ["ts"] = new JObject { [".sv"] = "timestamp" },
            };
            return body.ToString(Formatting.None);
        }

        /// <summary>막아 둔 낱말이 들어 있는가 (올리기 전에 본다).</summary>
        public static bool IsBlocked(string text, BbsOnlineConfig config)
        {
            if (string.IsNullOrEmpty(text) || config.BlockedWords == null)
            {
                return false;
            }

            var lower = text.ToLowerInvariant();
            foreach (var word in config.BlockedWords)
            {
                if (!string.IsNullOrEmpty(word) && lower.Contains(word.ToLowerInvariant()))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>받은 글 다듬기: 줄바꿈·제어 문자를 빼고, 길이를 자르고, 막아 둔 낱말은 ○로 가린다.</summary>
        public static string Clean(string text, int max, BbsOnlineConfig config)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "";
            }

            var builder = new StringBuilder(text.Length);
            foreach (var c in text)
            {
                builder.Append(char.IsControl(c) ? ' ' : c);
            }

            var cleaned = Trim(builder.ToString().Replace("<", "‹").Replace(">", "›"), max);   // 글꼴 태그로 장난치지 못하게
            if (config.BlockedWords != null)
            {
                foreach (var word in config.BlockedWords)
                {
                    if (string.IsNullOrEmpty(word))
                    {
                        continue;
                    }

                    var index = cleaned.IndexOf(word, StringComparison.OrdinalIgnoreCase);
                    while (index >= 0)
                    {
                        cleaned = cleaned.Substring(0, index) + new string('○', word.Length) + cleaned.Substring(index + word.Length);
                        index = cleaned.IndexOf(word, index + word.Length, StringComparison.OrdinalIgnoreCase);
                    }
                }
            }

            return cleaned;
        }

        /// <summary>시계 차이·전송 시간을 봐서 조금 더 기다리게 한다 (초).</summary>
        public const int ClockMarginSeconds = 3;

        /// <summary>
        /// sinceMs에서 seconds초(+여유)가 지날 때까지 남은 초 (올림). 기록이 없거나(0) 이미 지났으면 0.
        /// 시계가 거꾸로 갔으면(기록이 미래) 가장 길어야 seconds+여유만 기다리게 한다.
        /// </summary>
        public static int WaitSeconds(long nowMs, long sinceMs, int seconds)
        {
            if (sinceMs <= 0 || seconds <= 0)
            {
                return 0;
            }

            var total = (seconds + ClockMarginSeconds) * 1000L;
            var left = Math.Min(total, sinceMs + total - nowMs);
            return left <= 0 ? 0 : (int)Math.Ceiling(left / 1000.0);
        }

        /// <summary>지금 글을 올릴 수 있는가 (내 컴퓨터가 아는 만큼): 새 계정 대기 → 글 사이 간격. 올릴 수 있으면 (Ok, 0).</summary>
        public (BbsSendResult Result, int Seconds) PostWait()
        {
            var now = NowMs();
            if (!_testPath)
            {
                var fresh = WaitSeconds(now, _firstSeenMs, _config.NewAccountWaitSeconds);
                if (fresh > 0)
                {
                    return (BbsSendResult.NewAccount, fresh);
                }
            }

            var gap = WaitSeconds(now, _lastPostMs, _config.PostIntervalSeconds);
            return gap > 0 ? (BbsSendResult.TooSoon, gap) : (BbsSendResult.Ok, 0);
        }

        private static long NowMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        private static string Trim(string text, int max)
        {
            text = (text ?? "").Trim();
            return text.Length > max ? text.Substring(0, max).TrimEnd() : text;
        }

        // ── 통신 ──

        /// <summary>최근 글을 받아 온다. 실패하면 null을 돌려준다 (화면은 내 컴퓨터의 글만 보여 준다).</summary>
        public void Fetch(Action<List<BbsConfig.Post>> done)
        {
            if (!Available)
            {
                done?.Invoke(null);
                return;
            }

            WithToken(token =>
            {
                if (token == null)
                {
                    done?.Invoke(null);
                    return;
                }

                // 올린 시각 차례로 최근 글. 시각 색인(.indexOn ts)이 규칙에 없으면 서버가 400을 주니, 그때는 글 ID 차례로라도 받는다
                Get(BoardUrl + "?orderBy=%22ts%22&limitToLast=" + Mathf.Max(1, _config.ReadLimit) + "&auth=" + UnityWebRequest.EscapeURL(token), (text, code) =>
                {
                    if (text != null || code != 400)
                    {
                        done?.Invoke(text != null ? ParsePosts(text, _config) : null);
                        return;
                    }

                    Get(BoardUrl + "?orderBy=%22%24key%22&limitToLast=" + Mathf.Max(1, _config.ReadLimit) + "&auth=" + UnityWebRequest.EscapeURL(token),
                        (fallback, _) => done?.Invoke(fallback != null ? ParsePosts(fallback, _config) : null));
                });
            });
        }

        private void Get(string url, Action<string, long> done)
        {
            var request = UnityWebRequest.Get(url);
            request.timeout = _config.TimeoutSeconds;
            request.SendWebRequest().completed += _ =>
            {
                var ok = request.result == UnityWebRequest.Result.Success;
                var text = ok ? request.downloadHandler.text : null;
                var code = request.responseCode;
                request.Dispose();
                done(text, code);
            };
        }

        /// <summary>
        /// 글을 올린다. 결과와, 기다려야 하면 남은 초를 돌려준다.
        /// 차례: 내 컴퓨터가 아는 대기(새 계정·글 사이 간격)면 보내지 않는다 → 이용자 기록이 올라간 뒤 → 글 + 마지막 글 시각을 한 번에.
        /// 막는 것은 보안 규칙이다 — 여기서 통과해도 서버가 거절할 수 있다(Refused).
        /// </summary>
        public void Send(BbsConfig.Post post, Action<BbsSendResult, int> done)
        {
            if (!Available)
            {
                done?.Invoke(BbsSendResult.Offline, 0);
                return;
            }

            var wait = PostWait();
            if (wait.Result != BbsSendResult.Ok)
            {
                done?.Invoke(wait.Result, wait.Seconds);
                return;
            }

            // 이용자 기록(첫 접속 시각)이 있어야 규칙이 글을 받는다 → 등록이 끝난 뒤에 보낸다
            Register(post.Author, registered =>
            {
                if (!registered)
                {
                    done?.Invoke(BbsSendResult.Offline, 0);
                    return;
                }

                // 방금 만든 계정이면 여기서 새 계정 대기가 생긴다
                wait = PostWait();
                if (wait.Result != BbsSendResult.Ok)
                {
                    done?.Invoke(wait.Result, wait.Seconds);
                    return;
                }

                WithToken(token =>
                {
                    if (token == null)
                    {
                        done?.Invoke(BbsSendResult.Offline, 0);
                        return;
                    }

                    // 글 + 마지막 글 시각을 한 번에 (PATCH = 여러 자리 갱신). 규칙이 간격·차단·새 계정 대기를 본다
                    var request = new UnityWebRequest(RootUrl + ".json?auth=" + UnityWebRequest.EscapeURL(token), "PATCH")
                    {
                        uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(BuildSend(post, _uid, NewPostId(), _config))),
                        downloadHandler = new DownloadHandlerBuffer(),
                        timeout = _config.TimeoutSeconds,
                    };
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.SendWebRequest().completed += _ =>
                    {
                        var ok = request.result == UnityWebRequest.Result.Success;
                        var code = request.responseCode;
                        request.Dispose();
                        if (ok)
                        {
                            _lastPostMs = NowMs();
                            SaveIdentity();
                            done?.Invoke(BbsSendResult.Ok, 0);
                        }
                        else
                        {
                            // 규칙이 막으면 401/403(Permission denied), 형식이 틀리면 400. 그 밖은 회선 문제다
                            done?.Invoke(code == 400 || code == 401 || code == 403 ? BbsSendResult.Refused : BbsSendResult.Offline, 0);
                        }
                    };
                });
            });
        }

        /// <summary>
        /// 이용자 기록을 남긴다: 처음이면 첫 접속 시각, 그리고 지금 이름. PC통신을 알게 된 뒤 허브에 들어올 때와 게시판을 열 때 부른다
        /// (새 계정은 첫 접속 뒤 얼마 동안 글을 못 쓰게 보안 규칙이 막는다 — 계정을 새로 만들어 도배하기 어렵게. 그래서 미리 해 둔다).
        /// 한 번 끝나면 다시 하지 않고, 하는 중에 또 부르면 같은 결과를 기다린다. done(false) = 연결이 안 돼 못 했다 (다음에 다시 한다).
        /// </summary>
        public void Register(string name, Action<bool> done = null)
        {
            if (!Available)
            {
                done?.Invoke(false);
                return;
            }

            if (_registerState == 2)
            {
                done?.Invoke(true);
                return;
            }

            if (done != null)
            {
                _registerWaiters.Add(done);
            }

            if (_registerState == 1)
            {
                return;
            }

            _registerState = 1;
            WithToken(token =>
            {
                if (token == null || string.IsNullOrEmpty(_uid))
                {
                    FinishRegister(false);
                    return;
                }

                // 첫 접속 시각은 한 번만 쓸 수 있다. 됐으면 방금 만든 계정이고, 규칙이 거절(401/403)했으면 이미 있는 계정이다
                Put(UserUrl("firstSeen"), "{\".sv\":\"timestamp\"}", token, (ok, code) =>
                {
                    if (ok)
                    {
                        _firstSeenMs = NowMs();
                        SaveIdentity();
                    }
                    else if (code != 401 && code != 403)
                    {
                        FinishRegister(false);   // 회선 문제: 다음에 다시
                        return;
                    }

                    Put(UserUrl("name"), JsonConvert.SerializeObject(Trim(name, _config.MaxAuthor)), token, (_, __) => FinishRegister(true));
                });
            });
        }

        /// <summary>0 = 아직, 1 = 하는 중, 2 = 끝.</summary>
        private int _registerState;
        private readonly List<Action<bool>> _registerWaiters = new List<Action<bool>>();

        private void FinishRegister(bool ok)
        {
            _registerState = ok ? 2 : 0;
            var waiters = _registerWaiters.ToArray();
            _registerWaiters.Clear();
            foreach (var waiter in waiters)
            {
                waiter(ok);
            }
        }

        /// <summary>이 계정의 꼬리표 (로그인 전이면 빈 글).</summary>
        public string MyTag => Tag(_uid);

        private void Put(string url, string json, string token, Action<bool, long> done)
        {
            var request = new UnityWebRequest(url + "?auth=" + UnityWebRequest.EscapeURL(token), UnityWebRequest.kHttpVerbPUT)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = _config.TimeoutSeconds,
            };
            request.SetRequestHeader("Content-Type", "application/json");
            request.SendWebRequest().completed += _ =>
            {
                var ok = request.result == UnityWebRequest.Result.Success;
                var code = request.responseCode;
                request.Dispose();
                done?.Invoke(ok, code);
            };
        }

        /// <summary>쓸 수 있는 토큰을 준비해 넘긴다: 아직 살아 있으면 그대로, 갱신 토큰이 있으면 갱신, 없으면 익명 로그인. 실패하면 null.</summary>
        /// <remarks>
        /// 토큰을 받는 중에 또 부르면 같은 결과를 기다린다. 예전에는 등록과 목록 받기가 동시에 로그인해
        /// 처음 접속할 때 익명 계정이 둘 만들어졌다 — 첫 접속 기록은 한 계정에, 저장된 신분은 다른 계정에 남아 그날 글이 거절됐다.
        /// </remarks>
        private void WithToken(Action<string> use)
        {
            if (!string.IsNullOrEmpty(_idToken) && Time.realtimeSinceStartup < _tokenUntil)
            {
                use(_idToken);
                return;
            }

            _tokenWaiters.Add(use);
            if (_tokenBusy)
            {
                return;
            }

            _tokenBusy = true;
            RequestToken(token =>
            {
                _tokenBusy = false;
                var waiters = _tokenWaiters.ToArray();
                _tokenWaiters.Clear();
                foreach (var waiter in waiters)
                {
                    waiter(token);
                }
            });
        }

        private bool _tokenBusy;
        private readonly List<Action<string>> _tokenWaiters = new List<Action<string>>();

        private void RequestToken(Action<string> use)
        {
            if (!string.IsNullOrEmpty(_refreshToken))
            {
                var form = "grant_type=refresh_token&refresh_token=" + UnityWebRequest.EscapeURL(_refreshToken);
                var refresh = new UnityWebRequest("https://securetoken.googleapis.com/v1/token?key=" + _config.ApiKey, UnityWebRequest.kHttpVerbPOST)
                {
                    uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(form)),
                    downloadHandler = new DownloadHandlerBuffer(),
                    timeout = _config.TimeoutSeconds,
                };
                refresh.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
                refresh.SendWebRequest().completed += _ =>
                {
                    var ok = refresh.result == UnityWebRequest.Result.Success && Accept(refresh.downloadHandler.text, "id_token", "refresh_token", "user_id", "expires_in");
                    var code = refresh.responseCode;
                    refresh.Dispose();
                    if (ok)
                    {
                        use(_idToken);
                    }
                    else if (code == 400 || code == 401 || code == 403)
                    {
                        // 갱신 토큰이 죽었다(서버가 거절) → 새로 로그인한다. 새 계정이니 기록도 처음부터
                        _refreshToken = null;
                        SignIn(use);
                    }
                    else
                    {
                        // 회선 문제면 계정을 새로 만들지 않는다 (잠깐 끊겼다고 신분이 바뀌면 안 된다)
                        use(null);
                    }
                };
                return;
            }

            SignIn(use);
        }

        private void SignIn(Action<string> use)
        {
            var request = new UnityWebRequest("https://identitytoolkit.googleapis.com/v1/accounts:signUp?key=" + _config.ApiKey, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes("{\"returnSecureToken\":true}")),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = _config.TimeoutSeconds,
            };
            request.SetRequestHeader("Content-Type", "application/json");
            request.SendWebRequest().completed += _ =>
            {
                var ok = request.result == UnityWebRequest.Result.Success && Accept(request.downloadHandler.text, "idToken", "refreshToken", "localId", "expiresIn");
                request.Dispose();
                use(ok ? _idToken : null);
            };
        }

        private bool Accept(string json, string tokenKey, string refreshKey, string uidKey, string expiresKey)
        {
            try
            {
                var root = JObject.Parse(json);
                var token = root.Value<string>(tokenKey);
                if (string.IsNullOrEmpty(token))
                {
                    return false;
                }

                var uid = root.Value<string>(uidKey) ?? _uid;
                if (!string.IsNullOrEmpty(_uid) && uid != _uid)
                {
                    // 계정이 바뀌었다 (갱신 토큰이 죽어 새로 로그인): 이용자 기록·대기 시간을 처음부터
                    _registerState = 0;
                    _firstSeenMs = 0;
                    _lastPostMs = 0;
                }

                _idToken = token;
                _refreshToken = root.Value<string>(refreshKey) ?? _refreshToken;
                _uid = uid;
                var seconds = int.TryParse(root.Value<string>(expiresKey), out var parsed) ? parsed : 3600;
                _tokenUntil = Time.realtimeSinceStartup + Mathf.Max(60, seconds - 120);
                SaveIdentity();
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        // ── 익명 계정 (이 컴퓨터의 게시판 신분) ──

        [Serializable]
        private sealed class Identity
        {
            public string Uid = "";
            public string RefreshToken = "";

            /// <summary>이 계정을 처음 등록한 때·마지막으로 글을 올린 때 (내 컴퓨터 시계, 유닉스 ms, 0 = 모름·오래됨).</summary>
            public long FirstSeenMs;
            public long LastPostMs;
        }

        private string IdentityPath => _identityPath;

        private void LoadIdentity()
        {
            try
            {
                if (File.Exists(IdentityPath))
                {
                    var identity = JsonUtility.FromJson<Identity>(File.ReadAllText(IdentityPath));
                    _uid = identity?.Uid;
                    _refreshToken = identity?.RefreshToken;
                    _firstSeenMs = identity?.FirstSeenMs ?? 0;
                    _lastPostMs = identity?.LastPostMs ?? 0;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("PC통신 계정 읽기 실패: " + exception.Message);
            }
        }

        private void SaveIdentity()
        {
            try
            {
                File.WriteAllText(IdentityPath, JsonUtility.ToJson(new Identity
                {
                    Uid = _uid ?? "", RefreshToken = _refreshToken ?? "", FirstSeenMs = _firstSeenMs, LastPostMs = _lastPostMs,
                }));
            }
            catch (Exception exception)
            {
                Debug.LogWarning("PC통신 계정 저장 실패: " + exception.Message);
            }
        }
    }
}
