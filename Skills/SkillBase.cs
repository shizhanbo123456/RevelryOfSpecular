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

        #region 通用工具（服务端/客户端共用，保证伤害与特效一致）

        protected static Vector3[] FanDests(Vector3 pos, Vector3 dest, int count, float spreadDeg)
        {
            Vector3 forward = dest - pos;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            float dist = Vector3.Distance(pos, dest);
            var dests = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                float t = count <= 1 ? 0f : i / (float)(count - 1) - 0.5f;
                Vector3 dir = (forward + right * Mathf.Tan(t * spreadDeg * Mathf.Deg2Rad)).normalized;
                dests[i] = pos + dir * dist;
            }
            return dests;
        }

        protected static Vector3[] CircleDests(Vector3 center, float radius, int count, float startAngleDeg = 0f)
        {
            var dests = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                float angle = (startAngleDeg + i * 360f / count) * Mathf.Deg2Rad;
                dests[i] = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            }
            return dests;
        }

        protected static Vector3 RotateTargetAroundDirection(Vector3 pos, Vector3 dest, float angleDeg)
        {
            Vector3 forward = dest - pos;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            forward.Normalize();
            Vector3 dir = Quaternion.Euler(0f, angleDeg, 0f) * forward;
            return pos + dir * Vector3.Distance(pos, dest);
        }

        protected static EntityData GetNearestEnemy(EntityData entity, float radius = 10f, bool frontSector = true)
        {
            if (entity == null) return null;
            if (frontSector)
                return BattleManager.EntityContainer.GetNearestEnemyInFront(entity, radius, Config.default_skill_auto_target_sector_half_angle);
            return BattleManager.EntityContainer.GetNearestEnemy(entity, radius);
        }

        protected static EntityData GetNearestAlly(EntityData entity, float radius = Config.default_skill_auto_target_radius)
        {
            if (entity == null) return null;
            return BattleManager.EntityContainer.GetNearestInCamp(
                entity.transform.position, radius, entity.camp, entity.id);
        }

        protected static void SetEntitiesInCampToBuffer(EntityCamp camp)
        {
            TargetBuffer.Clear();
            foreach (var e in BattleManager.EntityContainer.Entities)
            {
                if (e != null && e.Alive && (camp & e.camp) != 0) TargetBuffer.Add(e);
            }
        }

        protected static void SetEntitiesOfCategoryToBuffer(EntityCategory category)
        {
            TargetBuffer.Clear();
            foreach (var e in BattleManager.EntityContainer.Entities)
            {
                if (e != null && e.Alive && e.type.category == category) TargetBuffer.Add(e);
            }
        }

        protected static EntityCamp HostileOf(EntityCamp camp) => EntityCampUtil.HostileOf(camp);

        protected static void SetEntitiesToBuffer(IEnumerable<EntityData> bucket)
        {
            TargetBuffer.Clear();
            foreach (var e in bucket)
            {
                if (e != null && e.Alive) TargetBuffer.Add(e);
            }
        }

        protected static void SetEntitiesInRangeToBuffer(IEnumerable<EntityData> bucket, Vector3 pos, float radius)
        {
            TargetBuffer.Clear();
            float sqr = radius * radius;
            foreach (var e in bucket)
            {
                if (e != null && e.Alive && (e.transform.position - pos).sqrMagnitude <= sqr) TargetBuffer.Add(e);
            }
        }

        protected static EntityData SelectNearest(IEnumerable<EntityData> bucket, Vector3 pos, float radius,
            ushort excludeId = 0)
        {
            EntityData best = null;
            float bestSqr = radius * radius;
            foreach (var e in bucket)
            {
                if (e == null || !e.Alive || e.id == excludeId) continue;
                float sqr = (e.transform.position - pos).sqrMagnitude;
                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    best = e;
                }
            }
            return best;
        }

        protected static void SetEnemiesInRangeToBuffer(Vector3 center, float radius, EntityCamp ownCamp)
        {
            TargetBuffer.Clear();
            EntityCamp hostile = EntityCampUtil.HostileOf(ownCamp);
            float sqr = radius * radius;
            foreach (var e in BattleManager.EntityContainer.Entities)
            {
                if (e == null || !e.Alive || (hostile & e.camp) == 0) continue;
                if ((e.transform.position - center).sqrMagnitude <= sqr) TargetBuffer.Add(e);
            }
        }

        protected static Vector3 SummonOffset(int index, float radius = 2f)
        {
            float angle = index * 72f * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }

        protected virtual Vector3 AimPos(EntityData entity, bool normalizeDistance = true, bool forceHorizontal = true)
        {
            float view = entity.floatingAttribute.viewDistance;
            var target = GetNearestEnemy(entity, view);
            if (target == null)
            {
                return entity.transform.position + entity.transform.forward * Config.default_forward_aim_distance;
            }
            else
            {
                if (normalizeDistance)
                {
                    var offset = target.transform.position - entity.transform.position;
                    if(forceHorizontal)
                        offset.y = 0;
                    Vector3 dir = offset.normalized;
                    return entity.transform.position + dir * Config.default_forward_aim_distance;
                }
                else
                {
                    var dest = target.transform.position;
                    if(forceHorizontal)
                        dest.y = entity.transform.position.y;
                    return dest;
                }
            }
        }
        #endregion

        #region 框架工具（攻击结算 / 特效播放）
        private static readonly EntityData[] s_hitBuffer = new EntityData[16];

        protected static readonly List<EntityData> TargetBuffer = new();

        protected static Vector3 GetWeaponFloatPositionById(ushort casterId, int slot)
        {
            if (BulletTrajectory.TryGetEntityTransform(casterId, out var pos, out var rot))
                return pos + rot * Config.GetWeaponFloatOffset(slot < 0 ? 0 : slot);
            return pos;
        }

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
        protected BulletTrajectory Line(SkillContext context, int index, float duration)
        {
            var t = new LineTrajectory(SkillContextConventions.GetShotOrigin(this, context, index), SkillContextConventions.GetShotDestination(context, index));
            t.Duration = duration;
            return t;
        }

        protected static BulletTrajectory Point(SkillContext context, int index, float duration)
        {
            var t = new PointTrajectory(SkillContextConventions.GetShotDestination(context, index));
            t.Duration = duration;
            return t;
        }

        protected BulletTrajectory SkyFall(SkillContext context, int index, float duration, float skyHeight = 30f)
        {
            var t = new SkyFallTrajectory(SkillContextConventions.GetShotOrigin(this, context, index), SkillContextConventions.GetShotDestination(context, index), skyHeight);
            t.Duration = duration;
            return t;
        }

        protected BulletTrajectory Arc(SkillContext context, int index, float duration, float height = 8f)
        {
            Vector3 from = SkillContextConventions.GetShotOrigin(this, context, index), to = SkillContextConventions.GetShotDestination(context, index);
            var t = new BezierTrajectory(from, from + Vector3.up * height, to + Vector3.up * height, to);
            t.Duration = duration;
            return t;
        }
        #endregion

        #region 客户端特效播放（技能 PlayVFX 的默认实现，技能按需调用）
        protected void PlayAlong(SkillContext context, SkillVfxKind kind, int[] vfx)
        {
            if (Tool.VfxManager == null || kind == SkillVfxKind.None) return;
            for (int i = 0; i < SkillContextConventions.GetShotCount(context); i++)
            {
                var trajectory = CreateTrajectory(context, i);
                if (trajectory != null) PlayOne(kind, vfx[Mathf.Min(i, vfx.Length - 1)], trajectory);
            }
        }

        protected void PlayFollow(SkillVfxKind kind, int index, ushort entityId, float lifeTime = 0f)
        {
            if (Tool.VfxManager == null || kind == SkillVfxKind.None) return;
            var trajectory = new FollowTrajectory(entityId);
            if (lifeTime > 0f) trajectory.Duration = lifeTime;
            PlayOne(kind, index, trajectory);
        }

        protected void PlayFollowAll(SkillContext context, SkillVfxKind kind, int index, float lifeTime = 0f)
        {
            if (Tool.VfxManager == null || kind == SkillVfxKind.None) return;
            for (int i = 0; i < SkillContextConventions.TargetCount(context); i++) PlayFollow(kind, index, SkillContextConventions.GetTargetId(context, i), lifeTime);
        }

        protected void PlayAt(SkillVfxKind kind, int index, Vector3 pos, float duration)
        {
            if (Tool.VfxManager == null || kind == SkillVfxKind.None || index < 0) return;
            switch (kind)
            {
                case SkillVfxKind.RangeMagic:
                    Tool.VfxManager.PlayRangeMagicVFX(index, pos, Quaternion.identity, duration);
                    break;
                case SkillVfxKind.MagicCircle:
                    Tool.VfxManager.PlayMagicCircleVFX(index, pos, duration);
                    break;
            }
        }

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
                    Tool.VfxManager.PlayRangeMagicVFX(index, trajectory.Lerp(1f), Quaternion.identity, life);
                    break;
                case SkillVfxKind.MagicCircle:
                    Tool.VfxManager.PlayMagicCircleVFX(index, trajectory.Lerp(1f), life);
                    break;
            }
        }
        #endregion

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

            // 起点在攻击帧实时取：服务器用实体、客户端按 casterId 取，攻击动画位移自然生效
            public static Vector3 GetShotOrigin(SkillBase skill, SkillContext context, int index)
            {
                ushort casterId = GetCasterId(context);
                int slot = GetCastingSlotIndex(context);
                EntityData caster = GetCasterById(context);
                if (caster != null) return skill.GetWeaponFloatPosition(caster, slot);
                return GetWeaponFloatPositionById(casterId, slot);
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
