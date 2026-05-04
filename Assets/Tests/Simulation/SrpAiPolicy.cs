using System;
using System.Collections.Generic;

public enum SrpAiActionType
{
    EndTurn,
    Move,
    Attack,
}

public struct SrpAiMoveOption
{
    public int x;
    public int y;
    public int cost;
    public int zocPenalty;
}

public struct SrpAiAttackOption
{
    public int targetUnitId;
}

public struct SrpAiDecisionContext
{
    public SrpBattleState state;
    public SrpUnitRuntime actor;
    public int remainingMove;
    public bool moved;
    public bool attacked;
    public List<SrpAiMoveOption> moves;
    public List<SrpAiAttackOption> attacks;
    public System.Random rng;
}

public struct SrpAiCommand
{
    public SrpAiActionType actionType;
    public int x;
    public int y;
    public int targetUnitId;

    public static SrpAiCommand EndTurn()
    {
        return new SrpAiCommand { actionType = SrpAiActionType.EndTurn };
    }

    public static SrpAiCommand MoveTo(int x, int y)
    {
        return new SrpAiCommand
        {
            actionType = SrpAiActionType.Move,
            x = x,
            y = y,
        };
    }

    public static SrpAiCommand Attack(int targetUnitId)
    {
        return new SrpAiCommand
        {
            actionType = SrpAiActionType.Attack,
            targetUnitId = targetUnitId,
        };
    }
}

public interface ISrpAiPolicy
{
    string Name { get; }
    SrpAiCommand SelectAction(SrpAiDecisionContext ctx);
}
