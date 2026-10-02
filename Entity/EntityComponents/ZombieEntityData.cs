using UnityEngine;

public class ZombieEntityData : EntityData
{
    private const float ArriveRadius = 0.4f;

    private const int SlotClawRight = 0;
    private const int SlotClawLeft = 1;
    private const int SlotRoar = 2;

    private float yaw;
    private float yawSpeed;
    private bool yawInitialized;

    #region//AI
    private Vector3 homePos;
    private Vector3 wanderPoint;
    private bool hasWanderPoint;
    private float wanderResumeTime;
    private ushort targetId;
    private ushort faceTargetId;
    private float nextDecideTime;
    private bool clawLeftNext;
    #endregion

    public override float YawSpeed => yawSpeed;

    public override void OnCreate(ushort id, EntityType type, int level, EntityCamp camp = EntityCamp.Neutral)
    {
        base.OnCreate(id, type, level, camp);
        homePos = transform.position;
        nextDecideTime = Time.time + Config.zombie_decide_interval * AIStaggerPhase;
    }

    public override void TickAI()
    {
        if (!Alive) return;
        if (Time.time < nextDecideTime) return;
        nextDecideTime = Time.time + Config.zombie_decide_interval;

        var target = AcquireTarget();
        if (target == null)
        {
            Wander();
            return;
        }

        float dist = FlatDistance(target.transform.position);
        if (dist > Config.zombie_max_chase_distance)
        {
            targetId = 0; // 目标拉开太远：放弃，回出生点游荡
            Wander();
            return;
        }

        if (dist <= Config.zombie_attack_range)
        {
            Stop();                   // 停在原地攻击
            faceTargetId = target.id;  // 停下后仍要能转到目标（见 OnTickMove 的无目的地分支）
            TryClaw();
            return;
        }

        faceTargetId = 0;
        MoveTo(target.transform.position); // 不在攻击范围内 → 追击
        if (dist > Config.zombie_roar_range) TryRoar(); // 距离近时直接近战更优，只在远处嘶吼
    }

    public void Stop()
    {
        StopMoving();
        yawSpeed = 0f;
        SetMoveInput(Vector3.zero);
        if (anim != null) anim.Move(false);
    }

    public override void OnTickMove(float deltaTime, bool canInput)
    {
        // 无目的地（攻击档或游荡停顿中）：只转向目标，不移动
        if (!HasNavDestination)
        {
            if (canInput) FaceTarget(deltaTime);
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

        // 强控（麻痹/冰冻/定身）与位移锁输入期间不推进，但**保留目标**，解除后继续走
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

        // 服务器权威渐转（客户端按 YawSpeed 推演）
        float prevYaw = yaw;
        yaw = Mathf.MoveTowardsAngle(yaw, Quaternion.LookRotation(dir).eulerAngles.y, Config.move_turn_rate * deltaTime);
        yawSpeed = deltaTime > 0f ? Mathf.DeltaAngle(prevYaw, yaw) / deltaTime : 0f;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        // 只喂"前进"：朝向已由上面的渐转负责。喂实际方向会在目标位于背后时触发"后退"语义
        // （前后声明的负号是给玩家输入的，见 TickVelocity），表现为僵尸倒着走
        SetMoveInput(Vector3.forward);
        if (anim != null) anim.Move(true);
    }

    #region AI 行为
    private EntityData AcquireTarget()
    {
        float range = AcquireRange;
        if (targetId != 0 && BattleManager.EntityContainer.Entities.TryGetObject(targetId, out var locked))
        {
            if (locked != null && locked.Alive && FlatDistance(locked.transform.position) <= range) return locked;
        }

        var nearest = BattleManager.EntityContainer.GetNearestEnemy(this, range);
        targetId = nearest != null ? nearest.id : (ushort)0;
        return nearest;
    }

    private float AcquireRange
    {
        get
        {
            float view = floatingAttribute != null && floatingAttribute.viewDistance > 0f
                ? floatingAttribute.viewDistance
                : Config.zombie_acquire_range;
            return Mathf.Min(view, Config.zombie_max_chase_distance);
        }
    }

    private void Wander()
    {
        faceTargetId = 0; // 已无攻击目标，别再朝着旧目标转
        if (hasWanderPoint)
        {
            if (FlatDistance(wanderPoint) > ArriveRadius) return; // 还在路上
            hasWanderPoint = false;
            Stop();
            wanderResumeTime = Time.time + Config.zombie_wander_pause;
            return;
        }
        if (Time.time < wanderResumeTime) return; // 到点后的停顿
        Vector2 offset = Random.insideUnitCircle * Config.zombie_wander_radius;
        wanderPoint = homePos + new Vector3(offset.x, 0f, offset.y);
        hasWanderPoint = true;
        MoveTo(wanderPoint);
    }

    private void TryClaw()
    {
        clawLeftNext = !clawLeftNext;
        int first = clawLeftNext ? SlotClawLeft : SlotClawRight;
        if (TryUseSlot(first)) return;
        TryUseSlot(clawLeftNext ? SlotClawRight : SlotClawLeft);
    }

    private void TryRoar() => TryUseSlot(SlotRoar);

    private bool TryUseSlot(int slot)
    {
        if (skillController == null) return false;
        return skillController.TryUseSkill(skillController.GetSkillIdAt(slot));
    }

    private void FaceTarget(float deltaTime)
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
        if (to.sqrMagnitude < 0.0001f)
        {
            yawSpeed = 0f;
            return;
        }

        EnsureYaw();
        float prevYaw = yaw;
        yaw = Mathf.MoveTowardsAngle(yaw, Quaternion.LookRotation(to).eulerAngles.y, Config.move_turn_rate * deltaTime);
        yawSpeed = deltaTime > 0f ? Mathf.DeltaAngle(prevYaw, yaw) / deltaTime : 0f;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }
    #endregion

    #region//Local
    private void EnsureYaw()
    {
        if (yawInitialized) return;
        yaw = transform.eulerAngles.y;
        yawInitialized = true;
    }
    #endregion
}
