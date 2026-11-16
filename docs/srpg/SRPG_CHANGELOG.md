# SRPG 변경 이력

## 2026-11-16

- 9차: 전투 중 되감기 횟수 설정
  - 설정 `SrpGameSettings.RewindLimit`: 0 = 끔, N = 한 판에 N회, -1(`SrpRewindBudget.Unlimited`) = 무제한. 기본값은 무제한이라 기존 플레이는 그대로다. 맵별 값은 두지 않았다(맵 형식 변경 없음)
  - 순수 로직 `SrpRewindBudget`: 남은 횟수 계산, 소모, 판 시작 초기화, 로비 순환 값
  - 컨트롤러: 판 시작과 맵 다시 적용 때 횟수를 채운다. 사용 횟수는 스냅숏 밖에 두어 되감기로 돌아가지 않는다. 되감기 단위(플레이어 행동 하나)와 적 AI 차례에 스냅숏을 쌓지 않는 동작은 그대로다. 핫시트 진영별 횟수는 설정 `SrpGameSettings.RewindPerSideInHotseat`(기본 공유). 진영별이면 스냅숏에 행동한 진영을 적고, 자기 진영 행동만 자기 횟수로 되감는다
  - HUD: 한도가 있으면 되감기 버튼에 남은 횟수를 붙이고 0이면 끈다. 상태 줄에 남은 횟수, "되감기 꺼짐", "되감기 횟수 소진"
  - 로비: 적 AI 버튼 옆에 되감기 버튼(끔 → 3 → 5 → 10 → 무제한)과 핫시트 버튼(공유/진영별). 패널 높이를 늘리지 않으려고 세 버튼을 한 줄에 두고 적 AI 버튼 글자를 줄였다
  - 기획 미정 항목은 설정값으로 두고 추천안을 기본값으로 했다(사용자 방침): 횟수 무제한, 핫시트 공유. 되감기 단위는 행동 하나로 고정. 계획 문서 5절 9차
  - 테스트: `SrpM1Phase9Tests` 6건(전체 242개 통과)
  - 남은 기획 질문 Q19~Q24(로어상 되감기 존재 여부, 판마다/캠페인 풀, 되감기 단위, 전투 중 자유 저장, 핫시트 진영별 횟수, 이동 규칙 AP1/AP2)는 계획 문서 16.2절
  - 검증: Unity 미실행(라이선스 미활성). 로직 테스트 242개 통과, `scripts/validate-repo.sh` 통과, `scripts/run-compile-check.sh` 통과(오류 0). 로비 버튼과 HUD 표시는 화면으로 확인하지 못했다. 확인 항목은 계획 문서 16.4절

## 2026-11-15

- 8차: 기획 결정 반영(Q16, Q17, Q18)
  - Q16: 추가 승리 조건 네 가지(`survive_rounds` N라운드 버티기, `reach_tile` 칸 도달·탈출, `unit_lost` 유닛 보호, `hold_point` 거점 N라운드 유지)와 패배 조건 목록(`victory.defeatConditions`). 판정기는 "어느 진영에 조건이 성립했는가"만 판정하고(`ISrpVictoryCondition.IsMet`), 조건을 승리 목록과 패배 목록 중 어디에 넣었는지가 결과를 정한다. 판정 순서는 전멸 > 패배 조건 > 승리 조건
  - 라운드 종료 훅(`SrpBattleFlow.EndRound`): 끝난 라운드 수와 거점 진행도를 전투 상태에 저장하고, 런타임과 시뮬레이션이 라운드가 끝날 때 승패를 판정한다
  - 적 AI가 내장 네 조건을 안다: 도달 칸으로 이동, 서면 패배하는 칸 회피, 보호 대상을 지휘관처럼 취급, 거점 진입·유지·저지, 시간에 쫓기면 버티지 않음
  - 맵 메이커: 추가 조건 줄(종류, 승리/패배 목록, 대상, 수치, 조건 칸, 추가/삭제)과 조건 칸 표시(`SrpMapConditionEdit`)
  - 전투 화면: 조건이 가리키는 칸의 바닥 색, 시작 로그의 "패배 조건" 줄, 라운드 끝의 거점 진행도 로그
  - 내장 프리셋 `M1ConditionLab`(추가 조건 검증 랩) 추가. 로비 프리셋 목록에 추가
  - Q17: 태세 선택 시점은 현행 유지. 기준서 §4에 해석 추가
  - Q18: 로비에 적 AI 켜기/끄기 버튼(끄면 양 진영 직접 조작)
  - Q18: 총기 종류 데이터(`firearmType`: 자동/소총/권총). 주무장이 총기가 아닌 유닛의 부무장 사격은 권총이고 HP 피해는 공격력 + 4다(소총은 공격력 × 2 + 4 그대로)
  - Q18: 회피 성공률에 방향(정면 50 / 측면 35 / 후면 20)과 속도 차이(1마다 2%p, 10~80%) 가중치(`SrpCombatResolver.GetDodgeChancePercent`). HUD 예상 표시가 대상별 성공률을 보여 준다
  - 주의: 7차의 `ISrpVictoryCondition`(`HasWon`/`IsDefeated`)을 구현한 코드는 `IsMet`으로 옮겨야 한다. 내장 맵과 저장된 맵 데이터는 그대로 읽힌다
  - 테스트: `SrpM1Phase8Tests`에 17건 추가(모두 32건), `SrpM1Phase7Tests`의 승리 조건 3건을 새 인터페이스로 옮김
  - 시뮬레이션 수치 변화(평균 라운드): `M1OpeningPrototype` H vs R 6.21 → 6.15, R vs H 6.46 → 6.97. `M1QaIntegrated` H vs R 5.35 → 5.47, H vs H 7.18 → 6.06(owner 0 승률 0.337 → 0.000). `M1BarricadeAssault` H vs H 7.08 → 6.93(플레이어 진영 승률 0.800 → 0.770). `M1ObjectiveLab` H vs H 4.00 → 5.08(승률 0.310 → 0.000). `M1ConditionLab` H vs H 8.43(승률 0.765). 임계치는 바꾸지 않았고 모두 통과한다
  - 리뷰 반영(PR #107): 도달 칸으로 가야 하는 보호 대상이 벽에 막히면 제자리에 멈추던 것을 고쳤다. 도달 칸까지의 경로를 따라 돌아가고, 그 경로의 칸이 위험하면 기다린다. 테스트 1건 추가(`SrpM1Phase8Tests` 33건, 전체 236개 통과). 시뮬레이션 수치는 변하지 않았다
  - 검증: Unity 미실행(라이선스 미활성). .NET 로직 테스트 235개 통과, Unity DLL 참조 컴파일 오류 0·경고 0, `scripts/validate-repo.sh` 통과. 맵 메이커의 추가 조건 줄, 로비의 새 버튼 두 개, 전투 화면의 조건 칸 색, HUD의 회피 성공률은 화면으로 확인하지 못했다. 확인 항목은 계획 문서 15절

- 8차: 적 AI 마무리와 지휘관의 위험 판단
  - 적 AI 태세: 활성화의 첫 행동 전에 고른다. 탱커와 교전 중인 유닛은 수비, 교전하지 않았고 사격 위협이 있으면 공격(회피), 그 밖에는 수비
  - 적 AI 방향: 제자리 행동 전과 마지막 AP 이동 전에 측면·후면을 가장 덜 내주는 방향으로 돌아선다
  - 적 AI 오버클럭: 쓰려는 스킬을 강화할 수 있으면 직전에 걸고, 쿨다운·충전으로 못 쓰는 스킬은 되살려 바로 쓸 곳이 있을 때만 건다
  - 적 AI 회복 접근: AP가 2 이상이면 다친 아군에게 회복 스킬이 닿는 칸으로 옮겨 가 회복시킨다
  - 적 AI 지휘관: 지휘관 처치 승리가 켜진 맵에서, 다음 적 차례에 쓰러지거나 붕괴할 수 있는 칸으로는 공격하러 나서지 않는다(`SrpEnemyAi.IsDangerousTile`). 승리 조건 실험실에서 지휘관이 전진했다가 같은 라운드에 쓰러지던 문제의 대응이다
  - AP를 쓰지 않는 행동(태세, 방향, 오버클럭)은 `SrpEnemyAi.DecideStep`이 AP 행동 앞에 끼워 넣는다. 런타임과 시뮬레이션이 `DecideStep`을 쓰고, `Decide`는 AP 행동만 고르는 기존 뜻 그대로다
  - 태세 변경 조건을 `SrpBattleFlow.CanChangeStance`로 옮겨 플레이어 조작과 AI가 같은 규칙을 쓴다(조건은 그대로). AI 행동 수 상한 8 → 12
  - 시뮬레이션 리포트에 판당 태세·방향 변경과 오버클럭 횟수를 추가
  - 테스트: `SrpM1Phase8Tests` 15건 추가
  - 시뮬레이션 수치 변화(평균 라운드): `M1OpeningPrototype` H vs R 6.45 → 6.21, R vs H 6.44 → 6.46. `M1QaIntegrated` H vs R 5.41 → 5.35, H vs H 7.08 → 7.18. `M1BarricadeAssault` H vs H 6.85 → 7.08(플레이어 진영 승률 0.700 → 0.800). `M1ObjectiveLab` H vs H 1.47 → 4.00(승률 0.000/1.000 → 0.310/0.690). 임계치는 바꾸지 않았고 모두 통과한다
  - 기획 질문 Q16~Q18(추가 승리 조건의 종류, 태세 선택 시점, 로비의 적 AI 선택 등)의 선택지와 권장안을 계획 문서 6.2절에 정리했다. 결정은 같은 날 받아 위 항목으로 반영했다
  - 검증: Unity 미실행(라이선스 미활성). .NET 로직 테스트 218개 통과, Unity DLL 참조 컴파일 오류 0·경고 0, `scripts/validate-repo.sh` 통과. 적 AI의 태세·방향 변경과 오버클럭 연출은 화면으로 확인하지 못했다. 확인 항목은 계획 문서 15절

- 7차: 기획 결정 9건 반영(Q4, Q6, Q7, Q10~Q15)
  - Q4: 교전·이탈 비용·기회공격의 인접 기준에 대각선을 포함한다(`SrpBattleState.IsEnemyAdjacentToTile`, `RebuildEngagements`). 근접 공격 범위와 같아져, 대각선에서 공격하고 기회공격 없이 빠지는 일이 없다
  - Q6: 패링 대상은 스킬의 `isParryable`만 본다. 공격자의 `weaponClass` 조건을 뺐다(`SrpCombatResolver.IsParryableThreat`)
  - Q14: 경계사격 동작은 그대로다. 경계태세 미리보기 문구를 "행동하는 적(이동·기본 공격·대상 지정 스킬)"으로 고쳤다
  - Q15: 적 AI 사격수는 쏠 대상이 없고 근접 적이 다가올 때, 쓸모 있는 엄폐가 있는 자리에서 전진 대신 엄폐 + 경계태세로 버틴다(`SrpEnemyAi.ShouldHoldWithOverwatch`)
  - Q13: 판정 순서와 목표 즉시 승리는 유지한다. 승리 조건을 판정기(`ISrpVictoryCondition`)로 나누고, 맵의 `victory.conditions`로 조건을 더 걸 수 있게 했다. 새 조건은 `SrpVictory.Register`로 등록한다. 내장 맵은 추가 조건을 쓰지 않는다
  - Q7, Q10, Q11, Q12: 현행 유지(코드 변경 없음)
  - 시뮬레이션 리포트에 추가 조건으로 끝난 비율(`customConditionRate`)을 추가
  - 테스트: `SrpM1Phase7Tests`에 9건 추가
  - 시뮬레이션 수치 변화(평균 라운드): `M1OpeningPrototype` H vs R 6.15 → 6.45, R vs H 6.13 → 6.44. `M1QaIntegrated` H vs R 5.16 → 5.41, H vs H 7.24 → 7.08. `M1BarricadeAssault` H vs H 6.53 → 6.85(플레이어 진영 승률 0.580 → 0.700). 임계치는 바꾸지 않았고 모두 통과한다
  - 검증: Unity 미실행(라이선스 미활성). .NET 로직 테스트 203개 통과, Unity DLL 참조 컴파일 오류 0·경고 0, `scripts/validate-repo.sh` 통과. 교전 표시, 경계태세 문구, 적 AI 버티기는 화면으로 확인하지 못했다. 확인 항목은 계획 문서 14절

- 7차: 적 AI 확장과 6차 결함 수정
  - 적 AI(`SrpEnemyAi`): 범위 피해, 적에게 붙이는 태그, 반응 약화, HP/PG 회복 스킬을 같은 척도로 견줘 쓴다. AP가 2면 태그를 먼저 붙이고 같은 대상을 공격한다. 활성화를 끝내는 스킬은 공격 뒤로 미룬다
  - 적 AI 엄폐: 사격 뒤나 더 할 행동이 없을 때, 사격해 올 적에게 실제로 효과가 있는 엄폐에만 AP를 쓴다
  - 적 AI 승리 조건(맵이 켠 경우만): 승리 목표 옆으로 가 상호작용하고, 적 지휘관과 목표에 닿으려는 적을 먼저 공격하며, 지휘관은 아군이 남아 있는 동안 앞으로 나서지 않는다
  - `SrpBattleFlow`에 엄폐(`CanTakeCover`, `TakeCover`)와 AI 행동 `TakeCover`/`Interact`를 넣었다. 플레이어의 엄폐도 같은 함수를 쓴다. AI 스킬 행동은 칸을 대상으로 받는다(`SrpSkills.CanTargetTile`)
  - 시뮬레이션 리포트에 판당 엄폐/상호작용 횟수와 종료 방식 비율(전멸/지휘관 처치/목표 확보)을 추가
  - 수정: 맵 메이커가 유닛 DB에 없는 템플릿(프리셋 전용 유닛 등)을 저장에서 빠뜨리던 문제(`SrpMapCarryOver.templates`, `ResolveTemplates`)
  - 수정: `전장 장막`의 오버클럭 강화가 소모만 되고 PG 회복에 적용되지 않던 문제. HP/PG를 회복하는 `BuffStat`에도 강화가 붙는다
  - 테스트: `SrpM1Phase7Tests` 20건 추가
  - 시뮬레이션 수치 변화(평균 라운드): `M1OpeningPrototype` H vs R 6.28 → 6.15, R vs H 6.13 → 6.13. `M1QaIntegrated` H vs R 4.95 → 5.16, H vs H 6.91 → 7.24. `M1BarricadeAssault` H vs H 6.46 → 6.53(플레이어 진영 승률 0.705 → 0.580). 임계치는 바꾸지 않았고 모두 통과한다
  - 남은 기획 질문 9건(Q4, Q6, Q7, Q10~Q15)의 선택지·권장안·측정값을 계획 문서 6.1절에 정리했다. 결정은 같은 날 받아 위 항목으로 반영했다
  - 검증: Unity 미실행(라이선스 미활성). .NET 로직 테스트 194개 통과, Unity DLL 참조 컴파일 오류 0·경고 0, `scripts/validate-repo.sh` 통과. 적 AI의 엄폐·상호작용 연출과 사람 상대 난이도는 화면으로 확인하지 못했다. 확인 항목은 계획 문서 14절

- 6차: 검증 기반·콘텐츠·메이커 마무리
  - 검증: Unity 없이 도는 전투 로직 테스트를 저장소에 넣고 PR CI에 붙였다(`tools/logic-tests`, `scripts/run-logic-tests.sh`, `docs/project/logic-tests.md`). 컨트롤러·화면 코드는 대상이 아니다
  - 데이터: 로컬 `skills.json`/`units.json`에 기본 데이터 버전을 저장하고, 버전이 낮으면 내장 id의 항목을 최신 값으로 갱신한다. 사용자 항목은 유지하고 이전 파일은 백업한다(`SrpDataIO`, `SrpDefaultDataMerge`)
  - 스킬 5종 추가: `투지`, `균열 베기`, `제압 사격`, `전열 재정비`, `충격장`
  - 유닛 `투사` 추가(GDD §10 근접 투사). 기본 유닛 DB의 탱커·사격수에 새 스킬 추가
  - 맵 프리셋 `M1BarricadeAssault`(방벽선 돌파) 추가. 로비 프리셋 목록에 추가
  - 맵 메이커: 엄폐 강도, 상호작용 포인트, 지휘관, 승리 규칙 편집 추가(`SrpMapObjectiveEdit`)
  - 테스트: `SrpM1ContentTests` 14건 추가
  - 주의: 예전 `skills.json`에서 내장 스킬을 직접 고쳐 쓰던 값은 기본값으로 돌아간다(백업 파일 생성)
  - 검증: Unity 미실행(라이선스 미활성). .NET 로직 테스트 174개 통과, Unity DLL 참조 컴파일 오류 0·경고 0, `scripts/validate-repo.sh` 통과. 맵 메이커와 로비의 새 UI는 화면으로 확인하지 못했다. 확인 항목은 계획 문서 13절

## 2026-11-14

- 5차: 스킬·맵 확장
  - 시야(LOS) 모듈 `SrpLineOfSight` 추가: 시야 불필요 / 지형 차단 / 지형·유닛 차단 세 규칙. 스킬 데이터에 `sightRule` 추가, 스킬 메이커에 "시야 규칙" 드롭다운 추가. 기본 스킬은 적 대상 태그 스킬 3종만 지형 차단이다
  - 범위 스킬 런타임 적용: 대상 칸 중심 `areaSize`칸 반경의 정사각형 안에서 지정한 진영에게만 적용. 전투 화면에 범위 칸 표시와 대상별 예상 변화 추가
  - 전투 태그 지속시간(`TBD-007`): `duration` N은 부여한 라운드를 빼고 N라운드 끝까지, 0은 소모 전까지. 유닛 카드에 남은 라운드 표시. 패링 보상 `균형 붕괴`는 지속 1
  - 레거시 `Cleave` 효과가 방어·반응을 건너뛰던 것을 없애고 `Damage`와 같은 규칙을 타게 했다(enum 값은 유지)
  - 맵 메이커: 엄폐물/엄폐 벽 편집 모드 추가. 불러온 맵의 엄폐물·엄폐 segment·허용 스킬 목록이 저장 시 사라지던 것을 수정(`SrpMapCoverEdit`, `SrpMapCarryOver.allowedSkillIds`)
  - 테스트: `SrpM1SkillMapTests` 22건 추가, 패링 테스트에 태그 만료 단언 1건 추가
  - 주의: 로컬 `persistentDataPath/SrpData/skills.json`이 이미 있으면 기본 스킬의 시야 규칙이 반영되지 않는다
  - 검증: Unity 미실행(라이선스 미활성). 저장소 밖 .NET 하네스 159개 중 157개 통과(실패 2개는 컨트롤러 의존으로 이전과 같음), Unity DLL 참조 컴파일 오류 0·경고 0(경로 정정 후의 유효한 확인), `scripts/validate-repo.sh` 통과. 맵 메이커와 스킬 메이커의 새 UI는 화면으로 확인하지 못했다. 확인 항목은 계획 문서 12절

- 긴급 수정: 스킬 예상 창 문구의 컴파일 오류 (PR #101 후속)
  - PR #101 리뷰 반영 커밋에서 `SrpGameController.Hud.cs`의 회피 안내 문자열에 `\n` 대신 실제 줄바꿈이 들어가 문자열 리터럴이 닫히지 않았다(CS1039). 이 상태로 main에 병합돼 Unity에서 컴파일되지 않는다. 이스케이프 문자로 고쳤다
  - 놓친 이유: 4차 작업 내내 "Unity DLL 참조 컴파일 오류 0"으로 보고한 확인이 실제로는 아무 파일도 컴파일하지 않았다. 확인용 프로젝트의 저장소 경로가 잘못 바뀌어 소스 glob이 비어 있었다. `SrpGameController*.cs`는 .NET 하네스에 포함되지 않아 테스트로도 잡히지 않았다
  - 검증: 경로를 바로잡은 뒤 같은 확인에서 수정 전 오류 19개(모두 이 문자열에서 파생), 수정 후 오류 0·경고 0. Unity 에디터는 여전히 미실행

- 스킬 예상 피해의 회피 전제 표시 (PR #101 리뷰 반영)
  - 예상 계산은 회피를 굴리지 않고 실패로 가정하는데, 기본 공격 예상 창만 이를 알리고 스킬 예상 창은 수치만 보여 줬다. 회피 가능한 피해 스킬(비인접, 대상이 공격 태세이고 RP 있음)의 예상 창에도 "대상 회피 시도: 성공률 50%, 성공 시 피해 무효 (위 수치는 회피 실패 기준)"을 표시한다
  - `SrpSkills.CanTargetDodgeSkillDamage` 추가(순수 로직), `SrpCombatResolverTests.CanTargetDodgeSkillDamage_MatchesSkillDodgeCondition` 추가
  - 검증: Unity 미실행. .NET 하네스 137개 중 135개 통과(실패 2개는 컨트롤러 의존), Unity DLL 참조 컴파일 오류 0. 기본 스킬 중 비인접 피해 스킬이 없어 화면 확인은 스킬 메이커로 만든 스킬이 필요하다

- 4차: 방어·피해 수치 확정 (기획 결정 Q5, Q8, Q9 반영)
  - Q5: 수비 태세와 수비 지속 완충의 HP 감쇠를 없애고 PG 감쇠를 1 → 2로 올렸다. Guard 반응(HP-2/PG-2)과 탱커 다중 교전(HP-1/PG-1)은 유지. 쓰이지 않은 상시 PG 감쇠는 총기 파급 PG를 줄인다(`SrpCombatResolver`)
  - Q8: 스킬 피해는 전부 중대 HP 피해로 확정. `강타` HP 피해 기준값 27 → 18(`SrpDefaultSkills`)
  - Q9: 회피를 전투 상태의 시드 난수(성공률 50%)로 판정. `SrpBattleState.SeedRandom`/`NextRandomPercent`/`CloneForPreview`, `SrpGameSettings.BattleRandomSeed` 추가. 되감기로 다시 굴릴 수 없고, 예상 피해 창은 회피 실패 기준 수치와 성공률을 표시한다
  - 수치 조정: 근접 PG 피해 공격력+4 → 공격력, 총기 HP 피해 공격력×2+6 → ×2+4, 총기 HP-PG 파급 50% → 33%
  - 그로기 회복 비율 35%는 유지(20~100% 사이에서 시뮬레이션 결과가 변하지 않음)
  - 전투 로그의 "수비 완충"은 PG 감쇠만 표시한다. `AttackOutcome.reducedHpBySustainedDefense` 제거
  - 테스트: `SrpCombatResolverTests` 8건 추가, 수치에 묶인 기존 기대값 갱신. PlayMode 테스트는 `[SetUp]`에서 전투 난수 시드를 고정
  - 시뮬레이션 수치 변화(케이스당 300판 평균 라운드): `M1OpeningPrototype` H vs R 5.42 → 6.28, R vs H 6.56 → 6.13, H vs H 5.00 → 4.92. `M1QaIntegrated` H vs R 4.61 → 4.94, R vs H 5.75 → 5.43, H vs H 6.00 → 6.91. 오프닝 하한 6을 바꾸지 않고 통과한다
  - 주의: 로컬 `persistentDataPath/SrpData/skills.json`이 이미 있으면 `강타`가 예전 값으로 남는다
  - 검증: Unity 미실행(라이선스 미활성). 저장소 밖 .NET 하네스 136개 중 134개 통과(실패 2개는 컨트롤러 의존으로 이전과 같음), Unity DLL 참조 컴파일 오류 0, `scripts/validate-repo.sh` 통과. 확인 항목은 계획 문서 11절

- 맵 메이커 저장 시 승리 조건 유실 수정 (PR #100 리뷰 반영)
  - 승리 조건을 켠 맵을 맵 메이커에서 불러와 다시 저장하면 `victory`와 배치의 `isCommander`가 기본값으로 돌아갔다. 메이커가 저장할 때 맵 객체를 새로 만들기 때문이다
  - `SrpMapCarryOver`(`SrpMapFile.cs`) 추가: 메이커가 편집하지 않는 `victory`와 상호작용 포인트(승리 목표 효과 포함)를 불러오기에서 저장까지 보존한다. 맵 크기를 벗어난 포인트는 저장하지 않는다. `SrpMapMakerController`는 배치별 `isCommander`도 유지한다
  - 상호작용 포인트는 이전에도 메이커 저장 시 사라졌다. 승리 목표가 포인트에 붙으므로 같이 보존했다. 엄폐물/엄폐 segment/허용 스킬 목록은 여전히 메이커 저장 시 사라진다(5차 메이커 범위)
  - 메이커에서 "그리드 재생성"을 하면 새 맵으로 보고 보존한 값을 비운다. 메이커에서 승리 조건을 편집하는 UI는 없다
  - `SrpM1VictoryTests.MapCarryOver_PreservesVictoryAndInteractionPoints` 추가
  - 검증: Unity 미실행. .NET 하네스 128개 중 125개 통과(실패 3개는 이전과 같음), Unity DLL 참조 컴파일 오류 0. 메이커 화면에서의 불러오기/저장은 확인하지 못했다

- 3차 나머지: 승리 조건, 전투 진행 로직 공유, 플레이 루프 보완
  - 승리 조건(`SrpVictory`, 순수 로직): 전멸 > 지휘관 처치 > 상호작용 목표 순으로 판정. 맵 스키마에 `victory`(`commanderKill`, `interactionObjective`), 배치 `isCommander`, 상호작용 포인트 `effect`(`VictoryObjective`) 추가. 값이 없는 기존 맵과 `M1OpeningPrototype`/`M1QaIntegrated`/`M1EngagementLab`은 전멸 규칙 그대로다
  - 확인용 내장 프리셋 `M1ObjectiveLab` 추가(로비 "승리 조건 검증 랩"). 양 진영 지휘관 1명, owner 0 전용 승리 목표 `신호 장치` 1개
  - 전투 진행 로직(`SrpBattleFlow`, 순수 로직): 라운드 시작, 활성화 시작, 이동/기본 공격/스킬/재장전, 기회공격·경계사격 반응, AI 행동 검증·실행. `SrpGameController`는 규칙 처리를 여기에 맡기고 되감기/로그/연출/활성화 종료만 처리한다(`ISrpBattleFlowObserver`)
  - AI 시뮬레이션(`SrpBattleSimRunner`)이 자체 진행 루프 대신 `SrpBattleFlow`/`SrpTurnOrder`/`SrpVictory`를 호출한다. 새로 반영된 것: 교전 이탈 기회공격, 경계사격, 라운드 자원 초기화(RP·경계태세·수비 피격 누적), 턴 시작 패시브. 리포트에 판당 행동 횟수(`actions`) 추가
  - 적 AI(`SrpEnemyAi`): 총기 역할 유닛은 더 할 행동이 없으면 경계태세를 예약한다
  - 플레이 루프 보완: 스킬 처치로 승패가 나면 즉시 종료, 적 AI 예외 시 적 차례가 멈추지 않게 처리, 전투 종료 후 시스템 탭(되감기/로비) 유지와 종료 안내 문구, 반응 사격으로 끝난 경우 HUD 갱신, HUD의 "공격 후 행동 종료" 안내를 현재 규칙(Q3)에 맞게 수정
  - 테스트: `SrpM1VictoryTests` 9건, `SrpM1BattleFlowTests` 10건 추가. `SrpM1PlayModeTests`의 상태 문구 단언 1곳을 새 안내 문구에 맞춤
  - 시뮬레이션 수치 변화(케이스당 300판 평균 라운드): `M1QaIntegrated` H vs R 5.04 → 4.61, R vs H 6.23 → 5.75, H vs H 6.00 → 6.00. `M1OpeningPrototype` H vs R 5.72 → 5.42, R vs H 7.50 → 6.56, H vs H 5.00 → 5.00. `SrpSimThresholds` 임계치는 모두 통과. 오프닝 하한 6은 변경 전부터 미달이며 하한과 오프닝 수치는 바꾸지 않았다(`SRPG_IMPLEMENTATION_DECISIONS.md`)
  - 검증: Unity 미실행(라이선스 미활성, 배치모드 return code 198). 저장소 밖 .NET 하네스 127개 중 124개 통과(실패 3개는 변경 전과 같은 컨트롤러 의존 2건과 오프닝 하한 1건), Unity DLL 참조 컴파일 오류 0, `scripts/validate-repo.sh` 통과

- 적 AI 장애물 우회 이동 (PR #97 리뷰 반영)
  - `SrpEnemyAi.TryPickMove`: 직선(맨해튼) 거리 점수로 고를 칸이 없거나 고른 칸이 공격 가능한 칸까지의 실제 경로 비용을 늘리면, 최단 경로를 따라 이번 이동력만큼 우회한다. 이동 직후 공격할 수 있는 칸을 고른 경우와 경로로 닿는 공격 칸이 없는 경우는 기존 판단 그대로다
  - `SrpPathfinder.GetReachableWithCosts`에 직전 칸(`cameFrom`)을 기록하는 오버로드 추가. 이동 규칙(ZOC/교전 이탈/점유)은 기존 탐색을 그대로 쓴다
  - `SrpCombatResolverTests`에 적 AI 우회 3건 추가
  - 시뮬레이션 수치 변화: 기본 맵 `M1OpeningPrototype`에서 owner 0 선봉이 (2,4)에 멈춰 있던 경우가 풀려 전투가 짧아졌다. `Opening_Heuristic_vs_Random` 평균 6.02 → 5.72라운드로 `Run_M1OpeningPrototype_Ai_Policy_Matrix_For_BalanceObservation`의 하한 6을 밑돈다. 하한 조정 여부는 미결정(`SRPG_IMPLEMENTATION_DECISIONS.md`)
  - 검증: Unity 미실행. 저장소 밖 .NET 하네스로 `SrpCombatResolverTests` 26건과 `SrpM1AiSimAllEntry` 3건 실행, 위 밸런스 관찰 1건만 실패. `scripts/validate-repo.sh` 통과

- 2차/3차 묶음: 그로기 브릿지, 공격 후 행동, 런타임 적 AI
  - 그로기 유닛은 다음 활성화를 잃고 PG 35%를 회복하며 그다음 자기 활성화까지 재붕괴 보호를 받는다(`SrpTurnOrder.TryConsumeActivationForGroggyRecovery`, `SrpUnitRuntime.IsPgBroken`/`pgBreakGuard`/`ApplyPgDamage`)
  - 그로기 중 가드/회피/패링/기회공격/경계사격 불가(`SrpCombatResolver`, `SrpOverwatch.CanTrigger`)
  - 기본 공격 후 AP가 남으면 계속 행동(`SrpGameController.DoAttack`), 행동 도중 PG가 붕괴하면 활성화 종료
  - `SrpEnemyAi` 추가: owner 0이 아닌 유닛을 적 AI가 자동 조작. `SrpGameController`에 적 차례 코루틴, 입력 차단용 `Phase.EnemyActing`, 인스펙터 필드 `enemyAiStepDelay`, `SrpGameSettings.EnemyAiEnabled` 추가
  - `SrpGameController`의 이동/스킬/재장전 실행부를 `PerformMove`/`PerformSkill`/`PerformReload`로 분리해 플레이어 입력과 AI가 같은 경로를 쓴다
  - AI 시뮬레이션: Heuristic 정책이 `SrpEnemyAi`를 사용, 재장전/피해 스킬/공격 후 행동/그로기/라운드 시작 스킬 자원 틱 반영
  - PlayMode 테스트는 `[SetUp]`에서 적 AI를 끄고 기존 수동 조작 흐름을 유지
  - 되감기 뒤 유닛을 다시 선택해도 이미 쓴 기본 공격과 남은 이동력이 유지된다. 공격 → 다른 행동 → 되감기 → 재공격 우회를 막는다(`SrpGameController.BeginSelectUnit`, 되감기 스냅샷에 남은 이동력 포함)
  - `SrpCombatResolverTests`에 그로기 3건, 적 AI 4건 추가
  - 검증: Unity 미실행. .NET 하네스 105개 중 103개 통과(2개는 컨트롤러 의존), Unity DLL 참조 컴파일 오류 0, `scripts/validate-repo.sh` 통과

- 재개 시점 갭 분석과 진행 계획
  - `SRPG_GAP_ANALYSIS_AND_PLAN_2026-11-14.md` 추가: 기준서 v2/밸런스 피드백 대비 구현 상태, 단계별 계획, 기획 질문 Q1~Q10
  - GDD 미정 수치의 경계태세 표기 정정, 마스터플랜/레거시 코드 분류/밸런스 피드백 프롬프트에 현재 상태 안내 추가
- 1차 개선(구현된 것의 버그/기획 어긋남 수정)
  - `SrpSkills.OnAttackResolved`: 회피/패링으로 무효화된 공격에는 공격 적중 패시브를 발동하지 않음
  - `SrpSkills` 스킬 피해: 처치 직후 교전 상태 재계산, 생존 대상의 피격 패시브 발동
  - `SrpGameController.CanSetSelectedStance`: AP를 쓴 뒤에는 태세 변경 불가(엄폐/상호작용 뒤 변경 가능하던 문제)
  - `SrpBattleSimRunner`: 이동/처치 후 교전 상태 재계산
  - `SrpCombatResolverTests`: `SrpM1All` 카테고리 추가, 테스트 3건 추가
  - 그로기 회복 브릿지는 시험 구현 결과 전투 고착이 확인돼 반영하지 않음(`SRPG_IMPLEMENTATION_DECISIONS.md` 2026-11-14 절)
  - 검증: Unity 미실행(라이선스 미활성). 저장소 밖 .NET 하네스로 순수 로직 EditMode 테스트 실행, `scripts/validate-repo.sh` 통과

## 2026-06-15

- Combat balance review correction
  - Added HP-based PG vulnerability to the shared combat resolver path so both basic attacks and `SrpSkills` damage skills apply the same final incoming PG modifier.
  - Tightened execution threat policy so groggy/PG 0 targets are executed only by adjacent melee threats when battle state is available.
  - Corrected execution resolution from bonus damage to guaranteed kill so adjacent PG-broken/groggy targets cannot survive the execution hit.
  - Updated AI simulation thresholds for the stricter state-based execution path: melee PG share floor `0.45`, first-battle player-policy observation max average rounds `13`, opening heuristic mirror match warn-only.
  - Kept TMP/font fallback changes out of this correction; `Pretendard-Regular SDF.asset` remains untouched.

- Tactical HUD skill selection follow-up (`TBD-017S`)
  - Moved skill selection out of the narrow `ContextPanel` into a dedicated `SkillSelectionDrawer`.
  - The drawer now opens immediately to the right of `CommandRailPanel` as an action detail drawer with preferred width 520px, minimum readable width 420px, and skill row minimum height 56px.
  - Removed the command-adjacent `ContextPanel` column from the left console; state/hover/preview summary now routes to the bottom tactical cards and `InspectorPreviewPanel`.
  - Skill labels use `NoWrap + Ellipsis` so long names do not collapse into one-character columns.
  - Added a visible `닫기` control and command-button toggle close behavior for the skill drawer.
  - Changed the battle log drawer default to collapsed while preserving explicit expand/collapse behavior.
  - Added PlayMode coverage for drawer default-closed state, detached parent, command-rail adjacency, width, row height, close controls, default-collapsed log, and text policy.
  - Added GameView/ScreenCapture observation captures with pre-capture assertions for skill drawer body, secondary drawer body, log expanded, and log collapsed HUD states.
  - Extended the tactical UI research document with XCOM 2 / Into the Breach / Triangle Strategy principles mapped to concrete implementation policies.
- Tactical HUD redesign follow-up (`TBD-017R`)
  - Added `SRPG_TACTICAL_UI_REDESIGN_RESEARCH_2026-06-15.md` and `docs/srpg/ux/tactical_hud_redesign_wireframe_2026-06-15.svg`.
  - Replaced the fixed-open secondary action panel behavior with a default-closed `SecondaryActionPanel` drawer opened from `SecondaryActionTabStripPanel`.
  - Split secondary controls into `태세/방향`, `전술 보조`, and `시스템` pages, with only one drawer page open at once and compact per-page heights.
  - Added PlayMode coverage for drawer default state, minimum 320px open width, per-page compact heights, close-space return, and primary HUD non-overlap.
- Cover semantics/visualization review (`TBD-016R`)
  - Added `SrpCoverObjectData` for occupying cover/obstacle tiles and kept `SrpCoverSegmentData` as directional edge cover.
  - Interpreted `M1OpeningPrototype` central non-walkable tiles as ruin cover objects for this pass.
  - Changed cover rendering so occupying cover objects are central ruin blocks while edge cover segments are low edge walls/boards instead of central cubes.
  - Added EditMode/PlayMode coverage for cover object standing, start overlap, edge walkability, edge visual placement, visual/unit overlap, and move-preview ghost avoidance.

- 전투 HUD 패널 피드백 반영
  - 좌측 전술 콘솔 폭을 명령 전용 레일 수준으로 줄이고, 설명/preview 정보는 하단 현재 유닛 카드와 행동 preview 카드로 분리
  - 스킬 목록을 좌측 콘솔 내부가 아닌 별도 `SkillSelectionDrawerPanel`로 분리하고 `닫기` 버튼과 스킬 버튼 재클릭 닫힘을 추가
  - 로그 패널을 기본 접힘 상태로 시작하게 하고, 접힘 시 작은 재열기 레일만 화면 공간을 차지하도록 조정
  - `SrpgBattle` 씬의 serialized HUD 폭/로그 기본값을 코드 기본값과 동기화
  - PlayMode HUD 스모크 테스트에 좌측 콘솔 폭, 로그 접힘/펼침, 스킬 drawer 분리/닫힘 계약을 추가
- PG 붕괴/그로기/처단 밸런스 후속 구현 프롬프트 추가
  - 기획자 피드백을 `SRPG_BALANCE_FEEDBACK_EXECUTION_PROMPT_2026-06-15.md`로 정리하고 문서 맵에 연결
  - 확정 구현 요구와 기획 확인 필요 항목을 분리해 다음 구현 세션에서 바로 착수 가능하게 정리

## 2026-06-11

- Playtest UX follow-up 3 (`TBD-017`)
  - Split the tactical console into `CommandRailPanel`, bottom tactical cards, `InspectorPreviewPanel`, and `SecondaryActionPanel`; core actions stay fixed in the rail while stance/facing/tools leave the narrow command column.
  - Removed the player-facing floating Tooltip object/path from battle HUD hover behavior. Hover information now routes through bottom tactical cards and `InspectorPreviewPanel`.
  - Reframed the bottom action preview as `InspectorPreviewPanel` and reused it for action hover, skill hover, unit/target hover, and turn-order token hover.
  - Widened the log drawer and made collapse return layout/screen space, leaving only a small reopen rail while preserving log data.
  - Removed the player-facing `턴 종료` HUD button; `행동 종료` is the single exposed activation-ending action, with round changes handled by automatic logs.
  - Changed tactical camera projection toggle to `C` and fixed perspective zoom to use a retained board focus point plus camera distance instead of feeding the camera position back as focus.
  - Expanded PlayMode coverage for rail/context/secondary/inspector/log drawer contracts, no player-facing floating Tooltip object, hover preview continuity, action-end naming, turn-order hover battlefield highlight, and perspective focus-distance zoom.

- Playtest UX follow-up 2 (`TBD-015` correction, `TBD-016` first pass)
  - Replaced move-hover threat tile marker chains with world-space parabolic `LineRenderer` objects.
  - Split basic attack and overwatch threat visuals: subdued/thin basic lines versus stronger overwatch lines with endpoint pulse markers.
  - Added `SrpTacticalCameraController` for perspective/top orthographic view, wheel zoom, middle-drag/WASD/arrow pan, and focus.
  - Added world-space unit facing arrows and one-tile cover cube objects, with a stronger visual tier for `blocksLineOfSight` cover.
  - Updated PlayMode coverage for world-space line existence, tier separation, hover cleanup, tactical camera, facing arrows, and cover object visibility.

- 전투 preview 문법 재정렬 (`TBD-015`)
  - 기본 전투 화면은 현재 행동 유닛의 이동 가능 marker만 유지하고, 일반 공격/경계태세/엄폐/스킬/상호작용 범위는 버튼 hover 때만 열리도록 분리
  - 좌측 전술 콘솔에 `일반 공격` 버튼과 hover preview를 추가
  - 이동 가능 칸 hover 시 ghost 유닛, 목적지 기준 엄폐 marker, 일반 threat line, 경계사격 강화 threat line/endpoint marker 표시
  - 행동 순서 token hover 시 해당 유닛 전장 highlight와 하단 preview/inspector 정보 갱신
  - preview evaluator는 clone을 사용해 원본 battle state를 변경하지 않도록 EditMode 계약 추가
  - 검증 통과: `git diff --check`, `scripts/validate-repo.sh`, Unity EditMode `80 passed / 0 failed` (`TestResults/EditMode-TBD-015-final.xml`), Unity PlayMode `9 passed / 0 failed` (`TestResults/PlayMode-TBD-015-final.xml`)

## 2026-06-10

- 전투 UX 추가 피드백 후속 구현 (`TBD-014`, `BUG-001`, `TBD-012` 후속)
  - 플레이어-facing HUD/버튼/로그/floating text/QA 프리셋 표시명을 `경계태세`/`경계사격` 기준으로 정리
  - 예약 문구는 `경계태세 준비`, 발동 world-space text는 `경계사격!`, 불가/예약 상태는 `경계태세 불가`/`경계태세 준비 중`으로 고정
  - 경계태세 발동으로 대상이 사망하면 선택/hover/aim overlay를 정리하고 유닛 mesh, HUD, 행동 순서 패널을 즉시 갱신
  - 공격/위험 범위와 경계태세 범위는 낮은 밀도 marker로 표시하고, ZOC warning ring/이동 marker/상호작용 marker와 시각 계약을 분리
  - PlayMode에 경계태세 사망 직후 갱신 시나리오와 공격/위험·경계태세 marker 계약 검증 추가
  - 검증: `git diff --check`, `scripts/validate-repo.sh` 통과. marker 후속 보정 뒤 Unity batchmode EditMode `79 passed / 0 failed`, PlayMode `9 passed / 0 failed` 확인 (`TestResults/EditMode-TBD-014-review-fix.xml`, `TestResults/PlayMode-TBD-014-review-fix.xml`).

## 2026-06-09

- 행동 순서 패널 분리 (`TBD-011`)
  - 상단 HUD의 현재 유닛/대기열 정보를 상단 우측의 별도 initiative/turn order icon strip으로 분리: `SrpGameController.Hud`
  - 현재 유닛은 큰 얼굴 토큰과 포인터로 강조하고, 다음 3~5명은 owner/무기 계열을 반영한 런타임 생성 토큰으로 표시
  - 상단 HUD는 라운드/상태/위험영역/맵 요약 중심으로 가볍게 정리
  - PlayMode HUD 테스트에 패널 존재, 로그 패널과의 분리, 현재 아이콘 강조, preview 범위, 턴 종료 후 갱신 검증 추가
  - 검증 통과: `scripts/validate-repo.sh`, EditMode `77 passed / 0 failed`, PlayMode `7 passed / 0 failed`
- 타일 overlay 시각 문법 개편 (`TBD-012`)
  - 이동 가능 범위는 타일 중심 marker, 공격/위험은 외곽 테두리, ZOC/패링은 warning ring, 상호작용은 objective marker로 분리
  - `M1OpeningPrototype` 첫 화면에서 북쪽 사격 루트와 남쪽 `신호 장치` 루트가 같은 색상 채움으로 묻히지 않도록 tile overlay를 별도 marker layer로 전환
  - PR #61의 current/selected/hover 유닛 발밑 ring은 유지하고, tile overlay marker 최대 높이를 current ring보다 낮게 검증
  - 검증 통과: `scripts/validate-repo.sh`, EditMode `77 passed / 0 failed`, PlayMode `7 passed / 0 failed`
- `TBD-010` 총기 발포 방향/조준 문법 브릿지 구현
  - 총기 기본 공격과 오버워치를 공용 목표 벡터 기반 `SrpFirearmAim` LOS helper로 판정
  - 오버워치의 8방향 직선 lane 제한을 제거하고, 사거리/장애물/유닛/`blocksLineOfSight` 차단만 targetability 제한으로 유지
  - `SrpAimSector8`은 `atan2` 기반 표시/디버그/방향성 판정 보조값으로 추가
  - 총기 기본 공격 hover preview에 황색 aim line과 `총기 기본 조준` 문구를 추가하고, 발포 시 총기 유닛 facing을 목표 방향으로 갱신
  - 비8방향 기본 총기 공격/오버워치 허용, LOS 차단, aim line PlayMode smoke 테스트 추가
  - 정식 VFX/애니메이션, 무기별 arc, diagonal facing은 후속 의사결정으로 유지
- 전투 UX 추가 피드백 문서화 (`TBD-014`, `BUG-001`)
  - 사용자 노출 명칭을 `오버워치`에서 `경계태세`로 변경하는 후속 작업을 등록
  - 경계태세 발동 가이드 텍스트 선정, 경계태세 사망 렌더링 지연 수정, 공격/위험 범위 다이아몬드 표시 재검토를 다음 UX 작업으로 정리

## 2026-06-08

- 첫 전투 화면 관찰 P2
  - `M1OpeningPrototype` 관찰용 PlayMode 캡처 테스트 추가: `SrpM1OpeningObservationTests.M1OpeningPrototype_Captures_FirstScreen_RouteObservation`
  - 표본 산출물: `TestResults/SrpPlayObservation/`의 첫 화면, 위험영역/이동 hover, `신호 장치` hover, ring/floating text 캡처와 Markdown 요약
  - 관찰: 첫 대치와 PR #61 ring/floating text는 읽히지만, 북쪽 사격 루트와 남쪽 신호 장치 루트의 차이는 현재 위험영역/상호작용 overlay 문법에 묻힘
  - 판단: AI matrix가 6~10라운드 목표를 통과했으므로 이번 차수에서는 적 수/HP/PG/배치 데이터를 보정하지 않고, `TBD-012` overlay 문법 후속을 우선함
  - 검증 통과: 관찰 PlayMode `1 passed / 0 failed`
- 첫 전투 밸런스 관찰 P2
  - `M1OpeningPrototype` 전용 AI policy matrix 관찰 테스트 추가: `SrpM1AiSimAllEntry.Run_M1OpeningPrototype_Ai_Policy_Matrix_For_BalanceObservation`
  - 핵심 정책 케이스 평균 종료 라운드 확인: Heuristic vs Random `8.31`, Random vs Heuristic `7.65`, Heuristic vs Heuristic `8.00`
  - 6~10라운드 목표 범위에 들어와 이번 차수에서는 적 수, 배치 간격, HP/PG, 속도, 사거리, 탄약, 엄폐/상호작용 배치를 조정하지 않음
  - Random vs Random은 평균 `15.64`라운드/무승부 `0.827`로 관찰용 경향으로만 유지
  - 검증 통과: EditMode `77 passed / 0 failed`, PlayMode `6 passed / 0 failed`

## 2026-06-03

- 첫 전투 프로토타입 프리셋 추가
  - 내장 프리셋 `M1OpeningPrototype` 추가: `M1QaIntegrated`는 기능 QA 맵으로 유지하고, 첫 전투 판단용 맵을 별도로 분리
  - 12x9 소형 전장에 북쪽 사격 루트와 남쪽 돌입/상호작용 루트를 배치
  - 플레이어 4인 파티를 주인공/탱커/사격수/마도사 역할로 재정렬하고, 적은 총기 압박병/근접 돌입병/방어형 적/측면 교란병/전술 장교로 비대칭 구성
  - 방향성 엄폐 segment, `blocksLineOfSight` 사선 차단, 상호작용 포인트 `신호 장치`를 첫 전투 검증 포인트로 배치
  - 기본 전투 진입값과 로비 첫 선택을 `M1OpeningPrototype`으로 교체하고, 기존 `M1QaIntegrated`는 로비 후순위 QA 선택지와 코드/테스트용 deprecated 프리셋으로 유지
  - `SrpgBattle` 씬에 직렬화된 `startPreset`도 `M1OpeningPrototype`으로 갱신하고, 로비 선택이 있을 때만 static 프리셋 전달값을 사용하도록 보정
  - 플레이어가 보는 기본 스킬 설명에서 `브릿지`/`호환용` 표현을 줄이고, 임시 수치성은 구현 의사결정 문서에 유지
  - 검증 통과: EditMode `76 passed / 0 failed`, PlayMode `6 passed / 0 failed`

## 2026-06-02

- 다음 P1 스프린트 구현
  - 초기 4인 고유 패시브 브릿지 추가: 주인공 `전장 적응`, 탱커 `전열 고정`, 사격수 `노출 처벌`, 마도사 `전장 해석`
  - `M1QaIntegrated`를 플레이어 4인 역할 검증 프리셋으로 갱신하고 주인공 전용 패링/탱커 Tank 태그를 데이터에서 분리
  - `blocksLineOfSight` 방향성 엄폐 segment를 오버워치와 총기 기본 공격 사선 차단에 연결
  - 여러 오버워치 후보 우선순위를 가까운 사수, 빠른 사수, 낮은 unit id 순으로 고정
  - 마법 전장 개입 최소 스킬 `전장 장막`(아군 PG +4) 추가
  - 메이커 엄폐 segment 편집 UI, 초기 4인 전직 연계/최종 수치, 특수 지형 복합 효과는 후속 의사결정으로 분리
  - 검증 통과: EditMode `75 passed / 0 failed`, PlayMode `5 passed / 0 failed`

## 2026-05-11

- 문서 동기화
  - `SRPG_PHASE2_CODE_BACKLOG.md`의 전투 플레이 가능성 P1 완료 범위, 검증 수, 다음 착수 후보를 현재 구현 상태와 맞춤
  - `SRPG_README.md`의 3차 작업 상태를 진행에서 완료로 정정
  - 문서 정리만 수행했으므로 Unity 테스트는 재실행하지 않음

## 2026-05-10

- 문서 진입점과 작업 처리 계약 정리
  - SRPG 문서 계층과 현재 실행 플랜을 `docs/srpg/README.md`로 분리
  - 작업 시작, 구현, 문서 갱신, 완료 보고 기준을 `docs/project/work-contract.md`에 추가
  - `docs/README.md`, 루트 `AGENTS.md`, `Assets/Scripts/SRPG/AGENTS.md`, `SRPG_README.md`의 진입 링크와 v1/v2 표기 정리
  - `SRPG_다음미팅_논의사항.md`를 과거 의사결정 메모로 표시
  - 문서 전용 변경이므로 Unity 테스트는 실행하지 않음
- 전투 플레이 가능성 P1 확장
  - 총기 HP-PG 파급을 최종 HP 피해량 기준으로 보정하고 엄폐 GRD 적용 순서를 고정: `SrpCombatResolver`
  - 공용 전투 태그(`표식`, `균형 붕괴`, `사살 지시`) 런타임 계약과 스킬 효과 타입 추가: `SrpUnitRuntime`, `SrpSkillData`, `SrpSkills`
  - 패링 성공 시 공격자 PG 피해와 `균형 붕괴` 부여: `SrpCombatResolver`
  - `완벽한 수비` 1차 구현: `SrpCombatResolver`
  - HUD/로그에 전투 태그, 총기 파급, 패링 보상, 완벽한 수비 결과 표시: `SrpGameController`, `SrpGameController.Hud`
  - 마도사/사격수 태그 대표 스킬과 `M1QaIntegrated` 노출 추가: `SrpDefaultSkills`, `SrpDefaultUnits`, `SrpDefaultMaps`
  - 구현 브릿지와 다음 의사결정 후보 문서 추가: `SRPG_IMPLEMENTATION_DECISIONS.md`
  - Unity EditMode `71 passed / 0 failed`, PlayMode `5 passed / 0 failed`

## 2026-04-27

- 신규 대화 11~22 정책 잠금/문서 정렬
  - `SRPG_NEW_DIALOG_POLICY_LOCK.md` 기준 범위를 06~22로 확장
  - 공통 DEF 제거 방향, GRD(PG 감쇠) 전용화, 중대/경미 HP 피해 분류 정책 추가
  - 탱커 `완벽한 수비`, 패링 성공 보상, 공용 전투 태그, `노출` 용어 정책, 초기 4인 역할 구조를 확정 항목으로 추가
  - `SRPG_전투규칙_기준서_v2`, `SRPG_GDD`, `SRPG_TDD`, `SRPG_PHASE2_CODE_BACKLOG`, `SRPG_BACKLOG`, `SRPG_GDD_TEST_TRACEABILITY`, `SRPG_README`를 새 정책 기준으로 갱신
  - 문서 전용 변경이므로 Unity 테스트는 실행하지 않음
- 23 대화/추가 논의 총기 HP-PG 파급 정책 문서화
  - `SRPG_NEW_DIALOG_POLICY_LOCK.md` 기준 범위를 06~23으로 확장
  - 총격으로 실제 받은 HP 피해량의 50%를 PG 피해로 추가 적용하는 v0.2 기준 추가
  - 50% 비율, 반올림, 최소값, GRD 적용 순서는 밸런스 검사와 전투 시뮬레이션 후 조정 가능하도록 `TBD-009`로 분리
  - 다음 코드 후보에 총기 HP-PG 파급 보정을 추가

## 2026-04-26

- Phase2 전투 코어 1차 코드 착수
  - 교전 상태 저장/클론 안전성 추가: `SrpBattleState`
  - 반응행동 상태 기록 추가: `SrpUnitRuntime`
  - DEF/GRD 상시 감쇠 및 수비 태세 Guard 반응 훅 추가: `SrpCombatResolver`
  - 라운드 AP/RP 리셋 정책 분리: `SrpTurnOrder`
  - 상태 기반 공격 해결, 피격 패시브 훅, 교전 재계산 연결: `SrpGameController`
  - EditMode 테스트 보강 및 통과: `14 passed / 0 failed`
- Phase2 교전 이탈/포지셔닝 패널티 비용 기반 브릿지 구현
  - 교전 중 적 인접 상태를 벗어나는 이동에 임시 추가 비용 적용: `SrpPathfinder`
  - 이동 후 교전 이탈 여부를 로그 힌트로 표시: `SrpGameController`
  - 기회공격 구현 이후에도 이동 선택 단계의 위험 힌트로 비용 브릿지 유지
  - 교전 이탈 비용 고정 입력 테스트 추가 및 EditMode 통과: `15 passed / 0 failed`
- Phase2 교전 이탈 기회공격/반응 이벤트 1차 구현
  - 교전 ID 복사 헬퍼 추가: `SrpBattleState`
  - RP 기반 기회공격 해석 추가: `SrpCombatResolver`
  - 이동 후 교전 이탈 시 `ReactionShot` 기회공격 연결: `SrpGameController`
  - 기회공격 발생/미발생 테스트 추가 및 EditMode 통과: `17 passed / 0 failed`
- Phase2 스킬 자원 기본 모델 구현
  - 쿨다운/충전/오버클럭 메타 및 런타임 상태 추가: `SrpSkillData`
  - 쿨다운/충전 소비·회복과 FH 기반 오버클럭 헬퍼 추가: `SrpSkills`
  - 라운드 스킬 자원 회복 흐름 중앙화: `SrpGameController`
  - 스킬 목록에 충전 상태 간단 표기 추가: `SrpGameController.Hud`
  - 쿨다운/충전/오버클럭 테스트 추가 및 EditMode 통과: `21 passed / 0 failed`
- Phase2 패링 조건/텔레그래프 1차 구현
  - 패링 가능자 태그와 패링 가능 스킬 메타 추가: `SrpUnitTags`, `SrpSkillData`
  - 정면/근접/RP/태그 기반 패링 가능 판정 헬퍼 추가: `SrpCombatResolver`
  - 공격/스킬 타깃에 청록색 패링 가능 오버레이 및 HUD 범례/툴팁 추가: `SrpGameController`, `SrpGameController.Hud`, `SrpGameController.Rendering`
  - 패링 가능/불가 조건 테스트 추가 및 EditMode 통과: `23 passed / 0 failed`
- Phase2 반응행동 파이프라인 1차 구현
  - `AttackOutcome`에 Dodge/Parry 결과 필드 추가 및 반응 선택 순서 확장: `SrpCombatResolver`
  - Parry/Dodge를 RP 소비 후 피해 무효화 브릿지로 연결: `SrpCombatResolver`, `SrpSkills`
  - AP 예약/RP 발동 기반 명시형 `ReactionShot` 오버워치 헬퍼 추가: `SrpOverwatch`
  - 오버워치 예약 상태, 버튼, 범위 오버레이, 발동 로그 연결: `SrpUnitRuntime`, `SrpTurnOrder`, `SrpGameController`, `SrpGameController.Hud`, `SrpGameController.Rendering`
  - Dodge/Parry/명시형 ReactionShot 테스트 추가 및 EditMode 통과: `26 passed / 0 failed`
- Phase2 수비 지속 완충/탱커 다중 대응 브릿지 구현
  - 탱커 전용 태그와 기본 탱커 데이터 연결: `SrpUnitTags`, `SrpDefaultUnits`, `SrpDefaultMaps`
  - 수비 피격 누적 상태 및 라운드 리셋/클론 반영: `SrpUnitRuntime`, `SrpTurnOrder`
  - 수비 태세 후속 피격 완충과 탱커 다중 교전 완충 추가: `SrpCombatResolver`
  - HUD/로그에 탱커 역할, 교전 수, 수비 완충 적용 힌트 추가: `SrpGameController`, `SrpGameController.Hud`, `SrpSkills`
  - 수비 지속 완충/탱커 다중 대응 테스트 추가 및 EditMode 통과: `28 passed / 0 failed`
- Phase2 메이커 메타데이터 UI 확장
  - 스킬 메이커에 충전/오버클럭/패링 메타 입력과 목록 요약 표시 추가: `SrpSkillMakerController`
  - 유닛 메이커에 v2 AP/RP/PG/속도, 무기/태세/방향, ParryUser/Tank 태그 편집 추가: `SrpUnitMakerController`
  - 유닛 메이커 저장 시 legacy AP/PG 필드를 v2 AP/PG와 동기화하는 호환 헬퍼 추가: `SrpUnitMakerController`
  - 스킬/유닛 메타 JSON 보존 및 v2/legacy 동기화 테스트 추가: `SrpMakerMetadataTests`
  - EditMode 테스트 통과: `31 passed / 0 failed`
- Phase2 중간 점검 보정
  - 명시 저장된 `Firearm` 무기 분류를 런타임에서 보존하도록 무기 해석 보정: `SrpBattleState`
  - 스킬 `Damage` 효과가 PG 0 그로기 흐름을 공용 피해 적용 헬퍼로 처리하도록 수정: `SrpCombatResolver`, `SrpSkills`
  - 스킬 메이커의 `ap`/`posture` stat 별칭을 실제 AP/PG 런타임 스탯에 연결: `SrpSkills`
  - 맵 `allowedSkillIds`, 배치 `disabledSkillIds`, 유닛 `maxSkills`를 스폰 시 반영: `SrpBattleState`
  - 회귀 테스트 추가 및 EditMode 통과: `35 passed / 0 failed`
- Phase2 유닛 시각 방향성 개선
  - 전투 유닛 뷰를 원기둥에서 전방 팁이 있는 쐐기형 삼각기둥 메시로 교체: `SrpGameController.Rendering`
  - `SrpFacing`에 따라 유닛 메시가 North/East/South/West 방향으로 회전하도록 연결: `SrpGameController.Rendering`
  - 방향 회전 매핑 테스트 추가 및 EditMode 통과: `36 passed / 0 failed`
- Phase2 교전/둘러싸임 검증 프리셋 보강
  - 내장 프리셋 `M1EngagementLab` 추가: `SrpMapPreset`, `SrpDefaultMaps`
  - 로비 프리셋 선택 버튼에 교전/포위 검증 랩 추가: `SrpLobbyController`
  - 프리셋 기반 다중 교전, 교전 이탈 비용/기회공격, 탱커/수비 완충 테스트 추가: `SrpM1RuleSpecTests`
  - EditMode 테스트 통과: `39 passed / 0 failed`
- Phase2 RP/HUD 노출 정책 정리
  - 전투 HUD에서 RP 원시 수치 대신 반응 준비/소모/예약 상태를 표시: `SrpGameController.Hud`
  - 오버워치 예약 가능/불가 상태 helper 추가: `SrpOverwatch`
  - 기회공격/오버워치/방어 반응 로그를 반응 발동 중심 문구로 정리: `SrpGameController`, `SrpSkills`
  - PlayMode HUD 테스트 기대값을 새 반응 상태 표기에 맞게 보정: `SrpM1PlayModeTests`, `SrpM1AiPlaySampleTests`
  - 오버워치 상태 helper와 라운드 리셋 회귀 테스트 추가 및 검증 통과: EditMode `41 passed / 0 failed`, PlayMode `4 passed / 0 failed`
- Phase2 다음 스프린트 사전 기획 정리
  - HUD/로그 가독성 동기화를 다음 P1 스프린트로 상세화: `SRPG_BACKLOG`, `SRPG_PHASE2_CODE_BACKLOG`
  - HUD 범례/오버레이/반응 상태/오버워치 버튼/로그 문구/PlayMode 테스트 확장을 작업 단위로 분리
  - 오버워치 상세 규칙, 탱커 패시브 최종안, 특수 스킬 피해 파이프라인은 후속 비범위로 정리
- Phase2 기획 대조 P1 보정 완료
  - 기본공격 패링 제거, Dodge 확률형 시도/실패 흐름, 측후면 방어 불리 브릿지를 P1 보정 범위로 구현 및 문서화
  - 턴 시작 태세 선택 UI는 아직 미구현 상태로 명시하고 후속 UX 작업으로 분리
  - 관련 추적 문서 갱신: `SRPG_BACKLOG`, `SRPG_PHASE2_CODE_BACKLOG`, `SRPG_TDD`, `SRPG_GDD_TEST_TRACEABILITY`
  - EditMode 테스트 통과: `43 passed / 0 failed`
- Phase2 HUD/로그 가독성 동기화 완료
  - HUD 범례를 실제 오버레이 의미와 맞춰 단일 문구로 통일: `SrpGameController.Hud`
  - 반응 상태, 오버워치 버튼, 스킬 목록의 쿨다운/충전/패링 가능/오버클럭 용어 정리
  - 공격/기회공격/오버워치/스킬/방어 반응 로그를 이벤트 단위 문구로 정리: `SrpGameController`, `SrpSkills`
  - PlayMode HUD 스모크 테스트를 범례, 반응 상태, 오버워치 버튼, hover 문구, 로그 핵심 문구까지 확장
  - 검증 통과: EditMode `43 passed / 0 failed`, PlayMode `4 passed / 0 failed`
- Phase2 오버워치 사선/횟수/해제 상세 규칙 완료
  - 오버워치 발동을 1차로 8방향 직선 사선으로 제한하고 중간 장애물/유닛 차단을 반영했으나, 이 lane 제한은 `TBD-010`에서 목표 벡터 LOS로 교체: `SrpOverwatch`
  - 오버워치 범위 오버레이가 실제 발동 가능한 사선 타일만 표시하도록 동기화: `SrpGameController.Rendering`
  - 예약 1회당 1회 발동, 발동/라운드 리셋 시 예약 해제 정책을 기준서와 백로그에 반영
  - 사선/차단 회귀 테스트 추가 및 검증 통과: EditMode `45 passed / 0 failed`, PlayMode `4 passed / 0 failed`
- Phase2 테스트 프리셋 v2 + HUD 레이아웃 개편 완료
  - `M1QaIntegrated`를 스킬 자원, 패링 가능 스킬, 오버워치 사선, 탱커 확인용 프리셋으로 갱신
  - HUD를 상단 전투 헤더, 보조 정보 바, 좌측 조작 콘솔, 우측 로그로 분리: `SrpGameController.Hud`
  - 긴 상태 문구와 버튼/스킬 목록 overflow를 줄이고 PlayMode HUD 스모크 기준을 새 레이아웃으로 갱신
  - 프리셋 v2 회귀 테스트 추가 및 검증 통과: EditMode `46 passed / 0 failed`, PlayMode `4 passed / 0 failed`
- Phase2 전투 직접 조작 UI 보강 완료
  - 좌측 전술 콘솔에 태세 선택, 최종 방향 선택, 오버클럭 실행 버튼 추가: `SrpGameController.Hud`
  - 태세/방향 변경을 전투 상태와 유닛 회전에 연결하고 오버클럭을 기존 스킬 자원 모델에 연결: `SrpGameController`, `SrpSkills`
  - 직접 조작 UI PlayMode 테스트와 오버클럭 조건 회귀 테스트 추가
  - 검증 통과: EditMode `47 passed / 0 failed`, PlayMode `5 passed / 0 failed`
- Phase2 오버클럭 성능 증폭 완료
  - 스킬 메타/런타임에 오버클럭 위력 보너스와 다음 사용 1회 강화 상태 추가: `SrpSkillData`
  - 오버클럭 실행 시 다음 액티브 스킬 피해/회복을 증폭하고 사용 후 상태를 소모: `SrpSkills`
  - HUD/스킬 목록/로그와 스킬 메이커 목록에 오버클럭 증폭/강화 대기 상태 표시
  - 검증 통과: EditMode `48 passed / 0 failed`, PlayMode `5 passed / 0 failed`
- Phase2 재장전 AP 행동 1차 구현 완료
  - 총기 유닛 전용 탄약/재장전 계약 추가: `SrpUnitRuntime`, `SrpMapFile`, `SrpBattleState`
  - 기본 공격과 오버워치 예약/발동에 탄약 검사와 소비 연결: `SrpGameController`, `SrpOverwatch`
  - 좌측 전술 콘솔 재장전 버튼, HUD 탄약 상태, 유닛 메이커 최대 탄약 입력 추가
  - 검증 통과: EditMode `51 passed / 0 failed`, PlayMode `5 passed / 0 failed`
- Phase2 엄폐 AP 행동 1차 구현 완료
  - 유닛 런타임 엄폐 상태와 인접 비보행 타일 엄폐 판정 추가: `SrpUnitRuntime`, `SrpBattleState`
  - 총기 기본 공격/오버워치 사격에 엄폐 완충을 연결하고 근접/마법/처단 비적용을 고정: `SrpCombatResolver`
  - 좌측 전술 콘솔 엄폐 버튼, HUD 엄폐 상태, 연두색 엄폐 오버레이 추가: `SrpGameController`, `SrpGameController.Hud`, `SrpGameController.Rendering`
  - 검증 통과: EditMode `54 passed / 0 failed`, PlayMode `5 passed / 0 failed`
- Phase2 상호작용 AP 행동 1차 구현 완료
  - 맵 상호작용 포인트 계약과 런타임 클론/인접 탐색 helper 추가: `SrpMapFile`, `SrpBattleState`
  - 인접 유닛이 AP 1로 포인트를 활성화하고 owner 제한/singleUse 상태를 처리하도록 전투 입력 연결: `SrpGameController`
  - 좌측 전술 콘솔 상호작용 버튼, HUD 상태, 노랑색 상호작용 오버레이 추가: `SrpGameController.Hud`, `SrpGameController.Rendering`
  - `M1QaIntegrated`에 전술 단말 상호작용 포인트를 추가하고 JSON/프리셋/HUD 회귀 테스트 보강
  - 검증 통과: EditMode `59 passed / 0 failed`, PlayMode `5 passed / 0 failed`
- Phase2 개발용 전술 HUD 개선 완료
  - 좌측 하단 현재 유닛 카드와 우측 하단 행동 preview 카드를 코드 생성 uGUI/TMP로 추가: `SrpGameController.Hud`
  - HP/PG/AP/탄약을 숫자와 단색 게이지로 함께 표시하고, hover 중인 이동/공격/스킬/상호작용 예상 정보를 표시
  - 유닛/타일 hover 상태를 preview 카드에 연결하되 실제 전투 상태는 변경하지 않는 읽기 전용 흐름으로 제한: `SrpGameController`
  - PlayMode HUD 회귀 테스트에 하단 카드/preview 카드 검증 추가
  - 검증 통과: EditMode `59 passed / 0 failed`, PlayMode `5 passed / 0 failed`
- Phase2 총기 1발 고화력 + 방향성 엄폐 설계 완료
  - 명시 `maxAmmo`가 없는 총기 기본 탄창을 1발로 낮춤: `SrpUnitRuntime`
  - 총기 기본 공격을 HP 고화력/낮은 PG 압박 브릿지로 조정: `SrpCombatResolver`
  - 기본 탄창 1발, 고화력 HP 피해, 비총기 탄약 예외, HUD `1/1` 표기 회귀 테스트 추가
  - 선형/방향성 엄폐는 `SrpCoverSegmentData` 초안과 edge 단계별 구현 방침으로 문서화
  - 검증 통과: EditMode `61 passed / 0 failed`, PlayMode `5 passed / 0 failed`
- Phase2 방향성 엄폐 1차 구현 완료
  - `SrpCoverSegmentData` 맵 스키마와 런타임 `CoverSegments` 로딩/클론 추가: `SrpMapFile`, `SrpBattleState`
  - 공격자-방어자 방향이 segment edge를 통과할 때만 원거리 총기 엄폐 완충 적용: `SrpCombatResolver`
  - 방향성 엄폐 overlay, HUD 범례, `M1QaIntegrated` QA segment 추가: `SrpGameController.Rendering`, `SrpGameController.Hud`, `SrpDefaultMaps`
  - JSON/프리셋/방향 판정/HUD 회귀 테스트 추가
  - 검증 통과: EditMode `64 passed / 0 failed`, PlayMode `5 passed / 0 failed`

## 2026-04-25

- 신규 대화 원본(06~10) 기반 문서 우선 개편 착수
- 신규 기준 문서 추가
  - `SRPG_전투규칙_기준서_v2.md`
  - `new/SRPG_NEW_DIALOG_POLICY_LOCK.md`
- 핵심 문서 정렬(v2)
  - `SRPG_GDD.md`
  - `SRPG_TDD.md`
  - `SRPG_프로토타입_마스터플랜.md`
- 하위 문서 동기화(v2)
  - `SRPG_README.md`
  - `SRPG_BACKLOG.md`
  - `SRPG_GDD_TEST_TRACEABILITY.md`
  - `SRPG_M1_QA_TEST_RUNNER_CHECKLIST.md`
- 코드 2차 착수 준비 문서 추가
  - `SRPG_PHASE2_CODE_BACKLOG.md`
- 신규 기획 PDF를 재활용 가능한 마크다운 자산으로 변환
  - `new/프로젝트-초기-기획서-초안-외.md`

## 2026-04-18

- 신규 기획서 기준 브랜치 `feature/prototype-v1` 생성
- 체스 코드/문서 폐기
- v1 기준 문서 세트 작성
  - `SRPG_프로토타입_마스터플랜.md`
  - `SRPG_GDD.md`
  - `SRPG_TDD.md`
  - `SRPG_README.md`
  - `SRPG_BACKLOG.md`
  - `SRPG_레거시_코드_분류.md`
  - `SRPG_다음미팅_논의사항.md`
- M0 완료 기준 최종 점검
  - 체스 코드/문서 트리 제거 상태 재확인 (`Assets/Scripts/Chess`, `docs/chess`)
  - README/GDD/TDD/BACKLOG/마스터플랜 교차 정합성 보강
- M1 전투 코어 전환 착수
  - 속도 기반 턴 큐, AP/RP, HP/PG, 무기 분기 구현 시작
- M1 1차 구현/검증
  - `SrpTurnOrder` 도입 및 `SrpGameController` 라운드 큐 흐름 전환
  - `SrpCombatResolver` 총기/근접/마법 분기 + HP/PG 이원화 반영
  - EditMode 테스트 `Assets/Tests/EditMode/Editor/SrpM1CoreTests.cs` 추가
- M1 통합 프리셋 전면 재구성
  - 내장 프리셋을 `M1 QA 통합 검증` 중심 단일 체계로 전환
  - 로비 프리셋 선택 UI 및 QA 체크리스트 문구 동기화
- M1 자동화 테스트 구축
  - 테스트 관측 API 추가 (`TestHudReady`, `TestTurnHudText`, 라운드/유닛 상태 getter)
  - PlayMode 자동 테스트 `Assets/Tests/PlayMode/Editor/SrpM1PlayModeTests.cs` 추가
  - 단일 엔트리 `SrpM1AllTestsEntry` + 카테고리 `SrpM1All` 구성
  - 메뉴 실행 엔트리 `SRPG > Run M1 Automated QA (Edit+Play)` 추가
- AI 스텁 하이브리드 시뮬레이션 구축
  - 정책 인터페이스/기본 정책(랜덤/휴리스틱) 추가
    - `Assets/Tests/Simulation/SrpAiPolicy.cs`
    - `Assets/Tests/Simulation/SrpAiPolicies.Basic.cs`
  - 대량 전투 루프/시드 재현 러너 추가
    - `Assets/Tests/Simulation/SrpBattleSimRunner.cs`
  - 지표/임계치/JSON 리포트 파이프라인 추가
    - `Assets/Tests/Simulation/SrpSimMetrics.cs`
    - `Assets/Tests/Simulation/SrpSimThresholds.cs`
    - `Assets/Tests/Simulation/SrpSimReportWriter.cs`
  - 하이브리드 검증 테스트 및 실행 메뉴 추가
    - `Assets/Tests/EditMode/Editor/SrpM1AiSimAllEntry.cs`
    - `Assets/Tests/PlayMode/Editor/SrpM1AiPlaySampleTests.cs`
    - `Assets/Tests/Editor/SrpAiSimMenu.cs`
  - 문서 가이드 신규 추가: `docs/srpg/SRPG_AI_SIMULATION_GUIDE.md`
- AI 정책 매트릭스 자동 비교 추가
  - EditMode에서 정책 4조합(`HvsR`, `RvsH`, `HvsH`, `RvsR`)을 일괄 실행
  - 케이스별 리포트 + 통합 요약 리포트(`srpg_ai_sim_matrix_*.json`) 생성
  - 비교 엔트리: `SrpM1AiSimAllEntry.Run_M1_Ai_Policy_Matrix_Comparison`
- M1 규칙 단위 테스트 보강
  - 신규 테스트 `Assets/Tests/EditMode/Editor/SrpM1RuleSpecTests.cs` 추가
  - ZOC 이동 비용, 태세(공격/수비), 처단 조건을 고정 입력 assert로 검증
  - `SrpM1AllTestsEntry`에 규칙 테스트 실행 연결
- GDD-테스트 추적 매핑 문서 추가
  - `docs/srpg/SRPG_GDD_TEST_TRACEABILITY.md`
  - GDD 항목별 자동화 커버 상태(완전/부분/미커버)와 후속 액션 명시
- M1 전투 화면 UI 개선 (코드 기반 HUD 유지 리팩터)
  - `UpdateHud()`를 텍스트 빌더 분리 구조로 정리해 가독성 개선
  - 대기 큐 프리뷰 확대 및 유닛 패널(태세/방향/그로기) 가시성 강화
  - 행동 단계 안내 문구 고도화 + `공격 후 턴 종료` 안내 명시
  - 무효 클릭 피드백 로그 추가로 입력 실수 교정성 개선
  - 스킬 목록 항목 재사용/로그 증분 갱신으로 UI 반응성 개선
  - PlayMode 테스트에 태세/방향 표시 assert 추가
- M1 기능 안정화/품질 보강
  - AP 부족 상태에서 스킬 진입/타깃 선택 시 pending 상태를 정리해 입력 잠김을 방지
  - 스킬 버튼 usable 판정에 AP 조건 반영, 이동 성공 시 `hasMovedThisActivation` 갱신
  - `Awake()`에서 HUD 패널 폭 하드코딩 제거(인스펙터 값 우선, 0 이하만 안전 기본값 보정)
  - 테스트 보강: 동일 속도 tie-break(owner/id), 상태 HUD 핵심 문구/상태 일치, ZOC 고정 비용값 assert
  - 테스트 러너 메뉴 보강: EditMode 실패 시 PlayMode 연쇄 실행 차단 가드 추가
- M1 전투 UX 확장 (위험도/범위/intent 가시화)
  - 타일 오버레이를 레이어 재합성 구조로 전환(이동/공격/스킬/위험/의도/hover 분리)
  - HUD에 `위험영역 보기/숨기기` 토글 추가, 적 공격 범위와 ZOC를 상시 시각화 가능
  - 이동 타일 hover 시 위험도/진입불가 상태를 즉시 상태 패널에 표시
  - 유닛 hover 시 해당 유닛의 공격범위/ZOC 미리보기 표시
  - 적 예상 intent(경량 휴리스틱) 경로/타깃 타일 표시 추가
  - PlayMode 테스트에 위험영역 토글/hover 상태 문구 검증 추가
- 전투 UX 피드백 레이어 P1
  - 현재 행동/선택/hover 유닛 ring과 ZOC/교전 unit badge 추가
  - 턴 시작/종료, 공격, 스킬 준비/사용, 재장전, 엄폐, 상호작용, 오버워치 world-space feedback 추가
  - 피해/회복/턴/선택 계열 unit flash 추가
  - PlayMode 테스트에 ring/badge/floating feedback 계약 assert 추가
  - TMP 기본 폰트는 한국어 UI 렌더링을 위해 Pretendard SDF를 유지하고, PlayMode 검증 전 LFS 원본 asset 확보 필요성을 확인
  - 검증: EditMode `76 passed / 0 failed`, PlayMode `6 passed / 0 failed`
- PR #61 전투 UX 피드백 레이어 P1 실플레이 보정
  - 유닛 ring을 타일 표면 위 발아래 decal 기준으로 올리고 mesh winding/반지름/높이 우선순위를 수정
  - world-space feedback text 표시 시간을 늘리고 hold 후 fade, TMP outline/shadow, per-unit stacking을 추가
  - 실플레이 2차 피드백에 맞춰 ring 크기/두께, 색상 채도, world-space feedback text 크기를 낮춤
  - Pretendard SDF 유지 원칙을 재확인하고 LiberationSans 대체를 금지
  - 검증: EditMode `76 passed / 0 failed`, PlayMode `6 passed / 0 failed`
- P2 총기 발포 방향/조준 문법 이슈 문서화
  - 총기 발포 방향이 기본 공격/오버워치/발포 연출에서 모두 8방향 직선 사선처럼 보이는 문제를 `TBD-010`으로 분리
  - 당시 오버워치 8방향 구현과 총기 기본 공격 UI/연출 문법을 같은 계약으로 둘지 P2에서 재검토 대상으로 등록
- P2/P3 UX 후속 항목 문서화
  - 행동 순서 패널 분리를 `TBD-011`로 명시
  - 이동/공격/ZOC/오버워치/패링/상호작용 타일 overlay 시각 문법 개편을 `TBD-012`로 분리
  - 메이커 효과유형 드롭다운 성능/툴팁 개선을 P3 `TBD-013`으로 분리

## 기록 원칙

- 마일스톤(M0~M4) 단위로 업데이트한다.
- 기능 목록보다 의사결정/검증 결과를 중심으로 기록한다.
