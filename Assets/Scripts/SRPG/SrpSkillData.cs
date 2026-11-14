using System;

public enum SrpSkillType { Active, Passive }

public enum SrpSkillTrigger
{
    OnActivate,
    OnTurnStart,
    OnAttackHit,
    OnTakeDamage,
}

public enum SrpTargetType
{
    None,
    Self,
    SingleEnemy,
    SingleAlly,
    AreaEnemy,
    AreaAlly,
}

public enum SrpEffectType
{
    Damage,
    Heal,
    BuffStat,
    DebuffStat,
    FrozenHeart,
    Cleave,
    ApplyCombatTag,
}

[Serializable]
public class SrpSkillEffect
{
    public SrpEffectType type;
    public string stat;
    public int value;
    public int duration;
}

[Serializable]
public class SrpSkillData
{
    public string id;
    public string displayName;
    public string description;
    public SrpSkillType skillType;
    public SrpSkillTrigger trigger;
    public SrpTargetType targetType;
    public int range;
    public int areaSize;
    public bool endsActivation;
    public int cooldown;
    public int maxCharges;
    public int chargeRecoveryTurns;
    public int overclockFrozenHeartCost;
    public int overclockCooldownReduction;
    public int overclockChargeRestore;
    public int overclockPowerBonus;
    public bool isParryable;
    public bool requiresParryTelegraph;
    /// <summary>대상 칸까지의 시야 규칙. 값이 없는 기존 데이터는 None(시야 불필요)으로 읽힌다.</summary>
    public SrpSightRule sightRule;
    public SrpSkillEffect[] effects = Array.Empty<SrpSkillEffect>();
}

[Serializable]
public class SrpSkillRuntime
{
    public string skillId;
    public int cooldownRemaining;
    public int chargesRemaining;
    public int chargeRecoveryRemaining;
    public int overclockedUsesRemaining;
    public bool chargesInitialized;

    public SrpSkillRuntime() { }
    public SrpSkillRuntime(string id) { skillId = id; }

    public SrpSkillRuntime Clone()
    {
        return new SrpSkillRuntime
        {
            skillId = skillId,
            cooldownRemaining = cooldownRemaining,
            chargesRemaining = chargesRemaining,
            chargeRecoveryRemaining = chargeRecoveryRemaining,
            overclockedUsesRemaining = overclockedUsesRemaining,
            chargesInitialized = chargesInitialized,
        };
    }
}

[Serializable]
public class SrpSkillDatabase
{
    /// <summary>저장 시점의 내장 기본 스킬 버전(SrpDefaultSkills.Version). 값이 없는 예전 파일은 0으로 읽힌다.</summary>
    public int defaultsVersion;
    public SrpSkillData[] skills = Array.Empty<SrpSkillData>();
}

[Serializable]
public class SrpUnitDatabase
{
    /// <summary>저장 시점의 내장 기본 유닛 버전(SrpDefaultUnits.Version). 값이 없는 예전 파일은 0으로 읽힌다.</summary>
    public int defaultsVersion;
    public SrpUnitTemplateData[] units = Array.Empty<SrpUnitTemplateData>();
}
