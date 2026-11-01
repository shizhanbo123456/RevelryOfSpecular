// 状态基类：Enter 承担"进入即生效"的动作，Tick 返回下一个状态（null = 保持本状态）
public abstract class AiState
{
    public abstract void Enter();

    public abstract AiState Tick(float deltaTime);

    public virtual void Exit() { }

    // 是否拒绝任务类消息（战斗态与危险躲避为 true，收到只写任务槽）
    public virtual bool BlocksTaskMessages => false;

    // 队友请求一起追击某个敌人：返回要切换到的状态，null = 不接受
    public virtual AiState OnAllyChaseRequest(EntityData target) => null;

    // 状态名（上报状况用）
    public virtual string Name => GetType().Name;

    // 上报状态：协议只传枚举值，聊天文本由客户端本地拼
    public virtual Ros.Transport.AiReportState ReportState => Ros.Transport.AiReportState.None;

    // 目的地走不到（一段时间没位移）：状态自行决定放弃当前目标或重选点位
    public virtual void OnStuck() { }
}
