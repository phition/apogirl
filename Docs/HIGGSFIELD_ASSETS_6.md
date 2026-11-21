# 힉스필드 에셋 6차: 프롤로그 등굣길 애니메이션 (2026-11-22)

새 게임을 시작하면 프롤로그 대화 첫 줄 "3월 둘째 주, 월요일 아침." 앞에 **30초짜리 등굣길 애니메이션**을 한 번 튼다.

- 주인공이 혼자 집에서 학교까지 걸어간다. 길에는 앞으로 게임의 배경이 될 마을 곳곳이 나온다: 빌라 골목, 편의점, 사거리, 약국, 버스 정류장, 마트, 아파트 언덕길, 교문.
- **앞쪽(주인공)**: 2D 셀 애니메이션이다. 익살스럽고, 여고생다운 발랄함이 있다.
- **뒤쪽(배경)**: 군데군데 무슨 일이 벌어지고 있다. 주인공은 이어폰을 끼고 있어 전혀 알아채지 못한다.
- **마지막 컷**: 주인공이 이어폰 한쪽을 빼고 뒤를 돌아본다. 화면 밖에서 누가 부르는 것처럼 끝나고, 프롤로그 대본의 "뒤에서 누군가 달려오는 발소리가 들린다 → 친구: 같이 가!"로 바로 이어진다.

> **이 영상만 그림체가 다르다.** 게임 본편은 차분한 비주얼노벨 일러스트에 정밀한 배경이다. 이 영상은 일부러 밝고 통통 튀는 셀 애니메이션으로 만든다. 평범한 마지막 아침이 가볍고 귀여울수록, 그 뒤에 오는 창고 장면과의 낙차가 커진다. 인물의 생김새(머리·교복)는 본편 기준 시트와 맞춘다.

---

## 0. 쓰는 법

- **회색 상자 하나 = 생성 하나.** 통째로 복사해 claude.ai 채팅(Higgsfield 커넥터)에 붙여 넣는다. 쓰는 법은 [HIGGSFIELD_ASSETS.md](HIGGSFIELD_ASSETS.md) 0.1과 같다.
- 결과는 **상자에 적힌 파일 이름 그대로** 넣는다.
  - 열쇠 그림: `ArtInbox/illustrations/`
  - 영상: `ArtInbox/video/`
  - 음악: `ArtInbox/music/`
- 순서는 셋이다.
  1. **열쇠 그림 3장**(1장)을 먼저 만든다. 셀 애니메이션 그림체와 주인공 얼굴을 여기서 고정한다.
  2. 그 그림을 시작 이미지로 넣어 **10초 영상 3개**(2장)를 만든다.
  3. 영상에 깔 **음악**(4장)을 만든다.
- 30초를 한 번에 만드는 판(3장)도 적어 두었다. 다만 10초 3개가 낫다. 마음에 안 드는 구간만 다시 만들 수 있고, 한 영상 안에 컷이 적어야 인물이 덜 흔들린다.

### 0.1 모델·설정

| 무엇 | 모델 | 설정 |
|---|---|---|
| 열쇠 그림 | `nano_banana_flash` (Nano Banana 2, 만화·애니 인물에 강함). 안 맞으면 `gpt_image_2_6` | 16:9, 참고 이미지 `char_seojin_sheet.png` |
| 영상 | `seedance_2_5` | `mode: omni_reference`, 시작 이미지 = 열쇠 그림, 16:9, **1080p**, 10초, `generate_audio` 끔 |
| 음악 | `sonilo_music` | `duration: 30` |

- 크레딧은 4차 기준으로 1080p가 초당 약 16이다. 10초 × 3 ≈ 480크레딧이고, 720p면 약 210이다. 오프닝은 전체 화면이라 1080p를 권한다.
- 소리는 영상에서 끄고 음악·효과음을 따로 깐다(1차 문서 6장과 같다).

### 0.2 공통 문구 (상자 안에 이미 들어 있음)

`[셀 애니]` — 이 영상 전용 그림체
```
Style: 2D hand-drawn TV anime cel animation, bold clean outlines, flat cel shading with a single shadow tone, bright soft spring palette (pale sky blue, cream, cherry pink, fresh green), simple painted backgrounds in flat gouache style, snappy comedic timing, squash and stretch, playful smear frames on quick moves, brief exaggerated cartoon reactions (sweat drop, sparkle eyes) for comic beats, lively and funny, everyday slice-of-life opening sequence. No 3D look, no photorealism.
```

`[주인공]` — 기준 시트와 같은 사람
```
The main character: a 16-year-old Korean high school girl, shoulder-length straight black hair with bangs, navy school blazer over a white shirt and a striped tie, navy plaid pleated skirt to the knee, black tights, white sneakers, a school backpack, white wired earbuds in both ears, bright cheerful face. Same face, hair and uniform in every shot as in the reference image.
```

`[배경 사건]` — 뒤쪽 일의 수위
```
Background incidents are small and easy to miss: they happen in the far background, in muted slightly desaturated colors and slower timing, while the girl in the foreground stays bright and oblivious, never looking at them. Distant people are small simple figures; anyone who looks unwell is only a far-away hunched silhouette with no visible face. Uneasy but not scary, nothing graphic.
```

`[한국 마을]`
```
Setting: a small provincial Korean city on an early spring morning in the second week of March, cold clear sunshine, bare trees with the first buds, red-brick four-story villas with piloti parking, tangled overhead wires on utility poles, green-and-white convenience store awnings, a pharmacy green cross sign, glass bus shelter, high-rise apartment towers on the hills. All signboards are blank or unreadable, no readable text anywhere.
```

`[안전]` — 1차 문서 0.4
```
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

### 0.3 30초 전체 흐름

| 시간 | 장소 | 주인공 (앞, 익살) | 배경 (뒤, 무슨 일이) |
|---|---|---|---|
| 0~3초 | 빌라 1층 필로티 | 운동화를 구겨 신으며 한 발로 깡충깡충 뛰어나온다. 이어폰을 꽂고 고개를 까딱인다 | 위층 창문 커튼이 홱 닫힌다. 멀리서 사이렌이 아주 작게 들린다 |
| 3~6초 | 빌라 골목 | 담 위 고양이에게 손을 흔든다. 고양이는 무시한다. 주인공은 시무룩해진다(땀 한 방울) | 고양이는 털을 세우고 골목 끝 화면 밖만 노려본다 |
| 6~10초 | 편의점 앞 | 삼각김밥 포장을 뜯다가 김이 다 찢어진다. 그래도 맛있게 문다 | 유리 너머 점원이 벽의 TV를 넋 놓고 본다. TV에는 빨간 속보 띠(글자 없음) |
| 10~13초 | 사거리 횡단보도 | 빨간불 동안 노래에 맞춰 작은 춤 동작을 한다 | 건너편 사람들은 모두 휴대폰만 보며 굳어 있다. 한 명이 커피를 떨어뜨린다 |
| 13~16초 | 약국 모퉁이 | 꽃눈 달린 가지 앞에서 브이 셀카. 사진을 보며 앞머리만 신경 쓴다 | 셀카 뒤로 구급차가 경광등을 켜고 쌩 지나간다. 약국 앞에는 마스크 쓴 사람들이 줄을 섰다 |
| 16~20초 | 버스 정류장 | 버스가 안 서고 지나간다. 어깨를 으쓱하고 "걷지 뭐", 물웅덩이를 폴짝 넘는다 | 꽉 찬 버스 창에 사람들이 눌려 모두 뒤쪽을 돌아보고 있다 |
| 20~23초 | 마트 앞 | 마트 유리문에 비친 자기 모습을 보며 앞머리를 정리한다 | 사람들이 생수·라면을 가득 실은 카트를 밀고 뛰어나온다. 유리문 반사 속 멀리 주차장에서 구부정한 실루엣이 비틀비틀 나온다 |
| 23~27초 | 아파트 언덕길 | 날아오르는 까마귀 떼를 따라 팔을 파닥이며 혼자 웃는다 | 까마귀가 한꺼번에 날아오른다. 높은 하늘에 헬기 한 대가 지나간다. 아파트 몇 집 베란다에 사람이 나와 같은 쪽을 본다 |
| 27~30초 | 교문이 보이는 길 | 걸음을 멈추고 이어폰 한쪽을 빼며 뒤(화면 쪽)를 돌아본다. 화면 가장자리에서 손 하나가 흔들린다 | 학교는 평온하다. 마지막 한 박자만 조용하다 |

- 배경 사건은 **한 컷에 하나**만 둔다. 많아지면 섬뜩한 영상이 되어 버린다.
- 마트(더불어 마트), 사거리(사거리 파), 약국, 편의점, 버스 정류장은 모두 본편에서 다시 가는 곳이다. 2막부터 플레이어가 "아, 그때 거기"라고 알아보는 장치다.

---

## 1. 열쇠 그림 3장 (먼저)

영상 세 구간의 첫 장면이다. 참고 이미지로 `char_seojin_sheet.png`를 넣는다(얼굴·머리·교복을 맞춘다).

`ArtInbox/illustrations/key_commute_01.png` — 1구간 첫 장면 (빌라 필로티)
```
16:9 anime key frame. A cheerful 16-year-old Korean schoolgirl bursting out of the piloti parking space under a red-brick villa, hopping on one foot while pulling on a white sneaker, mouth open in a little "oops I'm late" expression, earbuds in, backpack bouncing. Parked compact car and bicycles behind her, morning sun. Far up on the third floor, a curtain is being yanked shut by an unseen hand.
[셀 애니]
[주인공]
[배경 사건]
[한국 마을]
[안전]
```

`ArtInbox/illustrations/key_commute_02.png` — 2구간 첫 장면 (사거리 횡단보도)
```
16:9 anime key frame. A crossroads in a small Korean city. In the foreground the same schoolgirl waits at a red pedestrian light, doing a tiny silly dance move to her music with eyes closed and a happy smile. Across the street, a row of small simple adult figures all frozen staring at their phones; one has just dropped a paper coffee cup. Utility poles, tangled wires, blank signboards.
[셀 애니]
[주인공]
[배경 사건]
[한국 마을]
[안전]
```

`ArtInbox/illustrations/key_commute_03.png` — 3구간 첫 장면 (마트 앞)
```
16:9 anime key frame. In front of a large neighborhood supermarket with a blank signboard. In the foreground the same schoolgirl checks her reflection in the glass entrance door and fixes her bangs with a serious face. Behind her, a few small simple figures hurry out pushing shopping carts piled high with bottled water and instant noodle boxes. In the glass reflection, very far away in the parking lot, one tiny hunched silhouette stands still.
[셀 애니]
[주인공]
[배경 사건]
[한국 마을]
[안전]
```

- 셋을 나란히 놓았을 때 얼굴·교복·색감이 같아야 한다. 다르면 가장 마음에 드는 한 장을 참고 이미지로 더 넣어 나머지를 다시 만든다.
- `[주인공]`에 따라 이어폰은 처음부터 끝까지 양쪽 귀에 있다. 마지막 컷에서만 한쪽을 뺀다.

---

## 2. 영상 3개 (10초씩)

시작 이미지 = 1장의 같은 번호 열쇠 그림. 세 영상을 이어 붙이면 30초가 된다. 이음새는 장면이 바뀌는 곳이라 따로 맞추지 않아도 된다.

`ArtInbox/video/vid_prologue_commute_01.mp4` — 0~10초: 빌라 → 골목 → 편의점
```
10-second 2D anime cel animation, three quick shots, 16:9, start from the reference image.
[0-3s] The schoolgirl hops out from under the red-brick villa pulling on her sneaker, nearly trips, catches herself with a comic wobble, then bobs her head to the music in her earbuds and sets off. High above, a curtain in a third-floor window snaps shut.
[3-6s] A narrow alley between villas. She waves happily at a stray cat on top of a wall; the cat completely ignores her, its fur standing up as it stares hard at the far end of the alley off-screen. She pouts, a cartoon sweat drop appears, and she walks on.
[6-10s] She steps out of a convenience store tearing open a triangle kimbap; the seaweed rips into pieces, she shrugs and takes a big happy bite. Through the store window behind her, the clerk stands frozen watching a wall TV showing a red breaking-news band with no readable text.
Tracking camera moving alongside her at walking pace, smooth cuts between shots.
[셀 애니]
[주인공]
[배경 사건]
[한국 마을]
[안전]
```

`ArtInbox/video/vid_prologue_commute_02.mp4` — 10~20초: 사거리 → 약국 → 버스 정류장
```
10-second 2D anime cel animation, three quick shots, 16:9, start from the reference image.
[0-3s] At the crossroads red light she does a small silly dance move to her music, eyes closed. Across the street the small figures stay frozen staring at their phones; one drops a coffee cup that splashes on the pavement. The light turns green and she skips across.
[3-6s] At a pharmacy corner with a green cross sign she takes a peace-sign selfie with a budding branch. Behind her an ambulance with flashing lights speeds past and a short line of people in face masks waits at the pharmacy door. She checks the photo, frowns only at her bangs, and fixes them.
[6-10s] At a glass bus shelter a packed city bus rolls past without stopping; the passengers pressed against the windows are all looking backward down the road. She shrugs with a "whatever, I'll walk" face and hops over a puddle with a big cartoon leap.
Tracking camera moving alongside her at walking pace, smooth cuts between shots.
[셀 애니]
[주인공]
[배경 사건]
[한국 마을]
[안전]
```

`ArtInbox/video/vid_prologue_commute_03.mp4` — 20~30초: 마트 → 언덕길 → 교문
```
10-second 2D anime cel animation, three quick shots, 16:9, start from the reference image.
[0-3s] In front of the supermarket she fixes her bangs in the glass door reflection with a serious face while people hurry out behind her pushing carts piled with bottled water and instant noodles. In the reflection, very far away, a tiny hunched silhouette staggers slowly out of the parking garage.
[3-7s] An uphill road lined with high-rise apartment towers. A flock of crows bursts up from a tree all at once; she flaps her arms like a bird and laughs to herself. High in the sky a helicopter crosses; on a few apartment balconies small figures step out and look the same way.
[7-10s] The school gate appears at the top of the hill under budding cherry trees, calm and quiet. She stops, pulls out one earbud and turns her head back toward the camera as if someone just called her name; at the edge of the frame a hand waves. Hold on her curious face for the last second.
Tracking camera moving alongside her at walking pace, smooth cuts between shots, the final shot holds still.
[셀 애니]
[주인공]
[배경 사건]
[한국 마을]
[안전]
```

다시 만들어야 하는 경우:
- 배경 사건이 앞으로 나와 주인공이 그쪽을 본다.
- 실루엣에 얼굴이나 상처가 보인다.
- 사람이 쓰러지거나 공격당하는 장면이 생긴다.
- 간판에 엉터리 글자가 크게 보인다.
- 주인공의 머리 길이나 교복이 컷마다 바뀐다.

---

## 3. (다른 길) 30초 한 번에

10초 3개가 마음에 들지 않을 때만 쓴다. 시작 이미지 = `key_commute_01.png`, 길이 30초.

`ArtInbox/video/vid_prologue_commute_full.mp4`
```
30-second 2D anime cel animation opening sequence, 16:9, start from the reference image. A cheerful Korean schoolgirl walks to school alone through her small town on an ordinary spring morning, earbuds in, completely oblivious, while small strange things happen in the far background.
[0-3s] She hops out from under a red-brick villa pulling on her sneaker; a third-floor curtain snaps shut above.
[3-6s] She waves at a stray cat on a wall; it ignores her and stares fur-up at the end of the alley. She pouts.
[6-10s] Outside a convenience store her triangle kimbap wrapper rips; she bites it happily. Inside, the clerk is frozen watching a TV with a red breaking-news band, no readable text.
[10-13s] At a crossroads red light she does a silly little dance; across the street everyone stares at their phones, one drops a coffee.
[13-16s] Peace-sign selfie at a pharmacy corner; an ambulance with flashing lights speeds by behind her, people in masks line up at the pharmacy. She only fixes her bangs.
[16-20s] A packed bus passes without stopping, passengers all looking backward; she shrugs and leaps over a puddle.
[20-23s] She fixes her bangs in a supermarket's glass door while people rush out with carts full of water and noodles; far away in the reflection a tiny hunched silhouette staggers.
[23-27s] Crows burst from a tree on the apartment hill road; she flaps her arms and laughs; a helicopter crosses the sky.
[27-30s] The calm school gate under budding cherry trees; she stops, takes out one earbud and turns back toward the camera as someone calls her; a hand waves at the frame edge. Hold.
Tracking camera at walking pace, clean cuts.
[셀 애니]
[주인공]
[배경 사건]
[한국 마을]
[안전]
```

---

## 4. 음악 (30초)

`ArtInbox/music/bgm_prologue_commute.mp3` — `sonilo_music`, `duration: 30`
```
30-second bright bouncy Korean indie pop instrumental for an anime school-commute opening: plucky ukulele, glockenspiel, light claps and a skipping bass line, playful and a little goofy. In the last 4 seconds the music suddenly drops out to a single soft held note and silence, as if one earbud was pulled out. No vocals.
```

- 음악은 마지막 컷(이어폰을 빼는 순간)에서 끊겨야 한다. 영상 길이가 29~31초로 달라지면 게임에서 맞춰 자른다.
- 이어폰 밖 소리(사이렌·까마귀·버스)는 음악보다 훨씬 작게, 먼 소리로만 깐다. 효과음은 기존 `Sfx/`에 있는 것을 우선 쓰고, 모자라면 그때 따로 만든다.

---

## 5. 게임 연결 (받은 뒤에 할 일)

- **재생 위치**: 새 게임 → 프롤로그 대본 `PROLOGUE_START` 첫 줄 바로 앞. 아무 키나 누르면 건너뛴다. 2회차부터는 처음부터 건너뛰기 버튼을 보인다.
- **파일**: 세 영상을 하나로 이어 붙이고 음악을 입혀 `Resources`가 아닌 `StreamingAssets`에 둔다. 안드로이드 빌드 크기를 생각해 30초 1080p H.264, 15MB 안팎을 목표로 한다.
- **설계**: 연결할 때 재생 방식(VideoPlayer, 건너뛰기, 2회차 처리)을 따로 제안한다. 이 문서는 프롬프트만 다룬다.
