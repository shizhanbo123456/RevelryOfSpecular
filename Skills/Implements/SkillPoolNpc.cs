using Ros.Skill.Utils;
using Ros.Transport;
using UnityEngine;

namespace Ros.Skill
{
    // 非玩家单位：普通僵尸爪击×2 / 精英僵尸爪击×2+嘶吼 / 防御塔孢子喷射×2 / 中立雕像孢子喷发
    public static class SkillPoolNpc
    {
        public static void RegisterAll()
        {
            SkillManager.Register(new SkillZombieClawR());
            SkillManager.Register(new SkillZombieClawL());
            SkillManager.Register(new SkillEliteZombieClawR());
            SkillManager.Register(new SkillEliteZombieClawL());
            SkillManager.Register(new SkillEliteZombieScream());
            SkillManager.Register(new SkillCrystalStatueSporeA());
            SkillManager.Register(new SkillCrystalStatueSporeBlaze());
            SkillManager.Register(new SkillNeutralStatueSpore());
        }
    }

    #region 僵尸
    public class SkillZombieClawR : SkillBase
    {
        public override int Id => (int)SkillType.SkillZombieClawR;

        protected override void OnCast(EntityData caster)
            => StrikeSphere(caster, HandPos(caster).position, MeleeRadius, BuildAttack(caster));
    }

    public class SkillZombieClawL : SkillBase
    {
        public override int Id => (int)SkillType.SkillZombieClawL;

        protected override void OnCast(EntityData caster)
            => StrikeSphere(caster, HandPos(caster, leftHand: true).position, MeleeRadius, BuildAttack(caster));
    }

    public class SkillEliteZombieClawR : SkillBase
    {
        public override int Id => (int)SkillType.SkillEliteZombieClawR;

        protected override void OnCast(EntityData caster)
            => StrikeSphere(caster, HandPos(caster).position, MeleeRadius, BuildAttack(caster));
    }

    public class SkillEliteZombieClawL : SkillBase
    {
        public override int Id => (int)SkillType.SkillEliteZombieClawL;

        protected override void OnCast(EntityData caster)
            => StrikeSphere(caster, HandPos(caster, leftHand: true).position, MeleeRadius, BuildAttack(caster));
    }

    // 血嚎：自身与同阵营半径内的单位挂增益，半径读第 16 列
    public class SkillEliteZombieScream : SkillBase
    {
        public override int Id => (int)SkillType.SkillEliteZombieScream;

        protected override void OnCast(EntityData caster)
        {
            TargetSelect.SetEntitiesInCampToBuffer(caster.camp);
            Vector3 center = caster.transform.position;
            float sqr = TargetRadius * TargetRadius;
            for (int i = 0; i < TargetSelect.TargetBuffer.Count; i++)
            {
                EntityData ally = TargetSelect.TargetBuffer[i];
                if (ally == null || (ally.transform.position - center).sqrMagnitude > sqr) continue;
                ApplyConfigBuff(caster, ally, 0);
            }
        }
    }
    #endregion

    #region 水晶雕像 / 中立雕像
    // 孢子喷射：塔/树一律从体中心射出，落点朝范围内最近敌人、不用射程归一化
    public class SkillCrystalStatueSporeA : SkillBase
    {
        public override int Id => (int)SkillType.SkillCrystalStatueSporeA;

        private const int BulletVfx = 25;
        private const float BulletFlight = 0.6f;

        protected override void OnCast(EntityData caster)
        {
            Vector3 origin = EntityAnchor.UpCenter.GetTransform(caster.ModelInfo, caster.anim).position;
            Vector3 aim = TargetSelect.AimPos(caster, TargetSector, TargetRadius, CastRange);
            Tool.BattleManager.ShootBullet(caster, BuildAttack(caster),
                new SkillTrajectoryLine(origin, aim, BulletFlight), 0f,
                HitSpec(SkillVfxKind.Bullet, BulletVfx, VfxScale()));
            SendWave(caster, 0, context => { context.WriteVector3(origin); context.WriteVector3(aim); });
        }

        public override void PlayWave(SCSkillDisplayContext context)
        {
            Vector3 origin = context.ReadVector3();
            Vector3 aim = context.ReadVector3();
            VfxHelper.PlayOne(SkillVfxKind.Bullet, BulletVfx, new SkillTrajectoryLine(origin, aim, BulletFlight), VfxScale());
        }
    }

    // 灵火喷吐：塔带灵火时的普攻，子弹打敌人脚部，到点追加一次原地灵火爆炸（与 132 崩拳同一个 RM[6]）
    // 第 13 列 = 子弹倍率|灵火倍率，第 11 列 = 子弹尺寸|灵火尺寸
    public class SkillCrystalStatueSporeBlaze : SkillBase
    {
        public override int Id => (int)SkillType.SkillCrystalStatueSporeBlaze;

        private const int BulletVfx = 25;
        private const int BurstVfx = 6;
        private const float BulletFlight = 0.6f;
        private const float BurstLife = 0.6f;

        protected override void OnCast(EntityData caster)
        {
            Vector3 origin = EntityAnchor.UpCenter.GetTransform(caster.ModelInfo, caster.anim).position;
            // 打敌人脚部：不按射程归一化，否则子弹会飞过目标、爆炸点落不到脚下
            EntityData target = ResolveTarget(caster);
            Vector3 aim = target != null
                ? target.transform.position
                : caster.transform.position + caster.transform.forward * CastRange;
            Tool.BattleManager.ShootBullet(caster, BuildAttack(caster),
                new SkillTrajectoryLine(origin, aim, BulletFlight), 0f,
                HitSpec(SkillVfxKind.Bullet, BulletVfx, VfxScale(0), ConfigDamageRate(0)));
            Burst(caster, aim);
            SendWave(caster, 0, context => { context.WriteVector3(origin); context.WriteVector3(aim); });
        }

        // 子弹到点的灵火爆炸：伤害取第 13 列第 2 项，两端同一时刻同一点位各自创建
        private void Burst(EntityData caster, Vector3 at)
        {
            Timer.AddTimer(0, _ =>
            {
                if (caster == null) return;
                Tool.BattleManager.ShootBullet(caster, BuildAttack(caster),
                    new SkillTrajectoryPoint(at, BurstLife), BurstLife,
                    HitSpec(SkillVfxKind.RangeMagic, BurstVfx, VfxScale(1), ConfigDamageRate(1)));
            }, BulletFlight, 1, false);
        }

        public override void PlayWave(SCSkillDisplayContext context)
        {
            Vector3 origin = context.ReadVector3();
            Vector3 at = context.ReadVector3();
            VfxHelper.PlayOne(SkillVfxKind.Bullet, BulletVfx, new SkillTrajectoryLine(origin, at, BulletFlight), VfxScale(0));
            Timer.AddTimer(0, _ => VfxHelper.PlayAt(SkillVfxKind.RangeMagic, BurstVfx, at, BurstLife, VfxScale(1)),
                BulletFlight, 1, false);
        }
    }

    // 疫孢吐息：在目标位置落一座疫孢阵法（定点，不飞行），命中挂 Poison
    public class SkillNeutralStatueSpore : SkillBase
    {
        public override int Id => (int)SkillType.SkillNeutralStatueSpore;

        private const int SporeVfx = 9;
        private const float FormationSurfaceOffset = 0.2f;  // 阵法贴地：地形表面 + 0.2
        private const float SporeFallbackLife = 0.6f;       // 资产填 -1（自身不主动清除）时的兜底时长

        // 阵法存活时长优先取资产 BulletVfxInfo.lifeTime
        private static float SporeLife
        {
            get
            {
                float life = VfxHitSource.GetLifeTime(SkillVfxKind.RangeMagic, SporeVfx);
                return life > 0f ? life : SporeFallbackLife;
            }
        }

        protected override void OnCast(EntityData caster)
        {
            Vector3 at = FormationPos(caster);
            Tool.BattleManager.ShootBullet(caster, BuildAttack(caster, addEffect: ConfigHitBuff(caster, 0)),
                new SkillTrajectoryPoint(at, SporeLife), SporeLife,
                HitSpec(SkillVfxKind.RangeMagic, SporeVfx, VfxScale()));
            SendWave(caster, 0, context => context.WriteVector3(at));
        }

        // 阵法落点：目标所在 x/z，高度 = 地形表面 + 0.2；范围内没有敌人时退到自身面向的射程处
        private Vector3 FormationPos(EntityData entity)
        {
            EntityData target = ResolveTarget(entity);
            Vector3 at = target != null
                ? target.transform.position
                : entity.transform.position + entity.transform.forward * CastRange;
            float groundY = at.y;
            var spawns = Tool.LandscapeSpawns; // 未注册时 Tool 已报错
            if (spawns != null) spawns.TrySampleTerrainHeight(at.x, at.z, out groundY);
            return new Vector3(at.x, groundY + FormationSurfaceOffset, at.z);
        }

        public override void PlayWave(SCSkillDisplayContext context)
        {
            Vector3 at = context.ReadVector3();
            VfxHelper.PlayOne(SkillVfxKind.RangeMagic, SporeVfx, new SkillTrajectoryPoint(at, SporeLife), VfxScale());
        }
    }
    #endregion
}
