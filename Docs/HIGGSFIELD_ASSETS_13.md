# 힉스필드 에셋 13차: 8차 이벤트 컷 인물 맞춰 다시 받기 (2026-11-24)

8차([HIGGSFIELD_ASSETS_8.md](HIGGSFIELD_ASSETS_8.md))의 이벤트 컷 프롬프트는 인물을 "a girl", "a 15-year-old boy", "a middle-aged male teacher", "people", "the others"로만 적었다. 이름과 생김새를 적은 인물 문구가 없었다.

- 설정화를 참고 이미지로 넣은 컷은 대부분 맞게 나왔다.
- 다음 경우에는 설정과 다른 사람이 나왔다.
  - 손만 나오는 컷, 여럿이 모인 컷
  - 참고 이미지가 없던 달력·이야기 컷
- 그래서 8차 컷 중 인물이 나오는 89장을 모두 열어 보고, **다른 사람이 나온 컷**만 다시 받는다. 장면마다 대본에 나오는 인물을 이름으로 적었다.

쓰는 법:

- 공통 공식은 [HIGGSFIELD_ASSETS.md](HIGGSFIELD_ASSETS.md) 0장을 따른다(`[F-ILLU]`·`[F-KOREA]`·`[안전]`).
- 계절 문구 `[봄]`·`[여름]`·`[가을]`·`[겨울]`은 [HIGGSFIELD_ASSETS_8.md](HIGGSFIELD_ASSETS_8.md) 0.3에 있다.
- 인물 문구와 `[일관성]`은 [HIGGSFIELD_ASSETS_12.md](HIGGSFIELD_ASSETS_12.md) 0.2에 있다. 이 문서에 쓰는 것은 아래 1절에 다시 모았다.
- **작업 제출은 사용자가 한다.** 나(Claude)는 문서만 쓴다.
- 모두 `gpt_image_2_6`, 16:9, 2k 이상이다.
- **참고 이미지 = 그 컷에 나오는 인물의 시트 전부**(`ArtInbox/illustrations/char_*_sheet.png`). 4장을 넘으면 주인공·주요 인물부터 4장만 넣는다.
- 프롬프트 = `[F-ILLU]` + `[일관성]` + 장면 + 계절 + `[안전]`
- 넣는 곳: `ArtInbox/illustrations/redo13/`, 파일 이름은 지금 그림과 같다.
- 잇는 곳: 받으면 `Resources/Art/Story/Events/{파일}.jpg`를 덮어쓰고, 옛 그림은 `code_temp/apogirl/backup_redo13/`으로 옮긴다. `art_map.json`은 그대로다.

---

## 0. 한눈에 보기

### 0.1 다시 받는 컷 (17장)

| 번호 | 파일 | 지금 그림의 문제 | 나올 인물 | 우선 |
|---|---|---|---|---|
| 2.1 | `evt_a1_power_plan` | 머리를 묶은 다른 여학생, 젊은 남자가 선생님으로 나옴 | 서진, 윤 선생님 | **P1** |
| 2.2 | `evt_a1_raid_warning` | 담요를 쓴 누군지 모를 아이 하나 | 서진, 도현, 윤 선생님 | **P1** |
| 2.3 | `evt_a2_opening` | 도현이 곱슬머리에 반창고가 없음, 선생님이 2막 초에 이미 의족 | 서진, 도현, 윤 선생님 | P2 |
| 2.4 | `evt_a2_plan_next` | 모르는 학생 셋 | 서진, 윤 선생님, 도현 | **P1** (12차 4.5절) |
| 2.5 | `evt_cal_chobok` | 모르는 학생 넷 | 서진, 윤 선생님, 도현 | **P1** (12차 4.5절) |
| 2.6 | `evt_cal_vacation` | 모르는 여학생 둘이 더 있음 | 서진, 도현, 윤 선생님 | P2 |
| 2.7 | `evt_cal_second_term` | 출석부를 쓰는 모르는 남학생 | 서진, 윤 선생님 | **P1** |
| 2.8 | `evt_hr_first_night` | 둘러앉은 사람들이 모두 모르는 학생 | 최하린, 서진, 도현, 송예지, 윤 선생님 | **P1** |
| 2.9 | `evt_hr_chores` | 복도 끝 사람들이 모르는 사람 | 최하린, 송예지, 도현 | P2 |
| 2.10 | `evt_a3_diagnosis` | 서진 자리에 머리를 묶은 다른 여학생 | 박 선생님, 서진 | **P1** |
| 2.11 | `evt_a3_dj_research` | 무전기를 둘러싼 학생 다섯이 모두 모르는 얼굴 | 서진, 도현, 박 선생님, 송예지, 윤 선생님 | **P1** |
| 2.12 | `evt_a3_mart_news` | 앞쪽 인물이 선생님이 아닌 젊은 남자 | 서진, 윤 선생님 | **P1** |
| 2.13 | `evt_a3_mart_radio` | 뒤에서 빗장을 거는 두 사람이 모르는 사람 | 서진, 도현, 박 선생님 | P2 |
| 2.14 | `evt_a3_finale` | 문을 막는 사람들이 모르는 학생 | 윤 선생님, 서진, 도현 | **P1** |
| 2.15 | `evt_cal_dongji` | 둘러앉은 아이들이 모르는 얼굴 | 서진, 윤 선생님, 도현, 박 선생님 | **P1** |
| 2.16 | `evt_bc_first` | 문간에 모르는 학생 여럿 (대본은 도현 하나) | 서진, 도현 | **P1** |
| 2.17 | `evt_bond_senior_3` | 복도에 앉은 사람들이 모르는 학생 | 2학년 선배, 서진, 도현, 송예지 | P2 |
| 부록 | `evt_cat_warn` | (아직 안 쓰는 컷) 뒤의 사람들이 모르는 얼굴 | 서진, 도현, 박 선생님 | P3 |

### 0.2 확인하고 그대로 두는 컷

8차 컷 가운데 아래는 설정과 맞게 나왔다. 손만 나오거나 인물이 없는 컷도 그대로 둔다.

| 묶음 | 컷 |
|---|---|
| 동료 이벤트 (1.3) | `evt_dh_*` 7장, `evt_nurse_*` 4장, `evt_by_*` 4장, `evt_hr_storage`·`_secret`, `evt_wanderer_*` 2장 |
| 이야기 (1.4) | `evt_a1_tutorial_done`·`_crutch_plan`·`_keys`(손)·`_eve`(뒷모습), `evt_a2_subway_tape`·`_finale`, `evt_a3_flu`·`_armory`·`_sageori_raise`·`_sageori_note` |
| 다시 만든 것 (1.2) | `evt_talk_12`, `evt_a4_sageori_after`, `evt_sv_join`, `evt_a4_cured` |
| 달력·날씨 (1.5) | `evt_cal_final_exam`·`_suneung`·`_hangeul`, `evt_sys_first_science`, `evt_wx_*` 3장 |
| 개인 장면 (2.1·2.2) | `evt_bond_*` (선배 3만 다시) |
| 고양이·CD·깊이·송출·재해 | `evt_cat_arrive`·`_feed`·`_settle`·`_lap`, `evt_cd_shelf_full`, `evt_deep_*`, `evt_bc_reply`·`_found`, `evt_season_*` |
| 동아리 (3.1) | 12차 1장에서 28장으로 다시 받는다 |

---

## 1. 이 문서에서 쓰는 문구

**[일관성]** (12차 0.2와 같다)
```
Character consistency: draw every named character exactly as in the attached character reference sheets: same face, same hairstyle and hair color, same outfit and colors. Do not invent new characters or change clothes. Only the characters named in this prompt appear.
```

**인물 문구.** 장면 칸의 자리 표시를 아래 문구로 바꾼다.

| 자리 표시 | 시트 | 문구 |
|---|---|---|
| `{SEOJIN}` (봄·가을) | `char_seojin_sheet.png` | `Seojin, a 16-year-old Korean girl with a straight black bob just above the shoulders and straight bangs, navy school blazer, white shirt with a navy striped tie, navy plaid pleated knee-length skirt, black opaque tights` |
| `{SEOJIN_SUMMER}` | 같은 시트 | `Seojin, a 16-year-old Korean girl with a straight black bob just above the shoulders and straight bangs, in a summer school uniform: white short-sleeved shirt with a navy striped tie, a beige knit sweater vest, navy plaid pleated knee-length skirt` |
| `{SEOJIN_WINTER}` | 같은 시트 | `Seojin, a 16-year-old Korean girl with a straight black bob just above the shoulders and straight bangs, a worn dark padded jacket over her navy school uniform, navy plaid skirt, black tights` |
| `{DOHYEON}` | `char_dohyeon_sheet.png` | `Dohyeon, a thin 15-year-old Korean boy with short messy straight black hair, a small adhesive bandage on his forehead, an oversized dark green track jacket with white stripes down the sleeves, white T-shirt, faded blue jeans` |
| `{NURSE}` | `char_nurse_sheet.png` | `Nurse Park, a Korean school nurse in her 40s with a neat short dark bob, a white nurse gown over an olive quilted padded vest, sleeves rolled up, grey trousers` |
| `{YEJI}` | `char_bystander_sheet.png` | `Yeji, a slim 16-year-old Korean girl with long straight black hair tied low, a slightly oversized navy school blazer, white shirt, navy pleated skirt, shy posture` |
| `{HARIN}` | `char_ringleader_sheet.png` | `Harin, a thin 16-year-old Korean girl with shoulder-length brown hair, sharp but tired eyes, an oversized olive-green hooded parka over a navy school uniform, black tights` |
| `{SENIOR}` | `char_senior_sheet.png` | `the senior, a tall quiet 17-year-old Korean boy with messy dark hair falling over his eyes, a faded dark navy zip-up gym jacket over a white shirt, dark trousers, a thin pale fully healed scar along his left jaw` |
| `{TEACHER}` | `char_teacher_sheet.png` | `Mr. Yoon, a Korean male teacher in his mid-40s with short black hair, thin glasses and a grey cardigan over a white shirt, dark trousers` + 아래 다리 문구 하나 |

**윤 선생님 다리 문구** (장면 칸에 `+다리①` 같은 표시로 적었다)
- ① 1막, 목발 전·앉은 자세: `sitting on gym mats, his left trouser leg folded and pinned below the knee, the leg not visible`
- ② 1막 후반~2막, 목발: `leaning on a homemade crutch made from a mop handle and tape, his left trouser leg folded and pinned below the knee`
- ③ 2막 후반~3막, 의족: `a simple homemade prosthetic lower leg of plastic pipe and padding visible below the left trouser hem, a cane`
- ④ 3막 후반~4막, 병세: `pale and feverish, wrapped in a blanket, the left trouser leg folded below the knee, a crutch leaning nearby`

---

## 2. 다시 받을 컷

| 번호 | 파일 · 이벤트 | 계절 | 참고 시트 | 장면 (영어) |
|---|---|---|---|---|
| 2.1 | `evt_a1_power_plan.png` · `EVT_A1_POWER_PLAN` 비상 발전기 | `[봄]` | 서진, 윤 선생님 | `Daytime in a dusty school broadcasting room. In the foreground {SEOJIN} holds her dead smartphone with a black screen in both hands, looking down at it with a heavy, hopeful expression. Behind her {TEACHER} +다리① sits on a folded gym mat by the wall and points at a dusty portable emergency generator in the corner, coiled extension cords beside it. Only these two characters appear.` |
| 2.2 | `evt_a1_raid_warning.png` · `EVT_A1_RAID_WARNING` 긁는 소리 | `[봄]` | 도현, 서진, 윤 선생님 | `Pale morning in the classroom shelter. The front door is barricaded with desks that have been pushed back about a hand's width overnight, fresh scratch marks on the door frame (no stains). {DOHYEON} sits in the corner with a blanket pulled up to his eyes, only his messy hair and frightened eyes showing, his green track jacket sleeve visible. {SEOJIN} kneels by the barricade measuring the gap with her hand. {TEACHER} +다리① watches from his mat with a grave face. Only these three characters appear.` |
| 2.3 | `evt_a2_opening.png` · `EVT_A2_OPENING` 여름은 넓히는 계절 | `[여름]` | 윤 선생님, 서진, 도현 | `A muggy summer morning in the storage room shelter. {TEACHER} +다리② stands at a chalkboard drawing a floor plan of the school with a broken piece of chalk, rooms circled (no readable words). {SEOJIN_SUMMER} and {DOHYEON} (track jacket open, sleeves pushed up) sit on gym mats watching, Dohyeon fanning himself with a notebook. Cicadas and green leaves outside the high window. Only these three characters appear.` |
| 2.4 | `evt_a2_plan_next.png` · `EVT_A2_PLAN_NEXT` | `[여름]` | — | [HIGGSFIELD_ASSETS_12.md](HIGGSFIELD_ASSETS_12.md) **4.5절 그대로** (서진·윤 선생님·도현) |
| 2.5 | `evt_cal_chobok.png` · `EVT_CAL_CHOBOK` | `[여름]` | — | [HIGGSFIELD_ASSETS_12.md](HIGGSFIELD_ASSETS_12.md) **4.5절 그대로** (서진·윤 선생님·도현) |
| 2.6 | `evt_cal_vacation.png` · `EVT_CAL_VACATION` 방학식 | `[여름]` | 윤 선생님, 도현, 서진 | `A bright summer afternoon classroom. {TEACHER} +다리② stands at the front giving a short, half-joking closing-ceremony speech. {DOHYEON} (track jacket open, sleeves pushed up) leans far back on his chair grinning with his hands behind his head. {SEOJIN_SUMMER} sits at her desk smiling, a diary open in front of her. Cicada light through the windows, the rest of the desks empty. Only these three characters appear.` |
| 2.7 | `evt_cal_second_term.png` · `EVT_CAL_SECOND_TERM` 개학 출석 | `[가을]` | 윤 선생님, 서진 | `Early autumn morning in a mostly empty classroom. {TEACHER} +다리③ stands behind the teacher's desk holding a real class attendance book open, a red pen drawing a neat circle next to one name line (unreadable), a warm smile. {SEOJIN} sits alone in the front row with her hand raised high, answering the roll call. An empty corridor visible through the open door. Only these two characters appear.` |
| 2.8 | `evt_hr_first_night.png` · `EVT_A3_RINGLEADER_NIGHT` 첫날 밤 | `[가을]` | 하린, 서진, 도현, 예지 (+윤 선생님 문구만) | `Night in the classroom shelter, a camping stove in the middle. {SEOJIN}, {DOHYEON}, {YEJI} and {TEACHER} +다리③ sit in a loose circle around it with bowls. {HARIN} sits alone at the far edge of the room with half a bowl, a wide empty gap of floor between her and the others. Dohyeon glances at Seojin; Yeji keeps her head down; the teacher waits quietly; Seojin is about to speak. Lantern light, tense silence. Only these five characters appear.` |
| 2.9 | `evt_hr_chores.png` · `EVT_HR_02_CHORES` 시키지 않은 일 | `[가을]` | 하린, 예지, 도현 | `Early morning in a long school corridor, cold light, wet mop marks on the floor. {HARIN} carries two heavy water buckets, sleeves rolled up, palms red, face set. At the far end {DOHYEON} steps aside against the wall to let her pass, and {YEJI} glances at Harin and then turns her head away. Only these three characters appear.` |
| 2.10 | `evt_a3_diagnosis.png` · `EVT_A3_NURSE_DIAGNOSIS` 관찰 수첩 | `[가을]` | 박 선생님, 서진 | `Afternoon at the school nurse office desk. {NURSE} sits with an observation notebook open, months of handwritten columns of dates and temperatures (unreadable) and a simple hand-drawn line graph that wavers toward the end; her finger rests on the wavering part. {SEOJIN} sits across the desk holding an old health check-up record card, listening with a worried face. Only these two characters appear.` |
| 2.11 | `evt_a3_dj_research.png` · `EVT_A3_DJ_RESEARCH` DJ의 기억 | `[가을]` | 서진, 도현, 박 선생님, 윤 선생님 (+예지 문구만) | `Night, a handheld radio glowing on a classroom desk. {SEOJIN}, {DOHYEON}, {NURSE} and {YEJI} lean in around it, frozen and listening. In the background {TEACHER} +다리④ lies under a blanket on a mat, watching the radio with an oddly calm face. Moonlight, one lantern. Only these five characters appear.` |
| 2.12 | `evt_a3_mart_news.png` · `EVT_A3_MART_UNLOCK` 더불어 마트 소식 | `[가을]` | 윤 선생님, 서진 | `Night in the classroom. A handheld radio on a desk beside a hand-drawn town map with a red circle around a large supermarket and its warehouse. {TEACHER} +다리③ sits at the desk with a pencil hovering over the map, explaining. {SEOJIN} listens across from him with her chin on her folded arms. Lantern light, autumn leaves on the windowsill. Only these two characters appear.` |
| 2.13 | `evt_a3_mart_radio.png` · `EVT_A3_MART_RADIO` 정 반장의 경고 | `[가을]` | 서진, 도현, 박 선생님 | `Late night in a dark school corridor. In the foreground {SEOJIN} presses a handheld radio to her ear, listening intently. In the background {DOHYEON} and {NURSE} slide an extra wooden bar across a door, empty cans hanging on a string as an alarm. Only these three characters appear.` |
| 2.14 | `evt_a3_finale.png` · `EVT_A3_FINALE` 첫 한파의 밤 | `[겨울]` | 윤 선생님, 서진, 도현 | `First cold-snap night, frost on the classroom windows. {TEACHER} +다리④ lies on his mat and shines a flashlight toward the barricaded door, a handheld radio by his pillow. {SEOJIN_WINTER} and {DOHYEON} (a blanket over his track jacket) brace the barricade with their shoulders, breath fogging. Tense, nothing visible beyond the door, no close-up violence. Only these three characters appear.` |
| 2.15 | `evt_cal_dongji.png` · `EVT_CAL_DONGJI` 동지 | `[겨울]` | 서진, 윤 선생님, 도현, 박 선생님 | `The longest night of the year in a cold classroom, the window already dark. A pot of sweet red bean porridge with rice cake pieces on a small stove. {SEOJIN_WINTER} feeds a spoonful to {TEACHER} +다리④, who smiles at how sweet it is. {DOHYEON} laughs with his mouth full, and {NURSE} laughs behind her hand. Warm lantern light. Only these four characters appear.` |
| 2.16 | `evt_bc_first.png` · `EVT_BROADCAST_FIRST` 첫 송출 | (계절 없음) | 서진, 도현 | `Night in the school broadcasting room. {SEOJIN} leans toward an old microphone on the mixing desk, her fingertip on the send switch, a small red on-air bulb glowing, a portable CD player and the wire to a homemade rooftop antenna beside her. {DOHYEON} stands in the doorway giving two silent thumbs up. Only these two characters appear.` |
| 2.17 | `evt_bond_senior_3.png` · `EVT_BOND_SENIOR_3` 처음 치는 기타 | (계절 없음) | 선배, 서진, 도현, 예지 | `Evening at the school entrance reinforced with planks and metal sheets. {SENIOR} sits on a step of the barricade playing an acoustic guitar softly with his lips closed, humming, an old CD case beside him. In the corridor {SEOJIN} sits closest to him, with {DOHYEON} and {YEJI} sitting a little farther back, all listening in silence. Warm lantern light. Only these four characters appear.` |
| 부록 | `evt_cat_warn.png` · (고양이 경고, 이벤트 없음) | (계절 없음) | 서진, 도현, 박 선생님 + `cat_illust_avatar.png` | `Night, an orange tabby cat (cheese-colored with darker stripes, cream chest) on a classroom windowsill with its fur standing up and back arched, staring hard out at the dark schoolyard. Behind it {SEOJIN} sits up from her blanket, {DOHYEON} reaches for a flashlight, and {NURSE} props herself up on one elbow. Tense, nothing visible outside but darkness. Only these three people and the cat appear.` |

**쓰는 법 메모**
- 다섯 명 이상 나오는 컷(2.8·2.11)은 참고 시트를 4장만 넣는다. 나머지 한 명은 문구만으로 그린다. 얼굴이 흔들리면 그 인물을 뒷모습·옆모습으로 바꿔 다시 받는다.
- 2막 이벤트는 판마다 의족을 다는 때가 다르다. 그래서 2.3·2.6은 목발(②)로 둔다.
- 3막 이벤트도 같은 까닭으로, 병세가 깊어지기 전 장면은 의족(③), 뒤 장면은 병세(④)로 둔다.

## 3. 받는 차례 (추천)

1. P1 12장: 2.1·2.2·2.7·2.8·2.10·2.11·2.12·2.14·2.15·2.16, 그리고 12차 4.5절 2장(2.4·2.5).
2. P2 5장: 2.3·2.6·2.9·2.13·2.17.
3. 부록 `evt_cat_warn`은 고양이 경고 이벤트를 만들 때.
