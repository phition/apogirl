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

**[F-ILLU] 일러스트** — 인물은 비주얼노벨 그림체, 배경은 실사에 가까운 정밀한 한국 도시
```
Korean indie visual novel illustration for the characters, clean line art, soft painterly 2D shading, muted low-saturation palette, melancholic but warm mood. Background: highly detailed semi-photorealistic background art, realistic contemporary Korean architecture and interiors, accurate perspective and scale, real materials and textures (weathered concrete, rusted railings, peeling paint, cracked asphalt, dusty glass), natural cinematic lighting with soft volumetric light, quiet post-apocalyptic city with weeds through cracks, subtle film grain, like a meticulously painted background plate for a feature animation film. The characters stay stylized 2D illustration and are lit to match the background.
```

**[F-KOREA] 한국 건물** — 실외, 또는 창밖으로 거리가 보이는 그림에 [F-ILLU] 뒤에 붙인다
```
Korean details: realistic small provincial Korean city, four-story red-brick villas with piloti parking, numbered high-rise apartment towers in the distance (numbers unreadable), mixed-use commercial buildings with stacked signboards (faded or blank, no readable text), utility poles with tangled overhead wires, green-and-white convenience store awnings, pharmacy green cross signs, blue tin roofs and rooftop water tanks, metal window grilles, gas pipes on walls, yellow curb markings, glass bus shelters, ginkgo and cherry street trees, abandoned compact cars and scooters.
```

- **장소별 디테일**(학교 외관·교실·복도·옥상·급식실·편의점·주택가·지하철역·마트·파출소·정수장 등)은 [ART_PROMPTS.md](ART_PROMPTS.md) 0.1의 표에 있다. 해당 장소 문구를 하나 더 붙이면 건물이 더 한국답게 나온다.
- **캐릭터 기준 시트·표정**은 [F-ILLU]의 첫 문장(인물 부분)만 붙인다. 배경 문장까지 넣으면 인물이 실사로 바뀔 수 있다.
- **허브 오브젝트(9.8)처럼 사물만 있는 그림**은 반대로 `Background:` 문장만 붙인다. 실사풍 배경 위에 겹치므로 같은 질감이어야 한다.
- 간판 글자는 엉터리 한글이 나오기 쉬우므로 항상 바랜 간판·빈 간판으로 둔다.

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
| **P4** | 예비: 아직 게임에 없지만 개발 방향에 따라 들어올 수 있는 것 (9장) |

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
| 예비 에셋 (9장, P4) | 아이콘 약 220 + 설비 그림 약 25 + 소리 약 25 | 0 |

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

### 8.1 1차 연결 결과 (2026-11-20)

받은 368개 중 아래를 게임에 넣었다. 원본 → 사본 변환은 `I:/workshop/code_temp/apogirl/tools/`의 `art_import.py`(그림)·`audio_import.py`(소리).
어디에 무엇을 쓰는지는 데이터 표: 그림 `Resources/Art/art_map.json`(초상화·사건 그림·거처 배경·지역·아이콘), 소리 `Resources/Audio/sound_map.json`(분류별 음량·이벤트 소리·대사 음성). 그림·소리가 없으면 예전처럼 글자·코드 그림·무음으로 돌아간다.

| 연결함 | 어디에 |
|---|---|
| 타이틀 키 아트·로고 | 타이틀 배경, 제목 글자 대신 로고 |
| 거처 배경 (창고·교실) | 허브 가운데 (`CLASSROOM_MOVED_IN`이면 교실) |
| 사건 그림·배경 (약 50개 이벤트) | 이벤트 대화 뒤 배경 |
| 초상화 7명 (주인공·윤 선생님(아플 때 `_ill`)·박 선생님·도현·송예지·최하린·정 반장) | 대화창 왼쪽 |
| 엔딩 원화 16개 | 엔딩 화면 배경(영상 대신), 갤러리 썸네일 |
| 지역 그림 8개 | 출정 준비 카드 |
| 탐험 스프라이트 (주인공·좀비 3종 대기/휴면·조우 인물 5·문·소품 24) | `Resources/Art/ExplorationHD/` (PPU 72 = 코드 그림 18 PPU의 4배 픽셀, 피벗은 코드 그림을 따름) |
| UI 아이콘 | 상단 자원·방어·구조·날씨·계절, 행동 분류 탭, 상태(체력·피로·스트레스·포만·수분·수면 부채·양심·증상), 능력치·숙련, 동료 얼굴, 선생님 얼굴, 탐험 전리품 줄·배낭 칸 |
| 효과음 37 | 버튼·칸 놓기·대화 넘김·행동 성공/실패·습격 경보 / 탐험: 발소리·문(열기·잠김·부수기)·수색·줍기·밀치기·휘두르기·맞히기·다침·쓰러짐·손전등·숨기(숨소리)·쫓김(심장)·좀비 유형별 경계·기절·무리 깨어남·해 질 녘/새벽·탈출 |
| 라디오 음성 4 | DJ 인사·예보·끝인사, 구조 공지 (대사 첫 마디가 맞을 때) |

| 쓰지 않음 / 다시 필요 | 이유 |
|---|---|
| 엔딩 6개 그림 (`HIDDEN_CURE`·`HIDDEN_STAY`·`HIDDEN_TRUTH`·`NARR_CONFRONT`·`TOGETHER`·`EDUCATOR`) | 아직 없음 → 그 엔딩은 색 배경 그대로 |
| `ui_week_start.wav` | 거의 무음(−60 dBFS 잡음뿐). 다시 만들어야 함 |
| 효과음 대부분의 잡음 | 실제 소리 앞뒤로 −60 dBFS 안팎의 잡음이 길게 깔려 있어 잘라 냈다. 다음 생성은 소리만 짧게 |
| 벽·바닥 타일 | 이음매가 맞지 않음, `wall_store`에 사람이 그려짐, `wall_office`는 대부분 투명 → 코드 타일 유지 |
| UI 프레임 (`ui_*`) | 9-slice 여백을 정해 다음에 연결 |
| 캐릭터 시트, 걷기·달리기 프레임 | 대기 그림 하나뿐이라 걷기·추격 프레임 대신 대기 그림을 쓴다 (움직임 애니메이션은 시트가 오면) |
| 시설·지역 아이콘 (`icon_fac_*`, `icon_loc_*`), 선생님 병세 아이콘 | 붙일 자리를 정하지 않음 |
| 영상 `vid_helicopter.mp4` | 엔딩 영상은 원화로 대신하기로 함 |
| 음량 슬라이더 | 설정 화면이 아직 없음 (`Sound.MasterVolume`은 준비됨) |

### 8.2 탐험 픽셀 아트·조명 (2026-11-20, PixelLab 무료 체험 40회 중 34회 사용)

탐험 화면을 한 가지 도트 밀도(**PPU 36, 캐릭터 64px = 1.78유닛**)로 맞췄다. 찾는 순서: `Resources/Art/ExplorationPixel/` → `ExplorationHD/` → 코드 그림.
동작 프레임 수는 파일 수로 센다(`ExplorationArt.FrameCount`) — 걷기 8프레임을 넣으면 8프레임으로 돈다.

| 무엇 | 어떻게 | 생성 횟수 |
|---|---|---|
| 주인공 대기·걷기·뛰기·웅크려 걷기·웅크림·공격(뛰기 한 프레임) | 받은 그림 → `image-to-pixelart`(40x64) → `animate-with-text-v3`(72x72, 8프레임). 뛰기·웅크려 걷기는 첫 프레임 = 끝 프레임으로 다시 만들어 이어 돈다 | 7 (공격 1회 실패) |
| 좀비 3종 대기·걷기·쫓기, 기절은 대기 그림 | 같은 방법. 배회자 걷기는 머리 위 얼룩을 지웠다(가장 큰 덩어리만 남김), 청취자 걷기는 앞뒤로 되감아 14프레임 | 12 |
| 적대 생존자 대기·걷기·뛰기 | 같은 방법 (걷기·뛰기는 움직이기 시작한 뒤 프레임만) | 3 |
| 벽: 복도·교실 바깥·교실 안·사무실·가게 | `create-image-pixen`(128x144) → 평평한 부분을 잘라 좌우가 이어지게 섞음. 약국·화장실은 복도·가게 벽 색을 바꿔 만들었다(생성한 그림은 지저분해서 버림) | 9 (복도 1회 다시) |
| 소품 24·문·조우 인물 5·잠든 좀비 | 무료: ExplorationHD를 2배 줄이고 색을 24개로 줄여 테두리를 어둡게 (`pixel_props.py`). 마트 어른들·사물함 속 간호사·책상 밑 도현은 소품 크기에 맞춰 다시 줄였다 | 0 |

| 주인공 방망이 휘두르기·맨손 밀치기 (각 6프레임, 0.4초) | 대기 그림을 `edit-image-pixen`으로 방망이를 어깨에 멘 자세로 바꾸고, 그 자세를 첫·끝 프레임으로 `animate-with-text-v3`. 밀치기는 대기 자세에서. 무기 종류와 상관없이 방망이로 보인다 | 3 |

도구(저장소 밖): `I:/workshop/code_temp/apogirl/tools/pixellab.py`(API 클라이언트, 키는 `secrets/pixellab.env`), `pixel_props.py`, 작업 스크립트는 `scratch/pl/`.
무료 체험은 **작업 하나씩만** 돌아간다(429면 기다렸다 다시). 남은 체험 생성: 6회.

**조명** (`ExplorationLighting`, 값은 `BalanceConfig` 조명 절 — 그림만, 시야·수색 규칙은 그대로):
- 낮 복도: 화면 앞쪽 창으로 비스듬한 햇빛 줄기 (`SunbeamIntensity` 0.7, 간격 `SunbeamSpacing` 6), 전체 밝기 `DayCorridorAmbient` 0.8.
- 낮 방 안: 전기가 끊겨 어둡다 (`DayRoomAmbient` 0.32), 복도 쪽 창으로 희미한 빛 (`RoomWindowLightIntensity` 0.15, 선형 값), 주인공 주변만 살짝 밝다.
- 문을 지나면 밝기가 0.4초쯤에 걸쳐 바뀐다. 황혼은 붉은 빛줄기, 밤은 옅은 달빛, 새벽은 엷은 빛.

남은 일: 계단·출구 등 소품을 PixelLab으로 다시 그리면 더 선명해진다(지금은 로컬 변환), 바깥 벽돌·땅 타일, 무기별 휘두르기(파이프·대걸레 등)와 무기를 든 채 걷기.

---

### 8.3 탐험 픽셀 아트 전면 교체 (2026-11-20, PixelLab 유료 Tier 1)

탐험에 쓰는 그림을 모두 PixelLab v2 API로 다시 만들었다. 기준은 PPU 36, 사람 키 64px. 폴더는 `Resources/Art/ExplorationPixel/`.

- **무엇을 어디에 쓸지는 데이터**: `Resources/Art/exploration_art.json` (`ExplorationArtMap.cs`). 지역·방 템플릿별 벽(방 안·복도·방 바깥벽), 문·출구, 컨테이너, 조우 물건, 흩어 놓는 장식(바닥·벽, 밀도). 바깥 길 장식은 템플릿 `OUT_ENTRY` / `OUT_ESCAPE`.
- **기준점·9조각**: `ExplorationPixel/sprite_meta.json` (창문 테두리, 가방 끈 자리 등) → `Editor/ArtImportSettings`.
- **주인공**: `player_{옷}_{무기}_{동작}_{0..15}` — 옷 4(uniform·guard·padding·riot) × 무기 5(none·mop·bat·pipe·baton) = 20벌, 동작 5(idle·walk·run·crouch·attack 또는 shove) × 16프레임. 무기는 그림 안에 같이 그려져 있어 팔이 겹치지 않는다. 없는 차림은 `PlayerPrefix`가 교복+그 무기 → 그 옷+맨손 → 교복+맨손 순서로 물러난다.
- **가방**: `bag_{school|hiking|tote}_{brown|blue|pink|red|yellow|green}`. 색은 판(시드)마다 하나. 붙일 자리는 프레임마다 `player_anchors.json`(등 쪽 어깨 위치·기울기).
- **좀비·생존자·조우 인물**: 16프레임 동작(대기·걷기·기절·휴면 등), `SpriteFlipbook`.
- **환경**: 벽 23종(지역·특수 교실·거리·골목), 문 4종(닫힘·열림), 출구 2종, 계단(위·아래·무너짐), 소품 약 320종. 교실 책상은 옆모습, 모니터·TV는 평면형.
- **바깥 길**: 학교 밖 지역은 진입로(거리)에서 시작하고, 건물 반대쪽 출구는 탈출로(골목)로 이어진다. 두 길 모두 학교로 돌아가는 지점이 있다 (`BuildingView.BuildOutdoor`, `BalanceConfig.ApproachRoadWidth`·`EscapeRouteWidth`).
- **무너진 계단**: `ConnectorDef.Collapsed` (`LocationBuilder.Stairs(…, collapsedFrom)`). 학교 오른쪽 계단 2~3층, 정수장 왼쪽 계단 2~3층.
- **지금 층만 보이기**: 위·아래층은 가린다. **빛줄기**는 층마다 1~2곳(`SunbeamsPerFloorMin/Max`), 해의 위치에 따라 기울기와 바닥 빛 자리가 움직인다.
- 만드는 도구는 저장소 밖 `I:\workshop\code_temppogirl	ools\` (gen_env·gen_player·gen_attack·gen_chars → install_env·install_player·install_chars → make_special·bag_colors·make_anchors·fix_idle·make_art_map). API 키는 저장소에 두지 않는다.
- 화면 확인용: `ExpeditionDebug.Start(행동, 슬롯, 무기, 옷, 가방, 도구)`, `.Room(템플릿 글자)`, `ExpeditionDirector.DebugGoTo(층 또는 -1 진입로/-2 탈출로, x)`.

## 9. 예비 에셋 (P4: 앞으로 추가될 수 있는 것)

지금 게임 데이터에는 없지만 개발 방향에 따라 들어올 수 있는 아이템·도구·제작품·설비다.

- **언제 만드나:** P1~P3를 다 만든 뒤, 또는 무제한 기간이 남을 때 아이콘 묶음으로 한꺼번에 뽑아 둔다. 실제로 쓸지는 나중에 기획에서 정한다.
- **아이콘 규칙:** 4.4와 같다. [F-UI] + `transparent background, item icon, three-quarter view`, 1:1, 512px 이상.
- **세트로 요청:** 8~12개씩 묶어 "아래 N개를 같은 스타일의 아이콘 세트로, 하나씩 따로 투명 배경으로"라고 요청하면 그림체가 맞는다.
- **파일 이름:** `item_` + 장차 쓸 아이템 ID(소문자)다. 실제로 추가할 때 아이템 ID를 이 이름에 맞춘다(예: `item_bag_duffel.png` → `ITEM_BAG_DUFFEL`).
- **무기:** 총기는 넣지 않았다. 학교·생활 주변에서 구할 수 있는 둔기·도구 위주다. 날붙이도 깨끗한 상태로 그린다(0.4 안전 문구 그대로).
- **겹치는 그림:** 무기와 도구로 같이 쓰일 수 있는 것(망치·렌치·삽·도끼)은 그림 하나를 같이 써도 된다.

### 9.1 가방 (작은 것부터)

배낭 격자 크기가 다른 가방들이다. 크기가 그림에서 바로 구분되게 만든다.

| 파일 | 설명 | 크기 느낌 |
|---|---|---|
| `item_bag_pouch.png` | `small belt pouch / fanny pack` | 아주 작음 |
| `item_bag_sling.png` | `crossbody sling bag` | 작음 |
| `item_bag_school.png` (있음) | school backpack | 보통 |
| `item_bag_tote.png` (있음) | shopping tote bag | 보통, 꺼내기 쉬움 |
| `item_bag_duffel.png` | `sports duffel bag with a shoulder strap` | 큼, 손에 듦 |
| `item_bag_hiking.png` (있음) | large hiking backpack | 큼 |
| `item_bag_rucksack.png` | `large military-style rucksack with webbing straps` | 아주 큼 |
| `item_bag_frame.png` | `external frame backpack with a sleeping bag and gear tied on` | 최대 |
| `item_bag_suitcase.png` | `hard-shell rolling suitcase` | 큼, 시끄러움 (거점 운반용) |
| `item_cart_shopping.png` | `shopping cart` | 운반 수레 (지역 간 이동용) |
| `item_pouch_med.png` | `small red first aid pouch with velcro` | 확장 칸 |
| `item_vest_utility.png` | `utility vest with many small pockets` | 퀵슬롯 확장 |

> 메고 있는 가방을 탐험 스프라이트에 보여 줄 거라면 2.3 `player_gear_` 방식으로 `idle` 1장씩 만든다: `carrying a small sling bag` / `carrying a duffel bag` / `carrying a huge frame backpack`.

### 9.2 근접 무기·방패·던질 것

| 파일 | 설명 | 성격 |
|---|---|---|
| `item_weapon_umbrella.png` | `long black umbrella, closed` | 가벼움 |
| `item_weapon_broom.png` | `school broom with plastic bristles` | 가벼움 |
| `item_weapon_ruler.png` | `one-meter wooden classroom ruler` | 가벼움, 학교 |
| `item_weapon_hockey.png` | `field hockey stick` | 보통 |
| `item_weapon_golf.png` | `iron golf club` | 보통 |
| `item_weapon_hammer.png` | `claw hammer` | 짧고 강함 (도구 겸용) |
| `item_weapon_wrench.png` | `large red pipe wrench` | 짧고 강함 (도구 겸용) |
| `item_weapon_tire_iron.png` | `cross-shaped tire iron` | 짧고 강함 |
| `item_weapon_rebar.png` | `steel rebar with a taped grip` | 무거움 |
| `item_weapon_spear.png` | `mop handle spear with a taped metal tip` | 김, 거리 유지 |
| `item_weapon_extinguisher.png` | `red fire extinguisher` | 무거움, 분사로 기절 |
| `item_weapon_fire_axe.png` | `red fire axe, clean` | 무거움 (문 부수기 겸용) |
| `item_weapon_sledge.png` | `sledgehammer` | 아주 무거움, 시끄러움 |
| `item_shield_trashlid.png` | `metal trash can lid with a rope handle` | 방패 |
| `item_shield_board.png` | `blank school signboard with handles nailed on the back` | 방패 |
| `item_shield_riot.png` | `clear police riot shield` | 방패 |
| `item_slingshot.png` | `wooden slingshot` | 원거리, 조용함 |
| `item_ammo_marbles.png` | `handful of glass marbles` | 새총 탄 |
| `item_lure_bottle.png` | `empty glass bottle` | 던져서 소리로 유인 |
| `item_lure_alarm.png` | `wind-up twin-bell alarm clock` | 시간 맞춰 울리는 유인 |
| `item_lure_firecracker.png` | `string of red firecrackers` | 큰 소리 유인 |
| `item_lure_phone.png` | `old smartphone with a cracked screen, ringing` | 소리 유인 |

### 9.3 방어구·옷

| 파일 | 설명 | 파일 | 설명 |
|---|---|---|---|
| `item_armor_helmet_bike.png` | bicycle helmet | `item_armor_hardhat.png` | yellow construction hard hat |
| `item_armor_helmet_moto.png` | motorcycle helmet with visor | `item_armor_magazine.png` | thick magazines taped around a forearm |
| `item_armor_leather.png` | leather motorcycle jacket | `item_armor_raincoat.png` | yellow raincoat |
| `item_armor_wintercoat.png` | long padded winter coat | `item_armor_kneepads.png` | knee pads |
| `item_gloves_work.png` | cotton work gloves with rubber palms | `item_gloves_rubber.png` | yellow kitchen rubber gloves |
| `item_boots_rain.png` | rubber rain boots | `item_boots_work.png` | leather work boots |
| `item_mask_dust.png` | white dust mask | `item_mask_gas.png` | gas mask with filter |
| `item_scarf_wool.png` | wool scarf and beanie | | |

### 9.4 음식·물

**보관 식품·재료**

| 파일 | 설명 | 파일 | 설명 |
|---|---|---|---|
| `item_rice_bag.png` | 10 kg sack of rice | `item_instant_rice.png` | microwavable rice bowl, plain package |
| `item_ramen_pack.png` | five-pack of instant ramen, plain wrapper | `item_canned_tuna.png` | canned tuna |
| `item_canned_ham.png` | canned luncheon meat | `item_canned_fruit.png` | canned peaches |
| `item_canned_perilla.png` | small can of seasoned perilla leaves | `item_kimchi_jar.png` | glass jar of kimchi |
| `item_gim.png` | pack of seasoned laver sheets | `item_dried_squid.png` | dried squid |
| `item_crackers.png` | box of crackers | `item_chocolate.png` | chocolate bar |
| `item_candy.png` | bag of hard candies | `item_energy_bar.png` | energy bar |
| `item_honey.png` | jar of honey | `item_coffee_mix.png` | bundle of instant coffee mix sticks |
| `item_tea.png` | box of tea bags | `item_sugar.png` | bag of sugar |
| `item_salt.png` | salt container | `item_cooking_oil.png` | bottle of cooking oil |
| `item_soy_sauce.png` | soy sauce bottle | `item_gochujang.png` | red tub of chili paste |
| `item_flour.png` | bag of flour | `item_tteok.png` | pack of rice cakes |
| `item_ration.png` | military ration pouch | `item_bread.png` | loaf of bread (상함) |
| `item_milk.png` | milk carton (상함) | `item_eggs.png` | egg carton (상함) |
| `item_potato.png` | potatoes | `item_sweet_potato.png` | sweet potatoes |
| `item_cabbage.png` | napa cabbage | `item_lettuce.png` | lettuce |
| `item_tomato.png` | cherry tomatoes | `item_food_spoiled.png` | spoiled food container with a green tint, not gross |

**요리 (요리 숙련도용)**

| 파일 | 설명 | 파일 | 설명 |
|---|---|---|---|
| `item_meal_jumeokbap.png` | rice balls wrapped in seaweed | `item_meal_ramyeon_pot.png` | ramyeon in a small pot with a lid |
| `item_meal_kimchi_stew.png` | kimchi stew in an earthenware pot | `item_meal_army_stew.png` | stew with canned ham and ramen noodles |
| `item_meal_sweet_potato.png` | roasted sweet potatoes in foil | `item_meal_porridge.png` | bowl of rice porridge (환자용) |
| `item_meal_jeon.png` | Korean pancake on a plate | `item_meal_curry.png` | curry rice on a steel tray |

**물·음료**

| 파일 | 설명 | 파일 | 설명 |
|---|---|---|---|
| `item_water_dirty.png` | bottle of murky water | `item_water_rain.png` | bucket of rainwater |
| `item_water_boiled.png` | dented kettle | `item_water_tablet.png` | blister pack of purification tablets |
| `item_juice_box.png` | juice box with straw | `item_canned_coffee.png` | canned coffee |

**텃밭**

| 파일 | 설명 | 파일 | 설명 |
|---|---|---|---|
| `item_seed_pack.png` | generic seed packet (no text) | `item_seed_potato.png` | sprouting seed potatoes |
| `item_fertilizer.png` | small bag of fertilizer | `item_watering_can.png` | watering can |

### 9.5 의약품

삼키는 약은 일반 상비약까지만 넣고, 수면제처럼 13장에 걸릴 수 있는 것은 넣지 않았다.

| 파일 | 설명 | 파일 | 설명 |
|---|---|---|---|
| `item_cold_medicine.png` | cold medicine box | `item_fever_syrup.png` | fever reducer syrup bottle |
| `item_disinfectant.png` | iodine disinfectant bottle | `item_gauze.png` | pack of gauze pads |
| `item_splint.png` | padded splint | `item_burn_ointment.png` | ointment tube |
| `item_eye_drops.png` | eye drop bottle | `item_thermometer.png` | digital thermometer |
| `item_hot_pack.png` | disposable hand warmer (겨울) | `item_ice_pack.png` | instant ice pack (여름) |
| `item_ors.png` | oral rehydration salt sachets | `item_iv_set.png` | IV drip bag and tube, clean |
| `item_crutch.png` | pair of handmade wooden crutches wrapped in tape (선생님 여분) | `item_antiviral.png` | unlabeled pill bottle with a lab tag (연구 단서) |
| `item_vaccine_sample.png` | small vial in a cold case (연구 단서) | `item_blood_pack.png` | `sealed medical transfusion bag with a blank blood-type label, clean, clinical, no stains` (M7 수혈 후보) |

### 9.6 서바이벌·작업 도구

| 파일 | 설명 | 파일 | 설명 |
|---|---|---|---|
| `item_tool_shovel.png` | shovel | `item_tool_folding_shovel.png` | folding camp shovel |
| `item_tool_pickaxe.png` | pickaxe | `item_tool_hatchet.png` | hatchet |
| `item_tool_saw.png` | hand saw | `item_tool_hacksaw.png` | hacksaw |
| `item_tool_screwdriver.png` | screwdriver set | `item_tool_wrench.png` | adjustable wrench |
| `item_tool_pliers.png` | pliers | `item_tool_bolt_cutter.png` | bolt cutter (자물쇠 끊기) |
| `item_tool_multitool.png` | folding multitool | `item_tool_drill.png` | cordless drill |
| `item_tool_lockpick.png` | hairpins and a small tension wrench | `item_tool_rope.png` | coil of rope |
| `item_tool_ladder.png` | folding step ladder | `item_tool_binoculars.png` | binoculars (정찰) |
| `item_tool_compass.png` | compass | `item_tool_map.png` | folded city map, no readable text |
| `item_tool_walkie.png` | pair of walkie-talkies | `item_tool_crank_radio.png` | hand-crank emergency radio |
| `item_tool_headlamp.png` | headlamp | `item_tool_lantern.png` | camping LED lantern |
| `item_tool_candle.png` | candles | `item_tool_matches.png` | box of matches |
| `item_tool_ferro.png` | fire starter rod | `item_tool_butane.png` | butane gas canister |
| `item_tool_jerrycan.png` | jerry can | `item_tool_siphon.png` | siphon hose |
| `item_tool_bucket.png` | plastic bucket | `item_tool_tarp.png` | folded blue tarp |
| `item_tool_sleeping_bag.png` | rolled sleeping bag | `item_tool_blanket.png` | folded wool blanket |
| `item_tool_sewing_kit.png` | sewing kit tin | `item_tool_whistle.png` | whistle |
| `item_tool_fishing_rod.png` | fishing rod | `item_tool_sharpener.png` | sharpening stone |
| `item_tool_solar_charger.png` | foldable solar charger | `item_tool_power_bank.png` | power bank |
| `item_key_ring.png` | bundle of old keys | `item_keycard.png` | blank security keycard (연구동) |

### 9.7 제작 재료

| 파일 | 설명 | 파일 | 설명 |
|---|---|---|---|
| `item_mat_plank.png` | wooden planks | `item_mat_firewood.png` | bundle of firewood |
| `item_mat_nails.png` | box of nails | `item_mat_screws.png` | jar of screws |
| `item_mat_sheet_metal.png` | sheet of corrugated metal | `item_mat_wire.png` | coil of wire |
| `item_mat_cable_ties.png` | bundle of cable ties | `item_mat_cloth.png` | pile of cloth rags |
| `item_mat_pet_bottle.png` | empty plastic bottles | `item_mat_glass_jar.png` | empty glass jars |
| `item_mat_charcoal.png` | charcoal pieces | `item_mat_sand.png` | bag of sand |
| `item_mat_gravel.png` | bag of gravel | `item_mat_pvc_pipe.png` | PVC pipes |
| `item_mat_hose.png` | garden hose | `item_mat_tire.png` | old tire |
| `item_mat_electronics.png` | circuit boards and wires | `item_mat_motor.png` | small electric motor |
| `item_mat_solar_cell.png` | loose solar cells | `item_mat_glue.png` | glue tube |
| `item_mat_vinyl.png` | roll of plastic film | `item_mat_cardboard.png` | flattened cardboard |
| `item_mat_brick.png` | bricks | `item_mat_cement.png` | bag of cement |

### 9.8 제작품·거점 설비

거점에 세울 수 있는 것들이다. 필요한 그림이 두세 가지다.

- **아이콘** `icon_craft_*.png`: [F-UI]. 제작 목록·시설 칸에 쓴다.
- **허브 그림** `hub_obj_*.png`: [F-ILLU]의 `Background:` 문장 + `single object, semi-photorealistic, transparent background, soft shadow`. 허브 배경 위에 겹쳐 지은 설비가 보이게 한다.
- **탐험 소품** `prop_*.png`: 필요할 때만, 같은 설명에 [F-PIXEL]로 만든다.

| 파일 이름 (공통 뒷부분) | 설비 | 프롬프트 |
|---|---|---|
| `water_filter` | 페트병 정수 필터 | `upside-down cut plastic bottle water filter with visible layers of gravel, sand, charcoal and cloth, clear water dripping into a glass jar below` |
| `rain_collector` | 빗물 집수기 | `tarp funnel draining into a blue plastic barrel with a tap` |
| `boil_station` | 끓임 정수대 | `large pot of water on a camping stove with a stack of clean bottles beside it` |
| `workbench` | 작업대 | `sturdy workbench made of two school desks and a door board, a vise, scattered tools, pegboard behind` |
| `stove_drum` | 드럼통 난로 | `wood-burning stove made from an oil drum with a chimney pipe going out a window, warm glow` |
| `stove_rocket` | 깡통 로켓 스토브 | `small rocket stove made from tin cans and bricks, a pot on top` |
| `cooking_station` | 조리대 | `camping gas stove on a table with a pot, ladle and spice jars` |
| `solar_panel` | 태양광 패널 | `small solar panel leaning on the rooftop, cables running to a car battery` |
| `bike_generator` | 자전거 발전기 | `stationary bicycle connected to a small generator with wires` |
| `barricade_desks` | 책상 바리케이드 | `stacked school desks and chairs tied with rope blocking a corridor` |
| `barricade_reinforced` | 강화 바리케이드 | `wooden planks and metal sheets nailed over a doorway` |
| `alarm_cans` | 깡통 경보줄 | `string of empty tin cans hung across a hallway as a noise alarm` |
| `gate_barrier` | 정문 차단 | `school gate blocked by shopping carts, desks and a tarp` |
| `watchtower` | 옥상 망루 | `lookout post on the rooftop with a chair, binoculars and a beach umbrella` |
| `radio_antenna` | 무전 안테나 | `homemade radio antenna mast on the rooftop with wires` |
| `greenhouse` | 비닐 온실 | `small plastic-film greenhouse frame with seedlings` |
| `garden_buckets` | 양동이 텃밭 | `vegetables growing in buckets and cut water bottles` |
| `compost_bin` | 퇴비통 | `wooden compost bin with leaves` |
| `drying_rack` | 건조대 | `mesh drying rack with sliced vegetables and fruit in the sun` |
| `storage_jars` | 장독대 | `row of brown earthenware jars for kimchi storage` |
| `bunk_bed` | 이층 침대 | `bunk bed built from planks with gym mats as mattresses` |
| `shower_bucket` | 양동이 샤워 | `bucket shower hanging behind a shower curtain, no person` |
| `first_aid_station` | 응급 처치대 | `nurse office bed with medical supplies neatly arranged on a cart` |
| `heated_mat` | 온열 자리 | `makeshift warm corner with blankets, hot water bottles and a small heater` |
| `study_corner` | 공부 자리 | `desk with a candle, textbooks and a notebook, a small chalkboard on the wall` |

### 9.9 상태 아이콘 후보

| 파일 | 설명 | 파일 | 설명 |
|---|---|---|---|
| `icon_status_wet.png` | dripping umbrella | `icon_status_cold.png` | shivering snowflake |
| `icon_status_hot.png` | sun with a sweat drop | `icon_status_food_poison.png` | queasy swirl over a bowl |
| `icon_status_well_fed.png` | full rice bowl | `icon_status_rested.png` | pillow with a small star |
| `icon_status_inspired.png` | lit candle | `icon_status_lonely.png` | single empty chair |
| `icon_status_arm_hurt.png` | arm sling | `icon_status_leg_hurt.png` | single crutch |
| `icon_status_sick.png` | tissue box | `icon_status_morale.png` | small flag raised |

### 9.10 함께 필요할 소리 (`seed_audio`)

5장과 같이 효과음은 소리 하나만, `no music, no voice`를 붙인다.

| 파일 | 길이 | 프롬프트 |
|---|---|---|
| `sfx_craft_hammer.wav` | 1.5s | `hammering nails into wood, three hits` |
| `sfx_craft_saw.wav` | 2s | `hand saw cutting a wooden plank` |
| `sfx_craft_drill.wav` | 1.5s | `cordless drill driving a screw` |
| `sfx_dig_shovel.wav` | 1.5s | `shovel digging into soil` |
| `sfx_pickaxe.wav` | 1s | `pickaxe striking concrete, small debris` |
| `sfx_axe_chop.wav` | 1s | `hatchet chopping firewood` |
| `sfx_bolt_cut.wav` | 1s | `bolt cutter snapping a padlock chain` |
| `sfx_can_open.wav` | 1s | `pull-tab food can opening` |
| `sfx_eat.wav` | 1.5s | `quiet eating with chopsticks from a metal bowl` |
| `sfx_drink.wav` | 1s | `drinking water from a plastic bottle` |
| `sfx_stove_light.wav` | 1.5s | `match strike then a small fire catching` |
| `amb_stove_crackle.wav` | 20s, 반복 | `wood stove crackling softly, loopable` |
| `amb_filter_drip.wav` | 10s, 반복 | `slow water drips into a glass jar, loopable` |
| `amb_tarp_rain.wav` | 20s, 반복 | `rain falling on a plastic tarp, loopable` |
| `amb_bike_generator.wav` | 10s, 반복 | `bicycle pedaling with a small generator whirring, loopable` |
| `sfx_bottle_break.wav` | 1.5s | `glass bottle thrown and shattering on concrete in the distance` |
| `sfx_alarm_clock.wav` | 3s | `old twin-bell alarm clock ringing in an empty room` |
| `sfx_firecracker.wav` | 3s | `string of firecrackers popping in the distance` |
| `sfx_cans_alarm.wav` | 1.5s | `string of tin cans rattling in a dark hallway` |
| `sfx_walkie.wav` | 1s | `walkie-talkie squelch beep` |
| `sfx_radio_crank.wav` | 2s | `hand-crank radio being wound` |
| `sfx_extinguisher.wav` | 2s | `fire extinguisher spraying a burst of powder` |
| `sfx_slingshot.wav` | 0.6s | `slingshot rubber snap and a marble whizz` |
| `sfx_sew.wav` | 2s | `needle and thread pulling through thick fabric` |
| `sfx_watering.wav` | 2s | `watering can pouring onto soil` |
| `sfx_bag_swap.wav` | 1s | `heavy backpack set down, straps rustling` |
