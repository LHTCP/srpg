using UnityEngine;
using System.Collections.Generic;

public class Knight : ChessPiece
{
    public override List<ChessTile> GetPossibleMoves()
    {
        List<ChessTile> possibleMoves = new List<ChessTile>();
        
        int currentX = currentTile.x;
        int currentY = currentTile.y;
        
        // 나이트의 8가지 이동 패턴 (L자 형태)
        Vector2Int[] knightMoves = {
            new Vector2Int(2, 1),   new Vector2Int(2, -1),
            new Vector2Int(-2, 1),  new Vector2Int(-2, -1),
            new Vector2Int(1, 2),   new Vector2Int(1, -2),
            new Vector2Int(-1, 2),  new Vector2Int(-1, -2)
        };
        
        foreach (Vector2Int move in knightMoves)
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