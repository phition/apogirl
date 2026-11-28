# 클로드 코드 스킬 선별 보고서

조사일 2026-11-28. 공개 스킬 순위에서 상위권을 훑고, 〈마지막 학기〉에 쓸모 있는 것만 골랐다.
**3절의 6개는 2026-11-28에 설치했다** (사용자 결정). 결과는 3.1절.

## 1. 결론

- 순위 맨 위는 대부분 우리와 상관없다. 영상 제작·웹 프런트·사내 메신저(라크) 스킬이 상위 75위의 대부분이다.
- 쓸 만한 것은 두 갈래다. **일하는 방식**(설계 캐묻기·버그 진단·끝내기 전 확인)과 **게임 실무**(세이브·빌드 용량·타격감·스팀 출시).
- 바로 설치를 권하는 것은 6개다 (3절). 나머지는 M8(폴리싱·릴리즈)에 들어갈 때 넣으면 된다 (4절).
- 유니티 공식 스킬은 35개 중 34개가 이미 깔려 있다. 빠진 것은 `instrument-game-telemetry` 하나다.

## 2. 조사 방법과 믿을 수 있는 정도

| 출처 | 본 것 |
|---|---|
| [skills.sh](https://skills.sh/) 전체 순위 | 1~75위 (설치 수 기준) |
| [skills.sh/trending](https://skills.sh/trending) | 1~65위 |
| `npx skills find` | 키워드 15개 (unity, game design, pixel art, playtest, steam, firebase 등) |
| GitHub | 후보 저장소 16곳의 별 수·만든 날·마지막 푸시, 스킬 목록, 후보 스킬 설명문 |

읽은 것은 각 스킬의 **설명문(머리말)**까지다. 본문 전체를 읽고 검증한 것은 아니다.
그래서 설치 전에 그 스킬의 `SKILL.md`를 한 번 읽고 넣는 것을 전제로 한다.

**설치 수만 믿으면 안 된다.** 순위에 별 수와 설치 수가 맞지 않는 저장소가 섞여 있다.

- `101-skills/superpowers`: 설치 110만, 별 35개 (2026-08 생성).
- `magentosh/superpowers`, `skills-shell/superpowers`, `beltsh/superpowers`, `qu-skills/superpowers`: 이름이 같고 스킬 구성도 같은 묶음이 여럿이다.

진짜 `obra/superpowers`(별 32.8만)의 이름을 빌린 것으로 보인다. 이런 곳은 후보에서 뺐다.
아래 후보는 설치 수와 별 수를 함께 봤다.

## 3. 바로 설치를 권하는 것 (6개)

| 스킬 | 출처 (별) | 순위·설치 | 하는 일 | 우리에게 필요한 이유 |
|---|---|---|---|---|
| `grill-me` | mattpocock/skills (33.4만) | 전체 6위, 160만 | 계획·설계를 끝까지 캐묻는 인터뷰 | 작업 원칙 2(설계 요약 먼저)·4(`[결정 필요]`는 선택지로)와 같은 방향이다. 기획 제안서(제작·역할·선생님 추천 편성 같은)를 쓰기 전에 빈 곳을 먼저 찾는다. |
| `systematic-debugging` | obra/superpowers (32.8만) | 35만 | 고치기 전에 원인부터 찾게 하는 절차 | 스프라이트 배칭이 남의 그림을 그리던 일, 방 이름이 영문 키로 나오던 일처럼 원인이 한 겹 아래 있는 버그가 잦다. |
| `verification-before-completion` | obra/superpowers | (같은 묶음) | "됐다"고 말하기 전에 확인 명령을 돌리고 결과를 보게 함 | EditMode 테스트 523개와 봇 시뮬레이터가 있다. 밸런스 수치를 고친 뒤 안 돌리고 넘어가는 일을 막는다. |
| `save-systems` | gamedev-skills/awesome-gamedev-agent-skills (1.2만) | 약 1.5만 | 무엇을 저장할지, 원자적 쓰기, 버전 마이그레이션 | 세이브가 v16까지 올라왔고 버전마다 변환이 붙는다. 출시 뒤에는 깨진 세이브가 가장 비싼 버그다. |
| `unity-build-size` | 같은 곳 | 약 1.5만 | 빌드 보고서를 읽고 큰 원인(무압축 그림·PCM 소리)을 찾음 | 4의 배수가 아닌 큰 그림 약 260장이 무압축으로 들어가는 문제를 미뤄 뒀다. 그 작업을 할 때 그대로 쓴다. |
| `game-feel` | 같은 곳 | 약 1.5만 | 화면 흔들림·히트 스톱·이징·넉백 | 탐험 타격 판정과 `HitFx`는 있다. 그 위에 손맛을 얹는 것이 M8 폴리싱의 한 축이다. |

`systematic-debugging` 대신 `mattpocock/skills`의 `diagnosing-bugs`(전체 28위, 98만)를 써도 된다. 역할이 겹치니 **둘 중 하나만** 넣는다.

설치 명령 (프로젝트에만 넣는다. `-g`를 붙이면 모든 프로젝트에 깔린다):

```powershell
npx skills add mattpocock/skills@grill-me
```

```powershell
npx skills add obra/superpowers@systematic-debugging
```

```powershell
npx skills add obra/superpowers@verification-before-completion
```

```powershell
npx skills add gamedev-skills/awesome-gamedev-agent-skills@save-systems
```

```powershell
npx skills add gamedev-skills/awesome-gamedev-agent-skills@unity-build-size
```

```powershell
npx skills add gamedev-skills/awesome-gamedev-agent-skills@game-feel
```

### 3.1 설치 결과 (2026-11-28)

- 6개를 프로젝트 `.claude/skills/`에 넣었다 (전역 아님). 출처와 커밋은 저장소 뿌리의 `skills-lock.json`에 적힌다.
- **`grilling`을 하나 더 넣었다.** `grill-me`는 본문이 "`grilling` 스킬을 불러라" 한 줄뿐인 껍데기라, 그것만으로는 돌지 않는다. 같은 저장소(mattpocock/skills)의 `grilling`이 실제 인터뷰 절차다.
- `grill-me`는 사용자가 `/grill-me`로 부를 때만 돈다(`disable-model-invocation`). `grilling`은 "캐물어 줘" 같은 말에도 걸린다.
- 넣기 전에 파일 전부의 외부 주소·명령을 훑었다. 주소는 공식 문서 링크뿐이고, 밖으로 보내는 명령은 없다.
  - `unity-build-size/scripts/*.py`: 빌드 보고서·프로젝트 파일을 읽기만 하는 점검 도구.
  - `systematic-debugging/find-polluter.sh`: npm 테스트용이라 우리 프로젝트에서는 쓸 일이 없다.
- 새 세션부터 스킬 목록에 뜬다. 실제로 불러서 돌려 보지는 않았다.
- 유의: `unity-build-size`·`save-systems`·`game-feel`은 유니티 6.3 LTS·고도 예제 기준이다. 우리는 6000.6이니 메뉴 이름·API는 그대로 믿지 말고 확인한다.

## 4. 때가 되면 넣을 것

### M8 (폴리싱·릴리즈)에 들어갈 때

| 스킬 | 출처 | 하는 일 | 쓸 때 |
|---|---|---|---|
| `steam-publish` | gamedev-skills | 디포·`app_build` 스크립트·steamcmd 올리기·베타 가지 | 스팀에 올리기로 정하면 |
| `unity-build-pipeline` | gamedev-skills | 빌드 프로필·IL2CPP와 Mono·스트리핑·스크립트 빌드 | 빌드에서만 나는 오류, 빌드 자동화 |
| `performance-optimization` | gamedev-skills | 재서 보고 CPU·GPU 중 어디가 막혔는지부터 가림 | 탐험 화면이 끊길 때 (조명·그림자·후처리가 무겁다) |
| `audio-design` | gamedev-skills | 버스 구성·음량·대사 때 음악 낮추기·효과음 변주 | M6-5 소리 마무리. 이미 있는 `optimize-audio`·`audio-setup-mixers`와 짝 |
| `instrument-game-telemetry` | Unity-Technologies/skills (공식) | 게임 안 계측 넣기 (설명문은 못 읽음) | 테스트 빌드 플레이 기록을 더 모으려 할 때. 지금은 `PlayLogs`로 충분하다 |

### 필요가 생기면

| 스킬 | 출처 (별) | 하는 일 | 메모 |
|---|---|---|---|
| `balance-check` | Donchitos/Claude-Code-Game-Studios (4.1만) | 수식·데이터에서 튀는 값·깨진 성장·퇴화 전략 찾기 | 봇 시뮬레이터가 못 보는 '표만 봐도 이상한 값'용. 목표 맞추기는 여전히 마지막에 한 번 (D26) |
| `asset-audit` | 같은 곳 | 이름 규칙·크기 예산·연결 안 된 에셋 찾기 | `StoryUnused/` 같은 정리를 다시 할 때 |
| `consistency-check` | 같은 곳 | 기획 문서끼리 어긋난 수치 찾기 | 문서가 40개를 넘었다. 다만 그쪽 문서 틀(엔티티 등록부)을 전제로 해서 그대로는 안 맞을 수 있다 |
| `dialogue-systems` | gamedev-skills | Ink·Yarn 분기 대사, 막다른 대사 찾기 | Yarn 대본이 많다. 우리 파이프라인(`CONTENT_PIPELINE.md`)이 이미 있어 급하지 않다 |
| `survival-crafting` | gamedev-skills | 채집·레시피·부패·허기 설계, '귀찮기만 한 욕구' 진단 | 제작·포만·수분을 다시 손볼 때 참고 |
| `domain-modeling` | mattpocock/skills | 용어집(`GLOSSARY.md`)·결정 기록 정리 | CLAUDE.md가 길어져 용어·결정을 따로 뺄 때 |
| `handoff` | mattpocock/skills | 대화를 다음 세션용 인계 문서로 압축 | PROGRESS 기록 규칙과 겹친다. 긴 작업을 세션 사이에 넘길 때만 |
| `writing-skills` | obra/superpowers | 스킬을 만들고 시험하는 법 | 우리 스킬(`pixel-art-tools`·`sprite-gif`)을 더 만들 때 |

`Donchitos/Claude-Code-Game-Studios`는 스킬 75개와 에이전트 49개가 한 체계로 묶여 있다.
통째로 넣으면 우리 문서 구조·작업 원칙과 부딪치니 **낱개로만** 고른다. 출시 준비 스킬(`release-checklist`·`launch-checklist`·`patch-notes`·`playtest-report`·`smoke-check`)도 있는데, 이름만 확인했고 설명문은 읽지 못했다.

## 5. 이미 가지고 있는 것

새로 깔 필요가 없다.

- **유니티 공식** (Unity-Technologies/skills, 34개): `unity-cli`·`unity-pipeline`·`ui-uitk`·`urp-postprocessing`·`2d-pixel-perfect`·`manage-sprite-atlas`·`sprite-editor`·`optimize-audio`·`optimize-text-mesh-pro`·`localize-unity-project`·`project-auditor-fixes`·`craft-game-floors` 등.
- **코드 리뷰**: 클로드 코드에 들어 있는 `/code-review`·`/simplify`. 순위 37위 `code-review`(mattpocock)와 겹친다.
- **스킬 찾기**: `find-skills` (전체 1위, 460만). 이번 조사에 썼다.
- **그림**: 우리가 만든 `pixel-art-tools`·`sprite-gif`, Aseprite 라이브 MCP, 힉스필드 스킬 묶음.

가진 것 중 덜 쓰고 있는 것: `project-auditor-fixes`(프로젝트 감사)와 `manage-sprite-atlas`(아틀라스)는 M8 빌드 용량·성능 작업 때 먼저 써 볼 만하다.

## 6. 순위는 높지만 뺀 것

| 스킬 | 순위 | 뺀 이유 |
|---|---|---|
| `agent-browser` | 전체 2위 | 브라우저 조작 도구가 이미 있다 |
| `hyperframes` 묶음, `remotion-best-practices` | 3~4위 등 | 영상 제작용. 게임과 무관 |
| `frontend-design`, `vercel-react-best-practices`, `web-design-guidelines`, `ui-taste` | 16·54·61·17위 | 웹(React·CSS)용. 우리 UI는 UI Toolkit이다 |
| `lark-*` 묶음 | 33위~ | 사내 메신저 연동 |
| `design-mobile-apps` | 5위 | PC만 만든다 (2026-11-22 결정) |
| `reddit-automation` | 8위 | 홍보 자동화. 지금 단계가 아니다 |
| `*/superpowers` 복제본들 | 20위~ | 2절의 이유 |
| `brainstorming` (obra) | — | "모든 창작 작업 전에 반드시"를 강제한다. 우리 작업 원칙 2와 겹치고 작은 수정까지 느려진다 |
| `tdd` (mattpocock·obra) | — | 테스트는 이미 기능마다 붙이고 있다. 그림·UI 작업이 많아 테스트 먼저를 강제하면 맞지 않는다 |
| `unity-scriptableobjects`·`unity-csharp-scripting`·`unity-animation` (gamedev-skills) | 약 1.5만 | 유니티 6.3 LTS 기준의 기초 안내. 우리 코드 규칙(`CODING_CONVENTIONS.md`)이 더 구체적이고, 애니메이션은 Animator가 아니라 프레임 파일 수로 돈다 |
| `layer-pixel-art`·`layer-sprite-animation` (layerai) | 5.9천 | 별 11개, 외부 유료 서비스 연동. PixelLab·힉스필드로 충분 |
| `pixel-plugin` (willibrandon) | 300 | Aseprite 라이브 MCP와 겹친다. 2025-10 이후 갱신이 없다 |
| `firebase/agent-skills` | 15만~19만 | PC통신 게시판은 SDK 없이 REST로 붙였고 보안 규칙도 25가지 시험을 마쳤다. `firebase-security-rules-auditor`가 실시간 데이터베이스 규칙까지 보는지는 확인하지 못했다 — 정식 공개 전에 규칙을 다시 볼 때만 살펴본다 |

## 7. 설치할 때 지킬 것

1. **낱개로 넣는다.** `저장소@스킬` 꼴로 하나씩. 저장소 통째로 넣으면 수십 개가 한꺼번에 깔려 엉뚱한 때에 끼어든다.
2. **프로젝트에만 넣는다.** `.claude/skills/` 아래로. 전역(`-g`)은 쓰지 않는다.
3. **넣기 전에 본문을 읽는다.** 스킬은 내가 따르는 지시문이다. 외부 주소로 보내는 명령이나 스크립트가 들어 있는지 본다.
4. **CLAUDE.md가 우선이다.** 스킬이 "반드시 ~해라"라고 해도 한국어 응답·푸시 금지·한 번에 한 마일스톤 같은 프로젝트 규칙이 이긴다. 부딪치는 스킬은 뺀다.
5. **역할이 겹치는 것은 하나만.** 버그 진단 둘, 코드 리뷰 둘을 같이 두지 않는다.
6. **한 달 써 보고 안 쓰는 것은 지운다.** 스킬 목록이 길수록 매 세션이 무거워진다.

## 8. 출처

- [skills.sh 전체 순위](https://skills.sh/) · [급상승](https://skills.sh/trending)
- [mattpocock/skills](https://github.com/mattpocock/skills) · [obra/superpowers](https://github.com/obra/superpowers)
- [gamedev-skills/awesome-gamedev-agent-skills](https://github.com/gamedev-skills/awesome-gamedev-agent-skills)
- [Donchitos/Claude-Code-Game-Studios](https://github.com/Donchitos/Claude-Code-Game-Studios)
- [Unity-Technologies/skills](https://github.com/Unity-Technologies/skills) · [firebase/agent-skills](https://github.com/firebase/agent-skills)
