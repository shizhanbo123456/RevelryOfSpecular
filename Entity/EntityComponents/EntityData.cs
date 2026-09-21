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
    /// 仅作**退化速度**：动画模块还没声明状态速度、而玩家又在推进时用它；动画声明后完全以声明值为准。
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
    /// 移动系统**只取它的方向与正负**，速度大小来自动画声明的 <see cref="ResolveMoveVelocity"/> ——
    /// 输入是"要不要动"的开关，动画决定"动多快"。网络输入与 AI 都只写它。
    /// </summary>
    [HideInInspector] public Vector3 moveInput;

    /// <summary>绕 Y 角速度（度/秒）：随表现摘要下发客户端做包间推演，由输入渐转写入。</summary>
    public virtual float YawSpeed => 0f;

    /// <summary>当前手持武器（服务器权威）：释放技能时赋值，攻击动作结束清空；随实体摘要同步给客户端。</summary>
    [HideInInspector] public WeaponRef heldWeapon = WeaponRef.None;

    /// <summary>最近一次伤害来源（水晶掉武器归属判定等；死亡时保留供结算读取）。</summary>
    [HideInInspector] public EntityData lastAttacker;

    /// <summary>死亡动画是否已播完（由 AnimDieEvent 在片段 80% 处触发 OnDeathEventEnd 写入）。</summary>
    [HideInInspector] public bool deathAnimDone;

    /// <summary>脚是否踩在地面上（每帧物理检测，见 UpdateGrounded；写入状态机的 InAir 参数）。</summary>
    [HideInInspector] public bool grounded = true;

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

    /// <summary>伤害已落到血量之后的分支钩子：子类覆写以处理自己的受击后果（如守护点计分与采集量）。</summary>
    protected virtual void OnDamageApplied(float finalDamage, EntityData attacker) { }

    /// <summary>
    /// 按类别给物体补上对应的 EntityData 子类（服务器生成实体时调用）。
    /// 不能挂在预制体上：模板与客户端图形是同一批预制体，挂上去客户端表现体也会带上 EntityData。
    /// </summary>
    public static EntityData AddTo(GameObject target, EntityCategory category)
    {
        if (target == null) return null;
        switch (category)
        {
            case EntityCategory.Character_Attack:
            case EntityCategory.Character_Defense:
                return target.AddComponent<PlayerEntityData>();
            case EntityCategory.Zombie:
            case EntityCategory.EliteZombie:
                return target.AddComponent<ZombieEntityData>();
            case EntityCategory.Beacon:
                return target.AddComponent<BeaconEntityData>();
            case EntityCategory.Crystal:
                return target.AddComponent<CrystalEntityData>();
            case EntityCategory.Tower:
                return target.AddComponent<TowerEntityData>();
            case EntityCategory.PlagueTree:
                return target.AddComponent<PlagueTreeEntityData>();
            default:
                return null; // Prop 无专属行为，暂无子类
        }
    }

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
        moveSpeed = animData != null && animData.legHeight > 0f
            ? EntityAnimData.LegHeightToStandartRunSpeed(animData.legHeight)
            : Config.base_move_speed;

        // 动画初始化（一切动画控制统一走 EntityAnim）：激活 animator 引用与 AnimEvent 状态推送，
        // 服务器实体与客户端图形预制体都带 EntityAnim/Animator（差异只在图形），双端同资产同状态编号
        anim?.Init(this, OnAnimAttack);
        if (anim != null)
        {
            // SetType 必须排在 Init 之后：EntityAnim 的 animators 列表在 Init 里才收集，早调等于没设
            if (animData != null) anim.SetType(animData.type);
            anim.OnDeathEventEnd += OnDeathAnimEnd; // 死亡动画播完 → 允许销毁（销毁时机见 BattleManagerCombat）
            // 接收动画模块声明的速度：外部只负责把速度落到刚体上，具体多少完全由动画状态决定
            anim.OnSetVelocityForward += OnAnimSetVelocityForward;
            anim.OnSetVelocityHorizontal += OnAnimSetVelocityHorizontal;
            anim.OnSetVelocityVertical += OnAnimSetVelocityVertical;
            anim.DoSpawn();                         // 出生动画：Spawn 子状态机按 CharacterType 选 spawn / zombie_spawn
        }

        SetupBody();
    }

    /// <summary>动画攻击帧回调（AnimAttackEvent 触发；攻击帧相关逻辑如武器判定后续在此实现）。</summary>
    protected virtual void OnAnimAttack(EntityAnim.AttackType type) { }

    /// <summary>每帧更新（BattleManager 遍历调用）。技能 CD 为时间戳惰性计算，无需每帧推进。</summary>
    public virtual void OnUpdate()
    {
        effectController?.OnUpdate();
        UpdateMotion();
        UpdateGrounded();
    }

    /// <summary>朝向与移动输入的逐帧推进（由移动循环调用，canInput = 未被强控）。默认无操作。</summary>
    public virtual void OnTickMove(float deltaTime, bool canInput) { }

    /// <summary>接收移动输入（网络上行）。默认无操作：只有玩家角色会实现（见 PlayerEntityData）。</summary>
    public virtual void RecordMoveInput(Ros.Transport.CSMoveInput move) { }

    /// <summary>接收动作输入（攻击/跳跃/滑铲/技能槽，网络上行）。默认无操作：只有玩家角色会实现。</summary>
    public virtual void RecordActionInput(Ros.Transport.CSActionInput action) { }

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

    #region//接收动画声明的速度（外部只负责把速度落到刚体上，具体数值完全由动画模块决定）
    /// <summary>动画当前声明的"前后"速度（**角色本地前后**，正 = 朝前；0 = 本状态不动）。</summary>
    private bool declaredForward;
    private float declaredForwardSpeed;
    /// <summary>动画当前声明的"水平"速度（**世界空间**：x → 世界 X、y → 世界 Z）。</summary>
    private bool declaredHorizontal;
    private Vector2 declaredHorizontalSpeed;
    /// <summary>声明时所处的动画状态 hash：换状态即失效（"动画没设置"就是由此产生的）。</summary>
    private int declaredAnimId = -1;

    /// <summary>动画声明前后速度（EntityAnim.OnSetVelocityForward）：角色本地前后，正 = 朝前、负 = 朝后。声明在"当前动画状态"内有效。</summary>
    private void OnAnimSetVelocityForward(float speed)
    {
        declaredForward = true;
        declaredForwardSpeed = speed;
        declaredHorizontal = false; // 前后与水平通常不会同时声明；后声明的为准
        if (anim != null) anim.GetDisplayAnim(out declaredAnimId, out _);
    }

    /// <summary>动画声明水平速度（EntityAnim.OnSetVelocityHorizontal）：**世界空间**的水平速度，x → 世界 X、y → 世界 Z（不是本地系）。</summary>
    private void OnAnimSetVelocityHorizontal(Vector2 speed)
    {
        declaredHorizontal = true;
        declaredHorizontalSpeed = speed;
        declaredForward = false;
        if (anim != null) anim.GetDisplayAnim(out declaredAnimId, out _);
    }

    /// <summary>动画声明垂直速度（EntityAnim.OnSetVelocityVertical）：**只在这次调用生效**（起跳/下落初速），之后交给重力。</summary>
    private void OnAnimSetVelocityVertical(float speed)
    {
        if (body == null) return;
        Vector3 velocity = body.velocity;
        velocity.y = speed;
        body.velocity = velocity;
    }

    /// <summary>
    /// 本帧水平速度（服务器权威移动的唯一决策处，由 BattleManagerCombat.TickMovement 取用）。
    /// 只决定水平分量，**绝不写 Y**（Y 归重力与 OnSetVelocityVertical，即"没声明时按抛体运动"）。四种情况：
    /// ① 动画声明了速度 → **按声明值原样施加**，不做任何改写（见下）；
    /// ② 未声明但有推进输入、且在地面上 → 退化为模型移速 moveSpeed（动画还没声明速度时也能动；空中不再获得速度）；
    /// ③ 未推进（松开输入/被强控/位移锁输入）且在地面上 → 水平速度朝 0 按 Config.move_ground_friction 衰减；
    /// ④ 未推进且不在地面上 → 保持水平速度（空中无阻力，跳跃/被击飞不在空中掉速）。
    /// **加速/减速/泥沼不在这里参与**：EntityAnim 声明速度时已按当前动画播放速度缩放（SetVelocityForward/Horizontal
    /// 内部乘 PlaybackSpeed），所以这里拿到的就是缩放后的值；分支②的退化移速、MotionBase、重力则完全不吃这个倍率。
    /// </summary>
    public Vector3 ResolveMoveVelocity(float deltaTime, bool canInput)
    {
        Vector3 current = body != null ? body.velocity : Vector3.zero;
        // 扣掉位移效果的速度：它由 MotionBase 单独产出、调用方另行叠加，不参与这里的衰减/保持（否则会被累加两次）
        Vector3 horizontal = new Vector3(current.x - motionVelocity.x, 0f, current.z - motionVelocity.z);

        // 离开声明它的动画状态 → 声明失效；下一个状态若没声明，就回落到下面的默认行为
        if ((declaredForward || declaredHorizontal) && anim != null)
        {
            anim.GetDisplayAnim(out var animId, out _);
            if (animId != declaredAnimId)
            {
                declaredForward = false;
                declaredHorizontal = false;
            }
        }

        bool driving = canInput && MotionCanMove && moveInput.sqrMagnitude > 0.0001f;

        if (declaredHorizontal)
        {
            // 水平声明是**世界空间**的水平速度：直接施加，不做本地系换算、不乘任何系数
            return new Vector3(declaredHorizontalSpeed.x, 0f, declaredHorizontalSpeed.y);
        }
        if (declaredForward)
        {
            // 前后声明是**角色本地前后**的带符号速度：方向即角色朝向；符号取输入的前后轴
            // （动画只知道"在移动"，不知道玩家按的是前还是后，所以后退的负号由输入给）
            float sign = moveInput.z < -0.001f ? -1f : 1f;
            return transform.forward * (declaredForwardSpeed * sign);
        }

        // ② 未声明但有推进输入、且在地面上 → 退化为模型移速（动画还没声明速度时也能动）
        //    空中不做这件事：离地后"保持水平速度"，不因为按住方向就在空中重新获得速度（没有空中控制）
        if (driving && grounded) return (transform.rotation * moveInput).normalized * moveSpeed;
        if (!grounded) return horizontal;                                                          // ④ 空中保持

        float speed = horizontal.magnitude;                                                        // ③ 地面摩擦
        if (speed <= 0.0001f) return Vector3.zero;
        float next = Mathf.Max(0f, speed - Config.move_ground_friction * deltaTime);
        return horizontal * (next / speed);
    }
    #endregion

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
        OnDamageApplied(finalDamage, attacker); // 各子类在此处理自己的受击后果（守护点计分与采集量等）
        if (attacker != null && canReflect && effectController != null)
        {
            float reflect = effectController.GetReflectDamage();
            if (reflect > 0f) attacker.OnDamaged(reflect, this, fixedDamage: true, canReflect: false);
        }
        if (floatingAttribute.health <= 0f) MarkAsKilled();
    }

    /// <summary>被击杀回调（KilledEntities 统一处理后调用）。</summary>
    public virtual void OnKilled()
    {
        // TODO: 掉落/击杀事件/愈战愈勇等
    }

    /// <summary>销毁实体（由 BattleManager 调用）。</summary>
    public virtual void OnDestroyed()
    {
        if (anim != null)
        {
            anim.OnDeathEventEnd -= OnDeathAnimEnd;
            anim.OnSetVelocityForward -= OnAnimSetVelocityForward;
            anim.OnSetVelocityHorizontal -= OnAnimSetVelocityHorizontal;
            anim.OnSetVelocityVertical -= OnAnimSetVelocityVertical;
        }
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
            anim.GetDisplayAnim(out var animId, out var frame);
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
        MarkAsKilled();
    }

    /// <summary>
    /// 死亡标记：入本帧死亡列表（BattleManager 统一处理死后的产出/计分/复活）+ 立刻播放死亡动画。
    /// 播放与销毁分离：动画先播，物理销毁等 AnimDieEvent 播完（BattleManagerCombat.BeginDying / TickDying）。
    /// </summary>
    private void MarkAsKilled()
    {
        if (KilledEntities.Contains(this)) return;
        KilledEntities.Add(this);
        anim?.DoDie();
    }

    /// <summary>死亡动画播完（AnimDieEvent 于片段 80% 处回调）：此时才允许销毁物体。</summary>
    private void OnDeathAnimEnd()
    {
        deathAnimDone = true;
    }

    #region//Local
    /// <summary>落地检测探针：脚底附近的一个小重叠球（中心抬高 + 半径）。</summary>
    private const float ground_probe_up = 0.1f;
    private const float ground_probe_radius = 0.15f;
    private static readonly Collider[] s_groundBuffer = new Collider[4];

    /// <summary>
    /// 落地检测：脚底一个小重叠球，命中任何**非自身的非 trigger 碰撞体**即算踩在地面上，结果写入状态机 InAir。
    /// 空中/落地**只由物理决定**，不用"跳跃时长到了就当落地"这类计时——任何状态下 InAir 都必须反映真实姿态。
    /// 全层掩码 + 剔除自身与 trigger：既不必额外配地面层，也不受 entity_layer 配置正确与否的影响。
    /// </summary>
    private void UpdateGrounded()
    {
        if (anim == null || body == null) return; // 只有会动且带动画的实体需要
        // 出生动画期间状态机归 Spawn 子状态机接管，且出生点允许悬空 —— 此期间不写 InAir
        if (anim.CurrentState == EntityAnim.AnimState.Spawn) return;

        Vector3 center = transform.position + Vector3.up * ground_probe_up;
        int count = Physics.OverlapSphereNonAlloc(center, ground_probe_radius, s_groundBuffer, ~0, QueryTriggerInteraction.Ignore);
        bool onGround = false;
        for (int i = 0; i < count; i++)
        {
            var col = s_groundBuffer[i];
            if (col == null) continue;
            if (col.GetComponentInParent<EntityData>() == this) continue; // 自己的碰撞体不算地面
            onGround = true;
            break;
        }
        if (onGround == grounded) return; // 值没变就不打扰状态机
        grounded = onGround;
        anim.InAir(!onGround);
    }

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
