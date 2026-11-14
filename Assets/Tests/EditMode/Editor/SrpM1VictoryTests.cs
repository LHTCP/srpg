using NUnit.Framework;

/// <summary>
/// 승리 조건(SrpVictory)과 상호작용 승리 목표 연결. 기준서 §12, §13, 백로그 "특수 지형 상호작용/승리 조건 연동".
/// </summary>
[Category("SrpM1All")]
public class SrpM1VictoryTests
{
    [TestCase(TestName = "기존 프리셋은 전멸 규칙만 쓰고 지휘관이나 목표 지점으로는 승패가 나지 않는다")]
    public void ExistingPresets_KeepAnnihilationOnly()
    {
        foreach (var preset in new[] { SrpMapPreset.M1OpeningPrototype, SrpMapPreset.M1QaIntegrated, SrpMapPreset.M1EngagementLab })
        {
            var state = SrpBattleState.FromMap(SrpDefaultMaps.GetPreset(preset));
            Assert.That(state.VictoryRule.commanderKill, Is.False, $"{preset} 지휘관 처치 규칙이 켜져 있습니다.");
            Assert.That(state.VictoryRule.interactionObjective, Is.False, $"{preset} 목표 지점 규칙이 켜져 있습니다.");
            Assert.That(SrpVictory.DescribeRule(state), Is.EqualTo("적 전멸"));
            foreach (var unit in state.Units)
                Assert.That(unit.isCommander, Is.False, $"{preset} {unit.displayName}이 지휘관으로 표시됐습니다.");
            foreach (var point in state.InteractionPoints)
                Assert.That(point.effect, Is.EqualTo(SrpInteractionEffect.None), $"{preset} {point.id}에 효과가 붙었습니다.");
        }

        var opening = SrpBattleState.FromMap(SrpDefaultMaps.CreateM1OpeningPrototype());
        var officer = FindUnit(opening, "opening_officer");
        opening.RemoveUnit(officer);
        foreach (var point in opening.InteractionPoints)
        {
            point.activated = true;
            point.owner = 0;
        }
        Assert.That(SrpVictory.Evaluate(opening).decided, Is.False, "첫 전투 맵이 전멸 전에 끝났습니다.");
    }

    [TestCase(TestName = "한 진영만 남으면 그 진영이 전멸 승리하고 아무도 없으면 무승부다")]
    public void Annihilation_DecidesWinnerOrDraw()
    {
        var state = SrpBattleState.FromMap(SrpDefaultMaps.CreateM1EngagementLab());
        Assert.That(SrpVictory.Evaluate(state).decided, Is.False);

        foreach (var unit in state.Units)
        {
            if (unit.owner == 1)
                state.RemoveUnit(unit);
        }
        var result = SrpVictory.Evaluate(state);
        Assert.That(result.decided, Is.True);
        Assert.That(result.winnerOwner, Is.EqualTo(0));
        Assert.That(result.reason, Is.EqualTo(SrpVictoryReason.Annihilation));

        foreach (var unit in state.Units)
            state.RemoveUnit(unit);
        result = SrpVictory.Evaluate(state);
        Assert.That(result.decided, Is.True);
        Assert.That(result.winnerOwner, Is.EqualTo(-1));
        Assert.That(result.reason, Is.EqualTo(SrpVictoryReason.Draw));
    }

    [TestCase(TestName = "지휘관 처치 규칙을 켠 맵에서는 지휘관을 모두 잃은 진영이 패배한다")]
    public void CommanderKill_DefeatsOwnerWhenRuleEnabled()
    {
        var state = SrpBattleState.FromMap(SrpDefaultMaps.CreateM1ObjectiveLab());
        Assert.That(SrpVictory.DescribeRule(state), Is.EqualTo("적 전멸 / 지휘관 처치 / 목표 지점 확보"));
        Assert.That(SrpVictory.Evaluate(state).decided, Is.False);

        // 호위병이 죽어도 지휘관이 살아 있으면 계속된다.
        var guard = FindUnit(state, "objective_enemy_guard");
        state.RemoveUnit(guard);
        Assert.That(SrpVictory.Evaluate(state).decided, Is.False);

        var enemyOfficer = FindUnit(state, "objective_enemy_officer");
        Assert.That(enemyOfficer.isCommander, Is.True);
        state.RemoveUnit(enemyOfficer);

        var result = SrpVictory.Evaluate(state);
        Assert.That(result.decided, Is.True);
        Assert.That(result.winnerOwner, Is.EqualTo(0));
        Assert.That(result.reason, Is.EqualTo(SrpVictoryReason.CommanderKilled));
    }

    [TestCase(TestName = "지휘관 처치 규칙이 꺼져 있거나 지휘관이 없던 진영은 지휘관 규칙으로 패배하지 않는다")]
    public void CommanderKill_IgnoredWhenRuleOffOrNoCommander()
    {
        var map = SrpDefaultMaps.CreateM1ObjectiveLab();
        map.victory.commanderKill = false;
        var ruleOff = SrpBattleState.FromMap(map);
        ruleOff.RemoveUnit(FindUnit(ruleOff, "objective_enemy_officer"));
        Assert.That(SrpVictory.Evaluate(ruleOff).decided, Is.False, "규칙이 꺼진 맵에서 지휘관 사망으로 끝났습니다.");

        var noCommanderMap = SrpDefaultMaps.CreateM1ObjectiveLab();
        foreach (var placement in noCommanderMap.placements)
        {
            if (placement.owner == 1)
                placement.isCommander = false;
        }
        var noCommander = SrpBattleState.FromMap(noCommanderMap);
        noCommander.RemoveUnit(FindUnit(noCommander, "objective_enemy_officer"));
        Assert.That(SrpVictory.Evaluate(noCommander).decided, Is.False, "지휘관이 없던 진영이 지휘관 규칙으로 패배했습니다.");
    }

    [TestCase(TestName = "양 진영 지휘관이 모두 쓰러지면 무승부다")]
    public void CommanderKill_BothLost_IsDraw()
    {
        var state = SrpBattleState.FromMap(SrpDefaultMaps.CreateM1ObjectiveLab());
        state.RemoveUnit(FindUnit(state, "objective_enemy_officer"));
        state.RemoveUnit(FindUnit(state, "objective_leader"));

        var result = SrpVictory.Evaluate(state);
        Assert.That(result.decided, Is.True);
        Assert.That(result.winnerOwner, Is.EqualTo(-1));
        Assert.That(result.reason, Is.EqualTo(SrpVictoryReason.Draw));
    }

    [TestCase(TestName = "승리 목표 상호작용 포인트를 활성화하면 AP 1을 쓰고 그 진영이 승리한다")]
    public void InteractionObjective_ActivationWinsBattle()
    {
        var state = SrpBattleState.FromMap(SrpDefaultMaps.CreateM1ObjectiveLab());
        var point = state.InteractionPoints[0];
        Assert.That(point.effect, Is.EqualTo(SrpInteractionEffect.VictoryObjective));

        // 적 진영은 owner 0 전용 장치를 쓸 수 없다.
        var enemy = FindUnit(state, "objective_enemy_guard");
        enemy.anchorX = point.x - 1;
        enemy.anchorY = point.y;
        Assert.That(state.TryResolveInteractionAction(enemy, out _), Is.False);
        Assert.That(SrpVictory.Evaluate(state).decided, Is.False);
        enemy.anchorX = 6;
        enemy.anchorY = 2;

        var leader = FindUnit(state, "objective_leader");
        leader.anchorX = point.x;
        leader.anchorY = point.y - 1;
        leader.actionPoints = 2;
        Assert.That(state.TryResolveInteractionAction(leader, out var activated), Is.True);
        Assert.That(activated.id, Is.EqualTo(point.id));
        Assert.That(leader.actionPoints, Is.EqualTo(1));

        var result = SrpVictory.Evaluate(state);
        Assert.That(result.decided, Is.True);
        Assert.That(result.winnerOwner, Is.EqualTo(0));
        Assert.That(result.reason, Is.EqualTo(SrpVictoryReason.ObjectiveSecured));
    }

    [TestCase(TestName = "승리 목표가 여럿이면 전부 확보해야 하고 규칙이 꺼진 맵에서는 확보해도 끝나지 않는다")]
    public void InteractionObjective_RequiresAllPointsAndEnabledRule()
    {
        var map = SrpDefaultMaps.CreateM1ObjectiveLab();
        map.interactionPoints = new[]
        {
            map.interactionPoints[0],
            new SrpInteractionPointData
            {
                id = "objective_second",
                x = 2,
                y = 4,
                requiredOwner = 0,
                effect = SrpInteractionEffect.VictoryObjective,
            },
            // 효과 없는 포인트는 승리 목표 수에 들어가지 않는다.
            new SrpInteractionPointData { id = "plain_console", x = 0, y = 4, requiredOwner = 0 },
        };
        var state = SrpBattleState.FromMap(map);
        state.InteractionPoints[0].activated = true;
        state.InteractionPoints[0].owner = 0;
        Assert.That(SrpVictory.Evaluate(state).decided, Is.False, "목표 하나만으로 끝났습니다.");

        state.InteractionPoints[1].activated = true;
        state.InteractionPoints[1].owner = 0;
        Assert.That(SrpVictory.Evaluate(state).reason, Is.EqualTo(SrpVictoryReason.ObjectiveSecured));

        map.victory.interactionObjective = false;
        var ruleOff = SrpBattleState.FromMap(map);
        foreach (var point in ruleOff.InteractionPoints)
        {
            point.activated = true;
            point.owner = 0;
        }
        Assert.That(SrpVictory.Evaluate(ruleOff).decided, Is.False, "규칙이 꺼진 맵에서 목표 확보로 끝났습니다.");
    }

    [TestCase(TestName = "되감기용 상태 복제는 승리 조건과 지휘관 표시와 목표 효과를 보존한다")]
    public void Clone_PreservesVictoryData()
    {
        var state = SrpBattleState.FromMap(SrpDefaultMaps.CreateM1ObjectiveLab());
        var clone = state.Clone();

        Assert.That(clone.VictoryRule.commanderKill, Is.True);
        Assert.That(clone.VictoryRule.interactionObjective, Is.True);
        Assert.That(clone.VictoryRule, Is.Not.SameAs(state.VictoryRule));
        Assert.That(FindUnit(clone, "objective_enemy_officer").isCommander, Is.True);
        Assert.That(clone.InteractionPoints[0].effect, Is.EqualTo(SrpInteractionEffect.VictoryObjective));

        // 복제본에서 목표를 확보해도 원본은 미결이다.
        clone.InteractionPoints[0].activated = true;
        clone.InteractionPoints[0].owner = 0;
        Assert.That(SrpVictory.Evaluate(clone).decided, Is.True);
        Assert.That(SrpVictory.Evaluate(state).decided, Is.False);
    }

    [TestCase(TestName = "승리 조건 값이 없는 맵 데이터는 전멸 규칙으로 읽힌다")]
    public void MissingVictoryData_FallsBackToAnnihilation()
    {
        var map = SrpDefaultMaps.CreateM1ObjectiveLab();
        map.victory = null;
        var state = SrpBattleState.FromMap(map);

        Assert.That(state.VictoryRule, Is.Not.Null);
        Assert.That(state.VictoryRule.commanderKill, Is.False);
        state.RemoveUnit(FindUnit(state, "objective_enemy_officer"));
        Assert.That(SrpVictory.Evaluate(state).decided, Is.False);
    }

    static SrpUnitRuntime FindUnit(SrpBattleState state, string templateId)
    {
        foreach (var unit in state.Units)
        {
            if (!unit.eliminated && unit.templateId == templateId)
                return unit;
        }
        Assert.Fail($"{templateId} 유닛을 찾지 못했습니다.");
        return null;
    }
}
