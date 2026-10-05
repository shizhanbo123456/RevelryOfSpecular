using Ros.Info;
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

    public SpringWeapon springWeapon; // 双端共用：服务器发射点 / 客户端视觉都走它

    [HideInInspector] public MotionBase motion;

    [HideInInspector] public Rigidbody rb;

    [HideInInspector] public Vector3 moveInput;

    // 输入操作屏蔽：置位期间对应操作（前后/旋转）在 RecordInput 入口被抹除，实现输入阻断（外部直接置位/清位）
    [HideInInspector] public InputBlockOp inputBlockOperations;

    public virtual float YawSpeed => 0f;

    [HideInInspector] public WeaponRef heldWeapon = WeaponRef.None;

    [HideInInspector] public EntityData lastAttacker;

    [HideInInspector] public bool deathAnimDone;

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

        // 模型大小（预制体烘焙的本地包围盒）：血条/头顶锚点与发射点计算依据，所有实体必须挂载
        ModelInfo = GetComponentInChildren<EntityModelInfo>();
        if (ModelInfo == null) Debug.LogError($"实体缺少 EntityModelInfo：{name}（{type}，id={id}）");

        // 预制体/模板上的共用参数（动画类型）：服务端模板与客户端模型参数一致
        var animData = GetComponent<EntityAnimData>();
        if (animData == null) animData = GetComponentInChildren<EntityAnimData>();

        // 动画初始化（一切动画控制统一走 EntityAnim）
        if (anim != null) anim.Init(this, OnAnimAttack);
        if (anim != null)
        {
            if (animData != null) anim.SetType(animData.type);
            anim.OnDeathEventEnd += OnDeathAnimEnd;
            anim.DoSpawn();
        }

        SetupBody();

        ModelInfo.BuildCapsuleCollider();
        InitDynamicCapsule();

        // 武器漂浮弹簧：双端共用，服务器发射点 / 客户端视觉都走它（挂载在实体根，不跟随 Hips）
        // 服务器生成时位置已就位，先抬好高度再快照锚点
        springWeapon = SpringWeapon.Attach(gameObject);
        if (springWeapon != null)
        {
            TickWeaponFloat();
            springWeapon.Init();
        }
    }

    protected virtual void OnAnimAttack(EntityAnim.AttackType type) { }

    public virtual void OnUpdate()
    {
        if (effectController != null) effectController.OnUpdate();
        TickWeaponFloat();
    }

    // 武器漂浮高度跟随根骨骼 Hips（无骨骼回退模型高度中心）；XY/旋转仍由实体根决定
    private void TickWeaponFloat()
    {
        if (springWeapon == null) return;
        var hips = anim != null && anim.MainAnimator != null ? anim.MainAnimator.GetBoneTransform(HumanBodyBones.Hips) : null;
        float localY = hips != null
            ? hips.position.y - transform.position.y
            : (ModelInfo.yRange.y - ModelInfo.yRange.x) * 0.5f;
        springWeapon.transform.localPosition = new Vector3(0f, localY, 0f);
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

    private NavMeshPath navPath; // 懒创建：水晶/守护点等不寻路的实体不必付这份开销
    private Vector3[] navCorners;
    private float navNextRepathTime;

    public void MoveTo(Vector3 dest)
    {
        if (NavMesh.SamplePosition(dest, out var hit, nav_sample_radius, NavMesh.AllAreas)) dest = hit.position;

        Vector3 moved = dest - NavDestination;
        moved.y = 0f;
        if (!HasNavDestination || moved.sqrMagnitude > nav_corner_radius * nav_corner_radius) navNextRepathTime = 0f;

        NavDestination = dest;
        HasNavDestination = true;
    }

    public void StopMoving()
    {
        NavDestination = default;
        HasNavDestination = false;
        navCorners = null;
    }

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

    protected float FlatDistance(Vector3 point) => Mathf.Sqrt(FlatSqrDistance(point));

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

        UpdateGrounded();
        TickDynamicCapsule();

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
        if (effectController != null && effectController.IsActionBlocked()) return EndureType.None;
        if (motion != null) return EndureType.Common;
        if (effectController != null && effectController.HasSuperArmor()) return EndureType.Super;
        return anim != null ? anim.CurrentState.GetEndure() : EndureType.None;
    }

    public void ProcessHit(AttackData attack, int damage, bool isCrit, Vector3 hitOrigin, Vector3? floatPos = null)
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
        OnDamaged(damage, attacker, isCrit: isCrit, hitPos: floatPos ?? hitOrigin);
        Tool.BattleManager.OnHitPassive(attacker, this, isCrit); // 攻击方被动（暴击麻痹），放在伤害结算之后
    }

    //击飞
    private bool ApplyKnockback(AttackData attack, Vector3 hitOrigin)
    {
        if (rb == null) return false;
        float v = attack.knockbackPower - floatingAttribute.knockbackResistance;
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
    public virtual void OnDamaged(float damage, EntityData attacker = null, bool fixedDamage = false, bool canReflect = true, bool isCrit = false, Vector3? hitPos = null)
    {
        if (!Alive) return;
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
        SendDamageEvent(finalDamage, isCrit, hitPos);
        OnDamageApplied(finalDamage, attacker); // 各子类在此处理自己的受击后果（守护点计分与采集量等）
        if (attacker != null && canReflect && effectController != null)
        {
            float reflect = effectController.GetReflectDamage();
            if (reflect > 0f) attacker.OnDamaged(reflect, this, fixedDamage: true, canReflect: false);
        }
        if (floatingAttribute.health <= 0f) MarkAsKilled();
    }

    private void SendDamageEvent(float finalDamage, bool isCrit, Vector3? hitPos)
    {
        int display = Mathf.RoundToInt(finalDamage);
        if (isCrit) display = -display;
        var e = new SCBattleEvent()
        {
            type = SCBattleEvent.Type.Damage,
            value = display,
            targetId = id,
            hasHitPos = hitPos.HasValue,
            hitPos = hitPos ?? Vector3.zero,
        };
        if (Tool.BattleManager != null) Tool.BattleManager.SendBattleEventToViewers(this, e);
        else Tool.NetworkManager.SendBattleEvent(e);
    }

    public virtual void OnKilled()
    {
    }

    public virtual void OnDestroyed()
    {
        if (anim != null)
        {
            anim.OnDeathEventEnd -= OnDeathAnimEnd;
        }
        if (effectController != null) effectController.Clear();
        if (springWeapon != null)
        {
            Destroy(springWeapon.gameObject);
            springWeapon = null;
        }
    }

    public virtual SCEntityDisplayInfo GetDisplayInfo()
    {
        var info = new SCEntityDisplayInfo()
        {
            entityId = id,
            type = type,
            camp = camp,
            position = transform.position,
            yaw = transform.eulerAngles.y,
            health = (int)floatingAttribute.health,   // 当前生命值
            maxHealth = (int)baseAttribute.health,    // 生命值上限
        };
        if (effectController != null) effectController.FillDisplayInfo(info);
        if (skillController != null) skillController.FillDisplayInfo(info);
        return info;
    }

    public Vector3 GetWeaponFloatPos(int slotIndex)
    {
        if (springWeapon != null)
        {
            int i = Mathf.Clamp(slotIndex, 0, SpringWeapon.slotCount - 1);
            springWeapon.GetPos(i, out var p, out var q);
            return p;
        }
        return BulletShootPos();
    }

    // 从烘焙包围盒取中心 X/Z + 75% 高度（与客户端 ViewBulletShootPos 同公式）
    public Vector3 BulletShootPos()
    {
        var m = ModelInfo.transform;
        Vector3 local = new Vector3(
            (ModelInfo.xRange.x + ModelInfo.xRange.y) * 0.5f,
            Mathf.Lerp(ModelInfo.yRange.x, ModelInfo.yRange.y, 0.75f),
            (ModelInfo.zRange.x + ModelInfo.zRange.y) * 0.5f);
        return m.TransformPoint(local);
    }

    public void Kill()
    {
        if (!Alive) return;
        floatingAttribute.health = 0f;
        MarkAsKilled();
    }

    private void MarkAsKilled()
    {
        if (KilledEntities.Contains(this)) return;
        KilledEntities.Add(this);
        if (anim != null) anim.DoDie();
    }

    private void OnDeathAnimEnd()
    {
        deathAnimDone = true;
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
        var animator = anim != null ? anim.MainAnimator : null;
        if (animator == null) return false;
        var footL = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        var footR = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        var head = animator.GetBoneTransform(HumanBodyBones.Head);
        if (head == null || (footL == null && footR == null)) return false;

        headY = space.InverseTransformPoint(head.position).y;
        footY = float.MaxValue;
        if (footL != null) footY = space.InverseTransformPoint(footL.position).y;
        if (footR != null) footY = Mathf.Min(footY, space.InverseTransformPoint(footR.position).y);
        return true;
    }
    #endregion

    #region//Local
    private const float ground_probe_up = 0.1f;
    private const float ground_probe_length = 0.6f;
    private const float GroundMaxRiseSpeed = 1f;
    private static readonly RaycastHit[] s_groundHits = new RaycastHit[4];

    private bool TryGetFootWorldY(out float y)
    {
        y = 0f;
        var animator = anim != null ? anim.MainAnimator : null;
        if (animator == null) return false;
        var footL = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        var footR = animator.GetBoneTransform(HumanBodyBones.RightFoot);
        if (footL == null && footR == null) return false;
        y = float.MaxValue;
        if (footL != null) y = Mathf.Min(y, footL.position.y);
        if (footR != null) y = Mathf.Min(y, footR.position.y);
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
            ? new Vector3(transform.position.x, footY, transform.position.z) + Vector3.up * ground_probe_up
            : transform.position + Vector3.up * ground_probe_up;
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
