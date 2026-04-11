using UnityEngine;

/// <summary>
/// 내장 샘플 맵·템플릿 (코드 생성).
/// </summary>
public static class SrpDefaultMaps
{
    public static SrpMapFileV1 GetPreset(SrpMapPreset preset)
    {
        switch (preset)
        {
            case SrpMapPreset.TinyDuel:
                return CreateTinyDuel();
            case SrpMapPreset.Corridor:
                return CreateCorridor();
            case SrpMapPreset.Skirmish:
            default:
                return CreateSampleSkirmish();
        }
    }

    /// <summary>6×4, 1vs1 기사 — 입력·이동 검증용.</summary>
    public static SrpMapFileV1 CreateTinyDuel()
    {
        int w = 6, h = 4;
        int n = w * h;
        var walk = new bool[n];
        for (int i = 0; i < n; i++)
            walk[i] = true;

        var knight = new SrpUnitTemplateData
        {
            id = "knight",
            displayName = "기사",
            moveRange = 5,
            attackRange = 1,
            attackPower = 12,
            maxHp = 40,
            maxAp = 15,
            maxPosture = 80,
            skillIds = new string[0],
            maxSkills = 4,
            frozenHeart = 0,
            tags = 0,
        };

        return new SrpMapFileV1
        {
            version = 1,
            name = "tiny_duel",
            width = w,
            height = h,
            walkable = walk,
            playerOrder = new[] { 0, 1 },
            templates = new[] { knight },
            placements = new[]
            {
                new SrpPlacementData { templateId = "knight", owner = 0, x = 1, y = 1, footprint = new SrpOffset[0] },
                new SrpPlacementData { templateId = "knight", owner = 1, x = 4, y = 2, footprint = new SrpOffset[0] },
            },
        };
    }

    /// <summary>8×10, 중앙 장애물 띠(ZOC·우회 검증용).</summary>
    public static SrpMapFileV1 CreateCorridor()
    {
        int w = 8, h = 10;
        int n = w * h;
        var walk = new bool[n];
        for (int i = 0; i < n; i++)
            walk[i] = true;
        for (int y = 0; y < h; y++)
        {
            if (y == 5)
                continue;
            walk[3 + y * w] = false;
            walk[4 + y * w] = false;
        }

        var templates = new[]
        {
            new SrpUnitTemplateData
            {
                id = "knight",
                displayName = "기사",
                moveRange = 5,
                attackRange = 1,
                attackPower = 12,
                maxHp = 40,
                maxAp = 15,
                maxPosture = 80,
                skillIds = new string[0],
                maxSkills = 4,
                frozenHeart = 0,
                tags = 0,
            },
            new SrpUnitTemplateData
            {
                id = "archer",
                displayName = "궁수",
                moveRange = 4,
                attackRange = 3,
                attackPower = 9,
                maxHp = 28,
                maxAp = 8,
                maxPosture = 60,
                skillIds = new string[0],
                maxSkills = 4,
                frozenHeart = 0,
                tags = 0,
            },
        };

        return new SrpMapFileV1
        {
            version = 1,
            name = "corridor",
            width = w,
            height = h,
            walkable = walk,
            playerOrder = new[] { 0, 1 },
            templates = templates,
            placements = new[]
            {
                new SrpPlacementData { templateId = "knight", owner = 0, x = 1, y = 3, footprint = new SrpOffset[0] },
                new SrpPlacementData { templateId = "archer", owner = 0, x = 0, y = 7, footprint = new SrpOffset[0] },
                new SrpPlacementData { templateId = "knight", owner = 1, x = 6, y = 4, footprint = new SrpOffset[0] },
                new SrpPlacementData { templateId = "archer", owner = 1, x = 7, y = 8, footprint = new SrpOffset[0] },
            },
        };
    }

    public static SrpMapFileV1 CreateSampleSkirmish()
    {
        int w = 10, h = 8;
        int n = w * h;
        var walk = new bool[n];
        for (int i = 0; i < n; i++)
            walk[i] = true;
        walk[3 + 2 * w] = false;
        walk[4 + 2 * w] = false;
        walk[3 + 3 * w] = false;
        walk[4 + 3 * w] = false;

        var templates = new[]
        {
            new SrpUnitTemplateData
            {
                id = "knight",
                displayName = "기사",
                moveRange = 5,
                attackRange = 1,
                attackPower = 12,
                maxHp = 40,
                maxAp = 15,
                maxPosture = 80,
                skillIds = new[] { "heart_spike" },
                maxSkills = 4,
                frozenHeart = 0,
                tags = 0,
            },
            new SrpUnitTemplateData
            {
                id = "archer",
                displayName = "궁수",
                moveRange = 4,
                attackRange = 3,
                attackPower = 9,
                maxHp = 28,
                maxAp = 8,
                maxPosture = 60,
                skillIds = new[] { "fh_bless_ally" },
                maxSkills = 4,
                frozenHeart = 5,
                tags = 0,
            },
            new SrpUnitTemplateData
            {
                id = "boss_brute",
                displayName = "브루트 보스",
                moveRange = 3,
                attackRange = 1,
                attackPower = 18,
                maxHp = 80,
                maxAp = 25,
                maxPosture = 120,
                skillIds = new[] { "cleave" },
                maxSkills = 4,
                frozenHeart = 0,
                tags = (int)(SrpUnitTags.Boss | SrpUnitTags.Large),
            },
        };

        var placements = new[]
        {
            new SrpPlacementData { templateId = "knight", owner = 0, x = 1, y = 2, footprint = new SrpOffset[0] },
            new SrpPlacementData { templateId = "archer", owner = 0, x = 0, y = 5, footprint = new SrpOffset[0] },
            new SrpPlacementData { templateId = "knight", owner = 1, x = 8, y = 3, footprint = new SrpOffset[0] },
            new SrpPlacementData { templateId = "archer", owner = 1, x = 9, y = 6, footprint = new SrpOffset[0] },
            new SrpPlacementData
            {
                templateId = "boss_brute",
                owner = 1,
                x = 6,
                y = 4,
                footprint = new[]
                {
                    new SrpOffset { dx = 0, dy = 0 },
                    new SrpOffset { dx = 1, dy = 0 },
                },
            },
        };

        return new SrpMapFileV1
        {
            version = 1,
            name = "sample_skirmish",
            width = w,
            height = h,
            walkable = walk,
            playerOrder = new[] { 0, 1 },
            templates = templates,
            placements = placements,
        };
    }
}
