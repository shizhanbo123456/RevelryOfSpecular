using Ros.Transport;
using UnityEngine;

namespace Ros.Skill
{
    /// <summary>
    /// 空手攻击技能池（id 180~182）。拥有者：徒手实体（玩家 J 键按「移动/静止」选技能，走 TryUseSkill 同一链路）。
    /// 共用规则：动画攻击帧以手部骨骼为球心（砸击用角色位置）做球判定，半径 Config.melee_hit_radius，无特效。
    /// store 统一 -1（无限制），CD 取 Config.unarmed_skill_cd。
    /// </summary>
    public static class SkillPoolUnarmed
    {
        public static void RegisterAll()
        {
            SkillManager.Register(new SkillPunchLeft());
            SkillManager.Register(new SkillPunchRight());
            SkillManager.Register(new SkillPunchSmash());
        }
    }

    /// <summary>空手攻击·左手拳击（id 180）：移动中随机触发，判定球心 = 左手骨骼。</summary>
    public class SkillPunchLeft : SkillBase
    {
        public override int Id => Config.unarmed_punch_left;
        public override float CD => Config.unarmed_skill_cd;
        public override int Store => -1;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Hand_L, () => OnCast(context)))
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

    /// <summary>空手攻击·右手拳击（id 181）：移动中随机触发，判定球心 = 右手骨骼。</summary>
    public class SkillPunchRight : SkillBase
    {
        public override int Id => Config.unarmed_punch_right;
        public override float CD => Config.unarmed_skill_cd;
        public override int Store => -1;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Attack_Hand_R, () => OnCast(context)))
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

    /// <summary>空手攻击·原地砸击（id 182）：静止时触发，判定球心 = 角色位置（脚底落地冲击）。</summary>
    public class SkillPunchSmash : SkillBase
    {
        public override int Id => Config.unarmed_attack_smash;
        public override float CD => Config.unarmed_skill_cd;
        public override int Store => -1;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            if (!WaitAttackFrame(entity, EntityAnim.AttackType.Jump_Mega, () => OnCast(context)))
            {
                OnCast(context);
            }
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = Caster(context);
            StrikeSphere(caster, caster.transform.position, Config.melee_hit_radius,
                BuildAttack(caster, rate: 1f, radius: Config.melee_hit_radius));
        }

        public override void PlayVFX(SkillContext context) { } // 近战无特效
    }
}
