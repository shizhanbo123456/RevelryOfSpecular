using UnityEngine;

// 决策状态机骨架：只负责节流与状态切换，行为由子类写入请求字段后交给行为层
public abstract class AiDecisionMachine
{
    // <= 0 表示每帧思考；子类按自身决策频率设置
    protected float ThinkInterval { get; set; }

    private AiDecisionState current;
    private float nextThinkTime;

    protected AiDecisionState Current => current;

    public virtual void Tick()
    {
        if (Time.time < nextThinkTime) return;
        nextThinkTime = Time.time + ThinkInterval;

        if (current == null) throw new System.Exception($"{GetType().Name} 未经 Initialize 就参与 Tick");

        OnThink(); // 跨状态共享的决策数据（如刷新目标）必须先于状态更新

        AiDecisionState next = current.Decide();
        if (next == null || next == current) return;

        SwitchTo(next);
        // 切换后立刻让新状态产出一份请求，否则本拍行为层拿不到新的期望行为
        current.Decide();
    }

    // 每次思考的钩子：子类在此准备所有状态都要用的数据
    protected virtual void OnThink() { }

    // 队友请求一起追击敌人（末日呼叫 / 集结召唤都走这里）：由当前状态决定是否接受
    public void RequestChase(EntityData target)
    {
        if (current == null || target == null) return;
        AiDecisionState next = current.OnAllyChaseRequest(target);
        if (next == null || next == current) return;
        SwitchTo(next);
    }

    protected void SwitchTo(AiDecisionState next)
    {
        if (next == null || next == current) return;
        if (current != null) current.Exit();
        current = next;
        current.Enter();
    }

    // staggerPhase ∈ [0,1)：按实体 id 错峰首帧，避免同类单位同帧集中决策
    protected void Initialize(AiDecisionState initialState, float staggerPhase)
    {
        current = initialState;
        nextThinkTime = Time.time + ThinkInterval * staggerPhase;
        current.Enter();
    }
}
