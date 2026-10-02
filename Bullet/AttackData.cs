using System;

public class AttackData
{
    public ushort shooter;
    public EntityCamp shooterCamp;
    public float rate;
    public float radius;
    public bool useMagic;
    public bool breakEndure;
    public float knockbackPower;
    public Damageable.IDamageable damageable;
    public Action<EntityEffectController> addEffectEvent;
    public Action<EntityData> onHit;
    public EntityAttribute attribute;
    public float outDamageMultiplier = 1f;
    public int weaponExp;

    public static AttackData Create(EntityData shooter, float rate, float radius, bool breakEndure,
        bool useMagic = false, Damageable.IDamageable damageable = null,
        Action<EntityEffectController> addEffectEvent = null, Action<EntityData> onHit = null, int weaponExp = 0,
        float knockbackPower = 0f)
    {
        return new AttackData()
        {
            shooter = shooter != null ? shooter.id : (ushort)0,
            shooterCamp = shooter != null ? shooter.camp : EntityCamp.None,
            rate = rate,
            radius = radius,
            useMagic = useMagic,
            breakEndure = breakEndure,
            damageable = damageable,
            addEffectEvent = addEffectEvent,
            onHit = onHit,
            attribute = shooter != null ? shooter.floatingAttribute : null,
            // 出伤乘区必须在施放瞬间快照：命中时才取的话，攻击者状态已被后续帧改变
            outDamageMultiplier = shooter != null && shooter.effectController != null
                ? shooter.effectController.GetOutDamageMultiplier()
                : 1f,
            weaponExp = weaponExp,
            knockbackPower = knockbackPower,
        };
    }

    public int GetDamage(out bool isCrit)
    {
        isCrit = false;
        if (attribute == null) return 0;
        float final = rate * (useMagic ? attribute.magic : attribute.strength);
        final *= 1f + Config.skill_exp_damage_bonus * weaponExp; // 武器经验加伤
        final *= outDamageMultiplier;                            // 出伤乘区
        if (attribute.critRate > 0f && UnityEngine.Random.Range(0f, 100f) < attribute.critRate)
        {
            isCrit = true;
            final *= attribute.critDamage; // 暴击伤害为倍率
        }
        if (final < 1f) return 1;
        return (int)final;
    }
}
