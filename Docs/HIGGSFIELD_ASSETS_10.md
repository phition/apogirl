# 힉스필드 에셋 10차: 탐험 스프라이트 다듬기 · 공격 프레임 · 타격 연출 (2026-11-23)

탐험 화면 픽셀 아트는 지금까지 PixelLab으로 내가 만들었다(8차 문서 6절 "만들지 않는 것"). 이번에는 **그 그림을 힉스필드로 다시 그려 정교하게** 만든다.
계기는 다음과 같다.

- 대걸레를 들고 웅크려 걸을 때 움직임이 거의 없고, 중간중간 대걸레 머리가 위로 늘어나는 프레임이 섞여 있었다. 같은 문제가 다른 묶음에도 있었다. 아래 0.3에 정리했다.
- 무기 공격이 16프레임 하나로 뭉뚱그려져 있다. 들어 올리기·내려치기·맞는 순간·되돌리기가 분명하지 않다.
- 좀비가 맞았을 때 이펙트가 없다. 맞는 순간도 그림과 맞지 않았다(누르는 즉시 판정됐다). 이번에 코드는 고쳤고, 그림은 이 문서로 받는다.

공통 사항:
- 쓰는 법·모델·공통 공식은 [HIGGSFIELD_ASSETS.md](HIGGSFIELD_ASSETS.md) 0장을 따른다. 아래에서 `[F-PIXEL]`은 0.3의 픽셀 공식, `[안전]`은 0.4의 안전 문구다.
- 프롬프트는 영어, 설명은 한국어. 회색 상자 하나 또는 표의 프롬프트 칸 하나가 생성 하나다.
- 주인공은 기준 시트 `ArtInbox/illustrations/char_seojin_sheet.png`와 **지금 게임 프레임**(아래 1.2의 기준 프레임)을 함께 참고 이미지로 넣는다. 얼굴·머리·몸 비율을 지금 그림과 맞추기 위해서다.

---

## 0. 한눈에 보기

### 0.1 요약표

| 번호 | 내용 | 묶음 수 | 우선 |
|---|---|---|---|
| 1 | 규격·기준 프레임·넣는 법 | — | 먼저 읽기 |
| 2 | **대걸레 고치기**: 웅크려 걷기 6벌, 대기·달리기 다시 | 6 + 3 | **P1** |
| 3 | **무기 공격 다시 그리기**: 4무기 × 6벌 = 24묶음, 20프레임, 맞는 프레임 표시 | 24 | **P1** |
| 4 | **좀비 타격**: 타격 이펙트 3종, 맞음 반응 12프레임, 쓰러짐(사라짐) | 3 + 4 + 4 | **P1** |
| 5 | **PixelLab 그림 다듬기**: 주인공 기본 동작 126묶음, 좀비·생존자·동료 | 126 + 27 + 5 | P2 |
| 6 | **새 동작**: 수색·다침·숨기·헐떡임·잠듦·계단, 맨손 밀치기 | 8 × 6벌 | P2~P3 |

### 0.2 먼저 만들 것

1. **2.1 대걸레 웅크려 걷기**: 지금 가장 눈에 띈다. 경호·진압 두 벌은 임시로 대걸레를 안 든 그림을 쓰고 있다.
2. **4.1 타격 이펙트**: 그림이 오면 바로 붙는다. 코드는 연결해 두었다.
3. **3장 공격**: 무기 하나(야구방망이, 여름 교복)를 먼저 만들어 보내 주면 게임에 넣고 맞는 순간을 맞춰 본 뒤 나머지를 만든다.

### 0.3 이번에 찾은 PixelLab 그림 문제와 임시 조치

자동 점검(떨어진 얼룩 찾기)과 눈으로 보기로 찾았다. **멀쩡한 이웃 프레임으로 임시로 바꿔 두었다**(도구 `code_temp/apogirl/tools/patch_player_frames.py`, 원본은 `code_temp/apogirl/backup_player_frames/`). 그래서 지금은 동작이 조금 덜 부드럽다. 다시 그릴 때 아래를 꼭 피한다.

| 묶음 | 문제 | 지금 임시로 |
|---|---|---|
| `player_uniform_mop_crouch` | 대걸레 머리가 빗처럼 서고 위로 늘어남 (0~11) | 12~15만 돈다 |
| `player_guard_mop_crouch`, `player_riot_mop_crouch` | 한 프레임에 두 사람이 겹쳐 그려짐 (16장 전부) | 맨손 웅크리기 그림 |
| `player_summer_mop_idle` | 대걸레 머리에서 실 가닥이 솟음 (2~11) | 0·1·12~15 |
| `player_uniform_mop_idle` | 떨어진 얼룩 (13~15) | 앞 프레임 |
| `player_riot_mop_run` | 다른 사람이 겹침 (0·6·7·8·14·15) | 이웃 프레임 |
| `player_summer_bat_walk` | 공중에 뜬 선 (10~14) | 이웃 프레임 |
| `player_summer_bat_crouch` | 떨어진 얼룩 (5) | 이웃 프레임 |
| `player_summer_baton_idle`, `player_winter_baton_idle` | 경찰봉 끝의 흰 덩어리·점 | 0·1·15 |
| `player_summer_pipe_idle`, `player_winter_pipe_idle` | 머리 위·파이프 끝 연기 | 이웃 프레임 |
| `player_padding_pipe_run` | 떨어진 얼룩 (5·13) | 이웃 프레임 |

모든 프롬프트에 붙이는 **[NO-GLITCH]** 문구:
```
Exactly one character in every frame, never a second person or a ghost copy. The held object keeps the exact same length and shape in every frame; it never stretches, grows, bends or sprouts strands. No smoke, no sparkles, no floating lines, no stray pixels, no detached blobs.
```

---

## 1. 규격 (이번 문서 전체)

### 1.1 프레임

| 항목 | 값 |
|---|---|
| 프레임 크기 | **96 × 78 픽셀** (게임에 들어가는 크기). 힉스필드에서는 4배(384 × 312) 이상으로 받아도 된다. 내가 줄인다 |
| 화면 크기 | 36 픽셀 = 1미터 (PPU 36). 주인공 키 = 약 62픽셀 |
| 방향 | **오른쪽을 보는 옆모습**. 왼쪽은 게임이 뒤집는다 |
| 발 위치 | 모든 프레임에서 **발바닥이 같은 높이**(그림 아래에서 4픽셀 위). 프레임마다 위아래로 떠다니지 않게 |
| 가로 위치 | 몸통 중심이 프레임 가운데(x = 48)에 오도록. 무기·대걸레가 앞으로 나가는 만큼만 오른쪽으로 길어진다 |
| 배경 | 투명, 또는 마젠타 `#ff00ff` 단색 |
| 가방 | **그리지 않는다.** 가방은 게임이 따로 얹는다(가방 자리는 내가 프레임마다 잰다) |
| 색 | 지금 그림과 같은 팔레트 (참고 이미지의 색을 그대로) |

### 1.2 기준 프레임 (참고 이미지로 넣을 것)

`Assets/_Project/Resources/Art/ExplorationPixel/` 의 아래 파일을 참고 이미지로 함께 넣는다. 옷·무기 모양을 지금 게임과 맞춘다.

| 옷 토큰 | 뜻 | 기준 프레임 |
|---|---|---|
| `summer` | 여름 교복 (방어구 없음, 여름) | `player_summer_none_idle_0.png` |
| `winter` | 겨울 교복 (방어구 없음, 겨울) | `player_winter_none_idle_0.png` |
| `uniform` | 기본 교복 (계절 없는 예비) | `player_uniform_none_idle_0.png` |
| `padding` | 패딩 점퍼 | `player_padding_none_idle_0.png` |
| `guard` | 보호대·조끼 | `player_guard_none_idle_0.png` |
| `riot` | 진압복 | `player_riot_none_idle_0.png` |

| 무기 토큰 | 무기 | 기준 프레임 | 공격 방식 |
|---|---|---|---|
| `none` | 맨손 | — | 밀치기 (`shove`) |
| `bat` | 나무 야구방망이 | `player_summer_bat_idle_0.png` | 머리 위로 들어 내려치기 |
| `pipe` | 쇠파이프 | `player_summer_pipe_idle_0.png` | 머리 위로 들어 내려치기 (무겁고 느리게) |
| `baton` | 경찰봉 | `player_summer_baton_idle_0.png` | 짧고 빠르게 내려치기 |
| `mop` | 대걸레 | `player_summer_mop_idle_0.png` | 창처럼 앞으로 찌르기 |

### 1.3 넣는 법

- **시트 한 장 = 묶음 하나.** 가로 4칸, 왼쪽 위부터 오른쪽으로, 다음 줄로. 칸 사이에 여백을 두지 않는다(시트 크기 = 칸 크기 × 4 × 줄 수).
- 파일 이름: `ArtInbox/sprites10/{묶음 이름}_sheet.png` (예: `player_summer_mop_crouch_sheet.png`).
- 낱장으로 받았으면 `ArtInbox/sprites10/{묶음 이름}/00.png, 01.png …`.
- 받으면 내가 자르고 → 96×78로 줄이고 → 발 높이·가방 자리를 재서(`player_anchors.json`) → `ExplorationPixel/{묶음 이름}_{n}.png`로 넣는다. **게임은 파일 수로 프레임을 세므로** 프레임 수가 문서와 달라도 돈다.

### 1.4 만드는 법

- **기본**: `gpt_image_2_6`에 기준 프레임 + 이 문서 프롬프트로 **시트 한 장**을 만든다. 비율은 시트 칸 수에 맞춘다(16프레임 = 4×4 → 384×312 칸이면 1536×1248, 약 5:4).
- **사람 동작(걷기·달리기)**: `autosprite`로 만들고 결과를 위 규격으로 다듬어도 된다. `--kind walk --frame_count 16 --is_humanoid true --remove_bg default`.
- 한 벌을 먼저 만들고(여름 교복), 나머지 다섯 벌은 **그 시트를 참고 이미지로 넣고 옷만 바꾸게** 한다. 동작이 여섯 벌 모두 같아야 무기·가방 자리가 맞는다.
- 공통 머리말 **[SPRITE-HEAD]** (모든 주인공 프롬프트 앞에):
```
[F-PIXEL] Sprite sheet for a side-scrolling 2D survival game. Grid of equal cells, 4 columns, read left to right then top to bottom, no gaps, no borders, no numbers. Each cell is one animation frame of the same character facing right, feet on the same baseline near the bottom of every cell, body centered horizontally. Transparent background. Match the attached reference frame exactly: same face, same hair, same outfit, same colors, same proportions, same pixel scale. Do not draw a backpack.
```
- 끝에는 늘 `[NO-GLITCH]`와 `[안전]`을 붙인다.

인물 문구 (`{OUTFIT}` 자리):

| 토큰 | 문구 |
|---|---|
| `summer` | `a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers` |
| `winter` | `the same schoolgirl in the winter uniform: a long brown duffle coat over the navy uniform, dark tights and dark sneakers` |
| `uniform` | `the same schoolgirl in a navy school blazer, knee-length navy skirt, dark tights and sneakers` |
| `padding` | `the same schoolgirl in a puffy olive-green padded jacket over her uniform, dark trousers and sneakers` |
| `guard` | `the same schoolgirl in a red and black sports protective vest with elbow and knee guards over a dark tracksuit` |
| `riot` | `the same schoolgirl in an oversized black riot-police style padded suit, dark gloves, no helmet, her own black hair visible` |

---

## 2. 대걸레 고치기 (P1)

### 2.1 웅크려 걷기 `player_{옷}_mop_crouch` (16프레임, 6벌)

게임에서 쓰는 방식: 멈춰 웅크리면 0번 프레임, 웅크려 걸으면 16프레임을 돈다(초당 약 8프레임). **멈춘 0번 프레임도 자연스러워야 한다.**

```
[SPRITE-HEAD] 16 frames (4 x 4). {OUTFIT}, sneaking forward in a low crouch, knees deeply bent, back low, one careful step after another: frames 1-8 left foot steps forward, frames 9-16 right foot steps forward, clearly visible leg movement and a small up-down body bob. She holds a wooden mop low and level in both hands, the mop pole pointing forward and slightly down, the mop head resting just above the floor in front of her. The mop is rigid: the pole length and the grey-white mop head stay exactly the same size and shape in every frame, the mop head is a compact bundle of cotton strands hanging down, never fanned out, never pointing up. [NO-GLITCH] [안전]
```

| 묶음 | `{OUTFIT}` | 참고 이미지 |
|---|---|---|
| `player_summer_mop_crouch` | `summer` | `player_summer_mop_idle_0.png`, `player_summer_none_crouch_0.png` |
| `player_winter_mop_crouch` | `winter` | 여름 결과 시트 + `player_winter_mop_idle_0.png` |
| `player_uniform_mop_crouch` | `uniform` | 여름 결과 시트 + `player_uniform_mop_idle_0.png` |
| `player_padding_mop_crouch` | `padding` | 여름 결과 시트 + `player_padding_mop_idle_0.png` |
| `player_guard_mop_crouch` | `guard` | 여름 결과 시트 + `player_guard_mop_idle_0.png` |
| `player_riot_mop_crouch` | `riot` | 여름 결과 시트 + `player_riot_mop_idle_0.png` |

### 2.2 대걸레 대기·달리기 다시 (깨진 묶음만)

**대기** `player_summer_mop_idle`, `player_uniform_mop_idle` (16프레임):
```
[SPRITE-HEAD] 16 frames (4 x 4). {OUTFIT}, standing idle and breathing slowly, holding a wooden mop diagonally in both hands with the mop head resting on the floor in front of her; only a gentle shoulder rise and fall and a tiny weight shift, a slow loop where frame 16 flows back into frame 1. The mop never moves on its own: same length, same mop head shape on the floor in every frame. [NO-GLITCH] [안전]
```

**달리기** `player_riot_mop_run` (16프레임):
```
[SPRITE-HEAD] 16 frames (4 x 4). {OUTFIT}, running forward, two full strides (frames 1-8 and 9-16), arms pumping, holding a wooden mop at her side angled forward and down, the mop head trailing just above the floor. [NO-GLITCH] [안전]
```

---

## 3. 무기 공격 다시 그리기 (P1)

### 3.1 무엇이 바뀌나

- 지금: 16프레임, 동작 단계가 흐릿하다. 게임은 0.5초에 다 돈다.
- 새로: **20프레임**, 단계가 분명하게. 게임은 그대로 0.5초(초당 40프레임)에 돈다.
- **맞는 프레임**: 무기가 가장 앞·아래로 닿는 프레임. 게임은 이 순간에 판정하고 타격 이펙트를 띄운다. 값은 `Resources/Art/exploration_art.json`의 `Combat.ImpactAt`(공격 진행도 0~1)에 적혀 있고, 새 그림이 오면 내가 고친다.

| 단계 | 프레임 (20장 기준) | 내용 |
|---|---|---|
| 준비 | 1~5 | 무기를 들어 올리거나(내려치기) 뒤로 당긴다(찌르기). 무게중심이 뒤로 |
| 휘두르기 | 6~9 | 빠르게 움직인다. 프레임 사이 간격이 크다 |
| **맞음** | **10~11** | 무기가 앞쪽 가슴~허리 높이에 닿는다. 몸이 가장 앞으로 쏠린다. 이 두 장은 거의 같은 자세(타격 멈춤 느낌) |
| 뒤따름 | 12~15 | 무기가 아래로 더 내려가거나 끝까지 뻗는다 |
| 되돌리기 | 16~20 | 처음 서 있는 자세로 돌아온다. 20번은 대기 0번과 거의 같게 |

→ 새 `ImpactAt` = 10/20 = **0.5** (찌르기는 9/20 = 0.45로 할 수도 있다).

### 3.2 프롬프트 (무기별 동작, 옷 6벌 × 무기 4 = 24묶음)

묶음 이름: `player_{옷}_{무기}_attack` (예: `player_summer_bat_attack`).

**야구방망이 `bat`**
```
[SPRITE-HEAD] 20 frames (4 columns x 5 rows). {OUTFIT}, an overhead two-handed strike with a wooden baseball bat: frames 1-5 she raises the bat high above and slightly behind her head, weight on the back foot; frames 6-9 a fast downward arc forward; frames 10-11 impact pose, the bat horizontal in front of her at chest height, arms extended, front knee bent, body leaning forward; frames 12-15 follow-through, the bat continues down to knee height; frames 16-20 she recovers to a ready stance holding the bat diagonally. The bat keeps exactly the same length and shape in every frame. [NO-GLITCH] [안전]
```

**쇠파이프 `pipe`** — 무겁게: 준비가 길고 되돌리기가 느리다
```
[SPRITE-HEAD] 20 frames (4 columns x 5 rows). {OUTFIT}, a heavy overhead strike with a long rusty steel pipe held in both hands: frames 1-6 she slowly heaves the heavy pipe up over her shoulder, leaning back to balance its weight; frames 7-9 the pipe comes down in a heavy forward arc; frames 10-11 impact pose, pipe horizontal at chest height in front of her, her whole body behind the blow; frames 12-15 the weight of the pipe pulls it down close to the floor, she staggers half a step; frames 16-20 she drags it back up to a ready stance. Same pipe length and thickness in every frame. [NO-GLITCH] [안전]
```

**경찰봉 `baton`** — 짧고 빠르게: 한 손
```
[SPRITE-HEAD] 20 frames (4 columns x 5 rows). {OUTFIT}, a quick one-handed overhead strike with a short black police baton: frames 1-4 she raises the baton beside her head, other arm forward for balance; frames 5-9 a snapping downward strike; frames 10-11 impact pose, the baton level in front of her chest, arm fully extended; frames 12-14 short follow-through to waist height; frames 15-20 she pulls the baton back to a guarded stance near her shoulder. Same baton length in every frame. [NO-GLITCH] [안전]
```

**대걸레 `mop`** — 창처럼 찌르기
```
[SPRITE-HEAD] 20 frames (4 columns x 5 rows). {OUTFIT}, a two-handed forward thrust with a wooden mop used like a spear: frames 1-5 she pulls the mop back along her side, rear hand at her hip, mop head pointing forward at waist height; frames 6-8 she lunges forward, front foot stepping out; frames 9-11 full extension, arms straight, the mop head driven forward at chest height as far as it reaches, body leaning into the lunge; frames 12-14 she holds, then pulls back; frames 15-20 she returns to the idle stance with the mop head resting on the floor. The mop pole and the compact grey-white mop head keep exactly the same size and shape in every frame; the strands hang naturally and never fan out or stretch. [NO-GLITCH] [안전]
```

| 무기 | `ImpactAt` 지금 (16장) | 새 그림 (20장) |
|---|---|---|
| bat·pipe·baton | 0.5 (8번째 프레임) | 0.5 (10번째) |
| mop | 0.38 (6번째) | 0.45 (9번째) |

### 3.3 맨손 밀치기 `player_{옷}_none_shove` (20프레임, 6벌)

```
[SPRITE-HEAD] 20 frames (4 columns x 5 rows). {OUTFIT}, empty-handed, a desperate two-handed shove: frames 1-5 she draws both hands back to her chest and plants her back foot; frames 6-9 she pushes forward hard; frames 10-11 contact pose, both arms fully extended at chest height, palms forward, body leaning in; frames 12-15 she stumbles a small step back from the recoil; frames 16-20 she returns to a wary standing stance. [NO-GLITCH] [안전]
```
밀치기는 누르는 즉시 판정된다(맞는 프레임 = 10~11은 그림만 맞춘다). 바꾸려면 말해 줘.

---

## 4. 좀비 타격 (P1)

### 4.1 타격 이펙트 `fx_hit_` (6프레임, 48 × 48, 이미 연결됨)

- 맞는 순간 좀비 몸의 주인공 쪽, 가슴 높이에 한 번 돈다(초당 24프레임 = 0.25초). 빛을 받지 않아 어두운 곳에서도 보인다.
- 오른쪽(주인공 → 좀비가 오른쪽)을 기준으로 그린다. 왼쪽은 뒤집는다.
- 그림이 없으면 지금은 코드 그림(흰 십자 번쩍임)이 나온다.
- 피·살점은 없다. **충격 번쩍임 + 먼지**로만.

```
[F-PIXEL] Small one-shot impact effect sprite sheet, 6 frames in a row, each cell 48x48, transparent background. A blunt hit spark seen from the side: frame 1 a tiny bright white-yellow point, frame 2 a sharp four-point star burst with short white speed lines, frame 3 the largest burst with a pale ring, frames 4-5 the burst breaks into a few grey dust specks drifting outward to the left and right, frame 6 faint dust fading out. Off-white, pale yellow and light grey only. No blood, no red, no gore, no text. [안전]
```

같은 방식으로 두 가지를 더 받으면 무기별로 나눠 쓴다(지금 코드는 `fx_hit_` 하나만 쓴다. 나누는 연결은 그림이 오면 내가 한다).

| 파일 | 쓰임 | 바꿀 문구 |
|---|---|---|
| `fx_hit_heavy_` (8프레임, 64 × 64) | 쇠파이프·야구방망이 | `a heavier, wider burst with a short ground shockwave line under it` |
| `fx_hit_thrust_` (6프레임, 48 × 48) | 대걸레 찌르기 | `a narrow forward-pointing burst, like a poke, with water droplets instead of sparks` |

### 4.2 맞음 반응 `zombie_{종류}_hit_` (12프레임, 지금 8프레임)

- 지금은 맞는 순간 0.6초 동안 돈다. 반응하는 동안 좀비는 주인공 쪽을 본다.
- 좀비 3종 + 생존자(`survivor_hit_`, 사람이다: 아파하는 동작만, 좀비 같은 몸짓 없이).

| 묶음 | 기준 프레임 |
|---|---|
| `zombie_wanderer_hit` | `zombie_wanderer_idle_0.png` |
| `zombie_listener_hit` | `zombie_listener_idle_0.png` |
| `zombie_watcher_hit` | `zombie_watcher_idle_0.png` |
| `survivor_hit` | `survivor_idle_0.png` |

```
[F-PIXEL] Sprite sheet for a side-scrolling 2D survival game, 12 frames (4 columns x 3 rows), equal cells 96x78, no gaps, feet on the same baseline, facing right, transparent background. Match the attached reference frame exactly (same figure, clothes, colors, pixel scale). The figure is struck from the front: frame 1 the moment of impact, head and upper body snap backward; frames 2-4 the body recoils, staggering one step back, arms flung loosely; frames 5-7 it sways and almost loses balance; frames 8-12 it slowly straightens and leans forward again toward the attacker, ending in its normal idle pose. Weight and recoil shown through posture only. Exactly one figure per frame. No blood, no wounds, no gore. [안전]
```

### 4.3 쓰러짐 `zombie_{종류}_down_` (12프레임, 새로)

지금 쓰러진 좀비는 그 순간 화면에서 지운다(고어 지양, 문서 13-4). 그 사이를 이 그림으로 잇는다. **무너진 뒤 그림자처럼 흩어져 사라지는** 동작이다. 연결(쓰러짐을 다 보여 준 뒤 지우기)은 그림이 오면 내가 한다.

```
[F-PIXEL] Sprite sheet, 12 frames (4 columns x 3 rows), equal cells 96x78, facing right, transparent background, matching the attached reference figure. The infected figure collapses: frames 1-3 knees buckle and it drops to its knees; frames 4-6 it falls forward onto the floor; frames 7-12 the fallen shape turns into a flat dark silhouette and dissolves into drifting grey dust until nothing remains. No blood, no wounds, no body parts, no gore; it simply fades away like ash. Exactly one figure. [안전]
```

묶음: `zombie_wanderer_down`, `zombie_listener_down`, `zombie_watcher_down`. 생존자는 쓰러지지 않는다(만들지 않는다).

---

## 5. PixelLab 그림 다듬기 (P2)

규칙·문구는 2~4장과 같다. 한 벌(여름 교복)을 먼저 만들고 나머지는 그 시트를 참고로 옷만 바꾼다. 아래 프레임 수는 지금 게임과 같다(달라도 돈다).

### 5.1 주인공 기본 동작 (옷 6벌 × 아래)

| 동작 | 묶음 이름 | 프레임 | 게임 속도 | 프롬프트 동작 문구 |
|---|---|---|---|---|
| 대기 | `player_{옷}_{무기}_idle` (무기 5종) | 16 | 느린 반복 | `standing idle, breathing slowly, a slow seamless loop, holding {WEAPON} relaxed` |
| 걷기 | `player_{옷}_{무기}_walk` | 16 | 보통 | `walking cautiously, two full steps (frames 1-8, 9-16), holding {WEAPON}` |
| 달리기 | `player_{옷}_{무기}_run` | 16 | 빠름 | `running, two full strides, arms pumping, holding {WEAPON}` |
| 웅크려 걷기 | `player_{옷}_{무기}_crouch` | 16 | 느림 | 2.1과 같은 문구에서 무기만 바꾼다. **0번 = 멈춘 웅크림** |

`{WEAPON}` 문구:

| 토큰 | 문구 |
|---|---|
| `none` | `nothing, empty hands held close to her body` |
| `bat` | `a wooden baseball bat in her right hand, resting on her shoulder or hanging at her side` |
| `pipe` | `a long rusty steel pipe in both hands, pointing down at an angle` |
| `baton` | `a short black police baton in her right hand at her side` |
| `mop` | `a wooden mop held diagonally in both hands, mop head near the floor in front of her` |

= 6벌 × 5무기 × 4동작 = **120묶음** + 밀치기 6 = 126 (공격 24는 3장).

### 5.2 좀비·생존자 (지금 그림 다듬기)

| 묶음 | 프레임 | 문구 핵심 |
|---|---|---|
| `zombie_{종류}_idle` | 16 | `swaying slowly in place, slack shoulders` |
| `zombie_{종류}_walk` | 16 | `shambling forward, dragging steps` |
| `zombie_{종류}_chase` | 16 | `lurching forward fast, arms reaching, off-balance` |
| `zombie_{종류}_stun` | 16 | `dazed, swaying, hands drooping, head lolling` |
| `zombie_{종류}_dormant` | 16 | `standing asleep, head drooped, barely moving` |
| `zombie_{종류}_sleep` | 16 | `sitting slumped against the wall, asleep, very slow breathing` |
| `zombie_{종류}_pushed` | 8 | `shoved backward, stumbling two steps back and regaining balance` |
| `survivor_idle`·`walk`·`run`·`stun`·`pushed` | 16·16·16·16·8 | 사람 생존자. 좀비 몸짓 없이 |

종류 문구 (기존 [HIGGSFIELD_ASSETS.md](HIGGSFIELD_ASSETS.md) 2.3과 같다):
- `wanderer`: `slow shambling infected adult in torn office clothes, hunched, greyish-green skin, face in shadow, not gory`
- `listener`: `blind infected in a dirty hospital gown, head tilted to listen, very thin, eyes covered by hair, not gory`
- `watcher`: `tall infected in a long dark coat, unusually upright posture, faint pale eyes visible, slow deliberate movement, not gory`

= 3종 × 7 + 생존자 5 + (4.2·4.3은 4장) = 26묶음.

### 5.3 탐험 속 동료 (숨은 자세, 16프레임)

`npc_dohyeon_hiding`, `npc_nurse_hiding`, `npc_bystander_sitting`, `npc_ringleader_sitting`, `npc_mart_adults_idle` — 문구는 [HIGGSFIELD_ASSETS.md](HIGGSFIELD_ASSETS.md) 2.3 표의 custom 설명 그대로, 프레임만 16(느린 숨쉬기 반복).

---

## 6. 새 동작 (P2~P3)

지금 게임에 그림이 없어서 대기·웅크림으로 대신하는 동작이다. **연결 코드는 아직 없다.** 그림이 오면 내가 잇는다(이름 = `player_{옷}_{무기}_{동작}_{n}`, 없으면 지금처럼). 무기는 `none` 한 벌만 먼저 만들고, 무기를 든 채 필요한 것은 표에 적었다.

| 동작 | 묶음 | 프레임 | 쓰일 때 | 무기 | 프롬프트 동작 문구 |
|---|---|---|---|---|---|
| 수색 | `search` | 12 반복 | 상자·사물함을 길게 뒤질 때 | none만 (무기는 등 뒤로 치운 것으로 본다) | `kneeling and rummaging through a cabinet in front of her, both hands moving, glancing over her shoulder now and then` |
| 다침 | `hurt` | 8 | 좀비에게 물리거나 맞았을 때 | 6벌 × 5무기 | `flinching from a hit on her shoulder, stumbling back a step, then steadying herself, holding {WEAPON}` |
| 숨기 들어가기 | `hide` | 8 | Space를 길게 눌러 숨을 때 (끝 프레임 = 숨은 자세) | none | `ducking down and curling up small behind cover, hugging her knees, head low` |
| 헐떡임 | `exhausted` | 12 반복 | 스태미너가 바닥났을 때 | 6벌 × 5무기 | `bent over with hands on her knees, catching her breath, shoulders heaving, holding {WEAPON}` |
| 잠듦 | `doze` | 12 반복 | 숨어 있다가 잠들었을 때 | none | `curled up in hiding, nodding off, head slowly dropping and jerking back up` |
| 계단 오르기 | `stairs_up` | 12 | 계단을 오를 때 (지금은 바로 옮겨 감) | none | `walking up stairs seen from the side, climbing step by step, one hand on a railing` |
| 계단 내리기 | `stairs_down` | 12 | 계단을 내릴 때 | none | `walking down stairs seen from the side, careful steps, one hand on a railing` |
| 문 열기 | `open` | 8 | 방 문·출구를 열 때 | none | `reaching forward and pulling a door handle, leaning back slightly` |

> 계단 오르기·내리기는 지금 게임이 계단에서 바로 옮겨 가므로, 쓰려면 짧은 연출(0.4초)을 새로 넣어야 한다. 넣을지는 그림이 오면 함께 정한다.

`[결정 필요]` **다침·헐떡임을 무기 5종 모두 만들까?**
- (가) 무기 5종 모두 (6벌 × 5 = 30묶음씩) — 그림이 가장 맞지만 많다.
- (나) **맨손 한 벌만 만들고, 무기를 든 채 다치면 맨손 그림 위에 무기를 겹쳐 그린다** — 추천. 겹치기 코드가 필요하지만 그림 수가 1/5이다.
- (다) 맨손만 만들고 무기를 든 동안엔 그 동작을 쓰지 않는다 — 가장 적다.

---

## 7. 체크리스트

| 장 | 묶음 | 시트 | 우선 |
|---|---|---|---|
| 2.1 대걸레 웅크려 걷기 | 6 | 6 | **P1** |
| 2.2 대걸레 대기·달리기 | 3 | 3 | **P1** |
| 3.2 무기 공격 | 24 | 24 | **P1** (bat·summer 하나 먼저) |
| 3.3 밀치기 | 6 | 6 | P1 |
| 4.1 타격 이펙트 | 1 (+2) | 3 | **P1** |
| 4.2 맞음 반응 | 4 | 4 | **P1** |
| 4.3 쓰러짐 | 3 | 3 | P1 |
| 5.1 주인공 기본 | 120 | 120 | P2 |
| 5.2 좀비·생존자 | 26 | 26 | P2 |
| 5.3 동료 숨은 자세 | 5 | 5 | P3 |
| 6 새 동작 | 8 (+결정에 따라) | 8~ | P2~P3 |

## 8. 이번에 코드에서 바꾼 것 (참고)

- **맞는 순간**: 무기 공격은 누르는 즉시가 아니라 그림에서 무기가 닿는 순간에 판정한다 (`ExpeditionDirector.UpdateStrike`·`ResolveStrike`). 순간은 `exploration_art.json`의 `Combat.ImpactAt`(무기별 공격 진행도 0~1: 방망이·파이프·경찰봉 0.5, 대걸레 0.38). 그 사이에 숨거나 무기가 부서지면 헛친다.
- **타격 이펙트**: 맞는 순간 좀비 몸의 주인공 쪽 가슴 높이에 `HitFx`가 한 번 돈다. `fx_hit_0..n`이 있으면 그 그림, 없으면 코드 그림(흰 십자 번쩍임 0.14초). 빛을 받지 않는 머티리얼 `Resources/Art/exploration_fx_unlit.mat`. 크기·높이·속도는 `Combat.FxScale`·`FxHeight`·`FxFramesPerSecond`.
- **임시 땜질**: 0.3의 깨진 프레임을 이웃 프레임으로 바꿨다. 다시 그린 그림이 오면 덮어쓴다.
