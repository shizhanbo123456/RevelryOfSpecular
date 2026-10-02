using Ros.Skill;
using UnityEngine;

public class PlayerAiController
{
    private const float DecideInterval = 0.15f;
    private const float RetargetInterval = 1f;
    private const float MaxChaseDistance = 40f;
    private const float DefaultEngageRange = 12f;
    private const float RetreatHealthRatio = 0.3f;
    private const float RetreatDistance = 25f;
    private const float MaxRetreatTime = 4f;

    private enum State { Approach, Engage, Retreat }

    private readonly PlayerEntityData entity;

    private State state = State.Approach;
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

    private void TryCast()
    {
        var sc = entity.skillController;
        if (sc == null) return;
        for (int slot = sc.SkillCount - 1; slot >= 0; slot--)
        {
            if (sc.TryUseSkill(sc.GetSkillIdAt(slot))) return;
        }
    }

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

    private float HealthRatio
    {
        get
        {
            var attr = entity.floatingAttribute;
            float cap = entity.baseAttribute != null ? entity.baseAttribute.health : 0f;
            return attr == null || cap <= 0f ? 1f : attr.health / cap;
        }
    }

    private float FlatDistance(Vector3 point)
    {
        Vector3 d = point - entity.transform.position;
        d.y = 0f;
        return d.magnitude;
    }
    #endregion
}
