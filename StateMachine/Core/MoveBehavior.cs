using UnityEngine;

// 通用移动：Target 非空跟随目标物体，否则走到静止坐标
public sealed class MoveBehavior : Behavior
{
    private readonly IBehaviorHost host;

    private Vector3 point;
    private EntityData target;

    public MoveBehavior(IBehaviorHost host) { this.host = host; }

    public override BehaviorKind Kind => BehaviorKind.Move;

    public override void Bind(in BehaviorRequest request)
    {
        point = request.Point;
        target = request.Target;

        // 目标连续算不出路：停住并交给决策层放弃，绝不朝不可达点直冲
        if (host.Owner.NavUnreachable)
        {
            host.Stop();
            host.NotifyStuck();
            return;
        }

        Issue();
    }

    // 跟随移动目标时每帧刷新目的地；静止坐标只在请求变化时下发一次
    public override void Tick(float deltaTime)
    {
        if (target != null) Issue();
    }

    private void Issue()
    {
        host.Owner.MoveTo(target != null ? target.transform.position : point);
    }
}
