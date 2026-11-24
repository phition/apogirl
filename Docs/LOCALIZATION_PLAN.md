# 번역 기획서 — 영어(en)·일본어(ja)와 타이틀 언어 선택

- 작성: 2026-11-24. **상태: 보류** (사용자 결정 2026-11-24 — 작업량이 커서 기획서만 두고 시작하지 않음).
- 목표: 게임 안의 대사와 화면 문자 전부를 영어·일본어로 옮기고, 타이틀에서 언어(한국어·English·日本語)를 고르게 한다. 고른 언어는 다음 실행에도 유지한다.
- 관련: [M8_PLAN](M8_PLAN.md) "M8-5 번역 구조"와 결정 필요 2(언어 범위). 이 기획을 시작하면 결정 2는 "C. 영어·일본어 추가"로 정해진다.
- 아래 결정 L1~L9는 **추천안 기준**이다. 시작할 때 사용자가 추천안을 그대로 승인하거나 바꾼다.

---

## 1. 번역 대상과 분량 (2026-11-24 조사)

| 대상 | 위치 | 분량 | 비고 |
|---|---|---|---|
| 화면 문자열 | `Assets/_Project/Localization/Source/ko.json` (표 `UI` 1,720 · `Actions` 150 · `Items` 128) | 약 2,000키, 약 3.7만 자 | 메뉴 `ApoGirl/Localization/Sync Tables`로 표 생성 |
| 대사 | `Assets/_Project/Resources/Dialogue/**/*.yarn` 26개 파일 | 3,850줄, 약 14.6만 자(공백 포함) | 프롤로그 1 · 1막 2 · 2~4막 각 3 · 이벤트 14 |
| 데이터 문구 | `Content/**/*.json` | **없음** | 일기·후일담·엔딩·세력 기록·소풍 등은 모두 `TextKey`로 ko.json을 가리킨다. 세이브도 키만 저장(`DiaryEntry.TextKey`, `FactionLogEntry.TextKey`)이라 언어를 바꿔도 옛 세이브가 그대로 번역된다 |
| 코드 속 화면 문자열 | C# | 몇 곳 | 기본 이름 "한서진"(`GameState`·`YarnFunctions`·`StoryText`), 조사로 문장을 조립하는 곳(`PeopleView`·`CraftUi`·`HubScreen`), `ActionResolver`의 "딱히 없어" |
| 그림 속 글자 | `Resources/Art/Story/logo_title.png` | 1장 | 한글 제목 로고 |
| 빌드 안내 | `Editor/BuildMenu.cs`가 쓰는 "읽어 주세요.txt" | 1개 | 테스터용 |
| UXML 기본 글자 | `UI/Hub.uxml` 등 | 몇 줄 | 대부분 코드가 덮어쓰는지 확인 필요 |

합치면 약 18만 자 × 2개 언어. 한 번에 끝낼 분량이 아니라 단계(5절)로 나눈다.

### 조사하며 찾은 구조 문제 (번역 전에 고칠 것)

1. **Yarn 줄 ID가 고정되어 있지 않다.** 대사에 `#line:` 태그가 하나도 없어 Yarn이 자동 ID(파일·노드·순번)를 쓴다. 한 줄을 끼우면 뒤 ID가 밀려 번역이 엉뚱한 줄에 붙는다.
2. **초상·소리를 한국어 화자 이름으로 찾는다.** `art_map.json` `Name`("윤 선생님" 등)과 `DialogueView.ShowLine`의 `PortraitFor(speaker)`·`Sound.OnLine(speaker, …)`. 화자 이름을 번역하면 초상이 사라진다.
3. **`Loc` 캐시를 언어가 바뀌어도 비우지 않는다** (`Loc.Get`의 `Cache`).
4. **글꼴이 OS 맑은 고딕뿐이다** (`KoreanFont`, 런타임 동적 글꼴). 일본어 한자(신자체) 일부가 빠질 수 있다.
5. **로케일이 ko 하나뿐이다** (`Localization/Locales/Korean (ko).asset`, `ContentPipeline.LocaleCode`, `EventDialoguePlayer`의 `_parser.ParseString(…, LocaleCode)`).
6. **Yarn 인라인 식은 `{0}` 자리 표시로 컴파일된다.** `{name()}`·`{josa(given(), "이/가")}`는 대사 문자열 안에서 `{0}`·`{1}`이 되고 실행 때 채워진다. 번역문도 같은 자리 표시를 지켜야 한다(순서는 바꿔도 된다).

---

## 2. 설계 요약

- 언어는 ko(원본)·en·ja 세 개. Unity Localization 로케일 en·ja를 더한다.
- 번역 원본 파일:
  - 화면 문자열 `Localization/Source/en.json`·`ja.json` — ko.json과 같은 표·키 구조.
  - 대사 `Localization/Source/Dialogue/{en,ja}/<yarn 파일 이름>.json` — `{ "line:xxxxxx": { "ko": "원문 사본", "text": "번역" } }`.
- Sync(`LocalizationSync`·`ContentPipeline.SyncDialogueTable`)가 이 원본을 `UI_en`·`Dialogue_ja` 등의 표로 옮긴다. 지금 구조(D21: Unity Localization 연동) 그대로이고, 언어만 늘린다.
- 대사 3,850줄에 고정 `#line:` 태그를 한 번 붙인다(도구 스크립트, 이미 있는 `#shadow:`·`#choice:` 태그는 유지). 이후 새 줄에 태그가 없으면 점검이 잡아낸다(가져오기 때 자동으로 붙이는 것도 검토).
- 대사 번역 항목에 한국어 원문 사본(`ko`)을 함께 두어, 원문이 바뀌면 "낡은 번역"으로 잡아낸다. 화면 문자열은 같은 이유로 `Source/ko_snapshot.json`(번역할 때의 원문)을 둔다.
- **번역이 없는 키·줄은 한국어로 보인다.** 단계 중간에도 게임이 돌아간다.
- 화자 이름은 번역해서 보여 주고, 초상·소리는 **한국어 원문 줄의 화자**로 찾는다(`EventDialoguePlayer.Localize`가 원문 화자도 함께 돌려줌).
- 언어를 바꾸면 `Loc` 캐시를 비우고 타이틀을 다시 그린다. 고른 언어는 PlayerPrefs `ApoGirl.Language`에 저장하고, 부팅(`GameApp`) 때 어떤 문자열보다 먼저 적용한다.
- 빠진 번역 점검:
  - EditMode 테스트 `LocalizationCoverageTests`: 키·줄 누락, 빈 번역, en·ja 번역에 한글이 섞임, `{0}`·`{given}`·`{name}`·`{friend}` 자리 표시가 원문과 다름, 원문이 바뀐 낡은 번역, `#line:` 없는 대사 줄.
  - 에디터 메뉴 `ApoGirl/Localization/Coverage Report` → `Temp/ApoGirlLocalizationReport.txt` (언어·표·파일별 진행률).
- 개발 단계 확인용 가짜 언어(모든 문자열에 표시를 붙인 것)는 M8-5 기준과 같이, 점검 메뉴에서 켤 수 있게 한다.

---

## 3. 결정 (추천안)

| # | 항목 | 추천안 | 다른 선택지 |
|---|---|---|---|
| L1 | 대사 현지화 방식 | **A.** 고정 `#line:` 태그 + 언어별 JSON(원문 사본 포함) → Unity Localization `Dialogue_{lang}` 표. 지금 파이프라인과 같다 | B. Yarn Spinner 기본 CSV 현지화(낡은 번역 감지 내장, 하지만 화면 문자열과 대사가 두 체계로 갈림) · C. 언어별 yarn 사본(분기·조건이 언어마다 어긋남, 비추천) |
| L2 | 일본어 글꼴 | **A.** 지금처럼 OS 글꼴, 언어별 목록 — ja: Yu Gothic UI → Meiryo, 폴백 맑은 고딕(한글 이름용, `FontAsset.fallbackFontAssetTable`). en·ko: 맑은 고딕 그대로 | B. Noto Sans CJK(KR+JP)를 빌드에 넣기 — 정식 글꼴이라 M8에서 한국어 정식 글꼴과 함께 정하기를 권함 |
| L3 | 인물·고유명사 표기 | **A.** 인명은 소리대로, 세력·장소는 뜻으로 (4절 용어집 초안) | B. 영어권 별칭(Jenny 등) — 배경이 한국이라 비추천 |
| L4 | 말투·용어집 | **A.** 용어집 `Docs/LOCALIZATION_GLOSSARY.md`를 먼저 만들어 승인받고 번역 (4절이 초안) | B. 용어집 없이 파일별로 바로 번역 |
| L5 | 한국어 조사 | **A.** en·ja에서 `josa()`·`Loc.Josa`·`{given:은/는}`는 낱말만 돌려준다. 번역문에는 조사 쌍을 쓰지 않는다. 코드에서 조사로 문장을 조립하는 곳(`PeopleView`·`CraftUi`·`HubScreen`)은 서식 키(`{0}`)로 바꾼다 | — |
| L6 | 작업 순서 | **A.** 5절 단계별 커밋. 대사는 용어집을 주고 하위 에이전트로 파일별로 나눠 **직접** 번역(외부 번역 서비스에 보내지 않음) | B. 한 언어(en)를 끝까지 한 뒤 ja |
| L7 | 언어 선택 UI·저장 | **A.** 타이틀 오른쪽 아래에 "한국어 · English · 日本語"를 늘 둔다. 첫 실행은 OS 언어(`Application.systemLanguage`, 셋 중 아니면 en), 그다음은 저장값(PlayerPrefs). 게임 중 바꾸기는 M8-2 설정 화면에서 | B. 타이틀 메뉴에 "언어" 단추 → 하위 목록 |
| L8 | 번역에서 뺄 것 | **A.** 개발용 화면은 한국어로 둔다(F1 디버그·밸런스 표·봇 보고서·콘텐츠 검증 메시지·`BotLogWriter`). 로고는 `logo_title_en`·`logo_title_ja` 그림이 있으면 쓰고 없으면 글자 제목(`title.game`). 이벤트 컷 속 간판 글자는 그대로. "읽어 주세요.txt"에는 영어·일본어 절을 더한다 | B. 디버그 화면까지 번역 |
| L9 | 기본 이름·부르는 이름 | **A.** 저장된 이름이 기본값 "한서진"이면 언어에 맞게 보여 준다(Seojin Han / ハン・ソジン). 플레이어가 입력한 이름은 그대로. 부르는 이름(`given`)은 별명 → en은 첫 낱말, ja는 「・」 뒤(없으면 전체), ko는 지금처럼 세 글자면 성을 뺌 | B. en·ja 프롤로그에서 이름·성을 따로 입력 |

---

## 4. 용어집 초안 (L3·L4)

시작할 때 `Docs/LOCALIZATION_GLOSSARY.md`로 옮겨 다듬고 승인받는다.

### 제목
| ko | en | ja |
|---|---|---|
| 마지막 학기 (가제) | The Last Semester | 最後の学期 |

### 인물 (인명은 국어의 로마자 표기법, 영어는 이름-성 순)
| ko | en | ja |
|---|---|---|
| 한서진 / 서진 (주인공 기본 이름) | Seojin Han / Seojin | ハン・ソジン / ソジン |
| 윤 선생님 | Ms. Yoon | ユン先生 |
| 박 선생님 (간호) | Ms. Park | パク先生 |
| 도현 | Dohyeon | ドヒョン |
| 송예지 (방관자) | Yeji Song | ソン・イェジ |
| 최하린 (주동자) | Harin Choi | チェ・ハリン |
| 선배 | (이름이 나오면 이름, 아니면) the upperclassman | 先輩 |
| 유랑자 | the Wanderer | 流れ者 |
| 지우 (친구 기본 호칭) | Jiwoo | ジウ |
| 고양이 '급식' | the cat "Lunch" | 猫の「給食」 |
| 정 반장·강철·민규·준·백 등 | 대본 확인 뒤 정함 (반장 = 우두머리 뜻이면 en "Boss Jeong", ja 「チョン班長」) | |

### 세력·장소
| ko | en | ja |
|---|---|---|
| 사거리 파 | the Crossroads Crew | 四つ角組 |
| 더불어 마트 | Together Mart | トゥギャザーマート (간판 그대로의 느낌) |
| UBC | UBC | UBC |
| 교실동 / 체육창고 / 방송반 | Classroom Wing / gym storeroom / broadcast club room | 教室棟 / 体育倉庫 / 放送部室 |

### 말투
- **서술**: 한국어는 주어를 감춘 1인칭 과거형(했다체).
  - en: 1인칭 과거형("I knocked, but no one stopped.").
  - ja: 一人称「私」, だ・である체 과거형("ドアを叩いたが、誰も止まらなかった。").
- **서진**: 친구·후배에게는 반말 톤(en 가벼운 구어, ja タメ口), 선생님께는 존댓말 톤(en 공손, ja です・ます).
- **선생님**: 학생에게 다정한 반말 톤(en 따뜻한 구어, ja 「〜だよ」「〜しなさい」).
- **도현**(중학생): 짧고 어린 말투.
- 콘텐츠 안전(마스터 프롬프트 13장)은 번역에서도 그대로 지킨다. 원문보다 수위를 올리지 않는다.

### 시스템 용어 (일부)
| ko | en | ja |
|---|---|---|
| 주 / 낮 / 밤 | week / day / night | 週 / 昼 / 夜 |
| 탐험 / 수색 / 배낭 | expedition / search / backpack | 探索 / 捜索 / リュック |
| 호감 / 스트레스 / 포만 / 수분 | affinity / stress / fullness / hydration | 好感 / ストレス / 満腹 / 水分 |
| 생활기록부 / 결산 일기 | school record / weekly diary | 生活記録簿 / 週末の日記 |
| 습격 / 감염 / 부상 | raid / infection / injury | 襲撃 / 感染 / 負傷 |

---

## 5. 작업 단계

| 단계 | 내용 | 다 됐다는 기준 |
|---|---|---|
| **P1 구조** | 로케일 en·ja, 번역 원본 파일 틀, Sync 확장, `#line:` 태그 붙이기 도구(1회), 언어 선택 UI·PlayerPrefs·부팅 적용, `Loc` 캐시 비우기, 언어별 글꼴, 원문 화자로 초상 찾기, 조사·기본 이름 언어별 처리, 로고 대체, 빠진 번역 점검(테스트·메뉴) | 타이틀에서 언어를 바꾸면 (번역이 없는 곳은 한국어로) 화면이 바뀌고 일본어가 깨지지 않음. EditMode 통과 |
| **P2 용어집** | `LOCALIZATION_GLOSSARY.md` 확정 | 사용자 승인 |
| **P3 화면 문자열** | ko.json 약 2,000키 en·ja | 점검에서 화면 문자열 빠짐 0 |
| **P4 대사** | 묶음별 en·ja, 묶음마다 커밋: 프롤로그+1막 / 2막 / 3막 / 4막 / 이벤트 14파일 | 점검에서 대사 빠짐 0 |
| **P5 다듬기** | 글 길이로 넘치는 UI(영어는 길고 일본어는 짧다) 손보기, 통독, "읽어 주세요.txt" | 세 언어로 프롤로그~1막 실제 플레이 확인 |

- 커밋은 worktree에서 하고 main에 fast-forward. 단계마다 [PROGRESS](PROGRESS.md)에 한 줄.
- 번역 중 다른 세션이 대사를 고치면 원문 사본 비교로 낡은 번역이 잡히니, 점검 보고서를 보고 그 줄만 다시 옮긴다.
- 이후 새 콘텐츠를 만들 때의 규칙(시작 뒤 [CONTENT_PIPELINE](CONTENT_PIPELINE.md)에 추가): 새 대사 줄에 `#line:` 태그, ko.json 새 키는 en·ja에 빈칸 없이(못 옮기면 점검 경고로 남김).
