// 决策状态基类：只产出"期望行为"，绝不直接驱动执行层
public abstract class DecisionState
{
    public virtual void Enter() { }

    public virtual void Exit() { }

    // 每拍调用一次；返回下一决策状态，null = 保持本状态
    public abstract DecisionState Decide();

    // 是否拒绝任务类消息（战斗态与危险躲避为 true，收到只写任务槽）
    public virtual bool BlocksTaskMessages => false;

    // 队友请求一起追击某个敌人：返回要切换到的状态，null = 不接受
    public virtual DecisionState OnAllyChaseRequest(EntityData target) => null;

    // 上报状态：协议只传枚举值，聊天文本由客户端本地拼
    public virtual Ros.Transport.EntityReportState ReportState => Ros.Transport.EntityReportState.None;

    // 目的地走不到（一段时间没位移）：状态自行决定放弃当前目标或重选点位
    public virtual void OnStuck() { }
}
