using System.Collections.Generic;
using UnityEngine;

public static class SrpSkills
{
    public static bool TryApplyPassiveTurnStart(SrpUnitRuntime u, SrpBattleState state, System.Action<string> log)
    {
        bool any = false;
        foreach (var sr in u.skillRuntimes)
        {
            if (!state.SkillLookup.TryGetValue(sr.skillId, out var data)) continue;
            if (data.skillType != SrpSkillType.Passive) continue;
            if (data.trigger != SrpSkillTrigger.OnTurnStart) continue;
            if (sr.cooldownRemaining > 0) continue;

            ApplyEffects(data, null, u, u, state, log);
            sr.cooldownRemaining = data.cooldown;
            log?.Invoke($"스킬 발동: {u.displayName} → {data.displayName} | 턴 시작 효과");
            any = true;
        }
        return any;
    }

    public static void OnAttackResolved(SrpUnitRuntime attacker, SrpUnitRuntime defender,
        SrpCombatResolver.AttackOutcome outcome, SrpBattleState state, System.Action<string> log)
    {
        foreach (var sr in attacker.skillRuntimes)
        {
            if (!state.SkillLookup.TryGetValue(sr.skillId, out var data)) continue;
            if (data.skillType != SrpSkillType.Passive) continue;
            if (data.trigger != SrpSkillTrigger.OnAttackHit) continue;

            ApplyEffects(data, null, attacker, defender, state, log);
            log?.Invoke($"스킬 발동: {attacker.displayName} → {data.displayName} | 공격 적중 효과");
        }
    }

    public static void OnTakeDamage(SrpUnitRuntime defender, SrpBattleState state, System.Action<string> log)
    {
        foreach (var sr in defender.skillRuntimes)
        {
            if (!state.SkillLookup.TryGetValue(sr.skillId, out var data)) continue;
            if (data.skillType != SrpSkillType.Passive) continue;
            if (data.trigger != SrpSkillTrigger.OnTakeDamage) continue;

            ApplyEffects(data, null, defender, defender, state, log);
            log?.Invoke($"스킬 발동: {defender.displayName} → {data.displayName} | 피격 시 효과");
        }
    }

    public static bool CanUseActiveSkill(SrpSkillData data, SrpSkillRuntime runtime)
    {
        if (data == null || data.skillType != SrpSkillType.Active) return false;
        if (data.trigger != SrpSkillTrigger.OnActivate) return false;
        if (runtime == null || runtime.cooldownRemaining > 0) return false;
        EnsureRuntimeInitialized(data, runtime);
        if (UsesCharges(data) && runtime.chargesRemaining <= 0) return false;
        return true;
    }

    public static List<Vector2Int> GetSkillTargetTiles(SrpSkillData data, SrpUnitRuntime caster, SrpBattleState state)
    {
        var tiles = new List<Vector2Int>();
        if (data == null) return tiles;

        int range = data.range;
        for (int y = 0; y < state.Height; y++)
        for (int x = 0; x < state.Width; x++)
        {
            int dist = Mathf.Max(Mathf.Abs(x - caster.anchorX), Mathf.Abs(y - caster.anchorY));
            if (dist > range) continue;

            switch (data.targetType)
            {
                case SrpTargetType.Self:
                    if (x == caster.anchorX && y == caster.anchorY)
                        tiles.Add(new Vector2Int(x, y));
                    break;

                case SrpTargetType.SingleEnemy:
                    var occ = state.GetOccupant(x, y);
                    if (occ != null && !occ.eliminated && occ.owner != caster.owner)
                        tiles.Add(new Vector2Int(x, y));
                    break;

                case SrpTargetType.SingleAlly:
                    var ally = state.GetOccupant(x, y);
                    if (ally != null && !ally.eliminated && ally.owner == caster.owner)
                        tiles.Add(new Vector2Int(x, y));
                    break;

                case SrpTargetType.AreaEnemy:
                case SrpTargetType.AreaAlly:
                    tiles.Add(new Vector2Int(x, y));
                    break;
            }
        }
        return tiles;
    }

    public static void ResolveActiveSkill(SrpSkillData data, SrpSkillRuntime runtime,
        SrpUnitRuntime caster, int targetX, int targetY,
        SrpBattleState state, System.Action<string> log)
    {
        SrpUnitRuntime target = state.GetOccupant(targetX, targetY);
        if (target == null && data.targetType == SrpTargetType.Self)
            target = caster;

        EnsureRuntimeInitialized(data, runtime);
        ApplyEffects(data, runtime, caster, target, state, log);
        ConsumeOverclockPowerIfNeeded(data, runtime, log);
        SpendSkillResource(data, runtime);
        runtime.cooldownRemaining = data.cooldown;
        string targetName = target != null ? target.displayName : $"({targetX},{targetY})";
        log?.Invoke($"스킬 사용: {caster.displayName} → {data.displayName} → {targetName}");
    }

    public static void EnsureRuntimeInitialized(SrpSkillData data, SrpSkillRuntime runtime)
    {
        if (data == null || runtime == null || !UsesCharges(data) || runtime.chargesInitialized)
            return;

        runtime.chargesRemaining = Mathf.Clamp(runtime.chargesRemaining <= 0 ? data.maxCharges : runtime.chargesRemaining, 0, data.maxCharges);
        runtime.chargeRecoveryRemaining = 0;
        runtime.chargesInitialized = true;
    }

    public static bool UsesCharges(SrpSkillData data)
    {
        return data != null && data.maxCharges > 0;
    }

    public static void TickSkillResources(SrpSkillData data, SrpSkillRuntime runtime)
    {
        if (data == null || runtime == null)
            return;

        EnsureRuntimeInitialized(data, runtime);
        if (runtime.cooldownRemaining > 0)
            runtime.cooldownRemaining--;

        if (!UsesCharges(data) || runtime.chargesRemaining >= data.maxCharges)
            return;

        if (runtime.chargeRecoveryRemaining <= 0)
            runtime.chargeRecoveryRemaining = GetChargeRecoveryInterval(data);
        runtime.chargeRecoveryRemaining--;
        if (runtime.chargeRecoveryRemaining > 0)
            return;

        runtime.chargesRemaining = Mathf.Min(data.maxCharges, runtime.chargesRemaining + 1);
        runtime.chargeRecoveryRemaining = runtime.chargesRemaining < data.maxCharges
            ? GetChargeRecoveryInterval(data)
            : 0;
    }

    public static void TickSkillResourcesForUnit(SrpUnitRuntime unit, SrpBattleState state)
    {
        if (unit == null || state == null || unit.eliminated)
            return;

        foreach (var runtime in unit.skillRuntimes)
        {
            if (!state.SkillLookup.TryGetValue(runtime.skillId, out var data))
                continue;
            TickSkillResources(data, runtime);
        }
    }

    public static bool TryOverclockSkill(
        SrpUnitRuntime caster,
        SrpSkillData data,
        SrpSkillRuntime runtime,
        System.Action<string> log)
    {
        if (!CanOverclockSkill(caster, data, runtime))
            return false;

        caster.frozenHeart = Mathf.Max(0, caster.frozenHeart - data.overclockFrozenHeartCost);
        if (data.overclockCooldownReduction > 0 && runtime.cooldownRemaining > 0)
            runtime.cooldownRemaining = Mathf.Max(0, runtime.cooldownRemaining - data.overclockCooldownReduction);
        if (data.overclockChargeRestore > 0 && UsesCharges(data) && runtime.chargesRemaining < data.maxCharges)
        {
            runtime.chargesRemaining = Mathf.Min(data.maxCharges, runtime.chargesRemaining + data.overclockChargeRestore);
            runtime.chargeRecoveryRemaining = runtime.chargesRemaining < data.maxCharges ? runtime.chargeRecoveryRemaining : 0;
        }
        if (CanApplyOverclockPower(data, runtime))
            runtime.overclockedUsesRemaining = 1;

        string powerText = runtime.overclockedUsesRemaining > 0 && data.overclockPowerBonus > 0
            ? $" | 다음 사용 피해/회복 +{data.overclockPowerBonus}"
            : string.Empty;
        log?.Invoke($"오버클럭: {caster.displayName} → {data.displayName} | 안정도(FH)-{data.overclockFrozenHeartCost}{powerText}");
        return true;
    }

    public static bool CanOverclockSkill(SrpUnitRuntime caster, SrpSkillData data, SrpSkillRuntime runtime)
    {
        if (caster == null || data == null || runtime == null)
            return false;
        if (data.overclockFrozenHeartCost <= 0 || caster.frozenHeart < data.overclockFrozenHeartCost)
            return false;

        EnsureRuntimeInitialized(data, runtime);
        bool canReduceCooldown = data.overclockCooldownReduction > 0 && runtime.cooldownRemaining > 0;
        bool canRestoreCharge = data.overclockChargeRestore > 0
            && UsesCharges(data)
            && runtime.chargesRemaining < data.maxCharges;
        return canReduceCooldown || canRestoreCharge || CanApplyOverclockPower(data, runtime);
    }

    static bool CanApplyOverclockPower(SrpSkillData data, SrpSkillRuntime runtime)
    {
        return data != null
            && runtime != null
            && data.skillType == SrpSkillType.Active
            && data.trigger == SrpSkillTrigger.OnActivate
            && data.overclockPowerBonus > 0
            && runtime.overclockedUsesRemaining <= 0;
    }

    static void SpendSkillResource(SrpSkillData data, SrpSkillRuntime runtime)
    {
        if (!UsesCharges(data))
            return;

        runtime.chargesRemaining = Mathf.Max(0, runtime.chargesRemaining - 1);
        if (runtime.chargesRemaining < data.maxCharges && runtime.chargeRecoveryRemaining <= 0)
            runtime.chargeRecoveryRemaining = GetChargeRecoveryInterval(data);
    }

    static int GetChargeRecoveryInterval(SrpSkillData data)
    {
        return Mathf.Max(1, data != null ? data.chargeRecoveryTurns : 1);
    }

    static int ApplyOverclockPowerBonus(SrpSkillData data, SrpSkillRuntime runtime, int baseValue)
    {
        if (data == null || runtime == null || runtime.overclockedUsesRemaining <= 0 || data.overclockPowerBonus <= 0)
            return baseValue;
        return baseValue + data.overclockPowerBonus;
    }

    static void ConsumeOverclockPowerIfNeeded(SrpSkillData data, SrpSkillRuntime runtime, System.Action<string> log)
    {
        if (data == null || runtime == null || runtime.overclockedUsesRemaining <= 0 || data.overclockPowerBonus <= 0)
            return;
        runtime.overclockedUsesRemaining = Mathf.Max(0, runtime.overclockedUsesRemaining - 1);
        log?.Invoke($"오버클럭 강화 소모: {data.displayName} | 피해/회복 +{data.overclockPowerBonus}");
    }

    static void ApplyEffects(SrpSkillData data, SrpSkillRuntime runtime, SrpUnitRuntime caster, SrpUnitRuntime target,
        SrpBattleState state, System.Action<string> log)
    {
        if (data.effects == null) return;
        foreach (var eff in data.effects)
        {
            switch (eff.type)
            {
                case SrpEffectType.Damage:
                {
                    if (target == null) break;
                    int baseDmg = eff.value;
                    int dmg = ApplyOverclockPowerBonus(data, runtime, baseDmg);
                    if (dmg <= 0) break;
                    int pgDmg = Mathf.Max(1, dmg / 2);
                    int hpDmg = dmg;
                    var reaction = SrpCombatResolver.ApplySkillReaction(state, caster, target, data, ref hpDmg, ref pgDmg);
                    if (reaction.sustainedDefenseBufferApplied)
                        log?.Invoke($"수비 완충: {target.displayName} | HP-{reaction.reducedHpBySustainedDefense} PG-{reaction.reducedPgBySustainedDefense}");
                    if (reaction.tankMultiEngagementBufferApplied)
                        log?.Invoke($"탱커 대응: {target.displayName} | HP-{reaction.reducedHpByTank} PG-{reaction.reducedPgByTank}");
                    if (reaction.combatTagBonusApplied)
                        log?.Invoke($"전투 태그 소모: {target.displayName} | {SrpCombatTagUtility.BuildSummary(reaction.consumedCombatTags)} | HP+{reaction.bonusHpFromCombatTags} PG+{reaction.bonusPgFromCombatTags}");
                    if (reaction.reactionSpentRp)
                        LogSkillReaction(target, reaction, log);
                    SrpCombatResolver.ApplyResolvedDamage(target, ref reaction, hpDmg, pgDmg);
                    string overclockText = dmg != baseDmg ? $" | 오버클럭 +{data.overclockPowerBonus}" : string.Empty;
                    log?.Invoke($"스킬 피해: {target.displayName} | PG-{pgDmg} HP-{hpDmg} (기준 {baseDmg}{overclockText})");
                    if (reaction.becameGroggy)
                        log?.Invoke($"PG 붕괴: {target.displayName} | 처단 위험 상태");
                    if (reaction.defenderDied)
                    {
                        state.RemoveUnit(target);
                        log?.Invoke($"사망: {target.displayName}");
                    }
                    break;
                }
                case SrpEffectType.Heal:
                {
                    if (target == null) break;
                    int baseHeal = eff.value;
                    int heal = ApplyOverclockPowerBonus(data, runtime, baseHeal);
                    target.hp = Mathf.Min(target.hp + heal, target.maxHp);
                    string overclockText = heal != baseHeal ? $" | 오버클럭 +{data.overclockPowerBonus}" : string.Empty;
                    log?.Invoke($"회복: {target.displayName} | HP +{heal} (기준 {baseHeal}{overclockText})");
                    break;
                }
                case SrpEffectType.FrozenHeart:
                {
                    var affected = eff.stat == "self" ? caster : target;
                    if (affected == null) break;
                    affected.frozenHeart = Mathf.Max(0, affected.frozenHeart + eff.value);
                    log?.Invoke($"안정도 변화: {affected.displayName} | FH +{eff.value}");
                    break;
                }
                case SrpEffectType.BuffStat:
                case SrpEffectType.DebuffStat:
                {
                    if (target == null) break;
                    int delta = eff.type == SrpEffectType.BuffStat ? eff.value : -eff.value;
                    ApplyStatDelta(target, eff.stat, delta);
                    log?.Invoke($"스탯 변화: {target.displayName} | {eff.stat} {(delta >= 0 ? "+" : "")}{delta}");
                    break;
                }
                case SrpEffectType.ApplyCombatTag:
                {
                    var affected = eff.stat == "self" ? caster : target;
                    if (affected == null)
                        break;
                    if (!SrpCombatTagUtility.TryParse(eff.stat, out var tag) && !SrpCombatTagUtility.TryParse(eff.value.ToString(), out tag))
                        break;
                    affected.AddCombatTag(tag);
                    log?.Invoke($"전투 태그: {affected.displayName} | {SrpCombatTagUtility.GetDisplayName(tag)} 부여/갱신");
                    break;
                }
                case SrpEffectType.Cleave:
                {
                    if (target == null) break;
                    int dmg = caster.attackPower + eff.value;
                    target.hp -= dmg;
                    log?.Invoke($"강타 피해: {target.displayName} | HP-{dmg}");
                    if (target.hp <= 0)
                    {
                        state.RemoveUnit(target);
                        log?.Invoke($"사망: {target.displayName}");
                    }
                    break;
                }
            }
        }
    }

    static void LogSkillReaction(
        SrpUnitRuntime target,
        SrpCombatResolver.AttackOutcome reaction,
        System.Action<string> log)
    {
        if (target == null)
            return;

        switch (reaction.reactionKind)
        {
            case SrpReactionKind.Parry:
                string parryReward = reaction.parryAppliedBalanceBreak
                    ? $" | 반격 PG-{reaction.parryCounterDamageToPg}, 균형 붕괴"
                    : string.Empty;
                log?.Invoke($"방어 반응: {target.displayName} 패링 성공 | 피해 무효{parryReward}");
                break;
            case SrpReactionKind.Dodge:
                if (reaction.wasDodged)
                    log?.Invoke($"방어 반응: {target.displayName} 회피 성공 | 피해 무효");
                else if (reaction.dodgeFailed)
                    log?.Invoke($"방어 반응: {target.displayName} 회피 실패 | 기본 피해 적용");
                else
                    log?.Invoke($"방어 반응: {target.displayName} 회피 발동");
                break;
            case SrpReactionKind.Guard:
                log?.Invoke($"방어 반응: {target.displayName} 가드 발동 | 추가 감쇠 적용");
                break;
            default:
                log?.Invoke($"방어 반응: {target.displayName} {reaction.reactionKind} 발동");
                break;
        }
    }

    static void ApplyStatDelta(SrpUnitRuntime u, string stat, int delta)
    {
        switch (stat)
        {
            case "hp":          u.hp = Mathf.Clamp(u.hp + delta, 0, u.maxHp); break;
            case "pg":
            case "posture":     u.pg = Mathf.Clamp(u.pg + delta, 0, u.maxPg); break;
            case "ap":
            case "actionPoints": u.actionPoints = Mathf.Clamp(u.actionPoints + delta, 0, u.maxActionPoints); break;
            case "rp":
            case "reactionPoints": u.reactionPoints = Mathf.Clamp(u.reactionPoints + delta, 0, u.maxReactionPoints); break;
            case "attackPower": u.attackPower = Mathf.Max(0, u.attackPower + delta); break;
            case "moveRange":   u.moveRange = Mathf.Max(0, u.moveRange + delta); break;
            case "attackRange": u.attackRange = Mathf.Max(0, u.attackRange + delta); break;
        }
    }

    public static void TickCooldownsForPlayer(SrpBattleState state, int playerId)
    {
        foreach (var u in state.Units)
        {
            if (u.eliminated || u.owner != playerId) continue;
            TickSkillResourcesForUnit(u, state);
        }
    }
}
