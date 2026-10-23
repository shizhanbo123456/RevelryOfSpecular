using UnityEngine;

public class AnimFallEvent : AnimMotionEvent
{
    protected override InputBlockOp blockOp => InputBlockOp.Jump | InputBlockOp.Slide | InputBlockOp.Attack;

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateUpdate(animator, stateInfo, layerIndex);
        if (data == null) return;
        if (main)
        {
            if (MovingForward) anim.SetVelocityForward(MoveSpeed * 1.3f);
        }
    }
}
