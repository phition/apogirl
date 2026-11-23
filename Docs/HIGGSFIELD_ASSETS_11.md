# 힉스필드 에셋 11차: 탐험 스프라이트 프레임 늘리기 — 샘플 1장 (2026-11-23)

지금 탐험 주인공 그림(PixelLab 전투 스프라이트, 에셋 10차 뒤)은 생김새가 좋다. 하지만 프레임이 적어 움직임이 뚝뚝 끊긴다.
그래서 **지금 그림을 기준(키프레임)으로 두고, 그 사이를 채워 20~24프레임으로** 다시 받으려 한다.

이번 문서는 **샘플 시트 한 장**만 받는다. 결과를 게임에 넣어 보고 괜찮으면 나머지 묶음의 프롬프트 문서를 이어서 쓴다.

- 쓰는 법·모델·공통 공식은 [HIGGSFIELD_ASSETS.md](HIGGSFIELD_ASSETS.md) 0장, 규격은 [HIGGSFIELD_ASSETS_10.md](HIGGSFIELD_ASSETS_10.md) 1장과 같다. 다만 이 문서의 프롬프트는 **붙여 넣기만 하면 되게** 공통 문구를 모두 풀어 적었다.
- 작업 제출은 사용자가 한다. 나(Claude)는 문서만 쓴다.

---

## 1. 샘플로 고른 묶음: 야구방망이 공격 (여름 교복)

| 항목 | 값 |
|---|---|
| 묶음 | `player_summer_bat_attack` |
| 지금 | **8프레임** (PixelLab). 다른 동작(대기·걷기·달리기)은 16프레임이다 |
| 새로 | **24프레임** — 지금 8장을 그대로 키프레임으로 두고, 키프레임 사이에 2장씩 채운다 |
| 고른 까닭 | 프레임이 가장 적어 끊김이 가장 눈에 띄고, 게임에서 가장 자주 보는 동작이다. 지금 8장이 좋은 키프레임 역할을 한다 |

### 1.1 지금 8장 (키프레임)

| 키 | 지금 파일 | 자세 |
|---|---|---|
| K1 | `_0` | 방망이를 머리 위·뒤로 들어 올림, 뒷발에 무게 |
| K2 | `_1` | 더 높이·뒤로 젖힘 (가장 높이 든 순간) |
| K3 | `_2` | 내려치기 시작, 방망이가 머리 위에서 앞으로 |
| K4 | `_3` | 방망이가 빠르게 앞·아래로, 몸이 앞으로 |
| K5 | `_4` | **맞는 순간**: 크게 내딛고 몸을 앞으로 던짐, 방망이가 앞쪽 허리 높이 |
| K6 | `_5` | 뒤따름: 방망이가 앞으로 뻗음, 낮은 자세 |
| K7 | `_6` | 뒤따름: 팔을 앞으로 다 뻗음 |
| K8 | `_7` | 허리춤 마무리 자세 (게임은 이 자세를 잠깐 붙잡은 뒤 대기로 돌아간다) |

### 1.2 새 24장 배치

키프레임은 시트의 **1·4·7·10·13·16·19·22번 칸**에 그대로 둔다. 그 사이 두 칸이 새로 그리는 중간 프레임이다. 23·24번은 마무리 자세에서 아주 조금 숨을 고르는 2장이다.

| 시트 칸 | 내용 |
|---|---|
| 1 | K1 그대로 |
| 2–3 | K1 → K2 사이 (천천히 더 들어 올림) |
| 4 | K2 그대로 |
| 5–6 | K2 → K3 사이 (잠깐 멈췄다가 내려치기 시작) |
| 7 | K3 그대로 |
| 8–9 | K3 → K4 사이 (빠르게: 방망이 위치가 크게 바뀐다) |
| 10 | K4 그대로 |
| 11–12 | K4 → K5 사이 (빠르게) |
| **13** | **K5 그대로 = 맞는 프레임** |
| 14–15 | K5 → K6 사이 (타격 뒤 아주 작은 멈춤 느낌, 거의 같은 자세) |
| 16 | K6 그대로 |
| 17–18 | K6 → K7 사이 |
| 19 | K7 그대로 |
| 20–21 | K7 → K8 사이 |
| 22 | K8 그대로 |
| 23–24 | K8에서 어깨가 살짝 내려가고 숨을 고름 (발 위치는 그대로) |

→ 게임에 넣으면 맞는 순간은 13번째 칸이라 `Combat.ImpactAt`의 bat `At` = 12/24 = **0.5**로 지금과 같다. 프레임 차례 `Frames`는 내가 새로 맞춘다.

---

## 2. 넣을 참고 이미지 (3장)

| 순서 | 파일 | 왜 |
|---|---|---|
| 1 | `ArtInbox/sprites11/ref/player_summer_bat_attack_ref8.png` | **지금 8프레임을 가로로 이어 붙인 것** (4배 확대, 투명 배경). 키프레임 자세·그림체의 기준이다. 내가 만들어 두었다 |
| 2 | `ArtInbox/sprites11/ref/player_summer_bat_idle_ref.png` | 지금 대기 0번 프레임 (4배). 얼굴·머리·교복·방망이 모양의 기준이다 |
| 3 | `ArtInbox/illustrations/char_seojin_sheet.png` | 주인공 서진 기준 시트. 얼굴·비율 보조용이다 (그림이 흔들리면 빼도 된다) |

---

## 3. 규격

| 항목 | 값 |
|---|---|
| 모델 | `gpt_image_2_6` (시트 한 장 생성) |
| 시트 | **가로 6칸 × 세로 4줄 = 24칸**, 칸 사이 여백·테두리·번호 없음 |
| 칸 크기 | 384 × 312 (게임 프레임 96 × 78의 4배). 시트는 2304 × 1248, **비율 16:9**로 받는다. 크기가 조금 달라도 내가 칸을 잘라 맞춘다 |
| 읽는 순서 | 왼쪽 위 → 오른쪽, 다음 줄로 |
| 방향 | 오른쪽을 보는 옆모습 |
| 발 | 모든 칸에서 발바닥 높이가 같다(내딛는 발도 같은 바닥선 위). 칸 안에서 위아래로 떠다니지 않게 |
| 배경 | 투명. 안 되면 마젠타 `#ff00ff` 단색 |
| 가방 | 그리지 않는다 (게임이 따로 얹는다) |
| 저장 | `ArtInbox/sprites11/player_summer_bat_attack_sheet.png` |

---

## 4. 프롬프트 (그대로 붙여 넣기)

```
Sprite sheet for a side-scrolling 2D survival game, side-view 2D pixel art, crisp chunky pixels, limited muted palette, 1-pixel dark outline, no anti-aliasing, no blur, orthographic side view.
Grid of 24 equal cells: 6 columns x 4 rows, read left to right then top to bottom, no gaps, no borders, no numbers, no text. Each cell is one animation frame of the same character facing right, feet on the same floor baseline near the bottom of every cell, body centered horizontally. Transparent background.

Character: a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers, holding a wooden baseball bat in both hands. Match the attached reference frames exactly: same face, same hair, same outfit, same colors, same shading style, same proportions, same pixel scale, same bat. Do not draw a backpack.

Animation: one overhead two-handed bat strike, smoothed from the attached 8-frame reference strip. Keep the 8 reference poses unchanged as keyframes in cells 1, 4, 7, 10, 13, 16, 19 and 22, and draw two in-between frames between each pair of keyframes so the motion is smooth:
cells 1-4: she lifts the bat higher above and behind her head, weight shifting onto the back foot;
cells 4-7: a short wind-up pause, then the bat starts to come forward over her head;
cells 7-13: a fast downward arc, the bat position changes a lot between frames, ending in cell 13 at the impact pose - a big forward step, body thrown forward, the bat in front of her at waist height;
cells 13-16: a tiny impact hold, almost the same pose;
cells 16-22: follow-through, arms extending fully forward, settling into the low finishing pose at her waist;
cells 23-24: she holds the finishing pose and her shoulders relax slightly, feet do not move.

Exactly one character in every frame, never a second person or a ghost copy. The bat keeps the exact same length and shape in every frame; it never stretches, grows or bends. No motion-blur smears, no speed lines, no smoke, no sparkles, no floating lines, no stray pixels, no detached blobs.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

**잘 안 될 때**
- 칸 수가 틀리거나 순서가 섞이면: 첫 줄 `Grid of 24 equal cells`를 `Exactly 24 frames in a 6 x 4 grid`로 바꿔 다시.
- 그림체가 지금과 달라지면(매끈한 일러스트처럼): 참고 이미지 3번(서진 시트)을 빼고 1·2번만 넣는다.
- 24칸이 계속 어려우면 **20칸(가로 5 × 세로 4)** 으로 줄인다: 키프레임 칸을 `1, 4, 7, 10, 12, 14, 17, 20`으로 바꾸고, 맞는 프레임은 10번 칸이 된다(`At` = 0.45~0.5로 내가 맞춘다).

---

## 5. 받은 뒤 (내가 할 일)

1. 칸을 잘라 96 × 78로 줄이고, 발을 바닥 줄에 붙인다 (에셋 10차 도구 `code_temp/apogirl/tools/pl_swing_install.py`와 같은 방식). 옛 8장은 `code_temp/apogirl/backup_attack8/`로 옮겨 둔다.
2. `exploration_art.json`의 bat `Frames`를 24장에 맞추고 `At`을 맞는 칸에 맞춘다.
3. 게임에서 휘둘러 보고 아래를 함께 본다.
   - 지금 그림과 얼굴·옷·방망이가 같아 보이는가
   - 끊김 없이 부드러운가, 프레임 사이에 튀는 칸은 없는가
   - 맞는 순간과 타격 이펙트가 맞는가
4. 괜찮으면 다음 문서에서 **나머지 묶음**(공격: 옷 6벌 × 방망이·파이프·경찰봉, 그리고 16프레임인 대기·걷기·달리기·웅크리기)을 같은 방식으로 20~24프레임으로 늘리는 프롬프트를 쓴다.
