using System.Collections.Generic;
using UnityEngine;

public partial class SrpGameController
{
    enum SrpActionPreviewKind
    {
        None,
        BasicAttack,
        Overwatch,
        Cover,
        Skill,
        Interaction,
    }

    SrpActionPreviewKind _actionPreviewKind;
    SrpSkillData _hoverPreviewSkillData;
    SrpSkillRuntime _hoverPreviewSkillRuntime;

    void OnBasicAttackUi()
    {
        ShowActionPreview(SrpActionPreviewKind.BasicAttack);
    }

    void ShowActionPreview(
        SrpActionPreviewKind kind,
        SrpSkillData skillData = null,
        SrpSkillRuntime skillRuntime = null)
    {
        if (_gameOver || _phase != Phase.UnitActive || !_selectedId.HasValue)
            return;

        var unit = GetUnit(_selectedId.Value);
        if (unit == null)
            return;

        _actionPreviewKind = kind;
        _hoverPreviewSkillData = skillData;
        _hoverPreviewSkillRuntime = skillRuntime;
        ClearActionPreviewOverlays();
        RefreshAttackTargets(unit);

        switch (kind)
        {
            case SrpActionPreviewKind.BasicAttack:
                if (!_hasAttackedThisTurn)
                {
                    HighlightAttackTiles();
                    HighlightParryTelegraphForAttackTargets(unit);
                }
                _hoverStatusHint = $"일반 공격 preview: 현재 위치 기준 대상 {_attackIds.Count}명";
                break;
            case SrpActionPreviewKind.Overwatch:
                HighlightOverwatchTiles(unit);
                _hoverStatusHint = "경계태세 preview: 현재 위치 기준 경계사격 범위";
                break;
            case SrpActionPreviewKind.Cover:
                HighlightCoverTiles(unit);
                _hoverStatusHint = "엄폐 preview: 현재 위치 기준 엄폐 가능 지점";
                break;
            case SrpActionPreviewKind.Skill:
                HighlightSkillPreviewTiles(unit, skillData);
                _hoverStatusHint = skillData != null
                    ? $"스킬 preview: {skillData.displayName}"
                    : "스킬 preview";
                break;
            case SrpActionPreviewKind.Interaction:
                HighlightInteractionTiles(unit);
                _hoverStatusHint = "상호작용 preview: 현재 위치 기준 목표";
                break;
        }

        UpdateHud();
    }

    void ClearActionPreview()
    {
        if (_phase == Phase.SelectingSkillTarget)
            return;
        _actionPreviewKind = SrpActionPreviewKind.None;
        _hoverPreviewSkillData = null;
        _hoverPreviewSkillRuntime = null;
        ClearActionPreviewOverlays();
        if (_hoverTileX < 0 && _hoverTileY < 0)
            _hoverStatusHint = string.Empty;
        UpdateHud();
    }

    void ClearActionPreviewOverlays()
    {
        ClearOverlayLayer(OverlayAttack);
        ClearOverlayLayer(OverlayOverwatch);
        ClearOverlayLayer(OverlayCover);
        ClearOverlayLayer(OverlayInteraction);
        ClearOverlayLayer(OverlaySkill);
        ClearOverlayLayer(OverlayParryTelegraph);
        ClearOverlayLayer(OverlayAimLine);
    }

    void HighlightSkillPreviewTiles(SrpUnitRuntime unit, SrpSkillData skillData)
    {
        ClearOverlayLayer(OverlaySkill);
        ClearOverlayLayer(OverlayParryTelegraph);
        if (unit == null || skillData == null)
            return;

        var tiles = SrpSkills.GetSkillTargetTiles(skillData, unit, _state);
        foreach (var tile in tiles)
            SetOverlayTile(OverlaySkill, tile.x, tile.y, new Color(0.7f, 0.3f, 0.9f));
        HighlightParryTelegraphForPreviewSkill(unit, skillData, tiles);
    }

    void HighlightParryTelegraphForPreviewSkill(
        SrpUnitRuntime caster,
        SrpSkillData skill,
        IEnumerable<Vector2Int> tiles)
    {
        if (skill == null || !skill.requiresParryTelegraph || tiles == null)
            return;

        foreach (var tile in tiles)
        {
            var defender = _state.GetOccupant(tile.x, tile.y);
            if (!SrpCombatResolver.CanDefenderParry(_state, caster, defender, skill))
                continue;
            SetParryTelegraphForUnit(defender);
        }
    }

    void OnTurnOrderTokenHoverEnter(int unitId)
    {
        if (_gameOver)
            return;
        var unit = GetUnit(unitId);
        if (unit == null)
            return;

        _hoverUnitId = unitId;
        _hoverTileX = -1;
        _hoverTileY = -1;
        _hoverStatusHint = $"행동 순서 preview: {unit.displayName}({unit.id})";
        RenderUnitHoverOverlays(unit);
        UpdateUnitFeedbackVisuals();
        UpdateHud();
    }

    void OnTurnOrderTokenHoverExit(int unitId)
    {
        if (_hoverUnitId != unitId)
            return;
        _hoverUnitId = -1;
        ClearOverlayLayer(OverlayUnitHoverRange);
        ClearOverlayLayer(OverlayUnitHoverZoc);
        _hoverStatusHint = string.Empty;
        UpdateUnitFeedbackVisuals();
        UpdateHud();
    }
}
