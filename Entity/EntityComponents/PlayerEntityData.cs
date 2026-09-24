using Ros.Transport;
using UnityEngine;

/// <summary>
/// 玩家角色实体（进攻方与防守方共用同一个子类）：承载**操控**——把网络输入翻译成意图。
/// 攻守差异由技能池与防守方被动承担，不必拆成两个子类。
/// 复活流程不在这里：死亡即销毁实体，复活状态无处置身，仍由 BattleManager 按 clientId 持有。
/// </summary>
public class PlayerEntityData : EntityData
{
    /// <summary>移动输入状态（按下/抬起边沿维护；朝向由服务器渐转权威推进）。</summary>
    private class MoveState
    {
        public PlayerKey held;  // 当前按住的移动键
        public bool moving;
        public Vector2 dir;     // 原始按键输入（x = 左右横移，y = 前后）
        public float yaw;       // 角色朝向（度）
        public float yawSpeed;  // 绕 Y 角速度（度/秒，客户端推演用）
    }

    private MoveState moveState;

    /// <summary>翻滚冷却到期时刻（Time.time 基准 = 上次翻滚 + Config.roll_cd）。</summary>
    private float rollReadyTime;

    /// <summary>绕 Y 角速度（度/秒，随表现摘要下发客户端做包间推演）。</summary>
    public override float YawSpeed => moveState != null ? moveState.yawSpeed : 0f;

    /// <summary>记录移动输入（按下/抬起边沿 → 按住掩码；朝向仍由服务器渐转权威推进）。</summary>
    public override void RecordMoveInput(CSMoveInput move)
    {
        EnsureMoveState();
        moveState.held = (moveState.held | move.pressed) & ~move.released;

        float x = ((moveState.held & PlayerKey.D) != 0 ? 1f : 0f) - ((moveState.held & PlayerKey.A) != 0 ? 1f : 0f);
        float z = ((moveState.held & PlayerKey.W) != 0 ? 1f : 0f) - ((moveState.held & PlayerKey.S) != 0 ? 1f : 0f);
        moveState.dir = new Vector2(x, z).normalized;
        moveState.moving = x != 0f || z != 0f;

        // 输入只落到实体这一个字段（角色本地系方向与正负）；速度大小由动画模块声明的速度决定（见 ResolveMoveVelocity）
        SetMoveInput(new Vector3(moveState.dir.x, 0f, moveState.dir.y));

        // Run/Idle 随表现摘要同步给客户端
        anim?.Move(moveState.moving);
    }

    /// <summary>记录动作输入（攻击/跳跃/滑铲/技能槽，均为按下边沿）。</summary>
    public override void RecordActionInput(CSActionInput action)
    {
        bool moving = moveState != null && moveState.moving;

        // 跳跃键：移动中且翻滚不在冷却 → 优先翻滚；否则（未移动 / 冷却中）普通跳跃
        if ((action.pressed & PlayerKey.K) != 0)
        {
            if (moving && Time.time >= rollReadyTime)
            {
                rollReadyTime = Time.time + Config.roll_cd;
                anim?.Roll();
            }
            else
            {
                Jump();
            }
        }
        // 滑铲：只切进滑铲状态，持续多久由动画模块自己决定（外部不控时长）
        if ((action.pressed & PlayerKey.LShift) != 0)
        {
            anim?.DoSlide();
        }
        // 空手攻击走技能释放链路（策划案 12 章）：静止 = 原地砸击，移动 = 随机左右拳
        if ((action.pressed & PlayerKey.J) != 0)
        {
            int meleeSkill = moving
                ? (Random.Range(0, 2) == 0 ? Config.unarmed_punch_left : Config.unarmed_punch_right)
                : Config.unarmed_attack_smash;
            skillController.TryUseSkill(meleeSkill);
        }

        // 技能槽：键位 → 槽位下标（技能 id 由服务器权威决定）
        for (int i = 0; i < Config.skill_slot_player_keys.Length; i++)
        {
            if ((action.pressed & Config.skill_slot_player_keys[i]) == 0) continue;
            UseSkillSlot(i);
            break;
        }
    }

    /// <summary>
    /// 普通跳跃：把起跳的竖直初速度交给动画通道声明（EntityAnim.SetVelocityVertical → EntityData 接收后落到刚体），
    /// 之后交给重力。**这里不直接写刚体、也不写 InAir** —— 落地/空中由 EntityData.UpdateGrounded 的物理检测决定。
    /// </summary>
    private void Jump()
    {
        anim?.DoJump();
    }

    /// <summary>技能槽直触：槽位下标 → 服务器权威技能 id（CD/库存/强控校验在 TryUseSkill 内）。</summary>
    public void UseSkillSlot(int slot)
    {
        if (skillController == null) return;
        var ids = skillController.GetSkillIds();
        if (slot < 0 || slot >= ids.Count || ids[slot] < 0) return;

        skillController.SelectIndex(slot); // 选中下标供 UI 高亮
        skillController.TryUseSkill(ids[slot]);
    }

    /// <summary>
    /// 每帧朝向与移动推进。真人由输入驱动（前后 + 左右同按时逐渐偏向横移侧，yaw 正 = 右转）；
    /// AI 玩家走 <see cref="TickAiMove"/>：朝向与前进都由 AI 指定。
    /// </summary>
    public override void OnTickMove(float deltaTime, bool canInput)
    {
        EnsureMoveState();
        if (moveState == null) return;

        if (aiControlled)
        {
            TickAiMove(deltaTime, canInput);
            transform.rotation = Quaternion.Euler(0f, moveState.yaw, 0f);
            return;
        }

        if (canInput && moveState.moving && MotionCanMove)
        {
            float yawDelta = 0f;
            if (Mathf.Abs(moveState.dir.x) > 0.01f && Mathf.Abs(moveState.dir.y) > 0.01f)
            {
                yawDelta = Config.move_turn_rate * Mathf.Sign(moveState.dir.x) * deltaTime;
                moveState.yaw += yawDelta;
            }
            moveState.yawSpeed = deltaTime > 0f ? yawDelta / deltaTime : 0f;
        }
        else
        {
            moveState.yawSpeed = 0f;
        }
        transform.rotation = Quaternion.Euler(0f, moveState.yaw, 0f);
    }

    #region AI 驱动入口（由 PlayerAiController 调用；真人玩家不走这里）
    /// <summary>AI 决策器（仅 AI 玩家创建；aiControlled 由服务器在生成实体后置位）。</summary>
    private PlayerAiController ai;

    /// <summary>AI 决策（BattleManager.UpdateAI 每帧调用）。真人玩家由网络输入驱动，直接返回。</summary>
    public override void TickAI()
    {
        if (!aiControlled) return;
        ai ??= new PlayerAiController(this);
        ai.Tick();
    }

    /// <summary>AI 期望朝向的世界坐标点（aiFaceSet = false 时无效）。</summary>
    private Vector3 aiFacePoint;
    private bool aiFaceSet;

    /// <summary>AI 专用：设置期望朝向的世界坐标点（每帧由 PlayerAiController 更新）。</summary>
    public void SetAiFacePoint(Vector3 worldPoint)
    {
        aiFacePoint = worldPoint;
        aiFaceSet = true;
    }

    /// <summary>AI 专用：清除期望朝向（站定时不再转向）。</summary>
    public void ClearAiFacePoint() => aiFaceSet = false;

    /// <summary>
    /// AI 每帧推进：有目的地就沿 NavMesh 前进，否则站定、只转向 AI 指定的朝向点。
    /// 与僵尸走同一条链路 —— 只喂"前进方向 + 移动开关"，**不产生速度**（速度由动画声明，见 ResolveMoveVelocity）。
    /// </summary>
    private void TickAiMove(float deltaTime, bool canInput)
    {
        // 强控/位移锁输入期间不推进：清掉移动输入，但保留 AI 的朝向意图（解除后立刻恢复）
        if (!canInput || !MotionCanMove)
        {
            SetMoveInput(Vector3.zero);
            anim?.Move(false);
            moveState.yawSpeed = 0f;
            return;
        }

        Vector3 dir = ResolveNavDirection();
        if (dir.sqrMagnitude > 0.0001f)
        {
            SteerTo(transform.position + dir, deltaTime); // 朝行进方向渐转
            SetMoveInput(Vector3.forward);
            anim?.Move(true);
            return;
        }

        // 无目的地（已赶到攻击距离内）：站定，只朝 AI 指定的目标转
        SetMoveInput(Vector3.zero);
        anim?.Move(false);
        if (aiFaceSet) SteerTo(aiFacePoint, deltaTime);
        else moveState.yawSpeed = 0f;
    }

    /// <summary>朝世界坐标点渐转（AI 与输入共用同一条转向推进，角速度见 Config.move_turn_rate）。</summary>
    private void SteerTo(Vector3 worldPoint, float deltaTime)
    {
        Vector3 to = worldPoint - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude < 0.0001f)
        {
            moveState.yawSpeed = 0f;
            return;
        }

        float prevYaw = moveState.yaw;
        moveState.yaw = Mathf.MoveTowardsAngle(moveState.yaw,
            Quaternion.LookRotation(to).eulerAngles.y, Config.move_turn_rate * deltaTime);
        moveState.yawSpeed = deltaTime > 0f ? Mathf.DeltaAngle(prevYaw, moveState.yaw) / deltaTime : 0f;
    }
    #endregion

    #region//Local
    /// <summary>确保移动输入状态已建立（真人首次输入、AI 首次推进各建一次），初始朝向 = 生成时的朝向。</summary>
    private void EnsureMoveState()
    {
        if (moveState != null) return;
        moveState = new MoveState { yaw = transform.eulerAngles.y };
    }
    #endregion
}
