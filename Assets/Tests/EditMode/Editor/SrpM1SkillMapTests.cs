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
        Assert.That(tiles, Does.Contain(new Vector2Int(3, 0)));
        Assert.That(tiles, Does.Not.Contain(new Vector2Int(4, 2)), "벽 뒤의 적이 대상에 들어갔습니다.");
        Assert.That(SrpSkills.IsTileInSkillReach(skill, caster, 4, 2, state), Is.False);

        skill.sightRule = SrpSightRule.None;
        Assert.That(SrpSkills.GetSkillTargetTiles(skill, caster, state), Does.Contain(new Vector2Int(4, 2)));
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
