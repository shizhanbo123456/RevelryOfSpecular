using UnityEngine;

// 行为仲裁：每帧把决策层的请求落到行为上；动作没播完就压住切换
public sealed class BehaviorArbiter
{
    private readonly Behavior idleBehavior;
    private readonly Behavior moveBehavior;
    private readonly Behavior attackBehavior;

    private Behavior current;
    private BehaviorRequest request;
    private bool requestDirty;

    public BehaviorArbiter(IBehaviorHost host)
    {
        idleBehavior = new IdleBehavior(host);
        moveBehavior = new MoveBehavior(host);
        attackBehavior = new AttackBehavior(host);
        current = idleBehavior;
        current.Enter();
    }

    public Behavior Current => current;

    public void RequestIdle(EntityData faceTarget = null) => Write(BehaviorKind.Idle, default, faceTarget);

    // 走到静止坐标
    public void RequestMove(Vector3 point) => Write(BehaviorKind.Move, point, null);

    // 跟随目标物体（目标会动，目的地每帧刷新）
    public void RequestChase(EntityData target) => Write(BehaviorKind.Move, default, target);

    public void RequestAttack(EntityData target) => Write(BehaviorKind.Attack, default, target);

    public void Tick(float deltaTime)
    {
        if (request.Kind != BehaviorKind.None)
        {
            if (request.Kind == current.Kind)
            {
                if (requestDirty)
                {
                    requestDirty = false;
                    current.Bind(request);
                }
            }
            else if (current.CanInterrupt)
            {
                current.Exit();
                current = BehaviorOf(request.Kind);
                current.Enter();
                requestDirty = false;
                current.Bind(request);
            }
        }

        current.Tick(deltaTime);
    }

    // 清掉请求：决策层本拍没意见，行为层保持在当前动作
    public void ClearRequest() => request.Kind = BehaviorKind.None;

    private void Write(BehaviorKind kind, Vector3 point, EntityData target)
    {
        request.Kind = kind;
        request.Point = point;
        request.Target = target;
        requestDirty = true;
    }

    private Behavior BehaviorOf(BehaviorKind kind) => kind switch
    {
        BehaviorKind.Idle => idleBehavior,
        BehaviorKind.Move => moveBehavior,
        BehaviorKind.Attack => attackBehavior,
        _ => throw new System.ArgumentOutOfRangeException(nameof(kind), kind, "未定义的期望行为"),
    };
}
