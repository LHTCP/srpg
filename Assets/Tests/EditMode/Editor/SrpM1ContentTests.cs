using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 6차(콘텐츠·데이터) 테스트: 내장 프리셋/기본 데이터의 정합성, 새 스킬·유닛·맵, 로컬 DB의 기본 데이터 갱신.
/// </summary>
[Category("SrpM1All")]
public class SrpM1ContentTests
{
    static readonly SrpMapPreset[] AllPresets =
    {
        SrpMapPreset.M1OpeningPrototype,
        SrpMapPreset.M1EngagementLab,
        SrpMapPreset.M1QaIntegrated,
        SrpMapPreset.M1ObjectiveLab,
        SrpMapPreset.M1BarricadeAssault,
    };

    // ── 내장 데이터 정합성 ────────────────────────────────────────────────────

    [TestCase(TestName = "기본 스킬과 기본 유닛의 id는 겹치지 않고, 유닛의 스킬은 모두 기본 스킬에 있다")]
    public void DefaultData_IdsAreUniqueAndUnitSkillsExist()
    {
        var skillIds = new HashSet<string>();
        foreach (var skill in SrpDefaultSkills.Create())
            Assert.That(skillIds.Add(skill.id), Is.True, $"기본 스킬 id 중복: {skill.id}");

        var unitIds = new HashSet<string>();
        foreach (var unit in SrpDefaultUnits.Create())
        {
            Assert.That(unitIds.Add(unit.id), Is.True, $"기본 유닛 id 중복: {unit.id}");
            Assert.That(unit.skillIds.Length, Is.LessThanOrEqualTo(unit.maxSkills), $"{unit.id}의 스킬 수가 maxSkills를 넘습니다.");
            foreach (var skillId in unit.skillIds)
                Assert.That(skillIds.Contains(skillId), Is.True, $"{unit.id}의 스킬 {skillId}가 기본 스킬에 없습니다.");
        }
    }

    [TestCase(TestName = "모든 내장 프리셋은 배치·템플릿·스킬·상호작용 포인트가 서로 맞는다")]
    public void Presets_AreInternallyConsistent()
    {
        var skillIds = new HashSet<string>();
        foreach (var skill in SrpDefaultSkills.Create())
            skillIds.Add(skill.id);

        foreach (var preset in AllPresets)
        {
            var map = SrpDefaultMaps.GetPreset(preset);
            Assert.That(map.walkable.Length, Is.EqualTo(map.width * map.height), $"[{preset}] walkable 크기");

            var templates = new Dictionary<string, SrpUnitTemplateData>();
            foreach (var template in map.templates)
            {
                Assert.That(templates.ContainsKey(template.id), Is.False, $"[{preset}] 템플릿 id 중복: {template.id}");
                templates[template.id] = template;
                foreach (var skillId in template.skillIds)
                    Assert.That(skillIds.Contains(skillId), Is.True, $"[{preset}] {template.id}의 스킬 {skillId}가 기본 스킬에 없습니다.");
            }

            var occupied = new HashSet<int>();
            var commanderOwners = new HashSet<int>();
            foreach (var placement in map.placements)
            {
                Assert.That(templates.ContainsKey(placement.templateId), Is.True, $"[{preset}] 배치 템플릿 없음: {placement.templateId}");
                bool inBounds = placement.x >= 0 && placement.y >= 0 && placement.x < map.width && placement.y < map.height;
                Assert.That(inBounds, Is.True, $"[{preset}] 배치가 맵 밖: {placement.templateId}");
                int index = placement.x + placement.y * map.width;
                Assert.That(map.walkable[index], Is.True, $"[{preset}] 이동 불가 칸에 배치: {placement.templateId} ({placement.x},{placement.y})");
                Assert.That(occupied.Add(index), Is.True, $"[{preset}] 같은 칸에 두 유닛: ({placement.x},{placement.y})");
                if (placement.isCommander)
                    commanderOwners.Add(placement.owner);
            }

            foreach (var point in map.interactionPoints)
            {
                bool inBounds = point.x >= 0 && point.y >= 0 && point.x < map.width && point.y < map.height;
                Assert.That(inBounds, Is.True, $"[{preset}] 상호작용 포인트가 맵 밖: {point.id}");
            }
            foreach (var coverObject in map.coverObjects)
                Assert.That(map.walkable[coverObject.x + coverObject.y * map.width], Is.False, $"[{preset}] 엄폐물 칸이 이동 가능: ({coverObject.x},{coverObject.y})");

            if (map.victory != null && map.victory.commanderKill)
                Assert.That(commanderOwners.Count, Is.GreaterThan(0), $"[{preset}] 지휘관 처치 승리를 켰는데 지휘관이 없습니다.");

            var state = SrpBattleState.FromMap(map);
            Assert.That(state.Units.Count, Is.EqualTo(map.placements.Length), $"[{preset}] 전투 상태의 유닛 수");
        }
    }

    // ── 방벽선 돌파 맵 ───────────────────────────────────────────────────────

    [TestCase(TestName = "방벽선 돌파: 낮은 방벽은 이동만 막고 가운데 벽은 시야도 막으며, 틈으로 봉화대까지 갈 수 있다")]
    public void BarricadeAssault_BarricadeBlocksMovementNotSight()
    {
        var state = SrpBattleState.FromMap(SrpDefaultMaps.GetPreset(SrpMapPreset.M1BarricadeAssault));

        Assert.That(state.IsWalkableTile(7, 7), Is.False);
        Assert.That(state.BlocksSightTile(7, 7), Is.False, "낮은 방벽이 시야를 막았습니다.");
        Assert.That(state.IsWalkableTile(7, 4), Is.False);
        Assert.That(state.BlocksSightTile(7, 4), Is.True, "가운데 높은 벽이 시야를 막지 않았습니다.");
        Assert.That(state.BlocksSightTile(4, 4), Is.True, "폐허가 시야를 막지 않았습니다.");
        Assert.That(state.IsWalkableTile(7, 2), Is.True);
        Assert.That(state.IsWalkableTile(7, 6), Is.True);
        Assert.That(SrpLineOfSight.HasSight(state, 5, 7, 9, 7, SrpSightRule.Terrain), Is.True, "낮은 방벽 너머로 시야가 통하지 않았습니다.");
        Assert.That(SrpLineOfSight.HasSight(state, 5, 4, 9, 4, SrpSightRule.Terrain), Is.False, "가운데 벽 너머로 시야가 통했습니다.");

        Assert.That(state.VictoryRule.commanderKill, Is.True);
        Assert.That(state.VictoryRule.interactionObjective, Is.True);
        Assert.That(state.InteractionPoints.Count, Is.EqualTo(1));
        var beacon = state.InteractionPoints[0];
        Assert.That(beacon.effect, Is.EqualTo(SrpInteractionEffect.VictoryObjective));

        // 주인공 시작 칸에서 봉화대 옆 칸까지 걸어서 갈 수 있다(유닛 점유는 보지 않는다).
        int distance = WalkDistance(state, 1, 4, beacon.x + 1, beacon.y);
        Assert.That(distance, Is.GreaterThan(0), "봉화대까지 가는 길이 없습니다.");
        Assert.That(distance, Is.GreaterThanOrEqualTo(12), "봉화대가 너무 가까워 첫 라운드에 닿습니다.");

        int players = 0, enemies = 0, commanders = 0;
        foreach (var unit in state.Units)
        {
            if (unit.owner == 0) players++; else enemies++;
            if (unit.isCommander) commanders++;
        }
        Assert.That(players, Is.EqualTo(5));
        Assert.That(enemies, Is.EqualTo(6));
        Assert.That(commanders, Is.EqualTo(1), "지휘관은 적 방벽 대장 하나입니다.");
    }

    [Category("SrpAiSim")]
    [TestCase(TestName = "방벽선 돌파: AI끼리의 전투가 무승부 없이 적당한 길이로 끝난다")]
    public void BarricadeAssault_SimulationFinishesDecisively()
    {
        var config = new SrpBattleSimConfig
        {
            mapPreset = SrpMapPreset.M1BarricadeAssault,
            trials = 200,
            maxRounds = 24,
            baseSeed = 660001,
            sampleSeedCount = 3,
            owner0Policy = new SrpHeuristicAiPolicy(),
            owner1Policy = new SrpHeuristicAiPolicy(),
            thresholds = SrpSimThresholdConfig.Default(),
        };

        var output = SrpBattleSimRunner.RunBatch(config, writeReport: false);
        var outcome = output.report.outcome;
        Debug.Log(
            $"[SRPG][AI-Sim][Barricade] w0={outcome.owner0WinRate:F3}, w1={outcome.owner1WinRate:F3}, " +
            $"draw={outcome.drawRate:F3}, avgR={outcome.averageRounds:F2}");

        Assert.That(outcome.drawRate, Is.LessThanOrEqualTo(0.10f), "무승부가 많습니다. 방벽 틈에서 전투가 고착됐을 수 있습니다.");
        // 측정값(2026-11-15, 200회): 6차 평균 6.46라운드, 플레이어 진영 승률 0.705.
        // 7차(AI가 범위·태그·회복 스킬, 엄폐, 봉화대, 지휘관 방어를 쓴다) 평균 6.53라운드, 승률 0.580, 무승부 0.
        // 7차 기획 결정 반영(교전 대각선 포함, AI 사격수의 엄폐 + 경계태세 버티기) 평균 6.85라운드, 승률 0.700, 무승부 0.
        Assert.That(outcome.averageRounds, Is.InRange(4.5f, 12f), "전투 길이가 범위를 벗어났습니다.");
        Assert.That(outcome.owner0WinRate, Is.InRange(0.35f, 0.9f), "한쪽이 일방적으로 이깁니다.");
    }

    // ── 새 스킬 ──────────────────────────────────────────────────────────────

    [TestCase(TestName = "균열 베기는 피해를 준 뒤 균형 붕괴를 남기고, 이어지는 공격이 그 태그를 소모한다")]
    public void RendingStrike_LeavesBalanceBrokenForFollowUp()
    {
        var state = CreateState();
        var skill = state.SkillLookup["rending_strike"];
        var caster = Unit(id: 1, owner: 0, x: 1, y: 1, attackPower: 10, stance: SrpStance.Aggressive);
        var target = Unit(id: 2, owner: 1, x: 2, y: 1, hp: 30, pg: 30);
        target.facing = SrpFacing.West;
        state.Units.Add(caster);
        state.Units.Add(target);

        SrpSkills.ResolveActiveSkill(skill, new SrpSkillRuntime(skill.id), caster, 2, 1, state, null);

        Assert.That(target.hp, Is.EqualTo(22), "균열 베기 HP 피해(8)가 다릅니다.");
        Assert.That(target.HasCombatTag(SrpCombatTag.BalanceBroken), Is.True, "균열 베기가 자기 태그를 바로 소모했습니다.");
        Assert.That(skill.endsActivation, Is.False, "균열 베기 뒤에 기본 공격을 이어 갈 수 있어야 합니다.");

        int pgBefore = target.pg;
        var outcome = SrpCombatResolver.ApplyAttack(state, caster, target);
        Assert.That(target.HasCombatTag(SrpCombatTag.BalanceBroken), Is.False);
        Assert.That(outcome.bonusPgFromCombatTags, Is.EqualTo(4));
        Assert.That(target.pg, Is.LessThan(pgBefore));
    }

    [TestCase(TestName = "제압 사격은 범위 안 적에게 피해를 주고 RP를 깎으며, 범위 밖 적은 건드리지 않는다")]
    public void SuppressingFire_DamagesAndStripsReaction()
    {
        var state = CreateState(7, 5);
        var skill = state.SkillLookup["suppressing_fire"];
        var caster = Unit(id: 1, owner: 0, x: 0, y: 2);
        var inAreaA = Unit(id: 2, owner: 1, x: 4, y: 2);
        var inAreaB = Unit(id: 3, owner: 1, x: 5, y: 3);
        var outside = Unit(id: 4, owner: 1, x: 6, y: 0);
        var allyInArea = Unit(id: 5, owner: 0, x: 3, y: 1);
        foreach (var unit in new[] { caster, inAreaA, inAreaB, outside, allyInArea })
        {
            unit.maxReactionPoints = 1;
            unit.reactionPoints = 1;
            state.Units.Add(unit);
        }

        Assert.That(SrpSkills.GetSkillTargetTiles(skill, caster, state), Has.Member(new Vector2Int(4, 2)));
        SrpSkills.ResolveActiveSkill(skill, new SrpSkillRuntime(skill.id), caster, 4, 2, state, null);

        Assert.That(inAreaA.hp, Is.LessThan(20));
        Assert.That(inAreaB.hp, Is.LessThan(20));
        Assert.That(inAreaA.reactionPoints, Is.EqualTo(0));
        Assert.That(inAreaB.reactionPoints, Is.EqualTo(0));
        Assert.That(outside.hp, Is.EqualTo(20));
        Assert.That(outside.reactionPoints, Is.EqualTo(1));
        Assert.That(allyInArea.hp, Is.EqualTo(20), "제압 사격이 아군을 맞혔습니다.");
        Assert.That(allyInArea.reactionPoints, Is.EqualTo(1));
        Assert.That(skill.endsActivation, Is.True);
    }

    [TestCase(TestName = "전열 재정비는 자신과 주변 1칸 아군의 PG만 회복한다")]
    public void RallyLine_RestoresPgAroundCaster()
    {
        var state = CreateState();
        var skill = state.SkillLookup["rally_line"];
        var caster = Unit(id: 1, owner: 0, x: 2, y: 2);
        var nearAlly = Unit(id: 2, owner: 0, x: 3, y: 3);
        var farAlly = Unit(id: 3, owner: 0, x: 4, y: 0);
        var nearEnemy = Unit(id: 4, owner: 1, x: 1, y: 2);
        foreach (var unit in new[] { caster, nearAlly, farAlly, nearEnemy })
        {
            unit.pg = 10;
            state.Units.Add(unit);
        }

        var tiles = SrpSkills.GetSkillTargetTiles(skill, caster, state);
        Assert.That(tiles.Count, Is.EqualTo(1), "사거리 0 범위 스킬은 자기 칸만 지정할 수 있어야 합니다.");
        SrpSkills.ResolveActiveSkill(skill, new SrpSkillRuntime(skill.id), caster, 2, 2, state, null);

        Assert.That(caster.pg, Is.EqualTo(14));
        Assert.That(nearAlly.pg, Is.EqualTo(14));
        Assert.That(farAlly.pg, Is.EqualTo(10));
        Assert.That(nearEnemy.pg, Is.EqualTo(10));
    }

    [TestCase(TestName = "충격장은 범위 안 적에게 피해와 표식을 준다")]
    public void ShockField_DamagesAndMarksArea()
    {
        var state = CreateState();
        var skill = state.SkillLookup["shock_field"];
        var caster = Unit(id: 1, owner: 0, x: 0, y: 2);
        var enemyA = Unit(id: 2, owner: 1, x: 3, y: 2);
        var enemyB = Unit(id: 3, owner: 1, x: 2, y: 3);
        enemyA.facing = SrpFacing.West;
        enemyB.facing = SrpFacing.West;
        state.Units.Add(caster);
        state.Units.Add(enemyA);
        state.Units.Add(enemyB);

        SrpSkills.ResolveActiveSkill(skill, new SrpSkillRuntime(skill.id), caster, 3, 2, state, null);

        Assert.That(enemyA.hp, Is.EqualTo(15));
        Assert.That(enemyB.hp, Is.EqualTo(15));
        Assert.That(enemyA.HasCombatTag(SrpCombatTag.Marked), Is.True);
        Assert.That(enemyB.HasCombatTag(SrpCombatTag.Marked), Is.True);
        Assert.That(caster.HasCombatTag(SrpCombatTag.Marked), Is.False);
    }

    [TestCase(TestName = "투사의 투지는 턴 시작에 PG를 2 회복하고 최대치를 넘지 않는다")]
    public void DuelistSecondWind_RestoresPgAtTurnStart()
    {
        var state = SrpBattleState.FromMap(SrpDefaultMaps.GetPreset(SrpMapPreset.M1BarricadeAssault));
        SrpUnitRuntime duelist = null;
        foreach (var unit in state.Units)
        {
            if (unit.templateId == "duelist")
                duelist = unit;
        }
        Assert.That(duelist, Is.Not.Null);
        Assert.That(duelist.skillIds, Has.Member("rending_strike"));

        duelist.pg = 10;
        SrpSkills.TryApplyPassiveTurnStart(duelist, state, null);
        Assert.That(duelist.pg, Is.EqualTo(12));

        duelist.pg = duelist.maxPg - 1;
        SrpSkills.TryApplyPassiveTurnStart(duelist, state, null);
        Assert.That(duelist.pg, Is.EqualTo(duelist.maxPg));
    }

    // ── 로컬 DB의 기본 데이터 갱신 ───────────────────────────────────────────

    [TestCase(TestName = "기본 데이터 병합은 내장 id를 최신 값으로 바꾸고 사용자 항목을 남긴다")]
    public void DefaultDataMerge_RefreshesBuiltInsAndKeepsCustom()
    {
        var saved = new[]
        {
            new SrpSkillData { id = "my_custom", displayName = "내 스킬", range = 7 },
            new SrpSkillData { id = "cleave", displayName = "옛 강타", range = 9 },
        };

        var merged = SrpDefaultDataMerge.MergeSkills(saved, SrpDefaultSkills.Create());

        Assert.That(merged.Length, Is.EqualTo(SrpDefaultSkills.Create().Length + 1));
        SrpSkillData cleave = null, custom = null;
        int cleaveCount = 0;
        foreach (var skill in merged)
        {
            if (skill.id == "cleave") { cleave = skill; cleaveCount++; }
            if (skill.id == "my_custom") custom = skill;
        }
        Assert.That(cleaveCount, Is.EqualTo(1));
        Assert.That(cleave.range, Is.EqualTo(1), "내장 스킬이 최신 기본값으로 바뀌지 않았습니다.");
        Assert.That(custom, Is.Not.Null, "사용자 스킬이 사라졌습니다.");
        Assert.That(custom.range, Is.EqualTo(7));
        Assert.That(merged[merged.Length - 1].id, Is.EqualTo("my_custom"), "사용자 스킬은 기본 스킬 뒤에 둡니다.");

        var units = SrpDefaultDataMerge.MergeUnits(
            new[] { new SrpUnitTemplateData { id = "my_unit" }, new SrpUnitTemplateData { id = "vanguard", maxHp = 1 } },
            SrpDefaultUnits.Create());
        Assert.That(units.Length, Is.EqualTo(SrpDefaultUnits.Create().Length + 1));
        foreach (var unit in units)
        {
            if (unit.id == "vanguard")
                Assert.That(unit.maxHp, Is.GreaterThan(1));
        }
    }

    [TestCase(TestName = "예전 버전으로 저장된 skills.json은 불러올 때 갱신되고 백업이 남는다")]
    public void LoadSkillsOrDefault_UpgradesStaleFileAndKeepsBackup()
    {
        string path = SrpDataIO.SkillsPath;
        string backup = Path.Combine(Path.GetDirectoryName(path), "skills.backup-v0.json");
        string original = File.Exists(path) ? File.ReadAllText(path) : null;
        // 개발자 PC에 실제 갱신 때 생긴 백업이 있을 수 있다. 사용자가 고친 내장 스킬의 유일한 사본일 수 있으므로 보존한다.
        byte[] originalBackup = File.Exists(backup) ? File.ReadAllBytes(backup) : null;
        try
        {
            if (originalBackup != null)
                File.Delete(backup);

            // 버전 필드가 없던 시절의 파일: 강타가 옛 값(27)이고 사용자 스킬이 하나 있다.
            var stale = new SrpSkillDatabase
            {
                defaultsVersion = 0,
                skills = new[]
                {
                    new SrpSkillData
                    {
                        id = "cleave",
                        skillType = SrpSkillType.Active,
                        effects = new[] { new SrpSkillEffect { type = SrpEffectType.Damage, value = 27 } },
                    },
                    new SrpSkillData { id = "my_custom", displayName = "내 스킬" },
                },
            };
            File.WriteAllText(path, JsonUtility.ToJson(stale, true));

            var loaded = SrpDataIO.LoadSkillsOrDefault();

            bool hasCustom = false, hasNewSkill = false;
            foreach (var skill in loaded)
            {
                if (skill.id == "cleave")
                    Assert.That(skill.effects[0].value, Is.EqualTo(18), "강타가 최신 기본값으로 갱신되지 않았습니다.");
                if (skill.id == "my_custom") hasCustom = true;
                if (skill.id == "suppressing_fire") hasNewSkill = true;
            }
            Assert.That(hasCustom, Is.True);
            Assert.That(hasNewSkill, Is.True, "새 기본 스킬이 추가되지 않았습니다.");
            Assert.That(File.Exists(backup), Is.True, "갱신 전 파일의 백업이 없습니다.");

            // 갱신 뒤에는 사용자가 고친 내장 스킬을 그대로 둔다.
            foreach (var skill in loaded)
            {
                if (skill.id == "cleave")
                    skill.effects[0].value = 99;
            }
            SrpDataIO.SaveSkills(loaded);
            foreach (var skill in SrpDataIO.LoadSkillsOrDefault())
            {
                if (skill.id == "cleave")
                    Assert.That(skill.effects[0].value, Is.EqualTo(99), "같은 버전에서 사용자의 수정이 덮어써졌습니다.");
            }
        }
        finally
        {
            // 다른 테스트가 같은 저장 폴더를 읽으므로 원래 상태로 되돌린다.
            if (original != null)
                File.WriteAllText(path, original);
            else if (File.Exists(path))
                File.Delete(path);
            if (originalBackup != null)
                File.WriteAllBytes(backup, originalBackup);
            else if (File.Exists(backup))
                File.Delete(backup);
        }
    }

    // ── 맵 메이커: 승리 조건·엄폐 강도 편집 ──────────────────────────────────

    [TestCase(TestName = "메이커의 상호작용 포인트는 같은 칸을 다시 고르면 지워지고 id가 겹치지 않는다")]
    public void ObjectiveEdit_ToggleInteractionPoint()
    {
        var carry = new SrpMapCarryOver();

        Assert.That(SrpMapObjectiveEdit.ToggleInteractionPoint(carry, 2, 3, 0, true), Is.True);
        Assert.That(SrpMapObjectiveEdit.ToggleInteractionPoint(carry, 4, 1, 1, false), Is.True);
        Assert.That(carry.interactionPoints.Length, Is.EqualTo(2));
        Assert.That(carry.interactionPoints[0].effect, Is.EqualTo(SrpInteractionEffect.VictoryObjective));
        Assert.That(carry.interactionPoints[0].requiredOwner, Is.EqualTo(0));
        Assert.That(carry.interactionPoints[1].effect, Is.EqualTo(SrpInteractionEffect.None));
        Assert.That(carry.interactionPoints[1].requiredOwner, Is.EqualTo(1));
        Assert.That(carry.interactionPoints[0].id, Is.Not.EqualTo(carry.interactionPoints[1].id));

        Assert.That(SrpMapObjectiveEdit.ToggleInteractionPoint(carry, 2, 3, 0, true), Is.False);
        Assert.That(carry.interactionPoints.Length, Is.EqualTo(1));
        Assert.That(carry.interactionPoints[0].x, Is.EqualTo(4));

        // 지운 뒤 다시 놓아도 남아 있는 포인트와 id가 겹치지 않는다.
        SrpMapObjectiveEdit.ToggleInteractionPoint(carry, 0, 0, 0, true);
        Assert.That(carry.interactionPoints[0].id, Is.Not.EqualTo(carry.interactionPoints[1].id));
    }

    [TestCase(TestName = "메이커의 엄폐 강도는 새로 놓는 엄폐의 감쇠 수치를 정한다")]
    public void CoverEdit_StrengthSetsCoverValues()
    {
        var objects = new List<SrpCoverObjectData>();
        var segments = new List<SrpCoverSegmentData>();

        SrpMapCoverEdit.ToggleCoverObject(objects, 0, 0, true);
        SrpMapCoverEdit.ToggleCoverObject(objects, 1, 0, true, SrpMapCoverEdit.Strength.Heavy);
        SrpMapCoverEdit.ToggleCoverSegment(segments, 0, 1, SrpCoverEdge.East, false);
        SrpMapCoverEdit.ToggleCoverSegment(segments, 1, 1, SrpCoverEdge.East, false, SrpMapCoverEdit.Strength.Light);

        Assert.That(objects[0].coverDef, Is.EqualTo(2), "기본 엄폐물 수치가 이전(2/1)과 달라졌습니다.");
        Assert.That(objects[0].coverGrd, Is.EqualTo(1));
        Assert.That(objects[1].coverDef, Is.EqualTo(3));
        Assert.That(objects[1].coverGrd, Is.EqualTo(2));
        Assert.That(segments[0].coverDef, Is.EqualTo(0), "기본 엄폐 벽은 0으로 저장해 전투 규칙의 기본값을 써야 합니다.");
        Assert.That(segments[0].coverGrd, Is.EqualTo(0));
        Assert.That(segments[1].coverDef, Is.EqualTo(1));
        Assert.That(segments[1].coverGrd, Is.EqualTo(1));

        var strength = SrpMapCoverEdit.Strength.Default;
        strength = SrpMapCoverEdit.NextStrength(strength);
        Assert.That(strength, Is.EqualTo(SrpMapCoverEdit.Strength.Light));
        strength = SrpMapCoverEdit.NextStrength(SrpMapCoverEdit.NextStrength(strength));
        Assert.That(strength, Is.EqualTo(SrpMapCoverEdit.Strength.Default));
    }

    [TestCase(TestName = "메이커로 만든 승리 조건 맵은 저장 형식을 거쳐 전투에서 목표 확보와 지휘관 처치로 끝난다")]
    public void MakerData_VictoryMapWorksInBattleAfterJsonRoundTrip()
    {
        var carry = new SrpMapCarryOver();
        carry.victory.commanderKill = true;
        carry.victory.interactionObjective = true;
        SrpMapObjectiveEdit.ToggleInteractionPoint(carry, 3, 0, 0, true);

        var walk = new bool[25];
        for (int i = 0; i < walk.Length; i++)
            walk[i] = true;
        var map = new SrpMapFileV1
        {
            width = 5,
            height = 5,
            walkable = walk,
            playerOrder = new[] { 0, 1 },
            templates = new[]
            {
                new SrpUnitTemplateData { id = "a", maxHp = 20, maxPg = 10 },
                new SrpUnitTemplateData { id = "b", maxHp = 20, maxPg = 10 },
            },
            placements = new[]
            {
                new SrpPlacementData { templateId = "a", owner = 0, x = 2, y = 0 },
                new SrpPlacementData { templateId = "b", owner = 1, x = 4, y = 4, isCommander = true },
                new SrpPlacementData { templateId = "b", owner = 1, x = 4, y = 3 },
            },
        };
        carry.ApplyTo(map);

        var restored = JsonUtility.FromJson<SrpMapFileV1>(JsonUtility.ToJson(map));

        // 목표 확보: 플레이어 유닛이 옆 칸의 승리 목표를 활성화하면 이긴다.
        var state = SrpBattleState.FromMap(restored);
        Assert.That(SrpVictory.Evaluate(state).decided, Is.False);
        var player = state.Units[0];
        Assert.That(state.TryActivateInteraction(player, out _), Is.True, "메이커로 놓은 승리 목표를 활성화하지 못했습니다.");
        var byObjective = SrpVictory.Evaluate(state);
        Assert.That(byObjective.decided, Is.True);
        Assert.That(byObjective.winnerOwner, Is.EqualTo(0));
        Assert.That(byObjective.reason, Is.EqualTo(SrpVictoryReason.ObjectiveSecured));

        // 지휘관 처치: 호위가 남아 있어도 지휘관이 쓰러지면 끝난다.
        var second = SrpBattleState.FromMap(restored);
        foreach (var unit in second.Units)
        {
            if (unit.isCommander)
                second.RemoveUnit(unit);
        }
        var byCommander = SrpVictory.Evaluate(second);
        Assert.That(byCommander.decided, Is.True);
        Assert.That(byCommander.winnerOwner, Is.EqualTo(0));
        Assert.That(byCommander.reason, Is.EqualTo(SrpVictoryReason.CommanderKilled));
    }

    // ── 공용 ─────────────────────────────────────────────────────────────────

    /// <summary>이동 가능 칸만 밟아 가는 최단 거리(상하좌우). 길이 없으면 -1.</summary>
    static int WalkDistance(SrpBattleState state, int fromX, int fromY, int toX, int toY)
    {
        var distance = new Dictionary<int, int> { [state.Index(fromX, fromY)] = 0 };
        var queue = new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(fromX, fromY));
        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            int d = distance[state.Index(current.x, current.y)];
            if (current.x == toX && current.y == toY)
                return d;
            for (int i = 0; i < 4; i++)
            {
                int nx = current.x + dx[i];
                int ny = current.y + dy[i];
                if (!state.IsWalkableTile(nx, ny) || distance.ContainsKey(state.Index(nx, ny)))
                    continue;
                distance[state.Index(nx, ny)] = d + 1;
                queue.Enqueue(new Vector2Int(nx, ny));
            }
        }
        return -1;
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
