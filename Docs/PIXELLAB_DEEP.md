# 탐험의 깊이(B) 픽셀 아트 프롬프트 — PixelLab (2026-11-22)

깊은 곳(지하 층)을 채울 PixelLab 프롬프트다. **P1 12장은 만들어 넣었다 (2026-11-22)** — P2는 아직 코드 그림.
탐험 화면은 픽셀 아트라 힉스필드가 아니라 PixelLab으로 만든다 (힉스필드 8차 문서 6절).

- 만드는 도구: `code_temp/apogirl/tools/pixellab.py`의 `image_pro` (generate-image-v2), 옥상 때 쓴 `gen_roof.py`와 같은 방식 → 새 스크립트 `gen_deep.py`.
- 그림체 기준(style 이미지): 지금 탐험 그림 `Resources/Art/ExplorationPixel/`의 `wall_corridor`·`prop_exit`·`prop_locker`.
- 넣는 곳: `Resources/Art/ExplorationPixel/` (PPU 36). 후보 여러 장 중 고른 것만 넣는다.
- 그림이 없으면 지금처럼 코드 그림으로 그려지므로, 일부만 만들어도 화면은 멀쩡하다.

## 0. 한눈에 보기

| 절 | 무엇 | 개수 | 지금 | 연결 |
|---|---|---|---|---|
| 1 | 세 가지 문 (깊은 문·일방 출구 안·밖) | 3 (+막힘 2) | 코드 그림 | 키가 이미 있다 → 넣으면 바로 |
| 2 | 지하 벽·바닥 타일 | 공통 2 + 지역 5 | 코드 그림 | 공통은 바로, 지역별은 키 찾기 코드 한 줄 |
| 3 | 천장 배관 띠·비상등 | 2 | 코드 사각형 | `BuildingView.BuildDeep` 손봄 |
| 4 | 지역별 장식 | 10 | 없음 | 장식 배치 추가 (`LocationBuilder`) |
| 5 | 침수·CD 소품 | 2 | 반투명 사각형·종이 | 키 바꾸기 |

**전부 26장** (P1 12장 · P2 14장). 한 장에 후보 4~8장이 나온다.

## 0.1 공통 문구 (모든 프롬프트 앞에 붙는다)

```
side-view 2D pixel art for a side-scrolling survival game set in the abandoned underground spaces beneath a Korean town,
flat orthographic side view, muted realistic colors, thin dark outline, slightly worn and damp, no people, no text
```

- 문·장식은 **투명 배경**, 벽 타일은 **불투명 + 좌우가 이어지게**.
- 깊은 곳은 늘 어둡고 손전등으로 본다. 그림 자체는 **중간 밝기**로 만들고 어둠은 게임 조명이 입힌다 (너무 어둡게 그리면 손전등을 비춰도 검다).
- 글자 금지. 비상구 표지는 **달리는 사람 그림만**.

## 1. 세 가지 문 (P1 — 키가 이미 있다)

기획 B.2: 세 문은 **한눈에 달라 보여야** 한다. 깊은 문 = 녹슨 붉은 갈색 + 경고 테이프, 일방 출구 = 초록 비상등 + 가로 손잡이, 바깥 철문 = 손잡이 없는 회색 판.
문 크기는 겉 층 출구(`prop_exit` 46 × 78)에 맞춘다. 게임에서는 1.6 × 3 칸으로 그린다.

| 파일 (키) | 크기 | style | 프롬프트 (공통 문구 뒤) |
|---|---|---|---|
| `prop_deep_door` | 48 × 84, 투명 | `prop_exit` | `A heavy old rusted steel fire door set in a concrete frame, reddish-brown peeling paint with orange rust streaks, a lever handle, diagonal yellow-and-black hazard tape on the frame edges, an unlit dusty emergency lamp box above the door, a thin line of pale blue cold light leaking from the gap at the bottom, standing on the floor, transparent background` |
| `prop_exit_pushbar` | 48 × 84, 투명 | `prop_exit` | `A grey steel emergency exit door seen from inside a dark service corridor, a long horizontal push bar across the middle, a small glowing green emergency exit sign above it showing only a white running-figure pictogram with no letters, scuff marks at the bottom, standing on the floor, transparent background` |
| `prop_exit_blank` | 48 × 84, 투명 | `prop_exit_locked` | `A flat featureless grey steel door flush with a concrete wall, no handle and no keyhole on this side, only a small dented metal plate and four rivets, dried water stains, it clearly cannot be opened from here, standing on the floor, transparent background` |
| `prop_deep_door_flooded` | 48 × 84, 투명 | `prop_deep_door`(1번 결과) | 1번과 같은 문, `muddy water up to the lower third of the door, debris and a floating plastic bottle pressed against it` (장마로 막힘) |
| `prop_deep_door_snow` | 48 × 84, 투명 | `prop_deep_door`(1번 결과) | 1번과 같은 문, `half buried by a drift of packed snow and ice blown in from outside, icicles on the frame` (폭설로 막힘) |

- 막힌 문 두 장은 지금 메시지만 뜨는 자리를 그림으로 보여 준다. 연결은 `ExpeditionDirector`의 깊은 곳 연결 그림 고르는 곳에 `BlockedBy`별 키 한 줄.

## 2. 지하 벽·바닥 타일 (P1)

복도 벽(`wall_corridor` 128 × 144)·바닥 띠(`wall_floor_corridor` 64 × 16)와 같은 크기. **좌우가 맞물려 이어져야** 한다.

| 파일 (키) | 크기 | style | 프롬프트 (공통 문구 뒤) |
|---|---|---|---|
| `wall_deep` | 128 × 144, 불투명 | `wall_corridor` | `A seamless horizontally tileable wall section of an underground concrete service corridor: rough grey cast concrete panels with formwork seams, dark damp stains creeping up from the floor, a painted faded yellow stripe at waist height, a few hairline cracks, left and right edges line up so it repeats` |
| `wall_floor_deep` | 64 × 16, 불투명 | `wall_floor_corridor` | `A seamless horizontally tileable floor strip of a wet bare concrete floor, dark puddle patches, a worn painted yellow line, tileable left and right` |

지역마다 다른 벽 (`wall_deep_{지역}`, 없으면 `wall_deep`):

| 파일 (키) | 지역 (`deep.*` 이름) | 프롬프트 (공통 문구 뒤, 위 `wall_deep`의 크기·이어짐 조건 그대로) |
|---|---|---|
| `wall_deep_residential` | 빌라 지하 주차장 | `the back wall of a small underground parking garage of a Korean villa block: painted grey concrete with a green lower band, white parking bay numbers replaced by plain white stripes (no digits), a fluorescent tube fixture, black tire scuffs` |
| `wall_deep_subway` | 역무 구역·환기 통로 | `a subway station staff-only ventilation passage: white glazed tiles with grimy grout on the upper half, bare concrete below, a large round ventilation grille, cable conduits running along` |
| `wall_deep_mart` | 하역장·냉동 창고 | `a supermarket loading dock basement: corrugated insulated cold-storage panels in off-white with frost at the seams, a red horizontal bumper rail, a dented roll-up shutter edge` |
| `wall_deep_police` | 지하 보관실 | `a police station basement storage room: pale green painted concrete, a row of grey steel shelving silhouettes against the wall, a caged ceiling light, a heavy old radiator` |
| `wall_deep_waterworks` | 배관 갤러리·약품 저장고 | `a water treatment plant pipe gallery: large blue and green painted water pipes running horizontally across the wall with flanges and bolts, valve wheels, condensation drips, faded hazard markings` |

## 3. 천장 배관 띠·비상등 (P2)

| 파일 (키) | 크기 | style | 프롬프트 (공통 문구 뒤) |
|---|---|---|---|
| `deep_ceiling` | 128 × 32, 투명 | `wall_corridor` | `A seamless horizontally tileable ceiling strip of an underground corridor seen from the side: two rusty pipes of different thickness and a metal cable tray with black cables, hung on thin brackets from a concrete slab, left and right edges line up, transparent background below the pipes` |
| `deep_lamp` | 24 × 12, 투명 | `prop_exit` | `A small caged emergency bulkhead lamp mounted under a ceiling, a dim greenish glass lens behind a wire cage, transparent background` |

- 연결: `BuildingView.BuildDeep`의 배관·트레이·받침·비상등 사각형을 그림으로 바꾸고, 그림이 없으면 지금 사각형.

## 4. 지역별 장식 (P2 — 배치 코드가 새로 필요)

깊은 곳에는 아직 장식(`decor_*`)이 없다. 지역마다 두 개씩, 벽 앞 바닥에 놓인다 (수색 지점 아님).

| 파일 (키) | 지역 | 크기 | 프롬프트 (공통 문구 뒤, 모두 `standing on the floor, transparent background`로 끝냄) |
|---|---|---|---|
| `decor_deep_car` | 주택가 | 112 × 48 | `an abandoned small hatchback car parked in a basement garage, dusty, one flat tire, a door left open` |
| `decor_deep_pillar` | 주택가 | 32 × 144 | `a square concrete parking garage pillar with a yellow-and-black striped corner guard, a small round convex mirror` |
| `decor_deep_fan` | 지하철 | 64 × 64 | `a large industrial ventilation fan unit in a steel housing, stopped, dusty blades` |
| `decor_deep_cart` | 지하철 | 56 × 40 | `a subway maintenance hand cart with toolboxes and coiled cable` |
| `decor_deep_pallet` | 마트 | 64 × 48 | `a wooden pallet stacked with shrink-wrapped cardboard boxes, a manual pallet jack under it` |
| `decor_deep_curtain` | 마트 | 48 × 96 | `a strip curtain of hanging clear plastic strips at a cold-storage doorway, frosted at the bottom` |
| `decor_deep_shelf` | 파출소 | 56 × 84 | `a grey steel evidence shelf with sealed brown paper bags and labeled boxes with no readable text` |
| `decor_deep_cage` | 파출소 | 64 × 84 | `a chain-link storage cage section with a padlocked gate, old riot shields leaning inside` |
| `decor_deep_valve` | 정수장 | 48 × 64 | `a large vertical pipe with a big red valve wheel and a round pressure gauge` |
| `decor_deep_drums` | 정수장 | 56 × 48 | `two blue plastic chemical drums and one yellow drum with a hazard diamond pictogram, no text` |

- 연결: `LocationBuilder`의 각 `*Deep`에 장식 두세 개씩 (`exploration_art.json`에 키). 깊은 곳 장식도 기물 안개(`UpdateFog`)·그림자 대상.

## 5. 침수·CD 소품 (P2)

| 파일 (키) | 크기 | style | 프롬프트 (공통 문구 뒤) |
|---|---|---|---|
| `deep_flood_water` | 128 × 28, 투명 | `wall_floor_corridor` | `A seamless horizontally tileable strip of still murky flood water seen from the side, a flat surface line with faint ripples and a pale reflection, brownish-green semi-transparent water below, a floating leaf, left and right edges line up, transparent background above the water` |
| `prop_cd` | 20 × 20, 투명 | `prop_exit` | `a single plastic CD jewel case lying slightly tilted on the floor, the disc and a colorful cover faintly visible, a small glint on the plastic, transparent background` |

- `deep_flood_water`: `ExpeditionDirector.BuildFlood`의 반투명 사각형 자리.
- `prop_cd`: 지금 "CD 그림이 아직 없으면 종이 자리"로 그려지는 곳. 색은 `cd_map.json` `Color`로 물들인다.

## 6. 만드는 순서와 내가 할 일

1. **P1 (문 5 + 벽·바닥 7 = 12장)** 먼저 만든다. 넣는 즉시 화면이 바뀐다 (지역 벽은 키 찾기 한 줄).
2. 후보를 캡처로 비교해 고른다 (밤·손전등 + 낮, 깊은 곳 침수 층 포함).
3. **P2 (천장·비상등 2 + 장식 10 + 침수·CD 2 = 14장)**와 배치 코드.
4. CLAUDE.md "탐험의 깊이" 줄의 "그림은 아직 코드 그림이다"를 고친다.

## 6.1 P1 결과 (2026-11-22)

- 넣은 것: 깊은 문·막힌 문 2(침수·눈)·비상구 안(가로 손잡이)·밖(민짜 철문), 지하 벽 공통 + 지역 5, 지하 바닥 띠.
- 막힌 문은 `ExpeditionDirector.CreateDeepConnector`가 `BlockedBy`(장마·폭설)에 따라 고른다. 지역 벽은 `BuildingView.DeepWallKey`(`wall_deep_{지역}` → `wall_deep`).
- 함께 바꾼 것: **계단 정면 그림** — `prop_stairs_up`(올라가는 계단, 위로 갈수록 어둡게)·`prop_stairs_down`(내려가는 계단, 아래로 갈수록 어둡게)·`prop_stairs_collapsed`(건물 더미로 막힌 계단). 계단참 구멍(폭 약 2.2칸·높이 약 3칸)을 채우는 크기로 만들었다.
- 도구: `code_temp/apogirl/tools/gen_deep.py`(생성), `install_deep.py 키=후보[:flip]`(투명 여백 자르기 + 계단 어둡게), `sheet.py`·`tilesheet.py`(후보 한눈에 보기).

## 7. 만들지 않는 것

- 깊은 곳 이벤트 컷(`evt_deep_door`·`evt_deep_oneway`)은 힉스필드 8차로 이미 받았다 (장면이 생기면 연결).
- 깊은 곳 좀비·사람 스프라이트는 겉 층과 같은 것을 쓴다.
