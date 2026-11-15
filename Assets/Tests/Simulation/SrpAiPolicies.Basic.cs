using System.Collections.Generic;

public class SrpRandomAiPolicy : ISrpAiPolicy
{
    public string Name => "Random";

    public SrpAiCommand SelectAction(SrpAiDecisionContext ctx)
    {
        bool canMove = !ctx.moved && ctx.moves.Count > 0 && ctx.actor.actionPoints > 0;
        bool canAttack = !ctx.attacked && ctx.attacks.Count > 0 && ctx.actor.actionPoints > 0;
        if (!canMove && !canAttack)
            return SrpAiCommand.EndTurn();

        if (canMove && canAttack)
        {
            // 공격을 약간 우선시켜 전투가 질질 끌리지 않게 한다.
            bool pickAttack = ctx.rng.NextDouble() < 0.6d;
            if (pickAttack)
                return PickAttack(ctx.attacks, ctx.rng);
            return PickMove(ctx.moves, ctx.rng);
        }

        if (canAttack)
            return PickAttack(ctx.attacks, ctx.rng);
        return PickMove(ctx.moves, ctx.rng);
    }

    static SrpAiCommand PickMove(List<SrpAiMoveOption> moves, System.Random rng)
    {
        int idx = rng.Next(0, moves.Count);
        var move = moves[idx];
        return SrpAiCommand.MoveTo(move.x, move.y);
    }

    static SrpAiCommand PickAttack(List<SrpAiAttackOption> attacks, System.Random rng)
    {
        int idx = rng.Next(0, attacks.Count);
        return SrpAiCommand.Attack(attacks[idx].targetUnitId);
    }
}

/// <summary>
/// 런타임 적 AI(SrpEnemyAi)와 같은 판단을 쓰는 정책. 시뮬레이션이 실제 AI를 측정하게 한다.
/// </summary>
public class SrpHeuristicAiPolicy : ISrpAiPolicy
{
    public string Name => "Heuristic";

    public SrpAiCommand SelectAction(SrpAiDecisionContext ctx)
    {
        var action = SrpEnemyAi.Decide(ctx.state, ctx.actor, ctx.remainingMove, ctx.attacked);
        switch (action.type)
        {
            case SrpEnemyAiActionType.Move:
                return SrpAiCommand.MoveTo(action.x, action.y);
            case SrpEnemyAiActionType.Attack:
                return SrpAiCommand.Attack(action.targetUnitId);
            case SrpEnemyAiActionType.Reload:
                return SrpAiCommand.Reload();
            case SrpEnemyAiActionType.UseSkill:
                return SrpAiCommand.UseSkill(action.skillId, action.x, action.y, action.targetUnitId);
            case SrpEnemyAiActionType.Overwatch:
                return SrpAiCommand.Overwatch();
            case SrpEnemyAiActionType.TakeCover:
                return SrpAiCommand.TakeCover();
            case SrpEnemyAiActionType.Interact:
                return SrpAiCommand.Interact();
            default:
                return SrpAiCommand.EndTurn();
        }
    }
}
