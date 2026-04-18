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

public class SrpHeuristicAiPolicy : ISrpAiPolicy
{
    public string Name => "Heuristic";

    public SrpAiCommand SelectAction(SrpAiDecisionContext ctx)
    {
        bool canMove = !ctx.moved && ctx.moves.Count > 0 && ctx.actor.actionPoints > 0;
        bool canAttack = !ctx.attacked && ctx.attacks.Count > 0 && ctx.actor.actionPoints > 0;
        if (!canMove && !canAttack)
            return SrpAiCommand.EndTurn();

        if (canAttack)
        {
            int bestTarget = PickBestAttackTarget(ctx);
            return SrpAiCommand.Attack(bestTarget);
        }

        if (canMove)
        {
            var bestMove = PickBestMove(ctx);
            return SrpAiCommand.MoveTo(bestMove.x, bestMove.y);
        }

        return SrpAiCommand.EndTurn();
    }

    int PickBestAttackTarget(SrpAiDecisionContext ctx)
    {
        int bestTargetId = ctx.attacks[0].targetUnitId;
        int bestScore = int.MinValue;
        foreach (var atk in ctx.attacks)
        {
            var target = FindUnit(ctx.state, atk.targetUnitId);
            if (target == null) continue;

            int score = 0;
            score += (target.maxHp - target.hp);
            score += (target.maxPg - target.pg);
            if (target.pg <= 0 || target.groggy)
                score += 50;
            if (target.weaponClass == SrpWeaponClass.Firearm)
                score += 5;
            if (score > bestScore)
            {
                bestScore = score;
                bestTargetId = target.id;
            }
        }
        return bestTargetId;
    }

    SrpAiMoveOption PickBestMove(SrpAiDecisionContext ctx)
    {
        var best = ctx.moves[0];
        int bestScore = int.MinValue;

        foreach (var move in ctx.moves)
        {
            int minDist = int.MaxValue;
            foreach (var unit in ctx.state.Units)
            {
                if (unit.eliminated || unit.owner == ctx.actor.owner)
                    continue;
                int d = Manhattan(move.x, move.y, unit.anchorX, unit.anchorY);
                if (d < minDist)
                    minDist = d;
            }

            // 적에게 접근하되 과도한 ZOC 페널티는 피한다.
            int score = 0;
            score += (30 - minDist * 4);
            score -= move.zocPenalty * 3;
            score -= move.cost;
            if (score > bestScore)
            {
                bestScore = score;
                best = move;
            }
        }
        return best;
    }

    static int Manhattan(int x1, int y1, int x2, int y2)
    {
        return UnityEngine.Mathf.Abs(x1 - x2) + UnityEngine.Mathf.Abs(y1 - y2);
    }

    static SrpUnitRuntime FindUnit(SrpBattleState state, int id)
    {
        foreach (var unit in state.Units)
            if (unit.id == id)
                return unit;
        return null;
    }
}
