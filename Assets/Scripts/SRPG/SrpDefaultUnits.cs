public static class SrpDefaultUnits
{
    public static SrpUnitTemplateData[] Create()
    {
        return new[]
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
    }
}
