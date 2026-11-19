# Higgsfield 에셋 생성 문서 (게임 전체: 이미지·스프라이트·UI·사운드·영상)

〈마지막 학기〉에 들어가는 **모든 이미지·사운드·영상 에셋**을 Higgsfield에서 만들기 위한 프롬프트 모음이다.

- 일러스트(타이틀, 캐릭터 기준 시트와 표정, 허브 배경, 지역 카드, 1막 이벤트 컷, 엔딩 일러스트)는 [ART_PROMPTS.md](ART_PROMPTS.md)에 있다.
- 이 문서는 그 밖의 전부를 다룬다: 2~4막 인물·이벤트 컷, 탐험 스프라이트, 소품·타일, UI 아이콘·프레임, 효과음·환경음·음악·음성, 영상.
- 프롬프트는 영어(모델 성능), 설명은 한국어로 쓴다.

---

## 0. 쓰는 법

### 0.1 어디서 생성하나

| 경로 | 비고 |
|---|---|
| **Claude 채팅(claude.ai) + Higgsfield 커넥터** | 프로모션 무제한 생성이 여기서 된다. Claude Code에서는 무제한이 막혀 있다. 채팅에 이 문서의 프롬프트를 붙여 넣고 "이 프롬프트로 ○○ 모델, 16:9로 만들어 줘"라고 요청하면 된다 |
| Higgsfield 웹 | 프롬프트를 직접 붙여 넣는다 |
| `higgsfield` CLI (이 PC에 설치됨) | 크레딧이 있을 때만. 각 표의 명령 예시를 그대로 쓰면 된다 |

### 0.2 모델 선택

| 종류 | 모델 | 이유 |
|---|---|---|
| 일러스트·배경·이벤트 컷 | `gpt_image_2_6` | 기본 고품질. 참고 이미지를 받는다 |
| 캐릭터 표정·같은 인물 유지 | `seedream_v5_pro` 또는 `nano_banana_2_1` + 기준 시트를 참고 이미지로 | 얼굴 일관성 |
| 픽셀 스프라이트 원본 포즈 | `gpt_image_2_6` (`background: transparent`) 또는 `nano_banana_flash` | 픽셀 형태 유지 |
| **스프라이트 애니메이션 시트** | **`autosprite`** | 걷기·달리기 등을 한 번에 시트로 만들고 배경도 지운다 |
| 배경 제거 | `image_background_remover` | 투명 PNG가 필요할 때 |
| 이어지는 타일(벽·바닥) | `gpt_image_2` (편집으로 이음매 정리) | 반복 타일 |
| UI 아이콘·로고 | `recraft_v4_1` (`model_type: vector`) 또는 `gpt_image_2_6` (투명 배경) | 벡터 느낌, 단순한 실루엣 |
| 효과음·환경음 | `seed_audio` | 기본 오디오 모델 |
| 배경음악 | `sonilo_music` (`duration` 지정) | 음악 전용 |
| 라디오 목소리 | `text2speech_v2` 또는 `inworld_text_to_speech` | 한국어 음성 지원 여부는 `voices` 목록에서 먼저 확인 |
| 영상 | `seedance_2_5` (`mode: omni_reference` + 시작 이미지) | 일러스트를 움직이게, 4~15초 |

### 0.3 공통 스타일 공식 (프롬프트에 그대로 붙인다)

모든 이미지가 한 게임처럼 보이도록, 종류별 공식 하나를 **글자 그대로** 붙인다.

**[F-ILLU] 일러스트**
```
Korean indie visual novel illustration, soft painterly 2D, muted low-saturation palette, quiet post-apocalyptic school, gentle diffused light, subtle film grain, clean line art, melancholic but warm mood
```

**[F-PIXEL] 탐험 픽셀 아트**
```
side-view 2D pixel art game sprite, crisp chunky pixels on a 4x-detail grid, limited muted palette of at most 16 colors, 1-pixel dark outline, flat shading with a single highlight tone, no anti-aliasing, no blur, no gradient, orthographic side view
```

**[F-UI] UI 아이콘·프레임**
```
flat game UI asset, muted earthy palette with dark charcoal #1d1f22, brass accent #b9a37a and paper white #e4e1da, simple bold silhouette, thin dark outline, subtle school-supplies texture (paper, chalk, masking tape), readable at 32 pixels, centered, transparent background
```

**[F-AUDIO] 소리 공통 (프롬프트 끝에)**
```
quiet melancholic post-apocalyptic school atmosphere, realistic, not cartoonish
```

### 0.4 안전 규칙 (문서 13장)

모든 이미지·영상 프롬프트 끝에 붙인다.
```
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

- 주인공과 학생은 미성년이다. 성적 뉘앙스는 어떤 형태로도 넣지 않는다.
- 좀비는 멀리 있는 실루엣·흐릿한 형체·환경 재해처럼 그린다. 정면 고어는 없다.
- 선생님 다리는 접어 고정한 바지와 목발로만 보인다.
- 자해·자살을 암시하는 장면은 만들지 않는다.

### 0.5 결과물 넣는 곳과 파일 이름

`ArtInbox/` 아래 종류별 폴더에 이 문서의 **파일 이름 그대로** 넣는다. 받는 대로 크기·배경을 다듬어 게임에 연결한다.

```
ArtInbox/
  illustrations/   (ART_PROMPTS.md + 이 문서 1장)
  sprites/         (2장: 캐릭터·좀비·NPC 애니메이션 시트)
  props/           (3장: 소품·문·타일)
  ui/              (4장: 아이콘·프레임·로고)
  sfx/             (5.1~5.3)
  music/           (5.4)
  voice/           (5.5)
  video/           (6장)
```

### 0.6 우선순위

| 등급 | 뜻 |
|---|---|
| **P1** | 없으면 게임이 어색한 것: 주인공·좀비 스프라이트, 자원 아이콘, 핵심 효과음, 허브·탐험 음악 |
| **P2** | 있으면 크게 좋아지는 것: NPC 스프라이트, 아이템 아이콘, 환경음, 오프닝, 주요 엔딩 영상 |
| **P3** | 마감용: 모든 엔딩 영상, 계절별 변형, 음성 |

---

## 1. 일러스트 추가분 (ART_PROMPTS.md 보완)

ART_PROMPTS.md 이후 정해진 인물과 2~4막 장면이다. 모두 앞에 [F-ILLU], 끝에 0.4 안전 문구를 붙인다.

### 1.1 새 인물 기준 시트와 표정 (`gpt_image_2_6`, 16:9 시트 / 3:4 표정)

먼저 기준 시트를 만들고, 그 시트를 참고 이미지로 넣어 표정을 만든다(ART_PROMPTS 0.4와 같은 방식).

| 우선 | 파일 | 인물 | 기준 시트 프롬프트 (끝에 `front view and side view, full body, plain grey background`) | 표정 |
|---|---|---|---|---|
| P2 | `char_nurse_sheet.png` | 박 선생님 (보건 교사, 40대 여성) | `Korean school nurse, woman in her 40s, neat short bob haircut, tired but sharp eyes, white nurse gown over a quilted padded vest, rolled sleeves, medical pouch on the belt, holding a small notebook` | neutral, stern, worried, smile |
| P2 | `char_bystander_sheet.png` | 송예지 (방관했던 동창, 16세) | `16-year-old Korean high school girl, long straight hair tied low, thin and nervous, avoiding eye contact, slightly oversized navy school uniform, dirty white sneakers, hugging her own arms` | neutral, guilty, cry, small smile |
| P2 | `char_ringleader_sheet.png` | 최하린 (주동자, 16세) — 악마화하지 않는다 | `16-year-old Korean high school girl, sharp eyes but tired, a little thinner than she used to be, shoulder-length dyed brown hair grown out, navy uniform with a borrowed adult parka, ordinary face, not villainous` | neutral, defensive, ashamed, crying quietly |
| P3 | `char_wanderer_sheet.png` | 유랑자 (나이 불명 성인) | `adult wanderer of unknown age, worn faded hiking jacket, wide-brim hat shadowing the face, large backpack with a rolled sleeping mat, walking stick, face mostly hidden` | 표정 없음 (얼굴 가림) |
| P3 | `char_banjang_sheet.png` | 정 반장 (마트 무리 대표, 50대 남성) | `Korean man in his 50s, warehouse foreman, short grey buzz cut, broad shoulders, faded work vest with reflective stripes, work gloves, calm and practical expression` | neutral, smile |
| P3 | `char_teacher_ill.png` | 윤 선생님 병세 (상반신) | 선생님 기준 시트를 참고 이미지로: `same teacher, pale and feverish, sweating, wrapped in a blanket, glasses slightly crooked, still trying to look composed` | — |

### 1.2 2~4막 이벤트 컷 (`gpt_image_2_6`, 16:9)

인물이 나오면 기준 시트를 참고 이미지로 넣는다. 계절 문구(ART_PROMPTS 0.1)를 맞춘다.

| 우선 | 파일 | 장면 | 프롬프트 (F-ILLU 뒤에) |
|---|---|---|---|
| P2 | `evt_a2_cafeteria.png` | 급식실 첫 저녁 | `School cafeteria at dusk, a few people eating instant noodles on steel trays at a long table, a camping stove, the last lunch menu board still on the wall, summer, warm candlelight` |
| P2 | `evt_a2_library.png` | 도서관 | `Quiet school library, a teacher on crutches leaning against a bookshelf, sunbeams and floating dust, a girl holding a worn novel, summer` |
| P2 | `evt_a2_trace.png` | 친구의 흔적 | `Close-up of a bus stop bench, underneath it a torn bag strap, a cracked smartphone and a student ID card with a blurred photo, summer light, no people` |
| P2 | `evt_a2_locked_house.png` | 잠긴 집 | `A blue villa front door, two cans of food and a water bottle placed neatly on the doorstep, a narrow alley, quiet summer afternoon` |
| P2 | `evt_a2_records.png` | 생활기록부 | `A teacher sitting at a desk under a flashlight reading a student record folder, his hand covering his mouth, night, staff room` |
| P2 | `evt_a2_storm.png` | 태풍 | `Classroom window shattered by a typhoon, rain pouring in, desks stacked as a barricade, people bracing it, night, no injuries visible` |
| P2 | `evt_a3_radio.png` | DJ 첫 교신 | `School broadcasting room at night, an old radio glowing, a girl leaning toward the microphone, warm small light, autumn` |
| P2 | `evt_a3_rescue.png` | 구조 공지 | `People in a classroom gathered around a radio, a boy cheering with fists up, a teacher on a chair smiling while looking down at his folded trouser leg, autumn evening` |
| P2 | `evt_a3_face.png` | 주동자 직면 | `Warehouse office, two teenage girls facing each other at a distance across a desk of canned food, tense but not violent, cold autumn light` |
| P2 | `evt_a3_reinfection.png` | 재감염 | `Nurse office at night, a feverish teacher lying in a bed wrapped in a blanket, a nurse reading a thermometer, a girl holding his hand, single lamp` |
| P2 | `evt_a4_waterworks.png` | 정수장 연구동 | `Water treatment plant research annex at night, dark concrete settling tanks, a flashlight beam on a basement archive door, snow` |
| P2 | `evt_a4_last_lesson.png` | 마지막 수업 | `Night classroom, a teacher lying on a mattress near the chalkboard dictating, a girl writing on the chalkboard with fresh chalk, candles, winter` |
| P2 | `evt_a4_graduation.png` | 졸업식 | `Empty school auditorium in winter, a few chairs in front of many empty seats, a teacher on an office chair calling names from a list, a girl bowing` |
| P3 | `evt_a4_night_before.png` | 구조 전날 밤 | `School rooftop at night under stars, a red SOS freshly painted on snow, a small group of survivors sitting together wrapped in blankets` |
| P3 | `evt_ng_storage.png` | 창고 안의 7일 (2회차) | `Pitch dark sports storage room, a single thin line of light under the door, stacked water bottle crates, a girl's hand resting on the floor, almost black image` |

### 1.3 엔딩 일러스트 추가

ART_PROMPTS 6장의 21장에 하나를 더한다.

| 우선 | 파일 | 제목 | 프롬프트 |
|---|---|---|---|
| P2 | `ENDING_BAD_INFECTION.png` | 깨어난 열 | `A closed nurse office door, a diary left open on the windowsill inside seen through the glass, the last page with a short line of handwriting (unreadable), early spring light, a teacher sitting on the floor outside the door with a poetry book` |

---

## 2. 탐험 스프라이트 (P1~P2)

### 2.1 규격

- 지금 게임은 코드로 그린 16×32 픽셀 캐릭터를 쓴다(18픽셀 = 1미터).
- Higgsfield 결과는 **4배 디테일(64×128 프레임)**로 받아, 같은 화면 크기로 보이게 맞춘다. 더 크게 나와도 내가 줄인다.
- 모든 캐릭터는 **오른쪽을 보는 옆모습**. 왼쪽은 게임에서 뒤집는다.
- 배경은 캐릭터에 없는 단색(마젠타 `#ff00ff` 또는 초록 `#00ff00`)으로 생성하거나 투명 배경으로 받는다.
- 머리 위·발 아래에 여백을 둔다(애니메이션이 잘리지 않게).

### 2.2 만드는 순서 (캐릭터 하나당)

1. **원본 포즈**: [F-PIXEL] + 인물 설명 + `full body, standing idle, facing right, empty margin above head and below feet, plain magenta #ff00ff background`
   - 모델 `gpt_image_2_6`, 비율 1:2(또는 2:3)
2. **애니메이션 시트**: 원본을 `autosprite`에 넣는다.
   - CLI 예시: `higgsfield generate create autosprite --image_url <업로드 URL> --kind walk --frame_count 8 --frame_size 256 --remove_bg default --is_humanoid true --wait`
   - Claude 채팅에서는 "이 이미지로 autosprite walk 8프레임 시트 만들어 줘"
3. **표준 동작으로 안 되는 것**은 `--kind custom` + `--name` + `--prompt`(동작 설명)로 만든다.

### 2.3 캐릭터 목록

인물 설명은 ART_PROMPTS·1.1의 기준 시트와 같게 맞추고, 앞에 [F-PIXEL]을 붙인다.

| 우선 | 파일 접두어 | 인물 (원본 포즈 프롬프트 핵심) | 필요한 동작 (autosprite `kind` / custom 설명) |
|---|---|---|---|
| P1 | `player_` | `16-year-old Korean schoolgirl, shoulder-length black hair, navy school blazer and knee-length plaid skirt, brown backpack, worn sneakers` | `idle`(숨쉬기, 6프레임), `walk`(8), `run`(8), custom `sneak`(웅크려 걷기 8: `crouched low, tiptoeing carefully`), custom `attack`(5: `swinging a mop handle forward, one-shot`), custom `push`(4: `shoving forward with both hands`), custom `hide`(4: `crouching and covering head`), custom `hurt`(3), custom `down`(6: `collapsing to her knees, then lying still, no blood`), custom `search`(6: `rummaging through a locker`) |
| P2 | `player_gear_` | 같은 주인공, 장비 착용 변형 | `idle`만: `wearing a riot vest` / `wearing a thick padded jacket` / `carrying a hiking backpack` (장비 표시용 겹쳐 그리기는 내가 처리) |
| P1 | `zombie_wanderer_` | `slow shambling infected adult in torn office clothes, hunched, greyish-green skin, face in shadow, not gory` | custom `dormant`(2~4: `standing asleep with head drooped, swaying slightly`), `walk`(6), `run`(6: `lurching fast`), custom `stun`(3: `staggering back`), custom `grab`(4: `reaching forward`) |
| P1 | `zombie_listener_` | `blind infected in a dirty hospital gown, head tilted to listen, very thin, eyes covered by hair, not gory` | 위와 같은 5동작. dormant는 `head tilting toward sounds` |
| P1 | `zombie_watcher_` | `tall infected in a long dark coat, unusually upright posture, faint pale eyes visible, slow deliberate movement, not gory` | 위와 같은 5동작. dormant는 `standing perfectly still, head slowly turning` |
| P2 | `npc_dohyeon_` | `15-year-old Korean boy, messy black hair, oversized green middle school tracksuit, holding a flashlight` | custom `hiding`(2: `crouched under a desk, trembling`), `idle`(4) |
| P2 | `npc_nurse_` | 박 선생님 (1.1) | custom `hiding`(2: `hiding inside a wardrobe holding scissors`), `idle`(4) |
| P2 | `npc_bystander_` | 송예지 (1.1) | custom `sitting`(2: `sitting hugging her knees`), `idle`(4) |
| P2 | `npc_ringleader_` | 최하린 (1.1) | custom `sitting`(2: `sitting on a crate counting cans`), `idle`(4) |
| P3 | `npc_mart_adults_` | `three adult survivors in work clothes holding wooden planks, cautious but not hostile` (한 장) | `idle`(4) |
| P3 | `npc_wanderer_` | 유랑자 (1.1) | `walk`(8), `idle`(4) |
| P3 | `npc_survivor_a_` | `17-year-old student in a faded blue gym uniform clutching a bag of instant noodles` | `idle`(4) |
| P3 | `fx_` | 효과 스프라이트 | custom `noise_ring`(6: `expanding sound wave ring, white, transparent`), custom `zzz`(4: `small floating z letters, sleep indicator`), custom `alert`(4: `exclamation mark pop`), custom `search_sparkle`(6), custom `dust_hit`(5: `small dust puff`) |

> 좀비는 문서 2장 "환경 재해" 톤이다. 상처·피·내장은 넣지 않고, 실루엣과 자세로만 위협을 표현한다.

---

## 3. 소품·문·타일 (P1~P2)

### 3.1 소품 (`gpt_image_2_6`, 투명 배경)

[F-PIXEL] + 설명 + `single object, side view, centered, transparent background`. 지금 게임에 쓰는 이름 그대로 저장하면 바로 교체된다.

| 우선 | 파일 | 설명 |
|---|---|---|
| P1 | `prop_locker.png` | `tall grey school locker with two narrow doors and vents` |
| P1 | `prop_desk.png` | `Korean school student desk with attached chair, light wood top, steel legs` |
| P1 | `prop_cabinet.png` | `grey steel filing cabinet, three drawers` |
| P1 | `prop_fridge.png` | `old white refrigerator, slightly dirty` |
| P1 | `prop_box.png` | `stack of cardboard boxes taped shut` |
| P1 | `prop_shelf.png` | `convenience store shelf with colorful snack packages` |
| P1 | `prop_medcabinet.png` | `white wall medicine cabinet with a green cross` |
| P2 | `prop_chalkboard.png` | `long green classroom chalkboard with faint chalk notes (unreadable), wooden frame and chalk tray` (가로로 긴 비율 4:1) |
| P1 | `prop_stairs_up.png` / `prop_stairs_down.png` | `concrete school stairwell going up` / `going down`, `with metal handrail` |
| P1 | `prop_exit.png` / `prop_exit_locked.png` | `metal emergency exit door with a green running-man sign (no text)` / `same door chained and padlocked` |
| P2 | `prop_window.png` | `classroom window with four glass panes, grey aluminium frame, dusty` |
| P2 | `prop_ceiling_lamp.png` | `long fluorescent ceiling lamp` (가로로 긴 비율) |
| P2 | `prop_notice.png` | `school bulletin board with pinned papers (unreadable)` |
| P2 | `prop_plate.png` | `small classroom name plate sign (blank)` |
| P2 | `prop_paper.png` | `single folded document on the floor` |
| P2 | `door_closed.png` / `door_open.png` / `door_dark.png` | `wooden classroom sliding door, closed` / `same door half open showing darkness` / `same door seen in darkness` |
| P2 | `prop_counter.png` | `convenience store cashier counter` |
| P2 | `prop_pallet.png` | `warehouse pallet stacked with shrink-wrapped boxes` |
| P2 | `prop_lab_bench.png` | `laboratory bench with beakers and a water filter setup` |
| P2 | `prop_tank.png` | `concrete water treatment tank edge with railing` |
| P2 | `prop_bench.png` | `subway platform bench, metal` |
| P2 | `prop_police_rack.png` | `police equipment rack with riot shields and batons (no guns)` |
| P3 | `prop_mattress.png`, `prop_camping_stove.png`, `prop_radio.png`, `prop_bucket_garden.png` | 거점 소품 (허브용) |

### 3.2 벽·바닥 타일 (반복 이음매, `gpt_image_2`)

[F-PIXEL] + 설명 + `seamless tileable texture, flat front view, no perspective, tiles horizontally`. 정사각형으로 받고, 이음매가 보이면 `gpt_image_2` 편집으로 "make the edges seamless"를 한 번 더 돌린다.

| 우선 | 파일 | 설명 |
|---|---|---|
| P1 | `wall_corridor.png` | `school corridor wall, upper half cream plaster, lower half pale green wainscot panels` |
| P1 | `wall_classroom.png` | `classroom wall, beige plaster upper, brown wooden wainscot lower` |
| P2 | `wall_office.png` | `staff room wall, light grey plaster with a thin wooden rail` |
| P2 | `wall_restroom.png` | `white square bathroom tiles with grey grout` (지하철역에도 씀) |
| P2 | `wall_store.png` | `convenience store wall, clean white tiles with a thin colored stripe` |
| P2 | `wall_pharmacy.png` | `clinic wall, pale blue paint with a white lower panel` |
| P2 | `wall_room_outside.png` | `villa apartment hallway wall, beige textured plaster` |
| P1 | `wall_brick.png` | `red brick building exterior` |
| P1 | `ground.png` | `cracked asphalt ground with small weeds` |
| P3 | `wall_mart.png`, `wall_police.png`, `wall_waterworks.png` | `supermarket wall with faded promo stripes` / `police station wall, grey-blue paint` / `bare concrete industrial wall with pipes` |

---

## 4. UI 에셋 (P1~P2)

모두 [F-UI]. 아이콘은 **1:1, 512px 이상**으로 받고 내가 32·64px로 줄인다. 같은 묶음은 한 번에 "아이콘 세트, 같은 스타일로 N개"로 요청하면 통일감이 좋다.

### 4.1 자원·상태 아이콘

| 우선 | 파일 | 설명 |
|---|---|---|
| P1 | `icon_res_food.png` | `canned food tin` |
| P1 | `icon_res_water.png` | `plastic water bottle` |
| P1 | `icon_res_medicine.png` | `first aid box with a cross` |
| P1 | `icon_res_materials.png` | `bundle of planks and a roll of duct tape` |
| P1 | `icon_res_power.png` | `car battery with a lightning bolt` |
| P1 | `icon_res_fuel.png` | `small fuel canister` |
| P1 | `icon_vital_hp.png` | `simple heart` |
| P1 | `icon_vital_fatigue.png` | `drooping eyelid / sleepy face` |
| P1 | `icon_vital_stress.png` | `tangled scribble cloud` |
| P1 | `icon_vital_hunger.png` | `empty bowl with chopsticks` |
| P1 | `icon_vital_thirst.png` | `water drop with a crack` |
| P2 | `icon_symptom.png` | `thermometer` (감염 증상) |
| P1 | `icon_sleep_debt.png` | `crescent moon with a small chain` |
| P2 | `icon_conscience.png` | `small weight scale` |
| P1 | `icon_defense.png` / `icon_threat.png` | `barricaded door` / `eye peeking through a crack` |
| P2 | `icon_rescue.png` | `helicopter silhouette with a countdown tick` |
| P2 | `icon_teacher_stage_0~4.png` | `teacher's glasses` 단계별: `clean` / `with a small cough mark` / `with a fever line` / `lying on a pillow` / `dim and faded` |

### 4.2 날씨·계절

| 우선 | 파일 | 설명 |
|---|---|---|
| P1 | `icon_weather_clear.png` / `_rain.png` / `_heat.png` / `_cold.png` | `sun` / `rain cloud` / `sun with heat waves` / `snowflake with frost` |
| P2 | `icon_season_spring.png` / `_summer.png` / `_autumn.png` / `_winter.png` | `pale green sprout` / `cicada on a leaf` / `ginkgo leaf` / `bare branch with snow` |

### 4.3 능력치·숙련도·행동 분류

| 우선 | 파일 | 설명 |
|---|---|---|
| P2 | `icon_stat_stamina.png` ~ `icon_stat_leadership.png` (8종) | 체력 `running shoe`, 근력 `dumbbell`, 민첩 `feather`, 지력 `open book with a lightbulb`, 손재주 `screwdriver and wrench`, 의지 `clenched fist`, 감수성 `hand holding a small flower`, 통솔 `small flag` |
| P2 | `icon_prof_medicine.png` ~ `icon_prof_mind.png` (7종) | 응급·의학 `bandage roll`, 생존 `compass`, 공학 `gear`, 호신 `baseball bat`, 요리 `pot with steam`, 인문·기록 `fountain pen and notebook`, 심리 `cup of tea` |
| P1 | `icon_cat_education.png` / `_survival.png` / `_rest.png` / `_daily.png` / `_expedition.png` | `chalk and textbook` / `hammer` / `pillow` / `diary` / `backpack with flashlight` |

### 4.4 아이템 아이콘 (27종, P2)

[F-UI]에서 `transparent background, item icon, three-quarter view` 로 바꿔 쓴다. 파일 이름은 `item_` + 아이템 ID 소문자.

| 파일 | 설명 | 파일 | 설명 |
|---|---|---|---|
| `item_canned_food.png` | canned food | `item_snack.png` | bag of snack chips |
| `item_water_bottle.png` | water bottle | `item_bandage.png` | bandage roll |
| `item_painkiller.png` | blister pack of pills | `item_scrap.png` | scrap metal pieces |
| `item_battery.png` | AA batteries | `item_textbook.png` | worn school textbook |
| `item_instant_noodle.png` | instant noodle cup | `item_drink.png` | sports drink bottle |
| `item_medkit.png` | first aid kit | `item_antibiotic.png` | medicine bottle |
| `item_vitamin.png` | vitamin jar | `item_duct_tape.png` | duct tape roll |
| `item_lighter.png` | disposable lighter | `item_bag_school.png` | school backpack |
| `item_bag_hiking.png` | large hiking backpack | `item_bag_tote.png` | shopping tote bag |
| `item_weapon_mop.png` | mop handle wrapped with tape | `item_weapon_pipe.png` | steel pipe |
| `item_weapon_bat.png` | wooden baseball bat | `item_weapon_baton.png` | police telescopic baton |
| `item_tool_flashlight.png` | flashlight | `item_tool_crowbar.png` | crowbar |
| `item_armor_guard.png` | sports shin and elbow guards | `item_armor_padding.png` | thick padded jacket |
| `item_armor_riot.png` | riot vest | | |

### 4.5 시설·지역·인물 아이콘 (P2)

| 파일 | 설명 |
|---|---|
| `icon_fac_storage.png` ~ `icon_fac_staffroom.png` (10종) | 창고 `storage shelves`, 교실동 `school desk`, 보건실 `nurse bed with cross`, 옥상 `rooftop with SOS`, 방송실 `microphone`, 급식실 `food tray`, 도서관 `bookshelf`, 과학실 `beaker`, 체육관 `basketball hoop`, 교무실 `stack of folders` |
| `icon_loc_school.png` ~ `icon_loc_waterworks.png` (9종) | 학교 `school building`, 튜토리얼 `storage door`, 편의점 `convenience store front`, 약국 `pharmacy cross sign`, 주택가 `small villa`, 지하철 `subway entrance`, 마트 `shopping cart`, 파출소 `police box`, 정수장 `water tank and pipes` |
| `avatar_teacher.png`, `avatar_dohyeon.png`, `avatar_nurse.png`, `avatar_bystander.png`, `avatar_ringleader.png`, `avatar_friend.png` | 기준 시트를 참고 이미지로 `small round portrait badge, head and shoulders` |

### 4.6 프레임·패널·로고 (P1~P2)

| 우선 | 파일 | 설명 (F-UI 뒤에) | 비고 |
|---|---|---|---|
| P1 | `logo_title.png` | `game title logo, Korean text "마지막 학기" handwritten in white chalk on a dark chalkboard texture, small eraser smudges, centered, transparent background around the board` | 글자가 정확한지 꼭 확인. 틀리면 글자 없는 판만 만들고 글자는 내가 폰트로 올린다 |
| P1 | `ui_card_frame.png` | `rectangular card frame like a worn index card pinned with masking tape at the corners, empty center` | 9-slice로 씀 (가장자리 장식만, 가운데 단색) |
| P1 | `ui_button.png` / `ui_button_hover.png` / `ui_button_pressed.png` | `rounded rectangle button like a strip of masking tape on dark paper, empty, normal` / `slightly brighter` / `pressed darker` | 3:1 |
| P2 | `ui_tab.png` | `notebook divider tab` | |
| P2 | `ui_planner_paper.png` | `weekly planner grid paper texture, faint blue lines, seamless` | 주간 계획표 배경 |
| P2 | `ui_diary_page.png` | `lined diary page texture, slightly yellowed, seamless` | 일기·기록 화면 |
| P2 | `ui_chalkboard.png` | `dark green chalkboard texture with faint erased chalk, seamless` | 막 평가·선생님 한마디 |
| P2 | `ui_slot_empty.png` / `ui_slot_locked.png` | `empty planner slot square with dashed outline` / `same slot with a small padlock` | |
| P2 | `ui_backpack_grid.png` | `inventory grid cell, canvas fabric texture` | 배낭 격자 |
| P2 | `ui_dialog_box.png` | `dialogue box frame, translucent dark paper with a brass thin border` | 대화창 |
| P2 | `ui_nameplate.png` | `small name tag like a school uniform name badge, blank` | 화자 이름표 |
| P3 | `ui_gallery_locked.png` | `card back with a small padlock and question mark` | 갤러리 잠긴 칸 |
| P3 | `ui_cursor.png` / `ui_cursor_hand.png` | `small chalk-white arrow cursor` / `pointing hand cursor` | 32px |
| P3 | `ui_loading.png` | `a small paper plane, loading indicator` | |

---

## 5. 사운드

모두 `seed_audio` 기본(음악만 `sonilo_music`). 프롬프트 끝에 [F-AUDIO]를 붙인다. **효과음은 소리 하나만** 담고 `no music, no voice`를 붙인다.

### 5.1 탐험 효과음 (P1~P2)

| 우선 | 파일 | 길이 | 프롬프트 |
|---|---|---|---|
| P1 | `sfx_step_tile.wav` | 0.5s | `single quiet sneaker footstep on a school corridor linoleum floor, close, dry, no music, no voice` |
| P1 | `sfx_step_run.wav` | 0.4s | `single running footstep on a hard floor, slightly louder, no music, no voice` |
| P2 | `sfx_step_concrete.wav`, `sfx_step_metal.wav` | 0.5s | 같은 문장에서 `concrete stairs` / `metal grating` 로 바꿈 |
| P1 | `sfx_door_open.wav` | 1.5s | `old classroom sliding door opening slowly with a squeak, no music` |
| P1 | `sfx_door_locked.wav` | 1s | `locked door handle rattling, no music` |
| P1 | `sfx_door_force.wav` | 2s | `crowbar prying a door open, metal creak and a loud crack, no music` |
| P1 | `sfx_search.wav` | 2s | `rummaging through a metal locker, objects shuffling, no music` |
| P1 | `sfx_pickup.wav` | 0.5s | `small item picked up and put into a backpack, zipper, no music` |
| P2 | `sfx_backpack_full.wav` | 0.6s | `backpack zipper straining, soft thud, no music` |
| P1 | `sfx_swing.wav` | 0.5s | `wooden stick swinging through the air, whoosh, no music` |
| P1 | `sfx_hit.wav` | 0.5s | `dull thump of a wooden bat hitting a heavy coat, muffled, not gory, no music` |
| P1 | `sfx_push.wav` | 0.5s | `shove and stumble on a floor, no music` |
| P1 | `sfx_hurt.wav` | 0.6s | `sharp breath intake of a young woman in pain, no scream, no music` |
| P1 | `sfx_down.wav` | 1.5s | `body slumping to the floor, heavy breathing fading, no music` |
| P1 | `sfx_flashlight.wav` | 0.3s | `flashlight button click` |
| P1 | `sfx_hide_breath.wav` | 3s, 반복 | `held breathing, slow and quiet, heartbeat faint, loopable, no music` |
| P1 | `sfx_heartbeat.wav` | 2s, 반복 | `tense heartbeat, low, loopable, no music` |
| P2 | `sfx_peek.wav` | 1s | `door cracked open a few centimeters, faint draft, no music` |
| P2 | `sfx_listen.wav` | 2s | `ear pressed against a door, muffled room tone, no music` |
| P1 | `sfx_noise_sting.wav` | 1s | `sudden loud clatter of a metal object dropping in an empty hallway, echo, no music` |
| P1 | `sfx_extract.wav` | 2s | `heavy door closing behind, relief exhale, no music` |
| P1 | `sfx_timer_warning.wav` | 1.5s | `distant church-like bell toll muffled by walls, single, no music` (일몰·새벽 경고) |
| P2 | `sfx_encounter.wav` | 1.5s | `soft piano single note with a hush, discovery cue` |

### 5.2 좀비 소리 (P1) — 멀리서 들리는 연출이 기본

| 파일 | 길이 | 프롬프트 |
|---|---|---|
| `sfx_z_wanderer_idle.wav` | 3s, 반복 | `distant low raspy breathing of an infected person, slow, muffled by walls, not gory, no music` |
| `sfx_z_wanderer_alert.wav` | 1.5s | `low guttural groan rising in alarm, medium distance, no music` |
| `sfx_z_wanderer_chase.wav` | 2s | `fast shuffling footsteps and strained breathing approaching, no music` |
| `sfx_z_listener_idle.wav` | 3s | `faint clicking sounds and soft wet breathing, head turning, eerie, no music` |
| `sfx_z_listener_alert.wav` | 1.5s | `sharp high-pitched rasp, startled, no music` |
| `sfx_z_watcher_idle.wav` | 3s | `almost silent, slow deep exhale every few seconds, unsettling, no music` |
| `sfx_z_watcher_alert.wav` | 1.5s | `long low drone-like moan, ominous, no music` |
| `sfx_z_dormant_crowd.wav` | 6s, 반복 | `room full of sleeping infected, many slow overlapping breaths, very quiet, loopable, no music` |
| `sfx_z_stun.wav` | 1s | `stumbling footsteps and a dazed grunt, no music` |
| `sfx_z_grab.wav` | 1s | `cloth grab and struggle, no gore, no scream, no music` |
| `sfx_z_horde_far.wav` | 8s, 반복 | `distant horde moaning outside a building at night, wind, loopable` (습격 전조) |

### 5.3 허브·UI 효과음 (P1~P2)

| 우선 | 파일 | 길이 | 프롬프트 |
|---|---|---|---|
| P1 | `ui_click.wav` | 0.2s | `soft pencil tap on paper, UI click` |
| P1 | `ui_hover.wav` | 0.15s | `very soft paper rustle` |
| P1 | `ui_slot_place.wav` | 0.3s | `sticky note pressed onto paper` |
| P1 | `ui_week_start.wav` | 1s | `school bell short chime, gentle, distant` |
| P1 | `ui_success.wav` | 0.8s | `warm two-note piano chime, success` |
| P2 | `ui_great_success.wav` | 1.2s | `bright three-note piano and soft bell, great success` |
| P1 | `ui_failure.wav` | 0.8s | `low soft piano note, gentle disappointment` |
| P1 | `ui_typing.wav` | 0.05s | `single soft typewriter-like blip for dialogue text, very short` |
| P1 | `ui_page_turn.wav` | 0.5s | `notebook page turning` |
| P2 | `ui_diary_write.wav` | 1.5s | `pencil writing quickly on paper` |
| P1 | `ui_raid_alarm.wav` | 2s | `metal pot banged as an improvised alarm, urgent, echoing hallway` |
| P2 | `ui_raid_repelled.wav` | 2s | `barricade holding, heavy thuds stop, exhale of relief` |
| P2 | `ui_raid_breached.wav` | 2s | `barricade collapsing, desks crashing, no screams` |
| P2 | `ui_radio_tune.wav` | 2s | `old radio dial tuning through static` |
| P2 | `ui_radio_static.wav` | 6s, 반복 | `soft radio static hiss, loopable` |
| P2 | `ui_dj_jingle.wav` | 4s | `short warm lo-fi radio station jingle, acoustic guitar and soft hum, no lyrics` |
| P2 | `ui_rescue_heli.wav` | 8s | `rescue helicopter approaching from far to overhead, rotor thumping` |
| P2 | `ui_ending_unlock.wav` | 2s | `gentle music box arpeggio, unlock` |
| P2 | `ui_cough.wav` | 1.5s | `adult man coughing softly under a blanket, not gross` (선생님 병세) |

### 5.4 환경음 반복 (P2, 30초, 반복용)

| 파일 | 프롬프트 |
|---|---|
| `amb_hub_spring.wav` / `_summer.wav` / `_autumn.wav` / `_winter.wav` | `inside an empty school classroom, distant birds and wind` / `cicadas outside, ceiling fan creak` / `dry leaves in wind, crows far away` / `cold wind against windows, faint creak of radiator`, `loopable, no music` |
| `amb_night_hub.wav` | `school at night, crickets, faint distant moaning very far, candle flicker, loopable` |
| `amb_rain.wav` / `amb_storm.wav` | `steady rain on windows, loopable` / `typhoon wind and rain hammering windows, loopable` |
| `amb_exp_school.wav` | `empty school corridor room tone, distant dripping, loopable` |
| `amb_exp_store.wav` | `abandoned convenience store, broken fridge hum, loopable` |
| `amb_exp_pharmacy.wav` | `quiet clinic waiting room, many sleeping breaths far away, loopable` |
| `amb_exp_residential.wav` | `quiet residential alley, wind chimes, distant dog, loopable` |
| `amb_exp_subway.wav` | `underground subway station, deep low hum, distant echoes, loopable` |
| `amb_exp_mart.wav` | `large empty supermarket, flickering lights buzz, loopable` |
| `amb_exp_police.wav` | `small empty police box, radio noise faint, loopable` |
| `amb_exp_waterworks.wav` | `water treatment plant at night, rushing water and machinery, loopable` |
| `amb_exp_night.wav` | `generic night interior, very dark and still, occasional distant thud, loopable` |

### 5.5 배경음악 (`sonilo_music`, 연주곡, 가사 없음)

| 우선 | 파일 | 길이 | 프롬프트 |
|---|---|---|---|
| P1 | `bgm_title.wav` | 90s | `instrumental, slow melancholic piano and soft strings, hopeful at the end, quiet post-apocalyptic school, 70 bpm, no vocals` |
| P1 | `bgm_hub_spring.wav` | 120s, 반복 | `instrumental gentle piano with light acoustic guitar, calm daily routine, tender, 80 bpm, loopable, no vocals` |
| P2 | `bgm_hub_summer.wav` / `_autumn.wav` / `_winter.wav` | 120s | 위와 같은 구조에서 `warm, humid, lazy` / `wistful, cello added` / `sparse, music box and soft pad, cold` |
| P1 | `bgm_exp_day.wav` | 120s, 반복 | `instrumental minimal tense ambient, low pulses, sparse piano, stealth, 60 bpm, loopable, no vocals` |
| P1 | `bgm_exp_night.wav` | 120s, 반복 | `instrumental dark ambient drone, deep strings, distant percussion, dread, loopable, no vocals` |
| P1 | `bgm_chase.wav` | 60s, 반복 | `instrumental urgent percussion and staccato strings, chase, 130 bpm, loopable, no vocals` |
| P2 | `bgm_raid.wav` | 90s | `instrumental heavy drums and low brass, siege at night, determined, no vocals` |
| P2 | `bgm_teacher_theme.wav` | 120s | `instrumental warm piano solo, gentle and kind, a teacher's theme, slightly sad, no vocals` |
| P2 | `bgm_friend_theme.wav` | 90s | `instrumental bright acoustic guitar and glockenspiel, nostalgic memories of a best friend, no vocals` |
| P2 | `bgm_review.wav` | 60s | `instrumental reflective piano and strings, looking back on a season, no vocals` |
| P2 | `bgm_ending_hope.wav` | 150s | `instrumental uplifting orchestral piano, sunrise, new beginning, emotional climax, no vocals` |
| P2 | `bgm_ending_bitter.wav` | 150s | `instrumental bittersweet piano and cello, farewell, gentle, no vocals` |
| P2 | `bgm_ending_bad.wav` | 90s | `instrumental very quiet sparse piano, silence between notes, grief but tender, no vocals` |
| P3 | `bgm_ng_storage.wav` | 60s | `almost silent dark ambience, muffled sounds through a door, rain, a faintly hummed melody far away, no lyrics` |

### 5.6 라디오 목소리 (P3, `text2speech_v2`)

먼저 사용할 수 있는 목소리 목록을 보고(한국어 지원 여부 확인), 인물마다 **목소리 하나를 정해 계속 쓴다**.

| 파일 | 화자 | 대사 |
|---|---|---|
| `vo_dj_hello.wav` | DJ 새벽 (따뜻한 30대 남성 또는 여성 라디오 진행자) | "새벽시 어딘가에서 아직 깨어 있는 여러분, 좋은 밤입니다. 오늘도 살아 있어 줘서 고마워요." |
| `vo_dj_forecast.wav` | DJ | "오늘의 활동 예보. 북쪽 상가는 조용하고, 강변 쪽은 시끄럽습니다." |
| `vo_dj_goodnight.wav` | DJ | "내일도 살아 있어요. 그게 오늘의 숙제예요." |
| `vo_rescue_announce.wav` | 구조대 (차분하고 훈련된 목소리, 무전 질감) | "새벽시 일대 생존자에게 알립니다. 구조대가 내년 3월 둘째 주에 도착합니다. 감염자는 탑승할 수 없습니다." |
| `vo_rescue_route.wav` | 구조대 | "오는 길에 인근 주택가에서 생존자 한 명을 먼저 태웁니다." |
| `vo_subway_broadcast.wav` | 지하철 안내 방송 | "승객 여러분께 알려 드립니다. 정수장 방향 열차는 운행하지 않습니다." |

> 결과에 무전 질감(대역 제한, 잡음)은 내가 후처리로 입힌다.

---

## 6. 영상 (`seedance_2_5`)

- 가능하면 **해당 일러스트를 시작 이미지로** 넣는다(`mode: omni_reference`, `start_image`). 그림체가 유지된다.
- 길이는 5~12초, 1080p, 16:9. 대사 없는 장면이 기본이고, `generate_audio`는 끈다(소리는 5장 에셋으로 따로 깐다).
- 프롬프트 끝에 [F-ILLU]와 0.4 안전 문구를 붙인다. 움직임은 **천천히·조용히**가 기본이다.

### 6.1 타이틀·오프닝 (P2)

| 파일 | 시작 이미지 | 길이 | 프롬프트 |
|---|---|---|---|
| `vid_title_loop.mp4` | `title_key.png` | 10s, 처음 = 끝 | `subtle loop: laundry gently swaying, birds crossing the sky, slow sunrise glow, the girl's hair moving in the wind, camera static` |
| `vid_open_01_morning.mp4` | (없음, t2v) | 8s | `morning commute in a Korean small city, two schoolgirls walking side by side on a sidewalk toward a school gate, cherry trees, seen from behind, normal peaceful day, early spring` |
| `vid_open_02_tteokbokki.mp4` | (없음) | 6s | `close-up of two pairs of hands, one pointing at a tteokbokki shop sign across the street (no readable text), warm morning light` |
| `vid_open_03_school.mp4` | `bg_classroom.png` 기반 생성 이미지 | 6s | `school hallway between classes, students blurred in motion, then the noise fades and the hallway empties, slow push-in` |
| `vid_open_04_storage.mp4` | `evt_ng_storage.png` | 8s | `sports storage door slams shut from outside, the line of light under the door, muffled laughter fades, darkness` |
| `vid_open_05_sirens.mp4` | (없음) | 8s | `wide shot of the small city at dusk from the school rooftop, distant smoke columns, emergency lights blinking far away, no close-up of violence` |
| `vid_open_06_door_opens.mp4` | `evt_door_opens.png` | 8s | `after darkness, the storage door slowly opens, blinding light floods in, a silhouette of a man on the floor holding crutches reaches out` |

### 6.2 막 전환·주요 장면 (P2~P3)

| 우선 | 파일 | 시작 이미지 | 길이 | 프롬프트 |
|---|---|---|---|---|
| P2 | `vid_season_summer.mp4` | `bg_classroom_summer.png` | 6s | `time-lapse feel: sunlight moves across the classroom, cicada season, curtains flutter` |
| P2 | `vid_season_autumn.mp4` | `bg_classroom_autumn.png` | 6s | `ginkgo leaves drifting past the window, golden light shifts` |
| P2 | `vid_season_winter.mp4` | `bg_classroom_winter.png` | 6s | `first snow falls outside the frosted window, breath visible` |
| P2 | `vid_raid.mp4` | `evt_act1_raid.png` | 8s | `shadows press against frosted glass, the barricade shakes, people hold it, flashlight beams, no gore` |
| P2 | `vid_reinfection.mp4` | `evt_a3_reinfection.png` | 6s | `the teacher coughs under a blanket, the nurse lowers the thermometer, the girl's hand tightens, candle flickers` |
| P2 | `vid_last_lesson.mp4` | `evt_a4_last_lesson.png` | 8s | `chalk moving on the board in the girl's hand, the teacher's lips moving softly, candlelight` |
| P1 | `vid_helicopter.mp4` | `ENDING_SURVIVOR.png` | 10s | `a rescue helicopter descends onto the school rooftop at sunrise, snow and dust swirl, the girl waves both arms, the helicopter door slides open and another girl jumps out running toward her` |

### 6.3 엔딩 영상 (22종, P2~P3)

엔딩 일러스트(ART_PROMPTS 6장, 이 문서 1.3)를 시작 이미지로 넣고 **6~8초, 정적인 움직임**으로 만든다. 엔딩 첫 화면 뒤에서 반복 재생한다.

| 우선 | 파일 | 움직임 프롬프트 |
|---|---|---|
| P2 | `vid_ENDING_SURVIVOR.mp4` | `helicopter rotor blur overhead, hair and coat flapping, sun rising` |
| P2 | `vid_ENDING_BALANCED.mp4` | `camp life moving in the background, the young woman walking forward, people waving` |
| P2 | `vid_ENDING_DOCTOR.mp4` | `the doctor walks down the corridor away from the camera, morning light through windows` |
| P2 | `vid_ENDING_ENGINEER.mp4` | `the first traffic light flickers on, people in the distance cheer, dusk deepens` |
| P2 | `vid_ENDING_GUARD.mp4` | `dawn light grows, the camp gate slowly opens, recruits stand still` |
| P2 | `vid_ENDING_LEADER.mp4` | `the crowd's heads turn toward the speaker, wind moves a banner (no text)` |
| P2 | `vid_ENDING_WRITER.mp4` | `blossom petals drift past the window, the hand gently closes the diary` |
| P3 | `vid_ENDING_EDUCATOR.mp4` | `children raise hands one by one, the young teacher smiles, tent fabric moves` |
| P3 | `vid_ENDING_TOGETHER.mp4` | `the group's breath fogs in the cold dawn, the sun crests the horizon` |
| P2 | `vid_ENDING_LONE.mp4` | `wind moves the garden plants, a letter flutters on the porch` |
| P2 | `vid_ENDING_NARR_OVERCOME.mp4` | `she turns around in the hallway with a calm smile, sunlight sweeps across` |
| P2 | `vid_ENDING_NARR_UNHEALED.mp4` | `her hand hesitates, then knocks softly on the counseling room door` |
| P3 | `vid_ENDING_NARR_CONFRONT.mp4` | `wind blows leaves between the two girls, two helicopters fly away in opposite directions` |
| P3 | `vid_ENDING_HIDDEN_TRUTH.mp4` | `lamp light flickers, pages turn slowly, the camera pushes in on the diagram` |
| P3 | `vid_ENDING_HIDDEN_STAY.mp4` | `the helicopter shrinks into the sky, the two figures on the rooftop edge stay still, dusk` |
| P3 | `vid_ENDING_HIDDEN_CURE.mp4` | `the school grows smaller through the helicopter window, the three figures sit close, warm light` |
| P2 | `vid_ENDING_BAD_EXPEDITION.mp4` | `walkie-talkie static light blinking, the window reflection of the teacher still, night` |
| P2 | `vid_ENDING_BAD_RAID.mp4` | `dust settles in morning light over the broken barricade, quiet` |
| P2 | `vid_ENDING_BAD_STARVATION.mp4` | `pale light slowly fades in the window, the two silhouettes don't move` |
| P2 | `vid_ENDING_BAD_COLLAPSE.mp4` | `warm light under the door flickers gently, a hand knocks softly, hopeful` |
| P2 | `vid_ENDING_BAD_WOUNDS.mp4` | `the curtain moves slightly, the poetry book page turns in a draft` |
| P2 | `vid_ENDING_BAD_INFECTION.mp4` | `spring light grows on the open diary by the window, the teacher outside the door lowers the book` |

---

## 7. 체크리스트 (개수)

| 묶음 | 개수 | P1 |
|---|---|---|
| 일러스트 추가 (1장) | 인물 6 + 표정 약 20 + 이벤트 컷 15 + 엔딩 1 | 0 |
| 스프라이트 (2장) | 캐릭터 12종, 동작 약 55개 시트 + 효과 5 | 주인공·좀비 3종 |
| 소품·타일 (3장) | 소품 약 30 + 타일 13 | 약 20 |
| UI (4장) | 아이콘 약 110 + 프레임·로고 15 | 약 30 |
| 효과음·환경음 (5.1~5.4) | 약 75 | 약 40 |
| 음악 (5.5) | 15 | 5 |
| 음성 (5.6) | 6 | 0 |
| 영상 (6장) | 약 37 | 1 |

**3일 무제한 기간에 추천하는 순서**
1. 일러스트 기준 시트 → 표정 (얼굴이 정해져야 스프라이트·영상이 같은 사람이 된다)
2. 주인공·좀비 스프라이트 시트 (P1)
3. UI 아이콘 묶음 (P1 → P2)
4. 효과음·음악 (P1)
5. 영상: 헬기·타이틀 루프·오프닝 → 엔딩
6. 나머지 P2·P3

---

## 8. 받은 뒤 내가 하는 일 (M6-5)

- **스프라이트:** 시트를 잘라 지금 코드 스프라이트 이름에 맞춘다. 크기·피벗·애니메이션 속도를 정하고, 없으면 지금처럼 코드 그림으로 대체되게 둔다.
- **UI:** 아이콘을 상단 자원 막대·상태 패널·행동 팔레트·배낭에 붙인다. 프레임은 9-slice로 카드·버튼·대화창에 입힌다.
- **사운드:** 소리 관리자를 추가하고 볼륨을 맞춘다(음성 −6, 효과음 −10~−12, 음악 −18~−20 dBFS). 설정 화면에 음량 슬라이더를 넣는다.
- **영상:** 오프닝·막 전환·엔딩에 영상 재생을 넣는다(건너뛰기 가능). 영상이 없으면 일러스트로 대체한다.
- **출처 기록:** `Art/ThirdParty/Higgsfield/CREDITS.md`에 생성 모델과 날짜를 남긴다(스팀 AI 콘텐츠 공개용).
