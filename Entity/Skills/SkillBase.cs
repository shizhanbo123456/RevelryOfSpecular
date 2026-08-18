using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ros.Skill
{
    public abstract class SkillBase
    {
        public abstract int Id { get; }
        public virtual float CD { get; } = 10;
        public virtual int Store { get; } = 1;

        public abstract (Vector3, Vector3) DoDamageActs(EntityData entity, Vector3 dest);
        public abstract void PlayVFX(Vector3 pos,Vector3 dest);


        /// <summary>服务端延时：延迟后携带 (EntityData,Vector3,Vector3) 参数快照执行回调（经 Tool 单例交给 SkillDelayActionManager）。</summary>
        protected static void DelayActs(float delay,(EntityData,Vector3,Vector3)@param,Action<(EntityData, Vector3, Vector3)> action)
        {
            Tool.SkillDelayActionManager.DelayActs(delay, param, action);
        }
        /// <summary>客户端表现侧延时（PlayVFX 无 EntityData 引用时用此重载）。</summary>
        protected static void DelayActs(float delay, (Vector3,Vector3) param, Action<(Vector3,Vector3)> action)
        {
            Tool.SkillDelayActionManager.DelayActs(delay, param, action);
        }

        /// <summary>伪随机固定序列（不用 Random，多次使用结果一致）。</summary>
        private static readonly float[] s_fakeRand = { 0.53f, 0.11f, 0.87f, 0.32f, 0.69f, 0.24f, 0.94f, 0.47f, 0.08f, 0.72f, 0.35f, 0.81f, 0.16f, 0.58f, 0.91f, 0.27f, 0.63f, 0.04f, 0.76f, 0.42f };
        protected static float FR(int i) => s_fakeRand[i % s_fakeRand.Length];

        protected static Transform FigureUtility=new GameObject().transform;
        private static HashSet<int> buffer = new();
        protected static Vector3 DefaultDest(EntityData entity, float distance)
        {
            return entity.transform.position + entity.transform.forward * distance;
        }

        /// <summary>
        /// 生成围绕 center 的圆周终点（XZ 平面）。服务端 DoDamageActs 与客户端 PlayVFX 共用，保证特效与伤害一致。
        /// </summary>
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

        /// <summary>
        /// 以 pos→dest 为基准方向生成扇形终点（水平展开 spreadDeg），
        /// 与 pos/dest 严格一致，服务端与客户端共用。
        /// </summary>
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

        /// <summary>发射一颗 C 包子弹（伤害侧，服务端）。</summary>
        protected static void ShootC(int cId, EntityData entity, Vector3 dest)
        {
            SkillManager.DoDamageActs(cId, entity, dest);
        }

        /// <summary>播放一颗 C 包子弹特效（表现侧，客户端）。</summary>
        protected static void PlayC(int cId, Vector3 pos, Vector3 dest)
        {
            SkillManager.PlayVFX(cId, pos, dest);
        }

        /// <summary>一次发射多颗 C 包子弹（伤害侧）。</summary>
        protected static void ShootC(int cId, EntityData entity, params Vector3[] dests)
        {
            foreach (var d in dests) SkillManager.DoDamageActs(cId, entity, d);
        }

        /// <summary>一次播放多颗 C 包子弹特效（表现侧，与 ShootC 成对）。</summary>
        protected static void PlayC(int cId, Vector3 pos, params Vector3[] dests)
        {
            foreach (var d in dests) SkillManager.PlayVFX(cId, pos, d);
        }

        /// <summary>从自定义 source 发射一颗 C 包子弹（伤害侧）。</summary>
        protected static void ShootCFrom(int cId, EntityData entity, Vector3 source, Vector3 dest)
        {
            PackageC.PackageManager.TryShootFrom(cId, entity, source, dest);
        }

        /// <summary>从自定义 source 播放一颗 C 包子弹特效（表现侧）。</summary>
        protected static void PlayCFrom(int cId, Vector3 source, Vector3 dest)
        {
            PackageC.PackageManager.TryPlayFrom(cId, source, dest);
        }

        /// <summary>从同一 source 发射多颗 C 包子弹（伤害侧）。</summary>
        protected static void ShootCFrom(int cId, EntityData entity, Vector3 source, params Vector3[] dests)
        {
            foreach (var d in dests) PackageC.PackageManager.TryShootFrom(cId, entity, source, d);
        }

        /// <summary>从同一 source 播放多颗 C 包子弹特效（表现侧）。</summary>
        protected static void PlayCFrom(int cId, Vector3 source, params Vector3[] dests)
        {
            foreach (var d in dests) PackageC.PackageManager.TryPlayFrom(cId, source, d);
        }

        /// <summary>从自定义 source 延时发射一颗 C 包子弹（伤害侧）。</summary>
        protected static void DelayShootC(int cId, float delay, EntityData entity, Vector3 source, Vector3 dest)
        {
            DelayActs(delay, (entity, source, dest), p =>
                PackageC.PackageManager.TryShootFrom(cId, p.Item1, p.Item2, p.Item3));
        }

        /// <summary>从自定义 source 延时播放一颗 C 包子弹特效（表现侧）。</summary>
        protected static void DelayPlayC(int cId, float delay, Vector3 source, Vector3 dest)
        {
            DelayActs(delay, (source, dest), p =>
                PackageC.PackageManager.TryPlayFrom(cId, p.Item1, p.Item2));
        }

        /// <summary>
        /// 以 pos→dest 为基准方向，水平旋转 angleDeg（右正左负）得到新目标点（保持距离）。
        /// 服务端/客户端共用，保证特效与伤害一致。
        /// </summary>
        protected static Vector3 RotateDest(Vector3 pos, Vector3 dest, float angleDeg)
        {
            Vector3 forward = dest - pos;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            forward.Normalize();
            Vector3 dir = Quaternion.Euler(0f, angleDeg, 0f) * forward;
            return pos + dir * Vector3.Distance(pos, dest);
        }

        /// <summary>
        /// 围绕 center 生成 count 个带伪随机半径扰动的落点（模拟"随机位置"），
        /// 仅依赖 center 与固定伪随机序列，服务端/客户端一致。
        /// </summary>
        protected static Vector3[] JitterCircleDests(Vector3 center, float radius, float jitter, int count, int randOffset = 0)
        {
            var dests = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                float angle = (FR(i + randOffset) * 360f) * Mathf.Deg2Rad;
                float r = radius + (FR(i + randOffset + 7) * 2f - 1f) * jitter;
                dests[i] = center + new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
            }
            return dests;
        }

        protected static EntityData GetNearestEnemy(EntityData entity,float radius=10f,bool inFrontOnly=false)
        {
            BattleManager.EntityContainer.Entities.GetIdsInRange(entity.transform.position, radius, buffer);
            EntityData target=null;
            float sqrd=float.MaxValue;
            foreach(var entityId in buffer)
            {
                var enemy = BattleManager.EntityContainer.Entities[entityId];
                if (entity.id == enemy.id) continue;
                float distance=Vector3.SqrMagnitude(enemy.transform.position-entity.transform.position);
                if (distance < sqrd)
                {
                    sqrd = distance;
                    target = enemy;
                }
            }
            return target;
        }
    }
}
