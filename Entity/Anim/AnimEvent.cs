using UnityEngine;

public class AnimEvent : StateMachineBehaviour
{
    protected virtual EntityAnim.AnimState State { get; }
    //用于传递给客户端识别动画片段
    public int AnimId { get; private set; } = -1;

    protected EntityAnim anim;
    protected EntityData data;
    protected bool main;
    protected bool initialized = false;

    public void Init(EntityAnim anim, EntityData data,bool mainAnim)//仅仅主动画机的片段会触发事件
    {
        this.anim = anim;
        this.data = data;
        main = mainAnim;
        initialized = true;
    }

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        base.OnStateEnter(animator, stateInfo, layerIndex);
        AnimId = stateInfo.fullPathHash;
        if (!main) return;
        anim.RegisterAnimEvent(this);
        anim.NotifyStateEnter(AnimId, State);
    }
}
