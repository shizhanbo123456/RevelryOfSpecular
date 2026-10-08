using Ros.Transport;
using UnityEngine;

namespace Ros.Skill
{
    public static class SkillPoolUnarmed
    {
        public static void RegisterAll()
        {
            SkillManager.Register(new SkillPunchLeft());
            SkillManager.Register(new SkillPunchRight());
            SkillManager.Register(new SkillPunchSmash());
        }
    }

    public class SkillPunchLeft : SkillBase
    {
        public override int Id => Config.unarmed_punch_left;
        public override float CastRange => 2f;
        public override float CD => Config.unarmed_skill_cd;
        public override int Store => -1;

        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Hand_L;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
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

    public class SkillPunchRight : SkillBase
    {
        public override int Id => Config.unarmed_punch_right;
        public override float CastRange => 2f;
        public override float CD => Config.unarmed_skill_cd;
        public override int Store => -1;

        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Attack_Hand_R;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
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

    public class SkillPunchSmash : SkillBase
    {
        public override int Id => Config.unarmed_attack_smash;
        public override float CastRange => 2f;
        public override float CD => Config.unarmed_skill_cd;
        public override int Store => -1;

        protected override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Jump_Mega;
        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            return context;
        }

        protected override void OnCast(SkillContext context)
        {
            var caster = GetCaster(context);
            StrikeSphere(caster, caster.transform.position, Config.melee_hit_radius,
                BuildAttack(caster, rate: 1f, radius: Config.melee_hit_radius));
        }

        public override void PlayVFX(SkillContext context) { } // 近战无特效
    }
}
