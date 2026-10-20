// 伤害类型：随伤害数值一起下发，飘字样式与统计按类型区分（不再用数值正负编码暴击）
public enum DamageType
{
    Normal,         // 普通
    Crit,           // 暴击
    Evade,          // 闪避（命中判定未通过，无伤害）
    Invalid,        // 无效（护盾全吸收 / 免疫 / 无敌帧 / 阵型不符）
    ShieldAbsorb,   // 护盾吸收（伤害被盾吃掉，血条不变）
    Reflect,        // 反伤
    DoT,            // 持续伤害（燃烧 / 中毒 / 死灵漫步）
}
