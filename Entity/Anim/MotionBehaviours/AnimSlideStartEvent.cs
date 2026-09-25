using UnityEngine;

public class AnimSlideStartEvent : AnimMotionEvent
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);
        if (data == null) return;
        if (main)
        {
            anim.SetVelocityForward(MoveSpeed * 2);
        }
    }
}
