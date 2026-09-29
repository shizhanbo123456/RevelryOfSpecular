using UnityEngine;

/// <summary>
/// 周期性自动释放技能的实体基类（防御塔 / 瘟疫树等固定单位）。
/// 每隔 <see cref="AutoCastInterval"/> 秒从自身技能表里**随机取一个技能**尝试释放；释放间隔由各子类自行实现。
/// **索敌由本类的 AI 逻辑自己判断**：<see cref="CastSearchRange"/> 内没有敌人就不释放
/// （原先由 SkillBase.CastRange 在释放收口统一做距离门闸，该门闸已删除，故距离判断收归到 AI 侧）。
/// CD / 库存 / 沉默等校验收口在 EntitySkillController.TryUseSkill。
/// 会移动与追击的 AI（僵尸）不走这条链，见 ZombieEntityData。
/// </summary>
public abstract class AutoCastEntityData : EntityData
{
    /// <summary>自动释放技能的间隔（秒）：由各子类按自身单位定位决定。</summary>
    public abstract float AutoCastInterval { get; }

    /// <summary>索敌半径（米）= 本单位的攻击范围，由各子类给出（与其攻击技能的 CastRange 同源）。</summary>
    protected abstract float CastSearchRange { get; }

    /// <summary>下次尝试释放的时刻。</summary>
    private float nextCastTime;

    /// <summary>首次释放时刻按 id 错峰（避免同类实体同帧集中释放），之后按固定间隔推进。</summary>
    public override void OnCreate(ushort id, EntityType type, int level, EntityCamp camp = EntityCamp.Neutral)
    {
        base.OnCreate(id, type, level, camp);
        nextCastTime = Time.time + AutoCastInterval * AIStaggerPhase;
    }

    /// <summary>到点先索敌：攻击范围内有敌人才随机取一个技能释放；索敌不到就跳过本轮，等下个间隔再试。</summary>
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
