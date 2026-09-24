using UnityEngine;

public class AnimJumpStartEvent : AnimMotionEvent
{
    private const float threshold = 0.1f;
    private bool canTrigEvent;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);
        if (!main) return;
        canTrigEvent = true;
    }
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateUpdate(animator, stateInfo, layerIndex);
        if (main && canTrigEvent && stateInfo.normalizedTime > threshold)
        {
            canTrigEvent = false;
            anim.SetVelocityVertical(JumpSpeed);
        }
    }
}
