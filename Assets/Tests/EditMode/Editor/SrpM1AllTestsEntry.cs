using NUnit.Framework;

[Category("SrpM1All")]
public class SrpM1AllTestsEntry
{
    [Test]
    public void Run_All_M1_EditMode_Core_Tests()
    {
        var core = new SrpM1CoreTests();
        core.TurnOrder_UsesSpeedDescending();
        core.CombatSplit_FirearmAndMeleeProduceDifferentPressure();

        var spec = new SrpM1RuleSpecTests();
        spec.ZocPenalty_IncreasesMoveCost_WhenEnemyAdjacent();
        spec.Stance_Aggressive_IncreasesPgPressure();
        spec.Stance_Defensive_ReducesIncomingDamage();
        spec.Execution_Triggers_WhenDefenderPgZeroOrGroggy();
    }
}
