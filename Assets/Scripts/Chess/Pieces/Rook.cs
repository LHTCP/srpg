using UnityEngine;
using System.Collections.Generic;

public class Rook : ChessPiece
{
    public override List<ChessTile> GetPossibleMoves()
    {
        return GetStraightMoves();
    }
} 