using System.Collections.Generic;
using UnityEngine;

/// <summary>伤害判定范围可视化（仅 Editor）：挂到服务器场景任意物体。判定处调用 Sphere/Capsule，形状渲染 duration 秒并线性淡出（Scene 视图 Gizmos）。</summary>
#if UNITY_EDITOR
public class DamageRangeDebugHost : MonoBehaviour
{
    public static DamageRangeDebugHost Instance { get; private set; }

    [Tooltip("是否显示")]
    public bool show = true;

    [Tooltip("单次判定的渲染时长（秒），期间线性淡出")]
    public float duration = 1f;

    public Color sphereColor = new Color(1f, 0.45f, 0.15f);
    public Color capsuleColor = new Color(0.2f, 0.9f, 1f);

    [Tooltip("常驻测试球（原点 r=2），验证 Gizmos 显示是否开启")]
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

    private void OnEnable()
    {
        Instance = this;
        warnedNoInstance = false;
        Debug.Log($"[DamageRangeDebugHost] 已挂载（场景={gameObject.scene.name}, 物体={name}）");
    }
    private void OnDisable() { if (Instance == this) Instance = null; }

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

    private void Update()
    {
        while (shapes.Count > 0 && Time.time - shapes[0].birth >= duration) shapes.RemoveAt(0);
        if (Time.frameCount % 300 == 0) Debug.Log($"[DamageRangeDebugHost] 运行中 shapes={shapes.Count}");
    }

    private void OnDrawGizmos()
    {
        if (drawTestShape)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(Vector3.zero, 2f);
        }
        if (shapes.Count == 0 || !show) return;

        float duration = this.duration > 0f ? this.duration : 1f;
        for (int i = 0; i < shapes.Count; i++)
        {
            var s = shapes[i];
            Color c = s.capsule ? capsuleColor : sphereColor;
            c.a *= Mathf.Clamp01(1f - (Time.time - s.birth) / duration);
            Gizmos.color = c;
            if (s.capsule) DrawCapsuleWire(s.p0, s.p1, s.radius);
            else Gizmos.DrawWireSphere(s.p0, s.radius);
        }
    }

    private static void DrawCapsuleWire(Vector3 p0, Vector3 p1, float radius)
    {
        Vector3 axis = p1 - p0;
        float len = axis.magnitude;
        if (len < 0.0001f) { Gizmos.DrawWireSphere(p0, radius); return; }
        axis /= len;
        Vector3 u = Mathf.Abs(axis.y) < 0.99f ? Vector3.Cross(axis, Vector3.up).normalized : Vector3.right;
        Vector3 v = Vector3.Cross(axis, u).normalized;

        const int seg = 16;
        for (int end = 0; end < 2; end++)
        {
            Vector3 center = end == 0 ? p0 : p1;
            Vector3 prev = center + u * radius;
            for (int i = 1; i <= seg; i++)
            {
                float ang = i * Mathf.PI * 2f / seg;
                Vector3 cur = center + (u * Mathf.Cos(ang) + v * Mathf.Sin(ang)) * radius;
                Gizmos.DrawLine(prev, cur);
                prev = cur;
            }
        }
        for (int k = 0; k < 4; k++)
        {
            float ang = k * Mathf.PI * 0.5f;
            Vector3 offset = (u * Mathf.Cos(ang) + v * Mathf.Sin(ang)) * radius;
            Gizmos.DrawLine(p0 + offset, p1 + offset);
        }
    }
}
#endif
