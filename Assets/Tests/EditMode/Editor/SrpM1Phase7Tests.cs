using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 7차 테스트: 6차에서 남긴 결함 두 건(메이커 저장 시 템플릿 누락, 전장 장막 오버클럭)과 적 AI 확장.
/// 뒤쪽은 7차에서 받은 기획 결정(Q4 교전 인접, Q6 패링 대상, Q13 승리 조건 확장, Q15 AI 경계태세)을 고정한다.
/// </summary>
[Category("SrpM1All")]
public class SrpM1Phase7Tests
{
    // ── 6차에서 남긴 결함 ────────────────────────────────────────────────────

    [TestCase(TestName = "메이커 저장은 유닛 DB에 없는 템플릿을 불러온 맵에서 가져와 유지한다")]
    public void MakerSave_KeepsTemplatesMissingFromUnitDb()
    {
        // 방벽선 돌파의 적 템플릿(barricade_*)은 기본 유닛 DB에 없다.
        var loaded = SrpDefaultMaps.GetPreset(SrpMapPreset.M1BarricadeAssault);
        var carry = SrpMapCarryOver.Capture(loaded);
        var unitDb = SrpDefaultUnits.Create();
        var placedIds = new List<string>();
        foreach (var placement in loaded.placements)
            placedIds.Add(placement.templateId);

        // 메이커 저장처럼 맵 객체를 새로 만든다.
        var rebuilt = new SrpMapFileV1
        {
            width = loaded.width,
            height = loaded.height,
            walkable = (bool[])loaded.walkable.Clone(),
            playerOrder = new[] { 0, 1 },
            templates = carry.ResolveTemplates(placedIds, unitDb),
            placements = loaded.placements,
        };
        carry.ApplyTo(rebuilt);

        var templateIds = new List<string>();
        foreach (var template in rebuilt.templates)
            templateIds.Add(template.id);
        Assert.That(templateIds, Has.Member("barricade_captain"), "유닛 DB에 없는 템플릿이 저장에서 빠졌습니다.");
        Assert.That(templateIds, Has.Member("barricade_sniper"));
        Assert.That(templateIds, Is.Unique, "같은 템플릿이 두 번 들어갔습니다.");

        // 유닛 DB에 있는 템플릿은 지금까지처럼 DB 값을 쓴다(맵에 들어 있던 값이 아니라).
        SrpUnitTemplateData dbVanguard = null;
        foreach (var unit in unitDb)
        {
            if (unit.id == "vanguard")
                dbVanguard = unit;
        }
        foreach (var template in rebuilt.templates)
        {
            if (template.id == "vanguard")
                Assert.That(template.maxHp, Is.EqualTo(dbVanguard.maxHp));
        }

        // 저장 형식을 거쳐 전투에 올리면 배치한 유닛이 모두 나온다.
        var restored = JsonUtility.FromJson<SrpMapFileV1>(JsonUtility.ToJson(rebuilt));
        var state = SrpBattleState.FromMap(restored);
        Assert.That(state.Units.Count, Is.EqualTo(loaded.placements.Length), "저장한 맵에서 유닛이 빠졌습니다.");
    }

    [TestCase(TestName = "메이커 저장은 배치에 쓰이지 않은 템플릿을 넣지 않고, 새 맵에서는 유닛 DB만 쓴다")]
    public void MakerSave_ResolveTemplatesUsesOnlyPlacedIds()
    {
        var unitDb = SrpDefaultUnits.Create();

        // 새 맵(불러온 맵 없음): DB에 있는 것만 들어가고, 모르는 id는 건너뛴다.
        var fresh = new SrpMapCarryOver().ResolveTemplates(new[] { "mage", "mage", "unknown_unit", null }, unitDb);
        Assert.That(fresh.Length, Is.EqualTo(1));
        Assert.That(fresh[0].id, Is.EqualTo("mage"));

        // 불러온 맵에 있던 템플릿이라도 배치에 쓰이지 않으면 저장하지 않는다.
        var carry = SrpMapCarryOver.Capture(SrpDefaultMaps.GetPreset(SrpMapPreset.M1BarricadeAssault));
        var onlyCaptain = carry.ResolveTemplates(new[] { "barricade_captain" }, unitDb);
        Assert.That(onlyCaptain.Length, Is.EqualTo(1));
        Assert.That(onlyCaptain[0].id, Is.EqualTo("barricade_captain"));

        Assert.That(new SrpMapCarryOver().ResolveTemplates(null, null).Length, Is.EqualTo(0));
    }

    [TestCase(TestName = "전장 장막을 오버클럭하면 PG 회복량에 강화가 붙고 한 번 쓰면 사라진다")]
    public void ArcaneScreen_OverclockPowerBonusAppliesToPgRecovery()
    {
        var state = SrpBattleState.FromMap(SrpDefaultMaps.GetPreset(SrpMapPreset.M1QaIntegrated));
        var mage = FindUnit(state, "mage", 0);
        var tank = FindUnit(state, "vanguard", 0);
        var skill = state.SkillLookup["arcane_screen"];
        var runtime = FindRuntime(mage, skill.id);
        mage.frozenHeart = skill.overclockFrozenHeartCost;
        tank.pg = 10;

        Assert.That(SrpSkills.TryOverclockSkill(mage, skill, runtime, null), Is.True, "전장 장막을 오버클럭할 수 없습니다.");
        SrpSkills.ResolveActiveSkill(skill, runtime, mage, tank.anchorX, tank.anchorY, state, null);

        int baseRecovery = skill.effects[0].value;
        Assert.That(tank.pg, Is.EqualTo(10 + baseRecovery + skill.overclockPowerBonus),
            "오버클럭 강화가 PG 회복에 적용되지 않았습니다.");
        Assert.That(runtime.overclockedUsesRemaining, Is.EqualTo(0), "오버클럭 강화가 소모되지 않았습니다.");

        // 강화는 한 번만 적용된다. 다음 사용은 기준 회복량이다.
        runtime.cooldownRemaining = 0;
        runtime.chargesRemaining = 1;
        tank.pg = 10;
        SrpSkills.ResolveActiveSkill(skill, runtime, mage, tank.anchorX, tank.anchorY, state, null);
        Assert.That(tank.pg, Is.EqualTo(10 + baseRecovery));
    }

    [TestCase(TestName = "오버클럭 강화는 회복이 아닌 능력치 증감과 패시브 회복에는 붙지 않는다")]
    public void OverclockPowerBonus_DoesNotChangeNonRecoveryStatEffects()
    {
        var state = CreateState();
        var caster = Unit(id: 1, owner: 0, x: 0, y: 0);
        var enemy = Unit(id: 2, owner: 1, x: 1, y: 0);
        enemy.reactionPoints = 1;
        enemy.maxReactionPoints = 1;
        enemy.attackPower = 10;
        state.Units.Add(caster);
        state.Units.Add(enemy);

        var skill = new SrpSkillData
        {
            id = "oc_debuff",
            skillType = SrpSkillType.Active,
            trigger = SrpSkillTrigger.OnActivate,
            targetType = SrpTargetType.SingleEnemy,
            range = 1,
            overclockFrozenHeartCost = 1,
            overclockPowerBonus = 3,
            effects = new[]
            {
                new SrpSkillEffect { type = SrpEffectType.DebuffStat, stat = "attackPower", value = 2 },
                new SrpSkillEffect { type = SrpEffectType.BuffStat, stat = "moveRange", value = 1 },
            },
        };
        var runtime = new SrpSkillRuntime(skill.id);
        caster.frozenHeart = 1;
        enemy.moveRange = 3;
        Assert.That(SrpSkills.TryOverclockSkill(caster, skill, runtime, null), Is.True);

        SrpSkills.ResolveActiveSkill(skill, runtime, caster, 1, 0, state, null);

        Assert.That(enemy.attackPower, Is.EqualTo(8), "회복이 아닌 약화 효과에 오버클럭 강화가 붙었습니다.");
        Assert.That(enemy.moveRange, Is.EqualTo(4), "회복이 아닌 강화 효과에 오버클럭 강화가 붙었습니다.");
    }

    // ── 적 AI: 범위·태그·회복 스킬 ───────────────────────────────────────────

    [TestCase(TestName = "적 AI는 범위 스킬을 적이 가장 많이 드는 칸에 쓰고 아군은 휘말리지 않는다")]
    public void EnemyAi_AreaSkillTargetsTileCoveringMostEnemies()
    {
        var state = CreateState(6, 5);
        var blast = AddSkill(state, new SrpSkillData
        {
            id = "p7_blast",
            skillType = SrpSkillType.Active,
            trigger = SrpSkillTrigger.OnActivate,
            targetType = SrpTargetType.AreaEnemy,
            range = 4,
            areaSize = 1,
            effects = new[] { new SrpSkillEffect { type = SrpEffectType.Damage, value = 6 } },
        });
        var actor = AddUnit(state, id: 1, owner: 1, x: 0, y: 2, actionPoints: 1, skills: new[] { blast.id });
        var ally = AddUnit(state, id: 2, owner: 1, x: 2, y: 2);
        var enemyA = AddUnit(state, id: 3, owner: 0, x: 3, y: 1);
        var enemyB = AddUnit(state, id: 4, owner: 0, x: 4, y: 2);
        var enemyC = AddUnit(state, id: 5, owner: 0, x: 3, y: 3);

        var action = SrpEnemyAi.Decide(state, actor, 0, false);

        Assert.That(action.type, Is.EqualTo(SrpEnemyAiActionType.UseSkill));
        Assert.That(action.skillId, Is.EqualTo(blast.id));
        // 세 적을 모두 덮는 칸은 (3,2)와 (4,2)뿐이고, 동점이면 앞선 칸을 고른다.
        Assert.That((action.x, action.y), Is.EqualTo((3, 2)));
        Assert.That(action.targetUnitId, Is.EqualTo(-1), "범위 스킬은 대상 유닛을 하나로 정하지 않습니다.");

        int remaining = 0;
        var step = SrpBattleFlow.ExecuteAiAction(state, actor, action, ref remaining, null);
        Assert.That(step.executed, Is.True, "AI가 고른 범위 스킬이 실행되지 않았습니다.");
        Assert.That(enemyA.hp, Is.LessThan(30));
        Assert.That(enemyB.hp, Is.LessThan(30));
        Assert.That(enemyC.hp, Is.LessThan(30));
        Assert.That(ally.hp, Is.EqualTo(30), "범위 안의 아군이 피해를 받았습니다.");
    }

    [TestCase(TestName = "적 AI는 AP가 2면 태그 스킬을 먼저 쓰고 같은 대상을 공격해 태그를 바로 소모한다")]
    public void EnemyAi_TagSkillThenAttackConsumesTag()
    {
        var state = CreateState(6, 5);
        var hex = AddSkill(state, TagSkill("p7_hex", "balanceBroken"));
        var actor = AddUnit(state, id: 1, owner: 1, x: 0, y: 0, attackPower: 8, skills: new[] { hex.id });
        // id가 더 낮은 먼 적이 있어도, 지금 공격할 대상에게 태그를 붙여야 한다.
        var far = AddUnit(state, id: 2, owner: 0, x: 3, y: 3);
        var target = AddUnit(state, id: 3, owner: 0, x: 1, y: 0);

        var first = SrpEnemyAi.Decide(state, actor, 0, false);
        Assert.That(first.type, Is.EqualTo(SrpEnemyAiActionType.UseSkill));
        Assert.That(first.targetUnitId, Is.EqualTo(target.id));

        int remaining = 0;
        Assert.That(SrpBattleFlow.ExecuteAiAction(state, actor, first, ref remaining, null).executed, Is.True);
        Assert.That(target.HasCombatTag(SrpCombatTag.BalanceBroken), Is.True);
        Assert.That(far.HasCombatTag(SrpCombatTag.BalanceBroken), Is.False);

        var second = SrpEnemyAi.Decide(state, actor, 0, false);
        Assert.That(second.type, Is.EqualTo(SrpEnemyAiActionType.Attack));
        Assert.That(second.targetUnitId, Is.EqualTo(target.id));

        var attack = SrpBattleFlow.ExecuteAiAction(state, actor, second, ref remaining, null);
        Assert.That(attack.attack.outcome.consumedCombatTags, Is.EqualTo(SrpCombatTag.BalanceBroken),
            "이어지는 공격이 방금 붙인 태그를 소모하지 않았습니다.");
        Assert.That(target.HasCombatTag(SrpCombatTag.BalanceBroken), Is.False);
    }

    [TestCase(TestName = "적 AI는 AP가 1이면 태그보다 공격을 고르고, 이미 붙은 태그를 다시 붙이지 않는다")]
    public void EnemyAi_TagSkillIsNotWastedWhenAttackIsWorthMore()
    {
        var state = CreateState(6, 5);
        var hex = AddSkill(state, TagSkill("p7_hex", "balanceBroken"));
        var actor = AddUnit(state, id: 1, owner: 1, x: 0, y: 0, attackPower: 8, actionPoints: 1, skills: new[] { hex.id });
        var target = AddUnit(state, id: 2, owner: 0, x: 1, y: 0);

        Assert.That(SrpEnemyAi.Decide(state, actor, 0, false).type, Is.EqualTo(SrpEnemyAiActionType.Attack),
            "AP가 1뿐인데 공격 대신 태그 스킬을 골랐습니다.");

        actor.actionPoints = 2;
        target.AddCombatTag(SrpCombatTag.BalanceBroken);
        Assert.That(SrpEnemyAi.Decide(state, actor, 0, false).type, Is.EqualTo(SrpEnemyAiActionType.Attack),
            "이미 태그가 붙은 대상에게 같은 태그 스킬을 썼습니다.");
    }

    [TestCase(TestName = "적 AI는 활성화를 끝내는 피해 스킬을 공격 뒤로 미룬다")]
    public void EnemyAi_EndingSkillIsUsedAfterAttack()
    {
        var state = CreateState(6, 5);
        var actor = AddUnit(state, id: 1, owner: 1, x: 0, y: 0, attackPower: 8, skills: new[] { "cleave" });
        var target = AddUnit(state, id: 2, owner: 0, x: 1, y: 0);

        var first = SrpEnemyAi.Decide(state, actor, 0, false);
        Assert.That(first.type, Is.EqualTo(SrpEnemyAiActionType.Attack));

        int remaining = 0;
        SrpBattleFlow.ExecuteAiAction(state, actor, first, ref remaining, null);
        var second = SrpEnemyAi.Decide(state, actor, 0, true);
        Assert.That(second.type, Is.EqualTo(SrpEnemyAiActionType.UseSkill));
        Assert.That(second.skillId, Is.EqualTo("cleave"));
        Assert.That(second.targetUnitId, Is.EqualTo(target.id));
    }

    [TestCase(TestName = "적 AI는 크게 다친 아군을 이동보다 먼저 회복시키고, 멀쩡한 아군에게는 회복을 쓰지 않는다")]
    public void EnemyAi_HealsBadlyWoundedAllyBeforeMoving()
    {
        var state = CreateState(8, 5);
        var heal = AddSkill(state, new SrpSkillData
        {
            id = "p7_heal",
            skillType = SrpSkillType.Active,
            trigger = SrpSkillTrigger.OnActivate,
            targetType = SrpTargetType.SingleAlly,
            range = 2,
            effects = new[] { new SrpSkillEffect { type = SrpEffectType.Heal, stat = "hp", value = 10 } },
        });
        var healer = AddUnit(state, id: 1, owner: 1, x: 0, y: 0, moveRange: 3, skills: new[] { heal.id });
        var ally = AddUnit(state, id: 2, owner: 1, x: 2, y: 0);
        AddUnit(state, id: 3, owner: 0, x: 7, y: 4);

        // 조금 다친 정도(3)로는 쓰지 않는다. 회복량 10의 3/4 이상이 들어가야 한다.
        ally.hp = 27;
        Assert.That(SrpEnemyAi.Decide(state, healer, 3, false).type, Is.EqualTo(SrpEnemyAiActionType.Move));

        ally.hp = 8;
        var action = SrpEnemyAi.Decide(state, healer, 3, false);
        Assert.That(action.type, Is.EqualTo(SrpEnemyAiActionType.UseSkill));
        Assert.That(action.skillId, Is.EqualTo(heal.id));
        Assert.That(action.targetUnitId, Is.EqualTo(ally.id));

        int remaining = 3;
        Assert.That(SrpBattleFlow.ExecuteAiAction(state, healer, action, ref remaining, null).executed, Is.True);
        Assert.That(ally.hp, Is.EqualTo(18));
        // 스킬은 활성화당 1회다. 남은 AP로는 접근한다.
        Assert.That(SrpEnemyAi.Decide(state, healer, 3, false).type, Is.EqualTo(SrpEnemyAiActionType.Move));
    }

    [TestCase(TestName = "적 AI는 PG가 깎인 아군이 있으면 전열 재정비를 쓰고, 붕괴한 아군만 있으면 쓰지 않는다")]
    public void EnemyAi_UsesAreaPgRecoveryOnlyWhenItRestoresPg()
    {
        var state = CreateState(8, 5);
        var actor = AddUnit(state, id: 1, owner: 1, x: 2, y: 2, moveRange: 3, skills: new[] { "rally_line" });
        var ally = AddUnit(state, id: 2, owner: 1, x: 3, y: 2);
        AddUnit(state, id: 3, owner: 0, x: 7, y: 4);

        // 붕괴(그로기)한 유닛은 PG를 채워도 풀리지 않으므로 회복 대상으로 세지 않는다.
        ally.pg = 0;
        ally.groggy = true;
        Assert.That(SrpEnemyAi.Decide(state, actor, 3, false).type, Is.EqualTo(SrpEnemyAiActionType.Move));

        ally.groggy = false;
        ally.pg = 10;
        var action = SrpEnemyAi.Decide(state, actor, 3, false);
        Assert.That(action.type, Is.EqualTo(SrpEnemyAiActionType.UseSkill));
        Assert.That(action.skillId, Is.EqualTo("rally_line"));
        Assert.That((action.x, action.y), Is.EqualTo((2, 2)), "전열 재정비는 자기 칸을 중심으로 씁니다.");

        int remaining = 3;
        Assert.That(SrpBattleFlow.ExecuteAiAction(state, actor, action, ref remaining, null).executed, Is.True);
        Assert.That(ally.pg, Is.EqualTo(14));
    }

    // ── 적 AI: 엄폐 ──────────────────────────────────────────────────────────

    [TestCase(TestName = "적 AI 사격수는 사격 뒤 옆의 엄폐물에 엄폐하고, 다음 차례에 장전 후 다시 쏜다")]
    public void EnemyAi_ShooterTakesCoverAfterFiring()
    {
        var state = CreateState(7, 5);
        AddCoverObject(state, 2, 1);
        var shooter = AddUnit(state, id: 1, owner: 1, x: 2, y: 2, attackPower: 8, attackRange: 4,
            weaponClass: SrpWeaponClass.Firearm, maxAmmo: 1, ammo: 0, actionPoints: 1);
        var enemy = AddUnit(state, id: 2, owner: 0, x: 5, y: 2, attackPower: 8, attackRange: 4, moveRange: 3,
            weaponClass: SrpWeaponClass.Firearm, maxAmmo: 1, ammo: 1);

        var action = SrpEnemyAi.Decide(state, shooter, 0, true);
        Assert.That(action.type, Is.EqualTo(SrpEnemyAiActionType.TakeCover));
        Assert.That(shooter.coverActive, Is.False, "판단 과정에서 엄폐 상태가 바뀌었습니다.");

        int remaining = 0;
        var step = SrpBattleFlow.ExecuteAiAction(state, shooter, action, ref remaining, null);
        Assert.That(step.executed, Is.True);
        Assert.That((step.coverX, step.coverY), Is.EqualTo((2, 1)));
        Assert.That(shooter.coverActive, Is.True);
        Assert.That(shooter.actionPoints, Is.EqualTo(0));

        var shot = SrpCombatResolver.ApplyAttack(state, enemy, shooter);
        Assert.That(shot.reducedHpByCover, Is.GreaterThan(0), "AI가 건 엄폐가 사격 피해를 줄이지 않았습니다.");

        // 다음 차례: 이미 엄폐 중이므로 장전하고 쏜다.
        shooter.actionPoints = 2;
        Assert.That(SrpEnemyAi.Decide(state, shooter, 3, false).type, Is.EqualTo(SrpEnemyAiActionType.Reload));
        shooter.ReloadAmmo();
        shooter.actionPoints = 1;
        Assert.That(SrpEnemyAi.Decide(state, shooter, 3, false).type, Is.EqualTo(SrpEnemyAiActionType.Attack));
    }

    [TestCase(TestName = "적 AI는 사격해 올 적이 없으면 엄폐에 AP를 쓰지 않는다")]
    public void EnemyAi_DoesNotTakeCoverWithoutRangedThreat()
    {
        var state = CreateState(7, 5);
        AddCoverObject(state, 2, 1);
        var shooter = AddUnit(state, id: 1, owner: 1, x: 2, y: 2, attackPower: 8, attackRange: 4,
            weaponClass: SrpWeaponClass.Firearm, maxAmmo: 1, ammo: 0, actionPoints: 1);
        // 총이 없는 적.
        AddUnit(state, id: 2, owner: 0, x: 5, y: 2, attackRange: 1, moveRange: 3);

        Assert.That(SrpEnemyAi.Decide(state, shooter, 0, true).type, Is.EqualTo(SrpEnemyAiActionType.Reload));
    }

    [TestCase(TestName = "적 AI 근접 유닛은 더 할 행동이 없을 때 남은 AP로 엄폐한다")]
    public void EnemyAi_IdleMeleeUnitTakesCover()
    {
        var state = CreateState(9, 5);
        AddCoverObject(state, 2, 1);
        var melee = AddUnit(state, id: 1, owner: 1, x: 2, y: 2, actionPoints: 1);
        melee.hasMovedThisActivation = true;
        AddUnit(state, id: 2, owner: 0, x: 6, y: 2, attackPower: 8, attackRange: 4, moveRange: 3,
            weaponClass: SrpWeaponClass.Firearm, maxAmmo: 1, ammo: 1);

        Assert.That(SrpEnemyAi.Decide(state, melee, 0, false).type, Is.EqualTo(SrpEnemyAiActionType.TakeCover));

        // 이미 엄폐 중이면 다시 하지 않는다.
        melee.SetCover(state.RoundNumber, 2, 1);
        Assert.That(SrpEnemyAi.Decide(state, melee, 0, false).type, Is.EqualTo(SrpEnemyAiActionType.EndTurn));
    }

    // ── 적 AI: 승리 조건 ─────────────────────────────────────────────────────

    [TestCase(TestName = "적 AI는 승리 목표 옆으로 이동해 상호작용으로 승리한다")]
    public void EnemyAi_MovesToObjectiveAndSecuresIt()
    {
        var state = CreateObjectiveState(7, 5, pointX: 4, pointY: 2, requiredOwner: 1, objectiveRule: true);
        var runner = AddUnit(state, id: 1, owner: 1, x: 0, y: 2, moveRange: 4);
        AddUnit(state, id: 2, owner: 0, x: 6, y: 4);
        SrpBattleFlow.StartRound(state);
        int remaining = SrpBattleFlow.BeginActivation(state, runner, null);

        var move = SrpEnemyAi.Decide(state, runner, remaining, false);
        Assert.That(move.type, Is.EqualTo(SrpEnemyAiActionType.Move));
        Assert.That((move.x, move.y), Is.EqualTo((3, 2)), "이동력 안에서 목표 옆에 설 수 있는 칸으로 가야 합니다.");
        Assert.That(SrpBattleFlow.ExecuteAiAction(state, runner, move, ref remaining, null).executed, Is.True);

        var interact = SrpEnemyAi.Decide(state, runner, remaining, false);
        Assert.That(interact.type, Is.EqualTo(SrpEnemyAiActionType.Interact));
        var step = SrpBattleFlow.ExecuteAiAction(state, runner, interact, ref remaining, null);
        Assert.That(step.executed, Is.True);
        Assert.That(step.interaction.id, Is.EqualTo("p7_point"));

        var victory = SrpVictory.Evaluate(state);
        Assert.That(victory.decided, Is.True);
        Assert.That(victory.winnerOwner, Is.EqualTo(1));
        Assert.That(victory.reason, Is.EqualTo(SrpVictoryReason.ObjectiveSecured));
    }

    [TestCase(TestName = "적 AI는 목표 승리 규칙이 꺼져 있거나 자기 진영이 쓸 수 없는 목표에는 상호작용하지 않는다")]
    public void EnemyAi_IgnoresObjectiveItCannotWinWith()
    {
        // 규칙이 꺼진 맵: 포인트 옆에 서 있어도 상호작용하지 않고 적에게 접근한다.
        var ruleOff = CreateObjectiveState(7, 5, pointX: 1, pointY: 2, requiredOwner: 1, objectiveRule: false);
        var unit = AddUnit(ruleOff, id: 1, owner: 1, x: 0, y: 2, moveRange: 3);
        AddUnit(ruleOff, id: 2, owner: 0, x: 6, y: 4);
        Assert.That(SrpEnemyAi.Decide(ruleOff, unit, 3, false).type, Is.EqualTo(SrpEnemyAiActionType.Move));

        // 상대 진영 전용 목표: 옆에 서 있어도 쓸 수 없다.
        var enemyOnly = CreateObjectiveState(7, 5, pointX: 1, pointY: 2, requiredOwner: 0, objectiveRule: true);
        var blocked = AddUnit(enemyOnly, id: 1, owner: 1, x: 0, y: 2, moveRange: 3);
        AddUnit(enemyOnly, id: 2, owner: 0, x: 6, y: 4);
        Assert.That(SrpEnemyAi.Decide(enemyOnly, blocked, 3, false).type, Is.EqualTo(SrpEnemyAiActionType.Move));
    }

    [TestCase(TestName = "적 AI는 승리 목표에 닿으려는 적을 더 다친 적보다 먼저 공격한다")]
    public void EnemyAi_PrioritizesEnemyThreateningObjective()
    {
        var state = CreateObjectiveState(7, 5, pointX: 4, pointY: 2, requiredOwner: 0, objectiveRule: true);
        var guard = AddUnit(state, id: 1, owner: 1, x: 2, y: 2, attackPower: 8, actionPoints: 1);
        var wounded = AddUnit(state, id: 2, owner: 0, x: 1, y: 2, moveRange: 0);
        wounded.hp = 10;
        var runner = AddUnit(state, id: 3, owner: 0, x: 3, y: 2, moveRange: 3);

        var action = SrpEnemyAi.Decide(state, guard, 0, false);
        Assert.That(action.type, Is.EqualTo(SrpEnemyAiActionType.Attack));
        Assert.That(action.targetUnitId, Is.EqualTo(runner.id), "목표 바로 옆의 적을 두고 다른 적을 공격했습니다.");

        // 목표 승리 규칙이 꺼진 맵에서는 기존대로 더 다친 적을 고른다.
        var ruleOff = CreateObjectiveState(7, 5, pointX: 4, pointY: 2, requiredOwner: 0, objectiveRule: false);
        var guard2 = AddUnit(ruleOff, id: 1, owner: 1, x: 2, y: 2, attackPower: 8, actionPoints: 1);
        var wounded2 = AddUnit(ruleOff, id: 2, owner: 0, x: 1, y: 2, moveRange: 0);
        wounded2.hp = 10;
        AddUnit(ruleOff, id: 3, owner: 0, x: 3, y: 2, moveRange: 3);
        Assert.That(SrpEnemyAi.Decide(ruleOff, guard2, 0, false).targetUnitId, Is.EqualTo(wounded2.id));
    }

    [TestCase(TestName = "적 AI는 지휘관 처치 규칙이 켜진 맵에서 적 지휘관을 먼저 공격한다")]
    public void EnemyAi_PrioritizesEnemyCommander()
    {
        var state = CreateObjectiveState(7, 5, commanderRule: true);
        var actor = AddUnit(state, id: 1, owner: 1, x: 2, y: 2, attackPower: 8, actionPoints: 1);
        var wounded = AddUnit(state, id: 2, owner: 0, x: 1, y: 2);
        wounded.hp = 20;
        var commander = AddUnit(state, id: 3, owner: 0, x: 3, y: 2);
        commander.isCommander = true;

        Assert.That(SrpEnemyAi.Decide(state, actor, 0, false).targetUnitId, Is.EqualTo(commander.id));

        // 규칙이 꺼진 맵에서는 지휘관 표시가 판단에 영향을 주지 않는다.
        var ruleOff = CreateObjectiveState(7, 5, commanderRule: false);
        var actor2 = AddUnit(ruleOff, id: 1, owner: 1, x: 2, y: 2, attackPower: 8, actionPoints: 1);
        var wounded2 = AddUnit(ruleOff, id: 2, owner: 0, x: 1, y: 2);
        wounded2.hp = 20;
        AddUnit(ruleOff, id: 3, owner: 0, x: 3, y: 2).isCommander = true;
        Assert.That(SrpEnemyAi.Decide(ruleOff, actor2, 0, false).targetUnitId, Is.EqualTo(wounded2.id));
    }

    [TestCase(TestName = "적 AI 지휘관은 아군이 남아 있는 동안 앞으로 나서지 않고, 닿는 적은 공격하며, 혼자 남으면 움직인다")]
    public void EnemyAi_CommanderHoldsPositionWhileAlliesRemain()
    {
        var state = CreateObjectiveState(7, 5, commanderRule: true);
        var commander = AddUnit(state, id: 1, owner: 1, x: 0, y: 0, attackPower: 8, moveRange: 3);
        commander.isCommander = true;
        var ally = AddUnit(state, id: 2, owner: 1, x: 0, y: 1);
        var enemy = AddUnit(state, id: 3, owner: 0, x: 6, y: 4);

        Assert.That(SrpEnemyAi.Decide(state, commander, 3, false).type, Is.EqualTo(SrpEnemyAiActionType.EndTurn),
            "지휘관이 공격으로 이어지지 않는 접근 이동을 했습니다.");

        // 이동 후 바로 공격할 수 있는 적은 공격하러 간다.
        enemy.anchorX = 3;
        enemy.anchorY = 0;
        var strike = SrpEnemyAi.Decide(state, commander, 3, false);
        Assert.That(strike.type, Is.EqualTo(SrpEnemyAiActionType.Move));
        Assert.That(System.Math.Max(System.Math.Abs(strike.x - 3), System.Math.Abs(strike.y - 0)), Is.EqualTo(1));

        // 혼자 남으면 고착을 피하려고 평소처럼 접근한다.
        enemy.anchorX = 6;
        enemy.anchorY = 4;
        state.RemoveUnit(ally);
        Assert.That(SrpEnemyAi.Decide(state, commander, 3, false).type, Is.EqualTo(SrpEnemyAiActionType.Move));

        // 지휘관 처치 규칙이 꺼진 맵에서는 지휘관 표시가 있어도 평소처럼 접근한다.
        var ruleOff = CreateObjectiveState(7, 5, commanderRule: false);
        var leader = AddUnit(ruleOff, id: 1, owner: 1, x: 0, y: 0, attackPower: 8, moveRange: 3);
        leader.isCommander = true;
        AddUnit(ruleOff, id: 2, owner: 1, x: 0, y: 1);
        AddUnit(ruleOff, id: 3, owner: 0, x: 6, y: 4);
        Assert.That(SrpEnemyAi.Decide(ruleOff, leader, 3, false).type, Is.EqualTo(SrpEnemyAiActionType.Move));
    }

    // ── AI 행동 실행 규칙 ────────────────────────────────────────────────────

    [TestCase(TestName = "AI 행동 실행은 두 번째 스킬과 조건이 안 맞는 엄폐·상호작용을 거부하고 상태를 바꾸지 않는다")]
    public void ExecuteAiAction_RejectsInvalidSkillCoverAndInteraction()
    {
        var state = CreateState(6, 5);
        var hex = AddSkill(state, TagSkill("p7_hex", "marked"));
        var actor = AddUnit(state, id: 1, owner: 1, x: 0, y: 0, skills: new[] { hex.id, "cleave" });
        var target = AddUnit(state, id: 2, owner: 0, x: 1, y: 0);
        int remaining = 0;

        var cover = SrpBattleFlow.ExecuteAiAction(state, actor, SrpEnemyAiAction.TakeCover(), ref remaining, null);
        Assert.That(cover.executed, Is.False, "옆에 엄폐물이 없는데 엄폐했습니다.");
        var interact = SrpBattleFlow.ExecuteAiAction(state, actor, SrpEnemyAiAction.Interact(), ref remaining, null);
        Assert.That(interact.executed, Is.False, "옆에 상호작용 포인트가 없는데 상호작용했습니다.");
        Assert.That(actor.actionPoints, Is.EqualTo(2));

        // 적 대상 스킬을 빈 칸이나 아군(자기 자신)에게 쓸 수 없다.
        var onEmpty = SrpBattleFlow.ExecuteAiAction(state, actor, SrpEnemyAiAction.UseSkillAt(hex.id, 2, 2), ref remaining, null);
        Assert.That(onEmpty.executed, Is.False);
        var onSelf = SrpBattleFlow.ExecuteAiAction(state, actor, SrpEnemyAiAction.UseSkillAt(hex.id, 0, 0), ref remaining, null);
        Assert.That(onSelf.executed, Is.False);
        Assert.That(actor.actionPoints, Is.EqualTo(2));

        var first = SrpBattleFlow.ExecuteAiAction(state, actor, SrpEnemyAiAction.UseSkill(hex.id, target), ref remaining, null);
        Assert.That(first.executed, Is.True);
        Assert.That(actor.actionPoints, Is.EqualTo(1));

        // 스킬은 활성화당 1회.
        var second = SrpBattleFlow.ExecuteAiAction(state, actor, SrpEnemyAiAction.UseSkill("cleave", target), ref remaining, null);
        Assert.That(second.executed, Is.False);
        Assert.That(actor.actionPoints, Is.EqualTo(1));
        Assert.That(target.hp, Is.EqualTo(30));
    }

    [Category("SrpAiSim")]
    [TestCase(TestName = "AI 시뮬레이션은 방벽선 돌파에서 엄폐와 승리 목표와 지휘관 처치를 실제로 사용한다")]
    public void Simulation_UsesCoverObjectiveAndCommanderKill()
    {
        var config = new SrpBattleSimConfig
        {
            mapPreset = SrpMapPreset.M1BarricadeAssault,
            trials = 60,
            maxRounds = 24,
            baseSeed = 770001,
            sampleSeedCount = 1,
            owner0Policy = new SrpHeuristicAiPolicy(),
            owner1Policy = new SrpRandomAiPolicy(),
            thresholds = SrpSimThresholdConfig.Default(),
        };

        var report = SrpBattleSimRunner.RunBatch(config, writeReport: false).report;

        Debug.Log(
            $"[SRPG][AI-Sim][Phase7] cover={report.actions.coversPerBattle:F2}, " +
            $"interact={report.actions.interactionsPerBattle:F2}, skill={report.actions.skillUsesPerBattle:F2}, " +
            $"annihilation={report.outcome.annihilationRate:F3}, commander={report.outcome.commanderKillRate:F3}, " +
            $"objective={report.outcome.objectiveRate:F3}");
        Assert.That(report.actions.coversPerBattle, Is.GreaterThan(0f), "시뮬레이션이 엄폐를 쓰지 않았습니다.");
        Assert.That(report.actions.interactionsPerBattle, Is.GreaterThan(0f), "시뮬레이션이 승리 목표를 확보하지 않았습니다.");
        Assert.That(report.outcome.objectiveRate, Is.GreaterThan(0f), "목표 확보로 끝난 판이 없습니다.");
        Assert.That(report.outcome.commanderKillRate, Is.GreaterThan(0f), "지휘관 처치로 끝난 판이 없습니다.");
        float decided = report.outcome.annihilationRate + report.outcome.commanderKillRate + report.outcome.objectiveRate;
        Assert.That(decided, Is.EqualTo(1f - report.outcome.drawRate).Within(0.001f), "종료 방식 비율의 합이 승패가 난 판의 비율과 다릅니다.");
    }

    // ── 기획 결정 Q4: 교전 인접은 대각선 포함 ────────────────────────────────

    [TestCase(TestName = "대각선으로 맞닿은 적과도 교전 상태가 된다")]
    public void Engagement_IncludesDiagonalAdjacency()
    {
        var state = CreateState(6, 6);
        var unit = AddUnit(state, id: 1, owner: 0, x: 1, y: 1);
        var enemy = AddUnit(state, id: 2, owner: 1, x: 2, y: 2);

        Assert.That(state.IsUnitEngaged(unit.id), Is.True, "대각선 적과 교전하지 않았습니다.");
        Assert.That(state.GetEngagedEnemyIds(enemy.id), Has.Member(unit.id));
        Assert.That(state.IsEnemyAdjacentToTile(3, 3, 0), Is.True, "적의 대각선 칸이 인접으로 판정되지 않았습니다.");
        Assert.That(state.IsEnemyAdjacentToTile(1, 2, 0), Is.True, "적의 옆 칸이 인접으로 판정되지 않았습니다.");
        Assert.That(state.IsEnemyAdjacentToTile(4, 2, 0), Is.False, "두 칸 떨어진 칸이 인접으로 판정됐습니다.");

        enemy.anchorX = 3;
        enemy.anchorY = 3;
        state.RebuildEngagements();
        Assert.That(state.IsUnitEngaged(unit.id), Is.False, "두 칸 떨어진 적과 교전했습니다.");
    }

    [TestCase(TestName = "대각선 교전에서 빠지면 이탈 비용이 붙고 기회공격을 받는다")]
    public void Move_AwayFromDiagonalEnemyCostsExitAndTriggersOpportunityAttack()
    {
        var state = CreateState(6, 6);
        var mover = AddUnit(state, id: 1, owner: 0, x: 1, y: 1, moveRange: 3);
        var enemy = AddUnit(state, id: 2, owner: 1, x: 2, y: 2, attackPower: 6);
        enemy.reactionPoints = 1;
        int before = mover.hp + mover.pg;

        var costs = SrpPathfinder.GetReachableWithCosts(state, mover, mover.moveRange);
        Assert.That(costs.TryGetValue(new Vector2Int(0, 1), out int exitCost), Is.True);
        Assert.That(exitCost, Is.EqualTo(2), "대각선 교전 이탈 비용(기본 1 + 이탈 1)이 붙지 않았습니다.");
        // 같은 적의 옆 칸으로 도는 이동은 이탈이 아니다(인접 칸 진입 비용만 붙는다).
        Assert.That(costs.TryGetValue(new Vector2Int(1, 2), out int circleCost), Is.True);
        Assert.That(circleCost, Is.EqualTo(2));

        var result = SrpBattleFlow.Move(state, mover, 0, 1, exitCost, null);

        Assert.That(result.disengaged, Is.True);
        Assert.That(result.opportunity.triggered, Is.True, "대각선 교전 이탈에 기회공격이 발동하지 않았습니다.");
        Assert.That(result.opportunity.reactor, Is.SameAs(enemy));
        Assert.That(enemy.reactionPoints, Is.EqualTo(0));
        Assert.That(mover.hp + mover.pg, Is.LessThan(before), "기회공격 피해가 없습니다.");
    }

    [TestCase(TestName = "같은 적의 대각선 칸에서 옆 칸으로 옮기면 교전이 유지되어 기회공격을 받지 않는다")]
    public void Move_AroundSameEnemyKeepsEngagement()
    {
        var state = CreateState(6, 6);
        var mover = AddUnit(state, id: 1, owner: 0, x: 1, y: 1, moveRange: 3);
        var enemy = AddUnit(state, id: 2, owner: 1, x: 2, y: 2, attackPower: 6);
        enemy.reactionPoints = 1;

        var result = SrpBattleFlow.Move(state, mover, 1, 2, 2, null);

        Assert.That(result.disengaged, Is.False);
        Assert.That(result.opportunity.triggered, Is.False);
        Assert.That(enemy.reactionPoints, Is.EqualTo(1));
    }

    // ── 기획 결정 Q6: 패링 대상은 스킬의 isParryable만 본다 ──────────────────

    [TestCase(TestName = "패링은 공격자의 무기 분류와 무관하게 스킬의 패링 가능 표시만으로 판정한다")]
    public void Parry_DependsOnSkillFlagNotWeaponClass()
    {
        var parryable = new SrpSkillData { id = "p7_parryable", isParryable = true, requiresParryTelegraph = true };
        var plain = new SrpSkillData { id = "p7_plain" };

        foreach (var weaponClass in new[] { SrpWeaponClass.Melee, SrpWeaponClass.Firearm, SrpWeaponClass.Magic })
        {
            var state = CreateState(5, 5);
            var defender = AddUnit(state, id: 1, owner: 0, x: 2, y: 2);
            defender.facing = SrpFacing.North;
            defender.tags = (int)SrpUnitTags.ParryUser;
            defender.reactionPoints = 1;
            var attacker = AddUnit(state, id: 2, owner: 1, x: 2, y: 3, attackPower: 6, weaponClass: weaponClass);

            Assert.That(SrpCombatResolver.CanDefenderParry(state, attacker, defender, parryable), Is.True,
                $"{weaponClass} 공격자의 패링 가능 스킬을 패링할 수 없습니다.");
            Assert.That(SrpCombatResolver.CanDefenderParry(state, attacker, defender, plain), Is.False,
                $"{weaponClass} 공격자의 패링 불가 스킬이 패링 가능으로 판정됐습니다.");
            Assert.That(SrpCombatResolver.CanDefenderParry(state, attacker, defender), Is.False,
                $"{weaponClass} 공격자의 기본 공격이 패링 가능으로 판정됐습니다.");

            int before = defender.hp + defender.pg;
            var outcome = SrpCombatResolver.ApplyAttack(state, attacker, defender, parryable);
            Assert.That(outcome.wasParried, Is.True, $"{weaponClass} 공격자의 패링 가능 스킬이 패링되지 않았습니다.");
            Assert.That(defender.hp + defender.pg, Is.EqualTo(before));
        }
    }

    // ── 기획 결정 Q15: 총기 역할은 엄폐 자리에서 경계태세로 버틴다 ───────────

    [TestCase(TestName = "적 AI 사격수는 쏠 대상이 없고 근접 적이 다가오면 전진 대신 엄폐하고 경계태세를 건다")]
    public void EnemyAi_ShooterHoldsCoverWithOverwatchInsteadOfAdvancing()
    {
        var state = CreateHoldState(out var shooter, withMeleeEnemy: true, withCover: true);

        var first = SrpEnemyAi.Decide(state, shooter, shooter.moveRange, false);
        Assert.That(first.type, Is.EqualTo(SrpEnemyAiActionType.TakeCover));
        int remaining = shooter.moveRange;
        Assert.That(SrpBattleFlow.ExecuteAiAction(state, shooter, first, ref remaining, null).executed, Is.True);

        var second = SrpEnemyAi.Decide(state, shooter, remaining, false);
        Assert.That(second.type, Is.EqualTo(SrpEnemyAiActionType.Overwatch));
        Assert.That(SrpBattleFlow.ExecuteAiAction(state, shooter, second, ref remaining, null).executed, Is.True);
        Assert.That(shooter.coverActive, Is.True);
        Assert.That(shooter.overwatchArmed, Is.True);
        Assert.That((shooter.anchorX, shooter.anchorY), Is.EqualTo((2, 2)), "버티기로 했는데 자리를 옮겼습니다.");
    }

    [TestCase(TestName = "적 AI 사격수는 다가올 근접 적이 없거나 엄폐할 곳이 없으면 버티지 않고 전진한다")]
    public void EnemyAi_ShooterAdvancesWhenHoldingHasNoPayoff()
    {
        // 상대가 사격수뿐이면 서로 기다리기만 하게 되므로 전진한다.
        var shootersOnly = CreateHoldState(out var shooter, withMeleeEnemy: false, withCover: true);
        Assert.That(SrpEnemyAi.Decide(shootersOnly, shooter, shooter.moveRange, false).type,
            Is.EqualTo(SrpEnemyAiActionType.Move));

        var noCover = CreateHoldState(out shooter, withMeleeEnemy: true, withCover: false);
        Assert.That(SrpEnemyAi.Decide(noCover, shooter, shooter.moveRange, false).type,
            Is.EqualTo(SrpEnemyAiActionType.Move));

        // 이동해서 바로 쏠 수 있으면 버티기보다 사격이 먼저다.
        var inReach = CreateHoldState(out shooter, withMeleeEnemy: true, withCover: true);
        var melee = FindById(inReach, 3);
        melee.anchorX = 8;
        melee.anchorY = 2;
        inReach.RebuildEngagements();
        Assert.That(SrpEnemyAi.Decide(inReach, shooter, shooter.moveRange, false).type,
            Is.EqualTo(SrpEnemyAiActionType.Move));
    }

    /// <summary>
    /// 버티기 판단용 전장. 사격수(2,2)는 이동해도 이번 차례에 쏠 수 없는 거리에 있고,
    /// 상대 사격수(10,2)는 한 번 움직이면 쏠 수 있어 엄폐가 쓸모 있다. 근접 적(10,3)은 다음 이동으로 사거리에 들어온다.
    /// </summary>
    static SrpBattleState CreateHoldState(out SrpUnitRuntime shooter, bool withMeleeEnemy, bool withCover)
    {
        var state = CreateState(13, 5);
        if (withCover)
            AddCoverObject(state, 2, 1);
        shooter = AddUnit(state, id: 1, owner: 1, x: 2, y: 2, attackPower: 8, attackRange: 4, moveRange: 2,
            weaponClass: SrpWeaponClass.Firearm, maxAmmo: 1, ammo: 1);
        shooter.reactionPoints = 1;
        AddUnit(state, id: 2, owner: 0, x: 10, y: 2, attackPower: 8, attackRange: 5, moveRange: 3,
            weaponClass: SrpWeaponClass.Firearm, maxAmmo: 1, ammo: 1);
        if (withMeleeEnemy)
            AddUnit(state, id: 3, owner: 0, x: 10, y: 3, attackPower: 6, moveRange: 4);
        return state;
    }

    static SrpUnitRuntime FindById(SrpBattleState state, int id)
    {
        foreach (var unit in state.Units)
        {
            if (unit.id == id)
                return unit;
        }
        Assert.Fail($"유닛을 찾지 못했습니다: {id}");
        return null;
    }

    // ── 공용 ─────────────────────────────────────────────────────────────────

    static SrpSkillData AddSkill(SrpBattleState state, SrpSkillData skill)
    {
        state.SkillLookup[skill.id] = skill;
        return skill;
    }

    static SrpSkillData TagSkill(string id, string tag)
    {
        return new SrpSkillData
        {
            id = id,
            skillType = SrpSkillType.Active,
            trigger = SrpSkillTrigger.OnActivate,
            targetType = SrpTargetType.SingleEnemy,
            range = 4,
            effects = new[] { new SrpSkillEffect { type = SrpEffectType.ApplyCombatTag, stat = tag, duration = 1 } },
        };
    }

    static void AddCoverObject(SrpBattleState state, int x, int y)
    {
        Block(state, x, y);
        state.CoverObjects.Add(new SrpCoverObjectData { x = x, y = y, coverDef = 2, coverGrd = 1, blocksLineOfSight = true });
    }

    /// <summary>AI 테스트용 유닛. 총이 없는 근접 유닛이 기본이고, 반응(RP)은 0이라 피해 계산이 단순하다.</summary>
    static SrpUnitRuntime AddUnit(
        SrpBattleState state,
        int id,
        int owner,
        int x,
        int y,
        int attackPower = 0,
        int attackRange = 1,
        int moveRange = 0,
        SrpWeaponClass weaponClass = SrpWeaponClass.Melee,
        int maxAmmo = 0,
        int ammo = 0,
        int actionPoints = 2,
        string[] skills = null)
    {
        var unit = new SrpUnitRuntime
        {
            id = id,
            owner = owner,
            displayName = $"U{id}",
            anchorX = x,
            anchorY = y,
            hp = 30,
            maxHp = 30,
            pg = 20,
            maxPg = 20,
            attackPower = attackPower,
            attackRange = attackRange,
            moveRange = moveRange,
            weaponClass = weaponClass,
            maxAmmo = maxAmmo,
            ammo = ammo,
            actionPoints = actionPoints,
            maxActionPoints = 2,
            reactionPoints = 0,
            maxReactionPoints = 1,
            stance = SrpStance.Defensive,
        };
        if (skills != null)
        {
            foreach (string skillId in skills)
            {
                unit.skillIds.Add(skillId);
                unit.skillRuntimes.Add(new SrpSkillRuntime(skillId));
            }
        }
        state.Units.Add(unit);
        state.RebuildEngagements();
        return unit;
    }

    /// <summary>승리 조건을 켜거나 끌 수 있는 빈 맵. 포인트 좌표를 주면 승리 목표 포인트를 하나 놓는다.</summary>
    static SrpBattleState CreateObjectiveState(
        int width,
        int height,
        int pointX = -1,
        int pointY = -1,
        int requiredOwner = -1,
        bool objectiveRule = false,
        bool commanderRule = false)
    {
        var walk = new bool[width * height];
        for (int i = 0; i < walk.Length; i++)
            walk[i] = true;

        var map = new SrpMapFileV1
        {
            width = width,
            height = height,
            walkable = walk,
            playerOrder = new[] { 0, 1 },
            victory = new SrpVictoryRuleData { commanderKill = commanderRule, interactionObjective = objectiveRule },
        };
        if (pointX >= 0)
        {
            map.interactionPoints = new[]
            {
                new SrpInteractionPointData
                {
                    id = "p7_point",
                    x = pointX,
                    y = pointY,
                    requiredOwner = requiredOwner,
                    singleUse = true,
                    effect = SrpInteractionEffect.VictoryObjective,
                },
            };
        }
        return SrpBattleState.FromMap(map);
    }

    static SrpUnitRuntime FindUnit(SrpBattleState state, string templateId, int owner)
    {
        foreach (var unit in state.Units)
        {
            if (unit.templateId == templateId && unit.owner == owner)
                return unit;
        }
        Assert.Fail($"유닛을 찾지 못했습니다: {templateId} (P{owner})");
        return null;
    }

    static SrpSkillRuntime FindRuntime(SrpUnitRuntime unit, string skillId)
    {
        foreach (var runtime in unit.skillRuntimes)
        {
            if (runtime.skillId == skillId)
                return runtime;
        }
        Assert.Fail($"스킬 런타임을 찾지 못했습니다: {skillId}");
        return null;
    }

    static void Block(SrpBattleState state, int x, int y)
    {
        state.Walkable[state.Index(x, y)] = false;
    }

    static SrpUnitRuntime Unit(
        int id = 0,
        int owner = 0,
        int x = 0,
        int y = 0,
        int hp = 20,
        int pg = 20,
        int attackPower = 0,
        SrpStance stance = SrpStance.Defensive)
    {
        return new SrpUnitRuntime
        {
            id = id,
            owner = owner,
            anchorX = x,
            anchorY = y,
            hp = hp,
            maxHp = hp,
            pg = pg,
            maxPg = pg,
            attackPower = attackPower,
            stance = stance,
            reactionPoints = 0,
        };
    }

    static SrpBattleState CreateState(int width = 5, int height = 5)
    {
        var walk = new bool[width * height];
        for (int i = 0; i < walk.Length; i++)
            walk[i] = true;

        return SrpBattleState.FromMap(new SrpMapFileV1
        {
            width = width,
            height = height,
            walkable = walk,
            playerOrder = new[] { 0, 1 },
        });
    }
}
