"""HIGGSFIELD_ASSETS_3.md를 Claude 채팅(힉스필드 커넥터)에 바로 붙여 넣을 수 있는 형태로 만든다."""
OUT = r'I:/workshop/apogirl/apogirl/.claude/worktrees/unity-mcp-connection-4f686e/Docs/HIGGSFIELD_ASSETS_3.md'
CDN = 'https://d8j0ntlcm91z4.cloudfront.net/user_3K3GDbFrhFIfl8ivGn5P16JoJBa/'

# 힉스필드 라이브러리 (2026-11-20 생성 기록, show_generations) — 파일 이름 → (작업 ID, 결과 이미지)
LIB = {
    'stand_base': ('86bf82f6-c7fe-4109-a454-f01be423285d', 'hf_20261120_164257_86bf82f6-c7fe-4109-a454-f01be423285d.png'),
    'stand_face_smile': ('f2cee9af-a790-495f-80b5-104a350b0884', 'hf_20261120_165031_f2cee9af-a790-495f-80b5-104a350b0884.png'),
    'stand_face_bright': ('97fc0fd3-ad7a-49d4-9b53-491cfcf9c15c', 'hf_20261120_210434_97fc0fd3-ad7a-49d4-9b53-491cfcf9c15c.png'),
    'stand_face_worried': ('87514d9c-770b-44b4-a72b-c134665d03f8', 'hf_20261120_210539_87514d9c-770b-44b4-a72b-c134665d03f8.png'),
    'stand_face_tired': ('17d378cf-75b8-4b73-818a-2a3d3c990aaf', 'hf_20261120_165144_17d378cf-75b8-4b73-818a-2a3d3c990aaf.png'),
    'stand_face_sad': ('eb51241d-041b-4dd2-b29b-5a1a2f6b215d', 'hf_20261120_210648_eb51241d-041b-4dd2-b29b-5a1a2f6b215d.png'),
    'stand_face_determined': ('2acb9bfe-f2d5-4594-82c8-0e2a63e172d8', 'hf_20261120_210802_2acb9bfe-f2d5-4594-82c8-0e2a63e172d8.png'),
    'stand_face_surprised': ('8dc014f1-8ca0-4f6e-8f6d-ce3640e7e450', 'hf_20261120_210905_8dc014f1-8ca0-4f6e-8f6d-ce3640e7e450.png'),
    'stand_face_blank': ('ecd8abba-94e7-49c4-b9ab-3cc9711a856b', 'hf_20261120_211010_ecd8abba-94e7-49c4-b9ab-3cc9711a856b.png'),
    'stand_hair_mid': ('c66b0cab-04f9-4d00-beb3-49017c387c65', 'hf_20261120_210209_c66b0cab-04f9-4d00-beb3-49017c387c65.png'),
    'stand_hair_long': ('2c927180-dbbf-45a9-bb69-4fd7f53198d5', 'hf_20261120_210328_2c927180-dbbf-45a9-bb69-4fd7f53198d5.png'),
    'stand_outfit_summer': ('1ea6aa36-2d7f-4d14-81d1-adaba215bac1', 'hf_20261120_164547_1ea6aa36-2d7f-4d14-81d1-adaba215bac1.png'),
    'stand_outfit_winter': ('d0e6502f-4820-4b9d-adad-d8d5a48a048c', 'hf_20261120_205735_d0e6502f-4820-4b9d-adad-d8d5a48a048c.png'),
    'stand_outfit_gym': ('1b8e9a47-a220-49c0-85ce-b2596129c38d', 'hf_20261120_205842_1b8e9a47-a220-49c0-85ce-b2596129c38d.png'),
    'stand_outfit_guard': ('69bffb27-e656-40aa-9f11-41472727acff', 'hf_20261120_205952_69bffb27-e656-40aa-9f11-41472727acff.png'),
    'stand_outfit_padding': ('e5f5998c-31f6-4e75-ab86-b3448d5d33bb', 'hf_20261120_164643_e5f5998c-31f6-4e75-ab86-b3448d5d33bb.png'),
    'stand_outfit_riot': ('26b5ca0e-8887-4f03-959e-5d36300493ac', 'hf_20261120_210059_26b5ca0e-8887-4f03-959e-5d36300493ac.png'),
    'stand_mark_bandage_arm': ('6bbbad82-e5fa-414e-9541-10f3930b7ab3', 'hf_20261120_164919_6bbbad82-e5fa-414e-9541-10f3930b7ab3.png'),
    'stand_mark_bandage_head': ('b661c8e3-3662-421e-862e-852e678b7384', 'hf_20261120_211116_b661c8e3-3662-421e-862e-852e678b7384.png'),
    'stand_mark_bandage_leg': ('dbd54f61-0387-4104-85ae-ba7dcb549c63', 'hf_20261120_211220_dbd54f61-0387-4104-85ae-ba7dcb549c63.png'),
    'stand_mark_dirty': ('b58046cf-9e76-4f9e-852e-51acf5a22b42', 'hf_20261120_211324_b58046cf-9e76-4f9e-852e-51acf5a22b42.png'),
    'stand_mark_mask': ('3abe881f-c2ca-4c87-82a8-491f36b6c86e', 'hf_20261120_211531_3abe881f-c2ca-4c87-82a8-491f36b6c86e.png'),
    'stand_mark_thin': ('5b25d2ae-d07e-44b6-8f35-731ad2c92a85', 'hf_20261120_211640_5b25d2ae-d07e-44b6-8f35-731ad2c92a85.png'),
}
SHEET_ID = '5f2847e0-2263-4d57-a880-05080133d481'
SHEET_URL = 'https://d2ol7oe51mr4n9.cloudfront.net/user_3K3GDbFrhFIfl8ivGn5P16JoJBa/5f2847e0-2263-4d57-a880-05080133d481_resize.jpg'

FIX = ('Keep everything else exactly the same as the reference image: same girl, same face, same body proportions, same standing pose, '
       'same camera angle, same framing and canvas position, same lighting, same art style, same plain light grey background. '
       'Do not move, resize or re-pose the character.')
FACE_FIX = 'Only the eyes change. Hair, eyebrows, nose, mouth, face outline, neck and shoulders stay pixel-identical.'
SAFE = ('No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, '
        'no text, no logo, no watermark.')
VIDEO_FIX = ('Locked-off static camera, no camera movement, no zoom, no cut. The plain light grey background stays completely unchanged '
             'and empty. The girl stays in the same spot and the same size in the frame for the whole clip; her outfit, hair and face '
             'stay consistent with the first frame. The clip ends in exactly the same pose as it starts so it can loop seamlessly. '
             'Gentle, small, natural motion; 2D anime illustration style matching the reference, no 3D look.')
TURN_FIX = ('Keep the same girl, same face, same clothes, same body pose from the neck down, same camera angle, same framing and canvas '
            'position, same lighting, same art style, same plain light grey background. Do not move, resize or re-pose the body.')


def ref(name):
    jid, file = LIB[name]
    return jid, CDN + file


def image_block(out_name, ref_name, prompt):
    jid, url = ref(ref_name)
    return f'''#### `{out_name}.png`
참고 이미지: `{ref_name}` — 작업 ID `{jid}` · [이미지 보기]({url})
```
Higgsfield로 이미지를 편집해 줘.
- 모델: seedream_v5_pro, 비율 2:3, 해상도 2k
- 참고 이미지(medias): 첫 번째 = 이전 생성 작업 ID {jid} ({ref_name}), 두 번째 = 업로드 미디어 ID {SHEET_ID} (캐릭터 시트, 얼굴 확인용)
- 결과 파일 이름: {out_name}.png
- 프롬프트:
{prompt}
```
'''


def video_block(out_name, ref_name, seconds, motion):
    jid, url = ref(ref_name)
    return f'''#### `{out_name}.mp4` ({seconds}초)
시작·끝 프레임: `{ref_name}` — 작업 ID `{jid}` · [이미지 보기]({url})
```
Higgsfield로 영상을 만들어 줘.
- 모델: seedance_2_5, 모드 omni_reference, 1080p, 세로 9:16, 길이 {seconds}초, 소리 없음
- 시작 프레임과 끝 프레임: 둘 다 이전 생성 작업 ID {jid} ({ref_name}) — 처음과 끝이 같아야 반복 재생된다
- 결과 파일 이름: {out_name}.mp4
- 프롬프트:
{motion} The reference image is both the first and the last frame.
{VIDEO_FIX}
{SAFE}
```
'''


BLINK = [
    ('base', 'stand_base', 'she closes her eyes naturally, as in the middle of a blink, eyelids relaxed, eyelashes visible.'),
    ('smile', 'stand_face_smile', 'she closes her eyes naturally, as in the middle of a blink, keeping the same small smile, eyelids relaxed.'),
    ('bright', 'stand_face_bright', 'she closes her eyes in the middle of a blink while keeping the same bright smile: smiling closed eyes with gently curved lids.'),
    ('worried', 'stand_face_worried', 'she closes her eyes in the middle of a blink, brows still knitted with worry.'),
    ('tired', 'stand_face_tired', 'a slow heavy blink: her heavy eyelids fully closed, the same tired look.'),
    ('sad', 'stand_face_sad', 'she closes her eyes softly, eyelids gently squeezed, the same sad expression.'),
    ('determined', 'stand_face_determined', 'she closes her eyes in the middle of a blink, the same firm determined brows.'),
    ('surprised', 'stand_face_surprised', 'a quick blink: her eyes closed while her eyebrows stay raised in surprise.'),
    ('blank', 'stand_face_blank', 'she closes her eyes slowly, the same empty expressionless face.'),
]
TURN = [('base', 'stand_base'), ('hair_mid', 'stand_hair_mid'), ('hair_long', 'stand_hair_long'),
        ('outfit_summer', 'stand_outfit_summer'), ('outfit_winter', 'stand_outfit_winter')]
MOTIONS = [
    ('idle', 4, 'She stands still and breathes calmly: her shoulders and chest rise and fall slightly, she blinks once, a few strands of hair sway faintly.', 'P1 시험', '늘 (기본 반복)'),
    ('look', 5, 'She looks around cautiously: she turns her head slowly to the left, pauses, then to the right, pauses, then back to the front; her body stays still.', 'P1 시험', '몇십 초마다 한 번, 위협도가 높으면 더 자주'),
    ('walk', 2, 'She walks in place with a natural relaxed walking cycle, as if on a treadmill, arms swinging slightly, facing the same direction; she does not move across the frame.', 'P1', '한 주 실행 중'),
    ('wave', 3, 'She gives a small shy wave with her right hand at chest height, then lowers the hand back to her side.', 'P1', '계획 보드를 열 때'),
    ('sigh', 3, 'She lets out a tired sigh: her shoulders drop, she closes her eyes for a moment, then straightens up again.', 'P1', '피로·수면 부채가 높을 때'),
]

doc = []
w = doc.append
w('''# 힉스필드 에셋 3차: 허브 입상 움직임

허브 왼쪽에 서 있는 주인공 입상(`Resources/Art/Standing/stand_*.png`, 2차 문서 1장)에 움직임을 준다. 두 갈래를 동시에 진행한다.

| 갈래 | 무엇 | 그림 | 게임에서 |
|---|---|---|---|
| **A. 겹침 그림** | 눈 깜빡임, 눈동자만 좌우로, 고개 돌리기(일부 차림) | 정지 그림 (편집) | 숨쉬기·흔들림은 코드로. 눈·시선은 얼굴 조각을 잘라 지금 입상 위에 잠깐 겹친다. 고개는 그림을 부드럽게 바꿔 보여 준다 |
| **B. 짧은 영상** | 숨쉬며 서 있기, 두리번거리기, 제자리 걷기, 손 흔들기, 한숨 | 2~5초 영상 → 프레임 | 프레임을 잘라 배경을 지우고 반복 재생. 기본 차림 몇 가지에만 |

A는 모든 차림에 쓰이는 기본 움직임, B는 자주 보이는 기본 차림에서만 나오는 "지정된 동작"이다. B 영상이 없는 차림이면 A만 나온다.

---

## 0. 쓰는 법

### 0.1 Claude 채팅에 붙여 넣기

[HIGGSFIELD_ASSETS.md](HIGGSFIELD_ASSETS.md) 0.1처럼 **claude.ai 채팅 + Higgsfield 커넥터**에서 만든다. 아래 1장·2장의 **회색 상자 하나가 그림(영상) 하나**다. 상자를 통째로 복사해 채팅에 붙여 넣으면 된다. 모델·비율·참고 이미지(라이브러리의 작업 ID)·결과 파일 이름·프롬프트(고정·안전 문구 포함)가 모두 들어 있다.

- 한 번에 여러 상자를 붙여 넣어도 된다 ("아래를 차례로 만들어 줘").
- 결과를 내려받아 **상자에 적힌 파일 이름 그대로** `ArtInbox/standing/`(A) · `ArtInbox/standing_motion/`(B)에 넣는다.
- 참고 이미지는 이전에 만든 결과의 **작업 ID**로 넘긴다(커넥터의 `medias`에 그대로 넣을 수 있음). 채팅이 ID를 못 쓰면 상자 위의 "이미지 보기" 주소를 대신 넘기면 된다.

### 0.2 힉스필드 라이브러리의 기존 입상 (참고 이미지)

2026-11-20에 만든 2차 입상의 생성 기록이다. 힉스필드 웹의 생성 기록(Library)에서 같은 작업을 찾거나, 아래 주소로 원본을 바로 볼 수 있다.

| 파일 | 작업 ID | 원본 |
|---|---|---|
''')
for name, (jid, file) in LIB.items():
    w(f'| `{name}` | `{jid}` | [보기]({CDN + file}) |\n')
w(f'| 캐릭터 시트 (업로드) | `{SHEET_ID}` | [보기]({SHEET_URL}) |\n')
w('''
- `stand_face_smile`·`stand_mark_thin`은 기록이 두 개씩 있어 **나중 것**을 적었다. 게임에 들어간 것과 다르면 `ArtInbox/standing/`의 파일을 대신 올려 쓰면 된다.

### 0.3 공통 문구 (상자 안에 이미 들어 있음)

`[고정]` (2차 문서 1.1)
```
''' + FIX + '''
```
`[얼굴 고정]` (눈만 바꾸는 편집)
```
''' + FACE_FIX + '''
```
`[안전]` ([HIGGSFIELD_ASSETS.md](HIGGSFIELD_ASSETS.md) 0.4, 모든 이미지·영상 끝에)
```
''' + SAFE + '''
```
`[영상 고정]` (2장 영상 끝에)
```
''' + VIDEO_FIX + '''
```

---

## 1. A. 겹침 그림 (정지 그림, 편집)

### 1.1 눈 감은 얼굴 (깜빡임) — P1, 9장

눈 부분만 잘라 쓰므로 **표정마다 한 장**이면 모든 옷·머리 길이에 쓸 수 있다. 눈썹·입이 조금이라도 바뀐 것은 다시 만든다.

''')
for key, ref_name, change in BLINK:
    prompt = f'Edit the reference image (the first image; the second image is only the character sheet for identity): {change}\n{FACE_FIX}\n{FIX}\n{SAFE}'
    w(image_block(f'stand_blink_{key}', ref_name, prompt) + '\n')

w('''### 1.2 눈동자만 좌우로 (시선) — P1, 2장

고개는 그대로, **눈동자만** 옆을 본다. 깜빡임처럼 잘라 겹치므로 모든 차림에 쓴다(무표정·미소일 때). 파일 이름의 방향은 **그림을 보는 쪽 기준**이다(`left` = 화면 왼쪽을 본다). 반대로 나오면 파일 이름만 바꾸면 된다.

''')
for side, words in [('left', 'toward the left side of the picture'), ('right', 'toward the right side of the picture')]:
    prompt = (f'Edit the reference image (the first image; the second image is only the character sheet for identity): she glances {words} '
              f'with her eyes only, irises moved to the corner of the eyes, head and face not turning.\n{FACE_FIX}\n{FIX}\n{SAFE}')
    w(image_block(f'stand_gaze_{side}', 'stand_base', prompt) + '\n')

w('''### 1.3 고개 돌리기 (두리번) — P2, 10장

고개가 돌면 머리카락 윤곽이 바뀌어 잘라 겹칠 수 없다. 그래서 **자주 보이는 차림만** 전신으로 받는다. 나머지 차림은 1.2 시선으로 대신한다. 몸이 조금이라도 돌면 바꿔 보여 줄 때 튀므로 **목 아래가 그대로인 것**을 고른다.

''')
for key, ref_name in TURN:
    for side, words in [('left', 'toward the left side of the picture'), ('right', 'toward the right side of the picture')]:
        prompt = (f'Edit the reference image (the first image; the second image is only the character sheet for identity): she turns her head '
                  f'slightly to look {words}, about 25 degrees, as if checking a sound; the body, arms and legs do not move, only the head '
                  f'and neck turn, hair follows the head naturally.\n{TURN_FIX}\n{SAFE}')
        w(image_block(f'stand_turn_{key}_{side}', ref_name, prompt) + '\n')

w('''---

## 2. B. 짧은 영상 (지정 동작)

Seedance 2.5에 **시작 프레임과 끝 프레임으로 같은 입상**을 넣는다. 처음과 끝이 같아야 반복 재생할 때 튀지 않는다.

### 2.1 기본 차림 (`stand_base`)

**먼저 `idle`과 `look` 두 개만** 만들어 알려 주면, 게임에 넣어 자연스러운지 확인한 뒤 나머지를 진행한다.

| 동작 | 우선 | 게임에서 |
|---|---|---|
''')
for key, sec, motion, pri, use in MOTIONS:
    w(f'| `{key}` ({sec}초) | {pri} | {use} |\n')
w('\n')
for key, sec, motion, pri, use in MOTIONS:
    w(video_block(f'motion_base_{key}', 'stand_base', sec, motion) + '\n')

w('''### 2.2 계절 차림 (P2) — 2.1이 괜찮으면

같은 다섯 동작을 아래 그림으로 다시 만든다. 2.1의 상자에서 **작업 ID 두 곳**(시작·끝 프레임)과 **파일 이름의 `base`**만 바꾸면 된다.

| 차림 | 파일 이름 예 | 시작·끝 프레임 작업 ID |
|---|---|---|
''')
for key, ref_name in [('hair_mid', 'stand_hair_mid'), ('summer', 'stand_outfit_summer'), ('winter', 'stand_outfit_winter'), ('hair_long', 'stand_hair_long')]:
    jid, url = ref(ref_name)
    w(f'| `{key}` | `motion_{key}_idle.mp4` | `{jid}` ([보기]({url})) |\n')

w('''
---

## 3. 체크리스트

- [ ] A 1.1 눈 감은 얼굴 9장
- [ ] A 1.2 눈동자 시선 2장
- [ ] B 2.1 `idle`·`look` 시험 2개 → 확인 → 나머지 3개
- [ ] A 1.3 고개 돌리기 10장
- [ ] B 2.2 계절 차림

## 4. 받은 뒤 내가 하는 일

1. **A 겹침**: 눈 감은 얼굴·시선 그림에서 지금 입상과 다른 부분(눈 주변)만 찾아 잘라 `stand_blink_*`·`stand_gaze_*` 조각으로 만들고, 입상의 얼굴 위치에 겹친다. 고개 돌리기는 배경을 지워 전신 그림으로 넣고 바꿔 보여 줄 때 0.25초 정도 겹쳐 사라지게 한다.
2. **움직임 코드** (`StandingView`): 숨쉬기(아주 작은 위아래 크기 변화)·흔들림, 깜빡임(3~6초마다, 가끔 두 번), 시선(쉴 때 가끔), 고개 돌리기(차림에 그림이 있을 때). 수치는 `standing_map.json`에 둔다.
3. **B 영상**: 12프레임/초로 잘라 배경(밝은 회색)을 지우고 680×1024로 맞춰 `Resources/Art/Standing/Motion/{차림}_{동작}_{n}.png`로. 반복 이음매를 확인하고, 튀면 앞뒤 몇 장을 겹쳐 섞는다.
4. **어느 동작을 언제** (`standing_map.json`): 기본 `idle`, 이따금 `look`, 실행 중 `walk`, 보드 열 때 `wave`, 피곤할 때 `sigh`. 영상이 없는 차림·표정·상태(붕대·마스크 등)에서는 A만.
5. 상태 표시(붕대·얼룩·마스크)나 방어구 차림은 영상과 안 맞으므로 그때는 영상 대신 A로 돌아간다.
''')

open(OUT, 'w', encoding='utf-8').write(''.join(doc))
print('ok', sum(1 for _ in open(OUT, encoding='utf-8')))
