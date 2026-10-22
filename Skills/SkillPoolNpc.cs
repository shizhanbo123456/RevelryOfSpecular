using Ros.Skill.Utils;
using Ros.Transport;
using UnityEngine;

namespace Ros.Skill
{
    // 非玩家单位：普通僵尸爪击×2 / 精英僵尸爪击×2+嘶吼 / 水晶雕像孢子喷射×3 / 中立雕像孢子喷发
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
            SkillManager.Register(new SkillCrystalStatueSporeB());
            SkillManager.Register(new SkillCrystalStatueSporeC());
            SkillManager.Register(new SkillNeutralStatueSpore());
        }
    }

    #region 僵尸
    public class SkillZombieClawR : SkillBase
    {
        public override int Id => (int)SkillType.SkillZombieClawR;
        public override float CastRange => Config.zombie_attack_range;
        public override float CD => 1.5f;
        public override int Store => -1;
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Zombie_Hand_Attack_R;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            StrikeSphere(caster, HandPos(caster).position, Config.melee_hit_radius, BuildAttack(caster, 1f, Config.melee_hit_radius));
        }

        public override void PlayVFX(SkillContext context) { }
    }

    public class SkillZombieClawL : SkillBase
    {
        public override int Id => (int)SkillType.SkillZombieClawL;
        public override float CastRange => Config.zombie_attack_range;
        public override float CD => 1.5f;
        public override int Store => -1;
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Zombie_Hand_Attack_L;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            StrikeSphere(caster, HandPos(caster, leftHand: true).position, Config.melee_hit_radius, BuildAttack(caster, 1f, Config.melee_hit_radius));
        }

        public override void PlayVFX(SkillContext context) { }
    }

    public class SkillEliteZombieClawR : SkillBase
    {
        public override int Id => (int)SkillType.SkillEliteZombieClawR;
        public override float CastRange => Config.zombie_attack_range;
        public override float CD => 1.5f;
        public override int Store => -1;
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Zombie_Hand_Attack_R;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            StrikeSphere(caster, HandPos(caster).position, Config.melee_hit_radius, BuildAttack(caster, 1.1f, Config.melee_hit_radius));
        }

        public override void PlayVFX(SkillContext context) { }
    }

    public class SkillEliteZombieClawL : SkillBase
    {
        public override int Id => (int)SkillType.SkillEliteZombieClawL;
        public override float CastRange => Config.zombie_attack_range;
        public override float CD => 1.5f;
        public override int Store => -1;
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Zombie_Hand_Attack_L;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            StrikeSphere(caster, HandPos(caster, leftHand: true).position, Config.melee_hit_radius, BuildAttack(caster, 1.1f, Config.melee_hit_radius));
        }

        public override void PlayVFX(SkillContext context) { }
    }

    public class SkillEliteZombieScream : SkillBase
    {
        public override int Id => (int)SkillType.SkillEliteZombieScream;
        public override float CD => 8f;
        public override int Store => -1;
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Zombie_Scream;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            ApplyConfigBuff(caster, caster, 0);
        }

        public override void PlayVFX(SkillContext context) { } // buff 持续表现由客户端按 Config.buff_vfx 统一播
    }
    #endregion

    #region 水晶雕像 / 中立雕像
    // 三发孢子喷射共用实现，只有弹体特效不同
    public abstract class CrystalStatueSporeShot : SkillBase
    {
        public override float CastRange => Config.crystal_statue_attack_range;
        public override float CD => 2f;
        public override int Store => -1;
        protected override bool HasCastAnim => false; // 塔无攻击动画：按下即施放
        protected abstract int BulletVfx { get; }

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            // 塔/树一律从体中心发射，不走武器槽顶部；包内只留施法者 id
            Vector3 origin = EntityAnchor.UpCenter.GetTransform(entity.ModelInfo, entity.anim, entity.springWeapon).position;
            context.AddInts(entity.id);
            context.AddVectors(origin, TargetSelect.AimPos(entity, CastRange));
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
            Tool.BattleManager.ShootBullet(caster, BuildAttack(caster, 1f, 0.5f, useMagic: true), CreateTrajectory(context, 0));
        }

        public override void PlayVFX(SkillContext context)
            => VfxHelper.PlayOne(SkillVfxKind.Bullet, BulletVfx, CreateTrajectory(context, 0));
    }

    public class SkillCrystalStatueSporeA : CrystalStatueSporeShot
    {
        public override int Id => (int)SkillType.SkillCrystalStatueSporeA;
        protected override int BulletVfx => 25;
    }

    public class SkillCrystalStatueSporeB : CrystalStatueSporeShot
    {
        public override int Id => (int)SkillType.SkillCrystalStatueSporeB;
        protected override int BulletVfx => 24;
    }

    public class SkillCrystalStatueSporeC : CrystalStatueSporeShot
    {
        public override int Id => (int)SkillType.SkillCrystalStatueSporeC;
        protected override int BulletVfx => 26;
    }

    public class SkillNeutralStatueSpore : SkillBase
    {
        public override int Id => (int)SkillType.SkillNeutralStatueSpore;
        public override float CastRange => Config.neutral_statue_attack_range;
        public override float CD => 2.5f;
        public override int Store => -1;
        protected override bool HasCastAnim => false;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            // 塔/树一律从体中心发射，不走武器槽顶部；包内只留施法者 id
            Vector3 origin = EntityAnchor.UpCenter.GetTransform(entity.ModelInfo, entity.anim, entity.springWeapon).position;
            context.AddInts(entity.id);
            context.AddVectors(origin, TargetSelect.AimPos(entity, CastRange));
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
            var attack = BuildAttack(caster, 1f, 0.5f, useMagic: true,
                addEffect: ConfigHitBuff(caster, 0));
            Tool.BattleManager.ShootBullet(caster, attack, CreateTrajectory(context, 0));
        }

        public override void PlayVFX(SkillContext context)
        {
            VfxHelper.PlayOne(SkillVfxKind.RangeMagic, 9, CreateTrajectory(context, 0));
        }
    }
    #endregion
}
