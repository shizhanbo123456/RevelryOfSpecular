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
    /// [E] 示例技能：扇形三连直射飞弹 + 一发天降轰炸（展示轨迹上下文体系的标准写法）。
    /// 占用 id 999（正式技能 id 段见策划案第二十一章，0~48 武器 / 50~73 防守方角色）。
    ///
    /// 上下文约定（本技能自定义，无全局含义）：
    /// - ints[0] = 直射弹道数量 N；ints[1] = 天降弹道升空高度；
    /// - vectors[0 .. 2N-1] = 直射弹道每发 2 个 Vector3（起点、终点），第 i 发读 vectors[i*2] 与 vectors[i*2+1]；
    /// - vectors[2N] = 施放者位置，vectors[2N+1] = 天降目标点。
    ///
    /// 流程：服务器 DoDamageActs 填装上下文 → 用构建函数重建轨迹发射子弹（逻辑判定）→
    /// BroadcastSkillCast 广播（技能 id + 上下文）→ 客户端 PlayVFX 用【同一个】构建函数重建轨迹播放特效。
    /// </summary>
    public class SkillFanShot : SkillBase
    {
        public override int Id => 999;
        public override bool Ranged => true;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 0); // 示例技能：一把枪械（正式技能按需声明）
        public override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;

        private const int ShotCount = 3;
        private const float ShotLifeTime = 1.5f;
        private const float SkyFallLifeTime = 2.5f;

        // ---- 轨迹构建函数：每种轨迹一个，输入上下文，输出轨迹（服务器/客户端共用）----

        /// <summary>构建第 index 发直射飞弹的直线弹道（读取上下文中本发对应的下标段）。</summary>
        private BulletTrajectory BuildShotTrajectory(TrajectoryContext context, int index)
        {
            Vector3 start = context.vectors[index * 2];
            Vector3 end = context.vectors[index * 2 + 1];
            return new LineTrajectory(start, end);
        }

        /// <summary>构建天降轰炸弹道（升空超出视野 → 目标位置上空 → 天降命中）。</summary>
        private BulletTrajectory BuildSkyFallTrajectory(TrajectoryContext context)
        {
            int shotCount = context.ints[0];
            Vector3 origin = context.vectors[shotCount * 2];
            Vector3 target = context.vectors[shotCount * 2 + 1];
            return new SkyFallTrajectory(origin, target, context.ints[1]);
        }

        // ---- 服务器：填装上下文 → 构建轨迹 → 子弹逻辑 → 广播 ----

        public override void DoDamageActs(EntityData entity, Vector3 dest)
        {
            var context = new TrajectoryContext();

            // 计算三发扇形终点，填装直射弹道参数（ints 与 vectors 的含义由本技能自行定义）
            Vector3 origin = entity.transform.position;
            Vector3[] dests = FanDests(origin, dest, ShotCount, 10f);
            context.ints.Add(dests.Length);      // ints[0] = 直射弹道数量
            context.ints.Add(30);                // ints[1] = 天降升空高度（超出视野）
            foreach (var d in dests)
            {
                context.AddVectors(origin, d);
            }
            context.AddVectors(origin, dest);    // 天降：施放者位置 + 目标点

            // 服务器子弹逻辑：直射三连共用一份攻击数据（不破霸体）；武器经验随攻击数据加伤（策划案 14 章）
            int weaponExp = entity.skillController?.GetWeaponExp(Id) ?? 0;
            AttackData shotAttack = AttackData.Create(entity, rate: 20f, radius: 0.3f, breakEndure: false,
                weaponExp: weaponExp);
            for (int i = 0; i < dests.Length; i++)
            {
                BulletTrajectory trajectory = BuildShotTrajectory(context, i);
                Tool.BattleManager?.ShootBullet(entity, shotAttack, trajectory, ShotLifeTime);
            }

            // 天降轰炸（破霸体：命中可打破霸体等级 1 的目标，见策划案 12.1）
            AttackData fallAttack = AttackData.Create(entity, rate: 40f, radius: 0.8f, breakEndure: true,
                weaponExp: weaponExp);
            BulletTrajectory skyFall = BuildSkyFallTrajectory(context);
            Tool.BattleManager?.ShootBullet(entity, fallAttack, skyFall, SkyFallLifeTime);

            // 广播"使用技能"（技能 id + 上下文），客户端用同一构建函数重建轨迹播放特效
            BroadcastSkillCast(Id, context);
        }

        // ---- 客户端：用同一构建函数重建轨迹 → 经 VfxManager 统一播放特效 ----

        /// <summary>本技能使用的子弹特效下标（AssetsManager.BulletVFX 列表，见特效清单）。</summary>
        private const int ShotVfxIndex = 0;

        public override void PlayVFX(TrajectoryContext context)
        {
            if (Tool.VfxManager == null) return;

            // 直射飞弹
            int shotCount = context.ints[0];
            for (int i = 0; i < shotCount; i++)
            {
                BulletTrajectory trajectory = BuildShotTrajectory(context, i);
                Tool.VfxManager.PlayBulletVFX(ShotVfxIndex, trajectory, ShotLifeTime);
            }

            // 天降轰炸
            BulletTrajectory skyFall = BuildSkyFallTrajectory(context);
            Tool.VfxManager.PlayBulletVFX(ShotVfxIndex, skyFall, SkyFallLifeTime);
        }
    }
}
