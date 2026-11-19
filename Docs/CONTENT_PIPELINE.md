# 콘텐츠 파이프라인 (M3)

이벤트·NPC는 **JSON(데이터·조건·효과)** 과 **Yarn(대사·선택지 문구)** 두 원본으로 만든다.
SO 에셋과 문자열 테이블은 생성물이라 직접 고치지 않는다.

## 1. 파일 위치

| 원본 | 위치 | 생성물 |
|---|---|---|
| 이벤트·NPC | `Assets/_Project/Content/Events/*.json` | `Data/Events/*.asset`, `Data/NPCs/*.asset`, 정의 DB 등록 |
| 대사 | `Assets/_Project/Resources/Dialogue/**/*.yarn` | Yarn 프로젝트 `ApoGirl.yarnproject` → `Dialogue` 문자열 테이블 |
| UI 문자열 | `Assets/_Project/Localization/Source/ko.json` | `UI`·`Actions`·`Items` 문자열 테이블 |

## 2. 작업 순서

1. JSON에 이벤트를 추가한다. 필드 이름은 C# 정의(`EventDef`, `ConditionDef`, `GameEffect`)와 같고, enum은 이름 문자열로 쓴다.
2. Yarn에 `title: 이벤트ID` 노드를 쓴다. 선택지는 `-> 문구 #choice:선택지ID`로 쓴다.
3. 메뉴 **ApoGirl/Content/Import Events**를 실행한다. 이 메뉴가 다음을 차례로 처리한다.
   - SO 생성
   - DB 등록
   - 대사 테이블 동기화
   - 검증
4. 오류가 있으면 콘솔과 `Temp/ApoGirlContentReport.txt`를 본다.
5. **ApoGirl/Content/Event Browser**에서 조건 충족 여부를 미리 본다. 플레이 중이면 실제 세이브를 기준으로 보고, 아니면 지정한 주차를 기준으로 본다.
6. UI 문자열을 추가했으면 **ApoGirl/Localization/Sync Tables**도 실행한다.

## 3. 이벤트 규칙

- **종류**
  - `Story`·`Calendar`·`Night`: 조건이 맞으면 우선순위(`Priority`)가 가장 높은 하나가 발생한다.
  - `Random`·`Crisis`: 확률(`RandomEventChance`)과 가중치로 뽑는다. 하루 1개, 주 3개까지다.
- **판정 시점**: 슬롯이 끝날 때마다 판정한다. 기준은 그 슬롯의 요일·낮밤·행동이다.
  - 밤 휴식 대화: `"Phase": "Night"`, `"ActionCategories": ["Rest"]`
  - 탐험 직후: `"ActionCategories": ["Expedition"]`
- **학사력**: `"Month"`·`"DayOfMonth"`로 지정한다. 1주차 1일은 3월 16일이다(`BalanceConfig.StartMonth/StartDayOfMonth`). 48주차가 2월이다.
- **1회성·반복**: `Once` 기본값은 true다. 반복 이벤트는 `"Once": false`와 `CooldownWeeks`를 함께 쓴다.
- **선택지**
  - `Condition`이 맞지 않으면 숨긴다. `Costs`를 낼 수 없으면 비활성으로 보인다.
  - 판정이 필요하면 `HasCheck` + `Check`를 쓴다. 실패하면 `OnFailure`가 적용된다.
  - 조건·비용 없는 선택지를 최소 하나 둔다. 없으면 검증기가 경고한다.
- **연쇄**: `NextEventId`의 이벤트가 조건과 관계없이 바로 이어진다.
- **NPC**: `Npcs: [{ NpcId, Affinity, Trust, SetStatus, SetRole }]`. `NPC_TEACHER`는 윤 선생님 호감이다.

## 4. Yarn 규칙

- 화자는 `윤 선생님: 대사`처럼 쓴다. 이름이 정해지지 않은 인물은 `???:`로 쓴다.
- 함수는 `Presentation/Dialogue/YarnFunctions.cs`의 `[YarnFunction]`에 있다.

  | 함수 | 결과 |
  |---|---|
  | `name()` | 주인공 이름 (기본 한서진) |
  | `given()` | 부르는 이름 (서진) |
  | `friend()` | 친구 호칭 |
  | `josa(단어, "이/가")` | 단어 + 알맞은 조사 |
  | `flag("ID")` | 플래그 여부 |
  | `stat("Intellect")` | 능력치 |
  | `resource("Food")` | 자원 |
  | `affinity("NPC_ID")` | NPC 호감 |
  | `week()` | 주차 |
  | `succeeded()` / `great_success()` | 방금 고른 선택지의 판정 결과 |

- **조사 쌍**: `이/가`, `은/는`, `을/를`, `과/와`, `아/야`, `으로/로`. 받침이 있을 때의 형태를 앞에 쓴다.
- **판정 결과에 따른 분기**: 선택지를 고르는 즉시 판정이 끝난다. 이어지는 대사에서 `<<if succeeded()>>`로 나눈다.
- **카드 대체**: Yarn 노드가 없는 이벤트는 카드로 보인다. 이때 UI 키 `event.{ID}.title/body`와 선택지 `TextKey`가 필요하다.

## 5. 테스트

- EditMode `ContentPipelineTests`가 실제 원본(JSON + Yarn)을 검증하고, 10주 헤드리스 진행을 확인한다.
- PlayMode `HubDialogue_RunsYarnNode_AndAppliesChoice`가 허브에서 대사 표시, 조사 처리, 선택 반영을 확인한다.
