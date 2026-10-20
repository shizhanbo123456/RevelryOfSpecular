using System;
using UnityEngine;

// 牵引：持续被目标吸引，距离越远吸力越大（速度 = 距离 × 系数，上限封顶）
// 目标可以是 Transform（实体/跟随物实时位置）、固定点，或自定义位置来源（如"飞行中的子弹"）
public class MotionAttract : MotionBase
{
    private readonly Func<Vector3> targetProvider;
    private readonly float forcePerMeter;
    private readonly float maxSpeed;

    public MotionAttract(Transform target, float duration, float forcePerMeter = 6f, float maxSpeed = 18f)
        : this(() => target != null ? target.position : Vector3.zero, duration, forcePerMeter, maxSpeed)
    {
    }

    public MotionAttract(Vector3 target, float duration, float forcePerMeter = 6f, float maxSpeed = 18f)
        : this(() => target, duration, forcePerMeter, maxSpeed)
    {
    }

    public MotionAttract(Func<Vector3> targetProvider, float duration, float forcePerMeter = 6f, float maxSpeed = 18f)
    {
        this.targetProvider = targetProvider;
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
        if (targetProvider == null) return Vector3.zero;
        Vector3 to = targetProvider() - data.transform.position;
        to.y = 0f;
        float dist = to.magnitude;
        if (dist <= 0.05f) return Vector3.zero;
        float moveSpeed = Mathf.Min(maxSpeed, dist * forcePerMeter);
        return to / dist * moveSpeed;
    }
}
