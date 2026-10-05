using System;
using Ros.Transport;
using UnityEngine;

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
        public override int Id => 0;
        public override float CastRange => 2f;
        public override float CD => 1f;
        public override int Store => 15;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Knife, 0);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = SkillBase.SkillContextConventions.BuildShotContext(this, entity, ProjectilePattern.Line, Utils.TargetSelect.AimPos(entity,CastRange));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.7f);
        protected override void OnCast(SkillContext context) => ShootAll(SkillBase.SkillContextConventions.GetCasterById(context), context, BuildAttack(SkillBase.SkillContextConventions.GetCasterById(context), 1f, 0.45f));
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 39 });
    }

    public class SkillFlyingKnife : SkillBase
    {
        public override int Id => 1;
        public override float CastRange => 2f;
        public override float CD => 2f;
        public override int Store => 12;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Knife, 1);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = SkillBase.SkillContextConventions.BuildShotContext(this, entity, ProjectilePattern.Line, Utils.TargetSelect.AimPos(entity,CastRange));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.9f);
        protected override void OnCast(SkillContext context) => ShootAll(SkillBase.SkillContextConventions.GetCasterById(context), context, BuildAttack(SkillBase.SkillContextConventions.GetCasterById(context), 1.2f, 0.45f));
        public override void PlayVFX(SkillContext context)
        {
            PlayAlong(context, SkillVfxKind.Weapon, null);
            PlayAlong(context, SkillVfxKind.Bullet, new[] { 45 });
        }
    }

    public class SkillCrossSlash : SkillBase
    {
        public override int Id => 2;
        public override float CastRange => 2f;
        public override float CD => 5f;
        public override int Store => 10;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Knife, 2);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = SkillBase.SkillContextConventions.BuildShotContext(this, entity, ProjectilePattern.Line,
                Utils.SpreadStyle.FanDests(entity.transform.position, Utils.TargetSelect.AimPos(entity,CastRange), 2, 30f));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.8f);
        protected override void OnCast(SkillContext context) => ShootAll(SkillBase.SkillContextConventions.GetCasterById(context), context, BuildAttack(SkillBase.SkillContextConventions.GetCasterById(context), 1.6f, 0.5f));
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 39, 41 });
    }

    public class SkillChargeSlash : SkillBase
    {
        public override int Id => 3;
        public override float CD => 5f;
        public override int Store => 10;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Knife, 3);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            if (caster != null) caster.SetMotion(new MotionDash(0.4f, 10f)); // 冲锋位移（速度积分，不瞬移）
        }
        public override void PlayVFX(SkillContext context) => PlayFollow(SkillVfxKind.Buff, 27, SkillBase.SkillContextConventions.GetCasterId(context)); // BF28
    }

    public class SkillWhirlSlash : SkillBase
    {
        public override int Id => 4;
        public override float CastRange => 2f;
        public override float CD => 8f;
        public override int Store => 5;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Knife, 4);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = SkillBase.SkillContextConventions.BuildShotContext(this, entity, ProjectilePattern.Line,
                Utils.SpreadStyle.CircleDests(entity.transform.position, 3f, 8));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R_And_L, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.5f);
        protected override void OnCast(SkillContext context) => ShootAll(SkillBase.SkillContextConventions.GetCasterById(context), context, BuildAttack(SkillBase.SkillContextConventions.GetCasterById(context), 1.8f, 0.5f));
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 40 });
    }

    public class SkillBreakEndureSlash : SkillBase
    {
        public override int Id => 5;
        public override float CastRange => 2f;
        public override float CD => 10f;
        public override int Store => 3;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Knife, 5);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = SkillBase.SkillContextConventions.BuildShotContext(this, entity, ProjectilePattern.Line, Utils.TargetSelect.AimPos(entity,CastRange));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.8f);
        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            ushort casterId = caster != null ? caster.id : (ushort)0;
            ShootAll(caster, context, BuildAttack(caster, 2.2f, 0.5f, breakEndure: true,
                addEffect: ApplyEffect(EffectType.Stun, 1, Config.buff_duration_control, negative: true, sourceId: casterId)));
        }

        // 命中目标身上的 BF21 麻痹黄由客户端 Buff 表驱动（Config.buff_vfx）
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 41 });
    }

    public class SkillFanSwordQi : SkillBase
    {
        public override int Id => 6;
        public override float CastRange => 2f;
        public override float CD => 3f;
        public override int Store => 12;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Knife, 6);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = SkillBase.SkillContextConventions.BuildShotContext(this, entity, ProjectilePattern.Line,
                Utils.SpreadStyle.FanDests(entity.transform.position, Utils.TargetSelect.AimPos(entity,CastRange), 3, 30f));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.8f);
        protected override void OnCast(SkillContext context) => ShootAll(SkillBase.SkillContextConventions.GetCasterById(context), context, BuildAttack(SkillBase.SkillContextConventions.GetCasterById(context), 1.3f, 0.45f));
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 39, 40, 41 });
    }

    public class SkillCaptureThrow : SkillBase
    {
        public override int Id => 7;
        public override float CastRange => 2f;
        public override float CD => 12f;
        public override int Store => 4;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Knife, 7);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = SkillBase.SkillContextConventions.BuildShotContext(this, entity, ProjectilePattern.Point, Utils.TargetSelect.AimPos(entity,CastRange));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Point(context, index, 1.5f);
        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            ushort casterId = caster != null ? caster.id : (ushort)0;
            StrikeSphere(caster, SkillBase.SkillContextConventions.GetShotDestination(context, 0), 1.5f, BuildAttack(caster, 1f, 1.5f,
                addEffect: ApplyEffect(EffectType.Root, 1, Config.buff_duration_control, negative: true, sourceId: casterId)));
        }
        public override void PlayVFX(SkillContext context)
        {
            PlayAlong(context, SkillVfxKind.Weapon, null);
            PlayAlong(context, SkillVfxKind.MagicCircle, new[] { 2 }); // MC3
        }
    }

    public class SkillShadowStrike : SkillBase
    {
        public override int Id => 8;
        public override float CD => 10f;
        public override int Store => 4;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Knife, 8);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            context.AddVectors(entity.transform.position, Utils.TargetSelect.AimPos(entity,CastRange));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            if (caster != null) caster.SetMotion(new MotionToPoint(context.vectors[1], 20f)); // 位移到目标点
        }
        public override void PlayVFX(SkillContext context)
        {
            PlayAt(SkillVfxKind.MagicCircle, 6, context.vectors[0], 1.2f); // MC7 起点
            PlayAt(SkillVfxKind.MagicCircle, 6, context.vectors[1], 1.2f); // MC7 终点
        }
    }

    public class SkillMountainCrash : SkillBase
    {
        public override int Id => 9;
        public override float CastRange => 2f;
        public override float CD => 6f;
        public override int Store => 10;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Knife, 9);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            context.AddVectors(entity.transform.position, Utils.TargetSelect.AimPos(entity,CastRange));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Jump_Mega, () => OnCast(context))) OnCast(context);
            return context;
        }
        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            StrikeSphere(caster, context.vectors[1], 1.2f, BuildAttack(caster, 2f, 1.2f));
        }
        public override void PlayVFX(SkillContext context)
            => PlayAt(SkillVfxKind.RangeMagic, 4, context.vectors[1], 1.2f); // RM5
    }

    public class SkillBladeDance : SkillBase
    {
        public override int Id => 10;
        public override float CastRange => 2f;
        public override float CD => 4f;
        public override int Store => 12;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Knife, 10);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            StrikeSphere(caster, HandPos(caster), 0.6f, BuildAttack(caster, 1.4f, 0.6f));
        }
        public override void PlayVFX(SkillContext context) => PlayFollow(SkillVfxKind.Bullet, 40, SkillBase.SkillContextConventions.GetCasterId(context));
    }
    #endregion

    #region 长枪 11~18
    public class SkillJavelinThrow : SkillBase
    {
        public override int Id => 11;
        public override float CastRange => 8f;
        public override float CD => 1.5f;
        public override int Store => 15;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Spear, 0);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = SkillBase.SkillContextConventions.BuildShotContext(this, entity, ProjectilePattern.Line, Utils.TargetSelect.AimPos(entity,CastRange));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 1f);
        protected override void OnCast(SkillContext context) => ShootAll(SkillBase.SkillContextConventions.GetCasterById(context), context, BuildAttack(SkillBase.SkillContextConventions.GetCasterById(context), 1.2f, 0.45f));
        public override void PlayVFX(SkillContext context)
        {
            PlayAlong(context, SkillVfxKind.Weapon, null);
            PlayAlong(context, SkillVfxKind.Bullet, new[] { 48 });
        }
    }

    public class SkillPierceSpear : SkillBase
    {
        public override int Id => 12;
        public override float CastRange => 8f;
        public override float CD => 4f;
        public override int Store => 10;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Spear, 1);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = SkillBase.SkillContextConventions.BuildShotContext(this, entity, ProjectilePattern.Line, Utils.TargetSelect.AimPos(entity,CastRange));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 1.2f);
        protected override void OnCast(SkillContext context) => ShootAll(SkillBase.SkillContextConventions.GetCasterById(context), context, BuildAttack(SkillBase.SkillContextConventions.GetCasterById(context), 1.6f, 0.5f));
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 51 });
    }

    public class SkillThunderSpear : SkillBase
    {
        public override int Id => 13;
        public override float CastRange => 8f;
        public override float CD => 10f;
        public override int Store => 3;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Spear, 2);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = SkillBase.SkillContextConventions.BuildShotContext(this, entity, ProjectilePattern.SkyFall, Utils.TargetSelect.AimPos(entity,CastRange));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => SkyFall(context, index, 1.2f);
        protected override void OnCast(SkillContext context) => ShootAll(SkillBase.SkillContextConventions.GetCasterById(context), context, BuildAttack(SkillBase.SkillContextConventions.GetCasterById(context), 1.9f, 0.8f));
        public override void PlayVFX(SkillContext context)
        {
            PlayAlong(context, SkillVfxKind.Weapon, null);
            PlayAlong(context, SkillVfxKind.RangeMagic, new[] { 4 }); // RM5 落点
        }
    }

    public class SkillBindNail : SkillBase
    {
        public override int Id => 14;
        public override float CastRange => 8f;
        public override float CD => 12f;
        public override int Store => 4;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Spear, 3);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = SkillBase.SkillContextConventions.BuildShotContext(this, entity, ProjectilePattern.Point, Utils.TargetSelect.AimPos(entity,CastRange));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Point(context, index, 1.5f);
        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            ushort casterId = caster != null ? caster.id : (ushort)0;
            StrikeSphere(caster, SkillBase.SkillContextConventions.GetShotDestination(context, 0), 1.5f, BuildAttack(caster, 1f, 1.5f,
                addEffect: ApplyEffect(EffectType.Root, 1, Config.buff_duration_control, negative: true, sourceId: casterId)));
        }
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.MagicCircle, new[] { 2 }); // MC3
    }

    public class SkillThrustDash : SkillBase
    {
        public override int Id => 15;
        public override float CD => 5f;
        public override int Store => 10;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Spear, 4);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            if (caster != null) caster.SetMotion(new MotionDash(0.3f, 12f)); // 突进位移
        }
        public override void PlayVFX(SkillContext context) => PlayFollow(SkillVfxKind.Buff, 27, SkillBase.SkillContextConventions.GetCasterId(context)); // BF28
    }

    public class SkillSweepPole : SkillBase
    {
        public override int Id => 16;
        public override float CastRange => 2f;
        public override float CD => 6f;
        public override int Store => 10;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Spear, 5);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            StrikeSphere(caster, HandPos(caster), 0.9f, BuildAttack(caster, 1.5f, 0.9f, knockback: 3f));
        }
        public override void PlayVFX(SkillContext context) { } // 无特效
    }

    public class SkillSpearWall : SkillBase
    {
        public override int Id => 17;
        public override float CD => 20f;
        public override int Store => 2;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Spear, 6);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = SkillBase.SkillContextConventions.BuildShotContext(this, entity, ProjectilePattern.Point, Utils.TargetSelect.AimPos(entity,CastRange));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Point(context, index, 2f);
        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            if (caster == null) return;
            // 屏障不做阻挡：改为落点范围内的友方护盾
            BattleManager.EntityContainer.GetAllInCamp(SkillBase.SkillContextConventions.GetShotDestination(context, 0), 3f, caster.camp, Utils.TargetSelect.TargetBuffer);
            for (int i = 0; i < Utils.TargetSelect.TargetBuffer.Count; i++)
            {
                GiveEffect(Utils.TargetSelect.TargetBuffer[i], EffectType.Shield, 1, Config.buff_duration_buff,
                    shieldValue: Config.buff_shield_value, sourceId: caster.id);
            }
        }
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.MagicCircle, new[] { 0 }); // MC1
    }

    public class SkillSpearRain : SkillBase
    {
        public override int Id => 18;
        public override float CastRange => 8f;
        public override float CD => 15f;
        public override int Store => 3;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Spear, 7);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = SkillBase.SkillContextConventions.BuildShotContext(this, entity, ProjectilePattern.SkyFall,
                Utils.SpreadStyle.FanDests(entity.transform.position, Utils.TargetSelect.AimPos(entity,CastRange), 5, 30f));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => SkyFall(context, index, 1.5f);
        protected override void OnCast(SkillContext context) => ShootAll(SkillBase.SkillContextConventions.GetCasterById(context), context, BuildAttack(SkillBase.SkillContextConventions.GetCasterById(context), 2.4f, 0.7f));
        public override void PlayVFX(SkillContext context)
        {
            PlayAlong(context, SkillVfxKind.Bullet, new[] { 47 });
            PlayAlong(context, SkillVfxKind.RangeMagic, new[] { 4 }); // RM5 落点
        }
    }
    #endregion

    #region 枪械 19~33
    public class SkillQuickShot : SkillBase
    {
        public override int Id => 19;
        public override float CastRange => 18f;
        public override float CD => 0.5f;
        public override int Store => 20;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 0);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = SkillBase.SkillContextConventions.BuildShotContext(this, entity, ProjectilePattern.Line, Utils.TargetSelect.AimPos(entity,CastRange));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.6f);
        protected override void OnCast(SkillContext context) => ShootAll(SkillBase.SkillContextConventions.GetCasterById(context), context, BuildAttack(SkillBase.SkillContextConventions.GetCasterById(context), 1f, 0.4f));
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 42 });
    }

    public class SkillFanShotgun : SkillBase
    {
        public override int Id => 20;
        public override float CastRange => 18f;
        public override float CD => 2f;
        public override int Store => 15;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 1);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = SkillBase.SkillContextConventions.BuildShotContext(this, entity, ProjectilePattern.Line,
                Utils.SpreadStyle.FanDests(entity.transform.position, Utils.TargetSelect.AimPos(entity,CastRange), 3, 25f));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.6f);
        protected override void OnCast(SkillContext context) => ShootAll(SkillBase.SkillContextConventions.GetCasterById(context), context, BuildAttack(SkillBase.SkillContextConventions.GetCasterById(context), 1.2f, 0.4f));
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 42, 43, 44 });
    }

    public class SkillArmorPiercer : SkillBase
    {
        public override int Id => 21;
        public override float CastRange => 18f;
        public override float CD => 2f;
        public override int Store => 12;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 2);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = SkillBase.SkillContextConventions.BuildShotContext(this, entity, ProjectilePattern.Line, Utils.TargetSelect.AimPos(entity,CastRange));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.7f);
        protected override void OnCast(SkillContext context) => ShootAll(SkillBase.SkillContextConventions.GetCasterById(context), context, BuildAttack(SkillBase.SkillContextConventions.GetCasterById(context), 1.5f, 0.4f));
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 45 });
    }

    public class SkillGrenade : SkillBase
    {
        public override int Id => 22;
        public override float CastRange => 18f;
        public override float CD => 3.5f;
        public override int Store => 10;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 3);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = SkillBase.SkillContextConventions.BuildShotContext(this, entity, ProjectilePattern.Bezier, Utils.TargetSelect.AimPos(entity,CastRange));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Arc(context, index, 1.2f);
        protected override void OnCast(SkillContext context) => ShootAll(SkillBase.SkillContextConventions.GetCasterById(context), context, BuildAttack(SkillBase.SkillContextConventions.GetCasterById(context), 2f, 1f));
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.RangeMagic, new[] { 4 }); // RM5
    }

    public class SkillIncendiary : SkillBase
    {
        // 策划案只规定「1s/跳、固定数值」，每跳伤害与持续时长待策划定稿
        private const float BurnDamagePerTick = 8f;
        private const float BurnDuration = 5f;

        public override int Id => 23;
        public override float CastRange => 18f;
        public override float CD => 4f;
        public override int Store => 12;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 4);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = SkillBase.SkillContextConventions.BuildShotContext(this, entity, ProjectilePattern.Line, Utils.TargetSelect.AimPos(entity,CastRange));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.7f);
        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            ShootAll(caster, context,
                BuildAttack(caster, 1.2f, 0.4f, addEffect: Burn(caster != null ? caster.id : (ushort)0)));
        }

        private static Action<EntityEffectController> Burn(ushort casterId) => effect => effect.AddEffect(
            EffectType.Burning, 1, BurnDuration, negative: true,
            payload: new EntityEffectController.EffectPayload { damage = BurnDamagePerTick, sourceId = casterId });
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 15 });
    }

    public class SkillFreezeRound : SkillBase
    {
        public override int Id => 24;
        public override float CastRange => 18f;
        public override float CD => 5f;
        public override int Store => 10;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 5);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = SkillBase.SkillContextConventions.BuildShotContext(this, entity, ProjectilePattern.Line, Utils.TargetSelect.AimPos(entity,CastRange));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.7f);
        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            ushort casterId = caster != null ? caster.id : (ushort)0;
            ShootAll(caster, context, BuildAttack(caster, 1f, 0.4f,
                addEffect: ApplyEffect(EffectType.Freeze, 1, Config.buff_duration_control, negative: true, sourceId: casterId)));
        }
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 18 });
    }

    public class SkillParalysisRound : SkillBase
    {
        public override int Id => 25;
        public override float CastRange => 18f;
        public override float CD => 5f;
        public override int Store => 10;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 6);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = SkillBase.SkillContextConventions.BuildShotContext(this, entity, ProjectilePattern.Line, Utils.TargetSelect.AimPos(entity,CastRange));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.7f);
        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            ushort casterId = caster != null ? caster.id : (ushort)0;
            ShootAll(caster, context, BuildAttack(caster, 1f, 0.4f,
                addEffect: ApplyEffect(EffectType.Stun, 1, Config.buff_duration_control, negative: true, sourceId: casterId)));
        }
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 5 });
    }

    public class SkillToxicRound : SkillBase
    {
        public override int Id => 26;
        public override float CastRange => 18f;
        public override float CD => 6f;
        public override int Store => 10;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 7);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = SkillBase.SkillContextConventions.BuildShotContext(this, entity, ProjectilePattern.Bezier, Utils.TargetSelect.AimPos(entity,CastRange));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Arc(context, index, 1.2f);
        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            ushort casterId = caster != null ? caster.id : (ushort)0;
            ShootAll(caster, context, BuildAttack(caster, 1f, 1f,
                addEffect: ApplyEffect(EffectType.Poison, 1, Config.buff_duration_debuff, negative: true,
                    damage: Config.buff_dot_damage, sourceId: casterId)));
        }
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.RangeMagic, new[] { 0 }); // RM1
    }

    public class SkillHeavyBreaker : SkillBase
    {
        public override int Id => 27;
        public override float CastRange => 18f;
        public override float CD => 4f;
        public override int Store => 12;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 8);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = SkillBase.SkillContextConventions.BuildShotContext(this, entity, ProjectilePattern.Line, Utils.TargetSelect.AimPos(entity,CastRange));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.7f);
        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            ushort casterId = caster != null ? caster.id : (ushort)0;
            ShootAll(caster, context, BuildAttack(caster, 1.6f, 0.45f,
                addEffect: ApplyEffect(EffectType.AnimSlowDown, 1, Config.buff_duration_debuff, negative: true, sourceId: casterId)));
        }
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 49 });
    }

    public class SkillInspireRound : SkillBase
    {
        public override int Id => 28;
        public override float CD => 8f;
        public override int Store => 10;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 9);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            var ally = Utils.TargetSelect.GetNearestAlly(entity);
            context.AddInts(ally != null ? ally.id : entity.id); // 目标（无友方时指向自己）
            // 无友方：正前方发射，避免终点=自身把泡泡冻结在施法者
            Vector3 dest = ally != null
                ? ally.transform.position
                : entity.transform.position + entity.transform.forward * Config.default_skill_auto_target_radius;
            context.AddVectors(GetWeaponFloatPosition(entity), dest);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.7f);
        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            ushort casterId = caster != null ? caster.id : (ushort)0;
            GiveEffect(caster, EffectType.AnimSpeedUp, 1, Config.buff_duration_buff, sourceId: casterId);
            for (int i = 0; i < SkillBase.SkillContextConventions.TargetCount(context); i++)
            {
                var ally = BattleManager.GetEntity(SkillBase.SkillContextConventions.GetTargetId(context, i));
                if (ally != null && ally.id != casterId)
                {
                    GiveEffect(ally, EffectType.AnimSpeedUp, 1, Config.buff_duration_buff, sourceId: casterId);
                }
            }
        }
        // 泡泡球仅为表现：服务器不做子弹命中判定
        public override void PlayVFX(SkillContext context)
        {
            PlayAlong(context, SkillVfxKind.Bullet, new[] { 57 }); // B57 蓝泡泡球
            PlayFollow(SkillVfxKind.Buff, 27, SkillBase.SkillContextConventions.GetCasterId(context));  // BF28 环绕风（自身）
        }
    }

    public class SkillImpactRound : SkillBase
    {
        public override int Id => 29;
        public override float CastRange => 18f;
        public override float CD => 8f;
        public override int Store => 10;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 10);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = SkillBase.SkillContextConventions.BuildShotContext(this, entity, ProjectilePattern.Bezier, Utils.TargetSelect.AimPos(entity,CastRange));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Arc(context, index, 1.1f);
        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            ShootAll(caster, context, BuildAttack(caster, 0.4f, 1.2f, knockback: 5f));
        }
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.RangeMagic, new[] { 4 }); // RM5
    }

    public class SkillBlinkDash : SkillBase
    {
        public override int Id => 30;
        public override float CD => 12f;
        public override int Store => 4;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 11);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            context.AddVectors(Utils.TargetSelect.AimPos(entity,CastRange));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            if (caster != null) caster.SetMotion(new MotionToPoint(context.vectors[0], 28f)); // 长距闪现
        }
        public override void PlayVFX(SkillContext context) => PlayFollow(SkillVfxKind.Buff, 27, SkillBase.SkillContextConventions.GetCasterId(context)); // BF28
    }

    public class SkillSnipe : SkillBase
    {
        public override int Id => 31;
        public override float CastRange => 18f;
        public override float CD => 10f;
        public override int Store => 5;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 12);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = SkillBase.SkillContextConventions.BuildShotContext(this, entity, ProjectilePattern.Line, Utils.TargetSelect.AimPos(entity,CastRange));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.5f);
        protected override void OnCast(SkillContext context) => ShootAll(SkillBase.SkillContextConventions.GetCasterById(context), context, BuildAttack(SkillBase.SkillContextConventions.GetCasterById(context), 3f, 0.35f));
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 45 });
    }

    public class SkillBulletSpray : SkillBase
    {
        public override int Id => 32;
        public override float CastRange => 18f;
        public override float CD => 15f;
        public override int Store => 3;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 13);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = SkillBase.SkillContextConventions.BuildShotContext(this, entity, ProjectilePattern.Line,
                Utils.SpreadStyle.FanDests(entity.transform.position, Utils.TargetSelect.AimPos(entity,CastRange), 8, 30f));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.6f);
        protected override void OnCast(SkillContext context) => ShootAll(SkillBase.SkillContextConventions.GetCasterById(context), context, BuildAttack(SkillBase.SkillContextConventions.GetCasterById(context), 1f, 0.4f));
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 44 });
    }

    public class SkillAirstrikeMark : SkillBase
    {
        public override int Id => 33;
        public override float CastRange => 18f;
        public override float CD => 20f;
        public override int Store => 1;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 14);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            context.AddVectors(entity.transform.position, Utils.TargetSelect.AimPos(entity,CastRange));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            // 不延时：动画攻击帧直接对标记落点结算轰炸
            StrikeSphere(caster, context.vectors[1], Config.airstrike_radius,
                BuildAttack(caster, 2f, Config.airstrike_radius, useMagic: true));
        }
        public override void PlayVFX(SkillContext context)
        {
            PlayAt(SkillVfxKind.MagicCircle, 7, context.vectors[1], 2f);  // MC8 标记
            PlayAt(SkillVfxKind.RangeMagic, 5, context.vectors[1], 1.5f); // RM6 光柱
        }
    }
    #endregion

    #region 魔法球 34~49
    public class SkillMagicBolt : SkillBase
    {
        public override int Id => 34;
        public override float CastRange => 15f;
        public override float CD => 0.6f;
        public override int Store => 20;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 0);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = SkillBase.SkillContextConventions.BuildShotContext(this, entity, ProjectilePattern.Line, Utils.TargetSelect.AimPos(entity,CastRange));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.7f);
        protected override void OnCast(SkillContext context)
            => ShootAll(SkillBase.SkillContextConventions.GetCasterById(context), context, BuildAttack(SkillBase.SkillContextConventions.GetCasterById(context), 1f, 0.4f, useMagic: true));
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 0 });
    }

    public class SkillFireball : SkillBase
    {
        public override int Id => 35;
        public override float CastRange => 15f;
        public override float CD => 3f;
        public override int Store => 12;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 1);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = SkillBase.SkillContextConventions.BuildShotContext(this, entity, ProjectilePattern.Bezier, Utils.TargetSelect.AimPos(entity,CastRange));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Arc(context, index, 1f);
        protected override void OnCast(SkillContext context)
            => ShootAll(SkillBase.SkillContextConventions.GetCasterById(context), context, BuildAttack(SkillBase.SkillContextConventions.GetCasterById(context), 1.5f, 1f, useMagic: true));
        public override void PlayVFX(SkillContext context)
        {
            PlayAlong(context, SkillVfxKind.Bullet, new[] { 15 });
            PlayAlong(context, SkillVfxKind.RangeMagic, new[] { 4 }); // RM5
        }
    }

    public class SkillIceShard : SkillBase
    {
        public override int Id => 36;
        public override float CastRange => 15f;
        public override float CD => 3f;
        public override int Store => 12;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 2);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = SkillBase.SkillContextConventions.BuildShotContext(this, entity, ProjectilePattern.Line, Utils.TargetSelect.AimPos(entity,CastRange));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.8f);
        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            ushort casterId = caster != null ? caster.id : (ushort)0;
            ShootAll(caster, context, BuildAttack(caster, 1.2f, 0.4f, useMagic: true,
                addEffect: ApplyEffect(EffectType.Freeze, 1, Config.buff_duration_control, negative: true, sourceId: casterId)));
        }
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 18 });
    }

    public class SkillWindBlade : SkillBase
    {
        public override int Id => 37;
        public override float CastRange => 15f;
        public override float CD => 2.5f;
        public override int Store => 12;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 3);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = SkillBase.SkillContextConventions.BuildShotContext(this, entity, ProjectilePattern.Line, Utils.TargetSelect.AimPos(entity,CastRange));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.8f);
        protected override void OnCast(SkillContext context)
            => ShootAll(SkillBase.SkillContextConventions.GetCasterById(context), context, BuildAttack(SkillBase.SkillContextConventions.GetCasterById(context), 1.3f, 0.45f, useMagic: true));
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 46 });
    }

    public class SkillPoisonOrb : SkillBase
    {
        public override int Id => 38;
        public override float CastRange => 15f;
        public override float CD => 4f;
        public override int Store => 10;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 4);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = SkillBase.SkillContextConventions.BuildShotContext(this, entity, ProjectilePattern.Line, Utils.TargetSelect.AimPos(entity,CastRange));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.8f);
        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            ushort casterId = caster != null ? caster.id : (ushort)0;
            ShootAll(caster, context, BuildAttack(caster, 1f, 0.4f, useMagic: true,
                addEffect: ApplyEffect(EffectType.Poison, 1, Config.buff_duration_debuff, negative: true,
                    damage: Config.buff_dot_damage, sourceId: casterId)));
        }
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 10 });
    }

    public class SkillRockfall : SkillBase
    {
        public override int Id => 39;
        public override float CastRange => 15f;
        public override float CD => 5f;
        public override int Store => 10;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 5);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = SkillBase.SkillContextConventions.BuildShotContext(this, entity, ProjectilePattern.SkyFall, Utils.TargetSelect.AimPos(entity,CastRange));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => SkyFall(context, index, 1.1f);
        protected override void OnCast(SkillContext context)
            => ShootAll(SkillBase.SkillContextConventions.GetCasterById(context), context, BuildAttack(SkillBase.SkillContextConventions.GetCasterById(context), 1.6f, 0.9f, useMagic: true));
        public override void PlayVFX(SkillContext context)
        {
            PlayAlong(context, SkillVfxKind.Bullet, new[] { 25 });
            PlayAlong(context, SkillVfxKind.RangeMagic, new[] { 4 }); // RM5
        }
    }

    public class SkillThunderOrb : SkillBase
    {
        public override int Id => 40;
        public override float CastRange => 15f;
        public override float CD => 5f;
        public override int Store => 10;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 6);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            // 链式跳：记录「发射者 → 第一目标 → 第二目标」三个 id
            var first = Utils.TargetSelect.GetNearestEnemy(entity, CastRange);
            var second = first != null
                ? BattleManager.EntityContainer.GetNearestEnemy(first, Config.chain_jump_radius)
                : null;
            context.AddInts(first != null ? first.id : entity.id);
            context.AddInts(second != null ? second.id : (first != null ? first.id : entity.id));
            // 无敌人：存正前方直射终点，避免回退自身把弹道冻结在施法者
            if (first == null) context.AddVectors(GetWeaponFloatPosition(entity), Utils.TargetSelect.AimPos(entity,CastRange));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }

        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            // 首目标 id == 施法者 → 无敌人（见 SkillLogic）：改走正前方直射
            if (SkillBase.SkillContextConventions.GetTargetId(context, 0) == SkillBase.SkillContextConventions.GetCasterId(context))
                return Line(context, index, Config.chain_jump_duration);
            var trajectory = new ChainTrajectory(SkillBase.SkillContextConventions.GetCasterId(context), SkillBase.SkillContextConventions.GetTargetId(context, 0), SkillBase.SkillContextConventions.GetTargetId(context, 1));
            trajectory.Duration = Config.chain_jump_duration;
            return trajectory;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            var attack = BuildAttack(caster, 1.4f, 0.45f, useMagic: true);
            if (Tool.BattleManager != null) Tool.BattleManager.ShootBullet(caster, attack, CreateTrajectory(context, 0));
        }
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 3 });
    }

    public class SkillLightShield : SkillBase
    {
        public override int Id => 41;
        public override float CD => 10f;
        public override int Store => 8;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 7);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            if (caster == null) return;
            GiveEffect(caster, EffectType.Shield, 1, Config.buff_duration_buff,
                shieldValue: Config.buff_shield_value, sourceId: caster.id);
            var ally = Utils.TargetSelect.GetNearestAlly(caster);
            if (ally != null)
            {
                GiveEffect(ally, EffectType.Shield, 1, Config.buff_duration_buff,
                    shieldValue: Config.buff_shield_value, sourceId: caster.id);
            }
        }
        public override void PlayVFX(SkillContext context) => PlayFollow(SkillVfxKind.Shield, 5, SkillBase.SkillContextConventions.GetCasterId(context)); // S6
    }

    public class SkillPurgeWave : SkillBase
    {
        public override int Id => 42;
        public override float CD => 10f;
        public override int Store => 8;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 8);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            context.AddVectors(entity.transform.position);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            if (caster != null && caster.effectController != null) caster.effectController.RemoveAllNegative(); // 净化：移除自身全部负面 Buff
        }
        public override void PlayVFX(SkillContext context)
            => PlayAt(SkillVfxKind.MagicCircle, 1, context.vectors[0], 1.2f); // MC2
    }

    public class SkillInspireCircle : SkillBase
    {
        public override int Id => 43;
        public override float CD => 10f;
        public override int Store => 8;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 9);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = SkillBase.SkillContextConventions.BuildShotContext(this, entity, ProjectilePattern.Point, Utils.TargetSelect.AimPos(entity,CastRange));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Point(context, index, 2f);
        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            if (caster == null) return;
            BattleManager.EntityContainer.GetAllInCamp(SkillBase.SkillContextConventions.GetShotDestination(context, 0), 3f, caster.camp, Utils.TargetSelect.TargetBuffer);
            for (int i = 0; i < Utils.TargetSelect.TargetBuffer.Count; i++)
            {
                GiveEffect(Utils.TargetSelect.TargetBuffer[i], EffectType.AttrStrength, 1, Config.buff_duration_buff,
                    value: Config.buff_attr_value, sourceId: caster.id);
            }
        }
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.MagicCircle, new[] { 9 }); // MC10
    }

    public class SkillFrostNova : SkillBase
    {
        public override int Id => 44;
        public override float CastRange => 3f;
        public override float CD => 10f;
        public override int Store => 6;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 10);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = SkillBase.SkillContextConventions.BuildShotContext(this, entity, ProjectilePattern.Line,
                Utils.SpreadStyle.CircleDests(entity.transform.position, 3f, 8));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.7f);
        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            ushort casterId = caster != null ? caster.id : (ushort)0;
            ShootAll(caster, context, BuildAttack(caster, 1.2f, 0.5f, useMagic: true,
                addEffect: ApplyEffect(EffectType.Freeze, 1, Config.buff_duration_control, negative: true, sourceId: casterId)));
        }
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 20 });
    }

    public class SkillThunderFall : SkillBase
    {
        public override int Id => 45;
        public override float CastRange => 15f;
        public override float CD => 10f;
        public override int Store => 5;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 11);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = SkillBase.SkillContextConventions.BuildShotContext(this, entity, ProjectilePattern.SkyFall, Utils.TargetSelect.AimPos(entity,CastRange));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => SkyFall(context, index, 1.2f);
        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            ushort casterId = caster != null ? caster.id : (ushort)0;
            ShootAll(caster, context, BuildAttack(caster, 2f, 1f, useMagic: true,
                addEffect: ApplyEffect(EffectType.Stun, 1, Config.buff_duration_control, negative: true, sourceId: casterId)));
        }
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 3 });
    }

    public class SkillBlackHoleBomb : SkillBase
    {
        public override int Id => 46;
        public override float CastRange => 15f;
        public override float CD => 5f;
        public override int Store => 8;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 12);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = SkillBase.SkillContextConventions.BuildShotContext(this, entity, ProjectilePattern.Bezier, Utils.TargetSelect.AimPos(entity,CastRange));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Arc(context, index, 1.1f);
        protected override void OnCast(SkillContext context)
            => ShootAll(SkillBase.SkillContextConventions.GetCasterById(context), context, BuildAttack(SkillBase.SkillContextConventions.GetCasterById(context), 2f, 1.5f, useMagic: true));
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.RangeMagic, new[] { 1 }); // RM2
    }

    public class SkillStarfall : SkillBase
    {
        public override int Id => 47;
        public override float CastRange => 15f;
        public override float CD => 20f;
        public override int Store => 3;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 13);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = SkillBase.SkillContextConventions.BuildShotContext(this, entity, ProjectilePattern.SkyFall,
                Utils.SpreadStyle.FanDests(entity.transform.position, Utils.TargetSelect.AimPos(entity,CastRange), 5, 30f));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => SkyFall(context, index, 1.6f);
        protected override void OnCast(SkillContext context)
            => ShootAll(SkillBase.SkillContextConventions.GetCasterById(context), context, BuildAttack(SkillBase.SkillContextConventions.GetCasterById(context), 2.2f, 0.9f, useMagic: true));
        public override void PlayVFX(SkillContext context)
        {
            PlayAlong(context, SkillVfxKind.Bullet, new[] { 31 });
            PlayAlong(context, SkillVfxKind.RangeMagic, new[] { 4 }); // RM5
        }
    }

    public class SkillVoidGrasp : SkillBase
    {
        public override int Id => 48;
        public override float CastRange => 15f;
        public override float CD => 15f;
        public override int Store => 4;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 14);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = SkillBase.SkillContextConventions.BuildShotContext(this, entity, ProjectilePattern.Line, Utils.TargetSelect.AimPos(entity,CastRange));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.8f);
        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            Vector3 casterPos = caster != null ? caster.transform.position : Vector3.zero;
            ShootAll(caster, context, BuildAttack(caster, 1.2f, 0.6f, useMagic: true,
                onHit: PullTo(casterPos)));
        }
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 1 });
    }

    public class SkillArcaneBarrier : SkillBase
    {
        public override int Id => 49;
        public override float CD => 20f;
        public override int Store => 2;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 15);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = SkillBase.SkillContextConventions.BuildShotContext(this, entity, ProjectilePattern.Point, Utils.TargetSelect.AimPos(entity,CastRange));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Point(context, index, 2f);
        protected override void OnCast(SkillContext context)
        {
            var caster = SkillBase.SkillContextConventions.GetCasterById(context);
            if (caster == null) return;
            // 屏障不做阻挡：改为落点范围内的友方护盾
            BattleManager.EntityContainer.GetAllInCamp(SkillBase.SkillContextConventions.GetShotDestination(context, 0), 3f, caster.camp, Utils.TargetSelect.TargetBuffer);
            for (int i = 0; i < Utils.TargetSelect.TargetBuffer.Count; i++)
            {
                GiveEffect(Utils.TargetSelect.TargetBuffer[i], EffectType.Shield, 1, Config.buff_duration_buff,
                    shieldValue: Config.buff_shield_value, sourceId: caster.id);
            }
        }
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.MagicCircle, new[] { 6 }); // MC7
    }
    #endregion
}
