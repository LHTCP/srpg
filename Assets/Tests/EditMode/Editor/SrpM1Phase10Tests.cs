using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 10차 테스트: 이동 규칙 설정(<see cref="SrpMoveRule"/>). 기본은 짧은 이동(AP 1, moveRange)/긴 이동(AP 2, moveRange × 2),
/// 활성화당 이동 1회, 긴 이동 뒤 활성화 종료다. 기존 방식(Legacy)은 이동력 풀을 나눠 쓰고 이동마다 AP 1이다.
/// 컨트롤러의 두 색 표시, 클릭한 칸에 따른 AP 소모, 되감기 복원은 Unity에서 확인한다(여기서는 같은 규칙 함수를 본다).
/// </summary>
[Category("SrpM1All")]
public class SrpM1Phase10Tests
{
    [TestCase(TestName = "이동 규칙 기본값은 짧은 이동/긴 이동, 활성화당 1회, 긴 이동 2배, 긴 이동 뒤 종료다")]
    public void MoveRule_DefaultsAreRecommendedValues()
    {
        var rule = SrpMoveRule.CreateDefault();
        Assert.That(rule.mode, Is.EqualTo(SrpMoveMode.ShortLong));
        Assert.That(rule.MovesPerActivation, Is.EqualTo(1));
        Assert.That(rule.LongRangeMultiplier, Is.EqualTo(2f));
        Assert.That(rule.endActivationAfterLongMove, Is.True);

        Assert.That(SrpGameSettings.MoveRule.mode, Is.EqualTo(SrpMoveMode.ShortLong), "로비 기본값이 추천안이 아닙니다.");
        var state = CreateOpenState(9);
        Assert.That(state.MoveRule.mode, Is.EqualTo(SrpMoveMode.ShortLong));
        Assert.That(state.Clone().MoveRule.mode, Is.EqualTo(SrpMoveMode.ShortLong));

        // 설정값은 범위 안으로 맞춘다.
        var odd = new SrpMoveRule { movesPerActivation = 5, longRangeMultiplier = 0.5f };
        Assert.That(odd.MovesPerActivation, Is.EqualTo(SrpMoveRule.MaxMovesPerActivation));
        Assert.That(odd.LongRangeMultiplier, Is.EqualTo(SrpMoveRule.MinLongRangeMultiplier));
        var unit = new SrpUnitRuntime { moveRange = 3 };
        Assert.That(new SrpMoveRule { longRangeMultiplier = 1.5f }.LongRange(unit), Is.EqualTo(4));
        Assert.That(new SrpMoveRule { longRangeMultiplier = 2f }.LongRange(unit), Is.EqualTo(6));
    }

    [TestCase(TestName = "짧은 이동/긴 이동: 도달 칸이 짧은 범위(moveRange)와 긴 범위(moveRange × 2) 두 가지로 나온다")]
    public void ShortLong_ReachableTilesSplitIntoShortAndLong()
    {
        var state = CreateOpenState(9);
        var mover = AddUnit(state, id: 1, owner: 0, x: 4, y: 4);
        SrpBattleFlow.StartRound(state);
        int remaining = SrpBattleFlow.BeginActivation(state, mover, null);

        var costs = SrpBattleFlow.GetMoveCosts(state, mover, remaining);
        int shortCount = 0;
        int longCount = 0;
        foreach (var kv in costs)
        {
            Assert.That(kv.Value, Is.LessThanOrEqualTo(6));
            bool isLong = SrpBattleFlow.IsLongMove(state, mover, kv.Value);
            Assert.That(isLong, Is.EqualTo(kv.Value > 3));
            Assert.That(SrpBattleFlow.GetMoveApCost(state, mover, kv.Value), Is.EqualTo(isLong ? 2 : 1));
            if (isLong) longCount++;
            else shortCount++;
        }
        // 열린 9×9 맵 가운데: 맨해튼 거리 1~3 칸 24개, 4~6 칸은 경계에서 잘린 나머지.
        Assert.That(shortCount, Is.EqualTo(24));
        Assert.That(longCount, Is.GreaterThan(0));
        Assert.That(costs.ContainsKey(new Vector2Int(1, 1)), Is.True, "거리 6 칸이 긴 이동 범위에 없습니다.");
        Assert.That(costs.ContainsKey(new Vector2Int(0, 0)), Is.False, "거리 8 칸이 범위에 들어 있습니다.");
        Assert.That(SrpBattleFlow.GetMoveReach(state, mover, remaining), Is.EqualTo(6));
    }

    [TestCase(TestName = "짧은 이동은 AP 1을 쓰고 활성화가 이어지며, 활성화당 1회라 다시 움직일 수 없다")]
    public void ShortLong_ShortMoveSpendsOneApAndUsesTheOnlyMove()
    {
        var state = CreateOpenState(9);
        var mover = AddUnit(state, id: 1, owner: 0, x: 4, y: 4);
        SrpBattleFlow.StartRound(state);
        int remaining = SrpBattleFlow.BeginActivation(state, mover, null);

        var result = SrpBattleFlow.Move(state, mover, 4, 1, 3, null);

        Assert.That(result.apCost, Is.EqualTo(1));
        Assert.That(result.longMove, Is.False);
        Assert.That(result.activationEnded, Is.False);
        Assert.That(mover.actionPoints, Is.EqualTo(1));
        Assert.That(mover.movesThisActivation, Is.EqualTo(1));
        Assert.That(SrpBattleFlow.RemainingMoveAfter(state, remaining, 3), Is.EqualTo(remaining), "짧은 이동/긴 이동은 이동력 풀을 쓰지 않습니다.");
        Assert.That(SrpBattleFlow.CanMoveNow(state, mover, remaining), Is.False, "이동을 두 번 할 수 있습니다.");
        Assert.That(SrpBattleFlow.GetMoveCosts(state, mover, remaining), Is.Empty);
        Assert.That(SrpBattleFlow.GetMovesLeft(state, mover), Is.EqualTo(0));
    }

    [TestCase(TestName = "긴 이동은 AP 2를 쓰고 기본 설정에서 활성화를 끝낸다")]
    public void ShortLong_LongMoveSpendsTwoApAndEndsActivation()
    {
        var state = CreateOpenState(9);
        var mover = AddUnit(state, id: 1, owner: 0, x: 4, y: 4);
        SrpBattleFlow.StartRound(state);
        SrpBattleFlow.BeginActivation(state, mover, null);

        var result = SrpBattleFlow.Move(state, mover, 4, 8, 4, null);

        Assert.That(result.apCost, Is.EqualTo(2));
        Assert.That(result.longMove, Is.True);
        Assert.That(result.activationEnded, Is.True);
        Assert.That(mover.actionPoints, Is.EqualTo(0));
        Assert.That(mover.anchorY, Is.EqualTo(8));
    }

    [TestCase(TestName = "긴 이동 뒤 종료 설정을 끄면 긴 이동이 활성화를 끝내지 않는다(AP는 2를 쓴다)")]
    public void ShortLong_LongMoveWithoutForcedEnd()
    {
        var state = CreateOpenState(9);
        state.MoveRule = new SrpMoveRule { endActivationAfterLongMove = false };
        var mover = AddUnit(state, id: 1, owner: 0, x: 4, y: 4);
        SrpBattleFlow.StartRound(state);
        SrpBattleFlow.BeginActivation(state, mover, null);

        var result = SrpBattleFlow.Move(state, mover, 4, 8, 4, null);

        Assert.That(result.longMove, Is.True);
        Assert.That(result.activationEnded, Is.False);
        Assert.That(mover.actionPoints, Is.EqualTo(0));
    }

    [TestCase(TestName = "AP가 1뿐이면(공격 뒤) 짧은 이동만 할 수 있고 긴 이동 칸은 나오지 않는다")]
    public void ShortLong_AfterAttackOnlyShortMoveIsOffered()
    {
        var state = CreateOpenState(9);
        var attacker = AddUnit(state, id: 1, owner: 0, x: 4, y: 4, attackPower: 6);
        var target = AddUnit(state, id: 2, owner: 1, x: 5, y: 4);
        SrpBattleFlow.StartRound(state);
        target.reactionPoints = 0;
        int remaining = SrpBattleFlow.BeginActivation(state, attacker, null);

        var attack = SrpBattleFlow.BasicAttack(state, attacker, target, null);
        Assert.That(attack.executed, Is.True);
        Assert.That(attacker.actionPoints, Is.EqualTo(1));

        Assert.That(SrpBattleFlow.CanMoveNow(state, attacker, remaining), Is.True, "공격 뒤 짧은 이동이 막혔습니다.");
        Assert.That(SrpBattleFlow.GetMoveReach(state, attacker, remaining), Is.EqualTo(3));
        foreach (var kv in SrpBattleFlow.GetMoveCosts(state, attacker, remaining))
            Assert.That(SrpBattleFlow.IsLongMove(state, attacker, kv.Value), Is.False, "AP 1인데 긴 이동 칸이 나왔습니다.");
    }

    [TestCase(TestName = "활성화당 이동 2회 설정이면 짧은 이동을 두 번 할 수 있고 세 번째는 막힌다")]
    public void ShortLong_TwoMovesPerActivation()
    {
        var state = CreateOpenState(9);
        state.MoveRule = new SrpMoveRule { movesPerActivation = 2 };
        var mover = AddUnit(state, id: 1, owner: 0, x: 0, y: 0);
        SrpBattleFlow.StartRound(state);
        int remaining = SrpBattleFlow.BeginActivation(state, mover, null);

        SrpBattleFlow.Move(state, mover, 3, 0, 3, null);
        Assert.That(SrpBattleFlow.CanMoveNow(state, mover, remaining), Is.True);
        Assert.That(SrpBattleFlow.GetMoveReach(state, mover, remaining), Is.EqualTo(3), "AP 1로 긴 이동을 할 수 있습니다.");
        var second = SrpBattleFlow.Move(state, mover, 6, 0, 3, null);

        Assert.That(second.apCost, Is.EqualTo(1));
        Assert.That(mover.actionPoints, Is.EqualTo(0));
        Assert.That(mover.movesThisActivation, Is.EqualTo(2));
        Assert.That(SrpBattleFlow.CanMoveNow(state, mover, remaining), Is.False);

        // 새 활성화에서는 횟수가 다시 찬다.
        SrpBattleFlow.StartRound(state);
        SrpBattleFlow.BeginActivation(state, mover, null);
        Assert.That(mover.movesThisActivation, Is.EqualTo(0));
        Assert.That(SrpBattleFlow.GetMovesLeft(state, mover), Is.EqualTo(2));
    }

    [TestCase(TestName = "AI 행동 실행도 도착 칸의 비용으로 짧은/긴 이동을 정하고 긴 이동 범위 밖은 거부한다")]
    public void ShortLong_ExecuteAiActionUsesTileRange()
    {
        var state = CreateOpenState(9);
        var actor = AddUnit(state, id: 1, owner: 1, x: 4, y: 4);
        SrpBattleFlow.StartRound(state);
        int remaining = SrpBattleFlow.BeginActivation(state, actor, null);

        var tooFar = SrpBattleFlow.ExecuteAiAction(state, actor, SrpEnemyAiAction.MoveTo(0, 0), ref remaining, null);
        Assert.That(tooFar.executed, Is.False);
        Assert.That(actor.actionPoints, Is.EqualTo(2));

        var longMove = SrpBattleFlow.ExecuteAiAction(state, actor, SrpEnemyAiAction.MoveTo(1, 1), ref remaining, null);
        Assert.That(longMove.executed, Is.True);
        Assert.That(longMove.move.longMove, Is.True);
        Assert.That(longMove.activationEnded, Is.True);
        Assert.That(actor.actionPoints, Is.EqualTo(0));
    }

    [TestCase(TestName = "되감기 스냅숏(상태 복제)은 이동 횟수와 AP를 함께 되돌려 다시 움직일 수 있다")]
    public void ShortLong_SnapshotRestoresMoveCount()
    {
        var state = CreateOpenState(9);
        var mover = AddUnit(state, id: 1, owner: 0, x: 4, y: 4);
        SrpBattleFlow.StartRound(state);
        int remaining = SrpBattleFlow.BeginActivation(state, mover, null);

        // 컨트롤러는 행동 직전 상태를 복제해 두고(PushUndo), 되감기 때 그 복제본으로 바꾼다.
        var snapshot = state.Clone();
        SrpBattleFlow.Move(state, mover, 4, 1, 3, null);
        Assert.That(SrpBattleFlow.CanMoveNow(state, mover, remaining), Is.False);

        var restored = snapshot;
        var restoredMover = restored.FindUnitById(mover.id);
        Assert.That(restoredMover.movesThisActivation, Is.EqualTo(0));
        Assert.That(restoredMover.actionPoints, Is.EqualTo(2));
        Assert.That(SrpBattleFlow.CanMoveNow(restored, restoredMover, remaining), Is.True);
        Assert.That(restored.MoveRule.mode, Is.EqualTo(SrpMoveMode.ShortLong));
    }

    [TestCase(TestName = "기존 방식(Legacy)은 이동력 풀을 나눠 쓰고 이동마다 AP 1이다(9차까지와 같다)")]
    public void Legacy_SplitMovesKeepOldBehaviour()
    {
        var state = CreateOpenState(9);
        state.MoveRule = SrpMoveRule.CreateLegacy();
        var mover = AddUnit(state, id: 1, owner: 1, x: 0, y: 0);
        SrpBattleFlow.StartRound(state);
        int remaining = SrpBattleFlow.BeginActivation(state, mover, null);
        Assert.That(remaining, Is.EqualTo(3));

        var costs = SrpBattleFlow.GetMoveCosts(state, mover, remaining);
        var expected = SrpPathfinder.GetReachableWithCosts(state, mover, remaining);
        Assert.That(costs, Is.EquivalentTo(expected), "기존 방식의 도달 칸이 예전 계산과 다릅니다.");
        foreach (var kv in costs)
            Assert.That(SrpBattleFlow.IsLongMove(state, mover, kv.Value), Is.False);

        var first = SrpBattleFlow.ExecuteAiAction(state, mover, SrpEnemyAiAction.MoveTo(1, 0), ref remaining, null);
        Assert.That(first.executed, Is.True);
        Assert.That(first.move.apCost, Is.EqualTo(1));
        Assert.That(first.activationEnded, Is.False);
        Assert.That(remaining, Is.EqualTo(2));
        Assert.That(mover.actionPoints, Is.EqualTo(1));

        // 남은 이동력 2로 한 번 더 나눠 움직인다(이동 횟수 설정은 쓰지 않는다).
        Assert.That(SrpBattleFlow.CanMoveNow(state, mover, remaining), Is.True);
        Assert.That(SrpBattleFlow.GetMovesLeft(state, mover), Is.EqualTo(-1));
        var tooFar = SrpBattleFlow.ExecuteAiAction(state, mover, SrpEnemyAiAction.MoveTo(4, 0), ref remaining, null);
        Assert.That(tooFar.executed, Is.False, "남은 이동력보다 먼 칸으로 갔습니다.");
        var second = SrpBattleFlow.ExecuteAiAction(state, mover, SrpEnemyAiAction.MoveTo(3, 0), ref remaining, null);
        Assert.That(second.executed, Is.True);
        Assert.That(remaining, Is.EqualTo(0));
        Assert.That(mover.actionPoints, Is.EqualTo(0));
        Assert.That(SrpBattleFlow.GetMaxMoveDistance(state, mover), Is.EqualTo(3));
    }

    [TestCase(TestName = "AI: 짧은 이동으로 공격할 수 있으면 짧은 이동 뒤 공격한다")]
    public void Ai_ShortMoveThenAttackWhenTargetIsClose()
    {
        var state = CreateOpenState(9);
        var actor = AddUnit(state, id: 1, owner: 1, x: 0, y: 4, attackPower: 6);
        var target = AddUnit(state, id: 2, owner: 0, x: 3, y: 4);
        SrpBattleFlow.StartRound(state);
        target.reactionPoints = 0;
        int remaining = SrpBattleFlow.BeginActivation(state, actor, null);

        var move = SrpEnemyAi.Decide(state, actor, remaining, hasAttacked: false);
        Assert.That(move.type, Is.EqualTo(SrpEnemyAiActionType.Move));
        var step = SrpBattleFlow.ExecuteAiAction(state, actor, move, ref remaining, null);
        Assert.That(step.executed, Is.True);
        Assert.That(step.move.longMove, Is.False, "공격할 수 있는데 긴 이동을 골랐습니다.");
        Assert.That(actor.actionPoints, Is.EqualTo(1));

        var attack = SrpEnemyAi.Decide(state, actor, remaining, hasAttacked: false);
        Assert.That(attack.type, Is.EqualTo(SrpEnemyAiActionType.Attack));
        Assert.That(attack.targetUnitId, Is.EqualTo(target.id));
    }

    [TestCase(TestName = "AI: 이번에 공격할 수 없으면 긴 이동으로 접근한다(기존 방식은 이동력 안에서만)")]
    public void Ai_LongMoveToApproachWhenTargetIsFar()
    {
        var state = CreateOpenState(9);
        var actor = AddUnit(state, id: 1, owner: 1, x: 0, y: 4, attackPower: 6);
        AddUnit(state, id: 2, owner: 0, x: 8, y: 4);
        SrpBattleFlow.StartRound(state);
        int remaining = SrpBattleFlow.BeginActivation(state, actor, null);

        var decision = SrpEnemyAi.Decide(state, actor, remaining, hasAttacked: false);
        Assert.That(decision.type, Is.EqualTo(SrpEnemyAiActionType.Move));
        var costs = SrpBattleFlow.GetMoveCosts(state, actor, remaining);
        Assert.That(costs.TryGetValue(new Vector2Int(decision.x, decision.y), out int cost), Is.True);
        Assert.That(SrpBattleFlow.IsLongMove(state, actor, cost), Is.True, $"긴 이동으로 접근하지 않았습니다. 고른 칸 ({decision.x},{decision.y}) 비용 {cost}");

        // 같은 배치를 기존 방식으로 판단하면 이동력(3) 안의 칸을 고른다.
        var legacy = CreateOpenState(9);
        legacy.MoveRule = SrpMoveRule.CreateLegacy();
        var legacyActor = AddUnit(legacy, id: 1, owner: 1, x: 0, y: 4, attackPower: 6);
        AddUnit(legacy, id: 2, owner: 0, x: 8, y: 4);
        SrpBattleFlow.StartRound(legacy);
        int legacyRemaining = SrpBattleFlow.BeginActivation(legacy, legacyActor, null);
        var legacyDecision = SrpEnemyAi.Decide(legacy, legacyActor, legacyRemaining, hasAttacked: false);
        Assert.That(legacyDecision.type, Is.EqualTo(SrpEnemyAiActionType.Move));
        int legacyDistance = Mathf.Abs(legacyDecision.x - 0) + Mathf.Abs(legacyDecision.y - 4);
        Assert.That(legacyDistance, Is.LessThanOrEqualTo(3));
    }

    [TestCase(TestName = "시뮬레이션의 이동 후보도 이동 규칙을 따른다(무작위 정책이 긴 이동 칸을 고를 수 있다)")]
    public void Simulation_MoveOptionsFollowMoveRule()
    {
        var state = CreateOpenState(9);
        var actor = AddUnit(state, id: 1, owner: 0, x: 0, y: 4);
        AddUnit(state, id: 2, owner: 1, x: 8, y: 0);
        SrpBattleFlow.StartRound(state);
        int remaining = SrpBattleFlow.BeginActivation(state, actor, null);
        var costs = SrpBattleFlow.GetMoveCosts(state, actor, remaining);
        bool anyLong = false;
        foreach (var kv in costs)
            anyLong |= SrpBattleFlow.IsLongMove(state, actor, kv.Value);
        Assert.That(anyLong, Is.True);

        // 시뮬레이션 설정에 기존 방식을 넣으면 그 규칙으로 돈다.
        var config = new SrpBattleSimConfig
        {
            mapPreset = SrpMapPreset.M1EngagementLab,
            trials = 2,
            maxRounds = 4,
            baseSeed = 101,
            sampleSeedCount = 1,
            owner0Policy = new SrpRandomAiPolicy(),
            owner1Policy = new SrpHeuristicAiPolicy(),
            thresholds = SrpSimThresholdConfig.Default(),
            moveRule = SrpMoveRule.CreateLegacy(),
        };
        var report = SrpBattleSimRunner.RunBatch(config, writeReport: false).report;
        Assert.That(report, Is.Not.Null);
    }

    static SrpBattleState CreateOpenState(int size)
    {
        var walk = new bool[size * size];
        for (int i = 0; i < walk.Length; i++)
            walk[i] = true;

        return SrpBattleState.FromMap(new SrpMapFileV1
        {
            width = size,
            height = size,
            walkable = walk,
            playerOrder = new[] { 0, 1 },
        });
    }

    static SrpUnitRuntime AddUnit(SrpBattleState state, int id, int owner, int x, int y, int attackPower = 6)
    {
        var unit = new SrpUnitRuntime
        {
            id = id,
            displayName = "U" + id,
            owner = owner,
            anchorX = x,
            anchorY = y,
            hp = 30,
            maxHp = 30,
            pg = 20,
            maxPg = 20,
            actionPoints = 2,
            maxActionPoints = 2,
            reactionPoints = 1,
            maxReactionPoints = 1,
            speed = 10,
            moveRange = 3,
            attackPower = attackPower,
            attackRange = 1,
            weaponClass = SrpWeaponClass.Melee,
        };
        state.Units.Add(unit);
        state.RebuildEngagements();
        return unit;
    }
}
