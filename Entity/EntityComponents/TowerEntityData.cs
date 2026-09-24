/// <summary>
/// 防御塔（瘟疫孢子）：被摧毁后不复活。
/// 生成（4 座塔各配一种外观）与「不复活」的表现为：摧毁时无人为它排重生，因此调度留在 BattleManagerWorld。
/// 攻击行为 = 周期随机释放技能（见 AutoCastEntityData）；开火范围由 SkillTowerSporeShot 的 CastRange（20m）把守，
/// 策划案 8.1「靠近即被攻击、无预警」由此成立。
/// </summary>
public class TowerEntityData : AutoCastEntityData
{
    /// <summary>开火间隔（秒）。</summary>
    public override float AutoCastInterval => 5f;
}
