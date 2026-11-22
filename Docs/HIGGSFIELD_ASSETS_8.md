# 힉스필드 에셋 8차: 빈 그림 채우기 · 개인 장면 · CD · 깊이 (2026-11-22)

두 가지를 모았다.
- **(가) 이미 만든 기능인데 그림이 빈 것**: 그림이 없는 이벤트, 대사 초상이 없는 화자, 역할·외출 아이콘.
- **(나) 기획서에서 정했고 곧 만들 기능에 필요한 것**: 동료 개인 장면 사다리와 특기([PROPOSAL_CD_DEPTH_EXPANSION.md](PROPOSAL_CD_DEPTH_EXPANSION.md) C.2 ②), 윤 선생님 개인 장면(C.1), CD 수집(A), 탐험의 깊이(B), 고양이(2), 동아리(7), 방송반 송출(5), 계절 재해(6).

- 쓰는 법·모델·공통 공식은 [HIGGSFIELD_ASSETS.md](HIGGSFIELD_ASSETS.md) 0장을 따른다. `[F-ILLU]` = 0.3 일러스트 공식, `[F-KOREA]` = 한국 건물 문구, `[F-UI]` = UI 공식, `[F-AUDIO]` = 소리 공통, `[안전]` = 0.4 안전 문구.
- 프롬프트는 영어, 설명은 한국어. 회색 상자나 표의 프롬프트 칸 하나가 생성 하나다.
- 인물이 나오면 기준 시트(`ArtInbox/illustrations/char_*_sheet.png`)를 참고 이미지로 넣는다.
- **넣는 곳**: `ArtInbox/` 아래 이 문서의 폴더·파일 이름 그대로 넣으면 내가 크기를 맞춰 `Resources`로 옮기고 표(json)에 잇는다. 각 절에 "잇는 곳"을 적었다.
- 이벤트 컷은 모두 16:9(2k 이상), 대사 초상은 3:4, 아이콘은 1:1 투명 배경이다.

> **탐험 픽셀 아트는 이 문서에 없다.** 탐험 화면(`Resources/Art/ExplorationPixel/`)과 허브 단면도의 방·사람 그림은 PixelLab으로 내가 만든다. 맨 끝 "만들지 않는 것"에 정리했다.

---

## 0. 한눈에 보기

### 0.1 요약표

| 번호 | 파일 | 쓰이는 곳 | 우선 | 상태 |
|---|---|---|---|---|
| 1.1 | `char_wanderer_neutral.png` | 대사 초상 (화자 "유랑자", 대사 7줄) | **P1** | 비어 있음 |
| 1.2 | `evt_talk_12`, `evt_a4_sageori_after`, `evt_sv_join`, `evt_a4_cured` | 5차 문서 7절의 다시 만들 것 | **P1** | 비어 있음 (2장 미연결, 2장 교체 대기) |
| 1.3 | `evt_dh_*`, `evt_nurse_*`, `evt_by_*`, `evt_hr_*`, `evt_wanderer_*` 등 21장 | 동료 이벤트 대화 배경 | **P1** | 비어 있음 |
| 1.4 | `evt_a1_*`, `evt_a2_*`, `evt_a3_*` 19장 | 1~3막 이야기 이벤트 대화 배경 | **P1** | 비어 있음 |
| 1.5 | `evt_cal_*` 7장, `evt_sys_first_science`, `evt_wx_*` 3장 | 달력·시스템·날씨 이벤트 | P1 | 비어 있음 |
| 1.6 | `icon_role_*` 6장, `icon_status_away`, `icon_status_minor_wound`, `icon_stay_home` | 인물 서랍·동료 단추 (지금은 글자만) | P1 | 비어 있음 (코드 연결 작음) |
| 2.1 | `evt_bond_{동료}_{1~3}` 18장, `icon_special_*` 6장 | 동료 개인 장면 사다리 (호감 20·50·80), 특기 | **P2** | 새 기능 (시작) |
| 2.2 | `evt_bond_teacher_1`, `_2` | 윤 선생님 개인 장면 (호감 50·80) | P2 | 새 기능 |
| 2.3 | `evt_cat_*` 5장 (+ 결정에 따라 1장 다시) | 고양이 동료 | P2 | 새 기능, `[결정 필요]` |
| 2.4 | `cd_01~08` 표지 8, 음원 8, `hub_obj_cd_*` 3, `bg_title_music`, `evt_cd_shelf_full`, 효과음 3 | CD 수집·방송반 진열장·타이틀 "음악실" | P2 | 새 기능 |
| 2.5 | `evt_deep_door`, `evt_deep_oneway`, 소리 4 | 탐험의 깊이 (깊은 문·일방 출구) | P2 | 새 기능 |
| 3.1 | `icon_act_club_*` 4, `evt_club_*` 4 | 방과후 동아리 | P3 | 새 기능 |
| 3.2 | `evt_bc_*` 3, `icon_act_broadcast_send`, 소리 2 | 방송반 송출 | P3 | 새 기능 |
| 3.3 | `evt_season_*` 2, `icon_hazard_*` 2, 소리 2 | 계절 재해 지도 (장마 침수·폭설) | P3 | 새 기능 |
| 3.4 | `stand_item_*` 8 | 주인공 입상 든 것 (레이어 방식일 때만) | P3 | 비어 있음 (지금은 안 쓰임) |

### 0.2 개수

| 우선 | 묶음 | 개수 |
|---|---|---|
| **P1 지금 바로 쓰이는 것** | 초상 1, 다시 만들 것 4, 이벤트 컷 51, 아이콘 9 | **65** |
| **P2 곧 만들 것** | 개인 장면 20, 특기 아이콘 6, 고양이 5(+1), CD 24, 깊이 6 | **61** (+1) |
| **P3 나중** | 동아리 8, 송출 6, 계절 재해 6, 입상 든 것 8 | **28** |

**먼저 만들 것**: 1.1(초상 하나), 1.2(다시 만들 것), 그다음 2.1(개인 장면 사다리가 지금 작업 중인 기능이다).

### 0.3 이 문서의 공통 문구

계절은 장면에 맞춰 하나를 붙인다.

`[봄]`
```
Season: Korean spring, cherry blossoms and fresh pale green leaves, soft clear light.
```
`[여름]`
```
Season: humid Korean summer, heavy green trees, hazy warm light, cicada season.
```
`[가을]`
```
Season: Korean autumn, yellow ginkgo and red maple leaves, low golden light.
```
`[겨울]`
```
Season: deep Korean winter, snow, bare branches, cold blue light, breath fogging.
```

인물 문구 (참고 시트와 함께 넣는다):

| 인물 | 시트 | 프롬프트 안에서 쓰는 문구 |
|---|---|---|
| 주인공 서진 | `char_seojin_sheet.png` | `a 16-year-old Korean girl with shoulder-length straight black hair in a navy school uniform` (겨울엔 `under a worn padded jacket`) |
| 윤 선생님 | `char_teacher_sheet.png` | `a Korean male teacher in his mid-40s with thin glasses and a grey cardigan` + 다리 문구(아래) |
| 박 선생님 | `char_nurse_sheet.png` | `a Korean school nurse in her 40s with a neat short bob, a white nurse gown over a quilted padded vest` |
| 도현 | `char_dohyeon_sheet.png` | `a thin 15-year-old Korean boy with short messy hair in an oversized dark green tracksuit jacket` |
| 송예지 | `char_bystander_sheet.png` | `a slim 16-year-old Korean girl with long straight hair tied low, in a slightly oversized navy uniform` |
| 최하린 | `char_ringleader_sheet.png` | `a thin 16-year-old Korean girl with shoulder-length dyed brown hair grown out at the roots, sharp but tired eyes, a borrowed adult parka` |
| 2학년 선배 | `char_senior_sheet.png` | `a tall quiet 17-year-old Korean male student in a faded navy gym jacket, a long pale fully healed scar from his left cheek down his neck` |
| 유랑자 (얼굴 가림) | `char_wanderer_sheet.png` | `an adult wanderer in a worn faded hiking jacket and a wide-brim hat shadowing the face` |
| 백 (모자를 젖힌 뒤) | `char_wanderer_sheet.png` + `Portraits/char_baek_neutral` | `a sun-weathered Korean man in his 50s, former mountain guide, wide-brim hat pushed back, deep wrinkles, calm eyes` |

**윤 선생님 다리 문구** (13장, 그 막에 맞춰 하나):
- 1막 목발 전: `sitting on gym mats, his left trouser leg folded and pinned below the knee, the leg not visible`
- 1막 목발 뒤: `leaning on a homemade crutch made from a mop handle and tape, his left trouser leg folded and pinned below the knee`
- 2막 의족 뒤: `a simple homemade prosthetic lower leg of plastic pipe and padding visible below the left trouser hem, a cane`
- 3막 후반~4막 병세: `pale and feverish, wrapped in a blanket, a crutch leaning nearby, the left trouser leg folded below the knee`

---

## 1. P1 — 지금 바로 쓰이는 것 (그림이 빈 기능)

### 1.1 유랑자 대사 초상 (1장, 3:4)

- 대사에 "유랑자" 화자가 7줄 나오는데 `art_map.json` `Speakers`에 없어 초상이 비어 있다(모자를 젖힌 뒤의 "백"은 `char_baek_neutral`이 있다).
- 넣는 곳: `ArtInbox/illustrations/char_wanderer_neutral.png`
- 잇는 곳: `Resources/Art/Portraits/char_wanderer_neutral` + `art_map.json` `Speakers`에 `{ "Name": "유랑자", "Portrait": "char_wanderer" }`
- 모델 `seedream_v5_pro` 또는 `nano_banana_2_1`, 참고 = `char_wanderer_sheet.png`. `[F-ILLU]` **첫 문장만** + 아래 + `[안전]`

```
Bust-up portrait of the same wanderer from the reference sheet, visual novel sprite: an adult of unknown age in a worn faded hiking jacket, the wide-brim hat pulled low so the eyes stay hidden in shadow, only a stubbled jaw and a faint calm half-smile visible, the strap of a large backpack over one shoulder, soft rim light, plain light grey background.
```

### 1.2 다시 만들 것 (4장, 5차 문서 7절)

5차 때 받았지만 13장 위반·설정 오류로 **연결하지 않았거나 교체를 기다리는** 그림이다. 고친 프롬프트는 [HIGGSFIELD_ASSETS_5.md](HIGGSFIELD_ASSETS_5.md) 7절 그대로이고, 복사하기 쉽게 아래에 옮겼다. 같은 파일 이름으로 `ArtInbox/illustrations/`에 덮어쓴다.

| 파일 | 이벤트 (잇는 곳: `art_map.json` `Events`) | 프롬프트 (`[F-ILLU]` 뒤, 끝에 `[안전]`) |
|---|---|---|
| `evt_talk_12.png` | `EVT_TALK_12` (지금 그림 없음) | `Night, a nurse office: a middle-aged male teacher with glasses lies alone on a narrow cot under a blanket, eyes half closed; a 16-year-old girl in a school uniform and cardigan sits upright on a chair beside the cot, holding his hand resting on the blanket edge. A damp towel folded on a basin, a closed diary on the bedside table, a small lamp. Respectful distance, quiet, tender, like a student visiting a sick teacher.` |
| `evt_a4_sageori_after.png` | `EVT_A4_SAGEORI_AFTER` (지금 그림 없음) | `Winter morning after a long night, trampled snow along a school wall, a tipped-over can of bright yellow paint with a short yellow streak on the snow, a yellow spray-painted X on the wall, a 12-year-old boy in a huge dark raincoat with rolled sleeves standing alone at the open school gate, breath fogging. No red, nothing that looks like blood.` |
| `evt_sv_join.png` | `EVT_NPC_SURVIVOR_A_02` (교체) | 5차 3장 프롬프트 + `The teacher's left trouser leg is folded and pinned below the knee, no left foot, he stands on his right leg with two crutches.` |
| `evt_a4_cured.png` | `EVT_A4_CURED` (교체) | 5차 4장 프롬프트 + `The teacher's left trouser leg ends in a simple homemade prosthetic below the knee, a crutch leaning on the cot.` |

### 1.3 동료 이벤트 컷 (21장, 16:9)

대화가 있는데 그림이 없는 동료 장면이다. 모두 `[F-ILLU]` 뒤에 아래 문장과 계절 문구, 끝에 `[안전]`. 참고 = 나오는 인물 시트.
- 넣는 곳: `ArtInbox/illustrations/`
- 잇는 곳: `Resources/Art/Story/Events/{파일}` + `art_map.json` `Events`에 `{ "Event": "{이벤트}", "Image": "Events/{파일}" }`

| 파일 | 이벤트 | 계절 | 프롬프트 |
|---|---|---|---|
| `evt_dh_door.png` | `EVT_DH_02_DOOR` 도현이 문을 두드림 | `[봄]` | `Night inside a dark school storage room with a heavy steel door opened a hand's width: through the gap, a thin frightened 15-year-old boy in an oversized dark green tracksuit stands in the dim corridor clutching an empty water bottle. A 16-year-old girl kneels at the gap, her hand on the door edge, deciding. Behind her a middle-aged male teacher with glasses sits on gym mats with a blanket over his lap, his left trouser leg folded and pinned below the knee, watching her quietly.` |
| `evt_dh_nightmare.png` | `EVT_DH_03_NIGHTMARE` 도현의 악몽 | `[봄]` | `Before dawn in a storage room shelter, a 15-year-old boy in a dark green tracksuit sits up on a sleeping bag hugging his knees, hair damp with sweat, and a 16-year-old girl sits beside him on the floor listening, a small camping lantern turned low between them, blue light at the high window.` |
| `evt_dh_helper.png` | `EVT_DH_04_HELPER` 망 보는 공책 | `[봄]` | `Close-up on a page of a school notebook held out by a boy's hands: a pencil drawing of a schoolyard seen from above, dozens of small circles with tiny arrows and time marks beside them (unreadable scribbles), a 16-year-old girl's hands taking the notebook, window light.` |
| `evt_dh_home.png` | `EVT_DH_05_HOME` 빨간 지붕 | `[여름]` | `Night, a 15-year-old boy in a dark green tracksuit sitting on a classroom windowsill, pointing out toward a dark residential neighborhood where one red tiled roof catches faint moonlight; a 16-year-old girl stands beside him looking where he points. Seen from behind, quiet longing.` `[F-KOREA]` |
| `evt_dh_home_visit.png` | `EVT_DH_06_HOME_VISIT` 냉장고 쪽지 | `[여름]` | `Inside a tidy, abandoned Korean apartment kitchen, afternoon light through blinds: a 15-year-old boy in a dark green tracksuit stands before a fridge reading a handwritten note held by a magnet (unreadable), biting his lip, and a 16-year-old girl's hand rests on his shoulder. Family magnets and a child's drawing on the fridge.` |
| `evt_dh_shelter.png` | `EVT_DH_07_SHELTER` 파란 리본 소식 | `[가을]` | `Night in a school broadcasting room, a 15-year-old boy in a dark green tracksuit frozen in front of a glowing handheld radio, eyes wet, a short blue ribbon wrapped around his fingers; a 16-year-old girl sits next to him, the dial light on both faces.` |
| `evt_dh_fever.png` | `EVT_DH_NURSE_FEVER` 도현의 열 | `[여름]` | `Night in a school nurse office lit by one lamp, a feverish 15-year-old boy asleep on a cot under a thin blanket, a school nurse in a white gown over a padded vest wringing a wet towel over a basin, a 16-year-old girl sitting awake on a stool beside the cot, a clock on the wall near four.` |
| `evt_nurse_that_day.png` | `EVT_NURSE_03_THAT_DAY` 그날의 보건실 | `[여름]` | `Night in a school nurse office, a school nurse in her 40s with a short bob sits at her desk under a lamp with a pen set down on a handwritten medicine inventory list (unreadable), a thick stack of old early-leave slips with stamp marks beside it, a 16-year-old girl sitting across the desk listening. Quiet confession.` |
| `evt_nurse_ration.png` | `EVT_NURSE_04_RATION` 누가 먼저 쓰나 | `[여름]` | `A nearly empty white medicine cabinet with its door open: only two rolls of bandage and a few blister packs of painkillers on the shelves. In front of it a school nurse, a middle-aged male teacher with glasses and a cane, and a 16-year-old girl stand in a small circle, the two adults looking at the girl.` (선생님 다리: 2막 문구) |
| `evt_nurse_leg.png` | `EVT_NURSE_05_LEG` 의족 설계 | `[여름]` | `Night, a school nurse kneels with a tailor's measuring tape beside a seated middle-aged male teacher with glasses, his left trouser leg folded and pinned below the knee, a homemade mop-handle crutch leaning on the desk; on the desk a sheet of paper with a pencil sketch of a simple prosthetic leg made of plastic pipe, straps and a cushion (labels unreadable), a girl leaning in to look.` |
| `evt_nurse_lesson.png` | `EVT_A2_NURSE_LESSON` 붕대 수업 | `[여름]` | `Bright afternoon in a nurse office, a school nurse holds out her own forearm while a 16-year-old girl wraps an elastic bandage around it, a few practice bandages unrolled on the desk, the nurse watching the girl's hands closely with a small approving look.` |
| `evt_by_confess.png` | `EVT_A2_BYSTANDER_TALK` 다 봤어 | `[여름]` | `Night in a dim school cafeteria, two 16-year-old girls standing apart by a steel counter with a water jug: one with long hair tied low looks down, crying, hands clenched at her sides; the other in a navy uniform holds a cup of water and listens without moving. Moonlight through tall windows, rows of empty tables.` |
| `evt_by_choice.png` | `EVT_BY_02_CHOICE` 이번엔 네 편 | `[가을]` | `A classroom corner in the evening, a slim girl with long hair tied low sets a steaming cup of warm water on a desk in front of a girl in a navy uniform, then stays standing beside her instead of leaving, eyes lowered but not turning away. Soft lamp light.` |
| `evt_by_letter.png` | `EVT_BY_03_LETTER` 부치지 않은 편지 | `[여름]` | `Night at the window seat at the end of a school corridor, a slim girl with long hair tied low sits hugging a closed notebook; on the floor between her and another girl lies a single folded letter that has slipped out, handwriting on it unreadable. Moonlight, long corridor behind.` |
| `evt_by_map.png` | `EVT_BY_04_MAP` 손으로 그린 지도 | `[여름]` | `A large sheet of drawing paper spread on pushed-together school desks, covered with a detailed pencil map of a school and the surrounding alleys (no readable words), two girls leaning over it, one holding a red pen marking small circles, the other with long hair tied low pointing at a corner.` |
| `evt_hr_first_night.png` | `EVT_A3_RINGLEADER_NIGHT` 첫날 밤 | `[가을]` | `Night in a classroom shelter, people sitting in a loose circle around a camping stove eating, and a thin girl with grown-out dyed brown hair in a borrowed adult parka sitting alone at the far edge with half a bowl, a wide empty gap of floor between her and the others. Everyone carefully not looking at her.` |
| `evt_hr_chores.png` | `EVT_HR_02_CHORES` 시키지 않은 일 | `[가을]` | `Early morning in a school corridor, a thin girl with grown-out dyed brown hair carries two heavy water buckets, her palms red, sleeves rolled up; at the far end of the corridor other people glance at her and step aside. Cold light, wet mop marks on the floor.` |
| `evt_hr_storage.png` | `EVT_HR_03_STORAGE` 체육창고 | `[가을]` | `Night, the door of a school sports storage room standing wide open, inside a thin girl with grown-out dyed brown hair sits on a stack of gym mats hugging her knees in the dark; another girl in a navy uniform stands at the doorway, holding the door open with one hand. A strip of corridor light on the floor.` |
| `evt_hr_secret.png` | `EVT_HR_04_SECRET` 냉동고 이야기 | `[가을]` | `Afternoon by a classroom window, a thin girl with grown-out dyed brown hair speaks for the first time, leaning on a desk, not looking up; a girl in a navy uniform listens with a handheld radio on the desk between them. Cool light, a tense careful mood.` |
| `evt_wanderer_debt.png` | `EVT_A3_WANDERER_2` 빚 갚으러 | `[가을]` | `A school front gate in autumn morning, an adult wanderer in a faded hiking jacket and wide-brim hat hiding the eyes holds out a cardboard box with a first-aid cross through the bars; behind the gate a girl in a navy uniform hesitates, a new padlock on the gate. Muddy hiking boot prints on the ground.` `[F-KOREA]` |
| `evt_wanderer_last.png` | `EVT_A3_WANDERER_3` 마지막으로 들름 | `[가을]` | `Dusk at a school wall, a sun-weathered man in his 50s with a wide-brim hat pushed back and a much lighter backpack points along the north wall explaining where to reinforce it, a girl beside him following his finger; fallen ginkgo leaves, long shadows.` |

### 1.4 이야기 이벤트 컷 (19장, 16:9)

같은 방식(`[F-ILLU]` 뒤, 계절, `[안전]`), 같은 넣는 곳·잇는 곳.

| 파일 | 이벤트 | 계절 | 프롬프트 |
|---|---|---|---|
| `evt_a1_tutorial_done.png` | `EVT_A1_TUTORIAL_DONE` 창고 문이 닫히고 | `[봄]` | `Inside a school storage room just after the steel door shut behind them, a girl sitting on the floor catching her breath, a middle-aged male teacher with glasses on gym mats holding out a flashlight to her, an open cardboard box packed with cup noodles and water bottles between them, his left trouser leg folded and pinned below the knee.` |
| `evt_a1_crutch_plan.png` | `EVT_A1_CRUTCH_PLAN` 목발 약속 | `[봄]` | `Night in a storage room shelter, a middle-aged male teacher with glasses sits against the wall after failing to stand, one hand on the wall, his left trouser leg folded and pinned below the knee; a girl kneels in front of him holding up a wooden mop handle and a roll of tape, a small promise in her eyes. One lantern.` |
| `evt_a1_power_plan.png` | `EVT_A1_POWER_PLAN` 비상 발전기 | `[봄]` | `A girl's hand holding a dead smartphone with a black screen, and behind it, slightly out of focus, a dusty portable emergency generator in the corner of a school broadcasting room, coiled extension cords, a teacher's hand pointing at it.` |
| `evt_a1_keys.png` | `EVT_A1_SECRET_HINT` 교무실 열쇠 | `[봄]` | `Night, close-up of a teacher's bandaged hands turning a heavy ring of old school keys by candlelight, the candle burned short, a pair of thin glasses folded on the desk.` |
| `evt_a1_raid_warning.png` | `EVT_A1_RAID_WARNING` 긁는 소리 | `[봄]` | `Morning, a classroom door barricaded with desks that have been pushed back about a hand's width overnight, fresh scratch marks on the door frame (no stains), a boy wrapped in a blanket up to his eyes watching from the corner, pale light.` |
| `evt_a1_eve.png` | `EVT_A1_EVE` 큰 습격 전날 | `[봄]` | `Night, a girl and a teacher on a homemade mop-handle crutch side by side at a dark classroom window, looking down at a dark schoolyard where faint silhouettes stand far away by the gate; the teacher's other hand resting on the window frame. Calm before the storm.` |
| `evt_a2_opening.png` | `EVT_A2_OPENING` 여름은 넓히는 계절 | `[여름]` | `A muggy summer morning in a storage room shelter, a middle-aged male teacher with glasses draws a floor plan of the school on a chalkboard, a broken piece of chalk in his hand, rooms circled (no readable words), a girl and a boy watching, cicadas outside the high window.` |
| `evt_a2_plan_next.png` | `EVT_A2_PLAN_NEXT` 체육관은 다음에 | `[여름]` | `Night, a school floor plan drawn on paper under a flashlight, three rooms circled in pencil; one small room at the edge (a sports storage room) left blank, the teacher's pen stopped just above it.` |
| `evt_a2_subway_tape.png` | `EVT_A2_BROADCAST` 지하철 안내 방송 | `[여름]` | `Back in a classroom at night, a small old cassette player on a desk with a tape labeled only by a blank sticker, a girl's finger on the play button, a teacher with glasses leaning in to listen, a subway line map drawn on paper beside it with one line traced toward a water plant symbol.` |
| `evt_a2_finale.png` | `EVT_A2_FINALE` 태풍 뒤의 밤 | `[여름]` | `Night after a typhoon, a school wall partly collapsed with muddy debris piled against it, dark distant silhouettes gathering beyond the gap; inside, a boy at the window with a flashlight, a nurse laying out bandages in a neat row on a desk, a girl tightening a rope on a desk barricade. Tense, no close-up violence.` |
| `evt_a3_flu.png` | `EVT_A3_FLU` 독감 | `[가을]` | `A classroom shelter in autumn, two people sick with flu under blankets on mats, a girl in the foreground with a damp towel on her forehead, flushed and half asleep, a school nurse checking a thermometer, tissues and a water bottle, grey light.` |
| `evt_a3_diagnosis.png` | `EVT_A3_NURSE_DIAGNOSIS` 관찰 수첩 | `[가을]` | `A school nurse office desk, an observation notebook open with months of handwritten columns of dates and temperatures (unreadable), a simple hand-drawn line graph that wavers toward the end, a nurse's finger on the graph, a girl's hand holding an old health check-up record card.` |
| `evt_a3_dj_research.png` | `EVT_A3_DJ_RESEARCH` DJ의 기억 | `[가을]` | `Night, a handheld radio glowing on a classroom desk, everyone around it frozen and listening; in the background a middle-aged male teacher with glasses lies under a blanket on a mat, watching the radio with an oddly calm face.` |
| `evt_a3_finale.png` | `EVT_A3_FINALE` 첫 한파의 밤 | `[겨울]` | `First cold-snap night, frost on classroom windows, a middle-aged male teacher with glasses lying on mats wrapped in a blanket, shining a flashlight toward the barricaded door, a handheld radio by his pillow; a girl and others brace the door, breath fogging. Tense, no close-up violence.` |
| `evt_a3_armory.png` | `EVT_A3_ARMORY` 파출소 무기고 | `[가을]` | `A small police substation storeroom, an empty gun locker with its door hanging open, and on the wall pegs a black riot vest (no lettering) and a collapsible baton; a girl holding a crowbar looks at the vest, a pale rectangle where a name tag was removed.` |
| `evt_a3_mart_news.png` | `EVT_A3_MART_UNLOCK` 더불어 마트 소식 | `[가을]` | `Night, a handheld radio on a classroom desk beside a hand-drawn town map, a red circle drawn around a large supermarket with a warehouse, a teacher's hand holding a pencil above the map, a girl listening with her chin on her arms.` |
| `evt_a3_mart_radio.png` | `EVT_A3_MART_RADIO` 정 반장의 경고 | `[가을]` | `Late night, a girl pressing a handheld radio to her ear in a dark corridor, and in the background two people sliding an extra wooden bar across a door, cans hanging on a string.` |
| `evt_a3_sageori_raise.png` | `EVT_A3_SAGEORI_RAISE` 강철을 처음 봄 | `[가을]` | `A school gate in late autumn, three men in dark raincoats outside the bars; the one in the middle has taken off his baseball cap, a heavyset man around forty with a scar across one eyebrow, calm and unreadable, holding his cap; a girl inside the gate holds a box of canned food.` `[F-KOREA]` |
| `evt_a3_sageori_note.png` | `EVT_A3_SAGEORI_WARNING` 교문의 종이 | `[가을]` | `Close-up of a sheet of paper nailed to the inside of a school gate post: a circle with an X drawn in yellow paint and one short handwritten line (unreadable), frost at the edges, footprints in the dirt below.` (빨간 페인트는 쓰지 않는다: 5차 7절) |

### 1.5 달력·시스템·날씨 컷 (11장, 16:9)

같은 방식. 날씨 3장은 매번 같은 그림이 나오므로 인물 없이 사물만 둔다.

| 파일 | 이벤트 | 계절 | 프롬프트 |
|---|---|---|---|
| `evt_cal_final_exam.png` | `EVT_CAL_FINAL_EXAM` 기말고사 | `[여름]` | `A classroom front door with a hand-written paper sign taped on it (unreadable), inside three desks spaced apart, a girl, a boy in a green tracksuit and a nurse-made test sheet on each desk, a teacher with glasses sitting at the front with a stopwatch, trying to look strict.` |
| `evt_cal_vacation.png` | `EVT_CAL_VACATION` 방학식 | `[여름]` | `A summer afternoon classroom, a teacher with glasses standing at the front giving a short speech, a boy in a green tracksuit lying back on a chair grinning, a diary on each desk, bright cicada light through the windows.` |
| `evt_cal_second_term.png` | `EVT_CAL_SECOND_TERM` 개학 출석 | `[가을]` | `Close-up of a real class attendance book opened on a teacher's desk, a red pen drawing a neat circle next to one name line (unreadable), early autumn light, an empty corridor visible through the open door.` |
| `evt_cal_chobok.png` | `EVT_CAL_CHOBOK` 초복 | `[여름]` | `A dented pot of milky rice soup made from one can of chicken breast on a camping stove, steam, several mismatched bowls and spoons, sweaty hands reaching in, summer evening classroom.` |
| `evt_cal_suneung.png` | `EVT_CAL_SUNEUNG` 수능날 | `[가을]` | `An empty third-year classroom, on every desk a clear plastic file holding an exam admission ticket with a small photo (faces blurred, text unreadable), a girl straightening one of them; on the last desk a small gold star sticker beside the photo. Chalk numbers on the corner of the blackboard (unreadable).` |
| `evt_cal_hangeul.png` | `EVT_CAL_HANGEUL` 한글날 | `[가을]` | `A classroom blackboard with a large date written in the middle and a line of old Korean script characters in chalk (stylized, not legible), a teacher with glasses beaming with chalk dust on his fingers, a girl at the corner of the board writing one short private line, her body hiding it.` |
| `evt_cal_dongji.png` | `EVT_CAL_DONGJI` 동지 | `[겨울]` | `The longest night of the year, a pot of sweet red bean porridge on a small stove in a cold classroom, rice cake pieces in it, a girl feeding a spoonful to a pale teacher wrapped in blankets, others laughing at how sweet it is, the window already dark.` |
| `evt_sys_first_science.png` | `EVT_SYS_FIRST_SCIENCE` 첫 과학 수업 | 막 무관 | `A school science room, a textbook open to a diagram of boiling water, a middle-aged male teacher with glasses reading it aloud like a poem, a girl beside him with a notebook, a small pot of water steaming on a camping stove.` |
| `evt_wx_heat_spoil.png` | `EVT_WX_HEAT_SPOIL` 더위에 상함 | `[여름]` | `Heat haze in a storage room, an opened food container with spoiled side dishes, flies circling, a jar of salt and a few salted vegetables laid out on newspaper, harsh midday light, no people.` |
| `evt_wx_cold_illness.png` | `EVT_WX_COLD_ILLNESS` 한파 오한 | `[겨울]` | `Night, a girl curled under two blankets on a mat, a cup of water and a blister pack of cold medicine beside her pillow, frost on the window, a lantern turned low.` |
| `evt_wx_rain_leak.png` | `EVT_WX_RAIN_LEAK` 천장에서 샘 | `[여름]` | `Rainy night in a classroom, water dripping from a ceiling corner into a plastic bucket, a damp stain on the ceiling tiles, a mattress pulled aside, a rag on the floor, no people.` |

- **그림 없이 두는 것**: 무작위 사건(`EVT_RND_*`), 선생님 밤 대화(`EVT_TALK_02~11`), 막 정리(`EVT_A{n}_REVIEW`)는 자주 나오거나 글이 중심이라 새로 그리지 않는다. 원하면 거처 배경(`bg_classroom_night` 등)을 잇는다.
- `EVT_RND_A2_CAT`(급식실 고양이)은 2.3 고양이 결정에 따라 `evt_cat_arrive`를 잇는다.

### 1.6 역할·외출 아이콘 (9장, 1:1 투명)

동료 역할 6개(경비·정찰·간호·살림·공부·위로)와 동료 탐험(P3)은 이미 돌아가는데, 인물 서랍과 동료 단추에는 **글자만** 나온다. 아이콘이 오면 작은 코드 연결(`StoryArt.Icon` 키 조회)을 함께 한다.
- 모델 `recraft_v4_1`(vector) 또는 `gpt_image_2_6`. [HIGGSFIELD_ASSETS_2.md](HIGGSFIELD_ASSETS_2.md) 6.3 공통 문구 뒤에 그릴 것을 붙인다.
- 넣는 곳: `ArtInbox/ui/`
- 잇는 곳: `Resources/Art/UI/{파일}` + `art_map.json` `Icons`에 키

공통 (2차 6.3과 같음):
```
Simple flat game icon, single object, thick clean shapes, muted warm palette with one accent color, readable at 64 pixels, transparent background, no text.
```

| 파일 | 키 (`Icons`) | 그릴 것 (공통 문구 뒤) |
|---|---|---|
| `icon_role_guard.png` | `role.GUARD` (경비) | `a stack of school desks tied with rope as a barricade, a small flashlight on top` |
| `icon_role_scout.png` | `role.SCOUT` (정찰) | `a pair of small binoculars with a rooftop railing behind` |
| `icon_role_medic.png` | `role.MEDIC` (간호) | `a white first-aid tin with a plain green cross and a rolled bandage` |
| `icon_role_house.png` | `role.HOUSE` (살림) | `a dented cooking pot with a ladle and a wisp of steam` |
| `icon_role_study.png` | `role.STUDY` (공부) | `an open textbook with a pencil lying across it` |
| `icon_role_comfort.png` | `role.COMFORT` (위로) | `a steaming mug held in two cupped hands, a small musical note above` |
| `icon_status_away.png` | `status.away` (외출 중) | `a single empty slot on a school shoe rack with footprints leading out an open door` |
| `icon_status_minor_wound.png` | `status.minor_wound` (가벼운 상처) | `a single beige adhesive bandage plaster, slightly crossed` |
| `icon_stay_home.png` | `ctl.stay_home` (이번 주는 남아 줘) | `a hand gently holding the strap of a backpack` |

- 부상은 이미 있는 `icon_status_injured`(붕대)를 쓴다. 가벼운 상처는 그보다 작고 가벼워 보여야 한다.

---

## 2. P2 — 곧 만들 것 (기획에서 정한 기능)

### 2.1 동료 개인 장면 사다리 (18장) + 특기 아이콘 (6장)

[PROPOSAL_CD_DEPTH_EXPANSION.md](PROPOSAL_CD_DEPTH_EXPANSION.md) C.2 ②: 동료마다 호감 **20·50·80**에서 개인 장면이 하나씩 열리고, 마지막 장면을 보면 **특기**가 생긴다.

**장면 설계 원칙**
- 20 = 서로 익숙해지는 순간, 50 = 그 사람이 마음을 여는 순간, 80 = 특기와 이어지는 약속·선물.
- 동료가 합류하는 막이 판마다 달라 **계절이 드러나지 않게** 실내·밤 장면을 기본으로 했다. 계절이 보이는 장면은 `[계절]` 자리를 비워 두었다. 받은 뒤 쓰는 판에 맞춰 바꾸지 않아도 되게 하려는 것이다.
- 13장: 사제·동료 사이는 존중과 우정. 하린은 낭만화하지 않고 **용서를 강요하지 않는다**(사과는 행동으로만, 받을지는 주인공이 정한다). 선배는 말을 하지 않는다(입은 다문 채, 허밍은 가능).

- 이벤트 ID는 구현 때 정한다. 안: `EVT_BOND_{NURSE|DOHYEON|BYSTANDER|RINGLEADER|SENIOR|WANDERER}_{1|2|3}`.
- 넣는 곳: `ArtInbox/illustrations/evt_bond_{동료}_{n}.png`
- 잇는 곳: `Resources/Art/Story/Events/evt_bond_*` + `art_map.json` `Events`
- 모두 `[F-ILLU]` 뒤에 아래 문장, 끝에 `[안전]`. 참고 = 그 동료 시트 + `char_seojin_sheet.png`.

| 파일 | 동료 · 호감 | 장면 | 프롬프트 |
|---|---|---|---|
| `evt_bond_nurse_1.png` | 박 선생님 · 20 | 아침 회진: 맥 짚는 법을 가르친다 | `Morning rounds in a school nurse office, a school nurse in her 40s with a short bob and a white gown over a padded vest holds a girl's fingers against her own wrist to teach her how to take a pulse, a clipboard with a hand-ruled temperature list (unreadable) and a thermometer on the desk, soft window light.` |
| `evt_bond_nurse_2.png` | 박 선생님 · 50 | 붕대를 삶아 다시 쓰는 밤, 걱정을 털어놓는다 | `Night in a nurse office, washed bandages hanging to dry on a string line across the room, a pot of boiling water on a camping stove, the nurse and a 16-year-old girl sitting side by side rolling dried bandages into neat rolls, the nurse talking with her eyes on her hands, lamp light.` |
| `evt_bond_nurse_3.png` | 박 선생님 · 80 | 딸 이야기, 펜라이트를 건넨다 → 특기 | `Late evening, the nurse and a 16-year-old girl sitting together on the edge of a nurse office cot; the nurse holds a small worn photo from her wallet (the person in it turned away and blurred), and with her other hand presses an old medical penlight into the girl's palm. Warm lamp light, gentle smile.` |
| `evt_bond_dohyeon_1.png` | 도현 · 20 | 옥상 망 보기, 자기 표시법을 가르친다 | `School rooftop in daylight, a 15-year-old boy in an oversized dark green tracksuit crouches behind the railing with small binoculars, a notebook of tiny circles and arrows on his knee, explaining it to a girl crouching beside him; far below, a few distant silhouettes in a quiet street. [계절]` |
| `evt_bond_dohyeon_2.png` | 도현 · 50 | 교문에 파란 리본을 묶는다 | `Early evening at a school front gate, a 15-year-old boy in a dark green tracksuit ties a short blue ribbon to the gate bars, a girl holding the other end of the knot for him; a few older blue ribbons already flutter on the bars. [계절]` `[F-KOREA]` |
| `evt_bond_dohyeon_3.png` | 도현 · 80 | 밤 경계: "이번엔 내가 지킬게" → 특기 | `Night, a barricaded classroom door, a 15-year-old boy in a dark green tracksuit wrapped in a blanket sits wide awake on a chair in front of it, a wooden bat resting across his knees and the end of a can-alarm string tied to his wrist; a girl crouches beside him handing him a warm cup, both smiling a little. Lantern light.` |
| `evt_bond_bystander_1.png` | 송예지 · 20 | 도서실에서 몰래 그리던 그림 | `A quiet school library, a slim girl with long hair tied low sits on the floor between shelves sketching in a small notebook; over her shoulder another girl glimpses the page: a soft pencil sketch of people sleeping in a row under blankets, faces not drawn. Dust in sunbeams.` |
| `evt_bond_bystander_2.png` | 송예지 · 50 | 주인공의 초상을 그린다 | `A classroom window seat in the afternoon, a girl in a navy uniform sits still as a model, hands in her lap, while a slim girl with long hair tied low sits across from her drawing in a large sketchbook, the page showing only loose pencil outlines of shoulders and hair. Calm, a little shy.` |
| `evt_bond_bystander_3.png` | 송예지 · 80 | 맞춘 키링, 말없이 곁에 → 특기 | `Night in a classroom shelter, two girls sitting back to back on the floor, one writing in a diary, the other with long hair tied low resting her head back with eyes closed; on the wall hooks behind them hang two school bags side by side with the same small handmade felt keyring. One candle.` |
| `evt_bond_ringleader_1.png` | 최하린 · 20 | 새벽, 염색한 끝을 스스로 자른다 | `Dawn in a school restroom, a thin girl with shoulder-length dyed brown hair grown out at the roots stands at a cracked mirror cutting off the dyed ends with plain scissors, brown strands in the sink; another girl in a navy uniform pauses at the doorway, keeping her distance. Grey light. Not dramatic, quiet.` |
| `evt_bond_ringleader_2.png` | 최하린 · 50 | 옛 동네 뒷골목을 안내한다 | `A narrow back alley between red-brick villas near a large crossroads, a thin girl in a borrowed adult parka walking ahead and pointing at a small chalk mark on a wall (abstract, no letters) and a gap in a fence, a girl in a navy uniform following a step behind, both alert. [계절]` `[F-KOREA]` |
| `evt_bond_ringleader_3.png` | 최하린 · 80 | 옛날에 빼앗은 머리핀을 내민다 (받을지는 주인공) → 특기 | `School rooftop at dusk, two girls sitting on an old bench an arm's length apart, safely away from the railing; the thin girl with grown-out dyed brown hair holds out a small cheap hair clip on her open palm without looking at the other girl, who looks at the clip and has not taken it yet. Long shadows, still, uncertain, no forced reconciliation. [계절]` |
| `evt_bond_senior_1.png` | 선배 · 20 | 음악실에서 기타 줄을 간다 | `A dusty school music room at night, a tall quiet 17-year-old male student with a long pale healed scar from cheek to neck sits on a stool restringing a battered acoustic guitar, a new coiled pack of strings on his knee; a girl holds a flashlight for him, the beam on the guitar neck.` |
| `evt_bond_senior_2.png` | 선배 · 50 | 체육관에서 문 버티는 자세를 말없이 보여 준다 | `A school gymnasium in afternoon light, the tall scarred male student silently demonstrates how to brace a door with a shoulder and planted feet, using a mop handle as a bar; a girl copies the stance beside him, both serious. Basketball hoops, gym mats against the wall.` |
| `evt_bond_senior_3.png` | 선배 · 80 | 보강한 현관에서 처음으로 기타를 친다 → 특기 | `Evening at a school entrance reinforced with planks and metal sheets, the tall scarred male student sits on a step of the barricade playing an acoustic guitar softly with his lips closed, humming; the others sit in the corridor listening, a girl closest to him. An old CD case lies beside him. Warm lantern light, the first time he has played.` |
| `evt_bond_wanderer_1.png` | 유랑자 · 20 | 주인공의 지도를 연필로 고쳐 준다 | `Night at a classroom desk, an adult wanderer in a faded hiking jacket with a wide-brim hat hiding the eyes leans over a girl's hand-drawn town map, adding trails with a stubby pencil; a few hard candies in clear wrappers scattered on the map, the girl watching closely.` |
| `evt_bond_wanderer_2.png` | 유랑자 · 50 | 모자를 젖히고 별로 길 찾는 법을 알려 준다 | `School rooftop on a clear night, a sun-weathered man in his 50s with his wide-brim hat pushed back, face visible and calm, points up at the stars to show the girl beside him how to find north; a completely dark city below, an enormous starry sky.` |
| `evt_bond_wanderer_3.png` | 유랑자 · 80 | 기억으로 지하 통로 단면도를 그려 주고 나침반을 건넨다 → 특기 | `Lamp light on a classroom floor, a large sheet of paper with a hand-drawn cutaway diagram of underground service corridors, stairs and an emergency exit arrow (no readable words); the weathered former mountain guide presses a small old brass compass into a girl's hand.` |

**특기 아이콘 6장** (1:1 투명, 1.6과 같은 공통 문구)
- 잇는 곳: `Resources/Art/UI/{파일}` + `art_map.json` `Icons` 키 `special.{NPC_ID}` (인물 서랍 카드·결산 한 줄)

| 파일 | 키 | 특기 | 그릴 것 |
|---|---|---|---|
| `icon_special_nurse.png` | `special.NPC_NURSE` | 치료에 쓰는 의약품 −1 (2개 이상 드는 치료) | `a small medical penlight lying across a rolled bandage` |
| `icon_special_dohyeon.png` | `special.NPC_DOHYEON` | 밤 습격 피해 −20% | `a wooden bat leaning on a barricaded door under a crescent moon` |
| `icon_special_bystander.png` | `special.NPC_BYSTANDER` | 결산 스트레스 −3 | `two small matching felt keyrings side by side` |
| `icon_special_ringleader.png` | `special.NPC_RINGLEADER` | 사거리 파에 바치는 몫 −2 | `a lighter bundle of a few cans handed over at a crossroads sign, abstract` |
| `icon_special_senior.png` | `special.NPC_SURVIVOR_A` | 거점 방어도 +10 | `an acoustic guitar leaning against a plank barricade` |
| `icon_special_wanderer.png` | `special.NPC_WANDERER` | 깊은 곳 일방 출구 위치 | `a small brass compass on a folded map with an exit arrow` |

### 2.2 윤 선생님 개인 장면 (2장)

C.1: 호감 50·80에서 한 번씩. 사제 간의 존중이다(13장). 그림이 아직 없다(대화 2·5·13 컷과 다르다).
- 이벤트 ID 안: `EVT_BOND_TEACHER_1`, `_2`. 넣는 곳·잇는 곳은 2.1과 같다.
- 선생님 다리는 0.3 다리 문구 가운데 그 막 것을 붙인다. 아래 프롬프트는 책상에 앉은 구도라 어느 막이든 맞게 했다.

| 파일 | 호감 | 장면 | 프롬프트 (`[F-ILLU]` 뒤, 끝에 `[안전]`) |
|---|---|---|---|
| `evt_bond_teacher_1.png` | 50 | 교사가 된 이유: 자기 고등학교 때 공책 | `Night in a classroom, a middle-aged male teacher with glasses sits at a student desk with a crutch leaning beside him, showing a girl an old, faded high school notebook from his own student days, its margins full of red-pen comments from his own teacher (unreadable); he smiles a little embarrassed, she leans in, candlelight.` |
| `evt_bond_teacher_2.png` | 80 | 졸업장을 미리 써 두는 밤 | `Late night in a staff room lit by one candle, a middle-aged male teacher with glasses writes carefully with a brush pen on a sheet of thick cream paper with a printed border (the writing unreadable, the name area blurred), several finished sheets drying beside him; through the half-open door a girl in a cardigan watches quietly, not entering.` |

### 2.3 고양이 동료 (5장) `[결정 필요]`

**먼저 정할 것: 고양이 생김새가 두 가지다.**

| 이미 있는 것 | 생김새 |
|---|---|
| 프롤로그 등굣길 영상(6차), 뱃지 `avatar_cat.png`(7차) | **치즈 태비**(주황·흰색), 한쪽 귀 살짝 찢김, 시큰둥한 눈 |
| 대본 `EVT_RND_A2_CAT`("비쩍 마른 삼색 고양이, 귀 끝이 잘림"), 4막 "옥상 고양이", 좌석 엔딩 `ending_seat_senior`(5차) | **삼색**, 귀 끝 중성화 표시 |

기획(확장 2)은 "프롤로그의 그 고양이가 동료가 된다"이다.

| 안 | 내용 | 다시 할 것 |
|---|---|---|
| ① **치즈 태비로 하나 (추천)** | 영상·뱃지가 이미 치즈다. 대본의 "삼색"을 "치즈"로 고친다(문구 2곳) | 그림 1장: `ending_seat_senior.png`를 치즈 고양이로 다시 |
| ② 삼색으로 하나 | 엔딩 컷이 삼색이다 | 뱃지 1장 + 프롤로그 영상의 고양이 장면(영상은 다시 만들기 비쌈) |
| ③ 두 마리 | 프롤로그 치즈 = 동료, 삼색 '급식' = 옥상 고양이 따로 | 없음. 다만 "옥상 고양이"와 동료 고양이가 헷갈린다 |

아래 프롬프트는 ①(치즈)로 썼다. ②를 고르면 `[CAT]` 문구만 바꾼다.

`[CAT]`
```
a scruffy Korean street cat, short-haired cheese tabby (orange and white), one ear slightly notched, half-closed unimpressed eyes
```

①이면 다시 만들 것 (5차 1.3 프롬프트에서 고양이만 바꿈, `[F-ILLU] [F-KOREA]` 뒤, 끝에 `[봄] [헬기] [안전]` — 5차 문서의 문구):
`ending_seat_senior.png`
```
A school rooftop beside the faded red SOS, the rescue helicopter rising away in the background. A tall silent older male student with a long pale healed scar from his left cheek down his neck stands alone, holding a scruffy cheese tabby cat (orange and white, one ear slightly notched) against his chest with both arms, his chin resting lightly on the cat's head, eyes closed, the faintest smile. The cat is calm and content. Still, tender, lonely but at peace.
```

**고양이 컷 5장** (16:9, `[F-ILLU]` 뒤, 끝에 `[안전]`. 참고 = `avatar_cat.png`, 프롤로그 영상의 고양이 프레임). 13장: 고양이는 다치거나 죽지 않는다.
- 이벤트 ID는 구현 때 정한다. 넣는 곳 `ArtInbox/illustrations/`, 잇는 곳 `Story/Events` + `art_map.json` `Events`.

| 파일 | 쓰임 | 프롬프트 |
|---|---|---|
| `evt_cat_arrive.png` | 1막 후반, 학교 담장 위에 나타남 (`EVT_RND_A2_CAT`도 이 그림) | `On top of a school wall, [CAT] sits and stares down at a 16-year-old girl in a navy uniform who has stopped mid-step, recognizing it; overgrown schoolyard behind her. [계절]` `[F-KOREA]` |
| `evt_cat_feed.png` | 먹이를 나눠 줌 | `Behind a school cafeteria back door, a girl crouching and holding out a can lid with a little tuna on it, [CAT] stretching its neck forward cautiously, a 15-year-old boy in a green tracksuit holding his breath behind her.` |
| `evt_cat_settle.png` | 거처에 눌러앉음 | `Morning in a classroom shelter, [CAT] curled up asleep on a girl's sleeping bag, the girl sitting beside it very still so as not to wake it, a teacher with glasses smiling from his mat.` |
| `evt_cat_warn.png` | 습격 전 털을 세워 경고 | `Night, [CAT] on a classroom windowsill with its fur standing up and back arched, staring hard out at the dark schoolyard; behind it people sitting up from their blankets, one reaching for a flashlight. Tense, nothing visible outside but darkness.` |
| `evt_cat_lap.png` | 우울할 때 무릎에 올라옴 | `Lamp light, a tired girl sitting on the floor against the wall with a diary on her knees, [CAT] stepping onto her lap and settling down on top of the diary, her hand hovering, then resting on its back.` |

- 고양이의 허브 단면도 모습(걷기·잠)은 PixelLab 몫이다.

### 2.4 CD 수집 (A, 24개)

결정(A-1 늘 재생, A-2 판마다 새로 모으고 들은 곡은 타이틀 "음악실"). 8장 배치는 기획서 A.2 표를 따른다.

**지금 없는 것**: 게임에 배경음악 재생기와 배경음악 파일이 하나도 없다(1차 문서 5.4 환경음·5.5 배경음악은 받지 않았다). CD 기능을 만들 때 음악 재생 칸(`sound_map.json`에 `Music` 절)을 함께 만든다. 1차 5.5의 허브 음악도 그때 같이 받으면 좋다.

#### 2.4.1 CD 표지 8장 (1:1, 투명 배경)

진열장·CD 목록·"음악실"에 쓰는 **케이스 앞면 그림**이다. 케이스에 원래 주인의 흔적이 보여야 한다. 글자가 생기면 다시 만든다.
- 모델 `gpt_image_2_6`. 넣는 곳 `ArtInbox/ui/cd/`
- 잇는 곳: `Resources/Art/UI/CD/cd_0n` + `art_map.json` `Icons` 키 `cd.CD_0n` (안)

`[F-CD]`
```
Front view of a single worn CD jewel case lying flat, square, realistic scratched plastic and printed paper insert texture, soft even light, no shadow, centered, transparent background around the case, no readable text, no logo, no brand name, no watermark. The cover insert shows:
```

| 파일 | # · 지역 · 주인 | `[F-CD]` 뒤에 붙임 |
|---|---|---|
| `cd_01.png` | 1 교실동 · 3학년 방송반 선배 "졸업 방송용" | `a soft watercolor of cherry trees in front of a school building, a round blank club sticker in the corner, a folded note tucked under the case clip` |
| `cd_02.png` | 2 편의점 · 야간 알바생 "새벽 2시 플레이리스트" | `a blank white CD-R insert with a hand-drawn crescent moon and a coffee cup doodle in marker, a convenience-store receipt folded inside the case` |
| `cd_03.png` | 3 약국 · 약사의 딸 "엄마 가게에서 틀 것" | `a cozy pastel illustration of a small shop window with plants, a heart sticker and a small pressed flower taped inside the lid` |
| `cd_04.png` | 4 주택가(깊은 곳) · 밴드 대학생 데모 | `a rough black-and-white photocopied collage of a guitar amp and a garage door, punk style, a cracked corner on the case, handwritten scribbles on masking tape (unreadable)` |
| `cd_05.png` | 5 지하철역(깊은 곳) · 회사원 "월요일 버티기" | `a retro 1980s synthwave graphic of a train running toward a neon sunset grid, a subway pass card tucked behind the insert` |
| `cd_06.png` | 6 마트(깊은 곳) · 매장 음악 업무용 | `a plain corporate-looking insert in cheerful primary colors with abstract shopping bag shapes, a rubber office stamp mark in red-orange ink on the corner` |
| `cd_07.png` | 7 파출소 · 젊은 순경 "교대 끝나면" | `a moody blue photo of a city street at night with wet asphalt reflections, a small star sticker on the case spine` |
| `cd_08.png` | 8 정수장(깊은 곳) · 연구원, 아이에게 받은 동요 CD | `a child's crayon drawing of a stick-figure dad and a child holding hands under a big sun, the crayon pressed hard, a small heart drawn in the corner` |

#### 2.4.2 CD 음원 8곡 (`sonilo_music`, 연주곡, 90초, 반복)

모두 **가사 없음**, 원곡이 없는 새 멜로디, 실존 곡·가수를 흉내 내지 않는다. 끝과 처음이 이어지게(`loopable`). 프롬프트 끝에 `instrumental, original melody, no vocals, no lyrics, loopable, clean ending that loops back to the start`를 붙인다.
- 넣는 곳 `ArtInbox/music/`
- 잇는 곳: `Resources/Audio/Music/cd_0n` + `sound_map.json` `Music` 절(새로 만듦)

| 파일 | # · 분위기 (기획 A.2) | 프롬프트 |
|---|---|---|
| `cd_01.wav` | 1 잔잔한 피아노, 교정의 봄 | `gentle solo piano with a soft acoustic guitar joining in the second half, spring morning at a school, nostalgic graduation feeling, warm and bittersweet, 72 bpm` |
| `cd_02.wav` | 2 로파이, 형광등 | `lo-fi hip hop beat, dusty drums, mellow electric piano chords, faint vinyl crackle and a low fluorescent light hum texture, 2 a.m. convenience store, 80 bpm` |
| `cd_03.wav` | 3 따뜻한 어쿠스틱 | `warm acoustic guitar and ukulele with light hand percussion and a soft glockenspiel, a cozy neighborhood pharmacy on a sunny afternoon, 95 bpm` |
| `cd_04.wav` | 4 거친 인디 록 | `raw indie rock instrumental, overdriven guitars, live drums and bass, garage demo recording quality, energetic and a little sloppy, 140 bpm` |
| `cd_05.wav` | 5 신스팝, 달리는 열차 | `1980s-style synthpop instrumental, driving arpeggiated bass like a running train, bright synth lead, hopeful Monday-morning commute, 118 bpm` |
| `cd_06.wav` | 6 밝고 어색한 매장 음악 | `cheerful supermarket background muzak, bouncy electric piano and light brass, slightly cheesy and too happy, easy listening, 110 bpm` |
| `cd_07.wav` | 7 블루스 기타 | `slow late-night blues, clean electric guitar with soft bends, brushed drums and upright bass, tired but kind, 70 bpm` |
| `cd_08.wav` | 8 단순한 오르골, 마지막 곡 | `a simple music box playing a childlike lullaby melody, very sparse, a faint soft pad underneath, tender and fragile, 60 bpm` |

- 받으면 길이를 잘라 이음매를 다듬는다. 탐험 전까지 허브에서 흐르므로 음량은 효과음보다 낮게 맞춘다.

#### 2.4.3 방송반 진열장·플레이어 (허브 그림 3장, 투명)

방송반(`BROADCAST`) 방에 겹친다. 1차 9.8과 같은 방식: `[F-ILLU]`의 `Background:` 문장 + `single object, semi-photorealistic, transparent background, soft shadow` + 아래.
- 넣는 곳 `ArtInbox/ui/hubobj/`
- 잇는 곳: `Resources/Art/UI/HubObj/{파일}` + `hub_section.json` `Rooms[BROADCAST].Built` (CD 수에 따라 꽂힌 케이스를 코드에서 겹친다)

| 파일 | 그릴 것 |
|---|---|
| `hub_obj_cd_shelf.png` | `a small wall-mounted wooden CD display rack with eight empty vertical slots, front view, slightly dusty` |
| `hub_obj_cd_case.png` | `a single CD jewel case standing upright seen exactly from the front edge as a thin spine, plain, to be repeated in a rack` |
| `hub_obj_cd_player.png` | `a portable CD player with the lid open and a pair of wired headphones, a few AA batteries beside it, on a broadcasting room desk corner` |

#### 2.4.4 "음악실" 배경 · 다 모은 장면 (2장, 16:9)

| 파일 | 쓰임 · 잇는 곳 | 프롬프트 (`[F-ILLU]` 뒤, 끝에 `[안전]`) |
|---|---|---|
| `bg_title_music.png` | 타이틀 "음악실" 화면 배경 → `Resources/Art/Story/Backgrounds/bg_title_music` | `A school broadcasting room at dusk with no people, a wooden CD rack on the wall with eight CD cases, a portable CD player and headphones on the mixing desk, warm low sunlight through the blinds, dust in the air, heavily blurred toward the edges and darkened so a list of tracks can sit on top.` |
| `evt_cd_shelf_full.png` | 8장을 다 모은 장면 (이벤트 ID는 구현 때) → `Story/Events` + `Events` | `Night, close-up of a full wooden CD rack in a school broadcasting room, eight different worn CD cases side by side, a girl's fingertip touching the last one as she slides it in, a candle and a portable CD player below, quiet satisfaction.` |

#### 2.4.5 CD 효과음 (3개, `seed_audio`, 끝에 `[F-AUDIO]`)

넣는 곳 `ArtInbox/sfx/`, 잇는 곳 `Resources/Audio/Sfx/{파일}`.

| 파일 | 길이 | 프롬프트 |
|---|---|---|
| `sfx_cd_pickup.wav` | 1s | `a plastic CD jewel case picked up from a drawer, clack of the case opening and closing, no music, no voice` |
| `sfx_cd_insert.wav` | 1.5s | `a CD placed into a portable CD player, lid snapping shut, the disc starting to spin with a soft whir, no music, no voice` |
| `sfx_cd_stop.wav` | 0.8s | `portable CD player stop button click, disc spinning down, no music, no voice` |

### 2.5 탐험의 깊이 (B, 6개)

문·벽·바닥·비상등은 PixelLab 픽셀 아트다(기획 B.5). 힉스필드로는 **처음 만났을 때의 이벤트 컷**과 **소리**만 만든다.

**이벤트 컷 2장** (16:9, `[F-ILLU]` 뒤, 끝에 `[안전]`. 넣는 곳·잇는 곳은 1.3과 같고 이벤트 ID는 구현 때)

| 파일 | 쓰임 | 프롬프트 |
|---|---|---|
| `evt_deep_door.png` | 처음 깊은 문을 발견 | `A dim building corridor, at its end an old rusted steel fire door with yellow-and-black warning tape on the frame and an unlit green emergency sign above it; through the gap under the door a thin line of cold blue light and a faint draft stirring dust; a girl with a flashlight stops in front of it.` |
| `evt_deep_oneway.png` | 처음 일방 출구를 지남 | `Inside a dark concrete service corridor with pipes and cable trays, a heavy emergency exit door with a horizontal push bar, a lit green emergency exit sign with a simple running-figure pictogram and no text above it; a girl's hand pressing the push bar, a narrow slice of the outer building visible as it opens.` |

**소리 4개** (`seed_audio`, 끝에 `[F-AUDIO]`. 넣는 곳 `ArtInbox/sfx/`, 잇는 곳 `Resources/Audio/Sfx/`)

| 파일 | 길이 | 프롬프트 |
|---|---|---|
| `sfx_deep_door_open.wav` | 2.5s | `heavy rusted steel fire door dragging open, deep metallic groan followed by a low hollow wind rushing out, no music, no voice` |
| `sfx_oneway_lock.wav` | 1.5s | `emergency exit push bar clunk, heavy door swinging shut behind, a final metallic latch click that locks, no music, no voice` |
| `sfx_deep_draft.wav` | 2s | `cold draft whistling faintly through a door gap, hollow and distant, no music, no voice` (문 앞 W 살피기) |
| `amb_deep.wav` | 30s, 반복 | `underground concrete service corridor room tone, ventilation hum, slow water drips echoing, occasional distant pipe knock, loopable, no music` |

---

## 3. P3 — 나중 (진행 순서 A → B → 2 → 7 → 5 → 6의 뒤쪽)

### 3.1 방과후 동아리 (확장 7, 8개)

일주일에 한 번 밤 칸에서 동료 한 명과 활동한다. 동료가 바뀌어도 쓰도록 컷은 **누가 누군지 덜 드러나는 구도**로 한다.

**행동 아이콘 4장** (1.6 공통 문구, 잇는 곳 `Resources/Art/UI/` + `Icons` 키 `act.{행동 ID}`, 행동 ID는 구현 때)

| 파일 | 그릴 것 |
|---|---|
| `icon_act_club_boardgame.png` | `a folded board game with a few wooden pieces and a die` |
| `icon_act_club_sing.png` | `a small handheld microphone with two musical notes` |
| `icon_act_club_exercise.png` | `a jump rope coiled around a water bottle` |
| `icon_act_club_studygroup.png` | `two notebooks open side by side with one shared pencil` |

**컷 4장** (16:9, `[F-ILLU]` 뒤, 끝에 `[안전]`)

| 파일 | 프롬프트 |
|---|---|
| `evt_club_boardgame.png` | `Night in a classroom, two people sitting cross-legged on a mat over a homemade paper board game drawn in marker, bottle caps as pieces, a die mid-roll, one laughing with a hand over the mouth, lantern light; faces partly in shadow.` |
| `evt_club_sing.png` | `Night in a school music room, two people singing quietly into an unplugged microphone and a rolled-up notebook, one tapping rhythm on a piano lid, candlelight, seen from a little behind.` |
| `evt_club_exercise.png` | `Evening in a school gymnasium, two people doing a jump rope and push-up routine by lantern light, breathless and grinning, gym mats on the floor.` |
| `evt_club_studygroup.png` | `Night in a library corner, two people sharing one lamp over open textbooks, one pointing at a line with a pencil, the other writing, quiet focus.` |

### 3.2 방송반 송출 (확장 5, 6개)

**컷 3장** (16:9, `[F-ILLU]` 뒤, 끝에 `[안전]`)

| 파일 | 쓰임 | 프롬프트 |
|---|---|---|
| `evt_bc_first.png` | 첫 송출 | `Night in a school broadcasting room, a girl in a navy uniform leaning toward an old microphone on a mixing desk, a portable CD player and the wire to a homemade rooftop antenna, a small red on-air bulb glowing, the others crowded silently at the door.` |
| `evt_bc_reply.png` | 누군가 응답 | `Night seen from a school rooftop, far away across the dark city a single tiny flashlight blinking from the window of an apartment tower, a girl holding a handheld radio to her ear, eyes wide.` `[F-KOREA]` |
| `evt_bc_found.png` | 사거리 파에 들킴 | `Morning, the base of a homemade radio antenna on a school rooftop, a yellow spray-painted X on the concrete beside it and a single cigarette butt, a girl standing over it, uneasy. No red paint.` |

**아이콘 1장 · 소리 2개**

| 파일 | 종류 · 잇는 곳 | 프롬프트 |
|---|---|---|
| `icon_act_broadcast_send.png` | 행동 아이콘 (1.6 공통) → `Icons` `act.{행동 ID}` | `an old desk microphone with three small radio waves` |
| `sfx_mic_on.wav` | 효과음 0.8s → `Resources/Audio/Sfx/` | `old microphone switched on with a soft pop and a brief low feedback hum, no music, no voice` |
| `ui_school_jingle.wav` | 우리 방송 시그널 4s (`sonilo_music`) | `short warm instrumental radio jingle, a school chime motif played on acoustic guitar and glockenspiel, hopeful, no lyrics` |

### 3.3 계절 재해 지도 (확장 6, 6개)

침수된 바닥·눈 쌓인 진입로의 탐험 그림은 PixelLab 몫이다. 여기서는 알리는 컷·아이콘·소리만.

| 파일 | 종류 · 잇는 곳 | 프롬프트 |
|---|---|---|
| `evt_season_flood.png` | 컷 16:9 (장마 첫 침수) → `Story/Events` | `[F-ILLU] A dark basement service corridor flooded ankle-deep with still muddy water, a flashlight beam reflecting off the surface, floating leaves and a plastic bottle, a girl standing on the last dry stair looking down. [여름] [안전]` |
| `evt_season_snow_block.png` | 컷 16:9 (폭설로 막힌 진입로) → `Story/Events` | `[F-ILLU] [F-KOREA] A narrow street entrance to a building completely blocked by a waist-high snowdrift and a buried compact car, a girl in a padded jacket turning to look toward a side alley instead. [겨울] [안전]` |
| `icon_hazard_flood.png` | 아이콘 (출정 준비 화면) → `Icons` `hazard.flood` | 1.6 공통 + `a door half under rippling water` |
| `icon_hazard_snow.png` | 아이콘 → `Icons` `hazard.snow` | 1.6 공통 + `a doorway blocked by a snowdrift` |
| `sfx_step_water.wav` | 효과음 0.6s → `Resources/Audio/Sfx/` | `single footstep wading through ankle-deep water in a concrete corridor, splash and drip, no music, no voice` `[F-AUDIO]` |
| `amb_flooded.wav` | 환경음 30s 반복 → `Resources/Audio/Sfx/` | `flooded underground corridor, slow water lapping against walls, drips echoing, low hum, loopable, no music` `[F-AUDIO]` |

### 3.4 주인공 입상 "든 것" (8장, 참고만)

`standing_map.json`의 `Bags`·`Weapons`·`Tools`가 `stand_item_*` 8장을 찾는데 파일이 없다. 지금 입상은 **한 장짜리(`Mode: Whole`)**라 이 조각을 쓰지 않으므로 비어 있어도 화면은 멀쩡하다. 레이어 방식으로 돌아갈 때만 [HIGGSFIELD_ASSETS_2.md](HIGGSFIELD_ASSETS_2.md) 1.7 프롬프트 그대로 만든다.
- 파일: `stand_item_bag_school`, `stand_item_bag_hiking`, `stand_item_bag_tote`, `stand_item_mop`, `stand_item_bat`, `stand_item_pipe`, `stand_item_baton`, `stand_item_flashlight`
- 넣는 곳 `ArtInbox/standing/`, 잇는 곳 `Resources/Art/Standing/stand_item_*`

---

## 4. 체크리스트

- [ ] **P1** 1.1 유랑자 초상 1 → 1.2 다시 만들 것 4 → 1.3 동료 컷 21 → 1.4 이야기 컷 19 → 1.5 달력·시스템·날씨 11 → 1.6 아이콘 9
- [ ] **P2** 2.1 개인 장면 18 + 특기 아이콘 6 → 2.2 선생님 2 → 2.3 고양이 결정 → 컷 5 (+1) → 2.4 CD 24 → 2.5 깊이 6
- [ ] **P3** 3.1 동아리 8 → 3.2 송출 6 → 3.3 계절 재해 6 → (3.4 입상 든 것 8, 필요할 때)

## 5. 받은 뒤 내가 하는 일

1. 이벤트 컷: `Resources/Art/Story/Events`로 옮기고 `art_map.json` `Events`에 잇는다. 개인 장면·고양이·CD·깊이 컷은 그 이벤트를 만들 때 ID를 정해 잇는다.
2. 유랑자 초상: 배경을 지워 `Portraits/char_wanderer_neutral`로 두고 `Speakers`에 "유랑자"를 더한다.
3. 아이콘: 크기를 맞춰 `Resources/Art/UI`에 넣고 `Icons`에 키를 더한다. 역할·외출·특기 아이콘은 인물 서랍(`PeopleView`)에서 `StoryArt.Icon`으로 부르는 작은 코드를 같이 넣는다.
4. CD: 표지는 `UI/CD`, 음원은 `Resources/Audio/Music`. 배경음악 재생 칸(`sound_map.json` `Music`)과 재생기는 CD 기능 구현 때 만든다.
5. 고양이 결정이 ①이면 대본의 "삼색"을 고치고 `ending_seat_senior`를 바꿔 끼운다.

---

## 6. 만들지 않는 것 (PixelLab 몫)

아래는 픽셀 아트라 힉스필드로 만들지 않는다. 내가 PixelLab으로 만든다.
- 탐험 화면 전부: 주인공·동료·좀비·생존자 스프라이트, 소품, 벽·바닥 타일 (`Resources/Art/ExplorationPixel/`, 배치표 `exploration_art.json`).
- 탐험의 깊이(B): 깊은 문, 일방 출구(안·밖), 깊은 곳 벽·바닥 타일 2~3벌, 비상등 빛.
- 계절 재해(6): 침수된 바닥 타일·물결, 눈 쌓인 진입로.
- 허브 학교 단면도의 방 벽·바닥과 걷는 사람(`hub_section.json` `NpcSprites`), 고양이의 단면도 모습, 바깥 풍경(`UI/HubSky/`).
- CD를 줍는 탐험 소품(수색 지점 상자).
