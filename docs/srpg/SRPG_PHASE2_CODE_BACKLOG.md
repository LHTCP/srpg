# SRPG 코드 2차 착수 백로그 (파일 단위)

기준 문서:

- `SRPG_전투규칙_기준서_v2.md`
- `new/SRPG_NEW_DIALOG_POLICY_LOCK.md`

목적:

- 코드 착수 전 파일 단위 작업 목록을 요구사항 ID(`RQ-*`, `TBD-*`)에 연결한다.

## 0. 현재 착수 상태

2026-04-26 기준 Phase2 1차 전투 코어 기반 작업, 교전 이탈 비용 브릿지, 교전 이탈 기회공격 1차 구현, 스킬 자원 기본 모델, 패링 조건/텔레그래프 1차 구현, 반응행동 파이프라인 1차 구현, 수비 지속 완충/탱커 다중 대응 브릿지, 메이커 메타데이터 UI 확장, 중간 점검 보정, 유닛 시각 방향성 개선, 교전/둘러싸임 검증 프리셋 보강, RP/HUD 노출 정책 정리, 기획 대조 P1 보정, HUD/로그 가독성 동기화, 오버워치 사선/횟수/해제 상세 규칙을 완료했다.

완료 범위:

- `SrpBattleState.cs`: 교전 상태 저장 구조(`Engagements`)와 클론 안전성 추가
- `SrpUnitRuntime.cs`: 반응행동 종류/마지막 반응 상태 기록 필드 추가
- `SrpCombatResolver.cs`: 상태 기반 공격 해결 오버로드, DEF/GRD 감쇠, 수비 태세 Guard 반응 RP 소비 훅, RP 기반 기회공격 해석 추가
- `SrpTurnOrder.cs`: 라운드 AP/RP 리셋 정책 분리
- `SrpGameController.cs`: 상태 기반 전투 해결, 피격 패시브 훅, 교전 재계산, 교전 이탈 로그 힌트, 기회공격 실행 연결
- `SrpPathfinder.cs`: 교전 중 적 인접 상태를 벗어나는 이동에 임시 이탈 비용 추가
- `SrpSkillData.cs`: 충전/오버클럭 스킬 메타와 런타임 충전 상태 추가
- `SrpSkills.cs`: 쿨다운/충전 소비·회복, FH 기반 오버클럭 헬퍼 추가
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
- `SrpGameController.cs`, `SrpSkills.cs`: 공격/기회공격/오버워치/스킬/반응 로그를 이벤트 단위 문구로 정리
- EditMode 테스트: 교전/반응 클론 독립성, Guard RP 소비, 라운드 RP 리셋, 교전 이탈 비용, 기회공격 발생/미발생, 쿨다운/충전/오버클럭, 패링 가능/불가 조건, Parry/Dodge/명시형 ReactionShot, 오버워치 사선/차단, 수비 지속 완충/탱커 다중 대응, 메이커 JSON 메타 보존, 중간 점검 회귀, 유닛 뷰 방향 회전, 프리셋 기반 교전/포위 검증, 오버워치 상태 helper 검증 추가
- PlayMode 테스트: HUD의 반응 상태 표기(`반응: 준비/소모/예약`), 범례, 오버워치 버튼, hover 문구, 로그 핵심 문구를 스모크 검증

주의:

- 원기획의 교전 이탈 규칙은 이동력 추가 소모가 아니라 `기회공격` 리스크 중심이다.
- 현재 `+1` 비용은 기존 ZOC 비용 모델에 맞춘 브릿지 구현이며, 기회공격 1차 구현 후에도 이동 선택 단계의 위험 힌트로 유지한다.
- `ReactionShot` 기록은 교전 이탈 기회공격과 명시형 오버워치 발동 모두에 사용한다.
- 현재 오버클럭 비용은 기존 `frozenHeart` 값을 안정도 대용으로 사용한다. 별도 안정도 수치계/전용 UI는 후속으로 남긴다.
- 현재 Parry/Dodge는 피해 무효화 브릿지로 구현했다. 최종 회피 확률식, 패링 보상/실패 패널티 수치는 `TBD-003`, `TBD-005` 후속으로 남긴다.
- 현재 Dodge는 임시 확률형 브릿지로 성공/실패만 분기한다. 최종 확률식과 스탯/방향 가중치는 `TBD-003` 후속으로 남긴다.
- 현재 방향 방어 불리는 측후면 추가 피해 브릿지로만 반영한다. 최종 DEF/GRD 수치표와 방향별 공식은 `TBD-002` 후속으로 남긴다.
- 현재 명시형 `ReactionShot`은 AP 예약/RP 발동, 8방향 직선 사선, 장애물/유닛 차단, 예약 1회당 1회 발동, 라운드 리셋 해제를 1차 상세 규칙으로 사용한다. 여러 오버워치 후보의 우선순위와 특수 지형 상호작용은 `TBD-004` 후속으로 남긴다.
- 현재 탱커 다중 대응은 `Tank` 태그와 다중 교전 수 기반 감쇠 브릿지다. 탱커 전용 패시브 최종 형태/수치는 `TBD-006` 후속으로 남긴다.

검증:

- Unity EditMode 테스트 통과: `45 passed / 0 failed`
- Unity PlayMode 테스트 통과: `4 passed / 0 failed`
- 실행 명령:
  - `Unity.exe -batchmode -automated -projectPath c:/workdir/srpg -runTests -testPlatform EditMode -testResults c:/workdir/srpg/TestResults-EditMode.xml -logFile c:/workdir/srpg/UnityTest-EditMode.log`

다음 착수 후보:

- P1/P2: 테스트/시뮬레이션 기준 갱신
- P2: 탱커 패시브 최종안/특수 스킬 피해 파이프라인 정리

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

## 1. 전투 코어

| 우선순위 | 파일 | 작업 | 요구사항 ID |
| --- | --- | --- | --- |
| 1 | `Assets/Scripts/SRPG/SrpGameController.cs`, `Assets/Scripts/SRPG/SrpCombatResolver.cs` | 반응 이벤트 파이프라인 훅, 태세별 반응 우선순위 연결(완료) | `RQ-003`, `RQ-005`, `RQ-006`, `RQ-007` |
| 2 | `Assets/Scripts/SRPG/SrpBattleState.cs`, `Assets/Scripts/SRPG/SrpUnitRuntime.cs` | 교전/반응/수비 완충 상태 저장 구조 확장, 클론 안전성 점검(완료) | `RQ-003`, `RQ-010` |
| 3 | `Assets/Scripts/SRPG/SrpCombatResolver.cs` | DEF/GRD 상시 감쇠 + 반응행동 적용 순서 구현 | `RQ-004`, `TBD-002`, `TBD-003` |
| 4 | `Assets/Scripts/SRPG/SrpTurnOrder.cs` | 라운드 리셋 시 RP 정책 일관성 검증 | `RQ-001`, `RQ-003` |
| 5 | `Assets/Scripts/SRPG/SrpPathfinder.cs` | 교전 이탈/포지셔닝 패널티 비용 기반 브릿지 추가(완료) | `RQ-010` |
| 6 | `Assets/Scripts/SRPG/SrpGameController.cs` | 교전 이탈 기회공격/반응 이벤트 파이프라인 연결(완료) | `RQ-003`, `RQ-010` |

## 2. 스킬/데이터

| 우선순위 | 파일 | 작업 | 요구사항 ID |
| --- | --- | --- | --- |
| 7 | `Assets/Scripts/SRPG/SrpSkills.cs` | 쿨다운/충전 기본 모델 반영, 오버클럭 진입점 정의(완료) | `RQ-011`, `RQ-012` |
| 8 | `Assets/Scripts/SRPG/SrpSkillData.cs` | 스킬 데이터 스키마에 쿨다운/충전/오버클럭 메타 추가(완료) | `RQ-011`, `RQ-012` |
| 9 | `Assets/Scripts/SRPG/SrpUnitTags.cs`, `Assets/Scripts/SRPG/SrpSkillData.cs`, `Assets/Scripts/SRPG/SrpCombatResolver.cs` | 패링 가능자/공격 메타와 정면 근접 판정 헬퍼 추가(완료) | `RQ-008`, `RQ-009`, `TBD-005` |
| 10 | `Assets/Scripts/SRPG/SrpMapFile.cs` | 전투 규칙 버전 필드 및 호환 정책 점검 | `TBD-002`, `TBD-006` |
| 11 | `Assets/Scripts/SRPG/SrpDataIO.cs` | 신규 스키마 기본값/하위 호환 처리 | `RQ-011`, `TBD-002` |

## 3. HUD/렌더링

| 우선순위 | 파일 | 작업 | 요구사항 ID |
| --- | --- | --- | --- |
| 12 | `Assets/Scripts/SRPG/SrpGameController.Hud.cs` | 패링 텔레그래프 범례/툴팁, 스킬 충전 표기, RP/HUD 노출 정책 정리(완료) | `RQ-009`, `RQ-011`, `TBD-001` |
| 13 | `Assets/Scripts/SRPG/SrpGameController.Rendering.cs` | 패링 가능 시각 오버레이 확장(완료) | `RQ-009`, `RQ-010` |
| 14 | `Assets/Scripts/SRPG/SrpOverwatch.cs`, `Assets/Scripts/SRPG/SrpGameController.cs` | 명시형 ReactionShot 예약/발동 브릿지와 예약 상태 helper 연결(완료) | `RQ-003`, `TBD-004` |

## 4. 로비/메이커

| 우선순위 | 파일 | 작업 | 요구사항 ID |
| --- | --- | --- | --- |
| 15 | `Assets/Scripts/SRPG/SrpLobbyController.cs` | 규칙 버전/프리셋 표기 및 QA 진입 옵션 정리 | `RQ-001`, `RQ-011` |
| 16 | `Assets/Scripts/SRPG/SrpSkillMakerController.cs` | 쿨다운/충전/오버클럭/패링 필드 편집 지원(완료) | `RQ-009`, `RQ-011`, `RQ-012` |
| 17 | `Assets/Scripts/SRPG/SrpUnitMakerController.cs` | v2 스탯/전투 enum/패링 전용자/탱커 플래그 편집 지원(완료) | `RQ-008`, `RQ-010`, `TBD-006` |
| 18 | `Assets/Scripts/SRPG/SrpMapPreset.cs`, `Assets/Scripts/SRPG/SrpDefaultMaps.cs`, `Assets/Scripts/SRPG/SrpLobbyController.cs` | 교전/둘러싸임 검증용 내장 프리셋 보강(완료) | `RQ-010` |

## 5. 보류 전제

- `TBD-*`가 남은 항목은 구현 전 수치/정책 잠금이 필요하다.
- 2차 코드 작업 시 `SrpGameController` partial 파일(`.cs`, `.Hud.cs`, `.Rendering.cs`)을 분리 커밋하지 않고 한 묶음으로 검증한다.
- `SrpBattleState`는 Unity 타입 의존 없이 유지한다.
- 다음 코드 착수 전에는 `SRPG_BACKLOG.md`의 P1 순서와 이 파일의 우선순위를 함께 확인한다.
