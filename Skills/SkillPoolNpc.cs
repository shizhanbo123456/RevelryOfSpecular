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

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            StrikeSphere(caster, HandPos(caster).position, MeleeRadius, BuildAttack(caster));
        }

        protected override bool NeedSendContext => false;
        public override void PlayVFX(SkillContext context) { }
    }

    public class SkillZombieClawL : SkillBase
    {
        public override int Id => (int)SkillType.SkillZombieClawL;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            StrikeSphere(caster, HandPos(caster, leftHand: true).position, MeleeRadius, BuildAttack(caster));
        }

        protected override bool NeedSendContext => false;
        public override void PlayVFX(SkillContext context) { }
    }

    public class SkillEliteZombieClawR : SkillBase
    {
        public override int Id => (int)SkillType.SkillEliteZombieClawR;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            StrikeSphere(caster, HandPos(caster).position, MeleeRadius, BuildAttack(caster));
        }

        protected override bool NeedSendContext => false;
        public override void PlayVFX(SkillContext context) { }
    }

    public class SkillEliteZombieClawL : SkillBase
    {
        public override int Id => (int)SkillType.SkillEliteZombieClawL;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            StrikeSphere(caster, HandPos(caster, leftHand: true).position, MeleeRadius, BuildAttack(caster));
        }

        protected override bool NeedSendContext => false;
        public override void PlayVFX(SkillContext context) { }
    }

    public class SkillEliteZombieScream : SkillBase
    {
        public override int Id => (int)SkillType.SkillEliteZombieScream;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            context.AddVectors(entity.transform.position);
            Utils.TargetSelect.SetEntitiesInCampToBuffer(entity.camp);
            for (int i = 0; i < Utils.TargetSelect.TargetBuffer.Count; i++)
                context.AddInts(Utils.TargetSelect.TargetBuffer[i].id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            Vector3 center = context.vectors[0];
            for (int i = 1; i < context.ints.Count; i++)
            {
                var ally = BattleManager.GetEntity((ushort)context.ints[i]);
                if (ally == null || (ally.transform.position - center).sqrMagnitude > TargetRadius * TargetRadius) continue;
                ApplyConfigBuff(caster, ally, 0);
            }
        }

        protected override bool NeedSendContext => false;
        public override void PlayVFX(SkillContext context) { } // buff 持续表现由客户端按 effect.csv 统一播
    }
    #endregion

    #region 水晶雕像 / 中立雕像
    // 孢子喷射：从体中心射出弹体特效
    public abstract class CrystalStatueSporeShot : SkillBase
    {
        protected const float BulletFlight = 0.6f; // 弹道时长；灵火喷吐按同一时刻在落点起爆
        protected abstract int BulletVfx { get; }

        // 落点：默认朝体中心、水平距离取射程；灵火喷吐改为目标脚部实际位置
        protected virtual Vector3 AimPoint(EntityData entity)
            => TargetSelect.AimPos(entity, TargetSector, TargetRadius, CastRange);

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            // 塔/树一律从体中心发射，不走武器槽顶部；包内只留施法者 id
            Vector3 origin = EntityAnchor.UpCenter.GetTransform(entity.ModelInfo, entity.anim).position;
            context.AddInts(entity.id);
            context.AddVectors(origin, AimPoint(entity));
            return context;
        }

        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            Vector3 origin = context.vectors[0]; // 塔/树是静止单位，直接用包里的施放点
            return TrialConstructor.Line(origin, context.vectors[1], BulletFlight);
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            if (caster == null || Tool.BattleManager == null) return;
            Tool.BattleManager.ShootBullet(caster, BuildAttack(caster), CreateTrajectory(context, 0), 0f,
                HitSpec(SkillVfxKind.Bullet, BulletVfx, VfxScale()));
        }

        public override void PlayVFX(SkillContext context)
            => VfxHelper.PlayOne(SkillVfxKind.Bullet, BulletVfx, CreateTrajectory(context, 0), VfxScale());
    }

    public class SkillCrystalStatueSporeA : CrystalStatueSporeShot
    {
        public override int Id => (int)SkillType.SkillCrystalStatueSporeA;
        protected override int BulletVfx => 25;
    }

    // 灵火喷吐：塔带灵火时的普攻，子弹打敌人脚部，到点追加一次原地灵火爆炸（与普攻同一特效）
    // 第 13 列 = 子弹倍率|灵火倍率，第 11 列 = 子弹尺寸|灵火尺寸
    public class SkillCrystalStatueSporeBlaze : CrystalStatueSporeShot
    {
        public override int Id => (int)SkillType.SkillCrystalStatueSporeBlaze;
        protected override int BulletVfx => 25;

        private const float BurstLife = 0.6f; // 爆炸命中球存活时长；资产填了存活时长则以资产为准

        // 打敌人脚部：不按射程归一化，否则子弹会飞过目标、爆炸点落不到脚下
        protected override Vector3 AimPoint(EntityData entity)
        {
            var target = ResolveTarget(entity);
            return target != null
                ? target.transform.position
                : entity.transform.position + entity.transform.forward * CastRange;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            if (caster == null || Tool.BattleManager == null) return;
            Tool.BattleManager.ShootBullet(caster, BuildAttack(caster), CreateTrajectory(context, 0), 0f,
                HitSpec(SkillVfxKind.Bullet, BulletVfx, VfxScale(0), ConfigDamageRate(0)));
            Burst(caster, context.vectors[1]);
        }

        public override void PlayVFX(SkillContext context)
        {
            VfxHelper.PlayOne(SkillVfxKind.Bullet, BulletVfx, CreateTrajectory(context, 0), VfxScale(0));
            Vector3 at = context.vectors[1];
            Timer.AddTimer(0, _ => VfxHelper.PlayOne(SkillVfxKind.Bullet, BulletVfx,
                TrialConstructor.Point(at, BurstLife), VfxScale(1)), BulletFlight, 1, false);
        }

        // 子弹到点的灵火爆炸：伤害取第 13 列第 2 项，两端同一时刻同一点位各自创建
        private void Burst(EntityData caster, Vector3 at)
        {
            Timer.AddTimer(0, _ =>
            {
                if (caster == null) return;
                Tool.BattleManager.ShootBullet(caster, BuildAttack(caster),
                    TrialConstructor.Point(at, BurstLife), BurstLife,
                    HitSpec(SkillVfxKind.Bullet, BulletVfx, VfxScale(1), ConfigDamageRate(1)));
            }, BulletFlight, 1, false);
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

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            // 塔/树一律从体中心发射，不走武器槽顶部；包内只留施法者 id
            Vector3 origin = EntityAnchor.UpCenter.GetTransform(entity.ModelInfo, entity.anim).position;
            context.AddInts(entity.id);
            context.AddVectors(origin, FormationPos(entity));
            return context;
        }

        // 阵法落点：目标所在 x/z，高度 = 地形表面 + 0.2；范围内没有敌人时退到自身面向的射程处
        private Vector3 FormationPos(EntityData entity)
        {
            var target = ResolveTarget(entity);
            Vector3 at = target != null
                ? target.transform.position
                : entity.transform.position + entity.transform.forward * CastRange;
            float groundY = at.y;
            var spawns = Tool.LandscapeSpawns; // 未注册时 Tool 已报错
            if (spawns != null) spawns.TrySampleTerrainHeight(at.x, at.z, out groundY);
            return new Vector3(at.x, groundY + FormationSurfaceOffset, at.z);
        }

        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            return TrialConstructor.Point(context.vectors[1], SporeLife); // 点轨迹 = 落点定住不动
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            if (caster == null || Tool.BattleManager == null) return;
            var attack = BuildAttack(caster,
                addEffect: ConfigHitBuff(caster, 0));
            Tool.BattleManager.ShootBullet(caster, attack, CreateTrajectory(context, 0), SporeLife,
                HitSpec(SkillVfxKind.RangeMagic, SporeVfx, VfxScale()));
        }

        public override void PlayVFX(SkillContext context)
        {
            VfxHelper.PlayOne(SkillVfxKind.RangeMagic, SporeVfx, CreateTrajectory(context, 0), VfxScale());
        }
    }
    #endregion
}
