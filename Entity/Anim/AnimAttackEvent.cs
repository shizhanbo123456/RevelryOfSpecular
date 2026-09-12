using UnityEngine;

public class AnimAttackEvent : AnimEvent
{
    [SerializeField] private EntityAnim.AttackType type;
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
        canTrigAttack = true;
        canTrigAttack2 = true;
    }
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateExit(animator, stateInfo, layerIndex);
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
    private void OnAttack()
    {
        anim.onAttack?.Invoke(type);
    }
}
