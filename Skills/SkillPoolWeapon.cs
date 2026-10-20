using System;
using Ros.Skill.Utils;
using Ros.Transport;
using UnityEngine;

namespace Ros.Skill
{
    // 武器技能（50 个）。槽位 → 技能 的对应是临时顺序分配（刀 11 / 长枪 8 / 枪械 15 / 魔法球 16），
    // 需要调整时只改本文件里各类的 WeaponRef 与 SkillTypes 顺序，不用动实现。
    public static class SkillPoolWeapon
    {
        public static void RegisterAll()
        {
            // 刀（0~10）
            SkillManager.Register(new SkillFanIceCone());
            SkillManager.Register(new SkillTripleThunderCone());
            SkillManager.Register(new SkillFreezeShot());
            SkillManager.Register(new SkillStrengthBreakShot());
            SkillManager.Register(new SkillDoubleFanShot());
            SkillManager.Register(new SkillOrbitBlades());
            SkillManager.Register(new SkillBackstepShot());
            SkillManager.Register(new SkillLeapScatter());
            SkillManager.Register(new SkillThrowWeapon());
            SkillManager.Register(new SkillFanWeaponThrow());
            SkillManager.Register(new SkillSkyfallWeapon());

            // 长枪（11~18）
            SkillManager.Register(new SkillBoomerangWeapon());
            SkillManager.Register(new SkillBackstabWeapon());
            SkillManager.Register(new SkillSwiftAura());
            SkillManager.Register(new SkillOrbitWeaponKnockback());
            SkillManager.Register(new SkillShortDash());
            SkillManager.Register(new SkillShieldTen());
            SkillManager.Register(new SkillFlyingKick());
            SkillManager.Register(new SkillIceFanTiny());

            // 枪械（19~33）
            SkillManager.Register(new SkillBackVolley());
            SkillManager.Register(new SkillIceSweep());
            SkillManager.Register(new SkillRootShot());
            SkillManager.Register(new SkillDriftOrb());
            SkillManager.Register(new SkillTripleWaveIce());
            SkillManager.Register(new SkillForwardSpray());
            SkillManager.Register(new SkillSilenceBolt());
            SkillManager.Register(new SkillSkyfallVolley());
            SkillManager.Register(new SkillOrbitWeaponVolley());
            SkillManager.Register(new SkillOrbitWeaponRain());
            SkillManager.Register(new SkillPullSmash());
            SkillManager.Register(new SkillRainBarrage());
            SkillManager.Register(new SkillDashSpeed());
            SkillManager.Register(new SkillAllyShieldEight());
            SkillManager.Register(new SkillShieldFour());

            // 魔法球（34~49）
            SkillManager.Register(new SkillSplitShot());
            SkillManager.Register(new SkillPullBolt());
            SkillManager.Register(new SkillStaticBarrage());
            SkillManager.Register(new SkillFanFreeze());
            SkillManager.Register(new SkillSweepPoison());
            SkillManager.Register(new SkillBounceShot());
            SkillManager.Register(new SkillSweepBurn());
            SkillManager.Register(new SkillCircleVolley());
            SkillManager.Register(new SkillHookFan());
            SkillManager.Register(new SkillVortexBomb());
            SkillManager.Register(new SkillSixOrbs());
            SkillManager.Register(new SkillSkyfallSilence());
            SkillManager.Register(new SkillBlastKnockback());
            SkillManager.Register(new SkillBackArcWeapons());
            SkillManager.Register(new SkillAllyCritBoost());
            SkillManager.Register(new SkillAllyShieldTwentyFive());
        }
    }

    #region 刀
    public class SkillFanIceCone : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillFanIceCone;
        public override float CastRange => 12f;
        public override float CD => 6f;
        public override int Store => 10;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Knife, 0);
        protected override EntityAnchor OriginAnchor => EntityAnchor.UpFront;
        protected override int Shots => 3;
        protected override float SpreadDeg => 12f;
        protected override float BulletLife => 0.5f;
        protected override int VfxIndex => 18;
        protected override Action<EntityEffectController> HitEffect(EntityData caster) => ApplyEffect(EffectType.AnimSlowDown, 1, 5f, caster);
    }

    public class SkillTripleThunderCone : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillTripleThunderCone;
        public override float CastRange => 14f;
        public override float CD => 7f;
        public override int Store => 8;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Knife, 1);
        protected override EntityAnchor OriginAnchor => EntityAnchor.UpCenter;
        protected override int Shots => 1;
        protected override float SpreadDeg => 0f;
        protected override float BulletLife => 0.45f;
        protected override int VfxIndex => 19;
        protected override int WaveCount => 3;
        protected override float WaveInterval => 0.12f;
        protected override Action<EntityEffectController> HitEffect(EntityData caster) => ApplyEffect(EffectType.Stun, 1, 2f, caster);
    }

    public class SkillFreezeShot : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillFreezeShot;
        public override float CastRange => 12f;
        public override float CD => 6f;
        public override int Store => 10;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Knife, 2);
        protected override EntityAnchor OriginAnchor => EntityAnchor.UpFront;
        protected override int Shots => 1;
        protected override float SpreadDeg => 0f;
        protected override float BulletLife => 0.5f;
        protected override int VfxIndex => 21;
        protected override Action<EntityEffectController> HitEffect(EntityData caster) => ApplyEffect(EffectType.Freeze, 1, 2f, caster);
    }

    public class SkillStrengthBreakShot : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillStrengthBreakShot;
        public override float CastRange => 12f;
        public override float CD => 8f;
        public override int Store => 8;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Knife, 3);
        protected override EntityAnchor OriginAnchor => EntityAnchor.UpFront;
        protected override int Shots => 1;
        protected override float SpreadDeg => 0f;
        protected override float BulletLife => 0.55f;
        protected override int VfxIndex => 22;
        protected override Action<EntityEffectController> HitEffect(EntityData caster) => ApplyEffect(EffectType.AttrStrengthDecrease, 2, 20f, caster);
    }

    public class SkillDoubleFanShot : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillDoubleFanShot;
        public override float CastRange => 13f;
        public override float CD => 7f;
        public override int Store => 8;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Knife, 4);
        protected override EntityAnchor OriginAnchor => EntityAnchor.UpFront;
        protected override int Shots => 1;
        protected override float SpreadDeg => 0f;
        protected override float BulletLife => 0.5f;
        protected override int VfxIndex => 39;
        protected override int WaveCount => 2;
        protected override float WaveInterval => 0.18f;
        protected override int ShotsOfWave(int wave) => wave == 0 ? 1 : 2;
        protected override float SpreadOfWave(int wave) => wave == 0 ? 0f : 30f;
        protected override Action<EntityEffectController> HitEffect(EntityData caster) => ApplyEffect(EffectType.AnimSlowDown, 1, 4f, caster);
    }

    // 四发剑气势力绕自身旋转
    public class SkillOrbitBlades : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillOrbitBlades;
        public override float CD => 10f;
        public override int Store => 5;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Knife, 5);
        protected override EntityAnchor OriginAnchor => EntityAnchor.UpCenter;
        protected override int Shots => 4;
        protected override float SpreadDeg => 0f;
        protected override float BulletLife => 2.4f;
        protected override float BulletRadius => 0.6f;
        protected override float DamageInterval => 0.4f;
        protected override int VfxIndex => 40;
        private const float Radius = 2.2f;
        private const float Turns = 1.5f;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id, SkillSeed.New());
            context.AddVectors(entity.transform.position);
            return context;
        }

        protected override BulletTrajectory TrajectoryFor(SkillContext context, int wave, int shotIndex, int shotsInWave, float spread)
            => new OrbitTrajectory((ushort)context.ints[0], context.vectors[0], Radius, shotIndex * 360f / 4f, Turns, BulletLife);
    }

    // 后撤闪避（短暂闪避 buff）后向前一发
    public class SkillBackstepShot : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillBackstepShot;
        public override float CastRange => 12f;
        public override float CD => 9f;
        public override int Store => 6;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Knife, 6);
        protected override EntityAnchor OriginAnchor => EntityAnchor.UpFront;
        protected override int Shots => 1;
        protected override float SpreadDeg => 0f;
        protected override float BulletLife => 0.5f;
        protected override int VfxIndex => 41;
        private const float BackDash = 0.25f;

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            if (caster == null) return;
            caster.SetMotion(new MotionDash(BackDash, -9f));
            GiveEffect(caster, EffectType.DodgeBoost, 1, 0.8f, caster);
            base.OnCast(context);
        }
    }

    // 跃起，向周围随机散射
    public class SkillLeapScatter : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillLeapScatter;
        public override float CD => 14f;
        public override int Store => 4;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Knife, 7);
        protected override EntityAnchor OriginAnchor => EntityAnchor.UpCenter;
        protected override int Shots => 6;
        protected override float SpreadDeg => 0f;
        protected override float BulletLife => 0.9f;
        protected override int VfxIndex => 57;
        private const float Radius = 6f;

        protected override BulletTrajectory TrajectoryFor(SkillContext context, int wave, int shotIndex, int shotsInWave, float spread)
        {
            Vector3 origin = OriginPos((ushort)context.ints[0], OriginAnchor, OriginOffset, context.vectors[0]);
            var rng = SkillSeed.Rng(context.ints[1]);
            float angle = 0f;
            for (int i = 0; i <= shotIndex; i++) angle = (float)rng.NextDouble() * 360f;
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            return TrialConstructor.Line(origin, origin + dir * Radius, BulletLife);
        }
    }

    public class SkillThrowWeapon : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillThrowWeapon;
        public override float CastRange => 10f;
        public override float CD => 5f;
        public override int Store => 10;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Knife, 8);
        protected override EntityAnchor OriginAnchor => EntityAnchor.RightHand;
        protected override int Shots => 1;
        protected override float SpreadDeg => 0f;
        protected override float BulletLife => 0.6f;
        protected override float Knockback => 3f;
        protected override SkillVfxKind VfxKind => SkillVfxKind.Weapon;
        protected override int VfxIndex => (int)Weapon;
    }

    public class SkillFanWeaponThrow : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillFanWeaponThrow;
        public override float CastRange => 10f;
        public override float CD => 8f;
        public override int Store => 6;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Knife, 9);
        protected override EntityAnchor OriginAnchor => EntityAnchor.UpCenter;
        protected override int Shots => 5;
        protected override float SpreadDeg => 30f;
        protected override float BulletLife => 0.65f;
        protected override SkillVfxKind VfxKind => SkillVfxKind.Weapon;
        protected override int VfxIndex => (int)Weapon;
    }

    public class SkillSkyfallWeapon : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillSkyfallWeapon;
        public override float CastRange => 12f;
        public override float CD => 10f;
        public override int Store => 5;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Knife, 10);
        protected override EntityAnchor OriginAnchor => EntityAnchor.UpCenter;
        protected override int Shots => 1;
        protected override float SpreadDeg => 0f;
        protected override float BulletLife => 1.1f;
        protected override float Knockback => 3f;
        protected override SkillVfxKind VfxKind => SkillVfxKind.Weapon;
        protected override int VfxIndex => (int)Weapon;

        protected override BulletTrajectory TrajectoryFor(SkillContext context, int wave, int shotIndex, int shotsInWave, float spread)
        {
            Vector3 origin = OriginPos((ushort)context.ints[0], OriginAnchor, OriginOffset, context.vectors[0]);
            return TrialConstructor.SkyFall(origin, context.vectors[1], BulletLife);
        }
    }
    #endregion

    #region 长枪
    public class SkillBoomerangWeapon : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillBoomerangWeapon;
        public override float CastRange => 9f;
        public override float CD => 9f;
        public override int Store => 6;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Spear, 0);
        protected override EntityAnchor OriginAnchor => EntityAnchor.RightHand;
        protected override int Shots => 1;
        protected override float SpreadDeg => 0f;
        protected override float BulletLife => 1.6f;
        protected override float DamageInterval => 0.3f;
        protected override SkillVfxKind VfxKind => SkillVfxKind.Weapon;
        protected override int VfxIndex => (int)Weapon;

        protected override BulletTrajectory TrajectoryFor(SkillContext context, int wave, int shotIndex, int shotsInWave, float spread)
        {
            Vector3 origin = OriginPos((ushort)context.ints[0], OriginAnchor, OriginOffset, context.vectors[0]);
            return new BoomerangTrajectory((ushort)context.ints[0], origin, context.vectors[1]);
        }
    }

    // 后方飞出扎向敌人，命中麻痹；到达后原地静止漂浮一段
    public class SkillBackstabWeapon : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillBackstabWeapon;
        public override float CastRange => 11f;
        public override float CD => 10f;
        public override int Store => 5;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Spear, 1);
        protected override EntityAnchor OriginAnchor => EntityAnchor.UpCenter;
        protected override Vector3 OriginOffset => new Vector3(0f, 0f, -2f);
        protected override int Shots => 1;
        protected override float SpreadDeg => 0f;
        protected override float BulletLife => 0.55f;
        protected override SkillVfxKind VfxKind => SkillVfxKind.Weapon;
        protected override int VfxIndex => (int)Weapon;
        protected override Action<EntityEffectController> HitEffect(EntityData caster) => ApplyEffect(EffectType.Stun, 1, 2f, caster);
        private const float HoverTime = 1.2f;

        public override void PlayVFX(SkillContext context)
        {
            base.PlayVFX(context);
            // 命中后原地静止漂浮：纯表现，不参与判定
            Vector3 end = context.vectors[1];
            VfxHelper.PlayOne(SkillVfxKind.Weapon, VfxIndex, TrialConstructor.Point(end, HoverTime));
        }
    }

    public class SkillSwiftAura : SkillBase
    {
        public override int Id => (int)SkillType.SkillSwiftAura;
        public override float CD => 20f;
        public override int Store => 5;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Spear, 2);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Mega_Short;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            GiveEffect(caster, EffectType.AnimSpeedUp, 1, 20f, caster);
        }

        public override void PlayVFX(SkillContext context) { } // buff 持续表现由客户端按 Config.buff_vfx 统一播
    }

    public class SkillOrbitWeaponKnockback : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillOrbitWeaponKnockback;
        public override float CD => 12f;
        public override int Store => 4;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Spear, 3);
        protected override EntityAnchor OriginAnchor => EntityAnchor.UpCenter;
        protected override int Shots => 4;
        protected override float SpreadDeg => 0f;
        protected override float BulletLife => 2f;
        protected override float BulletRadius => 0.6f;
        protected override float Knockback => 6f;
        protected override float DamageInterval => 0.5f;
        protected override SkillVfxKind VfxKind => SkillVfxKind.Weapon;
        protected override int VfxIndex => (int)Weapon;
        private const float Radius = 2f;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id, SkillSeed.New());
            context.AddVectors(entity.transform.position);
            return context;
        }

        protected override BulletTrajectory TrajectoryFor(SkillContext context, int wave, int shotIndex, int shotsInWave, float spread)
            => new OrbitTrajectory((ushort)context.ints[0], context.vectors[0], Radius, shotIndex * 90f, 1.25f, BulletLife);
    }

    public class SkillShortDash : SkillBase
    {
        public override int Id => (int)SkillType.SkillShortDash;
        public override float CD => 6f;
        public override int Store => 8;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Spear, 4);

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            if (caster != null) caster.SetMotion(new MotionDash(0.25f, 11f));
        }

        public override void PlayVFX(SkillContext context) { } // 1019：无特效（纯位移）
    }

    public class SkillShieldTen : SkillBase
    {
        public override int Id => (int)SkillType.SkillShieldTen;
        public override float CD => 30f;
        public override int Store => 2;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Spear, 5);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Mega_Short;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            GiveEffect(caster, EffectType.Shield, 10, 60f, caster);
        }

        public override void PlayVFX(SkillContext context) { } // buff 持续表现由客户端按 Config.buff_vfx 统一播
    }

    public class SkillFlyingKick : SkillBase
    {
        public override int Id => (int)SkillType.SkillFlyingKick;
        public override float CastRange => 3f;
        public override float CD => 8f;
        public override int Store => 6;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Spear, 6);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Jump_Mega;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            context.AddVectors(entity.transform.position + entity.transform.forward * 1.2f);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            if (caster == null) return;
            StrikeSphere(caster, context.vectors[0], 1.4f, BuildAttack(caster, 1.2f, 1.4f, knockback: 7f));
        }

        public override void PlayVFX(SkillContext context) { } // 1019：无特效（飞踢）
    }

    public class SkillIceFanTiny : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillIceFanTiny;
        public override float CastRange => 13f;
        public override float CD => 6f;
        public override int Store => 9;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Spear, 7);
        protected override EntityAnchor OriginAnchor => EntityAnchor.UpFront;
        protected override int Shots => 4;
        protected override float SpreadDeg => 8f;
        protected override float BulletLife => 0.5f;
        protected override int VfxIndex => 9;
        protected override Action<EntityEffectController> HitEffect(EntityData caster) => ApplyEffect(EffectType.Freeze, 1, 2f, caster);
    }
    #endregion

    #region 枪械
    // 后方随机点创建子弹，集中射向前方一点后继续飞行
    public class SkillBackVolley : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillBackVolley;
        public override float CastRange => 14f;
        public override float CD => 10f;
        public override int Store => 6;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 0);
        protected override EntityAnchor OriginAnchor => EntityAnchor.UpCenter;
        protected override int Shots => 6;
        protected override float SpreadDeg => 0f;
        protected override float BulletLife => 0.9f;
        protected override int VfxIndex => 11;
        private const float BehindMin = 1.5f;
        private const float BehindMax = 3.5f;

        protected override BulletTrajectory TrajectoryFor(SkillContext context, int wave, int shotIndex, int shotsInWave, float spread)
        {
            Vector3 center = context.vectors[0];
            Quaternion rot = BulletTrajectory.TryGetEntityTransform((ushort)context.ints[0], out _, out var entityRot) ? entityRot : Quaternion.identity;
            var rng = SkillSeed.Rng(context.ints[1]);
            Vector3 origin = center;
            for (int i = 0; i <= shotIndex; i++)
            {
                float back = BehindMin + (float)rng.NextDouble() * (BehindMax - BehindMin);
                float side = ((float)rng.NextDouble() - 0.5f) * 3f;
                origin = center + rot * new Vector3(side, 0f, -back);
            }
            Vector3 aim = context.vectors[1];
            return TrialConstructor.Line(origin, aim, BulletLife);
        }
    }

    public class SkillIceSweep : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillIceSweep;
        public override float CastRange => 14f;
        public override float CD => 9f;
        public override int Store => 6;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 1);
        protected override EntityAnchor OriginAnchor => EntityAnchor.UpFront;
        protected override int Shots => 1;
        protected override float SpreadDeg => 0f;
        protected override float BulletLife => 0.45f;
        protected override int VfxIndex => 20;
        protected override int WaveCount => 4;
        protected override float WaveInterval => 0.1f;
        protected override float SpreadOfWave(int wave) => (wave - 1.5f) * 8f;
        protected override Action<EntityEffectController> HitEffect(EntityData caster) => ApplyEffect(EffectType.Freeze, 1, 3f, caster);
    }

    public class SkillRootShot : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillRootShot;
        public override float CastRange => 14f;
        public override float CD => 9f;
        public override int Store => 6;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 2);
        protected override EntityAnchor OriginAnchor => EntityAnchor.UpFront;
        protected override int Shots => 1;
        protected override float SpreadDeg => 0f;
        protected override float BulletLife => 0.45f;
        protected override int VfxIndex => 28;
        protected override Action<EntityEffectController> HitEffect(EntityData caster) => ApplyEffect(EffectType.Root, 1, 3f, caster);
    }

    public class SkillDriftOrb : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillDriftOrb;
        public override float CastRange => 12f;
        public override float CD => 8f;
        public override int Store => 6;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 3);
        protected override EntityAnchor OriginAnchor => EntityAnchor.UpCenter;
        protected override int Shots => 1;
        protected override float SpreadDeg => 0f;
        protected override float BulletLife => 1.6f;
        protected override float Knockback => 5f;
        protected override int VfxIndex => 29;
    }

    public class SkillTripleWaveIce : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillTripleWaveIce;
        public override float CastRange => 13f;
        public override float CD => 12f;
        public override int Store => 4;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 4);
        protected override EntityAnchor OriginAnchor => EntityAnchor.UpFront;
        protected override int Shots => 3;
        protected override float SpreadDeg => 25f;
        protected override float BulletLife => 0.5f;
        protected override int VfxIndex => 45;
        protected override int WaveCount => 3;
        protected override float WaveInterval => 0.2f;
        protected override int ShotsOfWave(int wave) => 3 + wave * 2;
        protected override Action<EntityEffectController> HitEffect(EntityData caster) => ApplyEffect(EffectType.Freeze, 1, 2f, caster);
    }

    public class SkillForwardSpray : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillForwardSpray;
        public override float CastRange => 12f;
        public override float CD => 10f;
        public override int Store => 5;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 5);
        protected override EntityAnchor OriginAnchor => EntityAnchor.UpFront;
        protected override int Shots => 5;
        protected override float SpreadDeg => 35f;
        protected override float BulletLife => 0.45f;
        protected override int VfxIndex => 46;
        protected override int WaveCount => 3;
        protected override float WaveInterval => 0.12f;
    }

    public class SkillSilenceBolt : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillSilenceBolt;
        public override float CastRange => 18f;
        public override float CD => 12f;
        public override int Store => 5;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 6);
        protected override EntityAnchor OriginAnchor => EntityAnchor.UpFront;
        protected override int Shots => 1;
        protected override float SpreadDeg => 0f;
        protected override float BulletLife => 0.25f;
        protected override float DamageRate => 1.2f;
        protected override int VfxIndex => 47;
        protected override Action<EntityEffectController> HitEffect(EntityData caster) => ApplyEffect(EffectType.Silence, 1, 12f, caster);
    }

    public class SkillSkyfallVolley : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillSkyfallVolley;
        public override float CastRange => 12f;
        public override float CD => 12f;
        public override int Store => 4;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 7);
        protected override EntityAnchor OriginAnchor => EntityAnchor.UpCenter;
        protected override int Shots => 5;
        protected override float SpreadDeg => 0f;
        protected override float BulletLife => 1.1f;
        protected override SkillVfxKind VfxKind => SkillVfxKind.RangeMagic;
        protected override int VfxIndex => 2;
        private const float Ring = 4f;

        protected override BulletTrajectory TrajectoryFor(SkillContext context, int wave, int shotIndex, int shotsInWave, float spread)
        {
            Vector3 center = context.vectors[1];
            return TrialConstructor.SkyFall(center, center + Utils.SpreadStyle.CircleDests(Vector3.zero, Ring, 5)[shotIndex], BulletLife);
        }
    }

    public class SkillOrbitWeaponVolley : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillOrbitWeaponVolley;
        public override float CastRange => 14f;
        public override float CD => 14f;
        public override int Store => 4;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 8);
        protected override EntityAnchor OriginAnchor => EntityAnchor.UpCenter;
        protected override int Shots => 2;
        protected override float SpreadDeg => 10f;
        protected override float BulletLife => 0.5f;
        protected override int WaveCount => 3;
        protected override float WaveInterval => 0.2f;
        protected override SkillVfxKind VfxKind => SkillVfxKind.Weapon;
        protected override int VfxIndex => (int)Weapon;

        protected override BulletTrajectory TrajectoryFor(SkillContext context, int wave, int shotIndex, int shotsInWave, float spread)
        {
            Vector3 center = context.vectors[0];
            Vector3 start = center + Utils.SpreadStyle.CircleDests(center, 1.5f, 6, wave * 60f + shotIndex * 30f)[0];
            return TrialConstructor.Line(start, context.vectors[1], BulletLife);
        }
    }

    public class SkillOrbitWeaponRain : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillOrbitWeaponRain;
        public override float CastRange => 13f;
        public override float CD => 15f;
        public override int Store => 3;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 9);
        protected override EntityAnchor OriginAnchor => EntityAnchor.UpCenter;
        protected override int Shots => 3;
        protected override float SpreadDeg => 0f;
        protected override float BulletLife => 1f;
        protected override int WaveCount => 3;
        protected override float WaveInterval => 0.25f;
        protected override SkillVfxKind VfxKind => SkillVfxKind.Weapon;
        protected override int VfxIndex => (int)Weapon;
        private const float Ring = 3f;

        protected override BulletTrajectory TrajectoryFor(SkillContext context, int wave, int shotIndex, int shotsInWave, float spread)
        {
            Vector3 center = context.vectors[1];
            int idx = (wave * shotsInWave + shotIndex) % 6;
            Vector3 land = center + Utils.SpreadStyle.CircleDests(Vector3.zero, Ring, 6)[idx];
            return TrialConstructor.SkyFall(center + Vector3.up * 8f, land, BulletLife);
        }
    }

    // 牵引周围敌人到自身位置，然后原地砸击
    public class SkillPullSmash : SkillBase
    {
        public override int Id => (int)SkillType.SkillPullSmash;
        public override float CD => 20f;
        public override int Store => 2;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 10);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Mega_Long;
        private const float PullRadius = 8f;
        private const float PullTime = 0.5f;

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
            if (caster == null) return;
            Vector3 center = caster.transform.position;
            Utils.TargetSelect.SetEnemiesInRangeToBuffer(center, PullRadius, caster.camp);
            for (int i = 0; i < Utils.TargetSelect.TargetBuffer.Count; i++)
            {
                var target = Utils.TargetSelect.TargetBuffer[i];
                if (target != null) target.SetMotion(new MotionAttract(center, PullTime)); // 牵引点 = 施放瞬间的位置
            }
            Timer.AddTimer(0, _ =>
            {
                var stillCaster = FindEntity((ushort)context.ints[0]);
                if (stillCaster == null) return;
                StrikeSphere(stillCaster, stillCaster.transform.position, 3f,
                    BuildAttack(stillCaster, 1.4f, 3f, knockback: 6f));
            }, PullTime, 1, false);
        }

        public override void PlayVFX(SkillContext context)
        {
            VfxHelper.PlayAt(SkillVfxKind.RangeMagic, 7, context.vectors[0], 1f);
            Timer.AddTimer(0, _ => VfxHelper.PlayAt(SkillVfxKind.RangeMagic, 4, context.vectors[0], 0.8f), PullTime, 1, false);
        }
    }

    public class SkillRainBarrage : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillRainBarrage;
        public override float CastRange => 14f;
        public override float CD => 14f;
        public override int Store => 4;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 11);
        protected override EntityAnchor OriginAnchor => EntityAnchor.UpFront;
        protected override int Shots => 4;
        protected override float SpreadDeg => 0f;
        protected override float BulletLife => 0.9f;
        protected override int VfxIndex => 44;
        protected override int WaveCount => 3;
        protected override float WaveInterval => 0.25f;

        protected override BulletTrajectory TrajectoryFor(SkillContext context, int wave, int shotIndex, int shotsInWave, float spread)
        {
            Vector3 center = context.vectors[1];
            int idx = (wave * shotsInWave + shotIndex) % 8;
            Vector3 land = center + Utils.SpreadStyle.CircleDests(Vector3.zero, 4f, 8)[idx];
            return TrialConstructor.SkyFall(center + Vector3.up * 10f, land, BulletLife);
        }
    }

    public class SkillDashSpeed : SkillBase
    {
        public override int Id => (int)SkillType.SkillDashSpeed;
        public override float CD => 16f;
        public override int Store => 4;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 12);

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
            caster.SetMotion(new MotionDash(0.35f, 13f));
            GiveEffect(caster, EffectType.AnimSpeedUp, 2, 10f, caster);
        }

        public override void PlayVFX(SkillContext context) { } // buff 持续表现由客户端按 Config.buff_vfx 统一播
    }

    public class SkillAllyShieldEight : SkillBase
    {
        public override int Id => (int)SkillType.SkillAllyShieldEight;
        public override float CD => 25f;
        public override int Store => 3;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 13);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Mega_Short;
        private const float Radius = 10f;

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
                if (ally == null || (ally.transform.position - center).sqrMagnitude > Radius * Radius) continue;
                GiveEffect(ally, EffectType.Shield, 8, 60f, caster);
            }
        }

        public override void PlayVFX(SkillContext context) { } // buff 持续表现由客户端按 Config.buff_vfx 统一播
    }

    public class SkillShieldFour : SkillBase
    {
        public override int Id => (int)SkillType.SkillShieldFour;
        public override float CD => 30f;
        public override int Store => 3;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.Gun, 14);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Mega_Short;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            GiveEffect(caster, EffectType.Shield, 4, 10f, caster);
        }

        public override void PlayVFX(SkillContext context) { } // buff 持续表现由客户端按 Config.buff_vfx 统一播
    }
    #endregion

    #region 魔法球
    // 母弹飞到终点（按弹速算出的时刻）后分裂 5 发扇形
    public class SkillSplitShot : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillSplitShot;
        public override float CastRange => 16f;
        public override float CD => 12f;
        public override int Store => 5;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 0);
        protected override EntityAnchor OriginAnchor => EntityAnchor.UpCenter;
        protected override int Shots => 1;
        protected override float SpreadDeg => 0f;
        protected override float BulletLife => 0.6f;
        protected override int VfxIndex => 0;
        private const int SplitCount = 5;
        private const float SplitSpread = 30f;
        private const int SplitVfx = 36;

        protected override void ShootWave(EntityData caster, SkillContext context, int wave)
        {
            base.ShootWave(caster, context, wave);
            Timer.AddTimer(0, _ => Split(caster, context), BulletLife, 1, false);
        }

        public override void PlayVFX(SkillContext context)
        {
            base.PlayVFX(context);
            Timer.AddTimer(0, _ =>
            {
                Vector3 end = context.vectors[1];
                Vector3 origin = OriginPos((ushort)context.ints[0], OriginAnchor, OriginOffset, context.vectors[0]);
                var dests = SpreadStyle.FanDests(origin, end, SplitCount, SplitSpread);
                for (int i = 0; i < SplitCount; i++)
                    VfxHelper.PlayOne(SkillVfxKind.Bullet, SplitVfx, TrialConstructor.Line(end, dests[i], BulletLife));
            }, BulletLife, 1, false);
        }

        private void Split(EntityData caster, SkillContext context)
        {
            if (caster == null) return;
            Vector3 end = context.vectors[1];
            Vector3 origin = OriginPos(caster, OriginAnchor, OriginOffset);
            var dests = SpreadStyle.FanDests(origin, end, SplitCount, SplitSpread);
            var attack = BuildAttack(caster, DamageRate, BulletRadius);
            for (int i = 0; i < SplitCount; i++)
                Tool.BattleManager.ShootBullet(caster, attack, TrialConstructor.Line(end, dests[i], BulletLife), BulletLife);
        }
    }

    // 命中后把目标持续拉向弹道终点
    public class SkillPullBolt : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillPullBolt;
        public override float CastRange => 15f;
        public override float CD => 14f;
        public override int Store => 4;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 1);
        protected override EntityAnchor OriginAnchor => EntityAnchor.UpCenter;
        protected override int Shots => 1;
        protected override float SpreadDeg => 0f;
        protected override float BulletLife => 0.7f;
        protected override int VfxIndex => 1;
        private const float PullDuration = 1.2f;

        // 命中后把目标持续拉向「正在飞的子弹」：服务器子弹没有 Transform，用轨迹+出生时间算实时位置
        protected override void ShootWave(EntityData caster, SkillContext context, int wave)
        {
            if (caster == null || Tool.BattleManager == null) return;
            BulletTrajectory trajectory = TrajectoryFor(context, wave, 0, 1, 0f);
            float spawn = Time.time;
            var attack = BuildAttack(caster, DamageRate, BulletRadius, onHit: target =>
            {
                if (target == null) return;
                target.SetMotion(new MotionAttract(
                    () => trajectory.Lerp(Mathf.Clamp01((Time.time - spawn) / BulletLife)), PullDuration));
            });
            Tool.BattleManager.ShootBullet(caster, attack, trajectory, BulletLife);
        }
    }

    public class SkillStaticBarrage : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillStaticBarrage;
        public override float CD => 18f;
        public override int Store => 3;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 2);
        protected override EntityAnchor OriginAnchor => EntityAnchor.UpCenter;
        protected override int Shots => 12;
        protected override float SpreadDeg => 0f;
        protected override float BulletLife => 3f;
        protected override float DamageInterval => 1f;
        protected override int VfxIndex => 2;
        private const float Ring = 2.6f;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id, SkillSeed.New());
            context.AddVectors(entity.transform.position);
            return context;
        }

        protected override BulletTrajectory TrajectoryFor(SkillContext context, int wave, int shotIndex, int shotsInWave, float spread)
            => TrialConstructor.Point(context.vectors[0] + Utils.SpreadStyle.CircleDests(Vector3.zero, Ring, 12)[shotIndex], BulletLife);

        protected override Action<EntityEffectController> HitEffect(EntityData caster) => ApplyEffect(EffectType.Root, 1, 10f, caster);
    }

    public class SkillFanFreeze : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillFanFreeze;
        public override float CastRange => 14f;
        public override float CD => 10f;
        public override int Store => 5;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 3);
        protected override EntityAnchor OriginAnchor => EntityAnchor.UpCenter;
        protected override int Shots => 5;
        protected override float SpreadDeg => 25f;
        protected override float BulletLife => 0.55f;
        protected override int VfxIndex => 3;
        protected override Action<EntityEffectController> HitEffect(EntityData caster) => ApplyEffect(EffectType.Freeze, 1, 4f, caster);
    }

    // 来回扫射
    public class SkillSweepPoison : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillSweepPoison;
        public override float CastRange => 12f;
        public override float CD => 12f;
        public override int Store => 4;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 4);
        protected override EntityAnchor OriginAnchor => EntityAnchor.UpCenter;
        protected override int Shots => 1;
        protected override float SpreadDeg => 0f;
        protected override float BulletLife => 0.4f;
        protected override int VfxIndex => 4;
        protected override int WaveCount => 6;
        protected override float WaveInterval => 0.1f;
        protected override Action<EntityEffectController> HitEffect(EntityData caster) => ApplyEffect(EffectType.Poison, 3, 5f, caster);

        protected override BulletTrajectory TrajectoryFor(SkillContext context, int wave, int shotIndex, int shotsInWave, float spread)
        {
            Vector3 origin = OriginPos((ushort)context.ints[0], OriginAnchor, OriginOffset, context.vectors[0]);
            float[] angles = { -25f, 0f, 25f, 25f, 0f, -25f };
            Vector3 aim = Utils.SpreadStyle.RotateTargetAroundDirection(origin, context.vectors[1], angles[wave % angles.Length]);
            return TrialConstructor.Line(origin, aim, BulletLife);
        }
    }

    // 在周围实体间弹跳 8 次：施放时算好 9 个点整串下发
    public class SkillBounceShot : SkillBase
    {
        public override int Id => (int)SkillType.SkillBounceShot;
        public override float CastRange => 16f;
        public override float CD => 16f;
        public override int Store => 3;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 5);
        private const int Jumps = 8;
        private const float JumpLife = 0.18f;
        private const float JumpRadius = 8f;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id, SkillSeed.New());
            context.AddVectors(entity.transform.position);
            var points = new System.Collections.Generic.List<Vector3> { entity.transform.position };
            Vector3 last = entity.transform.position;
            ushort lastId = entity.id;
            for (int i = 0; i < Jumps; i++)
            {
                Utils.TargetSelect.SetEnemiesInRangeToBuffer(last, JumpRadius, entity.camp);
                EntityData next = null;
                float best = float.MaxValue;
                for (int j = 0; j < Utils.TargetSelect.TargetBuffer.Count; j++)
                {
                    var candidate = Utils.TargetSelect.TargetBuffer[j];
                    if (candidate == null || candidate.id == lastId) continue;
                    float dist = (candidate.transform.position - last).sqrMagnitude;
                    if (dist >= best) continue;
                    best = dist;
                    next = candidate;
                }
                if (next == null) break;
                last = next.transform.position;
                lastId = next.id;
                points.Add(last);
            }
            for (int i = 0; i < points.Count; i++) context.AddVectors(points[i]);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            if (caster == null || Tool.BattleManager == null) return;
            var points = new Vector3[context.vectors.Count - 1];
            for (int i = 1; i < context.vectors.Count; i++) points[i - 1] = context.vectors[i];
            if (points.Length < 2) return;
            var trajectory = new PolylineTrajectory(points);
            trajectory.Duration = JumpLife * (points.Length - 1);
            Tool.BattleManager.ShootBullet(caster, BuildAttack(caster, 1f, 0.5f), trajectory, trajectory.Duration, 0f, 1f, 0.3f);
        }

        public override void PlayVFX(SkillContext context)
        {
            var points = new Vector3[context.vectors.Count - 1];
            for (int i = 1; i < context.vectors.Count; i++) points[i - 1] = context.vectors[i];
            if (points.Length < 2) return;
            var trajectory = new PolylineTrajectory(points);
            trajectory.Duration = JumpLife * (points.Length - 1);
            VfxHelper.PlayOne(SkillVfxKind.Bullet, 5, trajectory);
        }
    }

    public class SkillSweepBurn : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillSweepBurn;
        public override float CastRange => 13f;
        public override float CD => 14f;
        public override int Store => 4;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 6);
        protected override EntityAnchor OriginAnchor => EntityAnchor.UpCenter;
        protected override int Shots => 1;
        protected override float SpreadDeg => 0f;
        protected override float BulletLife => 0.4f;
        protected override int VfxIndex => 8;
        protected override int WaveCount => 5;
        protected override float WaveInterval => 0.1f;
        protected override float SpreadOfWave(int wave) => (wave - 2f) * 10f;
        protected override Action<EntityEffectController> HitEffect(EntityData caster) => ApplyEffect(EffectType.Burning, 5, 5f, caster);
    }

    // 先开法阵，再向 6 个方向连续扫射
    public class SkillCircleVolley : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillCircleVolley;
        public override float CD => 18f;
        public override int Store => 3;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 7);
        protected override EntityAnchor OriginAnchor => EntityAnchor.UpCenter;
        protected override int Shots => 1;
        protected override float SpreadDeg => 0f;
        protected override float BulletLife => 0.6f;
        protected override int VfxIndex => 12;
        protected override int WaveCount => 6;
        protected override float WaveInterval => 0.12f;
        private const float FireRange = 8f;
        private const int CircleVfx = 7;

        public override void PlayVFX(SkillContext context)
        {
            VfxHelper.PlayAt(SkillVfxKind.MagicCircle, CircleVfx, context.vectors[0], 1.2f);
            base.PlayVFX(context);
        }

        protected override BulletTrajectory TrajectoryFor(SkillContext context, int wave, int shotIndex, int shotsInWave, float spread)
        {
            Vector3 origin = OriginPos((ushort)context.ints[0], OriginAnchor, OriginOffset, context.vectors[0]);
            Vector3 dir = Quaternion.Euler(0f, wave * 60f, 0f) * Vector3.forward;
            return TrialConstructor.Line(origin, origin + dir * FireRange, BulletLife);
        }
    }

    // 把一名敌人拉到前方，再扇形散射
    public class SkillHookFan : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillHookFan;
        public override float CastRange => 14f;
        public override float CD => 16f;
        public override int Store => 3;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 8);
        protected override EntityAnchor OriginAnchor => EntityAnchor.UpCenter;
        protected override int Shots => 5;
        protected override float SpreadDeg => 28f;
        protected override float BulletLife => 0.5f;
        protected override int VfxIndex => 14;
        private const float HookTime = 0.35f;

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            if (caster == null) return;
            var target = Utils.TargetSelect.GetNearestEnemy(caster, CastRange);
            if (target != null)
            {
                Vector3 front = caster.transform.position + caster.transform.forward * 3f;
                target.SetMotion(new MotionAttract(front, HookTime, 20f, 40f));
            }
            base.OnCast(context);
        }

        public override void PlayVFX(SkillContext context)
        {
            base.PlayVFX(context);
        }
    }

    // 前方持续吸引，延迟爆炸
    public class SkillVortexBomb : SkillBase
    {
        public override int Id => (int)SkillType.SkillVortexBomb;
        public override float CastRange => 12f;
        public override float CD => 18f;
        public override int Store => 3;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 9);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Mega_Long;
        private const float PullTime = 1.2f;
        private const float PullRadius = 6f;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            Vector3 front = entity.transform.position + entity.transform.forward * 5f;
            context.AddVectors(front);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            if (caster == null) return;
            Vector3 center = context.vectors[0];
            Utils.TargetSelect.SetEnemiesInRangeToBuffer(center, PullRadius, caster.camp);
            for (int i = 0; i < Utils.TargetSelect.TargetBuffer.Count; i++)
            {
                var target = Utils.TargetSelect.TargetBuffer[i];
                if (target != null) target.SetMotion(new MotionAttract(center, PullTime)); // 牵引点 = 施放瞬间的位置
            }
            Timer.AddTimer(0, _ =>
            {
                var stillCaster = FindEntity((ushort)context.ints[0]);
                if (stillCaster == null) return;
                StrikeSphere(stillCaster, center, 3.5f, BuildAttack(stillCaster, 1.6f, 3.5f, useMagic: true, knockback: 4f));
            }, PullTime, 1, false);
        }

        public override void PlayVFX(SkillContext context)
        {
            VfxHelper.PlayAt(SkillVfxKind.RangeMagic, 1, context.vectors[0], PullTime);
        }
    }

    // 周围显现 6 发定点子弹持续伤害
    public class SkillSixOrbs : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillSixOrbs;
        public override float CD => 18f;
        public override int Store => 3;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 10);
        protected override EntityAnchor OriginAnchor => EntityAnchor.UpCenter;
        protected override int Shots => 6;
        protected override float SpreadDeg => 0f;
        protected override float BulletLife => 3f;
        protected override float DamageRate => 0.5f;
        protected override float DamageInterval => 0.6f;
        protected override SkillVfxKind VfxKind => SkillVfxKind.RangeMagic;
        protected override int VfxIndex => 3;
        private const float Ring = 3f;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id, SkillSeed.New());
            context.AddVectors(entity.transform.position);
            return context;
        }

        protected override BulletTrajectory TrajectoryFor(SkillContext context, int wave, int shotIndex, int shotsInWave, float spread)
            => TrialConstructor.Point(context.vectors[0] + Utils.SpreadStyle.CircleDests(Vector3.zero, Ring, 6)[shotIndex], BulletLife);
    }

    public class SkillSkyfallSilence : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillSkyfallSilence;
        public override float CastRange => 16f;
        public override float CD => 16f;
        public override int Store => 3;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 11);
        protected override EntityAnchor OriginAnchor => EntityAnchor.UpCenter;
        protected override int Shots => 1;
        protected override float SpreadDeg => 0f;
        protected override float BulletLife => 1.1f;
        protected override SkillVfxKind VfxKind => SkillVfxKind.RangeMagic;
        protected override int VfxIndex => 4;
        protected override Action<EntityEffectController> HitEffect(EntityData caster) => ApplyEffect(EffectType.Silence, 1, 10f, caster);

        protected override BulletTrajectory TrajectoryFor(SkillContext context, int wave, int shotIndex, int shotsInWave, float spread)
            => TrialConstructor.SkyFall(context.vectors[1] + Vector3.up * 10f, context.vectors[1], BulletLife);
    }

    // 自身位置爆炸：击飞 + 灼烧
    public class SkillBlastKnockback : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillBlastKnockback;
        public override float CastRange => 4f;
        public override float CD => 14f;
        public override int Store => 4;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 12);
        protected override EntityAnchor OriginAnchor => EntityAnchor.UpCenter;
        protected override int Shots => 6;
        protected override float SpreadDeg => 60f;
        protected override float BulletLife => 0.35f;
        protected override float BulletRadius => 0.8f;
        protected override float Knockback => 7f;
        protected override int VfxIndex => 7;
        protected override Action<EntityEffectController> HitEffect(EntityData caster) => ApplyEffect(EffectType.Burning, 3, 10f, caster);

        public override void PlayVFX(SkillContext context)
        {
            base.PlayVFX(context);
        }
    }

    public class SkillBackArcWeapons : ShotSkillBase
    {
        public override int Id => (int)SkillType.SkillBackArcWeapons;
        public override float CastRange => 13f;
        public override float CD => 16f;
        public override int Store => 3;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 13);
        protected override EntityAnchor OriginAnchor => EntityAnchor.UpCenter;
        protected override Vector3 OriginOffset => new Vector3(0f, 0f, -2f);
        protected override int Shots => 3;
        protected override float SpreadDeg => 15f;
        protected override float BulletLife => 0.8f;
        protected override int WaveCount => 3;
        protected override float WaveInterval => 0.15f;
        protected override SkillVfxKind VfxKind => SkillVfxKind.Weapon;
        protected override int VfxIndex => (int)Weapon;

        protected override BulletTrajectory TrajectoryFor(SkillContext context, int wave, int shotIndex, int shotsInWave, float spread)
        {
            Vector3 origin = OriginPos((ushort)context.ints[0], OriginAnchor, OriginOffset, context.vectors[0]);
            Vector3 dest = FanDest(origin, context.vectors[1], shotIndex, shotsInWave, spread);
            return TrialConstructor.Arc(origin, dest, BulletLife);
        }
    }

    public class SkillAllyCritBoost : SkillBase
    {
        public override int Id => (int)SkillType.SkillAllyCritBoost;
        public override float CD => 30f;
        public override int Store => 2;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 14);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Mega_Short;
        private const float Radius = 10f;

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
                if (ally == null || (ally.transform.position - center).sqrMagnitude > Radius * Radius) continue;
                GiveEffect(ally, EffectType.AttrCritRateBoost, 5, 20f, caster);
                GiveEffect(ally, EffectType.AttrCritDamageBoost, 5, 20f, caster);
            }
        }

        public override void PlayVFX(SkillContext context) { } // buff 持续表现由客户端按 Config.buff_vfx 统一播
    }

    public class SkillAllyShieldTwentyFive : SkillBase
    {
        public override int Id => (int)SkillType.SkillAllyShieldTwentyFive;
        public override float CD => 60f;
        public override int Store => 1;
        public override WeaponRef Weapon => new WeaponRef(WeaponCategory.MagicOrb, 15);
        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Mega_Long;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            Utils.TargetSelect.SetEntitiesInCampToBuffer(entity.camp);
            for (int i = 0; i < Utils.TargetSelect.TargetBuffer.Count; i++)
                context.AddInts(Utils.TargetSelect.TargetBuffer[i].id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            for (int i = 1; i < context.ints.Count; i++)
                GiveEffect(BattleManager.GetEntity((ushort)context.ints[i]), EffectType.Shield, 25, 30f, caster);
        }

        public override void PlayVFX(SkillContext context) { } // buff 持续表现由客户端按 Config.buff_vfx 统一播
    }
    #endregion
}
