using NUnit.Framework;

/// <summary>
/// 9차 테스트: 전투 중 되감기 횟수 한도(<see cref="SrpRewindBudget"/>).
/// 컨트롤러가 되감기 버튼을 막고 사용 횟수를 스냅숏 밖에 두는 부분은 Unity에서 확인한다.
/// </summary>
[Category("SrpM1All")]
public class SrpM1Phase9Tests
{
    [TestCase(TestName = "되감기 한도 기본값은 무제한이라 기존 플레이가 바뀌지 않는다")]
    public void Rewind_DefaultIsUnlimited()
    {
        Assert.That(SrpGameSettings.RewindLimit, Is.EqualTo(SrpRewindBudget.Unlimited));

        var budget = new SrpRewindBudget();
        Assert.That(budget.IsUnlimited, Is.True);
        for (int i = 0; i < 1000; i++)
            Assert.That(budget.TryConsume(), Is.True);
        Assert.That(budget.CanRewind, Is.True);
        Assert.That(budget.Used, Is.EqualTo(0));
        Assert.That(budget.Remaining, Is.EqualTo(int.MaxValue));
    }

    [TestCase(TestName = "되감기 한도 0이면 되감기를 할 수 없다")]
    public void Rewind_ZeroLimitDisablesRewind()
    {
        var budget = new SrpRewindBudget(0);
        Assert.That(budget.IsUnlimited, Is.False);
        Assert.That(budget.CanRewind, Is.False);
        Assert.That(budget.Remaining, Is.EqualTo(0));
        Assert.That(budget.TryConsume(), Is.False);
        Assert.That(budget.Used, Is.EqualTo(0));
    }

    [TestCase(TestName = "되감기 한도 N이면 N회 쓰고 나서 막힌다")]
    public void Rewind_LimitedBudgetRunsOut()
    {
        var budget = new SrpRewindBudget(3);
        Assert.That(budget.Remaining, Is.EqualTo(3));
        Assert.That(budget.TryConsume(), Is.True);
        Assert.That(budget.TryConsume(), Is.True);
        Assert.That(budget.Remaining, Is.EqualTo(1));
        Assert.That(budget.TryConsume(), Is.True);
        Assert.That(budget.Remaining, Is.EqualTo(0));
        Assert.That(budget.CanRewind, Is.False);
        Assert.That(budget.TryConsume(), Is.False);
        Assert.That(budget.Used, Is.EqualTo(3));
    }

    [TestCase(TestName = "판 시작 초기화는 사용 횟수를 0으로 채우고 새 한도를 적용한다")]
    public void Rewind_ResetRefillsForNewBattle()
    {
        var budget = new SrpRewindBudget(2);
        budget.TryConsume();
        budget.TryConsume();
        Assert.That(budget.CanRewind, Is.False);

        budget.Reset(2);
        Assert.That(budget.Used, Is.EqualTo(0));
        Assert.That(budget.Remaining, Is.EqualTo(2));

        budget.Reset(-7);
        Assert.That(budget.Limit, Is.EqualTo(SrpRewindBudget.Unlimited));
        Assert.That(budget.IsUnlimited, Is.True);
    }

    [TestCase(TestName = "핫시트 되감기는 기본이 공유이고 진영별 모드에서는 진영마다 따로 센다")]
    public void Rewind_SharedByDefaultAndPerOwnerWhenEnabled()
    {
        Assert.That(SrpGameSettings.RewindPerSideInHotseat, Is.False);

        var shared = new SrpRewindBudget(2);
        Assert.That(shared.TryConsumeFor(0), Is.True);
        Assert.That(shared.TryConsumeFor(1), Is.True);
        Assert.That(shared.CanRewindFor(0), Is.False);
        Assert.That(shared.CanRewindFor(1), Is.False);
        Assert.That(shared.CanRewind, Is.False);

        var perOwner = new SrpRewindBudget(2, perOwner: true);
        Assert.That(perOwner.TryConsumeFor(0), Is.True);
        Assert.That(perOwner.TryConsumeFor(0), Is.True);
        Assert.That(perOwner.CanRewindFor(0), Is.False);
        Assert.That(perOwner.TryConsumeFor(0), Is.False);
        Assert.That(perOwner.RemainingFor(1), Is.EqualTo(2));
        Assert.That(perOwner.TryConsumeFor(1), Is.True);
        Assert.That(perOwner.UsedFor(0), Is.EqualTo(2));
        Assert.That(perOwner.UsedFor(1), Is.EqualTo(1));

        perOwner.Reset(2, perOwner: true);
        Assert.That(perOwner.RemainingFor(0), Is.EqualTo(2));
        Assert.That(perOwner.RemainingFor(1), Is.EqualTo(2));
    }

    [TestCase(TestName = "진영별 되감기는 상대 진영의 활성화가 시작되면 그 전 기록을 버린다")]
    public void Rewind_PerOwnerCutsHistoryWhenOpponentActivationStarts()
    {
        // P0 행동 → P1이 행동 없이 활성화 종료 → P0 차례: P1 활성화 시작 시점에 기록을 버려야
        // P0이 P1의 활성화를 건너뛰어 되감지 못한다.
        var perOwner = new SrpRewindBudget(3, perOwner: true);
        Assert.That(perOwner.CutsHistoryAt(lastActionOwner: 0, nextActivationOwner: 1), Is.True);
        Assert.That(perOwner.CutsHistoryAt(lastActionOwner: 0, nextActivationOwner: 0), Is.False);

        // 공유 모드(기본)는 기존대로 기록을 유지한다.
        var shared = new SrpRewindBudget(3);
        Assert.That(shared.CutsHistoryAt(lastActionOwner: 0, nextActivationOwner: 1), Is.False);
    }

    [TestCase(TestName = "로비 되감기 버튼은 끔, 3, 5, 10, 무제한 순으로 돈다")]
    public void Rewind_LobbyCycleOrder()
    {
        int value = 0;
        var seen = new System.Collections.Generic.List<int>();
        for (int i = 0; i < 5; i++)
        {
            value = SrpRewindBudget.NextLobbyLimit(value);
            seen.Add(value);
        }
        Assert.That(seen, Is.EqualTo(new[] { 3, 5, 10, SrpRewindBudget.Unlimited, 0 }));

        // 목록에 없는 값(코드에서 직접 넣은 7)은 처음(끔)으로 돌아간다.
        Assert.That(SrpRewindBudget.NextLobbyLimit(7), Is.EqualTo(0));
        Assert.That(SrpRewindBudget.DescribeLimit(SrpRewindBudget.Unlimited), Is.EqualTo("무제한"));
        Assert.That(SrpRewindBudget.DescribeLimit(0), Is.EqualTo("끔"));
        Assert.That(SrpRewindBudget.DescribeLimit(5), Is.EqualTo("5회"));
    }
}
