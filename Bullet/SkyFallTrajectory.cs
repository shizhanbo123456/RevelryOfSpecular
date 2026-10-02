using UnityEngine;

public class SkyFallTrajectory : BulletTrajectory
{
    private readonly Vector3 start;
    private readonly Vector3 target;
    private readonly float skyHeight;

    private const float RiseEnd = 0.4f;
    private const float MoveEnd = 0.6f;

    public SkyFallTrajectory(Vector3 start, Vector3 target, float skyHeight = 30f)
    {
        this.start = start;
        this.target = target;
        this.skyHeight = skyHeight;
    }

    public override Vector3 Lerp(float factor)
    {
        if (factor <= 0f) return start;
        if (factor >= 1f) return target;

        Vector3 skyStart = start + Vector3.up * skyHeight;
        Vector3 skyTarget = target + Vector3.up * skyHeight;

        if (factor < RiseEnd)
        {
            // 升空段：起点 → 起点上空
            return Vector3.Lerp(start, skyStart, factor / RiseEnd);
        }
        if (factor < MoveEnd)
        {
            // 高空平移段：起 点上空 → 目标点上空（视野外，不影响表现）
            return Vector3.Lerp(skyStart, skyTarget, (factor - RiseEnd) / (MoveEnd - RiseEnd));
        }
        // 坠落段：目标点上空 → 目标点
        return Vector3.Lerp(skyTarget, target, (factor - MoveEnd) / (1f - MoveEnd));
    }

    public override Vector3 Start => start;
    public override Vector3 End => target;
}
