using UnityEngine;

// 弧线：三次贝塞尔，两个控制点都抬到起点/终点正上方 height 处
public class SkillTrajectoryBezier : BulletTrajectory
{
    private readonly Vector3 point1;
    private readonly Vector3 point2;
    private readonly Vector3 point3;
    private readonly Vector3 point4;

    public SkillTrajectoryBezier(Vector3 start, Vector3 end, float duration, float height = 8f)
    {
        point1 = start;
        point2 = start + Vector3.up * height;
        point3 = end + Vector3.up * height;
        point4 = end;
        Duration = duration;
    }

    public override Vector3 Lerp(float factor)
    {
        if (factor <= 0f) return point1;
        if (factor >= 1f) return point4;

        float t = factor;
        float t2 = t * t;
        float t3 = t2 * t;
        float oneMinusT = 1f - t;
        float oneMinusT2 = oneMinusT * oneMinusT;
        float oneMinusT3 = oneMinusT2 * oneMinusT;

        return point1 * oneMinusT3 +
               point2 * 3f * t * oneMinusT2 +
               point3 * 3f * t2 * oneMinusT +
               point4 * t3;
    }

    public override Vector3 Start => point1;
    public override Vector3 End => point4;
}
