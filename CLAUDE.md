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

## 밸런스 기록
- 봇 결과는 **전략별 엔딩 확률 + 방향성**만 보고한다. 큰 수정마다 [Docs/BOT_ENDING_LOG.md](Docs/BOT_ENDING_LOG.md)에 이어 적고, 목표 맞추기는 마지막에 한 번에 (마스터 프롬프트 9.2절·D26).

## Git
- 원격 저장소: **https://github.com/phition/apogirl** (`origin`, 2026-11-20부터). 예전 저장소는 `old-origin`(apogirl-2026-11-18_15-16-26)으로 남겨 두고 더 쓰지 않는다.
- 커밋은 로컬에만 한다. **푸시는 사용자가 말할 때만** `main`을 `origin`에 한다. 강제 푸시는 하지 않는다.

## 현재 단계
- M0~M5 완료. M6(전체 콘텐츠)은 M6-5(아트·사운드: 사용자가 `ArtInbox/`에 넣으면 연결, 프롬프트는 [Docs/HIGGSFIELD_ASSETS.md](Docs/HIGGSFIELD_ASSETS.md))만 남음 — 1차분 연결 완료(8.1절). 그림 `Presentation/Art/StoryArt` + `Resources/Art/art_map.json`, 소리 `Presentation/Audio/Sound` + `Resources/Audio/sound_map.json`, 가져오기 설정 `Editor/ArtImportSettings`, 탐험 그림은 `Resources/Art/ExplorationPixel/`(PixelLab 픽셀 아트, PPU 36) → `ExplorationHD/` → 코드 그림 순서, 동작 프레임 수는 파일 수로 센다. 탐험 조명(낮 복도 햇빛·어두운 방)은 `ExplorationLighting` + `BalanceConfig` 조명 절 (HIGGSFIELD_ASSETS 8.2절). 탐험 시야: 어두운 곳(밤·황혼, 낮에도 방 안 = `IsDarkIn`)에서는 먼 기물(장식·수색할 곳·인물)도 좀비처럼 안 보인다(기물 안개 `UpdateFog`), 문틈은 낮 방이면 문 근처(`RoomPeekRange`)만, 밤엔 아무것도 안 보여 귀 기울이기로, 창 너머 실루엣은 창 유리 마스크(`WindowMask`) 안에서만 (8.4절). 탐험 몸 상태: 스태미너(달리기·공격·밀치기 소모, 체력 능력치로 최대치, 피로·부상으로 깎인 몫은 게이지 붉은 칸), 수색 시간 ×0.7·스트레스가 높으면 더 걸리고 늘어난 순간 한숨(`Sfx/sfx_sigh_irritated`), 숨기는 Space 길게(민첩이 높을수록 짧게), 수면 부채가 쌓이면 숨다가 잠들어 시계 2배·Space 연타로 깬다 (`ExplorationRules`, `BalanceConfig` 스태미너·숨기 절, 8.5절). 탐험 그림 배치는 `Resources/Art/exploration_art.json`(`ExplorationArtMap`), 주인공은 `player_{옷}_{무기}_{동작}_{n}` 30벌(방어구가 없으면 계절 교복: 여름 `summer`·겨울 `winter`, `StandingConfig.PlayerOutfitToken` — 입상 `SeasonOutfit`과 같은 표), 학교 밖 지역은 진입로·탈출로(바깥 길)·무너진 계단·지금 층만 보이기 (8.3절). **M7(밸런싱 & 봇 테스트) 진행 중: M7-0(수혈·헤드리스 조우)·M7-1(봇 시뮬레이터) 완료, M7-2 수치 조정·1막 대사 보강(M7-h, 여섯 차례), M7-3 난이도 완료.** 봇 배드 목표 45~55% (부상 도입 뒤), 난이도별 식량·식수·의약품 수확 배율(`DifficultyDef`), 엔딩 목표 기본·특화 15~25·관계·서사 6~12·히든 1~3%. 난이도는 `BalanceConfig.Difficulties`, 새 게임에서 선택. 밸런스 수치 원본은 `Core/Definitions/BalanceConfig.cs`, 에셋은 가져오기 때 동기화(메뉴 `ApoGirl/Balance/Sync Balance Asset`). 봇 시뮬레이터: 메뉴 `ApoGirl/Balance/Run Bot Simulation` → `Temp/BotSim/summary.md`·CSV, 전략은 `Assets/_Project/Balance/bot_strategies.json`. 계획·결정: [Docs/M7_PLAN.md](Docs/M7_PLAN.md). 52주 전체 헤드리스 점검은 EditMode `FullYear_MixedStrategy_ReachesRescueEnding` → `Temp/ApoGirlYearBalance.txt`. 막별 대본은 `Resources/Dialogue/Act{n}/`, 데이터는 `Content/Story/act{n}_events.json`·`Content/Base/act{n}_world.json`. 막별 헤드리스 점검은 EditMode `Act{n}_…` → `Temp/ApoGirlAct{n}Balance.txt`. 계획·결정: [Docs/M6_PLAN.md](Docs/M6_PLAN.md) (이전: M4_PLAN, M5_PLAN). 그림 프롬프트: [Docs/ART_PROMPTS.md](Docs/ART_PROMPTS.md), 사용자가 만든 그림은 `ArtInbox/`.
- **허브 UI 개편 진행 중** ([Docs/UI_REDESIGN_PROPOSAL.md](Docs/UI_REDESIGN_PROPOSAL.md) 10절): 1단계(다섯 겹 — 장면·상시 표시·서랍·계획 보드·카드·대화, 14칸 도크, 주인공 입상 자리, 실행 결과 한 줄) 완료. 2단계(학교 단면도 `HubSectionView` + `Resources/Art/hub_section.json`, 방 누르면 그 방 행동, 실행 중 걷기·밤 불빛) 완료. 3단계(주간 결산 일기장 `SettlementView`, 출정 준비 전용 화면 `ExpeditionPrepView`, 입상 전신 그림 `Mode: Whole`, P4 설비 그림·아이콘) 완료. 세력 거래 전용 화면 `FactionView`(얼굴은 `art_map.json` `Factions`). 이번 주 되돌리기는 이야기 중심 + 52주차 엔딩을 본 뒤에만(`GameSession.UndoUnlocked`). 단면도 바깥 풍경(마을·하늘·해달·구름·비·눈) `HubSkyView` + `hub_section.json` `Sky`, 그림 `Resources/Art/UI/HubSky/`(PixelLab). 힉스필드 2차 에셋 연결(이벤트 컷·새 인물·아이콘·결산 밤 배경), 친구 화자 키 `{FRIEND}`. `HubLayers`·`DockView`·`MiniVitalsView`·`StandingView`, 입상 조각 표 `Resources/Art/standing_map.json`(그림은 `Resources/Art/Standing/stand_*.png`, 없으면 표정 초상), 눈 깜빡임·시선 조각 `Standing/Eyes/`, 동작 프레임 `Standing/Motion/{클립}/m_##.png` 12fps — `Eyes`·`Motion.Sets` 절: 그림(+얼굴)별 idle·이따금 동작, 힉스필드 3·4차 37개 연결). 생활기록부 서랍은 다이어리 한 쪽 `RecordDiaryView`(지난달 대표 이벤트 컷 폴라로이드·능력치 힌트·선생님의 한마디, 설정 `Resources/Art/record_diary.json`). 허브에서 Tab = 계획 보드, Esc = 닫기. 그림 프롬프트 2차는 [Docs/HIGGSFIELD_ASSETS_2.md](Docs/HIGGSFIELD_ASSETS_2.md). 입상 움직임 3차 [Docs/HIGGSFIELD_ASSETS_3.md](Docs/HIGGSFIELD_ASSETS_3.md), 상태 반응 영상 4차 [Docs/HIGGSFIELD_ASSETS_4.md](Docs/HIGGSFIELD_ASSETS_4.md)(`Tools/make_higgsfield_doc4.py`로 생성), 좌석 선택 엔딩 컷·2학년 선배·4막 이벤트 컷 5차 [Docs/HIGGSFIELD_ASSETS_5.md](Docs/HIGGSFIELD_ASSETS_5.md). 프롤로그 등굣길 셀 애니메이션 6차 [Docs/HIGGSFIELD_ASSETS_6.md](Docs/HIGGSFIELD_ASSETS_6.md). 기획 제안(CD 수집·탐험의 깊이·확장 7가지): [Docs/PROPOSAL_CD_DEPTH_EXPANSION.md](Docs/PROPOSAL_CD_DEPTH_EXPANSION.md).
- **2~4막 보강 완료** ([Docs/ACTS_EXPANSION_PLAN.md](Docs/ACTS_EXPANSION_PLAN.md)): 외부 세력 3개(사거리 파·더불어 마트·UBC, `Content/Base/factions.json`, `FactionRules`, 허브 세력 칸·카드·거래, 세이브 v6), 계절 소풍 4종·수학여행 1박 2일(`Content/Base/outings.json`), 막별 확장 장면(`Content/Story/act{2,3,4}_expansion.json`, `Resources/Dialogue/Act{n}/Act{n}Expansion.yarn`). 대본 함수 `joined()`·`faction()`·`faction_known()`.
- **동료 탭·이번 주 배분하기** (기획 [Docs/PROPOSAL_CD_DEPTH_EXPANSION.md](Docs/PROPOSAL_CD_DEPTH_EXPANSION.md) D절): 동료·선생님마다 몸 상태 `BodyState`(스트레스·포만·수분, 세이브 v7), 스트레스가 높으면 역할 효과 반. 상단 "사람들" 단추 → 인물 서랍 `PeopleDrawerView`(뱃지·호감은 느낌 문구만·막대·부상). "한 주 실행" → ① 배분 화면 `GiveSheetView`(음식·물·약·기호품, 종류별 주 1회) → ② 스케줄 실행, 받은 사람 머리 위 말풍선. 규칙 `GiveRules`, 수치 `BalanceConfig` 동료 절, 기호품 16종 `LikedItems`(전리품 표 + 헤드리스 확률). 호감 50 이상 동료가 허브 한 줄 멘트에 가끔 나온다(멘트를 누르면 다음). 뱃지·아이콘 그림은 [Docs/HIGGSFIELD_ASSETS_7.md](Docs/HIGGSFIELD_ASSETS_7.md).
- **동료 역할 재기획** ([Docs/ROLE_ROOMS_PROPOSAL.md](Docs/ROLE_ROOMS_PROPOSAL.md), P1~P5): P1 완료 — 역할 6개(경비·정찰 기본 + 간호·살림·공부·위로), 역할마다 방 묶음 `RoleDef.Rooms`(열린 방이 있어야 맡김, 방이 늘수록 +25%), 정원 `RoleCapacity` 2(넘치면 늦게 붙은 사람은 효과 없음), 적성 `BalanceConfig.RoleAptitudes` ×1.25, 규칙 `RoleRules`, 단면도 자리는 `RoleRules.RoomFor`, 선생님은 교실(`hub_section.json` `Teacher.Room`), 세이브 v8(예전 역할 ID 변환). 살림은 가사실을 뒤져야 맡긴다(창고 제외). P2 완료 — 함께한 시간 `TogetherRules`(방 스케줄 → 그 방 동료 호감 +1, 적성 +2, 주 상한 2·3, 윤 선생님은 가르치는 수업에서만 주 1, `SlotOutcome.Together` → 말풍선·결과 한 줄 `together.*`, 적성 방 바람 `people.wish.*`·`hubline.wish.*`), 수치 `BalanceConfig` 함께한 시간 절. P3(코어) 완료 — 동료 탐험 `OutingRules`(주 시작 `PrepareWeek`에 일정: 주 1~2번 낮, 정찰 2번, 하루 2명, (판 시드·주·동료)로 정해져 예고 = 실제; 낮 칸 시작에 나가고(`NpcState.Away`) 밤 칸 시작에 물자·기호품·다침(가벼운 상처 `MinorWoundWeeks`·부상)과 돌아옴; 자리 비움은 `OutingRules.AwayFactor`로 역할 합산에, 함께한 시간 `Absent`; 붙잡기 `StayHome`·`StayStreak`), 수치 `BalanceConfig` 동료 탐험 절, 결과 한 줄·말풍선·결산 한 줄. P4 완료 — 단면도: 그날 외출하는 동료는 낮 칸 시작에 현관(`hub_section.json` `Entrance`)으로 걸어가 사라지고 맡은 방 이름 옆 "· 외출", 밤 칸 시작에 현관에서 돌아온다(`HubSectionView._outToday`); 인물 서랍: 계획 중 예고(날짜 없이)·"이번 주는 남아 줘" 토글(`OutingRules.WantsOut`, 2주 연속 경고)·실행 중 "밖에 나가 있다"·가벼운 상처; 계획 예보 아래 `plan.outing_note`. P5 완료 — 봇 적성 역할 배정(`BotRoles`, `bot_strategies.json` `RoleAssignment`·`RolePriority`·`RoleKeepNpcs`), 봇 보고서 "동료 탐험" 절, 동료 수확은 주인공의 30%(`OutingYieldShare`)를 한 종류로(`OutingHaul`: 식량·물 30, 자재 20, 의약·연료·전력 5, 빈손 5%).
- **동료 특기·개인 장면** (기획 [Docs/PROPOSAL_CD_DEPTH_EXPANSION.md](Docs/PROPOSAL_CD_DEPTH_EXPANSION.md) C.2 ①②): 특기 6종 `SpecialRules` + `BalanceConfig.CompanionSpecials`(깃발 `SPECIAL_{NPC}` + 합류 + 호감 80, 인물 서랍 표시). 개인 장면 15편 `Content/Story/bond_events.json` + `Resources/Dialogue/Events/Bond.yarn`(`EVT_BOND_{X}_{1~3}`, 호감 20·50·80, 요일별 하나, 세 번째에서 특기). 유랑자는 방문 사다리(3막 방문 1·2·3 선택지 `NextEventId` → `EVT_BOND_WANDERER_{1~3}`, 특기 표 `Visitor`로 합류·호감 검사 없음). 윤 선생님 장면 `EVT_BOND_TEACHER_1·2`(호감 50·80, 80은 40주~, 요일 6, 격리 중 제외, 졸업장 깃발 `TEACHER_DIPLOMAS`).
- **위기 이탈** (기획 C.2 ③): `CrisisRules` — 위기 주(결산 때 식량·식수 바닥 또는 습격 돌파)에 호감 0 미만 동료 한 명 예고 `LEAVE_WARN_{NPC}`(결산 한 줄) → 다음 주 `EVT_CRISIS_LEAVE_*`(`Content/Story/crisis_events.json`, `Events/Crisis.yarn`: 나눠 주기·설득 판정·보내기, 떠나면 `Left` + `CRISIS_LEFT_{NPC}`). 습격 돌파 때 호감 60+ 동료가 대신 다침(`RaidResult.CoveredNpcId`). 수치 `BalanceConfig` 위기 이탈 절.
- **CD 수집** (기획 A): 지역 8곳에 CD 한 장씩. 줍는 조우 `EncounterDef.Collectible`, `LocationBuilder.WithCd`. 낮·밤 모두에 있고, 2막 이후 4장은 깊은 곳(B)이 생기면 옮긴다. 헤드리스는 `BalanceConfig.HeadlessCdChance`(따로 굴린 난수). 깃발 `CD_01~08`, 처음 주운 주차 `GameState.CdWeeks`, 고른 곡 `CdTrack`(세이브 v9), 규칙 `CdRules`. 방송반 방에 진열장 칸(`hub_section.json` `CdShelf`)이 있고, 방을 누르면 CD 플레이어가 뜬다(`CdPlayer`, 곡 표 `Resources/Art/cd_map.json`, 음원 `Resources/Audio/Music/`, 없으면 조용히 넘어감). 탐험을 떠나면 멈추고 돌아오면 이어진다. 타이틀 "음악실" `MusicRoomView`(들은 곡 `persistentDataPath/MusicRoom.json`). 8장을 모으면 `EVT_CD_SHELF_FULL`(`Events/Cd.yarn`).
- **엔딩·후일담 갈래** (기획 C.2 ⑤): 동료 후일담 가까움(호감 80+, 우선 25)·서먹함(<20, 5)·떠남(`CRISIS_LEFT_*`, 60), 유랑자·졸업장 줄 (`Content/Story/epilogues.json`). 함께 엔딩에 `CloseCompanions`(Key = 기준 호감 "60") ≥ 2. 좌석 선택 `EVT_A4_SEAT_CHOICE` 대사가 호감·개인 장면(`seen`)으로 갈린다.
- **고양이 '급식'** (확장 2)
  - 합류 사다리: `EVT_CAT_1~3`(`Content/Story/cat_events.json`·`Events/Cat.yarn`, 9·11·13주~ 밤)으로 `NPC_CAT`이 합류하고 `CAT_HOME`이 켜진다. 2막 `EVT_RND_A2_CAT`은 두 번째 기회다.
  - 효과: `CatRules`(`BalanceConfig` 고양이 절) — 주마다 식량, 지친 주에 무릎(스트레스), 습격 피해 배율.
  - 사람 수·엔딩 조건·위기 이탈·대신 다침·침입 부상·역할에서 빠진다 (`CatRules.IsCat`). 13장에 따라 다치거나 죽지 않는다.
  - 단면도에서 돌아다니고(`HubSectionView.CatRoom`, 임시 그림 `hub_cat`), 후일담은 `EP_CAT_*`다.
- **방과후 동아리** (확장 7)
  - 밤 행동 4종 `ACT_CLUB_*`: 14주~, 주 1회 (`PlanEditResult.WeeklyLimit`).
  - 같이 할 사람은 인물 서랍에서 고른다 (`GameState.ClubPartner`, 세이브 v10). 고르지 않으면 호감이 가장 높은 사람이 한다.
  - 규칙 `ClubRules`: 호감 +4, 좋아하는 활동이면 +7 (`BalanceConfig.ClubLikes`). 좋아하는 활동을 3번 하면 `CLUB_SCENE_{NPC}` → 개인 장면.
  - 장면: 활동 4편 `EVT_CLUB_*`(같이 한 사람 `CLUB_WITH_{NPC}`로 분기)·개인 6편 `EVT_CLUB_BOND_*` (`Content/Story/club_events.json`, `Events/Club.yarn`).
- **방송반 송출** (확장 5)
  - 밤 행동 3종 `ACT_BROADCAST_{SONG,INFO,LETTER}`: `DJ_CONTACT` 뒤, 여유 전력 2, 주 1회(동아리와 함께 `ActionDef.WeeklyGroup`). 곡은 CD가 있어야 한다.
  - 규칙 `BroadcastRules`(`BalanceConfig` 방송 절): 응답 30% → `BROADCAST_REPLY` → 응답 장면 `EVT_BROADCAST_REPLY_*`. 방송한 주(`BROADCAST_ON_AIR`)에는 사거리 파 휴전 전 습격 ×1.2.
  - 장면: 첫 방송·프로그램 3편·응답 4편 (`Content/Story/broadcast_events.json`, `Events/Broadcast.yarn`).
- **전력 = 유지력** (사용자 결정 2026-11-22)
  - 전력(`ResourceType.Power`)은 발전 용량이고 쓰면 줄지 않는다. 자전거 발전기 복구 = 용량 3.
  - 여유 전력 = 용량 − 켜진 전기 기기 (`PowerRules`). 기기 표는 `BalanceConfig.Appliances`: 지금은 냉장고 1(식량 부패 문턱 +30). 전자 편의기기는 여기에 한 줄씩 더한다.
  - 행동 조건은 `ActionDef.MinFreePower`(방송 2·무전 1)다.
  - 배터리(`ResourceType.Battery`)를 모아 `ACT_SURV_UPGRADE_POWER`로 용량 +1: 5개, 공학 숙련마다 −1, 최소 2, 상한 6.
  - 발전기 고장(`GENERATOR_BROKEN`)은 `ACT_SURV_REPAIR_GENERATOR`로 고친다. 세이브 v11.
- **탐험의 깊이** (기획 B): 2막 이후 지역 5곳의 깊은 곳 층
  - 층 번호와 연결: 겉 층 뒤 번호(`BuildingLayoutDef.DeepFloors`·`IsDeep`)이고, 연결은 깊은 문 `DeepDoor`(양쪽 한 쌍)·일방 출구 `OneWayExit`·바깥 철문 `OneWayBack`, 도착 자리는 `TargetX`다.
  - 배치: `LocationBuilder.Deep`/`DeepDoor`/`OneWay`로 놓고, Build Locations 때 `DeepRules.Issues`가 점검한다. 전리품 표는 `Content/Base/deep_loot.json`이다.
  - 탐험 화면: 늘 어둡고 소리가 울린다. 일방 출구는 두 번 눌러야 나가고, 유랑자 특기가 있으면 출구 쪽을 알려 준다.
  - 헤드리스는 `BalanceConfig` 깊은 곳 절의 `HeadlessDeepChance`를 쓴다. CD_04·05·06·08은 깊은 곳에 있다. 그림은 아직 코드 그림이다.
- **계절 재해** (확장 6)
  - 기간: 장마 `MONSOON`(15~19주, 비 75%)·폭설 `SNOWSTORM`(42~46주, 한파 80%). 표는 `BalanceConfig.Disasters`, 날씨 굴림은 `DisasterRules.RollWeather`다.
  - 깊은 곳이 있는 지역의 지도가 바뀐다. 실행 중에만 쓰는 사본이다(`DisasterRules.Apply`, `ConnectorDef`·`ExitDef.BlockedBy`).
  - 장마: 깊은 곳 맨 아래층이 침수된다(걸음 ×0.7·소음 ×1.5, `FloodMoveMultiplier`·`FloodNoiseMultiplier`). 그 층 일방 출구가 막히고, 잠긴 보조 탈출구가 열린다.
  - 폭설: 깊은 문이 둘인 곳은 1층 깊은 문 쌍이 막힌다. 1층 보조 탈출구는 눈에 파묻힌다(쇠지렛대로도 못 연다).
  - 헤드리스는 장마 깊은 곳 ×0.6이다.
  - 허브 날씨 칸에 장마·폭설이 뜨고(`disaster.*` 아이콘 `icon_weather_storm`·`icon_weather_snow`), 출정 준비 화면에 안내가 붙는다.
- **탐험 후처리** (보이는 맛 1단계): `ExplorationLook` — 탐험 카메라에만 후처리(톤매핑·대비·채도·스플릿 토닝·블룸·비네트, 낮·밤 따로)와 빛 기둥(손전등·햇빛 줄기 볼류메트릭). 값은 `Resources/Art/exploration_look.json`(탐험마다 다시 읽음), 디버그 F9로 켜고 끈다. 2단계 그림자: 손전등 빛(세 겹)만 그림자를 만들고(다른 빛은 `shadowsEnabled = false`), 좀비·사람·상자·바닥에 선 큰 장식(벽에 걸린 건 뺌)이 스프라이트 외곽선 그림자를 드리운다(`ExplorationLook.AddCaster`, 설정 `Shadows`: 세기 0.5·부드러움 0.3·최소 높이 0.8). 도트 빛: 셰이더 `Art/Shaders/ExplorationPixelLit.shader`(URP 2D 스프라이트 조명 바탕, 머티리얼 `Resources/Art/exploration_pixel_lit.mat`)가 빛을 그림 한 화소마다 읽어 빛 경계가 도트 격자를 따르고(늘린 단색 사각형은 `SnapMaxTexel`로 뺌), 빛 세기를 6단계로 끊어 반쯤 섞는다(`PixelLight` 절). 탐험 월드의 기본 조명 머티리얼 스프라이트만 바꾸고(`ExplorationLook.ApplyPixelLight`, 공간 이동 때 다시), 켜고 끄기는 전역 값 `_ApoPixelLightOn`(F9). 셰이더에 머티리얼별 값(`_MainTex_TexelSize` 등)을 CBUFFER 밖에 두면 스프라이트 배칭이 남의 그림을 그리니 주의 (텍스처 크기는 `GetDimensions`). 3단계 노멀: 노멀맵 파일 없이 같은 셰이더의 NormalsRendering 패스가 그림에서 화소 단위 노멀을 만든다(높이 = 알파 × 밝기, 이웃 화소 차이 → 기울기). 손전등 빛 세 겹만 노멀을 쓰고(Light2D 노멀 설정은 읽기 전용이라 `m_NormalMapQuality`·`m_NormalMapDistance` 필드를 리플렉션으로), 값은 `Normals` 절(세기 12·밝기 비율 0.7·빛 높이 2).
- 학교 교실동 4층 = 옥상(`BuildingLayoutDef.RooftopFloor`·`RooftopFlag`): 방·좀비 없음, 벽 대신 철조망 너머 마을(`roof_fence`·`roof_town`, PixelLab, 도구 `code_temp/apogirl/tools/gen_roof.py`·`install_roof.py`), 방송실은 3층. 옥상에 오르면 `REACHED_ROOFTOP` → 귀환 뒤 `EVT_A1_ROOFTOP_EARLY`로 거점 옥상 해금(4주차 전에도), 헤드리스는 `BalanceConfig.HeadlessRooftopChance`.
- 구조 헬기 정원 8(`BalanceConfig.RescueSeats`, `BoardingRules`): 선생님 완치 + 전원 합류로 9명이면 52주 5일째 밤 `EVT_A4_SEAT_CHOICE`에서 한 명을 남기고 짝이 함께 남는다. 완치 엔딩은 `EndingDef.Variants`로 컷·그림이 바뀐다 ([Docs/M7_PLAN.md](Docs/M7_PLAN.md) "구조 헬기 좌석 선택").
- 1주차: 1~6일은 창고에 갇혀 버티고(`ACT_STORAGE_WAIT`, `Confined`) 7일 낮에 한 번 탈출, 모두 잠김 (`TutorialWeekRules`, `BalanceConfig.TutorialDay`). 갇힌 날마다 밤 끝에 `EVT_A1_STORAGE_D1~D6`, 여섯째 날에서 `ThenEventId`로 선생님이 문을 연다(`EVT_A1_OPENING`, 2회차는 `EVT_NG_STORAGE_7DAYS` 회상을 거쳐).
- 부상(`INJURED`, 한 사건 체력 30%↑ 손실, 회복 불가·의약품 치료)·청결(주마다 감소, 50 미만이면 상처 감염)·빗물(`RawWater`, 정수해야 식수): `InjuryRules`, `BalanceConfig` 부상·청결 절. 교실동 특수실 6곳과 1-5 교실 수색 뒤 거처 이벤트(거점/창고 선택별 해금)는 [Docs/M7_PLAN.md](Docs/M7_PLAN.md) 마지막 절.
- 포만·수분(`Satiety`·`Hydration`, −100~+100, 0 = 보통): 0 이상이면 먹지 않아 아끼고, 단계별 체력·스트레스 버프·디버프 (`BalanceConfig` 포만·수분 절, `UpkeepRules.ApplyDaily`). 요리 수업 성공 → `CookingRules`(숙련별 요리, 포만·수분·능력치).
- 엔딩 확인: 허브 F1 → "52주차로" / "지금 상태로 구조 엔딩" / 원인을 고른 "배드 엔딩". 디버그 상태 줄에 지금 구조되면 받을 엔딩이 보인다. 갤러리는 `persistentDataPath/Gallery.json`.
- M6 디버그(F1): 감염 +25, 선생님 재감염/진행 +25, 날씨 바꾸기, 구조 공지 토글. 동료 역할은 허브 거점 칸의 동료 버튼을 눌러 바꾼다.
- 콘텐츠 작성법: [Docs/CONTENT_PIPELINE.md](Docs/CONTENT_PIPELINE.md). 원본은 `Content/**/*.json`(행동·아이템·전리품·탐험 모델·적·특성·시설·이벤트·NPC·일기/코멘트·프롤로그) + `Resources/Dialogue/**/*.yarn` → 메뉴 `ApoGirl/Content/Import Events`. 지역 레이아웃은 `Editor/LocationBuilder.cs` → 메뉴 `ApoGirl/Content/Build Locations`.
- 1막 밸런스 점검: EditMode `Act1BalanceTests`가 세 전략으로 13주를 돌려 `Temp/ApoGirlAct1Balance.txt`에 기록.
- 주 진행 중 이벤트: `GameSession.Step()`/`CompleteExpedition()`이 `AwaitingEvent`를 돌려주면 대화 → `ResolveEvent(choiceId)`. 허브 F1 디버그 패널에서 이벤트 강제 발생·자동 진행 전환.
- 코어 진입점: `NewGameFactory.Create` → `WeekRunner`(`PrepareWeek` → `ExecuteNextSlot` × 14 → `FinishWeek`), 정의 DB는 `Assets/_Project/Data/DefinitionDatabase.asset`.

## Unity
- Unity 6000.6, 2D URP. Unity MCP(`.mcp.json`, 서버 이름 `unity`)로 에디터와 연결된다.
- 시뮬레이션 코어(`ApoGirl.Core`)는 UnityEngine을 참조하지 않는다(asmdef `noEngineReferences`).
- 테스트: Unity Test Runner → EditMode (`ApoGirl.Tests.EditMode`), PlayMode (`ApoGirl.Tests.PlayMode`). 결과 요약은 `Temp/ApoGirlTestResults.txt`에도 기록된다.
- UI: UI Toolkit (`Assets/_Project/UI`: Title.uxml, Hub.uxml, Theme.uss, PanelSettings). 화면 문자열은 `Assets/_Project/Localization/Source/ko.json`이 원본 → 메뉴 `ApoGirl/Localization/Sync Tables`로 테이블 생성.
- 실행: Boot 씬에서 플레이 → 타이틀 "새 게임 (프롤로그)" 또는 "빠른 시작". 허브 F1 = 디버그 패널(지역·시설 전부 해금, UI 전부 보기, 이벤트 강제 발생). 탐험: Space 숨기, Q 무기 공격(무기 없으면 밀치기), 1~4 퀵슬롯, F 손전등(도구 필요), 문 앞 W 살피기·S 듣기, 디버그 F2 ×10 · F3 AI·소음 · F4 무적 · F5 시야 · F6 아이템 · F7 +1분 · F8 시계 끝 · F9 후처리 켜고 끄기.
- 테스트 빌드: 메뉴 `ApoGirl/Build/Windows Test Build` → `Builds/Windows/LastSemester_날짜시각/LastSemester.exe`(git 제외, 폴더에 "읽어 주세요.txt": 조작키·디버그 키·세이브/플레이 기록 위치). 개발 빌드 메뉴도 있다. 안드로이드: 메뉴 `ApoGirl/Build/Android Test APK` → `Builds/Android/LastSemester_날짜시각/LastSemester.apk`(ARM64·IL2CPP·가로·디버그 키, 패키지 `com.apogirl.lastsemester`). 휴대폰에는 터치 조작이 없어 탐험이 자동(헤드리스)으로 진행된다(`GameApp`, `Application.isMobilePlatform`). 빌드는 Unity MCP로 실행할 수 없어(사용자 상호작용 작업) 사용자가 메뉴를 누른다.
- Unity 6.6은 허브 UIDocument 옆에 PanelRenderer를 런타임에 붙여 UXML 사본을 그린다. 탐험 중 허브를 숨길 때 `HubScreen.SetPanelRendererEnabled(false)`로 함께 끈다.
- 플레이 기록: `persistentDataPath/PlayLogs/expeditions.jsonl` (디버그 사용 판·자동 테스트 제외) → 메뉴 `ApoGirl/Exploration/Summarize Play Logs`, `Apply Play Logs To Headless Models`.
- 탐험 아트(픽셀 아트): `Editor/PixelArtBuilder.cs` → 메뉴 `ApoGirl/Art/Build Exploration Sprites`가 `Resources/Art/Exploration/*.png`(캐릭터·좀비·소품·벽 타일, CC0 오피스 타일셋 조각)를 생성한다. 런타임은 `ExplorationArt`가 키로 불러오고, 없으면 사각형으로 대체한다. 외부 에셋 출처는 `Art/ThirdParty/*/CREDITS.md`.
- 탐험 슬롯은 2단계: `GameSession.Step()`이 `AwaitingExpedition`을 돌려주면 탐험 씬(Additive) → `CompleteExpedition(result)`. 디버그 패널에서 헤드리스(자동) 탐험으로 전환 가능. 귀환하면 결과 카드 → 이어지는 이벤트 순. 동료가 될 인물과의 조우는 탐험 화면 위 대화(`FieldDialogue`, `EncounterDef.DialogueNode`, `Events/Encounters.yarn`), 현장 장면(`PlayEventInField`)은 그 자리에서 치르고 `ExpeditionResult.FieldEvents`로 귀환 때 적용 ([Docs/M7_PLAN.md](Docs/M7_PLAN.md) 끝 절).
