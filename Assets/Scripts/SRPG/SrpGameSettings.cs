using UnityEngine.SceneManagement;

/// <summary>
/// 씬 전환 시 로비 → 전투 설정을 전달하는 정적 저장소.
/// Unity static 필드는 씬 로드 사이에도 유지된다.
/// </summary>
public static class SrpGameSettings
{
    /// <summary>전투 씬에서 사용할 내장 프리셋.</summary>
    public static SrpMapPreset SelectedPreset = SrpMapPreset.M1OpeningPrototype;
    public static bool HasSelectedPreset;

    /// <summary>로비에서 JSON으로 불러온 맵. null이면 SelectedPreset 사용.</summary>
    public static SrpMapFileV1 CustomMap = null;

    /// <summary>
    /// true면 전투에서 플레이어(owner 0)가 아닌 진영을 적 AI가 자동으로 조작한다.
    /// false면 양 진영을 직접 조작한다(핫시트). 로비의 "적 AI" 버튼으로 바꾼다.
    /// </summary>
    public static bool EnemyAiEnabled = true;

    /// <summary>
    /// 한 판에 쓸 수 있는 되감기 횟수. 0 = 끔, N = N회, <see cref="SrpRewindBudget.Unlimited"/>(음수) = 무제한.
    /// 기본값은 무제한(기존 동작). 로비의 "되감기" 버튼으로 바꾼다. 맵마다 다르게 주지 않는 전역 값이다.
    /// </summary>
    public static int RewindLimit = SrpRewindBudget.Unlimited;

    /// <summary>
    /// 핫시트(적 AI 끔)에서 되감기 횟수를 진영별로 따로 셀지. false(기본, 추천안)면 두 진영이 횟수 하나를 같이 쓰고
    /// 누구의 행동이든 되감을 수 있다(기존 동작). true면 진영마다 <see cref="RewindLimit"/>회씩 받고 자기 진영 행동만 되감는다.
    /// 적 AI가 켜져 있으면 쓰지 않는다. 기획 미정이라 설정으로 둔다. 로비의 "핫시트" 버튼으로 바꾼다.
    /// </summary>
    public static bool RewindPerSideInHotseat = false;

    /// <summary>
    /// 전투 난수(회피 판정) 시드. null이면 전투를 시작할 때마다 시각 기반 시드를 쓴다.
    /// 테스트처럼 결과를 재현해야 할 때만 값을 넣는다.
    /// </summary>
    public static int? BattleRandomSeed = null;

    public const string LobbyScene      = "SrpgLobby";
    public const string BattleScene     = "SrpgBattle";
    public const string SkillMakerScene = "SrpgSkillMaker";
    public const string UnitMakerScene  = "SrpgUnitMaker";
    public const string MapMakerScene   = "SrpgMapMaker";

    /// <summary>내장 프리셋으로 전투 씬 전환.</summary>
    public static void StartBattle(SrpMapPreset preset)
    {
        SelectedPreset = preset;
        HasSelectedPreset = true;
        CustomMap      = null;
        SceneManager.LoadScene(BattleScene);
    }

    /// <summary>로드한 JSON 맵으로 전투 씬 전환.</summary>
    public static void StartBattleWithMap(SrpMapFileV1 map)
    {
        CustomMap = map;
        HasSelectedPreset = false;
        SceneManager.LoadScene(BattleScene);
    }

    /// <summary>로비 씬으로 돌아간다.</summary>
    public static void ReturnToLobby()
    {
        CustomMap = null;
        HasSelectedPreset = false;
        SelectedPreset = SrpMapPreset.M1OpeningPrototype;
        SceneManager.LoadScene(LobbyScene);
    }
}
