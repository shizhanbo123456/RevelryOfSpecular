using UnityEngine;

// 直线：起点到终点线性插值
public class SkillTrajectoryLine : BulletTrajectory
{
    private readonly Vector3 start;
    private readonly Vector3 end;

    public SkillTrajectoryLine(Vector3 start, Vector3 end, float duration)
    {
        this.start = start;
        this.end = end;
        Duration = duration;
    }

    public override Vector3 Lerp(float factor)
    {
        return Vector3.LerpUnclamped(start, end, factor);
    }

    public override Vector3 Start => start;
    public override Vector3 End => end;
}
