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
        public static EntityData GetNearestEnemy(EntityData entity, float radius = 10f, bool frontSector = true)
        {
            if (entity == null) return null;
            if (frontSector)
                return BattleManager.EntityContainer.GetNearestEnemyInFront(entity, radius, Config.default_skill_auto_target_sector_half_angle);
            return BattleManager.EntityContainer.GetNearestEnemy(entity, radius);
        }

        public static EntityData GetNearestAlly(EntityData entity, float radius = Config.default_skill_auto_target_radius)
        {
            if (entity == null) return null;
            return BattleManager.EntityContainer.GetNearestInCamp(
                entity.transform.position, radius, entity.camp, entity.id);
        }

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
            float sqr = radius * radius;
            foreach (var e in bucket)
            {
                if (e != null && e.Alive && (e.transform.position - pos).sqrMagnitude <= sqr) TargetBuffer.Add(e);
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
            float sqr = radius * radius;
            foreach (var e in BattleManager.EntityContainer.Entities)
            {
                if (e == null || !e.Alive || (hostile & e.camp) == 0) continue;
                if ((e.transform.position - center).sqrMagnitude <= sqr) TargetBuffer.Add(e);
            }
        }

        public static Vector3 SummonOffset(int index, float radius = 2f)
        {
            float angle = index * 72f * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }

        public static Vector3 AimPos(EntityData entity, float castRange,bool normalizeDistance = true, bool forceHorizontal = true)
        {
            // 索敌与终点距离统一用技能自身 CastRange（真实攻击距离），再不行才用视野
            float view = entity.floatingAttribute.viewDistance;
            float searchRadius = castRange > 0f ? castRange : view;
            var target = GetNearestEnemy(entity, searchRadius);
            if (target == null)
            {
                return entity.transform.position + entity.transform.forward * searchRadius;
            }
            else
            {
                if (normalizeDistance)
                {
                    var offset = target.transform.position - entity.transform.position;
                    if (forceHorizontal)
                        offset.y = 0;
                    Vector3 dir = offset.normalized;
                    return entity.transform.position + dir * searchRadius;
                }
                else
                {
                    var dest = target.transform.position;
                    if (forceHorizontal)
                        dest.y = entity.transform.position.y;
                    return dest;
                }
            }
        }
    }
}