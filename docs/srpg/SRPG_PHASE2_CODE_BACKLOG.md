# SRPG 코드 2차 착수 백로그 (파일 단위)

기준 문서:

- `SRPG_전투규칙_기준서_v2.md`
- `new/SRPG_NEW_DIALOG_POLICY_LOCK.md`

목적:

- 코드 착수 전 파일 단위 작업 목록을 요구사항 ID(`RQ-*`, `TBD-*`)에 연결한다.

## 0. 현재 착수 상태

2026-06-03 기준 Phase2 1차 전투 코어 기반 작업, 교전 이탈 비용 브릿지, 교전 이탈 기회공격 1차 구현, 스킬 자원 기본 모델, 패링 조건/텔레그래프 1차 구현, 반응행동 파이프라인 1차 구현, 수비 지속 완충/탱커 다중 대응 브릿지, 메이커 메타데이터 UI 확장, 중간 점검 보정, 유닛 시각 방향성 개선, 교전/둘러싸임 검증 프리셋 보강, RP/HUD 노출 정책 정리, 기획 대조 P1 보정, HUD/로그 가독성 동기화, 오버워치 사선/횟수/해제 상세 규칙, 테스트 프리셋 v2 + HUD 레이아웃 개편, 전투 직접 조작 UI 보강, 오버클럭 성능 증폭, 재장전 AP 행동 1차 구현, 엄폐 AP 행동 1차 구현, 상호작용 AP 행동 1차 구현, 개발용 전술 HUD 개선, 총기 1발 고화력 + 방향성 엄폐 설계, 방향성 엄폐 1차 구현, 11~23 대화 정책 잠금/문서 정렬, 전투 플레이 가능성 P1 확장, 다음 P1 초기 4인/방향성 엄폐/오버워치/마법 브릿지, 첫 전투 프로토타입 프리셋 분리를 완료했다.

완료 범위:

- `SrpBattleState.cs`: 교전 상태 저장 구조(`Engagements`)와 클론 안전성 추가
- `SrpUnitRuntime.cs`: 반응행동 종류/마지막 반응 상태 기록 필드 추가
- `SrpCombatResolver.cs`: 상태 기반 공격 해결 오버로드, 기존 DEF/GRD 감쇠 브릿지, 수비 태세 Guard 반응 RP 소비 훅, RP 기반 기회공격 해석 추가
- `SrpTurnOrder.cs`: 라운드 AP/RP 리셋 정책 분리
- `SrpGameController.cs`: 상태 기반 전투 해결, 피격 패시브 훅, 교전 재계산, 교전 이탈 로그 힌트, 기회공격 실행 연결
- `SrpPathfinder.cs`: 교전 중 적 인접 상태를 벗어나는 이동에 임시 이탈 비용 추가
- `SrpSkillData.cs`: 충전/오버클럭 스킬 메타, 런타임 충전 상태, 오버클럭 1회 강화 상태 추가
- `SrpSkills.cs`: 쿨다운/충전 소비·회복, FH 기반 오버클럭 헬퍼, 다음 스킬 사용 1회 피해/회복 증폭 추가
- `SrpUnitTags.cs`: 패링 가능자 플래그 추가
- `SrpSkillData.cs`: 패링 가능 공격/텔레그래프 메타 추가
- `SrpCombatResolver.cs`: 정면/근접/RP/태그 기반 패링 가능 판정 헬퍼 추가
- `SrpCombatResolver.cs`: Dodge/Parry 실제 반응 소비와 피해 무효화 브릿지 추가
- `SrpOverwatch.cs`: AP 예약/RP 발동 기반 명시형 `ReactionShot` 브릿지 추가
- `SrpUnitRuntime.cs`: 오버워치 예약 상태 필드 추가
- `SrpUnitTags.cs`: 탱커 전용 태그 추가
- `SrpUnitRuntime.cs`: 수비 피격 누적 상태 추가
- `SrpCombatResolver.cs`: 수비 태세 후속 피격 완충과 탱커 다중 교전 완충 추가
- `SrpGameController.Hud.cs`: 스킬 목록 충전 상태와 패링 가능 범례/툴팁 간단 표기 추가
- `SrpGameController.Hud.cs`: 오버워치 예약 버튼과 상태 표기 추가
- `SrpGameController.Hud.cs`: RP 원시 수치 대신 반응 준비/소모/예약 상태 중심 표기로 정리
- `SrpGameController.Hud.cs`: 탱커/수비 압박 누적 상태 표기 추가
- `SrpGameController.Rendering.cs`: 공격/스킬 타깃의 패링 가능 청록 오버레이와 오버워치 예약 범위 오버레이 추가
- `SrpGameController.Rendering.cs`: 유닛 원기둥을 전방 팁이 있는 쐐기형 삼각기둥 메시로 교체하고 `SrpFacing` 회전 연결
- `SrpDefaultMaps.cs`, `SrpMapPreset.cs`: 다중 교전/교전 이탈/탱커 대응 검증용 `M1EngagementLab` 프리셋 추가
- `SrpLobbyController.cs`: 로비 프리셋 선택 UI에 교전/포위 검증 랩 추가
- `SrpSkillMakerController.cs`: 충전/오버클럭/패링 메타 입력과 목록 요약 표시 추가
- `SrpUnitMakerController.cs`: v2 AP/RP/PG/속도, 무기/태세/방향, ParryUser/Tank 태그 편집과 legacy AP/PG 동기화 추가
- `SrpBattleState.cs`: 명시 `Firearm` 무기 보존, 맵/배치 스킬 허용·비활성·최대 스킬 수 필터 반영
- `SrpCombatResolver.cs`, `SrpSkills.cs`: 스킬 피해의 PG 0 그로기 흐름과 AP/PG stat 별칭 해석 보정
- `SrpOverwatch.cs`: 오버워치 예약 가능/불가 상태 helper 추가
- `SrpOverwatch.cs`: 8방향 직선 사선, 장애물/유닛 차단, 라운드 일치 검증 추가
- `SrpGameController.cs`, `SrpSkills.cs`: 반응 로그를 RP 원시 수치보다 발동/소모 중심 문구로 정리
- `SrpCombatResolver.cs`: 기본공격 패링 제거, 패링 태그 기반 정면 근접 스킬 패링 제한, Dodge 확률형 시도/실패 브릿지, 측후면 방어 불리 브릿지 추가
- `SrpGameController.Hud.cs`: 실제 오버레이와 맞춘 공통 범례, 반응/오버워치/스킬 자원 용어 통일, PlayMode 테스트용 HUD 관측 헬퍼 추가
- `SrpGameController.Hud.cs`: 상단 전투 헤더, 보조 정보 바, 좌측 조작 콘솔, 우측 로그로 HUD 레이아웃 분리
- `SrpDefaultMaps.cs`: `M1QaIntegrated`를 최신 스킬 자원/패링 가능 스킬/오버워치 사선/탱커 확인용 프리셋으로 갱신
- `SrpBattleState.cs`: 로컬 스킬 DB가 구버전이어도 내장 QA 프리셋의 v2 기본 스킬 메타가 누락되지 않게 보강
- `SrpGameController.Hud.cs`: 태세 선택, 최종 방향 선택, 오버클럭 실행 버튼을 좌측 전술 콘솔에 추가
- `SrpGameController.cs`, `SrpSkills.cs`: 태세/방향 직접 변경과 오버클럭 가능 조건 helper를 전투 입력 흐름에 연결
- `SrpGameController.Hud.cs`, `SrpSkillMakerController.cs`: 오버클럭 증폭/강화 대기 상태와 메이커 입력 필드 추가
- `SrpGameController.cs`, `SrpSkills.cs`: 공격/기회공격/오버워치/스킬/반응 로그를 이벤트 단위 문구로 정리
- `SrpUnitRuntime.cs`, `SrpMapFile.cs`, `SrpBattleState.cs`: 총기 유닛 탄약/최대 탄약 계약과 스폰 기본 탄약 초기화 추가
- `SrpGameController.cs`, `SrpOverwatch.cs`: 기본 공격/오버워치 탄약 검사·소비, AP 1 재장전 행동 연결
- `SrpGameController.Hud.cs`, `SrpUnitMakerController.cs`: 좌측 전술 콘솔 재장전 버튼, HUD 탄약 표시, 유닛 메이커 최대 탄약 입력 추가
- `SrpUnitRuntime.cs`, `SrpBattleState.cs`: 엄폐 상태와 인접 비보행 타일 엄폐 판정 추가
- `SrpCombatResolver.cs`: 총기 기본 공격/오버워치 사격의 엄폐 완충과 근접/마법/처단 비적용 규칙 추가
- `SrpGameController.cs`, `SrpGameController.Hud.cs`, `SrpGameController.Rendering.cs`: 좌측 전술 콘솔 엄폐 버튼, HUD 엄폐 상태, 엄폐 오버레이 추가
- `SrpMapFile.cs`, `SrpBattleState.cs`: 맵 상호작용 포인트 계약과 런타임 클론/인접 탐색/활성화 helper 추가
- `SrpGameController.cs`, `SrpGameController.Hud.cs`, `SrpGameController.Rendering.cs`: AP 1 상호작용 실행, 좌측 전술 콘솔 상호작용 버튼, HUD 상태, 상호작용 오버레이 추가
- `SrpDefaultMaps.cs`: `M1QaIntegrated`에 직접 테스트용 상호작용 포인트 추가
- `SrpGameController.Hud.cs`: 좌측 하단 현재 유닛 카드, 우측 하단 행동 preview 카드, HP/PG/AP/탄약 숫자+게이지 표시 추가
- `SrpGameController.cs`: 유닛/타일 hover 상태를 이동/공격/스킬/상호작용 preview 카드에 읽기 전용으로 연결
- `SrpUnitRuntime.cs`: 공용 전투 태그(`SrpCombatTag`) 저장/갱신/소모 계약 추가
- `SrpSkillData.cs`, `SrpSkills.cs`: `ApplyCombatTag` 효과 타입과 태그 부여/소모 로그 연결
- `SrpCombatResolver.cs`: 총기 HP-PG 파급을 최종 HP 피해량 기준으로 보정하고 남은 엄폐 GRD 감쇠 순서 고정
- `SrpCombatResolver.cs`: 패링 성공 시 공격자 PG 피해와 `균형 붕괴` 태그 부여 추가
- `SrpCombatResolver.cs`: `Tank` 태그, 수비 태세, PG 미붕괴, 후방 아님 조건의 `완벽한 수비` 1차 구현
- `SrpGameController.cs`, `SrpGameController.Hud.cs`: 전투 태그, 총기 파급, 패링 보상, 완벽한 수비 로그/HUD 표시 추가
- `SrpDefaultSkills.cs`, `SrpDefaultUnits.cs`, `SrpDefaultMaps.cs`: `전술 표식`, `균형 교란`, `사살 지시`와 `M1QaIntegrated` 노출 추가
- `SrpSkillMakerController.cs`: 전투 태그 효과 입력 후보 추가
- `SrpDefaultSkills.cs`, `SrpDefaultUnits.cs`, `SrpDefaultMaps.cs`: 초기 4인 고유 패시브(`전장 적응`, `전열 고정`, `노출 처벌`, `전장 해석`)와 마법 전장 개입 스킬 `전장 장막` 추가
- `SrpBattleState.cs`, `SrpOverwatch.cs`, `SrpCombatResolver.cs`: `blocksLineOfSight` edge segment를 오버워치와 총기 기본 공격 사선 차단에 연결
- `SrpOverwatch.cs`, `SrpGameController.cs`: 여러 오버워치 후보 우선순위를 거리/속도/unit id 순으로 선택
- `SrpDefaultMaps.cs`: `M1QaIntegrated`를 플레이어 4인 역할 검증과 사선 차단 엄폐 segment 확인용으로 갱신
- `SrpMapPreset.cs`, `SrpDefaultMaps.cs`: 첫 전투 프로토타입 내장 프리셋 `M1OpeningPrototype` 추가
- `SrpGameSettings.cs`, `SrpGameController.cs`, `SrpLobbyController.cs`: 기본 전투 진입값과 로비 첫 선택을 `M1OpeningPrototype`으로 교체하고 `M1QaIntegrated`는 로비 후순위 QA 선택지와 코드/자동 테스트용 deprecated 프리셋으로 유지
- `SrpDefaultSkills.cs`: 플레이어가 보는 기본 스킬 설명에서 `브릿지`/`호환용` 표현을 줄이고 임시 수치는 문서로 분리
- EditMode 테스트: 교전/반응 클론 독립성, Guard RP 소비, 라운드 RP 리셋, 교전 이탈 비용, 기회공격 발생/미발생, 쿨다운/충전/오버클럭, 오버클럭 성능 증폭, 패링 가능/불가 조건, Parry/Dodge/명시형 ReactionShot, 오버워치 사선/차단/후보 우선순위, 수비 지속 완충/탱커 다중 대응, 메이커 JSON 메타 보존, 중간 점검 회귀, 유닛 뷰 방향 회전, 프리셋 기반 교전/포위 검증, QA 프리셋 v2 스킬/태그/초기 4인/사선 차단 검증, 오버클럭 가능 조건, 오버워치 상태 helper, 탄약/재장전/오버워치 탄약 소비, 엄폐 판정/완충/사선 차단, 상호작용 탐색/실행/owner 제한/클론/JSON/프리셋, 총기 HP-PG 파급, 공용 전투 태그, 패링 보상, 완벽한 수비, 마법 전장 개입 검증 추가
- PlayMode 테스트: HUD의 반응 상태 표기(`반응: 준비/소모/예약`), 상단 헤더/좌측 콘솔/하단 유닛 카드/행동 preview 카드, 태세/방향/오버클럭/재장전/엄폐/상호작용 직접 조작, 범례, 오버워치 버튼, hover 문구, 로그 핵심 문구를 스모크 검증

주의:

- 원기획의 교전 이탈 규칙은 이동력 추가 소모가 아니라 `기회공격` 리스크 중심이다.
- 현재 `+1` 비용은 기존 ZOC 비용 모델에 맞춘 브릿지 구현이며, 기회공격 1차 구현 후에도 이동 선택 단계의 위험 힌트로 유지한다.
- `ReactionShot` 기록은 교전 이탈 기회공격과 명시형 오버워치 발동 모두에 사용한다.
- 현재 오버클럭 비용은 기존 `frozenHeart` 값을 안정도 대용으로 사용하고, 좌측 전술 콘솔의 오버클럭 버튼으로 실행한다. 오버클럭은 쿨다운 단축, 충전 복구, 다음 스킬 사용 1회 피해/회복 증폭을 지원한다. 별도 안정도 수치계/전용 UI는 후속으로 남긴다.
- 현재 재장전은 총기 유닛 전용 AP 1 행동이다. 기본 공격과 오버워치 발동은 탄약 1을 소비하며, 오버워치 예약도 탄약이 있어야 가능하다. 명시 `maxAmmo`가 없는 총기 기본 탄창은 전장식 총기 정책에 따라 1발이며, 비총기 유닛에는 탄약 UI/제한을 적용하지 않는다.
- 현재 엄폐는 기존 비보행 장애물 타일에 인접하거나 같은 칸의 방향성 edge segment에 선 유닛이 AP 1로 취하는 1차 행동이다. 엄폐 완충은 총기 원거리 공격/오버워치 사격에만 적용하며, 근접/마법/처단에는 적용하지 않는다. 선형/방향성 엄폐는 `SrpCoverSegmentData { x, y, edge, shape, coverDef, coverGrd, blocksLineOfSight }` 계약으로 분리하고, ㄱ자/ㄷ자 엄폐는 여러 edge segment 조합으로 표현한다. 공격자-방어자 방향 기준 피해 완충과 `blocksLineOfSight` 사선 차단은 구현했다. 맵 메이커 편집 UI는 후속으로 남긴다.
- 현재 상호작용은 맵의 `SrpInteractionPointData`에 인접한 유닛이 AP 1로 활성화하는 1차 행동이다. `requiredOwner < 0`이면 누구나 가능하고, 아니면 해당 owner만 가능하다. 복잡한 시나리오 스크립트와 맵 메이커 전용 편집 UI는 후속으로 남긴다.
- 현재 Parry/Dodge는 피해 무효화 브릿지로 구현했다. 최종 회피 확률식, 패링 보상/실패 패널티 수치는 `TBD-003`, `TBD-005` 후속으로 남긴다.
- 현재 Dodge는 임시 확률형 브릿지로 성공/실패만 분기한다. 최종 확률식과 스탯/방향 가중치는 `TBD-003` 후속으로 남긴다.
- 현재 방향 방어 불리는 측후면 추가 피해 브릿지로만 반영한다. 공통 DEF 제거, GRD의 PG 전용화, 경미 HP 피해 공식은 `TBD-002` 후속으로 남긴다.
- 현재 총기 기본 공격은 최종 HP 피해량의 50%를 PG 피해로 추가 파급한다. 산정된 파급 PG는 남은 엄폐 GRD로 줄어들 수 있으며, 최종 비율/반올림/최소값은 `TBD-009` 후속 검증 대상으로 둔다.
- 현재 명시형 `ReactionShot`은 AP 예약/RP 발동, 8방향 직선 사선, 장애물/유닛/사선 차단 엄폐 segment 차단, 예약 1회당 1회 발동, 라운드 리셋 해제를 1차 상세 규칙으로 사용한다. 여러 오버워치 후보는 가까운 사수, 빠른 사수, 낮은 unit id 순으로 고른다. 추가 특수 지형 상호작용은 `TBD-004` 후속으로 남긴다.
- 현재 탱커 다중 대응은 `Tank` 태그와 다중 교전 수 기반 감쇠 브릿지를 유지하고, `완벽한 수비` 1차 구현은 수비 태세/PG 미붕괴/후방 아님 조건에서 경미 HP 피해를 무효화한다. Tank 태그와 캐릭터 고유 패시브의 최종 통합은 `TBD-006` 후속으로 남긴다.
- 현재 패링은 피해 무효화에 더해 공격자 PG 8 피해와 `균형 붕괴` 태그 부여를 적용한다. 보상 수치와 실패 패널티는 `TBD-005` 후속으로 남긴다.
- 현재 공용 전투 태그(`표식`, `균형 붕괴`, `사살 지시`) 런타임 계약은 저장/갱신/표시/다음 적대 피해 1회 소모까지 구현했다. `노출`은 디버프가 아니라 포지션 상태로만 구현한다.
- 현재 초기 4인 고유 패시브는 기존 패시브 스킬 계약으로 구현한다. 주인공 `전장 적응`은 공격 적중 FH +3, 탱커 `전열 고정`은 피격 후 PG +2, 사격수 `노출 처벌`은 공격 적중 FH +2, 마도사 `전장 해석`은 턴 시작 FH +2를 사용한다. 최종 이름/전직 연계/수치는 `TBD-008` 후속으로 남긴다.
- 현재 마법 전장 개입은 `전장 장막`의 아군 PG +4 브릿지와 기존 `전술 표식`/`균형 교란` 제어 축까지 구현했다. 지형 생성/광역 장판/강제 이동은 후속으로 남긴다.

검증:

- Unity EditMode 테스트 통과: `76 passed / 0 failed`
- Unity PlayMode 테스트 통과: `6 passed / 0 failed`
- 실행 명령:
  - `Unity.exe -batchmode -automated -projectPath <repo> -runTests -testPlatform EditMode -testResults <repo>/TestResults-EditMode.xml -logFile <repo>/UnityTest-EditMode.log`

다음 착수 후보:

- P1/P2: 맵 메이커 엄폐 segment 편집 UI
- P2: `M1OpeningPrototype` 실제 플레이/AI 시뮬레이션 기반 첫 전투 밸런스 보정
- P2: 초기 4인 고유 패시브/대표 스킬 최종 이름, 전직 연계, 밸런스 수치 확정
- P2: 특수 지형 상호작용의 복합 효과와 승리 조건 연동
- P2: 공용 전투 태그/패링/총기 파급 브릿지 수치 밸런스 검증
- P2: 총기 발포 방향/조준 문법 재정의 (`TBD-010`)
  - 기본 공격, 오버워치, 발포 연출/overlay가 모두 8방향 직선 사선처럼 보이는 현상을 확인한다.
  - 오버워치의 8방향 직선 사선 규칙과 총기 기본 발포 문법을 같은 계약으로 둘지 분리할지 결정한다.
- P2: 행동 순서 패널 분리 (`TBD-011`)
  - 상단 HUD에 섞인 현재 턴 정보와 별도 initiative/turn order tracker의 책임을 분리한다.
  - 현재 유닛 강조, 다음 3~5명 미리보기, 초상/아이콘 열 구성을 검토한다.
- P2/P3: 타일 overlay 시각 문법 개편 (`TBD-012`)
  - 이동 가능 범위는 중심 원/작은 그림자, 공격 가능 범위는 외곽 danger/테두리, ZOC는 얇은 경고 ring 후보로 분리 검토한다.
  - 오버워치/패링/상호작용은 동일 문법의 색상 변주로 충분한지 검증한다.
- P3: 메이커 화면 효과유형 드롭다운/툴팁 UX (`TBD-013`)
  - 드롭다운 스크롤 지연은 재현 확인 후 성능 개선 범위를 정한다.
  - 입력 가능 값과 의미 툴팁은 전투 플레이 가독성보다 후순위로 둔다.

## 0-1. 완료: 기획 대조 P1 보정

목표:

- 1차 스프린트 구현 중 브릿지로 넓게 잡힌 전투 규칙을 `SRPG_전투규칙_기준서_v2.md`의 확정 규칙에 맞춘다.
- 최종 수치가 필요한 항목은 과도하게 확정하지 않고, 테스트 가능한 최소 브릿지와 `TBD-*` 후속을 분리한다.

작업 범위:

| 우선순위 | 파일 | 작업 | 요구사항 ID |
| --- | --- | --- | --- |
| P1-0-1 | `Assets/Scripts/SRPG/SrpCombatResolver.cs` | 패링을 패링 태그가 있는 정면 근접 스킬 위협으로 제한하고 기본공격 패링을 제거 | `RQ-008`, `RQ-009`, `TBD-005` |
| P1-0-2 | `Assets/Scripts/SRPG/SrpCombatResolver.cs` | Dodge를 조건형 완전 무효 브릿지에서 확률형 시도/실패 흐름으로 보정 | `RQ-006`, `TBD-003` |
| P1-0-3 | `Assets/Scripts/SRPG/SrpCombatResolver.cs` | 측후면 피격 방어 불리 최소 브릿지를 추가하되 최종 수치는 `TBD-002`로 유지 | `RQ-004`, `TBD-002` |
| P1-0-4 | `Assets/Tests/EditMode/Editor/SrpM1RuleSpecTests.cs` | 패링/회피/방향 보정 회귀 테스트 추가 및 기존 기대값 갱신 | `RQ-004`, `RQ-006`, `RQ-008`, `RQ-009` |
| P1-0-5 | `docs/srpg/SRPG_BACKLOG.md`, `docs/srpg/SRPG_TDD.md`, `docs/srpg/SRPG_GDD_TEST_TRACEABILITY.md`, `docs/srpg/SRPG_CHANGELOG.md` | 기획 대조 결과와 P1 보정 범위를 문서화 | `TBD-001` |

비범위:

- 회피 확률식의 최종 수치/스탯 가중치는 `TBD-003` 후속으로 남긴다.
- 패링 성공 보상/실패 패널티 정량 수치는 `TBD-005` 후속으로 남긴다.
- 턴 시작 태세 선택 UI/UX는 HUD/입력 UX 스프린트 후보로 분리한다.
- 방향별 최종 DEF/GRD 수치표는 `TBD-002` 후속으로 남긴다.

검증:

- `ReadLints` 변경 파일 진단 통과
- Unity EditMode 테스트 통과: `43 passed / 0 failed`
- PlayMode 테스트는 HUD 직접 변경이 없어 기존 `4 passed / 0 failed` 상태를 유지

## 0-2. 완료: HUD/로그 가독성 동기화

목표:

- 1차 스프린트에서 구현한 전투 규칙을 플레이어가 HUD와 로그만 보고 이해할 수 있게 한다.
- 새 규칙을 추가하기보다, 기존 AP/반응/교전/패링/오버워치/스킬 자원 표기를 같은 용어와 색상 체계로 정렬한다.
- 전수 점검에서 확인된 PlayMode/HUD 기대값 불일치가 다시 생기지 않도록 테스트 기준을 보강한다.

작업 범위:

| 우선순위 | 파일 | 작업 | 요구사항 ID |
| --- | --- | --- | --- |
| P1-1 | `Assets/Scripts/SRPG/SrpGameController.Hud.cs` | HUD 범례를 실제 오버레이 레이어와 동기화하고, 반응 상태/오버워치/스킬 목록 용어를 통일 | `TBD-001`, `RQ-002`, `RQ-003`, `RQ-009`, `RQ-011` |
| P1-2 | `Assets/Scripts/SRPG/SrpGameController.cs`, `Assets/Scripts/SRPG/SrpSkills.cs` | 이동/공격/스킬/반응/상태 로그 문구를 이벤트 단위로 정리하고 중복·모호 표현 제거 | `TBD-001`, `RQ-003`, `RQ-010` |
| P1-3 | `Assets/Scripts/SRPG/SrpGameController.Rendering.cs` | 이동 hover, 위험 타일, 유닛 미리보기, 패링 가능, 오버워치 색상 의미를 HUD 범례와 대조 | `TBD-001`, `RQ-009`, `RQ-010` |
| P1-4 | `Assets/Tests/PlayMode/SrpM1PlayModeTests.cs`, `Assets/Tests/PlayMode/SrpM1AiPlaySampleTests.cs` | HUD 스모크 테스트를 범례, 반응 상태, 오버워치 버튼 라벨, hover 문구까지 확장 | `TBD-001`, `RQ-002`, `RQ-003` |
| P1-5 | `docs/srpg/SRPG_BACKLOG.md`, `docs/srpg/SRPG_CHANGELOG.md`, `docs/srpg/SRPG_GDD_TEST_TRACEABILITY.md`, `docs/srpg/SRPG_TDD.md` | 완료 범위, 테스트 수, 후속 후보를 문서 동기화 | `TBD-001` |

전수 점검에서 분리한 비범위:

- 오버워치 사선/각도/발동 횟수/해제 1차 규칙은 완료했다. 여러 후보 우선순위와 특수 지형 상호작용은 `TBD-004` 후속으로 남긴다.
- 탱커 다중 대응 수치/태세 종속 여부 확정은 `TBD-006` 후속으로 남기되, 현재 브릿지 동작은 문서에 명시한다.
- `Cleave` 같은 특수 스킬 피해가 공용 피해 파이프라인을 우회하는 문제는 스킬/밸런스 스프린트에서 별도 판단한다.
- 유닛 메이커 `maxSkills` 무음 잘림 경고는 메이커 UX 스프린트 후보로 분리한다.

검증:

- `ReadLints` 변경 파일 진단 통과
- Unity EditMode 테스트 통과: `43 passed / 0 failed`
- Unity PlayMode 테스트 통과: `4 passed / 0 failed`
- 테스트 산출물 확인 후 삭제

## 0-3. 완료: 오버워치 사선/횟수/해제 상세 규칙

목표:

- `TBD-004`로 남아 있던 명시형 `ReactionShot`의 사선/횟수/해제 정책을 1차 규칙으로 고정한다.
- 오버워치 표시 범위와 실제 발동 조건이 어긋나지 않게 한다.

작업 범위:

| 우선순위 | 파일 | 작업 | 요구사항 ID |
| --- | --- | --- | --- |
| P1-3-1 | `Assets/Scripts/SRPG/SrpOverwatch.cs` | 8방향 직선 사선, 장애물/유닛 차단, 라운드 일치 검증 추가 | `RQ-003`, `TBD-004` |
| P1-3-2 | `Assets/Scripts/SRPG/SrpGameController.Rendering.cs` | 오버워치 범위 오버레이를 실제 사선 가능 타일과 동기화 | `TBD-001`, `TBD-004` |
| P1-3-3 | `Assets/Tests/EditMode/Editor/SrpM1RuleSpecTests.cs` | 사선 밖/차단 사선 회귀 테스트 추가 | `RQ-003`, `TBD-004` |
| P1-3-4 | `docs/srpg/SRPG_전투규칙_기준서_v2.md`, `docs/srpg/SRPG_BACKLOG.md`, `docs/srpg/SRPG_TDD.md`, `docs/srpg/SRPG_GDD_TEST_TRACEABILITY.md`, `docs/srpg/SRPG_CHANGELOG.md` | 오버워치 1차 상세 규칙과 테스트 결과 문서화 | `TBD-004` |

1차 확정:

- 총기 유닛만 오버워치를 예약한다.
- 발동 사선은 8방향 직선(가로/세로/대각선)으로 제한한다.
- 사선 중간의 장애물 타일 또는 유닛은 발동을 차단한다.
- 예약 1회당 발동은 1회이며, 발동 즉시 예약을 해제한다.
- 라운드 리셋 시 남아 있는 예약은 해제한다.

후속 비범위:

- 여러 오버워치 후보가 동시에 가능한 경우의 우선순위
- 엄폐/특수 지형과 오버워치의 상호작용

검증:

- `ReadLints` 변경 파일 진단 통과
- Unity EditMode 테스트 통과: `45 passed / 0 failed`
- Unity PlayMode 테스트 통과: `4 passed / 0 failed`
- 테스트 산출물 확인 후 삭제

## 0-4. 완료: 테스트 프리셋 v2 + HUD 레이아웃 개편

목표:

- 최신 Phase2 전투 규칙을 플레이 중 직접 확인할 수 있도록 `M1QaIntegrated`를 갱신한다.
- 전투 상태 정보와 조작 콘솔을 분리해 HUD 가독성과 테스트 편의성을 높인다.

작업 범위:

| 우선순위 | 파일 | 작업 | 요구사항 ID |
| --- | --- | --- | --- |
| P1-4-1 | `Assets/Scripts/SRPG/SrpDefaultMaps.cs`, `Assets/Scripts/SRPG/SrpBattleState.cs` | 스킬 자원/패링 가능 스킬/오버워치 사선/탱커 확인용 `M1QaIntegrated` 갱신 및 구버전 로컬 스킬 DB 보강 | `RQ-003`, `RQ-009`, `RQ-011`, `TBD-001` |
| P1-4-2 | `Assets/Scripts/SRPG/SrpGameController.Hud.cs` | 상단 전투 헤더, 보조 정보 바, 좌측 조작 콘솔, 우측 로그로 HUD 레이아웃 분리 | `RQ-002`, `RQ-003`, `TBD-001` |
| P1-4-3 | `Assets/Tests/EditMode/Editor/SrpMakerMetadataTests.cs`, `Assets/Tests/PlayMode/SrpM1PlayModeTests.cs`, `Assets/Tests/PlayMode/SrpM1AiPlaySampleTests.cs` | 프리셋 v2와 HUD 레이아웃 스모크 테스트 갱신 | `RQ-002`, `RQ-009`, `RQ-011`, `TBD-001` |
| P1-4-4 | `docs/srpg/SRPG_BACKLOG.md`, `docs/srpg/SRPG_TDD.md`, `docs/srpg/SRPG_GDD_TEST_TRACEABILITY.md`, `docs/srpg/SRPG_CHANGELOG.md`, `docs/srpg/SRPG_README.md` | 완료 범위와 테스트 결과 문서화 | `TBD-001` |

비범위:

- 정식 튜토리얼/가이드 팝업
- 새 전투 규칙 추가
- 별도 UI 프리팹/아트 리소스 제작

검증:

- `ReadLints` 변경 파일 진단 통과
- Unity EditMode 테스트 통과: `46 passed / 0 failed`
- Unity PlayMode 테스트 통과: `4 passed / 0 failed`
- 테스트 산출물 확인 후 삭제

## 0-5. 완료: 전투 직접 조작 UI 보강

목표:

- 구현은 되어 있지만 전투 중 직접 설정할 UI가 부족했던 태세, 방향, 오버클럭 조작을 좌측 전술 콘솔에 연결한다.
- 자동 반응행동과 미구현 코어 기능을 직접 조작 UI 범위에서 분리한다.

작업 범위:

| 우선순위 | 파일 | 작업 | 요구사항 ID |
| --- | --- | --- | --- |
| P1-5-1 | `Assets/Scripts/SRPG/SrpGameController.Hud.cs` | 태세 선택, 방향 선택, 오버클럭 버튼과 테스트 관측 helper 추가 | `RQ-005`, `RQ-011`, `RQ-012`, `TBD-001` |
| P1-5-2 | `Assets/Scripts/SRPG/SrpGameController.cs` | 태세 변경 제한, 방향 변경, 오버클럭 실행을 전투 상태/Undo/로그/HUD에 연결 | `RQ-005`, `RQ-011`, `RQ-012` |
| P1-5-3 | `Assets/Scripts/SRPG/SrpSkills.cs` | 오버클럭 실행 전 가능 조건 helper 추가 | `RQ-011`, `RQ-012` |
| P1-5-4 | `Assets/Tests/EditMode/Editor/SrpMakerMetadataTests.cs`, `Assets/Tests/PlayMode/SrpM1PlayModeTests.cs` | 오버클럭 가능 조건 및 태세/방향/오버클럭 직접 조작 PlayMode 검증 | `RQ-005`, `RQ-011`, `RQ-012` |
| P1-5-5 | `docs/srpg/SRPG_BACKLOG.md`, `docs/srpg/SRPG_TDD.md`, `docs/srpg/SRPG_GDD_TEST_TRACEABILITY.md`, `docs/srpg/SRPG_CHANGELOG.md`, `docs/srpg/SRPG_README.md` | 완료 범위와 테스트 결과 문서화 | `TBD-001` |

비범위:

- 패링/회피/가드/기회공격/오버워치 발동 직접 선택 UI
- 엄폐/재장전/상호작용 UI

검증:

- `ReadLints` 변경 파일 진단 통과
- Unity EditMode 테스트 통과: `47 passed / 0 failed`
- Unity PlayMode 테스트 통과: `5 passed / 0 failed`
- 테스트 산출물 확인 후 삭제

## 0-6. 완료: 오버클럭 성능 증폭

목표:

- 안정도 오버클럭의 남은 확정 기능인 일시 성능 증폭을 기존 스킬 자원 모델에 통합한다.
- 지속 턴/중첩/범위 증폭은 도입하지 않고 다음 액티브 스킬 사용 1회 강화로 제한한다.

작업 범위:

| 우선순위 | 파일 | 작업 | 요구사항 ID |
| --- | --- | --- | --- |
| P1-6-1 | `Assets/Scripts/SRPG/SrpSkillData.cs` | 오버클럭 위력 보너스 메타와 런타임 강화 대기 상태 추가 | `RQ-012` |
| P1-6-2 | `Assets/Scripts/SRPG/SrpSkills.cs` | 오버클럭 실행 시 다음 사용 1회 피해/회복 보너스 적용 및 소모 | `RQ-011`, `RQ-012` |
| P1-6-3 | `Assets/Scripts/SRPG/SrpGameController.Hud.cs`, `Assets/Scripts/SRPG/SrpSkillMakerController.cs` | HUD/스킬 목록/로그/메이커에 증폭과 강화 대기 상태 표시 | `RQ-012`, `TBD-001` |
| P1-6-4 | `Assets/Tests/EditMode/Editor/SrpM1CoreTests.cs`, `Assets/Tests/EditMode/Editor/SrpMakerMetadataTests.cs`, `Assets/Tests/PlayMode/SrpM1PlayModeTests.cs` | 성능 증폭 적용/소모와 UI 표기 회귀 테스트 | `RQ-012` |
| P1-6-5 | `docs/srpg/SRPG_BACKLOG.md`, `docs/srpg/SRPG_TDD.md`, `docs/srpg/SRPG_GDD_TEST_TRACEABILITY.md`, `docs/srpg/SRPG_CHANGELOG.md`, `docs/srpg/SRPG_README.md` | 완료 범위와 테스트 결과 문서화 | `TBD-001` |

비범위:

- 지속 턴/중첩/범위 증폭
- 별도 안정도 수치계/전용 UI

검증:

- `ReadLints` 변경 파일 진단 통과
- Unity EditMode 테스트 통과: `48 passed / 0 failed`
- Unity PlayMode 테스트 통과: `5 passed / 0 failed`
- 테스트 산출물 확인 후 삭제

## 0-7. 완료: 재장전 AP 행동 1차 구현

목표:

- 기준서의 AP 능동 행동 후보 중 재장전을 첫 확정 기능으로 구현한다.
- 총기 유닛에만 탄약 제한을 적용하고, 비총기 유닛의 기존 공격 흐름은 유지한다.

작업 범위:

| 우선순위 | 파일 | 작업 | 요구사항 ID |
| --- | --- | --- | --- |
| P1-7-1 | `Assets/Scripts/SRPG/SrpUnitRuntime.cs`, `Assets/Scripts/SRPG/SrpMapFile.cs`, `Assets/Scripts/SRPG/SrpBattleState.cs` | 탄약/최대 탄약 런타임·템플릿 계약과 총기 기본 탄약 초기화 추가 | `RQ-002` |
| P1-7-2 | `Assets/Scripts/SRPG/SrpGameController.cs`, `Assets/Scripts/SRPG/SrpOverwatch.cs` | 기본 공격과 오버워치 예약/발동에 탄약 검사/소비 연결 | `RQ-002`, `RQ-003` |
| P1-7-3 | `Assets/Scripts/SRPG/SrpGameController.Hud.cs`, `Assets/Scripts/SRPG/SrpUnitMakerController.cs` | 좌측 콘솔 재장전 버튼, HUD 탄약 상태, 메이커 최대 탄약 입력 추가 | `RQ-002`, `TBD-001` |
| P1-7-4 | `Assets/Tests/EditMode/Editor/SrpM1CoreTests.cs`, `Assets/Tests/EditMode/Editor/SrpMakerMetadataTests.cs`, `Assets/Tests/PlayMode/SrpM1PlayModeTests.cs` | 탄약 소비/차단/재장전/클론/오버워치 탄약 소비와 HUD 표기 회귀 테스트 | `RQ-002`, `RQ-003` |
| P1-7-5 | `docs/srpg/SRPG_BACKLOG.md`, `docs/srpg/SRPG_TDD.md`, `docs/srpg/SRPG_GDD_TEST_TRACEABILITY.md`, `docs/srpg/SRPG_CHANGELOG.md`, `docs/srpg/SRPG_README.md` | 완료 범위와 테스트 결과 문서화 | `TBD-001` |

비범위:

- 엄폐/상호작용 AP 행동
- 탄창별 재장전 시간, 탄종, 잔탄 공유 자원
- 비총기 무기 탄약 제한

검증:

- `ReadLints` 변경 파일 진단 통과
- Unity EditMode 테스트 통과: `51 passed / 0 failed`
- Unity PlayMode 테스트 통과: `5 passed / 0 failed`
- 테스트 산출물 확인 후 삭제

## 0-8. 완료: 엄폐 AP 행동 1차 구현

목표:

- 재장전 다음 AP 행동으로 엄폐를 구현한다.
- 별도 엄폐 타일 스키마를 새로 도입하지 않고, 기존 비보행 장애물 타일을 인접 엄폐물로 해석한다.

작업 범위:

| 우선순위 | 파일 | 작업 | 요구사항 ID |
| --- | --- | --- | --- |
| P1-8-1 | `Assets/Scripts/SRPG/SrpUnitRuntime.cs`, `Assets/Scripts/SRPG/SrpBattleState.cs` | 엄폐 런타임 상태와 인접 비보행 타일 엄폐 판정 추가 | `RQ-002`, `RQ-004` |
| P1-8-2 | `Assets/Scripts/SRPG/SrpCombatResolver.cs` | 총기 원거리 공격/오버워치 사격 엄폐 완충과 근접/마법/처단 비적용 고정 | `RQ-004`, `TBD-004` |
| P1-8-3 | `Assets/Scripts/SRPG/SrpGameController.cs`, `Assets/Scripts/SRPG/SrpGameController.Hud.cs`, `Assets/Scripts/SRPG/SrpGameController.Rendering.cs` | 좌측 콘솔 엄폐 버튼, HUD 엄폐 상태, 엄폐 오버레이 추가 | `RQ-002`, `TBD-001` |
| P1-8-4 | `Assets/Tests/EditMode/Editor/SrpM1CoreTests.cs`, `Assets/Tests/PlayMode/SrpM1PlayModeTests.cs` | 엄폐 가능/클론/완충/비적용/HUD 표기 회귀 테스트 | `RQ-002`, `RQ-004`, `TBD-004` |
| P1-8-5 | `docs/srpg/SRPG_BACKLOG.md`, `docs/srpg/SRPG_TDD.md`, `docs/srpg/SRPG_GDD_TEST_TRACEABILITY.md`, `docs/srpg/SRPG_CHANGELOG.md`, `docs/srpg/SRPG_README.md` | 완료 범위와 테스트 결과 문서화 | `TBD-001` |

비범위:

- 별도 엄폐 타일 스키마와 맵 메이커 엄폐 전용 편집
- 여러 오버워치 후보 우선순위
- 특수 지형 상호작용과 엄폐 방향별 정밀 수치
- 상호작용 AP 행동

검증:

- `ReadLints` 변경 파일 진단 통과
- Unity EditMode 테스트 통과: `54 passed / 0 failed`
- Unity PlayMode 테스트 통과: `5 passed / 0 failed`
- 테스트 산출물 확인 후 삭제

## 0-9. 완료: 상호작용 AP 행동 1차 구현

목표:

- 재장전/엄폐에 이어 마지막 AP 행동 확정 후보인 상호작용을 1차 구현한다.
- 복잡한 시나리오 스크립트가 아니라 맵에 배치된 포인트를 인접 유닛이 AP 1로 활성화하는 최소 규칙을 고정한다.

작업 범위:

| 우선순위 | 파일 | 작업 | 요구사항 ID |
| --- | --- | --- | --- |
| P1-9-1 | `Assets/Scripts/SRPG/SrpMapFile.cs`, `Assets/Scripts/SRPG/SrpBattleState.cs` | 상호작용 포인트 데이터 계약, 런타임 목록, 클론, 인접 탐색 helper 추가 | `RQ-002`, `TBD-001` |
| P1-9-2 | `Assets/Scripts/SRPG/SrpGameController.cs` | AP 1 상호작용 실행, owner 제한, 활성화 상태 변경을 전투 입력에 연결 | `RQ-002` |
| P1-9-3 | `Assets/Scripts/SRPG/SrpGameController.Hud.cs`, `Assets/Scripts/SRPG/SrpGameController.Rendering.cs` | 좌측 콘솔 상호작용 버튼, HUD 상태, 노랑 상호작용 오버레이 추가 | `RQ-002`, `TBD-001` |
| P1-9-4 | `Assets/Scripts/SRPG/SrpDefaultMaps.cs` | `M1QaIntegrated`에 상호작용 포인트 1개 배치 | `RQ-002` |
| P1-9-5 | `Assets/Tests/EditMode/Editor/SrpM1CoreTests.cs`, `Assets/Tests/EditMode/Editor/SrpMakerMetadataTests.cs`, `Assets/Tests/PlayMode/SrpM1PlayModeTests.cs` | 탐색/실행/차단/클론/JSON/프리셋/HUD 회귀 테스트 추가 | `RQ-002`, `TBD-001` |

비범위:

- 복잡한 이벤트 스크립트/문 열림/승리 조건 연동
- 맵 메이커 상호작용 포인트 전용 편집 UI
- 특수 지형과 상호작용 결과의 연쇄 효과

검증:

- `ReadLints` 변경 파일 진단 통과
- Unity EditMode 테스트 통과: `59 passed / 0 failed`
- Unity PlayMode 테스트 통과: `5 passed / 0 failed`
- 테스트 산출물 확인 후 삭제

## 0-10. 완료: 개발용 전술 HUD 개선

목표:

- 텍스처/프리팹 정식 UI 전환 전에도 테스트 편의성을 높일 수 있는 개발용 전술 HUD를 추가한다.
- 전투 규칙에는 영향을 주지 않고, 선택 유닛과 hover 대상의 정보를 읽기 전용 preview로 표시한다.

작업 범위:

| 우선순위 | 파일 | 작업 | 요구사항 ID |
| --- | --- | --- | --- |
| P1-10-1 | `Assets/Scripts/SRPG/SrpGameController.Hud.cs` | 좌측 하단 현재 유닛 카드와 우측 하단 행동 preview 카드 추가 | `RQ-002`, `TBD-001` |
| P1-10-2 | `Assets/Scripts/SRPG/SrpGameController.Hud.cs` | HP/PG/AP/탄약 숫자+단색 게이지 helper 추가 | `RQ-002`, `TBD-001` |
| P1-10-3 | `Assets/Scripts/SRPG/SrpGameController.Hud.cs`, `Assets/Scripts/SRPG/SrpGameController.cs` | 이동/공격/스킬/상호작용 hover preview 데이터 구성 | `RQ-002`, `TBD-001` |
| P1-10-4 | `Assets/Tests/PlayMode/SrpM1PlayModeTests.cs` | 하단 카드 생성, 게이지 텍스트, 이동/유닛/상호작용 preview 회귀 테스트 추가 | `RQ-002`, `TBD-001` |

비범위:

- 텍스처/아이콘/프리팹 기반 정식 UI
- 애니메이션/사운드/팝업 연출
- 최종 밸런스 수치와 확률 기대값의 완전한 예측

검증:

- `ReadLints` 변경 파일 진단 통과
- Unity EditMode 테스트 통과: `59 passed / 0 failed`
- Unity PlayMode 테스트 통과: `5 passed / 0 failed`
- 테스트 산출물 확인 후 삭제

## 0-11. 완료: 총기 1발 고화력 + 방향성 엄폐 설계

목표:

- 전장식 총기 컨셉에 맞춰 기본 탄창을 1발로 낮추고, 기본 공격을 HP 고화력 압박으로 재조정한다.
- 선형/방향성 엄폐는 이번 단계에서 코드 구현하지 않고, 후속 데이터 계약과 판정 단계를 문서로 고정한다.

작업 범위:

| 우선순위 | 파일 | 작업 | 요구사항 ID |
| --- | --- | --- | --- |
| P1-11-1 | `Assets/Scripts/SRPG/SrpUnitRuntime.cs` | 명시 `maxAmmo`가 없는 총기 유닛의 기본 탄창을 1발로 변경 | `RQ-002` |
| P1-11-2 | `Assets/Scripts/SRPG/SrpCombatResolver.cs` | 총기 기본 공격을 HP 고화력/낮은 PG 압박 브릿지로 조정 | `RQ-002`, `RQ-004` |
| P1-11-3 | `Assets/Tests/EditMode/Editor/SrpM1CoreTests.cs`, `Assets/Tests/PlayMode/SrpM1PlayModeTests.cs` | 기본 탄창 1발, 고화력 HP 피해, 비총기 탄약 예외, HUD `1/1` 표기 회귀 테스트 | `RQ-002` |
| P1-11-4 | `docs/srpg/SRPG_TDD.md`, `docs/srpg/SRPG_PHASE2_CODE_BACKLOG.md`, `docs/srpg/SRPG_BACKLOG.md`, `docs/srpg/SRPG_README.md`, `docs/srpg/SRPG_CHANGELOG.md` | 방향성 엄폐 데이터 계약과 후속 구현 단계 문서화 | `TBD-001`, `TBD-004` |

방향성 엄폐 후속 단계:

1. edge 엄폐 데이터/렌더링
2. 공격자-방어자 방향 기준 엄폐 적용
3. 오버워치/사선 차단과 연동
4. 맵 메이커 편집 UI

비범위:

- 선형/방향성 엄폐의 런타임 판정 구현
- ㄱ자/ㄷ자 엄폐 렌더링
- 총기별 탄종, 장전 시간, 개별 사거리/피해 테이블

검증:

- `ReadLints` 변경 파일 진단 통과
- Unity EditMode 테스트 통과: `61 passed / 0 failed`
- Unity PlayMode 테스트 통과: `5 passed / 0 failed`
- 테스트 산출물 확인 후 삭제

## 0-12. 완료: 방향성 엄폐 1차 구현

목표:

- 선형/방향성 엄폐를 데이터, 런타임, 렌더링, 총기 피해 판정에 연결한다.
- 오버워치 사선 차단과 맵 메이커 편집 UI는 후속 단계로 분리한다.

작업 범위:

| 우선순위 | 파일 | 작업 | 요구사항 ID |
| --- | --- | --- | --- |
| P1-12-1 | `Assets/Scripts/SRPG/SrpMapFile.cs`, `Assets/Scripts/SRPG/SrpBattleState.cs` | `SrpCoverSegmentData` 계약, 맵 스키마, 런타임 로딩/클론 추가 | `TBD-001`, `TBD-004` |
| P1-12-2 | `Assets/Scripts/SRPG/SrpBattleState.cs`, `Assets/Scripts/SRPG/SrpCombatResolver.cs` | 공격자-방어자 방향이 segment edge를 통과할 때만 총기 엄폐 완충 적용 | `RQ-004`, `TBD-004` |
| P1-12-3 | `Assets/Scripts/SRPG/SrpGameController.Rendering.cs`, `Assets/Scripts/SRPG/SrpGameController.Hud.cs`, `Assets/Scripts/SRPG/SrpDefaultMaps.cs` | 방향성 엄폐 overlay, HUD 범례, `M1QaIntegrated` QA segment 추가 | `TBD-001` |
| P1-12-4 | `Assets/Tests/EditMode/Editor/SrpM1CoreTests.cs`, `Assets/Tests/EditMode/Editor/SrpMakerMetadataTests.cs`, `Assets/Tests/PlayMode/SrpM1PlayModeTests.cs` | 로딩/클론/방향 판정/JSON/프리셋/HUD 회귀 테스트 추가 | `RQ-004`, `TBD-004` |
| P1-12-5 | `docs/srpg/SRPG_BACKLOG.md`, `docs/srpg/SRPG_TDD.md`, `docs/srpg/SRPG_GDD_TEST_TRACEABILITY.md`, `docs/srpg/SRPG_CHANGELOG.md`, `docs/srpg/SRPG_README.md` | 완료 범위와 후속 범위 문서화 | `TBD-001` |

비범위:

- `blocksLineOfSight` 기반 오버워치/원거리 사선 차단
- ㄱ자/ㄷ자 엄폐 전용 edge mesh 렌더링
- 맵 메이커 방향성 엄폐 전용 편집 UI

검증:

- `ReadLints` 변경 파일 진단 통과
- Unity EditMode 테스트 통과: `64 passed / 0 failed`
- Unity PlayMode 테스트 통과: `5 passed / 0 failed`
- 테스트 산출물 확인 후 삭제

## 1. 전투 코어

| 우선순위 | 파일 | 작업 | 요구사항 ID |
| --- | --- | --- | --- |
| 1 | `Assets/Scripts/SRPG/SrpGameController.cs`, `Assets/Scripts/SRPG/SrpCombatResolver.cs` | 반응 이벤트 파이프라인 훅, 태세별 반응 우선순위 연결(완료) | `RQ-003`, `RQ-005`, `RQ-006`, `RQ-007` |
| 2 | `Assets/Scripts/SRPG/SrpBattleState.cs`, `Assets/Scripts/SRPG/SrpUnitRuntime.cs` | 교전/반응/수비 완충 상태 저장 구조 확장, 클론 안전성 점검(완료) | `RQ-003`, `RQ-010` |
| 3 | `Assets/Scripts/SRPG/SrpCombatResolver.cs` | 기존 DEF/GRD 감쇠 브릿지를 GRD(PG 감쇠) + 경미/중대 HP 피해 분류로 재정렬 | `RQ-004`, `RQ-013`, `TBD-002` |
| 3-1 | `Assets/Scripts/SRPG/SrpCombatResolver.cs` | 총격으로 실제 받은 HP 피해량의 50%를 PG 피해로 추가 적용하고 비율/반올림을 상수화(완료) | `RQ-022`, `TBD-009` |
| 4 | `Assets/Scripts/SRPG/SrpTurnOrder.cs` | 라운드 리셋 시 RP 정책 일관성 검증 | `RQ-001`, `RQ-003` |
| 5 | `Assets/Scripts/SRPG/SrpPathfinder.cs` | 교전 이탈/포지셔닝 패널티 비용 기반 브릿지 추가(완료) | `RQ-010` |
| 6 | `Assets/Scripts/SRPG/SrpGameController.cs` | 교전 이탈 기회공격/반응 이벤트 파이프라인 연결(완료) | `RQ-003`, `RQ-010` |

## 2. 스킬/데이터

| 우선순위 | 파일 | 작업 | 요구사항 ID |
| --- | --- | --- | --- |
| 7 | `Assets/Scripts/SRPG/SrpSkills.cs` | 쿨다운/충전 기본 모델 반영, 오버클럭 진입점 정의(완료) | `RQ-011`, `RQ-012` |
| 8 | `Assets/Scripts/SRPG/SrpSkillData.cs` | 스킬 데이터 스키마에 쿨다운/충전/오버클럭 메타 추가(완료) | `RQ-011`, `RQ-012` |
| 9 | `Assets/Scripts/SRPG/SrpUnitTags.cs`, `Assets/Scripts/SRPG/SrpSkillData.cs`, `Assets/Scripts/SRPG/SrpCombatResolver.cs` | 패링 가능자/공격 메타와 정면 근접 판정 헬퍼 추가(완료) | `RQ-008`, `RQ-009`, `TBD-005` |
| 9-1 | `Assets/Scripts/SRPG/SrpUnitRuntime.cs`, `Assets/Scripts/SRPG/SrpSkills.cs`, `Assets/Scripts/SRPG/SrpCombatResolver.cs` | 공용 전투 태그(`표식`, `균형 붕괴`, `사살 지시`) 저장/갱신/소모 계약 추가(완료) | `RQ-019`, `TBD-007` |
| 9-2 | `Assets/Scripts/SRPG/SrpCombatResolver.cs` | 패링 성공 시 대상 PG 대량 피해와 `균형 붕괴` 태그 부여(완료) | `RQ-018`, `RQ-019`, `TBD-005`, `TBD-007` |
| 10 | `Assets/Scripts/SRPG/SrpMapFile.cs` | 전투 규칙 버전 필드 및 호환 정책 점검 | `TBD-002`, `TBD-006` |
| 11 | `Assets/Scripts/SRPG/SrpDataIO.cs` | 신규 스키마 기본값/하위 호환 처리 | `RQ-011`, `TBD-002` |

## 3. HUD/렌더링

| 우선순위 | 파일 | 작업 | 요구사항 ID |
| --- | --- | --- | --- |
| 12 | `Assets/Scripts/SRPG/SrpGameController.Hud.cs` | 패링 텔레그래프 범례/툴팁, 스킬 충전 표기, RP/HUD 노출 정책 정리(완료) | `RQ-009`, `RQ-011`, `TBD-001` |
| 13 | `Assets/Scripts/SRPG/SrpGameController.Rendering.cs` | 패링 가능 시각 오버레이 확장(완료) | `RQ-009`, `RQ-010` |
| 14 | `Assets/Scripts/SRPG/SrpOverwatch.cs`, `Assets/Scripts/SRPG/SrpGameController.cs` | 명시형 ReactionShot 예약/발동 브릿지, 예약 상태 helper, 사선 차단 segment, 후보 우선순위 연결(완료) | `RQ-003`, `TBD-004` |

## 4. 로비/메이커

| 우선순위 | 파일 | 작업 | 요구사항 ID |
| --- | --- | --- | --- |
| 15 | `Assets/Scripts/SRPG/SrpLobbyController.cs` | 규칙 버전/프리셋 표기 및 QA 진입 옵션 정리 | `RQ-001`, `RQ-011` |
| 16 | `Assets/Scripts/SRPG/SrpSkillMakerController.cs` | 쿨다운/충전/오버클럭/패링 필드 편집 지원(완료) | `RQ-009`, `RQ-011`, `RQ-012` |
| 17 | `Assets/Scripts/SRPG/SrpUnitMakerController.cs` | v2 스탯/전투 enum/패링 전용자/탱커 플래그 편집 지원(완료) | `RQ-008`, `RQ-010`, `TBD-006` |
| 17-1 | `Assets/Scripts/SRPG/SrpUnitRuntime.cs`, `Assets/Scripts/SRPG/SrpCombatResolver.cs`, `Assets/Scripts/SRPG/SrpDefaultUnits.cs` | `완벽한 수비` 1차 구현과 Tank 태그 브릿지 정렬(완료) | `RQ-014`, `TBD-006` |
| 17-2 | `Assets/Scripts/SRPG/SrpDefaultUnits.cs`, `Assets/Scripts/SRPG/SrpDefaultSkills.cs`, `Assets/Scripts/SRPG/SrpDefaultMaps.cs` | 초기 4인 역할/대표 스킬 검증 데이터와 고유 패시브 브릿지 갱신(완료) | `RQ-015`, `RQ-017`, `RQ-021`, `TBD-008` |
| 18 | `Assets/Scripts/SRPG/SrpMapPreset.cs`, `Assets/Scripts/SRPG/SrpDefaultMaps.cs`, `Assets/Scripts/SRPG/SrpLobbyController.cs` | 교전/둘러싸임 검증용 내장 프리셋 보강(완료) | `RQ-010` |

## 5. 보류 전제

- `TBD-*`가 남은 항목은 구현 전 수치/정책 잠금이 필요하다.
- 2차 코드 작업 시 `SrpGameController` partial 파일(`.cs`, `.Hud.cs`, `.Rendering.cs`)을 분리 커밋하지 않고 한 묶음으로 검증한다.
- `SrpBattleState`는 Unity 타입 의존 없이 유지한다.
- 다음 코드 착수 전에는 `SRPG_BACKLOG.md`의 P1 순서와 이 파일의 우선순위를 함께 확인한다.
