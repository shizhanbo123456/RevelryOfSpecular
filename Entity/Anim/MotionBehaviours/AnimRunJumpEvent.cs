using UnityEngine;

public class AnimRunJumpEvent : AnimMotionEvent
{
    [SerializeField] private AnimationCurve speedCurve;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);
        if (data == null) return;
        if (main)
        {
            anim.SetVelocityVertical(JumpSpeed);
        }
    }
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateUpdate(animator, stateInfo, layerIndex);
        if (data == null) return;
        if (main)
        {
            anim.SetVelocityForward(speedCurve.Evaluate(stateInfo.normalizedTime) * MoveSpeed);
        }
    }
}
