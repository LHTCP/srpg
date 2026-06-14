# SRPG 밸런스 피드백 구현 프롬프트 (2026-06-15)

이 문서는 기획자 밸런스 피드백을 다음 구현 세션에 전달하기 위한 프롬프트다. 현재 코드에는 PG 0/그로기 처단 브릿지가 일부 있으므로, 신규 시스템을 덧붙이기보다 기존 계약을 실제 플레이 의도에 맞게 재정렬하는 것이 목표다.

## 전달 프롬프트

```text
목표:
Unity SRPG 프로토타입에서 PG 붕괴, 그로기, 처단, HP 상태에 따른 PG 취약도, 총기/근접 일반 공격 밸런스를 한 번의 검증 가능한 밸런스 브릿지로 구현한다.

작업 전 확인:
- 실제 체크아웃에서 `git status --short --branch`를 먼저 확인한다.
- 주요 파일 후보를 먼저 읽는다:
  - `Assets/Scripts/SRPG/SrpCombatResolver.cs`
  - `Assets/Scripts/SRPG/SrpBattleState.cs`
  - `Assets/Scripts/SRPG/SrpTurnOrder.cs`
  - `Assets/Scripts/SRPG/SrpGameController.cs`
  - `Assets/Tests/EditMode/Editor/SrpM1RuleSpecTests.cs`
  - `docs/srpg/SRPG_전투규칙_기준서_v2.md`
  - `docs/srpg/SRPG_BACKLOG.md`
  - `docs/srpg/SRPG_IMPLEMENTATION_DECISIONS.md`
- 현재 `Execution_Triggers_WhenDefenderPgZeroOrGroggy`와 `SrpCombatResolver.ApplyAttack`에 이미 처단/그로기 브릿지가 있으므로 중복 구현하지 말고 기존 로직을 교정한다.

구현 요구:
1. PG 붕괴 -> 그로기 -> 근접 처단 확정 킬
   - PG가 0 이하가 되면 대상은 `groggy` 상태가 된다.
   - 그로기 상태는 "1턴 동안 아무 것도 못하는 무방비 상태"로 취급한다.
   - 그로기 대상이 근접 상태에서 공격받으면 `처단`으로 판정하고 확정 킬이 나야 한다.
   - "근접 상태"의 1차 계약은 인접 타일 또는 근접 무기 기본 공격으로 제한한다. 총기 원거리 공격이 그로기 대상을 자동 처단하는 구조로 확장하지 않는다.
   - 처단은 대상 HP를 0으로 만들고 제거/전투불능 렌더링과 로그/HUD preview에 즉시 반영한다.
   - 기존처럼 단순 추가 HP 피해만 주는 처단이면 부족하다. 확정 킬 계약으로 고정한다.

2. 그로기 회복 브릿지
   - 기획자와 아직 확정된 규칙은 아니지만, 프로토타입 브릿지로 "그로기가 걸린 유닛의 다음 활성화가 오면 행동하지 못하고 회복"을 제안한다.
   - 회복 시 `groggy=false`, PG를 `ceil(maxPg * 0.35)` 또는 최소 1 이상으로 회복한다.
   - 이 활성화는 소모되어야 한다. 즉, 그로기 유닛이 바로 정상 행동하면 안 된다.
   - 이 수치와 타이밍은 `docs/srpg/SRPG_IMPLEMENTATION_DECISIONS.md`에 "기획 확인 필요"로 남긴다.

3. HP가 낮을수록 PG 피해 증가
   - 낮은 HP는 자세 유지력도 낮아지는 방향으로, incoming PG 피해에 보정 계수를 적용한다.
   - 권장 1차 수치:
     - HP 비율 50% 이하: PG 피해 +25%
     - HP 비율 25% 이하: PG 피해 +50%
   - 보정은 실제 PG 피해가 1 이상일 때만 적용하고, 회복/버프에는 적용하지 않는다.
   - 수비/엄폐/태그 보정 순서와 충돌하지 않게 적용 위치를 테스트로 고정한다.

4. 총기 일반 공격 피해 재조정
   - "총 맞으면 한 체력의 약 2/3, 권총은 더 약하게"라는 밸런스 방향을 반영한다.
   - 현재 데이터에 권총/소총 세분화가 없다면 `SrpWeaponClass.Firearm` 안에서 사거리/공격력/태그 등 기존 데이터로 세분화 가능 여부를 먼저 확인한다.
   - 권총 구분이 없으면 이번 PR에서 무기 세부 타입을 크게 확장하지 말고, 문서에 권총 세분화 필요를 남긴다.
   - 일반 총기 브릿지는 보통 유닛 기준 HP의 약 60~70% 위협으로 튜닝하고, 권총 후보는 40~50% 위협으로 낮춘다.
   - 기존 총격의 PG 파급 정책과 엄폐 완충을 깨지 않도록 한다.

5. 일반 근접 PG 페이싱
   - 보편적인 유닛 간 싸움에서 일반 근접만 반복하면 대략 2~3턴마다 PG가 모두 깎이는 수준을 목표로 한다.
   - 기본 유닛 max PG, 공격 태세 보정, 수비 태세 보정, 방향성 보정을 포함해 EditMode 테스트로 기대 범위를 잡는다.
   - 정확한 수치는 임시 브릿지로 문서화하고, 실제 플레이/AI 시뮬레이션으로 후속 조정 가능하게 둔다.

UI/피드백 요구:
- 처단 가능 대상은 HUD preview 또는 로그에서 "처단 가능"이 읽히게 한다.
- 처단 발생, 그로기 발생, 그로기 회복은 로그에 이벤트 단위로 남긴다.
- 기존 floating/world feedback 시스템이 있다면 재사용하되, 없으면 로그/preview만으로 1차 완료해도 된다.

테스트 요구:
- EditMode:
  - PG 붕괴 시 groggy가 켜진다.
  - 그로기 대상에게 인접/근접 처단이 들어가면 HP가 0이 되고 제거 상태가 된다.
  - 원거리 총격이 그로기 대상을 무조건 처단하지 않는 정책을 고정한다.
  - 그로기 유닛의 다음 활성화가 행동을 스킵하고 PG 일부 회복 후 groggy를 해제한다.
  - HP 50%/25% 이하에서 PG 피해 보정이 적용된다.
  - 일반 근접 반복 2~3회 안에 표준 target PG를 붕괴시키는 기대 범위를 검증한다.
- PlayMode:
  - 기본 전투 또는 QA 프리셋에서 그로기/처단/회복 로그와 HUD preview가 깨지지 않는다.
- 검증 순서:
  - `git diff --check`
  - `C:\Program Files\Git\bin\bash.exe scripts/validate-repo.sh`
  - Unity EditMode/PlayMode 배치 테스트. 이 환경에서는 `-quit`이 테스트 XML 생성을 방해할 수 있으므로 XML 파일 존재와 `failed=0`을 반드시 확인한다.

문서 업데이트:
- `docs/srpg/SRPG_BACKLOG.md`
- `docs/srpg/SRPG_TDD.md`
- `docs/srpg/SRPG_GDD_TEST_TRACEABILITY.md`
- `docs/srpg/SRPG_CHANGELOG.md`
- `docs/srpg/SRPG_IMPLEMENTATION_DECISIONS.md`

완료 보고:
- 구현한 확정 계약과 임시 브릿지 수치를 분리해서 한국어로 보고한다.
- 기획자 의사결정이 필요한 항목은 별도 목록으로 남긴다.
```

## 기획 확인 필요

- 그로기 회복 타이밍: 다음 활성화 시작 즉시 스킵+회복인지, 라운드 종료 회복인지.
- 그로기 회복 PG 비율: `maxPg * 0.35`가 적절한지.
- 권총/소총 등 총기 세부 타입을 데이터 구조에 추가할지, 기존 `Firearm` 안에서 사거리/공격력으로만 구분할지.
- 낮은 HP에 따른 PG 취약 보정이 모든 피해에 적용되는지, 근접/스킬에만 적용되는지.
- 일반 근접 2~3턴 붕괴 기준을 1:1 표준 유닛 기준으로 볼지, 태세/방향/엄폐 없는 실험실 기준으로 볼지.
