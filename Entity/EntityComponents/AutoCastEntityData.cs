using UnityEngine;

public abstract class AutoCastEntityData : EntityData
{
    public abstract float AutoCastInterval { get; }

    protected abstract float CastSearchRange { get; }

    private float nextCastTime;

    public override void OnCreate(ushort id, EntityType type, int level, EntityCamp camp = EntityCamp.Neutral)
    {
        base.OnCreate(id, type, level, camp);
        nextCastTime = Time.time + AutoCastInterval * AIStaggerPhase;
    }

    public override void TickAI()
    {
        if (!Alive) return;
        if (Time.time < nextCastTime) return;
        nextCastTime = Time.time + AutoCastInterval;

        if (skillController == null || skillController.SkillCount == 0) return;
        // AI 自己索敌：攻击范围内没有敌人就不放（释放收口已不再做距离判断）
        if (BattleManager.EntityContainer.GetNearestEnemy(this, CastSearchRange) == null) return;
        skillController.TryUseSkill(skillController.GetSkillIdAt(Random.Range(0, skillController.SkillCount)));
    }
}
