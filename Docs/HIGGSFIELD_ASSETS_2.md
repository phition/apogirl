# Higgsfield 에셋 생성 문서 2차 (2026-11-21)

메인 UI 개편([UI_REDESIGN_PROPOSAL.md](UI_REDESIGN_PROPOSAL.md))에 필요한 **주인공 입상 조각**과, 게임이 커지면서 그림이 비어 있는 **인물·이벤트 컷·엔딩·UI 에셋**을 모았다.

- 쓰는 법·모델 선택·공통 스타일 공식·안전 문구는 [HIGGSFIELD_ASSETS.md](HIGGSFIELD_ASSETS.md) 0장을 그대로 따른다. 이 문서에서 `[F-ILLU]`는 그 문서 0.3의 일러스트 공식, `[안전]`은 0.4의 안전 문구다.
- 인물 기준 시트(`char_*_sheet.png`)는 이미 받은 것을 **참고 이미지**로 넣는다.
- 프롬프트는 영어, 설명은 한국어.
- 넣는 곳: `ArtInbox/` 아래 이 문서의 폴더·파일 이름 그대로.

## 0. 한눈에 보기

| 장 | 내용 | 장수 | 우선 |
|---|---|---|---|
| 1 | 주인공 입상 조각 (시험분 6장 → 전체) | 시험 6 / 전체 약 30 | **P1** |
| 2 | 윤 선생님 입상 (허브 장면용, 선택) | 4 | P3 |
| 3 | 새 인물 (기준 시트·표정) | 시트 5 + 표정 약 18 | P2 |
| 4 | 이벤트 컷 추가 | 약 45 | P2~P3 |
| 5 | 엔딩 일러스트 빠진 6종 | 6 | P2 |
| 6 | UI 개편용 에셋 (종이·도장·아이콘) | 약 75 | P1~P2 |
| 7 | 허브 장면 추가분 | 약 10 | P2~P3 |

**먼저 만들 것**: 1.2의 시험분 6장. 겹쳤을 때 얼굴과 몸이 맞는지 확인한 뒤에 나머지를 만든다.

---

## 1. 주인공 입상 조각 (P1)

### 1.1 만드는 방식

AI는 "옷만 따로", "표정만 따로" 같은 **딱 맞는 투명 조각**을 직접 그리지 못한다. 그래서 이렇게 한다.

1. **기준 입상 1장**을 먼저 확정한다 (자세·화면 위치·크기의 기준).
2. 나머지는 모두 기준 입상을 참고 이미지로 넣고 **"한 가지만 바꾼 같은 그림"**으로 만든다 (편집 방식).
3. 받은 그림에서 바뀐 부분만 잘라 조각으로 만들고 위치를 맞추는 일은 내가 한다.

지켜야 할 것:
- **자세·구도·크기를 절대 바꾸지 않는다.** 모든 프롬프트에 아래 `[고정]` 문구를 붙인다.
- 비율 **2:3 세로**, 2k 이상. 배경은 **단색 밝은 회색**(투명이 되면 투명).
- 머리 꼭대기부터 신발까지 다 나오게, 위아래 여백 조금.
- 모델: 기준 입상은 `gpt_image_2_6`, 편집은 `seedream_v5_pro` 또는 `nano_banana_2_1` (참고 이미지 = `stand_base.png` + `char_seojin_sheet.png`).

`[고정]` — 편집 프롬프트 맨 뒤에 붙인다
```
Keep everything else exactly the same as the reference image: same girl, same face, same body proportions, same standing pose, same camera angle, same framing and canvas position, same lighting, same art style, same plain light grey background. Do not move, resize or re-pose the character.
```

### 1.2 시험분 (이 6장부터)

| 파일 (`ArtInbox/standing/`) | 내용 |
|---|---|
| `stand_base.png` | 기준 입상 (춘추복, 단발, 무표정) |
| `stand_outfit_summer.png` | 옷만 하복으로 |
| `stand_outfit_padding.png` | 옷만 패딩으로 |
| `stand_face_smile.png` | 표정만 미소로 |
| `stand_face_tired.png` | 표정만 지침으로 |
| `stand_mark_bandage_arm.png` | 팔에 붕대만 추가 |

**기준 입상** `stand_base.png`
```
[F-ILLU 첫 문장만]
Full-body standing character illustration of a 16-year-old Korean high school girl, first-year student, the same girl as the reference sheet. Shoulder-length straight black hair, plain quiet face, tired but observant eyes, calm neutral expression with lips closed.
She stands facing three-quarters to the right in a relaxed upright pose, feet slightly apart, both arms hanging naturally at her sides a little away from the body, hands empty and open, nothing in her hands, no bag.
Outfit: modest navy school blazer, white shirt, navy tie, knee-length navy plaid skirt, black tights, worn white sneakers; the uniform slightly dusty and wrinkled from days of survival.
Whole figure visible from the top of the head to the shoes with a small margin, centered, vertical 2:3 composition, even soft lighting from the upper left, plain light grey background, visual novel standing sprite.
[안전]
```
- 두 팔을 몸에서 살짝 뗀 자세가 중요하다. 팔이 몸에 붙으면 옷 조각을 나눌 때 겹친다.
- 손에 아무것도 없어야 무기·가방을 나중에 얹을 수 있다.
- 마음에 드는 한 장이 나올 때까지 여기서 고른다. **이 한 장이 모든 조각의 기준이다.**

### 1.3 옷 (각 1장, 편집)

공통 앞부분: `Edit the reference image: change only her clothes.` → 아래 문구 → `[고정]` → `[안전]`

| 파일 | 쓰임 | 바꿀 문구 |
|---|---|---|
| `stand_outfit_summer.png` | 여름 (6~8월) | `She now wears the summer school uniform: a short-sleeved white shirt with a small navy ribbon tie and a light knit vest, the same knee-length navy plaid skirt, white ankle socks instead of tights, the same worn sneakers.` |
| `stand_outfit_winter.png` | 겨울 (12~2월) | `She now wears the winter outfit: the navy blazer under a long charcoal duffle coat left open, a thick knitted muffler around her neck, black tights, the same sneakers.` |
| `stand_outfit_gym.png` | 훈련·작업할 때 | `She now wears the school gym uniform: a loose navy track jacket zipped halfway over a white T-shirt and matching loose navy track pants, the same sneakers.` |
| `stand_outfit_guard.png` | 방어구: 보호대 | `Over her navy school uniform she now wears school sports protective gear: a red-and-white padded taekwondo chest protector, black elbow pads and black knee pads.` |
| `stand_outfit_padding.png` | 방어구: 패딩 | `She now wears a thick puffy long down jacket in dark olive green over her uniform, zipped up to the chin, the hem reaching her thighs, the plaid skirt and black tights visible below.` |
| `stand_outfit_riot.png` | 방어구: 진압복 | `Over her navy blazer she now wears an oversized black police riot protective vest with shoulder pads (no readable lettering), and black forearm guards; it is clearly an adult's gear that is too big for her.` |

- 탐험 픽셀 캐릭터의 옷 4벌(교복·보호대·패딩·진압복)과 색을 맞췄다.
- 계절 옷과 방어구가 겹칠 때(예: 겨울 + 패딩)는 방어구 쪽을 보여 준다.

### 1.4 머리 길이 (각 1장, 편집)

공통 앞부분: `Edit the reference image: change only her hair.`

| 파일 | 때 | 바꿀 문구 |
|---|---|---|
| (기준 입상) | 봄 | 어깨까지 오는 단발 |
| `stand_hair_mid.png` | 여름~가을 | `Her black hair has grown past her shoulders and is tied in a low loose ponytail with a plain rubber band, a few strands falling beside her face.` |
| `stand_hair_long.png` | 겨울 | `Her black hair has grown long, down to the middle of her back, with uneven bangs she clearly trimmed herself, the hair worn down and slightly tangled.` |

### 1.5 표정 (각 1장, 편집)

공통 앞부분: `Edit the reference image: change only her facial expression.` 얼굴만 바뀌어야 하므로 머리카락·목·어깨는 그대로라고 `[고정]`에 더해 한 번 더 적는다: `Hair, neck and shoulders stay pixel-identical.`

| 파일 | 언제 | 바꿀 문구 |
|---|---|---|
| (기준 입상) | 보통 | 무표정 |
| `stand_face_smile.png` | 스트레스 낮음, 좋은 일 | `small shy smile, eyes softened` |
| `stand_face_bright.png` | 대성공, 희망 높음 | `open happy smile showing a little teeth, eyes bright, eyebrows raised` |
| `stand_face_worried.png` | 자원 부족, 경고 | `worried expression, brows knitted, lips pressed together, eyes looking slightly down` |
| `stand_face_tired.png` | 피로·수면 부채 높음 | `exhausted expression, heavy eyelids half closed, faint dark circles under the eyes, mouth slightly open as if sighing` |
| `stand_face_sad.png` | 스트레스 높음, 상실 | `sad expression, eyes lowered and glistening, holding back tears` |
| `stand_face_determined.png` | 탐험 주간, 결심 | `determined expression, firm gaze forward, jaw set` |
| `stand_face_surprised.png` | 편성 중 반응 | `surprised expression, eyes wide, mouth slightly open` |
| `stand_face_blank.png` | 스트레스 한계 근처 | `empty distant stare, eyes unfocused, face without expression, as if not really here` |

- 머리 길이가 바뀌어도 얼굴 부분만 오려 쓰므로 표정은 한 벌이면 된다.

### 1.6 상태 표시 (각 1장, 편집)

공통 앞부분: `Edit the reference image: add only the following.` 콘텐츠 가이드라인에 따라 상처는 **붕대와 반창고로만** 보인다.

| 파일 | 상태 | 더할 문구 |
|---|---|---|
| `stand_mark_bandage_arm.png` | 부상 | `a clean white bandage wrapped around her left forearm over the sleeve, neatly tied, no blood, no visible wound` |
| `stand_mark_bandage_head.png` | 부상 (심함) | `a small square gauze pad taped on her forehead and a plaster on her cheek, no blood, no visible wound` |
| `stand_mark_bandage_leg.png` | 부상 (다리) | `a white elastic bandage wrapped around her right knee over the tights, no blood` |
| `stand_mark_dirty.png` | 청결 낮음 | `her clothes and hands are noticeably dirtier: dust and dried mud smudges on the blazer and skirt, a smudge on her cheek, hair slightly greasy and messy` |
| `stand_mark_thin.png` | 포만 낮음 (오래 굶음) | `she looks a little underfed: cheeks slightly hollow, the blazer hanging a bit loose on her shoulders; subtle, not extreme` |
| `stand_mark_mask.png` | 감염 처치 중·독감 | `a white cloth face mask covering her nose and mouth` |

- 창백함(감염)과 눈 밑 그늘의 진하기는 그림을 새로 만들지 않고 게임에서 색으로 조절한다.

### 1.7 든 것 (각 1장, 편집)

공통 앞부분: `Edit the reference image: add only the following item.`

| 파일 | 내용 | 더할 문구 |
|---|---|---|
| `stand_item_bag_school.png` | 책가방 | `a plain school backpack worn on both shoulders, straps visible in front, the bag visible behind her back; the bag is a flat mid-grey color` |
| `stand_item_bag_hiking.png` | 등산 가방 | `a large hiking backpack with a rolled sleeping mat on top worn on both shoulders, chest strap buckled; the bag is a flat mid-grey color` |
| `stand_item_bag_tote.png` | 에코백 | `a canvas tote bag hanging from her left shoulder; the bag is a flat mid-grey color` |
| `stand_item_mop.png` | 대걸레 | `she holds a wooden mop upright in her right hand like a staff, the grey string mop head resting on the floor beside her foot` |
| `stand_item_bat.png` | 야구 방망이 | `she holds a wooden baseball bat in her right hand, pointing down beside her leg` |
| `stand_item_pipe.png` | 쇠파이프 | `she holds a plain straight grey steel pipe in her right hand, pointing down beside her leg` |
| `stand_item_baton.png` | 진압봉 | `she holds a short black expandable baton in her right hand, pointing down beside her leg` |
| `stand_item_flashlight.png` | 손전등 | `a small flashlight hangs from a strap on her left wrist` |

- 가방을 회색으로 받는 이유: 게임에서 여섯 가지 색(갈색·파랑·분홍·빨강·노랑·초록)으로 물들인다.
- 무기는 위협적으로 들지 않고 아래로 내린 모습이다.

### 1.8 이달의 사진 (P3, 12장)

일기에 달마다 붙는 폴라로이드. 입상 조각으로 합성해 만들 수 있으므로 **따로 그리지 않아도 된다**. 더 분위기 있는 것을 원할 때만: 3:4, `[F-ILLU]` 전체.

```
A candid instant-photo style snapshot of the same girl in the school [장소], [계절 문구], she [행동], slightly off-center amateur framing, soft natural light, faded colors like an old instant photo.
```
예: 3월 `storage room, early spring / sitting on a gym mat hugging her knees`, 5월 `rooftop / watering vegetables in cut plastic bottles`, 8월 `classroom / fanning herself with a notebook`, 12월 `classroom / wrapped in a blanket beside a small stove`.

---

## 2. 윤 선생님 입상 (P3, 선택)

허브 장면은 픽셀 아트라 선생님 입상이 꼭 필요하지는 않다. 대화 화면을 전신 구도로 바꿀 때 쓴다. 참고 이미지 = `char_teacher_sheet.png`. 2:3 세로, 회색 배경. 다리는 0.4 규칙대로 **접어 고정한 바지**로만 보인다.

| 파일 (`ArtInbox/standing/`) | 때 | 프롬프트 |
|---|---|---|
| `stand_teacher_crutch.png` | 1막 (목발) | `Full-body standing illustration of the same teacher from the reference sheet, Korean man in his mid-40s, glasses, cardigan over a shirt, leaning on a single homemade wooden crutch under his right arm, the left trouser leg folded and pinned below the knee, gentle tired smile, three-quarters view facing left, plain light grey background.` |
| `stand_teacher_seated.png` | 목발 만들기 전 | `...the same teacher sitting on a low stack of gym mats with a blanket over his lap, one hand resting on a textbook, the left trouser leg folded and pinned below the knee...` |
| `stand_teacher_prosthetic.png` | 2막 의족 완성 뒤 | `...standing with a simple homemade prosthetic lower leg made of metal pipe and padding visible below the left trouser hem, a cane in his right hand, standing a little straighter, quiet proud smile...` |
| `stand_teacher_ill.png` | 재감염 뒤 | `...the same teacher sitting wrapped in a blanket, pale and feverish, glasses slightly crooked, still trying to look composed...` |

---

## 3. 새 인물 (P2)

대사에 이름이 나오는데 초상이 없는 인물들이다. **나이와 생김새는 대본에 거의 적혀 있지 않아 아래는 내가 잡은 안이다**(대본에 있는 것: 강철의 모자·우비·두꺼운 점퍼, 민규 열아홉 살·자전거·다리를 조금 젊, DJ는 민규가 "누나"라고 부름, 백은 산 안내원 출신). 바꾸고 싶으면 프롬프트의 그 부분만 고치면 된다. 먼저 기준 시트(16:9, `front view and side view, full body, plain grey background`), 그 시트를 참고로 표정(3:4 상반신, 투명 또는 회색). 폴더 `ArtInbox/illustrations/`.

| 우선 | 파일 | 인물 | 기준 시트 프롬프트 | 표정 |
|---|---|---|---|---|
| P2 | `char_gangcheol_sheet.png` | 강철 (사거리 파 두목, 40대 남성). 악당이지만 아이들은 건드리지 않는 선이 있다 | `Korean man in his 40s, leader of a street gang of survivors, heavy build, weathered face with a short beard, a worn baseball cap pulled low, a dark raincoat over a thick jacket, work boots, a can of red spray paint hooked on his belt, hands in pockets, unreadable calm expression, not cartoonishly evil` | neutral, grin, serious, cap off (모자를 벗어 든 모습) |
| P2 | `char_dj_sheet.png` | DJ 새벽 (UBC 방송, 20대 후반 여성). 밤마다 방송하는 목소리 | `Korean woman in her late 20s, radio DJ turned survivor, tired warm eyes, hair in a messy bun, large studio headphones around her neck, an oversized cardigan over a faded station T-shirt, a handheld radio in one hand` | neutral, smile, worried, crying quietly |
| P2 | `char_mingyu_sheet.png` | 민규 (UBC 운반조, 19세 남성) | `19-year-old Korean young man, thin and tall, courier for a survivor group, windbreaker, bicycle helmet hanging from his backpack, scraped knees, an earnest slightly clumsy look` | neutral, relieved smile, scared |
| P2 | `char_jun_sheet.png` | 준 (사거리 파가 두고 간 아이, 12세쯤 남자아이) | `12-year-old Korean boy, small for his age, an adult-sized dark raincoat with the sleeves rolled up many times, dirty sneakers, guarded eyes, holding a dented tin cup` | neutral, scared, small smile |
| P3 | `char_baek_face.png` | 유랑자 '백' (얼굴이 드러난 뒤, 표정 1장) | 참고 = `char_wanderer_sheet.png`: `same wanderer with the wide-brim hat pushed back, face finally visible: a sun-weathered man in his 50s, former mountain guide, deep wrinkles, calm eyes` | — |
| P3 | `char_oh_sheet.png` | 오 씨 (마트 부반장, 50대 남성). 겁 때문에 나쁜 선택을 하는 사람 | `Korean man in his 50s, former warehouse deputy foreman, thin and tense, receding hair, a quilted work vest, constantly looking over his shoulder, not villainous, frightened` | neutral, angry |
| P3 | `char_rescue_face.png` | 구조대원 (표정 1장) | `Korean rescue worker in an orange rescue jumpsuit and a helmet with a headset, face mostly covered by a mask and goggles, reaching a gloved hand forward` | — |
| P2 | `char_friend_{표정}.png` | 친구 지우 표정 4장 (시트는 있음) | 참고 = `char_friend_sheet.png` | neutral, big smile, worried, crying |

---

## 4. 이벤트 컷 추가 (16:9)

이벤트 224개 중 그림이 있는 것은 50개다. 아래는 그림이 있으면 장면이 크게 사는 것들만 골랐다. 모두 앞에 `[F-ILLU]`, 끝에 `[안전]`. 인물이 나오면 기준 시트를 참고 이미지로 넣는다. 폴더 `ArtInbox/illustrations/`.

### 4.1 1막 (P2)

| 파일 | 이벤트 | 프롬프트 |
|---|---|---|
| `evt_a1_store_gate.png` | 편의점·약국 해금 | `View from inside a school gate looking out at a quiet street in spring: an abandoned car with its door left open, a single shoe on the road, a convenience store awning in the distance, a girl seen from behind holding a backpack strap` |
| `evt_a1_attendance.png` | 출석부 | `Close-up of an open class attendance book on a teacher's desk, rows of names with check marks stopping at the same date, a red pen resting on the page, soft window light, no people` |
| `evt_a1_far_rotor.png` | 먼 헬기 소리 | `Night, a girl and a teacher on crutches at a dark classroom window looking up at a tiny distant blinking light in the sky, seen from behind, spring night` |
| `evt_a1_teacher_first_day.png` | 선생님의 첫날 (회상) | `Dim school corridor seen from very low to the floor, a trail of scuff marks leading to a nurse office door left ajar, overturned chair, evening light, no people, no blood` |
| `evt_talk_rooftop_stars.png` | 밤 대화 5 | `School rooftop at night, a girl and a teacher on crutches seen from behind looking at an enormous starry sky over a completely dark city, one tiny light far away` |
| `evt_cal_midterm.png` | 중간고사 | `A single school desk by a classroom window, a hand-written test paper on recycled paper and a sharpened pencil, spring light, a teacher's hand placing a red colored pencil nearby` |
| `evt_cal_teachers_day.png` | 스승의 날 | `Close-up of a carnation folded from wrapping paper tucked into the handle of a homemade wooden crutch leaning against a chalkboard, May light` |
| `evt_cal_sports_day.png` | 체육대회 | `Empty school corridor, a girl in socks mid-run with her shoes left by the wall, a teacher sitting against the wall holding up a wristwatch, both trying not to laugh out loud, spring` |
| `evt_cal_birthday.png` | 생일 | `Morning, a cup of instant noodles on the floor beside a sleeping bag, 'happy birthday' handwritten on the lid (illegible scribble), soft light, no people` |
| `evt_dh_knock.png` | 도현의 노크 | `A barricaded classroom door at night seen from inside, a thin line of flashlight under the door, a small shadow of a boy visible through the frosted glass` |

### 4.2 2막 (P2)

| 파일 | 이벤트 | 프롬프트 |
|---|---|---|
| `evt_a2_nurse_join.png` | 박 선생님 합류 | `A school nurse in her 40s in a white gown over a padded vest standing in a classroom doorway with a heavy medical bag, summer evening light behind her` |
| `evt_a2_prosthetic.png` | 의족 완성 | `A teacher standing for the first time without crutches, holding the edge of a desk, a simple homemade prosthetic lower leg visible below his trouser hem, a girl and a nurse watching with held breath, summer classroom` |
| `evt_a2_sageori_mark.png` | 사거리 파의 표시 | `A school perimeter wall at dawn with a large rough mark sprayed in red paint (an abstract symbol, no letters), paint still dripping, an empty street, summer` |
| `evt_a2_sageori_toll.png` | 통행세 | `A school gate seen from inside, three adult figures in dark raincoats waiting outside the bars, faces hidden under hoods, a girl standing well back holding a box of cans, overcast` |
| `evt_a2_ubc_leaflet.png` | UBC 전단 | `A photocopied leaflet stuck to a utility pole with tape, a hand-drawn radio tower and frequency numbers smudged by rain (illegible), cicadas summer street` |
| `evt_a2_subway_gate.png` | 지하철역 해금 | `A subway station entrance stairway descending into darkness, shutters half down, summer weeds, a girl at the top step with a flashlight` |
| `evt_a2_residential.png` | 주택가 해금 | `A narrow residential alley of red-brick villas, laundry still hanging faded on a line, a tricycle on its side, summer afternoon, no people` |
| `evt_a2_bystander.png` | 송예지와의 재회 | `A girl in an oversized school uniform sitting on the floor of a subway kiosk hugging her knees, looking up startled into a flashlight beam` |
| `evt_a2_ration_fight.png` | 배급 다툼 | `A classroom table with canned food divided into uneven piles, four people standing apart from each other not meeting eyes, tense summer evening, a candle` |
| `evt_cal_rainy_season.png` | 장마 | `Rain pouring on a school rooftop, buckets and cut bottles overflowing with rainwater, a girl in a raincoat checking them` |
| `evt_cal_summer_festival.png` | 여름 축제 | `A classroom at night decorated with paper lanterns made from notebooks, a few candles, people sharing sliced watermelon-shaped paper crafts and real canned fruit, warm` |
| `evt_cal_liberation.png` | 광복절 | `A small paper Korean flag drawn in crayon taped to a classroom window, summer sky behind, a teacher's hand smoothing the tape` |

### 4.3 3막 (P2)

| 파일 | 이벤트 | 프롬프트 |
|---|---|---|
| `evt_a3_mart_group.png` | 마트 사람들 | `Inside a large supermarket warehouse, a dozen adult survivors around a stove made from a drum, pallets stacked as walls, a broad-shouldered man in a reflective work vest stepping forward, autumn` |
| `evt_a3_police.png` | 파출소 | `A small Korean police substation interior, overturned chairs, an open equipment locker, dust in the autumn light, a girl standing in the doorway` |
| `evt_a3_wanderer.png` | 유랑자 | `A lone adult wanderer with a wide-brim hat and a big backpack resting at a school gate at dusk, autumn leaves, face in shadow` |
| `evt_a3_ubc_sos.png` | 민규를 찾아서 | `A drainage culvert by a river at night, a flashlight beam finding a young man's hand gripping the concrete edge, autumn reeds` |
| `evt_a3_mart_split.png` | 마트의 분열 | `A warehouse loading dock at night, a small group of adults walking away with bags into the dark while others watch from the doorway, autumn` |
| `evt_a3_sageori_kid.png` | 두고 간 아이 | `A small boy in an adult-sized raincoat sitting alone on a curb at a large empty intersection, a dented tin cup beside him, cold autumn morning` |
| `evt_a3_teacher_confess.png` | 선생님의 고백 | `A teacher sitting by a classroom window at night rolling down his sleeve, looking away, a girl standing a few steps off, one candle between them, autumn` |
| `evt_a3_dj_show.png` | DJ의 방송 | `A makeshift radio studio in an office, a woman with headphones leaning toward a microphone, a hand-cranked generator, blankets on the walls for soundproofing, a single warm lamp` |
| `evt_cal_chuseok.png` | 추석 | `A full moon over a school rooftop, a small table with a few rice cakes made from scraps and canned fruit, people seen from behind looking at the moon` |
| `evt_cal_school_festival.png` | 학교 축제 | `A classroom chalkboard covered in colorful chalk drawings and a festival banner without readable text, desks arranged as booths, autumn afternoon` |
| `evt_cal_kimjang.png` | 김장 | `People in rubber gloves around a big basin making a small batch of kimchi in a school home economics room, late autumn, steam on the windows` |
| `evt_day_intrusion.png` | 낮의 침입 | `A classroom door being braced with desks from inside, hands pushing, shadows of adults visible through the frosted corridor windows, daylight` |

### 4.4 4막 (P2)

| 파일 | 이벤트 | 프롬프트 |
|---|---|---|
| `evt_a4_first_snow.png` | 첫눈 | `First snow falling on a school yard seen from a classroom window, a girl's hand on the cold glass, breath fogging` |
| `evt_a4_sageori_gate.png` | 강철의 방문 | `A heavyset man in a thick jacket and a baseball cap standing alone at a snow-covered school gate with a dry twig in his mouth, no weapons, winter dusk` |
| `evt_a4_finale_raid.png` | 마지막 한파의 밤 | `Night, snow, a school wall seen from the rooftop, a line of dark figures with a few flashlights approaching across the white field, tense, no close-up violence` |
| `evt_a4_raid_ally.png` | 사거리가 막아서다 | `Snowy school gate at night, figures in raincoats standing in a line with their backs to the school facing another group in the dark, a man in a cap at the center` |
| `evt_a4_mart_leaves.png` | 마트가 떠나다 | `A convoy of handcarts and bicycles loaded with boxes leaving a supermarket in the snow, a man in a work vest looking back and raising a hand` |
| `evt_a4_mingyu_visit.png` | 민규의 방문 | `A young man with a slight limp arriving at a snowy school gate on a bicycle, holding out a wrapped parcel, shy smile` |
| `evt_a4_ubc_last_show.png` | 마지막 방송 | `A small radio on a classroom desk at night, people gathered around it in blankets listening, the dial glowing, winter` |
| `evt_a4_blood.png` | 수혈 | `A nurse office at night, two cots side by side with a curtain between, a clear tube and a hanging bottle catching lamp light, a nurse's steady hands adjusting a clamp, no needles shown, no blood shown` |
| `evt_a4_farewell.png` | 작별 (동료 공통) | `A school corridor in winter morning light, several packed bags lined up by the wall, a row of indoor shoes left neatly on a shelf` |
| `evt_cal_christmas.png` | 크리스마스 | `A classroom corner, a mop handle stood upright as a tiny Christmas tree decorated with foil and bottle caps, a candle, snow outside` |
| `evt_cal_new_year.png` | 새해 | `School rooftop at dawn, people in blankets watching the first sunrise of the year over the snowy quiet city` |
| `evt_cal_seollal.png` | 설날 | `A bowl of rice cake soup made from scraps on a classroom desk, steam rising, several spoons, winter morning light` |
| `evt_cal_daeboreum.png` | 정월대보름 | `A full winter moon over a school yard, a small careful fire in a metal bucket, hands holding roasted beans` |

### 4.5 소풍·수학여행 (P2)

| 파일 | 이벤트 | 프롬프트 |
|---|---|---|
| `evt_outing_spring.png` | 봄 소풍 | `A small group picnic on a school hill under cherry blossoms, a mat, shared snacks, a teacher on crutches sitting on a tree root, the quiet city below` |
| `evt_outing_summer.png` | 여름 물놀이 | `A shallow clear stream under a bridge in summer, students with trousers rolled to the knees splashing ankle-deep, shoes lined up on a rock, modest clothing` |
| `evt_outing_autumn.png` | 가을 소풍 | `A ginkgo-lined path covered in yellow leaves, a small group walking with backpacks, one person collecting persimmons into a bag` |
| `evt_outing_winter.png` | 겨울 나들이 | `A snow-covered schoolyard, a lopsided snowman wearing a school tie, footprints, people in blankets laughing, winter sun` |
| `evt_trip_depart.png` | 수학여행 출발 | `Dawn at a school gate, a small group with backpacks and a handcart setting out along an empty road, a hand-drawn map held open` |
| `evt_trip_night.png` | 수학여행의 밤 | `Inside an abandoned mountain lodge at night, sleeping bags in a circle around a lantern, a window showing a huge starry sky` |
| `evt_trip_return.png` | 돌아오는 길 | `Late afternoon, the school building seen from a distant road as a small group returns, long shadows, a figure waving from the rooftop` |

### 4.6 새 시스템 장면 (P3)

| 파일 | 이벤트 | 프롬프트 |
|---|---|---|
| `evt_sys_first_injury.png` | 첫 부상 | `A teacher's hands carefully wrapping a clean bandage around a girl's forearm, a first-aid tin open on the floor, no wound visible, quiet concentration` |
| `evt_sys_raw_water.png` | 첫 빗물 | `A homemade water filter made from a cut plastic bottle with layers of cloth, charcoal and sand, dripping into a pot on a camping stove, classroom window light` |
| `evt_sys_first_dish.png` | 첫 요리 | `A dented pot of simple rice porridge on a camping stove in a school home economics room, two mismatched bowls, steam, proud small mess` |
| `evt_sys_first_music.png` | 첫 음악 수업 | `A dusty school music room, an upright piano with the lid open, a teacher's hand pressing one key softly, afternoon light` |
| `evt_sys_room_homeec.png` | 가사실 발견 | `A school home economics room, rows of cooking stations, hanging pots, a rice cooker, cupboards half open, dust and light, no people` |
| `evt_sys_room_computer.png` | 컴퓨터실 발견 | `A dark school computer lab, rows of flat monitors all off, cables on the floor, one chair pulled out, light from the corridor, no people` |
| `evt_sys_room_music.png` | 음악실 발견 | `A school music room, music stands fallen like dominoes, an instrument cabinet, composer portraits on the wall slightly crooked, no people` |
| `evt_lost_found.png` | 분실물 상자 | `A cardboard lost-and-found box in a school corridor filled with school backpacks in different colors, one with a keychain, dust, soft light, no people` |

---

## 5. 엔딩 일러스트 빠진 6종 (P2, 16:9)

엔딩 22종 중 16종을 받았다. 남은 6종은 **프롬프트가 이미 [ART_PROMPTS.md](ART_PROMPTS.md) 6.2에 있다**(엔딩 본문과 맞춰 쓴 것). 새로 쓰지 않고 그대로 만든다. 폴더 `ArtInbox/illustrations/`, 파일 이름은 엔딩 ID 그대로.

| 파일 | 엔딩 | 장면 (프롬프트는 ART_PROMPTS 6.2) |
|---|---|---|
| `ENDING_HIDDEN_CURE.png` | 마지막 수업 이후 | 헬기 안, 나란히 앉은 세 사람의 뒷모습, 사이에 놓인 목발, 창밖으로 작아지는 학교 |
| `ENDING_HIDDEN_STAY.png` | 구조를 거부한다 | 떠나는 헬기, 옥상 가장자리에 앉아 지켜보는 두 사람 |
| `ENDING_HIDDEN_TRUTH.png` | 진실 | 조각을 이은 노트와 도표, 페이지를 넘기는 조사관의 손 |
| `ENDING_NARR_CONFRONT.png` | 마주한 얼굴 | 빈 운동장에서 거리를 두고 마주 선 두 아이, 멀리 다른 방향의 헬기 두 대 |
| `ENDING_EDUCATOR.png` | 교단에 선 사람 | 천막 임시 학교, 출석부를 든 젊은 선생님과 아이 일곱 |
| `ENDING_TOGETHER.png` | 함께한 사람들 | 해 뜨는 옥상, 어깨를 겯은 사람들의 뒷모습 |

---

## 6. UI 개편용 에셋

### 6.1 수첩·종이 (P1, 결산 화면과 생활기록부 서랍)

투명 배경, 정면, 그림자 없음(그림자는 게임에서 넣는다). 모델 `gpt_image_2_6`. 폴더 `ArtInbox/ui/`.
공통 앞부분: `Flat top-down scan of a single object on a transparent background, realistic paper texture, soft even light, no shadow, no text, no logo.`

| 파일 | 쓰임 | 프롬프트 |
|---|---|---|
| `ui_paper_diary_spread.png` | 일기 펼침면 (16:10) | `an open Korean school diary notebook spread, two blank lined pages, slightly yellowed paper, a ribbon bookmark, worn cloth cover edges visible around the pages` |
| `ui_paper_record.png` | 생활기록부 양식 (3:4) | `a blank Korean school student record form on cream paper, empty table grid with thin printed lines, a square frame in the upper right for an ID photo, punch holes on the left edge` |
| `ui_paper_timetable.png` | 주간 시간표 양식 (16:9) | `a blank mimeographed weekly timetable form, seven columns and two rows of empty boxes, slightly uneven purple-blue ink lines, off-white paper` |
| `ui_paper_ledger.png` | 물자 대장 (3:4) | `a blank ruled ledger page with narrow columns, thin red and blue lines, a faint coffee ring stain in one corner` |
| `ui_paper_note_yellow.png` / `_pink.png` / `_blue.png` | 경고 쪽지 3색 (1:1) | `a square sticky note in pale yellow, slightly curled bottom corner, blank` (색만 바꿔 3장) |
| `ui_paper_polaroid.png` | 이달의 사진 틀 (4:5) | `an empty instant photo frame, white border thicker at the bottom, the photo area a flat mid-grey, slightly faded edges` |
| `ui_paper_envelope.png` | 출정 준비 봉투 (16:9) | `a worn manila document envelope, blank label area, a string closure, creased` |
| `ui_paper_tab_{1..5}.png` | 색인 탭 5색 | `a small colored paper index tab sticker, blank, slightly worn` (빨강·주황·초록·파랑·보라) |
| `ui_paper_clip.png`, `ui_paper_tape.png`, `ui_paper_pin.png` | 클립·테이프·압정 | `a single metal paper clip` / `a short strip of translucent masking tape` / `a red push pin seen from above` |
| `ui_desk_wood.png` | 책상 바탕 (16:9, 배경 있음) | `top-down view of a worn wooden teacher's desk surface, scratches and ink stains, even light, nothing on it` |

### 6.2 도장·손 글씨 표시 (P1)

투명 배경, 한 가지 색. 모델 `recraft_v4_1`(vector) 또는 `gpt_image_2_6`.
공통: `A single hand-made mark on a transparent background, slightly uneven ink, no text unless stated.`

| 파일 | 쓰임 | 프롬프트 |
|---|---|---|
| `ui_stamp_great.png` | 대성공 | `a round red rubber stamp impression with a five-petal flower shape in the center` |
| `ui_stamp_good.png` | 성공 | `a round blue rubber stamp impression with a simple check mark` |
| `ui_stamp_fail.png` | 실패 | `a grey rubber stamp impression with a simple X` |
| `ui_mark_circle_red.png` | 선생님 빨간 동그라미 | `a hand-drawn circle in red colored pencil, slightly open at the end` |
| `ui_mark_underline_red.png` | 빨간 밑줄 | `a hand-drawn wavy underline in red colored pencil` |
| `ui_mark_check.png`, `ui_mark_cross.png` | 한 일 표시 | `a quick pen check mark in dark blue ink` / `a quick pen cross in dark blue ink` |
| `ui_mark_arrow.png` | 강제 휴식 잇는 화살표 | `a short hand-drawn curved arrow in pencil` |
| `ui_mark_hatch.png` | 잠긴 칸 빗금 (반복 무늬) | `diagonal pencil hatching lines, seamless tileable pattern` |
| `ui_stamp_week_done.png` | 한 주 끝 도장 | `a rectangular red date stamp impression with empty space where the date would be` |

### 6.3 행동 아이콘 (P1, 약 40개)

주간 도크의 14칸에 찍힌다. 지금은 분류 아이콘 5개뿐이다. 64px에서도 알아볼 수 있는 단순한 실루엣. 모델 `recraft_v4_1`(vector), 투명 배경, 1:1.
공통: `Simple flat game icon, single object, thick clean shapes, muted warm palette with one accent color, readable at 64 pixels, transparent background, no text.` 파일 `icon_act_{이름}.png`.

| 분류 | 파일 이름 → 그릴 것 |
|---|---|
| 교육 | `humanities` 펼친 책과 연필 · `medicine` 구급상자와 책 · `selfdefense` 샌드백 · `survival` 매듭과 나침반 · `engineering` 스패너와 톱니 · `cooking` 냄비와 국자 · `mind` 방석 위 촛불 · `science` 삼각 플라스크 · `music` 음표와 건반 |
| 생존 | `purify_water` 페트병 정수기 · `barricade` 쌓은 책상 · `trap` 줄과 깡통 · `alarm` 줄에 매단 깡통 · `reinforce` 못 박은 문 · `covers` 가린 창문 · `rain_barrel` 빗물통 · `garden` 화분 새싹 · `clean` 양동이와 걸레 · `crutch` 목발 · `power` 발전기 · `prosthetic` 간이 의족 |
| 치료·돌봄 | `treat_wound` 붕대 감은 팔 · `treat_infection` 소독약과 거즈 · `care_teacher` 담요와 컵 |
| 휴식 | `rest_day` 책상에 엎드린 모습 · `rest_night` 침낭과 달 |
| 일상·밤 | `diary` 일기장과 펜 · `talk` 말풍선 두 개 · `watch` 손전등과 창문 · `night_work` 랜턴과 망치 · `night_study` 랜턴과 책 · `rooftop` 옥상 난간과 구름 · `radio` 무전기 · `tutor` 작은 칠판 |
| 외출 | `exp_school` 학교 건물 · `exp_store` 편의점 차양 · `exp_pharmacy` 초록 십자 · `exp_residential` 빌라 · `exp_subway` 지하철 입구 · `exp_mart` 쇼핑 카트 · `exp_police` 경광등 · `exp_waterworks` 물탱크 · `outing` 돗자리와 도시락 · `trip` 배낭과 지도 |
| 특수 | `confined` 잠긴 문 · `forced_rest` 붕대 감긴 침대 · `intrusion` 느낌표와 문 |

- 낮·밤 탐험은 같은 아이콘에 게임에서 해·달 표시를 덧붙인다.

### 6.4 그 밖의 UI 아이콘 (P2)

같은 공통 문구, 파일 `icon_{이름}.png`.

| 묶음 | 그릴 것 |
|---|---|
| 빠진 숙련도 2 | `prof_science` 플라스크 · `prof_music` 음표 |
| 빠진 상태 | `vital_cleanliness` 비누 거품 · `status_injured` 붕대 · `status_infected` 체온계 · `status_confined` 자물쇠 |
| 세력 3 | `faction_sageori` 붉은 스프레이 표시(추상 기호) · `faction_mart` 쇼핑 카트와 상자 · `faction_ubc` 방송 탑 |
| 인물 (없는 것) | `npc_teacher` 안경 · `npc_banjang` 작업 조끼 · `npc_gangcheol` 야구 모자 · `npc_dj` 헤드폰 |
| 서랍·메뉴 | `menu_record` 생활기록부 · `menu_school` 학교 건물 단면 · `menu_people` 사람 둘 · `menu_diary` 일기장 · `menu_plan` 시간표 · `menu_settings` 톱니 |
| 날씨 추가 | `weather_snow` · `weather_storm` · `weather_fog` |
| 실행 조작 | `speed_1` · `speed_2` · `speed_4` · `skip` · `pause` |

### 6.5 설정·주제 안내 화면 (P2, M8)

| 파일 | 쓰임 | 프롬프트 |
|---|---|---|
| `bg_notice.png` (16:9) | 첫 실행 주제 안내 배경 | `[F-ILLU] An empty classroom at dusk seen from the back row, one desk with a folded note on it, very quiet, heavily blurred and darkened so text can sit on top, no people` |
| `bg_settings.png` (16:9) | 설정 화면 배경 | `[F-ILLU] Close view of a school broadcasting room mixing console with sliders and knobs, dim warm lamp, shallow depth of field, darkened for text overlay` |
| `bg_credits.png` (16:9) | 크레딧 | `[F-ILLU] A school hallway bulletin board covered with pinned photos and notes (all illegible), spring sunlight, cherry petals on the floor, no people` |

---

## 7. 허브 장면 추가분

허브가 건물 단면도(픽셀 아트)로 바뀌면 방 그림은 PixelLab으로 내가 만든다. 여기에는 **일러스트로 필요한 것**만 적는다. 16:9, 인물 없음, `[F-ILLU]` + `[안전]`.

| 우선 | 파일 | 쓰임 | 프롬프트 |
|---|---|---|---|
| P2 | `bg_storage_night.png` | 창고 거처의 밤 (결산 화면 뒤) | `The same cramped school storage room shelter at night, a single camping lamp lit on the floor beside a sleeping bag, the high window dark blue` |
| P2 | `bg_classroom_night.png` | 교실 거처의 밤 | `The same classroom living space at night, curtains drawn, one small lamp on the pushed-together desks, long shadows` |
| P2 | `bg_homeec.png` | 가사실 (요리) | `School home economics room used as a kitchen, a camping stove on a cooking station, pots, sorted cans, a hand-written menu board without readable text` |
| P2 | `bg_music.png` | 음악실 | `School music room, an upright piano with a blanket over it, music stands, soundproof holes in the wall panels, afternoon light` |
| P2 | `bg_computer.png` | 컴퓨터실 | `School computer lab, one working monitor glowing among dark ones, cables running to a car battery, notes taped beside the screen` |
| P3 | `bg_school_section.png` | 단면도 참고용 원안 (그대로 쓰지 않고 구도 참고) | `Cutaway side view of a four-story Korean high school building like a dollhouse, each room visible: storage room, classrooms, nurse office, science lab, music room, rooftop with rain barrels and a small garden, stairwells at both ends, tiny figures, evening, some windows lit` |
| P3 | `bg_gate_{spring,summer,autumn,winter}.png` | 교문 4계절 (외출 화면) | `A Korean high school front gate seen from inside the grounds looking out, [계절 문구], empty road beyond, no people` |

---

## 8. 체크리스트

- [ ] **시험분 6장** (1.2) → 겹쳐 보고 방식 확정
- [ ] 입상: 옷 6, 머리 2, 표정 8, 상태 6, 든 것 8 (기준 포함 31장)
- [ ] 선생님 입상 4 (선택)
- [ ] 새 인물: 시트 5 (강철·DJ·민규·준·오 씨), 표정 약 18 (친구 4 포함), 한 장짜리 2 (백·구조대원)
- [ ] 이벤트 컷: 1막 10, 2막 12, 3막 12, 4막 13, 소풍·여행 7, 새 시스템 8 (62장 중 P2 54)
- [ ] 엔딩 6 (프롬프트는 ART_PROMPTS 6.2)
- [ ] UI: 종이 약 17, 도장·표시 10, 행동 아이콘 약 45, 그 밖의 아이콘 약 27, 배경 3
- [ ] 허브 장면 5 (+ 참고용 1, 교문 4)

## 9. 받은 뒤 내가 하는 일

> **연결 (2026-11-21)**: 214장 받음(일러스트 105, UI 105, 선생님 입상 4). 무엇을 어디에 연결했는지는 [UI_REDESIGN_PROPOSAL.md](UI_REDESIGN_PROPOSAL.md) 10절 "힉스필드 2차 에셋 연결". 가져오기 도구는 `code_temp/apogirl/tools/art_import2.py`, 표는 `scratch/art_map2.py`. 1장(주인공 입상 조각)은 아직 받지 않았다.

1. 입상: 기준 그림과 차이 나는 부분을 오려 조각으로 만들고, 위치를 맞춰 겹친다. 맞지 않는 조각은 목록으로 알려 다시 요청한다.
2. 가방은 회색을 여섯 색으로 물들인다.
3. 인물 표정은 배경을 지우고 `art_map.json`의 `Speakers`에 넣는다.
4. 이벤트 컷·엔딩은 `art_map.json`의 `Events`와 엔딩 표에 연결한다.
5. 아이콘은 크기를 맞춰 `Resources/Art/UI`에 넣고 `Icons`에 키를 더한다.
