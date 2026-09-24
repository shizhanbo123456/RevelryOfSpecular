using UnityEngine;

public class AnimRunEvent : AnimMotionEvent
{
    public float speedFactor=1;
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateUpdate(animator, stateInfo, layerIndex);
        if (main)
        {
            anim.SetVelocityForward(MoveSpeed*speedFactor);
        }
    }
}
