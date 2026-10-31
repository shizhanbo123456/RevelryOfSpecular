// 状态基类：Enter 承担"进入即生效"的动作，Tick 返回下一个状态（null = 保持本状态）
public abstract class AiState
{
    public abstract void Enter();

    public abstract AiState Tick(float deltaTime);

    public virtual void Exit() { }
}
