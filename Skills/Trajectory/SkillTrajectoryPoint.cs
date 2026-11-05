using UnityEngine;

// 定点：位置不随归一化进度变化（原地范围技、陪特效走的子弹用它）
public class SkillTrajectoryPoint : BulletTrajectory
{
    private readonly Vector3 point;

    public SkillTrajectoryPoint(Vector3 point, float duration)
    {
        this.point = point;
        Duration = duration;
    }

    public override Vector3 Lerp(float factor)
    {
        return point;
    }

    public override Vector3 Start => point;
    public override Vector3 End => point;
}
