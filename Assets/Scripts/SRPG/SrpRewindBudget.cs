using System;
using System.Collections.Generic;

/// <summary>
/// 전투 중 되감기(undo) 횟수 한도(순수 로직). 한 번의 되감기는 플레이어 행동 하나를 되돌린다.
/// 사용 횟수는 전투 상태 스냅숏 밖(컨트롤러)에 두므로, 되감기로 상태를 복원해도 사용 횟수는 돌아가지 않는다.
/// 한도 값: 0 = 되감기 끔, N(양수) = 한 판에 N회, <see cref="Unlimited"/>(음수) = 무제한.
/// 진영별 모드(<see cref="PerOwner"/>)에서는 진영마다 한도를 따로 센다. 끄면(기본) 모든 진영이 횟수 하나를 같이 쓴다.
/// </summary>
public sealed class SrpRewindBudget
{
    /// <summary>무제한을 뜻하는 한도 값. 음수는 모두 무제한으로 본다.</summary>
    public const int Unlimited = -1;

    /// <summary>공유 모드에서 사용 횟수를 모아 세는 키.</summary>
    const int SharedKey = int.MinValue;

    /// <summary>로비 버튼이 순환하는 한도 값(끔 → 3 → 5 → 10 → 무제한).</summary>
    public static readonly int[] LobbyCycle = { 0, 3, 5, 10, Unlimited };

    readonly Dictionary<int, int> _usedByKey = new Dictionary<int, int>();

    public int Limit { get; private set; }

    /// <summary>true면 진영(owner)마다 한도를 따로 센다.</summary>
    public bool PerOwner { get; private set; }

    public bool IsUnlimited => Limit < 0;

    /// <summary>공유 모드의 사용 횟수(진영별 모드에서는 모든 진영의 합).</summary>
    public int Used
    {
        get
        {
            int total = 0;
            foreach (var kv in _usedByKey)
                total += kv.Value;
            return total;
        }
    }

    /// <summary>공유 모드의 남은 횟수. 무제한이면 <see cref="int.MaxValue"/>.</summary>
    public int Remaining => RemainingFor(SharedKey);

    public bool CanRewind => CanRewindFor(SharedKey);

    public SrpRewindBudget() : this(Unlimited) { }

    public SrpRewindBudget(int limit, bool perOwner = false)
    {
        Reset(limit, perOwner);
    }

    /// <summary>판 시작 때 부른다. 한도와 모드를 정하고 사용 횟수를 0으로 채운다.</summary>
    public void Reset(int limit, bool perOwner = false)
    {
        Limit = Normalize(limit);
        PerOwner = perOwner;
        _usedByKey.Clear();
    }

    public int UsedFor(int owner)
    {
        return _usedByKey.TryGetValue(KeyFor(owner), out int used) ? used : 0;
    }

    /// <summary>그 진영의 남은 횟수(공유 모드에서는 진영과 무관하게 같은 값). 무제한이면 <see cref="int.MaxValue"/>.</summary>
    public int RemainingFor(int owner)
    {
        return IsUnlimited ? int.MaxValue : Math.Max(0, Limit - UsedFor(owner));
    }

    public bool CanRewindFor(int owner)
    {
        return IsUnlimited || UsedFor(owner) < Limit;
    }

    /// <summary>공유 모드에서 되감기 1회를 쓴다. 남은 횟수가 없으면 아무것도 바꾸지 않고 false.</summary>
    public bool TryConsume()
    {
        return TryConsumeFor(SharedKey);
    }

    /// <summary>그 진영의 되감기 1회를 쓴다(공유 모드에서는 공유 횟수). 남은 횟수가 없으면 아무것도 바꾸지 않고 false.</summary>
    public bool TryConsumeFor(int owner)
    {
        if (!CanRewindFor(owner))
            return false;
        if (!IsUnlimited)
        {
            int key = KeyFor(owner);
            _usedByKey[key] = UsedFor(owner) + 1;
        }
        return true;
    }

    int KeyFor(int owner)
    {
        return PerOwner ? owner : SharedKey;
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
