# 힉스필드 에셋 14차: 남의 그림을 빌려 쓰는 이벤트에 제 컷 주기 (2026-11-24)

12차 나머지 51장을 연결했다. 이제 이벤트 289개가 모두 그림을 가진다. 하지만 그 가운데 일부는 **자기 컷이 없다**. 다른 이벤트의 컷이나 빈 방 배경을 빌려 쓴다. 그래서 대사와 그림이 맞지 않는다.

- 예: 윤 선생님이 누운 채 출석부를 건네는 장면(`EVT_A4_TEACHER_BEDRIDDEN`)에 칠판 수업 컷이 나온다.
- 예: 동료 작별 4편이 모두 사람 없는 복도 컷 한 장을 같이 쓴다.
- 예: 박 선생님이 윤 선생님 다리를 살피는 밤(`EVT_A2_NURSE_EXAM`)에 빈 보건실 배경이 나온다.

이 문서는 빌려 쓰는 이벤트 59개의 대본을 읽고, **장면이 분명히 다른 22장**만 골라 새로 받는 문서다. 빌려 써도 맞는 것은 그대로 둔다(4장).

쓰는 법:

- 쓰는 법·모델·공통 공식은 [HIGGSFIELD_ASSETS.md](HIGGSFIELD_ASSETS.md) 0장을 따른다.
  - `[F-ILLU]` = 0.3 일러스트 공식, `[F-KOREA]` = 한국 건물 문구, `[안전]` = 0.4 안전 문구.
  - 계절 문구 `[봄]`·`[여름]`·`[가을]`·`[겨울]`은 [HIGGSFIELD_ASSETS_8.md](HIGGSFIELD_ASSETS_8.md) 0.3에 있다.
- 인물 문구·`[일관성]`·윤 선생님 다리 문구 ①~④는 [HIGGSFIELD_ASSETS_13.md](HIGGSFIELD_ASSETS_13.md) 1절을 그대로 쓴다. 이 문서에 새로 더한 인물 문구는 1절에 있다.
- 인물이 나오는 컷은 **그 인물의 시트(`ArtInbox/illustrations/char_*_sheet.png`)를 꼭 참고 이미지로 넣는다.** 참고는 4장까지다. 시트를 먼저 넣고, 자리가 남으면 13차 컷 한 장을 그림체 참고로 더한다(12차 머리 표).
- 프롬프트는 영어, 설명은 한국어로 쓴다. 표의 프롬프트 칸 하나가 생성 하나다.
- 이벤트 컷은 16:9, 2k 이상이다. 받은 그림은 `ArtInbox/illustrations/events14/`에 같은 파일 이름으로 둔다.
- **작업 제출은 사용자가 한다.** 나(Claude)는 이 문서만 만든다.

---

## 0. 한눈에 보기

| 절 | 무엇 | 우선 | 장수 |
|---|---|---|---|
| 2 | 4막 큰 장면: 출석부·최후통첩·혈액형 | **P1** | 3 |
| 3 | 동료 작별 4편 | **P1** | 4 |
| 4 | 2·3막 이야기 장면: 생활기록부·파출소 | P2 | 3 |
| 5 | 인물이 있어야 하는 밤 장면 | P2 | 5 |
| 6 | 단서·소품 장면 | P3 | 7 |
| | **합계** | | **22** |

**먼저 받을 것**: 2·3절 7장. 이야기가 가장 무거운 장면인데 지금 그림이 가장 안 맞는다.

---

## 1. 이 문서에서 더 쓰는 문구

13차 1절의 문구에 아래 두 사람을 더한다.

| 자리 표시 | 시트 | 문구 |
|---|---|---|
| `{GANGCHEOL}` | `char_gangcheol_sheet.png` | `Gangcheol, a Korean man in his 40s, leader of a street gang of survivors, heavy build, weathered face with a short beard, a worn baseball cap pulled low, a dark raincoat over a thick jacket, work boots, a can of red spray paint hooked on his belt, unreadable calm expression, not cartoonishly evil` |
| `{JUN}` | `char_jun_sheet.png` | `Jun, a 12-year-old Korean boy, small for his age, an adult-sized dark raincoat with the sleeves rolled up many times, dirty sneakers, guarded eyes` |

- `{SEOJIN}`은 봄·가을, `{SEOJIN_SUMMER}`는 여름, `{SEOJIN_WINTER}`는 겨울 차림이다. 표에는 계절에 맞춰 적었다.
- 윤 선생님은 `{TEACHER}` 뒤에 `+다리①`처럼 적은 다리 문구를 붙인다.
- `[안전]`: 다리·상처는 붕대와 접어 핀으로 고정한 바지로만 보인다. 피·상처 자리는 그리지 않는다.
- 글씨는 모두 **읽을 수 없게** 둔다(`no readable text`). `SOS` 한 곳만 예외다.

---

## 2. 4막 큰 장면 (3장, P1)

| 파일 | 이벤트 (지금 쓰는 그림) | 계절 | 프롬프트 |
|---|---|---|---|
| `evt_a4_teacher_bedridden.png` | `EVT_A4_TEACHER_BEDRIDDEN` (`evt_a4_last_lesson` 칠판 수업) | `[겨울]` | `Night in the classroom shelter, {TEACHER} +다리④ lies on a mat with his head on a folded-blanket pillow and holds out a worn attendance book to {SEOJIN_WINTER}, who kneels beside him taking it with both hands; small folded paper notes stick out between its pages. A candle by the pillow, others' silhouettes sitting quietly in the dim background. Tender, heavy mood.` |
| `evt_a4_sageori_ultimatum.png` | `EVT_A4_SAGEORI_ULTIMATUM` (`evt_a4_sageori_gate` 강철 혼자) | `[겨울]` | `Grey winter day at the school front gate, {GANGCHEOL} stands outside the bars with a group of men in dark raincoats behind him; across the school's concrete wall a huge message has been sprayed in dripping red paint (unreadable brush strokes). Small {JUN} stands half hidden behind the men. Inside the gate, {SEOJIN_WINTER} faces them, the snow trampled. Tense standoff, no weapons raised.` `[F-KOREA]` |
| `evt_a4_blood_other.png` | `EVT_A4_BLOOD_MISMATCH`·`EVT_A4_BLOOD_UNKNOWN` 둘이 함께 (`evt_a4_blood` 수혈) | `[겨울]` | `Night in the classroom shelter, close on a small student health record booklet open on a desk under a flashlight; {SEOJIN_WINTER}'s finger rests on one box of a blank or crossed-out row (no readable text). {NURSE} looks over her shoulder with a grave face, {TEACHER} +다리④ asleep on a mat behind them. Quiet, a decision forming.` |

- 혈액형 두 이벤트는 "맞지 않는다"와 "모른다"로 갈리지만, 둘 다 수혈을 못 하는 장면이라 한 장을 같이 쓴다. 지금 쓰는 수혈 컷은 `EVT_A4_BLOOD_MATCH`에만 남긴다.

## 3. 동료 작별 (4장, P1) — 50주~, 밤, 지금은 모두 `evt_a4_farewell`(사람 없는 복도)

| 파일 | 이벤트 | 시트 | 프롬프트 |
|---|---|---|---|
| `evt_farewell_dohyeon.png` | `EVT_DH_08_FAREWELL` | 서진·도현 | `Winter night in the classroom shelter, {DOHYEON} and {SEOJIN_WINTER} sit on the floor against the wall and hook their little fingers together in a pinky promise; he looks down, trying not to cry, a flashlight clutched in his other hand. One lantern, frost on the window.` `[겨울]` |
| `evt_farewell_harin.png` | `EVT_HR_05_FAREWELL` | 서진·하린 | `Winter night in front of the steel door of the school gym storage room, {HARIN} holds the door wide open with one hand, looking at faint old scratch marks below the handle; {SEOJIN_WINTER} stands a step behind her. Flashlight light, gym mats stacked inside the dark room. Quiet, heavy, a long-held apology.` `[겨울]` |
| `evt_farewell_nurse.png` | `EVT_NURSE_06_FAREWELL` | 서진·박 선생님 | `Winter night in the school infirmary, the medicine cabinet behind them nearly empty; {NURSE} places a worn pocket notebook with a frayed cover into {SEOJIN_WINTER}'s hands and closes the girl's fingers around it, a small proud smile. A candle on the steel desk.` `[겨울]` |
| `evt_farewell_yeji.png` | `EVT_BY_05_FAREWELL` | 서진·예지 | `Winter night at the window at the end of a school corridor, the first snow falling outside in the dark; {YEJI} takes a worn, many-times-folded letter from her blazer pocket and holds it out to {SEOJIN_WINTER} with both hands, eyes wet but steady. Snow light on their faces (no readable text).` `[겨울]` |

- 작별 4편은 그 동료가 함께 있을 때만 나온다. 받으면 지금 복도 컷은 쓰는 곳이 없어진다(파일은 남긴다).

## 4. 2·3막 이야기 장면 (3장, P2)

| 파일 | 이벤트 (지금 쓰는 그림) | 계절 | 프롬프트 |
|---|---|---|---|
| `evt_a2_staffroom.png` | `EVT_A2_STAFFROOM` (`evt_a2_records` 선생님이 읽는 컷) | `[여름]` | `Afternoon in an abandoned school staff room, a grey filing cabinet drawer pulled open, {SEOJIN_SUMMER} stands frozen holding a thin cardboard student record folder she has just found, her own photo clipped to it (blurred, no readable text). Dust in slanted sunlight, papers on desks.` |
| `evt_a3_police_outside.png` | `EVT_A3_POLICE_UNLOCK` (`evt_a3_police` 안쪽) | `[가을]` | `A small Korean neighborhood police box seen from the street in autumn, a patrol car sunk on flat tyres in front of it, fallen leaves on its hood; on the wall a large smeared painted arrow and a faded evacuation word (unreadable). No people, quiet, overcast.` `[F-KOREA]` |
| `evt_a3_police_board.png` | `EVT_A3_POLICE` (`evt_a3_police`) | `[가을]` | `Night in the classroom shelter, {TEACHER} +다리③ stands at the blackboard writing a word in chalk (unreadable), an open document folder with an official red stamp in his other hand; {SEOJIN} sits at a desk in front looking from the folder to the board. Lantern light, papers spread on the desk.` |

- `evt_a3_police`(안쪽 서류철 컷)는 그대로 두고 쓰지 않게 된다. 3절처럼 파일은 남긴다.

## 5. 인물이 있어야 하는 밤 장면 (5장, P2) — 지금은 빈 방 배경

| 파일 | 이벤트 (지금 쓰는 그림) | 계절 | 프롬프트 |
|---|---|---|---|
| `evt_a2_nurse_exam.png` | `EVT_A2_NURSE_EXAM` (`bg_infirmary`) | `[여름]` | `Night in the school infirmary, {NURSE} kneels and rewraps a clean white bandage around {TEACHER}'s left knee, the trouser leg rolled up, no wound visible; he sits on the bed edge, his homemade mop-handle crutch leaning nearby, jaw tight. Seen through a narrow door gap, with {SEOJIN_SUMMER}'s shoulder and eye at the edge of the frame. One desk lamp.` |
| `evt_sys_classroom_night.png` | `EVT_SYS_CLASSROOM_NIGHT` (`bg_classroom`) | `[봄]` | `First night in a new classroom shelter, curtains drawn over the windows, {TEACHER} +다리② sits on a student chair writing a new timetable on a sheet of paper on his knee, chalk dust on his cardigan sleeve; {SEOJIN} unrolls a sleeping mat between pushed-back desks. One candle.` |
| `evt_sys_storage_night.png` | `EVT_SYS_STORAGE_NIGHT` (`bg_storage`) | `[봄]` | `First night in the school's main storage room, {TEACHER} +다리② slides a long iron bar through the inside handles of the steel door as a bolt; {SEOJIN} holds the flashlight for him. Shelves of boxes and cleaning supplies around them, deep shadows.` |
| `evt_a2_locked_knock.png` | `EVT_A2_LOCKED_KNOCK` (`evt_a2_locked_house`) | `[여름]` | `Daytime in the dim stairwell of an old Korean low-rise villa, {SEOJIN_SUMMER} knocks softly on a closed steel apartment door; in the thin strip of light under the door, the shadow of feet inside has stopped. She holds her breath, one hand still raised.` `[F-KOREA]` |
| `evt_bc_reply_mart.png` | `EVT_BROADCAST_REPLY_MART` (`evt_bc_reply` 아파트 손전등) | `[가을]` | `Night in the school broadcasting room, {SEOJIN} leans back startled from a handheld radio on the desk as a different frequency cuts in, the radio's small screen glowing; she grabs a pencil to write the channel number on the wall paper. Console lights, dark windows.` |

## 6. 단서·소품 장면 (7장, P3)

| 파일 | 이벤트 (지금 쓰는 그림) | 계절 | 프롬프트 |
|---|---|---|---|
| `evt_rnd_a4_greeting.png` | `EVT_RND_A4_GREETING` (`evt_a3_dj_show` DJ 스튜디오) | `[겨울]` | `Winter night by a classroom window, {SEOJIN_WINTER} holds a handheld radio close to her mouth, listening to unfamiliar voices; through the frosted window, on the snow-covered flat rooftop of the building across the street, the letters SOS are painted huge in red. Moonlight on the snow.` `[F-KOREA]` |
| `evt_a1_pharmacy_unlock.png` | `EVT_A1_PHARMACY_UNLOCK` (`evt_a1_store_gate`) | `[봄]` | `Close view of a dusty convenience store counter, a pharmacy advertisement flyer lying on it with the pharmacy's location circled in pen (no readable text), {SEOJIN}'s hand pinning it flat; scattered receipts, an empty register drawer. Daylight through dirty glass.` |
| `evt_a1_secret_found.png` | `EVT_A1_SECRET_FOUND` (`bg_staffroom`) | `[봄]` | `Evening in the gym shelter, {TEACHER} +다리① has just read an official document and is folding it in half to tuck it inside his cardigan; {SEOJIN} catches a glimpse from across the mats, a scribble visible on the paper (no readable text). His face carefully calm.` |
| `evt_a1_infirmary.png` | `EVT_A1_INFIRMARY` (`bg_infirmary`) | `[봄]` | `Close view of an open glass medicine cabinet in an abandoned school infirmary: the upper shelves still neatly stocked, only the bottom shelf emptied, and a small handwritten note taped to the glass (no readable text). Soft afternoon light, no people.` |
| `evt_a1_classroom.png` | `EVT_A1_CLASSROOM` (`bg_classroom`) | `[봄]` | `Afternoon in a classroom at the end of the first floor, the door barricaded with stacked desks and chairs, warm sunlight through the windows; {SEOJIN} stands looking at the blackboard where a class name is written in chalk (unreadable), {TEACHER} +다리① sitting on a chair near the barricade.` |
| `evt_a1_storage_d2.png` | `EVT_A1_STORAGE_D2` (`evt_ng_storage`) | `[봄]` | `Night inside a cramped school gym storage room, {SEOJIN} stands on top of a vaulting box on tiptoe, looking out through a small high ventilation window at the empty moonlit schoolyard; gym mats and ball cages in the dark below her.` |
| `evt_a1_storage_d6.png` | `EVT_A1_STORAGE_D6` (`evt_ng_storage`) | `[봄]` | `Rainy night inside a cramped school gym storage room, {SEOJIN} reaches up and holds an empty plastic bottle under the small high ventilation window, catching rainwater dripping in; her face tired, a thin line of light under the locked door.` |

- 1주 창고 장면(`EVT_A1_STORAGE_D1~D6`)은 서진 혼자이고, 선생님은 6일 새벽에 목소리로만 나온다. 그림에 그리지 않는다.

---

## 7. 그대로 두는 것

장면이 같거나, 같은 대본을 조건만 바꿔 쓰는 이벤트라 새로 받지 않는다.

- **방을 처음 찾는 장면**(빈 방 배경이 맞음): `EVT_A1_ROOFTOP`·`_EARLY`, `EVT_A2_GYM`, `EVT_A2_SCIENCE`, `EVT_A2_RADIO_STATIC`, `EVT_SYS_ROOM_NURSE`·`_SCIENCE`·`_BROADCAST`.
- **조건만 다른 같은 대본**: `EVT_A3_REINFECTION`·`_EARLY`, `EVT_A4_FINALE_RAID`·`_KANGCHEOL`, `EVT_A4_WATERWORKS_UNLOCK`·`_LATE`, `EVT_A4_SAGEORI_TRUCE_END`.
- **같은 장면**: `EVT_A1_STORE_UNLOCK`, `EVT_A2_LOCKED_HOUSE`, `EVT_A2_TEACHER_KNOWS`·`_FINDS`, `EVT_A3_OPENING`·`EVT_A3_UBC_CALL`·`EVT_RND_A3_REQUEST`(무전 컷), `EVT_A4_BLOOD_MATCH`, `EVT_A4_FINAL_CHOICE`, 송출 4편, 응답 가족·아이, 휴대폰 2편, 창고 나머지 6편(`EVT_A1_TRAPPED`·`STORAGE_D1·D3·D4·D5`·`EVT_NG_STORAGE_7DAYS`).
- **마음 상태 `_3` 세 편**은 같은 옥상 컷을 쓰기로 12차에서 정했다.

## 8. 연결 (받으면 내가 한다)

- 도구 `code_temp/apogirl/tools/install_events12.py`로 16:9·너비 1920 jpg로 바꿔 `Resources/Art/Story/Events/`에 넣는다.
- `art_map.json` Events에서 위 표의 이벤트 줄을 새 그림으로 바꾼다. 혈액형 두 편은 `evt_a4_blood_other`를 같이 쓴다.
- 쓰는 곳이 없어진 옛 그림(`evt_a4_farewell`, `evt_a3_police` 등)은 지우지 않는다.

## 9. 받는 차례 (추천)

1. 2절 3장 + 3절 4장 (P1): 4막의 가장 무거운 장면이다.
2. 4절 3장 → 5절 5장 (P2).
3. 6절 7장 (P3).
