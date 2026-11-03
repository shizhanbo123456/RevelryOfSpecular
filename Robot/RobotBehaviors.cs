using UnityEngine;

// 行为层：仲裁决策层写下的请求并执行；动作未结束时把切换压住，何时切由本层决定
public partial class RobotAi
{
    private Behavior behavior;
    private BehaviorRequest request;
    private bool requestDirty;

    private sealed class IdleBehavior : Behavior
    {
        private readonly RobotAi robot;

        public IdleBehavior(RobotAi robot) { this.robot = robot; }

        public override BehaviorKind Kind => BehaviorKind.Idle;

        public override void Bind(in BehaviorRequest request)
        {
            robot.Owner.StopMoving();
            robot.FaceTarget(request.Target);
        }
    }

    private sealed class MoveBehavior : Behavior
    {
        private readonly RobotAi robot;

        public MoveBehavior(RobotAi robot) { this.robot = robot; }

        public override BehaviorKind Kind => BehaviorKind.Move;

        public override void Bind(in BehaviorRequest request)
        {
            // 目标连续算不出路：停住并交给决策层放弃，绝不朝不可达点直冲
            if (robot.Owner.NavUnreachable)
            {
                robot.Owner.StopMoving();
                robot.NotifyStuck();
                return;
            }

            robot.Owner.MoveTo(request.Point);
        }
    }

    private sealed class AttackBehavior : Behavior
    {
        private readonly RobotAi robot;

        public AttackBehavior(RobotAi robot) { this.robot = robot; }

        public override BehaviorKind Kind => BehaviorKind.Attack;

        // 攻击动作播完才允许切走：技能不能放一半就改去移动
        public override bool CanInterrupt
            => robot.Owner.anim == null || robot.Owner.anim.CurrentState != EntityAnim.AnimState.Attack;

        public override void Bind(in BehaviorRequest request)
        {
            robot.Owner.StopMoving();
            robot.FaceTarget(request.Target);
            robot.TryAttack(request.Target); // 栏内全没好就站着，不空放
        }

        public override void Exit() => robot.Owner.ClearRobotFacePoint();
    }

    #region//Local
    // 每帧跑：只比种类，只有请求被改写或真的切换时才落到执行层
    private void TickBehaviors()
    {
        if (request.Kind == BehaviorKind.None) return;

        if (request.Kind == behavior.Kind)
        {
            if (!requestDirty) return;
            requestDirty = false;
            behavior.Bind(request);
            return;
        }

        if (!behavior.CanInterrupt) return; // 动作没结束：请求先挂着

        behavior.Exit();
        behavior = BehaviorOf(request.Kind);
        behavior.Enter();
        requestDirty = false;
        behavior.Bind(request);
    }

    private Behavior BehaviorOf(BehaviorKind kind) => kind switch
    {
        BehaviorKind.Idle => idleBehavior,
        BehaviorKind.Move => moveBehavior,
        BehaviorKind.Attack => attackBehavior,
        _ => throw new System.ArgumentOutOfRangeException(nameof(kind), kind, "未定义的期望行为"),
    };

    protected void RequestIdle(EntityData faceTarget = null) => WriteRequest(BehaviorKind.Idle, default, faceTarget);

    protected void RequestMove(Vector3 point) => WriteRequest(BehaviorKind.Move, point, null);

    protected void RequestAttack(EntityData target) => WriteRequest(BehaviorKind.Attack, default, target);

    private void WriteRequest(BehaviorKind kind, Vector3 point, EntityData target)
    {
        request.Kind = kind;
        request.Point = point;
        request.Target = target;
        requestDirty = true;
    }
    #endregion
}
