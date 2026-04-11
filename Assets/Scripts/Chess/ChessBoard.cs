using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 체스 게임의 메인 보드를 관리하는 클래스
/// - 8x8 체스판 생성
/// - 체스 말들의 초기 배치
/// - 보드 상태 관리
/// </summary>
public class ChessBoard : MonoBehaviour
{
    [Header("보드 설정")]
    [Tooltip("타일로 사용할 프리팹. 없으면 기본 큐브를 생성합니다.")]
    public GameObject tilePrefab;
    
    [Tooltip("체스 말 프리팹들 (순서: Pawn, Rook, Knight, Bishop, Queen, King)")]
    public GameObject[] piecePrefabs; // Pawn=0, Rook=1, Knight=2, Bishop=3, Queen=4, King=5
    
    [Header("보드 크기")]
    [Tooltip("각 타일의 크기 (Unity 단위)")]
    public float tileSize = 1f;
    
    // 2차원 배열로 8x8 체스판의 모든 타일을 저장
    private ChessTile[,] tiles = new ChessTile[8, 8];
    
    // 현재 보드 위의 모든 체스 말들을 색깔별로 관리
    private List<ChessPiece> whitePieces = new List<ChessPiece>();
    private List<ChessPiece> blackPieces = new List<ChessPiece>();
    
    void Start()
    {
        InitializeBoard();
    }
    
    public void InitializeBoard()
    {
        CreateBoard();
        SetupPieces();
    }
    
    void CreateBoard()
    {
        // 보드를 중앙에 배치하기 위한 오프셋 (0,0부터 7,7까지의 중앙값)
        Vector3 boardCenter = new Vector3(3.5f * tileSize, 0, 3.5f * tileSize);
        
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                // 타일 생성 - 보드 중앙을 원점으로 맞춤
                Vector3 position = new Vector3(x * tileSize, 0, y * tileSize) - boardCenter;
                GameObject tileObject;
                
                if (tilePrefab != null)
                {
                    tileObject = Instantiate(tilePrefab, position, Quaternion.identity, transform);
                }
                else
                {
                    // 프리팹이 없을 경우 기본 큐브 생성
                    tileObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tileObject.transform.position = position;
                    tileObject.transform.localScale = new Vector3(tileSize, 0.1f, tileSize);
                    tileObject.transform.parent = transform;
                }
                
                // ChessTile 컴포넌트 추가 및 설정
                ChessTile tile = tileObject.GetComponent<ChessTile>();
                if (tile == null)
                    tile = tileObject.AddComponent<ChessTile>();
                
                tile.SetPosition(x, y);
                tiles[x, y] = tile;
            }
        }
    }
    
    void SetupPieces()
    {
        // 폰 배치
        for (int x = 0; x < 8; x++)
        {
            CreatePiece(PieceType.Pawn, true, x, 1);   // 백 폰
            CreatePiece(PieceType.Pawn, false, x, 6);  // 흑 폰
        }
        
        // 다른 말들 배치
        PieceType[] backRow = { 
            PieceType.Rook, PieceType.Knight, PieceType.Bishop, PieceType.Queen,
            PieceType.King, PieceType.Bishop, PieceType.Knight, PieceType.Rook 
        };
        
        for (int x = 0; x < 8; x++)
        {
            CreatePiece(backRow[x], true, x, 0);   // 백 말들
            CreatePiece(backRow[x], false, x, 7);  // 흑 말들
        }
    }
    
    void CreatePiece(PieceType pieceType, bool isWhite, int x, int y)
    {
        GameObject pieceObject;
        
        // 프리팹이 있으면 사용, 없으면 기본 모양 생성
        int pieceIndex = (int)pieceType;
        if (piecePrefabs != null && pieceIndex < piecePrefabs.Length && piecePrefabs[pieceIndex] != null)
        {
            pieceObject = Instantiate(piecePrefabs[pieceIndex]);
        }
        else
        {
            // 기본 모양으로 말 생성
            pieceObject = CreateDefaultPiece(pieceType);
        }
        
        // 체스 말 컴포넌트 추가
        ChessPiece chessPiece = AddChessPieceComponent(pieceObject, pieceType);
        
        // 말 초기화
        chessPiece.Initialize(isWhite, tiles[x, y], this);
        
        // 말 리스트에 추가
        if (isWhite)
            whitePieces.Add(chessPiece);
        else
            blackPieces.Add(chessPiece);
    }
    
    GameObject CreateDefaultPiece(PieceType pieceType)
    {
        GameObject piece;
        
        switch (pieceType)
        {
            case PieceType.Pawn:
                piece = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                piece.transform.localScale = new Vector3(0.6f, 0.8f, 0.6f);
                break;
            case PieceType.Rook:
                piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                piece.transform.localScale = new Vector3(0.8f, 1.2f, 0.8f);
                break;
            case PieceType.Knight:
                piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                piece.transform.localScale = new Vector3(0.7f, 1.4f, 0.7f);
                break;
            case PieceType.Bishop:
                piece = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                piece.transform.localScale = new Vector3(0.7f, 1.3f, 0.7f);
                break;
            case PieceType.Queen:
                piece = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                piece.transform.localScale = new Vector3(0.9f, 1.5f, 0.9f);
                break;
            case PieceType.King:
                piece = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                piece.transform.localScale = new Vector3(0.8f, 1.6f, 0.8f);
                break;
            default:
                piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                break;
        }
        
        return piece;
    }
    
    ChessPiece AddChessPieceComponent(GameObject pieceObject, PieceType pieceType)
    {
        ChessPiece chessPiece = null;
        
        switch (pieceType)
        {
            case PieceType.Pawn:
                chessPiece = pieceObject.AddComponent<Pawn>();
                break;
            case PieceType.Rook:
                chessPiece = pieceObject.AddComponent<Rook>();
                break;
            case PieceType.Knight:
                chessPiece = pieceObject.AddComponent<Knight>();
                break;
            case PieceType.Bishop:
                chessPiece = pieceObject.AddComponent<Bishop>();
                break;
            case PieceType.Queen:
                chessPiece = pieceObject.AddComponent<Queen>();
                break;
            case PieceType.King:
                chessPiece = pieceObject.AddComponent<King>();
                break;
        }
        
        if (chessPiece != null)
            chessPiece.pieceType = pieceType;
            
        return chessPiece;
    }
    
    public ChessTile GetTile(int x, int y)
    {
        if (x >= 0 && x < 8 && y >= 0 && y < 8)
            return tiles[x, y];
        return null;
    }
    
    public List<ChessPiece> GetPieces(bool isWhite)
    {
        return isWhite ? whitePieces : blackPieces;
    }
    
    public void RemovePiece(ChessPiece piece)
    {
        if (piece.isWhite)
            whitePieces.Remove(piece);
        else
            blackPieces.Remove(piece);
            
        Destroy(piece.gameObject);
    }
    
    public void HighlightPossibleMoves(List<ChessTile> possibleMoves)
    {
        foreach (ChessTile tile in possibleMoves)
        {
            tile.ShowPossibleMove();
        }
    }
    
    public void ResetAllHighlights()
    {
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                tiles[x, y].ResetColor();
            }
        }
    }
} 