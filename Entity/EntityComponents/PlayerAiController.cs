using Ros.Skill;
using UnityEngine;

/// <summary>
/// 玩家 AI 决策器（普通 C# 类，由 PlayerEntityData 在 aiControlled 时创建；真人玩家不创建）。
///
/// **目标选择只有一条规则**：取「索敌范围内最近的敌对目标」——敌方角色 / 守护点 / 防御塔 / 瘟疫树 /
/// 水晶 / 僵尸一视同仁，纯比距离。敌对由 EntityCampUtil.HostileOf(自身阵营) 现算，因此**攻守不需要行为分叉**：
/// 进攻方自然去拆守护点与防守方角色，防守方自然去拦进攻方。
///
/// 状态机三态：`Approach`（走向目标）⇄ `Engage`（攻击距离内站定交战），横切 `Retreat`（残血脱离）。
/// 本类只决定"去哪、打谁、放什么"；逐帧的移动与转向在 PlayerEntityData.TickAiMove / OnTickMove 里，
/// 且**不产生速度**（速度仍由动画声明，见 EntityData.ResolveMoveVelocity）。
/// </summary>
public class PlayerAiController
{
    /// <summary>移动决策间隔（秒）：各实体按自身 id 错峰，避免同类实体同帧集中决策。</summary>
    private const float DecideInterval = 0.15f;
    /// <summary>无目标时重新找目标的节流（秒）：要遍历区块桶，比每帧判断贵得多。目标刚失效时不受此限，立即重选。</summary>
    private const float RetargetInterval = 1f;
    /// <summary>最大追击距离（米）：目标拉开到此距离即放弃，回退到"最近的其它敌对目标"。</summary>
    private const float MaxChaseDistance = 40f;
    /// <summary>接敌距离的兜底值（米）：优先使用的那件技能没配 CastRange 时用它。</summary>
    private const float DefaultEngageRange = 12f;
    /// <summary>血量低于此比例即脱离战斗。</summary>
    private const float RetreatHealthRatio = 0.3f;
    /// <summary>撤退时朝背离威胁的方向退这么远（米）。</summary>
    private const float RetreatDistance = 25f;
    /// <summary>
    /// 撤退的最长时长（秒）：到点无论是否脱战都回去接着打。
    /// 必须有这条兜底 —— **游戏内没有任何回血机制**（EntityEffectController 明确"不自动回血"），
    /// 只看"血量回升"的话残血 AI 会一路逃到被打死。
    /// </summary>
    private const float MaxRetreatTime = 4f;

    private enum State { Approach, Engage, Retreat }

    private readonly PlayerEntityData entity;

    private State state = State.Approach;
    /// <summary>当前目标 id（0 = 无）。存 id 而非引用，避免持有已销毁实体。</summary>
    private ushort targetId;
    private float nextDecideTime;
    private float nextRetargetTime;
    private float retreatStartTime;

    public PlayerAiController(PlayerEntityData entity)
    {
        this.entity = entity;
        // 首次决策按 id 错峰
        nextDecideTime = Time.time + DecideInterval * entity.AIStaggerPhase;
    }

    /// <summary>决策入口（服务器每帧调用，内部按 DecideInterval 错峰）。</summary>
    public void Tick()
    {
        if (!entity.Alive) return;
        if (Time.time < nextDecideTime) return;
        nextDecideTime = Time.time + DecideInterval;

        // 残血 → 脱离战斗（优先级最高）
        if (HealthRatio < RetreatHealthRatio)
        {
            if (state != State.Retreat)
            {
                state = State.Retreat;
                targetId = 0;
                retreatStartTime = Time.time;
            }
            if (Time.time - retreatStartTime < MaxRetreatTime)
            {
                TickRetreat();
                return;
            }
            // 撤退超时：甩不掉就别逃了（游戏没有回血机制，再拖也是等死），落回下面的接敌逻辑
        }
        else if (state == State.Retreat)
        {
            state = State.Approach; // 血量回到阈值以上：结束撤退
        }

        var target = AcquireTarget();
        if (target == null)
        {
            // 附近没有敌对目标：原地待机（下次节流点再找）
            entity.StopMoving();
            entity.ClearAiFacePoint();
            return;
        }

        float dist = FlatDistance(target.transform.position);
        if (dist > MaxChaseDistance)
        {
            // 目标跑出最大追击距离：放弃并立刻重选（AcquireRadius 已夹在 MaxChaseDistance 内，不会又选回它）
            targetId = 0;
            nextRetargetTime = 0f;
            entity.StopMoving();
            entity.ClearAiFacePoint();
            return;
        }

        if (dist <= EngageRange)
        {
            // 进入攻击距离：站定、只转向目标、循环尝试放技能
            state = State.Engage;
            entity.StopMoving();
            entity.SetAiFacePoint(target.transform.position);
            TryCast();
            return;
        }

        // 还在路上：朝目标推进（朝向由 TickAiMove 按行进方向自动推进）
        state = State.Approach;
        entity.ClearAiFacePoint();
        entity.MoveTo(target.transform.position);
    }

    #region//Local
    /// <summary>
    /// 取当前目标：**永远打"索敌范围内最近的敌对目标"**（就近打，不区分类型）。
    /// 但重选要遍历区块桶，所以按 RetargetInterval 节流——节流期内沿用已选目标；
    /// 目标消失（死亡/被摧毁）时立刻重选，否则 AI 会站着发呆。
    /// </summary>
    private EntityData AcquireTarget()
    {
        if (targetId != 0)
        {
            if (BattleManager.EntityContainer.Entities.TryGetObject(targetId, out var kept) && kept != null && kept.Alive)
            {
                if (Time.time < nextRetargetTime) return kept;
            }
            else
            {
                targetId = 0;
                nextRetargetTime = 0f; // 目标已消失：不必等节流点
            }
        }

        if (Time.time < nextRetargetTime) return null;
        nextRetargetTime = Time.time + RetargetInterval;

        var nearest = BattleManager.EntityContainer.GetNearestEnemy(entity, AcquireRadius);
        targetId = nearest != null ? nearest.id : (ushort)0;
        return nearest;
    }

    /// <summary>
    /// 撤退推进：朝背离最近敌对目标的方向走 RetreatDistance 米。
    /// 「脱战」的判据就是这个距离够远 —— 找不到目标说明已经甩掉，直接原地待机等下个决策点。
    /// </summary>
    private void TickRetreat()
    {
        var threat = BattleManager.EntityContainer.GetNearestEnemy(entity, AcquireRadius);
        entity.ClearAiFacePoint();
        if (threat == null)
        {
            entity.StopMoving();
            return;
        }

        Vector3 away = entity.transform.position - threat.transform.position;
        away.y = 0f;
        if (away.sqrMagnitude < 0.0001f) away = -entity.transform.forward;
        entity.MoveTo(entity.transform.position + away.normalized * RetreatDistance);
    }

    /// <summary>
    /// 依槽位优先级释放技能：**从后往前**试，第一个成功即返回（CD / 库存 / 沉默 / 施法距离都在 TryUseSkill 内校验）。
    /// 后往前 = 防守方先大招、再主动2、最后主动1；进攻方先新拿到的武器技能、再初始攻击技能。
    /// </summary>
    private void TryCast()
    {
        var sc = entity.skillController;
        if (sc == null) return;
        for (int slot = sc.SkillCount - 1; slot >= 0; slot--)
        {
            if (sc.TryUseSkill(sc.GetSkillIdAt(slot))) return;
        }
    }

    /// <summary>
    /// 接敌距离（米）：取"优先使用的那件技能"的 CastRange（与 TryCast 的顺序一致）；
    /// 都没配（0）时用 DefaultEngageRange，这样 CastRange 还在补的过程中 AI 也能跑。
    /// </summary>
    private float EngageRange
    {
        get
        {
            var sc = entity.skillController;
            if (sc == null) return DefaultEngageRange;
            for (int slot = sc.SkillCount - 1; slot >= 0; slot--)
            {
                if (SkillManager.TryGet(sc.GetSkillIdAt(slot), out var skill) && skill.CastRange > 0f) return skill.CastRange;
            }
            return DefaultEngageRange;
        }
    }

    /// <summary>索敌半径（米）：优先取属性可见距离（可被 Buff 提升），未配置时回退全局默认；再夹到最大追击距离内。</summary>
    private float AcquireRadius
    {
        get
        {
            float view = entity.floatingAttribute != null && entity.floatingAttribute.viewDistance > 0f
                ? entity.floatingAttribute.viewDistance
                : Config.default_skill_auto_target_radius;
            return Mathf.Min(view, MaxChaseDistance);
        }
    }

    /// <summary>血量比（0~1）；属性缺失时按满血处理，避免误判成残血。</summary>
    private float HealthRatio
    {
        get
        {
            var attr = entity.floatingAttribute;
            return attr == null || attr.maxHealth <= 0f ? 1f : attr.health / attr.maxHealth;
        }
    }

    /// <summary>到某点的水平距离（米）。</summary>
    private float FlatDistance(Vector3 point)
    {
        Vector3 d = point - entity.transform.position;
        d.y = 0f;
        return d.magnitude;
    }
    #endregion
}
