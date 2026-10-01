using System.Collections.Generic;
using UnityEngine;

/// <summary>伤害判定范围可视化（仅 Editor）：挂到场景任意物体（如相机）。判定处调用 Sphere/Capsule，形状渲染 duration 秒并线性淡出。</summary>
#if UNITY_EDITOR
public class DamageRangeDebugHost : MonoBehaviour
{
    public static DamageRangeDebugHost Instance { get; private set; }

    [Tooltip("是否显示")]
    public bool show = true;

    [Tooltip("单次判定的渲染时长（秒），期间线性淡出")]
    public float duration = 1f;

    [Tooltip("圆环分段数")]
    [Min(6)] public int segments = 24;

    public Color sphereColor = new Color(1f, 0.45f, 0.15f);
    public Color capsuleColor = new Color(0.2f, 0.9f, 1f);

    [Tooltip("常驻测试球（原点 r=2，不淡出）。用于验证渲染链路：勾选后 Scene 视图对准世界原点应能看到球")]
    public bool drawTestShape = false;

    private struct Shape
    {
        public bool capsule;
        public Vector3 p0, p1;
        public float radius;
        public float birth;
    }

    private static readonly List<Shape> shapes = new();
    private const int MaxShapes = 512;
    private static bool warnedNoInstance;
    private Material lineMaterial;

    private void OnEnable()
    {
        Instance = this;
        warnedNoInstance = false;
        heartbeatLeft = 3;
        Debug.Log($"[DamageRangeDebugHost] 已挂载（场景={gameObject.scene.name}, 物体={name}），等待伤害判定数据");
    }
    private void OnDisable() { if (Instance == this) Instance = null; }

    private int heartbeatLeft;

    private void Update()
    {
        while (shapes.Count > 0 && Time.time - shapes[0].birth >= duration) shapes.RemoveAt(0);

        // 心跳：前 15 秒每 5 秒报一次存活与缓存形状数，确认组件确实在服务器进程中运行
        if (heartbeatLeft > 0 && Time.frameCount % 300 == 0)
        {
            heartbeatLeft--;
            Debug.Log($"[DamageRangeDebugHost] 运行中 shapes={shapes.Count}");
        }
    }
    public static void Sphere(Vector3 center, float radius) => Record(false, center, center, radius);
    public static void Capsule(Vector3 p0, Vector3 p1, float radius) => Record(true, p0, p1, radius);

    private static void Record(bool capsule, Vector3 p0, Vector3 p1, float radius)
    {
        var inst = Instance;
        if (inst == null)
        {
            if (!warnedNoInstance)
            {
                warnedNoInstance = true;
                Debug.LogWarning("[DamageRangeDebugHost] 有伤害判定调用被丢弃：本场景未挂载 DamageRangeDebugHost");
            }
            return;
        }
        if (!inst.show || radius <= 0f) return;
        if (shapes.Count >= MaxShapes) shapes.RemoveAt(0);
        shapes.Add(new Shape { capsule = capsule, p0 = p0, p1 = p1, radius = radius, birth = Time.time });
    }

    private void OnRenderObject()
    {
        bool hasTest = drawTestShape;
        if (shapes.Count == 0 && !hasTest) return;
        var cam = Camera.current;
        if (cam == null) return;
        if (lineMaterial == null)
            lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored")) { hideFlags = HideFlags.HideAndDontSave };

        lineMaterial.SetPass(0);
        GL.PushMatrix();
        GL.MultMatrix(cam.worldToCameraMatrix);
        GL.LoadProjectionMatrix(cam.projectionMatrix);
        GL.Begin(GL.LINES);
        if (hasTest)
        {
            var c = Color.green;
            c.a = 1f;
            DrawSphereWire(Vector3.zero, 2f, c, 24);
        }
        int seg = Mathf.Max(6, segments);
        for (int i = 0; i < shapes.Count; i++)
        {
            var s = shapes[i];
            Color c = s.capsule ? capsuleColor : sphereColor;
            c.a *= Mathf.Clamp01(1f - (Time.time - s.birth) / duration);
            if (s.capsule) DrawCapsuleWire(s.p0, s.p1, s.radius, c, seg);
            else DrawSphereWire(s.p0, s.radius, c, seg);
        }
        GL.End();
        GL.PopMatrix();
    }

    private static void DrawSphereWire(Vector3 center, float radius, Color color, int seg)
    {
        GL.Color(color);
        for (int axis = 0; axis < 3; axis++)
        {
            Vector3 prev = center + CirclePoint(axis, 0f, radius);
            for (int i = 1; i <= seg; i++)
            {
                Vector3 cur = center + CirclePoint(axis, i * Mathf.PI * 2f / seg, radius);
                GL.Vertex(prev);
                GL.Vertex(cur);
                prev = cur;
            }
        }
    }

    private static void DrawCapsuleWire(Vector3 p0, Vector3 p1, float radius, Color color, int seg)
    {
        Vector3 axis = p1 - p0;
        float len = axis.magnitude;
        if (len < 0.0001f) { DrawSphereWire(p0, radius, color, seg); return; }
        axis /= len;
        Vector3 u = Mathf.Abs(axis.y) < 0.99f ? Vector3.Cross(axis, Vector3.up).normalized : Vector3.right;
        Vector3 v = Vector3.Cross(axis, u).normalized;

        GL.Color(color);
        for (int end = 0; end < 2; end++)
        {
            Vector3 center = end == 0 ? p0 : p1;
            Vector3 prev = center + u * radius;
            for (int i = 1; i <= seg; i++)
            {
                float ang = i * Mathf.PI * 2f / seg;
                Vector3 cur = center + (u * Mathf.Cos(ang) + v * Mathf.Sin(ang)) * radius;
                GL.Vertex(prev);
                GL.Vertex(cur);
                prev = cur;
            }
        }
        for (int k = 0; k < 4; k++)
        {
            float ang = k * Mathf.PI * 0.5f;
            Vector3 offset = (u * Mathf.Cos(ang) + v * Mathf.Sin(ang)) * radius;
            GL.Vertex(p0 + offset);
            GL.Vertex(p1 + offset);
        }
    }

    private static Vector3 CirclePoint(int axis, float angle, float radius)
    {
        float cos = Mathf.Cos(angle) * radius, sin = Mathf.Sin(angle) * radius;
        if (axis == 0) return new Vector3(cos, 0f, sin);
        if (axis == 1) return new Vector3(cos, sin, 0f);
        return new Vector3(0f, cos, sin);
    }
}
#endif
