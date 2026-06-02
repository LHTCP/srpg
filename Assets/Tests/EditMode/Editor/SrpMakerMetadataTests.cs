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
                    overclockPowerBonus = 4,
                    isParryable = true,
                    requiresParryTelegraph = true,
                    effects = new[]
                    {
                        new SrpSkillEffect
                        {
                            type = SrpEffectType.ApplyCombatTag,
                            stat = "marked",
                            value = 0,
                            duration = 1,
                        },
                    },
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
        Assert.AreEqual(4, skill.overclockPowerBonus);
        Assert.IsTrue(skill.isParryable);
        Assert.IsTrue(skill.requiresParryTelegraph);
        Assert.AreEqual(SrpEffectType.ApplyCombatTag, skill.effects[0].type);
        Assert.AreEqual("marked", skill.effects[0].stat);
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
                    maxAmmo = 5,
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
        Assert.AreEqual(5, unit.maxAmmo);
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
            maxAmmo = -1,
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
        Assert.AreEqual(0, unit.maxAmmo);
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

    [Test]
    public void MapMetadata_JsonRoundTrip_PreservesInteractionPoints()
    {
        var map = CreateMetadataMap(new SrpUnitTemplateData
        {
            id = "interaction_actor",
            displayName = "Interaction Actor",
        });
        map.interactionPoints = new[]
        {
            new SrpInteractionPointData
            {
                id = "door_switch",
                displayName = "Door Switch",
                x = 2,
                y = 1,
                owner = -1,
                requiredOwner = 0,
                singleUse = true,
                activated = false,
            },
        };

        string json = JsonUtility.ToJson(map, true);
        var restored = JsonUtility.FromJson<SrpMapFileV1>(json);
        var point = restored.interactionPoints[0];

        Assert.AreEqual("door_switch", point.id);
        Assert.AreEqual("Door Switch", point.displayName);
        Assert.AreEqual(2, point.x);
        Assert.AreEqual(1, point.y);
        Assert.AreEqual(-1, point.owner);
        Assert.AreEqual(0, point.requiredOwner);
        Assert.IsTrue(point.singleUse);
        Assert.IsFalse(point.activated);
    }

    [Test]
    public void MapMetadata_JsonRoundTrip_PreservesCoverSegments()
    {
        var map = CreateMetadataMap(new SrpUnitTemplateData
        {
            id = "cover_actor",
            displayName = "Cover Actor",
        });
        map.coverSegments = new[]
        {
            new SrpCoverSegmentData
            {
                x = 3,
                y = 2,
                edge = SrpCoverEdge.West,
                shape = SrpCoverShape.Corner,
                coverDef = 4,
                coverGrd = 2,
                blocksLineOfSight = true,
            },
        };

        string json = JsonUtility.ToJson(map, true);
        var restored = JsonUtility.FromJson<SrpMapFileV1>(json);
        var segment = restored.coverSegments[0];

        Assert.AreEqual(3, segment.x);
        Assert.AreEqual(2, segment.y);
        Assert.AreEqual(SrpCoverEdge.West, segment.edge);
        Assert.AreEqual(SrpCoverShape.Corner, segment.shape);
        Assert.AreEqual(4, segment.coverDef);
        Assert.AreEqual(2, segment.coverGrd);
        Assert.IsTrue(segment.blocksLineOfSight);
    }

    [Test]
    public void M1QaIntegratedPreset_ExposesPhase2SkillsAndScenarioHooks()
    {
        var state = SrpBattleState.FromMap(SrpDefaultMaps.GetPreset(SrpMapPreset.M1QaIntegrated));
        bool foundChargeSkill = false;
        bool foundParrySkill = false;
        bool foundTank = false;
        bool foundFirearmLine = false;
        bool foundInteractionPoint = state.InteractionPoints.Count > 0;
        bool foundDirectionalCover = state.CoverSegments.Count > 0;
        bool foundLineBlockingCover = false;
        bool foundCombatTagSkill = false;
        bool foundHero = false;
        bool foundPlayerTank = false;
        bool foundRiflemanPassive = false;
        bool foundMageIntervention = false;

        foreach (var unit in state.Units)
        {
            if (unit.owner == 0 && unit.templateId == "breaker" && unit.HasTag(SrpUnitTags.ParryUser))
                foundHero = true;
            if (unit.owner == 0 && unit.templateId == "vanguard" && unit.HasTag(SrpUnitTags.Tank))
                foundPlayerTank = true;
            if (unit.HasTag(SrpUnitTags.Tank))
                foundTank = true;
            if (unit.weaponClass == SrpWeaponClass.Firearm && unit.attackRange >= 4)
                foundFirearmLine = true;

            foreach (var skillId in unit.skillIds)
            {
                Assert.IsTrue(state.SkillLookup.ContainsKey(skillId), $"프리셋 스킬 ID가 SkillLookup에 없습니다: {skillId}");
                var skill = state.SkillLookup[skillId];
                if (SrpSkills.UsesCharges(skill))
                    foundChargeSkill = true;
                if (skill.isParryable || skill.requiresParryTelegraph)
                    foundParrySkill = true;
                if (skill.id == "rifle_exposed_punisher")
                    foundRiflemanPassive = true;
                if (skill.id == "arcane_screen")
                    foundMageIntervention = true;
                if (skill.effects != null)
                    foreach (var effect in skill.effects)
                        if (effect.type == SrpEffectType.ApplyCombatTag)
                            foundCombatTagSkill = true;
            }
        }
        foreach (var segment in state.CoverSegments)
        {
            if (segment != null && segment.blocksLineOfSight)
                foundLineBlockingCover = true;
        }

        Assert.IsTrue(foundChargeSkill, "M1 QA 프리셋이 충전형 스킬 체험 유닛을 포함하지 않습니다.");
        Assert.IsTrue(foundParrySkill, "M1 QA 프리셋이 패링 가능 스킬 체험 유닛을 포함하지 않습니다.");
        Assert.IsTrue(foundTank, "M1 QA 프리셋이 탱커/교전 확인 유닛을 포함하지 않습니다.");
        Assert.IsTrue(foundFirearmLine, "M1 QA 프리셋이 오버워치 사선 확인용 총기 유닛을 포함하지 않습니다.");
        Assert.IsTrue(foundInteractionPoint, "M1 QA 프리셋이 상호작용 포인트를 포함하지 않습니다.");
        Assert.IsTrue(foundDirectionalCover, "M1 QA 프리셋이 방향성 엄폐 segment를 포함하지 않습니다.");
        Assert.IsTrue(foundLineBlockingCover, "M1 QA 프리셋이 사선 차단 방향성 엄폐 segment를 포함하지 않습니다.");
        Assert.IsTrue(foundCombatTagSkill, "M1 QA 프리셋이 공용 전투 태그 스킬을 포함하지 않습니다.");
        Assert.IsTrue(foundHero, "M1 QA 프리셋이 주인공/패링 역할 데이터를 포함하지 않습니다.");
        Assert.IsTrue(foundPlayerTank, "M1 QA 프리셋이 플레이어 탱커 역할 데이터를 포함하지 않습니다.");
        Assert.IsTrue(foundRiflemanPassive, "M1 QA 프리셋이 사격수 고유 패시브를 포함하지 않습니다.");
        Assert.IsTrue(foundMageIntervention, "M1 QA 프리셋이 마법 전장 개입 스킬을 포함하지 않습니다.");
    }

    [Test]
    public void M1OpeningPrototypePreset_LoadsAsAsymmetricFirstBattleScenario()
    {
        var map = SrpDefaultMaps.GetPreset(SrpMapPreset.M1OpeningPrototype);
        var state = SrpBattleState.FromMap(map);

        Assert.AreEqual("m1_opening_prototype", map.name);
        Assert.AreEqual(12, state.Width, "첫 전투 프리셋 가로 크기 불일치");
        Assert.AreEqual(9, state.Height, "첫 전투 프리셋 세로 크기 불일치");
        Assert.AreEqual(4, state.GetAliveUnitsForOwner(0).Count, "첫 전투 플레이어 파티는 4인이어야 합니다.");
        Assert.AreEqual(5, state.GetAliveUnitsForOwner(1).Count, "첫 전투 적 구성은 QA 대칭 복사본이 아니어야 합니다.");
        Assert.GreaterOrEqual(state.InteractionPoints.Count, 1, "첫 전투 프리셋에 상호작용 포인트가 없습니다.");

        var hero = FindUnit(state, "breaker", 0);
        var tank = FindUnit(state, "vanguard", 0);
        var rifleman = FindUnit(state, "rifleman", 0);
        var mage = FindUnit(state, "mage", 0);

        Assert.IsNotNull(hero, "주인공 역할이 배치되지 않았습니다.");
        Assert.IsNotNull(tank, "탱커 역할이 배치되지 않았습니다.");
        Assert.IsNotNull(rifleman, "사격수 역할이 배치되지 않았습니다.");
        Assert.IsNotNull(mage, "마도사 역할이 배치되지 않았습니다.");
        Assert.IsTrue(hero.HasTag(SrpUnitTags.ParryUser), "주인공 패링 전용 태그가 없습니다.");
        Assert.IsTrue(HasSkill(hero, "hero_adaptive_heart"), "주인공 전장 적응 패시브가 없습니다.");
        Assert.IsTrue(tank.HasTag(SrpUnitTags.Tank), "탱커 Tank 태그가 없습니다.");
        Assert.AreEqual(SrpStance.Defensive, tank.stance, "탱커가 수비 태세로 시작하지 않습니다.");
        Assert.IsTrue(HasSkill(tank, "tank_line_anchor"), "탱커 전열 고정 패시브가 없습니다.");
        Assert.AreEqual(SrpWeaponClass.Firearm, rifleman.weaponClass, "사격수 무기 분류가 총기가 아닙니다.");
        Assert.GreaterOrEqual(rifleman.attackRange, 4, "사격수 오버워치 확인 사거리가 부족합니다.");
        Assert.AreEqual(1, rifleman.maxAmmo, "사격수 총기 탄창은 1발이어야 합니다.");
        Assert.IsTrue(HasSkill(rifleman, "rifle_exposed_punisher"), "사격수 노출 처벌 패시브가 없습니다.");
        Assert.IsTrue(HasSkill(rifleman, "kill_order"), "사격수 사살 지시가 없습니다.");
        Assert.IsTrue(HasSkill(mage, "tactical_mark"), "마도사 전술 표식이 없습니다.");
        Assert.IsTrue(HasSkill(mage, "balance_hex"), "마도사 균형 교란이 없습니다.");
        Assert.IsTrue(HasSkill(mage, "arcane_screen"), "마도사 전장 장막이 없습니다.");

        var marksman = FindUnit(state, "opening_marksman", 1);
        var raider = FindUnit(state, "opening_raider", 1);
        var bulwark = FindUnit(state, "opening_bulwark", 1);
        var skirmisher = FindUnit(state, "opening_skirmisher", 1);
        var officer = FindUnit(state, "opening_officer", 1);

        Assert.IsNotNull(marksman, "총기 압박병이 배치되지 않았습니다.");
        Assert.IsNotNull(raider, "근접 돌입병이 배치되지 않았습니다.");
        Assert.IsNotNull(bulwark, "방어형 적이 배치되지 않았습니다.");
        Assert.IsNotNull(skirmisher, "측면 교란병이 배치되지 않았습니다.");
        Assert.IsNotNull(officer, "전술 장교가 배치되지 않았습니다.");
        Assert.AreEqual(SrpWeaponClass.Firearm, marksman.weaponClass, "총기 압박병이 총기 역할이 아닙니다.");
        Assert.GreaterOrEqual(marksman.attackRange, 5, "총기 압박병 장거리 사선 압박이 부족합니다.");
        Assert.AreEqual(SrpWeaponClass.Melee, raider.weaponClass, "근접 돌입병이 근접 역할이 아닙니다.");
        Assert.GreaterOrEqual(raider.moveRange, 5, "근접 돌입병 돌입 이동력이 부족합니다.");
        Assert.IsTrue(bulwark.HasTag(SrpUnitTags.Tank), "방어형 적이 PG 붕괴 대상으로 충분히 단단하지 않습니다.");
        Assert.AreEqual(SrpStance.Defensive, bulwark.stance, "방어형 적이 수비 태세가 아닙니다.");
        Assert.IsTrue(HasSkill(bulwark, "cleave"), "방어형 적이 패링 텔레그래프 확인 스킬을 들고 있지 않습니다.");
        Assert.IsTrue(HasSkill(officer, "tactical_mark"), "전술 장교가 표식 스킬을 들고 있지 않습니다.");
        Assert.IsTrue(HasSkill(officer, "kill_order"), "전술 장교가 사살 지시 스킬을 들고 있지 않습니다.");

        bool foundLineBlockingCover = false;
        bool foundCoverWithStats = false;
        foreach (var segment in state.CoverSegments)
        {
            if (segment == null)
                continue;
            if (segment.blocksLineOfSight)
                foundLineBlockingCover = true;
            if (segment.coverDef > 0 || segment.coverGrd > 0)
                foundCoverWithStats = true;
        }
        Assert.IsTrue(foundCoverWithStats, "첫 전투 프리셋에 전술적 엄폐 segment가 없습니다.");
        Assert.IsTrue(foundLineBlockingCover, "첫 전투 프리셋에 사선 차단 segment가 없습니다.");
        Assert.IsTrue(state.HasAdjacentCover(marksman), "총기 압박병이 방향성 엄폐를 실제로 사용할 수 없습니다.");
        Assert.IsTrue(state.SkillLookup["cleave"].isParryable, "강타가 패링 가능 스킬로 등록되어 있지 않습니다.");
        Assert.IsTrue(SrpOverwatch.CanArm(rifleman), "사격수가 오버워치를 예약할 수 있는 초기 상태가 아닙니다.");
    }

    [Test]
    public void SkillOverclock_CanOverclockOnlyWhenResourceCanRecover()
    {
        var unit = new SrpUnitRuntime
        {
            displayName = "Overclock Tester",
            frozenHeart = 5,
        };
        var data = new SrpSkillData
        {
            id = "charge_restore",
            maxCharges = 2,
            chargeRecoveryTurns = 2,
            overclockFrozenHeartCost = 5,
            overclockChargeRestore = 1,
        };
        var runtime = new SrpSkillRuntime(data.id)
        {
            chargesRemaining = 2,
            chargesInitialized = true,
        };

        Assert.IsFalse(SrpSkills.CanOverclockSkill(unit, data, runtime), "회복할 충전이 없는데 오버클럭 가능으로 판정되었습니다.");

        runtime.chargesRemaining = 1;

        Assert.IsTrue(SrpSkills.CanOverclockSkill(unit, data, runtime), "회복할 충전이 있는데 오버클럭 불가로 판정되었습니다.");
        Assert.IsTrue(SrpSkills.TryOverclockSkill(unit, data, runtime, null), "오버클럭 실행 실패");
        Assert.AreEqual(0, unit.frozenHeart, "오버클럭 비용이 소모되지 않았습니다.");
        Assert.AreEqual(2, runtime.chargesRemaining, "오버클럭 충전 복구가 반영되지 않았습니다.");
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

    static SrpUnitRuntime FindUnit(SrpBattleState state, string templateId, int owner)
    {
        foreach (var unit in state.Units)
            if (unit.templateId == templateId && unit.owner == owner)
                return unit;
        return null;
    }

    static bool HasSkill(SrpUnitRuntime unit, string skillId)
    {
        if (unit == null || unit.skillIds == null)
            return false;
        foreach (var id in unit.skillIds)
            if (id == skillId)
                return true;
        return false;
    }
}
