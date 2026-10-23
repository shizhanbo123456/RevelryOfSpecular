using UnityEngine;

public class AnimJumpEndEvent : AnimMotionEvent
{
    protected override InputBlockOp blockOp => InputBlockOp.Jump | InputBlockOp.Slide | InputBlockOp.Attack;
    [SerializeField]private float thresholdLand = 0.25f;

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateUpdate(animator, stateInfo, layerIndex);
        if (data == null) return;
        if (!main) return;

        if (stateInfo.normalizedTime > thresholdLand)
        {
            if (MovingForward) anim.SetVelocityForward(MoveSpeed);
            else anim.SetVelocityForward(0);
        }
    }
}
