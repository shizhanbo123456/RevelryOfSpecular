using UnityEngine;

// AI 状态机骨架：只负责节流与状态切换，具体行为全部在子类和各状态里
public abstract class AiStateMachine
{
    // <= 0 表示每帧思考；子类按自身决策频率设置
    protected float ThinkInterval { get; set; }

    private AiState current;
    private float nextThinkTime;

    protected AiState Current => current;

    public virtual void Tick()
    {
        if (Time.time < nextThinkTime) return;
        nextThinkTime = Time.time + ThinkInterval;

        if (current == null) throw new System.Exception($"{GetType().Name} 未经 Initialize 就参与 Tick");

        OnThink(); // 跨状态共享的决策数据（如刷新目标）必须先于状态更新

        AiState next = current.Tick(Time.deltaTime);
        if (next == null || next == current) return;
        SwitchTo(next);
    }

    // 每次思考的钩子：子类在此准备所有状态都要用的数据
    protected virtual void OnThink() { }

    // 队友请求一起追击敌人（末日呼叫 / 集结召唤都走这里）：由当前状态决定是否接受
    public void RequestChase(EntityData target)
    {
        if (current == null || target == null) return;
        AiState next = current.OnAllyChaseRequest(target);
        if (next == null || next == current) return;
        SwitchTo(next);
    }

    protected void SwitchTo(AiState next)
    {
        if (next == null || next == current) return;
        if (current != null) current.Exit();
        current = next;
        current.Enter();
    }

    // staggerPhase ∈ [0,1)：按实体 id 错峰首帧，避免同类单位同帧集中决策
    protected void Initialize(AiState initialState, float staggerPhase)
    {
        current = initialState;
        nextThinkTime = Time.time + ThinkInterval * staggerPhase;
        current.Enter();
    }
}
