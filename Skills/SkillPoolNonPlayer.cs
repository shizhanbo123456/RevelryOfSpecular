using System;
using Ros.Transport;
using UnityEngine;

namespace Ros.Skill
{
    /// <summary>
    /// 非玩家单位技能池（id 100~179）。拥有者：僵尸 / 精英僵尸 / 防御塔 / 瘟疫树。
    /// 与玩家技能共用同一套 SkillBase 逻辑，差异仅在施法来源（非玩家 = AI 自动索敌触发）。
    /// store 统一 -1（无限制）：非玩家单位没有水晶补充渠道，受限会让塔/僵尸打一会儿就哑掉。
    /// 数值（倍率 / 判定半径 / 弹道时长 / 部分特效）为占位初值，待策划定稿。
    /// </summary>
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
    /// <summary>普通僵尸·爪击右（id 101）：动画攻击帧手部球判定。</summary>
    public class SkillZombieClawR : SkillBase
    {
        public override int Id => 101;
        public override float CD => 1.5f;
        public override int Store => -1;

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
            var caster = Caster(context);
            StrikeSphere(caster, HandPos(caster), Config.melee_hit_radius,
                BuildAttack(caster, rate: 1f, radius: Config.melee_hit_radius));
        }

        public override void PlayVFX(SkillContext context) { } // 近战无特效
    }

    /// <summary>普通僵尸·爪击左（id 102）。</summary>
    public class SkillZombieClawL : SkillBase
    {
        public override int Id => 102;
        public override float CD => 1.5f;
        public override int Store => -1;

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
            var caster = Caster(context);
            StrikeSphere(caster, HandPos(caster, leftHand: true), Config.melee_hit_radius,
                BuildAttack(caster, rate: 1f, radius: Config.melee_hit_radius));
        }

        public override void PlayVFX(SkillContext context) { } // 近战无特效
    }

    /// <summary>普通僵尸·嘶吼（id 103）：提升自身攻击力类。</summary>
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
            var caster = Caster(context);
            // 不是伤害：嘶吼为自己提升攻击力
            GiveEffect(caster, EffectType.AttrStrength, 1, Config.buff_duration_buff,
                value: Config.buff_attr_value, sourceId: caster != null ? caster.id : (ushort)0);
        }

        public override void PlayVFX(SkillContext context) { }
    }
    #endregion

    #region 精英僵尸（120~139）
    /// <summary>精英僵尸·爪击右（id 120）。</summary>
    public class SkillEliteZombieClawR : SkillBase
    {
        public override int Id => 120;
        public override float CD => 1.5f;
        public override int Store => -1;

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
            var caster = Caster(context);
            StrikeSphere(caster, HandPos(caster), Config.melee_hit_radius,
                BuildAttack(caster, rate: 1f, radius: Config.melee_hit_radius));
        }

        public override void PlayVFX(SkillContext context) { } // 近战无特效
    }

    /// <summary>精英僵尸·爪击左（id 121）。</summary>
    public class SkillEliteZombieClawL : SkillBase
    {
        public override int Id => 121;
        public override float CD => 1.5f;
        public override int Store => -1;

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
            var caster = Caster(context);
            StrikeSphere(caster, HandPos(caster, leftHand: true), Config.melee_hit_radius,
                BuildAttack(caster, rate: 1f, radius: Config.melee_hit_radius));
        }

        public override void PlayVFX(SkillContext context) { } // 近战无特效
    }

    /// <summary>精英僵尸·嘶吼（id 122）。</summary>
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
            var caster = Caster(context);
            // 不是伤害：嘶吼为自己提升攻击力
            GiveEffect(caster, EffectType.AttrStrength, 1, Config.buff_duration_buff,
                value: Config.buff_attr_value, sourceId: caster != null ? caster.id : (ushort)0);
        }

        public override void PlayVFX(SkillContext context) { }
    }
    #endregion

    #region 防御塔（瘟疫孢子，140~159）
    /// <summary>防御塔·孢子喷射（id 140）：非人形无动画，释放即生效；从碰撞体上部通用发射点直线射出。</summary>
    public class SkillTowerSporeShot : SkillBase
    {
        public override int Id => 140;
        public override float CD => 2f;
        public override int Store => -1;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.Line, AimPos(entity));
            OnCast(context);
            return context;
        }

        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.6f);

        protected override void OnCast(SkillContext context)
        {
            var caster = Caster(context);
            ShootAll(caster, context, BuildAttack(caster, rate: 1f, radius: 0.5f, useMagic: true));
        }

        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 22 });
    }
    #endregion

    #region 瘟疫树（160~179）
    /// <summary>孢子喷发（id 160）：瘟疫树主动攻击，对进入攻击范围的任何单位（中立无友方）自动索敌；
    /// 直线孢子弹 + 命中中毒（弹体 B13 绿能量球）。</summary>
    public class SkillPlagueTreeSpore : SkillBase
    {
        // 中毒数值待策划定稿（策划案 21.6 只记「中立主动攻击」，未给数值）
        private const float PoisonDamagePerTick = 6f;
        private const float PoisonDuration = 4f;

        public override int Id => 160;
        public override float CD => 2.5f;
        public override int Store => -1;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = BuildShotContext(entity, ProjectilePattern.Line, AimPos(entity));
            OnCast(context); // 无动画组件：释放即生效
            return context;
        }

        public override BulletTrajectory CreateTrajectory(SkillContext context, int index) => Line(context, index, 0.6f);

        protected override void OnCast(SkillContext context)
        {
            var caster = Caster(context);
            ShootAll(caster, context, BuildAttack(caster, rate: 1f, radius: 0.5f, useMagic: true,
                addEffect: Poison(caster != null ? caster.id : (ushort)0)));
        }

        /// <summary>命中给目标挂中毒（负面 DoT，1s 一跳）。</summary>
        private static Action<EntityEffectController> Poison(ushort casterId) => effect => effect.AddEffect(
            EffectType.Poison, 1, PoisonDuration, negative: true,
            payload: new EntityEffectController.EffectPayload { damage = PoisonDamagePerTick, sourceId = casterId });

        public override void PlayVFX(SkillContext context) => PlayAlong(context, SkillVfxKind.Bullet, new[] { 13 });
    }
    #endregion
}
