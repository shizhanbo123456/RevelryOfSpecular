using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 僵尸与精英僵尸（共用，差异在属性与技能表）。
/// 刷新产出（夜间进度、数量上限、出生点、外观变体、等级）是对局级调度，仍在 BattleManagerWorld。
/// 本类承载其 **AI 行为**：索敌、攻击/技能触发时机（见 TickAI）与移动（寻路见 MoveTo/Stop）。
/// </summary>
public class ZombieEntityData : EntityData
{
    /// <summary>到点判定距离（米）。</summary>
    private const float ArriveRadius = 0.4f;
    /// <summary>拐点判定距离（米）：离拐点这么近就换下一个。</summary>
    private const float CornerRadius = 0.3f;
    /// <summary>重算路径的间隔（秒）：目标不动时不必每帧跑一次寻路。</summary>
    private const float RepathInterval = 0.25f;
    /// <summary>NavMesh 吸附半径（米）：目标点取自地面物件、出生点也可能贴边，都可能略有偏差。</summary>
    private const float SampleRadius = 1f;

    /// <summary>技能表槽位约定（顺序见 Config.initial_skills，普通与精英僵尸同构）：0 爪击右 / 1 爪击左 / 2 嘶吼。</summary>
    private const int SlotClawRight = 0;
    private const int SlotClawLeft = 1;
    private const int SlotRoar = 2;

    private readonly NavMeshPath path = new(); // 复用同一个对象，避免每次重算都新建
    private Vector3[] corners;

    private Vector3 destination;
    private bool hasDestination;
    private float nextRepathTime;

    private float yaw;
    private float yawSpeed;
    private bool yawInitialized;

    #region//AI
    /// <summary>出生点：游荡的圆心。</summary>
    private Vector3 homePos;
    /// <summary>游荡目标点（hasWanderPoint = false 时未选定）。</summary>
    private Vector3 wanderPoint;
    private bool hasWanderPoint;
    /// <summary>游荡到点后的停顿截止时刻。</summary>
    private float wanderResumeTime;
    /// <summary>当前追击目标 id（0 = 无）。存 id 而非引用，避免持有已销毁实体。</summary>
    private ushort targetId;
    /// <summary>攻击档的转向目标 id（0 = 无）：无目的地时只转不走，贴身时保持朝向。</summary>
    private ushort faceTargetId;
    /// <summary>下次决策时刻（按 id 错峰）。</summary>
    private float nextDecideTime;
    /// <summary>下次爪击用左手？左右交替。</summary>
    private bool clawLeftNext;
    #endregion

    /// <summary>绕 Y 角速度（度/秒，随表现摘要下发客户端做包间推演）。</summary>
    public override float YawSpeed => yawSpeed;

    /// <summary>记录出生点（游荡圆心），并把首次决策时刻按 id 错开。</summary>
    public override void OnCreate(ushort id, EntityType type, int level, EntityCamp camp = EntityCamp.Neutral)
    {
        base.OnCreate(id, type, level, camp);
        homePos = transform.position;
        nextDecideTime = Time.time + Config.zombie_decide_interval * AIStaggerPhase;
    }

    /// <summary>
    /// AI 决策（服务器每帧遍历调用，内部按 Config.zombie_decide_interval 错峰）。
    /// 索敌后按距离分档：超出最大追击距离 → 放弃并游荡；近战范围内 → 只爪击；否则 → 追击，且距离远时边走边嘶吼。
    /// 转向与推进仍由 OnTickMove 每帧负责，本方法只决定"去哪、放什么"。
    /// </summary>
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

    /// <summary>
    /// 前往某处。只记目标，"往哪走"留到 OnTickMove 里算 —— 寻路只借 NavMesh 算方向，
    /// 移动仍走"移动输入 + 动画声明速度"这一条链路（见 ResolveMoveVelocity）。可反复调用来改目标。
    /// </summary>
    public void MoveTo(Vector3 dest)
    {
        // 目标点常取自地面物件（守护点/水晶/角色），可能略偏网格 → 先吸附
        if (NavMesh.SamplePosition(dest, out var hit, SampleRadius, NavMesh.AllAreas)) dest = hit.position;

        // 目标没挪窝就不重算路径：AI 逐帧拿最新坐标调 MoveTo 追人时，不至于每帧跑一次寻路
        Vector3 moved = dest - destination;
        moved.y = 0f;
        if (!hasDestination || moved.sqrMagnitude > CornerRadius * CornerRadius) nextRepathTime = 0f;

        destination = dest;
        hasDestination = true;
        EnsureYaw(); // 首次接管朝向 = 生成时的朝向
    }

    /// <summary>停止：清目标与移动输入，动画回 Idle。不直接写速度 —— 水平速度按地面摩擦自然衰减（见 ResolveMoveVelocity ②）。</summary>
    public void Stop()
    {
        hasDestination = false;
        corners = null;
        yawSpeed = 0f;
        SetMoveInput(Vector3.zero);
        anim?.Move(false);
    }

    /// <summary>逐帧推进：算方向 → 渐转朝向 → 写移动输入（速度由动画声明，本类不产出速度）。</summary>
    public override void OnTickMove(float deltaTime, bool canInput)
    {
        // 无目的地（攻击档或游荡停顿中）：只转向目标，不移动
        if (!hasDestination)
        {
            if (canInput) FaceTarget(deltaTime);
            else yawSpeed = 0f;
            return;
        }

        Vector3 to = destination - transform.position;
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
            anim?.Move(false);
            return;
        }

        Vector3 dir = ResolveDirection();
        if (dir.sqrMagnitude < 0.0001f) // 方向退化（理论上不该发生）：停住，不沿用上一帧的输入
        {
            SetMoveInput(Vector3.zero);
            anim?.Move(false);
            return;
        }

        // 服务器权威渐转（客户端按 YawSpeed 推演）
        float prevYaw = yaw;
        yaw = Mathf.MoveTowardsAngle(yaw, Quaternion.LookRotation(dir).eulerAngles.y, Config.move_turn_rate * deltaTime);
        yawSpeed = deltaTime > 0f ? Mathf.DeltaAngle(prevYaw, yaw) / deltaTime : 0f;
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        // 只喂"前进"：朝向已由上面的渐转负责。喂实际方向会在目标位于背后时触发"后退"语义
        // （前后声明的负号是给玩家输入的，见 ResolveMoveVelocity），表现为僵尸倒着走
        SetMoveInput(Vector3.forward);
        anim?.Move(true);
    }

    /// <summary>本帧的水平前进方向：沿 NavMesh 拐点走；算不出路径（未烘焙/脱离网格）时直线朝目标兜底，等它自己走回网格。</summary>
    private Vector3 ResolveDirection()
    {
        if (Time.time >= nextRepathTime)
        {
            nextRepathTime = Time.time + RepathInterval;
            bool ok = NavMesh.CalculatePath(transform.position, destination, NavMesh.AllAreas, path);
            corners = ok && path.corners.Length > 1 ? path.corners : null;
        }

        Vector3 target = destination;
        if (corners != null)
        {
            int i = 0; // 起点与走过的拐点都靠这个距离跳过
            while (i < corners.Length && FlatSqrDistance(corners[i]) <= CornerRadius * CornerRadius) i++;
            if (i < corners.Length) target = corners[i];
        }

        Vector3 dir = target - transform.position;
        dir.y = 0f;
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.zero;
    }

    #region AI 行为
    /// <summary>
    /// 取当前目标：已锁定者仍有效（存活且在索敌范围内）就继续用 —— 同优先级不换目标，避免来回横跳；
    /// 否则取索敌范围内最近者。敌对阵营由 EntityCampUtil.HostileOf(本阵营) 算出（僵尸只敌视进攻方），此处不写死掩码。
    /// </summary>
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

    /// <summary>索敌半径：优先取属性可见距离；再夹到最大追击距离内，避免"刚索敌就要放弃"的空转。</summary>
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

    /// <summary>无目标时的游荡：出生点附近随机取点走过去，到点原地停 Config.zombie_wander_pause 秒再取下一个。</summary>
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

    /// <summary>爪击：左右交替；一次不成试另一只（两者同 CD 时自然都失败）。</summary>
    private void TryClaw()
    {
        clawLeftNext = !clawLeftNext;
        int first = clawLeftNext ? SlotClawLeft : SlotClawRight;
        if (TryUseSlot(first)) return;
        TryUseSlot(clawLeftNext ? SlotClawRight : SlotClawLeft);
    }

    /// <summary>嘶吼（自身加攻）。仅在 TickAI 判定「距离超过 Config.zombie_roar_range」时调用。</summary>
    private void TryRoar() => TryUseSlot(SlotRoar);

    /// <summary>按技能表槽位释放（顺序见类顶部常量）。</summary>
    private bool TryUseSlot(int slot)
    {
        if (skillController == null) return false;
        return skillController.TryUseSkill(skillController.GetSkillIdAt(slot));
    }

    /// <summary>原地转向攻击目标（只转不走）；目标已失效则清空并停转。</summary>
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
    /// <summary>首次接管朝向时以实体当前朝向为起点，避免从 0 度开始转。</summary>
    private void EnsureYaw()
    {
        if (yawInitialized) return;
        yaw = transform.eulerAngles.y;
        yawInitialized = true;
    }

    /// <summary>水平距离（米）。</summary>
    private float FlatDistance(Vector3 point) => Mathf.Sqrt(FlatSqrDistance(point));

    /// <summary>水平距离平方（寻路只关心水平面）。</summary>
    private float FlatSqrDistance(Vector3 point)
    {
        Vector3 d = point - transform.position;
        d.y = 0f;
        return d.sqrMagnitude;
    }
    #endregion
}
