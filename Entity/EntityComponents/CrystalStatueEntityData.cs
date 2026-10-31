public class CrystalStatueEntityData : EntityData
{
    private CrystalStatueAi ai;

    public override void OnCreate(ushort id, EntityType type, int level, EntityCamp camp = EntityCamp.Neutral)
    {
        base.OnCreate(id, type, level, camp);
        ai = new CrystalStatueAi(this);
    }

    public override void TickAI()
    {
        ai.Tick();
    }
}
