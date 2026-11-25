---
name: sprite-gif
description: 스프라이트 프레임(`{묶음}_{n}.png`)을 묶음별 재생 GIF로 만들 때 쓴다. 게임 탐험 화면과 같은 빠르기로 동작을 이어 보며 확인·비교·보관할 때 — "gif로 만들어", "움직임 보여 줘", "애니메이션 확인", "프레임 이어 보기", 새로 받은 탐험 그림을 넣기 전 점검 같은 요청에.
---

# 스프라이트 → 재생 GIF (`Tools/sprite_gif.py`)

프레임 폴더(또는 png 파일들)를 받아 같은 묶음끼리 번호 순으로 이어 GIF 하나씩 만든다.
`player_summer_bat_attack_0..23.png` → `player_summer_bat_attack.gif`. 번호 없는 `이름.png`은 `이름_still.gif`.

```powershell
python Tools/sprite_gif.py <폴더 또는 png ...> [--out 폴더] [--pattern "player_summer_*"]
```

- PIL은 시스템 python 사용자 site-packages에 있다(도구가 알아서 찾음). `-I`는 붙이지 않는다(사용자 site를 못 읽음).
- 출력: 입력 폴더 이름이 `frames`면 옆의 `gifs/`, 아니면 `<폴더>/gifs/`. **게임 폴더(`Assets/…/ExplorationPixel`)에 넣을 때는 꼭 `--out`으로 밖(scratchpad나 `ArtInbox`)에** — 안 그러면 Unity가 GIF를 가져온다.
- `--pattern`: 묶음 이름 거르기(fnmatch). 게임 폴더는 묶음이 수천 개라 늘 거른다. 예: `"player_summer_*_attack"`, `"zombie_*"`.

## 선택 사항

| 옵션 | 기본 | 뜻 |
|---|---|---|
| `--bg` | `3a3c42` | 바탕색 hex. `transparent`면 투명(알파 128 미만을 투명 칸으로) |
| `--min-height` | 240 | 이보다 낮은 그림은 정수배로 키움(최근접, 도트 유지) |
| `--scale N` | — | 배율 직접 지정 |
| `--fps N` | — | 모든 묶음을 이 fps 반복으로 |
| `--seconds S` | — | 모든 묶음을 S초 한 번 동작으로 |
| `--hold` | 400 | 한 번 동작 끝에서 멈추는 ms |
| `--strip` | 끔 | 프레임을 가로로 늘어놓은 `_strip.png`도 (한 장으로 보여 줄 때 편함) |

프레임 크기가 서로 달라도 가운데·발 바닥에 맞춰 같은 캔버스에 놓는다.

## 빠르기 (게임 `ExpeditionDirector` 값, 묶음 이름 끝 동작으로 고름)

- 주인공 도는 동작: 대기·지침·수색·잠듦 7fps, 걷기 18, 달리기 26, 웅크려 걷기 12 — 16장(지침·수색·잠듦 12장) 기준이고, 프레임이 많으면 한 바퀴 시간이 같게 fps를 늘린다(`LoopFrame` `designedFrames`).
- 주인공 한 번 동작: 공격·밀치기 0.5초(`AttackAnimSeconds`), 다침 0.4초(`HurtReactSeconds`), 숨기 1.5초(`HideSeconds`) + 끝 멈춤.
- 좀비 `zombie_*`: 걷기 11, 쫓기 20, 대기 6, 잠·휴면 3.6, 휘청 9.6, 맞음·밀림 0.6초, 쓰러짐 1초.
- 그 밖: 10fps 반복.
- 게임에서 이 값이 바뀌면 도구 위쪽 표(`PLAYER_LOOP`·`PLAYER_ONCE`·`ZOMBIE_*`)도 고친다.
- GIF는 0.01초 단위이고 20ms 미만은 브라우저가 느리게 틀어서 20ms로 올린다 → 24장 공격(0.5초)은 칸마다 0.02초, 실제보다 조금 빠름.

## 자주 쓰는 예

```powershell
# 차수별 보관 폴더 하나 (frames → gifs)
python Tools/sprite_gif.py I:\workshop\apogirl\apogirl\ArtInbox\player_sprites_by_round\PixelLab_08\frames

# 지금 게임 그림 중 하복 방망이만, 투명, 한 장 줄그림도
python Tools/sprite_gif.py Assets/_Project/Resources/Art/ExplorationPixel --pattern "player_summer_bat_*" --bg transparent --strip --out <scratchpad>\gif_summer_bat

# 새로 받아 자른 프레임 점검 (넣기 전)
python Tools/sprite_gif.py <잘라 낸 프레임 폴더> --out <scratchpad>\check
```

## 결과 보여 주기

- GIF 몇 개를 사용자에게 보낼 때는 `SendUserFile`(display `render`)로. 많으면 폴더 경로만 알려 준다.
- 내가 직접 확인할 때는 `--strip` PNG를 Read로 본다(GIF는 첫 칸만 보인다).
- 차수별 보관 폴더 `ArtInbox/player_sprites_by_round/`(git 밖)의 `gifs/`는 이 도구로 만든 것이다. 새 차수를 넣으면 거기 `frames/`를 만들고 이 도구를 돌린다.
