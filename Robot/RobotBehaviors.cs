using UnityEngine;

// 行为层：请求转给公共仲裁器，人机不再自己实现 Idle/Move/Attack
public partial class RobotAi
{
    #region//Local
    protected void RequestIdle(EntityData faceTarget = null) => arbiter.RequestIdle(faceTarget);

    protected void RequestMove(Vector3 point) => arbiter.RequestMove(point);

    protected void RequestAttack(EntityData target) => arbiter.RequestAttack(target);
    #endregion

    #region//IBehaviorHost
    EntityData IBehaviorHost.Owner => Owner;

    void IBehaviorHost.Stop() => Owner.StopMoving();

    void IBehaviorHost.FaceTarget(EntityData target) => FaceTarget(target);

    bool IBehaviorHost.TryAttack(EntityData target) => TryAttack(target);

    bool IBehaviorHost.IsActing => Owner.anim != null && Owner.anim.CurrentState == EntityAnim.AnimState.Attack;

    void IBehaviorHost.ClearFace() => Owner.ClearRobotFacePoint();

    void IBehaviorHost.NotifyStuck() => NotifyStuck();
    #endregion
}
