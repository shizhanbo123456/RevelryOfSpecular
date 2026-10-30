using Ros.Transport;
using UnityEngine;

public class PlayerEntityData : EntityData
{
    private class MoveState
    {
        public PlayerKey held;  // 当前按住的移动键
        public bool moving;     // 是否位移中（坦克式只认 W；俯视任意方向键都算）
        public Vector2 dir;     // x = 横向（-1 左 / +1 右），y = 前后（坦克式只用 +1，俯视含 -1）
        public float yaw;       // 角色朝向（度）
        public float yawSpeed;  // 绕 Y 角速度（度/秒，客户端推演用）
    }

    private MoveState moveState;

    private bool flightMode;
    private PlayerKey flyHeld; // 飞行垂直输入按住态（K 升 / Shift 降）

    private bool topDownMode; // 俯视相机模式：WASD 映射世界轴向，角色即时转向移动方向

    public override bool FlightMode => flightMode;

    // GM 飞行开关：仅真人玩家使用（与 AI 控制互斥，由数据保证）
    public void SetFlightMode(bool enabled)
    {
        if (flightMode == enabled) return;
        flightMode = enabled;
        if (enabled)
        {
            flyHeld = PlayerKey.None;
            SetGravityEnabled(false); // 关重力（UpdateGrounded 随之停用，每帧再由 TickVelocity 强制）
            grounded = true;          // 飞行视作贴地：全程 InAir=false，动画走地面状态
            if (anim != null) anim.InAir(false);
        }
        else
        {
            SetGravityEnabled(true); // 恢复重力，落地/悬空交回地面检测
        }
    }

    // 俯视模式：WASD 改为世界轴向四向（W=+Z / S=-Z / D=+X / A=-X），角色即时转向移动方向
    public void SetTopDown(bool enabled)
    {
        if (topDownMode == enabled) return;
        topDownMode = enabled;
        EnsureMoveState();
        ApplyMoveState(); // 按住态在新口径下重新解释，避免切模式当帧仍按旧语义移动
    }

    // 操作屏蔽 → 移动键位（前后=W，俯视下含 S；旋转=A/D；带 Press/Release 对，抹除需同时清 Release 位）
    private PlayerKey BlockMoveKeys(InputBlockOp ops)
    {
        PlayerKey k = PlayerKey.None;
        if ((ops & InputBlockOp.Forward) != 0) k |= topDownMode ? (PlayerKey.WPress | PlayerKey.SPress) : PlayerKey.WPress;
        if ((ops & InputBlockOp.Rotation) != 0) k |= PlayerKey.APress | PlayerKey.DPress;
        return k;
    }

    // 操作屏蔽 → 动作键位（跳跃=K，滑铲=LShift；单一位、边沿触发，只需抹去自身）
    // 攻击屏蔽不在这里：技能键已改成客户端直接上行技能 id，由 BattleManager.ReceiveSkillCast 拦截
    private PlayerKey BlockActionKeys(InputBlockOp ops)
    {
        PlayerKey k = PlayerKey.None;
        if ((ops & InputBlockOp.Jump) != 0) k |= PlayerKey.K;
        if ((ops & InputBlockOp.Slide) != 0) k |= PlayerKey.LShift;
        return k;
    }

    public override float YawSpeed => moveState != null ? moveState.yawSpeed : 0f;

    public override void RecordInput(CSPlayerInput input)
    {
        EnsureMoveState();

        // 输入阻断（操作级）：屏蔽期间对应键在服务器恒为抬起，按下/抬起请求均失效
        // 移动键带 Press/Release 对，需同时抹掉 Release 位；动作键单一位，仅抹自身
        var blockMove = BlockMoveKeys(inputBlockOperations);
        var blockAction = BlockActionKeys(inputBlockOperations);
        if (blockMove != PlayerKey.None || blockAction != PlayerKey.None)
        {
            uint clear = (uint)blockMove | ((uint)blockMove << 1) | (uint)blockAction;
            input.pressed = (PlayerKey)((uint)input.pressed & ~clear);
            if (moveState != null) { moveState.held &= ~blockMove; ApplyMoveState(); }
            flyHeld &= ~(PlayerKey.K | PlayerKey.LShift); // 屏蔽动作键期间飞行升降一并停止
        }

        // 飞行垂直输入：K/Shift 按住态维护（按下置位、抬起清位），仅飞行模式消费
        if ((input.pressed & PlayerKey.K) != 0) flyHeld |= PlayerKey.K;
        else if ((input.pressed & PlayerKey.KRelease) != 0) flyHeld &= ~PlayerKey.K;
        if ((input.pressed & PlayerKey.LShift) != 0) flyHeld |= PlayerKey.LShift;
        else if ((input.pressed & PlayerKey.LShiftRelease) != 0) flyHeld &= ~PlayerKey.LShift;

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

        // 跳跃键：K 只做跳跃；飞行模式下 K 转为上升长按，不跳
        if (!flightMode && (input.pressed & PlayerKey.K) != 0)
        {
            Jump();
            Debug.Log($"[输入处理] id={id} 跳跃键(K) 下沿 → 跳跃（移动中={moving}）");
        }
        // 滑铲：只切进滑铲状态，持续多久由动画模块自己决定（外部不控时长）；飞行模式禁用
        if (!flightMode && (input.pressed & PlayerKey.LShift) != 0)
        {
            if (anim != null) anim.DoSlide();
            Debug.Log($"[输入处理] id={id} 滑铲键(Shift) 下沿 → 滑铲");
        }
    }

    // 设置屏蔽的瞬间立即抬起对应操作涉及的键：存操作 + 复用抬起逻辑（不等下一帧读掩码）
    public override void SetInputBlock(InputBlockOp op)
    {
        base.SetInputBlock(op);
        SetInputReleased(op);
    }

    // 立刻抬起对应移动键（前后=W，旋转=A/D）：清掉服务器按住态并重算；动作键无按住态，屏蔽期间由 RecordInput 入口抹除
    public override void SetInputReleased(InputBlockOp op)
    {
        var released = BlockMoveKeys(op);
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
        float z = (moveState.held & PlayerKey.WPress) != 0 ? 1f : 0f;
        if (topDownMode) z -= (moveState.held & PlayerKey.SPress) != 0 ? 1f : 0f; // 俯视：S 参与移动
        moveState.dir = new Vector2(x, z);
        // 俯视：任意方向键都算移动；坦克式：只有 W 算移动，单独 A/D = 原地转向，S 弃用
        moveState.moving = topDownMode ? (x != 0f || z != 0f) : z != 0f;
        SetMoveInput(moveState.moving ? Vector3.forward : Vector3.zero);
        if (anim != null) anim.Move(moveState.moving);
    }

    private void Jump()
    {
        if (anim != null) anim.DoJump();
    }

    public override void OnTickMove(float deltaTime, bool canInput)
    {
        EnsureMoveState();
        if (moveState == null) return;

        // 飞行：垂直速度声明（长按 K 升 / 长按 Shift 降，固定速度）；水平移动与动画仍走原有地面链路
        if (FlightMode)
        {
            float v = 0f;
            if ((flyHeld & PlayerKey.K) != 0) v = Config.fly_vertical_speed;
            else if ((flyHeld & PlayerKey.LShift) != 0) v = -Config.fly_vertical_speed;
            SetVelocityVertical(v, VelocitySource.Flight);
        }

        if (aiControlled)
        {
            TickAiMove(deltaTime, canInput);
            transform.rotation = Quaternion.Euler(0f, moveState.yaw, 0f);
            return;
        }

        if (canInput && MotionCanMove)
        {
            if (topDownMode) TickTopDownYaw();
            else
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
        }
        else
        {
            moveState.yawSpeed = 0f;
        }
        transform.rotation = Quaternion.Euler(0f, moveState.yaw, 0f);
    }

    // 俯视：即时转向输入方向（W=+Z、D=+X、W+D 即 45°）；转向被屏蔽时保持朝向，位移仍沿当前朝向
    private void TickTopDownYaw()
    {
        moveState.yawSpeed = 0f;
        if (InputBlocked(InputBlockOp.Rotation)) return;
        if (Mathf.Abs(moveState.dir.x) > 0.01f || Mathf.Abs(moveState.dir.y) > 0.01f)
            moveState.yaw = Mathf.Atan2(moveState.dir.x, moveState.dir.y) * Mathf.Rad2Deg;
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

        bool blockForward = InputBlocked(InputBlockOp.Forward);
        bool blockRotation = InputBlocked(InputBlockOp.Rotation);

        Vector3 dir = ResolveNavDirection();
        if (dir.sqrMagnitude > 0.0001f)
        {
            if (!blockRotation) SteerTo(transform.position + dir, deltaTime); // 朝行进方向渐转
            SetMoveInput(blockForward ? Vector3.zero : Vector3.forward);
            if (anim != null) anim.Move(!blockForward);
            return;
        }

        // 无目的地（已赶到攻击距离内）：站定，只朝 AI 指定的目标转
        SetMoveInput(Vector3.zero);
        if (anim != null) anim.Move(false);
        if (aiFaceSet && !blockRotation) SteerTo(aiFacePoint, deltaTime);
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
