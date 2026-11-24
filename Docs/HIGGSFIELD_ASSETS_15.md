# 힉스필드 에셋 15차: 제작 장면 컷과 제작 아이콘·설비·소리 (2026-11-25)

제작 P8에서 짧은 장면 4편(`Events/Craft.yarn`)을 넣었다. 지금은 네 편 모두 빈 방 배경을 빌려 쓴다. 이 문서는 그 장면의 컷 4장과, 제작 기획서([CRAFTING_PROPOSAL.md](CRAFTING_PROPOSAL.md)) 6.2절에서 "없는 것"으로 남은 아이콘·단면도 설비 그림·효과음을 한 번에 받는 문서다.

쓰는 법:

- 쓰는 법·모델·공통 공식은 [HIGGSFIELD_ASSETS.md](HIGGSFIELD_ASSETS.md) 0장을 따른다.
  - `[F-ILLU]` = 0.3 일러스트 공식, `[F-KOREA]` = 한국 건물 문구, `[F-UI]` = 아이콘 공식, `[F-AUDIO]` = 소리 공통, `[안전]` = 0.4 안전 문구.
  - 계절 문구 `[봄]`·`[여름]`·`[가을]`·`[겨울]`은 [HIGGSFIELD_ASSETS_8.md](HIGGSFIELD_ASSETS_8.md) 0.3에 있다.
- 인물 문구 `{SEOJIN}`·`{SEOJIN_SUMMER}`·`{TEACHER}`, `[일관성]`, 윤 선생님 다리 문구 ①~④는 [HIGGSFIELD_ASSETS_13.md](HIGGSFIELD_ASSETS_13.md) 1절을 그대로 쓴다.
- 인물이 나오는 컷은 그 인물의 시트(`ArtInbox/illustrations/char_*_sheet.png`)를 참고 이미지로 넣는다. 교복 차림이면 14차 1절의 **[명찰]** 줄을 프롬프트 끝에 붙인다.
- 프롬프트는 영어, 설명은 한국어로 쓴다. 표의 프롬프트 칸 하나가 생성 하나다.
- **작업 제출은 사용자가 한다.** 나(Claude)는 이 문서만 만든다.
- 탐험 바닥에 서는 허수아비(`prop_decoy_*`)와 새 장비의 탐험 그림은 PixelLab 몫이라 [PIXELLAB_CRAFTING.md](PIXELLAB_CRAFTING.md)에 있다. 여기서는 다루지 않는다.

**허수아비 안전 규칙** (마스터 프롬프트 13장, 제작 기획서 3.7절)
- 허수아비에는 **얼굴이 없다.** 머리는 엎은 양동이다. 눈·입·표정을 그리지 않는다.
- 사람이나 아이처럼 보이지 않게 한다. 키·몸집은 어른보다 작고 뭉툭하며, 누가 봐도 물건이어야 한다.
- 프롬프트마다 `no face, clearly an object, not a person, not a child`를 넣었다. 얼굴처럼 보이는 무늬가 생기면 다시 받는다.

---

## 0. 한눈에 보기

| 절 | 무엇 | 우선 | 개수 | 받는 곳 |
|---|---|---|---|---|
| 1 | 제작 장면 컷 | **P1** | 4 | `ArtInbox/illustrations/events15/` |
| 2 | 아이템·설비 카드 아이콘 | P2 | 6 | `ArtInbox/ui/` |
| 3 | 단면도 설비 그림 | P2 | 4 | `ArtInbox/props/` |
| 4 | 효과음 | P3 | 4 | `ArtInbox/audio/` |
| | **합계** | | **18** | |

**먼저 받을 것**: 1절 4장. 대본과 그림이 가장 어긋나는 곳이다(작업대 장면에 빈 창고, 허수아비 장면에 빈 교문).

---

## 1. 제작 장면 컷 (4장, P1)

16:9, 2k 이상. 받은 그림은 `ArtInbox/illustrations/events15/`에 같은 파일 이름으로 둔다. 대본은 `Resources/Dialogue/Events/Craft.yarn`.

| 파일 | 이벤트 (지금 빌려 쓰는 그림) | 계절 | 참고 시트 | 프롬프트 |
|---|---|---|---|---|
| `evt_craft_workbench.png` | `EVT_CRAFT_WORKBENCH` 작업대 첫날 (`bg_storage`) | `[봄]` | 서진, 윤 선생님 | `Daytime in the school's main storage room shelter, two grey steel teacher's desks pushed together into a workbench, small wooden blocks wedged under their legs to level them. On the wall above, old hand tools hang in a neat row: a hammer, a hand saw, pliers, files. {SEOJIN} leans over the corner of the desk writing a short word on it with a black marker in slightly crooked handwriting (unreadable). {TEACHER} +다리① sits on a student chair beside the workbench, running his palm across the dusty desktop and looking at the dust on his hand with a small amused smile. Shelves of boxes behind them, a high small window. Only these two characters appear.` |
| `evt_craft_scarecrow.png` | `EVT_CRAFT_SCARECROW` 첫 허수아비 (`bg_school_gate`) | `[봄]` | 서진, 윤 선생님 | `Dusk at the front gate of a Korean high school, a crude scarecrow dummy is lashed to the outside of the iron gate bars: a stuffed burlap sack body on two mop handles tied in a cross, an old faded tracksuit jacket, an upturned dented metal bucket as the head with no face and nothing drawn on it, empty tin cans hanging from strings that sway in the wind. It is clearly an object, not a person, not a child, shorter than an adult. Inside the gate {SEOJIN} stands holding a black marker with the cap back on, looking at the blank bucket. {TEACHER} +다리② leans on his crutch beside her, looking at the dummy, calm. Empty street beyond the gate. Only these two characters appear.` `[F-KOREA]` |
| `evt_craft_purifier.png` | `EVT_CRAFT_PURIFIER` 자동 정수기 첫 아침 (`bg_science`) | `[여름]` | 서진, 윤 선생님 | `Early morning in a school science lab, on the lab bench a homemade water purifier: two stacked translucent plastic water tanks connected by a clear hose and a small electric pump, a thin cable running to a bicycle generator in the corner; the lower tank is half full of clear water. {TEACHER} +다리② stands by the bench holding a glass cup of water up to the sunlight from the window, studying it. {SEOJIN_SUMMER} stands beside him with a toothbrush in her mouth, sleepy, looking at the cup. Lab stools, a periodic table poster (unreadable), soft morning light. Only these two characters appear.` |
| `evt_craft_frame_pack.png` | `EVT_CRAFT_FRAME_PACK` 프레임 배낭 (`bg_homeec`) | `[가을]` | 서진, 윤 선생님 | `Afternoon in a school home economics room with sewing machines on the tables, {SEOJIN} stands wearing a large homemade frame backpack: a frame of bent aluminium chair legs, a big pouch sewn from thick curtain fabric, shoulder straps cut from car seat belts, a padded back panel of gym mat foam. {TEACHER} +다리② stands behind her, his crutch leaning on a table, pulling the waist belt tight with both hands; she stands up straight, a little surprised at how light it feels. Fabric scraps and thread on the table, autumn light. Only these two characters appear.` |

- 계절은 장면이 열리는 때에 맞췄다. 작업대는 2주차부터라 봄, 허수아비는 깡통 경보 뒤라 봄, 자동 정수기는 끓임 정수대·전력 복구·과학 숙련 2 뒤라 여름, 프레임 배낭은 작업대 Ⅱ·공학 숙련 2 뒤라 가을로 잡았다. 실제로는 다른 계절에 나올 수 있지만 한 장씩만 받는다.
- 윤 선생님 다리는 의족 갈래(`PROSTHETIC_DONE`)에 따라 달라지므로, 2막 이후 컷도 모든 판에 맞는 목발(②)로 그렸다. 1막 초반 작업대는 앉아 있는 ①이다.
- 작업대 장면의 도현 대사는 합류했을 때만 나오므로 그림에는 넣지 않는다.
- 허수아비 컷은 위 안전 규칙을 지켜야 한다. 양동이에 무엇이든 그려져 있으면 다시 받는다.

---

## 2. 아이템·설비 카드 아이콘 (6개, P2)

모두 `[F-UI]`. **1:1, 512px 이상, 투명 배경**으로 받고 내가 줄인다. 아이템 아이콘은 `[F-UI]`의 끝을 `transparent background, item icon, three-quarter view`로 바꿔 쓴다(기본 문서 아이템 절). 설비 아이콘은 기존 `icon_craft_*`와 같은 화풍으로, 기존 아이콘 한두 장(`icon_craft_alarm_cans.png`·`icon_craft_barricade_desks.png`)을 참고 이미지로 넣는다. 한 번에 "아이콘 세트, 같은 스타일로 6개"로 요청하면 통일감이 좋다.

| 파일 | 쓰는 곳 | 지금 빌려 쓰는 것 | 프롬프트 (`[F-UI]` 뒤에) |
|---|---|---|---|
| `item_weapon_club.png` | 나무 몽둥이 `ITEM_WEAPON_CLUB` | `item_weapon_bat` | `a crude wooden club whittled from a school desk leg, rough knife-carved grip, a few old screw holes, item icon, three-quarter view` |
| `item_weapon_club_taped.png` | 테이프 감은 몽둥이 `ITEM_WEAPON_CLUB_TAPED` | `item_weapon_bat` | `the same crude wooden club from a desk leg, its grip tightly wrapped in grey duct tape, a tape strip also reinforcing the head, item icon, three-quarter view` |
| `item_decoy_dummy.png` | 휴대 허수아비 `ITEM_DECOY_DUMMY` | `item_lure_alarm` | `a small folded carry-sized decoy dummy: a stuffed burlap sack bundle tied with rope to a short mop handle, a small dented metal bucket as the head with no face, two empty tin cans on strings, clearly an object not a person, item icon, three-quarter view` |
| `item_decoy_bell.png` | 울리는 허수아비 `ITEM_DECOY_BELL` | `item_lure_alarm` | `the same small burlap decoy dummy with a dented bucket head with no face, an old round twin-bell alarm clock wired on top of the bucket, clearly an object not a person, item icon, three-quarter view` |
| `icon_craft_scarecrow.png` | 교문 허수아비 카드 `ACT_CRAFT_SCARECROW`·`_BELL` | `item_lure_alarm` | `a crude scarecrow dummy on crossed mop handles with a burlap sack body and an upturned bucket head with no face, tin cans hanging on strings, standing behind a short section of iron school gate bars, clearly an object not a person` |
| `icon_craft_auto_purifier.png` | 자동 정수기 카드 `ACT_CRAFT_AUTO_PURIFIER` | `icon_craft_water_filter` | `two stacked translucent plastic water tanks joined by a clear hose and a small electric pump with a short power cable and a tiny lightning mark, clear water in the lower tank` |

- 허수아비 세 개는 1절 안전 규칙을 그대로 지킨다.
- 그 밖의 새 장비(덧댄 배낭·프레임 배낭·쇠심 몽둥이·잡지 보호대·덧댄 보호대·헤드랜턴 등)는 지금 빌린 아이콘이 크게 어긋나지 않아 이번에는 받지 않는다. 빌린 목록은 [PIXELLAB_CRAFTING.md](PIXELLAB_CRAFTING.md) 0.1절.

---

## 3. 단면도 설비 그림 (4개, P2)

기본 문서의 허브 그림 규칙을 따른다: `[F-ILLU]`의 `Background:` 문장 + `single object, semi-photorealistic, transparent background, soft shadow`. 1:1 또는 세로, 1k 이상. 이미 들어간 `hub_obj_workbench.png`·`hub_obj_boil_station.png`(`Resources/Art/UI/HubObj/`)를 참고 이미지로 넣어 화풍을 맞춘다.

| 파일 | 자리 | 프롬프트 (`[F-ILLU]` `Background:` 문장 뒤에) |
|---|---|---|
| `hub_obj_scarecrow.png` | 현관 밖, 교문 허수아비 서 있음 (`SCARECROW_UP`) | `a crude scarecrow dummy standing upright on crossed mop handles wedged into a pile of bricks, stuffed burlap sack body, an old faded tracksuit jacket, an upturned dented metal bucket as the head with no face and nothing drawn on it, empty tin cans hanging from strings, clearly an object not a person not a child, front view, single object, semi-photorealistic, transparent background, soft shadow` |
| `hub_obj_scarecrow_down.png` | 넘어진 허수아비 (내구 0, 다시 세우기 전) | `the same crude scarecrow dummy knocked over and lying on its side on the ground, the bucket head rolled a little away, cans scattered, nothing torn apart, no face, clearly an object, single object, semi-photorealistic, transparent background, soft shadow` |
| `hub_obj_scarecrow_bell.png` | 울리는 허수아비 (`SCARECROW_BELL`) | `the same crude standing scarecrow dummy with an old round twin-bell alarm clock wired to the top of its faceless bucket head, a thin wire running down the mop handle, clearly an object not a person, front view, single object, semi-photorealistic, transparent background, soft shadow` |
| `hub_obj_auto_purifier.png` | 과학실, 자동 정수기 (`AUTO_PURIFIER`) | `a homemade automatic water purifier on a small lab cart: two stacked translucent plastic water tanks joined by a clear hose, a small electric pump in between, a thin cable trailing off to the side, clear water in the lower tank, a tiny green indicator light, single object, semi-photorealistic, transparent background, soft shadow` |

- 넘어진 허수아비는 지금 규칙에 "넘어짐" 상태가 따로 있을 때 쓴다(교문 허수아비 내구 `Base.Scarecrow`가 0이 된 주). 없으면 연결할 때 서 있는 그림만 쓴다.

---

## 4. 효과음 (4개, P3)

모두 프롬프트 끝에 `[F-AUDIO]`. wav 또는 ogg, 모노 가능. 받는 곳 `ArtInbox/audio/`.

| 파일 | 길이 | 쓰는 곳 | 프롬프트 |
|---|---|---|---|
| `sfx_decoy_rattle.wav` | 2s | 탐험: 휴대 허수아비가 소리를 낼 때 | `empty tin cans on strings knocking against each other and a dented metal bucket, swaying in the wind, hollow clattering, no voice` |
| `sfx_decoy_bell.wav` | 3s | 탐험: 울리는 허수아비의 자명종 | `an old mechanical twin-bell alarm clock ringing loudly and rattling against a metal bucket, slightly muffled outdoors, no voice` |
| `sfx_decoy_fall.wav` | 1.5s | 탐험: 허수아비가 쓰러질 때 | `a light stuffed burlap dummy toppling over onto concrete, a metal bucket clanging and rolling, tin cans scattering, no voice` |
| `sfx_craft_done.wav` | 1.5s | 허브: 제작 완성 결과 한 줄 | `a last hammer tap on wood, then a short satisfied pat on a finished wooden object, quiet workshop room tone, no music, no voice` |

---

## 5. 연결 (받으면 내가 한다)

- **장면 컷**: 16:9·너비 1920 jpg로 바꿔 `Resources/Art/Story/Events/`에 넣고, `Resources/Art/art_map.json` Events에서 `EVT_CRAFT_*` 네 줄의 `Image`를 `Backgrounds/bg_*`에서 `Events/evt_craft_*`로 바꾼다. 도구는 `code_temp/apogirl/tools/install_events12.py`.
- **아이콘**: `Resources/Art/UI/`에 같은 이름으로 넣고, `art_map.json` Icons에서 빌려 쓰던 줄(`item.ITEM_WEAPON_CLUB`·`item.ITEM_WEAPON_CLUB_TAPED`·`item.ITEM_DECOY_DUMMY`·`item.ITEM_DECOY_BELL`·`act.ACT_CRAFT_SCARECROW`·`act.ACT_CRAFT_SCARECROW_BELL`·`act.ACT_CRAFT_AUTO_PURIFIER`)의 `Image`를 새 이름으로 바꾼다.
- **단면도 설비**: 384px로 맞춰 `Resources/Art/UI/HubObj/`에 넣고, `Resources/Art/hub_section.json`의 현관 `ENTRANCE`(깃발 `SCARECROW_UP`·`SCARECROW_BELL`)·과학실 `SCIENCE`(`AUTO_PURIFIER`) `Built` 줄에 한 줄씩 더한다(P7의 작업대·끓임 정수대와 같은 방식).
- **효과음**: `Presentation/Audio/Sound/`에 넣고 `Resources/Audio/sound_map.json`에 키를 더한 뒤, 허수아비 소음(`ThreatSimulation.AddDecoy` 쪽)·쓰러짐·제작 완성 결과에서 부른다.
- 쓰는 곳이 없어진 빌린 그림은 지우지 않는다.

## 6. 받는 차례 (추천)

1. 1절 4장 (P1).
2. 2절 아이콘 6개 → 3절 설비 그림 4개 (P2).
3. 4절 효과음 4개 (P3).
