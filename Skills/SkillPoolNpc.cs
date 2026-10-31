using Ros.Skill.Utils;
using Ros.Transport;
using UnityEngine;

namespace Ros.Skill
{
    // 非玩家单位：普通僵尸爪击×2 / 精英僵尸爪击×2+嘶吼 / 防御塔孢子喷射×1 / 中立雕像孢子喷发
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
            SkillManager.Register(new SkillNeutralStatueSpore());
        }
    }

    #region 僵尸
    public class SkillZombieClawR : SkillBase
    {
        public override int Id => (int)SkillType.SkillZombieClawR;
        public override float CastRange => Config.zombie_attack_range;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            StrikeSphere(caster, HandPos(caster).position, MeleeRadius, BuildAttack(caster, 1f));
        }

        public override void PlayVFX(SkillContext context) { }
    }

    public class SkillZombieClawL : SkillBase
    {
        public override int Id => (int)SkillType.SkillZombieClawL;
        public override float CastRange => Config.zombie_attack_range;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            StrikeSphere(caster, HandPos(caster, leftHand: true).position, MeleeRadius, BuildAttack(caster, 1f));
        }

        public override void PlayVFX(SkillContext context) { }
    }

    public class SkillEliteZombieClawR : SkillBase
    {
        public override int Id => (int)SkillType.SkillEliteZombieClawR;
        public override float CastRange => Config.zombie_attack_range;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            StrikeSphere(caster, HandPos(caster).position, MeleeRadius, BuildAttack(caster, 1.1f));
        }

        public override void PlayVFX(SkillContext context) { }
    }

    public class SkillEliteZombieClawL : SkillBase
    {
        public override int Id => (int)SkillType.SkillEliteZombieClawL;
        public override float CastRange => Config.zombie_attack_range;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            StrikeSphere(caster, HandPos(caster, leftHand: true).position, MeleeRadius, BuildAttack(caster, 1.1f));
        }

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

        public override void PlayVFX(SkillContext context) { } // buff 持续表现由客户端按 effect.csv 统一播
    }
    #endregion

    #region 水晶雕像 / 中立雕像
    // 孢子喷射：从体中心射出弹体特效
    public abstract class CrystalStatueSporeShot : SkillBase
    {
        public override float CastRange => Config.crystal_statue_attack_range;
        protected abstract int BulletVfx { get; }

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            // 塔/树一律从体中心发射，不走武器槽顶部；包内只留施法者 id
            Vector3 origin = EntityAnchor.UpCenter.GetTransform(entity.ModelInfo, entity.anim).position;
            context.AddInts(entity.id);
            context.AddVectors(origin, TargetSelect.AimPos(entity, TargetSector, TargetRadius, CastRange));
            return context;
        }

        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            Vector3 origin = context.vectors[0]; // 塔/树是静止单位，直接用包里的施放点
            return TrialConstructor.Line(origin, context.vectors[1], 0.6f);
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            if (caster == null || Tool.BattleManager == null) return;
            Tool.BattleManager.ShootBullet(caster, BuildAttack(caster, 1f), CreateTrajectory(context, 0), 0f,
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


    public class SkillNeutralStatueSpore : SkillBase
    {
        public override int Id => (int)SkillType.SkillNeutralStatueSpore;
        public override float CastRange => Config.neutral_statue_attack_range;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            // 塔/树一律从体中心发射，不走武器槽顶部；包内只留施法者 id
            Vector3 origin = EntityAnchor.UpCenter.GetTransform(entity.ModelInfo, entity.anim).position;
            context.AddInts(entity.id);
            context.AddVectors(origin, TargetSelect.AimPos(entity, TargetSector, TargetRadius, CastRange));
            return context;
        }

        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            Vector3 origin = context.vectors[0]; // 塔/树是静止单位，直接用包里的施放点
            return TrialConstructor.Line(origin, context.vectors[1], 0.6f);
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            if (caster == null || Tool.BattleManager == null) return;
            var attack = BuildAttack(caster, 1f,
                addEffect: ConfigHitBuff(caster, 0));
            Tool.BattleManager.ShootBullet(caster, attack, CreateTrajectory(context, 0), 0f,
                HitSpec(SkillVfxKind.RangeMagic, 9, VfxScale()));
        }

        public override void PlayVFX(SkillContext context)
        {
            VfxHelper.PlayOne(SkillVfxKind.RangeMagic, 9, CreateTrajectory(context, 0), VfxScale());
        }
    }
    #endregion
}
