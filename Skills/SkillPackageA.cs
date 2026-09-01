using UnityEngine;

namespace Ros.Skill
{
    /// <summary>
    /// 技能包 A：进攻方武器与角色技能（id 区间建议 0~999）。
    /// 【TODO】按新版策划案 11 章实现具体技能：每件武器绑定动作与事件，释放效果=生成子弹/范围判定/召唤/位移。
    /// 技能实现示例：
    ///   public class Skill0 : SkillBase
    ///   {
    ///       public override int Id => 0;
    ///       public override bool Ranged => true;
    ///       public override bool HasWeaponDisplay => true;
    ///       public override EntityAnim.AttackType CastAnim => EntityAnim.AttackType.Mega_Short;
    ///       public override void DoDamageActs(EntityData entity, Vector3 dest) { /* 生成子弹 */ }
    ///       public override void PlayVFX(Vector3 pos, Vector3 dest) { /* 播放特效 */ }
    ///   }
    /// </summary>
    public static class SkillPackageA
    {
        public static class PackageManager
        {
            public static void RegisterAll()
            {
                // TODO: 在此注册本包技能，例如 SkillManager.Register(new Skill0());
            }
        }
    }
}
