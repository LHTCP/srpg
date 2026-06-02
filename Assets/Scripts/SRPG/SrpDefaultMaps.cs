using UnityEngine;

/// <summary>
/// 내장 QA 맵 프리셋.
/// </summary>
public static class SrpDefaultMaps
{
    public static SrpMapFileV1 GetPreset(SrpMapPreset preset)
    {
        switch (preset)
        {
            case SrpMapPreset.M1OpeningPrototype:
                return CreateM1OpeningPrototype();
            case SrpMapPreset.M1EngagementLab:
                return CreateM1EngagementLab();
            case SrpMapPreset.M1QaIntegrated:
            default:
                return CreateM1QaIntegrated();
        }
    }

    /// <summary>
    /// M1 통합 QA 프리셋.
    /// - 속도 기반 턴 순환
    /// - 총기(HP 압박) / 근접(PG 압박) 비교
    /// - 장애물/유닛 차단을 통한 오버워치 사선 확인
    /// - 스킬 자원, 패링 텔레그래프, 측후면 노출 확인
    /// </summary>
    public static SrpMapFileV1 CreateM1QaIntegrated()
    {
        int w = 12, h = 8;
        int n = w * h;
        var walk = new bool[n];
        for (int i = 0; i < n; i++)
            walk[i] = true;

        // 중앙 2열 엄폐/장애물 벽 + 통로
        for (int y = 1; y < h - 1; y++)
        {
            if (y == 2 || y == 5)
                continue; // 이동/사격 통로
            walk[5 + y * w] = false;
            walk[6 + y * w] = false;
        }

        var templates = new[]
        {
            new SrpUnitTemplateData
            {
                id = "rifleman",
                displayName = "사격수",
                moveRange = 4,
                attackRange = 4,
                attackPower = 8,
                maxHp = 30,
                maxPg = 16,
                maxActionPoints = 2,
                maxReactionPoints = 1,
                speed = 12,
                weaponClass = SrpWeaponClass.Firearm,
                stance = SrpStance.Aggressive,
                facing = SrpFacing.East,
                skillIds = new[] { "rifle_exposed_punisher", "kill_order", "heal_light" },
                maxSkills = 4,
                tags = 0,
            },
            new SrpUnitTemplateData
            {
                id = "vanguard",
                displayName = "탱커",
                moveRange = 4,
                attackRange = 1,
                attackPower = 9,
                maxHp = 38,
                maxPg = 24,
                maxActionPoints = 2,
                maxReactionPoints = 1,
                speed = 8,
                weaponClass = SrpWeaponClass.Melee,
                stance = SrpStance.Defensive,
                facing = SrpFacing.West,
                skillIds = new[] { "tank_line_anchor", "cleave" },
                maxSkills = 4,
                tags = (int)SrpUnitTags.Tank,
            },
            new SrpUnitTemplateData
            {
                id = "breaker",
                displayName = "주인공",
                moveRange = 5,
                attackRange = 1,
                attackPower = 11,
                maxHp = 32,
                maxPg = 20,
                maxActionPoints = 2,
                maxReactionPoints = 1,
                speed = 10,
                weaponClass = SrpWeaponClass.Melee,
                stance = SrpStance.Aggressive,
                facing = SrpFacing.East,
                skillIds = new[] { "hero_adaptive_heart", "cleave" },
                maxSkills = 4,
                tags = (int)SrpUnitTags.ParryUser,
            },
            new SrpUnitTemplateData
            {
                id = "mage",
                displayName = "마도사",
                moveRange = 4,
                attackRange = 3,
                attackPower = 7,
                maxHp = 26,
                maxPg = 14,
                maxActionPoints = 2,
                maxReactionPoints = 1,
                speed = 9,
                weaponClass = SrpWeaponClass.Magic,
                stance = SrpStance.Aggressive,
                facing = SrpFacing.East,
                skillIds = new[] { "mage_field_theory", "tactical_mark", "balance_hex", "arcane_screen" },
                maxSkills = 4,
                tags = 0,
            },
        };

        var placements = new[]
        {
            // Owner 0
            new SrpPlacementData { templateId = "rifleman", owner = 0, x = 1, y = 2, footprint = new SrpOffset[0] },
            new SrpPlacementData { templateId = "vanguard", owner = 0, x = 1, y = 4, footprint = new SrpOffset[0] },
            new SrpPlacementData { templateId = "breaker", owner = 0, x = 2, y = 4, footprint = new SrpOffset[0] },
            new SrpPlacementData { templateId = "mage", owner = 0, x = 1, y = 6, footprint = new SrpOffset[0] },

            // Owner 1
            new SrpPlacementData { templateId = "rifleman", owner = 1, x = 10, y = 5, footprint = new SrpOffset[0] },
            new SrpPlacementData { templateId = "vanguard", owner = 1, x = 9, y = 3, footprint = new SrpOffset[0] },
            new SrpPlacementData { templateId = "breaker", owner = 1, x = 10, y = 1, footprint = new SrpOffset[0] },
            new SrpPlacementData { templateId = "mage", owner = 1, x = 9, y = 6, footprint = new SrpOffset[0] },
        };

        var interactionPoints = new[]
        {
            new SrpInteractionPointData
            {
                id = "qa_console",
                displayName = "전술 단말",
                x = 2,
                y = 2,
                owner = -1,
                requiredOwner = 0,
                singleUse = true,
                activated = false,
            },
        };

        var coverSegments = new[]
        {
            new SrpCoverSegmentData
            {
                x = 1,
                y = 2,
                edge = SrpCoverEdge.East,
                shape = SrpCoverShape.Linear,
                coverDef = 3,
                coverGrd = 1,
                blocksLineOfSight = false,
            },
            new SrpCoverSegmentData
            {
                x = 9,
                y = 5,
                edge = SrpCoverEdge.West,
                shape = SrpCoverShape.Linear,
                coverDef = 3,
                coverGrd = 1,
                blocksLineOfSight = true,
            },
        };

        return new SrpMapFileV1
        {
            version = 2,
            name = "m1_qa_integrated",
            width = w,
            height = h,
            walkable = walk,
            playerOrder = new[] { 0, 1 },
            templates = templates,
            placements = placements,
            interactionPoints = interactionPoints,
            coverSegments = coverSegments,
        };
    }

    /// <summary>
    /// 첫 전투 프로토타입 프리셋.
    /// - QA용 대칭 검증이 아니라 6~10턴 안에 판단 가능한 소형 전술 문제로 구성한다.
    /// - 북쪽 사격 루트와 남쪽 돌입/상호작용 루트를 분리하고, 사선 차단 엄폐로 총기 압박을 조절한다.
    /// - 승리 시스템은 전멸 유지지만, 동쪽 장교를 전술 목표처럼 느끼도록 배치한다.
    /// </summary>
    public static SrpMapFileV1 CreateM1OpeningPrototype()
    {
        int w = 12, h = 9;
        int n = w * h;
        var walk = new bool[n];
        for (int i = 0; i < n; i++)
            walk[i] = true;

        // 중앙 폐허. 북쪽 사격로와 남쪽 돌입로를 만들고, 중앙 직선 돌파는 좁게 만든다.
        walk[4 + 3 * w] = false;
        walk[5 + 3 * w] = false;
        walk[6 + 3 * w] = false;
        walk[4 + 5 * w] = false;
        walk[5 + 5 * w] = false;
        walk[6 + 5 * w] = false;

        var templates = new[]
        {
            new SrpUnitTemplateData
            {
                id = "breaker",
                displayName = "주인공",
                moveRange = 5,
                attackRange = 1,
                attackPower = 11,
                maxHp = 34,
                maxPg = 22,
                maxActionPoints = 2,
                maxReactionPoints = 1,
                speed = 10,
                weaponClass = SrpWeaponClass.Melee,
                stance = SrpStance.Aggressive,
                facing = SrpFacing.East,
                skillIds = new[] { "hero_adaptive_heart", "cleave" },
                maxSkills = 4,
                tags = (int)SrpUnitTags.ParryUser,
            },
            new SrpUnitTemplateData
            {
                id = "vanguard",
                displayName = "전열 탱커",
                moveRange = 4,
                attackRange = 1,
                attackPower = 9,
                maxHp = 42,
                maxPg = 28,
                maxActionPoints = 2,
                maxReactionPoints = 1,
                speed = 8,
                weaponClass = SrpWeaponClass.Melee,
                stance = SrpStance.Defensive,
                facing = SrpFacing.East,
                skillIds = new[] { "tank_line_anchor", "cleave" },
                maxSkills = 4,
                tags = (int)SrpUnitTags.Tank,
            },
            new SrpUnitTemplateData
            {
                id = "rifleman",
                displayName = "사격수",
                moveRange = 4,
                attackRange = 5,
                attackPower = 8,
                maxAmmo = 1,
                maxHp = 28,
                maxPg = 16,
                maxActionPoints = 2,
                maxReactionPoints = 1,
                speed = 12,
                weaponClass = SrpWeaponClass.Firearm,
                stance = SrpStance.Aggressive,
                facing = SrpFacing.East,
                skillIds = new[] { "rifle_exposed_punisher", "kill_order" },
                maxSkills = 4,
                tags = 0,
            },
            new SrpUnitTemplateData
            {
                id = "mage",
                displayName = "마도사",
                moveRange = 4,
                attackRange = 3,
                attackPower = 7,
                maxHp = 26,
                maxPg = 15,
                maxActionPoints = 2,
                maxReactionPoints = 1,
                speed = 9,
                weaponClass = SrpWeaponClass.Magic,
                stance = SrpStance.Aggressive,
                facing = SrpFacing.East,
                skillIds = new[] { "mage_field_theory", "tactical_mark", "balance_hex", "arcane_screen" },
                maxSkills = 4,
                tags = 0,
            },
            new SrpUnitTemplateData
            {
                id = "opening_marksman",
                displayName = "총기 압박병",
                moveRange = 3,
                attackRange = 5,
                attackPower = 7,
                maxAmmo = 1,
                maxHp = 24,
                maxPg = 14,
                maxActionPoints = 2,
                maxReactionPoints = 1,
                speed = 11,
                weaponClass = SrpWeaponClass.Firearm,
                stance = SrpStance.Aggressive,
                facing = SrpFacing.West,
                skillIds = new string[0],
                maxSkills = 4,
                tags = 0,
            },
            new SrpUnitTemplateData
            {
                id = "opening_raider",
                displayName = "근접 돌입병",
                moveRange = 5,
                attackRange = 1,
                attackPower = 8,
                maxHp = 28,
                maxPg = 18,
                maxActionPoints = 2,
                maxReactionPoints = 1,
                speed = 11,
                weaponClass = SrpWeaponClass.Melee,
                stance = SrpStance.Aggressive,
                facing = SrpFacing.West,
                skillIds = new string[0],
                maxSkills = 4,
                tags = 0,
            },
            new SrpUnitTemplateData
            {
                id = "opening_bulwark",
                displayName = "방어형 적",
                moveRange = 3,
                attackRange = 1,
                attackPower = 8,
                maxHp = 38,
                maxPg = 28,
                maxActionPoints = 2,
                maxReactionPoints = 1,
                speed = 7,
                weaponClass = SrpWeaponClass.Melee,
                stance = SrpStance.Defensive,
                facing = SrpFacing.West,
                skillIds = new[] { "cleave" },
                maxSkills = 4,
                tags = (int)SrpUnitTags.Tank,
            },
            new SrpUnitTemplateData
            {
                id = "opening_skirmisher",
                displayName = "측면 교란병",
                moveRange = 4,
                attackRange = 1,
                attackPower = 7,
                maxHp = 24,
                maxPg = 16,
                maxActionPoints = 2,
                maxReactionPoints = 1,
                speed = 10,
                weaponClass = SrpWeaponClass.Melee,
                stance = SrpStance.Aggressive,
                facing = SrpFacing.West,
                skillIds = new string[0],
                maxSkills = 4,
                tags = 0,
            },
            new SrpUnitTemplateData
            {
                id = "opening_officer",
                displayName = "전술 장교",
                moveRange = 3,
                attackRange = 3,
                attackPower = 7,
                maxHp = 30,
                maxPg = 18,
                maxActionPoints = 2,
                maxReactionPoints = 1,
                speed = 9,
                weaponClass = SrpWeaponClass.Magic,
                stance = SrpStance.Aggressive,
                facing = SrpFacing.West,
                skillIds = new[] { "tactical_mark", "kill_order" },
                maxSkills = 4,
                tags = 0,
            },
        };

        var placements = new[]
        {
            // Owner 0: 첫 4인 파티.
            new SrpPlacementData { templateId = "rifleman", owner = 0, x = 1, y = 2, footprint = new SrpOffset[0] },
            new SrpPlacementData { templateId = "vanguard", owner = 0, x = 2, y = 4, footprint = new SrpOffset[0] },
            new SrpPlacementData { templateId = "breaker", owner = 0, x = 1, y = 5, footprint = new SrpOffset[0] },
            new SrpPlacementData { templateId = "mage", owner = 0, x = 1, y = 6, footprint = new SrpOffset[0] },

            // Owner 1: 비대칭 적 역할.
            new SrpPlacementData { templateId = "opening_marksman", owner = 1, x = 9, y = 2, footprint = new SrpOffset[0] },
            new SrpPlacementData { templateId = "opening_bulwark", owner = 1, x = 8, y = 4, footprint = new SrpOffset[0] },
            new SrpPlacementData { templateId = "opening_raider", owner = 1, x = 7, y = 6, footprint = new SrpOffset[0] },
            new SrpPlacementData { templateId = "opening_skirmisher", owner = 1, x = 9, y = 6, footprint = new SrpOffset[0] },
            new SrpPlacementData { templateId = "opening_officer", owner = 1, x = 10, y = 5, footprint = new SrpOffset[0] },
        };

        var interactionPoints = new[]
        {
            new SrpInteractionPointData
            {
                id = "opening_signal_crank",
                displayName = "신호 장치",
                x = 4,
                y = 6,
                owner = -1,
                requiredOwner = 0,
                singleUse = true,
                activated = false,
            },
        };

        var coverSegments = new[]
        {
            new SrpCoverSegmentData
            {
                x = 3,
                y = 2,
                edge = SrpCoverEdge.East,
                shape = SrpCoverShape.Linear,
                coverDef = 3,
                coverGrd = 1,
                blocksLineOfSight = false,
            },
            new SrpCoverSegmentData
            {
                x = 9,
                y = 2,
                edge = SrpCoverEdge.West,
                shape = SrpCoverShape.Linear,
                coverDef = 4,
                coverGrd = 2,
                blocksLineOfSight = true,
            },
            new SrpCoverSegmentData
            {
                x = 7,
                y = 6,
                edge = SrpCoverEdge.West,
                shape = SrpCoverShape.Corner,
                coverDef = 2,
                coverGrd = 2,
                blocksLineOfSight = false,
            },
        };

        return new SrpMapFileV1
        {
            version = 2,
            name = "m1_opening_prototype",
            width = w,
            height = h,
            walkable = walk,
            playerOrder = new[] { 0, 1 },
            templates = templates,
            placements = placements,
            interactionPoints = interactionPoints,
            coverSegments = coverSegments,
        };
    }

    /// <summary>
    /// 교전/둘러싸임 QA 프리셋.
    /// - 탱커가 두 적에게 인접한 상태로 시작해 다중 교전 완충을 확인한다.
    /// - 서쪽으로 한 칸 이탈하면 교전 이탈 비용/기회공격을 확인할 수 있다.
    /// - 사격수는 오버워치 예약과 위험 범위 확인용으로 배치한다.
    /// </summary>
    public static SrpMapFileV1 CreateM1EngagementLab()
    {
        int w = 8, h = 6;
        int n = w * h;
        var walk = new bool[n];
        for (int i = 0; i < n; i++)
            walk[i] = true;

        // 좌측 통로와 우측 교전 구역을 느슨하게 분리한다.
        walk[3 + 0 * w] = false;
        walk[3 + 1 * w] = false;
        walk[3 + 4 * w] = false;
        walk[3 + 5 * w] = false;

        var templates = new[]
        {
            new SrpUnitTemplateData
            {
                id = "engage_tank",
                displayName = "교전 탱커",
                moveRange = 4,
                attackRange = 1,
                attackPower = 8,
                maxHp = 52,
                maxPg = 36,
                maxActionPoints = 2,
                maxReactionPoints = 1,
                speed = 8,
                weaponClass = SrpWeaponClass.Melee,
                stance = SrpStance.Defensive,
                facing = SrpFacing.East,
                skillIds = new string[0],
                maxSkills = 4,
                tags = (int)(SrpUnitTags.ParryUser | SrpUnitTags.Tank),
            },
            new SrpUnitTemplateData
            {
                id = "engage_guard",
                displayName = "지원 근접병",
                moveRange = 4,
                attackRange = 1,
                attackPower = 7,
                maxHp = 34,
                maxPg = 20,
                maxActionPoints = 2,
                maxReactionPoints = 1,
                speed = 10,
                weaponClass = SrpWeaponClass.Melee,
                stance = SrpStance.Aggressive,
                facing = SrpFacing.East,
                skillIds = new string[0],
                maxSkills = 4,
                tags = 0,
            },
            new SrpUnitTemplateData
            {
                id = "engage_raider",
                displayName = "포위 돌격병",
                moveRange = 4,
                attackRange = 1,
                attackPower = 8,
                maxHp = 30,
                maxPg = 18,
                maxActionPoints = 2,
                maxReactionPoints = 1,
                speed = 11,
                weaponClass = SrpWeaponClass.Melee,
                stance = SrpStance.Aggressive,
                facing = SrpFacing.West,
                skillIds = new string[0],
                maxSkills = 4,
                tags = 0,
            },
            new SrpUnitTemplateData
            {
                id = "engage_flanker",
                displayName = "포위 측면병",
                moveRange = 4,
                attackRange = 1,
                attackPower = 7,
                maxHp = 28,
                maxPg = 18,
                maxActionPoints = 2,
                maxReactionPoints = 1,
                speed = 9,
                weaponClass = SrpWeaponClass.Melee,
                stance = SrpStance.Aggressive,
                facing = SrpFacing.West,
                skillIds = new string[0],
                maxSkills = 4,
                tags = 0,
            },
            new SrpUnitTemplateData
            {
                id = "engage_overwatch",
                displayName = "오버워치 사격수",
                moveRange = 3,
                attackRange = 4,
                attackPower = 8,
                maxHp = 28,
                maxPg = 16,
                maxActionPoints = 2,
                maxReactionPoints = 1,
                speed = 7,
                weaponClass = SrpWeaponClass.Firearm,
                stance = SrpStance.Aggressive,
                facing = SrpFacing.West,
                skillIds = new string[0],
                maxSkills = 4,
                tags = 0,
            },
        };

        var placements = new[]
        {
            // Owner 0: 탱커가 이미 두 적에게 인접해 다중 교전 상태로 시작한다.
            new SrpPlacementData { templateId = "engage_tank", owner = 0, x = 3, y = 2, footprint = new SrpOffset[0] },
            new SrpPlacementData { templateId = "engage_guard", owner = 0, x = 1, y = 2, footprint = new SrpOffset[0] },

            // Owner 1
            new SrpPlacementData { templateId = "engage_raider", owner = 1, x = 4, y = 2, footprint = new SrpOffset[0] },
            new SrpPlacementData { templateId = "engage_flanker", owner = 1, x = 3, y = 3, footprint = new SrpOffset[0] },
            new SrpPlacementData { templateId = "engage_overwatch", owner = 1, x = 6, y = 2, footprint = new SrpOffset[0] },
        };

        return new SrpMapFileV1
        {
            version = 2,
            name = "m1_engagement_lab",
            width = w,
            height = h,
            walkable = walk,
            playerOrder = new[] { 0, 1 },
            templates = templates,
            placements = placements,
        };
    }
}
