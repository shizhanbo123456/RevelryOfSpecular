using Ros.Transport;
using UnityEngine;

public class PlayerEntityData : EntityData
{
    private class MoveState
    {
        public PlayerKey held;  // 当前按住的移动键
        public bool moving;     // 是否位移中（只有 W 算移动；A/D 单独按住 = 原地转向）
        public Vector2 dir;     // x = 转向（-1 左 / +1 右），y = W 按住标志（1/0）；S 弃用
        public float yaw;       // 角色朝向（度）
        public float yawSpeed;  // 绕 Y 角速度（度/秒，客户端推演用）
    }

    private MoveState moveState;

    // 操作屏蔽 → 实际按键位（前后=W，旋转=A/D）
    private PlayerKey BlockKeys(InputBlockOp ops)
    {
        PlayerKey k = PlayerKey.None;
        if ((ops & InputBlockOp.Forward) != 0) k |= PlayerKey.WPress;
        if ((ops & InputBlockOp.Rotation) != 0) k |= PlayerKey.APress | PlayerKey.DPress;
        return k;
    }

    public override float YawSpeed => moveState != null ? moveState.yawSpeed : 0f;

    public override void RecordInput(CSPlayerInput input)
    {
        EnsureMoveState();

        // 输入阻断（操作级：前后=W，旋转=A/D）：屏蔽期间对应键在服务器恒为抬起，按下/抬起请求均失效
        var block = BlockKeys(inputBlockOperations);
        if (block != PlayerKey.None)
        {
            input.pressed = (PlayerKey)((uint)input.pressed & ~((uint)block | ((uint)block << 1)));
            if (moveState != null) { moveState.held &= ~block; ApplyMoveState(); }
        }

        // 移动（坦克式）：W = 前进；A/D = 转向（W 按住 = 边走边转，未按 = 原地转）；S 弃用不参与方向计算。
        // 按下位并入按住掩码，抬起位清除对应按住位（右移一位，位布局刻意相邻），随即重算方向
        var press = input.pressed & PlayerKey.MovePressMask;
        var release = (PlayerKey)((uint)(input.pressed & PlayerKey.MoveReleaseMask) >> 1);
        if (press != 0 || release != 0)
        {
            moveState.held = (moveState.held | press) & ~release;
            ApplyMoveState();
        }

        bool moving = moveState.moving;

        // 跳跃键：普通跳跃（翻滚动作已整体移除，K 只做跳跃）
        if ((input.pressed & PlayerKey.K) != 0)
        {
            Jump();
            Debug.Log($"[输入处理] id={id} 跳跃键(K) 下沿 → 跳跃（移动中={moving}）");
        }
        // 滑铲：只切进滑铲状态，持续多久由动画模块自己决定（外部不控时长）
        if ((input.pressed & PlayerKey.LShift) != 0)
        {
            if (anim != null) anim.DoSlide();
            Debug.Log($"[输入处理] id={id} 滑铲键(Shift) 下沿 → 滑铲");
        }
        // 空手攻击走技能释放链路（策划案 12 章）：静止 = 原地砸击，移动 = 随机左右拳
        if ((input.pressed & PlayerKey.J) != 0)
        {
            int meleeSkill = moving
                ? (Random.Range(0, 2) == 0 ? Config.unarmed_punch_left : Config.unarmed_punch_right)
                : Config.unarmed_attack_smash;
            skillController.TryUseSkill(meleeSkill);
        }

        // 技能槽：键位 → 槽位下标（技能 id 由服务器权威决定）
        for (int i = 0; i < Config.skill_slot_player_keys.Length; i++)
        {
            if ((input.pressed & Config.skill_slot_player_keys[i]) == 0) continue;
            UseSkillSlot(i);
            break;
        }
    }

    // 设置屏蔽的瞬间立即抬起对应操作涉及的键：存操作 + 复用抬起逻辑（不等下一帧读掩码）
    public override void SetInputBlock(InputBlockOp op)
    {
        base.SetInputBlock(op);
        SetInputReleased(op);
    }

    // 立即抬起指定操作涉及的键（前后=W，旋转=A/D）：清掉服务器按住态并重算，不写操作掩码
    public override void SetInputReleased(InputBlockOp op)
    {
        var released = BlockKeys(op);
        if (released != PlayerKey.None && moveState != null)
        {
            moveState.held &= ~released;
            ApplyMoveState();
        }
    }

    // 按住态 → 方向/位移/表现，屏蔽抬起或清空按住态后重算都用同一份逻辑
    private void ApplyMoveState()
    {
        float x = ((moveState.held & PlayerKey.DPress) != 0 ? 1f : 0f) - ((moveState.held & PlayerKey.APress) != 0 ? 1f : 0f);
        float z = (moveState.held & PlayerKey.WPress) != 0 ? 1f : 0f; // S 弃用：前后方向只认 W
        moveState.dir = new Vector2(x, z);
        moveState.moving = z != 0f; // 只有 W 算移动；单独 A/D = 原地转向，不位移
        SetMoveInput(moveState.moving ? Vector3.forward : Vector3.zero);
        if (anim != null) anim.Move(moveState.moving);
    }

    private void Jump()
    {
        if (anim != null) anim.DoJump();
    }

    public void UseSkillSlot(int slot)
    {
        if (skillController == null)
        {
            Debug.Log($"[输入处理] id={id} 技能槽{slot} → 技能控制器缺失，无法释放");
            return;
        }
        var ids = skillController.GetSkillIds();
        if (slot < 0 || slot >= ids.Count || ids[slot] < 0)
        {
            Debug.Log($"[输入处理] id={id} 技能槽{slot} → 该槽位没有技能（技能表 [{string.Join(",", ids)}]）");
            return;
        }

        skillController.SelectIndex(slot); // 选中下标供 UI 高亮
        bool ok = skillController.TryUseSkill(ids[slot]);
        string key = slot < Config.skill_slot_keys.Length ? Config.skill_slot_keys[slot].ToString() : "?";
        Debug.Log($"[输入处理] id={id} 技能槽{slot}（键 {key}）→ 技能 {ids[slot]}，结果={(ok ? "释放成功" : "被拒绝：" + skillController.DescribeUseFailure(ids[slot]))}");
    }

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

        if (canInput && MotionCanMove)
        {
            // A/D 按住即转向，不再要求同时在移动（单独 A/D = 原地转）
            float yawDelta = 0f;
            if (Mathf.Abs(moveState.dir.x) > 0.01f)
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
    private PlayerAiController ai;

    public override void TickAI()
    {
        if (!aiControlled) return;
        ai ??= new PlayerAiController(this);
        ai.Tick();
    }

    private Vector3 aiFacePoint;
    private bool aiFaceSet;

    public void SetAiFacePoint(Vector3 worldPoint)
    {
        aiFacePoint = worldPoint;
        aiFaceSet = true;
    }

    public void ClearAiFacePoint() => aiFaceSet = false;

    private void TickAiMove(float deltaTime, bool canInput)
    {
        // 强控/位移锁输入期间不推进：清掉移动输入，但保留 AI 的朝向意图（解除后立刻恢复）
        if (!canInput || !MotionCanMove)
        {
            SetMoveInput(Vector3.zero);
            if (anim != null) anim.Move(false);
            moveState.yawSpeed = 0f;
            return;
        }

        Vector3 dir = ResolveNavDirection();
        if (dir.sqrMagnitude > 0.0001f)
        {
            SteerTo(transform.position + dir, deltaTime); // 朝行进方向渐转
            SetMoveInput(Vector3.forward);
            if (anim != null) anim.Move(true);
            return;
        }

        // 无目的地（已赶到攻击距离内）：站定，只朝 AI 指定的目标转
        SetMoveInput(Vector3.zero);
        if (anim != null) anim.Move(false);
        if (aiFaceSet) SteerTo(aiFacePoint, deltaTime);
        else moveState.yawSpeed = 0f;
    }

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
    private void EnsureMoveState()
    {
        if (moveState != null) return;
        moveState = new MoveState { yaw = transform.eulerAngles.y };
    }
    #endregion
}
