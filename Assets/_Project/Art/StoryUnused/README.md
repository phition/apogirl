# 게임에서 뺀 이벤트 원화 (StoryUnused)

게임 폴더 `Resources/Art/Story/Events/`에는 **지금 게임이 쓰는 그림만** 둔다(그림 표 `Resources/Art/art_map.json`이 가리키는 288장).
이 폴더의 그림은 연결할 이벤트가 없어서 빼 둔 것이다. `Resources` 밖이라 빌드에는 들어가지 않는다.

| 그림 | 빼 둔 까닭 |
|---|---|
| `evt_a3_police` | 그림 표에 줄이 없다 |
| `evt_a4_farewell` | 그림 표에 줄이 없다 |
| `evt_cat_warn` | 고양이가 경고하는 이벤트가 아직 없다 |
| `evt_deep_door` · `evt_deep_oneway` | 깊은 곳 문·일방 출구에 붙일 이벤트가 아직 없다 |
| `evt_lost_found` | 그림 표에 줄이 없다 |
| `evt_season_flood` · `evt_season_snow_block` | 계절 재해 이벤트가 아직 없다 |

## 다시 쓰려면
1. 그림을 `Resources/Art/Story/Events/`로 옮긴다(`.meta`도 같이 — Unity 안에서 끌어 옮기면 된다).
2. `Resources/Art/art_map.json`의 `Events`에 `{ "Event": "이벤트 ID", "Image": "Events/그림이름" }` 줄을 더한다.

## 원화(원본 png)는 어디에
`ArtInbox/illustrations/events_정리/` — `사용중`(게임에 들어간 판) · `교체된_옛판`(다시 받아 바뀐 것) · `미사용`.
원화를 고친 뒤 `python Tools/apply_event_art.py`를 돌리면 달라진 그림만 게임 폴더에 다시 만든다.
