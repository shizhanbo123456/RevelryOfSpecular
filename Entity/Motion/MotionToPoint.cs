using UnityEngine;

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

    public override Vector3 Exit(EntityData data, Vector3 speed) => speed; // 到点结束保留当前惯性，交回摩擦/动画接管
}
