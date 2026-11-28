# PC통신 온라인 게시판 (Firebase)

PC통신 '이제누리'의 **열린 게시판**을 다른 플레이어와 함께 쓰는 게시판으로 만든다 (사용자 요청 2026-11-26). 대화방은 아직 로컬이다.

## 1. 구성

- 저장소: Firebase 실시간 데이터베이스 (프로젝트 `apogirl-7aa98`, 리전 `asia-southeast1`).
- 통신: SDK 없이 REST (`UnityWebRequest`) — Firebase 유니티 SDK는 PC를 정식 지원하지 않아서다. 코드는 `Scripts/Presentation/Screens/BbsOnline.cs`.
- 신분: 익명 로그인. 처음 한 번 계정을 만들고, 갱신 토큰을 `persistentDataPath/BbsOnline.json`에 둔다 (이 컴퓨터의 게시판 신분). **IP는 쓰지도 남기지도 않는다.**
- 설정: `Resources/Bbs/bbs_online.json` — `Enabled`(끄면 예전처럼 내 컴퓨터에만), 주소, 웹 API 키, 뿌리 경로, 받는 글 수, 길이 한도, 금칙어.
- 뿌리 경로: 정식 빌드 `bbs/v1`, 에디터·개발 빌드 `bbs/test` (시험 글이 정식 게시판에 섞이지 않게).

## 2. 자료 모양

```
bbs/{v1|test}/
  board/{글 ID}
    title   제목 (30자까지)
    body    내용 한 줄 (120자까지)
    author  올린이 = 게임 속 주인공 이름 (12자까지)
    date    게임 안 날짜 "MM.DD"
    uid     익명 계정 ID
    ts      서버 시각 (올린 차례)
  users/{uid}
    firstSeen  첫 접속 시각 (한 번만 쓸 수 있다)
    lastPost   마지막으로 글을 올린 시각
    name       지금 이름
  banned/{uid}: true    차단한 계정 (콘솔에서만 넣는다)
```

- 목록은 최근 40건을 서버 시각 차례로 받는다.
- 올린이는 **이름(꼬리표)** 로 보인다. 꼬리표는 UID에서 뽑은 4글자(`BbsOnline.Tag`)라, 같은 이름을 쓰는 사람이 많아도 구분되고 이름을 바꿔도 같은 사람이다. 예: `한서진(3F2A)`.
- 글 올리기는 **글과 `lastPost`를 한 번의 쓰기로** 보낸다 (뿌리 경로에 PATCH). 규칙이 둘이 함께 올 때만 받아 주므로 글 사이 간격을 건너뛸 수 없다.
- **로그인은 한 번에 하나만** (`WithToken`의 대기 줄): 토큰을 받는 중에 또 부르면 같은 결과를 기다린다. 2026-11-28 전에는 게시판을 처음 열 때 등록과 목록 받기가 동시에 로그인해 익명 계정이 둘 만들어졌고, 첫 접속 기록이 저장된 계정과 다른 계정에 남아 그날 글이 거절됐다.
- **이용자 기록(`Register`)은 미리**: PC통신을 알게 된 뒤(`BBS_KNOWN`) 허브에 들어올 때와 게시판을 열 때 한다(`BbsView.Warm`). 새 계정 2분 대기가 컴퓨터 앞에 앉기 전에 끝나게 하려는 것이다(접속 시간은 한 주 180초뿐이다).
- **글 올리기 차례** (`BbsOnline.Send`): 내 컴퓨터가 아는 대기면 보내지 않고 남은 초를 알려 준다 → 이용자 기록이 올라간 뒤 → 글 + 시각을 한 번에. 결과 `BbsSendResult`: `Ok` · `TooSoon`(방금 올림) · `NewAccount`(새 회선) · `Refused`(서버가 거절: 401/403/400) · `Offline`(연결 안 됨). 화면 안내도 결과마다 다르다(`bbs.write_too_soon`·`write_new_line`·`write_refused`·`write_offline`).
- 대기 시간은 계정 파일(`BbsOnline.json`의 `FirstSeenMs`·`LastPostMs`, 내 컴퓨터 시계)로 셈하고 3초 여유를 둔다. **막는 것은 규칙이다** — 파일을 고쳐 보내도 서버가 거절한다. 간격 값은 `bbs_online.json`의 `PostIntervalSeconds`(60)·`NewAccountWaitSeconds`(120)이고 규칙의 60000·120000과 같아야 한다.
- 갱신 토큰이 실패했을 때, 서버가 거절(400/401/403)했을 때만 새로 로그인한다. 회선이 잠깐 끊긴 것으로 계정이 바뀌지 않는다.
- 게시판 화면: 처음부터 있던 글 → 받은 글 → 내 컴퓨터에만 있는 글. 받는 동안에는 있던 글을 먼저 보여 준다.
- 올리기에 실패하거나 규칙에 막히면 글을 내 컴퓨터(`Bbs.json`)에만 남긴다.
- 금칙어가 든 글은 올리지 않고, 받은 글의 금칙어는 ○로 가린다. 글꼴 태그(`<`, `>`)와 줄바꿈도 걸러 낸다.

## 3. 콘솔에서 해야 할 일 (사용자)

### 3.1 익명 로그인 켜기 — 완료 (2026-11-26)
Firebase 콘솔 → Authentication → Sign-in method → **익명(Anonymous)** 사용 설정.

### 3.2 보안 규칙
Firebase 콘솔 → Realtime Database → 규칙에 아래를 붙여 넣고 게시한다. 2026-11-27에 게시한 규칙에 **경로 제한**(`$env`가 `v1`·`test`일 때만)을 더했다(2026-11-28) — **다시 게시 완료 (2026-11-28, 사용자).** 예전 규칙은 `bbs/아무이름/…`에도 똑같이 열려 있어서(읽기로 확인: `bbs/zzprobe/board` 200), 로그인한 누구나 정해 두지 않은 경로에 자료를 쌓을 수 있었다.

```json
{
  "rules": {
    ".read": false,
    ".write": false,
    "bbs": {
      "$env": {
        "banned": {
          ".read": false,
          ".write": false
        },
        "users": {
          "$uid": {
            ".read": "($env === 'v1' || $env === 'test') && auth != null && auth.uid === $uid",
            "firstSeen": {
              ".write": "($env === 'v1' || $env === 'test') && auth != null && auth.uid === $uid && !data.exists()",
              ".validate": "newData.val() === now"
            },
            "lastPost": {
              ".write": "($env === 'v1' || $env === 'test') && auth != null && auth.uid === $uid && newData.exists() && (!data.exists() || now - data.val() >= 60000)",
              ".validate": "newData.val() === now"
            },
            "name": {
              ".write": "($env === 'v1' || $env === 'test') && auth != null && auth.uid === $uid",
              ".validate": "newData.isString() && newData.val().length <= 12"
            },
            "$other": { ".validate": false }
          }
        },
        "board": {
          ".read": "($env === 'v1' || $env === 'test') && auth != null",
          ".indexOn": ["ts"],
          "$post": {
            ".write": "($env === 'v1' || $env === 'test') && auth != null && !data.exists() && newData.exists() && !root.child('bbs').child($env).child('banned').child(auth.uid).exists() && root.child('bbs').child($env).child('users').child(auth.uid).child('firstSeen').exists() && ($env === 'test' || now - root.child('bbs').child($env).child('users').child(auth.uid).child('firstSeen').val() >= 120000)",
            ".validate": "newData.hasChildren(['title', 'body', 'author', 'date', 'uid', 'ts']) && newData.parent().parent().child('users').child(auth.uid).child('lastPost').val() === now",
            "title": { ".validate": "newData.isString() && newData.val().length > 0 && newData.val().length <= 30" },
            "body": { ".validate": "newData.isString() && newData.val().length > 0 && newData.val().length <= 120" },
            "author": { ".validate": "newData.isString() && newData.val().length <= 12" },
            "date": { ".validate": "newData.isString() && newData.val().length <= 8" },
            "uid": { ".validate": "newData.val() === auth.uid" },
            "ts": { ".validate": "newData.val() === now" },
            "$other": { ".validate": false }
          }
        }
      }
    }
  }
}
```

규칙이 하는 일:

| 규칙 | 뜻 |
|---|---|
| 로그인한 사람만 읽기 | 익명 계정이라도 있어야 목록을 받는다 |
| 새 글만 쓰기 | 고치기·지우기 불가. 글 지우기는 콘솔에서 한다 |
| 글 사이 60초 | `lastPost`가 60초 지나야 다시 쓸 수 있고, 글은 `lastPost`와 함께 올 때만 받는다 |
| 새 계정 대기 2분 | 첫 접속(`firstSeen`) 뒤 2분이 지나야 글을 쓸 수 있다 — 계정 파일을 지우고 새로 만들어 도배하기 어렵게. 시험 경로(`test`)에서는 기다리지 않는다 |
| 차단 | `bbs/v1/banned/{uid}: true`를 콘솔에서 넣으면 그 계정은 글을 못 쓴다 |
| 형식 | 길이 한도, 자기 UID, 서버 시각만 받는다. 다른 칸은 못 넣는다 |
| 경로 (2026-11-28) | `bbs/v1`·`bbs/test`만 읽고 쓸 수 있다. 다른 이름의 경로는 모두 거절 |

- 길이 한도는 `bbs_online.json`의 `MaxTitle`·`MaxBody`·`MaxAuthor`와 같아야 한다.
- 수치(60000 = 60초, 120000 = 2분)는 규칙에서 고치고, `bbs_online.json`의 `PostIntervalSeconds`·`NewAccountWaitSeconds`도 같이 맞춘다(화면의 "n초 뒤에 다시"가 이 값을 쓴다).
- 규칙을 올리기 전에는 시각 색인이 없어 글 ID 차례(뒤섞인 차례)로 받는다 (`BbsOnline.Fetch`의 예비 경로).

### 3.3 글 지우기·계정 차단
콘솔 → Realtime Database → 데이터에서 `bbs/v1/board/{글 ID}`를 지운다. 그 글의 `uid`를 `bbs/v1/banned/{uid}: true`로 넣으면 차단이다.

## 4. 남은 일

- 신고·숨김, 관리자 도구 없음 (콘솔에서 직접 지운다).
- 계정 파일을 지우면 새 계정이 된다 (2분 대기가 있을 뿐이다). 스팀에 올리면 스팀 계정 로그인으로 바꾸는 것이 가장 확실하다 — `BbsOnline`의 로그인 부분만 바꾸면 된다.
- 대화방 온라인 연결.
- 웹 API 키는 공개 값이지만, 구글 클라우드 콘솔에서 쓸 수 있는 API를 Identity Toolkit·Token Service로 좁혀 두면 좋다.
- 2026-11-26에 REST 흐름(익명 로그인 → 첫 접속 기록 → 글 + 시각 한 번에 쓰기 → 읽기)을 시험 경로에서 확인했다.
- 2026-11-27에 사용자가 보안 규칙을 게시했고, 시험 경로에서 25가지를 확인했다: 로그인 없이·루트 읽기 거절, 게시판·시각 차례 읽기(색인) 됨, 첫 접속 기록 없이 글쓰기 거절(정식 경로도), 첫 접속 다시 쓰기·남의 기록 쓰기 거절, 글 + 시각 쓰기 됨, 60초 안 다시 쓰기 거절·62초 뒤 됨, 글 고치기·지우기·마지막 글 시각 지우기·차단 목록 쓰기 거절, 시각 없이 글만·제목 31자·내용 121자·남의 uid·내 마음대로 시각·없는 칸 거절. **확인하지 않은 것**: 정식 경로의 새 계정 2분 대기(정식 경로에 기록이 남아서 하지 않음)와 차단 목록의 효과(차단은 콘솔에서만 넣을 수 있음). 시험 글 2건과 시험 계정 기록이 `bbs/test`에 남아 있다(규칙상 클라이언트는 지울 수 없으니 콘솔에서 지운다).
- **2026-11-28 다시 맞춤**: 위 2절의 로그인·등록·글 올리기 차례를 고치고, 시험 경로에서 게임 코드로 직접 확인했다(임시 PlayMode 테스트, 계정 파일은 임시 자리): 등록과 목록 받기를 동시에 불러도 계정 하나 · 저장된 계정 = 등록한 계정 · 글 올리기 됨 · 다시 받으면 `이름(꼬리표)`로 보임 · 곧바로 또 올리면 보내지 않고 `TooSoon`(63초) · 껐다 켜도 같은 계정이고 간격을 기억함 · 계정 파일의 간격 기록을 지우고 보내면 서버가 `Refused`. 시험 글 1건("다시 맞춤 …")과 시험 계정 2개(확인용·읽기 확인용)가 더 남았다 — 콘솔에서 지우면 된다.
- **2026-11-28 경로 제한 규칙 게시 확인** (사용자가 콘솔에 게시한 뒤, 확인용 계정으로): 읽기 — `bbs/test/board`·`bbs/v1/board`·시험 경로의 내 기록 200, `bbs/zzprobe/board`·`bbs/zzprobe/users/{내 UID}`·남의 기록·차단 목록·뿌리·로그인 없는 읽기 401. 쓰기 — `bbs/zzprobe`에 이름·첫 접속·글 쓰기 모두 401(남은 것 없음). 새 규칙 아래에서 게임 코드로 시험 경로 흐름도 다시 확인(임시 PlayMode 테스트): 등록·목록 받기 됨 · 글 올리기 됨 · `이름(꼬리표)`로 보임 · 곧바로 또 올리면 `TooSoon`(63초) · 다시 열어도 같은 계정 · 간격 기록을 지우고 보내면 `Refused` — 시험 글 1건·시험 계정 1개가 더 남았다. 확인 도구 `code_temp/apogirl/tools/fablable_shots/bbs_rules_check.py`·`bbs_rules_check_write.py`.
- **아직 확인하지 못한 것**: 정식 경로의 새 계정 2분 대기(화면 안내는 테스트로, 서버 쪽은 규칙 그대로), 차단 목록의 효과(콘솔에서만 넣을 수 있다), 실제 게임 화면에서 타이핑해 올리기.
- **생각해 볼 것**: 연결이 안 되거나 대기 중이라 내 컴퓨터에만 남은 글은 나중에 다시 보내지 않는다(목록 끝에 내 글로만 보인다). 다시 보내기를 넣을지는 정하지 않았다.
