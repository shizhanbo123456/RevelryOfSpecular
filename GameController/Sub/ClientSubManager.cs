public abstract class ClientSubManager
{
    protected ClientLogicManager logic;

    public virtual void Init(ClientLogicManager owner)
    {
        logic = owner;
        BindEvents();
    }

    public void Dispose()
    {
        UnbindEvents();
    }

    public virtual void Tick(float deltaTime) { }

    protected virtual void BindEvents() { }

    protected virtual void UnbindEvents() { }
}
