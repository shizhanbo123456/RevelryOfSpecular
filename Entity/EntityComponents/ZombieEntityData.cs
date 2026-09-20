/// <summary>
/// 僵尸与精英僵尸（共用，差异在属性与技能表）。
/// 刷新产出（夜间进度、数量上限、出生点、外观变体、等级）是对局级调度，仍在 BattleManagerWorld。
/// 本类将承载其 **AI 行为**：索敌、移动、攻击/技能触发时机 —— 目前尚无 AI（`BattleManager.UpdateAI` 为空），故暂无实现。
/// </summary>
public class ZombieEntityData : EntityData
{
}
