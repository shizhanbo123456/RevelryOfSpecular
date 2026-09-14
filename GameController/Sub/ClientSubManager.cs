/// <summary>
/// 客户端子管理器基类（普通 C# 类，由 ClientLogicManager 创建并持有，不挂场景、不进 Tool）。
/// 拆子管理器是为了防止客户端总控本身膨胀（架构说明 GameController/Sub）。
/// </summary>
public abstract class ClientSubManager
{
    /// <summary>所属总控（取其它子管理器与场景管理器都走它）。</summary>
    protected ClientLogicManager logic;

    /// <summary>由 ClientLogicManager 在 Awake 中统一调用，注入总控并订阅事件。</summary>
    public virtual void Init(ClientLogicManager owner)
    {
        logic = owner;
        BindEvents();
    }

    /// <summary>
    /// 由 ClientLogicManager.OnDestroy 统一调用。
    /// EventManager 是静态的，子管理器随场景销毁却不解绑会让回调跨场景累积，故必须成对。
    /// </summary>
    public void Dispose()
    {
        UnbindEvents();
    }

    /// <summary>每帧推进（由 ClientLogicManager.Update 统一驱动）。</summary>
    public virtual void Tick(float deltaTime) { }

    /// <summary>订阅客户端事件。</summary>
    protected virtual void BindEvents() { }

    /// <summary>解绑 BindEvents 中订阅的全部事件，必须与订阅一一对应。</summary>
    protected virtual void UnbindEvents() { }
}
