public static class SrpDefaultUnits
{
    public static SrpUnitTemplateData[] Create()
    {
        return new[]
        {
            new SrpUnitTemplateData
            {
                id = "vanguard",
                displayName = "탱커",
                moveRange = 5,
                attackRange = 1,
                attackPower = 10,
                maxHp = 36,
                maxPg = 24,
                maxActionPoints = 2,
                maxReactionPoints = 1,
                speed = 8,
                weaponClass = SrpWeaponClass.Melee,
                stance = SrpStance.Defensive,
                facing = SrpFacing.South,
                skillIds = new[] { "heart_spike" },
                maxSkills = 4,
                tags = 0,
            },
            new SrpUnitTemplateData
            {
                id = "rifleman",
                displayName = "사격수",
                moveRange = 4,
                attackRange = 4,
                attackPower = 8,
                maxHp = 28,
                maxPg = 16,
                maxActionPoints = 2,
                maxReactionPoints = 1,
                speed = 11,
                weaponClass = SrpWeaponClass.Firearm,
                stance = SrpStance.Aggressive,
                facing = SrpFacing.South,
                skillIds = new[] { "fh_bless_ally" },
                maxSkills = 4,
                tags = 0,
            },
            new SrpUnitTemplateData
            {
                id = "breaker",
                displayName = "근접 투사",
                moveRange = 4,
                attackRange = 1,
                attackPower = 11,
                maxHp = 32,
                maxPg = 20,
                maxActionPoints = 2,
                maxReactionPoints = 1,
                speed = 10,
                weaponClass = SrpWeaponClass.Melee,
                stance = SrpStance.Aggressive,
                facing = SrpFacing.South,
                skillIds = new[] { "cleave" },
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
                facing = SrpFacing.South,
                skillIds = new[] { "fh_bless_ally" },
                maxSkills = 4,
                tags = 0,
            },
        };
    }
}
