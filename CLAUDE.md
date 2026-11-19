# CLAUDE.md — 〈마지막 학기 (가제)〉 Unity 프로젝트

## 기준 문서
- 마스터 프롬프트(기획·개발 지침): **`C:\Users\phiti\Desktop\MASTER_PROMPT.md`가 원본**이다.
  `I:\workshop\apogirl\MASTER_PROMPT.md`는 원본을 복사한 사본이며, 원본이 바뀌면 다시 복사한다.
- 작업 전 마스터 프롬프트를 읽는다. 부록 C(결정 기록, D1~)의 확정 사항은 다시 논의하지 않는다.
- 코드 규칙: [Docs/CODING_CONVENTIONS.md](Docs/CODING_CONVENTIONS.md)

## 작업 원칙
1. 한 번에 한 마일스톤(마스터 프롬프트 12장)만 진행하고, 범위를 임의로 넓히지 않는다.
2. 코드를 쓰기 전에 설계 요약(3~10줄)을 먼저 제시하고, 큰 구조 변경은 승인을 받는다.
3. 수치·텍스트·이벤트는 하드코딩하지 않고 데이터(ScriptableObject / JSON / Yarn 스크립트)로 둔다.
4. `[결정 필요]` 항목은 추측하지 말고 선택지 2~3개와 추천안을 제시한다.
5. 콘텐츠는 마스터 프롬프트 13장(콘텐츠 안전 가이드라인)을 반드시 지킨다.
6. 응답은 한국어. 코드 식별자는 영어, 주석은 한국어.

## Git
- 로컬 커밋만 한다. **사용자가 요청하기 전까지 push 하지 않는다.**

## 현재 단계
- M0, M1, M2, M2b, M3, **M4(버티컬 슬라이스: 0막 프롤로그 + 1막 13주, 지역 3곳+튜토리얼, 로드아웃, 밤 습격, 도현 아크, 1막 평가) 구현 완료** → 사용자 플레이 확인 후 M5(엔딩 시스템). 계획·결정: [Docs/M4_PLAN.md](Docs/M4_PLAN.md).
- 콘텐츠 작성법: [Docs/CONTENT_PIPELINE.md](Docs/CONTENT_PIPELINE.md). 원본은 `Content/**/*.json`(행동·아이템·전리품·탐험 모델·적·특성·시설·이벤트·NPC·일기/코멘트·프롤로그) + `Resources/Dialogue/**/*.yarn` → 메뉴 `ApoGirl/Content/Import Events`. 지역 레이아웃은 `Editor/LocationBuilder.cs` → 메뉴 `ApoGirl/Content/Build Locations`.
- 1막 밸런스 점검: EditMode `Act1BalanceTests`가 세 전략으로 13주를 돌려 `Temp/ApoGirlAct1Balance.txt`에 기록.
- 주 진행 중 이벤트: `GameSession.Step()`/`CompleteExpedition()`이 `AwaitingEvent`를 돌려주면 대화 → `ResolveEvent(choiceId)`. 허브 F1 디버그 패널에서 이벤트 강제 발생·자동 진행 전환.
- 코어 진입점: `NewGameFactory.Create` → `WeekRunner`(`PrepareWeek` → `ExecuteNextSlot` × 14 → `FinishWeek`), 정의 DB는 `Assets/_Project/Data/DefinitionDatabase.asset`.

## Unity
- Unity 6000.6, 2D URP. Unity MCP(`.mcp.json`, 서버 이름 `unity`)로 에디터와 연결된다.
- 시뮬레이션 코어(`ApoGirl.Core`)는 UnityEngine을 참조하지 않는다(asmdef `noEngineReferences`).
- 테스트: Unity Test Runner → EditMode (`ApoGirl.Tests.EditMode`), PlayMode (`ApoGirl.Tests.PlayMode`). 결과 요약은 `Temp/ApoGirlTestResults.txt`에도 기록된다.
- UI: UI Toolkit (`Assets/_Project/UI`: Title.uxml, Hub.uxml, Theme.uss, PanelSettings). 화면 문자열은 `Assets/_Project/Localization/Source/ko.json`이 원본 → 메뉴 `ApoGirl/Localization/Sync Tables`로 테이블 생성.
- 실행: Boot 씬에서 플레이 → 타이틀 "새 게임 (프롤로그)" 또는 "빠른 시작". 허브 F1 = 디버그 패널(지역·시설 전부 해금, UI 전부 보기, 이벤트 강제 발생). 탐험: Space 숨기, Q 무기 공격(무기 없으면 밀치기), 1~4 퀵슬롯, F 손전등(도구 필요), 문 앞 W 살피기·S 듣기, 디버그 F2 ×10 · F3 AI·소음 · F4 무적 · F5 시야 · F6 아이템 · F7 +1분 · F8 시계 끝.
- Unity 6.6은 허브 UIDocument 옆에 PanelRenderer를 런타임에 붙여 UXML 사본을 그린다. 탐험 중 허브를 숨길 때 `HubScreen.SetPanelRendererEnabled(false)`로 함께 끈다.
- 플레이 기록: `persistentDataPath/PlayLogs/expeditions.jsonl` (디버그 사용 판·자동 테스트 제외) → 메뉴 `ApoGirl/Exploration/Summarize Play Logs`, `Apply Play Logs To Headless Models`.
- 탐험 슬롯은 2단계: `GameSession.Step()`이 `AwaitingExpedition`을 돌려주면 탐험 씬(Additive) → `CompleteExpedition(result)`. 디버그 패널에서 헤드리스(자동) 탐험으로 전환 가능.
