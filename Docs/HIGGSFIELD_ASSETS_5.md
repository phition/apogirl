# Higgsfield 에셋 생성 문서 5차 (2026-11-21)

세 가지를 모았다.
- **구조 헬기 좌석 선택**(9명일 때 한 명이 남는다)의 엔딩 컷.
- 새로 정식 인물이 된 **2학년 선배**.
- 4막 보강에서 그림이 비어 있는 **이벤트 컷**.

- 쓰는 법·모델·공통 공식은 [HIGGSFIELD_ASSETS.md](HIGGSFIELD_ASSETS.md) 0장을 따른다. `[F-ILLU]` = 0.3 일러스트 공식, `[F-KOREA]` = 한국 건물 문구, `[안전]` = 0.4 안전 문구.
- 프롬프트는 영어, 설명은 한국어. 넣는 곳은 `ArtInbox/illustrations/`, 파일 이름은 이 문서 그대로.
- 인물이 나오면 기준 시트(`char_*_sheet.png`)를 참고 이미지로 넣는다. 선배는 시트가 없으니 **2장부터 먼저** 만든다.
- 구조 날은 **3월 중순 이른 봄 아침**이다. 엔딩 컷에는 모두 `[봄]`을 붙인다.

`[봄]` — 엔딩·구조 날 컷
```
Season: early spring morning in mid-March, the last thin patches of old snow melting in shaded corners, pale green buds on bare branches, cold clear light, soft beige and pale blue tones.
```

`[헬기]` — 헬기가 나오는 컷
```
The rescue helicopter is a plain orange-and-white civilian rescue helicopter with no readable markings, no numbers and no text, rotor blades motion-blurred.
```

## 0. 한눈에 보기

| 장 | 내용 | 장수 | 우선 |
|---|---|---|---|
| 1 | 좌석 선택 엔딩 컷 | 8 (+선택 2) | **P1** |
| 2 | 2학년 선배 기준 시트·표정 | 시트 1 + 표정 4 | **P1** |
| 3 | 선배 이벤트 컷 | 6 | P2 |
| 4 | 4막 이벤트 컷 (그림이 빈 장면) | 17 | P2~P3 |

먼저 만들 것은 **2.1 선배 기준 시트**다. 1장의 엔딩 컷 중 선배가 나오는 것은 그 시트를 참고 이미지로 넣어야 얼굴이 맞는다.

---

## 1. 좌석 선택 엔딩 컷 (P1, 16:9)

### 1.1 상황

- 선생님이 완치되고 동료가 모두 모이면 탑승 인원이 9명이 되어, 정원 8명을 한 명 넘는다.
- 구조 직전 밤에 **한 명을 고른다**. 다음 헬기는 최소 한 달 뒤다.
- 고른 사람에게는 짝이 있어서 대부분 둘이 함께 남는다. 도현과 준(아이들)은 고를 수 없다.

| 고른 사람 | 같이 남는 사람 | 남는 사람 | 컷 파일 |
|---|---|---|---|
| 윤 선생님 또는 박 선생님 | 나머지 선생님 ("어른들의 책임") | 두 선생님 | `ending_seat_teachers.png` |
| 친구 | 주인공이 마지막에 헬기에서 뛰어내림 | 친구·주인공 | `ending_seat_friend.png` |
| 최하린 | 2학년 선배 | 하린·선배 | `ending_seat_ringleader.png` |
| 송예지 | 2학년 선배 | 송예지·선배 | `ending_seat_bystander.png` |
| 2학년 선배 | 없음. 옥상의 고양이를 안는다 | 선배 | `ending_seat_senior.png` |
| 주인공 | 최하린, 윤 선생님 (하린: "두 번은 없어") | 주인공·하린·윤 선생님 | `ending_seat_self.png` |

- 둘이 내리면 한 자리가 빈다. 그 자리는 사거리 파 아이에게 간다. 강철과 관계가 좋거나 준이 강철 앞을 막아선 판에서만이고, 아니면 빈자리를 싣고 떠난다 → `ending_seat_empty_seat.png`.
- 남은 사람들은 한 달 뒤 두 번째 헬기로 온다 → 공통 후일담 컷 `ending_seat_second_heli.png`.
- 엔딩 자체는 완치 엔딩(`ENDING_HIDDEN_CURE`) 그대로다. 위 컷은 **누가 남았는지에 따라 엔딩 그림을 바꿔 끼우는 용도**다. 게임에 연결하는 방식은 좌석 선택 설계안을 승인받은 뒤 정한다.

### 1.2 인물 생김새 (참고 시트와 함께)

| 인물 | 시트 | 이 컷들에서 붙이는 문구 |
|---|---|---|
| 주인공 서진 | `char_seojin_sheet.png` | `16-year-old Korean girl, shoulder-length straight black hair, navy school uniform under a worn padded jacket` |
| 윤 선생님 (완치) | `char_teacher_sheet.png` | `Korean man in his mid-40s, glasses, cardigan under a coat, healthy again but thin, his left trouser leg ends in a simple homemade prosthetic or is folded and pinned below the knee, a cane or crutch` |
| 박 선생님 | `char_nurse_sheet.png` | `Korean school nurse, woman in her 40s, neat short bob, quilted padded vest over a white nurse gown, a first-aid bag on her shoulder` |
| 친구 | `char_friend_sheet.png` | 시트 그대로 |
| 최하린 | `char_ringleader_sheet.png` | `16-year-old Korean girl, shoulder-length dyed brown hair grown out at the roots, thin, sharp but tired eyes` |
| 송예지 | `char_bystander_sheet.png` | `16-year-old Korean girl, long straight hair tied low, slightly oversized navy uniform` |
| 2학년 선배 | `char_senior_sheet.png` (2장) | 2.1 문구 |
| 고양이 '급식' | — | `a thin calico cat with the tip of one ear clipped, white chest, calm amber eyes` |

### 1.3 프롬프트

모두 `[F-ILLU] [F-KOREA]` 뒤에 아래 문장, 그 뒤에 `[봄] [헬기] [안전]`.

| 우선 | 파일 | 장면 | 프롬프트 |
|---|---|---|---|
| P1 | `ending_seat_teachers.png` | 두 선생님이 옥상에 남아 손을 흔든다 | `Seen from inside the open door of a rescue helicopter lifting off a school rooftop: on the rooftop below, beside a faded red SOS painted on the concrete, a middle-aged male teacher with glasses leaning on a cane and a female school nurse in a padded vest stand side by side, both raising one hand to wave, calm proud smiles, the nurse's other hand steadying the teacher's arm. Inside the helicopter in the foreground, the backs of several teenagers pressed to the window. Responsible, warm, bittersweet.` |
| P1 | `ending_seat_friend.png` | 친구가 내리고, 주인공이 뛰어내린다 | `A rescue helicopter hovering very low, only about a meter above a school rooftop, its side door open. A teenage girl in a padded jacket hops down from the doorway onto the rooftop, both feet landing, toward another teenage girl who is already standing on the rooftop with open arms, both laughing and crying. Hands reach out from inside the helicopter toward them. Wind from the rotor blowing their hair and loose papers. Joyful, reckless, loving. Safe low hop, not a fall.` |
| P1 | `ending_seat_ringleader.png` | 하린과 선배가 남는다 | `A school rooftop after the helicopter has gone, the small dot of the helicopter in the far sky. A thin teenage girl with grown-out dyed brown hair sits hugging her knees near the stairwell door, and a little apart from her, a tall silent older male student with a long pale healed scar from cheek to neck stands with his hands in his pockets, keeping her company without a word, both looking at the sky. Quiet, uneasy, not alone.` |
| P1 | `ending_seat_bystander.png` | 송예지와 선배가 남는다 | `A school rooftop after the helicopter has gone. A slim teenage girl with long hair tied low waves at the shrinking helicopter with both hands, tears on her face but smiling, and a tall silent older male student with a long pale healed scar from cheek to neck stands beside her holding her backpack for her. Gentle, protective, hopeful.` |
| P1 | `ending_seat_senior.png` | 선배가 혼자 남아 고양이를 안는다 (사용자 지정) | `A school rooftop beside the faded red SOS, the rescue helicopter rising away in the background. A tall silent older male student with a long pale healed scar from his left cheek down his neck stands alone, holding a thin calico cat with a clipped ear against his chest with both arms, his chin resting lightly on the cat's head, eyes closed, the faintest smile. The cat is calm and content. Still, tender, lonely but at peace.` |
| P1 | `ending_seat_self.png` | 주인공이 남고, 하린과 윤 선생님이 따라 남는다 (사용자 지정) | `View from the rooftop as the rescue helicopter rises into the morning sky: in the open helicopter door a teenage girl leans out reaching back with one hand, crying. On the rooftop three figures stand in a row and watch it go: in the center a 16-year-old girl with shoulder-length black hair in a padded jacket, her hand raised to wave; to her left a middle-aged male teacher with glasses leaning on a cane, one hand on her shoulder; to her right, half a step behind, a thin teenage girl with grown-out dyed brown hair, arms crossed, looking away but staying. Determined, bittersweet, chosen.` |
| P2 | `ending_seat_empty_seat.png` | 빈자리 하나 (사거리 아이가 탐) | `Inside a rescue helicopter, a row of seats with teenagers in worn coats; one seat by the door is taken by a small child in an oversized dark raincoat with rolled-up sleeves, clutching a dented tin cup, looking out the window. Through the window, far below, a heavyset man in a baseball cap stands at a snowy crossroads raising one hand. Quiet gratitude.` |
| P2 | `ending_seat_second_heli.png` | 한 달 뒤, 두 번째 헬기 (공통 후일담) | `One month later, mid-April: the same school rooftop with cherry blossoms in bloom in the schoolyard below, a second rescue helicopter landing. On the rooftop the people who stayed behind wait with their packed bags, and from the helicopter door the friends who left first jump down to meet them. Reunion, spring wind, petals in the air.` |

- `ending_seat_self.png`는 "주인공이 남는 경우의 엔딩 컷을 다시 그리자"에 맞춘 **새 구도**다. 헬기 문에서 손을 뻗는 친구가 주인공의 선택을 지켜보는 증인이 된다(마스터 프롬프트 8장의 친구 레이어).
- 기존 `ENDING_HIDDEN_STAY.png`(감염된 선생님 곁에 남는 히든 엔딩)는 아직 받지 않았다. 지금 이야기에 맞춰 프롬프트를 고친 안은 아래와 같다. 원본 [ART_PROMPTS.md](ART_PROMPTS.md) 6.2 대신 이 문구로 만들어도 된다.

| 우선 | 파일 | 프롬프트 (`[F-ILLU] [F-KOREA] [봄] [헬기]` 뒤, 끝에 `[안전]`) |
|---|---|---|
| P2 | `ENDING_HIDDEN_STAY.png` (고친 안) | `A rescue helicopter leaving the school rooftop, seen from the rooftop: a 16-year-old girl with shoulder-length black hair sits on a folded blanket beside a pale, feverish middle-aged male teacher with glasses wrapped in a blanket, the teacher's left trouser leg folded and pinned below the knee, a crutch lying beside them. In the open helicopter door a teenage girl reaches back toward them. The girl on the rooftop holds the teacher's hand and does not look away. Heartbreaking but calm.` |

---

## 2. 2학년 선배 (P1)

### 2.1 기준 시트 `char_senior_sheet.png` (16:9)

설정(대본에 있는 것):
- 2학년(17세). 왼쪽 뺨에서 턱 아래를 지나 목까지 길게 이어진, **이미 아문 흉터**가 있다.
- 말을 하지 않고, 명찰은 뜯겨 있다.
- 교복 위에 체육복을 겹쳐 입었다. 예전에는 알아주는 일진이었다.
- 지금은 조용하고, 맡기면 고개를 끄덕이고 해낸다.

성별: 대본은 성별을 정하지 않았다. 사용자 설명의 "그"를 따라 **남학생으로 잡았다**. 바꾸려면 아래 프롬프트의 `male`만 바꾸면 된다.

프롬프트: `[F-ILLU]` **첫 문장만** + 아래 + `[안전]`
```
Character reference sheet of a 17-year-old Korean male high school student, second-year, tall with broad but thin shoulders, short black hair grown out unevenly, a long pale fully healed scar running from his left cheek down under the jaw to the side of his neck, old and smooth, not fresh, not graphic. Quiet heavy-lidded eyes that used to be intimidating and now look tired and calm. A faded navy school gym jacket zipped over a white school shirt, navy uniform trousers, the name tag on the chest torn off leaving loose threads, worn sneakers, a battered school backpack on one shoulder. Hands in jacket pockets, slightly hunched, standing a little apart as if out of habit. Front view and side view, full body, plain grey background.
```

### 2.2 표정 4종 `char_senior_{표정}.png` (3:4 상반신, 회색 배경)

참고 이미지 = `char_senior_sheet.png`, 모델 `seedream_v5_pro` 또는 `nano_banana_2_1`. 말을 하지 않는 인물이라 **입은 다문 채로** 표정을 낸다.

| 파일 | 표정 | 프롬프트 (`Bust-up portrait of the same student from the reference sheet, the healed scar on the same side,` 뒤에) |
|---|---|---|
| `char_senior_neutral.png` | 무표정 (기본) | `neutral calm face, lips closed, looking straight ahead, unreadable` |
| `char_senior_nod.png` | 고개 끄덕임 | `a small nod with eyes lowered, lips closed, quiet acceptance` |
| `char_senior_smile.png` | 아주 옅은 웃음 (출석부·고양이) | `the faintest smile at one corner of the mouth, eyes softened, lips closed, as if surprised by his own smile` |
| `char_senior_away.png` | 시선 피함 (사거리에서 알아봤을 때) | `face turned slightly away, jaw tight, eyes cast down to the side, hand touching the scar on his neck` |

---

## 3. 선배 이벤트 컷 (P2, 16:9)

모두 `[F-ILLU]`(실외는 `[F-KOREA]`도) 뒤에, 끝에 `[안전]`. 참고 = `char_senior_sheet.png` (+ 나오는 인물 시트).

| 파일 | 이벤트 | 프롬프트 |
|---|---|---|
| `evt_sv_meet.png` | 만남 `EVT_NPC_SURVIVOR_A_01` | `Late spring afternoon, the shaded back wall of a school gymnasium, weeds in the cracked concrete. A tall silent male student in a faded navy gym jacket over a school shirt stands in the shade holding an empty plastic water bottle, a long pale healed scar from his cheek down his neck, looking calmly at the viewer, neither approaching nor running. The torn name tag threads on his chest.` |
| `evt_sv_join.png` | 합류 `EVT_NPC_SURVIVOR_A_02` | `In front of a school storage room door, a middle-aged male teacher with glasses leaning on a crutch looks up at the tall scarred student, who silently holds up two fingers. A first-year girl stands between them. Gentle, awkward first meeting.` |
| `evt_sv_errand.png` | 심부름 `EVT_SV_01_ERRAND` | `A classroom at dusk: a chalkboard with a short hand-drawn list of simple item pictograms (no readable text), and on the teacher's desk below, an opened backpack with exactly those items laid out neatly in a row: two rolls of bandage, four batteries, a matchbox, a folded towel. The scarred student is walking away toward the window seat, seen from behind.` |
| `evt_sv_scar.png` | 흉터의 밤 `EVT_SV_02_SCAR` | `Night, a dark classroom lit only by moonlight through the window, the scarred student sitting on the window ledge, his fingertips slowly tracing the healed scar on his neck, eyes on the empty moonlit schoolyard, a blanket beside him. Quiet, lonely.` |
| `evt_sv_recognized.png` | 사거리가 알아봄 `EVT_SV_03_RECOGNIZED` | `A snowy crossroads in a Korean town in winter, a man in a dark raincoat blocking the way and staring at the tall scarred student, who looks back at him without expression while adjusting the heavy bag on his shoulder; a first-year girl beside them stiffens. Tense, no weapons raised.` |
| `evt_sv_attendance.png` | 출석부 `EVT_SV_04_FAREWELL` | `Close-up at night under a small lamp: an open handwritten class attendance book on a desk, a large hand with a pencil drawing a small circle beside one line of the page, the lines of handwriting blurred and unreadable, a girl's hand holding the other side of the book.` |

---

## 4. 4막 이벤트 컷 (그림이 빈 장면, 16:9)

이번에 대사를 늘린 4막 장면 중 그림이 아직 연결되지 않은 것이다. 모두 `[F-ILLU]` 뒤, 끝에 `[안전]`. 겨울 장면은 `Season: deep winter, snow, cold blue light.`를 더한다.

| 우선 | 파일 | 이벤트 | 프롬프트 |
|---|---|---|---|
| P2 | `evt_a4_seat_choice.png` | 좌석 선택 (새, 52주) | `Night before the rescue, a classroom lit by a single candle, a sheet of paper on a desk with eight pencil-drawn boxes and nine small circles drawn beside them, an eraser and a pencil, a girl's hands resting on either side of the paper, hesitating. No readable text.` |
| P2 | `evt_a4_seat_count.png` | 좌석 세기 `EVT_A4_SEAT_COUNT` | `Night, people wrapped in blankets gathered around a small radio on a classroom desk, several hands raised with fingers slowly folding as they count, faces hidden in shadow, the radio dial glowing.` |
| P2 | `evt_a4_opening.png` | 겨울의 첫 밤 `EVT_A4_OPENING` | `Night, a girl standing alone at a classroom chalkboard holding chalk in front of a weekly grid drawn on the board, frost on the window, a teacher lying under blankets on mats in the corner watching her.` |
| P2 | `evt_a4_waterworks_map.png` | 정수장 해금 `EVT_A4_WATERWORKS_UNLOCK` | `A city map spread on a classroom floor at night, a red marker circle drawn around a riverside water treatment plant, a finger tracing a route along the river, a flashlight lying beside it.` |
| P2 | `evt_a4_research_memo.png` | 연구 메모 `EVT_A4_RESEARCH_MEMO` | `A laboratory whiteboard in a dark research building, hurried marker notes and dates (unreadable), the last line pressed so hard it scratched the board, a flashlight beam on it.` |
| P2 | `evt_a4_notice.png` | 학교 공문 `EVT_A4_NOTICE` | `A school staff room desk, an open document registry ledger with one row stamped and the next column left empty, an official letter beside it with a pencil note in the margin (unreadable), winter afternoon light.` |
| P2 | `evt_a4_truth_link.png` | 진실 잇기 `EVT_A4_TRUTH_LINK` | `Night, a classroom floor covered with papers, photos, a cassette and documents laid out in a long row by date, connected by red pen lines all converging on one hand-drawn water plant symbol, a girl kneeling over them holding the red pen.` |
| P2 | `evt_a4_cured.png` | 완치 `EVT_A4_CURED` | `A nurse office in the evening, a thermometer laid on an open observation notebook with many pages of handwritten numbers (unreadable), a middle-aged male teacher with glasses sitting up on his own on a cot holding an attendance book, everyone around him laughing.` |
| P2 | `evt_a4_cure_attempt.png` | 처치의 밤 `EVT_A4_CURE_ATTEMPT` | `Night in a school nurse office, lamps on, a teacher asleep on a cot under a blanket, a girl and a nurse sitting awake beside him checking a notebook and a clock, a basin and folded towels, no needles, no blood.` |
| P2 | `evt_a4_rescue_route.png` | 구조대 교신 `EVT_A4_RESCUE_ROUTE` | `Night, a girl gripping a handheld radio, frozen mid-word, and on the desk in front of her a small folded paper note with round handwriting (unreadable) and an empty plastic water bottle.` |
| P2 | `evt_a4_ubc_relay.png` | 명단 부르기 `EVT_A4_UBC_RELAY` | `Night, a girl speaking slowly into a radio microphone, the others sitting in a circle around her in blankets, each looking up as if their name has just been called.` |
| P2 | `evt_a4_dh_mother.png` | 도현 엄마 `EVT_A4_DH_MOTHER` | `Night, a 15-year-old boy in an oversized green tracksuit hugging a handheld radio to his chest with both arms, eyes shut, tears, the others pretending to sleep around him.` |
| P3 | `evt_a4_fuel.png` | 연료 `EVT_A4_FUEL` | `A cold classroom, a wooden school desk broken into pieces burning in a small metal stove, carved graffiti hearts visible on a plank about to be fed in, breath fogging.` |
| P3 | `evt_a4_sageori_after.png` | 다음 날 아침 `EVT_A4_SAGEORI_AFTER` | `Winter morning after a long night, trampled snow along the school wall, a tipped-over can of red paint, a 12-year-old boy in a huge raincoat standing alone at the school gate.` |
| P3 | `evt_talk_12.png` | 대화 12 `EVT_TALK_12` | `Night, a teacher lying under a blanket holding a girl's hand, a damp towel set aside, a diary closed on the blanket.` |
| P3 | `evt_talk_13.png` | 대화 13 `EVT_TALK_13` | `Night, a girl reading aloud from a worn diary by candlelight, a teacher listening with his eyes closed, a faint smile.` |
| P3 | `evt_a4_cure_hint.png` | 치료 힌트 `EVT_A4_CURE_HINT` | `A research file closed on a nurse office desk, a new observation notebook beside it with a freshly ruled table, a pencil, a thermometer, lamp light.` |

---

## 5. 체크리스트

- [ ] 2.1 선배 기준 시트 1 → 2.2 표정 4
- [ ] 1.3 좌석 엔딩 컷 6 (P1) + 2 (P2), 고친 `ENDING_HIDDEN_STAY` (선택)
- [ ] 3장 선배 이벤트 컷 6
- [ ] 4장 4막 이벤트 컷 12 (P2) + 5 (P3)

## 6. 받은 뒤 내가 하는 일

- 이벤트 컷과 인물 표정은 `Resources/Art/Story/Events`, `Portraits`로 옮기고 `art_map.json`에 잇는다. 선배 화자 초상은 `Speakers`에 등록한다(지금은 대사에서 말하지 않으니 표정은 장면 컷·출석 장면에 쓴다).
- 좌석 엔딩 컷은 좌석 선택 기능(설계 승인 뒤 만듦)이 정하는 "남은 사람" 플래그에 따라 완치 엔딩 그림을 바꿔 끼운다.
