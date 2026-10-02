using UnityEngine;

public class PlagueTreeEntityData : AutoCastEntityData
{
    public override float AutoCastInterval => 5f;

    protected override float CastSearchRange => Config.plague_tree_attack_range;

    private EntityData captor;

    public override void OnDamaged(float damage, EntityData attacker = null, bool fixedDamage = false, bool canReflect = true, bool isCrit = false, Vector3? hitPos = null)
    {
        bool wasAlive = Alive;
        base.OnDamaged(damage, attacker, fixedDamage, canReflect, isCrit, hitPos);
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
