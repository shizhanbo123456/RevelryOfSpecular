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
        protected virtual bool HasCastAnim => true;
        protected virtual EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;

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
                .GetTransform(entity.ModelInfo, entity.anim, entity.springWeapon);
        }

        // —— 显式参数原语：不解析 SkillContext，参数含义由各技能自己决定 ——

        // 按 id 找实体（服务器权威容器；客户端查不到返回 null）
        protected static EntityData FindEntity(ushort id) => BattleManager.GetEntity(id);

        // 施放入口：按下时先摇攻击动画，动画到攻击帧时再生成上下文（索敌基于位移后的实时位置）、结算并下发表现
        public void BeginCast(EntityData entity, int skillId)
        {
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

        // 数值来源（力量/魔法）与击退力度默认读 skill.csv，个别技能可显式覆盖
        protected AttackData BuildAttack(EntityData entity, float rate, float radius,
            bool breakEndure = false, bool? useMagic = null, Action<EntityEffectController> addEffect = null,
            Action<EntityData> onHit = null, float? knockback = null)
        {
            var config = ConfigManager.SkillConfig.Get(Id);
            int exp = entity != null && entity.skillController != null ? entity.skillController.GetWeaponExp(Id) : 0;
            return AttackData.Create(entity, rate, radius, breakEndure, useMagic ?? (config.valueSource == 1),
                addEffectEvent: addEffect, onHit: onHit, weaponExp: exp, knockbackPower: knockback ?? config.knockback ?? 0f);
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
                int damage = attack.GetDamage(out DamageType type);
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
