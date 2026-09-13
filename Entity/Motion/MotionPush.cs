using UnityEngine;

/// <summary>
/// 推离位移（击退）：Enter 时取「目标 → 远离 origin」的水平方向，以恒定速度推出 duration 秒。
/// 与 MotionToPoint 一样走速度积分（不改 position），保证与服务器权威移动同一条路径。
/// 用法：target.SetMotion(new MotionPush(casterPos, 14f, 0.25f));
/// </summary>
public class MotionPush : MotionBase
{
    private readonly Vector3 origin;
    private readonly float moveSpeed;
    private Vector3 dir;

    /// <param name="origin">推离的基准点（通常是施放者位置）。</param>
    /// <param name="speed">推离速度（米/秒）。</param>
    /// <param name="duration">持续时长（秒）。</param>
    public MotionPush(Vector3 origin, float speed, float duration)
    {
        this.origin = origin;
        moveSpeed = Mathf.Max(0.01f, speed);
        canMove = false;
        endTime = Time.time + Mathf.Max(0.01f, duration);
    }

    public override Vector3 Enter(EntityData data, Vector3 speed)
    {
        Vector3 away = data.transform.position - origin;
        away.y = 0f;
        dir = away.sqrMagnitude > 0.01f ? away.normalized : data.transform.forward;
        return dir * moveSpeed;
    }

    public override Vector3 Update(EntityData data, Vector3 speed) => dir * moveSpeed;

    public override void Exit(EntityData data)
    {
    }
}
