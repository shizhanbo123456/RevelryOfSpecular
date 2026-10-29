using System;
using System.Collections.Generic;
using Ros.Transport;
using UnityEngine;

public enum EffectType
{
    //防守方专有
    StatueShield = 0,                // 岩石护盾
    StatueBlaze = 1,                 // 灵火
    PopeGuard = 2,                  // 教皇守护
    PlagueMark = 3,                 // 瘟疫标记
    PaleLight = 4,                  // 苍白之光
    PaleDark = 5,                   // 苍白之暗
    PaleFire = 6,                   // 苍白之火
    PaleIce = 7,                    // 苍白之冰
    PaleThunder = 8,                // 苍白之雷
    EyeMark = 9,                    // 白眼标记
    NightMark = 10,                 // 黑夜标记
    PlaguePollute = 11,             // 瘟疫污染
    DeathStroll = 12,               // 死灵漫步
    DeathCurse = 13,                // 死灵诅咒
    Mire = 14,                      // 泥沼
    Fog = 15,                       // 迷雾

    //特殊来源
    GmSpeedUp = 16,                 // GM极速
    PlagueBless = 17,               // 瘟疫祝福
    BattleFury = 18,                // 愈战愈勇
    StatueReduce = 19,              // 分层减伤（每存活贤者雕像 25%）
    MinimapLost = 20,               // 小地图失联

    //通用
    Shield = 21,                    // 护盾
    DodgeBoost = 22,                // 闪避
    Stun = 23,                      // 麻痹
    Freeze = 24,                    // 冰冻
    Root = 25,                      // 定身
    Silence = 26,                   // 沉默
    ZombieScreamBoost = 27,         // 嘶吼增益
    Burning = 28,                   // 燃烧
    Poison = 29,                    // 中毒
    Reflect = 30,                   // 反伤

    //通用（纯属性改变类）
    AttrStrengthBoost = 31,         // 力量提升
    AttrStrengthDecrease = 32,      // 力量降低
    AttrMagicBoost = 33,            // 魔法提升
    AttrMagicDecrease = 34,         // 魔法降低
    AttrCritRateBoost = 35,         // 暴击率提升
    AttrCritRateDecrease = 36,      // 暴击率降低
    AttrCritDamageBoost = 37,       // 暴击伤害提升
    AttrCritDamageDecrease = 38,    // 暴击伤害降低
    AttrViewDistanceBoost = 39,     // 可见距离提升
    AttrViewDistanceDecrease = 40,  // 可见距离降低
    SpeedUp = 41,                   // 加速
    SlowDown = 42,                  // 减速
}

public static class EffectTypeExt
{
    public static bool IsAttribute(this EffectType type) => type switch
    {
        EffectType.AttrStrengthBoost or EffectType.AttrMagicBoost or
        EffectType.AttrCritRateBoost or EffectType.AttrCritDamageBoost or
        EffectType.AttrViewDistanceBoost or
        EffectType.AttrStrengthDecrease or EffectType.AttrMagicDecrease or
        EffectType.AttrCritRateDecrease or EffectType.AttrCritDamageDecrease or
        EffectType.AttrViewDistanceDecrease => true,
        _ => false,
    };

    public static bool IsControl(this EffectType type) => ConfigManager.EffectConfig.Get((int)type).lockMoveInput == true;

    public static bool IsDoT(this EffectType type) => type is EffectType.Burning or EffectType.Poison or EffectType.PaleFire;

    //固定间隔生效的类型（统一 1s 一跳，见 OnUpdate）
    public static bool IsTicking(this EffectType type) => type is EffectType.Burning or EffectType.Poison or EffectType.PaleFire or EffectType.DeathStroll;

    public static bool IsShield(this EffectType type) => ConfigManager.EffectConfig.Get((int)type).specialCalculate == 1;

    //极性看配表 effect.csv 的「正面效果」列
    public static bool IsNegative(this EffectType type) => ConfigManager.EffectConfig.Get((int)type).positive != true;
}

public class EntityEffectController
{
    private const float TickInterval = 1f; // 固定间隔生效统一 1s 一跳
    private const int MaxSyncLevel = 15;   // 协议 level 字段 4bit

    // 一层运行时：叠加模式 1 下一层一条，其它模式恒为一条
    public struct EffectLayer
    {
        public EffectType type;
        public int serial;                      // 层身份，OnUpdate 快照遍历后按 id 回写/移除
        public int level;
        public float shieldValue;               // 护盾剩余值（仅护盾类，每层独立）
        public ushort sourceId;                 // 施加者 id（DoT 击杀归属）
        public EntityAttribute sourceAttribute; // 施加者属性快照（施加时的独立克隆，不随后续变化）
        public float endTime;                   // 本层结束时间戳（永久 = float.MaxValue）
        public float nextTickTime;              // 本层下次生效时刻
        public bool isNegative;
    }

    public EntityData owner;

    private readonly Dictionary<EffectType, List<EffectLayer>> effects = new();
    private readonly List<EffectType> iterateKeys = new();
    private readonly List<EffectLayer> layerSnapshot = new();
    private readonly List<int> shieldOrder = new();
    private static readonly List<EntityData> s_tickTargets = new();

    private int nextSerial = 1;
    private bool blockMove;              // 任一存在层禁止移动输入
    private bool blockSkill;             // 任一存在层禁止技能输入
    private bool pauseAnim;              // 任一存在层冻结动画
    private EndureType? endureOverride;  // 霸体等级覆盖（多层取最高）

    public void Init(EntityData data)
    {
        owner = data;
        effects.Clear();
        nextSerial = 1;
        RefreshState();
    }

    public void Clear()
    {
        effects.Clear();
        nextSerial = 1;
        if (owner != null) owner.buffDirty = true;
        RefreshState();
    }

    #region//增删查（简易统一入口）
    // 叠加语义完全由 effect.csv「叠加模式」决定，见 ApplyStackMode
    public void AddEffect(EffectType type, int level = 1, float duration = -1f, EntityData source = null)
    {
        if (owner != null) owner.buffDirty = true;
        float endTime = duration < 0f ? float.MaxValue : Time.time + duration;
        if (!effects.TryGetValue(type, out var layers))
        {
            layers = new List<EffectLayer>();
            effects[type] = layers;
        }
        ApplyStackMode(layers, type, level, endTime, source);
        if (layers.Count == 0)
        {
            effects.Remove(type); // 模式 0 的低等级添加不生效，别留下空表
            return;
        }

        if (type.IsAttribute()) RecomputeAttributes();
        RefreshState();
        if (IsMoveSpeedEffect(type)) ApplyAnimSpeedScale();

        // 苍白之光/暗叠层后判定转化（苍白舞者被动：满层转冰/雷、双高转火，见 BattleManagerPassive）；转化出的控制/燃烧归属原施加者
        if (type == EffectType.PaleLight || type == EffectType.PaleDark)
        {
            if (Tool.BattleManager != null) Tool.BattleManager.CheckPaleConversion(owner, FindEntity(GetSourceId(type)));
        }
    }

    public void RemoveEffect(EffectType type)
    {
        if (!effects.Remove(type)) return;
        if (owner != null) owner.buffDirty = true;
        if (type.IsAttribute()) RecomputeAttributes();
        RefreshState();
        if (IsMoveSpeedEffect(type)) ApplyAnimSpeedScale();
    }

    public void RemoveAllNegative()
    {
        iterateKeys.Clear();
        foreach (var pair in effects)
        {
            if (pair.Value.Count > 0 && pair.Value[0].isNegative) iterateKeys.Add(pair.Key);
        }
        for (int i = 0; i < iterateKeys.Count; i++) RemoveEffect(iterateKeys[i]);
    }

    // GM：移除全部 Buff（逐个走 RemoveEffect，保证属性/控制/移速按类重算）
    public void RemoveAll()
    {
        iterateKeys.Clear();
        foreach (var pair in effects) iterateKeys.Add(pair.Key);
        for (int i = 0; i < iterateKeys.Count; i++) RemoveEffect(iterateKeys[i]);
    }

    public bool HasEffect(EffectType type) => effects.TryGetValue(type, out var layers) && layers.Count > 0;

    // 层数 = 各层等级之和
    public int GetLevel(EffectType type)
    {
        if (!effects.TryGetValue(type, out var layers)) return 0;
        int level = 0;
        for (int i = 0; i < layers.Count; i++) level += layers[i].level;
        return level;
    }

    // 以最新施加的一层为准
    public ushort GetSourceId(EffectType type)
    {
        if (!effects.TryGetValue(type, out var layers) || layers.Count == 0) return 0;
        return layers[layers.Count - 1].sourceId;
    }

    // 多层独立计时 → 取剩余时间最长的一层
    public float GetRemainTime(EffectType type)
    {
        if (!effects.TryGetValue(type, out var layers) || layers.Count == 0) return 0f;
        float remain = 0f;
        for (int i = 0; i < layers.Count; i++) remain = Mathf.Max(remain, layers[i].endTime - Time.time);
        return Mathf.Max(0f, remain);
    }
    #endregion

    #region//战斗管线查询（由战斗代码在明确时机调用，非每帧计算）
    public float GetShieldAbsorb()
    {
        float sum = 0f;
        sum += ShieldSumOf(EffectType.Shield);
        sum += ShieldSumOf(EffectType.StatueShield);
        return sum;
    }

    // 护盾逐层抵消：优先扣剩余时间最短的层，扣空即移除该层
    public void ConsumeShield(float amount)
    {
        ConsumeShieldOf(EffectType.Shield, ref amount);
        ConsumeShieldOf(EffectType.StatueShield, ref amount);
    }

    public float GetDamageReduceRate()
    {
        float rate = GetLevel(EffectType.StatueReduce) * 0.25f;
        if (HasEffect(EffectType.PopeGuard)) rate += Config.pope_guard_reduce_rate;
        return Mathf.Clamp01(rate);
    }

    public float GetOutDamageMultiplier()
    {
        float m = 1f + GetLevel(EffectType.BattleFury) * 0.1f;
        if (HasEffect(EffectType.PlagueBless)) m *= 1f + Config.plague_bless_damage_up;
        return m;
    }

    public float GetInDamageMultiplier()
    {
        float m = 1f / (1f + GetLevel(EffectType.BattleFury) * 0.1f);
        if (HasEffect(EffectType.PlagueBless)) m *= 1f - Config.plague_bless_damage_reduce;
        return m;
    }

    // 反弹量 = 实际受到伤害 × 等级 × 系数，向下取整且至少 1；实伤 ≤1 直接不反弹
    public float GetReflectDamage(float receivedDamage)
    {
        int level = GetLevel(EffectType.Reflect);
        if (level <= 0 || receivedDamage <= 1f) return 0f;
        return Mathf.Max(1, Mathf.FloorToInt(receivedDamage * level * Config.reflect_rate));
    }

    public bool IsActionBlocked() => blockMove;

    public bool IsSilenced() => blockSkill;

    public bool CanCastSkill() => !blockMove && !blockSkill;

    public bool CanMove() => !blockMove;

    // 配表「霸体等级覆盖」留空的 Buff 不参与覆盖
    public bool TryGetEndureOverride(out EndureType endure)
    {
        if (endureOverride.HasValue)
        {
            endure = endureOverride.Value;
            return true;
        }
        endure = EndureType.None;
        return false;
    }

    public float GetMoveAnimSpeedMultiplier()
    {
        float m = 1f;
        if (HasEffect(EffectType.SpeedUp)) m *= Config.anim_move_speed_up;
        if (HasEffect(EffectType.SlowDown)) m *= Config.anim_move_speed_down;
        if (HasEffect(EffectType.Mire)) m *= Config.anim_move_speed_mire;
        if (HasEffect(EffectType.DeathStroll)) m *= Config.death_stroll_speed_up;
        if (HasEffect(EffectType.GmSpeedUp)) m *= Config.gm_speed_multiplier;
        return m;
    }
    #endregion

    #region//每帧推进（只做两件事：到期移除、DoT/光环 tick）
    public void OnUpdate()
    {
        if (effects.Count == 0) return;
        // 先快照 key 与层：TickDamage 会走 OnDamaged → 可能施加/移除效果（苍白转化、死亡清空等）
        iterateKeys.Clear();
        iterateKeys.AddRange(effects.Keys);
        foreach (var key in iterateKeys)
        {
            if (!effects.TryGetValue(key, out var layers)) continue;
            layerSnapshot.Clear();
            layerSnapshot.AddRange(layers);
            for (int i = 0; i < layerSnapshot.Count; i++)
            {
                var layer = layerSnapshot[i];
                if (Time.time >= layer.endTime)
                {
                    RemoveLayer(key, layer.serial);
                    continue;
                }
                if (!layer.type.IsTicking() || Time.time < layer.nextTickTime) continue;
                layer.nextTickTime = Time.time + TickInterval;
                if (!WriteBackLayer(key, layer)) continue; // 快照层已被本轮结算移除
                TickDamage(layer);
            }
        }
    }

    private void TickDamage(EffectLayer layer)
    {
        switch (layer.type)
        {
            case EffectType.Burning:
                if (owner != null) owner.OnDamaged(Config.burning_damage_per_tick * layer.level, FindEntity(layer.sourceId), fixedDamage: true, damageType: CombatLabelType.Burning);
                break;
            case EffectType.PaleFire:
                if (owner != null) owner.OnDamaged(Config.burning_damage_per_tick * layer.level, FindEntity(layer.sourceId), fixedDamage: true, damageType: CombatLabelType.PaleFire);
                break;
            case EffectType.Poison:
            {
                if (owner == null) break;
                float stat = SourcePower(layer);
                owner.OnDamaged(Config.poison_damage_ratio * stat * layer.level, FindEntity(layer.sourceId), fixedDamage: true, damageType: CombatLabelType.Poison);
                break;
            }
            case EffectType.DeathStroll:
            {
                if (owner == null) break;
                float stat = SourcePower(layer);
                EntityCamp enemyCamp = EntityCampUtil.HostileOf(owner.camp);
                s_tickTargets.Clear();
                BattleManager.EntityContainer.GetAllInCamp(owner.transform.position, Config.death_stroll_radius, enemyCamp, s_tickTargets);
                for (int i = 0; i < s_tickTargets.Count; i++)
                {
                    if (s_tickTargets[i] != null) s_tickTargets[i].OnDamaged(Config.death_stroll_dot_ratio * stat * layer.level, owner, fixedDamage: true, damageType: CombatLabelType.DeathStroll);
                }
                break;
            }
        }
    }

    // 施加者快照里的 max(力量, 魔力)；无施加者时为 0
    private static float SourcePower(EffectLayer layer)
    {
        return layer.sourceAttribute != null ? Mathf.Max(layer.sourceAttribute.strength, layer.sourceAttribute.magic) : 0f;
    }

    private static EntityData FindEntity(ushort entityId)
    {
        return BattleManager.EntityContainer.Entities.TryGetObject(entityId, out var entity) ? entity : null;
    }
    #endregion

    #region//Local
    // 叠加模式（effect.csv 第 4 列），留空属配表错配
    private static int StackModeOf(EffectType type)
    {
        var mode = ConfigManager.EffectConfig.Get((int)type).stackMode;
        if (mode == null) throw new FormatException($"[配表] effect.csv 第 {(int)type} 行缺少叠加模式");
        return mode.Value;
    }

    private void ApplyStackMode(List<EffectLayer> layers, EffectType type, int level, float endTime, EntityData source)
    {
        int mode = StackModeOf(type);
        switch (mode)
        {
            case 0: ApplyOverwrite(layers, type, level, endTime, source); break;
            case 1: layers.Add(NewLayer(type, level, endTime, source)); break;
            case 2: ApplyAccumulate(layers, type, level, endTime, source); break;
            case 3: ApplyMaxTime(layers, type, endTime, source); break;
            default: throw new FormatException($"[配表] effect.csv 第 {(int)type} 行叠加模式非法：{mode}（只允许 0~3）");
        }
    }

    // 0：高等级覆盖低等级，相同等级刷新时间；低等级添加不生效
    private void ApplyOverwrite(List<EffectLayer> layers, EffectType type, int level, float endTime, EntityData source)
    {
        if (layers.Count == 0)
        {
            layers.Add(NewLayer(type, level, endTime, source));
            return;
        }
        var layer = layers[0];
        if (level < layer.level) return;
        if (level > layer.level)
        {
            layer.level = level;
            if (type.IsShield()) layer.shieldValue = ShieldValueOf(level);
        }
        layer.endTime = endTime;
        ApplySource(ref layer, source);
        layers[0] = layer;
    }

    // 2：刷新时间并叠加等级
    private void ApplyAccumulate(List<EffectLayer> layers, EffectType type, int level, float endTime, EntityData source)
    {
        if (layers.Count == 0)
        {
            layers.Add(NewLayer(type, level, endTime, source));
            return;
        }
        var layer = layers[0];
        layer.level += level;
        layer.endTime = endTime;
        if (type.IsShield()) layer.shieldValue = ShieldValueOf(layer.level);
        ApplySource(ref layer, source);
        layers[0] = layer;
    }

    // 3：强制 1 级，重复添加仅取最大时间
    private void ApplyMaxTime(List<EffectLayer> layers, EffectType type, float endTime, EntityData source)
    {
        if (layers.Count == 0)
        {
            layers.Add(NewLayer(type, 1, endTime, source));
            return;
        }
        var layer = layers[0];
        layer.level = 1;
        layer.endTime = Mathf.Max(layer.endTime, endTime);
        if (type.IsShield()) layer.shieldValue = ShieldValueOf(1);
        ApplySource(ref layer, source);
        layers[0] = layer;
    }

    private EffectLayer NewLayer(EffectType type, int level, float endTime, EntityData source)
    {
        var layer = new EffectLayer()
        {
            type = type,
            serial = nextSerial++,
            level = level,
            shieldValue = type.IsShield() ? ShieldValueOf(level) : 0f,
            endTime = endTime,
            nextTickTime = Time.time + TickInterval,
            isNegative = type.IsNegative(),
        };
        ApplySource(ref layer, source);
        return layer;
    }

    private static void ApplySource(ref EffectLayer layer, EntityData source)
    {
        layer.sourceId = source != null ? source.id : (ushort)0;
        layer.sourceAttribute = source != null && source.floatingAttribute != null ? source.floatingAttribute.Clone() : null;
    }

    private bool WriteBackLayer(EffectType type, EffectLayer layer)
    {
        if (!effects.TryGetValue(type, out var layers)) return false;
        for (int i = 0; i < layers.Count; i++)
        {
            if (layers[i].serial != layer.serial) continue;
            layers[i] = layer;
            return true;
        }
        return false;
    }

    private void RemoveLayer(EffectType type, int serial)
    {
        if (!effects.TryGetValue(type, out var layers)) return;
        for (int i = 0; i < layers.Count; i++)
        {
            if (layers[i].serial != serial) continue;
            layers.RemoveAt(i);
            break;
        }
        if (layers.Count == 0) effects.Remove(type);
        if (owner != null) owner.buffDirty = true;
        if (type.IsAttribute()) RecomputeAttributes();
        RefreshState();
        if (IsMoveSpeedEffect(type)) ApplyAnimSpeedScale();
    }

    private float ShieldSumOf(EffectType type)
    {
        if (!effects.TryGetValue(type, out var layers)) return 0f;
        float sum = 0f;
        for (int i = 0; i < layers.Count; i++) sum += layers[i].shieldValue;
        return sum;
    }

    private void ConsumeShieldOf(EffectType type, ref float amount)
    {
        if (amount <= 0f || !effects.TryGetValue(type, out var layers) || layers.Count == 0) return;
        shieldOrder.Clear();
        for (int i = 0; i < layers.Count; i++) shieldOrder.Add(i);
        shieldOrder.Sort((a, b) => layers[a].endTime.CompareTo(layers[b].endTime));
        for (int i = 0; i < shieldOrder.Count; i++)
        {
            if (amount <= 0f) break;
            int index = shieldOrder[i];
            var layer = layers[index];
            float used = Mathf.Min(layer.shieldValue, amount);
            layer.shieldValue -= used;
            amount -= used;
            layers[index] = layer;
        }
        for (int i = layers.Count - 1; i >= 0; i--)
        {
            if (layers[i].shieldValue > 0f) continue;
            layers.RemoveAt(i);
        }
        if (layers.Count == 0) effects.Remove(type);
        if (owner != null) owner.buffDirty = true;
    }

    // Health / WeaponSlotCount 不允许被 Buff 修改，走到这里属效果配置错误
    private static float UnsupportedAttrField(EntityAttributeDelta.Field field)
    {
        Debug.LogError($"[Buff] 属性通道不支持 {field}（该属性不可被 Buff 修改）");
        return 0f;
    }

    private static bool IsMoveSpeedEffect(EffectType type) =>
        type is EffectType.SpeedUp or EffectType.SlowDown or EffectType.Mire or EffectType.DeathStroll or EffectType.GmSpeedUp;

    private void ApplyAnimSpeedScale()
    {
        if (owner == null || owner.anim == null) return;
        owner.anim.SetMoveSpeedScale(GetMoveAnimSpeedMultiplier());
    }

    // 控制/动画冻结/霸体覆盖全部由配表列驱动，增删层后统一重算
    private void RefreshState()
    {
        bool move = false, skill = false, anim = false;
        EndureType? endure = null;
        foreach (var pair in effects)
        {
            if (pair.Value.Count == 0) continue;
            var cfg = ConfigManager.EffectConfig.Get((int)pair.Key);
            if (cfg.lockMoveInput == true) move = true;
            if (cfg.lockSkillInput == true) skill = true;
            if (cfg.freezeAnimation == true) anim = true;
            if (cfg.superArmorOverride != null)
            {
                var value = (EndureType)cfg.superArmorOverride.Value;
                if (endure == null || value > endure.Value) endure = value;
            }
        }
        bool animChanged = anim != pauseAnim;
        blockMove = move;
        blockSkill = skill;
        pauseAnim = anim;
        endureOverride = endure;
        if (animChanged && owner != null) owner.SetAnimPaused(pauseAnim);
    }

    private void RecomputeAttributes()
    {
        if (owner == null) return;
        float currentHealth = owner.floatingAttribute.health;
        var attr = owner.baseAttribute.Clone(); // 此刻 attr.health 是"生命值上限"
        foreach (var pair in effects)
        {
            if (!pair.Key.IsAttribute()) continue;
            for (int i = 0; i < pair.Value.Count; i++)
            {
                attr.ApplyDelta(new EntityAttributeDelta(FieldFromType(pair.Key), AttributeDeltaOf(pair.Key, pair.Value[i].level)));
            }
        }
        // 夹取上限读 base：生命值上限只由 base 承担（升级时定一次）。当前也无可达的生命类 Buff（不存在 AttrHealth）
        attr.health = Mathf.Clamp(currentHealth, 0f, owner.baseAttribute.health);
        owner.floatingAttribute = attr;
    }

    // 属性增减幅度内部定死：暴击率 / 暴伤按固定点数逐级线性叠加，力量 / 魔法 / 视野按初始值比例
    private float AttributeDeltaOf(EffectType type, int level)
    {
        var field = FieldFromType(type);
        float amount = field switch
        {
            EntityAttributeDelta.Field.CritRate => Config.buff_crit_rate_points * level,
            EntityAttributeDelta.Field.CritDamage => Config.buff_crit_damage_points * level,
            EntityAttributeDelta.Field.Strength => owner.baseAttribute.strength * Config.buff_attr_ratio * level,
            EntityAttributeDelta.Field.Magic => owner.baseAttribute.magic * Config.buff_attr_ratio * level,
            EntityAttributeDelta.Field.ViewDistance => owner.baseAttribute.viewDistance * Config.buff_attr_ratio * level,
            _ => UnsupportedAttrField(field),
        };
        return type.IsNegative() ? -amount : amount;
    }

    // 护盾每层初始值 = 被施加者生命值上限 × 系数 × 该层等级
    private float ShieldValueOf(int level)
    {
        return owner != null ? owner.baseAttribute.health * Config.shield_health_ratio * level : 0f;
    }

    private static EntityAttributeDelta.Field FieldFromType(EffectType type)
    {
        switch (type)
        {
            case EffectType.AttrStrengthBoost:
            case EffectType.AttrStrengthDecrease: return EntityAttributeDelta.Field.Strength;
            case EffectType.AttrMagicBoost:
            case EffectType.AttrMagicDecrease: return EntityAttributeDelta.Field.Magic;
            case EffectType.AttrCritRateBoost:
            case EffectType.AttrCritRateDecrease: return EntityAttributeDelta.Field.CritRate;
            case EffectType.AttrCritDamageBoost:
            case EffectType.AttrCritDamageDecrease: return EntityAttributeDelta.Field.CritDamage;
            case EffectType.AttrViewDistanceBoost:
            case EffectType.AttrViewDistanceDecrease: return EntityAttributeDelta.Field.ViewDistance;
            default:
                Debug.LogError($"[Buff] 未处理的属性 Buff 类型：{type}，已按力量兜底");
                return EntityAttributeDelta.Field.Strength; // 兜底不可达；新增 Attr* 时必须同时加进 IsAttribute 与本映射，否则会静默加错属性
        }
    }

    // 按 type 聚合成一条下发：level = 各层等级之和（协议 4bit，超出部分截断，客户端看不到）
    public void FillDisplayInfo(SCEntityDisplayInfo info)
    {
        if (info == null) return;
        foreach (var pair in effects)
        {
            if (pair.Value.Count == 0) continue;
            int level = 0;
            for (int i = 0; i < pair.Value.Count; i++) level += pair.Value[i].level;
            info.buffs.Add(new SCEntityDisplayInfo.BuffRuntime()
            {
                type = (int)pair.Key,
                level = Mathf.Min(level, MaxSyncLevel),
            });
        }
    }
    #endregion
}
