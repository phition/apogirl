# pixelfx 출처

탐험 타격 이펙트 일부는 오픈소스 [patch-gg/pixelfx](https://github.com/patch-gg/pixelfx)(MIT, 커밋 `5294518`, 2026-11-12)의
코드 이펙트를 렌더한 그림이다. 라이선스 전문은 같은 폴더 `LICENSE.txt`.

- Copyright (c) 2026 Patch
- Copyright (c) 2026 Hugo Duprez — 아래 두 이펙트는 그의 Sprite Fusion 오픈소스 이펙트(https://www.spritefusion.com/pixel-art-vfx, MIT)를 바탕으로 한다.

| 게임 안 그림 | pixelfx 이펙트 | 쓰는 곳 |
|---|---|---|
| `Resources/Art/ExplorationPixel/fx_pfx_blunt_0..21` | `impacts/blunt-hit` | 방망이·파이프 타격 (`exploration_art.json` `Combat.ImpactAt[].Fx`) |
| `Resources/Art/ExplorationPixel/fx_pfx_thrust_0..14` | `melee/thrust` | 대걸레 찌르기 |

만든 법: `node tools/render.mjs <id> --sheet --no-actor --fps 24` → 시트를 프레임으로 자르고 끝의 빈 프레임을 버림
(`I:\workshop\code_temp\pixelfx_try\tools\cut.py`). 기준점(맞는 자리)은 `sprite_meta.json`. 찌르기는 게임의 뒤집기 규칙(주인공이 왼쪽이면 뒤집음)에 맞춰 좌우를 미리 뒤집었다. 옛 그림 `fx_hit_heavy_`·`fx_hit_thrust_`은 그대로 두었다.
