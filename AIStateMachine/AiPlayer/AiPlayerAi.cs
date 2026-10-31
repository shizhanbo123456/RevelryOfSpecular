// AI 玩家状态机：占位骨架，尚未接线。现有行为仍由 EntityComponents/PlayerAiController 负责
public class AiPlayerAi : AiStateMachine
{
    public PlayerEntityData Owner { get; }

    public AiPlayerAi(PlayerEntityData owner)
    {
        Owner = owner;
    }

    // 待迁移：占位期不产生任何行为，也不修改实体状态
    public override void Tick() { }
}
