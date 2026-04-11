using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 체스 게임 전체를 관리하는 메인 매니저 클래스
/// - 턴 시스템 관리 (흰색/검은색 교대)
/// - 말 선택 및 이동 처리
/// - 게임 상태 확인 및 UI 업데이트
/// - 싱글톤 패턴으로 구현 (하나의 인스턴스만 존재)
/// </summary>
public class ChessGameManager : MonoBehaviour
{
    [Header("게임 설정")]
    [Tooltip("체스보드 참조 (ChessBoard 오브젝트를 드래그하세요)")]
    public ChessBoard chessBoard;
    
    [Tooltip("현재 흰색 차례인지 여부 (true=백, false=흑)")]
    public bool isWhiteTurn = true;
    
    [Header("UI 요소 (선택사항)")]
    [Tooltip("현재 턴을 표시할 UI Text (없어도 됨)")]
    public UnityEngine.UI.Text turnText;
    
    [Tooltip("게임 상태를 표시할 UI Text (없어도 됨)")]
    public UnityEngine.UI.Text statusText;
    
    // 현재 선택된 체스 말 (클릭해서 선택한 말)
    private ChessPiece selectedPiece;
    
    // 현재 하이라이트된 타일들의 목록 (가능한 이동 위치들)
    private List<ChessTile> highlightedTiles = new List<ChessTile>();
    
    /// <summary>
    /// 싱글톤 인스턴스 - 어디서든 ChessGameManager.Instance로 접근 가능
    /// 싱글톤 패턴: 프로그램 전체에서 오직 하나의 인스턴스만 존재
    /// </summary>
    public static ChessGameManager Instance { get; private set; }
    
        /// <summary>
    /// Unity 라이프사이클: Awake (Start보다 먼저 실행됨)
    /// 싱글톤 패턴 구현 - 오직 하나의 GameManager만 존재하도록 보장
    /// </summary>
    void Awake()
    {
        // 이미 인스턴스가 있다면
        if (Instance == null)
        {
            Instance = this; // 이 객체를 전역 인스턴스로 설정
        }
        else
        {
            Destroy(gameObject); // 중복된 매니저 제거
        }
    }
    
    /// <summary>
    /// Unity 라이프사이클: Start (게임 시작시 한 번 실행)
    /// 초기 설정 및 UI 업데이트
    /// </summary>
    void Start()
    {
        // ChessBoard가 연결되지 않았다면 자동으로 찾기
        if (chessBoard == null)
            chessBoard = FindObjectOfType<ChessBoard>();
            
        UpdateUI(); // 초기 UI 상태 설정
    }
    
    /// <summary>
    /// 체스 말이 클릭되었을 때 호출되는 메서드
    /// ChessPiece.OnMouseDown()에서 호출됨
    /// </summary>
    /// <param name="piece">클릭된 체스 말</param>
    public void OnPieceClicked(ChessPiece piece)
    {
        // 아군 유닛을 선택한 뒤 적 말을 클릭하면 레이가 타일이 아니라 말에 먼저 맞는다.
        // 잡기 가능 칸이면 해당 타일로 이동(포획)으로 처리한다.
        if (selectedPiece != null && piece.isWhite != isWhiteTurn)
        {
            ChessTile enemyTile = piece.currentTile;
            if (enemyTile != null)
            {
                List<ChessTile> possibleMoves = selectedPiece.GetPossibleMoves();
                if (possibleMoves.Contains(enemyTile))
                {
                    MovePiece(selectedPiece, enemyTile);
                    return;
                }
            }
            UpdateStatus("이동할 수 없는 칸입니다.");
            return;
        }

        // 현재 턴의 말인지 확인 (백 턴에 흑 말 클릭 방지)
        if (piece.isWhite != isWhiteTurn)
        {
            UpdateStatus($"{(isWhiteTurn ? "백" : "흑")}의 턴입니다!");
            return; // 잘못된 말 클릭시 아무 동작 안함
        }
        
        // 이미 선택된 말을 다시 클릭한 경우 = 선택 해제
        if (selectedPiece == piece)
        {
            DeselectPiece();
            return;
        }
        
        // 새로운 말 선택 처리
        SelectPiece(piece);
    }
    
    public void OnTileClicked(ChessTile tile)
    {
        if (selectedPiece == null)
            return;
            
        // 가능한 이동 중 하나인지 확인
        List<ChessTile> possibleMoves = selectedPiece.GetPossibleMoves();
        
        if (possibleMoves.Contains(tile))
        {
            MovePiece(selectedPiece, tile);
        }
        else
        {
            DeselectPiece();
        }
    }
    
    /// <summary>
    /// 체스 말을 선택했을 때의 처리
    /// - 이전 선택 해제
    /// - 가능한 이동 위치 하이라이트
    /// - UI 상태 업데이트
    /// </summary>
    /// <param name="piece">선택할 체스 말</param>
    void SelectPiece(ChessPiece piece)
    {
        // 이전에 선택된 말이 있다면 먼저 해제
        DeselectPiece();
        
        // 새로운 말 선택
        selectedPiece = piece;
        
        // 이 말이 이동할 수 있는 모든 위치 계산
        List<ChessTile> possibleMoves = piece.GetPossibleMoves();
        highlightedTiles = possibleMoves; // 나중에 해제하기 위해 저장
        
        // 가능한 이동 위치들을 초록색으로 표시
        chessBoard.HighlightPossibleMoves(possibleMoves);
        
        // 현재 선택된 말의 위치를 노란색으로 표시
        piece.currentTile.HighlightTile();
        
        // 사용자에게 상태 알림
        UpdateStatus($"{piece.pieceType} 선택됨. 이동할 위치를 클릭하세요.");
    }
    
    /// <summary>
    /// 현재 선택된 말의 선택을 해제
    /// - 모든 하이라이트 제거
    /// - 선택 상태 초기화
    /// </summary>
    void DeselectPiece()
    {
        // 선택된 말이 있다면
        if (selectedPiece != null)
        {
            selectedPiece = null; // 선택 해제
        }
        
        // 보드의 모든 타일 색상을 원래대로 복원
        chessBoard.ResetAllHighlights();
        
        // 하이라이트 목록 비우기
        highlightedTiles.Clear();
        
        // 사용자에게 새로운 상태 알림
        UpdateStatus("말을 선택하세요.");
    }
    
    void MovePiece(ChessPiece piece, ChessTile targetTile)
    {
        // 적의 말이 있다면 제거
        if (targetTile.IsOccupied())
        {
            ChessPiece capturedPiece = targetTile.currentPiece;
            chessBoard.RemovePiece(capturedPiece);
            UpdateStatus($"{capturedPiece.pieceType} 제거됨!");
        }
        
        // 말 이동
        piece.MoveTo(targetTile);
        
        // 선택 해제
        DeselectPiece();
        
        // 턴 변경
        isWhiteTurn = !isWhiteTurn;
        UpdateUI();
        
        // 게임 상태 확인 (체크, 체크메이트 등)
        CheckGameState();
    }
    
    void CheckGameState()
    {
        // 간단한 게임 상태 확인
        // 실제 체스에서는 체크, 체크메이트, 스테일메이트 등을 확인해야 함
        
        List<ChessPiece> currentPlayerPieces = chessBoard.GetPieces(isWhiteTurn);
        bool hasValidMoves = false;
        
        foreach (ChessPiece piece in currentPlayerPieces)
        {
            if (piece.GetPossibleMoves().Count > 0)
            {
                hasValidMoves = true;
                break;
            }
        }
        
        if (!hasValidMoves)
        {
            UpdateStatus($"{(isWhiteTurn ? "백" : "흑")}이 더 이상 움직일 수 없습니다!");
        }
    }
    
    void UpdateUI()
    {
        if (turnText != null)
        {
            turnText.text = $"현재 턴: {(isWhiteTurn ? "백" : "흑")}";
        }
        
        UpdateStatus($"{(isWhiteTurn ? "백" : "흑")}의 턴입니다. 말을 선택하세요.");
    }
    
    void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        
        Debug.Log($"[Chess] {message}");
    }
    
    public void ResetGame()
    {
        DeselectPiece();
        isWhiteTurn = true;
        
        // 보드 재생성
        if (chessBoard != null)
        {
            // 기존 말들 제거
            foreach (Transform child in chessBoard.transform)
            {
                if (child.GetComponent<ChessPiece>() != null)
                {
                    Destroy(child.gameObject);
                }
            }
            
            // 보드 재설정
            chessBoard.InitializeBoard();
        }
        
        UpdateUI();
    }
} 