// 行为层基类：只把请求落到执行层，不做任何战术判断
public abstract class AiBehavior
{
    public abstract AiBehaviorKind Kind { get; }

    // false = 当前动作还没结束，禁止切走
    public virtual bool CanInterrupt => true;

    public virtual void Enter() { }

    public virtual void Exit() { }

    public abstract void Bind(in AiBehaviorRequest request);
}
