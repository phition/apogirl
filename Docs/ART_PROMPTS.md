# 이미지 생성 프롬프트 모음

〈마지막 학기〉에 지금 필요한 그림(타이틀, 캐릭터, 허브 배경, 지역 카드, 이벤트 컷, 엔딩 일러스트)의 생성 프롬프트다.
직접 생성한 뒤 아래 "파일 이름"대로 저장해 `ArtInbox/` 폴더(프로젝트 루트)에 넣어 주면 게임에 연결한다.

- 탐험 화면은 코드로 만든 픽셀 아트를 그대로 쓴다. 여기 있는 것은 **허브·대화·이벤트·엔딩용 일러스트**다(문서 2장 "2D 일러스트 배경 + 캐릭터 일러스트").
- 우선순위: **P1**(먼저: 화풍과 캐릭터 기준), **P2**(지금 게임에 바로 붙는 것), **P3**(M6 2~4막용, 나중에 해도 됨).

---

## 0. 공통 규칙

### 0.1 화풍 문구 (모든 프롬프트 앞에 붙인다)

```
Style: Korean indie visual novel illustration, soft painterly 2D, muted low-saturation palette,
quiet post-apocalyptic school, gentle diffused light, subtle film grain, clean line art,
melancholic but warm mood, no text, no watermark.
```

계절이 있는 그림은 끝에 하나를 붙인다.

| 계절 (주차) | 붙일 문구 |
|---|---|
| 봄 (1~13) | `Season: early spring, pale green and soft beige tones, cold morning light.` |
| 여름 (14~26) | `Season: humid summer, dull murky greens, heavy overcast light.` |
| 가을 (27~39) | `Season: late autumn, faded orange and brown tones, low golden light.` |
| 겨울 (40~52) | `Season: winter, blue-grey tones, snow, pale cold light.` |

### 0.2 공통 제외 문구 (지원되는 모델이면 negative prompt에, 아니면 프롬프트 끝에 "Avoid: …"로)

```
gore, blood splatter, exposed wounds, visible amputation, corpses in detail, horror jump-scare face,
sexualized pose, revealing clothing, fan service, text, logo, watermark, signature, extra fingers,
oversaturated colors, photorealistic photo
```

> **안전 규칙 (문서 13장)**
> - 주인공과 학생들은 미성년이다. 의상은 단정한 교복과 체육복이고, 어떤 성적 뉘앙스도 넣지 않는다.
> - 좀비는 멀리 있는 실루엣이나 흐릿한 형체로만 그리고, 고어 묘사는 하지 않는다.
> - 선생님의 다리는 바지와 붕대, 목발로만 표현한다. 절단 부위는 보이지 않게 한다.

### 0.3 크기와 형식

| 종류 | 비율 | 권장 크기 | 배경 |
|---|---|---|---|
| 배경·엔딩·이벤트 컷 | 16:9 | 1920×1080 이상 | 있음 |
| 지역 카드 | 3:2 | 1200×800 이상 | 있음 |
| 캐릭터 기준 시트 | 16:9 | 2k | 단색 회색 |
| 캐릭터 표정 (상반신) | 3:4 | 1024×1365 이상 | **투명** (안 되면 단색 회색 → 내가 배경 제거) |

### 0.4 캐릭터 일관성

1. 각 캐릭터의 **기준 시트(1장)**를 먼저 만든다.
2. 마음에 드는 시트를 **참고 이미지(reference)**로 넣고 표정과 장면을 만든다.
   - Nano Banana·GPT Image·Seedream은 참고 이미지를 받는다.
3. 장면 그림에 인물이 들어가면 기준 시트를 참고 이미지로 함께 넣는다.

---

## 1. 캐릭터 (P1 기준 시트 → P2 표정)

### 1.1 한서진 (주인공, 고1 여학생, 16세) — 외모 고정

설정: 조용하고 눈에 띄지 않는 아이. 관찰력이 있고 참을성이 강하다. 탐험 화면 스프라이트와 맞춘다.
- 어깨까지 오는 검은 머리
- 남색 교복 재킷, 남색 체크 치마, 갈색 책가방

**기준 시트** — 파일 `char_seojin_sheet.png`
```
[화풍 문구]
Character reference sheet of a 16-year-old Korean high school girl, first-year student,
shoulder-length straight black hair, plain quiet face, tired but observant eyes,
modest navy school blazer, white shirt, navy tie, knee-length navy plaid skirt, black tights, worn sneakers,
brown school backpack, slightly dusty and wrinkled uniform from days of survival,
front view, side view and back view, full body, neutral standing pose, plain grey background.
```

**표정 6종** — 상반신, 정면에서 살짝 비스듬히, 기준 시트를 참고 이미지로 넣는다. 파일 `char_seojin_{표정}.png`

| 표정 | 프롬프트 끝에 붙일 문구 |
|---|---|
| `neutral` | `calm neutral expression, lips closed` |
| `smile` | `small shy smile, eyes softened` |
| `sad` | `sad expression, eyes lowered, holding back tears` |
| `worried` | `worried expression, brows knitted, hand clutching backpack strap` |
| `surprised` | `surprised expression, eyes wide, mouth slightly open` |
| `determined` | `determined expression, firm gaze forward, jaw set` |

공통 앞부분:
```
[화풍 문구]
Bust-up portrait of the same girl from the reference sheet (16-year-old Korean high school girl,
shoulder-length black hair, navy blazer, white shirt, navy tie), visual novel sprite,
transparent background, soft rim light, [표정 문구]
```

### 1.2 윤 선생님 (국어 교사, 멘토) — **[결정 필요] 성별·나이**

문서에는 국어 교사라는 것과, 무릎 아래를 스스로 잘라내 목발을 짚는다는 것만 정해져 있다. 성별과 나이는 정해지지 않았다.

| 안 | 내용 | 느낌 |
|---|---|---|
| **A (추천)** | 30대 후반 여성 | 엄마도 언니도 아닌 '어른'. 엄격하지만 다정한 담임 같은 거리감 |
| B | 40대 중반 남성 | 프린세스 메이커 집사의 전형. 무뚝뚝한 보호자 |
| C | 50대 여성 | 정년이 가까운 베테랑. 더 차분하고 지친 어른 |

**기준 시트** — 파일 `char_teacher_sheet.png` (A안 기준. B·C안이면 첫 줄만 바꾼다)
```
[화풍 문구]
Character reference sheet of a Korean high school literature teacher, woman in her late 30s,
short practical dark hair tied back, thin-framed glasses, kind but exhausted face, faint scar on the cheek,
oversized grey cardigan over a white blouse, long dark trousers, one trouser leg folded and pinned below the knee,
the leg is not visible, wooden crutches made from a mop handle and tape, bandaged hands,
front view and side view, full body, standing with crutches, plain grey background.
```
- B안 첫 줄: `Korean high school literature teacher, man in his mid 40s, short greying hair, stubble,`
- C안 첫 줄: `Korean high school literature teacher, woman in her early 50s, greying bob haircut,`

**표정 7종** — 파일 `char_teacher_{표정}.png`. 앞부분은 1.1과 같은 방식이고, 선생님 설명으로 바꾼다.

| 표정 | 문구 |
|---|---|
| `neutral` | `calm attentive expression` |
| `smile` | `gentle warm smile` |
| `stern` | `stern teacher expression, looking over glasses` |
| `sad` | `sad, eyes downcast` |
| `worried` | `worried, brows knitted` |
| `laugh` | `laughing softly, eyes closed` |
| `ill` (3막 이후, P3) | `pale, sweating, feverish, tired eyes, cardigan pulled tight, still trying to smile` |

### 1.3 도현 (인근 중학교 3학년 남학생, 첫 NPC)

**기준 시트** — 파일 `char_dohyeon_sheet.png`
```
[화풍 문구]
Character reference sheet of a 15-year-old Korean middle school boy, short messy black hair, thin,
alert nervous eyes, oversized dark green middle school tracksuit jacket over a t-shirt, jeans,
a small cut taped on the forehead, holding a flashlight,
front view and side view, full body, plain grey background.
```
**표정 5종** — `neutral`, `smile`, `scared`, `angry`, `determined`. 파일 `char_dohyeon_{표정}.png`

### 1.4 친구 (같은 중학교 출신 단짝, 기본 이름 "지우") — 외모 고정, P2

설정: 고등학교에서는 다른 반이다. 괴롭힘을 알았지만 무서워서 나서지 못했다. 악역이 아닌 현실적인 아이로 그린다.
```
[화풍 문구]
Character reference sheet of a 16-year-old Korean high school girl, cheerful round face,
medium-length brown-black hair in a low ponytail, same navy school uniform with a hoodie underneath,
a cracked smartphone in hand, canvas tote bag, warm but guilty eyes,
front view and side view, full body, plain grey background.
```
파일 `char_friend_sheet.png`. 표정 4종 `neutral`, `smile`, `cry`(울면서 웃음), `sorry`도 같은 방식이다.

### 1.5 체육복 학생 (1막 생존자 A) — P2, 표정 1장

```
[화풍 문구]
Bust-up portrait of a 17-year-old Korean high school student in a faded blue school gym uniform,
short hair, wary eyes, clutching a bag of instant noodles, transparent background, neutral guarded expression.
```
파일 `char_survivor_a_neutral.png`

### 1.6 M6 새 인물 (P3, 설계 승인 뒤 확정)

M6 결정 항목 b·c가 정해지면 이 표의 프롬프트를 확정한다. 지금은 초안이다.

| 인물 | 초안 |
|---|---|
| 박 간호사 (보건 선생님) | 40대 여성, 단정한 단발, 보건실 가운 위에 패딩 조끼, 지친 눈과 단호한 입매 |
| 가해 학생 (주동자) | 같은 중학교 출신 여학생, 날카로운 눈매, 교복을 흐트러지게 입음. 악마화하지 않고 평범한 얼굴 |
| 가해 학생 (방관자) | 같은 무리의 조용한 아이, 시선을 피하는 얼굴 |
| 라디오 DJ | 얼굴 없이 목소리만 (그림 불필요) |
| 유랑자 | 나이 불명의 성인, 낡은 등산복, 얼굴은 모자 그림자에 가림 |

---

## 2. 타이틀 키 아트 (P1, 화풍 기준으로도 쓴다) — `title_key.png`, 16:9

```
[화풍 문구] [봄 문구]
Key art: a lone Korean high school girl in a navy uniform standing on the school rooftop at dawn,
seen from behind, looking over a silent empty city, a hand-painted SOS sign on the rooftop floor,
laundry line with a few clothes, potted vegetables in buckets, distant birds,
the city below is quiet and overgrown, faint silhouettes of wandering figures far away in the streets,
wide cinematic composition, lots of sky on the upper third for the title.
```

---

## 3. 허브 배경 (거점 시설) — 16:9, 인물 없음

허브 화면 가운데 배경으로 쓴다. **사람이 없는** 장면이어야 그 위에 캐릭터를 올릴 수 있다.

| 우선 | 파일 | 시설 | 프롬프트 (화풍 문구 + 봄 문구 뒤에) |
|---|---|---|---|
| P2 | `bg_storage.png` | 본관 창고 (시작점) | `Interior of a cramped school storage room turned shelter, metal shelves with cardboard boxes, stacked bottled water and sports drink crates, a sleeping bag and a camping lamp on the floor, a single high window with dusty light, a chalk tally of days on the wall.` |
| P2 | `bg_classroom.png` | 교실동 (거점 중심) | `Interior of a Korean high school classroom converted into a living space, desks pushed together as a table, mattresses from the nurse office, a chalkboard with a weekly plan written in chalk, barricaded door with desks, curtains half drawn, candles and a camping stove, warm small comfort in a ruined place.` |
| P2 | `bg_infirmary.png` | 보건실 | `Interior of a school nurse office, two beds with white curtains, medicine cabinet mostly empty, bandages and labeled jars, a height chart on the wall, soft window light.` |
| P2 | `bg_rooftop.png` | 옥상 | `School rooftop garden, vegetables growing in buckets and cut water bottles, rainwater collection barrels, a large SOS painted on the floor, laundry line, a lawn chair, view of the quiet city.` |
| P2 | `bg_broadcast.png` | 방송실 | `School broadcasting room, old mixing console, microphones, a repaired radio with exposed wires, notes and frequencies taped on the wall, a small window to the corridor.` |
| P3 | `bg_cafeteria.png` | 급식실 | `School cafeteria kitchen, large steel pots, stacked trays, a cleared serving counter used as a pantry, canned food sorted by date.` |
| P3 | `bg_library.png` | 도서관 | `School library, tall bookshelves, a reading table with a lamp and open textbooks, quiet dust in sunbeams, a hand-drawn map of the town pinned to a shelf.` |
| P3 | `bg_science.png` | 과학실 | `School science lab, lab benches, a homemade water filtration setup with bottles and charcoal, glassware, a periodic table poster faded.` |
| P3 | `bg_gym.png` | 체육관 | `School gymnasium, basketball court with mats laid out as a training area, punching bag made from a sandbag, high windows, echoing empty space.` |
| P3 | `bg_staffroom.png` | 교무실 | `Teachers office, rows of desks with old monitors, filing cabinets, a whiteboard with a school year calendar, student record folders stacked on a desk.` |

**계절 변형 (P3):** `bg_classroom` 하나만 여름·가을·겨울 문구로 3장 더 만든다.
- 파일 `bg_classroom_summer.png`, `bg_classroom_autumn.png`, `bg_classroom_winter.png`
- 겨울에는 `window frosted, blankets and a small heater` 를 덧붙인다.

---

## 4. 지역 카드 (출정 준비 화면) — 3:2, 건물 외관, 인물 없음

| 우선 | 파일 | 지역 | 프롬프트 (화풍 문구 뒤에) |
|---|---|---|---|
| P2 | `loc_school.png` | 새벽고 교실동 | `Exterior of a Korean high school building, four floors, broken windows on some floors, school name sign blank, empty schoolyard with a fallen soccer goal, overgrown flower beds.` |
| P2 | `loc_store.png` | 학교 앞 편의점 | `Small Korean convenience store across from a school gate, shutter half open, faded sign without readable text, scattered flyers, a toppled ice cream freezer outside.` |
| P2 | `loc_pharmacy.png` | 새벽약국 · 연세의원 | `Two-story corner building with a pharmacy on the ground floor and a small clinic upstairs, green cross sign unlit, frosted glass door, waiting room chairs visible through the window, silhouettes slumped and still inside.` |
| P3 | `loc_residential.png` | 주택가 | `Quiet Korean residential alley with multi-family villas, a locked blue gate with canned food left in front of it, laundry still hanging on balconies.` |
| P3 | `loc_subway.png` | 지하철역 | `Entrance stairs to an underground subway station, flickering emergency light, posters peeling, darkness below, abandoned turnstiles.` |
| P3 | `loc_mart.png` | 마트·물류창고 | `Large discount mart and logistics warehouse, empty parking lot with shopping carts, loading docks with half-open shutters.` |
| P3 | `loc_police.png` | 파출소 | `Small Korean police box, patrol car with flat tires, barricade tape, a door with a handwritten warning note (no readable text).` |
| P3 | `loc_waterworks.png` | 강변·정수장 | `Riverside water treatment plant, concrete settling tanks, a research annex building with dark windows, reeds and mist over the river.` |

밤 변형이 필요하면 같은 프롬프트에 `at night, moonlight, no electric lights, deep shadows` 를 붙여 `_night`로 저장한다(P3).

---

## 5. 이벤트 컷 (1막 주요 장면) — 16:9, P2

대화 중 중요한 순간에 잠깐 띄우는 그림이다. 인물이 나오면 기준 시트를 참고 이미지로 넣는다.

| 파일 | 장면 | 프롬프트 (화풍 문구 + 봄 문구 뒤에) |
|---|---|---|
| `evt_door_opens.png` | 창고 문이 열리고 선생님을 만남 (0막 끝) | `View from inside a dark storage room as the door opens, a bright rectangle of light, the silhouette of a teacher on the floor holding the door with one hand and crutches beside them, dust in the light, the girl's hand raised against the glare in the foreground.` |
| `evt_first_lesson.png` | 첫 수업 | `Classroom by candlelight, the teacher sitting on a desk with crutches leaning beside, pointing at the chalkboard, the girl taking notes at a desk, warm and quiet.` |
| `evt_crutch.png` | 목발 완성 | `The teacher standing up for the first time with a handmade crutch, the girl steadying them, both smiling awkwardly, corridor with morning light.` |
| `evt_power.png` | 전력 복구 | `Corridor lights flickering on one by one in a dark school, the girl and the teacher looking up from the stairwell, a car battery and cables on the floor.` |
| `evt_messages.png` | 친구의 메시지 7일 치 | `Close-up of a cracked smartphone screen glowing in the dark, the girl's hands holding it, many unread message bubbles shown as blurred shapes without readable text, tears dropping on the screen.` |
| `evt_dohyeon.png` | 도현 구조 | `A frightened middle school boy hiding under a teacher's desk in a dark office, the girl crouching and offering her hand, flashlight beam.` |
| `evt_night_school.png` | 처음 걷는 밤의 학교 | `Dark school corridor at night seen from the girl's back, only a small flashlight circle, long hallway, distant shapes at the far end barely visible.` |
| `evt_act1_raid.png` | 1막 마무리 대습격 | `Night, the girl and the teacher bracing a barricaded classroom door with desks and chairs, many shadows pressing against the frosted glass, no gore, tension and resolve.` |

---

## 6. 엔딩 일러스트 — 16:9, 21장

엔딩 첫 화면 뒤에 깔린다. **얼굴을 크게 그리지 않는 구도**(뒷모습, 먼 거리, 손, 사물)를 권한다. 그래야 캐릭터가 달라 보이는 문제가 적다.
파일 이름은 엔딩 ID 그대로 쓴다(예: `ENDING_DOCTOR.png`). 배경색은 게임 데이터의 엔딩 색과 맞추면 좋다.

### 6.1 배드 엔딩 (P2)

| 파일 | 제목 | 프롬프트 (화풍 문구 뒤에) |
|---|---|---|
| `ENDING_BAD_EXPEDITION.png` | 돌아오지 못한 길 | `A school window at night, the teacher's silhouette waiting with a silent walkie-talkie on the sill, the school gate far below empty, cold blue light.` |
| `ENDING_BAD_RAID.png` | 무너진 바리케이드 | `Morning after: a classroom door with a broken barricade of desks, scattered chairs, a fallen backpack, soft daylight, very quiet, no people, no blood.` |
| `ENDING_BAD_STARVATION.png` | 빈 찬장 | `An empty pantry shelf with one opened can split in two, two figures seen from behind sitting side by side by a window, pale light.` |
| `ENDING_BAD_COLLAPSE.png` | 멈춰 버린 시계 | `A closed classroom door with warm light underneath, a book with a bookmark placed at the door, a figure's hand knocking gently from the corridor side, hopeful soft light.` |
| `ENDING_BAD_WOUNDS.png` | 닫힌 보건실 | `Nurse office at night, a bed with the curtain half drawn, a teacher's hand holding a wet towel and an open poetry book, a chalkboard in the background still showing next week's plan.` |

### 6.2 구조 엔딩 (P2: 지금 도달 가능 / P3: 2~4막 플래그 필요)

| 우선 | 파일 | 제목 | 프롬프트 (화풍 문구 + 겨울→봄 사이 이른 봄 문구 뒤에) |
|---|---|---|---|
| P2 | `ENDING_SURVIVOR.png` | 옥상의 아침 | `A rescue helicopter descending onto the school rooftop at sunrise, the girl seen from behind waving both arms beside the painted SOS, wind blowing her hair.` |
| P2 | `ENDING_BALANCED.png` | 무엇이든 해낸 사람 | `Five years later: a young woman in practical work clothes in a rebuilding camp, carrying a toolbox, a first-aid kit on her belt, people waving at her, tents and repaired buildings.` |
| P2 | `ENDING_DOCTOR.png` | 흰 가운 | `Ten years later: a young doctor in a white coat seen from behind in a rebuilt hospital corridor, holding a clipboard, morning light through the windows.` |
| P2 | `ENDING_ENGINEER.png` | 불을 켜는 사람 | `A city street at dusk where the first traffic light turns on, a young engineer with a hard hat looking up, people cheering in the distance, a small crutch sketch on her blueprint.` |
| P2 | `ENDING_GUARD.png` | 문 앞을 지키는 사람 | `A young guard captain standing at an open camp gate at dawn, practical jacket, seen in profile, new recruits lined up, calm and steady.` |
| P2 | `ENDING_LEADER.png` | 모두의 이름을 부르는 사람 | `A young woman speaking from a small stage made of school desks to a crowd of survivors in a schoolyard, seen from behind the crowd, hopeful light.` |
| P2 | `ENDING_WRITER.png` | 1년의 기록 | `A worn diary beside a newly printed book on a desk, the book cover plain, a window with spring blossoms, a hand resting on the diary.` |
| P2 | `ENDING_LONE.png` | 혼자 걸은 길 | `A small isolated house in the countryside with a vegetable garden, a woman seen from far away tending plants, a stack of letters on the porch.` |
| P2 | `ENDING_NARR_OVERCOME.png` | 열린 문 | `A school corridor of a new school, sunlight, a young girl turning back with a calm smile as someone calls her, open doors along the hallway.` |
| P2 | `ENDING_NARR_UNHEALED.png` | 아직 닫힌 문 | `A counseling room door with soft light, a young woman's hand about to knock, quiet hallway, a small potted plant beside the door.` |
| P3 | `ENDING_EDUCATOR.png` | 교단에 선 사람 | `A makeshift classroom in a tent, a young teacher holding an attendance book, seven children at mismatched desks, chalkboard on an easel.` |
| P3 | `ENDING_TOGETHER.png` | 함께한 사람들 | `A group of survivors of different ages gathered on a school rooftop at sunrise, seen from behind, arms around shoulders.` |
| P3 | `ENDING_NARR_CONFRONT.png` | 마주한 얼굴 | `Two teenage girls standing apart in an empty schoolyard, facing each other at a distance, neither aggressive, wind and fallen leaves between them, two helicopters far away in different directions.` |
| P3 | `ENDING_HIDDEN_TRUTH.png` | 진실 | `A notebook full of handwritten notes, clippings and a hand-drawn diagram connecting the school, a water plant and a calendar, on a desk under a lamp, a researcher's hand turning a page.` |
| P3 | `ENDING_HIDDEN_STAY.png` | 구조를 거부한다 | `A rescue helicopter leaving the school rooftop, seen from the rooftop, two figures sitting together on the rooftop edge watching it go, one with crutches, dusk.` |
| P3 | `ENDING_HIDDEN_CURE.png` | 마지막 수업 이후 | `Inside a rescue helicopter, three figures sitting side by side seen from behind, a pair of crutches between them, the school getting smaller through the window, warm light.` |

---

## 7. 넣는 방법

1. 이 문서의 파일 이름 그대로 저장한다(PNG 권장).
2. 프로젝트 루트의 `ArtInbox/` 폴더에 넣는다. 하위 폴더 없이 한곳에 넣어도 된다.
3. 알려 주면 내가 크기·배경 처리를 하고 `Assets/_Project/Art/Illustrations/`로 옮겨 게임에 연결한다.
   - 허브 배경, 대화 초상화, 이벤트 컷, 엔딩 배경, 갤러리 썸네일에 연결한다.
4. 생성 서비스의 상업 이용 조건(유료 요금제 여부)은 출시 전에 한 번 확인한다. 스팀은 AI 생성 콘텐츠를 상점 페이지에 공개하도록 요구한다.

## 8. 체크리스트

- [ ] P1: `title_key`, `char_seojin_sheet`, `char_teacher_sheet`(성별·나이 결정 후), `char_dohyeon_sheet`
- [ ] P2 캐릭터: 서진 표정 6, 선생님 표정 6, 도현 표정 5, 친구 시트+표정 4, 체육복 학생 1
- [ ] P2 배경: 허브 5, 지역 카드 3, 이벤트 컷 8
- [ ] P2 엔딩: 배드 5, 구조 10
- [ ] P3: 새 시설 5, 교실 계절 3, 새 지역 5(+밤), 선생님 `ill`, M6 새 인물, 엔딩 6
