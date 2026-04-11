using UnityEngine;
using System.Collections.Generic;

public class Pawn : ChessPiece
{
    public override List<ChessTile> GetPossibleMoves()
    {
        List<ChessTile> possibleMoves = new List<ChessTile>();
        
        int direction = isWhite ? 1 : -1; // 백은 위로(+), 흑은 아래로(-)
        int currentX = currentTile.x;
        int currentY = currentTile.y;
        
        // 앞으로 한 칸 이동
        int forwardY = currentY + direction;
        if (IsValidPosition(currentX, forwardY))
        {
            ChessTile forwardTile = chessBoard.GetTile(currentX, forwardY);
            if (!forwardTile.IsOccupied())
            {
                possibleMoves.Add(forwardTile);
                
                // 첫 이동시 두 칸 이동 가능
                if (!hasMoved)
                {
                    int doubleForwardY = currentY + (direction * 2);
                    if (IsValidPosition(currentX, doubleForwardY))
                    {
                        ChessTile doubleForwardTile = chessBoard.GetTile(currentX, doubleForwardY);
                        if (!doubleForwardTile.IsOccupied())
                        {
                            possibleMoves.Add(doubleForwardTile);
                        }
                    }
                }
            }
        }
        
        // 대각선 공격 (왼쪽)
        int leftX = currentX - 1;
        if (IsValidPosition(leftX, forwardY))
        {
            ChessTile leftDiagonalTile = chessBoard.GetTile(leftX, forwardY);
            if (leftDiagonalTile.HasEnemyPiece(isWhite))
            {
                possibleMoves.Add(leftDiagonalTile);
            }
        }
        
        // 대각선 공격 (오른쪽)
        int rightX = currentX + 1;
        if (IsValidPosition(rightX, forwardY))
        {
            ChessTile rightDiagonalTile = chessBoard.GetTile(rightX, forwardY);
            if (rightDiagonalTile.HasEnemyPiece(isWhite))
            {
                possibleMoves.Add(rightDiagonalTile);
            }
        }
        
        return possibleMoves;
    }
} 