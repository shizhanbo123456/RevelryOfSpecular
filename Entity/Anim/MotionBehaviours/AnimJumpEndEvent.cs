using UnityEngine;

public class AnimJumpEndEvent : AnimMotionEvent
{
    private const float thresholdLand = 0.5f;
    private const float thresholdMove = 0.8f;
    [SerializeField] private bool moveAfterLand;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);
    }
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateUpdate(animator, stateInfo, layerIndex);
        if (!main) return;
        if (moveAfterLand)
        {
            if (stateInfo.normalizedTime > thresholdMove)
            {
                anim.SetVelocityForward(MoveSpeed);
            }
            else if (stateInfo.normalizedTime > thresholdLand)
            {
                anim.SetVelocityForward(0);
            }
        }
        else
        {
            anim.SetVelocityForward(0);
        }
    }
}
