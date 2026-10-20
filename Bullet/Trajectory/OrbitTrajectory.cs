using UnityEngine;

// 环绕：绕某个实体转圈（起点角度固定、圈数固定，双端按实体 id 取实时位置）
public class OrbitTrajectory : BulletTrajectory
{
    private readonly ushort centerId;
    private readonly Vector3 fallbackCenter;
    private readonly float radius;
    private readonly float startAngleDeg;
    private readonly float turns;
    private readonly float height;

    public OrbitTrajectory(ushort centerId, Vector3 fallbackCenter, float radius, float startAngleDeg, float turns, float height = 0f)
    {
        this.centerId = centerId;
        this.fallbackCenter = fallbackCenter;
        this.radius = radius;
        this.startAngleDeg = startAngleDeg;
        this.turns = turns;
        this.height = height;
    }

    public override Vector3 Lerp(float factor)
    {
        Vector3 center = TryGetEntityPosition(centerId, out var pos) ? pos : fallbackCenter;
        float angle = (startAngleDeg + Mathf.Clamp01(factor) * 360f * turns) * Mathf.Deg2Rad;
        return center + new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
    }
}
