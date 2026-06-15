# SRPG 기술 설계 문서 (TDD) v2.0

## 2026-06-15 update - `TBD-017R` / `TBD-016R`

- Basic attack model correction: `SrpBasicAttackKind` is resolved by Chebyshev distance, not by `weaponClass`. `dist <= 1` resolves to `Melee`; `dist > 1` resolves to `Firearm`.
- Adjacent melee basic attacks are available to all humanoid units, do not require or spend ammo, do not use firearm LOS/aim line, and do not apply firearm HP-to-PG spillover or firearm cover buffering.
- Non-adjacent basic attacks are firearm attacks. They require `attackRange`, ammo, and `SrpFirearmAim` LOS, spend ammo on execution/simulation, turn the shooter toward the target, and keep the existing firearm HP pressure plus HP-based PG spillover policy.
- Execution now checks adjacent melee state only: `dist <= 1` and target `pg <= 0` or `groggy`, then resolves as a guaranteed kill. Firearm-role units can execute adjacent broken/groggy targets; `weaponClass == Melee` is no longer an execution prerequisite.
- UI/preview routing uses the resolved basic attack kind. Adjacent target hover shows melee/execute expectation and suppresses aim line; non-adjacent firearm target hover shows firearm aim line and firearm preview copy.
- AI simulation records damage by `AttackOutcome.basicAttackKind`, so adjacent attacks performed by Firearm-role units are counted as melee damage. Simulation attack execution also spends ammo only for resolved firearm attacks.
- Data bridge: `SrpBattleState.CreateUnitFromTemplate` gives humanoid templates a default sidearm ammo pool (`DefaultFirearmMaxAmmo`) and a minimum firearm reach (`DefaultHumanFirearmRange`) while leaving `weaponClass` as role/display metadata. A dedicated sidearm/firearm-range data model remains a follow-up decision.
- Overwatch/경계태세 remains a firearm reaction, but arming is based on firearm capability (`maxAmmo > 0`), firearm reach (`attackRange > 1` after the current bridge), ammo, AP/RP, and LOS at trigger time, not `weaponClass == Firearm`. Adjacent targets are always blocked from overwatch firearm reaction.
- Combat balance review correction: HP-based PG vulnerability is a final incoming PG modifier and must run after mitigation/reaction/tag/firearm spillover pressure on both basic attacks and `SrpSkills` damage skills.
- Execution threat policy is state-based: PG 0/groggy targets are executed only by adjacent melee threats (`Chebyshev <= 1`) when resolving against a `SrpBattleState`.
- AI simulation thresholds follow the distance-resolved attack metric bridge: melee PG share floor `0.44`, first-battle player-policy max average rounds `13`, opening heuristic mirror match warn-only.
- `TBD-017S` moves skill selection out of `ContextPanel` into a dedicated `SkillSelectionDrawer` because action choice lists need a readable selection surface, not a state-summary column.
- `SkillSelectionDrawer` is a canvas-level action detail drawer anchored immediately to the right of `CommandRailPanel`, with preferred width 520px, minimum readable width 420px, and 56px minimum row height. Skill labels use `NoWrap + Ellipsis`; detailed skill meaning remains in bottom tactical cards/`InspectorPreviewPanel` through hover/preview.
- The old command-adjacent `ContextPanel` column is removed from the left console because it reads as the previous one-character skill column. The left console must contain only `CommandRailPanel`.
- `SkillSelectionDrawer` must be closable with an explicit `닫기` button and by pressing the `스킬` command again.
- `LogDrawerPanel` starts collapsed by default and expands only when requested.
- `SrpM1OpeningObservationTests` now keeps the camera-render board samples and adds ScreenCapture/GameView HUD samples for skill drawer open, secondary drawer open, log expanded, and log collapsed states; each HUD capture asserts the corresponding visible body/collapsed state before capture.
- `TBD-017R` changes the secondary battle HUD contract from an always-visible floating `SecondaryActionPanel` to a default-closed drawer opened from `SecondaryActionTabStripPanel`.
- Secondary drawer pages are `태세/방향`, `전술 보조`, and `시스템`; `SetSecondaryDrawerOpen` keeps only one page active, requires a readable drawer width of at least 320px, and applies compact page heights (`태세/방향` 210px, `전술 보조` 124px, `시스템` 104px).
- The primary HUD contract is now: fixed `CommandRailPanel`, bottom `ActiveUnitCardPanel`, bottom/right `InspectorPreviewPanel`, right `LogDrawerPanel`, top `TurnOrderTracker`, and no player-facing floating tooltip. PlayMode checks that these panels do not overlap in the default state.
- `TBD-016R` separates occupying cover objects from directional edge cover. `SrpCoverObjectData` is for non-walkable obstacle/ruin tiles and can provide cover mitigation; `SrpCoverSegmentData` remains an edge-based segment that does not block standing on its tile.
- `M1OpeningPrototype` central non-walkable cells are currently interpreted as ruin cover objects. If those cells later become holes/empty voids, add separate terrain semantics instead of reusing `IsCoverTile`.
- Rendering contract: occupying cover objects render at tile center; edge cover segments render as low walls/boards on the tile edge, never as central cubes.

## 2026-06-11 update - `TBD-015` / `TBD-016` / `TBD-017`

- `TBD-015` correction keeps `SrpPreviewEvaluator` threat calculation unchanged and moves only the rendering grammar from tile marker chains to world-space parabolic `LineRenderer` objects. Lines start above the attacker, arc above tile overlays, and end near the move ghost.
- Threat visual tiers are explicit: basic attack is thin/subdued, overwatch is thicker/brighter and includes an endpoint pulse marker. Hover exit must destroy/clear all move-preview threat line objects.
- `TBD-016` first pass adds `SrpTacticalCameraController` for perspective/top orthographic toggle, zoom, pan, and focus. `SrpGameController` configures the board framing but camera input is separated into the component. The projection toggle key is `C`, not `Tab`, and PlayMode keeps a pan/zoom/focus drift guard. Perspective zoom is focus-point + distance based: pan moves `_focusPoint`, wheel zoom changes `_perspectiveDistance`, and zoom must never reuse the camera position as the focus. Top orthographic zoom changes orthographic size around the same focus point.
- Unit facing now has a world-space arrow mesh in addition to HUD text, and cover segments are visible one-tile cube objects. `blocksLineOfSight` cover is rendered with a stronger visual tier.
- `TBD-017` restructures the battle HUD into a fixed `CommandRailPanel`, bottom tactical cards, a state-based `SecondaryActionPanel`, an `InspectorPreviewPanel`, and a collapsible `LogDrawerPanel`. Hidden logs must be inactive/ignored by layout rather than transparent-only. Runtime battle HUD explanation does not use floating tooltip bubbles; hover contracts update bottom tactical cards and `InspectorPreviewPanel` instead.
- Player-facing action completion uses one label, `행동 종료`. `턴 종료` is not exposed as a normal HUD button until a future faction-turn/player-turn layer gives it distinct meaning.

상위 기준:

- `docs/srpg/SRPG_전투규칙_기준서_v2.md`
- `docs/srpg/new/SRPG_NEW_DIALOG_POLICY_LOCK.md`

## 1. 설계 목표

- GDD v2 규칙을 구현 가능한 모듈 계약으로 분해한다.
- 문서 1차 개편에서 확정한 계약을 기준으로 2차 구현과 중간 점검 보정을 진행한다.
- 전투/데이터/HUD/렌더링 경계를 유지해 단계별 리스크를 낮춘다.

## 2. 모듈 아키텍처

```text
SrpGameController
  ├─ SrpBattleState
  ├─ SrpTurnOrder
  ├─ SrpCombatResolver
  ├─ SrpSkills
  ├─ SrpPathfinder
  ├─ SrpGameController.Hud
  └─ SrpGameController.Rendering
```

2차 확장/후속 모듈:

- `SrpOverwatch`: AP 예약/RP 발동, 목표 벡터 기반 LOS, 장애물/유닛/`blocksLineOfSight` segment 차단, 1회 발동/해제, 다중 후보 우선순위 규칙 구현 완료
- `SrpReaction`: 별도 파일 대신 `SrpCombatResolver`의 반응 선택/소비 흐름에 흡수
- `SrpLineOfSight`: 현재는 `SrpOverwatch` helper로 유지, 다른 시스템이 사선을 공유할 때 별도 모듈 분리 검토
- `SrpFirearmAim`: 총기 기본 공격/오버워치 공용 조준 helper. 공격자-대상 중심 360도 벡터의 LOS/장애물/`blocksLineOfSight` 차단만 targetability로 검증하고, 8-sector는 `atan2` 기반 표시/디버그/방향성 판정 보조값으로만 제공한다.

## 3. 핵심 데이터 계약

### 3.1 유닛 런타임 계약

- 기본 필드:
  - `hp`, `maxHp`
  - `pg`, `maxPg`
  - `actionPoints`, `maxActionPoints`
  - `reactionPoints`, `maxReactionPoints`
  - `speed`
  - `stance`
  - `facing`
  - `weaponClass`
- 정책:
  - AP 기본 2, RP 기본 1
  - RP는 반응 전용 자원
  - 공통 `DEF` 런타임 스탯은 새 설계의 목표 상태에서 제거 대상이며, `GRD`는 PG 피해 감쇠 전용으로 정리한다.
  - `Tank` 태그는 `완벽한 수비` 브릿지 구현에 사용하되, 최종 캐릭터 고유 특성 모델과 통합한다.

### 3.2 전투 상태 계약

- 라운드/현재 행동 유닛/큐 상태를 유지한다.
- 교전 상태와 반응 대기 이벤트를 상태에서 추적 가능해야 한다.
- `SrpBattleState`는 Unity 엔진 타입에 의존하지 않는다.
- 총기 능력이 있는 유닛은 런타임 탄약(`ammo/maxAmmo`)을 가지며, 비인접 기본 공격/오버워치 발동 시 탄약을 소비한다. 현행 브릿지는 명시 `maxAmmo`가 없는 인간형 템플릿에도 전장식 총기 기본값 1발을 부여한다.
- 유닛은 엄폐 상태(`coverActive/coverRound/coverSourceX/Y`)를 가질 수 있으며, 1차 구현은 인접 비보행 타일과 같은 칸 edge 기반 방향성 엄폐 segment를 엄폐물로 해석한다.
- 맵은 방향성 엄폐 segment(`SrpCoverSegmentData`)를 가질 수 있으며, 런타임은 클론 가능한 `CoverSegments` 목록으로 보관한다.
- 맵은 상호작용 포인트(`SrpInteractionPointData`)를 가질 수 있으며, 런타임은 클론 가능한 `InteractionPoints` 목록으로 보관한다. 1차 구현은 상하좌우 인접 유닛이 AP 1로 `singleUse` 포인트를 활성화하고, `requiredOwner < 0`이면 누구나, 아니면 해당 owner만 실행 가능하게 한다.
- 내장 프리셋 역할은 분리한다.
  - `M1QaIntegrated`: 최신 기능 연결을 확인하는 QA 맵
  - `M1EngagementLab`: 교전/둘러싸임 고정 조건 QA 맵
  - `M1OpeningPrototype`: 기본 로비 첫 선택이자 첫 전투 판단용 소형 비대칭 전술 맵
- 유닛은 런타임 전투 태그(`SrpCombatTag`)를 가질 수 있다. 이 태그는 고유 유닛 태그(`SrpUnitTags`)와 분리되며, `표식`, `균형 붕괴`, `사살 지시`를 지원한다.

### 3.3 스킬 계약

- 기본 스킬 모델은 쿨다운/충전 기반.
- 안정도는 오버클럭 메타 데이터로 연결 가능해야 한다.
- 오버클럭은 쿨다운 단축, 충전 복구, 다음 스킬 사용 1회 피해/회복 증폭을 지원한다.
- MP/SP 고정 바 의존 필드는 도입하지 않는다.
- 공용 전투 태그는 `표식`, `균형 붕괴`, `사살 지시`를 우선 지원한다.
- 태그는 스킬/패링/적 장교 행동에서 부여하고, 패시브는 태그 대상과 상호작용한다.
- 1차 구현은 `SrpEffectType.ApplyCombatTag`로 태그를 부여하고, 다음 적대 피해 1회에 소모한다.
- `노출`은 저장형 디버프가 아니라 엄폐 밖/개활지/사선 노출을 판정하는 포지션 상태로 취급한다.
- 초기 4인 고유 패시브는 기존 패시브 스킬 계약으로 표현한다.
  - 주인공 `전장 적응`: OnAttackHit, FH +3
  - 탱커 `전열 고정`: OnTakeDamage, PG +2
  - 사격수 `노출 처벌`: OnAttackHit, FH +2
  - 마도사 `전장 해석`: OnTurnStart, FH +2
- 마법 전장 개입 최소 스킬 `전장 장막`은 기존 `BuffStat(pg)` 효과로 아군 PG +4를 적용한다.

## 4. 전투 해석 계약

### 4.1 기본 분기

- `Firearm`: HP 압박 중심
  - 전장식 총기 기본 모델은 1발 고화력이다. 탄약을 소비하며, AP 1 재장전으로 탄약을 최대치까지 회복한다.
  - 기본 공격은 HP 피해를 크게 주고, 실제 HP 피해량의 50%를 PG 피해로 추가 파급한다.
  - 비인접 기본 공격과 오버워치 사선은 `SrpFirearmAim`의 목표 벡터 LOS를 공유한다. 8방향 직선이 아니어도 사거리/LOS/장애물/`blocksLineOfSight` 차단을 통과하면 발포 가능하며, 인접 대상에게는 총기 발포/경계사격을 허용하지 않는다.
  - `SrpAimSector8`은 UI/facing/엄폐 설명용 보조값이며, 발포 가능 여부를 제한하지 않는다.
  - 1차 구현은 최종 HP 피해량 기준으로 파급량을 산정하고, 남은 엄폐 GRD가 있으면 파급 PG를 줄인다. 50% 비율, 반올림 방식, 최소 PG 피해량, GRD 적용 순서는 밸런스 검사와 전투 시뮬레이션 후 조정 가능하게 둔다.
  - 엄폐 중인 원거리 대상에게는 HP/PG 피해 완충을 적용한다.
- `Melee`: PG 붕괴 중심
- `Magic`: 전장 개입 중심(피해 분배는 스킬별)

### 4.2 방어/반응

- 공통 HP 감쇠 `DEF`는 새 설계의 목표 상태에서 제거한다.
- `GRD`는 PG 피해 감쇠 전용으로 해석한다.
- HP 피해는 중대/경미로 분류한다.
  - 중대: 총격, 처단, 기회공격, HP 직격 스킬, 치명타
  - 경미: PG가 살아 있어도 새는 약한 근접/압박 피해
- `완벽한 수비`는 수비 태세, PG 미붕괴, 후방 피격 아님 조건에서 경미 HP 피해를 0으로 만든다.
- 엄폐 완충은 총기 원거리 공격/오버워치 사격에만 적용하고, 근접/마법/처단에는 적용하지 않는다.
- 방향성 엄폐 계약:
  - 기존 비보행 타일 엄폐는 1차 브릿지로 유지한다.
  - 선형/방향성 엄폐는 `SrpCoverSegmentData` 계약으로 분리한다.
  - 필드: `x`, `y`, `edge`, `shape`, `coverDef`, `coverGrd`, `blocksLineOfSight`.
  - `edge`는 타일의 `North/East/South/West` 변을 의미한다.
  - ㄱ자/ㄷ자 엄폐는 한 칸을 차지하는 오브젝트 안에 여러 edge segment를 조합하는 방식으로 표현한다.
  - 피해 완충은 공격자와 방어자 사이의 방향/사선이 해당 edge를 통과할 때만 적용한다.
  - 원거리 총기 피해 완충에 연결한다.
  - `blocksLineOfSight`가 true인 segment는 오버워치와 총기 기본 공격의 사선을 차단한다.
- 공격 태세는 회피 시도 우선, 실패 시 백업 없음.
- 수비 태세는 안정 생존 우선.
- 패링은 주인공 전용 + 정면 근접 강공/스킬 태그 조건.
- 패링 성공은 공격 무효 + 대상 PG 대량 피해 + `균형 붕괴` 태그 부여로 확장한다.

### 4.3 교전/방향

- 방향 판정(정면/측면/후면) 인터페이스를 고정한다.
- 교전 이탈 시 기회공격 훅 포인트를 정의한다.

## 5. 테스트 계약

- 핵심 단위 테스트:
  - 턴 큐 정렬/진행
  - 무기 분기 및 PG 붕괴/처단
  - 태세 선택 효과(공격/수비)
  - 교전/이탈 규칙
  - 반응행동 소비 및 우선순위
- 통합 테스트:
  - HUD의 상단 헤더/정보 바/좌측 콘솔 분리와 전투 상태 반영
  - 좌측 전술 콘솔의 태세/방향/오버클럭/재장전/엄폐/상호작용 직접 조작
  - 하단 현재 유닛 카드와 행동 preview 카드의 숫자+게이지 및 hover 예상 정보 반영
  - 위험영역/의도/상태 문구 일관성
  - QA 프리셋이 최신 스킬·태그·오버워치 사선·상호작용·방향성 엄폐 확인 지점을 포함하는지 검증
  - 첫 전투 프리셋이 플레이어 4인 역할, 비대칭 적 역할, 사선 차단 엄폐, 상호작용 포인트, 패링/완벽한 수비/오버워치 확인 지점을 포함하는지 검증

## 6. 2차 구현 상태와 순서

완료된 1차 기반:

1. 속도 라운드/AP/RP 리셋
2. 교전 상태 저장/클론
3. 기존 DEF/GRD 감쇠 브릿지와 수비 Guard 반응
4. 교전 이탈 비용 브릿지
5. 교전 이탈 기회공격 1차 구현
6. 스킬 쿨다운/충전 및 오버클럭 기본 모델
7. 패링 가능 조건/태그/텔레그래프 1차 구현
8. Dodge/Parry/명시형 ReactionShot 반응행동 브릿지
9. 수비 지속 완충/탱커 다중 대응 브릿지
10. 스킬/유닛 메이커 v2 메타데이터 편집/저장 정합성 확장
11. 중간 점검 보정: 무기 분류 보존, 스킬 AP/PG 별칭, 스킬 피해 그로기, 맵/배치 스킬 필터
12. 유닛 시각 방향성 개선: 원기둥을 facing 기반 쐐기형 삼각기둥으로 교체
13. 교전/둘러싸임 검증 프리셋 보강: `M1EngagementLab` 내장 프리셋 추가
14. RP/HUD 노출 정책 정리: RP 원시 수치 대신 반응 준비/소모/예약 상태 중심 표기
15. 기획 대조 P1 보정: 기본공격 패링 제거, Dodge 확률형 시도/실패 브릿지, 측후면 방어 불리 브릿지 추가
16. HUD/로그 가독성 동기화: 범례/반응/오버워치/스킬 자원/로그 문구 용어 통일 및 PlayMode 스모크 보강
17. 오버워치 사선/횟수/해제 상세 규칙: 목표 벡터 LOS, 장애물/유닛 차단, 예약 1회당 1회 발동, 라운드 리셋 해제
18. 테스트 프리셋 v2 + HUD 레이아웃 개편: 최신 기능 체험용 `M1QaIntegrated`, 상단 헤더/정보 바/좌측 조작 콘솔 분리
19. 전투 직접 조작 UI 보강: 태세 선택, 최종 방향 선택, 오버클럭 실행을 좌측 전술 콘솔에 연결
20. 오버클럭 성능 증폭: 다음 스킬 사용 1회 피해/회복 보너스와 HUD/로그 상태 표기 추가
21. 재장전 AP 행동 1차 구현: 총기 탄약, 기본공격/오버워치 탄약 소비, 좌측 콘솔 재장전 연결
22. 엄폐 AP 행동 1차 구현: 인접 장애물 엄폐 판정, 총기/오버워치 엄폐 완충, 좌측 콘솔 엄폐 연결
23. 상호작용 AP 행동 1차 구현: 맵 상호작용 포인트, AP 1 활성화, owner 제한, 좌측 콘솔 상호작용 연결
24. 개발용 전술 HUD 개선: 하단 현재 유닛 카드, 대상/행동 preview 카드, HP/PG/AP/탄약 게이지와 hover 예상 정보 추가
25. 총기 1발 고화력 조정 + 방향성 엄폐 설계: 기본 총기 탄창 1발, HP 고화력 공식, 선형/방향성 엄폐 데이터 계약 초안 정리
26. 방향성 엄폐 1차 구현: edge 엄폐 데이터/렌더링, 공격자-방어자 방향 기준 총기 피해 완충 연결
27. 11~22 대화 정책 잠금: 공통 DEF 제거 방향, 중대/경미 HP 피해, `완벽한 수비`, 패링 보상, 공용 전투 태그, 초기 4인 역할 정책 문서화
28. 23 대화/추가 논의 정책 반영: 총격으로 실제 받은 HP 피해량의 50%를 PG 피해로 추가 파급하는 v0.2 기준 문서화
29. 전투 플레이 가능성 P1 확장: 총기 HP-PG 파급 보정, 공용 전투 태그 런타임, 패링 성공 보상, `완벽한 수비` 1차 구현, 태그 대표 스킬/프리셋 노출
30. 다음 P1 스프린트: 초기 4인 고유 패시브/대표 스킬 데이터, 방향성 엄폐 사선 차단, 오버워치 후보 우선순위, 마법 전장 개입 최소 스킬 구현
31. 첫 전투 프로토타입 프리셋 분리: `M1QaIntegrated` QA 맵 deprecated 유지, `M1OpeningPrototype` 첫 전투 판단용 기본 맵 추가, 로비 첫 선택/프리셋 검증/PlayMode 초기화 스모크 추가
32. 전투 UX 피드백 레이어 P1: 현재 행동/선택/hover ring, ZOC/교전 unit badge, 턴 시작/종료와 주요 행동 world-space feedback, 피해/회복/선택 flash 추가 및 PlayMode 계약 검증
33. 첫 전투 밸런스 관찰 P2: `M1OpeningPrototype` AI policy matrix를 EditMode에 추가하고, 핵심 정책 케이스 평균 종료 라운드 6~10 범위를 확인
34. 첫 전투 화면 관찰 P2: PlayMode 관찰 테스트로 첫 화면/위험영역/상호작용/ring feedback 캡처 표본을 생성하고, 데이터 보정보다 overlay 문법 후속이 우선임을 기록
35. 타일 overlay 시각 문법 P2: 이동은 중심 marker, 공격/위험과 경계태세는 낮은 밀도 marker, ZOC/패링은 warning ring, 상호작용은 objective marker로 분리하며, tile overlay marker 높이가 PR #61 유닛 발밑 ring 아래에 머무르는 계약을 PlayMode에 추가
36. 행동 순서 패널 분리 P2: 상단 HUD의 현재 유닛/대기열 정보를 상단 우측 `TurnOrderTrackerPanel` icon strip으로 분리하고, 현재 유닛 강조와 다음 3~5명 preview 및 턴 진행 후 갱신을 PlayMode에 추가
37. 총기 발포 방향/조준 문법 P2: 기본 총기 공격과 오버워치를 공용 목표 벡터 LOS helper로 통합하고, 8-sector는 표시/디버그 보조값으로만 유지, hover aim line/preview 문구/facing 갱신 및 EditMode/PlayMode 계약 검증
38. 전투 UX 추가 피드백 후속: 오버워치 사용자 노출 명칭을 `경계태세`로 교체하고, 예약 `경계태세 준비`/발동 `경계사격!` 문구를 고정했으며, 경계태세 사망 직후 렌더링/HUD/행동 순서 갱신과 공격/위험·경계태세 marker 계약을 PlayMode/관찰 QA로 검증
39. 전투 preview 문법 재정렬: 기본 상태는 현재 행동 유닛 이동 marker만 유지하고 일반 공격/경계태세/엄폐/스킬/상호작용 범위는 버튼 hover preview로 분리한다. 이동 칸 hover는 clone 기반 evaluator로 ghost, 목적지 엄폐, 일반 threat line, 경계사격 강화 threat line을 표시하며 행동 순서 token hover는 전장 highlight와 preview/inspector 정보를 갱신한다.
40. 전술 콘솔/로그/행동 종료 UX 정리: 핵심 행동은 command rail에 고정하고, 선택/hover 세부 정보는 context/inspector panel로 분리한다. 로그 drawer는 접힘 시 레이아웃 공간을 반환하며, player-facing 버튼은 `행동 종료` 하나로 통일한다. PlayMode에 패널 존재, 로그 collapse, hover preview 유지, 행동 순서 hover highlight, 카메라 `C` 토글/드리프트 가드를 추가한다.

다음 구현 순서:

1. 카메라/방향/엄폐물 시각화 후속(`TBD-016`)을 처리한다. projection 전환, pan/zoom/focus, 방향 표시 decal, cover object 시각화를 한 묶음으로 본다.
2. 전술 콘솔/로그/행동 종료 UX 정리(`TBD-017`)를 실제 에디터 플레이 화면에서 밀도/겹침/접힘 폭 기준으로 QA한다.
3. 메이커/맵 에디터 UX에서 엄폐 segment와 상호작용 포인트 편집 방식을 검토한다.
4. 초기 4인 고유 패시브/대표 스킬 최종 수치와 전직 연계를 확정한다.
5. 공용 전투 태그/패링/총기 파급 브릿지 수치 밸런스를 검증한다.
6. 메이커 효과유형 드롭다운 성능과 필드 의미 툴팁은 P3 UX로 재현/설계한다.

## 7. 미정 기술 항목

- GRD/경미 HP 피해 계산 공식
- 총기 HP-PG 파급 비율/반올림/최소값/GRD 적용 순서
- 총기 발포 후속 의사결정: 정식 VFX/애니메이션, 무기별 arc, diagonal facing을 도입할지 검토 (`TBD-010` 후속)
- 행동 순서 패널 후속 polish: `TBD-011` 1차 구현은 런타임 생성 얼굴 토큰 icon strip으로 고정했고, 정식 초상화/역할 아이콘/크기 미세 조정은 아트 에셋과 실제 플레이 피드백 후 결정한다.
- 타일 overlay 세부 튜닝: `TBD-012` 공격/위험 범위와 경계태세 범위는 후속 피드백 기준으로 낮은 밀도 marker를 사용한다. 정식 VFX/크기/채도/펄스 같은 화면 미세 조정은 실제 플레이 피드백 후 결정한다.
- 경계태세 후속 UX/버그: 사용자-facing 명칭과 사망 직후 갱신은 구현했다. 정식 경계태세 사격 VFX/애니메이션은 후속 아트 튜닝으로 둔다 (`TBD-014`, `BUG-001`).
- 전투 preview 문법 후속: `TBD-015` 1차는 clone 기반 예측과 hover 문법을 고정했다. 포물선 threat line의 정식 곡선/애니메이션, endpoint pulse, 버튼 hover 미세 연출은 실제 플레이 화면 QA 후 조정한다.
- 전술 콘솔 후속: `TBD-017` 1차는 command rail/inspector/log drawer 계약과 `행동 종료` 단일 버튼 정책을 고정했다. 실제 화면에서 버튼 밀도, 로그 접힘 폭, 스킬 drawer 닫기/배치 감각은 추가 QA 후 조정한다.
- 메이커 효과유형 드롭다운 성능과 필드 의미 툴팁 범위 (`TBD-013`)
- 회피 확률 계산식
- 경계태세 특수 지형 상호작용의 복합 효과
- 패링 PG 피해량, `균형 붕괴` 지속, 실패 패널티 정량 수치
- `완벽한 수비`와 기존 탱커 다중 대응 브릿지 통합 방식
- 공용 전투 태그 수치/지속시간/소모 조건
- 초기 4인 대표 스킬/고유 패시브 최종 수치와 전직 연계
