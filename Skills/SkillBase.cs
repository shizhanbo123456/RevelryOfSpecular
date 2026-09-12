using Ros.Transport;
using UnityEngine;

namespace Ros.Skill
{
    /// <summary>
    /// 技能基类（技能统一模型）。
    /// 三要素：武器显示（可选）/ 释放动作（可配置）/ 释放效果（必须）。
    ///
    /// 【轨迹上下文体系】
    /// 1. 服务器执行 DoDamageActs：计算并填装 TrajectoryContext（ints/vectors 完全无固定含义，
    ///    由技能任意填充；每个上下文只服务一次释放）→ 调用自己的轨迹构建函数得到 BulletTrajectory
    ///    → 用于服务器子弹逻辑（BattleManager.ShootBullet）→ 调用 BroadcastSkillCast 把
    ///    (技能 id, 上下文) 通过"使用技能"RPC 广播给客户端。
    /// 2. 客户端收到 (技能 id, 上下文) 后经 SkillManager.PlayVFX 调用本技能 PlayVFX(context)：
    ///    用【同一个】轨迹构建函数重建轨迹 → 调用 VfxManager.PlayBulletVFX(特效下标, 轨迹, 时长)
    ///    统一播放特效（技能内不直接 Instantiate / 引用 AssetsManager）。
    ///    服务器只做攻击判定（ShootBullet），特效表现完全在客户端由 VfxManager 管理。
    /// 3. 约定：技能中要为该技能涉及的每一种轨迹写一个构建函数——传入 TrajectoryContext，
    ///    传出 BulletTrajectory；函数内读取上下文中自己约定的下标段（多种轨迹各读各的，互不重叠）。
    /// 4. 若技能需要向客户端传递额外信息（目标点/施放者 id 等），一律放入 TrajectoryContext 传递。
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

        /// <summary>
        /// 技能对应的武器（悬浮武器模型）：类别 + 该类别列表中的下标。
        /// <see cref="WeaponRef.IsValid"/> == 是否有武器显示；防守方主动技能/大招与被动均为 <see cref="WeaponRef.None"/>。
        /// </summary>
        public virtual WeaponRef Weapon => WeaponRef.None;

        /// <summary>释放动作（0=None，无施法动作弹幕可配置为 None 由武器直接发射）。</summary>
        public virtual EntityAnim.AttackType CastAnim => 0;

        /// <summary>
        /// 释放效果（必须）：服务器伤害侧。
        /// 实现内容：填装 TrajectoryContext → 构建轨迹 → ShootBullet 逻辑判定 → BroadcastSkillCast 广播。
        /// dest 为玩家瞄准点（不需要目标位置的技能可忽略，客户端需要的参数请放入上下文）。
        /// </summary>
        public abstract void DoDamageActs(EntityData entity, Vector3 dest);

        /// <summary>
        /// 释放效果（必须）：客户端表现侧。
        /// 用与服务器相同的轨迹构建函数从上下文重建轨迹，经 VfxManager 接口播放特效。
        /// </summary>
        public abstract void PlayVFX(TrajectoryContext context);

        /// <summary>
        /// 服务器：广播"使用技能"RPC（技能 id + 轨迹上下文），客户端据此重建轨迹播放表现。
        /// </summary>
        protected static void BroadcastSkillCast(int skillId, TrajectoryContext context)
        {
            Tool.NetworkManager?.SendSkillCast(skillId, context);
        }

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
