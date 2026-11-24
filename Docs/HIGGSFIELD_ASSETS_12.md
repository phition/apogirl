# 힉스필드 에셋 12차: 동아리 컷 다시 그리기 · 허브 도트 동료·고양이 · 빈 컷 채우기 (2026-11-24)

이번 차례에 받을 것은 세 가지다.

- **(가) 동아리 컷 다시 그리기.** 8차 3.1의 동아리 컷 4장은 프롬프트가 "two people"이었고, 인물 문구도 참고 시트도 없었다. 그래서 주인공 서진과 도현이 설정과 다른 사람으로 나왔다.
  - 서진의 설정: 검은 단발, 남색 재킷, 체크 치마.
  - 도현의 설정: 초록 트랙 재킷, 청바지, 이마 반창고.
  - 대본은 **같이 한 동료마다 갈린다**(`CLUB_WITH_{NPC}`). 그래서 활동마다 동료 6명 + 혼자 = 7장씩 받는다. 개인 장면 6편(`EVT_CLUB_BOND_*`)은 아직 그림이 없어 함께 받는다.
- **(나) 허브 단면도 도트 스프라이트.** 다음 그림을 받는다.
  - 치즈색 고양이: 지금은 코드로 만든 삼색 임시 그림이다.
  - 2학년 선배·유랑자: 단면도 그림이 없어서 합류해도 단면도에 나오지 않는다.
  - 도현: 남색 재킷·회색 바지로 그려져 있어 설정과 다르다.
- **(다) 그림이 빈 이벤트 채우기.** 이벤트 281개 가운데 53개가 대화 뒤 그림 없이 나온다. 이번에 받는 것은 다음과 같다. 무전 신청곡·인사 두 편은 있는 그림을 쓴다.
  - 위기 이탈 5
  - 마음 상태 흐름 10편(8장)과 좋아하는 과목 1편
  - 윤 선생님 대화 9와 막 평가 3
  - 일상 무작위 이벤트 17

**13차를 먼저 받았다 (2026-11-24).** [HIGGSFIELD_ASSETS_13.md](HIGGSFIELD_ASSETS_13.md)는 8차 이벤트 컷 가운데 다른 사람이 나온 컷을 다시 받는 문서다. 사용자가 이 12차보다 먼저 받았다. 그래서 이 문서를 다음과 같이 고쳤다.

- 옛 4.5절(초복 삼계탕·평면도 다시 그리기 2장)은 13차 2.4·2.5로 받았다. 이 문서에서는 받지 않는다(4.5절은 안내만 남겼다).
- 받은 13차 18장은 `ArtInbox/illustrations/redo13/`에 있다. 게임에 덮어쓰는 일은 13차 문서를 따른다.
- 인물 문구를 13차 1절과 같게 맞췄다(0.2).
  - 도현은 곧은 머리다. 13차 검토에서 곱슬로 나온 컷이 있었다.
  - 서진은 계절별 차림 `{SEOJIN_SUMMER}`·`{SEOJIN_WINTER}`를 쓴다.
  - 윤 선생님 다리는 13차의 ①~④ 문구를 쓴다.
- **13차 결과를 보조 참고 이미지로 쓸 수 있다.** 인물이 둘 이상 나오는 컷에서 그림체와 사람 사이 키·크기 비율을 맞추는 데 쓴다. 참고는 4장까지이므로 **인물 시트를 먼저 넣고**, 자리가 남을 때만 13차 컷을 한 장 더한다.

| 이 문서의 컷 | 보조 참고로 좋은 13차 컷 |
|---|---|
| 서진·도현이 나오는 컷 (1.3 운동, 2 도현 개인 장면, 4.1 도현 위기 이탈) | `redo13/evt_bc_first.png`, `redo13/evt_a3_finale.png` |
| 윤 선생님이 나오는 컷 (4.2 마음 상태, 4.3 대화·막 평가, 4.4 선생님 통증) | `redo13/evt_a1_power_plan.png`(1막), `redo13/evt_cal_vacation.png`(2막), `redo13/evt_a3_mart_news.png`(3막), `redo13/evt_cal_dongji.png`(병세) |
| 여럿이 모인 컷 (4.2 `evt_mind_laugh_2`·`_face_2`, 4.3 `evt_a2_review`·`evt_a3_review`, 4.4 정전·열대야·추운 밤) | `redo13/evt_a3_dj_research.png`, `redo13/evt_hr_first_night.png` |
| 박 선생님이 나오는 컷 (1.x 박 선생님 짝, 2 박 선생님 개인 장면, 4.1, 4.3 `evt_talk_08`) | `redo13/evt_a3_diagnosis.png` |
| 2학년 선배가 나오는 컷 (1.x 선배 짝, 2 선배 개인 장면, 4.1) | `redo13/evt_bond_senior_3.png` |

쓰는 법:

- 쓰는 법·모델·공통 공식은 [HIGGSFIELD_ASSETS.md](HIGGSFIELD_ASSETS.md) 0장을 따른다.
  - `[F-ILLU]` = 0.3 일러스트 공식, `[F-KOREA]` = 한국 건물 문구, `[F-PIXEL]` = 탐험 픽셀 공식, `[안전]` = 0.4 안전 문구.
  - 계절 문구 `[봄]`·`[여름]`·`[가을]`·`[겨울]`은 [HIGGSFIELD_ASSETS_8.md](HIGGSFIELD_ASSETS_8.md) 0.3에 있다.
- 프롬프트는 영어, 설명은 한국어로 쓴다. 표의 프롬프트 칸 하나가 생성 하나다.
- **작업 제출은 사용자가 한다.** 나(Claude)는 이 문서와 참고 이미지만 만든다.
- 이벤트 컷은 16:9(2k 이상), 스프라이트 시트는 3장의 규격을 따른다.

---

## 0. 한눈에 보기

### 0.1 요약표

| 번호 | 파일 | 쓰이는 곳 | 우선 | 개수 |
|---|---|---|---|---|
| 1 | `evt_club_{활동}_{동료}`, `evt_club_{활동}_solo` | 동아리 활동 4종 × (동료 6 + 혼자) | **P1** | 28 |
| 2 | `evt_club_bond_{동료}` | 동아리 개인 장면 6편 | **P1** | 6 |
| 3 | `hub_cat_idle`·`hub_cat_walk`·`hub_dohyeon_idle`·`hub_senior_idle`·`hub_wanderer_idle` 시트 | 허브 단면도 | **P1** | 시트 5 |
| 3.6 | `hub_{동료}_walk` 시트 6 | 허브 단면도 동료 걷기 (지금은 대기 그림이 미끄러진다) | P3 | 시트 6 |
| 4.1 | `evt_crisis_leave_{동료}` | 위기 이탈 5편 | P2 | 5 |
| 4.2 | `evt_mind_*`, `evt_favorite_found` | 프롤로그의 메아리 (마음 상태 흐름) | P2 | 9 |
| 4.3 | `evt_talk_{02~11}`, `evt_a{1~3}_review` | 윤 선생님 대화·막 평가 | P3 | 12 |
| 4.4 | `evt_rnd_*` | 일상 무작위 이벤트 | P3 | 17 |
| 4.5 | ~~`evt_cal_chobok`, `evt_a2_plan_next`~~ | **13차 2.4·2.5로 받음** — 여기서는 받지 않는다 | — | 0 |

**먼저 받을 것**: 1장에서 ★ 표시한 11장. 동료가 좋아하는 활동 짝 6장, 도현 짝 3장, 혼자 2장이다. 그다음 2장과 3장을 받는다.

### 0.2 공통 문구

**[일관성]** — 인물이 나오는 모든 컷의 `[F-ILLU]` 바로 뒤에 붙인다. 이번 차례에서 가장 중요한 줄이다.
```
Character consistency: draw every named character exactly as in the attached character reference sheets: same face, same hairstyle and hair color, same outfit and colors. Do not invent new characters or change clothes. Only the characters named in this prompt appear.
```

**인물 문구.** 해당 시트(`ArtInbox/illustrations/char_*_sheet.png`)를 **꼭 참고 이미지로 넣고**, 프롬프트에는 아래 문구를 그대로 쓴다. 시트를 보고 8차 문구보다 자세히 고쳤다.

| 인물 | 시트 | 문구 |
|---|---|---|
| 주인공 서진 | `char_seojin_sheet.png` | `Seojin, a 16-year-old Korean girl with a straight black bob just above the shoulders and straight bangs, navy school blazer, white shirt with a navy striped tie, navy plaid pleated knee-length skirt, black opaque tights, dark canvas sneakers` |
| 서진 (여름) `{SEOJIN_SUMMER}` | 같은 시트 | `Seojin, a 16-year-old Korean girl with a straight black bob just above the shoulders and straight bangs, in a summer school uniform: white short-sleeved shirt with a navy striped tie, a beige knit sweater vest, navy plaid pleated knee-length skirt` |
| 서진 (겨울) `{SEOJIN_WINTER}` | 같은 시트 | `Seojin, a 16-year-old Korean girl with a straight black bob just above the shoulders and straight bangs, a worn dark padded jacket over her navy school uniform, navy plaid skirt, black tights` |
| 서진 (체육복) | 같은 시트 | `Seojin, a 16-year-old Korean girl with a straight black bob and straight bangs, in a plain dark navy school gym jacket and navy track pants` |
| 도현 | `char_dohyeon_sheet.png` | `Dohyeon, a thin 15-year-old Korean boy with short messy straight black hair, a small adhesive bandage on his forehead, an oversized dark green track jacket with white stripes down the sleeves, white T-shirt, faded blue jeans, black sneakers` |
| 박 선생님 (보건) | `char_nurse_sheet.png` | `Nurse Park, a Korean school nurse in her 40s with a neat short dark bob, a white nurse gown over an olive quilted padded vest, sleeves rolled up, a small pouch on her belt, grey trousers, brown shoes` |
| 송예지 | `char_bystander_sheet.png` | `Yeji, a slim 16-year-old Korean girl with long straight black hair tied low, a slightly oversized navy school blazer, white shirt, navy pleated skirt, white socks and white sneakers, shy posture` |
| 최하린 | `char_ringleader_sheet.png` | `Harin, a thin 16-year-old Korean girl with shoulder-length brown hair, sharp but tired eyes, an oversized olive-green hooded parka over a navy school uniform, black tights, dark loafers` |
| 2학년 선배 | `char_senior_sheet.png` | `the senior, a tall quiet 17-year-old Korean boy with messy dark hair falling over his eyes, a faded dark navy zip-up gym jacket over a white shirt, dark trousers, white sneakers, a thin pale fully healed scar along his left jaw; he never speaks and writes in a small notebook instead` |
| 유랑자 | `char_wanderer_sheet.png` | `the wanderer, a sun-weathered Korean man in his 50s, a worn wide-brim canvas hat, a faded olive hiking jacket, cargo trousers, hiking boots, a big old backpack with a rolled bedroll, a wooden walking stick` |
| 윤 선생님 | `char_teacher_sheet.png` | `Mr. Yoon, a Korean male teacher in his mid-40s with short black hair, thin glasses and a grey cardigan over a white shirt, dark trousers` + [13차](HIGGSFIELD_ASSETS_13.md) 1절의 **다리 문구 ①~④** 가운데 그 막 것 |

- 프롬프트 안의 자리 표시: `{SEOJIN}` 서진, `{DOHYEON}` 도현, `{NURSE}` 박 선생님, `{YEJI}` 송예지, `{HARIN}` 최하린, `{SENIOR}` 2학년 선배, `{WANDERER}` 유랑자, `{TEACHER}` 윤 선생님. 자리 표시를 위 문구로 바꾼다.
- 계절이 붙은 컷(4장의 `[여름]`·`[겨울]`)에서 서진은 `{SEOJIN_SUMMER}`·`{SEOJIN_WINTER}`를 쓴다. 표에는 그렇게 바꿔 적었다.
- 동아리는 14주부터 열리고(여름~겨울), 밤 행동이다. 그래서 동아리 컷은 **계절 문구 없이 밤 실내**로 받는다. 서진은 재킷 차림이 기본이고, 운동 컷만 체육복이다.
- 선배의 흉터는 "가늘고 다 나은 흰 흉터"로만 그린다(`[안전]`: 상처·피 없음).
- 유랑자는 동아리 무렵 이미 동료라서 얼굴을 가리지 않는다. 모자 그늘이 조금 지는 정도로 둔다.

**[밤 실내]** — 동아리 컷의 빛
```
Night inside an abandoned Korean high school, windows blacked out with cardboard and curtains, the only light is a small camping lantern or candles, warm pool of light, deep soft shadows, quiet and safe mood.
```

---

## 1. 동아리 활동 컷 다시 그리기 (28장, P1)

- **모델**: `gpt_image_2_6`, 16:9, 2k. 참고 이미지는 서진 시트 + 그 동료 시트, 모두 2장이다(혼자 컷은 서진 시트만).
- **프롬프트** = `[F-ILLU]` + `[일관성]` + `[밤 실내]` + 아래 장면 칸 + `[안전]`. 장면 칸의 `{SEOJIN}`·`{도현}` 같은 자리에는 0.2 인물 문구를 넣는다.
- **넣는 곳**: `ArtInbox/illustrations/club/` 아래 파일 이름 그대로.
- **잇는 곳** (연결할 때 내가 한다):
  - `art_map.json` Events에 이벤트마다 7줄을 둔다. 동료 컷 6줄은 `"Flag": "CLUB_WITH_{NPC}"`가 붙고, 혼자 컷 1줄은 깃발이 없다.
  - 이벤트 그림을 고르는 코드 `StoryArt.EventImage`가 지금 켜진 깃발과 맞는 줄을 먼저 고르게 고친다. 맞는 줄이 없으면 깃발 없는 줄을 쓴다.
  - 옛 4장(`evt_club_boardgame`·`_sing`·`_exercise`·`_studygroup`)은 백업으로 옮긴다.
- 대본 위치는 `Resources/Dialogue/Events/Club.yarn`이다. 장면은 그 동료 분기의 문장을 그대로 옮겼다.
- ★ = 먼저 받을 11장.

### 1.1 보드게임 `EVT_CLUB_BOARDGAME` — 교실 바닥, 낡은 장기판·윷·카드

| | 파일 | 깃발 | 장면 (영어) |
|---|---|---|---|
| ★ | `evt_club_boardgame_wanderer.png` | `CLUB_WITH_NPC_WANDERER` | `{SEOJIN} and {WANDERER} sit on a mat on the classroom floor over an old wooden Korean janggi board. The wanderer places the pieces in silence with practiced hands; Seojin, having lost three games, looks at the board with a wry frown. The wanderer has set one wrapped hard candy on the edge of the board as a small consolation. Lantern between them.` |
| ★ | `evt_club_boardgame_dohyeon.png` | `CLUB_WITH_NPC_DOHYEON` | `{SEOJIN} and {DOHYEON} play yut on a blanket on the classroom floor. Dohyeon has jumped up mid-cheer with both arms raised after a lucky throw, four wooden yut sticks scattered on the blanket; Seojin laughs, covering her mouth. A hand-drawn yut board and bottle-cap pieces between them.` |
| | `evt_club_boardgame_nurse.png` | `CLUB_WITH_NPC_NURSE` | `{SEOJIN} and {NURSE} play cards at a desk pushed against another. Nurse Park shuffles a worn deck with surprisingly skilled hands and a gentle teasing smile; Seojin holds her cards close, losing again. A tin cup of tea and the lantern on the desk.` |
| | `evt_club_boardgame_ringleader.png` | `CLUB_WITH_NPC_RINGLEADER` | `{SEOJIN} and {HARIN} play cards sitting cross-legged on the floor. Harin grips her cards tightly, lips pressed together trying not to show she is winning, eyes narrowed at Seojin; Seojin raises her hands as if to say she did not peek.` |
| | `evt_club_boardgame_bystander.png` | `CLUB_WITH_NPC_BYSTANDER` | `{SEOJIN} and {YEJI} lean over a janggi board on a desk. Yeji quietly points one finger at a square to correct Seojin's move, a small shy smile, almost laughing; Seojin looks surprised.` |
| | `evt_club_boardgame_senior.png` | `CLUB_WITH_NPC_SURVIVOR_A` | `{SEOJIN} and {SENIOR} play cards at a desk. The senior studies his hand without a word, one eyebrow raised very slightly at a good card; Seojin watches his face closely, trying to read him. A small notebook and pencil lie by his elbow.` |
| ★ | `evt_club_boardgame_solo.png` | (없음 = 기본) | `{SEOJIN} sits alone on the classroom floor playing cards against herself, two hands of cards laid out on either side, holding one card in each hand, a small amused half-smile. Empty desks around her, one lantern.` |

### 1.2 노래 `EVT_CLUB_SINGING` — 음악실, 천장을 비춘 손전등, 아주 작은 소리

| | 파일 | 깃발 | 장면 (영어) |
|---|---|---|---|
| ★ | `evt_club_sing_senior.png` | `CLUB_WITH_NPC_SURVIVOR_A` | `In a dark school music room, a flashlight stands upright on the floor pointing at the ceiling. {SENIOR} sits on a stool with an acoustic guitar on his knee, softly mouthing the lyrics without sound; {SEOJIN} stands beside him singing quietly, one hand on the upright piano lid.` |
| ★ | `evt_club_sing_bystander.png` | `CLUB_WITH_NPC_BYSTANDER` | `In a dark school music room lit by a flashlight pointed at the ceiling, {SEOJIN} sings softly while {YEJI} sits beside her on the risers, tapping the rhythm with her fingers on her knee, just beginning to sing along, eyes half closed, a clear quiet voice.` |
| ★ | `evt_club_sing_dohyeon.png` | `CLUB_WITH_NPC_DOHYEON` | `In a school music room lit by a flashlight on the floor, {DOHYEON} sings a dramatic game soundtrack with no words, fist raised like a boss fight; {SEOJIN} is bent over laughing so hard she holds her stomach.` |
| | `evt_club_sing_nurse.png` | `CLUB_WITH_NPC_NURSE` | `In a quiet school music room lit by one flashlight, {NURSE} sits on a chair humming a lullaby with her eyes gently closed, hands folded; {SEOJIN} sits on the floor hugging her knees, listening, very still.` |
| | `evt_club_sing_ringleader.png` | `CLUB_WITH_NPC_RINGLEADER` | `In a school music room lit by a flashlight, {HARIN} holds a rolled-up notebook like a microphone, singing an unfamiliar song and getting the words wrong, half embarrassed, half stubborn; {SEOJIN} sings next to her reading lyrics from a notebook.` |
| | `evt_club_sing_wanderer.png` | `CLUB_WITH_NPC_WANDERER` | `In a school music room lit by a flashlight, {WANDERER} whistles a mountain bird call, lips pursed, a faint smile under the hat brim; {SEOJIN} tries to copy it, cheeks puffed, only air coming out.` |
| | `evt_club_sing_solo.png` | (없음 = 기본) | `{SEOJIN} sits alone at the closed piano in a dark school music room, humming quietly to herself, the flashlight standing on the floor and pointing at the ceiling, music stands and chairs in shadow.` |

### 1.3 운동 `EVT_CLUB_SPORTS` — 체육관 바닥 매트, 잠근 문

서진은 **체육복 문구**를 쓴다.

| | 파일 | 깃발 | 장면 (영어) |
|---|---|---|---|
| ★ | `evt_club_sports_dohyeon.png` | `CLUB_WITH_NPC_DOHYEON` | `In a dim school gymnasium, gym mats on the floor, one lantern. {SEOJIN} jumps rope mid-air, counting under her breath, sweat on her forehead; {DOHYEON} kneels on the mat counting along with his fingers, grinning, a notebook with tally marks beside him.` |
| ★ | `evt_club_sports_ringleader.png` | `CLUB_WITH_NPC_RINGLEADER` | `In a dim school gymnasium after running laps, {SEOJIN} and {HARIN} lie flat on their backs side by side on a gym mat, arms spread, breathing hard, staring at the high ceiling; Harin is hiding a small smile. A lantern by the wall.` |
| | `evt_club_sports_nurse.png` | `CLUB_WITH_NPC_NURSE` | `In a dim school gymnasium, {NURSE} leads a stretching routine on a gym mat, reaching one arm over her head with a wince and a laugh; {SEOJIN} copies the stretch beside her, laughing too.` |
| | `evt_club_sports_senior.png` | `CLUB_WITH_NPC_SURVIVOR_A` | `In a dim school gymnasium under a basketball hoop, {SENIOR} gently adjusts {SEOJIN}'s elbow as she holds a half-deflated basketball ready to shoot, both looking up at the rim. Lantern light from the floor.` |
| | `evt_club_sports_bystander.png` | `CLUB_WITH_NPC_BYSTANDER` | `In a dim school gymnasium, {SEOJIN} and {YEJI} do push-ups side by side on one gym mat, Yeji concentrating hard, quietly counting; a lantern on the floor in front of them.` |
| | `evt_club_sports_wanderer.png` | `CLUB_WITH_NPC_WANDERER` | `In a dim school gymnasium, {SEOJIN} climbs the bleacher steps carrying a heavy backpack, legs shaking; {WANDERER} climbs beside her at a steady pace with his own big pack, calm, walking stick in hand.` |
| ★ | `evt_club_sports_solo.png` | (없음 = 기본) | `{SEOJIN} jumps rope alone in the middle of a dim school gymnasium, one lantern on the floor, her shadow huge on the wall, gym mats rolled up by the wall.` |

### 1.4 공부 `EVT_CLUB_STUDY` — 붙인 책상 두 개, 촛불 하나, 반쯤 젖은 문제집

| | 파일 | 깃발 | 장면 (영어) |
|---|---|---|---|
| ★ | `evt_club_study_nurse.png` | `CLUB_WITH_NPC_NURSE` | `Two classroom desks pushed together with one candle in the middle. {NURSE} quizzes {SEOJIN} on first aid: Seojin wraps a roll of white bandage around Nurse Park's forearm for practice, concentrating; Nurse Park watches with the soft proud face of a teacher. Water-stained workbooks pushed aside.` |
| ★ | `evt_club_study_dohyeon.png` | `CLUB_WITH_NPC_DOHYEON` | `Two classroom desks pushed together with one candle. {DOHYEON} slumps over an open math workbook with his cheek on the page, pouting; {SEOJIN} taps the page with her pencil, telling him to keep going.` |
| | `evt_club_study_bystander.png` | `CLUB_WITH_NPC_BYSTANDER` | `Two desks pushed together with one candle. {YEJI} draws tiny doodles in the corner of her notebook next to her notes, a candle, an eraser, a hand; {SEOJIN} leans over to look, then tries drawing in her own notebook.` |
| | `evt_club_study_ringleader.png` | `CLUB_WITH_NPC_RINGLEADER` | `Two desks pushed together with one candle. {HARIN} holds an English vocabulary booklet close to her face, silently counting words on her fingers; {SEOJIN} studies her own booklet across from her, glancing up.` |
| | `evt_club_study_senior.png` | `CLUB_WITH_NPC_SURVIVOR_A` | `Two desks pushed together with one candle. {SENIOR} explains a physics problem by drawing a few arrows in a shared notebook instead of speaking; {SEOJIN} leans in to read it, pencil in hand.` |
| | `evt_club_study_wanderer.png` | `CLUB_WITH_NPC_WANDERER` | `Two desks pushed together with one candle. {WANDERER} traces the contour lines of a school atlas with a weathered finger, explaining; {SEOJIN} listens closely, as if hearing it for the first time.` |
| | `evt_club_study_solo.png` | (없음 = 기본) | `{SEOJIN} studies alone at two desks pushed together, one candle, a water-stained workbook with colored index tabs, writing the date at the top of a notebook page.` |

---

## 2. 동아리 개인 장면 (6장, P1)

좋아하는 활동을 세 번 함께 하면 열리는 그 동료의 이야기다(`ClubRules`, 대본 `Club.yarn` `EVT_CLUB_BOND_*`).

- 모델·참고 이미지·공식은 1장과 같다.
- **넣는 곳**: `ArtInbox/illustrations/club/`
- **잇는 곳**: `art_map.json` Events에 한 줄씩 넣는다(깃발 없음).

| 파일 | 이벤트 | 장면 (영어) |
|---|---|---|
| `evt_club_bond_nurse.png` | `EVT_CLUB_BOND_NURSE` | `After a candlelit study session, {NURSE} holds {SEOJIN}'s hand palm up, looking at the small calluses from wrapping bandages, eyes soft and a little wet; an old pocket nurse's notebook lies open on the desk beside the candle (no readable text).` |
| `evt_club_bond_dohyeon.png` | `EVT_CLUB_BOND_DOHYEON` | `In a dim gymnasium after jump rope, {DOHYEON} sits up on a gym mat showing {SEOJIN} a worn notebook filled with rows of daily tally marks, proud and a little shy; Seojin, lying on the mat beside him, props herself up on one elbow to look. A jump rope coiled on the mat.` |
| `evt_club_bond_ringleader.png` | `EVT_CLUB_BOND_RINGLEADER` | `In a dim gymnasium after ten laps, {HARIN} stands and holds out her hand to {SEOJIN}, who is still lying on the gym mat; Harin looks away while offering it, a small unguarded smile. Lantern light from the side.` |
| `evt_club_bond_bystander.png` | `EVT_CLUB_BOND_BYSTANDER` | `In the dark school music room, {YEJI} sings the first line of a song with her eyes closed and her hands clasped tight, voice trembling; {SEOJIN} sits close beside her, singing a soft harmony underneath. Flashlight pointed at the ceiling.` |
| `evt_club_bond_senior.png` | `EVT_CLUB_BOND_SENIOR` | `In the dark school music room, {SENIOR} plays a slow unfinished song on his acoustic guitar with no sheet music; {SEOJIN} sits on the floor beside him humming along, and he glances at her mid-chord with quiet surprise. His small notebook lies open on the piano bench (no readable text).` |
| `evt_club_bond_wanderer.png` | `EVT_CLUB_BOND_WANDERER` | `Over a janggi board on the classroom floor, {WANDERER} holds out one wrapped hard candy on his weathered palm, looking at it rather than at {SEOJIN}, a quiet grief under the hat brim; only a few candies are left in a small tin beside the board. Seojin watches him silently.` |

---

## 3. 허브 단면도 도트 스프라이트 (P1 시트 5장 + P3 시트 6장)

허브 단면도는 학교를 옆에서 자른 그림이다. 동료와 고양이는 맡은 방에 서 있다가 방 사이를 걷는다.

- 지금 그림: `Resources/Art/ExplorationPixel/hub_*_idle_{0~15}.png`, 96 × 78 화소, 16프레임. 처음엔 PixelLab으로 만들었다.
- 이번에는 **그 그림체·크기에 맞춘 시트**를 받는다. 칸을 나누고 줄이는 일은 내가 한다.

### 3.1 시트 규격 (모든 시트 공통)

| 항목 | 값 |
|---|---|
| 모델 | `gpt_image_2_6` (`background: transparent`). 픽셀이 뭉개지면 `nano_banana_flash` |
| 비율 | **4:3** (5:4가 있으면 5:4). 4열 × 4행 = **16칸**, 칸 크기가 모두 같게 |
| 배경 | 투명. 투명이 안 되면 단색 밝은 회색(나중에 지운다) |
| 방향 | 모든 칸이 **오른쪽을 본다** (옆모습 또는 4분의 3 옆모습). 게임에서 좌우를 뒤집어 왼쪽도 쓴다 |
| 발 | 모든 칸에서 발(고양이는 발바닥)이 **같은 높이의 바닥 줄**에 닿는다 |
| 크기 | 칸 안에서 사람은 칸 높이의 약 85%, 고양이는 앉은 키가 약 35%다. 지금 그림(참고 이미지)과 같은 비율로 맞춘다 |
| 순서 | 왼쪽 위에서 오른쪽으로, 줄을 바꿔 1~16번. **16번 다음이 1번으로 자연스럽게 이어진다**(도는 동작) |
| 넣는 곳 | `ArtInbox/sprites12/{파일}_sheet.png` |

**[F-HUB]** — 모든 시트 프롬프트 앞에 붙인다.
```
2D pixel art sprite sheet for a side-view game, 4 columns by 4 rows, 16 equal cells, transparent background, one character per cell, all cells facing right, feet on the same baseline in every cell, frames in reading order form a smooth seamless loop. Crisp chunky pixels, at most 24 colors, 1-pixel dark outline, flat shading with a single highlight tone, no anti-aliasing, no blur, no gradient, no text, no grid lines. Match the pixel art style, outline thickness, palette softness and character scale of the attached reference sprite strip exactly.
```

**참고 이미지.** 내가 만들어 두었다: `ArtInbox/sprites12/ref/` (git 밖).

| 파일 | 내용 | 쓰는 시트 |
|---|---|---|
| `hub_nurse_idle_ref.png` | 지금 박 선생님 대기 8장을 4배로 이어 붙인 것. **그림체·크기의 기준** | 사람 시트 모두 |
| `hub_teacher_cane_idle_ref.png` | 지금 윤 선생님(지팡이) 대기 8장, 4배. 어른 남자의 크기 기준 | 유랑자 |
| `hub_dohyeon_idle_old_ref.png` | 지금 도현 대기 8장, 4배. **움직임만** 참고하고, 옷 색은 따르지 않는다 | 도현 |
| `hub_player_walk_ref.png` | 지금 주인공 걷기 8장, 4배. 걸음 박자의 기준 | 걷기 시트 |
| `hub_cat_old_ref.png` | 지금 임시 고양이 8장, 4배. **크기만** 참고하고, 색·모양은 따르지 않는다 | 고양이 |
| `cat_illust_feed.jpg`, `cat_illust_lap.jpg`, `cat_illust_avatar.png` | 이벤트 컷·아바타의 치즈 고양이. **고양이 생김새의 기준** | 고양이 |

인물 시트는 `ArtInbox/illustrations/char_*_sheet.png`를 함께 넣는다. 참고 이미지는 3장 이하로 둔다.

### 3.2 치즈색 고양이 '급식' — 대기 `hub_cat_idle` (P1)

| 항목 | 값 |
|---|---|
| 참고 | `hub_nurse_idle_ref.png`(그림체), `hub_cat_old_ref.png`(크기), `cat_illust_avatar.png`(생김새) |
| 쓰는 곳 | 고양이가 방에 있을 때. 지금은 삼색 임시 그림 `hub_cat`이다 |

```
[F-HUB] Subject: an orange tabby cat (cheese-colored ginger cat with darker orange stripes, cream white chest, muzzle and paws, amber-yellow half-lidded eyes, a slightly grumpy but calm face), sitting upright in side view facing right, tail curled around its front paws. Idle loop over 16 frames: slow breathing, tail tip flicks twice, one ear twitches, a slow blink around frames 9 to 11, head turns slightly toward the viewer and back. Small and compact; the cat's sitting height is about one third of a standing person.
```

### 3.3 치즈색 고양이 — 걷기 `hub_cat_walk` (P1)

| 항목 | 값 |
|---|---|
| 참고 | 3.2에서 받은 시트(생김새를 맞추려고 **꼭** 넣는다), `cat_illust_feed.jpg`, `hub_cat_old_ref.png` |
| 쓰는 곳 | 고양이가 방 사이를 옮겨 다닐 때 |

```
[F-HUB] Subject: the same orange tabby cat as the attached sprite sheet (cheese-colored ginger with darker orange stripes, cream white chest and paws, amber eyes), walking in place in side view facing right. Relaxed four-legged walk cycle over 16 frames: diagonal leg pairs alternate, shoulders and hips roll gently, head stays level, tail held up in a soft curve and swaying slightly. The cat does not move across the cell; the walk stays in place.
```

### 3.4 도현 다시 그리기 — 대기 `hub_dohyeon_idle` (P1)

| 항목 | 값 |
|---|---|
| 참고 | `char_dohyeon_sheet.png`(생김새·옷), `hub_dohyeon_idle_old_ref.png`(움직임), `hub_nurse_idle_ref.png`(그림체) |
| 왜 | 지금 단면도 도현은 남색 재킷·회색 바지다. 설정화·이벤트 컷의 초록 트랙 재킷·청바지와 다르다 |

```
[F-HUB] Subject: Dohyeon, a thin 15-year-old Korean boy with short messy black hair and a small adhesive bandage on his forehead, an oversized dark green track jacket with white stripes down the sleeves, white T-shirt, faded blue jeans, black sneakers, standing in side view facing right with his hands in his jacket pockets. Idle loop over 16 frames: restless breathing, shifts his weight from foot to foot, glances back over his shoulder once (frames 7 to 10), a quick blink. Same pose and timing as the attached old sprite strip, but with the green jacket and blue jeans from the character sheet.
```

### 3.5 새 동료 2명 — 대기 (P1)

지금은 단면도 그림이 없어서 두 사람이 합류해도 단면도에 나오지 않는다(`hub_section.json` `NpcSprites`에 줄이 없다).

**`hub_senior_idle`** — 2학년 선배 (`NPC_SURVIVOR_A`)

| 항목 | 값 |
|---|---|
| 참고 | `char_senior_sheet.png`, `hub_nurse_idle_ref.png` |

```
[F-HUB] Subject: a tall quiet 17-year-old Korean boy with messy dark hair falling over his eyes, a faded dark navy zip-up gym jacket over a white shirt, dark trousers, white sneakers, a small notebook held in one hand at his side, standing in side view facing right, slightly hunched. He is a little taller than the other teenage sprites. Idle loop over 16 frames: slow calm breathing, lifts the notebook and glances at it once (frames 6 to 11) then lowers it, hair sways slightly, one slow blink.
```

**`hub_wanderer_idle`** — 유랑자 (`NPC_WANDERER`)

| 항목 | 값 |
|---|---|
| 참고 | `char_wanderer_sheet.png`, `hub_teacher_cane_idle_ref.png`(어른 크기), `hub_nurse_idle_ref.png` |

```
[F-HUB] Subject: a sun-weathered Korean man in his 50s, worn wide-brim canvas hat, faded olive hiking jacket, cargo trousers, hiking boots, a big old backpack with a rolled bedroll on top, leaning lightly on a tall wooden walking stick in his front hand, standing in side view facing right. Adult height, same scale as the attached adult sprite. Idle loop over 16 frames: steady slow breathing, adjusts a backpack strap with his free hand once (frames 5 to 9), tips the hat brim slightly, a slow blink.
```

**잇는 곳** (3.2~3.5): 내가 한다.

- 시트를 16칸으로 나누고 96 × 78로 줄인다. 발 줄은 지금 그림과 맞춘다.
- `hub_cat_idle_{0~15}`·`hub_cat_walk_{0~15}`로 넣는다. 옛 임시 고양이 그림(`hub_cat`·`hub_cat_idle_0~7`)은 백업으로 옮긴다. `hub_dohyeon_idle_{0~15}`는 덮어쓴다(옛 그림은 백업).
- `hub_section.json` `NpcSprites`에 `NPC_SURVIVOR_A → hub_senior`, `NPC_WANDERER → hub_wanderer`를 더한다.
- 단면도는 지금 동료에게 대기 그림만 쓴다(`HubSectionView` `SetLook(idle, null)`). 그래서 `{키}_walk_` 그림이 있으면 걸을 때 그것을 쓰게 고친다.

### 3.6 동료 걷기 시트 6장 (P3, 나중)

지금 동료는 방 사이를 걸을 때 대기 그림인 채로 미끄러진다. 걷기 그림이 있으면 3.5 끝의 고침으로 바로 걷는다.

| 파일 | 참고 | 대상 |
|---|---|---|
| `hub_dohyeon_walk` | 3.4 결과 시트, `hub_player_walk_ref.png` | 도현 |
| `hub_nurse_walk` | `hub_nurse_idle_ref.png`, `hub_player_walk_ref.png` | 박 선생님 |
| `hub_bystander_walk` | 지금 `hub_bystander_idle` 4배(필요하면 내가 만든다), `hub_player_walk_ref.png` | 송예지 |
| `hub_ringleader_walk` | 지금 `hub_ringleader_idle` 4배, `hub_player_walk_ref.png` | 최하린 |
| `hub_senior_walk` | 3.5 결과 시트, `hub_player_walk_ref.png` | 2학년 선배 |
| `hub_wanderer_walk` | 3.5 결과 시트, `hub_player_walk_ref.png` | 유랑자 (지팡이를 짚으며) |

공통 프롬프트 (`{인물}` 자리에 3.4·3.5의 Subject 문장을 넣는다):
```
[F-HUB] Subject: {인물}, walking in place in side view facing right, the same character as the attached sprite sheet. Calm indoor walk cycle over 16 frames with the same leg rhythm as the attached walk strip: contact, down, passing, up for each foot; arms swing gently; the character does not move across the cell.
```

---

## 4. 그림이 빈 이벤트 채우기

모델은 `gpt_image_2_6`, 16:9이다.

- 프롬프트 = `[F-ILLU]` (+ 인물이 나오면 `[일관성]`과 시트) + 계절 문구(정해진 컷만) + 장면 + `[안전]`
- 넣는 곳: `ArtInbox/illustrations/events12/`
- 잇는 곳: `art_map.json` Events에 한 줄씩 넣는다.
- 장면 칸의 `{SEOJIN}`·`{TEACHER}` 같은 자리에는 0.2 인물 문구를 넣는다. 끝에 붙은 `[여름]`·`[F-KOREA]` 같은 표시도 그 문구로 바꿔 넣는다.

### 4.1 위기 이탈 (5장, P2) — `Content/Story/crisis_events.json`, `Events/Crisis.yarn`

위기 주에 호감이 바닥인 동료가 떠나려는 장면이다. 붙잡을 수도 있으므로 **떠나는 순간이 아니라 짐을 싸는 순간**을 그린다.

| 파일 | 이벤트 | 장면 (영어) |
|---|---|---|
| `evt_crisis_leave_dohyeon.png` | `EVT_CRISIS_LEAVE_DOHYEON` | `Before dawn at the school entrance, {DOHYEON} crouches tying his sneaker laces, a bulging backpack beside him, and looks up startled and guilty at {SEOJIN}, who stands in the dark hallway behind him in a blanket. Cold blue light through the glass doors.` |
| `evt_crisis_leave_nurse.png` | `EVT_CRISIS_LEAVE_NURSE` | `Morning in the school infirmary, {NURSE} divides medicine boxes from an open cabinet into two careful piles on the bed, her face calm and resolved; a small bag half packed. {SEOJIN} stands in the doorway.` |
| `evt_crisis_leave_bystander.png` | `EVT_CRISIS_LEAVE_BYSTANDER` | `In a corner of the school library, {YEJI} quietly packs a sketchbook, a few pencils and one water bottle into a small bag, head down, hair hiding her face; dusty shelves, grey daylight.` |
| `evt_crisis_leave_ringleader.png` | `EVT_CRISIS_LEAVE_RINGLEADER` | `At the far end of a school corridor, {HARIN} folds the thin blanket she always slept on, kneeling on the floor, a shrug of forced indifference on her face; {SEOJIN} approaches from the other end of the corridor.` |
| `evt_crisis_leave_senior.png` | `EVT_CRISIS_LEAVE_SENIOR` | `Morning in a classroom, {SENIOR} stands at the blackboard holding a piece of chalk; on the board a list of supplies written as unreadable scribbles, and he has drawn one line and a small cross at the very bottom. His packed bag waits by the door, shoes already tied.` |

### 4.2 마음 상태 흐름 (9장, P2) — `Content/Story/mind_events.json`, `Events/Mind.yarn`

[PROPOSAL_PROLOGUE_ECHOES.md](PROPOSAL_PROLOGUE_ECHOES.md)의 세 갈래(웃음·서두름·마주 봄)다. 세 갈래의 마지막 장면(`_3`)은 모두 **해 질 녘 옥상에서 교문 앞 횡단보도를 내려다보는** 장면이다. 그래서 한 장(`evt_mind_rooftop_dusk`)을 세 이벤트가 함께 쓴다. `EVT_MIND_HURRY_3`만 장소가 달라 따로 받는다.

| 파일 | 이벤트 | 장면 (영어) |
|---|---|---|
| `evt_mind_laugh_1.png` | `EVT_MIND_LAUGH_1` | `Evening in the classroom shelter after dinner, {TEACHER} sits holding up his hand, folding his fingers one by one as he counts, a gentle knowing look; {SEOJIN}, carrying stacked bowls, stops with a sheepish forced smile. A small bandage on her knee below the skirt.` |
| `evt_mind_laugh_2.png` | `EVT_MIND_LAUGH_2` | `A rainy night, everyone gathered around a pot of thin watery rice porridge in the classroom shelter, gloomy faces in lantern light; {SEOJIN} holds up her spoon like a critic and says something, and {DOHYEON} is just starting to crack a smile. Rain streaks on the window.` |
| `evt_mind_hurry_1.png` | `EVT_MIND_HURRY_1` | `Night in a long school corridor, locking up before bed. {SEOJIN} already stands on the stair landing at the far end, looking back; {TEACHER} is still in the middle of the corridor, steadying himself against the wall with one hand. Flashlight beams, long shadows.` |
| `evt_mind_hurry_2.png` | `EVT_MIND_HURRY_2` | `Afternoon, {SEOJIN} has one hand on the stone pillar of the school gate, out of breath, but she has stopped and turned to look back down the empty road toward a distant bus stop. Overgrown street, utility poles, quiet light. [F-KOREA]` |
| `evt_mind_hurry_3.png` | `EVT_MIND_HURRY_3` | `Inside the stairwell to the school rooftop, {SEOJIN} stands at the top holding the rooftop door handle and looks back down; the others are still climbing in the middle of the sixteen steps, {TEACHER} among them. Daylight spills through the door gap.` |
| `evt_mind_face_1.png` | `EVT_MIND_FACE_1` | `Night, {SEOJIN} kneels at a classroom window facing the schoolyard, a small notebook with tally marks in her hand, counting distant blurry shadow figures standing on the dark playground near the gate and the pull-up bars; {TEACHER} sits behind her in the dim room. The figures are only far, vague silhouettes.` |
| `evt_mind_face_2.png` | `EVT_MIND_FACE_2` | `Mealtime in the classroom shelter, {SEOJIN} looks around at everyone's faces with a small notebook open on her knee, little weather symbols doodled next to names (no readable text); {DOHYEON} sits in the seat nearest the door, glancing out the window before lifting his spoon.` |
| `evt_mind_rooftop_dusk.png` | `EVT_MIND_LAUGH_3`, `EVT_MIND_FACE_3`, `EVT_MIND_TELL_3` | `Dusk on the school rooftop, {SEOJIN} leans on the chain-link fence railing seen from slightly behind, looking down over the school gate at the zebra crosswalk in front of it, the traffic light dark for a year, an empty bus stop beyond. Long golden light, quiet. [F-KOREA]` |
| `evt_favorite_found.png` | `EVT_FAVORITE_FOUND` (메아리) | `Late night, {SEOJIN} sits at a desk unable to close her notebook, writing fast, a candle already burned halfway down, textbooks open around her, a small absorbed smile.` |

`{TEACHER}`에는 윤 선생님 문구를 넣는다. 다리 문구는 13차 1절의 ①~④ 가운데 마음 상태 이벤트가 열리는 막 것을 쓴다. `_1`은 1막이라 ① 또는 ②, `_2`는 2막이라 ②, `evt_mind_hurry_3`(구조 직전 연습)은 4막이라 ③이다.

### 4.3 윤 선생님 대화·막 평가 (12장, P3) — `Content/Story/act{1,2,3}_events.json`

선생님과 둘이 나누는 밤 이야기다. 01·05·12·13은 이미 그림이 있다. 다리 문구는 13차 1절의 ①~④ 가운데 그 막 것을 쓴다. 1막은 ①·②, 2막은 ②, 3막은 ③이고, `evt_talk_08`은 열이 나기 시작한 2막이라 ②다. `evt_a2_review`는 목발이라고 적혀 있으니 ②다.

| 파일 | 이벤트 | 장면 (영어) |
|---|---|---|
| `evt_talk_02.png` | `EVT_TALK_02` (1막) | `Night in the classroom shelter, {TEACHER} sits by the window looking out at the dark city, half turned away, a quiet loneliness; {SEOJIN} sits on a mat nearby holding a mug, listening.` |
| `evt_talk_03.png` | `EVT_TALK_03` (1막) | `Spring night, {TEACHER} recites a short poem in a low voice with his eyes closed, one finger keeping the rhythm; {SEOJIN} listens with her chin on her knees. A candle, cherry petals stuck to the window glass. [봄]` |
| `evt_talk_04.png` | `EVT_TALK_04` (1막) | `Night, {SEOJIN} holds a dead smartphone in both hands, the black screen reflecting the candle; {TEACHER} sits across from her, waiting gently for her to talk.` |
| `evt_talk_06.png` | `EVT_TALK_06` (2막) | `Summer evening, {TEACHER} laughs softly while telling a story, holding an old worn notebook from his school days; {SEOJIN_SUMMER} smiles, a paper fan in her hand. [여름]` |
| `evt_talk_07.png` | `EVT_TALK_07` (2막) | `Night, {SEOJIN} lies on her mat turning a friend's school ID card over in her fingers in the beam of a flashlight (no readable text, the photo blurred); {TEACHER} sits a little away, not pressing her.` |
| `evt_talk_08.png` | `EVT_TALK_08` (2막) | `Night, {TEACHER} sits with his hand resting on the folded, pinned left trouser leg below the knee, a tired, slightly feverish face; {NURSE} kneels beside him checking his temperature with the back of her hand; {SEOJIN} watches from the doorway.` |
| `evt_talk_09.png` | `EVT_TALK_09` (3막) | `Autumn night on the classroom floor, {TEACHER} and {SEOJIN} sit side by side wrapped in blankets; he has just asked her something and she stares at the ceiling, thinking about a future for the first time in a long while. [가을]` |
| `evt_talk_10.png` | `EVT_TALK_10` (3막) | `Daytime in the classroom, {SEOJIN} explains a math problem on a small chalkboard to {DOHYEON}, who is nodding; {TEACHER} watches from his desk with a proud, quiet smile.` |
| `evt_talk_11.png` | `EVT_TALK_11` (3막, 격리 중) | `Close view of the floor at the bottom of the broadcasting room door: a single handwritten lesson-plan sheet has just been slid out through the gap under the door (no readable text, neat rows of lines); {SEOJIN}'s hand reaches for it. Dim corridor light, the door stays closed.` |
| `evt_a1_review.png` | `EVT_A1_REVIEW` (1막 끝) | `The last night of spring, {TEACHER} sits cross-legged on the gym mats with a notebook open instead of an attendance book, giving a playful mid-term review; {SEOJIN} sits straight like a student waiting for her grade, trying not to smile. [봄]` |
| `evt_a2_review.png` | `EVT_A2_REVIEW` (2막 끝) | `The last night of summer on the school rooftop, everyone gathered sitting on blankets under the stars; {TEACHER} has just reached the top of the stairs with his crutch, breathing hard but grinning, and {SEOJIN_SUMMER} and {DOHYEON} reach out to him. [여름]` |
| `evt_a3_review.png` | `EVT_A3_REVIEW` (3막 끝) | `Dawn after a night raid, everyone lies exhausted on the classroom floor under extra blankets, someone snoring; a broken barricaded door lets cold wind in; {SEOJIN} lies awake, a tired smile at the snoring. Pale blue dawn light. [가을]` |

### 4.4 일상 무작위 이벤트 (17장, P3)

자원·몸 상태를 조금 바꾸는 짧은 장면이다. 사람은 작게 또는 뒷모습으로 두고, 사물과 공간이 주인공이다. 인물이 나오면 `[일관성]`을 붙인다.

| 파일 | 이벤트 | 장면 (영어) |
|---|---|---|
| `evt_rnd_rain_leak.png` | `EVT_RND_RAIN_LEAK` | `Morning in a classroom after heavy rain, a dark water stain spreading across the ceiling tiles, drops falling into a dented bucket set on a desk, a puddle on the floor, grey light.` |
| `evt_rnd_nightmare.png` | `EVT_RND_NIGHTMARE` | `Dreamlike dark image: the inside of a cramped school storage room seen from the floor, a gym mat in the corner, a door with a round handle and a thin line of light under it, everything slightly blurred and cold blue, no people.` |
| `evt_rnd_rats.png` | `EVT_RND_RATS` | `Close view of a cardboard food box on a storage shelf with one corner gnawed open, a torn rice bag beside it, a few grains spilled, a tiny rat tail disappearing behind the shelf, flashlight beam.` |
| `evt_rnd_water_smell.png` | `EVT_RND_WATER_SMELL` | `Close view of a blue plastic water container being poured into a cup, the water slightly cloudy with a thin film on the surface, {SEOJIN_SUMMER}'s hand hesitating, a frown visible at the edge of the frame. Humid summer light. [여름]` |
| `evt_rnd_quiet_morning.png` | `EVT_RND_QUIET_MORNING` | `A rare quiet morning, a sparrow on the classroom windowsill, two paper cups of instant coffee mix on a desk with steam, soft sunlight; through the window green leaves on the cherry tree by the schoolyard. [봄]` |
| `evt_rnd_teacher_pain.png` | `EVT_RND_TEACHER_PAIN` | `Midnight in the classroom shelter, {TEACHER} sits up on his mat with his jaw clenched, both hands gripping just above his left knee over the folded, pinned trouser leg, eyes shut in pain; a dim lantern, others asleep in the background.` |
| `evt_rnd_letter.png` | `EVT_RND_LETTER` | `Close view of {SEOJIN}'s hands holding a page torn from an exercise book: an unsent letter, handwriting erased and rewritten many times (no readable text), a date in the corner smudged; dust and flashlight light.` |
| `evt_rnd_a2_mosquito.png` | `EVT_RND_A2_MOSQUITO` | `Hot summer night in the classroom shelter, {SEOJIN_SUMMER} sits up on her mat mid-slap with her palm against her own cheek, annoyed and sleepy, a thin curl of smoke from a mosquito coil on a tin lid. [여름]` |
| `evt_rnd_a2_generator.png` | `EVT_RND_A2_GENERATOR` | `Evening, the fluorescent lights in the classroom have just gone out; everyone looks up at the ceiling, faces lit only by one flashlight, a dinner left half eaten on the desks.` |
| `evt_rnd_a2_looters.png` | `EVT_RND_A2_LOOTERS` | `Daytime by the school gate, a strange spray-painted mark on the concrete wall: a circle with an arrow; nearby a section of chain-link fence has been cut open. No people. [여름] [F-KOREA]` |
| `evt_rnd_a2_tropical_night.png` | `EVT_RND_A2_TROPICAL_NIGHT` | `Sweltering tropical night, everyone lies on the bare concrete classroom floor with wet towels around their necks, one person slowly waving a hand fan, windows half open with mosquito netting, moonlight. [여름]` |
| `evt_rnd_a3_leaves.png` | `EVT_RND_A3_LEAVES` | `Morning, yellow ginkgo leaves piled thick on the classroom window frame and drifted ankle-deep across the schoolyard below, soft golden light. [가을]` |
| `evt_rnd_a3_cold_night.png` | `EVT_RND_A3_COLD_NIGHT` | `Cold night in the classroom shelter, everyone curled up under too few blankets pressed close together, breath visible in the lantern light, thin ice on the surface of a water bucket in the foreground. [가을]` |
| `evt_rnd_a3_tracks.png` | `EVT_RND_A3_TRACKS` | `Early morning at the school gate, many footprints pressed into the mud and the white frost, human ones and stranger dragging ones, one large set coming inside the gate and turning back out. No people. [가을]` |
| `evt_rnd_a3_persimmon.png` | `EVT_RND_A3_PERSIMMON` | `A persimmon tree beside the schoolyard wall heavy with bright orange fruit nobody has picked, a magpie pecking a ripe one at the top, fallen persimmons dotting the ground like orange spots. [가을] [F-KOREA]` |
| `evt_rnd_a4_snow.png` | `EVT_RND_A4_SNOW` | `Dawn after an overnight snowfall, the whole schoolyard covered in untouched white snow, not a single footprint, seen from a second-floor classroom window, snow piled on the window frame. [겨울]` |
| `evt_rnd_a4_pipe.png` | `EVT_RND_A4_PIPE` | `The school cafeteria kitchen in deep winter, a frozen water pipe burst along the wall with icicles hanging from it, the floor turned into a sheet of ice, old duct tape wrapped around the pipe nearby. No people. [겨울]` |

### 4.5 (13차로 옮김) 초복 삼계탕·평면도 다시 그리기

이 절에 있던 두 컷(`evt_cal_chobok`·`evt_a2_plan_next`, 서진·윤 선생님·도현)은 [HIGGSFIELD_ASSETS_13.md](HIGGSFIELD_ASSETS_13.md) 2.4·2.5로 옮겨 **받았다**(2026-11-24, `ArtInbox/illustrations/redo13/`). 프롬프트는 13차 문서 2절에 함께 있다. 이 문서에서는 다시 받지 않는다.

**있는 그림을 쓰는 것** (새로 받지 않는다):

| 이벤트 | 쓸 그림 | 까닭 |
|---|---|---|
| `EVT_RND_A3_REQUEST` (DJ 신청곡) | `evt_a3_radio` | 무전기 앞에 모여 DJ 방송을 듣는 장면. 같은 무전 컷이 이미 두 이벤트에 쓰인다 |
| `EVT_RND_A4_GREETING` (무전 인사) | `evt_a3_dj_show` | DJ 방송 장면 |

---

## 5. 만들지 않는 것 · 확인한 여분

- **연결 안 된 여분 그림 6장**은 이미 쓸 곳이 있다. 그래서 이번 문서에 넣지 않았다.
  - `evt_cat_warn`(고양이 경고), `evt_deep_door`·`evt_deep_oneway`(깊은 곳), `evt_season_flood`·`evt_season_snow_block`(계절 재해): 아직 이벤트가 없는 기능용이다. `evt_cat_warn`은 인물을 맞춰 13차 부록으로 다시 받았다.
  - `evt_lost_found`: 분실물 상자 그림이다. 편지 이벤트(4.4 `evt_rnd_letter`)를 받기 전에 임시로 쓸 수 있다.
- **고양이 이벤트 컷**(`evt_cat_*`)은 이미 치즈 고양이로 잘 나와서 다시 받지 않는다. 3장의 고양이 도트는 이 컷의 고양이를 따른다.
- **박 선생님·송예지·최하린 단면도 대기 그림**은 설정화와 크게 어긋나지 않아 그대로 둔다(걷기만 3.6).
- **탐험 화면 주인공 스프라이트**는 이 문서에 없다. [PIXELLAB_24F_PLAN.md](PIXELLAB_24F_PLAN.md) 9절의 남은 5벌(PixelLab 크레딧을 채운 뒤)을 따른다.
- 대사 초상·입상은 이번에 필요한 것이 없다.
- **8차 이벤트 컷 다시 받기**는 13차 문서의 몫이다. 이 문서에 넣지 않는다. 13차에서 맞게 나왔다고 본 컷(13차 0.2)도 다시 받지 않는다.

## 6. 받는 차례 (추천)

1. 1장 ★ 11장: 좋아하는 짝 6, 도현 짝 3, 혼자 2. 받으면 깃발별 그림 고르기 코드와 함께 바로 잇는다. (옛 4.5의 2장은 13차로 이미 받았다.)
2. 3.2~3.5 시트 5장: 고양이 대기·걷기, 도현, 선배, 유랑자. 단면도에 바로 보인다.
3. 2장 개인 장면 6장 → 1장의 나머지 17장.
4. 4.1 위기 이탈 → 4.2 마음 상태 → 4.3 → 4.4 → 3.6 걷기.
