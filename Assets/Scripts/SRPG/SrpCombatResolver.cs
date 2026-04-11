using UnityEngine;

/// <summary>
/// AP → HP, 자세, 그로기, 처단.
/// </summary>
public static class SrpCombatResolver
{
    const float PostureFromHpDamageRatio = 0.5f;

    public struct AttackOutcome
    {
        public int damageToAp;
        public int damageToHp;
        public bool wasExecution;
        public bool defenderDied;
        public int postureGained;
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
        int raw = attacker.attackPower;

        if (defender.groggy)
        {
            o.wasExecution = true;
            o.damageToAp = 0;
            o.damageToHp = raw;
            defender.hp -= raw;
            defender.groggy = false;
            defender.posture = 0;
        }
        else
        {
            int apBlock = Mathf.Min(raw, defender.ap);
            o.damageToAp = apBlock;
            defender.ap -= apBlock;
            int remain = raw - apBlock;
            o.damageToHp = remain;
            defender.hp -= remain;
            if (remain > 0)
            {
                o.postureGained = Mathf.Max(1, Mathf.RoundToInt(remain * PostureFromHpDamageRatio));
                defender.posture += o.postureGained;
                if (defender.posture >= defender.maxPosture)
                {
                    defender.groggy = true;
                    o.becameGroggy = true;
                    defender.posture = defender.maxPosture;
                }
            }
        }

        if (defender.hp <= 0)
            o.defenderDied = true;
        return o;
    }
}
