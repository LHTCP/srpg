using UnityEngine;

/// <summary>한 활성화에서 이동하는 방식.</summary>
public enum SrpMoveMode
{
    /// <summary>
    /// 기존 방식(9차까지). 활성화마다 이동력 풀(moveRange) 하나를 받고, 이동할 때마다 AP 1을 쓴다.
    /// 남은 이동력 안에서 여러 번 나눠 움직일 수 있다. 비교와 회귀 확인용으로 남긴다.
    /// </summary>
    Legacy = 0,

    /// <summary>
    /// 짧은 이동/긴 이동(10차 기본값). 짧은 이동은 AP 1로 moveRange까지, 긴 이동은 AP 2로 moveRange × 배수까지 간다.
    /// 어느 쪽인지는 도착 칸의 이동 비용으로 정해진다. 활성화당 이동 횟수에 제한이 있다.
    /// </summary>
    ShortLong = 1,
}

/// <summary>
/// 이동 규칙 설정(순수 데이터). 기획이 정해지지 않아 설정값으로 두고 추천안을 기본값으로 한다(10차).
/// 전투 상태(<see cref="SrpBattleState.MoveRule"/>)가 들고 있어 런타임과 AI 시뮬레이션이 같은 규칙을 쓴다.
/// 규칙 처리는 <see cref="SrpBattleFlow"/>가 한다.
/// </summary>
[System.Serializable]
public sealed class SrpMoveRule
{
    public const int ShortMoveApCost = 1;
    public const int LongMoveApCost = 2;
    public const int MinMovesPerActivation = 1;
    public const int MaxMovesPerActivation = 2;
    public const float MinLongRangeMultiplier = 1f;
    public const float MaxLongRangeMultiplier = 4f;

    /// <summary>이동 방식. 기본은 짧은 이동/긴 이동.</summary>
    public SrpMoveMode mode = SrpMoveMode.ShortLong;

    /// <summary>
    /// (ShortLong) 활성화당 이동 횟수, 1~2. 기본 1(추천안): 짧은 이동을 두 번 하면 한 칸씩 보고 반응하는 정찰이 공짜가 되어
    /// 이동 거리 차이가 정보 균형 장치 역할을 못 하기 때문이다.
    /// </summary>
    public int movesPerActivation = 1;

    /// <summary>(ShortLong) 긴 이동 거리 = moveRange × 이 값(내림). 기본 2.</summary>
    public float longRangeMultiplier = 2f;

    /// <summary>(ShortLong) 긴 이동 뒤 활성화를 끝낼지. 기본 예. 아니오면 AP를 쓰지 않는 행동(방향 등)은 이어서 할 수 있다.</summary>
    public bool endActivationAfterLongMove = true;

    public static SrpMoveRule CreateDefault()
    {
        return new SrpMoveRule();
    }

    public static SrpMoveRule CreateLegacy()
    {
        return new SrpMoveRule { mode = SrpMoveMode.Legacy };
    }

    public bool IsLegacy => mode == SrpMoveMode.Legacy;

    public int MovesPerActivation => Mathf.Clamp(movesPerActivation, MinMovesPerActivation, MaxMovesPerActivation);

    public float LongRangeMultiplier => Mathf.Clamp(longRangeMultiplier, MinLongRangeMultiplier, MaxLongRangeMultiplier);

    /// <summary>짧은 이동으로 갈 수 있는 최대 이동 비용.</summary>
    public int ShortRange(SrpUnitRuntime unit)
    {
        return unit == null ? 0 : Mathf.Max(0, unit.moveRange);
    }

    /// <summary>긴 이동으로 갈 수 있는 최대 이동 비용. 짧은 이동보다 짧지 않다.</summary>
    public int LongRange(SrpUnitRuntime unit)
    {
        int shortRange = ShortRange(unit);
        // 1.5배 같은 값의 부동소수 오차로 한 칸이 빠지지 않게 조금 더해 내림한다.
        return Mathf.Max(shortRange, Mathf.FloorToInt(shortRange * LongRangeMultiplier + 0.001f));
    }

    public SrpMoveRule Clone()
    {
        return (SrpMoveRule)MemberwiseClone();
    }

    /// <summary>로비 버튼과 로그에 쓰는 짧은 이름.</summary>
    public static string DescribeMode(SrpMoveMode mode)
    {
        return mode == SrpMoveMode.Legacy ? "기존(이동력 나눠 쓰기)" : "짧게/길게";
    }
}
