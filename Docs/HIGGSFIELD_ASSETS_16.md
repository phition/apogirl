# 힉스필드 에셋 16차: PixelLab 몫이던 탐험 주인공 그림 · 잠든 좀비 소리 (2026-11-25)

> 이 문서는 `python Tools/make_higgsfield_doc16.py`로 만든다. 고칠 때는 스크립트를 고치고 다시 돌린다.

PixelLab에 보내려고 아껴 둔 탐험 그림을 **힉스필드로 받는** 문서다. 크레딧 문제로 미룬 두 가지를 옮겼다.

- [PIXELLAB_24F_PLAN.md](PIXELLAB_24F_PLAN.md) 9장: 주인공 나머지 5벌(동복·겨울·보호대·패딩·진압복)을 **무기 겹치기 방식**(결정 C5 ②)으로 다시 받기.
- [PIXELLAB_CRAFTING_REQUEST.md](PIXELLAB_CRAFTING_REQUEST.md) 4절: 몽둥이 3종(결정 E3 ② — 무기 겹치기 때 무기 그림만).
- **소리 1개 다시 받기** (사용자 요청 2026-11-25): 잠든 좀비 소리가 코 고는 것처럼 들려, 낮게 그르렁거리는 소리로 — 7절.
- 작업 제출은 사용자가 한다. 나(Claude)는 문서·입력 시트·프롬프트 파일만 만들었다.

## 0. 방식 — 하복 그림의 옷만 바꾼다

11차(힉스필드로 24장을 새로 그림)는 걷기·달리기 다리 박자가 고르지 않아 되돌렸다. 그래서 이번에는 **새로 그리게 하지 않는다.**

- **입력 그림 = 하복 그림.** 하복은 PixelLab으로 다시 받아 자세·박자·밝기가 가장 좋다(2배, 192 × 156). 그 프레임을 시트로 이어 붙여 입력으로 준다.
- **바꾸는 것은 하나만.** 옷만 바꾸거나(B·C), 무기만 지운다(A). 자세·다리 박자·크기·바닥 줄은 하복 그대로라 옷 6벌이 똑같이 움직인다(24F 계획 C4의 목표).
- **무기는 따로 한 장씩 받는다.** 몸은 빈손으로 쥔 자세만 받고, 무기 그림을 손 자리에 겹친다(C5 ②). 그래서 몽둥이 3종은 무기 그림 3장으로 옷 6벌 모두에 붙는다.
- 걷기·달리기·웅크려 걷기는 하복도 16장(수색 12장)이라 그 장수 그대로 받는다. 게임은 장수에 맞춰 돈다.

| 단계 | 무엇 | 장수 | 언제 |
|---|---|---|---|
| **A** 시범 | 무기 그림 7장 + 하복 쥔 자세·무기 지운 공격 14시트 | 21 | 먼저. 받으면 내가 겹치기 코드를 만들어 지금 하복 그림과 나란히 비교한다 |
| **B** 옷 5벌 — 겹치기와 무관 | 맨손 10동작 + 대걸레 찌르기 | 55 | 아무 때나 (A와 같이 해도 된다) |
| **C** 옷 5벌 — 겹치기용 | 쥔 자세 6동작 × 2 + 무기 지운 공격 2 | 70 | **A 시범이 괜찮다고 정한 뒤** (C의 입력은 A의 결과다) |
| 합계 | | **146** | |
| 소리 | 잠든 좀비 그르렁 (7절) | 1 | 아무 때나 |

## 1. 규격

| 항목 | 값 |
|---|---|
| 모델 | `gpt_image_2_6` (11차와 같음). 자세가 바뀌어 나오면 Nano Banana Pro로 다시 |
| 넣는 그림 | ① `input/{입력}_input.png` (입력 시트) ② (옷 바꾸기만) `ref/outfit_{옷}.png` |
| 비율 | 표의 비율 (24장 6 × 4 = 16:9, 16장 4 × 4 = 5:4, 12장 4 × 3 = 3:2) — 입력 시트와 같은 칸 배치로 |
| 배경 | 투명. 안 되면 마젠타 `#ff00ff` 단색 |
| 가방 | 그리지 않는다 (게임이 따로 얹는다) |
| 저장 | `ArtInbox/sprites16/{묶음}_sheet.png`, 무기는 `ArtInbox/sprites16/weapons/weapon_{토큰}.png` |

- 파일은 스크립트가 만들어 두었다 (`ArtInbox/sprites16/`, git 밖): 입력 시트 `input/`, 참고 `ref/`, 프롬프트 `prompts/{묶음}.txt`(그대로 붙여 넣는다).
- C 단계 입력은 A에서 받은 시트다: `ArtInbox/sprites16/player_summer_grip1_walk_sheet.png` 같은 파일을 입력 ①로 넣는다.
- 옷 참고 그림 `ref/outfit_*`은 지금 게임 그림(11차, 1배)이라 어둡다. 프롬프트가 "모양·색만 따르고 밝기·선명함은 입력처럼"이라고 적었다.

**잘 안 될 때**
- 칸 수가 틀리거나 자세가 바뀌면: 같은 프롬프트로 한 번 더, 그래도 안 되면 모델을 Nano Banana Pro로.
- 몇 칸만 어긋나면(두 사람 겹침·무기가 남음·칸 밖으로 밀림) 그 칸 번호만 알려 줘도 된다. 내가 이웃 칸이나 하복 칸으로 메운다.
- 무기 지우기(A)에서 손이 펴지면 다시 받는다. 겹친 무기가 손에 쥐어 보여야 한다.

## 2. A. 시범 (하복)

### 2.1 무기 그림 (모든 옷 공통)

입력 ① = `ref/weapon_src_{토큰}.png`(하복이 그 무기를 든 대기 그림 2배, 몽둥이는 방망이 그림). 비율 16:9. 크기는 내가 하복 그림의 무기 길이에 맞춰 줄인다.

| # | 토큰 | 무기 | 입력 ① | 프롬프트 |
|---|---|---|---|---|
| W1 | `bat` | 야구방망이 | `ref/weapon_src_bat.png` | `prompts/weapon_bat.txt` |
| W2 | `pipe` | 쇠파이프 | `ref/weapon_src_pipe.png` | `prompts/weapon_pipe.txt` |
| W3 | `baton` | 경찰봉 | `ref/weapon_src_baton.png` | `prompts/weapon_baton.txt` |
| W4 | `mop` | 대걸레 | `ref/weapon_src_mop.png` | `prompts/weapon_mop.txt` |
| W5 | `club` | 나무 몽둥이 | `ref/weapon_src_club.png` | `prompts/weapon_club.txt` |
| W6 | `club_taped` | 테이프 감은 몽둥이 | `ref/weapon_src_club_taped.png` | `prompts/weapon_club_taped.txt` |
| W7 | `club_rebar` | 철근 덧댄 몽둥이 | `ref/weapon_src_club_rebar.png` | `prompts/weapon_club_rebar.txt` |

- 무기는 수평으로 눕히고 손잡이가 왼쪽, 치는 끝이 오른쪽이다. 손잡이 자리(쥐는 점)는 내가 잰다.
- 몽둥이 3종의 공격은 모두 휘두르기다(철근 몽둥이도 파이프 내려치기가 아니라 휘두르기, E3 ②).

### 2.2 쥔 자세·무기 지운 공격

| # | 묶음 | 무엇 | 입력 ① | 장수 | 비율 |
|---|---|---|---|---|---|
| A1 | `player_summer_grip1_idle` | 하복 한 손 쥠 (방망이·파이프·경찰봉·몽둥이) 대기 | `input/player_summer_bat_idle_input.png` | 24 | 16:9 |
| A2 | `player_summer_grip1_walk` | 하복 한 손 쥠 (방망이·파이프·경찰봉·몽둥이) 걷기 | `input/player_summer_bat_walk_input.png` | 16 | 5:4 |
| A3 | `player_summer_grip1_run` | 하복 한 손 쥠 (방망이·파이프·경찰봉·몽둥이) 달리기 | `input/player_summer_bat_run_input.png` | 16 | 5:4 |
| A4 | `player_summer_grip1_crouch` | 하복 한 손 쥠 (방망이·파이프·경찰봉·몽둥이) 웅크려 걷기 | `input/player_summer_bat_crouch_input.png` | 16 | 5:4 |
| A5 | `player_summer_grip1_hurt` | 하복 한 손 쥠 (방망이·파이프·경찰봉·몽둥이) 다침 | `input/player_summer_bat_hurt_input.png` | 24 | 16:9 |
| A6 | `player_summer_grip1_exhausted` | 하복 한 손 쥠 (방망이·파이프·경찰봉·몽둥이) 지침 | `input/player_summer_bat_exhausted_input.png` | 24 | 16:9 |
| A7 | `player_summer_grip2_idle` | 하복 긴 막대 쥠 (대걸레) 대기 | `input/player_summer_mop_idle_input.png` | 24 | 16:9 |
| A8 | `player_summer_grip2_walk` | 하복 긴 막대 쥠 (대걸레) 걷기 | `input/player_summer_mop_walk_input.png` | 16 | 5:4 |
| A9 | `player_summer_grip2_run` | 하복 긴 막대 쥠 (대걸레) 달리기 | `input/player_summer_mop_run_input.png` | 16 | 5:4 |
| A10 | `player_summer_grip2_crouch` | 하복 긴 막대 쥠 (대걸레) 웅크려 걷기 | `input/player_summer_mop_crouch_input.png` | 16 | 5:4 |
| A11 | `player_summer_grip2_hurt` | 하복 긴 막대 쥠 (대걸레) 다침 | `input/player_summer_mop_hurt_input.png` | 24 | 16:9 |
| A12 | `player_summer_grip2_exhausted` | 하복 긴 막대 쥠 (대걸레) 지침 | `input/player_summer_mop_exhausted_input.png` | 24 | 16:9 |
| A13 | `player_summer_swing_attack` | 하복 휘두르기 (방망이·경찰봉·몽둥이 3종) — 무기 지움 | `input/player_summer_bat_attack_input.png` | 24 | 16:9 |
| A14 | `player_summer_smash_attack` | 하복 내려치기 (쇠파이프) — 무기 지움 | `input/player_summer_pipe_attack_input.png` | 24 | 16:9 |

받은 뒤 내가 하는 것: 무기 그림에서 손잡이 자리를 재고, 하복 무기 그림(지금 게임 그림)에서 프레임마다 손 위치·무기 각도·앞뒤를 자동으로 재어 겹치기 표를 만든다. 쥔 자세 + 무기 그림을 지금 하복 그림과 탐험 화면에서 나란히 보여 드린다. 괜찮으면 C 단계로 간다.

## 3. 옷 5벌

입력 ① = 하복 시트(B) 또는 A에서 받은 시트(C), 입력 ② = `ref/outfit_{옷}.png`. 옷 문구는 프롬프트 파일에 채워 두었다.

| 옷 | 문구 |
|---|---|
| `winter` 겨울 교복 | `the winter uniform: a long brown duffle coat over the navy uniform, dark tights and dark sneakers` |
| `padding` 패딩 점퍼 | `a puffy olive-green padded jacket over her uniform, dark trousers and sneakers` |
| `guard` 보호대·조끼 | `a red and black sports protective vest with elbow and knee guards over a dark tracksuit` |
| `riot` 진압복 | `an oversized black riot-police style padded suit, dark gloves, no helmet, her own black hair visible` |
| `uniform` 기본 교복(예비) | `a navy school blazer, knee-length navy pleated skirt, dark tights and sneakers` |

### 3.1 겨울 교복 `winter` (25시트)

| # | 단계 | 묶음 | 무엇 | 입력 ① | 장수 | 비율 |
|---|---|---|---|---|---|---|
| 1 | B | `player_winter_none_idle` | 겨울 교복 맨손 대기 | `input/player_summer_none_idle_input.png` | 24 | 16:9 |
| 2 | B | `player_winter_none_walk` | 겨울 교복 맨손 걷기 | `input/player_summer_none_walk_input.png` | 16 | 5:4 |
| 3 | B | `player_winter_none_run` | 겨울 교복 맨손 달리기 | `input/player_summer_none_run_input.png` | 16 | 5:4 |
| 4 | B | `player_winter_none_crouch` | 겨울 교복 맨손 웅크려 걷기 | `input/player_summer_none_crouch_input.png` | 16 | 5:4 |
| 5 | B | `player_winter_none_hurt` | 겨울 교복 맨손 다침 | `input/player_summer_none_hurt_input.png` | 24 | 16:9 |
| 6 | B | `player_winter_none_exhausted` | 겨울 교복 맨손 지침 | `input/player_summer_none_exhausted_input.png` | 24 | 16:9 |
| 7 | B | `player_winter_none_shove` | 겨울 교복 맨손 밀치기 | `input/player_summer_none_shove_input.png` | 24 | 16:9 |
| 8 | B | `player_winter_none_search` | 겨울 교복 맨손 수색 | `input/player_summer_none_search_input.png` | 12 | 16:9 |
| 9 | B | `player_winter_none_hide` | 겨울 교복 맨손 숨기 | `input/player_summer_none_hide_input.png` | 24 | 16:9 |
| 10 | B | `player_winter_none_doze` | 겨울 교복 맨손 잠듦 | `input/player_summer_none_doze_input.png` | 24 | 16:9 |
| 11 | C | `player_winter_grip1_idle` | 겨울 교복 한 손 쥠 (방망이·파이프·경찰봉·몽둥이) 대기 | A의 `player_summer_grip1_idle_sheet.png` | 24 | 16:9 |
| 12 | C | `player_winter_grip1_walk` | 겨울 교복 한 손 쥠 (방망이·파이프·경찰봉·몽둥이) 걷기 | A의 `player_summer_grip1_walk_sheet.png` | 16 | 5:4 |
| 13 | C | `player_winter_grip1_run` | 겨울 교복 한 손 쥠 (방망이·파이프·경찰봉·몽둥이) 달리기 | A의 `player_summer_grip1_run_sheet.png` | 16 | 5:4 |
| 14 | C | `player_winter_grip1_crouch` | 겨울 교복 한 손 쥠 (방망이·파이프·경찰봉·몽둥이) 웅크려 걷기 | A의 `player_summer_grip1_crouch_sheet.png` | 16 | 5:4 |
| 15 | C | `player_winter_grip1_hurt` | 겨울 교복 한 손 쥠 (방망이·파이프·경찰봉·몽둥이) 다침 | A의 `player_summer_grip1_hurt_sheet.png` | 24 | 16:9 |
| 16 | C | `player_winter_grip1_exhausted` | 겨울 교복 한 손 쥠 (방망이·파이프·경찰봉·몽둥이) 지침 | A의 `player_summer_grip1_exhausted_sheet.png` | 24 | 16:9 |
| 17 | C | `player_winter_grip2_idle` | 겨울 교복 긴 막대 쥠 (대걸레) 대기 | A의 `player_summer_grip2_idle_sheet.png` | 24 | 16:9 |
| 18 | C | `player_winter_grip2_walk` | 겨울 교복 긴 막대 쥠 (대걸레) 걷기 | A의 `player_summer_grip2_walk_sheet.png` | 16 | 5:4 |
| 19 | C | `player_winter_grip2_run` | 겨울 교복 긴 막대 쥠 (대걸레) 달리기 | A의 `player_summer_grip2_run_sheet.png` | 16 | 5:4 |
| 20 | C | `player_winter_grip2_crouch` | 겨울 교복 긴 막대 쥠 (대걸레) 웅크려 걷기 | A의 `player_summer_grip2_crouch_sheet.png` | 16 | 5:4 |
| 21 | C | `player_winter_grip2_hurt` | 겨울 교복 긴 막대 쥠 (대걸레) 다침 | A의 `player_summer_grip2_hurt_sheet.png` | 24 | 16:9 |
| 22 | C | `player_winter_grip2_exhausted` | 겨울 교복 긴 막대 쥠 (대걸레) 지침 | A의 `player_summer_grip2_exhausted_sheet.png` | 24 | 16:9 |
| 23 | C | `player_winter_swing_attack` | 겨울 교복 휘두르기 (방망이·경찰봉·몽둥이 3종) — 무기 없이 | A의 `player_summer_swing_attack_sheet.png` | 24 | 16:9 |
| 24 | C | `player_winter_smash_attack` | 겨울 교복 내려치기 (쇠파이프) — 무기 없이 | A의 `player_summer_smash_attack_sheet.png` | 24 | 16:9 |
| 25 | B | `player_winter_mop_attack` | 겨울 교복 찌르기 (대걸레, 무기째) | `input/player_summer_mop_attack_input.png` | 24 | 16:9 |

### 3.2 패딩 점퍼 `padding` (25시트)

| # | 단계 | 묶음 | 무엇 | 입력 ① | 장수 | 비율 |
|---|---|---|---|---|---|---|
| 1 | B | `player_padding_none_idle` | 패딩 점퍼 맨손 대기 | `input/player_summer_none_idle_input.png` | 24 | 16:9 |
| 2 | B | `player_padding_none_walk` | 패딩 점퍼 맨손 걷기 | `input/player_summer_none_walk_input.png` | 16 | 5:4 |
| 3 | B | `player_padding_none_run` | 패딩 점퍼 맨손 달리기 | `input/player_summer_none_run_input.png` | 16 | 5:4 |
| 4 | B | `player_padding_none_crouch` | 패딩 점퍼 맨손 웅크려 걷기 | `input/player_summer_none_crouch_input.png` | 16 | 5:4 |
| 5 | B | `player_padding_none_hurt` | 패딩 점퍼 맨손 다침 | `input/player_summer_none_hurt_input.png` | 24 | 16:9 |
| 6 | B | `player_padding_none_exhausted` | 패딩 점퍼 맨손 지침 | `input/player_summer_none_exhausted_input.png` | 24 | 16:9 |
| 7 | B | `player_padding_none_shove` | 패딩 점퍼 맨손 밀치기 | `input/player_summer_none_shove_input.png` | 24 | 16:9 |
| 8 | B | `player_padding_none_search` | 패딩 점퍼 맨손 수색 | `input/player_summer_none_search_input.png` | 12 | 16:9 |
| 9 | B | `player_padding_none_hide` | 패딩 점퍼 맨손 숨기 | `input/player_summer_none_hide_input.png` | 24 | 16:9 |
| 10 | B | `player_padding_none_doze` | 패딩 점퍼 맨손 잠듦 | `input/player_summer_none_doze_input.png` | 24 | 16:9 |
| 11 | C | `player_padding_grip1_idle` | 패딩 점퍼 한 손 쥠 (방망이·파이프·경찰봉·몽둥이) 대기 | A의 `player_summer_grip1_idle_sheet.png` | 24 | 16:9 |
| 12 | C | `player_padding_grip1_walk` | 패딩 점퍼 한 손 쥠 (방망이·파이프·경찰봉·몽둥이) 걷기 | A의 `player_summer_grip1_walk_sheet.png` | 16 | 5:4 |
| 13 | C | `player_padding_grip1_run` | 패딩 점퍼 한 손 쥠 (방망이·파이프·경찰봉·몽둥이) 달리기 | A의 `player_summer_grip1_run_sheet.png` | 16 | 5:4 |
| 14 | C | `player_padding_grip1_crouch` | 패딩 점퍼 한 손 쥠 (방망이·파이프·경찰봉·몽둥이) 웅크려 걷기 | A의 `player_summer_grip1_crouch_sheet.png` | 16 | 5:4 |
| 15 | C | `player_padding_grip1_hurt` | 패딩 점퍼 한 손 쥠 (방망이·파이프·경찰봉·몽둥이) 다침 | A의 `player_summer_grip1_hurt_sheet.png` | 24 | 16:9 |
| 16 | C | `player_padding_grip1_exhausted` | 패딩 점퍼 한 손 쥠 (방망이·파이프·경찰봉·몽둥이) 지침 | A의 `player_summer_grip1_exhausted_sheet.png` | 24 | 16:9 |
| 17 | C | `player_padding_grip2_idle` | 패딩 점퍼 긴 막대 쥠 (대걸레) 대기 | A의 `player_summer_grip2_idle_sheet.png` | 24 | 16:9 |
| 18 | C | `player_padding_grip2_walk` | 패딩 점퍼 긴 막대 쥠 (대걸레) 걷기 | A의 `player_summer_grip2_walk_sheet.png` | 16 | 5:4 |
| 19 | C | `player_padding_grip2_run` | 패딩 점퍼 긴 막대 쥠 (대걸레) 달리기 | A의 `player_summer_grip2_run_sheet.png` | 16 | 5:4 |
| 20 | C | `player_padding_grip2_crouch` | 패딩 점퍼 긴 막대 쥠 (대걸레) 웅크려 걷기 | A의 `player_summer_grip2_crouch_sheet.png` | 16 | 5:4 |
| 21 | C | `player_padding_grip2_hurt` | 패딩 점퍼 긴 막대 쥠 (대걸레) 다침 | A의 `player_summer_grip2_hurt_sheet.png` | 24 | 16:9 |
| 22 | C | `player_padding_grip2_exhausted` | 패딩 점퍼 긴 막대 쥠 (대걸레) 지침 | A의 `player_summer_grip2_exhausted_sheet.png` | 24 | 16:9 |
| 23 | C | `player_padding_swing_attack` | 패딩 점퍼 휘두르기 (방망이·경찰봉·몽둥이 3종) — 무기 없이 | A의 `player_summer_swing_attack_sheet.png` | 24 | 16:9 |
| 24 | C | `player_padding_smash_attack` | 패딩 점퍼 내려치기 (쇠파이프) — 무기 없이 | A의 `player_summer_smash_attack_sheet.png` | 24 | 16:9 |
| 25 | B | `player_padding_mop_attack` | 패딩 점퍼 찌르기 (대걸레, 무기째) | `input/player_summer_mop_attack_input.png` | 24 | 16:9 |

### 3.3 보호대·조끼 `guard` (25시트)

| # | 단계 | 묶음 | 무엇 | 입력 ① | 장수 | 비율 |
|---|---|---|---|---|---|---|
| 1 | B | `player_guard_none_idle` | 보호대·조끼 맨손 대기 | `input/player_summer_none_idle_input.png` | 24 | 16:9 |
| 2 | B | `player_guard_none_walk` | 보호대·조끼 맨손 걷기 | `input/player_summer_none_walk_input.png` | 16 | 5:4 |
| 3 | B | `player_guard_none_run` | 보호대·조끼 맨손 달리기 | `input/player_summer_none_run_input.png` | 16 | 5:4 |
| 4 | B | `player_guard_none_crouch` | 보호대·조끼 맨손 웅크려 걷기 | `input/player_summer_none_crouch_input.png` | 16 | 5:4 |
| 5 | B | `player_guard_none_hurt` | 보호대·조끼 맨손 다침 | `input/player_summer_none_hurt_input.png` | 24 | 16:9 |
| 6 | B | `player_guard_none_exhausted` | 보호대·조끼 맨손 지침 | `input/player_summer_none_exhausted_input.png` | 24 | 16:9 |
| 7 | B | `player_guard_none_shove` | 보호대·조끼 맨손 밀치기 | `input/player_summer_none_shove_input.png` | 24 | 16:9 |
| 8 | B | `player_guard_none_search` | 보호대·조끼 맨손 수색 | `input/player_summer_none_search_input.png` | 12 | 16:9 |
| 9 | B | `player_guard_none_hide` | 보호대·조끼 맨손 숨기 | `input/player_summer_none_hide_input.png` | 24 | 16:9 |
| 10 | B | `player_guard_none_doze` | 보호대·조끼 맨손 잠듦 | `input/player_summer_none_doze_input.png` | 24 | 16:9 |
| 11 | C | `player_guard_grip1_idle` | 보호대·조끼 한 손 쥠 (방망이·파이프·경찰봉·몽둥이) 대기 | A의 `player_summer_grip1_idle_sheet.png` | 24 | 16:9 |
| 12 | C | `player_guard_grip1_walk` | 보호대·조끼 한 손 쥠 (방망이·파이프·경찰봉·몽둥이) 걷기 | A의 `player_summer_grip1_walk_sheet.png` | 16 | 5:4 |
| 13 | C | `player_guard_grip1_run` | 보호대·조끼 한 손 쥠 (방망이·파이프·경찰봉·몽둥이) 달리기 | A의 `player_summer_grip1_run_sheet.png` | 16 | 5:4 |
| 14 | C | `player_guard_grip1_crouch` | 보호대·조끼 한 손 쥠 (방망이·파이프·경찰봉·몽둥이) 웅크려 걷기 | A의 `player_summer_grip1_crouch_sheet.png` | 16 | 5:4 |
| 15 | C | `player_guard_grip1_hurt` | 보호대·조끼 한 손 쥠 (방망이·파이프·경찰봉·몽둥이) 다침 | A의 `player_summer_grip1_hurt_sheet.png` | 24 | 16:9 |
| 16 | C | `player_guard_grip1_exhausted` | 보호대·조끼 한 손 쥠 (방망이·파이프·경찰봉·몽둥이) 지침 | A의 `player_summer_grip1_exhausted_sheet.png` | 24 | 16:9 |
| 17 | C | `player_guard_grip2_idle` | 보호대·조끼 긴 막대 쥠 (대걸레) 대기 | A의 `player_summer_grip2_idle_sheet.png` | 24 | 16:9 |
| 18 | C | `player_guard_grip2_walk` | 보호대·조끼 긴 막대 쥠 (대걸레) 걷기 | A의 `player_summer_grip2_walk_sheet.png` | 16 | 5:4 |
| 19 | C | `player_guard_grip2_run` | 보호대·조끼 긴 막대 쥠 (대걸레) 달리기 | A의 `player_summer_grip2_run_sheet.png` | 16 | 5:4 |
| 20 | C | `player_guard_grip2_crouch` | 보호대·조끼 긴 막대 쥠 (대걸레) 웅크려 걷기 | A의 `player_summer_grip2_crouch_sheet.png` | 16 | 5:4 |
| 21 | C | `player_guard_grip2_hurt` | 보호대·조끼 긴 막대 쥠 (대걸레) 다침 | A의 `player_summer_grip2_hurt_sheet.png` | 24 | 16:9 |
| 22 | C | `player_guard_grip2_exhausted` | 보호대·조끼 긴 막대 쥠 (대걸레) 지침 | A의 `player_summer_grip2_exhausted_sheet.png` | 24 | 16:9 |
| 23 | C | `player_guard_swing_attack` | 보호대·조끼 휘두르기 (방망이·경찰봉·몽둥이 3종) — 무기 없이 | A의 `player_summer_swing_attack_sheet.png` | 24 | 16:9 |
| 24 | C | `player_guard_smash_attack` | 보호대·조끼 내려치기 (쇠파이프) — 무기 없이 | A의 `player_summer_smash_attack_sheet.png` | 24 | 16:9 |
| 25 | B | `player_guard_mop_attack` | 보호대·조끼 찌르기 (대걸레, 무기째) | `input/player_summer_mop_attack_input.png` | 24 | 16:9 |

### 3.4 진압복 `riot` (25시트)

| # | 단계 | 묶음 | 무엇 | 입력 ① | 장수 | 비율 |
|---|---|---|---|---|---|---|
| 1 | B | `player_riot_none_idle` | 진압복 맨손 대기 | `input/player_summer_none_idle_input.png` | 24 | 16:9 |
| 2 | B | `player_riot_none_walk` | 진압복 맨손 걷기 | `input/player_summer_none_walk_input.png` | 16 | 5:4 |
| 3 | B | `player_riot_none_run` | 진압복 맨손 달리기 | `input/player_summer_none_run_input.png` | 16 | 5:4 |
| 4 | B | `player_riot_none_crouch` | 진압복 맨손 웅크려 걷기 | `input/player_summer_none_crouch_input.png` | 16 | 5:4 |
| 5 | B | `player_riot_none_hurt` | 진압복 맨손 다침 | `input/player_summer_none_hurt_input.png` | 24 | 16:9 |
| 6 | B | `player_riot_none_exhausted` | 진압복 맨손 지침 | `input/player_summer_none_exhausted_input.png` | 24 | 16:9 |
| 7 | B | `player_riot_none_shove` | 진압복 맨손 밀치기 | `input/player_summer_none_shove_input.png` | 24 | 16:9 |
| 8 | B | `player_riot_none_search` | 진압복 맨손 수색 | `input/player_summer_none_search_input.png` | 12 | 16:9 |
| 9 | B | `player_riot_none_hide` | 진압복 맨손 숨기 | `input/player_summer_none_hide_input.png` | 24 | 16:9 |
| 10 | B | `player_riot_none_doze` | 진압복 맨손 잠듦 | `input/player_summer_none_doze_input.png` | 24 | 16:9 |
| 11 | C | `player_riot_grip1_idle` | 진압복 한 손 쥠 (방망이·파이프·경찰봉·몽둥이) 대기 | A의 `player_summer_grip1_idle_sheet.png` | 24 | 16:9 |
| 12 | C | `player_riot_grip1_walk` | 진압복 한 손 쥠 (방망이·파이프·경찰봉·몽둥이) 걷기 | A의 `player_summer_grip1_walk_sheet.png` | 16 | 5:4 |
| 13 | C | `player_riot_grip1_run` | 진압복 한 손 쥠 (방망이·파이프·경찰봉·몽둥이) 달리기 | A의 `player_summer_grip1_run_sheet.png` | 16 | 5:4 |
| 14 | C | `player_riot_grip1_crouch` | 진압복 한 손 쥠 (방망이·파이프·경찰봉·몽둥이) 웅크려 걷기 | A의 `player_summer_grip1_crouch_sheet.png` | 16 | 5:4 |
| 15 | C | `player_riot_grip1_hurt` | 진압복 한 손 쥠 (방망이·파이프·경찰봉·몽둥이) 다침 | A의 `player_summer_grip1_hurt_sheet.png` | 24 | 16:9 |
| 16 | C | `player_riot_grip1_exhausted` | 진압복 한 손 쥠 (방망이·파이프·경찰봉·몽둥이) 지침 | A의 `player_summer_grip1_exhausted_sheet.png` | 24 | 16:9 |
| 17 | C | `player_riot_grip2_idle` | 진압복 긴 막대 쥠 (대걸레) 대기 | A의 `player_summer_grip2_idle_sheet.png` | 24 | 16:9 |
| 18 | C | `player_riot_grip2_walk` | 진압복 긴 막대 쥠 (대걸레) 걷기 | A의 `player_summer_grip2_walk_sheet.png` | 16 | 5:4 |
| 19 | C | `player_riot_grip2_run` | 진압복 긴 막대 쥠 (대걸레) 달리기 | A의 `player_summer_grip2_run_sheet.png` | 16 | 5:4 |
| 20 | C | `player_riot_grip2_crouch` | 진압복 긴 막대 쥠 (대걸레) 웅크려 걷기 | A의 `player_summer_grip2_crouch_sheet.png` | 16 | 5:4 |
| 21 | C | `player_riot_grip2_hurt` | 진압복 긴 막대 쥠 (대걸레) 다침 | A의 `player_summer_grip2_hurt_sheet.png` | 24 | 16:9 |
| 22 | C | `player_riot_grip2_exhausted` | 진압복 긴 막대 쥠 (대걸레) 지침 | A의 `player_summer_grip2_exhausted_sheet.png` | 24 | 16:9 |
| 23 | C | `player_riot_swing_attack` | 진압복 휘두르기 (방망이·경찰봉·몽둥이 3종) — 무기 없이 | A의 `player_summer_swing_attack_sheet.png` | 24 | 16:9 |
| 24 | C | `player_riot_smash_attack` | 진압복 내려치기 (쇠파이프) — 무기 없이 | A의 `player_summer_smash_attack_sheet.png` | 24 | 16:9 |
| 25 | B | `player_riot_mop_attack` | 진압복 찌르기 (대걸레, 무기째) | `input/player_summer_mop_attack_input.png` | 24 | 16:9 |

### 3.5 기본 교복(예비) `uniform` (25시트)

| # | 단계 | 묶음 | 무엇 | 입력 ① | 장수 | 비율 |
|---|---|---|---|---|---|---|
| 1 | B | `player_uniform_none_idle` | 기본 교복(예비) 맨손 대기 | `input/player_summer_none_idle_input.png` | 24 | 16:9 |
| 2 | B | `player_uniform_none_walk` | 기본 교복(예비) 맨손 걷기 | `input/player_summer_none_walk_input.png` | 16 | 5:4 |
| 3 | B | `player_uniform_none_run` | 기본 교복(예비) 맨손 달리기 | `input/player_summer_none_run_input.png` | 16 | 5:4 |
| 4 | B | `player_uniform_none_crouch` | 기본 교복(예비) 맨손 웅크려 걷기 | `input/player_summer_none_crouch_input.png` | 16 | 5:4 |
| 5 | B | `player_uniform_none_hurt` | 기본 교복(예비) 맨손 다침 | `input/player_summer_none_hurt_input.png` | 24 | 16:9 |
| 6 | B | `player_uniform_none_exhausted` | 기본 교복(예비) 맨손 지침 | `input/player_summer_none_exhausted_input.png` | 24 | 16:9 |
| 7 | B | `player_uniform_none_shove` | 기본 교복(예비) 맨손 밀치기 | `input/player_summer_none_shove_input.png` | 24 | 16:9 |
| 8 | B | `player_uniform_none_search` | 기본 교복(예비) 맨손 수색 | `input/player_summer_none_search_input.png` | 12 | 16:9 |
| 9 | B | `player_uniform_none_hide` | 기본 교복(예비) 맨손 숨기 | `input/player_summer_none_hide_input.png` | 24 | 16:9 |
| 10 | B | `player_uniform_none_doze` | 기본 교복(예비) 맨손 잠듦 | `input/player_summer_none_doze_input.png` | 24 | 16:9 |
| 11 | C | `player_uniform_grip1_idle` | 기본 교복(예비) 한 손 쥠 (방망이·파이프·경찰봉·몽둥이) 대기 | A의 `player_summer_grip1_idle_sheet.png` | 24 | 16:9 |
| 12 | C | `player_uniform_grip1_walk` | 기본 교복(예비) 한 손 쥠 (방망이·파이프·경찰봉·몽둥이) 걷기 | A의 `player_summer_grip1_walk_sheet.png` | 16 | 5:4 |
| 13 | C | `player_uniform_grip1_run` | 기본 교복(예비) 한 손 쥠 (방망이·파이프·경찰봉·몽둥이) 달리기 | A의 `player_summer_grip1_run_sheet.png` | 16 | 5:4 |
| 14 | C | `player_uniform_grip1_crouch` | 기본 교복(예비) 한 손 쥠 (방망이·파이프·경찰봉·몽둥이) 웅크려 걷기 | A의 `player_summer_grip1_crouch_sheet.png` | 16 | 5:4 |
| 15 | C | `player_uniform_grip1_hurt` | 기본 교복(예비) 한 손 쥠 (방망이·파이프·경찰봉·몽둥이) 다침 | A의 `player_summer_grip1_hurt_sheet.png` | 24 | 16:9 |
| 16 | C | `player_uniform_grip1_exhausted` | 기본 교복(예비) 한 손 쥠 (방망이·파이프·경찰봉·몽둥이) 지침 | A의 `player_summer_grip1_exhausted_sheet.png` | 24 | 16:9 |
| 17 | C | `player_uniform_grip2_idle` | 기본 교복(예비) 긴 막대 쥠 (대걸레) 대기 | A의 `player_summer_grip2_idle_sheet.png` | 24 | 16:9 |
| 18 | C | `player_uniform_grip2_walk` | 기본 교복(예비) 긴 막대 쥠 (대걸레) 걷기 | A의 `player_summer_grip2_walk_sheet.png` | 16 | 5:4 |
| 19 | C | `player_uniform_grip2_run` | 기본 교복(예비) 긴 막대 쥠 (대걸레) 달리기 | A의 `player_summer_grip2_run_sheet.png` | 16 | 5:4 |
| 20 | C | `player_uniform_grip2_crouch` | 기본 교복(예비) 긴 막대 쥠 (대걸레) 웅크려 걷기 | A의 `player_summer_grip2_crouch_sheet.png` | 16 | 5:4 |
| 21 | C | `player_uniform_grip2_hurt` | 기본 교복(예비) 긴 막대 쥠 (대걸레) 다침 | A의 `player_summer_grip2_hurt_sheet.png` | 24 | 16:9 |
| 22 | C | `player_uniform_grip2_exhausted` | 기본 교복(예비) 긴 막대 쥠 (대걸레) 지침 | A의 `player_summer_grip2_exhausted_sheet.png` | 24 | 16:9 |
| 23 | C | `player_uniform_swing_attack` | 기본 교복(예비) 휘두르기 (방망이·경찰봉·몽둥이 3종) — 무기 없이 | A의 `player_summer_swing_attack_sheet.png` | 24 | 16:9 |
| 24 | C | `player_uniform_smash_attack` | 기본 교복(예비) 내려치기 (쇠파이프) — 무기 없이 | A의 `player_summer_smash_attack_sheet.png` | 24 | 16:9 |
| 25 | B | `player_uniform_mop_attack` | 기본 교복(예비) 찌르기 (대걸레, 무기째) | `input/player_summer_mop_attack_input.png` | 24 | 16:9 |

## 4. 받는 차례

1. **A 시범** — 무기 7장 → 하복 쥔 자세 12시트 → 무기 지운 공격 2시트. 다 받으면 알려 준다. 나는 겹치기 코드와 비교판을 만든다.
2. **B** — 옷마다 맨손 10동작 + 대걸레 찌르기. A와 상관없으니 먼저 해도 된다. 차례는 겨울 → 패딩 → 보호대 → 진압복 → 동복.
3. **C** — A 시범을 "괜찮다"고 정한 뒤, 옷마다 쥔 자세 12시트 + 무기 지운 공격 2시트.

## 5. 받은 뒤 (내가 할 일)

- 넣기 도구는 PixelLab 하복과 같은 규칙으로 만든다: 시트를 칸으로 나눠 192 × 156(PPU 72, `sprite_meta.json` 그림별 `Ppu`)으로 맞추고, 발을 하복 바닥 줄에, 잔 점·떨어진 덩어리를 지운다. 하복과 같은 칸 배치라 칸마다 하복 프레임과 겹쳐 자리를 맞춘다.
- 무기 겹치기: 무기 층을 가방처럼(`player_anchors.json` 프레임마다 손 X·Y·각도·앞뒤) 둔다. 몸 그림 이름은 `player_{옷}_grip1_*`·`grip2_*`·`swing_attack`·`smash_attack`, 무기는 `weapon_{토큰}`.
- 옛 그림은 `code_temp/apogirl/backup_pl24/`로 옮기고, 보호구 색 덧칠(`armor_recolor.py install`)을 다시 돌린다(잡지·매트·철판은 보호대·패딩 그림에서 만든다).
- 하복도 몽둥이를 쓰려면 겹치기가 필요하다. 하복은 A의 쥔 자세로 몽둥이만 겹치고, 지금 있는 방망이·파이프·경찰봉·대걸레 그림은 그대로 둔다.

## 6. 안전 (마스터 프롬프트 13장)

- 모든 프롬프트 끝에 붙였다: 피·상처 없음, 노출·선정적 자세 없음, 글자·상표 없음.
- 진압복은 경찰 표식·글자 없이 검은 패딩 옷으로만. 헬멧 없이 얼굴이 보이게.

## 7. 소리: 잠든 좀비 다시 받기

지금 잠든 좀비 소리는 코 고는 소리처럼 들린다(1차 문구가 `many slow overlapping breaths`, 숨소리였다). **낮게 목 안에서 그르렁거리는 소리**로 바꾼다. 조용하지만 무섭게 — 가까이 가면 들리고, 깨우지 않게 살금살금 지나가야 한다는 느낌이 나야 한다.

| 파일 | 어디서 | 규격 | 프롬프트 |
|---|---|---|---|
| `sfx_z_dormant_crowd.wav` | 잠든 좀비 (가까이 있을 때 도는 환경음) | 6초, 이음매 없이 반복, 모노 | `prompts/sfx_z_dormant_crowd.txt` |

- 모델: Seed Audio 1.0 (`seed_audio`, 힉스필드 기본 소리 모델). 후보를 2~3개 받아 들어 보고 고른다.
- 게임에서는 잠든 좀비가 주인공 14칸 안에 있을 때 환경음으로 반복된다(`ExpeditionDirector.UpdateSounds`, 반복 이름 `crowd`). 코드는 그대로다.
- 저장: `ArtInbox/sound16/sfx_z_dormant_crowd.wav` (mp3·ogg로 와도 된다). 내가 이음매를 맞추고(끝과 처음을 겹쳐 이어 붙임) 크기를 지금 소리와 같게 맞춰 `Resources/Audio/Sfx/sfx_z_dormant_crowd.wav`를 바꾼다. 옛 소리는 `code_temp/apogirl/backup_sound16/`로 옮긴다.
- 마음에 안 들면 짧은 판으로 다시 받는다.

**`sfx_z_dormant_crowd`**

```
Sound effect for a 2D zombie survival game: several infected people asleep on their feet in a dark school corridor, heard from a few meters away. A low, slow, guttural growl in the throat, deep and rumbling like a large animal breathing in its sleep: long wet rattling exhales, a faint gravelly purr under each breath, the growls overlapping and rising and falling unevenly. Not snoring: no nasal snore, no whistling through the nose, no snorting, no human sleep sounds, no comedy, no words, no screams, no music. Very quiet and tense, close to silence between breaths, dark low frequencies, slight room reverb. About 6 seconds, mono, seamlessly loopable: the end must flow back into the start with no click and no gap.
```

짧은 판:

```
Low, quiet zombie growling while asleep: deep guttural throat rumble, slow menacing breaths with a rattling growl on each exhale, several sleepers overlapping, dark corridor reverb, never snoring, no nasal sounds, no voices, no music, 6 seconds, seamless loop.
```
