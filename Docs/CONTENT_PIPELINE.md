# 콘텐츠 파이프라인 (M3)

이벤트·NPC는 **JSON(데이터·조건·효과)** 과 **Yarn(대사·선택지 문구)** 두 원본으로 만든다.
SO 에셋과 문자열 테이블은 생성물이라 직접 고치지 않는다.

## 1. 파일 위치

| 원본 | 위치 | 생성물 |
|---|---|---|
| 이벤트·NPC | `Assets/_Project/Content/Events/*.json`, `Content/Story/*.json` | `Data/Events/*.asset`, `Data/NPCs/*.asset`, 정의 DB 등록 |
| 행동·아이템·전리품·탐험 모델·적 (M4) | `Content/Base/*.json` | `Data/Actions`, `Data/Items`, `Data/Loot`, `Data/Expeditions`, `Data/Enemies` |
| 특성·시설·일기/코멘트·프롤로그 (M4) | `Content/Base/traits_facilities.json`, `Content/Story/lines.json`, `Content/Story/prologue.json` | `Data/StoryExtras.asset` |
| 탐험 지역 레이아웃 (M4) | `Scripts/Editor/LocationBuilder.cs` (메뉴 Build Locations) | `Data/Locations/*.asset` |
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

## 5. M4 추가 규칙

- **행동 해금**: `ActionDef.Unlock`(조건)이 맞지 않으면 팔레트에 안 보인다. 지역 해금은 플래그 `LOC_SCHOOL_DAY`, `LOC_STORE`, `LOC_PHARMACY`, 밤 탐험은 `NIGHT_RUNS`.
- **점진적 노출**: 이벤트가 `UI_STATS`, `UI_PROFICIENCY`, `UI_RESOURCES`, `UI_SLEEPDEBT`, `UI_DEFENSE` 플래그를 세우면 허브 패널이 열린다. 4주차부터는 습격·부채를 뺀 나머지가 자동으로 열린다.
- **효과 확장**: `Defense`, `Threat`, `Facilities`, `AddTraits`, `Proficiencies`, `TeacherMobility`, `RestorePower`, `RaidStrength`(이벤트에서 즉시 습격 판정).
- **조건 확장**: `Facility`, `Defense`, `Threat`, `TeacherMobility`, `Trait`, `Expeditions`(Key Day/Night), `ActionCount`, `IsBirthday`.
- **탐험 조우**: `LocationVariantDef.Encounters`(LocationBuilder). 고르면 플래그 `ENC_{조우}_{선택지}`와 선택지 플래그·단서가 결과로 돌아온다. 문구는 UI 키 `enc.*`.
  - M7: `SpriteKey`로 탐험 화면 그림을 정한다(비면 웅크린 사람, 이름에 NOTE·NOTICE가 있으면 종이).
  - M7: 헤드리스 탐험(봇·자동 탐험)도 조건이 맞는 조우를 탐험 모델의 `EncounterChance`(낮·밤 따로, 기본 0.5)로 만난다.
- **수혈 (M7, D16)**: 선생님은 B형. 조건 값 `BloodCompatible`(Key = 받는 사람 혈액형)은 주인공이 수혈할 수 있으면 1, 혈액형을 모르면 0. `Flag`(Key = 플래그)는 플래그가 켜져 있으면 1이라 `AnyChecks`에서 "수치 또는 플래그"를 표현할 때 쓴다.
- **일기·선생님 한마디**: `Lines`(Kind Diary/TeacherComment), 조건이 맞는 것 중 Priority가 가장 높은 것.
- **프롤로그**: Yarn `PROLOGUE_START`(선택지 태그 `#choice:문항.선택지`, 입력 `<<input name|contact|birthday|personality>>`) → 생활기록부 카드 → `PROLOGUE_AFTER`.

## 6. M5 엔딩 규칙

- **엔딩**: `Content/Story/endings.json`의 `Endings`.
  - 종류: Bad, Normal, Balanced, Specialized, Relationship, Narrative, Hidden
  - `Priority`, `Condition`, `Score`(ValueKind·Key·Weight 목록), `CutKeys`(후일담 문장 키), `Color`(배경색)
  - 제목은 UI 키 `ending.{Id}.title`.
- **판정**
  - 게임오버면 `EndCauses`가 종료 원인과 맞는 배드 엔딩을 고른다. 원인이 빈 배드 엔딩은 대체 엔딩이다.
  - 구조면 조건을 만족한 엔딩 중 우선순위, 그다음 점수로 고른다.
  - 원인·조건 없는 배드 엔딩 1개와 조건 없는 구조 엔딩 1개가 꼭 있어야 한다(검증 도구가 확인).
- **종료 원인**: `END_CAUSE_EXPEDITION`, `END_CAUSE_RAID`, `END_CAUSE_STARVATION`, `END_CAUSE_COLLAPSE`(스트레스 `CollapseStress` 이상인 주가 `CollapseWeeks` 연속), `END_CAUSE_HP_ZERO`(그 밖).
- **공통 레이어**: `Lines`의 Kind `EndingTeacher`·`EndingFriend`·`EndingCure`·`EndingTendency`.
  - 구조 엔딩의 후일담 뒤에 종류마다 하나씩 붙는다.
  - `EndingMoment`는 조건 맞는 줄 전부가 "1년의 기록"의 주요 장면이 된다.
- **2~4막이 세울 플래그**(M6)
  - 선생님: `TEACHER_CURED`, `TEACHER_LAST_LESSON`, `TEACHER_ISOLATED`, `TEACHER_EXPOSED`, `TEACHER_LEFT_BEHIND`
  - 구조 거부 `CHOSE_TO_STAY`
  - 가해자 `BULLY_FORGAVE`, `BULLY_SEPARATED`, `BULLY_RESCUED`
  - 잠긴 집 `LEFT_SUPPLIES_LOCKED_HOUSE`
  - 교육 태그 `TAG_TEACHING`
  - 단서 접두어: `CURE_`(치료), `TRUTH_`(진실)
- **조건 확장**: `NightRatio`, `DiaryCount`, `Companions`, `StatSpread`, `StatMin`, `BreakdownWeeks`, `ClueCount`(Key = 접두어).
- **문장 치환**(후일담·기록): `{name}`, `{given}`, `{friend}`, 조사는 `{given:은/는}`처럼 쓴다.

## 7. M6 규칙

- **감염**
  - 감염도(`Vital Infection`)는 화면에 숫자로 보이지 않는다. 25·50·75에서 증상 문장(미열·기침·손떨림)이 나온다.
  - 일기·선생님 한마디는 조건 `Vital Infection ≥ N`으로 쓴다.
- **선생님 재감염**
  - 효과 `TeacherReinfect: true`로 발병하고, 플래그 `TEACHER_REINFECTED`가 선다.
  - 진행도는 효과 `TeacherInfection`으로 바뀐다(돌봄은 −).
  - 조건은 `TeacherInfection`(0~100), `TeacherStage`(25마다 1단계).
- **날씨**: 조건 `Weather`(Key = Clear/Rain/Heat/Cold, Op Equal, Value 1). 계절표는 `BalanceConfig.WeatherBySeason`.
- **구조 카운트다운**: 플래그 `RESCUE_ANNOUNCED`를 세우면 허브에 "구조까지 N주"가 뜬다. 조건은 `WeeksToRescue`.
- **시설 보정**: `Facilities[].Modifiers`(특성과 같은 형식, 레벨마다 곱해짐).
  - 보정 종류 추가: `RaidGuard`, `InfectionResist`, `TeacherCare`.
- **동료 역할**: `Roles`(Id, Modifiers). 합류한 NPC에게 허브에서 지정한다. 조건은 `RoleCount`(Key = 역할 ID).
- **정찰**: 플래그 `SCOUT_{지역 ID}`가 있으면 출정 준비 화면에 잠긴 방·조우 수가 나오고, 출정하면 사라진다.
- **완치 판정**: 이벤트 선택지 `HasCheck` + `Check.BaseRate`로 확률을 둔다(4막 0.3). 성공 효과에 `TEACHER_CURED`를 세우면 병세 진행이 멈춘다.
- **회차 요소**
  - `StartBonuses`(Id, Effect): 엔딩을 하나라도 본 뒤 새 게임 시작 때 고른다. 문자열은 `bonus.{Id}.name/desc`.
  - 2회차 새 게임에는 플래그 `NG_PLUS`가 서고, `BalanceConfig.NgPlusOpeningEventId`(창고 안의 7일)가 1막 시작 이벤트보다 먼저 나온다.
- **같은 대본 공유**: 이벤트의 `DialogueNode`에 다른 이벤트의 노드 이름을 적으면 그 대본을 쓴다(예: 재감염 조기 발병).
- **막 평가 카드**: 이벤트에 `ReviewAct`(막 번호)·`ReviewFromWeek`(그래프 시작 주)를 두면, 이벤트가 끝난 뒤 그래프와 한마디 카드가 뜬다.
  - 한마디는 `Lines`의 Kind `ActReview`.

## 8. 테스트

- EditMode `ContentPipelineTests`가 실제 원본(JSON + Yarn)을 검증하고, 10주 헤드리스 진행을 확인한다.
- PlayMode `HubDialogue_RunsYarnNode_AndAppliesChoice`가 허브에서 대사 표시, 조사 처리, 선택 반영을 확인한다.
- 콘텐츠를 바꾼 뒤 밸런스는 봇 시뮬레이터로 본다: 메뉴 `ApoGirl/Balance/Run Bot Simulation` → `Temp/BotSim/summary.md`. 새 행동·지역을 만들면 봇 전략(`Assets/_Project/Balance/bot_strategies.json`의 묶음)에도 넣는다. 자세한 내용은 [M7_PLAN.md](M7_PLAN.md).
- 적대적 생존자: 적 정의에 `"Kind": "Survivor"`, `SeesHiddenWithin`(숨은 사람 찾는 거리), `PatrolRange`, `CanBeDowned: false`. 지역 변형 `HostileChanceMultiplier`(0이면 안 나옴)·`HostileSpots`(시작 위치 후보)는 `Editor/LocationBuilder.cs`.
- 상자 고갈: 지역 `DepletingContainers = true`(LocationBuilder) + 헤드리스 모델 `"ContainersPerRun"`(items_loot.json 등의 ExpeditionModels).
- 행동 효과 `"TraumaCare": n` = 폭행 트라우마 회복을 n주 앞당긴다.
- 탐험 상자: 모든 지역 기본 고갈(`DepletingContainers`), 매달 복구는 `Restocks`(교실동 false). 처음부터 빈 상자 비율은 `BalanceConfig.BaseEmptyContainerRate`. 복구 뒤 첫 방문 문장은 문자열 `exp.restocked.{지역ID}` — 새 지역을 만들면 같이 추가.
- 낮 침입 이벤트는 `EVT_DAY_INTRUSION`(선택지 효과 `"IntrusionResponse": "Fight" | "Hide" | "Yield"`). 조건은 `SYSTEM_ONLY`라 일정 판정으로만 뜬다.
- 행동 효과 `"Traps": n`(함정), 조건 값 `Traps`(설치한 함정 수).

