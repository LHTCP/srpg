public static class SrpDefaultSkills
{
    /// <summary>
    /// 내장 기본 스킬 데이터의 버전. 기본 스킬을 추가하거나 값을 바꾸면 1 올린다.
    /// 로컬 skills.json이 더 낮은 버전으로 저장돼 있으면 내장 id의 스킬을 최신 값으로 갱신한다(SrpDataIO).
    /// </summary>
    public const int Version = 1;

    public static SrpSkillData[] Create()
    {
        return new[]
        {
            new SrpSkillData
            {
                id = "hero_adaptive_heart",
                displayName = "전장 적응",
                description = "주인공 고유 패시브. 공격 적중 시 안정도(FH) +3. 패링/오버클럭으로 전장 흐름을 다시 잡는다.",
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
                        value = 3,
                        duration = 0,
                    },
                },
            },
            new SrpSkillData
            {
                id = "tank_line_anchor",
                displayName = "전열 고정",
                description = "탱커 고유 패시브. 피격 후 PG +2를 회복해 전열과 완벽한 수비 조건을 유지한다.",
                skillType = SrpSkillType.Passive,
                trigger = SrpSkillTrigger.OnTakeDamage,
                targetType = SrpTargetType.None,
                range = 0,
                areaSize = 0,
                endsActivation = false,
                cooldown = 0,
                effects = new[]
                {
                    new SrpSkillEffect
                    {
                        type = SrpEffectType.BuffStat,
                        stat = "pg",
                        value = 2,
                        duration = 0,
                    },
                },
            },
            new SrpSkillData
            {
                id = "rifle_exposed_punisher",
                displayName = "노출 처벌",
                description = "사격수 고유 패시브. 공격 적중 시 안정도(FH) +2. 엄폐 밖의 적을 압박하고 후속 사격을 준비한다.",
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
                        value = 2,
                        duration = 0,
                    },
                },
            },
            new SrpSkillData
            {
                id = "mage_field_theory",
                displayName = "전장 해석",
                description = "마도사 고유 패시브. 턴 시작 시 안정도(FH) +2. 표식과 제어 스킬의 사용 리듬을 앞당긴다.",
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
                id = "heart_spike",
                displayName = "심장 관통",
                description = "공격 적중 시 자신의 빙결된 심장(FH) +5.",
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
                description = "턴 시작 시 자신의 빙결된 심장(FH) +2.",
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
                description = "사거리 1 내 적 하나에게 HP 18 / PG 9 기준 피해(중대 HP 피해). 사용 후 활성화 종료.",
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
                        value = 18,
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
            new SrpSkillData
            {
                id = "tactical_mark",
                displayName = "전술 표식",
                description = "사거리 3 내 시야가 닿는 적 하나에게 표식을 부여한다. 다음 아군 공격의 PG 압박을 높인다.",
                skillType = SrpSkillType.Active,
                trigger = SrpSkillTrigger.OnActivate,
                targetType = SrpTargetType.SingleEnemy,
                range = 3,
                areaSize = 0,
                endsActivation = false,
                cooldown = 1,
                sightRule = SrpSightRule.Terrain,
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
            new SrpSkillData
            {
                id = "balance_hex",
                displayName = "균형 교란",
                description = "사거리 3 내 시야가 닿는 적 하나에게 균형 붕괴를 부여한다. 다음 아군 공격의 PG 피해가 크게 오른다.",
                skillType = SrpSkillType.Active,
                trigger = SrpSkillTrigger.OnActivate,
                targetType = SrpTargetType.SingleEnemy,
                range = 3,
                areaSize = 0,
                endsActivation = false,
                cooldown = 2,
                maxCharges = 1,
                chargeRecoveryTurns = 2,
                sightRule = SrpSightRule.Terrain,
                effects = new[]
                {
                    new SrpSkillEffect
                    {
                        type = SrpEffectType.ApplyCombatTag,
                        stat = "balanceBroken",
                        value = 0,
                        duration = 1,
                    },
                },
            },
            new SrpSkillData
            {
                id = "kill_order",
                displayName = "사살 지시",
                description = "사거리 4 내 시야가 닿는 적 하나에게 사살 지시를 부여한다. 다음 아군 공격의 HP/PG 압박이 오른다.",
                skillType = SrpSkillType.Active,
                trigger = SrpSkillTrigger.OnActivate,
                targetType = SrpTargetType.SingleEnemy,
                range = 4,
                areaSize = 0,
                endsActivation = false,
                cooldown = 2,
                sightRule = SrpSightRule.Terrain,
                effects = new[]
                {
                    new SrpSkillEffect
                    {
                        type = SrpEffectType.ApplyCombatTag,
                        stat = "killOrder",
                        value = 0,
                        duration = 1,
                    },
                },
            },
            new SrpSkillData
            {
                id = "arcane_screen",
                displayName = "전장 장막",
                description = "사거리 3 내 아군의 PG를 4 회복한다. 전열을 다시 세우는 마법 장막.",
                skillType = SrpSkillType.Active,
                trigger = SrpSkillTrigger.OnActivate,
                targetType = SrpTargetType.SingleAlly,
                range = 3,
                areaSize = 0,
                endsActivation = false,
                cooldown = 2,
                maxCharges = 1,
                chargeRecoveryTurns = 2,
                overclockFrozenHeartCost = 5,
                overclockPowerBonus = 2,
                effects = new[]
                {
                    new SrpSkillEffect
                    {
                        type = SrpEffectType.BuffStat,
                        stat = "pg",
                        value = 4,
                        duration = 0,
                    },
                },
            },

            // ── 6차 추가(2026-11-15). 역할군(GDD §10)에 맞춘 프로토타입 브릿지 수치다. ──
            new SrpSkillData
            {
                id = "duelist_second_wind",
                displayName = "투지",
                description = "투사 고유 패시브. 턴 시작 시 PG +2를 회복해 근접전에서 버틴다.",
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
                        type = SrpEffectType.BuffStat,
                        stat = "pg",
                        value = 2,
                        duration = 0,
                    },
                },
            },
            new SrpSkillData
            {
                id = "rending_strike",
                displayName = "균열 베기",
                description = "사거리 1 내 적 하나에게 HP 8 / PG 4 기준 피해를 주고 균형 붕괴를 부여한다. 이어지는 공격의 PG 피해가 크게 오른다.",
                skillType = SrpSkillType.Active,
                trigger = SrpSkillTrigger.OnActivate,
                targetType = SrpTargetType.SingleEnemy,
                range = 1,
                areaSize = 0,
                endsActivation = false,
                cooldown = 2,
                overclockFrozenHeartCost = 5,
                overclockCooldownReduction = 1,
                isParryable = true,
                requiresParryTelegraph = true,
                effects = new[]
                {
                    // 피해를 먼저 준 뒤 태그를 붙인다. 순서를 바꾸면 이 스킬이 자기 태그를 바로 소모한다.
                    new SrpSkillEffect
                    {
                        type = SrpEffectType.Damage,
                        stat = "",
                        value = 8,
                        duration = 0,
                    },
                    new SrpSkillEffect
                    {
                        type = SrpEffectType.ApplyCombatTag,
                        stat = "balanceBroken",
                        value = 0,
                        duration = 1,
                    },
                },
            },
            new SrpSkillData
            {
                id = "suppressing_fire",
                displayName = "제압 사격",
                description = "사거리 4 내 사선이 통하는 칸을 중심으로 3x3 범위의 적에게 HP 6 / PG 3 기준 피해를 주고 RP를 1 깎는다. 사용 후 활성화 종료.",
                skillType = SrpSkillType.Active,
                trigger = SrpSkillTrigger.OnActivate,
                targetType = SrpTargetType.AreaEnemy,
                range = 4,
                areaSize = 1,
                endsActivation = true,
                cooldown = 3,
                overclockFrozenHeartCost = 5,
                overclockCooldownReduction = 1,
                sightRule = SrpSightRule.TerrainAndUnits,
                effects = new[]
                {
                    new SrpSkillEffect
                    {
                        type = SrpEffectType.Damage,
                        stat = "",
                        value = 6,
                        duration = 0,
                    },
                    new SrpSkillEffect
                    {
                        type = SrpEffectType.DebuffStat,
                        stat = "rp",
                        value = 1,
                        duration = 0,
                    },
                },
            },
            new SrpSkillData
            {
                id = "rally_line",
                displayName = "전열 재정비",
                description = "자신과 주변 1칸 안의 아군 PG를 4 회복한다. 무너지기 직전의 전열을 다시 세운다.",
                skillType = SrpSkillType.Active,
                trigger = SrpSkillTrigger.OnActivate,
                targetType = SrpTargetType.AreaAlly,
                range = 0,
                areaSize = 1,
                endsActivation = false,
                cooldown = 3,
                overclockFrozenHeartCost = 5,
                overclockCooldownReduction = 1,
                effects = new[]
                {
                    new SrpSkillEffect
                    {
                        type = SrpEffectType.BuffStat,
                        stat = "pg",
                        value = 4,
                        duration = 0,
                    },
                },
            },
            new SrpSkillData
            {
                id = "shock_field",
                displayName = "충격장",
                description = "사거리 3 내 시야가 닿는 칸을 중심으로 3x3 범위의 적에게 HP 5 / PG 2 기준 피해를 주고 표식을 부여한다.",
                skillType = SrpSkillType.Active,
                trigger = SrpSkillTrigger.OnActivate,
                targetType = SrpTargetType.AreaEnemy,
                range = 3,
                areaSize = 1,
                endsActivation = false,
                cooldown = 3,
                overclockFrozenHeartCost = 5,
                overclockCooldownReduction = 1,
                sightRule = SrpSightRule.Terrain,
                effects = new[]
                {
                    new SrpSkillEffect
                    {
                        type = SrpEffectType.Damage,
                        stat = "",
                        value = 5,
                        duration = 0,
                    },
                    new SrpSkillEffect
                    {
                        type = SrpEffectType.ApplyCombatTag,
                        stat = "marked",
                        value = 0,
                        duration = 1,
                    },
                },
            },
        };
    }
}
