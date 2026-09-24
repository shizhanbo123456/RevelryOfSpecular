using System;
using Ros.Transport;
using UnityEngine;

namespace Ros.Skill
{
    /// <summary>
    /// 水晶掉落技能池（id 0~49）。拥有者：摧毁水晶/蘑菇可获得的武器技能，攻防双方通用。
    /// 数值与特效下标依据策划案 21.1~21.4（B* = BulletVFX 下标；S*/RM*/MC*/BF* = 编号 − 1）。
    /// 倍率 / 判定半径 / 弹道时长策划案未给出，暂用占位初值（集中在 Config 或各技能常量）。
    /// </summary>
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
    /// <summary>疾风斩（id 0）：单发直线剑气 · B39。</summary>
    public class SkillGaleSlash : SkillBase
    {
        public override int Id => 0;
        public override float CastRange => 2f;
        public override float CD => 1f;
        public override int Store => 15;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.Knife, 0);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.Line, AimPos(entity));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.7f);
        protected override void OnCast(SkillContext context) => ShootAll(Caster(context), context, BuildAttack(Caster(context), 1f, 0.45f));
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 39 });
    }

    /// <summary>飞刀投掷（id 1）：刀模型作弹体飞行 + B45 拖尾。</summary>
    public class SkillFlyingKnife : SkillBase
    {
        public override int Id => 1;
        public override float CastRange => 2f;
        public override float CD => 2f;
        public override int Store => 12;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.Knife, 1);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.Line, AimPos(entity));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.9f);
        protected override void OnCast(SkillContext context) => ShootAll(Caster(context), context, BuildAttack(Caster(context), 1.2f, 0.45f));
        public override void PlayVFX(SkillContext context)
        {
            PlayAlong(context, SkillVfxKind.Weapon, null);
            PlayAlong(context, SkillVfxKind.Bullet, new[] { 45 });
        }
    }

    /// <summary>十字斩（id 2）：两道交叉剑气 · B39 + B41。</summary>
    public class SkillCrossSlash : SkillBase
    {
        public override int Id => 2;
        public override float CastRange => 2f;
        public override float CD => 5f;
        public override int Store => 10;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.Knife, 2);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.Line,
                FanDests(entity.transform.position, AimPos(entity), 2, 30f));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.8f);
        protected override void OnCast(SkillContext context) => ShootAll(Caster(context), context, BuildAttack(Caster(context), 1.6f, 0.5f));
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 39, 41 });
    }

    /// <summary>冲锋斩（id 3）：冲锋位移 + 输出 · BF28 环绕风。</summary>
    public class SkillChargeSlash : SkillBase
    {
        public override int Id => 3;
        public override float CD => 5f;
        public override int Store => 10;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.Knife, 3);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        protected override void OnCast(SkillContext context)
        {
            Caster(context)?.SetMotion(new MotionDash(0.4f, 10f)); // 冲锋位移（速度积分，不瞬移）
        }
        public override void PlayVFX(SkillContext context) => PlayFollow(SkillVfxKind.Buff, 27, CasterId(context)); // BF28
    }

    /// <summary>旋风斩（id 4）：环形八道剑气 · B40。</summary>
    public class SkillWhirlSlash : SkillBase
    {
        public override int Id => 4;
        public override float CastRange => 2f;
        public override float CD => 8f;
        public override int Store => 5;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.Knife, 4);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.Line,
                CircleDests(entity.transform.position, 3f, 8));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R_And_L, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.5f);
        protected override void OnCast(SkillContext context) => ShootAll(Caster(context), context, BuildAttack(Caster(context), 1.8f, 0.5f));
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 40 });
    }

    /// <summary>破霸重斩（id 5）：破霸体直线剑气 · B41。</summary>
    public class SkillBreakEndureSlash : SkillBase
    {
        public override int Id => 5;
        public override float CastRange => 2f;
        public override float CD => 10f;
        public override int Store => 3;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.Knife, 5);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.Line, AimPos(entity));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.8f);
        protected override void OnCast(SkillContext context)
        {
            var caster = Caster(context);
            ushort casterId = caster != null ? caster.id : (ushort)0;
            ShootAll(caster, context, BuildAttack(caster, 2.2f, 0.5f, breakEndure: true,
                addEffect: ApplyEffect(EffectType.Stun, 1, Config.buff_duration_control, negative: true, sourceId: casterId)));
        }

        // 命中目标身上的 BF21 麻痹黄由客户端 Buff 表驱动（Config.buff_vfx）
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 41 });
    }

    /// <summary>剑气纵横（id 6）：三道扇形剑气 · B39/B40/B41。</summary>
    public class SkillFanSwordQi : SkillBase
    {
        public override int Id => 6;
        public override float CastRange => 2f;
        public override float CD => 3f;
        public override int Store => 12;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.Knife, 6);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.Line,
                FanDests(entity.transform.position, AimPos(entity), 3, 30f));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.8f);
        protected override void OnCast(SkillContext context) => ShootAll(Caster(context), context, BuildAttack(Caster(context), 1.3f, 0.45f));
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 39, 40, 41 });
    }

    /// <summary>捕获投掷（id 7）：刀模型 + 落点禁锢法阵 MC3。</summary>
    public class SkillCaptureThrow : SkillBase
    {
        public override int Id => 7;
        public override float CastRange => 2f;
        public override float CD => 12f;
        public override int Store => 4;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.Knife, 7);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.Point, AimPos(entity));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Point(context, index, 1.5f);
        protected override void OnCast(SkillContext context)
        {
            var caster = Caster(context);
            ushort casterId = caster != null ? caster.id : (ushort)0;
            StrikeSphere(caster, Dest(context, 0), 1.5f, BuildAttack(caster, 1f, 1.5f,
                addEffect: ApplyEffect(EffectType.Root, 1, Config.buff_duration_control, negative: true, sourceId: casterId)));
        }
        public override void PlayVFX(SkillContext context)
        {
            PlayAlong(context, SkillVfxKind.Weapon, null);
            PlayAlong(context, SkillVfxKind.MagicCircle, new[] { 2 }); // MC3
        }
    }

    /// <summary>影袭（id 8）：起止法阵 · MC7。</summary>
    public class SkillShadowStrike : SkillBase
    {
        public override int Id => 8;
        public override float CD => 10f;
        public override int Store => 4;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.Knife, 8);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            context.AddVectors(entity.transform.position, AimPos(entity));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        protected override void OnCast(SkillContext context)
        {
            Caster(context)?.SetMotion(new MotionToPoint(context.vectors[1], 20f)); // 位移到目标点
        }
        public override void PlayVFX(SkillContext context)
        {
            PlayAt(SkillVfxKind.MagicCircle, 6, context.vectors[0], 1.2f); // MC7 起点
            PlayAt(SkillVfxKind.MagicCircle, 6, context.vectors[1], 1.2f); // MC7 终点
        }
    }

    /// <summary>崩山击（id 9）：跃起砸地，落点小型爆炸 RM5。</summary>
    public class SkillMountainCrash : SkillBase
    {
        public override int Id => 9;
        public override float CastRange => 2f;
        public override float CD => 6f;
        public override int Store => 10;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.Knife, 9);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            context.AddVectors(entity.transform.position, AimPos(entity));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Jump_Mega, () => OnCast(context))) OnCast(context);
            return context;
        }
        protected override void OnCast(SkillContext context)
        {
            var caster = Caster(context);
            StrikeSphere(caster, context.vectors[1], 1.2f, BuildAttack(caster, 2f, 1.2f));
        }
        public override void PlayVFX(SkillContext context)
            => PlayAt(SkillVfxKind.RangeMagic, 4, context.vectors[1], 1.2f); // RM5
    }

    /// <summary>刃舞连斩（id 10）：近战三段球判定 · B40。</summary>
    public class SkillBladeDance : SkillBase
    {
        public override int Id => 10;
        public override float CastRange => 2f;
        public override float CD => 4f;
        public override int Store => 12;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.Knife, 10);
        public override WeaponRef HoldWeapon => new WeaponRef(WeaponCategory.Knife, 10); // 近战：释放期间拿到手上
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        protected override void OnCast(SkillContext context)
        {
            var caster = Caster(context);
            StrikeSphere(caster, HandPos(caster), 0.6f, BuildAttack(caster, 1.4f, 0.6f));
        }
        public override void PlayVFX(SkillContext context) => PlayFollow(SkillVfxKind.Bullet, 40, CasterId(context));
    }
    #endregion

    #region 长枪 11~18
    /// <summary>投枪（id 11）：枪模型作弹体 + B48 拖尾。</summary>
    public class SkillJavelinThrow : SkillBase
    {
        public override int Id => 11;
        public override float CastRange => 8f;
        public override float CD => 1.5f;
        public override int Store => 15;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.Spear, 0);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.Line, AimPos(entity));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 1f);
        protected override void OnCast(SkillContext context) => ShootAll(Caster(context), context, BuildAttack(Caster(context), 1.2f, 0.45f));
        public override void PlayVFX(SkillContext context)
        {
            PlayAlong(context, SkillVfxKind.Weapon, null);
            PlayAlong(context, SkillVfxKind.Bullet, new[] { 48 });
        }
    }

    /// <summary>贯穿之枪（id 12）：细长直刺穿透 · B51。</summary>
    public class SkillPierceSpear : SkillBase
    {
        public override int Id => 12;
        public override float CastRange => 8f;
        public override float CD => 4f;
        public override int Store => 10;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.Spear, 1);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.Line, AimPos(entity));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 1.2f);
        protected override void OnCast(SkillContext context) => ShootAll(Caster(context), context, BuildAttack(Caster(context), 1.6f, 0.5f));
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 51 });
    }

    /// <summary>落雷枪（id 13）：枪模型升空天降 + 落点小型爆炸 RM5。</summary>
    public class SkillThunderSpear : SkillBase
    {
        public override int Id => 13;
        public override float CastRange => 8f;
        public override float CD => 10f;
        public override int Store => 3;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.Spear, 2);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.SkyFall, AimPos(entity));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => SkyFall(context, index, 1.2f);
        protected override void OnCast(SkillContext context) => ShootAll(Caster(context), context, BuildAttack(Caster(context), 1.9f, 0.8f));
        public override void PlayVFX(SkillContext context)
        {
            PlayAlong(context, SkillVfxKind.Weapon, null);
            PlayAlong(context, SkillVfxKind.RangeMagic, new[] { 4 }); // RM5 落点
        }
    }

    /// <summary>束缚钉（id 14）：落点禁锢法阵 MC3。</summary>
    public class SkillBindNail : SkillBase
    {
        public override int Id => 14;
        public override float CastRange => 8f;
        public override float CD => 12f;
        public override int Store => 4;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.Spear, 3);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.Point, AimPos(entity));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Point(context, index, 1.5f);
        protected override void OnCast(SkillContext context)
        {
            var caster = Caster(context);
            ushort casterId = caster != null ? caster.id : (ushort)0;
            StrikeSphere(caster, Dest(context, 0), 1.5f, BuildAttack(caster, 1f, 1.5f,
                addEffect: ApplyEffect(EffectType.Root, 1, Config.buff_duration_control, negative: true, sourceId: casterId)));
        }
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.MagicCircle, new[] { 2 }); // MC3
    }

    /// <summary>突进刺（id 15）：突进位移 + 输出 · BF28 环绕风。</summary>
    public class SkillThrustDash : SkillBase
    {
        public override int Id => 15;
        public override float CD => 5f;
        public override int Store => 10;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.Spear, 4);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        protected override void OnCast(SkillContext context)
        {
            Caster(context)?.SetMotion(new MotionDash(0.3f, 12f)); // 突进位移
        }
        public override void PlayVFX(SkillContext context) => PlayFollow(SkillVfxKind.Buff, 27, CasterId(context)); // BF28
    }

    /// <summary>横扫千军（id 16）：大范围近战球判定 + 纯物理击退（清单约定无特效）。</summary>
    public class SkillSweepPole : SkillBase
    {
        public override int Id => 16;
        public override float CastRange => 2f;
        public override float CD => 6f;
        public override int Store => 10;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.Spear, 5);
        public override WeaponRef HoldWeapon => new WeaponRef(WeaponCategory.Spear, 5);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        protected override void OnCast(SkillContext context)
        {
            var caster = Caster(context);
            StrikeSphere(caster, HandPos(caster), 0.9f, BuildAttack(caster, 1.5f, 0.9f, knockback: 3f));
        }
        public override void PlayVFX(SkillContext context) { } // 无特效
    }

    /// <summary>枪阵屏障（id 17）：落点力场 · MC1。</summary>
    public class SkillSpearWall : SkillBase
    {
        public override int Id => 17;
        public override float CD => 20f;
        public override int Store => 2;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.Spear, 6);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.Point, AimPos(entity));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Point(context, index, 2f);
        protected override void OnCast(SkillContext context)
        {
            var caster = Caster(context);
            if (caster == null) return;
            // 屏障不做阻挡：改为落点范围内的友方护盾
            BattleManager.EntityContainer.GetAllInCamp(Dest(context, 0), 3f, caster.camp, TargetBuffer);
            for (int i = 0; i < TargetBuffer.Count; i++)
            {
                GiveEffect(TargetBuffer[i], EffectType.Shield, 1, Config.buff_duration_buff,
                    shieldValue: Config.buff_shield_value, sourceId: caster.id);
            }
        }
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.MagicCircle, new[] { 0 }); // MC1
    }

    /// <summary>枪雨（id 18）：五发升空天降 + 落点小型爆炸 RM5。</summary>
    public class SkillSpearRain : SkillBase
    {
        public override int Id => 18;
        public override float CastRange => 8f;
        public override float CD => 15f;
        public override int Store => 3;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.Spear, 7);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.SkyFall,
                FanDests(entity.transform.position, AimPos(entity), 5, 30f));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => SkyFall(context, index, 1.5f);
        protected override void OnCast(SkillContext context) => ShootAll(Caster(context), context, BuildAttack(Caster(context), 2.4f, 0.7f));
        public override void PlayVFX(SkillContext context)
        {
            PlayAlong(context, SkillVfxKind.Bullet, new[] { 47 });
            PlayAlong(context, SkillVfxKind.RangeMagic, new[] { 4 }); // RM5 落点
        }
    }
    #endregion

    #region 枪械 19~33
    /// <summary>快速射击（id 19）：单发直线 · B42。</summary>
    public class SkillQuickShot : SkillBase
    {
        public override int Id => 19;
        public override float CastRange => 18f;
        public override float CD => 0.5f;
        public override int Store => 20;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.Gun, 0);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.Line, AimPos(entity));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.6f);
        protected override void OnCast(SkillContext context) => ShootAll(Caster(context), context, BuildAttack(Caster(context), 1f, 0.4f));
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 42 });
    }

    /// <summary>扇形散射（id 20）：三发扇形 · B42/B43/B44。</summary>
    public class SkillFanShotgun : SkillBase
    {
        public override int Id => 20;
        public override float CastRange => 18f;
        public override float CD => 2f;
        public override int Store => 15;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.Gun, 1);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.Line,
                FanDests(entity.transform.position, AimPos(entity), 3, 25f));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.6f);
        protected override void OnCast(SkillContext context) => ShootAll(Caster(context), context, BuildAttack(Caster(context), 1.2f, 0.4f));
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 42, 43, 44 });
    }

    /// <summary>穿甲弹（id 21）：直穿 · B45。</summary>
    public class SkillArmorPiercer : SkillBase
    {
        public override int Id => 21;
        public override float CastRange => 18f;
        public override float CD => 2f;
        public override int Store => 12;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.Gun, 2);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.Line, AimPos(entity));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.7f);
        protected override void OnCast(SkillContext context) => ShootAll(Caster(context), context, BuildAttack(Caster(context), 1.5f, 0.4f));
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 45 });
    }

    /// <summary>榴弹（id 22）：抛物线 + 落点小型爆炸 RM5。</summary>
    public class SkillGrenade : SkillBase
    {
        public override int Id => 22;
        public override float CastRange => 18f;
        public override float CD => 3.5f;
        public override int Store => 10;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.Gun, 3);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.Bezier, AimPos(entity));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Arc(context, index, 1.2f);
        protected override void OnCast(SkillContext context) => ShootAll(Caster(context), context, BuildAttack(Caster(context), 2f, 1f));
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.RangeMagic, new[] { 4 }); // RM5
    }

    /// <summary>燃烧弹（id 23）：B15 + 命中 DoT BF12。</summary>
    public class SkillIncendiary : SkillBase
    {
        // 策划案只规定「1s/跳、固定数值」，每跳伤害与持续时长待策划定稿
        private const float BurnDamagePerTick = 8f;
        private const float BurnDuration = 5f;

        public override int Id => 23;
        public override float CastRange => 18f;
        public override float CD => 4f;
        public override int Store => 12;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.Gun, 4);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.Line, AimPos(entity));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.7f);
        protected override void OnCast(SkillContext context)
        {
            var caster = Caster(context);
            ShootAll(caster, context,
                BuildAttack(caster, 1.2f, 0.4f, addEffect: Burn(caster != null ? caster.id : (ushort)0)));
        }

        /// <summary>命中给目标挂燃烧（负面 DoT，1s 一跳）。</summary>
        private static Action<EntityEffectController> Burn(ushort casterId) => effect => effect.AddEffect(
            EffectType.Burning, 1, BurnDuration, negative: true,
            payload: new EntityEffectController.EffectPayload { damage = BurnDamagePerTick, sourceId = casterId });
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 15 });
    }

    /// <summary>冻结弹（id 24）：B18 + 命中冻结 BF13。</summary>
    public class SkillFreezeRound : SkillBase
    {
        public override int Id => 24;
        public override float CastRange => 18f;
        public override float CD => 5f;
        public override int Store => 10;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.Gun, 5);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.Line, AimPos(entity));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.7f);
        protected override void OnCast(SkillContext context)
        {
            var caster = Caster(context);
            ushort casterId = caster != null ? caster.id : (ushort)0;
            ShootAll(caster, context, BuildAttack(caster, 1f, 0.4f,
                addEffect: ApplyEffect(EffectType.Freeze, 1, Config.buff_duration_control, negative: true, sourceId: casterId)));
        }
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 18 });
    }

    /// <summary>麻痹弹（id 25）：B5 + 命中麻痹 BF21。</summary>
    public class SkillParalysisRound : SkillBase
    {
        public override int Id => 25;
        public override float CastRange => 18f;
        public override float CD => 5f;
        public override int Store => 10;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.Gun, 6);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.Line, AimPos(entity));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.7f);
        protected override void OnCast(SkillContext context)
        {
            var caster = Caster(context);
            ushort casterId = caster != null ? caster.id : (ushort)0;
            ShootAll(caster, context, BuildAttack(caster, 1f, 0.4f,
                addEffect: ApplyEffect(EffectType.Stun, 1, Config.buff_duration_control, negative: true, sourceId: casterId)));
        }
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 5 });
    }

    /// <summary>毒气弹（id 26）：抛物线 + 落点毒场 RM1。</summary>
    public class SkillToxicRound : SkillBase
    {
        public override int Id => 26;
        public override float CastRange => 18f;
        public override float CD => 6f;
        public override int Store => 10;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.Gun, 7);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.Bezier, AimPos(entity));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Arc(context, index, 1.2f);
        protected override void OnCast(SkillContext context)
        {
            var caster = Caster(context);
            ushort casterId = caster != null ? caster.id : (ushort)0;
            ShootAll(caster, context, BuildAttack(caster, 1f, 1f,
                addEffect: ApplyEffect(EffectType.Poison, 1, Config.buff_duration_debuff, negative: true,
                    damage: Config.buff_dot_damage, sourceId: casterId)));
        }
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.RangeMagic, new[] { 0 }); // RM1
    }

    /// <summary>破甲重弹（id 27）：B49 + 减速 BF27。</summary>
    public class SkillHeavyBreaker : SkillBase
    {
        public override int Id => 27;
        public override float CastRange => 18f;
        public override float CD => 4f;
        public override int Store => 12;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.Gun, 8);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.Line, AimPos(entity));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.7f);
        protected override void OnCast(SkillContext context)
        {
            var caster = Caster(context);
            ushort casterId = caster != null ? caster.id : (ushort)0;
            ShootAll(caster, context, BuildAttack(caster, 1.6f, 0.45f,
                addEffect: ApplyEffect(EffectType.AnimSlowDown, 1, Config.buff_duration_debuff, negative: true, sourceId: casterId)));
        }
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 49 });
    }

    /// <summary>激励弹（id 28）：B57 + 友方加速 BF28。</summary>
    public class SkillInspireRound : SkillBase
    {
        public override int Id => 28;
        public override float CD => 8f;
        public override int Store => 10;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.Gun, 9);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            var ally = GetNearestAlly(entity);
            context.AddInts(ally != null ? ally.id : entity.id); // 目标（无友方时指向自己）
            context.AddVectors(ShootPos(entity), ally != null ? ally.transform.position : entity.transform.position);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.7f);
        protected override void OnCast(SkillContext context)
        {
            var caster = Caster(context);
            ushort casterId = caster != null ? caster.id : (ushort)0;
            GiveEffect(caster, EffectType.AnimSpeedUp, 1, Config.buff_duration_buff, sourceId: casterId);
            for (int i = 0; i < TargetCount(context); i++)
            {
                var ally = BattleManager.GetEntity(TargetId(context, i));
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
            PlayFollow(SkillVfxKind.Buff, 27, CasterId(context));  // BF28 环绕风（自身）
        }
    }

    /// <summary>冲击弹（id 29）：抛物线低伤强击退，落点 RM5。</summary>
    public class SkillImpactRound : SkillBase
    {
        public override int Id => 29;
        public override float CastRange => 18f;
        public override float CD => 8f;
        public override int Store => 10;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.Gun, 10);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.Bezier, AimPos(entity));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Arc(context, index, 1.1f);
        protected override void OnCast(SkillContext context)
        {
            var caster = Caster(context);
            ShootAll(caster, context, BuildAttack(caster, 0.4f, 1.2f, knockback: 5f));
        }
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.RangeMagic, new[] { 4 }); // RM5
    }

    /// <summary>闪现突进（id 30）：长距位移 · BF28 环绕风。</summary>
    public class SkillBlinkDash : SkillBase
    {
        public override int Id => 30;
        public override float CD => 12f;
        public override int Store => 4;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.Gun, 11);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            context.AddVectors(AimPos(entity));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        protected override void OnCast(SkillContext context)
        {
            Caster(context)?.SetMotion(new MotionToPoint(context.vectors[0], 28f)); // 长距闪现
        }
        public override void PlayVFX(SkillContext context) => PlayFollow(SkillVfxKind.Buff, 27, CasterId(context)); // BF28
    }

    /// <summary>狙击（id 31）：高伤细长直线 · B45。</summary>
    public class SkillSnipe : SkillBase
    {
        public override int Id => 31;
        public override float CastRange => 18f;
        public override float CD => 10f;
        public override int Store => 5;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.Gun, 12);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.Line, AimPos(entity));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.5f);
        protected override void OnCast(SkillContext context) => ShootAll(Caster(context), context, BuildAttack(Caster(context), 3f, 0.35f));
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 45 });
    }

    /// <summary>弹幕扫射（id 32）：八连发直线 · B44。</summary>
    public class SkillBulletSpray : SkillBase
    {
        public override int Id => 32;
        public override float CastRange => 18f;
        public override float CD => 15f;
        public override int Store => 3;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.Gun, 13);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.Line,
                FanDests(entity.transform.position, AimPos(entity), 8, 30f));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.6f);
        protected override void OnCast(SkillContext context) => ShootAll(Caster(context), context, BuildAttack(Caster(context), 1f, 0.4f));
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 44 });
    }

    /// <summary>空袭标记（id 33）：落点标记 MC8，随后橙色光柱轰炸 RM6。</summary>
    public class SkillAirstrikeMark : SkillBase
    {
        public override int Id => 33;
        public override float CastRange => 18f;
        public override float CD => 20f;
        public override int Store => 1;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.Gun, 14);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            context.AddVectors(entity.transform.position, AimPos(entity));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        protected override void OnCast(SkillContext context)
        {
            var caster = Caster(context);
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
    /// <summary>魔弹（id 34）：单发直线 · B0。</summary>
    public class SkillMagicBolt : SkillBase
    {
        public override int Id => 34;
        public override float CastRange => 15f;
        public override float CD => 0.6f;
        public override int Store => 20;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.MagicOrb, 0);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.Line, AimPos(entity));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.7f);
        protected override void OnCast(SkillContext context)
            => ShootAll(Caster(context), context, BuildAttack(Caster(context), 1f, 0.4f, useMagic: true));
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 0 });
    }

    /// <summary>火球（id 35）：抛物线 + 落点小型爆炸 RM5 · B15。</summary>
    public class SkillFireball : SkillBase
    {
        public override int Id => 35;
        public override float CastRange => 15f;
        public override float CD => 3f;
        public override int Store => 12;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.MagicOrb, 1);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.Bezier, AimPos(entity));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Arc(context, index, 1f);
        protected override void OnCast(SkillContext context)
            => ShootAll(Caster(context), context, BuildAttack(Caster(context), 1.5f, 1f, useMagic: true));
        public override void PlayVFX(SkillContext context)
        {
            PlayAlong(context, SkillVfxKind.Bullet, new[] { 15 });
            PlayAlong(context, SkillVfxKind.RangeMagic, new[] { 4 }); // RM5
        }
    }

    /// <summary>冰锥术（id 36）：B18 + 命中冻结 BF13。</summary>
    public class SkillIceShard : SkillBase
    {
        public override int Id => 36;
        public override float CastRange => 15f;
        public override float CD => 3f;
        public override int Store => 12;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.MagicOrb, 2);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.Line, AimPos(entity));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.8f);
        protected override void OnCast(SkillContext context)
        {
            var caster = Caster(context);
            ushort casterId = caster != null ? caster.id : (ushort)0;
            ShootAll(caster, context, BuildAttack(caster, 1.2f, 0.4f, useMagic: true,
                addEffect: ApplyEffect(EffectType.Freeze, 1, Config.buff_duration_control, negative: true, sourceId: casterId)));
        }
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 18 });
    }

    /// <summary>风刃（id 37）：贯穿直线 · B46。</summary>
    public class SkillWindBlade : SkillBase
    {
        public override int Id => 37;
        public override float CastRange => 15f;
        public override float CD => 2.5f;
        public override int Store => 12;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.MagicOrb, 3);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.Line, AimPos(entity));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.8f);
        protected override void OnCast(SkillContext context)
            => ShootAll(Caster(context), context, BuildAttack(Caster(context), 1.3f, 0.45f, useMagic: true));
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 46 });
    }

    /// <summary>毒珠（id 38）：B10 + 命中中毒 DoT BF19。</summary>
    public class SkillPoisonOrb : SkillBase
    {
        public override int Id => 38;
        public override float CastRange => 15f;
        public override float CD => 4f;
        public override int Store => 10;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.MagicOrb, 4);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.Line, AimPos(entity));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.8f);
        protected override void OnCast(SkillContext context)
        {
            var caster = Caster(context);
            ushort casterId = caster != null ? caster.id : (ushort)0;
            ShootAll(caster, context, BuildAttack(caster, 1f, 0.4f, useMagic: true,
                addEffect: ApplyEffect(EffectType.Poison, 1, Config.buff_duration_debuff, negative: true,
                    damage: Config.buff_dot_damage, sourceId: casterId)));
        }
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 10 });
    }

    /// <summary>岩崩（id 39）：升空天降 + 落点小型爆炸 RM5 · B25。</summary>
    public class SkillRockfall : SkillBase
    {
        public override int Id => 39;
        public override float CastRange => 15f;
        public override float CD => 5f;
        public override int Store => 10;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.MagicOrb, 5);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.SkyFall, AimPos(entity));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => SkyFall(context, index, 1.1f);
        protected override void OnCast(SkillContext context)
            => ShootAll(Caster(context), context, BuildAttack(Caster(context), 1.6f, 0.9f, useMagic: true));
        public override void PlayVFX(SkillContext context)
        {
            PlayAlong(context, SkillVfxKind.Bullet, new[] { 25 });
            PlayAlong(context, SkillVfxKind.RangeMagic, new[] { 4 }); // RM5
        }
    }

    /// <summary>雷球（id 40）：直线 + 链式跳 2 次 · B3。</summary>
    public class SkillThunderOrb : SkillBase
    {
        public override int Id => 40;
        public override float CastRange => 15f;
        public override float CD => 5f;
        public override int Store => 10;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.MagicOrb, 6);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            // 链式跳：记录「发射者 → 第一目标 → 第二目标」三个 id
            var first = GetNearestEnemy(entity, Config.default_skill_auto_target_radius);
            var second = first != null
                ? BattleManager.EntityContainer.GetNearestEnemy(first, Config.chain_jump_radius)
                : null;
            context.AddInts(first != null ? first.id : entity.id);
            context.AddInts(second != null ? second.id : (first != null ? first.id : entity.id));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }

        public override BulletTrajectory CreateTrajectory(SkillContext context, int index)
        {
            var trajectory = new ChainTrajectory(CasterId(context), (ushort)context.ints[1], (ushort)context.ints[2]);
            trajectory.Duration = Config.chain_jump_duration;
            return trajectory;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = Caster(context);
            var attack = BuildAttack(caster, 1.4f, 0.45f, useMagic: true);
            Tool.BattleManager?.ShootBullet(caster, attack, CreateTrajectory(context, 0));
        }
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 3 });
    }

    /// <summary>光盾（id 41）：自身/友方护盾 · S6。</summary>
    public class SkillLightShield : SkillBase
    {
        public override int Id => 41;
        public override float CD => 10f;
        public override int Store => 8;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.MagicOrb, 7);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        protected override void OnCast(SkillContext context)
        {
            var caster = Caster(context);
            if (caster == null) return;
            GiveEffect(caster, EffectType.Shield, 1, Config.buff_duration_buff,
                shieldValue: Config.buff_shield_value, sourceId: caster.id);
            var ally = GetNearestAlly(caster);
            if (ally != null)
            {
                GiveEffect(ally, EffectType.Shield, 1, Config.buff_duration_buff,
                    shieldValue: Config.buff_shield_value, sourceId: caster.id);
            }
        }
        public override void PlayVFX(SkillContext context) => PlayFollow(SkillVfxKind.Shield, 5, CasterId(context)); // S6
    }

    /// <summary>净化波动（id 42）：移除自身负面 Buff · MC2。</summary>
    public class SkillPurgeWave : SkillBase
    {
        public override int Id => 42;
        public override float CD => 10f;
        public override int Store => 8;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.MagicOrb, 8);
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
            Caster(context)?.effectController?.RemoveAllNegative(); // 净化：移除自身全部负面 Buff
        }
        public override void PlayVFX(SkillContext context)
            => PlayAt(SkillVfxKind.MagicCircle, 1, context.vectors[0], 1.2f); // MC2
    }

    /// <summary>激励法阵（id 43）：落点增伤法阵 · MC10。</summary>
    public class SkillInspireCircle : SkillBase
    {
        public override int Id => 43;
        public override float CD => 10f;
        public override int Store => 8;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.MagicOrb, 9);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.Point, AimPos(entity));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Point(context, index, 2f);
        protected override void OnCast(SkillContext context)
        {
            var caster = Caster(context);
            if (caster == null) return;
            BattleManager.EntityContainer.GetAllInCamp(Dest(context, 0), 3f, caster.camp, TargetBuffer);
            for (int i = 0; i < TargetBuffer.Count; i++)
            {
                GiveEffect(TargetBuffer[i], EffectType.AttrStrength, 1, Config.buff_duration_buff,
                    value: Config.buff_attr_value, sourceId: caster.id);
            }
        }
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.MagicCircle, new[] { 9 }); // MC10
    }

    /// <summary>冰霜新星（id 44）：环形八发 + 命中冻结 BF13 · B20。</summary>
    public class SkillFrostNova : SkillBase
    {
        public override int Id => 44;
        public override float CastRange => 3f;
        public override float CD => 10f;
        public override int Store => 6;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.MagicOrb, 10);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.Line,
                CircleDests(entity.transform.position, 3f, 8));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.7f);
        protected override void OnCast(SkillContext context)
        {
            var caster = Caster(context);
            ushort casterId = caster != null ? caster.id : (ushort)0;
            ShootAll(caster, context, BuildAttack(caster, 1.2f, 0.5f, useMagic: true,
                addEffect: ApplyEffect(EffectType.Freeze, 1, Config.buff_duration_control, negative: true, sourceId: casterId)));
        }
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 20 });
    }

    /// <summary>落雷术（id 45）：升空天降 + 落点麻痹 BF20 · B3。</summary>
    public class SkillThunderFall : SkillBase
    {
        public override int Id => 45;
        public override float CastRange => 15f;
        public override float CD => 10f;
        public override int Store => 5;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.MagicOrb, 11);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.SkyFall, AimPos(entity));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => SkyFall(context, index, 1.2f);
        protected override void OnCast(SkillContext context)
        {
            var caster = Caster(context);
            ushort casterId = caster != null ? caster.id : (ushort)0;
            ShootAll(caster, context, BuildAttack(caster, 2f, 1f, useMagic: true,
                addEffect: ApplyEffect(EffectType.Stun, 1, Config.buff_duration_control, negative: true, sourceId: casterId)));
        }
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 3 });
    }

    /// <summary>黑洞炸弹（id 46）：抛物线 + 落点黑洞爆炸 RM2。</summary>
    public class SkillBlackHoleBomb : SkillBase
    {
        public override int Id => 46;
        public override float CastRange => 15f;
        public override float CD => 5f;
        public override int Store => 8;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.MagicOrb, 12);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.Bezier, AimPos(entity));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Arc(context, index, 1.1f);
        protected override void OnCast(SkillContext context)
            => ShootAll(Caster(context), context, BuildAttack(Caster(context), 2f, 1.5f, useMagic: true));
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.RangeMagic, new[] { 1 }); // RM2
    }

    /// <summary>星辰坠落（id 47）：五发升空天降 + 落点小型爆炸 RM5 · B31。</summary>
    public class SkillStarfall : SkillBase
    {
        public override int Id => 47;
        public override float CastRange => 15f;
        public override float CD => 20f;
        public override int Store => 3;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.MagicOrb, 13);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.SkyFall,
                FanDests(entity.transform.position, AimPos(entity), 5, 30f));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => SkyFall(context, index, 1.6f);
        protected override void OnCast(SkillContext context)
            => ShootAll(Caster(context), context, BuildAttack(Caster(context), 2.2f, 0.9f, useMagic: true));
        public override void PlayVFX(SkillContext context)
        {
            PlayAlong(context, SkillVfxKind.Bullet, new[] { 31 });
            PlayAlong(context, SkillVfxKind.RangeMagic, new[] { 4 }); // RM5
        }
    }

    /// <summary>虚空之握（id 48）：直线 + 拉拽 · B1。</summary>
    public class SkillVoidGrasp : SkillBase
    {
        public override int Id => 48;
        public override float CastRange => 15f;
        public override float CD => 15f;
        public override int Store => 4;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.MagicOrb, 14);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.Line, AimPos(entity));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.8f);
        protected override void OnCast(SkillContext context)
        {
            var caster = Caster(context);
            Vector3 casterPos = caster != null ? caster.transform.position : Vector3.zero;
            ShootAll(caster, context, BuildAttack(caster, 1.2f, 0.6f, useMagic: true,
                onHit: PullTo(casterPos)));
        }
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 1 });
    }

    /// <summary>奥术屏障（id 49）：落点屏障墙 · MC7。</summary>
    public class SkillArcaneBarrier : SkillBase
    {
        public override int Id => 49;
        public override float CD => 20f;
        public override int Store => 2;
        public override WeaponRef FlyWeapon => new WeaponRef(WeaponCategory.MagicOrb, 15);
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.Point, AimPos(entity));
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Weapon_R, () => OnCast(context))) OnCast(context);
            return context;
        }
        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Point(context, index, 2f);
        protected override void OnCast(SkillContext context)
        {
            var caster = Caster(context);
            if (caster == null) return;
            // 屏障不做阻挡：改为落点范围内的友方护盾
            BattleManager.EntityContainer.GetAllInCamp(Dest(context, 0), 3f, caster.camp, TargetBuffer);
            for (int i = 0; i < TargetBuffer.Count; i++)
            {
                GiveEffect(TargetBuffer[i], EffectType.Shield, 1, Config.buff_duration_buff,
                    shieldValue: Config.buff_shield_value, sourceId: caster.id);
            }
        }
        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.MagicCircle, new[] { 6 }); // MC7
    }
    #endregion
}
