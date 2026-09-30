using Ros.Info;
using Ros.Transport;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;

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

    /// <summary>是否由 AI 驱动（服务器在生成玩家实体后置位）。真人玩家为 false，输入来自网络。</summary>
    public bool aiControlled;

    /// <summary>基础属性（配置，实体生成时定下、之后不再变化）。其 health 是**生命值上限**。</summary>
    public EntityAttribute baseAttribute;

    /// <summary>运行时属性（基础 + 效果/成长叠加）。其 health 是**当前生命值**（同名字段在 base 里是上限）。
    /// **读上限一律读 baseAttribute.health，读当前生命一律读 floatingAttribute.health。**</summary>
    public EntityAttribute floatingAttribute;

    /// <summary>效果（Buff）控制器。</summary>
    public EntityEffectController effectController;

    /// <summary>技能控制器。</summary>
    public EntitySkillController skillController;

    /// <summary>动画组件（OnCreate 一次性获取；非人形单位无动画时为 null）。</summary>
    public EntityAnim anim;

    /// <summary>当前位移效果（null = 无）；SetMotion 设置并调用 Enter，时间到由 OnUpdate 调用 Exit。</summary>
    [HideInInspector] public MotionBase motion;

    /// <summary>最后一次速度写入的来源。动画声明是内部唯一记录的速度值；位移每帧向 MotionBase 取、击飞一次性写刚体，均不留存。</summary>
    public VelocitySource LastVelocitySource { get; private set; } = VelocitySource.Animation;

    /// <summary>
    /// 刚体（可移动类别的权威速度载体，见 SetupBody 与 BattleManagerCombat.TickMovement）。
    /// 位移效果与输入移动都只产出速度、由它积分位置；非可移动类别为 null。
    /// </summary>
    [HideInInspector] public Rigidbody rb;

    /// <summary>
    /// 移动输入方向（**角色本地系**：X = 右、Z = 前、Y 恒为 0）。
    /// 移动系统**只取它的方向与正负**，速度大小由动画模块声明（见 <see cref="TickVelocity"/>）——
    /// 输入只决定"往哪走、朝前还是朝后"，**不产出速度**。网络输入与 AI 都只写它。
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

    /// <summary>实体模型的本地包围盒（预制体烘焙数据，见 Entity/Anim/EntityModelInfo）：模型大小/锚点计算的依据，服务器模板可能没有（null）。</summary>
    public EntityModelInfo ModelInfo { get; private set; }

    /// <summary>本帧死亡实体（由 BattleManager 统一处理）。</summary>
    protected static readonly List<EntityData> KilledEntities = new();

    /// <summary>本帧死亡实体列表（只读）。</summary>
    public static IReadOnlyList<EntityData> KilledList => KilledEntities;

    /// <summary>清空本帧死亡列表（由 BattleManager 每帧处理后调用）。</summary>
    public static void ClearKilled() => KilledEntities.Clear();

    /// <summary>是否存活。</summary>
    public bool Alive => floatingAttribute != null && floatingAttribute.health > 0f;

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
                throw new System.Exception(); // 未支持的类别无子类
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
        // 克隆即满血：base 的 health 是"生命值上限"，同一个字段克隆到 floating 后就承载"当前生命值"（复活走重建实体，同样落在这里）
        floatingAttribute = baseAttribute.Clone();
        effectController = new EntityEffectController();
        effectController.Init(this);
        skillController = new EntitySkillController();
        skillController.Init(this);

        anim = GetComponentInChildren<EntityAnim>();

        // 模型大小（预制体烘焙的本地包围盒）：血条/头顶等锚点计算依据；服务器模板无图形时为 null
        ModelInfo = GetComponentInChildren<EntityModelInfo>();

        // 预制体/模板上的共用参数（动画类型）：服务端模板与客户端模型参数一致
        var animData = GetComponent<EntityAnimData>();
        if (animData == null) animData = GetComponentInChildren<EntityAnimData>();

        // 动画初始化（一切动画控制统一走 EntityAnim）：激活 animator 引用与 AnimEvent 状态推送，
        // 服务器实体与客户端图形预制体都带 EntityAnim/Animator（差异只在图形），双端同资产同状态编号
        anim?.Init(this, OnAnimAttack);
        if (anim != null)
        {
            // SetType 必须排在 Init 之后：EntityAnim 的 animators 列表在 Init 里才收集，早调等于没设
            if (animData != null) anim.SetType(animData.type);
            anim.OnDeathEventEnd += OnDeathAnimEnd; // 死亡动画播完 → 允许销毁（销毁时机见 BattleManagerCombat）
            anim.DoSpawn();                         // 出生动画：Spawn 子状态机按 CharacterType 选 spawn / zombie_spawn
        }

        SetupBody();

        // 模型碰撞体：按烘焙的本地包围盒构造胶囊碰撞体（高度 = Y 范围，半径 = Z 范围一半）。
        // 服务器无图形模板时 ModelInfo 为 null，自动跳过。
        ModelInfo?.BuildCapsuleCollider();
    }

    /// <summary>动画攻击帧回调（AnimAttackEvent 触发；攻击帧相关逻辑如武器判定后续在此实现）。</summary>
    protected virtual void OnAnimAttack(EntityAnim.AttackType type) { }

    /// <summary>每帧更新（BattleManager 遍历调用）。技能 CD 为时间戳惰性计算，无需每帧推进。</summary>
    public virtual void OnUpdate()
    {
        effectController?.OnUpdate();
    }

    /// <summary>朝向与移动输入的逐帧推进（由移动循环调用，canInput = 未被强控）。默认无操作。</summary>
    public virtual void OnTickMove(float deltaTime, bool canInput) { }

    /// <summary>
    /// AI 决策（服务器每帧遍历调用，见 BattleManager.UpdateAI）。默认无操作：只有需要 AI 的实体覆写。
    /// 决策频率由各实现自行错峰控制——转向与推进仍走 OnTickMove（每帧），本方法只负责"想做什么"。
    /// </summary>
    public virtual void TickAI() { }

    /// <summary>AI 错峰槽位数：同类实体按 id 取余，把首次决策铺到不同时间相位上，避免同帧集中。</summary>
    protected const int AIStaggerSlots = 16;

    /// <summary>本实体的 AI 错峰相位（0~1，由 id 决定）：首次决策/释放时刻 = 间隔 × 该值。</summary>
    public float AIStaggerPhase => (id % AIStaggerSlots) / (float)AIStaggerSlots;

    #region 寻路与移动目标（寻路单位共用；只记目标与算方向，不产生速度）
    /// <summary>寻路目的地（由 <see cref="MoveTo"/> 设置，Y 已吸附到 NavMesh 表面）。</summary>
    protected Vector3 NavDestination { get; private set; }
    /// <summary>是否有寻路目的地（false = 站立）。</summary>
    protected bool HasNavDestination { get; private set; }

    /// <summary>NavMesh 吸附半径（米）：目标点常取自地面物件或位置略有偏差的实体。</summary>
    private const float nav_sample_radius = 1f;
    /// <summary>拐点判定距离（米）：离拐点这么近就换下一个。</summary>
    private const float nav_corner_radius = 0.3f;
    /// <summary>重算路径的最小间隔（秒）：目标不动时不必每帧跑一次寻路。</summary>
    private const float nav_repath_interval = 0.25f;

    private NavMeshPath navPath; // 懒创建：水晶/守护点等不寻路的实体不必付这份开销
    private Vector3[] navCorners;
    private float navNextRepathTime;

    /// <summary>
    /// 前往某处。只记目标，"往哪走"留到 <see cref="ResolveNavDirection"/> 算 —— 寻路只借 NavMesh 算方向，
    /// 移动仍走"移动输入 + 动画声明速度"这一条链路（见 <see cref="TickVelocity"/>）。
    /// 可反复调用改目标；目标没挪窝就不重算路径，所以逐帧拿最新坐标调它也不会每帧跑寻路。
    /// </summary>
    public void MoveTo(Vector3 dest)
    {
        if (NavMesh.SamplePosition(dest, out var hit, nav_sample_radius, NavMesh.AllAreas)) dest = hit.position;

        Vector3 moved = dest - NavDestination;
        moved.y = 0f;
        if (!HasNavDestination || moved.sqrMagnitude > nav_corner_radius * nav_corner_radius) navNextRepathTime = 0f;

        NavDestination = dest;
        HasNavDestination = true;
    }

    /// <summary>清除寻路目的地（站立）。</summary>
    public void StopMoving()
    {
        NavDestination = default;
        HasNavDestination = false;
        navCorners = null;
    }

    /// <summary>
    /// 本帧朝目的地的水平单位方向：沿 NavMesh 拐点走；算不出路径（未烘焙/脱离网格）时直线朝目标兜底，等它自己走回网格。
    /// 无目的地返回零向量。
    /// </summary>
    public Vector3 ResolveNavDirection()
    {
        if (!HasNavDestination) return Vector3.zero;

        if (Time.time >= navNextRepathTime)
        {
            navNextRepathTime = Time.time + nav_repath_interval;
            navPath ??= new NavMeshPath();
            bool ok = NavMesh.CalculatePath(transform.position, NavDestination, NavMesh.AllAreas, navPath);
            navCorners = ok && navPath.corners.Length > 1 ? navPath.corners : null;
        }

        Vector3 target = NavDestination;
        if (navCorners != null)
        {
            int i = 0; // 起点与走过的拐点都靠这个距离跳过
            while (i < navCorners.Length && FlatSqrDistance(navCorners[i]) <= nav_corner_radius * nav_corner_radius) i++;
            if (i < navCorners.Length) target = navCorners[i];
        }

        Vector3 dir = target - transform.position;
        dir.y = 0f;
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.zero;
    }

    /// <summary>到某点的水平距离（米，寻路只关心水平面）。</summary>
    protected float FlatDistance(Vector3 point) => Mathf.Sqrt(FlatSqrDistance(point));

    /// <summary>到某点的水平距离平方。</summary>
    protected float FlatSqrDistance(Vector3 point)
    {
        Vector3 d = point - transform.position;
        d.y = 0f;
        return d.sqrMagnitude;
    }
    #endregion

    /// <summary>接收输入（网络上行，移动边沿 + 动作按下）。默认无操作：只有玩家角色会实现（见 PlayerEntityData）。</summary>
    public virtual void RecordInput(Ros.Transport.CSPlayerInput input) { }

    /// <summary>设置位移效果（直接替换，旧效果不调用 Exit；对新效果调用 Enter（传当前角色速度）；本帧速度由 TickVelocity 向 Update 取）。</summary>
    public void SetMotion(MotionBase motion)
    {
        if (rb == null || anim == null) return;
        this.motion = motion;
        if (motion != null) declaredMotionEnter = true;
    }

    /// <summary>位移期间是否允许玩家输入移动（无位移效果时允许）。</summary>
    public bool MotionCanMove => motion == null || motion.canMove;

    #region//速度
    private bool declaredForward;
    private float declaredForwardSpeed;
    private bool declaredHorizontal;
    private Vector2 declaredHorizontalSpeed;
    private bool declaredVertical;
    private float declaredVerticalSpeed;
    private bool declaredMotionEnter;

    //设置前后速度
    public void SetVelocityForward(float speed, VelocitySource source)
    {
        if (rb == null) return;
        declaredForward = true;
        declaredForwardSpeed = speed;
        LastVelocitySource = source;
    }

    //设置水平速度
    public void SetVelocityHorizontal(Vector2 speed, VelocitySource source)
    {
        if (rb == null) return;
        declaredHorizontal = true;
        declaredHorizontalSpeed = speed;
        LastVelocitySource = source;
    }

    //设置垂直速度
    public void SetVelocityVertical(float speed, VelocitySource source)
    {
        if (rb == null) return;
        LastVelocitySource = source;
        declaredVerticalSpeed = speed;
        declaredVertical = true;
    }

    //水平方向速度混合
    private Vector3 DeclaredVelocity()
    {
        Vector3 v = Vector3.zero;
        if (declaredHorizontal) v = new Vector3(declaredHorizontalSpeed.x, 0f, declaredHorizontalSpeed.y);
        if (declaredForward) v = declaredForwardSpeed * transform.forward;
        if (declaredVertical) v = Vector3.up * declaredVerticalSpeed;
        return v;
    }

    // 每帧速度结算
    public void TickVelocity(float deltaTime)
    {
        if (rb == null) return;

        UpdateGrounded();

        //朝向与输入推进（强控/位移锁输入期间输入不生效）
        bool canInput = effectController == null || effectController.CanMove();
        OnTickMove(deltaTime, canInput);

        // 混合写刚体
        if (motion != null)
        {
            Vector3 final = Vector3.zero;
            if (declaredMotionEnter)
            {
                final = motion.Enter(this, rb.velocity);
            }
            else if (Time.time >= motion.endTime)
            {
                final = motion.Exit(this, rb.velocity);
            }
            else
            {
                final = motion.Update(this, rb.velocity);
            }

            if (motion.canMove) final += DeclaredVelocity();
            rb.velocity = final;
        }
        else if (declaredForward || declaredHorizontal || declaredVertical)
        {
            var declared= DeclaredVelocity();
            if (!grounded)
            {
                declared.y = rb.velocity.y;
            }
            rb.velocity = declared;
        }
        else
        {
            if (grounded)
            {
                rb.velocity=rb.velocity*Mathf.Min(0.9f,Time.deltaTime*500);
            }
        }

        //区块索引刷新
        BattleManager.EntityContainer.Entities.UpdateObjectPosition(id);

        declaredForward = false;
        declaredHorizontal = false;
        declaredVertical = false;
        declaredMotionEnter = false;
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

    /// <summary>暂停/恢复动画播放（强控施加 = 暂停，全部移除 = 恢复）。</summary>
    public void SetAnimPaused(bool paused)
    {
        anim?.SetPaused(paused);
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
    /// 命中判定入口（子弹容器/近战调用）：破霸体 vs 当前霸体等级 → 是否进入受击（打断位移 + 播受击动画），
    /// 随后按攻击力度施加击飞，最后结算伤害（damage 已由 AttackData.GetDamage 按公式与暴击算好，isCrit 为本次是否暴击）。
    /// hitOrigin = 命中位置（子弹位置 / 近战判定球心），用于确定击飞的水平方向。
    /// </summary>
    public void ProcessHit(AttackData attack, int damage, bool isCrit, Vector3 hitOrigin)
    {
        EndureType endure = GetEndureLevel();
        bool enterHit = anim!=null && (attack.breakEndure ? (endure == EndureType.None || endure == EndureType.Common) : endure == EndureType.None);
        if (enterHit)
        {
            motion=null;  // 破霸体命中：打断位移
            // 被击飞/被打断
            if (!ApplyKnockback(attack, hitOrigin)) 
                OnHitInterrupted(hitOrigin);
        }
        EntityData attacker = null;
        if (BattleManager.EntityContainer.Entities.TryGetObject(attack.shooter, out var shooter)) attacker = shooter;
        OnDamaged(damage, attacker, isCrit: isCrit);
        Tool.BattleManager.OnHitPassive(attacker, this, isCrit); // 攻击方被动（暴击麻痹），放在伤害结算之后
    }

    //击飞
    private bool ApplyKnockback(AttackData attack, Vector3 hitOrigin)
    {
        if (rb == null || floatingAttribute == null) return false;
        float v = attack.knockbackPower - floatingAttribute.knockbackResistance;
        if (v <= 0.01f) return false;

        Vector3 away = transform.position - hitOrigin;
        away.y = 0f;
        Vector3 dir = away.sqrMagnitude > 0.0001f ? away.normalized : transform.forward;
        // 水平声明是「二维对」：x→世界X、y→世界Z。这里必须显式取 (dir.x, dir.z)——
        // 直接传 dir*v 会走 Vector3→Vector2 隐式转换、只保留 (x, y)，dir.z*v 被静默丢弃，
        // 后果是击飞只能沿世界 ±X 推出（南北向命中会变成东西向飞）。
        SetVelocityHorizontal(new Vector2(dir.x * v, dir.z * v), VelocitySource.Knockback); // 水平
        SetVelocityVertical(v, VelocitySource.Knockback); // 垂直
        return true;
    }

    // 被打断但没击飞
    private void OnHitInterrupted(Vector3 hitOrigin)
    {
        anim.DoHit();
        Vector3 to = hitOrigin - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude < 0.0001f) return; // 命中来源几乎在正上/正下方，保持原朝向
        transform.rotation = Quaternion.LookRotation(to.normalized);
    }

    // 受伤计算
    public virtual void OnDamaged(float damage, EntityData attacker = null, bool fixedDamage = false, bool canReflect = true, bool isCrit = false)
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
        SendDamageEvent(finalDamage, isCrit);
        OnDamageApplied(finalDamage, attacker); // 各子类在此处理自己的受击后果（守护点计分与采集量等）
        if (attacker != null && canReflect && effectController != null)
        {
            float reflect = effectController.GetReflectDamage();
            if (reflect > 0f) attacker.OnDamaged(reflect, this, fixedDamage: true, canReflect: false);
        }
        if (floatingAttribute.health <= 0f) MarkAsKilled();
    }

    /// <summary>伤害飘字广播：value 0=无效，>0=普通伤害，<0=暴击（绝对值为伤害量），targetId=受击实体。</summary>
    private void SendDamageEvent(float finalDamage, bool isCrit)
    {
        int display = Mathf.RoundToInt(finalDamage);
        if (isCrit) display = -display;
        Tool.NetworkManager.SendBattleEvent(new SCBattleEvent()
        {
            type = SCBattleEvent.Type.Damage,
            value = display,
            targetId = id,
        });
    }

    /// <summary>被击杀回调（KilledEntities 统一处理后调用）。</summary>
    public virtual void OnKilled()
    {
    }

    /// <summary>销毁实体（由 BattleManager 调用）。</summary>
    public virtual void OnDestroyed()
    {
        if (anim != null)
        {
            anim.OnDeathEventEnd -= OnDeathAnimEnd;
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
            health = floatingAttribute != null ? (int)floatingAttribute.health : 0,   // 当前生命值
            maxHealth = baseAttribute != null ? (int)baseAttribute.health : 0,        // 生命值上限
            weaponCategory = (int)heldWeapon.category,
            weaponIndex = heldWeapon.index,
        };
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
    /// <summary>落地检测射线：起点为脚底上方一点。</summary>
    private const float ground_probe_up = 0.1f;
    /// <summary>落地检测射线长度：从起点（脚底上方 0.1m）向下 0.4m。</summary>
    private const float ground_probe_length = 0.4f;
    /// <summary>判落地允许的最大上升速度：超过它（起跳/击飞上升段）即使射线打到地面层也不算落地。</summary>
    private const float GroundMaxRiseSpeed = 1f;
    private static readonly RaycastHit[] s_groundHits = new RaycastHit[4];

    /// <summary>
    /// 落地检测：先判垂直速度（上升段必然离地，省一次物理查询），再从脚底上方 0.1m 向下打一条 0.4m 的射线，
    /// 命中**地面层**（InfoManager.ground_layer）上任意非 trigger 碰撞体即算踩在地面上，结果写入状态机 InAir。
    /// 只查地面层：全层查询会把踩着的角色也算成地面；仍剔除自身兜底（层号配错时防自踩）。
    /// 空中/落地**只由物理决定**，不用"跳跃时长到了就当落地"这类计时——任何状态下 InAir 都必须反映真实姿态。
    /// </summary>
    private void UpdateGrounded()
    {
        if (anim == null || rb == null) return; // 只有会动且带动画的实体需要
        // 出生动画期间状态机归 Spawn 子状态机接管，且出生点允许悬空 —— 此期间不写 InAir
        if (anim.CurrentState == EntityAnim.AnimState.Spawn) return;

        // 先判垂直速度：仍有明显上升速度（起跳/击飞上升段）必然离地
        if (rb.velocity.y >= GroundMaxRiseSpeed)
        {
            if (grounded)
            {
                grounded = false;
                anim.InAir(true);
            }
            return;
        }

        Vector3 origin = transform.position + Vector3.up * ground_probe_up;
        int count = Physics.RaycastNonAlloc(origin, Vector3.down, s_groundHits, ground_probe_length, EntityPhysics.GroundMask, QueryTriggerInteraction.Ignore);
        bool onGround = false;
        for (int i = 0; i < count; i++)
        {
            var col = s_groundHits[i].collider;
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
    /// 刚体准备（可移动单位的权威速度载体）。
    /// 可移动 = 挂了 EntityAnim 的单位；水晶/守护点/防御塔/瘟疫树等无动画单位不移动。
    /// 模板未配刚体时运行时补一个：服务器模板与客户端图形是两套预制体，手工同步参数必然漂移，代码里补最稳。
    /// 旋转只锁 X/Z（防倒地翻滚），**Y 轴不锁**；插值关闭 —— 权威位置读取必须是物理真值。
    /// </summary>
    private void SetupBody()
    {
        if (anim == null) return;

        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = true; //下落与被击飞依赖重力（单位自身 Collider 必须配好，否则会一直坠落）
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.drag = Config.rb_drag;
        rb.angularDrag = Config.rb_angular_drag;
    }
    #endregion
}
