using UnityEngine;

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
