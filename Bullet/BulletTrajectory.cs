using UnityEngine;

/// <summary>
/// 弹道轨迹基类（abstract）。
/// 子弹系统只依赖本基类，不关心具体轨迹实现；新增轨迹类型（直线/追踪/环绕/自定义等）
/// 继承并实现 Lerp 即可（现有实现示例：BezierTrajectory）。
/// 约定：factor 为归一化进度 0~1（0=起点，1=终点）；
/// 服务器与客户端需按相同参数各自构建轨迹（服务器用于逻辑判定，客户端用于表现）。
/// 轨迹实现允许携带状态，可实现随时间变化的动态轨迹（如跟随目标、受场景空间影响）。
/// 【重要】EntityData 组件只存在于服务器；客户端仅有表现物体。轨迹实现需要实体位置时，
/// 必须通过 TryGetEntityPosition 按实体 id 读取，禁止直接读取 EntityData 组件。
/// </summary>
public abstract class BulletTrajectory
{
    /// <summary>取归一化进度 factor（0~1）处的位置。</summary>
    public abstract Vector3 Lerp(float factor);

    /// <summary>轨迹起点。</summary>
    public virtual Vector3 Start => Lerp(0f);

    /// <summary>轨迹终点。</summary>
    public virtual Vector3 End => Lerp(1f);

    /// <summary>
    /// 轨迹时长（秒）：服务器判定与客户端表现共用同一个数，调用方不再各传一份。
    /// </summary>
    public float Duration = 1f;

    /// <summary>
    /// 通过实体 id 读取位置（服务器走 BattleManager 实体容器，客户端走 ClientDisplayManager 表现物体）。
    /// </summary>
    public static bool TryGetEntityPosition(ushort entityId, out Vector3 pos)
    {
        return TryGetEntityTransform(entityId, out pos, out _);
    }

    /// <summary>
    /// 通过实体 id 读取完整变换（位置 + 朝向）。两端都取得到：
    /// 服务器走 BattleManager 实体容器，客户端走 ClientDisplayManager 表现物体。
    /// 复原依赖朝向的挂点位置（如悬浮武器发射点）必须用它，只取位置会偏。
    /// </summary>
    public static bool TryGetEntityTransform(ushort entityId, out Vector3 pos, out Quaternion rot)
    {
        pos = Vector3.zero;
        rot = Quaternion.identity;
        // 服务器：实体容器
        if (Tool.BattleManager != null &&
            BattleManager.EntityContainer.Entities.TryGetObject(entityId, out var entity) &&
            entity != null)
        {
            pos = entity.transform.position;
            rot = entity.transform.rotation;
            return true;
        }
        // 客户端：表现物体
        if (Tool.ClientDisplayManager != null)
        {
            return Tool.ClientDisplayManager.TryGetEntityTransform(entityId, out pos, out rot);
        }
        return false;
    }
}
