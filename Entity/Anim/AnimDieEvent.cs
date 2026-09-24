using UnityEngine;

public class AnimDieEvent : AnimEvent
{
    protected override EntityAnim.AnimState State => EntityAnim.AnimState.Die;

    private const float threshold=0.8f;
    private bool canTrigEvent;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);
        if(main) canTrigEvent = true;
    }
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateUpdate(animator, stateInfo, layerIndex);
        if (!main) return;
        if (canTrigEvent && stateInfo.normalizedTime > threshold)
        {
            canTrigEvent = false;
            anim?.OnDeathEventEnd?.Invoke();
        }
    }
}
