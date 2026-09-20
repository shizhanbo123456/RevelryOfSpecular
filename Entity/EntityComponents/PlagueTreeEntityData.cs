/// <summary>
/// 瘟疫树：中立争抢单位，攻占后技能冷却恢复翻倍。
/// 生成（多个候选点随机取一）与攻占事件的广播是调度/广播，仍在 BattleManager（NotifyPlagueTreeCaptured）。
/// 本类将承载其 **主动攻击行为**：策划案 6.2 要求"对进入攻击范围的任何单位自动索敌（中立无友方，对攻守双方均敌对）"——
/// 现有 AimPos 对中立阵营会只找其它中立，属待修；且尚无 AI，故暂无实现。
/// </summary>
public class PlagueTreeEntityData : EntityData
{
}
