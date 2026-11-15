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
