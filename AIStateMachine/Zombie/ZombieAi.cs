// 僵尸状态机：占位骨架，尚未接线。现有行为仍由 EntityComponents/ZombieEntityData 内联负责
public class ZombieAi : AiStateMachine
{
    public ZombieEntityData Owner { get; }

    public ZombieAi(ZombieEntityData owner)
    {
        Owner = owner;
    }

    // 待迁移：占位期不产生任何行为，也不修改实体状态
    public override void Tick() { }
}
