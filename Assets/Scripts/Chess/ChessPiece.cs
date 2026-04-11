using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 체스 말의 종류를 나타내는 열거형
/// </summary>
public enum PieceType
{
    Pawn,    // 폰
    Rook,    // 룩
    Knight,  // 나이트
    Bishop,  // 비숍
    Queen,   // 퀸
    King     // 킹
}

/// <summary>
/// 모든 체스 말의 기본이 되는 추상 클래스
/// - 각 말의 공통 기능 제공
/// - 말의 이동, 위치 관리
/// - 파생 클래스에서 각 말의 고유한 이동 규칙을 구현해야 함
/// </summary>
public abstract class ChessPiece : MonoBehaviour
{
    [Header("말 정보")]
    [Tooltip("이 말의 종류 (폰, 룩, 나이트 등)")]
    public PieceType pieceType;
    
    [Tooltip("흰색 말인지 여부 (false면 검은색)")]
    public bool isWhite;
    
    [Tooltip("현재 이 말이 위치한 타일")]
    public ChessTile currentTile;
    
    [Header("이동 관련")]
    [Tooltip("이 말이 한 번이라도 움직였는지 여부 (캐슬링, 폰 2칸 이동에 사용)")]
    public bool hasMoved = false;
    
    protected ChessBoard chessBoard;
    protected Renderer pieceRenderer;
    
    public virtual void Initialize(bool isWhite, ChessTile startTile, ChessBoard board)
    {
        this.isWhite = isWhite;
        this.chessBoard = board;
        this.pieceRenderer = GetComponent<Renderer>();
        
        // 말 색상 설정
        Material material = new Material(pieceRenderer.material);
        material.color = isWhite ? Color.white : Color.black;
        pieceRenderer.material = material;
        
        // 시작 위치 설정
        MoveTo(startTile);
        
        // 말 이름 설정
        gameObject.name = $"{(isWhite ? "White" : "Black")}_{pieceType}";
    }
    
    public virtual void MoveTo(ChessTile targetTile)
    {
        // 이전 타일에서 말 제거
        if (currentTile != null)
            currentTile.RemovePiece();
        
        // 새 타일로 이동 - SetPiece에서 위치를 자동으로 맞춰줌
        targetTile.SetPiece(this);
        
        hasMoved = true;
    }
    
    public abstract List<ChessTile> GetPossibleMoves();
    
    protected bool IsValidPosition(int x, int y)
    {
        return x >= 0 && x < 8 && y >= 0 && y < 8;
    }
    
    protected List<ChessTile> GetLineMoves(Vector2Int direction, int maxDistance = 8)
    {
        List<ChessTile> possibleMoves = new List<ChessTile>();
        
        for (int i = 1; i <= maxDistance; i++)
        {
            int newX = currentTile.x + direction.x * i;
            int newY = currentTile.y + direction.y * i;
            
            if (!IsValidPosition(newX, newY))
                break;
                
            ChessTile targetTile = chessBoard.GetTile(newX, newY);
            
            if (targetTile.IsOccupied())
            {
                if (targetTile.HasEnemyPiece(isWhite))
                    possibleMoves.Add(targetTile);
                break;
            }
            else
            {
                possibleMoves.Add(targetTile);
            }
        }
        
        return possibleMoves;
    }
    
    protected List<ChessTile> GetDiagonalMoves(int maxDistance = 8)
    {
        List<ChessTile> possibleMoves = new List<ChessTile>();
        
        Vector2Int[] directions = {
            new Vector2Int(1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, 1),
            new Vector2Int(-1, -1)
        };
        
        foreach (Vector2Int direction in directions)
        {
            possibleMoves.AddRange(GetLineMoves(direction, maxDistance));
        }
        
        return possibleMoves;
    }
    
    protected List<ChessTile> GetStraightMoves(int maxDistance = 8)
    {
        List<ChessTile> possibleMoves = new List<ChessTile>();
        
        Vector2Int[] directions = {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };
        
        foreach (Vector2Int direction in directions)
        {
            possibleMoves.AddRange(GetLineMoves(direction, maxDistance));
        }
        
        return possibleMoves;
    }
    
    void OnMouseDown()
    {
        ChessGameManager.Instance?.OnPieceClicked(this);
    }
} 