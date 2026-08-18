using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 带时间和渐变效果的Gizmos绘制工具
/// 发布版本(RELEASE)中所有代码会被完全移除，零性能开销
/// </summary>
public static class GizmosDrawer
{
#if !RELEASE
    /// <summary>
    /// 绘制任务数据结构
    /// </summary>
    private class DrawTask
    {
        public Action<Color> DrawAction;      // 绘制委托
        public float StartTime;               // 开始时间
        public float Duration;                // 持续时间
        public Color BaseColor;               // 基础颜色
    }

    // 所有待绘制的任务列表
    private static readonly List<DrawTask> _drawTasks = new List<DrawTask>();

    // 用于在编辑器模式下更新的GameObject
    private static GameObject _updaterObject;
    private static GizmosDrawerUpdater _updater;

    /// <summary>
    /// 初始化更新器
    /// </summary>
    private static void EnsureUpdater()
    {
        if (_updater != null) return;

        // 查找或创建更新器对象
        _updaterObject = GameObject.Find("GizmosDrawerUpdater");
        if (_updaterObject == null)
        {
            _updaterObject = new GameObject("GizmosDrawerUpdater");
            _updaterObject.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;
        }

        // 添加更新器组件
        _updater = _updaterObject.GetComponent<GizmosDrawerUpdater>();
        if (_updater == null)
        {
            _updater = _updaterObject.AddComponent<GizmosDrawerUpdater>();
        }

        // 注册绘制事件
        _updater.OnDrawGizmosEvent += OnDrawGizmos;
        _updater.OnUpdateEvent += OnUpdate;
    }

    /// <summary>
    /// 每帧更新，移除过期任务
    /// </summary>
    private static void OnUpdate()
    {
        // 从后往前遍历，安全移除元素
        for (int i = _drawTasks.Count - 1; i >= 0; i--)
        {
            if (Time.time - _drawTasks[i].StartTime >= _drawTasks[i].Duration)
            {
                _drawTasks.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Gizmos绘制回调
    /// </summary>
    private static void OnDrawGizmos()
    {
        float currentTime = Time.time;

        foreach (var task in _drawTasks)
        {
            float elapsed = currentTime - task.StartTime;
            float progress = Mathf.Clamp01(elapsed / task.Duration);

            // 计算当前颜色（alpha从1线性过渡到0）
            Color currentColor = task.BaseColor;
            currentColor.a = 1.0f - progress;

            // 执行绘制
            task.DrawAction(currentColor);
        }
    }

    /// <summary>
    /// 添加一个绘制任务
    /// </summary>
    /// <param name="drawAction">绘制动作，接收当前颜色参数</param>
    /// <param name="duration">显示时间（秒）</param>
    /// <param name="color">基础颜色</param>
    private static void AddDrawTask(Action<Color> drawAction, float duration, Color color)
    {
        EnsureUpdater();

        _drawTasks.Add(new DrawTask
        {
            DrawAction = drawAction,
            StartTime = Time.time,
            Duration = duration,
            BaseColor = color
        });
    }
#endif

    #region 公共绘制API

    /// <summary>
    /// 绘制线段
    /// </summary>
    /// <param name="from">起点</param>
    /// <param name="to">终点</param>
    /// <param name="color">颜色</param>
    /// <param name="duration">显示时间（秒）</param>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void DrawLine(Vector3 from, Vector3 to, Color color, float duration = 1.0f)
    {
#if !RELEASE
        AddDrawTask(c =>
        {
            Gizmos.color = c;
            Gizmos.DrawLine(from, to);
        }, duration, color);
#endif
    }

    /// <summary>
    /// 绘制立方体线框
    /// </summary>
    /// <param name="center">中心</param>
    /// <param name="size">尺寸</param>
    /// <param name="color">颜色</param>
    /// <param name="duration">显示时间（秒）</param>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void DrawWireCube(Vector3 center, Vector3 size, Color color, float duration = 1.0f)
    {
#if !RELEASE
        AddDrawTask(c =>
        {
            Gizmos.color = c;
            Gizmos.DrawWireCube(center, size);
        }, duration, color);
#endif
    }

    /// <summary>
    /// 绘制实心立方体
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void DrawCube(Vector3 center, Vector3 size, Color color, float duration = 1.0f)
    {
#if !RELEASE
        AddDrawTask(c =>
        {
            Gizmos.color = c;
            Gizmos.DrawCube(center, size);
        }, duration, color);
#endif
    }

    /// <summary>
    /// 绘制球体线框
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void DrawWireSphere(Vector3 center, float radius, Color color, float duration = 1.0f)
    {
#if !RELEASE
        AddDrawTask(c =>
        {
            Gizmos.color = c;
            Gizmos.DrawWireSphere(center, radius);
        }, duration, color);
#endif
    }

    /// <summary>
    /// 绘制实心球体
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void DrawSphere(Vector3 center, float radius, Color color, float duration = 1.0f)
    {
#if !RELEASE
        AddDrawTask(c =>
        {
            Gizmos.color = c;
            Gizmos.DrawSphere(center, radius);
        }, duration, color);
#endif
    }

    /// <summary>
    /// 绘制胶囊体线框
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void DrawWireCapsule(Vector3 center, float radius, float height, Color color, float duration = 1.0f)
    {
#if !RELEASE
        AddDrawTask(c =>
        {
            Gizmos.color = c;

            // 计算上下半球中心
            float halfHeight = height * 0.5f - radius;
            Vector3 topCenter = center + Vector3.up * halfHeight;
            Vector3 bottomCenter = center - Vector3.up * halfHeight;

            // 绘制上下半球
            DrawWireHemisphere(topCenter, radius, Vector3.up, c);
            DrawWireHemisphere(bottomCenter, radius, Vector3.down, c);

            // 绘制侧面四条线
            Gizmos.DrawLine(topCenter + Vector3.right * radius, bottomCenter + Vector3.right * radius);
            Gizmos.DrawLine(topCenter - Vector3.right * radius, bottomCenter - Vector3.right * radius);
            Gizmos.DrawLine(topCenter + Vector3.forward * radius, bottomCenter + Vector3.forward * radius);
            Gizmos.DrawLine(topCenter - Vector3.forward * radius, bottomCenter - Vector3.forward * radius);
        }, duration, color);
#endif
    }

    /// <summary>
    /// 绘制任意方向的胶囊体线框（沿 from→to 轴向，半径 radius）。
    /// 用于表达子弹扫掠判定体（BulletHit 线段-胶囊判定）。
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void DrawWireCapsuleBetween(Vector3 from, Vector3 to, float radius, Color color, float duration = 1.0f)
    {
#if !RELEASE
        AddDrawTask(c =>
        {
            Gizmos.color = c;

            Vector3 axis = to - from;
            float len = axis.magnitude;
            if (len < 0.001f)
            {
                Gizmos.DrawWireSphere(from, radius);
                return;
            }
            Vector3 dir = axis / len;

            // 构造正交基（任意非平行方向）
            Vector3 refVec = Mathf.Abs(Vector3.Dot(dir, Vector3.up)) > 0.9f ? Vector3.right : Vector3.up;
            Vector3 right = Vector3.Cross(dir, refVec).normalized;
            Vector3 up = Vector3.Cross(dir, right).normalized;

            // 两端半球
            DrawWireHemisphere(from, radius, -dir, c);
            DrawWireHemisphere(to, radius, dir, c);

            // 侧面四条线（正交基两个方向 ±）
            Gizmos.DrawLine(from + right * radius, to + right * radius);
            Gizmos.DrawLine(from - right * radius, to - right * radius);
            Gizmos.DrawLine(from + up * radius, to + up * radius);
            Gizmos.DrawLine(from - up * radius, to - up * radius);
        }, duration, color);
#endif
    }

    /// <summary>
    /// 绘制圆柱体线框
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void DrawWireCylinder(Vector3 center, float radius, float height, Color color, float duration = 1.0f)
    {
#if !RELEASE
        AddDrawTask(c =>
        {
            Gizmos.color = c;

            float halfHeight = height * 0.5f;
            Vector3 topCenter = center + Vector3.up * halfHeight;
            Vector3 bottomCenter = center - Vector3.up * halfHeight;

            // 绘制上下两个圆
            DrawWireCircle(topCenter, radius, Vector3.up, c);
            DrawWireCircle(bottomCenter, radius, Vector3.up, c);

            // 绘制侧面四条线
            Gizmos.DrawLine(topCenter + Vector3.right * radius, bottomCenter + Vector3.right * radius);
            Gizmos.DrawLine(topCenter - Vector3.right * radius, bottomCenter - Vector3.right * radius);
            Gizmos.DrawLine(topCenter + Vector3.forward * radius, bottomCenter + Vector3.forward * radius);
            Gizmos.DrawLine(topCenter - Vector3.forward * radius, bottomCenter - Vector3.forward * radius);
        }, duration, color);
#endif
    }

    /// <summary>
    /// 绘制射线
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void DrawRay(Ray ray, float length, Color color, float duration = 1.0f)
    {
#if !RELEASE
        AddDrawTask(c =>
        {
            Gizmos.color = c;
            Gizmos.DrawRay(ray.origin, ray.direction * length);
        }, duration, color);
#endif
    }

    /// <summary>
    /// 绘制射线
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void DrawRay(Vector3 origin, Vector3 direction, float length, Color color, float duration = 1.0f)
    {
        DrawRay(new Ray(origin, direction), length, color, duration);
    }

    /// <summary>
    /// 绘制箭头
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void DrawArrow(Vector3 from, Vector3 to, Color color, float arrowHeadSize = 0.3f, float duration = 1.0f)
    {
#if !RELEASE
        AddDrawTask(c =>
        {
            Gizmos.color = c;

            // 绘制主线
            Gizmos.DrawLine(from, to);

            // 计算箭头方向
            Vector3 direction = (to - from).normalized;
            Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
            Vector3 up = Vector3.Cross(right, direction).normalized;

            // 绘制箭头头部
            Vector3 arrowPoint1 = to - direction * arrowHeadSize + right * arrowHeadSize * 0.5f;
            Vector3 arrowPoint2 = to - direction * arrowHeadSize - right * arrowHeadSize * 0.5f;
            Vector3 arrowPoint3 = to - direction * arrowHeadSize + up * arrowHeadSize * 0.5f;
            Vector3 arrowPoint4 = to - direction * arrowHeadSize - up * arrowHeadSize * 0.5f;

            Gizmos.DrawLine(to, arrowPoint1);
            Gizmos.DrawLine(to, arrowPoint2);
            Gizmos.DrawLine(to, arrowPoint3);
            Gizmos.DrawLine(to, arrowPoint4);
        }, duration, color);
#endif
    }

    /// <summary>
    /// 绘制自定义图形
    /// </summary>
    /// <param name="drawAction">自定义绘制动作，接收当前颜色参数</param>
    /// <param name="color">基础颜色</param>
    /// <param name="duration">显示时间（秒）</param>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void DrawCustom(Action<Color> drawAction, Color color, float duration = 1.0f)
    {
#if !RELEASE
        if (drawAction == null) return;
        AddDrawTask(drawAction, duration, color);
#endif
    }

    /// <summary>
    /// 清除所有绘制任务
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void ClearAll()
    {
#if !RELEASE
        _drawTasks.Clear();
#endif
    }

    #endregion

    #region 内部辅助方法

#if !RELEASE
    /// <summary>
    /// 绘制圆线框
    /// </summary>
    private static void DrawWireCircle(Vector3 center, float radius, Vector3 normal, Color color)
    {
        Gizmos.color = color;

        // 计算切线方向
        Vector3 tangent = Vector3.Cross(normal, Vector3.up).normalized;
        if (tangent.magnitude < 0.001f)
        {
            tangent = Vector3.Cross(normal, Vector3.right).normalized;
        }
        Vector3 bitangent = Vector3.Cross(normal, tangent).normalized;

        // 绘制32段圆
        int segments = 32;
        float angleStep = 360.0f / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;

            Vector3 point1 = center + (tangent * Mathf.Cos(angle1) + bitangent * Mathf.Sin(angle1)) * radius;
            Vector3 point2 = center + (tangent * Mathf.Cos(angle2) + bitangent * Mathf.Sin(angle2)) * radius;

            Gizmos.DrawLine(point1, point2);
        }
    }

    /// <summary>
    /// 绘制半球线框
    /// </summary>
    private static void DrawWireHemisphere(Vector3 center, float radius, Vector3 direction, Color color)
    {
        Gizmos.color = color;

        // 计算正交基
        Vector3 normal = direction.normalized;
        Vector3 tangent = Vector3.Cross(normal, Vector3.up).normalized;
        if (tangent.magnitude < 0.001f)
        {
            tangent = Vector3.Cross(normal, Vector3.right).normalized;
        }
        Vector3 bitangent = Vector3.Cross(normal, tangent).normalized;

        // 绘制经线（8条）
        int meridians = 8;
        float meridianStep = 360.0f / meridians;

        for (int i = 0; i < meridians; i++)
        {
            float angle = i * meridianStep * Mathf.Deg2Rad;
            Vector3 axis = tangent * Mathf.Cos(angle) + bitangent * Mathf.Sin(angle);

            // 绘制半圆
            int segments = 16;
            float segmentStep = 90.0f / segments;

            for (int j = 0; j < segments; j++)
            {
                float angle1 = j * segmentStep * Mathf.Deg2Rad;
                float angle2 = (j + 1) * segmentStep * Mathf.Deg2Rad;

                Vector3 point1 = center + (normal * Mathf.Cos(angle1) + axis * Mathf.Sin(angle1)) * radius;
                Vector3 point2 = center + (normal * Mathf.Cos(angle2) + axis * Mathf.Sin(angle2)) * radius;

                Gizmos.DrawLine(point1, point2);
            }
        }

        // 绘制纬线（4条）
        int parallels = 4;
        float parallelStep = 90.0f / parallels;

        for (int i = 1; i <= parallels; i++)
        {
            float angle = i * parallelStep * Mathf.Deg2Rad;
            float parallelRadius = radius * Mathf.Sin(angle);
            Vector3 parallelCenter = center + normal * radius * Mathf.Cos(angle);

            DrawWireCircle(parallelCenter, parallelRadius, normal, color);
        }
    }
#endif

    #endregion

    #region 内部更新器

#if !RELEASE
    /// <summary>
    /// 清空更新器引用（由 GizmosDrawerUpdater.OnDestroy 调用）。
    /// </summary>
    public static void ClearUpdater()
    {
        _updater = null;
        _updaterObject = null;
    }
#endif

    #endregion
}