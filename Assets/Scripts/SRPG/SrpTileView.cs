using UnityEngine;

public class SrpTileView : MonoBehaviour
{
    public int x;
    public int y;
    public SrpGameController game;

    void OnMouseDown()
    {
        game?.OnTileClicked(x, y);
    }
}
