using System;
using Ros.Transport;
using UnityEngine;
using Ros.Skill.Utils;

namespace Ros.Skill
{
    public static class SkillPoolCrystal
    {
        public static void RegisterAll()
        {
            // 近战刀 0~10
            SkillManager.Register(new SkillGaleSlash());
            SkillManager.Register(new SkillFlyingKnife());
            SkillManager.Register(new SkillCrossSlash());
            SkillManager.Register(new SkillChargeSlash());
            SkillManager.Register(new SkillWhirlSlash());
            SkillManager.Register(new SkillBreakEndureSlash());
            SkillManager.Register(new SkillFanSwordQi());
            SkillManager.Register(new SkillCaptureThrow());
            SkillManager.Register(new SkillShadowStrike());
            SkillManager.Register(new SkillMountainCrash());
            SkillManager.Register(new SkillBladeDance());
            // 长枪 11~18
            SkillManager.Register(new SkillJavelinThrow());
            SkillManager.Register(new SkillPierceSpear());
            SkillManager.Register(new SkillThunderSpear());
            SkillManager.Register(new SkillBindNail());
            SkillManager.Register(new SkillThrustDash());
            SkillManager.Register(new SkillSweepPole());
            SkillManager.Register(new SkillSpearWall());
            SkillManager.Register(new SkillSpearRain());
            // 枪械 19~33
            SkillManager.Register(new SkillQuickShot());
            SkillManager.Register(new SkillFanShotgun());
            SkillManager.Register(new SkillArmorPiercer());
            SkillManager.Register(new SkillGrenade());
            SkillManager.Register(new SkillIncendiary());
            SkillManager.Register(new SkillFreezeRound());
            SkillManager.Register(new SkillParalysisRound());
            SkillManager.Register(new SkillToxicRound());
            SkillManager.Register(new SkillHeavyBreaker());
            SkillManager.Register(new SkillInspireRound());
            SkillManager.Register(new SkillImpactRound());
            SkillManager.Register(new SkillBlinkDash());
            SkillManager.Register(new SkillSnipe());
            SkillManager.Register(new SkillBulletSpray());
            SkillManager.Register(new SkillAirstrikeMark());
            // 魔法球 34~49
            SkillManager.Register(new SkillMagicBolt());
            SkillManager.Register(new SkillFireball());
            SkillManager.Register(new SkillIceShard());
            SkillManager.Register(new SkillWindBlade());
            SkillManager.Register(new SkillPoisonOrb());
            SkillManager.Register(new SkillRockfall());
            SkillManager.Register(new SkillThunderOrb());
            SkillManager.Register(new SkillLightShield());
            SkillManager.Register(new SkillPurgeWave());
            SkillManager.Register(new SkillInspireCircle());
            SkillManager.Register(new SkillFrostNova());
            SkillManager.Register(new SkillThunderFall());
            SkillManager.Register(new SkillBlackHoleBomb());
            SkillManager.Register(new SkillStarfall());
            SkillManager.Register(new SkillVoidGrasp());
            SkillManager.Register(new SkillArcaneBarrier());
        }
    }

    #region 近战刀 0~10
    public class SkillGaleSlash : SkillBase
    {
        public override int Id => (int)SkillType.SkillGaleSlash;
        public override float CastRange => 2f;
        public override float CD => 1f;
        public override int Store => 15;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Knife, 0);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            int shotSlot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (shotSlot < 0) shotSlot = 0;
            Vector3 shotOrigin = GetWeaponFloatPosition(entity, shotSlot).position;
            context.AddInts(entity.id, shotSlot);
            context.AddVectors(shotOrigin, Utils.TargetSelect.AimPos(entity,CastRange));
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            Vector3 shotOrigin = ShotOrigin((ushort)context.ints[0], context.ints[1]);
            return TrialConstructor.Line(shotOrigin, ShotAim(shotOrigin, context.vectors[index * 2], context.vectors[index * 2 + 1]), 0.7f);
        }
        protected override void OnCast(SkillContext context)
        {
            var shotCaster = FindEntity((ushort)context.ints[0]);
            var shotAttack = BuildAttack(shotCaster, 1f, 0.45f);
            if (Tool.BattleManager == null) return;
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                Tool.BattleManager.ShootBullet(shotCaster, shotAttack, CreateTrajectory(context, shotIndex));
        }
        public override void PlayVFX(SkillContext context)
        {
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.Bullet, 39, CreateTrajectory(context, shotIndex));
        }
    }

    public class SkillFlyingKnife : SkillBase
    {
        public override int Id => (int)SkillType.SkillFlyingKnife;
        public override float CastRange => 2f;
        public override float CD => 2f;
        public override int Store => 12;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Knife, 1);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            int shotSlot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (shotSlot < 0) shotSlot = 0;
            Vector3 shotOrigin = GetWeaponFloatPosition(entity, shotSlot).position;
            context.AddInts(entity.id, shotSlot);
            context.AddVectors(shotOrigin, Utils.TargetSelect.AimPos(entity,CastRange));
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            Vector3 shotOrigin = ShotOrigin((ushort)context.ints[0], context.ints[1]);
            return TrialConstructor.Line(shotOrigin, ShotAim(shotOrigin, context.vectors[index * 2], context.vectors[index * 2 + 1]), 0.9f);
        }
        protected override void OnCast(SkillContext context)
        {
            var shotCaster = FindEntity((ushort)context.ints[0]);
            var shotAttack = BuildAttack(shotCaster, 1.2f, 0.45f);
            if (Tool.BattleManager == null) return;
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                Tool.BattleManager.ShootBullet(shotCaster, shotAttack, CreateTrajectory(context, shotIndex));
        }
        public override void PlayVFX(SkillContext context)
        {
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.Weapon, (int)Weapon, CreateTrajectory(context, shotIndex));
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.Bullet, 45, CreateTrajectory(context, shotIndex));
        }
    }

    public class SkillCrossSlash : SkillBase
    {
        public override int Id => (int)SkillType.SkillCrossSlash;
        public override float CastRange => 2f;
        public override float CD => 5f;
        public override int Store => 10;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Knife, 2);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            int shotSlot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (shotSlot < 0) shotSlot = 0;
            Vector3 shotOrigin = GetWeaponFloatPosition(entity, shotSlot).position;
            context.AddInts(entity.id, shotSlot);
            foreach (var shotDest in Utils.SpreadStyle.FanDests(entity.transform.position, Utils.TargetSelect.AimPos(entity,CastRange), 2, 30f))
                context.AddVectors(shotOrigin, shotDest);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            Vector3 shotOrigin = ShotOrigin((ushort)context.ints[0], context.ints[1]);
            return TrialConstructor.Line(shotOrigin, ShotAim(shotOrigin, context.vectors[index * 2], context.vectors[index * 2 + 1]), 0.8f);
        }
        protected override void OnCast(SkillContext context)
        {
            var shotCaster = FindEntity((ushort)context.ints[0]);
            var shotAttack = BuildAttack(shotCaster, 1.6f, 0.5f);
            if (Tool.BattleManager == null) return;
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                Tool.BattleManager.ShootBullet(shotCaster, shotAttack, CreateTrajectory(context, shotIndex));
        }
        public override void PlayVFX(SkillContext context)
        {
            int[] shotVfx = new[] { 39, 41 };
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.Bullet, shotVfx[shotIndex], CreateTrajectory(context, shotIndex));
        }
    }

    public class SkillChargeSlash : SkillBase
    {
        public override int Id => (int)SkillType.SkillChargeSlash;
        public override float CD => 5f;
        public override int Store => 10;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Knife, 3);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            return context;
        }
        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            if (caster != null) caster.SetMotion(new MotionDash(0.4f, 10f)); // 冲锋位移（速度积分，不瞬移）
        }
        public override void PlayVFX(SkillContext context) => VfxHelper.PlayFollow(SkillVfxKind.Buff, 27, (ushort)context.ints[0]); // BF28
    }

    public class SkillWhirlSlash : SkillBase
    {
        public override int Id => (int)SkillType.SkillWhirlSlash;
        public override float CastRange => 2f;
        public override float CD => 8f;
        public override int Store => 5;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Knife, 4);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R_And_L;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            int shotSlot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (shotSlot < 0) shotSlot = 0;
            Vector3 shotOrigin = GetWeaponFloatPosition(entity, shotSlot).position;
            context.AddInts(entity.id, shotSlot);
            foreach (var shotDest in Utils.SpreadStyle.CircleDests(entity.transform.position, 3f, 8))
                context.AddVectors(shotOrigin, shotDest);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            Vector3 shotOrigin = ShotOrigin((ushort)context.ints[0], context.ints[1]);
            return TrialConstructor.Line(shotOrigin, ShotAim(shotOrigin, context.vectors[index * 2], context.vectors[index * 2 + 1]), 0.5f);
        }
        protected override void OnCast(SkillContext context)
        {
            var shotCaster = FindEntity((ushort)context.ints[0]);
            var shotAttack = BuildAttack(shotCaster, 1.8f, 0.5f);
            if (Tool.BattleManager == null) return;
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                Tool.BattleManager.ShootBullet(shotCaster, shotAttack, CreateTrajectory(context, shotIndex));
        }
        public override void PlayVFX(SkillContext context)
        {
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.Bullet, 40, CreateTrajectory(context, shotIndex));
        }
    }

    public class SkillBreakEndureSlash : SkillBase
    {
        public override int Id => (int)SkillType.SkillBreakEndureSlash;
        public override float CastRange => 2f;
        public override float CD => 10f;
        public override int Store => 3;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Knife, 5);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            int shotSlot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (shotSlot < 0) shotSlot = 0;
            Vector3 shotOrigin = GetWeaponFloatPosition(entity, shotSlot).position;
            context.AddInts(entity.id, shotSlot);
            context.AddVectors(shotOrigin, Utils.TargetSelect.AimPos(entity,CastRange));
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            Vector3 shotOrigin = ShotOrigin((ushort)context.ints[0], context.ints[1]);
            return TrialConstructor.Line(shotOrigin, ShotAim(shotOrigin, context.vectors[index * 2], context.vectors[index * 2 + 1]), 0.8f);
        }
        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            ushort casterId = caster != null ? caster.id : (ushort)0;
            var shotAttack = BuildAttack(caster, 2.2f, 0.5f, breakEndure: true,
                addEffect: ApplyEffect(EffectType.Stun, 1, Config.buff_duration_control, caster));
            if (Tool.BattleManager == null) return;
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                Tool.BattleManager.ShootBullet(caster, shotAttack, CreateTrajectory(context, shotIndex));
        }

        // 命中目标身上的 BF21 麻痹黄由客户端 Buff 表驱动（Config.buff_vfx）
        public override void PlayVFX(SkillContext context)
        {
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.Bullet, 41, CreateTrajectory(context, shotIndex));
        }
    }

    public class SkillFanSwordQi : SkillBase
    {
        public override int Id => (int)SkillType.SkillFanSwordQi;
        public override float CastRange => 2f;
        public override float CD => 3f;
        public override int Store => 12;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Knife, 6);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            int shotSlot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (shotSlot < 0) shotSlot = 0;
            Vector3 shotOrigin = GetWeaponFloatPosition(entity, shotSlot).position;
            context.AddInts(entity.id, shotSlot);
            foreach (var shotDest in Utils.SpreadStyle.FanDests(entity.transform.position, Utils.TargetSelect.AimPos(entity,CastRange), 3, 30f))
                context.AddVectors(shotOrigin, shotDest);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            Vector3 shotOrigin = ShotOrigin((ushort)context.ints[0], context.ints[1]);
            return TrialConstructor.Line(shotOrigin, ShotAim(shotOrigin, context.vectors[index * 2], context.vectors[index * 2 + 1]), 0.8f);
        }
        protected override void OnCast(SkillContext context)
        {
            var shotCaster = FindEntity((ushort)context.ints[0]);
            var shotAttack = BuildAttack(shotCaster, 1.3f, 0.45f);
            if (Tool.BattleManager == null) return;
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                Tool.BattleManager.ShootBullet(shotCaster, shotAttack, CreateTrajectory(context, shotIndex));
        }
        public override void PlayVFX(SkillContext context)
        {
            int[] shotVfx = new[] { 39, 40, 41 };
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.Bullet, shotVfx[shotIndex], CreateTrajectory(context, shotIndex));
        }
    }

    public class SkillCaptureThrow : SkillBase
    {
        public override int Id => (int)SkillType.SkillCaptureThrow;
        public override float CastRange => 2f;
        public override float CD => 12f;
        public override int Store => 4;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Knife, 7);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            int shotSlot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (shotSlot < 0) shotSlot = 0;
            Vector3 shotOrigin = GetWeaponFloatPosition(entity, shotSlot).position;
            context.AddInts(entity.id, shotSlot);
            context.AddVectors(shotOrigin, Utils.TargetSelect.AimPos(entity,CastRange));
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => TrialConstructor.Point(context.vectors[index * 2 + 1], 1.5f);
        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            ushort casterId = caster != null ? caster.id : (ushort)0;
            StrikeSphere(caster, context.vectors[1], 1.5f, BuildAttack(caster, 1f, 1.5f,
                addEffect: ApplyEffect(EffectType.Root, 1, Config.buff_duration_control, caster)));
        }
        public override void PlayVFX(SkillContext context)
        {
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.Weapon, (int)Weapon, CreateTrajectory(context, shotIndex));
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.MagicCircle, 2, CreateTrajectory(context, shotIndex)); // MC3
        }
    }

    public class SkillShadowStrike : SkillBase
    {
        public override int Id => (int)SkillType.SkillShadowStrike;
        public override float CD => 10f;
        public override int Store => 4;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Knife, 8);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            context.AddVectors(entity.transform.position, Utils.TargetSelect.AimPos(entity,CastRange));
            return context;
        }
        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            if (caster != null) caster.SetMotion(new MotionToPoint(context.vectors[1], 20f)); // 位移到目标点
        }
        public override void PlayVFX(SkillContext context)
        {
            VfxHelper.PlayAt(SkillVfxKind.MagicCircle, 6, context.vectors[0], 1.2f); // MC7 起点
            VfxHelper.PlayAt(SkillVfxKind.MagicCircle, 6, context.vectors[1], 1.2f); // MC7 终点
        }
    }

    public class SkillMountainCrash : SkillBase
    {
        public override int Id => (int)SkillType.SkillMountainCrash;
        public override float CastRange => 2f;
        public override float CD => 6f;
        public override int Store => 10;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Knife, 9);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Jump_Mega;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            context.AddVectors(entity.transform.position, Utils.TargetSelect.AimPos(entity,CastRange));
            return context;
        }
        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            StrikeSphere(caster, context.vectors[1], 1.2f, BuildAttack(caster, 2f, 1.2f));
        }
        public override void PlayVFX(SkillContext context)
            => VfxHelper.PlayAt(SkillVfxKind.RangeMagic, 4, context.vectors[1], 1.2f); // RM5
    }

    public class SkillBladeDance : SkillBase
    {
        public override int Id => (int)SkillType.SkillBladeDance;
        public override float CastRange => 2f;
        public override float CD => 4f;
        public override int Store => 12;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Knife, 10);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            return context;
        }
        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            StrikeSphere(caster, HandPos(caster).position, 0.6f, BuildAttack(caster, 1.4f, 0.6f));
        }
        public override void PlayVFX(SkillContext context) => VfxHelper.PlayFollow(SkillVfxKind.Bullet, 40, (ushort)context.ints[0]);
    }
    #endregion

    #region 长枪 11~18
    public class SkillJavelinThrow : SkillBase
    {
        public override int Id => (int)SkillType.SkillJavelinThrow;
        public override float CastRange => 8f;
        public override float CD => 1.5f;
        public override int Store => 15;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Spear, 0);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            int shotSlot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (shotSlot < 0) shotSlot = 0;
            Vector3 shotOrigin = GetWeaponFloatPosition(entity, shotSlot).position;
            context.AddInts(entity.id, shotSlot);
            context.AddVectors(shotOrigin, Utils.TargetSelect.AimPos(entity,CastRange));
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            Vector3 shotOrigin = ShotOrigin((ushort)context.ints[0], context.ints[1]);
            return TrialConstructor.Line(shotOrigin, ShotAim(shotOrigin, context.vectors[index * 2], context.vectors[index * 2 + 1]), 1f);
        }
        protected override void OnCast(SkillContext context)
        {
            var shotCaster = FindEntity((ushort)context.ints[0]);
            var shotAttack = BuildAttack(shotCaster, 1.2f, 0.45f);
            if (Tool.BattleManager == null) return;
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                Tool.BattleManager.ShootBullet(shotCaster, shotAttack, CreateTrajectory(context, shotIndex));
        }
        public override void PlayVFX(SkillContext context)
        {
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.Weapon, (int)Weapon, CreateTrajectory(context, shotIndex));
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.Bullet, 48, CreateTrajectory(context, shotIndex));
        }
    }

    public class SkillPierceSpear : SkillBase
    {
        public override int Id => (int)SkillType.SkillPierceSpear;
        public override float CastRange => 8f;
        public override float CD => 4f;
        public override int Store => 10;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Spear, 1);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            int shotSlot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (shotSlot < 0) shotSlot = 0;
            Vector3 shotOrigin = GetWeaponFloatPosition(entity, shotSlot).position;
            context.AddInts(entity.id, shotSlot);
            context.AddVectors(shotOrigin, Utils.TargetSelect.AimPos(entity,CastRange));
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            Vector3 shotOrigin = ShotOrigin((ushort)context.ints[0], context.ints[1]);
            return TrialConstructor.Line(shotOrigin, ShotAim(shotOrigin, context.vectors[index * 2], context.vectors[index * 2 + 1]), 1.2f);
        }
        protected override void OnCast(SkillContext context)
        {
            var shotCaster = FindEntity((ushort)context.ints[0]);
            var shotAttack = BuildAttack(shotCaster, 1.6f, 0.5f);
            if (Tool.BattleManager == null) return;
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                Tool.BattleManager.ShootBullet(shotCaster, shotAttack, CreateTrajectory(context, shotIndex));
        }
        public override void PlayVFX(SkillContext context)
        {
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.Bullet, 51, CreateTrajectory(context, shotIndex));
        }
    }

    public class SkillThunderSpear : SkillBase
    {
        public override int Id => (int)SkillType.SkillThunderSpear;
        public override float CastRange => 8f;
        public override float CD => 10f;
        public override int Store => 3;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Spear, 2);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            int shotSlot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (shotSlot < 0) shotSlot = 0;
            Vector3 shotOrigin = GetWeaponFloatPosition(entity, shotSlot).position;
            context.AddInts(entity.id, shotSlot);
            context.AddVectors(shotOrigin, Utils.TargetSelect.AimPos(entity,CastRange));
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            Vector3 shotOrigin = ShotOrigin((ushort)context.ints[0], context.ints[1]);
            return TrialConstructor.SkyFall(shotOrigin, ShotAim(shotOrigin, context.vectors[index * 2], context.vectors[index * 2 + 1]), 1.2f);
        }
        protected override void OnCast(SkillContext context)
        {
            var shotCaster = FindEntity((ushort)context.ints[0]);
            var shotAttack = BuildAttack(shotCaster, 1.9f, 0.8f);
            if (Tool.BattleManager == null) return;
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                Tool.BattleManager.ShootBullet(shotCaster, shotAttack, CreateTrajectory(context, shotIndex));
        }
        public override void PlayVFX(SkillContext context)
        {
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.Weapon, (int)Weapon, CreateTrajectory(context, shotIndex));
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.RangeMagic, 4, CreateTrajectory(context, shotIndex)); // RM5 落点
        }
    }

    public class SkillBindNail : SkillBase
    {
        public override int Id => (int)SkillType.SkillBindNail;
        public override float CastRange => 8f;
        public override float CD => 12f;
        public override int Store => 4;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Spear, 3);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            int shotSlot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (shotSlot < 0) shotSlot = 0;
            Vector3 shotOrigin = GetWeaponFloatPosition(entity, shotSlot).position;
            context.AddInts(entity.id, shotSlot);
            context.AddVectors(shotOrigin, Utils.TargetSelect.AimPos(entity,CastRange));
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => TrialConstructor.Point(context.vectors[index * 2 + 1], 1.5f);
        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            ushort casterId = caster != null ? caster.id : (ushort)0;
            StrikeSphere(caster, context.vectors[1], 1.5f, BuildAttack(caster, 1f, 1.5f,
                addEffect: ApplyEffect(EffectType.Root, 1, Config.buff_duration_control, caster)));
        }
        public override void PlayVFX(SkillContext context)
        {
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.MagicCircle, 2, CreateTrajectory(context, shotIndex)); // MC3
        }
    }

    public class SkillThrustDash : SkillBase
    {
        public override int Id => (int)SkillType.SkillThrustDash;
        public override float CD => 5f;
        public override int Store => 10;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Spear, 4);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            return context;
        }
        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            if (caster != null) caster.SetMotion(new MotionDash(0.3f, 12f)); // 突进位移
        }
        public override void PlayVFX(SkillContext context) => VfxHelper.PlayFollow(SkillVfxKind.Buff, 27, (ushort)context.ints[0]); // BF28
    }

    public class SkillSweepPole : SkillBase
    {
        public override int Id => (int)SkillType.SkillSweepPole;
        public override float CastRange => 2f;
        public override float CD => 6f;
        public override int Store => 10;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Spear, 5);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            return context;
        }
        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            StrikeSphere(caster, HandPos(caster).position, 0.9f, BuildAttack(caster, 1.5f, 0.9f, knockback: 3f));
        }
        public override void PlayVFX(SkillContext context) { } // 无特效
    }

    public class SkillSpearWall : SkillBase
    {
        public override int Id => (int)SkillType.SkillSpearWall;
        public override float CD => 20f;
        public override int Store => 2;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Spear, 6);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            int shotSlot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (shotSlot < 0) shotSlot = 0;
            Vector3 shotOrigin = GetWeaponFloatPosition(entity, shotSlot).position;
            context.AddInts(entity.id, shotSlot);
            context.AddVectors(shotOrigin, Utils.TargetSelect.AimPos(entity,CastRange));
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => TrialConstructor.Point(context.vectors[index * 2 + 1], 2f);
        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            if (caster == null) return;
            // 屏障不做阻挡：改为落点范围内的友方护盾
            BattleManager.EntityContainer.GetAllInCamp(context.vectors[1], 3f, caster.camp, Utils.TargetSelect.TargetBuffer);
            for (int i = 0; i < Utils.TargetSelect.TargetBuffer.Count; i++)
            {
                GiveEffect(Utils.TargetSelect.TargetBuffer[i], EffectType.Shield, 1, Config.buff_duration_buff,
                    caster);
            }
        }
        public override void PlayVFX(SkillContext context)
        {
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.MagicCircle, 0, CreateTrajectory(context, shotIndex)); // MC1
        }
    }

    public class SkillSpearRain : SkillBase
    {
        public override int Id => (int)SkillType.SkillSpearRain;
        public override float CastRange => 8f;
        public override float CD => 15f;
        public override int Store => 3;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Spear, 7);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            int shotSlot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (shotSlot < 0) shotSlot = 0;
            Vector3 shotOrigin = GetWeaponFloatPosition(entity, shotSlot).position;
            context.AddInts(entity.id, shotSlot);
            foreach (var shotDest in Utils.SpreadStyle.FanDests(entity.transform.position, Utils.TargetSelect.AimPos(entity,CastRange), 5, 30f))
                context.AddVectors(shotOrigin, shotDest);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            Vector3 shotOrigin = ShotOrigin((ushort)context.ints[0], context.ints[1]);
            return TrialConstructor.SkyFall(shotOrigin, ShotAim(shotOrigin, context.vectors[index * 2], context.vectors[index * 2 + 1]), 1.5f);
        }
        protected override void OnCast(SkillContext context)
        {
            var shotCaster = FindEntity((ushort)context.ints[0]);
            var shotAttack = BuildAttack(shotCaster, 2.4f, 0.7f);
            if (Tool.BattleManager == null) return;
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                Tool.BattleManager.ShootBullet(shotCaster, shotAttack, CreateTrajectory(context, shotIndex));
        }
        public override void PlayVFX(SkillContext context)
        {
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.Bullet, 47, CreateTrajectory(context, shotIndex));
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.RangeMagic, 4, CreateTrajectory(context, shotIndex)); // RM5 落点
        }
    }
    #endregion

    #region 枪械 19~33
    public class SkillQuickShot : SkillBase
    {
        public override int Id => (int)SkillType.SkillQuickShot;
        public override float CastRange => 18f;
        public override float CD => 0.5f;
        public override int Store => 20;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 0);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            int shotSlot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (shotSlot < 0) shotSlot = 0;
            Vector3 shotOrigin = GetWeaponFloatPosition(entity, shotSlot).position;
            context.AddInts(entity.id, shotSlot);
            context.AddVectors(shotOrigin, Utils.TargetSelect.AimPos(entity,CastRange));
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            Vector3 shotOrigin = ShotOrigin((ushort)context.ints[0], context.ints[1]);
            return TrialConstructor.Line(shotOrigin, ShotAim(shotOrigin, context.vectors[index * 2], context.vectors[index * 2 + 1]), 0.6f);
        }
        protected override void OnCast(SkillContext context)
        {
            var shotCaster = FindEntity((ushort)context.ints[0]);
            var shotAttack = BuildAttack(shotCaster, 1f, 0.4f);
            if (Tool.BattleManager == null) return;
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                Tool.BattleManager.ShootBullet(shotCaster, shotAttack, CreateTrajectory(context, shotIndex));
        }
        public override void PlayVFX(SkillContext context)
        {
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.Bullet, 42, CreateTrajectory(context, shotIndex));
        }
    }

    public class SkillFanShotgun : SkillBase
    {
        public override int Id => (int)SkillType.SkillFanShotgun;
        public override float CastRange => 18f;
        public override float CD => 2f;
        public override int Store => 15;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 1);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            int shotSlot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (shotSlot < 0) shotSlot = 0;
            Vector3 shotOrigin = GetWeaponFloatPosition(entity, shotSlot).position;
            context.AddInts(entity.id, shotSlot);
            foreach (var shotDest in Utils.SpreadStyle.FanDests(entity.transform.position, Utils.TargetSelect.AimPos(entity,CastRange), 3, 25f))
                context.AddVectors(shotOrigin, shotDest);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            Vector3 shotOrigin = ShotOrigin((ushort)context.ints[0], context.ints[1]);
            return TrialConstructor.Line(shotOrigin, ShotAim(shotOrigin, context.vectors[index * 2], context.vectors[index * 2 + 1]), 0.6f);
        }
        protected override void OnCast(SkillContext context)
        {
            var shotCaster = FindEntity((ushort)context.ints[0]);
            var shotAttack = BuildAttack(shotCaster, 1.2f, 0.4f);
            if (Tool.BattleManager == null) return;
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                Tool.BattleManager.ShootBullet(shotCaster, shotAttack, CreateTrajectory(context, shotIndex));
        }
        public override void PlayVFX(SkillContext context)
        {
            int[] shotVfx = new[] { 42, 43, 44 };
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.Bullet, shotVfx[shotIndex], CreateTrajectory(context, shotIndex));
        }
    }

    public class SkillArmorPiercer : SkillBase
    {
        public override int Id => (int)SkillType.SkillArmorPiercer;
        public override float CastRange => 18f;
        public override float CD => 2f;
        public override int Store => 12;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 2);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            int shotSlot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (shotSlot < 0) shotSlot = 0;
            Vector3 shotOrigin = GetWeaponFloatPosition(entity, shotSlot).position;
            context.AddInts(entity.id, shotSlot);
            context.AddVectors(shotOrigin, Utils.TargetSelect.AimPos(entity,CastRange));
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            Vector3 shotOrigin = ShotOrigin((ushort)context.ints[0], context.ints[1]);
            return TrialConstructor.Line(shotOrigin, ShotAim(shotOrigin, context.vectors[index * 2], context.vectors[index * 2 + 1]), 0.7f);
        }
        protected override void OnCast(SkillContext context)
        {
            var shotCaster = FindEntity((ushort)context.ints[0]);
            var shotAttack = BuildAttack(shotCaster, 1.5f, 0.4f);
            if (Tool.BattleManager == null) return;
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                Tool.BattleManager.ShootBullet(shotCaster, shotAttack, CreateTrajectory(context, shotIndex));
        }
        public override void PlayVFX(SkillContext context)
        {
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.Bullet, 45, CreateTrajectory(context, shotIndex));
        }
    }

    public class SkillGrenade : SkillBase
    {
        public override int Id => (int)SkillType.SkillGrenade;
        public override float CastRange => 18f;
        public override float CD => 3.5f;
        public override int Store => 10;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 3);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            int shotSlot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (shotSlot < 0) shotSlot = 0;
            Vector3 shotOrigin = GetWeaponFloatPosition(entity, shotSlot).position;
            context.AddInts(entity.id, shotSlot);
            context.AddVectors(shotOrigin, Utils.TargetSelect.AimPos(entity,CastRange));
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            Vector3 shotOrigin = ShotOrigin((ushort)context.ints[0], context.ints[1]);
            return TrialConstructor.Arc(shotOrigin, ShotAim(shotOrigin, context.vectors[index * 2], context.vectors[index * 2 + 1]), 1.2f);
        }
        protected override void OnCast(SkillContext context)
        {
            var shotCaster = FindEntity((ushort)context.ints[0]);
            var shotAttack = BuildAttack(shotCaster, 2f, 1f);
            if (Tool.BattleManager == null) return;
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                Tool.BattleManager.ShootBullet(shotCaster, shotAttack, CreateTrajectory(context, shotIndex));
        }
        public override void PlayVFX(SkillContext context)
        {
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.RangeMagic, 4, CreateTrajectory(context, shotIndex)); // RM5
        }
    }

    public class SkillIncendiary : SkillBase
    {
        // 策划案只规定「1s/跳、固定数值」，每跳伤害与持续时长待策划定稿
        private const float BurnDuration = 5f;

        public override int Id => (int)SkillType.SkillIncendiary;
        public override float CastRange => 18f;
        public override float CD => 4f;
        public override int Store => 12;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 4);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            int shotSlot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (shotSlot < 0) shotSlot = 0;
            Vector3 shotOrigin = GetWeaponFloatPosition(entity, shotSlot).position;
            context.AddInts(entity.id, shotSlot);
            context.AddVectors(shotOrigin, Utils.TargetSelect.AimPos(entity,CastRange));
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            Vector3 shotOrigin = ShotOrigin((ushort)context.ints[0], context.ints[1]);
            return TrialConstructor.Line(shotOrigin, ShotAim(shotOrigin, context.vectors[index * 2], context.vectors[index * 2 + 1]), 0.7f);
        }
        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            var shotAttack = BuildAttack(caster, 1.2f, 0.4f, addEffect: Burn(caster));
            if (Tool.BattleManager == null) return;
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                Tool.BattleManager.ShootBullet(caster, shotAttack, CreateTrajectory(context, shotIndex));
        }

        private static Action<EntityEffectController> Burn(EntityData caster) => effect => effect.AddEffect(EffectType.Burning, 1, BurnDuration, caster);
        public override void PlayVFX(SkillContext context)
        {
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.Bullet, 15, CreateTrajectory(context, shotIndex));
        }
    }

    public class SkillFreezeRound : SkillBase
    {
        public override int Id => (int)SkillType.SkillFreezeRound;
        public override float CastRange => 18f;
        public override float CD => 5f;
        public override int Store => 10;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 5);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            int shotSlot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (shotSlot < 0) shotSlot = 0;
            Vector3 shotOrigin = GetWeaponFloatPosition(entity, shotSlot).position;
            context.AddInts(entity.id, shotSlot);
            context.AddVectors(shotOrigin, Utils.TargetSelect.AimPos(entity,CastRange));
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            Vector3 shotOrigin = ShotOrigin((ushort)context.ints[0], context.ints[1]);
            return TrialConstructor.Line(shotOrigin, ShotAim(shotOrigin, context.vectors[index * 2], context.vectors[index * 2 + 1]), 0.7f);
        }
        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            ushort casterId = caster != null ? caster.id : (ushort)0;
            var shotAttack = BuildAttack(caster, 1f, 0.4f,
                addEffect: ApplyEffect(EffectType.Freeze, 1, Config.buff_duration_control, caster));
            if (Tool.BattleManager == null) return;
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                Tool.BattleManager.ShootBullet(caster, shotAttack, CreateTrajectory(context, shotIndex));
        }
        public override void PlayVFX(SkillContext context)
        {
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.Bullet, 18, CreateTrajectory(context, shotIndex));
        }
    }

    public class SkillParalysisRound : SkillBase
    {
        public override int Id => (int)SkillType.SkillParalysisRound;
        public override float CastRange => 18f;
        public override float CD => 5f;
        public override int Store => 10;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 6);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            int shotSlot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (shotSlot < 0) shotSlot = 0;
            Vector3 shotOrigin = GetWeaponFloatPosition(entity, shotSlot).position;
            context.AddInts(entity.id, shotSlot);
            context.AddVectors(shotOrigin, Utils.TargetSelect.AimPos(entity,CastRange));
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            Vector3 shotOrigin = ShotOrigin((ushort)context.ints[0], context.ints[1]);
            return TrialConstructor.Line(shotOrigin, ShotAim(shotOrigin, context.vectors[index * 2], context.vectors[index * 2 + 1]), 0.7f);
        }
        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            ushort casterId = caster != null ? caster.id : (ushort)0;
            var shotAttack = BuildAttack(caster, 1f, 0.4f,
                addEffect: ApplyEffect(EffectType.Stun, 1, Config.buff_duration_control, caster));
            if (Tool.BattleManager == null) return;
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                Tool.BattleManager.ShootBullet(caster, shotAttack, CreateTrajectory(context, shotIndex));
        }
        public override void PlayVFX(SkillContext context)
        {
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.Bullet, 5, CreateTrajectory(context, shotIndex));
        }
    }

    public class SkillToxicRound : SkillBase
    {
        public override int Id => (int)SkillType.SkillToxicRound;
        public override float CastRange => 18f;
        public override float CD => 6f;
        public override int Store => 10;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 7);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            int shotSlot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (shotSlot < 0) shotSlot = 0;
            Vector3 shotOrigin = GetWeaponFloatPosition(entity, shotSlot).position;
            context.AddInts(entity.id, shotSlot);
            context.AddVectors(shotOrigin, Utils.TargetSelect.AimPos(entity,CastRange));
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            Vector3 shotOrigin = ShotOrigin((ushort)context.ints[0], context.ints[1]);
            return TrialConstructor.Arc(shotOrigin, ShotAim(shotOrigin, context.vectors[index * 2], context.vectors[index * 2 + 1]), 1.2f);
        }
        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            ushort casterId = caster != null ? caster.id : (ushort)0;
            var shotAttack = BuildAttack(caster, 1f, 1f,
                addEffect: ApplyEffect(EffectType.Poison, 1, Config.buff_duration_debuff,
                    caster));
            if (Tool.BattleManager == null) return;
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                Tool.BattleManager.ShootBullet(caster, shotAttack, CreateTrajectory(context, shotIndex));
        }
        public override void PlayVFX(SkillContext context)
        {
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.RangeMagic, 0, CreateTrajectory(context, shotIndex)); // RM1
        }
    }

    public class SkillHeavyBreaker : SkillBase
    {
        public override int Id => (int)SkillType.SkillHeavyBreaker;
        public override float CastRange => 18f;
        public override float CD => 4f;
        public override int Store => 12;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 8);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            int shotSlot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (shotSlot < 0) shotSlot = 0;
            Vector3 shotOrigin = GetWeaponFloatPosition(entity, shotSlot).position;
            context.AddInts(entity.id, shotSlot);
            context.AddVectors(shotOrigin, Utils.TargetSelect.AimPos(entity,CastRange));
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            Vector3 shotOrigin = ShotOrigin((ushort)context.ints[0], context.ints[1]);
            return TrialConstructor.Line(shotOrigin, ShotAim(shotOrigin, context.vectors[index * 2], context.vectors[index * 2 + 1]), 0.7f);
        }
        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            ushort casterId = caster != null ? caster.id : (ushort)0;
            var shotAttack = BuildAttack(caster, 1.6f, 0.45f,
                addEffect: ApplyEffect(EffectType.AnimSlowDown, 1, Config.buff_duration_debuff, caster));
            if (Tool.BattleManager == null) return;
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                Tool.BattleManager.ShootBullet(caster, shotAttack, CreateTrajectory(context, shotIndex));
        }
        public override void PlayVFX(SkillContext context)
        {
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.Bullet, 49, CreateTrajectory(context, shotIndex));
        }
    }

    public class SkillInspireRound : SkillBase
    {
        public override int Id => (int)SkillType.SkillInspireRound;
        public override float CD => 8f;
        public override int Store => 10;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 9);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            int shotSlot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (shotSlot < 0) shotSlot = 0;
            context.AddInts(entity.id, shotSlot);
            var ally = Utils.TargetSelect.GetNearestAlly(entity);
            context.AddInts(ally != null ? ally.id : entity.id); // 目标（无友方时指向自己）
            // 无友方：正前方发射，避免终点=自身把泡泡冻结在施法者
            Vector3 dest = ally != null
                ? ally.transform.position
                : entity.transform.position + entity.transform.forward * Config.default_skill_auto_target_radius;
            context.AddVectors(GetWeaponFloatPosition(entity, shotSlot).position, dest);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            Vector3 shotOrigin = ShotOrigin((ushort)context.ints[0], context.ints[1]);
            return TrialConstructor.Line(shotOrigin, ShotAim(shotOrigin, context.vectors[index * 2], context.vectors[index * 2 + 1]), 0.7f);
        }
        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            ushort casterId = caster != null ? caster.id : (ushort)0;
            GiveEffect(caster, EffectType.AnimSpeedUp, 1, Config.buff_duration_buff, caster);
            for (int i = 0; i < (context.ints.Count - 2); i++)
            {
                var ally = BattleManager.GetEntity((ushort)context.ints[i + 2]);
                if (ally != null && ally.id != casterId)
                {
                    GiveEffect(ally, EffectType.AnimSpeedUp, 1, Config.buff_duration_buff, caster);
                }
            }
        }
        // 泡泡球仅为表现：服务器不做子弹命中判定
        public override void PlayVFX(SkillContext context)
        {
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.Bullet, 57, CreateTrajectory(context, shotIndex)); // B57 蓝泡泡球
            VfxHelper.PlayFollow(SkillVfxKind.Buff, 27, (ushort)context.ints[0]);  // BF28 环绕风（自身）
        }
    }

    public class SkillImpactRound : SkillBase
    {
        public override int Id => (int)SkillType.SkillImpactRound;
        public override float CastRange => 18f;
        public override float CD => 8f;
        public override int Store => 10;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 10);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            int shotSlot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (shotSlot < 0) shotSlot = 0;
            Vector3 shotOrigin = GetWeaponFloatPosition(entity, shotSlot).position;
            context.AddInts(entity.id, shotSlot);
            context.AddVectors(shotOrigin, Utils.TargetSelect.AimPos(entity,CastRange));
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            Vector3 shotOrigin = ShotOrigin((ushort)context.ints[0], context.ints[1]);
            return TrialConstructor.Arc(shotOrigin, ShotAim(shotOrigin, context.vectors[index * 2], context.vectors[index * 2 + 1]), 1.1f);
        }
        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            if (Tool.BattleManager == null) return;
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                Tool.BattleManager.ShootBullet(caster, BuildAttack(caster, 0.4f, 1.2f, knockback: 5f), CreateTrajectory(context, shotIndex));
        }
        public override void PlayVFX(SkillContext context)
        {
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.RangeMagic, 4, CreateTrajectory(context, shotIndex)); // RM5
        }
    }

    public class SkillBlinkDash : SkillBase
    {
        public override int Id => (int)SkillType.SkillBlinkDash;
        public override float CD => 12f;
        public override int Store => 4;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 11);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            context.AddVectors(Utils.TargetSelect.AimPos(entity,CastRange));
            return context;
        }
        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            if (caster != null) caster.SetMotion(new MotionToPoint(context.vectors[0], 28f)); // 长距闪现
        }
        public override void PlayVFX(SkillContext context) => VfxHelper.PlayFollow(SkillVfxKind.Buff, 27, (ushort)context.ints[0]); // BF28
    }

    public class SkillSnipe : SkillBase
    {
        public override int Id => (int)SkillType.SkillSnipe;
        public override float CastRange => 18f;
        public override float CD => 10f;
        public override int Store => 5;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 12);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            int shotSlot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (shotSlot < 0) shotSlot = 0;
            Vector3 shotOrigin = GetWeaponFloatPosition(entity, shotSlot).position;
            context.AddInts(entity.id, shotSlot);
            context.AddVectors(shotOrigin, Utils.TargetSelect.AimPos(entity,CastRange));
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            Vector3 shotOrigin = ShotOrigin((ushort)context.ints[0], context.ints[1]);
            return TrialConstructor.Line(shotOrigin, ShotAim(shotOrigin, context.vectors[index * 2], context.vectors[index * 2 + 1]), 0.5f);
        }
        protected override void OnCast(SkillContext context)
        {
            var shotCaster = FindEntity((ushort)context.ints[0]);
            var shotAttack = BuildAttack(shotCaster, 3f, 0.35f);
            if (Tool.BattleManager == null) return;
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                Tool.BattleManager.ShootBullet(shotCaster, shotAttack, CreateTrajectory(context, shotIndex));
        }
        public override void PlayVFX(SkillContext context)
        {
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.Bullet, 45, CreateTrajectory(context, shotIndex));
        }
    }

    public class SkillBulletSpray : SkillBase
    {
        public override int Id => (int)SkillType.SkillBulletSpray;
        public override float CastRange => 18f;
        public override float CD => 15f;
        public override int Store => 3;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 13);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            int shotSlot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (shotSlot < 0) shotSlot = 0;
            Vector3 shotOrigin = GetWeaponFloatPosition(entity, shotSlot).position;
            context.AddInts(entity.id, shotSlot);
            foreach (var shotDest in Utils.SpreadStyle.FanDests(entity.transform.position, Utils.TargetSelect.AimPos(entity,CastRange), 8, 30f))
                context.AddVectors(shotOrigin, shotDest);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            Vector3 shotOrigin = ShotOrigin((ushort)context.ints[0], context.ints[1]);
            return TrialConstructor.Line(shotOrigin, ShotAim(shotOrigin, context.vectors[index * 2], context.vectors[index * 2 + 1]), 0.6f);
        }
        protected override void OnCast(SkillContext context)
        {
            var shotCaster = FindEntity((ushort)context.ints[0]);
            var shotAttack = BuildAttack(shotCaster, 1f, 0.4f);
            if (Tool.BattleManager == null) return;
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                Tool.BattleManager.ShootBullet(shotCaster, shotAttack, CreateTrajectory(context, shotIndex));
        }
        public override void PlayVFX(SkillContext context)
        {
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.Bullet, 44, CreateTrajectory(context, shotIndex));
        }
    }

    public class SkillAirstrikeMark : SkillBase
    {
        public override int Id => (int)SkillType.SkillAirstrikeMark;
        public override float CastRange => 18f;
        public override float CD => 20f;
        public override int Store => 1;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 14);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            context.AddVectors(entity.transform.position, Utils.TargetSelect.AimPos(entity,CastRange));
            return context;
        }
        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            // 不延时：动画攻击帧直接对标记落点结算轰炸
            StrikeSphere(caster, context.vectors[1], Config.airstrike_radius,
                BuildAttack(caster, 2f, Config.airstrike_radius, useMagic: true));
        }
        public override void PlayVFX(SkillContext context)
        {
            VfxHelper.PlayAt(SkillVfxKind.MagicCircle, 7, context.vectors[1], 2f);  // MC8 标记
            VfxHelper.PlayAt(SkillVfxKind.RangeMagic, 5, context.vectors[1], 1.5f); // RM6 光柱
        }
    }
    #endregion

    #region 魔法球 34~49
    public class SkillMagicBolt : SkillBase
    {
        public override int Id => (int)SkillType.SkillMagicBolt;
        public override float CastRange => 15f;
        public override float CD => 0.6f;
        public override int Store => 20;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 0);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            int shotSlot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (shotSlot < 0) shotSlot = 0;
            Vector3 shotOrigin = GetWeaponFloatPosition(entity, shotSlot).position;
            context.AddInts(entity.id, shotSlot);
            context.AddVectors(shotOrigin, Utils.TargetSelect.AimPos(entity,CastRange));
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            Vector3 shotOrigin = ShotOrigin((ushort)context.ints[0], context.ints[1]);
            return TrialConstructor.Line(shotOrigin, ShotAim(shotOrigin, context.vectors[index * 2], context.vectors[index * 2 + 1]), 0.7f);
        }
        protected override void OnCast(SkillContext context)
        {
            var shotCaster = FindEntity((ushort)context.ints[0]);
            var shotAttack = BuildAttack(shotCaster, 1f, 0.4f, useMagic: true);
            if (Tool.BattleManager == null) return;
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                Tool.BattleManager.ShootBullet(shotCaster, shotAttack, CreateTrajectory(context, shotIndex));
        }
        public override void PlayVFX(SkillContext context)
        {
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.Bullet, 0, CreateTrajectory(context, shotIndex));
        }
    }

    public class SkillFireball : SkillBase
    {
        public override int Id => (int)SkillType.SkillFireball;
        public override float CastRange => 15f;
        public override float CD => 3f;
        public override int Store => 12;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 1);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            int shotSlot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (shotSlot < 0) shotSlot = 0;
            Vector3 shotOrigin = GetWeaponFloatPosition(entity, shotSlot).position;
            context.AddInts(entity.id, shotSlot);
            context.AddVectors(shotOrigin, Utils.TargetSelect.AimPos(entity,CastRange));
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            Vector3 shotOrigin = ShotOrigin((ushort)context.ints[0], context.ints[1]);
            return TrialConstructor.Arc(shotOrigin, ShotAim(shotOrigin, context.vectors[index * 2], context.vectors[index * 2 + 1]), 1f);
        }
        protected override void OnCast(SkillContext context)
        {
            var shotCaster = FindEntity((ushort)context.ints[0]);
            var shotAttack = BuildAttack(shotCaster, 1.5f, 1f, useMagic: true);
            if (Tool.BattleManager == null) return;
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                Tool.BattleManager.ShootBullet(shotCaster, shotAttack, CreateTrajectory(context, shotIndex));
        }
        public override void PlayVFX(SkillContext context)
        {
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.Bullet, 15, CreateTrajectory(context, shotIndex));
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.RangeMagic, 4, CreateTrajectory(context, shotIndex)); // RM5
        }
    }

    public class SkillIceShard : SkillBase
    {
        public override int Id => (int)SkillType.SkillIceShard;
        public override float CastRange => 15f;
        public override float CD => 3f;
        public override int Store => 12;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 2);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            int shotSlot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (shotSlot < 0) shotSlot = 0;
            Vector3 shotOrigin = GetWeaponFloatPosition(entity, shotSlot).position;
            context.AddInts(entity.id, shotSlot);
            context.AddVectors(shotOrigin, Utils.TargetSelect.AimPos(entity,CastRange));
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            Vector3 shotOrigin = ShotOrigin((ushort)context.ints[0], context.ints[1]);
            return TrialConstructor.Line(shotOrigin, ShotAim(shotOrigin, context.vectors[index * 2], context.vectors[index * 2 + 1]), 0.8f);
        }
        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            ushort casterId = caster != null ? caster.id : (ushort)0;
            var shotAttack = BuildAttack(caster, 1.2f, 0.4f, useMagic: true,
                addEffect: ApplyEffect(EffectType.Freeze, 1, Config.buff_duration_control, caster));
            if (Tool.BattleManager == null) return;
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                Tool.BattleManager.ShootBullet(caster, shotAttack, CreateTrajectory(context, shotIndex));
        }
        public override void PlayVFX(SkillContext context)
        {
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.Bullet, 18, CreateTrajectory(context, shotIndex));
        }
    }

    public class SkillWindBlade : SkillBase
    {
        public override int Id => (int)SkillType.SkillWindBlade;
        public override float CastRange => 15f;
        public override float CD => 2.5f;
        public override int Store => 12;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 3);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            int shotSlot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (shotSlot < 0) shotSlot = 0;
            Vector3 shotOrigin = GetWeaponFloatPosition(entity, shotSlot).position;
            context.AddInts(entity.id, shotSlot);
            context.AddVectors(shotOrigin, Utils.TargetSelect.AimPos(entity,CastRange));
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            Vector3 shotOrigin = ShotOrigin((ushort)context.ints[0], context.ints[1]);
            return TrialConstructor.Line(shotOrigin, ShotAim(shotOrigin, context.vectors[index * 2], context.vectors[index * 2 + 1]), 0.8f);
        }
        protected override void OnCast(SkillContext context)
        {
            var shotCaster = FindEntity((ushort)context.ints[0]);
            var shotAttack = BuildAttack(shotCaster, 1.3f, 0.45f, useMagic: true);
            if (Tool.BattleManager == null) return;
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                Tool.BattleManager.ShootBullet(shotCaster, shotAttack, CreateTrajectory(context, shotIndex));
        }
        public override void PlayVFX(SkillContext context)
        {
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.Bullet, 46, CreateTrajectory(context, shotIndex));
        }
    }

    public class SkillPoisonOrb : SkillBase
    {
        public override int Id => (int)SkillType.SkillPoisonOrb;
        public override float CastRange => 15f;
        public override float CD => 4f;
        public override int Store => 10;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 4);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            int shotSlot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (shotSlot < 0) shotSlot = 0;
            Vector3 shotOrigin = GetWeaponFloatPosition(entity, shotSlot).position;
            context.AddInts(entity.id, shotSlot);
            context.AddVectors(shotOrigin, Utils.TargetSelect.AimPos(entity,CastRange));
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            Vector3 shotOrigin = ShotOrigin((ushort)context.ints[0], context.ints[1]);
            return TrialConstructor.Line(shotOrigin, ShotAim(shotOrigin, context.vectors[index * 2], context.vectors[index * 2 + 1]), 0.8f);
        }
        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            ushort casterId = caster != null ? caster.id : (ushort)0;
            var shotAttack = BuildAttack(caster, 1f, 0.4f, useMagic: true,
                addEffect: ApplyEffect(EffectType.Poison, 1, Config.buff_duration_debuff,
                    caster));
            if (Tool.BattleManager == null) return;
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                Tool.BattleManager.ShootBullet(caster, shotAttack, CreateTrajectory(context, shotIndex));
        }
        public override void PlayVFX(SkillContext context)
        {
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.Bullet, 10, CreateTrajectory(context, shotIndex));
        }
    }

    public class SkillRockfall : SkillBase
    {
        public override int Id => (int)SkillType.SkillRockfall;
        public override float CastRange => 15f;
        public override float CD => 5f;
        public override int Store => 10;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 5);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            int shotSlot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (shotSlot < 0) shotSlot = 0;
            Vector3 shotOrigin = GetWeaponFloatPosition(entity, shotSlot).position;
            context.AddInts(entity.id, shotSlot);
            context.AddVectors(shotOrigin, Utils.TargetSelect.AimPos(entity,CastRange));
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            Vector3 shotOrigin = ShotOrigin((ushort)context.ints[0], context.ints[1]);
            return TrialConstructor.SkyFall(shotOrigin, ShotAim(shotOrigin, context.vectors[index * 2], context.vectors[index * 2 + 1]), 1.1f);
        }
        protected override void OnCast(SkillContext context)
        {
            var shotCaster = FindEntity((ushort)context.ints[0]);
            var shotAttack = BuildAttack(shotCaster, 1.6f, 0.9f, useMagic: true);
            if (Tool.BattleManager == null) return;
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                Tool.BattleManager.ShootBullet(shotCaster, shotAttack, CreateTrajectory(context, shotIndex));
        }
        public override void PlayVFX(SkillContext context)
        {
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.Bullet, 25, CreateTrajectory(context, shotIndex));
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.RangeMagic, 4, CreateTrajectory(context, shotIndex)); // RM5
        }
    }

    public class SkillThunderOrb : SkillBase
    {
        public override int Id => (int)SkillType.SkillThunderOrb;
        public override float CastRange => 15f;
        public override float CD => 5f;
        public override int Store => 10;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 6);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            int shotSlot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (shotSlot < 0) shotSlot = 0;
            context.AddInts(entity.id, shotSlot);
            // 链式跳：记录「发射者 → 第一目标 → 第二目标」三个 id
            var first = Utils.TargetSelect.GetNearestEnemy(entity, CastRange);
            var second = first != null
                ? BattleManager.EntityContainer.GetNearestEnemy(first, Config.chain_jump_radius)
                : null;
            context.AddInts(first != null ? first.id : entity.id);
            context.AddInts(second != null ? second.id : (first != null ? first.id : entity.id));
            // 无敌人：存正前方直射终点，避免回退自身把弹道冻结在施法者
            if (first == null) context.AddVectors(GetWeaponFloatPosition(entity, shotSlot).position, Utils.TargetSelect.AimPos(entity,CastRange));
            return context;
        }

        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            // 首目标 id == 施法者 → 无敌人（见 SkillLogic）：改走正前方直射
            if ((ushort)context.ints[2] == (ushort)context.ints[0])
            {
                Vector3 shotOrigin = ShotOrigin((ushort)context.ints[0], context.ints[1]);
                return TrialConstructor.Line(shotOrigin, ShotAim(shotOrigin, context.vectors[0], context.vectors[1]), Config.chain_jump_duration);
            }
            var trajectory = new ChainTrajectory((ushort)context.ints[0], (ushort)context.ints[2], (ushort)context.ints[3]);
            trajectory.Duration = Config.chain_jump_duration;
            return trajectory;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            var attack = BuildAttack(caster, 1.4f, 0.45f, useMagic: true);
            if (Tool.BattleManager != null) Tool.BattleManager.ShootBullet(caster, attack, CreateTrajectory(context, 0));
        }
        public override void PlayVFX(SkillContext context)
        {
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.Bullet, 3, CreateTrajectory(context, shotIndex));
        }
    }

    public class SkillLightShield : SkillBase
    {
        public override int Id => (int)SkillType.SkillLightShield;
        public override float CD => 10f;
        public override int Store => 8;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 7);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            return context;
        }
        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            if (caster == null) return;
            GiveEffect(caster, EffectType.Shield, 1, Config.buff_duration_buff,
                caster);
            var ally = Utils.TargetSelect.GetNearestAlly(caster);
            if (ally != null)
            {
                GiveEffect(ally, EffectType.Shield, 1, Config.buff_duration_buff,
                    caster);
            }
        }
        public override void PlayVFX(SkillContext context) => VfxHelper.PlayFollow(SkillVfxKind.Shield, 5, (ushort)context.ints[0]); // S6
    }

    public class SkillPurgeWave : SkillBase
    {
        public override int Id => (int)SkillType.SkillPurgeWave;
        public override float CD => 10f;
        public override int Store => 8;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 8);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            context.AddVectors(entity.transform.position);
            return context;
        }
        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            if (caster != null && caster.effectController != null) caster.effectController.RemoveAllNegative(); // 净化：移除自身全部负面 Buff
        }
        public override void PlayVFX(SkillContext context)
            => VfxHelper.PlayAt(SkillVfxKind.MagicCircle, 1, context.vectors[0], 1.2f); // MC2
    }

    public class SkillInspireCircle : SkillBase
    {
        public override int Id => (int)SkillType.SkillInspireCircle;
        public override float CD => 10f;
        public override int Store => 8;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 9);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            int shotSlot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (shotSlot < 0) shotSlot = 0;
            Vector3 shotOrigin = GetWeaponFloatPosition(entity, shotSlot).position;
            context.AddInts(entity.id, shotSlot);
            context.AddVectors(shotOrigin, Utils.TargetSelect.AimPos(entity,CastRange));
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => TrialConstructor.Point(context.vectors[index * 2 + 1], 2f);
        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            if (caster == null) return;
            BattleManager.EntityContainer.GetAllInCamp(context.vectors[1], 3f, caster.camp, Utils.TargetSelect.TargetBuffer);
            for (int i = 0; i < Utils.TargetSelect.TargetBuffer.Count; i++)
            {
                GiveEffect(Utils.TargetSelect.TargetBuffer[i], EffectType.AttrStrengthBoost, 1, Config.buff_duration_buff,
                    caster);
            }
        }
        public override void PlayVFX(SkillContext context)
        {
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.MagicCircle, 9, CreateTrajectory(context, shotIndex)); // MC10
        }
    }

    public class SkillFrostNova : SkillBase
    {
        public override int Id => (int)SkillType.SkillFrostNova;
        public override float CastRange => 3f;
        public override float CD => 10f;
        public override int Store => 6;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 10);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            int shotSlot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (shotSlot < 0) shotSlot = 0;
            Vector3 shotOrigin = GetWeaponFloatPosition(entity, shotSlot).position;
            context.AddInts(entity.id, shotSlot);
            foreach (var shotDest in Utils.SpreadStyle.CircleDests(entity.transform.position, 3f, 8))
                context.AddVectors(shotOrigin, shotDest);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            Vector3 shotOrigin = ShotOrigin((ushort)context.ints[0], context.ints[1]);
            return TrialConstructor.Line(shotOrigin, ShotAim(shotOrigin, context.vectors[index * 2], context.vectors[index * 2 + 1]), 0.7f);
        }
        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            ushort casterId = caster != null ? caster.id : (ushort)0;
            var shotAttack = BuildAttack(caster, 1.2f, 0.5f, useMagic: true,
                addEffect: ApplyEffect(EffectType.Freeze, 1, Config.buff_duration_control, caster));
            if (Tool.BattleManager == null) return;
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                Tool.BattleManager.ShootBullet(caster, shotAttack, CreateTrajectory(context, shotIndex));
        }
        public override void PlayVFX(SkillContext context)
        {
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.Bullet, 20, CreateTrajectory(context, shotIndex));
        }
    }

    public class SkillThunderFall : SkillBase
    {
        public override int Id => (int)SkillType.SkillThunderFall;
        public override float CastRange => 15f;
        public override float CD => 10f;
        public override int Store => 5;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 11);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            int shotSlot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (shotSlot < 0) shotSlot = 0;
            Vector3 shotOrigin = GetWeaponFloatPosition(entity, shotSlot).position;
            context.AddInts(entity.id, shotSlot);
            context.AddVectors(shotOrigin, Utils.TargetSelect.AimPos(entity,CastRange));
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            Vector3 shotOrigin = ShotOrigin((ushort)context.ints[0], context.ints[1]);
            return TrialConstructor.SkyFall(shotOrigin, ShotAim(shotOrigin, context.vectors[index * 2], context.vectors[index * 2 + 1]), 1.2f);
        }
        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            ushort casterId = caster != null ? caster.id : (ushort)0;
            var shotAttack = BuildAttack(caster, 2f, 1f, useMagic: true,
                addEffect: ApplyEffect(EffectType.Stun, 1, Config.buff_duration_control, caster));
            if (Tool.BattleManager == null) return;
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                Tool.BattleManager.ShootBullet(caster, shotAttack, CreateTrajectory(context, shotIndex));
        }
        public override void PlayVFX(SkillContext context)
        {
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.Bullet, 3, CreateTrajectory(context, shotIndex));
        }
    }

    public class SkillBlackHoleBomb : SkillBase
    {
        public override int Id => (int)SkillType.SkillBlackHoleBomb;
        public override float CastRange => 15f;
        public override float CD => 5f;
        public override int Store => 8;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 12);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            int shotSlot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (shotSlot < 0) shotSlot = 0;
            Vector3 shotOrigin = GetWeaponFloatPosition(entity, shotSlot).position;
            context.AddInts(entity.id, shotSlot);
            context.AddVectors(shotOrigin, Utils.TargetSelect.AimPos(entity,CastRange));
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            Vector3 shotOrigin = ShotOrigin((ushort)context.ints[0], context.ints[1]);
            return TrialConstructor.Arc(shotOrigin, ShotAim(shotOrigin, context.vectors[index * 2], context.vectors[index * 2 + 1]), 1.1f);
        }
        protected override void OnCast(SkillContext context)
        {
            var shotCaster = FindEntity((ushort)context.ints[0]);
            var shotAttack = BuildAttack(shotCaster, 2f, 1.5f, useMagic: true);
            if (Tool.BattleManager == null) return;
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                Tool.BattleManager.ShootBullet(shotCaster, shotAttack, CreateTrajectory(context, shotIndex));
        }
        public override void PlayVFX(SkillContext context)
        {
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.RangeMagic, 1, CreateTrajectory(context, shotIndex)); // RM2
        }
    }

    public class SkillStarfall : SkillBase
    {
        public override int Id => (int)SkillType.SkillStarfall;
        public override float CastRange => 15f;
        public override float CD => 20f;
        public override int Store => 3;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 13);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            int shotSlot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (shotSlot < 0) shotSlot = 0;
            Vector3 shotOrigin = GetWeaponFloatPosition(entity, shotSlot).position;
            context.AddInts(entity.id, shotSlot);
            foreach (var shotDest in Utils.SpreadStyle.FanDests(entity.transform.position, Utils.TargetSelect.AimPos(entity,CastRange), 5, 30f))
                context.AddVectors(shotOrigin, shotDest);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            Vector3 shotOrigin = ShotOrigin((ushort)context.ints[0], context.ints[1]);
            return TrialConstructor.SkyFall(shotOrigin, ShotAim(shotOrigin, context.vectors[index * 2], context.vectors[index * 2 + 1]), 1.6f);
        }
        protected override void OnCast(SkillContext context)
        {
            var shotCaster = FindEntity((ushort)context.ints[0]);
            var shotAttack = BuildAttack(shotCaster, 2.2f, 0.9f, useMagic: true);
            if (Tool.BattleManager == null) return;
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                Tool.BattleManager.ShootBullet(shotCaster, shotAttack, CreateTrajectory(context, shotIndex));
        }
        public override void PlayVFX(SkillContext context)
        {
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.Bullet, 31, CreateTrajectory(context, shotIndex));
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.RangeMagic, 4, CreateTrajectory(context, shotIndex)); // RM5
        }
    }

    public class SkillVoidGrasp : SkillBase
    {
        public override int Id => (int)SkillType.SkillVoidGrasp;
        public override float CastRange => 15f;
        public override float CD => 15f;
        public override int Store => 4;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 14);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            int shotSlot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (shotSlot < 0) shotSlot = 0;
            Vector3 shotOrigin = GetWeaponFloatPosition(entity, shotSlot).position;
            context.AddInts(entity.id, shotSlot);
            context.AddVectors(shotOrigin, Utils.TargetSelect.AimPos(entity,CastRange));
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            Vector3 shotOrigin = ShotOrigin((ushort)context.ints[0], context.ints[1]);
            return TrialConstructor.Line(shotOrigin, ShotAim(shotOrigin, context.vectors[index * 2], context.vectors[index * 2 + 1]), 0.8f);
        }
        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            Vector3 casterPos = caster != null ? caster.transform.position : Vector3.zero;
            var shotAttack = BuildAttack(caster, 1.2f, 0.6f, useMagic: true,
                onHit: PullTo(casterPos));
            if (Tool.BattleManager == null) return;
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                Tool.BattleManager.ShootBullet(caster, shotAttack, CreateTrajectory(context, shotIndex));
        }
        public override void PlayVFX(SkillContext context)
        {
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.Bullet, 1, CreateTrajectory(context, shotIndex));
        }
    }

    public class SkillArcaneBarrier : SkillBase
    {
        public override int Id => (int)SkillType.SkillArcaneBarrier;
        public override float CD => 20f;
        public override int Store => 2;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 15);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Weapon_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            int shotSlot = entity.skillController != null ? entity.skillController.CastingSlotIndex : -1;
            if (shotSlot < 0) shotSlot = 0;
            Vector3 shotOrigin = GetWeaponFloatPosition(entity, shotSlot).position;
            context.AddInts(entity.id, shotSlot);
            context.AddVectors(shotOrigin, Utils.TargetSelect.AimPos(entity,CastRange));
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => TrialConstructor.Point(context.vectors[index * 2 + 1], 2f);
        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            if (caster == null) return;
            // 屏障不做阻挡：改为落点范围内的友方护盾
            BattleManager.EntityContainer.GetAllInCamp(context.vectors[1], 3f, caster.camp, Utils.TargetSelect.TargetBuffer);
            for (int i = 0; i < Utils.TargetSelect.TargetBuffer.Count; i++)
            {
                GiveEffect(Utils.TargetSelect.TargetBuffer[i], EffectType.Shield, 1, Config.buff_duration_buff,
                    caster);
            }
        }
        public override void PlayVFX(SkillContext context)
        {
            for (int shotIndex = 0; shotIndex < context.vectors.Count / 2; shotIndex++)
                VfxHelper.PlayOne(SkillVfxKind.MagicCircle, 6, CreateTrajectory(context, shotIndex)); // MC7
        }
    }
    #endregion
}
