/// <summary>
/// 防御塔（瘟疫孢子）：被摧毁后不复活。
/// 生成（4 座塔各配一种外观）与「不复活」的表现为：摧毁时无人为它排重生，因此调度留在 BattleManagerWorld。
/// 本类将承载其 **攻击行为**（索敌、开火时机、TowerBlaze 附加爆炸的触发）—— 目前尚无 AI，故暂无实现。
/// </summary>
public class TowerEntityData : EntityData
{
}
