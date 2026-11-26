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

    public sealed class BbsOnline
    {
        private readonly BbsOnlineConfig _config;
        private string _idToken;
        private string _refreshToken;
        private string _uid;
        private float _tokenUntil;

        public BbsOnline(BbsOnlineConfig config = null)
        {
            _config = config ?? BbsOnlineConfig.Load();
            LoadIdentity();
        }

        public BbsOnlineConfig Config => _config;

        /// <summary>온라인 게시판을 쓸 수 있는 설정인가 (켜져 있고 주소·키가 있다).</summary>
        public bool Available => _config.Enabled && !string.IsNullOrEmpty(_config.DatabaseUrl) && !string.IsNullOrEmpty(_config.ApiKey);

        private string RootUrl => _config.DatabaseUrl.TrimEnd('/') + "/" + (DebugAccess.Enabled ? _config.TestRootPath : _config.RootPath).Trim('/');

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

        /// <summary>글을 올린다. 성공 여부를 돌려준다.</summary>
        public void Send(BbsConfig.Post post, Action<bool> done)
        {
            if (!Available)
            {
                done?.Invoke(false);
                return;
            }

            WithToken(token =>
            {
                if (token == null)
                {
                    done?.Invoke(false);
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
                    request.Dispose();
                    done?.Invoke(ok);
                };
            });
        }

        /// <summary>
        /// 이용자 기록을 남긴다: 처음이면 첫 접속 시각, 그리고 지금 이름. 게시판에 들어올 때 부른다
        /// (새 계정은 첫 접속 뒤 얼마 동안 글을 못 쓰게 보안 규칙이 막는다 — 계정을 새로 만들어 도배하기 어렵게).
        /// </summary>
        public void Register(string name)
        {
            if (!Available || _registered)
            {
                return;
            }

            WithToken(token =>
            {
                if (token == null || string.IsNullOrEmpty(_uid))
                {
                    return;
                }

                _registered = true;
                // 첫 접속 시각은 한 번만 쓸 수 있다 (규칙: 이미 있으면 거절) — 거절돼도 괜찮다
                Put(UserUrl("firstSeen"), "{\".sv\":\"timestamp\"}", token, null);
                Put(UserUrl("name"), JsonConvert.SerializeObject(Trim(name, _config.MaxAuthor)), token, null);
            });
        }

        private bool _registered;

        /// <summary>이 계정의 꼬리표 (로그인 전이면 빈 글).</summary>
        public string MyTag => Tag(_uid);

        private void Put(string url, string json, string token, Action<bool> done)
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
                request.Dispose();
                done?.Invoke(ok);
            };
        }

        /// <summary>쓸 수 있는 토큰을 준비해 넘긴다: 아직 살아 있으면 그대로, 갱신 토큰이 있으면 갱신, 없으면 익명 로그인. 실패하면 null.</summary>
        private void WithToken(Action<string> use)
        {
            if (!string.IsNullOrEmpty(_idToken) && Time.realtimeSinceStartup < _tokenUntil)
            {
                use(_idToken);
                return;
            }

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
                    refresh.Dispose();
                    if (ok)
                    {
                        use(_idToken);
                    }
                    else
                    {
                        _refreshToken = null;   // 갱신 토큰이 죽었으면 새로 로그인한다
                        SignIn(use);
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

                _idToken = token;
                _refreshToken = root.Value<string>(refreshKey) ?? _refreshToken;
                _uid = root.Value<string>(uidKey) ?? _uid;
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
        }

        private static string IdentityPath => System.IO.Path.Combine(Application.persistentDataPath, "BbsOnline.json");

        private void LoadIdentity()
        {
            try
            {
                if (File.Exists(IdentityPath))
                {
                    var identity = JsonUtility.FromJson<Identity>(File.ReadAllText(IdentityPath));
                    _uid = identity?.Uid;
                    _refreshToken = identity?.RefreshToken;
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
                File.WriteAllText(IdentityPath, JsonUtility.ToJson(new Identity { Uid = _uid ?? "", RefreshToken = _refreshToken ?? "" }));
            }
            catch (Exception exception)
            {
                Debug.LogWarning("PC통신 계정 저장 실패: " + exception.Message);
            }
        }
    }
}
