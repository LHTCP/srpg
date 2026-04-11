using UnityEngine;
using System.Collections.Generic;

public class King : ChessPiece
{
    public override List<ChessTile> GetPossibleMoves()
    {
        List<ChessTile> possibleMoves = new List<ChessTile>();
        
        int currentX = currentTile.x;
        int currentY = currentTile.y;
        
        // 킹의 8방향 이동 (한 칸씩)
        Vector2Int[] kingMoves = {
            new Vector2Int(1, 0),   new Vector2Int(-1, 0),
            new Vector2Int(0, 1),   new Vector2Int(0, -1),
            new Vector2Int(1, 1),   new Vector2Int(1, -1),
            new Vector2Int(-1, 1),  new Vector2Int(-1, -1)
        };
        
        foreach (Vector2Int move in kingMoves)
        {
            int newX = currentX + move.x;
            int newY = currentY + move.y;
            
            if (IsValidPosition(newX, newY))
            {
                ChessTile targetTile = chessBoard.GetTile(newX, newY);
                
                // 빈 칸이거나 적의 말이 있는 경우
                if (!targetTile.IsOccupied() || targetTile.HasEnemyPiece(isWhite))
                {
                    possibleMoves.Add(targetTile);
                }
            }
        }
        
        return possibleMoves;
    }
} 