/// <summary>
/// 防御塔（瘟疫孢子）：被摧毁后不复活。
/// 生成（4 座塔各配一种外观）与「不复活」的表现为：摧毁时无人为它排重生，因此调度留在 BattleManagerWorld。
/// 攻击行为 = 周期随机释放技能（见 AutoCastEntityData）；**索敌由本单位的 AI 判断**——只有 20m
/// （<see cref="Config.tower_attack_range"/>）内有敌人才开火，策划案 8.1「靠近即被攻击、无预警」由此成立。
/// </summary>
public class TowerEntityData : AutoCastEntityData
{
    /// <summary>开火间隔（秒）。</summary>
    public override float AutoCastInterval => 5f;

    /// <summary>索敌半径 = 开火范围 20m（与技能 SkillTowerSporeShot 的 CastRange 同源）。</summary>
    protected override float CastSearchRange => Config.tower_attack_range;
}
