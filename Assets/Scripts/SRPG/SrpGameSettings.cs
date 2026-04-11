using UnityEngine.SceneManagement;

/// <summary>
/// 씬 전환 시 로비 → 전투 설정을 전달하는 정적 저장소.
/// Unity static 필드는 씬 로드 사이에도 유지된다.
/// </summary>
public static class SrpGameSettings
{
    /// <summary>전투 씬에서 사용할 내장 프리셋.</summary>
    public static SrpMapPreset SelectedPreset = SrpMapPreset.Skirmish;

    /// <summary>로비에서 JSON으로 불러온 맵. null이면 SelectedPreset 사용.</summary>
    public static SrpMapFileV1 CustomMap = null;

    public const string LobbyScene  = "SrpgLobby";
    public const string BattleScene = "SrpgBattle";

    /// <summary>내장 프리셋으로 전투 씬 전환.</summary>
    public static void StartBattle(SrpMapPreset preset)
    {
        SelectedPreset = preset;
        CustomMap      = null;
        SceneManager.LoadScene(BattleScene);
    }

    /// <summary>로드한 JSON 맵으로 전투 씬 전환.</summary>
    public static void StartBattleWithMap(SrpMapFileV1 map)
    {
        CustomMap = map;
        SceneManager.LoadScene(BattleScene);
    }

    /// <summary>로비 씬으로 돌아간다.</summary>
    public static void ReturnToLobby()
    {
        CustomMap = null;
        SceneManager.LoadScene(LobbyScene);
    }
}
