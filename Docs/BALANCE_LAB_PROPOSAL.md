# 밸런스 표 (Balance Lab) 기획서

> 2026-11-23 초안. M7(밸런싱 & 봇 테스트) 도구.
> 게임 안에서 중요 수치를 표로 보고 바로 고치고, 그 자리에서 봇을 돌려 결과를 `Docs/BOT_ENDING_LOG.md`에 이어 적는다.
> 느낌은 "삼국지 파워업 키트의 에디트 창"이다. 칸을 눌러 숫자를 바꾸면 게임이 곧바로 그 값으로 돈다.

**진행 (2026-11-23)**
- D-1 덮어쓰기 파일 · D-2 에디터 + 개발 빌드 · D-3 단추로 기록: 추천안대로 확정.
- L1·L2 완료: 허브 F1 → "밸런스 표". 탭 ①~⑧(탐험 공통 탭을 따로 둠), 검색, 칸 고치기(Enter·휠·Shift+휠), 바뀐 칸 노란색, 전부 되돌리기, "언제부터".
  - 코드: `Core/Balance/BalanceLab.cs`(칸 모델), `Systems/Content/BalanceLabJson.cs`, `Presentation/Screens/BalanceLabView.cs`.
  - 데이터: `Resources/Balance/balance_lab_layout.json`, 절 이름 `Resources/Balance/balance_sections.json`(Sync Balance Asset이 만든다).
  - 정의 DB는 이제 밸런스·탐험 모델·전리품 표를 사본으로 넘긴다(`DefinitionDatabaseAsset.Build`). 실행 중 고친 값이 에셋에 남지 않는다.
- 아직: 탐험 중 여는 키, 상자 전리품 표 펼치기, 일괄 배율 단추·도움 지표(L2 나머지), L3~L6.

---

## 1. 왜 필요한가

지금 밸런스를 바꾸는 길은 길다.

1. `BalanceConfig.cs` 기본값이나 `Content/Base/*.json`을 고친다.
2. 컴파일하고 `Sync Balance Asset`·`Import Events`를 누른다.
3. 메뉴 `Run Bot Simulation`을 돌린다.
4. `Temp/BotSim/summary.md`를 열어 엔딩 분포를 읽는다.
5. `BOT_ENDING_LOG.md`에 손으로 옮겨 적는다.

수치 421개가 1,000줄짜리 파일 한 곳에 흩어져 있다. 탐험 수확은 지역 JSON 여러 개에 나뉘어 있다. 무엇이 중요한지, 지금 값이 얼마인지 한눈에 보이지 않는다.

밸런스 표는 이 다섯 단계를 **한 창 안의 한 흐름**으로 줄인다.

> 표에서 고친다 → 바로 해 본다 → 봇을 돌린다 → 이전과 비교한다 → 기록한다

---

## 2. 화면

```
┌ 밸런스 표 ───────────────────────────────────────── [저장] [되돌리기] [닫기] ┐
│ [자원·소모][탐험 수확][스트레스·수면][부상·감염][위협][동료][난이도][전체]  🔍 │
├──────────────────────────────────────────────────────────────────────────────┤
│ 이름              │ 원래 │ 지금 │ 단위   │ 언제부터   │ 설명                   │
│ 하루 식량 (2인)   │   2  │ [3]  │ 개/일  │ 즉시       │ DailyFood              │
│ 겨울 식량 배율    │ 1.3  │ 1.3  │ ×      │ 즉시       │ WinterFoodMultiplier   │
│ 식량 부패 문턱    │  60  │  60  │ 개     │ 다음 결산  │ FoodSpoilageThreshold  │
│ …                                                                           │
├ 바뀐 값 2개 ─────────────────────────────────────────────────────────────────┤
│ DailyFood 2→3 · RaidBaseChance 0.15→0.12                                     │
├ 봇 ──────────────────────────────────────────────────────────────────────────┤
│ 전략 ☑DAY ☑NIGHT ☑MIXED ☑EDU ☑CARE  판 [5|40]  난이도 [표준▾]  [▶ 봇 돌리기] │
│ ████████████░░░░ 120/200                                                     │
│ 묶음     전체   DAY   NIGHT MIXED EDU  CARE   목표     지난번 대비            │
│ 배드     48%▲6  40%  80%   20%   22%  75%    45~55   ▲ 굶주림 사망 +9        │
│ 기본     22%▼4  …                                                            │
│                                         [기록에 붙이기 → BOT_ENDING_LOG #13] │
└──────────────────────────────────────────────────────────────────────────────┘
```

- **위:** 탭이 표 하나씩이다.
- **가운데:** 칸 표다.
- **아래 두 띠:** "바뀐 값" 목록과 봇 패널이다.
- **바뀐 칸:** 노란색으로 칠한다. 원래 값은 왼쪽 칸에 늘 보인다.
- **숫자 칸:**
  - 직접 입력한다.
  - 마우스 휠로 한 단계씩 바꾼다. 단계는 수치마다 정한다.
  - Shift + 휠은 10단계씩 바꾼다.
- **여러 칸 한꺼번에:** 골라서 "×1.1 / ×0.9"를 누르면 함께 바꾼다. 예: 지역 8곳 식량 수확을 다 같이 10% 올린다.
- **"언제부터" 칸:** 바꾼 값이 게임에 언제 먹는지 알려 준다 (5절).

---

## 3. 여는 곳

| 어디서 | 어떻게 | 비고 |
|---|---|---|
| 게임 실행 중 (허브) | F1 디버그 패널 → **"밸런스 표"** 단추 | 요청의 핵심 |
| 게임 실행 중 (탐험) | 디버그 키로 연다 (빈 키 하나, 예: F10) | 탐험 수치를 바꾸며 바로 걸어 본다. 열린 동안 시계를 멈춘다 |
| 에디터 (플레이 안 함) | 메뉴 `ApoGirl/Balance/Balance Lab` | 같은 화면을 에디터 창으로 띄운다. 플레이 없이 고치고 봇만 돌릴 때 |

- 화면은 UI Toolkit 한 벌이다. 게임 위 덧창과 에디터 창이 같은 화면을 쓴다.
- 창이 열린 동안 허브 입력(Tab·Esc·단축키)은 막는다.

---

## 4. 표 (탭) 구성

어떤 수치를 어느 탭에 어떤 이름으로 보일지는 **코드가 아니라 데이터**로 둔다: `Assets/_Project/Resources/Balance/balance_lab_layout.json` (CLAUDE.md 작업 원칙 3).

- 수치마다 적는 것: 필드 이름, 한국어 이름, 단위, 한 칸 단계, 최소·최대, 설명 한 줄.
- 여기 없는 수치는 "전체" 탭에서 자동으로 보인다.
- 아래는 첫 배치 제안이다. 우리가 지금까지 다룬 수치 위주다.

### ① 자원·소모

| 무엇 | 수치 |
|---|---|
| 하루 소모 | `DailyFood`·`DailyWater` |
| 포만·수분 | 하루 감소 `SatietyDailyDrain`·`HydrationDailyDrain`, 한 끼 회복 `MealSatiety`·`DrinkHydration` |
| 겨울 | `WinterFoodMultiplier`·`WinterWeeklyFuel`·`NoHeatStress` |
| 부패 | `FoodSpoilageThreshold`·`FoodSpoilageRate`, 냉장고 `Appliances` |
| 거점 생산 | `WeeklyRawWater`·`RainCollectWater`·`RainBarrelWater`·`RoofGardenFood` |
| 고양이 | `CatFoodPerWeek` |
| 시작 자원 | `InitialResources` |

### ② 탐험 수확 (드랍률)

**지역 × 낮/밤 행렬**이 이 탭의 중심이다. 원본은 `ExpeditionModels`(`items_loot.json`·`act{2,3,4}_world.json`)이고, 봇과 헤드리스 탐험이 쓰는 값이다.

| 지역 | 낮 식량 | 낮 물 | 낮 의약 | 낮 자재 | 쓰러짐 | 피로 | 스트레스 | 밤 식량 | … |
|---|---|---|---|---|---|---|---|---|---|
| 편의점 | 6~9 | 4~6 | 0~1 | 1~2 | 8% | 8 | 4~6 | … | |

- 칸은 `최소~최대` 두 숫자다.
- 상자 전리품 표 `LootTables`(실제로 걸어 다니는 탐험의 상자)는 지역 줄을 펼치면 아래에 보인다.
  - 빈 상자 확률 `EmptyChance`
  - 뽑기 횟수 `MinRolls~MaxRolls`
  - 아이템별 가중치. 옆에 "실제 확률 %"를 계산해 함께 보인다.
- 공통 수치:

  | 무엇 | 수치 |
  |---|---|
  | 상자 | `BaseEmptyContainerRate`·`RestockFraction` |
  | 깊은 곳 | `HeadlessDeepChance`·`HeadlessDeepItemChance` |
  | CD·옥상 | `HeadlessCdChance`·`HeadlessRooftopChance` |
  | 동료 외출 | `OutingYieldShare`·`OutingHaul`(종류별 비중) |
  | 난이도 배율 | `FoodYield`·`WaterYield`·`MedicineYield` (⑦에서 고치고, 여기서는 읽기만) |

### ③ 스트레스·피로·수면

| 무엇 | 수치 |
|---|---|
| 강제 휴식 위반 | `ViolationFatigue`·`ViolationStress` |
| 수면 부채 | `SleepDebtEfficiencyPenalty`(단계별 배열)·`NightExpeditionSleepDebt`·`NightRestRepayAmount` |
| 스트레스 한계 | `StressThresholdBase`·`StressThresholdPerWillpower` |
| 정신 붕괴 | `CollapseStress`·`CollapseWeeks` |
| 포만·수분 단계 | `SatietyBands`·`HydrationBands` (단계별 체력·스트레스) |
| 날씨 | `ColdDailyStress`, 비 `RainNoiseBonus` |
| 탐험 시간 | `SearchTimeMultiplier`·`StressSearchThreshold` |

### ④ 부상·감염·청결

| 무엇 | 수치 |
|---|---|
| 부상 | 문턱 `InjuryHpFraction`, 치료 의약품 `InjuryMedicineCost` |
| 상처 감염 | `WoundInfectionBase`·`WoundInfectionDirty`·`WoundCleanThreshold` |
| 청결 | `CleanlinessWeeklyDecay`·`StartingCleanliness` |
| 물림 | `BiteChanceOnDownDay`·`BiteChanceOnDownNight`·`BiteInfection` |
| 응급 처치 | `FirstAidBaseChance`·`FirstAidPerMedicineLevel` |
| 감염 진행 | `InfectionWeeklyGrowth`·`InfectionGrowthMin` |
| 선생님 재감염 | `TeacherInfectionWeekly`·`TeacherInfectionColdBonus` |
| 동료 다침 | `OutingInjuryChance`·`OutingScoutInjuryChance`·`OutingSevereShare` |
| 맞았을 때 | `BeatenHpLoss`·`BeatenStress` |

### ⑤ 위협 (습격·사람·침입)

| 무엇 | 수치 |
|---|---|
| 습격 | `RaidStartWeek`·`RaidBaseChance`·`RaidChancePerPoint`·`RaidMaxChance`·`RaidBasePower`, 돌파 피해 `RaidBreach*` |
| 위협도 | `ThreatPerDayExpedition`·`ThreatPerNightExpedition`·`ThreatWeekly` |
| 방어 | `TrapPower`·`AlarmPower`·`MaxTraps` |
| 적대적 생존자 | `HostileChanceByAct`·`HostileChancePerExposure`·`HeadlessHostileCaught*` |
| 낮 침입 | `IntrusionWeeklyChanceAct3·4`·`IntrusionStrength`·`IntrusionHide*` |
| 배율 | 방송 `BroadcastRaidMultiplier`, 비 `RainRaidMultiplier`, 고양이 `CatRaidDamageScale` |

### ⑥ 동료·관계

| 무엇 | 수치 |
|---|---|
| 함께한 시간 | `TogetherAffinity`·`TogetherWeeklyCap` |
| 소홀 | `NeglectWeeks`·`NeglectAffinity` |
| 위기 이탈 | `CrisisLeaveBelowAffinity`, 대신 다침 `RaidCover*` |
| 동료 외출 | `OutingExtraChance`·`OutingDailyCap`·`OutingStress` |
| 역할 | `RoleCapacity`·`RoleExtraRoomBonus`·`RoleAptitudeMultiplier` |
| 스트레스 | `CompanionStressRoleThreshold`·`CompanionStressedRoleMultiplier` |
| 동아리 | `ClubAffinity`·`ClubLikedAffinity` |
| 방송 | `BroadcastReplyChance` |

### ⑦ 난이도

- `Difficulties`를 행렬로 보인다. 줄은 난이도(이야기·표준·하드), 칸은 배율이다.
- 배율: `Consumption`·`Harm`·`HumanThreat`·`Infection`·`Yield`·`FoodYield`·`WaterYield`·`MedicineYield`.

### ⑧ 전체 (고급)

- `BalanceConfig`의 모든 숫자 필드를 `BalanceConfig.cs`의 절(`// ── … ──`) 순서대로 보인다. 지금 421개다.
- 위 검색 칸으로 이름(영어·한국어)을 찾는다.
- 배열·표 필드는 한 칸을 눌러 펼쳐 고친다. 예: `GrowthBands`·`WeatherBySeason`·`Disasters`.

### 칸 옆 "지금 이 값이면" (도움 지표)

몇몇 탭은 맨 위에 계산값 한 줄을 띄워, 숫자가 실제로 무슨 뜻인지 보인다.

- **자원:** "2인 기준 한 주 식량 소모 14 · 겨울 18 · 지금 거점 식량으로 3.2주"
- **탐험 수확:** "평균 낮 탐험 한 번 = 식량 7.5 · 물 5.0 · 의약 0.4"
- **위협:** "위협도 40일 때 습격 확률 21% · 방어 30이면 돌파 확률 35%"

---

## 5. 실시간 반영 — 무엇이 언제 먹나

- 게임 실행 중에 고친 값은 그 자리에서 **지금 판의 정의 데이터**(`GameSession.Definitions`)에 들어간다.
- 세이브 파일에는 들어가지 않는다. 세이브는 상태만 담는다.
- 다만 이미 굴려 둔 것은 거꾸로 바뀌지 않는다. 그래서 칸마다 "언제부터"를 적는다.

| 표시 | 뜻 | 예 |
|---|---|---|
| 즉시 | 다음 판정부터 쓴다 | 소모량, 판정 확률, 탐험 걸음·소음 |
| 다음 주 | `PrepareWeek`에서 굴린다 | 날씨, 동료 외출 일정, 습격 확률 |
| 다음 탐험 | 탐험을 만들 때 읽는다 | 상자 전리품, 깊은 곳 |
| 새 게임 | 시작할 때만 쓴다 | 시작 자원, 시작 능력치, 시작 청결 |

- "언제부터"는 `balance_lab_layout.json`에 적는다.
- 확실하지 않은 수치는 "다음 주"로 둔다. 만들면서 하나씩 확인한다.
- 이번 주에 이미 굴린 것(날씨·외출 일정)을 고친 값으로 다시 굴리는 디버그 단추 "이번 주 다시 굴리기"를 함께 만든다. 지금은 없는 기능이다.

---

## 6. 봇 돌리기

### 6.1 실행

- **설정:** 전략 체크, 판 수, 난이도, 시드다.
  - 판 수: 빠름 5 / 정식 40, 또는 직접 입력.
  - 시드 기본값은 `bot_strategies.json` `BaseSeed`다.
- **어떤 값으로 도나:** 누른 순간 **지금 표의 값으로 사본**을 만들어 그 사본으로 돈다. 그래서 봇이 도는 동안 표를 더 고쳐도 그 실행은 흔들리지 않는다.
- **어떻게 도나:** 시뮬레이션 코어(`ApoGirl.Core`, Unity 없음)는 그대로 쓰고, **뒤 스레드**에서 돈다.
  - 게임 화면은 멈추지 않는다.
  - 진행 막대와 취소 단추가 있다.
  - 지금 메뉴 `Run Bot Simulation`과 같은 `BotSimulator.Run`·`BotReport.Build`를 부른다.
- **결과 파일:** 지금처럼 `Temp/BotSim/`에 요약·CSV를 쓴다. 메뉴로 돌린 것과 같은 파일이다.

### 6.2 결과 보기

| 표 | 내용 |
|---|---|
| 묶음 × 전략 | 기본 · 특화 · 관계·서사 · 히든 · 배드 비율 (`BotReport.EndingTargets` 묶음) |
| 목표 띠 | 목표 범위 안이면 초록, 벗어나면 붉은색 |
| 지난번 대비 | 같은 창에서 마지막으로 돌린 결과와의 차이 ▲▼. 창을 닫아도 `Temp/BalanceLab/last_run.json`에 남는다 |
| 참고 지표 | 사망 원인, 주당 동료 외출, 동시 부상 등 지금 `summary.md`에 있는 것 |

CLAUDE.md 원칙대로 결과는 **전략별 엔딩 확률 + 방향성**만 앞에 보인다. 세부 지표는 접어 둔다.

### 6.3 기록에 붙이기 → `BOT_ENDING_LOG.md`

[기록에 붙이기]를 누르면 기록 문서의 형식대로 새 절 초안을 만들고, 편집 칸에 띄운다.

```
## #13 · 2026-11-23 · (제목 — 직접 입력)

**바뀐 것**
- DailyFood 2 → 3 (하루 식량)
- RaidBaseChance 0.15 → 0.12 (습격 기본 확률)
- (메모 — 직접 입력)

**봇 설정:** 5전략 × 40판, 표준 난이도, 시드 1000~1039, 코드 `66a72215` + 밸런스 표 수정 2개.

| 묶음 | 전체 | DAY_ONLY | … |
…

참고 지표: …

**방향성**
- (자동 초안) 배드 42% → 48%. NIGHT_ONLY·CARE에서 가장 크게 움직였다.
- (직접 입력) 마지막 조정 때 볼 것: …
```

- 번호(#13)는 문서의 마지막 번호 다음 수다.
- "바뀐 것"은 기본값 대비 바뀐 수치에서 자동으로 채운다. 바로 앞 기록 대비가 아니다.
- "방향성"의 첫 줄은 지난번 대비 숫자로 초안만 만든다. 해석은 사람이 쓴다.
- 확인을 누르면 문서 맨 아래에 붙인다. 예전 기록은 건드리지 않는다는 문서 규칙을 지킨다.
- 커밋은 하지 않는다. 지금처럼 사용자 요청이 있을 때 한다.

---

## 7. 저장 — 고친 값을 어디에 남기나

### 7.1 `BalanceConfig` 수치 — `[결정 필요]` D-1

지금 규칙은 "밸런스 수치 원본은 `BalanceConfig.cs`, 에셋은 가져오기 때 동기화"다. 표에서 고친 값을 어디에 두느냐가 갈림길이다.

| 안 | 방식 | 좋은 점 | 나쁜 점 |
|---|---|---|---|
| **A. 덮어쓰기 파일 (추천)** | `Assets/_Project/Balance/balance_overrides.json`에 **바뀐 값만** 적는다. 코드 기본값 위에 얹는다. 동기화·런타임 로드·봇이 모두 이 파일을 거친다 | 고친 것만 짧게 남아 diff가 잘 보인다. 배열·표 필드도 쉽다. [기본값으로] 한 번에 되돌린다 | 원본이 둘(코드 + 덮어쓰기)이 된다. 조정이 끝나면 "코드로 굳히기"(덮어쓰기 → `BalanceConfig.cs` 반영)를 한 번 한다 |
| B. 코드 직접 고치기 | 저장할 때 `BalanceConfig.cs`의 기본값 숫자를 바꿔 쓴다 | 원본이 하나다 | 배열·객체 필드(`Difficulties`·`SatietyBands` 등)는 고쳐 쓰기 어렵다. 저장마다 컴파일·도메인 리로드가 일어나 플레이가 끊긴다 |
| C. 에셋을 원본으로 | `BalanceConfig.asset`이 원본이 되고 코드는 처음값만 | 유니티다운 방식이다 | 지금 규칙을 뒤집는다. "Sync"가 고친 값을 덮어쓰는 사고가 난다 |

**추천 A.**

- 덮어쓰기는 `BalanceConfig`의 필드 이름을 키로 쓴다.
- 이름이 바뀌거나 사라진 키는 열 때 경고하고 버린다.

### 7.2 탐험 수확·전리품 (콘텐츠 JSON)

- 이것은 원래 데이터가 `Content/Base/*.json`이니, 그 파일에 그대로 쓴다.
- 줄 바꿈·들여쓰기는 원본 모양을 지킨다. 바뀐 숫자만 글자 단위로 바꾼다. 이전에 ko.json 통째 재작성으로 형식이 망가진 일이 있었다.
- 쓴 뒤 `Import Events`를 자동으로 부른다.

### 7.3 저장 단추의 동작

| 단추 | 동작 |
|---|---|
| 저장 | 덮어쓰기 파일 + 콘텐츠 JSON에 쓴다. 에디터에서만 켜진다 |
| 되돌리기 | 이번에 연 뒤 바꾼 것만 되돌린다 |
| 기본값으로 | 덮어쓰기 파일을 비운다 |
| 내보내기 | 지금 바뀐 값을 `persistentDataPath/BalanceLab/overrides_날짜.json`으로 쓴다. 빌드에서 고친 값을 에디터로 가져올 때 쓴다 |

---

## 8. 어디까지 열어 둘까 — `[결정 필요]` D-2

| 안 | 범위 |
|---|---|
| **가. 에디터 + 개발 빌드 (추천)** | 에디터는 모든 기능을 쓴다. 개발 빌드(테스트 빌드)는 보기·실시간 수정·봇 실행·내보내기만 하고, 저장과 기록 문서 쓰기는 없다. 정식 빌드에는 넣지 않는다 |
| 나. 에디터만 | 가장 단순하다. 빌드한 게임에서는 못 쓴다 |
| 다. 모든 빌드 | 플레이어도 쓸 수 있다. 숨긴 키가 새면 게임이 망가지니 권하지 않는다 |

- 기록 문서는 저장소 안의 파일이라 빌드에서는 쓸 곳이 없다.
- 빌드에서 돌린 봇 결과는 `persistentDataPath/BalanceLab/`에 같은 형식의 초안으로 남긴다.

---

## 9. 기록 시점 — `[결정 필요]` D-3

| 안 | 방식 |
|---|---|
| **가. 단추를 눌러야 기록 (추천)** | 봇은 마음껏 돌리고, 의미 있는 실행만 [기록에 붙이기]로 남긴다. CLAUDE.md "큰 수정마다" 기록과 맞는다 |
| 나. 돌릴 때마다 자동 기록 | 빠뜨림이 없다. 대신 빠른 5판 실험까지 쌓여 문서가 지저분해진다 |

---

## 10. 만드는 순서

| 단계 | 내용 | 끝났다고 보는 것 |
|---|---|---|
| L1 표 보기 | 레이아웃 데이터 + 탭 ①~⑦ 읽기 전용, ⑧ 전체 자동 목록, 검색 | F1에서 열려 지금 값이 보인다 |
| L2 실시간 수정 | 칸 고치기·휠·일괄 배율·되돌리기, "언제부터" 표시, 도움 지표 | 하루 식량을 바꾸면 그날 밤 소모가 바뀐다 |
| L3 저장 | 덮어쓰기 파일(D-1 결정대로), 콘텐츠 JSON 글자 단위 쓰기 + 가져오기 | 에디터를 껐다 켜도 값이 남는다. git diff가 고친 줄만 보인다 |
| L4 봇 | 사본 + 뒤 스레드 실행, 진행·취소, 결과 표·목표 띠·지난번 대비 | 게임을 멈추지 않고 5판이 돈다. 메뉴로 돌린 결과와 같은 시드면 같은 숫자가 나온다 |
| L5 기록 | `BOT_ENDING_LOG.md` 새 절 초안·편집·붙이기 | 문서 형식 그대로 #N이 붙는다 |
| L6 에디터 창 | 같은 화면을 `ApoGirl/Balance/Balance Lab` 창으로 | 플레이 없이 고치고 봇을 돌린다 |

- L1~L2만으로도 플레이하며 바로 손볼 수 있다. 여기서 한 번 써 보고 탭 구성을 다듬은 뒤 L3 이후로 간다.
- 각 단계는 EditMode 테스트로 확인한다.
  - 덮어쓰기 적용·되돌리기
  - 봇 사본 독립성: 실행 중 표를 고쳐도 결과가 같다
  - 기록 초안 형식

---

## 11. 만드는 법 (요약)

| 자리 | 이름 | 하는 일 |
|---|---|---|
| Core | `BalanceOverrides` | 필드 이름 → 값. 적용·차이·사본 만들기 (UnityEngine 없음) |
| Core | `BalanceSchema` | 레이아웃 데이터 + 리플렉션으로 필드 목록·절·종류 |
| Core | `BotBatch` | 사본 DB로 `BotSimulator.Run` 여러 번, 진행 알림·취소 |
| Presentation | `BalanceLabView` | UI Toolkit 화면 (`MultiColumnListView`), 탭·칸·봇 패널 |
| Presentation | `BalanceLabOverlay` | 게임 위 덧창, 디버그 패널·F10에서 연다 |
| Editor | `BalanceLabWindow` | 같은 화면을 에디터 창으로 |
| Editor | `BotLogWriter` | `BOT_ENDING_LOG.md` 다음 번호·초안·붙이기 |
| Editor | `ContentJsonPatcher` | 콘텐츠 JSON 숫자만 글자 단위로 바꾸기 |
| 데이터 | `balance_lab_layout.json` | 탭·이름·단위·단계·범위·언제부터 |
| 데이터 | `balance_overrides.json` | 바뀐 값 (D-1이 A일 때) |

- 화면 문자열은 `ko.json`에 둔다.
- 수치 이름(한국어)은 레이아웃 데이터에 둔다.

---

## 12. 정해 주실 것

1. **D-1 저장 방식:** A 덮어쓰기 파일 (추천) / B 코드 직접 / C 에셋 원본
2. **D-2 범위:** 가 에디터 + 개발 빌드 (추천) / 나 에디터만 / 다 모든 빌드
3. **D-3 기록 시점:** 가 단추로 (추천) / 나 자동
4. **탭 구성:** 4절 ①~⑧에서 빼거나 더할 수치가 있으면 알려 주세요. 특히 "우리가 자주 만진" 수치 가운데 빠진 것이 있는지 봐 주세요.
