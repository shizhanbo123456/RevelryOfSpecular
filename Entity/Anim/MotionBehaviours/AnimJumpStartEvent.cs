using UnityEngine;

public class AnimJumpStartEvent : AnimMotionEvent
{
    protected override InputBlockOp blockOp => InputBlockOp.Forward | InputBlockOp.Rotation | InputBlockOp.Jump | InputBlockOp.Slide | InputBlockOp.Attack;
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);
        if (data == null) return;
        if (!main) return;
        anim.SetVelocityVertical(JumpSpeed);
    }
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateUpdate(animator, stateInfo, layerIndex);
        if (data == null) return;
        if (main)
        {
            if(MovingForward)anim.SetVelocityForward(MoveSpeed*0.7f);
        }
    }
}
