# 코딩 규칙 (Coding Conventions)

마스터 프롬프트 11장(Unity 기술 지침)을 구체화한 규칙이다. 충돌하면 마스터 프롬프트가 우선한다.

## 1. 어셈블리와 의존 방향

| 어셈블리 | 폴더 | 역할 | 참조 가능 |
|---|---|---|---|
| `ApoGirl.Core` | `Scripts/Core` | 순수 C# 시뮬레이션(상태·규칙·판정·엔딩) | 없음 (**UnityEngine 참조 금지**) |
| `ApoGirl.Data` | `Scripts/Data` | ScriptableObject 정의 → Core 정의로 변환 | Core |
| `ApoGirl.Systems` | `Scripts/Systems` | 세이브(파일 I/O), 로컬라이제이션, 입력, 오디오 | Core, Data |
| `ApoGirl.Presentation` | `Scripts/Presentation` | UI, 연출, 일러스트 제어 | Core, Data, Systems |
| `ApoGirl.Exploration` | `Scripts/Exploration` | 탐험 모드(M2b~) | Core, Data, Systems |
| `ApoGirl.Tests.EditMode` | `Scripts/Tests/EditMode` | EditMode 유닛 테스트 | Core (+필요 시 Data) |

- 의존은 **위 표의 방향으로만** 흐른다. Core가 다른 어셈블리를 참조하는 일은 없다.
- 시스템 간 통신은 C# event 또는 이벤트 버스. 싱글턴 남용 금지(서비스 로케이터/가벼운 DI).
- 탐험 ↔ 허브는 `ExpeditionRequest` / `ExpeditionResult` 순수 데이터로만 주고받는다.

## 2. 네임스페이스와 파일
- 네임스페이스 = 어셈블리 이름 + 하위 폴더. 예: `ApoGirl.Core.State`.
- 파일 하나에 공개 타입 하나가 원칙. 단, 작은 상태 데이터 클래스 묶음(`GameState.cs`)은 예외로 허용.

## 3. 네이밍
| 대상 | 규칙 | 예 |
|---|---|---|
| 클래스·구조체·enum·메서드·프로퍼티 | PascalCase | `WeekPlan`, `ResolveSlot()` |
| 인터페이스 | `I` + PascalCase | `IRandomService` |
| private 필드 | `_camelCase` | `_slots` |
| 상수 | PascalCase | `TotalWeeks` |
| 지역 변수·매개변수 | camelCase | `slotIndex` |

## 4. 데이터 ID 규칙
- 콘텐츠는 **문자열 ID**로 참조하고 세이브에도 ID로 저장한다. ID는 한 번 정하면 바꾸지 않는다.
- 접두사: `ACT_`(행동) `ITEM_`(아이템) `EVT_`(이벤트) `NPC_` `END_`(엔딩) `LOC_`(탐험 지역) `FAC_`(시설) `FLAG_`(플래그) `CLUE_`(단서) `TRAIT_`(특성)
- 형식: 대문자 스네이크 케이스. 예: `EVT_SPRING_W03_RADIO_01`
- 설계상 고정된 집합(능력치 8종, 숙련도 7종, 자원 등)만 enum으로 둔다.

## 5. 수치와 텍스트
- 매직 넘버 금지. 밸런스 수치는 `Assets/_Project/Data/Balance/`의 SO 또는 상수로 둔다.
- 플레이어에게 보이는 문자열은 로컬라이제이션 키로 관리한다(초기엔 한국어만).
- 한국어 조사(이/가, 은/는, 을/를, 아/야)는 이름이 바뀔 수 있으므로 항상 조사 처리 함수를 거친다.

## 6. Unity 사용 규칙
- `Update()` 남용 금지. 이벤트·코루틴·async를 쓴다.
- `GetComponent`/`Find` 반복 호출 금지(캐싱).
- Inspector 노출 필드는 `[SerializeField] private` + `[Tooltip]`(한국어). public 필드 노출은 피한다.
- 공개 API에는 XML 주석을 한 줄 이상 단다.

## 7. 테스트
- 스탯 계산, 판정, 슬롯 규칙(강제 휴식·수면 부채), 엔딩 판정, 세이브/로드는 **EditMode 테스트 필수**.
- 난수는 시드 기반 `IRandomService`를 주입해 결과를 재현 가능하게 한다.
- 테스트 이름: `대상_조건_기대결과` 형식. 예: `NightExpedition_WithoutRest_AddsSleepDebt`.

## 8. Git
- 로컬 커밋만. push는 사용자가 요청할 때만.
- 바이너리(이미지·오디오·PSD 등)는 Git LFS(`.gitattributes`)로 관리한다.
- 씬 병합 충돌을 피하기 위해 씬 작업은 씬 단위로 나눈다.
