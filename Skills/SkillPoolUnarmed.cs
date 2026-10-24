using Ros.Skill.Utils;
using Ros.Transport;
using UnityEngine;

namespace Ros.Skill
{
    // 空手攻击：左拳 / 右拳 / 原地砸击（近战无弹道，砸击带范围魔法表现）
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
        public override int Id => (int)SkillType.SkillPunchLeft;
        public override float CastRange => 2f;

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

    public class SkillPunchRight : SkillBase
    {
        public override int Id => (int)SkillType.SkillPunchRight;
        public override float CastRange => 2f;

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

    public class SkillPunchSmash : SkillBase
    {
        public override int Id => (int)SkillType.SkillPunchSmash;
        public override float CastRange => 2f;
        private const float SmashLife = 0.6f;

        public override SkillContext SkillLogic(EntityData entity)
        {
            var context = new SkillContext();
            context.AddInts(entity.id);
            context.AddVectors(entity.transform.position);
            return context;
        }

        // 有特效：伤害范围取 RM[6] 的命中球，子弹定在原地陪特效走完
        protected override void OnCast(SkillContext context)
        {
            var caster = FindEntity((ushort)context.ints[0]);
            if (caster == null) return;
            Tool.BattleManager.ShootBullet(caster, BuildAttack(caster, 1f),
                TrialConstructor.Point(caster.transform.position, SmashLife), SmashLife,
                HitSpec(SkillVfxKind.RangeMagic, 6, VfxScale()));
        }

        public override void PlayVFX(SkillContext context)
            => VfxHelper.PlayAt(SkillVfxKind.RangeMagic, 6, context.vectors[0], SmashLife, VfxScale());
    }
}
