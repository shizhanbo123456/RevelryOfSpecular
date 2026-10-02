using UnityEngine;

public abstract class MotionBase
{
    public float endTime;
    public bool canMove;

    public abstract Vector3 Enter(EntityData data, Vector3 speed);

    public abstract Vector3 Update(EntityData data, Vector3 speed);

    public abstract Vector3 Exit(EntityData data, Vector3 speed);
}

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

    public override Vector3 Exit(EntityData data, Vector3 speed) => speed; // 冲锋结束保留当前惯性，交回摩擦/动画接管
}
