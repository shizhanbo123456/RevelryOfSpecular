using System.Collections.Generic;
using UnityEngine;

public class EntityEffectController
{
    public enum EffectType
    {
        HealthRegeneration,
        HealthDegeneration,
        Speed,
        Slowness,
        InstantHealth,
        InstantDamage,
        Burning,
        EnduranceRegeneration,
        EnduranceDegeneration,
        DefenseBoost,
        DefenseReduce,
        Poison,
        Freeze,
        Clear,
        Berserk,
        Deadly,
        Infection
    }

    public enum EffectGraphic
    {
        None = 0,
        HealthRegeneration = 1,
        HealthDegeneration = 2,
        Speed = 4,
        EnduranceRegeneration = 8,
        EnduranceDegeneration = 16,
        Poison = 32,
        Freeze = 64,
        BerserkOrDeadly = 128
    }

    private EntityData entity;
    private Dictionary<EffectType, int> effectLevel = new Dictionary<EffectType, int>();
    private Dictionary<EffectType, float> effectTimeLeft = new Dictionary<EffectType, float>();
    private Dictionary<EffectType, float> effectLastTime = new Dictionary<EffectType, float>();

    private Dictionary<EffectType, bool> isAttributeApplied = new Dictionary<EffectType, bool>();

    // ====================== 优化 1：全部改为 static readonly ======================
    private static readonly HashSet<EffectType> instantEffects = new HashSet<EffectType>
    {
        EffectType.InstantHealth,
        EffectType.InstantDamage,
        EffectType.Clear
    };

    private static readonly HashSet<EffectType> perSecondEffects = new HashSet<EffectType>
    {
        EffectType.HealthRegeneration,
        EffectType.HealthDegeneration,
        EffectType.Burning,
        EffectType.EnduranceRegeneration,
        EffectType.EnduranceDegeneration,
        EffectType.Poison
    };

    private static readonly HashSet<EffectType> attributeEffects = new HashSet<EffectType>
    {
        EffectType.Speed,
        EffectType.Slowness,
        EffectType.DefenseBoost,
        EffectType.DefenseReduce,
        EffectType.Freeze,
        EffectType.Berserk,
        EffectType.Deadly
    };

    private static readonly HashSet<EffectType> debuffs = new HashSet<EffectType>
    {
        EffectType.HealthDegeneration,
        EffectType.Slowness,
        EffectType.Burning,
        EffectType.EnduranceDegeneration,
        EffectType.DefenseReduce,
        EffectType.Poison,
        EffectType.Freeze,
        EffectType.Infection
    };

    public void Init(EntityData entity)
    {
        this.entity = entity;
    }

    public void OnUpdate()
    {
        if (entity == null) return;
        float deltaTime = Time.deltaTime;
        List<EffectType> expiredEffects = new List<EffectType>();

        foreach (var kvp in effectTimeLeft)
        {
            EffectType type = kvp.Key;
            effectTimeLeft[type] -= deltaTime;
            float currentLeft = effectTimeLeft[type];

            // 时间到 → 标记删除
            if (currentLeft <= 0)
            {
                expiredEffects.Add(type);
                continue;
            }

            // 每秒效果触发：5.00 → 4.92 立即生效
            if (perSecondEffects.Contains(type) && effectLastTime.TryGetValue(type, out float last))
            {
                if ((int)last > (int)currentLeft)
                {
                    ApplyPerSecondEffect(type, effectLevel[type]);
                }
            }

            if (perSecondEffects.Contains(type))
                effectLastTime[type] = currentLeft;
        }

        // 移除过期
        foreach (var type in expiredEffects)
            RemoveEffect(type);
    }

    public void AddEffect(EffectType type, int level, float time)
    {
        if (entity == null || level <= 0) return;

        if (instantEffects.Contains(type))
        {
            ApplyInstantEffect(type, level);
            return;
        }

        // 等级覆盖
        if (effectLevel.ContainsKey(type))
        {
            if (level >= effectLevel[type])
                RemoveEffect(type);
            else
                return;
        }

        effectLevel[type] = level;
        effectTimeLeft[type] = time;

        if (perSecondEffects.Contains(type))
            effectLastTime[type] = time;

        if (attributeEffects.Contains(type))
        {
            ApplyAttributeEffect(type, level);
            isAttributeApplied[type] = true;
        }
    }

    public bool TryGetEffectInfo(EffectType type, out int level, out float time)
    {
        level = 0;
        time = 0;

        if (effectLevel.TryGetValue(type, out int lvl) && effectTimeLeft.TryGetValue(type, out float left))
        {
            level = lvl;
            time = left;
            return true;
        }

        return false;
    }

    public EffectGraphic GetEffectGraphicInfo()
    {
        EffectGraphic graphic = EffectGraphic.None;

        if (effectTimeLeft.ContainsKey(EffectType.HealthRegeneration)) graphic |= EffectGraphic.HealthRegeneration;
        if (effectTimeLeft.ContainsKey(EffectType.HealthDegeneration)) graphic |= EffectGraphic.HealthDegeneration;
        if (effectTimeLeft.ContainsKey(EffectType.Speed)) graphic |= EffectGraphic.Speed;
        if (effectTimeLeft.ContainsKey(EffectType.EnduranceRegeneration)) graphic |= EffectGraphic.EnduranceRegeneration;
        if (effectTimeLeft.ContainsKey(EffectType.EnduranceDegeneration)) graphic |= EffectGraphic.EnduranceDegeneration;
        if (effectTimeLeft.ContainsKey(EffectType.Poison)) graphic |= EffectGraphic.Poison;
        if (effectTimeLeft.ContainsKey(EffectType.Freeze)) graphic |= EffectGraphic.Freeze;
        if (effectTimeLeft.ContainsKey(EffectType.Berserk) || 
            effectTimeLeft.ContainsKey(EffectType.Deadly)) graphic |= EffectGraphic.BerserkOrDeadly;

        return graphic;
    }

    #region 效果实现
    private void ApplyInstantEffect(EffectType type, int level)
    {
        switch (type)
        {
            case EffectType.InstantHealth:
                entity.floatingAttribute.health += 50 * level;
                break;
            case EffectType.InstantDamage:
                entity.floatingAttribute.health -= 50 * level;
                break;
            case EffectType.Clear:
                ClearAllDebuffs();
                break;
        }
    }

    private void ApplyPerSecondEffect(EffectType type, int level)
    {
        switch (type)
        {
            case EffectType.HealthRegeneration:
                entity.floatingAttribute.health += 10 * level;
                break;
            case EffectType.HealthDegeneration:
                entity.floatingAttribute.health -= 10 * level;
                break;
            case EffectType.Burning:
                entity.floatingAttribute.health -= 5 * level;
                break;
            case EffectType.EnduranceRegeneration:
                entity.floatingAttribute.endurance += 1 * level;
                break;
            case EffectType.EnduranceDegeneration:
                entity.floatingAttribute.endurance -= 1 * level;
                break;
            case EffectType.Poison:
                float poisonDamage = entity.baseAttribute.health * 0.05f * level;
                entity.floatingAttribute.health -= Mathf.Max((int)poisonDamage, 1);
                break;
        }
    }

    private void ApplyAttributeEffect(EffectType type, int level)
    {
        switch (type)
        {
            case EffectType.Speed:
                entity.floatingAttribute.speed += 10 * level;
                break;
            case EffectType.Slowness:
                entity.floatingAttribute.speed -= 5 * level;
                break;
            case EffectType.DefenseBoost:
                entity.floatingAttribute.defense += 20 * level;
                break;
            case EffectType.DefenseReduce:
                entity.floatingAttribute.defense -= 20 * level;
                break;
            case EffectType.Freeze:
                entity.floatingAttribute.speed -= 999;
                entity.floatingAttribute.strikeRateResistance += 10 * level;
                break;
            case EffectType.Berserk:
                entity.floatingAttribute.strikeRate += 10 * level;
                break;
            case EffectType.Deadly:
                entity.floatingAttribute.strikeDamage += 10 * level;
                break;
        }
    }

    private void RemoveAttributeEffect(EffectType type, int level)
    {
        switch (type)
        {
            case EffectType.Speed:
                entity.floatingAttribute.speed -= 10 * level;
                break;
            case EffectType.Slowness:
                entity.floatingAttribute.speed += 5 * level;
                break;
            case EffectType.DefenseBoost:
                entity.floatingAttribute.defense -= 20 * level;
                break;
            case EffectType.DefenseReduce:
                entity.floatingAttribute.defense += 20 * level;
                break;
            case EffectType.Freeze:
                entity.floatingAttribute.speed += 999;
                entity.floatingAttribute.strikeRateResistance -= 10 * level;
                break;
            case EffectType.Berserk:
                entity.floatingAttribute.strikeRate -= 10 * level;
                break;
            case EffectType.Deadly:
                entity.floatingAttribute.strikeDamage -= 10 * level;
                break;
        }
    }

    private void RemoveEffect(EffectType type)
    {
        if (!effectLevel.ContainsKey(type)) return;
        int level = effectLevel[type];

        if (attributeEffects.Contains(type) && isAttributeApplied.TryGetValue(type, out bool applied) && applied)
        {
            RemoveAttributeEffect(type, level);
            isAttributeApplied.Remove(type);
        }

        if (type == EffectType.Infection)
        {
            entity.floatingAttribute.health -= 100 * level;
        }

        effectLevel.Remove(type);
        effectTimeLeft.Remove(type);
        effectLastTime.Remove(type);
    }

    private void ClearAllDebuffs()
    {
        foreach (var debuff in debuffs)
        {
            if (effectLevel.ContainsKey(debuff))
                RemoveEffect(debuff);
        }
    }

    #endregion
}