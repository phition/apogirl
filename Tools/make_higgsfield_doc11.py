"""힉스필드 에셋 11차 전체 문서(Docs/HIGGSFIELD_ASSETS_11_ALL.md) 만들기: 탐험 주인공 스프라이트 전부를 24프레임으로.
샘플(여름 교복 야구방망이 공격, HIGGSFIELD_ASSETS_11.md)과 같은 방식 — 지금 프레임을 키프레임으로 두고 사이를 채운다.
함께 만드는 것 (ArtInbox는 git 밖):
  ArtInbox/sprites11/ref/{묶음}_ref.png      지금 프레임을 한 줄 8장씩 4배로 (투명 배경)
  ArtInbox/sprites11/ref/{옷}_{무기}_idle_ref.png  대기 0번 4배 (얼굴·옷·무기 기준)
  ArtInbox/sprites11/prompts/{묶음}.txt      붙여 넣을 프롬프트 (옷 문구까지 채운 것)
사용: python Tools/make_higgsfield_doc11.py
"""
import glob
import os
import re

from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PX = os.path.join(ROOT, 'Assets', '_Project', 'Resources', 'Art', 'ExplorationPixel')
OUT = os.path.join(ROOT, 'Docs', 'HIGGSFIELD_ASSETS_11_ALL.md')
# ArtInbox는 메인 체크아웃에 있다 (워크트리에서 돌려도 같은 곳)
INBOX = os.environ.get('APOGIRL_INBOX', 'I:/workshop/apogirl/apogirl/ArtInbox/sprites11')
REF = os.path.join(INBOX, 'ref')
PROMPTS = os.path.join(INBOX, 'prompts')
# 넣은 뒤에는 게임 폴더가 24프레임이라, 옛 프레임(키프레임 기준)은 넣기 도구의 백업에서 읽는다.
OLD = os.environ.get('APOGIRL_S11_BACKUP', 'I:/workshop/code_temp/apogirl/backup_s11')
if os.path.isdir(OLD):
    PX = OLD

CELLS = 24
DONE = {'player_summer_bat_attack'}           # 샘플로 이미 넣음
SKIP_ACTIONS = {'open'}                       # 문 열기는 쓰지 않는다 (사용자 결정 2026-11-23)

OUTFITS = ['summer', 'winter', 'padding', 'guard', 'riot', 'uniform']   # 받는 차례
OUTFIT_KO = {'summer': '여름 교복', 'winter': '겨울 교복', 'padding': '패딩 점퍼', 'guard': '보호대·조끼',
             'riot': '진압복', 'uniform': '기본 교복(예비)'}
OUTFIT_EN = {
    'summer': 'a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers',
    'winter': 'a 16-year-old Korean schoolgirl with shoulder-length straight black hair in the winter uniform: a long brown duffle coat over the navy uniform, dark tights and dark sneakers',
    'uniform': 'a 16-year-old Korean schoolgirl with shoulder-length straight black hair in a navy school blazer, knee-length navy skirt, dark tights and sneakers',
    'padding': 'a 16-year-old Korean schoolgirl with shoulder-length straight black hair in a puffy olive-green padded jacket over her uniform, dark trousers and sneakers',
    'guard': 'a 16-year-old Korean schoolgirl with shoulder-length straight black hair in a red and black sports protective vest with elbow and knee guards over a dark tracksuit',
    'riot': 'a 16-year-old Korean schoolgirl with shoulder-length straight black hair in an oversized black riot-police style padded suit, dark gloves, no helmet, her own black hair visible',
}

WEAPONS = ['none', 'bat', 'pipe', 'baton', 'mop']
WEAPON_KO = {'none': '맨손', 'bat': '야구방망이', 'pipe': '쇠파이프', 'baton': '경찰봉', 'mop': '대걸레'}
WEAPON_EN = {'bat': 'a wooden baseball bat', 'pipe': 'a rusty steel pipe', 'baton': 'a black police baton',
             'mop': 'a wooden mop with a grey-white cotton mop head'}

ACTIONS = ['walk', 'idle', 'run', 'attack', 'crouch', 'exhausted', 'hurt', 'shove', 'search', 'hide', 'doze']
ACTION_KO = {'walk': '걷기', 'idle': '대기', 'run': '달리기', 'attack': '무기 공격', 'crouch': '웅크려 걷기',
             'exhausted': '지침', 'hurt': '다침', 'shove': '밀치기', 'search': '수색', 'hide': '숨기', 'doze': '잠듦'}
LOOPS = {'walk', 'idle', 'run', 'crouch', 'exhausted', 'search', 'doze'}

# 20장짜리 한 번 동작: 키프레임으로 쓸 지금 프레임 (0부터). 대걸레는 찌르는 끝(9)이 13번 칸에 오게.
KEYS20 = {'attack': [0, 2, 4, 6, 9, 12, 15, 19], 'shove': [0, 3, 5, 7, 9, 12, 15, 19]}


def bundles():
    found = {}
    for f in glob.glob(os.path.join(PX, 'player_*_*_*_*.png')):
        m = re.match(r'(player_([a-z]+)_([a-z]+)_([a-z]+))_(\d+)\.png$', os.path.basename(f))
        if m:
            key = m.group(1)
            found.setdefault(key, (m.group(2), m.group(3), m.group(4), []))[3].append(int(m.group(5)))
    out = []
    for key, (outfit, weapon, action, idx) in found.items():
        if outfit in OUTFITS and weapon in WEAPONS and action in ACTIONS and key not in DONE:
            out.append({'key': key, 'outfit': outfit, 'weapon': weapon, 'action': action, 'count': max(idx) + 1})
    order = lambda b: (OUTFITS.index(b['outfit']), ACTIONS.index(b['action']), WEAPONS.index(b['weapon']))
    return sorted(out, key=order)


# ---------- 키프레임 배치 ----------

def placement(b):
    """(키프레임 문장, 표 줄 [(시트 칸, 지금 프레임)]) — 칸·프레임 번호는 1부터."""
    n, act = b['count'], b['action']
    if n == 12:
        pairs = [(1 + 2 * k, k + 1) for k in range(12)]
        text = ('The reference strip has 12 frames. Keep reference frames 1-12 unchanged as keyframes in cells 1, 3, 5, ..., 23 '
                '(reference frame k goes to cell 2k-1), and draw one in-between frame in each even cell; '
                'cell 24 is the in-between from reference frame 12 back to reference frame 1.')
    elif n == 16:
        pairs = [(1 + 3 * k, 2 * k + 1) for k in range(8)]
        text = ('The reference strip has 16 frames. Keep the odd reference frames 1, 3, 5, ..., 15 unchanged as keyframes in cells '
                '1, 4, 7, 10, 13, 16, 19 and 22, and draw two in-between frames between each pair of keyframes; the even reference '
                'frames show the pose halfway between, use them as a guide. Cells 23-24 are the in-betweens from reference frame 15 '
                'back to reference frame 1 (reference frame 16 is the guide).')
    elif n == 8:
        pairs = [(1 + 3 * k, k + 1) for k in range(8)]
        text = ('The reference strip has 8 frames. Keep reference frames 1-8 unchanged as keyframes in cells 1, 4, 7, 10, 13, 16, 19 '
                'and 22, and draw two in-between frames between each pair of keyframes.')
    else:   # 20
        keys = KEYS20[act]
        pairs = [(1 + 3 * k, f + 1) for k, f in enumerate(keys)]
        cells = ', '.join(str(c) for c, _ in pairs)
        refs = ', '.join(str(f) for _, f in pairs)
        text = (f'The reference strip has 20 frames. Keep reference frames {refs} unchanged as keyframes in cells {cells}, '
                'and draw two in-between frames between each pair of keyframes; the other reference frames show the poses in '
                'between, use them as a guide.')
    return text, pairs


# ---------- 동작 문장 ----------

def hold(w):
    return 'empty-handed' if w == 'none' else f'holding {WEAPON_EN[w]} exactly the way she holds it in the reference frames'


def motion(b):
    a, w = b['action'], b['weapon']
    loop = ' Seamless loop: cell 24 flows smoothly back into cell 1.'
    if a == 'walk':
        return f'Animation: walking forward at a steady pace, {hold(w)}, one full stride cycle (left step, then right step), natural leg movement and a small up-down body bob.' + loop
    if a == 'run':
        return f'Animation: running forward, leaning slightly forward, {hold(w)}, one full stride cycle (two steps), both feet briefly off the floor at the top of each stride but the lowest foot always comes back to the same floor baseline.' + loop
    if a == 'idle':
        return f'Animation: standing idle and breathing slowly, {hold(w)}; only a gentle shoulder rise and fall and a tiny weight shift, the feet never move.' + loop
    if a == 'crouch':
        return f'Animation: sneaking forward in a low crouch, knees deeply bent, back low, {hold(w)}, careful small steps with clearly visible leg movement. Cell 1 must also look natural as a still crouching pose, the game shows it alone when she stops.' + loop
    if a == 'exhausted':
        return f'Animation: standing exhausted, bent forward with hands near her knees, {hold(w)}, catching her breath: the back and shoulders heave up and down slowly, the feet never move.' + loop
    if a == 'search':
        return 'Animation: kneeling low and searching through something on the floor in front of her, empty-handed, both hands reaching forward and rummaging, the head following the hands, the knees stay in place.' + loop
    if a == 'doze':
        return 'Animation: sitting on the floor hugging her knees, hiding, dozing off: the head slowly nods down and jerks back up a little, the body stays curled up and does not slide.' + loop
    if a == 'hide':
        return ('Animation: from standing, she quickly crouches down and curls into a small ball hugging her knees to hide, empty-handed. '
                'Cells 22-24 are the final hidden pose and must be completely still and identical in silhouette, the game holds the last cell while she hides.')
    if a == 'hurt':
        return (f'Animation: she gets hit and flinches, {hold(w)}: the head and shoulders jerk back, one knee lifts, she staggers a little, '
                'then recovers back toward the standing pose by cell 24. Pain shown only by posture, no blood, no wounds.')
    if a == 'shove':
        return ('Animation: an empty-handed two-handed shove: she steps forward and pushes both palms out at chest height, '
                'arms fully extended in cell 13 (the moment of the push), then pulls back and returns to standing by cell 24.')
    if a == 'attack':
        if w == 'mop':
            return ('Animation: a two-handed spear-like thrust with the mop: she pulls the mop back, lunges forward with a big step and '
                    'thrusts the mop head straight forward at knee-to-waist height, fully extended in cell 13 (the hit), then draws the mop back '
                    'and settles into the ready pose by cell 22; cells 23-24 hold that pose with relaxed shoulders, feet do not move.')
        style = {'bat': 'one overhead two-handed bat strike',
                 'pipe': 'one heavy overhead two-handed strike with the pipe, a slower and heavier wind-up than a bat',
                 'baton': 'one short and fast overhead strike with the baton'}[w]
        return (f'Animation: {style}. Cells 1-7: she lifts the weapon higher above and behind her head, weight on the back foot, a short wind-up pause; '
                'cells 7-13: a fast downward arc, the weapon moves a lot between frames, ending in cell 13 at the impact pose - a big forward step, '
                'body thrown forward (the body moves forward, never backward), weapon in front of her at waist height; cells 13-16: a tiny impact hold; '
                'cells 16-22: follow-through into the low finishing pose at her waist; cells 23-24: she holds the finishing pose and her shoulders relax, feet do not move.')
    raise ValueError(a)


def rigid(w):
    if w == 'none':
        return 'Her hands are empty in every frame.'
    s = f'The {WEAPON_EN[w].split(" ", 2)[-1] if w != "mop" else "mop"} keeps the exact same length and shape in every frame; it never stretches, grows or bends.'
    if w == 'mop':
        s += ' The mop head stays a compact bundle of cotton strands hanging down, never fanned out, never pointing up.'
    return s


def prompt(b):
    text, _ = placement(b)
    return '\n'.join([
        'Sprite sheet for a side-scrolling 2D survival game, side-view 2D pixel art, crisp chunky pixels, limited muted palette, 1-pixel dark outline, no anti-aliasing, no blur, orthographic side view.',
        'Grid of 24 equal cells: 6 columns x 4 rows, read left to right then top to bottom, no gaps, no borders, no numbers, no text. Each cell is one animation frame of the same character facing right, feet on the same floor baseline near the bottom of every cell, body centered horizontally. Transparent background.',
        '',
        f'Character: {OUTFIT_EN[b["outfit"]]}{"" if b["weapon"] == "none" else ", " + hold(b["weapon"])}. Match the attached reference frames exactly: same face, same hair, same outfit, same colors, same shading style, same proportions, same pixel scale{"" if b["weapon"] == "none" else ", same weapon"}. Do not draw a backpack.',
        '',
        motion(b),
        text,
        '',
        'Exactly one character in every frame, never a second person or a ghost copy. ' + rigid(b['weapon']) + ' No motion-blur smears, no speed lines, no smoke, no sparkles, no floating lines, no stray pixels, no detached blobs.',
        'No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.',
    ])


# ---------- 참고 이미지 ----------

def frame(key, i):
    return Image.open(os.path.join(PX, f'{key}_{i}.png')).convert('RGBA')


def make_refs(bs):
    os.makedirs(REF, exist_ok=True)
    os.makedirs(PROMPTS, exist_ok=True)
    k = 4
    for b in bs:
        n = b['count']
        cols = min(8, n)
        rows = (n + cols - 1) // cols
        w, h = frame(b['key'], 0).size
        sheet = Image.new('RGBA', (w * k * cols, h * k * rows), (0, 0, 0, 0))
        for i in range(n):
            sheet.alpha_composite(frame(b['key'], i).resize((w * k, h * k), Image.NEAREST), ((i % cols) * w * k, (i // cols) * h * k))
        sheet.save(os.path.join(REF, b['key'] + '_ref.png'))
        with open(os.path.join(PROMPTS, b['key'] + '.txt'), 'w', encoding='utf-8') as f:
            f.write(prompt(b) + '\n')
    for o in OUTFITS:
        for wp in WEAPONS:
            key = f'player_{o}_{wp}_idle'
            if os.path.exists(os.path.join(PX, key + '_0.png')):
                a = frame(key, 0)
                a.resize((a.width * 4, a.height * 4), Image.NEAREST).save(os.path.join(REF, f'player_{o}_{wp}_idle0_ref.png'))


def refs_for(b):
    r = [f'`ref/{b["key"]}_ref.png`']
    idle = f'player_{b["outfit"]}_{b["weapon"]}_idle'
    if b['action'] != 'idle':
        r.append(f'`ref/{idle}0_ref.png`')
    if b['outfit'] != 'summer':
        r.append(f'받은 `player_summer_{b["weapon"]}_{b["action"]}_sheet.png`')
    return ', '.join(r)


# ---------- 문서 ----------

def doc(bs):
    L = []
    add = L.append
    add('# 힉스필드 에셋 11차 (전체): 탐험 주인공 스프라이트 24프레임 (2026-11-23)')
    add('')
    add('> 이 문서는 `python Tools/make_higgsfield_doc11.py`로 만든다. 고칠 때는 스크립트를 고치고 다시 돌린다.')
    add('')
    add('샘플(여름 교복 야구방망이 공격, [HIGGSFIELD_ASSETS_11.md](HIGGSFIELD_ASSETS_11.md))이 잘 맞아서, **탐험 주인공 그림 전부**를 같은 방식으로 다시 받는다. '
        '지금 프레임을 키프레임으로 그대로 두고 그 사이를 채워 **모든 묶음을 24프레임(6 × 4칸 시트 한 장)** 으로 만든다.')
    add('')
    add('- 작업 제출은 사용자가 한다. 나(Claude)는 문서·참고 이미지·프롬프트 파일만 만든다.')
    add('- 빠진 것: 문 열기 `open`(쓰지 않음, 사용자 결정 2026-11-23), 이미 넣은 `player_summer_bat_attack`.')
    add(f'- 모두 **{len(bs)}묶음**. 묶음마다 시트 한 장이다.')
    add('')
    add('## 0. 연결 결과 (2026-11-24)')
    add('')
    add('- 받은 시트 227장(2688 × 1520, 6 × 4칸, 투명 배경)을 모두 24프레임으로 넣었다. 도구 `code_temp/apogirl/tools/s11_install_all.py` (preview | install), 옛 그림·가방 자리 표는 `code_temp/apogirl/backup_s11/`.')
    add('- 칸 나누기: 대걸레 끝·발이 칸 경계를 넘은 시트가 있어, 시트 전체를 덩어리로 나눠 무게중심이 든 칸에 준다.')
    add('- 크기는 키프레임 짝의 면적 비(묶음마다 ×0.17~0.20). 세로는 칸마다 몸 맨 아래를 옛 프레임 바닥 줄에.')
    add('- 가로: 생성 그림은 칸마다 좌우로 4px 안팎 흔들려서, **도는 동작은 머리를 옛 자리에** 맞추고, 한 번 동작은 몸통을 옛 몸통 길에(방망이·파이프·경찰봉 공격은 샘플의 앞으로 내미는 곡선) 맞춘다.')
    add('- 가방 자리 `player_anchors.json`은 옛 값을 새 칸 위치로 보간, X는 옮긴 만큼.')
    add('- 게임: 도는 동작은 프레임 수에 맞춰 초당 프레임을 늘려 한 바퀴 시간이 예전과 같다(`ExpeditionDirector.LoopFrame` `designedFrames` 16·12). 공격은 모두 24장이라 고르게 돌고, 대걸레 맞는 순간 `At`은 0.45 → 0.5(13번 칸).')
    add('- 기본 교복·보호대의 옛 지침 그림은 똑바로 서 있었는데, 새 그림은 다른 옷처럼 허리를 숙여 숨을 몰아쉰다.')
    add('- 되돌림(사용자 요청 2026-11-24): 새 걷기·달리기·웅크려 걷기는 발 디딤 박자가 고르지 않아 다리가 안 맞고(웅크려 걸을 때 가방도 낮게 흔들림, 방망이 들고 달릴 때 방망이 자리가 섞임), 수색은 옛 그림이 낫다 — 이 네 동작 96묶음은 옛 PixelLab 그림(16·12장)과 옛 가방 자리로 되돌렸다(새 그림은 `code_temp/apogirl/s11_reverted_new/`). 남은 새 동작(대기·지침·다침·공격·밀치기·숨기·잠듦)의 가방 자리는 그림의 등 선·머리 위에 맞춰 다시 셌다(옛 그림에서 잰 간격의 가운데 값, 이웃 칸과 고르게, 각도는 묶음마다 하나) — 도구 `code_temp/apogirl/tools/s11_bag.py`.')
    add('')
    add('## 1. 동작별 정리')
    add('')
    add('| 동작 | 무기 | 지금 | 새로 | 게임에서 도는 법 | 키프레임 배치 |')
    add('|---|---|---|---|---|---|')
    rows = {}
    for b in bs:
        rows.setdefault((b['action'], b['count']), set()).add(b['weapon'])
    play = {'walk': '걸음 속도에 맞춰 돈다', 'run': '돈다', 'idle': '돈다', 'crouch': '돈다(멈추면 1번 칸)', 'exhausted': '멈춰 지쳤을 때 돈다',
            'search': '수색하는 동안 돈다', 'doze': '숨다 잠들면 돈다', 'hide': 'Space를 누르는 만큼 진행, 숨으면 마지막 칸', 'hurt': '0.4초 한 번',
            'shove': '0.5초 한 번', 'attack': '0.5초 한 번, 맞는 순간 = 13번 칸'}
    lay = {8: '1·4·7…22번 칸 (사이 2장씩)', 12: '홀수 칸 1·3…23 (사이 1장씩)', 16: '홀수 프레임 1·3…15 → 1·4…22번 칸 (짝수 프레임은 사이 그림 안내)', 20: '8장을 골라 1·4…22번 칸'}
    for a in ACTIONS:
        for (act, n), ws in sorted(rows.items(), key=lambda x: x[0][1]):
            if act == a:
                wk = '·'.join(WEAPON_KO[w] for w in WEAPONS if w in ws)
                add(f'| {ACTION_KO[a]} `{a}` | {wk} | {n} | 24 | {play[a]} | {lay[n]} |')
    add('')
    add('- 도는 동작(걷기·대기·달리기·웅크려 걷기·지침·수색·잠듦)은 24칸째가 1번 칸으로 자연스럽게 이어져야 한다.')
    add('- 20장짜리 한 번 동작의 키프레임(지금 프레임, 1부터): 대걸레 공격 `1, 3, 5, 7, 10, 13, 16, 20` (찌르는 끝 10번이 13번 칸), 밀치기 `1, 4, 6, 8, 10, 13, 16, 20`.')
    add('- 도는 동작은 프레임이 16·12장 → 24장으로 늘어도 **한 바퀴 시간이 같게** 내가 게임의 초당 프레임을 맞춘다(걸음이 느려 보이지 않게).')
    add('')
    add('## 2. 받는 차례')
    add('')
    add('1. **여름 교복** (방어구가 없는 여름의 기본 차림, 가장 오래 보인다) — 걷기·대기·달리기·공격부터.')
    add('2. **겨울 교복** — 여름 결과 시트를 참고로 넣고 옷만 바꾼다.')
    add('3. **패딩 점퍼·보호대·진압복** — 방어구 차림.')
    add('4. **기본 교복** — 계절 없는 예비 차림이라 마지막.')
    add('')
    add('옷마다 차례: 걷기 → 대기 → 달리기 → 공격 → 웅크려 걷기 → 지침 → 다침 → 밀치기 → 수색 → 숨기 → 잠듦. '
        '여섯 벌의 동작이 같아야 무기·가방 자리가 맞으므로, **두 번째 옷부터는 같은 묶음의 여름 결과 시트를 꼭 참고 이미지로 넣는다.**')
    add('')
    add('## 3. 참고 이미지와 프롬프트 파일')
    add('')
    add('스크립트가 만들어 둔다 (`ArtInbox/sprites11/`, git 밖):')
    add('')
    add('| 파일 | 내용 |')
    add('|---|---|')
    add('| `ref/{묶음}_ref.png` | 지금 프레임을 한 줄 8장씩 4배로 이어 붙인 것 (투명 배경). **키프레임·그림체의 기준** |')
    add('| `ref/player_{옷}_{무기}_idle0_ref.png` | 그 차림의 대기 0번 4배. 얼굴·옷·무기 모양의 기준 |')
    add('| `prompts/{묶음}.txt` | 그 묶음의 프롬프트 전체 (옷 문구까지 채움). **그대로 복사해 붙여 넣는다** |')
    add('')
    add('넣는 순서: ① `{묶음}_ref.png` ② 대기 기준 ③ (여름이 아니면) 받은 여름 시트. 서진 기준 시트는 그림체가 흔들려서 넣지 않는다.')
    add('')
    add('## 4. 규격 (샘플과 같음)')
    add('')
    add('| 항목 | 값 |')
    add('|---|---|')
    add('| 모델 | `gpt_image_2_6` (시트 한 장 생성) |')
    add('| 시트 | 가로 6칸 × 세로 4줄 = 24칸, 칸 사이 여백·테두리·번호 없음, **비율 16:9** (샘플은 2688 × 1520으로 왔다) |')
    add('| 방향·발 | 오른쪽을 보는 옆모습, 모든 칸에서 발바닥 높이가 같다 |')
    add('| 배경 | 투명. 안 되면 마젠타 `#ff00ff` 단색 |')
    add('| 가방 | 그리지 않는다 (게임이 따로 얹는다) |')
    add('| 저장 | `ArtInbox/sprites11/{묶음}_sheet.png` (예: `player_summer_none_walk_sheet.png`) |')
    add('')
    add('**잘 안 될 때** (샘플 문서와 같음)')
    add('- 칸 수가 틀리거나 순서가 섞이면: 첫 줄 `Grid of 24 equal cells`를 `Exactly 24 frames in a 6 x 4 grid`로 바꿔 다시.')
    add('- 그림체가 지금과 달라지면(매끈한 일러스트처럼): 참고 이미지를 ①만 넣고 다시.')
    add('- 두 사람이 겹치거나 무기가 늘어나면 그 칸만 알려 줘도 된다. 내가 이웃 칸으로 메운다.')
    add('')
    add('## 5. 묶음 목록')
    add('')
    for o in OUTFITS:
        ob = [b for b in bs if b['outfit'] == o]
        add(f'### 5.{OUTFITS.index(o) + 1} {OUTFIT_KO[o]} `{o}` ({len(ob)}묶음)')
        add('')
        add('| # | 묶음 | 동작 | 무기 | 지금 → 새로 | 참고 이미지 |')
        add('|---|---|---|---|---|---|')
        for i, b in enumerate(ob, 1):
            add(f'| {i} | `{b["key"]}` | {ACTION_KO[b["action"]]} | {WEAPON_KO[b["weapon"]]} | {b["count"]} → 24 | {refs_for(b)} |')
        add('')
    add('## 6. 프롬프트 (여름 교복 전부)')
    add('')
    add('다른 옷은 `prompts/{묶음}.txt`에 같은 글이 `Character:` 줄만 바뀌어 들어 있다. 옷 문구는 아래 6.0 표.')
    add('')
    add('### 6.0 옷 문구 (`Character:` 줄 앞부분)')
    add('')
    add('| 옷 | 문구 |')
    add('|---|---|')
    for o in OUTFITS:
        add(f'| `{o}` | `{OUTFIT_EN[o]}` |')
    add('')
    for i, b in enumerate([b for b in bs if b['outfit'] == 'summer'], 1):
        add(f'### 6.{i} `{b["key"]}` — {ACTION_KO[b["action"]]} · {WEAPON_KO[b["weapon"]]} ({b["count"]} → 24)')
        add('')
        add('```')
        add(prompt(b))
        add('```')
        add('')
    add('## 7. 받은 뒤 (내가 할 일)')
    add('')
    add('1. 샘플 도구 `code_temp/apogirl/tools/s11_install.py`를 묶음 이름·키프레임 칸을 받도록 넓혀 넣는다: 키프레임 짝의 면적 비로 크기, 몸통 위치로 가로, 칸마다 몸 맨 아래를 지금 0번 프레임의 바닥 줄에. 옛 프레임은 `code_temp/apogirl/backup_s11/`로 옮겨 둔다.')
    add('2. 공격·밀치기는 샘플처럼 **몸통 목표 곡선**으로 몸이 뒤로 밀리지 않게 맞춘다. 도는 동작은 몸통이 처음 자리에서 떠다니지 않게 고르게 맞춘다.')
    add('3. 가방 자리 `player_anchors.json`: 옛 값을 키프레임 칸에 두고 사이를 고르게 나눈다(샘플과 같음). 맨손 동작(수색·숨기·잠듦)도 같다.')
    add('4. 도는 동작은 프레임 수에 맞춰 초당 프레임을 늘려 한 바퀴 시간을 지금과 같게 한다(`ExpeditionDirector.LoopFrame`).')
    add('5. 공격 차례 표(`Combat.ImpactAt[].Frames`)는 옛 8장짜리에만 쓰이고, 24장 묶음은 고르게 돈다 — 맞는 순간 0.5 = 13번 칸. 대걸레는 `At`을 0.5로 옮긴다(받은 묶음부터).')
    add('6. 게임에서 옷·무기마다 걷기·달리기·공격을 보고 끊김·튀는 칸·발 미끄러짐을 확인한다.')
    add('')
    return '\n'.join(L)


if __name__ == '__main__':
    bs = bundles()
    make_refs(bs)
    with open(OUT, 'w', encoding='utf-8', newline='\n') as f:
        f.write(doc(bs))
    print('bundles', len(bs), '->', OUT)
