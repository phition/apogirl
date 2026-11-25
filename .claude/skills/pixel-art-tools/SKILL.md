---
name: pixel-art-tools
description: 원화(힉스필드 등 고해상도 그림)를 탐험용 도트 그림으로 바꾸거나, 도트 이펙트(타격·불꽃·먼지·마법 등)를 새로 만들거나 바꿀 때 쓴다. PixelOE(원화→도트)와 pixelfx(코드로 그리는 도트 이펙트) 사용법, 이 프로젝트에 넣는 규칙. "도트로 바꿔", "픽셀화", "이펙트 만들어", "타격 이펙트" 같은 요청에.
---

# 도트 그림·이펙트 도구 (PixelOE · pixelfx)

사용자 결정(2026-11-25): **원화를 도트로 바꿀 때는 PixelOE, 도트 이펙트는 pixelfx**를 쓴다. 다른 도구를 새로 찾기 전에 이 둘부터.

## 1. 원화 → 도트: PixelOE (Apache-2.0)

- 설치(되어 있음): 저장소 `I:\workshop\code_temp\pixeloe_try\repo`, 가상환경 `I:\workshop\code_temp\pixeloe_try\venv` (CPU torch, C: 드라이브가 꽉 차 I:에 둠).
- 도구: `I:\workshop\code_temp\apogirl\tools\pixeloe_convert.py`

```powershell
I:\workshop\code_temp\pixeloe_try\venv\Scripts\python.exe -I I:\workshop\code_temp\apogirl\tools\pixeloe_convert.py <원본.png ...> --height 72 --compare --out <출력 폴더>
```

- `--height`: 투명 여백을 잘라 낸 내용 높이(px). 탐험 도트(`ExplorationPixel`, PPU 36)에 넣을 거면 **지금 그 그림의 내용 높이**에 맞춘다. 2배 그림(PPU 72)은 2배 높이.
- `--thickness 2`(기본)가 우리 도트와 가장 잘 맞는다. 너무 굵으면 1.
- `--colors 32`(기본). `--compare`는 단순 축소판과 나란히 놓은 5배 비교 그림을 만든다 → **사용자에게 비교 그림을 먼저 보여 주고** 넣는다.
- 원본은 `I:\workshop\apogirl\apogirl\ArtInbox\`(git 제외: `sprites/`, `props/`, `illustrations/` …).

**잘 되는 것 / 안 되는 것** (2026-11-25 시험, 비교 그림 `I:\workshop\code_temp\pixeloe_try\out\`):
- 인물(조우 인물·동료 NPC 서 있기·숨기)은 확실히 낫다. 단순 축소는 얼굴·옷이 뭉개지고 PixelOE는 얼굴선·옷 무늬가 또렷하다.
- 사진처럼 질감이 많은 설비·배경은 잔점이 지저분해진다 → 단순 축소가 낫다.
- 이벤트 컷은 원화로 쓰므로 바꾸지 않는다.
- 동작 프레임 여러 장은 프레임마다 점이 달라 깜빡일 수 있다 → 넣기 전에 GIF로 이어 보고 확인.

## 2. 도트 이펙트: pixelfx (MIT)

- 저장소(받아 둠): `I:\workshop\code_temp\pixelfx_try\repo` (커밋 `5294518`). Node 18+만 있으면 되고 npm 설치 없음. ffmpeg은 없어서 `--gif`는 안 된다 — GIF가 필요하면 시트를 잘라 PIL로.
- 이펙트 목록: 저장소 `docs/CATALOG.md`, 코드 `src/fx/<분류>/<이름>.js` (244종: 타격·근접·불·물·날씨·UI·줍기 …). 모양을 보려면 `node tools/render.mjs <id>` → `records/<id>/contact.png`.
- 옵션(`meta.options`)으로 색(`palette`: 어두운→밝은 7색)·크기 등을 바꾼다. 옵션에 없는 것을 바꿀 때는 **원본을 고치지 말고 사본**(`<이름>-soft.js` 등)을 만들고, 사본 코드를 프로젝트 `Assets/_Project/Art/ThirdParty/pixelfx/source/<이름>.js.txt`에 남긴다(예: `hit-spark-soft` = 가운데 별 크기 옵션 `star`).
- 도구: `I:\workshop\code_temp\apogirl\tools\pixelfx_make.py`

```powershell
python I:\workshop\code_temp\apogirl\tools\pixelfx_make.py impacts/blunt-hit fx_pfx_blunt_ --out <출력 폴더>
```

  - 24fps로 렌더(`Combat.FxFramesPerSecond`), 끝의 빈 프레임 버림, **좌우를 미리 뒤집음**(탐험 `HitFx`는 주인공이 왼쪽이면 그림을 뒤집으므로; 방향 없는 이펙트면 `--no-mirror`도 무방).
  - 마지막 줄에 `sprite_meta.json`에 넣을 기준점(맞는 자리 = 가장 밝은 화소 중심)을 출력한다. 효과가 좀비 가슴(`FxHeight`)에서 터지는지 주인공·좀비 그림에 겹쳐 확인.

## 3. 프로젝트에 넣는 규칙

1. 그림은 `Assets/_Project/Resources/Art/ExplorationPixel/`(PPU 36, 가져오기 설정 `Editor/ArtImportSettings`), 이름은 pixelfx면 `fx_pfx_<이름>_0..n`.
2. 기준점은 `ExplorationPixel/sprite_meta.json`에 `"Key": "<접두어>*"` 한 줄 (키 끝 `*` = 앞부분 일치).
3. 타격 이펙트 연결은 `Resources/Art/exploration_art.json` `Combat.ImpactAt[].Fx`(무기별). 지금: 방망이·파이프 `fx_pfx_blunt_`, 대걸레 `fx_pfx_poke_`, 경찰봉·기본 `fx_hit_`.
4. 옛 그림은 바로 지우지 말고 둔다(되돌리기 = JSON 한 줄). 이번에 새로 넣었다가 바꾼 것만 지운다.
5. **출처**: pixelfx 그림은 `Assets/_Project/Art/ThirdParty/pixelfx/CREDITS.md` 표에 한 줄(그림·이펙트 id·쓰는 곳). Hugo Duprez 코드를 바탕으로 한 이펙트면 그것도 표기. PixelOE는 도구일 뿐(결과물에 라이선스 조건 없음)이라 크레딧은 필요 없지만 PROGRESS에 "PixelOE로 변환"을 적는다.
6. Unity 확인: worktree에서 커밋 → main에 `--ff-only` → Unity MCP로 `AssetDatabase.Refresh` + 해당 그림 `ImportAsset(ForceUpdate)` → 기준점·PPU 확인 → 생긴 `.meta`는 main에서 커밋.
7. PROGRESS 작업 기록 한 줄, 필요하면 CLAUDE.md의 해당 줄도 고친다.
