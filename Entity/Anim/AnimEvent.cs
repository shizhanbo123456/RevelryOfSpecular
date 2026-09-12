using UnityEngine;

/// <summary>
/// 状态机事件脚本（挂在 Animator 状态上，每状态一个实例）。
/// state 需在 Inspector 逐状态配置；子类可叠加帧事件（见 AnimAttackEvent）。
/// </summary>
public class AnimEvent : StateMachineBehaviour
{
    [SerializeField] private EntityAnim.AnimState state = EntityAnim.AnimState.Motion;
    public EntityAnim.AnimState State => state;
    /// <summary>用于传递给客户端识别动画片段</summary>
    public int AnimId { get; private set; } = -1;

    protected EntityAnim anim;
    protected EntityData data;
    protected bool initialized = false;

    public void Init(EntityAnim anim, EntityData data)
    {
        this.anim = anim;
        this.data = data;
        initialized = true;
    }

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);
        if (!initialized) return; // Init 前状态机可能已评估，忽略
        AnimId = stateInfo.fullPathHash; // 状态 hash，两端一致
        anim.RegisterAnimEvent(this);
        anim.NotifyStateEnter(AnimId, state);
    }
}
