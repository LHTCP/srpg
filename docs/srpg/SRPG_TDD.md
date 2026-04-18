# SRPG 기술 설계 문서 (TDD) v1.0

## 1. 설계 목표

- v1 GDD의 7개 검증 항목을 기술적으로 구현 가능한 형태로 분해한다.
- 기존 핫시트 플레이어 턴 구조를 속도 기반 라운드 구조로 전환한다.
- AP/RP, 태세, 방향, LOS, 반응행동을 독립 모듈로 분리한다.

## 2. 모듈 아키텍처

```text
SrpGameController
  ├─ SrpBattleState
  ├─ SrpTurnOrder           (new)
  ├─ SrpCombatResolver
  ├─ SrpReaction            (new)
  ├─ SrpOverwatch           (new)
  ├─ SrpLineOfSight         (new)
  ├─ SrpSkills
  └─ SrpPathfinder
```

## 3. 핵심 데이터 모델

### 3.1 SrpUnitRuntime (v1)

필수 필드

- `hp`, `maxHp`
- `pg`, `maxPg`
- `actionPoints`, `maxActionPoints` (기본 2)
- `reactionPoints`, `maxReactionPoints` (기본 1)
- `speed`
- `stance` (`Aggressive`, `Defensive`)
- `facing` (`North`, `East`, `South`, `West`)
- `weaponClass` (`Firearm`, `Melee`, `Magic`)

### 3.2 SrpBattleState

- 유닛 목록, 점유, 생존/제거 상태
- 교전 쌍(engagement pair) 또는 교전 집합
- 현재 라운드 번호, 현재 행동 유닛 id
- 반응 대기 이벤트 큐

### 3.3 SrpTurnOrder (new)

- 라운드 시작 시 정렬:
  - 살아있는 유닛을 `speed` 내림차순 정렬
  - 동속 처리 규칙: owner -> id 순
- API
  - `BuildRoundQueue(state)`
  - `AdvanceToNextUnit(state)`
  - `HasRemainingUnitInRound(state)`

## 4. 전투 해석 규칙

### 4.1 무기 분기

- `Firearm`: HP 중심 피해, 조건부 보너스
- `Melee`: PG 중심 압박, 교전 고정 보너스
- `Magic`: 직접 피해는 보조, 위치/상태 개입 중심

### 4.2 처단

- PG 임계 상태(붕괴)에 도달한 대상은 처단 위험 상태가 된다.
- 처단 판정 성공 시 큰 HP 피해를 적용한다.

### 4.3 방향 보정

- 피격자 기준 정면/측면/후방 판정으로 반응/피해 보정
- 후방은 대응 제한이 가장 크다.

## 5. 반응행동

### 5.1 기본 반응

- 방어(피해 경감)
- 회피(명중 판정 회피)
- 패링(조건부, 정면 근접 중심)
- 반응사격(경계태세 기반)

### 5.2 소비 규칙

- 반응행동은 RP를 소비한다.
- RP가 0이면 반응행동을 사용할 수 없다.

## 6. 경계태세

- AP 1 소비로 활성화
- 적 유닛이 조건(사선 진입 등)을 만족하면 반응사격 트리거
- 같은 적 턴 내 발동 제한은 밸런스 결정 사항으로 둔다

## 7. LOS

- 브레젠햄 라인 트레이스 기반 1차 구현
- 사선 차단 타일(엄폐/장애물) 데이터 참조
- 총기, 반응사격, 일부 마법 타게팅에 공통 사용

## 8. 맵/유닛 스키마 v2

`SrpMapFile.version = 2` 기준

- 유닛 템플릿 필드 추가:
  - `speed`
  - `maxActionPoints`
  - `maxReactionPoints`
  - `defaultStance`
  - `defaultFacing`
  - `weaponClass`
- 기존 `allowedSkillIds`, `disabledSkillIds`는 유지하되 v1 효과 타입과 동기화

## 9. 테스트 전략

우선 EditMode 단위 테스트를 추가한다.

- `SrpTurnOrderTests`
  - 속도 정렬, 동속 규칙, 라운드 종료
- `SrpCombatResolverTests`
  - 총기/근접/마법 분기, PG 붕괴/처단
- `SrpReactionTests`
  - RP 소비, 반응 우선순위
- `SrpLineOfSightTests`
  - 사선 차단/통과 케이스

## 10. 구현 순서

1. 상태/데이터 리네이밍 및 필드 전환
2. 턴 오더 모듈 도입
3. 전투 공식 교체
4. 반응/경계태세/LOS 도입
5. 스킬/메이커 동기화
6. 테스트와 튜닝
