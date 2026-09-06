using UnityEngine;

/// <summary>
/// 直线弹道轨迹：从起点匀速直线运动到终点。
/// </summary>
public class LineTrajectory : BulletTrajectory
{
    private readonly Vector3 start;
    private readonly Vector3 end;

    public LineTrajectory(Vector3 start, Vector3 end)
    {
        this.start = start;
        this.end = end;
    }

    public override Vector3 Lerp(float factor)
    {
        return Vector3.LerpUnclamped(start, end, factor);
    }

    public override Vector3 Start => start;
    public override Vector3 End => end;
}
