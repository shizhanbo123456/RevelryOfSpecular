using UnityEngine;

/// <summary>
/// 朝目标点位移（影袭 / 闪现等）：Enter 时朝目标点方向以恒定速度前进，走完距离即结束。
/// 用速度驱动（不改 position），保证与服务器权威移动同一条积分路径、不穿模。
/// 用法：entity.SetMotion(new MotionToPoint(dest, 12f));
/// </summary>
public class MotionToPoint : MotionBase
{
    private readonly Vector3 dest;
    private readonly float moveSpeed;
    private Vector3 dir;

    /// <param name="dest">目标点。</param>
    /// <param name="speed">位移速度（米/秒），时长由 Enter 时的实际距离推出。</param>
    public MotionToPoint(Vector3 dest, float speed)
    {
        this.dest = dest;
        moveSpeed = Mathf.Max(0.01f, speed);
        canMove = false;
        endTime = float.MaxValue; // Enter 时按实际距离重算
    }

    public override Vector3 Enter(EntityData data, Vector3 speed)
    {
        Vector3 to = dest - data.transform.position;
        to.y = 0f;
        float dist = to.magnitude;
        dir = dist > 0.01f ? to / dist : data.transform.forward;
        endTime = Time.time + dist / moveSpeed;
        return dir * moveSpeed;
    }

    public override Vector3 Update(EntityData data, Vector3 speed) => dir * moveSpeed;

    public override void Exit(EntityData data)
    {
    }
}
