using UnityEngine;

namespace Ros.Skill
{
    /// <summary>
    /// 空手攻击技能池（id 180~182，分配见策划案 21.6）。
    /// 触发规则：移动中随机左右拳，静止时原地砸击（J 键走 TryUseSkill，与其它技能同一条释放链路）。
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

    /// <summary>
    /// 空手攻击基类：动画攻击帧到达时，以手部骨骼（或角色位置）为圆心、Config.melee_hit_radius 为半径，
    /// 对球内全部敌方各结算一次（打不到同阵营；中立单位如水晶可被打）。
    /// </summary>
    public abstract class UnarmedStrikeSkill : SkillStub
    {
        private static readonly EntityData[] s_hitBuffer = new EntityData[16];

        private readonly EntityAnim.AttackType castAnim;
        private readonly bool leftHand;
        private readonly bool useBodyCenter;

        protected UnarmedStrikeSkill(int id, EntityAnim.AttackType castAnim, bool leftHand, bool useBodyCenter = false)
            : base(id, Config.unarmed_skill_cd, -1)
        {
            this.castAnim = castAnim;
            this.leftHand = leftHand;
            this.useBodyCenter = useBodyCenter;
        }

        /// <summary>释放动作 = 该拳法对应的攻击动作（AnimAttackEvent 按此播动画并在攻击帧回调）。</summary>
        public override EntityAnim.AttackType CastAnim => castAnim;
        /// <summary>近战接触判定，非远程（不参与右键触发）。</summary>
        public override bool Ranged => false;

        protected override void Execute(EntityData entity, Vector3 dest)
        {
            Vector3 center = useBodyCenter
                ? entity.transform.position
                : entity.GetComponentInChildren<EntityAnim>().GetHandMount(leftHand).position;
            int count = EntityPhysics.OverlapSphere(center, Config.melee_hit_radius, s_hitBuffer);
            var attack = AttackData.Create(entity, rate: 1f, radius: Config.melee_hit_radius,
                breakEndure: false, useMagic: false);
            for (int i = 0; i < count; i++)
            {
                var target = s_hitBuffer[i];
                if (!target.Alive) continue;
                if (target.id == entity.id || target.camp == entity.camp) continue;
                target.ProcessHit(attack, attack.GetDamage());
            }
        }
    }

    /// <summary>空手攻击·左手拳击（id 180）：移动中随机触发；判定球心 = 左手骨骼。</summary>
    public class SkillPunchLeft : UnarmedStrikeSkill
    {
        public SkillPunchLeft() : base(Config.unarmed_punch_left, EntityAnim.AttackType.Attack_Hand_L, true) { }
    }

    /// <summary>空手攻击·右手拳击（id 181）：移动中随机触发；判定球心 = 右手骨骼。</summary>
    public class SkillPunchRight : UnarmedStrikeSkill
    {
        public SkillPunchRight() : base(Config.unarmed_punch_right, EntityAnim.AttackType.Attack_Hand_R, false) { }
    }

    /// <summary>空手攻击·原地砸击（id 182）：静止时触发；判定球心 = 角色位置（脚底，落地冲击）。</summary>
    public class SkillPunchSmash : UnarmedStrikeSkill
    {
        public SkillPunchSmash() : base(Config.unarmed_attack_smash, EntityAnim.AttackType.Jump_Mega, false, true) { }
    }
}
