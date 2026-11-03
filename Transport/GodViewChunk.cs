// 上帝视角分块容量：协议单包上限 256B，按 220B 预算取值（位级开关决定单条大小）
// 实体：位级 7B / 非位级 24B；珊瑚（仅坐标）：位级 3B / 非位级 8B
public static class GodViewChunk
{
    public static int EntityCapacity => EnsInstance.enableBitTransform ? 24 : 8;
    public static int CoralCapacity => EnsInstance.enableBitTransform ? 64 : 24;
    public const int MaxHeldSkills = 24; // 详情里未装备技能的条数上限（技能栏槽位另算）
}
