using Ros.Transport;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using Ros.Skill.Utils;

namespace Ros.Skill
{
    public enum ProjectilePattern { Line, Point, Bezier, SkyFall }

    public enum SkillVfxKind { None, Bullet, RangeMagic, MagicCircle, Shield, Buff, Weapon }

    public abstract class SkillBase
    {
        public abstract int Id { get; }
        public abstract float CD { get; }
        public abstract int Store { get; }
        public virtual float CastRange => 0f;
        public virtual WeaponRef Weapon => WeaponRef.None;

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

        protected Vector3 GetWeaponFloatPosition(EntityData entity, int slot)
        {
            if (!Weapon.IsValid) return entity.BulletShootPos();
            return entity.GetWeaponFloatPos(slot);
        }

        protected Vector3 GetWeaponFloatPosition(EntityData entity)
        {
            int slot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            return GetWeaponFloatPosition(entity, slot < 0 ? 0 : slot);
        }

        // 技能是否带攻击动画：无动画技能（如固定炮台/瘟疫树）按下即施放，不等攻击帧
        protected virtual bool HasCastAnim => true;
        // 攻击动画类型（HasCastAnim 为真时生效）
        protected virtual EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;

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

        protected AttackData BuildAttack(EntityData entity, float rate, float radius,
            bool breakEndure = false, bool useMagic = false, Action<EntityEffectController> addEffect = null,
            Action<EntityData> onHit = null, float knockback = 0f)
        {
            int exp = entity != null && entity.skillController != null ? entity.skillController.GetWeaponExp(Id) : 0;
            return AttackData.Create(entity, rate, radius, breakEndure, useMagic,
                addEffectEvent: addEffect, onHit: onHit, weaponExp: exp, knockbackPower: knockback);
        }

        protected static Action<EntityEffectController> ApplyEffect(EffectType type, int level, float duration,
            bool negative = false, float value = 0f, float damage = 0f, float shieldValue = 0f, ushort sourceId = 0)
        {
            return effect => effect.AddEffect(type, level, duration, negative,
                new EntityEffectController.EffectPayload
                {
                    value = value,
                    damage = damage,
                    shieldValue = shieldValue,
                    sourceId = sourceId,
                });
        }

        protected static void GiveEffect(EntityData target, EffectType type, int level, float duration,
            bool negative = false, float value = 0f, float damage = 0f, float shieldValue = 0f, ushort sourceId = 0)
        {
            if (target == null || target.effectController == null) return;
            target.effectController.AddEffect(type, level, duration, negative,
                new EntityEffectController.EffectPayload
                {
                    value = value,
                    damage = damage,
                    shieldValue = shieldValue,
                    sourceId = sourceId,
                });
        }

        protected static Action<EntityData> PullTo(Vector3 to, float speed = 16f)
        {
            return target => { if (target != null) target.SetMotion(new MotionToPoint(to, speed)); };
        }

        protected static bool TryGetPos(ushort entityId, out Vector3 pos) =>
            BulletTrajectory.TryGetEntityPosition(entityId, out pos);

        protected static bool TryGetTransform(ushort entityId, out Vector3 pos, out Quaternion rot) =>
            BulletTrajectory.TryGetEntityTransform(entityId, out pos, out rot);

        protected static Vector3 HandPos(EntityData entity, bool leftHand = false)
        {
            var mount = entity != null && entity.anim != null ? entity.anim.GetHandMount(leftHand) : null;
            return mount != null ? mount.position : entity.transform.position;
        }

        protected void ShootAll(EntityData entity, SkillContext context, AttackData attack)
        {
            int count = context != null && context.vectors != null ? context.vectors.Count / 2 : 0;
            for (int i = 0; i < count; i++)
            {
                if (Tool.BattleManager != null) Tool.BattleManager.ShootBullet(entity, attack, CreateTrajectory(context, i));
            }
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
                int damage = attack.GetDamage(out bool isCrit);
                target.ProcessHit(attack, damage, isCrit, center); // 击飞方向取判定球心 → 目标
                if (attack.addEffectEvent != null && target.effectController != null)
                {
                    attack.addEffectEvent.Invoke(target.effectController);
                }
                if (attack.onHit != null) attack.onHit.Invoke(target);
            }
        }
        #endregion

        #region 上下文构建与双端解析（替代已弃用的 SkillContextConventions）
        // 弹道技能标准布局：ints[0]=施放者id，ints[1]=武器槽位；vectors 每 2 个 = 一发的(冻结起点, 冻结终点)
        protected SkillContext BuildShotContext(EntityData entity, params Vector3[] dests)
        {
            var context = new SkillContext();
            int slot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (slot < 0) slot = 0;
            context.AddInts(entity.id, slot);
            Vector3 origin = GetWeaponFloatPosition(entity, slot);
            foreach (var dest in dests) context.AddVectors(origin, dest);
            return context;
        }

        protected static EntityData GetCaster(SkillContext context)
            => context != null && context.ints.Count > 0 ? BattleManager.GetEntity((ushort)context.ints[0]) : null;

        // 双端实时起点：服务器取实体武器槽，客户端按同一规则镜像（弹簧/碰撞体）
        protected Vector3 GetShotOrigin(SkillContext context, int index)
        {
            ushort casterId = context != null && context.ints.Count > 0 ? (ushort)context.ints[0] : (ushort)0;
            int slot = context != null && context.ints.Count > 1 ? context.ints[1] : 0;
            EntityData caster = BattleManager.GetEntity(casterId);
            if (caster != null) return GetWeaponFloatPosition(caster, slot);
            var players = Tool.ClientLogicManager != null ? Tool.ClientLogicManager.EntityPlayers : null;
            return players != null && players.TryGetShotOrigin(casterId, slot, Weapon.IsValid, out var pos) ? pos : Vector3.zero;
        }

        // 弹道终点：实时起点 + 前摇冻结的「起点→终点」偏移（Y 清零，敌人可闪避、动画位移生效）
        protected Vector3 GetShotAim(SkillContext context, int index)
        {
            Vector3 origin = GetShotOrigin(context, index);
            if (context == null || context.vectors == null || index * 2 + 1 >= context.vectors.Count) return origin;
            Vector3 offset = context.vectors[index * 2 + 1] - context.vectors[index * 2];
            offset.y = 0f;
            return origin + offset;
        }

        // 目标列表技能：把目标 id 逐个写入上下文（ints[1..]）
        protected static void AddTargetIds(SkillContext context, List<EntityData> targets)
        {
            if (context == null || targets == null) return;
            for (int i = 0; i < targets.Count; i++)
                if (targets[i] != null) context.AddInts(targets[i].id);
        }

        // 逐发轨迹枚举（服务器结算与客户端的 VfxHelper.PlayAlong 共用，保证双端一致）
        protected IEnumerable<BulletTrajectory> ShotTrajectories(SkillContext context)
        {
            int count = context != null && context.vectors != null ? context.vectors.Count / 2 : 0;
            for (int i = 0; i < count; i++) yield return CreateTrajectory(context, i);
        }

        // 弹道类 Vfx 便捷封装：直转 VfxHelper.PlayAlong（vfx 长度不足时循环复用首个下标）
        protected void PlayShotVfx(SkillContext context, SkillVfxKind kind, params int[] vfx)
        {
            int count = context != null && context.vectors != null ? context.vectors.Count / 2 : 0;
            if (count <= 0 || vfx == null || vfx.Length == 0) return;
            var vfxEnum = vfx.Length == 1 ? Enumerable.Repeat(vfx[0], count) : vfx.Take(count);
            VfxHelper.PlayAlong(kind, vfxEnum.GetEnumerator(), ShotTrajectories(context).GetEnumerator());
        }

        // 群体跟随 Vfx 便捷封装：目标取自上下文 ints[1..]
        protected void PlayTargetsVfx(SkillContext context, SkillVfxKind kind, int index, float lifeTime = 0f)
        {
            if (context == null || context.ints.Count <= 1) return;
            VfxHelper.PlayFollowAll(kind, index, context.ints.Skip(1).GetEnumerator(), lifeTime);
        }
        #endregion
    }
}
