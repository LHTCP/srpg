using NUnit.Framework;
using UnityEngine;

[Category("SrpM1All")]
public class SrpM1CoreTests
{
    [Test]
    public void TurnOrder_UsesSpeedDescending()
    {
        var map = SrpDefaultMaps.GetPreset(SrpMapPreset.M1QaIntegrated);
        var state = SrpBattleState.FromMap(map);

        var queue = SrpTurnOrder.BuildRoundQueue(state);
        Assert.Greater(queue.Count, 0);

        int prevSpeed = int.MaxValue;
        foreach (int id in queue)
        {
            var unit = FindUnit(state, id);
            Assert.IsNotNull(unit);
            Assert.LessOrEqual(unit.speed, prevSpeed);
            prevSpeed = unit.speed;
        }
    }

    [Test]
    public void TurnOrder_UsesOwnerAndIdAsTieBreaker_WhenSpeedSame()
    {
        var map = SrpDefaultMaps.GetPreset(SrpMapPreset.M1QaIntegrated);
        var state = SrpBattleState.FromMap(map);
        foreach (var unit in state.Units)
            unit.speed = 10;

        var queue = SrpTurnOrder.BuildRoundQueue(state);
        Assert.Greater(queue.Count, 1);

        int prevOwner = int.MinValue;
        int prevId = int.MinValue;
        foreach (int id in queue)
        {
            var unit = FindUnit(state, id);
            Assert.IsNotNull(unit);

            if (unit.owner == prevOwner)
                Assert.Greater(unit.id, prevId, "동일 owner 내 id 오름차순 타이브레이크 불일치");
            else
                Assert.GreaterOrEqual(unit.owner, prevOwner, "owner 오름차순 타이브레이크 불일치");

            prevOwner = unit.owner;
            prevId = unit.id;
        }
    }

    [Test]
    public void CombatSplit_FirearmAndMeleeProduceDifferentPressure()
    {
        var firearm = new SrpUnitRuntime
        {
            attackPower = 12,
            weaponClass = SrpWeaponClass.Firearm,
            stance = SrpStance.Aggressive,
        };
        var melee = new SrpUnitRuntime
        {
            attackPower = 12,
            weaponClass = SrpWeaponClass.Melee,
            stance = SrpStance.Aggressive,
        };

        var defForFirearm = CreateDefender();
        var defForMelee = CreateDefender();

        var firearmOutcome = SrpCombatResolver.ApplyAttack(firearm, defForFirearm);
        var meleeOutcome = SrpCombatResolver.ApplyAttack(melee, defForMelee);

        Assert.Greater(firearmOutcome.damageToHp, meleeOutcome.damageToHp);
        Assert.Greater(meleeOutcome.damageToPg, firearmOutcome.damageToPg);
    }

    [Test]
    public void TurnOrder_SkipsEliminatedUnits_WhenAdvancing()
    {
        var map = SrpDefaultMaps.GetPreset(SrpMapPreset.M1QaIntegrated);
        var state = SrpBattleState.FromMap(map);
        state.RoundQueue.Clear();
        state.RoundQueue.AddRange(SrpTurnOrder.BuildRoundQueue(state));
        Assert.Greater(state.RoundQueue.Count, 2);

        int removedId = state.RoundQueue[0];
        var removed = FindUnit(state, removedId);
        Assert.IsNotNull(removed);
        removed.eliminated = true;

        int nextId = SrpTurnOrder.AdvanceToNextUnit(state);
        Assert.AreNotEqual(removedId, nextId, "제거된 유닛이 턴 큐에서 건너뛰어지지 않았습니다.");
    }

    [Test]
    public void TurnOrder_ResetRoundResources_RestoresRpAndClearsReactionState()
    {
        var map = SrpDefaultMaps.GetPreset(SrpMapPreset.M1QaIntegrated);
        var state = SrpBattleState.FromMap(map);
        var unit = state.Units[0];
        unit.actionPoints = 0;
        unit.reactionPoints = 0;
        unit.passiveAppliedThisTurn = true;
        unit.lastReactionKind = SrpReactionKind.Guard;
        unit.lastReactionRound = 99;
        unit.lastReactionSourceId = 1234;
        unit.overwatchArmed = true;
        unit.overwatchRange = 4;
        unit.overwatchRound = 99;
        unit.defensiveHitsTakenThisRound = 3;
        unit.defensiveHitsRound = 99;

        SrpTurnOrder.ResetRoundResources(state);

        Assert.AreEqual(unit.maxActionPoints, unit.actionPoints, "라운드 리셋 시 AP가 회복되지 않았습니다.");
        Assert.AreEqual(unit.maxReactionPoints > 0 ? unit.maxReactionPoints : 1, unit.reactionPoints, "라운드 리셋 시 RP 정책이 불일치합니다.");
        Assert.IsFalse(unit.passiveAppliedThisTurn, "라운드 리셋 시 패시브 플래그가 초기화되지 않았습니다.");
        Assert.AreEqual(SrpReactionKind.None, unit.lastReactionKind, "라운드 리셋 시 반응 상태가 초기화되지 않았습니다.");
        Assert.AreEqual(state.RoundNumber, unit.lastReactionRound, "반응 상태의 라운드 기준이 현재 라운드로 갱신되지 않았습니다.");
        Assert.AreEqual(-1, unit.lastReactionSourceId, "라운드 리셋 시 반응 원천 ID가 초기화되지 않았습니다.");
        Assert.IsFalse(unit.overwatchArmed, "라운드 리셋 시 오버워치 예약이 해제되지 않았습니다.");
        Assert.AreEqual(0, unit.overwatchRange, "라운드 리셋 시 오버워치 사거리가 초기화되지 않았습니다.");
        Assert.AreEqual(0, unit.overwatchRound, "라운드 리셋 시 오버워치 라운드가 초기화되지 않았습니다.");
        Assert.AreEqual(0, unit.defensiveHitsTakenThisRound, "라운드 리셋 시 수비 피격 누적이 초기화되지 않았습니다.");
        Assert.AreEqual(state.RoundNumber, unit.defensiveHitsRound, "라운드 리셋 시 수비 피격 라운드가 갱신되지 않았습니다.");
    }

    [Test]
    public void MovePreviewEvaluator_UsesCloneWithoutMutatingBattleState()
    {
        var walkable = new bool[25];
        for (int i = 0; i < walkable.Length; i++)
            walkable[i] = true;

        var map = new SrpMapFileV1
        {
            name = "preview_evaluator_clone_contract",
            width = 5,
            height = 5,
            walkable = walkable,
            templates = new[]
            {
                new SrpUnitTemplateData
                {
                    id = "player",
                    displayName = "Player",
                    moveRange = 4,
                    attackRange = 4,
                    maxAmmo = 1,
                    weaponClass = SrpWeaponClass.Firearm,
                },
                new SrpUnitTemplateData
                {
                    id = "watcher",
                    displayName = "Watcher",
                    moveRange = 4,
                    attackRange = 4,
                    maxAmmo = 1,
                    weaponClass = SrpWeaponClass.Firearm,
                },
            },
            placements = new[]
            {
                new SrpPlacementData { templateId = "player", owner = 0, x = 1, y = 1 },
                new SrpPlacementData { templateId = "watcher", owner = 1, x = 4, y = 1 },
            },
            coverSegments = new[]
            {
                new SrpCoverSegmentData
                {
                    x = 2,
                    y = 1,
                    edge = SrpCoverEdge.North,
                    coverDef = 2,
                    coverGrd = 1,
                },
            },
        };
        var state = SrpBattleState.FromMap(map);
        var player = state.Units[0];
        var watcher = state.Units[1];
        watcher.overwatchArmed = true;
        watcher.overwatchRange = watcher.attackRange;
        watcher.overwatchRound = state.RoundNumber;
        watcher.reactionPoints = 1;
        watcher.ammo = 1;

        var preview = SrpPreviewEvaluator.EvaluateMove(state, player, 2, 1);

        Assert.IsTrue(preview.valid, "이동 preview가 유효한 목적지를 평가하지 못했습니다.");
        Assert.IsTrue(preview.hasCover, "목적지 기준 엄폐 가능성이 preview에 반영되지 않았습니다.");
        Assert.AreEqual(1, player.anchorX, "preview evaluator가 원본 유닛 X 좌표를 변경했습니다.");
        Assert.AreEqual(1, player.anchorY, "preview evaluator가 원본 유닛 Y 좌표를 변경했습니다.");
        Assert.IsTrue(watcher.overwatchArmed, "preview evaluator가 원본 경계태세 예약을 변경했습니다.");
        Assert.AreEqual(1, watcher.ammo, "preview evaluator가 원본 탄약을 변경했습니다.");
        Assert.Greater(preview.threats.Count, 0, "목적지 위협 preview가 비어 있습니다.");
        Assert.IsTrue(preview.threats.Exists(t => t.isOverwatch), "경계사격 위협이 강화 threat로 분류되지 않았습니다.");
    }

    [Test]
    public void FirearmAmmo_SpendsBlocksAndReloads()
    {
        var unit = new SrpUnitRuntime
        {
            weaponClass = SrpWeaponClass.Firearm,
            maxAmmo = 2,
            ammo = 2,
        };

        Assert.IsTrue(unit.HasAmmoForAttack(), "초기 탄약이 있는데 공격 가능으로 판정되지 않았습니다.");
        Assert.IsTrue(unit.SpendAmmoForAttack(), "첫 총기 공격 탄약 소비 실패");
        Assert.AreEqual(1, unit.ammo, "총기 공격 후 탄약이 1 감소하지 않았습니다.");
        Assert.IsTrue(unit.SpendAmmoForAttack(), "두 번째 총기 공격 탄약 소비 실패");
        Assert.AreEqual(0, unit.ammo, "탄약이 0까지 감소하지 않았습니다.");
        Assert.IsFalse(unit.HasAmmoForAttack(), "탄약 0 총기 유닛이 공격 가능으로 판정되었습니다.");
        Assert.IsFalse(unit.SpendAmmoForAttack(), "탄약 0 총기 공격이 차단되지 않았습니다.");
        Assert.IsTrue(unit.ReloadAmmo(), "재장전이 실패했습니다.");
        Assert.AreEqual(2, unit.ammo, "재장전 후 탄약이 최대치로 복구되지 않았습니다.");
    }

    [Test]
    public void FirearmDefaults_StartWithSingleLoadedShot()
    {
        var state = SrpBattleState.FromMap(SrpDefaultMaps.GetPreset(SrpMapPreset.M1QaIntegrated));
        SrpUnitRuntime firearm = null;
        foreach (var unit in state.Units)
        {
            if (unit.weaponClass == SrpWeaponClass.Firearm)
            {
                firearm = unit;
                break;
            }
        }

        Assert.IsNotNull(firearm, "QA 프리셋에서 총기 유닛을 찾지 못했습니다.");
        Assert.AreEqual(1, firearm.maxAmmo, "명시 maxAmmo가 없는 총기 유닛의 기본 탄창은 1발이어야 합니다.");
        Assert.AreEqual(1, firearm.ammo, "총기 유닛은 기본 탄창 1발을 장전한 상태로 시작해야 합니다.");
    }

    [Test]
    public void FirearmBasicAttack_IsHighHpPressureAndNonFirearmsIgnoreAmmo()
    {
        var firearm = new SrpUnitRuntime
        {
            attackPower = 8,
            weaponClass = SrpWeaponClass.Firearm,
            stance = SrpStance.Aggressive,
            maxAmmo = 1,
            ammo = 1,
        };
        var melee = new SrpUnitRuntime
        {
            attackPower = 8,
            weaponClass = SrpWeaponClass.Melee,
            stance = SrpStance.Aggressive,
            maxAmmo = 0,
            ammo = 0,
        };
        var firearmDefender = CreateDefender();
        var meleeDefender = CreateDefender();

        var firearmOutcome = SrpCombatResolver.ApplyAttack(firearm, firearmDefender);
        var meleeOutcome = SrpCombatResolver.ApplyAttack(melee, meleeDefender);

        Assert.GreaterOrEqual(firearmOutcome.damageToHp, 20, "총기 기본 공격이 전장식 1발 고화력 HP 압박으로 동작하지 않습니다.");
        Assert.Greater(firearmOutcome.damageToHp, meleeOutcome.damageToHp, "총기 공격이 근접 공격보다 HP 압박이 낮습니다.");
        Assert.Less(firearmOutcome.damageToPg, meleeOutcome.damageToPg, "총기 공격이 근접 공격보다 PG 압박이 높아졌습니다.");
        Assert.IsTrue(melee.HasAmmoForAttack(), "비총기 유닛은 탄약 0이어도 공격 가능해야 합니다.");
        Assert.IsTrue(melee.SpendAmmoForAttack(), "비총기 유닛은 탄약 소비 없이 공격 가능해야 합니다.");
    }

    [Test]
    public void BattleStateClone_CopiesAmmoStateIndependently()
    {
        var state = SrpBattleState.FromMap(SrpDefaultMaps.GetPreset(SrpMapPreset.M1QaIntegrated));
        SrpUnitRuntime firearm = null;
        foreach (var unit in state.Units)
        {
            if (unit.weaponClass == SrpWeaponClass.Firearm)
            {
                firearm = unit;
                break;
            }
        }
        Assert.IsNotNull(firearm);
        firearm.ammo = 1;

        var clone = state.Clone();
        clone.FindUnitById(firearm.id).ammo = 0;

        Assert.AreEqual(1, firearm.ammo, "클론의 탄약 변경이 원본 상태에 전파되었습니다.");
    }

    [Test]
    public void Overwatch_RequiresAndConsumesSidearmAmmo()
    {
        var state = SrpBattleState.FromMap(SrpDefaultMaps.GetPreset(SrpMapPreset.M1QaIntegrated));
        var watcher = new SrpUnitRuntime
        {
            id = 100,
            owner = 0,
            anchorX = 0,
            anchorY = 0,
            hp = 30,
            maxHp = 30,
            pg = 18,
            maxPg = 18,
            weaponClass = SrpWeaponClass.Melee,
            attackRange = 4,
            attackPower = 8,
            actionPoints = 2,
            reactionPoints = 1,
            maxAmmo = 1,
            ammo = 0,
        };
        var target = new SrpUnitRuntime
        {
            id = 101,
            owner = 1,
            anchorX = 0,
            anchorY = 3,
            hp = 30,
            maxHp = 30,
            pg = 18,
            maxPg = 18,
            weaponClass = SrpWeaponClass.Melee,
            attackRange = 1,
            attackPower = 8,
        };
        state.Units.Clear();
        state.Units.Add(watcher);
        state.Units.Add(target);

        Assert.AreEqual(SrpOverwatchArmStatus.NoAmmo, SrpOverwatch.GetArmStatus(watcher), "탄약 0 총기 유닛이 오버워치 가능으로 판정되었습니다.");

        watcher.ammo = 1;
        Assert.IsTrue(SrpOverwatch.Arm(state, watcher), "탄약 있는 총기 유닛이 오버워치 예약에 실패했습니다.");
        Assert.IsTrue(SrpOverwatch.TryTrigger(state, watcher, target, out _), "오버워치 발동 실패");
        Assert.AreEqual(0, watcher.ammo, "오버워치 발동 후 탄약이 소비되지 않았습니다.");
    }

    [Test]
    public void CoverState_UsesAdjacentBlockedTileAndClonesIndependently()
    {
        var state = CreateCoverTestState();
        var defender = CreateCoverDefender();
        state.Units.Add(defender);

        Assert.IsTrue(state.TryGetAdjacentCover(defender, out int coverX, out int coverY), "인접 비보행 타일을 엄폐물로 판정하지 못했습니다.");
        Assert.AreEqual(1, coverX);
        Assert.AreEqual(2, coverY);

        defender.SetCover(state.RoundNumber, coverX, coverY);
        var clone = state.Clone();
        var cloneDefender = clone.FindUnitById(defender.id);
        cloneDefender.ClearCover();

        Assert.IsTrue(defender.coverActive, "클론의 엄폐 상태 변경이 원본에 전파되었습니다.");
        Assert.IsFalse(cloneDefender.coverActive, "클론 엄폐 해제가 적용되지 않았습니다.");
    }

    [Test]
    public void CoverSegment_LoadsFindsAndClonesIndependently()
    {
        var state = CreateDirectionalCoverTestState();
        var defender = CreateCoverDefender();
        state.Units.Add(defender);

        Assert.AreEqual(1, state.CoverSegments.Count, "방향성 엄폐 segment가 로드되지 않았습니다.");
        Assert.IsTrue(state.TryGetAdjacentCoverSegment(defender, out var segment), "유닛 발밑 edge 엄폐 segment를 찾지 못했습니다.");
        Assert.AreEqual(SrpCoverEdge.North, segment.edge);
        Assert.IsTrue(state.TryGetAdjacentCover(defender, out int coverX, out int coverY), "방향성 엄폐 segment가 엄폐 행동 후보로 잡히지 않았습니다.");
        Assert.AreEqual(2, coverX);
        Assert.AreEqual(2, coverY);

        var clone = state.Clone();
        clone.CoverSegments[0].coverDef = 99;

        Assert.AreEqual(4, state.CoverSegments[0].coverDef, "클론의 방향성 엄폐 변경이 원본에 전파되었습니다.");
        Assert.AreEqual(99, clone.CoverSegments[0].coverDef, "클론 방향성 엄폐 변경이 적용되지 않았습니다.");
    }

    [Test]
    public void CoverObject_LoadsAsBlockingOccupyingCover_AndClonesIndependently()
    {
        var state = CreateCoverTestState();
        var mover = CreateCoverDefender();

        Assert.AreEqual(1, state.CoverObjects.Count, "occupying cover object should load separately from edge segments");
        Assert.IsTrue(state.TryGetCoverObjectAt(1, 2, out var coverObject), "explicit occupying cover object was not found");
        Assert.IsTrue(state.IsCoverTile(1, 2), "cover object tile should be usable as a cover source");
        Assert.IsFalse(state.CanStandAt(mover, 1, 2, mover.id), "occupying cover object tile must remain non-standable");
        Assert.AreEqual("test_blocked_cover", coverObject.visualKey);

        var clone = state.Clone();
        clone.CoverObjects[0].coverDef = 77;

        Assert.AreEqual(2, state.CoverObjects[0].coverDef, "cover object clone mutation leaked to source");
        Assert.AreEqual(77, clone.CoverObjects[0].coverDef, "cover object clone mutation was not applied");
    }

    [Test]
    public void M1OpeningPrototype_CoverObjects_DoNotOverlapStarts_AndBlockStanding()
    {
        var state = SrpBattleState.FromMap(SrpDefaultMaps.GetPreset(SrpMapPreset.M1OpeningPrototype));
        Assert.Greater(state.CoverObjects.Count, 0, "M1 opening central ruin should declare occupying cover objects");

        foreach (var coverObject in state.CoverObjects)
        {
            Assert.IsFalse(state.IsWalkableTile(coverObject.x, coverObject.y), $"cover object tile must be non-walkable: {coverObject.x},{coverObject.y}");
            Assert.IsNull(state.GetOccupant(coverObject.x, coverObject.y), $"cover object overlaps a starting unit: {coverObject.x},{coverObject.y}");
            foreach (var unit in state.Units)
                Assert.IsFalse(state.CanStandAt(unit, coverObject.x, coverObject.y, unit.id), $"unit can stand on occupying cover object: {coverObject.x},{coverObject.y}");
        }
    }

    [Test]
    public void M1OpeningPrototype_EdgeCoverSegments_DoNotBlockStandingOnTheirTile()
    {
        var state = SrpBattleState.FromMap(SrpDefaultMaps.GetPreset(SrpMapPreset.M1OpeningPrototype));
        Assert.Greater(state.CoverSegments.Count, 0, "M1 opening should keep directional edge cover segments");

        foreach (var segment in state.CoverSegments)
        {
            Assert.IsTrue(state.IsWalkableTile(segment.x, segment.y), $"edge cover segment tile should remain walkable: {segment.x},{segment.y}");
            foreach (var unit in state.Units)
            {
                if (state.GetOccupant(segment.x, segment.y) != null)
                    continue;
                Assert.IsTrue(state.CanStandAt(unit, segment.x, segment.y, unit.id), $"edge cover segment blocked standing like a central cube: {segment.x},{segment.y}");
                break;
            }
        }
    }

    [Test]
    public void CoverBuffer_ReducesOnlyRangedFirearmDamage()
    {
        var state = CreateCoverTestState();
        var defender = CreateCoverDefender();
        defender.SetCover(state.RoundNumber, 1, 2);
        state.Units.Add(defender);

        var firearm = CreateCoverAttacker(SrpWeaponClass.Firearm, 2, 0, 8, 4);
        state.Units.Add(firearm);
        var firearmOutcome = SrpCombatResolver.ApplyAttack(state, firearm, defender);

        Assert.IsTrue(firearmOutcome.coverBufferApplied, "원거리 총기 공격에 엄폐 완충이 적용되지 않았습니다.");
        Assert.AreEqual(2, firearmOutcome.reducedHpByCover, "엄폐 HP 완충 수치가 기대와 다릅니다.");
        Assert.AreEqual(1, firearmOutcome.reducedPgByCover, "엄폐 PG 완충 수치가 기대와 다릅니다.");

        var meleeDefender = CreateCoverDefender();
        meleeDefender.SetCover(state.RoundNumber, 1, 2);
        var melee = CreateCoverAttacker(SrpWeaponClass.Melee, 2, 3, 8, 1);
        var meleeOutcome = SrpCombatResolver.ApplyAttack(state, melee, meleeDefender);
        Assert.IsFalse(meleeOutcome.coverBufferApplied, "근접 공격에 엄폐 완충이 적용되었습니다.");

        var magicDefender = CreateCoverDefender();
        magicDefender.SetCover(state.RoundNumber, 1, 2);
        var magic = CreateCoverAttacker(SrpWeaponClass.Magic, 2, 0, 8, 3);
        var magicOutcome = SrpCombatResolver.ApplyAttack(state, magic, magicDefender);
        Assert.AreEqual(SrpBasicAttackKind.Firearm, magicOutcome.basicAttackKind, "비인접 기본 공격은 역할과 무관하게 총기 kind여야 합니다.");
        Assert.IsTrue(magicOutcome.coverBufferApplied, "비인접 기본 공격에는 총기 엄폐 완충이 적용되어야 합니다.");

        var executionDefender = CreateCoverDefender();
        executionDefender.pg = 0;
        executionDefender.groggy = true;
        executionDefender.SetCover(state.RoundNumber, 1, 2);
        var executionMelee = CreateCoverAttacker(SrpWeaponClass.Melee, 2, 3, 8, 1);
        var executionOutcome = SrpCombatResolver.ApplyAttack(state, executionMelee, executionDefender);
        Assert.IsTrue(executionOutcome.wasExecution, "인접 근접 처단 입력이 처단으로 판정되지 않았습니다.");
        Assert.IsFalse(executionOutcome.coverBufferApplied, "처단 공격에 엄폐 완충이 적용되었습니다.");
    }

    [Test]
    public void FirearmSpillover_UsesPostCoverFinalHpDamage()
    {
        var state = CreateDirectionalCoverTestState();
        state.CoverSegments[0].coverDef = 3;
        state.CoverSegments[0].coverGrd = 0;
        var defender = CreateCoverDefender();
        defender.SetCover(state.RoundNumber, 2, 2);
        state.Units.Add(defender);
        var firearm = CreateCoverAttacker(SrpWeaponClass.Firearm, 2, 4, 15, 4);

        var outcome = SrpCombatResolver.ApplyAttack(state, firearm, defender);

        Assert.IsTrue(outcome.coverBufferApplied, "방향성 엄폐가 적용되지 않았습니다.");
        Assert.AreEqual(33, outcome.damageToHp, "엄폐 후 최종 HP 피해가 기대와 다릅니다.");
        Assert.AreEqual(16, outcome.firearmPgSpillover, "총기 PG 파급이 엄폐 후 최종 HP 피해의 50%로 계산되지 않았습니다.");
        Assert.AreEqual(15, outcome.damageToPg, "엄폐 GRD가 총기 PG 파급에 적용되지 않았습니다.");
    }

    [Test]
    public void CoverBuffer_AppliesToOverwatchFire()
    {
        var state = CreateCoverTestState();
        var watcher = CreateCoverAttacker(SrpWeaponClass.Firearm, 2, 0, 8, 4);
        watcher.actionPoints = 2;
        watcher.reactionPoints = 1;
        watcher.maxAmmo = 2;
        watcher.ammo = 2;
        var target = CreateCoverDefender();
        target.SetCover(state.RoundNumber, 1, 2);
        state.Units.Add(watcher);
        state.Units.Add(target);

        Assert.IsTrue(SrpOverwatch.Arm(state, watcher), "오버워치 예약 실패");
        Assert.IsTrue(SrpOverwatch.TryTrigger(state, watcher, target, out var outcome), "오버워치 발동 실패");
        Assert.IsTrue(outcome.coverBufferApplied, "오버워치 사격에 엄폐 완충이 적용되지 않았습니다.");
    }

    [Test]
    public void CoverSegment_AppliesOnlyFromProtectedDirection()
    {
        var state = CreateDirectionalCoverTestState();
        var defender = CreateCoverDefender();
        defender.SetCover(state.RoundNumber, 2, 2);
        state.Units.Add(defender);

        var northFirearm = CreateCoverAttacker(SrpWeaponClass.Firearm, 2, 4, 15, 4);
        var northOutcome = SrpCombatResolver.ApplyAttack(state, northFirearm, defender);
        Assert.IsTrue(northOutcome.coverBufferApplied, "보호 edge 방향 총기 공격에 방향성 엄폐가 적용되지 않았습니다.");
        Assert.AreEqual(4, northOutcome.reducedHpByCover, "segment coverDef가 엄폐 HP 완충에 반영되지 않았습니다.");
        Assert.AreEqual(2, northOutcome.reducedPgByCover, "segment coverGrd가 엄폐 PG 완충에 반영되지 않았습니다.");

        var southDefender = CreateCoverDefender();
        southDefender.SetCover(state.RoundNumber, 2, 2);
        var southFirearm = CreateCoverAttacker(SrpWeaponClass.Firearm, 2, 0, 8, 4);
        var southOutcome = SrpCombatResolver.ApplyAttack(state, southFirearm, southDefender);
        Assert.IsFalse(southOutcome.coverBufferApplied, "반대 방향 총기 공격에 방향성 엄폐가 적용되었습니다.");

        var meleeDefender = CreateCoverDefender();
        meleeDefender.SetCover(state.RoundNumber, 2, 2);
        var melee = CreateCoverAttacker(SrpWeaponClass.Melee, 2, 3, 8, 1);
        var meleeOutcome = SrpCombatResolver.ApplyAttack(state, melee, meleeDefender);
        Assert.IsFalse(meleeOutcome.coverBufferApplied, "근접 공격에 방향성 엄폐가 적용되었습니다.");

        var magicDefender = CreateCoverDefender();
        magicDefender.SetCover(state.RoundNumber, 2, 2);
        var magic = CreateCoverAttacker(SrpWeaponClass.Magic, 2, 4, 8, 3);
        var magicOutcome = SrpCombatResolver.ApplyAttack(state, magic, magicDefender);
        Assert.AreEqual(SrpBasicAttackKind.Firearm, magicOutcome.basicAttackKind, "비인접 기본 공격은 역할과 무관하게 총기 kind여야 합니다.");
        Assert.IsTrue(magicOutcome.coverBufferApplied, "비인접 기본 공격에는 방향성 엄폐가 적용되어야 합니다.");

        var executionDefender = CreateCoverDefender();
        executionDefender.pg = 0;
        executionDefender.groggy = true;
        executionDefender.SetCover(state.RoundNumber, 2, 2);
        var executionMelee = CreateCoverAttacker(SrpWeaponClass.Melee, 2, 3, 8, 1);
        var executionOutcome = SrpCombatResolver.ApplyAttack(state, executionMelee, executionDefender);
        Assert.IsTrue(executionOutcome.wasExecution, "인접 근접 처단 입력이 처단으로 판정되지 않았습니다.");
        Assert.IsFalse(executionOutcome.coverBufferApplied, "처단 공격에 방향성 엄폐가 적용되었습니다.");
    }

    [Test]
    public void InteractionPoint_FindsAdjacentAvailablePoint()
    {
        var state = CreateInteractionTestState(requiredOwner: -1);
        var unit = state.Units[0];

        Assert.IsTrue(state.TryGetAdjacentInteraction(unit, out var point), "인접 상호작용 포인트를 찾지 못했습니다.");
        Assert.AreEqual("lever", point.id);
        Assert.AreEqual(2, point.x);
        Assert.AreEqual(1, point.y);
    }

    [Test]
    public void InteractionAction_SpendsApActivatesAndClaimsOwner()
    {
        var state = CreateInteractionTestState(requiredOwner: 0);
        var unit = state.Units[0];
        unit.actionPoints = 2;

        Assert.IsTrue(state.TryResolveInteractionAction(unit, out var point), "상호작용 실행 실패");
        Assert.AreEqual(1, unit.actionPoints, "상호작용 AP 1 소비가 적용되지 않았습니다.");
        Assert.IsTrue(point.activated, "상호작용 포인트 활성화 상태가 갱신되지 않았습니다.");
        Assert.AreEqual(unit.owner, point.owner, "상호작용 포인트 소유자가 실행 유닛 owner로 갱신되지 않았습니다.");
        Assert.IsFalse(state.TryGetAdjacentInteraction(unit, out _), "singleUse 활성 포인트가 다시 상호작용 가능으로 판정되었습니다.");
    }

    [Test]
    public void InteractionPoint_BlocksOwnerMismatch()
    {
        var state = CreateInteractionTestState(requiredOwner: 1);
        var unit = state.Units[0];

        Assert.IsFalse(state.TryGetAdjacentInteraction(unit, out _), "requiredOwner가 다른 포인트가 상호작용 가능으로 판정되었습니다.");
        Assert.IsFalse(state.TryResolveInteractionAction(unit, out _), "requiredOwner가 다른 포인트가 실행되었습니다.");
    }

    [Test]
    public void InteractionPoint_CloneCopiesStateIndependently()
    {
        var state = CreateInteractionTestState(requiredOwner: -1);
        var unit = state.Units[0];
        Assert.IsTrue(state.TryResolveInteractionAction(unit, out var point), "상호작용 실행 실패");

        var clone = state.Clone();
        clone.InteractionPoints[0].activated = false;
        clone.InteractionPoints[0].owner = -1;

        Assert.IsTrue(point.activated, "클론의 상호작용 상태 변경이 원본에 전파되었습니다.");
        Assert.AreEqual(unit.owner, point.owner, "원본 상호작용 owner가 보존되지 않았습니다.");
        Assert.IsFalse(clone.InteractionPoints[0].activated, "클론 상호작용 상태 변경이 적용되지 않았습니다.");
    }

    [Test]
    public void SkillCharges_BlockUse_WhenNoChargesRemain()
    {
        var skill = CreateChargedSkill();
        var runtime = new SrpSkillRuntime(skill.id)
        {
            chargesRemaining = 0,
            chargesInitialized = true,
        };

        Assert.IsFalse(SrpSkills.CanUseActiveSkill(skill, runtime), "충전이 0인 스킬이 사용 가능으로 판정되었습니다.");
    }

    [Test]
    public void SkillUse_ConsumesChargeAndAppliesCooldown()
    {
        var state = SrpBattleState.FromMap(SrpDefaultMaps.GetPreset(SrpMapPreset.M1QaIntegrated));
        var caster = state.Units[0];
        var skill = CreateChargedSkill();
        var runtime = new SrpSkillRuntime(skill.id)
        {
            chargesRemaining = 2,
            chargesInitialized = true,
        };

        SrpSkills.ResolveActiveSkill(skill, runtime, caster, caster.anchorX, caster.anchorY, state, null);

        Assert.AreEqual(1, runtime.chargesRemaining, "스킬 사용 후 충전이 감소하지 않았습니다.");
        Assert.AreEqual(skill.cooldown, runtime.cooldownRemaining, "스킬 사용 후 쿨다운이 설정되지 않았습니다.");
        Assert.Greater(runtime.chargeRecoveryRemaining, 0, "충전 회복 타이머가 시작되지 않았습니다.");
    }

    [Test]
    public void SkillResourceTick_ReducesCooldownAndRestoresCharge()
    {
        var skill = CreateChargedSkill();
        var runtime = new SrpSkillRuntime(skill.id)
        {
            cooldownRemaining = 2,
            chargesRemaining = 0,
            chargeRecoveryRemaining = 1,
            chargesInitialized = true,
        };

        SrpSkills.TickSkillResources(skill, runtime);

        Assert.AreEqual(1, runtime.cooldownRemaining, "스킬 자원 틱이 쿨다운을 감소시키지 않았습니다.");
        Assert.AreEqual(1, runtime.chargesRemaining, "스킬 자원 틱이 충전을 회복하지 않았습니다.");
    }

    [Test]
    public void SkillOverclock_SpendsFrozenHeartAndRestoresSkillResource()
    {
        var caster = new SrpUnitRuntime
        {
            displayName = "Caster",
            frozenHeart = 10,
        };
        var skill = CreateChargedSkill();
        skill.overclockFrozenHeartCost = 5;
        skill.overclockCooldownReduction = 2;
        skill.overclockChargeRestore = 1;
        var runtime = new SrpSkillRuntime(skill.id)
        {
            cooldownRemaining = 3,
            chargesRemaining = 0,
            chargesInitialized = true,
        };

        bool applied = SrpSkills.TryOverclockSkill(caster, skill, runtime, null);

        Assert.IsTrue(applied, "오버클럭이 적용되지 않았습니다.");
        Assert.AreEqual(5, caster.frozenHeart, "오버클럭이 FH 비용을 소비하지 않았습니다.");
        Assert.AreEqual(1, runtime.cooldownRemaining, "오버클럭이 쿨다운을 단축하지 않았습니다.");
        Assert.AreEqual(1, runtime.chargesRemaining, "오버클럭이 충전을 복구하지 않았습니다.");
    }

    [Test]
    public void SkillOverclockPowerBonus_EnhancesNextActiveSkillUseOnce()
    {
        var state = SrpBattleState.FromMap(SrpDefaultMaps.GetPreset(SrpMapPreset.M1QaIntegrated));
        var caster = state.Units[0];
        caster.displayName = "Caster";
        caster.hp = 10;
        caster.maxHp = 30;
        caster.frozenHeart = 5;
        var skill = new SrpSkillData
        {
            id = "overclock_heal",
            displayName = "Overclock Heal",
            skillType = SrpSkillType.Active,
            trigger = SrpSkillTrigger.OnActivate,
            targetType = SrpTargetType.Self,
            overclockFrozenHeartCost = 5,
            overclockPowerBonus = 4,
            effects = new[]
            {
                new SrpSkillEffect
                {
                    type = SrpEffectType.Heal,
                    stat = "hp",
                    value = 3,
                },
            },
        };
        var runtime = new SrpSkillRuntime(skill.id);

        Assert.IsTrue(SrpSkills.CanOverclockSkill(caster, skill, runtime), "성능 증폭만 있는 스킬을 오버클럭할 수 없습니다.");
        Assert.IsTrue(SrpSkills.TryOverclockSkill(caster, skill, runtime, null), "성능 증폭 오버클럭 실행 실패");
        Assert.AreEqual(1, runtime.overclockedUsesRemaining, "오버클럭 강화 대기 상태가 설정되지 않았습니다.");

        SrpSkills.ResolveActiveSkill(skill, runtime, caster, caster.anchorX, caster.anchorY, state, null);

        Assert.AreEqual(17, caster.hp, "오버클럭 위력 보너스가 다음 회복에 적용되지 않았습니다.");
        Assert.AreEqual(0, runtime.overclockedUsesRemaining, "오버클럭 강화 상태가 1회 사용 후 소모되지 않았습니다.");
    }

    [Test]
    public void ApplyCombatTagSkill_AddsRuntimeTagToTarget()
    {
        var state = SrpBattleState.FromMap(SrpDefaultMaps.GetPreset(SrpMapPreset.M1QaIntegrated));
        var caster = FindUnit(state, "mage", 0);
        var target = FindUnit(state, "vanguard", 1);
        var skill = state.SkillLookup["tactical_mark"];
        var runtime = new SrpSkillRuntime(skill.id);

        SrpSkills.ResolveActiveSkill(skill, runtime, caster, target.anchorX, target.anchorY, state, null);

        Assert.IsTrue(target.HasCombatTag(SrpCombatTag.Marked), "전술 표식 스킬이 대상 런타임 전투 태그를 부여하지 않았습니다.");
        Assert.AreEqual(skill.cooldown, runtime.cooldownRemaining, "태그 스킬 사용 후 쿨다운이 설정되지 않았습니다.");
    }

    [Test]
    public void InitialFourRolePassives_ApplyBridgeEffects()
    {
        var state = SrpBattleState.FromMap(SrpDefaultMaps.GetPreset(SrpMapPreset.M1QaIntegrated));
        var hero = FindUnit(state, "breaker", 0);
        var tank = FindUnit(state, "vanguard", 0);
        var rifleman = FindUnit(state, "rifleman", 0);
        var mage = FindUnit(state, "mage", 0);
        var target = FindUnit(state, "vanguard", 1);

        Assert.IsNotNull(hero, "주인공 역할 유닛이 QA 프리셋에 없습니다.");
        Assert.IsNotNull(tank, "탱커 역할 유닛이 QA 프리셋에 없습니다.");
        Assert.IsNotNull(rifleman, "사격수 역할 유닛이 QA 프리셋에 없습니다.");
        Assert.IsNotNull(mage, "마도사 역할 유닛이 QA 프리셋에 없습니다.");
        Assert.IsTrue(hero.HasTag(SrpUnitTags.ParryUser), "주인공 고유 패링 태그가 없습니다.");
        Assert.IsTrue(tank.HasTag(SrpUnitTags.Tank), "탱커 고유 Tank 태그가 없습니다.");

        SrpSkills.OnAttackResolved(hero, target, new SrpCombatResolver.AttackOutcome { damageToHp = 1 }, state, null);
        SrpSkills.OnAttackResolved(rifleman, target, new SrpCombatResolver.AttackOutcome { damageToHp = 1 }, state, null);
        tank.pg = 10;
        SrpSkills.OnTakeDamage(tank, state, null);
        SrpSkills.TryApplyPassiveTurnStart(mage, state, null);

        Assert.AreEqual(3, hero.frozenHeart, "주인공 전장 적응 패시브 수치가 적용되지 않았습니다.");
        Assert.AreEqual(2, rifleman.frozenHeart, "사격수 노출 처벌 패시브 수치가 적용되지 않았습니다.");
        Assert.AreEqual(12, tank.pg, "탱커 전열 고정 패시브 PG 회복이 적용되지 않았습니다.");
        Assert.AreEqual(2, mage.frozenHeart, "마도사 전장 해석 패시브 수치가 적용되지 않았습니다.");
    }

    [Test]
    public void ArcaneScreen_RestoresAllyPgAsMagicBattlefieldIntervention()
    {
        var state = SrpBattleState.FromMap(SrpDefaultMaps.GetPreset(SrpMapPreset.M1QaIntegrated));
        var mage = FindUnit(state, "mage", 0);
        var tank = FindUnit(state, "vanguard", 0);
        var skill = state.SkillLookup["arcane_screen"];
        var runtime = new SrpSkillRuntime(skill.id);
        tank.pg = 10;

        SrpSkills.ResolveActiveSkill(skill, runtime, mage, tank.anchorX, tank.anchorY, state, null);

        Assert.AreEqual(14, tank.pg, "전장 장막이 아군 PG 회복을 적용하지 않았습니다.");
        Assert.AreEqual(skill.cooldown, runtime.cooldownRemaining, "전장 장막 사용 후 쿨다운이 설정되지 않았습니다.");
    }

    [Test]
    public void UnitViewFacingRotation_MapsFacingToWedgeForwardDirection()
    {
        AssertFacingForward(SrpFacing.North, Vector3.forward);
        AssertFacingForward(SrpFacing.East, Vector3.right);
        AssertFacingForward(SrpFacing.South, Vector3.back);
        AssertFacingForward(SrpFacing.West, Vector3.left);
    }

    static SrpUnitRuntime CreateDefender()
    {
        return new SrpUnitRuntime
        {
            hp = 40,
            maxHp = 40,
            pg = 24,
            maxPg = 24,
            stance = SrpStance.Defensive,
        };
    }

    static SrpBattleState CreateCoverTestState()
    {
        int width = 5;
        int height = 5;
        var walkable = new bool[width * height];
        for (int i = 0; i < walkable.Length; i++)
            walkable[i] = true;
        walkable[1 + 2 * width] = false;

        return SrpBattleState.FromMap(new SrpMapFileV1
        {
            width = width,
            height = height,
            walkable = walkable,
            playerOrder = new[] { 0, 1 },
            coverObjects = new[]
            {
                new SrpCoverObjectData
                {
                    x = 1,
                    y = 2,
                    coverDef = 2,
                    coverGrd = 1,
                    blocksLineOfSight = true,
                    visualKey = "test_blocked_cover",
                },
            },
        });
    }

    static SrpBattleState CreateDirectionalCoverTestState()
    {
        int width = 5;
        int height = 5;
        var walkable = new bool[width * height];
        for (int i = 0; i < walkable.Length; i++)
            walkable[i] = true;

        return SrpBattleState.FromMap(new SrpMapFileV1
        {
            width = width,
            height = height,
            walkable = walkable,
            playerOrder = new[] { 0, 1 },
            coverSegments = new[]
            {
                new SrpCoverSegmentData
                {
                    x = 2,
                    y = 2,
                    edge = SrpCoverEdge.North,
                    shape = SrpCoverShape.Linear,
                    coverDef = 4,
                    coverGrd = 2,
                    blocksLineOfSight = false,
                },
            },
        });
    }

    static SrpUnitRuntime CreateCoverDefender()
    {
        return new SrpUnitRuntime
        {
            id = 201,
            owner = 1,
            anchorX = 2,
            anchorY = 2,
            hp = 40,
            maxHp = 40,
            pg = 24,
            maxPg = 24,
            reactionPoints = 0,
            weaponClass = SrpWeaponClass.Melee,
            stance = SrpStance.Aggressive,
        };
    }

    static SrpUnitRuntime CreateCoverAttacker(SrpWeaponClass weaponClass, int x, int y, int attackPower, int attackRange)
    {
        return new SrpUnitRuntime
        {
            id = 200,
            owner = 0,
            anchorX = x,
            anchorY = y,
            hp = 40,
            maxHp = 40,
            pg = 24,
            maxPg = 24,
            weaponClass = weaponClass,
            stance = SrpStance.Defensive,
            attackPower = attackPower,
            attackRange = attackRange,
            maxAmmo = 1,
            ammo = 1,
        };
    }

    static SrpBattleState CreateInteractionTestState(int requiredOwner)
    {
        int width = 4;
        int height = 4;
        var walkable = new bool[width * height];
        for (int i = 0; i < walkable.Length; i++)
            walkable[i] = true;

        return SrpBattleState.FromMap(new SrpMapFileV1
        {
            width = width,
            height = height,
            walkable = walkable,
            playerOrder = new[] { 0, 1 },
            templates = new[]
            {
                new SrpUnitTemplateData
                {
                    id = "actor",
                    displayName = "Actor",
                    maxActionPoints = 2,
                    maxReactionPoints = 1,
                },
            },
            placements = new[]
            {
                new SrpPlacementData { templateId = "actor", owner = 0, x = 1, y = 1 },
            },
            interactionPoints = new[]
            {
                new SrpInteractionPointData
                {
                    id = "lever",
                    displayName = "Lever",
                    x = 2,
                    y = 1,
                    owner = -1,
                    requiredOwner = requiredOwner,
                    singleUse = true,
                    activated = false,
                },
            },
        });
    }

    static SrpSkillData CreateChargedSkill()
    {
        return new SrpSkillData
        {
            id = "charged_test",
            displayName = "Charged Test",
            skillType = SrpSkillType.Active,
            trigger = SrpSkillTrigger.OnActivate,
            targetType = SrpTargetType.Self,
            cooldown = 2,
            maxCharges = 2,
            chargeRecoveryTurns = 1,
            effects = new[]
            {
                new SrpSkillEffect
                {
                    type = SrpEffectType.Heal,
                    stat = "hp",
                    value = 1,
                },
            },
        };
    }

    static void AssertFacingForward(SrpFacing facing, Vector3 expected)
    {
        Vector3 actual = SrpGameController.GetFacingRotation(facing) * Vector3.forward;
        Assert.Less(Vector3.Distance(expected, actual), 0.001f, $"{facing} 유닛 뷰 전방 방향 불일치");
    }

    static SrpUnitRuntime FindUnit(SrpBattleState state, int id)
    {
        foreach (var unit in state.Units)
            if (unit.id == id)
                return unit;
        return null;
    }

    static SrpUnitRuntime FindUnit(SrpBattleState state, string templateId, int owner)
    {
        foreach (var unit in state.Units)
            if (unit.templateId == templateId && unit.owner == owner)
                return unit;
        return null;
    }
}
