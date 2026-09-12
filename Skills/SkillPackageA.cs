using Ros.Transport;
using UnityEngine;

namespace Ros.Skill
{
    /// <summary>
    /// 技能包 A：进攻方武器与角色技能（占位容器；正式技能见 SkillPoolWeapons / SkillPoolUnarmed 等技能池）。
    /// 新增技能继承 SkillBase，在 RegisterAll 中注册即可。
    /// </summary>
    public static class SkillPackageA
    {
        public static class PackageManager
        {
            public static void RegisterAll()
            {
                // TODO: 在此注册本包技能，例如 SkillManager.Register(new Skill1());
            }
        }
    }
}
