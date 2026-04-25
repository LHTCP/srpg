using NUnit.Framework;
using UnityEngine;

[Category("SrpM1All")]
public class SrpMakerMetadataTests
{
    [Test]
    public void SkillMetadata_JsonRoundTrip_PreservesChargeOverclockAndParryFields()
    {
        var source = new SrpSkillDatabase
        {
            skills = new[]
            {
                new SrpSkillData
                {
                    id = "maker_meta_skill",
                    displayName = "Maker Meta Skill",
                    skillType = SrpSkillType.Active,
                    trigger = SrpSkillTrigger.OnActivate,
                    targetType = SrpTargetType.SingleEnemy,
                    cooldown = 2,
                    maxCharges = 3,
                    chargeRecoveryTurns = 2,
                    overclockFrozenHeartCost = 1,
                    overclockCooldownReduction = 2,
                    overclockChargeRestore = 1,
                    isParryable = true,
                    requiresParryTelegraph = true,
                },
            },
        };

        string json = JsonUtility.ToJson(source, true);
        var restored = JsonUtility.FromJson<SrpSkillDatabase>(json);
        var skill = restored.skills[0];

        Assert.AreEqual(3, skill.maxCharges);
        Assert.AreEqual(2, skill.chargeRecoveryTurns);
        Assert.AreEqual(1, skill.overclockFrozenHeartCost);
        Assert.AreEqual(2, skill.overclockCooldownReduction);
        Assert.AreEqual(1, skill.overclockChargeRestore);
        Assert.IsTrue(skill.isParryable);
        Assert.IsTrue(skill.requiresParryTelegraph);
    }

    [Test]
    public void UnitMetadata_JsonRoundTrip_PreservesV2StatsEnumsAndTags()
    {
        var source = new SrpUnitDatabase
        {
            units = new[]
            {
                new SrpUnitTemplateData
                {
                    id = "maker_meta_unit",
                    displayName = "Maker Meta Unit",
                    maxActionPoints = 3,
                    maxReactionPoints = 2,
                    maxPg = 24,
                    speed = 15,
                    weaponClass = SrpWeaponClass.Firearm,
                    stance = SrpStance.Defensive,
                    facing = SrpFacing.West,
                    tags = (int)(SrpUnitTags.Boss | SrpUnitTags.Large | SrpUnitTags.ParryUser | SrpUnitTags.Tank),
                },
            },
        };

        string json = JsonUtility.ToJson(source, true);
        var restored = JsonUtility.FromJson<SrpUnitDatabase>(json);
        var unit = restored.units[0];

        Assert.AreEqual(3, unit.maxActionPoints);
        Assert.AreEqual(2, unit.maxReactionPoints);
        Assert.AreEqual(24, unit.maxPg);
        Assert.AreEqual(15, unit.speed);
        Assert.AreEqual(SrpWeaponClass.Firearm, unit.weaponClass);
        Assert.AreEqual(SrpStance.Defensive, unit.stance);
        Assert.AreEqual(SrpFacing.West, unit.facing);
        Assert.AreEqual((int)(SrpUnitTags.Boss | SrpUnitTags.Large | SrpUnitTags.ParryUser | SrpUnitTags.Tank), unit.tags);
    }

    [Test]
    public void UnitMaker_SyncV2LegacyStats_StoresApAndPgCompatibilityFields()
    {
        var unit = new SrpUnitTemplateData
        {
            maxActionPoints = 4,
            maxReactionPoints = 3,
            maxPg = 30,
            speed = 12,
            maxAp = 99,
            maxPosture = 99,
        };

        SrpUnitMakerController.SyncV2LegacyStats(unit);

        Assert.AreEqual(4, unit.maxAp);
        Assert.AreEqual(30, unit.maxPosture);
        Assert.AreEqual(4, unit.maxActionPoints);
        Assert.AreEqual(3, unit.maxReactionPoints);
        Assert.AreEqual(30, unit.maxPg);
        Assert.AreEqual(12, unit.speed);
    }

    [Test]
    public void BattleState_FromMap_PreservesExplicitFirearmWeaponClass()
    {
        var map = CreateMetadataMap(new SrpUnitTemplateData
        {
            id = "firearm_unit",
            displayName = "Firearm Unit",
            attackRange = 1,
            weaponClass = SrpWeaponClass.Firearm,
        });

        var state = SrpBattleState.FromMap(map);

        Assert.AreEqual(SrpWeaponClass.Firearm, state.Units[0].weaponClass);
    }

    [Test]
    public void BattleState_FromMap_AppliesAllowedDisabledAndMaxSkillFilters()
    {
        var map = CreateMetadataMap(new SrpUnitTemplateData
        {
            id = "skill_filter_unit",
            displayName = "Skill Filter Unit",
            maxSkills = 1,
            skillIds = new[] { "slash", "heal", "blocked" },
        });
        map.allowedSkillIds = new[] { "slash", "heal" };
        map.placements[0].disabledSkillIds = new[] { "heal" };

        var state = SrpBattleState.FromMap(map);
        var unit = state.Units[0];

        Assert.AreEqual(1, unit.skillIds.Count);
        Assert.AreEqual("slash", unit.skillIds[0]);
        Assert.AreEqual(1, unit.skillRuntimes.Count);
        Assert.AreEqual("slash", unit.skillRuntimes[0].skillId);
    }

    [Test]
    public void SkillBuffStat_ApAndPostureAliases_ModifyRuntimeStats()
    {
        var map = CreateMetadataMap(new SrpUnitTemplateData
        {
            id = "buff_target",
            displayName = "Buff Target",
            maxActionPoints = 4,
            maxPg = 20,
        });
        var state = SrpBattleState.FromMap(map);
        var unit = state.Units[0];
        unit.actionPoints = 1;
        unit.pg = 5;

        var skill = new SrpSkillData
        {
            id = "buff_aliases",
            skillType = SrpSkillType.Active,
            trigger = SrpSkillTrigger.OnActivate,
            targetType = SrpTargetType.Self,
            effects = new[]
            {
                new SrpSkillEffect { type = SrpEffectType.BuffStat, stat = "ap", value = 2 },
                new SrpSkillEffect { type = SrpEffectType.BuffStat, stat = "posture", value = 3 },
            },
        };

        SrpSkills.ResolveActiveSkill(skill, new SrpSkillRuntime(skill.id), unit, unit.anchorX, unit.anchorY, state, null);

        Assert.AreEqual(3, unit.actionPoints);
        Assert.AreEqual(8, unit.pg);
    }

    [Test]
    public void SkillDamage_TriggersGroggy_WhenPgFallsToZero()
    {
        var map = CreateMetadataMap(new SrpUnitTemplateData
        {
            id = "caster",
            displayName = "Caster",
        });
        map.templates = new[]
        {
            map.templates[0],
            new SrpUnitTemplateData
            {
                id = "target",
                displayName = "Target",
                maxHp = 50,
                maxPg = 4,
            },
        };
        map.placements = new[]
        {
            map.placements[0],
            new SrpPlacementData { templateId = "target", owner = 1, x = 2, y = 1 },
        };

        var state = SrpBattleState.FromMap(map);
        var caster = state.Units[0];
        var target = state.Units[1];
        target.reactionPoints = 0;
        var skill = new SrpSkillData
        {
            id = "pg_break",
            skillType = SrpSkillType.Active,
            trigger = SrpSkillTrigger.OnActivate,
            targetType = SrpTargetType.SingleEnemy,
            range = 3,
            effects = new[]
            {
                new SrpSkillEffect { type = SrpEffectType.Damage, value = 10 },
            },
        };

        SrpSkills.ResolveActiveSkill(skill, new SrpSkillRuntime(skill.id), caster, target.anchorX, target.anchorY, state, null);

        Assert.AreEqual(0, target.pg);
        Assert.IsTrue(target.groggy);
        Assert.IsFalse(target.eliminated);
    }

    static SrpMapFileV1 CreateMetadataMap(SrpUnitTemplateData template)
    {
        if (template.maxHp <= 0) template.maxHp = 30;
        if (template.maxPg <= 0) template.maxPg = 18;
        if (template.maxActionPoints <= 0) template.maxActionPoints = 2;
        if (template.maxReactionPoints <= 0) template.maxReactionPoints = 1;
        if (template.speed <= 0) template.speed = 10;

        return new SrpMapFileV1
        {
            width = 4,
            height = 4,
            walkable = new bool[16]
            {
                true, true, true, true,
                true, true, true, true,
                true, true, true, true,
                true, true, true, true,
            },
            templates = new[] { template },
            placements = new[]
            {
                new SrpPlacementData { templateId = template.id, owner = 0, x = 1, y = 1 },
            },
        };
    }
}
