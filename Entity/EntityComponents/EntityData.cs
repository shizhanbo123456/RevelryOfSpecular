using System.Collections.Generic;
using Ros.Info;
using Ros.Transport;
using UnityEngine;

/// <summary>
/// 实体数据（初始化入口，所有实体组件的中心）。
/// 持有技能控制器、效果控制器、动画等组件的引用；受伤计算与死亡事件汇总于此。
/// 服务器实体只含组件模板（无图形），客户端实体含具体贴图模型表现（见架构说明）。
/// </summary>
public abstract class EntityData : MonoBehaviour
{
    /// <summary>实体 id（服务器分配）。</summary>
    [HideInInspector] public ushort id;

    /// <summary>实体类型。</summary>
    public EntityType type;

    /// <summary>等级（角色等级/僵尸等级等）。</summary>
    public int level = 1;

    /// <summary>当前阵营（运行时由服务器分配）。</summary>
    public EntityCamp camp;

    /// <summary>基础属性（配置）。</summary>
    public EntityAttribute baseAttribute;

    /// <summary>运行时属性（基础 + 效果/成长叠加）。</summary>
    public EntityAttribute floatingAttribute;

    /// <summary>效果（Buff）控制器。</summary>
    public EntityEffectController effectController;

    /// <summary>技能控制器。</summary>
    public EntitySkillController skillController;

    /// <summary>动画组件（OnCreate 一次性获取；非人形单位无动画时为 null）。</summary>
    public EntityAnim anim;

    /// <summary>
    /// 模型移速（米/秒，按 EntityAnimData.legHeight 换算；模型参数而非属性）。
    /// 仅作**退化速度**：动画还没通过 SetAnimSpeed 声明状态速度时用它，声明后以动画的速度为准。
    /// </summary>
    [HideInInspector] public float moveSpeed = Config.base_move_speed;

    /// <summary>当前位移效果（null = 无）；SetMotion 设置并调用 Enter，时间到由 OnUpdate 调用 Exit。</summary>
    [HideInInspector] public MotionBase motion;
    /// <summary>位移效果产出的当前速度（服务器权威移动逻辑中消费；无位移效果时为零）。</summary>
    [HideInInspector] public Vector3 motionVelocity;

    /// <summary>
    /// 刚体（可移动类别的权威速度载体，见 SetupBody 与 BattleManagerCombat.TickMovement）。
    /// 位移效果与输入移动都只产出速度、由它积分位置；非可移动类别为 null。
    /// </summary>
    [HideInInspector] public Rigidbody body;

    /// <summary>
    /// 移动输入方向（**角色本地系**：X = 右、Z = 前、Y 恒为 0）。
    /// 移动系统**只取它的方向**，速度大小来自动画声明的 <see cref="animSpeed"/> ——
    /// 输入是"要不要动"的开关，动画决定"动多快"。网络输入与 AI 都只写它。
    /// </summary>
    [HideInInspector] public Vector3 moveInput;

    /// <summary>
    /// 动画声明的移动速度（由动画模块通过 <see cref="SetAnimSpeed"/> 写入）。
    /// x = 水平速度，正数向前 / 负数向后：进入状态时设定、在该状态内持续生效，**每次传入都直接覆盖（传 0 即停）**；
    /// y = 垂直速度，**仅进入状态那一次生效**（起跳），之后交给重力。
    /// </summary>
    [HideInInspector] public Vector2 animSpeed;

    /// <summary>动画是否已声明过水平速度：未声明时移动退化为模型移速，保证动画还没接完时也能动。</summary>
    [HideInInspector] public bool animSpeedDeclared;

    /// <summary>当前手持武器（服务器权威）：释放技能时赋值，攻击动作结束清空；随实体摘要同步给客户端。</summary>
    [HideInInspector] public WeaponRef heldWeapon = WeaponRef.None;

    /// <summary>最近一次伤害来源（水晶掉武器归属判定等；死亡时保留供结算读取）。</summary>
    [HideInInspector] public EntityData lastAttacker;

    /// <summary>血条锚点。</summary>
    public Transform BarPos;

    /// <summary>本帧死亡实体（由 BattleManager 统一处理）。</summary>
    protected static readonly List<EntityData> KilledEntities = new();

    /// <summary>本帧死亡实体列表（只读）。</summary>
    public static IReadOnlyList<EntityData> KilledList => KilledEntities;

    /// <summary>清空本帧死亡列表（由 BattleManager 每帧处理后调用）。</summary>
    public static void ClearKilled() => KilledEntities.Clear();

    /// <summary>是否存活。</summary>
    public bool Alive => floatingAttribute != null && floatingAttribute.Alive;

    /// <summary>
    /// 初始化入口（实体创建时调用，注意调用顺序：先赋值基础数据，再初始化组件）。
    /// </summary>
    public virtual void OnCreate(ushort id, EntityType type, int level, EntityCamp camp = EntityCamp.Neutral)
    {
        this.id = id;
        this.type = type;
        this.level = level;
        this.camp = camp;
        baseAttribute = Tool.InfoManager != null ? Tool.InfoManager.GetAttribute(type, level) : new EntityAttribute();
        floatingAttribute = baseAttribute.Clone();
        effectController = new EntityEffectController();
        effectController.Init(this);
        skillController = new EntitySkillController();
        skillController.Init(this);

        anim = GetComponentInChildren<EntityAnim>();

        // 预制体/模板上的共用参数（动画类型、腿高移速）：服务端模板与客户端模型参数一致
        var animData = GetComponent<EntityAnimData>();
        if (animData == null) animData = GetComponentInChildren<EntityAnimData>();
        if (animData != null)
        {
            moveSpeed = animData.legHeight > 0f
                ? EntityAnimData.LegHeightToStandartRunSpeed(animData.legHeight)
                : Config.base_move_speed;
            anim?.SetType(animData.type);
        }
        else
        {
            moveSpeed = Config.base_move_speed;
        }

        // 动画初始化（一切动画控制统一走 EntityAnim）：激活 animator 引用与 AnimEvent 状态推送，
        // 服务器实体与客户端图形预制体都带 EntityAnim/Animator（差异只在图形），双端同资产同状态编号
        anim?.Init(this, OnAnimAttack);

        SetupBody();
    }

    /// <summary>动画攻击帧回调（AnimAttackEvent 触发；攻击帧相关逻辑如武器判定后续在此实现）。</summary>
    protected virtual void OnAnimAttack(EntityAnim.AttackType type) { }

    /// <summary>每帧更新（BattleManager 遍历调用）。技能 CD 为时间戳惰性计算，无需每帧推进。</summary>
    public virtual void OnUpdate()
    {
        effectController?.OnUpdate();
        UpdateMotion();
    }

    /// <summary>设置位移效果（替换已有效果时先调用其 Exit；设置时对新效果调用 Enter）。</summary>
    public void SetMotion(MotionBase motion)
    {
        RemoveMotion();
        this.motion = motion;
        motionVelocity = motion != null ? motion.Enter(this, Vector3.zero) : Vector3.zero;
    }

    /// <summary>移除位移效果（Exit + 清速度；破霸体命中/强控打断时调用）。</summary>
    public void RemoveMotion()
    {
        if (motion == null) return;
        motion.Exit(this);
        motion = null;
        motionVelocity = Vector3.zero;
    }

    /// <summary>位移期间是否允许玩家输入移动（无位移效果时允许）。</summary>
    public bool MotionCanMove => motion == null || motion.canMove;

    /// <summary>
    /// 设置移动输入方向（角色本地系，Y 被忽略；只取方向，大小不限）。
    /// 玩家由网络输入写入，AI 直接调用本方法即可——这是唯一的输入入口。
    /// </summary>
    public void SetMoveInput(Vector3 localDirection)
    {
        moveInput = new Vector3(localDirection.x, 0f, localDirection.z);
    }

    /// <summary>
    /// 设置本帧速度的水平分量（服务器权威移动的唯一出口）。
    /// 只写 X/Z：Y 由重力与外力（击飞/下落）决定，绝不被输入覆盖。
    /// </summary>
    public void SetMoveVelocity(Vector3 horizontalVelocity)
    {
        if (body == null) return;
        Vector3 velocity = body.velocity;
        velocity.x = horizontalVelocity.x;
        velocity.z = horizontalVelocity.z;
        body.velocity = velocity;
    }

    /// <summary>
    /// 动画模块在切换动画状态时传入本状态的速度（见策划案 10.1：位移由动画驱动）。
    /// x：水平速度，正数向前、负数向后。**直接覆盖**，一次设定后在整个状态内持续保持（每帧按它驱动，不被物理衰减），
    ///    传 0 即"本状态不动"——所以动画可以表达停止。
    /// y：垂直速度，**只在这次调用生效**（起跳瞬时给一次），之后由重力接管，所以不每帧写。
    ///    垂直分量 |y| &lt; 0.01 视为"本次不设置"（阈值只对 y 生效），这样动画侧不关心纵向时
    ///    不会把下落中/被击飞的速度清掉；水平不做此保护，否则无法表达停止。
    /// </summary>
    public void SetAnimSpeed(Vector2 speed)
    {
        animSpeedDeclared = true;
        animSpeed.x = speed.x; // 水平：无条件覆盖（含 0）

        if (Mathf.Abs(speed.y) < 0.01f) return;
        animSpeed.y = speed.y; // 仅留档供调试/表现读取，不参与每帧驱动
        if (body == null) return;
        Vector3 velocity = body.velocity;
        velocity.y = speed.y;
        body.velocity = velocity;
    }

    /// <summary>暂停/恢复动画播放（强控施加 = 暂停，全部移除 = 恢复）。</summary>
    public void SetAnimPaused(bool paused)
    {
        anim?.SetPaused(paused);
    }

    /// <summary>位移效果每帧推进：时间到调用 Exit 并清除；否则 Update 产出本帧速度。</summary>
    private void UpdateMotion()
    {
        if (motion == null) return;
        if (Time.time >= motion.endTime)
        {
            RemoveMotion();
            return;
        }
        motionVelocity = motion.Update(this, motionVelocity);
    }

    /// <summary>
    /// 计算当前霸体等级（实时换算，不储存字段，见策划案 12.1）。
    /// 优先级：被强控 → None（强控期间霸体失效）＞ 位移中 → Common（位移自带普通霸体，
    /// 被破霸体命中时移除位移）＞ 强制霸体 Buff → Super ＞ 动画状态换算（Endure.cs）。
    /// </summary>
    public EndureType GetEndureLevel()
    {
        if (effectController != null && effectController.IsActionBlocked()) return EndureType.None;
        if (motion != null) return EndureType.Common;
        if (effectController != null && effectController.HasSuperArmor()) return EndureType.Super;
        return anim != null ? anim.CurrentState.GetEndure() : EndureType.None;
    }

    /// <summary>
    /// 命中判定入口（子弹容器/近战调用）：破霸体 vs 当前霸体等级 → 是否进入受击，
    /// 随后结算伤害（damage 已由 AttackData.GetDamage 按公式与暴击算好，isCrit 为本次是否暴击）。
    /// </summary>
    public void ProcessHit(AttackData attack, float damage, bool isCrit)
    {
        EndureType endure = GetEndureLevel();
        bool enterHit = endure == EndureType.None || (attack.breakEndure && endure == EndureType.Common);
        if (enterHit)
        {
            RemoveMotion();  // 破霸体命中：打断位移
            anim?.DoHit();
        }
        EntityData attacker = null;
        if (BattleManager.EntityContainer.Entities.TryGetObject(attack.shooter, out var shooter)) attacker = shooter;
        OnDamaged(damage, attacker);
        Tool.BattleManager?.OnHitPassive(attacker, this, isCrit); // 攻击方被动（暴击麻痹），放在伤害结算之后
    }

    /// <summary>
    /// 受伤计算（统一入口）。触发死亡时进入 KilledEntities 由 BattleManager 统一处理。
    /// 管线：护盾吸收 → 减伤 → 受伤乘区（愈战愈勇；固定数值伤害跳过）→ 扣血 → 反伤。
    /// fixedDamage = true 表示固定数值伤害（DoT/反伤：吃护盾/减伤，不吃增减伤乘区）。
    /// </summary>
    public virtual void OnDamaged(float damage, EntityData attacker = null, bool fixedDamage = false, bool canReflect = true)
    {
        if (floatingAttribute == null || !Alive) return;
        if (attacker != null) lastAttacker = attacker; // 记录伤害来源（掉落归属判定）
        float finalDamage = damage;
        if (effectController != null)
        {
            float shield = effectController.GetShieldAbsorb();
            if (shield > 0f)
            {
                float absorbed = Mathf.Min(shield, finalDamage);
                effectController.ConsumeShield(absorbed);
                finalDamage -= absorbed;
            }
            finalDamage *= 1f - effectController.GetDamageReduceRate();
            if (!fixedDamage) finalDamage *= effectController.GetInDamageMultiplier();
        }
        finalDamage = Mathf.Max(0f, finalDamage);
        floatingAttribute.health = Mathf.Max(0f, floatingAttribute.health - finalDamage);
        if (type.category == EntityCategory.Beacon)
        {
            Tool.BattleManager?.AddBeaconDamage(finalDamage); // 进攻方得分 = 对守护点造成的总伤害
            // 采集量 = 单个角色对守护点造成的伤害量（白眼标记与对局结算经验用）
            if (attacker != null && finalDamage > 0f && Tool.BattleManager != null &&
                Tool.BattleManager.EntityOwnerClient.TryGetValue(attacker.id, out var harvester))
            {
                Tool.BattleManager.AddHarvest(harvester, finalDamage);
            }
        }
        if (attacker != null && canReflect && effectController != null)
        {
            float reflect = effectController.GetReflectDamage();
            if (reflect > 0f) attacker.OnDamaged(reflect, this, fixedDamage: true, canReflect: false);
        }
        if (floatingAttribute.health <= 0f)
        {
            if (!KilledEntities.Contains(this)) KilledEntities.Add(this);
        }
    }

    /// <summary>被击杀回调（KilledEntities 统一处理后调用）。</summary>
    public virtual void OnKilled()
    {
        // TODO: 掉落/击杀事件/愈战愈勇等
    }

    /// <summary>销毁实体（由 BattleManager 调用）。</summary>
    public virtual void OnDestroyed()
    {
        effectController?.Clear();
    }

    /// <summary>
    /// 组装服务器→客户端的实体表现摘要（默认实现覆盖公共字段 + Buff + 技能槽 + 动画状态；
    /// 子类可 override 补充特殊数据）。
    /// </summary>
    public virtual SCEntityDisplayInfo GetDisplayInfo()
    {
        var info = new SCEntityDisplayInfo()
        {
            entityId = id,
            type = type,
            camp = camp,
            position = transform.position,
            yaw = transform.eulerAngles.y,
            health = floatingAttribute != null ? (int)floatingAttribute.health : 0,
            maxHealth = floatingAttribute != null ? (int)floatingAttribute.maxHealth : 0,
            weaponCategory = (int)heldWeapon.category,
            weaponIndex = heldWeapon.index,
        };
        if (anim != null)
        {
            anim.GetDisplayAnim(out _, out var animId, out var frame);
            info.animId = animId;
            info.animFrame = frame;
        }
        effectController?.FillDisplayInfo(info);
        skillController?.FillDisplayInfo(info);
        return info;
    }

    /// <summary>槽位对应的悬浮武器位置（远程技能从此处发射；与客户端显示共用 Config.weapon_float_offsets）。</summary>
    public Vector3 GetWeaponFloatPos(int slotIndex) => transform.TransformPoint(Config.GetWeaponFloatOffset(slotIndex));

    /// <summary>通用子弹发射位置（碰撞体从底部往上 75% 处；无武器单位用）。</summary>
    public Vector3 BulletShootPos()
    {
        Bounds bounds = GetComponentInChildren<Collider>().bounds;
        return new Vector3(bounds.center.x, Mathf.Lerp(bounds.min.y, bounds.max.y, 0.75f), bounds.center.z);
    }

    /// <summary>血条 Y 偏移（相对头顶）。</summary>
    public float GetBarYOffset()
    {
        return Tool.InfoManager != null ? Tool.InfoManager.GetEntityBarYOffset(type) : 0.9f;
    }

    /// <summary>标记死亡（供外部触发，如 Bullet 击杀）。</summary>
    public void Kill()
    {
        if (!Alive) return;
        floatingAttribute.health = 0f;
        if (!KilledEntities.Contains(this)) KilledEntities.Add(this);
    }

    #region//Local
    /// <summary>
    /// 刚体准备（可移动类别的权威速度载体）。
    /// 模板未配刚体时运行时补一个：服务器模板与客户端图形是两套预制体，手工同步参数必然漂移，代码里补最稳。
    /// 旋转三轴全锁 —— 唯一旋转来源是角色控制直接赋 rotation；插值关闭 —— 权威位置读取必须是物理真值。
    /// </summary>
    private void SetupBody()
    {
        if (!Config.IsMovable(type.category)) return;

        body = GetComponent<Rigidbody>();
        if (body == null) body = gameObject.AddComponent<Rigidbody>();
        body.useGravity = true; //下落与被击飞依赖重力（单位自身 Collider 必须配好，否则会一直坠落）
        body.constraints = RigidbodyConstraints.FreezeRotation;
        body.interpolation = RigidbodyInterpolation.None;
        body.drag = Config.rb_drag;
        body.angularDrag = Config.rb_angular_drag;
    }
    #endregion
}
