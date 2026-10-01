using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 伤害判定范围可视化（运行时调试工具，游戏视图与 Scene 视图都可见）：
/// 伤害判定触发处调用 <see cref="Sphere"/> / <see cref="Capsule"/> 传入判定范围，
/// 工具把形状**连续渲染 Duration 秒并逐渐淡出**（GL 线框，非 Editor 专用，打包后也可用）。
/// 只展示**伤害判定范围**：索敌范围、无范围直伤（DoT/反伤/直接 OnDamaged）不接入本工具。
/// 当前接入点：SkillBase.StrikeSphere（球形，近战/AoE）、BattleManagerCombat.TryHitBullet（胶囊形，子弹扫掠）。
/// 形状沿轴旋转提示：胶囊 = 两个端面圆环 + 4 条轴向连线；球 = 三条正交大圆。
/// </summary>
public static class DamageRangeDebug
{
    public static bool Enabled = true;

    /// <summary>单次判定的渲染持续时长（秒），期间透明度从 1 线性淡到 0。</summary>
    public static float Duration = 1f;

    /// <summary>球/胶囊圆环分段数（越大越圆越费）。</summary>
    public static int Segments = 24;

    public static Color SphereColor = new Color(1f, 0.45f, 0.15f);  // 球形判定：橙
    public static Color CapsuleColor = new Color(0.20f, 0.90f, 1f); // 胶囊判定：青

    /// <summary>同时存活的最大形状数（超出丢弃最旧的，防高频判定刷爆列表）。</summary>
    private const int MaxShapes = 512;

    internal struct Shape
    {
        public bool capsule;  // false = 球
        public Vector3 p0;
        public Vector3 p1;
        public float radius;
        public float birth;
    }

    internal static readonly List<Shape> shapes = new();

    private static DamageRangeDebugHost host;

    /// <summary>记录一次球形伤害判定范围。</summary>
    public static void Sphere(Vector3 center, float radius) => Record(false, center, center, radius);

    /// <summary>记录一次胶囊形伤害判定范围（p0→p1 扫掠，radius 为胶囊半径）。</summary>
    public static void Capsule(Vector3 p0, Vector3 p1, float radius) => Record(true, p0, p1, radius);

    private static void Record(bool capsule, Vector3 p0, Vector3 p1, float radius)
    {
        if (!Enabled || radius <= 0f) return;
        EnsureHost();
        if (shapes.Count >= MaxShapes) shapes.RemoveAt(0); // 丢最旧的
        shapes.Add(new Shape { capsule = capsule, p0 = p0, p1 = p1, radius = radius, birth = Time.time });
    }

    private static void EnsureHost()
    {
        if (host != null) return;
        var go = new GameObject("DamageRangeDebugHost") { hideFlags = HideFlags.HideAndDontSave };
        host = go.AddComponent<DamageRangeDebugHost>();
    }
}

/// <summary>
/// 渲染宿主：隐藏物体（HideAndDontSave，不进场景存档、不污染 Hierarchy）。
/// 每帧清掉过期形状；OnRenderObject 里用 GL.LINES 画所有存活形状（相机视图矩阵下直接发世界坐标顶点）。
/// </summary>
public class DamageRangeDebugHost : MonoBehaviour
{
    private Material lineMaterial;

    private void Update()
    {
        float now = Time.time;
        float duration = DamageRangeDebug.Duration;
        // 只从头部清理：列表按 birth 升序追加，遇到未过期即可停
        while (DamageRangeDebug.shapes.Count > 0 && now - DamageRangeDebug.shapes[0].birth >= duration)
        {
            DamageRangeDebug.shapes.RemoveAt(0);
        }
    }

    private void OnRenderObject()
    {
        var shapes = DamageRangeDebug.shapes;
        if (shapes.Count == 0) return;
        var cam = Camera.current;
        if (cam == null) return;

        if (lineMaterial == null)
        {
            lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"))
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
        }

        lineMaterial.SetPass(0);
        GL.PushMatrix();
        // 显式装载当前相机矩阵，顶点直接给世界坐标
        GL.MultMatrix(cam.worldToCameraMatrix);
        GL.LoadProjectionMatrix(cam.projectionMatrix);
        GL.Begin(GL.LINES);

        float now = Time.time;
        float duration = DamageRangeDebug.Duration;
        int segments = Mathf.Max(6, DamageRangeDebug.Segments);

        for (int i = 0; i < shapes.Count; i++)
        {
            var s = shapes[i];
            float alpha = Mathf.Clamp01(1f - (now - s.birth) / duration);
            Color color = s.capsule ? DamageRangeDebug.CapsuleColor : DamageRangeDebug.SphereColor;
            color.a *= alpha;

            if (s.capsule) DrawCapsuleWire(s.p0, s.p1, s.radius, color, segments);
            else DrawSphereWire(s.p0, s.radius, color, segments);
        }

        GL.End();
        GL.PopMatrix();
    }

    /// <summary>球线框：三条正交大圆（XZ / XY / YZ 平面）。</summary>
    private static void DrawSphereWire(Vector3 center, float radius, Color color, int segments)
    {
        GL.Color(color);
        for (int axis = 0; axis < 3; axis++)
        {
            Vector3 prev = center + CirclePoint(axis, 0f, radius);
            for (int i = 1; i <= segments; i++)
            {
                Vector3 cur = center + CirclePoint(axis, i * Mathf.PI * 2f / segments, radius);
                GL.Vertex(prev);
                GL.Vertex(cur);
                prev = cur;
            }
        }
    }

    /// <summary>胶囊线框：两端各一个垂直于轴的圆环 + 4 条轴向连线（0°/90°/180°/270° 对齐）。</summary>
    private static void DrawCapsuleWire(Vector3 p0, Vector3 p1, float radius, Color color, int segments)
    {
        Vector3 axis = p1 - p0;
        float len = axis.magnitude;
        if (len < 0.0001f)
        {
            DrawSphereWire(p0, radius, color, segments); // 退化为球
            return;
        }
        axis /= len;
        Vector3 u = Mathf.Abs(axis.y) < 0.99f ? Vector3.Cross(axis, Vector3.up).normalized : Vector3.right;
        Vector3 v = Vector3.Cross(axis, u).normalized;

        GL.Color(color);
        // 两个端面圆环
        for (int end = 0; end < 2; end++)
        {
            Vector3 center = end == 0 ? p0 : p1;
            Vector3 prev = center + (u * radius);
            for (int i = 1; i <= segments; i++)
            {
                float ang = i * Mathf.PI * 2f / segments;
                Vector3 cur = center + (u * Mathf.Cos(ang) + v * Mathf.Sin(ang)) * radius;
                GL.Vertex(prev);
                GL.Vertex(cur);
                prev = cur;
            }
        }
        // 4 条轴向连线（对应圆环上 0°/90°/180°/270°）
        for (int k = 0; k < 4; k++)
        {
            float ang = k * Mathf.PI / 2f;
            Vector3 offset = (u * Mathf.Cos(ang) + v * Mathf.Sin(ang)) * radius;
            GL.Vertex(p0 + offset);
            GL.Vertex(p1 + offset);
        }
    }

    /// <summary>axis：0 = XZ 平面圆，1 = XY 平面圆，2 = YZ 平面圆。</summary>
    private static Vector3 CirclePoint(int axis, float angle, float radius)
    {
        float cos = Mathf.Cos(angle) * radius;
        float sin = Mathf.Sin(angle) * radius;
        return axis switch
        {
            0 => new Vector3(cos, 0f, sin),
            1 => new Vector3(cos, sin, 0f),
            _ => new Vector3(0f, cos, sin),
        };
    }
}
