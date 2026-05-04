# SRPG v2 문서 우선 마스터플랜

상위 기준:
- `docs/srpg/SRPG_전투규칙_기준서_v2.md`
- `docs/srpg/new/SRPG_NEW_DIALOG_POLICY_LOCK.md`

## 1. 목적

본 문서는 신규 대화 원본(06~23) 기반 개편을 문서 우선으로 완수하기 위한 실행 기준이다.

핵심 목표:
1. 단일 전투 규칙 기준서(v2) 확정
2. GDD/TDD/README/Backlog/Traceability/Checklist/Changelog 동기화
3. 코드 2차 착수용 파일 단위 백로그 확정

## 2. 원칙

- 1차 범위는 문서만 수정한다.
- 코드 변경은 2차에서 수행한다.
- 확정 규칙(`RQ-*`)과 미정 항목(`TBD-*`)을 구분해 관리한다.

## 3. 단계별 실행

### P1. 기준 잠금

- `docs/srpg/new/SRPG_NEW_DIALOG_POLICY_LOCK.md` 작성/유지
- RP/패링/스킬자원/방어 구조/공용 태그 관련 충돌 문장 제거

완료 조건:
- 잠금표에 확정/미정/보류가 분리되어 있고, 하위 문서가 이를 참조

### P2. 기준서 확정

- `docs/srpg/SRPG_전투규칙_기준서_v2.md`를 단일 기준서로 고정
- GDD/TDD/마스터플랜 표현 통일

완료 조건:
- 상위 기준 문서 링크가 모든 핵심 문서에 반영됨

### P3. 운영 문서 동기화

- 대상: README, Backlog, Traceability, QA Checklist, Changelog, docs 인덱스, AGENTS
- 목적: 팀 진입점에서 동일 규칙을 보게 만들기

완료 조건:
- 문서 간 용어 및 정책 불일치가 없음

### P4. PDF 자산화

- `docs/srpg/new/프로젝트-초기-기획서-초안-외.md` 생성
- 원본 PDF 상태와 활용 가능 형태(요약/메타/후속 OCR 가이드) 문서화

완료 조건:
- PDF가 문서 링크로 검색 가능하고 후속 처리 기준이 명시됨

### P5. 코드 2차 착수 준비

- `docs/srpg/SRPG_PHASE2_CODE_BACKLOG.md` 작성
- 파일별 작업을 요구사항 ID(RQ/TBD)로 역추적

완료 조건:
- 전투/데이터/HUD/렌더링/로비/메이커 단위 착수 가능

## 4. 2차 코드 착수 범위(준비 기준)

- 전투 핵심:
  - `Assets/Scripts/SRPG/SrpGameController.cs`
  - `Assets/Scripts/SRPG/SrpBattleState.cs`
  - `Assets/Scripts/SRPG/SrpCombatResolver.cs`
  - `Assets/Scripts/SRPG/SrpSkills.cs`
- 데이터/IO:
  - `Assets/Scripts/SRPG/SrpSkillData.cs`
  - `Assets/Scripts/SRPG/SrpMapFile.cs`
  - `Assets/Scripts/SRPG/SrpDataIO.cs`
- UI/표현:
  - `Assets/Scripts/SRPG/SrpGameController.Hud.cs`
  - `Assets/Scripts/SRPG/SrpGameController.Rendering.cs`

## 5. 리스크와 대응

- 리스크: 문서 합의 전 코드 착수로 재작업 발생
  - 대응: 1차 문서 완료 전 코드 수정 금지
- 리스크: RP/패링/스킬자원 정책 혼선
  - 대응: 잠금표와 기준서에서 정책 단일화
- 리스크: 11~22 대화 반영 후 기존 DEF/GRD, 탱커, 패링 보상, `노출` 표현이 문서마다 다르게 남음
  - 대응: `SRPG_NEW_DIALOG_POLICY_LOCK.md`의 `RQ-013`~`RQ-021`을 상위 기준으로 삼고 하위 문서를 갱신
- 리스크: 23 대화/추가 논의 반영 후 총기가 `PG 무시`인지 `HP 피해 기반 PG 파급`인지 문서마다 다르게 남음
  - 대응: `RQ-022`와 `TBD-009`를 상위 기준으로 삼고, 역사적 원문/변환본은 과거 기준으로 취급
- 리스크: PDF 원문 추출 불가
  - 대응: 변환 문서에 상태/한계/후속 OCR 절차 명시
