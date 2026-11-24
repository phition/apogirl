# 제작 장비 탐험 도트 그림: PixelLab 요청 기획 (2026-11-25, 승인됨)

제작으로 만드는 장비 가운데 **탐험 화면에 보이는 것**을 PixelLab에 요청하는 계획이다. 무엇을 받을지, 얼마가 드는지, 무엇을 정해야 하는지를 적었다. **아직 아무것도 생성하지 않았다.**

**사용자 결정 (2026-11-25, 9절):** E1 허수아비 + 가방 · **E2 가방 1배(PPU 36)** · E3 몽둥이는 무기 겹치기 때 무기 그림 · **E4 보호구는 색 덧칠 스크립트**(추천과 다름, 굵게).

**끈 주머니 가방은 뺐다 (사용자 결정 2026-11-25):** 1단계 가방 레시피를 처음부터 **장바구니 만들기**(`ACT_CRAFT_BAG_TOTE` → 기존 `ITEM_BAG_TOTE`)로 바꾸고, 그림도 장바구니(`bag_tote`, 색 6벌)를 그대로 쓴다. `ITEM_BAG_SLING`은 없앴다(세이브 v15가 옛 이름을 바꾼다). 그래서 가방 요청은 2장이다.

- 프롬프트 원안은 [PIXELLAB_CRAFTING.md](PIXELLAB_CRAFTING.md)(2026-11-24)에 있다. 이 문서는 그 뒤에 바뀐 것을 반영해 요청 범위를 정한다.
  - 주인공 그림이 바뀌었다. 하복은 PixelLab 24프레임 2배(192×156, PPU 72)로 다시 받았다. 나머지 5벌도 **무기 겹치기 방식**으로 다시 받기로 정했다([PIXELLAB_24F_PLAN.md](PIXELLAB_24F_PLAN.md) 9절).
  - 탐험 허수아비가 게임에 들어갔다(제작 P6). 그림 키 `prop_decoy_*`가 코드에 있어 파일만 넣으면 바로 붙는다.
- 생성 도구는 `code_temp/apogirl/tools/pixellab.py`, 넣는 곳은 `Resources/Art/ExplorationPixel/`다.
- 아이콘·단면도 그림·효과음은 UI·일러스트라 힉스필드 몫이다([HIGGSFIELD_ASSETS_15.md](HIGGSFIELD_ASSETS_15.md)).

---

## 0. 한 줄 요약

| | 내용 |
|---|---|
| 이번에 받을 것 (결정) | **A. 탐험 허수아비 소품**(정지 3장 + 움직임 2줄 × 6프레임), **B. 제작 가방 겹 그림 2장(1배)** |
| 미룰 것 (결정) | C. 몽둥이 3종은 무기 겹치기 작업 때 무기 그림 3장만, D. 보호구 3벌은 PixelLab 없이 색 덧칠 스크립트(5절 ②) |
| 비용 (결정 범위) | 약 **$1.0** (다시 받기 포함 넉넉히 $2.0) |
| 지금 잔량 (2026-11-25 조회) | USD 크레딧 **$5.06**, 구독 generations 4.8 (사실상 다 씀 → USD로 나감) |
| 결정 | 9절 E1~E4 (2026-11-25 정해짐) |

---

## 1. 제작 장비가 탐험에 어떻게 보이나 (전수)

| 장비 | 레시피 | 탐험에 보이는 모양 | 지금 | 이번 요청 |
|---|---|---|---|---|
| 휴대 허수아비 `ITEM_DECOY_DUMMY` | `ACT_CRAFT_DECOY` | 바닥에 세운 소품, 소리 낼 때 흔들림, 부서지면 쓰러짐 | **코드 그림(사각형 3개)** | **A** |
| 울리는 허수아비 `ITEM_DECOY_BELL` | `ACT_CRAFT_DECOY_BELL` | 위 + 자명종 | **코드 그림** | **A** |
| 덧댄 배낭 `ITEM_BAG_RUCKSACK` | `ACT_CRAFT_BAG_RUCKSACK` | 같음 | 빌림 `bag_school` | **B** |
| 프레임 배낭 `ITEM_BAG_FRAME` | `ACT_CRAFT_BAG_FRAME` | 같음 | 빌림 `bag_hiking` | **B** |
| 나무 몽둥이 `ITEM_WEAPON_CLUB` | `ACT_CRAFT_CLUB` | 주인공 손의 무기(모든 동작) | 빌림 `bat` | C (결정 E3) |
| 테이프 감은 몽둥이 `ITEM_WEAPON_CLUB_TAPED` | `ACT_CRAFT_CLUB_TAPED` | 같음 | 빌림 `bat` | C |
| 철근 덧댄 몽둥이 `ITEM_WEAPON_CLUB_REBAR` | `ACT_CRAFT_CLUB_REBAR` | 같음 | 빌림 `pipe` | C |
| 잡지 보호대 `ITEM_ARMOR_MAGAZINE` | `ACT_CRAFT_ARMOR_MAGAZINE` | 주인공 옷 전체 | 빌림 `guard` | D (결정 E4) |
| 매트 덧댄 보호구 `ITEM_ARMOR_MATPAD` | `ACT_CRAFT_ARMOR_MATPAD` | 같음 | 빌림 `padding` | D |
| 철판 덧댄 조끼 `ITEM_ARMOR_PLATED` | `ACT_CRAFT_ARMOR_PLATED` | 같음 | 빌림 `riot` | D |
| 손전등·헤드랜턴·랜턴 멀티툴 | 컴퓨터실 줄 | **그림 없음** — 탐험에서는 빛(`LightRange`)만 보인다 | — | 대상 아님 |

- 빌린 그림 표는 `Resources/Art/standing_map.json` `ItemLooks`다. 새 그림을 넣으면 이 표의 토큰 한 칸을 바꾸는 것으로 끝난다.
- 허수아비는 지금 사각형 세 개로 그려져 가장 어색하다. 그래서 A를 먼저 받는다.

---

## 2. 요청 A: 탐험 허수아비 소품 (먼저)

게임은 허수아비를 0.6 × 1.7칸 상자에 맞춰 그리고(`ExpeditionDirector.PlaceDecoy`), 바닥에 선 큰 장식처럼 그림자를 드리운다. 다른 탐험 소품과 같은 **1배(PPU 36)** 로 만든다.

공통 문구(모든 호출 앞, 원안 0.2절과 같음):
```
side-view 2D pixel art sprite for a side-scrolling zombie survival game set in a Korean high school,
flat orthographic side view, muted realistic colors, thin dark outline, handmade from scavenged school materials, no text, no logos
```

| 키 | 크기 | 방법 | style 참고 | 프롬프트 (공통 문구 뒤) |
|---|---|---|---|---|
| `prop_decoy_stand` | 24 × 64, 투명 | `image_pro` 후보 3장 → 사용자 고름 | `decor_trashbags`, `decor_bucket` | `A crude standing decoy dummy shorter than an adult: a lumpy stuffed burlap sack body tied around an upright mop handle, an old faded tracksuit top tied around the sack, an upturned dented metal bucket as the head with no face and nothing drawn on it, three empty tin cans hanging from strings at the waist, its base is a broken chair leg frame wedged on the floor, clearly an object, not a person, not a child, transparent background` |
| `prop_decoy_bell_stand` | 24 × 64, 투명 | `edit_pro`, 위에서 고른 그림을 바탕으로 | `prop_decoy_stand` | `the same dummy with an old round twin-bell alarm clock tied on top of the bucket head with wire, nothing else changed` |
| `prop_decoy_down` | 64 × 24, 투명 | `edit_pro`, 위에서 고른 그림을 바탕으로 | `prop_decoy_stand` | `the same dummy lying on its side on the floor in one piece, the bucket head rolled slightly away, tin cans scattered, nothing torn apart, no face` |

움직임 (`animate`, 첫 장 = 위에서 고른 그림, 6프레임, 게임은 초당 10장으로 돈다):

| 키 | 첫 장 | `action` |
|---|---|---|
| `prop_decoy_sway_0`~`5` | `prop_decoy_stand` | `the dummy wobbles side to side on its base, the hanging tin cans swing and knock together, the base stays fixed on the floor` |
| `prop_decoy_bell_ring_0`~`5` | `prop_decoy_bell_stand` | `the alarm clock on the bucket head shakes and rings, the whole dummy trembles slightly, the base stays fixed on the floor` |

- 흔들림은 허수아비가 소리를 낼 때(좀비를 끌 때) 0.7초 돈다. 쓰러짐 그림은 내구가 다했을 때 한 장으로 바뀐다. 코드는 이미 있다.
- 받은 뒤 점검: 투명 배경, 바닥 줄(맨 아래 줄 ±1px), 6장 사이 크기 ±3%, 몸에서 떨어진 잔 점 없음, **양동이에 얼굴처럼 보이는 무늬 없음**.

## 3. 요청 B: 제작 가방 겹 그림 2장

주인공 프레임마다 등 자리에 겹쳐 그리는 작은 그림이다. 자리는 프레임마다 잰 기준점(`player_anchors.json`)을 그대로 따르고, 그림 기준점만 `sprite_meta.json`에 한 줄씩 적는다. 옆에서 본 모습이고 몸에 닿는 쪽이 왼쪽이다.

| 키 | 크기 (E2 결정 = 1배, PPU 36) | style 참고 | 프롬프트 (공통 문구 뒤) |
|---|---|---|---|
| `bag_rucksack` | 18 × 20, 투명 | `bag_school` | `A school backpack reinforced with extra patches cut from a blue gym mat and a canvas bag, visible thick hand stitching, duct-tape repaired corners, a tied-on side pocket, seen from the side as worn on the back, transparent background` |
| `bag_frame` | 24 × 30, 투명 | `bag_hiking` | `A large improvised frame backpack: a big sack sewn from thick curtain fabric lashed onto a light frame of bent aluminium chair legs, the frame ends stick out a little at the top and bottom, seat-belt shoulder straps, a padded waist belt, seen from the side as worn on the back, transparent background` |

- 프레임 배낭은 P8 장면(`EVT_CRAFT_FRAME_PACK`)의 대사와 같은 재료로 맞췄다(의자 다리 틀·커튼 천·안전벨트 끈·허리끈).
- 손으로 만든 가방이라 **색 바꾸기 6벌은 만들지 않는다.** 색 그림이 없으면 기본 그림으로 넘어간다(`ExplorationArtMap.BagKey`).
- 연결: `ItemLooks`에서 두 가방의 토큰을 `school`·`hiking` → `rucksack`·`frame`으로 바꾼다. 1단계 장바구니는 원래 장바구니 그림이라 고칠 것이 없다. 입상·단면도 그림은 토큰에 맞는 그림이 없으면 빌린 그림을 그대로 쓴다.
- 받은 뒤 점검: 하복(2배)과 동복(1배) 탐험 화면에서 대기·걷기·달리기·웅크림을 렌더해 비교판을 보여 준다. 가방이 몸에서 뜨거나 몸을 가리면 기준점을 고친다.

## 4. C. 몽둥이 3종 (결정 E3)

| 길 | 무엇 | 비용 | 위험 |
|---|---|---|---|
| ① 빌리기 유지 | 탐험 그림은 방망이·방망이·파이프 | 0 | 테이프·철근이 보이지 않는다 |
| ② **무기 겹치기 작업 때 무기 그림 3장** | [PIXELLAB_24F_PLAN.md](PIXELLAB_24F_PLAN.md) 9절의 무기 그림 4장(방망이·파이프·대걸레·경찰봉)에 몽둥이 3장을 더한다. 태그는 셋 다 휘두르기 `swing` | 약 $0.5 | 겹치기 코드가 생겨야 쓸 수 있다(5벌 다시 받기와 함께) |
| ③ 색 덧칠 스크립트 | 방망이 프레임의 방망이 화소만 골라 덧칠(나무색·회색 테이프 띠·짙은 쇠막대) | 0 (PixelLab 안 씀) | 6벌 × 약 150프레임. 하복 2배·5벌 1배를 따로 해야 하고, 5벌을 다시 받으면 다시 해야 한다 |
| ④ 전체 시트 새로 | 무기마다 옷 6벌 × 모든 동작 | 무기마다 약 $25~30 | 너무 비싸다 |

**추천: ②.** 지금은 빌린 그림을 쓰고, 나머지 5벌 다시 받기(크레딧을 채운 뒤 사용자가 시작)를 할 때 무기 그림만 3장 더 받는다. 그러면 몽둥이가 옷 6벌 모두에 붙는다. 철근 몽둥이는 지금 파이프(`smash` 내려치기)를 빌리지만 휘두르기로 바꾼다.

무기 문구(원안 3절과 같음):

| 토큰 | 문구 |
|---|---|
| `club` | `a rough wooden club carved from a school desk leg, thick at the end, pale raw wood with splinters` |
| `club_taped` | `the same wooden club with the grip and the striking end wrapped tightly in grey duct tape` |
| `club_rebar` | `a heavy wooden club reinforced with two short pieces of rusty rebar bound along the striking end with wire` |

## 5. D. 보호구 3벌 (결정 E4)

보호구는 주인공 옷 전체라 가장 비싸다. 무기 겹치기 방식이어도 옷 한 벌이 약 516장(약 $18)이고, 3벌이면 약 $54다.

| 길 | 무엇 | 비용 |
|---|---|---|
| ① **빌리기 유지** | 보호대(`guard`)·패딩(`padding`)·진압복(`riot`) 그림. 모양이 비슷하다 | 0 |
| ② 색 덧칠 스크립트 | 빌린 옷의 옷 색 영역만 바꾸고 무늬 덧칠(종이 띠·파란 매트·녹슨 철판) | 0, 다만 5벌을 다시 받으면 다시 해야 한다 |
| ③ 새 옷 3벌 | 겹치기 방식으로 옷마다 약 516장 | 약 $54 |

처음 추천은 ①(빌린 옷 유지)이었다. **결정: ② 색 덧칠 스크립트**(사용자 결정 2026-11-25). PixelLab은 쓰지 않는다.

②를 할 때:
- 도구는 `code_temp/apogirl/tools/`에 새로 둔다(`armor_recolor.py`). 빌린 옷 그림 `player_{guard,padding,riot}_*`에서 옷 영역 색만 골라 바꾸고 무늬를 덧칠해 `player_{magazine,matpad,plated}_*`로 저장한다. 원본은 건드리지 않는다.
  - 잡지 보호대(`guard` → `magazine`): 정강이·팔뚝 보호대 색을 바랜 종이색으로, 가로 줄무늬(말린 잡지 결)와 회색 테이프 띠.
  - 매트 덧댄 보호구(`padding` → `matpad`): 패딩 색을 체육관 매트 파랑으로, 끈 자리에 짙은 띠.
  - 철판 덧댄 조끼(`riot` → `plated`): 가슴판을 녹슨 회색으로, 판 가장자리에 리벳 점.
- 옷 그림은 무기 5종 × 모든 동작(옷마다 약 1,600장)이다. 같은 규칙을 기계적으로 적용하니 비용은 없지만, 넣기 전에 대기·걷기·공격 비교판을 보여 준다.
- 연결: `ItemLooks`의 세 보호구 토큰을 `magazine`·`matpad`·`plated`로 바꾸고, 입상 옷 표 `StandingConfig.PlayerOutfitToken`에는 빌린 옷 그대로 두거나 한 줄씩 더한다(입상 그림이 없으면 빌린 옷).
- 나머지 5벌을 무기 겹치기 방식으로 다시 받으면(24F 9절) 그 뒤에 이 스크립트를 다시 돌린다. 그래서 다시 받기 전후 어느 쪽에도 쓸 수 있게 옷 영역 고르기를 색 범위가 아니라 그림별 표(옷 색 목록)로 둔다.

---

## 6. 비용 견적 (로그 평균 단가, 2026-11-25)

PixelLab 기록(`code_temp/apogirl/scratch/pixellab_log.jsonl`)의 평균 단가: 그림 한 장 `image_pro`(generate-image-v2) 약 $0.088, 그림 고치기 `edit_pro` 약 $0.08, 움직임 `animate`(v3) 한 번 약 $0.016.

| 묶음 | 호출 | 예상 |
|---|---|---|
| A 허수아비 정지 | 후보 3장 + 고치기 2장 | 약 $0.43 |
| A 허수아비 움직임 | 2줄 × 다시 받기 포함 3번 | 약 $0.10 |
| B 가방 | 2종 × 후보 3장 | 약 $0.53 |
| **A + B 합계** | | **약 $1.06** (여유 포함 $2.0 상한) |
| C ② 몽둥이 무기 그림 (나중) | 3장 × 후보 2장 | 약 $0.53 |

- 생성 직전에 잔량(무료)을 다시 보고, 상한 $2.5를 넘을 것 같으면 멈추고 묻는다.
- 남는 크레딧(약 $2.5)은 5벌 다시 받기(약 $90)에 크게 모자라니, 그 작업은 크레딧을 채운 뒤 따로 한다.

## 7. 순서 (승인 뒤)

1. 잔량 확인 → A 허수아비 서 있는 그림 후보 3장 → **비교판을 보여 주고 사용자가 고른다.**
2. 고른 그림으로 자명종·쓰러짐 2장, 움직임 2줄.
3. B 가방 2종 후보 3장씩 → 비교판 → 사용자가 고른다.
4. 후보는 `code_temp/apogirl/pl_craft/{decoy,bag}/`에 받고, 고른 것만 `ExplorationPixel/`에 넣는다. 가져오기 설정은 기존 소품·가방과 같다(필터 없음, 압축 없음).
5. `sprite_meta.json`(가방 기준점·2배면 `Ppu` 72), `ItemLooks` 토큰 2칸을 고친다.
6. 탐험 화면 렌더(허수아비 세우기·흔들림·쓰러짐, 가방 4동작 × 하복·동복)를 `Docs/anim_versions/craft_props/`에 두고 보여 준다. EditMode 테스트를 돌린다.
7. PROGRESS·이 문서에 기록하고 커밋한다. 메타 파일의 안드로이드 블록은 지우지 않는다.

## 8. 안전 (마스터 프롬프트 13장, 제작 기획서 3.7절)

- 허수아비는 **얼굴이 없다.** 머리는 엎은 양동이이고 눈·입·표정을 그리지 않는다.
- 사람이나 아이처럼 보이지 않게 어른보다 작고 뭉툭하게, 누가 봐도 물건으로 그린다. 교복 상의는 입히지 않는다(원안의 `school jacket`을 체육복 상의로 바꿨다).
- 쓰러질 때 조각나지 않는다.
- 글자·상표를 넣지 않는다.
- 위 규칙에 어긋난 후보는 고르지 않고 다시 받는다.

## 9. [결정 필요]

| | 항목 | 선택지 | 추천 |
|---|---|---|---|
| E1 | 이번 요청 범위 | ① A 허수아비 + B 가방 ② A만 ③ A + B + C 몽둥이 무기 그림까지 | **①** → **결정 ①** |
| E2 | 가방 해상도 | ① 2배(PPU 72, 36×40·48×60) ② 1배(PPU 36, 기존 가방과 같음 18×20·24×30) | ① → **결정 ② 1배** (기존 가방과 같은 화소) |
| E3 | 몽둥이 3종 | 4절 ①~④ | **②** → **결정 ②** 무기 겹치기 작업 때 무기 그림 3장 |
| E4 | 보호구 3벌 | 5절 ①~③ | ① 빌린 옷 유지 → **결정 ② 색 덧칠 스크립트** |

7절 순서로 진행하고, 후보를 고를 때마다 비교판으로 묻는다. 생성은 사용자가 시작하라고 할 때 한다.
