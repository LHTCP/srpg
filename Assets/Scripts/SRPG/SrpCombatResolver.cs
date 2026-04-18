using UnityEngine;

/// <summary>
/// AP → HP, 자세, 그로기, 처단.
/// </summary>
public static class SrpCombatResolver
{
    public struct AttackOutcome
    {
        public int damageToPg;
        public int damageToAp;
        public int damageToHp;
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

    public static AttackOutcome ApplyAttack(SrpUnitRuntime attacker, SrpUnitRuntime defender)
    {
        var o = new AttackOutcome();
        int raw = Mathf.Max(1, attacker.attackPower);
        int hpDamage;
        int pgDamage;

        if (defender.pg <= 0 || defender.groggy)
        {
            o.wasExecution = true;
            hpDamage = raw + 6;
            pgDamage = 0;
        }
        else
        {
            switch (attacker.weaponClass)
            {
                case SrpWeaponClass.Firearm:
                    hpDamage = raw;
                    pgDamage = Mathf.Max(1, raw / 3);
                    break;
                case SrpWeaponClass.Magic:
                    hpDamage = Mathf.Max(1, raw / 2);
                    pgDamage = Mathf.Max(1, raw / 2);
                    break;
                case SrpWeaponClass.Melee:
                default:
                    hpDamage = Mathf.Max(1, raw / 3);
                    pgDamage = raw + 2;
                    break;
            }

            if (attacker.stance == SrpStance.Aggressive)
                pgDamage += 1;
            if (defender.stance == SrpStance.Defensive)
            {
                hpDamage = Mathf.Max(0, hpDamage - 1);
                pgDamage = Mathf.Max(0, pgDamage - 1);
            }
        }

        defender.hp -= hpDamage;
        o.damageToHp = hpDamage;
        o.damageToPg = pgDamage;
        o.damageToAp = 0;

        if (!o.wasExecution)
        {
            int prevPg = defender.pg;
            defender.pg = Mathf.Max(0, defender.pg - pgDamage);
            if (prevPg > 0 && defender.pg <= 0)
            {
                defender.groggy = true;
                o.becameGroggy = true;
            }
        }
        else
        {
            defender.pg = 0;
            defender.groggy = false;
        }

        if (defender.hp <= 0)
            o.defenderDied = true;
        return o;
    }
}
