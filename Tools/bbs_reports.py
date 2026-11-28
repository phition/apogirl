"""PC통신 열린 게시판: 신고된 글 모아 보기 (운영 도구, 읽기만 한다).

쓰는 법 (저장소 루트에서):
    python Tools/bbs_reports.py            정식 게시판(bbs/v1)
    python Tools/bbs_reports.py --test     시험 게시판(bbs/test)
    python Tools/bbs_reports.py --all      신고가 없는 글까지 모두

신고가 많은 글부터 글 ID·올린 계정·내용·신고한 계정 수를 보여 준다. 이 도구는 아무것도 고치지 못한다(보안 규칙이 막는다) —
글 지우기와 계정 차단은 Firebase 콘솔에서 한다. 방법은 맨 아래에 함께 나오고, Docs/BBS_ONLINE.md 5절에도 있다.

읽을 때 쓰는 익명 계정은 처음 한 번 만들어 ~/.apogirl/bbs_tool_account.json에 둔다 (게임의 계정 파일은 건드리지 않는다).
"""
import io
import json
import sys
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path

if hasattr(sys.stdout, 'reconfigure'):
    sys.stdout.reconfigure(encoding='utf-8')

ROOT = Path(__file__).resolve().parent.parent
CONFIG = ROOT / 'Assets' / '_Project' / 'Resources' / 'Bbs' / 'bbs_online.json'
ACCOUNT = Path.home() / '.apogirl' / 'bbs_tool_account.json'
READ_LIMIT = 300


def call(url, data=None, headers=None):
    request = urllib.request.Request(url, data=data, headers=headers or {})
    try:
        with urllib.request.urlopen(request, timeout=20) as response:
            return response.status, response.read().decode('utf-8')
    except urllib.error.HTTPError as error:
        return error.code, error.read().decode('utf-8')
    except urllib.error.URLError as error:
        return 0, str(error)


def token(key):
    """익명 계정의 토큰: 갱신 토큰이 있으면 갱신, 없으면 계정을 새로 만든다."""
    if ACCOUNT.exists():
        saved = json.loads(ACCOUNT.read_text(encoding='utf-8'))
        code, body = call('https://securetoken.googleapis.com/v1/token?key=' + key,
                          urllib.parse.urlencode({'grant_type': 'refresh_token', 'refresh_token': saved['refresh']}).encode())
        if code == 200:
            return json.loads(body)['id_token']
        if code not in (400, 401, 403):
            sys.exit('로그인 실패 (회선): %s %s' % (code, body[:200]))

    code, body = call('https://identitytoolkit.googleapis.com/v1/accounts:signUp?key=' + key,
                      b'{"returnSecureToken":true}', {'Content-Type': 'application/json'})
    if code != 200:
        sys.exit('익명 로그인 실패: %s %s' % (code, body[:200]))
    made = json.loads(body)
    ACCOUNT.parent.mkdir(parents=True, exist_ok=True)
    ACCOUNT.write_text(json.dumps({'uid': made['localId'], 'refresh': made['refreshToken']}), encoding='utf-8')
    return made['idToken']


def tag(uid):
    """계정 꼬리표 (게임의 BbsOnline.Tag와 같은 셈)."""
    if not uid:
        return ''
    alphabet = '23456789ABCDEFGHJKLMNPQRSTUVWXYZ'
    value = 2166136261
    for c in uid:
        value = ((value ^ ord(c)) * 16777619) & 0xFFFFFFFF
    out = ''
    for _ in range(4):
        out += alphabet[value & 31]
        value >>= 5
    return out


def main():
    test = '--test' in sys.argv
    show_all = '--all' in sys.argv
    config = json.load(io.open(CONFIG, encoding='utf-8-sig'))
    base = config['DatabaseUrl'].rstrip('/')
    path = (config['TestRootPath'] if test else config['RootPath']).strip('/')
    hide = int(config.get('ReportHideCount', 3))
    auth = 'auth=' + token(config['ApiKey'])

    def get(child, query=''):
        code, body = call('%s/%s/%s.json?%s%s' % (base, path, child, query, auth))
        if code != 200:
            return None, code
        return json.loads(body) or {}, code

    board, code = get('board', 'orderBy=%22ts%22&limitToLast=' + str(READ_LIMIT) + '&')
    if board is None:
        sys.exit('게시판을 읽지 못했습니다: %s' % code)
    reports, code = get('reports')
    if reports is None:
        print('! 신고 기록을 읽지 못했습니다 (%s). 신고 규칙이 아직 게시되지 않았으면 Docs/BBS_ONLINE.md 3.2절의 규칙을 게시하세요.\n' % code)
        reports = {}
    banned, code = get('banned')
    if banned is None:
        print('! 차단 목록을 읽지 못했습니다 (%s).\n' % code)
        banned = {}

    project = base.split('//')[-1].split('.')[0]
    project_id = project.replace('-default-rtdb', '')

    def console(child):
        return 'https://console.firebase.google.com/project/%s/database/%s/data/~2F%s' % (project_id, project, (path + '/' + child).replace('/', '~2F'))

    print('〈%s〉  글 %d편 (최근 %d편까지) · 신고된 글 %d편 · 차단된 계정 %d개 · 가리는 문턱 %d명\n'
          % (path, len(board), READ_LIMIT, len([k for k in reports if isinstance(reports[k], dict)]), len(banned), hide))

    rows = []
    for post_id, post in board.items():
        if not isinstance(post, dict):
            continue
        reporters = reports.get(post_id) if isinstance(reports.get(post_id), dict) else {}
        if reporters or show_all:
            rows.append((len(reporters), post.get('ts', 0), post_id, post, reporters))
    rows.sort(key=lambda row: (-row[0], -row[1]))

    if not rows:
        print('신고된 글이 없습니다.')
    for count, _, post_id, post, reporters in rows:
        uid = post.get('uid', '')
        state = []
        if hide > 0 and count >= hide:
            state.append('모두에게 가려짐')
        if uid in banned:
            state.append('차단된 계정')
        print('■ 신고 %d명%s' % (count, '  [' + ' · '.join(state) + ']' if state else ''))
        print('  글 ID   %s' % post_id)
        print('  올린이  %s(%s)   계정 %s' % (post.get('author', ''), tag(uid), uid))
        print('  날짜    %s' % post.get('date', ''))
        print('  제목    %s' % post.get('title', ''))
        print('  내용    %s' % post.get('body', ''))
        if reporters:
            print('  신고한 계정  %s' % ', '.join('%s(%s)' % (r, tag(r)) for r in reporters))
        print('  콘솔    %s' % console('board/' + post_id))
        print()

    gone = [k for k in reports if k not in board and isinstance(reports[k], dict)]
    if gone:
        print('글이 이미 지워졌거나 목록 밖인 신고 기록 %d건: %s' % (len(gone), ', '.join(gone[:20])))
        print('  (콘솔에서 %s 아래의 그 항목을 지우면 정리됩니다)\n' % console('reports'))
    if banned:
        print('차단된 계정: %s\n' % ', '.join('%s(%s)' % (u, tag(u)) for u in banned))

    print('── 처리하는 법 (Firebase 콘솔 → Realtime Database → 데이터) ──')
    print('  글 지우기      %s/{글 ID} 를 지운다 (신고 기록 reports/{글 ID}도 함께 지우면 깔끔하다)' % (path + '/board'))
    print('  계정 차단      %s/{계정} 에 true 를 넣는다 → 그 계정은 글·신고를 못 하고, 지난 글도 모두에게 가려진다' % (path + '/banned'))
    print('  차단 풀기      %s/{계정} 을 지운다' % (path + '/banned'))
    print('  신고 물리기    %s/{글 ID} 를 지운다 → 가려졌던 글이 다시 보인다' % (path + '/reports'))
    print('  금칙어 고치기  %s 에 words · remove · allow 를 쉼표로 나눈 글 한 줄로 넣는다 (게임에 든 목록에 얹힌다)' % (path + '/filter'))


if __name__ == '__main__':
    main()
