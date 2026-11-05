using System;
using System.Collections.Generic;
using Ros.Skill.Utils;
using Ros.Transport;
using UnityEngine;

namespace Ros.Skill
{
    public enum SkillVfxKind { None, Bullet, RangeMagic, MagicCircle, Shield, Buff, Weapon }

    public abstract class SkillBase
    {
        public abstract int Id { get; }

        // 射程读 skill.csv 第 19 列：既是弹道瞄准距离，也是 AI 的作战距离；留空 = 0 = 无射程
        public float CastRange => ConfigManager.SkillConfig.Get(Id).castRange;
        // 索敌口径读 skill.csv 第 15/16 列：夹角 <=0.1 = 不索敌，>179.9 = 不限方向，半径 >9999 = 不限距离
        protected float TargetSector => ConfigManager.SkillConfig.Get(Id).targetSector;
        protected float TargetRadius => ConfigManager.SkillConfig.Get(Id).targetRadius;
        // 攻击动画读 skill.csv 第 14 列，空/0 = 无动画（按下立刻施放）
        protected bool HasCastAnim => (int)CastAnim != 0;
        protected EntityAnim.AttackType CastAnim => (EntityAnim.AttackType)ConfigManager.SkillConfig.Get(Id).attackAnim;

        // 武器分类 / 细分 id 读 skill.csv（分类 0 或细分留空 = 无武器）
        public WeaponRef Weapon
        {
            get
            {
                var config = ConfigManager.SkillConfig.Get(Id);
                return new WeaponRef((WeaponCategory)(config.weaponCategory ?? 0), config.weaponSubId ?? -1);
            }
        }

        // 服务器唯一结算入口：直接拿实体干活，不经过任何上下文
        protected abstract void OnCast(EntityData caster);

        // 客户端表现入口：按波次 id 分派，只做表现
        public virtual void PlayWave(SCSkillDisplayContext context) { }

        // 施放入口：按下时先摇攻击动画，动画到攻击帧时才结算（此时索敌/起点基于位移后的实时位置）
        public void BeginCast(EntityData entity, int skillId)
        {
            BroadcastSpecial(skillId); // 每次施放请求只播报一次，不走 CastNow（多段攻击帧会重复）
            if (!HasCastAnim || entity.anim == null)
            {
                OnCast(entity);
                return;
            }
            if (CastAnim == EntityAnim.AttackType.Attack_Weapon_R
                || CastAnim == EntityAnim.AttackType.Attack_Weapon_L
                || CastAnim == EntityAnim.AttackType.Attack_Weapon_R_And_L)
            {
                entity.heldWeapon = Weapon;
            }
            entity.anim.onAttack = _ => OnCast(entity); // 不置空回调：多段攻击帧逐次结算技能
            entity.anim.DoAttack(CastAnim);
        }

        // 发一个波次包：头由基类填，payload 由技能自己按读端同样的顺序写
        protected void SendWave(EntityData caster, int wave, Action<SCSkillDisplayContext> write)
        {
            if (caster == null) throw new ArgumentNullException(nameof(caster), "发波次包需要施法者实体");
            if (wave < 0 || wave > byte.MaxValue) throw new ArgumentOutOfRangeException(nameof(wave), wave, "波次 id 超出 1 字节");
            var context = new SCSkillDisplayContext();
            context.SkillId = (byte)Id;
            context.SkillWave = (byte)wave;
            context.caster = new SkillTarget(caster);
            write(context);
            if (Tool.NetworkManager != null) Tool.NetworkManager.SendSkillCast(context);
            else PlayWave(context); // 单机测试场景没有网络，直接本地播一遍表现
        }

        // 配表第 17 列：特殊技能施放时向全客户端播报，客户端据此弹战斗界面的重要事件 banner
        private void BroadcastSpecial(int skillId)
        {
            if (!BattleManager.AtServer || Tool.NetworkManager == null) return;
            if (!ConfigManager.SkillConfig.Get(skillId).broadcast) return;
            Tool.NetworkManager.SendBattleEvent(new SCBattleEvent
            {
                type = SCBattleEvent.Type.SkillBroadcast,
                textId = skillId,
            });
        }

        #region 框架工具（起点 / 落点 / 攻击结算）
        private static readonly EntityData[] s_hitBuffer = new EntityData[50];

        protected static EntityAnchorInfo HandPos(EntityData entity, bool leftHand = false)
        {
            if (entity == null) return default;
            return (leftHand ? EntityAnchor.LeftHand : EntityAnchor.RightHand)
                .GetTransform(entity.ModelInfo, entity.anim);
        }

        // 起点 = 实体骨骼锚点 + 实体朝向下的固定偏移
        protected static Vector3 OriginPos(EntityData entity, EntityAnchor anchor, Vector3 offset)
        {
            Vector3 p = anchor.GetTransform(entity.ModelInfo, entity.anim).position;
            return p + entity.transform.rotation * offset;
        }

        // 本次施法的索敌目标：服务器结算时解析一次，客户端用包里下发的点位，不复算
        protected EntityData ResolveTarget(EntityData entity)
            => TargetSelect.GetNearestEnemy(entity, TargetRadius, TargetSector);

        // 落点口径（见《03-技能与Buff总表》三章）：不索敌 = 朝面向、水平距离取射程、与起点同高；
        // 索敌 = 朝目标体中心、水平距离仍取射程、高度取体中心
        protected Vector3 Aim(EntityData caster, Vector3 origin)
        {
            if (CastRange <= 0f)
                throw new FormatException($"[配表] skill.csv 技能 {Id} 需要瞄准点，但第 19 列没有配射程");
            Vector3 forward = caster.transform.forward;
            EntityData target = ResolveTarget(caster);
            if (target == null)
                return new Vector3(origin.x + forward.x * CastRange, origin.y, origin.z + forward.z * CastRange);
            Vector3 body = TargetSelect.BodyPos(target);
            Vector3 flat = body - caster.transform.position;
            flat.y = 0f;
            Vector3 dir = flat.sqrMagnitude > 0.0001f ? flat.normalized : forward;
            return new Vector3(origin.x + dir.x * CastRange, body.y, origin.z + dir.z * CastRange);
        }

        // 按配表半径收集场内敌对目标（半径 >9999 = 不限距离）；center 由技能决定
        protected void CollectEnemies(EntityData caster, Vector3 center)
            => TargetSelect.SetEnemiesInRangeToBuffer(center, TargetRadius, caster.type.category);

        // 按配表半径筛选指定集合（半径 >9999 = 不限距离），如水晶雕像 / 守护雕像 / 珊瑚
        protected void CollectInRange(EntityData caster, IEnumerable<EntityData> bucket)
            => TargetSelect.SetEntitiesInRangeToBuffer(bucket, caster.transform.position, TargetRadius);

        // 按 id 找实体（服务器权威容器；客户端查不到返回 null）
        protected static EntityData FindEntity(ushort id) => BattleManager.GetEntity(id);

        // 击退力度默认读 skill.csv，个别技能可显式覆盖；伤害范围由命中球决定，不在攻击数据里
        // 破霸体读第 20 列，伤害倍率读第 13 列：代码不再传这两项
        protected AttackData BuildAttack(EntityData entity,
            Action<EntityEffectController> addEffect = null,
            Action<EntityData> onHit = null, float? knockback = null)
        {
            var config = ConfigManager.SkillConfig.Get(Id);
            return AttackData.Create(entity, config.breakEndure,
                addEffectEvent: addEffect, onHit: onHit, knockbackPower: knockback ?? config.knockback ?? 0f);
        }

        // 技能附带的 Buff 全部读 skill.csv，按 index 取用；越界属配表错配，直接抛
        protected BuffEntry ConfigBuff(int index)
        {
            var buffs = ConfigManager.SkillConfig.Get(Id).buffs;
            if (index < 0 || index >= buffs.Count)
                throw new FormatException($"[配表] skill.csv 技能 {Id} 没有第 {index} 个 Buff（共 {buffs.Count} 个）");
            return buffs[index];
        }

        protected Action<EntityEffectController> ConfigHitBuff(EntityData caster, int index)
        {
            var buff = ConfigBuff(index);
            return effect => effect.AddEffect((EffectType)buff.type, buff.level, buff.duration, caster);
        }

        protected void ApplyConfigBuff(EntityData caster, EntityData target, int index)
        {
            if (target == null || target.effectController == null) return;
            var buff = ConfigBuff(index);
            target.effectController.AddEffect((EffectType)buff.type, buff.level, buff.duration, caster);
        }

        // 第 11 列：有特效的技能 = 各特效的缩放倍率（空 = 原大小），无特效近战 = 判定半径
        private List<float> SizedConfig => ConfigManager.SkillConfig.Get(Id).bulletSizes;

        // 取第 index 个特效的缩放倍率；没配 = 原大小，配了但越界属错配，直接抛
        protected float VfxScale(int index = 0)
        {
            var sizes = SizedConfig;
            if (sizes.Count == 0) return 1f;
            if (index < 0 || index >= sizes.Count)
                throw new FormatException($"[配表] skill.csv 技能 {Id} 只配了 {sizes.Count} 个尺寸，取第 {index} 个越界");
            return sizes[index];
        }

        // 无特效近战的判定半径：只认配表，缺配置立刻抛
        protected float MeleeRadius
        {
            get
            {
                var sizes = SizedConfig;
                if (sizes.Count == 0)
                    throw new FormatException($"[配表] skill.csv 技能 {Id} 是无特效近战，第 11 列必须配判定半径");
                return sizes[0];
            }
        }

        // 第 12 列：同一目标最短再伤害间隔（秒）；留空在解析阶段已填 100 = 只结算一次
        protected float DamageInterval => ConfigManager.SkillConfig.Get(Id).damageInterval;

        // 第 13 列指定项；空 = 全 1，越界属配表错配直接抛（与段倍率共用同一列）
        protected float ConfigDamageRate(int index = 0)
        {
            var rates = ConfigManager.SkillConfig.Get(Id).damageRates;
            if (rates.Count == 0) return 1f;
            if (index < 0 || index >= rates.Count)
                throw new FormatException($"[配表] skill.csv 技能 {Id} 只配了 {rates.Count} 个伤害倍率，取第 {index} 个越界");
            return rates[index];
        }

        // 第 13 列：伤害倍率。空 = 全 1；只有 1 项 = 所有段共用；多项 = 按段序取
        private float[] SegmentRates(int segments)
        {
            var rates = ConfigManager.SkillConfig.Get(Id).damageRates;
            if (rates.Count == 0) return null;
            if (rates.Count == 1)
            {
                var same = new float[segments];
                for (int i = 0; i < segments; i++) same[i] = rates[0];
                return same;
            }
            if (rates.Count < segments)
                throw new FormatException($"[配表] skill.csv 技能 {Id} 伤害倍率只有 {rates.Count} 项，不够 {segments} 段用");
            var arr = new float[segments];
            rates.CopyTo(0, arr, 0, segments);
            return arr;
        }

        // 组装子弹命中判定参数：命中球与存活时长取特效预制体，倍率/间隔取配表
        protected BulletHitSpec HitSpec(SkillVfxKind kind, int vfxIndex, float scale)
        {
            var hits = VfxHitSource.GetHits(kind, vfxIndex);
            return new BulletHitSpec(hits, SegmentRates(hits.Length), scale, DamageInterval,
                VfxHitSource.GetLifeTime(kind, vfxIndex), $"{kind}[{vfxIndex}]");
        }

        // 整段共用同一倍率：二次爆炸这类独立效果只从第 13 列取指定项，与命中球段数无关
        protected BulletHitSpec HitSpec(SkillVfxKind kind, int vfxIndex, float scale, float rate)
        {
            var hits = VfxHitSource.GetHits(kind, vfxIndex);
            var rates = new float[hits.Length];
            for (int i = 0; i < rates.Length; i++) rates[i] = rate;
            return new BulletHitSpec(hits, rates, scale, DamageInterval,
                VfxHitSource.GetLifeTime(kind, vfxIndex), $"{kind}[{vfxIndex}]");
        }

        // 无特效近战的球形判定：伤害倍率同样只认 skill.csv 第 13 列（单段取第 0 项，空 = 1）
        protected void StrikeSphere(EntityData entity, Vector3 center, float radius, AttackData attack)
        {
#if UNITY_EDITOR
            DamageRangeDebugHost.Sphere(center, radius); // 调试：可视化本次球形伤害判定范围（渐隐渲染）
#endif
            float rate = ConfigDamageRate();
            int count = EntityPhysics.OverlapCircleXZ(center, radius, s_hitBuffer);
            for (int i = 0; i < count; i++)
            {
                var target = s_hitBuffer[i];
                if (target == null || !target.Alive) continue;
                if (target.id == entity.id || !EntityCampUtil.IsHostile(entity.type.category, target.camp)) continue;
                int damage = attack.GetDamage(out CombatLabelType type, rate);
                target.ProcessHit(attack, damage, type, center); // 击飞方向取判定球心 → 目标
                if (attack.addEffectEvent != null && target.effectController != null)
                {
                    attack.addEffectEvent.Invoke(target.effectController);
                }
                if (attack.onHit != null) attack.onHit.Invoke(target);
            }
        }
        #endregion
    }
}
