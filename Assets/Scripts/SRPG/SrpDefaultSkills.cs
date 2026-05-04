public static class SrpDefaultSkills
{
    public static SrpSkillData[] Create()
    {
        return new[]
        {
            new SrpSkillData
            {
                id = "heart_spike",
                displayName = "심장 관통",
                description = "공격 적중 시 자신의 빙결된 심장(FH) +5",
                skillType = SrpSkillType.Passive,
                trigger = SrpSkillTrigger.OnAttackHit,
                targetType = SrpTargetType.None,
                range = 0,
                areaSize = 0,
                endsActivation = false,
                cooldown = 0,
                effects = new[]
                {
                    new SrpSkillEffect
                    {
                        type = SrpEffectType.FrozenHeart,
                        stat = "self",
                        value = 5,
                        duration = 0,
                    },
                },
            },
            new SrpSkillData
            {
                id = "fh_bless_ally",
                displayName = "빙결 축복",
                description = "턴 시작 시 자신의 FH +2",
                skillType = SrpSkillType.Passive,
                trigger = SrpSkillTrigger.OnTurnStart,
                targetType = SrpTargetType.None,
                range = 0,
                areaSize = 0,
                endsActivation = false,
                cooldown = 0,
                effects = new[]
                {
                    new SrpSkillEffect
                    {
                        type = SrpEffectType.FrozenHeart,
                        stat = "self",
                        value = 2,
                        duration = 0,
                    },
                },
            },
            new SrpSkillData
            {
                id = "cleave",
                displayName = "강타",
                description = "사거리 1 내 적 하나에게 27 고정 피해. 사용 후 활성화 종료.",
                skillType = SrpSkillType.Active,
                trigger = SrpSkillTrigger.OnActivate,
                targetType = SrpTargetType.SingleEnemy,
                range = 1,
                areaSize = 0,
                endsActivation = true,
                cooldown = 2,
                overclockFrozenHeartCost = 5,
                overclockCooldownReduction = 1,
                overclockPowerBonus = 8,
                isParryable = true,
                requiresParryTelegraph = true,
                effects = new[]
                {
                    new SrpSkillEffect
                    {
                        type = SrpEffectType.Damage,
                        stat = "",
                        value = 27,
                        duration = 0,
                    },
                },
            },
            new SrpSkillData
            {
                id = "heal_light",
                displayName = "치유의 빛",
                description = "사거리 2 내 아군의 HP를 15 회복. 공격은 별도로 가능.",
                skillType = SrpSkillType.Active,
                trigger = SrpSkillTrigger.OnActivate,
                targetType = SrpTargetType.SingleAlly,
                range = 2,
                areaSize = 0,
                endsActivation = false,
                cooldown = 1,
                maxCharges = 2,
                chargeRecoveryTurns = 2,
                overclockFrozenHeartCost = 5,
                overclockChargeRestore = 1,
                overclockPowerBonus = 5,
                effects = new[]
                {
                    new SrpSkillEffect
                    {
                        type = SrpEffectType.Heal,
                        stat = "hp",
                        value = 15,
                        duration = 0,
                    },
                },
            },
        };
    }
}
