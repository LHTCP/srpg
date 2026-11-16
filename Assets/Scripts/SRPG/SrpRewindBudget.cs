using System;

/// <summary>
/// 전투 중 되감기(undo) 횟수 한도(순수 로직). 한 번의 되감기는 플레이어 행동 하나를 되돌린다.
/// 사용 횟수는 전투 상태 스냅숏 밖(컨트롤러)에 두므로, 되감기로 상태를 복원해도 사용 횟수는 돌아가지 않는다.
/// 한도 값: 0 = 되감기 끔, N(양수) = 한 판에 N회, <see cref="Unlimited"/>(음수) = 무제한.
/// 핫시트(적 AI 끔)에서는 두 진영이 이 횟수 하나를 같이 쓴다(진영별로 나눌지는 기획 미정).
/// </summary>
public sealed class SrpRewindBudget
{
    /// <summary>무제한을 뜻하는 한도 값. 음수는 모두 무제한으로 본다.</summary>
    public const int Unlimited = -1;

    /// <summary>로비 버튼이 순환하는 한도 값(끔 → 3 → 5 → 10 → 무제한).</summary>
    public static readonly int[] LobbyCycle = { 0, 3, 5, 10, Unlimited };

    public int Limit { get; private set; }
    public int Used { get; private set; }

    public bool IsUnlimited => Limit < 0;

    /// <summary>남은 횟수. 무제한이면 <see cref="int.MaxValue"/>.</summary>
    public int Remaining => IsUnlimited ? int.MaxValue : Math.Max(0, Limit - Used);

    public bool CanRewind => IsUnlimited || Used < Limit;

    public SrpRewindBudget() : this(Unlimited) { }

    public SrpRewindBudget(int limit)
    {
        Reset(limit);
    }

    /// <summary>판 시작 때 부른다. 한도를 정하고 사용 횟수를 0으로 채운다.</summary>
    public void Reset(int limit)
    {
        Limit = Normalize(limit);
        Used = 0;
    }

    /// <summary>되감기 1회를 쓴다. 남은 횟수가 없으면 아무것도 바꾸지 않고 false.</summary>
    public bool TryConsume()
    {
        if (!CanRewind)
            return false;
        if (!IsUnlimited)
            Used++;
        return true;
    }

    public static int Normalize(int limit)
    {
        return limit < 0 ? Unlimited : limit;
    }

    /// <summary>로비 순환에서 다음 한도 값. 목록에 없는 값이면 처음(끔)으로 간다.</summary>
    public static int NextLobbyLimit(int current)
    {
        int normalized = Normalize(current);
        int index = Array.IndexOf(LobbyCycle, normalized);
        return LobbyCycle[(index + 1) % LobbyCycle.Length];
    }

    /// <summary>한도 값을 사람이 읽는 말로: "끔", "3회", "무제한".</summary>
    public static string DescribeLimit(int limit)
    {
        int normalized = Normalize(limit);
        if (normalized < 0)
            return "무제한";
        if (normalized == 0)
            return "끔";
        return $"{normalized}회";
    }
}
