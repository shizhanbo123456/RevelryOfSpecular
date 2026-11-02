using UnityEngine;

// 行为层：仲裁决策层写下的请求并执行；动作未结束时把切换压住，何时切由本层决定
public partial class AiPlayerAi
{
    private AiBehavior behavior;
    private AiBehaviorRequest request;
    private bool requestDirty;

    private sealed class IdleBehavior : AiBehavior
    {
        private readonly AiPlayerAi ai;

        public IdleBehavior(AiPlayerAi ai) { this.ai = ai; }

        public override AiBehaviorKind Kind => AiBehaviorKind.Idle;

        public override void Bind(in AiBehaviorRequest request)
        {
            ai.Owner.StopMoving();
            ai.FaceTarget(request.Target);
        }
    }

    private sealed class MoveBehavior : AiBehavior
    {
        private readonly AiPlayerAi ai;

        public MoveBehavior(AiPlayerAi ai) { this.ai = ai; }

        public override AiBehaviorKind Kind => AiBehaviorKind.Move;

        public override void Bind(in AiBehaviorRequest request)
        {
            // 目标连续算不出路：停住并交给决策层放弃，绝不朝不可达点直冲
            if (ai.Owner.NavUnreachable)
            {
                ai.Owner.StopMoving();
                ai.NotifyStuck();
                return;
            }

            ai.Owner.MoveTo(request.Point);
        }
    }

    private sealed class AttackBehavior : AiBehavior
    {
        private readonly AiPlayerAi ai;

        public AttackBehavior(AiPlayerAi ai) { this.ai = ai; }

        public override AiBehaviorKind Kind => AiBehaviorKind.Attack;

        // 攻击动作播完才允许切走：技能不能放一半就改去移动
        public override bool CanInterrupt
            => ai.Owner.anim == null || ai.Owner.anim.CurrentState != EntityAnim.AnimState.Attack;

        public override void Bind(in AiBehaviorRequest request)
        {
            ai.Owner.StopMoving();
            ai.FaceTarget(request.Target);
            ai.TryAttack(request.Target); // 栏内全没好就站着，不空放
        }

        public override void Exit() => ai.Owner.ClearAiFacePoint();
    }

    #region//Local
    // 每帧跑：只比种类，只有请求被改写或真的切换时才落到执行层
    private void TickBehaviors()
    {
        if (request.Kind == AiBehaviorKind.None) return;

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

    private AiBehavior BehaviorOf(AiBehaviorKind kind) => kind switch
    {
        AiBehaviorKind.Idle => idleBehavior,
        AiBehaviorKind.Move => moveBehavior,
        AiBehaviorKind.Attack => attackBehavior,
        _ => throw new System.ArgumentOutOfRangeException(nameof(kind), kind, "未定义的期望行为"),
    };

    protected void RequestIdle(EntityData faceTarget = null) => WriteRequest(AiBehaviorKind.Idle, default, faceTarget);

    protected void RequestMove(Vector3 point) => WriteRequest(AiBehaviorKind.Move, point, null);

    protected void RequestAttack(EntityData target) => WriteRequest(AiBehaviorKind.Attack, default, target);

    private void WriteRequest(AiBehaviorKind kind, Vector3 point, EntityData target)
    {
        request.Kind = kind;
        request.Point = point;
        request.Target = target;
        requestDirty = true;
    }
    #endregion
}
