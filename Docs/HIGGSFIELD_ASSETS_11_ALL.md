# 힉스필드 에셋 11차 (전체): 탐험 주인공 스프라이트 24프레임 (2026-11-23)

> 이 문서는 `python Tools/make_higgsfield_doc11.py`로 만든다. 고칠 때는 스크립트를 고치고 다시 돌린다.

샘플(여름 교복 야구방망이 공격, [HIGGSFIELD_ASSETS_11.md](HIGGSFIELD_ASSETS_11.md))이 잘 맞아서, **탐험 주인공 그림 전부**를 같은 방식으로 다시 받는다. 지금 프레임을 키프레임으로 그대로 두고 그 사이를 채워 **모든 묶음을 24프레임(6 × 4칸 시트 한 장)** 으로 만든다.

- 작업 제출은 사용자가 한다. 나(Claude)는 문서·참고 이미지·프롬프트 파일만 만든다.
- 빠진 것: 문 열기 `open`(쓰지 않음, 사용자 결정 2026-11-23), 이미 넣은 `player_summer_bat_attack`.
- 모두 **227묶음**. 묶음마다 시트 한 장이다.

## 0. 연결 결과 (2026-11-24)

- 받은 시트 227장(2688 × 1520, 6 × 4칸, 투명 배경)을 모두 24프레임으로 넣었다. 도구 `code_temp/apogirl/tools/s11_install_all.py` (preview | install), 옛 그림·가방 자리 표는 `code_temp/apogirl/backup_s11/`.
- 칸 나누기: 대걸레 끝·발이 칸 경계를 넘은 시트가 있어, 시트 전체를 덩어리로 나눠 무게중심이 든 칸에 준다.
- 크기는 키프레임 짝의 면적 비(묶음마다 ×0.17~0.20). 세로는 칸마다 몸 맨 아래를 옛 프레임 바닥 줄에.
- 가로: 생성 그림은 칸마다 좌우로 4px 안팎 흔들려서, **도는 동작은 머리를 옛 자리에** 맞추고, 한 번 동작은 몸통을 옛 몸통 길에(방망이·파이프·경찰봉 공격은 샘플의 앞으로 내미는 곡선) 맞춘다.
- 가방 자리 `player_anchors.json`은 옛 값을 새 칸 위치로 보간, X는 옮긴 만큼.
- 게임: 도는 동작은 프레임 수에 맞춰 초당 프레임을 늘려 한 바퀴 시간이 예전과 같다(`ExpeditionDirector.LoopFrame` `designedFrames` 16·12). 공격은 모두 24장이라 고르게 돌고, 대걸레 맞는 순간 `At`은 0.45 → 0.5(13번 칸).
- 기본 교복·보호대의 옛 지침 그림은 똑바로 서 있었는데, 새 그림은 다른 옷처럼 허리를 숙여 숨을 몰아쉰다.
- 되돌림(사용자 요청 2026-11-24): 새 걷기·달리기·웅크려 걷기는 발 디딤 박자가 고르지 않아 다리가 안 맞고(웅크려 걸을 때 가방도 낮게 흔들림, 방망이 들고 달릴 때 방망이 자리가 섞임), 수색은 옛 그림이 낫다 — 이 네 동작 96묶음은 옛 PixelLab 그림(16·12장)과 옛 가방 자리로 되돌렸다(새 그림은 `code_temp/apogirl/s11_reverted_new/`). 남은 새 동작(대기·지침·다침·공격·밀치기·숨기·잠듦)의 가방 자리는 그림의 등 선·머리 위에 맞춰 다시 셌다(옛 그림에서 잰 간격의 가운데 값, 이웃 칸과 고르게, 각도는 묶음마다 하나) — 도구 `code_temp/apogirl/tools/s11_bag.py`.

## 1. 동작별 정리

| 동작 | 무기 | 지금 | 새로 | 게임에서 도는 법 | 키프레임 배치 |
|---|---|---|---|---|---|
| 걷기 `walk` | 맨손·야구방망이·쇠파이프·경찰봉·대걸레 | 16 | 24 | 걸음 속도에 맞춰 돈다 | 홀수 프레임 1·3…15 → 1·4…22번 칸 (짝수 프레임은 사이 그림 안내) |
| 대기 `idle` | 맨손·야구방망이·쇠파이프·경찰봉·대걸레 | 16 | 24 | 돈다 | 홀수 프레임 1·3…15 → 1·4…22번 칸 (짝수 프레임은 사이 그림 안내) |
| 달리기 `run` | 맨손·야구방망이·쇠파이프·경찰봉·대걸레 | 16 | 24 | 돈다 | 홀수 프레임 1·3…15 → 1·4…22번 칸 (짝수 프레임은 사이 그림 안내) |
| 무기 공격 `attack` | 야구방망이·쇠파이프·경찰봉 | 8 | 24 | 0.5초 한 번, 맞는 순간 = 13번 칸 | 1·4·7…22번 칸 (사이 2장씩) |
| 무기 공격 `attack` | 대걸레 | 20 | 24 | 0.5초 한 번, 맞는 순간 = 13번 칸 | 8장을 골라 1·4…22번 칸 |
| 웅크려 걷기 `crouch` | 맨손·야구방망이·쇠파이프·경찰봉·대걸레 | 16 | 24 | 돈다(멈추면 1번 칸) | 홀수 프레임 1·3…15 → 1·4…22번 칸 (짝수 프레임은 사이 그림 안내) |
| 지침 `exhausted` | 맨손·야구방망이·쇠파이프·경찰봉·대걸레 | 12 | 24 | 멈춰 지쳤을 때 돈다 | 홀수 칸 1·3…23 (사이 1장씩) |
| 다침 `hurt` | 맨손·야구방망이·쇠파이프·경찰봉·대걸레 | 8 | 24 | 0.4초 한 번 | 1·4·7…22번 칸 (사이 2장씩) |
| 밀치기 `shove` | 맨손 | 20 | 24 | 0.5초 한 번 | 8장을 골라 1·4…22번 칸 |
| 수색 `search` | 맨손 | 12 | 24 | 수색하는 동안 돈다 | 홀수 칸 1·3…23 (사이 1장씩) |
| 숨기 `hide` | 맨손 | 8 | 24 | Space를 누르는 만큼 진행, 숨으면 마지막 칸 | 1·4·7…22번 칸 (사이 2장씩) |
| 잠듦 `doze` | 맨손 | 12 | 24 | 숨다 잠들면 돈다 | 홀수 칸 1·3…23 (사이 1장씩) |

- 도는 동작(걷기·대기·달리기·웅크려 걷기·지침·수색·잠듦)은 24칸째가 1번 칸으로 자연스럽게 이어져야 한다.
- 20장짜리 한 번 동작의 키프레임(지금 프레임, 1부터): 대걸레 공격 `1, 3, 5, 7, 10, 13, 16, 20` (찌르는 끝 10번이 13번 칸), 밀치기 `1, 4, 6, 8, 10, 13, 16, 20`.
- 도는 동작은 프레임이 16·12장 → 24장으로 늘어도 **한 바퀴 시간이 같게** 내가 게임의 초당 프레임을 맞춘다(걸음이 느려 보이지 않게).

## 2. 받는 차례

1. **여름 교복** (방어구가 없는 여름의 기본 차림, 가장 오래 보인다) — 걷기·대기·달리기·공격부터.
2. **겨울 교복** — 여름 결과 시트를 참고로 넣고 옷만 바꾼다.
3. **패딩 점퍼·보호대·진압복** — 방어구 차림.
4. **기본 교복** — 계절 없는 예비 차림이라 마지막.

옷마다 차례: 걷기 → 대기 → 달리기 → 공격 → 웅크려 걷기 → 지침 → 다침 → 밀치기 → 수색 → 숨기 → 잠듦. 여섯 벌의 동작이 같아야 무기·가방 자리가 맞으므로, **두 번째 옷부터는 같은 묶음의 여름 결과 시트를 꼭 참고 이미지로 넣는다.**

## 3. 참고 이미지와 프롬프트 파일

스크립트가 만들어 둔다 (`ArtInbox/sprites11/`, git 밖):

| 파일 | 내용 |
|---|---|
| `ref/{묶음}_ref.png` | 지금 프레임을 한 줄 8장씩 4배로 이어 붙인 것 (투명 배경). **키프레임·그림체의 기준** |
| `ref/player_{옷}_{무기}_idle0_ref.png` | 그 차림의 대기 0번 4배. 얼굴·옷·무기 모양의 기준 |
| `prompts/{묶음}.txt` | 그 묶음의 프롬프트 전체 (옷 문구까지 채움). **그대로 복사해 붙여 넣는다** |

넣는 순서: ① `{묶음}_ref.png` ② 대기 기준 ③ (여름이 아니면) 받은 여름 시트. 서진 기준 시트는 그림체가 흔들려서 넣지 않는다.

## 4. 규격 (샘플과 같음)

| 항목 | 값 |
|---|---|
| 모델 | `gpt_image_2_6` (시트 한 장 생성) |
| 시트 | 가로 6칸 × 세로 4줄 = 24칸, 칸 사이 여백·테두리·번호 없음, **비율 16:9** (샘플은 2688 × 1520으로 왔다) |
| 방향·발 | 오른쪽을 보는 옆모습, 모든 칸에서 발바닥 높이가 같다 |
| 배경 | 투명. 안 되면 마젠타 `#ff00ff` 단색 |
| 가방 | 그리지 않는다 (게임이 따로 얹는다) |
| 저장 | `ArtInbox/sprites11/{묶음}_sheet.png` (예: `player_summer_none_walk_sheet.png`) |

**잘 안 될 때** (샘플 문서와 같음)
- 칸 수가 틀리거나 순서가 섞이면: 첫 줄 `Grid of 24 equal cells`를 `Exactly 24 frames in a 6 x 4 grid`로 바꿔 다시.
- 그림체가 지금과 달라지면(매끈한 일러스트처럼): 참고 이미지를 ①만 넣고 다시.
- 두 사람이 겹치거나 무기가 늘어나면 그 칸만 알려 줘도 된다. 내가 이웃 칸으로 메운다.

## 5. 묶음 목록

### 5.1 여름 교복 `summer` (37묶음)

| # | 묶음 | 동작 | 무기 | 지금 → 새로 | 참고 이미지 |
|---|---|---|---|---|---|
| 1 | `player_summer_none_walk` | 걷기 | 맨손 | 16 → 24 | `ref/player_summer_none_walk_ref.png`, `ref/player_summer_none_idle0_ref.png` |
| 2 | `player_summer_bat_walk` | 걷기 | 야구방망이 | 16 → 24 | `ref/player_summer_bat_walk_ref.png`, `ref/player_summer_bat_idle0_ref.png` |
| 3 | `player_summer_pipe_walk` | 걷기 | 쇠파이프 | 16 → 24 | `ref/player_summer_pipe_walk_ref.png`, `ref/player_summer_pipe_idle0_ref.png` |
| 4 | `player_summer_baton_walk` | 걷기 | 경찰봉 | 16 → 24 | `ref/player_summer_baton_walk_ref.png`, `ref/player_summer_baton_idle0_ref.png` |
| 5 | `player_summer_mop_walk` | 걷기 | 대걸레 | 16 → 24 | `ref/player_summer_mop_walk_ref.png`, `ref/player_summer_mop_idle0_ref.png` |
| 6 | `player_summer_none_idle` | 대기 | 맨손 | 16 → 24 | `ref/player_summer_none_idle_ref.png` |
| 7 | `player_summer_bat_idle` | 대기 | 야구방망이 | 16 → 24 | `ref/player_summer_bat_idle_ref.png` |
| 8 | `player_summer_pipe_idle` | 대기 | 쇠파이프 | 16 → 24 | `ref/player_summer_pipe_idle_ref.png` |
| 9 | `player_summer_baton_idle` | 대기 | 경찰봉 | 16 → 24 | `ref/player_summer_baton_idle_ref.png` |
| 10 | `player_summer_mop_idle` | 대기 | 대걸레 | 16 → 24 | `ref/player_summer_mop_idle_ref.png` |
| 11 | `player_summer_none_run` | 달리기 | 맨손 | 16 → 24 | `ref/player_summer_none_run_ref.png`, `ref/player_summer_none_idle0_ref.png` |
| 12 | `player_summer_bat_run` | 달리기 | 야구방망이 | 16 → 24 | `ref/player_summer_bat_run_ref.png`, `ref/player_summer_bat_idle0_ref.png` |
| 13 | `player_summer_pipe_run` | 달리기 | 쇠파이프 | 16 → 24 | `ref/player_summer_pipe_run_ref.png`, `ref/player_summer_pipe_idle0_ref.png` |
| 14 | `player_summer_baton_run` | 달리기 | 경찰봉 | 16 → 24 | `ref/player_summer_baton_run_ref.png`, `ref/player_summer_baton_idle0_ref.png` |
| 15 | `player_summer_mop_run` | 달리기 | 대걸레 | 16 → 24 | `ref/player_summer_mop_run_ref.png`, `ref/player_summer_mop_idle0_ref.png` |
| 16 | `player_summer_pipe_attack` | 무기 공격 | 쇠파이프 | 8 → 24 | `ref/player_summer_pipe_attack_ref.png`, `ref/player_summer_pipe_idle0_ref.png` |
| 17 | `player_summer_baton_attack` | 무기 공격 | 경찰봉 | 8 → 24 | `ref/player_summer_baton_attack_ref.png`, `ref/player_summer_baton_idle0_ref.png` |
| 18 | `player_summer_mop_attack` | 무기 공격 | 대걸레 | 20 → 24 | `ref/player_summer_mop_attack_ref.png`, `ref/player_summer_mop_idle0_ref.png` |
| 19 | `player_summer_none_crouch` | 웅크려 걷기 | 맨손 | 16 → 24 | `ref/player_summer_none_crouch_ref.png`, `ref/player_summer_none_idle0_ref.png` |
| 20 | `player_summer_bat_crouch` | 웅크려 걷기 | 야구방망이 | 16 → 24 | `ref/player_summer_bat_crouch_ref.png`, `ref/player_summer_bat_idle0_ref.png` |
| 21 | `player_summer_pipe_crouch` | 웅크려 걷기 | 쇠파이프 | 16 → 24 | `ref/player_summer_pipe_crouch_ref.png`, `ref/player_summer_pipe_idle0_ref.png` |
| 22 | `player_summer_baton_crouch` | 웅크려 걷기 | 경찰봉 | 16 → 24 | `ref/player_summer_baton_crouch_ref.png`, `ref/player_summer_baton_idle0_ref.png` |
| 23 | `player_summer_mop_crouch` | 웅크려 걷기 | 대걸레 | 16 → 24 | `ref/player_summer_mop_crouch_ref.png`, `ref/player_summer_mop_idle0_ref.png` |
| 24 | `player_summer_none_exhausted` | 지침 | 맨손 | 12 → 24 | `ref/player_summer_none_exhausted_ref.png`, `ref/player_summer_none_idle0_ref.png` |
| 25 | `player_summer_bat_exhausted` | 지침 | 야구방망이 | 12 → 24 | `ref/player_summer_bat_exhausted_ref.png`, `ref/player_summer_bat_idle0_ref.png` |
| 26 | `player_summer_pipe_exhausted` | 지침 | 쇠파이프 | 12 → 24 | `ref/player_summer_pipe_exhausted_ref.png`, `ref/player_summer_pipe_idle0_ref.png` |
| 27 | `player_summer_baton_exhausted` | 지침 | 경찰봉 | 12 → 24 | `ref/player_summer_baton_exhausted_ref.png`, `ref/player_summer_baton_idle0_ref.png` |
| 28 | `player_summer_mop_exhausted` | 지침 | 대걸레 | 12 → 24 | `ref/player_summer_mop_exhausted_ref.png`, `ref/player_summer_mop_idle0_ref.png` |
| 29 | `player_summer_none_hurt` | 다침 | 맨손 | 8 → 24 | `ref/player_summer_none_hurt_ref.png`, `ref/player_summer_none_idle0_ref.png` |
| 30 | `player_summer_bat_hurt` | 다침 | 야구방망이 | 8 → 24 | `ref/player_summer_bat_hurt_ref.png`, `ref/player_summer_bat_idle0_ref.png` |
| 31 | `player_summer_pipe_hurt` | 다침 | 쇠파이프 | 8 → 24 | `ref/player_summer_pipe_hurt_ref.png`, `ref/player_summer_pipe_idle0_ref.png` |
| 32 | `player_summer_baton_hurt` | 다침 | 경찰봉 | 8 → 24 | `ref/player_summer_baton_hurt_ref.png`, `ref/player_summer_baton_idle0_ref.png` |
| 33 | `player_summer_mop_hurt` | 다침 | 대걸레 | 8 → 24 | `ref/player_summer_mop_hurt_ref.png`, `ref/player_summer_mop_idle0_ref.png` |
| 34 | `player_summer_none_shove` | 밀치기 | 맨손 | 20 → 24 | `ref/player_summer_none_shove_ref.png`, `ref/player_summer_none_idle0_ref.png` |
| 35 | `player_summer_none_search` | 수색 | 맨손 | 12 → 24 | `ref/player_summer_none_search_ref.png`, `ref/player_summer_none_idle0_ref.png` |
| 36 | `player_summer_none_hide` | 숨기 | 맨손 | 8 → 24 | `ref/player_summer_none_hide_ref.png`, `ref/player_summer_none_idle0_ref.png` |
| 37 | `player_summer_none_doze` | 잠듦 | 맨손 | 12 → 24 | `ref/player_summer_none_doze_ref.png`, `ref/player_summer_none_idle0_ref.png` |

### 5.2 겨울 교복 `winter` (38묶음)

| # | 묶음 | 동작 | 무기 | 지금 → 새로 | 참고 이미지 |
|---|---|---|---|---|---|
| 1 | `player_winter_none_walk` | 걷기 | 맨손 | 16 → 24 | `ref/player_winter_none_walk_ref.png`, `ref/player_winter_none_idle0_ref.png`, 받은 `player_summer_none_walk_sheet.png` |
| 2 | `player_winter_bat_walk` | 걷기 | 야구방망이 | 16 → 24 | `ref/player_winter_bat_walk_ref.png`, `ref/player_winter_bat_idle0_ref.png`, 받은 `player_summer_bat_walk_sheet.png` |
| 3 | `player_winter_pipe_walk` | 걷기 | 쇠파이프 | 16 → 24 | `ref/player_winter_pipe_walk_ref.png`, `ref/player_winter_pipe_idle0_ref.png`, 받은 `player_summer_pipe_walk_sheet.png` |
| 4 | `player_winter_baton_walk` | 걷기 | 경찰봉 | 16 → 24 | `ref/player_winter_baton_walk_ref.png`, `ref/player_winter_baton_idle0_ref.png`, 받은 `player_summer_baton_walk_sheet.png` |
| 5 | `player_winter_mop_walk` | 걷기 | 대걸레 | 16 → 24 | `ref/player_winter_mop_walk_ref.png`, `ref/player_winter_mop_idle0_ref.png`, 받은 `player_summer_mop_walk_sheet.png` |
| 6 | `player_winter_none_idle` | 대기 | 맨손 | 16 → 24 | `ref/player_winter_none_idle_ref.png`, 받은 `player_summer_none_idle_sheet.png` |
| 7 | `player_winter_bat_idle` | 대기 | 야구방망이 | 16 → 24 | `ref/player_winter_bat_idle_ref.png`, 받은 `player_summer_bat_idle_sheet.png` |
| 8 | `player_winter_pipe_idle` | 대기 | 쇠파이프 | 16 → 24 | `ref/player_winter_pipe_idle_ref.png`, 받은 `player_summer_pipe_idle_sheet.png` |
| 9 | `player_winter_baton_idle` | 대기 | 경찰봉 | 16 → 24 | `ref/player_winter_baton_idle_ref.png`, 받은 `player_summer_baton_idle_sheet.png` |
| 10 | `player_winter_mop_idle` | 대기 | 대걸레 | 16 → 24 | `ref/player_winter_mop_idle_ref.png`, 받은 `player_summer_mop_idle_sheet.png` |
| 11 | `player_winter_none_run` | 달리기 | 맨손 | 16 → 24 | `ref/player_winter_none_run_ref.png`, `ref/player_winter_none_idle0_ref.png`, 받은 `player_summer_none_run_sheet.png` |
| 12 | `player_winter_bat_run` | 달리기 | 야구방망이 | 16 → 24 | `ref/player_winter_bat_run_ref.png`, `ref/player_winter_bat_idle0_ref.png`, 받은 `player_summer_bat_run_sheet.png` |
| 13 | `player_winter_pipe_run` | 달리기 | 쇠파이프 | 16 → 24 | `ref/player_winter_pipe_run_ref.png`, `ref/player_winter_pipe_idle0_ref.png`, 받은 `player_summer_pipe_run_sheet.png` |
| 14 | `player_winter_baton_run` | 달리기 | 경찰봉 | 16 → 24 | `ref/player_winter_baton_run_ref.png`, `ref/player_winter_baton_idle0_ref.png`, 받은 `player_summer_baton_run_sheet.png` |
| 15 | `player_winter_mop_run` | 달리기 | 대걸레 | 16 → 24 | `ref/player_winter_mop_run_ref.png`, `ref/player_winter_mop_idle0_ref.png`, 받은 `player_summer_mop_run_sheet.png` |
| 16 | `player_winter_bat_attack` | 무기 공격 | 야구방망이 | 8 → 24 | `ref/player_winter_bat_attack_ref.png`, `ref/player_winter_bat_idle0_ref.png`, 받은 `player_summer_bat_attack_sheet.png` |
| 17 | `player_winter_pipe_attack` | 무기 공격 | 쇠파이프 | 8 → 24 | `ref/player_winter_pipe_attack_ref.png`, `ref/player_winter_pipe_idle0_ref.png`, 받은 `player_summer_pipe_attack_sheet.png` |
| 18 | `player_winter_baton_attack` | 무기 공격 | 경찰봉 | 8 → 24 | `ref/player_winter_baton_attack_ref.png`, `ref/player_winter_baton_idle0_ref.png`, 받은 `player_summer_baton_attack_sheet.png` |
| 19 | `player_winter_mop_attack` | 무기 공격 | 대걸레 | 20 → 24 | `ref/player_winter_mop_attack_ref.png`, `ref/player_winter_mop_idle0_ref.png`, 받은 `player_summer_mop_attack_sheet.png` |
| 20 | `player_winter_none_crouch` | 웅크려 걷기 | 맨손 | 16 → 24 | `ref/player_winter_none_crouch_ref.png`, `ref/player_winter_none_idle0_ref.png`, 받은 `player_summer_none_crouch_sheet.png` |
| 21 | `player_winter_bat_crouch` | 웅크려 걷기 | 야구방망이 | 16 → 24 | `ref/player_winter_bat_crouch_ref.png`, `ref/player_winter_bat_idle0_ref.png`, 받은 `player_summer_bat_crouch_sheet.png` |
| 22 | `player_winter_pipe_crouch` | 웅크려 걷기 | 쇠파이프 | 16 → 24 | `ref/player_winter_pipe_crouch_ref.png`, `ref/player_winter_pipe_idle0_ref.png`, 받은 `player_summer_pipe_crouch_sheet.png` |
| 23 | `player_winter_baton_crouch` | 웅크려 걷기 | 경찰봉 | 16 → 24 | `ref/player_winter_baton_crouch_ref.png`, `ref/player_winter_baton_idle0_ref.png`, 받은 `player_summer_baton_crouch_sheet.png` |
| 24 | `player_winter_mop_crouch` | 웅크려 걷기 | 대걸레 | 16 → 24 | `ref/player_winter_mop_crouch_ref.png`, `ref/player_winter_mop_idle0_ref.png`, 받은 `player_summer_mop_crouch_sheet.png` |
| 25 | `player_winter_none_exhausted` | 지침 | 맨손 | 12 → 24 | `ref/player_winter_none_exhausted_ref.png`, `ref/player_winter_none_idle0_ref.png`, 받은 `player_summer_none_exhausted_sheet.png` |
| 26 | `player_winter_bat_exhausted` | 지침 | 야구방망이 | 12 → 24 | `ref/player_winter_bat_exhausted_ref.png`, `ref/player_winter_bat_idle0_ref.png`, 받은 `player_summer_bat_exhausted_sheet.png` |
| 27 | `player_winter_pipe_exhausted` | 지침 | 쇠파이프 | 12 → 24 | `ref/player_winter_pipe_exhausted_ref.png`, `ref/player_winter_pipe_idle0_ref.png`, 받은 `player_summer_pipe_exhausted_sheet.png` |
| 28 | `player_winter_baton_exhausted` | 지침 | 경찰봉 | 12 → 24 | `ref/player_winter_baton_exhausted_ref.png`, `ref/player_winter_baton_idle0_ref.png`, 받은 `player_summer_baton_exhausted_sheet.png` |
| 29 | `player_winter_mop_exhausted` | 지침 | 대걸레 | 12 → 24 | `ref/player_winter_mop_exhausted_ref.png`, `ref/player_winter_mop_idle0_ref.png`, 받은 `player_summer_mop_exhausted_sheet.png` |
| 30 | `player_winter_none_hurt` | 다침 | 맨손 | 8 → 24 | `ref/player_winter_none_hurt_ref.png`, `ref/player_winter_none_idle0_ref.png`, 받은 `player_summer_none_hurt_sheet.png` |
| 31 | `player_winter_bat_hurt` | 다침 | 야구방망이 | 8 → 24 | `ref/player_winter_bat_hurt_ref.png`, `ref/player_winter_bat_idle0_ref.png`, 받은 `player_summer_bat_hurt_sheet.png` |
| 32 | `player_winter_pipe_hurt` | 다침 | 쇠파이프 | 8 → 24 | `ref/player_winter_pipe_hurt_ref.png`, `ref/player_winter_pipe_idle0_ref.png`, 받은 `player_summer_pipe_hurt_sheet.png` |
| 33 | `player_winter_baton_hurt` | 다침 | 경찰봉 | 8 → 24 | `ref/player_winter_baton_hurt_ref.png`, `ref/player_winter_baton_idle0_ref.png`, 받은 `player_summer_baton_hurt_sheet.png` |
| 34 | `player_winter_mop_hurt` | 다침 | 대걸레 | 8 → 24 | `ref/player_winter_mop_hurt_ref.png`, `ref/player_winter_mop_idle0_ref.png`, 받은 `player_summer_mop_hurt_sheet.png` |
| 35 | `player_winter_none_shove` | 밀치기 | 맨손 | 20 → 24 | `ref/player_winter_none_shove_ref.png`, `ref/player_winter_none_idle0_ref.png`, 받은 `player_summer_none_shove_sheet.png` |
| 36 | `player_winter_none_search` | 수색 | 맨손 | 12 → 24 | `ref/player_winter_none_search_ref.png`, `ref/player_winter_none_idle0_ref.png`, 받은 `player_summer_none_search_sheet.png` |
| 37 | `player_winter_none_hide` | 숨기 | 맨손 | 8 → 24 | `ref/player_winter_none_hide_ref.png`, `ref/player_winter_none_idle0_ref.png`, 받은 `player_summer_none_hide_sheet.png` |
| 38 | `player_winter_none_doze` | 잠듦 | 맨손 | 12 → 24 | `ref/player_winter_none_doze_ref.png`, `ref/player_winter_none_idle0_ref.png`, 받은 `player_summer_none_doze_sheet.png` |

### 5.3 패딩 점퍼 `padding` (38묶음)

| # | 묶음 | 동작 | 무기 | 지금 → 새로 | 참고 이미지 |
|---|---|---|---|---|---|
| 1 | `player_padding_none_walk` | 걷기 | 맨손 | 16 → 24 | `ref/player_padding_none_walk_ref.png`, `ref/player_padding_none_idle0_ref.png`, 받은 `player_summer_none_walk_sheet.png` |
| 2 | `player_padding_bat_walk` | 걷기 | 야구방망이 | 16 → 24 | `ref/player_padding_bat_walk_ref.png`, `ref/player_padding_bat_idle0_ref.png`, 받은 `player_summer_bat_walk_sheet.png` |
| 3 | `player_padding_pipe_walk` | 걷기 | 쇠파이프 | 16 → 24 | `ref/player_padding_pipe_walk_ref.png`, `ref/player_padding_pipe_idle0_ref.png`, 받은 `player_summer_pipe_walk_sheet.png` |
| 4 | `player_padding_baton_walk` | 걷기 | 경찰봉 | 16 → 24 | `ref/player_padding_baton_walk_ref.png`, `ref/player_padding_baton_idle0_ref.png`, 받은 `player_summer_baton_walk_sheet.png` |
| 5 | `player_padding_mop_walk` | 걷기 | 대걸레 | 16 → 24 | `ref/player_padding_mop_walk_ref.png`, `ref/player_padding_mop_idle0_ref.png`, 받은 `player_summer_mop_walk_sheet.png` |
| 6 | `player_padding_none_idle` | 대기 | 맨손 | 16 → 24 | `ref/player_padding_none_idle_ref.png`, 받은 `player_summer_none_idle_sheet.png` |
| 7 | `player_padding_bat_idle` | 대기 | 야구방망이 | 16 → 24 | `ref/player_padding_bat_idle_ref.png`, 받은 `player_summer_bat_idle_sheet.png` |
| 8 | `player_padding_pipe_idle` | 대기 | 쇠파이프 | 16 → 24 | `ref/player_padding_pipe_idle_ref.png`, 받은 `player_summer_pipe_idle_sheet.png` |
| 9 | `player_padding_baton_idle` | 대기 | 경찰봉 | 16 → 24 | `ref/player_padding_baton_idle_ref.png`, 받은 `player_summer_baton_idle_sheet.png` |
| 10 | `player_padding_mop_idle` | 대기 | 대걸레 | 16 → 24 | `ref/player_padding_mop_idle_ref.png`, 받은 `player_summer_mop_idle_sheet.png` |
| 11 | `player_padding_none_run` | 달리기 | 맨손 | 16 → 24 | `ref/player_padding_none_run_ref.png`, `ref/player_padding_none_idle0_ref.png`, 받은 `player_summer_none_run_sheet.png` |
| 12 | `player_padding_bat_run` | 달리기 | 야구방망이 | 16 → 24 | `ref/player_padding_bat_run_ref.png`, `ref/player_padding_bat_idle0_ref.png`, 받은 `player_summer_bat_run_sheet.png` |
| 13 | `player_padding_pipe_run` | 달리기 | 쇠파이프 | 16 → 24 | `ref/player_padding_pipe_run_ref.png`, `ref/player_padding_pipe_idle0_ref.png`, 받은 `player_summer_pipe_run_sheet.png` |
| 14 | `player_padding_baton_run` | 달리기 | 경찰봉 | 16 → 24 | `ref/player_padding_baton_run_ref.png`, `ref/player_padding_baton_idle0_ref.png`, 받은 `player_summer_baton_run_sheet.png` |
| 15 | `player_padding_mop_run` | 달리기 | 대걸레 | 16 → 24 | `ref/player_padding_mop_run_ref.png`, `ref/player_padding_mop_idle0_ref.png`, 받은 `player_summer_mop_run_sheet.png` |
| 16 | `player_padding_bat_attack` | 무기 공격 | 야구방망이 | 8 → 24 | `ref/player_padding_bat_attack_ref.png`, `ref/player_padding_bat_idle0_ref.png`, 받은 `player_summer_bat_attack_sheet.png` |
| 17 | `player_padding_pipe_attack` | 무기 공격 | 쇠파이프 | 8 → 24 | `ref/player_padding_pipe_attack_ref.png`, `ref/player_padding_pipe_idle0_ref.png`, 받은 `player_summer_pipe_attack_sheet.png` |
| 18 | `player_padding_baton_attack` | 무기 공격 | 경찰봉 | 8 → 24 | `ref/player_padding_baton_attack_ref.png`, `ref/player_padding_baton_idle0_ref.png`, 받은 `player_summer_baton_attack_sheet.png` |
| 19 | `player_padding_mop_attack` | 무기 공격 | 대걸레 | 20 → 24 | `ref/player_padding_mop_attack_ref.png`, `ref/player_padding_mop_idle0_ref.png`, 받은 `player_summer_mop_attack_sheet.png` |
| 20 | `player_padding_none_crouch` | 웅크려 걷기 | 맨손 | 16 → 24 | `ref/player_padding_none_crouch_ref.png`, `ref/player_padding_none_idle0_ref.png`, 받은 `player_summer_none_crouch_sheet.png` |
| 21 | `player_padding_bat_crouch` | 웅크려 걷기 | 야구방망이 | 16 → 24 | `ref/player_padding_bat_crouch_ref.png`, `ref/player_padding_bat_idle0_ref.png`, 받은 `player_summer_bat_crouch_sheet.png` |
| 22 | `player_padding_pipe_crouch` | 웅크려 걷기 | 쇠파이프 | 16 → 24 | `ref/player_padding_pipe_crouch_ref.png`, `ref/player_padding_pipe_idle0_ref.png`, 받은 `player_summer_pipe_crouch_sheet.png` |
| 23 | `player_padding_baton_crouch` | 웅크려 걷기 | 경찰봉 | 16 → 24 | `ref/player_padding_baton_crouch_ref.png`, `ref/player_padding_baton_idle0_ref.png`, 받은 `player_summer_baton_crouch_sheet.png` |
| 24 | `player_padding_mop_crouch` | 웅크려 걷기 | 대걸레 | 16 → 24 | `ref/player_padding_mop_crouch_ref.png`, `ref/player_padding_mop_idle0_ref.png`, 받은 `player_summer_mop_crouch_sheet.png` |
| 25 | `player_padding_none_exhausted` | 지침 | 맨손 | 12 → 24 | `ref/player_padding_none_exhausted_ref.png`, `ref/player_padding_none_idle0_ref.png`, 받은 `player_summer_none_exhausted_sheet.png` |
| 26 | `player_padding_bat_exhausted` | 지침 | 야구방망이 | 12 → 24 | `ref/player_padding_bat_exhausted_ref.png`, `ref/player_padding_bat_idle0_ref.png`, 받은 `player_summer_bat_exhausted_sheet.png` |
| 27 | `player_padding_pipe_exhausted` | 지침 | 쇠파이프 | 12 → 24 | `ref/player_padding_pipe_exhausted_ref.png`, `ref/player_padding_pipe_idle0_ref.png`, 받은 `player_summer_pipe_exhausted_sheet.png` |
| 28 | `player_padding_baton_exhausted` | 지침 | 경찰봉 | 12 → 24 | `ref/player_padding_baton_exhausted_ref.png`, `ref/player_padding_baton_idle0_ref.png`, 받은 `player_summer_baton_exhausted_sheet.png` |
| 29 | `player_padding_mop_exhausted` | 지침 | 대걸레 | 12 → 24 | `ref/player_padding_mop_exhausted_ref.png`, `ref/player_padding_mop_idle0_ref.png`, 받은 `player_summer_mop_exhausted_sheet.png` |
| 30 | `player_padding_none_hurt` | 다침 | 맨손 | 8 → 24 | `ref/player_padding_none_hurt_ref.png`, `ref/player_padding_none_idle0_ref.png`, 받은 `player_summer_none_hurt_sheet.png` |
| 31 | `player_padding_bat_hurt` | 다침 | 야구방망이 | 8 → 24 | `ref/player_padding_bat_hurt_ref.png`, `ref/player_padding_bat_idle0_ref.png`, 받은 `player_summer_bat_hurt_sheet.png` |
| 32 | `player_padding_pipe_hurt` | 다침 | 쇠파이프 | 8 → 24 | `ref/player_padding_pipe_hurt_ref.png`, `ref/player_padding_pipe_idle0_ref.png`, 받은 `player_summer_pipe_hurt_sheet.png` |
| 33 | `player_padding_baton_hurt` | 다침 | 경찰봉 | 8 → 24 | `ref/player_padding_baton_hurt_ref.png`, `ref/player_padding_baton_idle0_ref.png`, 받은 `player_summer_baton_hurt_sheet.png` |
| 34 | `player_padding_mop_hurt` | 다침 | 대걸레 | 8 → 24 | `ref/player_padding_mop_hurt_ref.png`, `ref/player_padding_mop_idle0_ref.png`, 받은 `player_summer_mop_hurt_sheet.png` |
| 35 | `player_padding_none_shove` | 밀치기 | 맨손 | 20 → 24 | `ref/player_padding_none_shove_ref.png`, `ref/player_padding_none_idle0_ref.png`, 받은 `player_summer_none_shove_sheet.png` |
| 36 | `player_padding_none_search` | 수색 | 맨손 | 12 → 24 | `ref/player_padding_none_search_ref.png`, `ref/player_padding_none_idle0_ref.png`, 받은 `player_summer_none_search_sheet.png` |
| 37 | `player_padding_none_hide` | 숨기 | 맨손 | 8 → 24 | `ref/player_padding_none_hide_ref.png`, `ref/player_padding_none_idle0_ref.png`, 받은 `player_summer_none_hide_sheet.png` |
| 38 | `player_padding_none_doze` | 잠듦 | 맨손 | 12 → 24 | `ref/player_padding_none_doze_ref.png`, `ref/player_padding_none_idle0_ref.png`, 받은 `player_summer_none_doze_sheet.png` |

### 5.4 보호대·조끼 `guard` (38묶음)

| # | 묶음 | 동작 | 무기 | 지금 → 새로 | 참고 이미지 |
|---|---|---|---|---|---|
| 1 | `player_guard_none_walk` | 걷기 | 맨손 | 16 → 24 | `ref/player_guard_none_walk_ref.png`, `ref/player_guard_none_idle0_ref.png`, 받은 `player_summer_none_walk_sheet.png` |
| 2 | `player_guard_bat_walk` | 걷기 | 야구방망이 | 16 → 24 | `ref/player_guard_bat_walk_ref.png`, `ref/player_guard_bat_idle0_ref.png`, 받은 `player_summer_bat_walk_sheet.png` |
| 3 | `player_guard_pipe_walk` | 걷기 | 쇠파이프 | 16 → 24 | `ref/player_guard_pipe_walk_ref.png`, `ref/player_guard_pipe_idle0_ref.png`, 받은 `player_summer_pipe_walk_sheet.png` |
| 4 | `player_guard_baton_walk` | 걷기 | 경찰봉 | 16 → 24 | `ref/player_guard_baton_walk_ref.png`, `ref/player_guard_baton_idle0_ref.png`, 받은 `player_summer_baton_walk_sheet.png` |
| 5 | `player_guard_mop_walk` | 걷기 | 대걸레 | 16 → 24 | `ref/player_guard_mop_walk_ref.png`, `ref/player_guard_mop_idle0_ref.png`, 받은 `player_summer_mop_walk_sheet.png` |
| 6 | `player_guard_none_idle` | 대기 | 맨손 | 16 → 24 | `ref/player_guard_none_idle_ref.png`, 받은 `player_summer_none_idle_sheet.png` |
| 7 | `player_guard_bat_idle` | 대기 | 야구방망이 | 16 → 24 | `ref/player_guard_bat_idle_ref.png`, 받은 `player_summer_bat_idle_sheet.png` |
| 8 | `player_guard_pipe_idle` | 대기 | 쇠파이프 | 16 → 24 | `ref/player_guard_pipe_idle_ref.png`, 받은 `player_summer_pipe_idle_sheet.png` |
| 9 | `player_guard_baton_idle` | 대기 | 경찰봉 | 16 → 24 | `ref/player_guard_baton_idle_ref.png`, 받은 `player_summer_baton_idle_sheet.png` |
| 10 | `player_guard_mop_idle` | 대기 | 대걸레 | 16 → 24 | `ref/player_guard_mop_idle_ref.png`, 받은 `player_summer_mop_idle_sheet.png` |
| 11 | `player_guard_none_run` | 달리기 | 맨손 | 16 → 24 | `ref/player_guard_none_run_ref.png`, `ref/player_guard_none_idle0_ref.png`, 받은 `player_summer_none_run_sheet.png` |
| 12 | `player_guard_bat_run` | 달리기 | 야구방망이 | 16 → 24 | `ref/player_guard_bat_run_ref.png`, `ref/player_guard_bat_idle0_ref.png`, 받은 `player_summer_bat_run_sheet.png` |
| 13 | `player_guard_pipe_run` | 달리기 | 쇠파이프 | 16 → 24 | `ref/player_guard_pipe_run_ref.png`, `ref/player_guard_pipe_idle0_ref.png`, 받은 `player_summer_pipe_run_sheet.png` |
| 14 | `player_guard_baton_run` | 달리기 | 경찰봉 | 16 → 24 | `ref/player_guard_baton_run_ref.png`, `ref/player_guard_baton_idle0_ref.png`, 받은 `player_summer_baton_run_sheet.png` |
| 15 | `player_guard_mop_run` | 달리기 | 대걸레 | 16 → 24 | `ref/player_guard_mop_run_ref.png`, `ref/player_guard_mop_idle0_ref.png`, 받은 `player_summer_mop_run_sheet.png` |
| 16 | `player_guard_bat_attack` | 무기 공격 | 야구방망이 | 8 → 24 | `ref/player_guard_bat_attack_ref.png`, `ref/player_guard_bat_idle0_ref.png`, 받은 `player_summer_bat_attack_sheet.png` |
| 17 | `player_guard_pipe_attack` | 무기 공격 | 쇠파이프 | 8 → 24 | `ref/player_guard_pipe_attack_ref.png`, `ref/player_guard_pipe_idle0_ref.png`, 받은 `player_summer_pipe_attack_sheet.png` |
| 18 | `player_guard_baton_attack` | 무기 공격 | 경찰봉 | 8 → 24 | `ref/player_guard_baton_attack_ref.png`, `ref/player_guard_baton_idle0_ref.png`, 받은 `player_summer_baton_attack_sheet.png` |
| 19 | `player_guard_mop_attack` | 무기 공격 | 대걸레 | 20 → 24 | `ref/player_guard_mop_attack_ref.png`, `ref/player_guard_mop_idle0_ref.png`, 받은 `player_summer_mop_attack_sheet.png` |
| 20 | `player_guard_none_crouch` | 웅크려 걷기 | 맨손 | 16 → 24 | `ref/player_guard_none_crouch_ref.png`, `ref/player_guard_none_idle0_ref.png`, 받은 `player_summer_none_crouch_sheet.png` |
| 21 | `player_guard_bat_crouch` | 웅크려 걷기 | 야구방망이 | 16 → 24 | `ref/player_guard_bat_crouch_ref.png`, `ref/player_guard_bat_idle0_ref.png`, 받은 `player_summer_bat_crouch_sheet.png` |
| 22 | `player_guard_pipe_crouch` | 웅크려 걷기 | 쇠파이프 | 16 → 24 | `ref/player_guard_pipe_crouch_ref.png`, `ref/player_guard_pipe_idle0_ref.png`, 받은 `player_summer_pipe_crouch_sheet.png` |
| 23 | `player_guard_baton_crouch` | 웅크려 걷기 | 경찰봉 | 16 → 24 | `ref/player_guard_baton_crouch_ref.png`, `ref/player_guard_baton_idle0_ref.png`, 받은 `player_summer_baton_crouch_sheet.png` |
| 24 | `player_guard_mop_crouch` | 웅크려 걷기 | 대걸레 | 16 → 24 | `ref/player_guard_mop_crouch_ref.png`, `ref/player_guard_mop_idle0_ref.png`, 받은 `player_summer_mop_crouch_sheet.png` |
| 25 | `player_guard_none_exhausted` | 지침 | 맨손 | 12 → 24 | `ref/player_guard_none_exhausted_ref.png`, `ref/player_guard_none_idle0_ref.png`, 받은 `player_summer_none_exhausted_sheet.png` |
| 26 | `player_guard_bat_exhausted` | 지침 | 야구방망이 | 12 → 24 | `ref/player_guard_bat_exhausted_ref.png`, `ref/player_guard_bat_idle0_ref.png`, 받은 `player_summer_bat_exhausted_sheet.png` |
| 27 | `player_guard_pipe_exhausted` | 지침 | 쇠파이프 | 12 → 24 | `ref/player_guard_pipe_exhausted_ref.png`, `ref/player_guard_pipe_idle0_ref.png`, 받은 `player_summer_pipe_exhausted_sheet.png` |
| 28 | `player_guard_baton_exhausted` | 지침 | 경찰봉 | 12 → 24 | `ref/player_guard_baton_exhausted_ref.png`, `ref/player_guard_baton_idle0_ref.png`, 받은 `player_summer_baton_exhausted_sheet.png` |
| 29 | `player_guard_mop_exhausted` | 지침 | 대걸레 | 12 → 24 | `ref/player_guard_mop_exhausted_ref.png`, `ref/player_guard_mop_idle0_ref.png`, 받은 `player_summer_mop_exhausted_sheet.png` |
| 30 | `player_guard_none_hurt` | 다침 | 맨손 | 8 → 24 | `ref/player_guard_none_hurt_ref.png`, `ref/player_guard_none_idle0_ref.png`, 받은 `player_summer_none_hurt_sheet.png` |
| 31 | `player_guard_bat_hurt` | 다침 | 야구방망이 | 8 → 24 | `ref/player_guard_bat_hurt_ref.png`, `ref/player_guard_bat_idle0_ref.png`, 받은 `player_summer_bat_hurt_sheet.png` |
| 32 | `player_guard_pipe_hurt` | 다침 | 쇠파이프 | 8 → 24 | `ref/player_guard_pipe_hurt_ref.png`, `ref/player_guard_pipe_idle0_ref.png`, 받은 `player_summer_pipe_hurt_sheet.png` |
| 33 | `player_guard_baton_hurt` | 다침 | 경찰봉 | 8 → 24 | `ref/player_guard_baton_hurt_ref.png`, `ref/player_guard_baton_idle0_ref.png`, 받은 `player_summer_baton_hurt_sheet.png` |
| 34 | `player_guard_mop_hurt` | 다침 | 대걸레 | 8 → 24 | `ref/player_guard_mop_hurt_ref.png`, `ref/player_guard_mop_idle0_ref.png`, 받은 `player_summer_mop_hurt_sheet.png` |
| 35 | `player_guard_none_shove` | 밀치기 | 맨손 | 20 → 24 | `ref/player_guard_none_shove_ref.png`, `ref/player_guard_none_idle0_ref.png`, 받은 `player_summer_none_shove_sheet.png` |
| 36 | `player_guard_none_search` | 수색 | 맨손 | 12 → 24 | `ref/player_guard_none_search_ref.png`, `ref/player_guard_none_idle0_ref.png`, 받은 `player_summer_none_search_sheet.png` |
| 37 | `player_guard_none_hide` | 숨기 | 맨손 | 8 → 24 | `ref/player_guard_none_hide_ref.png`, `ref/player_guard_none_idle0_ref.png`, 받은 `player_summer_none_hide_sheet.png` |
| 38 | `player_guard_none_doze` | 잠듦 | 맨손 | 12 → 24 | `ref/player_guard_none_doze_ref.png`, `ref/player_guard_none_idle0_ref.png`, 받은 `player_summer_none_doze_sheet.png` |

### 5.5 진압복 `riot` (38묶음)

| # | 묶음 | 동작 | 무기 | 지금 → 새로 | 참고 이미지 |
|---|---|---|---|---|---|
| 1 | `player_riot_none_walk` | 걷기 | 맨손 | 16 → 24 | `ref/player_riot_none_walk_ref.png`, `ref/player_riot_none_idle0_ref.png`, 받은 `player_summer_none_walk_sheet.png` |
| 2 | `player_riot_bat_walk` | 걷기 | 야구방망이 | 16 → 24 | `ref/player_riot_bat_walk_ref.png`, `ref/player_riot_bat_idle0_ref.png`, 받은 `player_summer_bat_walk_sheet.png` |
| 3 | `player_riot_pipe_walk` | 걷기 | 쇠파이프 | 16 → 24 | `ref/player_riot_pipe_walk_ref.png`, `ref/player_riot_pipe_idle0_ref.png`, 받은 `player_summer_pipe_walk_sheet.png` |
| 4 | `player_riot_baton_walk` | 걷기 | 경찰봉 | 16 → 24 | `ref/player_riot_baton_walk_ref.png`, `ref/player_riot_baton_idle0_ref.png`, 받은 `player_summer_baton_walk_sheet.png` |
| 5 | `player_riot_mop_walk` | 걷기 | 대걸레 | 16 → 24 | `ref/player_riot_mop_walk_ref.png`, `ref/player_riot_mop_idle0_ref.png`, 받은 `player_summer_mop_walk_sheet.png` |
| 6 | `player_riot_none_idle` | 대기 | 맨손 | 16 → 24 | `ref/player_riot_none_idle_ref.png`, 받은 `player_summer_none_idle_sheet.png` |
| 7 | `player_riot_bat_idle` | 대기 | 야구방망이 | 16 → 24 | `ref/player_riot_bat_idle_ref.png`, 받은 `player_summer_bat_idle_sheet.png` |
| 8 | `player_riot_pipe_idle` | 대기 | 쇠파이프 | 16 → 24 | `ref/player_riot_pipe_idle_ref.png`, 받은 `player_summer_pipe_idle_sheet.png` |
| 9 | `player_riot_baton_idle` | 대기 | 경찰봉 | 16 → 24 | `ref/player_riot_baton_idle_ref.png`, 받은 `player_summer_baton_idle_sheet.png` |
| 10 | `player_riot_mop_idle` | 대기 | 대걸레 | 16 → 24 | `ref/player_riot_mop_idle_ref.png`, 받은 `player_summer_mop_idle_sheet.png` |
| 11 | `player_riot_none_run` | 달리기 | 맨손 | 16 → 24 | `ref/player_riot_none_run_ref.png`, `ref/player_riot_none_idle0_ref.png`, 받은 `player_summer_none_run_sheet.png` |
| 12 | `player_riot_bat_run` | 달리기 | 야구방망이 | 16 → 24 | `ref/player_riot_bat_run_ref.png`, `ref/player_riot_bat_idle0_ref.png`, 받은 `player_summer_bat_run_sheet.png` |
| 13 | `player_riot_pipe_run` | 달리기 | 쇠파이프 | 16 → 24 | `ref/player_riot_pipe_run_ref.png`, `ref/player_riot_pipe_idle0_ref.png`, 받은 `player_summer_pipe_run_sheet.png` |
| 14 | `player_riot_baton_run` | 달리기 | 경찰봉 | 16 → 24 | `ref/player_riot_baton_run_ref.png`, `ref/player_riot_baton_idle0_ref.png`, 받은 `player_summer_baton_run_sheet.png` |
| 15 | `player_riot_mop_run` | 달리기 | 대걸레 | 16 → 24 | `ref/player_riot_mop_run_ref.png`, `ref/player_riot_mop_idle0_ref.png`, 받은 `player_summer_mop_run_sheet.png` |
| 16 | `player_riot_bat_attack` | 무기 공격 | 야구방망이 | 8 → 24 | `ref/player_riot_bat_attack_ref.png`, `ref/player_riot_bat_idle0_ref.png`, 받은 `player_summer_bat_attack_sheet.png` |
| 17 | `player_riot_pipe_attack` | 무기 공격 | 쇠파이프 | 8 → 24 | `ref/player_riot_pipe_attack_ref.png`, `ref/player_riot_pipe_idle0_ref.png`, 받은 `player_summer_pipe_attack_sheet.png` |
| 18 | `player_riot_baton_attack` | 무기 공격 | 경찰봉 | 8 → 24 | `ref/player_riot_baton_attack_ref.png`, `ref/player_riot_baton_idle0_ref.png`, 받은 `player_summer_baton_attack_sheet.png` |
| 19 | `player_riot_mop_attack` | 무기 공격 | 대걸레 | 20 → 24 | `ref/player_riot_mop_attack_ref.png`, `ref/player_riot_mop_idle0_ref.png`, 받은 `player_summer_mop_attack_sheet.png` |
| 20 | `player_riot_none_crouch` | 웅크려 걷기 | 맨손 | 16 → 24 | `ref/player_riot_none_crouch_ref.png`, `ref/player_riot_none_idle0_ref.png`, 받은 `player_summer_none_crouch_sheet.png` |
| 21 | `player_riot_bat_crouch` | 웅크려 걷기 | 야구방망이 | 16 → 24 | `ref/player_riot_bat_crouch_ref.png`, `ref/player_riot_bat_idle0_ref.png`, 받은 `player_summer_bat_crouch_sheet.png` |
| 22 | `player_riot_pipe_crouch` | 웅크려 걷기 | 쇠파이프 | 16 → 24 | `ref/player_riot_pipe_crouch_ref.png`, `ref/player_riot_pipe_idle0_ref.png`, 받은 `player_summer_pipe_crouch_sheet.png` |
| 23 | `player_riot_baton_crouch` | 웅크려 걷기 | 경찰봉 | 16 → 24 | `ref/player_riot_baton_crouch_ref.png`, `ref/player_riot_baton_idle0_ref.png`, 받은 `player_summer_baton_crouch_sheet.png` |
| 24 | `player_riot_mop_crouch` | 웅크려 걷기 | 대걸레 | 16 → 24 | `ref/player_riot_mop_crouch_ref.png`, `ref/player_riot_mop_idle0_ref.png`, 받은 `player_summer_mop_crouch_sheet.png` |
| 25 | `player_riot_none_exhausted` | 지침 | 맨손 | 12 → 24 | `ref/player_riot_none_exhausted_ref.png`, `ref/player_riot_none_idle0_ref.png`, 받은 `player_summer_none_exhausted_sheet.png` |
| 26 | `player_riot_bat_exhausted` | 지침 | 야구방망이 | 12 → 24 | `ref/player_riot_bat_exhausted_ref.png`, `ref/player_riot_bat_idle0_ref.png`, 받은 `player_summer_bat_exhausted_sheet.png` |
| 27 | `player_riot_pipe_exhausted` | 지침 | 쇠파이프 | 12 → 24 | `ref/player_riot_pipe_exhausted_ref.png`, `ref/player_riot_pipe_idle0_ref.png`, 받은 `player_summer_pipe_exhausted_sheet.png` |
| 28 | `player_riot_baton_exhausted` | 지침 | 경찰봉 | 12 → 24 | `ref/player_riot_baton_exhausted_ref.png`, `ref/player_riot_baton_idle0_ref.png`, 받은 `player_summer_baton_exhausted_sheet.png` |
| 29 | `player_riot_mop_exhausted` | 지침 | 대걸레 | 12 → 24 | `ref/player_riot_mop_exhausted_ref.png`, `ref/player_riot_mop_idle0_ref.png`, 받은 `player_summer_mop_exhausted_sheet.png` |
| 30 | `player_riot_none_hurt` | 다침 | 맨손 | 8 → 24 | `ref/player_riot_none_hurt_ref.png`, `ref/player_riot_none_idle0_ref.png`, 받은 `player_summer_none_hurt_sheet.png` |
| 31 | `player_riot_bat_hurt` | 다침 | 야구방망이 | 8 → 24 | `ref/player_riot_bat_hurt_ref.png`, `ref/player_riot_bat_idle0_ref.png`, 받은 `player_summer_bat_hurt_sheet.png` |
| 32 | `player_riot_pipe_hurt` | 다침 | 쇠파이프 | 8 → 24 | `ref/player_riot_pipe_hurt_ref.png`, `ref/player_riot_pipe_idle0_ref.png`, 받은 `player_summer_pipe_hurt_sheet.png` |
| 33 | `player_riot_baton_hurt` | 다침 | 경찰봉 | 8 → 24 | `ref/player_riot_baton_hurt_ref.png`, `ref/player_riot_baton_idle0_ref.png`, 받은 `player_summer_baton_hurt_sheet.png` |
| 34 | `player_riot_mop_hurt` | 다침 | 대걸레 | 8 → 24 | `ref/player_riot_mop_hurt_ref.png`, `ref/player_riot_mop_idle0_ref.png`, 받은 `player_summer_mop_hurt_sheet.png` |
| 35 | `player_riot_none_shove` | 밀치기 | 맨손 | 20 → 24 | `ref/player_riot_none_shove_ref.png`, `ref/player_riot_none_idle0_ref.png`, 받은 `player_summer_none_shove_sheet.png` |
| 36 | `player_riot_none_search` | 수색 | 맨손 | 12 → 24 | `ref/player_riot_none_search_ref.png`, `ref/player_riot_none_idle0_ref.png`, 받은 `player_summer_none_search_sheet.png` |
| 37 | `player_riot_none_hide` | 숨기 | 맨손 | 8 → 24 | `ref/player_riot_none_hide_ref.png`, `ref/player_riot_none_idle0_ref.png`, 받은 `player_summer_none_hide_sheet.png` |
| 38 | `player_riot_none_doze` | 잠듦 | 맨손 | 12 → 24 | `ref/player_riot_none_doze_ref.png`, `ref/player_riot_none_idle0_ref.png`, 받은 `player_summer_none_doze_sheet.png` |

### 5.6 기본 교복(예비) `uniform` (38묶음)

| # | 묶음 | 동작 | 무기 | 지금 → 새로 | 참고 이미지 |
|---|---|---|---|---|---|
| 1 | `player_uniform_none_walk` | 걷기 | 맨손 | 16 → 24 | `ref/player_uniform_none_walk_ref.png`, `ref/player_uniform_none_idle0_ref.png`, 받은 `player_summer_none_walk_sheet.png` |
| 2 | `player_uniform_bat_walk` | 걷기 | 야구방망이 | 16 → 24 | `ref/player_uniform_bat_walk_ref.png`, `ref/player_uniform_bat_idle0_ref.png`, 받은 `player_summer_bat_walk_sheet.png` |
| 3 | `player_uniform_pipe_walk` | 걷기 | 쇠파이프 | 16 → 24 | `ref/player_uniform_pipe_walk_ref.png`, `ref/player_uniform_pipe_idle0_ref.png`, 받은 `player_summer_pipe_walk_sheet.png` |
| 4 | `player_uniform_baton_walk` | 걷기 | 경찰봉 | 16 → 24 | `ref/player_uniform_baton_walk_ref.png`, `ref/player_uniform_baton_idle0_ref.png`, 받은 `player_summer_baton_walk_sheet.png` |
| 5 | `player_uniform_mop_walk` | 걷기 | 대걸레 | 16 → 24 | `ref/player_uniform_mop_walk_ref.png`, `ref/player_uniform_mop_idle0_ref.png`, 받은 `player_summer_mop_walk_sheet.png` |
| 6 | `player_uniform_none_idle` | 대기 | 맨손 | 16 → 24 | `ref/player_uniform_none_idle_ref.png`, 받은 `player_summer_none_idle_sheet.png` |
| 7 | `player_uniform_bat_idle` | 대기 | 야구방망이 | 16 → 24 | `ref/player_uniform_bat_idle_ref.png`, 받은 `player_summer_bat_idle_sheet.png` |
| 8 | `player_uniform_pipe_idle` | 대기 | 쇠파이프 | 16 → 24 | `ref/player_uniform_pipe_idle_ref.png`, 받은 `player_summer_pipe_idle_sheet.png` |
| 9 | `player_uniform_baton_idle` | 대기 | 경찰봉 | 16 → 24 | `ref/player_uniform_baton_idle_ref.png`, 받은 `player_summer_baton_idle_sheet.png` |
| 10 | `player_uniform_mop_idle` | 대기 | 대걸레 | 16 → 24 | `ref/player_uniform_mop_idle_ref.png`, 받은 `player_summer_mop_idle_sheet.png` |
| 11 | `player_uniform_none_run` | 달리기 | 맨손 | 16 → 24 | `ref/player_uniform_none_run_ref.png`, `ref/player_uniform_none_idle0_ref.png`, 받은 `player_summer_none_run_sheet.png` |
| 12 | `player_uniform_bat_run` | 달리기 | 야구방망이 | 16 → 24 | `ref/player_uniform_bat_run_ref.png`, `ref/player_uniform_bat_idle0_ref.png`, 받은 `player_summer_bat_run_sheet.png` |
| 13 | `player_uniform_pipe_run` | 달리기 | 쇠파이프 | 16 → 24 | `ref/player_uniform_pipe_run_ref.png`, `ref/player_uniform_pipe_idle0_ref.png`, 받은 `player_summer_pipe_run_sheet.png` |
| 14 | `player_uniform_baton_run` | 달리기 | 경찰봉 | 16 → 24 | `ref/player_uniform_baton_run_ref.png`, `ref/player_uniform_baton_idle0_ref.png`, 받은 `player_summer_baton_run_sheet.png` |
| 15 | `player_uniform_mop_run` | 달리기 | 대걸레 | 16 → 24 | `ref/player_uniform_mop_run_ref.png`, `ref/player_uniform_mop_idle0_ref.png`, 받은 `player_summer_mop_run_sheet.png` |
| 16 | `player_uniform_bat_attack` | 무기 공격 | 야구방망이 | 8 → 24 | `ref/player_uniform_bat_attack_ref.png`, `ref/player_uniform_bat_idle0_ref.png`, 받은 `player_summer_bat_attack_sheet.png` |
| 17 | `player_uniform_pipe_attack` | 무기 공격 | 쇠파이프 | 8 → 24 | `ref/player_uniform_pipe_attack_ref.png`, `ref/player_uniform_pipe_idle0_ref.png`, 받은 `player_summer_pipe_attack_sheet.png` |
| 18 | `player_uniform_baton_attack` | 무기 공격 | 경찰봉 | 8 → 24 | `ref/player_uniform_baton_attack_ref.png`, `ref/player_uniform_baton_idle0_ref.png`, 받은 `player_summer_baton_attack_sheet.png` |
| 19 | `player_uniform_mop_attack` | 무기 공격 | 대걸레 | 20 → 24 | `ref/player_uniform_mop_attack_ref.png`, `ref/player_uniform_mop_idle0_ref.png`, 받은 `player_summer_mop_attack_sheet.png` |
| 20 | `player_uniform_none_crouch` | 웅크려 걷기 | 맨손 | 16 → 24 | `ref/player_uniform_none_crouch_ref.png`, `ref/player_uniform_none_idle0_ref.png`, 받은 `player_summer_none_crouch_sheet.png` |
| 21 | `player_uniform_bat_crouch` | 웅크려 걷기 | 야구방망이 | 16 → 24 | `ref/player_uniform_bat_crouch_ref.png`, `ref/player_uniform_bat_idle0_ref.png`, 받은 `player_summer_bat_crouch_sheet.png` |
| 22 | `player_uniform_pipe_crouch` | 웅크려 걷기 | 쇠파이프 | 16 → 24 | `ref/player_uniform_pipe_crouch_ref.png`, `ref/player_uniform_pipe_idle0_ref.png`, 받은 `player_summer_pipe_crouch_sheet.png` |
| 23 | `player_uniform_baton_crouch` | 웅크려 걷기 | 경찰봉 | 16 → 24 | `ref/player_uniform_baton_crouch_ref.png`, `ref/player_uniform_baton_idle0_ref.png`, 받은 `player_summer_baton_crouch_sheet.png` |
| 24 | `player_uniform_mop_crouch` | 웅크려 걷기 | 대걸레 | 16 → 24 | `ref/player_uniform_mop_crouch_ref.png`, `ref/player_uniform_mop_idle0_ref.png`, 받은 `player_summer_mop_crouch_sheet.png` |
| 25 | `player_uniform_none_exhausted` | 지침 | 맨손 | 12 → 24 | `ref/player_uniform_none_exhausted_ref.png`, `ref/player_uniform_none_idle0_ref.png`, 받은 `player_summer_none_exhausted_sheet.png` |
| 26 | `player_uniform_bat_exhausted` | 지침 | 야구방망이 | 12 → 24 | `ref/player_uniform_bat_exhausted_ref.png`, `ref/player_uniform_bat_idle0_ref.png`, 받은 `player_summer_bat_exhausted_sheet.png` |
| 27 | `player_uniform_pipe_exhausted` | 지침 | 쇠파이프 | 12 → 24 | `ref/player_uniform_pipe_exhausted_ref.png`, `ref/player_uniform_pipe_idle0_ref.png`, 받은 `player_summer_pipe_exhausted_sheet.png` |
| 28 | `player_uniform_baton_exhausted` | 지침 | 경찰봉 | 12 → 24 | `ref/player_uniform_baton_exhausted_ref.png`, `ref/player_uniform_baton_idle0_ref.png`, 받은 `player_summer_baton_exhausted_sheet.png` |
| 29 | `player_uniform_mop_exhausted` | 지침 | 대걸레 | 12 → 24 | `ref/player_uniform_mop_exhausted_ref.png`, `ref/player_uniform_mop_idle0_ref.png`, 받은 `player_summer_mop_exhausted_sheet.png` |
| 30 | `player_uniform_none_hurt` | 다침 | 맨손 | 8 → 24 | `ref/player_uniform_none_hurt_ref.png`, `ref/player_uniform_none_idle0_ref.png`, 받은 `player_summer_none_hurt_sheet.png` |
| 31 | `player_uniform_bat_hurt` | 다침 | 야구방망이 | 8 → 24 | `ref/player_uniform_bat_hurt_ref.png`, `ref/player_uniform_bat_idle0_ref.png`, 받은 `player_summer_bat_hurt_sheet.png` |
| 32 | `player_uniform_pipe_hurt` | 다침 | 쇠파이프 | 8 → 24 | `ref/player_uniform_pipe_hurt_ref.png`, `ref/player_uniform_pipe_idle0_ref.png`, 받은 `player_summer_pipe_hurt_sheet.png` |
| 33 | `player_uniform_baton_hurt` | 다침 | 경찰봉 | 8 → 24 | `ref/player_uniform_baton_hurt_ref.png`, `ref/player_uniform_baton_idle0_ref.png`, 받은 `player_summer_baton_hurt_sheet.png` |
| 34 | `player_uniform_mop_hurt` | 다침 | 대걸레 | 8 → 24 | `ref/player_uniform_mop_hurt_ref.png`, `ref/player_uniform_mop_idle0_ref.png`, 받은 `player_summer_mop_hurt_sheet.png` |
| 35 | `player_uniform_none_shove` | 밀치기 | 맨손 | 20 → 24 | `ref/player_uniform_none_shove_ref.png`, `ref/player_uniform_none_idle0_ref.png`, 받은 `player_summer_none_shove_sheet.png` |
| 36 | `player_uniform_none_search` | 수색 | 맨손 | 12 → 24 | `ref/player_uniform_none_search_ref.png`, `ref/player_uniform_none_idle0_ref.png`, 받은 `player_summer_none_search_sheet.png` |
| 37 | `player_uniform_none_hide` | 숨기 | 맨손 | 8 → 24 | `ref/player_uniform_none_hide_ref.png`, `ref/player_uniform_none_idle0_ref.png`, 받은 `player_summer_none_hide_sheet.png` |
| 38 | `player_uniform_none_doze` | 잠듦 | 맨손 | 12 → 24 | `ref/player_uniform_none_doze_ref.png`, `ref/player_uniform_none_idle0_ref.png`, 받은 `player_summer_none_doze_sheet.png` |

## 6. 프롬프트 (여름 교복 전부)

다른 옷은 `prompts/{묶음}.txt`에 같은 글이 `Character:` 줄만 바뀌어 들어 있다. 옷 문구는 아래 6.0 표.

### 6.0 옷 문구 (`Character:` 줄 앞부분)

| 옷 | 문구 |
|---|---|
| `summer` | `a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers` |
| `winter` | `a 16-year-old Korean schoolgirl with shoulder-length straight black hair in the winter uniform: a long brown duffle coat over the navy uniform, dark tights and dark sneakers` |
| `padding` | `a 16-year-old Korean schoolgirl with shoulder-length straight black hair in a puffy olive-green padded jacket over her uniform, dark trousers and sneakers` |
| `guard` | `a 16-year-old Korean schoolgirl with shoulder-length straight black hair in a red and black sports protective vest with elbow and knee guards over a dark tracksuit` |
| `riot` | `a 16-year-old Korean schoolgirl with shoulder-length straight black hair in an oversized black riot-police style padded suit, dark gloves, no helmet, her own black hair visible` |
| `uniform` | `a 16-year-old Korean schoolgirl with shoulder-length straight black hair in a navy school blazer, knee-length navy skirt, dark tights and sneakers` |

### 6.1 `player_summer_none_walk` — 걷기 · 맨손 (16 → 24)

```
Sprite sheet for a side-scrolling 2D survival game, side-view 2D pixel art, crisp chunky pixels, limited muted palette, 1-pixel dark outline, no anti-aliasing, no blur, orthographic side view.
Grid of 24 equal cells: 6 columns x 4 rows, read left to right then top to bottom, no gaps, no borders, no numbers, no text. Each cell is one animation frame of the same character facing right, feet on the same floor baseline near the bottom of every cell, body centered horizontally. Transparent background.

Character: a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers. Match the attached reference frames exactly: same face, same hair, same outfit, same colors, same shading style, same proportions, same pixel scale. Do not draw a backpack.

Animation: walking forward at a steady pace, empty-handed, one full stride cycle (left step, then right step), natural leg movement and a small up-down body bob. Seamless loop: cell 24 flows smoothly back into cell 1.
The reference strip has 16 frames. Keep the odd reference frames 1, 3, 5, ..., 15 unchanged as keyframes in cells 1, 4, 7, 10, 13, 16, 19 and 22, and draw two in-between frames between each pair of keyframes; the even reference frames show the pose halfway between, use them as a guide. Cells 23-24 are the in-betweens from reference frame 15 back to reference frame 1 (reference frame 16 is the guide).

Exactly one character in every frame, never a second person or a ghost copy. Her hands are empty in every frame. No motion-blur smears, no speed lines, no smoke, no sparkles, no floating lines, no stray pixels, no detached blobs.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

### 6.2 `player_summer_bat_walk` — 걷기 · 야구방망이 (16 → 24)

```
Sprite sheet for a side-scrolling 2D survival game, side-view 2D pixel art, crisp chunky pixels, limited muted palette, 1-pixel dark outline, no anti-aliasing, no blur, orthographic side view.
Grid of 24 equal cells: 6 columns x 4 rows, read left to right then top to bottom, no gaps, no borders, no numbers, no text. Each cell is one animation frame of the same character facing right, feet on the same floor baseline near the bottom of every cell, body centered horizontally. Transparent background.

Character: a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers, holding a wooden baseball bat exactly the way she holds it in the reference frames. Match the attached reference frames exactly: same face, same hair, same outfit, same colors, same shading style, same proportions, same pixel scale, same weapon. Do not draw a backpack.

Animation: walking forward at a steady pace, holding a wooden baseball bat exactly the way she holds it in the reference frames, one full stride cycle (left step, then right step), natural leg movement and a small up-down body bob. Seamless loop: cell 24 flows smoothly back into cell 1.
The reference strip has 16 frames. Keep the odd reference frames 1, 3, 5, ..., 15 unchanged as keyframes in cells 1, 4, 7, 10, 13, 16, 19 and 22, and draw two in-between frames between each pair of keyframes; the even reference frames show the pose halfway between, use them as a guide. Cells 23-24 are the in-betweens from reference frame 15 back to reference frame 1 (reference frame 16 is the guide).

Exactly one character in every frame, never a second person or a ghost copy. The baseball bat keeps the exact same length and shape in every frame; it never stretches, grows or bends. No motion-blur smears, no speed lines, no smoke, no sparkles, no floating lines, no stray pixels, no detached blobs.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

### 6.3 `player_summer_pipe_walk` — 걷기 · 쇠파이프 (16 → 24)

```
Sprite sheet for a side-scrolling 2D survival game, side-view 2D pixel art, crisp chunky pixels, limited muted palette, 1-pixel dark outline, no anti-aliasing, no blur, orthographic side view.
Grid of 24 equal cells: 6 columns x 4 rows, read left to right then top to bottom, no gaps, no borders, no numbers, no text. Each cell is one animation frame of the same character facing right, feet on the same floor baseline near the bottom of every cell, body centered horizontally. Transparent background.

Character: a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers, holding a rusty steel pipe exactly the way she holds it in the reference frames. Match the attached reference frames exactly: same face, same hair, same outfit, same colors, same shading style, same proportions, same pixel scale, same weapon. Do not draw a backpack.

Animation: walking forward at a steady pace, holding a rusty steel pipe exactly the way she holds it in the reference frames, one full stride cycle (left step, then right step), natural leg movement and a small up-down body bob. Seamless loop: cell 24 flows smoothly back into cell 1.
The reference strip has 16 frames. Keep the odd reference frames 1, 3, 5, ..., 15 unchanged as keyframes in cells 1, 4, 7, 10, 13, 16, 19 and 22, and draw two in-between frames between each pair of keyframes; the even reference frames show the pose halfway between, use them as a guide. Cells 23-24 are the in-betweens from reference frame 15 back to reference frame 1 (reference frame 16 is the guide).

Exactly one character in every frame, never a second person or a ghost copy. The steel pipe keeps the exact same length and shape in every frame; it never stretches, grows or bends. No motion-blur smears, no speed lines, no smoke, no sparkles, no floating lines, no stray pixels, no detached blobs.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

### 6.4 `player_summer_baton_walk` — 걷기 · 경찰봉 (16 → 24)

```
Sprite sheet for a side-scrolling 2D survival game, side-view 2D pixel art, crisp chunky pixels, limited muted palette, 1-pixel dark outline, no anti-aliasing, no blur, orthographic side view.
Grid of 24 equal cells: 6 columns x 4 rows, read left to right then top to bottom, no gaps, no borders, no numbers, no text. Each cell is one animation frame of the same character facing right, feet on the same floor baseline near the bottom of every cell, body centered horizontally. Transparent background.

Character: a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers, holding a black police baton exactly the way she holds it in the reference frames. Match the attached reference frames exactly: same face, same hair, same outfit, same colors, same shading style, same proportions, same pixel scale, same weapon. Do not draw a backpack.

Animation: walking forward at a steady pace, holding a black police baton exactly the way she holds it in the reference frames, one full stride cycle (left step, then right step), natural leg movement and a small up-down body bob. Seamless loop: cell 24 flows smoothly back into cell 1.
The reference strip has 16 frames. Keep the odd reference frames 1, 3, 5, ..., 15 unchanged as keyframes in cells 1, 4, 7, 10, 13, 16, 19 and 22, and draw two in-between frames between each pair of keyframes; the even reference frames show the pose halfway between, use them as a guide. Cells 23-24 are the in-betweens from reference frame 15 back to reference frame 1 (reference frame 16 is the guide).

Exactly one character in every frame, never a second person or a ghost copy. The police baton keeps the exact same length and shape in every frame; it never stretches, grows or bends. No motion-blur smears, no speed lines, no smoke, no sparkles, no floating lines, no stray pixels, no detached blobs.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

### 6.5 `player_summer_mop_walk` — 걷기 · 대걸레 (16 → 24)

```
Sprite sheet for a side-scrolling 2D survival game, side-view 2D pixel art, crisp chunky pixels, limited muted palette, 1-pixel dark outline, no anti-aliasing, no blur, orthographic side view.
Grid of 24 equal cells: 6 columns x 4 rows, read left to right then top to bottom, no gaps, no borders, no numbers, no text. Each cell is one animation frame of the same character facing right, feet on the same floor baseline near the bottom of every cell, body centered horizontally. Transparent background.

Character: a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers, holding a wooden mop with a grey-white cotton mop head exactly the way she holds it in the reference frames. Match the attached reference frames exactly: same face, same hair, same outfit, same colors, same shading style, same proportions, same pixel scale, same weapon. Do not draw a backpack.

Animation: walking forward at a steady pace, holding a wooden mop with a grey-white cotton mop head exactly the way she holds it in the reference frames, one full stride cycle (left step, then right step), natural leg movement and a small up-down body bob. Seamless loop: cell 24 flows smoothly back into cell 1.
The reference strip has 16 frames. Keep the odd reference frames 1, 3, 5, ..., 15 unchanged as keyframes in cells 1, 4, 7, 10, 13, 16, 19 and 22, and draw two in-between frames between each pair of keyframes; the even reference frames show the pose halfway between, use them as a guide. Cells 23-24 are the in-betweens from reference frame 15 back to reference frame 1 (reference frame 16 is the guide).

Exactly one character in every frame, never a second person or a ghost copy. The mop keeps the exact same length and shape in every frame; it never stretches, grows or bends. The mop head stays a compact bundle of cotton strands hanging down, never fanned out, never pointing up. No motion-blur smears, no speed lines, no smoke, no sparkles, no floating lines, no stray pixels, no detached blobs.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

### 6.6 `player_summer_none_idle` — 대기 · 맨손 (16 → 24)

```
Sprite sheet for a side-scrolling 2D survival game, side-view 2D pixel art, crisp chunky pixels, limited muted palette, 1-pixel dark outline, no anti-aliasing, no blur, orthographic side view.
Grid of 24 equal cells: 6 columns x 4 rows, read left to right then top to bottom, no gaps, no borders, no numbers, no text. Each cell is one animation frame of the same character facing right, feet on the same floor baseline near the bottom of every cell, body centered horizontally. Transparent background.

Character: a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers. Match the attached reference frames exactly: same face, same hair, same outfit, same colors, same shading style, same proportions, same pixel scale. Do not draw a backpack.

Animation: standing idle and breathing slowly, empty-handed; only a gentle shoulder rise and fall and a tiny weight shift, the feet never move. Seamless loop: cell 24 flows smoothly back into cell 1.
The reference strip has 16 frames. Keep the odd reference frames 1, 3, 5, ..., 15 unchanged as keyframes in cells 1, 4, 7, 10, 13, 16, 19 and 22, and draw two in-between frames between each pair of keyframes; the even reference frames show the pose halfway between, use them as a guide. Cells 23-24 are the in-betweens from reference frame 15 back to reference frame 1 (reference frame 16 is the guide).

Exactly one character in every frame, never a second person or a ghost copy. Her hands are empty in every frame. No motion-blur smears, no speed lines, no smoke, no sparkles, no floating lines, no stray pixels, no detached blobs.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

### 6.7 `player_summer_bat_idle` — 대기 · 야구방망이 (16 → 24)

```
Sprite sheet for a side-scrolling 2D survival game, side-view 2D pixel art, crisp chunky pixels, limited muted palette, 1-pixel dark outline, no anti-aliasing, no blur, orthographic side view.
Grid of 24 equal cells: 6 columns x 4 rows, read left to right then top to bottom, no gaps, no borders, no numbers, no text. Each cell is one animation frame of the same character facing right, feet on the same floor baseline near the bottom of every cell, body centered horizontally. Transparent background.

Character: a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers, holding a wooden baseball bat exactly the way she holds it in the reference frames. Match the attached reference frames exactly: same face, same hair, same outfit, same colors, same shading style, same proportions, same pixel scale, same weapon. Do not draw a backpack.

Animation: standing idle and breathing slowly, holding a wooden baseball bat exactly the way she holds it in the reference frames; only a gentle shoulder rise and fall and a tiny weight shift, the feet never move. Seamless loop: cell 24 flows smoothly back into cell 1.
The reference strip has 16 frames. Keep the odd reference frames 1, 3, 5, ..., 15 unchanged as keyframes in cells 1, 4, 7, 10, 13, 16, 19 and 22, and draw two in-between frames between each pair of keyframes; the even reference frames show the pose halfway between, use them as a guide. Cells 23-24 are the in-betweens from reference frame 15 back to reference frame 1 (reference frame 16 is the guide).

Exactly one character in every frame, never a second person or a ghost copy. The baseball bat keeps the exact same length and shape in every frame; it never stretches, grows or bends. No motion-blur smears, no speed lines, no smoke, no sparkles, no floating lines, no stray pixels, no detached blobs.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

### 6.8 `player_summer_pipe_idle` — 대기 · 쇠파이프 (16 → 24)

```
Sprite sheet for a side-scrolling 2D survival game, side-view 2D pixel art, crisp chunky pixels, limited muted palette, 1-pixel dark outline, no anti-aliasing, no blur, orthographic side view.
Grid of 24 equal cells: 6 columns x 4 rows, read left to right then top to bottom, no gaps, no borders, no numbers, no text. Each cell is one animation frame of the same character facing right, feet on the same floor baseline near the bottom of every cell, body centered horizontally. Transparent background.

Character: a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers, holding a rusty steel pipe exactly the way she holds it in the reference frames. Match the attached reference frames exactly: same face, same hair, same outfit, same colors, same shading style, same proportions, same pixel scale, same weapon. Do not draw a backpack.

Animation: standing idle and breathing slowly, holding a rusty steel pipe exactly the way she holds it in the reference frames; only a gentle shoulder rise and fall and a tiny weight shift, the feet never move. Seamless loop: cell 24 flows smoothly back into cell 1.
The reference strip has 16 frames. Keep the odd reference frames 1, 3, 5, ..., 15 unchanged as keyframes in cells 1, 4, 7, 10, 13, 16, 19 and 22, and draw two in-between frames between each pair of keyframes; the even reference frames show the pose halfway between, use them as a guide. Cells 23-24 are the in-betweens from reference frame 15 back to reference frame 1 (reference frame 16 is the guide).

Exactly one character in every frame, never a second person or a ghost copy. The steel pipe keeps the exact same length and shape in every frame; it never stretches, grows or bends. No motion-blur smears, no speed lines, no smoke, no sparkles, no floating lines, no stray pixels, no detached blobs.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

### 6.9 `player_summer_baton_idle` — 대기 · 경찰봉 (16 → 24)

```
Sprite sheet for a side-scrolling 2D survival game, side-view 2D pixel art, crisp chunky pixels, limited muted palette, 1-pixel dark outline, no anti-aliasing, no blur, orthographic side view.
Grid of 24 equal cells: 6 columns x 4 rows, read left to right then top to bottom, no gaps, no borders, no numbers, no text. Each cell is one animation frame of the same character facing right, feet on the same floor baseline near the bottom of every cell, body centered horizontally. Transparent background.

Character: a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers, holding a black police baton exactly the way she holds it in the reference frames. Match the attached reference frames exactly: same face, same hair, same outfit, same colors, same shading style, same proportions, same pixel scale, same weapon. Do not draw a backpack.

Animation: standing idle and breathing slowly, holding a black police baton exactly the way she holds it in the reference frames; only a gentle shoulder rise and fall and a tiny weight shift, the feet never move. Seamless loop: cell 24 flows smoothly back into cell 1.
The reference strip has 16 frames. Keep the odd reference frames 1, 3, 5, ..., 15 unchanged as keyframes in cells 1, 4, 7, 10, 13, 16, 19 and 22, and draw two in-between frames between each pair of keyframes; the even reference frames show the pose halfway between, use them as a guide. Cells 23-24 are the in-betweens from reference frame 15 back to reference frame 1 (reference frame 16 is the guide).

Exactly one character in every frame, never a second person or a ghost copy. The police baton keeps the exact same length and shape in every frame; it never stretches, grows or bends. No motion-blur smears, no speed lines, no smoke, no sparkles, no floating lines, no stray pixels, no detached blobs.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

### 6.10 `player_summer_mop_idle` — 대기 · 대걸레 (16 → 24)

```
Sprite sheet for a side-scrolling 2D survival game, side-view 2D pixel art, crisp chunky pixels, limited muted palette, 1-pixel dark outline, no anti-aliasing, no blur, orthographic side view.
Grid of 24 equal cells: 6 columns x 4 rows, read left to right then top to bottom, no gaps, no borders, no numbers, no text. Each cell is one animation frame of the same character facing right, feet on the same floor baseline near the bottom of every cell, body centered horizontally. Transparent background.

Character: a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers, holding a wooden mop with a grey-white cotton mop head exactly the way she holds it in the reference frames. Match the attached reference frames exactly: same face, same hair, same outfit, same colors, same shading style, same proportions, same pixel scale, same weapon. Do not draw a backpack.

Animation: standing idle and breathing slowly, holding a wooden mop with a grey-white cotton mop head exactly the way she holds it in the reference frames; only a gentle shoulder rise and fall and a tiny weight shift, the feet never move. Seamless loop: cell 24 flows smoothly back into cell 1.
The reference strip has 16 frames. Keep the odd reference frames 1, 3, 5, ..., 15 unchanged as keyframes in cells 1, 4, 7, 10, 13, 16, 19 and 22, and draw two in-between frames between each pair of keyframes; the even reference frames show the pose halfway between, use them as a guide. Cells 23-24 are the in-betweens from reference frame 15 back to reference frame 1 (reference frame 16 is the guide).

Exactly one character in every frame, never a second person or a ghost copy. The mop keeps the exact same length and shape in every frame; it never stretches, grows or bends. The mop head stays a compact bundle of cotton strands hanging down, never fanned out, never pointing up. No motion-blur smears, no speed lines, no smoke, no sparkles, no floating lines, no stray pixels, no detached blobs.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

### 6.11 `player_summer_none_run` — 달리기 · 맨손 (16 → 24)

```
Sprite sheet for a side-scrolling 2D survival game, side-view 2D pixel art, crisp chunky pixels, limited muted palette, 1-pixel dark outline, no anti-aliasing, no blur, orthographic side view.
Grid of 24 equal cells: 6 columns x 4 rows, read left to right then top to bottom, no gaps, no borders, no numbers, no text. Each cell is one animation frame of the same character facing right, feet on the same floor baseline near the bottom of every cell, body centered horizontally. Transparent background.

Character: a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers. Match the attached reference frames exactly: same face, same hair, same outfit, same colors, same shading style, same proportions, same pixel scale. Do not draw a backpack.

Animation: running forward, leaning slightly forward, empty-handed, one full stride cycle (two steps), both feet briefly off the floor at the top of each stride but the lowest foot always comes back to the same floor baseline. Seamless loop: cell 24 flows smoothly back into cell 1.
The reference strip has 16 frames. Keep the odd reference frames 1, 3, 5, ..., 15 unchanged as keyframes in cells 1, 4, 7, 10, 13, 16, 19 and 22, and draw two in-between frames between each pair of keyframes; the even reference frames show the pose halfway between, use them as a guide. Cells 23-24 are the in-betweens from reference frame 15 back to reference frame 1 (reference frame 16 is the guide).

Exactly one character in every frame, never a second person or a ghost copy. Her hands are empty in every frame. No motion-blur smears, no speed lines, no smoke, no sparkles, no floating lines, no stray pixels, no detached blobs.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

### 6.12 `player_summer_bat_run` — 달리기 · 야구방망이 (16 → 24)

```
Sprite sheet for a side-scrolling 2D survival game, side-view 2D pixel art, crisp chunky pixels, limited muted palette, 1-pixel dark outline, no anti-aliasing, no blur, orthographic side view.
Grid of 24 equal cells: 6 columns x 4 rows, read left to right then top to bottom, no gaps, no borders, no numbers, no text. Each cell is one animation frame of the same character facing right, feet on the same floor baseline near the bottom of every cell, body centered horizontally. Transparent background.

Character: a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers, holding a wooden baseball bat exactly the way she holds it in the reference frames. Match the attached reference frames exactly: same face, same hair, same outfit, same colors, same shading style, same proportions, same pixel scale, same weapon. Do not draw a backpack.

Animation: running forward, leaning slightly forward, holding a wooden baseball bat exactly the way she holds it in the reference frames, one full stride cycle (two steps), both feet briefly off the floor at the top of each stride but the lowest foot always comes back to the same floor baseline. Seamless loop: cell 24 flows smoothly back into cell 1.
The reference strip has 16 frames. Keep the odd reference frames 1, 3, 5, ..., 15 unchanged as keyframes in cells 1, 4, 7, 10, 13, 16, 19 and 22, and draw two in-between frames between each pair of keyframes; the even reference frames show the pose halfway between, use them as a guide. Cells 23-24 are the in-betweens from reference frame 15 back to reference frame 1 (reference frame 16 is the guide).

Exactly one character in every frame, never a second person or a ghost copy. The baseball bat keeps the exact same length and shape in every frame; it never stretches, grows or bends. No motion-blur smears, no speed lines, no smoke, no sparkles, no floating lines, no stray pixels, no detached blobs.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

### 6.13 `player_summer_pipe_run` — 달리기 · 쇠파이프 (16 → 24)

```
Sprite sheet for a side-scrolling 2D survival game, side-view 2D pixel art, crisp chunky pixels, limited muted palette, 1-pixel dark outline, no anti-aliasing, no blur, orthographic side view.
Grid of 24 equal cells: 6 columns x 4 rows, read left to right then top to bottom, no gaps, no borders, no numbers, no text. Each cell is one animation frame of the same character facing right, feet on the same floor baseline near the bottom of every cell, body centered horizontally. Transparent background.

Character: a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers, holding a rusty steel pipe exactly the way she holds it in the reference frames. Match the attached reference frames exactly: same face, same hair, same outfit, same colors, same shading style, same proportions, same pixel scale, same weapon. Do not draw a backpack.

Animation: running forward, leaning slightly forward, holding a rusty steel pipe exactly the way she holds it in the reference frames, one full stride cycle (two steps), both feet briefly off the floor at the top of each stride but the lowest foot always comes back to the same floor baseline. Seamless loop: cell 24 flows smoothly back into cell 1.
The reference strip has 16 frames. Keep the odd reference frames 1, 3, 5, ..., 15 unchanged as keyframes in cells 1, 4, 7, 10, 13, 16, 19 and 22, and draw two in-between frames between each pair of keyframes; the even reference frames show the pose halfway between, use them as a guide. Cells 23-24 are the in-betweens from reference frame 15 back to reference frame 1 (reference frame 16 is the guide).

Exactly one character in every frame, never a second person or a ghost copy. The steel pipe keeps the exact same length and shape in every frame; it never stretches, grows or bends. No motion-blur smears, no speed lines, no smoke, no sparkles, no floating lines, no stray pixels, no detached blobs.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

### 6.14 `player_summer_baton_run` — 달리기 · 경찰봉 (16 → 24)

```
Sprite sheet for a side-scrolling 2D survival game, side-view 2D pixel art, crisp chunky pixels, limited muted palette, 1-pixel dark outline, no anti-aliasing, no blur, orthographic side view.
Grid of 24 equal cells: 6 columns x 4 rows, read left to right then top to bottom, no gaps, no borders, no numbers, no text. Each cell is one animation frame of the same character facing right, feet on the same floor baseline near the bottom of every cell, body centered horizontally. Transparent background.

Character: a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers, holding a black police baton exactly the way she holds it in the reference frames. Match the attached reference frames exactly: same face, same hair, same outfit, same colors, same shading style, same proportions, same pixel scale, same weapon. Do not draw a backpack.

Animation: running forward, leaning slightly forward, holding a black police baton exactly the way she holds it in the reference frames, one full stride cycle (two steps), both feet briefly off the floor at the top of each stride but the lowest foot always comes back to the same floor baseline. Seamless loop: cell 24 flows smoothly back into cell 1.
The reference strip has 16 frames. Keep the odd reference frames 1, 3, 5, ..., 15 unchanged as keyframes in cells 1, 4, 7, 10, 13, 16, 19 and 22, and draw two in-between frames between each pair of keyframes; the even reference frames show the pose halfway between, use them as a guide. Cells 23-24 are the in-betweens from reference frame 15 back to reference frame 1 (reference frame 16 is the guide).

Exactly one character in every frame, never a second person or a ghost copy. The police baton keeps the exact same length and shape in every frame; it never stretches, grows or bends. No motion-blur smears, no speed lines, no smoke, no sparkles, no floating lines, no stray pixels, no detached blobs.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

### 6.15 `player_summer_mop_run` — 달리기 · 대걸레 (16 → 24)

```
Sprite sheet for a side-scrolling 2D survival game, side-view 2D pixel art, crisp chunky pixels, limited muted palette, 1-pixel dark outline, no anti-aliasing, no blur, orthographic side view.
Grid of 24 equal cells: 6 columns x 4 rows, read left to right then top to bottom, no gaps, no borders, no numbers, no text. Each cell is one animation frame of the same character facing right, feet on the same floor baseline near the bottom of every cell, body centered horizontally. Transparent background.

Character: a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers, holding a wooden mop with a grey-white cotton mop head exactly the way she holds it in the reference frames. Match the attached reference frames exactly: same face, same hair, same outfit, same colors, same shading style, same proportions, same pixel scale, same weapon. Do not draw a backpack.

Animation: running forward, leaning slightly forward, holding a wooden mop with a grey-white cotton mop head exactly the way she holds it in the reference frames, one full stride cycle (two steps), both feet briefly off the floor at the top of each stride but the lowest foot always comes back to the same floor baseline. Seamless loop: cell 24 flows smoothly back into cell 1.
The reference strip has 16 frames. Keep the odd reference frames 1, 3, 5, ..., 15 unchanged as keyframes in cells 1, 4, 7, 10, 13, 16, 19 and 22, and draw two in-between frames between each pair of keyframes; the even reference frames show the pose halfway between, use them as a guide. Cells 23-24 are the in-betweens from reference frame 15 back to reference frame 1 (reference frame 16 is the guide).

Exactly one character in every frame, never a second person or a ghost copy. The mop keeps the exact same length and shape in every frame; it never stretches, grows or bends. The mop head stays a compact bundle of cotton strands hanging down, never fanned out, never pointing up. No motion-blur smears, no speed lines, no smoke, no sparkles, no floating lines, no stray pixels, no detached blobs.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

### 6.16 `player_summer_pipe_attack` — 무기 공격 · 쇠파이프 (8 → 24)

```
Sprite sheet for a side-scrolling 2D survival game, side-view 2D pixel art, crisp chunky pixels, limited muted palette, 1-pixel dark outline, no anti-aliasing, no blur, orthographic side view.
Grid of 24 equal cells: 6 columns x 4 rows, read left to right then top to bottom, no gaps, no borders, no numbers, no text. Each cell is one animation frame of the same character facing right, feet on the same floor baseline near the bottom of every cell, body centered horizontally. Transparent background.

Character: a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers, holding a rusty steel pipe exactly the way she holds it in the reference frames. Match the attached reference frames exactly: same face, same hair, same outfit, same colors, same shading style, same proportions, same pixel scale, same weapon. Do not draw a backpack.

Animation: one heavy overhead two-handed strike with the pipe, a slower and heavier wind-up than a bat. Cells 1-7: she lifts the weapon higher above and behind her head, weight on the back foot, a short wind-up pause; cells 7-13: a fast downward arc, the weapon moves a lot between frames, ending in cell 13 at the impact pose - a big forward step, body thrown forward (the body moves forward, never backward), weapon in front of her at waist height; cells 13-16: a tiny impact hold; cells 16-22: follow-through into the low finishing pose at her waist; cells 23-24: she holds the finishing pose and her shoulders relax, feet do not move.
The reference strip has 8 frames. Keep reference frames 1-8 unchanged as keyframes in cells 1, 4, 7, 10, 13, 16, 19 and 22, and draw two in-between frames between each pair of keyframes.

Exactly one character in every frame, never a second person or a ghost copy. The steel pipe keeps the exact same length and shape in every frame; it never stretches, grows or bends. No motion-blur smears, no speed lines, no smoke, no sparkles, no floating lines, no stray pixels, no detached blobs.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

### 6.17 `player_summer_baton_attack` — 무기 공격 · 경찰봉 (8 → 24)

```
Sprite sheet for a side-scrolling 2D survival game, side-view 2D pixel art, crisp chunky pixels, limited muted palette, 1-pixel dark outline, no anti-aliasing, no blur, orthographic side view.
Grid of 24 equal cells: 6 columns x 4 rows, read left to right then top to bottom, no gaps, no borders, no numbers, no text. Each cell is one animation frame of the same character facing right, feet on the same floor baseline near the bottom of every cell, body centered horizontally. Transparent background.

Character: a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers, holding a black police baton exactly the way she holds it in the reference frames. Match the attached reference frames exactly: same face, same hair, same outfit, same colors, same shading style, same proportions, same pixel scale, same weapon. Do not draw a backpack.

Animation: one short and fast overhead strike with the baton. Cells 1-7: she lifts the weapon higher above and behind her head, weight on the back foot, a short wind-up pause; cells 7-13: a fast downward arc, the weapon moves a lot between frames, ending in cell 13 at the impact pose - a big forward step, body thrown forward (the body moves forward, never backward), weapon in front of her at waist height; cells 13-16: a tiny impact hold; cells 16-22: follow-through into the low finishing pose at her waist; cells 23-24: she holds the finishing pose and her shoulders relax, feet do not move.
The reference strip has 8 frames. Keep reference frames 1-8 unchanged as keyframes in cells 1, 4, 7, 10, 13, 16, 19 and 22, and draw two in-between frames between each pair of keyframes.

Exactly one character in every frame, never a second person or a ghost copy. The police baton keeps the exact same length and shape in every frame; it never stretches, grows or bends. No motion-blur smears, no speed lines, no smoke, no sparkles, no floating lines, no stray pixels, no detached blobs.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

### 6.18 `player_summer_mop_attack` — 무기 공격 · 대걸레 (20 → 24)

```
Sprite sheet for a side-scrolling 2D survival game, side-view 2D pixel art, crisp chunky pixels, limited muted palette, 1-pixel dark outline, no anti-aliasing, no blur, orthographic side view.
Grid of 24 equal cells: 6 columns x 4 rows, read left to right then top to bottom, no gaps, no borders, no numbers, no text. Each cell is one animation frame of the same character facing right, feet on the same floor baseline near the bottom of every cell, body centered horizontally. Transparent background.

Character: a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers, holding a wooden mop with a grey-white cotton mop head exactly the way she holds it in the reference frames. Match the attached reference frames exactly: same face, same hair, same outfit, same colors, same shading style, same proportions, same pixel scale, same weapon. Do not draw a backpack.

Animation: a two-handed spear-like thrust with the mop: she pulls the mop back, lunges forward with a big step and thrusts the mop head straight forward at knee-to-waist height, fully extended in cell 13 (the hit), then draws the mop back and settles into the ready pose by cell 22; cells 23-24 hold that pose with relaxed shoulders, feet do not move.
The reference strip has 20 frames. Keep reference frames 1, 3, 5, 7, 10, 13, 16, 20 unchanged as keyframes in cells 1, 4, 7, 10, 13, 16, 19, 22, and draw two in-between frames between each pair of keyframes; the other reference frames show the poses in between, use them as a guide.

Exactly one character in every frame, never a second person or a ghost copy. The mop keeps the exact same length and shape in every frame; it never stretches, grows or bends. The mop head stays a compact bundle of cotton strands hanging down, never fanned out, never pointing up. No motion-blur smears, no speed lines, no smoke, no sparkles, no floating lines, no stray pixels, no detached blobs.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

### 6.19 `player_summer_none_crouch` — 웅크려 걷기 · 맨손 (16 → 24)

```
Sprite sheet for a side-scrolling 2D survival game, side-view 2D pixel art, crisp chunky pixels, limited muted palette, 1-pixel dark outline, no anti-aliasing, no blur, orthographic side view.
Grid of 24 equal cells: 6 columns x 4 rows, read left to right then top to bottom, no gaps, no borders, no numbers, no text. Each cell is one animation frame of the same character facing right, feet on the same floor baseline near the bottom of every cell, body centered horizontally. Transparent background.

Character: a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers. Match the attached reference frames exactly: same face, same hair, same outfit, same colors, same shading style, same proportions, same pixel scale. Do not draw a backpack.

Animation: sneaking forward in a low crouch, knees deeply bent, back low, empty-handed, careful small steps with clearly visible leg movement. Cell 1 must also look natural as a still crouching pose, the game shows it alone when she stops. Seamless loop: cell 24 flows smoothly back into cell 1.
The reference strip has 16 frames. Keep the odd reference frames 1, 3, 5, ..., 15 unchanged as keyframes in cells 1, 4, 7, 10, 13, 16, 19 and 22, and draw two in-between frames between each pair of keyframes; the even reference frames show the pose halfway between, use them as a guide. Cells 23-24 are the in-betweens from reference frame 15 back to reference frame 1 (reference frame 16 is the guide).

Exactly one character in every frame, never a second person or a ghost copy. Her hands are empty in every frame. No motion-blur smears, no speed lines, no smoke, no sparkles, no floating lines, no stray pixels, no detached blobs.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

### 6.20 `player_summer_bat_crouch` — 웅크려 걷기 · 야구방망이 (16 → 24)

```
Sprite sheet for a side-scrolling 2D survival game, side-view 2D pixel art, crisp chunky pixels, limited muted palette, 1-pixel dark outline, no anti-aliasing, no blur, orthographic side view.
Grid of 24 equal cells: 6 columns x 4 rows, read left to right then top to bottom, no gaps, no borders, no numbers, no text. Each cell is one animation frame of the same character facing right, feet on the same floor baseline near the bottom of every cell, body centered horizontally. Transparent background.

Character: a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers, holding a wooden baseball bat exactly the way she holds it in the reference frames. Match the attached reference frames exactly: same face, same hair, same outfit, same colors, same shading style, same proportions, same pixel scale, same weapon. Do not draw a backpack.

Animation: sneaking forward in a low crouch, knees deeply bent, back low, holding a wooden baseball bat exactly the way she holds it in the reference frames, careful small steps with clearly visible leg movement. Cell 1 must also look natural as a still crouching pose, the game shows it alone when she stops. Seamless loop: cell 24 flows smoothly back into cell 1.
The reference strip has 16 frames. Keep the odd reference frames 1, 3, 5, ..., 15 unchanged as keyframes in cells 1, 4, 7, 10, 13, 16, 19 and 22, and draw two in-between frames between each pair of keyframes; the even reference frames show the pose halfway between, use them as a guide. Cells 23-24 are the in-betweens from reference frame 15 back to reference frame 1 (reference frame 16 is the guide).

Exactly one character in every frame, never a second person or a ghost copy. The baseball bat keeps the exact same length and shape in every frame; it never stretches, grows or bends. No motion-blur smears, no speed lines, no smoke, no sparkles, no floating lines, no stray pixels, no detached blobs.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

### 6.21 `player_summer_pipe_crouch` — 웅크려 걷기 · 쇠파이프 (16 → 24)

```
Sprite sheet for a side-scrolling 2D survival game, side-view 2D pixel art, crisp chunky pixels, limited muted palette, 1-pixel dark outline, no anti-aliasing, no blur, orthographic side view.
Grid of 24 equal cells: 6 columns x 4 rows, read left to right then top to bottom, no gaps, no borders, no numbers, no text. Each cell is one animation frame of the same character facing right, feet on the same floor baseline near the bottom of every cell, body centered horizontally. Transparent background.

Character: a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers, holding a rusty steel pipe exactly the way she holds it in the reference frames. Match the attached reference frames exactly: same face, same hair, same outfit, same colors, same shading style, same proportions, same pixel scale, same weapon. Do not draw a backpack.

Animation: sneaking forward in a low crouch, knees deeply bent, back low, holding a rusty steel pipe exactly the way she holds it in the reference frames, careful small steps with clearly visible leg movement. Cell 1 must also look natural as a still crouching pose, the game shows it alone when she stops. Seamless loop: cell 24 flows smoothly back into cell 1.
The reference strip has 16 frames. Keep the odd reference frames 1, 3, 5, ..., 15 unchanged as keyframes in cells 1, 4, 7, 10, 13, 16, 19 and 22, and draw two in-between frames between each pair of keyframes; the even reference frames show the pose halfway between, use them as a guide. Cells 23-24 are the in-betweens from reference frame 15 back to reference frame 1 (reference frame 16 is the guide).

Exactly one character in every frame, never a second person or a ghost copy. The steel pipe keeps the exact same length and shape in every frame; it never stretches, grows or bends. No motion-blur smears, no speed lines, no smoke, no sparkles, no floating lines, no stray pixels, no detached blobs.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

### 6.22 `player_summer_baton_crouch` — 웅크려 걷기 · 경찰봉 (16 → 24)

```
Sprite sheet for a side-scrolling 2D survival game, side-view 2D pixel art, crisp chunky pixels, limited muted palette, 1-pixel dark outline, no anti-aliasing, no blur, orthographic side view.
Grid of 24 equal cells: 6 columns x 4 rows, read left to right then top to bottom, no gaps, no borders, no numbers, no text. Each cell is one animation frame of the same character facing right, feet on the same floor baseline near the bottom of every cell, body centered horizontally. Transparent background.

Character: a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers, holding a black police baton exactly the way she holds it in the reference frames. Match the attached reference frames exactly: same face, same hair, same outfit, same colors, same shading style, same proportions, same pixel scale, same weapon. Do not draw a backpack.

Animation: sneaking forward in a low crouch, knees deeply bent, back low, holding a black police baton exactly the way she holds it in the reference frames, careful small steps with clearly visible leg movement. Cell 1 must also look natural as a still crouching pose, the game shows it alone when she stops. Seamless loop: cell 24 flows smoothly back into cell 1.
The reference strip has 16 frames. Keep the odd reference frames 1, 3, 5, ..., 15 unchanged as keyframes in cells 1, 4, 7, 10, 13, 16, 19 and 22, and draw two in-between frames between each pair of keyframes; the even reference frames show the pose halfway between, use them as a guide. Cells 23-24 are the in-betweens from reference frame 15 back to reference frame 1 (reference frame 16 is the guide).

Exactly one character in every frame, never a second person or a ghost copy. The police baton keeps the exact same length and shape in every frame; it never stretches, grows or bends. No motion-blur smears, no speed lines, no smoke, no sparkles, no floating lines, no stray pixels, no detached blobs.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

### 6.23 `player_summer_mop_crouch` — 웅크려 걷기 · 대걸레 (16 → 24)

```
Sprite sheet for a side-scrolling 2D survival game, side-view 2D pixel art, crisp chunky pixels, limited muted palette, 1-pixel dark outline, no anti-aliasing, no blur, orthographic side view.
Grid of 24 equal cells: 6 columns x 4 rows, read left to right then top to bottom, no gaps, no borders, no numbers, no text. Each cell is one animation frame of the same character facing right, feet on the same floor baseline near the bottom of every cell, body centered horizontally. Transparent background.

Character: a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers, holding a wooden mop with a grey-white cotton mop head exactly the way she holds it in the reference frames. Match the attached reference frames exactly: same face, same hair, same outfit, same colors, same shading style, same proportions, same pixel scale, same weapon. Do not draw a backpack.

Animation: sneaking forward in a low crouch, knees deeply bent, back low, holding a wooden mop with a grey-white cotton mop head exactly the way she holds it in the reference frames, careful small steps with clearly visible leg movement. Cell 1 must also look natural as a still crouching pose, the game shows it alone when she stops. Seamless loop: cell 24 flows smoothly back into cell 1.
The reference strip has 16 frames. Keep the odd reference frames 1, 3, 5, ..., 15 unchanged as keyframes in cells 1, 4, 7, 10, 13, 16, 19 and 22, and draw two in-between frames between each pair of keyframes; the even reference frames show the pose halfway between, use them as a guide. Cells 23-24 are the in-betweens from reference frame 15 back to reference frame 1 (reference frame 16 is the guide).

Exactly one character in every frame, never a second person or a ghost copy. The mop keeps the exact same length and shape in every frame; it never stretches, grows or bends. The mop head stays a compact bundle of cotton strands hanging down, never fanned out, never pointing up. No motion-blur smears, no speed lines, no smoke, no sparkles, no floating lines, no stray pixels, no detached blobs.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

### 6.24 `player_summer_none_exhausted` — 지침 · 맨손 (12 → 24)

```
Sprite sheet for a side-scrolling 2D survival game, side-view 2D pixel art, crisp chunky pixels, limited muted palette, 1-pixel dark outline, no anti-aliasing, no blur, orthographic side view.
Grid of 24 equal cells: 6 columns x 4 rows, read left to right then top to bottom, no gaps, no borders, no numbers, no text. Each cell is one animation frame of the same character facing right, feet on the same floor baseline near the bottom of every cell, body centered horizontally. Transparent background.

Character: a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers. Match the attached reference frames exactly: same face, same hair, same outfit, same colors, same shading style, same proportions, same pixel scale. Do not draw a backpack.

Animation: standing exhausted, bent forward with hands near her knees, empty-handed, catching her breath: the back and shoulders heave up and down slowly, the feet never move. Seamless loop: cell 24 flows smoothly back into cell 1.
The reference strip has 12 frames. Keep reference frames 1-12 unchanged as keyframes in cells 1, 3, 5, ..., 23 (reference frame k goes to cell 2k-1), and draw one in-between frame in each even cell; cell 24 is the in-between from reference frame 12 back to reference frame 1.

Exactly one character in every frame, never a second person or a ghost copy. Her hands are empty in every frame. No motion-blur smears, no speed lines, no smoke, no sparkles, no floating lines, no stray pixels, no detached blobs.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

### 6.25 `player_summer_bat_exhausted` — 지침 · 야구방망이 (12 → 24)

```
Sprite sheet for a side-scrolling 2D survival game, side-view 2D pixel art, crisp chunky pixels, limited muted palette, 1-pixel dark outline, no anti-aliasing, no blur, orthographic side view.
Grid of 24 equal cells: 6 columns x 4 rows, read left to right then top to bottom, no gaps, no borders, no numbers, no text. Each cell is one animation frame of the same character facing right, feet on the same floor baseline near the bottom of every cell, body centered horizontally. Transparent background.

Character: a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers, holding a wooden baseball bat exactly the way she holds it in the reference frames. Match the attached reference frames exactly: same face, same hair, same outfit, same colors, same shading style, same proportions, same pixel scale, same weapon. Do not draw a backpack.

Animation: standing exhausted, bent forward with hands near her knees, holding a wooden baseball bat exactly the way she holds it in the reference frames, catching her breath: the back and shoulders heave up and down slowly, the feet never move. Seamless loop: cell 24 flows smoothly back into cell 1.
The reference strip has 12 frames. Keep reference frames 1-12 unchanged as keyframes in cells 1, 3, 5, ..., 23 (reference frame k goes to cell 2k-1), and draw one in-between frame in each even cell; cell 24 is the in-between from reference frame 12 back to reference frame 1.

Exactly one character in every frame, never a second person or a ghost copy. The baseball bat keeps the exact same length and shape in every frame; it never stretches, grows or bends. No motion-blur smears, no speed lines, no smoke, no sparkles, no floating lines, no stray pixels, no detached blobs.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

### 6.26 `player_summer_pipe_exhausted` — 지침 · 쇠파이프 (12 → 24)

```
Sprite sheet for a side-scrolling 2D survival game, side-view 2D pixel art, crisp chunky pixels, limited muted palette, 1-pixel dark outline, no anti-aliasing, no blur, orthographic side view.
Grid of 24 equal cells: 6 columns x 4 rows, read left to right then top to bottom, no gaps, no borders, no numbers, no text. Each cell is one animation frame of the same character facing right, feet on the same floor baseline near the bottom of every cell, body centered horizontally. Transparent background.

Character: a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers, holding a rusty steel pipe exactly the way she holds it in the reference frames. Match the attached reference frames exactly: same face, same hair, same outfit, same colors, same shading style, same proportions, same pixel scale, same weapon. Do not draw a backpack.

Animation: standing exhausted, bent forward with hands near her knees, holding a rusty steel pipe exactly the way she holds it in the reference frames, catching her breath: the back and shoulders heave up and down slowly, the feet never move. Seamless loop: cell 24 flows smoothly back into cell 1.
The reference strip has 12 frames. Keep reference frames 1-12 unchanged as keyframes in cells 1, 3, 5, ..., 23 (reference frame k goes to cell 2k-1), and draw one in-between frame in each even cell; cell 24 is the in-between from reference frame 12 back to reference frame 1.

Exactly one character in every frame, never a second person or a ghost copy. The steel pipe keeps the exact same length and shape in every frame; it never stretches, grows or bends. No motion-blur smears, no speed lines, no smoke, no sparkles, no floating lines, no stray pixels, no detached blobs.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

### 6.27 `player_summer_baton_exhausted` — 지침 · 경찰봉 (12 → 24)

```
Sprite sheet for a side-scrolling 2D survival game, side-view 2D pixel art, crisp chunky pixels, limited muted palette, 1-pixel dark outline, no anti-aliasing, no blur, orthographic side view.
Grid of 24 equal cells: 6 columns x 4 rows, read left to right then top to bottom, no gaps, no borders, no numbers, no text. Each cell is one animation frame of the same character facing right, feet on the same floor baseline near the bottom of every cell, body centered horizontally. Transparent background.

Character: a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers, holding a black police baton exactly the way she holds it in the reference frames. Match the attached reference frames exactly: same face, same hair, same outfit, same colors, same shading style, same proportions, same pixel scale, same weapon. Do not draw a backpack.

Animation: standing exhausted, bent forward with hands near her knees, holding a black police baton exactly the way she holds it in the reference frames, catching her breath: the back and shoulders heave up and down slowly, the feet never move. Seamless loop: cell 24 flows smoothly back into cell 1.
The reference strip has 12 frames. Keep reference frames 1-12 unchanged as keyframes in cells 1, 3, 5, ..., 23 (reference frame k goes to cell 2k-1), and draw one in-between frame in each even cell; cell 24 is the in-between from reference frame 12 back to reference frame 1.

Exactly one character in every frame, never a second person or a ghost copy. The police baton keeps the exact same length and shape in every frame; it never stretches, grows or bends. No motion-blur smears, no speed lines, no smoke, no sparkles, no floating lines, no stray pixels, no detached blobs.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

### 6.28 `player_summer_mop_exhausted` — 지침 · 대걸레 (12 → 24)

```
Sprite sheet for a side-scrolling 2D survival game, side-view 2D pixel art, crisp chunky pixels, limited muted palette, 1-pixel dark outline, no anti-aliasing, no blur, orthographic side view.
Grid of 24 equal cells: 6 columns x 4 rows, read left to right then top to bottom, no gaps, no borders, no numbers, no text. Each cell is one animation frame of the same character facing right, feet on the same floor baseline near the bottom of every cell, body centered horizontally. Transparent background.

Character: a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers, holding a wooden mop with a grey-white cotton mop head exactly the way she holds it in the reference frames. Match the attached reference frames exactly: same face, same hair, same outfit, same colors, same shading style, same proportions, same pixel scale, same weapon. Do not draw a backpack.

Animation: standing exhausted, bent forward with hands near her knees, holding a wooden mop with a grey-white cotton mop head exactly the way she holds it in the reference frames, catching her breath: the back and shoulders heave up and down slowly, the feet never move. Seamless loop: cell 24 flows smoothly back into cell 1.
The reference strip has 12 frames. Keep reference frames 1-12 unchanged as keyframes in cells 1, 3, 5, ..., 23 (reference frame k goes to cell 2k-1), and draw one in-between frame in each even cell; cell 24 is the in-between from reference frame 12 back to reference frame 1.

Exactly one character in every frame, never a second person or a ghost copy. The mop keeps the exact same length and shape in every frame; it never stretches, grows or bends. The mop head stays a compact bundle of cotton strands hanging down, never fanned out, never pointing up. No motion-blur smears, no speed lines, no smoke, no sparkles, no floating lines, no stray pixels, no detached blobs.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

### 6.29 `player_summer_none_hurt` — 다침 · 맨손 (8 → 24)

```
Sprite sheet for a side-scrolling 2D survival game, side-view 2D pixel art, crisp chunky pixels, limited muted palette, 1-pixel dark outline, no anti-aliasing, no blur, orthographic side view.
Grid of 24 equal cells: 6 columns x 4 rows, read left to right then top to bottom, no gaps, no borders, no numbers, no text. Each cell is one animation frame of the same character facing right, feet on the same floor baseline near the bottom of every cell, body centered horizontally. Transparent background.

Character: a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers. Match the attached reference frames exactly: same face, same hair, same outfit, same colors, same shading style, same proportions, same pixel scale. Do not draw a backpack.

Animation: she gets hit and flinches, empty-handed: the head and shoulders jerk back, one knee lifts, she staggers a little, then recovers back toward the standing pose by cell 24. Pain shown only by posture, no blood, no wounds.
The reference strip has 8 frames. Keep reference frames 1-8 unchanged as keyframes in cells 1, 4, 7, 10, 13, 16, 19 and 22, and draw two in-between frames between each pair of keyframes.

Exactly one character in every frame, never a second person or a ghost copy. Her hands are empty in every frame. No motion-blur smears, no speed lines, no smoke, no sparkles, no floating lines, no stray pixels, no detached blobs.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

### 6.30 `player_summer_bat_hurt` — 다침 · 야구방망이 (8 → 24)

```
Sprite sheet for a side-scrolling 2D survival game, side-view 2D pixel art, crisp chunky pixels, limited muted palette, 1-pixel dark outline, no anti-aliasing, no blur, orthographic side view.
Grid of 24 equal cells: 6 columns x 4 rows, read left to right then top to bottom, no gaps, no borders, no numbers, no text. Each cell is one animation frame of the same character facing right, feet on the same floor baseline near the bottom of every cell, body centered horizontally. Transparent background.

Character: a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers, holding a wooden baseball bat exactly the way she holds it in the reference frames. Match the attached reference frames exactly: same face, same hair, same outfit, same colors, same shading style, same proportions, same pixel scale, same weapon. Do not draw a backpack.

Animation: she gets hit and flinches, holding a wooden baseball bat exactly the way she holds it in the reference frames: the head and shoulders jerk back, one knee lifts, she staggers a little, then recovers back toward the standing pose by cell 24. Pain shown only by posture, no blood, no wounds.
The reference strip has 8 frames. Keep reference frames 1-8 unchanged as keyframes in cells 1, 4, 7, 10, 13, 16, 19 and 22, and draw two in-between frames between each pair of keyframes.

Exactly one character in every frame, never a second person or a ghost copy. The baseball bat keeps the exact same length and shape in every frame; it never stretches, grows or bends. No motion-blur smears, no speed lines, no smoke, no sparkles, no floating lines, no stray pixels, no detached blobs.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

### 6.31 `player_summer_pipe_hurt` — 다침 · 쇠파이프 (8 → 24)

```
Sprite sheet for a side-scrolling 2D survival game, side-view 2D pixel art, crisp chunky pixels, limited muted palette, 1-pixel dark outline, no anti-aliasing, no blur, orthographic side view.
Grid of 24 equal cells: 6 columns x 4 rows, read left to right then top to bottom, no gaps, no borders, no numbers, no text. Each cell is one animation frame of the same character facing right, feet on the same floor baseline near the bottom of every cell, body centered horizontally. Transparent background.

Character: a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers, holding a rusty steel pipe exactly the way she holds it in the reference frames. Match the attached reference frames exactly: same face, same hair, same outfit, same colors, same shading style, same proportions, same pixel scale, same weapon. Do not draw a backpack.

Animation: she gets hit and flinches, holding a rusty steel pipe exactly the way she holds it in the reference frames: the head and shoulders jerk back, one knee lifts, she staggers a little, then recovers back toward the standing pose by cell 24. Pain shown only by posture, no blood, no wounds.
The reference strip has 8 frames. Keep reference frames 1-8 unchanged as keyframes in cells 1, 4, 7, 10, 13, 16, 19 and 22, and draw two in-between frames between each pair of keyframes.

Exactly one character in every frame, never a second person or a ghost copy. The steel pipe keeps the exact same length and shape in every frame; it never stretches, grows or bends. No motion-blur smears, no speed lines, no smoke, no sparkles, no floating lines, no stray pixels, no detached blobs.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

### 6.32 `player_summer_baton_hurt` — 다침 · 경찰봉 (8 → 24)

```
Sprite sheet for a side-scrolling 2D survival game, side-view 2D pixel art, crisp chunky pixels, limited muted palette, 1-pixel dark outline, no anti-aliasing, no blur, orthographic side view.
Grid of 24 equal cells: 6 columns x 4 rows, read left to right then top to bottom, no gaps, no borders, no numbers, no text. Each cell is one animation frame of the same character facing right, feet on the same floor baseline near the bottom of every cell, body centered horizontally. Transparent background.

Character: a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers, holding a black police baton exactly the way she holds it in the reference frames. Match the attached reference frames exactly: same face, same hair, same outfit, same colors, same shading style, same proportions, same pixel scale, same weapon. Do not draw a backpack.

Animation: she gets hit and flinches, holding a black police baton exactly the way she holds it in the reference frames: the head and shoulders jerk back, one knee lifts, she staggers a little, then recovers back toward the standing pose by cell 24. Pain shown only by posture, no blood, no wounds.
The reference strip has 8 frames. Keep reference frames 1-8 unchanged as keyframes in cells 1, 4, 7, 10, 13, 16, 19 and 22, and draw two in-between frames between each pair of keyframes.

Exactly one character in every frame, never a second person or a ghost copy. The police baton keeps the exact same length and shape in every frame; it never stretches, grows or bends. No motion-blur smears, no speed lines, no smoke, no sparkles, no floating lines, no stray pixels, no detached blobs.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

### 6.33 `player_summer_mop_hurt` — 다침 · 대걸레 (8 → 24)

```
Sprite sheet for a side-scrolling 2D survival game, side-view 2D pixel art, crisp chunky pixels, limited muted palette, 1-pixel dark outline, no anti-aliasing, no blur, orthographic side view.
Grid of 24 equal cells: 6 columns x 4 rows, read left to right then top to bottom, no gaps, no borders, no numbers, no text. Each cell is one animation frame of the same character facing right, feet on the same floor baseline near the bottom of every cell, body centered horizontally. Transparent background.

Character: a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers, holding a wooden mop with a grey-white cotton mop head exactly the way she holds it in the reference frames. Match the attached reference frames exactly: same face, same hair, same outfit, same colors, same shading style, same proportions, same pixel scale, same weapon. Do not draw a backpack.

Animation: she gets hit and flinches, holding a wooden mop with a grey-white cotton mop head exactly the way she holds it in the reference frames: the head and shoulders jerk back, one knee lifts, she staggers a little, then recovers back toward the standing pose by cell 24. Pain shown only by posture, no blood, no wounds.
The reference strip has 8 frames. Keep reference frames 1-8 unchanged as keyframes in cells 1, 4, 7, 10, 13, 16, 19 and 22, and draw two in-between frames between each pair of keyframes.

Exactly one character in every frame, never a second person or a ghost copy. The mop keeps the exact same length and shape in every frame; it never stretches, grows or bends. The mop head stays a compact bundle of cotton strands hanging down, never fanned out, never pointing up. No motion-blur smears, no speed lines, no smoke, no sparkles, no floating lines, no stray pixels, no detached blobs.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

### 6.34 `player_summer_none_shove` — 밀치기 · 맨손 (20 → 24)

```
Sprite sheet for a side-scrolling 2D survival game, side-view 2D pixel art, crisp chunky pixels, limited muted palette, 1-pixel dark outline, no anti-aliasing, no blur, orthographic side view.
Grid of 24 equal cells: 6 columns x 4 rows, read left to right then top to bottom, no gaps, no borders, no numbers, no text. Each cell is one animation frame of the same character facing right, feet on the same floor baseline near the bottom of every cell, body centered horizontally. Transparent background.

Character: a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers. Match the attached reference frames exactly: same face, same hair, same outfit, same colors, same shading style, same proportions, same pixel scale. Do not draw a backpack.

Animation: an empty-handed two-handed shove: she steps forward and pushes both palms out at chest height, arms fully extended in cell 13 (the moment of the push), then pulls back and returns to standing by cell 24.
The reference strip has 20 frames. Keep reference frames 1, 4, 6, 8, 10, 13, 16, 20 unchanged as keyframes in cells 1, 4, 7, 10, 13, 16, 19, 22, and draw two in-between frames between each pair of keyframes; the other reference frames show the poses in between, use them as a guide.

Exactly one character in every frame, never a second person or a ghost copy. Her hands are empty in every frame. No motion-blur smears, no speed lines, no smoke, no sparkles, no floating lines, no stray pixels, no detached blobs.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

### 6.35 `player_summer_none_search` — 수색 · 맨손 (12 → 24)

```
Sprite sheet for a side-scrolling 2D survival game, side-view 2D pixel art, crisp chunky pixels, limited muted palette, 1-pixel dark outline, no anti-aliasing, no blur, orthographic side view.
Grid of 24 equal cells: 6 columns x 4 rows, read left to right then top to bottom, no gaps, no borders, no numbers, no text. Each cell is one animation frame of the same character facing right, feet on the same floor baseline near the bottom of every cell, body centered horizontally. Transparent background.

Character: a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers. Match the attached reference frames exactly: same face, same hair, same outfit, same colors, same shading style, same proportions, same pixel scale. Do not draw a backpack.

Animation: kneeling low and searching through something on the floor in front of her, empty-handed, both hands reaching forward and rummaging, the head following the hands, the knees stay in place. Seamless loop: cell 24 flows smoothly back into cell 1.
The reference strip has 12 frames. Keep reference frames 1-12 unchanged as keyframes in cells 1, 3, 5, ..., 23 (reference frame k goes to cell 2k-1), and draw one in-between frame in each even cell; cell 24 is the in-between from reference frame 12 back to reference frame 1.

Exactly one character in every frame, never a second person or a ghost copy. Her hands are empty in every frame. No motion-blur smears, no speed lines, no smoke, no sparkles, no floating lines, no stray pixels, no detached blobs.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

### 6.36 `player_summer_none_hide` — 숨기 · 맨손 (8 → 24)

```
Sprite sheet for a side-scrolling 2D survival game, side-view 2D pixel art, crisp chunky pixels, limited muted palette, 1-pixel dark outline, no anti-aliasing, no blur, orthographic side view.
Grid of 24 equal cells: 6 columns x 4 rows, read left to right then top to bottom, no gaps, no borders, no numbers, no text. Each cell is one animation frame of the same character facing right, feet on the same floor baseline near the bottom of every cell, body centered horizontally. Transparent background.

Character: a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers. Match the attached reference frames exactly: same face, same hair, same outfit, same colors, same shading style, same proportions, same pixel scale. Do not draw a backpack.

Animation: from standing, she quickly crouches down and curls into a small ball hugging her knees to hide, empty-handed. Cells 22-24 are the final hidden pose and must be completely still and identical in silhouette, the game holds the last cell while she hides.
The reference strip has 8 frames. Keep reference frames 1-8 unchanged as keyframes in cells 1, 4, 7, 10, 13, 16, 19 and 22, and draw two in-between frames between each pair of keyframes.

Exactly one character in every frame, never a second person or a ghost copy. Her hands are empty in every frame. No motion-blur smears, no speed lines, no smoke, no sparkles, no floating lines, no stray pixels, no detached blobs.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

### 6.37 `player_summer_none_doze` — 잠듦 · 맨손 (12 → 24)

```
Sprite sheet for a side-scrolling 2D survival game, side-view 2D pixel art, crisp chunky pixels, limited muted palette, 1-pixel dark outline, no anti-aliasing, no blur, orthographic side view.
Grid of 24 equal cells: 6 columns x 4 rows, read left to right then top to bottom, no gaps, no borders, no numbers, no text. Each cell is one animation frame of the same character facing right, feet on the same floor baseline near the bottom of every cell, body centered horizontally. Transparent background.

Character: a 16-year-old Korean schoolgirl with shoulder-length straight black hair, short-sleeved white summer school blouse with a beige knit vest, knee-length dark navy pleated skirt, white socks and dark sneakers. Match the attached reference frames exactly: same face, same hair, same outfit, same colors, same shading style, same proportions, same pixel scale. Do not draw a backpack.

Animation: sitting on the floor hugging her knees, hiding, dozing off: the head slowly nods down and jerks back up a little, the body stays curled up and does not slide. Seamless loop: cell 24 flows smoothly back into cell 1.
The reference strip has 12 frames. Keep reference frames 1-12 unchanged as keyframes in cells 1, 3, 5, ..., 23 (reference frame k goes to cell 2k-1), and draw one in-between frame in each even cell; cell 24 is the in-between from reference frame 12 back to reference frame 1.

Exactly one character in every frame, never a second person or a ghost copy. Her hands are empty in every frame. No motion-blur smears, no speed lines, no smoke, no sparkles, no floating lines, no stray pixels, no detached blobs.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

## 7. 받은 뒤 (내가 할 일)

1. 샘플 도구 `code_temp/apogirl/tools/s11_install.py`를 묶음 이름·키프레임 칸을 받도록 넓혀 넣는다: 키프레임 짝의 면적 비로 크기, 몸통 위치로 가로, 칸마다 몸 맨 아래를 지금 0번 프레임의 바닥 줄에. 옛 프레임은 `code_temp/apogirl/backup_s11/`로 옮겨 둔다.
2. 공격·밀치기는 샘플처럼 **몸통 목표 곡선**으로 몸이 뒤로 밀리지 않게 맞춘다. 도는 동작은 몸통이 처음 자리에서 떠다니지 않게 고르게 맞춘다.
3. 가방 자리 `player_anchors.json`: 옛 값을 키프레임 칸에 두고 사이를 고르게 나눈다(샘플과 같음). 맨손 동작(수색·숨기·잠듦)도 같다.
4. 도는 동작은 프레임 수에 맞춰 초당 프레임을 늘려 한 바퀴 시간을 지금과 같게 한다(`ExpeditionDirector.LoopFrame`).
5. 공격 차례 표(`Combat.ImpactAt[].Frames`)는 옛 8장짜리에만 쓰이고, 24장 묶음은 고르게 돈다 — 맞는 순간 0.5 = 13번 칸. 대걸레는 `At`을 0.5로 옮긴다(받은 묶음부터).
6. 게임에서 옷·무기마다 걷기·달리기·공격을 보고 끊김·튀는 칸·발 미끄러짐을 확인한다.
