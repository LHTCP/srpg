# SRPG GDD-테스트 추적 매핑표 (v2)

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
| RQ-002 | AP 2 표시/소비 | `SrpM1PlayModeTests.M1IntegratedPreset_InitializesRoundAndHud`, `SrpM1PlayModeTests.DangerAreaAndHoverPreview_UpdatesStatusText`, `SrpM1PlayModeTests.DirectControlUi_ChangesStanceFacingAndOverclocksSkill`, `SrpM1AiPlaySampleTests.PlayMode_Runtime_Revalidation_For_AiQa`, `SrpM1CoreTests.FirearmAmmo_SpendsBlocksAndReloads`, `SrpM1CoreTests.FirearmDefaults_StartWithSingleLoadedShot`, `SrpM1CoreTests.FirearmBasicAttack_IsHighHpPressureAndNonFirearmsIgnoreAmmo`, `SrpM1CoreTests.CoverState_UsesAdjacentBlockedTileAndClonesIndependently`, `SrpM1CoreTests.InteractionAction_SpendsApActivatesAndClaimsOwner`, `SrpM1CoreTests.InteractionPoint_FindsAdjacentAvailablePoint`, `SrpM1CoreTests.InteractionPoint_BlocksOwnerMismatch`, `SrpM1CoreTests.InteractionPoint_CloneCopiesStateIndependently`, `SrpMakerMetadataTests.MapMetadata_JsonRoundTrip_PreservesInteractionPoints` | 부분 | AP 실제 소모 시점별 HUD/로그 전이 assert 추가 |
| RQ-003 | RP 1 반응 자원 | `SrpM1CoreTests.TurnOrder_ResetRoundResources_RestoresRpAndClearsReactionState`, `SrpM1CoreTests.Overwatch_RequiresAndConsumesFirearmAmmo`, `SrpM1RuleSpecTests.DefensiveReaction_ConsumesRpAndReducesIncomingDamage_WhenStateProvided`, `SrpM1RuleSpecTests.OpportunityAttack_ConsumesEnemyRpAndDamagesMover_WhenLeavingEngagement`, `SrpM1RuleSpecTests.OpportunityAttack_DoesNotTrigger_WhenEnemyHasNoRp`, `SrpM1RuleSpecTests.DodgeReaction_ConsumesRpAndResolvesByChance_ForAggressiveDefender`, `SrpM1RuleSpecTests.DodgeReaction_FailureKeepsMitigatedDamageWithoutGuardBackup`, `SrpM1RuleSpecTests.ParryReaction_ConsumesRpAndNullifiesDamage_WhenTaggedSkillMatches`, `SrpM1RuleSpecTests.Overwatch_ArmCloneAndTrigger_UsesApReservationAndRpReactionShot`, `SrpM1RuleSpecTests.Overwatch_CanTrigger_RequiresEightDirectionLineOfSight`, `SrpM1RuleSpecTests.Overwatch_CanTrigger_BlocksWhenLineOfSightIsObstructed`, `SrpM1RuleSpecTests.Overwatch_ArmStatus_ExplainsHudPolicyConditions`, `SrpM1RuleSpecTests.Overwatch_ArmStatus_TracksReservationAndRoundReset`, `SrpM1PlayModeTests.M1IntegratedPreset_InitializesRoundAndHud`, `SrpM1AiPlaySampleTests.PlayMode_Runtime_Revalidation_For_AiQa` | 부분 | 반응 상태 전이는 HUD 스모크로 고정, 실제 반응 발생 PlayMode 시나리오는 후속 보강 |
| RQ-004 | GRD+반응+엄폐/위치/특성 방어 | `SrpM1CoreTests.FirearmBasicAttack_IsHighHpPressureAndNonFirearmsIgnoreAmmo`, `SrpM1CoreTests.CoverBuffer_ReducesOnlyRangedFirearmDamage`, `SrpM1CoreTests.CoverBuffer_AppliesToOverwatchFire`, `SrpM1CoreTests.CoverSegment_LoadsFindsAndClonesIndependently`, `SrpM1CoreTests.CoverSegment_AppliesOnlyFromProtectedDirection`, `SrpMakerMetadataTests.MapMetadata_JsonRoundTrip_PreservesCoverSegments`, `SrpM1RuleSpecTests.Stance_Defensive_ReducesIncomingDamage`, `SrpM1RuleSpecTests.DefensiveReaction_ConsumesRpAndReducesIncomingDamage_WhenStateProvided`, `SrpM1RuleSpecTests.DodgeReaction_ConsumesRpAndResolvesByChance_ForAggressiveDefender`, `SrpM1RuleSpecTests.DodgeReaction_FailureKeepsMitigatedDamageWithoutGuardBackup`, `SrpM1RuleSpecTests.ParryReaction_ConsumesRpAndNullifiesDamage_WhenTaggedSkillMatches`, `SrpM1RuleSpecTests.DirectionalVulnerability_IncreasesDamage_WhenHitFromBack` | 부분 | 공통 DEF 제거, GRD PG 전용화, 방향성 엄폐 사선 차단 적용 순서 테스트 확장 |
| RQ-005 | 공격/수비 2태세 | `SrpM1RuleSpecTests.Stance_Aggressive_IncreasesPgPressure`, `SrpM1RuleSpecTests.Stance_Defensive_ReducesIncomingDamage`, `SrpM1PlayModeTests.DirectControlUi_ChangesStanceFacingAndOverclocksSkill` | 부분 | 턴 시작 선택 UX 세부 제한은 PlayMode 스모크로 고정, 최종 턴 시작 전용 플로우 확정 시 보강 |
| RQ-006 | 공격 태세 고위험 회피 | `SrpM1RuleSpecTests.DodgeReaction_ConsumesRpAndResolvesByChance_ForAggressiveDefender`, `SrpM1RuleSpecTests.DodgeReaction_FailureKeepsMitigatedDamageWithoutGuardBackup` | 부분 | 회피 확률식/스탯 가중치 확정 후 기대값 테스트 |
| RQ-007 | 수비 태세 안정 생존 | `SrpM1RuleSpecTests.Stance_Defensive_ReducesIncomingDamage`, `SrpM1RuleSpecTests.SustainedDefenseBuffer_AppliesOnFollowUpHit_WhenDefensiveAndEngaged`, `SrpMakerMetadataTests.SkillDamage_TriggersGroggy_WhenPgFallsToZero` | 부분 | 수비 완충 수치 확정 후 추가 케이스 보강 |
| RQ-008 | 주인공 전용 패링 | `SrpM1RuleSpecTests.ParryCondition_AllowsTaggedFrontMeleeSkillParry_WhenDefenderHasTagAndRp`, `SrpM1RuleSpecTests.ParryCondition_BlocksInvalidThreats`, `SrpM1RuleSpecTests.ParryReaction_ConsumesRpAndNullifiesDamage_WhenTaggedSkillMatches`, `SrpMakerMetadataTests.UnitMetadata_JsonRoundTrip_PreservesV2StatsEnumsAndTags` | 부분 | 패링 보상/실패 패널티 최종 수치 테스트 추가 |
| RQ-009 | 패링 텔레그래프 | 패링 가능 공격/스킬 오버레이 코드 연결, 조건 판정 테스트 일부, `SrpMakerMetadataTests.SkillMetadata_JsonRoundTrip_PreservesChargeOverclockAndParryFields`, `SrpMakerMetadataTests.M1QaIntegratedPreset_ExposesPhase2SkillsAndScenarioHooks`, `SrpM1PlayModeTests.M1IntegratedPreset_InitializesRoundAndHud`, `SrpM1AiPlaySampleTests.PlayMode_Runtime_Revalidation_For_AiQa` | 부분 | 실제 패링 가능 스킬 선택 중 청록 오버레이 PlayMode 검증 추가 |
| RQ-010 | 둘러싸임 대응(탱커 축) | `SrpM1RuleSpecTests.ZocPenalty_IncreasesMoveCost_WhenEnemyAdjacent`, `SrpM1RuleSpecTests.EngagementExit_IncreasesMoveCost_WhenLeavingEnemyAdjacency`, `SrpM1RuleSpecTests.OpportunityAttack_ConsumesEnemyRpAndDamagesMover_WhenLeavingEngagement`, `SrpM1RuleSpecTests.BattleStateClone_CopiesEngagementAndReactionStateIndependently`, `SrpM1RuleSpecTests.SustainedDefenseBuffer_AppliesOnFollowUpHit_WhenDefensiveAndEngaged`, `SrpM1RuleSpecTests.TankMultiEngagementBuffer_AppliesOnlyForTank_WhenEngagedByMultipleEnemies`, `SrpM1RuleSpecTests.EngagementLabPreset_StartsWithTankInMultiEngagement`, `SrpM1RuleSpecTests.EngagementLabPreset_DisengageMoveHasExitCostAndOpportunityAttack`, `SrpM1RuleSpecTests.EngagementLabPreset_AppliesTankAndSustainedDefenseBuffers`, `SrpMakerMetadataTests.UnitMetadata_JsonRoundTrip_PreservesV2StatsEnumsAndTags` | 부분 | 탱커 패시브 최종 수치/형태 확정 후 프리셋 케이스 보강 |
| RQ-011 | 쿨다운/충전 스킬 | `SrpM1CoreTests.SkillCharges_BlockUse_WhenNoChargesRemain`, `SrpM1CoreTests.SkillUse_ConsumesChargeAndAppliesCooldown`, `SrpM1CoreTests.SkillResourceTick_ReducesCooldownAndRestoresCharge`, `SrpMakerMetadataTests.SkillMetadata_JsonRoundTrip_PreservesChargeOverclockAndParryFields`, `SrpMakerMetadataTests.BattleState_FromMap_AppliesAllowedDisabledAndMaxSkillFilters`, `SrpMakerMetadataTests.SkillBuffStat_ApAndPostureAliases_ModifyRuntimeStats`, `SrpMakerMetadataTests.M1QaIntegratedPreset_ExposesPhase2SkillsAndScenarioHooks`, `SrpMakerMetadataTests.SkillOverclock_CanOverclockOnlyWhenResourceCanRecover`, `SrpM1PlayModeTests.DangerAreaAndHoverPreview_UpdatesStatusText`, `SrpM1PlayModeTests.DirectControlUi_ChangesStanceFacingAndOverclocksSkill` | 부분 | 실제 메이커 UI 조작 스모크/PlayMode 테스트 검토 |
| RQ-012 | 안정도 오버클럭 | `SrpM1CoreTests.SkillOverclock_SpendsFrozenHeartAndRestoresSkillResource`, `SrpM1CoreTests.SkillOverclockPowerBonus_EnhancesNextActiveSkillUseOnce`, `SrpMakerMetadataTests.SkillMetadata_JsonRoundTrip_PreservesChargeOverclockAndParryFields`, `SrpMakerMetadataTests.SkillOverclock_CanOverclockOnlyWhenResourceCanRecover`, `SrpM1PlayModeTests.DirectControlUi_ChangesStanceFacingAndOverclocksSkill` | 부분 | 안정도 정식 수치계 확정 후 전용 UI 표기 보강 |
| RQ-013 | HP 피해 중대/경미 분류 | 없음 | 미커버 | 총격/처단/기회공격/HP 직격/치명타와 경미 근접 피해 분류 테스트 추가 |
| RQ-014 | 탱커 `완벽한 수비` | 기존 `TankMultiEngagementBuffer_AppliesOnlyForTank_WhenEngagedByMultipleEnemies`는 브릿지 검증만 담당 | 미커버 | 수비 태세, PG 미붕괴, 후방 아님 조건에서 경미 HP 피해 무효 및 중대 HP 피해 비무효 테스트 추가 |
| RQ-015 | 캐릭터 고유 패시브 | 없음 | 미커버 | 초기 4인 유닛 데이터에 고유 패시브/태그/설명 계약 추가 후 JSON/프리셋 테스트 |
| RQ-016 | 전직 특성 구조 | 없음 | 미커버 | 전직 시스템 착수 전까지 문서 추적만 유지 |
| RQ-017 | 주인공 전장 적응형 브루저 | 기존 `ParryUser`/패링 조건 테스트 일부 | 부분 | 주인공 대표 스킬/패시브 데이터 갱신 후 프리셋 테스트 추가 |
| RQ-018 | 패링 성공 보상 | `SrpM1RuleSpecTests.ParryReaction_ConsumesRpAndNullifiesDamage_WhenTaggedSkillMatches`는 공격 무효 브릿지까지만 검증 | 부분 | 패링 성공 시 PG 대량 피해와 `균형 붕괴` 부여 테스트 추가 |
| RQ-019 | 공용 전투 태그 | 없음 | 미커버 | `표식`, `균형 붕괴`, `사살 지시` 저장/갱신/표시/소모 테스트 추가 |
| RQ-020 | `노출` 포지션 상태 | 엄폐/방향성 엄폐 테스트 일부가 간접 검증 | 부분 | 엄폐 밖/사선 노출 판정 helper와 비디버프 보존 테스트 추가 |
| RQ-021 | 초기 4인 파티 역할 | `M1QaIntegratedPreset_ExposesPhase2SkillsAndScenarioHooks`는 최신 기능 노출만 검증 | 부분 | 주인공/탱커/사격수/마도사 역할별 대표 스킬과 전투 태그 연계 프리셋 테스트 추가 |
| RQ-022 | 총기 HP-PG 파급 | 기존 `FirearmBasicAttack_IsHighHpPressureAndNonFirearmsIgnoreAmmo`는 HP 고화력/낮은 PG 압박 브릿지만 검증 | 미커버 | 총격으로 실제 받은 HP 피해량의 50%를 PG 피해로 추가 적용하는 테스트 추가 |

## 운영 규칙

1. 새 규칙 구현 시 먼저 이 표의 상태를 업데이트한다.
2. 테스트 추가 시 `요구사항 ID`를 테스트 이름/주석/문서에 함께 기록한다.
3. 릴리즈 전 이 표의 `미커버` 항목을 재확인한다.
