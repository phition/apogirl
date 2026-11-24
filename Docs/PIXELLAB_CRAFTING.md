# 제작 장비 픽셀 아트 프롬프트: PixelLab (2026-11-24)

[CRAFTING_PROPOSAL.md](CRAFTING_PROPOSAL.md) 결정 C10(사용자 결정 2026-11-24)에 따라 **지금은 기존 그림과 아이콘을 빌려 쓴다.** 이 문서는 나중에 PixelLab에 맡길 프롬프트만 모은 것이다. **아직 아무것도 제출하지 않았다.**
탐험 화면은 픽셀 아트라서 힉스필드가 아니라 PixelLab으로 만든다([PIXELLAB_DEEP.md](PIXELLAB_DEEP.md)와 같은 방식).

> **요청 범위·비용·결정은 [PIXELLAB_CRAFTING_REQUEST.md](PIXELLAB_CRAFTING_REQUEST.md)(2026-11-25)를 따른다.** 이 문서는 프롬프트 원안이다. 그 뒤 주인공 그림이 24프레임·무기 겹치기 방식으로 바뀌어, 몽둥이·보호구의 길은 요청 문서 4·5절이 최신이다.

- 도구는 `code_temp/apogirl/tools/pixellab.py`를 쓴다.
  - 한 장짜리: `image_pro`
  - 움직임: `animate`
  - 그림 고치기: `edit_pixen`·`edit_pro`
  - 무기를 휘두르는 묶음: `pl_swing_batch.py` 방식
  - 가방 색 바꾸기: `bag_colors.py`
- 넣는 곳은 `Resources/Art/ExplorationPixel/`(PPU 36)다. 후보 여러 장 가운데 사용자가 고른 것만 넣는다.
- 그림이 없으면 빌린 그림(`exploration_art.json` `ItemLooks`)이 그려진다. 일부만 만들어도 화면이 깨지지 않는다.
- 콘텐츠 안전(마스터 프롬프트 13장)을 지킨다.
  - 허수아비는 사람이나 아이처럼 그리지 않는다. 마대 몸통에 양동이 머리를 얹고 얼굴은 없다.
  - 넘어질 때 조각나지 않는다.
  - 글자와 상표를 넣지 않는다.

## 0. 한눈에 보기

| 절 | 무엇 | 지금(빌린 그림) | 장수 | 비용 | 순서 |
|---|---|---|---|---|---|
| 1 | 가방 겹 그림 3종 × 색 7 | `tote`·`school`·`hiking` | 3장 + 색 바꾸기 18장 | 아주 적음 | **1** |
| 2 | 탐험 허수아비 소품 | 코드 그림(사각형) | 정지 2장 + 움직임 2줄(6프레임씩) | 적음 | **2** |
| 3 | 몽둥이 무기 3종 | `bat`·`bat`·`pipe` | 무기마다 옷 6벌 × 약 140프레임 | 매우 큼 | 3 (선택) |
| 4 | 보호구 옷 3벌 | `guard`·`padding`·`riot` | 옷마다 무기 5종 × 약 140프레임 | 매우 큼 | 4 (선택) |

- 3·4절은 비용이 커서, 사용자가 빌린 그림이 어색하다고 느낄 때만 한다.
- 3·4절에는 싼 길(지금 프레임을 고쳐 그리는 스크립트)도 함께 적었다.

### 0.1 지금 빌려 쓰는 그림

| 새 아이템 | 탐험·입상 그림 토큰 | 아이콘 (없는 동안) |
|---|---|---|
| `ITEM_BAG_SLING` 끈 주머니 가방 | `tote` | `item_bag_sling` (여분 있음) |
| `ITEM_BAG_RUCKSACK` 덧댄 배낭 | `school` | `item_bag_rucksack` (여분 있음) |
| `ITEM_BAG_FRAME` 프레임 배낭 | `hiking` | `item_bag_frame` (여분 있음) |
| `ITEM_WEAPON_CLUB` 나무 몽둥이 | `bat` | `item_weapon_bat` |
| `ITEM_WEAPON_CLUB_TAPED` 테이프 감은 몽둥이 | `bat` | `item_weapon_bat` |
| `ITEM_WEAPON_CLUB_REBAR` 철근 덧댄 몽둥이 | `pipe` | `item_weapon_rebar` (여분 있음) |
| `ITEM_ARMOR_MAGAZINE` 잡지 보호대 | `guard` | `item_armor_magazine` (여분 있음) |
| `ITEM_ARMOR_MATPAD` 매트 덧댄 보호구 | `padding` | `item_armor_kneepads` (여분, 가깝다) |
| `ITEM_ARMOR_PLATED` 철판 덧댄 조끼 | `riot` | `item_vest_utility` (여분, 가깝다) |
| `ITEM_DECOY_DUMMY`·`_BELL` 휴대·울리는 허수아비 | 코드 그림 | `item_lure_alarm` (여분 있음, 둘 다) |
| 자동 정수기(단면도) | `hub_obj_boil_station` | `icon_craft_workbench` |

- 아이콘이 여분에도 없으면 연결할 때 사용자에게 요청한다.
- 아이콘은 UI 그림이라 PixelLab 몫이 아니다. 새로 만들 목록은 기획서 6.2절에 있다.

## 0.2 공통 문구 (모든 프롬프트 앞에 붙인다)

```
side-view 2D pixel art sprite for a side-scrolling zombie survival game set in a Korean high school,
flat orthographic side view, muted realistic colors, thin dark outline, handmade from scavenged school materials, no text, no logos
```

- 소품은 **투명 배경**으로 만든다.
- 탐험은 어두워서 손전등으로 본다. 그림은 **중간 밝기**로 만들고 어둠은 게임 조명에 맡긴다.
- style 이미지는 절마다 적은 기존 그림을 쓴다.

## 1. 가방 겹 그림 (순서 1)

주인공 프레임 위에 등 쪽으로 겹쳐 그리는 작은 그림이다.
- 자리는 `s11_bag.py`가 프레임마다 계산한 기준점(`sprite_meta.json`)을 그대로 따른다.
- 기존 크기: `bag_school` 16 × 17, `bag_tote` 22 × 22, `bag_hiking` 22 × 27.
- 옆에서 본 모습이고, 몸에 닿는 쪽이 왼쪽이다. 기존 그림과 방향을 맞춘다.

| 파일 (키) | 크기 | style | 프롬프트 (공통 문구 뒤) |
|---|---|---|---|
| `bag_sling` | 18 × 20, 투명 | `bag_tote` | `A small drawstring sling pouch sewn from a faded school curtain fabric, a single knotted rope strap, lumpy because it is half full, seen from the side as worn on the back, transparent background` |
| `bag_rucksack` | 18 × 20, 투명 | `bag_school` | `A school backpack reinforced with extra patches cut from a gym mat and a canvas bag, visible thick hand stitching, duct-tape repaired corners, a tied-on side pocket, seen from the side as worn on the back, transparent background` |
| `bag_frame` | 24 × 30, 투명 | `bag_hiking` | `A large improvised frame backpack: a big canvas sack lashed with rope onto a light frame made from metal chair legs, the frame ends stick out a little at the top and bottom, a rolled blanket strapped under it, seen from the side as worn on the back, transparent background` |

- 색 6벌은 `bag_colors.py`의 `for kind in (...)`에 `sling`·`rucksack`·`frame`을 더해 바탕색만 바꿔 만든다.
- 연결은 `ItemLooks` 표에서 새 가방의 토큰을 `sling`·`rucksack`·`frame`으로 바꾸는 것으로 끝난다.
- 기준점은 `sprite_meta.json`에 기존 가방처럼 한 줄씩 적는다.

## 2. 탐험 허수아비 소품 (순서 2)

휴대 허수아비(`ITEM_DECOY_DUMMY`)와 울리는 허수아비(`ITEM_DECOY_BELL`)를 탐험 바닥에 세운 모습이다.
- 게임에서는 1 × 2칸쯤으로 그린다. 바닥에 선 큰 장식이라 그림자도 드리운다(`ExplorationLook.AddCaster`).
- style 이미지는 `prop_locker`, 그리고 장식 `decor_trashbags`.

| 파일 (키) | 크기 | style | 프롬프트 (공통 문구 뒤) |
|---|---|---|---|
| `prop_decoy_stand` | 36 × 72, 투명 | `decor_trashbags` | `A crude standing decoy dummy: a stuffed burlap sack body tied around a mop handle, an upturned dented metal bucket as the head with no face, an old school jacket draped over the shoulders, empty tin cans hanging from strings at the waist, its base is a broken chair wedged on the floor, clearly an object not a person, transparent background` |
| `prop_decoy_down` | 72 × 36, 투명 | `prop_decoy_stand`(위 결과) | 위 허수아비가 **통째로** 옆으로 쓰러져 바닥에 누운 모습. `the bucket head rolled slightly away, cans scattered, nothing broken into pieces` |
| `prop_decoy_bell_stand` | 36 × 72, 투명 | `prop_decoy_stand`(위 결과) | 위와 같은 허수아비, `with an old round twin-bell alarm clock tied to the bucket head with wire` |

움직임(`animate`, 첫 장은 위 결과):

| 키 (프레임) | 첫 장 | 동작 설명 (`action`) | 프레임 |
|---|---|---|---|
| `prop_decoy_sway_0`~`5` | `prop_decoy_stand` | `the dummy wobbles side to side on its base, the hanging tin cans swing and clatter` | 6 |
| `prop_decoy_bell_ring_0`~`5` | `prop_decoy_bell_stand` | `the alarm clock on the bucket head shakes and rings, the whole dummy trembles slightly` | 6 |

- 흔들림은 허수아비가 소음을 낼 때(좀비를 끌 때) 돈다. 쓰러짐은 좀비가 부쉈거나 내구가 다했을 때 한 장으로 보여 준다.
- 좀비가 허수아비를 때리는 동작은 기존 좀비 공격 그림을 그대로 쓴다.

## 3. 몽둥이 무기 3종 (선택, 순서 3)

지금 빌린 그림은 방망이(`bat`)·방망이·파이프(`pipe`)다. 손에 쥔 모양이 비슷해서 빌려도 크게 어색하지 않다. 아래 셋 중에서 고른다.

| 길 | 무엇 | 비용 |
|---|---|---|
| ① 빌리기 유지 | 아이콘만 다르고 탐험 그림은 방망이·파이프 | 0 |
| ② 고쳐 그리기 스크립트 | 방망이 프레임에서 무기 픽셀(방망이 색 영역)만 골라 덧칠한다. 나무 몽둥이는 색을 거칠게 바꾸고, 테이프 몽둥이는 손잡이 쪽에 회색 띠, 철근 몽둥이는 굵은 쪽에 짙은 쇠막대 두 줄 | 스크립트 하나, PixelLab 비용 없음 |
| ③ PixelLab 새 묶음 | `pl_swing_batch.py`처럼 옷 6벌 × 동작 전부(공격 24·대기 24·걷기 16·달리기 12·웅크림 16·지침 24·다침 24, 약 140프레임) | 무기마다 약 840프레임 |

③으로 할 때 무기 문구(`pl_swing_batch.py`의 `WEAPONS`에 넣는 꼴):

| 토큰 | 문구 |
|---|---|
| `club` | `a rough wooden club carved from a school desk leg, thick at the end, pale raw wood with splinters` |
| `club_taped` | `the same wooden club with the grip and the striking end wrapped tightly in grey duct tape` |
| `club_rebar` | `a heavy wooden club reinforced with two short pieces of rusty rebar bound along the striking end with wire` |

- 먼저 `pl_swing_sample.py` 방식으로 한 묶음(교복 `uniform`)만 샘플을 만든다. 사용자가 고른 뒤에 전부 만든다.
- 무기가 닿는 순간과 프레임 차례는 `exploration_art.json` `Combat.ImpactAt`에 무기별로 한 줄씩 더한다. 방망이 값 0.5에서 시작한다.

## 4. 보호구 옷 3벌 (선택, 순서 4)

지금 빌린 그림은 보호대(`guard`)·패딩(`padding`)·진압 조끼(`riot`)다. 옷 한 벌이 무기 5종(맨손 포함) × 모든 동작이라 가장 비싸다.

| 길 | 무엇 | 비용 |
|---|---|---|
| ① 빌리기 유지 | 탐험 그림은 비슷한 기존 옷 | 0 |
| ② 고쳐 그리기 스크립트 | 기존 옷 프레임의 옷 색 영역만 바꾸고 무늬를 덧칠한다. 잡지 보호대는 보호대 그림의 정강이·팔뚝에 종이 띠, 매트 보호구는 패딩 그림을 파란 매트색으로, 철판 조끼는 진압 조끼 그림의 가슴판을 녹슨 회색으로 | 스크립트 하나 |
| ③ PixelLab 새 묶음 | 옷 한 벌 × 무기 5종 × 동작 전부 | 옷마다 약 800프레임 |

③으로 할 때 옷 문구:

| 토큰 | 문구 |
|---|---|
| `magazine` | `school uniform with rolled-up thick magazines taped around the shins and forearms as makeshift guards` |
| `matpad` | `school uniform with pieces of a blue gym mat strapped over the chest, shoulders and thighs with belts` |
| `plated` | `a padded vest with scavenged dented metal plates sewn onto the chest and back, over the school uniform` |

- 옷 토큰을 새로 더하면 입상 `StandingConfig.PlayerOutfitToken` 표에도 한 줄씩 더한다(입상 그림이 없으면 빌린 옷).

## 5. 넣는 차례 (만들게 되면)

1. 후보 그림은 `code_temp/apogirl/pl_craft/{절}/`에 받는다. 사용자가 고른 것만 `ExplorationPixel/`에 넣는다.
2. 가방은 `bag_colors.py`로 색을 바꾸고 `sprite_meta.json`에 기준점을 적은 뒤, `ItemLooks` 토큰을 바꾼다.
3. 허수아비는 `exploration_art.json`에 소품 키를 적는다. 탐험 코드에서는 허수아비를 세우는 곳(기획서 P6)이 이 키를 쓴다.
4. 무기·옷(③)은 기존 설치 도구(`pl_swing_install.py`·`s11_install_all.py`) 방식으로 크기를 맞추고 발을 바닥 줄에 붙인 뒤 넣는다.
5. 메타 파일의 안드로이드 블록은 지우지 않는다(사용자 결정 2026-11-24).
