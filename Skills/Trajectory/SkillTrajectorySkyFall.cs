using UnityEngine;

// 天降：垂直升空 → 高空平移 → 垂直坠落，三段按固定比例分配
public class SkillTrajectorySkyFall : BulletTrajectory
{
    private readonly Vector3 start;
    private readonly Vector3 target;
    private readonly float skyHeight;

    private const float RiseEnd = 0.4f;
    private const float MoveEnd = 0.6f;

    public SkillTrajectorySkyFall(Vector3 start, Vector3 target, float duration, float skyHeight = 30f)
    {
        this.start = start;
        this.target = target;
        this.skyHeight = skyHeight;
        Duration = duration;
    }

    public override Vector3 Lerp(float factor)
    {
        if (factor <= 0f) return start;
        if (factor >= 1f) return target;

        Vector3 skyStart = start + Vector3.up * skyHeight;
        Vector3 skyTarget = target + Vector3.up * skyHeight;

        if (factor < RiseEnd)
        {
            return Vector3.Lerp(start, skyStart, factor / RiseEnd);
        }
        if (factor < MoveEnd)
        {
            // 高空平移段在视野外，只为把起飞点搬到目标正上方
            return Vector3.Lerp(skyStart, skyTarget, (factor - RiseEnd) / (MoveEnd - RiseEnd));
        }
        return Vector3.Lerp(skyTarget, target, (factor - MoveEnd) / (1f - MoveEnd));
    }

    public override Vector3 Start => start;
    public override Vector3 End => target;
}
