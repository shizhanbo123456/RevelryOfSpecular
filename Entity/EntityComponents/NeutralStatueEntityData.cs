using System;
using UnityEngine;

public class NeutralStatueEntityData : EntityData
{
    private NeutralStatueStateMachine stateMachine;

    private EntityData captor;

    public override void OnCreate(ushort id, EntityType type, int level, EntityCamp camp = EntityCamp.Neutral)
    {
        base.OnCreate(id, type, level, camp);
        stateMachine = new NeutralStatueStateMachine(this);
    }

    public override void TickAuto()
    {
        stateMachine.Tick();
    }

    public override void OnDamaged(float damage, EntityData attacker = null, bool fixedDamage = false, bool canReflect = true, CombatLabelType damageType = CombatLabelType.Normal, Vector3? hitPos = null)
    {
        bool wasAlive = Alive;
        base.OnDamaged(damage, attacker, fixedDamage, canReflect, damageType, hitPos);
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
            EffectType bless = type.value switch
            {
                0 => EffectType.AngelBless,
                1 => EffectType.DemonBless,
                _ => throw new ArgumentOutOfRangeException(nameof(type.value), type.value, "中立雕像 value 只允许 0/1"),
            };
            captor.effectController.AddEffect(bless, 1, Config.statue_bless_duration);
        }
        battle.NotifyNeutralStatueCaptured(this.id);
        battle.ScheduleNeutralStatueRespawn();
    }
}
