using UnityEngine;

public class FollowTrajectory : BulletTrajectory
{
    private readonly ushort entityId;
    private readonly Vector3 offset;
    private Vector3 lastPos = Vector3.zero;

    // 按实体 id 双端取位，不引用 EntityData 或表现物体
    public FollowTrajectory(ushort entityId, Vector3 offset = default)
    {
        this.entityId = entityId;
        this.offset = offset;
    }

    public override Vector3 Lerp(float factor)
    {
        if (TryGetEntityPosition(entityId, out var pos)) lastPos = pos;
        return lastPos + offset;
    }
}
