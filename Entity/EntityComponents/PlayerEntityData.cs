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
        if (moveState == null)
        {
            moveState = new MoveState { yaw = transform.eulerAngles.y }; // 初始朝向 = 生成时的朝向
        }
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
        // 滑铲：进入滑铲状态，持续时间后结束
        if ((action.pressed & PlayerKey.LShift) != 0)
        {
            anim?.DoSlide();
            var weak = this;
            GenericTimer.AddTimer(0, Config.slide_duration, _ =>
            {
                if (weak != null) weak.anim?.EndSlide();
            });
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
    /// 普通跳跃：只给刚体一次向上的初速度，之后交给重力。
    /// **不在这里写 InAir** —— 空中/落地由 EntityData.UpdateGrounded 的物理检测写入（按计时判定落地是错的）。
    /// </summary>
    private void Jump()
    {
        if (body == null) return;
        Vector3 velocity = body.velocity;
        velocity.y = Config.jump_speed;
        body.velocity = velocity;
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

    /// <summary>朝向推进：前后 + 左右同按时逐渐偏向横移侧（yaw 正 = 右转，负 = 左转）。</summary>
    public override void OnTickMove(float deltaTime, bool canInput)
    {
        if (moveState == null) return;

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
}
