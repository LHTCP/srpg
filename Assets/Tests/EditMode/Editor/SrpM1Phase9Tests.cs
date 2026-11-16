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
