using UnityEngine;
using System.Collections.Generic;

public class Queen : ChessPiece
{
    public override List<ChessTile> GetPossibleMoves()
    {
        List<ChessTile> possibleMoves = new List<ChessTile>();
        
        // 가로세로 + 대각선 모든 방향
        possibleMoves.AddRange(GetStraightMoves());
        possibleMoves.AddRange(GetDiagonalMoves());
        
        return possibleMoves;
    }
} 