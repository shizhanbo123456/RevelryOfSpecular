using UnityEngine;

/// <summary>
/// 标准三次贝塞尔轨迹（4控制点，BulletTrajectory 的实现）。
/// 曲线经过起点(point1)和终点(point4)，不经过中间控制点(point2, point3)。
/// 原 Utils/SimpleUtils/BezierCurve 的逻辑整体迁移至此。
/// </summary>
public class BezierTrajectory : BulletTrajectory
{
    private readonly Vector3 point1; // P0 起点
    private readonly Vector3 point2; // P1 第一个控制点
    private readonly Vector3 point3; // P2 第二个控制点
    private readonly Vector3 point4; // P3 终点

    public BezierTrajectory(Vector3 point1, Vector3 point2, Vector3 point3, Vector3 point4)
    {
        this.point1 = point1;
        this.point2 = point2;
        this.point3 = point3;
        this.point4 = point4;
    }

    public override Vector3 Lerp(float factor)
    {
        // 边界处理
        if (factor <= 0f) return point1;
        if (factor >= 1f) return point4;

        // 预计算常用项，减少重复运算
        float t = factor;
        float t2 = t * t;
        float t3 = t2 * t;
        float oneMinusT = 1f - t;
        float oneMinusT2 = oneMinusT * oneMinusT;
        float oneMinusT3 = oneMinusT2 * oneMinusT;

        // 标准三次贝塞尔曲线公式
        return point1 * oneMinusT3 +
               point2 * 3f * t * oneMinusT2 +
               point3 * 3f * t2 * oneMinusT +
               point4 * t3;
    }

    /// <summary>抛物线弹道（默认弧高 = 距离一半，roll 为绕弹道轴的滚转角）。</summary>
    public static BezierTrajectory GetProjection(Vector3 center, Vector3 offset, float roll)
    {
        return GetProjection(center, offset, offset.magnitude * 0.5f, roll);
    }

    /// <summary>抛物线弹道（自定义弧高 radius，roll 为绕弹道轴的滚转角）。</summary>
    public static BezierTrajectory GetProjection(Vector3 center, Vector3 offset, float radius, float roll)
    {
        var t = Tool.GetTemporaryTransform();
        t.position = Vector3.zero;
        t.LookAt(offset);
        var euler = t.rotation.eulerAngles;
        euler.z = roll;
        t.rotation = Quaternion.Euler(euler);
        Vector3 up = t.up * radius;
        return new BezierTrajectory(center, center + up, center + offset + up, center + offset);
    }

    /// <summary>原地静止弹道。</summary>
    public static BezierTrajectory GetPoint(Vector3 pos)
    {
        return new BezierTrajectory(pos, pos, pos, pos);
    }

    /// <summary>直线弹道。</summary>
    public static BezierTrajectory GetLine(Vector3 start, Vector3 end)
    {
        return new BezierTrajectory(start, Vector3.Lerp(start, end, 0.33f), Vector3.Lerp(start, end, 0.67f), end);
    }
}
