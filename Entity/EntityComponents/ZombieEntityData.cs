using UnityEngine;

public class ZombieEntityData : EntityData
{
    // 到达判定：寻路终点与游荡取点共用
    public const float ArriveRadius = 0.4f;

    private ZombieStateMachine stateMachine;

    private float yaw;
    private float yawSpeed;
    private bool yawInitialized;

    private ushort faceTargetId;

    public override float YawSpeed => yawSpeed;

    public override void OnCreate(ushort id, EntityType type, int level, EntityCamp camp = EntityCamp.Neutral)
    {
        base.OnCreate(id, type, level, camp);
        stateMachine = new ZombieStateMachine(this);
    }

    public override void TickAuto()
    {
        stateMachine.Tick();
    }

    // 由状态机指定朝向目标：停下攻击时每帧朝它转
    public void SetFaceTarget(ushort targetEntityId) => faceTargetId = targetEntityId;

    public void Stop()
    {
        StopMoving();
        yawSpeed = 0f;
        SetMoveInput(Vector3.zero);
        if (anim != null) anim.Move(false);
    }

    public override void OnTickMove(float deltaTime, bool canInput)
    {
        bool blockForward = InputBlocked(InputBlockOp.Forward);
        bool blockRotation = InputBlocked(InputBlockOp.Rotation);

        // 无目的地（攻击档或游荡停顿中）：只转向目标，不移动
        if (!HasNavDestination)
        {
            if (canInput && !blockRotation) FaceTarget();
            else yawSpeed = 0f;
            return;
        }

        Vector3 to = NavDestination - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude <= ArriveRadius * ArriveRadius)
        {
            Stop();
            return;
        }

        // 强控（麻痹/冰冻/定身）与位移锁输入期间不推进，但保留目标，解除后继续走
        if (!canInput || !MotionCanMove)
        {
            yawSpeed = 0f;
            if (anim != null) anim.Move(false);
            return;
        }

        Vector3 dir = ResolveNavDirection();
        if (dir.sqrMagnitude < 0.0001f) // 方向退化（理论上不该发生）：停住，不沿用上一帧的输入
        {
            SetMoveInput(Vector3.zero);
            if (anim != null) anim.Move(false);
            return;
        }

        // 转向即时对准行进方向（不再渐转；客户端按同步的 yaw 绝对值对齐）
        if (!blockRotation) yaw = Quaternion.LookRotation(dir).eulerAngles.y;
        yawSpeed = 0f;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        // 只喂"前进"：朝向已由上面的即时转向负责。喂实际方向会在目标位于背后时触发"后退"语义
        SetMoveInput(blockForward ? Vector3.zero : Vector3.forward);
        if (anim != null) anim.Move(!blockForward);
    }

    #region//Local
    private void FaceTarget()
    {
        if (faceTargetId == 0)
        {
            yawSpeed = 0f;
            return;
        }
        if (!BattleManager.EntityContainer.Entities.TryGetObject(faceTargetId, out var target)
            || target == null || !target.Alive)
        {
            faceTargetId = 0;
            yawSpeed = 0f;
            return;
        }

        Vector3 to = target.transform.position - transform.position;
        to.y = 0f;
        // 目标几乎与自己重合时方向向量极短、逐帧乱跳，直接不转
        if (to.sqrMagnitude < 0.09f)
        {
            yawSpeed = 0f;
            return;
        }

        EnsureYaw();
        yaw = Quaternion.LookRotation(to).eulerAngles.y; // 即时对准，不渐转
        yawSpeed = 0f;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    private void EnsureYaw()
    {
        if (yawInitialized) return;
        yaw = transform.eulerAngles.y;
        yawInitialized = true;
    }
    #endregion
}
