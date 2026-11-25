# 힉스필드 에셋 17차: 2차·5차 이벤트 컷 인물 맞춰 다시 받기 (2026-11-26)

2차([HIGGSFIELD_ASSETS_2.md](HIGGSFIELD_ASSETS_2.md), 2026-11-21 연결)의 소풍·수학여행·여름 축제 컷은 인물 설정화가 생기기 전에 받았다. 그래서 게임 인물이 아닌 낯선 학생·어른이 나온다. 9차 다시 그리기·13차 인물 맞추기에서도 빠졌다.

2026-11-26에 소풍·수학여행·행사 컷 11장을 인물 초상과 나란히 놓고 보았다. **다른 사람이 나온 6장**만 다시 받는다(2절).

**연결 완료 (2026-11-26):** 2절·3절 23장을 받아 `Story/Events`에 덮어썼다. 옛 그림은 `code_temp/apogirl/backup_redo17/`.

같은 날 김장 컷도 인물이 다르다는 말을 듣고, 사람이 나오는 이벤트 컷을 모두 다시 보았다. 2차·5차 컷 가운데 **17장**을 더 다시 받는다(3절). 12·14·15차 컷은 인물 시트를 넣고 받아서 맞다. 8차는 13차에서 이미 맞췄다.

쓰는 법 (13차와 같다):

- 공통 공식은 [HIGGSFIELD_ASSETS.md](HIGGSFIELD_ASSETS.md) 0장을 따른다(`[F-ILLU]`·`[F-KOREA]`·`[안전]`).
- 계절 문구 `[여름]`·`[가을]`·`[겨울]`은 [HIGGSFIELD_ASSETS_8.md](HIGGSFIELD_ASSETS_8.md) 0.3에 있다.
- 인물 문구·`[일관성]`·윤 선생님 다리 문구는 [HIGGSFIELD_ASSETS_13.md](HIGGSFIELD_ASSETS_13.md) 1절 것을 그대로 쓴다. 이 문서에 쓰는 것은 아래 1절에 다시 모았다.
- **작업 제출은 사용자가 한다.** 나(Claude)는 문서만 쓴다.
- 모두 `gpt_image_2_6`, 16:9, 2k 이상이다.
- **참고 이미지 = 그 컷에 나오는 인물의 시트 전부**(`ArtInbox/illustrations/char_*_sheet.png`). 4장을 넘으면 주인공·주요 인물부터 4장만 넣는다.
- 프롬프트 = `[F-ILLU]` + `[일관성]` + 장면 + 계절 + `[안전]`
- 넣는 곳: `ArtInbox/illustrations/redo17/`, 파일 이름은 지금 그림과 같다.
- 잇는 곳: 받으면 `Resources/Art/Story/Events/{파일}.jpg`를 덮어쓰고(16:9 · 너비 1920 jpg, `install_events12.py`와 같은 처리), 옛 그림은 `code_temp/apogirl/backup_redo17/`로 옮긴다. `art_map.json`은 그대로다.

---

## 0. 한눈에 보기

### 0.1 다시 받는 컷 (6장)

| 번호 | 파일 · 이벤트 | 시기 | 지금 그림의 문제 | 나올 인물 | 우선 |
|---|---|---|---|---|---|
| 2.1 | `evt_outing_summer` · `EVT_OUTING_SUMMER` 여름 소풍 | 18~22주 (2막) | 개울에서 노는 넷이 모두 모르는 얼굴, 선생님 없음 | 서진, 도현, 윤 선생님, 박 선생님 | **P1** |
| 2.2 | `evt_outing_autumn` · `EVT_OUTING_AUTUMN` 가을 소풍 | 34~37주 (3막) | 배낭 멘 등산객 넷, 누군지 알 수 없음 | 서진, 도현, 송예지, 윤 선생님 | **P1** |
| 2.3 | `evt_outing_winter` · `EVT_OUTING_WINTER` 겨울 소풍 | 44~48주 (4막) | 젊은 갈색 머리 남자가 선생님 자리, 나머지도 모르는 얼굴 | 서진, 도현, 윤 선생님, 박 선생님 | **P1** |
| 2.4 | `evt_trip_depart` · `EVT_TRIP_DEPART` 수학여행 출발 | 30~33주 (3막) | 뒷모습 넷이 모르는 사람, 선생님 없음 | 서진, 도현, 송예지, 윤 선생님 | P2 |
| 2.5 | `evt_trip_return` · `EVT_TRIP_RETURN` 수학여행 귀환 | 30~33주 (3막) | 수레 끄는 다섯이 모르는 사람 | 서진, 도현, 송예지, 윤 선생님 | P2 |
| 2.6 | `evt_cal_summer_festival` · `EVT_CAL_SUMMER_FESTIVAL` 여름 축제 날 | 7월 28일 밤 (2막) | 하복 입은 모르는 학생 다섯이 교실에서 수박 | 서진, 윤 선생님, 도현 | **P1** |

### 0.2 확인하고 그대로 두는 컷

| 컷 | 까닭 |
|---|---|
| `evt_outing_spring` (2차) | 회색 가디건·목발의 윤 선생님과 서진 뒷모습. 설정과 맞다 |
| `evt_cal_sports_day` (2차, 11-24 명찰 지움) | 서진과 안경·가디건·목발의 선생님. 선생님 머리가 조금 검지만 알아볼 수 있다 |
| `evt_cal_vacation` (13차 2.6) | 도현·서진·윤 선생님 모두 맞다 |
| `evt_trip_night`, `evt_cal_school_festival` (2차) | 사람이 나오지 않는다 |

### 0.3 장면을 고른 까닭

이벤트 그림은 선택지보다 먼저 뜬다. 그래서 선택지 하나의 장면이 아니라 **이벤트 첫머리에 맞는 장면**을 골랐다. 판마다 갈리는 대사(선생님 재감염·목발, 동료 합류)는 어느 쪽이어도 어색하지 않게 잡았다.

- 여름 소풍: 대본 첫 선택지 "빈 수영장에 고인 빗물에서 물놀이". 학교 안이라 어느 판에서나 맞고, 선생님이 가장자리에 앉아 발을 담그는 줄이 있다.
- 가을 소풍: 첫 선택지 "운동장에서 낙엽을 모은다". 송예지가 처음 소리 내어 웃는 줄이 있다.
- 겨울 소풍: 첫 선택지 "눈사람". 서진이 목도리를 풀어 감아 주고, 도현이 작은 눈사람을 하나 더 만든다(엄마 몫). 선생님은 재감염이면 창가에 남으니, 담요를 두르고 운동장 가 벤치에 앉은 자리로 두어 어느 쪽에도 덜 어긋나게 했다.
- 수학여행 출발: 선생님은 판에 따라 함께 가거나 남는다. 교문에서 목발을 짚고 서 있는 모습이면 둘 다 맞는다.
- 수학여행 귀환: 해 질 녘 학교 지붕이 보이는 길. 교문에서 선생님이 기다린다(함께 간 판이어도 먼저 들어가 맞는 모습으로 읽힌다).
- 여름 축제: 대본은 전단지(강변 불꽃놀이, 7월 28일) → 옥상에서 불꽃 대신 별을 본다 → 선생님 "백조자리다. 아니, 아닐 수도 있다. 국어 선생이라." 다 같이 웃는다.

---

## 1. 이 문서에서 쓰는 문구

**[일관성]** (12차 0.2·13차 1절과 같다)
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
| `{SENIOR}` | `char_senior_sheet.png` | `the senior, a tall quiet 17-year-old Korean boy with messy dark hair falling over his eyes, a faded dark navy zip-up gym jacket over a white shirt, dark trousers, a thin pale fully healed scar along his left jaw` |
| `{TEACHER}` | `char_teacher_sheet.png` | `Mr. Yoon, a Korean male teacher in his mid-40s with short black hair, thin glasses and a grey cardigan over a white shirt, dark trousers` + 아래 다리 문구 하나 |

**윤 선생님 다리 문구** (13차 1절과 같다)
- ② 1막 후반~2막, 목발: `leaning on a homemade crutch made from a mop handle and tape, his left trouser leg folded and pinned below the knee`
- ③ 2막 후반~3막, 의족: `a simple homemade prosthetic lower leg of plastic pipe and padding visible below the left trouser hem, a cane`
- ④ 3막 후반~4막, 병세: `pale and feverish, wrapped in a blanket, the left trouser leg folded below the knee, a crutch leaning nearby`
- ⑤ 4막 끝, 완치 (3절 3.11만): `a simple homemade prosthetic lower leg of plastic pipe and padding visible below the left trouser hem, a cane, thin but with healthy color back in his face`

---

## 2. 다시 받을 컷

| 번호 | 파일 · 이벤트 | 계절 | 참고 시트 | 장면 (영어) |
|---|---|---|---|---|
| 2.1 | `evt_outing_summer.png` · `EVT_OUTING_SUMMER` 여름 소풍 | `[여름]` | 서진, 도현, 윤 선생님, 박 선생님 | `A bright summer vacation afternoon at an abandoned outdoor school swimming pool, the pool drained except for knee-deep, slightly cloudy rainwater, cracked blue tiles, weeds at the edges. {SEOJIN_SUMMER} (shoes off, skirt hem wet) and {DOHYEON} (track jacket tied around his waist, T-shirt, jeans rolled up) splash water at each other, laughing while trying to keep quiet. {TEACHER} +다리② sits on the pool edge with his right foot in the water, his crutch laid beside him, smiling and squinting in the sun. {NURSE} sits in the shade of the pool fence with a towel on her lap, watching. Sneakers lined up on the edge. Only these four characters appear.` |
| 2.2 | `evt_outing_autumn.png` · `EVT_OUTING_AUTUMN` 가을 소풍 | `[가을]` | 서진, 도현, 송예지, 윤 선생님 | `A clear, high autumn sky over the school's dirt playground, ginkgo and maple trees in full yellow and red. A big pile of raked fallen leaves in the middle. {DOHYEON} leaps into the pile with his arms spread, leaves flying. {YEJI} stands beside it laughing out loud for the first time, one hand half covering her mouth, surprised at herself. {SEOJIN} kneels holding up one bright red maple leaf, a diary tucked under her arm. {TEACHER} +다리③ sits on a bench at the edge of the playground with his cane across his knees, watching with a gentle smile. Only these four characters appear.` |
| 2.3 | `evt_outing_winter.png` · `EVT_OUTING_WINTER` 겨울 소풍 | `[겨울]` | 서진, 도현, 윤 선생님, 박 선생님 | `The morning after an overnight snowfall, the school playground completely white and untouched except for footprints. {SEOJIN_WINTER} winds her own knitted scarf around the neck of a freshly built snowman with bottle-cap buttons and a pencil for a nose, her breath fogging. {DOHYEON} (a scarf over his green track jacket) kneels next to it shaping a second, much smaller snowman. {TEACHER} +다리④ sits on a bench at the edge of the playground wrapped in a blanket, a crutch leaning beside him, smiling and waving a gloved hand. {NURSE} stands behind the bench with her hands on his shoulders, laughing. The school building in the background with snow on the windowsills. Only these four characters appear.` |
| 2.4 | `evt_trip_depart.png` · `EVT_TRIP_DEPART` 수학여행 출발 | `[가을]` | 서진, 도현, 송예지, 윤 선생님 | `Early autumn dawn at the school's front gate, the chain unhooked and the gate half open onto an empty road leading toward hills. {SEOJIN}, {DOHYEON} and {YEJI} stand just outside the gate with full backpacks (rolled blankets strapped on top, water bottles, a tin cup hanging), Seojin holding a folded hand-drawn map. {TEACHER} +다리② stands inside the gate leaning on his crutch, a new rubber tip wound on its end, giving them a small farewell wave and a crooked smile. Soft pink dawn light. Only these four characters appear.` |
| 2.5 | `evt_trip_return.png` · `EVT_TRIP_RETURN` 수학여행 귀환 | `[가을]` | 서진, 도현, 송예지, 윤 선생님 | `Sunset on the road back to the school, the school roof and water tanks coming into view. {SEOJIN}, {DOHYEON} and {YEJI} walk toward the gate with dusty, half-empty backpacks, tired but relieved; Dohyeon points ahead at the school. Seojin holds a small seashell in her open palm. At the open gate {TEACHER} +다리③ waits leaning on his cane, raising one hand in welcome. Long golden shadows. Only these four characters appear.` |
| 2.6 | `evt_cal_summer_festival.png` · `EVT_CAL_SUMMER_FESTIVAL` 여름 축제 날 | `[여름]` | 서진, 윤 선생님, 도현 | `A warm summer night on the school rooftop. No fireworks: instead a sky full of stars and a faint Milky Way above a completely dark city skyline, a dark river beyond it. {SEOJIN_SUMMER} sits on a spread mat holding a crumpled festival flyer (a fireworks illustration, no readable text) in her lap, looking up. {TEACHER} +다리② sits beside her with his crutch laid on the mat, pointing up at a constellation with a slightly unsure, amused face. {DOHYEON} (track jacket open, sleeves pushed up) lies on his back next to them, laughing. A small lantern turned down low. Only these three characters appear.` |

**쓰는 법 메모**
- 모든 컷이 4명 이하라 시트를 전부 넣을 수 있다.
- 2.3은 4막(44~48주)이라 선생님이 병세(④)로 나온다. 재감염 전 판에서는 조금 아파 보이지만, 겨울 소풍 대본이 두 갈래 다 선생님을 밖(또는 창가)에 두니 크게 어긋나지 않는다. 너무 아파 보이면 `+다리③`(의족·지팡이)에 `wrapped in a blanket`만 붙여 다시 받는다.
- 2.4·2.5는 같은 판의 이틀이다. 배낭·옷 색이 두 컷에서 같게 나오는지 본다(같은 시트·같은 배낭 문구).
- 송예지는 2막 이후에 합류하는 동료라, 그를 만나지 않은 판에도 이 컷이 나온다. 13차와 같이 "그 판에 없을 수도 있는 동료"는 감수한다. 마음에 걸리면 2.2·2.4·2.5에서 `{YEJI}`를 빼고 세 명으로 받는다.
- 받은 뒤: `ArtInbox/illustrations/redo17/` → 덮어쓰기 → 13차처럼 비교 시트(예전 | 새 그림)를 만들어 확인한다.

---

## 3. 추가 점검: 2차·5차 이벤트 컷 (17장)

2차·5차 컷도 인물 설정화가 생기기 전에 받았다. 그래서 얼굴이 보이는 사람이 서진·동료·선생님과 다르다. 판마다 갈리는 인물(4막 선생님의 상태, 합류하지 않았을 수 있는 동료)은 어느 판에서나 맞도록 **꼭 필요한 컷에만** 넣었다.

### 3.0 한눈에 보기

| 번호 | 파일 · 이벤트 | 시기 | 지금 그림의 문제 | 나올 인물 | 우선 |
|---|---|---|---|---|---|
| 3.1 | `evt_cal_kimjang` · `EVT_CAL_KIMJANG` 김장 | 11월 21일 (36주, 3막) | 앞치마 두른 모르는 학생 다섯 | 서진, 도현, 송예지, 윤 선생님 | **P1** |
| 3.2 | `evt_cal_daeboreum` · `EVT_CAL_DAEBOREUM` 정월대보름 | 2월 20일 (49주, 4막) | 금발 안경 소년 등 모르는 넷 | 서진, 도현, 송예지, 윤 선생님 | **P1** |
| 3.3 | `evt_a2_ration_fight` · `EVT_A2_RATION_FIGHT` 배식 다툼 | 18주~ 아침 (2막) | 모르는 남녀 넷 | 서진, 도현, 송예지, 윤 선생님 | **P1** |
| 3.4 | `evt_day_intrusion` · `EVT_DAY_INTRUSION` 침입 | 3막 낮 (시스템 이벤트) | 바리케이드를 미는 모르는 학생 다섯 | 서진, 도현, 송예지, 윤 선생님 (+우비 그림자) | **P1** |
| 3.5 | `evt_a4_first_snow` · `EVT_A4_FIRST_SNOW` 첫눈 | 41주~ 낮 (4막) | 곱창 끈으로 머리를 묶은 다른 소녀 | 서진, 도현, 송예지 | **P1** |
| 3.6 | `evt_a4_ubc_last_show` · `EVT_A4_UBC_LAST_SHOW` 마지막 방송 | 51주~ 밤 | 담요 두른 모르는 넷 | 서진, 도현, 송예지, 박 선생님 | P2 |
| 3.7 | `evt_a4_cure_hint` · `EVT_A4_CURE_HINT` 처치의 실마리 | 4막 밤 | 조끼 입은 소년과 다른 소녀 | 박 선생님, 서진, 윤 선생님 | **P1** |
| 3.8 | `evt_a4_fuel` · `EVT_A4_FUEL` 연료 | 44~46주 (4막) | 난로 앞 모르는 소년·소녀 | 서진, 도현, 윤 선생님 | **P1** |
| 3.9 | `evt_a4_notice` · `EVT_A4_NOTICE` 접수 대장 | 41주~ (4막) | 목도리 소녀와 모르는 소년 | 서진, 윤 선생님 | **P1** |
| 3.10 | `evt_a4_research_memo` · `EVT_A4_RESEARCH_MEMO` 연구동 메모 | 4막 (정수장 탐험 뒤) | 화이트보드 앞 모르는 소녀 둘 | 서진, 윤 선생님 | P2 |
| 3.11 | `evt_a4_seat_choice` · `EVT_A4_SEAT_CHOICE` 좌석 고르기 | 52주 5일 밤 | 갈색 머리·조끼의 다른 소녀 | 서진, 윤 선생님(완치), 박 선생님, 도현 | **P1** |
| 3.12 | `evt_a4_seat_count` · `EVT_A4_SEAT_COUNT` 여덟 자리 | 41~44주 밤 | 어두운 모르는 무리 | 서진, 도현, 송예지, 박 선생님 | P2 |
| 3.13 | `evt_a4_ubc_relay` · `EVT_A4_UBC_RELAY` 명단 부르기 | 46주~ 밤 | 서진 닮은 아이 말고는 모르는 여섯 | 서진, 도현, 송예지, 박 선생님 | P2 |
| 3.14 | `evt_a4_waterworks_map` · `EVT_A4_WATERWORKS_UNLOCK`·`_LATE` 정수장 지도 | 40주~ 밤 | 지도 둘레 모르는 넷 | 서진, 박 선생님, 윤 선생님 | P2 |
| 3.15 | `evt_sv_attendance` · `EVT_SV_04_FAREWELL` 선배의 출석 | 50주~ 밤 | 갈색 머리·리본의 다른 소녀 | 서진, 선배 | **P1** |
| 3.16 | `evt_sv_recognized` · `EVT_SV_03_RECOGNIZED` 알아본 남자 | 40~48주 (겨울 거리) | 선배가 흉터 없는 패딩 차림, 서진 머리가 길다 | 서진, 선배 (+우비 남자) | P2 |
| 3.17 | `evt_cal_chuseok` · `EVT_CAL_CHUSEOK` 추석 | 10월 6일 (30주, 3막) | 금발 청년·땋은 머리 소녀 등 뒷모습 다섯 | 서진, 도현, 송예지, 윤 선생님 | P2 |

날짜 → 주차는 시작일 3월 16일(`BalanceConfig.StartMonth`·`StartDayOfMonth`)에서 셌다.

### 3.0.1 확인하고 그대로 두는 컷

| 컷 | 까닭 |
|---|---|
| `evt_a4_truth_link` (5차) | 혼자 앉은 서진. 머리가 조금 길지만 단발·교복·넥타이가 맞다 |
| `evt_a3_mart_split` (2차) | 마트 사람들과 떠나는 오 씨 일행. 인물 시트가 없는 이름 없는 어른들이다 |
| `evt_a4_finale_raid`, `evt_cal_new_year` (2차) | 어두운 뒷모습뿐이라 누군지 가려지지 않는다. 바꾸고 싶으면 다음으로 미룬다 |
| 12·14·15차 컷 전부 | 인물 시트를 넣고 받아서 맞다 |

### 3.0.2 장면을 고른 까닭

- 4막 선생님은 판에 따라 병세·누움·격리·완치로 갈린다. 그래서 갈래 없이 선생님이 나오는 컷(3.2·3.7·3.8·3.9·3.10·3.14)에만 넣고 `+다리④`(담요·열)로 그렸다. 갈래마다 다르게 나오는 컷(3.5·3.6·3.12·3.13)에서는 선생님을 뺐다. 3.11은 완치 판에만 나오니 `+다리⑤`다.
- 4막 동료는 판에 따라 합류가 갈린다. 13차처럼 가장 자주 함께하는 도현·송예지·박 선생님으로 잡았다. 하린·선배는 자기 이야기 컷(3.15·3.16)에만 넣었다.
- 3.4 침입: 들어오는 사람들은 우비 차림의 깡패(사거리 파)다. 이름 없는 사람이라 시트 없이 젖빛 유리 너머 그림자로 둔다.
- 3.16 우비 남자도 이름 없는 사람이다. 시트 없이 문구로만 그리고, 두건으로 얼굴을 가린다.

### 3.1 프롬프트

| 번호 | 파일 · 이벤트 | 계절 | 참고 시트 | 장면 (영어) |
|---|---|---|---|---|
| 3.1 | `evt_cal_kimjang.png` · `EVT_CAL_KIMJANG` 김장 | `[가을]` | 서진, 도현, 송예지, 윤 선생님 | `A cold late-autumn afternoon in a school home economics room, frosted windows, bare trees outside. A big red plastic tub in the middle holds small, hard home-grown napa cabbages, sliced radish and wilted mustard greens, smeared with red chili paste. {SEOJIN}, {DOHYEON} and {YEJI} wear red rubber gloves and mismatched aprons over their clothes, rubbing paste into the cabbage leaves, their hands red from the cold, eyes watering from chili powder while they laugh. {TEACHER} +다리③ sits on a stool beside the tub salting a cabbage, his cane hooked on the table edge, smiling. Two large plastic kimchi containers wait by the door. Only these four characters appear.` |
| 3.2 | `evt_cal_daeboreum.png` · `EVT_CAL_DAEBOREUM` 정월대보름 | `[겨울]` | 서진, 도현, 송예지, 윤 선생님 | `A clear winter evening at the edge of the snowy school playground, a huge bright full moon rising over the school roof. A small fire burns in an oil drum. {SEOJIN_WINTER}, {DOHYEON} (a scarf over his green track jacket) and {YEJI} (a padded coat over her blazer) stand around it cracking dried beans between their teeth, wincing and laughing. {TEACHER} +다리④ sits on a chair close to the fire wrapped in a blanket, holding a few dried beans in his open palm, smiling up at the moon. Breath fogging, snow glittering. Only these four characters appear.` |
| 3.3 | `evt_a2_ration_fight.png` · `EVT_A2_RATION_FIGHT` 배식 다툼 | `[여름]` | 서진, 도현, 송예지, 윤 선생님 | `Morning in a classroom shelter, an open food crate on a desk with only a few dented cans left. {DOHYEON} (track jacket tied around his waist, T-shirt) points at the crate, upset, his voice raised. {YEJI} stands across from him, arms folded, tired and defensive, holding up one finger as if to say "just one". {SEOJIN_SUMMER} stands between them looking down at the crate, about to decide. {TEACHER} +다리② leans on his crutch by the blackboard, silent, watching Seojin. Tense, flat morning light. Only these four characters appear.` |
| 3.4 | `evt_day_intrusion.png` · `EVT_DAY_INTRUSION` 침입 | (계절 없음) | 서진, 도현, 송예지, 윤 선생님 | `Daytime, a school classroom doorway barricaded with stacked desks, chairs and a wooden cabinet. Behind the frosted glass of the door and corridor windows, the blurred silhouettes of several men in rain ponchos are moving closer. {SEOJIN} and {DOHYEON} brace their shoulders against the barricade, faces tense. {YEJI} crouches behind a desk gripping a flashlight. {TEACHER} +다리③ stands just behind them leaning on his cane, one arm out to keep them back, staring at the door. No weapons raised, no injuries. Only these four characters appear, plus the faceless silhouettes behind the glass.` |
| 3.5 | `evt_a4_first_snow.png` · `EVT_A4_FIRST_SNOW` 첫눈 | `[겨울]` | 서진, 도현, 송예지 | `Morning, the first snow of winter, a strange white brightness pouring through a classroom window. Outside, the playground, the gate and a distant traffic light are all covered in fresh untouched snow. {SEOJIN_WINTER} stands at the window with both hands on the cold glass, her breath fogging it, looking out quietly. A little farther along the window {DOHYEON} rests his forehead against the glass, lost in thought. {YEJI} draws something small with one finger on the fogged pane, about to wipe it away with her sleeve. Soft, silent light. Only these three characters appear.` |
| 3.6 | `evt_a4_ubc_last_show.png` · `EVT_A4_UBC_LAST_SHOW` 마지막 방송 | `[겨울]` | 서진, 도현, 송예지, 박 선생님 | `A winter night in a classroom shelter, frost on the windows. An old portable radio glows on a desk, playing an upbeat song. {DOHYEON} has jumped up first, bouncing his shoulders to the beat with a big grin. {SEOJIN_WINTER} sits wrapped in a blanket tapping her foot, starting to laugh. {YEJI} beside her claps softly along. {NURSE} sits with a tin mug in both hands, smiling and nodding to the rhythm. A single lantern, warm light. Only these four characters appear.` |
| 3.7 | `evt_a4_cure_hint.png` · `EVT_A4_CURE_HINT` 처치의 실마리 | `[겨울]` | 박 선생님, 서진, 윤 선생님 | `Night in the school infirmary, a desk lamp the only light. {NURSE} has just closed a thick research file, holding her reading glasses in one hand and pressing the bridge of her nose with the other, serious but kind. {SEOJIN_WINTER} sits across the desk with an open observation notebook, pencil ready, a thermometer beside it, listening hard. In the background {TEACHER} +다리④ lies on the infirmary bed under blankets, head turned toward them with a faint wry smile. Only these three characters appear.` |
| 3.8 | `evt_a4_fuel.png` · `EVT_A4_FUEL` 연료 | `[겨울]` | 서진, 도현, 윤 선생님 | `A freezing winter day in a classroom shelter, frost on the inside of the windows, a skin of ice on a water bottle. A small iron stove burns in the middle. {SEOJIN_WINTER} kneels in front of it feeding in a broken wooden school desk top carved with old doodles and a small heart. {DOHYEON} breaks a chair leg over his knee beside her, his breath fogging. {TEACHER} +다리④ sits close to the stove under two layered blankets, warming his hands, watching the carved desk burn with a sad, gentle face. Only these three characters appear.` |
| 3.9 | `evt_a4_notice.png` · `EVT_A4_NOTICE` 접수 대장 | `[겨울]` | 서진, 윤 선생님 | `A cold winter afternoon in the school staff room, rows of grey steel cabinets, an old document intake ledger open on a desk with one line stamped but its processing column empty. {SEOJIN_WINTER} holds out a yellowed official letter to {TEACHER} +다리④, who sits at the desk with a blanket over his shoulders, taking off his glasses with one hand, looking at the paper with a heavy, quiet face. Grey window light. Only these two characters appear.` |
| 3.10 | `evt_a4_research_memo.png` · `EVT_A4_RESEARCH_MEMO` 연구동 메모 | `[겨울]` | 서진, 윤 선생님 | `Night by a small iron stove in the classroom shelter. {SEOJIN_WINTER} sits on a low stool reading aloud from a notebook page of hastily copied whiteboard notes (unreadable scribbles, one line underlined hard). {TEACHER} +다리④ sits across from her under a blanket, listening in silence, his eyes fixed on the fire. Orange firelight, deep shadows. Only these two characters appear.` |
| 3.11 | `evt_a4_seat_choice.png` · `EVT_A4_SEAT_CHOICE` 좌석 고르기 | `[겨울]` | 서진, 윤 선생님, 박 선생님, 도현 | `Night, two days before the rescue, a classroom lit by one candle. {SEOJIN_WINTER} sits at a desk in the foreground looking down at a sheet of paper with eight hand-drawn pencil boxes, an eraser beside it, her face torn. Around the room in soft candlelight: {TEACHER} +다리⑤ leans on his cane with a calm, kind look; {NURSE} stands with her arms folded; {DOHYEON} sits on a desk swinging his legs, trying to look brave. All of them are looking at Seojin. Moon through the window. Only these four characters appear.` |
| 3.12 | `evt_a4_seat_count.png` · `EVT_A4_SEAT_COUNT` 여덟 자리 | `[겨울]` | 서진, 도현, 송예지, 박 선생님 | `A winter night in the classroom shelter, an old radio on a desk crackling. {SEOJIN_WINTER}, {DOHYEON}, {YEJI} and {NURSE} sit around it wrapped in blankets, silent after the broadcast. Seojin is slowly folding down her fingers one by one, counting; Dohyeon watches her fingers; Yeji hugs her knees; the nurse looks at the radio with a tight mouth. Moonlight through frosted windows, one lantern. Only these four characters appear.` |
| 3.13 | `evt_a4_ubc_relay.png` · `EVT_A4_UBC_RELAY` 명단 부르기 | `[겨울]` | 서진, 도현, 송예지, 박 선생님 | `A winter night, a handheld radio transceiver on a desk among blankets and tin mugs. {SEOJIN_WINTER} holds the transmitter close to her mouth, calling out names slowly and clearly, eyes wet. {DOHYEON} leans toward the radio answering "Here!" out loud with a grin. {YEJI} sits with both hands pressed together tightly, listening for her own name. {NURSE} smiles, pointing at herself as if to say "add school nurse". A lantern and moonlight. Only these four characters appear.` |
| 3.14 | `evt_a4_waterworks_map.png` · `EVT_A4_WATERWORKS_UNLOCK` 정수장 지도 | `[겨울]` | 서진, 박 선생님, 윤 선생님 | `Night, a large old city map spread on a classroom floor, a river running across it and a water treatment plant beside it circled two or three times in red pen. {SEOJIN_WINTER} kneels over the map tracing a route with her finger, a flashlight lying beside it. {NURSE} kneels opposite, pointing at the circle with a worried face. {TEACHER} +다리④ sits against the wall under a blanket, watching silently, knowing he cannot stop her. Only these three characters appear.` |
| 3.15 | `evt_sv_attendance.png` · `EVT_SV_04_FAREWELL` 선배의 출석 | `[겨울]` | 서진, 선배 | `Night, a few days before the rescue, a desk lamp over an open attendance book. {SEOJIN_WINTER} sits at the desk holding the book open, looking up. {SENIOR} stands beside her holding a pencil above the last line, where only "2nd year" is written in the teacher's handwriting (no readable text), about to draw a small circle next to it. His face is quiet, almost a smile. Warm lamp light, dark window. Only these two characters appear.` |
| 3.16 | `evt_sv_recognized.png` · `EVT_SV_03_RECOGNIZED` 알아본 남자 | `[겨울]` | 서진, 선배 | `A cold grey winter day at a ruined city crossroads, wet snow on the asphalt, a dead traffic light. A rough-looking man in a dark hooded rain poncho, his face half hidden by the hood, blocks the way and stares at {SENIOR}, half recognizing him. The senior stands still with a heavy pack on his back, looking straight at the man with no expression, his knuckles white on the strap. {SEOJIN_WINTER} steps half in front of him, chin up. Tense, quiet. Only these two named characters appear, plus the poncho man (no reference sheet).` |
| 3.17 | `evt_cal_chuseok.png` · `EVT_CAL_CHUSEOK` 추석 | `[가을]` | 서진, 도현, 송예지, 윤 선생님 | `An autumn night on the school rooftop, a huge golden full moon over the dark city, water tanks and a chain-link fence. A low folding table on a mat holds canned fish, a small bowl of rice and one apple cut into equal pieces. {SEOJIN}, {DOHYEON} and {YEJI} sit around it facing the moon; Dohyeon grabs the biggest apple piece with a grin. {TEACHER} +다리③ sits at the end with his cane beside him, eyes closed for a moment as if remembering someone. Only these four characters appear.` |

**쓰는 법 메모 (3절)**
- 넣는 곳·잇는 곳은 2절과 같다(`ArtInbox/illustrations/redo17/`, 같은 파일 이름, 옛 그림은 `backup_redo17/`).
- 3.5~3.16의 `{SEOJIN_WINTER}` 패딩 색이 4막 컷마다 바뀌지 않는지 본다.
- 3.4는 싸우는 장면이 아니라 '막아선 순간'이다. 무기를 들거나 다친 모습이 나오면 다시 받는다(마스터 프롬프트 13장 안전).
- 3.11은 좌석 선택 엔딩 컷(`EndingDef.Variants`)과 따로다. 이벤트 첫머리 컷만 바꾼다.
- 4막 컷에서 선생님이 너무 아파 보이면, 2.3처럼 `+다리③`에 `wrapped in a blanket`만 붙여 다시 받는다.
