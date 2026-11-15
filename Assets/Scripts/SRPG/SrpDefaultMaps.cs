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
            case SrpMapPreset.M1ObjectiveLab:
                return CreateM1ObjectiveLab();
            case SrpMapPreset.M1BarricadeAssault:
                return CreateM1BarricadeAssault();
            case SrpMapPreset.M1ConditionLab:
                return CreateM1ConditionLab();
            case SrpMapPreset.M1QaIntegrated:
            default:
                return CreateM1QaIntegrated();
        }
    }

    /// <summary>
    /// M1 통합 QA 프리셋.
    /// - 속도 기반 턴 순환
    /// - 총기(HP 압박) / 근접(PG 압박) 비교
    /// - 장애물/유닛 차단을 통한 경계태세 사선 확인
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

        var coverObjects = new[]
        {
            new SrpCoverObjectData { x = 5, y = 1, coverDef = 2, coverGrd = 1, blocksLineOfSight = true, visualKey = "qa_ruin" },
            new SrpCoverObjectData { x = 6, y = 1, coverDef = 2, coverGrd = 1, blocksLineOfSight = true, visualKey = "qa_ruin" },
            new SrpCoverObjectData { x = 5, y = 3, coverDef = 2, coverGrd = 1, blocksLineOfSight = true, visualKey = "qa_ruin" },
            new SrpCoverObjectData { x = 6, y = 3, coverDef = 2, coverGrd = 1, blocksLineOfSight = true, visualKey = "qa_ruin" },
            new SrpCoverObjectData { x = 5, y = 4, coverDef = 2, coverGrd = 1, blocksLineOfSight = true, visualKey = "qa_ruin" },
            new SrpCoverObjectData { x = 6, y = 4, coverDef = 2, coverGrd = 1, blocksLineOfSight = true, visualKey = "qa_ruin" },
            new SrpCoverObjectData { x = 5, y = 6, coverDef = 2, coverGrd = 1, blocksLineOfSight = true, visualKey = "qa_ruin" },
            new SrpCoverObjectData { x = 6, y = 6, coverDef = 2, coverGrd = 1, blocksLineOfSight = true, visualKey = "qa_ruin" },
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
            coverObjects = coverObjects,
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

        var coverObjects = new[]
        {
            new SrpCoverObjectData { x = 4, y = 3, coverDef = 3, coverGrd = 1, blocksLineOfSight = true, visualKey = "opening_central_ruin" },
            new SrpCoverObjectData { x = 5, y = 3, coverDef = 3, coverGrd = 1, blocksLineOfSight = true, visualKey = "opening_central_ruin" },
            new SrpCoverObjectData { x = 6, y = 3, coverDef = 3, coverGrd = 1, blocksLineOfSight = true, visualKey = "opening_central_ruin" },
            new SrpCoverObjectData { x = 4, y = 5, coverDef = 3, coverGrd = 1, blocksLineOfSight = true, visualKey = "opening_central_ruin" },
            new SrpCoverObjectData { x = 5, y = 5, coverDef = 3, coverGrd = 1, blocksLineOfSight = true, visualKey = "opening_central_ruin" },
            new SrpCoverObjectData { x = 6, y = 5, coverDef = 3, coverGrd = 1, blocksLineOfSight = true, visualKey = "opening_central_ruin" },
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
            coverObjects = coverObjects,
            coverSegments = coverSegments,
        };
    }

    /// <summary>
    /// 교전/둘러싸임 QA 프리셋.
    /// - 탱커가 두 적에게 인접한 상태로 시작해 다중 교전 완충을 확인한다.
    /// - 서쪽으로 한 칸 이탈하면 교전 이탈 비용/기회공격을 확인할 수 있다.
    /// - 사격수는 경계태세 예약과 위험 범위 확인용으로 배치한다.
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
                displayName = "경계태세 사격수",
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

    /// <summary>
    /// 승리 조건 QA 프리셋.
    /// - 적 전멸 외에 지휘관 처치와 상호작용 목표(신호 장치) 승리를 고정 조건에서 확인한다.
    /// - 양 진영에 지휘관이 한 명씩 있고, 신호 장치는 owner 0만 쓸 수 있다.
    /// - 수치는 판정 확인용 임시값이며 첫 전투(M1OpeningPrototype) 승리 조건은 바꾸지 않는다.
    /// </summary>
    public static SrpMapFileV1 CreateM1ObjectiveLab()
    {
        int w = 9, h = 5;
        int n = w * h;
        var walk = new bool[n];
        for (int i = 0; i < n; i++)
            walk[i] = true;

        // 중앙 기둥으로 정면 통로와 남쪽 신호 장치 루트를 나눈다.
        walk[4 + 1 * w] = false;
        walk[4 + 3 * w] = false;

        var templates = new[]
        {
            new SrpUnitTemplateData
            {
                id = "objective_leader",
                displayName = "아군 지휘관",
                moveRange = 4,
                attackRange = 1,
                attackPower = 9,
                maxHp = 36,
                maxPg = 22,
                maxActionPoints = 2,
                maxReactionPoints = 1,
                speed = 11,
                weaponClass = SrpWeaponClass.Melee,
                stance = SrpStance.Aggressive,
                facing = SrpFacing.East,
                skillIds = new string[0],
                maxSkills = 4,
                tags = (int)SrpUnitTags.ParryUser,
            },
            new SrpUnitTemplateData
            {
                id = "objective_rifle",
                displayName = "아군 사격수",
                moveRange = 3,
                attackRange = 4,
                attackPower = 8,
                maxHp = 28,
                maxPg = 16,
                maxActionPoints = 2,
                maxReactionPoints = 1,
                speed = 9,
                weaponClass = SrpWeaponClass.Firearm,
                stance = SrpStance.Aggressive,
                facing = SrpFacing.East,
                skillIds = new string[0],
                maxSkills = 4,
                tags = 0,
            },
            new SrpUnitTemplateData
            {
                id = "objective_enemy_officer",
                displayName = "적 지휘관",
                moveRange = 3,
                attackRange = 1,
                attackPower = 7,
                maxHp = 30,
                maxPg = 18,
                maxActionPoints = 2,
                maxReactionPoints = 1,
                speed = 8,
                weaponClass = SrpWeaponClass.Melee,
                stance = SrpStance.Defensive,
                facing = SrpFacing.West,
                skillIds = new string[0],
                maxSkills = 4,
                tags = 0,
            },
            new SrpUnitTemplateData
            {
                id = "objective_enemy_guard",
                displayName = "적 호위병",
                moveRange = 4,
                attackRange = 1,
                attackPower = 8,
                maxHp = 34,
                maxPg = 20,
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
        };

        var placements = new[]
        {
            new SrpPlacementData { templateId = "objective_leader", owner = 0, x = 1, y = 2, footprint = new SrpOffset[0], isCommander = true },
            new SrpPlacementData { templateId = "objective_rifle", owner = 0, x = 0, y = 1, footprint = new SrpOffset[0] },

            new SrpPlacementData { templateId = "objective_enemy_officer", owner = 1, x = 8, y = 2, footprint = new SrpOffset[0], isCommander = true },
            new SrpPlacementData { templateId = "objective_enemy_guard", owner = 1, x = 6, y = 2, footprint = new SrpOffset[0] },
            new SrpPlacementData { templateId = "objective_enemy_guard", owner = 1, x = 7, y = 1, footprint = new SrpOffset[0] },
        };

        var interactionPoints = new[]
        {
            new SrpInteractionPointData
            {
                id = "objective_signal",
                displayName = "신호 장치",
                x = 7,
                y = 4,
                owner = -1,
                requiredOwner = 0,
                singleUse = true,
                activated = false,
                effect = SrpInteractionEffect.VictoryObjective,
            },
        };

        return new SrpMapFileV1
        {
            version = 2,
            name = "m1_objective_lab",
            width = w,
            height = h,
            walkable = walk,
            playerOrder = new[] { 0, 1 },
            templates = templates,
            placements = placements,
            interactionPoints = interactionPoints,
            victory = new SrpVictoryRuleData { commanderKill = true, interactionObjective = true },
        };
    }

    /// <summary>
    /// 추가 조건 검증 랩(8차 추가). 전령 호위 상황으로 추가 승리/패배 조건 네 종류를 한 맵에서 확인한다.
    /// - 승리 조건: 전령이 동쪽 출구 (10,3)에 도달(P0), 중앙 거점 (5,3) 주변 3x3을 3라운드 유지(P0),
    ///   9라운드 버티기(P1. P0에게는 제한 라운드다).
    /// - 패배 조건: 전령을 잃으면 P0 패배.
    /// - 지휘관 처치와 상호작용 목표는 쓰지 않는다. 스킬 없이 기본 행동만으로 조건을 본다.
    /// 수치와 배치는 조건 확인용 임시값(브릿지)이다.
    /// </summary>
    public static SrpMapFileV1 CreateM1ConditionLab()
    {
        int w = 11, h = 7;
        int n = w * h;
        var walk = new bool[n];
        for (int i = 0; i < n; i++)
            walk[i] = true;

        // 거점 양옆의 기둥. 가운데 길(y = 3)은 열어 둔다.
        walk[3 + 1 * w] = false;
        walk[3 + 5 * w] = false;
        walk[7 + 1 * w] = false;
        walk[7 + 5 * w] = false;

        var templates = new[]
        {
            new SrpUnitTemplateData
            {
                id = "lab_courier",
                displayName = "전령",
                moveRange = 4,
                attackRange = 1,
                attackPower = 4,
                maxHp = 24,
                maxPg = 14,
                maxActionPoints = 2,
                maxReactionPoints = 1,
                speed = 12,
                weaponClass = SrpWeaponClass.Melee,
                stance = SrpStance.Defensive,
                facing = SrpFacing.East,
                skillIds = new string[0],
                maxSkills = 4,
                tags = 0,
            },
            new SrpUnitTemplateData
            {
                id = "lab_escort",
                displayName = "호위 검사",
                moveRange = 4,
                attackRange = 1,
                attackPower = 10,
                maxHp = 34,
                maxPg = 22,
                maxActionPoints = 2,
                maxReactionPoints = 1,
                speed = 10,
                weaponClass = SrpWeaponClass.Melee,
                stance = SrpStance.Aggressive,
                facing = SrpFacing.East,
                skillIds = new string[0],
                maxSkills = 4,
                tags = (int)SrpUnitTags.ParryUser,
            },
            new SrpUnitTemplateData
            {
                id = "lab_escort_rifle",
                displayName = "호위 사격수",
                moveRange = 3,
                attackRange = 4,
                attackPower = 8,
                maxHp = 28,
                maxPg = 16,
                maxActionPoints = 2,
                maxReactionPoints = 1,
                speed = 9,
                weaponClass = SrpWeaponClass.Firearm,
                stance = SrpStance.Aggressive,
                facing = SrpFacing.East,
                skillIds = new string[0],
                maxSkills = 4,
                tags = 0,
            },
            new SrpUnitTemplateData
            {
                id = "lab_raider",
                displayName = "추격병",
                moveRange = 4,
                attackRange = 1,
                attackPower = 8,
                maxHp = 30,
                maxPg = 18,
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
                id = "lab_marksman",
                displayName = "저격수",
                moveRange = 3,
                attackRange = 4,
                attackPower = 8,
                maxHp = 26,
                maxPg = 14,
                maxActionPoints = 2,
                maxReactionPoints = 1,
                speed = 8,
                weaponClass = SrpWeaponClass.Firearm,
                stance = SrpStance.Defensive,
                facing = SrpFacing.West,
                skillIds = new string[0],
                maxSkills = 4,
                tags = 0,
            },
        };

        var placements = new[]
        {
            new SrpPlacementData { templateId = "lab_courier", owner = 0, x = 0, y = 3, footprint = new SrpOffset[0] },
            new SrpPlacementData { templateId = "lab_escort", owner = 0, x = 1, y = 2, footprint = new SrpOffset[0] },
            new SrpPlacementData { templateId = "lab_escort", owner = 0, x = 1, y = 4, footprint = new SrpOffset[0] },
            new SrpPlacementData { templateId = "lab_escort_rifle", owner = 0, x = 0, y = 1, footprint = new SrpOffset[0] },

            new SrpPlacementData { templateId = "lab_raider", owner = 1, x = 8, y = 2, footprint = new SrpOffset[0] },
            new SrpPlacementData { templateId = "lab_raider", owner = 1, x = 8, y = 4, footprint = new SrpOffset[0] },
            new SrpPlacementData { templateId = "lab_raider", owner = 1, x = 9, y = 3, footprint = new SrpOffset[0] },
            new SrpPlacementData { templateId = "lab_marksman", owner = 1, x = 10, y = 5, footprint = new SrpOffset[0] },
        };

        return new SrpMapFileV1
        {
            version = 2,
            name = "m1_condition_lab",
            width = w,
            height = h,
            walkable = walk,
            playerOrder = new[] { 0, 1 },
            templates = templates,
            placements = placements,
            victory = new SrpVictoryRuleData
            {
                conditions = new[]
                {
                    new SrpVictoryConditionData
                    {
                        type = SrpVictoryConditionTypes.ReachTile, owner = 0, targetId = "lab_courier", x = 10, y = 3,
                    },
                    new SrpVictoryConditionData
                    {
                        type = SrpVictoryConditionTypes.HoldPoint, owner = 0, x = 5, y = 3, radius = 1, value = 3,
                    },
                    new SrpVictoryConditionData
                    {
                        type = SrpVictoryConditionTypes.SurviveRounds, owner = 1, value = 9,
                    },
                },
                defeatConditions = new[]
                {
                    new SrpVictoryConditionData
                    {
                        type = SrpVictoryConditionTypes.UnitLost, owner = 0, targetId = "lab_courier",
                    },
                },
            },
        };
    }

    /// <summary>
    /// 방벽선 돌파 프리셋(6차 추가).
    /// - 5인 파티(주인공, 탱커, 사격수, 마도사, 투사)가 낮은 방벽선 뒤의 적 6명을 상대한다.
    /// - 방벽은 넘을 수 없고 남쪽·북쪽 틈으로만 넘어갈 수 있다. 가운데 높은 벽은 시야를 막고,
    ///   양 끝의 낮은 방벽은 시야와 사선이 통해 사격로가 된다.
    /// - 승리 조건: 전멸, 적 지휘관(방벽 대장) 처치, 봉화대 점화(플레이어만).
    /// - 범위 스킬(제압 사격, 충격장, 전열 재정비)과 투사의 균열 베기를 쓰는 맵이다.
    /// </summary>
    public static SrpMapFileV1 CreateM1BarricadeAssault()
    {
        int w = 14, h = 9;
        int n = w * h;
        var walk = new bool[n];
        for (int i = 0; i < n; i++)
            walk[i] = true;

        var coverObjects = new System.Collections.Generic.List<SrpCoverObjectData>();

        // x=7의 방벽선. y=2(남쪽 틈), y=6(북쪽 틈)만 열려 있다.
        // 가운데(y=3~5)는 시야를 막는 높은 벽, 양 끝은 시야가 통하는 낮은 방벽이다.
        for (int y = 0; y < h; y++)
        {
            if (y == 2 || y == 6)
                continue;
            walk[7 + y * w] = false;
            bool high = y >= 3 && y <= 5;
            coverObjects.Add(new SrpCoverObjectData { x = 7, y = y, coverDef = high ? 3 : 2, coverGrd = 1, blocksLineOfSight = high, visualKey = high ? "barricade_wall" : "barricade_low" });
        }

        // 시야를 막는 폐허. 서쪽은 접근 엄폐, 동쪽은 지휘관 앞 차폐.
        int[,] ruins = { { 4, 4 }, { 4, 5 }, { 10, 3 } };
        for (int i = 0; i < ruins.GetLength(0); i++)
        {
            int rx = ruins[i, 0];
            int ry = ruins[i, 1];
            walk[rx + ry * w] = false;
            coverObjects.Add(new SrpCoverObjectData { x = rx, y = ry, coverDef = 3, coverGrd = 1, blocksLineOfSight = true, visualKey = "barricade_ruin" });
        }

        var templates = new[]
        {
            BarricadeTemplate("breaker", "주인공", SrpWeaponClass.Melee, SrpStance.Aggressive, SrpFacing.East,
                move: 5, range: 1, attack: 11, hp: 34, pg: 22, speed: 10, ammo: 0, tags: (int)SrpUnitTags.ParryUser,
                skills: new[] { "hero_adaptive_heart", "cleave" }),
            BarricadeTemplate("vanguard", "전열 탱커", SrpWeaponClass.Melee, SrpStance.Defensive, SrpFacing.East,
                move: 4, range: 1, attack: 9, hp: 42, pg: 28, speed: 8, ammo: 0, tags: (int)SrpUnitTags.Tank,
                skills: new[] { "tank_line_anchor", "cleave", "rally_line" }),
            BarricadeTemplate("rifleman", "사격수", SrpWeaponClass.Firearm, SrpStance.Aggressive, SrpFacing.East,
                move: 4, range: 5, attack: 8, hp: 28, pg: 16, speed: 12, ammo: 1, tags: 0,
                skills: new[] { "rifle_exposed_punisher", "kill_order", "suppressing_fire" }),
            BarricadeTemplate("mage", "마도사", SrpWeaponClass.Magic, SrpStance.Aggressive, SrpFacing.East,
                move: 4, range: 3, attack: 7, hp: 26, pg: 15, speed: 9, ammo: 0, tags: 0,
                skills: new[] { "mage_field_theory", "tactical_mark", "shock_field", "arcane_screen" }),
            BarricadeTemplate("duelist", "투사", SrpWeaponClass.Melee, SrpStance.Aggressive, SrpFacing.East,
                move: 5, range: 1, attack: 10, hp: 28, pg: 18, speed: 12, ammo: 0, tags: 0,
                skills: new[] { "duelist_second_wind", "rending_strike" }),

            BarricadeTemplate("barricade_marksman", "방벽 사격병", SrpWeaponClass.Firearm, SrpStance.Aggressive, SrpFacing.West,
                move: 3, range: 5, attack: 7, hp: 24, pg: 14, speed: 11, ammo: 1, tags: 0,
                skills: new string[0]),
            BarricadeTemplate("barricade_sniper", "저격병", SrpWeaponClass.Firearm, SrpStance.Aggressive, SrpFacing.West,
                move: 3, range: 6, attack: 9, hp: 20, pg: 12, speed: 9, ammo: 1, tags: 0,
                skills: new string[0]),
            BarricadeTemplate("barricade_bulwark", "틈새 방패병", SrpWeaponClass.Melee, SrpStance.Defensive, SrpFacing.West,
                move: 3, range: 1, attack: 8, hp: 38, pg: 28, speed: 7, ammo: 0, tags: (int)SrpUnitTags.Tank,
                skills: new[] { "cleave" }),
            BarricadeTemplate("barricade_raider", "돌입병", SrpWeaponClass.Melee, SrpStance.Aggressive, SrpFacing.West,
                move: 5, range: 1, attack: 8, hp: 28, pg: 18, speed: 11, ammo: 0, tags: 0,
                skills: new string[0]),
            BarricadeTemplate("barricade_captain", "방벽 대장", SrpWeaponClass.Melee, SrpStance.Defensive, SrpFacing.West,
                move: 4, range: 1, attack: 9, hp: 40, pg: 26, speed: 9, ammo: 0, tags: 0,
                skills: new[] { "rending_strike" }),
        };

        var placements = new[]
        {
            // Owner 0: 5인 파티. 지휘관을 두지 않는다(앞에 서는 주인공이 지휘관이면 집중 사격에 바로 끝난다).
            new SrpPlacementData { templateId = "breaker", owner = 0, x = 1, y = 4, footprint = new SrpOffset[0] },
            new SrpPlacementData { templateId = "vanguard", owner = 0, x = 2, y = 5, footprint = new SrpOffset[0] },
            new SrpPlacementData { templateId = "rifleman", owner = 0, x = 1, y = 2, footprint = new SrpOffset[0] },
            new SrpPlacementData { templateId = "mage", owner = 0, x = 1, y = 6, footprint = new SrpOffset[0] },
            new SrpPlacementData { templateId = "duelist", owner = 0, x = 2, y = 3, footprint = new SrpOffset[0] },

            // Owner 1: 방벽 수비대. 방벽 대장이 지휘관이다.
            new SrpPlacementData { templateId = "barricade_marksman", owner = 1, x = 8, y = 1, footprint = new SrpOffset[0] },
            new SrpPlacementData { templateId = "barricade_marksman", owner = 1, x = 8, y = 7, footprint = new SrpOffset[0] },
            new SrpPlacementData { templateId = "barricade_bulwark", owner = 1, x = 8, y = 2, footprint = new SrpOffset[0] },
            new SrpPlacementData { templateId = "barricade_raider", owner = 1, x = 9, y = 6, footprint = new SrpOffset[0] },
            new SrpPlacementData { templateId = "barricade_sniper", owner = 1, x = 11, y = 1, footprint = new SrpOffset[0] },
            new SrpPlacementData { templateId = "barricade_captain", owner = 1, x = 11, y = 4, footprint = new SrpOffset[0], isCommander = true },
        };

        var interactionPoints = new[]
        {
            new SrpInteractionPointData
            {
                id = "barricade_beacon",
                displayName = "봉화대",
                x = 12,
                y = 4,
                owner = -1,
                requiredOwner = 0,
                singleUse = true,
                activated = false,
                effect = SrpInteractionEffect.VictoryObjective,
            },
        };

        var coverSegments = new[]
        {
            // 틈 앞의 접근 엄폐(서쪽)와 틈 뒤의 수비 엄폐(동쪽).
            new SrpCoverSegmentData { x = 5, y = 2, edge = SrpCoverEdge.East, shape = SrpCoverShape.Linear, coverDef = 3, coverGrd = 1, blocksLineOfSight = false },
            new SrpCoverSegmentData { x = 5, y = 6, edge = SrpCoverEdge.East, shape = SrpCoverShape.Linear, coverDef = 3, coverGrd = 1, blocksLineOfSight = false },
            new SrpCoverSegmentData { x = 9, y = 2, edge = SrpCoverEdge.West, shape = SrpCoverShape.Linear, coverDef = 3, coverGrd = 2, blocksLineOfSight = false },
            new SrpCoverSegmentData { x = 9, y = 6, edge = SrpCoverEdge.West, shape = SrpCoverShape.Linear, coverDef = 3, coverGrd = 2, blocksLineOfSight = false },
        };

        return new SrpMapFileV1
        {
            version = 2,
            name = "m1_barricade_assault",
            width = w,
            height = h,
            walkable = walk,
            playerOrder = new[] { 0, 1 },
            templates = templates,
            placements = placements,
            interactionPoints = interactionPoints,
            coverObjects = coverObjects.ToArray(),
            coverSegments = coverSegments,
            victory = new SrpVictoryRuleData { commanderKill = true, interactionObjective = true },
        };
    }

    static SrpUnitTemplateData BarricadeTemplate(
        string id, string displayName, SrpWeaponClass weaponClass, SrpStance stance, SrpFacing facing,
        int move, int range, int attack, int hp, int pg, int speed, int ammo, int tags, string[] skills)
    {
        return new SrpUnitTemplateData
        {
            id = id,
            displayName = displayName,
            moveRange = move,
            attackRange = range,
            attackPower = attack,
            maxAmmo = ammo,
            maxHp = hp,
            maxPg = pg,
            maxActionPoints = 2,
            maxReactionPoints = 1,
            speed = speed,
            weaponClass = weaponClass,
            stance = stance,
            facing = facing,
            skillIds = skills,
            maxSkills = 4,
            tags = tags,
        };
    }
}
