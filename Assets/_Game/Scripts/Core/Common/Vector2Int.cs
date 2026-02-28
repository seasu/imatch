// Lightweight Vector2Int for use in Game.Core (noEngineReferences=true).
// When compiled with UnityEngine, the Unity type takes precedence via alias.
#if !UNITY_5_3_OR_NEWER
namespace Game.Core
{
    public struct Vector2Int : System.IEquatable<Vector2Int>
    {
        public int x, y;
        public Vector2Int(int x, int y) { this.x = x; this.y = y; }

        public bool Equals(Vector2Int other) => x == other.x && y == other.y;
        public override bool Equals(object obj) => obj is Vector2Int v && Equals(v);
        public override int GetHashCode() => x * 10007 + y;
        public override string ToString() => $"({x},{y})";

        public static bool operator ==(Vector2Int a, Vector2Int b) => a.Equals(b);
        public static bool operator !=(Vector2Int a, Vector2Int b) => !a.Equals(b);
    }
}
#else
// When running in Unity, alias UnityEngine.Vector2Int into our namespace.
namespace Game.Core
{
    using Vector2Int = UnityEngine.Vector2Int;
}
#endif
