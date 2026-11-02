using Ros.Transport;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.AI;

// 输入操作屏蔽类别：屏蔽"前后"=屏蔽 W；屏蔽"旋转"=屏蔽 A/D（不再逐个按键屏蔽）
[Flags]
public enum InputBlockOp : byte
{
    None = 0,
    Forward = 1 << 0,  // 前后移动（W）
    Rotation = 1 << 1, // 旋转（A/D）
    Jump = 1 << 2,     // 跳跃（K）
    Slide = 1 << 3,    // 滑铲（LShift）
    Attack = 1 << 4,   // 攻击与技能（J + 技能槽 U/I/O/L/H/Y）
}

public abstract class EntityData : MonoBehaviour
{
    [HideInInspector] public ushort id;

    public EntityType type;

    public int level = 1;

    public EntityCamp camp;

    public bool aiControlled;

    public EntityAttribute baseAttribute;

    public EntityAttribute floatingAttribute;

    public EntityEffectController effectController;

    public EntitySkillController skillController;

    public EntityAnim anim;

    [HideInInspector] public MotionBase motion;

    [HideInInspector] public Rigidbody rb;

    [HideInInspector] public Vector3 moveInput;

    // 输入操作屏蔽：置位期间对应操作（前后/旋转）在 RecordInput 入口被抹除，实现输入阻断（外部直接置位/清位）
    [HideInInspector] public InputBlockOp inputBlockOperations;

    public virtual float YawSpeed => 0f;

    // 飞行模式（仅玩家实体启用，见 PlayerEntityData.SetFlightMode）：重力关闭、免疫击飞/打断/位移，动画按地面状态播放
    public virtual bool FlightMode => false;

    [HideInInspector] public WeaponRef heldWeapon = WeaponRef.None;

    [HideInInspector] public EntityData lastAttacker;

    [HideInInspector] public bool grounded = true;

    public EntityModelInfo ModelInfo { get; private set; }


    protected static readonly List<EntityData> KilledEntities = new();

    public static IReadOnlyList<EntityData> KilledList => KilledEntities;

    public static void ClearKilled() => KilledEntities.Clear();

    public bool Alive => floatingAttribute.health > 0f;

    protected virtual void OnDamageApplied(float finalDamage, EntityData attacker) { }

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
            case EntityCategory.GuardStatue:
                return target.AddComponent<GuardStatueEntityData>();
            case EntityCategory.Coral:
                return target.AddComponent<CoralEntityData>();
            case EntityCategory.CrystalStatue:
                return target.AddComponent<CrystalStatueEntityData>();
            case EntityCategory.NeutralStatue:
                return target.AddComponent<NeutralStatueEntityData>();
            default:
                throw new System.Exception(); // 未支持的类别无子类
        }
    }

    public virtual void OnCreate(ushort id, EntityType type, int level, EntityCamp camp = EntityCamp.Neutral)
    {
        this.id = id;
        this.type = type;
        this.level = Mathf.Min(level, Config.battle_max_level); // 战斗期等级上限
        level = this.level;
        this.camp = camp;
        baseAttribute = Tool.InfoManager != null ? Tool.InfoManager.GetAttribute(type, level) : new EntityAttribute();
        // 克隆即满血：base 的 health 是"生命值上限"，同一个字段克隆到 floating 后就承载"当前生命值"（复活走重建实体，同样落在这里）
        floatingAttribute = baseAttribute.Clone();
        effectController = new EntityEffectController();
        effectController.Init(this);
        skillController = new EntitySkillController();
        skillController.Init(this);

        anim = GetComponentInChildren<EntityAnim>();

        // 模型大小（预制体烘焙的本地包围盒）：血条/头顶锚点与发射点计算依据，所有实体必须挂载
        ModelInfo = GetComponentInChildren<EntityModelInfo>();
        if (ModelInfo == null) Debug.LogError($"实体缺少 EntityModelInfo：{name}（{type}，id={id}）");

        // 预制体/模板上的共用参数（动画类型）：服务端模板与客户端模型参数一致
        var animData = GetComponent<EntityAnimData>();
        if (animData == null) animData = GetComponentInChildren<EntityAnimData>();

        // 动画初始化（一切动画控制统一走 EntityAnim）
        if (anim != null) anim.Init(this);
        if (anim != null)
        {
            if (animData != null) anim.SetType(animData.type);
            anim.DoSpawn();
        }

        SetupBody();

        ModelInfo.BuildCapsuleCollider();
        InitDynamicCapsule();
    }

    public virtual void OnUpdate()
    {
        if (effectController != null) effectController.OnUpdate();
    }

    public virtual void OnTickMove(float deltaTime, bool canInput) { }

    public virtual void TickAI() { }

    protected const int AIStaggerSlots = 16;

    public float AIStaggerPhase => (id % AIStaggerSlots) / (float)AIStaggerSlots;

    #region 寻路与移动目标（寻路单位共用；只记目标与算方向，不产生速度）
    protected Vector3 NavDestination { get; private set; }
    protected bool HasNavDestination { get; private set; }

    private const float nav_sample_radius = 1f;
    private const float nav_corner_radius = 0.3f;
    private const float nav_repath_interval = 0.25f;
    private const float nav_arrive_radius = 0.3f; // 到达死区：执行层每帧判定，越过目标后不再掉头
    private const float nav_rescue_tolerance = 1f;      // 与导航面的距离在此以内视为仍在网格上
    private const float nav_rescue_search_radius = 10f; // 脱网救援点搜索半径
    private const float nav_rescue_interval = 1f;       // 脱网检查间隔

    private NavMeshPath navPath; // 懒创建：珊瑚/雕像等不寻路的实体不必付这份开销
    private Vector3[] navCorners;
    private float navNextRepathTime;
    private Vector3 navLastPos;
    private float navLastStep; // 上一帧实际位移，用于放大到达/拐点半径
    private float navArriveRadius = nav_arrive_radius; // 当前生效的到达半径（随帧率放大）
    private Vector3 navLastDir;
    private int navReverseFrames;
    private int navFailCount; // 连续算路失败次数：达到上限即判定目标不可达
    private float navNextRescueTime;

    public bool NavUnreachable => navFailCount >= Config.nav_path_fail_limit;

    public void MoveTo(Vector3 dest)
    {
        if (NavMesh.SamplePosition(dest, out var hit, nav_sample_radius, NavMesh.AllAreas)) dest = hit.position;

        Vector3 moved = dest - NavDestination;
        moved.y = 0f;
        if (!HasNavDestination || moved.sqrMagnitude > nav_corner_radius * nav_corner_radius)
        {
            navNextRepathTime = 0f;
            navFailCount = 0; // 换了目的地才重新计数，否则同一目标永远攒不满
        }

        NavDestination = dest;
        HasNavDestination = true;
    }

    public void StopMoving()
    {
        NavDestination = default;
        HasNavDestination = false;
        navCorners = null;
        navLastDir = Vector3.zero;
        navReverseFrames = 0;
        navFailCount = 0;
    }

    public Vector3 ResolveNavDirection()
    {
        if (!HasNavDestination) return Vector3.zero;

        // 到达/拐点半径随单帧位移放大：低帧率下一帧就能跨过固定半径，导致在目标两侧反复掉头
        float step = (transform.position - navLastPos).magnitude;
        navLastPos = transform.position;
        if (step > 0.001f && step < 2f) navLastStep = Mathf.Lerp(navLastStep, step, 0.15f); // 平滑：避免到达半径逐帧跳动
        navArriveRadius = Mathf.Max(nav_arrive_radius, navLastStep * 1.5f);
        float arrive = navArriveRadius;

        Vector3 toDest = NavDestination - transform.position;
        toDest.y = 0f;
        if (toDest.sqrMagnitude <= arrive * arrive) return Vector3.zero;

        if (Time.time >= navNextRepathTime)
        {
            navNextRepathTime = Time.time + nav_repath_interval;
            navPath ??= new NavMeshPath();
            // 起点先吸附到导航面：角色脚底与 NavMesh 有高度差时直接用 transform.position 会算出异常路径
            Vector3 start = NavMesh.SamplePosition(transform.position, out var startHit, 2f, NavMesh.AllAreas)
                ? startHit.position
                : transform.position;
            bool ok = NavMesh.CalculatePath(start, NavDestination, NavMesh.AllAreas, navPath);
            if (!ok || navPath.corners.Length <= 1)
            {
                // 算不出路（终点不可达/起点脱网）：原地等下一次重算，绝不退化成直线硬走
                navCorners = null;
                navFailCount++;
                return Vector3.zero;
            }

            navCorners = navPath.corners;
            navFailCount = 0;
        }

        // 上一轮就失败了：等重算，本帧不动
        if (navCorners == null) return Vector3.zero;

        Vector3 dir = NavDestination - transform.position;
        dir.y = 0f;

        if (navCorners != null)
        {
            int i = 0; // 起点与走过的拐点都靠这个距离跳过
            while (i < navCorners.Length && FlatSqrDistance(navCorners[i]) <= arrive * arrive) i++;
            if (i < navCorners.Length)
            {
                Vector3 toCorner = navCorners[i] - transform.position;
                toCorner.y = 0f;
                // 拐点落在身后说明这条路径与前进方向矛盾（起点脱网/贴边），弃用拐点改用直线，避免前后翻转
                if (toCorner.sqrMagnitude > 0.0001f && Vector3.Dot(toCorner, dir) > 0f) dir = toCorner;
            }
        }

        if (dir.sqrMagnitude <= 0.0001f) return Vector3.zero;

        Vector3 final = dir.normalized;
        // 方向与上一帧相反时先保持原方向，连续两帧才接受：目标在两点间跳变时不至于前后抽搐
        if (navLastDir.sqrMagnitude > 0.5f && Vector3.Dot(final, navLastDir) < 0f)
        {
            if (++navReverseFrames < 2) return navLastDir;
        }
        else
        {
            navReverseFrames = 0;
        }

        navLastDir = final;
        return final;
    }

    protected float FlatDistance(Vector3 point) => Mathf.Sqrt(FlatSqrDistance(point));

    // 到导航目的地的水平距离（含 NavMesh 吸附后的实际点）：状态机判定到达要跟执行层同一口径
    // 进入到达死区一律返回 0，否则「执行层已停、状态机还在等」会互相卡住
    public float DistanceToNavDestination
    {
        get
        {
            if (!HasNavDestination) return float.MaxValue;
            float d = FlatDistance(NavDestination);
            return d <= navArriveRadius ? 0f : d;
        }
    }

    // 脱网救援：被挤到导航面外（击退/斜坡/异常位移）时吸附回最近可达点，避免永久卡死
    private void TickNavRescue()
    {
        if (rb == null || Time.time < navNextRescueTime) return;

        // 只有正在移动的单位才需要检查，站桩单位（珊瑚/雕像）不必付这份开销
        if (!HasNavDestination && moveInput.sqrMagnitude <= 0.0001f) return;
        // 只对 AI 单位（僵尸/精英僵尸/人机玩家）救援；真人玩家靠刚体移动，允许站在导航面外（台阶/平台）
        if (!(this is ZombieEntityData) && !(this is PlayerEntityData && aiControlled)) return;
        navNextRescueTime = Time.time + nav_rescue_interval;

        // 站位附近仍有导航面，说明没脱网
        if (NavMesh.SamplePosition(transform.position, out _, nav_rescue_tolerance, NavMesh.AllAreas)) return;
        if (!NavMesh.SamplePosition(transform.position, out var rescuePoint, nav_rescue_search_radius, NavMesh.AllAreas)) return;

        rb.velocity = Vector3.zero; // 清掉惯性，避免下一次物理步又把它推回网格外
        rb.position = rescuePoint.position;
        transform.position = rescuePoint.position;
        navLastPos = rescuePoint.position;
        navNextRepathTime = 0f;
    }

    protected float FlatSqrDistance(Vector3 point)
    {
        Vector3 d = point - transform.position;
        d.y = 0f;
        return d.sqrMagnitude;
    }
    #endregion

    public virtual void RecordInput(CSPlayerInput input) { }

    // 设置输入操作屏蔽（None = 解除）；PlayerEntityData 在设置瞬间立即抬起对应操作涉及的键
    public virtual void SetInputBlock(InputBlockOp op)
    {
        inputBlockOperations = op;
    }

    // 立即让指定操作涉及的键在服务器处于抬起状态（模拟服务器收到抬起边沿），不持续屏蔽
    public virtual void SetInputReleased(InputBlockOp op) { }

    // 查询某操作是否处于屏蔽状态（玩家与 AI 共用）
    public bool InputBlocked(InputBlockOp op) => (inputBlockOperations & op) != 0;

    public void SetMotion(MotionBase motion)
    {
        if (FlightMode) return; // 飞行免疫：不施加位移
        if (rb == null || anim == null) return;
        this.motion = motion;
        if (motion != null) declaredMotionEnter = true;
    }

    public bool MotionCanMove => motion == null || motion.canMove;

    //设置是否开启重力
    public void SetGravityEnabled(bool enabled)
    {
        if (rb == null) return; //不可移动单位无刚体，无重力可关
        rb.useGravity = enabled;
        if (enabled || !grounded) return;
        grounded = true; //关重力期间强制在地面
        if (anim != null) anim.InAir(false);
    }

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
    }

    //设置水平速度
    public void SetVelocityHorizontal(Vector2 speed, VelocitySource source)
    {
        if (rb == null) return;
        declaredHorizontal = true;
        declaredHorizontalSpeed = speed;
    }

    //设置垂直速度
    public void SetVelocityVertical(float speed, VelocitySource source)
    {
        if (rb == null) return;
        declaredVertical = true;
        declaredVerticalSpeed = speed;
    }

    //水平方向速度混合
    private Vector3 DeclaredVelocity()
    {
        Vector3 v = Vector3.zero;
        if (declaredHorizontal) v = new Vector3(declaredHorizontalSpeed.x, 0f, declaredHorizontalSpeed.y);
        if (declaredForward) v += declaredForwardSpeed * transform.forward;
        if (declaredVertical) v.y=declaredVerticalSpeed;
        return v;
    }

    // 每帧速度结算
    public void TickVelocity(float deltaTime)
    {
        if (rb == null) return;

        if (FlightMode && rb.useGravity) rb.useGravity = false; // 飞行期间持续关重力（动画/技能状态可能设回）

        UpdateGrounded();
        TickDynamicCapsule();
        TickNavRescue(); // 被挤出导航面时先拉回来，否则后面的移动逻辑会一直失效

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
                motion = null; // 到期即移除：惯性随 Exit 返回值保留，下一帧交回声明/摩擦
            }
            else
            {
                final = motion.Update(this, rb.velocity);
            }

            if (motion != null && motion.canMove) final += DeclaredVelocity();
            rb.velocity = final;
        }
        else if (declaredForward || declaredHorizontal || declaredVertical)
        {
            var declared= DeclaredVelocity();
            if (!grounded&&!declaredVertical)
            {
                declared.y = rb.velocity.y;
            }
            rb.velocity = declared;
        }
        else if (grounded)
        {
            rb.velocity = rb.velocity * Mathf.Min(0.9f, Time.deltaTime * 500);
        }

        //区块索引刷新
        BattleManager.EntityContainer.Entities.UpdateObjectPosition(id);

        declaredForward = false;
        declaredHorizontal = false;
        declaredVertical = false;
        declaredMotionEnter = false;
    }
    #endregion

    public void SetMoveInput(Vector3 localDirection)
    {
        moveInput = new Vector3(localDirection.x, 0f, localDirection.z);
    }

    public bool IsMovingForward => moveInput.z > 0.01f;

    public void SetAnimPaused(bool paused)
    {
        if (anim != null) anim.SetPaused(paused);
    }

    public EndureType GetEndureLevel()
    {
        // 配表「霸体等级覆盖」优先于强控/位移/动画状态
        if (effectController != null && effectController.TryGetEndureOverride(out var endure)) return endure;
        if (effectController != null && effectController.IsActionBlocked()) return EndureType.None;
        if (motion != null) return EndureType.Common;
        return anim != null ? anim.CurrentState.GetEndure() : EndureType.None;
    }

    public void ProcessHit(AttackData attack, int damage, CombatLabelType damageType, Vector3 hitOrigin, Vector3? floatPos = null)
    {
        // 闪避：带闪避 Buff 时本次命中全部闪避（没有闪避率），不结算伤害、不进入受击
        if (effectController != null && effectController.HasEffect(EffectType.DodgeBoost))
        {
            OnDamaged(0f, damageType: CombatLabelType.Evade, hitPos: floatPos ?? hitOrigin);
            return;
        }
        EndureType endure = GetEndureLevel();
        bool enterHit = anim!=null && (attack.breakEndure ? (endure == EndureType.None || endure == EndureType.Common) : endure == EndureType.None);
        if (enterHit && FlightMode) enterHit = false; // 飞行免疫：不受击飞/被打断（伤害照常结算）
        if (enterHit)
        {
            motion=null;  // 破霸体命中：打断位移
            // 被击飞/被打断
            if (!ApplyKnockback(attack, hitOrigin)) 
                OnHitInterrupted(hitOrigin);
        }
        EntityData attacker = null;
        if (BattleManager.EntityContainer.Entities.TryGetObject(attack.shooter, out var shooter)) attacker = shooter;
        OnDamaged(damage, attacker, damageType: damageType, hitPos: floatPos ?? hitOrigin);
        Tool.BattleManager.OnHitPassive(attacker, this, damageType); // 攻击方被动（暴击麻痹），放在伤害结算之后
    }

    //击飞
    private bool ApplyKnockback(AttackData attack, Vector3 hitOrigin)
    {
        if (rb == null) return false;
        float v = attack.knockbackPower;
        if (v <= 0.01f) return false;

        Vector3 away = transform.position - hitOrigin;
        away.y = 0f;
        Vector3 dir = away.sqrMagnitude > 0.0001f ? away.normalized : transform.forward;
        // 水平为 Vector2(x→世界X, y→世界Z)，必须显式取 (dir.x, dir.z)，否则 Vector3→Vector2 隐式转换会丢弃 dir.z（击飞变沿 ±X）
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

    // 受伤计算（hitPos = 命中位置，飘字优先显示在命中处；DoT/反伤等无范围伤害传 null 回退实体头顶）
    public virtual void OnDamaged(float damage, EntityData attacker = null, bool fixedDamage = false, bool canReflect = true, CombatLabelType damageType = CombatLabelType.Normal, Vector3? hitPos = null)
    {
        if (!Alive) return;
        if (BattleManager.IsGmInvincible(id)) return; // GM 无敌：伤害按 0 处理（见 BattleManagerGm）
        if (attacker != null) lastAttacker = attacker; // 记录伤害来源（掉落归属判定）
        float finalDamage = damage;
        if (Tool.BattleManager != null) finalDamage = Tool.BattleManager.AdjustIncomingDamage(this, finalDamage); // 白眼伯爵：单次受伤上限
        float shieldDamage = 0f; // 命中类伤害面对的名义伤害（护盾飘字用）
        if (effectController != null)
        {
            // 只有命中类（正常/暴击）吃护盾；DoT、反伤、瘟疫引爆等特殊伤害无视护盾直接扣血
            if (damageType == CombatLabelType.Normal || damageType == CombatLabelType.Crit)
            {
                float shield = effectController.GetShieldAbsorb();
                if (shield > 0f && finalDamage > 0f)
                {
                    shieldDamage = finalDamage;
                    float absorbed = Mathf.Min(shield, finalDamage);
                    effectController.ConsumeShield(absorbed);
                    finalDamage -= absorbed; // 盾不够的溢出部分照常扣血
                }
            }
            finalDamage *= 1f - effectController.GetDamageReduceRate();
            if (!fixedDamage) finalDamage *= effectController.GetInDamageMultiplier();
        }
        finalDamage = Mathf.Max(0f, finalDamage);
        floatingAttribute.health = Mathf.Max(0f, floatingAttribute.health - finalDamage);
        healthDirty = true;
        if (Tool.BattleManager != null) Tool.BattleManager.CheckLowHealthShield(this, damageType); // 瘟疫使者：扣血后按血线挂盾
        // 撞盾的命中一律显示护盾页；闪避自带类型；其余 0 伤害改判失效
        CombatLabelType shownType = damageType;
        float shownDamage = finalDamage;
        if (shieldDamage > 0f)
        {
            shownType = CombatLabelType.Shield;
            shownDamage = shieldDamage;
        }
        else if (damageType != CombatLabelType.Evade && finalDamage <= 0f)
        {
            shownType = CombatLabelType.Invalid;
        }
        SendCombatLabelEvent(shownDamage, shownType, hitPos);
        OnDamageApplied(finalDamage, attacker); // 各子类在此处理自己的受击后果（雕像计分与采集量等）
        if (attacker != null && canReflect && effectController != null)
        {
            float reflect = effectController.GetReflectDamage(finalDamage); // 反弹按实际受伤（扣盾/减伤后）
            if (reflect > 0f) attacker.OnDamaged(reflect, this, fixedDamage: true, canReflect: false, damageType: CombatLabelType.Reflect);
        }
        if (floatingAttribute.health <= 0f) MarkAsKilled();
    }

    private void SendCombatLabelEvent(float shownAmount, CombatLabelType labelType, Vector3? hitPos)
    {
        var d = new SCCombatLabel()
        {
            amount = Mathf.RoundToInt(shownAmount),
            type = (int)labelType,
            targetId = id,
            hasHitPos = hitPos.HasValue,
            hitPos = hitPos ?? Vector3.zero,
        };
        if (Tool.BattleManager != null) Tool.BattleManager.SendCombatLabelToViewers(this, d);
        else Tool.NetworkManager.SendCombatLabel(d);
    }

    public virtual void OnKilled()
    {
    }

    public virtual void OnDestroyed()
    {
        if (effectController != null) effectController.Clear();
    }

    // 显示同步脏标记：health 只在当前生命值变化时置脏（上限全程不变）、buff 只在增删时置脏、skill 只在持有列表变化时置脏、
    // transform 记待发次数：0 = 位置/朝向/速度有变化，<2 时每轮下发并 +1，满 2 停发（停下后仍会多发一个包）
    public bool healthDirty;
    public bool buffDirty;
    public bool skillDirty;
    public int transformDirty;

    private Vector3 transformBasePos;    // 上次下发时的位姿基准（发包时对齐当前）
    private float transformBaseYaw;
    private float transformBaseSpeedSqr; // 上次下发时的水平速度模平方
    private bool transformBaseReady;

    // 每帧调用：待发次数满 2（已静止）才重新判定，超阈值置 0 重新计数（基准不随帧推进，误差恒定为阈值量级）
    public void CheckTransformDirty()
    {
        if (transformDirty < 2) return;
        Vector3 p = transform.position;
        float yaw = transform.eulerAngles.y;
        float speedSqr = SpeedSqr();
        if (!transformBaseReady)
        {
            transformBasePos = p;
            transformBaseYaw = yaw;
            transformBaseSpeedSqr = speedSqr;
            transformBaseReady = true;
            return;
        }
        float eps = Config.entity_sync_pos_epsilon;
        if ((p - transformBasePos).sqrMagnitude >= eps * eps) { transformDirty = 0; return; }
        if (Mathf.Abs(Mathf.DeltaAngle(transformBaseYaw, yaw)) >= Config.entity_sync_yaw_epsilon) { transformDirty = 0; return; }
        if (Mathf.Abs(speedSqr - transformBaseSpeedSqr) >= 0.01f) transformDirty = 0; // 0.01 ≈ 速度差 0.1m/s，兜住减速末段
    }

    // 位姿已下发：基准对齐当前，待发次数未满 2 时 +1（满 2 后的全量心跳包只是重复带位姿，不计数）
    public void OnTransformSent()
    {
        transformBasePos = transform.position;
        transformBaseYaw = transform.eulerAngles.y;
        transformBaseSpeedSqr = SpeedSqr();
        transformBaseReady = true;
        if (transformDirty < 2) transformDirty++;
    }

    // 与下发口径一致：只取水平分量，客户端不推演纵向
    private float SpeedSqr()
    {
        if (rb == null) return 0f;
        Vector3 v = rb.velocity;
        return v.x * v.x + v.z * v.z;
    }

    // 持有技能不在这里：改走 SCSkillSync 独立下发（见 EntitySkillController）
    public SCEntityDisplayInfo GetDisplayInfo(bool full)
    {
        // 各段按脏标记选择性携带（full = 完整同步 / 首见，必带全量）
        bool withTransform = full || transformDirty < 2;
        bool withHealth = full || healthDirty;
        bool withBuffs = full || buffDirty;
        var info = new SCEntityDisplayInfo()
        {
            entityId = id,
            type = type,
            camp = camp,
            hasTransform = withTransform,
            hasHealth = withHealth,
            hasBuffs = withBuffs,
        };
        if (withTransform)
        {
            info.position = transform.position;
            info.yaw = transform.eulerAngles.y;
        }
        if (withHealth)
        {
            info.health = (int)floatingAttribute.health;   // 当前生命值
            info.maxHealth = (int)baseAttribute.health;    // 生命值上限（全程不变，随 health 包顺带携带）
        }
        if (withBuffs && effectController != null) effectController.FillDisplayInfo(info);
        return info;
    }

    public void Kill()
    {
        if (!Alive) return;
        floatingAttribute.health = 0f;
        healthDirty = true;
        MarkAsKilled();
    }

    private void MarkAsKilled()
    {
        if (KilledEntities.Contains(this)) return;
        KilledEntities.Add(this); // 不播死亡动画：本帧末由 BattleManager 直接销毁
    }

    #region 动态受击体积（人形实体：胶囊上下边界跟随脚/头骨骼的动画姿态）
    private CapsuleCollider capsule;      // 动态胶囊（OnCreate 建好后缓存）
    private bool capsuleDynamicReady;     // 人形骨骼齐全才启用；否则胶囊保持静态包围盒
    private float boneInitFootY;          // 初始姿态脚部高度（模型根节点本地空间，双脚取低者）
    private float boneInitHeadY;          // 初始姿态头部高度（同上）
    private float capsuleBaseBottom;      // 建胶囊时的下边界（本地 Y）
    private float capsuleBaseTop;         // 建胶囊时的上边界（本地 Y）

    private void InitDynamicCapsule()
    {
        if (anim == null) return;
        capsule = ModelInfo.GetComponent<CapsuleCollider>();
        if (capsule == null) return;
        if (!TrySampleBoneSpan(ModelInfo.transform, out float footY, out float headY)) return;

        boneInitFootY = footY;
        boneInitHeadY = headY;
        capsuleBaseBottom = capsule.center.y - capsule.height * 0.5f;
        capsuleBaseTop = capsule.center.y + capsule.height * 0.5f;
        capsuleDynamicReady = true;
    }

    private void TickDynamicCapsule()
    {
        if (!capsuleDynamicReady) return;
        if (!TrySampleBoneSpan(ModelInfo.transform, out float footY, out float headY)) return;
        float bottom = capsuleBaseBottom + (footY - boneInitFootY);
        float top = capsuleBaseTop + (headY - boneInitHeadY);
        ModelInfo.SetCapsuleVerticalBounds(bottom, top);
    }

    private bool TrySampleBoneSpan(Transform space, out float footY, out float headY)
    {
        footY = headY = 0f;
        if (anim == null) return false;
        var head = EntityAnchor.Head.GetTransform(ModelInfo, anim);
        var footL = EntityAnchor.LeftFoot.GetTransform(ModelInfo, anim);
        var footR = EntityAnchor.RightFoot.GetTransform(ModelInfo, anim);
        if (head.transform == null || (footL.transform == null && footR.transform == null)) return false;

        headY = space.InverseTransformPoint(head.position).y;
        footY = float.MaxValue;
        if (footL.transform != null) footY = space.InverseTransformPoint(footL.position).y;
        if (footR.transform != null) footY = Mathf.Min(footY, space.InverseTransformPoint(footR.position).y);
        return true;
    }
    #endregion

    #region//Local
    private const float GroundMaxRiseSpeed = 1f;
    private static readonly RaycastHit[] s_groundHits = new RaycastHit[4];

    private bool TryGetFootWorldY(out float y)
    {
        y = 0f;
        if (anim == null) return false;
        var footL = EntityAnchor.LeftFoot.GetTransform(ModelInfo, anim);
        var footR = EntityAnchor.RightFoot.GetTransform(ModelInfo, anim);
        if (footL.transform == null && footR.transform == null) return false;
        y = float.MaxValue;
        if (footL.transform != null) y = Mathf.Min(y, footL.position.y);
        if (footR.transform != null) y = Mathf.Min(y, footR.position.y);
        return true;
    }

    private void UpdateGrounded()
    {
        if (anim == null) { grounded = true; return; } // 无动画实体永远在地面
        if (rb == null) return;
        if (!rb.useGravity) return; // 关重力期间强制在地面，不做地面检测
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

        // 检测起点取脚部高度（约定），反映动画姿态；非人形无脚骨骼回退实体位置
        Vector3 origin = TryGetFootWorldY(out float footY)
            ? new Vector3(transform.position.x, footY, transform.position.z) + Vector3.up * Config.ground_probe_up
            : transform.position + Vector3.up * Config.ground_probe_up;
        int count = Physics.RaycastNonAlloc(origin, Vector3.down, s_groundHits, Config.ground_probe_length, EntityPhysics.GroundMask, QueryTriggerInteraction.Ignore);
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
