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

        protected override void OnCast(EntityData caster)
            => StrikeSphere(caster, HandPos(caster, leftHand: true).position, MeleeRadius, BuildAttack(caster));
    }

    public class SkillPunchRight : SkillBase
    {
        public override int Id => (int)SkillType.SkillPunchRight;

        protected override void OnCast(EntityData caster)
            => StrikeSphere(caster, HandPos(caster).position, MeleeRadius, BuildAttack(caster));
    }

    public class SkillPunchSmash : SkillBase
    {
        public override int Id => (int)SkillType.SkillPunchSmash;

        private const int Vfx = 6;
        private const float SmashLife = 0.6f;

        // 伤害范围取 RM[6] 的命中球，子弹定在原地陪特效走完
        protected override void OnCast(EntityData caster)
        {
            Vector3 at = caster.transform.position;
            Tool.BattleManager.ShootBullet(caster, BuildAttack(caster),
                new SkillTrajectoryPoint(at, SmashLife), SmashLife,
                HitSpec(SkillVfxKind.RangeMagic, Vfx, VfxScale()));
            SendWave(caster, 0, context => context.WriteVector3(at));
        }

        public override void PlayWave(SCSkillDisplayContext context)
            => VfxHelper.PlayAt(SkillVfxKind.RangeMagic, Vfx, context.ReadVector3(), SmashLife, VfxScale());
    }
}
