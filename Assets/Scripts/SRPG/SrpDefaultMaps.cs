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
    /// - 장애물로 인한 HUD 상태 변화 확인
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
                skillIds = new string[0],
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
                skillIds = new string[0],
                maxSkills = 4,
                tags = (int)(SrpUnitTags.ParryUser | SrpUnitTags.Tank),
            },
            new SrpUnitTemplateData
            {
                id = "breaker",
                displayName = "근접 투사",
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
                skillIds = new string[0],
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
                maxPg = 14,
                maxActionPoints = 2,
                maxReactionPoints = 1,
                speed = 9,
                weaponClass = SrpWeaponClass.Magic,
                stance = SrpStance.Aggressive,
                facing = SrpFacing.East,
                skillIds = new string[0],
                maxSkills = 4,
                tags = 0,
            },
        };

        var placements = new[]
        {
            // Owner 0
            new SrpPlacementData { templateId = "rifleman", owner = 0, x = 1, y = 2, footprint = new SrpOffset[0] },
            new SrpPlacementData { templateId = "breaker", owner = 0, x = 2, y = 4, footprint = new SrpOffset[0] },
            new SrpPlacementData { templateId = "mage", owner = 0, x = 1, y = 6, footprint = new SrpOffset[0] },

            // Owner 1
            new SrpPlacementData { templateId = "rifleman", owner = 1, x = 10, y = 5, footprint = new SrpOffset[0] },
            new SrpPlacementData { templateId = "vanguard", owner = 1, x = 9, y = 3, footprint = new SrpOffset[0] },
            new SrpPlacementData { templateId = "breaker", owner = 1, x = 10, y = 1, footprint = new SrpOffset[0] },
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
