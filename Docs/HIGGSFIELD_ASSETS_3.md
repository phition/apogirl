# 힉스필드 에셋 3차: 허브 입상 움직임

허브 왼쪽에 서 있는 주인공 입상(`Resources/Art/Standing/stand_*.png`, 680×1024, 2차 문서 1장)에 움직임을 준다. 두 갈래를 동시에 진행한다.

| 갈래 | 무엇 | 그림 | 게임에서 |
|---|---|---|---|
| **A. 겹침 그림** | 눈 깜빡임, 눈동자만 좌우로, 고개 돌리기(일부 차림) | 정지 그림 (편집) | 숨쉬기·흔들림은 코드로. 눈·시선은 얼굴 조각을 잘라 지금 입상 위에 잠깐 겹친다. 고개는 그림을 부드럽게 바꿔 보여 준다 |
| **B. 짧은 영상** | 숨쉬며 서 있기, 두리번거리기, 제자리 걷기, 손 흔들기, 한숨 | 2~5초 영상 → 프레임 | 프레임을 잘라 배경을 지우고 반복 재생. 기본 차림 몇 가지에만 |

A는 모든 차림에 쓰이는 기본 움직임, B는 자주 보이는 기본 차림에서만 나오는 "지정된 동작"이다. B 영상이 없는 차림이면 A만 나온다.

---

## 0. 공통

- 참고 이미지: 지금 입상 원본 `ArtInbox/standing/stand_*.png` (2차에서 받은 것 그대로). 얼굴 기준은 `char_seojin_sheet.png`.
- 받는 폴더: A는 `ArtInbox/standing/`, B는 `ArtInbox/standing_motion/`.
- 비율 **2:3 세로**, 2k 이상. 배경은 지금 입상과 같은 **단색 밝은 회색**.
- 콘텐츠: 2차 문서와 같은 `[안전]` 문구를 끝에 붙인다.

`[고정]` — 편집 프롬프트 맨 뒤에 붙인다 (2차 문서 1.1과 같음)
```
Keep everything else exactly the same as the reference image: same girl, same face, same body proportions, same standing pose, same camera angle, same framing and canvas position, same lighting, same art style, same plain light grey background. Do not move, resize or re-pose the character.
```

`[얼굴 고정]` — 눈만 바꾸는 편집에 더 붙인다
```
Only the eyes change. Hair, eyebrows, nose, mouth, face outline, neck and shoulders stay pixel-identical.
```

---

## 1. A. 겹침 그림 (정지 그림, 편집)

모델: `seedream_v5_pro` 또는 `nano_banana_2_1` (2차와 같음). 참고 이미지 = 해당 입상 1장.

### 1.1 눈 감은 얼굴 (깜빡임) — P1, 9장

눈 부분만 잘라 쓰므로 **표정마다 한 장**이면 모든 옷·머리 길이에 쓸 수 있다.

공통: `Edit the reference image: she closes her eyes naturally, as in the middle of a blink, eyelids relaxed, eyelashes visible.` → `[얼굴 고정]` → `[고정]` → `[안전]`

| 파일 | 참고 이미지 |
|---|---|
| `stand_blink_base.png` | `stand_base.png` (무표정) |
| `stand_blink_smile.png` | `stand_face_smile.png` |
| `stand_blink_bright.png` | `stand_face_bright.png` (웃는 눈 그대로 감기: `smiling closed eyes, curved lids`) |
| `stand_blink_worried.png` | `stand_face_worried.png` |
| `stand_blink_tired.png` | `stand_face_tired.png` (`slow heavy blink`) |
| `stand_blink_sad.png` | `stand_face_sad.png` (`eyes squeezed shut softly`) |
| `stand_blink_determined.png` | `stand_face_determined.png` |
| `stand_blink_surprised.png` | `stand_face_surprised.png` (`quick blink, brows still raised`) |
| `stand_blink_blank.png` | `stand_face_blank.png` |

- 눈썹·입이 조금이라도 바뀌면 겹쳤을 때 티가 난다. 눈만 바뀐 것을 고른다.

### 1.2 눈동자만 좌우로 (시선) — P1, 2장

고개는 그대로, **눈동자만** 옆을 본다. 깜빡임처럼 잘라 겹치므로 모든 차림에 쓴다 (무표정·미소일 때만 쓴다).

공통: `Edit the reference image: she glances to the side with her eyes only, irises moved to the corner of the eyes, head and face not turning.` → `[얼굴 고정]` → `[고정]` → `[안전]`

| 파일 | 참고 | 바꿀 문구 |
|---|---|---|
| `stand_gaze_left.png` | `stand_base.png` | `glancing to her left (viewer's right)` |
| `stand_gaze_right.png` | `stand_base.png` | `glancing to her right (viewer's left)` |

- 방향 이름은 **그림을 보는 쪽 기준**으로 파일을 나눈다: `left` = 화면 왼쪽을 본다. 받은 그림이 반대로 나오면 파일 이름만 바꾸면 된다.

### 1.3 고개 돌리기 (두리번) — P2, 10장

고개가 돌면 머리카락 윤곽이 바뀌어 잘라 겹칠 수 없다. 그래서 **자주 보이는 차림만** 전신으로 받는다. 나머지 차림은 1.2 눈동자 시선으로 대신한다.

공통: `Edit the reference image: she turns her head slightly to look to the side, about 25 degrees, as if checking a sound; the body, arms and legs do not move, only the head and neck turn, hair follows the head naturally.` → 방향 문구 → `[고정]`의 첫 문장만 (`same girl, same body pose, same framing`) → `[안전]`

방향 문구: `left` = `turning to look toward the left side of the picture`, `right` = `turning to look toward the right side of the picture`

| 파일 | 참고 이미지 | 언제 보이나 |
|---|---|---|
| `stand_turn_base_left.png` / `_right.png` | `stand_base.png` | 봄, 단발 |
| `stand_turn_hair_mid_left.png` / `_right.png` | `stand_hair_mid.png` | 14주~, 묶은 머리 |
| `stand_turn_hair_long_left.png` / `_right.png` | `stand_hair_long.png` | 40주~, 긴 머리 |
| `stand_turn_outfit_summer_left.png` / `_right.png` | `stand_outfit_summer.png` | 여름 |
| `stand_turn_outfit_winter_left.png` / `_right.png` | `stand_outfit_winter.png` | 겨울 |

- 몸이 조금이라도 돌면 바꿔 보여 줄 때 튄다. **목 아래가 그대로인 것**을 고른다.

---

## 2. B. 짧은 영상 (지정 동작)

모델: **Seedance 2.5** (`seedance_2_5`), 모드 `omni_reference`, **시작 프레임과 끝 프레임을 같은 입상 그림**으로 넣는다 (`--start-image`·`--end-image`). 그래야 처음과 끝이 이어져 반복 재생할 때 튀지 않는다.
설정: 1080p, 세로(9:16 또는 2:3에 가장 가까운 비율), 길이는 표에 적힌 대로. 소리는 필요 없다.

`[영상 고정]` — 모든 영상 프롬프트 맨 뒤에 붙인다
```
Locked-off static camera, no camera movement, no zoom, no cut. The plain light grey background stays completely unchanged and empty. The girl stays in the same spot and the same size in the frame for the whole clip; her outfit, hair and face stay consistent with the first frame. The clip ends in exactly the same pose as it starts so it can loop seamlessly. Gentle, small, natural motion; 2D anime illustration style matching the reference, no 3D look.
```

### 2.1 기본 차림 (P1) — 참고 이미지 `stand_base.png`

| 파일 | 길이 | 동작 (프롬프트 앞부분) | 게임에서 |
|---|---|---|---|
| `motion_base_idle.mp4` | 4초 | `She stands still and breathes calmly: her shoulders and chest rise and fall slightly, she blinks once, a few strands of hair sway faintly.` | 늘 (기본 반복) |
| `motion_base_look.mp4` | 5초 | `She looks around cautiously: she turns her head slowly to the left, pauses, then to the right, pauses, then back to the front; her body stays still.` | 몇십 초마다 한 번, 위협도가 높을 때 더 자주 |
| `motion_base_walk.mp4` | 2초 | `She walks in place with a natural relaxed walking cycle, as if on a treadmill, arms swinging slightly, facing the same direction; she does not move across the frame.` | 한 주 실행 중 (방으로 이동하는 동안) |
| `motion_base_wave.mp4` | 3초 | `She gives a small shy wave with her right hand at chest height, then lowers the hand back to her side.` | 계획 보드를 열 때·다음 주 시작 |
| `motion_base_sigh.mp4` | 3초 | `She lets out a tired sigh: her shoulders drop, she closes her eyes for a moment, then straightens up again.` | 피로·수면 부채가 높을 때 |

### 2.2 계절 차림 (P2) — 2.1이 괜찮으면

같은 다섯 동작을 아래 그림으로 다시 만든다. 파일 이름의 `base` 자리에 차림 이름을 넣는다 (예: `motion_summer_idle.mp4`).

| 차림 | 참고 이미지 |
|---|---|
| `hair_mid` (14주~ 기본) | `stand_hair_mid.png` |
| `summer` | `stand_outfit_summer.png` |
| `winter` | `stand_outfit_winter.png` |
| `hair_long` (40주~) | `stand_hair_long.png` |

- 먼저 **2.1의 `idle`과 `look` 두 개만** 만들어 보고 알려 주면, 게임에 넣어 자연스러운지 확인한 뒤 나머지를 진행한다 (영상이 한 장씩 흔들리면 프레임 수·길이를 조정한다).

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
