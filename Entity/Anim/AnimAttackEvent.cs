using UnityEngine;

public class AnimAttackEvent : AnimEvent
{
    [SerializeField] private EntityAnim.AttackType type;
    [SerializeField][Range(0,1)] private float threshold;
    private bool canTrigAttack;
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        canTrigAttack = true;
    }
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (canTrigAttack && stateInfo.normalizedTime > threshold)
        {
            canTrigAttack = false;
            OnAttack();
        }
    }
    private void OnAttack()
    {
        anim.onAttack?.Invoke(type);
    }
}