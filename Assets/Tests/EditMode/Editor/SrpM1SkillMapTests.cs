using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 5차(스킬·맵 확장) 규칙 테스트: 시야(LOS) 모듈, 범위 스킬, 전투 태그 지속시간, 레거시 효과, 맵 메이커 엄폐 편집.
/// </summary>
[Category("SrpM1All")]
public class SrpM1SkillMapTests
{
    // ── 시야(LOS) 모듈 ────────────────────────────────────────────────────────

    [TestCase(TestName = "시야 규칙 None은 벽 너머도 통한다")]
    public void Sight_NoneIgnoresWalls()
    {
        var state = CreateState();
        Block(state, 2, 2);

        Assert.That(SrpLineOfSight.HasSight(state, 0, 2, 4, 2, SrpSightRule.None), Is.True);
    }

    [TestCase(TestName = "시야 규칙 Terrain은 이동 불가 타일에 막히고 유닛에는 막히지 않는다")]
    public void Sight_TerrainBlockedByWallNotByUnit()
    {
        var state = CreateState();
        state.Units.Add(Unit(id: 9, owner: 1, x: 2, y: 0));
        Assert.That(SrpLineOfSight.HasSight(state, 0, 0, 4, 0, SrpSightRule.Terrain), Is.True, "유닛이 지형 시야를 막았습니다.");

        Block(state, 2, 2);
        Assert.That(SrpLineOfSight.HasSight(state, 0, 2, 4, 2, SrpSightRule.Terrain), Is.False, "벽이 시야를 막지 않았습니다.");
        Assert.That(
            SrpLineOfSight.FindBlockingTile(state, 0, 2, 4, 2, SrpSightRule.Terrain, -1, -1, out var blocker), Is.True);
        Assert.That(blocker, Is.EqualTo(new Vector2Int(2, 2)));
        Assert.That(SrpLineOfSight.HasSight(state, 0, 3, 4, 3, SrpSightRule.Terrain), Is.True, "벽이 없는 줄의 시야가 막혔습니다.");
    }

    [TestCase(TestName = "시야 규칙 TerrainAndUnits는 사선 중간의 유닛에도 막힌다")]
    public void Sight_TerrainAndUnitsBlockedByUnitBetween()
    {
        var state = CreateState();
        state.Units.Add(Unit(id: 9, owner: 1, x: 2, y: 0));

        Assert.That(SrpLineOfSight.HasSight(state, 0, 0, 4, 0, SrpSightRule.TerrainAndUnits), Is.False);
        Assert.That(SrpLineOfSight.HasSight(state, 0, 0, 4, 0, SrpSightRule.TerrainAndUnits, 9), Is.True, "무시하도록 넘긴 유닛이 시야를 막았습니다.");
        Assert.That(SrpLineOfSight.HasSight(state, 0, 0, 2, 0, SrpSightRule.TerrainAndUnits), Is.True, "도착 칸의 유닛은 시야를 막지 않습니다.");
    }

    [TestCase(TestName = "LOS 차단 엄폐 segment만 시야를 막는다")]
    public void Sight_BlockedOnlyByLineBlockingCoverSegment()
    {
        var state = CreateState();
        var segment = new SrpCoverSegmentData { x = 2, y = 1, edge = SrpCoverEdge.East, blocksLineOfSight = false };
        state.CoverSegments.Add(segment);
        Assert.That(SrpLineOfSight.HasSight(state, 0, 1, 4, 1, SrpSightRule.Terrain), Is.True, "LOS를 막지 않는 엄폐가 시야를 막았습니다.");

        segment.blocksLineOfSight = true;
        Assert.That(SrpLineOfSight.HasSight(state, 0, 1, 4, 1, SrpSightRule.Terrain), Is.False, "LOS 차단 엄폐가 시야를 막지 않았습니다.");
        Assert.That(SrpLineOfSight.HasSight(state, 4, 1, 0, 1, SrpSightRule.Terrain), Is.False, "반대 방향 시야가 통했습니다.");
    }

    [TestCase(TestName = "인접 칸과 같은 칸은 규칙과 무관하게 시야가 통한다")]
    public void Sight_AdjacentAlwaysPasses()
    {
        var state = CreateState();
        Block(state, 1, 1);

        Assert.That(SrpLineOfSight.HasSight(state, 0, 0, 1, 1, SrpSightRule.TerrainAndUnits), Is.True);
        Assert.That(SrpLineOfSight.HasSight(state, 0, 0, 0, 0, SrpSightRule.TerrainAndUnits), Is.True);
    }

    [TestCase(TestName = "TerrainAndUnits 시야는 총기 조준선과 같은 결과를 낸다")]
    public void Sight_TerrainAndUnitsMatchesFirearmAimLine()
    {
        var state = CreateState();
        Block(state, 2, 2);
        var shooter = Unit(id: 1, owner: 0, x: 0, y: 0);
        state.Units.Add(shooter);
        state.Units.Add(Unit(id: 2, owner: 1, x: 3, y: 0));
        state.CoverSegments.Add(new SrpCoverSegmentData { x = 0, y = 2, edge = SrpCoverEdge.North, blocksLineOfSight = true });

        for (int y = 0; y < state.Height; y++)
        for (int x = 0; x < state.Width; x++)
        {
            int distance = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
            if (distance <= 1 || !state.IsWalkableTile(x, y))
                continue;
            bool aim = SrpFirearmAim.TryBuildAimLine(state, shooter, x, y, 10, -1, out _);
            bool sight = SrpLineOfSight.HasSight(state, 0, 0, x, y, SrpSightRule.TerrainAndUnits, shooter.id);
            Assert.That(sight, Is.EqualTo(aim), $"({x},{y}) 시야 판정이 총기 조준선과 다릅니다.");
        }
    }

    [TestCase(TestName = "스킬 대상 칸은 스킬의 시야 규칙을 따른다")]
    public void SkillTargetTiles_FollowSkillSightRule()
    {
        var state = CreateState();
        Block(state, 2, 2);
        var caster = Unit(id: 1, owner: 0, x: 0, y: 2);
        var hidden = Unit(id: 2, owner: 1, x: 4, y: 2);
        var visible = Unit(id: 3, owner: 1, x: 3, y: 0);
        state.Units.Add(caster);
        state.Units.Add(hidden);
        state.Units.Add(visible);
        var skill = new SrpSkillData
        {
            id = "test_mark",
            skillType = SrpSkillType.Active,
            trigger = SrpSkillTrigger.OnActivate,
            targetType = SrpTargetType.SingleEnemy,
            range = 4,
            sightRule = SrpSightRule.Terrain,
            effects = new[] { new SrpSkillEffect { type = SrpEffectType.ApplyCombatTag, stat = "marked", duration = 1 } },
        };

        var tiles = SrpSkills.GetSkillTargetTiles(skill, caster, state);
        Assert.That(tiles, Has.Member(new Vector2Int(3, 0)));
        Assert.That(tiles, Has.No.Member(new Vector2Int(4, 2)), "벽 뒤의 적이 대상에 들어갔습니다.");
        Assert.That(SrpSkills.IsTileInSkillReach(skill, caster, 4, 2, state), Is.False);

        skill.sightRule = SrpSightRule.None;
        Assert.That(SrpSkills.GetSkillTargetTiles(skill, caster, state), Has.Member(new Vector2Int(4, 2)));
    }

    [TestCase(TestName = "기본 스킬의 시야 규칙과 저장 형식")]
    public void DefaultSkills_SightRulesAndJsonRoundTrip()
    {
        foreach (var skill in SrpDefaultSkills.Create())
        {
            bool enemyTagSkill = skill.id == "tactical_mark" || skill.id == "balance_hex" || skill.id == "kill_order";
            Assert.That(skill.sightRule, Is.EqualTo(enemyTagSkill ? SrpSightRule.Terrain : SrpSightRule.None), skill.id);
        }

        var data = new SrpSkillData { id = "s", sightRule = SrpSightRule.TerrainAndUnits };
        var restored = JsonUtility.FromJson<SrpSkillData>(JsonUtility.ToJson(data));
        Assert.That(restored.sightRule, Is.EqualTo(SrpSightRule.TerrainAndUnits));

        var legacy = JsonUtility.FromJson<SrpSkillData>("{\"id\":\"old\",\"range\":3}");
        Assert.That(legacy.sightRule, Is.EqualTo(SrpSightRule.None), "시야 규칙이 없는 기존 데이터는 시야 불필요로 읽혀야 합니다.");
    }

    [TestCase(TestName = "적 AI는 시야가 막힌 대상에게 스킬을 쓰지 않는다")]
    public void EnemyAiAction_RejectsSkillWithoutSight()
    {
        var state = CreateState();
        Block(state, 2, 2);
        var caster = Unit(id: 1, owner: 1, x: 0, y: 2);
        var target = Unit(id: 2, owner: 0, x: 4, y: 2, hp: 30);
        state.Units.Add(caster);
        state.Units.Add(target);
        var skill = new SrpSkillData
        {
            id = "test_bolt",
            skillType = SrpSkillType.Active,
            trigger = SrpSkillTrigger.OnActivate,
            targetType = SrpTargetType.SingleEnemy,
            range = 4,
            sightRule = SrpSightRule.Terrain,
            effects = new[] { new SrpSkillEffect { type = SrpEffectType.Damage, value = 5 } },
        };
        state.SkillLookup[skill.id] = skill;
        caster.skillIds.Add(skill.id);
        caster.skillRuntimes.Add(new SrpSkillRuntime(skill.id));
        caster.actionPoints = 2;
        int remainingMove = 0;

        var step = SrpBattleFlow.ExecuteAiAction(state, caster, SrpEnemyAiAction.UseSkill(skill.id, target), ref remainingMove, null);

        Assert.That(step.executed, Is.False, "시야가 막힌 대상에게 스킬이 실행됐습니다.");
        Assert.That(target.hp, Is.EqualTo(30));
        Assert.That(caster.actionPoints, Is.EqualTo(2));
    }

    // ── 범위 스킬 ────────────────────────────────────────────────────────────

    [TestCase(TestName = "적 대상 범위 스킬은 범위 안의 적에게만 적용된다")]
    public void AreaEnemySkill_HitsOnlyEnemiesInsideArea()
    {
        var state = CreateState(7, 7);
        var caster = Unit(id: 1, owner: 0, x: 0, y: 3);
        var allyInArea = Unit(id: 2, owner: 0, x: 3, y: 2);
        var enemyCenter = Unit(id: 3, owner: 1, x: 3, y: 3);
        var enemyEdge = Unit(id: 4, owner: 1, x: 4, y: 4);
        var enemyOutside = Unit(id: 5, owner: 1, x: 5, y: 3);
        foreach (var unit in new[] { caster, allyInArea, enemyCenter, enemyEdge, enemyOutside })
            state.Units.Add(unit);
        var skill = AreaSkill(SrpTargetType.AreaEnemy, areaSize: 1, new SrpSkillEffect { type = SrpEffectType.Damage, value = 6 });

        var affected = SrpSkills.GetSkillAffectedUnits(skill, caster, 3, 3, state);
        Assert.That(affected.Count, Is.EqualTo(2));
        Assert.That(affected[0].id, Is.EqualTo(3), "가까운 대상이 먼저 와야 합니다.");
        Assert.That(affected[1].id, Is.EqualTo(4));

        SrpSkills.ResolveActiveSkill(skill, new SrpSkillRuntime(skill.id), caster, 3, 3, state, null);

        Assert.That(enemyCenter.hp, Is.LessThan(20), "범위 중심의 적이 피해를 받지 않았습니다.");
        Assert.That(enemyEdge.hp, Is.LessThan(20), "범위 모서리의 적이 피해를 받지 않았습니다.");
        Assert.That(enemyOutside.hp, Is.EqualTo(20), "범위 밖의 적이 피해를 받았습니다.");
        Assert.That(allyInArea.hp, Is.EqualTo(20), "범위 안의 아군이 피해를 받았습니다.");
        Assert.That(caster.hp, Is.EqualTo(20));
    }

    [TestCase(TestName = "아군 대상 범위 스킬은 시전자를 포함한 범위 안의 아군에게만 적용된다")]
    public void AreaAllySkill_HealsAlliesIncludingCaster()
    {
        var state = CreateState();
        var caster = Unit(id: 1, owner: 0, x: 2, y: 2);
        var ally = Unit(id: 2, owner: 0, x: 3, y: 2);
        var farAlly = Unit(id: 3, owner: 0, x: 4, y: 4);
        var enemy = Unit(id: 4, owner: 1, x: 2, y: 3);
        foreach (var unit in new[] { caster, ally, farAlly, enemy })
        {
            unit.hp = 10;
            state.Units.Add(unit);
        }
        var skill = AreaSkill(SrpTargetType.AreaAlly, areaSize: 1, new SrpSkillEffect { type = SrpEffectType.Heal, stat = "hp", value = 5 });

        SrpSkills.ResolveActiveSkill(skill, new SrpSkillRuntime(skill.id), caster, 2, 2, state, null);

        Assert.That(caster.hp, Is.EqualTo(15));
        Assert.That(ally.hp, Is.EqualTo(15));
        Assert.That(farAlly.hp, Is.EqualTo(10), "범위 밖의 아군이 회복됐습니다.");
        Assert.That(enemy.hp, Is.EqualTo(10), "범위 안의 적이 회복됐습니다.");
    }

    [TestCase(TestName = "범위 스킬의 시전자 대상 효과는 대상 수와 무관하게 한 번만 적용된다")]
    public void AreaSkill_SelfEffectAppliesOnce()
    {
        var state = CreateState();
        var caster = Unit(id: 1, owner: 0, x: 0, y: 2);
        state.Units.Add(caster);
        state.Units.Add(Unit(id: 2, owner: 1, x: 2, y: 2));
        state.Units.Add(Unit(id: 3, owner: 1, x: 3, y: 2));
        state.Units.Add(Unit(id: 4, owner: 1, x: 3, y: 3));
        var skill = AreaSkill(
            SrpTargetType.AreaEnemy,
            areaSize: 1,
            new SrpSkillEffect { type = SrpEffectType.Damage, value = 4 },
            new SrpSkillEffect { type = SrpEffectType.FrozenHeart, stat = "self", value = 3 });

        SrpSkills.ResolveActiveSkill(skill, new SrpSkillRuntime(skill.id), caster, 3, 2, state, null);
        Assert.That(caster.frozenHeart, Is.EqualTo(3), "시전자 효과가 대상 수만큼 반복됐습니다.");

        // 대상이 한 명도 없어도 시전자 효과와 쿨다운은 적용된다.
        var runtime = new SrpSkillRuntime(skill.id);
        SrpSkills.ResolveActiveSkill(skill, runtime, caster, 0, 0, state, null);
        Assert.That(caster.frozenHeart, Is.EqualTo(6));
        Assert.That(runtime.cooldownRemaining, Is.EqualTo(skill.cooldown));
    }

    [TestCase(TestName = "범위 칸은 areaSize 반경의 정사각형이고 맵 밖은 뺀다")]
    public void AreaTiles_AreSquareAndClippedToMap()
    {
        var state = CreateState();
        var area = AreaSkill(SrpTargetType.AreaEnemy, areaSize: 1);
        var single = new SrpSkillData { id = "single", targetType = SrpTargetType.SingleEnemy, areaSize = 2 };

        Assert.That(SrpSkills.GetSkillAreaTiles(area, 2, 2, state).Count, Is.EqualTo(9));
        Assert.That(SrpSkills.GetSkillAreaTiles(area, 0, 0, state).Count, Is.EqualTo(4), "맵 모서리에서 범위가 잘리지 않았습니다.");
        area.areaSize = 0;
        Assert.That(SrpSkills.GetSkillAreaTiles(area, 2, 2, state).Count, Is.EqualTo(1));
        Assert.That(SrpSkills.GetSkillAreaTiles(single, 2, 2, state).Count, Is.EqualTo(1), "단일 대상 스킬은 areaSize를 쓰지 않습니다.");
    }

    [TestCase(TestName = "범위 스킬 사용 결과는 대상 전체의 피해를 합산한다")]
    public void UseSkill_AreaDamageIsSummedAcrossTargets()
    {
        var state = CreateState();
        var caster = Unit(id: 1, owner: 0, x: 0, y: 2);
        caster.actionPoints = 2;
        var first = Unit(id: 2, owner: 1, x: 3, y: 2);
        var second = Unit(id: 3, owner: 1, x: 4, y: 2);
        state.Units.Add(caster);
        state.Units.Add(first);
        state.Units.Add(second);
        var skill = AreaSkill(SrpTargetType.AreaEnemy, areaSize: 1, new SrpSkillEffect { type = SrpEffectType.Damage, value = 6 });

        var result = SrpBattleFlow.UseSkill(state, caster, skill, new SrpSkillRuntime(skill.id), 3, 2, null);

        int total = (20 - first.hp) + (20 - second.hp);
        Assert.That(total, Is.GreaterThan(0));
        Assert.That(result.targetHpDamage, Is.EqualTo(total));
        Assert.That(caster.actionPoints, Is.EqualTo(1));
    }

    // ── 전투 태그 지속시간 ───────────────────────────────────────────────────

    [TestCase(TestName = "전투 태그는 부여한 라운드를 빼고 duration 라운드가 끝날 때 사라진다")]
    public void CombatTag_ExpiresAfterDurationRounds()
    {
        var state = CreateState();
        var caster = Unit(id: 1, owner: 0, x: 0, y: 0);
        var target = Unit(id: 2, owner: 1, x: 2, y: 0);
        state.Units.Add(caster);
        state.Units.Add(target);
        state.RoundNumber = 3;
        var skill = TagSkill("marked", duration: 1);

        SrpSkills.ResolveActiveSkill(skill, new SrpSkillRuntime(skill.id), caster, 2, 0, state, null);
        Assert.That(target.HasCombatTag(SrpCombatTag.Marked), Is.True);
        Assert.That(target.GetCombatTagExpireRound(SrpCombatTag.Marked), Is.EqualTo(4));
        Assert.That(SrpCombatTagUtility.BuildSummary(target, 3), Is.EqualTo("표식(2R)"));

        state.RoundNumber = 4;
        SrpBattleFlow.StartRound(state);
        Assert.That(target.HasCombatTag(SrpCombatTag.Marked), Is.True, "다음 라운드가 끝나기 전에 태그가 사라졌습니다.");

        state.RoundNumber = 5;
        SrpBattleFlow.StartRound(state);
        Assert.That(target.HasCombatTag(SrpCombatTag.Marked), Is.False, "지속시간이 끝난 태그가 남아 있습니다.");
        Assert.That(target.GetCombatTagExpireRound(SrpCombatTag.Marked), Is.EqualTo(0));
    }

    [TestCase(TestName = "duration 0 태그는 소모 전까지 남고, 갱신하면 만료가 다시 계산된다")]
    public void CombatTag_ZeroDurationIsPermanentAndRefreshResetsExpiry()
    {
        var state = CreateState();
        var caster = Unit(id: 1, owner: 0, x: 0, y: 0);
        var target = Unit(id: 2, owner: 1, x: 2, y: 0);
        state.Units.Add(caster);
        state.Units.Add(target);
        state.RoundNumber = 1;

        var permanent = TagSkill("killOrder", duration: 0);
        SrpSkills.ResolveActiveSkill(permanent, new SrpSkillRuntime(permanent.id), caster, 2, 0, state, null);
        state.RoundNumber = 9;
        SrpBattleFlow.StartRound(state);
        Assert.That(target.HasCombatTag(SrpCombatTag.KillOrder), Is.True, "duration 0 태그가 만료됐습니다.");
        Assert.That(SrpCombatTagUtility.BuildSummary(target, 9), Is.EqualTo("사살 지시"));

        var timed = TagSkill("marked", duration: 1);
        SrpSkills.ResolveActiveSkill(timed, new SrpSkillRuntime(timed.id), caster, 2, 0, state, null);
        Assert.That(target.GetCombatTagExpireRound(SrpCombatTag.Marked), Is.EqualTo(10));
        state.RoundNumber = 10;
        SrpSkills.ResolveActiveSkill(timed, new SrpSkillRuntime(timed.id), caster, 2, 0, state, null);
        Assert.That(target.GetCombatTagExpireRound(SrpCombatTag.Marked), Is.EqualTo(11), "갱신한 태그의 만료가 다시 계산되지 않았습니다.");

        // 소모되면 만료 기록도 함께 지워진다.
        var consumed = target.ConsumeCombatTags(SrpCombatTag.Marked);
        Assert.That(consumed, Is.EqualTo(SrpCombatTag.Marked));
        Assert.That(target.GetCombatTagExpireRound(SrpCombatTag.Marked), Is.EqualTo(0));
        Assert.That(target.HasCombatTag(SrpCombatTag.KillOrder), Is.True);
    }

    [TestCase(TestName = "유닛 복제본은 태그 만료를 따로 가진다")]
    public void CombatTag_ExpiryIsClonedIndependently()
    {
        var unit = Unit(id: 1);
        unit.AddCombatTag(SrpCombatTag.BalanceBroken, 4);
        var clone = unit.Clone();
        clone.AddCombatTag(SrpCombatTag.BalanceBroken, 7);

        Assert.That(unit.GetCombatTagExpireRound(SrpCombatTag.BalanceBroken), Is.EqualTo(4));
        Assert.That(clone.GetCombatTagExpireRound(SrpCombatTag.BalanceBroken), Is.EqualTo(7));
    }

    // ── 레거시 효과 ──────────────────────────────────────────────────────────

    [TestCase(TestName = "레거시 Cleave 효과는 Damage와 같은 방어·반응 규칙을 탄다")]
    public void LegacyCleaveEffect_UsesDamageRules()
    {
        var state = CreateState();
        var caster = Unit(id: 1, owner: 0, x: 1, y: 1, attackPower: 5);
        var target = Unit(id: 2, owner: 1, x: 2, y: 1, hp: 30, pg: 30, stance: SrpStance.Defensive);
        target.facing = SrpFacing.West;
        target.reactionPoints = 1;
        state.Units.Add(caster);
        state.Units.Add(target);
        var skill = new SrpSkillData
        {
            id = "test_legacy_cleave",
            skillType = SrpSkillType.Active,
            trigger = SrpSkillTrigger.OnActivate,
            targetType = SrpTargetType.SingleEnemy,
            range = 1,
            effects = new[] { new SrpSkillEffect { type = SrpEffectType.Cleave, value = 3 } },
        };

        SrpSkills.ResolveActiveSkill(skill, new SrpSkillRuntime(skill.id), caster, 2, 1, state, null);

        // 기준 피해 = 공격력 5 + 값 3 = 8. Guard 반응이 HP 2를 줄인다. 예전에는 방어를 건너뛰고 8이 그대로 들어갔다.
        Assert.That(target.hp, Is.EqualTo(24), "레거시 Cleave가 Guard 반응을 건너뛰었습니다.");
        Assert.That(target.reactionPoints, Is.EqualTo(0));
        // PG 피해 = 8/2 = 4, 수비 태세 PG-2와 Guard PG-2로 0.
        Assert.That(target.pg, Is.EqualTo(30));
    }

    static SrpSkillData AreaSkill(SrpTargetType targetType, int areaSize, params SrpSkillEffect[] effects)
    {
        return new SrpSkillData
        {
            id = "test_area",
            displayName = "테스트 범위",
            skillType = SrpSkillType.Active,
            trigger = SrpSkillTrigger.OnActivate,
            targetType = targetType,
            range = 5,
            areaSize = areaSize,
            cooldown = 2,
            effects = effects,
        };
    }

    static SrpSkillData TagSkill(string tag, int duration)
    {
        return new SrpSkillData
        {
            id = "test_tag_" + tag,
            skillType = SrpSkillType.Active,
            trigger = SrpSkillTrigger.OnActivate,
            targetType = SrpTargetType.SingleEnemy,
            range = 4,
            effects = new[] { new SrpSkillEffect { type = SrpEffectType.ApplyCombatTag, stat = tag, duration = duration } },
        };
    }

    // ── 공용 ─────────────────────────────────────────────────────────────────

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
