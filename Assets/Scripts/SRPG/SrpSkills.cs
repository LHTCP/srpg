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
            if (sr.cooldownRemaining > 0) { sr.cooldownRemaining--; continue; }

            ApplyEffects(data, u, u, state, log);
            sr.cooldownRemaining = data.cooldown;
            log?.Invoke($"[스킬] {data.displayName}: {u.displayName} 턴 시작 효과 발동");
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

            ApplyEffects(data, attacker, defender, state, log);
            log?.Invoke($"[스킬] {data.displayName}: 공격 적중 효과 발동");
        }
    }

    public static void OnTakeDamage(SrpUnitRuntime defender, SrpBattleState state, System.Action<string> log)
    {
        foreach (var sr in defender.skillRuntimes)
        {
            if (!state.SkillLookup.TryGetValue(sr.skillId, out var data)) continue;
            if (data.skillType != SrpSkillType.Passive) continue;
            if (data.trigger != SrpSkillTrigger.OnTakeDamage) continue;

            ApplyEffects(data, defender, defender, state, log);
            log?.Invoke($"[스킬] {data.displayName}: 피격 시 효과 발동");
        }
    }

    public static bool CanUseActiveSkill(SrpSkillData data, SrpSkillRuntime runtime)
    {
        if (data == null || data.skillType != SrpSkillType.Active) return false;
        if (data.trigger != SrpSkillTrigger.OnActivate) return false;
        return runtime.cooldownRemaining <= 0;
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

        ApplyEffects(data, caster, target, state, log);
        runtime.cooldownRemaining = data.cooldown;
        string targetName = target != null ? target.displayName : $"({targetX},{targetY})";
        log?.Invoke($"[스킬] {caster.displayName} → {data.displayName} → {targetName}");
    }

    static void ApplyEffects(SrpSkillData data, SrpUnitRuntime caster, SrpUnitRuntime target,
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
                    int dmg = eff.value;
                    if (dmg <= 0) break;
                    int pgDmg = Mathf.Max(1, dmg / 2);
                    target.pg = Mathf.Max(0, target.pg - pgDmg);
                    int hpDmg = dmg;
                    target.hp -= hpDmg;
                    log?.Invoke($"  피해: PG-{pgDmg} HP-{hpDmg} (총 {dmg})");
                    if (target.hp <= 0)
                    {
                        state.RemoveUnit(target);
                        log?.Invoke($"  사망: {target.displayName}");
                    }
                    break;
                }
                case SrpEffectType.Heal:
                {
                    if (target == null) break;
                    int heal = eff.value;
                    target.hp = Mathf.Min(target.hp + heal, target.maxHp);
                    log?.Invoke($"  회복: HP +{heal}");
                    break;
                }
                case SrpEffectType.FrozenHeart:
                {
                    var affected = eff.stat == "self" ? caster : target;
                    if (affected == null) break;
                    affected.frozenHeart = Mathf.Max(0, affected.frozenHeart + eff.value);
                    log?.Invoke($"  FH 변화: {affected.displayName} FH +{eff.value}");
                    break;
                }
                case SrpEffectType.BuffStat:
                case SrpEffectType.DebuffStat:
                {
                    if (target == null) break;
                    int delta = eff.type == SrpEffectType.BuffStat ? eff.value : -eff.value;
                    ApplyStatDelta(target, eff.stat, delta);
                    log?.Invoke($"  스탯 변화: {target.displayName} {eff.stat} {(delta >= 0 ? "+" : "")}{delta}");
                    break;
                }
                case SrpEffectType.Cleave:
                {
                    if (target == null) break;
                    int dmg = caster.attackPower + eff.value;
                    target.hp -= dmg;
                    log?.Invoke($"  강타 피해: HP-{dmg}");
                    if (target.hp <= 0)
                    {
                        state.RemoveUnit(target);
                        log?.Invoke($"  사망: {target.displayName}");
                    }
                    break;
                }
            }
        }
    }

    static void ApplyStatDelta(SrpUnitRuntime u, string stat, int delta)
    {
        switch (stat)
        {
            case "hp":          u.hp = Mathf.Clamp(u.hp + delta, 0, u.maxHp); break;
            case "pg":          u.pg = Mathf.Clamp(u.pg + delta, 0, u.maxPg); break;
            case "actionPoints": u.actionPoints = Mathf.Clamp(u.actionPoints + delta, 0, u.maxActionPoints); break;
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
            foreach (var sr in u.skillRuntimes)
                if (sr.cooldownRemaining > 0)
                    sr.cooldownRemaining--;
        }
    }
}
