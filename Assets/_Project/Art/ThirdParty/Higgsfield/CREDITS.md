# Higgsfield 생성 에셋 출처

〈마지막 학기 (가제)〉의 그림·소리 일부는 Higgsfield AI로 만들었다 (스팀 "AI 생성 콘텐츠" 공개용 기록).
프롬프트와 모델 선택은 [Docs/HIGGSFIELD_ASSETS.md](../../../../../Docs/HIGGSFIELD_ASSETS.md).

- 받은 날: 2026-11-20 (1차분, 원본은 `ArtInbox/`, git 제외)
- 게임용 사본: `Assets/_Project/Resources/Art/` (그림), `Assets/_Project/Resources/Audio/` (소리)
- 모델은 문서 0.2절의 계획 기준이다. 실제로 다른 모델을 썼다면 이 표를 고친다.

| 종류 | 게임 안 위치 | 모델 (계획) |
|---|---|---|
| 일러스트: 타이틀 키 아트·로고, 거처 배경, 사건 그림, 엔딩 원화, 지역 그림 | `Resources/Art/Story/` | `gpt_image_2_6` |
| 인물 초상화 | `Resources/Art/Portraits/` | `seedream_v5_pro` / `nano_banana_2_1` |
| 탐험 스프라이트·소품·문 | `Resources/Art/ExplorationHD/` | `gpt_image_2_6`, `nano_banana_flash`, `image_background_remover` |
| UI 아이콘·얼굴 아이콘·아이템 | `Resources/Art/UI/` | `recraft_v4_1` / `gpt_image_2_6` |
| 효과음 | `Resources/Audio/Sfx/` | `seed_audio` |
| 라디오 목소리 | `Resources/Audio/Voice/` | `text2speech_v2` / `inworld_text_to_speech` |

게임에 넣을 때 한 손질: 크기 줄이기·JPG/PNG 변환(`art_import.py`), 효과음 앞뒤 잡음 자르기·모노·최댓값 −3 dBFS 맞춤(`audio_import.py`).
