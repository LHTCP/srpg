// Unity 없이 순수 전투 로직을 컴파일하기 위한 UnityEngine 최소 스텁.
// 전투 로직과 EditMode 테스트가 실제로 쓰는 멤버만 둔다. Unity의 동작을 완전히 재현하지 않는다.
// - JsonUtility는 Newtonsoft.Json으로 대신한다. 필드 직렬화 규칙이 Unity와 완전히 같지는 않다.
// - 컨트롤러(MonoBehaviour)는 컴파일만 통과시키는 빈 껍데기다. 이를 쓰는 테스트는
//   [Category("RequiresUnity")]로 표시해 실행에서 뺀다.
// 새 로직이 여기에 없는 UnityEngine 멤버를 쓰면 컴파일이 깨진다. 그때 필요한 멤버만 더한다.
using System;
using System.IO;

namespace UnityEngine
{
    public class SerializeField : Attribute {}
    public class TooltipAttribute : Attribute { public TooltipAttribute(string s) {} }
    public class HeaderAttribute : Attribute { public HeaderAttribute(string s) {} }

    public static class Mathf
    {
        public const float Epsilon = 1.401298E-45f;
        public const float Rad2Deg = 57.29578f;
        public static int Abs(int v) => Math.Abs(v);
        public static float Abs(float v) => Math.Abs(v);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static int Max(params int[] v) { int m = v[0]; foreach (var x in v) if (x > m) m = x; return m; }
        public static int Min(int a, int b) => Math.Min(a, b);
        public static float Min(float a, float b) => Math.Min(a, b);
        public static int Clamp(int v, int lo, int hi) => v < lo ? lo : (v > hi ? hi : v);
        public static float Clamp(float v, float lo, float hi) => v < lo ? lo : (v > hi ? hi : v);
        public static float Clamp01(float v) => Clamp(v, 0f, 1f);
        public static int FloorToInt(float f) => (int)Math.Floor(f);
        public static int CeilToInt(float f) => (int)Math.Ceiling(f);
        // Unity와 같이 .5는 짝수로 반올림한다(Math.Round 기본 동작).
        public static int RoundToInt(float f) => (int)Math.Round(f);
        public static float Atan2(float y, float x) => (float)Math.Atan2(y, x);
        public static float Sqrt(float f) => (float)Math.Sqrt(f);
    }

    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 up => new Vector2(0, 1);
        public static Vector2 down => new Vector2(0, -1);
        public static Vector2 left => new Vector2(-1, 0);
        public static Vector2 right => new Vector2(1, 0);
        public static Vector2 zero => new Vector2(0, 0);
        public float sqrMagnitude => x * x + y * y;
        public float magnitude => (float)Math.Sqrt(sqrMagnitude);
        public Vector2 normalized { get { float m = magnitude; return m > 1E-05f ? new Vector2(x / m, y / m) : zero; } }
        public static float Dot(Vector2 a, Vector2 b) => a.x * b.x + a.y * b.y;
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 forward => new Vector3(0, 0, 1);
        public static Vector3 back => new Vector3(0, 0, -1);
        public static Vector3 left => new Vector3(-1, 0, 0);
        public static Vector3 right => new Vector3(1, 0, 0);
        public static Vector3 zero => new Vector3(0, 0, 0);
        public static float Distance(Vector3 a, Vector3 b) { float dx = a.x - b.x, dy = a.y - b.y, dz = a.z - b.z; return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz); }
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator *(Vector3 a, float f) => new Vector3(a.x * f, a.y * f, a.z * f);
    }

    public struct Vector2Int : IEquatable<Vector2Int>
    {
        public int x, y;
        public Vector2Int(int x, int y) { this.x = x; this.y = y; }
        public static Vector2Int zero => new Vector2Int(0, 0);
        public bool Equals(Vector2Int o) => x == o.x && y == o.y;
        public override bool Equals(object o) => o is Vector2Int v && Equals(v);
        public override int GetHashCode() => x * 73856093 ^ y * 19349663;
        public static bool operator ==(Vector2Int a, Vector2Int b) => a.Equals(b);
        public static bool operator !=(Vector2Int a, Vector2Int b) => !a.Equals(b);
        public static Vector2Int operator +(Vector2Int a, Vector2Int b) => new Vector2Int(a.x + b.x, a.y + b.y);
        public override string ToString() => $"({x}, {y})";
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1f) { this.r = r; this.g = g; this.b = b; this.a = a; }
    }

    public static class Debug
    {
        public static void Log(object o) => Console.WriteLine(o);
        public static void LogWarning(object o) => Console.WriteLine(o);
        public static void LogError(object o) => Console.WriteLine(o);
    }

    public static class Application
    {
        static string _dir;

        // 실행마다 새 임시 폴더를 쓴다. 개발자 PC의 실제 저장 데이터(skills.json 등)를 읽거나 덮어쓰지 않는다.
        public static string persistentDataPath
        {
            get
            {
                if (_dir == null)
                {
                    _dir = Path.Combine(Path.GetTempPath(), "srpg-logic-tests-" + Guid.NewGuid().ToString("N"));
                    Directory.CreateDirectory(_dir);
                }
                return _dir;
            }
        }

        public static string dataPath => persistentDataPath;
    }

    public static class JsonUtility
    {
        public static string ToJson(object o) => ToJson(o, false);
        public static string ToJson(object o, bool pretty) => Newtonsoft.Json.JsonConvert.SerializeObject(
            o, pretty ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None);
        public static T FromJson<T>(string json) => Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
    }
}

namespace UnityEngine.SceneManagement
{
    public static class SceneManager { public static void LoadScene(string s) {} }
}

// 컨트롤러 껍데기. 컴파일만 통과시키며, 이를 쓰는 테스트는 RequiresUnity 카테고리로 실행에서 뺀다.
public struct LogicTestsQuaternion
{
    public static UnityEngine.Vector3 operator *(LogicTestsQuaternion q, UnityEngine.Vector3 v) => UnityEngine.Vector3.zero;
}

public static class SrpGameController
{
    public static LogicTestsQuaternion GetFacingRotation(SrpFacing f) => default;
}

public static class SrpUnitMakerController
{
    public static void SyncV2LegacyStats(SrpUnitTemplateData u) {}
}
