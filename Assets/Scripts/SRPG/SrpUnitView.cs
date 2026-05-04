using UnityEngine;

public class SrpUnitView : MonoBehaviour
{
    public int unitId;
    public SrpGameController game;

    void OnMouseEnter()
    {
        game?.OnUnitHoverEnter(unitId);
    }

    void OnMouseExit()
    {
        game?.OnUnitHoverExit(unitId);
    }

    void OnMouseDown()
    {
        game?.OnUnitClicked(unitId);
    }
}
