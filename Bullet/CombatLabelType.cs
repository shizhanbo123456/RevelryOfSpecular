// 战斗飘字类型：描述单次结算的结果（伤害或治疗），随数值一起下发。
// 枚举值必须与 FGUI UI_DamageLabel 控制器页号一一对应，新增值须同步加页。
public enum CombatLabelType
{
    Normal = 0,       // 正常
    Crit = 1,         // 暴击
    Evade = 2,        // 闪避（命中判定未通过，无伤害）
    Invalid = 3,      // 失效（无护盾参与却打成 0 伤害：免疫 / 无敌帧 / 减伤减到 0）
    Shield = 4,       // 护盾（被护盾挡下，数值为名义伤害，不按实际掉血）
    Burning = 5,      // 燃烧（DoT）
    PaleFire = 6,     // 苍白之火（DoT）
    Poison = 7,       // 中毒（DoT）
    DeathStroll = 8,  // 死灵漫步（光环 DoT）
    Reflect = 9,      // 反伤（反弹给攻击者的伤害）
    PlagueBurst = 10, // 瘟疫引爆
    Heal = 11,        // 治疗（数值为恢复量）
}
