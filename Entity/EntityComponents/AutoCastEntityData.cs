using UnityEngine;

/// <summary>
/// 周期性自动释放技能的实体基类（防御塔 / 瘟疫树等固定单位）。
/// 每隔 <see cref="AutoCastInterval"/> 秒从自身技能表里**随机取一个技能**尝试释放；释放间隔由各子类自行实现。
/// 本类不移动、不产生速度、也不自己判目标 —— 索敌与射程由技能自身的 SkillBase.CastRange 门闸负责
/// （范围内没有目标自然就不放），CD / 库存 / 沉默等校验收口在 EntitySkillController.TryUseSkill。
/// 会移动与追击的 AI（僵尸）不走这条链，见 ZombieEntityData。
/// </summary>
public abstract class AutoCastEntityData : EntityData
{
    /// <summary>自动释放技能的间隔（秒）：由各子类按自身单位定位决定。</summary>
    public abstract float AutoCastInterval { get; }

    /// <summary>下次尝试释放的时刻。</summary>
    private float nextCastTime;

    /// <summary>首次释放时刻按 id 错峰（避免同类实体同帧集中释放），之后按固定间隔推进。</summary>
    public override void OnCreate(ushort id, EntityType type, int level, EntityCamp camp = EntityCamp.Neutral)
    {
        base.OnCreate(id, type, level, camp);
        nextCastTime = Time.time + AutoCastInterval * AIStaggerPhase;
    }

    /// <summary>到点就随机取一个技能释放；所有可行性判断都在 TryUseSkill 内完成。</summary>
    public override void TickAI()
    {
        if (!Alive) return;
        if (Time.time < nextCastTime) return;
        nextCastTime = Time.time + AutoCastInterval;

        if (skillController == null || skillController.SkillCount == 0) return;
        skillController.TryUseSkill(skillController.GetSkillIdAt(Random.Range(0, skillController.SkillCount)));
    }
}
