"""힉스필드 에셋 16차 문서(Docs/HIGGSFIELD_ASSETS_16.md) 만들기: PixelLab에 보내려던 탐험 주인공 그림을 힉스필드로.
PIXELLAB_24F_PLAN.md 9장(나머지 5벌, 무기 겹치기 방식 C5 ②)과 PIXELLAB_CRAFTING_REQUEST.md 4절(몽둥이 3종)을 옮긴다.
방식: PixelLab으로 다시 받은 하복 그림(2배, 192 x 156)을 시트로 이어 붙여 '입력 그림'으로 주고, 옷만 바꾸거나 무기만 지우게 한다.
  → 자세·박자·크기·밝기는 하복 그대로라 옷 5벌이 똑같이 움직인다.
함께 만드는 것 (ArtInbox는 git 밖):
  ArtInbox/sprites16/input/{묶음}_input.png   하복 프레임을 이어 붙인 입력 시트 (2배, 투명 배경)
  ArtInbox/sprites16/ref/outfit_{옷}.png       그 옷의 지금 대기 0번 4배 (옷 모양·색 기준)
  ArtInbox/sprites16/ref/weapon_src_{무기}.png  하복 그 무기 대기 0번 2배 (무기 모양 기준)
  ArtInbox/sprites16/prompts/{묶음}.txt        붙여 넣을 프롬프트
사용: python Tools/make_higgsfield_doc16.py
"""
import os

from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PX = os.path.join(ROOT, 'Assets', '_Project', 'Resources', 'Art', 'ExplorationPixel')
OUT = os.path.join(ROOT, 'Docs', 'HIGGSFIELD_ASSETS_16.md')
# ArtInbox는 메인 체크아웃에 있다 (워크트리에서 돌려도 같은 곳)
INBOX = os.environ.get('APOGIRL_INBOX', 'I:/workshop/apogirl/apogirl/ArtInbox/sprites16')
INPUT = os.path.join(INBOX, 'input')
REF = os.path.join(INBOX, 'ref')
PROMPTS = os.path.join(INBOX, 'prompts')

OUTFITS = ['winter', 'padding', 'guard', 'riot', 'uniform']   # 받는 차례 (11차와 같음, 하복은 끝남)
OUTFIT_KO = {'winter': '겨울 교복', 'padding': '패딩 점퍼', 'guard': '보호대·조끼', 'riot': '진압복', 'uniform': '기본 교복(예비)'}
OUTFIT_EN = {
    'winter': 'the winter uniform: a long brown duffle coat over the navy uniform, dark tights and dark sneakers',
    'uniform': 'a navy school blazer, knee-length navy pleated skirt, dark tights and sneakers',
    'padding': 'a puffy olive-green padded jacket over her uniform, dark trousers and sneakers',
    'guard': 'a red and black sports protective vest with elbow and knee guards over a dark tracksuit',
    'riot': 'an oversized black riot-police style padded suit, dark gloves, no helmet, her own black hair visible',
}

ACT_KO = {'idle': '대기', 'walk': '걷기', 'run': '달리기', 'crouch': '웅크려 걷기', 'search': '수색', 'hurt': '다침',
          'exhausted': '지침', 'shove': '밀치기', 'hide': '숨기', 'doze': '잠듦', 'attack': '공격'}
BARE = ['idle', 'walk', 'run', 'crouch', 'hurt', 'exhausted', 'shove', 'search', 'hide', 'doze']
MOVE = ['idle', 'walk', 'run', 'crouch', 'hurt', 'exhausted']
# 쥔 자세: 무기를 지운 몸. grip1 = 한 손에 짧은 무기(방망이·파이프·경찰봉·몽둥이 3종), grip2 = 긴 막대(대걸레)
GRIPS = {'grip1': ('bat', 'baseball bat', 'one hand'), 'grip2': ('mop', 'mop', 'her hands')}
GRIP_KO = {'grip1': '한 손 쥠 (방망이·파이프·경찰봉·몽둥이)', 'grip2': '긴 막대 쥠 (대걸레)'}
# 공격 태그: 무기를 지운 휘두르기·내려치기(무기는 겹치기로), 찌르기는 대걸레뿐이라 대걸레째 그린다
ATTACKS = {'swing': ('bat', 'baseball bat'), 'smash': ('pipe', 'steel pipe')}
ATTACK_KO = {'swing': '휘두르기 (방망이·경찰봉·몽둥이 3종)', 'smash': '내려치기 (쇠파이프)', 'thrust': '찌르기 (대걸레, 무기째)'}

WEAPONS = [
    ('bat', '야구방망이', 'bat', 'a wooden baseball bat, warm brown wood, rounded knob at the grip end'),
    ('pipe', '쇠파이프', 'pipe', 'a straight grey steel pipe with a darker cut end'),
    ('baton', '경찰봉', 'baton', 'a short straight black stick with a slightly thicker grip'),
    ('mop', '대걸레', 'mop', 'a long wooden mop handle with a compact bundle of off-white cotton strands at the end'),
    ('club', '나무 몽둥이', 'bat', 'a rough wooden club carved from a school desk leg, thick at the end, pale raw wood with splinters'),
    ('club_taped', '테이프 감은 몽둥이', 'bat', 'the same rough wooden club with the grip and the striking end wrapped tightly in grey duct tape'),
    ('club_rebar', '철근 덧댄 몽둥이', 'bat', 'a heavy wooden club reinforced with two short pieces of rusty rebar bound along the striking end with wire'),
]

# 소리 다시 받기 (사용자 요청 2026-11-25): 잠든 좀비가 코 고는 것처럼 들려 → 낮게 그르렁거리는 소리로
SOUNDS = [
    ('sfx_z_dormant_crowd', '잠든 좀비 (가까이 있을 때 도는 환경음)', '6초, 이음매 없이 반복, 모노',
     ' '.join([
         'Sound effect for a 2D zombie survival game: several infected people asleep on their feet in a dark school corridor, heard from a few meters away.',
         'A low, slow, guttural growl in the throat, deep and rumbling like a large animal breathing in its sleep: long wet rattling exhales,',
         'a faint gravelly purr under each breath, the growls overlapping and rising and falling unevenly.',
         'Not snoring: no nasal snore, no whistling through the nose, no snorting, no human sleep sounds, no comedy, no words, no screams, no music.',
         'Very quiet and tense, close to silence between breaths, dark low frequencies, slight room reverb.',
         'About 6 seconds, mono, seamlessly loopable: the end must flow back into the start with no click and no gap.',
     ]),
     'Low, quiet zombie growling while asleep: deep guttural throat rumble, slow menacing breaths with a rattling growl on each exhale, '
     'several sleepers overlapping, dark corridor reverb, never snoring, no nasal sounds, no voices, no music, 6 seconds, seamless loop.'),
]

SAFETY = 'No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest clothes, no text, no logo, no watermark.'
KEEP = ('Keep everything else exactly the same as image 1: the grid, every cell position, every pose and limb position, the feet on the same '
        'floor line, the body size, the face and the short black bob hair, the pixel scale, the crisp 1-pixel dark outline, no anti-aliasing, '
        'the brightness, the flat shading style and the lighting. Do not add, remove, reorder, re-pose or redraw any frame. No backpack, no bag, no pouch.')
BG = 'Transparent background (if transparency is impossible, a flat magenta #ff00ff background). No gaps, no borders, no numbers, no text.'


def frames(key):
    n = 0
    while os.path.exists(os.path.join(PX, f'{key}_{n}.png')):
        n += 1
    return n


def grid(n):
    cols = 6 if n == 24 else 4
    return cols, (n + cols - 1) // cols


def ratio(n):
    cols, rows = grid(n)
    r = (cols * 192) / (rows * 156)
    return min([('16:9', 16 / 9), ('3:2', 1.5), ('4:3', 4 / 3), ('5:4', 1.25)], key=lambda x: abs(x[1] - r))[0]


def head(n):
    cols, rows = grid(n)
    return (f'Image 1 is a sprite sheet for a side-scrolling 2D survival game: side-view 2D pixel art, {cols} columns x {rows} rows = {n} animation frames '
            'of the same Korean schoolgirl facing right, read left to right then top to bottom.')


def bundles():
    """(단계, 키, 입력 시트 출처, 프롬프트, 설명) 목록."""
    out = []
    # A. 하복 시범: 무기 그림 + 쥔 자세 + 무기 지운 공격
    for g, (w, en, hands) in GRIPS.items():
        for a in MOVE:
            src = f'player_summer_{w}_{a}'
            n = frames(src)
            p = '\n'.join([head(n),
                           f'Remove the {en} from every frame completely and redraw whatever it was hiding (legs, skirt, background stays transparent). '
                           f'Her {hands} stay exactly where they are, still closed in a loose fist as if holding a handle that is no longer there.',
                           KEEP, BG, SAFETY])
            out.append(('A', f'player_summer_{g}_{a}', src, n, p, f'하복 {GRIP_KO[g]} {ACT_KO[a]}'))
    for t, (w, en) in ATTACKS.items():
        src = f'player_summer_{w}_attack'
        n = frames(src)
        p = '\n'.join([head(n),
                       f'Remove the {en} from every frame completely and redraw whatever it was hiding. Both hands stay exactly where they are in every frame, '
                       'closed in a fist as if still swinging a handle that is no longer there. The arm and body motion stays exactly the same.',
                       KEEP, BG, SAFETY])
        out.append(('A', f'player_summer_{t}_attack', src, n, p, f'하복 {ATTACK_KO[t]} — 무기 지움'))
    # B. 옷 5벌: 하복 시트의 옷만 바꾼다
    for o in OUTFITS:
        dress = (f'Change only her clothes in every frame to {OUTFIT_EN[o]}. Image 2 shows this outfit (older, darker art): copy its design and colors, '
                 'but draw it as bright and crisp as image 1, following each pose and the fabric folds.')
        for a in BARE:
            src = f'player_summer_none_{a}'
            n = frames(src)
            out.append(('B', f'player_{o}_none_{a}', src, n,
                        '\n'.join([head(n), dress, 'Her hands are empty in every frame.', KEEP, BG, SAFETY]), f'{OUTFIT_KO[o]} 맨손 {ACT_KO[a]}'))
        for g in GRIPS:
            for a in MOVE:
                src = f'player_summer_{g}_{a}'
                n = frames(f'player_summer_{GRIPS[g][0]}_{a}')
                out.append(('C', f'player_{o}_{g}_{a}', src, n,
                            '\n'.join([head(n), dress, 'Her hands stay closed in a loose fist exactly as in image 1, holding nothing.', KEEP, BG, SAFETY]),
                            f'{OUTFIT_KO[o]} {GRIP_KO[g]} {ACT_KO[a]}'))
        for t in ATTACKS:
            src = f'player_summer_{t}_attack'
            n = frames(f'player_summer_{ATTACKS[t][0]}_attack')
            out.append(('C', f'player_{o}_{t}_attack', src, n,
                        '\n'.join([head(n), dress, 'Her hands stay closed in a fist exactly as in image 1, holding nothing.', KEEP, BG, SAFETY]),
                        f'{OUTFIT_KO[o]} {ATTACK_KO[t]} — 무기 없이'))
        src = 'player_summer_mop_attack'
        n = frames(src)
        out.append(('B', f'player_{o}_mop_attack', src, n,
                    '\n'.join([head(n), dress, 'Keep the mop exactly as it is in every frame: same length, same angle, same position in her hands.', KEEP, BG, SAFETY]),
                    f'{OUTFIT_KO[o]} {ATTACK_KO["thrust"]}'))
    return out


def weapon_prompt(en):
    return '\n'.join([
        f'A single side-view 2D pixel art game sprite of {en}, alone: no hands, no person, nothing else.',
        'Image 1 shows the character sprite this weapon belongs to: use the same pixel scale, the same crisp 1-pixel dark outline, '
        'no anti-aliasing and the same flat bright colors.',
        'The weapon lies perfectly horizontal and straight, the grip end on the left and the striking end on the right, centered, '
        'with plenty of empty space around it.',
        'Transparent background (if transparency is impossible, a flat magenta #ff00ff background). No shadow, no text, no logo, no watermark.',
    ])


def sheet(key):
    n = frames(key)
    cols, rows = grid(n)
    w, h = 192, 156
    k = 2
    s = Image.new('RGBA', (w * k * cols, h * k * rows), (0, 0, 0, 0))
    for i in range(n):
        im = Image.open(os.path.join(PX, f'{key}_{i}.png')).convert('RGBA').resize((w * k, h * k), Image.NEAREST)
        s.alpha_composite(im, ((i % cols) * w * k, (i // cols) * h * k))
    return s


def make_files(bs):
    for d in (INPUT, REF, PROMPTS):
        os.makedirs(d, exist_ok=True)
    for stage, key, src, n, p, _ in bs:
        if os.path.exists(os.path.join(PX, src + '_0.png')):
            sheet(src).save(os.path.join(INPUT, src + '_input.png'))
        with open(os.path.join(PROMPTS, key + '.txt'), 'w', encoding='utf-8') as f:
            f.write(p + '\n')
    for o in OUTFITS:
        im = Image.open(os.path.join(PX, f'player_{o}_none_idle_0.png')).convert('RGBA')
        im.resize((im.width * 4, im.height * 4), Image.NEAREST).save(os.path.join(REF, f'outfit_{o}.png'))
    for tok, _, src, en in WEAPONS:
        im = Image.open(os.path.join(PX, f'player_summer_{src}_idle_0.png')).convert('RGBA')
        im.resize((im.width * 2, im.height * 2), Image.NEAREST).save(os.path.join(REF, f'weapon_src_{tok}.png'))
        with open(os.path.join(PROMPTS, f'weapon_{tok}.txt'), 'w', encoding='utf-8') as f:
            f.write(weapon_prompt(en) + '\n')
    for key, _, _, main, alt in SOUNDS:
        with open(os.path.join(PROMPTS, f'{key}.txt'), 'w', encoding='utf-8') as f:
            f.write(main + '\n\n# 안 맞으면 짧은 판\n' + alt + '\n')


def doc(bs):
    L = []
    add = L.append
    count = {s: sum(1 for b in bs if b[0] == s) for s in 'ABC'}
    total = len(bs) + len(WEAPONS)
    add('# 힉스필드 에셋 16차: PixelLab 몫이던 탐험 주인공 그림 · 잠든 좀비 소리 (2026-11-25)')
    add('')
    add('> 이 문서는 `python Tools/make_higgsfield_doc16.py`로 만든다. 고칠 때는 스크립트를 고치고 다시 돌린다.')
    add('')
    add('PixelLab에 보내려고 아껴 둔 탐험 그림을 **힉스필드로 받는** 문서다. 크레딧 문제로 미룬 두 가지를 옮겼다.')
    add('')
    add('- [PIXELLAB_24F_PLAN.md](PIXELLAB_24F_PLAN.md) 9장: 주인공 나머지 5벌(동복·겨울·보호대·패딩·진압복)을 **무기 겹치기 방식**(결정 C5 ②)으로 다시 받기.')
    add('- [PIXELLAB_CRAFTING_REQUEST.md](PIXELLAB_CRAFTING_REQUEST.md) 4절: 몽둥이 3종(결정 E3 ② — 무기 겹치기 때 무기 그림만).')
    add('- **소리 1개 다시 받기** (사용자 요청 2026-11-25): 잠든 좀비 소리가 코 고는 것처럼 들려, 낮게 그르렁거리는 소리로 — 7절.')
    add('- 작업 제출은 사용자가 한다. 나(Claude)는 문서·입력 시트·프롬프트 파일만 만들었다.')
    add('')
    add('## 0. 방식 — 하복 그림의 옷만 바꾼다')
    add('')
    add('11차(힉스필드로 24장을 새로 그림)는 걷기·달리기 다리 박자가 고르지 않아 되돌렸다. 그래서 이번에는 **새로 그리게 하지 않는다.**')
    add('')
    add('- **입력 그림 = 하복 그림.** 하복은 PixelLab으로 다시 받아 자세·박자·밝기가 가장 좋다(2배, 192 × 156). 그 프레임을 시트로 이어 붙여 입력으로 준다.')
    add('- **바꾸는 것은 하나만.** 옷만 바꾸거나(B·C), 무기만 지운다(A). 자세·다리 박자·크기·바닥 줄은 하복 그대로라 옷 6벌이 똑같이 움직인다(24F 계획 C4의 목표).')
    add('- **무기는 따로 한 장씩 받는다.** 몸은 빈손으로 쥔 자세만 받고, 무기 그림을 손 자리에 겹친다(C5 ②). 그래서 몽둥이 3종은 무기 그림 3장으로 옷 6벌 모두에 붙는다.')
    add('- 걷기·달리기·웅크려 걷기는 하복도 16장(수색 12장)이라 그 장수 그대로 받는다. 게임은 장수에 맞춰 돈다.')
    add('')
    add('| 단계 | 무엇 | 장수 | 언제 |')
    add('|---|---|---|---|')
    add(f'| **A** 시범 | 무기 그림 {len(WEAPONS)}장 + 하복 쥔 자세·무기 지운 공격 {count["A"]}시트 | {count["A"] + len(WEAPONS)} | 먼저. 받으면 내가 겹치기 코드를 만들어 지금 하복 그림과 나란히 비교한다 |')
    add(f'| **B** 옷 5벌 — 겹치기와 무관 | 맨손 10동작 + 대걸레 찌르기 | {count["B"]} | 아무 때나 (A와 같이 해도 된다) |')
    add(f'| **C** 옷 5벌 — 겹치기용 | 쥔 자세 6동작 × 2 + 무기 지운 공격 2 | {count["C"]} | **A 시범이 괜찮다고 정한 뒤** (C의 입력은 A의 결과다) |')
    add(f'| 합계 | | **{total}** | |')
    add(f'| 소리 | 잠든 좀비 그르렁 (7절) | {len(SOUNDS)} | 아무 때나 |')
    add('')
    add('## 1. 규격')
    add('')
    add('| 항목 | 값 |')
    add('|---|---|')
    add('| 모델 | `gpt_image_2_6` (11차와 같음). 자세가 바뀌어 나오면 Nano Banana Pro로 다시 |')
    add('| 넣는 그림 | ① `input/{입력}_input.png` (입력 시트) ② (옷 바꾸기만) `ref/outfit_{옷}.png` |')
    add('| 비율 | 표의 비율 (24장 6 × 4 = 16:9, 16장 4 × 4 = 5:4, 12장 4 × 3 = 3:2) — 입력 시트와 같은 칸 배치로 |')
    add('| 배경 | 투명. 안 되면 마젠타 `#ff00ff` 단색 |')
    add('| 가방 | 그리지 않는다 (게임이 따로 얹는다) |')
    add('| 저장 | `ArtInbox/sprites16/{묶음}_sheet.png`, 무기는 `ArtInbox/sprites16/weapons/weapon_{토큰}.png` |')
    add('')
    add('- 파일은 스크립트가 만들어 두었다 (`ArtInbox/sprites16/`, git 밖): 입력 시트 `input/`, 참고 `ref/`, 프롬프트 `prompts/{묶음}.txt`(그대로 붙여 넣는다).')
    add('- C 단계 입력은 A에서 받은 시트다: `ArtInbox/sprites16/player_summer_grip1_walk_sheet.png` 같은 파일을 입력 ①로 넣는다.')
    add('- 옷 참고 그림 `ref/outfit_*`은 지금 게임 그림(11차, 1배)이라 어둡다. 프롬프트가 "모양·색만 따르고 밝기·선명함은 입력처럼"이라고 적었다.')
    add('')
    add('**잘 안 될 때**')
    add('- 칸 수가 틀리거나 자세가 바뀌면: 같은 프롬프트로 한 번 더, 그래도 안 되면 모델을 Nano Banana Pro로.')
    add('- 몇 칸만 어긋나면(두 사람 겹침·무기가 남음·칸 밖으로 밀림) 그 칸 번호만 알려 줘도 된다. 내가 이웃 칸이나 하복 칸으로 메운다.')
    add('- 무기 지우기(A)에서 손이 펴지면 다시 받는다. 겹친 무기가 손에 쥐어 보여야 한다.')
    add('')
    add('## 2. A. 시범 (하복)')
    add('')
    add('### 2.1 무기 그림 (모든 옷 공통)')
    add('')
    add('입력 ① = `ref/weapon_src_{토큰}.png`(하복이 그 무기를 든 대기 그림 2배, 몽둥이는 방망이 그림). 비율 16:9. 크기는 내가 하복 그림의 무기 길이에 맞춰 줄인다.')
    add('')
    add('| # | 토큰 | 무기 | 입력 ① | 프롬프트 |')
    add('|---|---|---|---|---|')
    for i, (tok, ko, src, en) in enumerate(WEAPONS, 1):
        add(f'| W{i} | `{tok}` | {ko} | `ref/weapon_src_{tok}.png` | `prompts/weapon_{tok}.txt` |')
    add('')
    add('- 무기는 수평으로 눕히고 손잡이가 왼쪽, 치는 끝이 오른쪽이다. 손잡이 자리(쥐는 점)는 내가 잰다.')
    add('- 몽둥이 3종의 공격은 모두 휘두르기다(철근 몽둥이도 파이프 내려치기가 아니라 휘두르기, E3 ②).')
    add('')
    add('### 2.2 쥔 자세·무기 지운 공격')
    add('')
    add('| # | 묶음 | 무엇 | 입력 ① | 장수 | 비율 |')
    add('|---|---|---|---|---|---|')
    for i, b in enumerate([b for b in bs if b[0] == 'A'], 1):
        add(f'| A{i} | `{b[1]}` | {b[5]} | `input/{b[2]}_input.png` | {b[3]} | {ratio(b[3])} |')
    add('')
    add('받은 뒤 내가 하는 것: 무기 그림에서 손잡이 자리를 재고, 하복 무기 그림(지금 게임 그림)에서 프레임마다 손 위치·무기 각도·앞뒤를 자동으로 재어 겹치기 표를 만든다. '
        '쥔 자세 + 무기 그림을 지금 하복 그림과 탐험 화면에서 나란히 보여 드린다. 괜찮으면 C 단계로 간다.')
    add('')
    add('## 3. 옷 5벌')
    add('')
    add('입력 ① = 하복 시트(B) 또는 A에서 받은 시트(C), 입력 ② = `ref/outfit_{옷}.png`. 옷 문구는 프롬프트 파일에 채워 두었다.')
    add('')
    add('| 옷 | 문구 |')
    add('|---|---|')
    for o in OUTFITS:
        add(f'| `{o}` {OUTFIT_KO[o]} | `{OUTFIT_EN[o]}` |')
    add('')
    for j, o in enumerate(OUTFITS, 1):
        ob = [b for b in bs if b[0] in 'BC' and b[1].startswith(f'player_{o}_')]
        add(f'### 3.{j} {OUTFIT_KO[o]} `{o}` ({len(ob)}시트)')
        add('')
        add('| # | 단계 | 묶음 | 무엇 | 입력 ① | 장수 | 비율 |')
        add('|---|---|---|---|---|---|---|')
        for i, b in enumerate(ob, 1):
            src = f'input/{b[2]}_input.png' if b[0] == 'B' else f'A의 `{b[2]}_sheet.png`'
            src = f'`{src}`' if b[0] == 'B' else src
            add(f'| {i} | {b[0]} | `{b[1]}` | {b[5]} | {src} | {b[3]} | {ratio(b[3])} |')
        add('')
    add('## 4. 받는 차례')
    add('')
    add('1. **A 시범** — 무기 7장 → 하복 쥔 자세 12시트 → 무기 지운 공격 2시트. 다 받으면 알려 준다. 나는 겹치기 코드와 비교판을 만든다.')
    add('2. **B** — 옷마다 맨손 10동작 + 대걸레 찌르기. A와 상관없으니 먼저 해도 된다. 차례는 겨울 → 패딩 → 보호대 → 진압복 → 동복.')
    add('3. **C** — A 시범을 "괜찮다"고 정한 뒤, 옷마다 쥔 자세 12시트 + 무기 지운 공격 2시트.')
    add('')
    add('## 5. 받은 뒤 (내가 할 일)')
    add('')
    add('- 넣기 도구는 PixelLab 하복과 같은 규칙으로 만든다: 시트를 칸으로 나눠 192 × 156(PPU 72, `sprite_meta.json` 그림별 `Ppu`)으로 맞추고, 발을 하복 바닥 줄에, 잔 점·떨어진 덩어리를 지운다. '
        '하복과 같은 칸 배치라 칸마다 하복 프레임과 겹쳐 자리를 맞춘다.')
    add('- 무기 겹치기: 무기 층을 가방처럼(`player_anchors.json` 프레임마다 손 X·Y·각도·앞뒤) 둔다. 몸 그림 이름은 `player_{옷}_grip1_*`·`grip2_*`·`swing_attack`·`smash_attack`, 무기는 `weapon_{토큰}`.')
    add('- 옛 그림은 `code_temp/apogirl/backup_pl24/`로 옮기고, 보호구 색 덧칠(`armor_recolor.py install`)을 다시 돌린다(잡지·매트·철판은 보호대·패딩 그림에서 만든다).')
    add('- 하복도 몽둥이를 쓰려면 겹치기가 필요하다. 하복은 A의 쥔 자세로 몽둥이만 겹치고, 지금 있는 방망이·파이프·경찰봉·대걸레 그림은 그대로 둔다.')
    add('')
    add('## 6. 안전 (마스터 프롬프트 13장)')
    add('')
    add('- 모든 프롬프트 끝에 붙였다: 피·상처 없음, 노출·선정적 자세 없음, 글자·상표 없음.')
    add('- 진압복은 경찰 표식·글자 없이 검은 패딩 옷으로만. 헬멧 없이 얼굴이 보이게.')
    add('')
    add('## 7. 소리: 잠든 좀비 다시 받기')
    add('')
    add('지금 잠든 좀비 소리는 코 고는 소리처럼 들린다(1차 문구가 `many slow overlapping breaths`, 숨소리였다). '
        '**낮게 목 안에서 그르렁거리는 소리**로 바꾼다. 조용하지만 무섭게 — 가까이 가면 들리고, 깨우지 않게 살금살금 지나가야 한다는 느낌이 나야 한다.')
    add('')
    add('| 파일 | 어디서 | 규격 | 프롬프트 |')
    add('|---|---|---|---|')
    for key, where, spec, _, _ in SOUNDS:
        add(f'| `{key}.wav` | {where} | {spec} | `prompts/{key}.txt` |')
    add('')
    add('- 모델: Seed Audio 1.0 (`seed_audio`, 힉스필드 기본 소리 모델). 후보를 2~3개 받아 들어 보고 고른다.')
    add('- 게임에서는 잠든 좀비가 주인공 14칸 안에 있을 때 환경음으로 반복된다(`ExpeditionDirector.UpdateSounds`, 반복 이름 `crowd`). 코드는 그대로다.')
    add('- 저장: `ArtInbox/sound16/sfx_z_dormant_crowd.wav` (mp3·ogg로 와도 된다). 내가 이음매를 맞추고(끝과 처음을 겹쳐 이어 붙임) 크기를 지금 소리와 같게 맞춰 '
        '`Resources/Audio/Sfx/sfx_z_dormant_crowd.wav`를 바꾼다. 옛 소리는 `code_temp/apogirl/backup_sound16/`로 옮긴다.')
    add('- 마음에 안 들면 짧은 판으로 다시 받는다.')
    add('')
    for key, _, _, main, alt in SOUNDS:
        add(f'**`{key}`**')
        add('')
        add('```')
        add(main)
        add('```')
        add('')
        add('짧은 판:')
        add('')
        add('```')
        add(alt)
        add('```')
        add('')
    with open(OUT, 'w', encoding='utf-8') as f:
        f.write('\n'.join(L).rstrip() + '\n')


if __name__ == '__main__':
    bs = bundles()
    make_files(bs)
    doc(bs)
    print(len(bs), 'sheets +', len(WEAPONS), 'weapons ->', OUT)
