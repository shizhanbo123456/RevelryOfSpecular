/// <summary>
/// 瘟疫树：中立争抢单位。被打死即「攻占」——由**把血量清零的那一次攻击的攻击者**获得「瘟疫祝福」
/// （+75% 出伤 / −25% 受伤，持续 30s）；被 Buff / DoT（固定数值伤害）打死的没有归属，不给任何实体加 Buff。
/// 树本身不属于任何一方，死后消失，由 BattleManager 排下一次刷新（生成/重生调度见 BattleManagerWorld）。
/// 主动攻击行为待实现：策划案 6.2 要求"对进入攻击范围的任何单位自动索敌"，但尚无 AI。
/// </summary>
public class PlagueTreeEntityData : EntityData
{
    /// <summary>攻占归属：把血量清零那次攻击的来源；被 Buff 打死时为 null。</summary>
    private EntityData captor;

    public override void OnDamaged(float damage, EntityData attacker = null, bool fixedDamage = false, bool canReflect = true)
    {
        bool wasAlive = Alive;
        base.OnDamaged(damage, attacker, fixedDamage, canReflect);
        // 只认直接攻击：固定数值伤害来自 Buff/DoT，不给归属（也不能沿用更早那次的归属）
        if (wasAlive) captor = fixedDamage ? null : attacker;
    }

    public override void OnKilled()
    {
        base.OnKilled();
        var battle = Tool.BattleManager;
        if (battle == null) return;

        if (captor != null && captor.effectController != null)
        {
            captor.effectController.AddEffect(EffectType.PlagueBless, 1, Config.plague_bless_duration);
        }
        battle.NotifyPlagueTreeCaptured(this.id);
        battle.SchedulePlagueTreeRespawn();
    }
}
