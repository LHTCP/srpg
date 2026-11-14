public static class SrpDefaultUnits
{
    /// <summary>
    /// 내장 기본 유닛 데이터의 버전. 기본 유닛을 추가하거나 값을 바꾸면 1 올린다.
    /// 로컬 units.json이 더 낮은 버전으로 저장돼 있으면 내장 id의 유닛을 최신 값으로 갱신한다(SrpDataIO).
    /// </summary>
    public const int Version = 1;

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
                skillIds = new[] { "tank_line_anchor", "cleave", "rally_line" },
                maxSkills = 4,
                tags = (int)SrpUnitTags.Tank,
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
                skillIds = new[] { "rifle_exposed_punisher", "kill_order", "heal_light", "suppressing_fire" },
                maxSkills = 4,
                tags = 0,
            },
            new SrpUnitTemplateData
            {
                id = "breaker",
                displayName = "주인공",
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
                facing = SrpFacing.South,
                skillIds = new[] { "mage_field_theory", "tactical_mark", "balance_hex", "arcane_screen" },
                maxSkills = 4,
                tags = 0,
            },
            // 6차 추가. GDD §10의 근접 투사 역할: PG 붕괴와 처단.
            new SrpUnitTemplateData
            {
                id = "duelist",
                displayName = "투사",
                moveRange = 5,
                attackRange = 1,
                attackPower = 10,
                maxHp = 28,
                maxPg = 18,
                maxActionPoints = 2,
                maxReactionPoints = 1,
                speed = 12,
                weaponClass = SrpWeaponClass.Melee,
                stance = SrpStance.Aggressive,
                facing = SrpFacing.South,
                skillIds = new[] { "duelist_second_wind", "rending_strike" },
                maxSkills = 4,
                tags = 0,
            },
        };
    }
}
