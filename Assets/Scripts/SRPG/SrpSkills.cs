using UnityEngine;

/// <summary>
/// 스킬 ID별 효과 (프로토타입: 일부만 실제 수치 반영).
/// </summary>
public static class SrpSkills
{
    public static bool TryApplyPassiveTurnStart(SrpUnitRuntime u, SrpBattleState state, System.Action<string> log)
    {
        foreach (var sid in u.skillIds)
        {
            if (sid == "fh_bless_ally")
            {
                int bonus = 2;
                u.frozenHeart = Mathf.Max(0, u.frozenHeart + bonus);
                log?.Invoke($"[스킬] {sid}: {u.templateId} FH +{bonus} (자기 턴 시작)");
                return true;
            }
        }
        return false;
    }

    public static void OnAttackResolved(SrpUnitRuntime attacker, SrpUnitRuntime defender, SrpCombatResolver.AttackOutcome outcome, System.Action<string> log)
    {
        foreach (var sid in attacker.skillIds)
        {
            if (sid == "heart_spike")
            {
                int d = 5;
                attacker.frozenHeart = Mathf.Max(0, attacker.frozenHeart + d);
                log?.Invoke($"[스킬] heart_spike: 공격자 FH +{d}");
            }
        }
    }
}
