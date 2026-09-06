using UnityEngine;

/// <summary>
/// 定点（静止）弹道轨迹：整个生命周期停留在固定点。
/// 适用：定点预警判定、悬浮引信、固定范围效果载体等。
/// </summary>
public class PointTrajectory : BulletTrajectory
{
    private readonly Vector3 point;

    public PointTrajectory(Vector3 point)
    {
        this.point = point;
    }

    public override Vector3 Lerp(float factor)
    {
        return point;
    }

    public override Vector3 Start => point;
    public override Vector3 End => point;
}
