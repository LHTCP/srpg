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
    public SrpSkillData[] skills = Array.Empty<SrpSkillData>();
}

[Serializable]
public class SrpUnitDatabase
{
    public SrpUnitTemplateData[] units = Array.Empty<SrpUnitTemplateData>();
}
