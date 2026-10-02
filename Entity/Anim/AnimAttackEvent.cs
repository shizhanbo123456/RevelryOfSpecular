using UnityEngine;

public class AnimAttackEvent : AnimEvent
{
    protected override EntityAnim.AnimState State => EntityAnim.AnimState.Attack;
    [SerializeField] private EntityAnim.AttackType type;
    [SerializeField] private AnimationCurve speedForward;
    [SerializeField] private AnimationCurve speedUpward;
    [Header("Hit1")]
    [SerializeField][Range(0,1)] private float threshold;
    private bool canTrigAttack;
    [Header("Hit2")]
    [SerializeField] private bool useHit2 = false;
    [SerializeField][Range(0, 1)] private float threshold2;
    private bool canTrigAttack2;
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator,stateInfo,layerIndex);
        canTrigAttack = true;   // 客户端无 EntityData：仍允许攻击帧触发 onAttack（特效/表现用），不依赖 data
        canTrigAttack2 = true;
        if (data == null) return;
        if (!main) return;
    }
    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateUpdate(animator, stateInfo, layerIndex);
        if (!main) return;

        float forward = speedForward.Evaluate(stateInfo.normalizedTime);
        float upward = speedUpward.Evaluate(stateInfo.normalizedTime);
        anim.SetVelocityForward(forward * anim.animData.RunSpeed);
        anim.SetVelocityVertical(upward * anim.animData.JumpSpeed);

        if (canTrigAttack && stateInfo.normalizedTime > threshold)
        {
            canTrigAttack = false;
            OnAttack();
        }
        if (useHit2&&canTrigAttack2 && stateInfo.normalizedTime > threshold2)
        {
            canTrigAttack2 = false;
            OnAttack();
        }
    }
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateExit(animator, stateInfo, layerIndex);
        if (data == null) return;
        if (!main) return;
        // 手持武器不在此清除：发包时按当前动画状态解析（非武器类攻击 = 空手），残留值不会发给客户端
    }
    private void OnAttack()
    {
        if (anim.onAttack != null) anim.onAttack.Invoke(type);
    }
}
