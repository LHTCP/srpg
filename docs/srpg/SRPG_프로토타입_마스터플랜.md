# SRPG v1 프로토타입 마스터플랜

기준 문서: `260418_신규_프로젝트 전투 설계 요약본 v0.pdf`

## 1. 목적

본 문서는 SRPG v1 프로토타입 완성을 위한 단일 실행 기준이다.
핵심 목표는 아래 7개 검증 항목을 실제 플레이 가능한 상태로 확인하는 것이다.

1. 속도 기반 캐릭터 턴이 진영 턴보다 재미있는가
2. AP 2 + RP 1 구조가 직관적인가
3. 공격/수비 2태세가 충분히 기능하는가
4. 총격(HP 압박)과 근접(PG 압박)이 역할 분담을 이루는가
5. 마법이 전장을 비틀 수 있는가
6. 탱커가 핵심 적을 교전으로 고정할 수 있는가
7. 근접 투사가 PG 연쇄 붕괴를 만들 수 있는가

## 2. 전환 원칙

- v0 체계(방어구 AP 중심 전투, FH 중심 스킬 해석)는 v1 기획과 충돌하므로 보존보다 재정의를 우선한다.
- 용어 충돌을 피한다.
  - v0 `ap`(방어구) -> 폐기
  - v1 `AP`(행동 포인트) + `RP`(반응 포인트) 도입
- 프로토타입은 완벽한 콘텐츠보다 전술 루프 검증을 우선한다.

## 3. 레거시 코드 분류 기준

상세 표는 `SRPG_레거시_코드_분류.md`를 따른다.

- **Discard**: 기획 충돌 요소, 체스 잔재, FH 특화 규칙
- **Rework**: 전투 상태/턴/전투 해석/스킬 데이터/HUD 코어
- **Keep**: IO, 폰트 워밍업, 씬 전환, 타일 입력 뼈대
- **New**: 턴 큐, 태세/방향, LOS, 반응행동, 경계태세

## 4. 파일 단위 작업 맵

| 구분 | 파일 | 작업 |
|------|------|------|
| Rework | `Assets/Scripts/SRPG/SrpBattleState.cs` | 속도 라운드 턴 상태, 교전 고정 상태, 반응 가능 이벤트 저장 |
| Rework | `Assets/Scripts/SRPG/SrpUnitRuntime.cs` | AP/RP, stance, facing, speed, weaponClass 필드 재구성 |
| Rework | `Assets/Scripts/SRPG/SrpCombatResolver.cs` | 총기/근접/마법 분기 + HP/PG 처리 |
| Rework | `Assets/Scripts/SRPG/SrpSkills.cs` | 전장 개입형 마법 효과(밀치기/끌기/재배치/차단) 중심으로 재작성 |
| Rework | `Assets/Scripts/SRPG/SrpGameController.cs` | 라운드 턴 진행, 반응 입력, 행동 종료 규칙 |
| Rework | `Assets/Scripts/SRPG/SrpGameController.Hud.cs` | AP/RP, 태세 버튼, 경계태세, 반응 UI |
| Rework | `Assets/Scripts/SRPG/SrpMapFile.cs` | v2 스키마(속도/무기/태세/방향 기본값) |
| New | `Assets/Scripts/SRPG/SrpTurnOrder.cs` | 속도 기반 라운드 순서 생성 |
| New | `Assets/Scripts/SRPG/SrpStance.cs` | 공격/수비 태세 enum 및 규칙 |
| New | `Assets/Scripts/SRPG/SrpFacing.cs` | 정면/측면/후방 판정 보조 |
| New | `Assets/Scripts/SRPG/SrpLineOfSight.cs` | 사선 판정(총기/경계태세 공용) |
| New | `Assets/Scripts/SRPG/SrpReaction.cs` | RP 소비 반응(회피/방어/패링/반응사격) |
| New | `Assets/Scripts/SRPG/SrpOverwatch.cs` | 경계태세 상태 및 트리거 |
| Keep | `Assets/Scripts/SRPG/SrpMapIO.cs` | 저장/불러오기 경로 유지, 스키마 버전 처리만 확장 |
| Keep | `Assets/Scripts/SRPG/SrpDataIO.cs` | 스킬/유닛 DB IO 유지 |
| Keep | `Assets/Scripts/SRPG/SrpGameSettings.cs` | 로비-전투 씬 전환 유지 |

## 5. Milestone 실행 계획

현재 상태(2026-04-18): M0 완료, M1 1차 완료, M2 이상 진행 예정

### M0. 정리와 기준 수립

- 브랜치 생성 및 체스 잔재 제거
- 문서 세트 v1 기준으로 재작성
- 레거시 분류 문서 확정

**완료 기준**
- 체스 코드/문서/링크가 저장소에서 제거됨
- 본 문서, GDD, TDD, Backlog, README가 상호 일치함

### M1. 전투 코어 전환

- 속도 기반 라운드 턴 큐 도입
- AP 2 + RP 1 기본 자원 도입
- HP/PG 이원화 전투 공식 도입
- 총기/근접 역할 분리 반영

**검증 시나리오**
- 4유닛 교전에서 속도 순서대로 행동이 순환되는지
- 동일 전투에서 총기와 근접의 기대 결과가 다르게 나오는지

### M2. 태세/방향/ZOC 확장

- 공격/수비 태세 실시간 전환
- 정면/측면/후방 보정 도입
- 교전 상태 고정 + 강제 이탈 시 기회공격 도입

**검증 시나리오**
- 탱커가 핵심 적 1개를 2턴 이상 교전 상태로 유지
- 측후방 진입이 전투 결과에 유의미한 차이를 만드는지

### M3. 반응행동/경계태세/LOS

- RP 소비 반응행동: 방어, 회피, 패링(조건부), 반응사격
- AP 1 소비 경계태세 추가
- LOS 차단 규칙 반영

**검증 시나리오**
- 적 진입 시 반응행동이 우선 처리되고 RP가 소모되는지
- LOS 차단 지형 뒤의 목표가 총기로 제한되는지

### M4. 마법 전장 개입 + 역할군 검증 완성

- 재배치/밀치기/끌기/사선차단/구조형 마법 구현
- 근접 투사의 PG 연쇄 붕괴 루프 검증
- 특수 적 1종(장갑/약점) 시범 적용

**검증 시나리오**
- 마법 없이 불가능한 포지션 전환이 실제로 발생하는지
- 근접 투사가 탱커 고정 대상에 처단 연계를 만들 수 있는지

## 6. 데이터 스펙 초안 (프로토타입)

표준 인간형 기준값

- HP: 30
- PG: 18
- AP: 2
- RP: 1

역할군 가이드

- 사격수: HP 압박, 엄폐 강제, 경계태세 효율 우수
- 마도사: 위치 재배치, 사선 개입, 구조/분리
- 탱커: 교전 고정, 정면 유지, 수비태세 효율 우수
- 근접 투사: PG 붕괴, 처단, 연쇄 정리

## 7. 리스크와 대응

- **리스크**: 기존 코드가 플레이어 턴 중심 구조에 강하게 결합
  - **대응**: M1에서 턴 큐 전용 모듈을 먼저 분리
- **리스크**: 스킬 데이터 스키마 변경으로 메이커 호환성 저하
  - **대응**: `version` 필드 기반 마이그레이션 함수 제공
- **리스크**: 반응행동 UI 복잡도 급증
  - **대응**: M3에서 3개 반응행동만 우선 적용 후 확장

## 8. 문서 갱신 규칙

- 구현 결정이 바뀌면 같은 PR에서 본 문서와 `SRPG_GDD.md`, `SRPG_TDD.md`를 같이 갱신한다.
- 다음 미팅 안건은 `SRPG_다음미팅_논의사항.md`를 기준으로 유지한다.
