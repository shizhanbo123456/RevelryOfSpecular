using Ros.Transport;
using System;
using System.Collections.Generic;
using UnityEngine;
using Ros.Skill.Utils;

namespace Ros.Skill
{
    public enum SkillVfxKind { None, Bullet, RangeMagic, MagicCircle, Shield, Buff, Weapon }

    public abstract class SkillBase
    {
        public abstract int Id { get; }
        public virtual float CastRange => 0f;
        // 索敌口径读 skill.csv 第 19/20 列：夹角 <=0.1 = 不索敌，>179.9 = 不限方向，半径 >9999 = 不限距离
        protected float TargetSector => ConfigManager.SkillConfig.Get(Id).targetSector;
        protected float TargetRadius => ConfigManager.SkillConfig.Get(Id).targetRadius;
        // 攻击动画读 skill.csv 第 18 列，空/0 = 无动画（按下立刻施放）
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

        //服务器使用技能主入口，内部生成上下文，并根据上下文生成轨迹发射子弹，并传出Context给客户端
        public abstract SkillContext SkillLogic(EntityData entity);
        //服务器客户端共用，确保轨迹一致
        public virtual BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            return null;
        }
        protected abstract void OnCast(SkillContext context);
        //客户端主入口，用于表现特效
        public abstract void PlayVFX(SkillContext context);


        #region 框架工具（攻击结算 / 特效播放）
        private static readonly EntityData[] s_hitBuffer = new EntityData[16];

        protected static EntityAnchorInfo HandPos(EntityData entity, bool leftHand = false)
        {
            if (entity == null) return default;
            return (leftHand ? EntityAnchor.LeftHand : EntityAnchor.RightHand)
                .GetTransform(entity.ModelInfo, entity.anim);
        }

        // —— 显式参数原语：不解析 SkillContext，参数含义由各技能自己决定 ——

        // 本次施法的索敌目标：只在服务器 SkillLogic 里解析一次并随包下发，客户端不复算
        protected EntityData ResolveTarget(EntityData entity)
            => TargetSelect.GetNearestEnemy(entity, TargetRadius, TargetSector);

        // 按配表半径收集场内敌对目标（半径 >9999 = 不限距离）；center 由技能决定
        protected void CollectEnemies(EntityData caster, Vector3 center)
            => TargetSelect.SetEnemiesInRangeToBuffer(center, TargetRadius, caster.camp);

        // 按配表半径筛选指定集合（半径 >9999 = 不限距离），如水晶雕像 / 守护雕像 / 珊瑚
        protected void CollectInRange(EntityData caster, IEnumerable<EntityData> bucket)
            => TargetSelect.SetEntitiesInRangeToBuffer(bucket, caster.transform.position, TargetRadius);

        // 按 id 找实体（服务器权威容器；客户端查不到返回 null）
        protected static EntityData FindEntity(ushort id) => BattleManager.GetEntity(id);

        // 施放入口：按下时先摇攻击动画，动画到攻击帧时再生成上下文（索敌基于位移后的实时位置）、结算并下发表现
        public void BeginCast(EntityData entity, int skillId)
        {
            BroadcastSpecial(skillId); // 每次施放请求只播报一次，不走 CastNow（多段攻击帧会重复）
            if (!HasCastAnim || entity.anim == null)
            {
                CastNow(entity, skillId);
                return;
            }
            if (CastAnim == EntityAnim.AttackType.Attack_Weapon_R
                || CastAnim == EntityAnim.AttackType.Attack_Weapon_L
                || CastAnim == EntityAnim.AttackType.Attack_Weapon_R_And_L)
            {
                entity.heldWeapon = Weapon;
            }
            entity.anim.onAttack = _ => CastNow(entity, skillId); // 不置空回调：多段攻击帧逐次结算技能
            entity.anim.DoAttack(CastAnim);
        }

        private void CastNow(EntityData entity, int skillId)
        {
            SkillContext context = SkillLogic(entity);
            OnCast(context);
            if (Tool.NetworkManager != null) Tool.NetworkManager.SendSkillCast(skillId, context);
        }

        // 配表第25列：特殊技能施放时向全客户端播报，客户端据此弹战斗界面的重要事件 banner
        private static void BroadcastSpecial(int skillId)
        {
            if (!BattleManager.AtServer || Tool.NetworkManager == null) return;
            if (!ConfigManager.SkillConfig.Get(skillId).broadcast) return;
            Tool.NetworkManager.SendBattleEvent(new SCBattleEvent
            {
                type = SCBattleEvent.Type.SkillBroadcast,
                textId = skillId,
            });
        }

        // 数值来源（力量/魔法）与击退力度默认读 skill.csv，个别技能可显式覆盖；伤害范围由命中球决定，不在攻击数据里
        protected AttackData BuildAttack(EntityData entity, float rate,
            bool breakEndure = false, bool? useMagic = null, Action<EntityEffectController> addEffect = null,
            Action<EntityData> onHit = null, float? knockback = null)
        {
            var config = ConfigManager.SkillConfig.Get(Id);
            return AttackData.Create(entity, rate, breakEndure, useMagic ?? (config.valueSource == 1),
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

        // 第15列：有特效的技能 = 各特效的缩放倍率（空 = 原大小），无特效近战 = 判定半径
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
                    throw new FormatException($"[配表] skill.csv 技能 {Id} 是无特效近战，第 15 列必须配判定半径");
                return sizes[0];
            }
        }

        // 第16列：同一目标最短再伤害间隔（秒）；留空在解析阶段已填 100 = 只结算一次
        protected float DamageInterval => ConfigManager.SkillConfig.Get(Id).damageInterval;

        // 第17列：伤害倍率。空 = 全 1；只有 1 项 = 所有段共用；多项 = 按段序取
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

        // 组装子弹命中判定参数：命中球取特效预制体，倍率/间隔取配表
        protected BulletHitSpec HitSpec(SkillVfxKind kind, int vfxIndex, float scale)
        {
            var hits = VfxHitSource.GetHits(kind, vfxIndex);
            return new BulletHitSpec(hits, SegmentRates(hits.Length), scale, DamageInterval, $"{kind}[{vfxIndex}]");
        }

        protected static void StrikeSphere(EntityData entity, Vector3 center, float radius, AttackData attack)
        {
#if UNITY_EDITOR
            DamageRangeDebugHost.Sphere(center, radius); // 调试：可视化本次球形伤害判定范围（渐隐渲染）
#endif
            int count = EntityPhysics.OverlapSphere(center, radius, s_hitBuffer);
            for (int i = 0; i < count; i++)
            {
                var target = s_hitBuffer[i];
                if (target == null || !target.Alive) continue;
                if (target.id == entity.id || !EntityCampUtil.IsHostile(entity.camp, target.camp)) continue;
                int damage = attack.GetDamage(out CombatLabelType type);
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
