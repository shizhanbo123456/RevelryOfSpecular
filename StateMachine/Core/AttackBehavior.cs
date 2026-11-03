// 通用攻击：停步 + 朝向 + 出手；动作播完前不允许切走
public sealed class AttackBehavior : Behavior
{
    private readonly IBehaviorHost host;

    public AttackBehavior(IBehaviorHost host) { this.host = host; }

    public override BehaviorKind Kind => BehaviorKind.Attack;

    public override bool CanInterrupt => !host.IsActing;

    public override void Bind(in BehaviorRequest request)
    {
        host.Stop();
        host.FaceTarget(request.Target);
        host.TryAttack(request.Target);
    }

    public override void Exit() => host.ClearFace();
}
