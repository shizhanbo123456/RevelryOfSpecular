using UnityEngine;

/// <summary>
/// 状态机事件脚本（挂在 Animator 状态上，每个状态一个实例；运行时实例按 Animator 独立，互不串扰）。
/// Inspector 逐状态配置：
///   animId —— 该状态的唯一编号（随 SCEntityDisplayInfo.animId 网络同步，客户端据此定位状态）；
///   state  —— 状态大类（驱动 CurrentState / 霸体换算等逻辑）。
/// 进入状态（进入过渡的第一帧）时推送回 EntityAnim，状态追踪为纯推送制；
/// 子类可继续叠加帧事件逻辑（范例：AnimAttackEvent 的攻击帧触发）。
/// </summary>
public class AnimEvent : StateMachineBehaviour
{
    [SerializeField] private int animId = -1;
    [SerializeField] private EntityAnim.AnimState state = EntityAnim.AnimState.Motion;
    public int AnimId => animId;
    public EntityAnim.AnimState State => state;

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
        // Init 前状态机可能已开始评估，未初始化时忽略（首次进入由默认值兜底）
        if (initialized) anim.NotifyStateEnter(animId, state);
    }
}
