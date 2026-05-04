using UnityEngine;

/// <summary>
/// AP → HP, 자세, 그로기, 처단.
/// </summary>
public static class SrpCombatResolver
{
    const int ExecutionBonusDamage = 6;
    const int DefensiveStanceDef = 1;
    const int DefensiveStanceGrd = 1;
    const int GuardReactionDef = 2;
    const int GuardReactionGrd = 2;
    const int SustainedDefenseDef = 1;
    const int SustainedDefenseGrd = 1;
    const int TankMultiEngagementDef = 1;
    const int TankMultiEngagementGrd = 1;
    const int CoverDef = 2;
    const int CoverGrd = 1;
    const int DodgeSuccessChancePercent = 50;
    const int SideAttackHpBonus = 1;
    const int SideAttackPgBonus = 1;
    const int BackAttackHpBonus = 2;
    const int BackAttackPgBonus = 2;
    const int FirearmHpToPgSpilloverPercent = 50;

    public struct AttackOutcome
    {
        public int rawDamageToPg;
        public int rawDamageToHp;
        public int damageToPg;
        public int damageToHp;
        public int reducedHpByDef;
        public int reducedPgByGrd;
        public int reducedHpBySustainedDefense;
        public int reducedPgBySustainedDefense;
        public int reducedHpByTank;
        public int reducedPgByTank;
        public int reducedHpByCover;
        public int reducedPgByCover;
        public SrpReactionKind reactionKind;
        public bool reactionSpentRp;
        public bool sustainedDefenseBufferApplied;
        public bool tankMultiEngagementBufferApplied;
        public bool coverBufferApplied;
        public bool wasDodged;
        public bool dodgeFailed;
        public bool wasParried;
        public bool wasExecution;
        public bool defenderDied;
        public bool becameGroggy;
    }

    public static bool CanAttack(SrpBattleState state, SrpUnitRuntime attacker, SrpUnitRuntime defender)
    {
        if (attacker.eliminated || defender.eliminated)
            return false;
        if (attacker.owner == defender.owner)
            return false;
        int dist = state.ChebyshevAnchor(attacker, defender);
        return dist <= attacker.attackRange;
    }

    public static bool CanDefenderParry(
        SrpBattleState state,
        SrpUnitRuntime attacker,
        SrpUnitRuntime defender,
        SrpSkillData attackSkill = null)
    {
        if (state == null || attacker == null || defender == null)
            return false;
        if (attacker.eliminated || defender.eliminated || attacker.owner == defender.owner)
            return false;
        if (!defender.HasTag(SrpUnitTags.ParryUser) || defender.reactionPoints <= 0)
            return false;
        if (!IsParryableThreat(attacker, attackSkill))
            return false;
        if (state.ChebyshevAnchor(attacker, defender) > 1)
            return false;
        return IsAttackerInDefenderFrontArc(attacker, defender);
    }

    public static bool IsParryableThreat(SrpUnitRuntime attacker, SrpSkillData attackSkill = null)
    {
        return attacker != null && attacker.weaponClass == SrpWeaponClass.Melee
            && attackSkill != null && attackSkill.isParryable;
    }

    public static bool IsAttackerInDefenderFrontArc(SrpUnitRuntime attacker, SrpUnitRuntime defender)
    {
        if (attacker == null || defender == null)
            return false;

        int dx = attacker.anchorX - defender.anchorX;
        int dy = attacker.anchorY - defender.anchorY;
        if (dx == 0 && dy == 0)
            return false;

        switch (defender.facing)
        {
            case SrpFacing.North:
                return dy > 0 && Mathf.Abs(dy) >= Mathf.Abs(dx);
            case SrpFacing.East:
                return dx > 0 && Mathf.Abs(dx) >= Mathf.Abs(dy);
            case SrpFacing.South:
                return dy < 0 && Mathf.Abs(dy) >= Mathf.Abs(dx);
            case SrpFacing.West:
                return dx < 0 && Mathf.Abs(dx) >= Mathf.Abs(dy);
            default:
                return false;
        }
    }

    public static AttackOutcome ApplyAttack(SrpUnitRuntime attacker, SrpUnitRuntime defender)
    {
        return ApplyAttack(null, attacker, defender, null);
    }

    public static bool TryApplyOpportunityAttack(
        SrpBattleState state,
        SrpUnitRuntime attacker,
        SrpUnitRuntime defender,
        out AttackOutcome outcome)
    {
        outcome = new AttackOutcome();
        if (state == null || attacker == null || defender == null)
            return false;
        if (attacker.eliminated || defender.eliminated || attacker.owner == defender.owner)
            return false;
        if (attacker.reactionPoints <= 0)
            return false;

        attacker.reactionPoints = Mathf.Max(0, attacker.reactionPoints - 1);
        attacker.lastReactionKind = SrpReactionKind.ReactionShot;
        attacker.lastReactionRound = state.RoundNumber;
        attacker.lastReactionSourceId = defender.id;
        outcome = ApplyAttack(state, attacker, defender, null);
        return true;
    }

    public static AttackOutcome ApplyAttack(SrpBattleState state, SrpUnitRuntime attacker, SrpUnitRuntime defender)
    {
        return ApplyAttack(state, attacker, defender, null);
    }

    public static AttackOutcome ApplySkillReaction(
        SrpBattleState state,
        SrpUnitRuntime attacker,
        SrpUnitRuntime defender,
        SrpSkillData attackSkill,
        ref int hpDamage,
        ref int pgDamage)
    {
        var outcome = new AttackOutcome
        {
            rawDamageToHp = hpDamage,
            rawDamageToPg = pgDamage,
        };
        ApplyDirectionalVulnerability(attacker, defender, ref hpDamage, ref pgDamage);
        ApplyConstantMitigation(defender, ref hpDamage, ref pgDamage, ref outcome);
        ApplySustainedDefenseBuffers(state, defender, ref hpDamage, ref pgDamage, ref outcome);
        ApplyCoverBuffer(state, attacker, defender, ref hpDamage, ref pgDamage, ref outcome);
        ApplyReactionIfAvailable(state, attacker, defender, attackSkill, ref hpDamage, ref pgDamage, ref outcome);
        RecordDefensivePressure(state, defender);
        return outcome;
    }

    public static AttackOutcome ApplyAttack(SrpBattleState state, SrpUnitRuntime attacker, SrpUnitRuntime defender, SrpSkillData attackSkill)
    {
        var o = new AttackOutcome();
        int raw = Mathf.Max(1, attacker.attackPower);
        int hpDamage;
        int pgDamage;

        if (defender.pg <= 0 || defender.groggy)
        {
            o.wasExecution = true;
            hpDamage = raw + ExecutionBonusDamage;
            pgDamage = 0;
        }
        else
        {
            switch (attacker.weaponClass)
            {
                case SrpWeaponClass.Firearm:
                    hpDamage = raw * 2 + 6;
                    pgDamage = 0;
                    break;
                case SrpWeaponClass.Magic:
                    hpDamage = Mathf.Max(1, raw / 2);
                    pgDamage = Mathf.Max(1, raw / 2);
                    break;
                case SrpWeaponClass.Melee:
                default:
                    hpDamage = Mathf.Max(1, raw / 4);
                    pgDamage = raw + 4;
                    break;
            }

            if (attacker.stance == SrpStance.Aggressive)
                pgDamage += 1;
            o.rawDamageToHp = hpDamage;
            o.rawDamageToPg = pgDamage;
            ApplyDirectionalVulnerability(attacker, defender, ref hpDamage, ref pgDamage);
            ApplyConstantMitigation(defender, ref hpDamage, ref pgDamage, ref o);
            ApplySustainedDefenseBuffers(state, defender, ref hpDamage, ref pgDamage, ref o);
            ApplyFirearmHpSpillover(attacker, hpDamage, ref pgDamage);
            ApplyCoverBuffer(state, attacker, defender, ref hpDamage, ref pgDamage, ref o);
            ApplyReactionIfAvailable(state, attacker, defender, attackSkill, ref hpDamage, ref pgDamage, ref o);
        }

        ApplyResolvedDamage(defender, ref o, hpDamage, pgDamage);
        RecordDefensivePressure(state, defender);
        return o;
    }

    public static void ApplyResolvedDamage(
        SrpUnitRuntime defender,
        ref AttackOutcome outcome,
        int hpDamage,
        int pgDamage)
    {
        if (defender == null)
            return;

        defender.hp -= hpDamage;
        outcome.damageToHp = hpDamage;
        outcome.damageToPg = pgDamage;
        if (!outcome.wasExecution)
        {
            int prevPg = defender.pg;
            defender.pg = Mathf.Max(0, defender.pg - pgDamage);
            if (prevPg > 0 && defender.pg <= 0)
            {
                defender.groggy = true;
                outcome.becameGroggy = true;
            }
        }
        else
        {
            defender.pg = 0;
            defender.groggy = false;
        }

        if (defender.hp <= 0)
            outcome.defenderDied = true;
    }

    static void ApplyConstantMitigation(SrpUnitRuntime defender, ref int hpDamage, ref int pgDamage, ref AttackOutcome outcome)
    {
        int def = GetDef(defender);
        int grd = GetGrd(defender);
        int beforeHp = hpDamage;
        int beforePg = pgDamage;

        hpDamage = Mathf.Max(0, hpDamage - def);
        pgDamage = Mathf.Max(0, pgDamage - grd);
        outcome.reducedHpByDef += beforeHp - hpDamage;
        outcome.reducedPgByGrd += beforePg - pgDamage;
    }

    static void ApplySustainedDefenseBuffers(
        SrpBattleState state,
        SrpUnitRuntime defender,
        ref int hpDamage,
        ref int pgDamage,
        ref AttackOutcome outcome)
    {
        if (state == null || defender == null || defender.stance != SrpStance.Defensive)
            return;

        bool isEngaged = state.CountEngagingEnemies(defender) > 0;
        bool hasPriorDefensiveHit = defender.defensiveHitsRound == state.RoundNumber
            && defender.defensiveHitsTakenThisRound > 0;
        if (isEngaged && hasPriorDefensiveHit)
            ApplySustainedDefenseBuffer(ref hpDamage, ref pgDamage, ref outcome);

        if (defender.HasTag(SrpUnitTags.Tank) && state.CountEngagingEnemies(defender) >= 2)
            ApplyTankMultiEngagementBuffer(ref hpDamage, ref pgDamage, ref outcome);
    }

    static void ApplySustainedDefenseBuffer(ref int hpDamage, ref int pgDamage, ref AttackOutcome outcome)
    {
        int beforeHp = hpDamage;
        int beforePg = pgDamage;
        hpDamage = Mathf.Max(0, hpDamage - SustainedDefenseDef);
        pgDamage = Mathf.Max(0, pgDamage - SustainedDefenseGrd);
        outcome.reducedHpBySustainedDefense += beforeHp - hpDamage;
        outcome.reducedPgBySustainedDefense += beforePg - pgDamage;
        outcome.sustainedDefenseBufferApplied = outcome.reducedHpBySustainedDefense > 0
            || outcome.reducedPgBySustainedDefense > 0;
    }

    static void ApplyTankMultiEngagementBuffer(ref int hpDamage, ref int pgDamage, ref AttackOutcome outcome)
    {
        int beforeHp = hpDamage;
        int beforePg = pgDamage;
        hpDamage = Mathf.Max(0, hpDamage - TankMultiEngagementDef);
        pgDamage = Mathf.Max(0, pgDamage - TankMultiEngagementGrd);
        outcome.reducedHpByTank += beforeHp - hpDamage;
        outcome.reducedPgByTank += beforePg - pgDamage;
        outcome.tankMultiEngagementBufferApplied = outcome.reducedHpByTank > 0 || outcome.reducedPgByTank > 0;
    }

    static void ApplyCoverBuffer(
        SrpBattleState state,
        SrpUnitRuntime attacker,
        SrpUnitRuntime defender,
        ref int hpDamage,
        ref int pgDamage,
        ref AttackOutcome outcome)
    {
        if (!TryGetCoverMitigation(state, attacker, defender, out int coverDef, out int coverGrd))
            return;

        int beforeHp = hpDamage;
        int beforePg = pgDamage;
        hpDamage = Mathf.Max(0, hpDamage - coverDef);
        pgDamage = Mathf.Max(0, pgDamage - coverGrd);
        outcome.reducedHpByCover += beforeHp - hpDamage;
        outcome.reducedPgByCover += beforePg - pgDamage;
        outcome.coverBufferApplied = outcome.reducedHpByCover > 0 || outcome.reducedPgByCover > 0;
    }

    static bool TryGetCoverMitigation(
        SrpBattleState state,
        SrpUnitRuntime attacker,
        SrpUnitRuntime defender,
        out int coverDef,
        out int coverGrd)
    {
        coverDef = 0;
        coverGrd = 0;
        if (state == null || attacker == null || defender == null || !defender.coverActive)
            return false;
        if (attacker.weaponClass != SrpWeaponClass.Firearm)
            return false;
        if (state.ChebyshevAnchor(attacker, defender) <= 1)
            return false;

        if (state.HasCoverBetween(attacker, defender, out var segment)
            && state.HasAdjacentCoverSource(defender, segment.x, segment.y))
        {
            coverDef = segment.coverDef > 0 ? segment.coverDef : CoverDef;
            coverGrd = segment.coverGrd > 0 ? segment.coverGrd : CoverGrd;
            return true;
        }

        if (state.IsCoverTile(defender.coverSourceX, defender.coverSourceY)
            && state.HasAdjacentCoverSource(defender, defender.coverSourceX, defender.coverSourceY))
        {
            coverDef = CoverDef;
            coverGrd = CoverGrd;
            return true;
        }

        return false;
    }

    static void RecordDefensivePressure(SrpBattleState state, SrpUnitRuntime defender)
    {
        if (state == null || defender == null || defender.stance != SrpStance.Defensive)
            return;
        if (defender.defensiveHitsRound != state.RoundNumber)
        {
            defender.defensiveHitsRound = state.RoundNumber;
            defender.defensiveHitsTakenThisRound = 0;
        }
        defender.defensiveHitsTakenThisRound++;
    }

    static void ApplyFirearmHpSpillover(SrpUnitRuntime attacker, int hpDamage, ref int pgDamage)
    {
        if (attacker == null || attacker.weaponClass != SrpWeaponClass.Firearm || hpDamage <= 0)
            return;

        int spillover = Mathf.FloorToInt(hpDamage * FirearmHpToPgSpilloverPercent / 100f);
        pgDamage += spillover;
    }

    static void ApplyReactionIfAvailable(
        SrpBattleState state,
        SrpUnitRuntime attacker,
        SrpUnitRuntime defender,
        SrpSkillData attackSkill,
        ref int hpDamage,
        ref int pgDamage,
        ref AttackOutcome outcome)
    {
        if (state == null || defender.reactionPoints <= 0)
            return;

        SrpReactionKind reaction = ChooseReaction(state, attacker, defender, attackSkill);
        if (reaction == SrpReactionKind.None)
            return;

        defender.reactionPoints = Mathf.Max(0, defender.reactionPoints - 1);
        defender.lastReactionKind = reaction;
        defender.lastReactionRound = state.RoundNumber;
        defender.lastReactionSourceId = attacker != null ? attacker.id : -1;
        outcome.reactionKind = reaction;
        outcome.reactionSpentRp = true;

        if (reaction == SrpReactionKind.Guard)
        {
            int beforeHp = hpDamage;
            int beforePg = pgDamage;
            hpDamage = Mathf.Max(0, hpDamage - GuardReactionDef);
            pgDamage = Mathf.Max(0, pgDamage - GuardReactionGrd);
            outcome.reducedHpByDef += beforeHp - hpDamage;
            outcome.reducedPgByGrd += beforePg - pgDamage;
        }
        else if (reaction == SrpReactionKind.Dodge)
        {
            if (DoesDodgeSucceed(state, attacker, defender))
            {
                outcome.wasDodged = true;
                hpDamage = 0;
                pgDamage = 0;
            }
            else
            {
                outcome.dodgeFailed = true;
            }
        }
        else if (reaction == SrpReactionKind.Parry)
        {
            outcome.wasParried = true;
            hpDamage = 0;
            pgDamage = 0;
        }
    }

    static SrpReactionKind ChooseReaction(SrpBattleState state, SrpUnitRuntime attacker, SrpUnitRuntime defender, SrpSkillData attackSkill)
    {
        if (defender == null)
            return SrpReactionKind.None;

        if (CanDefenderParry(state, attacker, defender, attackSkill))
            return SrpReactionKind.Parry;
        if (CanDefenderDodge(state, attacker, defender))
            return SrpReactionKind.Dodge;
        if (defender.stance == SrpStance.Defensive)
            return SrpReactionKind.Guard;
        return SrpReactionKind.None;
    }

    public static bool CanDefenderDodge(SrpBattleState state, SrpUnitRuntime attacker, SrpUnitRuntime defender)
    {
        if (state == null || attacker == null || defender == null)
            return false;
        if (attacker.eliminated || defender.eliminated || attacker.owner == defender.owner)
            return false;
        if (defender.stance != SrpStance.Aggressive || defender.reactionPoints <= 0)
            return false;
        if (attacker.weaponClass == SrpWeaponClass.Melee)
            return false;
        return state.ChebyshevAnchor(attacker, defender) > 1;
    }

    static bool DoesDodgeSucceed(SrpBattleState state, SrpUnitRuntime attacker, SrpUnitRuntime defender)
    {
        if (state == null || attacker == null || defender == null)
            return false;

        int seed = state.RoundNumber
            + attacker.id
            + defender.id
            + attacker.anchorX
            + attacker.anchorY
            + defender.anchorX
            + defender.anchorY;
        int roll = Mathf.Abs(seed) % 100;
        return roll < DodgeSuccessChancePercent;
    }

    static void ApplyDirectionalVulnerability(
        SrpUnitRuntime attacker,
        SrpUnitRuntime defender,
        ref int hpDamage,
        ref int pgDamage)
    {
        if (attacker == null || defender == null)
            return;
        if (attacker.anchorX == defender.anchorX && attacker.anchorY == defender.anchorY)
            return;
        if (IsAttackerInDefenderFrontArc(attacker, defender))
            return;

        bool isBackAttack = IsAttackerBehindDefender(attacker, defender);
        hpDamage += isBackAttack ? BackAttackHpBonus : SideAttackHpBonus;
        pgDamage += isBackAttack ? BackAttackPgBonus : SideAttackPgBonus;
    }

    static bool IsAttackerBehindDefender(SrpUnitRuntime attacker, SrpUnitRuntime defender)
    {
        int dx = attacker.anchorX - defender.anchorX;
        int dy = attacker.anchorY - defender.anchorY;
        if (dx == 0 && dy == 0)
            return false;

        switch (defender.facing)
        {
            case SrpFacing.North:
                return dy < 0 && Mathf.Abs(dy) >= Mathf.Abs(dx);
            case SrpFacing.East:
                return dx < 0 && Mathf.Abs(dx) >= Mathf.Abs(dy);
            case SrpFacing.South:
                return dy > 0 && Mathf.Abs(dy) >= Mathf.Abs(dx);
            case SrpFacing.West:
                return dx > 0 && Mathf.Abs(dx) >= Mathf.Abs(dy);
            default:
                return false;
        }
    }

    static int GetDef(SrpUnitRuntime defender)
    {
        return defender != null && defender.stance == SrpStance.Defensive ? DefensiveStanceDef : 0;
    }

    static int GetGrd(SrpUnitRuntime defender)
    {
        return defender != null && defender.stance == SrpStance.Defensive ? DefensiveStanceGrd : 0;
    }
}
