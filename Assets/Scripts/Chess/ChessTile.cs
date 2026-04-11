using UnityEngine;

/// <summary>
/// 체스판의 각 칸(타일)을 나타내는 클래스
/// - 타일의 위치와 색상 관리
/// - 현재 위치한 체스 말 관리
/// - 마우스 상호작용 처리
/// </summary>
public class ChessTile : MonoBehaviour
{
    [Header("타일 정보")]
    [Tooltip("체스판에서의 X 좌표 (0~7)")]
    public int x;
    
    [Tooltip("체스판에서의 Y 좌표 (0~7)")]
    public int y;
    
    [Tooltip("흰색 타일인지 여부 (체스판 패턴)")]
    public bool isWhite;
    
    [Tooltip("현재 이 타일 위에 있는 체스 말")]
    public ChessPiece currentPiece;
    
    [Header("타일 색상")]
    [Tooltip("흰색 타일의 색상")]
    public Color whiteColor = Color.white;
    
    [Tooltip("검은색 타일의 색상")]
    public Color blackColor = new Color(0.6f, 0.4f, 0.2f);
    
    [Tooltip("타일이 선택되었을 때의 색상")]
    public Color highlightColor = Color.yellow;
    
    [Tooltip("이동 가능한 위치를 표시할 때의 색상")]
    public Color possibleMoveColor = Color.green;
    
    private Renderer tileRenderer;
    private Color originalColor;
    
    void Awake()
    {
        tileRenderer = GetComponent<Renderer>();
        SetTileColor();
    }
    
    void SetTileColor()
    {
        originalColor = isWhite ? whiteColor : blackColor;
        tileRenderer.material.color = originalColor;
    }
    
    public void SetPosition(int x, int y)
    {
        this.x = x;
        this.y = y;
        this.isWhite = (x + y) % 2 == 0;
        gameObject.name = $"Tile_{x}_{y}";
        
        if (tileRenderer != null)
            SetTileColor();
    }
    
    public void SetPiece(ChessPiece piece)
    {
        currentPiece = piece;
        if (piece != null)
        {
            piece.currentTile = this;
            // 말을 타일과 같은 위치에 배치하되, Y축은 0.5f로 떠있게 함
            Vector3 tilePosition = transform.position;
            piece.transform.position = new Vector3(tilePosition.x, 0.5f, tilePosition.z);
        }
    }
    
    public void RemovePiece()
    {
        if (currentPiece != null)
        {
            currentPiece.currentTile = null;
            currentPiece = null;
        }
    }
    
    public void HighlightTile()
    {
        tileRenderer.material.color = highlightColor;
    }
    
    public void ShowPossibleMove()
    {
        tileRenderer.material.color = possibleMoveColor;
    }
    
    public void ResetColor()
    {
        tileRenderer.material.color = originalColor;
    }
    
    public bool IsOccupied()
    {
        return currentPiece != null;
    }
    
    public bool HasEnemyPiece(bool isWhitePiece)
    {
        return IsOccupied() && currentPiece.isWhite != isWhitePiece;
    }
    
    public bool HasFriendlyPiece(bool isWhitePiece)
    {
        return IsOccupied() && currentPiece.isWhite == isWhitePiece;
    }
    
    void OnMouseDown()
    {
        ChessGameManager.Instance?.OnTileClicked(this);
    }
    
    void OnMouseEnter()
    {
        if (currentPiece == null)
            tileRenderer.material.color = Color.Lerp(originalColor, highlightColor, 0.3f);
    }
    
    void OnMouseExit()
    {
        if (currentPiece == null)
            ResetColor();
    }
} 