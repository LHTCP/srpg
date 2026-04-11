using UnityEngine;
using System.Collections.Generic;

public class Bishop : ChessPiece
{
    public override List<ChessTile> GetPossibleMoves()
    {
        return GetDiagonalMoves();
    }
} 