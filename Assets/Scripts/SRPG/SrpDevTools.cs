using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 에디터·개발 빌드에서만 맵 저장/로드·맵 리로드 (프로덕션 릴리스에서는 비활성).
/// </summary>
public class SrpDevTools : MonoBehaviour
{
    public SrpGameController game;
    public string fileName = "my_map";

    bool _panel;
    string _status = "";

    static bool AllowDev =>
        Application.isEditor || Debug.isDebugBuild;

    void Update()
    {
        if (!AllowDev || game == null)
            return;
        if (Input.GetKeyDown(KeyCode.F3))
            _panel = !_panel;
    }

    void OnGUI()
    {
        if (!AllowDev || game == null || !_panel)
            return;

        const float panelW = 260f;
        const float panelH = 168f;
        GUILayout.BeginArea(new Rect(Screen.width - panelW - 10f, 10f, panelW, panelH));
        GUILayout.Label("SRPG Dev (F3 닫기)");
        fileName = GUILayout.TextField(fileName, GUILayout.ExpandWidth(true));
        if (GUILayout.Button("현재 맵을 JSON 저장"))
        {
            try
            {
                string path = SrpMapIO.Save(game.initialMap, fileName);
                _status = "저장: " + path;
            }
            catch (System.Exception e)
            {
                _status = e.Message;
            }
        }
        if (GUILayout.Button("JSON 불러와 적용"))
        {
            if (SrpMapIO.TryLoad(fileName, out var m))
            {
                game.ApplyMap(m);
                _status = "로드 완료";
            }
            else
                _status = "파일 없음: " + fileName;
        }
        if (GUILayout.Button("씬 재시작"))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        GUILayout.Label(_status);
        GUILayout.EndArea();
    }
}
