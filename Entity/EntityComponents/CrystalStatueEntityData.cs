public class CrystalStatueEntityData : EntityData
{
    private CrystalStatueStateMachine stateMachine;

    public override void OnCreate(ushort id, EntityType type, int level, EntityCamp camp = EntityCamp.Neutral)
    {
        base.OnCreate(id, type, level, camp);
        stateMachine = new CrystalStatueStateMachine(this);
    }

    public override void TickAuto()
    {
        stateMachine.Tick();
    }
}
