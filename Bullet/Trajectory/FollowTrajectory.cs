using UnityEngine;

public class FollowTrajectory : BulletTrajectory
{
    private readonly ushort entityId;
    private readonly Vector3 offset;
    private Vector3 lastPos = Vector3.zero;

    /// <param name="entityId">跟随的实体 id（双端按 id 取位置，禁止直接引用 EntityData/表现物体）。</param>
    /// <param name="offset">相对实体位置的偏移（如悬浮到胸口/头顶高度）。</param>
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
