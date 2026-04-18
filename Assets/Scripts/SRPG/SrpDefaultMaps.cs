using UnityEngine;

/// <summary>
/// 내장 QA 맵 프리셋.
/// </summary>
public static class SrpDefaultMaps
{
    public static SrpMapFileV1 GetPreset(SrpMapPreset preset)
    {
        return CreateM1QaIntegrated();
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
                tags = 0,
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
}
