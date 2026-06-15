# SRPG 구현 의사결정 메모

이 문서는 구현 중 임시로 고정한 브릿지 계약과 다음 목표에서 사용자 의사결정이 필요한 항목을 분리해 기록한다. 전투 규칙의 상위 기준은 `SRPG_전투규칙_기준서_v2.md`와 `new/SRPG_NEW_DIALOG_POLICY_LOCK.md`다.

## 2026-05-10 구현 브릿지

### 총기 HP-PG 파급 (`RQ-022`, `TBD-009`)

- 총격의 PG 파급량은 엄폐/방향성 엄폐/반응행동/전투 태그 보정을 거친 최종 HP 피해량을 기준으로 50%를 산정한다.
- 산정된 파급 PG는 남은 엄폐 GRD가 있으면 감소한다.
- 현재 반올림은 내림(`floor`)이다.
- 최종 비율, 반올림, 최소값, GRD 적용 순서는 밸런스 검사 후 조정 가능하다.

### 공용 전투 태그 (`RQ-019`, `RQ-020`, `TBD-007`)

- 런타임 전투 태그는 유닛 고유 태그(`SrpUnitTags`)와 분리한 `SrpCombatTag`로 관리한다.
- 지원 태그는 `표식`, `균형 붕괴`, `사살 지시`다.
- 태그는 중첩하지 않고 갱신한다.
- 다음 적대 공격이 실제 HP 또는 PG 피해를 만들면 태그를 소모한다.
- 1차 보정값:
  - `표식`: PG 피해 +2
  - `균형 붕괴`: PG 피해 +4
  - `사살 지시`: HP 피해 +2, PG 피해 +2
- `노출`은 런타임 디버프 태그로 만들지 않고, 엄폐/사선/포지션 상태 설명으로 유지한다.

### 패링 성공 보상 (`RQ-018`, `RQ-019`, `TBD-005`)

- 패링은 기존처럼 피해를 0으로 만든다.
- 패링 성공 시 공격자에게 PG 피해 8을 적용한다.
- 패링 성공 시 공격자에게 `균형 붕괴`를 부여한다.
- 큰 HP 반격과 전체 쿨타임 초기화는 기본 보상에 넣지 않았다.

### 완벽한 수비 (`RQ-013`, `RQ-014`, `TBD-002`, `TBD-006`)

- `Tank` 태그 유닛이 수비 태세이고, PG가 남아 있으며, 후방 피격이 아니면 경미 HP 피해를 0으로 만든다.
- 총격, 처단, 기회공격, 스킬 피해는 1차 구현에서 중대 HP 피해로 취급해 무효화하지 않는다.
- PG 피해는 완벽한 수비로 지우지 않는다.

### 초기 4인 대표 스킬 (`RQ-015`, `RQ-017`, `RQ-021`, `TBD-008`)

- `M1QaIntegrated`에서 공용 전투 태그를 직접 확인할 수 있게 최소 대표 스킬을 추가했다.
- 마도사: `전술 표식`, `균형 교란`
- 사격수: `사살 지시`
- 탱커/주인공 브릿지: `Tank`, `ParryUser`, 패링 가능 스킬 확인
- 캐릭터별 고유 패시브 이름/설명/전직 연계는 아직 확정하지 않았다.

## 다음 의사결정 후보

1. 공용 전투 태그 수치와 지속시간
   - 현재는 다음 적대 피해 1회 소모다.
   - 라운드 종료 소멸, 공격 종류별 소모, 중첩 금지 유지 여부를 결정해야 한다.
2. 패링 보상 수치
   - PG 8이 너무 큰지, `균형 붕괴` 지속/소모 조건을 어떻게 둘지 확인해야 한다.
3. `완벽한 수비`와 Tank 태그 최종 통합
   - Tank 태그를 그대로 캐릭터 고유 패시브로 볼지, 별도 패시브 데이터 계약을 둘지 결정해야 한다.
4. 초기 4인 고유 패시브
   - 주인공/탱커/사격수/마도사의 이름, 설명, 실제 효과를 캐릭터 시트 기준으로 확정해야 한다.
5. 방향성 엄폐 후속
   - `blocksLineOfSight`를 오버워치/원거리 기본 공격에 연결할지, 맵 메이커 편집 UI를 먼저 만들지 결정해야 한다.

## 웹 리서치 필요 여부

- 이번 구현은 프로젝트 내부 기준서와 Unity 로컬 테스트로 처리했다.
- 외부 웹 리서치는 필요하지 않았다.

## 2026-06-02 다음 P1 구현 브릿지

### 초기 4인 고유 패시브 (`RQ-015`, `RQ-017`, `RQ-021`, `TBD-008`)

- 주인공은 `breaker` 템플릿을 유지하되 표시명을 `주인공`으로 두고, `ParryUser` 태그와 `전장 적응` 패시브를 연결했다.
- 탱커는 `Tank` 태그만 유지하고 `전열 고정` 패시브를 연결했다. 주인공 전용 패링 기준에 맞춰 탱커의 `ParryUser` 태그는 제거했다.
- 사격수는 `노출 처벌` 패시브와 `사살 지시`를 같이 보유한다. PlayMode 직접 조작 스모크의 오버클럭 체험을 위해 기존 `치유의 빛`도 유지한다.
- 마도사는 `전장 해석`, `전술 표식`, `균형 교란`, `전장 장막`을 보유한다.
- 브릿지 수치:
  - `전장 적응`: 공격 적중 시 FH +3
  - `전열 고정`: 피격 후 PG +2
  - `노출 처벌`: 공격 적중 시 FH +2
  - `전장 해석`: 턴 시작 시 FH +2
- 캐릭터 이름, 서사, 전직 후 강화 방향, 최종 수치는 아직 확정하지 않았다.
- `M1QaIntegrated`는 역할 검증과 AI 시뮬레이션 균형을 위해 양측 4인 배치로 유지한다.

### 방향성 엄폐 사선 차단 (`TBD-001`, `TBD-004`)

- `blocksLineOfSight=true`인 `SrpCoverSegmentData`는 해당 edge를 통과하는 사선을 차단한다.
- 오버워치와 총기 기본 공격은 `TBD-010` 이후 같은 목표 벡터 LOS와 `blocksLineOfSight` 차단을 공유한다. 8-sector 분류는 표시/디버그/방향성 판정 보조값이며 targetability 제한이 아니다.
- 대각선 사선은 이동 단계마다 수평/수직 edge를 함께 검사하는 최소 브릿지다.
- 맵 메이커 엄폐 segment 편집 UI는 선행 조건이 아니라 후속 UX로 분리했다. 현재는 프리셋/JSON 데이터로 검증 가능하다.

### 오버워치 후보 우선순위와 특수 지형 (`TBD-004`)

- 여러 오버워치 후보가 동시에 발동 가능하면 가까운 사수, 빠른 사수, 낮은 unit id 순으로 1명을 선택한다.
- 특수 지형 상호작용은 현재 데이터 구조 안에서 `SrpInteractionPointData`와 `blocksLineOfSight` 엄폐 segment까지만 사용한다.
- 문 열림, 스위치-지형 연쇄, 승리 조건 변화 같은 복합 효과는 후속으로 둔다.

### 마법/전장 개입 스킬 (`RQ-011`, `RQ-021`, `TBD-008`)

- 현재 전투 코어와 충돌이 적은 마법 후보는 PG 회복/표식/제어 축으로 분류했다.
- 최소 구현 스킬은 `전장 장막`이다.
  - 대상: 사거리 3 내 아군 1명
  - 효과: PG +4
  - 자원: 쿨다운 2, 충전 1, 회복 2턴, 오버클럭 FH 5, 다음 사용 PG 회복 +2
- 지형 생성, 광역 장판, 강제 이동은 아직 데이터 구조와 UI가 부족해 후속으로 둔다.

## 2026-06-02 다음 의사결정 후보

1. 초기 4인 고유 패시브의 최종 이름/수치/전직 연계
2. `전열 고정`을 Tank 태그/`완벽한 수비`와 통합할지, 별도 고유 패시브로 유지할지
3. `전장 장막`을 PG 회복으로 유지할지, 엄폐/사선/태그와 상호작용하는 전장 스킬로 바꿀지
4. 맵 메이커에서 방향성 엄폐 segment를 어떤 UI로 배치/edge 선택하게 할지
5. 특수 지형 상호작용을 전투 규칙으로 확장할 범위

## 2026-06-02 웹 리서치 필요 여부

- 이번 구현은 프로젝트 내부 기준서와 기존 Unity 코드/테스트로 처리했다.
- 외부 웹 리서치는 필요하지 않았다.

## 2026-06-03 첫 전투 프로토타입 프리셋

### QA 맵과 첫 전투 맵 분리

- `M1QaIntegrated`는 기능 연결을 확인하는 QA 맵으로 유지한다.
- 새 내장 프리셋 `M1OpeningPrototype`은 첫 전투 한 판의 판단용 맵으로 추가했다.
- 기본 전투 진입값과 로비 첫 선택은 `M1OpeningPrototype`으로 교체했다.
- `M1QaIntegrated`는 로비 후순위 QA 선택지와 코드/자동 테스트 회귀 확인용 deprecated 프리셋으로 유지한다.
- 승리 조건 시스템은 아직 단순하므로 실제 승리는 적 전멸을 유지한다.
- 배치는 전술 장교를 목표처럼 보이게 두되, 별도 지휘관 처치 승리 조건은 추가하지 않았다.

### 맵/유닛 임시값

- 맵 크기는 12x9로 고정했다.
- 접근 루트는 북쪽 사격 루트와 남쪽 돌입/상호작용 루트 중심이다.
- 상호작용 포인트는 `신호 장치` 1개만 두고, 현재 효과는 기존 상호작용 계약의 활성화/소유자 갱신까지만 사용한다.
- 적 역할은 총기 압박병, 근접 돌입병, 방어형 적, 측면 교란병, 전술 장교로 나누었다.
- 플레이어가 보는 기본 스킬 설명에서는 `브릿지`/`호환용` 표현을 줄였지만, 아래 수치는 아직 검증용 임시값이다.
  - `전장 적응`: 공격 적중 시 FH +3
  - `전열 고정`: 피격 후 PG +2
  - `노출 처벌`: 공격 적중 시 FH +2
  - `전장 해석`: 턴 시작 FH +2
  - `전장 장막`: 아군 PG +4

## 2026-06-03 다음 의사결정 후보

1. `M1OpeningPrototype`의 실제 평균 종료 턴이 6~10턴에 들어오는지 확인하고 적 수/HP/PG/배치 간격을 조정
2. 북쪽 사격 루트와 남쪽 돌입 루트 중 한쪽이 명백한 정답이 되는지 확인
3. 총기 압박병의 엄폐/사선 차단 위치가 답답함이 아니라 읽을 수 있는 위협으로 작동하는지 확인
4. 전술 장교를 목표처럼 보이게 할지, 별도 지휘관 처치/상호작용 승리 조건을 도입할지 결정
5. 상호작용 포인트 `신호 장치`의 실제 전투 효과를 유지/확장/삭제할지 결정

## 2026-06-07 전투 UX 피드백 레이어 P1

### 임시 시각 규칙

- 현재 행동 유닛 ring은 노랑, 선택 유닛 ring은 청록, hover 유닛 ring은 흰색으로 둔다.
- 같은 유닛에 여러 상태가 겹치면 현재 행동 ring은 바깥쪽, 선택 ring은 중간, hover ring은 안쪽/상단으로 보이게 반지름과 높이를 다르게 둔다.
- ZOC/교전 표시는 타일 오버레이가 아니라 유닛 위 world-space badge로 둔다.
  - 교전 상태는 `교전` 자홍 badge
  - ZOC 인접 상태는 `ZOC` 노랑 badge
- 기존 타일 오버레이 색상 의미는 유지한다.
  - 초록: 이동
  - 빨강/비강: 공격/위험
  - 주황: ZOC/주의 타일
  - 보라: 스킬 대상
  - 청록: 패링 텔레그래프
- 이번 P1의 badge/ring 색은 유닛 위 표식이므로 위험영역 타일 의미와 직접 충돌하지 않게 배치한다.

### Floating text / flash 분류

- 턴 시작/턴 종료는 해당 유닛 위에 짧은 floating text를 띄운다.
- 공격, 기회공격, 오버워치 발동, 스킬 준비/사용, 재장전, 엄폐, 상호작용, 오버클럭은 즉시 world-space text를 띄운다.
- 피해/부정 변화는 붉은 flash, 회복/긍정 변화는 녹색/청록 flash, 턴/선택/중립 피드백은 노랑/흰색 flash로 둔다.
- 스킬 결과 flash는 HP/PG 변화량을 기준으로 판단한다. 버프/디버프 전용 정교화는 후속 VFX 단계에서 조정한다.

### 비범위와 후속

- 새 전투 규칙, 파티클/VFX 고도화, 행동 순서 패널, 메이커 드롭다운/툴팁 개선은 이번 P1 비범위다.
- 맵 메이커 엄폐 segment 편집 UI와 모바일/WebGL 전용 조정도 비범위다.
- TMP 기본 폰트는 한국어 UI 렌더링을 위해 `Pretendard-Regular SDF.asset`를 유지한다. 로컬 PlayMode 검증 전에는 LFS 원본 asset을 받아야 하며, `LiberationSans SDF.asset`로 대체하면 한국어 glyph가 누락될 수 있다.
- 실제 Unity 에디터 플레이에서는 ring 두께, badge 높이, floating text 지속시간을 화면 가독성 기준으로 추가 튜닝할 수 있다.

### 검증

- EditMode: `76 passed / 0 failed`
- PlayMode: `6 passed / 0 failed`
- PlayMode에 현재 행동/선택/hover ring, ZOC/교전 badge, 턴 시작/종료, 스킬 준비/사용 feedback 계약을 추가 검증했다.

## 2026-06-07 PR #61 실플레이 피드백 보정

### Ring 기준 보정

- ring은 유닛 발아래 타일 위에 얹히는 decal/annulus 표식을 기준으로 한다. 타일 전체를 다시 칠하는 overlay가 아니다.
- 현재 타일 cube는 중심 `y=0`, 높이 `0.15`이므로 표면은 `y=0.075`다. ring은 이보다 위에 있어야 하며, 현재 행동/선택/hover 순으로 `0.110 / 0.123 / 0.136` 높이를 사용한다.
- 기존 ring mesh는 위에서 볼 때 뒷면이 될 수 있어, triangle winding을 위쪽 normal 기준으로 뒤집었다.
- 현재 행동 ring은 가장 바깥 노랑, 선택 ring은 중간 청록, hover ring은 안쪽 흰색으로 둔다. 같은 유닛에 겹쳐도 반지름과 y offset이 모두 달라야 한다.
- 실플레이 2차 피드백 기준으로 ring은 큰 경고 원이 아니라 발아래 얇은 표식으로 보이게 current/selected/hover 반지름을 낮추고 선 두께를 줄였다.
- 색상도 고채도 원색 대신 차분한 amber/teal/ivory 계열로 낮춘다.

### Floating feedback 가독성 기준

- feedback text는 전체 `2.15s`, 초기 `1.25s` 완전 불투명 유지 후 후반 fade out을 기준으로 한다.
- TMP 기본 폰트는 한국어 glyph 보존을 위해 `Pretendard-Regular SDF.asset`를 유지한다. `LiberationSans SDF.asset` 대체는 금지한다.
- 텍스트 크기를 키우고 TMP outline과 검은 shadow를 적용해 전장 배경 위에서 대비를 확보한다.
- 실플레이 2차 피드백 기준으로 text 크기는 과하게 크지 않게 낮추되, outline/shadow와 hold time으로 읽힘을 보완한다.
- 같은 유닛에 짧은 시간 안에 feedback이 여러 개 뜨면 per-unit active lane 수로 시작 위치를 보드 평면의 screen-up/side 방향에 분산한다. 턴 종료+턴 시작, 스킬 준비+사용이 같은 위치에 완전히 겹치면 안 된다.
- 탑다운 카메라에서는 `Vector3.up` 이동이 화면상 거의 보이지 않으므로, 카메라 up/right를 보드 평면에 투영한 방향으로 이동/stack한다. world Y 이동은 연결감을 잃지 않는 작은 보조값만 둔다.

### 검증

- PlayMode에 ring 높이, ring 반지름/높이 구분, feedback duration/hold, 같은 유닛 feedback 2개 이상의 시작 위치 분산 계약을 추가했다.
- 로컬 Unity batchmode EditMode: `76 passed / 0 failed`.
- 로컬 Unity batchmode PlayMode: `6 passed / 0 failed`.

## 2026-06-08 첫 전투 밸런스 관찰 P2

### AI matrix 결과

- `M1OpeningPrototype` 전용 EditMode 관찰 테스트 `Run_M1OpeningPrototype_Ai_Policy_Matrix_For_BalanceObservation`을 추가했다.
- 300 trials, max 16 rounds 기준 핵심 정책 케이스 평균 종료 라운드는 Heuristic vs Random `8.31`, Random vs Heuristic `7.65`, Heuristic vs Heuristic `8.00`이다.
- 세 핵심 케이스가 6~10라운드 목표 범위에 들어오므로 이번 차수에서는 적 수, 초기 배치 간격, HP/PG, 속도, 사거리, 탄약, 엄폐/상호작용 배치를 조정하지 않는다.
- Random vs Random은 평균 `15.64`라운드, 무승부 `0.827`로 장기전 편향이 크지만 완전 랜덤 정책 관찰용이므로 밸런스 게이트에서 제외한다.

### 남은 판단

1. Heuristic vs Heuristic에서 owner0 승률이 `1.000`이므로, 첫 전투가 플레이어 우세 학습 전투인지 더 팽팽한 AI 미러 검증 맵이어야 하는지는 후속 플레이 세션에서 확인한다.
2. 북쪽 사격 루트와 남쪽 돌입/상호작용 루트가 실제 사람 플레이에서도 서로 다른 판단으로 읽히는지는 화면 관찰 표본이 더 필요하다.
3. `blocksLineOfSight` 엄폐는 자동 시뮬레이션에서 총기 HP 비중과 근접 PG 비중을 무너뜨리지 않았지만, 시각적으로 답답한 차단인지 읽을 수 있는 위협인지는 `TBD-012` 화면 문법과 함께 재확인한다.

## 2026-06-08 첫 전투 화면 관찰 표본

### 표본 방식

- PlayMode 관찰 테스트 `SrpM1OpeningObservationTests.M1OpeningPrototype_Captures_FirstScreen_RouteObservation`을 추가했다.
- 테스트는 `M1OpeningPrototype` 첫 화면, 위험영역/이동 hover, 남쪽 `신호 장치` hover, ring/floating text 표본을 `TestResults/SrpPlayObservation/`에 PNG와 Markdown으로 남긴다.
- batchmode 캡처는 camera render 기반이라 HUD overlay는 이미지가 아니라 Markdown 텍스트 필드로 기록한다.

### 관찰 결과

- 첫 화면에서 아군 4명과 적 5명의 좌우 대치, 북쪽 총기 압박병과 남쪽 돌입 병력의 위치 차이는 보인다.
- 첫 행동 유닛의 current/selected ring과 turn-start floating text는 실제 tile 위에서 충분히 읽힌다. PR #61의 얇은 floor decal 기준은 유지한다.
- 위험영역을 켜면 동쪽 전장이 압박권이라는 정보는 강해지지만, 북쪽 사격 루트와 남쪽 상호작용 루트의 차이가 전체 위험색에 묻힌다.
- `신호 장치`는 데이터와 hover preview에는 잡히지만, 전장 world 캡처에서는 독립 상호작용 목표처럼 강하게 보이지 않는다.
- `blocksLineOfSight` 엄폐는 북쪽 총기 압박병 옆 위협으로 배치되어 있지만, 현재 tile tint만으로는 "사선을 조절하는 엄폐"라는 뜻이 약하다.
- 총기 압박병, 근접 돌입병, 방어형 적, 측면 교란병, 전술 장교의 역할 차이는 배치와 무기/방향으로 일부 드러나지만, 첫 화면만으로는 방어형 적과 전술 장교의 기능 차이가 충분히 설명되지는 않는다.

### 판단

- 데이터 보정은 하지 않는다. AI matrix가 6~10라운드 목표를 통과했고, 현재 표본에서 발견된 문제는 적 수치/배치보다 화면 문법과 목표 강조의 문제에 가깝다.
- Heuristic vs Heuristic owner0 승률 `1.000`은 당장은 플레이어 우세 학습 전투로 받아들인다. 더 팽팽한 미러 검증은 첫 전투 프리셋이 아니라 별도 QA/밸런스 표본에서 다루는 편이 낫다.
- 후속 우선순위는 `TBD-012` 타일 overlay 문법과 `TBD-011` 행동 순서/초기 판단 지원이다. `TBD-010` 총기 조준 문법도 북쪽 사격 루트를 더 명확하게 보여줄 때 함께 재검토한다.

## 2026-06-08 P2 후보: 총기 발포 방향/조준 문법 (`TBD-010`)

- 현상: 총기 발포 방향이 기본 공격/오버워치/발포 연출에서 모두 8방향 직선 사선처럼 고정되어 보이는 문제가 있다.
- 기존 1차 구현 범위: 명시형 `ReactionShot`/오버워치는 한때 8방향 직선 사선, 장애물/유닛/`blocksLineOfSight` segment 차단으로 구현되어 있었다. `TBD-010`에서 8방향 직선 제한은 폐기하고 목표 벡터 LOS로 통합한다.
- 문제 해석: 총기 기본 공격과 오버워치가 모두 8방향 고정 사선처럼 보이면 플레이어가 보는 조준 가능 방향, 발포 연출, 타일 overlay, 유닛 facing 4방향/정면·측면·후방 판정이 서로 충돌해 보일 수 있다.
- P2 확인 항목:
  1. 총기 기본 공격과 오버워치의 조준 가능 범위를 목표 벡터 LOS로 통합할지, 무기별 arc로 분리할지 결정
  2. 8-sector를 targetability 제한이 아니라 UI/facing/엄폐 설명용 보조값으로만 둘 수 있는지 검토
  3. 유닛 facing 4방향, 방향성 엄폐 edge, `blocksLineOfSight` 차단이 발포 방향 표시와 같은 언어로 읽히는지 실제 플레이 화면에서 검증
  4. 확정 후 `SrpOverwatch`, `SrpGameController`, `SrpGameController.Rendering`, 관련 PlayMode 시각/계약 테스트 갱신

## 2026-06-09 총기 발포 방향/조준 문법 브릿지 결정 (`TBD-010`)

### 브릿지 결정

- 총기 기본 공격과 오버워치 사격의 targetability 계약을 통합한다.
- 기본 총기 공격과 오버워치는 `SrpFirearmAim`을 사용해 공격자-대상 중심 360도 벡터의 LOS를 검증한다.
  - 8방향 직선이 아니어도 사거리, walkable target, 중간 유닛/장애물, `blocksLineOfSight` segment 차단을 통과하면 발포 가능하다.
  - `SrpOverwatch.IsTileInLineOfSight`는 같은 LOS helper를 그대로 사용하며 8방향 직선 lane 제한을 추가하지 않는다.
  - 기본 공격 hover preview에는 황색 aim line과 `총기 기본 조준` 문구를 표시한다.
- 8-sector(`SrpAimSector8`)는 `atan2` 기반 표시/디버그/방향성 판정 보조값으로만 둔다. dx/dy가 가로/세로/대각선일 때만 발포 가능하다는 제한은 없다.
- 오버워치 overlay는 기존 청색 경계 범위 문법을 유지하고, 기본 공격 aim line과 섞지 않는다.
- 발포 시 총기 유닛 facing은 목표 벡터의 우세 축 방향으로 갱신한다. 현재 유닛 시각 방향성은 4방향만 지원하므로 diagonal facing은 만들지 않는다.

### 후속 의사결정

- 총기별 arc, 산탄/원뿔형 조준, diagonal facing, 정식 발포 VFX/애니메이션은 이번 브릿지 범위가 아니다.
- 타일 overlay 전체 문법(`TBD-012`)을 재정리할 때 aim line이 공격 가능 범위/위험 범위/오버워치 경계와 충분히 구분되는지 실제 플레이 화면에서 다시 검증한다.
- 목표 벡터 LOS의 샘플링 방식은 현재 프로토타입용 보수적 tile path다. 정식 탄도/시야 수학이 필요해지면 별도 `SrpLineOfSight` 모듈로 승격한다.

## 2026-06-08 P2/P3 UX 후속 범위 명시 (`TBD-011`~`TBD-013`)

- 행동 순서 패널 (`TBD-011`)
  - 현재 턴/라운드 정보는 상단 HUD에 남기되, 행동 순서와 다음 행동 후보는 별도 initiative/turn order tracker로 분리하는 방향을 P2 후보로 둔다.
  - 최소 기준은 현재 유닛 강조, 다음 3~5명 미리보기, 초상/아이콘 열 구성이다.
- 타일 overlay 시각 문법 (`TBD-012`)
  - PR #61의 유닛 발밑 ring은 유닛 상태 레이어이고, 이동/공격/ZOC/오버워치/패링/상호작용 타일 overlay는 별도 레이어다.
  - 이동 가능 범위는 중심 원/작은 그림자, 공격 가능 범위는 외곽 danger/테두리, ZOC는 얇은 경고 ring 후보로 분리한다.
  - 오버워치/패링/상호작용은 같은 시각 문법에 색상만 달리하는 방식이 충분한지 실제 플레이 화면에서 검증한다.
- 메이커 화면 UX (`TBD-013`)
  - 효과유형 드롭다운 스크롤 지연은 우선 재현 확인이 필요하다.
  - 입력 가능 값과 필드 의미 툴팁은 유용하지만 전투 플레이 가독성보다 후순위인 P3로 둔다.

## 2026-06-09 행동 순서 패널 분리 (`TBD-011`)

### 확정한 범위

- 상단 HUD는 라운드, 현재 입력 상태, 위험영역 ON/OFF, 맵 이름 요약만 남긴다.
- 현재 행동 유닛과 다음 행동 순서는 캔버스 상단 우측의 `TurnOrderTrackerPanel` icon strip으로 분리한다.
- 현재 행동 유닛은 더 큰 얼굴 토큰, 금색 frame, 하단 포인터로 강조한다.
- 다음 순서는 3~5개의 작은 얼굴 토큰으로 표시한다. 현재 정식 초상화/역할 아이콘 에셋은 없으므로 런타임에서 owner 색상과 무기 계열 디테일이 들어간 임시 토큰 sprite를 생성한다.

### 배치 판단

- 새 패널은 로그 패널 위/안이 아니라 캔버스의 별도 상단 우측 UI로 둔다. 뮤제닉스 레퍼런스처럼 전장 상단에 얇은 아이콘 줄로 읽히게 하되, 좌측 조작 콘솔과 우측 로그 panel의 고정 영역은 건드리지 않는다.
- 정식 초상화/아트 에셋 제작은 범위 밖이므로 외부 다운로드 에셋 대신 코드 생성 토큰을 사용한다. 이후 실제 캐릭터 초상화가 생기면 `portrait.sprite` 교체만으로 대체할 수 있게 둔다.

### 검증

- PlayMode HUD 테스트에 패널 존재, 로그 패널과의 분리, 현재 유닛 아이콘 강조, 3~5명 preview, 턴 종료 후 current icon 갱신 검증을 추가했다.
- `scripts/validate-repo.sh` 통과.
- Unity batchmode EditMode: `77 passed / 0 failed`.
- Unity batchmode PlayMode: `7 passed / 0 failed`.

## 2026-06-09 타일 overlay 시각 문법 1차 구현 (`TBD-012`)

### 확정한 문법

- PR #61의 current/selected/hover 유닛 발밑 ring은 유닛 상태 레이어로 유지한다. tile overlay는 별도 `SrpTileOverlayGrammarLayer` 아래 얇은 floor marker로 렌더링한다.
- 이동 가능 범위는 타일 중심 작은 원 marker로 둔다. 이동 후보가 전장을 넓게 채우더라도 경로 가능성만 낮은 밀도로 읽히게 한다.
- 공격 가능/위험 영역은 타일 외곽 danger 테두리로 둔다. `M1OpeningPrototype` 북쪽 사격 루트는 전체 빨강 채움이 아니라 외곽 압박으로 읽히게 한다.
- ZOC/교전권 tile 힌트와 패링 가능 telegraph는 얇은 warning ring 계열로 둔다. ZOC/교전 unit badge는 기존 world-space badge를 유지한다.
- 상호작용 목표는 노랑 objective diamond marker로 둔다. 남쪽 `신호 장치`가 이동/위험 채움에 섞이지 않고 목표로 읽히는 것을 우선한다.
- 오버워치와 엄폐는 별도 테두리 계열, 스킬과 intent target은 marker 계열로 둔다. 색상만 다른 동일 채움 방식은 사용하지 않는다.

### 레이어 기준

- 현재 타일 표면은 `y=0.075`이며 tile overlay marker는 `TileSurfaceY + 0.008`부터 `TileSurfaceY + 0.031` 사이에 둔다.
- PR #61 현재 행동 ring은 `TileSurfaceY + 0.035` 이상이므로 tile overlay가 유닛 발밑 ring을 덮지 않는다.
- PlayMode 계약은 이동 marker, 위험 테두리, ZOC ring, 상호작용 objective marker 존재와 tile overlay 최대 높이가 current ring보다 낮은지를 검증한다.

### 후속 결정

- 실제 Unity 에디터 플레이에서 marker 크기, 선 두께, 채도는 추가 조정할 수 있다.
- 총기 발포 방향/조준 문법(`TBD-010`)은 이번 overlay 문법과 분리했다. 공격/위험 테두리는 사격 가능성을 보여주지만 발포 방향 arc나 조준선 확정 문법은 아니다.

### 검증

- `scripts/validate-repo.sh` 통과.
- Unity EditMode 테스트 통과: `77 passed / 0 failed`.
- Unity PlayMode 테스트 통과: `7 passed / 0 failed`.
- PlayMode 관찰 테스트는 `M1OpeningPrototype` 첫 화면의 이동 marker, 위험 테두리, ZOC ring, `신호 장치` objective marker, PR #61 ring/floating feedback 표본을 다시 캡처한다.

## 2026-06-09 전투 UX 추가 피드백 후속 (`TBD-014`, `BUG-001`)

### 결정한 후속 방향

- 사용자 노출 명칭은 `오버워치` 대신 `경계태세`를 사용한다. 내부 코드 식별자 `SrpOverwatch`는 단기적으로 유지할 수 있지만, 버튼/로그/HUD/floating text/문서의 플레이어-facing 문구는 `경계태세`로 교체한다.
- 경계태세 발동 문구는 별도 UX 작업에서 실제 화면 기준으로 고른다. 현재 후보는 예약 `경계태세 준비`, 발동 `경계사격!` 또는 `경계태세 발동!`, 해제 `경계태세 해제`다.
- 경계태세로 사망한 유닛이 즉시 렌더링에 반영되지 않는 문제는 버그로 추적한다. 피해 적용, 사망 판정, 유닛 mesh/링/행동 순서/HUD 갱신이 같은 프레임 또는 발동 연출 직후 일관되게 보이는지 PlayMode로 검증한다.
- 공격/위험 범위의 다이아몬드형 외곽선은 실제 플레이 화면에서 과도한 시각 소음으로 보일 수 있다. 공격 범위 표시는 타일 전체 채움도, 전장 전체 다이아몬드 선도 아닌 더 조용한 문법으로 재검토한다.

### 다음 구현 후보

- 공격 범위는 기본적으로 낮은 채도/낮은 밀도의 중심 marker 또는 짧은 edge segment를 사용하고, 유닛 hover/선택 시에만 범위를 확장 표시한다.
- 전체 위험영역 토글은 “읽기용”이어야 하며, 전장이 움직이는 선 패턴처럼 보이면 실패로 본다.
- 경계태세 발동으로 대상이 사망하는 시나리오를 QA 프리셋 또는 전용 PlayMode 테스트에 넣고, 사망 mesh 제거/행동 순서 갱신/HUD 로그를 함께 검증한다.

## 2026-06-10 전투 UX 추가 피드백 구현 (`TBD-014`, `BUG-001`, `TBD-012` 후속)

### 사용자-facing 명칭

- 사용자 노출 명칭은 `경계태세`로 고정한다.
- 내부 코드 식별자 `SrpOverwatch`, `overwatchArmed` 등은 이번 범위에서 유지한다. 대규모 리네임은 별도 리팩터링 후보로 남긴다.
- 화면 문구는 짧게 읽히는 쪽을 우선해 예약 `경계태세 준비`, 발동 `경계사격!`, 불가 `경계태세 불가`, 예약 상태 `경계태세 준비 중`으로 둔다.
- 로그 발동 문구는 `경계사격: 사수 -> 대상` 형식을 사용한다.

### BUG-001 사망 즉시 갱신

- 경계태세 발동으로 대상이 사망하면 `RemoveUnit`/교전 재계산 직후 선택/hover/aim overlay를 정리하고 `RefreshUnitViews()`와 `UpdateHud()`를 호출한다.
- 이후 기존 activation 종료 흐름이 다음 유닛으로 넘기며, 행동 순서 패널은 제거된 유닛을 보여주지 않는다.
- PlayMode에 현재 행동 유닛이 경계태세로 사망하는 전용 3유닛 맵을 추가해 유닛 mesh 제거, HUD/행동 순서 갱신, `경계사격!` floating text, 사망 로그를 함께 검증한다.

### 공격/위험 overlay 후속

- 추가 확인 결과, 사용자가 어지럽다고 지적한 파란 선 다이아몬드는 공격/위험 범위가 아니라 경계태세 범위였다.
- 공격/위험 범위와 경계태세 범위는 모두 낮은 밀도의 중심 marker로 표시한다. 타일 전체 채움과 전장 전체 다이아몬드 선은 사용하지 않는다.
- 이동 가능 범위는 중심 marker, ZOC/패링은 warning ring, 상호작용은 objective marker로 유지해 범위 marker와 의미를 분리한다.
- PlayMode와 관찰 테스트는 공격/위험 및 경계태세 레이어가 tile tint 없이 mesh marker를 사용하는지 검증한다.

### 검증 메모

- `git diff --check`와 `scripts/validate-repo.sh`는 통과했다.
- marker 후속 보정 뒤 Unity batchmode EditMode `79 passed / 0 failed`, PlayMode `9 passed / 0 failed`를 확인했다 (`TestResults/EditMode-TBD-014-review-fix.xml`, `TestResults/PlayMode-TBD-014-review-fix.xml`).

### 후속 의사결정

- 경계태세 사격 VFX/애니메이션, marker 크기/채도/펄스, 정식 초상/행동 순서 아트는 실제 에디터 플레이와 아트 에셋이 생긴 뒤 별도 튜닝한다.
## 2026-06-15 tactical HUD drawer and cover semantics decisions

- 전투 HUD 보조 조작은 고정 side panel이 아니라 tab/drawer로 연다.
  - 기본 노출은 `CommandRailPanel`, `ActiveUnitCardPanel`, `InspectorPreviewPanel`, 접힌 `LogDrawerPanel`, `TurnOrderTracker`로 제한한다.
  - `SecondaryActionPanel`은 기본 닫힘이며 `SecondaryActionTabStripPanel`에서 `태세/방향`, `전술 보조`, `시스템` 중 하나만 연다.
  - 열린 drawer는 최소 320px 이상 읽기 폭을 보장한다.
  - page별 높이는 내용량에 맞춘다: `태세/방향` 210px, `전술 보조` 124px, `시스템` 104px.
- 점유형 엄폐물과 방향성 edge cover segment를 분리한다.
  - `SrpCoverObjectData`: 비보행 점유형 장애물/폐허. 해당 타일은 `walkable=false`, `CanStandAt=false`이며 중앙 visual을 가진다.
  - `SrpCoverSegmentData`: 유닛이 설 수 있는 타일의 방향성 edge 엄폐. 이동 점유를 막지 않고 edge 위 낮은 벽/판자 visual로 표현한다.
- `M1OpeningPrototype` 중앙 비보행 폐허 타일은 현재는 엄폐 가능한 장애물로 해석한다.
  - 중앙 비보행 칸을 빈 구멍으로 바꾸려면 후속으로 terrain semantics를 추가하고 `coverObjects`에서 제거한다.
  - 이번 범위에서는 중앙 폐허 visual을 생성해 플레이어가 왜 엄폐가 되는지 납득할 수 있게 한다.
## 2026-06-15 skill selection drawer decision

- 스킬 선택 UI는 `CommandRailPanel`/`ContextPanel` 안에 끼워 넣지 않는다.
  - 핵심 명령 버튼은 `CommandRailPanel`에 남기되, `스킬` 버튼은 별도 `SkillSelectionDrawer`를 여는 트리거다.
- command-adjacent `ContextPanel`은 제거한다.
  - 스킬 목록이 빠진 뒤에도 왼쪽 명령 rail 바로 옆에 설명 칸이 남으면 이전의 한 글자 UI 공간처럼 읽힌다.
  - 현재 유닛/hover/preview 설명은 `ActiveUnitCardPanel`과 `InspectorPreviewPanel`로 보낸다.
- `SkillSelectionDrawer`는 캔버스 직속 drawer로 배치한다.
  - anchor는 `CommandRailPanel` 바로 우측으로 고정해 `CommandRailPanel -> SkillSelectionDrawer` 흐름이 한 덩어리처럼 보이게 한다.
  - preferred width 520px, minimum readable width 420px.
  - skill row minimum height 56px.
  - label policy는 `NoWrap + Ellipsis`; 상세 설명은 bottom tactical cards/`InspectorPreviewPanel`로 보낸다.
  - drawer 안에는 `닫기` 버튼을 두고, 이미 열려 있을 때 `스킬` command를 다시 누르면 닫힌다.
- 로그 drawer는 기본 접힘 상태로 시작한다. 플레이어가 로그를 확인하려는 경우에만 우측 `로그` rail을 눌러 넓은 로그를 연다.
- HUD 검수 캡처는 두 계층으로 둔다.
  - camera-render 캡처는 전장/오버레이 표본용이다.
  - ScreenCapture/GameView 캡처는 ScreenSpaceOverlay HUD 검수용이며 skill drawer, secondary drawer, log expanded/collapsed 상태를 남긴다. 캡처 직전 visible body/collapsed state assertion을 둬 파일명과 실제 UI 상태가 어긋나지 않게 한다.
