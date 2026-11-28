# 힉스필드 에셋 19차: 출정 준비 화면 그림 5장 (2026-11-28)

출정 준비 화면(`ExpeditionPrepView`)에 그림 자리가 두 곳 생겼다. 지금은 있는 그림을 빌려 깔아 두었고, 이 문서의 그림을 받으면 **파일만 넣으면** 바뀐다(그림 표 `art_map.json`은 고치지 않는다).

- **왼쪽 — 가는 곳 그림** (`Locations` 표): 튜토리얼(창고 → 교실동)은 지금 교실동 그림 `loc_school`을 빌려 쓴다.
- **오른쪽 — 준비하는 자리** (`PrepScenes` 표): 주인공 입상과 장비 칸 뒤에 깔리는 그림. 지금은 거처 배경(`bg_storage`·`bg_classroom`과 밤 그림)을 빌려 쓴다.
- **받을 그림: 5장** — 왼쪽 1장 + 오른쪽 4장(거처 둘 × 낮·밤).
- **작업 제출은 사용자가 한다.** 나(Claude)는 문서만 쓴다.

쓰는 법:

- 공통 공식은 [HIGGSFIELD_ASSETS.md](HIGGSFIELD_ASSETS.md) 0.3·0.4를 따른다(`[F-ILLU]`·`[F-KOREA]`·`[안전]`).
- 모두 `gpt_image_2_6`, 2k 이상. **사람은 그리지 않는다** — 오른쪽 그림 앞에는 지금 차림의 주인공 입상이 서고(장비를 바꾸면 입상이 바뀐다), 왼쪽 그림은 다른 지역 그림(`loc_*`)처럼 빈 풍경이다. 그림 속에 주인공이 있으면 둘이 된다.
- 사람이 없으니 참고 이미지는 장소를 잇는 용도다: 표의 "참고 그림" 칸의 그림을 넣는다.
- 프롬프트 = `[F-ILLU]`의 `Background:` 문장부터(사물·풍경만 있는 그림이라 인물 문장은 뺀다 — HIGGSFIELD_ASSETS 0.3) + (실외·창밖이 보이면 `[F-KOREA]`) + 장면 + `[구도]` + `[안전]`
- 넣는 곳: `ArtInbox/illustrations/prep19/`, 파일 이름은 표의 이름 그대로.

---

## 0. 한눈에 보기

| 번호 | 파일 | 쓰이는 곳 | 비율 | 지금 빌려 쓰는 그림 | 우선 |
|---|---|---|---|---|---|
| 1.1 | `loc_tutorial.png` | 왼쪽: 1주차 7일 낮 탈출(체육관 창고 → 본관, 화면 제목 "체육관 → 본관 창고") | 4:3 | `Locations/loc_school` | **P1** |
| 2.1 | `prep_storage.png` | 오른쪽: 창고 거처, 낮 출정 | 3:4 (세로) | `Backgrounds/bg_storage` | **P1** |
| 2.2 | `prep_storage_night.png` | 오른쪽: 창고 거처, 밤 출정 | 3:4 | `Backgrounds/bg_storage_night` | P2 |
| 2.3 | `prep_classroom.png` | 오른쪽: 교실 거처(`CLASSROOM_MOVED_IN`), 낮 출정 | 3:4 | `Backgrounds/bg_classroom` | **P1** |
| 2.4 | `prep_classroom_night.png` | 오른쪽: 교실 거처, 밤 출정 | 3:4 | `Backgrounds/bg_classroom_night` | P2 |

밤 그림(P2)이 없으면 낮 그림을 어둡게 물들여 쓴다. 낮 그림(P1)도 없으면 지금처럼 거처 배경을 쓴다.

### 0.1 화면에서 보이는 자리 (구도를 잡을 때)

출정 준비 화면은 1920×1080이고 가운데 나무 띠로 둘로 나뉜다.

- **왼쪽 1080×1080 (정사각형에 가깝다)**: 그림을 꽉 채우고(넘치는 쪽을 자른다) 아래 70%는 위에서 아래로 어두워진다. 그 어두운 곳에 목적지 이름·예보 글이 놓인다. → **볼거리는 위쪽 절반, 가운데 정사각형 안에.** 아래쪽은 바닥·풀밭처럼 비어 있어도 된다.
- **오른쪽 840×1080 (세로로 길다)**: 그림을 꽉 채우고 전체를 조금 어둡게 덮는다. 왼쪽 380은 주인공 입상(전신)이 서고, 오른쪽 460은 위에 장비 칸 다섯 줄·맨 아래 출발 단추가 놓인다. 그림이 잘 보이는 곳은 **입상 머리 위·어깨 옆과, 장비 칸과 출발 단추 사이(오른쪽 가운데~아래)** 다. → **챙겨 둔 물건은 오른쪽 아래 1/3에, 빛(창·문·랜턴)은 위쪽에, 왼쪽 절반은 빈 바닥과 벽으로.**

---

## 1. 이 문서에서 쓰는 문구

**[구도-왼쪽]** — 1.1에 붙인다.
```
Composition: 4:3 landscape. All important elements sit in the upper half and inside the central square of the frame; the bottom third is plain ground (dirt, weeds, shadow) because text will be placed over it. Eye-level view, wide lens, no people, no animals.
```

**[구도-오른쪽]** — 2.1~2.4에 붙인다.
```
Composition: 3:4 portrait, eye-level view from inside the room. The left half of the frame is mostly empty floor and a plain wall (a character sprite will be overlaid there). The gathered gear is laid out in the lower right third. The light source (window, door or lamp) is in the upper part of the frame. No people, no hands, no animals. Slightly dim, calm, a quiet moment before leaving.
```

**[챙긴 것]** — 2.1~2.4의 물건. 게임 안의 장비와 맞춘다(무기는 학교에서 구할 수 있는 둔기뿐, 총기·날붙이 없음).
```
an open worn canvas school backpack with its flap folded back, a wooden baseball bat leaning beside it, a small flashlight, two plastic water bottles, a rolled clean bandage, a folded hand-drawn paper map (unreadable), a pair of worn sneakers placed neatly side by side
```

---

## 2. 왼쪽: 가는 곳 그림 1장

| 번호 | 파일 | 참고 그림 | 그리는 장면 | 장면 (영어, `[F-ILLU]` Background 문장 + `[F-KOREA]` 뒤에) |
|---|---|---|---|---|
| 1.1 | `loc_tutorial.png` | `loc_school.jpg`(교실동 생김새), `bg_storage.jpg`(창고 문) | 봄, 한낮. 열린 체육관 창고 철문 안쪽에서 내다본 운동장과 그 건너 본관(교실동). 7일 만에 처음 보는 바깥 | `View from just inside the open grey steel door of a school gym's sports equipment storage room, looking out. The dark door frame and a strip of the storage room's shadowed floor form a thin frame at the left and bottom edges. Outside in bright spring noon light: an empty dirt schoolyard with weeds and a fallen rusty soccer goal, and across it the four-story red-brick classroom building of a Korean public high school with a tiled roof, a few broken windows, an open concrete stairwell at its left end and a canopied main entrance; cherry trees in bloom along the wall, petals drifting. The building is the same one as in the attached reference. Nobody in sight, very quiet, a little too bright after days in the dark.` + `[구도-왼쪽]` |

---

## 3. 오른쪽: 준비하는 자리 4장

| 번호 | 파일 | 참고 그림 | 그리는 장면 | 장면 (영어, `[F-ILLU]` Background 문장 뒤에) |
|---|---|---|---|---|
| 2.1 | `prep_storage.png` | `bg_storage.jpg` | 창고 거처, 낮. 철문 옆 바닥에 챙겨 둔 것들 | `Interior of the same cramped school sports storage room turned shelter as in the attached reference: peeling pale-green and cream walls, rusty steel shelves with plastic boxes and duffel bags, ball baskets, stacked gym mats, a worn plank floor. In the upper right, a grey steel door stands slightly ajar with a thin vertical line of daylight; dusty light also falls from one small high barred window. On a flattened cardboard sheet on the floor near the door lies the gathered gear:` + `[챙긴 것]` + `. A chalk tally of days on the wall.` + `[구도-오른쪽]` |
| 2.2 | `prep_storage_night.png` | `bg_storage_night.jpg`, 받은 2.1 | 같은 자리, 밤. 캠핑 랜턴 하나 | `The same storage room shelter and the same laid-out gear as in the attached reference, at night: the steel door is shut, the high window is dark blue, and a single small camping lantern on a low box in the upper middle of the frame gives a warm pool of light over the gear; everything else falls into deep blue shadow. A wind-up alarm clock next to the lantern.` + `[챙긴 것]` + `[구도-오른쪽]` |
| 2.3 | `prep_classroom.png` | `bg_classroom.jpg` | 교실 거처, 낮. 붙여 놓은 책상 위에 챙겨 둔 것들, 칠판의 분필 약도 | `Interior of the same Korean high school classroom turned living space as in the attached reference: light wood desks with steel legs, a green chalkboard with a hand-drawn chalk map of the neighborhood (lines and circles only, unreadable), pale yellow curtains, warm daylight from tall windows in the upper left. In the lower right, two student desks pushed together hold the gathered gear:` + `[챙긴 것]` + `. The sneakers are on the floor under the desk. A sliding wooden classroom door barricaded with stacked chairs is visible at the right edge.` + `[구도-오른쪽]` |
| 2.4 | `prep_classroom_night.png` | `bg_classroom_night.jpg`, 받은 2.3 | 같은 자리, 밤. 커튼을 치고 랜턴 하나 | `The same classroom living space and the same gear on the two pushed-together desks as in the attached reference, at night: heavy curtains drawn over the windows, one small camping lantern on the desks lighting the gear with a warm glow, the chalk map on the board barely visible, long soft shadows across the floor.` + `[챙긴 것]` + `[구도-오른쪽]` |

- 2.2·2.4는 **받은 낮 그림(2.1·2.3)을 참고 이미지로 넣어** 같은 자리의 밤으로 만든다. 물건 자리가 낮·밤에 같아야 낮 출정·밤 출정을 오갈 때 어색하지 않다.
- 계절은 넣지 않았다(창이 작거나 커튼을 쳐서 계절이 거의 안 보인다). 한 해 내내 같은 그림을 쓴다.

---

## 4. 받은 뒤 내가 하는 일

1. `ArtInbox/illustrations/prep19/`의 그림을 jpg로 바꾼다. **가로·세로 모두 4의 배수** — `loc_tutorial` 1600×1200, `prep_*` 1080×1440 (4의 배수가 아니면 압축 없이 들어가 빌드가 커진다).
2. 넣는 곳 — 파일 이름이 표와 같으면 `art_map.json`을 고치지 않아도 바뀐다(표에 이미 이 이름이 먼저, 빌린 그림이 `Fallback`으로 적혀 있다).
   - `Resources/Art/Story/Locations/loc_tutorial.jpg`
   - `Resources/Art/Story/Prep/prep_storage.jpg`, `prep_storage_night.jpg`, `prep_classroom.jpg`, `prep_classroom_night.jpg`
3. 출정 준비 화면을 1920×1080으로 찍어 본다: 장비 칸 글이 잘 읽히는지, 챙겨 둔 물건이 장비 칸과 출발 단추 사이로 보이는지. 가로로 넘치는 그림이면 표의 `FocusX`(0 = 그림 왼쪽 끝, 1 = 오른쪽 끝)로 보여 줄 자리를 옮긴다(빌린 그림은 `FallbackFocusX`). 어둠 막 짙기는 `ExpeditionPrepView`의 `VeilLeft`·`VeilRight`.
4. 원하면 다른 지역의 밤 그림·다른 거처 그림도 같은 표에 줄만 더하면 된다.

## 5. 체크리스트

- [ ] P1 3장: 1.1 `loc_tutorial`, 2.1 `prep_storage`, 2.3 `prep_classroom`
- [ ] P2 2장: 2.2 `prep_storage_night`, 2.4 `prep_classroom_night`
