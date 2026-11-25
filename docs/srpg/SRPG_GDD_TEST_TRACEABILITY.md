# SRPG GDD-테스트 추적 매핑표 (v2)

## 2026-11-25 update - 10차 이동 규칙 설정 추적

| Item | Tests | Status | Notes |
| --- | --- | --- | --- |
| 이동 규칙 기본값(짧은 이동/긴 이동, 1회, 2배, 긴 이동 뒤 종료) (계획 문서 17절) | `SrpM1Phase10Tests.MoveRule_DefaultsAreRecommendedValues` | 완전 | `SrpGameSettings.MoveRule`, 전투 상태 기본값과 복제, 설정값 범위 |
| 짧은/긴 두 범위 계산 | `SrpM1Phase10Tests.ShortLong_ReachableTilesSplitIntoShortAndLong` | 완전 | 도착 칸의 경로 비용으로 짧은 이동(AP 1)/긴 이동(AP 2)을 가른다 |
| AP 소모와 활성화당 이동 횟수 | `SrpM1Phase10Tests.ShortLong_ShortMoveSpendsOneApAndUsesTheOnlyMove`, `ShortLong_TwoMovesPerActivation` | 완전 | 횟수 2 설정, 새 활성화에서 횟수 초기화 포함 |
| 긴 이동 뒤 종료(설정) | `SrpM1Phase10Tests.ShortLong_LongMoveSpendsTwoApAndEndsActivation`, `ShortLong_LongMoveWithoutForcedEnd` | 부분 | 규칙 결과만. 컨트롤러가 활성화를 끝내는지는 Unity 확인 필요 |
| 공격 뒤 짧은 이동만(Q3와의 관계) | `SrpM1Phase10Tests.ShortLong_AfterAttackOnlyShortMoveIsOffered` | 완전 | |
| AI 행동 실행의 범위 판정 | `SrpM1Phase10Tests.ShortLong_ExecuteAiActionUsesTileRange` | 완전 | |
| 되감기 스냅숏의 이동 횟수 복원 | `SrpM1Phase10Tests.ShortLong_SnapshotRestoresMoveCount` | 부분 | 상태 복제만. 컨트롤러의 되감기 흐름은 Unity 확인 필요 |
| 기존 방식(Legacy) 회귀 | `SrpM1Phase10Tests.Legacy_SplitMovesKeepOldBehaviour`, Legacy로 고정한 기존 AI 테스트 6건 | 완전 | 시뮬레이션 수치도 9차와 같다(계획 문서 17.2절) |
| AI 짧은/긴 이동 선택 | `SrpM1Phase10Tests.Ai_ShortMoveThenAttackWhenTargetIsClose`, `Ai_LongMoveToApproachWhenTargetIsFar`, `SrpM1Phase8Tests.EnemyAi_FacesFromDestinationWhenMoveSpendsLastAp`, `EnemyAi_MovesOntoReachTile` | 완전 | 긴 이동 전 방향, 긴 이동으로 도달 칸 |
| 시뮬레이션 이동 후보와 규칙 지정 | `SrpM1Phase10Tests.Simulation_MoveOptionsFollowMoveRule` | 완전 | `SrpBattleSimConfig.moveRule` |
| 이동 칸 두 색, HUD 글자, 로비 이동 버튼 | 없음 | 미확인 | 화면 코드라 Unity 확인 필요 |

## 2026-11-16 update - 9차 되감기 횟수 설정 추적

| Item | Tests | Status | Notes |
| --- | --- | --- | --- |
| 되감기 횟수 한도 기본값 무제한 (계획 문서 16절) | `SrpM1Phase9Tests.Rewind_DefaultIsUnlimited` | 완전 | `SrpGameSettings.RewindLimit` 기본값과 무제한 소모 |
| 한도 0(끔)과 한도 N 소진 | `SrpM1Phase9Tests.Rewind_ZeroLimitDisablesRewind`, `Rewind_LimitedBudgetRunsOut` | 완전 | 소진 뒤 소모 시도는 상태를 바꾸지 않는다 |
| 판 시작 초기화 | `SrpM1Phase9Tests.Rewind_ResetRefillsForNewBattle` | 부분 | 순수 로직만. 컨트롤러가 `Awake`/`ApplyMap`에서 부르는지와 되감기 복원 뒤 횟수가 돌아가지 않는지는 Unity 확인 필요 |
| 핫시트 되감기 공유(기본)/진영별 | `SrpM1Phase9Tests.Rewind_SharedByDefaultAndPerOwnerWhenEnabled` | 부분 | 횟수 계산만. 스냅숏의 진영 기록과 "자기 진영 행동만 되감기"는 컨트롤러 몫이라 Unity 확인 필요 |
| 진영별 되감기: 상대 활성화 시작 시 기록 끊기 | `SrpM1Phase9Tests.Rewind_PerOwnerCutsHistoryWhenOpponentActivationStarts` | 부분 | 판단만. 컨트롤러의 `AdvanceToNextActivation`이 부르는지는 Unity 확인 필요 |
| 로비 되감기 버튼 순환과 문구 | `SrpM1Phase9Tests.Rewind_LobbyCycleOrder` | 부분 | 값 순환과 문구만. 로비·HUD 화면은 미확인 |

## 2026-11-15 update - 8차 기획 결정 추적

| Item | Tests | Status | Notes |
| --- | --- | --- | --- |
| 승리 조건 목록과 패배 조건 목록, 판정 순서 (기준서 §12, 계획 문서 Q16) | `SrpM1Phase8Tests.Victory_DefeatListRanksBeforeVictoryListAndSurvivesJson`, `SrpM1Phase7Tests.Victory_MapCanAddRegisteredCondition`, `Victory_AddedConditionRanksAfterBuiltInsAndCanDefeat`, `Victory_UnknownConditionIsSkippedButPreserved` | 완전 | 전멸 > 패배 조건 > 승리 조건. 저장 형식 왕복 포함 |
| N라운드 버티기와 제한 라운드 (`survive_rounds`) | `SrpM1Phase8Tests.Victory_SurviveRoundsDecidesWhenRoundEnds` | 완전 | 라운드가 끝나는 순간 성립(`SrpBattleFlow.EndRound`). 컨트롤러의 라운드 종료 판정은 PlayMode 미커버 |
| 칸 도달·탈출 (`reach_tile`) | `SrpM1Phase8Tests.Victory_ReachTileNeedsMatchingUnitOnTile` | 완전 | 대상 유닛 지정(모든 유닛/지휘관/템플릿) 포함 |
| 유닛 보호 (`unit_lost`) | `SrpM1Phase8Tests.Victory_UnitLostInDefeatListDefeatsOwner` | 완전 | 대상이 여럿이면 모두 잃어야 성립 |
| 거점 N라운드 유지 (`hold_point`) | `SrpM1Phase8Tests.Victory_HoldPointCountsConsecutiveRoundEnds` | 완전 | 연속 유지, 복제(되감기) 시 진행도 유지 포함 |
| 추가 조건의 적 AI | `SrpM1Phase8Tests.EnemyAi_MovesOntoReachTile`, `EnemyAi_ProtectedRunnerDetoursAroundWallWhenSafe`, `EnemyAi_AvoidsTileThatDefeatsItsOwner`, `EnemyAi_TreatsProtectedUnitLikeCommander`, `EnemyAi_EntersAndKeepsHoldZone`, `EnemyAi_TimePressureChangesHoldAndAdvance` | 부분 | 내장 네 조건만. 가중치는 판단용 임시값. 사람 상대 체감은 Unity 확인 필요 |
| 추가 조건 검증 랩 (`M1ConditionLab`) | `SrpM1Phase8Tests.ConditionLab_DeclaresAllFourConditions`, `ConditionLab_SimulationEndsByListedConditions`, `SrpM1ContentTests.Presets_AreInternallyConsistent` | 부분 | AI끼리는 전령 도달로 끝나지 않는다(0%). 화면 표시는 미확인 |
| 맵 메이커의 추가 조건 편집 규칙 | `SrpM1Phase8Tests.MakerCondition_BuildKeepsOnlyUsedFieldsAndRejectsInvalid`, `MakerCondition_AddedConditionsWorkInBattleAfterSave` | 부분 | 순수 로직(`SrpMapConditionEdit`)과 저장 → 전투 판정까지 고정. 메이커 화면은 미확인 |
| 권총/소총 구분 (밸런스 지시 4, 계획 문서 Q18) | `SrpM1Phase8Tests.Firearm_PistolDealsLessThanRifleAndTypeCanBeSetInData` | 완전 | 권총 HP = 공격력 + 4(브릿지). 유닛 메이커 편집 항목은 없다 |
| 회피의 방향·속도 가중치 (`TBD-003`, 계획 문서 Q18) | `SrpM1Phase8Tests.Dodge_ChanceDependsOnDirectionAndSpeed`, `Dodge_ResolutionUsesPerTargetChance` | 부분 | 수치는 브릿지. HUD의 대상별 성공률 표시는 미확인 |
| 로비의 적 AI 켜기/끄기 (계획 문서 Q18) | 없음 | 미커버 | 로비 버튼이 `SrpGameSettings.EnemyAiEnabled`를 바꾼다. 화면 확인 항목은 계획 문서 15절 |
| 태세 선택 시점의 해석 (기준서 §4, 계획 문서 Q17) | `SrpM1Phase8Tests.CanChangeStance_OnlyBeforeFirstAction` | 부분 | 코드 변경 없음(기준서 문구만 추가) |

## 2026-11-15 update - 8차 추적

| Item | Tests | Status | Notes |
| --- | --- | --- | --- |
| 적 AI의 태세 선택과 태세 변경 시점 (`RQ-006`, 기준서 §4) | `SrpM1Phase8Tests.EnemyAi_PicksAggressiveStanceAgainstRangedThreatBeforeActing`, `EnemyAi_PicksDefensiveStanceWhenEngagedTankOrUnthreatened`, `CanChangeStance_OnlyBeforeFirstAction` | 부분 | 조건(`SrpBattleFlow.CanChangeStance`)은 플레이어 조작과 공유한다. 컨트롤러의 태세 버튼 잠금은 PlayMode로만 확인 가능 |
| 적 AI의 방향 선택 (`RQ-007`, 기준서 §5) | `SrpM1Phase8Tests.EnemyAi_TurnsToFaceThreatBeforeActingInPlace`, `EnemyAi_KeepsFacingOnTieAndAvoidsExposingBack`, `EnemyAi_FacesFromDestinationWhenMoveSpendsLastAp`, `EnemyAi_DoesNotPreTurnBeforeFirearmAttack` | 부분 | 정면/후면 판정은 피해 규칙과 같은 함수를 쓴다. 가중치는 판단용 임시값 |
| 적 AI의 오버클럭 사용 (`RQ-012`, 기준서 §8) | `SrpM1Phase8Tests.EnemyAi_OverclocksSkillForPowerBeforeUsingIt`, `EnemyAi_OverclocksToReviveSkillOnCooldownOnlyWhenItBecomesUsable` | 부분 | 판단 중 상태를 바꾸지 않는 것까지 고정. 런타임 연출은 PlayMode 미커버 |
| 적 AI가 회복하러 다가가는 이동 (`RQ-021`) | `SrpM1Phase8Tests.EnemyAi_MovesIntoRangeToHealBadlyWoundedAlly`, `EnemyAi_HealApproachYieldsToAttackAndNeedsSpareAp` | 부분 | 이동 뒤 같은 차례에 회복할 수 있을 때만. 가산은 판단용 임시값 |
| 적 AI 지휘관의 위험 판단 (기준서 §12, §13) | `SrpM1Phase8Tests.EnemyAi_CommanderDoesNotAdvanceIntoLethalTile`, `EnemyAi_CommanderDangerCheckOnlyAppliesWhileHolding`, `ObjectiveLab_SimulationDoesNotEndWithCommanderRush` | 부분 | 기준 피해 합으로 어림한다. 물러나는 판단은 없다 |
| AI 시뮬레이션이 태세·방향 변경과 오버클럭을 사용 | `SrpM1Phase8Tests.Simulation_UsesStanceFacingAndOverclock`, `SrpM1AiSimAllEntry` 3종 | 부분 | 리포트의 `actions.stanceChangesPerBattle`/`facingChangesPerBattle`/`overclocksPerBattle`로 확인. 임계치는 7차와 같다 |

## 2026-11-15 update - 7차 기획 결정 추적

| Item | Tests | Status | Notes |
| --- | --- | --- | --- |
| 교전 인접은 대각선 포함 (`RQ-010`, 기준서 §5, 계획 문서 Q4) | `SrpM1Phase7Tests.Engagement_IncludesDiagonalAdjacency`, `Move_AwayFromDiagonalEnemyCostsExitAndTriggersOpportunityAttack`, `Move_AroundSameEnemyKeepsEngagement` | 부분 | 교전 표시와 이동 미리보기는 화면 확인이 필요하다 |
| 패링 대상은 스킬의 패링 가능 표시만으로 판정 (`RQ-008`, 기준서 §6, 계획 문서 Q6) | `SrpM1Phase7Tests.Parry_DependsOnSkillFlagNotWeaponClass` | 완전 | 정면, 인접, 패링 가능자, RP 조건은 `SrpM1RuleSpecTests.ParryCondition_*`가 고정 |
| 적 AI 사격수의 엄폐 + 경계태세 버티기 (기준서 §3.5, 계획 문서 Q15) | `SrpM1Phase7Tests.EnemyAi_ShooterHoldsCoverWithOverwatchInsteadOfAdvancing`, `EnemyAi_ShooterAdvancesWhenHoldingHasNoPayoff` | 부분 | 사람 상대 체감은 Unity 확인이 필요하다 |
| 맵마다 다른 승리 조건의 추가 (기준서 §12, 계획 문서 Q13) | `SrpM1Phase7Tests.Victory_MapCanAddRegisteredCondition`, `Victory_AddedConditionRanksAfterBuiltInsAndCanDefeat`, `Victory_UnknownConditionIsSkippedButPreserved` | 부분 | 구조만 고정. 실제 추가 조건은 아직 없고 적 AI도 추가 조건을 모른다 |
| 경계사격 발동 범위 안내 문구 (기준서 §3.5, 계획 문서 Q14) | 없음 | 미커버 | HUD 문구만 바꿨다. 화면 확인 항목은 계획 문서 14절 |

## 2026-11-15 update - 7차 추적

| Item | Tests | Status | Notes |
| --- | --- | --- | --- |
| 적 AI의 범위·태그·회복 스킬 사용 (`RQ-019`, `RQ-021`) | `SrpM1Phase7Tests.EnemyAi_AreaSkillTargetsTileCoveringMostEnemies`, `EnemyAi_TagSkillThenAttackConsumesTag`, `EnemyAi_TagSkillIsNotWastedWhenAttackIsWorthMore`, `EnemyAi_EndingSkillIsUsedAfterAttack`, `EnemyAi_HealsBadlyWoundedAllyBeforeMoving`, `EnemyAi_UsesAreaPgRecoveryOnlyWhenItRestoresPg` | 부분 | 판단과 실행(`SrpBattleFlow.ExecuteAiAction`)은 고정. 가중치는 판단용 임시값. 런타임 연출은 PlayMode 미커버 |
| 적 AI의 엄폐 사용 (`RQ-004`, 기준서 §3.1) | `SrpM1Phase7Tests.EnemyAi_ShooterTakesCoverAfterFiring`, `EnemyAi_DoesNotTakeCoverWithoutRangedThreat`, `EnemyAi_IdleMeleeUnitTakesCover` | 부분 | 엄폐 유지 시점은 계획 문서 Q7. 컨트롤러의 엄폐 후처리(`AfterTakeCover`)는 PlayMode로만 확인 가능 |
| 적 AI의 승리 조건 공략과 방어 (기준서 §12, §13) | `SrpM1Phase7Tests.EnemyAi_MovesToObjectiveAndSecuresIt`, `EnemyAi_IgnoresObjectiveItCannotWinWith`, `EnemyAi_PrioritizesEnemyThreateningObjective`, `EnemyAi_PrioritizesEnemyCommander`, `EnemyAi_CommanderHoldsPositionWhileAlliesRemain` | 부분 | 맵이 켠 조건만 본다. 목표 옆 칸 점거, 지휘관 후퇴 판단은 없다 |
| AI 행동 실행 규칙: 스킬 활성화당 1회, 대상 칸 검증, 엄폐·상호작용 조건 (`RQ-002`) | `SrpM1Phase7Tests.ExecuteAiAction_RejectsInvalidSkillCoverAndInteraction` | 완전 | 실행할 수 없는 행동은 상태를 바꾸지 않는다 |
| AI 시뮬레이션이 엄폐·승리 목표·지휘관 처치를 사용 | `SrpM1Phase7Tests.Simulation_UsesCoverObjectiveAndCommanderKill`, `SrpM1ContentTests.BarricadeAssault_SimulationFinishesDecisively`, `SrpM1AiSimAllEntry` 3종 | 부분 | 리포트의 `actions.coversPerBattle`/`interactionsPerBattle`, `outcome`의 종료 방식 비율로 확인. 임계치는 6차와 같다 |
| 맵 메이커 저장 시 유닛 DB에 없는 템플릿 유지 | `SrpM1Phase7Tests.MakerSave_KeepsTemplatesMissingFromUnitDb`, `MakerSave_ResolveTemplatesUsesOnlyPlacedIds` | 부분 | 순수 로직(`SrpMapCarryOver.ResolveTemplates`)은 고정. 메이커 화면에서의 불러오기/저장은 미확인 |
| 오버클럭 강화가 HP/PG 회복 강화 효과에 적용 (`RQ-012`) | `SrpM1Phase7Tests.ArcaneScreen_OverclockPowerBonusAppliesToPgRecovery`, `OverclockPowerBonus_DoesNotChangeNonRecoveryStatEffects` | 완전 | `전장 장막` PG 4 → 6. 회복이 아닌 능력치 증감에는 붙지 않는다 |

## 2026-11-14 update - 1차 개선 추적

| Item | Tests | Status | Notes |
| --- | --- | --- | --- |
| 공격 적중 패시브는 회피/패링된 공격에 발동하지 않음 (`RQ-015`) | `SrpCombatResolverTests.OnAttackResolved_DodgedAttackDoesNotTriggerOnHitPassive` | 완전 | `전장 적응`/`노출 처벌`의 "공격 적중 시" 조건 |
| 스킬 처치 직후 교전 상태 갱신 (`RQ-010`) | `SrpCombatResolverTests.ResolveActiveSkill_KillRebuildsEngagements` | 완전 | 수비 지속 완충/탱커 다중 교전/이탈 비용이 사망 유닛을 세지 않음 |
| 스킬 피해의 피격 패시브 발동 (`RQ-015`) | `SrpCombatResolverTests.ResolveActiveSkill_DamageTriggersTargetOnTakeDamagePassive` | 완전 | `전열 고정` PG +2 |
| AP 사용 후 태세 변경 불가 (`RQ-005`) | 없음 | 미커버 | `SrpGameController.CanSetSelectedStance`. PlayMode에 엄폐/상호작용 후 태세 변경 거부 assert 추가 필요 |
| 그로기 1턴 무방비/회복/재붕괴 보호 (밸런스 피드백 2026-06-15 요구 2) | `SrpCombatResolverTests.GroggyRecovery_ConsumesActivationAndGrantsBreakGuard`, `SrpCombatResolverTests.BreakGuard_KeepsPgAtOneUntilNextActivation`, `SrpCombatResolverTests.GroggyUnit_CannotReact` | 부분 | 순수 로직은 고정. 컨트롤러의 활성화 스킵/행동 중단과 HUD 표시는 PlayMode 미커버 |
| 적 AI 판단 (`SrpEnemyAi`) | `SrpCombatResolverTests.EnemyAi_PrefersExecutionOfAdjacentBrokenTarget`, `SrpCombatResolverTests.EnemyAi_ShooterReloadsThenFires`, `SrpCombatResolverTests.EnemyAi_MeleeMovesIntoAttackRangeWithoutMutatingState`, `SrpCombatResolverTests.EnemyAi_UsesDamageSkillWhenSingleActionLeft`, `SrpCombatResolverTests.EnemyAi_DetoursAroundBarrierWhenEveryReachableTileIsFartherAway`, `SrpCombatResolverTests.EnemyAi_DoesNotMoveWhenNoPathLeadsToAnyTarget`, `SrpCombatResolverTests.EnemyAi_OpenFieldMoveChoiceIsUnchanged`, `SrpM1AiSimAllEntry` 3종 | 부분 | 판단 로직과 시뮬레이션 임계치는 고정. 장애물 우회 이동 반영 뒤 `Run_M1OpeningPrototype_Ai_Policy_Matrix_For_BalanceObservation`의 평균 라운드 하한(6)은 .NET 하네스에서 5.72로 미달(`SRPG_IMPLEMENTATION_DECISIONS.md` 2026-11-14 적 AI 우회 이동 절). 런타임 적 차례 자동 진행(코루틴, 입력 차단, 되감기 비움)은 PlayMode 미커버 |
| 기본 공격 후 AP가 남으면 계속 행동 (`RQ-002`) | 없음 | 미커버 | `SrpGameController.DoAttack`. PlayMode에 공격 후 이동 가능/두 번째 공격 불가 assert 추가 필요 |
| 승리 조건: 전멸/지휘관 처치/상호작용 목표 (기준서 §12, §13, `TBD-004`) | `SrpM1VictoryTests.ExistingPresets_KeepAnnihilationOnly`, `Annihilation_DecidesWinnerOrDraw`, `CommanderKill_DefeatsOwnerWhenRuleEnabled`, `CommanderKill_IgnoredWhenRuleOffOrNoCommander`, `CommanderKill_BothLost_IsDraw`, `InteractionObjective_ActivationWinsBattle`, `InteractionObjective_RequiresAllPointsAndEnabledRule`, `Clone_PreservesVictoryData`, `MissingVictoryData_FallsBackToAnnihilation`, `MapCarryOver_PreservesVictoryAndInteractionPoints` | 부분 | 판정 로직(`SrpVictory`)과 데이터 계약은 고정. 런타임 연결(`SrpGameController.CheckWin`, 종료 HUD, 상호작용 직후 종료)은 PlayMode 미커버. 첫 전투 맵 도입 여부는 계획 문서 Q11 |
| 전투 진행 로직 공유: 이동/기본 공격/스킬/재장전과 기회공격·경계사격 (`RQ-002`, `RQ-003`, `RQ-010`) | `SrpM1BattleFlowTests.Move_DisengageTriggersOpportunityAttack`, `Move_IntoArmedOverwatchTriggersReactionShot`, `Move_LethalOverwatchEndsActivation`, `BasicAttack_SpendsResourcesAndRejectsEmptyGun`, `ExecuteAiAction_RejectsInvalidActionsWithoutSideEffects`, `StartRound_ResetsRoundResources`, `UseSkill_KillingLastEnemyDecidesBattleImmediately` | 부분 | `SrpBattleFlow` 규칙 처리는 고정. 컨트롤러 쪽 로그/연출/되감기/활성화 종료 연결은 PlayMode로만 확인 가능 |
| 적 AI 경계태세 (`RQ-003`) | `SrpM1BattleFlowTests.EnemyAi_ShooterArmsOverwatchWhenNothingElseToDo` | 완전 | 총기 역할만, 더 할 행동이 없을 때만 예약. 근접/마법 역할은 예약하지 않음(계획 문서 Q15) |
| AI 시뮬레이션이 런타임과 같은 진행·판정을 사용 | `SrpM1BattleFlowTests.Simulation_UsesReloadSkillOverwatchAndOpportunityAttack`, `Simulation_UsesSharedVictoryRule`, `SrpM1AiSimAllEntry` 3종 | 부분 | 재장전/스킬/경계태세/경계사격/기회공격 사용을 리포트 `actions` 수치로 확인. `Run_M1OpeningPrototype_Ai_Policy_Matrix_For_BalanceObservation`의 평균 라운드 하한(6)은 5.42로 미달 상태 유지(하한 유지 결정, 4차에서 재검토) |

`SrpCombatResolverTests`는 이번부터 `SrpM1All` 카테고리에 포함된다.

## 2026-06-15 update - HUD drawer and cover semantics traceability

| Item | Tests | Status | Notes |
| --- | --- | --- | --- |
| Basic attack / execution model correction | `SrpCombatResolverTests.ApplyAttack_FirearmRoleExecutesAdjacentPgBrokenTarget`, `SrpCombatResolverTests.ApplyAttack_FirearmRoleAdjacentAttackDoesNotUseFirearmSpillover`, `SrpCombatResolverTests.CanAttack_AdjacentMeleeIgnoresAmmoButNonAdjacentFirearmRequiresAmmo`, `SrpCombatResolverTests.ApplyAttack_NonAdjacentFirearmConsumesAmmoAndAppliesSpillover`, `SrpCombatResolverTests.SimulationMetrics_RecordActualResolvedAttackKind`, `SrpM1PlayModeTests.BasicAttackHoverAndExecution_ResolveByDistance` | Partial | Locks distance-based basic attack kind, adjacent Firearm-role guaranteed execution kill, adjacent no-ammo/no-aim/no-spillover behavior, non-adjacent firearm ammo/LOS/spillover behavior, and simulation metric attribution. Explicit sidearm/firearm-range data fields remain a follow-up decision. |
| Overwatch firearm capability correction | `SrpM1RuleSpecTests.Overwatch_MeleeRoleSidearm_BlocksAdjacentAndTriggersFirearmAtRange`, `SrpM1RuleSpecTests.Overwatch_ArmStatus_ExplainsHudPolicyConditions`, `SrpM1CoreTests.Overwatch_RequiresAndConsumesSidearmAmmo`, `SrpM1PlayModeTests.DangerAreaAndHoverPreview_UpdatesStatusText` | Partial | Locks that overwatch/경계태세 is a firearm reaction gated by sidearm ammo capacity, ammo, range, AP/RP, and LOS rather than `weaponClass == Firearm`; adjacent targets remain blocked. |
| Combat balance review correction | `SrpCombatResolverTests.ApplyAttack_LowHpIncreasesFinalIncomingPgDamage`, `SrpCombatResolverTests.ApplySkillReaction_LowHpVulnerabilityAppliesToSkillPgDamage`, `SrpCombatResolverTests.ApplySkillReaction_LowHpVulnerabilityAppliesAfterTagPressure`, `SrpCombatResolverTests.ApplyAttack_NonAdjacentMeleeDoesNotExecuteGroggyTarget`, `SrpM1RuleSpecTests.Execution_Triggers_WhenDefenderPgZeroOrGroggy` | Partial | Locks shared basic/skill PG vulnerability and adjacent melee execution policy. Full balance feel remains simulation/playtest work. |
| `TBD-017S` skill selection readability | `SrpM1PlayModeTests.DefaultOpeningPrototypePreset_InitializesRoundAndHud`, `SrpM1OpeningObservationTests.M1OpeningPrototype_Captures_FirstScreen_RouteObservation` | Partial | Verifies `SkillSelectionDrawer` is not parented under command rail, is closed by default, opens immediately to the right of `CommandRailPanel`, uses width >= 420px, rows >= 56px, `NoWrap + Ellipsis`, exposes a `닫기` close control, closes when the `스킬` command is pressed again, and leaves no command-adjacent `ContextPanel` column. Final visual feel remains editor-play QA. |
| `TBD-017R` secondary HUD drawer | `SrpM1PlayModeTests.DefaultOpeningPrototypePreset_InitializesRoundAndHud`, `SrpM1OpeningObservationTests.M1OpeningPrototype_Captures_FirstScreen_RouteObservation` | Partial | Verifies default-closed `SecondaryActionPanel`, visible tab strip, drawer width >= 320px, one active tab page, compact heights for `태세/방향`/`전술 보조`/`시스템`, visible drawer body before capture, close-space return, and primary HUD non-overlap. Actual editor-play feel remains manual QA. |
| `TBD-016R` occupying cover object vs edge segment | `SrpM1CoreTests.CoverObject_LoadsAsBlockingOccupyingCover_AndClonesIndependently`, `SrpM1CoreTests.M1OpeningPrototype_CoverObjects_DoNotOverlapStarts_AndBlockStanding`, `SrpM1CoreTests.M1OpeningPrototype_EdgeCoverSegments_DoNotBlockStandingOnTheirTile`, `SrpMakerMetadataTests.MapMetadata_JsonRoundTrip_PreservesCoverObjects`, `SrpM1PlayModeTests.DefaultOpeningPrototypePreset_InitializesRoundAndHud`, `SrpM1PlayModeTests.DangerAreaAndHoverPreview_UpdatesStatusText` | Partial | Verifies map data split, start-position overlap guard, non-standable cover objects, edge segment walkability, edge-wall visual contract, occupying cover visual/unit separation, and move ghost avoidance. |

## 2026-06-11 update - UX traceability

| Item | Tests | Status | Notes |
| --- | --- | --- | --- |
| `TBD-015` move-hover threat line correction | `SrpM1PlayModeTests.DangerAreaAndHoverPreview_UpdatesStatusText` | Partial | Verifies world-space threat line objects, no tile threat marker chain, basic/overwatch tier split when both are present, overwatch endpoint marker, and hover-exit cleanup. |
| `TBD-016` tactical camera/facing/cover visualization | `SrpM1PlayModeTests.DefaultOpeningPrototypePreset_InitializesRoundAndHud` | Partial | Verifies tactical camera component, `C` mode toggle, pan/zoom/focus drift guard, perspective focus-distance zoom, pan-then-zoom focus stability, facing arrow objects, cover cube objects, and distinct `blocksLineOfSight` cover visual tier. Input feel and art polish remain editor-play QA items. |
| `TBD-017` tactical console/log/action-end cleanup | `SrpM1PlayModeTests.DefaultOpeningPrototypePreset_InitializesRoundAndHud`, `SrpM1PlayModeTests.DangerAreaAndHoverPreview_UpdatesStatusText` | Partial | Verifies command rail/secondary/inspector/log drawer presence, no player-facing floating Tooltip object, log drawer default-collapsed and expandable/collapsible layout behavior, player-facing `행동 종료` only, action hover preview continuity through bottom tactical cards/inspector panel, and turn-order hover battlefield highlight. Final visual density remains editor-play QA. |

## 목적

- `SRPG_GDD.md`와 `SRPG_전투규칙_기준서_v2.md`의 요구사항을 테스트와 연결한다.
- 코드 2차 구현 시 어떤 규칙이 검증 대상인지 ID 기준으로 추적한다.

## 상태 정의

- `완전`: 자동 테스트가 핵심 조건을 직접 검증
- `부분`: 간접 검증 또는 일부 시나리오만 검증
- `미커버`: 자동 테스트 없음

## 요구사항 매핑

| 요구사항 ID | 규칙 요약 | 현재 커버 테스트 | 상태 | 다음 액션 |
| --- | --- | --- | --- | --- |
| RQ-001 | 속도 기반 라운드 턴 | `SrpM1CoreTests.TurnOrder_UsesSpeedDescending` | 완전 | 동적 속도 변경 도입 시 케이스 추가 |
| RQ-002 | AP 2 표시/소비 | `SrpM1PlayModeTests.DefaultOpeningPrototypePreset_InitializesRoundAndHud`, `SrpM1PlayModeTests.DangerAreaAndHoverPreview_UpdatesStatusText`, `SrpM1PlayModeTests.DirectControlUi_ChangesStanceFacingAndOverclocksSkill`, `SrpM1AiPlaySampleTests.PlayMode_Runtime_Revalidation_For_AiQa`, `SrpM1CoreTests.FirearmAmmo_SpendsBlocksAndReloads`, `SrpM1CoreTests.FirearmDefaults_StartWithSingleLoadedShot`, `SrpM1CoreTests.FirearmBasicAttack_IsHighHpPressureAndNonFirearmsIgnoreAmmo`, `SrpM1CoreTests.CoverState_UsesAdjacentBlockedTileAndClonesIndependently`, `SrpM1CoreTests.InteractionAction_SpendsApActivatesAndClaimsOwner`, `SrpM1CoreTests.InteractionPoint_FindsAdjacentAvailablePoint`, `SrpM1CoreTests.InteractionPoint_BlocksOwnerMismatch`, `SrpM1CoreTests.InteractionPoint_CloneCopiesStateIndependently`, `SrpMakerMetadataTests.MapMetadata_JsonRoundTrip_PreservesInteractionPoints` | 부분 | AP 실제 소모 시점별 HUD/로그 전이 assert 추가 |
| RQ-003 | RP 1 반응 자원 | `SrpM1CoreTests.TurnOrder_ResetRoundResources_RestoresRpAndClearsReactionState`, `SrpM1CoreTests.Overwatch_RequiresAndConsumesSidearmAmmo`, `SrpM1RuleSpecTests.DefensiveReaction_ConsumesRpAndReducesIncomingDamage_WhenStateProvided`, `SrpM1RuleSpecTests.OpportunityAttack_ConsumesEnemyRpAndDamagesMover_WhenLeavingEngagement`, `SrpM1RuleSpecTests.OpportunityAttack_DoesNotTrigger_WhenEnemyHasNoRp`, `SrpM1RuleSpecTests.DodgeReaction_ConsumesRpAndResolvesByChance_ForAggressiveDefender`, `SrpM1RuleSpecTests.DodgeReaction_FailureKeepsMitigatedDamageWithoutGuardBackup`, `SrpM1RuleSpecTests.ParryReaction_ConsumesRpAndNullifiesDamage_WhenTaggedSkillMatches`, `SrpM1RuleSpecTests.Overwatch_ArmCloneAndTrigger_UsesApReservationAndRpReactionShot`, `SrpM1RuleSpecTests.Overwatch_MeleeRoleSidearm_BlocksAdjacentAndTriggersFirearmAtRange`, `SrpM1RuleSpecTests.Overwatch_CanTrigger_AllowsVectorAimOutsideAxisOrDiagonalLine`, `SrpM1RuleSpecTests.Overwatch_CanTrigger_BlocksWhenLineOfSightIsObstructed`, `SrpM1RuleSpecTests.LineOfSight_CoverSegmentBlocksOverwatchAndFirearmBasicAttack`, `SrpM1RuleSpecTests.Overwatch_SelectTriggerWatcher_UsesDistanceSpeedAndIdPriority`, `SrpM1RuleSpecTests.Overwatch_ArmStatus_ExplainsHudPolicyConditions`, `SrpM1RuleSpecTests.Overwatch_ArmStatus_TracksReservationAndRoundReset`, `SrpM1PlayModeTests.DefaultOpeningPrototypePreset_InitializesRoundAndHud`, `SrpM1AiPlaySampleTests.PlayMode_Runtime_Revalidation_For_AiQa` | 부분 | 반응 상태 전이는 HUD 스모크로 고정, 경계태세 가능 조건은 `weaponClass`가 아니라 총기 능력/탄약/사거리/LOS 기반으로 고정 |
| RQ-004 | GRD+반응+엄폐/위치/특성 방어 | `SrpM1CoreTests.FirearmBasicAttack_IsHighHpPressureAndNonFirearmsIgnoreAmmo`, `SrpM1CoreTests.CoverBuffer_ReducesOnlyRangedFirearmDamage`, `SrpM1CoreTests.CoverBuffer_AppliesToOverwatchFire`, `SrpM1CoreTests.CoverSegment_LoadsFindsAndClonesIndependently`, `SrpM1CoreTests.CoverSegment_AppliesOnlyFromProtectedDirection`, `SrpMakerMetadataTests.MapMetadata_JsonRoundTrip_PreservesCoverSegments`, `SrpM1RuleSpecTests.LineOfSight_CoverSegmentBlocksOverwatchAndFirearmBasicAttack`, `SrpMakerMetadataTests.M1OpeningPrototypePreset_LoadsAsAsymmetricFirstBattleScenario`, `SrpM1RuleSpecTests.Stance_Defensive_ReducesIncomingDamage`, `SrpM1RuleSpecTests.DefensiveReaction_ConsumesRpAndReducesIncomingDamage_WhenStateProvided`, `SrpM1RuleSpecTests.DodgeReaction_ConsumesRpAndResolvesByChance_ForAggressiveDefender`, `SrpM1RuleSpecTests.DodgeReaction_FailureKeepsMitigatedDamageWithoutGuardBackup`, `SrpM1RuleSpecTests.ParryReaction_ConsumesRpAndNullifiesDamage_WhenTaggedSkillMatches`, `SrpM1RuleSpecTests.DirectionalVulnerability_IncreasesDamage_WhenHitFromBack`, `SrpCombatResolverTests.ApplyAttack_DefensiveStanceReducesPgOnly`, `SrpCombatResolverTests.ApplyAttack_GuardReactionReducesHpAndPg`, `SrpCombatResolverTests.ApplyAttack_UnusedStanceGrdReducesFirearmSpillover` | 부분 | 공통 DEF 제거와 GRD PG 전용화 수치 확정 후 테스트 확장 |
| RQ-005 | 공격/수비 2태세 | `SrpM1RuleSpecTests.Stance_Aggressive_IncreasesPgPressure`, `SrpM1RuleSpecTests.Stance_Defensive_ReducesIncomingDamage`, `SrpM1PlayModeTests.DirectControlUi_ChangesStanceFacingAndOverclocksSkill` | 부분 | 턴 시작 선택 UX 세부 제한은 PlayMode 스모크로 고정, 최종 턴 시작 전용 플로우 확정 시 보강 |
| RQ-006 | 공격 태세 고위험 회피 | `SrpM1RuleSpecTests.DodgeReaction_ConsumesRpAndResolvesByChance_ForAggressiveDefender`, `SrpM1RuleSpecTests.DodgeReaction_FailureKeepsMitigatedDamageWithoutGuardBackup`, `SrpCombatResolverTests.BattleRandom_SameSeedAndCloneProduceSameSequence`, `SrpCombatResolverTests.BattleRandom_DodgeRateIsCloseToConfiguredChance`, `SrpCombatResolverTests.PreviewClone_DoesNotRollOrRevealDodge` | 부분 | 시드 난수 50%는 고정. 스탯/방향 가중치 확정 시 기대값 테스트 |
| RQ-007 | 수비 태세 안정 생존 | `SrpM1RuleSpecTests.Stance_Defensive_ReducesIncomingDamage`, `SrpM1RuleSpecTests.SustainedDefenseBuffer_AppliesOnFollowUpHit_WhenDefensiveAndEngaged`, `SrpMakerMetadataTests.SkillDamage_TriggersGroggy_WhenPgFallsToZero`, `SrpCombatResolverTests.ApplyAttack_SustainedDefenseBufferReducesPgOnly` | 부분 | 수비 완충 수치 확정 후 추가 케이스 보강 |
| RQ-008 | 주인공 전용 패링 | `SrpM1RuleSpecTests.ParryCondition_AllowsTaggedFrontMeleeSkillParry_WhenDefenderHasTagAndRp`, `SrpM1RuleSpecTests.ParryCondition_BlocksInvalidThreats`, `SrpM1RuleSpecTests.ParryReaction_ConsumesRpAndNullifiesDamage_WhenTaggedSkillMatches`, `SrpMakerMetadataTests.UnitMetadata_JsonRoundTrip_PreservesV2StatsEnumsAndTags` | 부분 | 패링 보상/실패 패널티 최종 수치 테스트 추가 |
| RQ-009 | 패링 텔레그래프 | 패링 가능 공격/스킬 오버레이 코드 연결, 조건 판정 테스트 일부, `SrpMakerMetadataTests.SkillMetadata_JsonRoundTrip_PreservesChargeOverclockAndParryFields`, `SrpMakerMetadataTests.M1QaIntegratedPreset_ExposesPhase2SkillsAndScenarioHooks`, `SrpMakerMetadataTests.M1OpeningPrototypePreset_LoadsAsAsymmetricFirstBattleScenario`, `SrpM1PlayModeTests.DefaultOpeningPrototypePreset_InitializesRoundAndHud`, `SrpM1AiPlaySampleTests.PlayMode_Runtime_Revalidation_For_AiQa` | 부분 | 실제 패링 가능 스킬 선택 중 청록 오버레이 PlayMode 검증 추가 |
| RQ-010 | 둘러싸임 대응(탱커 축) | `SrpM1RuleSpecTests.ZocPenalty_IncreasesMoveCost_WhenEnemyAdjacent`, `SrpM1RuleSpecTests.EngagementExit_IncreasesMoveCost_WhenLeavingEnemyAdjacency`, `SrpM1RuleSpecTests.OpportunityAttack_ConsumesEnemyRpAndDamagesMover_WhenLeavingEngagement`, `SrpM1RuleSpecTests.BattleStateClone_CopiesEngagementAndReactionStateIndependently`, `SrpM1RuleSpecTests.SustainedDefenseBuffer_AppliesOnFollowUpHit_WhenDefensiveAndEngaged`, `SrpM1RuleSpecTests.TankMultiEngagementBuffer_AppliesOnlyForTank_WhenEngagedByMultipleEnemies`, `SrpM1RuleSpecTests.EngagementLabPreset_StartsWithTankInMultiEngagement`, `SrpM1RuleSpecTests.EngagementLabPreset_DisengageMoveHasExitCostAndOpportunityAttack`, `SrpM1RuleSpecTests.EngagementLabPreset_AppliesTankAndSustainedDefenseBuffers`, `SrpMakerMetadataTests.UnitMetadata_JsonRoundTrip_PreservesV2StatsEnumsAndTags` | 부분 | 탱커 패시브 최종 수치/형태 확정 후 프리셋 케이스 보강 |
| RQ-011 | 쿨다운/충전 스킬 | `SrpM1CoreTests.SkillCharges_BlockUse_WhenNoChargesRemain`, `SrpM1CoreTests.SkillUse_ConsumesChargeAndAppliesCooldown`, `SrpM1CoreTests.SkillResourceTick_ReducesCooldownAndRestoresCharge`, `SrpMakerMetadataTests.SkillMetadata_JsonRoundTrip_PreservesChargeOverclockAndParryFields`, `SrpMakerMetadataTests.BattleState_FromMap_AppliesAllowedDisabledAndMaxSkillFilters`, `SrpMakerMetadataTests.SkillBuffStat_ApAndPostureAliases_ModifyRuntimeStats`, `SrpMakerMetadataTests.M1QaIntegratedPreset_ExposesPhase2SkillsAndScenarioHooks`, `SrpMakerMetadataTests.SkillOverclock_CanOverclockOnlyWhenResourceCanRecover`, `SrpM1PlayModeTests.DangerAreaAndHoverPreview_UpdatesStatusText`, `SrpM1PlayModeTests.DirectControlUi_ChangesStanceFacingAndOverclocksSkill`, `SrpM1SkillMapTests.AreaEnemySkill_HitsOnlyEnemiesInsideArea`, `SrpM1SkillMapTests.AreaAllySkill_HealsAlliesIncludingCaster`, `SrpM1SkillMapTests.AreaSkill_SelfEffectAppliesOnce`, `SrpM1SkillMapTests.SkillTargetTiles_FollowSkillSightRule`, `SrpM1SkillMapTests.LegacyCleaveEffect_UsesDamageRules` | 부분 | 스킬 drawer 분리/닫힘은 PlayMode로 보강됨. 실제 메이커 UI 조작 스모크는 후속 검토 |
| RQ-012 | 안정도 오버클럭 | `SrpM1CoreTests.SkillOverclock_SpendsFrozenHeartAndRestoresSkillResource`, `SrpM1CoreTests.SkillOverclockPowerBonus_EnhancesNextActiveSkillUseOnce`, `SrpMakerMetadataTests.SkillMetadata_JsonRoundTrip_PreservesChargeOverclockAndParryFields`, `SrpMakerMetadataTests.SkillOverclock_CanOverclockOnlyWhenResourceCanRecover`, `SrpM1PlayModeTests.DirectControlUi_ChangesStanceFacingAndOverclocksSkill` | 부분 | 안정도 정식 수치계 확정 후 전용 UI 표기 보강 |
| RQ-013 | HP 피해 중대/경미 분류 | `SrpM1RuleSpecTests.PerfectDefense_NullsMinorHpDamage_ButNotMajorOrBackAttack`, `SrpCombatResolverTests.DefaultCleave_IsMajorHpDamageAndPiercesPerfectDefense` | 부분 | 스킬 피해는 전부 중대로 고정. 치명타나 경미 HP 피해 스킬이 생기면 케이스 추가 |
| RQ-014 | 탱커 `완벽한 수비` | `SrpM1RuleSpecTests.PerfectDefense_NullsMinorHpDamage_ButNotMajorOrBackAttack`, 기존 `TankMultiEngagementBuffer_AppliesOnlyForTank_WhenEngagedByMultipleEnemies` | 완전 | Tank 태그와 고유 패시브 데이터 계약을 분리할 경우 JSON/프리셋 테스트 추가 |
| RQ-015 | 캐릭터 고유 패시브 | `SrpM1CoreTests.InitialFourRolePassives_ApplyBridgeEffects`, `SrpMakerMetadataTests.M1QaIntegratedPreset_ExposesPhase2SkillsAndScenarioHooks`, `SrpMakerMetadataTests.M1OpeningPrototypePreset_LoadsAsAsymmetricFirstBattleScenario` | 부분 | 최종 이름/전직 연계/밸런스 수치 확정 후 기대값 갱신 |
| RQ-016 | 전직 특성 구조 | 없음 | 미커버 | 전직 시스템 착수 전까지 문서 추적만 유지 |
| RQ-017 | 주인공 전장 적응형 브루저 | 기존 `ParryUser`/패링 조건 테스트, `SrpM1CoreTests.InitialFourRolePassives_ApplyBridgeEffects`, `SrpMakerMetadataTests.M1QaIntegratedPreset_ExposesPhase2SkillsAndScenarioHooks`, `SrpMakerMetadataTests.M1OpeningPrototypePreset_LoadsAsAsymmetricFirstBattleScenario` | 부분 | 주인공 패시브 최종 수치와 전직 연계 확정 후 테스트 갱신 |
| RQ-018 | 패링 성공 보상 | `SrpM1RuleSpecTests.ParryReaction_ConsumesRpAndNullifiesDamage_WhenTaggedSkillMatches` | 완전 | PG 피해량/균형 붕괴 지속 수치 변경 시 기대값 갱신 |
| RQ-019 | 공용 전투 태그 | `SrpM1RuleSpecTests.CombatTags_RefreshConsumeAndApplyNextAttackPressure`, `SrpM1CoreTests.ApplyCombatTagSkill_AddsRuntimeTagToTarget`, `SrpMakerMetadataTests.SkillMetadata_JsonRoundTrip_PreservesChargeOverclockAndParryFields`, `SrpMakerMetadataTests.M1QaIntegratedPreset_ExposesPhase2SkillsAndScenarioHooks`, `SrpM1SkillMapTests.CombatTag_ExpiresAfterDurationRounds`, `SrpM1SkillMapTests.CombatTag_ZeroDurationIsPermanentAndRefreshResetsExpiry` | 부분 | 지속시간은 라운드 기준으로 고정. 태그 수치 확정 시 기대값 갱신, PlayMode 표시 검증 추가 |
| RQ-020 | `노출` 포지션 상태 | 엄폐/방향성 엄폐 테스트 일부가 간접 검증, `SrpM1SkillMapTests.Sight_BlockedOnlyByLineBlockingCoverSegment`, `SrpM1SkillMapTests.MakerData_CoverMapAndAreaSkillWorkInBattleAfterJsonRoundTrip` | 부분 | 엄폐 밖/사선 노출 판정 helper와 비디버프 보존 테스트 추가 |
| RQ-021 | 초기 4인 파티 역할 | `SrpM1CoreTests.InitialFourRolePassives_ApplyBridgeEffects`, `SrpM1CoreTests.ArcaneScreen_RestoresAllyPgAsMagicBattlefieldIntervention`, `SrpMakerMetadataTests.M1QaIntegratedPreset_ExposesPhase2SkillsAndScenarioHooks`, `SrpMakerMetadataTests.M1OpeningPrototypePreset_LoadsAsAsymmetricFirstBattleScenario`, `SrpM1PlayModeTests.M1OpeningPrototypePreset_InitializesFromGameSettings` | 부분 | 캐릭터별 대표 스킬 최종 수치와 전직 연계 확정 후 프리셋 테스트 갱신 |
| RQ-022 | 총기 HP-PG 파급 | `SrpCombatResolverTests.ApplyAttack_FirearmSpillsThirdOfFinalHpDamageToPg`, `SrpM1CoreTests.FirearmSpillover_UsesPostCoverFinalHpDamage` | 완전 | 비율/반올림/최소값 변경 시 기대값 갱신 |

## UX 피드백 계약

| 항목 | 검증 테스트 | 상태 | 다음 액션 |
| --- | --- | --- | --- |
| 현재 행동/선택/hover 유닛 ring | `SrpM1PlayModeTests.DefaultOpeningPrototypePreset_InitializesRoundAndHud`, `SrpM1PlayModeTests.DangerAreaAndHoverPreview_UpdatesStatusText` | 부분 | 로컬 Unity 플레이 화면에서 ring 두께/겹침 가독성 추가 확인 |
| 행동 순서 icon strip (`TBD-011`) | `SrpM1PlayModeTests.DefaultOpeningPrototypePreset_InitializesRoundAndHud`, `SrpM1AiPlaySampleTests.PlayMode_Runtime_Revalidation_For_AiQa` | 부분 | 정식 초상화/역할 아이콘 도입 시 토큰 구성과 밀도 재검증 |
| ZOC/교전 unit badge | `SrpM1PlayModeTests.DangerAreaAndHoverPreview_UpdatesStatusText` | 부분 | 실제 교전 밀집 장면에서 badge 위치와 타일 오버레이 충돌 여부 확인 |
| 턴 시작/종료 floating feedback | `SrpM1PlayModeTests.DefaultOpeningPrototypePreset_InitializesRoundAndHud` | 부분 | 정식 VFX 단계에서 지속시간/애니메이션 보정 |
| 스킬 준비/사용 floating feedback | `SrpM1PlayModeTests.DirectControlUi_ChangesStanceFacingAndOverclocksSkill` | 부분 | 대표 스킬별 짧은 표시명 최종화 후 문구 보정 |
| 피해/회복/중립 flash | PlayMode 피드백 생성 계약과 런타임 HP/PG 변화 기반 flash 연결 | 부분 | 실제 화면 기준 과도한 flash 여부 확인 |
| 총기 기본 조준/aim line (`TBD-010`) | `SrpM1RuleSpecTests.FirearmAim_HelperIsSharedByBasicAttackAndOverwatch`, `SrpM1RuleSpecTests.FirearmBasicAttack_NonEightDirectionAimStillRespectsBlockers`, `SrpM1PlayModeTests.FirearmAimPreview_ShowsNonEightDirectionAimLine` | 부분 | 정식 VFX/무기별 arc/diagonal facing 도입 여부 확정 후 확장 |
| 경계태세 사용자-facing 문구/사망 즉시 갱신 (`TBD-014`, `BUG-001`) | `SrpM1PlayModeTests.DefaultOpeningPrototypePreset_InitializesRoundAndHud`, `SrpM1PlayModeTests.OverwatchReactionKill_RefreshesWorldAndHudImmediately`, `SrpM1RuleSpecTests.Overwatch_MeleeRoleSidearm_BlocksAdjacentAndTriggersFirearmAtRange`, `SrpM1AiPlaySampleTests.PlayMode_Runtime_Revalidation_For_AiQa` | 부분 | 정식 사격 VFX/애니메이션 도입 시 발동 연출 재검증. 예약 가능 여부는 역할 표시용 `weaponClass`가 아니라 총기 능력/탄약/사거리/LOS 조건을 따른다. |
| 공격/위험·경계태세 marker (`TBD-012` 후속) | `SrpM1PlayModeTests.DangerAreaAndHoverPreview_UpdatesStatusText`, `SrpM1OpeningObservationTests.M1OpeningPrototype_Captures_FirstScreen_RouteObservation` | 부분 | 실제 에디터 플레이 화면에서 marker 크기/채도 미세 조정 |
| 전투 preview 문법 재정렬 (`TBD-015`) | `SrpM1CoreTests.MovePreviewEvaluator_UsesCloneWithoutMutatingBattleState`, `SrpM1PlayModeTests.DefaultOpeningPrototypePreset_InitializesRoundAndHud`, `SrpM1PlayModeTests.DangerAreaAndHoverPreview_UpdatesStatusText` | 부분 | threat line 곡선/endpoint pulse와 버튼 hover 연출은 실제 에디터 플레이 화면에서 미세 조정 |

## 운영 규칙

1. 새 규칙 구현 시 먼저 이 표의 상태를 업데이트한다.
2. 테스트 추가 시 `요구사항 ID`를 테스트 이름/주석/문서에 함께 기록한다.
3. 릴리즈 전 이 표의 `미커버` 항목을 재확인한다.
