using UnityEngine;

namespace Ros.Skill
{
    /// <summary>
    /// 技能基类（V0.8 技能统一模型）。
    /// 三要素：武器显示（可选）/ 释放动作（可配置）/ 释放效果（必须）。
    /// 服务器执行 DoDamageActs（伤害侧），客户端执行 PlayVFX（表现侧），两者必须一致。
    /// 【TODO】技能包具体实现待后续完善，本类仅提供框架与工具。
    /// </summary>
    public abstract class SkillBase
    {
        /// <summary>技能 id（全技能唯一，按包分配区间）。</summary>
        public abstract int Id { get; }

        /// <summary>CD（秒）。</summary>
        public virtual float CD => 5f;

        /// <summary>库存（-1 无限制）。</summary>
        public virtual int Store => -1;

        /// <summary>是否远程/施法类（决定右键是否可触发）。</summary>
        public virtual bool Ranged => true;

        /// <summary>是否有武器显示（悬浮武器）。</summary>
        public virtual bool HasWeaponDisplay => false;

        /// <summary>释放动作（0=None，无施法动作弹幕可配置为 None 由武器直接发射）。</summary>
        public virtual EntityAnim.AttackType CastAnim => 0;

        /// <summary>释放效果（必须）：服务器伤害侧。</summary>
        public abstract void DoDamageActs(EntityData entity, Vector3 dest);

        /// <summary>释放效果（必须）：客户端表现侧。</summary>
        public abstract void PlayVFX(Vector3 pos, Vector3 dest);

        #region 通用工具（服务端/客户端共用，保证伤害与特效一致）
        /// <summary>以 pos→dest 为基准方向生成扇形终点（水平展开 spreadDeg）。</summary>
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

        /// <summary>围绕 center 生成圆周终点（XZ 平面）。</summary>
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

        /// <summary>以 pos→dest 为基准方向水平旋转 angleDeg（右正左负）得到新目标点。</summary>
        protected static Vector3 RotateDest(Vector3 pos, Vector3 dest, float angleDeg)
        {
            Vector3 forward = dest - pos;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            forward.Normalize();
            Vector3 dir = Quaternion.Euler(0f, angleDeg, 0f) * forward;
            return pos + dir * Vector3.Distance(pos, dest);
        }

        /// <summary>默认目标点：面前 distance 米。</summary>
        protected static Vector3 DefaultDest(EntityData entity, float distance)
        {
            return entity.transform.position + entity.transform.forward * distance;
        }

        /// <summary>最近敌人位置（范围内）。</summary>
        protected static EntityData GetNearestEnemy(EntityData entity, float radius = 10f)
        {
            return BattleManager.EntityContainer.GetNearestEnemy(entity, radius);
        }

        /// <summary>服务器延时执行（快照参数，避免闭包引用变化）。</summary>
        protected static void DelayActs(float delay, (EntityData, Vector3, Vector3) param, System.Action<(EntityData, Vector3, Vector3)> action)
        {
            GenericTimer.AddTimer(param, delay, action);
        }

        /// <summary>客户端延时执行。</summary>
        protected static void DelayActs(float delay, (Vector3, Vector3) param, System.Action<(Vector3, Vector3)> action)
        {
            GenericTimer.AddTimer(param, delay, action);
        }
        #endregion
    }
}
