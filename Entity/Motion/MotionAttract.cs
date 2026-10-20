using UnityEngine;

// 牵引：朝目标点持续位移，距离越远吸力越大（速度 = 距离 × 系数，上限封顶）
public class MotionAttract : MotionBase
{
    private readonly Vector3 target;
    private readonly float forcePerMeter;
    private readonly float maxSpeed;

    public MotionAttract(Vector3 target, float duration, float forcePerMeter = 6f, float maxSpeed = 18f)
    {
        this.target = target;
        this.forcePerMeter = forcePerMeter;
        this.maxSpeed = maxSpeed;
        endTime = Time.time + duration;
        canMove = false;
    }

    public override Vector3 Enter(EntityData data, Vector3 speed) => Velocity(data);

    public override Vector3 Update(EntityData data, Vector3 speed) => Velocity(data);

    public override Vector3 Exit(EntityData data, Vector3 speed) => speed; // 牵引结束保留惯性，交回摩擦/动画

    private Vector3 Velocity(EntityData data)
    {
        Vector3 to = target - data.transform.position;
        to.y = 0f;
        float dist = to.magnitude;
        if (dist <= 0.05f) return Vector3.zero;
        float moveSpeed = Mathf.Min(maxSpeed, dist * forcePerMeter);
        return to / dist * moveSpeed;
    }
}
