using UnityEngine;

/// <summary>
/// 跟随轨迹：位置恒等于目标实体当前位置（+ 可选偏移），与进度无关。
/// 服务器判定与客户端表现共用（通过 TryGetEntityPosition 按实体 id 取位置，双端一致）。
/// 适用：护盾/Buff/光环等绑定实体的持续型特效与相关范围判定。
/// 目标消失（死亡/表现移除）时保持最后已知位置；特效生命周期由调用方
/// 按 Buff 增删管理（Add 时创建，Remove 时销毁返回的实例，或用 Buff 剩余时长作 lifeTime）。
/// </summary>
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

    /// <summary>进度无关：任意 factor 都返回目标当前位置 + 偏移；目标丢失时保持最后已知位置。</summary>
    public override Vector3 Lerp(float factor)
    {
        if (TryGetEntityPosition(entityId, out var pos)) lastPos = pos;
        return lastPos + offset;
    }
}
