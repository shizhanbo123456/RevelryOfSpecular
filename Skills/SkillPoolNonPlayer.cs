using System;
using Ros.Transport;
using UnityEngine;

namespace Ros.Skill
{
    public static class SkillPoolNonPlayer
    {
        public static void RegisterAll()
        {
            SkillManager.Register(new SkillZombieClawR());     // 101
            SkillManager.Register(new SkillZombieClawL());     // 102
            SkillManager.Register(new SkillZombieScream());    // 103
            SkillManager.Register(new SkillEliteZombieClawR());// 120
            SkillManager.Register(new SkillEliteZombieClawL());// 121
            SkillManager.Register(new SkillEliteZombieScream());// 122
            SkillManager.Register(new SkillTowerSporeShot());  // 140
            SkillManager.Register(new SkillPlagueTreeSpore()); // 160
        }
    }

    #region 普通僵尸（101~119）
    public class SkillZombieClawR : SkillBase
    {
        public override int Id => 101;
        public override float CD => 1.5f;
        public override int Store => -1;
        public override float CastRange => Config.zombie_attack_range;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Zombie_Hand_Attack_R, () => OnCast(context)))
            {
                OnCast(context);
            }
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = GetCaster(context);
            StrikeSphere(caster, HandPos(caster), Config.melee_hit_radius,
                BuildAttack(caster, rate: 1f, radius: Config.melee_hit_radius));
        }

        public override void PlayVFX(SkillContext context) { } // 近战无特效
    }

    public class SkillZombieClawL : SkillBase
    {
        public override int Id => 102;
        public override float CD => 1.5f;
        public override int Store => -1;
        public override float CastRange => Config.zombie_attack_range;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Zombie_Hand_Attack_L, () => OnCast(context)))
            {
                OnCast(context);
            }
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = GetCaster(context);
            StrikeSphere(caster, HandPos(caster, leftHand: true), Config.melee_hit_radius,
                BuildAttack(caster, rate: 1f, radius: Config.melee_hit_radius));
        }

        public override void PlayVFX(SkillContext context) { } // 近战无特效
    }

    public class SkillZombieScream : SkillBase
    {
        public override int Id => 103;
        public override float CD => 8f;
        public override int Store => -1;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Zombie_Scream, () => OnCast(context)))
            {
                OnCast(context);
            }
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = GetCaster(context);
            // 不是伤害：嘶吼为自己提升攻击力
            GiveEffect(caster, EffectType.AttrStrength, 1, Config.buff_duration_buff,
                value: Config.buff_attr_value, sourceId: caster != null ? caster.id : (ushort)0);
        }

        public override void PlayVFX(SkillContext context) { }
    }
    #endregion

    #region 精英僵尸（120~139）
    public class SkillEliteZombieClawR : SkillBase
    {
        public override int Id => 120;
        public override float CD => 1.5f;
        public override int Store => -1;
        public override float CastRange => Config.zombie_attack_range;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Zombie_Hand_Attack_R, () => OnCast(context)))
            {
                OnCast(context);
            }
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = GetCaster(context);
            StrikeSphere(caster, HandPos(caster), Config.melee_hit_radius,
                BuildAttack(caster, rate: 1f, radius: Config.melee_hit_radius));
        }

        public override void PlayVFX(SkillContext context) { } // 近战无特效
    }

    public class SkillEliteZombieClawL : SkillBase
    {
        public override int Id => 121;
        public override float CD => 1.5f;
        public override int Store => -1;
        public override float CastRange => Config.zombie_attack_range;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Zombie_Hand_Attack_L, () => OnCast(context)))
            {
                OnCast(context);
            }
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = GetCaster(context);
            StrikeSphere(caster, HandPos(caster, leftHand: true), Config.melee_hit_radius,
                BuildAttack(caster, rate: 1f, radius: Config.melee_hit_radius));
        }

        public override void PlayVFX(SkillContext context) { } // 近战无特效
    }

    public class SkillEliteZombieScream : SkillBase
    {
        public override int Id => 122;
        public override float CD => 8f;
        public override int Store => -1;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Zombie_Scream, () => OnCast(context)))
            {
                OnCast(context);
            }
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = GetCaster(context);
            // 不是伤害：嘶吼为自己提升攻击力
            GiveEffect(caster, EffectType.AttrStrength, 1, Config.buff_duration_buff,
                value: Config.buff_attr_value, sourceId: caster != null ? caster.id : (ushort)0);
        }

        public override void PlayVFX(SkillContext context) { }
    }
    #endregion

    #region 防御塔（瘟疫孢子，140~159）
    public class SkillTowerSporeShot : SkillBase
    {
        public override int Id => 140;
        public override float CD => 2f;
        public override int Store => -1;
        public override float CastRange => Config.tower_attack_range;

        private const int BlazeFlagIndex = 2;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, Utils.TargetSelect.AimPos(entity,CastRange));
            // 灵火在攻击生成时查询一次：之后塔身上的 Buff 变化不影响这一发
            bool blaze = entity.effectController != null && entity.effectController.HasEffect(EffectType.TowerBlaze);
            context.AddInts(blaze ? 1 : 0);
            OnCast(context);
            return context;
        }

        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => TrialConstructor.Line(GetShotOrigin(context, index), GetShotAim(context, index), 0.6f);

        protected override void OnCast(SkillContext context)
        {
            var caster = GetCaster(context);
            bool blaze = context.ints != null && context.ints.Count > BlazeFlagIndex && context.ints[BlazeFlagIndex] != 0;
            ShootAll(caster, context, BuildAttack(caster, rate: 1f, radius: 0.5f, useMagic: true,
                onHit: blaze ? BlazeHit(caster) : null));
        }

        private Action<EntityData> BlazeHit(EntityData caster) => target =>
        {
            if (caster == null || target == null) return;
            StrikeSphere(caster, target.transform.position, Config.tower_blaze_radius,
                BuildAttack(caster, rate: Config.tower_blaze_rate, radius: Config.tower_blaze_radius, useMagic: true));
        };

        public override void PlayVFX(SkillContext context)
        {
            PlayShotVfx(context, SkillVfxKind.Bullet, new[] { 22 });
            if (context.ints == null || context.ints.Count <= BlazeFlagIndex || context.ints[BlazeFlagIndex] == 0) return;
            for (int i = 0; i < (context.vectors != null ? context.vectors.Count / 2 : 0); i++)
            {
                VfxHelper.PlayAt(SkillVfxKind.RangeMagic, 7, context.vectors[i * 2 + 1], 1f); // RM8 岩浆连环爆炸
            }
        }
    }
    #endregion

    #region 瘟疫树（160~179）
    public class SkillPlagueTreeSpore : SkillBase
    {
        // 中毒数值待策划定稿（策划案 21.6 只记「中立主动攻击」，未给数值）
        private const float PoisonDamagePerTick = 6f;
        private const float PoisonDuration = 4f;

        public override int Id => 160;
        public override float CD => 2.5f;
        public override int Store => -1;
        public override float CastRange => Config.plague_tree_attack_range;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, Utils.TargetSelect.AimPos(entity,CastRange));
            OnCast(context); // 无动画组件：释放即生效
            return context;
        }

        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => TrialConstructor.Line(GetShotOrigin(context, index), GetShotAim(context, index), 0.6f);

        protected override void OnCast(SkillContext context)
        {
            var caster = GetCaster(context);
            ShootAll(caster, context, BuildAttack(caster, rate: 1f, radius: 0.5f, useMagic: true,
                addEffect: Poison(caster != null ? caster.id : (ushort)0)));
        }

        private static Action<EntityEffectController> Poison(ushort casterId) => effect => effect.AddEffect(
            EffectType.Poison, 1, PoisonDuration, negative: true,
            payload: new EntityEffectController.EffectPayload { damage = PoisonDamagePerTick, sourceId = casterId });

        public override void PlayVFX(SkillContext context) => PlayShotVfx(context, SkillVfxKind.Bullet, new[] { 13 });
    }
    #endregion
}
