using Ros.Transport;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

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

        protected bool WaitAttackFrame(EntityData entity, EntityAnim.AttackType castAnim, System.Action onFrame)
        {
            if (entity.anim == null) return false;
            if (castAnim == EntityAnim.AttackType.Attack_Weapon_R
                || castAnim == EntityAnim.AttackType.Attack_Weapon_L
                || castAnim == EntityAnim.AttackType.Attack_Weapon_R_And_L)
            {
                entity.heldWeapon = Weapon;
            }
            entity.anim.onAttack = _ => onFrame();
            entity.anim.DoAttack(castAnim);
            return true;
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
            for (int i = 0; i < SkillContextConventions.GetShotCount(context); i++)
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

        #region 常用轨迹构建（CreateTrajectory 的默认实现，技能按需调用）
        [Obsolete]
        protected BulletTrajectory Line(SkillContext context, int index, float duration)
        {
            Vector3 from = SkillContextConventions.GetShotOrigin(this, context, index);
            Vector3 to = SkillContextConventions.GetShotAimPoint(this, context, index);
            var t = new LineTrajectory(from, to);
            t.Duration = duration;
            return t;
        }
        [Obsolete]
        protected static BulletTrajectory Point(SkillContext context, int index, float duration)
        {
            var t = new PointTrajectory(SkillContextConventions.GetShotDestination(context, index));
            t.Duration = duration;
            return t;
        }
        [Obsolete]
        protected BulletTrajectory SkyFall(SkillContext context, int index, float duration, float skyHeight = 30f)
        {
            Vector3 from = SkillContextConventions.GetShotOrigin(this, context, index);
            Vector3 to = SkillContextConventions.GetShotAimPoint(this, context, index);
            var t = new SkyFallTrajectory(from, to, skyHeight);
            t.Duration = duration;
            return t;
        }
        [Obsolete]
        protected BulletTrajectory Arc(SkillContext context, int index, float duration, float height = 8f)
        {
            Vector3 from = SkillContextConventions.GetShotOrigin(this, context, index), to = SkillContextConventions.GetShotAimPoint(this, context, index);
            var t = new BezierTrajectory(from, from + Vector3.up * height, to + Vector3.up * height, to);
            t.Duration = duration;
            return t;
        }
        #endregion

        #region 客户端特效播放（技能 PlayVFX 的默认实现，技能按需调用）
        [Obsolete]
        protected void PlayAlong(SkillContext context, SkillVfxKind kind, int[] vfx)
        {
            if (Tool.VfxManager == null || kind == SkillVfxKind.None) return;
            for (int i = 0; i < SkillContextConventions.GetShotCount(context); i++)
            {
                var trajectory = CreateTrajectory(context, i);
                if (trajectory != null) PlayOne(kind, vfx[Mathf.Min(i, vfx.Length - 1)], trajectory);
            }
        }

        [Obsolete]
        protected void PlayFollow(SkillVfxKind kind, int index, ushort entityId, float lifeTime = 0f)
        {
            if (Tool.VfxManager == null || kind == SkillVfxKind.None) return;
            var trajectory = new FollowTrajectory(entityId);
            if (lifeTime > 0f) trajectory.Duration = lifeTime;
            PlayOne(kind, index, trajectory);
        }

        [Obsolete]
        protected void PlayFollowAll(SkillContext context, SkillVfxKind kind, int index, float lifeTime = 0f)
        {
            if (Tool.VfxManager == null || kind == SkillVfxKind.None) return;
            for (int i = 0; i < SkillContextConventions.TargetCount(context); i++) PlayFollow(kind, index, SkillContextConventions.GetTargetId(context, i), lifeTime);
        }

        [Obsolete]
        protected void PlayAt(SkillVfxKind kind, int index, Vector3 pos, float duration)
        {
            if (Tool.VfxManager == null || kind == SkillVfxKind.None || index < 0) return;
            switch (kind)
            {
                case SkillVfxKind.Bullet:
                    Tool.VfxManager.PlayBulletVFX(index, pos, Quaternion.identity, duration);
                    break;
                case SkillVfxKind.Shield:
                    Tool.VfxManager.PlayShieldVFX(index, pos, Quaternion.identity, duration);
                    break;
                case SkillVfxKind.Buff:
                    Tool.VfxManager.PlayBuffVFX(index, pos, Quaternion.identity, duration);
                    break;
                case SkillVfxKind.Weapon:
                    Tool.VfxManager.PlayWeaponVFX(Weapon, pos, Quaternion.identity, duration);
                    break;
                case SkillVfxKind.RangeMagic:
                    Tool.VfxManager.PlayRangeMagicVFX(index, pos, Quaternion.identity, duration);
                    break;
                case SkillVfxKind.MagicCircle:
                    Tool.VfxManager.PlayMagicCircleVFX(index, pos, duration);
                    break;
            }
        }

        [Obsolete]
        private void PlayOne(SkillVfxKind kind, int index, BulletTrajectory trajectory)
        {
            float life = trajectory.Duration;
            switch (kind)
            {
                case SkillVfxKind.Weapon:
                    Tool.VfxManager.PlayWeaponVFX(Weapon, trajectory);
                    break;
                case SkillVfxKind.Bullet when index >= 0:
                    Tool.VfxManager.PlayBulletVFX(index, trajectory);
                    break;
                case SkillVfxKind.Shield when index >= 0:
                    Tool.VfxManager.PlayShieldVFX(index, trajectory);
                    break;
                case SkillVfxKind.Buff when index >= 0:
                    Tool.VfxManager.PlayBuffVFX(index, trajectory);
                    break;
                case SkillVfxKind.RangeMagic:
                    Tool.VfxManager.PlayRangeMagicVFX(index, trajectory, life);
                    break;
                case SkillVfxKind.MagicCircle:
                    Tool.VfxManager.PlayMagicCircleVFX(index, trajectory, life);
                    break;
            }
        }
        #endregion
        [Obsolete]
        internal static class SkillContextConventions
        {
            public const int CasterIdIndex = 0;
            public const int PatternIndex = 1;
            public const int SlotIndex = 2;

            public static SkillContext BuildShotContext(SkillBase skill, EntityData entity, ProjectilePattern pattern, params Vector3[] dests)
            {
                var context = new SkillContext();
                int slot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
                context.AddInts(entity.id, (int)pattern, slot < 0 ? 0 : slot);
                Vector3 origin = skill.GetWeaponFloatPosition(entity);
                foreach (var dest in dests) context.AddVectors(origin, dest);
                return context;
            }

            public static ushort GetCasterId(SkillContext context) =>
                context != null && context.ints.Count > CasterIdIndex ? (ushort)context.ints[CasterIdIndex] : (ushort)0;

            public static int GetCastingSlotIndex(SkillContext context) =>
                context != null && context.ints.Count > SlotIndex ? context.ints[SlotIndex] : 0;

            // 每发 {起点, 终点} 成对存于 vectors；终点冻结于前摇瞬间，供敌人前摇闪避
            public static Vector3 GetShotDestination(SkillContext context, int index) => context.vectors[index * 2 + 1];

            public static int GetShotCount(SkillContext context) =>
                context != null && context.vectors != null ? context.vectors.Count / 2 : 0;

            // 起点在攻击帧实时取：服务器用实体，客户端按同规则镜像（自己的弹簧/碰撞体），保证弹道同源
            public static Vector3 GetShotOrigin(SkillBase skill, SkillContext context, int index)
            {
                ushort casterId = GetCasterId(context);
                int slot = GetCastingSlotIndex(context);
                EntityData caster = GetCasterById(context);
                if (caster != null) return skill.GetWeaponFloatPosition(caster, slot);
                var players = Tool.ClientLogicManager != null ? Tool.ClientLogicManager.EntityPlayers : null;
                if (players == null) return Vector3.zero;
                return players.TryGetShotOrigin(casterId, slot, skill.Weapon.IsValid, out var pos) ? pos : Vector3.zero;
            }

            // 弹道终点：实时起点 + 前摇冻结的「起点→终点」偏移（偏移 Y 清零 → 水平）。
            // 起点随攻击动画位移实时取，弹道整体随位移前移；偏移（方向/距离）仍冻结，敌人仍可闪避。
            public static Vector3 GetShotAimPoint(SkillBase skill, SkillContext context, int index)
            {
                Vector3 origin = GetShotOrigin(skill, context, index);
                Vector3 offset = context.vectors[index * 2 + 1] - context.vectors[index * 2];
                offset.y = 0f;
                return origin + offset;
            }

            public static void AddTargets(SkillContext context, List<EntityData> targets)
            {
                if (context == null || targets == null) return;
                for (int i = 0; i < targets.Count; i++)
                    if (targets[i] != null) context.AddInts(targets[i].id);
            }

            // 多目标布局：ints[0]=施放者，目标从 ints[1] 起
            public static int TargetCount(SkillContext context) =>
                context != null && context.ints != null && context.ints.Count > 1 ? context.ints.Count - 1 : 0;

            public static ushort GetTargetId(SkillContext context, int index) => (ushort)context.ints[index + 1];

            public static EntityData GetCasterById(SkillContext context, int idIndex = CasterIdIndex)
            {
                if (context == null || idIndex < 0 || idIndex >= context.ints.Count) return null;
                return BattleManager.GetEntity((ushort)context.ints[idIndex]);
            }
        }
    }
}
