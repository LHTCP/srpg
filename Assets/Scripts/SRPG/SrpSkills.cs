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
        // 회피/패링으로 무효화된 공격은 "적중"이 아니므로 적중 패시브를 발동하지 않는다.
        if (outcome.wasDodged || outcome.wasParried)
            return;

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

    /// <summary>
    /// 이 스킬의 피해를 대상이 회피 반응으로 무효화할 수 있는지. 예상 피해 표시에 쓴다.
    /// 판정 조건은 실제 해결(SrpCombatResolver.ApplySkillReaction)과 같다.
    /// </summary>
    public static bool CanTargetDodgeSkillDamage(
        SrpSkillData data, SrpUnitRuntime caster, SrpUnitRuntime target, SrpBattleState state)
    {
        if (data == null || data.effects == null || caster == null || target == null)
            return false;

        bool hasDamage = false;
        foreach (var eff in data.effects)
        {
            if (eff == null)
                continue;
            // 레거시 Cleave는 공격력 + 값을 기준 피해로 삼아 Damage와 같은 경로를 탄다.
            if (eff.type == SrpEffectType.Damage && eff.value > 0)
                hasDamage = true;
            else if (eff.type == SrpEffectType.Cleave && caster.attackPower + eff.value > 0)
                hasDamage = true;
        }
        return hasDamage && SrpCombatResolver.CanDefenderDodge(state, caster, target);
    }

    /// <summary>스킬의 시야 규칙(sightRule)으로 시전자에서 대상 칸까지 시야가 통하는가.</summary>
    public static bool HasSightToTile(SrpSkillData data, SrpUnitRuntime caster, int x, int y, SrpBattleState state)
    {
        if (data == null || caster == null || state == null)
            return false;
        var occupant = state.GetOccupant(x, y);
        return SrpLineOfSight.HasSight(
            state, caster.anchorX, caster.anchorY, x, y, data.sightRule,
            caster.id, occupant != null ? occupant.id : -1);
    }

    /// <summary>시전자가 이 칸을 스킬 대상으로 지정할 수 있는가(사거리와 시야). 대상 유형별 점유 조건은 보지 않는다.</summary>
    public static bool IsTileInSkillReach(SrpSkillData data, SrpUnitRuntime caster, int x, int y, SrpBattleState state)
    {
        if (data == null || caster == null || state == null || !state.InBounds(x, y))
            return false;
        int dist = Mathf.Max(Mathf.Abs(x - caster.anchorX), Mathf.Abs(y - caster.anchorY));
        return dist <= data.range && HasSightToTile(data, caster, x, y, state);
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
            if (!HasSightToTile(data, caster, x, y, state)) continue;

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
        var targets = GetSkillAffectedUnits(data, caster, targetX, targetY, state);

        EnsureRuntimeInitialized(data, runtime);
        if (targets.Count == 0)
        {
            // 대상이 없어도 시전자에게 걸리는 효과(안정도 등)는 적용한다.
            ApplyEffects(data, runtime, caster, null, state, log);
        }
        else
        {
            // 시전자 자신에게 걸리는 효과는 대상 수와 무관하게 한 번만 적용한다.
            for (int i = 0; i < targets.Count; i++)
                ApplyEffects(data, runtime, caster, targets[i], state, log, applySelfEffects: i == 0);
        }
        ConsumeOverclockPowerIfNeeded(data, runtime, log);
        SpendSkillResource(data, runtime);
        runtime.cooldownRemaining = data.cooldown;
        string targetName;
        if (IsAreaSkill(data))
            targetName = $"({targetX},{targetY}) 범위 {targets.Count}명";
        else
            targetName = targets.Count > 0 ? targets[0].displayName : $"({targetX},{targetY})";
        log?.Invoke($"스킬 사용: {caster.displayName} → {data.displayName} → {targetName}");
    }

    public static bool IsAreaSkill(SrpSkillData data)
    {
        return data != null
            && (data.targetType == SrpTargetType.AreaEnemy || data.targetType == SrpTargetType.AreaAlly);
    }

    /// <summary>
    /// 스킬이 덮는 칸. 범위 스킬은 대상 칸을 중심으로 areaSize칸 반경의 정사각형(1이면 3x3)이고,
    /// 그 밖의 스킬은 대상 칸 하나다. 맵 밖의 칸은 뺀다.
    /// </summary>
    public static List<Vector2Int> GetSkillAreaTiles(SrpSkillData data, int centerX, int centerY, SrpBattleState state)
    {
        var tiles = new List<Vector2Int>();
        if (data == null || state == null)
            return tiles;

        int radius = IsAreaSkill(data) ? Mathf.Max(0, data.areaSize) : 0;
        for (int y = centerY - radius; y <= centerY + radius; y++)
        for (int x = centerX - radius; x <= centerX + radius; x++)
        {
            if (state.InBounds(x, y))
                tiles.Add(new Vector2Int(x, y));
        }
        return tiles;
    }

    /// <summary>
    /// 이 칸을 대상으로 스킬을 쓰면 효과를 받는 유닛. 범위 스킬은 지정한 진영만 받는다
    /// (AreaEnemy는 적, AreaAlly는 시전자를 포함한 아군). 가까운 순, 같으면 id 순으로 돌려준다.
    /// </summary>
    public static List<SrpUnitRuntime> GetSkillAffectedUnits(
        SrpSkillData data, SrpUnitRuntime caster, int targetX, int targetY, SrpBattleState state)
    {
        var units = new List<SrpUnitRuntime>();
        if (data == null || caster == null || state == null)
            return units;

        if (!IsAreaSkill(data))
        {
            var single = state.GetOccupant(targetX, targetY);
            if (single == null && data.targetType == SrpTargetType.Self)
                single = caster;
            if (single != null)
                units.Add(single);
            return units;
        }

        int radius = Mathf.Max(0, data.areaSize);
        bool wantsEnemy = data.targetType == SrpTargetType.AreaEnemy;
        foreach (var unit in state.Units)
        {
            if (unit == null || unit.eliminated)
                continue;
            if ((unit.owner != caster.owner) != wantsEnemy)
                continue;
            if (FootprintDistanceToTile(unit, targetX, targetY) > radius)
                continue;
            units.Add(unit);
        }
        units.Sort((a, b) =>
        {
            int da = FootprintDistanceToTile(a, targetX, targetY);
            int db = FootprintDistanceToTile(b, targetX, targetY);
            return da != db ? da.CompareTo(db) : a.id.CompareTo(b.id);
        });
        return units;
    }

    /// <summary>유닛이 차지한 칸 중 대상 칸에 가장 가까운 칸까지의 거리(대각선 포함). 여러 칸 유닛은 한 칸만 걸쳐도 범위에 든다.</summary>
    static int FootprintDistanceToTile(SrpUnitRuntime unit, int x, int y)
    {
        if (unit.footprintOffsets == null || unit.footprintOffsets.Count == 0)
            return Mathf.Max(Mathf.Abs(unit.anchorX - x), Mathf.Abs(unit.anchorY - y));

        int best = int.MaxValue;
        foreach (var off in unit.footprintOffsets)
        {
            int dist = Mathf.Max(Mathf.Abs(unit.anchorX + off.x - x), Mathf.Abs(unit.anchorY + off.y - y));
            if (dist < best)
                best = dist;
        }
        return best;
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
        SrpBattleState state, System.Action<string> log, bool applySelfEffects = true)
    {
        if (data.effects == null) return;
        foreach (var eff in data.effects)
        {
            if (eff == null) continue;
            // 범위 스킬의 두 번째 대상부터는 시전자 자신에게 걸리는 효과를 다시 적용하지 않는다.
            if (!applySelfEffects && eff.stat == "self"
                && (eff.type == SrpEffectType.FrozenHeart || eff.type == SrpEffectType.ApplyCombatTag))
                continue;

            switch (eff.type)
            {
                case SrpEffectType.Damage:
                case SrpEffectType.Cleave:
                {
                    if (target == null) break;
                    // Cleave는 레거시 효과다. 공격력 + 값을 기준 피해로 삼고 Damage와 같은 방어·반응·태그 규칙을 탄다.
                    int baseDmg = eff.type == SrpEffectType.Cleave ? caster.attackPower + eff.value : eff.value;
                    int dmg = ApplyOverclockPowerBonus(data, runtime, baseDmg);
                    if (dmg <= 0) break;
                    int pgDmg = Mathf.Max(1, dmg / 2);
                    int hpDmg = dmg;
                    var reaction = SrpCombatResolver.ApplySkillReaction(state, caster, target, data, ref hpDmg, ref pgDmg);
                    if (reaction.sustainedDefenseBufferApplied)
                        log?.Invoke($"수비 완충: {target.displayName} | PG-{reaction.reducedPgBySustainedDefense}");
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
                        state.RebuildEngagements();
                        log?.Invoke($"사망: {target.displayName}");
                    }
                    else if ((hpDmg > 0 || pgDmg > 0) && target != caster)
                    {
                        OnTakeDamage(target, state, log);
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
                    // duration N: 부여한 라운드를 빼고 N라운드가 끝날 때까지 유지한다. 0이면 소모 전까지 유지한다.
                    int expireRound = eff.duration > 0 ? state.RoundNumber + eff.duration : 0;
                    affected.AddCombatTag(tag, expireRound);
                    string durationText = expireRound > 0 ? $" | {expireRound}라운드 끝까지" : string.Empty;
                    log?.Invoke($"전투 태그: {affected.displayName} | {SrpCombatTagUtility.GetDisplayName(tag)} 부여/갱신{durationText}");
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
