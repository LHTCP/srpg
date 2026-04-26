# SRPG v2 백로그

기준:

- `SRPG_전투규칙_기준서_v2.md`
- `new/SRPG_NEW_DIALOG_POLICY_LOCK.md`

## 완료된 스프린트 (문서 1차 + Phase2 전투 코어/교전 이탈, P0)

- [x] 신규 대화(06~10) 정책 잠금표 작성
- [x] 전투규칙 기준서(v2) 작성
- [x] GDD/TDD/마스터플랜 정렬
- [x] README/Traceability/Checklist/Changelog 동기화 마무리
- [x] PDF/DOCX 마크다운 변환 자산 정리
- [x] 코드 2차 착수 백로그 문서 확정
- [x] Phase2 전투 코어 1차 기반 구현
  - 교전 상태 저장/클론 안전성
  - DEF/GRD 감쇠 및 수비 Guard 반응 훅
  - 라운드 AP/RP 리셋 테스트
  - EditMode 테스트 `14 passed / 0 failed`
- [x] Phase2 교전 이탈/포지셔닝 패널티 비용 기반 브릿지 구현
  - 교전 중 적 인접 상태를 벗어나는 이동에 임시 비용 추가
  - 이동 후 교전 재계산 및 교전 이탈 로그 힌트 확인
  - EditMode 테스트 `15 passed / 0 failed`
- [x] Phase2 교전 이탈 기회공격/반응 이벤트 1차 구현
  - 이동 전후 교전 차이로 기회공격 후보 판별
  - 적 RP 1 소비 후 `ReactionShot`으로 이탈자 공격
  - RP 0 미발동 및 피해 반영 테스트
  - EditMode 테스트 `17 passed / 0 failed`
- [x] Phase2 쿨다운/충전 스킬 모델 및 오버클럭 진입점 구현
  - 충전/회복/오버클럭 메타와 런타임 상태 추가
  - 쿨다운/충전 소비·회복 헬퍼 중앙화
  - FH 기반 오버클럭으로 쿨다운 단축/충전 복구
  - EditMode 테스트 `21 passed / 0 failed`
- [x] Phase2 패링 조건/태그/텔레그래프 1차 구현
  - 패링 가능자 태그와 패링 가능 스킬 메타 추가
  - 정면/근접/RP/태그 기반 패링 가능 판정 헬퍼 추가
  - 공격/스킬 타깃의 패링 가능 청록 오버레이와 HUD 범례 추가
  - EditMode 테스트 `23 passed / 0 failed`
- [x] Phase2 반응행동 파이프라인 1차 구현
  - Parry/Dodge 반응 소비와 피해 무효화 브릿지 추가
  - 명시형 ReactionShot을 AP 예약/RP 발동 오버워치로 연결
  - 오버워치 예약 버튼, 예약 범위 오버레이, 발동 로그 추가
  - EditMode 테스트 `26 passed / 0 failed`
- [x] Phase2 수비 지속 완충/탱커 다중 대응 브릿지 구현
  - 탱커 전용 태그와 기본 탱커 데이터 연결
  - 수비 태세 후속 피격 완충과 탱커 다중 교전 완충 추가
  - HUD/로그에 탱커/수비 완충 상태 최소 표기
  - EditMode 테스트 `28 passed / 0 failed`
- [x] Phase2 메이커 메타데이터 UI 확장
  - 스킬 메이커에 충전·오버클럭·패링 메타 편집/목록 표시 추가
  - 유닛 메이커에 v2 AP/RP/PG/속도, 무기/태세/방향, ParryUser/Tank 태그 편집 추가
  - 저장 시 legacy AP/PG 필드와 v2 필드 동기화
  - EditMode 테스트 `31 passed / 0 failed`
- [x] Phase2 중간 점검 보정
  - 명시 저장된 `Firearm` 무기 분류가 런타임에서 보존되도록 수정
  - 스킬 효과 AP/PG 별칭과 피해로 인한 그로기 처리를 런타임 규칙에 맞춤
  - 맵 `allowedSkillIds`와 배치 `disabledSkillIds`, `maxSkills`를 전투 스폰 시 반영
  - EditMode 테스트 `35 passed / 0 failed`
- [x] Phase2 유닛 시각 방향성 개선
  - 전투 유닛 뷰를 원기둥에서 전방이 보이는 쐐기형 삼각기둥 메시로 교체
  - `SrpFacing`에 따라 유닛 메시 전방이 North/East/South/West로 회전
  - EditMode 테스트 `36 passed / 0 failed`
- [x] Phase2 교전/둘러싸임 검증 프리셋 보강
  - 내장 프리셋 `M1EngagementLab` 추가
  - 로비에서 교전/포위 검증 랩을 선택할 수 있도록 프리셋 버튼 확장
  - 다중 교전 시작 상태, 교전 이탈 비용/기회공격, 탱커/수비 완충을 프리셋 기반으로 검증
  - EditMode 테스트 `39 passed / 0 failed`
- [x] Phase2 RP/HUD 노출 정책 정리
  - RP를 원시 수치 대신 반응 준비/소모/예약 상태 중심으로 HUD에 표시
  - 오버워치 예약 가능/불가 상태 helper 추가
  - 반응 로그의 `RP-1` 표기를 반응 발동/소모 중심 문구로 정리
  - PlayMode HUD 기대값을 새 반응 상태 표기에 맞게 보정
  - EditMode 테스트 `41 passed / 0 failed`, PlayMode 테스트 `4 passed / 0 failed`
- [x] Phase2 기획 대조 P1 보정
  - 기본공격 패링을 제거하고 패링 태그가 있는 정면 근접 스킬 위협만 패링 가능하도록 정렬
  - Dodge를 조건형 완전 무효 브릿지에서 확률형 시도/실패 흐름으로 보정
  - 측후면 피격 방어 불리 최소 브릿지를 추가하고 최종 수치표는 `TBD-002`로 유지
  - EditMode 테스트 `43 passed / 0 failed`, PlayMode 테스트 `4 passed / 0 failed`
- [x] Phase2 HUD/로그 가독성 동기화
  - 실제 오버레이 색상과 HUD 범례를 일치
  - 이동/공격/스킬/반응/상태 로그 문구를 읽기 쉬운 이벤트 단위로 정리
  - 오버워치 버튼/반응 상태/스킬 목록/패링 텔레그래프 안내 문구를 같은 용어로 통일
  - PlayMode HUD 스모크 테스트를 범례, 반응 상태, 오버워치 버튼 라벨까지 확장
  - EditMode 테스트 `43 passed / 0 failed`, PlayMode 테스트 `4 passed / 0 failed`
- [x] Phase2 오버워치 사선/횟수/해제 상세 규칙
  - 오버워치 발동을 8방향 직선 사선으로 제한
  - 장애물 타일과 중간 유닛이 사선을 차단하도록 처리
  - 예약 1회당 1회 발동, 발동/라운드 리셋 시 예약 해제 정책을 문서화
  - EditMode 테스트 `45 passed / 0 failed`, PlayMode 테스트 `4 passed / 0 failed`
- [x] Phase2 테스트 프리셋 v2 + HUD 레이아웃 개편
  - `M1QaIntegrated`를 스킬 자원/패링 가능 스킬/오버워치 사선/탱커 확인용 프리셋으로 갱신
  - HUD를 상단 전투 헤더, 보조 정보 바, 좌측 조작 콘솔, 우측 로그로 재배치
  - 텍스트 overflow를 줄이고 버튼/스킬 목록 문구를 짧은 테스트용 표기로 정리
  - EditMode 테스트 `46 passed / 0 failed`, PlayMode 테스트 `4 passed / 0 failed`
- [x] Phase2 전투 직접 조작 UI 보강
  - 좌측 전술 콘솔에 태세 선택, 최종 방향 선택, 오버클럭 실행 UI 추가
  - 태세는 행동 전 변경, 방향은 행동 종료 전 변경, 오버클럭은 기존 자원 조건을 따르도록 연결
  - EditMode 테스트 `47 passed / 0 failed`, PlayMode 테스트 `5 passed / 0 failed`
- [x] Phase2 오버클럭 성능 증폭
  - 오버클럭 메타에 다음 스킬 사용 1회 피해/회복 보너스를 추가
  - HUD/스킬 목록/로그에 오버클럭 강화 대기 상태 표시
  - EditMode 테스트 `48 passed / 0 failed`, PlayMode 테스트 `5 passed / 0 failed`
- [x] Phase2 재장전 AP 행동 1차 구현
  - 총기 유닛 전용 탄약/재장전 계약 추가
  - 기본 공격과 오버워치에 탄약 검사/소비 연결
  - 좌측 전술 콘솔 재장전 버튼과 HUD 탄약 상태 표시 추가
  - EditMode 테스트 `51 passed / 0 failed`, PlayMode 테스트 `5 passed / 0 failed`
- [x] Phase2 엄폐 AP 행동 1차 구현
  - 기존 비보행 장애물 타일을 인접 엄폐물로 판정하는 1차 계약 추가
  - 총기 기본 공격/오버워치 사격에 엄폐 완충 연결
  - 좌측 전술 콘솔 엄폐 버튼, HUD 엄폐 상태, 엄폐 오버레이 추가
  - EditMode 테스트 `54 passed / 0 failed`, PlayMode 테스트 `5 passed / 0 failed`
- [x] Phase2 상호작용 AP 행동 1차 구현
  - 맵 상호작용 포인트 계약과 런타임 클론/인접 탐색 helper 추가
  - 인접 유닛의 AP 1 상호작용 실행, owner 제한, singleUse 활성화 처리 연결
  - 좌측 전술 콘솔 상호작용 버튼, HUD 상태, 노랑 상호작용 오버레이 추가
  - EditMode 테스트 `59 passed / 0 failed`, PlayMode 테스트 `5 passed / 0 failed`
- [x] Phase2 개발용 전술 HUD 개선
  - 좌측 하단 현재 유닛 카드와 우측 하단 행동 preview 카드 추가
  - HP/PG/AP/탄약 숫자+게이지 표시와 이동/공격/스킬/상호작용 hover preview 연결
  - 텍스처 없이 uGUI/TMP 단색 패널로 구현해 정식 UI 전환 전 테스트 편의성 확보
  - EditMode 테스트 `59 passed / 0 failed`, PlayMode 테스트 `5 passed / 0 failed`
- [x] Phase2 총기 1발 고화력 + 방향성 엄폐 설계
  - 명시 `maxAmmo`가 없는 총기 기본 탄창을 전장식 총기 정책에 맞춰 1발로 변경
  - 총기 기본 공격을 HP 고화력/낮은 PG 압박 공식으로 조정
  - 선형/방향성 엄폐는 `SrpCoverSegmentData` 초안과 후속 단계로 분리
  - EditMode 테스트 `61 passed / 0 failed`, PlayMode 테스트 `5 passed / 0 failed`
- [x] Phase2 방향성 엄폐 1차 구현
  - `SrpCoverSegmentData` 맵 스키마와 런타임 로딩/클론 추가
  - 공격자-방어자 방향이 segment edge를 통과할 때만 총기 엄폐 완충 적용
  - 방향성 엄폐 overlay, HUD 범례, `M1QaIntegrated` QA segment 추가
  - EditMode 테스트 `64 passed / 0 failed`, PlayMode 테스트 `5 passed / 0 failed`

## 다음 스프린트 (코드 2차 확장, P1/P2)

- [ ] 1차 전수 점검 후속 정리 (`TBD-001`, `TBD-006`)
  - 탱커 다중 대응이 수비 태세 전용인지 별도 패시브인지 결정 전 문서에 임시 정책 명시
  - 기회공격 다중 후보 우선순위와 스킬 특수 피해 파이프라인은 다음 밸런스/규칙 스프린트 후보로 분리
- [ ] 방향성 엄폐 후속 구현 (`TBD-001`, `TBD-004`)
  - 오버워치/사선 차단과 맵 메이커 편집 UI 순서로 분리
- [ ] 오버워치 고급 우선순위/특수 지형 상호작용 (`TBD-004`)
  - 여러 오버워치 후보의 발동 우선순위와 특수 지형 상호작용은 후속으로 분리
- [ ] 마법/전장 개입 스킬 콘텐츠 검토
  - AP 행동 1차 후보 완료 후 남은 확정 기능/스킬 콘텐츠 범위를 재분류

## 후속 스프린트 (밸런스/검증, P2)

- [ ] DEF/GRD 공식 수치화 (`TBD-002`)
- [ ] 회피 계산식 확정 (`TBD-003`)
- [ ] 패링 보상/실패 수치 확정 (`TBD-005`)
- [ ] 탱커 전용 패시브 최종안 확정 (`TBD-006`)

## 보류

- 네트워크 대전
- 대규모 콘텐츠 확장
