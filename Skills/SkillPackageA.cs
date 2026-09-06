using Ros.Transport;
using UnityEngine;

namespace Ros.Skill
{
    /// <summary>
    /// 技能包 A：进攻方武器与角色技能（id 区间建议 0~999）。
    /// 技能写法见下方 SkillFanShot 示例（[E]）：新技能继承 SkillBase，在 RegisterAll 中注册即可。
    /// </summary>
    public static class SkillPackageA
    {
        public static class PackageManager
        {
            public static void RegisterAll()
            {
                // 示例技能（可删除后替换为正式技能）
                SkillManager.Register(new SkillFanShot());
                // TODO: 在此注册本包其余技能，例如 SkillManager.Register(new Skill1());
            }
        }
    }

    /// <summary>
    /// [E] 示例技能：扇形三连直射飞弹（展示轨迹上下文体系的标准写法）。
    ///
    /// 上下文约定（本技能自定义，无全局含义）：
    /// - ints[0] = 弹道数量 N；
    /// - vectors = 每发飞弹 2 个 Vector3（起点、终点），共 2N 个；第 i 发读 vectors[i*2] 与 vectors[i*2+1]。
    ///
    /// 流程：服务器 DoDamageActs 填装上下文 → 用构建函数重建轨迹发射子弹（逻辑判定）→
    /// BroadcastSkillCast 广播（技能 id + 上下文）→ 客户端 PlayVFX 用【同一个】构建函数重建轨迹播放特效。
    /// </summary>
    public class SkillFanShot : SkillBase
    {
        public override int Id => 0;
        public override bool Ranged => true;
        public override bool HasWeaponDisplay => true;
        public override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;

        // ---- 轨迹构建函数：每种轨迹一个，输入上下文，输出轨迹（服务器/客户端共用）----

        /// <summary>构建第 index 发飞弹的直线弹道（读取上下文中本发对应的下标段）。</summary>
        private BulletTrajectory BuildShotTrajectory(TrajectoryContext context, int index)
        {
            Vector3 start = context.vectors[index * 2];
            Vector3 end = context.vectors[index * 2 + 1];
            return BezierTrajectory.GetLine(start, end);
        }

        // ---- 服务器：填装上下文 → 构建轨迹 → 子弹逻辑 → 广播 ----

        public override void DoDamageActs(EntityData entity, Vector3 dest)
        {
            var context = new TrajectoryContext();

            // 计算三发扇形终点，填装轨迹参数（ints 与 vectors 的含义由本技能自行定义）
            Vector3 origin = entity.transform.position;
            Vector3[] dests = FanDests(origin, dest, 3, 10f);
            context.ints.Add(dests.Length);
            foreach (var d in dests)
            {
                context.AddVectors(origin, d);
            }

            // 服务器子弹逻辑（TODO：BulletContainer 完成后在此结算命中与伤害）
            for (int i = 0; i < dests.Length; i++)
            {
                BulletTrajectory trajectory = BuildShotTrajectory(context, i);
                Tool.BattleManager?.ShootBullet(entity, 20f, trajectory, 0.3f, 1.5f, null, null);
            }

            // 广播"使用技能"（技能 id + 上下文），客户端用同一构建函数重建轨迹播放特效
            BroadcastSkillCast(Id, context);
        }

        // ---- 客户端：用同一构建函数重建轨迹 → 播放特效 ----

        public override void PlayVFX(TrajectoryContext context)
        {
            int count = context.ints[0];
            for (int i = 0; i < count; i++)
            {
                BulletTrajectory trajectory = BuildShotTrajectory(context, i);

                // 特效物体取自 AssetsManager（客户端专属）；示例取 0 号子弹特效
                if (Tool.AssetsManager == null || Tool.AssetsManager.BulletVFX.Count == 0) continue;
                var vfx = Object.Instantiate(Tool.AssetsManager.BulletVFX[0]);
                vfx.name = $"SkillFanShot_{Id}_{i}";
                BulletPlayer.Create(vfx, trajectory, 1.5f, BulletPlayer.RotationMode.Tangent);
            }
        }
    }
}
