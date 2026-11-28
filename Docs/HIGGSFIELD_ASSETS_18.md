# 힉스필드 에셋 18차: 새 엔딩 그림 28장 (+ 막별 그림 4장) (2026-11-28)

엔딩 확장([PROPOSAL_ENDINGS_EXPANSION.md](PROPOSAL_ENDINGS_EXPANSION.md) P1~P3)으로 엔딩이 22종에서 54종이 되었다. 새 엔딩 27종은 그림이 없어서 지금은 배경색만 깔린다. 이 문서는 그 그림을 받기 위한 프롬프트다(기획서 11절 P5, 결정 E7-A).

- **받을 그림: 28장** — 새 미래 엔딩 26종 + 회고 축 「무게를 든 채로」 + 아직 넣지 않은 「매일 물을 긷는 사람」(엔딩은 스트레스 균형 뒤에 넣는다 — 그림은 미리 받아 둔다).
- **더 받으면 좋은 그림: 4장** (5절) — 세 막 엔딩의 새 구조 장면 3종과 기본 회고 축. 지금 게임은 엔딩 그림을 한 장만 깔아서(대표 엔딩 = 막 Ⅲ) 이 넉 장은 **막마다 다른 배경을 넣은 뒤에야** 쓰인다.
- **작업 제출은 사용자가 한다.** 나(Claude)는 문서만 쓴다.

쓰는 법:

- 공통 공식은 [HIGGSFIELD_ASSETS.md](HIGGSFIELD_ASSETS.md) 0.3·0.4를 따른다(`[F-ILLU]`·`[F-KOREA]`·`[안전]`).
- 구도 원칙은 [ART_PROMPTS.md](ART_PROMPTS.md) 6장과 같다: **얼굴을 크게 그리지 않는다**(뒷모습, 먼 거리, 손, 사물). 엔딩 그림은 글 뒤에 깔리고, 10년 뒤의 어른이 된 주인공이라 얼굴이 크면 다른 사람처럼 보인다.
- 모두 `gpt_image_2_6`, 16:9, 2k 이상이다.
- 프롬프트 = `[F-ILLU]` + (실외나 창밖이 보이면 `[F-KOREA]`) + `[어른]` + 장면 + `[재건]` + `[안전]`
- **참고 이미지**: `char_seojin_sheet.png`를 늘 넣는다(머리 모양과 얼굴형을 잇기 위해). 다른 인물이 나오는 컷은 표의 "참고 시트" 칸에 적은 시트를 더 넣는다.
- 넣는 곳: `ArtInbox/illustrations/endings18/`, 파일 이름은 표의 이름 그대로(`ENDING_*.png`).
- 잇는 곳: 받으면 16:9 · 너비 1920 jpg로 바꿔 `Resources/Art/Story/Endings/{엔딩 ID}.jpg`에 넣는다. 파일 이름이 엔딩 ID와 같으면 데이터를 고치지 않아도 그 엔딩에 깔린다(`EndingResult.ArtKey`). 갤러리 썸네일도 같은 그림을 쓴다.

---

## 0. 한눈에 보기

### 0.1 받을 그림 28장

| 번호 | 파일 | 제목 | 그 뒤의 길 | 묶음 | 우선 |
|---|---|---|---|---|---|
| 2.1 | `ENDING_PARAMEDIC` | 가장 먼저 닿는 손 | 응급구조사 | 직업 | **P1** |
| 2.2 | `ENDING_SCHOOL_NURSE` | 보건실을 지키는 사람 | 보건 교사 | 직업 | **P1** |
| 2.3 | `ENDING_CHEF` | 한 그릇의 온기 | 요리사 | 직업 | **P1** |
| 2.4 | `ENDING_FARMER` | 씨앗을 심는 사람 | 농업 기술자 | 직업 | **P1** |
| 2.5 | `ENDING_WATER_SCIENTIST` | 맑은 물을 만드는 사람 | 수질 연구원 | 직업 | **P1** |
| 2.6 | `ENDING_MUSICIAN` | 그 겨울의 노래 | 음악가 | 직업 | **P1** |
| 2.7 | `ENDING_COUNSELOR` | 문을 두드려 주는 사람 | 상담사 | 직업 | **P1** |
| 2.8 | `ENDING_NEGOTIATOR` | 사거리의 협상가 | 중재관 | 직업 | P2 |
| 2.9 | `ENDING_ATHLETE` | 다시 달리는 트랙 | 육상 선수 | 직업 | **P1** |
| 2.10 | `ENDING_JOURNALIST` | 기록하는 눈 | 기자 | 직업 | P2 |
| 2.11 | `ENDING_RADIO_HOST` | 새벽의 목소리 | 라디오 진행자 | 직업 | P2 |
| 2.12 | `ENDING_ARCHITECT` | 다시 짓는 사람 | 건축가 | 직업 | P2 |
| 2.13 | `ENDING_SCHOLAR` | 다시 펼친 교과서 | 연구자 | 직업 | P3 |
| 2.14 | `ENDING_NETWORK` | 끊긴 선을 잇는 사람 | 통신 기술자 | 직업 | P3 |
| 3.1 | `ENDING_REHAB` | 선생님의 걸음 | 재활 치료사 | 기록 | **P1** |
| 3.2 | `ENDING_RECORD_SHOP` | 여덟 장의 CD | 음반 가게 주인 | 기록 | P2 |
| 3.3 | `ENDING_VET` | 급식이의 주치의 | 수의사 | 기록 | **P1** |
| 3.4 | `ENDING_SOLDIER` | 맨 앞에 선 사람 | 재건 방위대 | 기록 | P2 |
| 3.5 | `ENDING_NIGHT_SCOUT` | 밤을 걷는 사람 | 야간 수색대 | 기록 | P2 |
| 3.6 | `ENDING_KEEPER` | 학교를 기억하는 사람 | 학교 관리인 | 기록 | **P1** |
| 3.7 | `ENDING_SEEKER` | 이름을 불러 준 사람 | 사람 찾기 센터 | 기록 | P2 |
| 3.8 | `ENDING_SHADOW` | 발소리 없는 사람 | 정찰 요원 | 기록 | P2 |
| 3.9 | `ENDING_UNSCATHED` | 한 번도 쓰러지지 않은 사람 | 안전 교관 | 기록 | P2 |
| 3.10 | `ENDING_CARTOGRAPHER` | 새벽동 지도 | 지도 제작자 | 기록 | P3 |
| 3.11 | `ENDING_MERCHANT` | 더불어 장터 | 장터 상인 | 기록 | P3 |
| 3.12 | `ENDING_RATIONER` | 마지막 통조림 | 구호 식량 배분관 | 기록 | P3 |
| 4.1 | `ENDING_NARR_BURDEN` | 무게를 든 채로 | (회고 축) | 내면 | P2 |
| 4.2 | `ENDING_NARR_ABSURD` | 매일 물을 긷는 사람 | (회고 축, 엔딩은 아직 없음) | 내면 | P3 |

우선순위는 "그 엔딩이 얼마나 자주 나올 것 같은가"로 잡았다. **P1** = 한 가지를 꾸준히 하면 닿는 엔딩(11장), P2 = 플레이 방식을 바꿔야 닿는 엔딩(11장), P3 = 드물거나 아직 조건을 다듬는 엔딩(6장). 봇 분포가 나오면(P6) 다시 본다.

### 0.2 그림을 고른 원칙

- **후일담 3컷 가운데 마지막 컷**(10년 후)을 그린다. 엔딩 그림은 제목 화면부터 끝까지 깔려 있어서, 가장 오래 남는 장면이 맞다.
- **게임 안의 물건 하나**를 꼭 넣는다(대걸레 자루 목발, 옥상 흙, 방송반 CD, 창고 문고리 …). 그 물건이 "이 사람이 그 학교에서 왔다"는 것을 말해 준다(기획서 1절 원칙 3).
- **변형이 있는 엔딩은 어느 변형에도 맞는 장면**으로 잡았다. 예: 재활 치료사는 선생님이 화면에 없다(남은 판과 완치한 판 모두 맞는다). 수질 연구원은 CD가 없다. 라디오 진행자는 CD가 책상 구석에 있어도 되고 없어도 된다.
- **판마다 달라지는 사람은 넣지 않는다.** 동료는 합류하지 않았을 수 있다. 조건이 그 사람을 보장하는 컷에만 넣는다(보건 교사의 박 선생님, 수의사의 고양이, 장터 상인의 정 반장).

---

## 1. 이 문서에서 쓰는 문구

**[어른]** — 주인공이 나오는 모든 컷에 붙인다.
```
The woman is the grown-up version (mid-20s) of the girl in the attached reference sheet: the same straight black bob just above the shoulders with straight bangs and the same face shape, adult proportions, wearing the work clothes described here instead of a school uniform. She is shown from behind, in profile from a distance, or by her hands only; her face is never large or frontal.
```

**[재건]** — 장면 뒤에 붙인다. 5~10년 뒤의 세상이 어떤 곳인지 맞춘다.
```
Setting: a small Korean city about ten years after a disaster, slowly rebuilt by hand: patched buildings, repainted walls, vegetable planters and rain barrels on rooftops, handmade wooden signs with no readable text, bicycles and handcarts instead of cars, laundry lines, young trees. Calm, lived-in, modest and hopeful, not futuristic, not ruined. Soft natural light.
```

**인물 문구** (나오는 컷에서만)

| 자리 표시 | 시트 | 문구 |
|---|---|---|
| `{NURSE_OLD}` | `char_nurse_sheet.png` | `Nurse Park, the same Korean school nurse as the reference sheet about ten years older, now in her 50s with a neat short bob streaked with grey, a white nurse gown, sleeves rolled up` |
| `{BANJANG_OLD}` | `char_banjang_sheet.png` | `Foreman Jung, the man in the attached reference sheet about ten years older: the same build, face and clothes as the sheet, hair turned grey` |
| `{CAT_OLD}` | `ArtInbox/sprites12/ref/cat_illust_avatar.png` (12차의 고양이 생김새 기준) | `an old orange tabby cat (cheese-colored with darker stripes, cream chest and paws, one ear slightly notched), its muzzle and whiskers gone white with age, healthy and well fed, fast asleep` |

**고칠 것 하나를 찾았다**: 좌석 선택 엔딩 문장(`ending.ENDING_HIDDEN_CURE.seat_senior.cut2`)은 급식이를 "삼색 고양이"라고 쓰는데, 그림(8차·12차·13차)은 모두 **치즈색** 고양이다. 이 문서는 그림 쪽(치즈색)을 따른다. 문장을 "치즈색"으로 고칠지는 사용자가 정한다.

**안전 (13장) — 이 문서에서 특히 지킬 것**
- 주인공은 어른으로 나오지만 몸을 드러내거나 꾸민 차림은 넣지 않는다. 모두 일하는 옷이다.
- 윤 선생님의 다리는 그리지 않는다(재활 치료사 컷). 대걸레 자루 목발만 사물로 나온다.
- 방위대·야간 수색대·정찰 요원 컷에는 무기를 든 모습, 싸우는 모습, 좀비를 넣지 않는다. 실존 군·경찰의 제복·계급장·휘장도 넣지 않는다(`plain work clothes, no insignia`).
- 구호 식량 배분관 컷에는 마른 몸이나 굶주린 얼굴을 그리지 않는다. 고르게 나눈 몫만 보인다.
- 내면 엔딩 두 컷은 손과 사물로만 그린다. 자해·자살을 떠올리게 하는 구도(높은 곳 가장자리, 약병, 끈 등)는 쓰지 않는다.
- 고양이는 늙었지만 건강하다.
- 글씨는 모두 읽을 수 없게 둔다(`no readable text`).

---

## 2. 직업 엔딩 14장

| 번호 | 파일 · 배경색 | 참고 시트 | 그리는 장면 | 장면 (영어, `[어른]` 뒤에) |
|---|---|---|---|---|
| 2.1 | `ENDING_PARAMEDIC.png` · `#2E2426` | 서진 | 10년 후, 구급차가 가장 먼저 닿는 골목 | `Early morning in a narrow rebuilt alley. A hand-repaired ambulance van (plain white, a simple green cross, no text) is parked with its back doors open. A young paramedic seen from behind in a plain navy work jacket with a green-cross armband kneels on the pavement, calmly wrapping a clean bandage around an elderly man's forearm; the old man sits on a doorstep, relieved. A folded stretcher leans on the van. Neighbors watch quietly from doorways.` |
| 2.2 | `ENDING_SCHOOL_NURSE.png` · `#243032` | 서진, 박 선생님 | 다시 문을 연 새벽고 보건실, 약장 맨 위의 장부 | `A reopened school nurse office in soft morning light: two neat beds with white curtains, a medicine cabinet. On the top shelf of the cabinet stands one worn, water-stained ledger among new boxes. A young school nurse in a white gown, seen from behind, reaches up toward that ledger; an old stethoscope hangs around her neck. In the doorway {NURSE_OLD} stands watching with a small proud smile, half in shadow. A potted plant on the windowsill, children's voices implied by small indoor shoes lined up at the door.` |
| 2.3 | `ENDING_CHEF.png` · `#33291F` | 서진 | 간판 없는 식당, 값 대신 글이 적힌 메뉴판 | `Evening at a tiny restaurant with no signboard on a rebuilt street, warm light spilling from the open sliding door. Inside, a young cook seen from behind in an apron and a headscarf ladles steaming soup from a big dented aluminum pot into bowls. On the wall a small chalk menu board with a few handwritten lines (unreadable) and no prices. Three mismatched tables; an old man and a child eating, a school bag on a chair. An opened tin can used as a chopstick holder on the counter.` |
| 2.4 | `ENDING_FARMER.png` · `#243022` | 서진 | 봄, 새 밭에 학교 옥상 흙을 한 줌 섞는다 | `A spring morning on a community farm at the edge of the rebuilt city: long tidy rows of lettuce seedlings, a row of small greenhouses made of pipes and plastic sheeting. A young woman in work trousers, rubber boots and a straw hat, seen in profile from a distance, kneels at the head of a new row and sprinkles a handful of dark soil from a small cloth pouch onto the bed. A paper seed envelope is tucked in her back pocket. Far in the background, the flat roof of a four-story school with rooftop planters.` |
| 2.5 | `ENDING_WATER_SCIENTIST.png` · `#1F2C36` | 서진 | 10년 후, 수도꼭지에서 다시 마실 수 있는 물 | `A public water tap in a small rebuilt square at noon. A child on tiptoe drinks clear water running from a brass tap, water sparkling in the sun, people waiting with kettles and bottles. At the edge of the frame a young woman in a plain lab coat over work clothes, seen from behind, holds a glass sample jar of clear water up to the light. On a low wall beside her sits a homemade filter made from a cut plastic bottle layered with sand and charcoal, kept like a keepsake. In the distance, the tanks and pipes of a small water plant.` |
| 2.6 | `ENDING_MUSICIAN.png` · `#2E2434` | 서진 | 공연이 끝난 무대, "같이 버틴 사람들 거예요" | `A small outdoor stage built from school desks and planks in a rebuilt plaza at dusk, strings of small warm bulbs overhead. The concert has just ended: a young musician seen from behind stands at the front of the stage with an acoustic guitar slung on her back, bowing slightly toward a modest crowd of all ages who are standing and clapping. An old portable cassette recorder sits on a stool beside the microphone stand. Winter coats, visible breath, a few people wiping their eyes.` |
| 2.7 | `ENDING_COUNSELOR.png` · `#2A2C34` | 서진 | 상담실, 창문을 열고 같이 하늘을 본다 | `A small counseling room in a rebuilt school: two low armchairs, a rug, a box of tissues, a paper note taped to the half-open door (unreadable). A young counselor seen from behind has just opened the window wide; beside her a small child, also seen from behind and saying nothing, stands on tiptoe holding the windowsill. Both look out at a wide sky with slow clouds above school rooftops. No one is talking. A cup of barley tea steams on the table.` |
| 2.8 | `ENDING_NEGOTIATOR.png` · `#30281E` | 서진 | 재건 의회, 세 자리 한가운데의 자리 | `Inside a repaired community hall used as a council room: three groups of plain wooden chairs arranged facing each other in a triangle, and one empty chair exactly in the middle with a folded worn jacket over its back and a notebook on the seat. A young woman seen from behind stands beside that chair with one hand resting on it, facing the room as people file in and take seats; the three groups are told apart only by small things (dark raincoats, mart work vests, headphones around necks). Sunlight through tall windows, dust in the air. Calm, not tense.` |
| 2.9 | `ENDING_ATHLETE.png` · `#33261E` | 서진 | 출발선에 서서 숨을 고른다 | `A rebuilt school dirt track on a bright autumn morning, lanes freshly drawn with white lime powder, a hand-painted finish banner (no text) in the distance. A young runner seen from behind in a plain tracksuit top and running shorts over leggings crouches at the starting line with her fingertips on the ground, shoulders loose, breathing out slowly. Worn running shoes with mismatched laces. Children sit along the track edge watching; a teacher holds up a small flag.` |
| 2.10 | `ENDING_JOURNALIST.png` · `#26282C` | 서진 | 조각을 들고 사람들을 찾아다닌 책상 | `A cramped newsroom desk at night under a single lamp: a manual typewriter with a sheet in it, an open school notebook thick with clippings, and a cork board on the wall where photos and notes are joined by thread, with one gap in the middle where a single pin holds nothing. A young reporter seen from behind, sleeves rolled up, a press armband with no text, pins one more note beside the gap. Through the window, the lights of the rebuilt city.` |
| 2.11 | `ENDING_RADIO_HOST.png` · `#22283A` | 서진 | 새벽 방송, 마이크 앞 | `A small radio studio just before dawn: a desk with an old microphone, a mixing board with warm glowing dials, headphones, a mug, and a short stack of handwritten letters (unreadable). A young host seen from behind leans toward the microphone with one hand raised to cue a song. A red ON-AIR lamp glows without text. One worn CD case lies at the corner of the desk. Through the studio window the sky is turning pale blue over rooftops with a rebuilt antenna tower.` |
| 2.12 | `ENDING_ARCHITECT.png` · `#2C2A26` | 서진 | 준공식 날, 창고 문고리를 직접 돌려 본다 | `The opening day of a small newly built school: fresh pale walls, wide windows, a ribbon tied on the gate in the background, children and parents gathered far behind. In the foreground a young architect seen from behind, a rolled blueprint under her arm and a hard hat hooked on her belt, stands at a storeroom door and turns its handle with her own hand; the door swings open easily, sunlight falling inside. Every classroom along the corridor visibly has two doors.` |
| 2.13 | `ENDING_SCHOLAR.png` · `#28263A` | 서진 | 연구실 벽의 채점한 모의고사 한 장 | `A quiet university study room late in the afternoon: shelves of repaired books, a desk with open notebooks and a desk lamp. Pinned on the wall above the desk is one yellowed mock exam answer sheet marked with red pen and a short handwritten note in the margin (unreadable). A young researcher seen from behind sits at the desk writing, a pencil behind her ear. A school backpack, worn at the corners, hangs from the chair.` |
| 2.14 | `ENDING_NETWORK.png` · `#1E2A30` | 서진 | 다시 세운 기지국, 처음 열린 게시판 | `Dusk on a hill above the rebuilt city. A newly raised communication tower with a single blinking light. At its base a young technician seen from behind, a tool belt and a coil of cable over her shoulder, kneels at an open metal cabinet where an old beige computer monitor glows with pale blue text (unreadable) and a modem with a row of small green lights. Below the hill, windows of the city are lighting up one by one.` |

---

## 3. 기록 엔딩 12장

| 번호 | 파일 · 배경색 | 참고 시트 | 그리는 장면 | 장면 (영어, `[어른]` 뒤에) |
|---|---|---|---|---|
| 3.1 | `ENDING_REHAB.png` · `#2C2A22` | 서진 | 누군가 처음으로 혼자 서는 날 | `A bright rehabilitation room with wooden parallel bars and a mirror. An elderly patient seen from behind stands between the bars for the first time, both hands gripping them, knees a little unsteady. A young therapist seen in profile from behind stands half a step back with her open hands held near but not touching, ready to catch. In the corner of the room, kept like a keepsake, leans an old homemade crutch made from a mop handle wrapped with tape. Morning light, a quiet proud moment.` |
| 3.2 | `ENDING_RECORD_SHOP.png` · `#2A2238` | 서진 | 골목의 작은 음반 가게, 매일 트는 한 장 | `A tiny record shop in a rebuilt alley at night, its window glowing violet and amber; a hand-painted sign with no readable text. Inside, wooden crates of CDs and records, and behind the counter a wall shelf holding eight CD cases in a row, seven slots now empty with small paper tags, one CD case still in place. A young shop owner seen from behind places a disc into an old portable CD player on the counter. A customer browses. Rain-wet pavement reflecting the light.` |
| 3.3 | `ENDING_VET.png` · `#302C24` | 서진, 고양이 | 대기실 창가 자리의 늙은 고양이 | `The waiting room of a small animal clinic in the afternoon: wooden benches, a pet carrier, a child holding a puppy wrapped in a towel. On the sunny windowsill, on a folded blanket that is clearly its own place, lies {CAT_OLD}. A young veterinarian in a plain white coat, seen from behind, passes by and lightly rests two fingers on the cat's head without waking it. A stethoscope in her coat pocket.` |
| 3.4 | `ENDING_SOLDIER.png` · `#2E2422` | 서진 | 분대원들에게 돌아오는 길부터 가르친다 | `Dawn at the edge of an overgrown, long-abandoned neighborhood. A young squad leader seen from behind in plain olive work clothes and a hand-sewn cloth armband (no insignia, no rank marks) crouches with a stick, drawing a route in the dirt; a simple arrow curves out and back to where they stand. Four young volunteers of mixed ages crouch around her watching, carrying backpacks, rope and a first-aid bag. Nobody holds a weapon. An old worn wooden bat wrapped in tape is strapped, unused, to the side of her backpack. Calm, careful mood.` |
| 3.5 | `ENDING_NIGHT_SCOUT.png` · `#1C2030` | 서진 | 새벽에 돌아와 "전원 귀환" | `The grey-blue hour just before sunrise at a checkpoint gate of the rebuilt city. A young night searcher seen from behind, a headlamp pushed up on her forehead and switched off, a rope coil on her pack, walks in through the gate holding a walkie-talkie to her mouth. Behind her, three teammates and two rescued civilians wrapped in blankets follow in a tired line. A gatekeeper raises a lantern. The darkness of the unlit district lies behind them; the first pale light ahead.` |
| 3.6 | `ENDING_KEEPER.png` · `#2A2C28` | 서진 | 졸업식 날, 활짝 열어 둔 체육관 창고 문 | `Graduation day at the reopened school, seen from inside the gym: rows of folding chairs, paper garlands, students in uniform gathering far in the background. In the foreground the gym storeroom door stands wide open, propped with a wooden wedge, sunlight pouring through it onto stacked mats and ball baskets. A young caretaker seen from behind in a work apron hangs a heavy ring of keys on a hook beside the open door.` |
| 3.7 | `ENDING_SEEKER.png` · `#2A2830` | 서진 | 이름이 이어질 때마다 걸리는 종이학 | `A reunion office in a repaired public building: long walls covered with rows of handwritten name slips (unreadable), and hanging from strings across the ceiling, hundreds of small paper cranes in faded colors. A young woman seen from behind stands on a low stool hanging one more crane. Across the room, out of focus, two people who have just found each other embrace. The very first crane, folded from lined school notebook paper, hangs nearest the window.` |
| 3.8 | `ENDING_SHADOW.png` · `#1E2226` | 서진 | 문을 열기 전에 먼저 귀를 댄다 | `A dim stairwell landing in an abandoned apartment block, dust in a thin shaft of daylight. A young scout seen in profile from behind, in soft dark plain clothes and cloth-wrapped shoes, stands perfectly still with her ear and one flat palm against a closed apartment door, listening, her other hand raised to signal "wait" to two teammates who stand motionless on the stairs below. Nothing threatening is visible. Quiet, patient, careful.` |
| 3.9 | `ENDING_UNSCATHED.png` · `#26302C` | 서진 | 첫 수업 칠판의 세 줄 | `A classroom used for a safety course: adults and teenagers sit at school desks with notebooks. At the blackboard a young instructor seen from behind, chalk in hand, has just written three short lines one under another (handwriting unreadable, clearly three brief lines). A whistle and a coiled rope hang from her belt. On the teacher's desk a well-worn pair of walking shoes sits like a teaching prop. Afternoon light through the windows.` |
| 3.10 | `ENDING_CARTOGRAPHER.png` · `#2C2A20` | 서진 | 다시 그린 도시 지도와 일기장 뒤쪽의 첫 지도 | `A mapmaker's drafting table by a window: a large, carefully inked new map of a small city spread out, with rulers, a compass and ink bottles. Beside it lies an old school diary opened to its back pages, crammed with a hand-drawn pencil map of alleys, stairs and blocked doors with small symbols. A young woman's hands (only hands and forearms visible) add a tiny line of small writing to the corner of the new map (unreadable). A walking compass and a worn pencil stub next to the diary.` |
| 3.11 | `ENDING_MERCHANT.png` · `#302820` | 서진, 정 반장 | 값 대신 팻말이 걸린 장터 | `A lively open-air market in the parking lot of a repaired mart on a clear morning: stalls of vegetables, canned goods, batteries, candles and medicine boxes under patchwork awnings, a hanging wooden sign with a short handwritten phrase (unreadable) and no prices anywhere. A young market keeper seen from behind, in an apron with a ledger under her arm, hands a paper bundle to an old woman. Behind the next stall {BANJANG_OLD} weighs rice on a balance scale, laughing. Handcarts, bicycles, children running.` |
| 3.12 | `ENDING_RATIONER.png` · `#2A2622` | 서진 | 줄 맨 끝의 아이에게도 같은 크기의 몫 | `A relief distribution point in a repaired warehouse: a long, orderly queue, shelves of sacks and cans behind a wooden counter. On the counter stands a neat row of identical paper parcels, all exactly the same size. A young woman seen from behind, a ring of keys at her belt, bends down to hand the last parcel to a small child at the very end of the line, the parcel the same size as all the others. Everyone looks healthy and patient. Soft daylight from high windows.` |

---

## 4. 내면 엔딩 2장

막 Ⅱ(돌아보는 밤)의 회고 축이다. 지금 게임은 대표 엔딩(막 Ⅲ) 그림 한 장만 깔아서, 이 두 장은 **막마다 다른 배경을 넣은 뒤에** 화면에 나온다. 그때까지는 갤러리 썸네일로만 쓰인다. 사람 얼굴 없이 손과 사물로 그린다.

| 번호 | 파일 · 배경색 | 참고 시트 | 그리는 장면 | 장면 (영어, `[어른]` 없이) |
|---|---|---|---|---|
| 4.1 | `ENDING_NARR_BURDEN.png` · `#262430` | (없음) | 주전자와 컵 두 개, 손바닥 위의 돌 | `Night in a school staff room lit by one small desk lamp. On the desk: a dented kettle and two cups, one pushed toward the viewer. In the foreground a teenage girl's open hands rest on the desk (only hands and the cuffs of a navy school blazer visible), and in one palm lies a single smooth grey river stone, held, not thrown away. Across the desk, out of focus, the folded hands of an adult in a grey cardigan. Warm lamp light, deep blue shadows, a quiet and unhurried mood.` |
| 4.2 | `ENDING_NARR_ABSURD.png` · `#24262E` | (없음) | 새벽 옥상, 빗물통과 양동이 | `Just before sunrise on a school rooftop after rain: a row of blue rain barrels and a tarp funnel, puddles reflecting a pale sky. A teenage girl seen from behind and from a distance, in a padded jacket over a school uniform, carries a full bucket in both hands across the middle of the rooftop, well away from the edges, toward a homemade filter stand. Her breath fogs. Nothing dramatic happens: it is simply the same chore as every morning. Cold blue air with one thin line of warm light on the horizon.` |

- 4.2의 배경색 `#24262E`는 제안값이다. 엔딩 데이터가 생길 때 맞춘다.
- 4.2는 옥상이지만 **가장자리에서 떨어진 한가운데**를 걷는 구도로만 받는다(1절 안전).

---

## 5. 더 받으면 좋은 그림 4장 (막별 그림)

세 막 엔딩에서 새로 생긴 구조 장면과 기본 회고 축이다. **지금은 받지 않아도 된다** — 막마다 다른 배경을 넣는 작업(기획서 12.2절 "P0에서 하지 않은 것")을 할 때 함께 받는다. 그 전에는 화면에 나올 자리가 없다(갤러리 칸에는 배경색이 깔린다).

| 번호 | 파일 · 배경색 | 참고 시트 | 그리는 장면 | 장면 (영어) |
|---|---|---|---|---|
| 5.1 | `ENDING_RESCUE_CROWD.png` · `#2A3030` | 서진 | 자리가 모자랄 만큼 사람이 모인 옥상 | `Sunrise on the school rooftop beside a faded painted SOS. A rescue helicopter hovers low, its side door open. A teenage girl with a straight black bob in a school uniform, seen from behind, stands at the front of a small crowd of survivors of different ages, all seen from behind with bags and blankets, while a rescue worker counts heads with a clipboard and looks surprised at how many there are. Wind from the rotors lifts hair and coats.` |
| 5.2 | `ENDING_RESCUE_ALONE.png` · `#1E2428` | 서진 | 혼자 선 옥상 | `Sunrise on the school rooftop beside a faded painted SOS. A rescue helicopter hovers low with its door open. One teenage girl with a straight black bob in a school uniform, seen from behind, stands alone in the wide empty rooftop holding the straps of her backpack, her long shadow stretching behind her. In the schoolyard far below, a small figure leaning on a crutch raises one hand. Very quiet, wide composition, a lot of empty space.` |
| 5.3 | `ENDING_RESCUE_MORNING.png` · `#20283A` | — | (새로 받지 않는다) | 기존 `ENDING_SURVIVOR.jpg`(옥상에서 두 팔을 흔드는 뒷모습)를 그대로 쓴다. 파일을 복사해 이 이름으로 넣는다. |
| 5.4 | `ENDING_REFLECT_DEFAULT.png` · `#20283A` | (없음) | 남은 질문: 캠프의 밤, 덮지 못한 일기장 | `Night in a relief camp tent lit by a hanging lantern: a folding cot with a grey blanket, a school backpack at its foot. On the blanket lies an open diary with a pencil across a half-written page (unreadable); a teenage girl's hand rests beside it, not writing. Through the open tent flap, rows of other lit tents and a clear starry sky. Still, wakeful, neither sad nor happy.` |

- `ENDING_RESCUE_SEAT`(비워 둔 자리)는 남은 사람마다 이미 그림이 있다(`ending_seat_*.jpg`, 5차). 새로 받지 않는다.

---

## 6. 받은 뒤 내가 하는 일

1. `ArtInbox/illustrations/endings18/`의 그림을 16:9 · 너비 1920 jpg로 바꿔 `Resources/Art/Story/Endings/`에 넣는다(9차 엔딩 그림과 같은 처리).
2. 갤러리와 엔딩 화면에서 그림이 깔리는지 확인한다. 글이 잘 읽히는지(밝은 그림 위의 흰 글씨)도 본다.
3. 배경색을 그림의 주된 색에 맞춰 손본다(`endings.json`의 `Color`).
4. 5절 그림은 막별 배경 작업을 할 때 연결한다.

## 7. 체크리스트

- [ ] P1 11장: 2.1~2.7, 2.9, 3.1, 3.3, 3.6
- [ ] P2 11장: 2.8, 2.10~2.12, 3.2, 3.4, 3.5, 3.7~3.9, 4.1
- [ ] P3 6장: 2.13, 2.14, 3.10~3.12, 4.2
- [ ] (나중) 막별 그림 3장: 5.1, 5.2, 5.4
