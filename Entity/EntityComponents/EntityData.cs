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
    [System.Serializable]
    public struct EntityColliderInfo
    {
        public float bottom;
        public float top;
        public float radius;
    }

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

    /// <summary>碰撞体信息（判定柱：bottom~top，radius）。</summary>
    public EntityColliderInfo colliderInfo;

    /// <summary>权威移动速度（米/秒，按 EntityAnimData.legHeight 换算；模型参数而非属性，不吃 Buff）。</summary>
    [HideInInspector] public float moveSpeed = Config.base_move_speed;

    /// <summary>当前位移效果（null = 无）；SetMotion 设置并调用 Enter，时间到由 OnUpdate 调用 Exit。</summary>
    [HideInInspector] public MotionBase motion;
    /// <summary>位移效果产出的当前速度（服务器权威移动逻辑 TODO 中消费；无位移效果时为零）。</summary>
    [HideInInspector] public Vector3 motionVelocity;

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

        // 预制体/模板上的共用参数（判定柱、动画类型、腿高移速）：服务端模板与客户端模型参数一致
        var animData = GetComponent<EntityAnimData>();
        if (animData == null) animData = GetComponentInChildren<EntityAnimData>();
        if (animData != null)
        {
            colliderInfo = new EntityColliderInfo()
            {
                bottom = animData.Bottom,
                top = animData.Top,
                radius = animData.Radius,
            };
            moveSpeed = animData.legHeight > 0f
                ? EntityAnimData.LegHeightToStandartRunSpeed(animData.legHeight)
                : Config.base_move_speed;
            GetComponentInChildren<EntityAnim>()?.SetType(animData.type);
        }
        else
        {
            moveSpeed = Config.base_move_speed;
        }

        // 动画初始化（一切动画控制统一走 EntityAnim）：激活 animator 引用与 AnimEvent 状态推送，
        // 服务器实体与客户端图形预制体都带 EntityAnim/Animator（差异只在图形），双端同资产同状态编号
        GetComponentInChildren<EntityAnim>()?.Init(this, OnAnimAttack);
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

    /// <summary>暂停/恢复动画播放（强控施加 = 暂停，全部移除 = 恢复）。</summary>
    public void SetAnimPaused(bool paused)
    {
        GetComponentInChildren<EntityAnim>()?.SetPaused(paused);
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
        var anim = GetComponentInChildren<EntityAnim>();
        return anim != null ? anim.CurrentState.GetEndure() : EndureType.None;
    }

    /// <summary>
    /// 命中判定入口（BulletContainer/近战调用）：破霸体 vs 当前霸体等级 → 是否进入受击，
    /// 随后结算伤害（damage 已由 AttackData.GetDamage 按公式与暴击算好）。
    /// </summary>
    public void ProcessHit(AttackData attack, float damage)
    {
        EndureType endure = GetEndureLevel();
        bool enterHit = endure == EndureType.None || (attack.breakEndure && endure == EndureType.Common);
        if (enterHit)
        {
            RemoveMotion();  // 破霸体命中：打断位移
            GetComponentInChildren<EntityAnim>()?.DoHit();
        }
        EntityData attacker = null;
        if (BattleManager.EntityContainer.Entities.TryGetObject(attack.shooter, out var shooter)) attacker = shooter;
        OnDamaged(damage, attacker);
    }

    /// <summary>
    /// 受伤计算（统一入口）。触发死亡时进入 KilledEntities 由 BattleManager 统一处理。
    /// 管线：护盾吸收 → 减伤 → 受伤乘区（愈战愈勇；固定数值伤害跳过）→ 扣血 → 反伤。
    /// fixedDamage = true 表示固定数值伤害（DoT/反伤：吃护盾/减伤，不吃增减伤乘区）。
    /// </summary>
    public virtual void OnDamaged(float damage, EntityData attacker = null, bool fixedDamage = false, bool canReflect = true)
    {
        if (floatingAttribute == null || !Alive) return;
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
        }
        else if (type.category == EntityCategory.Crystal && attacker != null && finalDamage > 0f &&
                 BattleManager.EntityOwnerClient.TryGetValue(attacker.id, out var expOwner))
        {
            Tool.BattleManager?.AddCrystalExp(expOwner, finalDamage); // 经验 = 对水晶造成的伤害量（策划案 17.3）
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
        };
        var anim = GetComponentInChildren<EntityAnim>();
        if (anim != null)
        {
            anim.GetDisplayAnim(out var state, out var animId, out var frame);
            info.animState = (int)state;
            info.animId = animId;
            info.animFrame = frame;
        }
        effectController?.FillDisplayInfo(info);
        skillController?.FillDisplayInfo(info);
        return info;
    }

    /// <summary>子弹发射位置（基于碰撞体 top）。</summary>
    public Vector3 BulletShootPos()
    {
        Vector3 pos = transform.position;
        pos.y += colliderInfo.top;
        return pos;
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * colliderInfo.bottom, colliderInfo.radius);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * colliderInfo.top, colliderInfo.radius);
    }
}
