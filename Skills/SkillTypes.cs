namespace Ros.Skill
{
    // 技能 id 唯一来源：值由枚举顺序自动分配（禁止手写数字），全工程引用技能 id 一律读这里
    // 阶段说明：旧技能已全部作废；这里先按「类别 + 武器槽位」占位，保证武器段连续（Config.GetRandomWeaponId / WeaponRef 依赖），
    // 技能定稿后按技能名重命名并补齐技能类，顺序与段长不变。
    public enum SkillType
    {
        // 刀 11（对应武器槽 0~10）
        SkillKnife01, SkillKnife02, SkillKnife03, SkillKnife04, SkillKnife05, SkillKnife06,
        SkillKnife07, SkillKnife08, SkillKnife09, SkillKnife10, SkillKnife11,

        // 长枪 8
        SkillSpear01, SkillSpear02, SkillSpear03, SkillSpear04,
        SkillSpear05, SkillSpear06, SkillSpear07, SkillSpear08,

        // 枪械 15
        SkillGun01, SkillGun02, SkillGun03, SkillGun04, SkillGun05,
        SkillGun06, SkillGun07, SkillGun08, SkillGun09, SkillGun10,
        SkillGun11, SkillGun12, SkillGun13, SkillGun14, SkillGun15,

        // 魔法球 16
        SkillOrb01, SkillOrb02, SkillOrb03, SkillOrb04, SkillOrb05, SkillOrb06,
        SkillOrb07, SkillOrb08, SkillOrb09, SkillOrb10, SkillOrb11, SkillOrb12,
        SkillOrb13, SkillOrb14, SkillOrb15, SkillOrb16,

        // 防守方 18（6 角色 × 主动1 / 主动2 / 大招，被动不占 id）
        SkillDefense01, SkillDefense02, SkillDefense03, SkillDefense04, SkillDefense05, SkillDefense06,
        SkillDefense07, SkillDefense08, SkillDefense09, SkillDefense10, SkillDefense11, SkillDefense12,
        SkillDefense13, SkillDefense14, SkillDefense15, SkillDefense16, SkillDefense17, SkillDefense18,

        // 非玩家单位 8（普通僵尸 3 / 精英僵尸 3 / 防御塔 1 / 瘟疫树 1）
        SkillNpc01, SkillNpc02, SkillNpc03, SkillNpc04,
        SkillNpc05, SkillNpc06, SkillNpc07, SkillNpc08,

        // 空手攻击 3（左拳 / 右拳 / 原地砸击）
        SkillUnarmed01, SkillUnarmed02, SkillUnarmed03,
    }
}
