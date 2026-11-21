# 힉스필드 에셋 4차: 상태에 반응하는 입상 영상

허브 입상이 지금 몸 상태(피로·스트레스·부상·감염 등)에 맞게 움직이도록, **그 상태일 때 게임이 보여 주는 전신 그림**을 시작·끝 프레임으로 한 짧은 영상을 만든다.
3차 샘플(`motion_base_idle`·`look`)을 넣어 보니 영상은 그 영상의 시작 그림이 화면에 있을 때만 이어 붙일 수 있었다. 기본 그림(`stand_base`)은 봄 1~13주·보통 얼굴일 때만 보이므로, 상태 그림마다 영상을 두면 **그 상태가 될 때 바로 그 영상이 나온다**.

- 모두 12개 (P1 7개 먼저, P2 나머지). 영상 하나 4초.
- **720p로 만든다**: 게임은 340×512로 줄여 쓰므로 1080p가 필요 없다. 720p 4초 = 28크레딧(1080p는 64), P1 7개 ≈ 196크레딧, 전체 ≈ 336크레딧.
- Seedance 2.5는 4초보다 짧게 만들지 않는다(3초로 하면 4초로 늘어난다).

---

## 0. 쓰는 법

- [HIGGSFIELD_ASSETS_3.md](HIGGSFIELD_ASSETS_3.md) 0.1과 같다. **회색 상자 하나가 영상 하나**, 통째로 복사해 claude.ai 채팅(Higgsfield 커넥터)에 붙여 넣는다. 여러 상자를 한 번에 붙여도 된다.
- 결과를 **상자에 적힌 파일 이름 그대로** `ArtInbox/standing_motion/`에 넣는다.
- 시작·끝 프레임은 2차 입상의 작업 ID로 넘긴다(3차 문서 0.2 표). 채팅이 ID를 못 쓰면 "이미지 보기" 주소를 넘긴다.
- 좋은 영상의 조건 (3차 샘플에서 확인): 카메라 고정, 배경이 처음부터 끝까지 같은 회색, 첫 프레임과 끝 프레임이 거의 같음, 인물이 화면 안에서 움직이지 않음. **몸이 크게 돌거나 손이 화면 밖으로 나가면** 다시 만든다.

### 0.1 공통 문구 (상자 안에 이미 들어 있음)

`[영상 고정]`
```
Locked-off static camera, no camera movement, no zoom, no cut. The plain light grey background stays completely unchanged and empty. The girl stays in the same spot and the same size in the frame for the whole clip; her outfit, hair and face stay consistent with the first frame. The clip ends in exactly the same pose as it starts so it can loop seamlessly. Gentle, small, natural motion; 2D anime illustration style matching the reference, no 3D look.
```
`[안전]` (부상·감염 장면도 피·상처는 보이지 않게, 붕대는 깨끗한 흰색)
```
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

### 0.2 게임에서 언제 나오나

| 상태 그림 | 동작 | 길이 | 우선 | 게임에서 |
|---|---|---|---|---|
| `face_tired` | `idle` | 4초 | P1 | 지친 얼굴일 때 늘 (기본 반복) |
| `face_tired` | `yawn` | 4초 | P1 | 지친 얼굴일 때 15~30초마다 |
| `face_sad` | `idle` | 4초 | P1 | 슬픈 얼굴일 때 늘 |
| `face_sad` | `calm` | 4초 | P2 | 슬픈 얼굴일 때 20~40초마다 (스스로 다독이기) |
| `face_blank` | `idle` | 4초 | P1 | 멍한 얼굴일 때 늘 |
| `face_blank` | `flinch` | 4초 | P2 | 멍한 얼굴일 때 가끔 (작은 소리에 놀람) |
| `face_worried` | `idle` | 4초 | P1 | 걱정하는 얼굴일 때 늘 |
| `face_smile` | `idle` | 4초 | P2 | 웃는 얼굴일 때 늘 |
| `mark_bandage_arm` | `idle` | 4초 | P1 | 팔 붕대일 때 늘 |
| `mark_mask` | `idle` | 4초 | P1 | 마스크를 썼을 때 늘 |
| `mark_thin` | `idle` | 4초 | P2 | 굶주렸을 때 늘 |
| `mark_dirty` | `idle` | 4초 | P2 | 꼬질꼬질할 때 늘 |

- 그림을 고르는 순서는 지금과 같다(`standing_map.json` `WholePriority`: 상태 표시 → 옷 → 잠깐 표정 → 표정 → 머리). 그 그림에 `idle` 영상이 있으면 반복하고, 다른 동작은 이따금 한 번 끼운다. 영상이 없는 그림은 3차의 눈 깜빡임·시선만.
- **상태 표시(붕대·마스크·마름·얼룩)는 계절·옷보다 먼저 골라지므로 1년 내내 나온다.** 표정 그림은 옷 다음이라 계절 옷이 없는 봄·가을(방어구 없음)에만 나온다. 여름·겨울·방어구 차림의 반응 영상은 이번 문서에 없다(필요하면 다음 차례).

---

## 1. 피로·졸림

`face_tired` 그림이 보일 때: 수면 부채 3 이상 또는 피로 70 이상.

#### `motion_face_tired_idle.mp4` (4초)
시작·끝 프레임: `stand_face_tired` — 작업 ID `17d378cf-75b8-4b73-818a-2a3d3c990aaf` · [이미지 보기](https://d8j0ntlcm91z4.cloudfront.net/user_3K3GDbFrhFIfl8ivGn5P16JoJBa/hf_20261120_165144_17d378cf-75b8-4b73-818a-2a3d3c990aaf.png)
```
Higgsfield로 영상을 만들어 줘.
- 모델: seedance_2_5, 모드 omni_reference, 720p, 세로 9:16, 길이 4초, 소리 없음
- 시작 프레임과 끝 프레임: 둘 다 이전 생성 작업 ID 17d378cf-75b8-4b73-818a-2a3d3c990aaf (stand_face_tired) — 처음과 끝이 같아야 반복 재생된다
- 결과 파일 이름: motion_face_tired_idle.mp4
- 프롬프트:
She stands wearily: slow heavy breathing, her shoulders slightly slumped, her eyelids droop and she blinks slowly once, her head nods forward a little as if fighting sleep, then lifts again. The reference image is both the first and the last frame.
Locked-off static camera, no camera movement, no zoom, no cut. The plain light grey background stays completely unchanged and empty. The girl stays in the same spot and the same size in the frame for the whole clip; her outfit, hair and face stay consistent with the first frame. The clip ends in exactly the same pose as it starts so it can loop seamlessly. Gentle, small, natural motion; 2D anime illustration style matching the reference, no 3D look.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

#### `motion_face_tired_yawn.mp4` (4초)
시작·끝 프레임: `stand_face_tired` — 작업 ID `17d378cf-75b8-4b73-818a-2a3d3c990aaf` · [이미지 보기](https://d8j0ntlcm91z4.cloudfront.net/user_3K3GDbFrhFIfl8ivGn5P16JoJBa/hf_20261120_165144_17d378cf-75b8-4b73-818a-2a3d3c990aaf.png)
```
Higgsfield로 영상을 만들어 줘.
- 모델: seedance_2_5, 모드 omni_reference, 720p, 세로 9:16, 길이 4초, 소리 없음
- 시작 프레임과 끝 프레임: 둘 다 이전 생성 작업 ID 17d378cf-75b8-4b73-818a-2a3d3c990aaf (stand_face_tired) — 처음과 끝이 같아야 반복 재생된다
- 결과 파일 이름: motion_face_tired_yawn.mp4
- 프롬프트:
She covers her mouth with her right hand and yawns quietly, eyes squeezed shut, then lowers the hand and rubs one eye briefly with the back of her fingers before returning to the starting pose. The reference image is both the first and the last frame.
Locked-off static camera, no camera movement, no zoom, no cut. The plain light grey background stays completely unchanged and empty. The girl stays in the same spot and the same size in the frame for the whole clip; her outfit, hair and face stay consistent with the first frame. The clip ends in exactly the same pose as it starts so it can loop seamlessly. Gentle, small, natural motion; 2D anime illustration style matching the reference, no 3D look.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

## 2. 스트레스

`face_sad`(스트레스 60 이상) · `face_blank`(한계의 90% 이상) 그림이 보일 때.

#### `motion_face_sad_idle.mp4` (4초)
시작·끝 프레임: `stand_face_sad` — 작업 ID `eb51241d-041b-4dd2-b29b-5a1a2f6b215d` · [이미지 보기](https://d8j0ntlcm91z4.cloudfront.net/user_3K3GDbFrhFIfl8ivGn5P16JoJBa/hf_20261120_210648_eb51241d-041b-4dd2-b29b-5a1a2f6b215d.png)
```
Higgsfield로 영상을 만들어 줘.
- 모델: seedance_2_5, 모드 omni_reference, 720p, 세로 9:16, 길이 4초, 소리 없음
- 시작 프레임과 끝 프레임: 둘 다 이전 생성 작업 ID eb51241d-041b-4dd2-b29b-5a1a2f6b215d (stand_face_sad) — 처음과 끝이 같아야 반복 재생된다
- 결과 파일 이름: motion_face_sad_idle.mp4
- 프롬프트:
She stands with her head slightly lowered, holding her left elbow with her right hand close to her body, breathing a little unevenly; she glances down and then back up. Subdued, quiet mood. The reference image is both the first and the last frame.
Locked-off static camera, no camera movement, no zoom, no cut. The plain light grey background stays completely unchanged and empty. The girl stays in the same spot and the same size in the frame for the whole clip; her outfit, hair and face stay consistent with the first frame. The clip ends in exactly the same pose as it starts so it can loop seamlessly. Gentle, small, natural motion; 2D anime illustration style matching the reference, no 3D look.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

#### `motion_face_sad_calm.mp4` (4초)
시작·끝 프레임: `stand_face_sad` — 작업 ID `eb51241d-041b-4dd2-b29b-5a1a2f6b215d` · [이미지 보기](https://d8j0ntlcm91z4.cloudfront.net/user_3K3GDbFrhFIfl8ivGn5P16JoJBa/hf_20261120_210648_eb51241d-041b-4dd2-b29b-5a1a2f6b215d.png)
```
Higgsfield로 영상을 만들어 줘.
- 모델: seedance_2_5, 모드 omni_reference, 720p, 세로 9:16, 길이 4초, 소리 없음
- 시작 프레임과 끝 프레임: 둘 다 이전 생성 작업 ID eb51241d-041b-4dd2-b29b-5a1a2f6b215d (stand_face_sad) — 처음과 끝이 같아야 반복 재생된다
- 결과 파일 이름: motion_face_sad_calm.mp4
- 프롬프트:
She closes her eyes and takes one slow deep breath to calm herself, her shoulders rise and fall, then she opens her eyes and looks ahead with a small determined nod. The reference image is both the first and the last frame.
Locked-off static camera, no camera movement, no zoom, no cut. The plain light grey background stays completely unchanged and empty. The girl stays in the same spot and the same size in the frame for the whole clip; her outfit, hair and face stay consistent with the first frame. The clip ends in exactly the same pose as it starts so it can loop seamlessly. Gentle, small, natural motion; 2D anime illustration style matching the reference, no 3D look.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

#### `motion_face_blank_idle.mp4` (4초)
시작·끝 프레임: `stand_face_blank` — 작업 ID `ecd8abba-94e7-49c4-b9ab-3cc9711a856b` · [이미지 보기](https://d8j0ntlcm91z4.cloudfront.net/user_3K3GDbFrhFIfl8ivGn5P16JoJBa/hf_20261120_211010_ecd8abba-94e7-49c4-b9ab-3cc9711a856b.png)
```
Higgsfield로 영상을 만들어 줘.
- 모델: seedance_2_5, 모드 omni_reference, 720p, 세로 9:16, 길이 4초, 소리 없음
- 시작 프레임과 끝 프레임: 둘 다 이전 생성 작업 ID ecd8abba-94e7-49c4-b9ab-3cc9711a856b (stand_face_blank) — 처음과 끝이 같아야 반복 재생된다
- 결과 파일 이름: motion_face_blank_idle.mp4
- 프롬프트:
She stares blankly into the distance, almost motionless, with very slow shallow breathing; one delayed, slow blink. Her arms hang loosely at her sides. Numb, exhausted, empty mood. The reference image is both the first and the last frame.
Locked-off static camera, no camera movement, no zoom, no cut. The plain light grey background stays completely unchanged and empty. The girl stays in the same spot and the same size in the frame for the whole clip; her outfit, hair and face stay consistent with the first frame. The clip ends in exactly the same pose as it starts so it can loop seamlessly. Gentle, small, natural motion; 2D anime illustration style matching the reference, no 3D look.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

#### `motion_face_blank_flinch.mp4` (4초)
시작·끝 프레임: `stand_face_blank` — 작업 ID `ecd8abba-94e7-49c4-b9ab-3cc9711a856b` · [이미지 보기](https://d8j0ntlcm91z4.cloudfront.net/user_3K3GDbFrhFIfl8ivGn5P16JoJBa/hf_20261120_211010_ecd8abba-94e7-49c4-b9ab-3cc9711a856b.png)
```
Higgsfield로 영상을 만들어 줘.
- 모델: seedance_2_5, 모드 omni_reference, 720p, 세로 9:16, 길이 4초, 소리 없음
- 시작 프레임과 끝 프레임: 둘 다 이전 생성 작업 ID ecd8abba-94e7-49c4-b9ab-3cc9711a856b (stand_face_blank) — 처음과 끝이 같아야 반복 재생된다
- 결과 파일 이름: motion_face_blank_flinch.mp4
- 프롬프트:
She flinches slightly as if hearing a sudden small noise: her shoulders jerk up and her eyes widen for a moment, she glances to the side, then slowly sinks back into the same blank stare. The reference image is both the first and the last frame.
Locked-off static camera, no camera movement, no zoom, no cut. The plain light grey background stays completely unchanged and empty. The girl stays in the same spot and the same size in the frame for the whole clip; her outfit, hair and face stay consistent with the first frame. The clip ends in exactly the same pose as it starts so it can loop seamlessly. Gentle, small, natural motion; 2D anime illustration style matching the reference, no 3D look.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

## 3. 걱정·불안

`face_worried` 그림이 보일 때: 경고(식량·식수 부족, 위협 등)가 있을 때.

#### `motion_face_worried_idle.mp4` (4초)
시작·끝 프레임: `stand_face_worried` — 작업 ID `87514d9c-770b-44b4-a72b-c134665d03f8` · [이미지 보기](https://d8j0ntlcm91z4.cloudfront.net/user_3K3GDbFrhFIfl8ivGn5P16JoJBa/hf_20261120_210539_87514d9c-770b-44b4-a72b-c134665d03f8.png)
```
Higgsfield로 영상을 만들어 줘.
- 모델: seedance_2_5, 모드 omni_reference, 720p, 세로 9:16, 길이 4초, 소리 없음
- 시작 프레임과 끝 프레임: 둘 다 이전 생성 작업 ID 87514d9c-770b-44b4-a72b-c134665d03f8 (stand_face_worried) — 처음과 끝이 같아야 반복 재생된다
- 결과 파일 이름: motion_face_worried_idle.mp4
- 프롬프트:
She fidgets nervously: her fingers fiddle with the hem of her sleeve, she bites her lip lightly and glances to the left and right, then back to the front. The reference image is both the first and the last frame.
Locked-off static camera, no camera movement, no zoom, no cut. The plain light grey background stays completely unchanged and empty. The girl stays in the same spot and the same size in the frame for the whole clip; her outfit, hair and face stay consistent with the first frame. The clip ends in exactly the same pose as it starts so it can loop seamlessly. Gentle, small, natural motion; 2D anime illustration style matching the reference, no 3D look.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

## 4. 좋은 기분

`face_smile` 그림이 보일 때: 스트레스 25 미만.

#### `motion_face_smile_idle.mp4` (4초)
시작·끝 프레임: `stand_face_smile` — 작업 ID `f2cee9af-a790-495f-80b5-104a350b0884` · [이미지 보기](https://d8j0ntlcm91z4.cloudfront.net/user_3K3GDbFrhFIfl8ivGn5P16JoJBa/hf_20261120_165031_f2cee9af-a790-495f-80b5-104a350b0884.png)
```
Higgsfield로 영상을 만들어 줘.
- 모델: seedance_2_5, 모드 omni_reference, 720p, 세로 9:16, 길이 4초, 소리 없음
- 시작 프레임과 끝 프레임: 둘 다 이전 생성 작업 ID f2cee9af-a790-495f-80b5-104a350b0884 (stand_face_smile) — 처음과 끝이 같아야 반복 재생된다
- 결과 파일 이름: motion_face_smile_idle.mp4
- 프롬프트:
She sways gently from side to side with a relaxed light smile, as if humming a song to herself, her hair swaying softly; a cheerful, calm mood. The reference image is both the first and the last frame.
Locked-off static camera, no camera movement, no zoom, no cut. The plain light grey background stays completely unchanged and empty. The girl stays in the same spot and the same size in the frame for the whole clip; her outfit, hair and face stay consistent with the first frame. The clip ends in exactly the same pose as it starts so it can loop seamlessly. Gentle, small, natural motion; 2D anime illustration style matching the reference, no 3D look.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

## 5. 부상

`mark_bandage_arm` 그림이 보일 때: 부상(INJURED) 상태 (지금 게임은 부상이면 팔 붕대만 고른다 — 머리·다리 붕대 그림은 쓰이지 않아 영상도 만들지 않는다).

#### `motion_mark_bandage_arm_idle.mp4` (4초)
시작·끝 프레임: `stand_mark_bandage_arm` — 작업 ID `6bbbad82-e5fa-414e-9541-10f3930b7ab3` · [이미지 보기](https://d8j0ntlcm91z4.cloudfront.net/user_3K3GDbFrhFIfl8ivGn5P16JoJBa/hf_20261120_164919_6bbbad82-e5fa-414e-9541-10f3930b7ab3.png)
```
Higgsfield로 영상을 만들어 줘.
- 모델: seedance_2_5, 모드 omni_reference, 720p, 세로 9:16, 길이 4초, 소리 없음
- 시작 프레임과 끝 프레임: 둘 다 이전 생성 작업 ID 6bbbad82-e5fa-414e-9541-10f3930b7ab3 (stand_mark_bandage_arm) — 처음과 끝이 같아야 반복 재생된다
- 결과 파일 이름: motion_mark_bandage_arm_idle.mp4
- 프롬프트:
She holds her bandaged arm gently against her body with the other hand, shifts her weight carefully and winces slightly as she moves it, then breathes out and steadies herself. The bandage stays clean and white. The reference image is both the first and the last frame.
Locked-off static camera, no camera movement, no zoom, no cut. The plain light grey background stays completely unchanged and empty. The girl stays in the same spot and the same size in the frame for the whole clip; her outfit, hair and face stay consistent with the first frame. The clip ends in exactly the same pose as it starts so it can loop seamlessly. Gentle, small, natural motion; 2D anime illustration style matching the reference, no 3D look.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

## 6. 몸 상태

`mark_mask`(감염 증상 2 이상) · `mark_thin`(포만 −50 미만) · `mark_dirty`(청결 50 미만) 그림이 보일 때.

#### `motion_mark_mask_idle.mp4` (4초)
시작·끝 프레임: `stand_mark_mask` — 작업 ID `3abe881f-c2ca-4c87-82a8-491f36b6c86e` · [이미지 보기](https://d8j0ntlcm91z4.cloudfront.net/user_3K3GDbFrhFIfl8ivGn5P16JoJBa/hf_20261120_211531_3abe881f-c2ca-4c87-82a8-491f36b6c86e.png)
```
Higgsfield로 영상을 만들어 줘.
- 모델: seedance_2_5, 모드 omni_reference, 720p, 세로 9:16, 길이 4초, 소리 없음
- 시작 프레임과 끝 프레임: 둘 다 이전 생성 작업 ID 3abe881f-c2ca-4c87-82a8-491f36b6c86e (stand_mark_mask) — 처음과 끝이 같아야 반복 재생된다
- 결과 파일 이름: motion_mark_mask_idle.mp4
- 프롬프트:
Wearing the mask, she coughs lightly twice into the crook of her elbow, then lowers her arm, breathes a little heavily through the mask and stands upright again. Mild cold-like symptoms only. The reference image is both the first and the last frame.
Locked-off static camera, no camera movement, no zoom, no cut. The plain light grey background stays completely unchanged and empty. The girl stays in the same spot and the same size in the frame for the whole clip; her outfit, hair and face stay consistent with the first frame. The clip ends in exactly the same pose as it starts so it can loop seamlessly. Gentle, small, natural motion; 2D anime illustration style matching the reference, no 3D look.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

#### `motion_mark_thin_idle.mp4` (4초)
시작·끝 프레임: `stand_mark_thin` — 작업 ID `5b25d2ae-d07e-44b6-8f35-731ad2c92a85` · [이미지 보기](https://d8j0ntlcm91z4.cloudfront.net/user_3K3GDbFrhFIfl8ivGn5P16JoJBa/hf_20261120_211640_5b25d2ae-d07e-44b6-8f35-731ad2c92a85.png)
```
Higgsfield로 영상을 만들어 줘.
- 모델: seedance_2_5, 모드 omni_reference, 720p, 세로 9:16, 길이 4초, 소리 없음
- 시작 프레임과 끝 프레임: 둘 다 이전 생성 작업 ID 5b25d2ae-d07e-44b6-8f35-731ad2c92a85 (stand_mark_thin) — 처음과 끝이 같아야 반복 재생된다
- 결과 파일 이름: motion_mark_thin_idle.mp4
- 프롬프트:
She presses one hand lightly on her stomach as if hungry, her shoulders sag slightly and she looks down for a moment, then lowers her hand and straightens up. The reference image is both the first and the last frame.
Locked-off static camera, no camera movement, no zoom, no cut. The plain light grey background stays completely unchanged and empty. The girl stays in the same spot and the same size in the frame for the whole clip; her outfit, hair and face stay consistent with the first frame. The clip ends in exactly the same pose as it starts so it can loop seamlessly. Gentle, small, natural motion; 2D anime illustration style matching the reference, no 3D look.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

#### `motion_mark_dirty_idle.mp4` (4초)
시작·끝 프레임: `stand_mark_dirty` — 작업 ID `b58046cf-9e76-4f9e-852e-51acf5a22b42` · [이미지 보기](https://d8j0ntlcm91z4.cloudfront.net/user_3K3GDbFrhFIfl8ivGn5P16JoJBa/hf_20261120_211324_b58046cf-9e76-4f9e-852e-51acf5a22b42.png)
```
Higgsfield로 영상을 만들어 줘.
- 모델: seedance_2_5, 모드 omni_reference, 720p, 세로 9:16, 길이 4초, 소리 없음
- 시작 프레임과 끝 프레임: 둘 다 이전 생성 작업 ID b58046cf-9e76-4f9e-852e-51acf5a22b42 (stand_mark_dirty) — 처음과 끝이 같아야 반복 재생된다
- 결과 파일 이름: motion_mark_dirty_idle.mp4
- 프롬프트:
She notices the dust on her sleeve, brushes it off with a few quick strokes of her hand and pats her skirt, then returns to the starting pose. The reference image is both the first and the last frame.
Locked-off static camera, no camera movement, no zoom, no cut. The plain light grey background stays completely unchanged and empty. The girl stays in the same spot and the same size in the frame for the whole clip; her outfit, hair and face stay consistent with the first frame. The clip ends in exactly the same pose as it starts so it can loop seamlessly. Gentle, small, natural motion; 2D anime illustration style matching the reference, no 3D look.
No gore, no blood, no visible wounds, no sexualized poses or revealing clothing, modest school uniforms, no text, no logo, no watermark.
```

---

## 7. 체크리스트

- [ ] `motion_face_tired_idle.mp4` (P1)
- [ ] `motion_face_tired_yawn.mp4` (P1)
- [ ] `motion_face_sad_idle.mp4` (P1)
- [ ] `motion_face_sad_calm.mp4` (P2)
- [ ] `motion_face_blank_idle.mp4` (P1)
- [ ] `motion_face_blank_flinch.mp4` (P2)
- [ ] `motion_face_worried_idle.mp4` (P1)
- [ ] `motion_face_smile_idle.mp4` (P2)
- [ ] `motion_mark_bandage_arm_idle.mp4` (P1)
- [ ] `motion_mark_mask_idle.mp4` (P1)
- [ ] `motion_mark_thin_idle.mp4` (P2)
- [ ] `motion_mark_dirty_idle.mp4` (P2)

## 받은 뒤 내가 하는 일

1. 영상을 프레임으로 뽑아(플레이 모드 VideoPlayer) 배경을 지우고 정지 그림과 같은 자리·크기로 맞춘다 (`code_temp/apogirl/tools/make_standing_motion.py`, 12fps 340×512).
2. `Resources/Art/Standing/Motion/{그림}_{동작}/`에 두고, `standing_map.json` `Motion`을 그림별 표로 늘린다 (그림 → idle·이따금 끼울 동작·간격).
3. 표정 그림이 바뀔 때(예: 보통 → 지침) 영상과 정지 그림 사이가 튀면 0.25초 겹쳐 바꾼다.
