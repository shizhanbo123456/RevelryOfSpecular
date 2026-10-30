using Ros.Transport;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ros.Skill.Utils
{
    public static class SpreadStyle
    {
        public static Vector3[] FanDests(Vector3 pos, Vector3 dest, int count, float spreadDeg)
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

        public static Vector3[] CircleDests(Vector3 center, float radius, int count, float startAngleDeg = 0f)
        {
            var dests = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                float angle = (startAngleDeg + i * 360f / count) * Mathf.Deg2Rad;
                dests[i] = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            }
            return dests;
        }

        public static Vector3 RotateTargetAroundDirection(Vector3 pos, Vector3 dest, float angleDeg)
        {
            Vector3 forward = dest - pos;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            forward.Normalize();
            Vector3 dir = Quaternion.Euler(0f, angleDeg, 0f) * forward;
            return pos + dir * Vector3.Distance(pos, dest);
        }
    }
    public static class TargetSelect
    {
        public static readonly List<EntityData> TargetBuffer = new();

        // 索敌口径统一读 skill.csv：夹角 <=0.1 不索敌，>179.9 不限方向，半径 >9999 不限距离
        public static EntityData GetNearestEnemy(EntityData entity, float radius, float sectorHalfAngle)
            => BattleManager.EntityContainer.GetNearestEnemy(entity, radius, sectorHalfAngle);

        public static void SetEntitiesInCampToBuffer(EntityCamp camp)
        {
            TargetBuffer.Clear();
            foreach (var e in BattleManager.EntityContainer.Entities)
            {
                if (e != null && e.Alive && (camp & e.camp) != 0) TargetBuffer.Add(e);
            }
        }

        public static void SetEntitiesOfCategoryToBuffer(EntityCategory category)
        {
            TargetBuffer.Clear();
            foreach (var e in BattleManager.EntityContainer.Entities)
            {
                if (e != null && e.Alive && e.type.category == category) TargetBuffer.Add(e);
            }
        }

        public static EntityCamp HostileOf(EntityCamp camp) => EntityCampUtil.HostileOf(camp);

        public static void SetEntitiesToBuffer(IEnumerable<EntityData> bucket)
        {
            TargetBuffer.Clear();
            foreach (var e in bucket)
            {
                if (e != null && e.Alive) TargetBuffer.Add(e);
            }
        }

        public static void SetEntitiesInRangeToBuffer(IEnumerable<EntityData> bucket, Vector3 pos, float radius)
        {
            TargetBuffer.Clear();
            bool unlimited = radius > Config.skill_target_unlimited_radius;
            float sqr = radius * radius;
            foreach (var e in bucket)
            {
                if (e == null || !e.Alive) continue;
                if (!unlimited && (e.transform.position - pos).sqrMagnitude > sqr) continue;
                TargetBuffer.Add(e);
            }
        }

        public static EntityData SelectNearest(IEnumerable<EntityData> bucket, Vector3 pos, float radius,
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

        public static void SetEnemiesInRangeToBuffer(Vector3 center, float radius, EntityCamp ownCamp)
        {
            TargetBuffer.Clear();
            EntityCamp hostile = EntityCampUtil.HostileOf(ownCamp);
            bool unlimited = radius > Config.skill_target_unlimited_radius;
            float sqr = radius * radius;
            foreach (var e in BattleManager.EntityContainer.Entities)
            {
                if (e == null || !e.Alive || (hostile & e.camp) == 0) continue;
                if (!unlimited && (e.transform.position - center).sqrMagnitude > sqr) continue;
                TargetBuffer.Add(e);
            }
        }

        public static Vector3 SummonOffset(int index, float radius = 2f)
        {
            float angle = index * 72f * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }

        // 体中心（脚部以上）：索敌取点用这个，不能锁 transform.position（那是脚）
        public static Vector3 BodyPos(EntityData entity)
            => EntityAnchor.UpCenter.GetTransform(entity.ModelInfo, entity.anim).position;

        // 索敌命中点：夹角 <=0.1 或范围内无敌人 → 朝自身面向 fallbackDistance；否则朝目标体中心、水平距离 = fallbackDistance
        public static Vector3 AimPos(EntityData entity, float sectorHalfAngle, float radius, float fallbackDistance,
            bool normalizeDistance = true, bool forceHorizontal = true)
        {
            var target = GetNearestEnemy(entity, radius, sectorHalfAngle);
            if (target == null)
            {
                return entity.transform.position + entity.transform.forward * fallbackDistance;
            }
            Vector3 body = BodyPos(target);
            Vector3 offset = body - entity.transform.position;
            if (forceHorizontal)
                offset.y = 0;
            Vector3 dir = offset.sqrMagnitude > 0.0001f ? offset.normalized : entity.transform.forward;
            if (!normalizeDistance) return body; // 自身位置即落点：取体中心
            // 方向水平、高度取目标体中心（不落到脚上）
            return new Vector3(entity.transform.position.x + dir.x * fallbackDistance, body.y,
                entity.transform.position.z + dir.z * fallbackDistance);
        }
    }
    public static class TrialConstructor
    {
        public static BulletTrajectory Line(Vector3 from,Vector3 to, float duration)
        {
            var t = new LineTrajectory(from, to);
            t.Duration = duration;
            return t;
        }

        public static BulletTrajectory Point(Vector3 point, float duration)
        {
            var t = new PointTrajectory(point);
            t.Duration = duration;
            return t;
        }

        public static BulletTrajectory SkyFall(Vector3 from, Vector3 to, float duration, float skyHeight = 30f)
        {
            var t = new SkyFallTrajectory(from, to, skyHeight);
            t.Duration = duration;
            return t;
        }

        public static BulletTrajectory Arc(Vector3 from,Vector3 to, float duration, float height = 8f)
        {
            var t = new BezierTrajectory(from, from + Vector3.up * height, to + Vector3.up * height, to);
            t.Duration = duration;
            return t;
        }
    }
    public static class VfxHelper
    {
        public static void PlayAt(SkillVfxKind kind, int index, Vector3 pos, float duration, float scale = 0f)
        {
            if (Tool.VfxManager == null || kind == SkillVfxKind.None || index < 0) return;
            switch (kind)
            {
                case SkillVfxKind.Bullet:
                    Tool.VfxManager.PlayBulletVFX(index, pos, Quaternion.identity, duration, scale);
                    break;
                case SkillVfxKind.Shield:
                    Tool.VfxManager.PlayShieldVFX(index, pos, Quaternion.identity, duration, scale);
                    break;
                case SkillVfxKind.Buff:
                    Tool.VfxManager.PlayBuffVFX(index, pos, Quaternion.identity, duration, scale);
                    break;
                case SkillVfxKind.Weapon:
                    Tool.VfxManager.PlayWeaponVFX(index, pos, Quaternion.identity, duration, scale);
                    break;
                case SkillVfxKind.RangeMagic:
                    Tool.VfxManager.PlayRangeMagicVFX(index, pos, Quaternion.identity, duration, scale);
                    break;
                case SkillVfxKind.MagicCircle:
                    Tool.VfxManager.PlayMagicCircleVFX(index, pos, duration, scale);
                    break;
            }
        }

        public static void PlayOne(SkillVfxKind kind, int index, BulletTrajectory trajectory, float scale = 0f)
        {
            float life = trajectory.Duration;
            switch (kind)
            {
                case SkillVfxKind.Weapon:
                    Tool.VfxManager.PlayWeaponVFX(index, trajectory, 0f, BulletPlayer.RotationMode.Tangent, scale);
                    break;
                case SkillVfxKind.Bullet when index >= 0:
                    Tool.VfxManager.PlayBulletVFX(index, trajectory, 0f, BulletPlayer.RotationMode.Tangent, scale);
                    break;
                case SkillVfxKind.Shield when index >= 0:
                    Tool.VfxManager.PlayShieldVFX(index, trajectory, 0f, BulletPlayer.RotationMode.Constant, scale);
                    break;
                case SkillVfxKind.Buff when index >= 0:
                    Tool.VfxManager.PlayBuffVFX(index, trajectory, 0f, BulletPlayer.RotationMode.Constant, scale);
                    break;
                case SkillVfxKind.RangeMagic:
                    Tool.VfxManager.PlayRangeMagicVFX(index, trajectory, life, scale);
                    break;
                case SkillVfxKind.MagicCircle:
                    Tool.VfxManager.PlayMagicCircleVFX(index, trajectory, life, scale);
                    break;
            }
        }

        // —— 以下为不依赖 SkillContext 布局的通用版本：轨迹 / 目标 id 由调用方自备 ——
        // 约定同上：index 在 Weapon 类下 = 全局武器 id，其余类 = 特效下标

        // 逐发弹道播放：vfx 与 trajectories 顺序一一配对取 Current；
        // Weapon 类下 vfx 元素 = 全局武器 id，其余类 = 特效下标
        public static void PlayAlong(SkillVfxKind kind, IEnumerator<int> vfx, IEnumerator<BulletTrajectory> trajectories)
        {
            if (kind == SkillVfxKind.None || vfx == null || trajectories == null) return;
            while (trajectories.MoveNext())
            {
                var trajectory = trajectories.Current;
                if (trajectory == null) continue;
                if (!vfx.MoveNext())
                    throw new InvalidOperationException($"vfx 枚举耗尽但仍有轨迹未播放，两者必须一一配对");
                PlayOne(kind, vfx.Current, trajectory);
            }
        }

        // 单目标跟随特效（护盾/Buff/光环等）
        public static void PlayFollow(SkillVfxKind kind, int index, ushort entityId, float lifeTime = 0f)
        {
            if (kind == SkillVfxKind.None) return;
            var trajectory = new FollowTrajectory(entityId);
            if (lifeTime > 0f) trajectory.Duration = lifeTime;
            PlayOne(kind, index, trajectory);
        }

        // 多目标跟随：目标 id 与上下文布局解耦，由调用方逐个喂入（如 context.ints 目标段的枚举器）
        public static void PlayFollowAll(SkillVfxKind kind, int index, IEnumerator<int> targetIds, float lifeTime = 0f)
        {
            if (kind == SkillVfxKind.None || targetIds == null) return;
            while (targetIds.MoveNext()) PlayFollow(kind, index, (ushort)targetIds.Current, lifeTime);
        }
    }
}