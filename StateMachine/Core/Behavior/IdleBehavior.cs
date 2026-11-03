// 通用静止：停步 + 可选朝向；不产生任何位移
public sealed class IdleBehavior : Behavior
{
    private readonly IBehaviorHost host;

    public IdleBehavior(IBehaviorHost host) { this.host = host; }

    public override BehaviorKind Kind => BehaviorKind.Idle;

    public override void Bind(in BehaviorRequest request)
    {
        host.Stop();
        host.FaceTarget(request.Target);
    }
}
