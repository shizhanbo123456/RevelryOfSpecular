namespace Ros.Skill
{
    // 新技能包注册入口：按拥有者类型分文件，统一在这里登记
    public static class SkillPools
    {
        public static void RegisterAll()
        {
            SkillPoolWeapon.RegisterAll();   // 武器技能 50（刀 / 长枪 / 枪械 / 魔法球）
            SkillPoolDefense.RegisterAll();  // 防守方 6 角色 × 主动1 / 主动2 / 大招
            SkillPoolNpc.RegisterAll();      // 僵尸 / 水晶雕像 / 中立雕像
            SkillPoolUnarmed.RegisterAll();  // 空手左拳 / 右拳 / 砸击
        }
    }
}
