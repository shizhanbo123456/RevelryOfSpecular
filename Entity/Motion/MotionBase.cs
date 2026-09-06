using UnityEngine;

/// <summary>
/// 位移效果基类（abstract）：施放者的一次受控位移（冲锋/击退/拉拽/强制位移等）。
/// 生命周期：EntityData.SetMotion 设置时调用 Enter → 生效期间每帧调用 Update（可不断改写角色速度，
/// 例如恒定返回 data.transform.forward * 8 即让角色自动前进）→ 到达 endTime 由 EntityData 调用 Exit。
/// canMove = false 时位移期间锁玩家输入移动，速度完全由本效果控制。
/// 注意：速度的实际应用在服务器权威移动逻辑（TODO）中消费 EntityData.motionVelocity；
/// 需要实体位置时通过 EntityData/BulletTrajectory.TryGetEntityPosition 按 id 读取（客户端无 EntityData）。
/// </summary>
public abstract class MotionBase
{
    /// <summary>生效结束时间（Time.time 时刻），由施放侧设置。</summary>
    public float endTime;
    /// <summary>生效期间是否允许玩家输入移动（false = 速度完全由本效果控制）。</summary>
    public bool canMove;

    /// <summary>生效时调用（SetMotion 设置时）：返回生效后的初始速度。</summary>
    public abstract Vector3 Enter(EntityData data, Vector3 speed);

    /// <summary>生效期间每帧调用：返回本帧速度（内部可用 Time.deltaTime）。</summary>
    public abstract Vector3 Update(EntityData data, Vector3 speed);

    /// <summary>结束时调用（时间到 / 被替换 / 打断）。</summary>
    public abstract void Exit(EntityData data);
}

/// <summary>
/// [E] 示例：冲锋位移（0.3 秒，期间恒定向前 8m/s 并锁玩家输入移动）。
/// 用法：entity.SetMotion(new MotionDash(0.3f, 8f));
/// </summary>
public class MotionDash : MotionBase
{
    private readonly float speed;

    public MotionDash(float duration, float speed)
    {
        endTime = Time.time + duration;
        canMove = false;
        this.speed = speed;
    }

    public override Vector3 Enter(EntityData data, Vector3 speed)
    {
        return data.transform.forward * this.speed;
    }

    public override Vector3 Update(EntityData data, Vector3 speed)
    {
        return data.transform.forward * this.speed;
    }

    public override void Exit(EntityData data)
    {
        // 结束处理（如恢复速度/播放收尾表现）按需实现
    }
}
