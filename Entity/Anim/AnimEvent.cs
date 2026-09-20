using UnityEngine;

public class AnimEvent : StateMachineBehaviour
{
    protected virtual EntityAnim.AnimState State { get; }
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
        anim.NotifyStateEnter(AnimId, State);
    }
}
