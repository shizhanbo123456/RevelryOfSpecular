using System.Collections.Generic;
using Ros.Info;
using Ros.Transport;
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

    /// <summary>血条锚点。</summary>
    public Transform BarPos;

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
                return null; // 未支持的类别无子类
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
    }

    /// <summary>动画攻击帧回调（AnimAttackEvent 触发；攻击帧相关逻辑如武器判定后续在此实现）。</summary>
    protected virtual void OnAnimAttack(EntityAnim.AttackType type) { }

    /// <summary>每帧更新（BattleManager 遍历调用）。技能 CD 为时间戳惰性计算，无需每帧推进。</summary>
    public virtual void OnUpdate()
    {
        effectController?.OnUpdate();
        UpdateGrounded();
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

    /// <summary>设置位移效果（替换已有效果时先调用其 Exit；设置时对新效果调用 Enter（传当前角色速度）；本帧速度由 TickVelocity 向 Update 取）。</summary>
    public void SetMotion(MotionBase motion)
    {
        RemoveMotion();
        this.motion = motion;
        if (motion != null && rb != null) motion.Enter(this, rb.velocity);
    }

    /// <summary>移除位移效果（Exit；破霸体命中/强控打断时调用）。</summary>
    public void RemoveMotion()
    {
        if (motion == null) return;
        motion.Exit(this);
        motion = null;
    }

    /// <summary>位移期间是否允许玩家输入移动（无位移效果时允许）。</summary>
    public bool MotionCanMove => motion == null || motion.canMove;

    #region//速度（写入必须携带 VelocitySource 来源；内部只记录动画声明值，位移逐帧取、击飞一次性覆盖，混合在本区内完成）
    /// <summary>动画当前声明的"前后"速度（**角色本地前后**，正 = 朝前；0 = 本状态不动）。</summary>
    private bool declaredForward;
    private float declaredForwardSpeed;
    /// <summary>动画当前声明的"水平"速度（**世界空间**：x → 世界 X、y → 世界 Z）。</summary>
    private bool declaredHorizontal;
    private Vector2 declaredHorizontalSpeed;
    /// <summary>声明时所处的动画状态 hash：换状态即失效（"动画没设置"就是由此产生的）。</summary>
    private int declaredAnimId = -1;

    /// <summary>声明前后速度（EntityAnim.SetVelocityForward）：角色本地前后，正 = 朝前、负 = 朝后。声明在"当前动画状态"内有效。</summary>
    public void SetVelocityForward(float speed, VelocitySource source)
    {
        declaredForward = true;
        declaredForwardSpeed = speed;
        declaredHorizontal = false; // 前后与水平通常不会同时声明；后声明的为准
        LastVelocitySource = source;
        if (anim != null) anim.GetDisplayAnim(out declaredAnimId, out _);
    }

    /// <summary>声明水平速度（EntityAnim.SetVelocityHorizontal）：**世界空间**的水平速度，x → 世界 X、y → 世界 Z（不是本地系）。</summary>
    public void SetVelocityHorizontal(Vector2 speed, VelocitySource source)
    {
        declaredHorizontal = true;
        declaredHorizontalSpeed = speed;
        declaredForward = false;
        LastVelocitySource = source;
        if (anim != null) anim.GetDisplayAnim(out declaredAnimId, out _);
    }

    /// <summary>声明垂直速度（EntityAnim.SetVelocityVertical）：**只在这次调用生效**（起跳/下落初速），之后交给重力。</summary>
    public void SetVelocityVertical(float speed, VelocitySource source)
    {
        LastVelocitySource = source;
        if (rb == null) return;
        Vector3 velocity = rb.velocity;
        velocity.y = speed;
        rb.velocity = velocity;
    }

    /// <summary>
    /// 统一设速入口：写入必须携带 VelocitySource。目前只有击飞（Knockback）走这里——
    /// 一次性覆盖：直接写刚体水平分量并清除动画声明，之后由未声明分支自然保持/衰减。
    /// 动画速度走声明接口，MotionBase 由 TickVelocity 每帧内部结算。
    /// </summary>
    public void SetVelocity(VelocitySource source, Vector3 horizontalVelocity)
    {
        LastVelocitySource = source;
        if (rb == null || source != VelocitySource.Knockback) return;
        declaredForward = false;
        declaredHorizontal = false;
        SetRbHorizontal(horizontalVelocity);
    }

    /// <summary>
    /// 每帧速度结算（服务器权威移动的唯一决策处，由 BattleManagerCombat.TickMovement 调用）：
    /// ① 更新 MotionBase（时间到移除，否则取本帧位移速度）② 朝向与输入推进（OnTickMove）③ 混合写刚体 ④ 区块索引同步。
    /// 混合规则：**有 MotionBase 时无视摩擦**——canMove 时最终速度 = 位移速度 + 动画声明（空中同样生效），
    /// 否则位移完全接管（阻断动画来源）；无 MotionBase 时：动画声明有效按声明施加（空中同样生效），
    /// 未声明且在地面按 Config.move_ground_friction 衰减、空中保持刚体速度。
    /// **玩家主动操控的速度只能来自动画声明**；加速/减速/泥沼已在 EntityAnim 声明时按播放速度缩放，这里不再乘。
    /// </summary>
    public void TickVelocity(float deltaTime)
    {
        if (rb == null) return;

        // ① MotionBase 每帧更新：时间到移除，否则取本帧位移速度（传入当前角色速度，供位移实现基于现有速度计算）
        Vector3 motionVelocity = Vector3.zero;
        if (motion != null)
        {
            if (Time.time >= motion.endTime) RemoveMotion();
            else motionVelocity = motion.Update(this, rb.velocity);
        }

        // ② 朝向与输入推进（强控/位移锁输入期间输入不生效）
        bool canInput = effectController == null || effectController.CanMove();
        OnTickMove(deltaTime, canInput);

        // 换状态 → 动画声明失效
        if ((declaredForward || declaredHorizontal) && anim != null)
        {
            anim.GetDisplayAnim(out var animId, out _);
            if (animId != declaredAnimId)
            {
                declaredForward = false;
                declaredHorizontal = false;
            }
        }

        // ③ 混合写刚体（只写 X/Z，Y 归重力与 SetVelocityVertical）
        if (motion != null)
        {
            Vector3 final = motionVelocity;
            if (motion.canMove) final += DeclaredVelocity();
            SetRbHorizontal(final);
        }
        else if (declaredForward || declaredHorizontal)
        {
            SetRbHorizontal(DeclaredVelocity()); // 空中也照常应用动画速度
        }
        else
        {
            // 未声明：刚体现有水平速度（含击飞残留）原地保持；在地面按摩擦朝 0 衰减
            Vector3 carried = rb.velocity;
            carried.y = 0f;
            float speed = carried.magnitude;
            if (grounded)
            {
                float next = Mathf.Max(0f, speed - Config.move_ground_friction * deltaTime);
                SetRbHorizontal(speed <= 0.0001f ? Vector3.zero : carried * (next / speed));
            }
        }

        // ④ 区块索引跟着走：范围索敌（GetNearestEnemy 等）只查区块桶，不更新就会一直按出生区块找人
        BattleManager.EntityContainer.Entities.UpdateObjectPosition(id);
    }

    /// <summary>当前动画声明换算成的世界水平速度（未声明返回零）。</summary>
    private Vector3 DeclaredVelocity()
    {
        if (declaredHorizontal) return new Vector3(declaredHorizontalSpeed.x, 0f, declaredHorizontalSpeed.y);
        if (declaredForward)
        {
            // 前后声明是角色本地前后：符号取输入的前后轴（动画只知道"在移动"，不知道玩家按的是前还是后）
            float sign = moveInput.z < -0.001f ? -1f : 1f;
            return transform.forward * (declaredForwardSpeed * sign);
        }
        return Vector3.zero;
    }

    /// <summary>把水平分量写入刚体（Y 不动）。</summary>
    private void SetRbHorizontal(Vector3 horizontal)
    {
        Vector3 velocity = rb.velocity;
        velocity.x = horizontal.x;
        velocity.z = horizontal.z;
        rb.velocity = velocity;
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
    public void ProcessHit(AttackData attack, float damage, bool isCrit, Vector3 hitOrigin)
    {
        EndureType endure = GetEndureLevel();
        bool enterHit = endure == EndureType.None || (attack.breakEndure && endure == EndureType.Common);
        if (enterHit)
        {
            RemoveMotion();  // 破霸体命中：打断位移
            anim?.DoHit();
            // 被击飞（还会受到击飞抗性影响）；被打断但没击飞时转身面向命中来源，让 Hit 动画朝向正确
            if (!ApplyKnockback(attack, hitOrigin)) OnHitInterrupted(hitOrigin);
        }
        EntityData attacker = null;
        if (BattleManager.EntityContainer.Entities.TryGetObject(attack.shooter, out var shooter)) attacker = shooter;
        OnDamaged(damage, attacker);
        Tool.BattleManager.OnHitPassive(attacker, this, isCrit); // 攻击方被动（暴击麻痹），放在伤害结算之后
    }

    /// <summary>
    /// 击飞：v = 攻击力度 − 被击飞抗性（同量纲，v ≤ 0 一并落在阈值内），不够阈值就不击飞并返回 false。
    /// 水平方向 = 命中位置指向本实体的水平方向，垂直方向向上，**两个方向的速度值都取 v**。
    /// **这是一条独立的水平速度来源**（攻击命中的一次性速度覆盖，不走动画声明、也不走 MotionBase）；
    /// 写入 Knockback 来源时清除动画声明与保留值，之后空中保持、落地才按地面摩擦衰减。
    /// </summary>
    private bool ApplyKnockback(AttackData attack, Vector3 hitOrigin)
    {
        if (rb == null || floatingAttribute == null) return false;
        float v = attack.knockbackPower - floatingAttribute.knockbackResistance;
        if (v <= 0.01f) return false;

        Vector3 away = transform.position - hitOrigin;
        away.y = 0f;
        Vector3 dir = away.sqrMagnitude > 0.0001f ? away.normalized : transform.forward;
        SetVelocity(VelocitySource.Knockback, dir * v); // 水平（只写 X/Z）
        SetVelocityVertical(v, VelocitySource.Knockback); // 垂直（一次性初速，之后交回重力）
        return true;
    }

    /// <summary>
    /// 被打断但没击飞：转身面向命中来源（被击飞方向的反向）。
    /// 保持视角水平——命中来源坐标压平到水平面后 LookRotation，结果只含绕 Y 的旋转。
    /// </summary>
    protected virtual void OnHitInterrupted(Vector3 hitOrigin)
    {
        Vector3 to = hitOrigin - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude < 0.0001f) return; // 命中来源几乎在正上/正下方，保持原朝向
        transform.rotation = Quaternion.LookRotation(to.normalized);
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
    /// <summary>判落地允许的最大上升速度：超过它（起跳/击飞上升段）即使脚底碰到地面层也不算落地。</summary>
    private const float GroundMaxRiseSpeed = 0.1f;
    private static readonly Collider[] s_groundBuffer = new Collider[4];

    /// <summary>
    /// 落地检测：先判垂直速度（上升段必然离地，省一次物理查询），再脚底一个小重叠球，
    /// 命中**地面层**（InfoManager.ground_layer）上任意非 trigger 碰撞体即算踩在地面上，结果写入状态机 InAir。
    /// 只查地面层：全层查询会把踩着的角色也算成地面；仍剔除自身兜底（层号配错时防自踩）。
    /// 空中/落地**只由物理决定**，不用"跳跃时长到了就当落地"这类计时——任何状态下 InAir 都必须反映真实姿态。
    /// </summary>
    private void UpdateGrounded()
    {
        if (anim == null || rb == null) return; // 只有会动且带动画的实体需要
        // 出生动画期间状态机归 Spawn 子状态机接管，且出生点允许悬空 —— 此期间不写 InAir
        if (anim.CurrentState == EntityAnim.AnimState.Spawn) return;

        // 先判垂直速度：仍有明显上升速度（起跳/击飞上升段，初帧脚底球可能仍与地面重叠）必然离地
        if (rb.velocity.y >= GroundMaxRiseSpeed)
        {
            if (grounded)
            {
                grounded = false;
                anim.InAir(true);
            }
            return;
        }

        Vector3 center = transform.position + Vector3.up * ground_probe_up;
        int count = Physics.OverlapSphereNonAlloc(center, ground_probe_radius, s_groundBuffer, EntityPhysics.GroundMask, QueryTriggerInteraction.Ignore);
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
