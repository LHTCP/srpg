using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 7차 테스트: 6차에서 남긴 결함 두 건(메이커 저장 시 템플릿 누락, 전장 장막 오버클럭)과 적 AI 확장.
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

    // ── 공용 ─────────────────────────────────────────────────────────────────

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
