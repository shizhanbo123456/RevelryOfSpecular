using UnityEngine;

public class AnimMotionEvent : AnimEvent
{
    protected override EntityAnim.AnimState State => EntityAnim.AnimState.Motion;
    protected float VelocityX
    {
        set
        {
            anim.SetVelocityForward(value);
        }
    }
    protected float VelocityY
    {
        set
        {
            anim.SetVelocityVertical(value);
        }
    }
}
