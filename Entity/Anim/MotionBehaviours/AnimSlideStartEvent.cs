using UnityEngine;

public class AnimSlideStartEvent : AnimMotionEvent
{
    protected override InputBlockOp blockOp => InputBlockOp.Forward | InputBlockOp.Rotation | InputBlockOp.Jump | InputBlockOp.Slide | InputBlockOp.Attack;
    [SerializeField] private AnimationCurve speedCurve;
    private float slideTime;
    private bool canEnd;
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);
        if (data == null) return;
        if (main)
        {
            slideTime = 0;
            canEnd = true;
        }
    }
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateUpdate(animator, stateInfo, layerIndex);
        if (main)
        {
            anim.SetVelocityForward(speedCurve.Evaluate(stateInfo.normalizedTime) * MoveSpeed);
            slideTime += Time.deltaTime;
            if (slideTime > 0.9f && canEnd)
            {
                anim.EndSlide();
                canEnd = false;
            }
        }
    }
}
