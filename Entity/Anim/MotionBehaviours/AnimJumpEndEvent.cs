using UnityEngine;

public class AnimJumpEndEvent : AnimMotionEvent
{
    private const float thresholdLand = 0.25f;
    private bool canTrig = false;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);
        if (!main) return;
        canTrig = true;
    }
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateUpdate(animator, stateInfo, layerIndex);
        if (data == null) return;
        if (!main) return;

        if (canTrig&&stateInfo.normalizedTime > thresholdLand)
        {
            anim.SetVelocityForward(0);
            canTrig = false;
        }
    }
}
