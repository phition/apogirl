# 힉스필드 에셋 17차: 2차 소풍·수학여행·여름 축제 컷 인물 맞춰 다시 받기 (2026-11-26)

2차([HIGGSFIELD_ASSETS_2.md](HIGGSFIELD_ASSETS_2.md), 2026-11-21 연결)의 소풍·수학여행·여름 축제 컷은 인물 설정화가 생기기 전에 받았다. 그래서 게임 인물이 아닌 낯선 학생·어른이 나온다. 9차 다시 그리기·13차 인물 맞추기에서도 빠졌다.

2026-11-26에 소풍·수학여행·행사 컷 11장을 인물 초상과 나란히 놓고 보았다. **다른 사람이 나온 6장**만 다시 받는다.

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
| `{TEACHER}` | `char_teacher_sheet.png` | `Mr. Yoon, a Korean male teacher in his mid-40s with short black hair, thin glasses and a grey cardigan over a white shirt, dark trousers` + 아래 다리 문구 하나 |

**윤 선생님 다리 문구** (13차 1절과 같다)
- ② 1막 후반~2막, 목발: `leaning on a homemade crutch made from a mop handle and tape, his left trouser leg folded and pinned below the knee`
- ③ 2막 후반~3막, 의족: `a simple homemade prosthetic lower leg of plastic pipe and padding visible below the left trouser hem, a cane`
- ④ 3막 후반~4막, 병세: `pale and feverish, wrapped in a blanket, the left trouser leg folded below the knee, a crutch leaning nearby`

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
